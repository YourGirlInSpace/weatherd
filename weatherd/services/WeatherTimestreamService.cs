using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using UnitsNet.Units;
using weatherd.datasources;

namespace weatherd.services
{
    public interface IWeatherTimestreamService
    {
        Task<bool> Initialize(IAsyncWeatherDataSource dataSource);
        Task<bool> Start();
        Task<bool> Stop();
    }

    public class WeatherTimestreamService : IWeatherTimestreamService
    {
        private readonly IConfiguration _config;
        private IAsyncWeatherDataSource _wxDataSource;
        private WeatherState lastState;
        private AmazonTimestreamWriteClient timestreamClient;
        private readonly bool _enableDataWrite = true;

        public float _altitude = 0;

        public WeatherTimestreamService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            IConfigurationSection tsConfig = config.GetSection("TimestreamService");

            if (tsConfig is not null)
            {
                _enableDataWrite = tsConfig.GetValue("EnableDataWrite", true);
                _altitude = tsConfig.GetValue("Altitude", 0f);
            }
        }

        /// <inheritdoc />
        public async Task<bool> Initialize(IAsyncWeatherDataSource dataSource)
        {
            _wxDataSource = dataSource;
            if (_wxDataSource.Initialized)
                return true;

            if (!await _wxDataSource.Initialize())
                throw new InvalidOperationException("Could not initialize data source.");

            timestreamClient = new AmazonTimestreamWriteClient(new EnvironmentVariablesAWSCredentials(), RegionEndpoint.USEast1);

            ListTablesResponse listResp = await timestreamClient.ListTablesAsync(new ListTablesRequest()
            {
                DatabaseName = "weather"
            });

            if (listResp.Tables.Any(
                table => table.TableName.Equals("local", StringComparison.InvariantCultureIgnoreCase)))
                return true;

            CreateTableResponse createResp = await timestreamClient.CreateTableAsync(new CreateTableRequest
            {
                DatabaseName = "weather",
                TableName = "local"
            });

            if (createResp.HttpStatusCode == HttpStatusCode.OK)
                return true;

            Log.Fatal("Could not create Timestream table '{tableName}'.", "local");
            return false;

        }

        /// <inheritdoc />
        public async Task<bool> Start()
        {
            if (!_wxDataSource.Running)
            {
                if (!await _wxDataSource.Start())
                    throw new InvalidOperationException("Could not start data source.");
            }

            _wxDataSource.SampleAvailable += WxDataSourceOnSampleAvailable;

            return true;
        }

        private async void WxDataSourceOnSampleAvailable(object sender, WeatherDataEventArgs e)
        {
            WeatherState wxState = _wxDataSource.Conditions;

            if (wxState is null)
            {
                Log.Warning("Could not retrieve valid sample from data source.");
                return;
            }

            if (lastState != null && lastState.Time == wxState.Time)
            {
                Log.Warning("Sample retrieved from weather data source did not update at polling interval");
                return;
            }

            // Sea level pressure compensation
            if (_altitude != 0)
                wxState.SeaLevelPressure = new Pressure(wxState.Pressure.Pascals * Math.Pow(1 - (0.0065 * _altitude) / (wxState.Temperature.DegreesCelsius + 0.0065 * _altitude + 273.15), -5.257), PressureUnit.Pascal);
            
            Log.Verbose("Sample:  T={temp}  Dp={dewpoint}  RH={relativeHumidity}  P={pressure} SLP={seaLevelPressure} L={irradiance}  Ws={windSpeed}  Wd={windDir}  Rain={rain}",
                        wxState.Temperature.ToUnit(TemperatureUnit.DegreeFahrenheit),
                        wxState.Dewpoint.ToUnit(TemperatureUnit.DegreeFahrenheit),
                        wxState.RelativeHumidity,
                        wxState.Pressure.ToUnit(PressureUnit.InchOfMercury),
                        wxState.SeaLevelPressure.ToUnit(PressureUnit.InchOfMercury),
                        wxState.Luminosity.ToUnit(IrradianceUnit.WattPerSquareMeter),
                        wxState.WindSpeed.ToUnit(SpeedUnit.MilePerHour),
                        wxState.WindDirection,
                        wxState.RainfallLastHour.ToUnit(LengthUnit.Inch));

            if (_enableDataWrite)
            {
                try
                {
                    await WriteToTimestream(wxState);
                } catch (Exception ex)
                {
                    Log.Error(ex, "Failed to ingest logs into TimeStream");
                }
            }

            lastState = wxState;
        }

        private async Task WriteToTimestream(WeatherState wxState)
        {
            WriteRecordsRequest recordsRequest = new WriteRecordsRequest
            {
                DatabaseName = "weather",
                TableName = "local",
                CommonAttributes = new Record
                {
                    Time = "now",
                    Dimensions = new List<Dimension>
                    {
                        new()
                        {
                            Name = "Location",
                            DimensionValueType = DimensionValueType.VARCHAR,
                            Value = "Local"
                        }
                    }
                },
                Records = new List<Record>
                {
                    new()
                    {
                        MeasureName = "Temperature",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.Temperature.DegreesCelsius.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "Dewpoint",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.Dewpoint.DegreesCelsius.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "RelativeHumidity",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.RelativeHumidity.Percent.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "BarometricPressure",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.Pressure.Hectopascals.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "SeaLevelPressure",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.SeaLevelPressure.Hectopascals.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "Luminosity",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.Luminosity.WattsPerSquareMeter.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "WindSpeed",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.WindSpeed.MetersPerSecond.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "WindDirection",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.WindDirection.Degrees.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "RainSinceMidnight",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.RainfallSinceMidnight.Millimeters.ToString(CultureInfo.InvariantCulture)
                    },
                    new()
                    {
                        MeasureName = "IRainRate",
                        MeasureValueType = MeasureValueType.DOUBLE,
                        MeasureValue = wxState.InstantaneousRainRate.MillimetersPerHour.ToString(CultureInfo.InvariantCulture)
                    }
                }
            };

            var response = await timestreamClient.WriteRecordsAsync(recordsRequest);

            Log.Verbose("Records ingested: {recordsIngest}", response.RecordsIngested.Total);
        }

        /// <inheritdoc />
        public Task<bool> Stop() => _wxDataSource.Stop();
    }
}

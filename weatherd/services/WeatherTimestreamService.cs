using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet.Units;
using weatherd.datasources;
using weatherd.io;

namespace weatherd.services
{
    public interface IWeatherTimestreamService
    {
        bool IsRunning { get; }
        Task<bool> Initialize(IAsyncWeatherDataSource dataSource);
        Task<bool> Start(AutoResetEvent endSignaller);
        Task<bool> Stop();
    }

    public class WeatherTimestreamService : IWeatherTimestreamService
    {
        private IAsyncWeatherDataSource _wxDataSource;
        private WeatherState lastState;
        private readonly ITimestreamClient timestreamClient;
        private readonly bool _enableDataWrite = true;
        private AutoResetEvent _endSignaller;

        public WeatherTimestreamService(IConfiguration config, ITimestreamClient client)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            timestreamClient = client;

            IConfigurationSection tsConfig = config.GetSection("TimestreamService");

            if (tsConfig is not null)
            {
                _enableDataWrite = tsConfig.GetValue("EnableDataWrite", true);
            }
        }

        /// <inheritdoc />
        public bool IsRunning => _wxDataSource.Running;

        /// <inheritdoc />
        public async Task<bool> Initialize(IAsyncWeatherDataSource dataSource)
        {
            _wxDataSource = dataSource;
            if (_wxDataSource.Initialized)
                return true;

            if (!await _wxDataSource.Initialize())
                throw new InvalidOperationException("Could not initialize data source.");

            timestreamClient.Connect();

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
        public async Task<bool> Start(AutoResetEvent endSignaller)
        {
            _endSignaller = endSignaller;
            _endSignaller?.Reset();

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
                } catch (RecordIngestForbiddenException rife)
                {
                    Log.Fatal(rife, "Cannot write records to Timestream.  Terminating...");
                    await Stop();
                } catch (Exception ex)
                {
                    Log.Error(ex, "Failed to ingest logs into TimeStream");
                    throw;
                }
            }

            lastState = wxState;
        }

        private async Task WriteToTimestream(WeatherState wxState)
        {
            List<Record> records = new List<Record>();
            
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.Temperature,
                               n => n.DegreesCelsius,
                               "Temperature");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.Dewpoint,
                               n => n.DegreesCelsius,
                               "Dewpoint");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.RelativeHumidity,
                               n => n.Percent,
                               "RelativeHumidity");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.Pressure,
                               n => n.Hectopascals,
                               "BarometricPressure");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.SeaLevelPressure,
                               n => n.Hectopascals,
                               "SeaLevelPressure");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.Luminosity,
                               n => n.WattsPerSquareMeter,
                               "Luminosity");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.WindSpeed,
                               n => n.MetersPerSecond,
                               "WindSpeed");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.WindDirection,
                               n => n.Degrees,
                               "WindDirection");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.RainfallSinceMidnight,
                               n => n.Millimeters,
                               "RainSinceMidnight");

            if (records.Count == 0)
                return;

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
                Records = records
            };

            try
            {
                var response = await timestreamClient.WriteRecordsAsync(recordsRequest);

                switch (response.HttpStatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        throw new RecordIngestForbiddenException(
                            $"Failed to ingest Timestream records:  {response.HttpStatusCode} in request ID {response.ResponseMetadata.RequestId}!");
                    case HttpStatusCode.OK:
                        Log.Information("Records ingested: {recordsIngest} in request ID {requestId}",
                                        response.RecordsIngested.Total, response.ResponseMetadata.RequestId);
                        break;
                    default:
                        Log.Warning(
                            "Failed to ingest Timestream records.  Status code: {statusCode} in request ID {requestId}",
                            response.HttpStatusCode, response.ResponseMetadata.RequestId);
                        break;
                }
            }
            catch (RejectedRecordsException)
            {
                // Ignore it
            }
        }

        private static void AddIfNotDuplicated<T>(ICollection<Record> list, WeatherState last, WeatherState current, Func<WeatherState, T> unitSelector, Func<T, double> valueSelector, string name)
            where T : struct
        {
            T lastMeasurement = unitSelector(last);
            T currentMeasurement = unitSelector(current);

            double lastValue = valueSelector(lastMeasurement);
            double currentValue = valueSelector(currentMeasurement);

            if (Math.Abs(lastValue - currentValue) < 1e-5)
                return;
            
            list.Add(new Record
            {
                MeasureName = name,
                MeasureValueType = MeasureValueType.DOUBLE,
                MeasureValue = currentValue.ToString(CultureInfo.InvariantCulture)
            });
        }
        
        /// <inheritdoc />
        public async Task<bool> Stop()
        {
            await Retry.DoAsync(() => _wxDataSource.Stop(), TimeSpan.FromSeconds(1), 5);
            return _endSignaller?.Set() ?? true;
        }
    }
}

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
        Task<bool> Initialize(params IAsyncWeatherDataSource[] dataSource);
        Task<bool> Start(AutoResetEvent endSignaller);
        Task<bool> Stop();
    }

    public class WeatherTimestreamService : IWeatherTimestreamService
    {
        private IAsyncWeatherDataSource[] _wxDataSources;
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
        public bool IsRunning => _wxDataSources.All(x => x.Running);

        /// <inheritdoc />
        public async Task<bool> Initialize(params IAsyncWeatherDataSource[] dataSource)
        {
            _wxDataSources = dataSource;
            if (_wxDataSources.All(n => n.Initialized))
                return true;

            foreach (IAsyncWeatherDataSource wxds in _wxDataSources)
                if (!await wxds.Initialize())
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

            Log.Fatal("Could not create Timestream table '{TableName}'", "local");
            return false;

        }
        
        /// <inheritdoc />
        public async Task<bool> Start(AutoResetEvent endSignaller)
        {
            _endSignaller = endSignaller;
            _endSignaller?.Reset();

            foreach (IAsyncWeatherDataSource wxds in _wxDataSources)
            {
                if (!wxds.Running)
                {
                    if (!await wxds.Start())
                        throw new InvalidOperationException("Could not start data source.");
                }

                wxds.SampleAvailable += WxDataSourceOnSampleAvailable;
            }

            return true;
        }

        private async void WxDataSourceOnSampleAvailable(object sender, WeatherDataEventArgs e)
        {
            // Don't do anything until we have conditions on ALL data sources
            if (_wxDataSources.Any(n => n.Conditions == null))
                return;
            
            WeatherState wxState = _wxDataSources[0].Conditions;
            
            for (int i = 1; i < _wxDataSources.Length; i++)
                wxState = WeatherState.Merge(wxState, _wxDataSources[i].Conditions);

            if (wxState is null)
            {
                Log.Warning("Could not retrieve valid sample from data sources");
                return;
            }

            if (lastState != null && lastState.Time == wxState.Time)
            {
                Log.Warning("Sample retrieved from weather data source did not update at polling interval");
                return;
            }
            
            /*Log.Verbose("Sample:  T={Temp}  Dp={Dewpoint}  RH={RelativeHumidity}  P={Pressure} SLP={SeaLevelPressure} L={Irradiance}  Ws={WindSpeed}  Wd={WindDir}  Rain={Rain}  Visibility={Visibility}  Weather={Weather}",
                        wxState.Temperature.ToUnit(TemperatureUnit.DegreeFahrenheit),
                        wxState.Dewpoint.ToUnit(TemperatureUnit.DegreeFahrenheit),
                        wxState.RelativeHumidity,
                        wxState.Pressure.ToUnit(PressureUnit.InchOfMercury),
                        wxState.SeaLevelPressure.ToUnit(PressureUnit.InchOfMercury),
                        wxState.Luminosity.ToUnit(IrradianceUnit.WattPerSquareMeter),
                        wxState.WindSpeed.ToUnit(SpeedUnit.MilePerHour),
                        wxState.WindDirection,
                        wxState.RainfallLastHour.ToUnit(LengthUnit.Inch),
                        wxState.Visibility.ToUnit(LengthUnit.Meter),
                        wxState.Weather);*/
            
            Log.Verbose("{Metar}", wxState.ToMETAR("KPAX"));

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
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.SnowfallSinceMidnight,
                               n => n.Millimeters,
                               "SnowSinceMidnight");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.Visibility,
                               n => n.Meters,
                               "Visibility");
            AddIfNotDuplicated(records, lastState, wxState,
                               n => n.Weather,
                               n => n.GetEnumMemberValue(),
                               "Weather");

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
                        Log.Information("Records ingested: {RecordsIngest} in request ID {RequestId}",
                                        response.RecordsIngested.Total, response.ResponseMetadata.RequestId);
                        break;
                    default:
                        Log.Warning(
                            "Failed to ingest Timestream records.  Status code: {StatusCode} in request ID {RequestId}",
                            response.HttpStatusCode, response.ResponseMetadata.RequestId);
                        break;
                }
            }
            catch (RejectedRecordsException)
            {
                // Ignore it
            }
        }

        private static void AddIfNotDuplicated<T, U>(ICollection<Record> list, WeatherState last, WeatherState current, Func<WeatherState, T> unitSelector, Func<T, U> valueSelector, string name, MeasureValueType mvt = null)
            where T : struct
        {
            T lastMeasurement = unitSelector(last);
            T currentMeasurement = unitSelector(current);

            U lastValue = valueSelector(lastMeasurement);
            U currentValue = valueSelector(currentMeasurement);

            if (lastValue.Equals(currentValue))
                return;
            
            list.Add(new Record
            {
                MeasureName = name,
                MeasureValueType = mvt ?? MeasureValueType.DOUBLE,
                MeasureValue = currentValue.ToString()
            });
        }
        
        /// <inheritdoc />
        public async Task<bool> Stop()
        {
            foreach (IAsyncWeatherDataSource wxds in _wxDataSources)
                await Retry.DoAsync(() => wxds.Stop(), TimeSpan.FromSeconds(1), 5);

            return _endSignaller?.Set() ?? true;
        }
    }
}

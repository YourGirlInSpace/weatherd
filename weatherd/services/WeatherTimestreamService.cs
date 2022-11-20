using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
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

            Extrapolate(ref wxState);
            
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

        internal static void Extrapolate(ref WeatherState wxState)
        {
            /*
             * We only want to extrapolate weather conditions, and thus we
             * need a minimum of four pieces of information:
             *   - Generalized weather code from precipitation discriminator
             *   - Visibility
             *   - Dewpoint
             *   - Temperature
             *
             * We will use this table as a guideline:
             *
             * | WX CODE                      | VISIBILITY        | DEWPOINT DEPR.       | TEMPERATURE         | OUTPUT           |
             * |------------------------------|-------------------|----------------------|---------------------|------------------|
             * | RA, DZ                       |                   |                      | T > 0°C             | RA or DZ         |
             * | RA, DZ                       |                   |                      | -10°C < T < 0°C     | FZRA or FZDZ     |
             * | RA, DZ, FZRA, FZDZ, SN, RASN |                   |                      | T < -10°C           | SN               |
             * | FZRA, FZDZ, SN, RASN, PL     |                   |                      | T > 6°C             | RA or DZ         |
             * | HZ, FG, BR, CLR              | Vis < 2 km        | DD > 2°C             | N/A                 | HZ               |
             * | HZ, FG, BR, CLR              | Vis > 1 km        | DD < 2°C             | T > 0°C             | BR               |
             * | HZ, FG, BR, CLR              | Vis < 1 km        | DD < 2°C             | T > 0°C             | FG               |
             * | HZ, FG, BR, CLR              | Vis < 1 km        | DD < 2°C             | T < 0°C             | FZFG             |
             * |------------------------------|-------------------|----------------------|---------------------|------------------|
             *
             * Notes:
             *   - Empty cells are not considered in the calculation.
             *   - The above table tracks closely with ASOS algorithms, but omits a freezing rain sensor due to
             *     the prohibitive cost of these sensors.
             *   - It is presumed that rain or drizzle occurring below 0°C will be freezing rain or freezing drizzle.
             *   - While it could be argued that we could include blowing snow in this algorithm, blowing snow is
             *     entirely dependent on the composition of the snow, the angle of the winds and how easily lofted the
             *     top layer of snow is.  As a result, we will not include blowing snow in our calculations.
            */

            if (wxState.Weather is null
                || wxState.Visibility == default
                || wxState.Dewpoint == default
                || wxState.Temperature == default)
                return;

            WeatherCondition condition = wxState.Weather;

            double t = wxState.Temperature.DegreesCelsius;
            double dd = wxState.DewpointDepression.DegreesCelsius;
            double vis = wxState.Visibility.Meters;

            if (condition.Precipitation != Precipitation.None)
            {
                switch (t)
                {
                    case <= -10:
                        condition.Descriptor &= ~Descriptor.Freezing;
                        condition.Precipitation = condition.Precipitation switch
                        {
                            Precipitation.Rain => Precipitation.Snow,
                            Precipitation.Drizzle => Precipitation.Snow,
                            _ => condition.Precipitation
                        };
                        break;
                    case > -10 and < 0:
                        condition.Descriptor |= condition.Precipitation switch
                        {
                            Precipitation.Rain => Descriptor.Freezing,
                            Precipitation.Drizzle => Descriptor.Freezing,
                            _ => Descriptor.None
                        };
                        break;
                    case > 0 and < 6:
                        condition.Descriptor &= ~Descriptor.Freezing;
                        break;
                    case > 6:
                        condition.Precipitation = condition.Precipitation switch
                        {
                            Precipitation.Snow => Precipitation.Rain,
                            Precipitation.Sleet => Precipitation.Rain,
                            _ => condition.Precipitation
                        };
                        break;
                }
            } else if (condition.Obscuration != Obscuration.None)
            {
                condition.Obscuration = vis switch
                {
                    < 2000 when dd > 2 => Obscuration.Haze,
                    >= 1000 and < 2000 when dd < 2 => Obscuration.Mist,
                    < 1000 when dd < 2 => Obscuration.Fog,
                    _ => condition.Obscuration
                };

                if (condition.Obscuration == Obscuration.Fog && t < 0)
                    condition.Descriptor |= Descriptor.Freezing;
            }

            wxState.Weather = condition;
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

            if (wxState.SnowfallSinceMidnight != default)
                AddIfNotDuplicated(records, lastState, wxState,
                                   n => n.SnowfallSinceMidnight,
                                   n => n.Millimeters,
                                   "SnowSinceMidnight");
            
            records.Add(new Record
            {
                MeasureName = "Visibility",
                MeasureValueType = MeasureValueType.DOUBLE,
                MeasureValue = wxState.Visibility.Meters.ToString(CultureInfo.InvariantCulture)
            });
            records.Add(new Record
            {
                MeasureName = "Weather",
                MeasureValueType = MeasureValueType.VARCHAR,
                MeasureValue = wxState.Weather.ToString()
            });

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
            if (current is null)
                throw new ArgumentNullException(nameof(current));
            
            T currentMeasurement = unitSelector(current);
            U currentValue = valueSelector(currentMeasurement);

            if (last is not null)
            {
                T lastMeasurement = unitSelector(last);

                U lastValue = valueSelector(lastMeasurement);

                if (lastValue.Equals(currentValue))
                    return;
            }
            
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

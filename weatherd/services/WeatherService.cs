using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using UnitsNet.Units;
using weatherd.datasources;

namespace weatherd.services
{
    public interface IWeatherService
    {
        bool IsRunning { get; }
        Task<bool> Initialize(params IAsyncWeatherDataSource[] dataSource);
        Task<bool> Start(AutoResetEvent endSignaller);
        Task<bool> Stop();
    }

    public class WeatherService : IWeatherService
    {
        private readonly IInfluxService _influxService;
        private readonly ICWOPService _cwopService;
        private IAsyncWeatherDataSource[] _wxDataSources;
        private WeatherState lastState;
        private AutoResetEvent _endSignaller;
        private bool _enableCorrectness;
        private float _elevation;
        private float _anemometerOrientation;
        private DateTime _lastCwopSendTime = DateTime.MinValue;

        /// <inheritdoc />
        public bool IsRunning => _wxDataSources.All(x => x.Running);

        public WeatherService(IConfiguration config, IInfluxService influxService, ICWOPService cwopService)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _influxService = influxService ?? throw new ArgumentNullException(nameof(influxService));
            _cwopService = cwopService ?? throw new ArgumentNullException(nameof(cwopService));

            LoadConfig(config);
        }

        private void LoadConfig(IConfiguration config)
        {
            IConfigurationSection wxConfig = config.GetSection("WeatherService");
            IConfigurationSection siteConfig = config.GetSection("Site");

            if (wxConfig is null)
                throw new StationConfigurationException(
                    $"Failed to load configuration for {nameof(WeatherService)}:  'WeatherService' is missing.");
            if (siteConfig is null)
                throw new StationConfigurationException(
                    $"Failed to load configuration for {nameof(WeatherService)}:  'Site' is missing.");

            _enableCorrectness = wxConfig.GetValue("EnableCorrectness", true);
            _elevation = siteConfig.GetValue("Elevation", 0);
            _anemometerOrientation = wxConfig.GetValue("AnemometerOrientation", 0);
        }

        /// <inheritdoc />
        public async Task<bool> Initialize(params IAsyncWeatherDataSource[] dataSource)
        {
            _wxDataSources = dataSource;
            if (_wxDataSources.All(n => n.Initialized))
                return true;

            foreach (IAsyncWeatherDataSource wxds in _wxDataSources)
                if (!await wxds.Initialize())
                    throw new InvalidOperationException("Could not initialize data source.");

            if (!_influxService.Initialized)
                return await _influxService.Initialize();

            return true;
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
            wxState.Elevation = new Length(_elevation, LengthUnit.Meter);
            
            for (int i = 1; i < _wxDataSources.Length; i++)
                wxState = WeatherState.Merge(wxState, _wxDataSources[i].Conditions);

            if (wxState is null)
            {
                Log.Warning("Could not retrieve valid sample from data sources");
                return;
            }

            if (lastState != null && lastState.Time == wxState.Time)
            {
                Log.Debug("Sample retrieved from weather data source did not update at polling interval");
                return;
            }
            
            // Update the anemometer orientation
            wxState.WindDirection += Angle.FromDegrees(_anemometerOrientation);

            if (_enableCorrectness)
                EnforceCorrectness(ref wxState);

            if (DateTime.UtcNow - _lastCwopSendTime > TimeSpan.FromMinutes(2))
            {
                try
                {
                    _lastCwopSendTime = DateTime.UtcNow;    
                    await _cwopService.SendCWOP(wxState);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to send CWOP data stream");
                    // ignore;  CWOP is an auxiliary data stream.
                }
            }

            try
            {
                await _influxService.Push(wxState);
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to ingest logs into Influx");
                return;
            }
            
            lastState = wxState;
        }

        internal static void EnforceCorrectness(ref WeatherState wxState)
        {
            /*
             * We only want to correct weather conditions, and thus we
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
             * | FZRA, FZDZ, SN, RASN, PL     |                   |                      | T > 10°C            | RA or DZ         |
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
             *   - Snow has been observed in conditions above 10°C, but it is extremely rare.  We will ignore this
             *     possibility and enforce that all precipitation above 10°C must be liquidous.
             *   - In the future, a PM2.5 sensor may be incorporated to differentiate between haze and smoke.
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
                    case > 0 and < 10:
                        condition.Descriptor &= ~Descriptor.Freezing;
                        break;
                    case > 10:
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

        /// <inheritdoc />
        public async Task<bool> Stop()
        {
            foreach (IAsyncWeatherDataSource wxds in _wxDataSources)
                await Retry.DoAsync(() => wxds.Stop(), TimeSpan.FromSeconds(1), 5);

            return _endSignaller?.Set() ?? true;
        }
    }
}

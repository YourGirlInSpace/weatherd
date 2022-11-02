#if DEBUG
using System;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using UnitsNet.Units;

namespace weatherd.datasources.testdatasource
{
    public interface ITestDataSource : IAsyncWeatherDataSource
    {
    }

    public class TestDataSource : ITestDataSource
    {
        private const int DefaultPollingInterval = 1;
        private const int DefaultSampleInterval = 1;

        private static readonly DiurnalParameter DefaultTemperatureDiurnalParameters = new(15, 30, 4, 24, 1);
        private static readonly DiurnalParameter DefaultDewpointDiurnalParameters = new(10, 25, 6, 24, 1);
        private static readonly DiurnalParameter DefaultPressureDiurnalParameters = new(1013.25f, 1014.65f, 4, 12, 1);

        public bool Enabled { get; private set; }

        public TestDataSource(IConfiguration config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc />
        public string Name => "Test Data Source";

        public int PollingInterval { get; private set; }

        /// <inheritdoc />
        public bool Initialized { get; private set; }

        /// <inheritdoc />
        public bool Running { get; private set; }

        /// <inheritdoc />
        public WeatherState Conditions { get; set; }

        /// <inheritdoc />
        public event EventHandler<WeatherDataEventArgs> SampleAvailable;

        /// <inheritdoc />
        public async Task<bool> Initialize()
        {
            IConfigurationSection section = Configuration.GetSection(nameof(TestDataSource));
            if (section == null)
                return true;

            Enabled = section["Enabled"].Equals("true", StringComparison.InvariantCultureIgnoreCase);
            if (!Enabled)
                return false;

            if (!int.TryParse(section["PollingInterval"], out int pollingInterval))
                pollingInterval = DefaultPollingInterval;

            PollingInterval = pollingInterval;

            if (!int.TryParse(section["SampleInterval"], out sampleInterval))
                sampleInterval = DefaultSampleInterval;

            IConfigurationSection syntheticsSection = section.GetSection("Synthetics");
            if (syntheticsSection != null)
            {
                DiurnalTemperature =
                    new DiurnalParameter(syntheticsSection.GetSection("Temperature"),
                                         DefaultTemperatureDiurnalParameters);
                DiurnalDewpoint =
                    new DiurnalParameter(syntheticsSection.GetSection("Dewpoint"), DefaultDewpointDiurnalParameters);
                DiurnalPressure =
                    new DiurnalParameter(syntheticsSection.GetSection("Pressure"), DefaultPressureDiurnalParameters);
                _syntheticWind = new SyntheticWind(syntheticsSection.GetSection("Wind"));
            }

            if (!float.TryParse(section["Latitude"], out float latitude))
                latitude = 0;
            if (!float.TryParse(section["Longitude"], out float longitude))
                longitude = 0;

            _meyersDaleSolarRadModel = new MeyersDaleSolarRadiationModel(latitude, longitude);

            Log.Information(
                "{dataSource} diurnal temperature variation simulation parameters:  Min={minTemp}°C, Max={maxTemp}°C, Period={period} hours, Trough={lowPoint}, Deviation={deviation}",
                Name,
                DiurnalTemperature.Minimum,
                DiurnalTemperature.Maximum,
                DiurnalTemperature.Period,
                DiurnalTemperature.Trough,
                DiurnalTemperature.Deviation);
            Log.Information(
                "{dataSource} diurnal dewpoint variation simulation parameters:  Min={minDewpoint}°C, Max={maxDewpoint}°C, Period={period} hours, Trough={lowPoint}, Deviation={deviation}",
                Name,
                DiurnalDewpoint.Minimum,
                DiurnalDewpoint.Maximum,
                DiurnalDewpoint.Period,
                DiurnalDewpoint.Trough,
                DiurnalDewpoint.Deviation);
            Log.Information(
                "{dataSource} diurnal pressure variation simulation parameters:  Min={minPressure} hPa, Max={maxPressure} hPa, Period={period} hours, Trough={lowPoint}, Deviation={deviation}",
                Name,
                DiurnalPressure.Minimum,
                DiurnalPressure.Maximum,
                DiurnalPressure.Period,
                DiurnalPressure.Trough,
                DiurnalPressure.Deviation);

            Log.Information(
                "{dataSource} initialized with polling interval of {pollingInterval} seconds and sample interval of {sampleInterval} seconds",
                Name,
                PollingInterval,
                sampleInterval);

            Initialized = true;
            return Initialized;
        }

        /// <inheritdoc />
        public async Task<bool> Start()
        {
            if (!Enabled)
            {
                Log.Information("{dataSource} could not start because the data source is disabled in configuration.",
                                Name);
                return false;
            }

            if (!Initialized)
            {
                Log.Error("Failed to start {dataSource}: Initialize not called before Start!", Name);
                return false;
            }

            timer = new Timer(sampleInterval * 1000);
            timer.Elapsed += (_, _) => Sample();
            timer.Start();

            Log.Verbose("{dataSource} timer enabled: {timerEnabled}", Name, timer.Enabled);
            if (timer.Enabled)
                Log.Information("{dataSource} started!", Name);
            else
                Log.Warning("{dataSource} failed to start: timer could not be enabled.", Name);

            Running = timer.Enabled;
            return Running;
        }

        /// <inheritdoc />
        public async Task<bool> Stop()
        {
            timer.Enabled = false;
            Running = timer.Enabled;
            return !timer.Enabled;
        }

        private void Sample()
        {
            DateTime sampleTime = DateTime.Now;

            float simTemp = DiurnalTemperature.Sample(sampleTime);
            float simDewpoint = DiurnalDewpoint.Sample(sampleTime);
            float simPressure = DiurnalPressure.Sample(sampleTime);
            Irradiance simSolarRad = _meyersDaleSolarRadModel.CalculateSolarRadiation(sampleTime);

            /* Dewpoint should never really exceed the ambient air temperature.
             * While this *is* possible in real life (ref: supersaturation), we
             * don't want to simulate it here.
             */
            if (simDewpoint > simTemp)
                simDewpoint = simTemp;

            float windSpeed = _syntheticWind.WindSpeed();
            float windDir = _syntheticWind.WindDirection();

            Conditions = new WeatherState
            {
                Time = sampleTime,
                Temperature = new Temperature(simTemp, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(simDewpoint, TemperatureUnit.DegreeCelsius),
                Pressure = new Pressure(simPressure, PressureUnit.Hectopascal),
                Elevation = new Length(0, LengthUnit.Meter),
                Luminosity = simSolarRad,
                WindDirection = new Angle(windDir, AngleUnit.Degree),
                WindSpeed = new Speed(windSpeed, SpeedUnit.MeterPerSecond),
                RainfallLastHour = Length.Zero,
                RainfallLast24Hours = Length.Zero,
                RainfallSinceMidnight = Length.Zero
            };

            SampleAvailable?.Invoke(this, new WeatherDataEventArgs(Conditions));
        }

        private readonly IConfiguration Configuration;

        private MeyersDaleSolarRadiationModel _meyersDaleSolarRadModel;
        private SyntheticWind _syntheticWind;
        private DiurnalParameter DiurnalDewpoint;
        private DiurnalParameter DiurnalPressure;

        private DiurnalParameter DiurnalTemperature;
        private int sampleInterval;
        private Timer timer;
    }
}
#endif

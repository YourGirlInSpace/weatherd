#if DEBUG
using System;
using System.Globalization;
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
                "{DataSource} diurnal temperature variation simulation parameters:  Min={MinTemp}°C, Max={MaxTemp}°C, Period={Period} hours, Trough={LowPoint}, Deviation={Deviation}",
                Name,
                DiurnalTemperature.Minimum.ToString(CultureInfo.CurrentCulture),
                DiurnalTemperature.Maximum.ToString(CultureInfo.CurrentCulture),
                DiurnalTemperature.Period.ToString(CultureInfo.CurrentCulture),
                DiurnalTemperature.Trough.ToString(CultureInfo.CurrentCulture),
                DiurnalTemperature.Deviation.ToString(CultureInfo.CurrentCulture));
            Log.Information(
                "{DataSource} diurnal dewpoint variation simulation parameters:  Min={MinDewpoint}°C, Max={MaxDewpoint}°C, Period={Period} hours, Trough={LowPoint}, Deviation={Deviation}",
                Name,
                DiurnalDewpoint.Minimum.ToString(CultureInfo.CurrentCulture),
                DiurnalDewpoint.Maximum.ToString(CultureInfo.CurrentCulture),
                DiurnalDewpoint.Period.ToString(CultureInfo.CurrentCulture),
                DiurnalDewpoint.Trough.ToString(CultureInfo.CurrentCulture),
                DiurnalDewpoint.Deviation.ToString(CultureInfo.CurrentCulture));
            Log.Information(
                "{DataSource} diurnal pressure variation simulation parameters:  Min={MinPressure} hPa, Max={MaxPressure} hPa, Period={Period} hours, Trough={LowPoint}, Deviation={Deviation}",
                Name,
                DiurnalPressure.Minimum.ToString(CultureInfo.CurrentCulture),
                DiurnalPressure.Maximum.ToString(CultureInfo.CurrentCulture),
                DiurnalPressure.Period.ToString(CultureInfo.CurrentCulture),
                DiurnalPressure.Trough.ToString(CultureInfo.CurrentCulture),
                DiurnalPressure.Deviation.ToString(CultureInfo.CurrentCulture));

            Log.Information(
                "{DataSource} initialized with polling interval of {PollingInterval} seconds and sample interval of {SampleInterval} seconds",
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
                Log.Information("{DataSource} could not start because the data source is disabled in configuration",
                                Name);
                return false;
            }

            if (!Initialized)
            {
                Log.Error("Failed to start {DataSource}: Initialize not called before Start!", Name);
                return false;
            }

            timer = new Timer(sampleInterval * 1000);
            timer.Elapsed += (_, _) => Sample();
            timer.Start();

            Log.Verbose("{DataSource} timer enabled: {TimerEnabled}", Name, timer.Enabled);
            if (timer.Enabled)
                Log.Information("{DataSource} started!", Name);
            else
                Log.Warning("{DataSource} failed to start: timer could not be enabled", Name);

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
                RainfallSinceMidnight = Length.Zero,
                Weather = WeatherCondition.FromWMOCode(WMOCodeTable.Clear),
                WeatherLast15Minutes = WeatherCondition.FromWMOCode(WMOCodeTable.Clear),
                WeatherLastHour = WeatherCondition.FromWMOCode(WMOCodeTable.Clear),
                Visibility = new Length(2000, LengthUnit.Meter),
                BatteryVoltage = new ElectricPotentialDc(13.8, ElectricPotentialDcUnit.VoltDc),
                BatteryChargeCurrent = new ElectricCurrent(482, ElectricCurrentUnit.Milliampere),
                BatteryDrainCurrent = new ElectricCurrent(420, ElectricCurrentUnit.Milliampere)
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

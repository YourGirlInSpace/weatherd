using System;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using UnitsNet.Units;
using weatherd.datasources.Vaisala.Messages;

namespace weatherd.datasources.Vaisala
{
    public class PWD12DataSource : IVaisalaDataSource
    {
        public IConfiguration Configuration { get; }

        public PWD12DataSource(IConfiguration config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));

            IConfigurationSection section = Configuration.GetRequiredSection(nameof(PWD12DataSource));
            IConfigurationSection portSection = section.GetRequiredSection("Port");
            string portName = portSection.GetValue("Name", "/dev/ttyUSB1");
            int baud = portSection.GetValue("Baud", 9600);
            string sensorId = portSection.GetValue("SensorID", "7");

            _connection = new VaisalaConnection(portName, baud, sensorId);
        }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public int PollingInterval => 15;

        /// <inheritdoc />
        public bool Initialized { get; set; }

        /// <inheritdoc />
        public bool Running { get; set; }

        /// <inheritdoc />
        public Task<bool> Initialize()
        {
            Initialized = true;
            return Task.FromResult(Initialized);
        }

        /// <inheritdoc />
        public Task<bool> Start() => Task.FromResult(_connection.Start(DataCallback, CompletionCallback));

        /// <inheritdoc />
        public Task<bool> Stop()
            => Task.FromResult(_connection.Stop());

        /// <inheritdoc />
        public WeatherState Conditions { get; set; }

        /// <inheritdoc />
        public event EventHandler<WeatherDataEventArgs> SampleAvailable;

        private void CompletionCallback(bool obj)
        {
            /* The run has stopped for some reason.
             * Whatever the reason, and whether or
             * not the run was successful, we will
             * treat this as a failure state.
             */

            Running = false;
            Log.Fatal("Unexpected termination of Vaisala connection");
        }

        private void DataCallback(VaisalaMessage obj)
        {
            if (obj is not VaisalaAviationMessage avMsg)
                return;

            if (!avMsg.OneMinuteAverageVisibility.HasValue &&
                !avMsg.InstantaneousWeather.HasValue &&
                !avMsg.OneMinuteWaterIntensity.HasValue)
                return;

            Conditions = new WeatherState
            {
                Time = DateTime.UtcNow
            };

            if (avMsg.TenMinuteAverageVisibility.HasValue)
                Conditions.Visibility = new Length(avMsg.TenMinuteAverageVisibility.Value, LengthUnit.Meter);
            else if (avMsg.OneMinuteAverageVisibility.HasValue)
                Conditions.Visibility = new Length(avMsg.OneMinuteAverageVisibility.Value, LengthUnit.Meter);

            if (avMsg.OneMinuteWaterIntensity.HasValue)
                Conditions.WaterIntensity = new Speed(avMsg.OneMinuteWaterIntensity.Value, SpeedUnit.MillimeterPerHour);
            if (avMsg.CumulativeSnow.HasValue)
                Conditions.SnowfallSinceMidnight = new Length(avMsg.CumulativeSnow.Value, LengthUnit.Millimeter);

            bool isDRDError = false;
            if (avMsg.HardwareAlarm == HardwareAlarm.HardwareWarning)
            {
                // This may just be the RainCap acting up.  Verify this.
                VaisalaStationStatusMessage statusMessage =
                    AsyncHelpers.RunSync(() => _connection.SendStationStatusCommand());

                if (statusMessage is not null)
                    isDRDError = statusMessage.Warnings.Length == 1 && statusMessage.Warnings[0] == "DRD ERROR";
            }


            WMOCodeTable weather = WMOCodeTable.Unknown;
            if (!avMsg.InstantaneousWeather.HasValue && avMsg.NWSWeatherCode.HasValue)
            {
                // Sometimes the WMO code is unavailable.  We can generalize using the
                // NWS code table.
                weather = avMsg.NWSWeatherCode.Value switch
                {
                    "C" => WMOCodeTable.Clear,
                    "P" => WMOCodeTable.Precipitation,
                    "-P" => WMOCodeTable.Precipitation,
                    "+P" => WMOCodeTable.PrecipitationHeavy,
                    "-L" => WMOCodeTable.DrizzleSlight,
                    "L" => WMOCodeTable.Drizzle,
                    "+L" => WMOCodeTable.DrizzleHeavy,
                    "-R" => WMOCodeTable.RainLight,
                    "R" => WMOCodeTable.Rain,
                    "+R" => WMOCodeTable.RainHeavy,
                    "-S" => WMOCodeTable.SnowLight,
                    "S" => WMOCodeTable.Snow,
                    "+S" => WMOCodeTable.SnowHeavy,
                    "-IP" => WMOCodeTable.SleetLight,
                    "IP" => WMOCodeTable.SleetModerate,
                    "+IP" => WMOCodeTable.SleetHeavy,
                    _ => weather
                };
            } else
                weather = avMsg.InstantaneousWeather.Value;
            
            if (isDRDError && weather == WMOCodeTable.Unknown)
            {
                // Let's try and make a best guess.  We'll presume that the RainCap is OOS.
                // Since the RainCap functions normally during wet conditions, we will assume
                // that there is no precipitation.

                WMOCodeTable bestGuess = Conditions.Visibility.Value switch
                {
                    >= 2000 => WMOCodeTable.Clear,
                    < 2000 and >= 1000 => WMOCodeTable.HazeVisGreaterThan1Km,
                    < 1000 => WMOCodeTable.HazeVisLessThan1Km,
                    _ => WMOCodeTable.Unknown
                };

                weather = bestGuess;
            }

            WeatherCondition condition = WeatherCondition.FromWMOCode(weather);

            // We can make some inferences if we have temperature data.
            // By default, the PWD12 does not express this information.
            // However, we can presume that fog will become freezing fog
            // and drizzle will become freezing drizzle between -10°C and
            // 0°C.
            if (avMsg.Temperature.HasValue)
            {
                float temp = avMsg.Temperature.Value;
                if (temp is >= -10 and <= 0)
                {
                    if (condition.Precipitation == Precipitation.None && condition.Obscuration == Obscuration.Fog
                        || condition.Precipitation is Precipitation.Rain or Precipitation.Rain)
                        condition.Descriptor |= Descriptor.Freezing;
                }
            }
            
            Log.Information("Visibility: {Visibility}\tWeather: {Weather} (METAR value {MetarCode}) {InAlarm}",
                            $"{(Conditions.Visibility.Value >= 2000 ? ">" : "")}{Conditions.Visibility}",
                            weather, weather == WMOCodeTable.Clear ? "CLR" : condition.ToString() ?? "?",
                            avMsg.HardwareAlarm == HardwareAlarm.None ? "" : "*");

            Conditions.Weather = condition;
            SampleAvailable?.Invoke(this, new WeatherDataEventArgs(Conditions));

            if (_lastPacketTime.DayOfYear != DateTime.Now.DayOfYear)
                if (!Retry.Do(() => AsyncHelpers.RunSync(() => _connection.SendResetTotalsCommand())))
                    Log.Warning("Could not reset precipitation totals on PWD12!");

            _lastPacketTime = DateTime.UtcNow;
        }

        private readonly VaisalaConnection _connection;

        private DateTime _lastPacketTime = DateTime.Now;
    }
}

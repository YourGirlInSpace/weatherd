using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using UnitsNet.Units;

namespace weatherd.datasources.pakbus
{
    public interface IPakbusDataSource : IAsyncWeatherDataSource
    {
    }

    public class PakbusDataSource : IPakbusDataSource
    {
        public IConfiguration Configuration { get; }

        public PakbusDataSource(IConfiguration config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));

            IConfigurationSection section = Configuration.GetRequiredSection(nameof(PakbusDataSource));
            int nodeId       = section.GetValue("NodeID", 4096);
            int targetNode   = section.GetValue("TargetNode", 1);
            int securityCode = section.GetValue("SecurityCode", 0);

            IConfigurationSection siteSection = Configuration.GetSection("Site");
            _elevation       = siteSection?.GetValue("Elevation", 0f) ?? 0;

            IConfigurationSection portSection = section.GetRequiredSection("Port");
            string portName  = portSection.GetValue("Name", "/dev/ttyUSB0");
            int baud         = portSection.GetValue("Baud", 9600);

            _connection = new PakbusConnection(portName, baud, nodeId, targetNode, securityCode);
        }

        /// <inheritdoc />
        public Task<bool> Initialize()
        {
            // No initialization needed
            Initialized = true;
            return Task.FromResult(Initialized);
        }

        public Task<bool> Start()
            => Task.FromResult(_connection.Start(DataCallback, CompletionCallback));

        /// <inheritdoc />
        public Task<bool> Stop()
            => Task.FromResult(_connection.Stop());

        /// <inheritdoc />
        public WeatherState Conditions { get; set; }

        /// <inheritdoc />
        public event EventHandler<WeatherDataEventArgs> SampleAvailable;

        /// <inheritdoc />
        public string Name => "Pakbus Data Source";

        /// <inheritdoc />
        public int PollingInterval => 1;

        /// <inheritdoc />
        public bool Initialized { get; private set; }

        /// <inheritdoc />
        public bool Running { get; private set; }

        private void CompletionCallback(bool runSuccessful)
        {
            /* The run has stopped for some reason.
             * Whatever the reason, and whether or
             * not the run was successful, we will
             * treat this as a failure state.
             */

            Running = false;
            Log.Fatal("Unexpected termination of Pakbus connection");
        }

        private void DataCallback(PakbusResult data)
        {
            long recTime = data.Get<long>("RECTIME");
            DateTime dt = DateTime.UnixEpoch.AddSeconds(recTime);
            
            Conditions = new WeatherState
            {
                Time                  = dt,
                Elevation             = new Length(_elevation, LengthUnit.Meter),

                Temperature           = new Temperature(data.Get<float>("AirTC"), TemperatureUnit.DegreeCelsius),
                RelativeHumidity      = new RelativeHumidity(data.Get<float>("RH"), RelativeHumidityUnit.Percent),
                Pressure              = new Pressure(data.Get<float>("BPrs_hPa"), PressureUnit.Hectopascal),
                WindDirection         = new Angle(data.Get<float>("WDir_deg"), AngleUnit.Degree),
                WindSpeed             = new Speed(data.Get<float>("WSpd_mph"), SpeedUnit.MilePerHour),
                Luminosity            = new Irradiance(data.Get<float>("SlrW"), IrradianceUnit.WattPerSquareMeter),
                RainfallSinceMidnight = new Length(data.Get<float>("Rain24"), LengthUnit.Millimeter)
            };

            Log.Verbose("Invoking SampleAvailable..");
            SampleAvailable?.Invoke(this, new WeatherDataEventArgs(Conditions));
        }

        private readonly PakbusConnection _connection;

        private readonly float _elevation;
    }
}

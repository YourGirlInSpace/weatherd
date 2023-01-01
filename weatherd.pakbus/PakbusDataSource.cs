using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using UnitsNet.Units;

namespace weatherd.datasources.pakbus
{
    public interface IPakbusDataSource : IAsyncWeatherDataSource
    {
        void EmitPakbusInformation();
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
                RainfallSinceMidnight = new Length(data.Get<float>("Rain24"), LengthUnit.Millimeter),

                BatteryVoltage        = new ElectricPotentialDc(data.Get<float>("BattV"), ElectricPotentialDcUnit.VoltDc),
                BatteryChargeCurrent  = new ElectricCurrent(data.Get<float>("ChgI"), ElectricCurrentUnit.Milliampere),
                BatteryDrainCurrent   = new ElectricCurrent(data.Get<float>("BattI"), ElectricCurrentUnit.Milliampere)
            };

            Log.Verbose("Invoking SampleAvailable..");
            SampleAvailable?.Invoke(this, new WeatherDataEventArgs(Conditions));
        }

        public void EmitPakbusInformation()
        {
            AutoResetEvent are = new(false);
            PakbusResult result = null;
            _connection.Start(r =>
            {
                result = r;
                are.Set();
            }, b => { });

            are.WaitOne();

            // We should now be connected with a valid table definition.
            // The following data, we just want to spit out to the console itself.

            Console.WriteLine("PAKBUS CONNECTION ESTABLISHED");
            Console.WriteLine($"RS232 Port:       {_connection.PortName}");
            Console.WriteLine($"RS232 Baud:       {_connection.Baud}");
            Console.WriteLine("RS232 Data Bits:  8");
            Console.WriteLine("RS232 Parity:     None");
            Console.WriteLine("RS232 Stop Bits:  1");
            Console.WriteLine("");
            Console.WriteLine($"Local Node ID:    {_connection.LocalNodeID}");
            Console.WriteLine($"Remote Node ID:   {_connection.RemoteNodeID}");
            Console.WriteLine($"Security Code:    {_connection.SecurityCode}");
            Console.WriteLine("");
            Console.WriteLine("InLocs Data:");

            Table inlocs = XTDTableDefinition.Current["Inlocs"];
            foreach (Field field in inlocs.Fields)
            {
                StringBuilder lineBuilder = new();
                lineBuilder.Append($"{field.Name}:".PadRight(18));

                switch (field.Type)
                {
                    case PakbusDatumType.ASCII:
                    case PakbusDatumType.ASCIIZ:
                        lineBuilder.Append(result.Get<string>(field.Name));
                        break;
                    case PakbusDatumType.FP2:
                        lineBuilder.Append("<FP2>");
                        break;
                    case PakbusDatumType.FP3:
                        lineBuilder.Append("<FP3>");
                        break;
                    case PakbusDatumType.FP4:
                    case PakbusDatumType.IEEE4B:
                    case PakbusDatumType.IEEE4L:
                        lineBuilder.Append(result.Get<float>(field.Name));
                        break;
                    case PakbusDatumType.IEEE8B:
                    case PakbusDatumType.IEEE8L:
                        lineBuilder.Append(result.Get<double>(field.Name));
                        break;
                    case PakbusDatumType.Bool:
                    case PakbusDatumType.Bool2:
                    case PakbusDatumType.Bool4:
                    case PakbusDatumType.Bool8:
                    case PakbusDatumType.Int1:
                    case PakbusDatumType.Byte:
                        lineBuilder.Append($"{result.Get<byte>(field.Name):X}");
                        break;
                    case PakbusDatumType.Short:
                    case PakbusDatumType.Int2:
                        lineBuilder.Append(result.Get<short>(field.Name));
                        break;
                    case PakbusDatumType.Int4:
                        lineBuilder.Append(result.Get<int>(field.Name));
                        break;
                    case PakbusDatumType.UShort:
                    case PakbusDatumType.UInt2:
                        lineBuilder.Append(result.Get<ushort>(field.Name));
                        break;
                    case PakbusDatumType.UInt4:
                        lineBuilder.Append(result.Get<uint>(field.Name));
                        break;
                    case PakbusDatumType.Long:
                        lineBuilder.Append(result.Get<long>(field.Name));
                        break;
                    case PakbusDatumType.ULong:
                        lineBuilder.Append(result.Get<ulong>(field.Name));
                        break;
                    case PakbusDatumType.Sec:
                    case PakbusDatumType.USec:
                    case PakbusDatumType.NSec:
                    case PakbusDatumType.SecNano:
                        lineBuilder.Append(result.Get<NSec>(field.Name).ToTime());
                        break;
                    default:
                        lineBuilder.Append($"<unhandled type:{field.Type}>");
                        break;
                }

                Console.WriteLine(lineBuilder.ToString());
            }

            Console.WriteLine("");

            float battCharge = Math.Min(0.758534f * result.Get<float>("BattV") - 8.78859f, 1f);
            float batteryDrawHrs = 8.0f / (result.Get<float>("BattI")/1000f);
            Console.WriteLine($"The battery is {battCharge:P} charged.");
            Console.WriteLine($"At current drain, the battery will last {batteryDrawHrs} hours.");
            Console.WriteLine("");

            float battChargeI12V = (120 * (result.Get<float>("ChgI") / 1000.0f)) / 12f;
            
            string sChargeCurrent = $"{battChargeI12V * 1000.0f:F1}mA";
            string sDrainCurrent = $"{result.Get<float>("BattI"):F1}mA";

            StringBuilder sb = new();
            sb.Append("┌");
            sb.Append(string.Empty.PadRight(sChargeCurrent.Length + 2, '─'));
            sb.Append("┐");
            sb.Append(string.Empty.PadRight(5));
            sb.Append("┌");
            sb.Append(string.Empty.PadRight("Battery".Length + 2, '─'));
            sb.Append("┐");
            sb.Append(string.Empty.PadRight(5));
            sb.Append("┌");
            sb.Append(string.Empty.PadRight(sDrainCurrent.Length + 2, '─'));
            sb.AppendLine("┐");

            
            sb.Append("│ ");
            sb.Append(sChargeCurrent);
            sb.Append(" │");
            sb.Append(" ──> ");
            sb.Append("│ Battery │");
            sb.Append(" ──> ");
            sb.Append("│ ");
            sb.Append(sDrainCurrent);
            sb.AppendLine(" │");

            sb.Append("└");
            sb.Append(string.Empty.PadRight(sChargeCurrent.Length + 2, '─'));
            sb.Append("┘");
            sb.Append(string.Empty.PadRight(5));
            sb.Append("└");
            sb.Append(string.Empty.PadRight("Battery".Length + 2, '─'));
            sb.Append("┘");
            sb.Append(string.Empty.PadRight(5));
            sb.Append("└");
            sb.Append(string.Empty.PadRight(sDrainCurrent.Length + 2, '─'));
            sb.AppendLine("┘");

            Console.WriteLine(sb.ToString());
        }

        private readonly PakbusConnection _connection;

        private readonly float _elevation;
    }
}

using System;
using System.Text;

namespace weatherd.aprs
{
    public class APRSISLoginMessage : APRSISMessage
    {
        private int _udpPort;
        public const string CWOPSendPasscode = "0";
        public const string ReceiveOnlyPasscode = "-1";

        public string User { get; }
        public string Passcode { get; }
        public string SoftwareName { get; set; }

        public string SoftwareVersion { get; set; }
        public bool EnableUDP { get; set; }

        public int UDPPort
        {
            get => _udpPort;
            set
            {
                if (value <= 0 || value >= ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), "UDP port must be between 1 and 65535.");
                _udpPort = value;
            }
        }

        public string ServerCommand { get; set; }

        /// <inheritdoc />
        public APRSISLoginMessage(string callsign)
            : base(string.Empty, string.Empty)
        {
            User = callsign;
            Passcode = APRSUtilities.GetPasscode(callsign);
        }

        /// <inheritdoc />
        public APRSISLoginMessage(string callsign, string passcode)
            : base(string.Empty, string.Empty)
        {
            User = callsign;
            Passcode = passcode;
        }
        
        /// <inheritdoc />
        public override string Compile()
        {
            StringBuilder commandBuilder = new StringBuilder();

            commandBuilder.Append($"user {User} ");
            commandBuilder.Append($"pass {(string.IsNullOrEmpty(Passcode) ? ReceiveOnlyPasscode : Passcode)}");

            if (!string.IsNullOrEmpty(SoftwareName))
            {
                commandBuilder.Append($" vers {SoftwareName}");
                commandBuilder.Append(!string.IsNullOrEmpty(SoftwareVersion) ? $" {SoftwareVersion}" : " 1.0");
            }

            if (EnableUDP)
            {
                if (UDPPort <= 0 || UDPPort >= ushort.MaxValue)
                    throw new InvalidOperationException($"UDP port must be between 1 and 65535.  Actual value was {UDPPort}.");

                commandBuilder.Append($" UDP {UDPPort}");
            }

            if (!string.IsNullOrEmpty(ServerCommand))
                commandBuilder.Append($" {ServerCommand}");

            return commandBuilder.ToString();
        }
    }
}

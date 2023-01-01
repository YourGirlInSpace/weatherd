using System;
using System.Text.RegularExpressions;

namespace weatherd.aprs.responses
{
    public class APRSISLoginResponse : IParsable
    {
        private static readonly Lazy<Regex> _respRegex =
            new Lazy<Regex>(() => new Regex(
                                @"#\slogresp\s(?<callsign>[A-Za-z0-9\-\\]+)\s(?<verificationStatus>verified|unverified),\sserver\s(?<server>.+)"));

        public bool IsValid { get; private set; }
        public string Callsign { get; private set; }
        public bool IsVerified { get; private set; }
        public string Server { get; private set; }

        /// <inheritdoc />
        public void Parse(string message)
        {
            Match match = _respRegex.Value.Match(message);

            if (!match.Success)
            {
                IsValid = false;
                return;
            }

            Callsign = match.Groups["callsign"].Value;
            IsVerified = match.Groups["verificationStatus"].Value
                              .Equals("verified", StringComparison.OrdinalIgnoreCase);
            Server = match.Groups["server"].Value;
            IsValid = true;
        }
    }
}

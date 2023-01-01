using System;
using Serilog;

namespace weatherd.datasources.Vaisala.Messages
{
    public abstract class VaisalaMessage
    {
        private const char SOH = '\x01';
        private const char STX = '\x02';
        private const char ETX = '\x03';
        private const string Header = "PW";

        private const int SOTPosition = 6;

        public VaisalaMessageType MessageType { get; protected set; }
        public string SensorID { get; private set; }

        public HardwareAlarm HardwareAlarm { get; protected set; }
        public VisibilityAlarm VisibilityAlarm { get; protected set; }

        protected VaisalaMessage(VaisalaMessageType type)
        {
            MessageType = type;
        }

        public static VaisalaMessage Parse(string message)
        {
            Log.Verbose("[PWD12]: {Message}", message.Trim(SOH, STX, ETX, '\r', '\n'));

            var span = message.Trim().AsSpan();
            
            // 9 = SOH + 'PW ' + ID(2) + SOT + Msg + EOT
            if (span.Length < 9)
                return null;
            if (span[0] != SOH)
                return null;
            
            if (span[^1] != ETX) // i.e. the last character should be EOT
                return null;

            // Position of SOT should be at index 6
            if (span[SOTPosition] != STX)
                return null;

            var header = span[1..SOTPosition];
            var payload = span[(SOTPosition+1)..^1];

            // First, process the header
            if (!header[..2].Equals(Header, StringComparison.Ordinal))
                return null;

            string sensorId = header[^2..].Trim().ToString();

            // Now things get a little hairy.  There is no built in
            // message code that we can use to discriminate messages
            // from a PWD12.  However, there are only 4 that we need
            // to worry about, since message codes 3-6 are not
            // relevant to the station.
            //
            // Since the message type is not encoded, we must discriminate
            // messages manually.  We can do this by the number of parts
            // in a given message:
            //   Message 0 = 3 parts
            //   Message 1 = 4 parts
            //   Message 2 = 10 parts
            //   Message 7 = 12 parts
            // ...where each part is delimited by 1 or more spaces.
            SpanSplitEnumerator spanSplit =
                new(payload, new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int numParts = 0;
            while (spanSplit.MoveNext())
                numParts++;
            spanSplit.Reset();

            return numParts switch
            {
                VaisalaVisibilityMessage.Parts    => new VaisalaVisibilityMessage().WithSensorID(sensorId).Parse(spanSplit),
                VaisalaPrecipitationMessage.Parts => new VaisalaPrecipitationMessage().WithSensorID(sensorId).Parse(spanSplit),
                VaisalaFullMessage.Parts          => new VaisalaFullMessage().WithSensorID(sensorId).Parse(spanSplit),
                VaisalaAviationMessage.Parts      => new VaisalaAviationMessage().WithSensorID(sensorId).Parse(spanSplit),
                // The status message is the only one beyond 50 parts
                > 50 => new VaisalaStationStatusMessage().WithSensorID(sensorId).Parse(spanSplit),
                _ => null
            };
        }

        protected VaisalaMessage WithSensorID(string sensorId)
        {
            SensorID = sensorId;
            return this;
        }

        protected abstract VaisalaMessage Parse(SpanSplitEnumerator spanSplit);

        protected bool ParseAlarms(ref SpanSplitEnumerator spanSplit)
        {
            if (!spanSplit.MoveNext())
                return false;
            if (!int.TryParse(spanSplit.Current[..1], out int iVisAlarm))
                return false;
            if (!int.TryParse(spanSplit.Current[1..2], out int iHwAlarm))
                return false;

            VisibilityAlarm = (VisibilityAlarm)iVisAlarm;
            HardwareAlarm = (HardwareAlarm)iHwAlarm;
            return true;
        }
    }
}

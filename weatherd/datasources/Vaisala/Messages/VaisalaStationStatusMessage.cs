using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace weatherd.datasources.Vaisala.Messages
{
    public class VaisalaStationStatusMessage : VaisalaMessage
    {
        public VaisalaValue<string> FirmwareVersion { get; private set; }
        public VaisalaValue<string> FirmwareDate { get; private set; }
        public VaisalaValue<string> SerialNumber { get; private set; }

        public VaisalaAlarmValue<float> Signal { get; private set; }
        public VaisalaAlarmValue<float> Offset { get; private set; }
        public VaisalaAlarmValue<float> Drift { get; private set; }
        public VaisalaAlarmValue<int> ReceiverBackscatter { get; private set; }
        public VaisalaAlarmValue<int> ReceiverBackscatterChange { get; private set; }
        public VaisalaAlarmValue<float> TransmitterBackscatter { get; private set; }
        public VaisalaAlarmValue<float> TransmitterBackscatterChange { get; private set; }
        public VaisalaAlarmValue<float> TransmitterIntensity { get; private set; }
        public VaisalaAlarmValue<float> AmbientLight { get; private set; }
        public VaisalaAlarmValue<float> BatteryVoltage { get; private set; }
        public VaisalaAlarmValue<float> DCDCPositiveRail { get; private set; }
        public VaisalaAlarmValue<float> DCDCNegativeRail { get; private set; }
        public VaisalaAlarmValue<float> AmbientTemperature { get; private set; }
        public VaisalaAlarmValue<float> CPUTemperature { get; private set; }
        public VaisalaAlarmValue<float> RainCapTemperature { get; private set; }
        public VaisalaAlarmValue<float> RainCapValue { get; private set; }
        public VaisalaAlarmValue<float> RainCapDryValue { get; private set; }
        public VaisalaAlarmValue<int> BackgroundLuminance { get; private set; }
        public VaisalaAlarmValue<string> Relay1State { get; private set; }
        public VaisalaAlarmValue<string> Relay2State { get; private set; }
        public VaisalaAlarmValue<string> Relay3State { get; private set; }
        public VaisalaAlarmValue<string> HoodHeaters { get; private set; }
        public string[] HardwareAlarms { get; private set; }
        public string[] Warnings { get; private set; }

        /// <inheritdoc />
        public VaisalaStationStatusMessage() : base(VaisalaMessageType.Status)
        { }

        /// <inheritdoc />
        protected override VaisalaMessage Parse(SpanSplitEnumerator spanSplit)
        {
            bool isValid = true;
            
            AssertText(ref spanSplit, "PWD", ref isValid);
            AssertText(ref spanSplit, "STATUS", ref isValid);

            if (!isValid)
                return null; // Don't bother processing anymore, this isn't a status message

            
            AssertText(ref spanSplit, "VAISALA", ref isValid);
            AssertText(ref spanSplit, "PWD12", ref isValid);
            AssertText(ref spanSplit, "V", ref isValid);
            
            FirmwareVersion = VaisalaValue<string>.Parse(ref spanSplit, ref isValid);
            FirmwareDate = VaisalaValue<string>.Parse(ref spanSplit, ref isValid);
            SerialNumber = VaisalaValue<string>.Parse(ref spanSplit, ref isValid);

            AssertText(ref spanSplit, "ID", ref isValid);
            AssertText(ref spanSplit, "STRING:", ref isValid);

            VaisalaValue<string> idString = VaisalaValue<string>.Parse(ref spanSplit, ref isValid);

            if (idString.HasValue && !idString.Value.Equals(SensorID))
                return null; // Wrong sensor ID

            
            AssertText(ref spanSplit, "SIGNAL", ref isValid);
            Signal = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "OFFSET", ref isValid);
            Offset = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "DRIFT", ref isValid);
            Drift = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "REC.", ref isValid);
            AssertText(ref spanSplit, "BACKSCATTER", ref isValid);
            ReceiverBackscatter = VaisalaAlarmValue<int>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "CHANGE", ref isValid);
            ReceiverBackscatterChange = VaisalaAlarmValue<int>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "TR.", ref isValid);
            AssertText(ref spanSplit, "BACKSCATTER", ref isValid);
            TransmitterBackscatter = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "CHANGE", ref isValid);
            TransmitterBackscatterChange = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "LEDI", ref isValid);
            TransmitterIntensity = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "AMBL", ref isValid);
            AmbientLight = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "VBB", ref isValid);
            BatteryVoltage = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "P12", ref isValid);
            DCDCPositiveRail = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "M12", ref isValid);
            DCDCNegativeRail = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "TS", ref isValid);
            AmbientTemperature = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "TB", ref isValid);
            CPUTemperature = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "TDRD", ref isValid);
            RainCapTemperature = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "DRD", ref isValid);
            RainCapValue = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            AssertText(ref spanSplit, "DRY", ref isValid);
            RainCapDryValue = VaisalaAlarmValue<float>.Parse(ref spanSplit, ref isValid);
            
            // The following information is optional
            if (AssertText(ref spanSplit, "BL"))
                BackgroundLuminance = VaisalaAlarmValue<int>.Parse(ref spanSplit, ref isValid);
            else spanSplit.BackOne();

            if (AssertText(ref spanSplit, "RELAYS"))
            {
                Relay1State = VaisalaAlarmValue<string>.Parse(ref spanSplit, ref isValid);
                Relay2State = VaisalaAlarmValue<string>.Parse(ref spanSplit, ref isValid);
                Relay3State = VaisalaAlarmValue<string>.Parse(ref spanSplit, ref isValid);
            } else spanSplit.BackOne();

            if (AssertText(ref spanSplit, "HOOD"))
            {
                AssertText(ref spanSplit, "HEATERS", ref isValid);
                HoodHeaters = VaisalaAlarmValue<string>.Parse(ref spanSplit, ref isValid);
            } else
                spanSplit.BackOne();


            AssertText(ref spanSplit, "HARDWARE", ref isValid);
            AssertText(ref spanSplit, ":", ref isValid);

            // Don't bother processing any further
            if (!isValid)
                return null;

            List<string> hardwareAlarms = new List<string>();
            List<string> warnings = new List<string>();

            int index = spanSplit.Index;
            var span = spanSplit.Original;

            // We should be at a \n
            if (span[index] != '\n')
                return null;
            
            // Keep looping through lines until we get to the word "WARNINGS"
            bool hardware = true;
            int start = index;
            for (; index < span.Length; index++)
            {
                if (!(span[index] == '\r' && span[++index] == '\n'))
                    continue;

                string va = span[start..index].Trim().ToString();
                start = index;

                if (va.Equals("WARNINGS :", StringComparison.Ordinal))
                {
                    hardware = false;
                    continue;
                }

                if (va.Equals("OK", StringComparison.Ordinal))
                {
                    // ignore
                    continue;
                }

                if (hardware)
                    hardwareAlarms.Add(va);
                else
                    warnings.Add(va);
            }

            HardwareAlarms = hardwareAlarms.ToArray();
            Warnings = warnings.ToArray();
            
            return this;
        }

        public static bool AssertText(ref SpanSplitEnumerator enumerator, string text, ref bool isValid)
        {
            // If one of the parses fails, don't continue to attempt parsing
            if (!isValid)
                return false;

            if (!enumerator.MoveNext())
            {
                isValid = false;
                return false;
            }

            if (enumerator.Current.Equals(text, StringComparison.Ordinal))
                return true;

            isValid = false;
            return false;

        }

        public static bool AssertText(ref SpanSplitEnumerator enumerator, string text)
            => enumerator.MoveNext() && enumerator.Current.Equals(text, StringComparison.Ordinal);
    }
}

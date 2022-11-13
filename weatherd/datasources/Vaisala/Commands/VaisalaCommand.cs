using System;
using System.Text;

namespace weatherd.datasources.Vaisala.Commands
{
    public abstract class VaisalaCommand
    {
        protected const char ESC = '\x5';
        protected const char CR = '\r';
        
        public string SensorID { get; }

        protected VaisalaCommand(string sensorId)
        {
            if (sensorId.Length >= 2)
                throw new ArgumentOutOfRangeException(nameof(sensorId), "Sensor ID must be a string of length 1 or 2.");
            SensorID = sensorId;
        }

        protected void CompileHeader(ref StringBuilder stringBuilder)
        {
            stringBuilder.Append(CR);
            stringBuilder.Append(ESC);
            stringBuilder.Append("PW ");

            switch (SensorID.Length)
            {
                case 1:
                    stringBuilder.Append(" ");
                    stringBuilder.Append(SensorID);
                    break;
                case 2:
                    stringBuilder.Append(SensorID);
                    break;
                default:
                    throw new InvalidOperationException("Sensor ID cannot be greater than 2 characters.");
            }

            stringBuilder.Append(" ");
        }

        public abstract string Compile();
    }
}

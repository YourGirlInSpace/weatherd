using System;

namespace weatherd.aprs.telemetry.metrics
{
    public class AnalogTelemetryMetric : TelemetryMetric
    {
        public float EqnA;
        public float EqnB;
        public float EqnC;

        public AnalogTelemetryMetric(string name, string unit, float a, float b, float c)
            : base(name, unit)
        {
            EqnA = a;
            EqnB = b;
            EqnC = c;
        }

        public byte TransformTo(float value)
        {
            // an^2 + bn + c = value, we solve for 'n'

            // n = sqrt(4a^2v-4a^2c+b^2)-b / 2a

            float result;

            switch (EqnA)
            {
                case 0 when EqnB != 0:
                    // bn+c=v
                    result = (value - EqnC) / EqnB;
                    break;
                case 0 when EqnB == 0:
                    result = EqnC;
                    break;
                default:
                    float x = 4 * EqnA * EqnA * value;
                    float y = -4 * EqnA * EqnA * EqnC;
                    float z = EqnB * EqnB;
                    float w = 2 * EqnA * EqnA;

                    result = ((float) Math.Sqrt(x + y + z) - EqnB) / w;
                    break;
            }

            return (byte)Math.Floor(result);
        }

        public float TransformFrom(byte analog) => (float)(Math.Pow(EqnA * analog, 2) + (EqnB * analog) + EqnC);
    }
}

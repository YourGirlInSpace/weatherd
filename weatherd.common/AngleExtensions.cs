using UnitsNet;

namespace weatherd
{
    public static class AngleExtensions
    {
        /// <summary>
        /// Normalizes an <see cref="Angle"/> to between the range of 0°-360° / 0-2π
        /// </summary>
        /// <param name="angle"></param>
        public static void Normalize(this Angle angle)
        {
            // Normalize the wind direction
            while (angle < Angle.Zero)
                angle += Angle.FromDegrees(360);
            while (angle.Degrees > 360)
                angle -= Angle.FromDegrees(360);
        }
    }
}
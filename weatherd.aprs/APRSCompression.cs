using System;
using System.Text;

namespace weatherd.aprs
{
    public static class APRSCompression
    {
        private const int Base = 91;
        private const int ASCIIOffset = 33;

        /// <summary>
        /// Compresses integer information into a compressed data string.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <returns>The compressed data string.</returns>
        public static string Compress(int data)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 3; i >= 0; i--)
            {
                int factor = (int) Math.Pow(Base, i);
                int ch = (int) Math.Floor(data / (double)factor);
                data %= factor;

                builder.Append((char)(ch+ASCIIOffset));
            }

            return builder.ToString();
        }
        
        /// <summary>
        /// Decompresses a compressed data string.
        /// </summary>
        /// <param name="data">The compressed data string.</param>
        /// <returns>The decompressed value.</returns>
        public static int Decompress(string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            int sum = 0;
            for (int i = (data.Length - 1), j = 0; j < data.Length; i--, j++)
            {
                sum += (data[j] - ASCIIOffset) * (int) Math.Pow(Base, i);
            }

            return sum;
        }
        
        /// <summary>
        /// Decompresses a compressed data string containing latitude information.
        /// </summary>
        /// <param name="data">The compressed data string containing latitude information.</param>
        /// <returns>The decompressed latitude.</returns>
        public static float DecompressLatitude(string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length != 4)
                throw new ArgumentOutOfRangeException(nameof(data), "Compressed data must be 4 bytes in length.");

            int decomp = Decompress(data);

            return 90 - decomp / 380926f;
        }

        /// <summary>
        /// Decompresses a compressed data string containing longitude information.
        /// </summary>
        /// <param name="data">The compressed data string containing longitude information.</param>
        /// <returns>The decompressed longitude.</returns>
        public static float DecompressLongitude(string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length != 4)
                throw new ArgumentOutOfRangeException(nameof(data), "Compressed data must be 4 bytes in length.");

            int decomp = Decompress(data);

            return -180 + decomp / 190463f;
        }

        public static string CompressLatitude(float latitude)
        {
            if (latitude < -90 || latitude > 90)
                throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and +90 degrees.");

            int rez = (int) Math.Floor(380926 * (90 - latitude));

            return Compress(rez);
        }

        public static string CompressLongitude(float longitude)
        {
            if (longitude < -180 || longitude > 180)
                throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and +180 degrees.");

            int rez = (int) Math.Floor(190463 * (180 + longitude));

            return Compress(rez);
        }

        public static string CompressCourseSpeed(float course, float speed)
        {
            if (course < 0 || course > 360)
                throw new ArgumentOutOfRangeException(nameof(course), "Course must be between 0 and 360 degrees.");
            if (speed < 0)
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed cannot be negative.");

            char c = (char) (course / 4f + ASCIIOffset);
            char s = (char) (Math.Round(Math.Log(speed + 1, 1.08)) + ASCIIOffset);

            return $"{c}{s}";
        }

        public static (float course, float speed) DecompressCourseSpeed(string cs)
        {
            if (cs == null)
                throw new ArgumentNullException(nameof(cs));
            if (cs.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(cs), "Compressed data must be 2 bytes in length.");

            int c = cs[0] - ASCIIOffset;
            int s = cs[1] - ASCIIOffset;

            float course = c * 4;
            float speed = (float) Math.Pow(1.08, s) - 1f;

            return (course, speed);
        }
    }
}

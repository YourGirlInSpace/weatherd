using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Extensions.Configuration;

namespace weatherd
{
    internal static class Utilities
    {
        public const double JulianPeriod = 2451544.5;
        
        public static double ToJulianDate(DateTime dt)
        {
            DateTime utc = dt.ToUniversalTime();
            DateTime epoch = new DateTime(2000, 1, 1, 0, 0, 0);

            TimeSpan diff = utc - epoch;

            double numDays = Math.Floor(diff.TotalDays);

            return numDays + JulianPeriod + (utc.Hour + utc.Minute / 60.0 + utc.Second / 3600.0) / 24.0;
        }

        public static double ToJulianDate(int year, int month, int day, int hour, int minute, int second)
        {
            const long IGREG2 = 15 + 31L * (10 + 12L * 1582);

            double deltaTime = hour / 24.0 + minute / (24.0 * 60.0) + second / (24.0 * 60.0 * 60.0) - 0.5;

            long jm;

            long jy = year;
            if (month > 2)
                jm = month + 1;
            else
            {
                --jy;
                jm = month + 13;
            }

            long laa = 1461 * jy / 4;
            if (jy < 0 && jy % 4 == 0)
                --laa;
            long lbb = 306001 * jm / 10000;
            long ljul = laa + lbb + day + 1720995;

            if (day + 31 * (month + 12 * year) < IGREG2)
                return ljul + deltaTime;

            long lcc = jy / 100;
            if (jy < 0 && jy % 100 == 0)
                --lcc;
            long lee = lcc / 4;
            if (lcc < 0 && lcc % 4 == 0)
                --lee;
            ljul += 2 - lcc + lee;

            return ljul + deltaTime;
        }

        internal static double Clamp(double val, double min, double max)
        {
            if (val < min)
                return min;
            if (val > max)
                return max;

            return val;
        }

        public static DateTime FromJulianDate(double jd)
        {
            double jde = jd - JulianPeriod;
            DateTime epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return epoch.AddDays(jde);
        }

        public static void FromJulianDate(double jd, out int year, out int month, out int day)
        {
            const long JD_GREG_CAL = 2299161;
            const int JB_MAX_WITHOUT_OVERFLOW = 107374182;

            long julian = (long)Math.Floor(jd + 0.5);

            long ta, jalpha, tc;

            if (julian >= JD_GREG_CAL)
            {
                jalpha = (4 * (julian - 1867216) - 1) / 146097;
                ta = julian + 1 + jalpha - jalpha / 4;
            }
            else if (julian < 0)
                ta = julian + 36525 * (1 - julian / 36525);
            else
                ta = julian;

            long tb = ta + 1524;
            if (tb <= JB_MAX_WITHOUT_OVERFLOW)
                tc = (tb * 20 - 2442) / 7305;
            else
                tc = (tb * 20 - 2442) / 7305;
            long td = 365 * tc + tc / 4;
            long te = (tb - td) * 10000 / 306001;

            day = (int)(tb - td - 306001 * te / 10000);

            month = (int)(te - 1);
            if (month > 12)
                month -= 12;
            year = (int)(tc - 4715);
            if (month > 2)
                --year;
            if (julian < 0)
                year -= (int)(100 * (1 - julian / 36525));
        }

        public static double ToJulianCentury(DateTime dt) => ToJulianCentury(ToJulianDate(dt));
        public static double ToJulianCentury(double jd) => (jd - 2451545.0) / 36525.0;

        public static double Lerp(double low, double high, double amount) => low + (high - low) * amount;

        public static float TryGetConfigurationKey(IConfiguration cfg, string key)
        {
            if (!float.TryParse(cfg[key], out float value))
                throw new InvalidOperationException($"Config key '{key}' is missing");

            return value;
        }

        /// <summary>
        /// Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The attribute of type T that exists on the enum value</returns>
        /// <example><![CDATA[string desc = myEnumVariable.GetAttributeOfType<DescriptionAttribute>().Description;]]></example>
        public static T GetAttributeOfType<T>(Enum enumVal) where T:Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T)attributes[0] : null;
        }

        public static byte[] StringToByteArrayFastest(string hex) {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex) {
            int val = hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static string GetEnumMemberValue<T>(this T value)
            where T : Enum
        {
            return typeof(T)
                   .GetTypeInfo()
                   .DeclaredMembers
                   .SingleOrDefault(x => x.Name == value.ToString())
                   ?.GetCustomAttribute<EnumMemberAttribute>(false)
                   ?.Value;
        }
    }
}

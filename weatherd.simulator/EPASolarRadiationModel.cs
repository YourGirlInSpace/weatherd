#if DEBUG
using System;
using UnitsNet;
using UnitsNet.Units;
using static System.Math;

namespace weatherd
{
    public class EPASolarRadiationModel
    {
        private readonly double _lat;
        private double _lon;
        private double _latDeg;
        private readonly double _lonDeg;

        public EPASolarRadiationModel(double latitude, double longitude)
        {
            _latDeg = latitude;
            _lonDeg = longitude;

            _lat = latitude * PI / 180.0;
            _lon = longitude * PI / 180.0;
        }

        public Irradiance SolarRadiation => GetSolarRadiation();

        private Irradiance GetSolarRadiation() => CalculateSolarRadiation(DateTime.Now);

        public Irradiance CalculateSolarRadiation(DateTime now)
        {
            double currentHour = now.Hour + now.Minute / 60.0 + now.Second / 3600.0;
            double julianDay = now.DayOfYear + currentHour / 24.0;

            // The equation of time (hours) represents the difference between true solar
            // time and mean solar time due to seasonal variations in the orbital velocity
            // of the Earth [DiLaura, 1984]
            double eqTime = 0.170 * Sin(4.0 * PI * (Floor(julianDay) - 80.0) / 373.0) -
                            0.129 * Sin(2.0 * PI * (Floor(julianDay) - 8.0) / 355.0);

            // The nearest standard meridian to the longitude.
            // An alternative way to calculate this is:
            //   stdMerid = -15.0 * Math.Floor(tzOffset)
            double stdMerid = 15.0 * Floor(_lonDeg / 15.0);

            // Hour angle (radians) is the angular position for a given location at a specific
            // time during the day [Ryan and Stolzenbach, 1972]
            double hourAngle = 2 * PI / 24.0 *
                               (currentHour - (_lonDeg - stdMerid) * (24.0 / 360.0) + eqTime - 12.0);

            // Angular fraction of the year [Spencer, 1971] 
            double tD = 2 * PI * (Floor(julianDay) - 1) / 365.0;

            // Solar declination angle (radians) [Spencer, 1971]
            double declination = 0.006918 - 
                                 0.399912 * Cos(tD)     + 0.070257 * Sin(tD) -
                                 0.006758 * Cos(2 * tD) + 0.000907 * Sin(2 * tD) -
                                 0.002697 * Cos(3 * tD) + 0.001480 * Sin(3 * tD);

            // A0 (degrees) is the angular inclination of the sun relative to the horizon from an observer's
            // perspective [Winderlich, 1972; Meeus, 1999]
            double A0 = Asin(Sin(_lat) * Sin(declination) +
                                  Cos(_lat) * Cos(declination) * Cos(hourAngle)) * 180.0 / PI;

            // No solar radiation is present if the sun is below the horizon
            if (A0 < 0)
                return Irradiance.Zero;

            // Clear sky solar radiation at the ground surface was originally computed in BTU/sqft*day but
            // was converted to W/m^2 below (using the 0.1314 factor).  [Cole and Wells, 2000; EPA 1971]
            double phiS = 24 * (2.044 * A0 + 0.1269 * Pow(A0, 2) - 1.941e-3 * Pow(A0, 3) +
                                7.591e-6 * Pow(A0, 4)) * 0.1314;

            return new Irradiance(phiS, IrradianceUnit.WattPerSquareMeter);
        }
    }
}
#endif
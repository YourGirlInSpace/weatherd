#if DEBUG
using System;
using weatherd.math;

namespace weatherd
{
    public class Sun
    {
        public static DateTime CalculateSolarNoon(double jd, Angle latitude, Angle longitude)
        {
            double tNoon = Utilities.ToJulianCentury(jd - longitude.Degrees / 360.0);
            double eqTime = CalculateEquationofTime(tNoon);
            double solarNoonoffset = 720.0 - longitude.Degrees * 4 - eqTime;
            double newt = Utilities.ToJulianCentury(jd + solarNoonoffset / 1440.0);

            eqTime = CalculateEquationofTime(newt);
            double solarNoonUTC = 720 - longitude.Degrees * 4 - eqTime;

            while (solarNoonUTC < 0.0)
                solarNoonUTC += 1440;
            while (solarNoonUTC >= 1440.0)
                solarNoonUTC -= 1440.0;

            DateTime time = Utilities.FromJulianDate(jd).Date;

            return time + TimeSpan.FromMinutes(solarNoonUTC);
        }

        public static DateTime CalculateSunrise(double jd, Angle latitude, Angle longitude)
        {
            double t = Utilities.ToJulianCentury(jd);
            double eqTime = CalculateEquationofTime(t);
            Angle solarDec = CalculateSolarDeclination(t);
            Angle hourAngle = CalculateHourAngleSunrise(latitude, solarDec);

            Angle delta = longitude + hourAngle;

            double timeUTC = 720 - 4.0 * delta.Degrees - eqTime; // in minutes

            DateTime time = Utilities.FromJulianDate(jd).Date;

            return time + TimeSpan.FromMinutes(timeUTC);
        }

        public static DateTime CalculateSunset(double jd, Angle latitude, Angle longitude)
        {
            double t = Utilities.ToJulianCentury(jd);
            double eqTime = CalculateEquationofTime(t);
            Angle solarDec = CalculateSolarDeclination(t);
            Angle hourAngle = -CalculateHourAngleSunrise(latitude, solarDec);

            Angle delta = longitude + hourAngle;

            double timeUTC = 720 - 4.0 * delta.Degrees - eqTime; // in minutes

            DateTime time = Utilities.FromJulianDate(jd).Date;

            return time + TimeSpan.FromMinutes(timeUTC);
        }

        private static Angle CalculateHourAngleSunrise(Angle latitude, Angle solarDec)
        {
            double HAarg = Angle.FromDegrees(90.833).Cos() / (latitude.Cos() * solarDec.Cos()) -
                           latitude.Tan() * solarDec.Tan();

            Angle HA = Angle.Acos(HAarg);

            return HA;
        }

        public static Angle CalculateElevation(DateTime time, Angle latitude, Angle longitude)
        {
            double t = Utilities.ToJulianCentury(time);

            double localTime = time.Hour * 60.0 + time.Minute + time.Second / 60.0;

            double zone = TimeZoneInfo.Local.BaseUtcOffset.TotalHours;
            // Example?

            Angle elev = CalculateElevation(t, localTime, latitude, longitude, zone);

            return elev;
        }

        public static Angle CalculateElevation(double t, double localTime, Angle latitude, Angle longitude, double zone)
        {
            double eqTime = CalculateEquationofTime(t);
            Angle theta = CalculateSolarDeclination(t);

            double solarTimeFix = eqTime + 4.0 * longitude.Degrees - 60.0 * zone;
            double trueSolarTime = localTime + solarTimeFix;

            while (trueSolarTime > 1440)
                trueSolarTime -= 1440;

            Angle hourAngle = Angle.FromDegrees(trueSolarTime / 4.0 - 180.0);
            if (hourAngle.Degrees < -180)
                hourAngle += Angle.Pos360;

            double csz = latitude.Sin() * theta.Sin() + latitude.Cos() * theta.Cos() * hourAngle.Cos();
            csz = Utilities.Clamp(csz, -1.0, 1.0);

            Angle zenith = Angle.Acos(csz);
            Angle exoatmElevation = Angle.Pos90 - zenith;

            Angle refractionCorrection = CalculateRefraction(exoatmElevation);

            Angle solarZen = zenith - refractionCorrection;
            Angle elevation = Angle.Pos90 - solarZen;

            return elevation;
        }

        private static Angle CalculateRefraction(Angle elev)
        {
            if (elev.Degrees > 85.0)
                return Angle.Zero;

            double te = elev.Tan();

            double correction;
            if (elev.Degrees > 5.0)
                correction = 58.1 / te - 0.07 / (te * te * te) + 0.000086 / (te * te * te * te * te);
            else if (elev.Degrees > -0.575)
                correction = 1735.0 + elev.Degrees *
                    (-518.2 + elev.Degrees * (103.4 + elev.Degrees * (-12.79 + elev.Degrees * 0.711)));
            else
                correction = -20.774 / te;

            return Angle.FromDegrees(correction / 3600.0);
        }

        private static Angle CalculateSolarDeclination(double t)
        {
            Angle e = CalculateObliquityCorrection(t);
            Angle lambda = CalculateApparentSolarLongitude(t);

            double sint = e.Sin() * lambda.Sin();
            Angle theta = Angle.Asin(sint);

            return theta;
        }

        private static Angle CalculateApparentSolarLongitude(double t)
        {
            Angle o = CalculateTrueSolarLongitude(t);
            Angle omega = Angle.FromDegrees(125.04 - 1934.136 * t);
            Angle lambda = Angle.FromDegrees(o.Degrees - 0.00569 - 0.00478 * omega.Sin());

            return lambda;
        }

        private static Angle CalculateTrueSolarLongitude(double t)
        {
            Angle l0 = CalculateGeometricSolarMeanLongitude(t);
            Angle c = CalculateSolarEquationOfCenter(t);
            Angle O = l0 + c;

            return O;
        }

        private static Angle CalculateSolarEquationOfCenter(double t)
        {
            Angle m = CalculateGeometricSolarMeanAnomaly(t);
            double sinm = m.Sin();
            double sin2m = (2 * m).Sin();
            double sin3m = (3 * m).Sin();
            double c = sinm * (1.914602 - t * (0.004817 + 0.000014 * t)) + sin2m * (0.019993 - 0.000101 * t) +
                       sin3m * 0.000289;

            return Angle.FromDegrees(c);
        }

        private static double CalculateEquationofTime(double t)
        {
            Angle epsilon = CalculateObliquityCorrection(t);
            Angle l0 = CalculateGeometricSolarMeanLongitude(t);
            double e = CalculateEarthOrbitEccentricity(t);
            Angle m = CalculateGeometricSolarMeanAnomaly(t);

            double y = (epsilon / 2).Tan();
            y *= y;

            double sin2l0 = (2 * l0).Sin();
            double sinm = m.Sin();
            double cos2l0 = (2 * l0).Cos();
            double sin4l0 = (4 * l0).Sin();
            double sin2m = (2 * m).Sin();

            Angle eTime = Angle.FromRadians(y * sin2l0 - 2.0 * e * sinm + 4.0 * e * y * sinm * cos2l0 -
                                            0.5 * y * y * sin4l0 -
                                            1.25 * e * e * sin2m);

            return eTime.Degrees * 4.0;
        }

        private static Angle CalculateGeometricSolarMeanAnomaly(double t)
        {
            double M = 357.52911 + t * (35999.05029 - 0.0001537 * t);
            return Angle.FromDegrees(M);
        }

        private static double CalculateEarthOrbitEccentricity(double t)
        {
            double e = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);

            return e;
        }

        private static Angle CalculateGeometricSolarMeanLongitude(double t)
        {
            double L0 = 280.46646 + t * (36000.76983 + t * 0.0003032);
            while (L0 > 360.0)
                L0 -= 360;
            while (L0 < 0)
                L0 += 360;

            return Angle.FromDegrees(L0);
        }

        private static Angle CalculateObliquityCorrection(double t)
        {
            Angle e0 = CalculateMeanObliquityOfEcliptic(t);
            Angle omega = Angle.FromDegrees(125.04 - 1934.136 * t);
            Angle e = Angle.FromDegrees(e0.Degrees + 0.00256 * omega.Cos());

            return e;
        }

        private static Angle CalculateMeanObliquityOfEcliptic(double t)
        {
            double seconds = 21.448 - t * (46.8150 + t * (0.00059 - t * 0.001813));
            double e0 = 23.0 + (26.0 + seconds / 60.0) / 60.0;

            return Angle.FromDegrees(e0);
        }
    }
}
#endif

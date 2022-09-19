#if DEBUG
using System;
using weatherd.models;

namespace weatherd
{
    public class Sun
    {
        public static DateTime CalculateSolarNoon(double jd, Angle latitude, Angle longitude)
        {
            var tNoon = Utilities.ToJulianCentury(jd - longitude.Degrees / 360.0);
            var eqTime = CalculateEquationofTime(tNoon);
            var solarNoonoffset = 720.0 - longitude.Degrees * 4 - eqTime;
            var newt = Utilities.ToJulianCentury(jd + solarNoonoffset / 1440.0);

            eqTime = CalculateEquationofTime(newt);
            var solarNoonUTC = 720 - longitude.Degrees * 4 - eqTime;

            while (solarNoonUTC < 0.0)
                solarNoonUTC += 1440;
            while (solarNoonUTC >= 1440.0)
                solarNoonUTC -= 1440.0;

            DateTime time = Utilities.FromJulianDate(jd).Date;

            return time + TimeSpan.FromMinutes(solarNoonUTC);
        }

        public static DateTime CalculateSunrise(double jd, Angle latitude, Angle longitude)
        {
            var t = Utilities.ToJulianCentury(jd);
            var eqTime = CalculateEquationofTime(t);
            var solarDec = CalculateSolarDeclination(t);
            var hourAngle = CalculateHourAngleSunrise(latitude, solarDec);

            var delta = longitude + hourAngle;

            var timeUTC = 720 - 4.0 * delta.Degrees - eqTime; // in minutes

            DateTime time = Utilities.FromJulianDate(jd).Date;

            return time + TimeSpan.FromMinutes(timeUTC);
        }

        public static DateTime CalculateSunset(double jd, Angle latitude, Angle longitude)
        {
            var t = Utilities.ToJulianCentury(jd);
            var eqTime = CalculateEquationofTime(t);
            var solarDec = CalculateSolarDeclination(t);
            var hourAngle = -CalculateHourAngleSunrise(latitude, solarDec);

            var delta = longitude + hourAngle;

            var timeUTC = 720 - 4.0 * delta.Degrees - eqTime; // in minutes

            DateTime time = Utilities.FromJulianDate(jd).Date;

            return time + TimeSpan.FromMinutes(timeUTC);
        }

        private static Angle CalculateHourAngleSunrise(Angle latitude, Angle solarDec)
        {
            var HAarg = Angle.FromDegrees(90.833).Cos() / (latitude.Cos() * solarDec.Cos()) -
                        latitude.Tan() * solarDec.Tan();

            var HA = Angle.Acos(HAarg);

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
            var eqTime = CalculateEquationofTime(t);
            var theta = CalculateSolarDeclination(t);

            var solarTimeFix = eqTime + 4.0 * longitude.Degrees - 60.0 * zone;
            var trueSolarTime = localTime + solarTimeFix;

            while (trueSolarTime > 1440)
                trueSolarTime -= 1440;

            Angle hourAngle = Angle.FromDegrees(trueSolarTime / 4.0 - 180.0);
            if (hourAngle.Degrees < -180)
                hourAngle += Angle.Pos360;

            var csz = latitude.Sin() * theta.Sin() + latitude.Cos() * theta.Cos() * hourAngle.Cos();
            csz = Utilities.Clamp(csz, -1.0, 1.0);

            var zenith = Angle.Acos(csz);
            var exoatmElevation = Angle.Pos90 - zenith;

            var refractionCorrection = CalculateRefraction(exoatmElevation);

            var solarZen = zenith - refractionCorrection;
            var elevation = Angle.Pos90 - solarZen;

            return elevation;
        }

        private static Angle CalculateRefraction(Angle elev)
        {
            if (elev.Degrees > 85.0)
                return Angle.Zero;

            var te = elev.Tan();

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

            var sint = e.Sin() * lambda.Sin();
            var theta = Angle.Asin(sint);

            return theta;
        }

        private static Angle CalculateApparentSolarLongitude(double t)
        {
            var o = CalculateTrueSolarLongitude(t);
            var omega = Angle.FromDegrees(125.04 - 1934.136 * t);
            var lambda = Angle.FromDegrees(o.Degrees - 0.00569 - 0.00478 * omega.Sin());

            return lambda;
        }

        private static Angle CalculateTrueSolarLongitude(double t)
        {
            Angle l0 = CalculateGeometricSolarMeanLongitude(t);
            Angle c = CalculateSolarEquationOfCenter(t);
            var O = l0 + c;

            return O;
        }
        
        private static Angle CalculateSolarEquationOfCenter(double t)
        {
            var m = CalculateGeometricSolarMeanAnomaly(t);
            var sinm = m.Sin();
            var sin2m = (2 * m).Sin();
            var sin3m = (3 * m).Sin();
            var c = sinm * (1.914602 - t * (0.004817 + 0.000014 * t)) + sin2m * (0.019993 - 0.000101 * t) +
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

            var sin2l0 = (2 * l0).Sin();
            var sinm = m.Sin();
            var cos2l0 = (2 * l0).Cos();
            var sin4l0 = (4 * l0).Sin();
            var sin2m = (2 * m).Sin();

            var eTime = Angle.FromRadians(y * sin2l0 - 2.0 * e * sinm + 4.0 * e * y * sinm * cos2l0 - 0.5 * y * y * sin4l0 -
                                          1.25 * e * e * sin2m);

            return eTime.Degrees * 4.0;
        }

        private static Angle CalculateGeometricSolarMeanAnomaly(double t)
        {
            var M = 357.52911 + t * (35999.05029 - 0.0001537 * t);
            return Angle.FromDegrees(M);
        }

        private static double CalculateEarthOrbitEccentricity(double t)
        {
            var e = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);

            return e;
        }

        private static Angle CalculateGeometricSolarMeanLongitude(double t)
        {
            var L0 = 280.46646 + t * (36000.76983 + t * 0.0003032);
            while (L0 > 360.0)
                L0 -= 360;
            while (L0 < 0)
                L0 += 360;

            return Angle.FromDegrees(L0);
        }

        private static Angle CalculateObliquityCorrection(double t)
        {
            var e0 = CalculateMeanObliquityOfEcliptic(t);
            var omega = Angle.FromDegrees(125.04 - 1934.136 * t);
            var e = Angle.FromDegrees(e0.Degrees + 0.00256 * omega.Cos());

            return e;
        }

        private static Angle CalculateMeanObliquityOfEcliptic(double t)
        {
            var seconds = 21.448 - t * (46.8150 + t * (0.00059 - t * 0.001813));
            var e0 = 23.0 + (26.0 + seconds / 60.0) / 60.0;

            return Angle.FromDegrees(e0);
        }
    }
}
#endif
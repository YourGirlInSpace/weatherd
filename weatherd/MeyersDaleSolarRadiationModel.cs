#if DEBUG
using System;
using UnitsNet;
using UnitsNet.Units;
using static System.Math;
using Angle = weatherd.math.Angle;
using Log = Serilog.Log;

namespace weatherd
{
    public class MeyersDaleSolarRadiationModel
    {
        public MeyersDaleSolarRadiationModel(double latitude, double longitude)
            : this(Angle.FromDegrees(latitude), Angle.FromDegrees(longitude))
        {
        }

        public MeyersDaleSolarRadiationModel(Angle latitude, Angle longitude)
        {
            _latitude = latitude;
            _longitude = longitude;

            Log.Information("Meyers-Dale solar radition model parameters:  Lat={latitude}, Lon={longitude}",
                            latitude, longitude);
        }

        public Irradiance CalculateSolarRadiation(DateTime now)
        {
            // Standard surface dewpoint in celsius
            int Td = 59;
            // Standard surface pressure in kPa
            double p = 101.325;
            double x = 0.935;

            double lambda = CalculateLambda(now, _latitude);

            Angle solarZenith = Angle.Pos90 - Sun.CalculateElevation(now, _latitude, _longitude);
            double m = 35 * Pow(1224 * Pow(solarZenith.Cos(), 2) + 1, -0.5);
            double u = Exp(0.1133 - Log(lambda + 1) + 0.0393 * Td);
            double TrTg = 1.021 - 0.084 * Pow(m * (949 * p * Pow(10, -5) + 0.051), 0.5);
            double Tw = 1 - 0.077 * Pow(u * m, 0.3);
            double Ta = Pow(x, m);

            Irradiance I0 = CalculateAtmosphericTopSolarInsolation(now);

            double surfaceIrradiance = I0.WattsPerSquareMeter * solarZenith.Cos() * TrTg * Tw * Ta;

            if (surfaceIrradiance < 0)
                surfaceIrradiance = 0;

            return new Irradiance(surfaceIrradiance, IrradianceUnit.WattPerSquareMeter);
        }

        private static Irradiance CalculateAtmosphericTopSolarInsolation(DateTime time)
        {
            const double Isc = 1366.1;

            double dayOfYear = time.DayOfYear;
            Angle theta = Angle.FromRadians(2 * PI * (dayOfYear - 1) / 365.0);

            return new Irradiance(Isc * (1.00011 + 0.034221 * theta.Cos() + 0.00128 * theta.Sin() -
                                         0.000719 * (2 * theta).Cos() +
                                         0.000077 * (2 * theta).Sin()), IrradianceUnit.WattPerSquareMeter);
        }

        private double CalculateLambda(DateTime time, Angle latitude)
        {
            double[][] Lambdas =
            {
                new[] { 3.37, 2.85, 2.80, 2.64 },
                new[] { 2.99, 3.02, 2.70, 2.93 },
                new[] { 3.60, 3.00, 2.98, 2.93 },
                new[] { 3.04, 3.11, 2.92, 2.94 },
                new[] { 2.70, 2.95, 2.77, 2.71 },
                new[] { 2.52, 3.07, 2.67, 2.93 },
                new[] { 1.76, 2.69, 2.61, 2.61 },
                new[] { 1.60, 1.67, 2.24, 2.63 },
                new[] { 1.11, 1.44, 1.94, 2.02 }
            };

            int latIndex = (int)Floor(latitude.Degrees / 10);

            double[] lambdaArgs = Lambdas[latIndex];
            double daysInYear = DateTime.IsLeapYear(time.Year) ? 366.0 : 365.0;
            double yearPercent = time.DayOfYear / daysInYear;

            int indexLow = (int)Floor(yearPercent * 4);
            int indexHigh = (int)Ceiling(yearPercent * 4);
            double percentIn = yearPercent * 4 - indexLow;

            return Utilities.Lerp(lambdaArgs[indexLow], lambdaArgs[indexHigh], percentIn);
        }

        private readonly Angle _latitude;
        private readonly Angle _longitude;
    }
}
#endif

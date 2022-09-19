#if DEBUG
using System;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace weatherd.datasources.testdatasource
{
    public class SyntheticWind
    {
        public float WeibullShapeFactor { get; }

        public float OneHourAutocorrelationFactor { get; }
        public float DiurnalPatternStrength { get; }
        public int HourOfPeakWindSpeed { get; }
        public float MeanWindSpeed { get; }

        public int TimeStep { get; }

        public SyntheticWind()
        {
            random = new Random();
            _lastSpeed = 0;
            _lastDir = 0;
        }

        public SyntheticWind(
            float meanWindSpeed,
            float weibullShapeFactor,
            float oneHourAutocorrelationFactor,
            float diurnalPatternStrength,
            int hourOfPeakWindSpeed,
            int timeStep)
            : this()
        {
            WeibullShapeFactor = weibullShapeFactor;
            OneHourAutocorrelationFactor = oneHourAutocorrelationFactor;
            DiurnalPatternStrength = diurnalPatternStrength;
            HourOfPeakWindSpeed = hourOfPeakWindSpeed;
            MeanWindSpeed = meanWindSpeed;
            TimeStep = timeStep;

            Log.Information(
                "Synthetic wind model parameters:  Weibull Shape Factor={weibull}, Autocorrelation Factor={autoCorr}, Diurnal strength parameter={diurnal}, Mean wind speed={meanWind}",
                WeibullShapeFactor, OneHourAutocorrelationFactor, DiurnalPatternStrength, MeanWindSpeed);
        }

        public SyntheticWind(IConfiguration config)
            : this()
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            HourOfPeakWindSpeed = (int)Utilities.TryGetConfigurationKey(config, "PeakWindSpeedHour");
            DiurnalPatternStrength = Utilities.TryGetConfigurationKey(config, "DiurnalPatternStrength");
            OneHourAutocorrelationFactor = Utilities.TryGetConfigurationKey(config, "OneHourAutocorrelationFactor");
            WeibullShapeFactor = Utilities.TryGetConfigurationKey(config, "WeibullShapeFactor");
            MeanWindSpeed = Utilities.TryGetConfigurationKey(config, "MeanWindSpeed");
            TimeStep = (int)Utilities.TryGetConfigurationKey(config, "TimeStep");

            Log.Information(
                "Synthetic wind model parameters:  Weibull Shape Factor={weibull}, Autocorrelation Factor={autoCorr}, Diurnal strength parameter={diurnal}, Mean wind speed={meanWind}",
                WeibullShapeFactor, OneHourAutocorrelationFactor, DiurnalPatternStrength, MeanWindSpeed);
        }

        public float WindSpeed()
        {
            float fractionalHour = DateTime.Now.Hour + DateTime.Now.Minute / 60.0f + DateTime.Now.Second / 3600.0f;

            float k = 60f / (TimeStep * 60f);
            float r1 = (float)Math.Exp(Math.Log(OneHourAutocorrelationFactor) / k);

            float whiteNoise = WhiteNoise(0, 1);
            float newValue = r1 * _lastSpeed + whiteNoise;
            float hourMeanWindSpeed = HourMeanWindSpeed(fractionalHour);

            newValue += hourMeanWindSpeed;
            newValue *= 0.05f;

            _lastSpeed = newValue;
            return newValue;
        }

        public float WindDirection()
        {
            float k = 60f / (TimeStep * 60f);
            float r1 = (float)Math.Exp(Math.Log(OneHourAutocorrelationFactor) / k);
            float whiteNoise = WhiteNoise(0, 0.25f);
            float newValue = r1 * _lastDir + whiteNoise;

            float deg = newValue * 90;

            while (deg < 0)
                deg += 360;
            while (deg > 360)
                deg -= 360;

            _lastDir = newValue;
            return deg;
        }

        public float HourMeanWindSpeed(float hour) =>
            (float)(MeanWindSpeed *
                    (1 + DiurnalPatternStrength * Math.Cos(Math.PI / 12 * (hour - HourOfPeakWindSpeed))));

        public float WhiteNoise(float mean, float stdDev)
        {
            double u1 = 1.0 - random.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal =
                mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)

            return (float)randNormal;
        }

        private readonly Random random;
        private float _lastDir;

        private float _lastSpeed;
    }
}
#endif

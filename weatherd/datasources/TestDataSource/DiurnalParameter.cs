#if DEBUG
using System;
using Microsoft.Extensions.Configuration;

namespace weatherd.datasources.testdatasource
{
    internal record DiurnalParameter
    {
        public float Minimum => _min;
        public float Maximum => _max;
        public float Trough => _trough;
        public float Period => _period;
        public float Deviation => _deviation;

        private readonly float _min;
        private readonly float _max;
        private readonly float _trough;
        private readonly float _period;
        private readonly float _deviation;
        private readonly Random _random;

        public DiurnalParameter(float min, float max, float trough, float period, float deviation)
        {
            _min = min;
            _max = max;
            _trough = trough;
            _period = period;
            _deviation = deviation;
        }

        public DiurnalParameter()
        {
            _random = new Random();
        }

        public DiurnalParameter(IConfiguration config)
            : this()
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            _min = Utilities.TryGetConfigurationKey(config, "Min");
            _max = Utilities.TryGetConfigurationKey(config, "Max");
            _trough = Utilities.TryGetConfigurationKey(config, "Trough");
            _period = Utilities.TryGetConfigurationKey(config, "Period");
            _deviation = Utilities.TryGetConfigurationKey(config, "Deviation");
        }

        public DiurnalParameter(IConfiguration config, DiurnalParameter defaults)
            : this()
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!float.TryParse(config["Min"], out _min))
                _min = defaults._min;
            if (!float.TryParse(config["Max"], out _max))
                _max = defaults._max;
            if (!float.TryParse(config["Trough"], out _trough))
                _trough = defaults._trough;
            if (!float.TryParse(config["Period"], out _period))
                _period = defaults._period;
            if (!float.TryParse(config["Deviation"], out _deviation))
                _deviation = defaults._deviation;
        }
        public float Sample(DateTime time)
        {
            // Asin(B(x-D))+C
            // A = -(max - C)
            // B = pi/(period/2)
            // C = (max+min)/2
            // D = (H - 4)
            // D(minTime + n*period) = min
            // D((minTime + period/2) + n*period) = max
            
            float fractionalHour = DateTime.Now.Hour + DateTime.Now.Minute / 60.0f + DateTime.Now.Second / 3600.0f;
            float c = (_max + _min) / 2.0f;
            float a = -(_max - c);
            float b = (float) Math.PI / (_period/2);
            float d = fractionalHour - _trough;
            float e = (float) (_random.NextDouble() * _deviation * 2) - _deviation;

            return a * (float)Math.Cos(b * d) + c + e;
        }
    }
}
#endif
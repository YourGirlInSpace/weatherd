using System;

namespace weatherd.datasources
{
    public class WeatherDataEventArgs : EventArgs
    {
        public WeatherState Conditions { get; }

        public WeatherDataEventArgs(WeatherState conditions)
        {
            Conditions = conditions;
        }
    }
}

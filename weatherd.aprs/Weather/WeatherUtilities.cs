namespace weatherd.aprs.weather
{
    internal static class WeatherUtilities
    {
        internal static float CelsiusToFahrenheit(float celsius) => (celsius * 9.0f / 5.0f) + 32f;
    }
}

using System;

namespace weatherd.datasources
{
    public interface IWeatherDataSource : IDataSource
    {
        WeatherState Conditions { get; }
    }

    public interface IAsyncWeatherDataSource : IAsyncDataSource
    {
        WeatherState Conditions { get; }

        event EventHandler<WeatherDataEventArgs> SampleAvailable;
    }
}

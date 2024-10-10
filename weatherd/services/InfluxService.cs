using System;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace weatherd.services
{
    public interface IInfluxService
    {
        bool Initialized { get; }
        Task<bool> Initialize();
        Task Push(WeatherState state);
    }
    
    public sealed class InfluxService : IInfluxService, IDisposable
    {
        private InfluxDBClient _client;
        private readonly string _bucket = Environment.GetEnvironmentVariable("INFLUX_BUCKET")!;
        private readonly string _org = Environment.GetEnvironmentVariable("INFLUX_ORG")!;
        private readonly string _endpoint = Environment.GetEnvironmentVariable("INFLUX_ENDPOINT")!;

        public bool Initialized { get; private set; }
        
        public Task<bool> Initialize()
        {
            var token = Environment.GetEnvironmentVariable("INFLUX_TOKEN")!;
            
            _client = new InfluxDBClient(_endpoint, token);
            Initialized = true;

            return Task.FromResult(true);
        }

        public async Task Push(WeatherState state)
        {
            if (!Initialized)
                return;
            
            var point = PointData.Measurement("weather")
                .Tag("City", "Bremerton")
                .Tag("State", "WA")
                .Field("temperature", state.Temperature.DegreesCelsius)
                .Field("dewpoint", state.Dewpoint.DegreesCelsius)
                .Field("relative_humidity", state.RelativeHumidity.Percent)
                .Field("barometric_pressure", state.Pressure.Hectopascals)
                .Field("sea_leveL_pressure", state.SeaLevelPressure.Hectopascals)
                .Field("luminosity", state.Luminosity.WattsPerSquareMeter)
                .Field("wind_speed", state.WindSpeed.MetersPerSecond)
                .Field("wind_direction", state.WindDirection.Degrees)
                .Field("rainfall_since_midnight", state.RainfallSinceMidnight.Millimeters)
                .Field("snowfall_since_midnight", state.SnowfallSinceMidnight.Millimeters)
                .Field("visibility", state.Visibility.Meters)
                .Field("precipitation_intensity", state.WaterIntensity.MillimetersPerHour)
                .Field("batt_charge_current", state.BatteryChargeCurrent.Amperes)
                .Field("batt_drain_current", state.BatteryDrainCurrent.Amperes)
                .Field("batt_voltage", state.BatteryVoltage.VoltsDc)
                .Field("enclosure_temperature", state.EnclosureTemperature.DegreesCelsius)
                .Timestamp(DateTime.UtcNow, WritePrecision.S);

            var writeApi = _client.GetWriteApiAsync();
            await writeApi.WritePointAsync(point, bucket: _bucket, org: _org);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
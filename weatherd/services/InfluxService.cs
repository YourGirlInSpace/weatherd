using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Configuration;
using Serilog;

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
        private string _bucket;
        private string _org ;
        private string _endpoint;
        private string _token;

        private bool _enableDataWrite = true;
        private InfluxRecordDefinition[] _recordDefinitions;

        public bool Initialized { get; private set; }

        public InfluxService(IConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            LoadConfig(config);
        }
        
        public Task<bool> Initialize()
        {
            _client = new InfluxDBClient(_endpoint, _token);
            Initialized = true;

            return Task.FromResult(true);
        }

        private void LoadConfig(IConfiguration config)
        {
            IConfigurationSection ifxConfig = config.GetSection("InfluxService");
            if (ifxConfig is null)
                throw new StationConfigurationException(
                    $"Failed to load configuration for {nameof(InfluxService)}:  'InfluxService' is missing.");
            
            _enableDataWrite = ifxConfig.GetValue("EnableDataWrite", true);
            _bucket = ifxConfig.GetValue("Bucket", string.Empty);
            _org = ifxConfig.GetValue("Organization", string.Empty);
            _endpoint = ifxConfig.GetValue("Endpoint", string.Empty);
            _token = ifxConfig.GetValue("Token", string.Empty);
            
            _recordDefinitions = Utilities.GetConfigurationArray<InfluxRecordDefinition>(ifxConfig.GetSection("Records")).ToArray();
            
            // We cannot actually sync data without record definitions, so ...
            if (_enableDataWrite && _recordDefinitions.Length == 0)
                throw new InvalidOperationException(
                    "Cannot sync to Influx without record definitions in configuration.");
        }

        public async Task Push(WeatherState state)
        {
            if (!Initialized)
                return;

            if (state == null)
            {
                Log.Error("Failed to push Influx data: state was null");
                return;
            }
            
            var point = PointData.Measurement("weather")
                .Tag("City", "Bremerton")
                .Tag("State", "WA")
                .Timestamp(DateTime.UtcNow, WritePrecision.S);

            if (state.Weather != null)
            {
                point = point.Tag("Weather", state.Weather.ToString());
            }

            foreach (InfluxRecordDefinition defn in _recordDefinitions)
            {
                try
                {
                    var value = GetProperty(state, defn.Property, defn.Unit);

                    if (value == null)
                        continue;

                    point = point.Field(defn.Name, value);
                } catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to find property {Property} with unit {Unit} in wxstate", defn.Property, defn.Unit);
                }
            }

            var writeApi = _client.GetWriteApiAsync();
            await writeApi.WritePointAsync(point, bucket: _bucket, org: _org);
        }
        
        public static object GetProperty(WeatherState wxState, string propertyName, string unitName)
        {
            Type weatherStateType = wxState.GetType();

            PropertyInfo propInfo = weatherStateType.GetProperty(propertyName);
            if (propInfo is null)
                throw new InvalidOperationException($"Could not find meteorological property '{propertyName}'");

            object value = propInfo.GetValue(wxState);
            if (value is null)
                throw new InvalidOperationException($"Could not resolve '{propertyName}' into a value.");

            if (string.IsNullOrEmpty(unitName))
                return value;

            Type valueType = value.GetType();

            PropertyInfo unitPropInfo = valueType.GetProperty(unitName);
            if (unitPropInfo is null)
                throw new InvalidOperationException($"Could not find unit '{unitName}'");
            
            return unitPropInfo.GetValue(value);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
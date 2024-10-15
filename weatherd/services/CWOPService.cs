using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using weatherd.aprs;
using weatherd.aprs.telemetry;
using weatherd.aprs.telemetry.metrics;
using weatherd.aprs.weather;

namespace weatherd.services
{
    public interface ICWOPService
    {
        bool Enabled { get; }
        DateTime LastSendTime { get; }
        DateTime NextSendTime { get; }
        bool LastSendOK { get; }

        Task<bool> SendData(APRSISClient client, APRSMessage data);

        Task<bool> SendCWOP(WeatherState wxState);
    }

    public class CWOPService : ICWOPService
    {
        public bool Enabled { get; private set; }

        /// <inheritdoc />
        public DateTime LastSendTime { get; private set; }

        /// <inheritdoc />
        public DateTime NextSendTime { get; private set; }

        /// <inheritdoc />
        public bool LastSendOK { get; private set; }

        public float Latitude { get; private set; }
        public float Longitude { get; private set; }
        public string Callsign { get; private set; }
        public string Equipment { get; private set; }
        
        private const int MetricVisibility = 0;
        private const int MetricBatteryVoltage = 1;
        private const int MetricDrainCurrent = 2;
        private const int MetricChargeCurrent = 3;
        private const int MetricRainRate = 4;
        private const int MetricRFEnable = 0x1;
        private const int MetricOnACPower = 0x2;

        private MetricSet _metricSet = new MetricSet(new[]
            {
                new AnalogTelemetryMetric("Visblty", "deg.C", 0, 10f, 0), // 255×10 = 2550m
                new AnalogTelemetryMetric("BattV", "V", 0, 0.075f, 0), // 255×0.075 = 19.125V
                new AnalogTelemetryMetric("DrainI", "mA", 0, 4f, 0), // 255×4 = 1020mA
                new AnalogTelemetryMetric("ChgI", "mA", 0, 4f, 0), // 255x4 = 1020mA
                new AnalogTelemetryMetric("RainRt", "mm/h", 0, 1f, 0), // 255x1 = 255mm/h
            },
            new[]
            {
                new BinaryTelemetryMetric("RFEnbl"), // 1 = emitting CWOP data via 144.390 MHz
                new BinaryTelemetryMetric("ACPwr") // 1 = AC power is being provided
            });

        private TelemetryValueMessage _valueMessage;
        private TelemetryUnitMessage _unitMessage;
        private TelemetryEquationsMessage _equationsMessage;
        private TelemetryParameterMessage _parameterMessage;
        private DateTime _lastUnitSendTime = DateTime.MinValue;

        public CWOPService(IConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            IConfigurationSection tsConfig = config.GetSection("CWOPService");

            Enabled = tsConfig.GetValue("Enabled", false);
            Latitude = tsConfig.GetValue("Latitude", float.NaN);
            Longitude = tsConfig.GetValue("Longitude", float.NaN);
            Callsign = tsConfig.GetValue("Callsign", string.Empty);
            Equipment = tsConfig.GetValue("Equipment", string.Empty);
            NextSendTime = DateTime.Now.AddMinutes(5);

            if (!Enabled)
                return;

            LoadConfig(config);
            ValidateConfig();

            _valueMessage = new TelemetryValueMessage(Callsign, Equipment, _metricSet);
            _unitMessage = new TelemetryUnitMessage(_valueMessage);
            _equationsMessage = new TelemetryEquationsMessage(_valueMessage);
            _parameterMessage = new TelemetryParameterMessage(_valueMessage);
        }

        private void LoadConfig(IConfiguration config)
        {
            IConfigurationSection cwopConfig = config.GetSection("CWOPService");
            IConfigurationSection siteConfig = config.GetSection("Site");

            if (cwopConfig is null)
                throw new StationConfigurationException(
                    $"Failed to load configuration for {nameof(CWOPService)}:  'CWOPService' is missing.");
            if (siteConfig is null)
                throw new StationConfigurationException(
                    $"Failed to load configuration for {nameof(CWOPService)}:  'Site' is missing.");

            Enabled = cwopConfig.GetValue("Enabled", false);
            Callsign = cwopConfig.GetValue("Callsign", string.Empty);
            Equipment = cwopConfig.GetValue("Equipment", string.Empty);

            Latitude = siteConfig.GetValue("Latitude", float.NaN);
            Longitude = siteConfig.GetValue("Longitude", float.NaN);
        }

        private void ValidateConfig()
        {
            if (float.IsNaN(Latitude) || float.IsNaN(Longitude))
            {
                Log.Error("Cannot enable CWOP service:  Location not defined");
                Enabled = false;
            }

            if (string.IsNullOrWhiteSpace(Callsign))
            {
                Log.Error("Cannot enable CWOP service:  Callsign not defined");
                Enabled = false;
            }

            if (string.IsNullOrWhiteSpace(Equipment))
                Log.Warning("CWOP service has no equipment type defined");
        }

        public async Task<bool> SendCWOP(WeatherState wxState)
        {
            if ((DateTime.Now - NextSendTime).TotalMilliseconds > 0)
                return true;

            if (!Enabled)
                return true;
            
            // Telemetry data is needed
            MetricSet metricSet = new MetricSet();

            Log.Information("[{Service}] Beginning APRS transmission", nameof(CWOPService));

            WeatherConditions wxConditions = new()
            {
                ReportTime = DateTime.UtcNow,
                Latitude = Latitude,
                Longitude = Longitude,

                WindDirection = wxState.WindDirection,
                WindSpeed = wxState.WindSpeed,
                Gust = wxState.WindGust5Minute,

                Temperature = wxState.Temperature,
                Humidity = wxState.RelativeHumidity,
                Pressure = wxState.SeaLevelPressure,

                Luminosity = wxState.Luminosity,
                RainfallSinceMidnight = wxState.OpticalRainfallSinceMidnight
            };

            if (wxConditions.Gust?.Value < 0)
                wxConditions.Gust = Speed.Zero;

            WeatherReportMessage wrm = new(Callsign, wxConditions)
            {
                Comment = wxState.ToMETAR(Callsign)
            };

            using var client = await Login();
            if (client == null)
                return false;

            Log.Information("[{Service}] Sending weather data...", nameof(CWOPService));
            if (!await SendData(client, wrm))
                return false;
            
            // Telemetry?
            _valueMessage.SetValue(MetricVisibility, (float) wxState.Visibility.Meters);
            _valueMessage.SetValue(MetricBatteryVoltage, (float) wxState.BatteryVoltage.VoltsDc);
            _valueMessage.SetValue(MetricDrainCurrent, (float) wxState.BatteryDrainCurrent.Milliamperes);
            _valueMessage.SetValue(MetricChargeCurrent, (float) wxState.BatteryChargeCurrent.Milliamperes);
            _valueMessage.SetValue(MetricRainRate, (float) wxState.WaterIntensity.MillimetersPerHour);
            
            _valueMessage.SetFlag(MetricRFEnable, false);
            _valueMessage.SetFlag(MetricOnACPower, wxState.BatteryChargeCurrent.Milliamperes > 10);
            
            // Before we continue, should we send the units again?
            if (DateTime.UtcNow - _lastUnitSendTime > TimeSpan.FromHours(1))
            {
                Log.Information("[{Service}] Sending telemetry metadata...", nameof(CWOPService));
                await SendData(client, _parameterMessage);
                await SendData(client, _unitMessage);
                await SendData(client, _equationsMessage);
                _lastUnitSendTime = DateTime.UtcNow;
            }

            Log.Information("[{Service}] Sending telemetry...", nameof(CWOPService));
            return await SendData(client, _valueMessage);
        }

        private async Task<APRSISClient> Login()
        {
            APRSISClient client = new(APRSISClient.CWOP, APRSISClient.DefaultPort);

            if (!await client.Connect())
            {
                Log.Warning("[{Service}] Failed to connect", nameof(CWOPService));
                return null;
            }

            if (await client.Login(Callsign, APRSISClient.CWOPSend, Equipment))
                return client;
            
            Log.Warning("[{Service}] Failed to log in", nameof(CWOPService));
            return null;

        }

        /// <inheritdoc />
        public async Task<bool> SendData(APRSISClient client, APRSMessage data)
        {
            try
            {
                await client.SendCommand(data);

                LastSendOK = true;
                LastSendTime = DateTime.Now;

                return true;
            } catch (Exception ex)
            {
                Log.Warning(ex, "[{Service}] Failed to send APRS message", nameof(CWOPService));
                // derp!
                LastSendOK = false;
                return false;
            }
        }
    }
}

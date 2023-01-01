using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using weatherd.aprs;
using weatherd.aprs.weather;

namespace weatherd.services
{
    public interface ICWOPService
    {
        bool Enabled { get; }
        DateTime LastSendTime { get; }
        DateTime NextSendTime { get; }
        bool LastSendOK { get; }

        Task<bool> SendData(APRSMessage data);

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

            if (Enabled)
                return;

            LoadConfig(config);
            ValidateConfig();
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
                Log.Error("Cannot enable CWOP service:  Location not defined.");
                Enabled = false;
            }

            if (string.IsNullOrWhiteSpace(Callsign))
            {
                Log.Error("Cannot enable CWOP service:  Callsign not defined.");
                Enabled = false;
            }

            if (string.IsNullOrWhiteSpace(Equipment))
                Log.Warning("CWOP service has no equipment type defined.");
        }

        public async Task<bool> SendCWOP(WeatherState wxState)
        {
            if ((DateTime.Now - NextSendTime).TotalMilliseconds > 0)
                return true;

            if (!Enabled)
                return true;

            Log.Information($"[{nameof(CWOPService)}] Sending information...");

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
                EquipmentType = Equipment
            };

            return await SendData(wrm);
        }

        /// <inheritdoc />
        public async Task<bool> SendData(APRSMessage data)
        {
            try
            {
                APRSISClient client = new(APRSISClient.CWOP, APRSISClient.DefaultPort);

                if (!await client.Connect())
                {
                    Log.Warning($"[{nameof(CWOPService)}] Failed to connect.");
                    return false;
                }

                if (!await client.Login(data.SourceCallsign, APRSISClient.CWOPSend, Equipment))
                {
                    Log.Warning($"[{nameof(CWOPService)}] Failed to log in.");
                    return false;
                }

                await client.SendCommand(data);

                LastSendOK = true;
                LastSendTime = DateTime.Now;

                return true;
            } catch (Exception ex)
            {
                Log.Debug(ex, "Failed to send APRS message.");
                // derp!
                LastSendOK = false;
                return false;
            }
        }
    }
}

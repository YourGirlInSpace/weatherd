using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using Prometheus;
using Serilog;

namespace weatherd.services
{
    public interface IPrometheusService
    {
        bool Initialized { get; }
        Task<bool> Initialize();
        Task Push(WeatherState state);
    }
    
    public class PrometheusService : IPrometheusService
    {
        private class MetricRegistration
        {
            private readonly Func<WeatherState, double> _valueFunc;
            private readonly Gauge _gauge;

            public MetricRegistration(string name, string help, Func<WeatherState, double> valueFunc)
            {
                _valueFunc = valueFunc;
                _gauge = Metrics.CreateGauge(name, help);
            }

            public void Update(WeatherState wxState)
            {
                _gauge.Set(_valueFunc(wxState));
            }
        }
        
        public bool Initialized { get; private set; }

        private readonly List<MetricRegistration> _registrations = new();
        
        public Task<bool> Initialize()
        {
            // Register the relevant metrics...
            Register("stn_local_temperature", "Local station temperature in °C", wx => wx.Temperature.DegreesCelsius);
            Register("stn_local_dewpoint", "Local station dewpoint in °C", wx => wx.Dewpoint.DegreesCelsius);
            Register("stn_local_relative_humidity", "Local station relative humidity in %", wx => wx.RelativeHumidity.Percent);
            Register("stn_local_barometric_pressure", "Local station barometric pressure in hPa", wx => wx.Pressure.Hectopascals);
            Register("stn_local_sea_leveL_pressure", "Local station sea level pressure in hPa", wx => wx.SeaLevelPressure.Hectopascals);
            Register("stn_local_luminosity", "Local station luminosity in W/m²", wx => wx.Luminosity.WattsPerSquareMeter);
            Register("stn_local_wind_speed", "Local station wind speed in meters per second", wx => wx.WindSpeed.MetersPerSecond);
            Register("stn_local_wind_direction", "Local station wind direction in degrees clockwise from true North", wx => wx.WindDirection.Degrees);
            Register("stn_local_rainfall_since_midnight", "Local station rainfall since local midnight in mm", wx => wx.RainfallSinceMidnight.Millimeters);
            Register("stn_local_snowfall_since_midnight", "Local station snowfall since local midnight in mm", wx => wx.SnowfallSinceMidnight.Millimeters);
            Register("stn_local_visibility", "Local station visibility in meters", wx => wx.Visibility.Meters);
            Register("stn_precipitation_intensity", "Local station precipitation intensity in millimeters per hour", wx => wx.WaterIntensity.MillimetersPerHour);
            Register("stn_batt_charge_current", "Local station battery charge current in amperes", wx => wx.BatteryChargeCurrent.Amperes);
            Register("stn_batt_drain_current", "Local station battery drain current in amperes", wx => wx.BatteryDrainCurrent.Amperes);
            Register("stn_batt_voltage", "Local station battery voltage in volts", wx => wx.BatteryVoltage.VoltsDc);
            Register("stn_enclosure_temperature", "Local station enclosure temperature in °C", wx => wx.EnclosureTemperature.DegreesCelsius);
            
            var metricServer = new MetricServer(port: 1234);

            try
            {
                metricServer.Start();
            }
            catch (HttpListenerException hle)
            {
                Log.Error(hle, "Failed to start metric server");
            }

            Initialized = true;

            return Task.FromResult(true);
        }

        private void Register(string name, string help, Func<WeatherState, double> valueFunc)
        {
            _registrations.Add(new MetricRegistration(name, help, valueFunc));
        }

        public Task Push(WeatherState state)
        {
            _registrations.ForEach(reg => reg.Update(state));

            return Task.CompletedTask;
        }
    }
}
using System;
using UnitsNet;

namespace weatherd.aprs.weather
{
    public class WeatherConditions
    {
        /// <summary>
        /// The report time in UTC
        /// </summary>
        public DateTime? ReportTime { get; set; }

        /// <summary>
        /// The latitude of the weather station
        /// </summary>
        public float? Latitude { get; set; }

        /// <summary>
        /// The longitude of the weather station
        /// </summary>
        public float? Longitude { get; set; }

        /// <summary>
        /// One minute average wind speed in degrees
        /// </summary>
        public Angle? WindDirection { get; set; }

        /// <summary>
        /// Sustained one-minute wind speed in meters per second
        /// </summary>
        public Speed? WindSpeed { get; set; }

        /// <summary>
        /// Peak wind speed in meters per second in the last 5 minutes
        /// </summary>
        public Speed? Gust { get; set; }

        /// <summary>
        /// Temperature (in degrees Celsius).
        /// </summary>
        public Temperature? Temperature { get; set; }

        /// <summary>
        /// Rainfall (in millimeters) in the last hour.
        /// </summary>
        public Length? Rainfall1Hour { get; set; }

        /// <summary>
        /// Rainfall (in millimeters) in the last 24 hours.
        /// </summary>
        public Length? Rainfall24Hours { get; set; }

        /// <summary>
        /// Rainfall (in millimeters) since midnight local time.
        /// </summary>
        public Length? RainfallSinceMidnight { get; set; }

        /// <summary>
        /// Humidity in %, ranging from 0 to 100.
        /// </summary>
        public RelativeHumidity? Humidity { get; set; }

        /// <summary>
        /// Barometric pressure in hectopascals
        /// </summary>
        public Pressure? Pressure { get; set; }

        /// <summary>
        /// In watts per square meter
        /// </summary>
        public Irradiance? Luminosity { get; set; }

        /// <summary>
        /// Snowfall (in millimeters) in the past 24 hours.
        /// </summary>
        public Length? Snowfall { get; set; }

        public int? RawRainCounter { get; set; }
    }
}

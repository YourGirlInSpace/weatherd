using System;
using System.Diagnostics;
using System.Text;

namespace weatherd.aprs.weather
{
    public class WeatherReportMessage : APRSISMessage
    {
        // Symbol ID /_ = Weather station (blue)
        private const string DefaultSymbol = "/_";
        private const char CompressionType = 'C';
        
        /// <summary>
        /// The conditions to report.
        /// </summary>
        public WeatherConditions Conditions { get; }
        /// <summary>
        /// Should we compress the position, wind direction and wind speed fields?
        /// </summary>
        public bool Compressed { get; }
        /// <summary>
        /// The equipment type to report.
        /// </summary>
        public string EquipmentType { get; set; }

        /// <summary>
        /// The APRS symbol for this station.
        /// </summary>
        /// <remarks>
        ///     The following symbols may be used:
        ///     <code>
        ///       ● /W = National Weather Service Site
        ///       ● \W = NWS Site (with overlay)
        ///       ● /_ = WX Station (blue)
        ///       ● \_ = WX Station with digipeater (with overlay) (green)
        ///       ● \D = Drizzle
        ///       ● \E = Smoke
        ///       ● \F = Freezing Rain
        ///       ● \G = Snow Shower
        ///       ● \H = Haze
        ///       ● \I = Rain Shower
        ///       ● \J = Lightning
        ///       ● \T = Thunderstorm
        ///       ● \U = Sunny
        ///       ● \[ = Wall Cloud
        ///       ● \` = Rain
        ///       ● \b = Blowing Dust/Sand
        ///       ● \e = Sleet
        ///       ● \f = Funnel Cloud
        ///       ● \g = Gale Flags
        ///       ● \p = Partly Cloudy
        ///       ● \t = Tornado
        ///       ● \w = Flooding
        ///       ● \{ = Fog
        ///       ● \( = Cloudy
        ///       ● \* = Snow
        ///       ● \: = Hail
        ///       ● \&lt; = NWS Advisory (Gale flag)
        ///       ● \@ = Hurricane/Tropical Storm
        ///       ● \B = Blowing Snow
        ///     </code>
        /// </remarks>
        public string Symbol { get; set; } = DefaultSymbol;

        /// <inheritdoc />
        public WeatherReportMessage(string sourceCallsign, WeatherConditions wxConditions)
            : base(sourceCallsign, "APRS")
        {
            Conditions = wxConditions ?? throw new ArgumentNullException(nameof(wxConditions));
            Compressed = false;
        }

        /// <inheritdoc />
        public WeatherReportMessage(string sourceCallsign, WeatherConditions wxConditions, bool compressed)
            : base(sourceCallsign, "APRS")
        {
            Conditions = wxConditions ?? throw new ArgumentNullException(nameof(wxConditions));
            Compressed = compressed;
        }

        /// <inheritdoc />
        public WeatherReportMessage(string sourceCallsign, WeatherConditions wxConditions, string symbol)
            : base(sourceCallsign, "APRS")
        {
            Conditions = wxConditions ?? throw new ArgumentNullException(nameof(wxConditions));
            Compressed = false;
            Symbol = symbol;
        }

        /// <inheritdoc />
        public WeatherReportMessage(string sourceCallsign, WeatherConditions wxConditions, string symbol, bool compressed)
            : base(sourceCallsign, "APRS")
        {
            Conditions = wxConditions ?? throw new ArgumentNullException(nameof(wxConditions));
            Compressed = compressed;
            Symbol = symbol;
        }

        /// <inheritdoc />
        public override string Compile()
        {
            bool hasTime = Conditions.ReportTime.HasValue;
            bool hasLocation = Conditions.Latitude.HasValue && Conditions.Longitude.HasValue;

            if (hasLocation && !hasTime)
                TypeIdentifier = "!";
            if (hasLocation && hasTime)
                TypeIdentifier = "/";
            
            StringBuilder packetBuilder = new StringBuilder();
            packetBuilder.Append(base.Compile());
            packetBuilder.Append(TypeIdentifier);

            if (hasTime)
                packetBuilder.AppendFormat("{0:ddHHmm}z", Conditions.ReportTime.Value.ToUniversalTime());
            if (Compressed)
                BuildCompressedHeader(packetBuilder, hasLocation);
            else
                BuildUncompressedHeader(packetBuilder, hasLocation);
            
            if (Conditions.Temperature.HasValue)
            {
                // Temperatures less than 0°F are represented as "t-##".  Greater than
                // 0°F is represented as "t###".
                packetBuilder.Append(Conditions.Temperature.Value.DegreesFahrenheit < 0
                                         ? $"t{Conditions.Temperature.Value.DegreesFahrenheit:00}"
                                         : $"t{Conditions.Temperature.Value.DegreesFahrenheit:000}");
            } else
                packetBuilder.Append("t...");
            
            // Rainfall is all reported in hundredths of an inch (0.01in)
            if (Conditions.Rainfall1Hour.HasValue)
                packetBuilder.Append($"r{Conditions.Rainfall1Hour.Value.Inches * 100:000}");
            // Rainfall in the last 24 hours is a rolling average.
            if (Conditions.Rainfall24Hours.HasValue)
                packetBuilder.Append($"p{Conditions.Rainfall24Hours.Value.Inches * 100:000}");
            // Rainfall since midnight is the total accumulated rain since midnight local time.
            if (Conditions.RainfallSinceMidnight.HasValue)
                packetBuilder.Append($"P{Conditions.RainfallSinceMidnight.Value.Inches * 100:000}");

            // If the humidity is 100% or supersaturated, it is reported as h00.  Otherwise, h##, where ## is the percent value.
            if (Conditions.Humidity.HasValue)
                packetBuilder.Append(Conditions.Humidity.Value.Percent >= 100 ? "h00" : $"h{Conditions.Humidity.Value.Percent:00}");
            // Pressure is reported in tenths of a hectopascal.
            if (Conditions.Pressure.HasValue)
                packetBuilder.Append($"b{Conditions.Pressure.Value.Hectopascals * 10:00000}");

            // Luminosity has a special format.
            // If the luminosity is below 1000, it is prefixed with 'L'.
            // If the luminosity is above 1000, it is truncated to 3 digits and prefixed with 'l'.
            var wsm = Conditions.Luminosity?.WattsPerSquareMeter;
            if (wsm <= 999)
                packetBuilder.Append($"L{Math.Min(wsm.Value, 999):000}");
            if (wsm >= 1000)
                packetBuilder.Append($"l{Math.Min(wsm.Value - 1000, 999):000}");

            // Snowfall has a special format.
            // The format is #.# for any value below 10 inches, and
            // ### for any value above 10 inches.
            if (Conditions.Snowfall.HasValue)
            {
                float snowfallInInches = (float) Math.Round(Conditions.Snowfall.Value.Inches, 1);
                packetBuilder.Append(snowfallInInches < 10 ? $"s{snowfallInInches:0.0}" : $"s{snowfallInInches:000}");
            }

            // NOTE:  I purposefully omitted the # (raw rain counter), F (water height in feet) and f (water height in meters)
            //        because they have little to no purpose in this message.

            if (!string.IsNullOrEmpty(EquipmentType))
                packetBuilder.Append($"e{EquipmentType}");

            return packetBuilder.ToString();
        }

        private void BuildUncompressedHeader(StringBuilder packetBuilder, bool hasLocation)
        {
            if (hasLocation)
            {
                Debug.Assert(Conditions.Latitude != null, "Conditions.Latitude != null");
                Debug.Assert(Conditions.Longitude != null, "Conditions.Longitude != null");

                string latNS = Conditions.Latitude.Value > 0 ? "N" : "S";
                string lonEW = Conditions.Longitude.Value > 0 ? "E" : "W";

                float lat_degrees = (float)Math.Floor(Math.Abs(Conditions.Latitude.Value));
                float lon_degrees = (float)Math.Floor(Math.Abs(Conditions.Longitude.Value));

                float lat_minutes = (Math.Abs(Conditions.Latitude.Value) - lat_degrees) * 60;
                float lon_minutes = (Math.Abs(Conditions.Longitude.Value) - lon_degrees) * 60;

                packetBuilder.AppendFormat("{0:00}{1:00.00}{2}{3}{4:000}{5:00.00}{6}",
                                           lat_degrees, lat_minutes, latNS, Symbol[0], lon_degrees, lon_minutes, lonEW);
            }

            packetBuilder.Append(Symbol[1]);
            packetBuilder.Append(Conditions.WindDirection.HasValue ? $"{Conditions.WindDirection.Value.Degrees:000}" : "...");
            packetBuilder.Append("/");
            // Wind speed is a one minute sustained wind speed.
            packetBuilder.Append(Conditions.WindSpeed.HasValue ? $"{Conditions.WindSpeed.Value.MilesPerHour:000}" : "...");
            // Wind gust is the maximum instantaneous wind speed recorded in the past 5 minutes.
            packetBuilder.Append(Conditions.Gust.HasValue ? $"g{Conditions.Gust.Value.MilesPerHour:000}" : "g...");
        }

        private void BuildCompressedHeader(StringBuilder packetBuilder, bool hasLocation)
        {
            if (!Conditions.WindDirection.HasValue || !Conditions.WindSpeed.HasValue)
                throw new InvalidOperationException(
                    "Cannot compress an APRS weather report message without a valid wind direction and wind speed.");

            if (hasLocation)
            {
                Debug.Assert(Conditions.Latitude != null, "Conditions.Latitude != null");
                Debug.Assert(Conditions.Longitude != null, "Conditions.Longitude != null");

                string compressedLat = APRSCompression.CompressLatitude(Conditions.Latitude.Value);
                string compressedLon = APRSCompression.CompressLongitude(Conditions.Longitude.Value);
                packetBuilder.AppendFormat("{0}{1}{2}{3}", Symbol[0], compressedLat, compressedLon, Symbol[1]);
            }

            string compressedDirSpeed =
                APRSCompression.CompressCourseSpeed((float)Conditions.WindDirection.Value.Degrees,
                                                    (float)Conditions.WindSpeed.Value.MilesPerHour);

            packetBuilder.AppendFormat("{0}{1}", compressedDirSpeed, CompressionType);

            // Wind gust is the maximum instantaneous wind speed recorded in the past 5 minutes.
            packetBuilder.Append(Conditions.Gust.HasValue ? $"g{Conditions.Gust.Value.MilesPerHour:000}" : "g...");
        }
    }
}

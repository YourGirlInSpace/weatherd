using System;
using System.Text;
using UnitsNet;
using UnitsNet.Units;
using static System.Math;

namespace weatherd
{
    public class WeatherState
    {
        /// <summary>
        /// Specific gas constant of water vapor
        /// </summary>
        public const double ℜv = 461.495;
        /// <summary>
        /// Specific gas constant of dry air
        /// </summary>
        public const double ℜd = 287.058;
        /// <summary>
        /// Standard lapse rate
        /// </summary>
        public const double γstd = 0.0065;
        /// <summary>
        /// Molar mass of dry air
        /// </summary>
        public const double Md = 0.028964;
        /// <summary>
        /// Universal gas constant
        /// </summary>
        public const double ℜ = 8.314450948;
        /// <summary>
        /// Sea level standard pressure
        /// </summary>
        public const double Pstd = 1013.25;
        /// <summary>
        /// Sea level standard temperature
        /// </summary>
        public const double Tstd = 288.15;
        /// <summary>
        /// Acceleration due to gravity
        /// </summary>
        public const double g = 9.80665;
        /// <summary>
        /// Gas constant ratio for dry air/water vapor
        /// </summary>
        public const double ε = ℜd / ℜv;
        /// <summary>
        /// Vapor pressure @ 0°C
        /// </summary>
        public const double e0 = 0.6113;
        /// <summary>
        /// 0 °C in Kelvin
        /// </summary>
        /// <remarks>To convert a temperature which is defined in degrees Celsius to degrees Kelvin, simply add <see cref="T0"/>.
        /// Perform the inverse to convert back to degrees Celsius.</remarks>
        public const double T0 = 273.15;
        /// <summary>
        /// Latent heat of vaporization at 0°C
        /// </summary>
        public const double Lv = 2.501e6;
        /// <summary>
        /// Latent heat of fusion at 0°C
        /// </summary>
        public const double Lf = 3.337e5;
        /// <summary>
        /// Latent heat of deposition at 0°C
        /// </summary>
        public const double Ld = 2.834e6;
        /// <summary>
        /// Average radius of earth
        /// </summary>
        public const double Rearth = 6356766; // m
        /// <summary>
        /// Dry air specific heat at constant pressure
        /// </summary>
        public const double Cpd = 1004;

        /// <summary>
        /// Time when the observation was made.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Ambient station temperature in °C
        /// </summary>
        public Temperature Temperature { get; set; }

        /// <summary>
        /// Ambient station pressure in hPa
        /// </summary>
        public Pressure Pressure { get; set; }

        /// <summary>
        /// Station elevation in meters
        /// </summary>
        public Length Elevation { get; set; }
            = new(0, LengthUnit.Meter);

        /// <summary>
        /// Sea level pressure in hPa
        /// </summary>
        public Pressure SeaLevelPressure => new(Pressure.Pascals * Pow(1 - γstd * Elevation.Meters / (Temperature.DegreesCelsius + γstd * Elevation.Meters + T0), -(g/(ℜd*γstd))), PressureUnit.Pascal);

        /// <summary>
        /// Wind speed from a standard 10m anemometer, in m/s
        /// </summary>
        public Speed WindSpeed { get; set; }

        /// <summary>
        /// Wind direction from a standard 10m anemometer, in degrees true
        /// </summary>
        public Angle WindDirection { get; set; }

        /// <summary>
        /// Solar luminosity in W/m²
        /// </summary>
        public Irradiance Luminosity { get; set; }

        /// <summary>
        /// Total rainfall in the last hour, in inches
        /// </summary>
        public Length RainfallLastHour { get; set; }

        /// <summary>
        /// Total rainfall in the last 24 hours, in inches
        /// </summary>
        public Length RainfallLast24Hours { get; set; }

        /// <summary>
        /// Total rainfall since midnight local time, in inches
        /// </summary>
        public Length RainfallSinceMidnight { get; set; }

        /// <summary>
        /// Total rainfall since midnight local time, in millimeters
        /// </summary>
        public Length SnowfallSinceMidnight { get; set; }

        /// <summary>
        /// The total rainfall rate in mm/h
        /// </summary>
        public Speed InstantaneousRainRate { get; set; }

        /// <summary>
        /// Highest sustained wind gust over 5 minutes, in m/s
        /// </summary>
        public Speed WindGust5Minute { get; set; }

        /// <summary>
        /// Highest sustained wind gust over 30 minutes, in m/s
        /// </summary>
        public Speed WindGust30Minute { get; set; }

        /// <summary>
        /// Highest sustained wind gust over 1 hour, in m/s
        /// </summary>
        public Speed WindGust1Hour { get; set; }

        /// <summary>
        /// Battery voltage
        /// </summary>
        public ElectricPotentialDc BatteryVoltage { get; set; }

        /// <summary>
        /// Station enclosure temperature in °C
        /// </summary>
        public Temperature EnclosureTemperature { get; set; }

        /// <summary>
        /// The visibility in meters
        /// </summary>
        public Length Visibility { get; set; }

        /// <summary>
        /// The current precipitation/obscuration conditions
        /// </summary>
        public WeatherCode Weather { get; set; } = WeatherCode.Unknown;

        /// <summary>
        /// The precipitation water intensity in mm/h
        /// </summary>
        public Speed WaterIntensity { get; set; }

        /// <summary>
        /// Ambient station dewpoint in °C
        /// </summary>
        /// <remarks>Can be set directly or derived from temperature and relative humidity.</remarks>
        /// <exception cref="InsufficientMeteorologicalInformationException">If the humidity or temperature has not been set.</exception>
        public Temperature Dewpoint
        {
            get
            {
                double Gamma(Temperature t, RelativeHumidity rv)
                    => Log(rv.Percent/100.0) + 17.5 * t.DegreesCelsius / (237.3 + t.DegreesCelsius);

                if (_suppliedHumidity)
                {

                    _dewpoint = new Temperature(237.3 * Gamma(Temperature, RelativeHumidity) / (17.5 - Gamma(Temperature, RelativeHumidity)), TemperatureUnit.DegreeCelsius);
                    return _dewpoint;
                }

                if (_dewpoint > Temperature.Zero)
                    return _dewpoint;

                throw new InsufficientMeteorologicalInformationException(
                    "Insufficient data:  Humidity and Temperature required for Dewpoint calculation.");
            }

            set
            {
                _suppliedHumidity = false;
                _dewpoint = value;
            }
        }

        /// <summary>
        /// Relative Humidity in %
        /// </summary>
        /// <remarks><para>Relative humidity here is shown as a percentage from 0.0 to 1.0.  It can be set directly or derived from the dewpoint.</para>
        /// <para>In order to derive humidity, <see cref="Temperature">Temperature</see>, <see cref="Dewpoint">Dewpoint</see> and <see cref="Pressure">Station Pressure</see> are required.</para></remarks>
        /// <exception cref="InsufficientMeteorologicalInformationException">If the dewpoint has not been set.</exception>
        public RelativeHumidity RelativeHumidity
        {
            get
            {
                if (_suppliedHumidity && _humidity > RelativeHumidity.Zero)
                    return _humidity;

                if (_dewpoint == Temperature.Zero)
                    throw new InsufficientMeteorologicalInformationException("Insufficient data:  Dewpoint required for Humidity calculation.");

                return new RelativeHumidity(MixingRatio / SaturationMixingRatio * 100, RelativeHumidityUnit.Percent);
            }

            set
            {
                _suppliedHumidity = true;
                _humidity = value;
            }
        }

        /// <summary>
        /// Dewpoint depression, in °C
        /// </summary>
        public TemperatureDelta DewpointDepression => Temperature - Dewpoint;

        /// <summary>
        /// Vapor pressure, in hPa
        /// </summary>
        public Pressure VaporPressure => new(e0 * Exp(Lv / ℜv * (1 / T0 - 1 / Dewpoint.Kelvins)) * 10, PressureUnit.Hectopascal);

        /// <summary>
        /// Saturation vapor pressure, in hPa
        /// </summary>
        public Pressure SaturationVaporPressure => new(e0 * Exp(Lv / ℜv * (1 / T0 - 1 / Temperature.Kelvins)) * 10, PressureUnit.Hectopascal);

        /// <summary>
        /// Partial pressure of dry air, in hPa
        /// </summary>
        public Pressure PartialPressureDryAir => Pressure - VaporPressure;

        /// <summary>
        /// Mixing ratio, in g/kg
        /// </summary>
        public double MixingRatio => ε * VaporPressure.Hectopascals / (Pressure - VaporPressure).Hectopascals * 1000;

        /// <summary>
        /// Saturation mixing ratio in g/kg
        /// </summary>
        public double SaturationMixingRatio => ε * SaturationVaporPressure.Hectopascals / (Pressure - SaturationVaporPressure).Hectopascals * 1000;

        /// <summary>
        /// Density of water vapor in kg/m³
        /// </summary>
        public Density VaporDensity => new(VaporPressure.Hectopascals * 100 / (ℜv * Temperature.Kelvins), DensityUnit.KilogramPerCubicMeter);

        /// <summary>
        /// Density of dry air in kg/m³
        /// </summary>
        public Density DryAirDensity => new(PartialPressureDryAir.Hectopascals * 100 / (ℜd * Temperature.Kelvins), DensityUnit.KilogramPerCubicMeter);

        /// <summary>
        /// Total air density in kg/m³
        /// </summary>
        public Density Density => VaporDensity + DryAirDensity;

        /// <summary>
        /// Density altitude in meters
        /// </summary>
        public Length DensityAltitude => new(Tstd / γstd * (1 - Pow(Pressure.Hectopascals / Pstd / (Temperature.Kelvins / Tstd), γstd * ℜ / (g * Md - γstd * ℜ))), LengthUnit.Meter);

        /// <summary>
        /// Virtual air temperature in °C
        /// </summary>
        public Temperature VirtualTemperature => new(Temperature.Kelvins * (1 + ε * (MixingRatio / 1000)), TemperatureUnit.Kelvin);

        /// <summary>
        /// Potential Temperature in °C
        /// </summary>
        public Temperature PotentialTemperature => new(Temperature.DegreesCelsius + Pow(1000 / Pressure.Kilopascals, ℜd / Cp), TemperatureUnit.DegreeCelsius);

        /// <summary>
        /// Theta-E, in °C
        /// </summary>
        public Temperature ThetaE => new(PotentialTemperature.DegreesCelsius + Lv / Cp * (MixingRatio / 1000), TemperatureUnit.DegreeCelsius);

        /// <summary>
        /// Wet bulb temperature in °C
        /// </summary>
        public Temperature WetBulbTemperature => new(Temperature.DegreesCelsius
                                                     * Atan(0.151977 * Sqrt(RelativeHumidity.Percent + 8.313659))
                                                     + Atan(Temperature.DegreesCelsius + RelativeHumidity.Percent)
                                                     - Atan(RelativeHumidity.Percent - 1.676331)
                                                     + 0.00391838 * Pow(RelativeHumidity.Percent, 3.0 / 2.0)
                                                     * Atan(0.023101 * RelativeHumidity.Percent)
                                                     - 4.686035, TemperatureUnit.DegreeCelsius);

        /// <summary>
        /// Height of the lifted condensation level in meters
        /// </summary>
        public Length LCLHeight => new(0.125 * (Temperature.DegreesCelsius - Dewpoint.DegreesCelsius) * 1000, LengthUnit.Meter);

        /// <summary>
        /// Pressure height of the lifted condensation level in hPa
        /// </summary>
        public Pressure LCLPressure => new(Pressure.Hectopascals * Pow(1 - 1.225 * ((Temperature.DegreesCelsius - Dewpoint.DegreesCelsius) / Temperature.Kelvins), 3.5), PressureUnit.Hectopascal);

        /// <summary>
        /// Wind chill in °C.  Only applicable if Temperature &lt;10°C and Wind Speed &gt;1.33 m/s
        /// </summary>
        /// <remarks>Wind chill is only valid below 10°C with wind speeds above 1.33m/s.  Otherwise, this will return the ambient temperature.</remarks>
        public Temperature Windchill
        {
            get
            {
                if (Temperature.DegreesCelsius > 10 || WindSpeed.MetersPerSecond < 1.333333333)
                    return Temperature;

                return new Temperature(0.62 * Temperature.DegreesCelsius + 13.1 + (0.51 * Temperature.DegreesCelsius - 14.6) * Pow(WindSpeed.MetersPerSecond / 1.33333333333, 0.16), TemperatureUnit.DegreeCelsius);
            }
        }

        /// <summary>
        /// Heat Index in °C
        /// </summary>
        /// <remarks>Although technically valid at lower temperatures, usefulness only comes when the temperature is at or above 25°C.</remarks>
        public Temperature HeatIndex => new(0.8841 * Temperature.DegreesCelsius + 0.19 + (Temperature.DegreesCelsius - (0.8841 * Temperature.DegreesCelsius + 0.19))
                                            * Pow(RelativeHumidity.Percent/100.0 * SaturationVaporPressure.Hectopascals / 16, 0.0196 * Temperature.DegreesCelsius + 0.9031), TemperatureUnit.DegreeCelsius);

        /// <summary>
        /// Specific heat for ambient air at constant pressure
        /// </summary>
        public double Cp => Cpd * (1 + 1.84 * (MixingRatio / 1000));

        /// <summary>
        /// Calculates the standard pressure for a given geopotential height.
        /// </summary>
        /// <remarks>
        /// <para>Geopotential height can be approximated to actual height in a pinch.  To calculate geopotential height, use <see cref="CalculateGeopotentialHeight">GeopotentialHeight</see></para>
        /// <para>Roland Stull, "Practical Meteorology" pg. 12 (for 0-51km)<br />
        /// <a href="http://www.braeunig.us/space/atmmodel.htm">http://www.braeunig.us/space/atmmodel.htm</a> (above 51km)<br />
        /// Validation data: <a href="https://www.avs.org/AVS/files/c7/c7edaedb-95b2-438f-adfb-36de54f87b9e.pdf">https://www.avs.org/AVS/files/c7/c7edaedb-95b2-438f-adfb-36de54f87b9e.pdf</a></para>
        /// </remarks>
        /// <param name="geopotH">Geopotential height in km</param>
        /// <returns>Standard pressure in hPa (mb) for the provided geopotential height.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the geopotential height is either below 0 km or above 84.85 km.</exception>
        public static Pressure CalculateStandardPressure(Length geopotH)
        {
            /* Roland Stull, "Practical Meteorology" pg. 12 (for 0-51km)
             * http://www.braeunig.us/space/atmmodel.htm (above 51km)
             * Validation data: https://www.avs.org/AVS/files/c7/c7edaedb-95b2-438f-adfb-36de54f87b9e.pdf
             */

            return geopotH.Kilometers switch
            {
                <= 11 => new Pressure(101.325    * Pow(288.15 / CalculateStandardTemperature(geopotH).DegreesCelsius, -5.255877), PressureUnit.Hectopascal),
                <= 20 => new Pressure(22.63206   * Exp(-0.1577 * (geopotH.Kilometers - 11)), PressureUnit.Hectopascal),
                <= 32 => new Pressure(5.474889   * Pow(216.65 / CalculateStandardTemperature(geopotH).DegreesCelsius, 31.16319), PressureUnit.Hectopascal),
                <= 47 => new Pressure(0.8680187  * Pow(228.65 / CalculateStandardTemperature(geopotH).DegreesCelsius, 12.2011), PressureUnit.Hectopascal),
                <= 51 => new Pressure(0.110      * Exp(-0.1262 * (geopotH.Kilometers - 47)), PressureUnit.Hectopascal),
                <= 71 => new Pressure(0.06693887 * Pow(270.65 / CalculateStandardTemperature(geopotH).DegreesCelsius, -12.2011),
                                      PressureUnit.Hectopascal),
                <= 84.85 => new Pressure(0.003956420 * Pow(214.65 / CalculateStandardTemperature(geopotH).DegreesCelsius, -17.0816),
                                         PressureUnit.Hectopascal),
                _ => throw new ArgumentOutOfRangeException(nameof(geopotH),
                                                           "Geopotential height must be in range 0 - 84.85km.")
            };
        }

        /// <summary>
        /// Calculates the standard temperature for a given geopotential height.
        /// </summary>
        /// <remarks>
        /// <para>Geopotential height can be approximated to actual height in a pinch.  To calculate geopotential height, use <see cref="CalculateGeopotentialHeight">GeopotentialHeight</see></para>
        /// <para>Roland Stull, "Practical Meteorology" pg. 12 (for 0-51km)<br />
        /// <a href="http://www.braeunig.us/space/atmmodel.htm">http://www.braeunig.us/space/atmmodel.htm</a> (above 51km)<br />
        /// Validation data: <a href="https://www.avs.org/AVS/files/c7/c7edaedb-95b2-438f-adfb-36de54f87b9e.pdf">https://www.avs.org/AVS/files/c7/c7edaedb-95b2-438f-adfb-36de54f87b9e.pdf</a></para>
        /// </remarks>
        /// <param name="geopotH">Geopotential height in km</param>
        /// <returns>Standard temperature in °C for the provided geopotential height.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the geopotential height is either below 0 km or above 84.85 km.</exception>
        public static Temperature CalculateStandardTemperature(Length geopotH)
        {
            return geopotH.Kilometers switch
            {
                <= 11 => new Temperature(288.15 - 6.5 * geopotH.Kilometers, TemperatureUnit.DegreeCelsius),
                <= 20 => new Temperature(216.65, TemperatureUnit.DegreeCelsius),
                <= 32 => new Temperature(196.65 + geopotH.Kilometers, TemperatureUnit.DegreeCelsius),
                <= 47 => new Temperature(228.65 + 2.8 * (geopotH.Kilometers - 32), TemperatureUnit.DegreeCelsius),
                <= 51 => new Temperature(270.65, TemperatureUnit.DegreeCelsius),
                <= 71 => new Temperature(270.65 - 2.8 * (geopotH.Kilometers - 51), TemperatureUnit.DegreeCelsius),
                <= 84.85 => new Temperature(214.65 - 2 * (geopotH.Kilometers - 71), TemperatureUnit.DegreeCelsius),
                _ => throw new ArgumentOutOfRangeException(nameof(geopotH),
                                                           "Geopotential height must be in range 0 - 84.85km.")
            };
        }

        /// <summary>
        /// Calculates the geopotential height from a given elevation.
        /// </summary>
        /// <param name="elev">Elevation above sea level, in meters.</param>
        /// <returns>Geopotential height, in meters.</returns>
        /// <remarks>The calculation here is an approximation based on Roland Stull's "Practical Meteorology" pg. 11.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="elev"/> is less than the inverse of <see cref="Rearth"/>.</exception>
        public static Length CalculateGeopotentialHeight(Length elev)
        {
            if (elev.Meters <= -Rearth)
                throw new ArgumentOutOfRangeException(nameof(elev), $"{nameof(elev)} must not be less than the inverse radius of the Earth!  ({nameof(elev)} must be greater than {-Rearth}m!)");

            return new Length(Rearth * elev.Meters / (Rearth + elev.Meters), LengthUnit.Meter);
        }

        /// <summary>
        /// If <see cref="WeatherState"/> was supplied <see cref="RelativeHumidity"/>, this value will be <b>true</b>; otherwise <b>false</b>.
        /// </summary>
        private bool _suppliedHumidity;

        /// <summary>
        /// Underlying field for <see cref="RelativeHumidity"/>
        /// </summary>
        private RelativeHumidity _humidity = RelativeHumidity.Zero;
        /// <summary>
        /// Underlying field for <see cref="Dewpoint"/>
        /// </summary>
        private Temperature _dewpoint = Temperature.Zero;

        public string ToMETAR(string locationCode)
        {
            // The minimum information we need for a valid METAR is temperature and pressure.
            if (Temperature == default && Dewpoint == default && Pressure == default)
                return null;
            
            StringBuilder metarBuilder = new StringBuilder();

            metarBuilder.Append(locationCode.ToUpperInvariant());
            metarBuilder.Append(' ');

            metarBuilder.Append(Time.ToUniversalTime().ToString("ddHHmm"));
            metarBuilder.Append('Z');
            metarBuilder.Append(' ');

            if (WindSpeed != default && WindDirection != default)
            {
                metarBuilder.Append(WindDirection.Degrees.ToString("000"));
                metarBuilder.Append(WindSpeed.Knots.ToString("#00"));

                if (WindGust5Minute != default && WindGust5Minute.Knots - 3 > WindSpeed.Knots)
                {
                    metarBuilder.Append('G');
                    metarBuilder.Append(WindGust5Minute.Knots.ToString("#00"));
                }

                metarBuilder.Append("KT");
                metarBuilder.Append(' ');
            }

            if (Visibility != default)
            {
                double visMiles = Visibility.Miles;

                switch (visMiles)
                {
                    case < 1/4f:
                        metarBuilder.Append("M1/4SM ");
                        break;
                    case < 1/2f:
                        metarBuilder.Append("1/2SM ");
                        break;
                    case < 3/4f:
                        metarBuilder.Append("3/4SM ");
                        break;
                    case < 1f:
                        metarBuilder.Append("1SM ");
                        break;
                }
            }

            if (Weather != WeatherCode.Unknown)
            {
                string? metarCode = Weather.GetEnumMemberValue();

                if (!string.IsNullOrEmpty(metarCode))
                {
                    metarBuilder.Append(metarCode);
                    metarBuilder.Append(' ');
                }
            }

            if (Temperature != default && Dewpoint != default)
            {
                if (Temperature.DegreesCelsius < 0)
                    metarBuilder.Append('M');
                metarBuilder.Append(Abs(Floor(Temperature.DegreesCelsius)).ToString("00"));
                metarBuilder.Append('/');
                if (Dewpoint.DegreesCelsius < 0)
                    metarBuilder.Append('M');
                metarBuilder.Append(Abs(Floor(Dewpoint.DegreesCelsius)).ToString("00"));
                metarBuilder.Append(' ');
            }

            if (SeaLevelPressure != default)
            {
                metarBuilder.Append('A');
                metarBuilder.Append((SeaLevelPressure.InchesOfMercury * 100).ToString("0000"));
                metarBuilder.Append(' ');
            }

            metarBuilder.Append("RMK AO2 ");

            if (SeaLevelPressure != default)
            {
                metarBuilder.Append("SLP");

                var adjustedSLP = (SeaLevelPressure.Hectopascals * 10) - 10000;
                metarBuilder.Append(adjustedSLP.ToString("000"));
                metarBuilder.Append(' ');
            }

            if (RainfallLastHour != default)
            {
                metarBuilder.Append('P');
                metarBuilder.Append(Floor(RainfallLastHour.Inches * 100).ToString("0000"));
                metarBuilder.Append(' ');
            }
            
            if (Temperature != default && Dewpoint != default)
            {
                metarBuilder.Append('T');
                metarBuilder.Append(Temperature.DegreesCelsius < 0 ? '1' : '0');
                metarBuilder.Append(Abs(Floor(Temperature.DegreesCelsius*10)).ToString("000"));
                
                metarBuilder.Append(Dewpoint.DegreesCelsius < 0 ? '1' : '0');
                metarBuilder.Append(Abs(Floor(Dewpoint.DegreesCelsius*10)).ToString("000"));
                metarBuilder.Append(' ');
            }

            if (SnowfallSinceMidnight != default && SnowfallSinceMidnight.Inches > 1)
            {
                metarBuilder.Append("4/");
                metarBuilder.Append($"{Round(SnowfallSinceMidnight.Inches):000}");
                metarBuilder.Append(' ');
            }

            if (RainfallSinceMidnight != default && RainfallSinceMidnight.Inches > 0.01)
            {
                metarBuilder.Append('7');
                metarBuilder.Append($"{Round(RainfallSinceMidnight.Inches * 100):0000}");
                metarBuilder.Append(' ');
            }

            return metarBuilder.ToString().Trim() + '=';
        }

        public static WeatherState Merge(WeatherState a, WeatherState b)
        {
            V AOrB<T, V>(T x, T y, Func<T, V> selector) => selector(x).Equals(default(V)) ? selector(y) : selector(x);
            
            return new WeatherState
            {
                Time = AOrB(a, b, x => x.Time),
                Temperature = AOrB(a, b, x => x.Temperature),
                Pressure = AOrB(a, b, x => x.Pressure),
                Elevation = AOrB(a, b, x => x.Elevation),
                WindSpeed = AOrB(a, b, x => x.WindSpeed),
                WindDirection = AOrB(a, b, x => x.WindDirection),
                Luminosity = AOrB(a, b, x => x.Luminosity),
                RainfallLastHour = AOrB(a, b, x => x.RainfallLastHour),
                RainfallLast24Hours = AOrB(a, b, x => x.RainfallLast24Hours),
                RainfallSinceMidnight = AOrB(a, b, x => x.RainfallSinceMidnight),
                SnowfallSinceMidnight = AOrB(a, b, x => x.SnowfallSinceMidnight),
                InstantaneousRainRate = AOrB(a, b, x => x.InstantaneousRainRate),
                WindGust5Minute = AOrB(a, b, x => x.WindGust5Minute),
                WindGust30Minute = AOrB(a, b, x => x.WindGust30Minute),
                WindGust1Hour = AOrB(a, b, x => x.WindGust1Hour),
                BatteryVoltage = AOrB(a, b, x => x.BatteryVoltage),
                EnclosureTemperature = AOrB(a, b, x => x.Temperature),
                Visibility = AOrB(a, b, x => x.Visibility),
                Weather = a.Weather == WeatherCode.Unknown ? b.Weather : a.Weather,
                WaterIntensity = AOrB(a, b, x => x.WaterIntensity),
                Dewpoint = AOrB(a, b, x => x.Dewpoint),
                RelativeHumidity = AOrB(a, b, x => x.RelativeHumidity)
            };
        }
    }
}

using System;
using FluentAssertions;
using UnitsNet;
using UnitsNet.Units;
using Xunit;

namespace weatherd.tests
{
    public class WeatherStateTests
    {
        [Fact]
        public void RelativeHumidity_ShouldBeAccurate_WhenProvidedValidTemperatureAndDewpoint()
        {
            // Arrange
            Pressure standardPressure = new Pressure(1013.25, PressureUnit.Hectopascal);
            Temperature temp = new Temperature(25, TemperatureUnit.DegreeCelsius);
            Temperature dewpoint = new Temperature(15, TemperatureUnit.DegreeCelsius);

            RelativeHumidity expectedHumidity = new RelativeHumidity(52.4, RelativeHumidityUnit.Percent);

            // Act
            WeatherState weatherState = new WeatherState
            {
                Pressure = standardPressure,
                Temperature = temp,
                Dewpoint = dewpoint
            };

            // Assert
            weatherState.Should().NotBeNull();
            weatherState.RelativeHumidity.Percent.Should().BeApproximately(expectedHumidity.Percent, 2);
        }

        [Fact]
        public void RelativeHumidity_ShouldThrowException_WhenProvidedValidTemperatureAndNoDewpoint()
        {
            // Arrange
            Pressure standardPressure = new Pressure(1013.25, PressureUnit.Hectopascal);
            Temperature temp = new Temperature(25, TemperatureUnit.DegreeCelsius);

            // Act
            WeatherState weatherState = new WeatherState
            {
                Pressure = standardPressure,
                Temperature = temp
            };
            
            // Assert
            weatherState.Should().NotBeNull();
            weatherState.RelativeHumidity.Should().Be(default);
        }
        
        [Fact]
        public void Dewpoint_ShouldBeAccurate_WhenProvidedValidTemperatureAndHumidity()
        {
            // Arrange
            Pressure standardPressure = new Pressure(1013.25, PressureUnit.Hectopascal);
            Temperature temp = new Temperature(25, TemperatureUnit.DegreeCelsius);
            RelativeHumidity humidity = new RelativeHumidity(52.4, RelativeHumidityUnit.Percent);
            
            Temperature expectedDewpoint = new Temperature(15, TemperatureUnit.DegreeCelsius);

            // Act
            WeatherState weatherState = new WeatherState
            {
                Pressure = standardPressure,
                Temperature = temp,
                RelativeHumidity = humidity
            };

            // Assert
            weatherState.Should().NotBeNull();
            weatherState.Dewpoint.DegreesCelsius.Should().BeApproximately(expectedDewpoint.DegreesCelsius, 1);
        }
        
        [Fact]
        public void Dewpoint_ShouldThrowException_WhenProvidedValidTemperatureAndNoHumidity()
        {
            // Arrange
            Pressure standardPressure = new Pressure(1013.25, PressureUnit.Hectopascal);
            Temperature temp = new Temperature(25, TemperatureUnit.DegreeCelsius);

            // Act
            WeatherState weatherState = new WeatherState
            {
                Pressure = standardPressure,
                Temperature = temp
            };

            // Assert
            weatherState.Should().NotBeNull();
            weatherState.Dewpoint.Should().Be(default);
        }
        
        [Fact]
        public void DewpointDepression_ShouldBeAccurate_WhenProvidedValidTemperatureAndDewpoint()
        {
            // Arrange
            Pressure standardPressure = new Pressure(1013.25, PressureUnit.Hectopascal);
            Temperature temp = new Temperature(25, TemperatureUnit.DegreeCelsius);
            Temperature dewpoint = new Temperature(15, TemperatureUnit.DegreeCelsius);

            // Act
            WeatherState weatherState = new WeatherState
            {
                Pressure = standardPressure,
                Temperature = temp,
                Dewpoint = dewpoint
            };

            // Assert
            weatherState.Should().NotBeNull();
            weatherState.DewpointDepression.Should().Be(temp - dewpoint);
        }
        
        [Fact]
        public void WetBulb_ShouldBeAccurate_WhenProvidedValidTemperatureAndHumidity()
        {
            // Arrange
            Pressure standardPressure = new Pressure(1013.25, PressureUnit.Hectopascal);
            Temperature temp = new Temperature(17, TemperatureUnit.DegreeCelsius);
            RelativeHumidity humidity = new RelativeHumidity(80, RelativeHumidityUnit.Percent);

            // Act
            WeatherState weatherState = new WeatherState
            {
                Pressure = standardPressure,
                Temperature = temp,
                RelativeHumidity = humidity
            };

            // Assert
            weatherState.Should().NotBeNull();
            weatherState.WetBulbTemperature.DegreesCelsius.Should().BeApproximately(14.65, 1);
        }
        
        [Fact]
        public void Merge_ShouldReturnMergedResult_WhenProvidedTwoWeatherStates()
        {
            // Arrange
            WeatherState a = new WeatherState()
            {
                Time = DateTime.UtcNow,
                Temperature = new Temperature(20, TemperatureUnit.DegreeCelsius),
                RelativeHumidity = new RelativeHumidity(78, RelativeHumidityUnit.Percent),
                Pressure = new Pressure(29.92, PressureUnit.InchOfMercury),
                Luminosity = new Irradiance(482, IrradianceUnit.WattPerSquareMeter)
            };

            WeatherState b = new WeatherState()
            {
                Visibility = new Length(2000, LengthUnit.Meter),
                Weather = new WeatherCondition(Intensity.Light, Precipitation.Rain)
            };
            
            // Act
            WeatherState c = WeatherState.Merge(a, b);
            
            // Assert
            c.Time.Should().Be(a.Time);
            c.Temperature.Should().Be(a.Temperature);
            c.RelativeHumidity.Should().Be(a.RelativeHumidity);
            c.Dewpoint.Should().Be(a.Dewpoint);
            c.Pressure.Should().Be(a.Pressure);
            c.Luminosity.Should().Be(a.Luminosity);
            c.Visibility.Should().Be(b.Visibility);
            c.Weather.Should().Be(b.Weather);
        }
        
        [Fact]
        public void ToMETAR_ShouldReturnValidMETAR_WhenProvidedValidWeatherState()
        {
            // Arrange
            WeatherState a = new WeatherState()
            {
                Time = new DateTime(2022, 4, 5, 18, 53, 0, DateTimeKind.Utc),
                
                WindDirection = new Angle(040, AngleUnit.Degree),
                WindSpeed = new Speed(11, SpeedUnit.Knot),
                
                Visibility = new Length(370, LengthUnit.Meter),
                Weather = new WeatherCondition(Intensity.Moderate, Precipitation.Snow),
                
                Temperature = new Temperature(-1.7, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-2.3, TemperatureUnit.DegreeCelsius),
                
                Pressure = new Pressure(30.06, PressureUnit.InchOfMercury),
                
                RainfallLastHour = new Length(0.02, LengthUnit.Inch)
            };
            
            // Act
            string metar = a.ToMETAR("PAXX");
            
            // Assert
            metar.Should()
                 .Be(
                     "PAXX 051853Z 04011KT M1/4SM SN M02/M03 A3006 RMK AO2 SLP179 P0002 T10171023=");
        }
    }
}

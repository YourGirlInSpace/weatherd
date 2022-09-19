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
                SeaLevelPressure = standardPressure,
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
                SeaLevelPressure = standardPressure,
                Temperature = temp
            };

            Func<RelativeHumidity> test_relativeHumidity = () => weatherState.RelativeHumidity;

            // Assert
            weatherState.Should().NotBeNull();
            test_relativeHumidity.Should().Throw<InsufficientMeteorologicalInformationException>();
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
                SeaLevelPressure = standardPressure,
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
                SeaLevelPressure = standardPressure,
                Temperature = temp
            };

            Func<Temperature> test_dewpoint = () => weatherState.Dewpoint;

            // Assert
            weatherState.Should().NotBeNull();
            test_dewpoint.Should().Throw<InsufficientMeteorologicalInformationException>();
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
                SeaLevelPressure = standardPressure,
                Temperature = temp,
                Dewpoint = dewpoint
            };

            // Assert
            weatherState.Should().NotBeNull();
            weatherState.DewpointDepression.Should().Be(temp - dewpoint);
        }
    }
}

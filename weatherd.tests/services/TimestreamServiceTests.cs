using System;
using FluentAssertions;
using UnitsNet;
using UnitsNet.Units;
using weatherd.services;
using Xunit;

namespace weatherd.tests.services
{
    public class TimestreamServiceTests
    {
        [Fact]
        public void GetProperty_ShouldReturnValidData_WhenProvidedValidPropertyAndUnit()
        {
            // Arrange 
            WeatherState wxState = new WeatherState
            {
                Temperature = new Temperature(23, TemperatureUnit.DegreeCelsius)
            };

            // Act
            double result = (double) TimestreamService.GetProperty(wxState, nameof(WeatherState.Temperature),
                                                 nameof(Temperature.DegreesCelsius));

            // Assert
            result.Should().Be(23);
        }

        [Fact]
        public void GetProperty_ShouldThrowInvalidOperationException_WhenProvidedInvalidProperty()
        {
            // Arrange
            WeatherState wxState = new WeatherState
            {
                Temperature = new Temperature(23, TemperatureUnit.DegreeCelsius)
            };

            // Act
            Action r = () =>
                TimestreamService.GetProperty(wxState, "Hoopla", nameof(Temperature.DegreesCelsius));

            // Assert
            r.Should().Throw<InvalidOperationException>()
             .WithMessage("Could not find meteorological property 'Hoopla'");
        }

        [Fact]
        public void GetProperty_ShouldThrowInvalidOperationException_WhenProvidedInvalidUnit()
        {
            // Arrange
            WeatherState wxState = new WeatherState
            {
                Temperature = new Temperature(23, TemperatureUnit.DegreeCelsius)
            };

            // Act
            Action r = () =>
                TimestreamService.GetProperty(wxState, nameof(WeatherState.Temperature), nameof(Length.Millimeters));

            // Assert
            r.Should().Throw<InvalidOperationException>()
             .WithMessage("Could not find unit 'Millimeters'");
        }

        [Fact]
        public void GetProperty_ShouldThrowInvalidOperationException_WhenValueIsNotSet()
        {
            // Arrange
            WeatherState wxState = new WeatherState
            {
                Temperature = new Temperature(23, TemperatureUnit.DegreeCelsius)
            };

            // Act
            Action r = () =>
                TimestreamService.GetProperty(wxState, nameof(WeatherState.Weather), nameof(Temperature.DegreesCelsius));

            // Assert
            r.Should().Throw<InvalidOperationException>()
             .WithMessage("Could not resolve 'Weather' into a value.");
        }
    }
}

using System;
using FluentAssertions;
using UnitsNet;
using UnitsNet.Units;
using weatherd.services;
using Xunit;

namespace weatherd.tests.services
{
    public class WeatherServiceTests
    {
        [Fact]
        public void EnforceCorrectness_NotSufficientInfo_StateNotChanged()
        {
            // Arrange
            WeatherState wxState = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Rain, Obscuration.Fog),
                Temperature = new Temperature(-40, TemperatureUnit.DegreeCelsius)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxState);

            // Assert
            wxState.Weather.Precipitation.Should().NotBe(Precipitation.Snow);
        }
        
        [Fact]
        public void EnforceCorrectness_ShouldChangeRainAndDrizzleToSnow_WhenTemperatureIsBelowMinus10Celsius()
        {
            // Arrange
            WeatherState wxStateA = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Rain, Obscuration.None),
                Temperature = new Temperature(-40, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-50, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };
            WeatherState wxStateB = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Drizzle, Obscuration.None),
                Temperature = new Temperature(-40, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-50, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxStateA);
            WeatherService.EnforceCorrectness(ref wxStateB);

            // Assert
            wxStateA.Weather.Precipitation.Should().Be(Precipitation.Snow, "rain should be snow at -40°C");
            wxStateB.Weather.Precipitation.Should().Be(Precipitation.Snow, "drizzle should be snow at -40°C");
        }
        
        [Fact]
        public void EnforceCorrectness_ShouldNotChangeOtherPrecipitationToSnow_WhenTemperatureIsBelowMinus10Celsius()
        {
            // Arrange
            WeatherState wxStateA = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Sleet, Obscuration.None),
                Temperature = new Temperature(-40, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-50, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };
            WeatherState wxStateB = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Unknown, Obscuration.None),
                Temperature = new Temperature(-40, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-50, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxStateA);
            WeatherService.EnforceCorrectness(ref wxStateB);

            // Assert
            wxStateA.Weather.Precipitation.Should().NotBe(Precipitation.Snow);
            wxStateB.Weather.Precipitation.Should().NotBe(Precipitation.Snow);
        }
        
        [Fact]
        public void EnforceCorrectness_ShouldMarkRainAndDrizzleAsFreezing_WhenTemperatureIs0CelsiusAndAboveNegative10()
        {
            // Arrange
            WeatherState wxStateA = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Rain, Obscuration.None),
                Temperature = new Temperature(-3, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-4, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };
            WeatherState wxStateB = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Drizzle, Obscuration.None),
                Temperature = new Temperature(-3, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-4, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxStateA);
            WeatherService.EnforceCorrectness(ref wxStateB);

            // Assert
            wxStateA.Weather.Descriptor.Should().HaveFlag(Descriptor.Freezing);
            wxStateB.Weather.Descriptor.Should().HaveFlag(Descriptor.Freezing);
        }

        [Fact]
        public void EnforceCorrectness_ShouldNotMarkRainAndDrizzleAsFreezing_WhenTemperatureIsBelowNegative10()
        {
            // Arrange
            WeatherState wxStateA = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Rain, Obscuration.None),
                Temperature = new Temperature(-13, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-14, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };
            WeatherState wxStateB = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Drizzle, Obscuration.None),
                Temperature = new Temperature(-13, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-14, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxStateA);
            WeatherService.EnforceCorrectness(ref wxStateB);

            // Assert
            wxStateA.Weather.Descriptor.Should().NotHaveFlag(Descriptor.Freezing);
            wxStateB.Weather.Descriptor.Should().NotHaveFlag(Descriptor.Freezing);
        }

        [Fact]
        public void EnforceCorrectness_ShouldRemoveFreezingFlag_WhenTemperatureIsBelowNegative10()
        {
            // Arrange
            WeatherState wxStateA = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.Freezing, Precipitation.Rain, Obscuration.None),
                Temperature = new Temperature(-13, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-14, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };
            WeatherState wxStateB = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.Freezing, Precipitation.Drizzle, Obscuration.None),
                Temperature = new Temperature(-13, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-14, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxStateA);
            WeatherService.EnforceCorrectness(ref wxStateB);

            // Assert
            wxStateA.Weather.Descriptor.Should().NotHaveFlag(Descriptor.Freezing);
            wxStateB.Weather.Descriptor.Should().NotHaveFlag(Descriptor.Freezing);
        }

        [Fact]
        public void EnforceCorrectness_ShouldRemoveFreezingFlag_WhenTemperatureIsAbove0()
        {
            // Arrange
            WeatherState wxStateA = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.Freezing, Precipitation.Rain, Obscuration.None),
                Temperature = new Temperature(3, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-14, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };
            WeatherState wxStateB = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.Freezing, Precipitation.Drizzle, Obscuration.None),
                Temperature = new Temperature(3, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-14, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxStateA);
            WeatherService.EnforceCorrectness(ref wxStateB);

            // Assert
            wxStateA.Weather.Descriptor.Should().NotHaveFlag(Descriptor.Freezing);
            wxStateB.Weather.Descriptor.Should().NotHaveFlag(Descriptor.Freezing);
        }
        
        [Fact]
        public void EnforceCorrectness_ShouldChangeSnowAndSleetToRain_WhenTemperatureIsAbove10Celsius()
        {
            // Arrange
            WeatherState wxStateA = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Snow, Obscuration.None),
                Temperature = new Temperature(11, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-50, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };
            WeatherState wxStateB = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.Sleet, Obscuration.None),
                Temperature = new Temperature(11, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-50, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(2, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxStateA);
            WeatherService.EnforceCorrectness(ref wxStateB);

            // Assert
            wxStateA.Weather.Precipitation.Should().Be(Precipitation.Rain);
            wxStateB.Weather.Precipitation.Should().Be(Precipitation.Rain);
        }
        
        [Fact]
        public void EnforceCorrectness_ShouldCorrectObscurationToHaze_WhenVisibilityIsLessThan2KMAndDewpointDepressionIsGreaterThan2Celsius()
        {
            // Arrange
            WeatherState wxState = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.None, Obscuration.Fog),
                Temperature = new Temperature(10, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-50, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(1.5, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxState);

            // Assert
            wxState.Weather.Obscuration.Should().Be(Obscuration.Haze);
        }
        
        [Fact]
        public void EnforceCorrectness_ShouldCorrectObscurationToMist_WhenVisibilityIsLessThan2KMAndDewpointDepressionIsLessThan2Celsius()
        {
            // Arrange
            WeatherState wxState = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.None, Obscuration.Fog),
                Temperature = new Temperature(10, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(9, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(1.5, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxState);

            // Assert
            wxState.Weather.Obscuration.Should().Be(Obscuration.Mist);
        }
        
        [Fact]
        public void EnforceCorrectness_ShouldCorrectObscurationToFog_WhenVisibilityIsLessThan1KMAndDewpointDepressionIsLessThan2Celsius()
        {
            // Arrange
            WeatherState wxState = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.None, Obscuration.Haze),
                Temperature = new Temperature(10, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(9, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(0.5, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxState);

            // Assert
            wxState.Weather.Obscuration.Should().Be(Obscuration.Fog);
        }
        
        [Fact]
        public void EnforceCorrectness_ShouldAddFreezingDescriptorToFog_WhenTemperatureIsBelowZero()
        {
            // Arrange
            WeatherState wxState = new()
            {
                Time = DateTime.UtcNow,
                Weather = new WeatherCondition(Intensity.Moderate, Descriptor.None, Precipitation.None, Obscuration.Fog),
                Temperature = new Temperature(-10, TemperatureUnit.DegreeCelsius),
                Dewpoint = new Temperature(-11, TemperatureUnit.DegreeCelsius),
                Visibility = new Length(0.5, LengthUnit.Kilometer)
            };

            // Act
            WeatherService.EnforceCorrectness(ref wxState);

            // Assert
            wxState.Weather.Obscuration.Should().Be(Obscuration.Fog);
            wxState.Weather.Descriptor.Should().HaveFlag(Descriptor.Freezing);
        }
    }
}

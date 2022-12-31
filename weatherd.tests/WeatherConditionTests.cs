using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;

namespace weatherd.tests
{
    public class WeatherConditionTests
    {
        [Fact]
        public void ToString_ShouldGiveValidMetar_WhenGivenValidState()
        {
            // Arrange
            Dictionary<WeatherCondition, string> mappingTable = new Dictionary<WeatherCondition, string>
            {
                { new WeatherCondition(Precipitation.None), "" },
                { new WeatherCondition(Obscuration.Haze), "HZ" },
                { new WeatherCondition(Obscuration.Mist), "BR" },
                { new WeatherCondition(Descriptor.Recent, Obscuration.Fog), "REFG" },
                { new WeatherCondition(Descriptor.Recent, Precipitation.Unknown), "REUP" },
                { new WeatherCondition(Descriptor.Recent, Precipitation.Drizzle), "REDZ" },
                { new WeatherCondition(Descriptor.Recent, Precipitation.Rain), "RERA" },
                { new WeatherCondition(Descriptor.Recent, Precipitation.Snow), "RESN" },
                { new WeatherCondition(Descriptor.Recent | Descriptor.Freezing, Precipitation.Rain), "REFZRA" },
                { new WeatherCondition(Obscuration.Fog), "FG" },
                { new WeatherCondition(Descriptor.Patchy, Obscuration.Fog), "BCFG" },
                { new WeatherCondition(Precipitation.Unknown), "UP" },
                { new WeatherCondition(Intensity.Heavy, Precipitation.Unknown), "+UP" },
                { new WeatherCondition(Precipitation.Drizzle), "DZ" },
                { new WeatherCondition(Intensity.Light, Precipitation.Drizzle), "-DZ" },
                { new WeatherCondition(Intensity.Moderate, Precipitation.Drizzle), "DZ" },
                { new WeatherCondition(Intensity.Heavy, Precipitation.Drizzle), "+DZ" },
                { new WeatherCondition(Intensity.Light, Descriptor.Freezing, Precipitation.Drizzle), "-FZDZ" },
                { new WeatherCondition(Intensity.Moderate, Descriptor.Freezing, Precipitation.Drizzle), "FZDZ" },
                { new WeatherCondition(Intensity.Heavy, Descriptor.Freezing, Precipitation.Drizzle), "+FZDZ" },
                { new WeatherCondition(Precipitation.Rain), "RA" },
                { new WeatherCondition(Intensity.Light, Precipitation.Rain), "-RA" },
                { new WeatherCondition(Intensity.Moderate, Precipitation.Rain), "RA" },
                { new WeatherCondition(Intensity.Heavy, Precipitation.Rain), "+RA" },
                { new WeatherCondition(Intensity.Light, Descriptor.Freezing, Precipitation.Rain), "-FZRA" },
                { new WeatherCondition(Intensity.Moderate, Descriptor.Freezing, Precipitation.Rain), "FZRA" },
                { new WeatherCondition(Intensity.Heavy, Descriptor.Freezing, Precipitation.Rain), "+FZRA" },
                { new WeatherCondition(Intensity.Light, Precipitation.Rain | Precipitation.Snow), "-RASN" },
                { new WeatherCondition(Intensity.Moderate, Precipitation.Rain | Precipitation.Snow), "RASN" },
                { new WeatherCondition(Precipitation.Snow), "SN" },
                { new WeatherCondition(Intensity.Light, Precipitation.Snow), "-SN" },
                { new WeatherCondition(Intensity.Moderate, Precipitation.Snow), "SN" },
                { new WeatherCondition(Intensity.Heavy, Precipitation.Snow), "+SN" },
                { new WeatherCondition(Intensity.Light, Precipitation.Sleet), "-PL" },
                { new WeatherCondition(Intensity.Moderate, Precipitation.Sleet), "PL" },
                { new WeatherCondition(Intensity.Heavy, Precipitation.Sleet), "+PL" },
                { new WeatherCondition(Descriptor.Showers, Precipitation.Unknown), "SHUP" },
                { new WeatherCondition(Descriptor.Showers, Precipitation.None), "SH" },
                { new WeatherCondition(Intensity.Light, Descriptor.Showers, Precipitation.Rain), "-SHRA" },
                { new WeatherCondition(Intensity.Moderate, Descriptor.Showers, Precipitation.Rain), "SHRA" },
                { new WeatherCondition(Intensity.Heavy, Descriptor.Showers, Precipitation.Rain), "+SHRA" },
                { new WeatherCondition(AdditionalWeather.Squall), "SQ" },
                { new WeatherCondition(Intensity.Light, Descriptor.Showers, Precipitation.Snow), "-SHSN" },
                { new WeatherCondition(Intensity.Moderate, Descriptor.Showers, Precipitation.Snow), "SHSN" },
                { new WeatherCondition(Intensity.Heavy, Descriptor.Showers, Precipitation.Snow), "+SHSN" }
            };

            // Assert
            foreach ((WeatherCondition condition, string expectedValue) in mappingTable)
            {
                condition.ToString().Should().Be(expectedValue);
            }
        }
    }
}

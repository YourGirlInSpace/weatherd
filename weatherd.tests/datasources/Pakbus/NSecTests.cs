using System;
using FluentAssertions;
using weatherd.datasources.pakbus;
using Xunit;

namespace weatherd.tests.datasources.Pakbus
{
    public class NSecTests
    {
        
        [Fact]
        public void ToTime_WithValidTimestamp_ShouldReturnValidTime()
        {
            // Arrange
            DateTime time = new DateTime(2022, 9, 7, 20, 47, 23, DateTimeKind.Utc);
            NSec nsec = NSec.FromUnixTimestamp(((DateTimeOffset)time).ToUnixTimeSeconds());

            // Act
            DateTime calcTime = nsec.ToTime();

            // Assert
            calcTime.Should().Be(time);
        }

        [Fact]
        public void FromUnixTimestamp_WithValidTimestamp_ShouldReturnValidNSec()
        {
            const long timestamp = 1662583531;

            NSec nsec = NSec.FromUnixTimestamp(timestamp);

            nsec.Seconds.Should().Be(1031431531);
            nsec.Nanoseconds.Should().Be(0);
        }

        [Fact]
        public void FromDateTime_WithValidTime_ShouldReturnValidNSec()
        {
            DateTime time = new DateTime(2022, 9, 7, 20, 47, 23, DateTimeKind.Utc);

            NSec nsec = NSec.FromTime(time);

            nsec.Seconds.Should().Be(1031431643);
            nsec.Nanoseconds.Should().Be(0);
        }
    }
}

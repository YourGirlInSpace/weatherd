using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using weatherd.datasources.Vaisala;
using weatherd.datasources.Vaisala.Messages;
using Xunit;

namespace weatherd.tests.datasources.Vaisala.Messages
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public class VaisalaMessageTests
    {
        private const char SOH = '\x01';
        private const char STX = '\x02';
        private const char ETX = '\x03';

        [Fact]
        public void VaisalaMessage_ShouldReturnNull_WhenProvidedWithUnknownString()
        {
            var message = VaisalaMessage.Parse("test");

            message.Should().BeNull();
        }
        
        [Theory]
        [InlineData("PW  7\u000200   0  1230\x03")]
        [InlineData("\x01PW  700   0  1230\x03")]
        [InlineData("\x01PW  7\u000200   0  1230")]
        [InlineData("\x01PW 7\u000200   0  1230\x03")]
        public void VaisalaMessage_ShouldReturnNull_WhenMessageFrameCorrupted(string rawMessage)
        {
            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeNull();
        }
        
        [Fact]
        public void VaisalaMessage_ShouldReturnNull_WhenProvidedWithMalformedMessage()
        {
            string rawMessage = $"{SOH}PW  7{STX}00   YARP  1230{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeNull();
        }
        
        [Theory]
        [InlineData("\x01PW  7\u0002G0   0  1230\x03")]
        [InlineData("\x01PW  7\u00020G   0  1230\x03")]
        [InlineData("\x01PW  7\u0002\x03")]
        public void VaisalaMessage_ShouldReturnNull_WhenProvidedWithMalformedAlarm(string rawMessage)
        {
            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeNull();
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnNull_WhenProvidedWithPrematureMessageEnd()
        {
            string rawMessage = $"{SOH}PW  7{STX}00   0{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeNull();
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnNull_WhenProvidedWithNoETX()
        {
            string rawMessage = $"{SOH}PW  7{STX}00    680  1230";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeNull();
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnVisibility_WhenProvidedWithVisibilityMessage()
        {
            string rawMessage = $"{SOH}PW  7{STX}00    680  1230{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaVisibilityMessage>();

            var actualMessage = message as VaisalaVisibilityMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Visibility);
            actualMessage.SensorID.Should().Be("7");
            actualMessage.HardwareAlarm.Should().Be(HardwareAlarm.None);
            actualMessage.VisibilityAlarm.Should().Be(VisibilityAlarm.None);
            
            AssertValue(actualMessage.OneMinuteAverageVisibility, 680);
            AssertValue(actualMessage.TenMinuteAverageVisibility, 1230);
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnVisibility_WhenProvidedWithVisibilityMessageWithUnknownValues()
        {
            string rawMessage = $"{SOH}PW  7{STX}00 ////// ////{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaVisibilityMessage>();

            var actualMessage = message as VaisalaVisibilityMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Visibility);
            actualMessage.SensorID.Should().Be("7");
            actualMessage.HardwareAlarm.Should().Be(HardwareAlarm.None);
            actualMessage.VisibilityAlarm.Should().Be(VisibilityAlarm.None);

            AssertNoValue(actualMessage.OneMinuteAverageVisibility);
            AssertNoValue(actualMessage.TenMinuteAverageVisibility);
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnPrecipitation_WhenProvidedWithPrecipitationMessage()
        {
            string rawMessage = $"{SOH}PW  7{STX}00   1839 61   0.3{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaPrecipitationMessage>();

            var actualMessage = message as VaisalaPrecipitationMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Precipitation);
            actualMessage.SensorID.Should().Be("7");
            actualMessage.HardwareAlarm.Should().Be(HardwareAlarm.None);
            actualMessage.VisibilityAlarm.Should().Be(VisibilityAlarm.None);
            
            AssertValue(actualMessage.OneMinuteAverageVisibility, 1839);
            AssertValue(actualMessage.InstantaneousWeather, WMOCodeTable.RainLight);
            AssertValue(actualMessage.OneMinuteWaterIntensity, 0.3f);
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnPrecipitation_WhenProvidedWithPrecipitationMessageWithOneEmptyField()
        {
            string rawMessage = $"{SOH}PW  7{STX}00 ////// 61   0.3{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaPrecipitationMessage>();

            var actualMessage = message as VaisalaPrecipitationMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Precipitation);
            actualMessage.SensorID.Should().Be("7");
            actualMessage.HardwareAlarm.Should().Be(HardwareAlarm.None);
            actualMessage.VisibilityAlarm.Should().Be(VisibilityAlarm.None);

            AssertNoValue(actualMessage.OneMinuteAverageVisibility);
            AssertValue(actualMessage.InstantaneousWeather, WMOCodeTable.RainLight);
            AssertValue(actualMessage.OneMinuteWaterIntensity, 0.3f);
        }
        
        [Fact]
        public void VaisalaMessage_ShouldReturnFull_WhenProvidedWithFullMessage()
        {
            string rawMessage = $"{SOH}PW  7{STX}00  6839  7505  R  61 61 61  0.33 12.16   0{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaFullMessage>();

            var actualMessage = message as VaisalaFullMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Full);
            actualMessage.SensorID.Should().Be("7");
            actualMessage.HardwareAlarm.Should().Be(HardwareAlarm.None);
            actualMessage.VisibilityAlarm.Should().Be(VisibilityAlarm.None);

            AssertValue(actualMessage.OneMinuteAverageVisibility, 6839);
            AssertValue(actualMessage.TenMinuteAverageVisibility, 7505);
            AssertValue(actualMessage.NWSWeatherCode, "R");
            AssertValue(actualMessage.InstantaneousWeather, WMOCodeTable.RainLight);
            AssertValue(actualMessage.Weather15Minute, WMOCodeTable.RainLight);
            AssertValue(actualMessage.Weather1Hour, WMOCodeTable.RainLight);
            AssertValue(actualMessage.OneMinuteWaterIntensity, 0.33f);
            AssertValue(actualMessage.CumulativeWater, 12.16f);
            AssertValue(actualMessage.CumulativeSnow, 0);
        }
        
        [Fact]
        public void VaisalaMessage_ShouldReturnAviation_WhenProvidedWithAviatonMessage()
        {
            string rawMessage = $"{SOH}PW  7{STX}00  6839  7505  R  61 61 61  0.33 12.16   0  23.4 12345{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaAviationMessage>();

            var actualMessage = message as VaisalaAviationMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Aviation);
            actualMessage.SensorID.Should().Be("7");
            actualMessage.HardwareAlarm.Should().Be(HardwareAlarm.None);
            actualMessage.VisibilityAlarm.Should().Be(VisibilityAlarm.None);

            AssertValue(actualMessage.OneMinuteAverageVisibility, 6839);
            AssertValue(actualMessage.TenMinuteAverageVisibility, 7505);
            AssertValue(actualMessage.NWSWeatherCode, "R");
            AssertValue(actualMessage.InstantaneousWeather, WMOCodeTable.RainLight);
            AssertValue(actualMessage.Weather15Minute, WMOCodeTable.RainLight);
            AssertValue(actualMessage.Weather1Hour, WMOCodeTable.RainLight);
            AssertValue(actualMessage.OneMinuteWaterIntensity, 0.33f);
            AssertValue(actualMessage.CumulativeWater, 12.16f);
            AssertValue(actualMessage.CumulativeSnow, 0);
            AssertValue(actualMessage.Temperature, 23.4f);
            AssertValue(actualMessage.BackgroundLuminance, 12345);
        }
        
        [Fact]
        public void VaisalaMessage_ShouldReturnAviation_WhenProvidedWithAviatonMessageWithAlarms()
        {
            string rawMessage = $"{SOH}PW  7{STX}02    17    84 /// // // // ////// ////// ////  22.9 /////{ETX}";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaAviationMessage>();

            var actualMessage = message as VaisalaAviationMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Aviation);
            actualMessage.SensorID.Should().Be("7");
            actualMessage.HardwareAlarm.Should().Be(HardwareAlarm.HardwareWarning);
            actualMessage.VisibilityAlarm.Should().Be(VisibilityAlarm.None);
            
            AssertValue(actualMessage.OneMinuteAverageVisibility, 17);
            AssertValue(actualMessage.TenMinuteAverageVisibility, 84);
            AssertNoValue(actualMessage.NWSWeatherCode);
            AssertNoValue(actualMessage.InstantaneousWeather);
            AssertNoValue(actualMessage.Weather15Minute);
            AssertNoValue(actualMessage.Weather1Hour);
            AssertNoValue(actualMessage.OneMinuteWaterIntensity);
            AssertNoValue(actualMessage.CumulativeWater);
            AssertNoValue(actualMessage.CumulativeSnow);
            AssertValue(actualMessage.Temperature, 22.9f);
            AssertNoValue(actualMessage.BackgroundLuminance);
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnStationStatus_WhenProvidedWithStatusMessage()
        {
            string rawMessage = $"{SOH}PW  7{STX}PWD STATUS\r\nVAISALA PWD12 V 2.05   2012-03-16 SN:M4151153 ID STRING: 7\r\n\r\nSIGNAL      1.08 OFFSET    148.21 DRIFT      0.54\r\n REC. BACKSCATTER      593  CHANGE    -63\r\n TR. BACKSCATTER      -1.4  CHANGE   -0.0\r\n LEDI    3.1  AMBL    -1.0\r\n VBB    12.4  P12     11.4  M12     -11.2\r\n TS     20.3  TB        19\r\n TDRD     18  DRD    *   0  DRY   *   0.0\r\n HOOD HEATERS OFF\r\n HARDWARE :\r\n OK\r\n WARNINGS :\r\n DRD ERROR\r\n{ETX}\r\n";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaStationStatusMessage>();

            var actualMessage = message as VaisalaStationStatusMessage;
            actualMessage.Should().NotBeNull();

            actualMessage.MessageType.Should().Be(VaisalaMessageType.Status);
            actualMessage.SensorID.Should().Be("7");
            
            AssertValue(actualMessage.FirmwareVersion, "2.05");
            AssertValue(actualMessage.FirmwareDate, "2012-03-16");
            AssertValue(actualMessage.SerialNumber, "SN:M4151153");
            AssertValue(actualMessage.Signal, 1.08f);
            AssertValue(actualMessage.Offset, 148.21f);
            AssertValue(actualMessage.Drift, 0.54f);
            AssertValue(actualMessage.ReceiverBackscatter, 593);
            AssertValue(actualMessage.ReceiverBackscatterChange, -63);
            AssertValue(actualMessage.TransmitterBackscatter, -1.4f);
            AssertValue(actualMessage.TransmitterBackscatterChange, -0.0f);
            AssertValue(actualMessage.TransmitterIntensity, 3.1f);
            AssertValue(actualMessage.AmbientLight, -1.0f);
            AssertValue(actualMessage.BatteryVoltage, 12.4f);
            AssertValue(actualMessage.DCDCPositiveRail, 11.4f);
            AssertValue(actualMessage.DCDCNegativeRail, -11.2f);
            AssertValue(actualMessage.AmbientTemperature, 20.3f);
            AssertValue(actualMessage.CPUTemperature, 19f);
            AssertValue(actualMessage.RainCapTemperature, 18f);
            AssertValueInAlarm(actualMessage.RainCapValue, 0f);
            AssertValueInAlarm(actualMessage.RainCapDryValue, 0.0f);
            AssertValue(actualMessage.HoodHeaters, "OFF");
            
            actualMessage.Relay1State.Should().BeNull();
            actualMessage.Relay2State.Should().BeNull();
            actualMessage.Relay3State.Should().BeNull();
            actualMessage.BackgroundLuminance.Should().BeNull();
            actualMessage.RainCapValue.InAlarm.Should().BeTrue();
            actualMessage.RainCapDryValue.InAlarm.Should().BeTrue();

            actualMessage.HardwareAlarms.Should().BeEmpty();
            actualMessage.Warnings.Should().Contain("DRD ERROR");
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnStationStatus_WhenProvidedWithStatusMessageWithExtendedInformation()
        {
            string rawMessage = $"{SOH}PW  7{STX}PWD STATUS\r\nVAISALA PWD12 V 2.05   2012-03-16 SN:M4151153 ID STRING: 7\r\n\r\nSIGNAL      1.08 OFFSET    148.21 DRIFT      0.54\r\nREC. BACKSCATTER      593  CHANGE    -63\r\nTR. BACKSCATTER      -1.4  CHANGE   -0.0\r\nLEDI    3.1  AMBL    -1.0\r\nVBB    12.4  P12     11.4  M12     -11.2\r\nTS     20.3  TB        19\r\nTDRD     18  DRD    *   0  DRY   *   0.0\r\nBL       29\r\nRELAYS  OFF OFF OFF\r\n\r\nHOOD HEATERS OFF\r\nHARDWARE :\r\n OK\r\nWARNINGS :\r\n DRD ERROR\r\n{ETX}\r\n";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaStationStatusMessage>();

            var actualMessage = message as VaisalaStationStatusMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Status);
            actualMessage.SensorID.Should().Be("7");
            
            AssertValue(actualMessage.FirmwareVersion, "2.05");
            AssertValue(actualMessage.FirmwareDate, "2012-03-16");
            AssertValue(actualMessage.SerialNumber, "SN:M4151153");
            AssertValue(actualMessage.Signal, 1.08f);
            AssertValue(actualMessage.Offset, 148.21f);
            AssertValue(actualMessage.Drift, 0.54f);
            AssertValue(actualMessage.ReceiverBackscatter, 593);
            AssertValue(actualMessage.ReceiverBackscatterChange, -63);
            AssertValue(actualMessage.TransmitterBackscatter, -1.4f);
            AssertValue(actualMessage.TransmitterBackscatterChange, -0.0f);
            AssertValue(actualMessage.TransmitterIntensity, 3.1f);
            AssertValue(actualMessage.AmbientLight, -1.0f);
            AssertValue(actualMessage.BatteryVoltage, 12.4f);
            AssertValue(actualMessage.DCDCPositiveRail, 11.4f);
            AssertValue(actualMessage.DCDCNegativeRail, -11.2f);
            AssertValue(actualMessage.AmbientTemperature, 20.3f);
            AssertValue(actualMessage.CPUTemperature, 19f);
            AssertValue(actualMessage.RainCapTemperature, 18f);
            AssertValueInAlarm(actualMessage.RainCapValue, 0f);
            AssertValueInAlarm(actualMessage.RainCapDryValue, 0.0f);
            AssertValue(actualMessage.HoodHeaters, "OFF");
            AssertValue(actualMessage.BackgroundLuminance, 29);
            AssertValue(actualMessage.Relay1State, "OFF");
            AssertValue(actualMessage.Relay2State, "OFF");
            AssertValue(actualMessage.Relay3State, "OFF");

            actualMessage.RainCapValue.InAlarm.Should().BeTrue();
            actualMessage.RainCapDryValue.InAlarm.Should().BeTrue();

            actualMessage.HardwareAlarms.Should().BeEmpty();
            actualMessage.Warnings.Should().Contain("DRD ERROR");
        }

        [Fact]
        public void VaisalaMessage_ShouldReturnStationStatus_WhenProvidedWithStatusMessageWithExtendedInformation2()
        {
            string rawMessage = $"{SOH}PW  7{STX}PWD STATUS\r\nVAISALA PWD12 V 2.05   2012-03-16 SN:M4151153 ID STRING: 7\r\n\r\n SIGNAL      4.50 OFFSET    148.20 DRIFT      0.53\r\n REC. BACKSCATTER      500  CHANGE    -10\r\n TR. BACKSCATTER      -1.3  CHANGE   -0.0\r\n LEDI    3.1  AMBL    -1.0\r\n VBB    12.4  P12     11.4  M12     -11.2\r\n TS     18.6  TB        22\r\n TDRD     18  DRD    *   0  DRY   *1002.5\r\n HOOD HEATERS OFF\r\n HARDWARE :\r\n OK\r\n WARNINGS :\r\n DRD ERROR\r\n{ETX}\r\n";

            var message = VaisalaMessage.Parse(rawMessage);

            message.Should().BeOfType<VaisalaStationStatusMessage>();

            var actualMessage = message as VaisalaStationStatusMessage;
            actualMessage.Should().NotBeNull();
            
            actualMessage.MessageType.Should().Be(VaisalaMessageType.Status);
            actualMessage.SensorID.Should().Be("7");
            
            AssertValue(actualMessage.FirmwareVersion, "2.05");
            AssertValue(actualMessage.FirmwareDate, "2012-03-16");
            AssertValue(actualMessage.SerialNumber, "SN:M4151153");
            AssertValue(actualMessage.Signal, 4.5f);
            AssertValue(actualMessage.Offset, 148.2f);
            AssertValue(actualMessage.Drift, 0.53f);
            AssertValue(actualMessage.ReceiverBackscatter, 500);
            AssertValue(actualMessage.ReceiverBackscatterChange, -10);
            AssertValue(actualMessage.TransmitterBackscatter, -1.3f);
            AssertValue(actualMessage.TransmitterBackscatterChange, -0.0f);
            AssertValue(actualMessage.TransmitterIntensity, 3.1f);
            AssertValue(actualMessage.AmbientLight, -1.0f);
            AssertValue(actualMessage.BatteryVoltage, 12.4f);
            AssertValue(actualMessage.DCDCPositiveRail, 11.4f);
            AssertValue(actualMessage.DCDCNegativeRail, -11.2f);
            AssertValue(actualMessage.AmbientTemperature, 18.6f);
            AssertValue(actualMessage.CPUTemperature, 22f);
            AssertValue(actualMessage.RainCapTemperature, 18f);
            AssertValueInAlarm(actualMessage.RainCapValue, 0f);
            AssertValueInAlarm(actualMessage.RainCapDryValue, 1002.5f);
            AssertValue(actualMessage.HoodHeaters, "OFF");

            actualMessage.RainCapValue.InAlarm.Should().BeTrue();
            actualMessage.RainCapDryValue.InAlarm.Should().BeTrue();

            actualMessage.HardwareAlarms.Should().BeEmpty();
            actualMessage.Warnings.Should().Contain("DRD ERROR");
        }
        
        private static void AssertValue<T>(VaisalaValue<T> value, T expectedValue)
        {
            value.Should().NotBeNull();
            value.HasValue.Should().BeTrue();
            value.Value.Should().Be(expectedValue);

            if (value is VaisalaAlarmValue<T> alarmValue)
                alarmValue.InAlarm.Should().BeFalse();
        }

        private static void AssertValueInAlarm<T>(VaisalaAlarmValue<T> value, T expectedValue)
        {
            value.Should().NotBeNull();
            value.HasValue.Should().BeTrue();
            value.InAlarm.Should().BeTrue();
            value.Value.Should().Be(expectedValue);
        }

        private static void AssertNoValue<T>(VaisalaValue<T> value)
        {
            value.Should().NotBeNull();
            value.HasValue.Should().BeFalse();
        }
    }
}

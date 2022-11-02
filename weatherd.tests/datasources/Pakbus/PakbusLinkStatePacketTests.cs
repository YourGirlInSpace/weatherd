using System.Linq;
using FluentAssertions;
using weatherd.datasources.pakbus;
using Xunit;

namespace weatherd.tests.datasources.Pakbus
{
    public class PakbusLinkStatePacketTests
    {
        [Fact]
        public void Encode_WithValidData_ShouldEmitCorrectByteSequence()
        {
            // Arrange
            byte[] expectedSequence = Utilities.StringToByteArrayFastest("BD90010FFC75D4BD");
            PakbusLinkStatePacket packet = PakbusLinkStatePacket.FromState(4092, 1, PakbusLinkState.Ring);

            // Act
            byte[] data = packet.Encode().ToArray();

            // Assert
            data.Should().Equal(expectedSequence);
        }
    }
}

using FluentAssertions;
using weatherd.datasources.pakbus;
using Xunit;

namespace weatherd.tests.datasources.Pakbus
{
    public class PakbusHeaderTests
    {
        [Fact]
        public void PakbusHeader_DecodeNormal_ValidResult()
        {
            // Arrange
            byte[] headerData = Utilities.StringToByteArrayFastest("A0645FFE10640FFE");

            // Act
            var header = PakbusHeader.Decompile(headerData);

            // Assert
            header.Type.Should().Be(PakbusHeaderType.Normal);
            header.SourceNodeID.Should().Be(4094);
            header.DestinationNodeID.Should().Be(100);
            header.SourcePhysicalAddress.Should().Be(4094);
            header.DestinationPhysicalAddress.Should().Be(100);
            header.LinkState.Should().Be(PakbusLinkState.Ready);
            header.ExpectModeCode.Should().Be(1);
            header.HopCount.Should().Be(0);
            header.Protocol.Should().Be(PakbusProtocol.BMP);
        }

        [Fact]
        public void PakbusHeader_DecodeCompressedLinkState_ValidResult()
        {
            // Arrange
            byte[] headerData = Utilities.StringToByteArrayFastest("90010FFC");

            // Act
            var header = PakbusHeader.Decompile(headerData);

            // Assert
            header.Type.Should().Be(PakbusHeaderType.CompressedLinkState);
            header.SourcePhysicalAddress.Should().Be(4092);
            header.DestinationPhysicalAddress.Should().Be(1);
            header.LinkState.Should().Be(PakbusLinkState.Ring);
            header.ExpectModeCode.Should().Be(0);
            header.HopCount.Should().Be(0);
            header.Protocol.Should().Be(PakbusProtocol.LinkState);
        }
    }
}

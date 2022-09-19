using FluentAssertions;
using weatherd.datasources.pakbus;
using weatherd.io;
using Xunit;

namespace weatherd.tests.datasources.Pakbus
{
    public class PakbusBinaryStreamTests
    {
        [Fact]
        public void WriteUSec_WithValidTimestamp_ShouldEmitValidByteSequence()
        {
            // Arrange
            byte[] expectedByteArray = { 0x5D, 0xCE, 0x23, 0xEC, 0xB8, 0xC4 };
            NSec nsec = new NSec(1031399473, 62500000);
            PakbusBinaryStream bs = new PakbusBinaryStream(Endianness.Big);

            // Act
            bs.WriteUSec(nsec);
            byte[] result = bs.ToArray();

            // Assert
            result.Should().NotBeNull();
            result.Should().Equal(expectedByteArray);
        }

        [Fact]
        public void ReadUSec_WithValidByteSequence_ShouldReturnValidNSec()
        {
            // Arrange
            byte[] data = { 0x5D, 0xCE, 0x23, 0xEC, 0xB8, 0xC4 };
            
            NSec expectedNsec = new NSec(1031399473, 62500000);
            PakbusBinaryStream bs = new PakbusBinaryStream(data, Endianness.Big);

            // Act
            NSec result = bs.ReadUSec();

            // Assert
            result.Should().NotBeNull();
            result.Seconds.Should().Be(expectedNsec.Seconds);
            result.Nanoseconds.Should().Be(expectedNsec.Nanoseconds);
        }
    }
}

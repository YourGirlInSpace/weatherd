using FluentAssertions;
using weatherd.datasources.pakbus;
using weatherd.datasources.pakbus.Messages.BMP5;
using Xunit;

namespace weatherd.tests.datasources.Pakbus.Messages.BMP5
{
    public class PakbusDataCollectCommandMessageTests
    {
        [Fact]
        public void Decode_GetLastRecord_WithValidData_ShouldPresentValidParameters()
        {
            // Arrange

            // Note: This is the message portion of the packet, with the boundary byte
            //       bytes have been stripped
            byte[] messageData = DecodeHex("09DC000005000477D80000000100");
            const int TransactionNumber = 0xDC;
            const int SecurityCode = 0;
            const int TableNumber = 4;
            const int TableSignature = 0x77D8;
            const PakbusCollectionMode CollectMode = PakbusCollectionMode.GetLastRecord;
            const int P1 = 1;
            const int P2 = 0;

            // Act
            PakbusDataCollectCommandMessage cmdMessage = PakbusMessage.Decompile(PakbusProtocol.BMP, messageData) as PakbusDataCollectCommandMessage;
            
            // Assert
            cmdMessage.Should().NotBeNull();
            cmdMessage.TransactionNumber.Should().Be(TransactionNumber);
            cmdMessage.SecurityCode.Should().Be(SecurityCode);
            cmdMessage.TableNumber.Should().Be(TableNumber);
            cmdMessage.TableSignature.Should().Be(TableSignature);
            cmdMessage.CollectMode.Should().Be(CollectMode);
            cmdMessage.P1.Should().Be(P1);
            cmdMessage.P2.Should().Be(P2);
        }

        
        [Fact]
        public void Encode_GetLastRecord_WithValidData_ShouldProvideValidBody()
        {
            // Arrange

            // Note: This is the message portion of the packet, the header and surrounding
            //       bytes have been stripped
            byte[] expectedData = DecodeHex("09DC000005000477D80000000100");
            const int TransactionNumber = 0xDC;
            const int SecurityCode = 0;
            const int TableNumber = 4;
            const int TableSignature = 0x77D8;
            const PakbusCollectionMode CollectMode = PakbusCollectionMode.GetLastRecord;
            const int P1 = 1;
            const int P2 = 0;

            // Act
            PakbusDataCollectCommandMessage cmdMessage =
                new PakbusDataCollectCommandMessage(TransactionNumber, TableNumber, TableSignature, SecurityCode, CollectMode, P1, P2);

            byte[] actualData = cmdMessage.Encode();
            
            // Assert
            actualData.Should().NotBeNull();
            actualData.Should().Equal(expectedData);
        }

        private static byte[] DecodeHex(string hex)
            => Utilities.StringToByteArrayFastest(hex.Replace(" ", ""));
    }
}

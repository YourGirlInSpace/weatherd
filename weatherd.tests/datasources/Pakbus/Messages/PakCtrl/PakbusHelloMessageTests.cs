using FluentAssertions;
using weatherd.datasources.pakbus;
using weatherd.datasources.pakbus.Messages.PakCtrl;
using Xunit;

namespace weatherd.tests.datasources.Pakbus.Messages.PakCtrl
{
    public class PakbusHelloMessageTests
    {
        [Fact]
        public void Encode_WithValidData_ShouldProvideValidBody()
        {
            // Arrange

            // Note: This is the message portion of the packet, the header and surrounding
            //       bytes have been stripped
            byte[] expectedData = DecodeHex("BDA0016FFC00010FFC09D90102FFFFDE86BD");
            const int TransactionNumber = 217;


            // Act
            PakbusHeader header = new PakbusHeader(PakbusHeaderType.Normal, 1, 4092, 1, 4092, PakbusProtocol.PakCtrl, 1,
                                                   PakbusLinkState.Ready, PakbusPriority.High, 0);

            PakbusHelloMessage cmdMessage =
                new PakbusHelloMessage(TransactionNumber)
                {
                    IsRouter = 1,
                    HopMetric = 2
                };

            PakbusPacket packet = new PakbusPacket(header, cmdMessage);

            byte[] actualData = packet.Encode();
            
            // Assert
            actualData.Should().NotBeNull();
            actualData.Should().Equal(expectedData);
        }
        
        private static byte[] DecodeHex(string hex)
            => Utilities.StringToByteArrayFastest(hex.Replace(" ", ""));
    }
}

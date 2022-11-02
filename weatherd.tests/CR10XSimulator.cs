using System;
using System.Collections.Generic;
using weatherd.datasources.pakbus;
using weatherd.datasources.pakbus.Messages.BMP5;
using weatherd.datasources.pakbus.Messages.PakCtrl;

namespace weatherd.tests
{
    public class CR10XSimulator
    {
        /// <summary>
        /// Interprets a packet received from a byte stream, then formulates a reply.
        /// </summary>
        /// <param name="packetData"></param>
        /// <returns></returns>
        public static IEnumerable<byte> HandlePacket(byte[] packetData)
        {
            PakbusPacket packet = PakbusPacket.Decode(packetData);

            return packet.Message switch
            {
                PakbusHelloMessage _                      => GenerateHelloReply(packet),
                PakbusXTDGetTableDefinitionsCommand _     => GenerateTableResponse(packet),
                PakbusXTDClockCommand _                   => GenerateClockResponse(packet),
                PakbusDataCollectCommandMessage _         => GenerateDataCollectionResponse(packet),
                null when packet is PakbusLinkStatePacket => GenerateLinkStateReply(packet),
                _ => throw new NotImplementedException($"Could not handle packet type {packet.Message?.MessageType}")
            };
        }

        private static IEnumerable<byte> GenerateLinkStateReply(PakbusPacket linkStatePacket)
        {
            var from = linkStatePacket.Header.DestinationNodeID;
            var to = linkStatePacket.Header.SourceNodeID;

            return PakbusLinkStatePacket.FromState(from, to, PakbusLinkState.Ready).Encode();
        }

        private static IEnumerable<byte> GenerateHelloReply(PakbusPacket packet)
        {
            if (!(packet.Message is PakbusHelloMessage helloMessage))
                return Array.Empty<byte>();

            var header = GetReplyHeader(packet.Header);

            PakbusHelloResponseMessage hrm =
                new PakbusHelloResponseMessage(packet.Message.TransactionNumber, 0, helloMessage.HopMetric);

            PakbusPacket responsePacket = new PakbusPacket(header, hrm);
            return responsePacket.Encode();
        }

        private static IEnumerable<byte> GenerateTableResponse(PakbusPacket packet)
        {
            if (!(packet.Message is PakbusXTDGetTableDefinitionsCommand getTblCmd))
                return Array.Empty<byte>();

            var header = GetReplyHeader(packet.Header);

            PakbusMessage msg = getTblCmd.FragmentNumber switch
            {
                0 =>
                    // This is a special case
                    new RawPakbusMessage(PakbusMessageType.BMP5_XTDGetTableDefinitionsResponse,
                                         packet.Message.TransactionNumber,
                                         new byte[]
                                         {
                                             0x00, 0x80, 0x00, 0x53, 0x74, 0x61, 0x74, 0x75, 0x73, 0x00, 0x00, 0x00,
                                             0x00, 0x01, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x42, 0x61,
                                             0x74, 0x74, 0x65, 0x72, 0x79, 0x00, 0x00, 0x01, 0x01, 0x57, 0x61, 0x74,
                                             0x63, 0x68, 0x44, 0x6F, 0x67, 0x00, 0x00, 0x01, 0x01, 0x4F, 0x76, 0x65,
                                             0x72, 0x52, 0x75, 0x6E, 0x73, 0x00, 0x00, 0x01, 0x02, 0x49, 0x6E, 0x4C,
                                             0x6F, 0x63, 0x73, 0x00, 0x00, 0x01, 0x02, 0x50, 0x72, 0x67, 0x6D, 0x46,
                                             0x72, 0x65, 0x65, 0x00, 0x00, 0x01, 0x03, 0x53, 0x74, 0x6F, 0x72, 0x61,
                                             0x67, 0x65, 0x00, 0x00, 0x01, 0x02, 0x54, 0x61, 0x62, 0x6C, 0x65, 0x73,
                                             0x00, 0x00, 0x01, 0x08, 0x44, 0x61, 0x79, 0x73, 0x46, 0x75, 0x6C, 0x6C,
                                             0x00, 0x00, 0x01, 0x02, 0x48, 0x6F, 0x6C, 0x65, 0x73, 0x00, 0x00, 0x01,
                                             0x02, 0x50, 0x72, 0x67, 0x6D, 0x53, 0x69, 0x67, 0x00, 0x00, 0x01, 0x02,
                                             0x4F, 0x53, 0x53, 0x69, 0x67, 0x00, 0x00, 0x01, 0x08, 0x4F, 0x53, 0x49,
                                             0x44, 0x00, 0x00, 0x01, 0x02, 0x4F, 0x62, 0x6A, 0x53, 0x72, 0x6C, 0x4E,
                                             0x6F, 0x00, 0x00, 0x01, 0x02, 0x53, 0x74, 0x6E, 0x49, 0x44, 0x00, 0x00,
                                             0x01, 0x08, 0x4C, 0x69, 0x74, 0x68, 0x42, 0x61, 0x74, 0x00, 0x00, 0x01,
                                             0x02, 0x41, 0x64, 0x76, 0x69, 0x73, 0x65, 0x53, 0x69, 0x67, 0x00, 0x00,
                                             0x01, 0x00, 0x54, 0x69, 0x6D, 0x65, 0x53, 0x65, 0x74, 0x00, 0x00, 0x00,
                                             0x00, 0x0A, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0D, 0x4F, 0x6C,
                                             0x64, 0x54, 0x69, 0x6D, 0x65, 0x00, 0x00, 0x01, 0x00, 0x45, 0x72, 0x72,
                                             0x6F, 0x72, 0x4C, 0x6F, 0x67, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x0D, 0x00,
                                             0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x43, 0x6F, 0x64, 0x65, 0x00, 0x00,
                                             0x01, 0x00, 0x49, 0x6E, 0x6C, 0x6F, 0x63, 0x73, 0x00, 0x00, 0x00, 0x00,
                                             0x01, 0x0D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x46, 0x6C, 0x61,
                                             0x67, 0x73, 0x00, 0x00, 0x01, 0x11, 0x50, 0x6F, 0x72, 0x74, 0x73, 0x00,
                                             0x00, 0x01, 0x08, 0x42, 0x61, 0x74, 0x74, 0x56, 0x00, 0x00, 0x01, 0x08,
                                             0x50, 0x72, 0x6F, 0x67, 0x53, 0x69, 0x67, 0x00, 0x00, 0x01, 0x08, 0x57,
                                             0x44, 0x69, 0x72, 0x5F, 0x64, 0x65, 0x67, 0x00, 0x00, 0x01, 0x08, 0x57,
                                             0x53, 0x70, 0x64, 0x5F, 0x6D, 0x70, 0x68, 0x00, 0x00, 0x01, 0x08, 0x42,
                                             0x50, 0x72, 0x73, 0x5F, 0x68, 0x50, 0x61, 0x00, 0x00, 0x01, 0x08, 0x41,
                                             0x69, 0x72, 0x54, 0x43, 0x00, 0x00, 0x01, 0x08, 0x52, 0x48, 0x00, 0x00,
                                             0x01, 0x08, 0x53, 0x6C, 0x72, 0x57, 0x00, 0x00, 0x01, 0x08, 0x53, 0x6C,
                                             0x72, 0x4D, 0x4A, 0x00, 0x00, 0x01, 0x08, 0x52, 0x61, 0x69, 0x6E, 0x5F,
                                             0x6D, 0x6D, 0x00, 0x00, 0x01, 0x08, 0x50, 0x54, 0x65, 0x6D, 0x70, 0x5F,
                                             0x43, 0x00, 0x00, 0x01, 0x00
                                         }),
                1 => new RawPakbusMessage(PakbusMessageType.BMP5_XTDGetTableDefinitionsResponse,
                                          packet.Message.TransactionNumber, new byte[] { 0x00, 0x00, 0x01 }),
                _ => throw new InvalidOperationException()
            };

            PakbusPacket responsePacket = new PakbusPacket(header, msg);
            return responsePacket.Encode();
        }

        private static IEnumerable<byte> GenerateClockResponse(PakbusPacket packet)
        {
            if (!(packet.Message is PakbusXTDClockCommand clockCmd))
                return Array.Empty<byte>();

            var header = GetReplyHeader(packet.Header);

            PakbusXTDClockResponse resp =
                new PakbusXTDClockResponse(clockCmd.TransactionNumber, PakbusXTDResponseCode.ClockNotChanged,
                                           DateTime.UtcNow);
            
            PakbusPacket responsePacket = new PakbusPacket(header, resp);
            return responsePacket.Encode();
        }

        private static IEnumerable<byte> GenerateDataCollectionResponse(PakbusPacket packet)
        {
            if (!(packet.Message is PakbusDataCollectCommandMessage collectMsg))
                return Array.Empty<byte>();

            var header = GetReplyHeader(packet.Header);

            RawPakbusMessage rawMessage = new RawPakbusMessage(PakbusMessageType.BMP5_CollectDataResponse,
                                                               collectMsg.TransactionNumber, new byte[]
                                                               {
                                                                   0x00, 0x00, 0x04, 0x00, 0x00, 0x06, 0xE4, 0x00, 0x01,
                                                                   0x5D, 0xDD, 0x7A, 0x7D, 0x7D, 0x40, 0x00, 0x00, 0x44,
                                                                   0xDC, 0x4C, 0x4C, 0x00, 0x00, 0x00, 0x00, 0x48, 0xC1,
                                                                   0x4E, 0xEF, 0xBC, 0xDD, 0x90, 0xC8, 0x00, 0x49, 0xED,
                                                                   0x80, 0x00, 0x45, 0xBB, 0x37, 0x4B, 0x46, 0xEE, 0xB2,
                                                                   0x2D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                                   0x00, 0x00, 0x00, 0x00, 0x45, 0xBC, 0xDC, 0x25, 0x2C,
                                                                   0x00
                                                               });


            PakbusPacket responsePacket = new PakbusPacket(header, rawMessage);
            return responsePacket.Encode();
        }

        private static PakbusHeader GetReplyHeader(PakbusHeader header)
        {
            var from = header.DestinationNodeID;
            var to = header.SourceNodeID;

            return PakbusHeader.Create(from, to, header.Protocol);
        }
    }
}

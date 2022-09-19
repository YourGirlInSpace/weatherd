using System;
using System.Linq;
using Serilog;

namespace weatherd.datasources.pakbus
{
    public class PakbusLinkStatePacket : PakbusPacket
    {
        private PakbusLinkStatePacket(PakbusHeader header)
            : base(header, null)
        { }

        public override byte[] Encode()
        {
            byte[] encodedFrame = Header.Encode();
            
            byte[] signature = PakbusUtilities.CalculateSignatureNullifier(PakbusUtilities.ComputeSignature(encodedFrame, 4));
            Array.Resize(ref encodedFrame, encodedFrame.Length + signature.Length);
            
            Array.Copy(signature, 0, encodedFrame, encodedFrame.Length - signature.Length, signature.Length);

            encodedFrame = Quote(encodedFrame).ToArray();

            byte[] encodedPacket = new byte[2 + encodedFrame.Length];
            encodedPacket[0] = encodedPacket[^1] = PacketBoundary;

            Array.Copy(encodedFrame, 0, encodedPacket, 1, encodedFrame.Length);
            return encodedPacket;
        }

        public static PakbusLinkStatePacket FromState(uint from, uint to, PakbusLinkState linkState)
        {
            PakbusHeader header = new PakbusHeader(PakbusHeaderType.CompressedLinkState, 0, 0, to, from,
                                                   PakbusProtocol.LinkState, 0, linkState, 0, 0);

            return new PakbusLinkStatePacket(header);
        }

        public new static PakbusLinkStatePacket Decode(byte[] packet)
        {
            byte[] packetContent = new byte[packet.Length - 2];
            Array.Copy(packet, 1, packetContent, 0, packetContent.Length);

            byte[] unquoted = Unquote(packetContent).ToArray();
            
            PakbusHeader header = PakbusHeader.Decompile(unquoted[..^2]);

            bool isTransmit = header.SourcePhysicalAddress == 4092;
            Log.Debug("[Pakbus {txrx}] Link State: {linkStateCode:X} {linkState} from {srcAddr}", isTransmit ? "TX" : "RX", (byte) header.LinkState, header.LinkState, header.SourcePhysicalAddress);
            
            return new PakbusLinkStatePacket(header);
        }
    }
}

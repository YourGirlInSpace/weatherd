using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Serilog;
using weatherd.datasources.Pakbus.Messages.PakCtrl;

namespace weatherd.datasources.Pakbus
{
    public class PakbusPacket
    {
        public const int MaxLength = 1112;

        public PakbusHeader Header { get; }
        public PakbusMessage Message { get; }

        protected const byte PacketBoundary = 0xBD;

        private static uint _transactionNumber = 127;
        private static readonly object _locker = new();
        
        public PakbusPacket(PakbusHeader header, PakbusMessage message)
        {
            Header = header;
            Message = message;
        }
        
        public static byte GenerateNewTransactionNumber()
        {
            lock (_locker)
            {
                _transactionNumber++;
                _transactionNumber &= 0xFF;
            }
            Log.Verbose("Generated transaction ID {transId}", _transactionNumber);

            return (byte) _transactionNumber;
        }

        private byte[] EncodeFrame()
        {
            byte[] encodedHeader = Header.Encode();
            byte[] encodedMessage = Message.Encode();

            byte[] encodedPacket = new byte[encodedHeader.Length + encodedMessage.Length];
            Array.Copy(encodedHeader, 0, encodedPacket, 0, encodedHeader.Length);
            Array.Copy(encodedMessage, 0, encodedPacket, encodedHeader.Length, encodedMessage.Length);

            byte[] signature = PakbusUtilities.CalculateSignatureNullifier(PakbusUtilities.ComputeSignature(encodedPacket, encodedPacket.Length));
            Array.Resize(ref encodedPacket, encodedPacket.Length + signature.Length);
            
            Array.Copy(signature, 0, encodedPacket, encodedPacket.Length - signature.Length, signature.Length);

            return encodedPacket;
        }

        protected static IEnumerable<byte> Quote(IEnumerable<byte> data)
        {
            foreach (byte b in data)
            {
                switch (b)
                {
                    case (byte) '\xBC':
                        yield return (byte) '\xBC';
                        yield return (byte) '\xDC';
                        break;
                    case (byte) '\xBD':
                        yield return (byte)'\xBC';
                        yield return (byte)'\xDD';
                        break;
                    default:
                        yield return b;
                        break;
                }
            }
        }

        protected static IEnumerable<byte> Unquote(IEnumerable<byte> data)
        {
            byte[] buf = data.ToArray();
            int i = 0;
            while (i < buf.Length)
            {
                if (buf[i] == 0xBC)
                {
                    i++;
                    yield return buf[i] switch
                    {
                        0xDD => 0xBD, // BC DD => BD
                        0xDC => 0xBC, // BC DC => BC
                        _ => buf[i]
                    };
                    i++;
                } else
                    yield return buf[i++];
            }
        }

        public virtual byte[] Encode()
        {
            byte[] frame = Quote(EncodeFrame()).ToArray();
            byte[] compositePacket = new byte[2 + frame.Length];
            compositePacket[0] = compositePacket[^1] = PacketBoundary;

            Array.Copy(frame, 0, compositePacket, 1, frame.Length);
            return compositePacket;
        }

        public static PakbusPacket Decode(byte[] data)
            => Decode(data, data.Length);

        public static PakbusPacket Decode(byte[] data, int length)
        {
            byte[] packet = data[..length];

            if (packet[0] != 0xBD || packet[^1] != 0xBD)
            {
                Log.Error("Malformed packet");
                return null;
            }

            // Special cases
            switch (packet.Length)
            {
                case 8:
                    return PakbusLinkStatePacket.Decode(packet);
                case 12:
                    return PakbusLinkStatePacket.Decode(packet);
                case <= 14:
                    return null;
            }

            byte[] packetContent = new byte[packet.Length - 2];
            Array.Copy(packet, 1, packetContent, 0, packetContent.Length);

            byte[] unquoted = Unquote(packetContent).ToArray();

            if (PakbusUtilities.ComputeSignature(unquoted, unquoted.Length) != 0)
            {
                Log.Warning("Malformed packet: signature was not zero");
                return null;
            }

            PakbusHeader header = PakbusHeader.Decompile(unquoted[..8]);
            PakbusMessage message = PakbusMessage.Decompile(header.Protocol, unquoted[8..^2]);

            if (message is not null)
            {
                bool isTransmit = header.SourcePhysicalAddress == 4092;

                Log.Debug(
                    "[Pakbus {txrx}] {msgType} [{msgTypeByte:X}] (tx={transNum}) from {sourceNode} to {destNode}",
                    isTransmit ? "TX" : "RX",
                    message.MessageType, (byte)message.MessageType & 0xFF, message.TransactionNumber,
                    header.SourceNodeID, header.DestinationNodeID);
            }


            return new PakbusPacket(header, message);
        }
    }
}

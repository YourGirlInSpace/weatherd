using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using UnitsNet;
using UnitsNet.Units;

namespace weatherd.datasources.pakbus
{
    public class PakbusPacket
    {
        public const int MaxLength = 1112;
        public const int SignatureLength = 2;

        public PakbusHeader Header { get; }
        public PakbusMessage Message { get; }

        public const byte PacketBoundary = 0xBD;

        // A bit arbitrary, I chose to start transaction numbers at 127.  Sue me.
        private static uint _transactionNumber = 127;
        private static readonly object _locker = new();
        
        public PakbusPacket(PakbusHeader header)
            : this(header, null)
        { }
        
        public PakbusPacket(PakbusHeader header, PakbusMessage message)
        {
            Header = header;
            Message = message;
        }
        
        /// <summary>
        /// Generates a new transaction number in a thread-safe context.
        /// </summary>
        /// <returns>The new transaction number.</returns>
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
        
        private IEnumerable<byte> EncodeFrame()
        {
            byte[] encodedHeader  = Header.Encode();
            byte[] encodedMessage = Message.Encode();

            byte[] buffer = new byte[encodedHeader.Length + encodedMessage.Length + SignatureLength];
            
            Buffer.BlockCopy(encodedHeader,  0, buffer, 0, encodedHeader.Length);
            Buffer.BlockCopy(encodedMessage, 0, buffer, encodedHeader.Length, encodedMessage.Length);

            ushort signature = PakbusUtilities.ComputeSignature(buffer, encodedHeader.Length + encodedMessage.Length);
            byte[] signatureNullifier = PakbusUtilities.CalculateSignatureNullifier(signature);
            
            Buffer.BlockCopy(signatureNullifier, 0, buffer, encodedHeader.Length + encodedMessage.Length, signatureNullifier.Length);

            return buffer;
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

        public virtual IEnumerable<byte> Encode()
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

            if (message is null)
                return new PakbusPacket(header);

            bool isTransmit = header.SourcePhysicalAddress == 4092;

            Log.Debug(
                "[Pakbus {txrx}] {msgType} [{msgTypeByte:X}] (tx={transNum}) from {sourceNode} to {destNode}",
                isTransmit ? "TX" : "RX",
                message.MessageType, (byte)message.MessageType & 0xFF, message.TransactionNumber,
                header.SourceNodeID, header.DestinationNodeID);
            
            return new PakbusPacket(header, message);
        }
    }
}

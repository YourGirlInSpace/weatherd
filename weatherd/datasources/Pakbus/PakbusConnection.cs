using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using weatherd.datasources.pakbus.Messages.BMP5;
using weatherd.datasources.pakbus.Messages.PakCtrl;
using weatherd.io;

namespace weatherd.datasources.pakbus
{
    public class PakbusConnection
    {
        private const int MaxDataloggerTimeDeviation = 5000;
        private const int MaxTimeBeforeDisconnectionAssumed = 5000;

        public uint LocalNodeID { get; }
        public uint RemoteNodeID { get; }
        public int SecurityCode { get; }
        public bool Connected { get; private set; }
        public bool Faulted { get; private set; }

        /// <summary>
        ///     Instantiates a new PakbusConnection from the local node to a remote node using an existing serial connection.
        /// </summary>
        /// <param name="serialInterface">The serial interface to use</param>
        /// <param name="localNodeId">The local (i.e. this library) node ID</param>
        /// <param name="remoteNodeId">The remote (i.e. the datalogger) node ID.</param>
        internal PakbusConnection(ISerialInterface serialInterface, int localNodeId, int remoteNodeId)
            : this("TEST_PORT", 9600, localNodeId, remoteNodeId)
        {
            _port = serialInterface;
            _usingTestSerialInterface = true;
        }

        /// <summary>
        ///     Instantiates a new PakbusConnection from the local node to a remote node.
        /// </summary>
        /// <param name="comPort">The system serial port name that is connected to the Pakbus network</param>
        /// <param name="baud">The baud rate of the Pakbus network</param>
        /// <param name="localNodeId">The local (i.e. this library) node ID</param>
        /// <param name="remoteNodeId">The remote (i.e. the datalogger) node ID.</param>
        public PakbusConnection(string comPort, int baud, int localNodeId, int remoteNodeId)
            : this(comPort, baud, localNodeId, remoteNodeId, 0)
        {
        }

        /// <summary>
        ///     Instantiates a new PakbusConnection from the local node to a remote node.
        /// </summary>
        /// <param name="comPort">The system serial port name that is connected to the Pakbus network</param>
        /// <param name="baud">The baud rate of the Pakbus network</param>
        /// <param name="localNodeId">The local (i.e. this library) node ID</param>
        /// <param name="remoteNodeId">The remote (i.e. the datalogger) node ID.</param>
        /// <param name="securityCode">The security code of the datalogger.</param>
        public PakbusConnection(string comPort, int baud, int localNodeId, int remoteNodeId, int securityCode)
        {
            if (localNodeId is <= 0 or > 4096)
                throw new ArgumentOutOfRangeException(nameof(localNodeId), "Node ID should be between 0 and 4096.");
            if (remoteNodeId is <= 0 or > 4096)
                throw new ArgumentOutOfRangeException(nameof(remoteNodeId), "Node ID should be between 0 and 4096.");
            if (securityCode is < 0 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(securityCode),
                                                      "Security code should be between 0 and 65535.");

            LocalNodeID = (uint)localNodeId;
            RemoteNodeID = (uint)remoteNodeId;
            SecurityCode = securityCode;

            _portName = comPort;
            _baud = baud;
            _canceller = new CancellationTokenSource();
        }

        private bool OpenPort(string comPort, int baud)
        {
            if (baud <= 0)
                throw new ArgumentOutOfRangeException(nameof(baud));
            if (string.IsNullOrEmpty(comPort))
                throw new ArgumentException("Value cannot be null or empty.", nameof(comPort));

            Log.Information("Opening port {ComPort} ({Baud} 8N1, RTS, DTR, No handshake)", comPort, baud);
            if (_usingTestSerialInterface)
                return _port.IsOpen;

            _port = new SerialInterface(comPort, baud, Parity.None, 8, StopBits.One)
            {
                RtsEnable = true,
                DtrEnable = true,

                ReadTimeout = 2000,
                WriteTimeout = 2000,

                Handshake = Handshake.None
            };

            try
            {
                _port.Open();
            } catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to open {ComPort}", comPort);
                return false;
            }

            return _port.IsOpen;
        }

        private async Task<bool> Connect(string comPort, int baud)
        {
            Connected = false;
            if (!OpenPort(comPort, baud))
                return false;

            if (!await Retry.DoAsync(InitiateConnection))
            {
                Log.Verbose("{Function}: InitiateConnection failed all retries", nameof(Connect));
                return false;
            }

            // This is optional
            await Retry.DoAsync(SyncClock, 1000, 3);

            if (!await Retry.DoAsync(UpdateTableDefinitions))
            {
                Log.Verbose("{Function}: UpdateTableDefinitions failed all retries", nameof(Connect));
                return false;
            }

            Connected = true;
            return Connected;
        }

        public bool Start(Action<PakbusResult> callback, Action<bool> completionCallback)
        {
            Task.Factory.StartNew(async () =>
                                  {
                                      Faulted = !await Retry.DoPerpetuallyAsync(() => DataRunner(callback));
                                      completionCallback(Faulted);
                                  },
                                  _canceller.Token);
            return true;
        }

        public bool Stop()
        {
            _canceller.Cancel();
            return true;
        }

        private async Task<bool> DataRunner(Action<PakbusResult> callback)
        {
            if (!Connected && !await Retry.DoAsync(() => Connect(_portName, _baud)))
                return false;

            while (true)
            {
                if (DateTime.UtcNow - _lastPacketTime > TimeSpan.FromMilliseconds(MaxTimeBeforeDisconnectionAssumed) &&
                    !await Retry.DoAsync(
                        async () => await SendHelloTransaction(LocalNodeID, RemoteNodeID) is not null))
                {
                    Connected = false;
                    return false;
                }

                if (DateTime.UtcNow - _lastClockSetTime > TimeSpan.FromMinutes(5))
                    await SyncClock();

                PakbusDataCollectResponseMessage data;
                try
                {
                    data =
                        await SendCollectDataTransaction(LocalNodeID, RemoteNodeID,
                                                         XTDTableDefinition.Current["Inlocs"]);
                } catch (Exception ex)
                {
                    Log.Error(ex, "Failed to download data");
                    return false;
                }

                if (data is not null)
                {
                    int currentRecNo = (int)data["RECNO"];

                    if (currentRecNo != _lastRecordNumber)
                        try
                        {
                            callback?.Invoke(data.Results);
                        } catch
                        {
                            // not our problem, ignore
                        }

                    _lastRecordNumber = currentRecNo;
                    _lastPacketTime = DateTime.UtcNow;
                }

                await Task.Delay(1000, _canceller.Token);
            }
        }

        private async Task<bool> UpdateTableDefinitions()
        {
            // Download table definitions
            XTDTableDefinition tableDef = await DownloadXTDTableDefinitionsTransaction(LocalNodeID, RemoteNodeID);

            if (tableDef is not null)
                return true;

            Log.Error("Failed to start Pakbus data source");
            Log.Verbose("{Function}: tableDef is null", nameof(UpdateTableDefinitions));
            return false;
        }

        private async Task<bool> SyncClock()
        {
            // Validate the clock
            PakbusXTDClockResponse r = await GetTimeTransaction(LocalNodeID, RemoteNodeID);
            if (r.ResponseCode != PakbusXTDResponseCode.ClockNotChanged)
                throw new InvalidOperationException("Get clock command managed to set the clock!!");

            TimeSpan deviation = DateTime.Now - r.Time.ToTime();
            Log.Information(
                "Datalogger clock is {Time}, which differs from server time by {Deviation} seconds",
                r.Time.ToTime(), deviation.TotalSeconds);
            if (deviation <= TimeSpan.FromMilliseconds(MaxDataloggerTimeDeviation) ||
                await SetClock(LocalNodeID, RemoteNodeID, DateTime.Now))
                return true;

            Log.Warning("Could not set the datalogger's clock");
            return false;
        }

        private async Task<bool> InitiateConnection()
        {
            Log.Verbose("Clearing the buffer...");
            for (int i = 0; i < 5; i++)
            {
                Send(new[] { PakbusPacket.PacketBoundary });
                await Task.Delay(10, _canceller.Token);
            }

            Log.Verbose("Beginning 'ring' transaction...");
            // Let's say hello
            PakbusLinkStatePacket linkStateResponse = await Ring(LocalNodeID, RemoteNodeID);

            if (linkStateResponse is null)
            {
                Log.Error("Failed to start Pakbus data source");
                Log.Verbose("{Function}: linkStateResponse is null", nameof(InitiateConnection));
                return false;
            }

            // Let's say hello!
            PakbusHelloResponseMessage helloResponse = await SendHelloTransaction(LocalNodeID, RemoteNodeID);

            if (helloResponse is null)
            {
                Log.Error("Failed to start Pakbus data source");
                Log.Verbose("{Function}: helloResponse is null", nameof(InitiateConnection));
                return false;
            }

            await Task.Delay(1000, _canceller.Token);
            return true;
        }

        internal void Send(PakbusPacket packet)
        {
            try
            {
                if (packet is PakbusLinkStatePacket)
                    Log.Debug("[Pakbus {TxRx}] Link State: {LinkStateCode:X} {LinkState} from {SrcAddr}", "TX",
                              ((byte)packet.Header.LinkState).ToString(CultureInfo.CurrentCulture),
                              packet.Header.LinkState.ToString(),
                              packet.Header.SourcePhysicalAddress.ToString(CultureInfo.CurrentCulture));
                else
                    Log.Debug(
                        "[Pakbus {TxRx}] {MsgType} [{MsgTypeByte:X}] (size={MsgSize}, tx={TransNum}) from {SourceNode} to {DestNode}",
                        "TX",
                        packet.Message.MessageType,
                        ((byte)packet.Message.MessageType & 0xFF).ToString(CultureInfo.CurrentCulture),
                        packet.Message.Size.ToString(CultureInfo.CurrentCulture),
                        packet.Message.TransactionNumber.ToString(CultureInfo.CurrentCulture),
                        packet.Header.SourceNodeID.ToString(CultureInfo.CurrentCulture),
                        packet.Header.DestinationNodeID.ToString(CultureInfo.CurrentCulture));

                IEnumerable<byte> compiled = packet.Encode();
                Send(compiled.ToArray());
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send Pakbus data packet");
            }
        }

        internal void Send(byte[] data)
        {
            Log.Verbose("Sending {Data}", BitConverter.ToString(data));
            _port.Write(data, 0, data.Length);
            _port.Flush();
        }

        internal async Task<PakbusPacket> Read(CancellationToken token, byte transactionId = 0)
        {
            return await Task.Factory.StartNew(() =>
            {
                try
                {
                    byte[] buffer = new byte[PakbusPacket.MaxLength];
                    Log.Verbose("Waiting for response on transaction ID {TransId}", transactionId);
                    while (true)
                    {
                        if (_port.ReadByte() != 0xBD)
                            continue;

                        // We have read a byte, continue reading until we get to the first non-0xBD byte
                        int lastByte;
                        while ((lastByte = _port.ReadByte()) == 0xBD)
                        {
                        }

                        int n = 0;
                        buffer[n++] = 0xBD;
                        buffer[n++] = (byte)lastByte;
                        // keep buffering until we reach a full packet
                        while (n < buffer.Length)
                        {
                            byte b = (byte)_port.ReadByte();
                            buffer[n++] = b;

                            if (b == 0xBD)
                                break;
                        }

                        if (n == buffer.Length)
                            continue; // malformed: too long

                        Log.Verbose("Received {Data}", BitConverter.ToString(buffer, 0, n));

                        // We now have a packet!  Try to decode it
                        try
                        {
                            PakbusPacket packet = PakbusPacket.Decode(buffer, n);

                            if (packet.Message == null || packet is PakbusLinkStatePacket)
                                return packet;

                            if (packet.Message.TransactionNumber != transactionId)
                                continue; // wrong transaction

                            if (packet.Header.DestinationPhysicalAddress != LocalNodeID &&
                                packet.Header.DestinationNodeID != LocalNodeID)
                                continue; // not for us

                            return packet;
                        } catch (Exception ex)
                        {
                            Log.Verbose(ex, "Failed to decode packet");
                        }
                    }
                } catch
                {
                    Log.Verbose("Failed to read packet");
                    return null;
                }
            }, token);
        }

        private async Task<bool> SetClock(uint from, uint to, DateTime time)
        {
            DateTime currentTime = DateTime.UtcNow;
            _lastClockSetTime = currentTime;
            Log.Information("Setting clock to {Time}", currentTime);
            PakbusXTDClockResponse resp = await SetTimeTransaction(from, to, time);
            if (resp.ResponseCode != PakbusXTDResponseCode.ClockChanged)
            {
                Log.Warning("Could not set clock:  Response code was {RespCode}", resp.ResponseCode);
                return false;
            }

            Log.Verbose("Successfully set clock to {Time}:  {RespCode}", currentTime, resp.ResponseCode);
            return true;
        }

        private async Task<PakbusLinkStatePacket> Ring(uint from, uint to)
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    var cts = new CancellationTokenSource(1000);
                    cts.CancelAfter(1000);

                    PakbusLinkStatePacket ring = PakbusLinkStatePacket.FromState(from, to, PakbusLinkState.Ring);
                    Send(ring);

                    PakbusPacket reply = await Read(cts.Token);
                    if (reply is not PakbusLinkStatePacket linkStatePacket ||
                        linkStatePacket.Header.LinkState != PakbusLinkState.Ready)
                        continue;

                    return linkStatePacket;
                }

                Log.Error("Failed to establish link with datalogger");
                return null;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to establish link with datalogger");
                return null;
            }
        }

        private async Task<PakbusHelloResponseMessage> SendHelloTransaction(uint from, uint to)
        {
            try
            {
                byte transactionID = PakbusPacket.GenerateNewTransactionNumber();

                var header = PakbusHeader.Create(from, to, PakbusProtocol.PakCtrl);
                PakbusMessage msg = new PakbusHelloMessage(transactionID)
                {
                    IsRouter = 0x01,
                    HopMetric = 0x02
                };
                var packet = new PakbusPacket(header, msg);

                Send(packet);
                Log.Verbose("Data sent, waiting for response...");

                PakbusPacket response = await Read(CancellationToken.None, transactionID);

                if (response.Message is not PakbusHelloResponseMessage helloResponse)
                    throw new InvalidOperationException("Response was not a hello response");

                return helloResponse;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send hello transaction");
                return null;
            }
        }

        private async Task<PakbusXTDClockResponse> GetTimeTransaction(uint from, uint to)
        {
            try
            {
                byte transactionID = PakbusPacket.GenerateNewTransactionNumber();

                var header = PakbusHeader.Create(from, to, PakbusProtocol.BMP);
                PakbusMessage msg = new PakbusXTDClockCommand(transactionID, (ushort)SecurityCode);
                var packet = new PakbusPacket(header, msg);

                Send(packet);
                Log.Verbose("Data sent, waiting for response...");

                PakbusPacket response = await Read(CancellationToken.None, transactionID);

                if (response.Message is not PakbusXTDClockResponse clockResponse)
                    throw new InvalidOperationException("Response was not a clock response");

                return clockResponse;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send clock transaction");
                return null;
            }
        }

        private async Task<PakbusXTDClockResponse> SetTimeTransaction(uint from, uint to, DateTime time)
        {
            try
            {
                byte transactionID = PakbusPacket.GenerateNewTransactionNumber();

                var header = PakbusHeader.Create(from, to, PakbusProtocol.BMP);
                PakbusMessage msg = new PakbusXTDClockCommand(transactionID, (ushort)SecurityCode, time);
                var packet = new PakbusPacket(header, msg);

                Send(packet);
                Log.Verbose("Data sent, waiting for response...");

                PakbusPacket response = await Read(CancellationToken.None, transactionID);

                if (response.Message is not PakbusXTDClockResponse clockResponse)
                    throw new InvalidOperationException("Response was not a clock response");

                return clockResponse;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send clock transaction");
                return null;
            }
        }

        private async Task<XTDTableDefinition> DownloadXTDTableDefinitionsTransaction(uint from, uint to)
        {
            try
            {
                byte transactionID = PakbusPacket.GenerateNewTransactionNumber();

                bool hasMore;
                int i = 0;
                int fragment = 0;

                byte[] data = Array.Empty<byte>();
                do
                {
                    var header = PakbusHeader.Create(from, to, PakbusProtocol.BMP);
                    PakbusMessage msg =
                        new PakbusXTDGetTableDefinitionsCommand(transactionID, SecurityCode, fragment++);
                    var packet = new PakbusPacket(header, msg);

                    Send(packet);

                    PakbusPacket response = await Read(CancellationToken.None, transactionID);

                    if (response.Message is not PakbusXTDGetTableDefinitionsResponse dataResponse)
                        throw new InvalidOperationException("Response was not a data response");

                    hasMore = dataResponse.MoreFragments;

                    Array.Resize(ref data, i + dataResponse.Fragment.Length);
                    Array.Copy(dataResponse.Fragment, 0, data, i, dataResponse.Fragment.Length);
                    i += dataResponse.Fragment.Length;
                } while (hasMore);

                XTDTableDefinition tableDef = XTDTableDefinition.Decode(data);

                return tableDef;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to download XTD table definitions");
                return null;
            }
        }

        private async Task<PakbusDataCollectResponseMessage> SendCollectDataTransaction(uint from, uint to, Table table)
        {
            try
            {
                byte transactionID = PakbusPacket.GenerateNewTransactionNumber();

                var header = new PakbusHeader(PakbusHeaderType.Normal, to, from, to, from, PakbusProtocol.BMP,
                                              1, PakbusLinkState.Ready, PakbusPriority.High, 0);
                PakbusMessage msg = new PakbusDataCollectCommandMessage(transactionID, (ushort)table.Index,
                                                                        table.Signature,
                                                                        (ushort)SecurityCode,
                                                                        PakbusCollectionMode.GetLastRecord, 1, 0);
                var packet = new PakbusPacket(header, msg);

                Send(packet);

                PakbusPacket response = await Read(CancellationToken.None, transactionID);

                if (response.Message is not PakbusDataCollectResponseMessage dataResponse)
                    throw new InvalidOperationException("Response was not a data response");

                return dataResponse;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve data");
                return null;
            }
        }

        private readonly int _baud;
        private readonly CancellationTokenSource _canceller;

        private readonly string _portName;
        private DateTime _lastClockSetTime = DateTime.UtcNow;
        private DateTime _lastPacketTime;
        private int _lastRecordNumber;
        private ISerialInterface _port;
        internal readonly bool _usingTestSerialInterface;
    }
}

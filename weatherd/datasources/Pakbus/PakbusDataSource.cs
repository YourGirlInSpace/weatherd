using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnitsNet;
using UnitsNet.Units;
using weatherd.datasources.Pakbus.Messages.BMP5;
using weatherd.datasources.Pakbus.Messages.PakCtrl;

namespace weatherd.datasources.Pakbus
{
    public interface IPakbusDataSource : IAsyncWeatherDataSource
    { }

    public class PakbusDataSource : IPakbusDataSource
    {
        public IConfiguration Configuration { get; }

        public PakbusDataSource(IConfiguration config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));

            IConfigurationSection section = Configuration.GetSection(nameof(PakbusDataSource));
            if (section == null)
                throw new InvalidOperationException(
                    $"Could not find configuration section named {nameof(PakbusDataSource)}!");

            if (!uint.TryParse(section["NodeID"], out uint nodeId))
                throw new InvalidOperationException(
                    $"Could not find configuration section named {nameof(PakbusDataSource)}.NodeID!");
            if (!uint.TryParse(section["TargetNode"], out uint targetNode))
                throw new InvalidOperationException(
                    $"Could not find configuration section named {nameof(PakbusDataSource)}.TargetNode!");
            if (!ushort.TryParse(section["SecurityCode"], out ushort securityCode))
                securityCode = 0;

            NodeID = nodeId;
            TargetNode = targetNode;
            SecurityCode = securityCode;
        }

        /// <inheritdoc />
        public async Task<bool> Initialize()
        {
            IConfigurationSection section = Configuration.GetSection(nameof(PakbusDataSource));
            if (section == null)
                throw new InvalidOperationException(
                    $"Could not find configuration section named {nameof(PakbusDataSource)}!");

            section = section.GetSection("Port");
            if (section == null)
                throw new InvalidOperationException(
                    $"Could not find configuration section named {nameof(PakbusDataSource)}.Port!");

            string portName = section["Name"];
            if (!int.TryParse(section["Baud"], out int baud))
                throw new InvalidOperationException(
                    $"Could not find configuration section named {nameof(PakbusDataSource)}.Port.Baud!");

            Log.Information("Opening port {comPort} ({baud} 8N1, RTS, DTR, No handshake)", portName, baud);

            _port = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
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
                Log.Fatal(ex, "Failed to open {comPort}", portName);
                return false;
            }

            Initialized = _port.IsOpen;
            return Initialized;
        }

        public async Task<bool> Start()
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    int reconnectAttempts = 0;
                    Running = true;
                    while (true)
                    {
                        if (reconnectAttempts != 0)
                            await Task.Delay(5000);

                        if (reconnectAttempts == 5)
                        {
                            Log.Fatal("Failed to connect to Pakbus data source after 5 attempts.");
                            break;
                        }

                        Log.Verbose("Clearing the buffer...");
                        for (int i = 0; i < 5; i++)
                        {
                            Send(new byte[] { 0xBD });
                            await Task.Delay(10);
                        }

                        Log.Verbose("Beginning 'ring' transaction...");
                        // Let's say hello
                        PakbusLinkStatePacket linkStateResponse = await Ring(NodeID, TargetNode);

                        if (linkStateResponse is null)
                        {
                            Log.Error("Failed to start Pakbus data source");
                            reconnectAttempts++;
                            continue;
                        }

                        // Let's say hello!
                        PakbusHelloResponseMessage helloResponse = await SendHelloTransaction(NodeID, TargetNode);

                        if (helloResponse is null)
                        {
                            Log.Error("Failed to start Pakbus data source");
                            reconnectAttempts++;
                            continue;
                        }

                        await Task.Delay(1000);

                        // Validate the clock
                        PakbusXTDClockResponse r = await GetTimeTransaction(NodeID, TargetNode);
                        if (r.ResponseCode != PakbusXTDResponseCode.ClockNotChanged)
                            throw new InvalidOperationException("Get clock command managed to set the clock!!");

                        TimeSpan deviation = DateTime.UtcNow - r.Time.ToTime();
                        Log.Information(
                            "Datalogger clock is {time}, which differs from server time by {deviation} seconds",
                            r.Time.ToTime(), deviation.TotalSeconds);
                        if (deviation > TimeSpan.FromSeconds(5))
                            await SetClock(NodeID, TargetNode, DateTime.UtcNow);

                        // Download table definitions
                        XTDTableDefinition tableDef = await DownloadXTDTableDefinitionsTransaction(NodeID, TargetNode);

                        if (tableDef is null)
                        {
                            Log.Error("Failed to start Pakbus data source");
                            reconnectAttempts++;
                            continue;
                        }

                        // Let's collect the latest data
                        while (true)
                        {
                            if (DateTime.UtcNow - _lastPacketTime > TimeSpan.FromMilliseconds(2500))
                            {
                                helloResponse = await SendHelloTransaction(NodeID, TargetNode);
                                if (helloResponse is null)
                                    break;
                            }

                            if (DateTime.UtcNow - _lastClockSetTime > TimeSpan.FromMinutes(5))
                                await SetClock(NodeID, TargetNode, DateTime.UtcNow);

                            PakbusDataCollectResponseMessage data =
                                await SendCollectDataTransaction(NodeID, TargetNode, tableDef["Inlocs"]);

                            if (data is not null)
                            {
                                int currentRecNo = (int)data["RECNO"];
                                if (currentRecNo != lastRecNo)
                                {
                                    long recTime = (long)data["RECTIME"];
                                    DateTime dt = DateTime.UnixEpoch.AddSeconds(recTime);
                                    
                                    Conditions = new WeatherState
                                    {
                                        Time = dt,
                                        Temperature =
                                            new Temperature((float)data["AirTC"], TemperatureUnit.DegreeCelsius),
                                        RelativeHumidity =
                                            new RelativeHumidity((float)data["RH"], RelativeHumidityUnit.Percent),
                                        Pressure = new Pressure(1013.25, PressureUnit.Hectopascal),
                                        WindDirection = new Angle((float)data["WDir_deg"], AngleUnit.Degree),
                                        WindSpeed = new Speed((float)data["WSpd_mph"], SpeedUnit.MilePerHour),
                                        Luminosity =
                                            new Irradiance((float)data["SlrW"], IrradianceUnit.WattPerSquareMeter),
                                        RainfallSinceMidnight = new Length((float)data["Rain24"], LengthUnit.Millimeter)
                                    };

                                    Log.Information(
                                        "Received data: Rain = {rain} at {time}",
                                        Conditions.RainfallSinceMidnight,
                                        dt);

                                    Log.Verbose("Invoking SampleAvailable..");
                                    SampleAvailable?.Invoke(this, new WeatherDataEventArgs(Conditions));
                                }

                                lastRecNo = currentRecNo;
                                _lastPacketTime = DateTime.UtcNow;
                            }

                            await Task.Delay(1000);
                        }

                        // Error state:  Could not retrieve data from datalogger.  Try again.
                        reconnectAttempts++;
                    }
                } catch (Exception ex)
                {
                    Log.Error(ex, "Failed to connect to Pakbus data logger");
                }

                Running = false;
            });

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> Stop() => true;

        internal void Send(PakbusPacket packet)
        {
            try
            {
                if (packet is PakbusLinkStatePacket)
                    Log.Debug("[Pakbus {txrx}] Link State: {linkStateCode:X} {linkState} from {srcAddr}", "TX",
                              (byte)packet.Header.LinkState, packet.Header.LinkState,
                              packet.Header.SourcePhysicalAddress);
                else
                    Log.Debug(
                        "[Pakbus {txrx}] {msgType} [{msgTypeByte:X}] (size={msgSize}, tx={transNum}) from {sourceNode} to {destNode}",
                        "TX",
                        packet.Message.MessageType, (byte)packet.Message.MessageType & 0xFF, packet.Message.Size,
                        packet.Message.TransactionNumber,
                        packet.Header.SourceNodeID, packet.Header.DestinationNodeID);

                byte[] compiled = packet.Encode();
                Send(compiled);
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send Pakbus data packet.");
            }
        }

        internal void Send(byte[] data)
        {
            Log.Verbose("Sending {data}", BitConverter.ToString(data));
            _port.Write(data, 0, data.Length);
            _port.BaseStream.Flush();
        }

        internal async Task<PakbusPacket> Read(CancellationToken token, byte transactionId = 0)
        {
            return await Task.Factory.StartNew(() =>
            {
                try
                {
                    byte[] buffer = new byte[PakbusPacket.MaxLength];
                    Log.Verbose("Waiting for response on transaction ID {transId}", transactionId);
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

                        Log.Verbose("Received {data}", BitConverter.ToString(buffer, 0, n));

                        // We now have a packet!  Try to decode it
                        try
                        {
                            PakbusPacket packet = PakbusPacket.Decode(buffer, n);

                            if (packet.Message == null || packet is PakbusLinkStatePacket)
                                return packet;

                            if (packet.Message.TransactionNumber != transactionId)
                                continue; // wrong transaction

                            if (packet.Header.DestinationPhysicalAddress != NodeID &&
                                packet.Header.DestinationNodeID != NodeID)
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
            Log.Information("Setting clock to {time}", currentTime);
            PakbusXTDClockResponse resp = await SetTimeTransaction(from, to, time);
            if (resp.ResponseCode != PakbusXTDResponseCode.ClockChanged)
            {
                Log.Warning("Could not set clock:  Response code was {respCode}", resp.ResponseCode);
                return false;
            }

            Log.Verbose("Successfully set clock to {time}:  {respCode}", currentTime, resp.ResponseCode);
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

                Log.Error("Failed to establish link with datalogger.");
                return null;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to establish link with datalogger.");
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
                Log.Error(ex, "Failed to send hello transaction.");
                return null;
            }
        }

        private async Task<PakbusXTDClockResponse> GetTimeTransaction(uint from, uint to)
        {
            try
            {
                byte transactionID = PakbusPacket.GenerateNewTransactionNumber();

                var header = PakbusHeader.Create(from, to, PakbusProtocol.BMP);
                PakbusMessage msg = new PakbusXTDClockCommand(transactionID, SecurityCode);
                var packet = new PakbusPacket(header, msg);

                Send(packet);
                Log.Verbose("Data sent, waiting for response...");

                PakbusPacket response = await Read(CancellationToken.None, transactionID);

                if (response.Message is not PakbusXTDClockResponse clockResponse)
                    throw new InvalidOperationException("Response was not a clock response");

                return clockResponse;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send clock transaction.");
                return null;
            }
        }

        private async Task<PakbusXTDClockResponse> SetTimeTransaction(uint from, uint to, DateTime time)
        {
            try
            {
                byte transactionID = PakbusPacket.GenerateNewTransactionNumber();

                var header = PakbusHeader.Create(from, to, PakbusProtocol.BMP);
                PakbusMessage msg = new PakbusXTDClockCommand(transactionID, SecurityCode, time);
                var packet = new PakbusPacket(header, msg);

                Send(packet);
                Log.Verbose("Data sent, waiting for response...");

                PakbusPacket response = await Read(CancellationToken.None, transactionID);

                if (response.Message is not PakbusXTDClockResponse clockResponse)
                    throw new InvalidOperationException("Response was not a clock response");

                return clockResponse;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send clock transaction.");
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
                Log.Error(ex, "Failed to download XTD table definitions.");
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
                                                                        SecurityCode,
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

        private DateTime _lastClockSetTime = DateTime.UtcNow;

        private DateTime _lastPacketTime = DateTime.UtcNow;

        private SerialPort _port;

        private int lastRecNo;
        public uint NodeID;
        public ushort SecurityCode;
        public uint TargetNode;

        /// <inheritdoc />
        public WeatherState Conditions { get; set; }

        /// <inheritdoc />
        public event EventHandler<WeatherDataEventArgs> SampleAvailable;

        /// <inheritdoc />
        public string Name => "Pakbus Data Source";

        /// <inheritdoc />
        public int PollingInterval => 1;

        /// <inheritdoc />
        public bool Initialized { get; private set; }

        /// <inheritdoc />
        public bool Running { get; private set; }
    }
}

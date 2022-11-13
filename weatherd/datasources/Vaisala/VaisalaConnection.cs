using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using weatherd.datasources.Vaisala.Commands;
using weatherd.datasources.Vaisala.Messages;
using weatherd.io;

namespace weatherd.datasources.Vaisala
{
    public class VaisalaConnection
    {
        private const char SOH = '\x01';
        private const char ETX = '\x03';
        private const char ACK = '\x06';
        private const char CR = '\r';
        private const char LF = '\n';
        
        public string SensorID { get; }
        public bool Connected { get; private set; }
        public bool Faulted { get; private set; }
        
        private ISerialInterface _port;
        internal readonly bool _usingTestSerialInterface;
        private readonly int _baud;
        private readonly string _portName;
        private readonly CancellationTokenSource _canceller;

        /// <summary>
        ///     Instantiates a new VaisalaConnection using an existing serial connection.
        /// </summary>
        /// <param name="serialInterface">The serial interface to use</param>
        /// <param name="sensorId">The sensor ID to use in this connection.</param>
        internal VaisalaConnection(ISerialInterface serialInterface, string sensorId)
            : this("TEST_PORT", 9600, sensorId)
        {
            _port = serialInterface;
            _usingTestSerialInterface = true;
        }

        /// <summary>
        ///     Instantiates a new VaisalaConnection.
        /// </summary>
        /// <param name="comPort">The system serial port name that is connected to the Pakbus network</param>
        /// <param name="baud">The baud rate of the Pakbus network</param>
        /// <param name="sensorId">The sensor ID to use in this connection.</param>
        public VaisalaConnection(string comPort, int baud, string sensorId)
        {
            if (sensorId.Length >= 2)
                throw new ArgumentOutOfRangeException(nameof(sensorId), "Sensor ID must be a string of length 1 or 2.");
            _portName = comPort;
            _baud = baud;
            SensorID = sensorId;
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

                ReadTimeout = 30000,
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
            if (!Retry.Do(() => OpenPort(comPort, baud)))
                return false;

            try
            {
                var stationStatus = await SendStationStatusCommand();

                if (stationStatus.HardwareAlarms.Length != 0)
                    Log.Error("Vaisala sensor {SensorID} reports hardware alarms: {HardwareAlarms}", SensorID,
                              string.Join(", ", stationStatus.HardwareAlarms));

                if (stationStatus.Warnings.Length != 0)
                    Log.Warning("Vaisala sensor {SensorID} reports hardware warnings: {Warnings}", SensorID,
                                string.Join(", ", stationStatus.Warnings));
            } catch (Exception ex)
            {
                Log.Warning(ex, "Failed to retrieve station status");
            }

            Connected = true;
            return Connected;
        }
        
        public bool Start(Action<VaisalaMessage> callback, Action<bool> completionCallback)
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

        private async Task<bool> DataRunner(Action<VaisalaMessage> callback)
        {
            if (!_port.IsOpen && !await Retry.DoAsync(() => Connect(_portName, _baud)))
                return false;

            while (true)
            {
                try
                {
                    VaisalaAviationMessage vaisalaAvMessage = await Read<VaisalaAviationMessage>(_canceller.Token);
                    callback(vaisalaAvMessage);
                } catch (Exception ex)
                {
                    Log.Error(ex, "Failed to retrieve message from Vaisala sensor {SensorID}", SensorID);
                    return false;
                }
                
                await Task.Delay(15000, _canceller.Token);
            }
        }

        internal async Task<T> Read<T>(CancellationToken token, int timeout = 15000)
            where T : VaisalaMessage
        {
            if (!_port.IsOpen)
            {
                Log.Warning("Failed to read data from Vaisala connection:  Port is closed");
                return default;
            }

            return await Task.Factory.StartNew(() =>
            {
                byte[] buffer = new byte[1024];

                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                        return null;
                    
                    buffer[0] = (byte) _port.ReadByte();

                    if (buffer[0] != SOH)
                        continue;

                    // Check if the next two bytes are 'PW'
                    buffer[1] = (byte) _port.ReadByte();
                    if (buffer[1] != 'P')
                        continue;
                    buffer[2] = (byte)_port.ReadByte();
                    if (buffer[2] != 'W')
                        continue;

                    // Continue reading until we encounter EOT + CR + LF
                    int i = 3;
                    for (; i < buffer.Length; i++)
                    {
                        buffer[i] = (byte)_port.ReadByte();

                        if (buffer[i] != ETX)
                            continue;

                        // We found an ETX.  Is it followed by a CR+LF?
                        buffer[++i] = (byte)_port.ReadByte();
                        if (buffer[i] != CR)
                            continue;
                        buffer[++i] = (byte)_port.ReadByte();
                        if (buffer[i] != LF)
                            continue;

                        // We have a message!
                        string message = Encoding.ASCII.GetString(buffer, 0, i + 1);

                        // Try to decode it
                        VaisalaMessage msg = null;
                        try
                        {
                            msg = VaisalaMessage.Parse(message);
                        } catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to decode Vaisala message");
                        }

                        if (msg is not T specificMessageType)
                            break;

                        return specificMessageType;
                    }
                }
            }, _canceller.Token);
        }

        internal void Send(VaisalaCommand command)
        {
            try
            {
                byte[] sendBuffer = Encoding.ASCII.GetBytes(command.Compile());
                
                Send(sendBuffer);
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send Vaisala command");
            }
        }

        internal void Send(byte[] data)
        {
            //Log.Verbose("Sending {Data}", BitConverter.ToString(data));
            _port.Write(data, 0, data.Length);
            _port.Flush();
        }

        internal async Task<VaisalaStationStatusMessage> SendStationStatusCommand()
        {
            try
            {
                VaisalaPollMessageCommand pollMessageCommand =
                    new VaisalaPollMessageCommand(SensorID, VaisalaMessageType.Status);

                Send(pollMessageCommand);

                VaisalaStationStatusMessage status = await Read<VaisalaStationStatusMessage>(_canceller.Token, 1000);
                return status;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to send station status command");
                return null;
            }
        }
        internal Task<bool> SendResetTotalsCommand()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    VaisalaResetPrecipitationTotalsCommand pollMessageCommand =
                        new VaisalaResetPrecipitationTotalsCommand(SensorID);

                    Send(pollMessageCommand);

                    return _port.ReadByte() == ACK;
                } catch (Exception ex)
                {
                    Log.Error(ex, "Failed to send station status command");
                    return false;
                }
            });
        }
    }
}

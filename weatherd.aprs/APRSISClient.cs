using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using weatherd.aprs.responses;
using Serilog;

namespace weatherd.aprs
{
    public class APRSISClient : IDisposable
    {
        public const string DefaultHost = APRS;
        public const int DefaultPort = 14580;
        
        public const int SendTimeout = 1000;
        public const int ReceiveTimeout = 1000;

        
        public const string CWOP = "cwop.aprs.net";
        public const string APRS = "rotate.aprs.net";

        public const string CWOPSend = "0";
        public const string ReceiveOnly = "-1";

        public string Host { get; }

        public int Port { get; }
        
        public bool IsConnected => _client?.Connected ?? false;

        public bool IsLoggedIn { get; private set; }

        public bool IsVerified { get; private set; }

        public string Server { get; private set; }

        private TcpClient _client;
        private NetworkStream _stream;
        private StreamWriter _writer;
        private StreamReader _reader;

        public APRSISClient()
            : this(DefaultHost, DefaultPort)
        { }

        public APRSISClient(string host, int port)
        {
            if (port <= 0 || port >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
        }

        public APRSISClient(IPEndPoint ipEndPoint)
        {
            Host = ipEndPoint.Address.ToString();
            Port = ipEndPoint.Port;
        }

        public async Task<bool> Connect()
        {
            try
            {
                Log.Information($"Connecting to {Host}:{Port} with parameters SendTimeout={SendTimeout}, ReceiveTimeout={ReceiveTimeout}");
                _client = new TcpClient { SendTimeout = SendTimeout, ReceiveTimeout = ReceiveTimeout };
                await _client.ConnectAsync(Host, Port);

                _stream = _client.GetStream();
                _stream.WriteTimeout = SendTimeout;
                _stream.ReadTimeout = ReceiveTimeout;

                _writer = new StreamWriter(_stream);
                _reader = new StreamReader(_stream);

                string result = await _reader.ReadLineAsync();
                Log.Verbose(result);

                return _client.Connected;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to connect to APRS server.");
                return false;
            }
        }

        public Task<bool> Login(string callsign)
            => Login(callsign, APRSISLoginMessage.CWOPSendPasscode);

        public Task<bool> Login(string callsign, string passcode)
            => Login(callsign, passcode, string.Empty);

        public Task<bool> Login(string callsign, string passcode, string softwareName)
            => Login(callsign, passcode, softwareName, string.Empty);

        public async Task<bool> Login(string callsign, string passcode, string softwareName, string softwareVersion)
        {
            if (!IsConnected)
            {
                Log.Verbose($"Attempted to call {nameof(Login)} but {nameof(IsConnected)}==false");
                return false;
            }

            APRSISLoginMessage loginMessage = new APRSISLoginMessage(callsign, passcode)
            {
                SoftwareName = softwareName,
                SoftwareVersion = softwareVersion
            };

            Log.Information($"Attempting to login on {Host}:{Port} with callsign {callsign}, passcode calculated as {loginMessage.Passcode}");
            
            // Note: .Compile() here is necessary to avoid the IsVerified check
            APRSISLoginResponse response = await SendCommandWithResponse<APRSISLoginResponse>(loginMessage.Compile());

            // # logresp logincall verifystatus, server servercall
            if (!response.IsValid)
            {
                Log.Warning("Login response was not valid!");
                return false;
            }

            // Verify two things:  First, do we have the correct callsign?  Second, are we verified?
            if (!response.Callsign.Equals(callsign, StringComparison.Ordinal))
            {
                Log.Warning("Login response does not contain the original callsign-ssid!");
                return false;
            }

            // CWOP doesn't have verification
            IsVerified = response.IsVerified || Host == CWOP;
            if (!IsVerified)
                Log.Warning("Login successful, APRS client in receive-only mode");

            Server = response.Server;

            IsLoggedIn = true;
            Log.Information($"User {callsign} logged in on {Host}:{Port}.  Verified = {IsVerified}");

            return true; 
        }

        /// <summary>
        ///     Sends a command if the client is verified.
        /// </summary>
        /// <param name="compilable">A compilable APRS command</param>
        public async Task SendCommand(ICompilable compilable)
        {
            string command = compilable.Compile();

            if (IsVerified)
            {
                await SendCommand(command);
                return;
            }

            Log.Warning($"Client in RX only mode.  Cannot send command {command}.");
        }

        /// <summary>
        ///     Sends a command and returns the response for the command if the client is verified.
        /// </summary>
        /// <param name="compilable">A compilable APRS command</param>
        /// <returns>The response, or nothing if the client is in RX only mode.</returns>
        public async Task<string> SendCommandWithResponse(ICompilable compilable)
        {
            string command = compilable.Compile();
            if (IsVerified)
                return await SendCommandWithResponse(command);

            Log.Warning($"Client in RX only mode.  Cannot send command {command}.");
            return string.Empty;
        }

        private async Task<string> SendCommandWithResponse(string command)
        {
            await SendCommand(command);
            return await ReceiveCommand();
        }

        public async Task<T> SendCommandWithResponse<T>(ICompilable compilable) where T : IParsable, new()
        {
            string command = compilable.Compile();
            if (IsVerified)
            {
                string response = await SendCommandWithResponse(command);
                T t = new T();
                t.Parse(response);

                return t;
            }

            Log.Warning($"Client in RX only mode.  Cannot send command {command}.");
            return default;
        }

        private async Task<T> SendCommandWithResponse<T>(string command) where T : IParsable, new()
        {
            string response = await SendCommandWithResponse(command);

            T t = new T();
            t.Parse(response);

            return t;
        }

        private async Task SendCommand(string command)
        {
            try
            {
                Log.Verbose("APRS-IS Send:  " + command);
                await _writer.WriteLineAsync(command);
                await _writer.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send command.");
            }
        }

        private async Task<string> ReceiveCommand()
        {
            try
            {
                string received = await _reader.ReadLineAsync();

                Log.Verbose("APRS-IS Recv: " + received);
                return received;
            } catch (Exception ex)
            {
                Log.Error(ex, "Failed to receive command.");
                return string.Empty;
            }
        }

        public async Task Close()
        {
            await _writer.FlushAsync();
            _writer.Close();
            _reader.Close();
            await _stream.FlushAsync();

            _client.Close();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _client?.Dispose();
            _stream?.Dispose();
            _writer?.Dispose();
            _reader?.Dispose();
        }
    }
}

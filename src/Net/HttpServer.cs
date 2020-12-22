namespace iPhoneController.Net
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;

    using Newtonsoft.Json;

    using iPhoneController.Diagnostics;
    using iPhoneController.Models;
    using iPhoneController.Utils;

    /// <summary>
    /// HTTP listener class
    /// </summary>
    public class HttpServer
    {
        #region Variables

        private static readonly IEventLogger _logger = EventLogger.GetLogger("HTTP");//, Program.LogLevel);
        private HttpListener _server;
        private bool _initialized = false;
        private static string _endpoint;

        #endregion

        #region Properties

        /// <summary>
        /// HTTP listening interface/host address
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Http listening port
        /// </summary>
        public ushort Port { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Instantiates a new <see cref="HttpServer"/> class.
        /// </summary>
        /// <param name="host">Listing host interface</param>
        /// <param name="port">Listening port</param>
        public HttpServer(string host, ushort port)
        {
            Host = host ?? "*";
            Port = port;

            Initialize();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the HTTP listener server
        /// </summary>
        public void Start()
        {
            _logger.Trace($"Start");

            if (!_initialized)
            {
                _logger.Error("HTTP listener not initalized, make sure you run as administrator or root.");
                return;
            }

            // If already listening, return
            if (_server.IsListening)
            {
                _logger.Debug($"Already listening, failed to start...");
                return;
            }

            try
            {
                // Start listener
                _logger.Info($"Starting...");
                _server.Start();
                _logger.Info($"Started");
            }
            catch (HttpListenerException ex)
            {
                // Access denied
                if (ex.ErrorCode == 5)
                {
                    _logger.Warn("You need to run the following command in order to not have to run as Administrator or root every start:");
                    _logger.Warn($"netsh http add urlacl url={_endpoint} user={Environment.UserDomainName}\\{Environment.UserName} listen=yes");
                }
                else
                {
                    // Unexpected error thrown
                    throw;
                }
            }

            // Check if the listener is listening
            if (_server.IsListening)
            {
                _logger.Debug($"Listening on port {Port}...");
            }

            // Start accepting requests
            _logger.Info($"Starting HttpServer request handler...");
            var requestThread = new Thread(RequestHandler) { IsBackground = true };
            requestThread.Start();
        }

        /// <summary>
        /// Attempts to stop the HTTP listener server
        /// </summary>
        public void Stop()
        {
            _logger.Trace($"Stop");

            if (!_server.IsListening)
            {
                _logger.Debug($"Not running, failed to stop...");
                return;
            }

            _logger.Info($"Stopping...");
            _server.Stop();
        }

        #endregion

        #region Request Handlers

        private void RequestHandler()
        {
            while (_server.IsListening)
            {
                var context = _server.GetContext();
                var response = context.Response;

                if (context.Request?.InputStream == null)
                    continue;

                using (var sr = new StreamReader(context.Request.InputStream))
                {
                    try
                    {
                        var data = sr.ReadToEnd();
                        var endpoint = context.Request.RawUrl;
                        var method = context.Request.HttpMethod;
                        switch (endpoint)
                        {
                            case "/":
                                if (string.Compare(method, "POST", true) == 0)
                                {
                                    var obj = JsonConvert.DeserializeObject<WebhookPayload>(data);
                                    switch (obj.Type.ToLower())
                                    {
                                        case "restart":
                                            HandleRebootDeviceRequest(obj.Device);
                                            break;
                                        case "reopen":
                                            HandleReopenGameRequest(obj.Device);
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                    catch (HttpListenerException hle)
                    {
                        _logger.Error(hle);

                        //Disconnected, reconnect.
                        HandleDisconnect();
                    }
                }

                try
                {
                    var buffer = Encoding.UTF8.GetBytes(Strings.DefaultResponseMessage);
                    response.ContentLength64 = buffer.Length;
                    if (response?.OutputStream != null)
                    {
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    context.Response.Close();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }

                Thread.Sleep(10);
            }
        }

        private void HandleRebootDeviceRequest(string deviceName)
        {
            _logger.Info($"Handling restart request for device {deviceName}...");
            var devices = Device.GetAll();
            if (!devices.ContainsKey(deviceName))
            {
                _logger.Warn($"{deviceName} does not exist in device list, skipping reboot.");
                return;
            }

            var device = devices[deviceName];
            var output = Shell.Execute("idevicediagnostics", $"-u {device.Uuid} restart", out var exitCode);
            var message = exitCode == 0 ? $"Restarting device {device.Name} ({device.Uuid})" : output;
            _logger.Info(message);
        }

        private void HandleReopenGameRequest(string deviceName)
        {
            _logger.Info($"Handling reopen request for device {deviceName}...");
            var devices = Device.GetAll();
            if (!devices.ContainsKey(deviceName))
            {
                _logger.Warn($"{deviceName} does not exist in device list, skipping reopen.");
                return;
            }

            var device = devices[deviceName];
            if (string.IsNullOrEmpty(device.IPAddress))
            {
                _logger.Warn($"Unable to find IP address for device {deviceName}, failed to send reopen request.");
                return;
            }
            var url = $"http://{device.IPAddress}:8080/reopen";
            var response = NetUtils.Get(url);
            var message = string.IsNullOrEmpty(response) ? $"Reopening game for device {device.Name} ({device.Uuid})" : response;
            _logger.Info(message);
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            _logger.Trace("Initialize");
            try
            {
                _server = CreateListener();
                _endpoint = $"http://{Host}:{Port}/";
                if (!_server.Prefixes.Contains(_endpoint))
                {
                    _server.Prefixes.Add(_endpoint);
                }
                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private HttpListener CreateListener()
        {
            return new HttpListener();
        }

        private void HandleDisconnect()
        {
            _logger.Warn("!!!!! HTTP listener disconnected, handling reconnect...");
            _logger.Warn("Stopping existing listeners...");
            Stop();

            _logger.Warn("Disposing of old listener objects...");
            _server.Close();
            _server = null;

            _logger.Warn("Initializing...");
            Initialize();

            //if (_requestThread != null)
            //{
            //    _logger.Info($"Existing HttpServer main thread...");
            //    _requestThread.Abort();
            //    _requestThread = null;
            //}

            _logger.Warn("Starting back up...");
            Start();

            _logger.Debug("Disconnect handled.");
        }

        #endregion

        private class WebhookPayload
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("device")]
            public dynamic Device { get; set; }
        }
    }
}

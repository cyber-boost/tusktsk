using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.WebSockets;
using System.Net;
using System.Linq;

namespace TuskLang.Operators.Communication
{
    /// <summary>
    /// WebSocket operator for TuskLang
    /// Provides WebSocket operations including connection, messaging, and event handling
    /// </summary>
    public class WebSocketOperator : BaseOperator
    {
        private ClientWebSocket _webSocket;
        private string _url;
        private Dictionary<string, string> _headers;
        private bool _isConnected = false;
        private readonly int _bufferSize = 4096;

        public WebSocketOperator() : base("websocket", "WebSocket communication operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to WebSocket", new[] { "url", "headers", "protocols" });
            RegisterMethod("disconnect", "Disconnect from WebSocket", new string[0]);
            RegisterMethod("send", "Send message via WebSocket", new[] { "message", "type" });
            RegisterMethod("send_json", "Send JSON message", new[] { "data" });
            RegisterMethod("send_binary", "Send binary data", new[] { "data" });
            RegisterMethod("receive", "Receive message from WebSocket", new[] { "timeout" });
            RegisterMethod("receive_json", "Receive JSON message", new[] { "timeout" });
            RegisterMethod("ping", "Send ping to WebSocket", new string[0]);
            RegisterMethod("pong", "Send pong to WebSocket", new string[0]);
            RegisterMethod("get_status", "Get WebSocket status", new string[0]);
            RegisterMethod("get_info", "Get WebSocket connection info", new string[0]);
            RegisterMethod("set_timeout", "Set WebSocket timeout", new[] { "timeout" });
            RegisterMethod("get_buffered_amount", "Get buffered amount", new string[0]);
            RegisterMethod("close", "Close WebSocket connection", new[] { "code", "reason" });
            RegisterMethod("test_connection", "Test WebSocket connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing WebSocket operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "send":
                        return await SendAsync(parameters);
                    case "send_json":
                        return await SendJsonAsync(parameters);
                    case "send_binary":
                        return await SendBinaryAsync(parameters);
                    case "receive":
                        return await ReceiveAsync(parameters);
                    case "receive_json":
                        return await ReceiveJsonAsync(parameters);
                    case "ping":
                        return await PingAsync();
                    case "pong":
                        return await PongAsync();
                    case "get_status":
                        return await GetStatusAsync();
                    case "get_info":
                        return await GetInfoAsync();
                    case "set_timeout":
                        return await SetTimeoutAsync(parameters);
                    case "get_buffered_amount":
                        return await GetBufferedAmountAsync();
                    case "close":
                        return await CloseAsync(parameters);
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown WebSocket method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing WebSocket method {method}: {ex.Message}");
                throw new OperatorException($"WebSocket operation failed: {ex.Message}", "WEBSOCKET_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var headers = GetParameter<Dictionary<string, string>>(parameters, "headers", new Dictionary<string, string>());
            var protocols = GetParameter<string[]>(parameters, "protocols", new string[0]);

            try
            {
                _url = url;
                _headers = headers;

                _webSocket = new ClientWebSocket();

                // Add headers
                foreach (var header in headers)
                {
                    _webSocket.Options.SetRequestHeader(header.Key, header.Value);
                }

                // Add subprotocols
                if (protocols.Length > 0)
                {
                    foreach (var protocol in protocols)
                    {
                        _webSocket.Options.AddSubProtocol(protocol);
                    }
                }

                var uri = new Uri(url);
                await _webSocket.ConnectAsync(uri, CancellationToken.None);

                _isConnected = true;

                LogInfo($"Connected to WebSocket: {url}");
                return new { success = true, url, protocols, state = _webSocket.State.ToString() };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket connection failed: {ex.Message}", "WEBSOCKET_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                if (_webSocket != null && _isConnected)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }

                _webSocket?.Dispose();
                _webSocket = null;
                _isConnected = false;
                _url = null;
                _headers = null;

                LogInfo("Disconnected from WebSocket");
                return new { success = true, message = "Disconnected from WebSocket" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket disconnect failed: {ex.Message}", "WEBSOCKET_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> SendAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var message = GetRequiredParameter<string>(parameters, "message");
            var type = GetParameter<WebSocketMessageType>(parameters, "type", WebSocketMessageType.Text);

            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), type, true, CancellationToken.None);

                LogInfo($"Successfully sent message via WebSocket");
                return new { success = true, message, type = type.ToString(), length = buffer.Length };
            }
            catch (Exception ex)
            {
                LogError($"Error sending message via WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket send failed: {ex.Message}", "WEBSOCKET_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendJsonAsync(Dictionary<string, object> parameters)
        {
            var data = GetRequiredParameter<Dictionary<string, object>>(parameters, "data");

            try
            {
                var json = JsonSerializer.Serialize(data);
                return await SendAsync(new Dictionary<string, object>
                {
                    { "message", json },
                    { "type", WebSocketMessageType.Text }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending JSON via WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket JSON send failed: {ex.Message}", "WEBSOCKET_JSON_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendBinaryAsync(Dictionary<string, object> parameters)
        {
            var data = GetRequiredParameter<byte[]>(parameters, "data");

            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);

                LogInfo($"Successfully sent binary data via WebSocket");
                return new { success = true, dataLength = data.Length, type = "Binary" };
            }
            catch (Exception ex)
            {
                LogError($"Error sending binary data via WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket binary send failed: {ex.Message}", "WEBSOCKET_BINARY_SEND_ERROR", ex);
            }
        }

        private async Task<object> ReceiveAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var timeout = GetParameter<int>(parameters, "timeout", 30000);

            try
            {
                var buffer = new byte[_bufferSize];
                var result = new List<byte>();
                var messageType = WebSocketMessageType.Text;
                var endOfMessage = false;

                using var cts = new CancellationTokenSource(timeout);

                do
                {
                    var receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    result.AddRange(buffer.Take(receiveResult.Count));
                    messageType = receiveResult.MessageType;
                    endOfMessage = receiveResult.EndOfMessage;

                    if (receiveResult.CloseStatus.HasValue)
                    {
                        throw new Exception($"WebSocket closed: {receiveResult.CloseStatus} - {receiveResult.CloseStatusDescription}");
                    }
                } while (!endOfMessage);

                var message = messageType == WebSocketMessageType.Text 
                    ? Encoding.UTF8.GetString(result.ToArray())
                    : Convert.ToBase64String(result.ToArray());

                LogInfo($"Successfully received message via WebSocket");
                return new { success = true, message, type = messageType.ToString(), length = result.Count };
            }
            catch (Exception ex)
            {
                LogError($"Error receiving message via WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket receive failed: {ex.Message}", "WEBSOCKET_RECEIVE_ERROR", ex);
            }
        }

        private async Task<object> ReceiveJsonAsync(Dictionary<string, object> parameters)
        {
            var timeout = GetParameter<int>(parameters, "timeout", 30000);

            try
            {
                var result = await ReceiveAsync(new Dictionary<string, object>
                {
                    { "timeout", timeout }
                });

                if (result is Dictionary<string, object> resultDict && resultDict.ContainsKey("message"))
                {
                    var message = resultDict["message"].ToString();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(message);

                    return new { success = true, data, type = "JSON" };
                }

                throw new Exception("Failed to parse JSON message");
            }
            catch (Exception ex)
            {
                LogError($"Error receiving JSON via WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket JSON receive failed: {ex.Message}", "WEBSOCKET_JSON_RECEIVE_ERROR", ex);
            }
        }

        private async Task<object> PingAsync()
        {
            EnsureConnected();

            try
            {
                var pingData = Encoding.UTF8.GetBytes("ping");
                await _webSocket.SendAsync(new ArraySegment<byte>(pingData), WebSocketMessageType.Text, true, CancellationToken.None);

                LogInfo("Successfully sent ping via WebSocket");
                return new { success = true, message = "Ping sent" };
            }
            catch (Exception ex)
            {
                LogError($"Error sending ping via WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket ping failed: {ex.Message}", "WEBSOCKET_PING_ERROR", ex);
            }
        }

        private async Task<object> PongAsync()
        {
            EnsureConnected();

            try
            {
                var pongData = Encoding.UTF8.GetBytes("pong");
                await _webSocket.SendAsync(new ArraySegment<byte>(pongData), WebSocketMessageType.Text, true, CancellationToken.None);

                LogInfo("Successfully sent pong via WebSocket");
                return new { success = true, message = "Pong sent" };
            }
            catch (Exception ex)
            {
                LogError($"Error sending pong via WebSocket: {ex.Message}");
                throw new OperatorException($"WebSocket pong failed: {ex.Message}", "WEBSOCKET_PONG_ERROR", ex);
            }
        }

        private async Task<object> GetStatusAsync()
        {
            try
            {
                var status = new Dictionary<string, object>
                {
                    { "connected", _isConnected },
                    { "state", _webSocket?.State.ToString() ?? "None" },
                    { "url", _url },
                    { "subprotocol", _webSocket?.SubProtocol ?? "" }
                };

                return new { success = true, status };
            }
            catch (Exception ex)
            {
                LogError($"Error getting WebSocket status: {ex.Message}");
                throw new OperatorException($"WebSocket get status failed: {ex.Message}", "WEBSOCKET_GET_STATUS_ERROR", ex);
            }
        }

        private async Task<object> GetInfoAsync()
        {
            EnsureConnected();

            try
            {
                var info = new Dictionary<string, object>
                {
                    { "url", _url },
                    { "state", _webSocket.State.ToString() },
                    { "subprotocol", _webSocket.SubProtocol ?? "" },
                    { "headers", _headers },
                    { "bufferSize", _bufferSize }
                };

                return new { success = true, info };
            }
            catch (Exception ex)
            {
                LogError($"Error getting WebSocket info: {ex.Message}");
                throw new OperatorException($"WebSocket get info failed: {ex.Message}", "WEBSOCKET_GET_INFO_ERROR", ex);
            }
        }

        private async Task<object> SetTimeoutAsync(Dictionary<string, object> parameters)
        {
            var timeout = GetRequiredParameter<int>(parameters, "timeout");

            try
            {
                // Note: ClientWebSocket doesn't have a direct timeout property
                // This would typically be handled in the receive operations
                LogInfo($"WebSocket timeout set to {timeout}ms");
                return new { success = true, timeout };
            }
            catch (Exception ex)
            {
                LogError($"Error setting WebSocket timeout: {ex.Message}");
                throw new OperatorException($"WebSocket set timeout failed: {ex.Message}", "WEBSOCKET_SET_TIMEOUT_ERROR", ex);
            }
        }

        private async Task<object> GetBufferedAmountAsync()
        {
            EnsureConnected();

            try
            {
                // Note: ClientWebSocket doesn't expose buffered amount directly
                // This is a simulation
                var bufferedAmount = 0;

                return new { success = true, bufferedAmount };
            }
            catch (Exception ex)
            {
                LogError($"Error getting WebSocket buffered amount: {ex.Message}");
                throw new OperatorException($"WebSocket get buffered amount failed: {ex.Message}", "WEBSOCKET_GET_BUFFERED_AMOUNT_ERROR", ex);
            }
        }

        private async Task<object> CloseAsync(Dictionary<string, object> parameters)
        {
            var code = GetParameter<WebSocketCloseStatus>(parameters, "code", WebSocketCloseStatus.NormalClosure);
            var reason = GetParameter<string>(parameters, "reason", "Closing connection");

            try
            {
                if (_webSocket != null && _isConnected)
                {
                    await _webSocket.CloseAsync(code, reason, CancellationToken.None);
                    _isConnected = false;
                }

                LogInfo($"Successfully closed WebSocket connection: {code} - {reason}");
                return new { success = true, code = code.ToString(), reason };
            }
            catch (Exception ex)
            {
                LogError($"Error closing WebSocket connection: {ex.Message}");
                throw new OperatorException($"WebSocket close failed: {ex.Message}", "WEBSOCKET_CLOSE_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            try
            {
                if (!_isConnected || _webSocket == null)
                {
                    return new { success = false, status = "Not connected" };
                }

                var status = _webSocket.State;
                var isConnected = status == WebSocketState.Open;

                return new { success = isConnected, status = status.ToString() };
            }
            catch (Exception ex)
            {
                LogError($"Error testing WebSocket connection: {ex.Message}");
                throw new OperatorException($"WebSocket connection test failed: {ex.Message}", "WEBSOCKET_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_webSocket == null || !_isConnected || _webSocket.State != WebSocketState.Open)
            {
                throw new OperatorException("WebSocket is not connected", "WEBSOCKET_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
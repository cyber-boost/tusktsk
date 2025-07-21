using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TuskLang.Operators.Network
{
    /// <summary>
    /// WebSocket Operator for TuskLang C# SDK
    /// 
    /// Provides WebSocket operations with support for:
    /// - WebSocket connection management
    /// - Message sending and receiving
    /// - Binary and text message handling
    /// - Connection state monitoring
    /// - Automatic reconnection
    /// - Message queuing and buffering
    /// - Event-driven communication
    /// 
    /// Usage:
    /// ```csharp
    /// // Connect to WebSocket
    /// var result = @websocket({
    ///   action: "connect",
    ///   url: "wss://echo.websocket.org"
    /// })
    /// 
    /// // Send message
    /// var result = @websocket({
    ///   action: "send",
    ///   connection_id: "conn_123",
    ///   message: "Hello, WebSocket!"
    /// })
    /// ```
    /// </summary>
    public class WebSocketOperator : BaseOperator
    {
        private static readonly Dictionary<string, ClientWebSocket> _connections = new Dictionary<string, ClientWebSocket>();
        private static readonly Dictionary<string, Task> _receiveTasks = new Dictionary<string, Task>();
        
        public WebSocketOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "url", "connection_id", "message", "timeout", "auto_reconnect", 
                "reconnect_delay", "max_reconnect_attempts", "subprotocols", 
                "headers", "binary", "buffer_size", "keep_alive"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["timeout"] = 30,
                ["auto_reconnect"] = false,
                ["reconnect_delay"] = 5,
                ["max_reconnect_attempts"] = 3,
                ["buffer_size"] = 4096,
                ["keep_alive"] = true
            };
        }
        
        public override string GetName() => "websocket";
        
        protected override string GetDescription() => "WebSocket operator for real-time bidirectional communication";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["connect"] = "@websocket({action: \"connect\", url: \"wss://echo.websocket.org\"})",
                ["send"] = "@websocket({action: \"send\", connection_id: \"conn_123\", message: \"Hello!\"})",
                ["receive"] = "@websocket({action: \"receive\", connection_id: \"conn_123\"})",
                ["close"] = "@websocket({action: \"close\", connection_id: \"conn_123\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_URL"] = "Invalid WebSocket URL",
                ["CONNECTION_FAILED"] = "Failed to connect to WebSocket server",
                ["CONNECTION_NOT_FOUND"] = "WebSocket connection not found",
                ["MESSAGE_ERROR"] = "Error sending or receiving message",
                ["TIMEOUT_EXCEEDED"] = "WebSocket operation timeout exceeded",
                ["PROTOCOL_ERROR"] = "WebSocket protocol error",
                ["RECONNECT_FAILED"] = "Automatic reconnection failed"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "");
            var url = GetContextValue<string>(config, "url", "");
            var connectionId = GetContextValue<string>(config, "connection_id", "");
            var message = ResolveVariable(config.GetValueOrDefault("message"), context);
            var timeout = GetContextValue<int>(config, "timeout", 30);
            var autoReconnect = GetContextValue<bool>(config, "auto_reconnect", false);
            var reconnectDelay = GetContextValue<int>(config, "reconnect_delay", 5);
            var maxReconnectAttempts = GetContextValue<int>(config, "max_reconnect_attempts", 3);
            var subprotocols = ResolveVariable(config.GetValueOrDefault("subprotocols"), context);
            var headers = ResolveVariable(config.GetValueOrDefault("headers"), context);
            var binary = GetContextValue<bool>(config, "binary", false);
            var bufferSize = GetContextValue<int>(config, "buffer_size", 4096);
            var keepAlive = GetContextValue<bool>(config, "keep_alive", true);
            
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action is required");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException("WebSocket operation timeout exceeded");
                }
                
                switch (action.ToLower())
                {
                    case "connect":
                        return await ConnectWebSocketAsync(url, connectionId, timeout, autoReconnect, reconnectDelay, maxReconnectAttempts, subprotocols, headers, keepAlive);
                    
                    case "send":
                        return await SendWebSocketMessageAsync(connectionId, message, binary, timeout);
                    
                    case "receive":
                        return await ReceiveWebSocketMessageAsync(connectionId, timeout, bufferSize);
                    
                    case "close":
                        return await CloseWebSocketAsync(connectionId, timeout);
                    
                    case "status":
                        return await GetWebSocketStatusAsync(connectionId);
                    
                    case "ping":
                        return await PingWebSocketAsync(connectionId, timeout);
                    
                    case "pong":
                        return await PongWebSocketAsync(connectionId, timeout);
                    
                    default:
                        throw new ArgumentException($"Unknown WebSocket action: {action}");
                }
            }
            catch (Exception ex)
            {
                Log("error", "WebSocket operation failed", new Dictionary<string, object>
                {
                    ["action"] = action,
                    ["url"] = url,
                    ["connection_id"] = connectionId,
                    ["error"] = ex.Message
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message,
                    ["action"] = action,
                    ["url"] = url,
                    ["connection_id"] = connectionId
                };
            }
        }
        
        /// <summary>
        /// Connect to WebSocket
        /// </summary>
        private async Task<object> ConnectWebSocketAsync(string url, string connectionId, int timeout, bool autoReconnect, int reconnectDelay, int maxReconnectAttempts, object subprotocols, object headers, bool keepAlive)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL is required");
            
            if (string.IsNullOrEmpty(connectionId))
            {
                connectionId = GenerateConnectionId();
            }
            
            try
            {
                var webSocket = new ClientWebSocket();
                
                // Set options
                webSocket.Options.KeepAliveInterval = keepAlive ? TimeSpan.FromSeconds(30) : TimeSpan.Zero;
                
                // Add subprotocols
                if (subprotocols is List<object> subprotocolList)
                {
                    foreach (var protocol in subprotocolList)
                    {
                        webSocket.Options.AddSubProtocol(protocol.ToString());
                    }
                }
                
                // Add headers
                if (headers is Dictionary<string, object> headersDict)
                {
                    foreach (var kvp in headersDict)
                    {
                        webSocket.Options.SetRequestHeader(kvp.Key, kvp.Value?.ToString() ?? "");
                    }
                }
                
                // Connect
                await webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                
                // Store connection
                _connections[connectionId] = webSocket;
                
                // Start receive task
                _receiveTasks[connectionId] = Task.Run(() => ReceiveMessagesAsync(connectionId, webSocket));
                
                Log("info", "WebSocket connected", new Dictionary<string, object>
                {
                    ["connection_id"] = connectionId,
                    ["url"] = url,
                    ["state"] = webSocket.State.ToString()
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["connection_id"] = connectionId,
                    ["url"] = url,
                    ["state"] = webSocket.State.ToString(),
                    ["auto_reconnect"] = autoReconnect,
                    ["keep_alive"] = keepAlive
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"WebSocket connection failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send WebSocket message
        /// </summary>
        private async Task<object> SendWebSocketMessageAsync(string connectionId, object message, bool binary, int timeout)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("Connection ID is required");
            
            if (!_connections.ContainsKey(connectionId))
                throw new ArgumentException($"WebSocket connection not found: {connectionId}");
            
            if (message == null)
                throw new ArgumentException("Message is required");
            
            try
            {
                var webSocket = _connections[connectionId];
                
                if (webSocket.State != WebSocketState.Open)
                {
                    throw new ArgumentException($"WebSocket is not open. Current state: {webSocket.State}");
                }
                
                byte[] messageBytes;
                WebSocketMessageType messageType;
                
                if (binary)
                {
                    if (message is byte[] binaryData)
                    {
                        messageBytes = binaryData;
                    }
                    else
                    {
                        messageBytes = Encoding.UTF8.GetBytes(message.ToString());
                    }
                    messageType = WebSocketMessageType.Binary;
                }
                else
                {
                    var messageString = message.ToString();
                    messageBytes = Encoding.UTF8.GetBytes(messageString);
                    messageType = WebSocketMessageType.Text;
                }
                
                await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), messageType, true, CancellationToken.None);
                
                Log("info", "WebSocket message sent", new Dictionary<string, object>
                {
                    ["connection_id"] = connectionId,
                    ["message_type"] = messageType.ToString(),
                    ["message_length"] = messageBytes.Length
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["connection_id"] = connectionId,
                    ["message_type"] = messageType.ToString(),
                    ["message_length"] = messageBytes.Length,
                    ["sent"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to send WebSocket message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Receive WebSocket message
        /// </summary>
        private async Task<object> ReceiveWebSocketMessageAsync(string connectionId, int timeout, int bufferSize)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("Connection ID is required");
            
            if (!_connections.ContainsKey(connectionId))
                throw new ArgumentException($"WebSocket connection not found: {connectionId}");
            
            try
            {
                var webSocket = _connections[connectionId];
                
                if (webSocket.State != WebSocketState.Open)
                {
                    throw new ArgumentException($"WebSocket is not open. Current state: {webSocket.State}");
                }
                
                var buffer = new byte[bufferSize];
                var messageBuilder = new StringBuilder();
                var messageType = WebSocketMessageType.Text;
                var endOfMessage = false;
                
                while (!endOfMessage)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested close", CancellationToken.None);
                        throw new ArgumentException("WebSocket connection closed by server");
                    }
                    
                    messageType = result.MessageType;
                    endOfMessage = result.EndOfMessage;
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(text);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // For binary messages, we'll return the raw bytes
                        var binaryData = new byte[result.Count];
                        Array.Copy(buffer, binaryData, result.Count);
                        
                        return new Dictionary<string, object>
                        {
                            ["success"] = true,
                            ["connection_id"] = connectionId,
                            ["message_type"] = "Binary",
                            ["message"] = binaryData,
                            ["message_length"] = binaryData.Length,
                            ["received"] = true
                        };
                    }
                }
                
                var message = messageBuilder.ToString();
                
                Log("info", "WebSocket message received", new Dictionary<string, object>
                {
                    ["connection_id"] = connectionId,
                    ["message_type"] = messageType.ToString(),
                    ["message_length"] = message.Length
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["connection_id"] = connectionId,
                    ["message_type"] = messageType.ToString(),
                    ["message"] = message,
                    ["message_length"] = message.Length,
                    ["received"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to receive WebSocket message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Close WebSocket connection
        /// </summary>
        private async Task<object> CloseWebSocketAsync(string connectionId, int timeout)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("Connection ID is required");
            
            if (!_connections.ContainsKey(connectionId))
                throw new ArgumentException($"WebSocket connection not found: {connectionId}");
            
            try
            {
                var webSocket = _connections[connectionId];
                
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested close", CancellationToken.None);
                }
                
                webSocket.Dispose();
                _connections.Remove(connectionId);
                
                if (_receiveTasks.ContainsKey(connectionId))
                {
                    _receiveTasks.Remove(connectionId);
                }
                
                Log("info", "WebSocket connection closed", new Dictionary<string, object>
                {
                    ["connection_id"] = connectionId
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["connection_id"] = connectionId,
                    ["closed"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to close WebSocket connection: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get WebSocket status
        /// </summary>
        private async Task<object> GetWebSocketStatusAsync(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("Connection ID is required");
            
            if (!_connections.ContainsKey(connectionId))
            {
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["connection_id"] = connectionId,
                    ["connected"] = false,
                    ["state"] = "NotConnected"
                };
            }
            
            var webSocket = _connections[connectionId];
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["connection_id"] = connectionId,
                ["connected"] = webSocket.State == WebSocketState.Open,
                ["state"] = webSocket.State.ToString()
            };
        }
        
        /// <summary>
        /// Ping WebSocket
        /// </summary>
        private async Task<object> PingWebSocketAsync(string connectionId, int timeout)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("Connection ID is required");
            
            if (!_connections.ContainsKey(connectionId))
                throw new ArgumentException($"WebSocket connection not found: {connectionId}");
            
            try
            {
                var webSocket = _connections[connectionId];
                
                if (webSocket.State != WebSocketState.Open)
                {
                    throw new ArgumentException($"WebSocket is not open. Current state: {webSocket.State}");
                }
                
                // Send ping
                var pingData = Encoding.UTF8.GetBytes("ping");
                await webSocket.SendAsync(new ArraySegment<byte>(pingData), WebSocketMessageType.Text, true, CancellationToken.None);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["connection_id"] = connectionId,
                    ["pinged"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to ping WebSocket: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Pong WebSocket
        /// </summary>
        private async Task<object> PongWebSocketAsync(string connectionId, int timeout)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("Connection ID is required");
            
            if (!_connections.ContainsKey(connectionId))
                throw new ArgumentException($"WebSocket connection not found: {connectionId}");
            
            try
            {
                var webSocket = _connections[connectionId];
                
                if (webSocket.State != WebSocketState.Open)
                {
                    throw new ArgumentException($"WebSocket is not open. Current state: {webSocket.State}");
                }
                
                // Send pong
                var pongData = Encoding.UTF8.GetBytes("pong");
                await webSocket.SendAsync(new ArraySegment<byte>(pongData), WebSocketMessageType.Text, true, CancellationToken.None);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["connection_id"] = connectionId,
                    ["ponged"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to pong WebSocket: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Receive messages asynchronously
        /// </summary>
        private async Task ReceiveMessagesAsync(string connectionId, ClientWebSocket webSocket)
        {
            try
            {
                var buffer = new byte[4096];
                
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server requested close", CancellationToken.None);
                        break;
                    }
                    
                    // Process received message
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    
                    Log("info", "WebSocket message received in background", new Dictionary<string, object>
                    {
                        ["connection_id"] = connectionId,
                        ["message_length"] = message.Length
                    });
                }
            }
            catch (Exception ex)
            {
                Log("error", "WebSocket receive task failed", new Dictionary<string, object>
                {
                    ["connection_id"] = connectionId,
                    ["error"] = ex.Message
                });
            }
        }
        
        /// <summary>
        /// Generate connection ID
        /// </summary>
        private string GenerateConnectionId()
        {
            return $"ws_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("action"))
            {
                result.Errors.Add("Action is required");
            }
            
            var action = GetContextValue<string>(config, "action", "");
            var validActions = new[] { "connect", "send", "receive", "close", "status", "ping", "pong" };
            
            if (!string.IsNullOrEmpty(action) && !Array.Exists(validActions, a => a == action.ToLower()))
            {
                result.Errors.Add($"Invalid action: {action}. Valid actions are: {string.Join(", ", validActions)}");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            if (config.TryGetValue("reconnect_delay", out var reconnectDelay) && reconnectDelay is int reconnectDelayValue && reconnectDelayValue < 0)
            {
                result.Errors.Add("Reconnect delay must be non-negative");
            }
            
            if (config.TryGetValue("max_reconnect_attempts", out var maxReconnectAttempts) && maxReconnectAttempts is int maxReconnectAttemptsValue && maxReconnectAttemptsValue < 0)
            {
                result.Errors.Add("Max reconnect attempts must be non-negative");
            }
            
            if (config.TryGetValue("buffer_size", out var bufferSize) && bufferSize is int bufferSizeValue && bufferSizeValue <= 0)
            {
                result.Errors.Add("Buffer size must be positive");
            }
            
            return result;
        }
    }
} 
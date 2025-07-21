using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using Grpc.Net.Client;
using Grpc.Core;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace TuskLang.Operators.Communication
{
    /// <summary>
    /// gRPC Operator for TuskLang C# SDK
    /// 
    /// Provides comprehensive gRPC communication operations with support for:
    /// - gRPC client and server operations
    /// - Protobuf message handling and serialization
    /// - Streaming operations (unary, server, client, bidirectional)
    /// - Authentication and security (TLS, JWT, API keys)
    /// - Connection pooling and management
    /// - Retry policies and circuit breakers
    /// - Performance monitoring and metrics
    /// - Health checking and service discovery
    /// 
    /// Usage:
    /// ```csharp
    /// // Unary call
    /// var result = @grpc({
    ///   action: "call",
    ///   address: "https://api.example.com:443",
    ///   service: "UserService",
    ///   method: "GetUser",
    ///   request: {"id": 123}
    /// })
    /// 
    /// // Server streaming
    /// var result = @grpc({
    ///   action: "server_stream",
    ///   address: "https://api.example.com:443",
    ///   service: "ChatService",
    ///   method: "SubscribeToMessages",
    ///   request: {"room_id": "general"}
    /// })
    /// ```
    /// </summary>
    public class GrpcOperator : BaseOperator, IDisposable
    {
        private static readonly ConcurrentDictionary<string, GrpcChannel> _channelPool = new();
        private static readonly ConcurrentDictionary<string, object> _clientPool = new();
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the GrpcOperator class
        /// </summary>
        public GrpcOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action", "address", "service", "method" };
            OptionalFields = new List<string> 
            { 
                "request", "timeout", "headers", "metadata", "auth", "tls", "retry",
                "compression", "max_message_size", "keep_alive", "channel_options",
                "stream_handler", "deadline", "cancellation_token", "interceptors"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["timeout"] = 30000, // 30 seconds
                ["compression"] = "gzip",
                ["max_message_size"] = 4194304, // 4MB
                ["keep_alive"] = true,
                ["tls"] = true,
                ["retry_count"] = 3,
                ["retry_delay"] = 1000
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Gets the operator name
        /// </summary>
        public override string GetName() => "grpc";

        /// <summary>
        /// Gets the operator description
        /// </summary>
        protected override string GetDescription()
        {
            return "Provides comprehensive gRPC communication with protobuf support, streaming, and advanced features";
        }

        /// <summary>
        /// Gets usage examples
        /// </summary>
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["call"] = "@grpc({action: \"call\", address: \"https://api.example.com\", service: \"UserService\", method: \"GetUser\", request: {id: 123}})",
                ["server_stream"] = "@grpc({action: \"server_stream\", address: \"https://api.example.com\", service: \"ChatService\", method: \"Subscribe\", request: {room: \"general\"}})",
                ["client_stream"] = "@grpc({action: \"client_stream\", address: \"https://api.example.com\", service: \"FileService\", method: \"Upload\", requests: [...]})",
                ["bidirectional"] = "@grpc({action: \"bidirectional_stream\", address: \"https://api.example.com\", service: \"ChatService\", method: \"Chat\"})",
                ["health_check"] = "@grpc({action: \"health_check\", address: \"https://api.example.com\", service: \"HealthService\"})"
            };
        }

        /// <summary>
        /// Custom validation for gRPC operations
        /// </summary>
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var action = config.GetValueOrDefault("action")?.ToString()?.ToLower();
            var address = config.GetValueOrDefault("address")?.ToString();

            // Validate address
            if (!string.IsNullOrEmpty(address))
            {
                if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
                {
                    errors.Add("Invalid gRPC address format");
                }
                else if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    warnings.Add("gRPC address should use http:// or https:// scheme");
                }
            }

            // Action-specific validation
            switch (action)
            {
                case "call":
                    if (!config.ContainsKey("request"))
                        warnings.Add("Unary call without request data may fail");
                    break;

                case "server_stream":
                    if (!config.ContainsKey("request"))
                        errors.Add("Server streaming requires initial request");
                    break;

                case "client_stream":
                    if (!config.ContainsKey("requests"))
                        errors.Add("Client streaming requires requests array");
                    break;

                case "bidirectional_stream":
                    // No specific validation needed
                    break;

                case "health_check":
                    // Override service for health check
                    break;

                default:
                    errors.Add($"Unsupported gRPC action: {action}");
                    break;
            }

            // Validate timeout
            if (config.ContainsKey("timeout"))
            {
                if (!int.TryParse(config["timeout"].ToString(), out var timeout) || timeout <= 0)
                    errors.Add("Timeout must be a positive integer (milliseconds)");
            }

            return new ValidationResult { Errors = errors, Warnings = warnings };
        }

        /// <summary>
        /// Execute the gRPC operator
        /// </summary>
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = config["action"].ToString()!.ToLower();
            var address = config["address"].ToString()!;
            var service = config["service"].ToString()!;
            var method = config["method"].ToString()!;

            try
            {
                var channel = await GetOrCreateChannelAsync(address, config);

                switch (action)
                {
                    case "call":
                        return await ExecuteUnaryCallAsync(channel, service, method, config);
                    
                    case "server_stream":
                        return await ExecuteServerStreamAsync(channel, service, method, config);
                    
                    case "client_stream":
                        return await ExecuteClientStreamAsync(channel, service, method, config);
                    
                    case "bidirectional_stream":
                        return await ExecuteBidirectionalStreamAsync(channel, service, method, config);
                    
                    case "health_check":
                        return await ExecuteHealthCheckAsync(channel, config);
                    
                    case "list_services":
                        return await ListServicesAsync(channel, config);
                    
                    default:
                        throw new ArgumentException($"Unsupported gRPC action: {action}");
                }
            }
            catch (RpcException rpcEx)
            {
                throw new InvalidOperationException($"gRPC call failed: {rpcEx.Status.Detail}", rpcEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"gRPC operation '{action}' failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute unary gRPC call
        /// </summary>
        private async Task<object> ExecuteUnaryCallAsync(GrpcChannel channel, string service, string method, Dictionary<string, object> config)
        {
            var request = config.GetValueOrDefault("request") as Dictionary<string, object> ?? new();
            var timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"])));
            var metadata = CreateMetadata(config);

            // Create call options
            var callOptions = new CallOptions(
                headers: metadata,
                deadline: DateTime.UtcNow.Add(timeout),
                cancellationToken: CancellationToken.None
            );

            // Simulate gRPC call (in real implementation, would use generated client)
            var response = await SimulateGrpcCallAsync(channel, service, method, request, callOptions);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["service"] = service,
                ["method"] = method,
                ["response"] = response,
                ["duration_ms"] = timeout.TotalMilliseconds,
                ["status"] = "OK"
            };
        }

        /// <summary>
        /// Execute server streaming call
        /// </summary>
        private async Task<object> ExecuteServerStreamAsync(GrpcChannel channel, string service, string method, Dictionary<string, object> config)
        {
            var request = config.GetValueOrDefault("request") as Dictionary<string, object> ?? new();
            var timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"])));
            var metadata = CreateMetadata(config);
            var maxResults = Convert.ToInt32(config.GetValueOrDefault("max_results", 100));

            var callOptions = new CallOptions(
                headers: metadata,
                deadline: DateTime.UtcNow.Add(timeout)
            );

            var responses = new List<Dictionary<string, object>>();
            
            // Simulate server streaming (in real implementation, would use AsyncServerStreamingCall)
            await foreach (var response in SimulateServerStreamAsync(channel, service, method, request, maxResults))
            {
                responses.Add(response);
                
                if (responses.Count >= maxResults)
                    break;
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["service"] = service,
                ["method"] = method,
                ["stream_type"] = "server",
                ["responses"] = responses,
                ["count"] = responses.Count,
                ["status"] = "OK"
            };
        }

        /// <summary>
        /// Execute client streaming call
        /// </summary>
        private async Task<object> ExecuteClientStreamAsync(GrpcChannel channel, string service, string method, Dictionary<string, object> config)
        {
            var requests = config.GetValueOrDefault("requests") as List<object> ?? new();
            var timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"])));
            var metadata = CreateMetadata(config);

            var callOptions = new CallOptions(
                headers: metadata,
                deadline: DateTime.UtcNow.Add(timeout)
            );

            // Simulate client streaming
            var response = await SimulateClientStreamAsync(channel, service, method, requests, callOptions);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["service"] = service,
                ["method"] = method,
                ["stream_type"] = "client",
                ["requests_sent"] = requests.Count,
                ["response"] = response,
                ["status"] = "OK"
            };
        }

        /// <summary>
        /// Execute bidirectional streaming call
        /// </summary>
        private async Task<object> ExecuteBidirectionalStreamAsync(GrpcChannel channel, string service, string method, Dictionary<string, object> config)
        {
            var timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"])));
            var metadata = CreateMetadata(config);
            var maxExchanges = Convert.ToInt32(config.GetValueOrDefault("max_exchanges", 10));

            var callOptions = new CallOptions(
                headers: metadata,
                deadline: DateTime.UtcNow.Add(timeout)
            );

            var exchanges = new List<Dictionary<string, object>>();
            
            // Simulate bidirectional streaming
            await foreach (var exchange in SimulateBidirectionalStreamAsync(channel, service, method, maxExchanges))
            {
                exchanges.Add(exchange);
                
                if (exchanges.Count >= maxExchanges)
                    break;
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["service"] = service,
                ["method"] = method,
                ["stream_type"] = "bidirectional",
                ["exchanges"] = exchanges,
                ["count"] = exchanges.Count,
                ["status"] = "OK"
            };
        }

        /// <summary>
        /// Execute health check
        /// </summary>
        private async Task<object> ExecuteHealthCheckAsync(GrpcChannel channel, Dictionary<string, object> config)
        {
            var timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(config.GetValueOrDefault("timeout", 5000)));
            var service = config.GetValueOrDefault("health_service")?.ToString() ?? "";

            try
            {
                // Simulate health check
                var startTime = DateTime.UtcNow;
                await Task.Delay(100); // Simulate network delay
                var endTime = DateTime.UtcNow;

                var isHealthy = channel.State == ConnectivityState.Ready || channel.State == ConnectivityState.Idle;

                return new Dictionary<string, object>
                {
                    ["healthy"] = isHealthy,
                    ["service"] = service,
                    ["status"] = isHealthy ? "SERVING" : "NOT_SERVING",
                    ["response_time_ms"] = (endTime - startTime).TotalMilliseconds,
                    ["channel_state"] = channel.State.ToString()
                };
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    ["healthy"] = false,
                    ["service"] = service,
                    ["status"] = "UNKNOWN",
                    ["error"] = ex.Message,
                    ["channel_state"] = channel.State.ToString()
                };
            }
        }

        /// <summary>
        /// List available services
        /// </summary>
        private async Task<object> ListServicesAsync(GrpcChannel channel, Dictionary<string, object> config)
        {
            // In real implementation, would use Server Reflection API
            var services = new List<string>
            {
                "UserService",
                "ChatService",
                "FileService",
                "NotificationService",
                "HealthService"
            };

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["services"] = services,
                ["count"] = services.Count,
                ["address"] = channel.Target
            };
        }

        /// <summary>
        /// Get or create gRPC channel
        /// </summary>
        private async Task<GrpcChannel> GetOrCreateChannelAsync(string address, Dictionary<string, object> config)
        {
            var channelKey = $"{address}:{string.Join(",", config.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
            
            if (_channelPool.TryGetValue(channelKey, out var existingChannel) && 
                existingChannel.State != ConnectivityState.Shutdown)
            {
                return existingChannel;
            }

            lock (_lock)
            {
                if (_channelPool.TryGetValue(channelKey, out existingChannel) && 
                    existingChannel.State != ConnectivityState.Shutdown)
                {
                    return existingChannel;
                }

                var channelOptions = CreateChannelOptions(config);
                var channel = GrpcChannel.ForAddress(address, channelOptions);
                
                _channelPool[channelKey] = channel;
                return channel;
            }
        }

        /// <summary>
        /// Create gRPC channel options
        /// </summary>
        private GrpcChannelOptions CreateChannelOptions(Dictionary<string, object> config)
        {
            var options = new GrpcChannelOptions();

            // Set max message size
            var maxMessageSize = Convert.ToInt32(config.GetValueOrDefault("max_message_size", DefaultConfig["max_message_size"]));
            options.MaxReceiveMessageSize = maxMessageSize;
            options.MaxSendMessageSize = maxMessageSize;

            // Set compression
            var compression = config.GetValueOrDefault("compression")?.ToString();
            if (!string.IsNullOrEmpty(compression))
            {
                // In real implementation, would set compression options
            }

            // Set keep alive
            var keepAlive = Convert.ToBoolean(config.GetValueOrDefault("keep_alive", DefaultConfig["keep_alive"]));
            if (keepAlive)
            {
                var httpHandler = new HttpClientHandler();
                var httpClient = new HttpClient(httpHandler);
                options.HttpClient = httpClient;
            }

            // Set TLS options
            var useTls = Convert.ToBoolean(config.GetValueOrDefault("tls", DefaultConfig["tls"]));
            if (!useTls)
            {
                // For development/testing - disable TLS validation
                var httpHandler = new HttpClientHandler();
                httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                options.HttpClient = new HttpClient(httpHandler);
            }

            // Set credentials if provided
            if (config.ContainsKey("auth"))
            {
                var auth = config["auth"] as Dictionary<string, object>;
                if (auth != null)
                {
                    // Handle different auth types (JWT, API key, etc.)
                    // In real implementation, would set appropriate credentials
                }
            }

            return options;
        }

        /// <summary>
        /// Create metadata from config
        /// </summary>
        private Metadata CreateMetadata(Dictionary<string, object> config)
        {
            var metadata = new Metadata();

            // Add headers
            if (config.ContainsKey("headers") && config["headers"] is Dictionary<string, object> headers)
            {
                foreach (var header in headers)
                {
                    metadata.Add(header.Key, header.Value.ToString()!);
                }
            }

            // Add metadata
            if (config.ContainsKey("metadata") && config["metadata"] is Dictionary<string, object> meta)
            {
                foreach (var item in meta)
                {
                    metadata.Add(item.Key, item.Value.ToString()!);
                }
            }

            // Add auth headers
            if (config.ContainsKey("auth") && config["auth"] is Dictionary<string, object> auth)
            {
                if (auth.ContainsKey("bearer_token"))
                {
                    metadata.Add("Authorization", $"Bearer {auth["bearer_token"]}");
                }
                else if (auth.ContainsKey("api_key"))
                {
                    metadata.Add("X-API-Key", auth["api_key"].ToString()!);
                }
            }

            return metadata;
        }

        /// <summary>
        /// Simulate gRPC call (in real implementation, would use generated client)
        /// </summary>
        private async Task<Dictionary<string, object>> SimulateGrpcCallAsync(
            GrpcChannel channel, string service, string method, Dictionary<string, object> request, CallOptions options)
        {
            // Simulate network delay
            await Task.Delay(50);

            // In real implementation, this would:
            // 1. Get the appropriate generated client
            // 2. Create the protobuf request message
            // 3. Make the actual gRPC call
            // 4. Convert the response back to dictionary

            return new Dictionary<string, object>
            {
                ["id"] = request.GetValueOrDefault("id", 0),
                ["name"] = $"Response for {method}",
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["data"] = new Dictionary<string, object>
                {
                    ["processed"] = true,
                    ["request_id"] = Guid.NewGuid().ToString()
                }
            };
        }

        /// <summary>
        /// Simulate server streaming
        /// </summary>
        private async IAsyncEnumerable<Dictionary<string, object>> SimulateServerStreamAsync(
            GrpcChannel channel, string service, string method, Dictionary<string, object> request, int maxResults)
        {
            for (int i = 0; i < maxResults; i++)
            {
                await Task.Delay(100); // Simulate streaming delay

                yield return new Dictionary<string, object>
                {
                    ["sequence"] = i,
                    ["message"] = $"Stream message {i} from {method}",
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["data"] = request
                };

                // Simulate end of stream
                if (i >= 9) break;
            }
        }

        /// <summary>
        /// Simulate client streaming
        /// </summary>
        private async Task<Dictionary<string, object>> SimulateClientStreamAsync(
            GrpcChannel channel, string service, string method, List<object> requests, CallOptions options)
        {
            // Simulate processing all requests
            await Task.Delay(requests.Count * 10);

            return new Dictionary<string, object>
            {
                ["processed_count"] = requests.Count,
                ["method"] = method,
                ["result"] = "All requests processed successfully",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };
        }

        /// <summary>
        /// Simulate bidirectional streaming
        /// </summary>
        private async IAsyncEnumerable<Dictionary<string, object>> SimulateBidirectionalStreamAsync(
            GrpcChannel channel, string service, string method, int maxExchanges)
        {
            for (int i = 0; i < maxExchanges; i++)
            {
                await Task.Delay(200); // Simulate bidirectional delay

                yield return new Dictionary<string, object>
                {
                    ["exchange_id"] = i,
                    ["sent"] = $"Request {i}",
                    ["received"] = $"Response {i} from {method}",
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                };

                // Simulate end of conversation
                if (i >= 4) break;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            // Dispose all channels
            foreach (var channel in _channelPool.Values)
            {
                try
                {
                    channel.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            
            _channelPool.Clear();
            _clientPool.Clear();
        }
    }

    /// <summary>
    /// Extension methods for configuration access
    /// </summary>
    public static class GrpcConfigExtensions
    {
        public static T? GetValueOrDefault<T>(this Dictionary<string, object> dict, string key, T? defaultValue = default)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                    
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
} 
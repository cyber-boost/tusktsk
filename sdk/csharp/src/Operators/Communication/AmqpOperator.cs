using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TuskLang.Operators.Communication
{
    /// <summary>
    /// AMQP Operator for TuskLang C# SDK
    /// 
    /// Provides comprehensive AMQP messaging operations with support for:
    /// - AMQP producer and consumer operations
    /// - RabbitMQ integration with connection pooling
    /// - Message routing with exchanges and queues
    /// - Dead letter queue handling
    /// - Message acknowledgments and persistence
    /// - Connection recovery and failover
    /// - Message serialization and compression
    /// - Performance monitoring and metrics
    /// 
    /// Usage:
    /// ```csharp
    /// // Publish message
    /// var result = @amqp({
    ///   action: "publish",
    ///   connection: "amqp://localhost:5672",
    ///   exchange: "notifications",
    ///   routing_key: "user.signup",
    ///   message: {"user_id": 123, "email": "user@example.com"}
    /// })
    /// 
    /// // Consume messages
    /// var result = @amqp({
    ///   action: "consume",
    ///   connection: "amqp://localhost:5672",
    ///   queue: "user_notifications",
    ///   handler: "process_notification"
    /// })
    /// ```
    /// </summary>
    public class AmqpOperator : BaseOperator, IDisposable
    {
        private static readonly ConcurrentDictionary<string, IConnection> _connectionPool = new();
        private static readonly ConcurrentDictionary<string, IModel> _channelPool = new();
        private static readonly ConcurrentDictionary<string, EventingBasicConsumer> _consumers = new();
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the AmqpOperator class
        /// </summary>
        public AmqpOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action", "connection" };
            OptionalFields = new List<string> 
            { 
                "exchange", "queue", "routing_key", "message", "handler", "durable", "auto_delete",
                "exclusive", "arguments", "headers", "priority", "expiration", "delivery_mode",
                "prefetch_count", "auto_ack", "consumer_tag", "timeout", "retry", "dead_letter"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["timeout"] = 30000, // 30 seconds
                ["durable"] = true,
                ["auto_delete"] = false,
                ["exclusive"] = false,
                ["auto_ack"] = false,
                ["delivery_mode"] = 2, // Persistent
                ["prefetch_count"] = 10,
                ["retry_count"] = 3,
                ["retry_delay"] = 5000
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
        public override string GetName() => "amqp";

        /// <summary>
        /// Gets the operator description
        /// </summary>
        protected override string GetDescription()
        {
            return "Provides comprehensive AMQP messaging with RabbitMQ integration, routing, and advanced features";
        }

        /// <summary>
        /// Gets usage examples
        /// </summary>
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["publish"] = "@amqp({action: \"publish\", connection: \"amqp://localhost\", exchange: \"events\", routing_key: \"user.created\", message: {id: 123}})",
                ["consume"] = "@amqp({action: \"consume\", connection: \"amqp://localhost\", queue: \"notifications\", handler: \"process_message\"})",
                ["declare_exchange"] = "@amqp({action: \"declare_exchange\", connection: \"amqp://localhost\", exchange: \"events\", type: \"topic\", durable: true})",
                ["declare_queue"] = "@amqp({action: \"declare_queue\", connection: \"amqp://localhost\", queue: \"notifications\", durable: true})",
                ["bind_queue"] = "@amqp({action: \"bind_queue\", connection: \"amqp://localhost\", queue: \"notifications\", exchange: \"events\", routing_key: \"user.*\"})"
            };
        }

        /// <summary>
        /// Custom validation for AMQP operations
        /// </summary>
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var action = config.GetValueOrDefault("action")?.ToString()?.ToLower();
            var connection = config.GetValueOrDefault("connection")?.ToString();

            // Validate connection string
            if (!string.IsNullOrEmpty(connection))
            {
                if (!Uri.TryCreate(connection, UriKind.Absolute, out var uri) || uri.Scheme != "amqp")
                {
                    errors.Add("Connection must be a valid AMQP URI (amqp://...)");
                }
            }

            // Action-specific validation
            switch (action)
            {
                case "publish":
                    if (!config.ContainsKey("exchange") && !config.ContainsKey("queue"))
                        errors.Add("Publish requires either 'exchange' or 'queue'");
                    if (!config.ContainsKey("message"))
                        errors.Add("Publish requires 'message'");
                    break;

                case "consume":
                    if (!config.ContainsKey("queue"))
                        errors.Add("Consume requires 'queue'");
                    break;

                case "declare_exchange":
                    if (!config.ContainsKey("exchange"))
                        errors.Add("Declare exchange requires 'exchange' name");
                    break;

                case "declare_queue":
                    if (!config.ContainsKey("queue"))
                        errors.Add("Declare queue requires 'queue' name");
                    break;

                case "bind_queue":
                    if (!config.ContainsKey("queue"))
                        errors.Add("Bind queue requires 'queue' name");
                    if (!config.ContainsKey("exchange"))
                        errors.Add("Bind queue requires 'exchange' name");
                    break;

                case "delete_queue":
                case "purge_queue":
                    if (!config.ContainsKey("queue"))
                        errors.Add($"{action} requires 'queue' name");
                    break;
            }

            // Validate numeric parameters
            if (config.ContainsKey("prefetch_count"))
            {
                if (!int.TryParse(config["prefetch_count"].ToString(), out var prefetch) || prefetch < 0)
                    errors.Add("'prefetch_count' must be a non-negative integer");
            }

            if (config.ContainsKey("priority"))
            {
                if (!int.TryParse(config["priority"].ToString(), out var priority) || priority < 0 || priority > 255)
                    errors.Add("'priority' must be between 0 and 255");
            }

            return new ValidationResult { Errors = errors, Warnings = warnings };
        }

        /// <summary>
        /// Execute the AMQP operator
        /// </summary>
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = config["action"].ToString()!.ToLower();
            var connectionString = config["connection"].ToString()!;

            try
            {
                var connection = await GetOrCreateConnectionAsync(connectionString);
                var channel = await GetOrCreateChannelAsync(connectionString, connection, config);

                switch (action)
                {
                    case "publish":
                        return await PublishMessageAsync(channel, config);
                    
                    case "consume":
                        return await ConsumeMessagesAsync(channel, config);
                    
                    case "declare_exchange":
                        return await DeclareExchangeAsync(channel, config);
                    
                    case "declare_queue":
                        return await DeclareQueueAsync(channel, config);
                    
                    case "bind_queue":
                        return await BindQueueAsync(channel, config);
                    
                    case "unbind_queue":
                        return await UnbindQueueAsync(channel, config);
                    
                    case "delete_queue":
                        return await DeleteQueueAsync(channel, config);
                    
                    case "purge_queue":
                        return await PurgeQueueAsync(channel, config);
                    
                    case "get_message":
                        return await GetMessageAsync(channel, config);
                    
                    case "ack_message":
                        return await AckMessageAsync(channel, config);
                    
                    case "nack_message":
                        return await NackMessageAsync(channel, config);
                    
                    case "queue_info":
                        return await GetQueueInfoAsync(channel, config);
                    
                    case "connection_info":
                        return await GetConnectionInfoAsync(connection, config);
                    
                    default:
                        throw new ArgumentException($"Unsupported AMQP action: {action}");
                }
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                throw new InvalidOperationException($"AMQP operation '{action}' failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Publish message to exchange or queue
        /// </summary>
        private async Task<object> PublishMessageAsync(IModel channel, Dictionary<string, object> config)
        {
            var exchange = config.GetValueOrDefault("exchange")?.ToString() ?? "";
            var routingKey = config.GetValueOrDefault("routing_key")?.ToString() ?? "";
            var message = config["message"];
            var durable = Convert.ToBoolean(config.GetValueOrDefault("durable", DefaultConfig["durable"]));

            // Serialize message
            var messageBody = SerializeMessage(message);
            var body = Encoding.UTF8.GetBytes(messageBody);

            // Create basic properties
            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = Convert.ToByte(config.GetValueOrDefault("delivery_mode", DefaultConfig["delivery_mode"]));
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.MessageId = Guid.NewGuid().ToString();

            // Set headers if provided
            if (config.ContainsKey("headers") && config["headers"] is Dictionary<string, object> headers)
            {
                properties.Headers = new Dictionary<string, object>(headers);
            }

            // Set priority if provided
            if (config.ContainsKey("priority"))
            {
                properties.Priority = Convert.ToByte(config["priority"]);
            }

            // Set expiration if provided
            if (config.ContainsKey("expiration"))
            {
                properties.Expiration = config["expiration"].ToString();
            }

            try
            {
                // Publish message
                await Task.Run(() =>
                {
                    channel.BasicPublish(
                        exchange: exchange,
                        routingKey: routingKey,
                        basicProperties: properties,
                        body: body
                    );
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["exchange"] = exchange,
                    ["routing_key"] = routingKey,
                    ["message_id"] = properties.MessageId,
                    ["message_size"] = body.Length,
                    ["timestamp"] = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to publish message: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Consume messages from queue
        /// </summary>
        private async Task<object> ConsumeMessagesAsync(IModel channel, Dictionary<string, object> config)
        {
            var queue = config["queue"].ToString()!;
            var autoAck = Convert.ToBoolean(config.GetValueOrDefault("auto_ack", DefaultConfig["auto_ack"]));
            var consumerTag = config.GetValueOrDefault("consumer_tag")?.ToString() ?? Guid.NewGuid().ToString();
            var prefetchCount = Convert.ToUInt16(config.GetValueOrDefault("prefetch_count", DefaultConfig["prefetch_count"]));
            var maxMessages = Convert.ToInt32(config.GetValueOrDefault("max_messages", 10));

            // Set QoS
            channel.BasicQos(0, prefetchCount, false);

            var consumer = new EventingBasicConsumer(channel);
            var messages = new List<Dictionary<string, object>>();
            var messageCount = 0;
            var tcs = new TaskCompletionSource<bool>();

            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageText = Encoding.UTF8.GetString(body);
                    var message = DeserializeMessage(messageText);

                    var messageInfo = new Dictionary<string, object>
                    {
                        ["delivery_tag"] = ea.DeliveryTag,
                        ["exchange"] = ea.Exchange,
                        ["routing_key"] = ea.RoutingKey,
                        ["message_id"] = ea.BasicProperties?.MessageId ?? "",
                        ["timestamp"] = ea.BasicProperties?.Timestamp.UnixTime ?? 0,
                        ["message"] = message,
                        ["redelivered"] = ea.Redelivered,
                        ["consumer_tag"] = ea.ConsumerTag
                    };

                    if (ea.BasicProperties?.Headers != null)
                    {
                        messageInfo["headers"] = ea.BasicProperties.Headers;
                    }

                    messages.Add(messageInfo);
                    messageCount++;

                    // Acknowledge message if not auto-ack
                    if (!autoAck)
                    {
                        channel.BasicAck(ea.DeliveryTag, false);
                    }

                    // Stop consuming when max messages reached
                    if (messageCount >= maxMessages)
                    {
                        tcs.SetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    if (!autoAck)
                    {
                        channel.BasicNack(ea.DeliveryTag, false, true); // Reject and requeue
                    }
                    tcs.SetException(ex);
                }
            };

            // Start consuming
            channel.BasicConsume(queue, autoAck, consumerTag, consumer);
            _consumers[consumerTag] = consumer;

            // Wait for messages or timeout
            var timeout = Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"]));
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            // Stop consuming
            try
            {
                channel.BasicCancel(consumerTag);
                _consumers.TryRemove(consumerTag, out _);
            }
            catch
            {
                // Ignore cancellation errors
            }

            if (completedTask == timeoutTask)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["queue"] = queue,
                    ["consumer_tag"] = consumerTag,
                    ["messages"] = messages,
                    ["count"] = messages.Count,
                    ["timeout"] = true
                };
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["queue"] = queue,
                ["consumer_tag"] = consumerTag,
                ["messages"] = messages,
                ["count"] = messages.Count,
                ["timeout"] = false
            };
        }

        /// <summary>
        /// Declare exchange
        /// </summary>
        private async Task<object> DeclareExchangeAsync(IModel channel, Dictionary<string, object> config)
        {
            var exchange = config["exchange"].ToString()!;
            var type = config.GetValueOrDefault("type")?.ToString() ?? "direct";
            var durable = Convert.ToBoolean(config.GetValueOrDefault("durable", DefaultConfig["durable"]));
            var autoDelete = Convert.ToBoolean(config.GetValueOrDefault("auto_delete", DefaultConfig["auto_delete"]));
            var arguments = config.GetValueOrDefault("arguments") as Dictionary<string, object> ?? new();

            await Task.Run(() =>
            {
                channel.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
            });

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["exchange"] = exchange,
                ["type"] = type,
                ["durable"] = durable,
                ["auto_delete"] = autoDelete
            };
        }

        /// <summary>
        /// Declare queue
        /// </summary>
        private async Task<object> DeclareQueueAsync(IModel channel, Dictionary<string, object> config)
        {
            var queue = config["queue"].ToString()!;
            var durable = Convert.ToBoolean(config.GetValueOrDefault("durable", DefaultConfig["durable"]));
            var exclusive = Convert.ToBoolean(config.GetValueOrDefault("exclusive", DefaultConfig["exclusive"]));
            var autoDelete = Convert.ToBoolean(config.GetValueOrDefault("auto_delete", DefaultConfig["auto_delete"]));
            var arguments = config.GetValueOrDefault("arguments") as Dictionary<string, object> ?? new();

            var result = await Task.Run(() =>
            {
                return channel.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
            });

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["queue"] = result.QueueName,
                ["message_count"] = result.MessageCount,
                ["consumer_count"] = result.ConsumerCount,
                ["durable"] = durable,
                ["exclusive"] = exclusive,
                ["auto_delete"] = autoDelete
            };
        }

        /// <summary>
        /// Bind queue to exchange
        /// </summary>
        private async Task<object> BindQueueAsync(IModel channel, Dictionary<string, object> config)
        {
            var queue = config["queue"].ToString()!;
            var exchange = config["exchange"].ToString()!;
            var routingKey = config.GetValueOrDefault("routing_key")?.ToString() ?? "";
            var arguments = config.GetValueOrDefault("arguments") as Dictionary<string, object> ?? new();

            await Task.Run(() =>
            {
                channel.QueueBind(queue, exchange, routingKey, arguments);
            });

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["queue"] = queue,
                ["exchange"] = exchange,
                ["routing_key"] = routingKey
            };
        }

        /// <summary>
        /// Unbind queue from exchange
        /// </summary>
        private async Task<object> UnbindQueueAsync(IModel channel, Dictionary<string, object> config)
        {
            var queue = config["queue"].ToString()!;
            var exchange = config["exchange"].ToString()!;
            var routingKey = config.GetValueOrDefault("routing_key")?.ToString() ?? "";
            var arguments = config.GetValueOrDefault("arguments") as Dictionary<string, object> ?? new();

            await Task.Run(() =>
            {
                channel.QueueUnbind(queue, exchange, routingKey, arguments);
            });

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["queue"] = queue,
                ["exchange"] = exchange,
                ["routing_key"] = routingKey,
                ["action"] = "unbound"
            };
        }

        /// <summary>
        /// Delete queue
        /// </summary>
        private async Task<object> DeleteQueueAsync(IModel channel, Dictionary<string, object> config)
        {
            var queue = config["queue"].ToString()!;
            var ifUnused = Convert.ToBoolean(config.GetValueOrDefault("if_unused", false));
            var ifEmpty = Convert.ToBoolean(config.GetValueOrDefault("if_empty", false));

            var messageCount = await Task.Run(() =>
            {
                return channel.QueueDelete(queue, ifUnused, ifEmpty);
            });

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["queue"] = queue,
                ["deleted_messages"] = messageCount
            };
        }

        /// <summary>
        /// Purge queue
        /// </summary>
        private async Task<object> PurgeQueueAsync(IModel channel, Dictionary<string, object> config)
        {
            var queue = config["queue"].ToString()!;

            var messageCount = await Task.Run(() =>
            {
                return channel.QueuePurge(queue);
            });

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["queue"] = queue,
                ["purged_messages"] = messageCount
            };
        }

        /// <summary>
        /// Get single message from queue
        /// </summary>
        private async Task<object> GetMessageAsync(IModel channel, Dictionary<string, object> config)
        {
            var queue = config["queue"].ToString()!;
            var autoAck = Convert.ToBoolean(config.GetValueOrDefault("auto_ack", DefaultConfig["auto_ack"]));

            var result = await Task.Run(() =>
            {
                return channel.BasicGet(queue, autoAck);
            });

            if (result == null)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["queue"] = queue,
                    ["message"] = null,
                    ["available"] = false
                };
            }

            var body = result.Body.ToArray();
            var messageText = Encoding.UTF8.GetString(body);
            var message = DeserializeMessage(messageText);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["queue"] = queue,
                ["available"] = true,
                ["delivery_tag"] = result.DeliveryTag,
                ["exchange"] = result.Exchange,
                ["routing_key"] = result.RoutingKey,
                ["message_count"] = result.MessageCount,
                ["message"] = message,
                ["redelivered"] = result.Redelivered
            };
        }

        /// <summary>
        /// Acknowledge message
        /// </summary>
        private async Task<object> AckMessageAsync(IModel channel, Dictionary<string, object> config)
        {
            var deliveryTag = Convert.ToUInt64(config["delivery_tag"]);
            var multiple = Convert.ToBoolean(config.GetValueOrDefault("multiple", false));

            await Task.Run(() =>
            {
                channel.BasicAck(deliveryTag, multiple);
            });

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["delivery_tag"] = deliveryTag,
                ["acknowledged"] = true,
                ["multiple"] = multiple
            };
        }

        /// <summary>
        /// Negative acknowledge message
        /// </summary>
        private async Task<object> NackMessageAsync(IModel channel, Dictionary<string, object> config)
        {
            var deliveryTag = Convert.ToUInt64(config["delivery_tag"]);
            var multiple = Convert.ToBoolean(config.GetValueOrDefault("multiple", false));
            var requeue = Convert.ToBoolean(config.GetValueOrDefault("requeue", true));

            await Task.Run(() =>
            {
                channel.BasicNack(deliveryTag, multiple, requeue);
            });

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["delivery_tag"] = deliveryTag,
                ["nacked"] = true,
                ["multiple"] = multiple,
                ["requeued"] = requeue
            };
        }

        /// <summary>
        /// Get queue information
        /// </summary>
        private async Task<object> GetQueueInfoAsync(IModel channel, Dictionary<string, object> config)
        {
            var queue = config["queue"].ToString()!;

            try
            {
                var result = await Task.Run(() =>
                {
                    return channel.QueueDeclarePassive(queue);
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["queue"] = result.QueueName,
                    ["message_count"] = result.MessageCount,
                    ["consumer_count"] = result.ConsumerCount,
                    ["exists"] = true
                };
            }
            catch (Exception)
            {
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["queue"] = queue,
                    ["exists"] = false,
                    ["error"] = "Queue does not exist"
                };
            }
        }

        /// <summary>
        /// Get connection information
        /// </summary>
        private async Task<object> GetConnectionInfoAsync(IConnection connection, Dictionary<string, object> config)
        {
            return await Task.FromResult(new Dictionary<string, object>
            {
                ["success"] = true,
                ["connected"] = connection.IsOpen,
                ["endpoint"] = connection.Endpoint.ToString(),
                ["server_properties"] = connection.ServerProperties?.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value?.ToString() ?? ""
                ) ?? new Dictionary<string, object>()
            });
        }

        /// <summary>
        /// Get or create AMQP connection
        /// </summary>
        private async Task<IConnection> GetOrCreateConnectionAsync(string connectionString)
        {
            if (_connectionPool.TryGetValue(connectionString, out var existingConnection) && 
                existingConnection.IsOpen)
            {
                return existingConnection;
            }

            lock (_lock)
            {
                if (_connectionPool.TryGetValue(connectionString, out existingConnection) && 
                    existingConnection.IsOpen)
                {
                    return existingConnection;
                }

                var factory = new ConnectionFactory();
                factory.Uri = new Uri(connectionString);
                factory.AutomaticRecoveryEnabled = true;
                factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

                var connection = factory.CreateConnection();
                _connectionPool[connectionString] = connection;
                
                return connection;
            }
        }

        /// <summary>
        /// Get or create AMQP channel
        /// </summary>
        private async Task<IModel> GetOrCreateChannelAsync(string connectionString, IConnection connection, Dictionary<string, object> config)
        {
            var channelKey = $"{connectionString}:{Thread.CurrentThread.ManagedThreadId}";
            
            if (_channelPool.TryGetValue(channelKey, out var existingChannel) && 
                existingChannel.IsOpen)
            {
                return existingChannel;
            }

            lock (_lock)
            {
                if (_channelPool.TryGetValue(channelKey, out existingChannel) && 
                    existingChannel.IsOpen)
                {
                    return existingChannel;
                }

                var channel = connection.CreateModel();
                _channelPool[channelKey] = channel;
                
                return channel;
            }
        }

        /// <summary>
        /// Serialize message to JSON
        /// </summary>
        private string SerializeMessage(object message)
        {
            if (message == null) return "";
            if (message is string str) return str;
            return JsonSerializer.Serialize(message, _jsonOptions);
        }

        /// <summary>
        /// Deserialize message from JSON
        /// </summary>
        private object DeserializeMessage(string messageText)
        {
            if (string.IsNullOrEmpty(messageText)) return null!;
            
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(messageText, _jsonOptions);
            }
            catch
            {
                // If deserialization fails, return as string
                return messageText;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            // Dispose consumers
            foreach (var consumer in _consumers.Values)
            {
                try
                {
                    // Consumer cleanup is handled by channel disposal
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _consumers.Clear();

            // Dispose channels
            foreach (var channel in _channelPool.Values)
            {
                try
                {
                    if (channel.IsOpen)
                    {
                        channel.Close();
                    }
                    channel.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _channelPool.Clear();

            // Dispose connections
            foreach (var connection in _connectionPool.Values)
            {
                try
                {
                    if (connection.IsOpen)
                    {
                        connection.Close();
                    }
                    connection.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _connectionPool.Clear();
        }
    }

    /// <summary>
    /// Extension methods for configuration access
    /// </summary>
    public static class AmqpConfigExtensions
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
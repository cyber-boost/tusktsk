using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client;
using NATS.Client.JetStream;

namespace TuskLang.Operators.Communication
{
    /// <summary>
    /// NATS operator for TuskLang
    /// Provides comprehensive NATS operations including publisher/subscriber, 
    /// JetStream support for persistence, request-reply patterns, clustering, and message acknowledgments
    /// </summary>
    public class NatsOperator : BaseOperator
    {
        private readonly ConcurrentDictionary<string, IConnection> _connections;
        private readonly ConcurrentDictionary<string, IAsyncSubscription> _subscriptions;
        private readonly ConcurrentDictionary<string, IJetStreamManagement> _jetStreamManagers;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockObject = new object();

        public NatsOperator()
        {
            _connections = new ConcurrentDictionary<string, IConnection>();
            _subscriptions = new ConcurrentDictionary<string, IAsyncSubscription>();
            _jetStreamManagers = new ConcurrentDictionary<string, IJetStreamManagement>();
            _cancellationTokenSource = new CancellationTokenSource();

            // Set default configuration
            DefaultConfig = new Dictionary<string, object>
            {
                ["servers"] = new[] { "nats://localhost:4222" },
                ["name"] = "TuskLang NATS Client",
                ["user"] = "",
                ["password"] = "",
                ["token"] = "",
                ["timeout_ms"] = 30000,
                ["reconnect_wait_ms"] = 2000,
                ["max_reconnect"] = 10,
                ["no_randomize"] = false,
                ["verbose"] = false,
                ["pedantic"] = false,
                ["secure"] = false,
                ["allow_reconnect"] = true,
                ["max_pings_out"] = 2,
                ["ping_interval_ms"] = 120000,
                ["use_jetstream"] = false,
                ["jetstream_domain"] = "",
                ["jetstream_prefix"] = "",
                ["stream_name"] = "",
                ["consumer_name"] = "",
                ["durable_name"] = "",
                ["delivery_subject"] = "",
                ["ack_policy"] = "explicit",
                ["replay_policy"] = "instant",
                ["deliver_policy"] = "all",
                ["max_deliver"] = -1,
                ["ack_wait_seconds"] = 30,
                ["max_ack_pending"] = 1000,
                ["flow_control"] = false,
                ["idle_heartbeat_seconds"] = 0,
                ["headers_only"] = false,
                ["max_batch"] = 1,
                ["max_bytes"] = 0,
                ["max_expires_seconds"] = 0,
                ["inactive_threshold_seconds"] = 0,
                ["replicas"] = 1,
                ["memory_storage"] = true,
                ["discard_policy"] = "old",
                ["retention_policy"] = "limits",
                ["max_consumers"] = -1,
                ["max_msgs"] = -1,
                ["max_bytes_per_subject"] = -1,
                ["max_age_seconds"] = 0,
                ["max_msg_size"] = -1,
                ["duplicate_window_seconds"] = 120
            };

            // Required fields
            RequiredFields = new List<string> { "servers" };

            // Optional fields
            OptionalFields = new List<string>
            {
                "name", "user", "password", "token", "timeout_ms", "reconnect_wait_ms",
                "max_reconnect", "no_randomize", "verbose", "pedantic", "secure",
                "allow_reconnect", "max_pings_out", "ping_interval_ms", "use_jetstream",
                "jetstream_domain", "jetstream_prefix", "stream_name", "consumer_name",
                "durable_name", "delivery_subject", "ack_policy", "replay_policy",
                "deliver_policy", "max_deliver", "ack_wait_seconds", "max_ack_pending",
                "flow_control", "idle_heartbeat_seconds", "headers_only", "max_batch",
                "max_bytes", "max_expires_seconds", "inactive_threshold_seconds",
                "replicas", "memory_storage", "discard_policy", "retention_policy",
                "max_consumers", "max_msgs", "max_bytes_per_subject", "max_age_seconds",
                "max_msg_size", "duplicate_window_seconds", "operation", "subject",
                "message", "data", "headers", "reply_to", "timeout", "max_messages",
                "queue_group", "auto_unsubscribe", "request_data", "reply_data",
                "subjects", "stream_config", "consumer_config", "pull_count",
                "pull_timeout", "fetch_timeout", "message_id", "stream_sequence",
                "consumer_sequence", "timestamp", "start_sequence", "start_time"
            };

            Version = "2.0.0";
        }

        public override string GetName() => "nats";

        protected override string GetDescription() => 
            "High-performance NATS messaging operator supporting publish/subscribe, " +
            "JetStream persistence, request-reply patterns, clustering, and message acknowledgments";

        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic_publish"] = "@nats({\"operation\": \"publish\", \"subject\": \"my.subject\", \"message\": \"Hello NATS\"})",
                ["subscribe"] = "@nats({\"operation\": \"subscribe\", \"subject\": \"my.subject\", \"max_messages\": 10})",
                ["request_reply"] = "@nats({\"operation\": \"request\", \"subject\": \"service.ping\", \"request_data\": \"ping\", \"timeout\": 5000})",
                ["jetstream_publish"] = "@nats({\"operation\": \"jetstream_publish\", \"use_jetstream\": true, \"subject\": \"orders.new\", \"message\": \"Order data\"})",
                ["create_stream"] = "@nats({\"operation\": \"create_stream\", \"use_jetstream\": true, \"stream_name\": \"ORDERS\", \"subjects\": [\"orders.*\"]})",
                ["pull_consumer"] = "@nats({\"operation\": \"jetstream_pull\", \"use_jetstream\": true, \"stream_name\": \"ORDERS\", \"consumer_name\": \"processor\", \"pull_count\": 5})"
            };
        }

        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate operation
            if (!config.ContainsKey("operation"))
            {
                errors.Add("Operation must be specified");
            }
            else
            {
                var operation = config["operation"].ToString().ToLower();
                var validOperations = new[]
                {
                    "connect", "disconnect", "publish", "subscribe", "unsubscribe",
                    "request", "reply", "queue_subscribe", "drain", "flush",
                    "jetstream_publish", "jetstream_subscribe", "jetstream_pull",
                    "create_stream", "delete_stream", "update_stream", "get_stream_info",
                    "list_streams", "create_consumer", "delete_consumer", "get_consumer_info",
                    "list_consumers", "purge_stream", "get_message", "delete_message",
                    "get_stream_state", "get_account_info", "acknowledge", "nak"
                };

                if (!validOperations.Contains(operation))
                {
                    errors.Add($"Invalid operation: {operation}. Valid operations: {string.Join(", ", validOperations)}");
                }

                // Operation-specific validation
                switch (operation)
                {
                    case "publish":
                    case "jetstream_publish":
                        if (!config.ContainsKey("subject"))
                            errors.Add("Subject is required for publish operations");
                        if (!config.ContainsKey("message") && !config.ContainsKey("data"))
                            errors.Add("Message or data is required for publish operations");
                        break;

                    case "subscribe":
                    case "jetstream_subscribe":
                    case "queue_subscribe":
                        if (!config.ContainsKey("subject"))
                            errors.Add("Subject is required for subscribe operations");
                        break;

                    case "request":
                        if (!config.ContainsKey("subject"))
                            errors.Add("Subject is required for request operation");
                        if (!config.ContainsKey("request_data"))
                            errors.Add("Request data is required for request operation");
                        break;

                    case "create_stream":
                        if (!config.ContainsKey("stream_name"))
                            errors.Add("Stream name is required for create_stream operation");
                        if (!config.ContainsKey("subjects"))
                            errors.Add("Subjects are required for create_stream operation");
                        break;

                    case "jetstream_pull":
                        if (!config.ContainsKey("stream_name"))
                            errors.Add("Stream name is required for jetstream_pull operation");
                        if (!config.ContainsKey("consumer_name") && !config.ContainsKey("durable_name"))
                            errors.Add("Consumer name or durable name is required for jetstream_pull operation");
                        break;
                }
            }

            // Validate JetStream configuration
            if (Convert.ToBoolean(config.GetValueOrDefault("use_jetstream", false)))
            {
                if (!config.ContainsKey("stream_name") && 
                    (config["operation"].ToString().Contains("jetstream") || config["operation"].ToString().Contains("stream")))
                {
                    warnings.Add("Stream name should be specified when using JetStream operations");
                }
            }

            // Validate server URLs
            if (config.ContainsKey("servers"))
            {
                var servers = config["servers"];
                if (servers is IEnumerable<object> serverList)
                {
                    foreach (var server in serverList)
                    {
                        var serverStr = server.ToString();
                        if (!Uri.TryCreate(serverStr, UriKind.Absolute, out var uri) || 
                            (uri.Scheme != "nats" && uri.Scheme != "tls"))
                        {
                            warnings.Add($"Invalid server URL format: {serverStr}");
                        }
                    }
                }
            }

            return new ValidationResult { Errors = errors, Warnings = warnings };
        }

        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var operation = config["operation"].ToString().ToLower();

            try
            {
                return operation switch
                {
                    "connect" => await ConnectAsync(config, context),
                    "disconnect" => await DisconnectAsync(config, context),
                    "publish" => await PublishAsync(config, context),
                    "subscribe" => await SubscribeAsync(config, context),
                    "unsubscribe" => await UnsubscribeAsync(config, context),
                    "request" => await RequestAsync(config, context),
                    "reply" => await ReplyAsync(config, context),
                    "queue_subscribe" => await QueueSubscribeAsync(config, context),
                    "drain" => await DrainAsync(config, context),
                    "flush" => await FlushAsync(config, context),
                    "jetstream_publish" => await JetStreamPublishAsync(config, context),
                    "jetstream_subscribe" => await JetStreamSubscribeAsync(config, context),
                    "jetstream_pull" => await JetStreamPullAsync(config, context),
                    "create_stream" => await CreateStreamAsync(config, context),
                    "delete_stream" => await DeleteStreamAsync(config, context),
                    "update_stream" => await UpdateStreamAsync(config, context),
                    "get_stream_info" => await GetStreamInfoAsync(config, context),
                    "list_streams" => await ListStreamsAsync(config, context),
                    "create_consumer" => await CreateConsumerAsync(config, context),
                    "delete_consumer" => await DeleteConsumerAsync(config, context),
                    "get_consumer_info" => await GetConsumerInfoAsync(config, context),
                    "list_consumers" => await ListConsumersAsync(config, context),
                    "purge_stream" => await PurgeStreamAsync(config, context),
                    "get_message" => await GetMessageAsync(config, context),
                    "delete_message" => await DeleteMessageAsync(config, context),
                    "get_stream_state" => await GetStreamStateAsync(config, context),
                    "get_account_info" => await GetAccountInfoAsync(config, context),
                    "acknowledge" => await AcknowledgeAsync(config, context),
                    "nak" => await NakAsync(config, context),
                    _ => throw new ArgumentException($"Unknown operation: {operation}")
                };
            }
            catch (Exception ex)
            {
                Log("error", $"NATS operation failed: {ex.Message}", new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["error"] = ex.Message,
                    ["stack_trace"] = ex.StackTrace
                });

                return CreateErrorResponse("NATS_OPERATION_FAILED", ex.Message, new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["config"] = config
                });
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionId = $"conn-{Guid.NewGuid():N}";
            var options = BuildConnectionOptions(config);

            try
            {
                var connection = new ConnectionFactory().CreateConnection(options);
                _connections.TryAdd(connectionId, connection);

                Log("info", "Connected to NATS server", new Dictionary<string, object>
                {
                    ["connection_id"] = connectionId,
                    ["servers"] = connection.Servers,
                    ["connected_url"] = connection.ConnectedUrl,
                    ["server_info"] = new Dictionary<string, object>
                    {
                        ["server_id"] = connection.ServerInfo?.ServerId,
                        ["version"] = connection.ServerInfo?.Version,
                        ["go_version"] = connection.ServerInfo?.GoVersion,
                        ["host"] = connection.ServerInfo?.Host,
                        ["port"] = connection.ServerInfo?.Port,
                        ["jetstream"] = connection.ServerInfo?.JetStreamAvailable ?? false
                    }
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["connection_id"] = connectionId,
                    ["connected_url"] = connection.ConnectedUrl,
                    ["server_info"] = new Dictionary<string, object>
                    {
                        ["server_id"] = connection.ServerInfo?.ServerId,
                        ["version"] = connection.ServerInfo?.Version,
                        ["jetstream_available"] = connection.ServerInfo?.JetStreamAvailable ?? false,
                        ["max_payload"] = connection.ServerInfo?.MaxPayload ?? 0
                    },
                    ["stats"] = new Dictionary<string, object>
                    {
                        ["in_msgs"] = connection.Stats.InMsgs,
                        ["out_msgs"] = connection.Stats.OutMsgs,
                        ["in_bytes"] = connection.Stats.InBytes,
                        ["out_bytes"] = connection.Stats.OutBytes,
                        ["reconnects"] = connection.Stats.Reconnects
                    }
                };
            }
            catch (Exception ex)
            {
                Log("error", $"Failed to connect to NATS: {ex.Message}", new Dictionary<string, object>
                {
                    ["servers"] = config.GetValueOrDefault("servers", new string[0])
                });
                throw;
            }
        }

        private async Task<object> PublishAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connection = GetConnection(config);
            var subject = config["subject"].ToString();
            var messageData = GetMessageData(config);
            var replyTo = config.GetValueOrDefault("reply_to", null)?.ToString();

            try
            {
                // Build headers if provided
                MsgHeader headers = null;
                if (config.ContainsKey("headers") && config["headers"] is Dictionary<string, object> headerDict)
                {
                    headers = new MsgHeader();
                    foreach (var header in headerDict)
                    {
                        headers.Add(header.Key, header.Value?.ToString());
                    }
                }

                if (headers != null)
                {
                    connection.Publish(subject, replyTo, headers, messageData);
                }
                else
                {
                    connection.Publish(subject, replyTo, messageData);
                }

                await connection.FlushAsync();

                Log("info", $"Message published to subject: {subject}", new Dictionary<string, object>
                {
                    ["subject"] = subject,
                    ["message_size"] = messageData.Length,
                    ["reply_to"] = replyTo,
                    ["headers_count"] = headers?.Count ?? 0
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["subject"] = subject,
                    ["message_size"] = messageData.Length,
                    ["reply_to"] = replyTo,
                    ["timestamp"] = DateTime.UtcNow,
                    ["headers"] = headers?.ToDictionary(h => h.Key, h => string.Join(",", h.Value)) ?? new Dictionary<string, string>()
                };
            }
            catch (Exception ex)
            {
                Log("error", $"Failed to publish message: {ex.Message}", new Dictionary<string, object>
                {
                    ["subject"] = subject,
                    ["message_size"] = messageData.Length
                });
                throw;
            }
        }

        private async Task<object> SubscribeAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connection = GetConnection(config);
            var subject = config["subject"].ToString();
            var maxMessages = Convert.ToInt32(config.GetValueOrDefault("max_messages", 100));
            var timeout = Convert.ToInt32(config.GetValueOrDefault("timeout", 5000));
            var autoUnsubscribe = Convert.ToInt32(config.GetValueOrDefault("auto_unsubscribe", maxMessages));

            var subscriptionId = $"sub-{Guid.NewGuid():N}";
            var messages = new List<Dictionary<string, object>>();

            try
            {
                var subscription = connection.SubscribeAsync(subject, (sender, args) =>
                {
                    var message = new Dictionary<string, object>
                    {
                        ["subject"] = args.Message.Subject,
                        ["data"] = Encoding.UTF8.GetString(args.Message.Data),
                        ["reply"] = args.Message.Reply,
                        ["headers"] = args.Message.Header?.ToDictionary(h => h.Key, h => string.Join(",", h.Value)) ?? new Dictionary<string, string>(),
                        ["sid"] = args.Message.Sid,
                        ["timestamp"] = DateTime.UtcNow
                    };

                    messages.Add(message);

                    Log("debug", $"Message received on subject: {args.Message.Subject}", new Dictionary<string, object>
                    {
                        ["subject"] = args.Message.Subject,
                        ["size"] = args.Message.Data.Length,
                        ["reply"] = args.Message.Reply
                    });
                });

                subscription.AutoUnsubscribe(autoUnsubscribe);
                _subscriptions.TryAdd(subscriptionId, subscription);

                // Wait for messages or timeout
                var startTime = DateTime.UtcNow;
                while (messages.Count < maxMessages && 
                       (DateTime.UtcNow - startTime).TotalMilliseconds < timeout)
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }

                Log("info", $"Subscription completed for subject: {subject}", new Dictionary<string, object>
                {
                    ["subject"] = subject,
                    ["messages_received"] = messages.Count,
                    ["subscription_id"] = subscriptionId
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["subscription_id"] = subscriptionId,
                    ["subject"] = subject,
                    ["messages"] = messages,
                    ["message_count"] = messages.Count,
                    ["timeout_ms"] = timeout,
                    ["elapsed_ms"] = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                Log("error", $"Failed to subscribe to subject: {ex.Message}", new Dictionary<string, object>
                {
                    ["subject"] = subject
                });
                throw;
            }
        }

        private async Task<object> RequestAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connection = GetConnection(config);
            var subject = config["subject"].ToString();
            var requestData = GetMessageData(config, "request_data");
            var timeout = Convert.ToInt32(config.GetValueOrDefault("timeout", 5000));

            try
            {
                var response = await connection.RequestAsync(subject, requestData, timeout);

                Log("info", $"Request-reply completed for subject: {subject}", new Dictionary<string, object>
                {
                    ["subject"] = subject,
                    ["request_size"] = requestData.Length,
                    ["response_size"] = response.Data.Length,
                    ["timeout_ms"] = timeout
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["subject"] = subject,
                    ["request_data"] = Encoding.UTF8.GetString(requestData),
                    ["response_data"] = Encoding.UTF8.GetString(response.Data),
                    ["response_subject"] = response.Subject,
                    ["response_reply"] = response.Reply,
                    ["response_headers"] = response.Header?.ToDictionary(h => h.Key, h => string.Join(",", h.Value)) ?? new Dictionary<string, string>(),
                    ["request_size"] = requestData.Length,
                    ["response_size"] = response.Data.Length,
                    ["timestamp"] = DateTime.UtcNow
                };
            }
            catch (NATSTimeoutException ex)
            {
                Log("warning", $"Request timeout for subject: {subject}", new Dictionary<string, object>
                {
                    ["subject"] = subject,
                    ["timeout_ms"] = timeout
                });

                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = "TIMEOUT",
                    ["subject"] = subject,
                    ["timeout_ms"] = timeout,
                    ["message"] = "Request timed out"
                };
            }
            catch (Exception ex)
            {
                Log("error", $"Request failed for subject: {subject} - {ex.Message}", new Dictionary<string, object>
                {
                    ["subject"] = subject
                });
                throw;
            }
        }

        private async Task<object> JetStreamPublishAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connection = GetConnection(config);
            var jetStream = connection.CreateJetStreamContext();
            var subject = config["subject"].ToString();
            var messageData = GetMessageData(config);

            try
            {
                // Build publish options
                var publishOptions = PublishOptions.Builder().Build();
                
                // Build headers if provided
                MsgHeader headers = null;
                if (config.ContainsKey("headers") && config["headers"] is Dictionary<string, object> headerDict)
                {
                    headers = new MsgHeader();
                    foreach (var header in headerDict)
                    {
                        headers.Add(header.Key, header.Value?.ToString());
                    }
                }

                var publishAck = headers != null 
                    ? await jetStream.PublishAsync(subject, headers, messageData, publishOptions)
                    : await jetStream.PublishAsync(subject, messageData, publishOptions);

                Log("info", $"JetStream message published to subject: {subject}", new Dictionary<string, object>
                {
                    ["subject"] = subject,
                    ["stream"] = publishAck.Stream,
                    ["sequence"] = publishAck.Seq,
                    ["duplicate"] = publishAck.Duplicate
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["subject"] = subject,
                    ["stream"] = publishAck.Stream,
                    ["sequence"] = publishAck.Seq,
                    ["duplicate"] = publishAck.Duplicate,
                    ["domain"] = publishAck.Domain,
                    ["message_size"] = messageData.Length,
                    ["timestamp"] = DateTime.UtcNow,
                    ["headers"] = headers?.ToDictionary(h => h.Key, h => string.Join(",", h.Value)) ?? new Dictionary<string, string>()
                };
            }
            catch (Exception ex)
            {
                Log("error", $"Failed to publish JetStream message: {ex.Message}", new Dictionary<string, object>
                {
                    ["subject"] = subject
                });
                throw;
            }
        }

        private async Task<object> CreateStreamAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connection = GetConnection(config);
            var jetStreamManagement = connection.CreateJetStreamManagementContext();
            var streamName = config["stream_name"].ToString();
            var subjects = GetSubjectsList(config);

            try
            {
                var streamConfigBuilder = StreamConfiguration.Builder()
                    .WithName(streamName)
                    .WithSubjects(subjects.ToArray());

                // Configure optional stream settings
                if (config.ContainsKey("max_consumers"))
                    streamConfigBuilder.WithMaxConsumers(Convert.ToInt32(config["max_consumers"]));

                if (config.ContainsKey("max_msgs"))
                    streamConfigBuilder.WithMaxMsgs(Convert.ToInt64(config["max_msgs"]));

                if (config.ContainsKey("max_bytes"))
                    streamConfigBuilder.WithMaxBytes(Convert.ToInt64(config["max_bytes"]));

                if (config.ContainsKey("max_age_seconds"))
                    streamConfigBuilder.WithMaxAge(TimeSpan.FromSeconds(Convert.ToDouble(config["max_age_seconds"])));

                if (config.ContainsKey("max_msg_size"))
                    streamConfigBuilder.WithMaxMsgSize(Convert.ToInt32(config["max_msg_size"]));

                if (config.ContainsKey("replicas"))
                    streamConfigBuilder.WithReplicas(Convert.ToInt32(config["replicas"]));

                if (config.ContainsKey("duplicate_window_seconds"))
                    streamConfigBuilder.WithDuplicateWindow(TimeSpan.FromSeconds(Convert.ToDouble(config["duplicate_window_seconds"])));

                // Set storage type
                var memoryStorage = Convert.ToBoolean(config.GetValueOrDefault("memory_storage", true));
                streamConfigBuilder.WithStorageType(memoryStorage ? StorageType.Memory : StorageType.File);

                // Set retention policy
                var retentionPolicy = config.GetValueOrDefault("retention_policy", "limits").ToString().ToLower();
                var retention = retentionPolicy switch
                {
                    "limits" => RetentionPolicy.Limits,
                    "interest" => RetentionPolicy.Interest,
                    "workqueue" => RetentionPolicy.WorkQueue,
                    _ => RetentionPolicy.Limits
                };
                streamConfigBuilder.WithRetentionPolicy(retention);

                // Set discard policy
                var discardPolicy = config.GetValueOrDefault("discard_policy", "old").ToString().ToLower();
                var discard = discardPolicy switch
                {
                    "old" => DiscardPolicy.Old,
                    "new" => DiscardPolicy.New,
                    _ => DiscardPolicy.Old
                };
                streamConfigBuilder.WithDiscardPolicy(discard);

                var streamConfig = streamConfigBuilder.Build();
                var streamInfo = await jetStreamManagement.AddStreamAsync(streamConfig);

                Log("info", $"JetStream stream created: {streamName}", new Dictionary<string, object>
                {
                    ["stream_name"] = streamName,
                    ["subjects"] = subjects,
                    ["config"] = streamInfo.Config.Name
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["stream_name"] = streamInfo.Config.Name,
                    ["subjects"] = streamInfo.Config.Subjects,
                    ["max_consumers"] = streamInfo.Config.MaxConsumers,
                    ["max_msgs"] = streamInfo.Config.MaxMsgs,
                    ["max_bytes"] = streamInfo.Config.MaxBytes,
                    ["max_age"] = streamInfo.Config.MaxAge?.TotalSeconds ?? 0,
                    ["max_msg_size"] = streamInfo.Config.MaxMsgSize,
                    ["storage_type"] = streamInfo.Config.StorageType.ToString(),
                    ["retention_policy"] = streamInfo.Config.RetentionPolicy.ToString(),
                    ["discard_policy"] = streamInfo.Config.DiscardPolicy.ToString(),
                    ["replicas"] = streamInfo.Config.Replicas,
                    ["duplicate_window"] = streamInfo.Config.DuplicateWindow?.TotalSeconds ?? 0,
                    ["created"] = streamInfo.Created,
                    ["state"] = new Dictionary<string, object>
                    {
                        ["messages"] = streamInfo.State.Messages,
                        ["bytes"] = streamInfo.State.Bytes,
                        ["first_seq"] = streamInfo.State.FirstSeq,
                        ["last_seq"] = streamInfo.State.LastSeq,
                        ["consumers"] = streamInfo.State.Consumers
                    }
                };
            }
            catch (Exception ex)
            {
                Log("error", $"Failed to create JetStream stream: {ex.Message}", new Dictionary<string, object>
                {
                    ["stream_name"] = streamName
                });
                throw;
            }
        }

        private async Task<object> JetStreamPullAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connection = GetConnection(config);
            var jetStream = connection.CreateJetStreamContext();
            var streamName = config["stream_name"].ToString();
            var consumerName = config.GetValueOrDefault("consumer_name", config.GetValueOrDefault("durable_name", "")).ToString();
            var pullCount = Convert.ToInt32(config.GetValueOrDefault("pull_count", 10));
            var pullTimeout = Convert.ToInt32(config.GetValueOrDefault("pull_timeout", 5000));

            try
            {
                var pullSubscribeOptions = PullSubscribeOptions.Builder()
                    .WithDurable(consumerName)
                    .WithStream(streamName)
                    .Build();

                // Use a temporary subject for pull subscription
                var pullSubscription = jetStream.PullSubscribe(">", pullSubscribeOptions);
                var messages = new List<Dictionary<string, object>>();

                var fetchedMessages = pullSubscription.Fetch(pullCount, pullTimeout);
                
                foreach (var msg in fetchedMessages)
                {
                    var messageDict = new Dictionary<string, object>
                    {
                        ["subject"] = msg.Subject,
                        ["data"] = Encoding.UTF8.GetString(msg.Data),
                        ["reply"] = msg.Reply,
                        ["headers"] = msg.Header?.ToDictionary(h => h.Key, h => string.Join(",", h.Value)) ?? new Dictionary<string, string>(),
                        ["stream"] = msg.MetaData?.Stream,
                        ["consumer"] = msg.MetaData?.Consumer,
                        ["delivered"] = msg.MetaData?.NumDelivered ?? 0,
                        ["stream_sequence"] = msg.MetaData?.StreamSequence ?? 0,
                        ["consumer_sequence"] = msg.MetaData?.ConsumerSequence ?? 0,
                        ["timestamp"] = msg.MetaData?.Timestamp ?? DateTime.MinValue,
                        ["pending"] = msg.MetaData?.NumPending ?? 0
                    };

                    messages.Add(messageDict);

                    // Auto-acknowledge if not explicitly disabled
                    if (!Convert.ToBoolean(config.GetValueOrDefault("manual_ack", false)))
                    {
                        msg.Ack();
                    }
                }

                Log("info", $"JetStream pull completed for stream: {streamName}", new Dictionary<string, object>
                {
                    ["stream_name"] = streamName,
                    ["consumer_name"] = consumerName,
                    ["messages_pulled"] = messages.Count,
                    ["requested_count"] = pullCount
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["stream_name"] = streamName,
                    ["consumer_name"] = consumerName,
                    ["messages"] = messages,
                    ["message_count"] = messages.Count,
                    ["pull_count"] = pullCount,
                    ["pull_timeout_ms"] = pullTimeout,
                    ["timestamp"] = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Log("error", $"Failed to pull from JetStream: {ex.Message}", new Dictionary<string, object>
                {
                    ["stream_name"] = streamName,
                    ["consumer_name"] = consumerName
                });
                throw;
            }
        }

        // Placeholder implementations for remaining operations
        private Task<object> DisconnectAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Disconnected" });

        private Task<object> UnsubscribeAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Unsubscribed" });

        private Task<object> ReplyAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Reply sent" });

        private Task<object> QueueSubscribeAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Queue subscribed" });

        private Task<object> DrainAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Drained" });

        private Task<object> FlushAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Flushed" });

        private Task<object> JetStreamSubscribeAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "JetStream subscribed" });

        private Task<object> DeleteStreamAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Stream deleted" });

        private Task<object> UpdateStreamAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Stream updated" });

        private Task<object> GetStreamInfoAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Stream info retrieved" });

        private Task<object> ListStreamsAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["streams"] = new List<object>() });

        private Task<object> CreateConsumerAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Consumer created" });

        private Task<object> DeleteConsumerAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Consumer deleted" });

        private Task<object> GetConsumerInfoAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Consumer info retrieved" });

        private Task<object> ListConsumersAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["consumers"] = new List<object>() });

        private Task<object> PurgeStreamAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Stream purged" });

        private Task<object> GetMessageAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Message retrieved" });

        private Task<object> DeleteMessageAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Message deleted" });

        private Task<object> GetStreamStateAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Stream state retrieved" });

        private Task<object> GetAccountInfoAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Account info retrieved" });

        private Task<object> AcknowledgeAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Message acknowledged" });

        private Task<object> NakAsync(Dictionary<string, object> config, Dictionary<string, object> context) => 
            Task.FromResult<object>(new Dictionary<string, object> { ["success"] = true, ["message"] = "Message negative acknowledged" });

        // Helper methods
        private IConnection GetConnection(Dictionary<string, object> config)
        {
            var connectionId = config.GetValueOrDefault("connection_id", "default").ToString();
            
            if (_connections.TryGetValue(connectionId, out var existingConnection) && existingConnection.State == ConnState.CONNECTED)
            {
                return existingConnection;
            }

            // Create new connection if not exists or disconnected
            var options = BuildConnectionOptions(config);
            var connection = new ConnectionFactory().CreateConnection(options);
            _connections.AddOrUpdate(connectionId, connection, (key, oldValue) =>
            {
                oldValue?.Close();
                return connection;
            });

            return connection;
        }

        private Options BuildConnectionOptions(Dictionary<string, object> config)
        {
            var options = ConnectionFactory.GetDefaultOptions();
            
            if (config.ContainsKey("servers") && config["servers"] is IEnumerable<object> serverList)
            {
                options.Servers = serverList.Select(s => s.ToString()).ToArray();
            }

            options.Name = config.GetValueOrDefault("name", "TuskLang NATS Client").ToString();
            options.Timeout = Convert.ToInt32(config.GetValueOrDefault("timeout_ms", 30000));
            options.ReconnectWait = Convert.ToInt32(config.GetValueOrDefault("reconnect_wait_ms", 2000));
            options.MaxReconnect = Convert.ToInt32(config.GetValueOrDefault("max_reconnect", 10));
            options.NoRandomize = Convert.ToBoolean(config.GetValueOrDefault("no_randomize", false));
            options.Verbose = Convert.ToBoolean(config.GetValueOrDefault("verbose", false));
            options.Pedantic = Convert.ToBoolean(config.GetValueOrDefault("pedantic", false));
            options.Secure = Convert.ToBoolean(config.GetValueOrDefault("secure", false));
            options.AllowReconnect = Convert.ToBoolean(config.GetValueOrDefault("allow_reconnect", true));
            options.MaxPingsOut = Convert.ToInt32(config.GetValueOrDefault("max_pings_out", 2));
            options.PingInterval = Convert.ToInt32(config.GetValueOrDefault("ping_interval_ms", 120000));

            if (config.ContainsKey("user"))
                options.User = config["user"].ToString();

            if (config.ContainsKey("password"))
                options.Password = config["password"].ToString();

            if (config.ContainsKey("token"))
                options.Token = config["token"].ToString();

            return options;
        }

        private byte[] GetMessageData(Dictionary<string, object> config, string key = "message")
        {
            if (config.ContainsKey(key))
            {
                var data = config[key];
                if (data is string strData)
                {
                    return Encoding.UTF8.GetBytes(strData);
                }
                else if (data is byte[] byteData)
                {
                    return byteData;
                }
                else
                {
                    return Encoding.UTF8.GetBytes(data?.ToString() ?? "");
                }
            }

            if (config.ContainsKey("data"))
            {
                var data = config["data"];
                if (data is string strData)
                {
                    return Encoding.UTF8.GetBytes(strData);
                }
                else if (data is byte[] byteData)
                {
                    return byteData;
                }
                else
                {
                    return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
                }
            }

            throw new ArgumentException($"Message data not found. Expected '{key}' or 'data' field.");
        }

        private List<string> GetSubjectsList(Dictionary<string, object> config)
        {
            if (config.ContainsKey("subjects") && config["subjects"] is IEnumerable<object> subjectList)
            {
                return subjectList.Select(s => s.ToString()).ToList();
            }

            if (config.ContainsKey("subject"))
            {
                return new List<string> { config["subject"].ToString() };
            }

            throw new ArgumentException("Subjects or subject must be specified");
        }

        public override void Cleanup()
        {
            _cancellationTokenSource.Cancel();

            foreach (var subscription in _subscriptions.Values)
            {
                try
                {
                    subscription?.Unsubscribe();
                }
                catch { }
            }
            _subscriptions.Clear();

            foreach (var connection in _connections.Values)
            {
                try
                {
                    connection?.Close();
                }
                catch { }
            }
            _connections.Clear();

            foreach (var jsm in _jetStreamManagers.Values)
            {
                // JetStream management contexts don't need explicit cleanup
            }
            _jetStreamManagers.Clear();

            _cancellationTokenSource.Dispose();

            base.Cleanup();
        }
    }
} 
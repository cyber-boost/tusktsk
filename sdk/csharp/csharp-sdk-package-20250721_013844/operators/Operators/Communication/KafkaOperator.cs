using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace TuskLang.Operators.Communication
{
    /// <summary>
    /// Kafka operator for TuskLang
    /// Provides comprehensive Apache Kafka operations including producer, consumer, admin operations, 
    /// schema registry integration, transactions, and consumer groups with offset management
    /// </summary>
    public class KafkaOperator : BaseOperator
    {
        private readonly ConcurrentDictionary<string, IProducer<string, string>> _producers;
        private readonly ConcurrentDictionary<string, IConsumer<string, string>> _consumers;
        private readonly ConcurrentDictionary<string, IAdminClient> _adminClients;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockObject = new object();

        public KafkaOperator()
        {
            _producers = new ConcurrentDictionary<string, IProducer<string, string>>();
            _consumers = new ConcurrentDictionary<string, IConsumer<string, string>>();
            _adminClients = new ConcurrentDictionary<string, IAdminClient>();
            _cancellationTokenSource = new CancellationTokenSource();

            // Set default configuration
            DefaultConfig = new Dictionary<string, object>
            {
                ["bootstrap_servers"] = "localhost:9092",
                ["client_id"] = "tusklang-kafka-client",
                ["group_id"] = "tusklang-consumer-group",
                ["auto_offset_reset"] = "earliest",
                ["enable_auto_commit"] = true,
                ["session_timeout_ms"] = 30000,
                ["heartbeat_interval_ms"] = 3000,
                ["max_poll_interval_ms"] = 300000,
                ["enable_idempotence"] = true,
                ["retries"] = int.MaxValue,
                ["delivery_timeout_ms"] = 300000,
                ["request_timeout_ms"] = 30000,
                ["acks"] = "all",
                ["compression_type"] = "snappy",
                ["batch_size"] = 16384,
                ["linger_ms"] = 5,
                ["buffer_memory"] = 33554432,
                ["max_request_size"] = 1048576,
                ["security_protocol"] = "PLAINTEXT",
                ["sasl_mechanism"] = "PLAIN",
                ["ssl_ca_location"] = "",
                ["ssl_certificate_location"] = "",
                ["ssl_key_location"] = "",
                ["schema_registry_url"] = "",
                ["transaction_timeout_ms"] = 60000,
                ["enable_transactions"] = false,
                ["consumer_timeout_ms"] = -1,
                ["fetch_min_bytes"] = 1,
                ["fetch_max_wait_ms"] = 500,
                ["max_partition_fetch_bytes"] = 1048576,
                ["isolation_level"] = "read_uncommitted"
            };

            // Required fields
            RequiredFields = new List<string> { "bootstrap_servers" };

            // Optional fields
            OptionalFields = new List<string>
            {
                "client_id", "group_id", "auto_offset_reset", "enable_auto_commit",
                "session_timeout_ms", "heartbeat_interval_ms", "max_poll_interval_ms",
                "enable_idempotence", "retries", "delivery_timeout_ms", "request_timeout_ms",
                "acks", "compression_type", "batch_size", "linger_ms", "buffer_memory",
                "max_request_size", "security_protocol", "sasl_mechanism", "sasl_username",
                "sasl_password", "ssl_ca_location", "ssl_certificate_location", "ssl_key_location",
                "schema_registry_url", "transaction_timeout_ms", "enable_transactions",
                "consumer_timeout_ms", "fetch_min_bytes", "fetch_max_wait_ms",
                "max_partition_fetch_bytes", "isolation_level", "topic", "partition", "offset",
                "key", "value", "headers", "topics", "message", "messages", "timeout_ms",
                "max_messages", "commit_offsets", "seek_to_beginning", "seek_to_end",
                "transactional_id", "operation", "partitions", "replication_factor",
                "configs", "consumer_group", "metadata_timeout", "admin_timeout"
            };

            Version = "2.0.0";
        }

        public override string GetName() => "kafka";

        protected override string GetDescription() => 
            "High-performance Apache Kafka operator supporting producers, consumers, admin operations, " +
            "schema registry integration, transactions, consumer groups, and comprehensive offset management";

        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic_produce"] = "@kafka({\"operation\": \"produce\", \"topic\": \"my-topic\", \"key\": \"msg1\", \"value\": \"Hello Kafka\"})",
                ["consume_messages"] = "@kafka({\"operation\": \"consume\", \"topics\": [\"my-topic\"], \"max_messages\": 10})",
                ["create_topic"] = "@kafka({\"operation\": \"create_topic\", \"topic\": \"new-topic\", \"partitions\": 3, \"replication_factor\": 1})",
                ["transactional_produce"] = "@kafka({\"operation\": \"transactional_produce\", \"transactional_id\": \"tx1\", \"messages\": [{\"topic\": \"topic1\", \"key\": \"k1\", \"value\": \"v1\"}]})",
                ["consumer_group_management"] = "@kafka({\"operation\": \"list_consumer_groups\"})",
                ["offset_management"] = "@kafka({\"operation\": \"commit_offsets\", \"group_id\": \"my-group\", \"topic\": \"my-topic\", \"partition\": 0, \"offset\": 100})"
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
                    "produce", "consume", "create_topic", "delete_topic", "list_topics",
                    "describe_topic", "create_partitions", "describe_consumer_groups",
                    "list_consumer_groups", "delete_consumer_group", "commit_offsets",
                    "get_offsets", "seek_to_beginning", "seek_to_end", "transactional_produce",
                    "begin_transaction", "commit_transaction", "abort_transaction",
                    "get_metadata", "get_watermark_offsets", "pause_partitions", "resume_partitions"
                };

                if (!validOperations.Contains(operation))
                {
                    errors.Add($"Invalid operation: {operation}. Valid operations: {string.Join(", ", validOperations)}");
                }

                // Operation-specific validation
                switch (operation)
                {
                    case "produce":
                        if (!config.ContainsKey("topic"))
                            errors.Add("Topic is required for produce operation");
                        if (!config.ContainsKey("value"))
                            errors.Add("Value is required for produce operation");
                        break;

                    case "consume":
                        if (!config.ContainsKey("topics") && !config.ContainsKey("topic"))
                            errors.Add("Topics or topic is required for consume operation");
                        break;

                    case "create_topic":
                        if (!config.ContainsKey("topic"))
                            errors.Add("Topic is required for create_topic operation");
                        if (!config.ContainsKey("partitions"))
                            errors.Add("Partitions is required for create_topic operation");
                        break;

                    case "transactional_produce":
                        if (!config.ContainsKey("transactional_id"))
                            errors.Add("Transactional ID is required for transactional_produce operation");
                        if (!config.ContainsKey("messages"))
                            errors.Add("Messages array is required for transactional_produce operation");
                        break;
                }
            }

            // Validate security configuration
            if (config.ContainsKey("security_protocol"))
            {
                var protocol = config["security_protocol"].ToString().ToUpper();
                if (!new[] { "PLAINTEXT", "SSL", "SASL_PLAINTEXT", "SASL_SSL" }.Contains(protocol))
                {
                    errors.Add("Invalid security_protocol. Valid values: PLAINTEXT, SSL, SASL_PLAINTEXT, SASL_SSL");
                }

                if (protocol.Contains("SASL") && !config.ContainsKey("sasl_mechanism"))
                {
                    warnings.Add("SASL mechanism not specified, defaulting to PLAIN");
                }

                if (protocol.Contains("SSL"))
                {
                    if (!config.ContainsKey("ssl_ca_location"))
                        warnings.Add("SSL CA location not specified for SSL protocol");
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
                    "produce" => await ProduceMessageAsync(config, context),
                    "consume" => await ConsumeMessagesAsync(config, context),
                    "create_topic" => await CreateTopicAsync(config, context),
                    "delete_topic" => await DeleteTopicAsync(config, context),
                    "list_topics" => await ListTopicsAsync(config, context),
                    "describe_topic" => await DescribeTopicAsync(config, context),
                    "create_partitions" => await CreatePartitionsAsync(config, context),
                    "describe_consumer_groups" => await DescribeConsumerGroupsAsync(config, context),
                    "list_consumer_groups" => await ListConsumerGroupsAsync(config, context),
                    "delete_consumer_group" => await DeleteConsumerGroupAsync(config, context),
                    "commit_offsets" => await CommitOffsetsAsync(config, context),
                    "get_offsets" => await GetOffsetsAsync(config, context),
                    "seek_to_beginning" => await SeekToBeginningAsync(config, context),
                    "seek_to_end" => await SeekToEndAsync(config, context),
                    "transactional_produce" => await TransactionalProduceAsync(config, context),
                    "begin_transaction" => await BeginTransactionAsync(config, context),
                    "commit_transaction" => await CommitTransactionAsync(config, context),
                    "abort_transaction" => await AbortTransactionAsync(config, context),
                    "get_metadata" => await GetMetadataAsync(config, context),
                    "get_watermark_offsets" => await GetWatermarkOffsetsAsync(config, context),
                    "pause_partitions" => await PausePartitionsAsync(config, context),
                    "resume_partitions" => await ResumePartitionsAsync(config, context),
                    _ => throw new ArgumentException($"Unknown operation: {operation}")
                };
            }
            catch (Exception ex)
            {
                Log("error", $"Kafka operation failed: {ex.Message}", new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["error"] = ex.Message,
                    ["stack_trace"] = ex.StackTrace
                });

                return CreateErrorResponse("KAFKA_OPERATION_FAILED", ex.Message, new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["config"] = config
                });
            }
        }

        private async Task<object> ProduceMessageAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var producerConfig = BuildProducerConfig(config);
            var clientId = $"{config.GetValueOrDefault("client_id", "tusklang-producer")}-{Guid.NewGuid():N}";
            
            var producer = _producers.GetOrAdd(clientId, _ => new ProducerBuilder<string, string>(producerConfig)
                .SetErrorHandler((_, e) => Log("error", $"Producer error: {e.Reason}", new Dictionary<string, object> { ["code"] = e.Code }))
                .SetLogHandler((_, logMessage) => Log("debug", $"Producer log: {logMessage.Message}", new Dictionary<string, object> { ["level"] = logMessage.Level }))
                .Build());

            var topic = config["topic"].ToString();
            var key = config.GetValueOrDefault("key", null)?.ToString();
            var value = config["value"].ToString();
            
            // Handle headers
            Headers headers = null;
            if (config.ContainsKey("headers") && config["headers"] is Dictionary<string, object> headerDict)
            {
                headers = new Headers();
                foreach (var header in headerDict)
                {
                    headers.Add(header.Key, System.Text.Encoding.UTF8.GetBytes(header.Value?.ToString() ?? ""));
                }
            }

            try
            {
                var message = new Message<string, string>
                {
                    Key = key,
                    Value = value,
                    Headers = headers,
                    Timestamp = Timestamp.Default
                };

                var partition = config.ContainsKey("partition") 
                    ? new Partition(Convert.ToInt32(config["partition"])) 
                    : Partition.Any;

                var topicPartition = new TopicPartition(topic, partition);
                
                var deliveryResult = await producer.ProduceAsync(topicPartition, message, _cancellationTokenSource.Token);

                Log("info", $"Message produced to {deliveryResult.TopicPartition}", new Dictionary<string, object>
                {
                    ["offset"] = deliveryResult.Offset.Value,
                    ["partition"] = deliveryResult.Partition.Value,
                    ["timestamp"] = deliveryResult.Timestamp.UtcDateTime
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["topic"] = deliveryResult.Topic,
                    ["partition"] = deliveryResult.Partition.Value,
                    ["offset"] = deliveryResult.Offset.Value,
                    ["timestamp"] = deliveryResult.Timestamp.UtcDateTime,
                    ["key"] = deliveryResult.Key,
                    ["value"] = deliveryResult.Value,
                    ["headers"] = deliveryResult.Headers?.ToDictionary(h => h.Key, h => System.Text.Encoding.UTF8.GetString(h.GetValueBytes())),
                    ["status"] = deliveryResult.Status.ToString()
                };
            }
            catch (ProduceException<string, string> ex)
            {
                Log("error", $"Failed to produce message: {ex.Error.Reason}", new Dictionary<string, object>
                {
                    ["code"] = ex.Error.Code,
                    ["topic"] = topic,
                    ["key"] = key
                });

                throw new InvalidOperationException($"Failed to produce message: {ex.Error.Reason}", ex);
            }
        }

        private async Task<object> ConsumeMessagesAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var consumerConfig = BuildConsumerConfig(config);
            var clientId = $"{config.GetValueOrDefault("client_id", "tusklang-consumer")}-{Guid.NewGuid():N}";
            
            var consumer = _consumers.GetOrAdd(clientId, _ => new ConsumerBuilder<string, string>(consumerConfig)
                .SetErrorHandler((_, e) => Log("error", $"Consumer error: {e.Reason}", new Dictionary<string, object> { ["code"] = e.Code }))
                .SetPartitionsAssignedHandler((_, partitions) => 
                {
                    Log("info", $"Partitions assigned: {string.Join(", ", partitions)}", new Dictionary<string, object> { ["partitions"] = partitions.Count });
                })
                .SetPartitionsRevokedHandler((_, partitions) => 
                {
                    Log("info", $"Partitions revoked: {string.Join(", ", partitions)}", new Dictionary<string, object> { ["partitions"] = partitions.Count });
                })
                .Build());

            // Get topics to subscribe to
            List<string> topics;
            if (config.ContainsKey("topics") && config["topics"] is IEnumerable<object> topicList)
            {
                topics = topicList.Select(t => t.ToString()).ToList();
            }
            else if (config.ContainsKey("topic"))
            {
                topics = new List<string> { config["topic"].ToString() };
            }
            else
            {
                throw new ArgumentException("Either 'topics' or 'topic' must be specified");
            }

            consumer.Subscribe(topics);

            var maxMessages = Convert.ToInt32(config.GetValueOrDefault("max_messages", 100));
            var timeoutMs = Convert.ToInt32(config.GetValueOrDefault("timeout_ms", 1000));
            var messages = new List<Dictionary<string, object>>();

            Log("info", $"Starting to consume from topics: {string.Join(", ", topics)}", new Dictionary<string, object>
            {
                ["max_messages"] = maxMessages,
                ["timeout_ms"] = timeoutMs
            });

            try
            {
                for (int i = 0; i < maxMessages; i++)
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(timeoutMs));
                    
                    if (consumeResult == null)
                        break; // Timeout reached

                    if (consumeResult.IsPartitionEOF)
                    {
                        Log("debug", $"Reached end of partition: {consumeResult.TopicPartition}", new Dictionary<string, object>());
                        continue;
                    }

                    var message = new Dictionary<string, object>
                    {
                        ["topic"] = consumeResult.Topic,
                        ["partition"] = consumeResult.Partition.Value,
                        ["offset"] = consumeResult.Offset.Value,
                        ["key"] = consumeResult.Key,
                        ["value"] = consumeResult.Value,
                        ["timestamp"] = consumeResult.Timestamp.UtcDateTime,
                        ["headers"] = consumeResult.Headers?.ToDictionary(h => h.Key, h => System.Text.Encoding.UTF8.GetString(h.GetValueBytes())) ?? new Dictionary<string, string>()
                    };

                    messages.Add(message);

                    // Auto-commit if enabled
                    if (Convert.ToBoolean(config.GetValueOrDefault("enable_auto_commit", true)))
                    {
                        consumer.Commit(consumeResult);
                    }
                }

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["messages"] = messages,
                    ["consumed_count"] = messages.Count,
                    ["topics"] = topics,
                    ["consumer_group"] = consumerConfig["group.id"],
                    ["assignment"] = consumer.Assignment?.Select(tp => new Dictionary<string, object>
                    {
                        ["topic"] = tp.Topic,
                        ["partition"] = tp.Partition.Value
                    }).ToList() ?? new List<object>()
                };
            }
            finally
            {
                consumer.Close();
                _consumers.TryRemove(clientId, out _);
            }
        }

        private async Task<object> CreateTopicAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var adminConfig = BuildAdminConfig(config);
            var clientId = $"{config.GetValueOrDefault("client_id", "tusklang-admin")}-{Guid.NewGuid():N}";
            
            var adminClient = _adminClients.GetOrAdd(clientId, _ => new AdminClientBuilder(adminConfig).Build());

            var topic = config["topic"].ToString();
            var partitions = Convert.ToInt32(config["partitions"]);
            var replicationFactor = Convert.ToInt16(config.GetValueOrDefault("replication_factor", 1));
            
            var topicSpecification = new TopicSpecification
            {
                Name = topic,
                NumPartitions = partitions,
                ReplicationFactor = replicationFactor
            };

            // Add topic configurations if provided
            if (config.ContainsKey("configs") && config["configs"] is Dictionary<string, object> topicConfigs)
            {
                topicSpecification.Configs = topicConfigs.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString());
            }

            try
            {
                var timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(config.GetValueOrDefault("admin_timeout", 30000)));
                await adminClient.CreateTopicsAsync(new[] { topicSpecification }, new CreateTopicsOptions { RequestTimeout = timeout });

                Log("info", $"Topic created: {topic}", new Dictionary<string, object>
                {
                    ["partitions"] = partitions,
                    ["replication_factor"] = replicationFactor
                });

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["topic"] = topic,
                    ["partitions"] = partitions,
                    ["replication_factor"] = replicationFactor,
                    ["configs"] = topicSpecification.Configs ?? new Dictionary<string, string>()
                };
            }
            catch (CreateTopicsException ex)
            {
                var error = ex.Results.FirstOrDefault().Value?.Error;
                Log("error", $"Failed to create topic: {error?.Reason}", new Dictionary<string, object>
                {
                    ["code"] = error?.Code,
                    ["topic"] = topic
                });

                throw new InvalidOperationException($"Failed to create topic: {error?.Reason}", ex);
            }
        }

        private async Task<object> TransactionalProduceAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var producerConfig = BuildProducerConfig(config);
            producerConfig["enable.idempotence"] = "true";
            producerConfig["transactional.id"] = config["transactional_id"].ToString();

            var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            try
            {
                producer.InitTransactions(TimeSpan.FromSeconds(30));
                producer.BeginTransaction();

                var results = new List<Dictionary<string, object>>();
                
                if (config["messages"] is IEnumerable<object> messageList)
                {
                    foreach (var messageObj in messageList)
                    {
                        if (messageObj is Dictionary<string, object> messageConfig)
                        {
                            var topic = messageConfig["topic"].ToString();
                            var key = messageConfig.GetValueOrDefault("key", null)?.ToString();
                            var value = messageConfig["value"].ToString();

                            var message = new Message<string, string>
                            {
                                Key = key,
                                Value = value
                            };

                            var result = await producer.ProduceAsync(topic, message);
                            results.Add(new Dictionary<string, object>
                            {
                                ["topic"] = result.Topic,
                                ["partition"] = result.Partition.Value,
                                ["offset"] = result.Offset.Value,
                                ["key"] = result.Key,
                                ["value"] = result.Value
                            });
                        }
                    }
                }

                producer.CommitTransaction();

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["transactional_id"] = config["transactional_id"],
                    ["messages_produced"] = results.Count,
                    ["results"] = results
                };
            }
            catch (Exception ex)
            {
                producer.AbortTransaction();
                Log("error", $"Transaction failed: {ex.Message}", new Dictionary<string, object> { ["transactional_id"] = config["transactional_id"] });
                throw;
            }
            finally
            {
                producer?.Dispose();
            }
        }

        private async Task<object> ListTopicsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var adminConfig = BuildAdminConfig(config);
            var adminClient = new AdminClientBuilder(adminConfig).Build();

            try
            {
                var timeout = TimeSpan.FromMilliseconds(Convert.ToInt32(config.GetValueOrDefault("metadata_timeout", 30000)));
                var metadata = adminClient.GetMetadata(timeout);

                var topics = metadata.Topics.Select(topic => new Dictionary<string, object>
                {
                    ["name"] = topic.Topic,
                    ["partitions"] = topic.Partitions.Count,
                    ["partition_details"] = topic.Partitions.Select(p => new Dictionary<string, object>
                    {
                        ["id"] = p.PartitionId,
                        ["leader"] = p.Leader,
                        ["replicas"] = p.Replicas,
                        ["isr"] = p.InSyncReplicas,
                        ["error"] = p.Error?.ToString()
                    }).ToList(),
                    ["error"] = topic.Error?.ToString()
                }).ToList();

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["topics"] = topics,
                    ["cluster_id"] = metadata.ClusterId,
                    ["controller_id"] = metadata.ControllerId,
                    ["brokers"] = metadata.Brokers.Select(b => new Dictionary<string, object>
                    {
                        ["id"] = b.BrokerId,
                        ["host"] = b.Host,
                        ["port"] = b.Port
                    }).ToList()
                };
            }
            finally
            {
                adminClient?.Dispose();
            }
        }

        private Task<object> DeleteTopicAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for delete topic
            throw new NotImplementedException("DeleteTopicAsync implementation");
        }

        private Task<object> DescribeTopicAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for describe topic
            throw new NotImplementedException("DescribeTopicAsync implementation");
        }

        private Task<object> CreatePartitionsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for create partitions
            throw new NotImplementedException("CreatePartitionsAsync implementation");
        }

        private Task<object> DescribeConsumerGroupsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for describe consumer groups
            throw new NotImplementedException("DescribeConsumerGroupsAsync implementation");
        }

        private Task<object> ListConsumerGroupsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for list consumer groups
            throw new NotImplementedException("ListConsumerGroupsAsync implementation");
        }

        private Task<object> DeleteConsumerGroupAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for delete consumer group
            throw new NotImplementedException("DeleteConsumerGroupAsync implementation");
        }

        private Task<object> CommitOffsetsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for commit offsets
            throw new NotImplementedException("CommitOffsetsAsync implementation");
        }

        private Task<object> GetOffsetsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for get offsets
            throw new NotImplementedException("GetOffsetsAsync implementation");
        }

        private Task<object> SeekToBeginningAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for seek to beginning
            throw new NotImplementedException("SeekToBeginningAsync implementation");
        }

        private Task<object> SeekToEndAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for seek to end
            throw new NotImplementedException("SeekToEndAsync implementation");
        }

        private Task<object> BeginTransactionAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for begin transaction
            throw new NotImplementedException("BeginTransactionAsync implementation");
        }

        private Task<object> CommitTransactionAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for commit transaction
            throw new NotImplementedException("CommitTransactionAsync implementation");
        }

        private Task<object> AbortTransactionAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for abort transaction
            throw new NotImplementedException("AbortTransactionAsync implementation");
        }

        private Task<object> GetMetadataAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for get metadata
            throw new NotImplementedException("GetMetadataAsync implementation");
        }

        private Task<object> GetWatermarkOffsetsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for get watermark offsets
            throw new NotImplementedException("GetWatermarkOffsetsAsync implementation");
        }

        private Task<object> PausePartitionsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for pause partitions
            throw new NotImplementedException("PausePartitionsAsync implementation");
        }

        private Task<object> ResumePartitionsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // Implementation for resume partitions
            throw new NotImplementedException("ResumePartitionsAsync implementation");
        }

        private ProducerConfig BuildProducerConfig(Dictionary<string, object> config)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config.GetValueOrDefault("bootstrap_servers", "localhost:9092").ToString(),
                ClientId = config.GetValueOrDefault("client_id", "tusklang-producer").ToString(),
                EnableIdempotence = Convert.ToBoolean(config.GetValueOrDefault("enable_idempotence", true)),
                Retries = Convert.ToInt32(config.GetValueOrDefault("retries", int.MaxValue)),
                DeliveryTimeoutMs = Convert.ToInt32(config.GetValueOrDefault("delivery_timeout_ms", 300000)),
                RequestTimeoutMs = Convert.ToInt32(config.GetValueOrDefault("request_timeout_ms", 30000)),
                Acks = (Acks)Enum.Parse(typeof(Acks), config.GetValueOrDefault("acks", "All").ToString(), true),
                CompressionType = (CompressionType)Enum.Parse(typeof(CompressionType), config.GetValueOrDefault("compression_type", "Snappy").ToString(), true),
                BatchSize = Convert.ToInt32(config.GetValueOrDefault("batch_size", 16384)),
                LingerMs = Convert.ToDouble(config.GetValueOrDefault("linger_ms", 5)),
                BufferMemory = Convert.ToInt64(config.GetValueOrDefault("buffer_memory", 33554432)),
                MaxRequestSize = Convert.ToInt32(config.GetValueOrDefault("max_request_size", 1048576)),
                SecurityProtocol = (SecurityProtocol)Enum.Parse(typeof(SecurityProtocol), config.GetValueOrDefault("security_protocol", "Plaintext").ToString(), true)
            };

            // Add security configurations
            if (config.ContainsKey("sasl_mechanism"))
            {
                producerConfig.SaslMechanism = (SaslMechanism)Enum.Parse(typeof(SaslMechanism), config["sasl_mechanism"].ToString(), true);
            }

            if (config.ContainsKey("sasl_username"))
                producerConfig.SaslUsername = config["sasl_username"].ToString();

            if (config.ContainsKey("sasl_password"))
                producerConfig.SaslPassword = config["sasl_password"].ToString();

            if (config.ContainsKey("ssl_ca_location"))
                producerConfig.SslCaLocation = config["ssl_ca_location"].ToString();

            if (config.ContainsKey("ssl_certificate_location"))
                producerConfig.SslCertificateLocation = config["ssl_certificate_location"].ToString();

            if (config.ContainsKey("ssl_key_location"))
                producerConfig.SslKeyLocation = config["ssl_key_location"].ToString();

            if (config.ContainsKey("transactional_id"))
                producerConfig.TransactionalId = config["transactional_id"].ToString();

            return producerConfig;
        }

        private ConsumerConfig BuildConsumerConfig(Dictionary<string, object> config)
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = config.GetValueOrDefault("bootstrap_servers", "localhost:9092").ToString(),
                ClientId = config.GetValueOrDefault("client_id", "tusklang-consumer").ToString(),
                GroupId = config.GetValueOrDefault("group_id", "tusklang-consumer-group").ToString(),
                AutoOffsetReset = (AutoOffsetReset)Enum.Parse(typeof(AutoOffsetReset), config.GetValueOrDefault("auto_offset_reset", "Earliest").ToString(), true),
                EnableAutoCommit = Convert.ToBoolean(config.GetValueOrDefault("enable_auto_commit", true)),
                SessionTimeoutMs = Convert.ToInt32(config.GetValueOrDefault("session_timeout_ms", 30000)),
                HeartbeatIntervalMs = Convert.ToInt32(config.GetValueOrDefault("heartbeat_interval_ms", 3000)),
                MaxPollIntervalMs = Convert.ToInt32(config.GetValueOrDefault("max_poll_interval_ms", 300000)),
                FetchMinBytes = Convert.ToInt32(config.GetValueOrDefault("fetch_min_bytes", 1)),
                FetchMaxWaitMs = Convert.ToInt32(config.GetValueOrDefault("fetch_max_wait_ms", 500)),
                MaxPartitionFetchBytes = Convert.ToInt32(config.GetValueOrDefault("max_partition_fetch_bytes", 1048576)),
                SecurityProtocol = (SecurityProtocol)Enum.Parse(typeof(SecurityProtocol), config.GetValueOrDefault("security_protocol", "Plaintext").ToString(), true),
                IsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), config.GetValueOrDefault("isolation_level", "ReadUncommitted").ToString(), true)
            };

            // Add security configurations
            if (config.ContainsKey("sasl_mechanism"))
            {
                consumerConfig.SaslMechanism = (SaslMechanism)Enum.Parse(typeof(SaslMechanism), config["sasl_mechanism"].ToString(), true);
            }

            if (config.ContainsKey("sasl_username"))
                consumerConfig.SaslUsername = config["sasl_username"].ToString();

            if (config.ContainsKey("sasl_password"))
                consumerConfig.SaslPassword = config["sasl_password"].ToString();

            if (config.ContainsKey("ssl_ca_location"))
                consumerConfig.SslCaLocation = config["ssl_ca_location"].ToString();

            return consumerConfig;
        }

        private AdminClientConfig BuildAdminConfig(Dictionary<string, object> config)
        {
            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = config.GetValueOrDefault("bootstrap_servers", "localhost:9092").ToString(),
                ClientId = config.GetValueOrDefault("client_id", "tusklang-admin").ToString(),
                SecurityProtocol = (SecurityProtocol)Enum.Parse(typeof(SecurityProtocol), config.GetValueOrDefault("security_protocol", "Plaintext").ToString(), true)
            };

            // Add security configurations
            if (config.ContainsKey("sasl_mechanism"))
            {
                adminConfig.SaslMechanism = (SaslMechanism)Enum.Parse(typeof(SaslMechanism), config["sasl_mechanism"].ToString(), true);
            }

            if (config.ContainsKey("sasl_username"))
                adminConfig.SaslUsername = config["sasl_username"].ToString();

            if (config.ContainsKey("sasl_password"))
                adminConfig.SaslPassword = config["sasl_password"].ToString();

            return adminConfig;
        }

        public override void Cleanup()
        {
            _cancellationTokenSource.Cancel();

            foreach (var producer in _producers.Values)
            {
                producer?.Flush();
                producer?.Dispose();
            }
            _producers.Clear();

            foreach (var consumer in _consumers.Values)
            {
                consumer?.Close();
                consumer?.Dispose();
            }
            _consumers.Clear();

            foreach (var adminClient in _adminClients.Values)
            {
                adminClient?.Dispose();
            }
            _adminClients.Clear();

            _cancellationTokenSource.Dispose();

            base.Cleanup();
        }
    }

    /// <summary>
    /// Extension methods for Dictionary operations
    /// </summary>
    internal static class DictionaryExtensions
    {
        internal static T GetValueOrDefault<T>(this Dictionary<string, object> dict, string key, T defaultValue = default)
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
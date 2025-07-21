using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.IO;

namespace TuskLang
{
    /// <summary>
    /// Advanced event streaming and message queue system for TuskLang C# SDK
    /// Provides event publishing, subscription, message queuing, and stream processing
    /// </summary>
    public class EventStreamingMessageQueue
    {
        private readonly Dictionary<string, EventStream> _eventStreams;
        private readonly Dictionary<string, MessageQueue> _messageQueues;
        private readonly List<IEventProcessor> _eventProcessors;
        private readonly List<IMessageHandler> _messageHandlers;
        private readonly EventMetrics _metrics;
        private readonly StreamProcessor _streamProcessor;
        private readonly object _lock = new object();

        public EventStreamingMessageQueue()
        {
            _eventStreams = new Dictionary<string, EventStream>();
            _messageQueues = new Dictionary<string, MessageQueue>();
            _eventProcessors = new List<IEventProcessor>();
            _messageHandlers = new List<IMessageHandler>();
            _metrics = new EventMetrics();
            _streamProcessor = new StreamProcessor();

            // Register default processors
            RegisterEventProcessor(new JsonEventProcessor());
            RegisterEventProcessor(new BinaryEventProcessor());
            
            // Register default message handlers
            RegisterMessageHandler(new AsyncMessageHandler());
            RegisterMessageHandler(new BatchMessageHandler());
        }

        /// <summary>
        /// Create an event stream
        /// </summary>
        public EventStream CreateEventStream(string streamName, EventStreamConfig config = null)
        {
            lock (_lock)
            {
                if (_eventStreams.ContainsKey(streamName))
                {
                    throw new InvalidOperationException($"Event stream '{streamName}' already exists");
                }

                var stream = new EventStream(streamName, config ?? new EventStreamConfig());
                _eventStreams[streamName] = stream;
                return stream;
            }
        }

        /// <summary>
        /// Create a message queue
        /// </summary>
        public MessageQueue CreateMessageQueue(string queueName, MessageQueueConfig config = null)
        {
            lock (_lock)
            {
                if (_messageQueues.ContainsKey(queueName))
                {
                    throw new InvalidOperationException($"Message queue '{queueName}' already exists");
                }

                var queue = new MessageQueue(queueName, config ?? new MessageQueueConfig());
                _messageQueues[queueName] = queue;
                return queue;
            }
        }

        /// <summary>
        /// Publish an event to a stream
        /// </summary>
        public async Task<EventResult> PublishEventAsync(string streamName, EventData eventData)
        {
            if (!_eventStreams.TryGetValue(streamName, out var stream))
            {
                throw new InvalidOperationException($"Event stream '{streamName}' not found");
            }

            var result = await stream.PublishEventAsync(eventData);
            _metrics.RecordEventPublished(streamName, result.Success);
            
            return result;
        }

        /// <summary>
        /// Subscribe to an event stream
        /// </summary>
        public async Task<EventSubscription> SubscribeToStreamAsync(
            string streamName, 
            Func<EventData, Task> handler,
            SubscriptionConfig config = null)
        {
            if (!_eventStreams.TryGetValue(streamName, out var stream))
            {
                throw new InvalidOperationException($"Event stream '{streamName}' not found");
            }

            var subscription = await stream.SubscribeAsync(handler, config ?? new SubscriptionConfig());
            _metrics.RecordSubscriptionCreated(streamName);
            
            return subscription;
        }

        /// <summary>
        /// Send a message to a queue
        /// </summary>
        public async Task<MessageResult> SendMessageAsync(string queueName, MessageData messageData)
        {
            if (!_messageQueues.TryGetValue(queueName, out var queue))
            {
                throw new InvalidOperationException($"Message queue '{queueName}' not found");
            }

            var result = await queue.SendMessageAsync(messageData);
            _metrics.RecordMessageSent(queueName, result.Success);
            
            return result;
        }

        /// <summary>
        /// Receive messages from a queue
        /// </summary>
        public async Task<List<MessageData>> ReceiveMessagesAsync(
            string queueName, 
            int maxMessages = 10,
            TimeSpan? timeout = null)
        {
            if (!_messageQueues.TryGetValue(queueName, out var queue))
            {
                throw new InvalidOperationException($"Message queue '{queueName}' not found");
            }

            var messages = await queue.ReceiveMessagesAsync(maxMessages, timeout);
            _metrics.RecordMessagesReceived(queueName, messages.Count);
            
            return messages;
        }

        /// <summary>
        /// Process events with stream processing
        /// </summary>
        public async Task<StreamProcessingResult> ProcessEventStreamAsync(
            string streamName,
            IStreamProcessor processor,
            ProcessingConfig config = null)
        {
            if (!_eventStreams.TryGetValue(streamName, out var stream))
            {
                throw new InvalidOperationException($"Event stream '{streamName}' not found");
            }

            return await _streamProcessor.ProcessStreamAsync(stream, processor, config ?? new ProcessingConfig());
        }

        /// <summary>
        /// Register an event processor
        /// </summary>
        public void RegisterEventProcessor(IEventProcessor processor)
        {
            lock (_lock)
            {
                _eventProcessors.Add(processor);
            }
        }

        /// <summary>
        /// Register a message handler
        /// </summary>
        public void RegisterMessageHandler(IMessageHandler handler)
        {
            lock (_lock)
            {
                _messageHandlers.Add(handler);
            }
        }

        /// <summary>
        /// Get event metrics
        /// </summary>
        public EventMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Get all stream names
        /// </summary>
        public List<string> GetStreamNames()
        {
            lock (_lock)
            {
                return _eventStreams.Keys.ToList();
            }
        }

        /// <summary>
        /// Get all queue names
        /// </summary>
        public List<string> GetQueueNames()
        {
            lock (_lock)
            {
                return _messageQueues.Keys.ToList();
            }
        }
    }

    /// <summary>
    /// Event stream implementation
    /// </summary>
    public class EventStream
    {
        private readonly string _name;
        private readonly EventStreamConfig _config;
        private readonly ConcurrentQueue<EventData> _events;
        private readonly List<EventSubscription> _subscriptions;
        private readonly object _lock = new object();

        public EventStream(string name, EventStreamConfig config)
        {
            _name = name;
            _config = config;
            _events = new ConcurrentQueue<EventData>();
            _subscriptions = new List<EventSubscription>();
        }

        public async Task<EventResult> PublishEventAsync(EventData eventData)
        {
            eventData.StreamName = _name;
            eventData.Timestamp = DateTime.UtcNow;
            eventData.EventId = Guid.NewGuid().ToString();

            _events.Enqueue(eventData);

            // Notify subscribers
            var tasks = _subscriptions.Select(sub => sub.Handler(eventData));
            await Task.WhenAll(tasks);

            return new EventResult
            {
                Success = true,
                EventId = eventData.EventId,
                Timestamp = eventData.Timestamp
            };
        }

        public async Task<EventSubscription> SubscribeAsync(Func<EventData, Task> handler, SubscriptionConfig config)
        {
            var subscription = new EventSubscription
            {
                Id = Guid.NewGuid().ToString(),
                StreamName = _name,
                Handler = handler,
                Config = config
            };

            lock (_lock)
            {
                _subscriptions.Add(subscription);
            }

            return await Task.FromResult(subscription);
        }

        public void Unsubscribe(string subscriptionId)
        {
            lock (_lock)
            {
                var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
                if (subscription != null)
                {
                    _subscriptions.Remove(subscription);
                }
            }
        }

        public List<EventData> GetEvents(int maxEvents = 100)
        {
            var events = new List<EventData>();
            var count = 0;

            while (_events.TryDequeue(out var eventData) && count < maxEvents)
            {
                events.Add(eventData);
                count++;
            }

            return events;
        }
    }

    /// <summary>
    /// Message queue implementation
    /// </summary>
    public class MessageQueue
    {
        private readonly string _name;
        private readonly MessageQueueConfig _config;
        private readonly ConcurrentQueue<MessageData> _messages;
        private readonly SemaphoreSlim _semaphore;

        public MessageQueue(string name, MessageQueueConfig config)
        {
            _name = name;
            _config = config;
            _messages = new ConcurrentQueue<MessageData>();
            _semaphore = new SemaphoreSlim(config.MaxConcurrentConsumers, config.MaxConcurrentConsumers);
        }

        public async Task<MessageResult> SendMessageAsync(MessageData messageData)
        {
            messageData.QueueName = _name;
            messageData.Timestamp = DateTime.UtcNow;
            messageData.MessageId = Guid.NewGuid().ToString();

            _messages.Enqueue(messageData);

            return await Task.FromResult(new MessageResult
            {
                Success = true,
                MessageId = messageData.MessageId,
                Timestamp = messageData.Timestamp
            });
        }

        public async Task<List<MessageData>> ReceiveMessagesAsync(int maxMessages, TimeSpan? timeout)
        {
            var messages = new List<MessageData>();
            var timeoutToken = timeout.HasValue ? new CancellationTokenSource(timeout.Value).Token : CancellationToken.None;

            try
            {
                await _semaphore.WaitAsync(timeoutToken);

                for (int i = 0; i < maxMessages && _messages.TryDequeue(out var message); i++)
                {
                    messages.Add(message);
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return messages;
        }

        public async Task<bool> DeleteMessageAsync(string messageId)
        {
            // In a real implementation, this would mark the message as deleted
            return await Task.FromResult(true);
        }
    }

    /// <summary>
    /// Stream processor for processing event streams
    /// </summary>
    public class StreamProcessor
    {
        public async Task<StreamProcessingResult> ProcessStreamAsync(
            EventStream stream,
            IStreamProcessor processor,
            ProcessingConfig config)
        {
            var startTime = DateTime.UtcNow;
            var processedEvents = 0;
            var errors = new List<string>();

            try
            {
                var events = stream.GetEvents(config.MaxEvents);
                
                foreach (var eventData in events)
                {
                    try
                    {
                        await processor.ProcessEventAsync(eventData);
                        processedEvents++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error processing event {eventData.EventId}: {ex.Message}");
                    }
                }

                return new StreamProcessingResult
                {
                    Success = errors.Count == 0,
                    ProcessedEvents = processedEvents,
                    Errors = errors,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new StreamProcessingResult
                {
                    Success = false,
                    Errors = new List<string> { ex.Message },
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Event processor interface
    /// </summary>
    public interface IEventProcessor
    {
        Task<EventData> ProcessEventAsync(EventData eventData);
    }

    /// <summary>
    /// Message handler interface
    /// </summary>
    public interface IMessageHandler
    {
        Task<bool> HandleMessageAsync(MessageData message);
    }

    /// <summary>
    /// Stream processor interface
    /// </summary>
    public interface IStreamProcessor
    {
        Task ProcessEventAsync(EventData eventData);
    }

    /// <summary>
    /// JSON event processor
    /// </summary>
    public class JsonEventProcessor : IEventProcessor
    {
        public async Task<EventData> ProcessEventAsync(EventData eventData)
        {
            if (eventData.Data is string jsonData)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    eventData.ProcessedData = data;
                }
                catch
                {
                    // Invalid JSON, keep original data
                }
            }

            return await Task.FromResult(eventData);
        }
    }

    /// <summary>
    /// Binary event processor
    /// </summary>
    public class BinaryEventProcessor : IEventProcessor
    {
        public async Task<EventData> ProcessEventAsync(EventData eventData)
        {
            if (eventData.Data is byte[] binaryData)
            {
                // Process binary data
                eventData.ProcessedData = Convert.ToBase64String(binaryData);
            }

            return await Task.FromResult(eventData);
        }
    }

    /// <summary>
    /// Async message handler
    /// </summary>
    public class AsyncMessageHandler : IMessageHandler
    {
        public async Task<bool> HandleMessageAsync(MessageData message)
        {
            // Process message asynchronously
            await Task.Delay(10); // Simulate processing time
            
            return true;
        }
    }

    /// <summary>
    /// Batch message handler
    /// </summary>
    public class BatchMessageHandler : IMessageHandler
    {
        private readonly List<MessageData> _batch = new List<MessageData>();
        private readonly object _lock = new object();

        public async Task<bool> HandleMessageAsync(MessageData message)
        {
            lock (_lock)
            {
                _batch.Add(message);
                
                if (_batch.Count >= 10) // Process in batches of 10
                {
                    ProcessBatch();
                }
            }

            return await Task.FromResult(true);
        }

        private void ProcessBatch()
        {
            // Process the batch
            _batch.Clear();
        }
    }

    // Data transfer objects
    public class EventData
    {
        public string EventId { get; set; }
        public string StreamName { get; set; }
        public string EventType { get; set; }
        public object Data { get; set; }
        public object ProcessedData { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class MessageData
    {
        public string MessageId { get; set; }
        public string QueueName { get; set; }
        public string MessageType { get; set; }
        public object Data { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
    }

    public class EventResult
    {
        public bool Success { get; set; }
        public string EventId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MessageResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EventSubscription
    {
        public string Id { get; set; }
        public string StreamName { get; set; }
        public Func<EventData, Task> Handler { get; set; }
        public SubscriptionConfig Config { get; set; }
    }

    public class StreamProcessingResult
    {
        public bool Success { get; set; }
        public int ProcessedEvents { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ProcessingTime { get; set; }
    }

    // Configuration classes
    public class EventStreamConfig
    {
        public int MaxEvents { get; set; } = 1000;
        public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
        public bool EnablePersistence { get; set; } = false;
    }

    public class MessageQueueConfig
    {
        public int MaxConcurrentConsumers { get; set; } = 5;
        public int MaxQueueSize { get; set; } = 10000;
        public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableDeadLetterQueue { get; set; } = true;
    }

    public class SubscriptionConfig
    {
        public bool StartFromBeginning { get; set; } = true;
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);
        public int MaxBatchSize { get; set; } = 100;
    }

    public class ProcessingConfig
    {
        public int MaxEvents { get; set; } = 1000;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableParallelProcessing { get; set; } = true;
    }

    /// <summary>
    /// Event metrics collection
    /// </summary>
    public class EventMetrics
    {
        private readonly Dictionary<string, int> _eventsPublished = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _eventsConsumed = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _subscriptionsCreated = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _messagesSent = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _messagesReceived = new Dictionary<string, int>();
        private readonly object _lock = new object();

        public void RecordEventPublished(string streamName, bool success)
        {
            lock (_lock)
            {
                if (success)
                {
                    _eventsPublished[streamName] = _eventsPublished.GetValueOrDefault(streamName, 0) + 1;
                }
            }
        }

        public void RecordEventConsumed(string streamName)
        {
            lock (_lock)
            {
                _eventsConsumed[streamName] = _eventsConsumed.GetValueOrDefault(streamName, 0) + 1;
            }
        }

        public void RecordSubscriptionCreated(string streamName)
        {
            lock (_lock)
            {
                _subscriptionsCreated[streamName] = _subscriptionsCreated.GetValueOrDefault(streamName, 0) + 1;
            }
        }

        public void RecordMessageSent(string queueName, bool success)
        {
            lock (_lock)
            {
                if (success)
                {
                    _messagesSent[queueName] = _messagesSent.GetValueOrDefault(queueName, 0) + 1;
                }
            }
        }

        public void RecordMessagesReceived(string queueName, int count)
        {
            lock (_lock)
            {
                _messagesReceived[queueName] = _messagesReceived.GetValueOrDefault(queueName, 0) + count;
            }
        }

        public Dictionary<string, int> GetEventsPublished() => new Dictionary<string, int>(_eventsPublished);
        public Dictionary<string, int> GetEventsConsumed() => new Dictionary<string, int>(_eventsConsumed);
        public Dictionary<string, int> GetSubscriptionsCreated() => new Dictionary<string, int>(_subscriptionsCreated);
        public Dictionary<string, int> GetMessagesSent() => new Dictionary<string, int>(_messagesSent);
        public Dictionary<string, int> GetMessagesReceived() => new Dictionary<string, int>(_messagesReceived);
    }
} 
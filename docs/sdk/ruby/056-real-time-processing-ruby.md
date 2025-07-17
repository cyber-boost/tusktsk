# Real-Time Processing with TuskLang and Ruby

## ⚡ **Process Data at the Speed of Thought**

TuskLang enables sophisticated real-time processing for Ruby applications, providing streaming data pipelines, WebSocket communication, message queues, and real-time analytics. Build systems that process and respond to data as it happens.

## 🚀 **Quick Start: Real-Time Stream Processing**

### Basic Real-Time Configuration

```ruby
# config/real_time.tsk
[real_time]
enabled: @env("REAL_TIME_ENABLED", "true")
processing_mode: @env("PROCESSING_MODE", "streaming") # streaming, batch, hybrid
buffer_size: @env("BUFFER_SIZE", "1000")
flush_interval: @env("FLUSH_INTERVAL", "5")
parallel_workers: @env("PARALLEL_WORKERS", "4")

[streaming]
enabled: @env("STREAMING_ENABLED", "true")
source_type: @env("STREAM_SOURCE", "kafka") # kafka, redis, rabbitmq, custom
batch_size: @env("STREAM_BATCH_SIZE", "100")
processing_timeout: @env("STREAM_PROCESSING_TIMEOUT", "30")
backpressure_handling: @env("BACKPRESSURE_HANDLING", "true")

[websockets]
enabled: @env("WEBSOCKETS_ENABLED", "true")
port: @env("WEBSOCKET_PORT", "8080")
max_connections: @env("MAX_WEBSOCKET_CONNECTIONS", "10000")
heartbeat_interval: @env("WEBSOCKET_HEARTBEAT", "30")
```

### Stream Processor Implementation

```ruby
# lib/stream_processor.rb
require 'tusk'
require 'redis'
require 'json'
require 'securerandom'
require 'concurrent'

class StreamProcessor
  def initialize(config_path = 'config/real_time.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    @processors = {}
    @streams = {}
    @workers = []
    setup_stream_processor
  end

  def create_stream(stream_name, source_config = {})
    return { success: false, error: 'Real-time processing disabled' } unless @config['real_time']['enabled'] == 'true'

    stream_id = SecureRandom.uuid
    stream_config = {
      id: stream_id,
      name: stream_name,
      source_type: source_config[:source_type] || @config['streaming']['source_type'],
      source_config: source_config,
      batch_size: source_config[:batch_size] || @config['streaming']['batch_size'].to_i,
      processing_timeout: source_config[:processing_timeout] || @config['streaming']['processing_timeout'].to_i,
      created_at: Time.now.iso8601,
      status: 'active'
    }

    @streams[stream_name] = stream_config
    @redis.hset('streams', stream_name, stream_config.to_json)
    
    # Initialize stream source
    initialize_stream_source(stream_config)
    
    {
      success: true,
      stream_id: stream_id,
      stream_name: stream_name
    }
  end

  def add_processor(stream_name, processor_name, processor_config)
    return { success: false, error: 'Stream not found' } unless @streams[stream_name]

    processor_id = SecureRandom.uuid
    processor = {
      id: processor_id,
      name: processor_name,
      stream_name: stream_name,
      config: processor_config,
      created_at: Time.now.iso8601,
      status: 'active'
    }

    @processors[processor_name] = processor
    @redis.hset("processors:#{stream_name}", processor_name, processor.to_json)
    
    {
      success: true,
      processor_id: processor_id,
      processor_name: processor_name
    }
  end

  def start_processing(stream_name)
    return { success: false, error: 'Stream not found' } unless @streams[stream_name]

    stream_config = @streams[stream_name]
    processors = get_stream_processors(stream_name)

    # Start processing workers
    worker_count = @config['real_time']['parallel_workers'].to_i
    
    worker_count.times do |worker_id|
      worker = Thread.new do
        process_stream_data(stream_config, processors, worker_id)
      end
      @workers << worker
    end

    {
      success: true,
      stream_name: stream_name,
      workers_started: worker_count
    }
  end

  def stop_processing(stream_name)
    # Stop workers for this stream
    @workers.each(&:exit)
    @workers.clear

    {
      success: true,
      stream_name: stream_name
    }
  end

  def publish_event(stream_name, event_data)
    return { success: false, error: 'Stream not found' } unless @streams[stream_name]

    event = {
      id: SecureRandom.uuid,
      stream_name: stream_name,
      data: event_data,
      timestamp: Time.now.iso8601,
      metadata: {
        source: 'manual',
        worker_id: nil
      }
    }

    # Publish to stream
    publish_to_stream(stream_name, event)
    
    {
      success: true,
      event_id: event[:id]
    }
  end

  def get_stream_statistics(stream_name)
    return nil unless @streams[stream_name]

    stream_config = @streams[stream_name]
    processors = get_stream_processors(stream_name)

    {
      stream_name: stream_name,
      stream_id: stream_config[:id],
      status: stream_config[:status],
      processors_count: processors.length,
      total_events_processed: get_total_events_processed(stream_name),
      events_per_second: get_events_per_second(stream_name),
      average_processing_time: get_average_processing_time(stream_name),
      error_rate: get_error_rate(stream_name)
    }
  end

  def get_processor_statistics(processor_name)
    return nil unless @processors[processor_name]

    processor = @processors[processor_name]
    
    {
      processor_name: processor_name,
      processor_id: processor[:id],
      stream_name: processor[:stream_name],
      status: processor[:status],
      total_events_processed: get_processor_events_processed(processor_name),
      average_processing_time: get_processor_avg_processing_time(processor_name),
      error_count: get_processor_error_count(processor_name)
    }
  end

  private

  def setup_stream_processor
    # Initialize stream processor components
  end

  def initialize_stream_source(stream_config)
    case stream_config[:source_type]
    when 'kafka'
      initialize_kafka_source(stream_config)
    when 'redis'
      initialize_redis_source(stream_config)
    when 'rabbitmq'
      initialize_rabbitmq_source(stream_config)
    when 'custom'
      initialize_custom_source(stream_config)
    end
  end

  def initialize_kafka_source(stream_config)
    # Implementation for Kafka source
  end

  def initialize_redis_source(stream_config)
    # Implementation for Redis source
  end

  def initialize_rabbitmq_source(stream_config)
    # Implementation for RabbitMQ source
  end

  def initialize_custom_source(stream_config)
    # Implementation for custom source
  end

  def get_stream_processors(stream_name)
    processors_data = @redis.hgetall("processors:#{stream_name}")
    processors = []

    processors_data.each do |processor_name, processor_json|
      processor = JSON.parse(processor_json)
      processors << processor
    end

    processors
  end

  def process_stream_data(stream_config, processors, worker_id)
    loop do
      begin
        # Fetch data from stream source
        events = fetch_stream_events(stream_config, worker_id)
        
        if events.any?
          # Process events through all processors
          events.each do |event|
            process_event_through_processors(event, processors, worker_id)
          end
        else
          # No events, sleep briefly
          sleep 0.1
        end
      rescue => e
        Rails.logger.error "Stream processing error: #{e.message}"
        sleep 1
      end
    end
  end

  def fetch_stream_events(stream_config, worker_id)
    case stream_config[:source_type]
    when 'kafka'
      fetch_kafka_events(stream_config, worker_id)
    when 'redis'
      fetch_redis_events(stream_config, worker_id)
    when 'rabbitmq'
      fetch_rabbitmq_events(stream_config, worker_id)
    when 'custom'
      fetch_custom_events(stream_config, worker_id)
    else
      []
    end
  end

  def fetch_kafka_events(stream_config, worker_id)
    # Implementation for fetching Kafka events
    []
  end

  def fetch_redis_events(stream_config, worker_id)
    # Implementation for fetching Redis events
    []
  end

  def fetch_rabbitmq_events(stream_config, worker_id)
    # Implementation for fetching RabbitMQ events
    []
  end

  def fetch_custom_events(stream_config, worker_id)
    # Implementation for fetching custom events
    []
  end

  def process_event_through_processors(event, processors, worker_id)
    start_time = Time.now

    processors.each do |processor|
      begin
        process_event_with_processor(event, processor, worker_id)
        record_processor_success(processor[:name], Time.now - start_time)
      rescue => e
        record_processor_error(processor[:name], e.message)
        Rails.logger.error "Processor error: #{e.message}"
      end
    end

    record_event_processed(event[:stream_name])
  end

  def process_event_with_processor(event, processor, worker_id)
    processor_class = get_processor_class(processor[:name])
    return unless processor_class

    processor_instance = processor_class.new(processor[:config])
    processor_instance.process(event)
  end

  def get_processor_class(processor_name)
    # Map processor names to classes
    processor_classes = {
      'data_transformer' => DataTransformer,
      'event_filter' => EventFilter,
      'aggregator' => EventAggregator,
      'enricher' => EventEnricher
    }

    processor_classes[processor_name]
  end

  def publish_to_stream(stream_name, event)
    case @streams[stream_name][:source_type]
    when 'kafka'
      publish_to_kafka(stream_name, event)
    when 'redis'
      publish_to_redis(stream_name, event)
    when 'rabbitmq'
      publish_to_rabbitmq(stream_name, event)
    when 'custom'
      publish_to_custom(stream_name, event)
    end
  end

  def publish_to_kafka(stream_name, event)
    # Implementation for publishing to Kafka
  end

  def publish_to_redis(stream_name, event)
    @redis.lpush("stream:#{stream_name}", event.to_json)
  end

  def publish_to_rabbitmq(stream_name, event)
    # Implementation for publishing to RabbitMQ
  end

  def publish_to_custom(stream_name, event)
    # Implementation for publishing to custom source
  end

  def record_event_processed(stream_name)
    @redis.incr("events_processed:#{stream_name}")
  end

  def record_processor_success(processor_name, processing_time)
    @redis.incr("processor_success:#{processor_name}")
    @redis.lpush("processing_times:#{processor_name}", processing_time)
    @redis.ltrim("processing_times:#{processor_name}", 0, 999)
  end

  def record_processor_error(processor_name, error_message)
    @redis.incr("processor_errors:#{processor_name}")
    @redis.lpush("processor_error_log:#{processor_name}", {
      error: error_message,
      timestamp: Time.now.iso8601
    }.to_json)
    @redis.ltrim("processor_error_log:#{processor_name}", 0, 99)
  end

  def get_total_events_processed(stream_name)
    @redis.get("events_processed:#{stream_name}").to_i
  end

  def get_events_per_second(stream_name)
    # Implementation to calculate events per second
    0
  end

  def get_average_processing_time(stream_name)
    # Implementation to calculate average processing time
    0.0
  end

  def get_error_rate(stream_name)
    # Implementation to calculate error rate
    0.0
  end

  def get_processor_events_processed(processor_name)
    @redis.get("processor_success:#{processor_name}").to_i
  end

  def get_processor_avg_processing_time(processor_name)
    times = @redis.lrange("processing_times:#{processor_name}", 0, -1)
    return 0 if times.empty?

    total_time = times.map(&:to_f).sum
    (total_time / times.length).round(3)
  end

  def get_processor_error_count(processor_name)
    @redis.get("processor_errors:#{processor_name}").to_i
  end
end
```

## 🌐 **WebSocket Server Implementation**

### Real-Time Communication

```ruby
# lib/websocket_server.rb
require 'tusk'
require 'redis'
require 'json'
require 'securerandom'
require 'websocket-eventmachine-server'

class WebSocketServer
  def initialize(config_path = 'config/real_time.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    @connections = {}
    @channels = {}
    @heartbeat_thread = nil
    setup_websocket_server
  end

  def start_server
    return { success: false, error: 'WebSockets disabled' } unless @config['websockets']['enabled'] == 'true'

    port = @config['websockets']['port'].to_i
    max_connections = @config['websockets']['max_connections'].to_i

    EM.run do
      EM.start_server '0.0.0.0', port, WebSocketConnection, self, max_connections
      start_heartbeat
      
      Rails.logger.info "WebSocket server started on port #{port}"
    end

    { success: true, port: port }
  end

  def stop_server
    EM.stop
    stop_heartbeat
    { success: true }
  end

  def broadcast_message(channel, message, exclude_connection = nil)
    return { success: false, error: 'Channel not found' } unless @channels[channel]

    connections = @channels[channel].reject { |conn_id| conn_id == exclude_connection }
    
    connections.each do |conn_id|
      connection = @connections[conn_id]
      connection&.send_message(message)
    end

    {
      success: true,
      channel: channel,
      recipients: connections.length
    }
  end

  def send_to_connection(connection_id, message)
    connection = @connections[connection_id]
    return { success: false, error: 'Connection not found' } unless connection

    connection.send_message(message)
    { success: true }
  end

  def subscribe_to_channel(connection_id, channel)
    @channels[channel] ||= []
    @channels[channel] << connection_id unless @channels[channel].include?(connection_id)

    # Store subscription in Redis for persistence
    @redis.sadd("websocket_subscriptions:#{connection_id}", channel)
    
    {
      success: true,
      connection_id: connection_id,
      channel: channel
    }
  end

  def unsubscribe_from_channel(connection_id, channel)
    @channels[channel]&.delete(connection_id)
    @redis.srem("websocket_subscriptions:#{connection_id}", channel)
    
    {
      success: true,
      connection_id: connection_id,
      channel: channel
    }
  end

  def get_server_statistics
    {
      total_connections: @connections.length,
      active_connections: @connections.count { |_, conn| conn.active? },
      total_channels: @channels.length,
      channels_with_subscribers: @channels.count { |_, subscribers| subscribers.any? },
      max_connections: @config['websockets']['max_connections'].to_i
    }
  end

  def get_channel_statistics(channel)
    return nil unless @channels[channel]

    {
      channel: channel,
      subscriber_count: @channels[channel].length,
      subscribers: @channels[channel]
    }
  end

  def get_connection_statistics(connection_id)
    connection = @connections[connection_id]
    return nil unless connection

    {
      connection_id: connection_id,
      connected_at: connection.connected_at,
      last_activity: connection.last_activity,
      subscribed_channels: connection.subscribed_channels,
      messages_sent: connection.messages_sent,
      messages_received: connection.messages_received
    }
  end

  private

  def setup_websocket_server
    # Initialize WebSocket server components
  end

  def start_heartbeat
    interval = @config['websockets']['heartbeat_interval'].to_i
    
    @heartbeat_thread = Thread.new do
      loop do
        send_heartbeat_to_all_connections
        sleep interval
      end
    end
  end

  def stop_heartbeat
    @heartbeat_thread&.exit
    @heartbeat_thread = nil
  end

  def send_heartbeat_to_all_connections
    @connections.each do |connection_id, connection|
      begin
        connection.send_heartbeat
      rescue => e
        Rails.logger.error "Heartbeat error for connection #{connection_id}: #{e.message}"
        remove_connection(connection_id)
      end
    end
  end

  def add_connection(connection)
    @connections[connection.id] = connection
  end

  def remove_connection(connection_id)
    connection = @connections[connection_id]
    return unless connection

    # Remove from all channels
    connection.subscribed_channels.each do |channel|
      @channels[channel]&.delete(connection_id)
    end

    # Remove connection
    @connections.delete(connection_id)
    
    # Clean up Redis subscriptions
    @redis.del("websocket_subscriptions:#{connection_id}")
  end
end

class WebSocketConnection
  attr_reader :id, :connected_at, :last_activity, :subscribed_channels, :messages_sent, :messages_received

  def initialize(websocket, server, max_connections)
    @websocket = websocket
    @server = server
    @max_connections = max_connections
    @id = SecureRandom.uuid
    @connected_at = Time.now
    @last_activity = Time.now
    @subscribed_channels = []
    @messages_sent = 0
    @messages_received = 0
    @active = true

    # Check connection limit
    if @server.connections.length >= @max_connections
      close_connection("Maximum connections reached")
      return
    end

    @server.add_connection(self)
    send_welcome_message
  end

  def receive_message(message)
    @last_activity = Time.now
    @messages_received += 1

    begin
      data = JSON.parse(message)
      handle_message(data)
    rescue JSON::ParserError => e
      send_error("Invalid JSON format")
    rescue => e
      send_error("Message processing error: #{e.message}")
    end
  end

  def send_message(message)
    return unless @active

    begin
      if message.is_a?(Hash)
        message_json = message.to_json
      else
        message_json = message
      end

      @websocket.send(message_json)
      @messages_sent += 1
    rescue => e
      Rails.logger.error "Error sending message to connection #{@id}: #{e.message}"
      close_connection("Send error")
    end
  end

  def send_heartbeat
    send_message({ type: 'heartbeat', timestamp: Time.now.iso8601 })
  end

  def close_connection(reason = nil)
    @active = false
    @server.remove_connection(@id)
    
    close_message = { type: 'close', reason: reason }
    send_message(close_message)
    
    @websocket.close
  end

  def active?
    @active
  end

  private

  def send_welcome_message
    welcome_message = {
      type: 'welcome',
      connection_id: @id,
      timestamp: Time.now.iso8601
    }
    send_message(welcome_message)
  end

  def handle_message(data)
    case data['type']
    when 'subscribe'
      handle_subscribe(data)
    when 'unsubscribe'
      handle_unsubscribe(data)
    when 'message'
      handle_channel_message(data)
    when 'ping'
      handle_ping(data)
    else
      send_error("Unknown message type: #{data['type']}")
    end
  end

  def handle_subscribe(data)
    channel = data['channel']
    return send_error("Channel not specified") unless channel

    result = @server.subscribe_to_channel(@id, channel)
    if result[:success]
      @subscribed_channels << channel unless @subscribed_channels.include?(channel)
      send_message({
        type: 'subscribed',
        channel: channel,
        timestamp: Time.now.iso8601
      })
    else
      send_error(result[:error])
    end
  end

  def handle_unsubscribe(data)
    channel = data['channel']
    return send_error("Channel not specified") unless channel

    result = @server.unsubscribe_from_channel(@id, channel)
    if result[:success]
      @subscribed_channels.delete(channel)
      send_message({
        type: 'unsubscribed',
        channel: channel,
        timestamp: Time.now.iso8601
      })
    else
      send_error(result[:error])
    end
  end

  def handle_channel_message(data)
    channel = data['channel']
    message = data['message']
    
    return send_error("Channel not specified") unless channel
    return send_error("Message not specified") unless message

    # Broadcast to channel, excluding sender
    @server.broadcast_message(channel, {
      type: 'channel_message',
      channel: channel,
      message: message,
      sender: @id,
      timestamp: Time.now.iso8601
    }, @id)
  end

  def handle_ping(data)
    send_message({
      type: 'pong',
      timestamp: Time.now.iso8601
    })
  end

  def send_error(error_message)
    send_message({
      type: 'error',
      error: error_message,
      timestamp: Time.now.iso8601
    })
  end
end
```

## 📊 **Real-Time Analytics**

### Streaming Analytics Engine

```ruby
# lib/real_time_analytics.rb
require 'tusk'
require 'redis'
require 'json'
require 'securerandom'

class RealTimeAnalytics
  def initialize(config_path = 'config/real_time.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    @metrics = {}
    @aggregations = {}
    @alerts = {}
    setup_analytics
  end

  def track_event(event_type, event_data, dimensions = {})
    return { success: false, error: 'Real-time analytics disabled' } unless @config['real_time']['enabled'] == 'true'

    event = {
      id: SecureRandom.uuid,
      type: event_type,
      data: event_data,
      dimensions: dimensions,
      timestamp: Time.now.iso8601
    }

    # Store event
    store_event(event)
    
    # Update real-time metrics
    update_metrics(event)
    
    # Check alerts
    check_alerts(event)
    
    {
      success: true,
      event_id: event[:id]
    }
  end

  def create_metric(metric_name, metric_config)
    metric_id = SecureRandom.uuid
    metric = {
      id: metric_id,
      name: metric_name,
      type: metric_config[:type], # counter, gauge, histogram
      aggregation: metric_config[:aggregation], # sum, avg, min, max, count
      dimensions: metric_config[:dimensions] || [],
      window_size: metric_config[:window_size] || 3600, # seconds
      created_at: Time.now.iso8601
    }

    @metrics[metric_name] = metric
    @redis.hset('analytics_metrics', metric_name, metric.to_json)
    
    {
      success: true,
      metric_id: metric_id,
      metric_name: metric_name
    }
  end

  def get_metric_value(metric_name, dimensions = {})
    return nil unless @metrics[metric_name]

    metric = @metrics[metric_name]
    metric_key = generate_metric_key(metric_name, dimensions)
    
    case metric[:type]
    when 'counter'
      get_counter_value(metric_key)
    when 'gauge'
      get_gauge_value(metric_key)
    when 'histogram'
      get_histogram_value(metric_key)
    end
  end

  def create_aggregation(aggregation_name, aggregation_config)
    aggregation_id = SecureRandom.uuid
    aggregation = {
      id: aggregation_id,
      name: aggregation_name,
      source_metric: aggregation_config[:source_metric],
      function: aggregation_config[:function], # sum, avg, min, max, count
      group_by: aggregation_config[:group_by] || [],
      window_size: aggregation_config[:window_size] || 3600,
      created_at: Time.now.iso8601
    }

    @aggregations[aggregation_name] = aggregation
    @redis.hset('analytics_aggregations', aggregation_name, aggregation.to_json)
    
    {
      success: true,
      aggregation_id: aggregation_id,
      aggregation_name: aggregation_name
    }
  end

  def get_aggregation_value(aggregation_name, group_values = {})
    return nil unless @aggregations[aggregation_name]

    aggregation = @aggregations[aggregation_name]
    aggregation_key = generate_aggregation_key(aggregation_name, group_values)
    
    @redis.get(aggregation_key).to_f
  end

  def create_alert(alert_name, alert_config)
    alert_id = SecureRandom.uuid
    alert = {
      id: alert_id,
      name: alert_name,
      metric: alert_config[:metric],
      condition: alert_config[:condition], # >, <, >=, <=, ==, !=
      threshold: alert_config[:threshold],
      dimensions: alert_config[:dimensions] || {},
      cooldown: alert_config[:cooldown] || 300, # seconds
      created_at: Time.now.iso8601,
      status: 'active'
    }

    @alerts[alert_name] = alert
    @redis.hset('analytics_alerts', alert_name, alert.to_json)
    
    {
      success: true,
      alert_id: alert_id,
      alert_name: alert_name
    }
  end

  def get_analytics_dashboard
    {
      metrics: get_all_metrics,
      aggregations: get_all_aggregations,
      alerts: get_all_alerts,
      recent_events: get_recent_events(100),
      system_stats: get_system_stats
    }
  end

  def get_metric_history(metric_name, dimensions = {}, hours = 24)
    return nil unless @metrics[metric_name]

    metric = @metrics[metric_name]
    metric_key = generate_metric_key(metric_name, dimensions)
    
    # Get historical data from time series
    end_time = Time.now
    start_time = end_time - (hours * 3600)
    
    get_time_series_data(metric_key, start_time, end_time)
  end

  private

  def setup_analytics
    # Initialize analytics components
  end

  def store_event(event)
    # Store event in Redis
    @redis.lpush('analytics_events', event.to_json)
    @redis.ltrim('analytics_events', 0, 99999) # Keep last 100k events
  end

  def update_metrics(event)
    @metrics.each do |metric_name, metric|
      if should_update_metric(metric, event)
        update_metric_value(metric, event)
      end
    end
  end

  def should_update_metric(metric, event)
    # Check if event matches metric dimensions
    metric[:dimensions].all? do |dimension|
      event[:dimensions][dimension] == metric[:dimensions][dimension]
    end
  end

  def update_metric_value(metric, event)
    metric_key = generate_metric_key(metric[:name], event[:dimensions])
    
    case metric[:type]
    when 'counter'
      increment_counter(metric_key, event[:data][:value] || 1)
    when 'gauge'
      set_gauge(metric_key, event[:data][:value])
    when 'histogram'
      add_to_histogram(metric_key, event[:data][:value])
    end
  end

  def generate_metric_key(metric_name, dimensions)
    dimension_str = dimensions.map { |k, v| "#{k}:#{v}" }.join(':')
    "metric:#{metric_name}:#{dimension_str}"
  end

  def generate_aggregation_key(aggregation_name, group_values)
    group_str = group_values.map { |k, v| "#{k}:#{v}" }.join(':')
    "aggregation:#{aggregation_name}:#{group_str}"
  end

  def increment_counter(key, value)
    @redis.incrby(key, value)
    @redis.expire(key, 86400) # 24 hours
  end

  def set_gauge(key, value)
    @redis.set(key, value)
    @redis.expire(key, 86400) # 24 hours
  end

  def add_to_histogram(key, value)
    @redis.lpush(key, value)
    @redis.ltrim(key, 0, 999) # Keep last 1000 values
    @redis.expire(key, 86400) # 24 hours
  end

  def get_counter_value(key)
    @redis.get(key).to_i
  end

  def get_gauge_value(key)
    @redis.get(key).to_f
  end

  def get_histogram_value(key)
    values = @redis.lrange(key, 0, -1).map(&:to_f)
    return 0 if values.empty?

    {
      count: values.length,
      sum: values.sum,
      average: values.sum / values.length,
      min: values.min,
      max: values.max
    }
  end

  def check_alerts(event)
    @alerts.each do |alert_name, alert|
      next unless alert[:status] == 'active'

      metric_value = get_metric_value(alert[:metric], alert[:dimensions])
      next unless metric_value

      if alert_condition_met(alert, metric_value)
        trigger_alert(alert, metric_value)
      end
    end
  end

  def alert_condition_met(alert, value)
    case alert[:condition]
    when '>'
      value > alert[:threshold]
    when '<'
      value < alert[:threshold]
    when '>='
      value >= alert[:threshold]
    when '<='
      value <= alert[:threshold]
    when '=='
      value == alert[:threshold]
    when '!='
      value != alert[:threshold]
    else
      false
    end
  end

  def trigger_alert(alert, value)
    alert_event = {
      id: SecureRandom.uuid,
      alert_name: alert[:name],
      metric: alert[:metric],
      threshold: alert[:threshold],
      actual_value: value,
      timestamp: Time.now.iso8601
    }

    # Store alert event
    @redis.lpush('analytics_alerts', alert_event.to_json)
    
    # Send notification (implement based on your notification system)
    send_alert_notification(alert_event)
    
    # Set cooldown
    cooldown_key = "alert_cooldown:#{alert[:name]}"
    @redis.setex(cooldown_key, alert[:cooldown], '1')
  end

  def send_alert_notification(alert_event)
    # Implementation for sending alert notifications
    Rails.logger.warn "Alert triggered: #{alert_event.to_json}"
  end

  def get_all_metrics
    @metrics.values.map do |metric|
      {
        name: metric[:name],
        type: metric[:type],
        current_value: get_metric_value(metric[:name])
      }
    end
  end

  def get_all_aggregations
    @aggregations.values.map do |aggregation|
      {
        name: aggregation[:name],
        function: aggregation[:function],
        current_value: get_aggregation_value(aggregation[:name])
      }
    end
  end

  def get_all_alerts
    @alerts.values.map do |alert|
      {
        name: alert[:name],
        status: alert[:status],
        metric: alert[:metric],
        threshold: alert[:threshold]
      }
    end
  end

  def get_recent_events(limit)
    events = @redis.lrange('analytics_events', 0, limit - 1)
    events.map { |event_json| JSON.parse(event_json) }
  end

  def get_system_stats
    {
      total_events: @redis.llen('analytics_events'),
      total_metrics: @metrics.length,
      total_aggregations: @aggregations.length,
      total_alerts: @alerts.length,
      active_alerts: @alerts.count { |_, alert| alert[:status] == 'active' }
    }
  end

  def get_time_series_data(key, start_time, end_time)
    # Implementation for getting time series data
    []
  end
end
```

## 🎯 **Configuration Management**

### Real-Time Processing Configuration

```ruby
# config/real_time_features.tsk
[real_time]
enabled: @env("REAL_TIME_ENABLED", "true")
processing_mode: @env("PROCESSING_MODE", "streaming")
buffer_size: @env("BUFFER_SIZE", "1000")
flush_interval: @env("FLUSH_INTERVAL", "5")
parallel_workers: @env("PARALLEL_WORKERS", "4")
max_memory_usage: @env("MAX_MEMORY_USAGE", "1GB")

[streaming]
enabled: @env("STREAMING_ENABLED", "true")
source_type: @env("STREAM_SOURCE", "kafka")
batch_size: @env("STREAM_BATCH_SIZE", "100")
processing_timeout: @env("STREAM_PROCESSING_TIMEOUT", "30")
backpressure_handling: @env("BACKPRESSURE_HANDLING", "true")
watermark_enabled: @env("WATERMARK_ENABLED", "true")

[websockets]
enabled: @env("WEBSOCKETS_ENABLED", "true")
port: @env("WEBSOCKET_PORT", "8080")
max_connections: @env("MAX_WEBSOCKET_CONNECTIONS", "10000")
heartbeat_interval: @env("WEBSOCKET_HEARTBEAT", "30")
compression_enabled: @env("WEBSOCKET_COMPRESSION", "true")

[analytics]
enabled: @env("REAL_TIME_ANALYTICS_ENABLED", "true")
metrics_retention: @env("METRICS_RETENTION", "30d")
aggregation_enabled: @env("AGGREGATION_ENABLED", "true")
alerting_enabled: @env("ALERTING_ENABLED", "true")
dashboard_enabled: @env("DASHBOARD_ENABLED", "true")

[performance]
optimization_enabled: @env("REAL_TIME_OPTIMIZATION_ENABLED", "true")
caching_enabled: @env("REAL_TIME_CACHING_ENABLED", "true")
monitoring_enabled: @env("REAL_TIME_MONITORING_ENABLED", "true")
```

## 🎯 **Summary**

This comprehensive guide covers real-time processing with TuskLang and Ruby, including:

- **Stream Processing**: Real-time data processing with multiple sources and processors
- **WebSocket Server**: Real-time bidirectional communication
- **Real-Time Analytics**: Streaming analytics with metrics, aggregations, and alerts
- **Configuration Management**: Enterprise-grade real-time processing configuration
- **Performance Optimization**: Caching, monitoring, and optimization features

The real-time processing features with TuskLang provide a robust foundation for building systems that can process and respond to data as it happens, enabling real-time analytics, live dashboards, and instant notifications. 
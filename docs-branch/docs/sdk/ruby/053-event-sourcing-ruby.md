# Event Sourcing with TuskLang and Ruby

## 📡 **Build Systems That Remember Everything**

TuskLang enables sophisticated event sourcing for Ruby applications, providing event stores, aggregates, projections, and event-driven architecture. Build systems that maintain complete audit trails and can reconstruct any state at any point in time.

## 🚀 **Quick Start: Event Store**

### Basic Event Sourcing Configuration

```ruby
# config/event_sourcing.tsk
[event_sourcing]
enabled: @env("EVENT_SOURCING_ENABLED", "true")
event_store_type: @env("EVENT_STORE_TYPE", "postgresql") # postgresql, redis, mongodb
snapshot_frequency: @env("SNAPSHOT_FREQUENCY", "100")
event_serialization: @env("EVENT_SERIALIZATION", "json") # json, msgpack, protobuf

[event_store]
host: @env("EVENT_STORE_HOST", "localhost")
port: @env("EVENT_STORE_PORT", "5432")
database: @env("EVENT_STORE_DATABASE", "event_store")
username: @env("EVENT_STORE_USERNAME", "postgres")
password: @env.secure("EVENT_STORE_PASSWORD")

[projections]
enabled: @env("PROJECTIONS_ENABLED", "true")
real_time: @env("REAL_TIME_PROJECTIONS", "true")
batch_size: @env("PROJECTION_BATCH_SIZE", "1000")
parallel_processing: @env("PARALLEL_PROJECTIONS", "true")

[aggregates]
snapshot_enabled: @env("AGGREGATE_SNAPSHOTS_ENABLED", "true")
snapshot_retention: @env("SNAPSHOT_RETENTION_PERIOD", "30d")
optimistic_concurrency: @env("OPTIMISTIC_CONCURRENCY", "true")
```

### Event Store Implementation

```ruby
# lib/event_store.rb
require 'tusk'
require 'redis'
require 'json'
require 'securerandom'

class EventStore
  def initialize(config_path = 'config/event_sourcing.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    setup_event_store
  end

  def append_events(stream_id, events, expected_version = nil)
    return { success: false, error: 'Event sourcing disabled' } unless @config['event_sourcing']['enabled'] == 'true'

    # Validate expected version for optimistic concurrency
    if expected_version && @config['aggregates']['optimistic_concurrency'] == 'true'
      current_version = get_stream_version(stream_id)
      if current_version != expected_version
        return {
          success: false,
          error: "Concurrency conflict: expected version #{expected_version}, got #{current_version}"
        }
      end
    end

    # Prepare events for storage
    prepared_events = events.map do |event|
      prepare_event_for_storage(event, stream_id)
    end

    # Store events atomically
    begin
      store_events(stream_id, prepared_events)
      update_stream_metadata(stream_id, prepared_events.length)
      
      # Trigger projections
      trigger_projections(prepared_events) if @config['projections']['enabled'] == 'true'
      
      # Create snapshot if needed
      create_snapshot_if_needed(stream_id) if @config['aggregates']['snapshot_enabled'] == 'true'

      {
        success: true,
        stream_id: stream_id,
        events_count: prepared_events.length,
        next_expected_version: get_stream_version(stream_id)
      }
    rescue => e
      {
        success: false,
        error: "Failed to append events: #{e.message}"
      }
    end
  end

  def get_events(stream_id, from_version = 0, to_version = nil)
    events = []
    current_version = from_version

    loop do
      batch = get_events_batch(stream_id, current_version, 100)
      break if batch.empty?

      events.concat(batch)
      current_version = batch.last[:version] + 1

      break if to_version && current_version > to_version
    end

    events
  end

  def get_stream_metadata(stream_id)
    metadata = @redis.hgetall("stream_metadata:#{stream_id}")
    return nil if metadata.empty?

    {
      stream_id: stream_id,
      version: metadata['version'].to_i,
      event_count: metadata['event_count'].to_i,
      created_at: metadata['created_at'],
      last_updated: metadata['last_updated']
    }
  end

  def create_snapshot(stream_id, aggregate_state, version)
    snapshot = {
      id: SecureRandom.uuid,
      stream_id: stream_id,
      version: version,
      state: aggregate_state,
      created_at: Time.now.iso8601
    }

    @redis.hset("snapshots:#{stream_id}", version.to_s, snapshot.to_json)
    snapshot
  end

  def get_latest_snapshot(stream_id)
    snapshots = @redis.hgetall("snapshots:#{stream_id}")
    return nil if snapshots.empty?

    latest_version = snapshots.keys.map(&:to_i).max
    snapshot_data = snapshots[latest_version.to_s]
    
    JSON.parse(snapshot_data) if snapshot_data
  end

  def subscribe_to_events(event_types = nil, &block)
    subscription_id = SecureRandom.uuid
    
    subscription = {
      id: subscription_id,
      event_types: event_types,
      callback: block,
      created_at: Time.now.iso8601,
      active: true
    }

    @redis.hset('event_subscriptions', subscription_id, subscription.to_json)
    subscription
  end

  def unsubscribe_from_events(subscription_id)
    @redis.hdel('event_subscriptions', subscription_id)
  end

  def get_event_statistics
    {
      total_events: get_total_event_count,
      total_streams: get_total_stream_count,
      events_by_type: get_events_by_type,
      recent_events: get_recent_events(100)
    }
  end

  def replay_events(from_timestamp = nil, to_timestamp = nil)
    events = get_events_in_timerange(from_timestamp, to_timestamp)
    
    replay_result = {
      events_processed: 0,
      errors: [],
      start_time: Time.now.iso8601
    }

    events.each do |event|
      begin
        process_event_for_replay(event)
        replay_result[:events_processed] += 1
      rescue => e
        replay_result[:errors] << {
          event_id: event[:id],
          error: e.message
        }
      end
    end

    replay_result[:end_time] = Time.now.iso8601
    replay_result[:duration] = Time.parse(replay_result[:end_time]) - Time.parse(replay_result[:start_time])
    replay_result
  end

  private

  def setup_event_store
    # Initialize event store tables/collections if needed
    case @config['event_sourcing']['event_store_type']
    when 'postgresql'
      setup_postgresql_event_store
    when 'redis'
      setup_redis_event_store
    when 'mongodb'
      setup_mongodb_event_store
    end
  end

  def setup_postgresql_event_store
    # Implementation for PostgreSQL event store setup
  end

  def setup_redis_event_store
    # Implementation for Redis event store setup
  end

  def setup_mongodb_event_store
    # Implementation for MongoDB event store setup
  end

  def prepare_event_for_storage(event, stream_id)
    {
      id: SecureRandom.uuid,
      stream_id: stream_id,
      type: event[:type],
      data: event[:data],
      metadata: event[:metadata] || {},
      version: get_next_version(stream_id),
      timestamp: Time.now.iso8601,
      correlation_id: event[:correlation_id] || SecureRandom.uuid,
      causation_id: event[:causation_id]
    }
  end

  def store_events(stream_id, events)
    events.each do |event|
      event_key = "event:#{stream_id}:#{event[:version]}"
      @redis.setex(event_key, 86400 * 365, event.to_json) # 1 year retention
      
      # Store event in global event log
      @redis.lpush('event_log', event.to_json)
      @redis.ltrim('event_log', 0, 999999) # Keep last 1M events
    end
  end

  def update_stream_metadata(stream_id, events_count)
    metadata_key = "stream_metadata:#{stream_id}"
    current_metadata = @redis.hgetall(metadata_key)

    if current_metadata.empty?
      @redis.hmset(metadata_key,
        'version', events_count - 1,
        'event_count', events_count,
        'created_at', Time.now.iso8601,
        'last_updated', Time.now.iso8601
      )
    else
      current_version = current_metadata['version'].to_i
      current_event_count = current_metadata['event_count'].to_i
      
      @redis.hmset(metadata_key,
        'version', current_version + events_count,
        'event_count', current_event_count + events_count,
        'last_updated', Time.now.iso8601
      )
    end
  end

  def get_stream_version(stream_id)
    metadata = get_stream_metadata(stream_id)
    metadata ? metadata[:version] : -1
  end

  def get_next_version(stream_id)
    get_stream_version(stream_id) + 1
  end

  def get_events_batch(stream_id, from_version, limit)
    events = []
    current_version = from_version

    limit.times do
      event_key = "event:#{stream_id}:#{current_version}"
      event_data = @redis.get(event_key)
      
      break unless event_data
      
      events << JSON.parse(event_data)
      current_version += 1
    end

    events
  end

  def trigger_projections(events)
    subscriptions = @redis.hgetall('event_subscriptions')
    
    subscriptions.each do |subscription_id, subscription_data|
      subscription = JSON.parse(subscription_data)
      next unless subscription['active']

      events.each do |event|
        if should_process_event_for_subscription(event, subscription)
          begin
            subscription['callback'].call(event)
          rescue => e
            Rails.logger.error "Projection error: #{e.message}"
          end
        end
      end
    end
  end

  def should_process_event_for_subscription(event, subscription)
    return true unless subscription['event_types']
    subscription['event_types'].include?(event[:type])
  end

  def create_snapshot_if_needed(stream_id)
    frequency = @config['event_sourcing']['snapshot_frequency'].to_i
    metadata = get_stream_metadata(stream_id)
    
    return unless metadata && metadata[:version] % frequency == 0

    # This would typically reconstruct the aggregate state
    # For now, we'll create a placeholder snapshot
    create_snapshot(stream_id, { placeholder: true }, metadata[:version])
  end

  def get_total_event_count
    @redis.llen('event_log')
  end

  def get_total_stream_count
    @redis.keys('stream_metadata:*').length
  end

  def get_events_by_type
    events = @redis.lrange('event_log', 0, 9999)
    event_types = {}

    events.each do |event_data|
      event = JSON.parse(event_data)
      event_types[event['type']] ||= 0
      event_types[event['type']] += 1
    end

    event_types
  end

  def get_recent_events(limit)
    events = @redis.lrange('event_log', 0, limit - 1)
    events.map { |event_data| JSON.parse(event_data) }
  end

  def get_events_in_timerange(from_timestamp, to_timestamp)
    events = @redis.lrange('event_log', 0, -1)
    filtered_events = []

    events.each do |event_data|
      event = JSON.parse(event_data)
      event_time = Time.parse(event['timestamp'])
      
      if from_timestamp && event_time < Time.parse(from_timestamp)
        next
      end
      
      if to_timestamp && event_time > Time.parse(to_timestamp)
        next
      end
      
      filtered_events << event
    end

    filtered_events
  end

  def process_event_for_replay(event)
    # Implementation for event replay processing
    Rails.logger.info "Replaying event: #{event['id']}"
  end
end
```

## 🏗️ **Aggregate Implementation**

### Event-Sourced Aggregates

```ruby
# lib/aggregate.rb
require 'tusk'
require 'json'

class Aggregate
  def initialize(id, event_store)
    @id = id
    @event_store = event_store
    @version = 0
    @uncommitted_events = []
    @state = initial_state
  end

  def load_from_history
    # Load from snapshot if available
    snapshot = @event_store.get_latest_snapshot(@id)
    if snapshot
      @state = snapshot['state']
      @version = snapshot['version']
    end

    # Load remaining events
    events = @event_store.get_events(@id, @version + 1)
    events.each do |event|
      apply_event(event)
      @version = event['version']
    end

    self
  end

  def save
    return { success: true, events_count: 0 } if @uncommitted_events.empty?

    result = @event_store.append_events(@id, @uncommitted_events, @version)
    
    if result[:success]
      @version = result[:next_expected_version]
      @uncommitted_events.clear
    end

    result
  end

  def apply_event(event)
    handler_method = "apply_#{event['type'].underscore}"
    
    if respond_to?(handler_method, true)
      send(handler_method, event['data'])
    else
      Rails.logger.warn "No handler found for event: #{event['type']}"
    end
  end

  def record_event(event_type, data, metadata = {})
    event = {
      type: event_type,
      data: data,
      metadata: metadata,
      correlation_id: get_correlation_id,
      causation_id: get_causation_id
    }

    @uncommitted_events << event
    apply_event(event)
  end

  def get_state
    @state.dup
  end

  def get_version
    @version
  end

  def get_uncommitted_events
    @uncommitted_events.dup
  end

  protected

  def initial_state
    {}
  end

  def get_correlation_id
    Thread.current[:correlation_id] || SecureRandom.uuid
  end

  def get_causation_id
    Thread.current[:causation_id] || SecureRandom.uuid
  end
end

# Example User Aggregate
class UserAggregate < Aggregate
  def initialize(id, event_store)
    super(id, event_store)
  end

  def create_user(email, name)
    return { success: false, error: 'User already exists' } if @state[:email]

    record_event('UserCreated', {
      email: email,
      name: name,
      created_at: Time.now.iso8601
    })

    { success: true }
  end

  def update_profile(updates)
    return { success: false, error: 'User does not exist' } unless @state[:email]

    record_event('UserProfileUpdated', {
      updates: updates,
      updated_at: Time.now.iso8601
    })

    { success: true }
  end

  def deactivate_user(reason)
    return { success: false, error: 'User does not exist' } unless @state[:email]
    return { success: false, error: 'User already deactivated' } if @state[:deactivated]

    record_event('UserDeactivated', {
      reason: reason,
      deactivated_at: Time.now.iso8601
    })

    { success: true }
  end

  private

  def apply_user_created(data)
    @state.merge!(
      email: data['email'],
      name: data['name'],
      created_at: data['created_at'],
      active: true
    )
  end

  def apply_user_profile_updated(data)
    @state.merge!(data['updates'])
    @state[:updated_at] = data['updated_at']
  end

  def apply_user_deactivated(data)
    @state[:deactivated] = true
    @state[:deactivated_at] = data['deactivated_at']
    @state[:deactivation_reason] = data['reason']
  end
end

# Example Order Aggregate
class OrderAggregate < Aggregate
  def initialize(id, event_store)
    super(id, event_store)
  end

  def create_order(user_id, items)
    record_event('OrderCreated', {
      user_id: user_id,
      items: items,
      total: calculate_total(items),
      created_at: Time.now.iso8601
    })

    { success: true }
  end

  def add_item(item)
    return { success: false, error: 'Order cannot be modified' } unless can_modify?

    record_event('ItemAdded', {
      item: item,
      added_at: Time.now.iso8601
    })

    { success: true }
  end

  def remove_item(item_id)
    return { success: false, error: 'Order cannot be modified' } unless can_modify?

    record_event('ItemRemoved', {
      item_id: item_id,
      removed_at: Time.now.iso8601
    })

    { success: true }
  end

  def confirm_order
    return { success: false, error: 'Order already confirmed' } if @state[:confirmed]
    return { success: false, error: 'Order has no items' } if @state[:items]&.empty?

    record_event('OrderConfirmed', {
      confirmed_at: Time.now.iso8601
    })

    { success: true }
  end

  def cancel_order(reason)
    return { success: false, error: 'Order already cancelled' } if @state[:cancelled]
    return { success: false, error: 'Order already shipped' } if @state[:shipped]

    record_event('OrderCancelled', {
      reason: reason,
      cancelled_at: Time.now.iso8601
    })

    { success: true }
  end

  private

  def can_modify?
    !@state[:confirmed] && !@state[:cancelled] && !@state[:shipped]
  end

  def calculate_total(items)
    items.sum { |item| item['price'] * item['quantity'] }
  end

  def apply_order_created(data)
    @state.merge!(
      user_id: data['user_id'],
      items: data['items'],
      total: data['total'],
      created_at: data['created_at'],
      status: 'pending'
    )
  end

  def apply_item_added(data)
    @state[:items] ||= []
    @state[:items] << data['item']
    @state[:total] = calculate_total(@state[:items])
    @state[:updated_at] = data['added_at']
  end

  def apply_item_removed(data)
    @state[:items].reject! { |item| item['id'] == data['item_id'] }
    @state[:total] = calculate_total(@state[:items])
    @state[:updated_at] = data['removed_at']
  end

  def apply_order_confirmed(data)
    @state[:confirmed] = true
    @state[:confirmed_at] = data['confirmed_at']
    @state[:status] = 'confirmed'
  end

  def apply_order_cancelled(data)
    @state[:cancelled] = true
    @state[:cancelled_at] = data['cancelled_at']
    @state[:cancellation_reason] = data['reason']
    @state[:status] = 'cancelled'
  end
end
```

## 📊 **Projections**

### Event Projections

```ruby
# lib/projection.rb
require 'tusk'
require 'redis'
require 'json'

class Projection
  def initialize(name, event_store, config_path = 'config/event_sourcing.tsk')
    @name = name
    @event_store = event_store
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    @state = {}
  end

  def process_event(event)
    handler_method = "handle_#{event['type'].underscore}"
    
    if respond_to?(handler_method, true)
      send(handler_method, event)
      update_projection_state
    end
  end

  def get_state
    @state
  end

  def reset
    @state = {}
    @redis.del("projection_state:#{@name}")
  end

  def rebuild
    reset
    events = @event_store.get_events_in_timerange(nil, nil)
    
    events.each do |event|
      process_event(event)
    end
  end

  protected

  def update_projection_state
    @redis.set("projection_state:#{@name}", @state.to_json)
  end

  def load_projection_state
    state_data = @redis.get("projection_state:#{@name}")
    @state = state_data ? JSON.parse(state_data) : {}
  end
end

# Example User Projection
class UserProjection < Projection
  def initialize(event_store, config_path)
    super('user_projection', event_store, config_path)
    load_projection_state
  end

  private

  def handle_user_created(event)
    user_id = event['stream_id']
    data = event['data']
    
    @state[user_id] = {
      id: user_id,
      email: data['email'],
      name: data['name'],
      created_at: data['created_at'],
      active: true,
      updated_at: data['created_at']
    }
  end

  def handle_user_profile_updated(event)
    user_id = event['stream_id']
    data = event['data']
    
    return unless @state[user_id]
    
    @state[user_id].merge!(data['updates'])
    @state[user_id][:updated_at] = data['updated_at']
  end

  def handle_user_deactivated(event)
    user_id = event['stream_id']
    data = event['data']
    
    return unless @state[user_id]
    
    @state[user_id].merge!(
      deactivated: true,
      deactivated_at: data['deactivated_at'],
      deactivation_reason: data['reason'],
      active: false,
      updated_at: data['deactivated_at']
    )
  end
end

# Example Order Projection
class OrderProjection < Projection
  def initialize(event_store, config_path)
    super('order_projection', event_store, config_path)
    load_projection_state
  end

  def get_user_orders(user_id)
    @state.values.select { |order| order['user_id'] == user_id }
  end

  def get_active_orders
    @state.values.select { |order| order['status'] == 'pending' }
  end

  def get_order_statistics
    total_orders = @state.length
    confirmed_orders = @state.values.count { |order| order['confirmed'] }
    cancelled_orders = @state.values.count { |order| order['cancelled'] }
    total_revenue = @state.values.sum { |order| order['total'] || 0 }

    {
      total_orders: total_orders,
      confirmed_orders: confirmed_orders,
      cancelled_orders: cancelled_orders,
      total_revenue: total_revenue,
      conversion_rate: total_orders > 0 ? (confirmed_orders.to_f / total_orders * 100).round(2) : 0
    }
  end

  private

  def handle_order_created(event)
    order_id = event['stream_id']
    data = event['data']
    
    @state[order_id] = {
      id: order_id,
      user_id: data['user_id'],
      items: data['items'],
      total: data['total'],
      created_at: data['created_at'],
      status: 'pending',
      updated_at: data['created_at']
    }
  end

  def handle_item_added(event)
    order_id = event['stream_id']
    data = event['data']
    
    return unless @state[order_id]
    
    @state[order_id][:items] ||= []
    @state[order_id][:items] << data['item']
    @state[order_id][:total] = calculate_total(@state[order_id][:items])
    @state[order_id][:updated_at] = data['added_at']
  end

  def handle_item_removed(event)
    order_id = event['stream_id']
    data = event['data']
    
    return unless @state[order_id]
    
    @state[order_id][:items].reject! { |item| item['id'] == data['item_id'] }
    @state[order_id][:total] = calculate_total(@state[order_id][:items])
    @state[order_id][:updated_at] = data['removed_at']
  end

  def handle_order_confirmed(event)
    order_id = event['stream_id']
    data = event['data']
    
    return unless @state[order_id]
    
    @state[order_id].merge!(
      confirmed: true,
      confirmed_at: data['confirmed_at'],
      status: 'confirmed',
      updated_at: data['confirmed_at']
    )
  end

  def handle_order_cancelled(event)
    order_id = event['stream_id']
    data = event['data']
    
    return unless @state[order_id]
    
    @state[order_id].merge!(
      cancelled: true,
      cancelled_at: data['cancelled_at'],
      cancellation_reason: data['reason'],
      status: 'cancelled',
      updated_at: data['cancelled_at']
    )
  end

  def calculate_total(items)
    items.sum { |item| item['price'] * item['quantity'] }
  end
end
```

## 🔄 **Event Handlers**

### Event Processing Pipeline

```ruby
# lib/event_handler.rb
require 'tusk'
require 'redis'
require 'json'

class EventHandler
  def initialize(config_path = 'config/event_sourcing.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    @handlers = {}
    @middleware = []
  end

  def register_handler(event_type, handler)
    @handlers[event_type] ||= []
    @handlers[event_type] << handler
  end

  def add_middleware(middleware)
    @middleware << middleware
  end

  def process_event(event)
    return unless @config['event_sourcing']['enabled'] == 'true'

    # Apply middleware
    processed_event = apply_middleware(event)
    return unless processed_event

    # Get handlers for this event type
    handlers = @handlers[event['type']] || []
    
    # Process with all registered handlers
    results = handlers.map do |handler|
      process_with_handler(handler, processed_event)
    end

    {
      event_id: event['id'],
      event_type: event['type'],
      handlers_processed: handlers.length,
      results: results
    }
  end

  def process_events_batch(events)
    return { processed: 0, errors: [] } if events.empty?

    batch_size = @config['projections']['batch_size'].to_i
    processed = 0
    errors = []

    events.each_slice(batch_size) do |batch|
      batch.each do |event|
        begin
          process_event(event)
          processed += 1
        rescue => e
          errors << {
            event_id: event['id'],
            error: e.message
          }
        end
      end
    end

    {
      processed: processed,
      errors: errors,
      total_events: events.length
    }
  end

  def get_handler_statistics
    stats = {}
    
    @handlers.each do |event_type, handlers|
      stats[event_type] = {
        handler_count: handlers.length,
        handler_types: handlers.map(&:class).map(&:name)
      }
    end

    stats
  end

  private

  def apply_middleware(event)
    processed_event = event.dup

    @middleware.each do |middleware|
      begin
        processed_event = middleware.process(processed_event)
        break unless processed_event
      rescue => e
        Rails.logger.error "Middleware error: #{e.message}"
        return nil
      end
    end

    processed_event
  end

  def process_with_handler(handler, event)
    start_time = Time.now

    begin
      if handler.respond_to?(:call)
        result = handler.call(event)
      elsif handler.respond_to?(:process_event)
        result = handler.process_event(event)
      else
        raise "Handler does not respond to call or process_event"
      end

      {
        handler: handler.class.name,
        success: true,
        result: result,
        duration: Time.now - start_time
      }
    rescue => e
      {
        handler: handler.class.name,
        success: false,
        error: e.message,
        duration: Time.now - start_time
      }
    end
  end
end

# Example Email Notification Handler
class EmailNotificationHandler
  def process_event(event)
    case event['type']
    when 'UserCreated'
      send_welcome_email(event['data'])
    when 'OrderConfirmed'
      send_order_confirmation_email(event['data'])
    when 'OrderCancelled'
      send_order_cancellation_email(event['data'])
    end
  end

  private

  def send_welcome_email(user_data)
    # Implementation for sending welcome email
    Rails.logger.info "Sending welcome email to #{user_data['email']}"
  end

  def send_order_confirmation_email(order_data)
    # Implementation for sending order confirmation email
    Rails.logger.info "Sending order confirmation email"
  end

  def send_order_cancellation_email(order_data)
    # Implementation for sending order cancellation email
    Rails.logger.info "Sending order cancellation email"
  end
end

# Example Audit Log Handler
class AuditLogHandler
  def process_event(event)
    audit_entry = {
      event_id: event['id'],
      event_type: event['type'],
      stream_id: event['stream_id'],
      timestamp: event['timestamp'],
      data: event['data'],
      metadata: event['metadata']
    }

    store_audit_entry(audit_entry)
  end

  private

  def store_audit_entry(audit_entry)
    # Implementation for storing audit entry
    Rails.logger.info "Audit log: #{audit_entry.to_json}"
  end
end
```

## 🎯 **Configuration Management**

### Event Sourcing Configuration

```ruby
# config/event_sourcing_features.tsk
[event_sourcing]
enabled: @env("EVENT_SOURCING_ENABLED", "true")
event_store_type: @env("EVENT_STORE_TYPE", "postgresql")
snapshot_frequency: @env("SNAPSHOT_FREQUENCY", "100")
event_serialization: @env("EVENT_SERIALIZATION", "json")

[event_store]
host: @env("EVENT_STORE_HOST", "localhost")
port: @env("EVENT_STORE_PORT", "5432")
database: @env("EVENT_STORE_DATABASE", "event_store")
username: @env("EVENT_STORE_USERNAME", "postgres")
password: @env.secure("EVENT_STORE_PASSWORD")
connection_pool: @env("EVENT_STORE_CONNECTION_POOL", "10")

[projections]
enabled: @env("PROJECTIONS_ENABLED", "true")
real_time: @env("REAL_TIME_PROJECTIONS", "true")
batch_size: @env("PROJECTION_BATCH_SIZE", "1000")
parallel_processing: @env("PARALLEL_PROJECTIONS", "true")
retry_attempts: @env("PROJECTION_RETRY_ATTEMPTS", "3")

[aggregates]
snapshot_enabled: @env("AGGREGATE_SNAPSHOTS_ENABLED", "true")
snapshot_retention: @env("SNAPSHOT_RETENTION_PERIOD", "30d")
optimistic_concurrency: @env("OPTIMISTIC_CONCURRENCY", "true")
max_events_per_aggregate: @env("MAX_EVENTS_PER_AGGREGATE", "10000")

[handlers]
enabled: @env("EVENT_HANDLERS_ENABLED", "true")
async_processing: @env("ASYNC_EVENT_PROCESSING", "true")
error_handling: @env("EVENT_HANDLER_ERROR_HANDLING", "true")
retry_failed_events: @env("RETRY_FAILED_EVENTS", "true")

[monitoring]
metrics_enabled: @env("EVENT_SOURCING_METRICS_ENABLED", "true")
event_tracking: @env("EVENT_TRACKING_ENABLED", "true")
performance_monitoring: @env("EVENT_PERFORMANCE_MONITORING", "true")
```

## 🎯 **Summary**

This comprehensive guide covers event sourcing with TuskLang and Ruby, including:

- **Event Store**: Complete event storage and retrieval system
- **Aggregates**: Event-sourced aggregates with state reconstruction
- **Projections**: Real-time and batch event projections
- **Event Handlers**: Event processing pipeline with middleware
- **Configuration Management**: Enterprise-grade event sourcing configuration
- **Snapshot Management**: Performance optimization through snapshots
- **Event Replay**: Complete system state reconstruction capabilities

The event sourcing features with TuskLang provide a robust foundation for building systems that maintain complete audit trails, enable temporal queries, and support complex event-driven architectures while ensuring data consistency and scalability. 
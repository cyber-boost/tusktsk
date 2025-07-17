# CQRS with TuskLang and Ruby

## ⚡ **Separate Commands from Queries for Maximum Performance**

TuskLang enables sophisticated CQRS (Command Query Responsibility Segregation) for Ruby applications, providing separate command and query handlers, optimized read models, and scalable write models. Build systems that can handle massive read and write loads independently.

## 🚀 **Quick Start: CQRS Setup**

### Basic CQRS Configuration

```ruby
# config/cqrs.tsk
[cqrs]
enabled: @env("CQRS_ENABLED", "true")
command_bus: @env("COMMAND_BUS_TYPE", "synchronous") # synchronous, asynchronous, distributed
query_bus: @env("QUERY_BUS_TYPE", "synchronous") # synchronous, asynchronous, distributed
event_sourcing: @env("CQRS_EVENT_SOURCING", "true")

[commands]
validation_enabled: @env("COMMAND_VALIDATION_ENABLED", "true")
authorization_enabled: @env("COMMAND_AUTHORIZATION_ENABLED", "true")
logging_enabled: @env("COMMAND_LOGGING_ENABLED", "true")
retry_attempts: @env("COMMAND_RETRY_ATTEMPTS", "3")

[queries]
caching_enabled: @env("QUERY_CACHING_ENABLED", "true")
cache_ttl: @env("QUERY_CACHE_TTL", "300")
authorization_enabled: @env("QUERY_AUTHORIZATION_ENABLED", "true")
pagination_enabled: @env("QUERY_PAGINATION_ENABLED", "true")

[read_models]
database: @env("READ_MODEL_DATABASE", "postgresql")
optimization_enabled: @env("READ_MODEL_OPTIMIZATION_ENABLED", "true")
denormalization_enabled: @env("READ_MODEL_DENORMALIZATION_ENABLED", "true")
```

### Command Bus Implementation

```ruby
# lib/command_bus.rb
require 'tusk'
require 'redis'
require 'json'
require 'securerandom'

class CommandBus
  def initialize(config_path = 'config/cqrs.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    @handlers = {}
    @middleware = []
    setup_command_bus
  end

  def register_handler(command_class, handler)
    @handlers[command_class] = handler
  end

  def add_middleware(middleware)
    @middleware << middleware
  end

  def execute(command)
    return { success: false, error: 'CQRS disabled' } unless @config['cqrs']['enabled'] == 'true'

    command_id = SecureRandom.uuid
    start_time = Time.now

    begin
      # Apply middleware
      processed_command = apply_middleware(command, command_id)
      return processed_command unless processed_command[:success]

      # Validate command
      validation_result = validate_command(command)
      unless validation_result[:valid]
        return {
          success: false,
          error: 'Command validation failed',
          details: validation_result[:errors],
          command_id: command_id
        }
      end

      # Authorize command
      authorization_result = authorize_command(command)
      unless authorization_result[:authorized]
        return {
          success: false,
          error: 'Command authorization failed',
          details: authorization_result[:reason],
          command_id: command_id
        }
      end

      # Execute command
      handler = get_handler(command.class)
      unless handler
        return {
          success: false,
          error: "No handler registered for command: #{command.class}",
          command_id: command_id
        }
      end

      result = execute_with_handler(handler, command, command_id)
      
      # Log command execution
      log_command_execution(command, command_id, result, Time.now - start_time)
      
      result
    rescue => e
      error_result = {
        success: false,
        error: e.message,
        command_id: command_id,
        duration: Time.now - start_time
      }
      
      log_command_error(command, command_id, e)
      error_result
    end
  end

  def execute_batch(commands)
    return { success: false, error: 'No commands provided' } if commands.empty?

    results = []
    batch_id = SecureRandom.uuid

    commands.each_with_index do |command, index|
      result = execute(command)
      result[:batch_id] = batch_id
      result[:batch_index] = index
      results << result
    end

    {
      batch_id: batch_id,
      total_commands: commands.length,
      successful: results.count { |r| r[:success] },
      failed: results.count { |r| !r[:success] },
      results: results
    }
  end

  def get_command_statistics
    {
      total_commands: get_total_command_count,
      successful_commands: get_successful_command_count,
      failed_commands: get_failed_command_count,
      average_execution_time: get_average_execution_time,
      commands_by_type: get_commands_by_type
    }
  end

  private

  def setup_command_bus
    case @config['cqrs']['command_bus']
    when 'asynchronous'
      setup_asynchronous_bus
    when 'distributed'
      setup_distributed_bus
    end
  end

  def setup_asynchronous_bus
    # Implementation for asynchronous command bus
  end

  def setup_distributed_bus
    # Implementation for distributed command bus
  end

  def apply_middleware(command, command_id)
    processed_command = command.dup

    @middleware.each do |middleware|
      begin
        result = middleware.process(processed_command, command_id)
        return result unless result[:success]
        processed_command = result[:command]
      rescue => e
        return {
          success: false,
          error: "Middleware error: #{e.message}",
          command_id: command_id
        }
      end
    end

    { success: true, command: processed_command }
  end

  def validate_command(command)
    return { valid: true } unless @config['commands']['validation_enabled'] == 'true'

    if command.respond_to?(:valid?)
      { valid: command.valid?, errors: command.errors.full_messages }
    else
      { valid: true }
    end
  end

  def authorize_command(command)
    return { authorized: true } unless @config['commands']['authorization_enabled'] == 'true'

    if command.respond_to?(:authorize)
      command.authorize
    else
      { authorized: true }
    end
  end

  def get_handler(command_class)
    @handlers[command_class]
  end

  def execute_with_handler(handler, command, command_id)
    start_time = Time.now

    begin
      if handler.respond_to?(:call)
        result = handler.call(command)
      elsif handler.respond_to?(:handle)
        result = handler.handle(command)
      else
        raise "Handler does not respond to call or handle"
      end

      {
        success: true,
        result: result,
        command_id: command_id,
        duration: Time.now - start_time
      }
    rescue => e
      {
        success: false,
        error: e.message,
        command_id: command_id,
        duration: Time.now - start_time
      }
    end
  end

  def log_command_execution(command, command_id, result, duration)
    return unless @config['commands']['logging_enabled'] == 'true'

    log_entry = {
      command_id: command_id,
      command_type: command.class.name,
      success: result[:success],
      duration: duration,
      timestamp: Time.now.iso8601
    }

    @redis.lpush('command_log', log_entry.to_json)
    @redis.ltrim('command_log', 0, 9999)
  end

  def log_command_error(command, command_id, error)
    error_entry = {
      command_id: command_id,
      command_type: command.class.name,
      error: error.message,
      backtrace: error.backtrace.first(5),
      timestamp: Time.now.iso8601
    }

    @redis.lpush('command_errors', error_entry.to_json)
    @redis.ltrim('command_errors', 0, 9999)
  end

  def get_total_command_count
    @redis.llen('command_log')
  end

  def get_successful_command_count
    commands = @redis.lrange('command_log', 0, -1)
    commands.count { |cmd| JSON.parse(cmd)['success'] }
  end

  def get_failed_command_count
    commands = @redis.lrange('command_log', 0, -1)
    commands.count { |cmd| !JSON.parse(cmd)['success'] }
  end

  def get_average_execution_time
    commands = @redis.lrange('command_log', 0, -1)
    return 0 if commands.empty?

    total_time = commands.sum { |cmd| JSON.parse(cmd)['duration'] }
    (total_time / commands.length).round(3)
  end

  def get_commands_by_type
    commands = @redis.lrange('command_log', 0, -1)
    command_types = {}

    commands.each do |cmd|
      cmd_data = JSON.parse(cmd)
      command_type = cmd_data['command_type']
      command_types[command_type] ||= 0
      command_types[command_type] += 1
    end

    command_types
  end
end
```

## 🔍 **Query Bus Implementation**

### Optimized Query Handling

```ruby
# lib/query_bus.rb
require 'tusk'
require 'redis'
require 'json'
require 'securerandom'

class QueryBus
  def initialize(config_path = 'config/cqrs.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    @handlers = {}
    @middleware = []
    setup_query_bus
  end

  def register_handler(query_class, handler)
    @handlers[query_class] = handler
  end

  def add_middleware(middleware)
    @middleware << middleware
  end

  def execute(query)
    return { success: false, error: 'CQRS disabled' } unless @config['cqrs']['enabled'] == 'true'

    query_id = SecureRandom.uuid
    start_time = Time.now

    begin
      # Check cache first
      if @config['queries']['caching_enabled'] == 'true'
        cached_result = get_cached_result(query)
        if cached_result
          return {
            success: true,
            result: cached_result,
            cached: true,
            query_id: query_id,
            duration: Time.now - start_time
          }
        end
      end

      # Apply middleware
      processed_query = apply_middleware(query, query_id)
      return processed_query unless processed_query[:success]

      # Authorize query
      authorization_result = authorize_query(query)
      unless authorization_result[:authorized]
        return {
          success: false,
          error: 'Query authorization failed',
          details: authorization_result[:reason],
          query_id: query_id
        }
      end

      # Execute query
      handler = get_handler(query.class)
      unless handler
        return {
          success: false,
          error: "No handler registered for query: #{query.class}",
          query_id: query_id
        }
      end

      result = execute_with_handler(handler, query, query_id)
      
      # Cache result if successful
      if result[:success] && @config['queries']['caching_enabled'] == 'true'
        cache_result(query, result[:result])
      end
      
      # Log query execution
      log_query_execution(query, query_id, result, Time.now - start_time)
      
      result
    rescue => e
      error_result = {
        success: false,
        error: e.message,
        query_id: query_id,
        duration: Time.now - start_time
      }
      
      log_query_error(query, query_id, e)
      error_result
    end
  end

  def execute_batch(queries)
    return { success: false, error: 'No queries provided' } if queries.empty?

    results = []
    batch_id = SecureRandom.uuid

    queries.each_with_index do |query, index|
      result = execute(query)
      result[:batch_id] = batch_id
      result[:batch_index] = index
      results << result
    end

    {
      batch_id: batch_id,
      total_queries: queries.length,
      successful: results.count { |r| r[:success] },
      failed: results.count { |r| !r[:success] },
      results: results
    }
  end

  def invalidate_cache(pattern = nil)
    if pattern
      keys = @redis.keys("query_cache:#{pattern}")
      @redis.del(*keys) if keys.any?
    else
      keys = @redis.keys('query_cache:*')
      @redis.del(*keys) if keys.any?
    end

    { invalidated_keys: keys&.length || 0 }
  end

  def get_query_statistics
    {
      total_queries: get_total_query_count,
      successful_queries: get_successful_query_count,
      failed_queries: get_failed_query_count,
      cache_hits: get_cache_hit_count,
      cache_miss_rate: get_cache_miss_rate,
      average_execution_time: get_average_execution_time,
      queries_by_type: get_queries_by_type
    }
  end

  private

  def setup_query_bus
    case @config['cqrs']['query_bus']
    when 'asynchronous'
      setup_asynchronous_bus
    when 'distributed'
      setup_distributed_bus
    end
  end

  def setup_asynchronous_bus
    # Implementation for asynchronous query bus
  end

  def setup_distributed_bus
    # Implementation for distributed query bus
  end

  def get_cached_result(query)
    cache_key = generate_cache_key(query)
    cached_data = @redis.get(cache_key)
    
    if cached_data
      increment_cache_hits
      JSON.parse(cached_data)
    else
      increment_cache_misses
      nil
    end
  end

  def cache_result(query, result)
    cache_key = generate_cache_key(query)
    ttl = @config['queries']['cache_ttl'].to_i
    
    @redis.setex(cache_key, ttl, result.to_json)
  end

  def generate_cache_key(query)
    query_data = query.to_h rescue query.to_s
    "query_cache:#{query.class.name}:#{Digest::SHA256.hexdigest(query_data.to_json)}"
  end

  def apply_middleware(query, query_id)
    processed_query = query.dup

    @middleware.each do |middleware|
      begin
        result = middleware.process(processed_query, query_id)
        return result unless result[:success]
        processed_query = result[:query]
      rescue => e
        return {
          success: false,
          error: "Middleware error: #{e.message}",
          query_id: query_id
        }
      end
    end

    { success: true, query: processed_query }
  end

  def authorize_query(query)
    return { authorized: true } unless @config['queries']['authorization_enabled'] == 'true'

    if query.respond_to?(:authorize)
      query.authorize
    else
      { authorized: true }
    end
  end

  def get_handler(query_class)
    @handlers[query_class]
  end

  def execute_with_handler(handler, query, query_id)
    start_time = Time.now

    begin
      if handler.respond_to?(:call)
        result = handler.call(query)
      elsif handler.respond_to?(:handle)
        result = handler.handle(query)
      else
        raise "Handler does not respond to call or handle"
      end

      {
        success: true,
        result: result,
        query_id: query_id,
        duration: Time.now - start_time
      }
    rescue => e
      {
        success: false,
        error: e.message,
        query_id: query_id,
        duration: Time.now - start_time
      }
    end
  end

  def log_query_execution(query, query_id, result, duration)
    log_entry = {
      query_id: query_id,
      query_type: query.class.name,
      success: result[:success],
      cached: result[:cached] || false,
      duration: duration,
      timestamp: Time.now.iso8601
    }

    @redis.lpush('query_log', log_entry.to_json)
    @redis.ltrim('query_log', 0, 9999)
  end

  def log_query_error(query, query_id, error)
    error_entry = {
      query_id: query_id,
      query_type: query.class.name,
      error: error.message,
      backtrace: error.backtrace.first(5),
      timestamp: Time.now.iso8601
    }

    @redis.lpush('query_errors', error_entry.to_json)
    @redis.ltrim('query_errors', 0, 9999)
  end

  def increment_cache_hits
    @redis.incr('cache_hits')
  end

  def increment_cache_misses
    @redis.incr('cache_misses')
  end

  def get_total_query_count
    @redis.llen('query_log')
  end

  def get_successful_query_count
    queries = @redis.lrange('query_log', 0, -1)
    queries.count { |q| JSON.parse(q)['success'] }
  end

  def get_failed_query_count
    queries = @redis.lrange('query_log', 0, -1)
    queries.count { |q| !JSON.parse(q)['success'] }
  end

  def get_cache_hit_count
    @redis.get('cache_hits').to_i
  end

  def get_cache_miss_rate
    hits = get_cache_hit_count
    misses = @redis.get('cache_misses').to_i
    total = hits + misses
    
    return 0 if total == 0
    (misses.to_f / total * 100).round(2)
  end

  def get_average_execution_time
    queries = @redis.lrange('query_log', 0, -1)
    return 0 if queries.empty?

    total_time = queries.sum { |q| JSON.parse(q)['duration'] }
    (total_time / queries.length).round(3)
  end

  def get_queries_by_type
    queries = @redis.lrange('query_log', 0, -1)
    query_types = {}

    queries.each do |q|
      q_data = JSON.parse(q)
      query_type = q_data['query_type']
      query_types[query_type] ||= 0
      query_types[query_type] += 1
    end

    query_types
  end
end
```

## 📝 **Command and Query Classes**

### Command Definitions

```ruby
# lib/commands.rb
require 'active_model'

class CreateUserCommand
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :email, :name, :password, :role

  validates :email, presence: true, format: { with: URI::MailTo::EMAIL_REGEXP }
  validates :name, presence: true, length: { minimum: 2 }
  validates :password, presence: true, length: { minimum: 8 }
  validates :role, inclusion: { in: %w[user admin moderator] }

  def initialize(attributes = {})
    super
    @role ||= 'user'
  end

  def authorize
    # Implementation for command authorization
    { authorized: true }
  end
end

class UpdateUserCommand
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :user_id, :email, :name, :role

  validates :user_id, presence: true
  validates :email, format: { with: URI::MailTo::EMAIL_REGEXP }, allow_blank: true
  validates :name, length: { minimum: 2 }, allow_blank: true
  validates :role, inclusion: { in: %w[user admin moderator] }, allow_blank: true

  def authorize
    # Implementation for command authorization
    { authorized: true }
  end
end

class DeleteUserCommand
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :user_id, :reason

  validates :user_id, presence: true
  validates :reason, presence: true, length: { minimum: 10 }

  def authorize
    # Implementation for command authorization
    { authorized: true }
  end
end

class CreateOrderCommand
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :user_id, :items, :shipping_address

  validates :user_id, presence: true
  validates :items, presence: true
  validates :shipping_address, presence: true

  def authorize
    # Implementation for command authorization
    { authorized: true }
  end
end

class UpdateOrderCommand
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :order_id, :status, :notes

  validates :order_id, presence: true
  validates :status, inclusion: { in: %w[pending confirmed shipped delivered cancelled] }

  def authorize
    # Implementation for command authorization
    { authorized: true }
  end
end
```

### Query Definitions

```ruby
# lib/queries.rb
require 'active_model'

class GetUserQuery
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :user_id

  validates :user_id, presence: true

  def authorize
    # Implementation for query authorization
    { authorized: true }
  end
end

class GetUsersQuery
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :page, :per_page, :filters, :sort_by, :sort_order

  validates :page, numericality: { greater_than: 0 }, allow_blank: true
  validates :per_page, numericality: { greater_than: 0, less_than_or_equal_to: 100 }, allow_blank: true
  validates :sort_order, inclusion: { in: %w[asc desc] }, allow_blank: true

  def initialize(attributes = {})
    super
    @page ||= 1
    @per_page ||= 20
    @filters ||= {}
    @sort_by ||= 'created_at'
    @sort_order ||= 'desc'
  end

  def authorize
    # Implementation for query authorization
    { authorized: true }
  end
end

class GetUserOrdersQuery
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :user_id, :page, :per_page, :status

  validates :user_id, presence: true
  validates :page, numericality: { greater_than: 0 }, allow_blank: true
  validates :per_page, numericality: { greater_than: 0, less_than_or_equal_to: 100 }, allow_blank: true
  validates :status, inclusion: { in: %w[pending confirmed shipped delivered cancelled] }, allow_blank: true

  def initialize(attributes = {})
    super
    @page ||= 1
    @per_page ||= 20
  end

  def authorize
    # Implementation for query authorization
    { authorized: true }
  end
end

class GetOrderQuery
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :order_id

  validates :order_id, presence: true

  def authorize
    # Implementation for query authorization
    { authorized: true }
  end
end

class GetOrdersQuery
  include ActiveModel::Model
  include ActiveModel::Validations

  attr_accessor :page, :per_page, :filters, :sort_by, :sort_order

  validates :page, numericality: { greater_than: 0 }, allow_blank: true
  validates :per_page, numericality: { greater_than: 0, less_than_or_equal_to: 100 }, allow_blank: true
  validates :sort_order, inclusion: { in: %w[asc desc] }, allow_blank: true

  def initialize(attributes = {})
    super
    @page ||= 1
    @per_page ||= 20
    @filters ||= {}
    @sort_by ||= 'created_at'
    @sort_order ||= 'desc'
  end

  def authorize
    # Implementation for query authorization
    { authorized: true }
  end
end
```

## 🎯 **Command and Query Handlers**

### Command Handlers

```ruby
# lib/command_handlers.rb
require 'tusk'
require 'redis'

class CreateUserCommandHandler
  def initialize(event_store, read_model)
    @event_store = event_store
    @read_model = read_model
  end

  def handle(command)
    # Check if user already exists
    existing_user = @read_model.find_user_by_email(command.email)
    if existing_user
      return { success: false, error: 'User with this email already exists' }
    end

    # Create user aggregate
    user_aggregate = UserAggregate.new(SecureRandom.uuid, @event_store)
    
    # Execute command on aggregate
    result = user_aggregate.create_user(command.email, command.name, command.password, command.role)
    
    if result[:success]
      # Save aggregate
      save_result = user_aggregate.save
      if save_result[:success]
        { success: true, user_id: user_aggregate.id }
      else
        { success: false, error: save_result[:error] }
      end
    else
      result
    end
  end
end

class UpdateUserCommandHandler
  def initialize(event_store, read_model)
    @event_store = event_store
    @read_model = read_model
  end

  def handle(command)
    # Load user aggregate
    user_aggregate = UserAggregate.new(command.user_id, @event_store)
    user_aggregate.load_from_history

    # Check if user exists
    unless user_aggregate.get_state[:email]
      return { success: false, error: 'User not found' }
    end

    # Execute command on aggregate
    result = user_aggregate.update_profile({
      email: command.email,
      name: command.name,
      role: command.role
    })

    if result[:success]
      # Save aggregate
      save_result = user_aggregate.save
      if save_result[:success]
        { success: true, user_id: user_aggregate.id }
      else
        { success: false, error: save_result[:error] }
      end
    else
      result
    end
  end
end

class DeleteUserCommandHandler
  def initialize(event_store, read_model)
    @event_store = event_store
    @read_model = read_model
  end

  def handle(command)
    # Load user aggregate
    user_aggregate = UserAggregate.new(command.user_id, @event_store)
    user_aggregate.load_from_history

    # Check if user exists
    unless user_aggregate.get_state[:email]
      return { success: false, error: 'User not found' }
    end

    # Execute command on aggregate
    result = user_aggregate.deactivate_user(command.reason)

    if result[:success]
      # Save aggregate
      save_result = user_aggregate.save
      if save_result[:success]
        { success: true, user_id: user_aggregate.id }
      else
        { success: false, error: save_result[:error] }
      end
    else
      result
    end
  end
end

class CreateOrderCommandHandler
  def initialize(event_store, read_model)
    @event_store = event_store
    @read_model = read_model
  end

  def handle(command)
    # Validate user exists
    user = @read_model.find_user(command.user_id)
    unless user
      return { success: false, error: 'User not found' }
    end

    # Create order aggregate
    order_aggregate = OrderAggregate.new(SecureRandom.uuid, @event_store)
    
    # Execute command on aggregate
    result = order_aggregate.create_order(command.user_id, command.items, command.shipping_address)
    
    if result[:success]
      # Save aggregate
      save_result = order_aggregate.save
      if save_result[:success]
        { success: true, order_id: order_aggregate.id }
      else
        { success: false, error: save_result[:error] }
      end
    else
      result
    end
  end
end

class UpdateOrderCommandHandler
  def initialize(event_store, read_model)
    @event_store = event_store
    @read_model = read_model
  end

  def handle(command)
    # Load order aggregate
    order_aggregate = OrderAggregate.new(command.order_id, @event_store)
    order_aggregate.load_from_history

    # Check if order exists
    unless order_aggregate.get_state[:user_id]
      return { success: false, error: 'Order not found' }
    end

    # Execute command on aggregate
    result = order_aggregate.update_status(command.status, command.notes)

    if result[:success]
      # Save aggregate
      save_result = order_aggregate.save
      if save_result[:success]
        { success: true, order_id: order_aggregate.id }
      else
        { success: false, error: save_result[:error] }
      end
    else
      result
    end
  end
end
```

### Query Handlers

```ruby
# lib/query_handlers.rb
require 'tusk'
require 'redis'

class GetUserQueryHandler
  def initialize(read_model)
    @read_model = read_model
  end

  def handle(query)
    user = @read_model.find_user(query.user_id)
    
    if user
      { success: true, user: user }
    else
      { success: false, error: 'User not found' }
    end
  end
end

class GetUsersQueryHandler
  def initialize(read_model)
    @read_model = read_model
  end

  def handle(query)
    users = @read_model.find_users(
      page: query.page,
      per_page: query.per_page,
      filters: query.filters,
      sort_by: query.sort_by,
      sort_order: query.sort_order
    )

    total_count = @read_model.count_users(query.filters)

    {
      success: true,
      users: users,
      pagination: {
        page: query.page,
        per_page: query.per_page,
        total_count: total_count,
        total_pages: (total_count.to_f / query.per_page).ceil
      }
    }
  end
end

class GetUserOrdersQueryHandler
  def initialize(read_model)
    @read_model = read_model
  end

  def handle(query)
    orders = @read_model.find_user_orders(
      user_id: query.user_id,
      page: query.page,
      per_page: query.per_page,
      status: query.status
    )

    total_count = @read_model.count_user_orders(query.user_id, query.status)

    {
      success: true,
      orders: orders,
      pagination: {
        page: query.page,
        per_page: query.per_page,
        total_count: total_count,
        total_pages: (total_count.to_f / query.per_page).ceil
      }
    }
  end
end

class GetOrderQueryHandler
  def initialize(read_model)
    @read_model = read_model
  end

  def handle(query)
    order = @read_model.find_order(query.order_id)
    
    if order
      { success: true, order: order }
    else
      { success: false, error: 'Order not found' }
    end
  end
end

class GetOrdersQueryHandler
  def initialize(read_model)
    @read_model = read_model
  end

  def handle(query)
    orders = @read_model.find_orders(
      page: query.page,
      per_page: query.per_page,
      filters: query.filters,
      sort_by: query.sort_by,
      sort_order: query.sort_order
    )

    total_count = @read_model.count_orders(query.filters)

    {
      success: true,
      orders: orders,
      pagination: {
        page: query.page,
        per_page: query.per_page,
        total_count: total_count,
        total_pages: (total_count.to_f / query.per_page).ceil
      }
    }
  end
end
```

## 🎯 **Configuration Management**

### CQRS Configuration

```ruby
# config/cqrs_features.tsk
[cqrs]
enabled: @env("CQRS_ENABLED", "true")
command_bus: @env("COMMAND_BUS_TYPE", "synchronous")
query_bus: @env("QUERY_BUS_TYPE", "synchronous")
event_sourcing: @env("CQRS_EVENT_SOURCING", "true")
read_write_separation: @env("READ_WRITE_SEPARATION", "true")

[commands]
validation_enabled: @env("COMMAND_VALIDATION_ENABLED", "true")
authorization_enabled: @env("COMMAND_AUTHORIZATION_ENABLED", "true")
logging_enabled: @env("COMMAND_LOGGING_ENABLED", "true")
retry_attempts: @env("COMMAND_RETRY_ATTEMPTS", "3")
async_processing: @env("COMMAND_ASYNC_PROCESSING", "false")

[queries]
caching_enabled: @env("QUERY_CACHING_ENABLED", "true")
cache_ttl: @env("QUERY_CACHE_TTL", "300")
authorization_enabled: @env("QUERY_AUTHORIZATION_ENABLED", "true")
pagination_enabled: @env("QUERY_PAGINATION_ENABLED", "true")
optimization_enabled: @env("QUERY_OPTIMIZATION_ENABLED", "true")

[read_models]
database: @env("READ_MODEL_DATABASE", "postgresql")
optimization_enabled: @env("READ_MODEL_OPTIMIZATION_ENABLED", "true")
denormalization_enabled: @env("READ_MODEL_DENORMALIZATION_ENABLED", "true")
indexing_enabled: @env("READ_MODEL_INDEXING_ENABLED", "true")

[write_models]
database: @env("WRITE_MODEL_DATABASE", "postgresql")
event_store_enabled: @env("WRITE_MODEL_EVENT_STORE", "true")
snapshot_enabled: @env("WRITE_MODEL_SNAPSHOTS", "true")
optimistic_concurrency: @env("WRITE_MODEL_OPTIMISTIC_CONCURRENCY", "true")

[performance]
command_timeout: @env("COMMAND_TIMEOUT", "30")
query_timeout: @env("QUERY_TIMEOUT", "10")
max_concurrent_commands: @env("MAX_CONCURRENT_COMMANDS", "100")
max_concurrent_queries: @env("MAX_CONCURRENT_QUERIES", "200")
```

## 🎯 **Summary**

This comprehensive guide covers CQRS implementation with TuskLang and Ruby, including:

- **Command Bus**: Command handling with validation, authorization, and middleware
- **Query Bus**: Query handling with caching, authorization, and optimization
- **Command Classes**: Structured command definitions with validation
- **Query Classes**: Structured query definitions with pagination
- **Command Handlers**: Business logic for command processing
- **Query Handlers**: Optimized query processing with read models
- **Configuration Management**: Enterprise-grade CQRS configuration

The CQRS features with TuskLang provide a robust foundation for building systems that can handle massive read and write loads independently, optimize performance through read/write separation, and maintain data consistency through event sourcing. 
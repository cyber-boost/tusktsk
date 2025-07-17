# 📝 Advanced Logging with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Build sophisticated logging systems with TuskLang's advanced logging features. From structured logging to distributed tracing, TuskLang provides the flexibility and power you need to monitor and debug your Ruby applications effectively.

## 🚀 Quick Start

### Basic Logging Setup
```ruby
require 'tusklang'
require 'tusklang/logging'

# Initialize logging system
logging_system = TuskLang::Logging::System.new

# Configure logging
logging_system.configure do |config|
  config.default_level = 'info'
  config.format = 'json'
  config.output = 'file'
  config.rotation_enabled = true
end

# Register logging strategies
logging_system.register_strategy(:structured, TuskLang::Logging::Strategies::StructuredLoggingStrategy.new)
logging_system.register_strategy(:distributed, TuskLang::Logging::Strategies::DistributedLoggingStrategy.new)
logging_system.register_strategy(:performance, TuskLang::Logging::Strategies::PerformanceLoggingStrategy.new)
```

### TuskLang Configuration
```tsk
# config/logging.tsk
[logging]
enabled: true
default_level: "info"
format: "json"
output: "file"
rotation_enabled: true

[logging.levels]
trace: 0
debug: 1
info: 2
warn: 3
error: 4
fatal: 5

[logging.outputs]
file: {
    path: "/var/log/app.log",
    max_size: "100MB",
    max_files: 10,
    compress: true
}
stdout: {
    enabled: true,
    colorize: true
}
syslog: {
    enabled: false,
    facility: "local0",
    tag: "myapp"
}

[logging.formats]
json: {
    include_timestamp: true,
    include_level: true,
    include_context: true,
    include_stack_trace: true
}
text: {
    include_timestamp: true,
    include_level: true,
    include_context: false,
    colorize: true
}

[logging.context]
include_request_id: true
include_user_id: true
include_session_id: true
include_correlation_id: true

[logging.filters]
sensitive_fields: ["password", "token", "secret", "api_key"]
mask_patterns: ["\\b\\d{4}-\\d{4}-\\d{4}-\\d{4}\\b"] # Credit card numbers
```

## 🎯 Core Features

### 1. Structured Logging Strategy
```ruby
require 'tusklang/logging'
require 'json'

class StructuredLoggingStrategy
  include TuskLang::Logging::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/logging.tsk')
    @outputs = setup_outputs
    @filters = setup_filters
  end
  
  def log(level, message, context = {})
    return unless should_log?(level)
    
    # Build log entry
    log_entry = build_log_entry(level, message, context)
    
    # Filter sensitive data
    filtered_entry = filter_sensitive_data(log_entry)
    
    # Format log entry
    formatted_entry = format_log_entry(filtered_entry)
    
    # Write to outputs
    write_to_outputs(formatted_entry, level)
  end
  
  def trace(message, context = {})
    log(:trace, message, context)
  end
  
  def debug(message, context = {})
    log(:debug, message, context)
  end
  
  def info(message, context = {})
    log(:info, message, context)
  end
  
  def warn(message, context = {})
    log(:warn, message, context)
  end
  
  def error(message, context = {})
    log(:error, message, context)
  end
  
  def fatal(message, context = {})
    log(:fatal, message, context)
  end
  
  private
  
  def should_log?(level)
    level_priority = @config['logging']['levels'][level.to_s]
    default_priority = @config['logging']['levels'][@config['logging']['default_level']]
    
    level_priority >= default_priority
  end
  
  def build_log_entry(level, message, context)
    entry = {
      timestamp: Time.now.iso8601,
      level: level.to_s.upcase,
      message: message,
      context: context
    }
    
    # Add request context if available
    if @config['logging']['context']['include_request_id'] && context[:request_id]
      entry[:request_id] = context[:request_id]
    end
    
    if @config['logging']['context']['include_user_id'] && context[:user_id]
      entry[:user_id] = context[:user_id]
    end
    
    if @config['logging']['context']['include_session_id'] && context[:session_id]
      entry[:session_id] = context[:session_id]
    end
    
    if @config['logging']['context']['include_correlation_id'] && context[:correlation_id]
      entry[:correlation_id] = context[:correlation_id]
    end
    
    # Add stack trace for errors
    if level == :error || level == :fatal
      entry[:stack_trace] = build_stack_trace
    end
    
    entry
  end
  
  def filter_sensitive_data(log_entry)
    filtered_entry = log_entry.dup
    
    @filters[:sensitive_fields].each do |field|
      filter_sensitive_field(filtered_entry, field)
    end
    
    @filters[:mask_patterns].each do |pattern|
      filter_by_pattern(filtered_entry, pattern)
    end
    
    filtered_entry
  end
  
  def filter_sensitive_field(entry, field)
    if entry[:context].is_a?(Hash)
      entry[:context].each do |key, value|
        if key.to_s.downcase.include?(field.downcase)
          entry[:context][key] = '[FILTERED]'
        elsif value.is_a?(Hash)
          filter_sensitive_field({ context: value }, field)
        end
      end
    end
  end
  
  def filter_by_pattern(entry, pattern)
    entry_json = entry.to_json
    filtered_json = entry_json.gsub(Regexp.new(pattern), '[MASKED]')
    
    if filtered_json != entry_json
      entry.replace(JSON.parse(filtered_json, symbolize_names: true))
    end
  end
  
  def format_log_entry(log_entry)
    format = @config['logging']['format']
    
    case format
    when 'json'
      format_as_json(log_entry)
    when 'text'
      format_as_text(log_entry)
    else
      format_as_json(log_entry)
    end
  end
  
  def format_as_json(log_entry)
    JSON.generate(log_entry)
  end
  
  def format_as_text(log_entry)
    timestamp = log_entry[:timestamp]
    level = log_entry[:level]
    message = log_entry[:message]
    
    text = "[#{timestamp}] #{level}: #{message}"
    
    if log_entry[:context] && !log_entry[:context].empty?
      text += " #{log_entry[:context].to_json}"
    end
    
    if log_entry[:stack_trace]
      text += "\n#{log_entry[:stack_trace]}"
    end
    
    text
  end
  
  def write_to_outputs(formatted_entry, level)
    @outputs.each do |output|
      begin
        output.write(formatted_entry, level)
      rescue => e
        # Fallback to stderr if output fails
        $stderr.puts "Logging output failed: #{e.message}"
        $stderr.puts formatted_entry
      end
    end
  end
  
  def build_stack_trace
    caller(2).join("\n")
  end
  
  def setup_outputs
    outputs = []
    
    if @config['logging']['outputs']['file']['enabled']
      outputs << FileOutput.new(@config['logging']['outputs']['file'])
    end
    
    if @config['logging']['outputs']['stdout']['enabled']
      outputs << StdoutOutput.new(@config['logging']['outputs']['stdout'])
    end
    
    if @config['logging']['outputs']['syslog']['enabled']
      outputs << SyslogOutput.new(@config['logging']['outputs']['syslog'])
    end
    
    outputs
  end
  
  def setup_filters
    {
      sensitive_fields: @config['logging']['filters']['sensitive_fields'],
      mask_patterns: @config['logging']['filters']['mask_patterns']
    }
  end
end
```

### 2. Distributed Logging Strategy
```ruby
require 'tusklang/logging'

class DistributedLoggingStrategy
  include TuskLang::Logging::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/logging.tsk')
    @correlation_id = SecureRandom.uuid
    @span_id = 0
    @tracer = setup_tracer
  end
  
  def log(level, message, context = {})
    # Add distributed tracing context
    distributed_context = add_distributed_context(context)
    
    # Create span for this log entry
    span = create_span(level, message, distributed_context)
    
    # Log with distributed context
    structured_logger = StructuredLoggingStrategy.new
    structured_logger.log(level, message, distributed_context)
    
    # Close span
    span.finish
  end
  
  def start_span(name, context = {})
    @span_id += 1
    span_context = {
      trace_id: @correlation_id,
      span_id: @span_id,
      parent_span_id: context[:parent_span_id]
    }
    
    Span.new(name, span_context, @tracer)
  end
  
  def inject_context(context)
    {
      correlation_id: @correlation_id,
      trace_id: @correlation_id,
      span_id: @span_id
    }.merge(context)
  end
  
  private
  
  def add_distributed_context(context)
    context.merge({
      correlation_id: @correlation_id,
      trace_id: @correlation_id,
      span_id: @span_id,
      node_id: Socket.gethostname,
      process_id: Process.pid
    })
  end
  
  def create_span(level, message, context)
    span_name = "#{level}_#{message.gsub(/\s+/, '_')}"
    
    @tracer.start_span(span_name, {
      tags: {
        'log.level' => level,
        'log.message' => message,
        'node.id' => context[:node_id],
        'process.id' => context[:process_id]
      }
    })
  end
  
  def setup_tracer
    # Setup OpenTracing compatible tracer
    # This could be Jaeger, Zipkin, etc.
    OpenTracing.global_tracer
  end
end

class Span
  def initialize(name, context, tracer)
    @name = name
    @context = context
    @tracer = tracer
    @span = tracer.start_span(name, { child_of: context[:parent_span] })
  end
  
  def log(level, message, context = {})
    @span.log_kv({
      event: 'log',
      level: level,
      message: message
    }.merge(context))
  end
  
  def set_tag(key, value)
    @span.set_tag(key, value)
  end
  
  def finish
    @span.finish
  end
end
```

### 3. Performance Logging Strategy
```ruby
require 'tusklang/logging'

class PerformanceLoggingStrategy
  include TuskLang::Logging::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/logging.tsk')
    @metrics = setup_metrics
    @thresholds = setup_thresholds
  end
  
  def log_performance(operation, duration, context = {})
    # Log performance data
    log(:info, "Performance: #{operation}", {
      operation: operation,
      duration_ms: duration,
      duration_seconds: duration / 1000.0
    }.merge(context))
    
    # Record metrics
    record_metrics(operation, duration, context)
    
    # Check thresholds
    check_thresholds(operation, duration, context)
  end
  
  def time_operation(operation, context = {})
    start_time = Time.now
    
    begin
      result = yield
      duration = ((Time.now - start_time) * 1000).to_i
      
      log_performance(operation, duration, context.merge(success: true))
      
      result
    rescue => e
      duration = ((Time.now - start_time) * 1000).to_i
      
      log_performance(operation, duration, context.merge(
        success: false,
        error: e.message,
        error_class: e.class.name
      ))
      
      raise e
    end
  end
  
  def log_memory_usage(context = {})
    memory_usage = get_memory_usage
    
    log(:info, "Memory usage", {
      memory_mb: memory_usage[:memory_mb],
      heap_size_mb: memory_usage[:heap_size_mb],
      heap_allocated_mb: memory_usage[:heap_allocated_mb]
    }.merge(context))
  end
  
  def log_database_query(sql, duration, context = {})
    log(:debug, "Database query", {
      sql: sql,
      duration_ms: duration,
      slow_query: duration > @thresholds[:slow_query_threshold]
    }.merge(context))
  end
  
  def log_http_request(method, url, status, duration, context = {})
    log(:info, "HTTP request", {
      method: method,
      url: url,
      status: status,
      duration_ms: duration,
      slow_request: duration > @thresholds[:slow_request_threshold]
    }.merge(context))
  end
  
  private
  
  def record_metrics(operation, duration, context)
    @metrics.increment("performance.operations.#{operation}")
    @metrics.histogram("performance.duration.#{operation}", duration)
    
    if context[:success] == false
      @metrics.increment("performance.errors.#{operation}")
    end
  end
  
  def check_thresholds(operation, duration, context)
    threshold = @thresholds[:performance_thresholds][operation]
    return unless threshold
    
    if duration > threshold
      log(:warn, "Performance threshold exceeded", {
        operation: operation,
        duration_ms: duration,
        threshold_ms: threshold
      }.merge(context))
    end
  end
  
  def get_memory_usage
    if defined?(GC)
      stats = GC.stat
      {
        memory_mb: (Process.getrusage.maxrss / 1024.0).round(2),
        heap_size_mb: (stats[:heap_allocated_pages] * stats[:heap_page_size] / 1024.0 / 1024.0).round(2),
        heap_allocated_mb: (stats[:heap_allocated_pages] * stats[:heap_page_size] / 1024.0 / 1024.0).round(2)
      }
    else
      {
        memory_mb: (Process.getrusage.maxrss / 1024.0).round(2),
        heap_size_mb: 0,
        heap_allocated_mb: 0
      }
    end
  end
  
  def setup_metrics
    TuskLang::Metrics::Collector.new
  end
  
  def setup_thresholds
    {
      slow_query_threshold: 100, # ms
      slow_request_threshold: 1000, # ms
      performance_thresholds: {
        'database_query' => 50,
        'http_request' => 500,
        'file_operation' => 100
      }
    }
  end
end
```

### 4. Logging Middleware
```ruby
require 'tusklang/logging'

class LoggingMiddleware
  def initialize(app)
    @app = app
    @logger = TuskLang::Logging::System.new
    @config = TuskLang.parse_file('config/logging.tsk')
  end
  
  def call(env)
    request = Rack::Request.new(env)
    start_time = Time.now
    
    # Generate request ID
    request_id = SecureRandom.uuid
    
    # Add request ID to environment
    env['REQUEST_ID'] = request_id
    
    # Log request start
    log_request_start(request, request_id)
    
    # Process request
    status, headers, response = @app.call(env)
    
    # Calculate duration
    duration = ((Time.now - start_time) * 1000).to_i
    
    # Log request end
    log_request_end(request, status, duration, request_id)
    
    # Add request ID to response headers
    headers['X-Request-ID'] = request_id
    
    [status, headers, response]
  rescue => e
    # Log error
    log_request_error(request, e, request_id)
    raise e
  end
  
  private
  
  def log_request_start(request, request_id)
    @logger.info("Request started", {
      request_id: request_id,
      method: request.method,
      path: request.path,
      query_string: request.query_string,
      ip: request.ip,
      user_agent: request.user_agent,
      content_length: request.content_length
    })
  end
  
  def log_request_end(request, status, duration, request_id)
    @logger.info("Request completed", {
      request_id: request_id,
      method: request.method,
      path: request.path,
      status: status,
      duration_ms: duration,
      slow_request: duration > @config['logging']['thresholds']['slow_request_threshold']
    })
  end
  
  def log_request_error(request, error, request_id)
    @logger.error("Request failed", {
      request_id: request_id,
      method: request.method,
      path: request.path,
      error: error.message,
      error_class: error.class.name,
      stack_trace: error.backtrace&.first(10)
    })
  end
end
```

### 5. Logging Decorators
```ruby
require 'tusklang/logging'

module LoggingDecorators
  def self.included(base)
    base.extend(ClassMethods)
  end
  
  module ClassMethods
    def log_operations(*methods, **options)
      methods.each do |method|
        define_method("#{method}_with_logging") do |*args, &block|
          logger = TuskLang::Logging::System.new
          performance_logger = TuskLang::Logging::Strategies::PerformanceLoggingStrategy.new
          
          operation = "#{self.class.name}##{method}"
          
          performance_logger.time_operation(operation, {
            args: args,
            context: options[:context] || {}
          }) do
            send("#{method}_without_logging", *args, &block)
          end
        end
        
        alias_method "#{method}_without_logging", method
        alias_method method, "#{method}_with_logging"
      end
    end
  end
end

# Usage in models
class User < ApplicationRecord
  include LoggingDecorators
  
  log_operations :create, :update, :destroy, context: { model: 'User' }
  
  def self.find_by_email_with_logging(email)
    logger = TuskLang::Logging::System.new
    performance_logger = TuskLang::Logging::Strategies::PerformanceLoggingStrategy.new
    
    performance_logger.time_operation('User.find_by_email', {
      email: email
    }) do
      find_by_email_without_logging(email)
    end
  end
  
  alias_method :find_by_email_without_logging, :find_by_email
  alias_method :find_by_email, :find_by_email_with_logging
end
```

### 6. Log Output Classes
```ruby
require 'tusklang/logging'

class FileOutput
  def initialize(config)
    @config = config
    @file = nil
    @mutex = Mutex.new
    setup_file
  end
  
  def write(log_entry, level)
    @mutex.synchronize do
      @file.puts(log_entry)
      @file.flush
      
      # Check if rotation is needed
      rotate_if_needed
    end
  end
  
  private
  
  def setup_file
    @file = File.open(@config['path'], 'a')
    @file.sync = true
  end
  
  def rotate_if_needed
    return unless @config['rotation_enabled']
    
    if File.size(@config['path']) > parse_size(@config['max_size'])
      rotate_file
    end
  end
  
  def rotate_file
    # Close current file
    @file.close
    
    # Rotate existing files
    rotate_existing_files
    
    # Open new file
    setup_file
  end
  
  def rotate_existing_files
    max_files = @config['max_files']
    
    (max_files - 1).downto(1) do |i|
      old_file = "#{@config['path']}.#{i}"
      new_file = "#{@config['path']}.#{i + 1}"
      
      if File.exist?(old_file)
        if i == max_files - 1
          # Compress and remove oldest file
          compress_and_remove(old_file)
        else
          File.rename(old_file, new_file)
        end
      end
    end
    
    # Move current file to .1
    File.rename(@config['path'], "#{@config['path']}.1")
  end
  
  def compress_and_remove(file_path)
    if @config['compress']
      system("gzip #{file_path}")
    else
      File.delete(file_path)
    end
  end
  
  def parse_size(size_string)
    case size_string
    when /(\d+)MB/i
      $1.to_i * 1024 * 1024
    when /(\d+)GB/i
      $1.to_i * 1024 * 1024 * 1024
    else
      100 * 1024 * 1024 # Default 100MB
    end
  end
end

class StdoutOutput
  def initialize(config)
    @config = config
    @colorize = config['colorize']
  end
  
  def write(log_entry, level)
    if @colorize
      colored_entry = colorize_log_entry(log_entry, level)
      puts colored_entry
    else
      puts log_entry
    end
  end
  
  private
  
  def colorize_log_entry(log_entry, level)
    color = get_color_for_level(level)
    "\e[#{color}m#{log_entry}\e[0m"
  end
  
  def get_color_for_level(level)
    case level
    when :trace, :debug
      36 # Cyan
    when :info
      32 # Green
    when :warn
      33 # Yellow
    when :error, :fatal
      31 # Red
    else
      0 # Default
    end
  end
end

class SyslogOutput
  def initialize(config)
    @config = config
    require 'syslog'
    setup_syslog
  end
  
  def write(log_entry, level)
    priority = get_syslog_priority(level)
    Syslog.log(priority, log_entry)
  end
  
  private
  
  def setup_syslog
    Syslog.open(@config['tag'], Syslog::LOG_PID, get_syslog_facility(@config['facility']))
  end
  
  def get_syslog_priority(level)
    case level
    when :trace, :debug
      Syslog::LOG_DEBUG
    when :info
      Syslog::LOG_INFO
    when :warn
      Syslog::LOG_WARNING
    when :error
      Syslog::LOG_ERR
    when :fatal
      Syslog::LOG_CRIT
    else
      Syslog::LOG_INFO
    end
  end
  
  def get_syslog_facility(facility)
    case facility
    when 'local0'
      Syslog::LOG_LOCAL0
    when 'local1'
      Syslog::LOG_LOCAL1
    when 'local2'
      Syslog::LOG_LOCAL2
    when 'local3'
      Syslog::LOG_LOCAL3
    when 'local4'
      Syslog::LOG_LOCAL4
    when 'local5'
      Syslog::LOG_LOCAL5
    when 'local6'
      Syslog::LOG_LOCAL6
    when 'local7'
      Syslog::LOG_LOCAL7
    else
      Syslog::LOG_LOCAL0
    end
  end
end
```

## 🔧 Advanced Configuration

### Log Aggregation
```ruby
require 'tusklang/logging'

class LogAggregator
  def initialize
    @config = TuskLang.parse_file('config/logging.tsk')
    @buffer = []
    @buffer_size = 100
    @flush_interval = 5 # seconds
    start_flush_thread
  end
  
  def add_log(log_entry)
    @buffer << log_entry
    
    if @buffer.size >= @buffer_size
      flush_buffer
    end
  end
  
  private
  
  def flush_buffer
    return if @buffer.empty?
    
    logs_to_send = @buffer.dup
    @buffer.clear
    
    # Send logs to aggregation service
    send_logs_to_aggregator(logs_to_send)
  end
  
  def send_logs_to_aggregator(logs)
    # Implementation to send logs to ELK, Splunk, etc.
    # This could be HTTP, TCP, or other protocol
  end
  
  def start_flush_thread
    Thread.new do
      loop do
        sleep @flush_interval
        flush_buffer
      end
    end
  end
end
```

## 🚀 Performance Optimization

### Logging Caching
```ruby
require 'tusklang/logging'

class LoggingCache
  def initialize
    @cache = TuskLang::Cache::RedisCache.new
    @config = TuskLang.parse_file('config/logging.tsk')
  end
  
  def cache_log_entry(key, log_entry, ttl = 300)
    @cache.set(key, log_entry, ttl)
  end
  
  def get_cached_log_entry(key)
    @cache.get(key)
  end
  
  def cache_error_pattern(pattern, count, ttl = 3600)
    key = "error_pattern:#{Digest::MD5.hexdigest(pattern)}"
    @cache.increment(key, count, ttl)
  end
  
  def get_error_pattern_count(pattern)
    key = "error_pattern:#{Digest::MD5.hexdigest(pattern)}"
    @cache.get(key) || 0
  end
end
```

## 📊 Monitoring and Analytics

### Logging Analytics
```ruby
require 'tusklang/logging'

class LoggingAnalytics
  def initialize
    @metrics = TuskLang::Metrics::Collector.new
  end
  
  def track_log_entry(level, context = {})
    @metrics.increment("logging.entries.total")
    @metrics.increment("logging.entries.#{level}")
    
    if context[:operation]
      @metrics.increment("logging.entries.operation.#{context[:operation]}")
    end
    
    if context[:error]
      @metrics.increment("logging.entries.errors.#{context[:error_class]}")
    end
  end
  
  def track_performance_metric(operation, duration)
    @metrics.histogram("logging.performance.#{operation}", duration)
  end
  
  def get_logging_stats
    {
      total_entries: @metrics.get("logging.entries.total"),
      entries_by_level: @metrics.get_top("logging.entries", 5),
      error_rate: @metrics.get_rate("logging.entries.error", "logging.entries.total"),
      popular_operations: @metrics.get_top("logging.entries.operation", 10)
    }
  end
end
```

This comprehensive logging system provides enterprise-grade monitoring features while maintaining the flexibility and power of TuskLang. The combination of structured logging, distributed tracing, and performance monitoring creates a robust foundation for debugging and monitoring Ruby applications. 
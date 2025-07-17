# Advanced Monitoring with TuskLang and Ruby

## 📊 **Monitor Your Applications with Surgical Precision**

TuskLang enables sophisticated monitoring and observability for Ruby applications, providing comprehensive metrics collection, intelligent alerting, distributed tracing, and real-time insights. Build applications that you can monitor, debug, and optimize with confidence.

## 🚀 **Quick Start: Metrics Collection**

### Basic Monitoring Configuration

```ruby
# config/monitoring.tsk
[monitoring]
enabled: @env("MONITORING_ENABLED", "true")
environment: @env("MONITORING_ENVIRONMENT", "production")
service_name: @env("SERVICE_NAME", "tusklang-app")
version: @env("APP_VERSION", "1.0.0")

[metrics]
collection_interval: @env("METRICS_COLLECTION_INTERVAL", "15")
retention_period: @env("METRICS_RETENTION_PERIOD", "30d")
compression_enabled: @env("METRICS_COMPRESSION", "true")

[prometheus]
enabled: @env("PROMETHEUS_ENABLED", "true")
port: @env("PROMETHEUS_PORT", "9090")
path: @env("PROMETHEUS_PATH", "/metrics")

[alerting]
enabled: @env("ALERTING_ENABLED", "true")
slack_webhook: @env.secure("SLACK_WEBHOOK_URL")
email_recipients: @env("ALERT_EMAIL_RECIPIENTS", "admin@example.com")

[tracing]
enabled: @env("TRACING_ENABLED", "true")
jaeger_endpoint: @env("JAEGER_ENDPOINT", "http://localhost:14268")
sampling_rate: @env("TRACING_SAMPLING_RATE", "0.1")
```

### Comprehensive Metrics Collector

```ruby
# lib/advanced_metrics_collector.rb
require 'prometheus/client'
require 'prometheus/client/push'
require 'tusk'
require 'redis'
require 'json'

class AdvancedMetricsCollector
  def initialize(config_path = 'config/monitoring.tsk')
    @config = Tusk.load(config_path)
    setup_prometheus_metrics
    setup_redis_metrics_storage
    start_metrics_collection
  end

  def record_http_request(method, path, status, duration, instance_id = nil)
    labels = {
      method: method,
      path: normalize_path(path),
      status: status.to_s,
      instance: instance_id || get_instance_id
    }

    @http_requests_total.increment(labels: labels)
    @http_request_duration.observe(duration, labels: labels)
    @http_request_size.observe(get_request_size, labels: labels)

    # Store in Redis for custom analytics
    store_request_metrics(method, path, status, duration)
  end

  def record_database_query(query_type, table, duration, rows_affected = nil)
    labels = {
      query_type: query_type,
      table: table,
      instance: get_instance_id
    }

    @database_queries_total.increment(labels: labels)
    @database_query_duration.observe(duration, labels: labels)
    
    if rows_affected
      @database_rows_affected.observe(rows_affected, labels: labels)
    end

    store_database_metrics(query_type, table, duration, rows_affected)
  end

  def record_cache_operation(operation, cache_layer, duration, hit = nil)
    labels = {
      operation: operation,
      layer: cache_layer,
      instance: get_instance_id
    }

    @cache_operations_total.increment(labels: labels)
    @cache_operation_duration.observe(duration, labels: labels)

    if hit
      @cache_hits_total.increment(labels: labels.merge(hit: hit.to_s))
    end

    store_cache_metrics(operation, cache_layer, duration, hit)
  end

  def record_business_metric(metric_name, value, labels = {})
    labels[:instance] = get_instance_id
    labels[:metric_name] = metric_name

    @business_metrics.observe(value, labels: labels)
    store_business_metrics(metric_name, value, labels)
  end

  def record_error(error_type, error_message, stack_trace = nil)
    labels = {
      error_type: error_type,
      instance: get_instance_id
    }

    @errors_total.increment(labels: labels)
    @error_duration.observe(Time.now.to_f, labels: labels)

    store_error_metrics(error_type, error_message, stack_trace)
  end

  def record_system_metrics
    # CPU and Memory metrics
    cpu_usage = get_cpu_usage
    memory_usage = get_memory_usage
    disk_usage = get_disk_usage
    network_io = get_network_io

    @cpu_usage.set(cpu_usage, labels: { instance: get_instance_id })
    @memory_usage.set(memory_usage, labels: { instance: get_instance_id })
    @disk_usage.set(disk_usage, labels: { instance: get_instance_id })
    @network_io.set(network_io, labels: { instance: get_instance_id })

    store_system_metrics(cpu_usage, memory_usage, disk_usage, network_io)
  end

  def get_metrics_summary
    {
      http_requests: get_http_metrics_summary,
      database: get_database_metrics_summary,
      cache: get_cache_metrics_summary,
      errors: get_error_metrics_summary,
      system: get_system_metrics_summary
    }
  end

  def push_metrics_to_gateway
    return unless @config['prometheus']['enabled'] == 'true'

    Prometheus::Client::Push.new(
      job: @config['monitoring']['service_name'],
      gateway: @config['prometheus']['gateway_url']
    ).add(@registry)
  end

  private

  def setup_prometheus_metrics
    @registry = Prometheus::Client::Registry.new

    # HTTP metrics
    @http_requests_total = @registry.counter(
      :http_requests_total,
      docstring: 'Total number of HTTP requests',
      labels: [:method, :path, :status, :instance]
    )

    @http_request_duration = @registry.histogram(
      :http_request_duration_seconds,
      docstring: 'HTTP request duration in seconds',
      labels: [:method, :path, :status, :instance]
    )

    @http_request_size = @registry.histogram(
      :http_request_size_bytes,
      docstring: 'HTTP request size in bytes',
      labels: [:method, :path, :status, :instance]
    )

    # Database metrics
    @database_queries_total = @registry.counter(
      :database_queries_total,
      docstring: 'Total number of database queries',
      labels: [:query_type, :table, :instance]
    )

    @database_query_duration = @registry.histogram(
      :database_query_duration_seconds,
      docstring: 'Database query duration in seconds',
      labels: [:query_type, :table, :instance]
    )

    @database_rows_affected = @registry.histogram(
      :database_rows_affected,
      docstring: 'Number of rows affected by database queries',
      labels: [:query_type, :table, :instance]
    )

    # Cache metrics
    @cache_operations_total = @registry.counter(
      :cache_operations_total,
      docstring: 'Total number of cache operations',
      labels: [:operation, :layer, :instance]
    )

    @cache_operation_duration = @registry.histogram(
      :cache_operation_duration_seconds,
      docstring: 'Cache operation duration in seconds',
      labels: [:operation, :layer, :instance]
    )

    @cache_hits_total = @registry.counter(
      :cache_hits_total,
      docstring: 'Total number of cache hits',
      labels: [:operation, :layer, :hit, :instance]
    )

    # Business metrics
    @business_metrics = @registry.histogram(
      :business_metrics,
      docstring: 'Business-specific metrics',
      labels: [:metric_name, :instance]
    )

    # Error metrics
    @errors_total = @registry.counter(
      :errors_total,
      docstring: 'Total number of errors',
      labels: [:error_type, :instance]
    )

    @error_duration = @registry.histogram(
      :error_duration_seconds,
      docstring: 'Error occurrence timestamps',
      labels: [:error_type, :instance]
    )

    # System metrics
    @cpu_usage = @registry.gauge(
      :cpu_usage_percent,
      docstring: 'CPU usage percentage',
      labels: [:instance]
    )

    @memory_usage = @registry.gauge(
      :memory_usage_percent,
      docstring: 'Memory usage percentage',
      labels: [:instance]
    )

    @disk_usage = @registry.gauge(
      :disk_usage_percent,
      docstring: 'Disk usage percentage',
      labels: [:instance]
    )

    @network_io = @registry.gauge(
      :network_io_bytes,
      docstring: 'Network I/O in bytes',
      labels: [:instance]
    )
  end

  def setup_redis_metrics_storage
    @redis = Redis.new(url: @config['redis']['url'])
  end

  def start_metrics_collection
    Thread.new do
      loop do
        record_system_metrics
        push_metrics_to_gateway if @config['prometheus']['push_enabled'] == 'true'
        sleep @config['metrics']['collection_interval'].to_i
      end
    end
  end

  def normalize_path(path)
    # Normalize paths for better metrics aggregation
    path.gsub(/\d+/, ':id').gsub(/[a-f0-9]{8,}/, ':uuid')
  end

  def get_instance_id
    @instance_id ||= SecureRandom.uuid
  end

  def get_request_size
    # Implementation to get request size
    0
  end

  def get_cpu_usage
    # Implementation to get CPU usage
    # Could use system commands or monitoring libraries
    0.0
  end

  def get_memory_usage
    # Implementation to get memory usage
    0.0
  end

  def get_disk_usage
    # Implementation to get disk usage
    0.0
  end

  def get_network_io
    # Implementation to get network I/O
    0
  end

  def store_request_metrics(method, path, status, duration)
    now = Time.now.to_i
    pipeline = @redis.pipeline

    pipeline.hincrby("metrics:requests:#{now}", "#{method}:#{path}:#{status}", 1)
    pipeline.lpush("metrics:durations:#{now}", duration)
    pipeline.expire("metrics:requests:#{now}", 3600)
    pipeline.expire("metrics:durations:#{now}", 3600)

    pipeline.exec
  end

  def store_database_metrics(query_type, table, duration, rows_affected)
    now = Time.now.to_i
    pipeline = @redis.pipeline

    pipeline.hincrby("metrics:db:#{now}", "#{query_type}:#{table}", 1)
    pipeline.lpush("metrics:db_durations:#{now}", duration)
    pipeline.expire("metrics:db:#{now}", 3600)
    pipeline.expire("metrics:db_durations:#{now}", 3600)

    pipeline.exec
  end

  def store_cache_metrics(operation, cache_layer, duration, hit)
    now = Time.now.to_i
    pipeline = @redis.pipeline

    pipeline.hincrby("metrics:cache:#{now}", "#{operation}:#{cache_layer}", 1)
    pipeline.lpush("metrics:cache_durations:#{now}", duration)
    pipeline.expire("metrics:cache:#{now}", 3600)
    pipeline.expire("metrics:cache_durations:#{now}", 3600)

    pipeline.exec
  end

  def store_business_metrics(metric_name, value, labels)
    now = Time.now.to_i
    @redis.hset("metrics:business:#{now}", metric_name, value)
    @redis.expire("metrics:business:#{now}", 3600)
  end

  def store_error_metrics(error_type, error_message, stack_trace)
    now = Time.now.to_i
    pipeline = @redis.pipeline

    pipeline.hincrby("metrics:errors:#{now}", error_type, 1)
    pipeline.lpush("metrics:error_messages:#{now}", error_message)
    pipeline.expire("metrics:errors:#{now}", 3600)
    pipeline.expire("metrics:error_messages:#{now}", 3600)

    pipeline.exec
  end

  def store_system_metrics(cpu_usage, memory_usage, disk_usage, network_io)
    now = Time.now.to_i
    @redis.hmset("metrics:system:#{now}",
      "cpu", cpu_usage,
      "memory", memory_usage,
      "disk", disk_usage,
      "network", network_io
    )
    @redis.expire("metrics:system:#{now}", 3600)
  end

  def get_http_metrics_summary
    now = Time.now.to_i
    requests = @redis.hgetall("metrics:requests:#{now}")
    durations = @redis.lrange("metrics:durations:#{now}", 0, -1).map(&:to_f)

    {
      total_requests: requests.values.sum(&:to_i),
      average_duration: durations.empty? ? 0 : durations.sum / durations.length,
      requests_by_status: group_by_status(requests)
    }
  end

  def get_database_metrics_summary
    now = Time.now.to_i
    queries = @redis.hgetall("metrics:db:#{now}")
    durations = @redis.lrange("metrics:db_durations:#{now}", 0, -1).map(&:to_f)

    {
      total_queries: queries.values.sum(&:to_i),
      average_duration: durations.empty? ? 0 : durations.sum / durations.length,
      queries_by_type: group_by_type(queries)
    }
  end

  def get_cache_metrics_summary
    now = Time.now.to_i
    operations = @redis.hgetall("metrics:cache:#{now}")
    durations = @redis.lrange("metrics:cache_durations:#{now}", 0, -1).map(&:to_f)

    {
      total_operations: operations.values.sum(&:to_i),
      average_duration: durations.empty? ? 0 : durations.sum / durations.length,
      operations_by_layer: group_by_layer(operations)
    }
  end

  def get_error_metrics_summary
    now = Time.now.to_i
    errors = @redis.hgetall("metrics:errors:#{now}")

    {
      total_errors: errors.values.sum(&:to_i),
      errors_by_type: errors
    }
  end

  def get_system_metrics_summary
    now = Time.now.to_i
    system = @redis.hgetall("metrics:system:#{now}")

    {
      cpu_usage: system['cpu'].to_f,
      memory_usage: system['memory'].to_f,
      disk_usage: system['disk'].to_f,
      network_io: system['network'].to_i
    }
  end

  def group_by_status(requests)
    requests.group_by { |key, _| key.split(':').last }
  end

  def group_by_type(queries)
    queries.group_by { |key, _| key.split(':').first }
  end

  def group_by_layer(operations)
    operations.group_by { |key, _| key.split(':').last }
  end
end
```

## 🚨 **Intelligent Alerting System**

### Advanced Alert Manager

```ruby
# lib/alert_manager.rb
require 'tusk'
require 'redis'
require 'json'
require 'net/http'
require 'uri'

class AlertManager
  def initialize(config_path = 'config/monitoring.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    setup_alert_rules
    start_alert_monitoring
  end

  def check_alert_conditions
    @alert_rules.each do |rule|
      check_rule(rule)
    end
  end

  def create_alert(severity, message, details = {})
    alert = {
      id: SecureRandom.uuid,
      severity: severity,
      message: message,
      details: details,
      timestamp: Time.now.iso8601,
      instance: get_instance_id,
      status: 'active'
    }

    store_alert(alert)
    send_alert_notifications(alert)
  end

  def resolve_alert(alert_id)
    alert = get_alert(alert_id)
    return unless alert

    alert['status'] = 'resolved'
    alert['resolved_at'] = Time.now.iso8601
    store_alert(alert)
  end

  def get_active_alerts
    alerts = @redis.hgetall('alerts:active')
    alerts.values.map { |alert| JSON.parse(alert) }
  end

  def get_alert_history(limit = 100)
    alerts = @redis.lrange('alerts:history', 0, limit - 1)
    alerts.map { |alert| JSON.parse(alert) }
  end

  private

  def setup_alert_rules
    @alert_rules = [
      {
        name: 'high_error_rate',
        condition: -> { get_error_rate > 5.0 },
        severity: 'critical',
        message: 'Error rate is above 5%',
        cooldown: 300
      },
      {
        name: 'high_response_time',
        condition: -> { get_average_response_time > 1000 },
        severity: 'warning',
        message: 'Average response time is above 1 second',
        cooldown: 300
      },
      {
        name: 'high_cpu_usage',
        condition: -> { get_cpu_usage > 80 },
        severity: 'warning',
        message: 'CPU usage is above 80%',
        cooldown: 300
      },
      {
        name: 'high_memory_usage',
        condition: -> { get_memory_usage > 85 },
        severity: 'critical',
        message: 'Memory usage is above 85%',
        cooldown: 300
      },
      {
        name: 'low_cache_hit_rate',
        condition: -> { get_cache_hit_rate < 70 },
        severity: 'warning',
        message: 'Cache hit rate is below 70%',
        cooldown: 600
      }
    ]
  end

  def start_alert_monitoring
    Thread.new do
      loop do
        check_alert_conditions
        sleep 60 # Check every minute
      end
    end
  end

  def check_rule(rule)
    return if rule_in_cooldown?(rule)

    if rule[:condition].call
      create_alert(rule[:severity], rule[:message], { rule: rule[:name] })
      set_rule_cooldown(rule)
    end
  end

  def rule_in_cooldown?(rule)
    cooldown_key = "alert_cooldown:#{rule[:name]}"
    @redis.exists(cooldown_key)
  end

  def set_rule_cooldown(rule)
    cooldown_key = "alert_cooldown:#{rule[:name]}"
    @redis.setex(cooldown_key, rule[:cooldown], '1')
  end

  def store_alert(alert)
    @redis.hset('alerts:active', alert['id'], alert.to_json)
    @redis.lpush('alerts:history', alert.to_json)
    @redis.ltrim('alerts:history', 0, 999) # Keep last 1000 alerts
  end

  def get_alert(alert_id)
    alert_data = @redis.hget('alerts:active', alert_id)
    return nil unless alert_data
    JSON.parse(alert_data)
  end

  def send_alert_notifications(alert)
    send_slack_alert(alert) if @config['alerting']['slack_webhook']
    send_email_alert(alert) if @config['alerting']['email_recipients']
    send_webhook_alert(alert) if @config['alerting']['webhook_url']
  end

  def send_slack_alert(alert)
    webhook_url = @config['alerting']['slack_webhook']
    return unless webhook_url

    payload = {
      text: format_slack_message(alert),
      attachments: [format_slack_attachment(alert)]
    }

    send_webhook(webhook_url, payload)
  end

  def send_email_alert(alert)
    recipients = @config['alerting']['email_recipients'].split(',')
    return if recipients.empty?

    # Implementation would use your email service
    Rails.logger.info "Email alert sent to #{recipients.join(', ')}: #{alert['message']}"
  end

  def send_webhook_alert(alert)
    webhook_url = @config['alerting']['webhook_url']
    return unless webhook_url

    send_webhook(webhook_url, alert)
  end

  def send_webhook(url, payload)
    uri = URI(url)
    http = Net::HTTP.new(uri.host, uri.port)
    http.use_ssl = uri.scheme == 'https'

    request = Net::HTTP::Post.new(uri)
    request['Content-Type'] = 'application/json'
    request.body = payload.to_json

    response = http.request(request)
    Rails.logger.info "Webhook sent: #{response.code}"
  rescue => e
    Rails.logger.error "Webhook error: #{e.message}"
  end

  def format_slack_message(alert)
    severity_emoji = case alert['severity']
                    when 'critical' then '🚨'
                    when 'warning' then '⚠️'
                    else 'ℹ️'
                    end

    "#{severity_emoji} #{alert['message']}"
  end

  def format_slack_attachment(alert)
    {
      color: alert['severity'] == 'critical' ? 'danger' : 'warning',
      fields: [
        {
          title: 'Severity',
          value: alert['severity'].upcase,
          short: true
        },
        {
          title: 'Instance',
          value: alert['instance'],
          short: true
        },
        {
          title: 'Timestamp',
          value: alert['timestamp'],
          short: true
        },
        {
          title: 'Details',
          value: alert['details'].to_json,
          short: false
        }
      ]
    }
  end

  def get_error_rate
    # Implementation to get current error rate
    0.0
  end

  def get_average_response_time
    # Implementation to get average response time
    0
  end

  def get_cpu_usage
    # Implementation to get CPU usage
    0.0
  end

  def get_memory_usage
    # Implementation to get memory usage
    0.0
  end

  def get_cache_hit_rate
    # Implementation to get cache hit rate
    0.0
  end

  def get_instance_id
    @instance_id ||= SecureRandom.uuid
  end
end
```

## 🔍 **Distributed Tracing**

### OpenTelemetry Integration

```ruby
# lib/distributed_tracer.rb
require 'opentelemetry/sdk'
require 'opentelemetry/exporter/jaeger'
require 'opentelemetry/instrumentation/all'
require 'tusk'

class DistributedTracer
  def initialize(config_path = 'config/monitoring.tsk')
    @config = Tusk.load(config_path)
    setup_tracing if @config['tracing']['enabled'] == 'true'
  end

  def trace_operation(operation_name, attributes = {}, &block)
    return yield unless @tracer

    @tracer.in_span(operation_name, attributes: attributes) do |span|
      begin
        result = yield
        span.set_status(OpenTelemetry::Trace::Status.ok)
        result
      rescue => e
        span.set_status(OpenTelemetry::Trace::Status.error(e.message))
        span.record_exception(e)
        raise e
      end
    end
  end

  def trace_http_request(method, url, headers = {}, &block)
    attributes = {
      'http.method' => method,
      'http.url' => url,
      'http.request.header.user_agent' => headers['User-Agent']
    }

    trace_operation("http.request", attributes, &block)
  end

  def trace_database_query(query, table, &block)
    attributes = {
      'db.statement' => query,
      'db.table' => table,
      'db.system' => 'postgresql'
    }

    trace_operation("database.query", attributes, &block)
  end

  def trace_cache_operation(operation, key, &block)
    attributes = {
      'cache.operation' => operation,
      'cache.key' => key
    }

    trace_operation("cache.operation", attributes, &block)
  end

  def trace_business_operation(operation_name, business_data = {}, &block)
    attributes = {
      'business.operation' => operation_name
    }.merge(business_data)

    trace_operation("business.operation", attributes, &block)
  end

  def add_span_event(event_name, attributes = {})
    current_span = OpenTelemetry::Trace.current_span
    current_span.add_event(event_name, attributes: attributes) if current_span
  end

  def set_span_attribute(key, value)
    current_span = OpenTelemetry::Trace.current_span
    current_span.set_attribute(key, value) if current_span
  end

  private

  def setup_tracing
    OpenTelemetry::SDK.configure do |c|
      c.service_name = @config['monitoring']['service_name']
      c.use_all
    end

    @tracer = OpenTelemetry.tracer_provider.tracer('tusklang-app')
  end
end
```

## 📈 **Performance Profiling**

### Application Profiler

```ruby
# lib/application_profiler.rb
require 'tusk'
require 'redis'
require 'json'

class ApplicationProfiler
  def initialize(config_path = 'config/monitoring.tsk')
    @config = Tusk.load(config_path)
    @redis = Redis.new(url: @config['redis']['url'])
    @profiles = {}
  end

  def profile_operation(operation_name, &block)
    start_time = Time.now
    start_memory = get_memory_usage

    result = block.call

    end_time = Time.now
    end_memory = get_memory_usage

    duration = (end_time - start_time) * 1000 # Convert to milliseconds
    memory_delta = end_memory - start_memory

    record_profile(operation_name, duration, memory_delta)

    result
  end

  def profile_method(klass, method_name)
    original_method = klass.instance_method(method_name)

    klass.define_method(method_name) do |*args, &block|
      profiler = ApplicationProfiler.new
      profiler.profile_operation("#{klass.name}##{method_name}") do
        original_method.bind(self).call(*args, &block)
      end
    end
  end

  def get_performance_summary
    {
      slowest_operations: get_slowest_operations,
      memory_intensive_operations: get_memory_intensive_operations,
      most_called_operations: get_most_called_operations,
      performance_trends: get_performance_trends
    }
  end

  def generate_performance_report
    summary = get_performance_summary
    report = {
      timestamp: Time.now.iso8601,
      summary: summary,
      recommendations: generate_recommendations(summary)
    }

    store_performance_report(report)
    report
  end

  private

  def record_profile(operation_name, duration, memory_delta)
    now = Time.now.to_i
    pipeline = @redis.pipeline

    # Record duration
    pipeline.lpush("profiles:#{operation_name}:durations:#{now}", duration)
    pipeline.expire("profiles:#{operation_name}:durations:#{now}", 3600)

    # Record memory usage
    pipeline.lpush("profiles:#{operation_name}:memory:#{now}", memory_delta)
    pipeline.expire("profiles:#{operation_name}:memory:#{now}", 3600)

    # Increment call count
    pipeline.hincrby("profiles:#{operation_name}:calls", now.to_s, 1)
    pipeline.expire("profiles:#{operation_name}:calls", 3600)

    pipeline.exec
  end

  def get_slowest_operations
    operations = get_all_operations
    slowest = {}

    operations.each do |operation|
      durations = get_operation_durations(operation)
      next if durations.empty?

      slowest[operation] = {
        average: durations.sum / durations.length,
        max: durations.max,
        min: durations.min,
        p95: calculate_percentile(durations, 95),
        p99: calculate_percentile(durations, 99)
      }
    end

    slowest.sort_by { |_, stats| -stats[:average] }.first(10)
  end

  def get_memory_intensive_operations
    operations = get_all_operations
    memory_intensive = {}

    operations.each do |operation|
      memory_usage = get_operation_memory_usage(operation)
      next if memory_usage.empty?

      memory_intensive[operation] = {
        average: memory_usage.sum / memory_usage.length,
        max: memory_usage.max,
        min: memory_usage.min
      }
    end

    memory_intensive.sort_by { |_, stats| -stats[:average] }.first(10)
  end

  def get_most_called_operations
    operations = get_all_operations
    call_counts = {}

    operations.each do |operation|
      calls = get_operation_call_count(operation)
      call_counts[operation] = calls.sum
    end

    call_counts.sort_by { |_, count| -count }.first(10)
  end

  def get_performance_trends
    # Implementation to get performance trends over time
    {}
  end

  def get_all_operations
    keys = @redis.keys("profiles:*:durations:*")
    keys.map { |key| key.split(':')[1] }.uniq
  end

  def get_operation_durations(operation)
    now = Time.now.to_i
    durations = []

    60.times do |i|
      times = @redis.lrange("profiles:#{operation}:durations:#{now - i}", 0, -1)
      durations.concat(times.map(&:to_f))
    end

    durations
  end

  def get_operation_memory_usage(operation)
    now = Time.now.to_i
    memory_usage = []

    60.times do |i|
      usage = @redis.lrange("profiles:#{operation}:memory:#{now - i}", 0, -1)
      memory_usage.concat(usage.map(&:to_f))
    end

    memory_usage
  end

  def get_operation_call_count(operation)
    now = Time.now.to_i
    calls = []

    60.times do |i|
      count = @redis.hget("profiles:#{operation}:calls", (now - i).to_s)
      calls << count.to_i
    end

    calls
  end

  def calculate_percentile(values, percentile)
    sorted = values.sort
    index = (percentile / 100.0 * (sorted.length - 1)).round
    sorted[index]
  end

  def generate_recommendations(summary)
    recommendations = []

    # Check for slow operations
    summary[:slowest_operations].each do |operation, stats|
      if stats[:average] > 1000 # 1 second
        recommendations << {
          type: 'performance',
          operation: operation,
          issue: 'Slow operation',
          recommendation: "Consider optimizing #{operation} - average time: #{stats[:average].round(2)}ms"
        }
      end
    end

    # Check for memory-intensive operations
    summary[:memory_intensive_operations].each do |operation, stats|
      if stats[:average] > 10_000_000 # 10MB
        recommendations << {
          type: 'memory',
          operation: operation,
          issue: 'Memory intensive',
          recommendation: "Consider optimizing memory usage in #{operation} - average: #{stats[:average].round(2)} bytes"
        }
      end
    end

    recommendations
  end

  def store_performance_report(report)
    @redis.lpush('performance_reports', report.to_json)
    @redis.ltrim('performance_reports', 0, 99) # Keep last 100 reports
  end

  def get_memory_usage
    # Implementation to get current memory usage
    0
  end
end
```

## 🎯 **Configuration Management**

### Advanced Monitoring Configuration

```ruby
# config/advanced_monitoring.tsk
[monitoring]
enabled: @env("MONITORING_ENABLED", "true")
mode: @env("MONITORING_MODE", "production") # development, staging, production
service_name: @env("SERVICE_NAME", "tusklang-app")
version: @env("APP_VERSION", "1.0.0")
instance_id: @env("INSTANCE_ID", "auto")

[metrics]
collection_interval: @env("METRICS_COLLECTION_INTERVAL", "15")
retention_period: @env("METRICS_RETENTION_PERIOD", "30d")
compression_enabled: @env("METRICS_COMPRESSION", "true")
custom_metrics_enabled: @env("CUSTOM_METRICS_ENABLED", "true")

[prometheus]
enabled: @env("PROMETHEUS_ENABLED", "true")
port: @env("PROMETHEUS_PORT", "9090")
path: @env("PROMETHEUS_PATH", "/metrics")
gateway_url: @env("PROMETHEUS_GATEWAY_URL", "")
push_enabled: @env("PROMETHEUS_PUSH_ENABLED", "false")

[alerting]
enabled: @env("ALERTING_ENABLED", "true")
slack_webhook: @env.secure("SLACK_WEBHOOK_URL")
email_recipients: @env("ALERT_EMAIL_RECIPIENTS", "admin@example.com")
webhook_url: @env.secure("ALERT_WEBHOOK_URL")
pagerduty_key: @env.secure("PAGERDUTY_KEY")

[alert_rules]
error_rate_threshold: @env("ERROR_RATE_THRESHOLD", "5.0")
response_time_threshold: @env("RESPONSE_TIME_THRESHOLD", "1000")
cpu_usage_threshold: @env("CPU_USAGE_THRESHOLD", "80")
memory_usage_threshold: @env("MEMORY_USAGE_THRESHOLD", "85")
cache_hit_rate_threshold: @env("CACHE_HIT_RATE_THRESHOLD", "70")

[tracing]
enabled: @env("TRACING_ENABLED", "true")
jaeger_endpoint: @env("JAEGER_ENDPOINT", "http://localhost:14268")
sampling_rate: @env("TRACING_SAMPLING_RATE", "0.1")
max_trace_duration: @env("MAX_TRACE_DURATION", "300")

[profiling]
enabled: @env("PROFILING_ENABLED", "true")
sample_rate: @env("PROFILING_SAMPLE_RATE", "0.01")
max_profiles: @env("MAX_PROFILES", "1000")
retention_period: @env("PROFILING_RETENTION_PERIOD", "7d")

[logging]
level: @env("LOG_LEVEL", "INFO")
format: @env("LOG_FORMAT", "json") # json, text
structured_logging: @env("STRUCTURED_LOGGING", "true")
correlation_id_enabled: @env("CORRELATION_ID_ENABLED", "true")
```

## 🎯 **Summary**

This comprehensive guide covers advanced monitoring with TuskLang and Ruby, including:

- **Metrics Collection**: Comprehensive Prometheus metrics with custom business metrics
- **Alerting System**: Intelligent alerting with multiple notification channels
- **Distributed Tracing**: OpenTelemetry integration for request tracing
- **Performance Profiling**: Application profiling and optimization recommendations
- **Configuration Management**: Advanced monitoring configuration options
- **Real-time Monitoring**: Live metrics and performance tracking
- **Error Tracking**: Comprehensive error monitoring and alerting
- **System Monitoring**: CPU, memory, disk, and network monitoring

The advanced monitoring capabilities with TuskLang provide complete observability into your Ruby applications, enabling proactive monitoring, rapid debugging, and continuous performance optimization. 
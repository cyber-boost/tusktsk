# 📊 Advanced Metrics with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Build sophisticated metrics and monitoring systems with TuskLang's advanced metrics features. From custom metrics collection to real-time dashboards, TuskLang provides the flexibility and power you need to monitor your Ruby applications effectively.

## 🚀 Quick Start

### Basic Metrics Setup
```ruby
require 'tusklang'
require 'tusklang/metrics'

# Initialize metrics system
metrics_system = TuskLang::Metrics::System.new

# Configure metrics
metrics_system.configure do |config|
  config.default_backend = 'prometheus'
  config.collection_interval = 15.seconds
  config.retention_period = 30.days
  config.aggregation_enabled = true
end

# Register metrics strategies
metrics_system.register_strategy(:prometheus, TuskLang::Metrics::Strategies::PrometheusStrategy.new)
metrics_system.register_strategy(:statsd, TuskLang::Metrics::Strategies::StatsDStrategy.new)
metrics_system.register_strategy(:custom, TuskLang::Metrics::Strategies::CustomMetricsStrategy.new)
```

### TuskLang Configuration
```tsk
# config/metrics.tsk
[metrics]
enabled: true
default_backend: "prometheus"
collection_interval: "15s"
retention_period: "30d"
aggregation_enabled: true

[metrics.backends]
prometheus: {
    enabled: true,
    port: 9090,
    path: "/metrics",
    labels: ["app", "version", "environment"]
}
statsd: {
    enabled: false,
    host: "localhost",
    port: 8125,
    prefix: "myapp"
}
influxdb: {
    enabled: false,
    url: "http://localhost:8086",
    database: "metrics",
    retention_policy: "30d"
}

[metrics.collectors]
system: {
    enabled: true,
    interval: "60s",
    metrics: ["cpu", "memory", "disk", "network"]
}
application: {
    enabled: true,
    interval: "30s",
    metrics: ["requests", "errors", "response_time", "throughput"]
}
business: {
    enabled: true,
    interval: "5m",
    metrics: ["users", "orders", "revenue", "conversions"]
}

[metrics.alerts]
cpu_high: {
    condition: "cpu_usage > 80",
    duration: "5m",
    severity: "warning"
}
memory_critical: {
    condition: "memory_usage > 95",
    duration: "2m",
    severity: "critical"
}
error_rate_high: {
    condition: "error_rate > 5",
    duration: "10m",
    severity: "warning"
}
```

## 🎯 Core Features

### 1. Prometheus Metrics Strategy
```ruby
require 'tusklang/metrics'
require 'prometheus/client'

class PrometheusStrategy
  include TuskLang::Metrics::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/metrics.tsk')
    @registry = Prometheus::Client.registry
    @metrics = {}
    setup_metrics
  end
  
  def increment(metric_name, value = 1, labels = {})
    metric = get_or_create_counter(metric_name)
    metric.increment(labels: labels, by: value)
  end
  
  def gauge(metric_name, value, labels = {})
    metric = get_or_create_gauge(metric_name)
    metric.set(value, labels: labels)
  end
  
  def histogram(metric_name, value, labels = {})
    metric = get_or_create_histogram(metric_name)
    metric.observe(value, labels: labels)
  end
  
  def summary(metric_name, value, labels = {})
    metric = get_or_create_summary(metric_name)
    metric.observe(value, labels: labels)
  end
  
  def get_metrics
    @registry.metrics
  end
  
  def export_metrics
    @registry.metrics.map do |metric|
      {
        name: metric.name,
        type: metric.type,
        help: metric.help,
        samples: metric.values.map do |labels, value|
          {
            labels: labels,
            value: value
          }
        end
      }
    end
  end
  
  private
  
  def setup_metrics
    # Setup default metrics
    setup_system_metrics
    setup_application_metrics
    setup_business_metrics
  end
  
  def setup_system_metrics
    if @config['metrics']['collectors']['system']['enabled']
      @metrics['cpu_usage'] = @registry.gauge(:cpu_usage, docstring: 'CPU usage percentage')
      @metrics['memory_usage'] = @registry.gauge(:memory_usage, docstring: 'Memory usage percentage')
      @metrics['disk_usage'] = @registry.gauge(:disk_usage, docstring: 'Disk usage percentage')
      @metrics['network_bytes'] = @registry.counter(:network_bytes_total, docstring: 'Total network bytes')
    end
  end
  
  def setup_application_metrics
    if @config['metrics']['collectors']['application']['enabled']
      @metrics['http_requests_total'] = @registry.counter(:http_requests_total, docstring: 'Total HTTP requests')
      @metrics['http_request_duration'] = @registry.histogram(:http_request_duration_seconds, docstring: 'HTTP request duration')
      @metrics['http_errors_total'] = @registry.counter(:http_errors_total, docstring: 'Total HTTP errors')
      @metrics['active_connections'] = @registry.gauge(:active_connections, docstring: 'Active connections')
    end
  end
  
  def setup_business_metrics
    if @config['metrics']['collectors']['business']['enabled']
      @metrics['users_total'] = @registry.counter(:users_total, docstring: 'Total users')
      @metrics['orders_total'] = @registry.counter(:orders_total, docstring: 'Total orders')
      @metrics['revenue_total'] = @registry.counter(:revenue_total, docstring: 'Total revenue')
      @metrics['conversion_rate'] = @registry.gauge(:conversion_rate, docstring: 'Conversion rate')
    end
  end
  
  def get_or_create_counter(name)
    @metrics[name] ||= @registry.counter(name.to_sym, docstring: "Counter for #{name}")
  end
  
  def get_or_create_gauge(name)
    @metrics[name] ||= @registry.gauge(name.to_sym, docstring: "Gauge for #{name}")
  end
  
  def get_or_create_histogram(name)
    @metrics[name] ||= @registry.histogram(name.to_sym, docstring: "Histogram for #{name}")
  end
  
  def get_or_create_summary(name)
    @metrics[name] ||= @registry.summary(name.to_sym, docstring: "Summary for #{name}")
  end
end
```

### 2. StatsD Metrics Strategy
```ruby
require 'tusklang/metrics'
require 'statsd'

class StatsDStrategy
  include TuskLang::Metrics::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/metrics.tsk')
    setup_statsd_client
  end
  
  def increment(metric_name, value = 1, labels = {})
    metric = build_metric_name(metric_name, labels)
    @statsd.increment(metric, value)
  end
  
  def gauge(metric_name, value, labels = {})
    metric = build_metric_name(metric_name, labels)
    @statsd.gauge(metric, value)
  end
  
  def histogram(metric_name, value, labels = {})
    metric = build_metric_name(metric_name, labels)
    @statsd.histogram(metric, value)
  end
  
  def timing(metric_name, value, labels = {})
    metric = build_metric_name(metric_name, labels)
    @statsd.timing(metric, value)
  end
  
  def set(metric_name, value, labels = {})
    metric = build_metric_name(metric_name, labels)
    @statsd.set(metric, value)
  end
  
  private
  
  def setup_statsd_client
    statsd_config = @config['metrics']['backends']['statsd']
    @statsd = Statsd.new(
      statsd_config['host'],
      statsd_config['port'],
      prefix: statsd_config['prefix']
    )
  end
  
  def build_metric_name(name, labels)
    metric_name = name.to_s
    
    unless labels.empty?
      label_strings = labels.map { |k, v| "#{k}=#{v}" }
      metric_name += ".#{label_strings.join('.')}"
    end
    
    metric_name
  end
end
```

### 3. Custom Metrics Strategy
```ruby
require 'tusklang/metrics'

class CustomMetricsStrategy
  include TuskLang::Metrics::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/metrics.tsk')
    @metrics = {}
    @aggregations = {}
    setup_aggregations
  end
  
  def increment(metric_name, value = 1, labels = {})
    key = build_metric_key(metric_name, labels)
    @metrics[key] ||= { count: 0, sum: 0, min: Float::INFINITY, max: -Float::INFINITY, values: [] }
    
    metric = @metrics[key]
    metric[:count] += value
    metric[:sum] += value
    metric[:min] = [metric[:min], value].min
    metric[:max] = [metric[:max], value].max
    metric[:values] << { value: value, timestamp: Time.now }
    
    # Keep only recent values
    cutoff_time = Time.now - 1.hour
    metric[:values].reject! { |v| v[:timestamp] < cutoff_time }
  end
  
  def gauge(metric_name, value, labels = {})
    key = build_metric_key(metric_name, labels)
    @metrics[key] = { value: value, timestamp: Time.now }
  end
  
  def histogram(metric_name, value, labels = {})
    key = build_metric_key(metric_name, labels)
    @metrics[key] ||= { buckets: {}, count: 0, sum: 0 }
    
    metric = @metrics[key]
    metric[:count] += 1
    metric[:sum] += value
    
    # Update histogram buckets
    bucket = calculate_bucket(value)
    metric[:buckets][bucket] ||= 0
    metric[:buckets][bucket] += 1
  end
  
  def get_metric(metric_name, labels = {})
    key = build_metric_key(metric_name, labels)
    @metrics[key]
  end
  
  def get_aggregated_metrics
    aggregated = {}
    
    @metrics.each do |key, metric|
      metric_name, label_string = parse_metric_key(key)
      
      if @aggregations[metric_name]
        aggregated[metric_name] ||= {}
        aggregated[metric_name][label_string] = calculate_aggregation(metric, @aggregations[metric_name])
      end
    end
    
    aggregated
  end
  
  def export_metrics
    {
      metrics: @metrics,
      aggregated: get_aggregated_metrics,
      timestamp: Time.now.iso8601
    }
  end
  
  private
  
  def setup_aggregations
    @aggregations = {
      'http_request_duration' => ['avg', 'p95', 'p99'],
      'response_time' => ['avg', 'p95', 'p99'],
      'error_rate' => ['avg', 'sum'],
      'throughput' => ['avg', 'sum']
    }
  end
  
  def build_metric_key(name, labels)
    if labels.empty?
      name.to_s
    else
      label_string = labels.map { |k, v| "#{k}=#{v}" }.join(',')
      "#{name}{#{label_string}}"
    end
  end
  
  def parse_metric_key(key)
    if key.include?('{')
      name, labels = key.split('{', 2)
      [name, labels.chomp('}')]
    else
      [key, '']
    end
  end
  
  def calculate_bucket(value)
    case value
    when 0..10
      '0-10'
    when 11..50
      '11-50'
    when 51..100
      '51-100'
    when 101..500
      '101-500'
    else
      '500+'
    end
  end
  
  def calculate_aggregation(metric, aggregations)
    result = {}
    
    aggregations.each do |agg|
      case agg
      when 'avg'
        result[:avg] = metric[:sum] / metric[:count] if metric[:count] > 0
      when 'sum'
        result[:sum] = metric[:sum]
      when 'min'
        result[:min] = metric[:min]
      when 'max'
        result[:max] = metric[:max]
      when 'p95'
        result[:p95] = calculate_percentile(metric[:values], 95)
      when 'p99'
        result[:p99] = calculate_percentile(metric[:values], 99)
      end
    end
    
    result
  end
  
  def calculate_percentile(values, percentile)
    return nil if values.empty?
    
    sorted_values = values.map { |v| v[:value] }.sort
    index = (percentile / 100.0 * (sorted_values.length - 1)).round
    sorted_values[index]
  end
end
```

### 4. Metrics Collection System
```ruby
require 'tusklang/metrics'

class MetricsCollector
  def initialize
    @config = TuskLang.parse_file('config/metrics.tsk')
    @metrics_system = TuskLang::Metrics::System.new
    @collectors = {}
    setup_collectors
  end
  
  def start_collection
    @collectors.each do |name, collector|
      start_collector(name, collector)
    end
  end
  
  def stop_collection
    @collectors.each do |name, collector|
      stop_collector(name, collector)
    end
  end
  
  def collect_system_metrics
    return unless @config['metrics']['collectors']['system']['enabled']
    
    # CPU usage
    cpu_usage = get_cpu_usage
    @metrics_system.gauge('cpu_usage', cpu_usage, { type: 'system' })
    
    # Memory usage
    memory_usage = get_memory_usage
    @metrics_system.gauge('memory_usage', memory_usage, { type: 'system' })
    
    # Disk usage
    disk_usage = get_disk_usage
    @metrics_system.gauge('disk_usage', disk_usage, { type: 'system' })
    
    # Network usage
    network_bytes = get_network_bytes
    @metrics_system.increment('network_bytes_total', network_bytes, { type: 'system' })
  end
  
  def collect_application_metrics
    return unless @config['metrics']['collectors']['application']['enabled']
    
    # Request metrics
    request_count = get_request_count
    @metrics_system.increment('http_requests_total', request_count, { type: 'application' })
    
    # Error metrics
    error_count = get_error_count
    @metrics_system.increment('http_errors_total', error_count, { type: 'application' })
    
    # Response time metrics
    response_time = get_average_response_time
    @metrics_system.histogram('http_request_duration_seconds', response_time, { type: 'application' })
    
    # Connection metrics
    active_connections = get_active_connections
    @metrics_system.gauge('active_connections', active_connections, { type: 'application' })
  end
  
  def collect_business_metrics
    return unless @config['metrics']['collectors']['business']['enabled']
    
    # User metrics
    user_count = get_user_count
    @metrics_system.gauge('users_total', user_count, { type: 'business' })
    
    # Order metrics
    order_count = get_order_count
    @metrics_system.increment('orders_total', order_count, { type: 'business' })
    
    # Revenue metrics
    revenue = get_revenue
    @metrics_system.increment('revenue_total', revenue, { type: 'business' })
    
    # Conversion metrics
    conversion_rate = get_conversion_rate
    @metrics_system.gauge('conversion_rate', conversion_rate, { type: 'business' })
  end
  
  private
  
  def setup_collectors
    @collectors = {
      system: {
        interval: parse_duration(@config['metrics']['collectors']['system']['interval']),
        enabled: @config['metrics']['collectors']['system']['enabled']
      },
      application: {
        interval: parse_duration(@config['metrics']['collectors']['application']['interval']),
        enabled: @config['metrics']['collectors']['application']['enabled']
      },
      business: {
        interval: parse_duration(@config['metrics']['collectors']['business']['interval']),
        enabled: @config['metrics']['collectors']['business']['enabled']
      }
    }
  end
  
  def start_collector(name, collector)
    return unless collector[:enabled]
    
    Thread.new do
      loop do
        case name
        when :system
          collect_system_metrics
        when :application
          collect_application_metrics
        when :business
          collect_business_metrics
        end
        
        sleep collector[:interval]
      end
    end
  end
  
  def stop_collector(name, collector)
    # Implementation to stop collector thread
  end
  
  def get_cpu_usage
    # Implementation to get CPU usage
    # This could use /proc/stat on Linux or other system calls
    0.0
  end
  
  def get_memory_usage
    # Implementation to get memory usage
    if defined?(GC)
      stats = GC.stat
      (stats[:heap_allocated_pages] * stats[:heap_page_size] / 1024.0 / 1024.0).round(2)
    else
      0.0
    end
  end
  
  def get_disk_usage
    # Implementation to get disk usage
    0.0
  end
  
  def get_network_bytes
    # Implementation to get network bytes
    0
  end
  
  def get_request_count
    # Implementation to get request count
    0
  end
  
  def get_error_count
    # Implementation to get error count
    0
  end
  
  def get_average_response_time
    # Implementation to get average response time
    0.0
  end
  
  def get_active_connections
    # Implementation to get active connections
    0
  end
  
  def get_user_count
    # Implementation to get user count
    User.count
  end
  
  def get_order_count
    # Implementation to get order count
    Order.count
  end
  
  def get_revenue
    # Implementation to get revenue
    0.0
  end
  
  def get_conversion_rate
    # Implementation to get conversion rate
    0.0
  end
  
  def parse_duration(duration_string)
    case duration_string
    when /(\d+)s/
      $1.to_i
    when /(\d+)m/
      $1.to_i * 60
    when /(\d+)h/
      $1.to_i * 3600
    else
      60 # Default 1 minute
    end
  end
end
```

### 5. Metrics Middleware
```ruby
require 'tusklang/metrics'

class MetricsMiddleware
  def initialize(app)
    @app = app
    @metrics_system = TuskLang::Metrics::System.new
    @config = TuskLang.parse_file('config/metrics.tsk')
  end
  
  def call(env)
    request = Rack::Request.new(env)
    start_time = Time.now
    
    # Record request start
    @metrics_system.increment('http_requests_total', 1, {
      method: request.method,
      path: request.path,
      status: 'started'
    })
    
    # Process request
    status, headers, response = @app.call(env)
    
    # Calculate duration
    duration = Time.now - start_time
    
    # Record request completion
    @metrics_system.increment('http_requests_total', 1, {
      method: request.method,
      path: request.path,
      status: status.to_s
    })
    
    # Record response time
    @metrics_system.histogram('http_request_duration_seconds', duration, {
      method: request.method,
      path: request.path
    })
    
    # Record errors
    if status >= 400
      @metrics_system.increment('http_errors_total', 1, {
        method: request.method,
        path: request.path,
        status: status.to_s
      })
    end
    
    [status, headers, response]
  rescue => e
    # Record error
    @metrics_system.increment('http_errors_total', 1, {
      method: request.method,
      path: request.path,
      error: e.class.name
    })
    
    raise e
  end
end
```

### 6. Metrics Decorators
```ruby
require 'tusklang/metrics'

module MetricsDecorators
  def self.included(base)
    base.extend(ClassMethods)
  end
  
  module ClassMethods
    def track_metrics(*methods, **options)
      methods.each do |method|
        define_method("#{method}_with_metrics") do |*args, &block|
          metrics_system = TuskLang::Metrics::System.new
          start_time = Time.now
          
          begin
            result = send("#{method}_without_metrics", *args, &block)
            
            # Record success
            duration = Time.now - start_time
            metrics_system.increment("#{self.class.name.downcase}_#{method}_total", 1, { status: 'success' })
            metrics_system.histogram("#{self.class.name.downcase}_#{method}_duration", duration, { status: 'success' })
            
            result
          rescue => e
            # Record error
            duration = Time.now - start_time
            metrics_system.increment("#{self.class.name.downcase}_#{method}_total", 1, { status: 'error' })
            metrics_system.histogram("#{self.class.name.downcase}_#{method}_duration", duration, { status: 'error' })
            metrics_system.increment("#{self.class.name.downcase}_#{method}_errors", 1, { error: e.class.name })
            
            raise e
          end
        end
        
        alias_method "#{method}_without_metrics", method
        alias_method method, "#{method}_with_metrics"
      end
    end
  end
end

# Usage in models
class User < ApplicationRecord
  include MetricsDecorators
  
  track_metrics :create, :update, :destroy
  
  def self.find_by_email_with_metrics(email)
    metrics_system = TuskLang::Metrics::System.new
    start_time = Time.now
    
    begin
      result = find_by_email_without_metrics(email)
      
      duration = Time.now - start_time
      metrics_system.increment('user_find_by_email_total', 1, { found: result ? 'yes' : 'no' })
      metrics_system.histogram('user_find_by_email_duration', duration, { found: result ? 'yes' : 'no' })
      
      result
    rescue => e
      duration = Time.now - start_time
      metrics_system.increment('user_find_by_email_errors', 1, { error: e.class.name })
      metrics_system.histogram('user_find_by_email_duration', duration, { error: e.class.name })
      
      raise e
    end
  end
  
  alias_method :find_by_email_without_metrics, :find_by_email
  alias_method :find_by_email, :find_by_email_with_metrics
end
```

## 🔧 Advanced Configuration

### Metrics Aggregation
```ruby
require 'tusklang/metrics'

class MetricsAggregator
  def initialize
    @config = TuskLang.parse_file('config/metrics.tsk')
    @aggregations = {}
    setup_aggregations
  end
  
  def aggregate_metrics(metrics, interval = '1m')
    aggregated = {}
    
    metrics.each do |metric_name, values|
      if @aggregations[metric_name]
        aggregated[metric_name] = calculate_aggregation(values, @aggregations[metric_name], interval)
      end
    end
    
    aggregated
  end
  
  def calculate_aggregation(values, aggregation_rules, interval)
    result = {}
    
    aggregation_rules.each do |rule|
      case rule
      when 'avg'
        result[:avg] = values.sum / values.length if values.any?
      when 'sum'
        result[:sum] = values.sum
      when 'min'
        result[:min] = values.min
      when 'max'
        result[:max] = values.max
      when 'count'
        result[:count] = values.length
      when 'p95'
        result[:p95] = calculate_percentile(values, 95)
      when 'p99'
        result[:p99] = calculate_percentile(values, 99)
      end
    end
    
    result
  end
  
  private
  
  def setup_aggregations
    @aggregations = {
      'http_request_duration' => ['avg', 'p95', 'p99'],
      'response_time' => ['avg', 'p95', 'p99'],
      'error_rate' => ['avg', 'sum'],
      'throughput' => ['avg', 'sum']
    }
  end
  
  def calculate_percentile(values, percentile)
    return nil if values.empty?
    
    sorted_values = values.sort
    index = (percentile / 100.0 * (sorted_values.length - 1)).round
    sorted_values[index]
  end
end
```

## 🚀 Performance Optimization

### Metrics Caching
```ruby
require 'tusklang/metrics'

class MetricsCache
  def initialize
    @cache = TuskLang::Cache::RedisCache.new
    @config = TuskLang.parse_file('config/metrics.tsk')
  end
  
  def cache_metric(key, value, ttl = 300)
    @cache.set(key, value, ttl)
  end
  
  def get_cached_metric(key)
    @cache.get(key)
  end
  
  def cache_aggregation(metric_name, aggregation, ttl = 3600)
    key = "metrics_agg:#{metric_name}:#{aggregation}"
    @cache.set(key, aggregation, ttl)
  end
  
  def get_cached_aggregation(metric_name, aggregation)
    key = "metrics_agg:#{metric_name}:#{aggregation}"
    @cache.get(key)
  end
end
```

## 📊 Monitoring and Analytics

### Metrics Analytics
```ruby
require 'tusklang/metrics'

class MetricsAnalytics
  def initialize
    @metrics = TuskLang::Metrics::Collector.new
  end
  
  def track_metric(metric_name, value, context = {})
    @metrics.increment("metrics.tracked.total")
    @metrics.increment("metrics.tracked.#{metric_name}")
    
    if context[:type]
      @metrics.increment("metrics.tracked.type.#{context[:type]}")
    end
  end
  
  def track_aggregation(metric_name, aggregation_type, value)
    @metrics.increment("metrics.aggregations.total")
    @metrics.increment("metrics.aggregations.#{aggregation_type}")
    @metrics.histogram("metrics.aggregation_values.#{aggregation_type}", value)
  end
  
  def get_metrics_stats
    {
      total_metrics: @metrics.get("metrics.tracked.total"),
      metrics_by_type: @metrics.get_top("metrics.tracked.type", 5),
      popular_metrics: @metrics.get_top("metrics.tracked", 10),
      aggregation_count: @metrics.get("metrics.aggregations.total")
    }
  end
end
```

This comprehensive metrics system provides enterprise-grade monitoring features while maintaining the flexibility and power of TuskLang. The combination of multiple metrics backends, custom aggregations, and performance optimizations creates a robust foundation for monitoring Ruby applications. 
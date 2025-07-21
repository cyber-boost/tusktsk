# Microservices with TuskLang and Ruby

## 🏗️ **Building Distributed Systems with Grace**

TuskLang enables you to build sophisticated microservices architectures with Ruby, providing seamless service discovery, configuration management, and inter-service communication. Create resilient, scalable systems that adapt to your needs.

## 🚀 **Quick Start: Service Discovery**

### Basic Service Configuration

```ruby
# config/services.tsk
[service:user-service]
host: @env("USER_SERVICE_HOST", "localhost")
port: @env("USER_SERVICE_PORT", 3001)
health_check: @http("GET", "http://#{host}:#{port}/health")
version: @env("USER_SERVICE_VERSION", "1.0.0")

[service:payment-service]
host: @env("PAYMENT_SERVICE_HOST", "localhost")
port: @env("PAYMENT_SERVICE_PORT", 3002)
health_check: @http("GET", "http://#{host}:#{port}/health")
version: @env("PAYMENT_SERVICE_VERSION", "1.0.0")

[service:notification-service]
host: @env("NOTIFICATION_SERVICE_HOST", "localhost")
port: @env("NOTIFICATION_SERVICE_PORT", 3003)
health_check: @http("GET", "http://#{host}:#{port}/health")
version: @env("NOTIFICATION_SERVICE_VERSION", "1.0.0")
```

### Service Registry Implementation

```ruby
# lib/service_registry.rb
require 'tusk'
require 'net/http'
require 'json'

class ServiceRegistry
  def initialize(config_path = 'config/services.tsk')
    @config = Tusk.load(config_path)
    @services = {}
    @health_checks = {}
  end

  def register_service(name, service_config)
    @services[name] = {
      host: service_config['host'],
      port: service_config['port'],
      version: service_config['version'],
      health_url: service_config['health_check'],
      last_check: Time.now,
      status: 'unknown'
    }
    
    schedule_health_check(name)
  end

  def get_service(name)
    service = @services[name]
    return nil unless service && service[:status] == 'healthy'
    service
  end

  def list_services
    @services.select { |_, service| service[:status] == 'healthy' }
  end

  private

  def schedule_health_check(name)
    Thread.new do
      loop do
        check_service_health(name)
        sleep 30 # Check every 30 seconds
      end
    end
  end

  def check_service_health(name)
    service = @services[name]
    return unless service

    begin
      uri = URI(service[:health_url])
      response = Net::HTTP.get_response(uri)
      
      @services[name][:status] = response.code == '200' ? 'healthy' : 'unhealthy'
      @services[name][:last_check] = Time.now
    rescue => e
      @services[name][:status] = 'unhealthy'
      @services[name][:last_check] = Time.now
      Rails.logger.error "Health check failed for #{name}: #{e.message}"
    end
  end
end
```

## 🔄 **Inter-Service Communication**

### HTTP Client with Circuit Breaker

```ruby
# lib/microservice_client.rb
require 'net/http'
require 'json'
require 'tusk'

class MicroserviceClient
  def initialize(service_name, config_path = 'config/services.tsk')
    @config = Tusk.load(config_path)
    @service_name = service_name
    @circuit_breaker = CircuitBreaker.new
  end

  def get(path, headers = {})
    @circuit_breaker.call do
      service = get_service_config
      uri = URI("http://#{service['host']}:#{service['port']}#{path}")
      
      request = Net::HTTP::Get.new(uri)
      headers.each { |key, value| request[key] = value }
      
      response = Net::HTTP.start(uri.hostname, uri.port) do |http|
        http.request(request)
      end
      
      handle_response(response)
    end
  end

  def post(path, data, headers = {})
    @circuit_breaker.call do
      service = get_service_config
      uri = URI("http://#{service['host']}:#{service['port']}#{path}")
      
      request = Net::HTTP::Post.new(uri)
      request['Content-Type'] = 'application/json'
      headers.each { |key, value| request[key] = value }
      request.body = data.to_json
      
      response = Net::HTTP.start(uri.hostname, uri.port) do |http|
        http.request(request)
      end
      
      handle_response(response)
    end
  end

  private

  def get_service_config
    @config["service:#{@service_name}"]
  end

  def handle_response(response)
    case response
    when Net::HTTPSuccess
      JSON.parse(response.body)
    when Net::HTTPClientError
      raise ClientError, "Client error: #{response.code}"
    when Net::HTTPServerError
      raise ServerError, "Server error: #{response.code}"
    else
      raise UnexpectedError, "Unexpected response: #{response.code}"
    end
  end
end

class CircuitBreaker
  def initialize(failure_threshold = 5, timeout = 60)
    @failure_threshold = failure_threshold
    @timeout = timeout
    @failure_count = 0
    @last_failure_time = nil
    @state = :closed
  end

  def call
    case @state
    when :open
      raise CircuitBreakerOpenError if Time.now - @last_failure_time < @timeout
      @state = :half_open
    end

    result = yield
    on_success
    result
  rescue => e
    on_failure
    raise e
  end

  private

  def on_success
    @failure_count = 0
    @state = :closed
  end

  def on_failure
    @failure_count += 1
    @last_failure_time = Time.now
    
    if @failure_count >= @failure_threshold
      @state = :open
    end
  end
end
```

## 📡 **Message Queue Integration**

### RabbitMQ Configuration

```ruby
# config/message_queue.tsk
[rabbitmq]
host: @env("RABBITMQ_HOST", "localhost")
port: @env("RABBITMQ_PORT", 5672)
username: @env("RABBITMQ_USERNAME", "guest")
password: @env("RABBITMQ_PASSWORD", "guest")
vhost: @env("RABBITMQ_VHOST", "/")

[queues]
user_events: user-service.events
payment_events: payment-service.events
notification_events: notification-service.events

[exchanges]
user_exchange: user.events
payment_exchange: payment.events
notification_exchange: notification.events
```

### Message Publisher

```ruby
# lib/message_publisher.rb
require 'bunny'
require 'json'
require 'tusk'

class MessagePublisher
  def initialize(config_path = 'config/message_queue.tsk')
    @config = Tusk.load(config_path)
    @connection = create_connection
    @channel = @connection.create_channel
    setup_exchanges
  end

  def publish_user_event(event_type, data)
    publish_event('user_exchange', event_type, data)
  end

  def publish_payment_event(event_type, data)
    publish_event('payment_exchange', event_type, data)
  end

  def publish_notification_event(event_type, data)
    publish_event('notification_exchange', event_type, data)
  end

  private

  def create_connection
    Bunny.new(
      host: @config['rabbitmq']['host'],
      port: @config['rabbitmq']['port'],
      user: @config['rabbitmq']['username'],
      pass: @config['rabbitmq']['password'],
      vhost: @config['rabbitmq']['vhost']
    )
  end

  def setup_exchanges
    @exchanges = {}
    @config['exchanges'].each do |name, exchange_name|
      @exchanges[name] = @channel.topic(exchange_name, durable: true)
    end
  end

  def publish_event(exchange_name, event_type, data)
    exchange = @exchanges[exchange_name]
    message = {
      event_type: event_type,
      data: data,
      timestamp: Time.now.iso8601,
      service: Rails.application.class.module_parent_name.underscore
    }

    exchange.publish(
      message.to_json,
      routing_key: event_type,
      persistent: true,
      content_type: 'application/json'
    )
  end
end
```

### Message Consumer

```ruby
# lib/message_consumer.rb
require 'bunny'
require 'json'
require 'tusk'

class MessageConsumer
  def initialize(service_name, config_path = 'config/message_queue.tsk')
    @config = Tusk.load(config_path)
    @service_name = service_name
    @connection = create_connection
    @channel = @connection.create_channel
    @queue_name = @config['queues']["#{service_name}_events"]
  end

  def start_consuming
    queue = @channel.queue(@queue_name, durable: true)
    
    # Bind to relevant exchanges
    bind_to_exchanges(queue)
    
    queue.subscribe(manual_ack: true) do |delivery_info, properties, payload|
      begin
        process_message(payload)
        @channel.ack(delivery_info.delivery_tag)
      rescue => e
        Rails.logger.error "Error processing message: #{e.message}"
        @channel.nack(delivery_info.delivery_tag, false, true)
      end
    end
  end

  private

  def create_connection
    Bunny.new(
      host: @config['rabbitmq']['host'],
      port: @config['rabbitmq']['port'],
      user: @config['rabbitmq']['username'],
      pass: @config['rabbitmq']['password'],
      vhost: @config['rabbitmq']['vhost']
    )
  end

  def bind_to_exchanges(queue)
    # Bind to exchanges based on service needs
    case @service_name
    when 'user-service'
      queue.bind(@channel.topic('user.events'), routing_key: '#')
    when 'payment-service'
      queue.bind(@channel.topic('payment.events'), routing_key: '#')
    when 'notification-service'
      queue.bind(@channel.topic('user.events'), routing_key: 'user.*')
      queue.bind(@channel.topic('payment.events'), routing_key: 'payment.*')
    end
  end

  def process_message(payload)
    message = JSON.parse(payload)
    event_type = message['event_type']
    data = message['data']
    
    case event_type
    when 'user.created'
      handle_user_created(data)
    when 'user.updated'
      handle_user_updated(data)
    when 'payment.completed'
      handle_payment_completed(data)
    when 'payment.failed'
      handle_payment_failed(data)
    else
      Rails.logger.warn "Unknown event type: #{event_type}"
    end
  end

  def handle_user_created(data)
    # Implementation specific to service
    Rails.logger.info "User created: #{data['user_id']}"
  end

  def handle_user_updated(data)
    # Implementation specific to service
    Rails.logger.info "User updated: #{data['user_id']}"
  end

  def handle_payment_completed(data)
    # Implementation specific to service
    Rails.logger.info "Payment completed: #{data['payment_id']}"
  end

  def handle_payment_failed(data)
    # Implementation specific to service
    Rails.logger.info "Payment failed: #{data['payment_id']}"
  end
end
```

## 🔐 **Service Authentication**

### JWT Token Management

```ruby
# config/auth.tsk
[auth]
jwt_secret: @env.secure("JWT_SECRET")
jwt_expiration: @env("JWT_EXPIRATION", "3600")
service_token_expiration: @env("SERVICE_TOKEN_EXPIRATION", "86400")

[services]
user_service_token: @env.secure("USER_SERVICE_TOKEN")
payment_service_token: @env.secure("PAYMENT_SERVICE_TOKEN")
notification_service_token: @env.secure("NOTIFICATION_SERVICE_TOKEN")
```

### Service Authentication Middleware

```ruby
# lib/service_auth.rb
require 'jwt'
require 'tusk'

class ServiceAuth
  def initialize(config_path = 'config/auth.tsk')
    @config = Tusk.load(config_path)
  end

  def generate_service_token(service_name, payload = {})
    secret = @config['auth']['jwt_secret']
    expiration = @config['auth']['service_token_expiration'].to_i
    
    payload.merge!(
      service: service_name,
      exp: Time.now.to_i + expiration,
      iat: Time.now.to_i
    )
    
    JWT.encode(payload, secret, 'HS256')
  end

  def verify_service_token(token)
    secret = @config['auth']['jwt_secret']
    decoded = JWT.decode(token, secret, true, { algorithm: 'HS256' })
    decoded[0]
  rescue JWT::DecodeError => e
    raise AuthenticationError, "Invalid token: #{e.message}"
  end

  def verify_service_request(request)
    auth_header = request.headers['Authorization']
    return false unless auth_header&.start_with?('Bearer ')
    
    token = auth_header.split(' ').last
    payload = verify_service_token(token)
    
    # Verify service name matches expected
    expected_service = determine_expected_service(request)
    payload['service'] == expected_service
  rescue => e
    Rails.logger.error "Service authentication failed: #{e.message}"
    false
  end

  private

  def determine_expected_service(request)
    # Determine expected service based on request path or other criteria
    case request.path
    when /^\/api\/users/
      'user-service'
    when /^\/api\/payments/
      'payment-service'
    when /^\/api\/notifications/
      'notification-service'
    else
      'unknown'
    end
  end
end
```

## 📊 **Distributed Tracing**

### OpenTelemetry Configuration

```ruby
# config/tracing.tsk
[tracing]
enabled: @env("TRACING_ENABLED", "true")
endpoint: @env("TRACING_ENDPOINT", "http://localhost:4317")
service_name: @env("SERVICE_NAME", "unknown-service")
environment: @env("ENVIRONMENT", "development")

[sampling]
probability: @env("TRACING_SAMPLING_PROBABILITY", "0.1")
```

### Tracing Implementation

```ruby
# lib/distributed_tracing.rb
require 'opentelemetry/sdk'
require 'opentelemetry/exporter/otlp'
require 'opentelemetry/instrumentation/all'
require 'tusk'

class DistributedTracing
  def initialize(config_path = 'config/tracing.tsk')
    @config = Tusk.load(config_path)
    setup_tracing if @config['tracing']['enabled'] == 'true'
  end

  def trace_service_call(service_name, operation, &block)
    return yield unless @tracer

    @tracer.in_span("service_call.#{service_name}.#{operation}") do |span|
      span.set_attribute('service.name', service_name)
      span.set_attribute('operation', operation)
      
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

  def trace_database_query(query, &block)
    return yield unless @tracer

    @tracer.in_span('database.query') do |span|
      span.set_attribute('db.statement', query)
      span.set_attribute('db.system', 'postgresql')
      
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

  private

  def setup_tracing
    OpenTelemetry::SDK.configure do |c|
      c.service_name = @config['tracing']['service_name']
      c.use_all
    end

    @tracer = OpenTelemetry.tracer_provider.tracer('tusklang-microservices')
  end
end
```

## 🚀 **Deployment Strategies**

### Docker Compose Configuration

```yaml
# docker-compose.yml
version: '3.8'

services:
  user-service:
    build: ./user-service
    ports:
      - "3001:3001"
    environment:
      - SERVICE_NAME=user-service
      - DATABASE_URL=postgresql://user:pass@postgres:5432/user_db
      - RABBITMQ_HOST=rabbitmq
    depends_on:
      - postgres
      - rabbitmq

  payment-service:
    build: ./payment-service
    ports:
      - "3002:3002"
    environment:
      - SERVICE_NAME=payment-service
      - DATABASE_URL=postgresql://user:pass@postgres:5432/payment_db
      - RABBITMQ_HOST=rabbitmq
    depends_on:
      - postgres
      - rabbitmq

  notification-service:
    build: ./notification-service
    ports:
      - "3003:3003"
    environment:
      - SERVICE_NAME=notification-service
      - DATABASE_URL=postgresql://user:pass@postgres:5432/notification_db
      - RABBITMQ_HOST=rabbitmq
    depends_on:
      - postgres
      - rabbitmq

  postgres:
    image: postgres:13
    environment:
      - POSTGRES_PASSWORD=pass
    volumes:
      - postgres_data:/var/lib/postgresql/data

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"

volumes:
  postgres_data:
```

### Kubernetes Deployment

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: user-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: user-service
  template:
    metadata:
      labels:
        app: user-service
    spec:
      containers:
      - name: user-service
        image: user-service:latest
        ports:
        - containerPort: 3001
        env:
        - name: SERVICE_NAME
          value: "user-service"
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: database-secret
              key: url
        - name: RABBITMQ_HOST
          value: "rabbitmq-service"
        livenessProbe:
          httpGet:
            path: /health
            port: 3001
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 3001
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: user-service
spec:
  selector:
    app: user-service
  ports:
  - port: 80
    targetPort: 3001
  type: ClusterIP
```

## 🔍 **Monitoring and Observability**

### Health Check Endpoints

```ruby
# app/controllers/health_controller.rb
class HealthController < ApplicationController
  def check
    health_status = {
      status: 'healthy',
      timestamp: Time.now.iso8601,
      service: Rails.application.class.module_parent_name.underscore,
      version: Rails.application.config.version,
      checks: {
        database: database_healthy?,
        redis: redis_healthy?,
        rabbitmq: rabbitmq_healthy?
      }
    }

    if health_status[:checks].values.all?
      render json: health_status, status: :ok
    else
      health_status[:status] = 'unhealthy'
      render json: health_status, status: :service_unavailable
    end
  end

  def ready
    # Readiness check - service is ready to receive traffic
    if ready_for_traffic?
      render json: { status: 'ready' }, status: :ok
    else
      render json: { status: 'not_ready' }, status: :service_unavailable
    end
  end

  private

  def database_healthy?
    ActiveRecord::Base.connection.execute('SELECT 1')
    true
  rescue => e
    Rails.logger.error "Database health check failed: #{e.message}"
    false
  end

  def redis_healthy?
    Redis.new.ping == 'PONG'
  rescue => e
    Rails.logger.error "Redis health check failed: #{e.message}"
    false
  end

  def rabbitmq_healthy?
    # Implement RabbitMQ health check
    true
  rescue => e
    Rails.logger.error "RabbitMQ health check failed: #{e.message}"
    false
  end

  def ready_for_traffic?
    # Check if service is ready to handle requests
    database_healthy? && redis_healthy?
  end
end
```

## 🎯 **Best Practices**

### Service Design Principles

1. **Single Responsibility**: Each service should have one clear purpose
2. **Loose Coupling**: Services should communicate through well-defined interfaces
3. **High Cohesion**: Related functionality should be grouped together
4. **Stateless**: Services should not maintain state between requests
5. **Resilient**: Services should handle failures gracefully

### Configuration Management

```ruby
# config/microservices.tsk
[service_discovery]
type: @env("SERVICE_DISCOVERY_TYPE", "static") # static, consul, etcd
refresh_interval: @env("SERVICE_DISCOVERY_REFRESH", "30")

[circuit_breaker]
failure_threshold: @env("CIRCUIT_BREAKER_FAILURE_THRESHOLD", "5")
timeout: @env("CIRCUIT_BREAKER_TIMEOUT", "60")
half_open_max_calls: @env("CIRCUIT_BREAKER_HALF_OPEN_MAX", "3")

[retry]
max_attempts: @env("RETRY_MAX_ATTEMPTS", "3")
backoff_multiplier: @env("RETRY_BACKOFF_MULTIPLIER", "2")
max_backoff: @env("RETRY_MAX_BACKOFF", "60")
```

### Error Handling

```ruby
# lib/microservice_error_handler.rb
class MicroserviceErrorHandler
  def self.handle_service_error(error, context = {})
    case error
    when CircuitBreakerOpenError
      handle_circuit_breaker_error(error, context)
    when Timeout::Error
      handle_timeout_error(error, context)
    when Net::HTTPError
      handle_http_error(error, context)
    else
      handle_unknown_error(error, context)
    end
  end

  private

  def self.handle_circuit_breaker_error(error, context)
    Rails.logger.warn "Circuit breaker open for #{context[:service]}"
    # Return cached response or fallback
    context[:fallback]&.call
  end

  def self.handle_timeout_error(error, context)
    Rails.logger.error "Timeout calling #{context[:service]}: #{error.message}"
    # Implement retry logic or return error
    raise ServiceTimeoutError, "Service #{context[:service]} timed out"
  end

  def self.handle_http_error(error, context)
    Rails.logger.error "HTTP error calling #{context[:service]}: #{error.message}"
    # Handle specific HTTP status codes
    case error.response&.code
    when '404'
      raise ServiceNotFoundError, "Service #{context[:service]} not found"
    when '500'
      raise ServiceError, "Service #{context[:service]} internal error"
    else
      raise ServiceError, "Service #{context[:service]} error: #{error.message}"
    end
  end

  def self.handle_unknown_error(error, context)
    Rails.logger.error "Unknown error calling #{context[:service]}: #{error.message}"
    raise ServiceError, "Unknown error calling #{context[:service]}"
  end
end
```

## 🚀 **Performance Optimization**

### Connection Pooling

```ruby
# config/database.tsk
[database]
pool_size: @env("DB_POOL_SIZE", "5")
pool_timeout: @env("DB_POOL_TIMEOUT", "5")
checkout_timeout: @env("DB_CHECKOUT_TIMEOUT", "5")
reaping_frequency: @env("DB_REAPING_FREQUENCY", "10")
```

### Caching Strategy

```ruby
# lib/service_cache.rb
class ServiceCache
  def initialize(ttl = 300)
    @redis = Redis.new
    @ttl = ttl
  end

  def get(key)
    cached = @redis.get(cache_key(key))
    return nil unless cached
    
    JSON.parse(cached)
  rescue JSON::ParserError
    nil
  end

  def set(key, value, ttl = @ttl)
    @redis.setex(cache_key(key), ttl, value.to_json)
  end

  def delete(key)
    @redis.del(cache_key(key))
  end

  def clear_pattern(pattern)
    keys = @redis.keys(cache_key(pattern))
    @redis.del(*keys) unless keys.empty?
  end

  private

  def cache_key(key)
    "service_cache:#{Rails.application.class.module_parent_name.underscore}:#{key}"
  end
end
```

## 🔒 **Security Considerations**

### Service-to-Service Authentication

```ruby
# lib/service_security.rb
class ServiceSecurity
  def initialize(config_path = 'config/auth.tsk')
    @config = Tusk.load(config_path)
  end

  def authenticate_service_request(request)
    token = extract_token(request)
    return false unless token
    
    verify_service_token(token)
  end

  def authorize_service_action(service_name, action, resource)
    # Implement service-specific authorization logic
    case service_name
    when 'user-service'
      authorize_user_service_action(action, resource)
    when 'payment-service'
      authorize_payment_service_action(action, resource)
    else
      false
    end
  end

  private

  def extract_token(request)
    auth_header = request.headers['Authorization']
    return nil unless auth_header&.start_with?('Bearer ')
    
    auth_header.split(' ').last
  end

  def verify_service_token(token)
    secret = @config['auth']['jwt_secret']
    decoded = JWT.decode(token, secret, true, { algorithm: 'HS256' })
    decoded[0]
  rescue JWT::DecodeError
    false
  end

  def authorize_user_service_action(action, resource)
    # Implement user service authorization
    true
  end

  def authorize_payment_service_action(action, resource)
    # Implement payment service authorization
    true
  end
end
```

## 📈 **Scaling Strategies**

### Horizontal Scaling

```ruby
# config/scaling.tsk
[scaling]
auto_scaling: @env("AUTO_SCALING_ENABLED", "true")
min_instances: @env("MIN_INSTANCES", "2")
max_instances: @env("MAX_INSTANCES", "10")
cpu_threshold: @env("CPU_THRESHOLD", "70")
memory_threshold: @env("MEMORY_THRESHOLD", "80")

[load_balancing]
algorithm: @env("LB_ALGORITHM", "round_robin") # round_robin, least_connections, ip_hash
health_check_interval: @env("LB_HEALTH_CHECK_INTERVAL", "30")
```

### Load Balancing Configuration

```ruby
# lib/load_balancer.rb
class LoadBalancer
  def initialize(algorithm = 'round_robin')
    @algorithm = algorithm
    @services = []
    @current_index = 0
  end

  def add_service(service)
    @services << service
  end

  def get_next_service
    return nil if @services.empty?
    
    case @algorithm
    when 'round_robin'
      service = @services[@current_index]
      @current_index = (@current_index + 1) % @services.length
      service
    when 'least_connections'
      @services.min_by(&:connection_count)
    when 'ip_hash'
      # Implement IP hash algorithm
      @services.first
    else
      @services.first
    end
  end

  def remove_service(service)
    @services.delete(service)
  end
end
```

## 🎯 **Testing Microservices**

### Service Integration Tests

```ruby
# spec/integration/microservice_integration_spec.rb
require 'rails_helper'

RSpec.describe 'Microservice Integration', type: :integration do
  let(:user_service_client) { MicroserviceClient.new('user-service') }
  let(:payment_service_client) { MicroserviceClient.new('payment-service') }

  before do
    # Mock external services
    stub_request(:get, /user-service.*/).to_return(status: 200, body: '{}')
    stub_request(:post, /payment-service.*/).to_return(status: 200, body: '{}')
  end

  describe 'user creation flow' do
    it 'creates user and sends welcome notification' do
      # Test complete user creation flow
      user_data = { name: 'John Doe', email: 'john@example.com' }
      
      # Create user
      user_response = user_service_client.post('/users', user_data)
      expect(user_response).to include('user_id')
      
      # Verify payment service was notified
      expect(WebMock).to have_requested(:post, /payment-service.*/)
        .with(body: hash_including('user_id'))
    end
  end

  describe 'circuit breaker behavior' do
    it 'opens circuit breaker after multiple failures' do
      # Mock service failure
      stub_request(:get, /user-service.*/).to_return(status: 500)
      
      # Make multiple calls
      5.times do
        expect { user_service_client.get('/users/1') }.to raise_error(ServerError)
      end
      
      # Next call should hit circuit breaker
      expect { user_service_client.get('/users/1') }.to raise_error(CircuitBreakerOpenError)
    end
  end
end
```

## 🚀 **Deployment Pipeline**

### CI/CD Configuration

```yaml
# .github/workflows/microservice-deploy.yml
name: Deploy Microservice

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Set up Ruby
      uses: ruby/setup-ruby@v1
      with:
        ruby-version: 3.2
    - name: Install dependencies
      run: bundle install
    - name: Run tests
      run: bundle exec rspec
    - name: Run security scan
      run: bundle exec brakeman

  build:
    needs: test
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Build Docker image
      run: docker build -t ${{ github.repository }}:${{ github.sha }} .
    - name: Push to registry
      run: |
        echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
        docker push ${{ github.repository }}:${{ github.sha }}

  deploy:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - name: Deploy to staging
      run: |
        kubectl set image deployment/${{ github.event.repository.name }} \
          ${{ github.event.repository.name }}=${{ github.repository }}:${{ github.sha }}
```

## 🎯 **Monitoring and Alerting**

### Prometheus Metrics

```ruby
# lib/microservice_metrics.rb
require 'prometheus/client'

class MicroserviceMetrics
  def initialize
    @registry = Prometheus::Client.registry
    
    # Define metrics
    @request_counter = @registry.counter(
      :service_requests_total,
      docstring: 'Total number of service requests',
      labels: [:service, :method, :status]
    )
    
    @request_duration = @registry.histogram(
      :service_request_duration_seconds,
      docstring: 'Service request duration in seconds',
      labels: [:service, :method]
    )
    
    @circuit_breaker_state = @registry.gauge(
      :circuit_breaker_state,
      docstring: 'Circuit breaker state (0=closed, 1=half_open, 2=open)',
      labels: [:service]
    )
  end

  def record_request(service, method, status, duration)
    @request_counter.increment(labels: { service: service, method: method, status: status })
    @request_duration.observe(duration, labels: { service: service, method: method })
  end

  def set_circuit_breaker_state(service, state)
    state_value = case state
                  when :closed then 0
                  when :half_open then 1
                  when :open then 2
                  else 0
                  end
    
    @circuit_breaker_state.set(state_value, labels: { service: service })
  end
end
```

## 🎯 **Summary**

This comprehensive guide covers building microservices with TuskLang and Ruby, including:

- **Service Discovery**: Automatic service registration and health checking
- **Inter-Service Communication**: HTTP clients with circuit breakers and retry logic
- **Message Queues**: RabbitMQ integration for asynchronous communication
- **Authentication**: JWT-based service-to-service authentication
- **Distributed Tracing**: OpenTelemetry integration for observability
- **Deployment**: Docker and Kubernetes deployment strategies
- **Monitoring**: Health checks, metrics, and alerting
- **Security**: Service authentication and authorization
- **Testing**: Integration testing strategies
- **Scaling**: Horizontal scaling and load balancing

The microservices architecture with TuskLang provides a robust foundation for building scalable, resilient distributed systems that can adapt to changing requirements and handle failures gracefully. 
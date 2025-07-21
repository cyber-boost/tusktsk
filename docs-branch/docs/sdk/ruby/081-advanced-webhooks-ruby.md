# 🔗 Advanced Webhooks with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Transform your Ruby applications with TuskLang's advanced webhook system. From real-time notifications to complex event-driven architectures, TuskLang webhooks provide the power and flexibility you need to build responsive, scalable applications.

## 🚀 Quick Start

### Basic Webhook Setup
```ruby
require 'tusklang'
require 'tusklang/webhooks'

# Initialize webhook system
webhook_system = TuskLang::Webhooks::System.new

# Register a simple webhook
webhook_system.register('user.created') do |payload|
  puts "New user created: #{payload['user']['email']}"
  # Send welcome email, create profile, etc.
end

# Trigger the webhook
webhook_system.trigger('user.created', {
  user: { id: 123, email: 'alice@example.com', name: 'Alice' },
  timestamp: Time.now.iso8601
})
```

### TuskLang Configuration
```tsk
# config/webhooks.tsk
[webhooks]
enabled: true
timeout: "30s"
retry_attempts: 3
retry_delay: "5s"

[webhooks.endpoints]
user_created: "https://api.example.com/webhooks/user-created"
order_placed: "https://api.example.com/webhooks/order-placed"
payment_processed: "https://api.example.com/webhooks/payment-processed"

[webhooks.security]
signature_header: "X-Webhook-Signature"
signature_algorithm: "sha256"
secret_key: @env("WEBHOOK_SECRET", "default-secret")

[webhooks.rate_limiting]
requests_per_minute: 100
burst_limit: 20
```

## 🎯 Core Features

### 1. Event-Driven Architecture
```ruby
require 'tusklang/webhooks'

class WebhookManager
  include TuskLang::Webhooks::EventEmitter
  
  def initialize
    @config = TuskLang.parse_file('config/webhooks.tsk')
    setup_webhooks
  end
  
  private
  
  def setup_webhooks
    # User events
    on('user.created') do |payload|
      send_welcome_email(payload['user'])
      create_user_profile(payload['user'])
      notify_admin(payload['user'])
    end
    
    on('user.updated') do |payload|
      update_search_index(payload['user'])
      invalidate_cache(payload['user']['id'])
    end
    
    on('user.deleted') do |payload|
      archive_user_data(payload['user'])
      remove_from_search_index(payload['user']['id'])
    end
    
    # Order events
    on('order.placed') do |payload|
      process_payment(payload['order'])
      update_inventory(payload['order'])
      send_order_confirmation(payload['order'])
    end
    
    on('order.shipped') do |payload|
      send_tracking_email(payload['order'])
      update_order_status(payload['order']['id'])
    end
    
    # Payment events
    on('payment.processed') do |payload|
      update_order_status(payload['payment']['order_id'])
      send_receipt(payload['payment'])
    end
    
    on('payment.failed') do |payload|
      notify_user_of_failure(payload['payment'])
      retry_payment(payload['payment'])
    end
  end
  
  def send_welcome_email(user)
    # Implementation
  end
  
  def create_user_profile(user)
    # Implementation
  end
  
  def notify_admin(user)
    # Implementation
  end
end
```

### 2. Advanced Webhook Handlers
```ruby
require 'tusklang/webhooks'
require 'json'

class AdvancedWebhookHandler
  include TuskLang::Webhooks::Handler
  
  def initialize
    @config = TuskLang.parse_file('config/webhooks.tsk')
    @rate_limiter = TuskLang::RateLimiter.new(
      requests_per_minute: @config['webhooks']['rate_limiting']['requests_per_minute'],
      burst_limit: @config['webhooks']['rate_limiting']['burst_limit']
    )
  end
  
  # Handle incoming webhooks
  def handle_webhook(request)
    # Verify signature
    return unauthorized unless verify_signature(request)
    
    # Rate limiting
    return rate_limited unless @rate_limiter.allow?(request.ip)
    
    # Parse payload
    payload = JSON.parse(request.body.read)
    event_type = request.headers['X-Event-Type']
    
    # Process based on event type
    case event_type
    when 'user.created'
      handle_user_created(payload)
    when 'order.placed'
      handle_order_placed(payload)
    when 'payment.processed'
      handle_payment_processed(payload)
    else
      handle_unknown_event(event_type, payload)
    end
    
    { status: 'success', message: 'Webhook processed' }
  rescue => e
    log_error(e)
    { status: 'error', message: e.message }
  end
  
  private
  
  def verify_signature(request)
    signature = request.headers[@config['webhooks']['security']['signature_header']]
    expected_signature = generate_signature(request.body.read)
    signature == expected_signature
  end
  
  def generate_signature(payload)
    secret = @config['webhooks']['security']['secret_key']
    algorithm = @config['webhooks']['security']['signature_algorithm']
    
    case algorithm
    when 'sha256'
      OpenSSL::HMAC.hexdigest('SHA256', secret, payload)
    when 'sha1'
      OpenSSL::HMAC.hexdigest('SHA1', secret, payload)
    else
      raise "Unsupported signature algorithm: #{algorithm}"
    end
  end
  
  def handle_user_created(payload)
    user = payload['user']
    
    # Create user profile
    profile = UserProfile.create!(
      user_id: user['id'],
      email: user['email'],
      name: user['name'],
      created_at: Time.parse(user['created_at'])
    )
    
    # Send welcome email
    UserMailer.welcome_email(user['email']).deliver_later
    
    # Update analytics
    Analytics.track('user.created', {
      user_id: user['id'],
      source: payload['source'],
      timestamp: Time.now
    })
  end
  
  def handle_order_placed(payload)
    order = payload['order']
    
    # Process payment
    payment_result = PaymentProcessor.process(order['payment'])
    
    if payment_result.success?
      # Update order status
      order_record = Order.find(order['id'])
      order_record.update!(status: 'paid', payment_id: payment_result.payment_id)
      
      # Update inventory
      InventoryManager.update_stock(order['items'])
      
      # Send confirmation
      OrderMailer.confirmation_email(order['customer_email'], order).deliver_later
    else
      # Handle payment failure
      handle_payment_failure(order, payment_result.error)
    end
  end
  
  def handle_payment_processed(payload)
    payment = payload['payment']
    
    # Update order status
    order = Order.find(payment['order_id'])
    order.update!(status: 'paid', payment_processed_at: Time.now)
    
    # Send receipt
    PaymentMailer.receipt_email(payment['customer_email'], payment).deliver_later
    
    # Update analytics
    Analytics.track('payment.processed', {
      order_id: payment['order_id'],
      amount: payment['amount'],
      currency: payment['currency']
    })
  end
  
  def handle_unknown_event(event_type, payload)
    Rails.logger.warn "Unknown webhook event: #{event_type}"
    Rails.logger.debug "Payload: #{payload.inspect}"
  end
  
  def log_error(error)
    Rails.logger.error "Webhook processing error: #{error.message}"
    Rails.logger.error error.backtrace.join("\n")
  end
end
```

### 3. Webhook Middleware
```ruby
require 'tusklang/webhooks'

class WebhookMiddleware
  def initialize(app)
    @app = app
    @config = TuskLang.parse_file('config/webhooks.tsk')
  end
  
  def call(env)
    request = Rack::Request.new(env)
    
    # Check if this is a webhook request
    if webhook_request?(request)
      handle_webhook_request(request)
    else
      @app.call(env)
    end
  end
  
  private
  
  def webhook_request?(request)
    request.path.start_with?('/webhooks/') && request.post?
  end
  
  def handle_webhook_request(request)
    handler = AdvancedWebhookHandler.new
    result = handler.handle_webhook(request)
    
    [
      result[:status] == 'success' ? 200 : 400,
      { 'Content-Type' => 'application/json' },
      [result.to_json]
    ]
  end
end

# In config/application.rb
module MyApp
  class Application < Rails::Application
    # Add webhook middleware
    config.middleware.use WebhookMiddleware
  end
end
```

### 4. Webhook Testing Framework
```ruby
require 'tusklang/webhooks'
require 'rspec'

RSpec.describe 'Webhook System' do
  let(:webhook_system) { TuskLang::Webhooks::System.new }
  let(:config) { TuskLang.parse_file('spec/fixtures/webhooks.tsk') }
  
  before do
    webhook_system.configure(config)
  end
  
  describe 'user.created webhook' do
    it 'sends welcome email' do
      expect(UserMailer).to receive(:welcome_email).with('alice@example.com')
      
      webhook_system.trigger('user.created', {
        user: { email: 'alice@example.com', name: 'Alice' }
      })
    end
    
    it 'creates user profile' do
      expect {
        webhook_system.trigger('user.created', {
          user: { id: 123, email: 'alice@example.com', name: 'Alice' }
        })
      }.to change(UserProfile, :count).by(1)
    end
    
    it 'tracks analytics event' do
      expect(Analytics).to receive(:track).with('user.created', hash_including(
        user_id: 123
      ))
      
      webhook_system.trigger('user.created', {
        user: { id: 123, email: 'alice@example.com', name: 'Alice' }
      })
    end
  end
  
  describe 'order.placed webhook' do
    let(:order_payload) do
      {
        order: {
          id: 456,
          customer_email: 'bob@example.com',
          items: [{ product_id: 1, quantity: 2 }],
          payment: { method: 'credit_card', amount: 99.99 }
        }
      }
    end
    
    it 'processes payment' do
      expect(PaymentProcessor).to receive(:process).with(
        hash_including(method: 'credit_card', amount: 99.99)
      ).and_return(double(success?: true, payment_id: 'pay_123'))
      
      webhook_system.trigger('order.placed', order_payload)
    end
    
    it 'updates inventory' do
      expect(InventoryManager).to receive(:update_stock).with(
        [{ product_id: 1, quantity: 2 }]
      )
      
      webhook_system.trigger('order.placed', order_payload)
    end
    
    it 'sends confirmation email' do
      expect(OrderMailer).to receive(:confirmation_email).with(
        'bob@example.com',
        hash_including(id: 456)
      )
      
      webhook_system.trigger('order.placed', order_payload)
    end
  end
  
  describe 'webhook security' do
    it 'verifies signature' do
      request = double(
        headers: { 'X-Webhook-Signature' => 'invalid_signature' },
        body: double(read: '{"test": "data"}'),
        ip: '127.0.0.1'
      )
      
      handler = AdvancedWebhookHandler.new
      result = handler.handle_webhook(request)
      
      expect(result[:status]).to eq('error')
    end
    
    it 'enforces rate limiting' do
      request = double(
        headers: { 'X-Webhook-Signature' => 'valid_signature' },
        body: double(read: '{"test": "data"}'),
        ip: '127.0.0.1'
      )
      
      # Make too many requests
      101.times do
        handler = AdvancedWebhookHandler.new
        handler.handle_webhook(request)
      end
      
      # Last request should be rate limited
      handler = AdvancedWebhookHandler.new
      result = handler.handle_webhook(request)
      
      expect(result[:status]).to eq('rate_limited')
    end
  end
end
```

## 🔧 Advanced Configuration

### Webhook Retry Logic
```ruby
require 'tusklang/webhooks'

class RetryableWebhook
  include TuskLang::Webhooks::Retryable
  
  def initialize
    @config = TuskLang.parse_file('config/webhooks.tsk')
    @retry_attempts = @config['webhooks']['retry_attempts']
    @retry_delay = parse_duration(@config['webhooks']['retry_delay'])
  end
  
  def send_webhook(endpoint, payload)
    with_retry(attempts: @retry_attempts, delay: @retry_delay) do
      response = HTTP.timeout(@config['webhooks']['timeout'])
                    .post(endpoint, json: payload)
      
      unless response.status.success?
        raise "Webhook failed with status #{response.status}"
      end
      
      response
    end
  end
  
  private
  
  def parse_duration(duration_string)
    case duration_string
    when /(\d+)s/
      $1.to_i
    when /(\d+)m/
      $1.to_i * 60
    when /(\d+)h/
      $1.to_i * 3600
    else
      5 # Default 5 seconds
    end
  end
end
```

### Webhook Queue System
```ruby
require 'tusklang/webhooks'
require 'sidekiq'

class WebhookWorker
  include Sidekiq::Worker
  
  def perform(event_type, payload, endpoint = nil)
    webhook_system = TuskLang::Webhooks::System.new
    config = TuskLang.parse_file('config/webhooks.tsk')
    
    # Use endpoint from payload or config
    target_endpoint = endpoint || config['webhooks']['endpoints'][event_type]
    
    if target_endpoint
      # Send to external endpoint
      send_external_webhook(target_endpoint, event_type, payload)
    else
      # Process internally
      webhook_system.trigger(event_type, payload)
    end
  end
  
  private
  
  def send_external_webhook(endpoint, event_type, payload)
    config = TuskLang.parse_file('config/webhooks.tsk')
    
    headers = {
      'Content-Type' => 'application/json',
      'X-Event-Type' => event_type,
      'X-Webhook-Signature' => generate_signature(payload.to_json)
    }
    
    response = HTTP.timeout(config['webhooks']['timeout'])
                  .headers(headers)
                  .post(endpoint, json: payload)
    
    unless response.status.success?
      raise "External webhook failed: #{response.status} - #{response.body}"
    end
    
    Rails.logger.info "External webhook sent successfully to #{endpoint}"
  end
  
  def generate_signature(payload)
    secret = TuskLang.parse_file('config/webhooks.tsk')['webhooks']['security']['secret_key']
    OpenSSL::HMAC.hexdigest('SHA256', secret, payload)
  end
end

# Usage
WebhookWorker.perform_async('user.created', {
  user: { id: 123, email: 'alice@example.com' }
})
```

## 🚀 Performance Optimization

### Webhook Batching
```ruby
require 'tusklang/webhooks'

class BatchedWebhookProcessor
  def initialize
    @batch_size = 100
    @batch_timeout = 30 # seconds
    @batches = {}
    @mutex = Mutex.new
  end
  
  def add_to_batch(event_type, payload)
    @mutex.synchronize do
      @batches[event_type] ||= []
      @batches[event_type] << payload
      
      if @batches[event_type].size >= @batch_size
        process_batch(event_type)
      end
    end
  end
  
  def process_batch(event_type)
    batch = @batches[event_type]
    @batches[event_type] = []
    
    # Process batch in background
    Thread.new do
      begin
        batch.each do |payload|
          WebhookWorker.perform_async(event_type, payload)
        end
      rescue => e
        Rails.logger.error "Batch processing error: #{e.message}"
      end
    end
  end
  
  def start_batch_timer
    Thread.new do
      loop do
        sleep @batch_timeout
        
        @mutex.synchronize do
          @batches.each do |event_type, batch|
            process_batch(event_type) unless batch.empty?
          end
        end
      end
    end
  end
end
```

### Webhook Monitoring
```ruby
require 'tusklang/webhooks'

class WebhookMonitor
  include TuskLang::Webhooks::Monitorable
  
  def initialize
    @metrics = TuskLang::Metrics::Collector.new
    @config = TuskLang.parse_file('config/webhooks.tsk')
  end
  
  def track_webhook_event(event_type, payload, response_time)
    @metrics.increment("webhook.events.#{event_type}")
    @metrics.histogram("webhook.response_time", response_time)
    
    if response_time > @config['webhooks']['timeout']
      @metrics.increment("webhook.timeouts.#{event_type}")
    end
  end
  
  def track_webhook_error(event_type, error)
    @metrics.increment("webhook.errors.#{event_type}")
    @metrics.increment("webhook.errors.#{error.class.name}")
  end
  
  def get_webhook_stats
    {
      total_events: @metrics.get("webhook.events.total"),
      average_response_time: @metrics.get_average("webhook.response_time"),
      error_rate: @metrics.get_error_rate("webhook.errors"),
      top_events: @metrics.get_top("webhook.events", 10)
    }
  end
end
```

## 🔒 Security Best Practices

### Webhook Authentication
```ruby
require 'tusklang/webhooks'

class SecureWebhookHandler
  def initialize
    @config = TuskLang.parse_file('config/webhooks.tsk')
  end
  
  def authenticate_webhook(request)
    # Verify signature
    return false unless verify_signature(request)
    
    # Check IP whitelist
    return false unless ip_whitelisted?(request.ip)
    
    # Verify timestamp (prevent replay attacks)
    return false unless verify_timestamp(request)
    
    true
  end
  
  private
  
  def verify_signature(request)
    signature = request.headers['X-Webhook-Signature']
    payload = request.body.read
    expected_signature = generate_signature(payload)
    
    # Use constant-time comparison to prevent timing attacks
    Rack::Utils.secure_compare(signature, expected_signature)
  end
  
  def ip_whitelisted?(ip)
    whitelist = @config['webhooks']['security']['ip_whitelist'] || []
    whitelist.include?(ip)
  end
  
  def verify_timestamp(request)
    timestamp = request.headers['X-Webhook-Timestamp']
    return false unless timestamp
    
    webhook_time = Time.at(timestamp.to_i)
    current_time = Time.now
    
    # Allow 5-minute window for clock skew
    (current_time - webhook_time).abs <= 300
  end
end
```

## 📊 Monitoring and Debugging

### Webhook Dashboard
```ruby
require 'tusklang/webhooks'

class WebhookDashboardController < ApplicationController
  def index
    @stats = webhook_monitor.get_webhook_stats
    @recent_events = webhook_monitor.get_recent_events(limit: 50)
    @error_logs = webhook_monitor.get_error_logs(limit: 20)
  end
  
  def event_details
    @event = webhook_monitor.get_event_details(params[:id])
    @payload = JSON.pretty_generate(@event.payload)
    @response = @event.response
  end
  
  def retry_failed
    event_id = params[:id]
    webhook_monitor.retry_failed_event(event_id)
    
    redirect_to webhook_dashboard_path, notice: 'Webhook retry initiated'
  end
  
  private
  
  def webhook_monitor
    @webhook_monitor ||= WebhookMonitor.new
  end
end
```

## 🎯 Real-World Examples

### E-commerce Webhook System
```ruby
# config/ecommerce-webhooks.tsk
[ecommerce_webhooks]
enabled: true
timeout: "60s"
retry_attempts: 5

[ecommerce_webhooks.events]
order_created: "https://api.shopify.com/webhooks/orders/create"
order_updated: "https://api.shopify.com/webhooks/orders/updated"
order_cancelled: "https://api.shopify.com/webhooks/orders/cancelled"
payment_processed: "https://api.stripe.com/webhooks/payment_intent.succeeded"
inventory_updated: "https://api.shopify.com/webhooks/inventory_levels/update"

[ecommerce_webhooks.handlers]
order_created: "OrderCreatedHandler"
order_updated: "OrderUpdatedHandler"
payment_processed: "PaymentProcessedHandler"
```

```ruby
class EcommerceWebhookSystem
  def initialize
    @config = TuskLang.parse_file('config/ecommerce-webhooks.tsk')
    @handlers = load_handlers
  end
  
  def process_webhook(event_type, payload)
    handler_class = @handlers[event_type]
    return unless handler_class
    
    handler = handler_class.constantize.new
    handler.process(payload)
  end
  
  private
  
  def load_handlers
    @config['ecommerce_webhooks']['handlers']
  end
end

class OrderCreatedHandler
  def process(payload)
    order = payload['order']
    
    # Create order in local system
    local_order = Order.create!(
      external_id: order['id'],
      customer_email: order['email'],
      total: order['total_price'],
      currency: order['currency'],
      status: 'pending'
    )
    
    # Process line items
    order['line_items'].each do |item|
      OrderItem.create!(
        order: local_order,
        product_id: item['product_id'],
        quantity: item['quantity'],
        price: item['price']
      )
    end
    
    # Send confirmation email
    OrderMailer.confirmation_email(local_order).deliver_later
    
    # Update inventory
    InventoryManager.reserve_items(local_order)
  end
end
```

This comprehensive webhook system provides the foundation for building robust, scalable event-driven applications with TuskLang and Ruby. The combination of TuskLang's configuration flexibility and Ruby's expressive syntax creates a powerful platform for handling complex webhook scenarios. 
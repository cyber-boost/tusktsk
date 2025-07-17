# Message Queue Integration with TuskLang and Ruby

This guide covers integrating message queues with TuskLang and Ruby applications for asynchronous processing and distributed systems.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Setup](#basic-setup)
4. [Producer Implementation](#producer-implementation)
5. [Consumer Implementation](#consumer-implementation)
6. [Message Types](#message-types)
7. [Error Handling](#error-handling)
8. [Monitoring](#monitoring)
9. [Testing](#testing)
10. [Deployment](#deployment)

## Overview

Message queues enable asynchronous processing and decoupled communication between services. This guide shows how to integrate various message queue systems with TuskLang and Ruby applications.

### Key Features

- **Multiple queue backends** (Redis, RabbitMQ, Apache Kafka)
- **Reliable message delivery** with acknowledgments
- **Dead letter queues** for failed messages
- **Message routing** and filtering
- **Batch processing** support
- **Monitoring and metrics**

## Installation

### Dependencies

```ruby
# Gemfile
gem 'sidekiq'
gem 'redis'
gem 'bunny'
gem 'ruby-kafka'
gem 'connection_pool'
gem 'json'
```

### TuskLang Configuration

```tusk
# config/message_queue.tusk
message_queue:
  backend: "redis"  # redis, rabbitmq, kafka
  
  redis:
    url: "redis://localhost:6379/1"
    pool_size: 10
    pool_timeout: 5
    retry_attempts: 3
    retry_delay: 1
  
  rabbitmq:
    host: "localhost"
    port: 5672
    username: "guest"
    password: "guest"
    vhost: "/"
    connection_pool_size: 5
    channel_pool_size: 10
  
  kafka:
    brokers: ["localhost:9092"]
    client_id: "tusk_ruby_client"
    group_id: "tusk_ruby_group"
    auto_offset_reset: "earliest"
    enable_auto_commit: true
  
  queues:
    default: "default"
    high_priority: "high_priority"
    low_priority: "low_priority"
    dead_letter: "dead_letter"
    retry: "retry"
  
  retry:
    max_attempts: 3
    backoff_multiplier: 2
    initial_delay: 1
  
  monitoring:
    enabled: true
    metrics_port: 9090
    health_check_interval: 30
```

## Basic Setup

### Message Queue Manager

```ruby
# app/message_queue/queue_manager.rb
class QueueManager
  include Singleton

  def initialize
    @config = Rails.application.config.message_queue
    @backend = create_backend
  end

  def publish(queue_name, message, options = {})
    @backend.publish(queue_name, message, options)
  end

  def subscribe(queue_name, handler_class, options = {})
    @backend.subscribe(queue_name, handler_class, options)
  end

  def delete_queue(queue_name)
    @backend.delete_queue(queue_name)
  end

  def purge_queue(queue_name)
    @backend.purge_queue(queue_name)
  end

  def queue_size(queue_name)
    @backend.queue_size(queue_name)
  end

  private

  def create_backend
    case @config[:backend]
    when 'redis'
      RedisQueueBackend.new(@config[:redis])
    when 'rabbitmq'
      RabbitMQBackend.new(@config[:rabbitmq])
    when 'kafka'
      KafkaBackend.new(@config[:kafka])
    else
      raise "Unsupported message queue backend: #{@config[:backend]}"
    end
  end
end
```

### Base Message

```ruby
# app/message_queue/base_message.rb
class BaseMessage
  include ActiveModel::Model
  include ActiveModel::Serialization

  attr_accessor :id, :type, :data, :metadata, :created_at, :attempts

  def initialize(attributes = {})
    @id = attributes[:id] || SecureRandom.uuid
    @type = attributes[:type] || self.class.name
    @data = attributes[:data] || {}
    @metadata = attributes[:metadata] || {}
    @created_at = attributes[:created_at] || Time.current
    @attempts = attributes[:attempts] || 0
  end

  def to_json
    {
      id: id,
      type: type,
      data: data,
      metadata: metadata,
      created_at: created_at.iso8601,
      attempts: attempts
    }.to_json
  end

  def self.from_json(json_string)
    data = JSON.parse(json_string)
    new(
      id: data['id'],
      type: data['type'],
      data: data['data'],
      metadata: data['metadata'],
      created_at: Time.parse(data['created_at']),
      attempts: data['attempts']
    )
  end

  def increment_attempts!
    @attempts += 1
  end

  def max_attempts_reached?
    attempts >= Rails.application.config.message_queue[:retry][:max_attempts]
  end
end
```

## Producer Implementation

### Message Producer

```ruby
# app/message_queue/producer.rb
class MessageProducer
  include Singleton

  def initialize
    @queue_manager = QueueManager.instance
    @config = Rails.application.config.message_queue
  end

  def publish_user_created(user)
    message = UserCreatedMessage.new(
      data: {
        user_id: user.id,
        email: user.email,
        name: user.name
      }
    )
    
    publish(@config[:queues][:default], message)
  end

  def publish_user_updated(user)
    message = UserUpdatedMessage.new(
      data: {
        user_id: user.id,
        email: user.email,
        name: user.name,
        updated_at: user.updated_at
      }
    )
    
    publish(@config[:queues][:default], message)
  end

  def publish_post_created(post)
    message = PostCreatedMessage.new(
      data: {
        post_id: post.id,
        user_id: post.user_id,
        title: post.title,
        category: post.category
      }
    )
    
    publish(@config[:queues][:high_priority], message)
  end

  def publish_email_notification(user, template, data)
    message = EmailNotificationMessage.new(
      data: {
        user_id: user.id,
        email: user.email,
        template: template,
        data: data
      }
    )
    
    publish(@config[:queues][:low_priority], message)
  end

  def publish_data_export(user, format)
    message = DataExportMessage.new(
      data: {
        user_id: user.id,
        format: format,
        requested_at: Time.current
      }
    )
    
    publish(@config[:queues][:low_priority], message)
  end

  private

  def publish(queue_name, message, options = {})
    Rails.logger.info "Publishing message to #{queue_name}: #{message.type}"
    
    @queue_manager.publish(queue_name, message.to_json, options)
    
    # Track metrics
    track_metrics(:published, queue_name, message.type)
  rescue => e
    Rails.logger.error "Failed to publish message: #{e.message}"
    track_metrics(:publish_error, queue_name, message.type)
    raise e
  end

  def track_metrics(action, queue_name, message_type)
    return unless @config[:monitoring][:enabled]
    
    # Implementation would send metrics to monitoring system
    Rails.logger.debug "Metric: #{action} - #{queue_name} - #{message_type}"
  end
end
```

### Message Types

```ruby
# app/message_queue/messages/user_created_message.rb
class UserCreatedMessage < BaseMessage
  def initialize(attributes = {})
    super(attributes.merge(type: 'UserCreated'))
  end

  def user_id
    data['user_id']
  end

  def email
    data['email']
  end

  def name
    data['name']
  end
end

# app/message_queue/messages/user_updated_message.rb
class UserUpdatedMessage < BaseMessage
  def initialize(attributes = {})
    super(attributes.merge(type: 'UserUpdated'))
  end

  def user_id
    data['user_id']
  end

  def email
    data['email']
  end

  def name
    data['name']
  end

  def updated_at
    Time.parse(data['updated_at']) if data['updated_at']
  end
end

# app/message_queue/messages/post_created_message.rb
class PostCreatedMessage < BaseMessage
  def initialize(attributes = {})
    super(attributes.merge(type: 'PostCreated'))
  end

  def post_id
    data['post_id']
  end

  def user_id
    data['user_id']
  end

  def title
    data['title']
  end

  def category
    data['category']
  end
end

# app/message_queue/messages/email_notification_message.rb
class EmailNotificationMessage < BaseMessage
  def initialize(attributes = {})
    super(attributes.merge(type: 'EmailNotification'))
  end

  def user_id
    data['user_id']
  end

  def email
    data['email']
  end

  def template
    data['template']
  end

  def notification_data
    data['data']
  end
end

# app/message_queue/messages/data_export_message.rb
class DataExportMessage < BaseMessage
  def initialize(attributes = {})
    super(attributes.merge(type: 'DataExport'))
  end

  def user_id
    data['user_id']
  end

  def format
    data['format']
  end

  def requested_at
    Time.parse(data['requested_at']) if data['requested_at']
  end
end
```

## Consumer Implementation

### Base Consumer

```ruby
# app/message_queue/consumers/base_consumer.rb
class BaseConsumer
  include Singleton

  def initialize
    @config = Rails.application.config.message_queue
  end

  def process(message)
    Rails.logger.info "Processing message: #{message.type} - #{message.id}"
    
    start_time = Time.current
    
    begin
      handle_message(message)
      track_metrics(:processed, message.type, Time.current - start_time)
    rescue => e
      handle_error(message, e)
      track_metrics(:error, message.type, Time.current - start_time)
      raise e
    end
  end

  protected

  def handle_message(message)
    raise NotImplementedError, "#{self.class} must implement handle_message"
  end

  def handle_error(message, error)
    Rails.logger.error "Error processing message #{message.id}: #{error.message}"
    
    message.increment_attempts!
    
    if message.max_attempts_reached?
      send_to_dead_letter_queue(message, error)
    else
      retry_message(message, error)
    end
  end

  def send_to_dead_letter_queue(message, error)
    dead_letter_message = DeadLetterMessage.new(
      original_message: message,
      error: error.message,
      failed_at: Time.current
    )
    
    QueueManager.instance.publish(
      @config[:queues][:dead_letter],
      dead_letter_message.to_json
    )
    
    Rails.logger.warn "Message #{message.id} sent to dead letter queue"
  end

  def retry_message(message, error)
    delay = calculate_retry_delay(message.attempts)
    
    retry_message = RetryMessage.new(
      original_message: message,
      retry_at: Time.current + delay.seconds
    )
    
    QueueManager.instance.publish(
      @config[:queues][:retry],
      retry_message.to_json
    )
    
    Rails.logger.info "Message #{message.id} scheduled for retry in #{delay} seconds"
  end

  def calculate_retry_delay(attempts)
    initial_delay = @config[:retry][:initial_delay]
    multiplier = @config[:retry][:backoff_multiplier]
    
    initial_delay * (multiplier ** (attempts - 1))
  end

  def track_metrics(action, message_type, duration = nil)
    return unless @config[:monitoring][:enabled]
    
    # Implementation would send metrics to monitoring system
    Rails.logger.debug "Metric: #{action} - #{message_type} - #{duration}"
  end
end
```

### Specific Consumers

```ruby
# app/message_queue/consumers/user_consumer.rb
class UserConsumer < BaseConsumer
  def handle_message(message)
    case message.type
    when 'UserCreated'
      handle_user_created(message)
    when 'UserUpdated'
      handle_user_updated(message)
    else
      raise "Unknown message type: #{message.type}"
    end
  end

  private

  def handle_user_created(message)
    user = User.find(message.user_id)
    
    # Send welcome email
    UserMailer.welcome_email(user).deliver_later
    
    # Create user profile
    UserProfile.create!(
      user: user,
      bio: "Welcome to our platform!",
      avatar_url: nil
    )
    
    # Update user statistics
    update_user_statistics(user)
    
    Rails.logger.info "Processed user created: #{user.id}"
  end

  def handle_user_updated(message)
    user = User.find(message.user_id)
    
    # Update search index
    SearchIndex.update_user(user)
    
    # Notify followers
    notify_followers_of_update(user)
    
    # Update cache
    Rails.cache.delete("user:#{user.id}")
    
    Rails.logger.info "Processed user updated: #{user.id}"
  end

  def update_user_statistics(user)
    # Update global user count
    Rails.cache.increment('total_users')
    
    # Update daily signups
    today = Date.current
    Rails.cache.increment("signups:#{today}")
  end

  def notify_followers_of_update(user)
    user.followers.find_each do |follower|
      Notification.create!(
        user: follower,
        notifiable: user,
        action: 'profile_updated',
        data: { user_name: user.name }
      )
    end
  end
end

# app/message_queue/consumers/post_consumer.rb
class PostConsumer < BaseConsumer
  def handle_message(message)
    case message.type
    when 'PostCreated'
      handle_post_created(message)
    else
      raise "Unknown message type: #{message.type}"
    end
  end

  private

  def handle_post_created(message)
    post = Post.find(message.post_id)
    
    # Update search index
    SearchIndex.index_post(post)
    
    # Send notifications to followers
    notify_followers_of_post(post)
    
    # Update user post count
    post.user.increment!(:posts_count)
    
    # Process hashtags
    process_hashtags(post)
    
    Rails.logger.info "Processed post created: #{post.id}"
  end

  def notify_followers_of_post(post)
    post.user.followers.find_each do |follower|
      Notification.create!(
        user: follower,
        notifiable: post,
        action: 'new_post',
        data: {
          user_name: post.user.name,
          post_title: post.title
        }
      )
    end
  end

  def process_hashtags(post)
    hashtags = post.content.scan(/#\w+/)
    
    hashtags.each do |hashtag|
      tag = Tag.find_or_create_by(name: hashtag.downcase)
      PostTag.create!(post: post, tag: tag)
    end
  end
end

# app/message_queue/consumers/email_consumer.rb
class EmailConsumer < BaseConsumer
  def handle_message(message)
    case message.type
    when 'EmailNotification'
      handle_email_notification(message)
    else
      raise "Unknown message type: #{message.type}"
    end
  end

  private

  def handle_email_notification(message)
    user = User.find(message.user_id)
    
    case message.template
    when 'welcome'
      UserMailer.welcome_email(user).deliver_now
    when 'password_reset'
      UserMailer.password_reset_email(user, message.notification_data['token']).deliver_now
    when 'post_notification'
      UserMailer.post_notification_email(user, message.notification_data).deliver_now
    else
      raise "Unknown email template: #{message.template}"
    end
    
    Rails.logger.info "Sent email notification to user: #{user.id}"
  end
end

# app/message_queue/consumers/data_export_consumer.rb
class DataExportConsumer < BaseConsumer
  def handle_message(message)
    case message.type
    when 'DataExport'
      handle_data_export(message)
    else
      raise "Unknown message type: #{message.type}"
    end
  end

  private

  def handle_data_export(message)
    user = User.find(message.user_id)
    
    case message.format
    when 'csv'
      export_to_csv(user)
    when 'json'
      export_to_json(user)
    when 'pdf'
      export_to_pdf(user)
    else
      raise "Unknown export format: #{message.format}"
    end
    
    # Notify user when export is complete
    Notification.create!(
      user: user,
      action: 'data_export_complete',
      data: { format: message.format }
    )
    
    Rails.logger.info "Completed data export for user: #{user.id}"
  end

  def export_to_csv(user)
    # Implementation for CSV export
    csv_data = generate_csv_data(user)
    save_export_file(user, csv_data, 'csv')
  end

  def export_to_json(user)
    # Implementation for JSON export
    json_data = generate_json_data(user)
    save_export_file(user, json_data, 'json')
  end

  def export_to_pdf(user)
    # Implementation for PDF export
    pdf_data = generate_pdf_data(user)
    save_export_file(user, pdf_data, 'pdf')
  end

  def generate_csv_data(user)
    # Generate CSV data for user
    CSV.generate do |csv|
      csv << ['ID', 'Email', 'Name', 'Created At']
      csv << [user.id, user.email, user.name, user.created_at]
    end
  end

  def generate_json_data(user)
    user.as_json(include: :posts)
  end

  def generate_pdf_data(user)
    # Generate PDF data for user
    # Implementation would use a PDF generation library
    "PDF data for user #{user.id}"
  end

  def save_export_file(user, data, format)
    filename = "export_#{user.id}_#{Time.current.to_i}.#{format}"
    file_path = Rails.root.join('tmp', 'exports', filename)
    
    FileUtils.mkdir_p(File.dirname(file_path))
    File.write(file_path, data)
    
    # Store file reference in database
    DataExport.create!(
      user: user,
      file_path: file_path.to_s,
      format: format,
      completed_at: Time.current
    )
  end
end
```

## Error Handling

### Dead Letter Queue Handler

```ruby
# app/message_queue/consumers/dead_letter_consumer.rb
class DeadLetterConsumer < BaseConsumer
  def handle_message(message)
    case message.type
    when 'DeadLetter'
      handle_dead_letter(message)
    else
      raise "Unknown message type: #{message.type}"
    end
  end

  private

  def handle_dead_letter(message)
    original_message = message.original_message
    error = message.error
    
    # Log the failure
    Rails.logger.error "Dead letter message: #{original_message.id} - #{error}"
    
    # Store in database for analysis
    DeadLetterMessage.create!(
      message_id: original_message.id,
      message_type: original_message.type,
      message_data: original_message.data,
      error_message: error,
      failed_at: message.failed_at,
      attempts: original_message.attempts
    )
    
    # Send alert to monitoring system
    send_alert(original_message, error)
  end

  def send_alert(message, error)
    # Implementation would send alert to monitoring system
    Rails.logger.warn "Alert: Dead letter message #{message.id} - #{error}"
  end
end

# app/message_queue/consumers/retry_consumer.rb
class RetryConsumer < BaseConsumer
  def handle_message(message)
    case message.type
    when 'Retry'
      handle_retry(message)
    else
      raise "Unknown message type: #{message.type}"
    end
  end

  private

  def handle_retry(message)
    original_message = message.original_message
    
    # Wait until retry time
    if message.retry_at > Time.current
      sleep_time = message.retry_at - Time.current
      sleep(sleep_time) if sleep_time > 0
    end
    
    # Re-publish to original queue
    QueueManager.instance.publish(
      @config[:queues][:default],
      original_message.to_json
    )
    
    Rails.logger.info "Retried message: #{original_message.id}"
  end
end
```

## Monitoring

### Queue Monitor

```ruby
# app/message_queue/monitoring/queue_monitor.rb
class QueueMonitor
  include Singleton

  def initialize
    @queue_manager = QueueManager.instance
    @config = Rails.application.config.message_queue
  end

  def monitor_queues
    queues = @config[:queues].values
    
    queues.each do |queue_name|
      size = @queue_manager.queue_size(queue_name)
      track_queue_metrics(queue_name, size)
    end
  end

  def health_check
    {
      status: 'healthy',
      queues: queue_statuses,
      timestamp: Time.current.iso8601
    }
  end

  private

  def queue_statuses
    @config[:queues].map do |name, queue_name|
      {
        name: name,
        queue: queue_name,
        size: @queue_manager.queue_size(queue_name),
        status: queue_healthy?(queue_name) ? 'healthy' : 'unhealthy'
      }
    end
  end

  def queue_healthy?(queue_name)
    size = @queue_manager.queue_size(queue_name)
    size < 1000 # Threshold for healthy queue
  end

  def track_queue_metrics(queue_name, size)
    return unless @config[:monitoring][:enabled]
    
    # Implementation would send metrics to monitoring system
    Rails.logger.debug "Queue metric: #{queue_name} - #{size} messages"
  end
end
```

## Testing

### Message Queue Test Helper

```ruby
# spec/support/message_queue_helper.rb
module MessageQueueHelper
  def publish_test_message(queue_name, message_type, data = {})
    message = BaseMessage.new(
      type: message_type,
      data: data
    )
    
    QueueManager.instance.publish(queue_name, message.to_json)
  end

  def clear_all_queues
    config = Rails.application.config.message_queue
    config[:queues].values.each do |queue_name|
      QueueManager.instance.purge_queue(queue_name)
    end
  end

  def expect_message_published(queue_name, message_type)
    # Implementation would verify message was published
    # This is a simplified version
    expect(QueueManager.instance.queue_size(queue_name)).to be > 0
  end
end

RSpec.configure do |config|
  config.include MessageQueueHelper, type: :message_queue
  
  config.before(:each, type: :message_queue) do
    clear_all_queues
  end
end
```

### Message Queue Tests

```ruby
# spec/message_queue/producer_spec.rb
RSpec.describe MessageProducer, type: :message_queue do
  let(:producer) { MessageProducer.instance }
  let(:user) { create(:user) }
  let(:post) { create(:post, user: user) }

  describe '#publish_user_created' do
    it 'publishes user created message' do
      producer.publish_user_created(user)
      
      expect_message_published('default', 'UserCreated')
    end
  end

  describe '#publish_post_created' do
    it 'publishes post created message to high priority queue' do
      producer.publish_post_created(post)
      
      expect_message_published('high_priority', 'PostCreated')
    end
  end

  describe '#publish_email_notification' do
    it 'publishes email notification to low priority queue' do
      producer.publish_email_notification(user, 'welcome', {})
      
      expect_message_published('low_priority', 'EmailNotification')
    end
  end
end

# spec/message_queue/consumers/user_consumer_spec.rb
RSpec.describe UserConsumer, type: :message_queue do
  let(:consumer) { UserConsumer.instance }
  let(:user) { create(:user) }

  describe '#handle_user_created' do
    it 'processes user created message' do
      message = UserCreatedMessage.new(
        data: {
          user_id: user.id,
          email: user.email,
          name: user.name
        }
      )
      
      expect {
        consumer.process(message)
      }.to change { UserProfile.count }.by(1)
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # Message queue configuration
  config.message_queue = {
    backend: ENV['MESSAGE_QUEUE_BACKEND'] || 'redis',
    redis: {
      url: ENV['REDIS_URL'] || 'redis://localhost:6379/1',
      pool_size: ENV['REDIS_POOL_SIZE'] || 10,
      pool_timeout: ENV['REDIS_POOL_TIMEOUT'] || 5,
      retry_attempts: ENV['REDIS_RETRY_ATTEMPTS'] || 3,
      retry_delay: ENV['REDIS_RETRY_DELAY'] || 1
    },
    rabbitmq: {
      host: ENV['RABBITMQ_HOST'] || 'localhost',
      port: ENV['RABBITMQ_PORT'] || 5672,
      username: ENV['RABBITMQ_USERNAME'] || 'guest',
      password: ENV['RABBITMQ_PASSWORD'] || 'guest',
      vhost: ENV['RABBITMQ_VHOST'] || '/',
      connection_pool_size: ENV['RABBITMQ_CONNECTION_POOL_SIZE'] || 5,
      channel_pool_size: ENV['RABBITMQ_CHANNEL_POOL_SIZE'] || 10
    },
    kafka: {
      brokers: ENV['KAFKA_BROKERS']&.split(',') || ['localhost:9092'],
      client_id: ENV['KAFKA_CLIENT_ID'] || 'tusk_ruby_client',
      group_id: ENV['KAFKA_GROUP_ID'] || 'tusk_ruby_group',
      auto_offset_reset: ENV['KAFKA_AUTO_OFFSET_RESET'] || 'earliest',
      enable_auto_commit: ENV['KAFKA_ENABLE_AUTO_COMMIT'] != 'false'
    },
    queues: {
      default: ENV['DEFAULT_QUEUE'] || 'default',
      high_priority: ENV['HIGH_PRIORITY_QUEUE'] || 'high_priority',
      low_priority: ENV['LOW_PRIORITY_QUEUE'] || 'low_priority',
      dead_letter: ENV['DEAD_LETTER_QUEUE'] || 'dead_letter',
      retry: ENV['RETRY_QUEUE'] || 'retry'
    },
    retry: {
      max_attempts: ENV['RETRY_MAX_ATTEMPTS'] || 3,
      backoff_multiplier: ENV['RETRY_BACKOFF_MULTIPLIER'] || 2,
      initial_delay: ENV['RETRY_INITIAL_DELAY'] || 1
    },
    monitoring: {
      enabled: ENV['MESSAGE_QUEUE_MONITORING_ENABLED'] != 'false',
      metrics_port: ENV['MESSAGE_QUEUE_METRICS_PORT'] || 9090,
      health_check_interval: ENV['MESSAGE_QUEUE_HEALTH_CHECK_INTERVAL'] || 30
    }
  }
end
```

### Systemd Service

```ini
# /etc/systemd/system/message-queue-consumer.service
[Unit]
Description=Message Queue Consumer
After=network.target

[Service]
Type=simple
User=deploy
WorkingDirectory=/var/www/tsk/sdk
Environment=RAILS_ENV=production
ExecStart=/usr/bin/bundle exec ruby app/message_queue/consumer_runner.rb
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

### Docker Configuration

```dockerfile
# Dockerfile.message-queue
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    redis

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

CMD ["bundle", "exec", "ruby", "app/message_queue/consumer_runner.rb"]
```

```yaml
# docker-compose.message-queue.yml
version: '3.8'

services:
  message-queue-consumer:
    build:
      context: .
      dockerfile: Dockerfile.message-queue
    environment:
      - RAILS_ENV=production
      - REDIS_URL=redis://redis:6379/1
      - MESSAGE_QUEUE_BACKEND=redis
    depends_on:
      - redis
      - db

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

  db:
    image: postgres:15-alpine
    environment:
      - POSTGRES_DB=message_queue_app
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  redis_data:
  postgres_data:
```

This comprehensive message queue integration guide provides everything needed to build robust asynchronous processing systems with TuskLang and Ruby, including multiple backend support, reliable message delivery, error handling, monitoring, testing, and deployment strategies. 
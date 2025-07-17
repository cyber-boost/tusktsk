# Notification Integration with TuskLang and Ruby

This guide covers integrating notification systems with TuskLang and Ruby applications for real-time alerts and user communication.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Setup](#basic-setup)
4. [Notification Types](#notification-types)
5. [Delivery Channels](#delivery-channels)
6. [Advanced Features](#advanced-features)
7. [Performance Optimization](#performance-optimization)
8. [Testing](#testing)
9. [Deployment](#deployment)

## Overview

Notification systems enable real-time communication with users through various channels. This guide shows how to integrate comprehensive notification systems with TuskLang and Ruby applications.

### Key Features

- **Multiple delivery channels** (email, SMS, push, in-app)
- **Notification templates** and personalization
- **Real-time delivery** with WebSockets
- **Notification preferences** and opt-outs
- **Delivery tracking** and analytics
- **Batch notifications** support

## Installation

### Dependencies

```ruby
# Gemfile
gem 'twilio-ruby'
gem 'fcm'
gem 'aws-sdk-sns'
gem 'redis'
gem 'connection_pool'
```

### TuskLang Configuration

```tusk
# config/notification.tusk
notification:
  channels:
    email: true
    sms: true
    push: true
    in_app: true
  
  email:
    provider: "smtp"  # smtp, sendgrid, mailgun
    smtp:
      host: "localhost"
      port: 587
      username: "user"
      password: "pass"
      domain: "example.com"
    sendgrid:
      api_key: "your_api_key"
    mailgun:
      api_key: "your_api_key"
      domain: "example.com"
  
  sms:
    provider: "twilio"  # twilio, aws_sns
    twilio:
      account_sid: "your_account_sid"
      auth_token: "your_auth_token"
      from_number: "+1234567890"
    aws_sns:
      region: "us-east-1"
      access_key_id: "your_access_key"
      secret_access_key: "your_secret_key"
  
  push:
    provider: "fcm"  # fcm, aws_sns
    fcm:
      server_key: "your_server_key"
    aws_sns:
      region: "us-east-1"
      platform_application_arn: "your_platform_arn"
  
  templates:
    path: "app/views/notifications"
    cache_enabled: true
    cache_ttl: 3600
  
  delivery:
    batch_size: 100
    retry_attempts: 3
    retry_delay: 60
    timeout: 30
  
  monitoring:
    enabled: true
    metrics_port: 9090
    health_check_interval: 30
```

## Basic Setup

### Notification Manager

```ruby
# app/notifications/notification_manager.rb
class NotificationManager
  include Singleton

  def initialize
    @config = Rails.application.config.notification
    @channels = create_channels
  end

  def send_notification(user, template, data = {}, options = {})
    notification = create_notification(user, template, data, options)
    
    channels = determine_channels(user, options)
    channels.each do |channel|
      send_to_channel(channel, notification)
    end
    
    notification
  end

  def send_bulk_notification(users, template, data = {}, options = {})
    notifications = []
    
    users.find_each(batch_size: @config[:delivery][:batch_size]) do |user|
      notification = send_notification(user, template, data, options)
      notifications << notification
    end
    
    notifications
  end

  def mark_as_read(notification_id, user_id)
    notification = Notification.find_by(id: notification_id, user_id: user_id)
    notification&.update!(read_at: Time.current)
  end

  def mark_all_as_read(user_id)
    Notification.where(user_id: user_id, read_at: nil)
                .update_all(read_at: Time.current)
  end

  def delete_notification(notification_id, user_id)
    Notification.find_by(id: notification_id, user_id: user_id)&.destroy
  end

  def get_user_notifications(user_id, options = {})
    notifications = Notification.where(user_id: user_id)
    
    notifications = notifications.where(read_at: nil) if options[:unread_only]
    notifications = notifications.limit(options[:limit] || 20)
    notifications = notifications.offset(options[:offset] || 0)
    
    notifications.order(created_at: :desc)
  end

  def health_check
    {
      status: 'healthy',
      channels: channel_health_checks,
      timestamp: Time.current.iso8601
    }
  end

  private

  def create_channels
    {
      email: EmailChannel.new(@config[:email]),
      sms: SmsChannel.new(@config[:sms]),
      push: PushChannel.new(@config[:push]),
      in_app: InAppChannel.new
    }
  end

  def create_notification(user, template, data, options)
    Notification.create!(
      user: user,
      template: template,
      data: data,
      channels: options[:channels] || ['in_app'],
      scheduled_at: options[:scheduled_at],
      priority: options[:priority] || 'normal'
    )
  end

  def determine_channels(user, options)
    channels = options[:channels] || ['in_app']
    
    # Filter based on user preferences
    user_preferences = user.notification_preferences
    channels.select { |channel| user_preferences[channel] }
  end

  def send_to_channel(channel_name, notification)
    channel = @channels[channel_name.to_sym]
    return unless channel
    
    begin
      channel.send(notification)
      track_delivery_success(notification, channel_name)
    rescue => e
      track_delivery_error(notification, channel_name, e.message)
      raise e
    end
  end

  def channel_health_checks
    @channels.map do |name, channel|
      {
        name: name,
        status: channel.health_check[:status]
      }
    end
  end

  def track_delivery_success(notification, channel)
    return unless @config[:monitoring][:enabled]
    
    Rails.logger.debug "Notification delivered: #{notification.id} via #{channel}"
  end

  def track_delivery_error(notification, channel, error)
    return unless @config[:monitoring][:enabled]
    
    Rails.logger.error "Notification delivery failed: #{notification.id} via #{channel} - #{error}"
  end
end
```

### Base Channel

```ruby
# app/notifications/channels/base_channel.rb
class BaseChannel
  def send(notification)
    raise NotImplementedError, "#{self.class} must implement send"
  end

  def health_check
    raise NotImplementedError, "#{self.class} must implement health_check"
  end

  protected

  def render_template(template, data)
    template_path = Rails.application.config.notification[:templates][:path]
    template_file = File.join(template_path, "#{template}.erb")
    
    return nil unless File.exist?(template_file)
    
    template_content = File.read(template_file)
    ERB.new(template_content).result(binding)
  end

  def log_delivery(notification, channel, success = true)
    NotificationDelivery.create!(
      notification: notification,
      channel: channel,
      success: success,
      delivered_at: Time.current
    )
  end
end
```

## Notification Types

### User Notifications

```ruby
# app/notifications/types/user_notifications.rb
class UserNotifications
  def self.welcome(user)
    NotificationManager.instance.send_notification(
      user,
      'welcome',
      {
        user_name: user.name,
        activation_url: generate_activation_url(user)
      },
      { channels: ['email', 'in_app'] }
    )
  end

  def self.password_reset(user, token)
    NotificationManager.instance.send_notification(
      user,
      'password_reset',
      {
        user_name: user.name,
        reset_url: generate_reset_url(token)
      },
      { channels: ['email'], priority: 'high' }
    )
  end

  def self.email_verification(user, token)
    NotificationManager.instance.send_notification(
      user,
      'email_verification',
      {
        user_name: user.name,
        verification_url: generate_verification_url(token)
      },
      { channels: ['email'] }
    )
  end

  def self.account_locked(user, reason)
    NotificationManager.instance.send_notification(
      user,
      'account_locked',
      {
        user_name: user.name,
        reason: reason,
        unlock_url: generate_unlock_url(user)
      },
      { channels: ['email', 'sms'], priority: 'high' }
    )
  end

  def self.profile_updated(user)
    NotificationManager.instance.send_notification(
      user,
      'profile_updated',
      {
        user_name: user.name,
        updated_at: user.updated_at
      },
      { channels: ['in_app'] }
    )
  end

  private

  def self.generate_activation_url(user)
    Rails.application.routes.url_helpers.activate_user_url(
      token: user.activation_token,
      host: Rails.application.config.action_mailer.default_url_options[:host]
    )
  end

  def self.generate_reset_url(token)
    Rails.application.routes.url_helpers.reset_password_url(
      token: token,
      host: Rails.application.config.action_mailer.default_url_options[:host]
    )
  end

  def self.generate_verification_url(token)
    Rails.application.routes.url_helpers.verify_email_url(
      token: token,
      host: Rails.application.config.action_mailer.default_url_options[:host]
    )
  end

  def self.generate_unlock_url(user)
    Rails.application.routes.url_helpers.unlock_account_url(
      token: user.unlock_token,
      host: Rails.application.config.action_mailer.default_url_options[:host]
    )
  end
end
```

### Post Notifications

```ruby
# app/notifications/types/post_notifications.rb
class PostNotifications
  def self.new_post(post)
    post.user.followers.find_each do |follower|
      NotificationManager.instance.send_notification(
        follower,
        'new_post',
        {
          user_name: post.user.name,
          post_title: post.title,
          post_url: generate_post_url(post)
        },
        { channels: ['email', 'in_app'] }
      )
    end
  end

  def self.post_liked(post, liker)
    return if post.user == liker
    
    NotificationManager.instance.send_notification(
      post.user,
      'post_liked',
      {
        liker_name: liker.name,
        post_title: post.title,
        post_url: generate_post_url(post)
      },
      { channels: ['in_app'] }
    )
  end

  def self.post_commented(post, commenter)
    return if post.user == commenter
    
    NotificationManager.instance.send_notification(
      post.user,
      'post_commented',
      {
        commenter_name: commenter.name,
        post_title: post.title,
        comment_preview: commenter.comment.content.truncate(50),
        post_url: generate_post_url(post)
      },
      { channels: ['email', 'in_app'] }
    )
  end

  def self.post_shared(post, sharer)
    return if post.user == sharer
    
    NotificationManager.instance.send_notification(
      post.user,
      'post_shared',
      {
        sharer_name: sharer.name,
        post_title: post.title,
        post_url: generate_post_url(post)
      },
      { channels: ['in_app'] }
    )
  end

  def self.trending_post(post)
    NotificationManager.instance.send_notification(
      post.user,
      'trending_post',
      {
        post_title: post.title,
        views_count: post.views.count,
        post_url: generate_post_url(post)
      },
      { channels: ['in_app'], priority: 'high' }
    )
  end

  private

  def self.generate_post_url(post)
    Rails.application.routes.url_helpers.post_url(
      post,
      host: Rails.application.config.action_mailer.default_url_options[:host]
    )
  end
end
```

### System Notifications

```ruby
# app/notifications/types/system_notifications.rb
class SystemNotifications
  def self.maintenance_scheduled(scheduled_at, duration)
    User.all.find_each do |user|
      NotificationManager.instance.send_notification(
        user,
        'maintenance_scheduled',
        {
          scheduled_at: scheduled_at.strftime('%B %d, %Y at %I:%M %p'),
          duration: duration,
          estimated_end: (scheduled_at + duration.hours).strftime('%I:%M %p')
        },
        { channels: ['email', 'in_app'], priority: 'high' }
      )
    end
  end

  def self.new_feature(feature_name, description)
    User.all.find_each do |user|
      NotificationManager.instance.send_notification(
        user,
        'new_feature',
        {
          feature_name: feature_name,
          description: description
        },
        { channels: ['email', 'in_app'] }
      )
    end
  end

  def self.security_alert(user, alert_type, details)
    NotificationManager.instance.send_notification(
      user,
      'security_alert',
      {
        alert_type: alert_type,
        details: details,
        timestamp: Time.current.strftime('%B %d, %Y at %I:%M %p')
      },
      { channels: ['email', 'sms'], priority: 'critical' }
    )
  end

  def self.payment_failed(user, amount, reason)
    NotificationManager.instance.send_notification(
      user,
      'payment_failed',
      {
        amount: amount,
        reason: reason,
        retry_url: generate_payment_retry_url(user)
      },
      { channels: ['email', 'sms'], priority: 'high' }
    )
  end

  def self.payment_successful(user, amount)
    NotificationManager.instance.send_notification(
      user,
      'payment_successful',
      {
        amount: amount,
        transaction_id: SecureRandom.hex(8)
      },
      { channels: ['email', 'in_app'] }
    )
  end

  private

  def self.generate_payment_retry_url(user)
    Rails.application.routes.url_helpers.payment_retry_url(
      user_id: user.id,
      host: Rails.application.config.action_mailer.default_url_options[:host]
    )
  end
end
```

## Delivery Channels

### Email Channel

```ruby
# app/notifications/channels/email_channel.rb
class EmailChannel < BaseChannel
  def initialize(config)
    @config = config
    @provider = create_provider
  end

  def send(notification)
    user = notification.user
    template = notification.template
    data = notification.data
    
    subject = render_subject(template, data)
    body = render_body(template, data)
    
    @provider.send_email(
      to: user.email,
      subject: subject,
      body: body,
      html_body: render_html_body(template, data)
    )
    
    log_delivery(notification, 'email', true)
  rescue => e
    log_delivery(notification, 'email', false)
    raise e
  end

  def health_check
    begin
      @provider.health_check
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end

  private

  def create_provider
    case @config[:provider]
    when 'smtp'
      SmtpProvider.new(@config[:smtp])
    when 'sendgrid'
      SendgridProvider.new(@config[:sendgrid])
    when 'mailgun'
      MailgunProvider.new(@config[:mailgun])
    else
      raise "Unsupported email provider: #{@config[:provider]}"
    end
  end

  def render_subject(template, data)
    subject_template = render_template("#{template}_subject", data)
    subject_template&.strip || "Notification from #{Rails.application.config.app_name}"
  end

  def render_body(template, data)
    render_template("#{template}_text", data)
  end

  def render_html_body(template, data)
    render_template("#{template}_html", data)
  end
end
```

### SMS Channel

```ruby
# app/notifications/channels/sms_channel.rb
class SmsChannel < BaseChannel
  def initialize(config)
    @config = config
    @provider = create_provider
  end

  def send(notification)
    user = notification.user
    template = notification.template
    data = notification.data
    
    message = render_message(template, data)
    
    @provider.send_sms(
      to: user.phone_number,
      message: message
    )
    
    log_delivery(notification, 'sms', true)
  rescue => e
    log_delivery(notification, 'sms', false)
    raise e
  end

  def health_check
    begin
      @provider.health_check
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end

  private

  def create_provider
    case @config[:provider]
    when 'twilio'
      TwilioProvider.new(@config[:twilio])
    when 'aws_sns'
      AwsSnsProvider.new(@config[:aws_sns])
    else
      raise "Unsupported SMS provider: #{@config[:provider]}"
    end
  end

  def render_message(template, data)
    render_template("#{template}_sms", data)
  end
end
```

### Push Channel

```ruby
# app/notifications/channels/push_channel.rb
class PushChannel < BaseChannel
  def initialize(config)
    @config = config
    @provider = create_provider
  end

  def send(notification)
    user = notification.user
    template = notification.template
    data = notification.data
    
    title = render_title(template, data)
    body = render_body(template, data)
    
    user.devices.find_each do |device|
      @provider.send_push(
        device_token: device.token,
        title: title,
        body: body,
        data: data
      )
    end
    
    log_delivery(notification, 'push', true)
  rescue => e
    log_delivery(notification, 'push', false)
    raise e
  end

  def health_check
    begin
      @provider.health_check
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end

  private

  def create_provider
    case @config[:provider]
    when 'fcm'
      FcmProvider.new(@config[:fcm])
    when 'aws_sns'
      AwsSnsPushProvider.new(@config[:aws_sns])
    else
      raise "Unsupported push provider: #{@config[:provider]}"
    end
  end

  def render_title(template, data)
    render_template("#{template}_push_title", data)
  end

  def render_body(template, data)
    render_template("#{template}_push_body", data)
  end
end
```

### In-App Channel

```ruby
# app/notifications/channels/in_app_channel.rb
class InAppChannel < BaseChannel
  def send(notification)
    # In-app notifications are stored in the database
    # and delivered via WebSocket or API
    log_delivery(notification, 'in_app', true)
    
    # Broadcast to WebSocket if user is online
    broadcast_notification(notification)
  end

  def health_check
    { status: 'healthy' }
  end

  private

  def broadcast_notification(notification)
    # Broadcast via WebSocket if user is online
    WebSocketServer.instance.send_to_user(
      notification.user_id,
      {
        type: 'notification',
        notification: notification.as_json
      }
    )
  rescue => e
    Rails.logger.error "Failed to broadcast notification: #{e.message}"
  end
end
```

## Advanced Features

### Notification Preferences

```ruby
# app/notifications/preferences/notification_preferences.rb
class NotificationPreferences
  include Singleton

  def get_user_preferences(user)
    user.notification_preferences || default_preferences
  end

  def update_user_preferences(user, preferences)
    user.update!(notification_preferences: preferences)
  end

  def can_send_notification?(user, channel, notification_type)
    preferences = get_user_preferences(user)
    
    # Check if channel is enabled
    return false unless preferences[channel]
    
    # Check if notification type is allowed
    return false if preferences[:blocked_types]&.include?(notification_type)
    
    # Check quiet hours
    return false if in_quiet_hours?(preferences)
    
    true
  end

  def unsubscribe_user(user, channel, notification_type = nil)
    preferences = get_user_preferences(user)
    
    if notification_type
      preferences[:blocked_types] ||= []
      preferences[:blocked_types] << notification_type
    else
      preferences[channel] = false
    end
    
    update_user_preferences(user, preferences)
  end

  def resubscribe_user(user, channel, notification_type = nil)
    preferences = get_user_preferences(user)
    
    if notification_type
      preferences[:blocked_types]&.delete(notification_type)
    else
      preferences[channel] = true
    end
    
    update_user_preferences(user, preferences)
  end

  private

  def default_preferences
    {
      email: true,
      sms: true,
      push: true,
      in_app: true,
      quiet_hours_start: '22:00',
      quiet_hours_end: '08:00',
      blocked_types: []
    }
  end

  def in_quiet_hours?(preferences)
    return false unless preferences[:quiet_hours_start] && preferences[:quiet_hours_end]
    
    current_time = Time.current.strftime('%H:%M')
    start_time = preferences[:quiet_hours_start]
    end_time = preferences[:quiet_hours_end]
    
    if start_time < end_time
      current_time >= start_time && current_time <= end_time
    else
      current_time >= start_time || current_time <= end_time
    end
  end
end
```

### Notification Analytics

```ruby
# app/notifications/analytics/notification_analytics.rb
class NotificationAnalytics
  include Singleton

  def initialize
    @redis = Redis.new
  end

  def track_notification_sent(notification, channel)
    key = "notifications:sent:#{Date.current}"
    @redis.hincrby(key, channel, 1)
    @redis.expire(key, 30.days.to_i)
  end

  def track_notification_delivered(notification, channel)
    key = "notifications:delivered:#{Date.current}"
    @redis.hincrby(key, channel, 1)
    @redis.expire(key, 30.days.to_i)
  end

  def track_notification_opened(notification, channel)
    key = "notifications:opened:#{Date.current}"
    @redis.hincrby(key, channel, 1)
    @redis.expire(key, 30.days.to_i)
  end

  def get_delivery_stats(days = 7)
    end_date = Date.current
    start_date = end_date - days.days
    
    {
      sent: get_total_sent(start_date, end_date),
      delivered: get_total_delivered(start_date, end_date),
      opened: get_total_opened(start_date, end_date),
      delivery_rate: calculate_delivery_rate(start_date, end_date),
      open_rate: calculate_open_rate(start_date, end_date)
    }
  end

  def get_channel_stats(days = 7)
    end_date = Date.current
    start_date = end_date - days.days
    
    channels = ['email', 'sms', 'push', 'in_app']
    
    channels.map do |channel|
      {
        channel: channel,
        sent: get_channel_sent(channel, start_date, end_date),
        delivered: get_channel_delivered(channel, start_date, end_date),
        opened: get_channel_opened(channel, start_date, end_date)
      }
    end
  end

  private

  def get_total_sent(start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      key = "notifications:sent:#{date}"
      total += @redis.hvals(key).map(&:to_i).sum
    end
    total
  end

  def get_total_delivered(start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      key = "notifications:delivered:#{date}"
      total += @redis.hvals(key).map(&:to_i).sum
    end
    total
  end

  def get_total_opened(start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      key = "notifications:opened:#{date}"
      total += @redis.hvals(key).map(&:to_i).sum
    end
    total
  end

  def calculate_delivery_rate(start_date, end_date)
    sent = get_total_sent(start_date, end_date)
    delivered = get_total_delivered(start_date, end_date)
    
    return 0.0 if sent == 0
    
    (delivered.to_f / sent * 100).round(2)
  end

  def calculate_open_rate(start_date, end_date)
    delivered = get_total_delivered(start_date, end_date)
    opened = get_total_opened(start_date, end_date)
    
    return 0.0 if delivered == 0
    
    (opened.to_f / delivered * 100).round(2)
  end

  def get_channel_sent(channel, start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      key = "notifications:sent:#{date}"
      total += @redis.hget(key, channel).to_i
    end
    total
  end

  def get_channel_delivered(channel, start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      key = "notifications:delivered:#{date}"
      total += @redis.hget(key, channel).to_i
    end
    total
  end

  def get_channel_opened(channel, start_date, end_date)
    total = 0
    (start_date..end_date).each do |date|
      key = "notifications:opened:#{date}"
      total += @redis.hget(key, channel).to_i
    end
    total
  end
end
```

## Performance Optimization

### Notification Batching

```ruby
# app/notifications/batching/notification_batcher.rb
class NotificationBatcher
  include Singleton

  def initialize
    @config = Rails.application.config.notification
  end

  def batch_send_notifications(users, template, data = {}, options = {})
    batch_size = options[:batch_size] || @config[:delivery][:batch_size]
    
    users.each_slice(batch_size) do |batch|
      batch.each do |user|
        begin
          NotificationManager.instance.send_notification(user, template, data, options)
        rescue => e
          Rails.logger.error "Failed to send notification to user #{user.id}: #{e.message}"
        end
      end
      
      # Add delay between batches to avoid rate limiting
      sleep(1) if batch_size > 10
    end
  end

  def schedule_batch_notification(users, template, data = {}, options = {})
    BatchNotificationJob.perform_async(users.pluck(:id), template, data, options)
  end
end
```

## Testing

### Notification Test Helper

```ruby
# spec/support/notification_helper.rb
module NotificationHelper
  def clear_notifications
    Notification.delete_all
    NotificationDelivery.delete_all
  end

  def expect_notification_sent(user, template)
    expect(Notification.where(user: user, template: template)).to exist
  end

  def expect_notification_delivered(user, template, channel)
    notification = Notification.find_by(user: user, template: template)
    expect(NotificationDelivery.where(notification: notification, channel: channel, success: true)).to exist
  end
end

RSpec.configure do |config|
  config.include NotificationHelper, type: :notification
  
  config.before(:each, type: :notification) do
    clear_notifications
  end
end
```

### Notification Tests

```ruby
# spec/notifications/user_notifications_spec.rb
RSpec.describe UserNotifications, type: :notification do
  let(:user) { create(:user) }

  describe '.welcome' do
    it 'sends welcome notification' do
      expect {
        UserNotifications.welcome(user)
      }.to change { Notification.count }.by(1)
      
      expect_notification_sent(user, 'welcome')
    end
  end

  describe '.password_reset' do
    it 'sends password reset notification' do
      token = SecureRandom.hex(32)
      
      expect {
        UserNotifications.password_reset(user, token)
      }.to change { Notification.count }.by(1)
      
      expect_notification_sent(user, 'password_reset')
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # Notification configuration
  config.notification = {
    channels: {
      email: ENV['NOTIFICATION_EMAIL_ENABLED'] != 'false',
      sms: ENV['NOTIFICATION_SMS_ENABLED'] != 'false',
      push: ENV['NOTIFICATION_PUSH_ENABLED'] != 'false',
      in_app: ENV['NOTIFICATION_IN_APP_ENABLED'] != 'false'
    },
    email: {
      provider: ENV['EMAIL_PROVIDER'] || 'smtp',
      smtp: {
        host: ENV['SMTP_HOST'] || 'localhost',
        port: ENV['SMTP_PORT'] || 587,
        username: ENV['SMTP_USERNAME'],
        password: ENV['SMTP_PASSWORD'],
        domain: ENV['SMTP_DOMAIN']
      },
      sendgrid: {
        api_key: ENV['SENDGRID_API_KEY']
      },
      mailgun: {
        api_key: ENV['MAILGUN_API_KEY'],
        domain: ENV['MAILGUN_DOMAIN']
      }
    },
    sms: {
      provider: ENV['SMS_PROVIDER'] || 'twilio',
      twilio: {
        account_sid: ENV['TWILIO_ACCOUNT_SID'],
        auth_token: ENV['TWILIO_AUTH_TOKEN'],
        from_number: ENV['TWILIO_FROM_NUMBER']
      },
      aws_sns: {
        region: ENV['AWS_SNS_REGION'] || 'us-east-1',
        access_key_id: ENV['AWS_ACCESS_KEY_ID'],
        secret_access_key: ENV['AWS_SECRET_ACCESS_KEY']
      }
    },
    push: {
      provider: ENV['PUSH_PROVIDER'] || 'fcm',
      fcm: {
        server_key: ENV['FCM_SERVER_KEY']
      },
      aws_sns: {
        region: ENV['AWS_SNS_REGION'] || 'us-east-1',
        platform_application_arn: ENV['AWS_SNS_PLATFORM_ARN']
      }
    },
    templates: {
      path: ENV['NOTIFICATION_TEMPLATES_PATH'] || 'app/views/notifications',
      cache_enabled: ENV['NOTIFICATION_TEMPLATE_CACHE_ENABLED'] != 'false',
      cache_ttl: ENV['NOTIFICATION_TEMPLATE_CACHE_TTL'] || 3600
    },
    delivery: {
      batch_size: ENV['NOTIFICATION_BATCH_SIZE'] || 100,
      retry_attempts: ENV['NOTIFICATION_RETRY_ATTEMPTS'] || 3,
      retry_delay: ENV['NOTIFICATION_RETRY_DELAY'] || 60,
      timeout: ENV['NOTIFICATION_TIMEOUT'] || 30
    },
    monitoring: {
      enabled: ENV['NOTIFICATION_MONITORING_ENABLED'] != 'false',
      metrics_port: ENV['NOTIFICATION_METRICS_PORT'] || 9090,
      health_check_interval: ENV['NOTIFICATION_HEALTH_CHECK_INTERVAL'] || 30
    }
  }
end
```

### Docker Configuration

```dockerfile
# Dockerfile.notification
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    redis

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

CMD ["bundle", "exec", "ruby", "app/notifications/notification_runner.rb"]
```

```yaml
# docker-compose.notification.yml
version: '3.8'

services:
  notification-service:
    build:
      context: .
      dockerfile: Dockerfile.notification
    environment:
      - RAILS_ENV=production
      - REDIS_URL=redis://redis:6379/2
      - NOTIFICATION_EMAIL_ENABLED=true
      - NOTIFICATION_SMS_ENABLED=true
      - NOTIFICATION_PUSH_ENABLED=true
      - NOTIFICATION_IN_APP_ENABLED=true
    depends_on:
      - redis

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

This comprehensive notification integration guide provides everything needed to build robust notification systems with TuskLang and Ruby, including multiple delivery channels, templates, preferences, analytics, testing, and deployment strategies. 
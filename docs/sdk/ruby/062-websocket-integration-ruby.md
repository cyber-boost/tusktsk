# WebSocket Integration with TuskLang and Ruby

This guide covers integrating WebSockets with TuskLang and Ruby applications for real-time communication.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Setup](#basic-setup)
4. [Connection Management](#connection-management)
5. [Message Handling](#message-handling)
6. [Authentication](#authentication)
7. [Channels and Rooms](#channels-and-rooms)
8. [Performance Optimization](#performance-optimization)
9. [Testing](#testing)
10. [Deployment](#deployment)

## Overview

WebSockets provide real-time bidirectional communication between clients and servers. This guide shows how to integrate WebSockets with TuskLang and Ruby applications.

### Key Features

- **Real-time communication** with WebSocket connections
- **Channel-based messaging** for organized communication
- **Authentication and authorization** for secure connections
- **Scalable architecture** with Redis pub/sub
- **Performance monitoring** and optimization
- **Comprehensive testing** strategies

## Installation

### Dependencies

```ruby
# Gemfile
gem 'websocket-rails'
gem 'redis'
gem 'connection_pool'
gem 'jwt'
gem 'bcrypt'
```

### TuskLang Configuration

```tusk
# config/websocket.tusk
websocket:
  server:
    host: "0.0.0.0"
    port: 8080
    max_connections: 10000
  
  authentication:
    enabled: true
    token_expiry: 3600
    refresh_token_expiry: 86400
  
  channels:
    default_room: "general"
    max_channels_per_user: 50
    max_users_per_channel: 1000
  
  performance:
    heartbeat_interval: 30
    connection_timeout: 300
    message_queue_size: 1000
  
  redis:
    url: "redis://localhost:6379/1"
    pool_size: 10
    pool_timeout: 5
```

## Basic Setup

### WebSocket Server

```ruby
# app/websocket/websocket_server.rb
class WebSocketServer
  include Singleton
  
  def initialize
    @connections = {}
    @channels = {}
    @redis = Redis.new(url: Rails.application.config.websocket[:redis][:url])
    setup_redis_subscriptions
  end

  def start
    EM.run do
      EM.start_server(
        Rails.application.config.websocket[:server][:host],
        Rails.application.config.websocket[:server][:port],
        WebSocketConnection
      )
      
      Rails.logger.info "WebSocket server started on #{Rails.application.config.websocket[:server][:host]}:#{Rails.application.config.websocket[:server][:port]}"
    end
  end

  def add_connection(connection)
    @connections[connection.id] = connection
    Rails.logger.info "New WebSocket connection: #{connection.id}"
  end

  def remove_connection(connection_id)
    @connections.delete(connection_id)
    Rails.logger.info "WebSocket connection closed: #{connection_id}"
  end

  def broadcast_to_channel(channel, message)
    @redis.publish("websocket:channel:#{channel}", message.to_json)
  end

  def send_to_user(user_id, message)
    @redis.publish("websocket:user:#{user_id}", message.to_json)
  end

  private

  def setup_redis_subscriptions
    Thread.new do
      @redis.subscribe("websocket:channel:*", "websocket:user:*") do |on|
        on.message do |channel, message|
          handle_redis_message(channel, message)
        end
      end
    end
  end

  def handle_redis_message(channel, message)
    parsed_message = JSON.parse(message)
    
    case channel
    when /^websocket:channel:(.+)$/
      channel_name = $1
      broadcast_to_channel_connections(channel_name, parsed_message)
    when /^websocket:user:(.+)$/
      user_id = $1
      send_to_user_connection(user_id, parsed_message)
    end
  end

  def broadcast_to_channel_connections(channel_name, message)
    return unless @channels[channel_name]
    
    @channels[channel_name].each do |connection_id|
      connection = @connections[connection_id]
      connection.send_message(message) if connection
    end
  end

  def send_to_user_connection(user_id, message)
    @connections.each do |connection_id, connection|
      if connection.user_id == user_id
        connection.send_message(message)
        break
      end
    end
  end
end
```

### WebSocket Connection

```ruby
# app/websocket/websocket_connection.rb
class WebSocketConnection
  include EM::WebSocket::Connection

  attr_accessor :id, :user_id, :channels

  def initialize
    @id = SecureRandom.uuid
    @channels = Set.new
    @authenticated = false
    @last_heartbeat = Time.current
  end

  def post_init
    WebSocketServer.instance.add_connection(self)
    send_welcome_message
    start_heartbeat
  end

  def receive_message(message)
    begin
      data = JSON.parse(message)
      handle_message(data)
    rescue JSON::ParserError => e
      send_error("Invalid JSON format")
    rescue StandardError => e
      Rails.logger.error "WebSocket error: #{e.message}"
      send_error("Internal server error")
    end
  end

  def unbind
    WebSocketServer.instance.remove_connection(@id)
    leave_all_channels
  end

  def send_message(message)
    send(message.to_json)
  end

  private

  def handle_message(data)
    case data['type']
    when 'authenticate'
      authenticate(data['token'])
    when 'join_channel'
      join_channel(data['channel'])
    when 'leave_channel'
      leave_channel(data['channel'])
    when 'send_message'
      send_channel_message(data['channel'], data['message'])
    when 'heartbeat'
      handle_heartbeat
    else
      send_error("Unknown message type: #{data['type']}")
    end
  end

  def authenticate(token)
    begin
      decoded_token = JWT.decode(token, Rails.application.secrets.secret_key_base, true, algorithm: 'HS256')
      user_id = decoded_token[0]['user_id']
      user = User.find(user_id)
      
      @user_id = user.id
      @authenticated = true
      
      send_success('authenticated', { user_id: user.id, username: user.name })
    rescue JWT::DecodeError, ActiveRecord::RecordNotFound => e
      send_error("Authentication failed")
    end
  end

  def join_channel(channel_name)
    return send_error("Not authenticated") unless @authenticated
    return send_error("Channel name required") if channel_name.blank?
    
    if @channels.size >= Rails.application.config.websocket[:channels][:max_channels_per_user]
      return send_error("Maximum channels per user exceeded")
    end

    @channels.add(channel_name)
    WebSocketServer.instance.add_to_channel(channel_name, @id)
    
    send_success('channel_joined', { channel: channel_name })
    broadcast_to_channel(channel_name, {
      type: 'user_joined',
      user_id: @user_id,
      channel: channel_name
    })
  end

  def leave_channel(channel_name)
    return send_error("Not authenticated") unless @authenticated
    
    @channels.delete(channel_name)
    WebSocketServer.instance.remove_from_channel(channel_name, @id)
    
    send_success('channel_left', { channel: channel_name })
    broadcast_to_channel(channel_name, {
      type: 'user_left',
      user_id: @user_id,
      channel: channel_name
    })
  end

  def send_channel_message(channel_name, message)
    return send_error("Not authenticated") unless @authenticated
    return send_error("Not in channel") unless @channels.include?(channel_name)
    
    broadcast_to_channel(channel_name, {
      type: 'message',
      user_id: @user_id,
      channel: channel_name,
      message: message,
      timestamp: Time.current.iso8601
    })
  end

  def handle_heartbeat
    @last_heartbeat = Time.current
    send_message({ type: 'heartbeat_ack' })
  end

  def start_heartbeat
    EM.add_periodic_timer(Rails.application.config.websocket[:performance][:heartbeat_interval]) do
      check_heartbeat
    end
  end

  def check_heartbeat
    timeout = Rails.application.config.websocket[:performance][:connection_timeout]
    if Time.current - @last_heartbeat > timeout
      Rails.logger.warn "WebSocket connection #{@id} timed out"
      close_connection
    end
  end

  def send_welcome_message
    send_message({
      type: 'welcome',
      connection_id: @id,
      server_time: Time.current.iso8601
    })
  end

  def send_success(action, data = {})
    send_message({
      type: 'success',
      action: action,
      data: data
    })
  end

  def send_error(message)
    send_message({
      type: 'error',
      message: message
    })
  end

  def broadcast_to_channel(channel, message)
    WebSocketServer.instance.broadcast_to_channel(channel, message)
  end

  def leave_all_channels
    @channels.each do |channel|
      WebSocketServer.instance.remove_from_channel(channel, @id)
    end
  end
end
```

## Connection Management

### Connection Pool

```ruby
# app/websocket/connection_pool.rb
class ConnectionPool
  include Singleton

  def initialize
    @pool = ConnectionPool.new(size: Rails.application.config.websocket[:redis][:pool_size]) do
      Redis.new(url: Rails.application.config.websocket[:redis][:url])
    end
  end

  def with_connection
    @pool.with do |redis|
      yield redis
    end
  end

  def publish(channel, message)
    with_connection do |redis|
      redis.publish(channel, message.to_json)
    end
  end

  def subscribe(channel, &block)
    with_connection do |redis|
      redis.subscribe(channel) do |on|
        on.message(&block)
      end
    end
  end
end
```

### Channel Management

```ruby
# app/websocket/channel_manager.rb
class ChannelManager
  include Singleton

  def initialize
    @channels = {}
    @user_channels = {}
  end

  def add_user_to_channel(channel_name, user_id, connection_id)
    @channels[channel_name] ||= Set.new
    @channels[channel_name].add(connection_id)
    
    @user_channels[user_id] ||= Set.new
    @user_channels[user_id].add(channel_name)
    
    Rails.logger.info "User #{user_id} joined channel #{channel_name}"
  end

  def remove_user_from_channel(channel_name, user_id, connection_id)
    @channels[channel_name]&.delete(connection_id)
    @user_channels[user_id]&.delete(channel_name)
    
    Rails.logger.info "User #{user_id} left channel #{channel_name}"
  end

  def get_channel_users(channel_name)
    @channels[channel_name] || Set.new
  end

  def get_user_channels(user_id)
    @user_channels[user_id] || Set.new
  end

  def channel_exists?(channel_name)
    @channels.key?(channel_name)
  end

  def user_in_channel?(user_id, channel_name)
    @user_channels[user_id]&.include?(channel_name)
  end
end
```

## Message Handling

### Message Types

```ruby
# app/websocket/message_types.rb
module MessageTypes
  AUTHENTICATE = 'authenticate'
  JOIN_CHANNEL = 'join_channel'
  LEAVE_CHANNEL = 'leave_channel'
  SEND_MESSAGE = 'send_message'
  HEARTBEAT = 'heartbeat'
  WELCOME = 'welcome'
  SUCCESS = 'success'
  ERROR = 'error'
  MESSAGE = 'message'
  USER_JOINED = 'user_joined'
  USER_LEFT = 'user_left'
  CHANNEL_LIST = 'channel_list'
  USER_LIST = 'user_list'
end

# app/websocket/message_handler.rb
class MessageHandler
  def self.handle(connection, data)
    handler = new(connection)
    handler.process(data)
  end

  def initialize(connection)
    @connection = connection
  end

  def process(data)
    case data['type']
    when MessageTypes::AUTHENTICATE
      handle_authenticate(data)
    when MessageTypes::JOIN_CHANNEL
      handle_join_channel(data)
    when MessageTypes::LEAVE_CHANNEL
      handle_leave_channel(data)
    when MessageTypes::SEND_MESSAGE
      handle_send_message(data)
    when MessageTypes::HEARTBEAT
      handle_heartbeat
    else
      @connection.send_error("Unknown message type: #{data['type']}")
    end
  end

  private

  def handle_authenticate(data)
    token = data['token']
    return @connection.send_error("Token required") if token.blank?

    begin
      decoded_token = JWT.decode(token, Rails.application.secrets.secret_key_base, true, algorithm: 'HS256')
      user_id = decoded_token[0]['user_id']
      user = User.find(user_id)
      
      @connection.user_id = user.id
      @connection.authenticated = true
      
      @connection.send_success(MessageTypes::AUTHENTICATE, {
        user_id: user.id,
        username: user.name,
        email: user.email
      })
    rescue JWT::DecodeError, ActiveRecord::RecordNotFound => e
      @connection.send_error("Authentication failed")
    end
  end

  def handle_join_channel(data)
    return @connection.send_error("Not authenticated") unless @connection.authenticated?
    
    channel_name = data['channel']
    return @connection.send_error("Channel name required") if channel_name.blank?

    ChannelManager.instance.add_user_to_channel(channel_name, @connection.user_id, @connection.id)
    @connection.channels.add(channel_name)
    
    @connection.send_success(MessageTypes::JOIN_CHANNEL, { channel: channel_name })
    
    # Notify other users in the channel
    broadcast_to_channel(channel_name, {
      type: MessageTypes::USER_JOINED,
      user_id: @connection.user_id,
      channel: channel_name,
      timestamp: Time.current.iso8601
    })
  end

  def handle_leave_channel(data)
    return @connection.send_error("Not authenticated") unless @connection.authenticated?
    
    channel_name = data['channel']
    return @connection.send_error("Channel name required") if channel_name.blank?

    ChannelManager.instance.remove_user_from_channel(channel_name, @connection.user_id, @connection.id)
    @connection.channels.delete(channel_name)
    
    @connection.send_success(MessageTypes::LEAVE_CHANNEL, { channel: channel_name })
    
    # Notify other users in the channel
    broadcast_to_channel(channel_name, {
      type: MessageTypes::USER_LEFT,
      user_id: @connection.user_id,
      channel: channel_name,
      timestamp: Time.current.iso8601
    })
  end

  def handle_send_message(data)
    return @connection.send_error("Not authenticated") unless @connection.authenticated?
    
    channel_name = data['channel']
    message = data['message']
    
    return @connection.send_error("Channel name required") if channel_name.blank?
    return @connection.send_error("Message required") if message.blank?
    return @connection.send_error("Not in channel") unless @connection.channels.include?(channel_name)

    # Store message in database
    stored_message = Message.create!(
      user_id: @connection.user_id,
      channel: channel_name,
      content: message
    )

    # Broadcast to channel
    broadcast_to_channel(channel_name, {
      type: MessageTypes::MESSAGE,
      id: stored_message.id,
      user_id: @connection.user_id,
      channel: channel_name,
      message: message,
      timestamp: stored_message.created_at.iso8601
    })
  end

  def handle_heartbeat
    @connection.last_heartbeat = Time.current
    @connection.send_message({ type: 'heartbeat_ack' })
  end

  def broadcast_to_channel(channel, message)
    ConnectionPool.instance.publish("websocket:channel:#{channel}", message)
  end
end
```

## Authentication

### JWT Authentication

```ruby
# app/websocket/authentication/jwt_authenticator.rb
class JwtAuthenticator
  def self.authenticate(token)
    return nil if token.blank?

    begin
      decoded_token = JWT.decode(token, Rails.application.secrets.secret_key_base, true, algorithm: 'HS256')
      user_id = decoded_token[0]['user_id']
      User.find(user_id)
    rescue JWT::DecodeError, ActiveRecord::RecordNotFound => e
      nil
    end
  end

  def self.generate_token(user)
    payload = {
      user_id: user.id,
      email: user.email,
      exp: Rails.application.config.websocket[:authentication][:token_expiry].seconds.from_now.to_i
    }
    
    JWT.encode(payload, Rails.application.secrets.secret_key_base, 'HS256')
  end

  def self.refresh_token(user)
    payload = {
      user_id: user.id,
      email: user.email,
      exp: Rails.application.config.websocket[:authentication][:refresh_token_expiry].seconds.from_now.to_i
    }
    
    JWT.encode(payload, Rails.application.secrets.secret_key_base, 'HS256')
  end
end
```

### Authorization

```ruby
# app/websocket/authorization/channel_authorizer.rb
class ChannelAuthorizer
  def self.can_join_channel?(user, channel_name)
    return false if user.nil?
    return false if channel_name.blank?
    
    # Check if user is banned from the channel
    return false if ChannelBan.exists?(user: user, channel: channel_name)
    
    # Check channel-specific permissions
    case channel_name
    when /^admin-/
      user.admin?
    when /^moderator-/
      user.moderator? || user.admin?
    else
      true
    end
  end

  def self.can_send_message?(user, channel_name)
    return false unless can_join_channel?(user, channel_name)
    
    # Check if user is muted
    return false if ChannelMute.exists?(user: user, channel: channel_name)
    
    true
  end

  def self.can_moderate?(user, channel_name)
    return false if user.nil?
    
    user.admin? || 
    (user.moderator? && channel_name.start_with?('moderator-')) ||
    ChannelModerator.exists?(user: user, channel: channel_name)
  end
end
```

## Channels and Rooms

### Channel Types

```ruby
# app/models/channel.rb
class Channel < ApplicationRecord
  has_many :messages, dependent: :destroy
  has_many :channel_users, dependent: :destroy
  has_many :users, through: :channel_users
  has_many :channel_bans, dependent: :destroy
  has_many :channel_mutes, dependent: :destroy
  has_many :channel_moderators, dependent: :destroy

  validates :name, presence: true, uniqueness: true
  validates :channel_type, presence: true, inclusion: { in: %w[public private admin moderator] }

  scope :public_channels, -> { where(channel_type: 'public') }
  scope :private_channels, -> { where(channel_type: 'private') }
  scope :admin_channels, -> { where(channel_type: 'admin') }
  scope :moderator_channels, -> { where(channel_type: 'moderator') }

  def public?
    channel_type == 'public'
  end

  def private?
    channel_type == 'private'
  end

  def admin?
    channel_type == 'admin'
  end

  def moderator?
    channel_type == 'moderator'
  end

  def user_count
    channel_users.count
  end

  def online_users
    channel_users.joins(:user).where(users: { online: true })
  end
end

# app/models/channel_user.rb
class ChannelUser < ApplicationRecord
  belongs_to :user
  belongs_to :channel

  validates :user_id, uniqueness: { scope: :channel_id }
  validates :joined_at, presence: true

  before_create :set_joined_at

  private

  def set_joined_at
    self.joined_at = Time.current
  end
end
```

### Channel Management Commands

```ruby
# app/websocket/commands/channel_commands.rb
module ChannelCommands
  def self.handle(connection, data)
    case data['command']
    when 'create_channel'
      create_channel(connection, data)
    when 'delete_channel'
      delete_channel(connection, data)
    when 'invite_user'
      invite_user(connection, data)
    when 'kick_user'
      kick_user(connection, data)
    when 'ban_user'
      ban_user(connection, data)
    when 'mute_user'
      mute_user(connection, data)
    else
      connection.send_error("Unknown command: #{data['command']}")
    end
  end

  private

  def self.create_channel(connection, data)
    return connection.send_error("Not authenticated") unless connection.authenticated?
    
    channel_name = data['name']
    channel_type = data['type'] || 'public'
    
    return connection.send_error("Channel name required") if channel_name.blank?
    return connection.send_error("Invalid channel type") unless %w[public private].include?(channel_type)

    user = User.find(connection.user_id)
    
    channel = Channel.create!(
      name: channel_name,
      channel_type: channel_type,
      created_by: user
    )

    connection.send_success('channel_created', {
      channel: channel.as_json(only: [:id, :name, :channel_type, :created_at])
    })
  end

  def self.delete_channel(connection, data)
    return connection.send_error("Not authenticated") unless connection.authenticated?
    
    channel_name = data['channel']
    return connection.send_error("Channel name required") if channel_name.blank?

    channel = Channel.find_by(name: channel_name)
    return connection.send_error("Channel not found") unless channel

    user = User.find(connection.user_id)
    return connection.send_error("Not authorized") unless channel.created_by == user || user.admin?

    channel.destroy!
    
    connection.send_success('channel_deleted', { channel: channel_name })
    
    # Notify all users in the channel
    broadcast_to_channel(channel_name, {
      type: 'channel_deleted',
      channel: channel_name,
      timestamp: Time.current.iso8601
    })
  end

  def self.invite_user(connection, data)
    return connection.send_error("Not authenticated") unless connection.authenticated?
    
    channel_name = data['channel']
    user_id = data['user_id']
    
    return connection.send_error("Channel name required") if channel_name.blank?
    return connection.send_error("User ID required") if user_id.blank?

    channel = Channel.find_by(name: channel_name)
    return connection.send_error("Channel not found") unless channel

    user = User.find(connection.user_id)
    target_user = User.find(user_id)
    
    return connection.send_error("Not authorized") unless ChannelAuthorizer.can_moderate?(user, channel_name)

    ChannelUser.create!(user: target_user, channel: channel)
    
    connection.send_success('user_invited', {
      channel: channel_name,
      user_id: user_id
    })
  end

  def self.kick_user(connection, data)
    return connection.send_error("Not authenticated") unless connection.authenticated?
    
    channel_name = data['channel']
    user_id = data['user_id']
    
    return connection.send_error("Channel name required") if channel_name.blank?
    return connection.send_error("User ID required") if user_id.blank?

    channel = Channel.find_by(name: channel_name)
    return connection.send_error("Channel not found") unless channel

    user = User.find(connection.user_id)
    target_user = User.find(user_id)
    
    return connection.send_error("Not authorized") unless ChannelAuthorizer.can_moderate?(user, channel_name)

    ChannelUser.find_by(user: target_user, channel: channel)&.destroy!
    
    connection.send_success('user_kicked', {
      channel: channel_name,
      user_id: user_id
    })
    
    # Notify the kicked user
    ConnectionPool.instance.publish("websocket:user:#{user_id}", {
      type: 'kicked_from_channel',
      channel: channel_name,
      timestamp: Time.current.iso8601
    })
  end

  def self.ban_user(connection, data)
    return connection.send_error("Not authenticated") unless connection.authenticated?
    
    channel_name = data['channel']
    user_id = data['user_id']
    reason = data['reason']
    
    return connection.send_error("Channel name required") if channel_name.blank?
    return connection.send_error("User ID required") if user_id.blank?

    channel = Channel.find_by(name: channel_name)
    return connection.send_error("Channel not found") unless channel

    user = User.find(connection.user_id)
    target_user = User.find(user_id)
    
    return connection.send_error("Not authorized") unless ChannelAuthorizer.can_moderate?(user, channel_name)

    ChannelBan.create!(
      user: target_user,
      channel: channel,
      banned_by: user,
      reason: reason
    )
    
    connection.send_success('user_banned', {
      channel: channel_name,
      user_id: user_id,
      reason: reason
    })
    
    # Notify the banned user
    ConnectionPool.instance.publish("websocket:user:#{user_id}", {
      type: 'banned_from_channel',
      channel: channel_name,
      reason: reason,
      timestamp: Time.current.iso8601
    })
  end

  def self.mute_user(connection, data)
    return connection.send_error("Not authenticated") unless connection.authenticated?
    
    channel_name = data['channel']
    user_id = data['user_id']
    duration = data['duration'] || 3600 # Default 1 hour
    
    return connection.send_error("Channel name required") if channel_name.blank?
    return connection.send_error("User ID required") if user_id.blank?

    channel = Channel.find_by(name: channel_name)
    return connection.send_error("Channel not found") unless channel

    user = User.find(connection.user_id)
    target_user = User.find(user_id)
    
    return connection.send_error("Not authorized") unless ChannelAuthorizer.can_moderate?(user, channel_name)

    ChannelMute.create!(
      user: target_user,
      channel: channel,
      muted_by: user,
      expires_at: duration.seconds.from_now
    )
    
    connection.send_success('user_muted', {
      channel: channel_name,
      user_id: user_id,
      duration: duration
    })
    
    # Notify the muted user
    ConnectionPool.instance.publish("websocket:user:#{user_id}", {
      type: 'muted_in_channel',
      channel: channel_name,
      duration: duration,
      timestamp: Time.current.iso8601
    })
  end

  def self.broadcast_to_channel(channel, message)
    ConnectionPool.instance.publish("websocket:channel:#{channel}", message)
  end
end
```

## Performance Optimization

### Connection Pooling

```ruby
# app/websocket/performance/connection_pool_manager.rb
class ConnectionPoolManager
  include Singleton

  def initialize
    @pools = {}
    @pool_config = Rails.application.config.websocket[:redis]
  end

  def get_pool(name)
    @pools[name] ||= create_pool(name)
  end

  def with_connection(pool_name, &block)
    pool = get_pool(pool_name)
    pool.with(&block)
  end

  private

  def create_pool(name)
    ConnectionPool.new(
      size: @pool_config[:pool_size],
      timeout: @pool_config[:pool_timeout]
    ) do
      Redis.new(url: @pool_config[:url])
    end
  end
end
```

### Message Queuing

```ruby
# app/websocket/performance/message_queue.rb
class MessageQueue
  include Singleton

  def initialize
    @queue = Queue.new
    @max_size = Rails.application.config.websocket[:performance][:message_queue_size]
    start_processor
  end

  def enqueue(message)
    return false if @queue.size >= @max_size
    
    @queue << message
    true
  end

  private

  def start_processor
    Thread.new do
      loop do
        begin
          message = @queue.pop
          process_message(message)
        rescue => e
          Rails.logger.error "Message queue error: #{e.message}"
        end
      end
    end
  end

  def process_message(message)
    case message[:type]
    when 'broadcast'
      broadcast_message(message)
    when 'direct'
      send_direct_message(message)
    when 'channel'
      send_channel_message(message)
    end
  end

  def broadcast_message(message)
    WebSocketServer.instance.connections.each do |connection_id, connection|
      connection.send_message(message[:data])
    end
  end

  def send_direct_message(message)
    user_id = message[:user_id]
    WebSocketServer.instance.connections.each do |connection_id, connection|
      if connection.user_id == user_id
        connection.send_message(message[:data])
        break
      end
    end
  end

  def send_channel_message(message)
    channel = message[:channel]
    WebSocketServer.instance.broadcast_to_channel(channel, message[:data])
  end
end
```

## Testing

### WebSocket Test Helper

```ruby
# spec/support/websocket_helper.rb
module WebSocketHelper
  def create_websocket_connection(user = nil)
    token = user ? JwtAuthenticator.generate_token(user) : nil
    
    connection = WebSocketConnection.new
    connection.post_init
    
    if token
      connection.receive_message({
        type: 'authenticate',
        token: token
      }.to_json)
    end
    
    connection
  end

  def send_websocket_message(connection, type, data = {})
    connection.receive_message({
      type: type,
      **data
    }.to_json)
  end

  def expect_websocket_message(connection, expected_type)
    expect(connection.sent_messages.last).to include('type' => expected_type)
  end

  def expect_websocket_error(connection, expected_message)
    expect(connection.sent_messages.last).to include(
      'type' => 'error',
      'message' => expected_message
    )
  end
end

RSpec.configure do |config|
  config.include WebSocketHelper, type: :websocket
end
```

### WebSocket Tests

```ruby
# spec/websocket/websocket_connection_spec.rb
RSpec.describe WebSocketConnection, type: :websocket do
  let(:user) { create(:user) }
  let(:connection) { create_websocket_connection }

  describe 'authentication' do
    it 'authenticates with valid token' do
      token = JwtAuthenticator.generate_token(user)
      
      send_websocket_message(connection, 'authenticate', { token: token })
      
      expect(connection.authenticated?).to be true
      expect(connection.user_id).to eq(user.id)
      expect_websocket_message(connection, 'success')
    end

    it 'rejects invalid token' do
      send_websocket_message(connection, 'authenticate', { token: 'invalid' })
      
      expect(connection.authenticated?).to be false
      expect_websocket_error(connection, 'Authentication failed')
    end
  end

  describe 'channel management' do
    before do
      token = JwtAuthenticator.generate_token(user)
      send_websocket_message(connection, 'authenticate', { token: token })
    end

    it 'joins a channel' do
      send_websocket_message(connection, 'join_channel', { channel: 'test-channel' })
      
      expect(connection.channels).to include('test-channel')
      expect_websocket_message(connection, 'success')
    end

    it 'leaves a channel' do
      connection.channels.add('test-channel')
      
      send_websocket_message(connection, 'leave_channel', { channel: 'test-channel' })
      
      expect(connection.channels).not_to include('test-channel')
      expect_websocket_message(connection, 'success')
    end
  end

  describe 'messaging' do
    before do
      token = JwtAuthenticator.generate_token(user)
      send_websocket_message(connection, 'authenticate', { token: token })
      send_websocket_message(connection, 'join_channel', { channel: 'test-channel' })
    end

    it 'sends a message to channel' do
      send_websocket_message(connection, 'send_message', {
        channel: 'test-channel',
        message: 'Hello, world!'
      })
      
      expect(Message.count).to eq(1)
      expect(Message.first.content).to eq('Hello, world!')
    end

    it 'rejects message when not in channel' do
      send_websocket_message(connection, 'send_message', {
        channel: 'other-channel',
        message: 'Hello, world!'
      })
      
      expect_websocket_error(connection, 'Not in channel')
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # WebSocket configuration
  config.websocket = {
    server: {
      host: ENV['WEBSOCKET_HOST'] || '0.0.0.0',
      port: ENV['WEBSOCKET_PORT'] || 8080,
      max_connections: ENV['WEBSOCKET_MAX_CONNECTIONS'] || 10000
    },
    authentication: {
      enabled: true,
      token_expiry: 3600,
      refresh_token_expiry: 86400
    },
    channels: {
      default_room: 'general',
      max_channels_per_user: 50,
      max_users_per_channel: 1000
    },
    performance: {
      heartbeat_interval: 30,
      connection_timeout: 300,
      message_queue_size: 1000
    },
    redis: {
      url: ENV['REDIS_URL'] || 'redis://localhost:6379/1',
      pool_size: ENV['REDIS_POOL_SIZE'] || 10,
      pool_timeout: ENV['REDIS_POOL_TIMEOUT'] || 5
    }
  }
end
```

### Systemd Service

```ini
# /etc/systemd/system/websocket.service
[Unit]
Description=WebSocket Server
After=network.target

[Service]
Type=simple
User=deploy
WorkingDirectory=/var/www/tsk/sdk
Environment=RAILS_ENV=production
ExecStart=/usr/bin/bundle exec ruby app/websocket/websocket_server.rb
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

### Nginx Configuration

```nginx
# nginx.conf
upstream websocket_backend {
  server 127.0.0.1:8080;
}

server {
  listen 80;
  server_name ws.example.com;

  location /websocket {
    proxy_pass http://websocket_backend;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    
    # WebSocket specific settings
    proxy_read_timeout 86400;
    proxy_send_timeout 86400;
    proxy_connect_timeout 86400;
  }
}
```

### Docker Configuration

```dockerfile
# Dockerfile.websocket
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    redis

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

EXPOSE 8080

CMD ["bundle", "exec", "ruby", "app/websocket/websocket_server.rb"]
```

```yaml
# docker-compose.websocket.yml
version: '3.8'

services:
  websocket:
    build:
      context: .
      dockerfile: Dockerfile.websocket
    ports:
      - "8080:8080"
    environment:
      - RAILS_ENV=production
      - REDIS_URL=redis://redis:6379/1
      - WEBSOCKET_HOST=0.0.0.0
      - WEBSOCKET_PORT=8080
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
      - POSTGRES_DB=websocket_app
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  redis_data:
  postgres_data:
```

This comprehensive WebSocket integration guide provides everything needed to build real-time communication features with TuskLang and Ruby, including connection management, authentication, channel-based messaging, performance optimization, testing, and deployment strategies. 
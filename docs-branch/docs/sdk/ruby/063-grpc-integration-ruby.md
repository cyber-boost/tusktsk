# gRPC Integration with TuskLang and Ruby

This guide covers integrating gRPC with TuskLang and Ruby applications for high-performance microservices communication.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Protocol Buffer Definition](#protocol-buffer-definition)
4. [Server Implementation](#server-implementation)
5. [Client Implementation](#client-implementation)
6. [Service Discovery](#service-discovery)
7. [Load Balancing](#load-balancing)
8. [Authentication](#authentication)
9. [Error Handling](#error-handling)
10. [Testing](#testing)
11. [Deployment](#deployment)

## Overview

gRPC is a high-performance RPC framework that enables efficient communication between microservices. This guide shows how to integrate gRPC with TuskLang and Ruby applications.

### Key Features

- **High-performance communication** with Protocol Buffers
- **Bidirectional streaming** support
- **Service discovery** and load balancing
- **Authentication and authorization**
- **Comprehensive error handling**
- **Production-ready deployment**

## Installation

### Dependencies

```ruby
# Gemfile
gem 'grpc'
gem 'grpc-tools'
gem 'google-protobuf'
gem 'redis'
gem 'connection_pool'
```

### TuskLang Configuration

```tusk
# config/grpc.tusk
grpc:
  server:
    host: "0.0.0.0"
    port: 50051
    max_concurrent_streams: 1000
    max_connection_idle: 300
    max_connection_age: 600
  
  client:
    timeout: 30
    retry_attempts: 3
    retry_delay: 1
    keepalive_time: 300
    keepalive_timeout: 20
  
  service_discovery:
    enabled: true
    registry_url: "redis://localhost:6379/2"
    service_ttl: 30
    health_check_interval: 10
  
  authentication:
    enabled: true
    token_expiry: 3600
    method: "jwt"
  
  monitoring:
    enabled: true
    metrics_port: 9090
    tracing_enabled: true
```

## Protocol Buffer Definition

### User Service

```protobuf
// proto/user_service.proto
syntax = "proto3";

package user;

service UserService {
  rpc GetUser(GetUserRequest) returns (GetUserResponse);
  rpc CreateUser(CreateUserRequest) returns (CreateUserResponse);
  rpc UpdateUser(UpdateUserRequest) returns (UpdateUserResponse);
  rpc DeleteUser(DeleteUserRequest) returns (DeleteUserResponse);
  rpc ListUsers(ListUsersRequest) returns (ListUsersResponse);
  rpc StreamUsers(StreamUsersRequest) returns (stream StreamUsersResponse);
}

message GetUserRequest {
  int32 user_id = 1;
}

message GetUserResponse {
  User user = 1;
  bool success = 2;
  string error_message = 3;
}

message CreateUserRequest {
  string email = 1;
  string name = 2;
  string password = 3;
}

message CreateUserResponse {
  User user = 1;
  bool success = 2;
  string error_message = 3;
}

message UpdateUserRequest {
  int32 user_id = 1;
  string email = 2;
  string name = 3;
}

message UpdateUserResponse {
  User user = 1;
  bool success = 2;
  string error_message = 3;
}

message DeleteUserRequest {
  int32 user_id = 1;
}

message DeleteUserResponse {
  bool success = 1;
  string error_message = 2;
}

message ListUsersRequest {
  int32 page = 1;
  int32 per_page = 2;
  string search = 3;
}

message ListUsersResponse {
  repeated User users = 1;
  int32 total_count = 2;
  int32 page = 3;
  int32 per_page = 4;
  bool success = 5;
  string error_message = 6;
}

message StreamUsersRequest {
  int32 batch_size = 1;
}

message StreamUsersResponse {
  repeated User users = 1;
  bool has_more = 2;
}

message User {
  int32 id = 1;
  string email = 2;
  string name = 3;
  string created_at = 4;
  string updated_at = 5;
}
```

### Post Service

```protobuf
// proto/post_service.proto
syntax = "proto3";

package post;

service PostService {
  rpc GetPost(GetPostRequest) returns (GetPostResponse);
  rpc CreatePost(CreatePostRequest) returns (CreatePostResponse);
  rpc UpdatePost(UpdatePostRequest) returns (UpdatePostResponse);
  rpc DeletePost(DeletePostRequest) returns (DeletePostResponse);
  rpc ListPosts(ListPostsRequest) returns (ListPostsResponse);
  rpc StreamPosts(StreamPostsRequest) returns (stream StreamPostsResponse);
}

message GetPostRequest {
  int32 post_id = 1;
}

message GetPostResponse {
  Post post = 1;
  bool success = 2;
  string error_message = 3;
}

message CreatePostRequest {
  int32 user_id = 1;
  string title = 2;
  string content = 3;
  string category = 4;
}

message CreatePostResponse {
  Post post = 1;
  bool success = 2;
  string error_message = 3;
}

message UpdatePostRequest {
  int32 post_id = 1;
  string title = 2;
  string content = 3;
  string category = 4;
}

message UpdatePostResponse {
  Post post = 1;
  bool success = 2;
  string error_message = 3;
}

message DeletePostRequest {
  int32 post_id = 1;
}

message DeletePostResponse {
  bool success = 1;
  string error_message = 2;
}

message ListPostsRequest {
  int32 user_id = 1;
  int32 page = 2;
  int32 per_page = 3;
  string category = 4;
}

message ListPostsResponse {
  repeated Post posts = 1;
  int32 total_count = 2;
  int32 page = 3;
  int32 per_page = 4;
  bool success = 5;
  string error_message = 6;
}

message StreamPostsRequest {
  int32 user_id = 1;
  int32 batch_size = 2;
}

message StreamPostsResponse {
  repeated Post posts = 1;
  bool has_more = 2;
}

message Post {
  int32 id = 1;
  int32 user_id = 2;
  string title = 3;
  string content = 4;
  string category = 5;
  string created_at = 6;
  string updated_at = 7;
}
```

## Server Implementation

### Base Server

```ruby
# app/grpc/base_server.rb
class BaseServer
  include GRPC::GenericService

  def self.inherited(subclass)
    subclass.class_eval do
      include GRPC::GenericService
    end
  end

  protected

  def authenticate_call(call)
    metadata = call.metadata
    token = metadata['authorization']&.gsub('Bearer ', '')
    
    return nil unless token
    
    begin
      decoded_token = JWT.decode(token, Rails.application.secrets.secret_key_base, true, algorithm: 'HS256')
      user_id = decoded_token[0]['user_id']
      User.find(user_id)
    rescue JWT::DecodeError, ActiveRecord::RecordNotFound => e
      raise GRPC::Unauthenticated.new("Invalid token")
    end
  end

  def handle_errors
    yield
  rescue ActiveRecord::RecordNotFound => e
    raise GRPC::NotFound.new("Record not found")
  rescue ActiveRecord::RecordInvalid => e
    raise GRPC::InvalidArgument.new(e.record.errors.full_messages.join(", "))
  rescue StandardError => e
    Rails.logger.error "gRPC Error: #{e.message}"
    raise GRPC::Internal.new("Internal server error")
  end

  def log_request(service, method, request)
    Rails.logger.info "gRPC Request: #{service}##{method} - #{request.inspect}"
  end

  def log_response(service, method, response)
    Rails.logger.info "gRPC Response: #{service}##{method} - #{response.inspect}"
  end
end
```

### User Service Server

```ruby
# app/grpc/user_service_server.rb
class UserServiceServer < BaseServer
  self.service_name = 'user.UserService'

  def get_user(request, call)
    log_request('UserService', 'get_user', request)
    
    handle_errors do
      user = User.find(request.user_id)
      response = User::GetUserResponse.new(
        user: user_to_proto(user),
        success: true
      )
      
      log_response('UserService', 'get_user', response)
      response
    end
  end

  def create_user(request, call)
    log_request('UserService', 'create_user', request)
    
    handle_errors do
      user = User.create!(
        email: request.email,
        name: request.name,
        password: request.password
      )
      
      response = User::CreateUserResponse.new(
        user: user_to_proto(user),
        success: true
      )
      
      log_response('UserService', 'create_user', response)
      response
    end
  end

  def update_user(request, call)
    log_request('UserService', 'update_user', request)
    
    handle_errors do
      user = User.find(request.user_id)
      user.update!(
        email: request.email,
        name: request.name
      )
      
      response = User::UpdateUserResponse.new(
        user: user_to_proto(user),
        success: true
      )
      
      log_response('UserService', 'update_user', response)
      response
    end
  end

  def delete_user(request, call)
    log_request('UserService', 'delete_user', request)
    
    handle_errors do
      user = User.find(request.user_id)
      user.destroy!
      
      response = User::DeleteUserResponse.new(success: true)
      
      log_response('UserService', 'delete_user', response)
      response
    end
  end

  def list_users(request, call)
    log_request('UserService', 'list_users', request)
    
    handle_errors do
      users = User.all
      users = users.where("name ILIKE ? OR email ILIKE ?", 
                         "%#{request.search}%", "%#{request.search}%") if request.search.present?
      
      total_count = users.count
      users = users.offset((request.page - 1) * request.per_page)
                   .limit(request.per_page)
      
      response = User::ListUsersResponse.new(
        users: users.map { |user| user_to_proto(user) },
        total_count: total_count,
        page: request.page,
        per_page: request.per_page,
        success: true
      )
      
      log_response('UserService', 'list_users', response)
      response
    end
  end

  def stream_users(request, call)
    log_request('UserService', 'stream_users', request)
    
    handle_errors do
      users = User.all
      batch_size = request.batch_size || 100
      
      users.find_in_batches(batch_size: batch_size) do |batch|
        response = User::StreamUsersResponse.new(
          users: batch.map { |user| user_to_proto(user) },
          has_more: batch.size == batch_size
        )
        
        call.send_msg(response)
      end
    end
  end

  private

  def user_to_proto(user)
    User::User.new(
      id: user.id,
      email: user.email,
      name: user.name,
      created_at: user.created_at.iso8601,
      updated_at: user.updated_at.iso8601
    )
  end
end
```

### Post Service Server

```ruby
# app/grpc/post_service_server.rb
class PostServiceServer < BaseServer
  self.service_name = 'post.PostService'

  def get_post(request, call)
    log_request('PostService', 'get_post', request)
    
    handle_errors do
      post = Post.find(request.post_id)
      response = Post::GetPostResponse.new(
        post: post_to_proto(post),
        success: true
      )
      
      log_response('PostService', 'get_post', response)
      response
    end
  end

  def create_post(request, call)
    log_request('PostService', 'create_post', request)
    
    handle_errors do
      post = Post.create!(
        user_id: request.user_id,
        title: request.title,
        content: request.content,
        category: request.category
      )
      
      response = Post::CreatePostResponse.new(
        post: post_to_proto(post),
        success: true
      )
      
      log_response('PostService', 'create_post', response)
      response
    end
  end

  def update_post(request, call)
    log_request('PostService', 'update_post', request)
    
    handle_errors do
      post = Post.find(request.post_id)
      post.update!(
        title: request.title,
        content: request.content,
        category: request.category
      )
      
      response = Post::UpdatePostResponse.new(
        post: post_to_proto(post),
        success: true
      )
      
      log_response('PostService', 'update_post', response)
      response
    end
  end

  def delete_post(request, call)
    log_request('PostService', 'delete_post', request)
    
    handle_errors do
      post = Post.find(request.post_id)
      post.destroy!
      
      response = Post::DeletePostResponse.new(success: true)
      
      log_response('PostService', 'delete_post', response)
      response
    end
  end

  def list_posts(request, call)
    log_request('PostService', 'list_posts', request)
    
    handle_errors do
      posts = Post.all
      posts = posts.where(user_id: request.user_id) if request.user_id > 0
      posts = posts.where(category: request.category) if request.category.present?
      
      total_count = posts.count
      posts = posts.offset((request.page - 1) * request.per_page)
                   .limit(request.per_page)
      
      response = Post::ListPostsResponse.new(
        posts: posts.map { |post| post_to_proto(post) },
        total_count: total_count,
        page: request.page,
        per_page: request.per_page,
        success: true
      )
      
      log_response('PostService', 'list_posts', response)
      response
    end
  end

  def stream_posts(request, call)
    log_request('PostService', 'stream_posts', request)
    
    handle_errors do
      posts = Post.all
      posts = posts.where(user_id: request.user_id) if request.user_id > 0
      batch_size = request.batch_size || 100
      
      posts.find_in_batches(batch_size: batch_size) do |batch|
        response = Post::StreamPostsResponse.new(
          posts: batch.map { |post| post_to_proto(post) },
          has_more: batch.size == batch_size
        )
        
        call.send_msg(response)
      end
    end
  end

  private

  def post_to_proto(post)
    Post::Post.new(
      id: post.id,
      user_id: post.user_id,
      title: post.title,
      content: post.content,
      category: post.category,
      created_at: post.created_at.iso8601,
      updated_at: post.updated_at.iso8601
    )
  end
end
```

## Client Implementation

### Base Client

```ruby
# app/grpc/base_client.rb
class BaseClient
  def initialize(service_url, timeout: 30)
    @service_url = service_url
    @timeout = timeout
    @stub = nil
  end

  protected

  def stub
    @stub ||= create_stub
  end

  def create_stub
    # Override in subclasses
    raise NotImplementedError
  end

  def call_with_retry(method, request, max_retries: 3)
    retries = 0
    
    begin
      stub.send(method, request, deadline: Time.now + @timeout)
    rescue GRPC::Unavailable, GRPC::DeadlineExceeded => e
      retries += 1
      if retries <= max_retries
        sleep(2 ** retries) # Exponential backoff
        retry
      else
        raise e
      end
    end
  end

  def call_with_auth(method, request, token: nil)
    metadata = {}
    metadata['authorization'] = "Bearer #{token}" if token
    
    stub.send(method, request, deadline: Time.now + @timeout, metadata: metadata)
  end

  def log_request(service, method, request)
    Rails.logger.info "gRPC Client Request: #{service}##{method} - #{request.inspect}"
  end

  def log_response(service, method, response)
    Rails.logger.info "gRPC Client Response: #{service}##{method} - #{response.inspect}"
  end
end
```

### User Service Client

```ruby
# app/grpc/user_service_client.rb
class UserServiceClient < BaseClient
  def initialize(service_url = nil)
    super(service_url || Rails.application.config.grpc[:user_service_url])
  end

  def get_user(user_id, token: nil)
    request = User::GetUserRequest.new(user_id: user_id)
    log_request('UserService', 'get_user', request)
    
    response = call_with_auth(:get_user, request, token: token)
    log_response('UserService', 'get_user', response)
    
    response
  end

  def create_user(email:, name:, password:, token: nil)
    request = User::CreateUserRequest.new(
      email: email,
      name: name,
      password: password
    )
    log_request('UserService', 'create_user', request)
    
    response = call_with_auth(:create_user, request, token: token)
    log_response('UserService', 'create_user', response)
    
    response
  end

  def update_user(user_id:, email: nil, name: nil, token: nil)
    request = User::UpdateUserRequest.new(
      user_id: user_id,
      email: email,
      name: name
    )
    log_request('UserService', 'update_user', request)
    
    response = call_with_auth(:update_user, request, token: token)
    log_response('UserService', 'update_user', response)
    
    response
  end

  def delete_user(user_id, token: nil)
    request = User::DeleteUserRequest.new(user_id: user_id)
    log_request('UserService', 'delete_user', request)
    
    response = call_with_auth(:delete_user, request, token: token)
    log_response('UserService', 'delete_user', response)
    
    response
  end

  def list_users(page: 1, per_page: 20, search: nil, token: nil)
    request = User::ListUsersRequest.new(
      page: page,
      per_page: per_page,
      search: search
    )
    log_request('UserService', 'list_users', request)
    
    response = call_with_auth(:list_users, request, token: token)
    log_response('UserService', 'list_users', response)
    
    response
  end

  def stream_users(batch_size: 100, token: nil)
    request = User::StreamUsersRequest.new(batch_size: batch_size)
    log_request('UserService', 'stream_users', request)
    
    call_with_auth(:stream_users, request, token: token) do |response|
      yield response
    end
  end

  private

  def create_stub
    User::UserService::Stub.new(@service_url, :this_channel_is_insecure)
  end
end
```

### Post Service Client

```ruby
# app/grpc/post_service_client.rb
class PostServiceClient < BaseClient
  def initialize(service_url = nil)
    super(service_url || Rails.application.config.grpc[:post_service_url])
  end

  def get_post(post_id, token: nil)
    request = Post::GetPostRequest.new(post_id: post_id)
    log_request('PostService', 'get_post', request)
    
    response = call_with_auth(:get_post, request, token: token)
    log_response('PostService', 'get_post', response)
    
    response
  end

  def create_post(user_id:, title:, content:, category:, token: nil)
    request = Post::CreatePostRequest.new(
      user_id: user_id,
      title: title,
      content: content,
      category: category
    )
    log_request('PostService', 'create_post', request)
    
    response = call_with_auth(:create_post, request, token: token)
    log_response('PostService', 'create_post', response)
    
    response
  end

  def update_post(post_id:, title: nil, content: nil, category: nil, token: nil)
    request = Post::UpdatePostRequest.new(
      post_id: post_id,
      title: title,
      content: content,
      category: category
    )
    log_request('PostService', 'update_post', request)
    
    response = call_with_auth(:update_post, request, token: token)
    log_response('PostService', 'update_post', response)
    
    response
  end

  def delete_post(post_id, token: nil)
    request = Post::DeletePostRequest.new(post_id: post_id)
    log_request('PostService', 'delete_post', request)
    
    response = call_with_auth(:delete_post, request, token: token)
    log_response('PostService', 'delete_post', response)
    
    response
  end

  def list_posts(user_id: nil, page: 1, per_page: 20, category: nil, token: nil)
    request = Post::ListPostsRequest.new(
      user_id: user_id || 0,
      page: page,
      per_page: per_page,
      category: category
    )
    log_request('PostService', 'list_posts', request)
    
    response = call_with_auth(:list_posts, request, token: token)
    log_response('PostService', 'list_posts', response)
    
    response
  end

  def stream_posts(user_id: nil, batch_size: 100, token: nil)
    request = Post::StreamPostsRequest.new(
      user_id: user_id || 0,
      batch_size: batch_size
    )
    log_request('PostService', 'stream_posts', request)
    
    call_with_auth(:stream_posts, request, token: token) do |response|
      yield response
    end
  end

  private

  def create_stub
    Post::PostService::Stub.new(@service_url, :this_channel_is_insecure)
  end
end
```

## Service Discovery

### Service Registry

```ruby
# app/grpc/service_registry.rb
class ServiceRegistry
  include Singleton

  def initialize
    @redis = Redis.new(url: Rails.application.config.grpc[:service_discovery][:registry_url])
    @services = {}
    @health_check_thread = nil
    start_health_check
  end

  def register_service(service_name, host, port, metadata = {})
    service_key = "grpc:service:#{service_name}"
    service_data = {
      host: host,
      port: port,
      metadata: metadata,
      registered_at: Time.current.iso8601,
      last_heartbeat: Time.current.iso8601
    }
    
    @redis.hset(service_key, "#{host}:#{port}", service_data.to_json)
    @redis.expire(service_key, Rails.application.config.grpc[:service_discovery][:service_ttl])
    
    Rails.logger.info "Registered gRPC service: #{service_name} at #{host}:#{port}"
  end

  def unregister_service(service_name, host, port)
    service_key = "grpc:service:#{service_name}"
    @redis.hdel(service_key, "#{host}:#{port}")
    
    Rails.logger.info "Unregistered gRPC service: #{service_name} at #{host}:#{port}"
  end

  def get_service_instances(service_name)
    service_key = "grpc:service:#{service_name}"
    instances = []
    
    @redis.hgetall(service_key).each do |address, data|
      service_data = JSON.parse(data)
      instances << {
        host: service_data['host'],
        port: service_data['port'],
        metadata: service_data['metadata'],
        last_heartbeat: Time.parse(service_data['last_heartbeat'])
      }
    end
    
    instances.select { |instance| instance[:last_heartbeat] > Rails.application.config.grpc[:service_discovery][:service_ttl].seconds.ago }
  end

  def heartbeat(service_name, host, port)
    service_key = "grpc:service:#{service_name}"
    address = "#{host}:#{port}"
    
    if @redis.hexists(service_key, address)
      service_data = JSON.parse(@redis.hget(service_key, address))
      service_data['last_heartbeat'] = Time.current.iso8601
      @redis.hset(service_key, address, service_data.to_json)
    end
  end

  private

  def start_health_check
    @health_check_thread = Thread.new do
      loop do
        check_services_health
        sleep Rails.application.config.grpc[:service_discovery][:health_check_interval]
      end
    end
  end

  def check_services_health
    @redis.keys("grpc:service:*").each do |service_key|
      service_name = service_key.gsub("grpc:service:", "")
      
      @redis.hgetall(service_key).each do |address, data|
        service_data = JSON.parse(data)
        last_heartbeat = Time.parse(service_data['last_heartbeat'])
        
        if last_heartbeat < Rails.application.config.grpc[:service_discovery][:service_ttl].seconds.ago
          @redis.hdel(service_key, address)
          Rails.logger.warn "Removed stale gRPC service: #{service_name} at #{address}"
        end
      end
    end
  end
end
```

## Load Balancing

### Load Balancer

```ruby
# app/grpc/load_balancer.rb
class LoadBalancer
  include Singleton

  def initialize
    @registry = ServiceRegistry.instance
  end

  def get_service_instance(service_name, strategy: :round_robin)
    instances = @registry.get_service_instances(service_name)
    return nil if instances.empty?
    
    case strategy
    when :round_robin
      round_robin_select(service_name, instances)
    when :least_connections
      least_connections_select(instances)
    when :random
      random_select(instances)
    else
      round_robin_select(service_name, instances)
    end
  end

  private

  def round_robin_select(service_name, instances)
    key = "grpc:round_robin:#{service_name}"
    current_index = Redis.current.incr(key) - 1
    Redis.current.expire(key, 60)
    
    instances[current_index % instances.size]
  end

  def least_connections_select(instances)
    instances.min_by { |instance| get_connection_count(instance) }
  end

  def random_select(instances)
    instances.sample
  end

  def get_connection_count(instance)
    # This would typically query a metrics service
    # For now, return a random number for demonstration
    rand(1..10)
  end
end
```

## Authentication

### JWT Authentication

```ruby
# app/grpc/authentication/jwt_authenticator.rb
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
      exp: Rails.application.config.grpc[:authentication][:token_expiry].seconds.from_now.to_i
    }
    
    JWT.encode(payload, Rails.application.secrets.secret_key_base, 'HS256')
  end

  def self.refresh_token(user)
    payload = {
      user_id: user.id,
      email: user.email,
      exp: Rails.application.config.grpc[:authentication][:refresh_token_expiry].seconds.from_now.to_i
    }
    
    JWT.encode(payload, Rails.application.secrets.secret_key_base, 'HS256')
  end
end
```

## Error Handling

### Error Handler

```ruby
# app/grpc/error_handler.rb
class ErrorHandler
  def self.handle_error(error, context = {})
    case error
    when ActiveRecord::RecordNotFound
      GRPC::NotFound.new("Record not found")
    when ActiveRecord::RecordInvalid
      GRPC::InvalidArgument.new(error.record.errors.full_messages.join(", "))
    when JWT::DecodeError
      GRPC::Unauthenticated.new("Invalid token")
    when ActiveRecord::RecordNotUnique
      GRPC::AlreadyExists.new("Record already exists")
    when StandardError
      Rails.logger.error "gRPC Error: #{error.message} - #{context}"
      GRPC::Internal.new("Internal server error")
    else
      GRPC::Unknown.new("Unknown error")
    end
  end

  def self.log_error(error, context = {})
    Rails.logger.error "gRPC Error: #{error.class} - #{error.message}"
    Rails.logger.error "Context: #{context}"
    Rails.logger.error "Backtrace: #{error.backtrace.first(5).join("\n")}"
  end
end
```

## Testing

### gRPC Test Helper

```ruby
# spec/support/grpc_helper.rb
module GrpcHelper
  def create_grpc_server(service_class)
    server = GRPC::RpcServer.new
    server.add_http2_port('0.0.0.0:0', :this_port_is_insecure)
    server.handle(service_class.new)
    
    Thread.new { server.run }
    server.wait_till_running
    
    server
  end

  def create_grpc_client(service_class, server)
    port = server.add_http2_port('0.0.0.0:0', :this_port_is_insecure)
    service_class::Stub.new("localhost:#{port}", :this_channel_is_insecure)
  end

  def generate_auth_token(user)
    JwtAuthenticator.generate_token(user)
  end
end

RSpec.configure do |config|
  config.include GrpcHelper, type: :grpc
end
```

### gRPC Tests

```ruby
# spec/grpc/user_service_spec.rb
RSpec.describe UserServiceServer, type: :grpc do
  let(:server) { create_grpc_server(UserServiceServer) }
  let(:client) { create_grpc_client(User::UserService, server) }
  let(:user) { create(:user) }
  let(:token) { generate_auth_token(user) }

  after do
    server.stop
  end

  describe '#get_user' do
    it 'returns user when found' do
      request = User::GetUserRequest.new(user_id: user.id)
      response = client.get_user(request)
      
      expect(response.success).to be true
      expect(response.user.id).to eq(user.id)
      expect(response.user.email).to eq(user.email)
    end

    it 'returns error when user not found' do
      request = User::GetUserRequest.new(user_id: 999999)
      
      expect {
        client.get_user(request)
      }.to raise_error(GRPC::NotFound)
    end
  end

  describe '#create_user' do
    it 'creates a new user' do
      request = User::CreateUserRequest.new(
        email: 'test@example.com',
        name: 'Test User',
        password: 'password123'
      )
      
      response = client.create_user(request)
      
      expect(response.success).to be true
      expect(response.user.email).to eq('test@example.com')
      expect(response.user.name).to eq('Test User')
    end

    it 'returns error for invalid data' do
      request = User::CreateUserRequest.new(
        email: '',
        name: '',
        password: ''
      )
      
      expect {
        client.create_user(request)
      }.to raise_error(GRPC::InvalidArgument)
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # gRPC configuration
  config.grpc = {
    server: {
      host: ENV['GRPC_HOST'] || '0.0.0.0',
      port: ENV['GRPC_PORT'] || 50051,
      max_concurrent_streams: ENV['GRPC_MAX_CONCURRENT_STREAMS'] || 1000,
      max_connection_idle: ENV['GRPC_MAX_CONNECTION_IDLE'] || 300,
      max_connection_age: ENV['GRPC_MAX_CONNECTION_AGE'] || 600
    },
    client: {
      timeout: ENV['GRPC_CLIENT_TIMEOUT'] || 30,
      retry_attempts: ENV['GRPC_RETRY_ATTEMPTS'] || 3,
      retry_delay: ENV['GRPC_RETRY_DELAY'] || 1,
      keepalive_time: ENV['GRPC_KEEPALIVE_TIME'] || 300,
      keepalive_timeout: ENV['GRPC_KEEPALIVE_TIMEOUT'] || 20
    },
    service_discovery: {
      enabled: true,
      registry_url: ENV['GRPC_REGISTRY_URL'] || 'redis://localhost:6379/2',
      service_ttl: ENV['GRPC_SERVICE_TTL'] || 30,
      health_check_interval: ENV['GRPC_HEALTH_CHECK_INTERVAL'] || 10
    },
    authentication: {
      enabled: true,
      token_expiry: ENV['GRPC_TOKEN_EXPIRY'] || 3600,
      method: 'jwt'
    },
    monitoring: {
      enabled: true,
      metrics_port: ENV['GRPC_METRICS_PORT'] || 9090,
      tracing_enabled: ENV['GRPC_TRACING_ENABLED'] == 'true'
    }
  }
end
```

### Systemd Service

```ini
# /etc/systemd/system/grpc-server.service
[Unit]
Description=gRPC Server
After=network.target

[Service]
Type=simple
User=deploy
WorkingDirectory=/var/www/tsk/sdk
Environment=RAILS_ENV=production
ExecStart=/usr/bin/bundle exec ruby app/grpc/grpc_server.rb
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

### Docker Configuration

```dockerfile
# Dockerfile.grpc
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    protobuf \
    grpc

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

EXPOSE 50051

CMD ["bundle", "exec", "ruby", "app/grpc/grpc_server.rb"]
```

```yaml
# docker-compose.grpc.yml
version: '3.8'

services:
  grpc-server:
    build:
      context: .
      dockerfile: Dockerfile.grpc
    ports:
      - "50051:50051"
    environment:
      - RAILS_ENV=production
      - GRPC_HOST=0.0.0.0
      - GRPC_PORT=50051
      - REDIS_URL=redis://redis:6379/1
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
      - POSTGRES_DB=grpc_app
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  redis_data:
  postgres_data:
```

This comprehensive gRPC integration guide provides everything needed to build high-performance microservices with TuskLang and Ruby, including Protocol Buffer definitions, server and client implementations, service discovery, load balancing, authentication, error handling, testing, and deployment strategies. 
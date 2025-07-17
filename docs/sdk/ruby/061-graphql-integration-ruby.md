# GraphQL Integration with TuskLang and Ruby

This guide covers integrating GraphQL with TuskLang and Ruby applications, including schema definition, resolvers, mutations, subscriptions, and advanced patterns.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Setup](#basic-setup)
4. [Schema Definition](#schema-definition)
5. [Resolvers](#resolvers)
6. [Mutations](#mutations)
7. [Subscriptions](#subscriptions)
8. [Advanced Patterns](#advanced-patterns)
9. [Performance Optimization](#performance-optimization)
10. [Security](#security)
11. [Testing](#testing)
12. [Deployment](#deployment)

## Overview

GraphQL provides a powerful query language for APIs that allows clients to request exactly the data they need. This guide shows how to integrate GraphQL with TuskLang and Ruby applications.

### Key Features

- **Schema-first development** with TuskLang configuration
- **Type-safe resolvers** with Ruby
- **Real-time subscriptions** with WebSocket support
- **Performance optimization** with batching and caching
- **Security** with authentication and authorization
- **Testing** with comprehensive test suites

## Installation

### Dependencies

```ruby
# Gemfile
gem 'graphql'
gem 'graphql-batch'
gem 'graphql-persisted_queries'
gem 'graphql-tracers'
gem 'redis'
gem 'connection_pool'
```

### TuskLang Configuration

```tusk
# config/graphql.tusk
graphql:
  schema:
    path: "app/graphql/schema.graphql"
    introspection: true
    mutation: true
    subscription: true
  
  resolvers:
    batch_size: 100
    timeout: 30
    cache_enabled: true
    cache_ttl: 300
  
  subscriptions:
    transport: "websocket"
    redis_url: "redis://localhost:6379/1"
    channel_prefix: "graphql:"
  
  security:
    max_query_depth: 10
    max_query_complexity: 1000
    rate_limit:
      enabled: true
      requests_per_minute: 1000
  
  performance:
    query_analysis: true
    field_instrumentation: true
    query_complexity_analysis: true
```

## Basic Setup

### GraphQL Schema

```ruby
# app/graphql/types/base_object.rb
module Types
  class BaseObject < GraphQL::Schema::Object
    field_class Types::BaseField
  end
end

# app/graphql/types/base_field.rb
module Types
  class BaseField < GraphQL::Schema::Field
    def initialize(*args, **kwargs, &block)
      super
      # Add custom field logic here
    end
  end
end

# app/graphql/types/base_input_object.rb
module Types
  class BaseInputObject < GraphQL::Schema::InputObject
    argument_class Types::BaseArgument
  end
end

# app/graphql/types/base_argument.rb
module Types
  class BaseArgument < GraphQL::Schema::Argument
  end
end
```

### Schema Definition

```ruby
# app/graphql/schema.rb
class Schema < GraphQL::Schema
  query Types::QueryType
  mutation Types::MutationType
  subscription Types::SubscriptionType

  # Use batch loading
  use GraphQL::Batch

  # Use custom instrumentation
  instrument :field, GraphQL::Models::Instrumentation.new

  # Add custom middleware
  middleware GraphQL::Schema::Middleware::Authorization.new

  # Configure error handling
  rescue_from(ActiveRecord::RecordNotFound) do |err, obj, args, ctx, field|
    raise GraphQL::ExecutionError.new("Record not found")
  end

  rescue_from(ActiveRecord::RecordInvalid) do |err, obj, args, ctx, field|
    raise GraphQL::ExecutionError.new(err.record.errors.full_messages.join(", "))
  end
end
```

## Schema Definition

### Query Type

```ruby
# app/graphql/types/query_type.rb
module Types
  class QueryType < Types::BaseObject
    field :users, [Types::UserType], null: false do
      argument :limit, Integer, required: false, default_value: 10
      argument :offset, Integer, required: false, default_value: 0
      argument :search, String, required: false
    end

    field :user, Types::UserType, null: true do
      argument :id, ID, required: true
    end

    field :posts, [Types::PostType], null: false do
      argument :user_id, ID, required: false
      argument :category, String, required: false
      argument :limit, Integer, required: false, default_value: 20
    end

    def users(limit:, offset:, search:)
      users = User.all
      users = users.where("name ILIKE ?", "%#{search}%") if search.present?
      users.limit(limit).offset(offset)
    end

    def user(id:)
      User.find(id)
    end

    def posts(user_id: nil, category: nil, limit:)
      posts = Post.all
      posts = posts.where(user_id: user_id) if user_id.present?
      posts = posts.where(category: category) if category.present?
      posts.limit(limit)
    end
  end
end
```

### User Type

```ruby
# app/graphql/types/user_type.rb
module Types
  class UserType < Types::BaseObject
    field :id, ID, null: false
    field :email, String, null: false
    field :name, String, null: false
    field :avatar_url, String, null: true
    field :created_at, GraphQL::Types::ISO8601DateTime, null: false
    field :updated_at, GraphQL::Types::ISO8601DateTime, null: false
    
    field :posts, [Types::PostType], null: false do
      argument :limit, Integer, required: false, default_value: 10
    end
    
    field :followers_count, Integer, null: false
    field :following_count, Integer, null: false
    field :is_following, Boolean, null: false do
      argument :user_id, ID, required: true
    end

    def posts(limit:)
      object.posts.limit(limit)
    end

    def followers_count
      object.followers.count
    end

    def following_count
      object.following.count
    end

    def is_following(user_id:)
      object.following.exists?(user_id)
    end
  end
end
```

### Post Type

```ruby
# app/graphql/types/post_type.rb
module Types
  class PostType < Types::BaseObject
    field :id, ID, null: false
    field :title, String, null: false
    field :content, String, null: false
    field :category, String, null: false
    field :published_at, GraphQL::Types::ISO8601DateTime, null: true
    field :created_at, GraphQL::Types::ISO8601DateTime, null: false
    field :updated_at, GraphQL::Types::ISO8601DateTime, null: false
    
    field :author, Types::UserType, null: false
    field :comments, [Types::CommentType], null: false do
      argument :limit, Integer, required: false, default_value: 10
    end
    field :likes_count, Integer, null: false
    field :is_liked, Boolean, null: false

    def author
      object.user
    end

    def comments(limit:)
      object.comments.limit(limit)
    end

    def likes_count
      object.likes.count
    end

    def is_liked
      return false unless context[:current_user]
      object.likes.exists?(user: context[:current_user])
    end
  end
end
```

## Resolvers

### Batch Loading

```ruby
# app/graphql/loaders/user_loader.rb
class UserLoader < GraphQL::Batch::Loader
  def initialize(model)
    @model = model
  end

  def perform(ids)
    @model.where(id: ids).each { |user| fulfill(user.id, user) }
    ids.each { |id| fulfill(id, nil) unless fulfilled?(id) }
  end
end

# app/graphql/loaders/association_loader.rb
class AssociationLoader < GraphQL::Batch::Loader
  def initialize(model, association_name)
    @model = model
    @association_name = association_name
  end

  def perform(records)
    preloader = ActiveRecord::Associations::Preloader.new
    preloader.preload(records, @association_name)
    records.each { |record| fulfill(record, record.public_send(@association_name)) }
  end
end
```

### Custom Resolvers

```ruby
# app/graphql/resolvers/base_resolver.rb
module Resolvers
  class BaseResolver < GraphQL::Schema::Resolver
    def self.authorized?(object, context)
      super && context[:current_user].present?
    end
  end
end

# app/graphql/resolvers/users_resolver.rb
module Resolvers
  class UsersResolver < BaseResolver
    type [Types::UserType], null: false

    argument :limit, Integer, required: false, default_value: 10
    argument :offset, Integer, required: false, default_value: 0
    argument :search, String, required: false
    argument :sort_by, String, required: false, default_value: "created_at"
    argument :sort_order, String, required: false, default_value: "desc"

    def resolve(limit:, offset:, search:, sort_by:, sort_order:)
      users = User.all
      
      if search.present?
        users = users.where("name ILIKE ? OR email ILIKE ?", 
                           "%#{search}%", "%#{search}%")
      end
      
      users = users.order(sort_by => sort_order)
      users.limit(limit).offset(offset)
    end
  end
end
```

## Mutations

### Base Mutation

```ruby
# app/graphql/mutations/base_mutation.rb
module Mutations
  class BaseMutation < GraphQL::Schema::Mutation
    def self.authorized?(object, context)
      super && context[:current_user].present?
    end

    def current_user
      context[:current_user]
    end

    def authorize_user!(user)
      return if current_user == user || current_user.admin?
      raise GraphQL::ExecutionError.new("Not authorized")
    end
  end
end
```

### Create User Mutation

```ruby
# app/graphql/mutations/create_user.rb
module Mutations
  class CreateUser < BaseMutation
    argument :email, String, required: true
    argument :name, String, required: true
    argument :password, String, required: true
    argument :password_confirmation, String, required: true

    field :user, Types::UserType, null: true
    field :errors, [String], null: false

    def resolve(email:, name:, password:, password_confirmation:)
      user = User.new(
        email: email,
        name: name,
        password: password,
        password_confirmation: password_confirmation
      )

      if user.save
        {
          user: user,
          errors: []
        }
      else
        {
          user: nil,
          errors: user.errors.full_messages
        }
      end
    end
  end
end
```

### Update User Mutation

```ruby
# app/graphql/mutations/update_user.rb
module Mutations
  class UpdateUser < BaseMutation
    argument :id, ID, required: true
    argument :name, String, required: false
    argument :email, String, required: false
    argument :avatar_url, String, required: false

    field :user, Types::UserType, null: true
    field :errors, [String], null: false

    def resolve(id:, **attributes)
      user = User.find(id)
      authorize_user!(user)

      if user.update(attributes)
        {
          user: user,
          errors: []
        }
      else
        {
          user: nil,
          errors: user.errors.full_messages
        }
      end
    end
  end
end
```

### Create Post Mutation

```ruby
# app/graphql/mutations/create_post.rb
module Mutations
  class CreatePost < BaseMutation
    argument :title, String, required: true
    argument :content, String, required: true
    argument :category, String, required: true
    argument :published, Boolean, required: false, default_value: false

    field :post, Types::PostType, null: true
    field :errors, [String], null: false

    def resolve(title:, content:, category:, published:)
      post = current_user.posts.build(
        title: title,
        content: content,
        category: category,
        published_at: published ? Time.current : nil
      )

      if post.save
        {
          post: post,
          errors: []
        }
      else
        {
          post: nil,
          errors: post.errors.full_messages
        }
      end
    end
  end
end
```

## Subscriptions

### Subscription Type

```ruby
# app/graphql/types/subscription_type.rb
module Types
  class SubscriptionType < Types::BaseObject
    field :post_created, Types::PostType, null: false do
      argument :user_id, ID, required: false
    end

    field :comment_added, Types::CommentType, null: false do
      argument :post_id, ID, required: false
    end

    field :user_updated, Types::UserType, null: false do
      argument :user_id, ID, required: true
    end

    def post_created(user_id: nil)
      if user_id.present?
        Post.where(user_id: user_id)
      else
        Post.all
      end
    end

    def comment_added(post_id: nil)
      if post_id.present?
        Comment.where(post_id: post_id)
      else
        Comment.all
      end
    end

    def user_updated(user_id:)
      User.where(id: user_id)
    end
  end
end
```

### Subscription Triggers

```ruby
# app/models/post.rb
class Post < ApplicationRecord
  belongs_to :user
  has_many :comments, dependent: :destroy
  has_many :likes, dependent: :destroy

  after_create :broadcast_post_created
  after_update :broadcast_post_updated

  private

  def broadcast_post_created
    GraphQL::Subscriptions.trigger(
      "post_created",
      { user_id: user_id },
      self
    )
  end

  def broadcast_post_updated
    GraphQL::Subscriptions.trigger(
      "post_updated",
      { user_id: user_id },
      self
    )
  end
end

# app/models/comment.rb
class Comment < ApplicationRecord
  belongs_to :user
  belongs_to :post

  after_create :broadcast_comment_added

  private

  def broadcast_comment_added
    GraphQL::Subscriptions.trigger(
      "comment_added",
      { post_id: post_id },
      self
    )
  end
end
```

## Advanced Patterns

### Field Instrumentation

```ruby
# app/graphql/instrumentation/field_instrumentation.rb
class FieldInstrumentation
  def instrument(type, field)
    old_resolve_proc = field.resolve_proc
    field.redefine do
      resolve ->(obj, args, ctx) do
        start_time = Time.current
        result = old_resolve_proc.call(obj, args, ctx)
        duration = Time.current - start_time
        
        Rails.logger.info(
          "GraphQL Field: #{type.name}.#{field.name} " \
          "took #{duration * 1000}ms"
        )
        
        result
      end
    end
  end
end
```

### Query Complexity Analysis

```ruby
# app/graphql/analysis/query_complexity_analyzer.rb
class QueryComplexityAnalyzer < GraphQL::Analysis::AST::Analyzer
  def on_operation_multiplex(operation_multiplex)
    operation_multiplex.operations.each do |operation|
      complexity = calculate_complexity(operation)
      
      if complexity > max_complexity
        raise GraphQL::AnalysisError.new(
          "Query complexity #{complexity} exceeds maximum #{max_complexity}"
        )
      end
    end
  end

  private

  def calculate_complexity(operation)
    # Implement complexity calculation logic
    operation.selections.sum { |selection| field_complexity(selection) }
  end

  def field_complexity(selection)
    case selection
    when GraphQL::Language::Nodes::Field
      base_complexity = 1
      base_complexity += selection.arguments.size
      base_complexity += selection.selections&.size.to_i * 2
      base_complexity
    else
      0
    end
  end

  def max_complexity
    context[:max_complexity] || 1000
  end
end
```

### Persisted Queries

```ruby
# app/graphql/persisted_queries/redis_store.rb
class RedisPersistedQueryStore
  def initialize(redis_client)
    @redis = redis_client
  end

  def save_query(query_id, query_string)
    @redis.setex("graphql:query:#{query_id}", 86400, query_string)
  end

  def get_query(query_id)
    @redis.get("graphql:query:#{query_id}")
  end

  def delete_query(query_id)
    @redis.del("graphql:query:#{query_id}")
  end
end

# config/initializers/graphql.rb
GraphQL::PersistedQueries.configure do |config|
  config.store = RedisPersistedQueryStore.new(Redis.new)
  config.compiled_queries = true
end
```

## Performance Optimization

### Query Batching

```ruby
# app/graphql/loaders/association_loader.rb
class AssociationLoader < GraphQL::Batch::Loader
  def initialize(model, association_name, scope = nil)
    @model = model
    @association_name = association_name
    @scope = scope
  end

  def perform(records)
    preloader = ActiveRecord::Associations::Preloader.new
    preloader.preload(records, @association_name => @scope)
    records.each { |record| fulfill(record, record.public_send(@association_name)) }
  end
end

# Usage in types
def comments
  AssociationLoader.for(Post, :comments).load(object)
end
```

### Field-level Caching

```ruby
# app/graphql/types/user_type.rb
module Types
  class UserType < Types::BaseObject
    field :posts_count, Integer, null: false do
      cache_key { |user| "user:#{user.id}:posts_count" }
      cache_ttl 300 # 5 minutes
    end

    def posts_count
      Rails.cache.fetch(cache_key, expires_in: cache_ttl) do
        object.posts.count
      end
    end
  end
end
```

### Query Analysis

```ruby
# app/graphql/analysis/query_analyzer.rb
class QueryAnalyzer < GraphQL::Analysis::AST::Analyzer
  def on_operation_multiplex(operation_multiplex)
    operation_multiplex.operations.each do |operation|
      analyze_operation(operation)
    end
  end

  private

  def analyze_operation(operation)
    fields = extract_fields(operation)
    
    Rails.logger.info(
      "GraphQL Query Analysis: " \
      "Operation: #{operation.operation_type}, " \
      "Fields: #{fields.join(', ')}"
    )
  end

  def extract_fields(selection_set, prefix = "")
    fields = []
    
    selection_set.selections.each do |selection|
      if selection.is_a?(GraphQL::Language::Nodes::Field)
        field_name = prefix.empty? ? selection.name : "#{prefix}.#{selection.name}"
        fields << field_name
        
        if selection.selections
          fields.concat(extract_fields(selection.selections, field_name))
        end
      end
    end
    
    fields
  end
end
```

## Security

### Authentication

```ruby
# app/controllers/graphql_controller.rb
class GraphqlController < ApplicationController
  def execute
    variables = prepare_variables(params[:variables])
    query = params[:query]
    operation_name = params[:operationName]
    
    context = {
      current_user: current_user,
      request: request,
      session: session
    }
    
    result = Schema.execute(
      query,
      variables: variables,
      context: context,
      operation_name: operation_name
    )
    
    render json: result
  rescue StandardError => e
    raise e unless Rails.env.development?
    handle_error_in_development(e)
  end

  private

  def current_user
    token = request.headers['Authorization']&.split(' ')&.last
    return nil unless token
    
    begin
      decoded_token = JWT.decode(token, Rails.application.secrets.secret_key_base, true, algorithm: 'HS256')
      User.find(decoded_token[0]['user_id'])
    rescue JWT::DecodeError, ActiveRecord::RecordNotFound
      nil
    end
  end
end
```

### Authorization

```ruby
# app/graphql/authorization/field_authorization.rb
class FieldAuthorization
  def self.authorized?(object, context)
    return true unless context[:current_user]
    
    case object
    when User
      object == context[:current_user] || context[:current_user].admin?
    when Post
      object.user == context[:current_user] || object.published? || context[:current_user].admin?
    when Comment
      object.user == context[:current_user] || context[:current_user].admin?
    else
      true
    end
  end
end

# Usage in types
field :email, String, null: false do
  authorize { |user, context| user == context[:current_user] || context[:current_user].admin? }
end
```

### Rate Limiting

```ruby
# app/graphql/middleware/rate_limiting.rb
class RateLimiting
  def self.call(schema_member, context)
    user_id = context[:current_user]&.id || context[:request]&.remote_ip
    
    return yield unless user_id
    
    key = "graphql:rate_limit:#{user_id}"
    current_count = Redis.current.get(key).to_i
    
    if current_count >= max_requests_per_minute
      raise GraphQL::ExecutionError.new("Rate limit exceeded")
    end
    
    Redis.current.multi do |multi|
      multi.incr(key)
      multi.expire(key, 60)
    end
    
    yield
  end

  private

  def self.max_requests_per_minute
    1000
  end
end
```

## Testing

### GraphQL Spec Helper

```ruby
# spec/support/graphql_helper.rb
module GraphQLHelper
  def execute_query(query, variables: {}, context: {})
    Schema.execute(
      query,
      variables: variables,
      context: context
    )
  end

  def graphql_response
    JSON.parse(response.body)
  end

  def graphql_errors
    graphql_response['errors']
  end

  def graphql_data
    graphql_response['data']
  end
end

RSpec.configure do |config|
  config.include GraphQLHelper, type: :request
end
```

### Query Tests

```ruby
# spec/requests/graphql/queries/users_spec.rb
RSpec.describe "GraphQL Queries", type: :request do
  describe "users query" do
    let(:user) { create(:user) }
    let(:query) do
      <<~GQL
        query GetUsers($limit: Int!, $offset: Int!) {
          users(limit: $limit, offset: $offset) {
            id
            name
            email
            posts {
              id
              title
            }
          }
        }
      GQL
    end

    it "returns users with posts" do
      post = create(:post, user: user)
      
      post "/graphql", params: {
        query: query,
        variables: { limit: 10, offset: 0 }
      }

      expect(response).to have_http_status(:success)
      expect(graphql_data['users']).to be_present
      expect(graphql_data['users'].first['posts']).to be_present
    end
  end
end
```

### Mutation Tests

```ruby
# spec/requests/graphql/mutations/create_user_spec.rb
RSpec.describe "GraphQL Mutations", type: :request do
  describe "createUser mutation" do
    let(:mutation) do
      <<~GQL
        mutation CreateUser($email: String!, $name: String!, $password: String!, $passwordConfirmation: String!) {
          createUser(input: {
            email: $email,
            name: $name,
            password: $password,
            passwordConfirmation: $passwordConfirmation
          }) {
            user {
              id
              email
              name
            }
            errors
          }
        }
      GQL
    end

    it "creates a new user" do
      post "/graphql", params: {
        query: mutation,
        variables: {
          email: "test@example.com",
          name: "Test User",
          password: "password123",
          passwordConfirmation: "password123"
        }
      }

      expect(response).to have_http_status(:success)
      expect(graphql_data['createUser']['user']).to be_present
      expect(graphql_data['createUser']['errors']).to be_empty
    end

    it "returns errors for invalid input" do
      post "/graphql", params: {
        query: mutation,
        variables: {
          email: "invalid-email",
          name: "",
          password: "short",
          passwordConfirmation: "different"
        }
      }

      expect(response).to have_http_status(:success)
      expect(graphql_data['createUser']['user']).to be_nil
      expect(graphql_data['createUser']['errors']).to be_present
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # GraphQL configuration
  config.graphql = {
    schema_path: Rails.root.join("app/graphql/schema.graphql"),
    introspection: false,
    persisted_queries: true,
    query_analysis: true,
    field_instrumentation: true
  }
end
```

### Nginx Configuration

```nginx
# nginx.conf
upstream graphql_backend {
  server unix:/tmp/graphql.sock;
}

server {
  listen 80;
  server_name api.example.com;

  location /graphql {
    proxy_pass http://graphql_backend;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    
    # GraphQL specific headers
    proxy_set_header X-GraphQL-Operation $http_x_graphql_operation;
    proxy_set_header X-GraphQL-Variables $http_x_graphql_variables;
  }

  location /graphql/websocket {
    proxy_pass http://graphql_backend;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
  }
}
```

### Docker Configuration

```dockerfile
# Dockerfile
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    postgresql-dev \
    redis

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

RUN bundle exec rake assets:precompile

EXPOSE 3000

CMD ["bundle", "exec", "rails", "server", "-b", "0.0.0.0"]
```

```yaml
# docker-compose.yml
version: '3.8'

services:
  web:
    build: .
    ports:
      - "3000:3000"
    environment:
      - RAILS_ENV=production
      - DATABASE_URL=postgresql://postgres:password@db:5432/graphql_app
      - REDIS_URL=redis://redis:6379/1
    depends_on:
      - db
      - redis

  db:
    image: postgres:15-alpine
    environment:
      - POSTGRES_DB=graphql_app
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
```

This comprehensive GraphQL integration guide provides everything needed to build robust, scalable GraphQL APIs with TuskLang and Ruby, including advanced patterns, performance optimization, security, testing, and deployment strategies. 
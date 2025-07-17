# 🌐 TuskLang Ruby Advanced API Design Guide

**"We don't bow to any king" - Ruby Edition**

Design world-class APIs with TuskLang in Ruby. Master REST, GraphQL, versioning, documentation, and advanced API patterns.

## 🚀 REST API Design

### 1. API Configuration
```ruby
# config/api_design.tsk
[api_design]
enabled: true
version: "v1"
base_url: "https://api.myapp.com"
documentation_url: "https://docs.myapp.com"

[rest_api]
versioning: "url"  # url, header, parameter
default_format: "json"
supported_formats: ["json", "xml"]
pagination: {
    default_limit: 20
    max_limit: 100
    page_parameter: "page"
    limit_parameter: "limit"
}

[authentication]
type: "jwt"
header: "Authorization"
token_prefix: "Bearer"
expiration: "24h"
refresh_expiration: "7d"

[rate_limiting]
enabled: true
default_limit: 1000
window: "1h"
headers: {
    limit: "X-RateLimit-Limit"
    remaining: "X-RateLimit-Remaining"
    reset: "X-RateLimit-Reset"
}
```

### 2. API Endpoints
```ruby
# config/api_endpoints.tsk
[api_endpoints]
# Users API
users: {
    base_path: "/users"
    methods: ["GET", "POST", "PUT", "DELETE"]
    authentication: true
    rate_limit: 100
    cache_ttl: "5m"
}

user_profile: {
    path: "/users/{id}/profile"
    methods: ["GET", "PUT"]
    authentication: true
    rate_limit: 50
    cache_ttl: "10m"
}

user_orders: {
    path: "/users/{id}/orders"
    methods: ["GET", "POST"]
    authentication: true
    rate_limit: 200
    pagination: true
}

# Orders API
orders: {
    base_path: "/orders"
    methods: ["GET", "POST", "PUT", "DELETE"]
    authentication: true
    rate_limit: 500
    cache_ttl: "2m"
}

order_items: {
    path: "/orders/{id}/items"
    methods: ["GET", "POST", "PUT", "DELETE"]
    authentication: true
    rate_limit: 300
}

# Products API
products: {
    base_path: "/products"
    methods: ["GET", "POST", "PUT", "DELETE"]
    authentication: true
    rate_limit: 1000
    cache_ttl: "1h"
}

product_search: {
    path: "/products/search"
    methods: ["GET"]
    authentication: false
    rate_limit: 2000
    cache_ttl: "30m"
}
```

## 🕸️ GraphQL API Design

### 1. GraphQL Configuration
```ruby
# config/graphql.tsk
[graphql]
enabled: true
endpoint: "/graphql"
playground: true
introspection: true

[graphql_schema]
user_type: {
    fields: ["id", "email", "name", "profile", "orders"]
    resolvers: ["user_resolver", "profile_resolver", "orders_resolver"]
}

order_type: {
    fields: ["id", "user", "items", "total", "status", "created_at"]
    resolvers: ["order_resolver", "items_resolver"]
}

product_type: {
    fields: ["id", "name", "description", "price", "category"]
    resolvers: ["product_resolver"]
}

[graphql_queries]
get_user: {
    type: "query"
    resolver: "user_resolver"
    arguments: ["id"]
    authentication: true
}

get_orders: {
    type: "query"
    resolver: "orders_resolver"
    arguments: ["user_id", "status"]
    authentication: true
    pagination: true
}

search_products: {
    type: "query"
    resolver: "product_search_resolver"
    arguments: ["query", "category", "price_range"]
    authentication: false
    cache_ttl: "30m"
}

[graphql_mutations]
create_user: {
    type: "mutation"
    resolver: "create_user_resolver"
    arguments: ["email", "name", "password"]
    authentication: false
}

update_order: {
    type: "mutation"
    resolver: "update_order_resolver"
    arguments: ["id", "status"]
    authentication: true
}

create_order: {
    type: "mutation"
    resolver: "create_order_resolver"
    arguments: ["user_id", "items"]
    authentication: true
}
```

## 📚 API Versioning

### 1. Version Management
```ruby
# config/api_versioning.tsk
[api_versioning]
enabled: true
strategy: "url"  # url, header, parameter
current_version: "v1"
supported_versions: ["v1", "v2"]
deprecated_versions: ["v0"]
sunset_versions: ["v0"]

[version_strategies]
url_versioning: {
    pattern: "/api/{version}/"
    default_version: "v1"
}

header_versioning: {
    header_name: "API-Version"
    default_version: "v1"
}

parameter_versioning: {
    parameter_name: "version"
    default_version: "v1"
}

[version_migration]
v0_to_v1: {
    deprecated_endpoints: ["/api/v0/users", "/api/v0/orders"]
    migration_guide: "https://docs.myapp.com/migration/v0-to-v1"
    sunset_date: "2024-12-31"
}

v1_to_v2: {
    breaking_changes: ["user.profile", "order.items"]
    migration_guide: "https://docs.myapp.com/migration/v1-to-v2"
    beta_date: "2024-06-01"
    release_date: "2024-12-01"
}
```

## 📖 API Documentation

### 1. OpenAPI/Swagger Configuration
```ruby
# config/api_documentation.tsk
[api_documentation]
enabled: true
format: "openapi"
version: "3.0.0"
title: "MyApp API"
description: "Comprehensive API for MyApp"
contact: {
    name: "API Support"
    email: "api@myapp.com"
    url: "https://support.myapp.com"
}

[documentation_servers]
production: {
    url: "https://api.myapp.com"
    description: "Production server"
}

staging: {
    url: "https://api-staging.myapp.com"
    description: "Staging server"
}

development: {
    url: "http://localhost:3000"
    description: "Development server"
}

[documentation_schemas]
user_schema: {
    type: "object"
    properties: {
        id: { type: "integer" }
        email: { type: "string", format: "email" }
        name: { type: "string" }
        created_at: { type: "string", format: "date-time" }
    }
    required: ["email", "name"]
}

order_schema: {
    type: "object"
    properties: {
        id: { type: "integer" }
        user_id: { type: "integer" }
        total: { type: "number" }
        status: { type: "string", enum: ["pending", "completed", "cancelled"] }
        created_at: { type: "string", format: "date-time" }
    }
    required: ["user_id", "total"]
}

[documentation_endpoints]
get_users: {
    path: "/users"
    method: "GET"
    summary: "List users"
    description: "Retrieve a list of users with pagination"
    parameters: ["page", "limit", "search"]
    responses: {
        200: "List of users"
        400: "Bad request"
        401: "Unauthorized"
    }
}

create_user: {
    path: "/users"
    method: "POST"
    summary: "Create user"
    description: "Create a new user account"
    request_body: "user_schema"
    responses: {
        201: "User created"
        400: "Validation error"
        409: "User already exists"
    }
}
```

## 🛠️ Ruby Integration Example

### 1. API Controller
```ruby
# app/controllers/api/v1/base_controller.rb
class Api::V1::BaseController < ApplicationController
  before_action :authenticate_user!
  before_action :check_rate_limit
  before_action :set_api_version

  private

  def authenticate_user!
    token = request.headers['Authorization']&.gsub('Bearer ', '')
    @current_user = User.find_by_jwt_token(token)
    
    unless @current_user
      render json: { error: 'Unauthorized' }, status: :unauthorized
    end
  end

  def check_rate_limit
    config = TuskLang.config
    limit = config['api_endpoints'][controller_name]['rate_limit']
    
    if RateLimitService.exceeded?(@current_user, limit)
      render json: { error: 'Rate limit exceeded' }, status: :too_many_requests
    end
  end

  def set_api_version
    @api_version = request.path.split('/')[2] || 'v1'
  end
end

# app/controllers/api/v1/users_controller.rb
class Api::V1::UsersController < BaseController
  def index
    config = TuskLang.config
    limit = config['rest_api']['pagination']['default_limit']
    
    users = User.limit(limit)
    
    render json: {
      users: users,
      pagination: {
        limit: limit,
        total: User.count
      }
    }
  end

  def show
    user = User.find(params[:id])
    
    render json: user
  end

  def create
    user = User.new(user_params)
    
    if user.save
      render json: user, status: :created
    else
      render json: { errors: user.errors }, status: :unprocessable_entity
    end
  end

  private

  def user_params
    params.require(:user).permit(:email, :name, :password)
  end
end
```

### 2. GraphQL Resolvers
```ruby
# app/graphql/resolvers/user_resolver.rb
class UserResolver
  def self.call(obj, args, ctx)
    config = TuskLang.config
    
    if args[:id]
      User.find(args[:id])
    else
      User.all
    end
  end
end

# app/graphql/resolvers/orders_resolver.rb
class OrdersResolver
  def self.call(obj, args, ctx)
    config = TuskLang.config
    
    orders = Order.where(user_id: args[:user_id])
    orders = orders.where(status: args[:status]) if args[:status]
    
    orders
  end
end
```

### 3. API Service
```ruby
# app/services/api_service.rb
require 'tusklang'

class ApiService
  def self.load_api_config
    parser = TuskLang.new
    parser.parse_file('config/api_design.tsk')
  end

  def self.generate_documentation
    config = load_api_config
    
    if config['api_documentation']['enabled']
      OpenApiGenerator.generate(config)
    end
  end

  def self.validate_api_version(version)
    config = load_api_config
    supported_versions = config['api_versioning']['supported_versions']
    
    supported_versions.include?(version)
  end

  def self.get_endpoint_config(endpoint_name)
    config = load_api_config
    config['api_endpoints'][endpoint_name]
  end

  def self.monitor_api_usage
    config = load_api_config
    
    # Monitor API metrics
    ApiMetricsService.record_usage
    ApiMetricsService.check_rate_limits
  end
end

# Usage
config = ApiService.load_api_config
ApiService.generate_documentation
ApiService.monitor_api_usage
```

## 🛡️ Best Practices
- Design RESTful APIs with proper HTTP methods and status codes.
- Use GraphQL for complex data requirements.
- Implement proper API versioning strategy.
- Document APIs with OpenAPI/Swagger.
- Monitor API usage and performance.

**Ready to build world-class APIs? Let's Tusk! 🚀** 
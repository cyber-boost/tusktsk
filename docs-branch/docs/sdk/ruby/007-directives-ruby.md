# 🏷️ TuskLang Ruby Directives Guide

**"We don't bow to any king" - Ruby Edition**

Directives (hashes) in TuskLang let you define powerful behaviors for web, API, CLI, cron, middleware, auth, cache, and more—directly in your config files.

## 🚦 What Are Directives?

Directives are special sections that control how your application behaves in different contexts. They can define routes, middleware, authentication, caching, rate limits, and more.

## 🚀 Common Directives

### 1. Web Routes
```ruby
# config/routes.tsk
web {
  route "/" {
    controller: "home#index"
    method: "GET"
    auth: false
  }
  route "/users" {
    controller: "users#index"
    method: "GET"
    auth: true
    cache: "5m"
  }
}
```

### 2. API Endpoints
```ruby
# config/api.tsk
api {
  endpoint "/v1/users" {
    method: "GET"
    controller: "api/v1/users#index"
    rate_limit: 1000
    auth: true
  }
  endpoint "/v1/orders" {
    method: "POST"
    controller: "api/v1/orders#create"
    auth: true
    validate: ["order_params"]
  }
}
```

### 3. CLI Commands
```ruby
# config/cli.tsk
cli {
  command "import_users" {
    script: "scripts/import_users.rb"
    schedule: "0 2 * * *"
    notify: true
  }
  command "cleanup" {
    script: "scripts/cleanup.rb"
    schedule: "@daily"
    notify: false
  }
}
```

### 4. Middleware
```ruby
# config/middleware.tsk
middleware {
  use "Rack::Attack" {
    config: "config/rack_attack.rb"
  }
  use "RequestLogger" {
    level: "info"
    format: "json"
  }
}
```

### 5. Auth
```ruby
# config/auth.tsk
auth {
  provider: "devise"
  strategies: ["database_authenticatable", "jwt"]
  jwt_secret: @env.secure("JWT_SECRET")
  password_min_length: 12
}
```

### 6. Cache
```ruby
# config/cache.tsk
cache {
  driver: "redis"
  ttl: "5m"
  namespace: "myapp"
}
```

### 7. Rate Limiting
```ruby
# config/rate_limit.tsk
rate_limit {
  global: 1000
  per_user: 100
  per_ip: 500
  window: "1m"
}
```

## 🛡️ Best Practices
- Use descriptive directive names and group related settings.
- Leverage @ operators inside directives for dynamic values.
- Validate all user input in API and CLI directives.
- Use environment variables for secrets and credentials.

## 🛠️ Ruby Integration Example
```ruby
# app/services/directive_demo.rb
require 'tusklang'

class DirectiveDemo
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/directives.tsk')
  end
end

config = DirectiveDemo.load_config
puts "Web Routes: #{config['web']}"
puts "API Endpoints: #{config['api']}"
```

## 🎯 Next Steps
- Explore advanced directives: middleware, auth, cache, rate-limit
- Integrate directives with Rails routes, controllers, and jobs
- Use directives for scalable, maintainable configuration

**Ready to command your app with directives? Let's Tusk! 🚀** 
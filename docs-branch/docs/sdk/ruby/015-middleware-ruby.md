# 🧩 TuskLang Ruby Middleware Integration Guide

**"We don't bow to any king" - Ruby Edition**

Supercharge your Ruby apps with TuskLang-driven middleware. Integrate with Rack, build custom middleware, and control behavior from your configs.

## 🚦 Rack Middleware Integration

### 1. Define Middleware in Config
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

### 2. Load Middleware in Rails
```ruby
# config/application.rb
require 'tusklang'

module MyApp
  class Application < Rails::Application
    config.middleware.use Rack::Attack if TuskLang.config['middleware']['Rack::Attack']
    config.middleware.use RequestLogger, level: 'info', format: 'json' if TuskLang.config['middleware']['RequestLogger']
  end
end
```

## 🛠️ Custom Middleware

### 1. Build Custom Middleware
```ruby
# lib/middleware/request_logger.rb
class RequestLogger
  def initialize(app, level: 'info', format: 'json')
    @app = app
    @level = level
    @format = format
  end

  def call(env)
    # Log request details
    puts "[#{@level}] #{env['REQUEST_METHOD']} #{env['PATH_INFO']}"
    @app.call(env)
  end
end
```

### 2. Register Custom Middleware
```ruby
# config/application.rb
require_relative '../lib/middleware/request_logger'
config.middleware.use RequestLogger, level: 'info', format: 'json'
```

## 🛡️ Best Practices
- Use config-driven middleware for flexibility.
- Validate middleware configs before loading.
- Log all middleware actions for debugging and auditing.

**Ready to control the stack? Let's Tusk! 🚀** 
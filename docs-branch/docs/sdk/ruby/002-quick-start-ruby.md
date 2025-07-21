# 🚀 TuskLang Ruby Quick Start

**"We don't bow to any king" - Ruby Edition**

Get up and running with TuskLang in Ruby in under 5 minutes. This guide will show you the core concepts and get you building dynamic configurations immediately.

## ⚡ 5-Minute Setup

### 1. Install TuskLang
```bash
gem install tusklang
```

### 2. Create Your First TSK File
```ruby
# config/app.tsk
$app_name: "My Awesome App"
$version: "1.0.0"

[database]
host: "localhost"
port: 5432
name: "myapp"

[server]
host: "0.0.0.0"
port: 8080
debug: true
```

### 3. Parse and Use
```ruby
# app.rb
require 'tusklang'

# Parse the configuration
parser = TuskLang.new
config = parser.parse_file('config/app.tsk')

# Use the configuration
puts "App: #{config['app_name']} v#{config['version']}"
puts "Database: #{config['database']['host']}:#{config['database']['port']}"
puts "Server: #{config['server']['host']}:#{config['server']['port']}"
```

### 4. Run It
```bash
ruby app.rb
```

**Output:**
```
App: My Awesome App v1.0.0
Database: localhost:5432
Server: 0.0.0.0:8080
```

## 🎯 Core Concepts in 10 Minutes

### 1. Multiple Syntax Styles
TuskLang supports your preferred syntax style:

```ruby
# Traditional INI-style (sections)
[server]
host: "0.0.0.0"
port: 8080

# JSON-like objects (curly braces)
server {
    host: "0.0.0.0"
    port: 8080
}

# XML-inspired (angle brackets)
server >
    host: "0.0.0.0"
    port: 8080
<
```

### 2. Global Variables
```ruby
# config/app.tsk
$app_name: "MyApp"
$version: "1.0.0"
$environment: @env("RAILS_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
```

### 3. @ Operators - The Magic
```ruby
# config/dynamic.tsk
$current_time: @date.now()
$user_count: @query("SELECT COUNT(*) FROM users")
$api_key: @env("API_KEY", "default_key")
$cached_value: @cache("5m", "expensive_operation")
$encrypted_data: @encrypt("sensitive_info", "AES-256-GCM")
```

## 🔥 Real-World Examples

### 1. Rails Application Configuration
```ruby
# config/application.tsk
$app_name: "MyRailsApp"
$version: "2.1.0"
$environment: @env("RAILS_ENV", "development")

[database]
host: @env("DATABASE_HOST", "localhost")
port: @env("DATABASE_PORT", 5432)
name: "#{app_name}_#{environment}"
user: @env("DATABASE_USER", "postgres")
password: @env("DATABASE_PASSWORD", "")

[server]
host: @env("SERVER_HOST", "0.0.0.0")
port: @env("SERVER_PORT", 3000)
workers: @if($environment == "production", 4, 1)
ssl: @if($environment == "production", true, false)

[cache]
driver: @env("CACHE_DRIVER", "redis")
host: @env("REDIS_HOST", "localhost")
port: @env("REDIS_PORT", 6379)
ttl: "5m"

[security]
cors_origins: @if($environment == "production", 
    ["https://myapp.com"], 
    ["http://localhost:3000", "http://127.0.0.1:3000"]
)
rate_limit: @if($environment == "production", 100, 1000)
```

### 2. API Configuration with Database Queries
```ruby
# config/api.tsk
$api_version: "v1"
$base_url: "https://api.myapp.com"

[authentication]
jwt_secret: @env("JWT_SECRET", "default_secret")
token_expiry: "24h"
refresh_expiry: "7d"

[rate_limits]
default: 1000
premium: 10000
admin: 100000

[features]
user_count: @query("SELECT COUNT(*) FROM users WHERE active = true")
premium_users: @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > ?", @date.subtract("7d"))

[webhooks]
enabled: @if(@query("SELECT COUNT(*) FROM webhooks") > 0, true, false)
endpoints: @query("SELECT url FROM webhooks WHERE active = true")
```

### 3. Microservice Configuration
```ruby
# config/microservice.tsk
$service_name: "user-service"
$service_version: "1.2.0"
$instance_id: @env("INSTANCE_ID", @uuid.generate())

[service]
name: $service_name
version: $service_version
instance_id: $instance_id
health_check: "/health"
metrics: "/metrics"

[database]
primary: {
    host: @env("DB_PRIMARY_HOST", "localhost")
    port: @env("DB_PRIMARY_PORT", 5432)
    name: @env("DB_NAME", "users")
    user: @env("DB_USER", "postgres")
    password: @env("DB_PASSWORD", "")
}
replica: {
    host: @env("DB_REPLICA_HOST", "localhost")
    port: @env("DB_REPLICA_PORT", 5432)
    name: @env("DB_NAME", "users")
    user: @env("DB_USER", "postgres")
    password: @env("DB_PASSWORD", "")
}

[cache]
redis: {
    host: @env("REDIS_HOST", "localhost")
    port: @env("REDIS_PORT", 6379)
    db: 0
    ttl: "10m"
}

[monitoring]
log_level: @env("LOG_LEVEL", "info")
metrics_enabled: true
tracing_enabled: @if($environment == "production", true, false)
```

## 🛠️ Ruby Integration Examples

### 1. Rails Controller Usage
```ruby
# app/controllers/api/v1/base_controller.rb
class Api::V1::BaseController < ApplicationController
  def tusk_config
    Rails.application.config.tusk_config
  end
  
  def rate_limit
    tusk_config['rate_limits']['default']
  end
  
  def api_version
    tusk_config['api_version']
  end
end

# app/controllers/api/v1/users_controller.rb
class Api::V1::UsersController < BaseController
  def index
    config = tusk_config
    
    # Use configuration values
    limit = config['rate_limits']['default']
    user_count = config['features']['user_count']
    
    users = User.limit(limit)
    
    render json: {
      users: users,
      total_users: user_count,
      api_version: api_version
    }
  end
end
```

### 2. Model Configuration
```ruby
# app/models/user.rb
class User < ApplicationRecord
  def self.config
    TuskLang.config
  end
  
  def self.rate_limit
    config['rate_limits']['default']
  end
  
  def self.premium_rate_limit
    config['rate_limits']['premium']
  end
  
  def self.active_user_count
    config['features']['user_count']
  end
end
```

### 3. Background Job Configuration
```ruby
# app/jobs/email_job.rb
class EmailJob < ApplicationJob
  queue_as :default
  
  def perform(user_id)
    config = TuskLang.config
    
    # Use configuration for email settings
    email_config = config['email']
    
    user = User.find(user_id)
    
    UserMailer.with(
      user: user,
      template: email_config['template'],
      from: email_config['from_address']
    ).welcome_email.deliver_now
  end
end
```

## 🔧 Advanced Quick Examples

### 1. Conditional Configuration
```ruby
# config/conditional.tsk
$environment: @env("RAILS_ENV", "development")
$is_production: @if($environment == "production", true, false)

[logging]
level: @if($is_production, "error", "debug")
format: @if($is_production, "json", "text")
file: @if($is_production, "/var/log/app.log", "console")

[security]
ssl: $is_production
cors: @if($is_production, {
    origin: ["https://myapp.com"],
    credentials: true
}, {
    origin: "*",
    credentials: false
})
```

### 2. Cross-File References
```ruby
# config/main.tsk
$app_name: "MyApp"
$version: "1.0.0"

[database]
host: @config.database.tsk.get("host")
port: @config.database.tsk.get("port")

[server]
host: @config.server.tsk.get("host")
port: @config.server.tsk.get("port")
```

```ruby
# config/database.tsk
host: "localhost"
port: 5432
name: "myapp"
```

```ruby
# config/server.tsk
host: "0.0.0.0"
port: 8080
```

### 3. Dynamic Database Queries
```ruby
# config/dynamic.tsk
[analytics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("30d"))
premium_users: @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")
revenue_today: @query("SELECT SUM(amount) FROM transactions WHERE DATE(created_at) = ?", @date.today())
```

## 🎯 Next Steps

Now that you've seen the basics, explore:

1. **@ Operators Guide** - Master all the powerful built-in functions
2. **Database Integration** - Connect to SQLite, PostgreSQL, MySQL, MongoDB, Redis
3. **Rails Integration** - Deep integration with Rails applications
4. **Security Features** - Validation, encryption, and security best practices
5. **Advanced Features** - Caching, monitoring, and performance optimization

## 🚀 Why TuskLang for Ruby?

- **Revolutionary**: Database queries in configuration files
- **Flexible**: Multiple syntax styles to match your preferences
- **Powerful**: @ operators for dynamic, intelligent configuration
- **Rails-Ready**: Seamless integration with Rails applications
- **Secure**: Built-in validation and encryption
- **Fast**: Optimized parsing and caching

**Ready to revolutionize your Ruby configuration? Let's Tusk! 🚀** 
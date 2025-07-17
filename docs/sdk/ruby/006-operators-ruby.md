# ⚡ TuskLang Ruby @ Operators Guide

**"We don't bow to any king" - Ruby Edition**

Master the @ operator system in TuskLang for Ruby. These operators bring dynamic power, real-time data, and advanced logic to your configuration files.

## 🚀 What Are @ Operators?

@ operators are built-in functions that inject dynamic values, perform computations, and integrate with external systems directly in your TSK files.

## 🧩 Core @ Operators

### 1. Environment Variables
```ruby
# config/env.tsk
$api_key: @env("API_KEY", "default_key")
$rails_env: @env("RAILS_ENV", "development")
$debug: @env("DEBUG", false)
```

### 2. Date/Time
```ruby
# config/dates.tsk
$current_time: @date.now()
$today: @date.today()
$yesterday: @date.subtract("1d")
$last_week: @date.subtract("7d")
$formatted: @date("%Y-%m-%d %H:%M:%S")
```

### 3. Database Queries
```ruby
# config/db_queries.tsk
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
```

### 4. Caching
```ruby
# config/cache.tsk
cached_value: @cache("5m", @query("SELECT COUNT(*) FROM users"))
```

### 5. HTTP Requests
```ruby
# config/http.tsk
api_response: @http("GET", "https://api.example.com/data")
```

### 6. File Operations
```ruby
# config/files.tsk
file_content: @file.read("/etc/hosts")
```

### 7. Machine Learning
```ruby
# config/ml.tsk
optimal_setting: @learn("cache_ttl", "5m")
```

### 8. Metrics
```ruby
# config/metrics.tsk
response_time: @metrics("response_time_ms", 150)
```

### 9. PHP Execution (for hybrid Ruby/PHP stacks)
```ruby
# config/php.tsk
memory_usage: @php("memory_get_usage(true)")
```

## 🔒 Security Notes
- Use @env.secure for sensitive environment variables.
- Validate all user input with @validate operators.
- Avoid exposing secrets in logs or error messages.

## 🚀 Performance Notes
- Use @cache to avoid repeated expensive queries.
- Prefer parameterized @query calls for security and speed.
- Use @metrics to monitor and optimize config-driven performance.

## 🛠️ Ruby Integration Example
```ruby
# app/services/operator_demo.rb
require 'tusklang'

class OperatorDemo
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/operators.tsk')
  end
end

config = OperatorDemo.load_config
puts "API Key: #{config['api_key']}"
puts "User Count: #{config['user_count']}"
puts "Current Time: #{config['current_time']}"
```

## 🎯 Next Steps
- Explore advanced operators: @optimize, @validate, @encrypt, @http, @file, @php
- Integrate @ operators with Rails, background jobs, and API endpoints
- Monitor operator performance with @metrics

**Ready to wield the power of @? Let's Tusk! 🚀** 
# ⚡ TuskLang Ruby Advanced @ Operators Guide

**"We don't bow to any king" - Ruby Edition**

Unlock the full power of TuskLang with advanced @ operators for Ruby. Use optimization, machine learning, validation, encryption, file, HTTP, and PHP operators in your configs.

## 🧠 Machine Learning & Optimization

### 1. @learn
```ruby
# config/ml.tsk
[ml]
optimal_cache_ttl: @learn("cache_ttl", "5m", {
  features: ["user_count", "request_rate", "cache_hit_rate"],
  algorithm: "random_forest"
})
```

### 2. @optimize
```ruby
# config/optimize.tsk
[adaptive]
cache_ttl: @optimize("cache_ttl", "5m", {
  metric: "cache_hit_rate",
  target: 0.8,
  min_value: "1m",
  max_value: "1h"
})
```

## 🛡️ Validation & Encryption

### 1. @validate
```ruby
# config/validation.tsk
[validation]
email: @validate.email(@request.email)
password: @validate.password(@request.password, {
  min_length: 12,
  require_uppercase: true,
  require_numbers: true,
  require_special: true
})
```

### 2. @encrypt
```ruby
# config/encryption.tsk
[encryption]
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")
```

## 📂 File & HTTP Operations

### 1. @file
```ruby
# config/files.tsk
file_content: @file.read("/etc/hosts")
```

### 2. @http
```ruby
# config/http.tsk
api_response: @http("GET", "https://api.example.com/data")
```

## 🐘 PHP Execution (for hybrid stacks)
```ruby
# config/php.tsk
memory_usage: @php("memory_get_usage(true)")
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_operator_demo.rb
require 'tusklang'

class AdvancedOperatorDemo
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/advanced_operators.tsk')
  end
end

config = AdvancedOperatorDemo.load_config
puts "Optimal Cache TTL: #{config['ml']['optimal_cache_ttl']}"
puts "API Response: #{config['http']['api_response']}"
```

## 🛡️ Best Practices
- Use @learn and @optimize for adaptive, intelligent configs.
- Always validate and encrypt sensitive data.
- Use @file and @http for dynamic, external data sources.
- Monitor performance and security of all advanced operators.

**Ready to go beyond basic? Let's Tusk! 🚀** 
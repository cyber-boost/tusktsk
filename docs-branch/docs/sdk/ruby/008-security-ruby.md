# 🔒 TuskLang Ruby Security Features Guide

**"We don't bow to any king" - Ruby Edition**

TuskLang brings enterprise-grade security to your Ruby configuration. Learn how to protect secrets, validate input, encrypt data, and enforce best practices.

## 🛡️ Core Security Features

### 1. Secure Environment Variables
```ruby
# config/secrets.tsk
[secrets]
api_key: @env.secure("API_KEY")
db_password: @env.secure("DATABASE_PASSWORD")
jwt_secret: @env.secure("JWT_SECRET")
```

### 2. Encryption
```ruby
# config/encryption.tsk
[encryption]
key: @env.secure("ENCRYPTION_KEY")
algorithm: "AES-256-GCM"

[sensitive]
db_password: @encrypt(@env("DATABASE_PASSWORD"), "AES-256-GCM")
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")
```

### 3. Input Validation
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
age: @validate.range(@request.age, 18, 120)
website: @validate.url(@request.website)
```

### 4. Rate Limiting
```ruby
# config/rate_limit.tsk
[rate_limit]
global: 1000
per_user: 100
per_ip: 500
window: "1m"
```

### 5. Security Directives
```ruby
# config/security.tsk
security {
  cors_origins: ["https://myapp.com"]
  csrf_protection: true
  hsts: true
  xss_protection: true
  content_security_policy: "default-src 'self'"
}
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/security_demo.rb
require 'tusklang'

class SecurityDemo
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/security.tsk')
  end
end

config = SecurityDemo.load_config
puts "API Key: #{config['secrets']['api_key']}"
puts "Encryption Key: #{config['encryption']['key']}"
```

## 🔒 Best Practices
- Always use @env.secure for secrets and credentials.
- Encrypt sensitive data at rest and in transit.
- Validate all user input with @validate operators.
- Use strong password policies and rate limiting.
- Enable security directives (CORS, CSRF, HSTS, XSS, CSP) for web apps.

## 🚨 Troubleshooting
- If a secret is missing, check your environment variables.
- For encryption errors, verify the key and algorithm.
- For validation failures, review input and validation rules.

**Ready to secure your Ruby configs? Let's Tusk! 🚀** 
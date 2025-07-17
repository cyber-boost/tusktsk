# 🔒 TuskLang Ruby Advanced Security Guide

**"We don't bow to any king" - Ruby Edition**

Implement enterprise-grade security with TuskLang in Ruby. Master encryption, authentication, authorization, and advanced security patterns.

## 🔐 Advanced Encryption

### 1. Multi-Algorithm Encryption
```ruby
# config/encryption.tsk
[encryption]
# Primary encryption key
primary_key: @env.secure("ENCRYPTION_PRIMARY_KEY")
# Backup encryption key
backup_key: @env.secure("ENCRYPTION_BACKUP_KEY")

# Algorithm configurations
algorithms {
    aes_256_gcm: {
        key_size: 256
        mode: "GCM"
        tag_length: 128
    }
    chacha20_poly1305: {
        key_size: 256
        mode: "ChaCha20-Poly1305"
        nonce_size: 12
    }
}

[sensitive_data]
# Encrypted database credentials
db_password: @encrypt(@env("DATABASE_PASSWORD"), "AES-256-GCM")
api_key: @encrypt(@env("API_KEY"), "ChaCha20-Poly1305")
jwt_secret: @encrypt(@env("JWT_SECRET"), "AES-256-GCM")

# Encrypted configuration values
payment_gateway_key: @encrypt(@env("PAYMENT_GATEWAY_KEY"), "AES-256-GCM")
email_password: @encrypt(@env("EMAIL_PASSWORD"), "AES-256-GCM")
```

### 2. Key Rotation
```ruby
# config/key_rotation.tsk
[key_rotation]
enabled: true
rotation_interval: "90d"
grace_period: "7d"

[keys]
current_key: @env.secure("ENCRYPTION_CURRENT_KEY")
previous_key: @env.secure("ENCRYPTION_PREVIOUS_KEY")
next_key: @env.secure("ENCRYPTION_NEXT_KEY")

[encrypted_data]
# Data encrypted with current key
current_data: @encrypt.rotate(@env("SENSITIVE_DATA"), "AES-256-GCM")
```

## 🔑 Advanced Authentication

### 1. Multi-Factor Authentication
```ruby
# config/authentication.tsk
[authentication]
provider: "devise"
strategies: ["database_authenticatable", "jwt", "two_factor_authenticatable"]

[mfa]
enabled: true
methods: ["totp", "sms", "email"]
backup_codes: true
grace_period: "24h"

[jwt]
secret: @env.secure("JWT_SECRET")
algorithm: "HS256"
expiration: "24h"
refresh_expiration: "7d"

[oauth]
providers: ["google", "github", "microsoft"]
client_ids: {
    google: @env.secure("GOOGLE_CLIENT_ID")
    github: @env.secure("GITHUB_CLIENT_ID")
    microsoft: @env.secure("MICROSOFT_CLIENT_ID")
}
client_secrets: {
    google: @env.secure("GOOGLE_CLIENT_SECRET")
    github: @env.secure("GITHUB_CLIENT_SECRET")
    microsoft: @env.secure("MICROSOFT_CLIENT_SECRET")
}
```

### 2. Session Management
```ruby
# config/session.tsk
[session]
driver: "redis"
secret: @env.secure("SESSION_SECRET")
expiration: "24h"
secure: @if($environment == "production", true, false)
http_only: true
same_site: "Lax"

[session_storage]
redis {
    host: @env("REDIS_HOST", "localhost")
    port: @env("REDIS_PORT", 6379)
    db: 1
    ttl: "24h"
}
```

## 🛡️ Advanced Authorization

### 1. Role-Based Access Control (RBAC)
```ruby
# config/authorization.tsk
[authorization]
provider: "pundit"
enabled: true

[roles]
admin: {
    permissions: ["read", "write", "delete", "manage_users", "manage_system"]
    level: 100
}
manager: {
    permissions: ["read", "write", "manage_team"]
    level: 50
}
user: {
    permissions: ["read", "write_own"]
    level: 10
}
guest: {
    permissions: ["read_public"]
    level: 1
}

[permissions]
# Resource-based permissions
users: {
    read: ["admin", "manager", "user"]
    write: ["admin", "manager"]
    delete: ["admin"]
}
orders: {
    read: ["admin", "manager", "user"]
    write: ["admin", "manager", "user"]
    delete: ["admin"]
}
payments: {
    read: ["admin", "manager"]
    write: ["admin"]
    delete: ["admin"]
}
```

### 2. Attribute-Based Access Control (ABAC)
```ruby
# config/abac.tsk
[abac]
enabled: true

[policies]
# User can only access their own data
user_data_access: @policy("user.id == resource.user_id OR user.role == 'admin'")

# Manager can access team data
team_data_access: @policy("user.role == 'manager' AND user.team_id == resource.team_id")

# Payment access based on amount
payment_access: @policy("user.role == 'admin' OR (user.role == 'manager' AND resource.amount <= 10000)")
```

## 🔍 Advanced Validation

### 1. Input Validation
```ruby
# config/validation.tsk
[validation]
# Email validation
email: @validate.email(@request.email, {
    allow_plus: true
    allow_dots: true
    check_mx: true
})

# Password validation
password: @validate.password(@request.password, {
    min_length: 12
    require_uppercase: true
    require_lowercase: true
    require_numbers: true
    require_special: true
    prevent_common: true
    prevent_sequential: true
})

# Custom validation rules
strong_password: @validate.custom(@request.password, "strong_password_rule")
valid_username: @validate.custom(@request.username, "username_rule")
valid_phone: @validate.phone(@request.phone, {
    country: "US"
    format: "international"
})
```

### 2. Data Sanitization
```ruby
# config/sanitization.tsk
[sanitization]
# HTML sanitization
html_content: @sanitize.html(@request.content, {
    allowed_tags: ["p", "br", "strong", "em", "a"]
    allowed_attributes: ["href", "title"]
})

# SQL injection prevention
sql_safe: @sanitize.sql(@request.query, {
    allowed_operators: ["=", "!=", ">", "<", "LIKE"]
    max_length: 100
})

# XSS prevention
xss_safe: @sanitize.xss(@request.input, {
    remove_scripts: true
    encode_html: true
})
```

## 🚨 Security Monitoring

### 1. Security Events
```ruby
# config/security_monitoring.tsk
[security_monitoring]
enabled: true

[events]
# Authentication events
login_attempts: @security.event("login_attempt", {
    user_id: @request.user_id
    ip_address: @request.ip
    success: @request.login_success
    timestamp: @date.now()
})

# Authorization events
access_denied: @security.event("access_denied", {
    user_id: @request.user_id
    resource: @request.resource
    action: @request.action
    reason: @request.denial_reason
})

# Suspicious activity
suspicious_activity: @security.event("suspicious_activity", {
    user_id: @request.user_id
    activity_type: @request.activity_type
    risk_score: @security.risk_score(@request.activity)
})
```

### 2. Rate Limiting
```ruby
# config/rate_limiting.tsk
[rate_limiting]
enabled: true

[limits]
# Global rate limits
global_requests: @if($environment == "production", 1000, 10000)
api_requests: @if($environment == "production", 100, 1000)

# User-specific rate limits
user_requests: @if($environment == "production", 100, 1000)
user_api_calls: @if($environment == "production", 50, 500)

# IP-based rate limits
ip_requests: @if($environment == "production", 500, 5000)
ip_login_attempts: 10

# Custom rate limiting rules
premium_user_requests: @if(@request.user.subscription_type == "premium", 200, 100)
admin_requests: @if(@request.user.role == "admin", 1000, 100)

# Burst protection
burst_limit: 50
burst_window: "1s"
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_security_service.rb
require 'tusklang'

class AdvancedSecurityService
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/advanced_security.tsk')
  end

  def self.encrypt_sensitive_data(data, algorithm = "AES-256-GCM")
    config = load_config
    encryption_config = config['encryption']
    
    case algorithm
    when "AES-256-GCM"
      key = encryption_config['primary_key']
      # Use OpenSSL for encryption
      cipher = OpenSSL::Cipher.new('aes-256-gcm')
      cipher.encrypt
      cipher.key = key
      iv = cipher.random_iv
      encrypted = cipher.update(data) + cipher.final
      tag = cipher.auth_tag
      
      # Return encrypted data with IV and tag
      { encrypted: encrypted, iv: iv, tag: tag }
    end
  end

  def self.validate_permission(user, resource, action)
    config = load_config
    permissions = config['permissions'][resource]
    
    return false unless permissions
    return permissions[action]&.include?(user.role)
  end

  def self.record_security_event(event_type, data)
    config = load_config
    security_config = config['security_monitoring']
    
    if security_config['enabled']
      event_config = security_config['events'][event_type]
      if event_config
        # Send to security monitoring system
        SecurityMonitoringSystem.record_event(event_type, data)
      end
    end
  end
end

# Usage
config = AdvancedSecurityService.load_config
encrypted_data = AdvancedSecurityService.encrypt_sensitive_data("sensitive_info")
has_permission = AdvancedSecurityService.validate_permission(current_user, "users", "read")
AdvancedSecurityService.record_security_event("login_attempt", { user_id: 1, success: true })
```

## 🛡️ Best Practices
- Use strong encryption algorithms and key rotation.
- Implement multi-factor authentication and session management.
- Use RBAC and ABAC for fine-grained authorization.
- Validate and sanitize all user input.
- Monitor security events and implement rate limiting.

**Ready to secure everything? Let's Tusk! 🚀** 
# 🔐 Advanced Authentication with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Build enterprise-grade authentication systems with TuskLang's advanced authentication features. From multi-factor authentication to OAuth2 integration, TuskLang provides the security and flexibility you need to protect your Ruby applications.

## 🚀 Quick Start

### Basic Authentication Setup
```ruby
require 'tusklang'
require 'tusklang/auth'

# Initialize authentication system
auth_system = TuskLang::Auth::System.new

# Configure authentication
auth_system.configure do |config|
  config.jwt_secret = ENV['JWT_SECRET']
  config.session_timeout = 24.hours
  config.password_min_length = 8
  config.require_mfa = true
end

# Register authentication strategies
auth_system.register_strategy(:jwt, TuskLang::Auth::Strategies::JWTStrategy.new)
auth_system.register_strategy(:oauth2, TuskLang::Auth::Strategies::OAuth2Strategy.new)
auth_system.register_strategy(:mfa, TuskLang::Auth::Strategies::MFAStrategy.new)
```

### TuskLang Configuration
```tsk
# config/authentication.tsk
[authentication]
enabled: true
default_strategy: "jwt"
session_timeout: "24h"
password_min_length: 8
require_mfa: true

[authentication.jwt]
secret: @env("JWT_SECRET", "default-secret")
algorithm: "HS256"
expiration: "24h"
refresh_expiration: "7d"

[authentication.oauth2]
providers: {
    google: {
        client_id: @env("GOOGLE_CLIENT_ID"),
        client_secret: @env("GOOGLE_CLIENT_SECRET"),
        redirect_uri: "https://app.example.com/auth/google/callback"
    },
    github: {
        client_id: @env("GITHUB_CLIENT_ID"),
        client_secret: @env("GITHUB_CLIENT_SECRET"),
        redirect_uri: "https://app.example.com/auth/github/callback"
    }
}

[authentication.mfa]
enabled: true
methods: ["totp", "sms", "email"]
backup_codes_count: 10
totp_issuer: "MyApp"

[authentication.security]
password_policy: {
    min_length: 8,
    require_uppercase: true,
    require_lowercase: true,
    require_numbers: true,
    require_special: true,
    prevent_common: true
}
rate_limiting: {
    login_attempts: 5,
    lockout_duration: "15m",
    reset_after: "1h"
}
```

## 🎯 Core Features

### 1. Multi-Strategy Authentication
```ruby
require 'tusklang/auth'

class AuthenticationController < ApplicationController
  include TuskLang::Auth::Controller
  
  def initialize
    @auth_system = TuskLang::Auth::System.new
    @config = TuskLang.parse_file('config/authentication.tsk')
    setup_strategies
  end
  
  private
  
  def setup_strategies
    # JWT Strategy
    jwt_strategy = TuskLang::Auth::Strategies::JWTStrategy.new(
      secret: @config['authentication']['jwt']['secret'],
      algorithm: @config['authentication']['jwt']['algorithm'],
      expiration: parse_duration(@config['authentication']['jwt']['expiration'])
    )
    @auth_system.register_strategy(:jwt, jwt_strategy)
    
    # OAuth2 Strategy
    oauth2_strategy = TuskLang::Auth::Strategies::OAuth2Strategy.new(
      providers: @config['authentication']['oauth2']['providers']
    )
    @auth_system.register_strategy(:oauth2, oauth2_strategy)
    
    # MFA Strategy
    mfa_strategy = TuskLang::Auth::Strategies::MFAStrategy.new(
      enabled: @config['authentication']['mfa']['enabled'],
      methods: @config['authentication']['mfa']['methods'],
      backup_codes_count: @config['authentication']['mfa']['backup_codes_count']
    )
    @auth_system.register_strategy(:mfa, mfa_strategy)
  end
  
  def authenticate_user(credentials)
    # Try different authentication strategies
    strategies = [:jwt, :oauth2, :mfa]
    
    strategies.each do |strategy|
      begin
        result = @auth_system.authenticate(strategy, credentials)
        return result if result.success?
      rescue => e
        Rails.logger.warn "Authentication strategy #{strategy} failed: #{e.message}"
      end
    end
    
    AuthenticationResult.failure("Invalid credentials")
  end
end
```

### 2. JWT Authentication Strategy
```ruby
require 'tusklang/auth'
require 'jwt'

class JWTStrategy
  include TuskLang::Auth::Strategy
  
  def initialize(secret:, algorithm: 'HS256', expiration: 24.hours)
    @secret = secret
    @algorithm = algorithm
    @expiration = expiration
  end
  
  def authenticate(credentials)
    return AuthenticationResult.failure("Missing token") unless credentials[:token]
    
    begin
      # Decode and verify JWT
      decoded_token = JWT.decode(
        credentials[:token],
        @secret,
        true,
        { algorithm: @algorithm }
      )
      
      payload = decoded_token[0]
      user_id = payload['user_id']
      
      # Find user
      user = User.find(user_id)
      return AuthenticationResult.failure("User not found") unless user
      
      # Check if token is expired
      if payload['exp'] && Time.at(payload['exp']) < Time.now
        return AuthenticationResult.failure("Token expired")
      end
      
      # Check if user is active
      return AuthenticationResult.failure("User account disabled") unless user.active?
      
      AuthenticationResult.success(user, { token: credentials[:token] })
    rescue JWT::DecodeError => e
      AuthenticationResult.failure("Invalid token: #{e.message}")
    rescue => e
      AuthenticationResult.failure("Authentication error: #{e.message}")
    end
  end
  
  def generate_token(user)
    payload = {
      user_id: user.id,
      email: user.email,
      role: user.role,
      exp: (Time.now + @expiration).to_i,
      iat: Time.now.to_i
    }
    
    JWT.encode(payload, @secret, @algorithm)
  end
  
  def refresh_token(token)
    begin
      decoded_token = JWT.decode(token, @secret, false, { algorithm: @algorithm })
      payload = decoded_token[0]
      
      # Create new token with extended expiration
      new_payload = payload.merge(
        exp: (Time.now + @expiration).to_i,
        iat: Time.now.to_i
      )
      
      JWT.encode(new_payload, @secret, @algorithm)
    rescue JWT::DecodeError
      nil
    end
  end
end
```

### 3. OAuth2 Authentication Strategy
```ruby
require 'tusklang/auth'
require 'oauth2'

class OAuth2Strategy
  include TuskLang::Auth::Strategy
  
  def initialize(providers:)
    @providers = providers
    @clients = {}
    setup_clients
  end
  
  def authenticate(credentials)
    provider = credentials[:provider]
    code = credentials[:code]
    
    return AuthenticationResult.failure("Missing provider or code") unless provider && code
    
    provider_config = @providers[provider.to_sym]
    return AuthenticationResult.failure("Unknown provider") unless provider_config
    
    begin
      # Exchange code for access token
      client = @clients[provider]
      token = client.auth_code.get_token(code, redirect_uri: provider_config['redirect_uri'])
      
      # Get user info from provider
      user_info = get_user_info(provider, token)
      
      # Find or create user
      user = find_or_create_user(provider, user_info)
      
      # Generate JWT token
      jwt_strategy = TuskLang::Auth::Strategies::JWTStrategy.new(
        secret: ENV['JWT_SECRET'],
        algorithm: 'HS256'
      )
      token = jwt_strategy.generate_token(user)
      
      AuthenticationResult.success(user, { token: token, provider: provider })
    rescue OAuth2::Error => e
      AuthenticationResult.failure("OAuth2 error: #{e.message}")
    rescue => e
      AuthenticationResult.failure("Authentication error: #{e.message}")
    end
  end
  
  private
  
  def setup_clients
    @providers.each do |provider, config|
      @clients[provider] = OAuth2::Client.new(
        config['client_id'],
        config['client_secret'],
        authorize_url: get_authorize_url(provider),
        token_url: get_token_url(provider)
      )
    end
  end
  
  def get_authorize_url(provider)
    case provider
    when :google
      'https://accounts.google.com/o/oauth2/auth'
    when :github
      'https://github.com/login/oauth/authorize'
    else
      raise "Unknown provider: #{provider}"
    end
  end
  
  def get_token_url(provider)
    case provider
    when :google
      'https://oauth2.googleapis.com/token'
    when :github
      'https://github.com/login/oauth/access_token'
    else
      raise "Unknown provider: #{provider}"
    end
  end
  
  def get_user_info(provider, token)
    case provider
    when :google
      response = token.get('https://www.googleapis.com/oauth2/v2/userinfo')
      JSON.parse(response.body)
    when :github
      response = token.get('https://api.github.com/user')
      JSON.parse(response.body)
    else
      raise "Unknown provider: #{provider}"
    end
  end
  
  def find_or_create_user(provider, user_info)
    # Find existing user by provider ID
    user = User.find_by(provider: provider.to_s, provider_id: user_info['id'])
    
    unless user
      # Create new user
      user = User.create!(
        email: user_info['email'],
        name: user_info['name'],
        provider: provider.to_s,
        provider_id: user_info['id'],
        avatar_url: user_info['picture'] || user_info['avatar_url']
      )
    end
    
    user
  end
end
```

### 4. Multi-Factor Authentication Strategy
```ruby
require 'tusklang/auth'
require 'rotp'
require 'securerandom'

class MFAStrategy
  include TuskLang::Auth::Strategy
  
  def initialize(enabled: true, methods: ['totp'], backup_codes_count: 10)
    @enabled = enabled
    @methods = methods
    @backup_codes_count = backup_codes_count
  end
  
  def authenticate(credentials)
    user = credentials[:user]
    mfa_code = credentials[:mfa_code]
    mfa_method = credentials[:mfa_method] || 'totp'
    
    return AuthenticationResult.failure("MFA not enabled") unless @enabled
    return AuthenticationResult.failure("User not found") unless user
    return AuthenticationResult.failure("MFA code required") unless mfa_code
    
    case mfa_method
    when 'totp'
      authenticate_totp(user, mfa_code)
    when 'sms'
      authenticate_sms(user, mfa_code)
    when 'email'
      authenticate_email(user, mfa_code)
    when 'backup'
      authenticate_backup_code(user, mfa_code)
    else
      AuthenticationResult.failure("Unknown MFA method")
    end
  end
  
  def setup_mfa(user)
    return { error: "MFA not enabled" } unless @enabled
    
    # Generate TOTP secret
    totp_secret = ROTP::Base32.random
    
    # Generate backup codes
    backup_codes = generate_backup_codes
    
    # Save to user
    user.update!(
      totp_secret: totp_secret,
      backup_codes: backup_codes,
      mfa_enabled: true
    )
    
    # Generate QR code for TOTP
    totp = ROTP::TOTP.new(totp_secret, issuer: 'MyApp')
    qr_code = totp.provisioning_uri(user.email, name: user.name)
    
    {
      totp_secret: totp_secret,
      qr_code: qr_code,
      backup_codes: backup_codes
    }
  end
  
  private
  
  def authenticate_totp(user, code)
    return AuthenticationResult.failure("TOTP not set up") unless user.totp_secret
    
    totp = ROTP::TOTP.new(user.totp_secret)
    
    if totp.verify(code, drift_behind: 30)
      AuthenticationResult.success(user, { mfa_method: 'totp' })
    else
      AuthenticationResult.failure("Invalid TOTP code")
    end
  end
  
  def authenticate_sms(user, code)
    # Check if SMS code is valid and not expired
    sms_code = user.sms_codes.find_by(
      code: code,
      expires_at: Time.now..,
      used: false
    )
    
    if sms_code
      sms_code.update!(used: true)
      AuthenticationResult.success(user, { mfa_method: 'sms' })
    else
      AuthenticationResult.failure("Invalid or expired SMS code")
    end
  end
  
  def authenticate_email(user, code)
    # Check if email code is valid and not expired
    email_code = user.email_codes.find_by(
      code: code,
      expires_at: Time.now..,
      used: false
    )
    
    if email_code
      email_code.update!(used: true)
      AuthenticationResult.success(user, { mfa_method: 'email' })
    else
      AuthenticationResult.failure("Invalid or expired email code")
    end
  end
  
  def authenticate_backup_code(user, code)
    backup_codes = user.backup_codes || []
    
    if backup_codes.include?(code)
      # Remove used backup code
      user.update!(backup_codes: backup_codes - [code])
      AuthenticationResult.success(user, { mfa_method: 'backup' })
    else
      AuthenticationResult.failure("Invalid backup code")
    end
  end
  
  def generate_backup_codes
    Array.new(@backup_codes_count) { SecureRandom.hex(4).upcase }
  end
end
```

### 5. Password Policy Enforcement
```ruby
require 'tusklang/auth'

class PasswordPolicy
  def initialize(policy_config)
    @min_length = policy_config['min_length'] || 8
    @require_uppercase = policy_config['require_uppercase'] || false
    @require_lowercase = policy_config['require_lowercase'] || false
    @require_numbers = policy_config['require_numbers'] || false
    @require_special = policy_config['require_special'] || false
    @prevent_common = policy_config['prevent_common'] || false
  end
  
  def validate(password)
    errors = []
    
    # Check minimum length
    if password.length < @min_length
      errors << "Password must be at least #{@min_length} characters long"
    end
    
    # Check for uppercase letters
    if @require_uppercase && !password.match?(/[A-Z]/)
      errors << "Password must contain at least one uppercase letter"
    end
    
    # Check for lowercase letters
    if @require_lowercase && !password.match?(/[a-z]/)
      errors << "Password must contain at least one lowercase letter"
    end
    
    # Check for numbers
    if @require_numbers && !password.match?(/\d/)
      errors << "Password must contain at least one number"
    end
    
    # Check for special characters
    if @require_special && !password.match?(/[!@#$%^&*(),.?":{}|<>]/)
      errors << "Password must contain at least one special character"
    end
    
    # Check against common passwords
    if @prevent_common && common_password?(password)
      errors << "Password is too common, please choose a stronger password"
    end
    
    { valid: errors.empty?, errors: errors }
  end
  
  private
  
  def common_password?(password)
    common_passwords = [
      'password', '123456', '123456789', 'qwerty', 'abc123',
      'password123', 'admin', 'letmein', 'welcome', 'monkey'
    ]
    
    common_passwords.include?(password.downcase)
  end
end
```

### 6. Rate Limiting and Security
```ruby
require 'tusklang/auth'

class SecurityManager
  def initialize(config)
    @config = config
    @rate_limiter = TuskLang::RateLimiter.new
    @failed_attempts = {}
  end
  
  def check_rate_limit(identifier, action)
    key = "#{identifier}:#{action}"
    
    # Check if account is locked
    if account_locked?(identifier)
      return { allowed: false, reason: "Account temporarily locked" }
    end
    
    # Check rate limit
    if @rate_limiter.exceeded?(key, @config['rate_limiting']['login_attempts'])
      lock_account(identifier)
      return { allowed: false, reason: "Too many attempts, account locked" }
    end
    
    { allowed: true }
  end
  
  def record_failed_attempt(identifier)
    key = "#{identifier}:failed_attempts"
    @failed_attempts[key] ||= 0
    @failed_attempts[key] += 1
    
    # Check if we should lock the account
    if @failed_attempts[key] >= @config['rate_limiting']['login_attempts']
      lock_account(identifier)
    end
  end
  
  def record_successful_attempt(identifier)
    key = "#{identifier}:failed_attempts"
    @failed_attempts[key] = 0
    
    # Unlock account if it was locked
    unlock_account(identifier)
  end
  
  private
  
  def account_locked?(identifier)
    lock_key = "#{identifier}:locked"
    locked_until = @rate_limiter.get(lock_key)
    
    return false unless locked_until
    return false if Time.now > locked_until
    
    true
  end
  
  def lock_account(identifier)
    lock_key = "#{identifier}:locked"
    lockout_duration = parse_duration(@config['rate_limiting']['lockout_duration'])
    locked_until = Time.now + lockout_duration
    
    @rate_limiter.set(lock_key, locked_until, lockout_duration)
  end
  
  def unlock_account(identifier)
    lock_key = "#{identifier}:locked"
    @rate_limiter.delete(lock_key)
  end
  
  def parse_duration(duration_string)
    case duration_string
    when /(\d+)m/
      $1.to_i * 60
    when /(\d+)h/
      $1.to_i * 3600
    else
      900 # Default 15 minutes
    end
  end
end
```

## 🔧 Advanced Configuration

### Session Management
```ruby
require 'tusklang/auth'

class SessionManager
  def initialize(config)
    @config = config
    @sessions = {}
  end
  
  def create_session(user, strategy = 'jwt')
    session_id = SecureRandom.uuid
    session_data = {
      user_id: user.id,
      strategy: strategy,
      created_at: Time.now,
      expires_at: Time.now + parse_duration(@config['session_timeout']),
      ip_address: request.remote_ip,
      user_agent: request.user_agent
    }
    
    @sessions[session_id] = session_data
    
    # Store in database for persistence
    Session.create!(
      session_id: session_id,
      user: user,
      data: session_data,
      expires_at: session_data[:expires_at]
    )
    
    session_id
  end
  
  def validate_session(session_id)
    session = @sessions[session_id]
    return nil unless session
    
    # Check if session is expired
    if session[:expires_at] < Time.now
      delete_session(session_id)
      return nil
    end
    
    # Extend session if needed
    extend_session(session_id)
    
    session
  end
  
  def delete_session(session_id)
    @sessions.delete(session_id)
    
    # Remove from database
    Session.where(session_id: session_id).destroy_all
  end
  
  private
  
  def extend_session(session_id)
    session = @sessions[session_id]
    return unless session
    
    session[:expires_at] = Time.now + parse_duration(@config['session_timeout'])
    
    # Update database
    Session.where(session_id: session_id).update_all(
      expires_at: session[:expires_at]
    )
  end
end
```

### Audit Logging
```ruby
require 'tusklang/auth'

class AuditLogger
  def initialize
    @config = TuskLang.parse_file('config/authentication.tsk')
  end
  
  def log_auth_event(user, event_type, details = {})
    audit_entry = {
      user_id: user&.id,
      event_type: event_type,
      timestamp: Time.now,
      ip_address: request.remote_ip,
      user_agent: request.user_agent,
      details: details
    }
    
    # Store in database
    AuditLog.create!(audit_entry)
    
    # Send to external logging service if configured
    if @config['audit']['external_logging']
      send_to_external_logger(audit_entry)
    end
  end
  
  def log_login_success(user, strategy)
    log_auth_event(user, 'login_success', {
      strategy: strategy,
      session_id: session[:session_id]
    })
  end
  
  def log_login_failure(email, reason)
    log_auth_event(nil, 'login_failure', {
      email: email,
      reason: reason
    })
  end
  
  def log_logout(user)
    log_auth_event(user, 'logout', {
      session_id: session[:session_id]
    })
  end
  
  def log_password_change(user)
    log_auth_event(user, 'password_change')
  end
  
  def log_mfa_setup(user, method)
    log_auth_event(user, 'mfa_setup', {
      method: method
    })
  end
  
  private
  
  def send_to_external_logger(audit_entry)
    # Implementation for external logging service
    # e.g., Splunk, ELK, etc.
  end
end
```

## 🚀 Performance Optimization

### Authentication Caching
```ruby
require 'tusklang/auth'

class AuthenticationCache
  def initialize
    @cache = TuskLang::Cache::RedisCache.new
    @config = TuskLang.parse_file('config/authentication.tsk')
  end
  
  def cache_user_session(user_id, session_data)
    cache_key = "user_session:#{user_id}"
    ttl = parse_duration(@config['session_timeout'])
    
    @cache.set(cache_key, session_data, ttl)
  end
  
  def get_cached_session(user_id)
    cache_key = "user_session:#{user_id}"
    @cache.get(cache_key)
  end
  
  def invalidate_user_cache(user_id)
    cache_key = "user_session:#{user_id}"
    @cache.delete(cache_key)
  end
  
  def cache_user_permissions(user_id, permissions)
    cache_key = "user_permissions:#{user_id}"
    ttl = 1.hour
    
    @cache.set(cache_key, permissions, ttl)
  end
  
  def get_cached_permissions(user_id)
    cache_key = "user_permissions:#{user_id}"
    @cache.get(cache_key)
  end
end
```

## 🔒 Security Best Practices

### Secure Password Hashing
```ruby
require 'bcrypt'

class PasswordHasher
  def self.hash_password(password)
    BCrypt::Password.create(password, cost: 12)
  end
  
  def self.verify_password(password, hash)
    BCrypt::Password.new(hash) == password
  end
  
  def self.needs_rehash?(hash)
    BCrypt::Password.new(hash).cost < 12
  end
end
```

### CSRF Protection
```ruby
require 'tusklang/auth'

class CSRFProtection
  def initialize
    @config = TuskLang.parse_file('config/authentication.tsk')
  end
  
  def generate_token
    SecureRandom.hex(32)
  end
  
  def verify_token(token, stored_token)
    return false unless token && stored_token
    
    # Use constant-time comparison
    Rack::Utils.secure_compare(token, stored_token)
  end
  
  def validate_csrf_token(request)
    token = request.headers['X-CSRF-Token'] || request.params['csrf_token']
    stored_token = session[:csrf_token]
    
    verify_token(token, stored_token)
  end
end
```

## 📊 Monitoring and Analytics

### Authentication Analytics
```ruby
require 'tusklang/auth'

class AuthenticationAnalytics
  def initialize
    @metrics = TuskLang::Metrics::Collector.new
  end
  
  def track_login_attempt(strategy, success)
    @metrics.increment("auth.login_attempts.#{strategy}")
    @metrics.increment("auth.login_attempts.#{strategy}.#{success ? 'success' : 'failure'}")
  end
  
  def track_mfa_usage(method)
    @metrics.increment("auth.mfa_usage.#{method}")
  end
  
  def track_session_duration(duration)
    @metrics.histogram("auth.session_duration", duration)
  end
  
  def get_auth_stats
    {
      total_logins: @metrics.get("auth.login_attempts.total"),
      success_rate: @metrics.get_success_rate("auth.login_attempts"),
      popular_strategies: @metrics.get_top("auth.login_attempts", 5),
      mfa_adoption: @metrics.get("auth.mfa_usage.total")
    }
  end
end
```

This comprehensive authentication system provides enterprise-grade security features while maintaining the flexibility and power of TuskLang. The combination of multiple authentication strategies, MFA support, and advanced security features creates a robust foundation for protecting Ruby applications. 
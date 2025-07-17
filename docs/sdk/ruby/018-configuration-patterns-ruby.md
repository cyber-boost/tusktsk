# 🏗️ TuskLang Ruby Configuration Patterns Guide

**"We don't bow to any king" - Ruby Edition**

Design maintainable, scalable configs for Ruby apps. Use modular patterns, environment overrides, and DRY principles with TuskLang.

## 🧩 Modular Configs

### 1. Split Configs by Domain
```ruby
# config/app.tsk
@include("config/database.tsk")
@include("config/server.tsk")
@include("config/cache.tsk")
@include("config/security.tsk")
```

### 2. Use Feature-Specific Files
```ruby
# config/features/users.tsk
[users]
registration_enabled: true
email_verification: true
```

# config/features/payments.tsk
[payments]
enabled: true
provider: "stripe"
```

## 🌱 Environment Overrides

### 1. Environment-Specific Files
```ruby
# config/environments/development.tsk
$environment: "development"
[logging]
level: "debug"
```

# config/environments/production.tsk
$environment: "production"
[logging]
level: "error"
```

### 2. Conditional Includes
```ruby
# config/app.tsk
$environment: @env("RAILS_ENV", "development")
@if($environment == "production") {
  @include("config/environments/production.tsk")
} @else {
  @include("config/environments/development.tsk")
}
```

## 🔁 DRY Principles

### 1. Global Variables
```ruby
# config/app.tsk
$app_name: "MyApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080
name: $app_name
```

### 2. Reusable Snippets
```ruby
# config/snippets.tsk
[defaults]
cache_ttl: "5m"
rate_limit: 1000

# config/app.tsk
@include("config/snippets.tsk")
[cache]
ttl: @defaults.cache_ttl
[rate_limit]
value: @defaults.rate_limit
```

## 🛡️ Best Practices
- Organize configs by domain and environment.
- Use global variables and snippets for DRY configs.
- Validate all includes and overrides.
- Document config structure for your team.

**Ready to architect world-class configs? Let's Tusk! 🚀** 
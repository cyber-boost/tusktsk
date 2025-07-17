# 🏗️ TuskLang Ruby Microservices Guide

**"We don't bow to any king" - Ruby Edition**

Build scalable microservices with TuskLang. Learn service discovery, config sharing, and distributed configuration patterns for Ruby microservices.

## 🔍 Service Discovery

### 1. Service Registry Config
```ruby
# config/services.tsk
[service_registry]
host: @env("SERVICE_REGISTRY_HOST", "localhost")
port: @env("SERVICE_REGISTRY_PORT", 8500)

[services]
user_service: {
    host: @env("USER_SERVICE_HOST", "localhost")
    port: @env("USER_SERVICE_PORT", 3001)
    health_check: "/health"
}
order_service: {
    host: @env("ORDER_SERVICE_HOST", "localhost")
    port: @env("ORDER_SERVICE_PORT", 3002)
    health_check: "/health"
}
payment_service: {
    host: @env("PAYMENT_SERVICE_HOST", "localhost")
    port: @env("PAYMENT_SERVICE_PORT", 3003)
    health_check: "/health"
}
```

### 2. Service Discovery Client
```ruby
# app/services/service_discovery.rb
require 'tusklang'

class ServiceDiscovery
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/services.tsk')
  end

  def self.get_service_url(service_name)
    config = load_config
    service = config['services'][service_name]
    "http://#{service['host']}:#{service['port']}"
  end
end
```

## 🔗 Config Sharing

### 1. Shared Config Repository
```ruby
# config/shared.tsk
[shared]
database: {
    host: @env("SHARED_DB_HOST", "localhost")
    port: @env("SHARED_DB_PORT", 5432)
    name: @env("SHARED_DB_NAME", "shared")
}
redis: {
    host: @env("SHARED_REDIS_HOST", "localhost")
    port: @env("SHARED_REDIS_PORT", 6379)
}
```

### 2. Service-Specific Overrides
```ruby
# config/user_service.tsk
@include("config/shared.tsk")

[user_service]
database: {
    name: "users"
    pool_size: 10
}
features: {
    registration_enabled: true
    email_verification: true
}
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/microservice_config.rb
require 'tusklang'

class MicroserviceConfig
  def self.load_service_config(service_name)
    parser = TuskLang.new
    parser.parse_file("config/#{service_name}.tsk")
  end

  def self.get_shared_config
    parser = TuskLang.new
    parser.parse_file('config/shared.tsk')
  end
end

# Usage in a microservice
config = MicroserviceConfig.load_service_config('user_service')
shared_config = MicroserviceConfig.get_shared_config
```

## 🛡️ Best Practices
- Use service discovery for dynamic service locations.
- Share common configs across services.
- Use environment variables for service-specific settings.
- Monitor service health and config changes.

**Ready to scale with microservices? Let's Tusk! 🚀** 
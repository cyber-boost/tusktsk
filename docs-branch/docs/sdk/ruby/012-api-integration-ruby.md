# 🔗 TuskLang Ruby API Integration Guide

**"We don't bow to any king" - Ruby Edition**

Integrate TuskLang with REST, GraphQL, and webhook APIs in Ruby. Make your configs dynamic, connected, and ready for the modern web.

## 🌐 REST API Integration

### 1. HTTP Requests in Config
```ruby
# config/api.tsk
[external]
user_data: @http("GET", "https://api.example.com/users/1")
weather: @http("GET", "https://api.weather.com/v1/current?city=Boston")
```

### 2. Ruby Usage
```ruby
# app/services/api_service.rb
require 'tusklang'

class ApiService
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/api.tsk')
  end
end

config = ApiService.load_config
puts "User Data: #{config['external']['user_data']}"
puts "Weather: #{config['external']['weather']}"
```

## 🕸️ GraphQL API Integration

### 1. GraphQL Queries in Config
```ruby
# config/graphql.tsk
[graphql]
user_query: @http("POST", "https://api.example.com/graphql", {
  query: "{ user(id: 1) { id name email } }"
})
```

### 2. Ruby Usage
```ruby
# app/services/graphql_service.rb
require 'tusklang'

class GraphqlService
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/graphql.tsk')
  end
end

config = GraphqlService.load_config
puts "User Query Result: #{config['graphql']['user_query']}"
```

## 📣 Webhook Integration

### 1. Webhook Endpoints in Config
```ruby
# config/webhooks.tsk
[webhooks]
order_created: @http("POST", "https://hooks.example.com/order", {
  order_id: @request.order_id,
  amount: @request.amount
})
user_signed_up: @http("POST", "https://hooks.example.com/signup", {
  user_id: @request.user_id,
  email: @request.email
})
```

### 2. Ruby Usage
```ruby
# app/services/webhook_service.rb
require 'tusklang'

class WebhookService
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/webhooks.tsk')
  end
end

config = WebhookService.load_config
puts "Order Webhook: #{config['webhooks']['order_created']}"
puts "Signup Webhook: #{config['webhooks']['user_signed_up']}"
```

## 🛡️ Best Practices
- Use @env.secure for API keys and secrets.
- Validate all incoming and outgoing data.
- Handle API errors gracefully in Ruby code.
- Use @cache for frequently accessed API data.

## 🚨 Troubleshooting
- For HTTP errors, check endpoint URLs and credentials.
- For GraphQL, verify query syntax and API schema.
- For webhooks, ensure endpoints are reachable and accept POST requests.

**Ready to connect your Ruby app to the world? Let's Tusk! 🚀** 
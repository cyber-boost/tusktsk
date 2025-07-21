# 🌐 TuskLang Bash @http Function Guide

**"We don't bow to any king" – HTTP is your configuration's network bridge.**

The @http function in TuskLang is your web communication powerhouse, enabling dynamic HTTP requests, API integrations, and web service interactions directly within your configuration files. Whether you're fetching external data, calling REST APIs, or monitoring web services, @http provides the connectivity and flexibility to make your configurations truly networked.

## 🎯 What is @http?
The @http function provides HTTP client capabilities in TuskLang. It offers:
- **HTTP methods** - GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS
- **Request customization** - Headers, body, authentication, timeouts
- **Response handling** - Status codes, headers, body parsing
- **Error handling** - Network errors, timeouts, HTTP errors
- **Caching integration** - Automatic caching with @cache

## 📝 Basic @http Syntax

### Simple GET Requests
```ini
[simple_get]
# Basic GET request
weather_data: @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London")
github_user: @http("GET", "https://api.github.com/users/octocat")
public_ip: @http("GET", "https://httpbin.org/ip")
```

### POST Requests with Data
```ini
[post_requests]
# POST with JSON data
$post_data: {"name": "John", "email": "john@example.com"}
create_user: @http("POST", "https://api.example.com/users", {
    "headers": {"Content-Type": "application/json"},
    "body": $post_data
})

# POST with form data
form_data: @http("POST", "https://httpbin.org/post", {
    "headers": {"Content-Type": "application/x-www-form-urlencoded"},
    "body": "name=John&email=john@example.com"
})
```

### Authenticated Requests
```ini
[authenticated_requests]
# Bearer token authentication
$api_token: @env("API_TOKEN")
authenticated_request: @http("GET", "https://api.example.com/protected", {
    "headers": {
        "Authorization": @string.format("Bearer {token}", {"token": $api_token}),
        "Content-Type": "application/json"
    }
})

# Basic authentication
basic_auth: @http("GET", "https://api.example.com/data", {
    "headers": {"Authorization": "Basic " + @string.base64_encode("user:pass")}
})
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > http-quickstart.tsk << 'EOF'
[basic_requests]
# Basic HTTP requests
public_ip: @http("GET", "https://httpbin.org/ip")
user_agent: @http("GET", "https://httpbin.org/user-agent")
headers: @http("GET", "https://httpbin.org/headers")

[api_requests]
# API requests with data
$post_data: {"name": "TuskLang", "version": "2.1.0"}
post_response: @http("POST", "https://httpbin.org/post", {
    "headers": {"Content-Type": "application/json"},
    "body": $post_data
})

[authenticated_requests]
# Authenticated request
$api_key: @env("API_KEY", "demo-key")
auth_request: @http("GET", "https://httpbin.org/bearer", {
    "headers": {
        "Authorization": @string.format("Bearer {key}", {"key": $api_key})
    }
})

[error_handling]
# Request with error handling
valid_request: @http("GET", "https://httpbin.org/status/200")
error_request: @http("GET", "https://httpbin.org/status/404")
timeout_request: @http("GET", "https://httpbin.org/delay/10", {"timeout": 5})
EOF

config=$(tusk_parse http-quickstart.tsk)

echo "=== Basic Requests ==="
echo "Public IP: $(tusk_get "$config" basic_requests.public_ip)"
echo "User Agent: $(tusk_get "$config" basic_requests.user_agent)"
echo "Headers: $(tusk_get "$config" basic_requests.headers)"

echo ""
echo "=== API Requests ==="
echo "POST Response: $(tusk_get "$config" api_requests.post_response)"

echo ""
echo "=== Authenticated Requests ==="
echo "Auth Request: $(tusk_get "$config" authenticated_requests.auth_request)"

echo ""
echo "=== Error Handling ==="
echo "Valid Request: $(tusk_get "$config" error_handling.valid_request)"
echo "Error Request: $(tusk_get "$config" error_handling.error_request)"
echo "Timeout Request: $(tusk_get "$config" error_handling.timeout_request)"
```

## 🔗 Real-World Use Cases

### 1. External API Integration
```ini
[api_integration]
# Weather API integration
$weather_api_key: @env("WEATHER_API_KEY")
$city: @env("DEFAULT_CITY", "London")

weather_data: @http("GET", @string.format("https://api.weatherapi.com/v1/current.json?key={key}&q={city}", {
    "key": $weather_api_key,
    "city": $city
}))

# Currency exchange API
currency_rates: @http("GET", "https://api.exchangerate-api.com/v4/latest/USD")

# GitHub API integration
$github_token: @env("GITHUB_TOKEN")
github_user: @http("GET", "https://api.github.com/user", {
    "headers": {
        "Authorization": @string.format("Bearer {token}", {"token": $github_token}),
        "Accept": "application/vnd.github.v3+json"
    }
})

github_repos: @http("GET", "https://api.github.com/user/repos", {
    "headers": {
        "Authorization": @string.format("Bearer {token}", {"token": $github_token}),
        "Accept": "application/vnd.github.v3+json"
    }
})
```

### 2. Web Service Monitoring
```ini
[service_monitoring]
# Health check endpoints
$services: [
    {"name": "web", "url": "https://web.example.com/health"},
    {"name": "api", "url": "https://api.example.com/health"},
    {"name": "database", "url": "https://db.example.com/health"}
]

# Monitor all services
service_status: @array.map($services, {
    "name": item.name,
    "url": item.url,
    "status": @http("GET", item.url, {"timeout": 10}),
    "timestamp": @date("Y-m-d H:i:s")
})

# Check for failed services
failed_services: @array.filter($service_status, "item.status.status_code >= 400")
healthy_services: @array.filter($service_status, "item.status.status_code < 400")

# Overall health status
overall_healthy: @array.length($healthy_services) == @array.length($services)
```

### 3. Data Synchronization
```ini
[data_sync]
# Sync data with external service
$sync_data: {
    "users": @query("SELECT id, name, email FROM users WHERE updated_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)"),
    "timestamp": @date("Y-m-d H:i:s")
}

# Send data to external API
$api_endpoint: @env("SYNC_API_ENDPOINT")
$api_key: @env("SYNC_API_KEY")

sync_response: @http("POST", $api_endpoint, {
    "headers": {
        "Authorization": @string.format("Bearer {key}", {"key": $api_key}),
        "Content-Type": "application/json"
    },
    "body": $sync_data
})

# Handle sync response
sync_success: @validate.range($sync_response.status_code, 200, 299)
sync_errors: @if($sync_success, [], $sync_response.body.errors)
```

### 4. Configuration Management via API
```ini
[config_api]
# Fetch configuration from remote API
$config_api_url: @env("CONFIG_API_URL")
$config_api_token: @env("CONFIG_API_TOKEN")

remote_config: @http("GET", @string.format("{url}/config/{env}", {
    "url": $config_api_url,
    "env": @env("APP_ENV", "development")
}), {
    "headers": {
        "Authorization": @string.format("Bearer {token}", {"token": $config_api_token}),
        "Accept": "application/json"
    }
})

# Update configuration via API
$new_config: {
    "database": {
        "host": @env("DB_HOST"),
        "port": @env("DB_PORT")
    },
    "api": {
        "timeout": @env("API_TIMEOUT", "30")
    }
}

config_update: @http("PUT", @string.format("{url}/config/{env}", {
    "url": $config_api_url,
    "env": @env("APP_ENV", "development")
}), {
    "headers": {
        "Authorization": @string.format("Bearer {token}", {"token": $config_api_token}),
        "Content-Type": "application/json"
    },
    "body": $new_config
})
```

## 🧠 Advanced @http Patterns

### Request Retry Logic
```ini
[retry_logic]
# Implement retry logic for failed requests
$retry_config: {
    "max_retries": 3,
    "retry_delay": 1,
    "backoff_multiplier": 2
}

$api_url: "https://api.example.com/data"
$api_key: @env("API_KEY")

# Retry function
retry_request: @http.retry("GET", $api_url, {
    "headers": {
        "Authorization": @string.format("Bearer {key}", {"key": $api_key})
    },
    "max_retries": $retry_config.max_retries,
    "retry_delay": $retry_config.retry_delay,
    "backoff_multiplier": $retry_config.backoff_multiplier
})
```

### Batch API Requests
```ini
[batch_requests]
# Process multiple API requests in parallel
$endpoints: [
    "https://api.example.com/users",
    "https://api.example.com/posts",
    "https://api.example.com/comments"
]

$api_key: @env("API_KEY")

# Batch request with common headers
batch_responses: @array.map($endpoints, @http("GET", item, {
    "headers": {
        "Authorization": @string.format("Bearer {key}", {"key": $api_key}),
        "Accept": "application/json"
    }
}))

# Process batch results
successful_requests: @array.filter($batch_responses, "item.status_code >= 200 && item.status_code < 300")
failed_requests: @array.filter($batch_responses, "item.status_code >= 400")
```

### Webhook Integration
```ini
[webhook_integration]
# Send webhook notifications
$webhook_url: @env("WEBHOOK_URL")
$webhook_secret: @env("WEBHOOK_SECRET")

$webhook_data: {
    "event": "user.created",
    "timestamp": @date("Y-m-d H:i:s"),
    "data": {
        "user_id": @env("USER_ID"),
        "email": @env("USER_EMAIL")
    }
}

# Calculate webhook signature
$payload: @string.json_encode($webhook_data)
$signature: @string.hmac_sha256($payload, $webhook_secret)

webhook_response: @http("POST", $webhook_url, {
    "headers": {
        "Content-Type": "application/json",
        "X-Webhook-Signature": $signature
    },
    "body": $webhook_data
})
```

## 🛡️ Security & Performance Notes
- **HTTPS only:** Always use HTTPS for production requests
- **API key security:** Store API keys securely and never log them
- **Request timeouts:** Set appropriate timeouts to prevent hanging requests
- **Rate limiting:** Respect API rate limits and implement backoff
- **Input validation:** Validate all data before sending in requests
- **Error handling:** Handle network errors and HTTP error responses gracefully

## 🐞 Troubleshooting
- **Network errors:** Check connectivity and DNS resolution
- **Authentication errors:** Verify API keys and authentication headers
- **Timeout issues:** Adjust timeout values for slow APIs
- **Rate limiting:** Implement exponential backoff for rate-limited APIs
- **SSL/TLS issues:** Check certificate validity and SSL configuration

## 💡 Best Practices
- **Use HTTPS:** Always use HTTPS for secure communication
- **Set timeouts:** Configure appropriate request timeouts
- **Handle errors:** Implement proper error handling for failed requests
- **Cache responses:** Use @cache for frequently requested data
- **Validate responses:** Validate API responses before using data
- **Monitor usage:** Track API usage and implement rate limiting

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@cache Function](033-at-cache-function-bash.md)
- [@string Function](030-at-string-function-bash.md)
- [Error Handling](062-error-handling-bash.md)
- [API Integration](077-api-integration-bash.md)

---

**Master @http in TuskLang and connect your configurations to the world wide web. 🌐** 
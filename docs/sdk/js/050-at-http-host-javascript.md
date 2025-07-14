# @http Host - JavaScript

## Overview

The `@http` host function in TuskLang provides comprehensive HTTP client capabilities that allow your configuration to make HTTP requests, interact with APIs, and fetch external data. This is essential for JavaScript applications that need to integrate with external services, APIs, or web resources.

## Basic Syntax

```tsk
# Simple HTTP GET request
api_response: @http("GET", "https://api.example.com/users")
weather_data: @http("GET", "https://api.weatherapi.com/v1/current.json?key=$api_key&q=London")

# HTTP POST request with data
user_creation: @http("POST", "https://api.example.com/users", {"name": "John Doe", "email": "john@example.com"})
data_submission: @http("POST", "https://api.example.com/data", $form_data, {"headers": {"Content-Type": "application/json"}})
```

## JavaScript Integration

### Node.js HTTP Integration

```javascript
const tusk = require('tusklang');

// Load configuration with HTTP requests
const config = tusk.load('http.tsk');

// Access HTTP response data
console.log(config.api_response); // Full response object
console.log(config.weather_data); // Weather data object

// Use HTTP data in application
const userService = {
  async getUsers() {
    return await tusk.http("GET", "https://api.example.com/users");
  },
  
  async createUser(userData) {
    return await tusk.http("POST", "https://api.example.com/users", userData, {
      headers: { "Content-Type": "application/json" }
    });
  },
  
  async updateUser(userId, userData) {
    return await tusk.http("PUT", `https://api.example.com/users/${userId}`, userData);
  }
};

// Dynamic HTTP requests
const response = await tusk.http("GET", `https://api.example.com/users/${userId}`);
const userData = response.data;
```

### Browser HTTP Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('http.tsk');

// Use HTTP data in frontend
class APIClient {
  constructor() {
    this.baseUrl = 'https://api.example.com';
    this.apiKey = config.api_key;
  }
  
  async fetchData(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const response = await tusk.http("GET", url, null, {
      headers: {
        "Authorization": `Bearer ${this.apiKey}`,
        ...options.headers
      }
    });
    
    return response.data;
  }
  
  async submitData(endpoint, data, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const response = await tusk.http("POST", url, data, {
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${this.apiKey}`,
        ...options.headers
      }
    });
    
    return response.data;
  }
}

// Usage
const apiClient = new APIClient();
const users = await apiClient.fetchData('/users');
const newUser = await apiClient.submitData('/users', { name: 'Jane Doe', email: 'jane@example.com' });
```

## Advanced Usage

### HTTP Methods

```tsk
# Different HTTP methods
get_users: @http("GET", "https://api.example.com/users")
create_user: @http("POST", "https://api.example.com/users", {"name": "John", "email": "john@example.com"})
update_user: @http("PUT", "https://api.example.com/users/123", {"name": "John Updated"})
delete_user: @http("DELETE", "https://api.example.com/users/123")
patch_user: @http("PATCH", "https://api.example.com/users/123", {"status": "active"})
```

### Headers and Authentication

```tsk
# HTTP requests with headers
authenticated_request: @http("GET", "https://api.example.com/protected", null, {
  "headers": {
    "Authorization": "Bearer $api_token",
    "Content-Type": "application/json",
    "Accept": "application/json"
  }
})

# API key authentication
weather_api: @http("GET", "https://api.weatherapi.com/v1/current.json?key=$weather_api_key&q=$city")
```

### Query Parameters and Dynamic URLs

```tsk
# Dynamic query parameters
user_search: @http("GET", "https://api.example.com/users?name=$search_term&limit=$limit")
paginated_data: @http("GET", "https://api.example.com/data?page=$page_number&size=$page_size")

# Dynamic URL construction
user_profile: @http("GET", "https://api.example.com/users/$user_id/profile")
user_posts: @http("GET", "https://api.example.com/users/$user_id/posts?limit=10")
```

## JavaScript Implementation

### Custom HTTP Client

```javascript
class TuskHTTPClient {
  constructor() {
    this.defaultHeaders = {
      'User-Agent': 'TuskLang-HTTP-Client/1.0',
      'Accept': 'application/json'
    };
    this.timeout = 30000;
    this.retries = 3;
  }
  
  async request(method, url, data = null, options = {}) {
    const requestOptions = {
      method: method.toUpperCase(),
      headers: {
        ...this.defaultHeaders,
        ...options.headers
      },
      timeout: options.timeout || this.timeout
    };
    
    // Add body for POST, PUT, PATCH requests
    if (data && ['POST', 'PUT', 'PATCH'].includes(requestOptions.method)) {
      if (typeof data === 'object') {
        requestOptions.body = JSON.stringify(data);
        requestOptions.headers['Content-Type'] = 'application/json';
      } else {
        requestOptions.body = data;
      }
    }
    
    // Add query parameters for GET requests
    if (requestOptions.method === 'GET' && data) {
      const queryParams = new URLSearchParams(data);
      url = `${url}${url.includes('?') ? '&' : '?'}${queryParams.toString()}`;
    }
    
    // Execute request with retries
    return await this.executeWithRetries(url, requestOptions, options.retries || this.retries);
  }
  
  async executeWithRetries(url, options, retries) {
    for (let attempt = 0; attempt <= retries; attempt++) {
      try {
        const response = await fetch(url, options);
        
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const contentType = response.headers.get('content-type');
        let data;
        
        if (contentType && contentType.includes('application/json')) {
          data = await response.json();
        } else {
          data = await response.text();
        }
        
        return {
          status: response.status,
          statusText: response.statusText,
          headers: Object.fromEntries(response.headers.entries()),
          data: data,
          url: response.url
        };
        
      } catch (error) {
        if (attempt === retries) {
          throw error;
        }
        
        // Wait before retry with exponential backoff
        await this.delay(Math.pow(2, attempt) * 1000);
      }
    }
  }
  
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
  
  // Convenience methods
  async get(url, options = {}) {
    return await this.request('GET', url, null, options);
  }
  
  async post(url, data, options = {}) {
    return await this.request('POST', url, data, options);
  }
  
  async put(url, data, options = {}) {
    return await this.request('PUT', url, data, options);
  }
  
  async delete(url, options = {}) {
    return await this.request('DELETE', url, null, options);
  }
  
  async patch(url, data, options = {}) {
    return await this.request('PATCH', url, data, options);
  }
}
```

### TypeScript Support

```typescript
interface HTTPOptions {
  headers?: Record<string, string>;
  timeout?: number;
  retries?: number;
  query?: Record<string, any>;
}

interface HTTPResponse<T = any> {
  status: number;
  statusText: string;
  headers: Record<string, string>;
  data: T;
  url: string;
}

class TypedHTTPClient {
  private defaultHeaders: Record<string, string>;
  private timeout: number;
  private retries: number;
  
  constructor() {
    this.defaultHeaders = {
      'User-Agent': 'TuskLang-HTTP-Client/1.0',
      'Accept': 'application/json'
    };
    this.timeout = 30000;
    this.retries = 3;
  }
  
  async request<T>(
    method: string,
    url: string,
    data?: any,
    options: HTTPOptions = {}
  ): Promise<HTTPResponse<T>> {
    // Implementation similar to JavaScript version
    return {} as HTTPResponse<T>;
  }
  
  async get<T>(url: string, options: HTTPOptions = {}): Promise<HTTPResponse<T>> {
    return await this.request<T>('GET', url, undefined, options);
  }
  
  async post<T>(url: string, data: any, options: HTTPOptions = {}): Promise<HTTPResponse<T>> {
    return await this.request<T>('POST', url, data, options);
  }
  
  async put<T>(url: string, data: any, options: HTTPOptions = {}): Promise<HTTPResponse<T>> {
    return await this.request<T>('PUT', url, data, options);
  }
  
  async delete<T>(url: string, options: HTTPOptions = {}): Promise<HTTPResponse<T>> {
    return await this.request<T>('DELETE', url, undefined, options);
  }
  
  async patch<T>(url: string, data: any, options: HTTPOptions = {}): Promise<HTTPResponse<T>> {
    return await this.request<T>('PATCH', url, data, options);
  }
}
```

## Real-World Examples

### API Integration

```tsk
# REST API integration
users_api: @http("GET", "https://jsonplaceholder.typicode.com/users")
create_user_api: @http("POST", "https://jsonplaceholder.typicode.com/users", {
  "name": "John Doe",
  "email": "john@example.com",
  "username": "johndoe"
})

# Weather API integration
weather_api: @http("GET", "https://api.openweathermap.org/data/2.5/weather?q=$city&appid=$api_key&units=metric")
```

### External Service Integration

```tsk
# Payment gateway integration
payment_processing: @http("POST", "https://api.stripe.com/v1/payment_intents", {
  "amount": $amount,
  "currency": "usd",
  "payment_method_types": ["card"]
}, {
  "headers": {
    "Authorization": "Bearer $stripe_secret_key",
    "Content-Type": "application/x-www-form-urlencoded"
  }
})

# Email service integration
send_email: @http("POST", "https://api.sendgrid.com/v3/mail/send", {
  "personalizations": [{"to": [{"email": $recipient_email}]}],
  "from": {"email": "noreply@example.com"},
  "subject": $email_subject,
  "content": [{"type": "text/plain", "value": $email_content}]
}, {
  "headers": {
    "Authorization": "Bearer $sendgrid_api_key",
    "Content-Type": "application/json"
  }
})
```

### Data Fetching

```tsk
# RSS feed fetching
rss_feed: @http("GET", "https://feeds.bbci.co.uk/news/rss.xml")
news_api: @http("GET", "https://newsapi.org/v2/top-headlines?country=us&apiKey=$news_api_key")

# File download
file_download: @http("GET", "https://example.com/files/$filename", null, {
  "headers": {"Accept": "application/octet-stream"}
})
```

## Performance Considerations

### HTTP Caching

```tsk
# Cache HTTP responses
cached_api_response: @cache("5m", @http("GET", "https://api.example.com/users"))
cached_weather_data: @cache("10m", @http("GET", "https://api.weatherapi.com/v1/current.json?key=$api_key&q=London"))
```

### Connection Pooling

```javascript
// Implement connection pooling for better performance
class HTTPConnectionPool {
  constructor(maxConnections = 10) {
    this.pool = [];
    this.maxConnections = maxConnections;
    this.activeConnections = 0;
  }
  
  async getConnection() {
    if (this.pool.length > 0) {
      return this.pool.pop();
    }
    
    if (this.activeConnections < this.maxConnections) {
      this.activeConnections++;
      return await this.createConnection();
    }
    
    // Wait for available connection
    return new Promise(resolve => {
      const checkPool = () => {
        if (this.pool.length > 0) {
          resolve(this.pool.pop());
        } else {
          setTimeout(checkPool, 100);
        }
      };
      checkPool();
    });
  }
  
  releaseConnection(connection) {
    this.pool.push(connection);
  }
}
```

## Security Notes

- **HTTPS**: Always use HTTPS for sensitive data
- **API Key Security**: Secure API keys and tokens
- **Input Validation**: Validate URLs and data before making requests
- **Rate Limiting**: Implement rate limiting for external APIs

```javascript
// Secure HTTP client implementation
class SecureHTTPClient extends TuskHTTPClient {
  constructor() {
    super();
    this.allowedDomains = new Set(['api.example.com', 'jsonplaceholder.typicode.com']);
    this.maxRequestSize = 1024 * 1024; // 1MB
  }
  
  async request(method, url, data, options = {}) {
    // Validate URL
    const urlObj = new URL(url);
    if (!this.allowedDomains.has(urlObj.hostname)) {
      throw new Error(`Request to unauthorized domain: ${urlObj.hostname}`);
    }
    
    // Ensure HTTPS
    if (urlObj.protocol !== 'https:' && urlObj.hostname !== 'localhost') {
      throw new Error('HTTPS required for external requests');
    }
    
    // Validate request size
    if (data && JSON.stringify(data).length > this.maxRequestSize) {
      throw new Error('Request data too large');
    }
    
    return await super.request(method, url, data, options);
  }
  
  sanitizeHeaders(headers) {
    const sanitized = {};
    const allowedHeaders = ['content-type', 'authorization', 'accept', 'user-agent'];
    
    Object.keys(headers).forEach(key => {
      if (allowedHeaders.includes(key.toLowerCase())) {
        sanitized[key] = headers[key];
      }
    });
    
    return sanitized;
  }
}
```

## Best Practices

1. **Error Handling**: Implement comprehensive error handling for HTTP requests
2. **Timeout Management**: Set appropriate timeouts for different types of requests
3. **Retry Logic**: Implement retry logic for failed requests
4. **Rate Limiting**: Respect API rate limits
5. **Caching**: Cache responses when appropriate
6. **Security**: Always validate and sanitize input data

## Next Steps

- Learn about [@env variables](./051-at-env-variables-javascript.md) for environment management
- Explore [@server variables](./052-at-server-variables-javascript.md) for server configuration
- Master [@global variables](./053-at-global-variables-javascript.md) for global state management 
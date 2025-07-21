# REST API & HTTP Operations (A2-G1)

## Overview

The REST API & HTTP Operations module provides comprehensive HTTP client and server capabilities for building robust web services and consuming HTTP APIs.

## Features

### HTTP Client
- Complete HTTP method support (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS)
- SSL/TLS support with certificate validation
- Middleware pipeline system
- Authentication handling (Basic, Bearer, Digest, OAuth, API Key)
- Rate limiting and retry mechanisms
- Connection pooling and keep-alive
- Request/response logging

### REST API Server
- Resource-based routing system
- HTTP status code management
- Content negotiation (JSON, XML, HTML, CSV, YAML)
- HATEOAS implementation
- Request validation and sanitization
- CORS support
- Security features (CSRF, XSS protection, security headers)
- Middleware pipeline

## Quick Start

### HTTP Client Usage

```php
use TuskLang\Communication\Http\HttpClient;

// Create client with configuration
$client = new HttpClient([
    'timeout' => 30,
    'verify_ssl' => true,
    'rate_limit' => 60
]);

// Simple GET request
$response = $client->get('https://api.example.com/users');
if ($response->isSuccess()) {
    $data = $response->getJson();
    print_r($data);
}

// POST with data
$response = $client->post('https://api.example.com/users', [
    'name' => 'John Doe',
    'email' => 'john@example.com'
]);

// Authentication
$client->setAuth('bearer', ['token' => 'your-jwt-token']);
$response = $client->get('https://api.example.com/protected');

// Custom headers
$client->setHeaders([
    'X-API-Key' => 'your-api-key',
    'Accept' => 'application/json'
]);
```

### REST API Server Usage

```php
use TuskLang\Communication\Http\RestApiServer;
use TuskLang\Communication\Http\ApiRequest;
use TuskLang\Communication\Http\ApiResponse;

// Create server
$server = new RestApiServer([
    'cors_enabled' => true,
    'rate_limit_enabled' => true,
    'hateoas_enabled' => true
]);

// Define routes
$server->get('/users', function(ApiRequest $request) {
    return ApiResponse::success(['users' => getUserList()]);
});

$server->post('/users', function(ApiRequest $request) {
    $errors = $request->validate([
        'name' => 'required|string|min:3',
        'email' => 'required|email'
    ]);
    
    if (!empty($errors)) {
        return ApiResponse::validationError($errors);
    }
    
    $user = createUser($request->getData());
    return ApiResponse::created($user);
});

// Route with parameters
$server->get('/users/{id}', function(ApiRequest $request) {
    $id = $request->getRouteParam('id');
    $user = findUser($id);
    
    if (!$user) {
        return ApiResponse::notFound();
    }
    
    return ApiResponse::success($user);
});

// Resource routes (RESTful CRUD)
$server->resource('posts', PostController::class);

// Global middleware
$server->middleware(function(ApiRequest $request, $next) {
    // Log all requests
    error_log("API Request: {$request->getMethod()} {$request->getPath()}");
    return $next($request);
});

// Handle incoming request
$server->handle();
```

## Advanced Features

### Middleware System

```php
// Authentication middleware
$server->middleware(function(ApiRequest $request, $next) {
    $token = $request->getHeader('Authorization');
    if (!isValidToken($token)) {
        return ApiResponse::unauthorized();
    }
    return $next($request);
});

// Rate limiting middleware
$server->middleware(function(ApiRequest $request, $next) {
    $ip = $request->getClientIp();
    if (!HttpSecurity::checkRateLimit($ip, 60)) {
        return ApiResponse::error('Rate limit exceeded', 429);
    }
    return $next($request);
});
```

### Content Negotiation

```php
// Server automatically handles different content types
$server->get('/data', function() {
    return ApiResponse::success(['message' => 'Hello World']);
});

// Client request with Accept: application/xml returns XML
// Client request with Accept: application/json returns JSON
// Client request with Accept: text/csv returns CSV
```

### Request Validation

```php
$server->post('/users', function(ApiRequest $request) {
    $errors = $request->validate([
        'name' => 'required|string|min:3|max:50',
        'email' => 'required|email',
        'age' => 'integer|min:18|max:120',
        'role' => 'required|in:admin,user,moderator',
        'website' => 'url',
        'bio' => 'string|max:500'
    ]);
    
    if (!empty($errors)) {
        return ApiResponse::validationError($errors);
    }
    
    // Process valid data
    return ApiResponse::success(['status' => 'created']);
});
```

### HATEOAS Implementation

```php
// Responses automatically include hypermedia links
{
    "id": 123,
    "name": "John Doe",
    "_links": {
        "self": {"href": "/users/123"},
        "edit": {"href": "/users/123"},
        "delete": {"href": "/users/123"},
        "collection": {"href": "/users"}
    }
}
```

## Security Features

### CSRF Protection

```php
use TuskLang\Communication\Http\HttpSecurity;

// Generate token
$token = HttpSecurity::generateCsrfToken();
$_SESSION['csrf_token'] = $token;

// Validate token
if (!HttpSecurity::validateCsrfToken($submittedToken, $_SESSION['csrf_token'])) {
    return ApiResponse::forbidden('Invalid CSRF token');
}
```

### Security Headers

```php
// Automatic security headers
$headers = HttpSecurity::getSecurityHeaders([
    'csp' => "default-src 'self'; script-src 'self' 'unsafe-inline'",
    'frame_options' => 'SAMEORIGIN'
]);
```

### SSRF Prevention

```php
$url = 'https://user-provided-url.com';
if (!HttpSecurity::preventSSRF($url)) {
    return ApiResponse::forbidden('Invalid URL');
}
```

## Configuration Options

### HTTP Client Configuration

```php
$client = new HttpClient([
    'timeout' => 30,                    // Request timeout in seconds
    'connect_timeout' => 10,            // Connection timeout
    'max_redirects' => 5,              // Maximum redirects to follow
    'user_agent' => 'MyApp/1.0',       // User agent string
    'verify_ssl' => true,              // SSL certificate verification
    'verify_host' => true,             // SSL host verification
    'allow_redirects' => true,         // Follow redirects
    'decode_gzip' => true,             // Decode gzip responses
    'keep_alive' => true,              // Enable keep-alive
    'max_connections' => 100,          // Connection pool size
    'retry_attempts' => 3,             // Retry failed requests
    'retry_delay' => 1000,             // Retry delay in milliseconds
    'rate_limit' => 60,                // Requests per minute
    'enable_http2' => true,            // Enable HTTP/2
    'enable_compression' => true       // Enable compression
]);
```

### REST API Server Configuration

```php
$server = new RestApiServer([
    'cors_enabled' => true,
    'cors_origins' => ['*'],
    'cors_methods' => ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
    'rate_limit_enabled' => true,
    'rate_limit_requests' => 60,
    'rate_limit_window' => 60,
    'hateoas_enabled' => true,
    'api_version' => 'v1',
    'content_types' => ['application/json', 'application/xml', 'text/html'],
    'default_content_type' => 'application/json'
]);
```

## Performance Optimization

### Connection Pooling

```php
// Automatic connection reuse for better performance
$client = new HttpClient(['keep_alive' => true]);

// Multiple requests will reuse the same connection
$client->get('https://api.example.com/endpoint1');
$client->get('https://api.example.com/endpoint2');
$client->get('https://api.example.com/endpoint3');
```

### Response Caching

```php
$server->get('/cached-data', function() {
    $response = ApiResponse::success(getExpensiveData());
    return $response->cache(3600); // Cache for 1 hour
});
```

## Error Handling

### Client Error Handling

```php
try {
    $response = $client->get('https://api.example.com/data');
    
    if ($response->isSuccess()) {
        $data = $response->getJson();
    } else {
        handleHttpError($response->getStatusCode(), $response->getBody());
    }
    
} catch (HttpException $e) {
    if ($e->isClientError()) {
        // Handle 4xx errors
    } elseif ($e->isServerError()) {
        // Handle 5xx errors
    }
}
```

### Server Error Handling

```php
$server->middleware(function(ApiRequest $request, $next) {
    try {
        return $next($request);
    } catch (\Exception $e) {
        // Log error
        error_log("API Error: " . $e->getMessage());
        
        // Return appropriate error response
        if ($e instanceof ValidationException) {
            return ApiResponse::validationError($e->getErrors());
        } elseif ($e instanceof AuthException) {
            return ApiResponse::unauthorized($e->getMessage());
        } else {
            return ApiResponse::serverError('Internal server error');
        }
    }
});
```

## Testing

### Unit Tests

```php
// Test HTTP client
$client = new HttpClient(['verify_ssl' => false]);
$response = $client->get('https://httpbin.org/get');
$this->assertEquals(200, $response->getStatusCode());

// Test API server
$server = new RestApiServer();
$server->get('/test', function() {
    return ApiResponse::success(['test' => true]);
});

$response = $this->simulateRequest('GET', '/test');
$this->assertEquals(200, $response->getStatusCode());
```

### Integration Tests

See `/tests` directory for comprehensive test suites covering all functionality.

## Best Practices

1. **Always validate input data** using the built-in validation system
2. **Use appropriate HTTP status codes** for different scenarios
3. **Implement proper error handling** with try-catch blocks
4. **Enable CORS** for cross-origin requests when needed
5. **Use HTTPS** in production environments
6. **Implement rate limiting** to prevent abuse
7. **Add authentication middleware** for protected endpoints
8. **Use connection pooling** for better performance
9. **Implement proper logging** for debugging and monitoring
10. **Test all endpoints** with comprehensive unit and integration tests

## Related Modules

- **A3-G1**: Security & Encryption (JWT, OAuth integration)
- **A2-G4**: Message Queues (HTTP-based messaging)
- **A4-G5**: Cloud Services (HTTP client for cloud APIs)
- **A5-G1**: Data Processing (REST API data ingestion) 
# API Gateway Patterns in C# with TuskLang

## Overview

API gateways serve as the entry point for client applications, providing routing, authentication, rate limiting, and other cross-cutting concerns. This guide demonstrates how to implement robust API gateway patterns using C# and TuskLang configuration.

## Table of Contents

- [Basic API Gateway Setup](#basic-api-gateway-setup)
- [Routing Configuration](#routing-configuration)
- [Authentication & Authorization](#authentication--authorization)
- [Rate Limiting](#rate-limiting)
- [Request/Response Transformation](#requestresponse-transformation)
- [Circuit Breaker Integration](#circuit-breaker-integration)
- [Load Balancing](#load-balancing)
- [Monitoring & Observability](#monitoring--observability)
- [Advanced Patterns](#advanced-patterns)

## Basic API Gateway Setup

### Gateway Configuration

```csharp
// Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TuskLang;

var builder = WebApplication.CreateBuilder(args);

// Load TuskLang configuration
var tuskConfig = TuskConfig.Load("gateway.tsk");

builder.Services.AddApiGateway(tuskConfig);
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options => {
        options.Authority = tuskConfig.GetValue<string>("auth.authority");
        options.Audience = tuskConfig.GetValue<string>("auth.audience");
    });

var app = builder.Build();

app.UseApiGateway();
app.Run();
```

### TuskLang Gateway Configuration

```ini
# gateway.tsk
[gateway]
name = "Main API Gateway"
version = "1.0.0"
environment = @env("GATEWAY_ENV", "development")

[auth]
authority = @env("AUTH_AUTHORITY", "https://auth.example.com")
audience = @env("AUTH_AUDIENCE", "api.example.com")
require_auth = true

[rate_limiting]
enabled = true
requests_per_minute = @env("RATE_LIMIT_RPM", "100")
burst_limit = @env("RATE_LIMIT_BURST", "20")

[monitoring]
metrics_enabled = true
tracing_enabled = true
log_level = @env("LOG_LEVEL", "Information")

[services]
user_service = @env("USER_SERVICE_URL", "http://user-service:8080")
order_service = @env("ORDER_SERVICE_URL", "http://order-service:8081")
payment_service = @env("PAYMENT_SERVICE_URL", "http://payment-service:8082")
```

## Routing Configuration

### Dynamic Route Configuration

```csharp
// ApiGatewayService.cs
public class ApiGatewayService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiGatewayService> _logger;

    public ApiGatewayService(IConfiguration config, HttpClient httpClient, ILogger<ApiGatewayService> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IActionResult> RouteRequest(HttpContext context, string serviceName)
    {
        var serviceUrl = _config.GetValue<string>($"services:{serviceName}");
        if (string.IsNullOrEmpty(serviceUrl))
        {
            return new NotFoundResult();
        }

        var targetUrl = $"{serviceUrl}{context.Request.Path}{context.Request.QueryString}";
        
        _logger.LogInformation("Routing request to {ServiceName} at {TargetUrl}", 
            serviceName, targetUrl);

        return await ForwardRequest(context, targetUrl);
    }

    private async Task<IActionResult> ForwardRequest(HttpContext context, string targetUrl)
    {
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = new Uri(targetUrl)
        };

        // Copy headers
        foreach (var header in context.Request.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        // Copy body
        if (context.Request.Body != null)
        {
            request.Content = new StreamContent(context.Request.Body);
        }

        var response = await _httpClient.SendAsync(request);
        return new HttpResponseMessageResult(response);
    }
}
```

### Route Configuration with TuskLang

```ini
# routes.tsk
[routes.users]
path = "/api/users/*"
service = "user_service"
auth_required = true
rate_limit = @env("USER_RATE_LIMIT", "50")

[routes.orders]
path = "/api/orders/*"
service = "order_service"
auth_required = true
rate_limit = @env("ORDER_RATE_LIMIT", "30")

[routes.payments]
path = "/api/payments/*"
service = "payment_service"
auth_required = true
rate_limit = @env("PAYMENT_RATE_LIMIT", "20")

[routes.public]
path = "/api/public/*"
service = "public_service"
auth_required = false
rate_limit = @env("PUBLIC_RATE_LIMIT", "100")
```

## Authentication & Authorization

### JWT Token Validation

```csharp
// JwtAuthenticationHandler.cs
public class JwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _config;
    private readonly ILogger<JwtAuthenticationHandler> _logger;

    public JwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration config)
        : base(options, logger, encoder, clock)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("No token provided");
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config.GetValue<string>("auth:secret_key"));
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config.GetValue<string>("auth:issuer"),
                ValidateAudience = true,
                ValidAudience = _config.GetValue<string>("auth:audience"),
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == "user_id").Value;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, jwtToken.Claims.First(x => x.Type == "name").Value),
                new Claim(ClaimTypes.Role, jwtToken.Claims.First(x => x.Type == "role").Value)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogInformation("User {UserId} authenticated successfully", userId);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return AuthenticateResult.Fail("Invalid token");
        }
    }
}
```

### Authorization Policies

```csharp
// AuthorizationService.cs
public class AuthorizationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(IConfiguration config, ILogger<AuthorizationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> AuthorizeAsync(ClaimsPrincipal user, string resource, string action)
    {
        var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        var requiredRoles = _config.GetSection($"authorization:{resource}:{action}").Get<string[]>();

        if (requiredRoles == null || !requiredRoles.Any())
        {
            _logger.LogWarning("No authorization rules found for {Resource}:{Action}", resource, action);
            return false;
        }

        var hasPermission = userRoles.Any(role => requiredRoles.Contains(role));
        
        _logger.LogInformation("User {UserId} authorization for {Resource}:{Action} = {Result}", 
            user.Identity?.Name, resource, action, hasPermission);

        return hasPermission;
    }
}
```

## Rate Limiting

### Token Bucket Rate Limiter

```csharp
// TokenBucketRateLimiter.cs
public class TokenBucketRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
    private readonly IConfiguration _config;
    private readonly ILogger<TokenBucketRateLimiter> _logger;

    public TokenBucketRateLimiter(IConfiguration config, ILogger<TokenBucketRateLimiter> logger)
    {
        _buckets = new ConcurrentDictionary<string, TokenBucket>();
        _config = config;
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(string key, int tokens = 1)
    {
        var bucket = _buckets.GetOrAdd(key, _ => CreateBucket(key));
        var acquired = await bucket.TryAcquireAsync(tokens);

        if (!acquired)
        {
            _logger.LogWarning("Rate limit exceeded for key: {Key}", key);
        }

        return acquired;
    }

    private TokenBucket CreateBucket(string key)
    {
        var capacity = _config.GetValue<int>($"rate_limiting:{key}:capacity", 100);
        var refillRate = _config.GetValue<double>($"rate_limiting:{key}:refill_rate", 10.0);
        var refillPeriod = TimeSpan.FromSeconds(_config.GetValue<int>($"rate_limiting:{key}:refill_period", 1));

        return new TokenBucket(capacity, refillRate, refillPeriod);
    }
}

public class TokenBucket
{
    private readonly int _capacity;
    private readonly double _refillRate;
    private readonly TimeSpan _refillPeriod;
    private double _tokens;
    private DateTime _lastRefill;

    public TokenBucket(int capacity, double refillRate, TimeSpan refillPeriod)
    {
        _capacity = capacity;
        _refillRate = refillRate;
        _refillPeriod = refillPeriod;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }

    public async Task<bool> TryAcquireAsync(int tokens)
    {
        await RefillTokensAsync();
        
        if (_tokens >= tokens)
        {
            _tokens -= tokens;
            return true;
        }

        return false;
    }

    private async Task RefillTokensAsync()
    {
        var now = DateTime.UtcNow;
        var timePassed = now - _lastRefill;
        var refillAmount = timePassed.TotalSeconds * _refillRate;

        _tokens = Math.Min(_capacity, _tokens + refillAmount);
        _lastRefill = now;
    }
}
```

### Rate Limiting Middleware

```csharp
// RateLimitingMiddleware.cs
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TokenBucketRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        TokenBucketRateLimiter rateLimiter,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var key = GetRateLimitKey(context);
        
        if (!await _rateLimiter.TryAcquireAsync(key))
        {
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.Headers.Add("Retry-After", "60");
            
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private string GetRateLimitKey(HttpContext context)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var endpoint = context.Request.Path.Value ?? "";
        
        return $"{userId}:{endpoint}";
    }
}
```

## Request/Response Transformation

### Request Transformation

```csharp
// RequestTransformer.cs
public class RequestTransformer
{
    private readonly IConfiguration _config;
    private readonly ILogger<RequestTransformer> _logger;

    public RequestTransformer(IConfiguration config, ILogger<RequestTransformer> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<HttpRequestMessage> TransformRequestAsync(HttpContext context, string serviceName)
    {
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = new Uri($"{GetServiceUrl(serviceName)}{context.Request.Path}{context.Request.QueryString}")
        };

        // Add service-specific headers
        var serviceHeaders = _config.GetSection($"services:{serviceName}:headers").Get<Dictionary<string, string>>();
        if (serviceHeaders != null)
        {
            foreach (var header in serviceHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        // Add correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
        request.Headers.Add("X-Correlation-ID", correlationId);

        // Transform request body if needed
        if (context.Request.Body != null)
        {
            var transformedBody = await TransformRequestBodyAsync(context.Request.Body, serviceName);
            request.Content = new StringContent(transformedBody, Encoding.UTF8, "application/json");
        }

        _logger.LogInformation("Request transformed for service {ServiceName} with correlation ID {CorrelationId}", 
            serviceName, correlationId);

        return request;
    }

    private async Task<string> TransformRequestBodyAsync(Stream body, string serviceName)
    {
        using var reader = new StreamReader(body);
        var originalBody = await reader.ReadToEndAsync();

        var transformationRules = _config.GetSection($"transformations:{serviceName}:request").Get<Dictionary<string, string>>();
        if (transformationRules == null)
        {
            return originalBody;
        }

        var transformedBody = originalBody;
        foreach (var rule in transformationRules)
        {
            transformedBody = transformedBody.Replace(rule.Key, rule.Value);
        }

        return transformedBody;
    }

    private string GetServiceUrl(string serviceName)
    {
        return _config.GetValue<string>($"services:{serviceName}") 
            ?? throw new InvalidOperationException($"Service {serviceName} not configured");
    }
}
```

### Response Transformation

```csharp
// ResponseTransformer.cs
public class ResponseTransformer
{
    private readonly IConfiguration _config;
    private readonly ILogger<ResponseTransformer> _logger;

    public ResponseTransformer(IConfiguration config, ILogger<ResponseTransformer> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<string> TransformResponseAsync(string responseBody, string serviceName, HttpStatusCode statusCode)
    {
        var transformationRules = _config.GetSection($"transformations:{serviceName}:response").Get<Dictionary<string, string>>();
        if (transformationRules == null)
        {
            return responseBody;
        }

        var transformedBody = responseBody;
        foreach (var rule in transformationRules)
        {
            transformedBody = transformedBody.Replace(rule.Key, rule.Value);
        }

        // Add response metadata
        var responseMetadata = new
        {
            original_response = responseBody,
            transformed_at = DateTime.UtcNow,
            service = serviceName,
            status_code = (int)statusCode
        };

        var metadataJson = JsonSerializer.Serialize(responseMetadata);
        _logger.LogInformation("Response transformed for service {ServiceName}: {Metadata}", 
            serviceName, metadataJson);

        return transformedBody;
    }
}
```

## Circuit Breaker Integration

### Circuit Breaker for Downstream Services

```csharp
// CircuitBreakerPolicy.cs
public class CircuitBreakerPolicy
{
    private readonly IConfiguration _config;
    private readonly ILogger<CircuitBreakerPolicy> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _states;

    public CircuitBreakerPolicy(IConfiguration config, ILogger<CircuitBreakerPolicy> logger)
    {
        _config = config;
        _logger = logger;
        _states = new ConcurrentDictionary<string, CircuitBreakerState>();
    }

    public async Task<T> ExecuteAsync<T>(string serviceName, Func<Task<T>> action)
    {
        var state = _states.GetOrAdd(serviceName, _ => CreateCircuitBreaker(serviceName));
        
        if (state.IsOpen)
        {
            _logger.LogWarning("Circuit breaker is OPEN for service {ServiceName}", serviceName);
            throw new CircuitBreakerOpenException($"Circuit breaker is open for service {serviceName}");
        }

        try
        {
            var result = await action();
            state.OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            state.OnFailure();
            _logger.LogError(ex, "Error calling service {ServiceName}", serviceName);
            throw;
        }
    }

    private CircuitBreakerState CreateCircuitBreaker(string serviceName)
    {
        var failureThreshold = _config.GetValue<int>($"circuit_breaker:{serviceName}:failure_threshold", 5);
        var timeout = TimeSpan.FromSeconds(_config.GetValue<int>($"circuit_breaker:{serviceName}:timeout", 60));
        
        return new CircuitBreakerState(failureThreshold, timeout);
    }
}

public class CircuitBreakerState
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitBreakerStatus _status;

    public CircuitBreakerState(int failureThreshold, TimeSpan timeout)
    {
        _failureThreshold = failureThreshold;
        _timeout = timeout;
        _status = CircuitBreakerStatus.Closed;
    }

    public bool IsOpen => _status == CircuitBreakerStatus.Open;

    public void OnSuccess()
    {
        _status = CircuitBreakerStatus.Closed;
        _failureCount = 0;
    }

    public void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= _failureThreshold)
        {
            _status = CircuitBreakerStatus.Open;
        }
    }

    public bool ShouldAttemptReset()
    {
        return _status == CircuitBreakerStatus.Open && 
               DateTime.UtcNow - _lastFailureTime >= _timeout;
    }
}

public enum CircuitBreakerStatus
{
    Closed,
    Open,
    HalfOpen
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
```

## Load Balancing

### Round-Robin Load Balancer

```csharp
// RoundRobinLoadBalancer.cs
public class RoundRobinLoadBalancer
{
    private readonly IConfiguration _config;
    private readonly ILogger<RoundRobinLoadBalancer> _logger;
    private readonly ConcurrentDictionary<string, LoadBalancerState> _states;

    public RoundRobinLoadBalancer(IConfiguration config, ILogger<RoundRobinLoadBalancer> logger)
    {
        _config = config;
        _logger = logger;
        _states = new ConcurrentDictionary<string, LoadBalancerState>();
    }

    public string GetNextEndpoint(string serviceName)
    {
        var state = _states.GetOrAdd(serviceName, _ => CreateLoadBalancer(serviceName));
        var endpoint = state.GetNextEndpoint();
        
        _logger.LogDebug("Selected endpoint {Endpoint} for service {ServiceName}", 
            endpoint, serviceName);
        
        return endpoint;
    }

    private LoadBalancerState CreateLoadBalancer(string serviceName)
    {
        var endpoints = _config.GetSection($"load_balancing:{serviceName}:endpoints")
            .Get<string[]>() ?? new string[0];
        
        return new LoadBalancerState(endpoints);
    }
}

public class LoadBalancerState
{
    private readonly string[] _endpoints;
    private int _currentIndex;
    private readonly object _lock = new object();

    public LoadBalancerState(string[] endpoints)
    {
        _endpoints = endpoints;
        _currentIndex = 0;
    }

    public string GetNextEndpoint()
    {
        if (_endpoints.Length == 0)
        {
            throw new InvalidOperationException("No endpoints configured");
        }

        lock (_lock)
        {
            var endpoint = _endpoints[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _endpoints.Length;
            return endpoint;
        }
    }
}
```

## Monitoring & Observability

### Gateway Metrics

```csharp
// GatewayMetrics.cs
public class GatewayMetrics
{
    private readonly IConfiguration _config;
    private readonly ILogger<GatewayMetrics> _logger;
    private readonly Counter _requestCounter;
    private readonly Histogram _responseTimeHistogram;
    private readonly Counter _errorCounter;

    public GatewayMetrics(IConfiguration config, ILogger<GatewayMetrics> logger)
    {
        _config = config;
        _logger = logger;
        
        _requestCounter = Metrics.CreateCounter("gateway_requests_total", "Total requests processed");
        _responseTimeHistogram = Metrics.CreateHistogram("gateway_response_time_seconds", "Response time distribution");
        _errorCounter = Metrics.CreateCounter("gateway_errors_total", "Total errors");
    }

    public void RecordRequest(string serviceName, string method, int statusCode, TimeSpan duration)
    {
        var labels = new Dictionary<string, string>
        {
            ["service"] = serviceName,
            ["method"] = method,
            ["status_code"] = statusCode.ToString()
        };

        _requestCounter.WithLabels(labels.Values.ToArray()).Inc();
        _responseTimeHistogram.WithLabels(labels.Values.ToArray()).Observe(duration.TotalSeconds);

        if (statusCode >= 400)
        {
            _errorCounter.WithLabels(labels.Values.ToArray()).Inc();
        }

        _logger.LogInformation("Request processed: {Service} {Method} {StatusCode} in {Duration}ms", 
            serviceName, method, statusCode, duration.TotalMilliseconds);
    }
}
```

### Distributed Tracing

```csharp
// TracingMiddleware.cs
public class TracingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<TracingMiddleware> _logger;

    public TracingMiddleware(RequestDelegate next, IConfiguration config, ILogger<TracingMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
        
        context.Response.Headers.Add("X-Correlation-ID", correlationId);

        using var activity = ActivitySource.StartActivity("Gateway.Request");
        activity?.SetTag("correlation.id", correlationId);
        activity?.SetTag("http.method", context.Request.Method);
        activity?.SetTag("http.url", context.Request.Path);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            activity?.SetTag("http.status_code", context.Response.StatusCode);
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
```

## Advanced Patterns

### Service Discovery Integration

```csharp
// ServiceDiscoveryClient.cs
public class ServiceDiscoveryClient
{
    private readonly IConfiguration _config;
    private readonly ILogger<ServiceDiscoveryClient> _logger;
    private readonly HttpClient _httpClient;

    public ServiceDiscoveryClient(IConfiguration config, HttpClient httpClient, ILogger<ServiceDiscoveryClient> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<string>> DiscoverServiceEndpointsAsync(string serviceName)
    {
        var discoveryUrl = _config.GetValue<string>("service_discovery:url");
        var response = await _httpClient.GetAsync($"{discoveryUrl}/services/{serviceName}");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var endpoints = JsonSerializer.Deserialize<List<string>>(content);
            
            _logger.LogInformation("Discovered {Count} endpoints for service {ServiceName}", 
                endpoints?.Count ?? 0, serviceName);
            
            return endpoints ?? new List<string>();
        }

        _logger.LogWarning("Failed to discover endpoints for service {ServiceName}", serviceName);
        return new List<string>();
    }
}
```

### Configuration Hot Reload

```csharp
// ConfigurationReloadService.cs
public class ConfigurationReloadService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ConfigurationReloadService> _logger;
    private readonly FileSystemWatcher _watcher;

    public ConfigurationReloadService(IConfiguration config, ILogger<ConfigurationReloadService> logger)
    {
        _config = config;
        _logger = logger;
        _watcher = new FileSystemWatcher(".", "*.tsk");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _watcher.Changed += OnConfigurationChanged;
        _watcher.EnableRaisingEvents = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private void OnConfigurationChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Configuration file {FileName} changed, triggering reload", e.Name);
        
        // Trigger configuration reload
        // This would typically involve reloading the TuskLang configuration
        // and updating the gateway settings
    }
}
```

## Best Practices

### Security Considerations

1. **Input Validation**: Always validate and sanitize incoming requests
2. **Rate Limiting**: Implement per-user and per-endpoint rate limiting
3. **Authentication**: Use strong authentication mechanisms (JWT, OAuth2)
4. **Authorization**: Implement fine-grained authorization policies
5. **HTTPS**: Always use HTTPS in production
6. **Headers**: Sanitize and validate all request headers

### Performance Optimization

1. **Caching**: Implement response caching for static content
2. **Connection Pooling**: Use connection pooling for downstream services
3. **Compression**: Enable response compression
4. **Monitoring**: Implement comprehensive monitoring and alerting
5. **Circuit Breakers**: Use circuit breakers to prevent cascade failures

### Deployment Considerations

1. **Health Checks**: Implement health checks for all downstream services
2. **Graceful Shutdown**: Handle graceful shutdown to avoid dropping requests
3. **Configuration Management**: Use external configuration management
4. **Logging**: Implement structured logging with correlation IDs
5. **Metrics**: Expose metrics for monitoring and alerting

## Conclusion

API gateways are essential components in modern microservices architectures. By implementing these patterns with C# and TuskLang, you can create robust, scalable, and maintainable gateway solutions that provide excellent developer experience and operational reliability.

The combination of C#'s strong typing and TuskLang's dynamic configuration capabilities makes it possible to build sophisticated API gateways that can adapt to changing requirements while maintaining high performance and reliability. 
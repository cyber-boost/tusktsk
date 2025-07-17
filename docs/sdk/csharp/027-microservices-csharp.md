# 🔧 Microservices - TuskLang for C# - "Service Mastery"

**Master microservices architecture with TuskLang in your C# applications!**

Microservices enable scalable, maintainable applications. This guide covers service discovery, communication, configuration management, and real-world microservices scenarios for TuskLang in C# environments.

## 🏗️ Microservices Philosophy

### "We Don't Bow to Any King"
- **Service independence** - Each service is autonomous
- **Configuration per service** - Service-specific configurations
- **Inter-service communication** - Efficient service-to-service calls
- **Service discovery** - Dynamic service location
- **Resilient patterns** - Handle service failures gracefully

## 🔍 Service Discovery

### Example: Service Discovery with TuskLang
```csharp
// ServiceDiscoveryService.cs
using Consul;
using Microsoft.Extensions.Hosting;

public class ServiceDiscoveryService : BackgroundService
{
    private readonly IConsulClient _consulClient;
    private readonly TuskLang _parser;
    private readonly ILogger<ServiceDiscoveryService> _logger;
    private readonly Dictionary<string, ServiceInfo> _serviceRegistry;
    
    public ServiceDiscoveryService(
        IConsulClient consulClient,
        ILogger<ServiceDiscoveryService> logger)
    {
        _consulClient = consulClient;
        _parser = new TuskLang();
        _serviceRegistry = new Dictionary<string, ServiceInfo>();
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateServiceRegistryAsync();
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service discovery update failed");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
    
    private async Task UpdateServiceRegistryAsync()
    {
        var services = await _consulClient.Agent.Services();
        
        foreach (var service in services.Response)
        {
            var serviceInfo = new ServiceInfo
            {
                Id = service.Value.ID,
                Name = service.Value.Service,
                Address = service.Value.Address,
                Port = service.Value.Port,
                Tags = service.Value.Tags?.ToList() ?? new List<string>(),
                LastSeen = DateTime.UtcNow
            };
            
            _serviceRegistry[service.Value.ID] = serviceInfo;
        }
        
        _logger.LogInformation("Updated service registry with {Count} services", _serviceRegistry.Count);
    }
    
    public async Task<Dictionary<string, object>> GetServiceConfigurationAsync()
    {
        var config = new Dictionary<string, object>();
        
        // Service registry information
        config["services"] = _serviceRegistry.Values.Select(s => new Dictionary<string, object>
        {
            ["id"] = s.Id,
            ["name"] = s.Name,
            ["address"] = s.Address,
            ["port"] = s.Port,
            ["tags"] = s.Tags,
            ["last_seen"] = s.LastSeen.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
        
        // Service health information
        config["service_health"] = await GetServiceHealthAsync();
        
        return config;
    }
    
    private async Task<Dictionary<string, object>> GetServiceHealthAsync()
    {
        var health = new Dictionary<string, object>();
        
        foreach (var service in _serviceRegistry.Values)
        {
            try
            {
                var healthCheck = await _consulClient.Health.Service(service.Name);
                var isHealthy = healthCheck.Response.Any(h => h.Checks.All(c => c.Status == "passing"));
                
                health[service.Name] = new Dictionary<string, object>
                {
                    ["healthy"] = isHealthy,
                    ["instance_count"] = healthCheck.Response.Count,
                    ["last_check"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check health for service {ServiceName}", service.Name);
                health[service.Name] = new Dictionary<string, object>
                {
                    ["healthy"] = false,
                    ["error"] = ex.Message
                };
            }
        }
        
        return health;
    }
}

public class ServiceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public DateTime LastSeen { get; set; }
}
```

## 📡 Inter-Service Communication

### Example: Service Communication with TuskLang
```csharp
// ServiceCommunicationService.cs
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;

public class ServiceCommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly ServiceDiscoveryService _serviceDiscovery;
    private readonly TuskLang _parser;
    private readonly ILogger<ServiceCommunicationService> _logger;
    private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreaker;
    
    public ServiceCommunicationService(
        HttpClient httpClient,
        ServiceDiscoveryService serviceDiscovery,
        ILogger<ServiceCommunicationService> logger)
    {
        _httpClient = httpClient;
        _serviceDiscovery = serviceDiscovery;
        _parser = new TuskLang();
        _logger = logger;
        
        _circuitBreaker = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(1)
            );
    }
    
    public async Task<Dictionary<string, object>> CallServiceAsync(
        string serviceName,
        string endpoint,
        Dictionary<string, object>? data = null)
    {
        try
        {
            // Get service information from discovery
            var serviceConfig = await _serviceDiscovery.GetServiceConfigurationAsync();
            var services = serviceConfig["services"] as List<object>;
            
            var targetService = services?.Cast<Dictionary<string, object>>()
                .FirstOrDefault(s => s["name"].ToString() == serviceName);
            
            if (targetService == null)
            {
                throw new Exception($"Service {serviceName} not found in registry");
            }
            
            var serviceAddress = targetService["address"].ToString();
            var servicePort = (int)targetService["port"];
            var serviceUrl = $"http://{serviceAddress}:{servicePort}{endpoint}";
            
            // Make service call with circuit breaker
            var response = await _circuitBreaker.ExecuteAsync(async () =>
            {
                if (data != null)
                {
                    var json = JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(serviceUrl, content);
                }
                else
                {
                    return await _httpClient.GetAsync(serviceUrl);
                }
            });
            
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            
            _logger.LogInformation("Successfully called service {ServiceName} at {Endpoint}", 
                serviceName, endpoint);
            
            return result;
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker is open for service {ServiceName}", serviceName);
            return new Dictionary<string, object> { ["error"] = "Service temporarily unavailable" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call service {ServiceName} at {Endpoint}", 
                serviceName, endpoint);
            throw;
        }
    }
    
    public async Task<Dictionary<string, object>> GetServiceMetricsAsync()
    {
        var metrics = new Dictionary<string, object>();
        
        try
        {
            // Call metrics endpoint on each service
            var serviceConfig = await _serviceDiscovery.GetServiceConfigurationAsync();
            var services = serviceConfig["services"] as List<object>;
            
            foreach (var service in services?.Cast<Dictionary<string, object>>() ?? Enumerable.Empty<Dictionary<string, object>>())
            {
                var serviceName = service["name"].ToString();
                
                try
                {
                    var serviceMetrics = await CallServiceAsync(serviceName, "/metrics");
                    metrics[serviceName] = serviceMetrics;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get metrics for service {ServiceName}", serviceName);
                    metrics[serviceName] = new Dictionary<string, object> { ["error"] = ex.Message };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service metrics");
        }
        
        return metrics;
    }
}
```

## ⚙️ Service Configuration Management

### Example: Service-Specific Configuration
```csharp
// ServiceConfigurationService.cs
public class ServiceConfigurationService
{
    private readonly TuskLang _parser;
    private readonly ILogger<ServiceConfigurationService> _logger;
    private readonly string _serviceName;
    
    public ServiceConfigurationService(
        string serviceName,
        ILogger<ServiceConfigurationService> logger)
    {
        _serviceName = serviceName;
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<Dictionary<string, object>> LoadServiceConfigurationAsync()
    {
        var config = new Dictionary<string, object>();
        
        // Load base configuration
        var baseConfig = _parser.ParseFile("config/base.tsk");
        config.Merge(baseConfig);
        
        // Load service-specific configuration
        var serviceConfigPath = $"config/services/{_serviceName}.tsk";
        if (File.Exists(serviceConfigPath))
        {
            var serviceConfig = _parser.ParseFile(serviceConfigPath);
            config.Merge(serviceConfig);
        }
        
        // Load environment-specific configuration
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var envConfigPath = $"config/environments/{environment}.tsk";
        if (File.Exists(envConfigPath))
        {
            var envConfig = _parser.ParseFile(envConfigPath);
            config.Merge(envConfig);
        }
        
        // Validate service configuration
        await ValidateServiceConfigurationAsync(config);
        
        _logger.LogInformation("Loaded configuration for service {ServiceName}", _serviceName);
        return config;
    }
    
    private async Task ValidateServiceConfigurationAsync(Dictionary<string, object> config)
    {
        var requiredFields = new[] { "service_name", "port", "database_connection" };
        
        foreach (var field in requiredFields)
        {
            if (!config.ContainsKey(field))
            {
                throw new Exception($"Required configuration field '{field}' is missing for service {_serviceName}");
            }
        }
        
        // Validate service-specific requirements
        if (_serviceName == "user-service" && !config.ContainsKey("jwt_secret"))
        {
            throw new Exception("JWT secret is required for user service");
        }
        
        if (_serviceName == "payment-service" && !config.ContainsKey("stripe_api_key"))
        {
            throw new Exception("Stripe API key is required for payment service");
        }
    }
}

public static class DictionaryExtensions
{
    public static void Merge<TKey, TValue>(this Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> source)
    {
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value;
        }
    }
}
```

## 🛠️ Real-World Microservices Scenarios
- **E-commerce platform**: User service, product service, order service, payment service
- **Social media**: User service, post service, notification service, analytics service
- **Banking system**: Account service, transaction service, fraud detection service
- **IoT platform**: Device service, data service, analytics service, notification service

## 🧩 Best Practices
- Use service discovery for dynamic service location
- Implement circuit breakers for fault tolerance
- Use configuration per service
- Monitor service health and performance
- Implement proper logging and tracing

## 🏁 You're Ready!

You can now:
- Build microservices with C# TuskLang
- Implement service discovery and communication
- Manage service-specific configurations
- Handle service failures gracefully

**Next:** [Event-Driven Architecture](028-event-driven-csharp.md)

---

**"We don't bow to any king" - Your service mastery, your architectural excellence, your microservices power.**

Build scalable services. Deploy with confidence. 🔧 
# Service Mesh Integration in C# with TuskLang

## Overview

Service mesh provides a dedicated infrastructure layer for handling service-to-service communication, offering features like traffic management, security, and observability. This guide demonstrates how to integrate with popular service mesh solutions using C# and TuskLang configuration.

## Table of Contents

- [Service Mesh Concepts](#service-mesh-concepts)
- [Istio Integration](#istio-integration)
- [Linkerd Integration](#linkerd-integration)
- [Consul Connect](#consul-connect)
- [Traffic Management](#traffic-management)
- [Security & mTLS](#security--mtls)
- [Observability](#observability)
- [Configuration Management](#configuration-management)
- [Advanced Patterns](#advanced-patterns)

## Service Mesh Concepts

### Service Mesh Architecture

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TuskLang;

var builder = WebApplication.CreateBuilder(args);

// Load TuskLang configuration
var tuskConfig = TuskConfig.Load("service-mesh.tsk");

builder.Services.AddServiceMeshIntegration(tuskConfig);
builder.Services.AddTrafficManagement(tuskConfig);
builder.Services.AddServiceMeshSecurity(tuskConfig);
builder.Services.AddServiceMeshObservability(tuskConfig);

var app = builder.Build();
app.Run();
```

### TuskLang Service Mesh Configuration

```ini
# service-mesh.tsk
[service_mesh]
enabled = @env("SERVICE_MESH_ENABLED", "true")
provider = @env("SERVICE_MESH_PROVIDER", "istio")
namespace = @env("SERVICE_MESH_NAMESPACE", "default")
service_name = @env("SERVICE_NAME", "user-service")
service_version = @env("SERVICE_VERSION", "v1")

[istio]
enabled = @env("ISTIO_ENABLED", "true")
proxy_port = @env("ISTIO_PROXY_PORT", "15001")
admin_port = @env("ISTIO_ADMIN_PORT", "15000")
inbound_port = @env("ISTIO_INBOUND_PORT", "8080")
outbound_port = @env("ISTIO_OUTBOUND_PORT", "8080")

[linkerd]
enabled = @env("LINKERD_ENABLED", "false")
proxy_port = @env("LINKERD_PROXY_PORT", "4140")
admin_port = @env("LINKERD_ADMIN_PORT", "4191")

[consul]
enabled = @env("CONSUL_ENABLED", "false")
datacenter = @env("CONSUL_DATACENTER", "dc1")
service_id = @env("CONSUL_SERVICE_ID", "user-service-1")

[traffic_management]
enabled = true
load_balancing = @env("TRAFFIC_LB_STRATEGY", "round-robin")
timeout = @env("TRAFFIC_TIMEOUT_MS", "5000")
retries = @env("TRAFFIC_RETRIES", "3")
circuit_breaker = @env("TRAFFIC_CIRCUIT_BREAKER", "true")

[security]
mtls_enabled = @env("MTLS_ENABLED", "true")
certificate_path = @env("CERT_PATH", "/etc/certs")
key_path = @env("KEY_PATH", "/etc/certs")
ca_path = @env("CA_PATH", "/etc/certs")

[observability]
metrics_enabled = @env("METRICS_ENABLED", "true")
tracing_enabled = @env("TRACING_ENABLED", "true")
logging_enabled = @env("LOGGING_ENABLED", "true")
jaeger_endpoint = @env("JAEGER_ENDPOINT", "http://jaeger:14268")
prometheus_port = @env("PROMETHEUS_PORT", "9090")
```

## Istio Integration

### Istio Service Client

```csharp
// IIstioServiceClient.cs
public interface IIstioServiceClient
{
    Task<T> CallServiceAsync<T>(string serviceName, string endpoint, CancellationToken cancellationToken = default);
    Task<T> CallServiceWithRetryAsync<T>(string serviceName, string endpoint, int retries = 3, CancellationToken cancellationToken = default);
    Task<T> CallServiceWithTimeoutAsync<T>(string serviceName, string endpoint, TimeSpan timeout, CancellationToken cancellationToken = default);
}

// IstioServiceClient.cs
public class IstioServiceClient : IIstioServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<IstioServiceClient> _logger;
    private readonly CircuitBreaker _circuitBreaker;

    public IstioServiceClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<IstioServiceClient> logger,
        CircuitBreaker circuitBreaker)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
    }

    public async Task<T> CallServiceAsync<T>(string serviceName, string endpoint, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var url = $"{serviceName}{endpoint}";
            
            _logger.LogInformation("Calling Istio service: {ServiceName} at {Endpoint}", serviceName, endpoint);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        });
    }

    public async Task<T> CallServiceWithRetryAsync<T>(string serviceName, string endpoint, int retries = 3, CancellationToken cancellationToken = default)
    {
        var retryPolicy = Policy<T>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(retries, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await CallServiceAsync<T>(serviceName, endpoint, cancellationToken);
        });
    }

    public async Task<T> CallServiceWithTimeoutAsync<T>(string serviceName, string endpoint, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        return await CallServiceAsync<T>(serviceName, endpoint, combinedCts.Token);
    }
}
```

### Istio Configuration

```csharp
// IstioConfiguration.cs
public static class IstioConfiguration
{
    public static IServiceCollection AddIstioIntegration(this IServiceCollection services, IConfiguration config)
    {
        var istioEnabled = config.GetValue<bool>("istio:enabled", false);
        
        if (!istioEnabled)
        {
            return services;
        }

        services.AddHttpClient<IIstioServiceClient, IstioServiceClient>(client =>
        {
            client.Timeout = TimeSpan.FromMilliseconds(
                config.GetValue<int>("traffic_management:timeout", 5000));
        });

        services.AddSingleton<CircuitBreaker>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<CircuitBreaker>>();
            var serviceName = config.GetValue<string>("service_mesh:service_name", "unknown");
            return new CircuitBreaker(serviceName, config, logger);
        });

        return services;
    }
}
```

## Linkerd Integration

### Linkerd Service Client

```csharp
// ILinkerdServiceClient.cs
public interface ILinkerdServiceClient
{
    Task<T> CallServiceAsync<T>(string serviceName, string endpoint, CancellationToken cancellationToken = default);
    Task<LinkerdMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
}

// LinkerdServiceClient.cs
public class LinkerdServiceClient : ILinkerdServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<LinkerdServiceClient> _logger;
    private readonly HttpClient _adminClient;

    public LinkerdServiceClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<LinkerdServiceClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        
        var adminPort = config.GetValue<int>("linkerd:admin_port", 4191);
        _adminClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{adminPort}")
        };
    }

    public async Task<T> CallServiceAsync<T>(string serviceName, string endpoint, CancellationToken cancellationToken = default)
    {
        var url = $"{serviceName}{endpoint}";
        
        _logger.LogInformation("Calling Linkerd service: {ServiceName} at {Endpoint}", serviceName, endpoint);
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<LinkerdMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _adminClient.GetAsync("/metrics", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseLinkerdMetrics(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Linkerd metrics");
            return new LinkerdMetrics();
        }
    }

    private LinkerdMetrics ParseLinkerdMetrics(string metricsContent)
    {
        var metrics = new LinkerdMetrics();
        var lines = metricsContent.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.StartsWith("linkerd_proxy_request_total"))
            {
                metrics.TotalRequests = ParseMetricValue(line);
            }
            else if (line.StartsWith("linkerd_proxy_request_duration_ms"))
            {
                metrics.AverageLatency = ParseMetricValue(line);
            }
        }
        
        return metrics;
    }

    private double ParseMetricValue(string metricLine)
    {
        var parts = metricLine.Split(' ');
        if (parts.Length >= 2 && double.TryParse(parts[1], out var value))
        {
            return value;
        }
        return 0.0;
    }
}

public class LinkerdMetrics
{
    public double TotalRequests { get; set; }
    public double AverageLatency { get; set; }
}
```

## Consul Connect

### Consul Connect Service Client

```csharp
// IConsulConnectServiceClient.cs
public interface IConsulConnectServiceClient
{
    Task<T> CallServiceAsync<T>(string serviceName, string endpoint, CancellationToken cancellationToken = default);
    Task RegisterServiceAsync(ConsulServiceRegistration registration, CancellationToken cancellationToken = default);
    Task DeregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default);
}

// ConsulConnectServiceClient.cs
public class ConsulConnectServiceClient : IConsulConnectServiceClient
{
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsulConnectServiceClient> _logger;
    private readonly HttpClient _httpClient;

    public ConsulConnectServiceClient(
        IConsulClient consulClient,
        IConfiguration config,
        ILogger<ConsulConnectServiceClient> logger)
    {
        _consulClient = consulClient;
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<T> CallServiceAsync<T>(string serviceName, string endpoint, CancellationToken cancellationToken = default)
    {
        var instances = await _consulClient.Catalog.Service(serviceName, cancellationToken);
        
        if (!instances.Response.Any())
        {
            throw new InvalidOperationException($"No instances found for service: {serviceName}");
        }

        // Use round-robin or other load balancing strategy
        var instance = instances.Response.First();
        var url = $"http://{instance.ServiceAddress}:{instance.ServicePort}{endpoint}";
        
        _logger.LogInformation("Calling Consul Connect service: {ServiceName} at {Url}", serviceName, url);
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task RegisterServiceAsync(ConsulServiceRegistration registration, CancellationToken cancellationToken = default)
    {
        var serviceRegistration = new AgentServiceRegistration
        {
            ID = registration.ServiceId,
            Name = registration.ServiceName,
            Address = registration.Address,
            Port = registration.Port,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{registration.Address}:{registration.Port}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5)
            }
        };

        await _consulClient.Agent.ServiceRegister(serviceRegistration, cancellationToken);
        
        _logger.LogInformation("Registered service with Consul Connect: {ServiceName}", registration.ServiceName);
    }

    public async Task DeregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await _consulClient.Agent.ServiceDeregister(serviceId, cancellationToken);
        
        _logger.LogInformation("Deregistered service from Consul Connect: {ServiceId}", serviceId);
    }
}

public class ConsulServiceRegistration
{
    public string ServiceId { get; set; }
    public string ServiceName { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }
}
```

## Traffic Management

### Traffic Management Service

```csharp
// ITrafficManagementService.cs
public interface ITrafficManagementService
{
    Task<T> RouteTrafficAsync<T>(string serviceName, string endpoint, TrafficPolicy policy, CancellationToken cancellationToken = default);
    Task UpdateTrafficPolicyAsync(string serviceName, TrafficPolicy policy, CancellationToken cancellationToken = default);
    Task<TrafficStats> GetTrafficStatsAsync(string serviceName, CancellationToken cancellationToken = default);
}

// TrafficManagementService.cs
public class TrafficManagementService : ITrafficManagementService
{
    private readonly IIstioServiceClient _istioClient;
    private readonly ILinkerdServiceClient _linkerdClient;
    private readonly IConsulConnectServiceClient _consulClient;
    private readonly IConfiguration _config;
    private readonly ILogger<TrafficManagementService> _logger;
    private readonly Dictionary<string, TrafficPolicy> _policies;

    public TrafficManagementService(
        IIstioServiceClient istioClient,
        ILinkerdServiceClient linkerdClient,
        IConsulConnectServiceClient consulClient,
        IConfiguration config,
        ILogger<TrafficManagementService> logger)
    {
        _istioClient = istioClient;
        _linkerdClient = linkerdClient;
        _consulClient = consulClient;
        _config = config;
        _logger = logger;
        _policies = new Dictionary<string, TrafficPolicy>();
    }

    public async Task<T> RouteTrafficAsync<T>(string serviceName, string endpoint, TrafficPolicy policy, CancellationToken cancellationToken = default)
    {
        var meshProvider = _config.GetValue<string>("service_mesh:provider", "istio");
        
        switch (meshProvider.ToLower())
        {
            case "istio":
                return await RouteWithIstioAsync<T>(serviceName, endpoint, policy, cancellationToken);
            case "linkerd":
                return await RouteWithLinkerdAsync<T>(serviceName, endpoint, policy, cancellationToken);
            case "consul":
                return await RouteWithConsulAsync<T>(serviceName, endpoint, policy, cancellationToken);
            default:
                throw new NotSupportedException($"Service mesh provider {meshProvider} is not supported");
        }
    }

    private async Task<T> RouteWithIstioAsync<T>(string serviceName, string endpoint, TrafficPolicy policy, CancellationToken cancellationToken)
    {
        if (policy.RetryCount > 0)
        {
            return await _istioClient.CallServiceWithRetryAsync<T>(serviceName, endpoint, policy.RetryCount, cancellationToken);
        }
        
        if (policy.Timeout.HasValue)
        {
            return await _istioClient.CallServiceWithTimeoutAsync<T>(serviceName, endpoint, policy.Timeout.Value, cancellationToken);
        }
        
        return await _istioClient.CallServiceAsync<T>(serviceName, endpoint, cancellationToken);
    }

    private async Task<T> RouteWithLinkerdAsync<T>(string serviceName, string endpoint, TrafficPolicy policy, CancellationToken cancellationToken)
    {
        // Linkerd handles retries and timeouts automatically based on configuration
        return await _linkerdClient.CallServiceAsync<T>(serviceName, endpoint, cancellationToken);
    }

    private async Task<T> RouteWithConsulAsync<T>(string serviceName, string endpoint, TrafficPolicy policy, CancellationToken cancellationToken)
    {
        // Consul Connect with custom retry logic
        var retryPolicy = Policy<T>
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(policy.RetryCount, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await _consulClient.CallServiceAsync<T>(serviceName, endpoint, cancellationToken);
        });
    }

    public async Task UpdateTrafficPolicyAsync(string serviceName, TrafficPolicy policy, CancellationToken cancellationToken = default)
    {
        _policies[serviceName] = policy;
        
        // In a real implementation, this would update the service mesh configuration
        _logger.LogInformation("Updated traffic policy for service: {ServiceName}", serviceName);
    }

    public async Task<TrafficStats> GetTrafficStatsAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var meshProvider = _config.GetValue<string>("service_mesh:provider", "istio");
        
        switch (meshProvider.ToLower())
        {
            case "linkerd":
                var linkerdMetrics = await _linkerdClient.GetMetricsAsync(cancellationToken);
                return new TrafficStats
                {
                    TotalRequests = linkerdMetrics.TotalRequests,
                    AverageLatency = linkerdMetrics.AverageLatency
                };
            default:
                return new TrafficStats();
        }
    }
}

public class TrafficPolicy
{
    public int RetryCount { get; set; } = 0;
    public TimeSpan? Timeout { get; set; }
    public string LoadBalancingStrategy { get; set; } = "round-robin";
    public bool CircuitBreakerEnabled { get; set; } = true;
}

public class TrafficStats
{
    public double TotalRequests { get; set; }
    public double AverageLatency { get; set; }
    public double ErrorRate { get; set; }
}
```

## Security & mTLS

### mTLS Configuration

```csharp
// IMtlsService.cs
public interface IMtlsService
{
    Task<bool> ValidateCertificateAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default);
    Task<bool> VerifyServerCertificateAsync(string serverName, X509Certificate2 certificate, CancellationToken cancellationToken = default);
}

// MtlsService.cs
public class MtlsService : IMtlsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MtlsService> _logger;
    private readonly string _certPath;
    private readonly string _keyPath;
    private readonly string _caPath;

    public MtlsService(IConfiguration config, ILogger<MtlsService> logger)
    {
        _config = config;
        _logger = logger;
        _certPath = _config.GetValue<string>("security:certificate_path", "/etc/certs");
        _keyPath = _config.GetValue<string>("security:key_path", "/etc/certs");
        _caPath = _config.GetValue<string>("security:ca_path", "/etc/certs");
    }

    public async Task<bool> ValidateCertificateAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var certFile = Path.Combine(_certPath, $"{serviceName}.crt");
            var keyFile = Path.Combine(_keyPath, $"{serviceName}.key");
            
            if (!File.Exists(certFile) || !File.Exists(keyFile))
            {
                _logger.LogWarning("Certificate or key file not found for service: {ServiceName}", serviceName);
                return false;
            }

            var certificate = new X509Certificate2(certFile);
            
            if (certificate.NotAfter < DateTime.UtcNow)
            {
                _logger.LogWarning("Certificate expired for service: {ServiceName}", serviceName);
                return false;
            }

            _logger.LogInformation("Certificate validated for service: {ServiceName}", serviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate for service: {ServiceName}", serviceName);
            return false;
        }
    }

    public async Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default)
    {
        var serviceName = _config.GetValue<string>("service_mesh:service_name", "unknown");
        var certFile = Path.Combine(_certPath, $"{serviceName}.crt");
        
        if (!File.Exists(certFile))
        {
            throw new FileNotFoundException($"Client certificate not found: {certFile}");
        }

        return new X509Certificate2(certFile);
    }

    public async Task<bool> VerifyServerCertificateAsync(string serverName, X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        try
        {
            var caFile = Path.Combine(_caPath, "ca.crt");
            if (!File.Exists(caFile))
            {
                _logger.LogWarning("CA certificate not found");
                return false;
            }

            var caCertificate = new X509Certificate2(caFile);
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            
            var isValid = chain.Build(certificate);
            
            _logger.LogInformation("Server certificate verification result: {IsValid} for {ServerName}", 
                isValid, serverName);
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying server certificate for: {ServerName}", serverName);
            return false;
        }
    }
}
```

## Observability

### Service Mesh Observability

```csharp
// IServiceMeshObservability.cs
public interface IServiceMeshObservability
{
    Task<ServiceMeshMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<List<ServiceMeshTrace>> GetTracesAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<List<ServiceMeshLog>> GetLogsAsync(string serviceName, CancellationToken cancellationToken = default);
}

// ServiceMeshObservability.cs
public class ServiceMeshObservability : IServiceMeshObservability
{
    private readonly IConfiguration _config;
    private readonly ILogger<ServiceMeshObservability> _logger;
    private readonly HttpClient _prometheusClient;
    private readonly HttpClient _jaegerClient;

    public ServiceMeshObservability(IConfiguration config, ILogger<ServiceMeshObservability> logger)
    {
        _config = config;
        _logger = logger;
        
        var prometheusPort = _config.GetValue<int>("observability:prometheus_port", 9090);
        _prometheusClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{prometheusPort}")
        };
        
        var jaegerEndpoint = _config.GetValue<string>("observability:jaeger_endpoint", "http://jaeger:14268");
        _jaegerClient = new HttpClient
        {
            BaseAddress = new Uri(jaegerEndpoint)
        };
    }

    public async Task<ServiceMeshMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _prometheusClient.GetAsync("/metrics", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParsePrometheusMetrics(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service mesh metrics");
            return new ServiceMeshMetrics();
        }
    }

    public async Task<List<ServiceMeshTrace>> GetTracesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _jaegerClient.GetAsync($"/api/traces?service={serviceName}", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseJaegerTraces(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service mesh traces for service: {ServiceName}", serviceName);
            return new List<ServiceMeshTrace>();
        }
    }

    public async Task<List<ServiceMeshLog>> GetLogsAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would query the logging system
        _logger.LogInformation("Retrieving logs for service: {ServiceName}", serviceName);
        return new List<ServiceMeshLog>();
    }

    private ServiceMeshMetrics ParsePrometheusMetrics(string metricsContent)
    {
        var metrics = new ServiceMeshMetrics();
        var lines = metricsContent.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.StartsWith("istio_requests_total"))
            {
                metrics.TotalRequests = ParseMetricValue(line);
            }
            else if (line.StartsWith("istio_request_duration_milliseconds"))
            {
                metrics.AverageLatency = ParseMetricValue(line);
            }
            else if (line.StartsWith("istio_request_errors_total"))
            {
                metrics.TotalErrors = ParseMetricValue(line);
            }
        }
        
        return metrics;
    }

    private List<ServiceMeshTrace> ParseJaegerTraces(string tracesContent)
    {
        // Simplified parsing - in reality, this would parse the full Jaeger response
        return new List<ServiceMeshTrace>();
    }

    private double ParseMetricValue(string metricLine)
    {
        var parts = metricLine.Split(' ');
        if (parts.Length >= 2 && double.TryParse(parts[1], out var value))
        {
            return value;
        }
        return 0.0;
    }
}

public class ServiceMeshMetrics
{
    public double TotalRequests { get; set; }
    public double AverageLatency { get; set; }
    public double TotalErrors { get; set; }
    public double ErrorRate => TotalRequests > 0 ? TotalErrors / TotalRequests : 0.0;
}

public class ServiceMeshTrace
{
    public string TraceId { get; set; }
    public string SpanId { get; set; }
    public string ServiceName { get; set; }
    public string OperationName { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
}

public class ServiceMeshLog
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; }
    public string ServiceName { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

## Configuration Management

### Service Mesh Configuration Manager

```csharp
// IServiceMeshConfigManager.cs
public interface IServiceMeshConfigManager
{
    Task<ServiceMeshConfig> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task UpdateConfigurationAsync(ServiceMeshConfig config, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(ServiceMeshConfig config, CancellationToken cancellationToken = default);
}

// ServiceMeshConfigManager.cs
public class ServiceMeshConfigManager : IServiceMeshConfigManager
{
    private readonly IConfiguration _config;
    private readonly ILogger<ServiceMeshConfigManager> _logger;
    private readonly TuskConfig _tuskConfig;

    public ServiceMeshConfigManager(IConfiguration config, ILogger<ServiceMeshConfigManager> logger)
    {
        _config = config;
        _logger = logger;
        _tuskConfig = TuskConfig.Load("service-mesh.tsk");
    }

    public async Task<ServiceMeshConfig> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return new ServiceMeshConfig
        {
            Enabled = _tuskConfig.GetValue<bool>("service_mesh:enabled", true),
            Provider = _tuskConfig.GetValue<string>("service_mesh:provider", "istio"),
            Namespace = _tuskConfig.GetValue<string>("service_mesh:namespace", "default"),
            ServiceName = _tuskConfig.GetValue<string>("service_mesh:service_name", "unknown"),
            ServiceVersion = _tuskConfig.GetValue<string>("service_mesh:service_version", "v1"),
            TrafficManagement = new TrafficManagementConfig
            {
                Enabled = _tuskConfig.GetValue<bool>("traffic_management:enabled", true),
                LoadBalancing = _tuskConfig.GetValue<string>("traffic_management:load_balancing", "round-robin"),
                Timeout = TimeSpan.FromMilliseconds(_tuskConfig.GetValue<int>("traffic_management:timeout", 5000)),
                Retries = _tuskConfig.GetValue<int>("traffic_management:retries", 3),
                CircuitBreaker = _tuskConfig.GetValue<bool>("traffic_management:circuit_breaker", true)
            },
            Security = new SecurityConfig
            {
                MtlsEnabled = _tuskConfig.GetValue<bool>("security:mtls_enabled", true),
                CertificatePath = _tuskConfig.GetValue<string>("security:certificate_path", "/etc/certs"),
                KeyPath = _tuskConfig.GetValue<string>("security:key_path", "/etc/certs"),
                CaPath = _tuskConfig.GetValue<string>("security:ca_path", "/etc/certs")
            },
            Observability = new ObservabilityConfig
            {
                MetricsEnabled = _tuskConfig.GetValue<bool>("observability:metrics_enabled", true),
                TracingEnabled = _tuskConfig.GetValue<bool>("observability:tracing_enabled", true),
                LoggingEnabled = _tuskConfig.GetValue<bool>("observability:logging_enabled", true),
                JaegerEndpoint = _tuskConfig.GetValue<string>("observability:jaeger_endpoint", "http://jaeger:14268"),
                PrometheusPort = _tuskConfig.GetValue<int>("observability:prometheus_port", 9090)
            }
        };
    }

    public async Task UpdateConfigurationAsync(ServiceMeshConfig config, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would update the TuskLang configuration file
        _logger.LogInformation("Updating service mesh configuration for service: {ServiceName}", config.ServiceName);
        
        // Validate the configuration before applying
        if (!await ValidateConfigurationAsync(config, cancellationToken))
        {
            throw new InvalidOperationException("Invalid service mesh configuration");
        }
    }

    public async Task<bool> ValidateConfigurationAsync(ServiceMeshConfig config, CancellationToken cancellationToken = default)
    {
        if (config == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(config.ServiceName))
        {
            _logger.LogError("Service name is required");
            return false;
        }

        if (config.Security.MtlsEnabled)
        {
            if (!Directory.Exists(config.Security.CertificatePath))
            {
                _logger.LogError("Certificate path does not exist: {Path}", config.Security.CertificatePath);
                return false;
            }
        }

        return true;
    }
}

public class ServiceMeshConfig
{
    public bool Enabled { get; set; }
    public string Provider { get; set; }
    public string Namespace { get; set; }
    public string ServiceName { get; set; }
    public string ServiceVersion { get; set; }
    public TrafficManagementConfig TrafficManagement { get; set; }
    public SecurityConfig Security { get; set; }
    public ObservabilityConfig Observability { get; set; }
}

public class TrafficManagementConfig
{
    public bool Enabled { get; set; }
    public string LoadBalancing { get; set; }
    public TimeSpan Timeout { get; set; }
    public int Retries { get; set; }
    public bool CircuitBreaker { get; set; }
}

public class SecurityConfig
{
    public bool MtlsEnabled { get; set; }
    public string CertificatePath { get; set; }
    public string KeyPath { get; set; }
    public string CaPath { get; set; }
}

public class ObservabilityConfig
{
    public bool MetricsEnabled { get; set; }
    public bool TracingEnabled { get; set; }
    public bool LoggingEnabled { get; set; }
    public string JaegerEndpoint { get; set; }
    public int PrometheusPort { get; set; }
}
```

## Advanced Patterns

### Service Mesh Health Check

```csharp
// ServiceMeshHealthCheck.cs
public class ServiceMeshHealthCheck : IHealthCheck
{
    private readonly IServiceMeshConfigManager _configManager;
    private readonly IMtlsService _mtlsService;
    private readonly IConfiguration _config;
    private readonly ILogger<ServiceMeshHealthCheck> _logger;

    public ServiceMeshHealthCheck(
        IServiceMeshConfigManager configManager,
        IMtlsService mtlsService,
        IConfiguration config,
        ILogger<ServiceMeshHealthCheck> logger)
    {
        _configManager = configManager;
        _mtlsService = mtlsService;
        _config = config;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _configManager.GetConfigurationAsync(cancellationToken);
            
            if (!config.Enabled)
            {
                return HealthCheckResult.Healthy("Service mesh is disabled");
            }

            var healthChecks = new List<HealthCheckResult>();

            // Check mTLS if enabled
            if (config.Security.MtlsEnabled)
            {
                var serviceName = config.ServiceName;
                var certValid = await _mtlsService.ValidateCertificateAsync(serviceName, cancellationToken);
                
                if (certValid)
                {
                    healthChecks.Add(HealthCheckResult.Healthy("mTLS certificates are valid"));
                }
                else
                {
                    healthChecks.Add(HealthCheckResult.Unhealthy("mTLS certificates are invalid"));
                }
            }

            // Check service mesh provider
            var providerHealthy = await CheckProviderHealthAsync(config.Provider, cancellationToken);
            if (providerHealthy)
            {
                healthChecks.Add(HealthCheckResult.Healthy($"Service mesh provider {config.Provider} is healthy"));
            }
            else
            {
                healthChecks.Add(HealthCheckResult.Unhealthy($"Service mesh provider {config.Provider} is unhealthy"));
            }

            if (healthChecks.All(h => h.Status == HealthStatus.Healthy))
            {
                return HealthCheckResult.Healthy("Service mesh is healthy");
            }

            var unhealthyChecks = healthChecks.Where(h => h.Status != HealthStatus.Healthy);
            return HealthCheckResult.Unhealthy("Service mesh has health issues", 
                data: unhealthyChecks.ToDictionary(h => h.Description, h => h.Exception?.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service mesh health");
            return HealthCheckResult.Unhealthy("Service mesh health check failed", ex);
        }
    }

    private async Task<bool> CheckProviderHealthAsync(string provider, CancellationToken cancellationToken)
    {
        try
        {
            switch (provider.ToLower())
            {
                case "istio":
                    return await CheckIstioHealthAsync(cancellationToken);
                case "linkerd":
                    return await CheckLinkerdHealthAsync(cancellationToken);
                case "consul":
                    return await CheckConsulHealthAsync(cancellationToken);
                default:
                    _logger.LogWarning("Unknown service mesh provider: {Provider}", provider);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for provider: {Provider}", provider);
            return false;
        }
    }

    private async Task<bool> CheckIstioHealthAsync(CancellationToken cancellationToken)
    {
        // Check Istio proxy health
        var adminPort = _config.GetValue<int>("istio:admin_port", 15000);
        using var client = new HttpClient();
        
        try
        {
            var response = await client.GetAsync($"http://localhost:{adminPort}/ready", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckLinkerdHealthAsync(CancellationToken cancellationToken)
    {
        // Check Linkerd proxy health
        var adminPort = _config.GetValue<int>("linkerd:admin_port", 4191);
        using var client = new HttpClient();
        
        try
        {
            var response = await client.GetAsync($"http://localhost:{adminPort}/ready", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckConsulHealthAsync(CancellationToken cancellationToken)
    {
        // Check Consul health
        using var client = new HttpClient();
        
        try
        {
            var response = await client.GetAsync("http://localhost:8500/v1/status/leader", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

## Best Practices

### Service Mesh Best Practices

1. **Provider Selection**: Choose the right service mesh provider for your use case
2. **Configuration Management**: Use TuskLang for centralized configuration management
3. **Security**: Enable mTLS for secure service-to-service communication
4. **Observability**: Implement comprehensive monitoring and tracing
5. **Traffic Management**: Use appropriate traffic routing and load balancing strategies
6. **Health Checks**: Implement proper health checks for all services

### Performance Optimization

1. **Connection Pooling**: Use connection pooling for service mesh proxies
2. **Caching**: Implement appropriate caching strategies
3. **Circuit Breakers**: Use circuit breakers to prevent cascade failures
4. **Retry Policies**: Implement intelligent retry policies
5. **Timeout Management**: Set appropriate timeouts for service calls

### Security Considerations

1. **mTLS**: Enable mutual TLS for all service-to-service communication
2. **Certificate Management**: Implement proper certificate rotation and management
3. **Access Control**: Use service mesh policies for access control
4. **Network Policies**: Implement network policies for additional security
5. **Audit Logging**: Enable audit logging for security events

## Conclusion

Service mesh integration provides powerful capabilities for managing service-to-service communication in distributed systems. By implementing these patterns with C# and TuskLang, you can build robust, secure, and observable microservices architectures.

The combination of Istio, Linkerd, or Consul Connect with proper configuration management and observability provides a comprehensive solution for modern service mesh deployments. 
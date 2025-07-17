# @http.host - HTTP Host in Java

The `@http.host` operator provides access to HTTP host information in Java applications, integrating with Spring Boot's HTTP context, Servlet API, and enterprise HTTP client configurations.

## Basic Syntax

```java
// TuskLang configuration
host_name: @http.host()
host_port: @http.host.port()
host_protocol: @http.host.protocol()
full_url: @http.host.url()
```

```java
// Java Spring Boot integration
@Configuration
public class HttpHostConfig {
    
    @Bean
    public HttpHostService httpHostService() {
        return HttpHostService.builder()
            .defaultHost("localhost")
            .defaultPort(8080)
            .defaultProtocol("http")
            .build();
    }
}
```

## HTTP Host Information

```java
// Java HTTP host service
@Component
public class HttpHostService {
    
    @Autowired
    private HttpServletRequest request;
    
    public String getHost() {
        return request.getServerName();
    }
    
    public int getPort() {
        return request.getServerPort();
    }
    
    public String getProtocol() {
        return request.getScheme();
    }
    
    public String getFullUrl() {
        return request.getScheme() + "://" + request.getServerName() + 
               (request.getServerPort() != 80 && request.getServerPort() != 443 ? 
                ":" + request.getServerPort() : "") + request.getRequestURI();
    }
    
    public String getBaseUrl() {
        return request.getScheme() + "://" + request.getServerName() + 
               (request.getServerPort() != 80 && request.getServerPort() != 443 ? 
                ":" + request.getServerPort() : "");
    }
    
    public String getContextPath() {
        return request.getContextPath();
    }
}
```

```java
// TuskLang HTTP host information
http_host_config: {
    # Basic host information
    host: @http.host()
    port: @http.host.port()
    protocol: @http.host.protocol()
    
    # Full URL information
    full_url: @http.host.url()
    base_url: @http.host.base_url()
    context_path: @http.host.context_path()
    
    # Host details
    host_with_port: @http.host.with_port()
    canonical_host: @http.host.canonical()
    is_secure: @http.host.is_secure()
}
```

## HTTP Host Validation

```java
// Java HTTP host validation
@Component
public class HttpHostValidationService {
    
    @Autowired
    private HttpHostService httpHostService;
    
    public boolean isValidHost(String host) {
        if (host == null || host.trim().isEmpty()) {
            return false;
        }
        
        // Check for valid hostname pattern
        String hostnamePattern = "^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$";
        if (!host.matches(hostnamePattern)) {
            return false;
        }
        
        // Check for localhost or valid domain
        return host.equals("localhost") || host.contains(".");
    }
    
    public boolean isValidPort(int port) {
        return port > 0 && port <= 65535;
    }
    
    public boolean isSecureProtocol(String protocol) {
        return "https".equalsIgnoreCase(protocol);
    }
    
    public void validateHttpHost() {
        String host = httpHostService.getHost();
        int port = httpHostService.getPort();
        String protocol = httpHostService.getProtocol();
        
        if (!isValidHost(host)) {
            throw new IllegalArgumentException("Invalid host: " + host);
        }
        
        if (!isValidPort(port)) {
            throw new IllegalArgumentException("Invalid port: " + port);
        }
        
        if (!isSecureProtocol(protocol) && !"http".equalsIgnoreCase(protocol)) {
            throw new IllegalArgumentException("Invalid protocol: " + protocol);
        }
    }
}
```

```java
// TuskLang HTTP host validation
http_host_validation: {
    # Validate host
    @http.host.validate()
    @http.host.validate.secure()
    @http.host.validate.port()
    
    # Host validation results
    is_valid_host: @http.host.is_valid()
    is_secure_host: @http.host.is_secure()
    is_valid_port: @http.host.port.is_valid()
    
    # Validation errors
    validation_errors: @http.host.validation_errors()
}
```

## HTTP Host Configuration

```java
// Java HTTP host configuration
@Configuration
public class HttpHostConfiguration {
    
    @Value("${server.host:localhost}")
    private String serverHost;
    
    @Value("${server.port:8080}")
    private int serverPort;
    
    @Value("${server.ssl.enabled:false}")
    private boolean sslEnabled;
    
    @Bean
    public HttpHostConfigurationService httpHostConfigurationService() {
        return HttpHostConfigurationService.builder()
            .host(serverHost)
            .port(serverPort)
            .protocol(sslEnabled ? "https" : "http")
            .sslEnabled(sslEnabled)
            .build();
    }
}
```

```java
// TuskLang HTTP host configuration
http_host_configuration: {
    # Server configuration
    server_host: @http.host.config.host()
    server_port: @http.host.config.port()
    server_protocol: @http.host.config.protocol()
    
    # SSL configuration
    ssl_enabled: @http.host.config.ssl_enabled()
    ssl_protocol: @http.host.config.ssl_protocol()
    ssl_cipher_suite: @http.host.config.ssl_cipher_suite()
    
    # Proxy configuration
    proxy_enabled: @http.host.config.proxy_enabled()
    proxy_host: @http.host.config.proxy_host()
    proxy_port: @http.host.config.proxy_port()
}
```

## HTTP Host Routing

```java
// Java HTTP host routing
@Component
public class HttpHostRoutingService {
    
    @Autowired
    private HttpHostService httpHostService;
    
    public String getApiUrl() {
        return httpHostService.getBaseUrl() + "/api";
    }
    
    public String getWebhookUrl() {
        return httpHostService.getBaseUrl() + "/webhooks";
    }
    
    public String getStaticUrl() {
        return httpHostService.getBaseUrl() + "/static";
    }
    
    public String getAdminUrl() {
        return httpHostService.getBaseUrl() + "/admin";
    }
    
    public String getUrlForPath(String path) {
        return httpHostService.getBaseUrl() + path;
    }
    
    public Map<String, String> getAllUrls() {
        return Map.of(
            "api", getApiUrl(),
            "webhook", getWebhookUrl(),
            "static", getStaticUrl(),
            "admin", getAdminUrl()
        );
    }
}
```

```java
// TuskLang HTTP host routing
http_host_routing: {
    # API routing
    api_url: @http.host.url("/api")
    api_v1_url: @http.host.url("/api/v1")
    api_v2_url: @http.host.url("/api/v2")
    
    # Webhook routing
    webhook_url: @http.host.url("/webhooks")
    webhook_stripe: @http.host.url("/webhooks/stripe")
    webhook_github: @http.host.url("/webhooks/github")
    
    # Static assets
    static_url: @http.host.url("/static")
    assets_url: @http.host.url("/assets")
    images_url: @http.host.url("/images")
    
    # Admin routes
    admin_url: @http.host.url("/admin")
    admin_dashboard: @http.host.url("/admin/dashboard")
    admin_users: @http.host.url("/admin/users")
}
```

## HTTP Host Security

```java
// Java HTTP host security
@Component
public class HttpHostSecurityService {
    
    @Autowired
    private HttpHostService httpHostService;
    
    public boolean isAllowedHost(String host) {
        List<String> allowedHosts = Arrays.asList(
            "localhost", "127.0.0.1", "app.example.com", "api.example.com"
        );
        return allowedHosts.contains(host);
    }
    
    public boolean isSecureConnection() {
        return "https".equalsIgnoreCase(httpHostService.getProtocol());
    }
    
    public void validateHostSecurity() {
        String host = httpHostService.getHost();
        
        if (!isAllowedHost(host)) {
            throw new SecurityException("Host not allowed: " + host);
        }
        
        if (!isSecureConnection()) {
            log.warn("Insecure connection detected for host: {}", host);
        }
    }
    
    public Map<String, Object> getSecurityInfo() {
        return Map.of(
            "host", httpHostService.getHost(),
            "protocol", httpHostService.getProtocol(),
            "is_secure", isSecureConnection(),
            "is_allowed", isAllowedHost(httpHostService.getHost())
        );
    }
}
```

```java
// TuskLang HTTP host security
http_host_security: {
    # Security checks
    is_allowed_host: @http.host.security.is_allowed()
    is_secure_connection: @http.host.security.is_secure()
    is_trusted_host: @http.host.security.is_trusted()
    
    # Security validation
    @http.host.security.validate()
    @http.host.security.validate.ssl()
    @http.host.security.validate.origin()
    
    # Security information
    security_info: {
        host: @http.host()
        protocol: @http.host.protocol()
        is_secure: @http.host.security.is_secure()
        is_allowed: @http.host.security.is_allowed()
        ssl_version: @http.host.security.ssl_version()
        cipher_suite: @http.host.security.cipher_suite()
    }
}
```

## HTTP Host Monitoring

```java
// Java HTTP host monitoring
@Component
public class HttpHostMonitoringService {
    
    @Autowired
    private HttpHostService httpHostService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public void recordHostAccess() {
        String host = httpHostService.getHost();
        
        Counter.builder("http.host.access")
            .tag("host", host)
            .tag("protocol", httpHostService.getProtocol())
            .register(meterRegistry)
            .increment();
    }
    
    public void recordHostError(String error) {
        String host = httpHostService.getHost();
        
        Counter.builder("http.host.errors")
            .tag("host", host)
            .tag("error", error)
            .register(meterRegistry)
            .increment();
    }
    
    public Map<String, Object> getHostMetrics() {
        return Map.of(
            "host", httpHostService.getHost(),
            "port", httpHostService.getPort(),
            "protocol", httpHostService.getProtocol(),
            "access_count", getAccessCount(),
            "error_count", getErrorCount(),
            "last_access", Instant.now()
        );
    }
    
    private long getAccessCount() {
        // Implementation to get access count from metrics
        return 0L;
    }
    
    private long getErrorCount() {
        // Implementation to get error count from metrics
        return 0L;
    }
}
```

```java
// TuskLang HTTP host monitoring
http_host_monitoring: {
    # Monitor host access
    @http.host.monitor.access()
    @http.host.monitor.errors()
    @http.host.monitor.performance()
    
    # Host metrics
    host_metrics: {
        host: @http.host()
        port: @http.host.port()
        protocol: @http.host.protocol()
        access_count: @http.host.metrics.access_count()
        error_count: @http.host.metrics.error_count()
        response_time: @http.host.metrics.response_time()
        last_access: @http.host.metrics.last_access()
    }
    
    # Host health
    host_health: @http.host.health.check()
}
```

## HTTP Host Caching

```java
// Java HTTP host caching
@Component
public class HttpHostCachingService {
    
    @Autowired
    private HttpHostService httpHostService;
    
    @Cacheable("host-info")
    public HostInfo getHostInfo() {
        return HostInfo.builder()
            .host(httpHostService.getHost())
            .port(httpHostService.getPort())
            .protocol(httpHostService.getProtocol())
            .fullUrl(httpHostService.getFullUrl())
            .baseUrl(httpHostService.getBaseUrl())
            .build();
    }
    
    @Cacheable("host-urls")
    public Map<String, String> getHostUrls() {
        return Map.of(
            "api", httpHostService.getBaseUrl() + "/api",
            "webhook", httpHostService.getBaseUrl() + "/webhooks",
            "static", httpHostService.getBaseUrl() + "/static",
            "admin", httpHostService.getBaseUrl() + "/admin"
        );
    }
    
    @CacheEvict("host-info")
    public void clearHostInfoCache() {
        // Cache will be cleared
    }
}
```

```java
// TuskLang HTTP host caching
http_host_caching: {
    # Cache host information
    cached_host_info: @http.host.cache.get("host-info", {
        host: @http.host()
        port: @http.host.port()
        protocol: @http.host.protocol()
        full_url: @http.host.url()
        base_url: @http.host.base_url()
    })
    
    # Cache host URLs
    cached_urls: @http.host.cache.get("host-urls", {
        api: @http.host.url("/api")
        webhook: @http.host.url("/webhooks")
        static: @http.host.url("/static")
        admin: @http.host.url("/admin")
    })
    
    # Cache invalidation
    @http.host.cache.invalidate("host-info")
    @http.host.cache.invalidate("host-urls")
}
```

## HTTP Host Testing

```java
// JUnit test for HTTP host
@SpringBootTest
class HttpHostServiceTest {
    
    @Autowired
    private HttpHostService httpHostService;
    
    @MockBean
    private HttpServletRequest request;
    
    @Test
    void testGetHost() {
        when(request.getServerName()).thenReturn("localhost");
        when(request.getServerPort()).thenReturn(8080);
        when(request.getScheme()).thenReturn("http");
        when(request.getRequestURI()).thenReturn("/api/test");
        
        assertThat(httpHostService.getHost()).isEqualTo("localhost");
        assertThat(httpHostService.getPort()).isEqualTo(8080);
        assertThat(httpHostService.getProtocol()).isEqualTo("http");
        assertThat(httpHostService.getFullUrl()).isEqualTo("http://localhost:8080/api/test");
    }
    
    @Test
    void testGetBaseUrl() {
        when(request.getServerName()).thenReturn("app.example.com");
        when(request.getServerPort()).thenReturn(443);
        when(request.getScheme()).thenReturn("https");
        
        assertThat(httpHostService.getBaseUrl()).isEqualTo("https://app.example.com");
    }
    
    @Test
    void testGetContextPath() {
        when(request.getContextPath()).thenReturn("/myapp");
        
        assertThat(httpHostService.getContextPath()).isEqualTo("/myapp");
    }
}
```

```java
// TuskLang HTTP host testing
test_http_host: {
    # Test host information
    test_host: @http.host()
    assert(@test_host != "", "Host should not be empty")
    
    test_port: @http.host.port()
    assert(@test_port > 0, "Port should be positive")
    
    test_protocol: @http.host.protocol()
    assert(@test_protocol in ["http", "https"], "Protocol should be http or https")
    
    # Test URL generation
    test_url: @http.host.url("/test")
    assert(@test_url contains @http.host(), "URL should contain host")
    assert(@test_url contains "/test", "URL should contain path")
    
    # Test security
    assert(@http.host.security.is_allowed(), "Host should be allowed")
    assert(@http.host.is_valid(), "Host should be valid")
}
```

## Best Practices

### 1. Environment-Specific Host Configuration
```java
// Use different host configurations per environment
@Configuration
@Profile("development")
public class DevHttpHostConfig {
    
    @Bean
    public HttpHostService devHttpHostService() {
        return HttpHostService.builder()
            .defaultHost("localhost")
            .defaultPort(8080)
            .defaultProtocol("http")
            .build();
    }
}

@Configuration
@Profile("production")
public class ProdHttpHostConfig {
    
    @Bean
    public HttpHostService prodHttpHostService() {
        return HttpHostService.builder()
            .defaultHost("app.example.com")
            .defaultPort(443)
            .defaultProtocol("https")
            .build();
    }
}
```

### 2. Security Validation
```java
// Validate HTTP host security
@Component
public class HttpHostSecurityValidator {
    
    @Autowired
    private HttpHostService httpHostService;
    
    @PostConstruct
    public void validateHostSecurity() {
        String host = httpHostService.getHost();
        String protocol = httpHostService.getProtocol();
        
        // Check for secure connections in production
        if ("production".equals(System.getenv("NODE_ENV")) && 
            !"https".equalsIgnoreCase(protocol)) {
            throw new SecurityException("HTTPS required in production");
        }
        
        // Validate host against allowed list
        if (!isAllowedHost(host)) {
            throw new SecurityException("Host not allowed: " + host);
        }
    }
    
    private boolean isAllowedHost(String host) {
        List<String> allowedHosts = Arrays.asList(
            "localhost", "127.0.0.1", "app.example.com", "api.example.com"
        );
        return allowedHosts.contains(host);
    }
}
```

### 3. Monitoring and Metrics
```java
// Monitor HTTP host usage
@Component
public class HttpHostMetricsCollector {
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    @EventListener
    public void handleHttpRequest(HttpServletRequest request) {
        String host = request.getServerName();
        String protocol = request.getScheme();
        
        // Record host access metrics
        Counter.builder("http.host.requests")
            .tag("host", host)
            .tag("protocol", protocol)
            .register(meterRegistry)
            .increment();
        
        // Record response time
        Timer.builder("http.host.response.time")
            .tag("host", host)
            .register(meterRegistry)
            .record(System.currentTimeMillis(), TimeUnit.MILLISECONDS);
    }
}
```

The `@http.host` operator in Java provides comprehensive access to HTTP host information, enabling applications to dynamically generate URLs, validate host security, and monitor host usage patterns for enterprise-grade applications. 
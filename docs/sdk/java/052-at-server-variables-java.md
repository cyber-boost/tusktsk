# @server - Server Variables in Java

The `@server` operator provides access to server-specific variables and configuration in Java applications, integrating with Spring Boot's server context, JVM system properties, and enterprise server environments.

## Basic Syntax

```java
// TuskLang configuration
server_host: @server.host()
server_port: @server.port()
server_protocol: @server.protocol()
server_name: @server.name()
```

```java
// Java Spring Boot integration
@Configuration
public class ServerConfig {
    
    @Value("${server.port:8080}")
    private int serverPort;
    
    @Value("${server.host:localhost}")
    private String serverHost;
    
    @Bean
    public ServerVariableService serverVariableService() {
        return ServerVariableService.builder()
            .host(serverHost)
            .port(serverPort)
            .protocol("http")
            .name("tusk-java-app")
            .build();
    }
}
```

## Server Information

```java
// Java server information access
@Component
public class ServerInfoService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    public ServerInfo getServerInfo() {
        return ServerInfo.builder()
            .host(serverVariableService.getHost())
            .port(serverVariableService.getPort())
            .protocol(serverVariableService.getProtocol())
            .name(serverVariableService.getName())
            .startTime(serverVariableService.getStartTime())
            .uptime(serverVariableService.getUptime())
            .build();
    }
    
    public Map<String, Object> getServerVariables() {
        return Map.of(
            "host", serverVariableService.getHost(),
            "port", serverVariableService.getPort(),
            "protocol", serverVariableService.getProtocol(),
            "name", serverVariableService.getName(),
            "pid", serverVariableService.getProcessId(),
            "java_version", serverVariableService.getJavaVersion(),
            "os_name", serverVariableService.getOsName()
        );
    }
}
```

```java
// TuskLang server information
server_config: {
    host: @server.host()
    port: @server.port()
    protocol: @server.protocol()
    name: @server.name()
    
    # Server details
    pid: @server.pid()
    java_version: @server.java_version()
    os_name: @server.os_name()
    os_version: @server.os_version()
    
    # Server timing
    start_time: @server.start_time()
    uptime: @server.uptime()
    uptime_formatted: @server.uptime_formatted()
}
```

## Server Environment

```java
// Java server environment access
@Component
public class ServerEnvironmentService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    public ServerEnvironment getEnvironment() {
        return ServerEnvironment.builder()
            .environment(serverVariableService.getEnvironment())
            .profile(serverVariableService.getProfile())
            .instanceId(serverVariableService.getInstanceId())
            .region(serverVariableService.getRegion())
            .zone(serverVariableService.getZone())
            .build();
    }
    
    public boolean isProduction() {
        return "production".equals(serverVariableService.getEnvironment());
    }
    
    public boolean isDevelopment() {
        return "development".equals(serverVariableService.getEnvironment());
    }
}
```

```java
// TuskLang server environment
server_environment: {
    environment: @server.environment()
    profile: @server.profile()
    instance_id: @server.instance_id()
    region: @server.region()
    zone: @server.zone()
    
    # Environment checks
    is_production: @server.environment() == "production"
    is_development: @server.environment() == "development"
    is_staging: @server.environment() == "staging"
    is_testing: @server.environment() == "testing"
}
```

## Server Resources

```java
// Java server resource monitoring
@Component
public class ServerResourceService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    public ServerResources getResources() {
        Runtime runtime = Runtime.getRuntime();
        
        return ServerResources.builder()
            .totalMemory(runtime.totalMemory())
            .freeMemory(runtime.freeMemory())
            .usedMemory(runtime.totalMemory() - runtime.freeMemory())
            .maxMemory(runtime.maxMemory())
            .availableProcessors(runtime.availableProcessors())
            .build();
    }
    
    public double getMemoryUsagePercentage() {
        ServerResources resources = getResources();
        return (double) resources.getUsedMemory() / resources.getTotalMemory() * 100;
    }
    
    public double getCpuUsage() {
        return serverVariableService.getCpuUsage();
    }
}
```

```java
// TuskLang server resources
server_resources: {
    # Memory information
    total_memory: @server.memory.total()
    free_memory: @server.memory.free()
    used_memory: @server.memory.used()
    max_memory: @server.memory.max()
    memory_usage_percent: @server.memory.usage_percent()
    
    # CPU information
    cpu_count: @server.cpu.count()
    cpu_usage: @server.cpu.usage()
    cpu_load_average: @server.cpu.load_average()
    
    # Disk information
    disk_total: @server.disk.total()
    disk_free: @server.disk.free()
    disk_used: @server.disk.used()
    disk_usage_percent: @server.disk.usage_percent()
}
```

## Server Network

```java
// Java server network information
@Component
public class ServerNetworkService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    public NetworkInfo getNetworkInfo() {
        return NetworkInfo.builder()
            .localAddress(serverVariableService.getLocalAddress())
            .publicAddress(serverVariableService.getPublicAddress())
            .hostname(serverVariableService.getHostname())
            .domain(serverVariableService.getDomain())
            .build();
    }
    
    public List<NetworkInterface> getNetworkInterfaces() {
        return serverVariableService.getNetworkInterfaces();
    }
    
    public String getClientIp(HttpServletRequest request) {
        return serverVariableService.getClientIp(request);
    }
}
```

```java
// TuskLang server network
server_network: {
    # Network addresses
    local_address: @server.network.local_address()
    public_address: @server.network.public_address()
    hostname: @server.network.hostname()
    domain: @server.network.domain()
    
    # Network interfaces
    interfaces: @server.network.interfaces()
    
    # Client information
    client_ip: @server.network.client_ip()
    client_port: @server.network.client_port()
    user_agent: @server.network.user_agent()
}
```

## Server Configuration

```java
// Java server configuration access
@Component
public class ServerConfigurationService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    @Value("${server.servlet.context-path:}")
    private String contextPath;
    
    @Value("${server.ssl.enabled:false}")
    private boolean sslEnabled;
    
    @Value("${server.compression.enabled:false}")
    private boolean compressionEnabled;
    
    public ServerConfiguration getConfiguration() {
        return ServerConfiguration.builder()
            .contextPath(contextPath)
            .sslEnabled(sslEnabled)
            .compressionEnabled(compressionEnabled)
            .maxThreads(serverVariableService.getMaxThreads())
            .minThreads(serverVariableService.getMinThreads())
            .idleTimeout(serverVariableService.getIdleTimeout())
            .build();
    }
}
```

```java
// TuskLang server configuration
server_configuration: {
    # Servlet configuration
    context_path: @server.config.context_path()
    session_timeout: @server.config.session_timeout()
    
    # SSL configuration
    ssl_enabled: @server.config.ssl_enabled()
    ssl_protocol: @server.config.ssl_protocol()
    ssl_cipher_suite: @server.config.ssl_cipher_suite()
    
    # Thread configuration
    max_threads: @server.config.max_threads()
    min_threads: @server.config.min_threads()
    idle_timeout: @server.config.idle_timeout()
    
    # Compression configuration
    compression_enabled: @server.config.compression_enabled()
    compression_mime_types: @server.config.compression_mime_types()
}
```

## Server Metrics

```java
// Java server metrics collection
@Component
public class ServerMetricsService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public ServerMetrics getMetrics() {
        return ServerMetrics.builder()
            .activeConnections(serverVariableService.getActiveConnections())
            .totalConnections(serverVariableService.getTotalConnections())
            .requestsPerSecond(serverVariableService.getRequestsPerSecond())
            .averageResponseTime(serverVariableService.getAverageResponseTime())
            .errorRate(serverVariableService.getErrorRate())
            .build();
    }
    
    public void recordRequest(String path, long duration, int statusCode) {
        Timer.builder("server.requests")
            .tag("path", path)
            .tag("status", String.valueOf(statusCode))
            .register(meterRegistry)
            .record(duration, TimeUnit.MILLISECONDS);
    }
}
```

```java
// TuskLang server metrics
server_metrics: {
    # Connection metrics
    active_connections: @server.metrics.active_connections()
    total_connections: @server.metrics.total_connections()
    max_connections: @server.metrics.max_connections()
    
    # Request metrics
    requests_per_second: @server.metrics.requests_per_second()
    total_requests: @server.metrics.total_requests()
    average_response_time: @server.metrics.average_response_time()
    
    # Error metrics
    error_rate: @server.metrics.error_rate()
    total_errors: @server.metrics.total_errors()
    
    # Performance metrics
    throughput: @server.metrics.throughput()
    latency_p95: @server.metrics.latency_p95()
    latency_p99: @server.metrics.latency_p99()
}
```

## Server Health

```java
// Java server health monitoring
@Component
public class ServerHealthService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    public HealthStatus getHealthStatus() {
        double memoryUsage = serverVariableService.getMemoryUsagePercentage();
        double cpuUsage = serverVariableService.getCpuUsage();
        double diskUsage = serverVariableService.getDiskUsagePercentage();
        
        HealthStatus status = HealthStatus.HEALTHY;
        
        if (memoryUsage > 90 || cpuUsage > 90 || diskUsage > 90) {
            status = HealthStatus.CRITICAL;
        } else if (memoryUsage > 80 || cpuUsage > 80 || diskUsage > 80) {
            status = HealthStatus.WARNING;
        }
        
        return ServerHealth.builder()
            .status(status)
            .memoryUsage(memoryUsage)
            .cpuUsage(cpuUsage)
            .diskUsage(diskUsage)
            .uptime(serverVariableService.getUptime())
            .lastCheck(Instant.now())
            .build();
    }
}
```

```java
// TuskLang server health
server_health: {
    # Health status
    status: @server.health.status()
    is_healthy: @server.health.is_healthy()
    is_warning: @server.health.is_warning()
    is_critical: @server.health.is_critical()
    
    # Health metrics
    memory_usage: @server.health.memory_usage()
    cpu_usage: @server.health.cpu_usage()
    disk_usage: @server.health.disk_usage()
    
    # Health details
    uptime: @server.health.uptime()
    last_check: @server.health.last_check()
    health_score: @server.health.score()
}
```

## Server Clustering

```java
// Java server clustering information
@Component
public class ServerClusteringService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    public ClusterInfo getClusterInfo() {
        return ClusterInfo.builder()
            .clusterName(serverVariableService.getClusterName())
            .nodeId(serverVariableService.getNodeId())
            .nodeRole(serverVariableService.getNodeRole())
            .totalNodes(serverVariableService.getTotalNodes())
            .activeNodes(serverVariableService.getActiveNodes())
            .build();
    }
    
    public boolean isLeader() {
        return "leader".equals(serverVariableService.getNodeRole());
    }
    
    public boolean isFollower() {
        return "follower".equals(serverVariableService.getNodeRole());
    }
}
```

```java
// TuskLang server clustering
server_cluster: {
    # Cluster information
    cluster_name: @server.cluster.name()
    node_id: @server.cluster.node_id()
    node_role: @server.cluster.node_role()
    
    # Node status
    total_nodes: @server.cluster.total_nodes()
    active_nodes: @server.cluster.active_nodes()
    is_leader: @server.cluster.is_leader()
    is_follower: @server.cluster.is_follower()
    
    # Cluster health
    cluster_health: @server.cluster.health()
    quorum_status: @server.cluster.quorum_status()
}
```

## Server Security

```java
// Java server security information
@Component
public class ServerSecurityService {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    public SecurityInfo getSecurityInfo() {
        return SecurityInfo.builder()
            .sslEnabled(serverVariableService.isSslEnabled())
            .sslProtocol(serverVariableService.getSslProtocol())
            .sslCipherSuite(serverVariableService.getSslCipherSuite())
            .certificateExpiry(serverVariableService.getCertificateExpiry())
            .securityHeaders(serverVariableService.getSecurityHeaders())
            .build();
    }
    
    public boolean isSecureConnection() {
        return serverVariableService.isSslEnabled();
    }
    
    public List<String> getSecurityHeaders() {
        return serverVariableService.getSecurityHeaders();
    }
}
```

```java
// TuskLang server security
server_security: {
    # SSL/TLS information
    ssl_enabled: @server.security.ssl_enabled()
    ssl_protocol: @server.security.ssl_protocol()
    ssl_cipher_suite: @server.security.ssl_cipher_suite()
    certificate_expiry: @server.security.certificate_expiry()
    
    # Security headers
    security_headers: @server.security.headers()
    hsts_enabled: @server.security.hsts_enabled()
    csp_enabled: @server.security.csp_enabled()
    
    # Authentication
    auth_enabled: @server.security.auth_enabled()
    auth_type: @server.security.auth_type()
}
```

## Server Configuration Examples

```java
// Development server configuration
dev_server: {
    host: "localhost"
    port: 8080
    protocol: "http"
    environment: "development"
    
    # Development settings
    debug_enabled: true
    hot_reload: true
    cors_enabled: true
}

// Production server configuration
prod_server: {
    host: "0.0.0.0"
    port: 443
    protocol: "https"
    environment: "production"
    
    # Production settings
    ssl_enabled: true
    compression_enabled: true
    security_headers: true
}

// Testing server configuration
test_server: {
    host: "localhost"
    port: 0  # Random port
    protocol: "http"
    environment: "testing"
    
    # Testing settings
    ephemeral: true
    auto_cleanup: true
}
```

## Server Monitoring Integration

```java
// Integration with Micrometer for server metrics
@Component
public class ServerMetricsIntegration {
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    @EventListener
    public void handleServerEvent(ServerEvent event) {
        // Record server metrics
        Gauge.builder("server.connections.active")
            .register(meterRegistry, () -> event.getActiveConnections());
        
        Gauge.builder("server.memory.usage")
            .register(meterRegistry, () -> event.getMemoryUsage());
        
        Gauge.builder("server.cpu.usage")
            .register(meterRegistry, () -> event.getCpuUsage());
    }
}
```

```java
// Integration with Spring Boot Actuator
@Configuration
@EnableActuator
public class ServerActuatorConfig {
    
    @Bean
    public HealthIndicator serverHealthIndicator(ServerHealthService healthService) {
        return () -> {
            ServerHealth health = healthService.getHealthStatus();
            
            if (health.getStatus() == HealthStatus.HEALTHY) {
                return Health.up()
                    .withDetail("memory_usage", health.getMemoryUsage())
                    .withDetail("cpu_usage", health.getCpuUsage())
                    .withDetail("uptime", health.getUptime())
                    .build();
            } else {
                return Health.down()
                    .withDetail("status", health.getStatus())
                    .withDetail("memory_usage", health.getMemoryUsage())
                    .withDetail("cpu_usage", health.getCpuUsage())
                    .build();
            }
        };
    }
}
```

## Testing Server Variables

```java
// JUnit test for server variables
@SpringBootTest
class ServerVariableServiceTest {
    
    @Autowired
    private ServerVariableService serverVariableService;
    
    @Test
    void testServerInfo() {
        assertThat(serverVariableService.getHost()).isNotNull();
        assertThat(serverVariableService.getPort()).isPositive();
        assertThat(serverVariableService.getProtocol()).isIn("http", "https");
    }
    
    @Test
    void testServerResources() {
        ServerResources resources = serverVariableService.getResources();
        
        assertThat(resources.getTotalMemory()).isPositive();
        assertThat(resources.getFreeMemory()).isPositive();
        assertThat(resources.getUsedMemory()).isPositive();
        assertThat(resources.getAvailableProcessors()).isPositive();
    }
    
    @Test
    void testServerHealth() {
        ServerHealth health = serverVariableService.getHealthStatus();
        
        assertThat(health.getStatus()).isNotNull();
        assertThat(health.getMemoryUsage()).isBetween(0.0, 100.0);
        assertThat(health.getCpuUsage()).isBetween(0.0, 100.0);
    }
}
```

```java
// TuskLang server variable testing
test_server_variables: {
    # Test server information
    assert(@server.host() != "", "Server host should not be empty")
    assert(@server.port() > 0, "Server port should be positive")
    assert(@server.protocol() in ["http", "https"], "Protocol should be http or https")
    
    # Test server resources
    assert(@server.memory.total() > 0, "Total memory should be positive")
    assert(@server.memory.usage_percent() >= 0, "Memory usage should be non-negative")
    assert(@server.cpu.count() > 0, "CPU count should be positive")
    
    # Test server health
    assert(@server.health.status() in ["healthy", "warning", "critical"], "Health status should be valid")
    assert(@server.health.memory_usage() >= 0, "Memory usage should be non-negative")
}
```

## Best Practices

### 1. Environment-Specific Configuration
```java
// Use different server configurations per environment
@Configuration
@Profile("dev")
public class DevServerConfig {
    
    @Bean
    public ServerVariableService devServerVariableService() {
        return ServerVariableService.builder()
            .host("localhost")
            .port(8080)
            .protocol("http")
            .environment("development")
            .build();
    }
}

@Configuration
@Profile("prod")
public class ProdServerConfig {
    
    @Bean
    public ServerVariableService prodServerVariableService() {
        return ServerVariableService.builder()
            .host("0.0.0.0")
            .port(443)
            .protocol("https")
            .environment("production")
            .build();
    }
}
```

### 2. Health Check Integration
```java
// Integrate server health with application health checks
@Component
public class ServerHealthIndicator implements HealthIndicator {
    
    @Autowired
    private ServerHealthService healthService;
    
    @Override
    public Health health() {
        ServerHealth health = healthService.getHealthStatus();
        
        Health.Builder builder = new Health.Builder();
        
        if (health.getStatus() == HealthStatus.HEALTHY) {
            builder.up();
        } else {
            builder.down();
        }
        
        return builder
            .withDetail("memory_usage", health.getMemoryUsage())
            .withDetail("cpu_usage", health.getCpuUsage())
            .withDetail("uptime", health.getUptime())
            .build();
    }
}
```

### 3. Metrics Collection
```java
// Collect comprehensive server metrics
@Component
public class ServerMetricsCollector {
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    @Scheduled(fixedRate = 30000) // Every 30 seconds
    public void collectMetrics() {
        ServerMetrics metrics = serverVariableService.getMetrics();
        
        Gauge.builder("server.connections.active")
            .register(meterRegistry, () -> metrics.getActiveConnections());
        
        Gauge.builder("server.memory.usage")
            .register(meterRegistry, () -> metrics.getMemoryUsagePercentage());
        
        Gauge.builder("server.cpu.usage")
            .register(meterRegistry, () -> metrics.getCpuUsage());
        
        Timer.builder("server.response.time")
            .register(meterRegistry, () -> metrics.getAverageResponseTime());
    }
}
```

The `@server` operator in Java provides comprehensive access to server-specific variables and configuration, enabling applications to adapt their behavior based on the server environment, monitor server health, and collect performance metrics for enterprise-grade applications. 
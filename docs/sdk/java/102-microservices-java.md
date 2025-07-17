# 🔧 Microservices Patterns with TuskLang Java

**"We don't bow to any king" - Microservices Edition**

TuskLang Java enables sophisticated microservices architectures with built-in support for service discovery, API gateways, distributed tracing, and fault tolerance. Build resilient, scalable microservices that communicate seamlessly.

## 🎯 Microservices Architecture Overview

### Service Decomposition Pattern
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cloud.client.discovery.EnableDiscoveryClient;

@SpringBootApplication
@EnableDiscoveryClient
public class UserServiceApplication {
    
    @Bean
    public TuskConfig tuskConfig() {
        TuskLang parser = new TuskLang();
        return parser.parseFile("user-service.tsk", TuskConfig.class);
    }
    
    public static void main(String[] args) {
        SpringApplication.run(UserServiceApplication.class, args);
    }
}

// Microservice configuration
@TuskConfig
public class UserServiceConfig {
    private String serviceName;
    private String version;
    private String environment;
    private ApiConfig api;
    private DatabaseConfig database;
    private MessagingConfig messaging;
    private SecurityConfig security;
    private MonitoringConfig monitoring;
    
    // Getters and setters
    public String getServiceName() { return serviceName; }
    public void setServiceName(String serviceName) { this.serviceName = serviceName; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public String getEnvironment() { return environment; }
    public void setEnvironment(String environment) { this.environment = environment; }
    
    public ApiConfig getApi() { return api; }
    public void setApi(ApiConfig api) { this.api = api; }
    
    public DatabaseConfig getDatabase() { return database; }
    public void setDatabase(DatabaseConfig database) { this.database = database; }
    
    public MessagingConfig getMessaging() { return messaging; }
    public void setMessaging(MessagingConfig messaging) { this.messaging = messaging; }
    
    public SecurityConfig getSecurity() { return security; }
    public void setSecurity(SecurityConfig security) { this.security = security; }
    
    public MonitoringConfig getMonitoring() { return monitoring; }
    public void setMonitoring(MonitoringConfig monitoring) { this.monitoring = monitoring; }
}

@TuskConfig
public class ApiConfig {
    private String basePath;
    private int port;
    private String contextPath;
    private CorsConfig cors;
    private RateLimitConfig rateLimit;
    private DocumentationConfig documentation;
    
    // Getters and setters
    public String getBasePath() { return basePath; }
    public void setBasePath(String basePath) { this.basePath = basePath; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public String getContextPath() { return contextPath; }
    public void setContextPath(String contextPath) { this.contextPath = contextPath; }
    
    public CorsConfig getCors() { return cors; }
    public void setCors(CorsConfig cors) { this.cors = cors; }
    
    public RateLimitConfig getRateLimit() { return rateLimit; }
    public void setRateLimit(RateLimitConfig rateLimit) { this.rateLimit = rateLimit; }
    
    public DocumentationConfig getDocumentation() { return documentation; }
    public void setDocumentation(DocumentationConfig documentation) { this.documentation = documentation; }
}

@TuskConfig
public class CorsConfig {
    private boolean enabled;
    private List<String> allowedOrigins;
    private List<String> allowedMethods;
    private List<String> allowedHeaders;
    private boolean allowCredentials;
    private int maxAge;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public List<String> getAllowedOrigins() { return allowedOrigins; }
    public void setAllowedOrigins(List<String> allowedOrigins) { this.allowedOrigins = allowedOrigins; }
    
    public List<String> getAllowedMethods() { return allowedMethods; }
    public void setAllowedMethods(List<String> allowedMethods) { this.allowedMethods = allowedMethods; }
    
    public List<String> getAllowedHeaders() { return allowedHeaders; }
    public void setAllowedHeaders(List<String> allowedHeaders) { this.allowedHeaders = allowedHeaders; }
    
    public boolean isAllowCredentials() { return allowCredentials; }
    public void setAllowCredentials(boolean allowCredentials) { this.allowCredentials = allowCredentials; }
    
    public int getMaxAge() { return maxAge; }
    public void setMaxAge(int maxAge) { this.maxAge = maxAge; }
}

@TuskConfig
public class RateLimitConfig {
    private boolean enabled;
    private int requestsPerMinute;
    private int burstLimit;
    private String strategy;
    private Map<String, Integer> customLimits;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public int getRequestsPerMinute() { return requestsPerMinute; }
    public void setRequestsPerMinute(int requestsPerMinute) { this.requestsPerMinute = requestsPerMinute; }
    
    public int getBurstLimit() { return burstLimit; }
    public void setBurstLimit(int burstLimit) { this.burstLimit = burstLimit; }
    
    public String getStrategy() { return strategy; }
    public void setStrategy(String strategy) { this.strategy = strategy; }
    
    public Map<String, Integer> getCustomLimits() { return customLimits; }
    public void setCustomLimits(Map<String, Integer> customLimits) { this.customLimits = customLimits; }
}

@TuskConfig
public class DocumentationConfig {
    private boolean enabled;
    private String title;
    private String description;
    private String version;
    private String contactEmail;
    private String license;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    
    public String getDescription() { return description; }
    public void setDescription(String description) { this.description = description; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public String getContactEmail() { return contactEmail; }
    public void setContactEmail(String contactEmail) { this.contactEmail = contactEmail; }
    
    public String getLicense() { return license; }
    public void setLicense(String license) { this.license = license; }
}

@TuskConfig
public class DatabaseConfig {
    private String type;
    private String host;
    private int port;
    private String database;
    private String username;
    private String password;
    private ConnectionPoolConfig connectionPool;
    private MigrationConfig migration;
    
    // Getters and setters
    public String getType() { return type; }
    public void setType(String type) { this.type = type; }
    
    public String getHost() { return host; }
    public void setHost(String host) { this.host = host; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public String getDatabase() { return database; }
    public void setDatabase(String database) { this.database = database; }
    
    public String getUsername() { return username; }
    public void setUsername(String username) { this.username = username; }
    
    public String getPassword() { return password; }
    public void setPassword(String password) { this.password = password; }
    
    public ConnectionPoolConfig getConnectionPool() { return connectionPool; }
    public void setConnectionPool(ConnectionPoolConfig connectionPool) { this.connectionPool = connectionPool; }
    
    public MigrationConfig getMigration() { return migration; }
    public void setMigration(MigrationConfig migration) { this.migration = migration; }
}

@TuskConfig
public class ConnectionPoolConfig {
    private int initialSize;
    private int maxSize;
    private int minIdle;
    private int maxIdle;
    private int maxWait;
    private boolean testOnBorrow;
    private String validationQuery;
    
    // Getters and setters
    public int getInitialSize() { return initialSize; }
    public void setInitialSize(int initialSize) { this.initialSize = initialSize; }
    
    public int getMaxSize() { return maxSize; }
    public void setMaxSize(int maxSize) { this.maxSize = maxSize; }
    
    public int getMinIdle() { return minIdle; }
    public void setMinIdle(int minIdle) { this.minIdle = minIdle; }
    
    public int getMaxIdle() { return maxIdle; }
    public void setMaxIdle(int maxIdle) { this.maxIdle = maxIdle; }
    
    public int getMaxWait() { return maxWait; }
    public void setMaxWait(int maxWait) { this.maxWait = maxWait; }
    
    public boolean isTestOnBorrow() { return testOnBorrow; }
    public void setTestOnBorrow(boolean testOnBorrow) { this.testOnBorrow = testOnBorrow; }
    
    public String getValidationQuery() { return validationQuery; }
    public void setValidationQuery(String validationQuery) { this.validationQuery = validationQuery; }
}

@TuskConfig
public class MigrationConfig {
    private boolean enabled;
    private String location;
    private String baselineOnMigrate;
    private boolean validateOnMigrate;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public String getLocation() { return location; }
    public void setLocation(String location) { this.location = location; }
    
    public String getBaselineOnMigrate() { return baselineOnMigrate; }
    public void setBaselineOnMigrate(String baselineOnMigrate) { this.baselineOnMigrate = baselineOnMigrate; }
    
    public boolean isValidateOnMigrate() { return validateOnMigrate; }
    public void setValidateOnMigrate(boolean validateOnMigrate) { this.validateOnMigrate = validateOnMigrate; }
}

@TuskConfig
public class MessagingConfig {
    private String broker;
    private List<String> brokers;
    private String topic;
    private String groupId;
    private int partitions;
    private int replicationFactor;
    private RetentionConfig retention;
    
    // Getters and setters
    public String getBroker() { return broker; }
    public void setBroker(String broker) { this.broker = broker; }
    
    public List<String> getBrokers() { return brokers; }
    public void setBrokers(List<String> brokers) { this.brokers = brokers; }
    
    public String getTopic() { return topic; }
    public void setTopic(String topic) { this.topic = topic; }
    
    public String getGroupId() { return groupId; }
    public void setGroupId(String groupId) { this.groupId = groupId; }
    
    public int getPartitions() { return partitions; }
    public void setPartitions(int partitions) { this.partitions = partitions; }
    
    public int getReplicationFactor() { return replicationFactor; }
    public void setReplicationFactor(int replicationFactor) { this.replicationFactor = replicationFactor; }
    
    public RetentionConfig getRetention() { return retention; }
    public void setRetention(RetentionConfig retention) { this.retention = retention; }
}

@TuskConfig
public class RetentionConfig {
    private long timeMs;
    private long sizeBytes;
    private String cleanupPolicy;
    
    // Getters and setters
    public long getTimeMs() { return timeMs; }
    public void setTimeMs(long timeMs) { this.timeMs = timeMs; }
    
    public long getSizeBytes() { return sizeBytes; }
    public void setSizeBytes(long sizeBytes) { this.sizeBytes = sizeBytes; }
    
    public String getCleanupPolicy() { return cleanupPolicy; }
    public void setCleanupPolicy(String cleanupPolicy) { this.cleanupPolicy = cleanupPolicy; }
}

@TuskConfig
public class SecurityConfig {
    private boolean enabled;
    private String jwtSecret;
    private int jwtExpiration;
    private List<String> allowedRoles;
    private OAuthConfig oauth;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public String getJwtSecret() { return jwtSecret; }
    public void setJwtSecret(String jwtSecret) { this.jwtSecret = jwtSecret; }
    
    public int getJwtExpiration() { return jwtExpiration; }
    public void setJwtExpiration(int jwtExpiration) { this.jwtExpiration = jwtExpiration; }
    
    public List<String> getAllowedRoles() { return allowedRoles; }
    public void setAllowedRoles(List<String> allowedRoles) { this.allowedRoles = allowedRoles; }
    
    public OAuthConfig getOauth() { return oauth; }
    public void setOauth(OAuthConfig oauth) { this.oauth = oauth; }
}

@TuskConfig
public class OAuthConfig {
    private String clientId;
    private String clientSecret;
    private String issuer;
    private List<String> scopes;
    
    // Getters and setters
    public String getClientId() { return clientId; }
    public void setClientId(String clientId) { this.clientId = clientId; }
    
    public String getClientSecret() { return clientSecret; }
    public void setClientSecret(String clientSecret) { this.clientSecret = clientSecret; }
    
    public String getIssuer() { return issuer; }
    public void setIssuer(String issuer) { this.issuer = issuer; }
    
    public List<String> getScopes() { return scopes; }
    public void setScopes(List<String> scopes) { this.scopes = scopes; }
}

@TuskConfig
public class MonitoringConfig {
    private String prometheusEndpoint;
    private boolean enabled;
    private Map<String, String> labels;
    private int scrapeInterval;
    private AlertingConfig alerting;
    
    // Getters and setters
    public String getPrometheusEndpoint() { return prometheusEndpoint; }
    public void setPrometheusEndpoint(String prometheusEndpoint) { this.prometheusEndpoint = prometheusEndpoint; }
    
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public Map<String, String> getLabels() { return labels; }
    public void setLabels(Map<String, String> labels) { this.labels = labels; }
    
    public int getScrapeInterval() { return scrapeInterval; }
    public void setScrapeInterval(int scrapeInterval) { this.scrapeInterval = scrapeInterval; }
    
    public AlertingConfig getAlerting() { return alerting; }
    public void setAlerting(AlertingConfig alerting) { this.alerting = alerting; }
}

@TuskConfig
public class AlertingConfig {
    private String slackWebhook;
    private String emailEndpoint;
    private Map<String, AlertRule> rules;
    
    // Getters and setters
    public String getSlackWebhook() { return slackWebhook; }
    public void setSlackWebhook(String slackWebhook) { this.slackWebhook = slackWebhook; }
    
    public String getEmailEndpoint() { return emailEndpoint; }
    public void setEmailEndpoint(String emailEndpoint) { this.emailEndpoint = emailEndpoint; }
    
    public Map<String, AlertRule> getRules() { return rules; }
    public void setRules(Map<String, AlertRule> rules) { this.rules = rules; }
}

@TuskConfig
public class AlertRule {
    private String condition;
    private String threshold;
    private String duration;
    private List<String> channels;
    private String severity;
    
    // Getters and setters
    public String getCondition() { return condition; }
    public void setCondition(String condition) { this.condition = condition; }
    
    public String getThreshold() { return threshold; }
    public void setThreshold(String threshold) { this.threshold = threshold; }
    
    public String getDuration() { return duration; }
    public void setDuration(String duration) { this.duration = duration; }
    
    public List<String> getChannels() { return channels; }
    public void setChannels(List<String> channels) { this.channels = channels; }
    
    public String getSeverity() { return severity; }
    public void setSeverity(String severity) { this.severity = severity; }
}
```

## 🏗️ Microservices TuskLang Configuration

### user-service.tsk
```tsk
# User Service Microservice Configuration
[service]
name: "user-service"
version: "2.1.0"
environment: @env("ENVIRONMENT", "production")

[api]
base_path: "/api/v1"
port: 8080
context_path: "/user-service"

[cors]
enabled: true
allowed_origins: [
    "https://app.example.com",
    "https://admin.example.com"
]
allowed_methods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
allowed_headers: ["Content-Type", "Authorization", "X-Request-ID"]
allow_credentials: true
max_age: 3600

[rate_limit]
enabled: true
requests_per_minute: 1000
burst_limit: 100
strategy: "token_bucket"
custom_limits {
    "admin": 2000
    "premium": 1500
}

[documentation]
enabled: true
title: "User Service API"
description: "Microservice for user management operations"
version: "2.1.0"
contact_email: "api@example.com"
license: "MIT"

[database]
type: "postgresql"
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
database: @env("DB_NAME", "user_service")
username: @env("DB_USER", "postgres")
password: @env.secure("DB_PASSWORD")

[connection_pool]
initial_size: 5
max_size: 20
min_idle: 5
max_idle: 10
max_wait: 30000
test_on_borrow: true
validation_query: "SELECT 1"

[migration]
enabled: true
location: "classpath:db/migration"
baseline_on_migrate: "true"
validate_on_migrate: true

[messaging]
broker: "kafka"
brokers: [
    "kafka-1:9092",
    "kafka-2:9092",
    "kafka-3:9092"
]
topic: "user-events"
group_id: "user-service-group"
partitions: 6
replication_factor: 3

[retention]
time_ms: 604800000
size_bytes: 1073741824
cleanup_policy: "delete"

[security]
enabled: true
jwt_secret: @env.secure("JWT_SECRET")
jwt_expiration: 3600
allowed_roles: ["USER", "ADMIN", "MODERATOR"]

[oauth]
client_id: @env("OAUTH_CLIENT_ID")
client_secret: @env.secure("OAUTH_CLIENT_SECRET")
issuer: "https://auth.example.com"
scopes: [
    "read:users",
    "write:users",
    "delete:users"
]

[monitoring]
prometheus_endpoint: "/actuator/prometheus"
enabled: true
labels {
    service: "user-service"
    version: "2.1.0"
    environment: @env("ENVIRONMENT", "production")
}
scrape_interval: 15

[alerting]
slack_webhook: @env.secure("SLACK_WEBHOOK")
email_endpoint: @env("ALERT_EMAIL")

[rules]
high_error_rate {
    condition: "error_rate > 0.05"
    threshold: "5%"
    duration: "5m"
    channels: ["slack", "email"]
    severity: "critical"
}

high_latency {
    condition: "p95_latency > 2000"
    threshold: "2s"
    duration: "3m"
    channels: ["slack"]
    severity: "warning"
}

# Dynamic microservice configuration
[monitoring]
user_count: @query("SELECT COUNT(*) FROM users WHERE active = true")
recent_signups: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("24h"))
active_sessions: @query("SELECT COUNT(*) FROM user_sessions WHERE expires_at > ?", @date.now())
api_requests: @metrics("http_requests_total", 0)
error_rate: @metrics("http_requests_errors_total", 0)
response_time: @metrics("http_request_duration_seconds", 0)
```

## 🔗 Service Communication

### API Gateway Configuration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@TuskConfig
public class ApiGatewayConfig {
    private String gatewayUrl;
    private String serviceName;
    private RouteConfig routes;
    private LoadBalancerConfig loadBalancer;
    private CircuitBreakerConfig circuitBreaker;
    private AuthConfig auth;
    
    // Getters and setters
    public String getGatewayUrl() { return gatewayUrl; }
    public void setGatewayUrl(String gatewayUrl) { this.gatewayUrl = gatewayUrl; }
    
    public String getServiceName() { return serviceName; }
    public void setServiceName(String serviceName) { this.serviceName = serviceName; }
    
    public RouteConfig getRoutes() { return routes; }
    public void setRoutes(RouteConfig routes) { this.routes = routes; }
    
    public LoadBalancerConfig getLoadBalancer() { return loadBalancer; }
    public void setLoadBalancer(LoadBalancerConfig loadBalancer) { this.loadBalancer = loadBalancer; }
    
    public CircuitBreakerConfig getCircuitBreaker() { return circuitBreaker; }
    public void setCircuitBreaker(CircuitBreakerConfig circuitBreaker) { this.circuitBreaker = circuitBreaker; }
    
    public AuthConfig getAuth() { return auth; }
    public void setAuth(AuthConfig auth) { this.auth = auth; }
}

@TuskConfig
public class RouteConfig {
    private String path;
    private String serviceId;
    private String method;
    private int timeout;
    private boolean stripPrefix;
    private Map<String, String> headers;
    private List<String> filters;
    
    // Getters and setters
    public String getPath() { return path; }
    public void setPath(String path) { this.path = path; }
    
    public String getServiceId() { return serviceId; }
    public void setServiceId(String serviceId) { this.serviceId = serviceId; }
    
    public String getMethod() { return method; }
    public void setMethod(String method) { this.method = method; }
    
    public int getTimeout() { return timeout; }
    public void setTimeout(int timeout) { this.timeout = timeout; }
    
    public boolean isStripPrefix() { return stripPrefix; }
    public void setStripPrefix(boolean stripPrefix) { this.stripPrefix = stripPrefix; }
    
    public Map<String, String> getHeaders() { return headers; }
    public void setHeaders(Map<String, String> headers) { this.headers = headers; }
    
    public List<String> getFilters() { return filters; }
    public void setFilters(List<String> filters) { this.filters = filters; }
}

@TuskConfig
public class LoadBalancerConfig {
    private String algorithm;
    private int maxConnections;
    private boolean healthCheckEnabled;
    private String healthCheckPath;
    private int healthCheckInterval;
    
    // Getters and setters
    public String getAlgorithm() { return algorithm; }
    public void setAlgorithm(String algorithm) { this.algorithm = algorithm; }
    
    public int getMaxConnections() { return maxConnections; }
    public void setMaxConnections(int maxConnections) { this.maxConnections = maxConnections; }
    
    public boolean isHealthCheckEnabled() { return healthCheckEnabled; }
    public void setHealthCheckEnabled(boolean healthCheckEnabled) { this.healthCheckEnabled = healthCheckEnabled; }
    
    public String getHealthCheckPath() { return healthCheckPath; }
    public void setHealthCheckPath(String healthCheckPath) { this.healthCheckPath = healthCheckPath; }
    
    public int getHealthCheckInterval() { return healthCheckInterval; }
    public void setHealthCheckInterval(int healthCheckInterval) { this.healthCheckInterval = healthCheckInterval; }
}

@TuskConfig
public class CircuitBreakerConfig {
    private boolean enabled;
    private int failureThreshold;
    private int recoveryTimeout;
    private int expectedResponseTime;
    private String fallbackMethod;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public int getFailureThreshold() { return failureThreshold; }
    public void setFailureThreshold(int failureThreshold) { this.failureThreshold = failureThreshold; }
    
    public int getRecoveryTimeout() { return recoveryTimeout; }
    public void setRecoveryTimeout(int recoveryTimeout) { this.recoveryTimeout = recoveryTimeout; }
    
    public int getExpectedResponseTime() { return expectedResponseTime; }
    public void setExpectedResponseTime(int expectedResponseTime) { this.expectedResponseTime = expectedResponseTime; }
    
    public String getFallbackMethod() { return fallbackMethod; }
    public void setFallbackMethod(String fallbackMethod) { this.fallbackMethod = fallbackMethod; }
}

@TuskConfig
public class AuthConfig {
    private boolean enabled;
    private String type;
    private String secret;
    private String issuer;
    private List<String> requiredScopes;
    private List<String> excludedPaths;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public String getType() { return type; }
    public void setType(String type) { this.type = type; }
    
    public String getSecret() { return secret; }
    public void setSecret(String secret) { this.secret = secret; }
    
    public String getIssuer() { return issuer; }
    public void setIssuer(String issuer) { this.issuer = issuer; }
    
    public List<String> getRequiredScopes() { return requiredScopes; }
    public void setRequiredScopes(List<String> requiredScopes) { this.requiredScopes = requiredScopes; }
    
    public List<String> getExcludedPaths() { return excludedPaths; }
    public void setExcludedPaths(List<String> excludedPaths) { this.excludedPaths = excludedPaths; }
}
```

### api-gateway.tsk
```tsk
# API Gateway Configuration
[gateway]
url: @env("GATEWAY_URL", "http://gateway:8080")
service_name: "api-gateway"

[routes]
user_service {
    path: "/api/users"
    service_id: "user-service"
    method: "GET,POST,PUT,DELETE"
    timeout: 10000
    strip_prefix: false
    headers {
        "X-Service-Version": "2.1.0"
        "X-Request-ID": @env("REQUEST_ID")
    }
    filters: ["auth", "rate-limit", "logging"]
}

order_service {
    path: "/api/orders"
    service_id: "order-service"
    method: "GET,POST,PUT,DELETE"
    timeout: 15000
    strip_prefix: false
    filters: ["auth", "rate-limit", "logging"]
}

payment_service {
    path: "/api/payments"
    service_id: "payment-service"
    method: "GET,POST"
    timeout: 20000
    strip_prefix: false
    filters: ["auth", "rate-limit", "logging", "encryption"]
}

[load_balancer]
algorithm: "round_robin"
max_connections: 1000
health_check_enabled: true
health_check_path: "/health"
health_check_interval: 30

[circuit_breaker]
enabled: true
failure_threshold: 5
recovery_timeout: 60000
expected_response_time: 2000
fallback_method: "handleFallback"

[auth]
enabled: true
type: "jwt"
secret: @env.secure("JWT_SECRET")
issuer: "api-gateway"
required_scopes: [
    "read:users",
    "write:users",
    "read:orders",
    "write:orders"
]
excluded_paths: [
    "/health",
    "/metrics",
    "/actuator/*"
]

# Gateway monitoring
[monitoring]
active_routes: @query("SELECT COUNT(*) FROM routes WHERE status = 'active'")
request_count: @metrics("gateway_requests_total", 0)
response_time: @metrics("gateway_response_time_ms", 0)
error_rate: @metrics("gateway_error_rate_percent", 0)
circuit_breaker_status: @metrics("circuit_breaker_status", 0)
```

## 📊 Service Discovery

### Service Registry Configuration
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@TuskConfig
public class ServiceRegistryConfig {
    private String registryType;
    private String registryUrl;
    private String serviceName;
    private String serviceId;
    private int port;
    private String healthCheckUrl;
    private Map<String, String> metadata;
    private HeartbeatConfig heartbeat;
    
    // Getters and setters
    public String getRegistryType() { return registryType; }
    public void setRegistryType(String registryType) { this.registryType = registryType; }
    
    public String getRegistryUrl() { return registryUrl; }
    public void setRegistryUrl(String registryUrl) { this.registryUrl = registryUrl; }
    
    public String getServiceName() { return serviceName; }
    public void setServiceName(String serviceName) { this.serviceName = serviceName; }
    
    public String getServiceId() { return serviceId; }
    public void setServiceId(String serviceId) { this.serviceId = serviceId; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public String getHealthCheckUrl() { return healthCheckUrl; }
    public void setHealthCheckUrl(String healthCheckUrl) { this.healthCheckUrl = healthCheckUrl; }
    
    public Map<String, String> getMetadata() { return metadata; }
    public void setMetadata(Map<String, String> metadata) { this.metadata = metadata; }
    
    public HeartbeatConfig getHeartbeat() { return heartbeat; }
    public void setHeartbeat(HeartbeatConfig heartbeat) { this.heartbeat = heartbeat; }
}

@TuskConfig
public class HeartbeatConfig {
    private int interval;
    private int timeout;
    private boolean enabled;
    private String path;
    
    // Getters and setters
    public int getInterval() { return interval; }
    public void setInterval(int interval) { this.interval = interval; }
    
    public int getTimeout() { return timeout; }
    public void setTimeout(int timeout) { this.timeout = timeout; }
    
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public String getPath() { return path; }
    public void setPath(String path) { this.path = path; }
}
```

### service-registry.tsk
```tsk
[registry]
type: "consul"
url: @env("CONSUL_URL", "http://consul:8500")
service_name: "user-service"
service_id: @env("HOSTNAME", "user-service-1")
port: 8080
health_check_url: "http://localhost:8080/health"

[metadata]
version: "2.1.0"
environment: @env("ENVIRONMENT", "production")
team: "identity"
data_center: @env("DATACENTER", "us-east-1")
instance_type: @env("INSTANCE_TYPE", "t3.medium")

[heartbeat]
interval: 30
timeout: 10
enabled: true
path: "/heartbeat"

# Service discovery monitoring
[monitoring]
registered_services: @query("SELECT COUNT(*) FROM services WHERE status = 'passing'")
service_instances: @query("SELECT COUNT(*) FROM service_instances WHERE healthy = true")
registry_health: @http("GET", "http://consul:8500/v1/status/leader")
```

## 🎯 Best Practices

### 1. Service Design
- Follow single responsibility principle
- Design for failure
- Implement proper error handling
- Use appropriate data formats

### 2. API Design
- Use RESTful principles
- Implement proper versioning
- Design for backward compatibility
- Use appropriate HTTP status codes

### 3. Communication
- Use asynchronous messaging
- Implement circuit breakers
- Use service discovery
- Monitor service health

### 4. Data Management
- Use database per service
- Implement eventual consistency
- Use event sourcing where appropriate
- Implement proper data validation

### 5. Security
- Implement proper authentication
- Use authorization
- Encrypt sensitive data
- Implement rate limiting

## 🔧 Troubleshooting

### Common Issues

1. **Service Discovery Failures**
   - Check registry connectivity
   - Verify service registration
   - Review health check configuration
   - Check network connectivity

2. **API Gateway Issues**
   - Monitor route configuration
   - Check load balancer health
   - Review circuit breaker settings
   - Verify authentication configuration

3. **Database Connectivity**
   - Check connection pool settings
   - Monitor database performance
   - Review migration status
   - Verify credentials

4. **Message Queue Problems**
   - Monitor consumer lag
   - Check partition distribution
   - Review retention policies
   - Verify broker connectivity

### Debug Commands

```bash
# Check service discovery
curl -X GET http://consul:8500/v1/catalog/services

# Test API gateway
curl -X GET http://gateway:8080/api/users

# Check service health
curl -X GET http://user-service:8080/health

# Monitor message queue
kafka-consumer-groups.sh --bootstrap-server kafka:9092 --describe --group user-service-group
```

## 🚀 Next Steps

1. **Implement service mesh** for traffic management
2. **Set up distributed tracing** for observability
3. **Configure monitoring** and alerting
4. **Implement CI/CD** pipeline
5. **Test failure scenarios** and recovery

---

**Ready to build resilient microservices with TuskLang Java? The future of distributed systems is here, and it's microservices!** 
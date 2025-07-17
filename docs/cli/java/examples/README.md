# Examples

Complete examples and workflows for TuskLang Java CLI.

## Quick Examples

### Basic Usage Patterns

```bash
# Initialize a new project
tsk init my-project
cd my-project

# Start development server
tsk serve 8080

# Run tests
tsk test all

# Compile configuration
tsk config compile ./
```

### Database Operations

```bash
# Initialize SQLite database
tsk db init

# Run migrations
tsk db migrate migrations/001_create_users.sql

# Check database status
tsk db status --performance

# Create backup
tsk db backup backup_$(date +%Y%m%d).sql
```

### Configuration Management

```bash
# Get configuration value
tsk config get server.port

# Validate configuration
tsk config validate ./

# Compile to binary format
tsk config compile ./

# Generate documentation
tsk config docs ./
```

## Complete Workflows

### [Web Application Development](./web-app-development.md)

Complete workflow for building a web application with TuskLang Java CLI.

### [Microservice Setup](./microservice-setup.md)

Step-by-step guide for setting up microservices with database and caching.

### [CI/CD Pipeline](./ci-cd-pipeline.md)

Automated testing and deployment pipeline using TuskLang CLI commands.

### [Production Deployment](./production-deployment.md)

Production-ready deployment with monitoring and backup strategies.

## Framework Integrations

### [Spring Boot Integration](./spring-boot-integration.md)

Complete Spring Boot application with TuskLang configuration and database management.

### [Micronaut Integration](./micronaut-integration.md)

Micronaut application setup with TuskLang CLI for configuration and testing.

### [Quarkus Integration](./quarkus-integration.md)

Quarkus native application with TuskLang binary compilation and optimization.

## Advanced Patterns

### [Performance Tuning](./performance-tuning.md)

Optimization strategies for high-performance applications.

### [Security Hardening](./security-hardening.md)

Security best practices and configuration for production environments.

### [Monitoring and Alerting](./monitoring-alerting.md)

Comprehensive monitoring setup with TuskLang CLI integration.

## Language-Specific Examples

### Java Concurrency

```java
// Thread-safe configuration access
public class ConfigManager {
    private static final PeanutConfig CONFIG = new PeanutConfig();
    private static final Map<String, Object> CACHED_CONFIG;
    
    static {
        try {
            CACHED_CONFIG = CONFIG.load();
        } catch (IOException e) {
            throw new RuntimeException("Failed to load config", e);
        }
    }
    
    public static Map<String, Object> getConfig() {
        return CACHED_CONFIG;
    }
}
```

### Spring Boot Auto-Configuration

```java
@Configuration
@EnableConfigurationProperties
public class TuskLangAutoConfiguration {
    
    @Bean
    @ConditionalOnMissingBean
    public PeanutConfig peanutConfig() {
        return new PeanutConfig(true, true);
    }
    
    @Bean
    @ConditionalOnMissingBean
    public DatabaseCommands databaseCommands(PeanutConfig config) {
        return new DatabaseCommands(config);
    }
}
```

### Maven Integration

```xml
<plugin>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-maven-plugin</artifactId>
    <version>2.0.0</version>
    <executions>
        <execution>
            <goals>
                <goal>compile-config</goal>
                <goal>run-tests</goal>
            </goals>
        </execution>
    </executions>
</plugin>
```

## Testing Examples

### Unit Testing

```java
@Test
public void testDatabaseConnection() {
    DatabaseCommands db = new DatabaseCommands();
    DatabaseStatus status = db.status();
    assertTrue(status.isConnected());
    assertTrue(status.getResponseTimeMs() < 100);
}
```

### Integration Testing

```java
@SpringBootTest
@TestPropertySource(properties = {
    "tusk.config.auto-load=true",
    "tusk.config.watch=false"
})
class DatabaseIntegrationTest {
    
    @Autowired
    private DatabaseCommands db;
    
    @Test
    void testMigrationWorkflow() {
        // Initialize database
        db.init();
        
        // Run migration
        db.migrate("migrations/001_create_users.sql");
        
        // Verify schema
        DatabaseStatus status = db.status();
        assertTrue(status.isConnected());
    }
}
```

## Performance Examples

### Benchmarking

```bash
# Benchmark configuration loading
tsk config benchmark

# Benchmark database operations
tsk db status --performance

# Benchmark binary compilation
tsk binary benchmark app.tsk
```

### Optimization

```java
// Optimized configuration access
public class OptimizedConfig {
    private static final PeanutConfig CONFIG = new PeanutConfig(true, false);
    private static final Map<String, Object> SETTINGS;
    
    static {
        try {
            SETTINGS = CONFIG.load();
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }
    
    public static <T> T get(String key, Class<T> type, T defaultValue) {
        Object value = SETTINGS.get(key);
        return type.isInstance(value) ? type.cast(value) : defaultValue;
    }
}
```

## Security Examples

### Secure Configuration

```ini
# production.peanuts
[security]
encryption_enabled: true
key_rotation_days: 30
audit_logging: true

[database]
url: "${DB_URL}"
username: "${DB_USER}"
password: "${DB_PASS}"
```

### Access Control

```java
@Component
public class SecureConfigAccess {
    
    private final PeanutConfig config;
    private final SecurityContext securityContext;
    
    public SecureConfigAccess(PeanutConfig config, SecurityContext securityContext) {
        this.config = config;
        this.securityContext = securityContext;
    }
    
    public <T> T getSecureValue(String key, Class<T> type, T defaultValue) {
        if (!securityContext.hasPermission("config.read." + key)) {
            throw new AccessDeniedException("No permission to read: " + key);
        }
        
        return config.get(key, type, defaultValue, "/path/to/project");
    }
}
```

## Troubleshooting Examples

### Debug Configuration

```bash
# Enable debug mode
export TSK_LOG_LEVEL=DEBUG
tsk config check --verbose

# Check specific configuration
tsk config get database.url --verbose

# Validate configuration hierarchy
tsk config validate ./ --json
```

### Database Troubleshooting

```bash
# Check database connectivity
tsk db status --verbose

# Test connection with timeout
tsk db status --timeout 60

# Analyze slow queries
tsk db console
> EXPLAIN QUERY PLAN SELECT * FROM users WHERE email = 'test@example.com';
```

## Real-World Scenarios

### E-commerce Application

Complete e-commerce application setup with:
- Product catalog database
- User authentication
- Order processing
- Payment integration
- Inventory management

### API Gateway

Microservices API gateway with:
- Service discovery
- Load balancing
- Rate limiting
- Authentication
- Monitoring

### Data Processing Pipeline

ETL pipeline with:
- Data extraction
- Transformation rules
- Loading strategies
- Error handling
- Performance monitoring

## Best Practices

### Configuration Management

1. **Use hierarchical configuration** for environment-specific settings
2. **Compile to binary format** for production performance
3. **Implement proper validation** for configuration integrity
4. **Use environment variables** for sensitive data
5. **Version control** configuration changes

### Database Operations

1. **Use connection pooling** for optimal performance
2. **Implement proper migrations** for schema changes
3. **Regular backups** with automated scheduling
4. **Monitor performance** with detailed metrics
5. **Handle transactions** properly

### Testing Strategy

1. **Unit tests** for individual components
2. **Integration tests** for database operations
3. **Performance tests** for critical paths
4. **Security tests** for configuration access
5. **End-to-end tests** for complete workflows

## Next Steps

- [Web Application Development](./web-app-development.md)
- [Spring Boot Integration](./spring-boot-integration.md)
- [Performance Tuning](./performance-tuning.md)
- [Production Deployment](./production-deployment.md) 
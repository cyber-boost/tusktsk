# Testing TuskLang in Java Applications

This guide covers comprehensive testing strategies for TuskLang configurations in Java applications, including unit testing, integration testing, performance testing, and best practices.

## Table of Contents

- [Overview](#overview)
- [Unit Testing](#unit-testing)
- [Integration Testing](#integration-testing)
- [Performance Testing](#performance-testing)
- [Test Utilities](#test-utilities)
- [Mocking and Stubbing](#mocking-and-stubbing)
- [Test Data Management](#test-data-management)
- [Continuous Integration](#continuous-integration)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

Testing TuskLang configurations ensures reliability, performance, and correctness of your Java applications. This guide covers various testing approaches and tools.

### Test Categories

```java
// Test categories for TuskLang configurations
public enum TestCategory {
    UNIT,           // Individual configuration components
    INTEGRATION,    // Configuration with external systems
    PERFORMANCE,    // Load and stress testing
    SECURITY,       // Security validation
    REGRESSION      // Ensuring changes don't break existing functionality
}
```

## Unit Testing

Unit testing focuses on testing individual TuskLang configuration components in isolation.

### Basic Unit Test Structure

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;

public class TuskLangUnitTest {
    
    private TuskLangParser parser;
    private TuskLangValidator validator;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLangParser();
        validator = new TuskLangValidator();
    }
    
    @Test
    void testBasicConfigurationParsing() {
        // Test basic configuration parsing
        String config = """
            [database]
            host = localhost
            port = 5432
            name = testdb
            """;
        
        TuskLangConfig result = parser.parse(config);
        
        assertNotNull(result);
        assertEquals("localhost", result.getString("database.host"));
        assertEquals(5432, result.getInt("database.port"));
        assertEquals("testdb", result.getString("database.name"));
    }
    
    @Test
    void testVariableResolution() {
        // Test variable resolution
        String config = """
            [app]
            name = MyApp
            version = 1.0.0
            
            [database]
            url = postgresql://${app.name}:5432/${database.name}
            name = testdb
            """;
        
        TuskLangConfig result = parser.parse(config);
        
        assertEquals("postgresql://MyApp:5432/testdb", 
                     result.getString("database.url"));
    }
    
    @Test
    void testValidationRules() {
        // Test validation rules
        String config = """
            [database]
            port = 99999  # Invalid port
            """;
        
        TuskLangConfig result = parser.parse(config);
        ValidationResult validation = validator.validate(result);
        
        assertFalse(validation.isValid());
        assertTrue(validation.getErrors().stream()
                   .anyMatch(e -> e.contains("port")));
    }
}
```

### Testing FUJSEN Functions

```java
@Test
void testFujsenFunctionExecution() {
    // Test FUJSEN function execution
    String config = """
        [app]
        timestamp = @date(now)
        random_id = @uuid()
        hash = @hash(sha256, "test")
        """;
    
    TuskLangConfig result = parser.parse(config);
    
    // Test timestamp
    String timestamp = result.getString("app.timestamp");
    assertNotNull(timestamp);
    assertTrue(timestamp.matches("\\d{4}-\\d{2}-\\d{2}.*"));
    
    // Test UUID
    String uuid = result.getString("app.random_id");
    assertNotNull(uuid);
    assertTrue(uuid.matches("[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}"));
    
    // Test hash
    String hash = result.getString("app.hash");
    assertNotNull(hash);
    assertEquals(64, hash.length()); // SHA256 hash length
}
```

### Testing @ Operators

```java
@Test
void testAtOperators() {
    // Test @ operators
    String config = """
        [app]
        env = @env(NODE_ENV, production)
        cache_key = @cache(users, 300)
        http_data = @http(GET, https://api.example.com/data)
        """;
    
    TuskLangConfig result = parser.parse(config);
    
    // Test environment variable
    String env = result.getString("app.env");
    assertNotNull(env);
    
    // Test cache key
    String cacheKey = result.getString("app.cache_key");
    assertNotNull(cacheKey);
    assertTrue(cacheKey.startsWith("users:"));
    
    // Test HTTP data (mock)
    String httpData = result.getString("app.http_data");
    assertNotNull(httpData);
}
```

## Integration Testing

Integration testing verifies that TuskLang configurations work correctly with external systems.

### Database Integration Testing

```java
@SpringBootTest
@TestPropertySource(properties = "spring.profiles.active=test")
public class DatabaseIntegrationTest {
    
    @Autowired
    private DataSource dataSource;
    
    @Autowired
    private TuskLangConfig config;
    
    @Test
    void testDatabaseConnection() throws SQLException {
        // Test database connection using TuskLang config
        String url = config.getString("database.url");
        String username = config.getString("database.username");
        String password = config.getString("database.password");
        
        try (Connection conn = DriverManager.getConnection(url, username, password)) {
            assertTrue(conn.isValid(5));
            
            // Test query execution
            try (Statement stmt = conn.createStatement()) {
                ResultSet rs = stmt.executeQuery("SELECT 1");
                assertTrue(rs.next());
                assertEquals(1, rs.getInt(1));
            }
        }
    }
    
    @Test
    void testDatabaseMigration() {
        // Test database migration using TuskLang config
        String migrationPath = config.getString("database.migrations.path");
        assertNotNull(migrationPath);
        
        // Verify migration files exist
        File migrationDir = new File(migrationPath);
        assertTrue(migrationDir.exists());
        assertTrue(migrationDir.isDirectory());
        
        File[] migrationFiles = migrationDir.listFiles((dir, name) -> name.endsWith(".sql"));
        assertNotNull(migrationFiles);
        assertTrue(migrationFiles.length > 0);
    }
}
```

### Spring Boot Integration Testing

```java
@SpringBootTest
@AutoConfigureTestDatabase(replace = AutoConfigureTestDatabase.Replace.NONE)
public class SpringBootIntegrationTest {
    
    @Autowired
    private ApplicationContext context;
    
    @Autowired
    private TuskLangConfig config;
    
    @Test
    void testSpringBootAutoConfiguration() {
        // Test that TuskLang beans are properly configured
        assertTrue(context.containsBean("tuskLangConfig"));
        assertTrue(context.containsBean("tuskLangParser"));
        assertTrue(context.containsBean("tuskLangValidator"));
    }
    
    @Test
    void testConfigurationProperties() {
        // Test @ConfigurationProperties integration
        DatabaseProperties dbProps = context.getBean(DatabaseProperties.class);
        
        assertEquals(config.getString("database.host"), dbProps.getHost());
        assertEquals(config.getInt("database.port"), dbProps.getPort());
        assertEquals(config.getString("database.name"), dbProps.getName());
    }
    
    @Test
    void testRestTemplateConfiguration() {
        // Test REST template configuration
        RestTemplate restTemplate = context.getBean(RestTemplate.class);
        assertNotNull(restTemplate);
        
        // Test timeout configuration
        HttpComponentsClientHttpRequestFactory factory = 
            (HttpComponentsClientHttpRequestFactory) restTemplate.getRequestFactory();
        
        assertEquals(config.getInt("http.timeout.connect"), factory.getConnectTimeout());
        assertEquals(config.getInt("http.timeout.read"), factory.getReadTimeout());
    }
}
```

### External Service Integration Testing

```java
@Testcontainers
public class ExternalServiceIntegrationTest {
    
    @Container
    static PostgreSQLContainer<?> postgres = new PostgreSQLContainer<>("postgres:13")
        .withDatabaseName("testdb")
        .withUsername("testuser")
        .withPassword("testpass");
    
    @Container
    static RedisContainer<?> redis = new RedisContainer<>("redis:6-alpine");
    
    @Test
    void testExternalServices() {
        // Test configuration with external services
        String config = """
            [database]
            url = %s
            username = %s
            password = %s
            
            [cache]
            url = %s
            """.formatted(
                postgres.getJdbcUrl(),
                postgres.getUsername(),
                postgres.getPassword(),
                redis.getHost() + ":" + redis.getFirstMappedPort()
            );
        
        TuskLangConfig tuskConfig = new TuskLangParser().parse(config);
        
        // Test database connection
        try (Connection conn = DriverManager.getConnection(
                tuskConfig.getString("database.url"),
                tuskConfig.getString("database.username"),
                tuskConfig.getString("database.password"))) {
            assertTrue(conn.isValid(5));
        } catch (SQLException e) {
            fail("Database connection failed: " + e.getMessage());
        }
        
        // Test Redis connection
        try (Jedis jedis = new Jedis(
                tuskConfig.getString("cache.url").split(":")[0],
                Integer.parseInt(tuskConfig.getString("cache.url").split(":")[1]))) {
            jedis.set("test", "value");
            assertEquals("value", jedis.get("test"));
        }
    }
}
```

## Performance Testing

Performance testing ensures TuskLang configurations meet performance requirements.

### Load Testing

```java
public class PerformanceTest {
    
    private TuskLangParser parser;
    private TuskLangConfig config;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLangParser();
        config = parser.parse(loadLargeConfig());
    }
    
    @Test
    void testParsingPerformance() {
        // Test parsing performance
        String largeConfig = loadLargeConfig();
        
        long startTime = System.currentTimeMillis();
        TuskLangConfig result = parser.parse(largeConfig);
        long endTime = System.currentTimeMillis();
        
        long parseTime = endTime - startTime;
        assertTrue(parseTime < 1000, "Parsing took too long: " + parseTime + "ms");
        assertNotNull(result);
    }
    
    @Test
    void testVariableResolutionPerformance() {
        // Test variable resolution performance
        long startTime = System.currentTimeMillis();
        
        for (int i = 0; i < 10000; i++) {
            config.getString("app.name");
            config.getInt("database.port");
            config.getBoolean("cache.enabled");
        }
        
        long endTime = System.currentTimeMillis();
        long resolutionTime = endTime - startTime;
        
        assertTrue(resolutionTime < 1000, "Variable resolution took too long: " + resolutionTime + "ms");
    }
    
    @Test
    void testFujsenFunctionPerformance() {
        // Test FUJSEN function performance
        long startTime = System.currentTimeMillis();
        
        for (int i = 0; i < 1000; i++) {
            config.getString("app.timestamp");
            config.getString("app.random_id");
        }
        
        long endTime = System.currentTimeMillis();
        long functionTime = endTime - startTime;
        
        assertTrue(functionTime < 5000, "FUJSEN functions took too long: " + functionTime + "ms");
    }
    
    private String loadLargeConfig() {
        // Load a large configuration file for testing
        StringBuilder config = new StringBuilder();
        config.append("[app]\n");
        config.append("name = TestApp\n");
        config.append("version = 1.0.0\n\n");
        
        // Add many sections
        for (int i = 0; i < 1000; i++) {
            config.append("[section").append(i).append("]\n");
            config.append("key1 = value1\n");
            config.append("key2 = value2\n");
            config.append("key3 = ").append(i).append("\n\n");
        }
        
        return config.toString();
    }
}
```

### Memory Usage Testing

```java
@Test
void testMemoryUsage() {
    // Test memory usage during parsing
    Runtime runtime = Runtime.getRuntime();
    
    // Force garbage collection
    System.gc();
    long initialMemory = runtime.totalMemory() - runtime.freeMemory();
    
    // Parse large configuration
    String largeConfig = loadLargeConfig();
    TuskLangConfig config = parser.parse(largeConfig);
    
    // Force garbage collection again
    System.gc();
    long finalMemory = runtime.totalMemory() - runtime.freeMemory();
    
    long memoryUsed = finalMemory - initialMemory;
    
    // Memory usage should be reasonable (less than 10MB for large config)
    assertTrue(memoryUsed < 10 * 1024 * 1024, 
               "Memory usage too high: " + (memoryUsed / 1024 / 1024) + "MB");
}
```

## Test Utilities

Create utility classes to simplify testing.

### Test Configuration Builder

```java
public class TestConfigBuilder {
    
    private final Map<String, Object> config = new HashMap<>();
    
    public TestConfigBuilder addDatabase(String host, int port, String name) {
        config.put("database.host", host);
        config.put("database.port", port);
        config.put("database.name", name);
        return this;
    }
    
    public TestConfigBuilder addCache(String host, int port) {
        config.put("cache.host", host);
        config.put("cache.port", port);
        return this;
    }
    
    public TestConfigBuilder addApp(String name, String version) {
        config.put("app.name", name);
        config.put("app.version", version);
        return this;
    }
    
    public String build() {
        StringBuilder sb = new StringBuilder();
        
        // Group by section
        Map<String, Map<String, Object>> sections = new HashMap<>();
        
        for (Map.Entry<String, Object> entry : config.entrySet()) {
            String[] parts = entry.getKey().split("\\.");
            String section = parts[0];
            String key = parts[1];
            
            sections.computeIfAbsent(section, k -> new HashMap<>())
                   .put(key, entry.getValue());
        }
        
        // Build TuskLang format
        for (Map.Entry<String, Map<String, Object>> section : sections.entrySet()) {
            sb.append("[").append(section.getKey()).append("]\n");
            
            for (Map.Entry<String, Object> entry : section.getValue().entrySet()) {
                sb.append(entry.getKey()).append(" = ").append(entry.getValue()).append("\n");
            }
            sb.append("\n");
        }
        
        return sb.toString();
    }
}
```

### Test Data Generator

```java
public class TestDataGenerator {
    
    public static String generateLargeConfig(int sections, int keysPerSection) {
        StringBuilder config = new StringBuilder();
        
        for (int i = 0; i < sections; i++) {
            config.append("[section").append(i).append("]\n");
            
            for (int j = 0; j < keysPerSection; j++) {
                config.append("key").append(j).append(" = value").append(j).append("\n");
            }
            config.append("\n");
        }
        
        return config.toString();
    }
    
    public static String generateComplexConfig() {
        return """
            [app]
            name = TestApp
            version = 1.0.0
            environment = @env(NODE_ENV, development)
            
            [database]
            host = localhost
            port = 5432
            name = testdb
            url = postgresql://${database.host}:${database.port}/${database.name}
            
            [cache]
            enabled = true
            host = localhost
            port = 6379
            ttl = 300
            
            [http]
            timeout = 5000
            retries = 3
            base_url = https://api.example.com
            
            [security]
            jwt_secret = @env(JWT_SECRET, default-secret)
            encryption_key = @hash(sha256, ${app.name})
            """;
    }
}
```

## Mocking and Stubbing

Use mocking to isolate components during testing.

### Mock External Dependencies

```java
@ExtendWith(MockitoExtension.class)
public class MockedTest {
    
    @Mock
    private DatabaseService databaseService;
    
    @Mock
    private CacheService cacheService;
    
    @InjectMocks
    private ApplicationService applicationService;
    
    @Test
    void testWithMockedServices() {
        // Setup mocks
        when(databaseService.isConnected()).thenReturn(true);
        when(cacheService.get("test-key")).thenReturn("test-value");
        
        // Test with mocked services
        TuskLangConfig config = new TuskLangParser().parse("""
            [app]
            name = TestApp
            """);
        
        boolean result = applicationService.processConfig(config);
        assertTrue(result);
        
        // Verify interactions
        verify(databaseService).isConnected();
        verify(cacheService).get("test-key");
    }
}
```

### Stub FUJSEN Functions

```java
@Test
void testWithStubbedFujsenFunctions() {
    // Create parser with stubbed FUJSEN functions
    TuskLangParser parser = new TuskLangParser();
    parser.setFujsenFunctionProvider(new StubbedFujsenProvider());
    
    String config = """
        [app]
        timestamp = @date(now)
        random_id = @uuid()
        """;
    
    TuskLangConfig result = parser.parse(config);
    
    assertEquals("2024-01-01T00:00:00Z", result.getString("app.timestamp"));
    assertEquals("test-uuid-123", result.getString("app.random_id"));
}

private static class StubbedFujsenProvider implements FujsenFunctionProvider {
    
    @Override
    public String executeFunction(String functionName, String... args) {
        return switch (functionName) {
            case "date" -> "2024-01-01T00:00:00Z";
            case "uuid" -> "test-uuid-123";
            case "hash" -> "test-hash-" + args[1];
            default -> "stubbed-value";
        };
    }
}
```

## Test Data Management

Manage test data effectively across different test scenarios.

### Test Configuration Profiles

```java
public class TestProfiles {
    
    public static final String UNIT_TEST = "unit-test";
    public static final String INTEGRATION_TEST = "integration-test";
    public static final String PERFORMANCE_TEST = "performance-test";
    
    public static String getConfigForProfile(String profile) {
        return switch (profile) {
            case UNIT_TEST -> """
                [app]
                name = UnitTestApp
                environment = test
                
                [database]
                host = localhost
                port = 5432
                name = unit_test_db
                """;
                
            case INTEGRATION_TEST -> """
                [app]
                name = IntegrationTestApp
                environment = test
                
                [database]
                host = test-db.example.com
                port = 5432
                name = integration_test_db
                
                [cache]
                host = test-cache.example.com
                port = 6379
                """;
                
            case PERFORMANCE_TEST -> """
                [app]
                name = PerformanceTestApp
                environment = test
                
                [database]
                host = perf-db.example.com
                port = 5432
                name = performance_test_db
                
                [cache]
                host = perf-cache.example.com
                port = 6379
                """;
                
            default -> throw new IllegalArgumentException("Unknown profile: " + profile);
        };
    }
}
```

### Test Data Cleanup

```java
@ExtendWith(TestDataCleanupExtension.class)
public class TestWithCleanup {
    
    @Test
    void testWithCleanup() {
        // Test that creates temporary data
        TuskLangConfig config = new TuskLangParser().parse("""
            [temp]
            file = /tmp/test-data.txt
            """);
        
        // Create temporary file
        File tempFile = new File(config.getString("temp.file"));
        try {
            tempFile.createNewFile();
            assertTrue(tempFile.exists());
        } catch (IOException e) {
            fail("Failed to create temp file: " + e.getMessage());
        }
        
        // Test logic here...
        
        // Cleanup is handled by TestDataCleanupExtension
    }
}

class TestDataCleanupExtension implements BeforeEachCallback, AfterEachCallback {
    
    private final List<File> tempFiles = new ArrayList<>();
    
    @Override
    public void beforeEach(ExtensionContext context) {
        // Setup before each test
    }
    
    @Override
    public void afterEach(ExtensionContext context) {
        // Cleanup after each test
        for (File file : tempFiles) {
            if (file.exists()) {
                file.delete();
            }
        }
        tempFiles.clear();
    }
    
    public void addTempFile(File file) {
        tempFiles.add(file);
    }
}
```

## Continuous Integration

Set up CI/CD pipelines for automated testing.

### GitHub Actions Workflow

```yaml
# .github/workflows/tusk-test.yml
name: TuskLang Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:13
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: testdb
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
      
      redis:
        image: redis:6-alpine
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 6379:6379
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up JDK 17
      uses: actions/setup-java@v3
      with:
        java-version: '17'
        distribution: 'temurin'
    
    - name: Cache Maven packages
      uses: actions/cache@v3
      with:
        path: ~/.m2
        key: ${{ runner.os }}-m2-${{ hashFiles('**/pom.xml') }}
        restore-keys: ${{ runner.os }}-m2
    
    - name: Run unit tests
      run: mvn test -Dtest=*UnitTest
    
    - name: Run integration tests
      run: mvn test -Dtest=*IntegrationTest
      env:
        DATABASE_URL: jdbc:postgresql://localhost:5432/testdb
        REDIS_URL: redis://localhost:6379
    
    - name: Run performance tests
      run: mvn test -Dtest=*PerformanceTest
    
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: target/surefire-reports/
```

### Jenkins Pipeline

```groovy
// Jenkinsfile
pipeline {
    agent any
    
    environment {
        JAVA_HOME = tool 'JDK-17'
        MAVEN_HOME = tool 'Maven-3.8'
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Unit Tests') {
            steps {
                sh 'mvn test -Dtest=*UnitTest'
            }
            post {
                always {
                    publishTestResults testResultsPattern: '**/surefire-reports/*.xml'
                }
            }
        }
        
        stage('Integration Tests') {
            steps {
                sh 'mvn test -Dtest=*IntegrationTest'
            }
            post {
                always {
                    publishTestResults testResultsPattern: '**/surefire-reports/*.xml'
                }
            }
        }
        
        stage('Performance Tests') {
            steps {
                sh 'mvn test -Dtest=*PerformanceTest'
            }
        }
        
        stage('Security Tests') {
            steps {
                sh 'mvn test -Dtest=*SecurityTest'
            }
        }
    }
    
    post {
        always {
            cleanWs()
        }
    }
}
```

## Best Practices

Follow these best practices for effective TuskLang testing.

### Test Organization

```java
// Organize tests by functionality
@Nested
class DatabaseTests {
    
    @Test
    void testDatabaseConnection() { /* ... */ }
    
    @Test
    void testDatabaseMigration() { /* ... */ }
}

@Nested
class CacheTests {
    
    @Test
    void testCacheConnection() { /* ... */ }
    
    @Test
    void testCacheOperations() { /* ... */ }
}

@Nested
class SecurityTests {
    
    @Test
    void testEncryption() { /* ... */ }
    
    @Test
    void testAuthentication() { /* ... */ }
}
```

### Test Naming Conventions

```java
// Use descriptive test names
@Test
void shouldParseValidConfigurationWithoutErrors() { /* ... */ }

@Test
void shouldResolveVariablesCorrectly() { /* ... */ }

@Test
void shouldThrowExceptionForInvalidPort() { /* ... */ }

@Test
void shouldConnectToDatabaseWithValidCredentials() { /* ... */ }
```

### Test Data Isolation

```java
@Test
void testWithIsolatedData() {
    // Use unique identifiers for test data
    String uniqueId = UUID.randomUUID().toString();
    
    String config = """
        [test]
        id = %s
        timestamp = %d
        """.formatted(uniqueId, System.currentTimeMillis());
    
    // Test with isolated data
    TuskLangConfig result = parser.parse(config);
    assertEquals(uniqueId, result.getString("test.id"));
}
```

### Error Testing

```java
@Test
void testInvalidConfiguration() {
    // Test invalid configuration
    String invalidConfig = """
        [database]
        port = invalid_port
        """;
    
    // Should throw exception
    assertThrows(TuskLangParseException.class, () -> {
        parser.parse(invalidConfig);
    });
}

@Test
void testMissingRequiredFields() {
    // Test missing required fields
    String config = """
        [database]
        host = localhost
        # Missing port and name
        """;
    
    TuskLangConfig result = parser.parse(config);
    ValidationResult validation = validator.validate(result);
    
    assertFalse(validation.isValid());
    assertTrue(validation.getErrors().stream()
               .anyMatch(e -> e.contains("port")));
    assertTrue(validation.getErrors().stream()
               .anyMatch(e -> e.contains("name")));
}
```

## Troubleshooting

Common testing issues and solutions.

### Test Environment Issues

```java
// Handle test environment setup issues
@BeforeAll
static void setUpTestEnvironment() {
    // Check if required services are available
    if (!isDatabaseAvailable()) {
        throw new AssumptionViolatedException("Database not available for testing");
    }
    
    if (!isRedisAvailable()) {
        throw new AssumptionViolatedException("Redis not available for testing");
    }
}

private static boolean isDatabaseAvailable() {
    try (Connection conn = DriverManager.getConnection(
            "jdbc:postgresql://localhost:5432/testdb",
            "testuser", "testpass")) {
        return conn.isValid(5);
    } catch (SQLException e) {
        return false;
    }
}

private static boolean isRedisAvailable() {
    try (Jedis jedis = new Jedis("localhost", 6379)) {
        jedis.ping();
        return true;
    } catch (Exception e) {
        return false;
    }
}
```

### Performance Test Issues

```java
// Handle performance test issues
@Test
@Timeout(value = 30, unit = TimeUnit.SECONDS)
void testPerformanceWithTimeout() {
    // Test with timeout to prevent hanging
    String largeConfig = loadLargeConfig();
    
    long startTime = System.currentTimeMillis();
    TuskLangConfig result = parser.parse(largeConfig);
    long endTime = System.currentTimeMillis();
    
    long parseTime = endTime - startTime;
    assertTrue(parseTime < 5000, "Parsing took too long: " + parseTime + "ms");
}
```

### Memory Leak Detection

```java
@Test
void testMemoryLeaks() {
    // Test for memory leaks
    Runtime runtime = Runtime.getRuntime();
    
    // Force garbage collection
    System.gc();
    long initialMemory = runtime.totalMemory() - runtime.freeMemory();
    
    // Perform operations that might cause memory leaks
    for (int i = 0; i < 1000; i++) {
        String config = "key" + i + " = value" + i;
        TuskLangConfig result = parser.parse(config);
        // Use result to prevent optimization
        assertNotNull(result);
    }
    
    // Force garbage collection again
    System.gc();
    long finalMemory = runtime.totalMemory() - runtime.freeMemory();
    
    long memoryIncrease = finalMemory - initialMemory;
    
    // Memory increase should be minimal
    assertTrue(memoryIncrease < 1024 * 1024, 
               "Possible memory leak: " + (memoryIncrease / 1024) + "KB increase");
}
```

### Test Flakiness

```java
// Handle flaky tests
@Test
@RepeatedTest(10)
void testConsistentBehavior() {
    // Run test multiple times to ensure consistency
    String config = """
        [app]
        random_value = @uuid()
        """;
    
    TuskLangConfig result = parser.parse(config);
    String randomValue = result.getString("app.random_value");
    
    // Verify UUID format
    assertTrue(randomValue.matches("[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}"));
}

@Test
void testDeterministicBehavior() {
    // Test deterministic behavior
    String config = """
        [app]
        hash = @hash(sha256, "test")
        """;
    
    TuskLangConfig result1 = parser.parse(config);
    TuskLangConfig result2 = parser.parse(config);
    
    // Same input should produce same output
    assertEquals(result1.getString("app.hash"), result2.getString("app.hash"));
}
```

This comprehensive testing guide provides everything needed to thoroughly test TuskLang configurations in Java applications, ensuring reliability, performance, and correctness across all testing scenarios. 
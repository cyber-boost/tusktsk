# @env - Environment Variables in Java

The `@env` operator provides secure access to environment variables in Java applications, integrating with Spring Boot's configuration system, JVM system properties, and enterprise environment management.

## Basic Syntax

```java
// TuskLang configuration
api_key: @env("API_KEY")
database_url: @env("DATABASE_URL", "jdbc:postgresql://localhost:5432/mydb")
debug_mode: @env("DEBUG", false)
port: @env("PORT", 8080)
```

```java
// Java Spring Boot integration
@Configuration
public class EnvironmentConfig {
    
    @Value("${API_KEY}")
    private String apiKey;
    
    @Value("${DATABASE_URL:jdbc:postgresql://localhost:5432/mydb}")
    private String databaseUrl;
    
    @Value("${DEBUG:false}")
    private boolean debugMode;
    
    @Value("${PORT:8080}")
    private int port;
    
    @Bean
    public EnvironmentVariableService environmentVariableService() {
        return EnvironmentVariableService.builder()
            .apiKey(apiKey)
            .databaseUrl(databaseUrl)
            .debugMode(debugMode)
            .port(port)
            .build();
    }
}
```

## Environment Variable Access

```java
// Java environment variable service
@Component
public class EnvironmentVariableService {
    
    @Autowired
    private Environment environment;
    
    public String getString(String key) {
        return environment.getProperty(key);
    }
    
    public String getString(String key, String defaultValue) {
        return environment.getProperty(key, defaultValue);
    }
    
    public Integer getInteger(String key) {
        return environment.getProperty(key, Integer.class);
    }
    
    public Integer getInteger(String key, Integer defaultValue) {
        return environment.getProperty(key, Integer.class, defaultValue);
    }
    
    public Boolean getBoolean(String key) {
        return environment.getProperty(key, Boolean.class);
    }
    
    public Boolean getBoolean(String key, Boolean defaultValue) {
        return environment.getProperty(key, Boolean.class, defaultValue);
    }
    
    public Double getDouble(String key) {
        return environment.getProperty(key, Double.class);
    }
    
    public Double getDouble(String key, Double defaultValue) {
        return environment.getProperty(key, Double.class, defaultValue);
    }
}
```

```java
// TuskLang environment variables
app_config: {
    # Basic environment variables
    api_key: @env("API_KEY")
    database_url: @env("DATABASE_URL", "jdbc:postgresql://localhost:5432/mydb")
    debug_mode: @env("DEBUG", false)
    port: @env("PORT", 8080)
    
    # Type-specific environment variables
    max_connections: @env.int("MAX_CONNECTIONS", 100)
    timeout_seconds: @env.float("TIMEOUT_SECONDS", 30.0)
    enable_ssl: @env.bool("ENABLE_SSL", true)
    
    # Required environment variables
    required_api_key: @env.required("API_KEY")
    required_secret: @env.required("SECRET_KEY")
}
```

## Secure Environment Variables

```java
// Java secure environment variable handling
@Component
public class SecureEnvironmentService {
    
    @Autowired
    private EnvironmentVariableService envService;
    
    public String getSecureString(String key) {
        String value = envService.getString(key);
        if (value == null) {
            throw new SecurityException("Required secure environment variable not found: " + key);
        }
        return value;
    }
    
    public String getEncryptedString(String key) {
        String encryptedValue = envService.getString(key);
        if (encryptedValue == null) {
            return null;
        }
        return decryptValue(encryptedValue);
    }
    
    public void validateRequiredVariables(String... keys) {
        for (String key : keys) {
            if (envService.getString(key) == null) {
                throw new SecurityException("Required environment variable not found: " + key);
            }
        }
    }
    
    private String decryptValue(String encryptedValue) {
        // Implementation for decryption
        return encryptedValue; // Placeholder
    }
}
```

```java
// TuskLang secure environment variables
secure_config: {
    # Secure environment variables
    api_key: @env.secure("API_KEY")
    secret_key: @env.secure("SECRET_KEY")
    jwt_secret: @env.secure("JWT_SECRET")
    
    # Encrypted environment variables
    encrypted_password: @env.encrypted("ENCRYPTED_PASSWORD")
    encrypted_token: @env.encrypted("ENCRYPTED_TOKEN")
    
    # Required secure variables
    required_secrets: @env.required.secure(["API_KEY", "SECRET_KEY", "JWT_SECRET"])
}
```

## Environment-Specific Configuration

```java
// Java environment-specific configuration
@Configuration
@Profile("development")
public class DevelopmentConfig {
    
    @Bean
    public EnvironmentVariableService devEnvironmentService() {
        return EnvironmentVariableService.builder()
            .environment("development")
            .debugMode(true)
            .logLevel("DEBUG")
            .databaseUrl("jdbc:h2:mem:testdb")
            .build();
    }
}

@Configuration
@Profile("production")
public class ProductionConfig {
    
    @Bean
    public EnvironmentVariableService prodEnvironmentService() {
        return EnvironmentVariableService.builder()
            .environment("production")
            .debugMode(false)
            .logLevel("WARN")
            .databaseUrl(System.getenv("DATABASE_URL"))
            .build();
    }
}
```

```java
// TuskLang environment-specific configuration
# Development environment
dev_config: {
    environment: @env("NODE_ENV", "development")
    debug_enabled: @env("DEBUG", true)
    log_level: @env("LOG_LEVEL", "debug")
    database_url: @env("DATABASE_URL", "jdbc:h2:mem:testdb")
    cors_origin: @env("CORS_ORIGIN", "*")
}

# Production environment
prod_config: {
    environment: @env("NODE_ENV", "production")
    debug_enabled: @env("DEBUG", false)
    log_level: @env("LOG_LEVEL", "warn")
    database_url: @env.required("DATABASE_URL")
    cors_origin: @env("CORS_ORIGIN", "https://app.example.com")
}

# Testing environment
test_config: {
    environment: @env("NODE_ENV", "testing")
    debug_enabled: @env("DEBUG", true)
    log_level: @env("LOG_LEVEL", "debug")
    database_url: @env("DATABASE_URL", "jdbc:h2:mem:testdb")
    test_mode: @env("TEST_MODE", true)
}
```

## Environment Variable Validation

```java
// Java environment variable validation
@Component
public class EnvironmentValidationService {
    
    @Autowired
    private EnvironmentVariableService envService;
    
    public void validateEnvironment() {
        validateRequiredVariables();
        validateVariableTypes();
        validateVariableFormats();
    }
    
    private void validateRequiredVariables() {
        String[] required = {"API_KEY", "DATABASE_URL", "JWT_SECRET"};
        for (String key : required) {
            if (envService.getString(key) == null) {
                throw new IllegalStateException("Required environment variable not found: " + key);
            }
        }
    }
    
    private void validateVariableTypes() {
        // Validate numeric variables
        Integer port = envService.getInteger("PORT");
        if (port != null && (port < 1 || port > 65535)) {
            throw new IllegalStateException("PORT must be between 1 and 65535");
        }
        
        // Validate boolean variables
        Boolean debug = envService.getBoolean("DEBUG");
        if (debug == null) {
            throw new IllegalStateException("DEBUG must be a boolean value");
        }
    }
    
    private void validateVariableFormats() {
        // Validate URL format
        String databaseUrl = envService.getString("DATABASE_URL");
        if (databaseUrl != null && !isValidUrl(databaseUrl)) {
            throw new IllegalStateException("DATABASE_URL must be a valid URL");
        }
        
        // Validate email format
        String adminEmail = envService.getString("ADMIN_EMAIL");
        if (adminEmail != null && !isValidEmail(adminEmail)) {
            throw new IllegalStateException("ADMIN_EMAIL must be a valid email address");
        }
    }
    
    private boolean isValidUrl(String url) {
        try {
            new URL(url);
            return true;
        } catch (MalformedURLException e) {
            return false;
        }
    }
    
    private boolean isValidEmail(String email) {
        return email.matches("^[A-Za-z0-9+_.-]+@(.+)$");
    }
}
```

```java
// TuskLang environment variable validation
environment_validation: {
    # Validate required variables
    @env.validate.required(["API_KEY", "DATABASE_URL", "JWT_SECRET"])
    
    # Validate variable types
    @env.validate.type("PORT", "integer")
    @env.validate.type("DEBUG", "boolean")
    @env.validate.type("TIMEOUT", "float")
    
    # Validate variable ranges
    @env.validate.range("PORT", 1, 65535)
    @env.validate.range("MAX_CONNECTIONS", 1, 1000)
    @env.validate.range("TIMEOUT_SECONDS", 1.0, 300.0)
    
    # Validate variable formats
    @env.validate.format("DATABASE_URL", "url")
    @env.validate.format("ADMIN_EMAIL", "email")
    @env.validate.format("API_VERSION", "semver")
    
    # Validate variable patterns
    @env.validate.pattern("SECRET_KEY", "^[A-Za-z0-9]{32,}$")
    @env.validate.pattern("ENVIRONMENT", "^(development|staging|production)$")
}
```

## Environment Variable Encryption

```java
// Java environment variable encryption
@Component
public class EnvironmentEncryptionService {
    
    @Autowired
    private EnvironmentVariableService envService;
    
    private final String encryptionKey = System.getenv("ENCRYPTION_KEY");
    
    public String encryptValue(String value) {
        if (value == null) {
            return null;
        }
        
        try {
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            SecretKeySpec keySpec = new SecretKeySpec(
                encryptionKey.getBytes(StandardCharsets.UTF_8), "AES");
            cipher.init(Cipher.ENCRYPT_MODE, keySpec);
            
            byte[] encrypted = cipher.doFinal(value.getBytes(StandardCharsets.UTF_8));
            return Base64.getEncoder().encodeToString(encrypted);
        } catch (Exception e) {
            throw new RuntimeException("Failed to encrypt value", e);
        }
    }
    
    public String decryptValue(String encryptedValue) {
        if (encryptedValue == null) {
            return null;
        }
        
        try {
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            SecretKeySpec keySpec = new SecretKeySpec(
                encryptionKey.getBytes(StandardCharsets.UTF_8), "AES");
            cipher.init(Cipher.DECRYPT_MODE, keySpec);
            
            byte[] decrypted = cipher.doFinal(
                Base64.getDecoder().decode(encryptedValue));
            return new String(decrypted, StandardCharsets.UTF_8);
        } catch (Exception e) {
            throw new RuntimeException("Failed to decrypt value", e);
        }
    }
}
```

```java
// TuskLang environment variable encryption
encrypted_config: {
    # Encrypt sensitive values
    encrypted_password: @env.encrypt("mysecretpassword", "AES-256-GCM")
    encrypted_token: @env.encrypt("sensitive-token", "AES-256-GCM")
    
    # Decrypt encrypted environment variables
    decrypted_password: @env.decrypt(@env("ENCRYPTED_PASSWORD"), "AES-256-GCM")
    decrypted_token: @env.decrypt(@env("ENCRYPTED_TOKEN"), "AES-256-GCM")
    
    # Auto-encrypt/decrypt
    auto_encrypted: @env.auto.encrypt("sensitive_data")
    auto_decrypted: @env.auto.decrypt(@env("AUTO_ENCRYPTED_DATA"))
}
```

## Environment Variable Templates

```java
// Java environment variable templating
@Component
public class EnvironmentTemplateService {
    
    @Autowired
    private EnvironmentVariableService envService;
    
    public String processTemplate(String template) {
        return template.replaceAll("\\$\\{([^}]+)\\}", matchResult -> {
            String key = matchResult.group(1);
            return envService.getString(key, "");
        });
    }
    
    public String getTemplatedUrl(String baseUrl) {
        return baseUrl
            .replace("${HOST}", envService.getString("HOST", "localhost"))
            .replace("${PORT}", String.valueOf(envService.getInteger("PORT", 8080)))
            .replace("${PROTOCOL}", envService.getString("PROTOCOL", "http"));
    }
}
```

```java
// TuskLang environment variable templates
template_config: {
    # Template with environment variables
    api_url: @env.template("${PROTOCOL}://${HOST}:${PORT}/api")
    webhook_url: @env.template("${WEBHOOK_BASE_URL}/webhooks/${WEBHOOK_PATH}")
    
    # Conditional templates
    database_url: @env.template.conditional({
        development: "jdbc:h2:mem:testdb"
        production: "${DATABASE_URL}"
        testing: "jdbc:h2:mem:testdb"
    })
    
    # Nested templates
    full_url: @env.template("${PROTOCOL}://${HOST}:${PORT}${BASE_PATH}/api/v${API_VERSION}")
}
```

## Environment Variable Monitoring

```java
// Java environment variable monitoring
@Component
public class EnvironmentMonitoringService {
    
    @Autowired
    private EnvironmentVariableService envService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public void monitorEnvironmentVariables() {
        // Monitor environment variable access
        Counter.builder("env.variable.access")
            .tag("variable", "API_KEY")
            .register(meterRegistry)
            .increment();
        
        // Monitor missing environment variables
        if (envService.getString("MISSING_VAR") == null) {
            Counter.builder("env.variable.missing")
                .tag("variable", "MISSING_VAR")
                .register(meterRegistry)
                .increment();
        }
    }
    
    public Map<String, Object> getEnvironmentSummary() {
        return Map.of(
            "total_variables", envService.getAllVariables().size(),
            "missing_required", envService.getMissingRequiredVariables(),
            "encrypted_variables", envService.getEncryptedVariables().size(),
            "last_updated", Instant.now()
        );
    }
}
```

```java
// TuskLang environment variable monitoring
environment_monitoring: {
    # Monitor environment variable access
    @env.monitor.access("API_KEY")
    @env.monitor.access("DATABASE_URL")
    @env.monitor.access("JWT_SECRET")
    
    # Monitor missing variables
    @env.monitor.missing(["API_KEY", "DATABASE_URL", "JWT_SECRET"])
    
    # Environment summary
    env_summary: {
        total_variables: @env.count()
        missing_required: @env.missing.count()
        encrypted_variables: @env.encrypted.count()
        last_updated: @date.now()
    }
    
    # Environment health check
    env_health: @env.health.check()
}
```

## Environment Variable Testing

```java
// JUnit test for environment variables
@SpringBootTest
class EnvironmentVariableServiceTest {
    
    @Autowired
    private EnvironmentVariableService envService;
    
    @Test
    void testStringVariable() {
        // Set test environment variable
        System.setProperty("TEST_VAR", "test_value");
        
        String value = envService.getString("TEST_VAR");
        assertThat(value).isEqualTo("test_value");
    }
    
    @Test
    void testStringVariableWithDefault() {
        String value = envService.getString("NON_EXISTENT_VAR", "default_value");
        assertThat(value).isEqualTo("default_value");
    }
    
    @Test
    void testIntegerVariable() {
        System.setProperty("TEST_PORT", "8080");
        
        Integer port = envService.getInteger("TEST_PORT");
        assertThat(port).isEqualTo(8080);
    }
    
    @Test
    void testBooleanVariable() {
        System.setProperty("TEST_DEBUG", "true");
        
        Boolean debug = envService.getBoolean("TEST_DEBUG");
        assertThat(debug).isTrue();
    }
    
    @Test
    void testRequiredVariable() {
        // This should throw an exception
        assertThatThrownBy(() -> {
            envService.getRequiredString("MISSING_REQUIRED_VAR");
        }).isInstanceOf(IllegalStateException.class);
    }
}
```

```java
// TuskLang environment variable testing
test_environment_variables: {
    # Test basic environment variables
    test_var: @env("TEST_VAR", "default_value")
    assert(@test_var == "default_value", "Should use default value")
    
    # Test type conversion
    test_port: @env.int("TEST_PORT", 8080)
    assert(@test_port == 8080, "Should convert to integer")
    
    test_debug: @env.bool("TEST_DEBUG", true)
    assert(@test_debug == true, "Should convert to boolean")
    
    # Test required variables
    @env.validate.required(["API_KEY", "DATABASE_URL"])
    
    # Test validation
    @env.validate.range("PORT", 1, 65535)
    @env.validate.format("EMAIL", "email")
}
```

## Best Practices

### 1. Environment-Specific Configuration
```java
// Use profiles for environment-specific configuration
@Configuration
@Profile("dev")
public class DevEnvironmentConfig {
    
    @Bean
    public EnvironmentVariableService devEnvService() {
        return EnvironmentVariableService.builder()
            .environment("development")
            .debugMode(true)
            .logLevel("DEBUG")
            .build();
    }
}

@Configuration
@Profile("prod")
public class ProdEnvironmentConfig {
    
    @Bean
    public EnvironmentVariableService prodEnvService() {
        return EnvironmentVariableService.builder()
            .environment("production")
            .debugMode(false)
            .logLevel("WARN")
            .build();
    }
}
```

### 2. Secure Variable Handling
```java
// Use encryption for sensitive environment variables
@Component
public class SecureEnvironmentHandler {
    
    @Autowired
    private EnvironmentEncryptionService encryptionService;
    
    public String getSecureVariable(String key) {
        String value = System.getenv(key);
        if (value == null) {
            throw new SecurityException("Required secure variable not found: " + key);
        }
        
        // Check if value is encrypted
        if (value.startsWith("ENC:")) {
            return encryptionService.decryptValue(value.substring(4));
        }
        
        return value;
    }
    
    public void setSecureVariable(String key, String value) {
        String encryptedValue = "ENC:" + encryptionService.encryptValue(value);
        // Store encrypted value securely
    }
}
```

### 3. Validation and Monitoring
```java
// Validate environment variables on startup
@Component
public class EnvironmentValidator {
    
    @PostConstruct
    public void validateEnvironment() {
        validateRequiredVariables();
        validateVariableTypes();
        validateVariableFormats();
        logEnvironmentSummary();
    }
    
    private void validateRequiredVariables() {
        String[] required = {"API_KEY", "DATABASE_URL", "JWT_SECRET"};
        List<String> missing = Arrays.stream(required)
            .filter(key -> System.getenv(key) == null)
            .collect(Collectors.toList());
        
        if (!missing.isEmpty()) {
            throw new IllegalStateException("Missing required environment variables: " + missing);
        }
    }
    
    private void logEnvironmentSummary() {
        log.info("Environment validation completed successfully");
        log.info("Total environment variables: {}", System.getenv().size());
        log.info("Environment: {}", System.getenv("NODE_ENV"));
    }
}
```

The `@env` operator in Java provides secure and flexible access to environment variables, enabling applications to adapt their configuration based on the deployment environment while maintaining security and validation standards. 
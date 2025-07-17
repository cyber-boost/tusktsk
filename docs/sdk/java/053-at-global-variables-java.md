# @global - Global Variables in Java

The `@global` operator provides access to global variables and application-wide state in Java applications, integrating with Spring Boot's ApplicationContext, static variables, and enterprise configuration management.

## Basic Syntax

```java
// TuskLang configuration
app_name: @global.get("app_name")
version: @global.get("version", "1.0.0")
debug_mode: @global.get("debug_mode", false)
```

```java
// Java Spring Boot integration
@Component
public class GlobalVariableService {
    
    @Autowired
    private ApplicationContext applicationContext;
    
    public void setGlobal(String key, Object value) {
        applicationContext.getBean(GlobalVariableRegistry.class).set(key, value);
    }
    
    public Object getGlobal(String key) {
        return applicationContext.getBean(GlobalVariableRegistry.class).get(key);
    }
    
    public <T> T getGlobal(String key, Class<T> type) {
        return applicationContext.getBean(GlobalVariableRegistry.class).get(key, type);
    }
}
```

## Global Variable Registry

```java
// Global variable registry component
@Component
public class GlobalVariableRegistry {
    
    private final Map<String, Object> globals = new ConcurrentHashMap<>();
    private final Map<String, Class<?>> types = new ConcurrentHashMap<>();
    
    public void set(String key, Object value) {
        globals.put(key, value);
        types.put(key, value != null ? value.getClass() : null);
    }
    
    public Object get(String key) {
        return globals.get(key);
    }
    
    @SuppressWarnings("unchecked")
    public <T> T get(String key, Class<T> type) {
        Object value = globals.get(key);
        if (value != null && type.isAssignableFrom(value.getClass())) {
            return (T) value;
        }
        return null;
    }
    
    public boolean has(String key) {
        return globals.containsKey(key);
    }
    
    public void remove(String key) {
        globals.remove(key);
        types.remove(key);
    }
    
    public Set<String> keys() {
        return new HashSet<>(globals.keySet());
    }
    
    public Map<String, Object> getAll() {
        return new HashMap<>(globals);
    }
}
```

## Application Configuration

```java
// application.yml
tusk:
  globals:
    app-name: "TuskLang Java App"
    version: "1.0.0"
    debug-mode: false
    environment: "${SPRING_PROFILES_ACTIVE:dev}"
    max-connections: 100
    cache-ttl: 3600
```

```java
// TuskLang global configuration
app_config: {
    # Application globals
    app_name: @global.get("app_name")
    version: @global.get("version")
    debug_mode: @global.get("debug_mode")
    environment: @global.get("environment")
    
    # Database globals
    db_host: @global.get("db_host")
    db_port: @global.get("db_port")
    db_name: @global.get("db_name")
    
    # Cache globals
    cache_ttl: @global.get("cache_ttl", 3600)
    cache_max_size: @global.get("cache_max_size", 1000)
    
    # Security globals
    jwt_secret: @global.get("jwt_secret")
    session_timeout: @global.get("session_timeout", 1800)
}
```

## Environment-Specific Globals

```java
// Development environment
dev_globals: {
    debug_mode: true
    log_level: "DEBUG"
    cache_enabled: false
    database_url: "jdbc:h2:mem:testdb"
    api_rate_limit: 1000
}

// Production environment
prod_globals: {
    debug_mode: false
    log_level: "WARN"
    cache_enabled: true
    database_url: @env.DATABASE_URL
    api_rate_limit: 100
}

// Testing environment
test_globals: {
    debug_mode: true
    log_level: "TRACE"
    cache_enabled: false
    database_url: "jdbc:h2:mem:testdb"
    api_rate_limit: 10000
}
```

## Global Variable Initialization

```java
// Java initialization service
@Component
public class GlobalVariableInitializer {
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    @PostConstruct
    public void initializeGlobals() {
        // Application metadata
        registry.set("app_name", "TuskLang Java Application");
        registry.set("version", "1.0.0");
        registry.set("startup_time", Instant.now());
        
        // Environment detection
        String environment = System.getProperty("spring.profiles.active", "dev");
        registry.set("environment", environment);
        
        // JVM information
        registry.set("java_version", System.getProperty("java.version"));
        registry.set("jvm_memory_max", Runtime.getRuntime().maxMemory());
        registry.set("jvm_memory_total", Runtime.getRuntime().totalMemory());
        
        // System information
        registry.set("os_name", System.getProperty("os.name"));
        registry.set("os_version", System.getProperty("os.version"));
        registry.set("user_home", System.getProperty("user.home"));
    }
}
```

```java
// TuskLang global initialization
initialize_globals: {
    # Application metadata
    @global.set("app_name", "TuskLang Java Application")
    @global.set("version", "1.0.0")
    @global.set("startup_time", @date.now())
    
    # Environment detection
    environment: @env.SPRING_PROFILES_ACTIVE|"dev"
    @global.set("environment", @environment)
    
    # JVM information
    @global.set("java_version", @env.JAVA_VERSION)
    @global.set("jvm_memory_max", @env.JVM_MEMORY_MAX)
    @global.set("jvm_memory_total", @env.JVM_MEMORY_TOTAL)
    
    # System information
    @global.set("os_name", @env.OS_NAME)
    @global.set("os_version", @env.OS_VERSION)
    @global.set("user_home", @env.USER_HOME)
}
```

## Global Variable Types

```java
// Java type-safe global variables
@Component
public class TypedGlobalService {
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    // String globals
    public void setString(String key, String value) {
        registry.set(key, value);
    }
    
    public String getString(String key, String defaultValue) {
        return registry.get(key, String.class) != null ? 
            registry.get(key, String.class) : defaultValue;
    }
    
    // Integer globals
    public void setInteger(String key, Integer value) {
        registry.set(key, value);
    }
    
    public Integer getInteger(String key, Integer defaultValue) {
        return registry.get(key, Integer.class) != null ? 
            registry.get(key, Integer.class) : defaultValue;
    }
    
    // Boolean globals
    public void setBoolean(String key, Boolean value) {
        registry.set(key, value);
    }
    
    public Boolean getBoolean(String key, Boolean defaultValue) {
        return registry.get(key, Boolean.class) != null ? 
            registry.get(key, Boolean.class) : defaultValue;
    }
    
    // Object globals
    public void setObject(String key, Object value) {
        registry.set(key, value);
    }
    
    @SuppressWarnings("unchecked")
    public <T> T getObject(String key, Class<T> type) {
        return registry.get(key, type);
    }
}
```

```java
// TuskLang typed globals
typed_globals: {
    # String globals
    app_name: @global.get("app_name", "Unknown App")
    version: @global.get("version", "1.0.0")
    
    # Integer globals
    max_connections: @global.get("max_connections", 100)
    cache_ttl: @global.get("cache_ttl", 3600)
    session_timeout: @global.get("session_timeout", 1800)
    
    # Boolean globals
    debug_mode: @global.get("debug_mode", false)
    cache_enabled: @global.get("cache_enabled", true)
    ssl_enabled: @global.get("ssl_enabled", false)
    
    # Object globals
    database_config: @global.get("database_config")
    cache_config: @global.get("cache_config")
    security_config: @global.get("security_config")
}
```

## Global Variable Scopes

```java
// Java scoped global variables
@Component
public class ScopedGlobalService {
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    // Application scope (shared across all requests)
    public void setApplicationGlobal(String key, Object value) {
        registry.set("app:" + key, value);
    }
    
    public Object getApplicationGlobal(String key) {
        return registry.get("app:" + key);
    }
    
    // Session scope (shared across user session)
    public void setSessionGlobal(String sessionId, String key, Object value) {
        registry.set("session:" + sessionId + ":" + key, value);
    }
    
    public Object getSessionGlobal(String sessionId, String key) {
        return registry.get("session:" + sessionId + ":" + key);
    }
    
    // Request scope (shared across single request)
    public void setRequestGlobal(String requestId, String key, Object value) {
        registry.set("request:" + requestId + ":" + key, value);
    }
    
    public Object getRequestGlobal(String requestId, String key) {
        return registry.get("request:" + requestId + ":" + key);
    }
}
```

```java
// TuskLang scoped globals
scoped_globals: {
    # Application scope
    app_name: @global.get("app:name")
    app_version: @global.get("app:version")
    
    # Session scope
    user_id: @global.get("session:" + @session.id + ":user_id")
    user_role: @global.get("session:" + @session.id + ":user_role")
    
    # Request scope
    request_id: @global.get("request:" + @request.id + ":request_id")
    request_start: @global.get("request:" + @request.id + ":start_time")
}
```

## Global Variable Persistence

```java
// Java persistent global variables
@Component
public class PersistentGlobalService {
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    @Autowired
    private RedisTemplate<String, Object> redisTemplate;
    
    // Persistent globals (stored in Redis)
    public void setPersistentGlobal(String key, Object value) {
        registry.set(key, value);
        redisTemplate.opsForValue().set("global:" + key, value);
    }
    
    public Object getPersistentGlobal(String key) {
        Object value = registry.get(key);
        if (value == null) {
            value = redisTemplate.opsForValue().get("global:" + key);
            if (value != null) {
                registry.set(key, value);
            }
        }
        return value;
    }
    
    // Load persistent globals on startup
    @PostConstruct
    public void loadPersistentGlobals() {
        Set<String> keys = redisTemplate.keys("global:*");
        for (String key : keys) {
            String globalKey = key.substring(7); // Remove "global:" prefix
            Object value = redisTemplate.opsForValue().get(key);
            registry.set(globalKey, value);
        }
    }
}
```

```java
// TuskLang persistent globals
persistent_globals: {
    # Load persistent globals
    @global.load_persistent()
    
    # Set persistent global
    @global.set_persistent("last_maintenance", @date.now())
    @global.set_persistent("total_users", @query("SELECT COUNT(*) FROM users"))
    @global.set_persistent("system_status", "healthy")
    
    # Get persistent global
    last_maintenance: @global.get_persistent("last_maintenance")
    total_users: @global.get_persistent("total_users")
    system_status: @global.get_persistent("system_status")
}
```

## Global Variable Validation

```java
// Java global variable validation
@Component
public class GlobalVariableValidator {
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    public void setWithValidation(String key, Object value, Predicate<Object> validator) {
        if (validator.test(value)) {
            registry.set(key, value);
        } else {
            throw new IllegalArgumentException("Invalid value for global variable: " + key);
        }
    }
    
    public void setIntegerWithRange(String key, Integer value, int min, int max) {
        setWithValidation(key, value, v -> {
            Integer intValue = (Integer) v;
            return intValue >= min && intValue <= max;
        });
    }
    
    public void setStringWithPattern(String key, String value, String pattern) {
        setWithValidation(key, value, v -> {
            String stringValue = (String) v;
            return stringValue.matches(pattern);
        });
    }
}
```

```java
// TuskLang global validation
validated_globals: {
    # Validate and set integer with range
    @global.set_with_validation("max_connections", 150, @validate.range(1, 1000))
    @global.set_with_validation("cache_ttl", 3600, @validate.range(60, 86400))
    
    # Validate and set string with pattern
    @global.set_with_validation("email_domain", "example.com", @validate.pattern("^[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"))
    @global.set_with_validation("api_version", "v1", @validate.pattern("^v[0-9]+$"))
    
    # Get validated globals
    max_connections: @global.get("max_connections")
    cache_ttl: @global.get("cache_ttl")
    email_domain: @global.get("email_domain")
    api_version: @global.get("api_version")
}
```

## Global Variable Monitoring

```java
// Java global variable monitoring
@Component
public class GlobalVariableMonitor {
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    @EventListener
    public void handleGlobalVariableChange(GlobalVariableChangeEvent event) {
        // Track global variable changes
        Counter.builder("tusk.globals.changes")
            .tag("key", event.getKey())
            .tag("operation", event.getOperation())
            .register(meterRegistry)
            .increment();
        
        // Log significant changes
        if (isSignificantChange(event.getKey(), event.getOldValue(), event.getNewValue())) {
            logSignificantChange(event);
        }
    }
    
    private boolean isSignificantChange(String key, Object oldValue, Object newValue) {
        // Define significant changes based on key patterns
        return key.contains("config") || key.contains("status") || key.contains("mode");
    }
    
    private void logSignificantChange(GlobalVariableChangeEvent event) {
        // Log to monitoring system
        System.out.println("Significant global variable change: " + 
            event.getKey() + " from " + event.getOldValue() + " to " + event.getNewValue());
    }
}
```

```java
// TuskLang global monitoring
global_monitoring: {
    # Monitor global changes
    @global.on_change("app_config", (key, old_value, new_value) => {
        @debug.log("config_change", {
            key: @key
            old_value: @old_value
            new_value: @new_value
            timestamp: @date.now()
        })
    })
    
    # Monitor significant globals
    @global.on_change("system_status", (key, old_value, new_value) => {
        @if(@new_value != @old_value) {
            @alert.send("System status changed from " + @old_value + " to " + @new_value)
        }
    })
    
    # Track global usage
    @global.track_usage("max_connections")
    @global.track_usage("cache_ttl")
    @global.track_usage("debug_mode")
}
```

## Global Variable Testing

```java
// JUnit test for global variables
@SpringBootTest
class GlobalVariableServiceTest {
    
    @Autowired
    private GlobalVariableService globalService;
    
    @Test
    void testGlobalVariableSetAndGet() {
        // Set global variable
        globalService.setGlobal("test_key", "test_value");
        
        // Get global variable
        Object value = globalService.getGlobal("test_key");
        assertThat(value).isEqualTo("test_value");
    }
    
    @Test
    void testTypedGlobalVariable() {
        // Set typed global variable
        globalService.setGlobal("test_int", 42);
        
        // Get typed global variable
        Integer value = globalService.getGlobal("test_int", Integer.class);
        assertThat(value).isEqualTo(42);
    }
    
    @Test
    void testGlobalVariableWithDefault() {
        // Get non-existent global with default
        String value = globalService.getGlobal("non_existent", "default_value");
        assertThat(value).isEqualTo("default_value");
    }
}
```

```java
// TuskLang global testing
test_globals: {
    # Test global variable operations
    @global.set("test_key", "test_value")
    test_value: @global.get("test_key")
    assert(@test_value == "test_value", "Global variable should be set correctly")
    
    # Test global variable with default
    default_value: @global.get("non_existent", "default_value")
    assert(@default_value == "default_value", "Should return default value")
    
    # Test global variable removal
    @global.remove("test_key")
    removed_value: @global.get("test_key")
    assert(@removed_value == null, "Global variable should be removed")
}
```

## Best Practices

### 1. Namespace Global Variables
```java
// Use namespaces to organize global variables
@Component
public class NamespacedGlobalService {
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    public void setAppGlobal(String key, Object value) {
        registry.set("app." + key, value);
    }
    
    public void setDbGlobal(String key, Object value) {
        registry.set("db." + key, value);
    }
    
    public void setCacheGlobal(String key, Object value) {
        registry.set("cache." + key, value);
    }
}
```

### 2. Immutable Global Variables
```java
// Use immutable global variables for configuration
@Component
public class ImmutableGlobalService {
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    public void setImmutableGlobal(String key, Object value) {
        if (!registry.has(key)) {
            registry.set(key, value);
        } else {
            throw new IllegalStateException("Global variable " + key + " is immutable");
        }
    }
}
```

### 3. Thread-Safe Global Variables
```java
// Ensure thread safety for global variables
@Component
public class ThreadSafeGlobalService {
    
    private final ReadWriteLock lock = new ReentrantReadWriteLock();
    
    @Autowired
    private GlobalVariableRegistry registry;
    
    public void setGlobalThreadSafe(String key, Object value) {
        lock.writeLock().lock();
        try {
            registry.set(key, value);
        } finally {
            lock.writeLock().unlock();
        }
    }
    
    public Object getGlobalThreadSafe(String key) {
        lock.readLock().lock();
        try {
            return registry.get(key);
        } finally {
            lock.readLock().unlock();
        }
    }
}
```

The `@global` operator in Java provides enterprise-grade global variable management that integrates seamlessly with Spring Boot's ApplicationContext and supports various scopes, persistence, validation, and monitoring capabilities. 
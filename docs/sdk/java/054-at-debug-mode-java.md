# @debug - Debug Mode in Java

The `@debug` operator provides comprehensive debugging capabilities for Java applications, integrating with Spring Boot's logging system, JVM debugging tools, and enterprise monitoring solutions.

## Basic Syntax

```java
// TuskLang configuration
debug_mode: @debug.enabled()
debug_level: @debug.level()  // "trace", "debug", "info", "warn", "error"
debug_output: @debug.output()  // "console", "file", "remote"
```

```java
// Java Spring Boot integration
@Configuration
@EnableTuskDebug
public class DebugConfig {
    
    @Bean
    public TuskDebugService tuskDebugService() {
        return TuskDebugService.builder()
            .enabled(true)
            .level(LogLevel.DEBUG)
            .output(DebugOutput.CONSOLE)
            .build();
    }
}
```

## Debug Configuration

```java
// application.yml
tusk:
  debug:
    enabled: true
    level: DEBUG
    output: CONSOLE
    file:
      path: "/var/log/tusk-debug.log"
      max-size: "100MB"
      max-files: 10
    remote:
      host: "debug.tusklang.org"
      port: 8080
      api-key: "${DEBUG_API_KEY}"
```

```java
// TuskLang configuration with debug settings
app_config: {
    debug: @debug.enabled()
    
    # Debug levels
    trace_enabled: @debug.level("trace")
    debug_enabled: @debug.level("debug")
    info_enabled: @debug.level("info")
    
    # Debug outputs
    console_debug: @debug.output("console")
    file_debug: @debug.output("file")
    remote_debug: @debug.output("remote")
    
    # Debug file settings
    debug_file: @debug.file.path()
    debug_max_size: @debug.file.max_size()
    debug_max_files: @debug.file.max_files()
}
```

## Debug Logging

```java
// Java logging with TuskLang debug integration
@Component
public class UserService {
    
    private static final Logger logger = LoggerFactory.getLogger(UserService.class);
    
    @Autowired
    private TuskDebugService debugService;
    
    public User createUser(UserDto userDto) {
        // Debug logging
        if (debugService.isEnabled()) {
            logger.debug("Creating user: {}", userDto);
            debugService.log("user_creation", Map.of(
                "email", userDto.getEmail(),
                "timestamp", Instant.now()
            ));
        }
        
        try {
            User user = userRepository.save(userDto.toEntity());
            
            if (debugService.isEnabled()) {
                logger.debug("User created successfully: {}", user.getId());
                debugService.log("user_created", Map.of(
                    "user_id", user.getId(),
                    "duration_ms", System.currentTimeMillis()
                ));
            }
            
            return user;
        } catch (Exception e) {
            if (debugService.isEnabled()) {
                logger.error("Failed to create user: {}", e.getMessage(), e);
                debugService.log("user_creation_error", Map.of(
                    "error", e.getMessage(),
                    "stack_trace", Arrays.toString(e.getStackTrace())
                ));
            }
            throw e;
        }
    }
}
```

```java
// TuskLang debug logging
create_user: (user_data) => {
    # Debug logging
    @debug.log("user_creation_start", {
        email: @user_data.email
        timestamp: @date.now()
    })
    
    # Create user
    user: @User.create(@user_data)
    
    # Debug success
    @debug.log("user_creation_success", {
        user_id: @user.id
        duration_ms: @debug.timer("user_creation")
    })
    
    return @user
}
```

## Debug Timers

```java
// Java timer integration
@Component
public class PerformanceService {
    
    @Autowired
    private TuskDebugService debugService;
    
    public void expensiveOperation() {
        String timerId = debugService.startTimer("expensive_operation");
        
        try {
            // Perform expensive operation
            Thread.sleep(1000);
            
            debugService.stopTimer(timerId);
            debugService.log("operation_completed", Map.of(
                "duration_ms", debugService.getTimerDuration(timerId)
            ));
        } catch (Exception e) {
            debugService.stopTimer(timerId);
            debugService.log("operation_failed", Map.of(
                "error", e.getMessage(),
                "duration_ms", debugService.getTimerDuration(timerId)
            ));
            throw e;
        }
    }
}
```

```java
// TuskLang debug timers
process_data: (data) => {
    # Start timer
    @debug.timer.start("data_processing")
    
    # Process data
    result: @process(@data)
    
    # Stop timer and log
    duration: @debug.timer.stop("data_processing")
    @debug.log("data_processed", {
        input_size: @data.length
        output_size: @result.length
        duration_ms: @duration
    })
    
    return @result
}
```

## Debug Variables

```java
// Java debug variable tracking
@Component
public class DebugVariableService {
    
    @Autowired
    private TuskDebugService debugService;
    
    public void trackVariables(String operation, Map<String, Object> variables) {
        if (debugService.isEnabled()) {
            debugService.setVariable(operation, variables);
        }
    }
    
    public Map<String, Object> getVariables(String operation) {
        return debugService.getVariables(operation);
    }
}
```

```java
// TuskLang debug variables
calculate_tax: (amount, rate) => {
    # Set debug variables
    @debug.variable("tax_calculation", {
        amount: @amount
        rate: @rate
        timestamp: @date.now()
    })
    
    # Calculate tax
    tax: @amount * @rate
    
    # Update debug variables
    @debug.variable("tax_calculation", {
        amount: @amount
        rate: @rate
        tax: @tax
        timestamp: @date.now()
    })
    
    return @tax
}
```

## Debug Breakpoints

```java
// Java debug breakpoints
@Component
public class DebugBreakpointService {
    
    @Autowired
    private TuskDebugService debugService;
    
    public void setBreakpoint(String name, String condition) {
        debugService.setBreakpoint(name, condition);
    }
    
    public void checkBreakpoint(String name, Map<String, Object> context) {
        if (debugService.shouldBreak(name, context)) {
            // Trigger breakpoint
            debugService.break(name, context);
        }
    }
}
```

```java
// TuskLang debug breakpoints
validate_user: (user) => {
    # Set breakpoint for invalid users
    @debug.breakpoint("invalid_user", @user.email == "" || @user.email == null)
    
    # Check breakpoint
    @debug.break("user_validation", {
        user_id: @user.id
        email: @user.email
        valid: @user.email != "" && @user.email != null
    })
    
    # Continue processing
    return @user.email != "" && @user.email != null
}
```

## Debug Memory Tracking

```java
// Java memory debugging
@Component
public class MemoryDebugService {
    
    @Autowired
    private TuskDebugService debugService;
    
    public void trackMemory(String operation) {
        Runtime runtime = Runtime.getRuntime();
        long usedMemory = runtime.totalMemory() - runtime.freeMemory();
        
        debugService.log("memory_usage", Map.of(
            "operation", operation,
            "used_mb", usedMemory / 1024 / 1024,
            "total_mb", runtime.totalMemory() / 1024 / 1024,
            "free_mb", runtime.freeMemory() / 1024 / 1024
        ));
    }
}
```

```java
// TuskLang memory debugging
process_large_data: (data) => {
    # Track memory before processing
    @debug.memory.track("before_processing")
    
    # Process data
    result: @process(@data)
    
    # Track memory after processing
    @debug.memory.track("after_processing")
    
    # Log memory difference
    @debug.log("memory_usage", {
        before_mb: @debug.memory.get("before_processing")
        after_mb: @debug.memory.get("after_processing")
        difference_mb: @debug.memory.diff("before_processing", "after_processing")
    })
    
    return @result
}
```

## Debug Profiling

```java
// Java profiling integration
@Component
public class ProfilingService {
    
    @Autowired
    private TuskDebugService debugService;
    
    public void profileOperation(String operation, Runnable task) {
        long startTime = System.nanoTime();
        long startMemory = Runtime.getRuntime().totalMemory() - Runtime.getRuntime().freeMemory();
        
        try {
            task.run();
        } finally {
            long endTime = System.nanoTime();
            long endMemory = Runtime.getRuntime().totalMemory() - Runtime.getRuntime().freeMemory();
            
            debugService.log("profile", Map.of(
                "operation", operation,
                "duration_ns", endTime - startTime,
                "memory_delta_bytes", endMemory - startMemory,
                "cpu_usage", getCpuUsage()
            ));
        }
    }
    
    private double getCpuUsage() {
        // Implementation for CPU usage tracking
        return 0.0;
    }
}
```

```java
// TuskLang profiling
profile_function: (function_name, function) => {
    # Start profiling
    @debug.profile.start(@function_name)
    
    # Execute function
    result: @function()
    
    # Stop profiling
    profile_data: @debug.profile.stop(@function_name)
    
    # Log profile data
    @debug.log("function_profile", {
        function: @function_name
        duration_ms: @profile_data.duration
        memory_mb: @profile_data.memory
        cpu_percent: @profile_data.cpu
    })
    
    return @result
}
```

## Debug Remote Monitoring

```java
// Java remote debugging
@Component
public class RemoteDebugService {
    
    @Autowired
    private TuskDebugService debugService;
    
    public void sendToRemote(String event, Map<String, Object> data) {
        if (debugService.isRemoteEnabled()) {
            debugService.sendRemote(event, data);
        }
    }
    
    public void enableRemoteDebugging(String host, int port, String apiKey) {
        debugService.configureRemote(host, port, apiKey);
    }
}
```

```java
// TuskLang remote debugging
remote_debug: {
    # Configure remote debugging
    @debug.remote.enable("debug.tusklang.org", 8080, @env.DEBUG_API_KEY)
    
    # Send debug events
    @debug.remote.send("app_started", {
        version: @app.version
        environment: @env.NODE_ENV
        timestamp: @date.now()
    })
    
    # Send performance metrics
    @debug.remote.send("performance_metrics", {
        memory_usage: @debug.memory.current()
        cpu_usage: @debug.cpu.current()
        active_connections: @debug.connections.active()
    })
}
```

## Debug Configuration Examples

```java
// Development environment
dev_debug: {
    enabled: true
    level: "debug"
    output: "console"
    file: {
        path: "/tmp/tusk-debug.log"
        max_size: "50MB"
        max_files: 5
    }
    remote: {
        enabled: false
    }
}

// Production environment
prod_debug: {
    enabled: false
    level: "error"
    output: "file"
    file: {
        path: "/var/log/tusk-debug.log"
        max_size: "100MB"
        max_files: 10
    }
    remote: {
        enabled: true
        host: "monitoring.tusklang.org"
        port: 8080
        api_key: @env.DEBUG_API_KEY
    }
}

// Testing environment
test_debug: {
    enabled: true
    level: "trace"
    output: "console"
    file: {
        path: "/tmp/test-debug.log"
        max_size: "10MB"
        max_files: 1
    }
    remote: {
        enabled: false
    }
}
```

## Debug Testing

```java
// JUnit test with debug integration
@SpringBootTest
class DebugServiceTest {
    
    @Autowired
    private TuskDebugService debugService;
    
    @Test
    void testDebugLogging() {
        // Enable debug for test
        debugService.setEnabled(true);
        debugService.setLevel(LogLevel.DEBUG);
        
        // Test debug logging
        debugService.log("test_event", Map.of("test", true));
        
        // Verify log was created
        List<DebugLog> logs = debugService.getLogs();
        assertThat(logs).hasSize(1);
        assertThat(logs.get(0).getEvent()).isEqualTo("test_event");
    }
    
    @Test
    void testDebugTimer() {
        String timerId = debugService.startTimer("test_timer");
        
        // Simulate work
        Thread.sleep(100);
        
        long duration = debugService.stopTimer(timerId);
        assertThat(duration).isGreaterThan(0);
    }
}
```

```java
// TuskLang debug testing
test_debug: {
    # Enable debug for testing
    @debug.enable()
    @debug.level("trace")
    
    # Test debug logging
    @debug.log("test_event", { test: true })
    
    # Test debug timer
    @debug.timer.start("test_timer")
    @sleep(100)  # Simulate work
    duration: @debug.timer.stop("test_timer")
    
    # Verify results
    assert(@duration > 0, "Timer should record duration")
    assert(@debug.logs.count() > 0, "Should have debug logs")
}
```

## Best Practices

### 1. Environment-Specific Debugging
```java
// Use different debug levels per environment
@Configuration
public class EnvironmentDebugConfig {
    
    @Bean
    @Profile("dev")
    public TuskDebugService devDebugService() {
        return TuskDebugService.builder()
            .enabled(true)
            .level(LogLevel.DEBUG)
            .output(DebugOutput.CONSOLE)
            .build();
    }
    
    @Bean
    @Profile("prod")
    public TuskDebugService prodDebugService() {
        return TuskDebugService.builder()
            .enabled(false)
            .level(LogLevel.ERROR)
            .output(DebugOutput.FILE)
            .build();
    }
}
```

### 2. Performance-Safe Debugging
```java
// Use conditional debugging to avoid performance impact
@Component
public class PerformanceSafeDebugService {
    
    public void debugIfEnabled(String event, Supplier<Map<String, Object>> dataSupplier) {
        if (isDebugEnabled()) {
            Map<String, Object> data = dataSupplier.get();
            log(event, data);
        }
    }
    
    private boolean isDebugEnabled() {
        return debugService.isEnabled() && debugService.getLevel().ordinal() <= LogLevel.DEBUG.ordinal();
    }
}
```

### 3. Structured Debug Data
```java
// Use structured debug data for better analysis
@Component
public class StructuredDebugService {
    
    public void logStructured(String event, String operation, Map<String, Object> context) {
        Map<String, Object> structuredData = Map.of(
            "event", event,
            "operation", operation,
            "timestamp", Instant.now(),
            "context", context,
            "thread", Thread.currentThread().getName(),
            "memory_usage", getCurrentMemoryUsage()
        );
        
        debugService.log(event, structuredData);
    }
}
```

## Integration with Monitoring Tools

```java
// Integration with Micrometer for metrics
@Component
public class MicrometerDebugIntegration {
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    @EventListener
    public void handleDebugEvent(DebugEvent event) {
        // Convert debug events to metrics
        Counter.builder("tusk.debug.events")
            .tag("event", event.getEvent())
            .tag("level", event.getLevel().toString())
            .register(meterRegistry)
            .increment();
    }
}
```

```java
// Integration with ELK stack
@Component
public class ELKDebugIntegration {
    
    @Autowired
    private ElasticsearchTemplate elasticsearchTemplate;
    
    @EventListener
    public void handleDebugEvent(DebugEvent event) {
        // Send debug events to Elasticsearch
        DebugLogDocument logDoc = DebugLogDocument.builder()
            .event(event.getEvent())
            .level(event.getLevel())
            .data(event.getData())
            .timestamp(event.getTimestamp())
            .build();
        
        elasticsearchTemplate.save(logDoc);
    }
}
```

The `@debug` operator in Java provides enterprise-grade debugging capabilities that integrate seamlessly with Spring Boot, JVM tools, and monitoring systems. It enables comprehensive application introspection while maintaining performance and security standards. 
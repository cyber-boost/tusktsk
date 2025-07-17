# ☕ TuskLang Java Variables Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang variables in Java with comprehensive coverage of variable syntax, Java integration patterns, variable scoping, and best practices for dynamic configuration management.

## 🎯 Variable Basics

### Simple Variables

```tsk
# Define variables
app_name: "My TuskLang App"
version: "1.0.0"
debug: true

# Use variables
[app]
name: $app_name
version: $version
debug_mode: $debug

# Variables with different types
port: 8080
host: "localhost"
ssl_enabled: false
max_connections: 100
```

### Java Variable Access

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class VariableConfig {
    public String appName;
    public String version;
    public boolean debug;
    public AppConfig app;
}

@TuskConfig
public class AppConfig {
    public String name;
    public String version;
    public boolean debugMode;
}

public class VariablesExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        VariableConfig config = parser.parseFile("config.tsk", VariableConfig.class);
        
        // Access variables
        System.out.println("App name: " + config.appName);
        System.out.println("Version: " + config.version);
        System.out.println("Debug: " + config.debug);
        
        // Access nested variables
        System.out.println("App config name: " + config.app.name);
        System.out.println("App config version: " + config.app.version);
        System.out.println("App config debug: " + config.app.debugMode);
    }
}
```

## 🔄 Variable References

### Basic Variable References

```tsk
# Define base variables
base_url: "https://api.example.com"
api_version: "v1"
timeout: 30

# Use variable references
[api]
base_url: $base_url
version: $api_version
timeout_seconds: $timeout
full_url: $base_url + "/" + $api_version

[client]
timeout: $timeout
retry_attempts: 3
max_connections: 100
```

### Java Variable Reference Handling

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class ReferenceConfig {
    public String baseUrl;
    public String apiVersion;
    public int timeout;
    public ApiConfig api;
    public ClientConfig client;
}

@TuskConfig
public class ApiConfig {
    public String baseUrl;
    public String version;
    public int timeoutSeconds;
    public String fullUrl;
}

@TuskConfig
public class ClientConfig {
    public int timeout;
    public int retryAttempts;
    public int maxConnections;
}

public class ReferenceExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ReferenceConfig config = parser.parseFile("config.tsk", ReferenceConfig.class);
        
        // Access base variables
        System.out.println("Base URL: " + config.baseUrl);
        System.out.println("API Version: " + config.apiVersion);
        System.out.println("Timeout: " + config.timeout);
        
        // Access referenced variables
        System.out.println("API Base URL: " + config.api.baseUrl);
        System.out.println("API Full URL: " + config.api.fullUrl);
        System.out.println("Client Timeout: " + config.client.timeout);
    }
}
```

## 📊 Complex Variable Expressions

### String Concatenation

```tsk
# String variables
protocol: "https"
domain: "api.example.com"
path: "/users"

# Complex string expressions
full_url: $protocol + "://" + $domain + $path
api_endpoint: $full_url + "/profile"
health_check: $protocol + "://" + $domain + "/health"

# Template-style strings
welcome_message: "Welcome to " + $app_name + " v" + $version
error_message: "Error connecting to " + $domain + " on port " + $port
```

### Mathematical Expressions

```tsk
# Numeric variables
base_port: 8000
port_offset: 100
max_workers: 10
timeout_base: 30

# Mathematical expressions
server_port: $base_port + $port_offset
worker_timeout: $timeout_base * 2
connection_limit: $max_workers * 5
retry_delay: $timeout_base / 2
```

### Boolean Expressions

```tsk
# Boolean variables
development: true
production: false
ssl_available: true

# Boolean expressions
debug_enabled: $development && $ssl_available
production_mode: $production
ssl_required: $production || $ssl_available
```

### Java Expression Handling

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class ExpressionConfig {
    public String protocol;
    public String domain;
    public String path;
    public String fullUrl;
    public String apiEndpoint;
    public String healthCheck;
    
    public int basePort;
    public int portOffset;
    public int serverPort;
    public int workerTimeout;
    
    public boolean development;
    public boolean production;
    public boolean sslAvailable;
    public boolean debugEnabled;
    public boolean sslRequired;
}

public class ExpressionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ExpressionConfig config = parser.parseFile("config.tsk", ExpressionConfig.class);
        
        // Access computed expressions
        System.out.println("Full URL: " + config.fullUrl);
        System.out.println("API Endpoint: " + config.apiEndpoint);
        System.out.println("Health Check: " + config.healthCheck);
        
        System.out.println("Server Port: " + config.serverPort);
        System.out.println("Worker Timeout: " + config.workerTimeout);
        
        System.out.println("Debug Enabled: " + config.debugEnabled);
        System.out.println("SSL Required: " + config.sslRequired);
    }
}
```

## 🔧 Variable Scoping

### Global Variables

```tsk
# Global variables (file scope)
app_name: "My TuskLang App"
version: "1.0.0"
environment: "development"

# All sections can access these variables
[app]
name: $app_name
version: $version

[database]
name: $app_name + "_db"
```

### Section-Specific Variables

```tsk
# Global variables
base_url: "https://api.example.com"

[api]
# Section-specific variables
version: "v1"
timeout: 30
endpoint: $base_url + "/" + $version

[client]
# Different section-specific variables
timeout: 60
retry_attempts: 3
max_connections: 100
```

### Nested Section Variables

```tsk
# Global variables
app_name: "My App"

[application]
# Application-level variables
version: "1.0.0"
debug: true

[application.database]
# Database-specific variables
host: "localhost"
port: 5432
name: $app_name + "_" + $application.version + "_db"

[application.server]
# Server-specific variables
host: "0.0.0.0"
port: 8080
ssl: $application.debug == false
```

### Java Scoping Integration

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class ScopedConfig {
    public String appName;
    public String version;
    public String environment;
    public ApplicationConfig application;
}

@TuskConfig
public class ApplicationConfig {
    public String version;
    public boolean debug;
    public DatabaseConfig database;
    public ServerConfig server;
}

@TuskConfig
public class DatabaseConfig {
    public String host;
    public int port;
    public String name;
}

@TuskConfig
public class ServerConfig {
    public String host;
    public int port;
    public boolean ssl;
}

public class ScopingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ScopedConfig config = parser.parseFile("config.tsk", ScopedConfig.class);
        
        // Access global variables
        System.out.println("Global App Name: " + config.appName);
        System.out.println("Global Version: " + config.version);
        System.out.println("Global Environment: " + config.environment);
        
        // Access section variables
        System.out.println("App Version: " + config.application.version);
        System.out.println("App Debug: " + config.application.debug);
        
        // Access nested section variables
        System.out.println("Database: " + config.application.database.host + ":" + config.application.database.port);
        System.out.println("Database Name: " + config.application.database.name);
        System.out.println("Server: " + config.application.server.host + ":" + config.application.server.port);
        System.out.println("Server SSL: " + config.application.server.ssl);
    }
}
```

## 🔄 Dynamic Variables

### Environment Variables

```tsk
# Use environment variables
app_name: @env("APP_NAME", "Default App")
version: @env("APP_VERSION", "1.0.0")
debug: @env("DEBUG", "false")

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "myapp")
user: @env("DB_USER", "postgres")
password: @env.secure("DB_PASSWORD")
```

### Date and Time Variables

```tsk
# Date and time variables
current_time: @date.now()
formatted_date: @date("yyyy-MM-dd")
start_of_day: @date.startOfDay()
end_of_day: @date.endOfDay()

[logging]
timestamp: $current_time
log_file: "app_" + $formatted_date + ".log"
```

### Computed Variables

```tsk
# Computed variables using @ operators
user_count: @query("SELECT COUNT(*) FROM users")
active_sessions: @query("SELECT COUNT(*) FROM sessions WHERE active = true")
system_memory: @php("memory_get_usage(true)")

[metrics]
total_users: $user_count
active_users: $active_sessions
memory_usage: $system_memory
```

### Java Dynamic Variable Handling

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class DynamicConfig {
    public String appName;
    public String version;
    public boolean debug;
    public DatabaseConfig database;
    public LoggingConfig logging;
    public MetricsConfig metrics;
}

@TuskConfig
public class DatabaseConfig {
    public String host;
    public int port;
    public String name;
    public String user;
    public String password;
}

@TuskConfig
public class LoggingConfig {
    public String timestamp;
    public String logFile;
}

@TuskConfig
public class MetricsConfig {
    public int totalUsers;
    public int activeUsers;
    public long memoryUsage;
}

public class DynamicVariablesExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Set up environment variables for testing
        System.setProperty("APP_NAME", "Dynamic App");
        System.setProperty("APP_VERSION", "2.0.0");
        System.setProperty("DEBUG", "true");
        
        DynamicConfig config = parser.parseFile("config.tsk", DynamicConfig.class);
        
        // Access dynamic variables
        System.out.println("Dynamic App Name: " + config.appName);
        System.out.println("Dynamic Version: " + config.version);
        System.out.println("Dynamic Debug: " + config.debug);
        
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Log File: " + config.logging.logFile);
        System.out.println("Total Users: " + config.metrics.totalUsers);
    }
}
```

## 🔧 Variable Utilities

### Variable Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.VariableValidator;
import java.util.List;
import java.util.Map;

public class VariableValidationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Validate variables
        VariableValidator validator = new VariableValidator();
        List<String> validationErrors = validator.validateVariables(config);
        
        if (validationErrors.isEmpty()) {
            System.out.println("All variables are valid");
        } else {
            System.out.println("Variable validation errors:");
            for (String error : validationErrors) {
                System.err.println("  " + error);
            }
        }
        
        // Check for undefined variables
        List<String> undefinedVars = validator.findUndefinedVariables(config);
        
        if (!undefinedVars.isEmpty()) {
            System.out.println("Undefined variables:");
            for (String var : undefinedVars) {
                System.out.println("  " + var);
            }
        }
    }
}
```

### Variable Resolution

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.variables.VariableResolver;
import java.util.Map;

public class VariableResolutionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Resolve variables
        VariableResolver resolver = new VariableResolver();
        Map<String, Object> resolvedConfig = resolver.resolveAllVariables(config);
        
        System.out.println("Resolved configuration:");
        for (Map.Entry<String, Object> entry : resolvedConfig.entrySet()) {
            System.out.println("  " + entry.getKey() + ": " + entry.getValue());
        }
        
        // Resolve specific variable
        String resolvedValue = resolver.resolveVariable("$app_name + ' v' + $version", config);
        System.out.println("Resolved expression: " + resolvedValue);
    }
}
```

## 🧪 Testing Variables

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import org.tusklang.java.variables.VariableResolver;
import java.util.Map;

class VariablesTest {
    
    private TuskLang parser;
    private VariableResolver resolver;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
        resolver = new VariableResolver();
    }
    
    @Test
    void testSimpleVariables() {
        String tskContent = """
            app_name: "Test App"
            version: "1.0.0"
            
            [app]
            name: $app_name
            version: $version
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        Map<String, Object> resolved = resolver.resolveAllVariables(config);
        
        Map<String, Object> appSection = (Map<String, Object>) resolved.get("app");
        assertEquals("Test App", appSection.get("name"));
        assertEquals("1.0.0", appSection.get("version"));
    }
    
    @Test
    void testStringConcatenation() {
        String tskContent = """
            protocol: "https"
            domain: "api.example.com"
            path: "/users"
            
            full_url: $protocol + "://" + $domain + $path
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        Map<String, Object> resolved = resolver.resolveAllVariables(config);
        
        assertEquals("https://api.example.com/users", resolved.get("full_url"));
    }
    
    @Test
    void testMathematicalExpressions() {
        String tskContent = """
            base_port: 8000
            port_offset: 100
            
            server_port: $base_port + $port_offset
            worker_timeout: $base_port / 2
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        Map<String, Object> resolved = resolver.resolveAllVariables(config);
        
        assertEquals(8100, resolved.get("server_port"));
        assertEquals(4000, resolved.get("worker_timeout"));
    }
    
    @Test
    void testBooleanExpressions() {
        String tskContent = """
            development: true
            production: false
            ssl_available: true
            
            debug_enabled: $development && $ssl_available
            ssl_required: $production || $ssl_available
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        Map<String, Object> resolved = resolver.resolveAllVariables(config);
        
        assertTrue((Boolean) resolved.get("debug_enabled"));
        assertTrue((Boolean) resolved.get("ssl_required"));
    }
}
```

## 🔧 Troubleshooting

### Common Variable Issues

1. **Undefined Variables**
```tsk
# Error: Variable not defined
app_name: "My App"
version: $undefined_variable  # This will cause an error

# Solution: Define the variable first
app_name: "My App"
version: "1.0.0"
full_name: $app_name + " v" + $version
```

2. **Circular References**
```tsk
# Error: Circular reference
var1: $var2
var2: $var1  # This creates a circular reference

# Solution: Use direct values or restructure
var1: "value1"
var2: "value2"
combined: $var1 + " and " + $var2
```

3. **Type Mismatches**
```tsk
# Error: String + number without conversion
port: 8080
url: "http://localhost:" + $port  # This works in TuskLang

# Solution: TuskLang handles type conversion automatically
port: 8080
url: "http://localhost:" + $port
```

## 📚 Best Practices

### Variable Naming

1. **Use descriptive names**
```tsk
# Good: Descriptive variable names
application_name: "My App"
database_host: "localhost"
max_connection_count: 100

# Bad: Unclear variable names
app: "My App"
host: "localhost"
max: 100
```

2. **Use consistent naming conventions**
```tsk
# Use snake_case for variable names
app_name: "My App"
db_host: "localhost"
api_version: "v1"
ssl_enabled: true
```

3. **Group related variables**
```tsk
# Group related variables together
# Application settings
app_name: "My App"
app_version: "1.0.0"
app_environment: "development"

# Database settings
db_host: "localhost"
db_port: 5432
db_name: "myapp"
```

### Variable Organization

1. **Define variables at the top**
```tsk
# Global variables (define first)
app_name: "My App"
version: "1.0.0"
environment: "development"

# Use variables in sections
[app]
name: $app_name
version: $version
```

2. **Use sections for organization**
```tsk
# Global variables
app_name: "My App"

[application]
version: "1.0.0"
debug: true

[database]
host: "localhost"
name: $app_name + "_db"
```

## 📚 Next Steps

1. **Master variable syntax** - Understand all variable types and expressions
2. **Implement dynamic variables** - Use @ operators for dynamic values
3. **Use variable scoping** - Organize variables by scope and purpose
4. **Add variable validation** - Ensure variable integrity and correctness
5. **Test variable handling** - Create comprehensive variable testing

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang variables in Java! From simple variables to complex expressions, you can create dynamic, maintainable configurations with proper scoping and validation. 
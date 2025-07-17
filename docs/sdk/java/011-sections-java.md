# ☕ TuskLang Java Sections Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang sections in Java with comprehensive coverage of section syntax, Java integration patterns, nested sections, and best practices for organizing configuration data.

## 🎯 Section Basics

### Traditional INI-Style Sections

```tsk
# Basic sections
[app]
name: "My TuskLang App"
version: "1.0.0"
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

[server]
host: "0.0.0.0"
port: 8080
ssl: false
```

### Java Section Mapping

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class AppConfig {
    public String name;
    public String version;
    public boolean debug;
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
public class ServerConfig {
    public String host;
    public int port;
    public boolean ssl;
}

@TuskConfig
public class MainConfig {
    public AppConfig app;
    public DatabaseConfig database;
    public ServerConfig server;
}

public class SectionsExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        MainConfig config = parser.parseFile("config.tsk", MainConfig.class);
        
        // Access section data
        System.out.println("App: " + config.app.name + " v" + config.app.version);
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Server: " + config.server.host + ":" + config.server.port);
    }
}
```

## 🔄 Alternative Syntax Styles

### JSON-Like Objects

```tsk
# JSON-like object syntax
app {
    name: "My TuskLang App"
    version: "1.0.0"
    debug: true
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: "secret"
}

server {
    host: "0.0.0.0"
    port: 8080
    ssl: false
}
```

### XML-Inspired Syntax

```tsk
# XML-inspired syntax
app >
    name: "My TuskLang App"
    version: "1.0.0"
    debug: true
<

database >
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: "secret"
<

server >
    host: "0.0.0.0"
    port: 8080
    ssl: false
<
```

### Java Integration with Different Syntax

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@TuskConfig
public class FlexibleConfig {
    public AppConfig app;
    public DatabaseConfig database;
    public ServerConfig server;
}

public class FlexibleSyntaxExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Parse different syntax styles
        FlexibleConfig iniConfig = parser.parseFile("config-ini.tsk", FlexibleConfig.class);
        FlexibleConfig jsonConfig = parser.parseFile("config-json.tsk", FlexibleConfig.class);
        FlexibleConfig xmlConfig = parser.parseFile("config-xml.tsk", FlexibleConfig.class);
        
        // All produce the same Java objects
        System.out.println("INI: " + iniConfig.app.name);
        System.out.println("JSON: " + jsonConfig.app.name);
        System.out.println("XML: " + xmlConfig.app.name);
    }
}
```

## 📊 Nested Sections

### Deep Nesting

```tsk
[application]
name: "My App"
version: "1.0.0"

[application.database]
host: "localhost"
port: 5432
name: "myapp"

[application.database.connection]
pool_size: 10
timeout: 30
retry_attempts: 3

[application.database.connection.ssl]
enabled: true
cert_file: "/path/to/cert.pem"
key_file: "/path/to/key.pem"

[application.server]
host: "0.0.0.0"
port: 8080

[application.server.security]
ssl_enabled: true
cors_enabled: true
rate_limiting: true

[application.server.security.cors]
allowed_origins: ["http://localhost:3000", "https://example.com"]
allowed_methods: ["GET", "POST", "PUT", "DELETE"]
allowed_headers: ["Content-Type", "Authorization"]
```

### Java Nested Section Mapping

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.List;

@TuskConfig
public class ApplicationConfig {
    public String name;
    public String version;
    public DatabaseConfig database;
    public ServerConfig server;
}

@TuskConfig
public class DatabaseConfig {
    public String host;
    public int port;
    public String name;
    public ConnectionConfig connection;
}

@TuskConfig
public class ConnectionConfig {
    public int poolSize;
    public int timeout;
    public int retryAttempts;
    public SSLConfig ssl;
}

@TuskConfig
public class SSLConfig {
    public boolean enabled;
    public String certFile;
    public String keyFile;
}

@TuskConfig
public class ServerConfig {
    public String host;
    public int port;
    public SecurityConfig security;
}

@TuskConfig
public class SecurityConfig {
    public boolean sslEnabled;
    public boolean corsEnabled;
    public boolean rateLimiting;
    public CORSConfig cors;
}

@TuskConfig
public class CORSConfig {
    public List<String> allowedOrigins;
    public List<String> allowedMethods;
    public List<String> allowedHeaders;
}

public class NestedSectionsExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ApplicationConfig config = parser.parseFile("config.tsk", ApplicationConfig.class);
        
        // Access deeply nested properties
        System.out.println("App: " + config.name + " v" + config.version);
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Pool size: " + config.database.connection.poolSize);
        System.out.println("SSL enabled: " + config.database.connection.ssl.enabled);
        System.out.println("Server: " + config.server.host + ":" + config.server.port);
        System.out.println("CORS origins: " + config.server.security.cors.allowedOrigins);
    }
}
```

## 🔧 Section Access Patterns

### Direct Section Access

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class DirectAccessExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Access sections directly
        Map<String, Object> appSection = (Map<String, Object>) config.get("app");
        Map<String, Object> dbSection = (Map<String, Object>) config.get("database");
        Map<String, Object> serverSection = (Map<String, Object>) config.get("server");
        
        // Access section properties
        String appName = (String) appSection.get("name");
        String dbHost = (String) dbSection.get("host");
        Integer serverPort = (Integer) serverSection.get("port");
        
        System.out.println("App: " + appName);
        System.out.println("Database: " + dbHost);
        System.out.println("Server port: " + serverPort);
    }
}
```

### Nested Section Access

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class NestedAccessExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Access nested sections
        Map<String, Object> appSection = (Map<String, Object>) config.get("application");
        Map<String, Object> dbSection = (Map<String, Object>) appSection.get("database");
        Map<String, Object> connSection = (Map<String, Object>) dbSection.get("connection");
        Map<String, Object> sslSection = (Map<String, Object>) connSection.get("ssl");
        
        // Access deeply nested properties
        String appName = (String) appSection.get("name");
        String dbHost = (String) dbSection.get("host");
        Integer poolSize = (Integer) connSection.get("pool_size");
        Boolean sslEnabled = (Boolean) sslSection.get("enabled");
        
        System.out.println("App: " + appName);
        System.out.println("Database: " + dbHost);
        System.out.println("Pool size: " + poolSize);
        System.out.println("SSL enabled: " + sslEnabled);
    }
}
```

### Section Path Access

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class PathAccessExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Access using dot notation paths
        String appName = parser.get("application.name");
        String dbHost = parser.get("application.database.host");
        Integer poolSize = parser.get("application.database.connection.pool_size");
        Boolean sslEnabled = parser.get("application.database.connection.ssl.enabled");
        
        System.out.println("App: " + appName);
        System.out.println("Database: " + dbHost);
        System.out.println("Pool size: " + poolSize);
        System.out.println("SSL enabled: " + sslEnabled);
        
        // Set values using paths
        parser.set("application.database.connection.pool_size", 20);
        parser.set("application.server.security.rate_limiting", false);
    }
}
```

## 🔄 Section Inheritance and Overrides

### Base Configuration

Create `base.tsk`:

```tsk
[app]
name: "Base App"
version: "1.0.0"
debug: false

[database]
host: "localhost"
port: 5432
name: "base_app"
user: "postgres"
password: "secret"

[server]
host: "0.0.0.0"
port: 8080
ssl: false
```

### Environment-Specific Overrides

Create `development.tsk`:

```tsk
# Import base configuration
@import("base.tsk")

# Override for development
[app]
debug: true

[database]
name: "dev_app"

[server]
port: 3000
```

Create `production.tsk`:

```tsk
# Import base configuration
@import("base.tsk")

# Override for production
[app]
name: "Production App"

[database]
host: "prod-db.example.com"
password: @env.secure("DB_PASSWORD")

[server]
host: "prod-server.example.com"
port: 443
ssl: true
```

### Java Section Inheritance

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@TuskConfig
public class EnvironmentConfig {
    public AppConfig app;
    public DatabaseConfig database;
    public ServerConfig server;
}

public class InheritanceExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Load environment-specific configuration
        String environment = System.getProperty("APP_ENV", "development");
        EnvironmentConfig config = parser.parseFile(environment + ".tsk", EnvironmentConfig.class);
        
        System.out.println("Environment: " + environment);
        System.out.println("App: " + config.app.name + " (debug: " + config.app.debug + ")");
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Server: " + config.server.host + ":" + config.server.port + " (ssl: " + config.server.ssl + ")");
    }
}
```

## 📊 Section Arrays and Lists

### Array of Sections

```tsk
[servers]
server1 {
    name: "web-server-1"
    host: "192.168.1.10"
    port: 8080
    role: "web"
}

server2 {
    name: "web-server-2"
    host: "192.168.1.11"
    port: 8080
    role: "web"
}

server3 {
    name: "db-server-1"
    host: "192.168.1.20"
    port: 5432
    role: "database"
}

[databases]
db1 {
    name: "users"
    host: "db1.example.com"
    port: 5432
    type: "postgresql"
}

db2 {
    name: "analytics"
    host: "db2.example.com"
    port: 27017
    type: "mongodb"
}
```

### Java Array Section Mapping

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.List;

@TuskConfig
public class InfrastructureConfig {
    public List<ServerConfig> servers;
    public List<DatabaseConfig> databases;
}

@TuskConfig
public class ServerConfig {
    public String name;
    public String host;
    public int port;
    public String role;
}

@TuskConfig
public class DatabaseConfig {
    public String name;
    public String host;
    public int port;
    public String type;
}

public class ArraySectionsExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        InfrastructureConfig config = parser.parseFile("config.tsk", InfrastructureConfig.class);
        
        // Process server array
        System.out.println("Servers:");
        for (ServerConfig server : config.servers) {
            System.out.println("  " + server.name + " (" + server.role + ") at " + server.host + ":" + server.port);
        }
        
        // Process database array
        System.out.println("Databases:");
        for (DatabaseConfig db : config.databases) {
            System.out.println("  " + db.name + " (" + db.type + ") at " + db.host + ":" + db.port);
        }
        
        // Filter servers by role
        List<ServerConfig> webServers = config.servers.stream()
            .filter(s -> "web".equals(s.role))
            .collect(Collectors.toList());
        
        System.out.println("Web servers: " + webServers.size());
    }
}
```

## 🔧 Section Utilities

### Section Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.Validator;
import org.tusklang.java.validation.ValidationRule;
import java.util.Map;

public class SectionValidationExample {
    public static void main(String[] args) {
        Validator validator = new Validator();
        
        // Add section-specific validation rules
        validator.addRule(new ValidationRule("app.name", value -> {
            return value != null && !value.toString().isEmpty();
        }, "App name is required"));
        
        validator.addRule(new ValidationRule("database.host", value -> {
            return value != null && !value.toString().isEmpty();
        }, "Database host is required"));
        
        validator.addRule(new ValidationRule("database.port", value -> {
            int port = Integer.parseInt(value.toString());
            return port > 0 && port <= 65535;
        }, "Database port must be between 1 and 65535"));
        
        TuskLang parser = new TuskLang();
        parser.setValidator(validator);
        
        // Parse with section validation
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Configuration validated successfully");
    }
}
```

### Section Merging

```java
import org.tusklang.java.TuskLang;
import java.util.Map;
import java.util.HashMap;

public class SectionMergingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Parse multiple configuration files
        Map<String, Object> baseConfig = parser.parseFile("base.tsk");
        Map<String, Object> overrideConfig = parser.parseFile("override.tsk");
        
        // Merge configurations
        Map<String, Object> mergedConfig = mergeConfigs(baseConfig, overrideConfig);
        
        System.out.println("Merged configuration: " + mergedConfig);
    }
    
    private static Map<String, Object> mergeConfigs(Map<String, Object> base, Map<String, Object> override) {
        Map<String, Object> merged = new HashMap<>(base);
        
        for (Map.Entry<String, Object> entry : override.entrySet()) {
            String key = entry.getKey();
            Object value = entry.getValue();
            
            if (value instanceof Map && merged.get(key) instanceof Map) {
                // Recursively merge nested maps
                @SuppressWarnings("unchecked")
                Map<String, Object> baseMap = (Map<String, Object>) merged.get(key);
                @SuppressWarnings("unchecked")
                Map<String, Object> overrideMap = (Map<String, Object>) value;
                merged.put(key, mergeConfigs(baseMap, overrideMap));
            } else {
                // Override with new value
                merged.put(key, value);
            }
        }
        
        return merged;
    }
}
```

## 🧪 Testing Sections

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import java.util.Map;

class SectionsTest {
    
    private TuskLang parser;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
    }
    
    @Test
    void testBasicSections() {
        String tskContent = """
            [app]
            name: "Test App"
            version: "1.0.0"
            
            [database]
            host: "localhost"
            port: 5432
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        Map<String, Object> appSection = (Map<String, Object>) config.get("app");
        Map<String, Object> dbSection = (Map<String, Object>) config.get("database");
        
        assertEquals("Test App", appSection.get("name"));
        assertEquals("1.0.0", appSection.get("version"));
        assertEquals("localhost", dbSection.get("host"));
        assertEquals(5432, dbSection.get("port"));
    }
    
    @Test
    void testNestedSections() {
        String tskContent = """
            [app]
            name: "Test App"
            
            [app.database]
            host: "localhost"
            port: 5432
            
            [app.database.connection]
            pool_size: 10
            timeout: 30
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        Map<String, Object> appSection = (Map<String, Object>) config.get("app");
        Map<String, Object> dbSection = (Map<String, Object>) appSection.get("database");
        Map<String, Object> connSection = (Map<String, Object>) dbSection.get("connection");
        
        assertEquals("Test App", appSection.get("name"));
        assertEquals("localhost", dbSection.get("host"));
        assertEquals(10, connSection.get("pool_size"));
        assertEquals(30, connSection.get("timeout"));
    }
    
    @Test
    void testSectionPathAccess() {
        String tskContent = """
            [app]
            name: "Test App"
            
            [app.database]
            host: "localhost"
            port: 5432
            """;
        
        parser.parse(tskContent);
        
        assertEquals("Test App", parser.get("app.name"));
        assertEquals("localhost", parser.get("app.database.host"));
        assertEquals(5432, parser.get("app.database.port"));
    }
}
```

## 🔧 Troubleshooting

### Common Section Issues

1. **Section Not Found**
```java
// Check if section exists
Map<String, Object> config = parser.parseFile("config.tsk");
if (config.containsKey("database")) {
    Map<String, Object> dbSection = (Map<String, Object>) config.get("database");
    // Use dbSection
} else {
    System.err.println("Database section not found");
}
```

2. **Nested Section Access**
```java
// Safe nested access
Map<String, Object> config = parser.parseFile("config.tsk");
Map<String, Object> appSection = (Map<String, Object>) config.get("app");
if (appSection != null) {
    Map<String, Object> dbSection = (Map<String, Object>) appSection.get("database");
    if (dbSection != null) {
        String host = (String) dbSection.get("host");
        // Use host
    }
}
```

3. **Section Type Issues**
```java
// Validate section type
Object section = config.get("database");
if (section instanceof Map) {
    @SuppressWarnings("unchecked")
    Map<String, Object> dbSection = (Map<String, Object>) section;
    // Use dbSection
} else {
    System.err.println("Database section is not a map");
}
```

## 📚 Next Steps

1. **Master section syntax** - Understand all section styles and patterns
2. **Implement nested sections** - Build complex hierarchical configurations
3. **Use section inheritance** - Create reusable base configurations
4. **Add section validation** - Validate section structure and content
5. **Test section handling** - Create comprehensive section testing

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang sections in Java! From basic sections to complex nested structures, you can organize and access configuration data efficiently with type safety and validation. 
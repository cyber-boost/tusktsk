# ☕ TuskLang Java Imports Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang imports in Java with comprehensive coverage of import syntax, Java integration patterns, modular configuration, and best practices for organizing configuration across multiple files.

## 🎯 Import Basics

### Basic Import Syntax

```tsk
# Import other TSK files
@import("database.tsk")
@import("server.tsk")
@import("security.tsk")

# Main configuration
app_name: "My TuskLang App"
version: "1.0.0"
debug: true
```

### Java Import Integration

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class ImportedConfig {
    public String appName;
    public String version;
    public boolean debug;
    public DatabaseConfig database;
    public ServerConfig server;
    public SecurityConfig security;
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
public class SecurityConfig {
    public boolean sslEnabled;
    public String certFile;
    public String keyFile;
}

public class ImportsExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ImportedConfig config = parser.parseFile("main.tsk", ImportedConfig.class);
        
        // Access imported configurations
        System.out.println("App: " + config.appName + " v" + config.version);
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Server: " + config.server.host + ":" + config.server.port);
        System.out.println("SSL Enabled: " + config.security.sslEnabled);
    }
}
```

## 📁 Modular Configuration Structure

### Database Configuration

Create `database.tsk`:

```tsk
# Database Configuration
# ---------------------
# PostgreSQL database settings for the application

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

[database.pool]
min_size: 5
max_size: 20
timeout: 30
retry_attempts: 3

[database.ssl]
enabled: false
cert_file: "/path/to/cert.pem"
key_file: "/path/to/key.pem"
```

### Server Configuration

Create `server.tsk`:

```tsk
# Server Configuration
# -------------------
# HTTP server settings and configuration

[server]
host: "0.0.0.0"
port: 8080
ssl: false

[server.cors]
enabled: true
allowed_origins: ["http://localhost:3000", "https://example.com"]
allowed_methods: ["GET", "POST", "PUT", "DELETE"]
allowed_headers: ["Content-Type", "Authorization"]

[server.rate_limiting]
enabled: true
requests_per_minute: 100
burst_size: 20
```

### Security Configuration

Create `security.tsk`:

```tsk
# Security Configuration
# ---------------------
# Security settings and authentication

[security]
ssl_enabled: false
cert_file: "/path/to/cert.pem"
key_file: "/path/to/key.pem"

[security.authentication]
jwt_secret: "your-secret-key"
token_expiry: 3600
refresh_token_expiry: 86400

[security.cors]
enabled: true
allowed_origins: ["http://localhost:3000"]
allowed_methods: ["GET", "POST", "PUT", "DELETE"]
```

### Main Configuration

Create `main.tsk`:

```tsk
# Main Application Configuration
# -----------------------------
# Imports all modular configuration files

@import("database.tsk")
@import("server.tsk")
@import("security.tsk")

# Application settings
app_name: "My TuskLang App"
version: "1.0.0"
debug: true
environment: "development"

# Override imported settings for this environment
[database]
host: "localhost"
name: "myapp_dev"

[server]
port: 3000
ssl: false
```

## 🔄 Import Overrides and Merging

### Base Configuration

Create `base.tsk`:

```tsk
# Base Configuration
# -----------------
# Common settings shared across environments

app_name: "My TuskLang App"
version: "1.0.0"

[database]
host: "localhost"
port: 5432
user: "postgres"
password: "secret"

[server]
host: "0.0.0.0"
ssl: false

[security]
ssl_enabled: false
jwt_secret: "default-secret"
```

### Environment-Specific Overrides

Create `development.tsk`:

```tsk
# Development Configuration
# ------------------------
# Overrides base configuration for development

@import("base.tsk")

# Override for development
debug: true
environment: "development"

[database]
name: "myapp_dev"
password: "dev123"

[server]
port: 3000

[security]
jwt_secret: "dev-secret-key"
```

Create `production.tsk`:

```tsk
# Production Configuration
# -----------------------
# Overrides base configuration for production

@import("base.tsk")

# Override for production
debug: false
environment: "production"

[database]
host: @env("DB_HOST", "prod-db.example.com")
name: @env("DB_NAME", "myapp_prod")
password: @env.secure("DB_PASSWORD")

[server]
port: 443
ssl: true

[security]
ssl_enabled: true
jwt_secret: @env.secure("JWT_SECRET")
```

### Java Import Override Handling

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@TuskConfig
public class EnvironmentConfig {
    public String appName;
    public String version;
    public boolean debug;
    public String environment;
    public DatabaseConfig database;
    public ServerConfig server;
    public SecurityConfig security;
}

public class ImportOverridesExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Load environment-specific configuration
        String environment = System.getProperty("APP_ENV", "development");
        EnvironmentConfig config = parser.parseFile(environment + ".tsk", EnvironmentConfig.class);
        
        System.out.println("Environment: " + environment);
        System.out.println("App: " + config.appName + " v" + config.version);
        System.out.println("Debug: " + config.debug);
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Server: " + config.server.host + ":" + config.server.port);
        System.out.println("SSL: " + config.security.sslEnabled);
    }
}
```

## 📊 Conditional Imports

### Environment-Based Imports

```tsk
# Conditional imports based on environment
@import("base.tsk")

# Import environment-specific configuration
@import.if(@env("APP_ENV") == "development", "development.tsk")
@import.if(@env("APP_ENV") == "production", "production.tsk")
@import.if(@env("APP_ENV") == "testing", "testing.tsk")

# Import optional features
@import.if(@env("ENABLE_ANALYTICS") == "true", "analytics.tsk")
@import.if(@env("ENABLE_MONITORING") == "true", "monitoring.tsk")
```

### Feature-Based Imports

```tsk
# Feature-based imports
@import("core.tsk")

# Import optional features
@import.if($enable_analytics, "analytics.tsk")
@import.if($enable_monitoring, "monitoring.tsk")
@import.if($enable_caching, "cache.tsk")

# Core configuration
enable_analytics: true
enable_monitoring: false
enable_caching: true
```

### Java Conditional Import Handling

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class ConditionalConfig {
    public String appName;
    public String version;
    public boolean enableAnalytics;
    public boolean enableMonitoring;
    public boolean enableCaching;
    public AnalyticsConfig analytics;
    public MonitoringConfig monitoring;
    public CacheConfig cache;
}

@TuskConfig
public class AnalyticsConfig {
    public String provider;
    public String apiKey;
    public boolean enabled;
}

@TuskConfig
public class MonitoringConfig {
    public String endpoint;
    public int interval;
    public boolean enabled;
}

@TuskConfig
public class CacheConfig {
    public String type;
    public String host;
    public int port;
    public boolean enabled;
}

public class ConditionalImportsExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Set environment variables for conditional imports
        System.setProperty("ENABLE_ANALYTICS", "true");
        System.setProperty("ENABLE_MONITORING", "false");
        System.setProperty("ENABLE_CACHING", "true");
        
        ConditionalConfig config = parser.parseFile("conditional.tsk", ConditionalConfig.class);
        
        System.out.println("App: " + config.appName);
        System.out.println("Analytics enabled: " + config.enableAnalytics);
        System.out.println("Monitoring enabled: " + config.enableMonitoring);
        System.out.println("Caching enabled: " + config.enableCaching);
        
        if (config.analytics != null) {
            System.out.println("Analytics provider: " + config.analytics.provider);
        }
        
        if (config.cache != null) {
            System.out.println("Cache type: " + config.cache.type);
        }
    }
}
```

## 🔧 Import Utilities

### Import Resolution

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.imports.ImportResolver;
import java.util.List;
import java.util.Map;

public class ImportResolutionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("main.tsk");
        
        // Resolve imports
        ImportResolver resolver = new ImportResolver();
        List<String> importedFiles = resolver.getImportedFiles(parser.getRawContent());
        
        System.out.println("Imported files:");
        for (String file : importedFiles) {
            System.out.println("  " + file);
        }
        
        // Get import hierarchy
        Map<String, List<String>> importHierarchy = resolver.getImportHierarchy("main.tsk");
        
        System.out.println("Import hierarchy:");
        for (Map.Entry<String, List<String>> entry : importHierarchy.entrySet()) {
            System.out.println("  " + entry.getKey() + " imports:");
            for (String imported : entry.getValue()) {
                System.out.println("    " + imported);
            }
        }
    }
}
```

### Import Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.ImportValidator;
import java.util.List;

public class ImportValidationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        parser.parseFile("main.tsk");
        
        // Validate imports
        ImportValidator validator = new ImportValidator();
        List<String> validationErrors = validator.validateImports(parser.getRawContent());
        
        if (validationErrors.isEmpty()) {
            System.out.println("All imports are valid");
        } else {
            System.out.println("Import validation errors:");
            for (String error : validationErrors) {
                System.err.println("  " + error);
            }
        }
        
        // Check for circular imports
        List<String> circularImports = validator.findCircularImports(parser.getRawContent());
        
        if (!circularImports.isEmpty()) {
            System.out.println("Circular imports detected:");
            for (String circular : circularImports) {
                System.err.println("  " + circular);
            }
        }
    }
}
```

## 📊 Import Statistics

### Import Analysis

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.imports.ImportAnalyzer;
import java.util.Map;

public class ImportAnalysisExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        parser.parseFile("main.tsk");
        
        // Analyze imports
        ImportAnalyzer analyzer = new ImportAnalyzer();
        Map<String, Object> stats = analyzer.analyzeImports(parser.getRawContent());
        
        System.out.println("Import Statistics:");
        System.out.println("  Total files: " + stats.get("totalFiles"));
        System.out.println("  Imported files: " + stats.get("importedFiles"));
        System.out.println("  Import depth: " + stats.get("importDepth"));
        System.out.println("  Circular imports: " + stats.get("circularImports"));
        
        // Get import graph
        Map<String, List<String>> importGraph = analyzer.getImportGraph(parser.getRawContent());
        
        System.out.println("Import graph:");
        for (Map.Entry<String, List<String>> entry : importGraph.entrySet()) {
            System.out.println("  " + entry.getKey() + " -> " + entry.getValue());
        }
    }
}
```

## 🧪 Testing Imports

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import org.tusklang.java.imports.ImportResolver;
import java.util.List;
import java.util.Map;

class ImportsTest {
    
    private TuskLang parser;
    private ImportResolver resolver;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
        resolver = new ImportResolver();
    }
    
    @Test
    void testBasicImports() {
        String tskContent = """
            @import("database.tsk")
            @import("server.tsk")
            
            app_name: "Test App"
            version: "1.0.0"
            """;
        
        List<String> importedFiles = resolver.getImportedFiles(tskContent);
        
        assertEquals(2, importedFiles.size());
        assertTrue(importedFiles.contains("database.tsk"));
        assertTrue(importedFiles.contains("server.tsk"));
    }
    
    @Test
    void testConditionalImports() {
        String tskContent = """
            @import("base.tsk")
            @import.if($environment == "development", "development.tsk")
            @import.if($environment == "production", "production.tsk")
            
            environment: "development"
            """;
        
        List<String> importedFiles = resolver.getImportedFiles(tskContent);
        
        assertTrue(importedFiles.contains("base.tsk"));
        assertTrue(importedFiles.contains("development.tsk"));
        assertFalse(importedFiles.contains("production.tsk"));
    }
    
    @Test
    void testImportOverrides() {
        // Create base configuration
        String baseContent = """
            app_name: "Base App"
            version: "1.0.0"
            
            [database]
            host: "localhost"
            port: 5432
            """;
        
        // Create override configuration
        String overrideContent = """
            @import("base.tsk")
            
            app_name: "Override App"
            
            [database]
            port: 5433
            """;
        
        // Test that overrides work correctly
        Map<String, Object> baseConfig = parser.parse(baseContent);
        Map<String, Object> overrideConfig = parser.parse(overrideContent);
        
        assertEquals("Base App", baseConfig.get("app_name"));
        assertEquals("Override App", overrideConfig.get("app_name"));
        assertEquals(5432, baseConfig.get("database"));
        assertEquals(5433, overrideConfig.get("database"));
    }
}
```

## 🔧 Troubleshooting

### Common Import Issues

1. **File Not Found**
```tsk
# Error: Imported file doesn't exist
@import("nonexistent.tsk")  # This will cause an error

# Solution: Ensure file exists or use conditional import
@import.if(file_exists("optional.tsk"), "optional.tsk")
```

2. **Circular Imports**
```tsk
# Error: Circular import
# file1.tsk
@import("file2.tsk")

# file2.tsk
@import("file1.tsk")  # This creates a circular import

# Solution: Restructure to avoid circular dependencies
# Use a common base file or restructure the configuration
```

3. **Import Path Issues**
```tsk
# Error: Wrong import path
@import("../config/database.tsk")  # Relative path issues

# Solution: Use absolute paths or ensure correct relative paths
@import("database.tsk")  # Same directory
@import("config/database.tsk")  # Subdirectory
```

## 📚 Best Practices

### Import Organization

1. **Use descriptive file names**
```tsk
# Good: Descriptive file names
@import("database-postgresql.tsk")
@import("server-nginx.tsk")
@import("security-jwt.tsk")

# Bad: Unclear file names
@import("db.tsk")
@import("srv.tsk")
@import("sec.tsk")
```

2. **Group related imports**
```tsk
# Group imports by category
# Core configuration
@import("base.tsk")

# Database configuration
@import("database-postgresql.tsk")
@import("database-redis.tsk")

# Server configuration
@import("server-nginx.tsk")
@import("server-ssl.tsk")

# Security configuration
@import("security-jwt.tsk")
@import("security-cors.tsk")
```

3. **Use conditional imports for optional features**
```tsk
# Import optional features conditionally
@import("core.tsk")

# Optional features
@import.if($enable_analytics, "analytics-google.tsk")
@import.if($enable_monitoring, "monitoring-prometheus.tsk")
@import.if($enable_caching, "cache-redis.tsk")
```

### Import Structure

1. **Create a clear hierarchy**
```tsk
# main.tsk - Main configuration file
@import("base.tsk")
@import("environment/" + $environment + ".tsk")
@import("features/" + $feature + ".tsk")
```

2. **Use environment-specific imports**
```tsk
# development.tsk
@import("base.tsk")
@import("development-specific.tsk")

# production.tsk
@import("base.tsk")
@import("production-specific.tsk")
```

## 📚 Next Steps

1. **Master import syntax** - Understand all import patterns and options
2. **Implement modular configuration** - Organize configuration into logical modules
3. **Use conditional imports** - Create flexible, environment-specific configurations
4. **Add import validation** - Ensure import integrity and prevent circular dependencies
5. **Test import handling** - Create comprehensive import testing

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang imports in Java! From basic imports to complex modular configurations, you can create organized, maintainable configuration systems with proper separation of concerns. 
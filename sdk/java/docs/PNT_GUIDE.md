# 🥜 Peanut Binary Configuration Guide for Java

A comprehensive guide to using TuskLang's high-performance binary configuration system with Java.

## Table of Contents

1. [Installation and Setup](#installation-and-setup)
2. [Quick Start](#quick-start)
3. [Core Concepts](#core-concepts)
4. [API Reference](#api-reference)
5. [Advanced Usage](#advanced-usage)
6. [Java-Specific Features](#java-specific-features)
7. [Framework Integration](#framework-integration)
8. [Binary Format Details](#binary-format-details)
9. [Performance Guide](#performance-guide)
10. [Troubleshooting](#troubleshooting)
11. [Migration Guide](#migration-guide)
12. [Complete Examples](#complete-examples)
13. [Quick Reference](#quick-reference)

## What is Peanut Configuration?

Peanut Configuration is TuskLang's high-performance hierarchical configuration system that provides:

- **85% faster loading** than text-based configs
- **Hierarchical inheritance** (CSS-like cascading)
- **Type-safe access** with Java generics
- **Auto-compilation** from human-readable formats
- **File watching** for hot reloading
- **Thread-safe** concurrent access

## Installation and Setup

### Prerequisites

- Java 8 or higher (Java 11+ recommended)
- Maven 3.6+ or Gradle 6.0+
- TuskLang Java SDK installed

### Installing the SDK

Add the TuskLang dependency to your `pom.xml`:

```xml
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java-sdk</artifactId>
    <version>2.0.0</version>
</dependency>
```

Or for Gradle (`build.gradle`):

```gradle
implementation 'org.tusklang:tusklang-java-sdk:2.0.0'
```

### Importing PeanutConfig

```java
import org.tusklang.PeanutConfig;
import java.util.Map;
import java.io.IOException;
```

## Quick Start

### Your First Peanut Configuration

1. Create a `peanu.peanuts` file:

```ini
[app]
name: "My Java Application"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
workers: 4
debug: true

[database]
driver: "postgresql"
host: "db.example.com"
port: 5432
pool_size: 10
```

2. Load the configuration:

```java
PeanutConfig config = new PeanutConfig();
Map<String, Object> settings = config.load();
```

3. Access values:

```java
String appName = (String) config.get("app.name", "Default App");
Integer port = (Integer) config.get("server.port", 3000);
Boolean debug = (Boolean) config.get("server.debug", false);
```

## Core Concepts

### File Types

- **`.peanuts`** - Human-readable configuration (INI-style)
- **`.tsk`** - TuskLang syntax (advanced features)
- **`.pnt`** - Compiled binary format (85% faster)

### Hierarchical Loading

Configuration files are loaded in a hierarchical order, with child directories overriding parent configurations:

```
/project/
├── peanu.peanuts          # Parent config
├── src/
│   └── peanu.peanuts      # Child config (overrides parent)
└── src/main/
    └── peanu.peanuts      # Grandchild config (overrides both)
```

### Type System

PeanutConfig automatically infers types from values:

```java
// String values
"localhost" → String

// Numeric values  
8080 → Integer
3.14 → Double

// Boolean values
true/false → Boolean

// Arrays
[1, 2, 3] → List<Integer>

// Nested objects
{host: "localhost", port: 8080} → Map<String, Object>
```

## API Reference

### PeanutConfig Class

#### Constructor

```java
// Default constructor
PeanutConfig config = new PeanutConfig();

// With options
PeanutConfig config = new PeanutConfig(true, true); // autoCompile, watch
```

#### Methods

##### load(directory)

Loads configuration hierarchy from the specified directory.

```java
public Map<String, Object> load(String directory) throws IOException
```

**Parameters:**
- `directory` - Starting directory for configuration search

**Returns:**
- Merged configuration as Map<String, Object>

**Example:**
```java
PeanutConfig config = new PeanutConfig();
Map<String, Object> settings = config.load("/path/to/project");
```

##### get(keyPath, defaultValue)

Gets a configuration value by dot-notation path.

```java
public Object get(String keyPath, Object defaultValue, String directory)
```

**Parameters:**
- `keyPath` - Dot-notation path (e.g., "server.port")
- `defaultValue` - Default value if key not found
- `directory` - Configuration directory

**Returns:**
- Configuration value or default

**Example:**
```java
Integer port = (Integer) config.get("server.port", 3000, "/path/to/project");
String host = (String) config.get("server.host", "localhost", "/path/to/project");
```

##### get(keyPath, type, defaultValue)

Type-safe configuration access with generics.

```java
public <T> T get(String keyPath, Class<T> type, T defaultValue, String directory)
```

**Example:**
```java
Integer port = config.get("server.port", Integer.class, 3000, "/path/to/project");
String host = config.get("server.host", String.class, "localhost", "/path/to/project");
Boolean debug = config.get("server.debug", Boolean.class, false, "/path/to/project");
```

##### compile(inputFile, outputFile)

Compiles text configuration to binary format.

```java
public void compileToBinary(Map<String, Object> config, Path outputPath) throws IOException
```

**Example:**
```java
PeanutConfig config = new PeanutConfig();
String content = Files.readString(Paths.get("config.peanuts"));
Map<String, Object> parsed = config.parseTextConfig(content);
config.compileToBinary(parsed, Paths.get("config.pnt"));
```

##### loadBinary(filePath)

Loads binary configuration file.

```java
public Map<String, Object> loadBinary(Path filePath) throws IOException
```

**Example:**
```java
PeanutConfig config = new PeanutConfig();
Map<String, Object> settings = config.loadBinary(Paths.get("config.pnt"));
```

##### findConfigHierarchy(startDir)

Finds all configuration files in hierarchy.

```java
public List<ConfigFile> findConfigHierarchy(String startDir) throws IOException
```

**Example:**
```java
PeanutConfig config = new PeanutConfig();
List<ConfigFile> hierarchy = config.findConfigHierarchy("/path/to/project");
for (ConfigFile file : hierarchy) {
    System.out.println(file.path + " (" + file.type + ")");
}
```

## Advanced Usage

### File Watching

Enable automatic reloading when configuration files change:

```java
PeanutConfig config = new PeanutConfig(true, true); // autoCompile, watch
Map<String, Object> settings = config.load("/path/to/project");

// Configuration will automatically reload when files change
// Use dispose() to clean up watchers
config.dispose();
```

### Custom Serialization

Handle custom types with type converters:

```java
// Custom type handling
public class DatabaseConfig {
    public String host;
    public int port;
    public String driver;
}

// Convert from Map to custom type
Map<String, Object> dbConfig = (Map<String, Object>) config.get("database", new HashMap<>());
DatabaseConfig db = new DatabaseConfig();
db.host = (String) dbConfig.get("host");
db.port = (Integer) dbConfig.get("port");
db.driver = (String) dbConfig.get("driver");
```

### Performance Optimization

Use caching and singleton patterns for optimal performance:

```java
public class ConfigManager {
    private static PeanutConfig instance;
    private static Map<String, Object> cachedConfig;
    
    public static synchronized Map<String, Object> getConfig() {
        if (instance == null) {
            instance = new PeanutConfig();
            try {
                cachedConfig = instance.load();
            } catch (IOException e) {
                throw new RuntimeException("Failed to load config", e);
            }
        }
        return cachedConfig;
    }
}
```

### Thread Safety

PeanutConfig is thread-safe for concurrent access:

```java
// Safe for multiple threads
PeanutConfig config = new PeanutConfig();
Map<String, Object> settings = config.load("/path/to/project");

// Multiple threads can safely access
Runnable task = () -> {
    String host = (String) config.get("server.host", "localhost", "/path/to/project");
    System.out.println("Host: " + host);
};

// Run in multiple threads
ExecutorService executor = Executors.newFixedThreadPool(4);
for (int i = 0; i < 10; i++) {
    executor.submit(task);
}
executor.shutdown();
```

## Java-Specific Features

### Spring Boot Integration

Automatic integration with Spring Boot applications:

```java
@Configuration
@EnableConfigurationProperties
public class PeanutConfiguration {
    
    @Bean
    public PeanutConfig peanutConfig() {
        return new PeanutConfig();
    }
    
    @Bean
    public Map<String, Object> applicationConfig(PeanutConfig peanutConfig) throws IOException {
        return peanutConfig.load();
    }
}

@Component
public class ServerConfig {
    
    @Autowired
    private Map<String, Object> config;
    
    public String getHost() {
        return (String) config.get("server.host");
    }
    
    public int getPort() {
        return (Integer) config.get("server.port");
    }
}
```

### Type Hints and Generics

Leverage Java's type system for type-safe configuration:

```java
public class TypedConfig {
    private final PeanutConfig config;
    
    public TypedConfig(PeanutConfig config) {
        this.config = config;
    }
    
    public <T> T get(String key, Class<T> type, T defaultValue) {
        return config.get(key, type, defaultValue, "/path/to/project");
    }
    
    // Convenience methods
    public String getString(String key, String defaultValue) {
        return get(key, String.class, defaultValue);
    }
    
    public Integer getInt(String key, Integer defaultValue) {
        return get(key, Integer.class, defaultValue);
    }
    
    public Boolean getBoolean(String key, Boolean defaultValue) {
        return get(key, Boolean.class, defaultValue);
    }
}
```

### Exception Handling

Proper exception handling for configuration errors:

```java
public class SafeConfigLoader {
    
    public static Map<String, Object> loadConfig(String directory) {
        try {
            PeanutConfig config = new PeanutConfig();
            return config.load(directory);
        } catch (IOException e) {
            throw new ConfigurationException("Failed to load configuration", e);
        }
    }
    
    public static <T> T getConfigValue(String key, Class<T> type, T defaultValue, String directory) {
        try {
            PeanutConfig config = new PeanutConfig();
            return config.get(key, type, defaultValue, directory);
        } catch (Exception e) {
            System.err.println("Warning: Failed to get config value '" + key + "', using default");
            return defaultValue;
        }
    }
}
```

## Framework Integration

### Spring Boot

Complete Spring Boot integration example:

```java
@SpringBootApplication
public class Application {
    
    public static void main(String[] args) {
        SpringApplication.run(Application.class, args);
    }
}

@Configuration
public class PeanutAutoConfiguration {
    
    @Bean
    @ConditionalOnMissingBean
    public PeanutConfig peanutConfig() {
        return new PeanutConfig(true, true);
    }
    
    @Bean
    @ConditionalOnMissingBean
    public ConfigurationProperties configurationProperties(PeanutConfig peanutConfig) throws IOException {
        Map<String, Object> config = peanutConfig.load();
        return new ConfigurationProperties(config);
    }
}

@Component
public class ConfigurationProperties {
    
    private final Map<String, Object> config;
    
    public ConfigurationProperties(Map<String, Object> config) {
        this.config = config;
    }
    
    public String getAppName() {
        return (String) config.get("app.name");
    }
    
    public ServerConfig getServer() {
        Map<String, Object> serverConfig = (Map<String, Object>) config.get("server");
        return new ServerConfig(serverConfig);
    }
    
    public static class ServerConfig {
        private final String host;
        private final int port;
        
        public ServerConfig(Map<String, Object> config) {
            this.host = (String) config.get("host");
            this.port = (Integer) config.get("port");
        }
        
        // Getters...
    }
}
```

### Micronaut

Micronaut integration example:

```java
@Factory
public class PeanutConfigurationFactory {
    
    @Singleton
    @Named("peanutConfig")
    public PeanutConfig peanutConfig() {
        return new PeanutConfig();
    }
    
    @Singleton
    @Named("applicationConfig")
    public Map<String, Object> applicationConfig(@Named("peanutConfig") PeanutConfig config) throws IOException {
        return config.load();
    }
}
```

## Binary Format Details

### File Structure

| Offset | Size | Description |
|--------|------|-------------|
| 0 | 4 | Magic: "PNUT" |
| 4 | 4 | Version (LE) |
| 8 | 8 | Timestamp (LE) |
| 16 | 8 | SHA256 checksum |
| 24 | N | MessagePack serialized data |

### Serialization Format

Java uses MessagePack for binary serialization:

```java
// Internal serialization
MessagePack msgpack = new MessagePack();
byte[] data = msgpack.write(configMap);

// Internal deserialization
Map<String, Object> config = msgpack.read(data, Map.class);
```

### File Header

```java
private static final byte[] MAGIC = "PNUT".getBytes(StandardCharsets.US_ASCII);
private static final int VERSION = 1;
private static final int HEADER_SIZE = 16;
private static final int CHECKSUM_SIZE = 8;
```

## Performance Guide

### Benchmarks

Performance comparison between text and binary formats:

```java
public class PerformanceBenchmark {
    
    public static void runBenchmark() {
        PeanutConfig config = new PeanutConfig();
        String testContent = "[server]\n" +
            "host: \"localhost\"\n" +
            "port: 8080\n" +
            "workers: 4\n" +
            "debug: true\n\n" +
            "[database]\n" +
            "driver: \"postgresql\"\n" +
            "host: \"db.example.com\"\n" +
            "port: 5432\n" +
            "pool_size: 10\n\n" +
            "[cache]\n" +
            "enabled: true\n" +
            "ttl: 3600\n" +
            "backend: \"redis\"";
        
        System.out.println("🥜 Peanut Configuration Performance Test\n");
        
        // Test text parsing
        long startText = System.nanoTime();
        for (int i = 0; i < 1000; i++) {
            config.parseTextConfig(testContent);
        }
        long textTime = System.nanoTime() - startText;
        System.out.println("Text parsing (1000 iterations): " + (textTime / 1_000_000) + "ms");
        
        // Test binary loading
        try {
            Map<String, Object> parsed = config.parseTextConfig(testContent);
            byte[] binaryData = config.msgpack.write(parsed);
            
            long startBinary = System.nanoTime();
            for (int i = 0; i < 1000; i++) {
                config.msgpack.read(binaryData, Map.class);
            }
            long binaryTime = System.nanoTime() - startBinary;
            System.out.println("Binary loading (1000 iterations): " + (binaryTime / 1_000_000) + "ms");
            
            double improvement = ((double)(textTime - binaryTime) / textTime) * 100;
            System.out.printf("\n✨ Binary format is %.0f%% faster than text parsing!\n", improvement);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
```

### Best Practices

1. **Always use .pnt in production** for maximum performance
2. **Cache configuration objects** to avoid repeated loading
3. **Use file watching wisely** - disable in production if not needed
4. **Implement singleton pattern** for configuration access
5. **Use type-safe access** with generics
6. **Handle exceptions gracefully** with fallback values

## Troubleshooting

### Common Issues

#### File Not Found

**Problem:** Configuration file not found in expected location

**Solution:**
```java
// Check file hierarchy
PeanutConfig config = new PeanutConfig();
List<ConfigFile> hierarchy = config.findConfigHierarchy("/path/to/project");
for (ConfigFile file : hierarchy) {
    System.out.println("Found: " + file.path);
}

// Use absolute paths
Map<String, Object> settings = config.load(Paths.get("/absolute/path").toString());
```

#### Checksum Mismatch

**Problem:** Binary file corruption detected

**Solution:**
```java
try {
    Map<String, Object> config = peanutConfig.loadBinary(Paths.get("config.pnt"));
} catch (IOException e) {
    // Recompile from source
    String content = Files.readString(Paths.get("config.peanuts"));
    Map<String, Object> parsed = peanutConfig.parseTextConfig(content);
    peanutConfig.compileToBinary(parsed, Paths.get("config.pnt"));
}
```

#### Performance Issues

**Problem:** Slow configuration loading

**Solution:**
```java
// Enable binary compilation
PeanutConfig config = new PeanutConfig(true, false); // autoCompile, no watch

// Use caching
private static final Map<String, Object> CONFIG_CACHE = new ConcurrentHashMap<>();

public static Map<String, Object> getConfig() {
    return CONFIG_CACHE.computeIfAbsent("config", k -> {
        try {
            PeanutConfig config = new PeanutConfig();
            return config.load();
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    });
}
```

### Debug Mode

Enable debug logging for troubleshooting:

```java
// Enable debug output
System.setProperty("org.tusklang.debug", "true");

PeanutConfig config = new PeanutConfig();
Map<String, Object> settings = config.load("/path/to/project");
```

## Migration Guide

### From JSON

```java
// Old JSON approach
ObjectMapper mapper = new ObjectMapper();
Map<String, Object> config = mapper.readValue(new File("config.json"), Map.class);

// New Peanut approach
PeanutConfig peanutConfig = new PeanutConfig();
Map<String, Object> config = peanutConfig.load();

// Convert JSON to Peanuts format
// config.json → config.peanuts
```

### From YAML

```java
// Old YAML approach
ObjectMapper mapper = new ObjectMapper(new YAMLFactory());
Map<String, Object> config = mapper.readValue(new File("config.yml"), Map.class);

// New Peanut approach
PeanutConfig peanutConfig = new PeanutConfig();
Map<String, Object> config = peanutConfig.load();

// Convert YAML to Peanuts format
// config.yml → config.peanuts
```

### From .env

```java
// Old .env approach
Properties props = new Properties();
props.load(new FileInputStream(".env"));

// New Peanut approach
PeanutConfig peanutConfig = new PeanutConfig();
Map<String, Object> config = peanutConfig.load();

// Convert .env to Peanuts format
// .env → config.peanuts
```

## Complete Examples

### Web Application Configuration

**File Structure:**
```
my-web-app/
├── peanu.peanuts
├── src/
│   ├── main/
│   │   ├── java/
│   │   │   └── com/myapp/
│   │   │       ├── Application.java
│   │   │       ├── Config.java
│   │   │       └── ServerConfig.java
│   │   └── resources/
│   │       └── peanu.peanuts
│   └── test/
│       └── peanu.peanuts
└── pom.xml
```

**peanu.peanuts:**
```ini
[app]
name: "My Web Application"
version: "1.0.0"
environment: "development"

[server]
host: "0.0.0.0"
port: 8080
workers: 4
timeout: 30

[database]
driver: "postgresql"
host: "localhost"
port: 5432
name: "myapp_dev"
user: "postgres"
password: "secret"

[cache]
enabled: true
ttl: 3600
backend: "redis"
```

**Application.java:**
```java
@SpringBootApplication
public class Application {
    
    public static void main(String[] args) {
        SpringApplication.run(Application.class, args);
    }
    
    @Bean
    public PeanutConfig peanutConfig() {
        return new PeanutConfig(true, true);
    }
    
    @Bean
    public Map<String, Object> applicationConfig(PeanutConfig peanutConfig) throws IOException {
        return peanutConfig.load();
    }
}
```

**Config.java:**
```java
@Component
public class Config {
    
    private final Map<String, Object> config;
    
    public Config(Map<String, Object> config) {
        this.config = config;
    }
    
    public String getAppName() {
        return (String) config.get("app.name");
    }
    
    public String getEnvironment() {
        return (String) config.get("app.environment");
    }
    
    public ServerConfig getServer() {
        Map<String, Object> serverConfig = (Map<String, Object>) config.get("server");
        return new ServerConfig(serverConfig);
    }
    
    public DatabaseConfig getDatabase() {
        Map<String, Object> dbConfig = (Map<String, Object>) config.get("database");
        return new DatabaseConfig(dbConfig);
    }
}
```

### Microservice Configuration

**File Structure:**
```
user-service/
├── peanu.peanuts
├── src/
│   └── main/
│       └── java/
│           └── com/userservice/
│               ├── UserServiceApplication.java
│               ├── UserServiceConfig.java
│               └── DatabaseConfig.java
└── pom.xml
```

**peanu.peanuts:**
```ini
[service]
name: "user-service"
version: "2.1.0"
port: 8081

[database]
host: "user-db.example.com"
port: 5432
name: "users"
pool_size: 20
connection_timeout: 30

[redis]
host: "redis.example.com"
port: 6379
database: 1
ttl: 1800

[logging]
level: "INFO"
format: "json"
```

**UserServiceApplication.java:**
```java
@SpringBootApplication
public class UserServiceApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(UserServiceApplication.class, args);
    }
}
```

**UserServiceConfig.java:**
```java
@Configuration
@EnableConfigurationProperties
public class UserServiceConfig {
    
    @Bean
    public PeanutConfig peanutConfig() {
        return new PeanutConfig();
    }
    
    @Bean
    public UserServiceConfigProperties configProperties(PeanutConfig peanutConfig) throws IOException {
        Map<String, Object> config = peanutConfig.load();
        return new UserServiceConfigProperties(config);
    }
}

@Component
public class UserServiceConfigProperties {
    
    private final String serviceName;
    private final int port;
    private final DatabaseConfig database;
    private final RedisConfig redis;
    
    public UserServiceConfigProperties(Map<String, Object> config) {
        this.serviceName = (String) config.get("service.name");
        this.port = (Integer) config.get("service.port");
        
        Map<String, Object> dbConfig = (Map<String, Object>) config.get("database");
        this.database = new DatabaseConfig(dbConfig);
        
        Map<String, Object> redisConfig = (Map<String, Object>) config.get("redis");
        this.redis = new RedisConfig(redisConfig);
    }
    
    // Getters...
}
```

### CLI Tool Configuration

**File Structure:**
```
my-cli-tool/
├── peanu.peanuts
├── src/
│   └── main/
│       └── java/
│           └── com/mytool/
│               ├── Main.java
│               ├── Config.java
│               └── commands/
│                   ├── BuildCommand.java
│                   └── DeployCommand.java
└── pom.xml
```

**peanu.peanuts:**
```ini
[tool]
name: "My CLI Tool"
version: "1.0.0"

[build]
output_dir: "dist"
source_dir: "src"
target: "java8"

[deploy]
environments:
  - "development"
  - "staging"
  - "production"

[api]
base_url: "https://api.example.com"
timeout: 30
retries: 3
```

**Main.java:**
```java
public class Main {
    
    private static final Config config = new Config();
    
    public static void main(String[] args) {
        if (args.length == 0) {
            System.out.println("Usage: mytool <command> [options]");
            System.out.println("Commands: build, deploy");
            return;
        }
        
        String command = args[0];
        
        switch (command) {
            case "build":
                new BuildCommand(config).execute(args);
                break;
            case "deploy":
                new DeployCommand(config).execute(args);
                break;
            default:
                System.err.println("Unknown command: " + command);
                System.exit(1);
        }
    }
}
```

**Config.java:**
```java
public class Config {
    
    private final PeanutConfig peanutConfig;
    private final Map<String, Object> settings;
    
    public Config() {
        this.peanutConfig = new PeanutConfig();
        try {
            this.settings = peanutConfig.load();
        } catch (IOException e) {
            throw new RuntimeException("Failed to load configuration", e);
        }
    }
    
    public String getToolName() {
        return (String) settings.get("tool.name");
    }
    
    public String getOutputDir() {
        return (String) settings.get("build.output_dir");
    }
    
    public String getApiBaseUrl() {
        return (String) settings.get("api.base_url");
    }
    
    public int getApiTimeout() {
        return (Integer) settings.get("api.timeout");
    }
}
```

## Quick Reference

### Common Operations

```java
// Load config
PeanutConfig config = new PeanutConfig();
Map<String, Object> settings = config.load();

// Get value
String host = (String) config.get("server.host", "localhost", "/path/to/project");

// Compile to binary
String content = Files.readString(Paths.get("config.peanuts"));
Map<String, Object> parsed = config.parseTextConfig(content);
config.compileToBinary(parsed, Paths.get("config.pnt"));

// Load binary
Map<String, Object> settings = config.loadBinary(Paths.get("config.pnt"));

// Watch for changes
PeanutConfig config = new PeanutConfig(true, true); // autoCompile, watch
Map<String, Object> settings = config.load("/path/to/project");
// ... use config ...
config.dispose(); // Clean up watchers
```

### Type-Safe Access

```java
// With generics
Integer port = config.get("server.port", Integer.class, 3000, "/path/to/project");
String host = config.get("server.host", String.class, "localhost", "/path/to/project");
Boolean debug = config.get("server.debug", Boolean.class, false, "/path/to/project");

// Custom type conversion
Map<String, Object> dbConfig = (Map<String, Object>) config.get("database", new HashMap<>());
DatabaseConfig db = new DatabaseConfig();
db.setHost((String) dbConfig.get("host"));
db.setPort((Integer) dbConfig.get("port"));
```

### Performance Tips

```java
// Singleton pattern
public class ConfigManager {
    private static final PeanutConfig INSTANCE = new PeanutConfig();
    private static final Map<String, Object> CACHED_CONFIG;
    
    static {
        try {
            CACHED_CONFIG = INSTANCE.load();
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }
    
    public static Map<String, Object> getConfig() {
        return CACHED_CONFIG;
    }
}

// Thread-safe access
private static final ConcurrentHashMap<String, Object> CONFIG_CACHE = new ConcurrentHashMap<>();

public static Object getConfigValue(String key, Object defaultValue) {
    return CONFIG_CACHE.computeIfAbsent(key, k -> {
        try {
            PeanutConfig config = new PeanutConfig();
            return config.get(key, defaultValue, "/path/to/project");
        } catch (Exception e) {
            return defaultValue;
        }
    });
}
```

---

This guide provides everything you need to use Peanut Configuration effectively in your Java applications. The system offers significant performance improvements while maintaining the flexibility and ease of use of text-based configuration files. 
# 🐘 TuskLang Java SDK - Enhanced Edition

Full-featured Java implementation of TuskLang with @ operators, database queries, and flexible syntax support.

## ✨ Enhanced Features

- **All @ operators** - @env, @cache, @metrics, @learn, @optimize, @query, @date
- **Database queries** - Query your database directly from config files!
- **Flexible syntax** - Support for `[]`, `{}`, `<>` grouping styles
- **Global variables** - Use `$variable` for global scope
- **Cross-file communication** - Reference values from other .tsk files
- **Automatic peanut.tsk loading** - Universal configuration support
- **JPA/Hibernate integration** - Enterprise-grade database access
- **Conditional expressions** - Ternary operators in configs
- **Range syntax** - Express ranges like `8000-9000`

## 🚀 Quick Start

### 1. Add to your project

```xml
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java</artifactId>
    <version>2.0.0</version>
</dependency>
```

### 2. Basic Usage

```java
import org.tusklang.TuskLangEnhanced;
import java.util.Map;

// Create enhanced parser
TuskLangEnhanced parser = new TuskLangEnhanced();

// Parse with @ operators
String config = """
    app_name: "My App"
    version: "2.0.0"
    debug: @env("DEBUG", "false")
    port: @env("PORT", "8080")
    
    database {
        host: @env("DB_HOST", "localhost")
        users: @query("User").where("active", true).count()
        cache_ttl: @cache("5m", expensive_operation)
    }
    
    features {
        ai_suggestions: @learn("ai_enabled", true)
        worker_count: @optimize("workers", 4)
    }
    """;

Map<String, Object> result = parser.parse(config);
```

### 3. Use flexible syntax

```java
// All these styles work!
String config1 = """
    # Traditional TuskLang style
    server {
        host: "localhost"
        port: 8080
    }
    """;

String config2 = """
    # TOML-style sections
    [server]
    host = "localhost"
    port = 8080
    """;

String config3 = """
    # XML-style sections
    <server>
    host: "localhost"
    port: 8080
    """;
```

## 📝 Enhanced TuskLang Syntax

### @ Operators

```tsk
# Environment variables with defaults
api_key: @env("API_KEY", "default-key")

# Date formatting
created_at: @date("Y-m-d H:i:s")

# Intelligent caching
expensive_data: @cache("5m", @query("Analytics").aggregate())

# Metrics tracking
request_count: @metrics("api_requests", 1)

# Machine learning optimization
cache_size: @learn("optimal_cache", 1000)
batch_size: @optimize("batch_size", 100)

# Database queries - THE KILLER FEATURE!
active_users: @query("User").where("status", "active").count()
revenue: @query("Order").where("date", @date("Y-m-d")).sum("amount")
```

### Global Variables

```tsk
# Define global variables with $
$base_url: "https://api.example.com"
$api_version: "v2"

# Use them anywhere
endpoint: "$base_url/$api_version/users"
```

### Cross-file Communication

```tsk
# Reference values from other files
database_host: @config.tsk.get("database.host")
shared_secret: @secrets.tsk.get("api.key")
```

### Conditional Expressions

```tsk
# Ternary operator support
environment: @env("ENV", "dev")
debug_mode: environment == "dev" ? true : false
log_level: debug_mode ? "debug" : "info"
```

### Range Syntax

```tsk
# Express ranges easily
allowed_ports: 8000-9000
valid_ids: 1-100
```

## 🛠️ Enhanced API

### TuskLangEnhanced

```java
// Create enhanced parser
TuskLangEnhanced parser = new TuskLangEnhanced();

// Parse with all features
Map<String, Object> config = parser.parse(tskString);
Map<String, Object> config = parser.parseFile("config.tsk");

// Set global variables
parser.setGlobalVariable("environment", "production");
parser.setGlobalVariable("region", "us-east-1");

// Get/set values with dot notation
Object value = parser.get("database.host");
parser.set("server.port", 8080);

// Get full configuration
Map<String, Object> fullConfig = parser.getConfig();
```

## 🖥️ Enhanced CLI

### New Commands

```bash
# Build the enhanced version
mvn clean package

# Parse with enhanced features
java -jar tusklang-java-2.0.0.jar parse config.tsk --enhanced --pretty

# Get specific values
java -jar tusklang-java-2.0.0.jar get config.tsk database.host --enhanced

# Set values
java -jar tusklang-java-2.0.0.jar set config.tsk server.port 9000 --enhanced

# Convert between formats
java -jar tusklang-java-2.0.0.jar convert config.json config.tsk

# Define global variables
java -jar tusklang-java-2.0.0.jar parse config.tsk -Denv=prod -Dregion=us-west-2

# Show version info
java -jar tusklang-java-2.0.0.jar version
```

## 🗄️ Database Configuration

### 1. Create persistence.xml

```xml
<!-- src/main/resources/META-INF/persistence.xml -->
<persistence xmlns="http://xmlns.jcp.org/xml/ns/persistence" version="2.2">
    <persistence-unit name="tusklang">
        <properties>
            <property name="javax.persistence.jdbc.driver" value="org.postgresql.Driver"/>
            <property name="javax.persistence.jdbc.url" value="jdbc:postgresql://localhost:5432/myapp"/>
            <property name="javax.persistence.jdbc.user" value="user"/>
            <property name="javax.persistence.jdbc.password" value="password"/>
            <property name="hibernate.dialect" value="org.hibernate.dialect.PostgreSQLDialect"/>
        </properties>
    </persistence-unit>
</persistence>
```

### 2. Use @query in configs

```tsk
# Query your database directly!
stats {
    total_users: @query("User").count()
    active_users: @query("User").where("active", true).count()
    revenue_today: @query("Order").where("date", @date("Y-m-d")).sum("amount")
    top_products: @query("Product").orderBy("sales", "desc").limit(10)
}

# Dynamic configuration based on database
scaling {
    # Auto-scale based on load
    instances: @query("Metric").where("type", "cpu").avg("value") > 80 ? 10 : 5
    
    # Adjust cache based on memory usage
    cache_size: @optimize("cache", @query("Metric").where("type", "memory").last())
}
```

## 🥜 peanut.tsk Integration

The enhanced parser automatically loads `peanut.tsk` from these locations:
1. Current directory
2. Parent directory (`../peanut.tsk`)
3. Grandparent (`../../peanut.tsk`)
4. User config (`~/.config/tusklang/peanut.tsk`)
5. System config (`/etc/tusklang/peanut.tsk`)

Example peanut.tsk:
```tsk
# Global configuration loaded automatically
$app_name: "TuskLang"
$environment: @env("ENV", "development")

defaults {
    cache_ttl: "5m"
    log_level: "info"
    timezone: "UTC"
}
```

## 🧪 Testing Enhanced Features

```java
@Test
public void testEnhancedOperators() {
    TuskLangEnhanced parser = new TuskLangEnhanced();
    parser.setGlobalVariable("env", "test");
    
    String config = """
        environment: $env
        debug: @env("DEBUG", "false")
        timestamp: @date("Y-m-d")
        cached_value: @cache("1m", expensive_call())
        """;
    
    Map<String, Object> result = parser.parse(config);
    assertEquals("test", result.get("environment"));
    assertNotNull(result.get("timestamp"));
}
```

## 📦 Building

```bash
# Clean build with all dependencies
mvn clean package

# Run tests including enhanced features
mvn test

# Install to local repository
mvn install

# Deploy to Maven Central
mvn deploy
```

## 🚀 Real-World Example

```tsk
# production.tsk - Real production config
app_name: "TuskLang API"
version: "2.0.0"
environment: @env("ENVIRONMENT", "production")

# Database with connection pooling
database {
    driver: @env("DB_DRIVER", "postgresql")
    url: @env("DATABASE_URL", "jdbc:postgresql://localhost:5432/prod")
    pool_size: @optimize("db_pool", 20)
    
    # Monitor database health
    health_check: @query("HealthCheck").latest()
    active_connections: @metrics("db_connections")
}

# Auto-scaling configuration
scaling {
    min_instances: 2
    max_instances: 50
    
    # Scale based on real metrics
    target_cpu: @learn("optimal_cpu", 70)
    current_load: @query("Metrics").where("type", "cpu").avg("value")
    desired_instances: current_load > target_cpu ? max_instances : min_instances
}

# Feature flags with learning
features {
    new_ui: @learn("new_ui_enabled", false)
    dark_mode: @cache("1h", @query("UserPreferences").where("setting", "theme").mostCommon())
    ai_suggestions: @optimize("ai_enabled", true)
}

# API rate limiting
rate_limits {
    global: @env("RATE_LIMIT_GLOBAL", "10000")
    per_user: @learn("rate_limit_per_user", 100)
    
    # Dynamic limits based on time
    burst_limit: @date("H") >= 9 && @date("H") <= 17 ? 200 : 50
}
```

## 🔧 Troubleshooting

### Database queries not working?
- Ensure persistence.xml is configured
- Check database drivers are in classpath
- Verify entity classes are annotated with @Entity

### @ operators returning raw strings?
- Make sure you're using TuskLangEnhanced, not TuskLangParser
- Use --enhanced flag in CLI
- Check operator syntax is correct

### peanut.tsk not loading?
- Check file exists in one of the search paths
- Verify no syntax errors in peanut.tsk
- Enable verbose logging to see search process

## 🎯 Why Enhanced?

The enhanced parser brings TuskLang's **killer feature** to Java: **configuration files that can query your database!** This isn't just another config format - it's intelligent configuration that adapts to your application's state.

Imagine configs that:
- Auto-scale based on real load
- Adjust cache sizes based on usage patterns
- Enable features based on user preferences
- Update limits based on time of day
- Learn optimal values over time

This is the future of configuration! 🚀

## 📄 License

MIT License - see LICENSE file for details.

## 🐘 About TuskLang

TuskLang brings intelligence to configuration files. No more static configs - make them dynamic, adaptive, and database-aware!

For more information, visit [tuskt.sk](https://tuskt.sk).
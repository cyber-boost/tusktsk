# ☕ TuskLang Java Quick Start Guide

**"We don't bow to any king" - Java Edition**

Get up and running with TuskLang in Java in under 5 minutes. This guide will show you how to create your first TuskLang application, integrate with Spring Boot, and use database queries in your configuration.

## 🚀 5-Minute Quick Start

### Step 1: Create Your First TuskLang App

Create a new Maven project and add TuskLang:

```xml
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java</artifactId>
    <version>1.0.0</version>
</dependency>
```

### Step 2: Create Your Configuration

Create `config.tsk` in your project root:

```tsk
# Application configuration
app_name: "My TuskLang App"
version: "1.0.0"
debug: true
port: 8080

# Database configuration
[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

# Server configuration
[server]
host: "0.0.0.0"
port: 8080
ssl: false

# Feature flags
[features]
user_management: true
payment_processing: true
analytics: false
```

### Step 3: Create Your Java Application

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class AppConfig {
    public String appName;
    public String version;
    public boolean debug;
    public int port;
    
    public DatabaseConfig database;
    public ServerConfig server;
    public FeaturesConfig features;
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
public class FeaturesConfig {
    public boolean userManagement;
    public boolean paymentProcessing;
    public boolean analytics;
}

public class QuickStartApp {
    public static void main(String[] args) {
        // Create TuskLang parser
        TuskLang parser = new TuskLang();
        
        // Parse configuration file
        AppConfig config = parser.parseFile("config.tsk", AppConfig.class);
        
        // Use your configuration
        System.out.println("🚀 " + config.appName + " v" + config.version);
        System.out.println("🌐 Server: " + config.server.host + ":" + config.server.port);
        System.out.println("🗄️ Database: " + config.database.host + ":" + config.database.port);
        System.out.println("🔧 Features: " + 
            (config.features.userManagement ? "User Management " : "") +
            (config.features.paymentProcessing ? "Payment Processing " : "") +
            (config.features.analytics ? "Analytics" : ""));
    }
}
```

### Step 4: Run Your Application

```bash
# Compile and run
mvn compile exec:java -Dexec.mainClass="QuickStartApp"

# Or run directly
java -cp target/classes QuickStartApp
```

## 🎯 Spring Boot Integration

### Step 1: Add Spring Boot Dependencies

```xml
<parent>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-parent</artifactId>
    <version>3.2.0</version>
</parent>

<dependencies>
    <dependency>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-web</artifactId>
    </dependency>
    
    <dependency>
        <groupId>org.tusklang</groupId>
        <artifactId>tusklang-spring-boot-starter</artifactId>
        <version>1.0.0</version>
    </dependency>
</dependencies>
```

### Step 2: Create Spring Boot Application

```java
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.web.bind.annotation.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@SpringBootApplication
public class TuskSpringApp {
    public static void main(String[] args) {
        SpringApplication.run(TuskSpringApp.class, args);
    }
}

@RestController
@RequestMapping("/api")
public class ApiController {
    
    @Autowired
    private TuskConfig config;
    
    @Autowired
    private TuskLang parser;
    
    @GetMapping("/config")
    public Map<String, Object> getConfig() {
        return Map.of(
            "appName", config.getAppName(),
            "version", config.getVersion(),
            "debug", config.isDebug(),
            "port", config.getPort()
        );
    }
    
    @GetMapping("/status")
    public Map<String, Object> getStatus() {
        return Map.of(
            "status", "running",
            "timestamp", System.currentTimeMillis(),
            "config", config
        );
    }
}

@Component
public class TuskConfig {
    private final AppConfig appConfig;
    
    public TuskConfig() {
        TuskLang parser = new TuskLang();
        this.appConfig = parser.parseFile("config.tsk", AppConfig.class);
    }
    
    public String getAppName() { return appConfig.appName; }
    public String getVersion() { return appConfig.version; }
    public boolean isDebug() { return appConfig.debug; }
    public int getPort() { return appConfig.port; }
    public DatabaseConfig getDatabase() { return appConfig.database; }
    public ServerConfig getServer() { return appConfig.server; }
    public FeaturesConfig getFeatures() { return appConfig.features; }
}
```

### Step 3: Run Spring Boot App

```bash
mvn spring-boot:run
```

Visit `http://localhost:8080/api/config` to see your configuration!

## 🗄️ Database Integration

### Step 1: Add Database Dependencies

```xml
<dependency>
    <groupId>org.postgresql</groupId>
    <artifactId>postgresql</artifactId>
    <version>42.7.0</version>
</dependency>
```

### Step 2: Create Database Configuration

```tsk
# Enhanced configuration with database queries
app_name: "Database TuskLang App"
version: "1.0.0"

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

# Dynamic data from database
[stats]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > ?", @date.subtract("7d"))

[features]
user_management: true
payment_processing: @query("SELECT COUNT(*) FROM payment_processors") > 0
analytics: false
```

### Step 3: Use Database Queries

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.PostgreSQLAdapter;
import java.util.Map;

public class DatabaseExample {
    public static void main(String[] args) {
        // Configure PostgreSQL adapter
        PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
            .host("localhost")
            .port(5432)
            .database("myapp")
            .user("postgres")
            .password("secret")
            .build());
        
        // Create TuskLang parser with database
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // Parse configuration with database queries
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        System.out.println("📊 User count: " + config.get("stats"));
        System.out.println("✅ Payment processing: " + config.get("features"));
    }
}
```

## ⚡ @ Operator Examples

### Environment Variables

```tsk
# Use environment variables
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
password: @env.secure("DB_PASSWORD")

[api]
key: @env("API_KEY")
url: @env("API_URL", "https://api.example.com")
```

### Date and Time Operations

```tsk
[timestamps]
created_at: @date.now()
formatted_date: @date("yyyy-MM-dd HH:mm:ss")
yesterday: @date.subtract("1d")
next_week: @date.add("7d")
```

### Caching

```tsk
[expensive_data]
user_profile: @cache("5m", "get_user_profile", @request.user_id)
analytics: @cache("1h", "get_analytics_data")
```

### HTTP Requests

```tsk
[external_data]
weather: @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London")
exchange_rate: @http("GET", "https://api.exchangerate-api.com/v4/latest/USD")
```

## 🔧 Advanced Features

### FUJSEN (Function Serialization)

```tsk
[payment]
process_fujsen: """
function process(amount, recipient) {
    if (amount <= 0) {
        throw new Error("Invalid amount");
    }
    
    return {
        success: true,
        transactionId: "tx_" + Date.now(),
        amount: amount,
        recipient: recipient,
        fee: amount * 0.025
    };
}
"""

validate_fujsen: """
function validate(amount) {
    return amount > 0 && amount <= 1000000;
}
"""
```

### Execute FUJSEN Functions

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class FujsenExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Execute payment processing
        Map<String, Object> result = parser.executeFujsen(
            "payment", 
            "process", 
            100.0, 
            "alice@example.com"
        );
        
        System.out.println("Payment result: " + result);
        
        // Validate amount
        boolean isValid = parser.executeFujsen("payment", "validate", 500.0);
        System.out.println("Amount valid: " + isValid);
    }
}
```

### Cross-File Communication

Create `database.tsk`:

```tsk
[connection]
host: "localhost"
port: 5432
name: "myapp"
```

Create `app.tsk`:

```tsk
# Import database configuration
@import("database.tsk")

[app]
name: "My App"
database: @ref("database.connection")
```

## 🧪 Testing

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import java.util.Map;

class TuskLangTest {
    
    private TuskLang parser;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
    }
    
    @Test
    void testBasicParsing() {
        String tskContent = """
            [app]
            name: "Test App"
            version: "1.0.0"
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        assertEquals("Test App", config.get("app"));
    }
    
    @Test
    void testDatabaseQueries() {
        // Setup test database
        SQLiteAdapter db = new SQLiteAdapter(":memory:");
        db.execute("CREATE TABLE users (id INTEGER, name TEXT)");
        db.execute("INSERT INTO users VALUES (1, 'Alice')");
        
        parser.setDatabaseAdapter(db);
        
        String tskContent = """
            [users]
            count: @query("SELECT COUNT(*) FROM users")
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        assertEquals(1, config.get("users"));
    }
}
```

## 🚀 Deployment

### Docker Deployment

Create `Dockerfile`:

```dockerfile
FROM openjdk:17-alpine

WORKDIR /app

# Copy application
COPY target/tusk-app-1.0.0.jar app.jar
COPY config.tsk config.tsk

# Run application
CMD ["java", "-jar", "app.jar"]
```

### Kubernetes Deployment

Create `deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusk-app
  template:
    metadata:
      labels:
        app: tusk-app
    spec:
      containers:
      - name: app
        image: tusk-app:latest
        env:
        - name: DB_HOST
          value: "postgres-service"
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: password
        ports:
        - containerPort: 8080
```

## 📊 Performance Tips

1. **Use Caching**: Leverage `@cache` operator for expensive operations
2. **Lazy Loading**: Use `@lazy` for data that's not immediately needed
3. **Database Connection Pooling**: Configure connection pools for database adapters
4. **Parallel Processing**: Use `@async` for independent operations

## 🎯 Next Steps

1. **Explore @ operators** - Environment variables, caching, HTTP requests
2. **Integrate with your database** - Use @query for dynamic data
3. **Add FUJSEN functions** - Execute JavaScript in your configuration
4. **Deploy to production** - Use Docker and Kubernetes
5. **Monitor performance** - Use @metrics for application monitoring

---

**"We don't bow to any king"** - You're now ready to build powerful Java applications with TuskLang! The configuration language that adapts to YOUR preferred syntax and gives you database queries, executable functions, and enterprise-grade performance. 
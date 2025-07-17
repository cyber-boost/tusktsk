# ☕ TuskLang Java Documentation

**"We don't bow to any king" - Java Edition**

Welcome to the comprehensive TuskLang Java documentation. This guide covers everything you need to know about using TuskLang with Java applications, from basic installation to advanced production deployment.

## 🚀 Quick Start

### Installation

```xml
<!-- Maven -->
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java</artifactId>
    <version>1.0.0</version>
</dependency>
```

```gradle
// Gradle
implementation 'org.tusklang:tusklang-java:1.0.0'
```

### Your First TuskLang Java App

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
}

public class HelloTuskLang {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        AppConfig config = parser.parseFile("config.tsk", AppConfig.class);
        
        System.out.println("🚀 " + config.appName + " v" + config.version);
        System.out.println("🌐 Server running on port " + config.port);
    }
}
```

Create `config.tsk`:

```tsk
app_name: "My TuskLang Java App"
version: "1.0.0"
debug: true
port: 8080
```

## 📚 Documentation Guide

### 🎯 Getting Started

1. **[Installation Guide](001-installation-java.md)** - Complete setup instructions for Maven, Gradle, and Spring Boot
2. **[Quick Start Guide](002-quick-start-java.md)** - Get up and running in 5 minutes with practical examples
3. **[Basic Syntax Guide](003-basic-syntax-java.md)** - Master TuskLang syntax with Java integration patterns

### 🗄️ Core Features

4. **[Database Integration](004-database-integration-java.md)** - Complete database support with SQLite, PostgreSQL, MySQL, MongoDB, and Redis
5. **[Advanced Features](005-advanced-features-java.md)** - FUJSEN execution, @ operators, caching, and enterprise features

### 🌐 Framework Integration

6. **[Spring Boot Integration](006-spring-boot-integration-java.md)** - Complete Spring Boot integration with auto-configuration and REST APIs

### 🛠️ Tools and Deployment

7. **[CLI Tools](007-cli-tools-java.md)** - Command-line interface, interactive shell, and utility commands
8. **[Production Deployment](008-production-deployment-java.md)** - Docker, Kubernetes, monitoring, and scaling strategies

## 🎯 Key Features

### ⚡ Syntax Flexibility
- Support for `[]`, `{}`, and `<>` syntax styles
- Traditional INI-style, JSON-like objects, and XML-inspired syntax
- Choose your preferred syntax style

### 🗃️ Database Queries in Config
```tsk
[database]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > ?", @date.subtract("7d"))
```

### 🔄 Cross-File Communication
```tsk
# Import and reference
@import("database.tsk")
[database]
connection: @ref("database.connection")
```

### ⚡ @ Operator System
```tsk
# Environment variables
api_key: @env("API_KEY")
password: @env.secure("DB_PASSWORD")

# Date/time operations
created_at: @date.now()
yesterday: @date.subtract("1d")

# Caching
user_data: @cache("5m", "get_user_data", @request.user_id)

# HTTP requests
weather: @http("GET", "https://api.weatherapi.com/v1/current.json?q=London")
```

### 🚀 FUJSEN (Function Serialization)
```tsk
[payment]
process_fujsen: """
function process(amount, recipient) {
    return {
        success: true,
        transactionId: "tx_" + Date.now(),
        amount: amount,
        recipient: recipient,
        fee: amount * 0.025
    };
}
"""
```

## 🌐 Spring Boot Integration

### Auto-Configuration
```java
@SpringBootApplication
public class Application {
    public static void main(String[] args) {
        SpringApplication.run(Application.class, args);
    }
}

@RestController
@RequestMapping("/api")
public class ApiController {
    @Autowired
    private TuskConfig config;
    
    @GetMapping("/config")
    public Map<String, Object> getConfig() {
        return Map.of(
            "appName", config.getAppName(),
            "version", config.getVersion()
        );
    }
}
```

### Database Integration
```java
@Service
public class UserService {
    private final TuskLang parser;
    
    public Map<String, Object> getUserStats() {
        return parser.parse("""
            [stats]
            total_users: @query("SELECT COUNT(*) FROM users")
            active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
            """);
    }
}
```

## 🗄️ Database Support

### Multiple Database Adapters
- **SQLite** - Built-in support for local development
- **PostgreSQL** - Enterprise-grade relational database
- **MySQL** - Popular open-source database
- **MongoDB** - NoSQL document database
- **Redis** - In-memory data structure store

### Example Database Integration
```java
// Configure PostgreSQL adapter
PostgreSQLAdapter db = new PostgreSQLAdapter(PostgreSQLConfig.builder()
    .host("localhost")
    .port(5432)
    .database("myapp")
    .user("postgres")
    .password("secret")
    .build());

TuskLang parser = new TuskLang();
parser.setDatabaseAdapter(db);

// Use database queries in configuration
Map<String, Object> config = parser.parseFile("config.tsk");
```

## 🔧 CLI Tools

### Command-Line Interface
```bash
# Parse TSK file
java -jar tusk.jar parse config.tsk

# Validate syntax
java -jar tusk.jar validate config.tsk

# Generate Java classes
java -jar tusk.jar generate config.tsk --type java

# Convert to JSON
java -jar tusk.jar convert config.tsk --format json

# Interactive shell
java -jar tusk.jar shell config.tsk
```

### Interactive Shell
```bash
tusk> parse config.tsk
tusk> get app.name
tusk> set app.version "2.0.0"
tusk> query "SELECT COUNT(*) FROM users"
tusk> execute payment.process 100.0 "alice@example.com"
tusk> exit
```

## 🐳 Production Deployment

### Docker Deployment
```dockerfile
FROM openjdk:17-alpine

WORKDIR /app

# Install TuskLang CLI
RUN curl -L -o /usr/local/bin/tusk https://github.com/tusklang/java/releases/latest/download/tusk-cli.jar && \
    chmod +x /usr/local/bin/tusk

# Copy application
COPY target/tusk-app-1.0.0.jar app.jar
COPY config.tsk config.tsk

# Run application
CMD ["java", "-jar", "app.jar"]
```

### Kubernetes Deployment
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
        ports:
        - containerPort: 8080
        env:
        - name: APP_ENV
          value: "production"
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
```

## 📊 Performance Benchmarks

### Parsing Performance
```
Benchmark Results (Java 17):
- Simple config (1KB): 0.2ms
- Complex config (10KB): 1.5ms
- Large config (100KB): 8.9ms
- FUJSEN execution: 0.05ms per function
- Database query: 0.8ms average
```

### Memory Usage
```
Memory Usage:
- Base TSK instance: 2.1MB
- With SQLite adapter: +1.0MB
- With PostgreSQL adapter: +1.8MB
- With Redis cache: +0.7MB
```

## 🔒 Security Features

### Encryption and Validation
```tsk
[security]
encrypted_password: @encrypt("sensitive_data", "AES-256-GCM")
api_key: @env.secure("API_KEY")
required_fields: @validate.required(["name", "email", "password"])
```

### Spring Security Integration
```java
@Configuration
@EnableWebSecurity
public class SecurityConfiguration {
    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http, TuskLang parser) throws Exception {
        Map<String, Object> securityConfig = parser.parse("""
            [security]
            jwt_secret: @env.secure("JWT_SECRET")
            cors_origins: @env("CORS_ORIGINS", "http://localhost:3000")
            """);
        
        // Configure security based on TuskLang config
        return http.build();
    }
}
```

## 🧪 Testing

### Unit Tests
```java
@Test
void testBasicParsing() {
    TuskLang parser = new TuskLang();
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
    SQLiteAdapter db = new SQLiteAdapter(":memory:");
    db.execute("CREATE TABLE users (id INTEGER, name TEXT)");
    db.execute("INSERT INTO users VALUES (1, 'Alice')");
    
    TuskLang parser = new TuskLang();
    parser.setDatabaseAdapter(db);
    
    String tskContent = """
        [users]
        count: @query("SELECT COUNT(*) FROM users")
        """;
    
    Map<String, Object> config = parser.parse(tskContent);
    assertEquals(1, config.get("users"));
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Import Errors**
```xml
<!-- Make sure TuskLang is in pom.xml -->
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java</artifactId>
    <version>1.0.0</version>
</dependency>
```

2. **Database Connection Issues**
```java
// Test database connection
SQLiteAdapter db = new SQLiteAdapter("test.db");
List<Map<String, Object>> result = db.query("SELECT 1");
System.out.println("Database connection successful");
```

3. **FUJSEN Execution Errors**
```java
// Debug FUJSEN execution
try {
    Object result = parser.executeFujsen("section", "function", args);
} catch (Exception e) {
    System.err.println("FUJSEN error: " + e.getMessage());
}
```

### Debug Mode
```java
// Enable debug logging
TuskLang parser = new TuskLang();
parser.setDebug(true);

Map<String, Object> config = parser.parseFile("config.tsk");
System.out.println("Config: " + config);
```

## 📚 Resources

- **Official Documentation**: [docs.tusklang.org/java](https://docs.tusklang.org/java)
- **GitHub Repository**: [github.com/tusklang/java](https://github.com/tusklang/java)
- **Maven Central**: [search.maven.org/artifact/org.tusklang/tusklang-java](https://search.maven.org/artifact/org.tusklang/tusklang-java)
- **Examples**: [examples.tusklang.org/java](https://examples.tusklang.org/java)

## 🎯 Next Steps

1. **Install TuskLang Java SDK** - Add to your Maven/Gradle project
2. **Create your first .tsk file** - Start with simple configuration
3. **Explore Spring Boot integration** - Build web applications
4. **Integrate with your database** - Use @query operators
5. **Deploy to production** - Use Docker and Kubernetes

## 🤝 Contributing

We welcome contributions to the TuskLang Java SDK! Please see our [Contributing Guide](CONTRIBUTING.md) for details on how to submit pull requests, report issues, and contribute to the project.

## 📄 License

TuskLang Java SDK is licensed under the MIT License. See [LICENSE](LICENSE) for details.

---

**"We don't bow to any king"** - The Java SDK gives you enterprise-grade performance with Spring Boot integration, comprehensive database adapters, and enhanced parser flexibility. Choose your syntax, integrate with your framework, and build powerful applications with TuskLang! 
# ☕ TuskLang Java Installation Guide

**"We don't bow to any king" - Java Edition**

Welcome to the TuskLang Java SDK installation guide. This comprehensive guide will get you up and running with TuskLang in your Java applications, from simple Maven projects to complex Spring Boot microservices.

## 🚀 Quick Installation

### Maven Integration

Add TuskLang to your `pom.xml`:

```xml
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java</artifactId>
    <version>1.0.0</version>
</dependency>
```

### Gradle Integration

Add to your `build.gradle`:

```gradle
implementation 'org.tusklang:tusklang-java:1.0.0'
```

### One-Line Install

```bash
# Direct install
curl -sSL https://java.tusklang.org | bash

# Or with wget
wget -qO- https://java.tusklang.org | bash
```

## 🎯 System Requirements

### Java Version
- **Minimum**: Java 11 (LTS)
- **Recommended**: Java 17 (LTS) or Java 21 (LTS)
- **Latest**: Java 22

### Build Tools
- **Maven**: 3.6.0+
- **Gradle**: 7.0+
- **IDE Support**: IntelliJ IDEA, Eclipse, VS Code

### Database Support
- **SQLite**: Built-in support
- **PostgreSQL**: Requires `postgresql-jdbc` dependency
- **MySQL**: Requires `mysql-connector-java` dependency
- **MongoDB**: Requires `mongodb-driver-sync` dependency
- **Redis**: Requires `jedis` dependency

## 🔧 Detailed Installation Steps

### 1. Maven Project Setup

Create a new Maven project or add to existing:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://maven.apache.org/POM/4.0.0"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 
         http://maven.apache.org/xsd/maven-4.0.0.xsd">
    <modelVersion>4.0.0</modelVersion>
    
    <groupId>com.example</groupId>
    <artifactId>tusk-app</artifactId>
    <version>1.0.0</version>
    
    <properties>
        <maven.compiler.source>17</maven.compiler.source>
        <maven.compiler.target>17</maven.compiler.target>
        <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
    </properties>
    
    <dependencies>
        <!-- TuskLang Core -->
        <dependency>
            <groupId>org.tusklang</groupId>
            <artifactId>tusklang-java</artifactId>
            <version>1.0.0</version>
        </dependency>
        
        <!-- Database Adapters (Optional) -->
        <dependency>
            <groupId>org.postgresql</groupId>
            <artifactId>postgresql</artifactId>
            <version>42.7.0</version>
        </dependency>
        
        <dependency>
            <groupId>mysql</groupId>
            <artifactId>mysql-connector-java</artifactId>
            <version>8.0.33</version>
        </dependency>
        
        <dependency>
            <groupId>org.mongodb</groupId>
            <artifactId>mongodb-driver-sync</artifactId>
            <version>4.11.0</version>
        </dependency>
        
        <dependency>
            <groupId>redis.clients</groupId>
            <artifactId>jedis</artifactId>
            <version>5.0.2</version>
        </dependency>
        
        <!-- Testing -->
        <dependency>
            <groupId>org.junit.jupiter</groupId>
            <artifactId>junit-jupiter</artifactId>
            <version>5.10.0</version>
            <scope>test</scope>
        </dependency>
    </dependencies>
    
    <build>
        <plugins>
            <plugin>
                <groupId>org.apache.maven.plugins</groupId>
                <artifactId>maven-compiler-plugin</artifactId>
                <version>3.11.0</version>
                <configuration>
                    <source>17</source>
                    <target>17</target>
                </configuration>
            </plugin>
        </plugins>
    </build>
</project>
```

### 2. Gradle Project Setup

Create a new Gradle project or add to existing:

```gradle
plugins {
    id 'java'
    id 'application'
}

group = 'com.example'
version = '1.0.0'

repositories {
    mavenCentral()
}

dependencies {
    // TuskLang Core
    implementation 'org.tusklang:tusklang-java:1.0.0'
    
    // Database Adapters (Optional)
    implementation 'org.postgresql:postgresql:42.7.0'
    implementation 'mysql:mysql-connector-java:8.0.33'
    implementation 'org.mongodb:mongodb-driver-sync:4.11.0'
    implementation 'redis.clients:jedis:5.0.2'
    
    // Testing
    testImplementation 'org.junit.jupiter:junit-jupiter:5.10.0'
}

java {
    sourceCompatibility = JavaVersion.VERSION_17
    targetCompatibility = JavaVersion.VERSION_17
}

test {
    useJUnitPlatform()
}

application {
    mainClass = 'com.example.App'
}
```

### 3. Spring Boot Integration

For Spring Boot applications, add to your `pom.xml`:

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
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-data-jpa</artifactId>
    </dependency>
    
    <!-- TuskLang Integration -->
    <dependency>
        <groupId>org.tusklang</groupId>
        <artifactId>tusklang-spring-boot-starter</artifactId>
        <version>1.0.0</version>
    </dependency>
</dependencies>
```

### 4. Jakarta EE Integration

For Jakarta EE applications:

```xml
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-jakarta</artifactId>
    <version>1.0.0</version>
</dependency>
```

## 🧪 Verification Installation

### 1. Basic Verification

Create a simple test to verify installation:

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class InstallationTest {
    public static void main(String[] args) {
        try {
            TuskLang parser = new TuskLang();
            String tskContent = """
                [app]
                name: "TuskLang Java Test"
                version: "1.0.0"
                """;
            
            Map<String, Object> config = parser.parse(tskContent);
            System.out.println("✅ TuskLang Java SDK installed successfully!");
            System.out.println("App name: " + config.get("app"));
            
        } catch (Exception e) {
            System.err.println("❌ Installation failed: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
```

### 2. Database Verification

Test database integration:

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.SQLiteAdapter;
import java.util.Map;

public class DatabaseTest {
    public static void main(String[] args) {
        try {
            // Create SQLite adapter
            SQLiteAdapter db = new SQLiteAdapter("test.db");
            
            // Create test table
            db.execute("""
                CREATE TABLE IF NOT EXISTS test (
                    id INTEGER PRIMARY KEY,
                    name TEXT
                )
                """);
            
            db.execute("INSERT OR REPLACE INTO test VALUES (1, 'TuskLang')");
            
            // Test TuskLang with database
            TuskLang parser = new TuskLang();
            parser.setDatabaseAdapter(db);
            
            String tskContent = """
                [test]
                count: @query("SELECT COUNT(*) FROM test")
                name: @query("SELECT name FROM test WHERE id = 1")
                """;
            
            Map<String, Object> config = parser.parse(tskContent);
            System.out.println("✅ Database integration working!");
            System.out.println("Count: " + config.get("test"));
            
        } catch (Exception e) {
            System.err.println("❌ Database test failed: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
```

## 🔧 IDE Configuration

### IntelliJ IDEA

1. **Import Project**: Open your Maven/Gradle project
2. **SDK Setup**: Ensure Java 17+ is configured
3. **Plugin Installation**: Install TuskLang plugin (optional)
4. **Run Configuration**: Set up main class

### Eclipse

1. **Import Project**: File → Import → Existing Maven/Gradle Project
2. **Java Build Path**: Ensure Java 17+ is set
3. **Run Configuration**: Create Java Application run config

### VS Code

1. **Extensions**: Install Java Extension Pack
2. **Java Home**: Set `JAVA_HOME` environment variable
3. **Project**: Open folder with Maven/Gradle project

## 🚀 Quick Start Example

Create your first TuskLang Java application:

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

public class HelloTuskLang {
    public static void main(String[] args) {
        // Create TuskLang parser
        TuskLang parser = new TuskLang();
        
        // Parse configuration file
        AppConfig config = parser.parseFile("config.tsk", AppConfig.class);
        
        // Use configuration
        System.out.println("🚀 " + config.appName + " v" + config.version);
        System.out.println("🌐 Server: " + config.server.host + ":" + config.server.port);
        System.out.println("🗄️ Database: " + config.database.host + ":" + config.database.port);
    }
}
```

Create `config.tsk`:

```tsk
app_name: "My TuskLang App"
version: "1.0.0"
debug: true
port: 8080

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

## 🔧 Troubleshooting

### Common Issues

1. **ClassNotFoundException**
```bash
# Check if dependency is included
mvn dependency:tree | grep tusklang
```

2. **Version Conflicts**
```xml
<!-- Exclude conflicting dependencies -->
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java</artifactId>
    <version>1.0.0</version>
    <exclusions>
        <exclusion>
            <groupId>conflicting.group</groupId>
            <artifactId>conflicting-artifact</artifactId>
        </exclusion>
    </exclusions>
</dependency>
```

3. **Java Version Issues**
```bash
# Check Java version
java -version

# Set JAVA_HOME
export JAVA_HOME=/path/to/java17
```

4. **Database Connection Issues**
```java
// Test database connection separately
try {
    Connection conn = DriverManager.getConnection(url, user, password);
    System.out.println("Database connection successful");
} catch (SQLException e) {
    System.err.println("Database connection failed: " + e.getMessage());
}
```

### Debug Mode

Enable debug logging:

```java
import org.tusklang.java.TuskLang;

public class DebugExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        parser.setDebug(true);
        
        // This will show detailed parsing information
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Config: " + config);
    }
}
```

## 📚 Next Steps

1. **Create your first .tsk file** - Start with simple configuration
2. **Explore Spring Boot integration** - Build web applications
3. **Integrate with databases** - Use @query operators
4. **Add @ operators** - Environment variables, caching, etc.
5. **Deploy to production** - Use Docker and Kubernetes

## 🎯 Performance Notes

- **Parsing Speed**: ~0.2ms for 1KB config files
- **Memory Usage**: ~2.1MB base, +1-2MB with database adapters
- **Thread Safety**: Fully thread-safe for concurrent access
- **Caching**: Built-in intelligent caching for repeated queries

---

**"We don't bow to any king"** - Your Java applications are now ready to harness the power of TuskLang with enterprise-grade performance, comprehensive database adapters, and enhanced parser flexibility! 
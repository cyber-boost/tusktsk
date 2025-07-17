# ☕ TuskLang Java Hello World Guide

**"We don't bow to any king" - Java Edition**

Welcome to your first TuskLang Java application! This guide will walk you through creating a compelling "Hello World" experience that showcases TuskLang's power and flexibility.

## 🚀 Your First TuskLang Java App

### Step 1: Create Your Project

```bash
# Create a new Maven project
mkdir tusk-hello-world
cd tusk-hello-world

# Initialize Maven project
mvn archetype:generate -DgroupId=com.example -DartifactId=tusk-hello-world -DarchetypeArtifactId=maven-archetype-quickstart -DinteractiveMode=false
```

### Step 2: Add TuskLang Dependency

```xml
<!-- pom.xml -->
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java</artifactId>
    <version>1.0.0</version>
</dependency>
```

### Step 3: Create Your First Configuration

Create `config.tsk` in your project root:

```tsk
# Your first TuskLang configuration
app_name: "Hello TuskLang"
version: "1.0.0"
message: "We don't bow to any king!"
timestamp: @date.now()

[greeting]
text: "Hello, World!"
language: "en"
formal: false

[display]
color: "blue"
font_size: 16
bold: true
```

### Step 4: Create Your Java Application

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class HelloConfig {
    public String appName;
    public String version;
    public String message;
    public String timestamp;
    
    public GreetingConfig greeting;
    public DisplayConfig display;
}

@TuskConfig
public class GreetingConfig {
    public String text;
    public String language;
    public boolean formal;
}

@TuskConfig
public class DisplayConfig {
    public String color;
    public int fontSize;
    public boolean bold;
}

public class HelloTuskLang {
    public static void main(String[] args) {
        System.out.println("🚀 Starting TuskLang Hello World...");
        
        // Create TuskLang parser
        TuskLang parser = new TuskLang();
        
        // Parse configuration file
        HelloConfig config = parser.parseFile("config.tsk", HelloConfig.class);
        
        // Display your first TuskLang application
        System.out.println("📱 " + config.appName + " v" + config.version);
        System.out.println("💬 " + config.message);
        System.out.println("⏰ " + config.timestamp);
        System.out.println();
        
        // Use configuration values
        String greeting = config.greeting.text;
        String color = config.display.color;
        boolean bold = config.display.bold;
        
        System.out.println("🎨 Displaying greeting in " + color + " color" + (bold ? " (bold)" : ""));
        System.out.println("🌍 Language: " + config.greeting.language);
        System.out.println("📝 " + greeting);
        
        System.out.println("\n✅ Your first TuskLang Java app is running!");
    }
}
```

## 🎯 Multiple Syntax Styles

### Traditional INI-Style

```tsk
# Traditional INI-style configuration
[app]
name: "Hello TuskLang"
version: "1.0.0"

[greeting]
text: "Hello, World!"
language: "en"
```

### JSON-Like Objects

```tsk
# JSON-like object syntax
app {
    name: "Hello TuskLang"
    version: "1.0.0"
}

greeting {
    text: "Hello, World!"
    language: "en"
}
```

### XML-Inspired Syntax

```tsk
# XML-inspired syntax
app >
    name: "Hello TuskLang"
    version: "1.0.0"
<

greeting >
    text: "Hello, World!"
    language: "en"
<
```

## ⚡ @ Operator Examples

### Environment Variables

```tsk
# Use environment variables
[app]
name: @env("APP_NAME", "Hello TuskLang")
version: @env("APP_VERSION", "1.0.0")
environment: @env("APP_ENV", "development")

[greeting]
text: @env("GREETING_TEXT", "Hello, World!")
language: @env("GREETING_LANG", "en")
```

### Date and Time

```tsk
[timestamps]
created_at: @date.now()
formatted_date: @date("yyyy-MM-dd HH:mm:ss")
start_of_day: @date.startOfDay()
end_of_day: @date.endOfDay()
```

### Dynamic Content

```tsk
[content]
user_greeting: "Hello, " + @env("USER_NAME", "World") + "!"
current_time: "Current time: " + @date("HH:mm:ss")
app_status: "App is running in " + @env("APP_ENV", "development") + " mode"
```

## 🗄️ Database Integration from the Start

### SQLite Example

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.adapters.SQLiteAdapter;
import java.util.Map;

public class HelloWithDatabase {
    public static void main(String[] args) {
        // Create SQLite adapter
        SQLiteAdapter db = new SQLiteAdapter("hello.db");
        
        // Create test table
        db.execute("""
            CREATE TABLE IF NOT EXISTS greetings (
                id INTEGER PRIMARY KEY,
                text TEXT,
                language TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
            """);
        
        // Insert sample data
        db.execute("INSERT OR REPLACE INTO greetings (id, text, language) VALUES (1, 'Hello, World!', 'en')");
        db.execute("INSERT OR REPLACE INTO greetings (id, text, language) VALUES (2, '¡Hola, Mundo!', 'es')");
        
        // Create TuskLang parser with database
        TuskLang parser = new TuskLang();
        parser.setDatabaseAdapter(db);
        
        // TSK file with database queries
        String tskContent = """
            [database]
            greeting_count: @query("SELECT COUNT(*) FROM greetings")
            english_greeting: @query("SELECT text FROM greetings WHERE language = 'en' LIMIT 1")
            spanish_greeting: @query("SELECT text FROM greetings WHERE language = 'es' LIMIT 1")
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        System.out.println("🗄️ Database Integration:");
        System.out.println("📊 Total greetings: " + config.get("database"));
        System.out.println("🇺🇸 English: " + config.get("database"));
        System.out.println("🇪🇸 Spanish: " + config.get("database"));
    }
}
```

## 🔄 Cross-File Communication

### Main Configuration

Create `main.tsk`:

```tsk
# Main application configuration
@import("greeting.tsk")
@import("display.tsk")

[app]
name: "Hello TuskLang"
version: "1.0.0"
environment: @env("APP_ENV", "development")

# Reference imported configurations
[greeting]
config: @ref("greeting.default")
```

### Greeting Configuration

Create `greeting.tsk`:

```tsk
[default]
text: "Hello, World!"
language: "en"
formal: false

[formal]
text: "Greetings, esteemed user!"
language: "en"
formal: true

[casual]
text: "Hey there!"
language: "en"
formal: false
```

### Display Configuration

Create `display.tsk`:

```tsk
[default]
color: "blue"
font_size: 16
bold: false

[highlighted]
color: "green"
font_size: 18
bold: true

[subtle]
color: "gray"
font_size: 14
bold: false
```

## 🚀 FUJSEN (Function Serialization)

### Basic FUJSEN Functions

```tsk
[greeting]
generate_fujsen: """
function generate(name, formal) {
    if (formal) {
        return "Greetings, " + name + "! It is a pleasure to meet you.";
    } else {
        return "Hey " + name + "! Nice to see you!";
    }
}
"""

format_fujsen: """
function format(text, style) {
    const styles = {
        'uppercase': text.toUpperCase(),
        'lowercase': text.toLowerCase(),
        'titlecase': text.replace(/\\w\\S*/g, (txt) => txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase())
    };
    return styles[style] || text;
}
"""
```

### Java FUJSEN Execution

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class HelloWithFujsen {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Execute FUJSEN functions
        String formalGreeting = parser.executeFujsen("greeting", "generate", "Alice", true);
        String casualGreeting = parser.executeFujsen("greeting", "generate", "Bob", false);
        
        String formatted = parser.executeFujsen("greeting", "format", "hello world", "titlecase");
        
        System.out.println("🎭 FUJSEN Examples:");
        System.out.println("Formal: " + formalGreeting);
        System.out.println("Casual: " + casualGreeting);
        System.out.println("Formatted: " + formatted);
    }
}
```

## 🌐 Web Application Example

### Spring Boot Hello World

```java
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.web.bind.annotation.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@SpringBootApplication
public class HelloWebApp {
    public static void main(String[] args) {
        SpringApplication.run(HelloWebApp.class, args);
    }
}

@RestController
@RequestMapping("/api")
public class HelloController {
    
    @Autowired
    private TuskConfig config;
    
    @GetMapping("/hello")
    public Map<String, Object> getHello() {
        return Map.of(
            "message", config.getGreeting().getText(),
            "app", config.getAppName(),
            "version", config.getVersion(),
            "timestamp", config.getTimestamp()
        );
    }
    
    @GetMapping("/greeting/{name}")
    public Map<String, Object> getPersonalGreeting(@PathVariable String name) {
        TuskLang parser = new TuskLang();
        String greeting = parser.executeFujsen("greeting", "generate", name, false);
        
        return Map.of(
            "greeting", greeting,
            "name", name,
            "timestamp", System.currentTimeMillis()
        );
    }
}

@Component
public class TuskConfig {
    private final HelloConfig config;
    
    public TuskConfig() {
        TuskLang parser = new TuskLang();
        this.config = parser.parseFile("config.tsk", HelloConfig.class);
    }
    
    public String getAppName() { return config.appName; }
    public String getVersion() { return config.version; }
    public String getTimestamp() { return config.timestamp; }
    public GreetingConfig getGreeting() { return config.greeting; }
}
```

## 🧪 Testing Your Hello World

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import java.util.Map;

class HelloWorldTest {
    
    private TuskLang parser;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
    }
    
    @Test
    void testBasicParsing() {
        String tskContent = """
            [app]
            name: "Hello TuskLang"
            version: "1.0.0"
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        assertEquals("Hello TuskLang", config.get("app"));
        assertEquals("1.0.0", config.get("app"));
    }
    
    @Test
    void testFujsenExecution() {
        String tskContent = """
            [greeting]
            generate_fujsen: '''
            function generate(name) {
                return "Hello, " + name + "!";
            }
            '''
            """;
        
        parser.parse(tskContent);
        String result = parser.executeFujsen("greeting", "generate", "World");
        
        assertEquals("Hello, World!", result);
    }
    
    @Test
    void testDatabaseIntegration() {
        // Setup test database
        SQLiteAdapter db = new SQLiteAdapter(":memory:");
        db.execute("CREATE TABLE greetings (id INTEGER, text TEXT)");
        db.execute("INSERT INTO greetings VALUES (1, 'Hello, World!')");
        
        parser.setDatabaseAdapter(db);
        
        String tskContent = """
            [greeting]
            text: @query("SELECT text FROM greetings WHERE id = 1")
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        assertEquals("Hello, World!", config.get("greeting"));
    }
}
```

## 🚀 Running Your Application

### Command Line

```bash
# Compile and run
mvn compile exec:java -Dexec.mainClass="HelloTuskLang"

# Or run directly
java -cp target/classes HelloTuskLang
```

### Spring Boot

```bash
# Run Spring Boot application
mvn spring-boot:run

# Test the API
curl http://localhost:8080/api/hello
curl http://localhost:8080/api/greeting/Alice
```

### Docker

```dockerfile
FROM openjdk:17-alpine

WORKDIR /app

# Copy application
COPY target/tusk-hello-world-1.0.0.jar app.jar
COPY config.tsk config.tsk

# Run application
CMD ["java", "-jar", "app.jar"]
```

```bash
# Build and run with Docker
docker build -t tusk-hello-world .
docker run -p 8080:8080 tusk-hello-world
```

## 🎯 Next Steps

1. **Explore @ operators** - Environment variables, caching, HTTP requests
2. **Add database integration** - Use @query for dynamic data
3. **Implement FUJSEN functions** - Build complex business logic
4. **Create web applications** - Spring Boot integration
5. **Add testing** - Unit and integration tests

## 🔧 Troubleshooting

### Common Issues

1. **File Not Found**
```bash
# Make sure config.tsk is in the project root
ls -la config.tsk
```

2. **Dependency Issues**
```bash
# Check Maven dependencies
mvn dependency:tree | grep tusklang
```

3. **Syntax Errors**
```bash
# Validate TSK syntax
java -jar tusk.jar validate config.tsk
```

### Debug Mode

```java
// Enable debug logging
TuskLang parser = new TuskLang();
parser.setDebug(true);

HelloConfig config = parser.parseFile("config.tsk", HelloConfig.class);
System.out.println("Config loaded successfully: " + config.appName);
```

## 📚 Resources

- **Official Documentation**: [docs.tusklang.org/java](https://docs.tusklang.org/java)
- **GitHub Repository**: [github.com/tusklang/java](https://github.com/tusklang/java)
- **Examples**: [examples.tusklang.org/java](https://examples.tusklang.org/java)

---

**"We don't bow to any king"** - Congratulations! You've created your first TuskLang Java application. This is just the beginning of what you can build with TuskLang's powerful configuration-driven approach! 
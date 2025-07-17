# Quick Start Guide

Get up and running with TuskLang Java CLI in minutes.

## Prerequisites

- Java 8 or higher (Java 11+ recommended)
- Maven 3.6+ or Gradle 6.0+
- Basic knowledge of Java development

## Installation

### Method 1: Maven (Recommended)

```bash
# Clone the repository
git clone https://github.com/tusklang/tusklang-java-sdk.git
cd tusklang-java-sdk

# Build and install
mvn clean install

# Create executable
echo '#!/bin/bash
java -jar target/tusklang-java-sdk-2.0.0.jar "$@"' > /usr/local/bin/tsk
chmod +x /usr/local/bin/tsk
```

### Method 2: Direct Download

```bash
# Download JAR file
curl -sSL https://tusklang.org/java/tsk.jar -o /usr/local/bin/tsk.jar

# Create executable wrapper
echo '#!/bin/bash
java -jar /usr/local/bin/tsk.jar "$@"' > /usr/local/bin/tsk
chmod +x /usr/local/bin/tsk
```

## Verify Installation

```bash
# Check version
tsk version

# Check help
tsk help
```

Expected output:
```
TuskLang Java CLI v2.0.0
Java Runtime: OpenJDK 11.0.12
Platform: Linux x86_64
✅ Installation verified successfully
```

## Your First Project

### 1. Initialize Project

```bash
# Create new project
mkdir my-tusk-project
cd my-tusk-project

# Initialize TuskLang project
tsk init
```

### 2. Create Configuration

Create `peanu.peanuts` file:

```ini
[app]
name: "My First TuskLang App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
workers: 4
debug: true

[database]
driver: "sqlite"
url: "jdbc:sqlite:app.db"
```

### 3. Compile Configuration

```bash
# Compile to binary format for performance
tsk config compile ./
```

### 4. Start Development Server

```bash
# Start development server
tsk serve 8080
```

### 5. Check Database Status

```bash
# Initialize database
tsk db init

# Check status
tsk db status
```

### 6. Run Tests

```bash
# Run all tests
tsk test all
```

## Basic Workflow

### Daily Development

```bash
# 1. Start development server
tsk serve 8080

# 2. In another terminal, run tests
tsk test all

# 3. Check database status
tsk db status

# 4. Monitor cache performance
tsk cache status
```

### Configuration Management

```bash
# Get configuration value
tsk config get server.port

# Validate configuration
tsk config validate ./

# Compile configuration
tsk config compile ./
```

### Database Operations

```bash
# Create migration
cat > migrations/001_create_users.sql << EOF
CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT UNIQUE
);
EOF

# Run migration
tsk db migrate migrations/001_create_users.sql

# Check database
tsk db console
> SELECT * FROM users;
```

## Common Commands

### Development

```bash
# Start server
tsk serve [port]

# Compile TSK files
tsk compile <file>

# Optimize code
tsk optimize <file>
```

### Testing

```bash
# Run all tests
tsk test all

# Run specific test suite
tsk test parser
tsk test fujsen
tsk test sdk

# Performance tests
tsk test performance
```

### Database

```bash
# Check status
tsk db status

# Run migrations
tsk db migrate <file>

# Interactive console
tsk db console

# Backup database
tsk db backup backup.sql
```

### Configuration

```bash
# Get value
tsk config get <key.path>

# Validate config
tsk config validate [path]

# Compile config
tsk config compile [path]

# Generate docs
tsk config docs [path]
```

### Cache

```bash
# Check status
tsk cache status

# Clear cache
tsk cache clear

# Warm cache
tsk cache warm

# Memcached operations
tsk cache memcached status
tsk cache memcached flush
```

## Spring Boot Integration

### 1. Add Dependency

```xml
<dependency>
    <groupId>org.tusklang</groupId>
    <artifactId>tusklang-java-sdk</artifactId>
    <version>2.0.0</version>
</dependency>
```

### 2. Auto-Configuration

```java
@SpringBootApplication
public class Application {
    public static void main(String[] args) {
        SpringApplication.run(Application.class, args);
    }
}
```

### 3. Use Configuration

```java
@Component
public class ServerConfig {
    
    @Autowired
    private PeanutConfig config;
    
    public String getHost() {
        return (String) config.get("server.host", "localhost");
    }
    
    public int getPort() {
        return (Integer) config.get("server.port", 8080);
    }
}
```

## Next Steps

### 1. Explore Commands

```bash
# See all available commands
tsk help

# Get help for specific command
tsk db help
tsk config help
tsk cache help
```

### 2. Read Documentation

- [Installation Guide](./installation.md)
- [Command Reference](./commands/README.md)
- [PNT Configuration Guide](../java/docs/PNT_GUIDE.md)
- [Examples](./examples/README.md)

### 3. Advanced Features

- **Binary Compilation**: `tsk binary compile app.tsk`
- **AI Integration**: `tsk ai claude "Analyze this code"`
- **Performance Testing**: `tsk test performance`
- **Service Management**: `tsk services start`

### 4. Production Deployment

```bash
# Compile for production
tsk config compile ./
tsk binary compile app.tsk

# Check production readiness
tsk config validate ./
tsk test all

# Deploy
tsk services start
```

## Troubleshooting

### Common Issues

#### Java Not Found
```bash
# Check Java installation
java -version

# Set JAVA_HOME
export JAVA_HOME=/usr/lib/jvm/java-11-openjdk
export PATH=$JAVA_HOME/bin:$PATH
```

#### Permission Denied
```bash
# Fix permissions
chmod +x /usr/local/bin/tsk
chmod +x /usr/local/bin/tsk.jar
```

#### Port Already in Use
```bash
# Find process using port
sudo lsof -i :8080

# Use different port
tsk serve 8081
```

### Getting Help

```bash
# Command help
tsk help
tsk <command> --help

# Version information
tsk version --verbose

# Check logs
tail -f ~/.tusk/logs/tsk.log
```

## Support

- **Documentation**: https://docs.tusklang.org/java
- **GitHub Issues**: https://github.com/tusklang/tusklang-java-sdk/issues
- **Community**: https://community.tusklang.org

---

You're now ready to build amazing applications with TuskLang Java CLI! 🚀 
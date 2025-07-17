# ☕ TuskLang Java CLI Tools Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang CLI tools in Java for command-line configuration management, interactive shell, utility commands, and automation scripts.

## 🚀 CLI Installation

### Maven Plugin

```xml
<build>
    <plugins>
        <plugin>
            <groupId>org.tusklang</groupId>
            <artifactId>tusklang-maven-plugin</artifactId>
            <version>1.0.0</version>
            <executions>
                <execution>
                    <goals>
                        <goal>parse</goal>
                    </goals>
                </execution>
            </executions>
        </plugin>
    </plugins>
</build>
```

### Gradle Plugin

```gradle
plugins {
    id 'org.tusklang.tusklang' version '1.0.0'
}

tusk {
    configFile = 'config.tsk'
    outputFormat = 'json'
    validate = true
}
```

### Standalone JAR

```bash
# Download standalone JAR
curl -L -o tusk.jar https://github.com/tusklang/java/releases/latest/download/tusk-cli.jar

# Make executable
chmod +x tusk.jar

# Run
java -jar tusk.jar --help
```

## 🎯 Basic CLI Commands

### Parse Command

```bash
# Parse TSK file
java -jar tusk.jar parse config.tsk

# Parse with output format
java -jar tusk.jar parse config.tsk --format json
java -jar tusk.jar parse config.tsk --format yaml
java -jar tusk.jar parse config.tsk --format xml

# Parse with environment variables
APP_ENV=production java -jar tusk.jar parse config.tsk

# Parse with custom variables
java -jar tusk.jar parse config.tsk --var user_id=123 --var debug=true

# Output to file
java -jar tusk.jar parse config.tsk --output result.json
```

### Validate Command

```bash
# Validate TSK syntax
java -jar tusk.jar validate config.tsk

# Validate with schema
java -jar tusk.jar validate config.tsk --schema schema.json

# Validate with custom rules
java -jar tusk.jar validate config.tsk --rules validation-rules.tsk

# Show detailed errors
java -jar tusk.jar validate config.tsk --verbose
```

### Generate Command

```bash
# Generate Java classes
java -jar tusk.jar generate config.tsk --type java --output src/main/java

# Generate with package
java -jar tusk.jar generate config.tsk --type java --package com.example.config

# Generate multiple types
java -jar tusk.jar generate config.tsk --type java,json,yaml --output generated/

# Generate with custom template
java -jar tusk.jar generate config.tsk --type java --template custom-template.ftl
```

### Convert Command

```bash
# Convert TSK to JSON
java -jar tusk.jar convert config.tsk --format json

# Convert TSK to YAML
java -jar tusk.jar convert config.tsk --format yaml

# Convert TSK to XML
java -jar tusk.jar convert config.tsk --format xml

# Convert TSK to Properties
java -jar tusk.jar convert config.tsk --format properties

# Convert from other formats to TSK
java -jar tusk.jar convert config.json --format tsk --output config.tsk
java -jar tusk.jar convert config.yaml --format tsk --output config.tsk
```

## 🔧 Advanced CLI Features

### Interactive Shell

```bash
# Start interactive shell
java -jar tusk.jar shell

# Start shell with config file
java -jar tusk.jar shell config.tsk

# Start shell with database
java -jar tusk.jar shell config.tsk --database postgresql://localhost:5432/myapp
```

### Shell Commands

```bash
# In interactive shell
tusk> help
tusk> parse config.tsk
tusk> get app.name
tusk> set app.version "2.0.0"
tusk> query "SELECT COUNT(*) FROM users"
tusk> execute payment.process 100.0 "alice@example.com"
tusk> cache.set "key" "value" 300
tusk> cache.get "key"
tusk> env.set "DEBUG" "true"
tusk> env.get "DEBUG"
tusk> exit
```

### Database Integration

```bash
# Parse with database queries
java -jar tusk.jar parse config.tsk --database postgresql://localhost:5432/myapp

# Execute database queries
java -jar tusk.jar query "SELECT COUNT(*) FROM users" --database postgresql://localhost:5432/myapp

# Execute with parameters
java -jar tusk.jar query "SELECT * FROM users WHERE id = ?" --params 123 --database postgresql://localhost:5432/myapp

# Execute FUJSEN functions
java -jar tusk.jar execute payment.process 100.0 "alice@example.com" --database postgresql://localhost:5432/myapp
```

### Environment Management

```bash
# Set environment variables
java -jar tusk.jar env set DEBUG true
java -jar tusk.jar env set API_KEY "secret-key"

# Get environment variables
java -jar tusk.jar env get DEBUG
java -jar tusk.jar env get API_KEY

# List all environment variables
java -jar tusk.jar env list

# Load from file
java -jar tusk.jar env load .env
java -jar tusk.jar env load environment.properties
```

### Cache Management

```bash
# Set cache value
java -jar tusk.jar cache set "user:123" "user_data" 300

# Get cache value
java -jar tusk.jar cache get "user:123"

# Delete cache value
java -jar tusk.jar cache delete "user:123"

# Clear all cache
java -jar tusk.jar cache clear

# List cache keys
java -jar tusk.jar cache keys "user:*"
```

## 🛠️ Utility Commands

### Diff Command

```bash
# Compare two TSK files
java -jar tusk.jar diff config1.tsk config2.tsk

# Compare with output format
java -jar tusk.jar diff config1.tsk config2.tsk --format json

# Compare with ignore patterns
java -jar tusk.jar diff config1.tsk config2.tsk --ignore "*.password" --ignore "*.secret"

# Generate patch file
java -jar tusk.jar diff config1.tsk config2.tsk --output patch.diff
```

### Merge Command

```bash
# Merge multiple TSK files
java -jar tusk.jar merge config1.tsk config2.tsk config3.tsk --output merged.tsk

# Merge with conflict resolution
java -jar tusk.jar merge config1.tsk config2.tsk --resolve conflicts.json --output merged.tsk

# Merge with priority
java -jar tusk.jar merge config1.tsk config2.tsk --priority config2.tsk --output merged.tsk
```

### Watch Command

```bash
# Watch for file changes
java -jar tusk.jar watch config.tsk --command "echo 'Config changed'"

# Watch with reload
java -jar tusk.jar watch config.tsk --reload

# Watch multiple files
java -jar tusk.jar watch config.tsk database.tsk --command "java -jar app.jar"

# Watch with debounce
java -jar tusk.jar watch config.tsk --debounce 1000 --command "echo 'Config changed'"
```

### Benchmark Command

```bash
# Benchmark parsing performance
java -jar tusk.jar benchmark config.tsk --iterations 1000

# Benchmark with different file sizes
java -jar tusk.jar benchmark config.tsk --sizes 1k,10k,100k

# Benchmark with memory profiling
java -jar tusk.jar benchmark config.tsk --memory --iterations 100

# Benchmark comparison
java -jar tusk.jar benchmark config.tsk --compare json,yaml,xml
```

## 🔄 Automation Scripts

### Shell Scripts

```bash
#!/bin/bash
# deploy.sh - Deploy application with TuskLang configuration

echo "Deploying application..."

# Parse configuration
CONFIG=$(java -jar tusk.jar parse config.tsk --format json)

# Extract values
APP_NAME=$(echo $CONFIG | java -jar tusk.jar extract app.name)
VERSION=$(echo $CONFIG | java -jar tusk.jar extract app.version)
PORT=$(echo $CONFIG | java -jar tusk.jar extract server.port)

echo "Deploying $APP_NAME v$VERSION on port $PORT"

# Build application
mvn clean package

# Deploy with configuration
java -jar target/app.jar --config config.tsk
```

### Python Scripts

```python
#!/usr/bin/env python3
# deploy.py - Python deployment script with TuskLang

import subprocess
import json
import sys

def run_tusk_command(args):
    """Run TuskLang CLI command"""
    result = subprocess.run(['java', '-jar', 'tusk.jar'] + args, 
                          capture_output=True, text=True)
    if result.returncode != 0:
        print(f"Error: {result.stderr}")
        sys.exit(1)
    return result.stdout

def main():
    # Parse configuration
    config_json = run_tusk_command(['parse', 'config.tsk', '--format', 'json'])
    config = json.loads(config_json)
    
    # Extract values
    app_name = config['app']['name']
    version = config['app']['version']
    port = config['server']['port']
    
    print(f"Deploying {app_name} v{version} on port {port}")
    
    # Validate configuration
    run_tusk_command(['validate', 'config.tsk'])
    
    # Build and deploy
    subprocess.run(['mvn', 'clean', 'package'])
    subprocess.run(['java', '-jar', 'target/app.jar', '--config', 'config.tsk'])

if __name__ == '__main__':
    main()
```

### Java Scripts

```java
// Deploy.java - Java deployment script with TuskLang
import org.tusklang.java.TuskLang;
import java.util.Map;

public class Deploy {
    public static void main(String[] args) {
        try {
            // Parse configuration
            TuskLang parser = new TuskLang();
            Map<String, Object> config = parser.parseFile("config.tsk");
            
            // Extract values
            String appName = (String) config.get("app");
            String version = (String) config.get("app");
            Integer port = (Integer) config.get("server");
            
            System.out.println("Deploying " + appName + " v" + version + " on port " + port);
            
            // Validate configuration
            boolean isValid = parser.validate("config.tsk");
            if (!isValid) {
                System.err.println("Configuration validation failed");
                System.exit(1);
            }
            
            // Build and deploy
            ProcessBuilder pb = new ProcessBuilder("mvn", "clean", "package");
            Process p = pb.start();
            p.waitFor();
            
            pb = new ProcessBuilder("java", "-jar", "target/app.jar", "--config", "config.tsk");
            p = pb.start();
            p.waitFor();
            
        } catch (Exception e) {
            System.err.println("Deployment failed: " + e.getMessage());
            System.exit(1);
        }
    }
}
```

## 🔧 Custom CLI Commands

### Custom Command Implementation

```java
import picocli.CommandLine;
import org.tusklang.java.TuskLang;
import java.util.Map;

@CommandLine.Command(
    name = "tusk",
    subcommands = {
        ParseCommand.class,
        ValidateCommand.class,
        GenerateCommand.class,
        ConvertCommand.class,
        ShellCommand.class,
        CustomCommand.class
    }
)
public class TuskCLI {
    public static void main(String[] args) {
        int exitCode = new CommandLine(new TuskCLI()).execute(args);
        System.exit(exitCode);
    }
}

@CommandLine.Command(name = "custom", description = "Custom command")
class CustomCommand implements Runnable {
    
    @CommandLine.Parameters(description = "TSK file to process")
    private String file;
    
    @CommandLine.Option(names = {"--option"}, description = "Custom option")
    private String option;
    
    @Override
    public void run() {
        try {
            TuskLang parser = new TuskLang();
            Map<String, Object> config = parser.parseFile(file);
            
            // Custom processing logic
            System.out.println("Processing " + file + " with option: " + option);
            System.out.println("Config: " + config);
            
        } catch (Exception e) {
            System.err.println("Error: " + e.getMessage());
            System.exit(1);
        }
    }
}
```

### Plugin System

```java
import org.tusklang.java.cli.plugin.CommandPlugin;
import org.tusklang.java.cli.plugin.PluginManager;

public class CustomPlugin implements CommandPlugin {
    
    @Override
    public String getName() {
        return "custom";
    }
    
    @Override
    public String getDescription() {
        return "Custom plugin command";
    }
    
    @Override
    public void execute(String[] args) {
        // Plugin implementation
        System.out.println("Custom plugin executed with args: " + String.join(" ", args));
    }
}

// Register plugin
PluginManager.registerPlugin(new CustomPlugin());
```

## 📊 CLI Configuration

### Configuration File

```json
{
  "cli": {
    "defaultFormat": "json",
    "outputDirectory": "./output",
    "logLevel": "INFO",
    "plugins": [
      "custom-plugin.jar"
    ],
    "aliases": {
      "p": "parse",
      "v": "validate",
      "g": "generate",
      "c": "convert"
    }
  },
  "database": {
    "default": "postgresql://localhost:5432/myapp",
    "connections": {
      "dev": "postgresql://localhost:5432/dev",
      "prod": "postgresql://prod-server:5432/prod"
    }
  },
  "cache": {
    "type": "redis",
    "host": "localhost",
    "port": 6379
  }
}
```

### Environment Variables

```bash
# CLI Configuration
export TUSK_CONFIG_FILE=config.tsk
export TUSK_OUTPUT_FORMAT=json
export TUSK_LOG_LEVEL=DEBUG
export TUSK_DATABASE_URL=postgresql://localhost:5432/myapp

# Plugin Configuration
export TUSK_PLUGIN_PATH=/path/to/plugins
export TUSK_PLUGIN_CONFIG=plugin-config.json
```

## 🧪 Testing CLI Commands

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import java.io.ByteArrayOutputStream;
import java.io.PrintStream;

class CLITest {
    
    private ByteArrayOutputStream outputStream;
    private PrintStream originalOut;
    
    @BeforeEach
    void setUp() {
        outputStream = new ByteArrayOutputStream();
        originalOut = System.out;
        System.setOut(new PrintStream(outputStream));
    }
    
    @Test
    void testParseCommand() {
        // Create test config file
        String configContent = """
            [app]
            name: "Test App"
            version: "1.0.0"
            """;
        
        // Test parse command
        String[] args = {"parse", "test-config.tsk"};
        TuskCLI.main(args);
        
        String output = outputStream.toString();
        assertTrue(output.contains("Test App"));
        assertTrue(output.contains("1.0.0"));
    }
    
    @Test
    void testValidateCommand() {
        // Test valid config
        String[] args = {"validate", "valid-config.tsk"};
        TuskCLI.main(args);
        
        String output = outputStream.toString();
        assertTrue(output.contains("Valid"));
    }
    
    @Test
    void testInvalidConfig() {
        // Test invalid config
        String[] args = {"validate", "invalid-config.tsk"};
        TuskCLI.main(args);
        
        String output = outputStream.toString();
        assertTrue(output.contains("Invalid"));
    }
}
```

### Integration Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import java.io.File;
import java.nio.file.Files;

class CLIIntegrationTest {
    
    private File tempDir;
    
    @BeforeEach
    void setUp() throws Exception {
        tempDir = Files.createTempDirectory("tusk-test").toFile();
    }
    
    @Test
    void testEndToEndWorkflow() throws Exception {
        // Create test config
        String configContent = """
            [app]
            name: "Integration Test"
            version: "1.0.0"
            
            [database]
            host: "localhost"
            port: 5432
            """;
        
        File configFile = new File(tempDir, "config.tsk");
        Files.write(configFile.toPath(), configContent.getBytes());
        
        // Test parse
        String[] parseArgs = {"parse", configFile.getAbsolutePath()};
        TuskCLI.main(parseArgs);
        
        // Test validate
        String[] validateArgs = {"validate", configFile.getAbsolutePath()};
        TuskCLI.main(validateArgs);
        
        // Test convert
        String[] convertArgs = {"convert", configFile.getAbsolutePath(), "--format", "json"};
        TuskCLI.main(convertArgs);
        
        // Verify output files
        File jsonFile = new File(tempDir, "config.json");
        assertTrue(jsonFile.exists());
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **JAR Not Found**
```bash
# Check if JAR exists
ls -la tusk.jar

# Download if missing
curl -L -o tusk.jar https://github.com/tusklang/java/releases/latest/download/tusk-cli.jar
```

2. **Permission Denied**
```bash
# Make executable
chmod +x tusk.jar

# Run with sudo if needed
sudo java -jar tusk.jar parse config.tsk
```

3. **Java Version Issues**
```bash
# Check Java version
java -version

# Use specific Java version
JAVA_HOME=/path/to/java17 java -jar tusk.jar parse config.tsk
```

4. **Configuration Errors**
```bash
# Enable debug mode
java -jar tusk.jar parse config.tsk --debug

# Validate configuration
java -jar tusk.jar validate config.tsk --verbose
```

### Debug Mode

```bash
# Enable debug logging
java -jar tusk.jar parse config.tsk --debug

# Set log level
java -jar tusk.jar parse config.tsk --log-level DEBUG

# Show execution time
java -jar tusk.jar parse config.tsk --timing
```

## 📚 Next Steps

1. **Master CLI commands** - Parse, validate, generate, convert
2. **Build automation scripts** - Shell, Python, Java scripts
3. **Create custom commands** - Plugin system and extensions
4. **Integrate with CI/CD** - Automated testing and deployment
5. **Optimize performance** - Benchmarking and optimization

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang CLI tools in Java! From basic commands to advanced automation, you can build powerful command-line tools for configuration management and deployment automation. 
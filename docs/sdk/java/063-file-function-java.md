# 📂 @file Function in TuskLang Java

**"We don't bow to any king" - Master file operations like a Java architect**

TuskLang Java provides robust @file function capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Read, write, and manage files with enterprise-grade performance and security.

## 🎯 Overview

The @file function in TuskLang Java combines Java's file I/O power with TuskLang's dynamic configuration. From reading configuration files to processing data and automating file workflows, you can build powerful, maintainable solutions.

## 🔧 Core @file Function Features

### 1. Basic File Operations
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.file.TuskFileFunctionManager;
import java.util.Map;

public class FileFunctionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        String tskContent = """
            [file_function_examples]
            # Read a file
            file_content: @file.read("/etc/hostname")
            
            # Write to a file
            write_result: @file.write("/tmp/output.txt", "Hello, TuskLang!")
            
            # Append to a file
            append_result: @file.append("/tmp/output.txt", "\nAppended line.")
            
            # Check if file exists
            exists: @file.exists("/tmp/output.txt")
            
            # Delete a file
            delete_result: @file.delete("/tmp/output.txt")
            """;
        Map<String, Object> config = parser.parse(tskContent);
        System.out.println("File content: " + config.get("file_content"));
        System.out.println("Write result: " + config.get("write_result"));
        System.out.println("Append result: " + config.get("append_result"));
        System.out.println("Exists: " + config.get("exists"));
        System.out.println("Delete result: " + config.get("delete_result"));
    }
}
```

### 2. Advanced File Patterns
```java
import org.tusklang.java.TuskLang;
import org.springframework.stereotype.Service;
import java.util.Map;

@Service
public class AdvancedFileService {
    private final TuskLang tuskParser;
    public AdvancedFileService(TuskLang tuskParser) { this.tuskParser = tuskParser; }
    public Map<String, Object> processFiles() {
        String tskContent = """
            [advanced_file_operations]
            # Read JSON config
            config: @file.read("config/app.json")
                .parseJson()
            
            # List files in a directory
            files: @file.list("/var/log/")
            
            # Move and copy files
            move_result: @file.move("/tmp/a.txt", "/tmp/b.txt")
            copy_result: @file.copy("/tmp/b.txt", "/tmp/c.txt")
            
            # File metadata
            metadata: @file.metadata("/tmp/c.txt")
            """;
        return tuskParser.parse(tskContent);
    }
}
```

### 3. Spring Boot Integration
```java
import org.tusklang.java.TuskLang;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import java.util.Map;

@Configuration
public class FileFunctionConfig {
    @Bean
    public TuskLang tuskLang() { return new TuskLang(); }
    @Bean
    public Map<String, Object> fileFunctionConfiguration() {
        TuskLang parser = new TuskLang();
        String tskContent = """
            [spring_file_functions]
            # Spring Boot config with file operations
            application: {
                config_file: @file.read("config/app.tsk")
                log_dir: @file.list("/var/log/app/")
                temp_file: @file.write("/tmp/app.tmp", "temp data")
            }
            """;
        return parser.parse(tskContent);
    }
}
```

## 🚀 Best Practices

- Always validate file paths and handle exceptions.
- Use @file.exists before reading or deleting files.
- For sensitive data, combine @file with @encrypt.
- Use .parseJson() or .parseYaml() for config files.

## 🎯 Summary

@file function in TuskLang Java provides:
- **File I/O**: Read, write, append, delete, move, and copy files
- **Metadata**: Access file metadata and directory listings
- **Spring Boot Integration**: Use file operations in configuration
- **Security**: Validate and secure file access
- **Automation**: Automate workflows and config management

Master @file to automate, secure, and optimize file operations in your Java TuskLang projects. 
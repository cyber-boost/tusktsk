# 🗃️ @json.file Function in TuskLang Java

**"We don't bow to any king" - Handle JSON files like a Java master**

TuskLang Java provides robust @json.file function capabilities that integrate seamlessly with Spring Boot, JPA, and modern Java patterns. Read, write, and process JSON files with enterprise-grade performance and type safety.

## 🎯 Overview

The @json.file function in TuskLang Java combines Java's JSON processing power with TuskLang's dynamic configuration. From reading and writing JSON config files to transforming and validating data, you can build powerful, maintainable solutions.

## 🔧 Core @json.file Function Features

### 1. Basic JSON File Operations
```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.json.TuskJsonFileFunctionManager;
import java.util.Map;

public class JsonFileFunctionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        String tskContent = """
            [json_file_examples]
            # Read a JSON file
            json_data: @json.file.read("data/users.json")
            
            # Write to a JSON file
            write_result: @json.file.write("data/output.json", { name: "Alice", age: 30 })
            
            # Append to a JSON array in a file
            append_result: @json.file.append("data/output.json", { name: "Bob", age: 25 })
            
            # Validate JSON file
            is_valid: @json.file.validate("data/output.json")
            """;
        Map<String, Object> config = parser.parse(tskContent);
        System.out.println("JSON data: " + config.get("json_data"));
        System.out.println("Write result: " + config.get("write_result"));
        System.out.println("Append result: " + config.get("append_result"));
        System.out.println("Is valid: " + config.get("is_valid"));
    }
}
```

### 2. Advanced JSON Patterns
```java
import org.tusklang.java.TuskLang;
import org.springframework.stereotype.Service;
import java.util.Map;

@Service
public class AdvancedJsonFileService {
    private final TuskLang tuskParser;
    public AdvancedJsonFileService(TuskLang tuskParser) { this.tuskParser = tuskParser; }
    public Map<String, Object> processJsonFiles() {
        String tskContent = """
            [advanced_json_file_operations]
            # Merge two JSON files
            merged: @json.file.merge("data/a.json", "data/b.json")
            
            # Filter JSON array
            filtered: @json.file.read("data/users.json")
                .filter(user -> user.age > 18)
            
            # Map and transform JSON data
            mapped: @json.file.read("data/users.json")
                .map(user -> { name: user.name.toUpperCase(), age: user.age })
            
            # JSON schema validation
            schema_valid: @json.file.validate("data/users.json", "schemas/user.schema.json")
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
public class JsonFileFunctionConfig {
    @Bean
    public TuskLang tuskLang() { return new TuskLang(); }
    @Bean
    public Map<String, Object> jsonFileFunctionConfiguration() {
        TuskLang parser = new TuskLang();
        String tskContent = """
            [spring_json_file_functions]
            # Spring Boot config with JSON file operations
            application: {
                users: @json.file.read("data/users.json")
                config: @json.file.read("config/app.json")
                merged: @json.file.merge("data/a.json", "data/b.json")
            }
            """;
        return parser.parse(tskContent);
    }
}
```

## 🚀 Best Practices

- Always validate JSON files before processing.
- Use schema validation for critical data.
- For large files, use .stream() or .batch() methods.
- Combine @json.file with @file for hybrid workflows.

## 🎯 Summary

@json.file function in TuskLang Java provides:
- **JSON File I/O**: Read, write, append, merge, and validate JSON files
- **Data Transformation**: Filter, map, and transform JSON data
- **Schema Validation**: Validate JSON files against schemas
- **Spring Boot Integration**: Use JSON file operations in configuration
- **Type Safety**: Leverage Java's type system for safe data handling

Master @json.file to automate, validate, and optimize JSON file operations in your Java TuskLang projects. 
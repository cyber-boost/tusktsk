# ☕ TuskLang Java Basic Types Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang data types in Java with comprehensive coverage of primitive types, complex types, type mapping, validation, and best practices for type-safe configuration.

## 🎯 Primitive Types

### String Values

```tsk
# String literals
app_name: "My TuskLang App"
description: "A powerful configuration-driven application"
version: "1.0.0"

# Multi-line strings
long_description: """
This is a multi-line string
that can span multiple lines
and preserve formatting.
"""

# String with special characters
path: "C:\\Users\\username\\app\\config.tsk"
url: "https://api.example.com/v1/users"
```

### Numeric Values

```tsk
# Integer values
port: 8080
max_connections: 100
timeout_seconds: 30

# Floating-point values
pi: 3.14159
tax_rate: 0.0825
memory_limit: 1024.5

# Scientific notation
avogadro_number: 6.022e23
plank_constant: 6.626e-34
```

### Boolean Values

```tsk
# Boolean literals
debug: true
production: false
ssl_enabled: true
auto_reload: false

# Boolean in sections
[features]
user_management: true
payment_processing: true
analytics: false
notifications: true
```

### Null Values

```tsk
# Null values
optional_setting: null
default_value: null
unused_config: null

# Null in objects
[database]
optional_field: null
required_field: "value"
```

## 🔧 Java Type Mapping

### Basic Type Mapping

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class BasicTypesConfig {
    // String types
    public String appName;
    public String description;
    public String version;
    
    // Numeric types
    public int port;
    public long maxConnections;
    public double taxRate;
    public float memoryLimit;
    
    // Boolean types
    public boolean debug;
    public boolean production;
    public boolean sslEnabled;
    
    // Null handling
    public String optionalSetting;
    public Integer nullableInt;
    public Boolean nullableBoolean;
}

public class BasicTypesExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        BasicTypesConfig config = parser.parseFile("config.tsk", BasicTypesConfig.class);
        
        // Access typed values
        System.out.println("App: " + config.appName + " v" + config.version);
        System.out.println("Port: " + config.port);
        System.out.println("Debug: " + config.debug);
        System.out.println("Tax Rate: " + config.taxRate);
        
        // Handle null values
        if (config.optionalSetting != null) {
            System.out.println("Optional: " + config.optionalSetting);
        }
    }
}
```

### Type Conversion

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class TypeConversionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        String tskContent = """
            [numbers]
            int_value: 42
            long_value: 9223372036854775807
            double_value: 3.14159
            float_value: 2.71828
            
            [strings]
            number_as_string: "123"
            boolean_as_string: "true"
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        // Automatic type conversion
        Integer intValue = (Integer) config.get("numbers");
        Long longValue = (Long) config.get("numbers");
        Double doubleValue = (Double) config.get("numbers");
        Float floatValue = (Float) config.get("numbers");
        
        // String conversion
        String numberString = (String) config.get("strings");
        String booleanString = (String) config.get("strings");
        
        System.out.println("Int: " + intValue);
        System.out.println("Long: " + longValue);
        System.out.println("Double: " + doubleValue);
        System.out.println("Float: " + floatValue);
    }
}
```

## 📊 Complex Types

### Arrays

```tsk
# String arrays
[features]
enabled: ["user_management", "payment_processing", "analytics"]
disabled: ["beta_features", "experimental"]

# Numeric arrays
[ports]
http_ports: [80, 8080, 8000]
https_ports: [443, 8443]

# Mixed arrays
[data]
mixed_array: ["text", 42, true, null]
timeouts: [30, 60, 120, 300]

# Nested arrays
[matrix]
grid: [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
```

### Java Array Mapping

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.List;

@TuskConfig
public class ArrayTypesConfig {
    // String arrays
    public List<String> enabledFeatures;
    public List<String> disabledFeatures;
    
    // Numeric arrays
    public List<Integer> httpPorts;
    public List<Long> httpsPorts;
    
    // Mixed arrays (use Object)
    public List<Object> mixedArray;
    public List<Integer> timeouts;
    
    // Nested arrays
    public List<List<Integer>> matrix;
}

public class ArrayTypesExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ArrayTypesConfig config = parser.parseFile("config.tsk", ArrayTypesConfig.class);
        
        // Access array elements
        System.out.println("Enabled features: " + config.enabledFeatures);
        System.out.println("HTTP ports: " + config.httpPorts);
        
        // Iterate through arrays
        for (String feature : config.enabledFeatures) {
            System.out.println("Feature: " + feature);
        }
        
        // Access nested arrays
        for (List<Integer> row : config.matrix) {
            System.out.println("Row: " + row);
        }
    }
}
```

### Objects and Nested Structures

```tsk
# Simple objects
[database]
connection {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: "secret"
}

# Nested objects
[server]
config {
    host: "0.0.0.0"
    port: 8080
    ssl {
        enabled: true
        cert_file: "/path/to/cert.pem"
        key_file: "/path/to/key.pem"
    }
    cors {
        allowed_origins: ["http://localhost:3000", "https://example.com"]
        allowed_methods: ["GET", "POST", "PUT", "DELETE"]
        allowed_headers: ["Content-Type", "Authorization"]
    }
}
```

### Java Object Mapping

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.List;

@TuskConfig
public class ObjectTypesConfig {
    public DatabaseConfig database;
    public ServerConfig server;
}

@TuskConfig
public class DatabaseConfig {
    public ConnectionConfig connection;
}

@TuskConfig
public class ConnectionConfig {
    public String host;
    public int port;
    public String name;
    public String user;
    public String password;
}

@TuskConfig
public class ServerConfig {
    public ServerConfigData config;
}

@TuskConfig
public class ServerConfigData {
    public String host;
    public int port;
    public SSLConfig ssl;
    public CORSConfig cors;
}

@TuskConfig
public class SSLConfig {
    public boolean enabled;
    public String certFile;
    public String keyFile;
}

@TuskConfig
public class CORSConfig {
    public List<String> allowedOrigins;
    public List<String> allowedMethods;
    public List<String> allowedHeaders;
}

public class ObjectTypesExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ObjectTypesConfig config = parser.parseFile("config.tsk", ObjectTypesConfig.class);
        
        // Access nested object properties
        System.out.println("Database host: " + config.database.connection.host);
        System.out.println("Database port: " + config.database.connection.port);
        
        System.out.println("Server host: " + config.server.config.host);
        System.out.println("SSL enabled: " + config.server.config.ssl.enabled);
        
        // Access array properties in objects
        System.out.println("CORS origins: " + config.server.config.cors.allowedOrigins);
    }
}
```

## 🔄 Type Validation

### Built-in Validation

```tsk
# Type validation examples
[validation]
required_string: @validate.required("app_name")
email_format: @validate.email("user_email")
password_strength: @validate.password("user_password", {
    "min_length": 8,
    "require_uppercase": true,
    "require_lowercase": true,
    "require_numbers": true,
    "require_special": true
})

# Numeric validation
port_range: @validate.range("server_port", 1, 65535)
positive_number: @validate.positive("timeout_seconds")
```

### Custom Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.Validator;
import org.tusklang.java.validation.ValidationRule;
import java.util.Map;

public class CustomValidationExample {
    public static void main(String[] args) {
        Validator validator = new Validator();
        
        // Add custom validation rules
        validator.addRule(new ValidationRule("port", value -> {
            int port = Integer.parseInt(value.toString());
            return port > 0 && port <= 65535;
        }, "Port must be between 1 and 65535"));
        
        validator.addRule(new ValidationRule("email", value -> {
            String email = value.toString();
            return email.contains("@") && email.contains(".");
        }, "Email must contain @ and ."));
        
        TuskLang parser = new TuskLang();
        parser.setValidator(validator);
        
        // Parse with validation
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Configuration validated successfully");
    }
}
```

## 🔧 Type Conversion Utilities

### Manual Type Conversion

```java
import org.tusklang.java.TuskLang;
import java.util.Map;

public class TypeConversionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Safe type conversion
        String stringValue = safeString(config.get("string_key"));
        Integer intValue = safeInteger(config.get("int_key"));
        Boolean boolValue = safeBoolean(config.get("bool_key"));
        Double doubleValue = safeDouble(config.get("double_key"));
        
        System.out.println("String: " + stringValue);
        System.out.println("Integer: " + intValue);
        System.out.println("Boolean: " + boolValue);
        System.out.println("Double: " + doubleValue);
    }
    
    // Safe conversion methods
    private static String safeString(Object value) {
        return value != null ? value.toString() : null;
    }
    
    private static Integer safeInteger(Object value) {
        if (value == null) return null;
        if (value instanceof Number) return ((Number) value).intValue();
        try {
            return Integer.parseInt(value.toString());
        } catch (NumberFormatException e) {
            return null;
        }
    }
    
    private static Boolean safeBoolean(Object value) {
        if (value == null) return null;
        if (value instanceof Boolean) return (Boolean) value;
        return Boolean.parseBoolean(value.toString());
    }
    
    private static Double safeDouble(Object value) {
        if (value == null) return null;
        if (value instanceof Number) return ((Number) value).doubleValue();
        try {
            return Double.parseDouble(value.toString());
        } catch (NumberFormatException e) {
            return null;
        }
    }
}
```

## 📝 Type-Safe Configuration Classes

### Advanced Type Mapping

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.time.LocalDateTime;
import java.util.Map;
import java.util.Set;

@TuskConfig
public class AdvancedTypesConfig {
    // Basic types
    public String appName;
    public int port;
    public boolean debug;
    
    // Collections
    public List<String> features;
    public Set<String> uniqueFeatures;
    public Map<String, Object> settings;
    
    // Custom types
    public LocalDateTime createdAt;
    public DatabaseConfig database;
    public List<ServerConfig> servers;
    
    // Optional types
    public String optionalField;
    public Integer nullableInt;
    public Boolean nullableBoolean;
}

@TuskConfig
public class DatabaseConfig {
    public String host;
    public int port;
    public String name;
    public PoolConfig pool;
}

@TuskConfig
public class PoolConfig {
    public int minSize;
    public int maxSize;
    public int timeout;
}

@TuskConfig
public class ServerConfig {
    public String name;
    public String host;
    public int port;
    public boolean ssl;
}

public class AdvancedTypesExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        AdvancedTypesConfig config = parser.parseFile("config.tsk", AdvancedTypesConfig.class);
        
        // Access typed values
        System.out.println("App: " + config.appName);
        System.out.println("Port: " + config.port);
        System.out.println("Debug: " + config.debug);
        
        // Access collections
        System.out.println("Features: " + config.features);
        System.out.println("Settings: " + config.settings);
        
        // Access nested objects
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Pool size: " + config.database.pool.minSize + "-" + config.database.pool.maxSize);
        
        // Access lists of objects
        for (ServerConfig server : config.servers) {
            System.out.println("Server: " + server.name + " at " + server.host + ":" + server.port);
        }
    }
}
```

## 🔒 Type Safety Best Practices

### Null Safety

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Optional;

@TuskConfig
public class NullSafeConfig {
    public String requiredField;
    public String optionalField;
    public Integer nullableInt;
    public Boolean nullableBoolean;
    
    // Use Optional for nullable fields
    public Optional<String> getOptionalField() {
        return Optional.ofNullable(optionalField);
    }
    
    public Optional<Integer> getNullableInt() {
        return Optional.ofNullable(nullableInt);
    }
}

public class NullSafetyExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        NullSafeConfig config = parser.parseFile("config.tsk", NullSafeConfig.class);
        
        // Safe access to nullable fields
        config.getOptionalField().ifPresent(value -> 
            System.out.println("Optional field: " + value)
        );
        
        config.getNullableInt().ifPresent(value -> 
            System.out.println("Nullable int: " + value)
        );
        
        // Default values for nullable fields
        String optionalValue = config.getOptionalField().orElse("default");
        int nullableValue = config.getNullableInt().orElse(0);
        
        System.out.println("Optional with default: " + optionalValue);
        System.out.println("Nullable with default: " + nullableValue);
    }
}
```

### Type Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.List;

@TuskConfig
public class ValidatedConfig {
    public String appName;
    public int port;
    public List<String> features;
    
    // Validation methods
    public boolean isValid() {
        return appName != null && !appName.isEmpty() &&
               port > 0 && port <= 65535 &&
               features != null && !features.isEmpty();
    }
    
    public List<String> getValidationErrors() {
        List<String> errors = new ArrayList<>();
        
        if (appName == null || appName.isEmpty()) {
            errors.add("App name is required");
        }
        
        if (port <= 0 || port > 65535) {
            errors.add("Port must be between 1 and 65535");
        }
        
        if (features == null || features.isEmpty()) {
            errors.add("At least one feature is required");
        }
        
        return errors;
    }
}

public class ValidationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ValidatedConfig config = parser.parseFile("config.tsk", ValidatedConfig.class);
        
        if (config.isValid()) {
            System.out.println("Configuration is valid");
        } else {
            System.out.println("Configuration errors:");
            config.getValidationErrors().forEach(System.err::println);
        }
    }
}
```

## 🧪 Testing Type Handling

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import java.util.Map;

class TypeHandlingTest {
    
    private TuskLang parser;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
    }
    
    @Test
    void testStringTypes() {
        String tskContent = """
            [test]
            string_value: "Hello, World!"
            empty_string: ""
            null_string: null
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        assertEquals("Hello, World!", config.get("test"));
        assertEquals("", config.get("test"));
        assertNull(config.get("test"));
    }
    
    @Test
    void testNumericTypes() {
        String tskContent = """
            [test]
            int_value: 42
            long_value: 9223372036854775807
            double_value: 3.14159
            float_value: 2.71828
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        assertEquals(42, config.get("test"));
        assertEquals(9223372036854775807L, config.get("test"));
        assertEquals(3.14159, config.get("test"));
        assertEquals(2.71828f, config.get("test"));
    }
    
    @Test
    void testBooleanTypes() {
        String tskContent = """
            [test]
            true_value: true
            false_value: false
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        assertTrue((Boolean) config.get("test"));
        assertFalse((Boolean) config.get("test"));
    }
    
    @Test
    void testArrayTypes() {
        String tskContent = """
            [test]
            string_array: ["one", "two", "three"]
            int_array: [1, 2, 3, 4, 5]
            mixed_array: ["text", 42, true, null]
            """;
        
        Map<String, Object> config = parser.parse(tskContent);
        
        List<String> stringArray = (List<String>) config.get("test");
        assertEquals(3, stringArray.size());
        assertEquals("one", stringArray.get(0));
        
        List<Integer> intArray = (List<Integer>) config.get("test");
        assertEquals(5, intArray.size());
        assertEquals(1, intArray.get(0));
    }
}
```

## 🔧 Troubleshooting

### Common Type Issues

1. **Type Mismatch**
```java
// Error: String to int conversion
String portString = "8080";  // String
int port = Integer.parseInt(portString);  // Convert to int

// In TSK file, use number directly
port: 8080  // Integer, not "8080"
```

2. **Null Handling**
```java
// Safe null handling
Object value = config.get("optional_field");
if (value != null) {
    String stringValue = value.toString();
    // Use stringValue
}
```

3. **Array Type Issues**
```java
// Cast to correct type
List<Object> mixedArray = (List<Object>) config.get("mixed_array");
for (Object item : mixedArray) {
    if (item instanceof String) {
        String stringItem = (String) item;
    } else if (item instanceof Integer) {
        Integer intItem = (Integer) item;
    }
}
```

## 📚 Next Steps

1. **Master type mapping** - Understand all TuskLang to Java type conversions
2. **Implement validation** - Add type validation and error handling
3. **Use type-safe classes** - Leverage @TuskConfig annotation for type safety
4. **Handle null values** - Implement proper null safety patterns
5. **Test type handling** - Create comprehensive type testing

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang data types in Java! From primitive types to complex nested structures, you can build type-safe, robust configurations with comprehensive validation and error handling. 
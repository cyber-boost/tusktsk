# ☕ TuskLang Java Validation Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang validation in Java with comprehensive coverage of validation syntax, Java integration patterns, custom validators, and best practices for ensuring configuration integrity and data quality.

## 🎯 Validation Basics

### Built-in Validation

```tsk
# Basic validation examples
app_name: @validate.required("app_name")
version: @validate.pattern("version", "^\\d+\\.\\d+\\.\\d+$")
port: @validate.range("port", 1, 65535)
email: @validate.email("user_email")
password: @validate.password("user_password", {
    "min_length": 8,
    "require_uppercase": true,
    "require_lowercase": true,
    "require_numbers": true,
    "require_special": true
})

# Database validation
[database]
host: @validate.required("database.host")
port: @validate.range("database.port", 1, 65535)
name: @validate.required("database.name")
user: @validate.required("database.user")
password: @validate.required("database.password")
```

### Java Validation Integration

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.validation.ValidationResult;
import java.util.Map;

@TuskConfig
public class ValidatedConfig {
    public String appName;
    public String version;
    public int port;
    public String email;
    public String password;
    public DatabaseConfig database;
}

@TuskConfig
public class DatabaseConfig {
    public String host;
    public int port;
    public String name;
    public String user;
    public String password;
}

public class ValidationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Parse with validation
        ValidationResult result = parser.parseWithValidation("config.tsk", ValidatedConfig.class);
        
        if (result.isValid()) {
            ValidatedConfig config = result.getConfig();
            System.out.println("Configuration is valid");
            System.out.println("App: " + config.appName + " v" + config.version);
            System.out.println("Port: " + config.port);
            System.out.println("Database: " + config.database.host + ":" + config.database.port);
        } else {
            System.out.println("Validation errors:");
            for (String error : result.getErrors()) {
                System.err.println("  " + error);
            }
        }
    }
}
```

## 🔧 Validation Types

### Required Field Validation

```tsk
# Required field validation
app_name: @validate.required("app_name")
version: @validate.required("version")
debug: @validate.required("debug")

[database]
host: @validate.required("database.host")
port: @validate.required("database.port")
name: @validate.required("database.name")
user: @validate.required("database.user")
password: @validate.required("database.password")

[server]
host: @validate.required("server.host")
port: @validate.required("server.port")
ssl: @validate.required("server.ssl")
```

### Type Validation

```tsk
# Type validation
port: @validate.type("port", "integer")
timeout: @validate.type("timeout", "number")
ssl_enabled: @validate.type("ssl_enabled", "boolean")
host: @validate.type("host", "string")
max_connections: @validate.type("max_connections", "integer")
```

### Range Validation

```tsk
# Numeric range validation
port: @validate.range("port", 1, 65535)
timeout: @validate.range("timeout", 1, 3600)
max_connections: @validate.range("max_connections", 1, 1000)
pool_size: @validate.range("pool_size", 1, 100)

# String length validation
app_name: @validate.length("app_name", 1, 100)
description: @validate.length("description", 0, 500)
password: @validate.length("password", 8, 128)
```

### Pattern Validation

```tsk
# Pattern validation with regex
version: @validate.pattern("version", "^\\d+\\.\\d+\\.\\d+$")
email: @validate.pattern("email", "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")
phone: @validate.pattern("phone", "^\\+?[1-9]\\d{1,14}$")
url: @validate.pattern("url", "^https?://[^\\s/$.?#].[^\\s]*$")
```

### Email Validation

```tsk
# Email validation
user_email: @validate.email("user_email")
admin_email: @validate.email("admin_email")
support_email: @validate.email("support_email")

# Email with custom options
primary_email: @validate.email("primary_email", {
    "allow_local": false,
    "check_mx": true,
    "check_disposable": true
})
```

### Password Validation

```tsk
# Password validation with options
user_password: @validate.password("user_password", {
    "min_length": 8,
    "max_length": 128,
    "require_uppercase": true,
    "require_lowercase": true,
    "require_numbers": true,
    "require_special": true,
    "forbidden_words": ["password", "123456", "qwerty"]
})

# Simple password validation
simple_password: @validate.password("simple_password", {
    "min_length": 6
})
```

### URL Validation

```tsk
# URL validation
api_url: @validate.url("api_url")
webhook_url: @validate.url("webhook_url")
callback_url: @validate.url("callback_url")

# URL with protocols
https_url: @validate.url("https_url", {
    "allowed_protocols": ["https"]
})

# URL with custom options
secure_url: @validate.url("secure_url", {
    "allowed_protocols": ["https"],
    "require_path": true,
    "allow_query": true,
    "allow_fragment": false
})
```

## 🔄 Custom Validation

### Custom Validation Functions

```tsk
# Custom validation using FUJSEN
port: @validate.custom("port", """
function validatePort(value) {
    if (value < 1 || value > 65535) {
        return "Port must be between 1 and 65535";
    }
    if (value < 1024) {
        return "Port " + value + " requires root privileges";
    }
    return null; // null means valid
}
""")

# Custom validation for database connection
database_url: @validate.custom("database_url", """
function validateDatabaseUrl(value) {
    if (!value.startsWith("postgresql://")) {
        return "Database URL must start with postgresql://";
    }
    if (!value.includes("@")) {
        return "Database URL must include credentials";
    }
    return null;
}
""")
```

### Java Custom Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.CustomValidator;
import org.tusklang.java.validation.ValidationRule;
import java.util.Map;

public class CustomValidationExample {
    public static void main(String[] args) {
        // Create custom validator
        CustomValidator validator = new CustomValidator();
        
        // Add custom validation rules
        validator.addRule(new ValidationRule("port", value -> {
            int port = Integer.parseInt(value.toString());
            if (port < 1 || port > 65535) {
                return "Port must be between 1 and 65535";
            }
            if (port < 1024) {
                return "Port " + port + " requires root privileges";
            }
            return null; // null means valid
        }));
        
        validator.addRule(new ValidationRule("database_url", value -> {
            String url = value.toString();
            if (!url.startsWith("postgresql://")) {
                return "Database URL must start with postgresql://";
            }
            if (!url.contains("@")) {
                return "Database URL must include credentials";
            }
            return null;
        }));
        
        // Add validation for environment-specific rules
        validator.addRule(new ValidationRule("production_password", value -> {
            String password = value.toString();
            if (System.getProperty("ENVIRONMENT", "").equals("production")) {
                if (password.length() < 12) {
                    return "Production passwords must be at least 12 characters";
                }
                if (!password.matches(".*[A-Z].*")) {
                    return "Production passwords must contain uppercase letters";
                }
                if (!password.matches(".*[a-z].*")) {
                    return "Production passwords must contain lowercase letters";
                }
                if (!password.matches(".*\\d.*")) {
                    return "Production passwords must contain numbers";
                }
                if (!password.matches(".*[!@#$%^&*].*")) {
                    return "Production passwords must contain special characters";
                }
            }
            return null;
        }));
        
        TuskLang parser = new TuskLang();
        parser.setCustomValidator(validator);
        
        // Parse with custom validation
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Configuration validated with custom rules");
    }
}
```

## 📊 Complex Validation

### Cross-Field Validation

```tsk
# Cross-field validation
[database]
host: "localhost"
port: 5432
ssl_enabled: true

# Validate SSL configuration when SSL is enabled
ssl_cert_file: @validate.conditional("ssl_cert_file", {
    "condition": "$database.ssl_enabled == true",
    "rule": "required",
    "message": "SSL certificate file is required when SSL is enabled"
})

ssl_key_file: @validate.conditional("ssl_key_file", {
    "condition": "$database.ssl_enabled == true",
    "rule": "required",
    "message": "SSL key file is required when SSL is enabled"
})
```

### Environment-Specific Validation

```tsk
# Environment-specific validation
environment: "production"

# Production-specific validations
production_password: @validate.conditional("production_password", {
    "condition": "$environment == 'production'",
    "rule": "password",
    "options": {
        "min_length": 12,
        "require_uppercase": true,
        "require_lowercase": true,
        "require_numbers": true,
        "require_special": true
    }
})

production_url: @validate.conditional("production_url", {
    "condition": "$environment == 'production'",
    "rule": "url",
    "options": {
        "allowed_protocols": ["https"]
    }
})
```

### Java Complex Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.ComplexValidator;
import org.tusklang.java.validation.CrossFieldRule;
import java.util.Map;

public class ComplexValidationExample {
    public static void main(String[] args) {
        ComplexValidator validator = new ComplexValidator();
        
        // Add cross-field validation
        validator.addCrossFieldRule(new CrossFieldRule(
            "ssl_cert_file",
            config -> {
                Boolean sslEnabled = (Boolean) config.get("database");
                if (sslEnabled != null && sslEnabled) {
                    String certFile = (String) config.get("ssl_cert_file");
                    if (certFile == null || certFile.isEmpty()) {
                        return "SSL certificate file is required when SSL is enabled";
                    }
                }
                return null;
            }
        ));
        
        validator.addCrossFieldRule(new CrossFieldRule(
            "production_password",
            config -> {
                String environment = (String) config.get("environment");
                if ("production".equals(environment)) {
                    String password = (String) config.get("production_password");
                    if (password != null && password.length() < 12) {
                        return "Production passwords must be at least 12 characters";
                    }
                }
                return null;
            }
        ));
        
        TuskLang parser = new TuskLang();
        parser.setComplexValidator(validator);
        
        // Parse with complex validation
        Map<String, Object> config = parser.parseFile("config.tsk");
        System.out.println("Configuration validated with complex rules");
    }
}
```

## 🔧 Validation Utilities

### Validation Result Handling

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.ValidationResult;
import org.tusklang.java.validation.ValidationError;
import java.util.List;

public class ValidationResultExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ValidationResult result = parser.parseWithValidation("config.tsk", ValidatedConfig.class);
        
        if (result.isValid()) {
            System.out.println("✅ Configuration is valid");
            ValidatedConfig config = result.getConfig();
            // Use the validated configuration
        } else {
            System.out.println("❌ Configuration has validation errors:");
            
            List<ValidationError> errors = result.getValidationErrors();
            for (ValidationError error : errors) {
                System.err.println("  " + error.getField() + ": " + error.getMessage());
                System.err.println("    Value: " + error.getValue());
                System.err.println("    Rule: " + error.getRule());
            }
            
            // Get errors by field
            List<ValidationError> portErrors = result.getErrorsForField("port");
            List<ValidationError> emailErrors = result.getErrorsForField("email");
            
            // Get summary
            Map<String, Integer> errorSummary = result.getErrorSummary();
            System.out.println("Error summary: " + errorSummary);
        }
    }
}
```

### Validation Configuration

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.ValidationConfig;
import java.util.Map;

public class ValidationConfigExample {
    public static void main(String[] args) {
        // Configure validation behavior
        ValidationConfig config = new ValidationConfig();
        config.setStopOnFirstError(false);  // Continue validation after first error
        config.setValidateImports(true);    // Validate imported files
        config.setValidateVariables(true);  // Validate variable references
        config.setStrictMode(true);         // Enable strict validation mode
        
        TuskLang parser = new TuskLang();
        parser.setValidationConfig(config);
        
        // Parse with custom validation configuration
        Map<String, Object> result = parser.parseFile("config.tsk");
        System.out.println("Configuration parsed with custom validation settings");
    }
}
```

## 🧪 Testing Validation

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.ValidationResult;
import org.tusklang.java.validation.CustomValidator;

class ValidationTest {
    
    private TuskLang parser;
    private CustomValidator validator;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
        validator = new CustomValidator();
        parser.setCustomValidator(validator);
    }
    
    @Test
    void testRequiredFieldValidation() {
        String tskContent = """
            # Missing required field
            version: "1.0.0"
            port: 8080
            """;
        
        ValidationResult result = parser.parseWithValidation(tskContent, ValidatedConfig.class);
        
        assertFalse(result.isValid());
        assertTrue(result.getErrors().contains("app_name is required"));
    }
    
    @Test
    void testRangeValidation() {
        String tskContent = """
            app_name: "Test App"
            version: "1.0.0"
            port: 70000  # Invalid port
            """;
        
        ValidationResult result = parser.parseWithValidation(tskContent, ValidatedConfig.class);
        
        assertFalse(result.isValid());
        assertTrue(result.getErrors().stream()
            .anyMatch(error -> error.contains("Port must be between 1 and 65535")));
    }
    
    @Test
    void testEmailValidation() {
        String tskContent = """
            app_name: "Test App"
            version: "1.0.0"
            port: 8080
            email: "invalid-email"  # Invalid email
            """;
        
        ValidationResult result = parser.parseWithValidation(tskContent, ValidatedConfig.class);
        
        assertFalse(result.isValid());
        assertTrue(result.getErrors().stream()
            .anyMatch(error -> error.contains("Invalid email format")));
    }
    
    @Test
    void testCustomValidation() {
        // Add custom validation rule
        validator.addRule(new ValidationRule("port", value -> {
            int port = Integer.parseInt(value.toString());
            if (port < 1024) {
                return "Port " + port + " requires root privileges";
            }
            return null;
        }));
        
        String tskContent = """
            app_name: "Test App"
            version: "1.0.0"
            port: 80  # Requires root privileges
            """;
        
        ValidationResult result = parser.parseWithValidation(tskContent, ValidatedConfig.class);
        
        assertFalse(result.isValid());
        assertTrue(result.getErrors().stream()
            .anyMatch(error -> error.contains("requires root privileges")));
    }
    
    @Test
    void testValidConfiguration() {
        String tskContent = """
            app_name: "Test App"
            version: "1.0.0"
            port: 8080
            email: "test@example.com"
            password: "SecurePass123!"
            
            [database]
            host: "localhost"
            port: 5432
            name: "testdb"
            user: "postgres"
            password: "secret"
            """;
        
        ValidationResult result = parser.parseWithValidation(tskContent, ValidatedConfig.class);
        
        assertTrue(result.isValid());
        assertNotNull(result.getConfig());
        assertEquals("Test App", result.getConfig().appName);
        assertEquals(8080, result.getConfig().port);
    }
}
```

## 🔧 Troubleshooting

### Common Validation Issues

1. **Validation Not Running**
```java
// Ensure validation is enabled
TuskLang parser = new TuskLang();
parser.setValidationEnabled(true);  // Enable validation

ValidationResult result = parser.parseWithValidation("config.tsk", ValidatedConfig.class);
```

2. **Custom Validation Not Working**
```java
// Ensure custom validator is set before parsing
CustomValidator validator = new CustomValidator();
validator.addRule(new ValidationRule("field", value -> {
    // Validation logic
    return null;
}));

TuskLang parser = new TuskLang();
parser.setCustomValidator(validator);  // Set before parsing

Map<String, Object> config = parser.parseFile("config.tsk");
```

3. **Cross-Field Validation Issues**
```tsk
# Ensure variables are defined before cross-field validation
ssl_enabled: true
ssl_cert_file: @validate.conditional("ssl_cert_file", {
    "condition": "$ssl_enabled == true",  # Variable must be defined
    "rule": "required"
})
```

## 📚 Best Practices

### Validation Strategy

1. **Validate early and often**
```java
// Validate at configuration load time
ValidationResult result = parser.parseWithValidation("config.tsk", ValidatedConfig.class);
if (!result.isValid()) {
    // Handle validation errors immediately
    throw new ConfigurationException("Invalid configuration: " + result.getErrors());
}
```

2. **Use appropriate validation rules**
```tsk
# Use specific validation rules
email: @validate.email("email")  # Better than pattern validation
password: @validate.password("password", {"min_length": 8})  # Better than length validation
url: @validate.url("url")  # Better than pattern validation
```

3. **Provide clear error messages**
```tsk
# Clear, actionable error messages
port: @validate.range("port", 1, 65535, "Port must be between 1 and 65535")
email: @validate.email("email", "Please provide a valid email address")
password: @validate.password("password", {
    "min_length": 8,
    "message": "Password must be at least 8 characters long"
})
```

### Validation Organization

1. **Group related validations**
```tsk
# Group database validations
[database]
host: @validate.required("database.host")
port: @validate.range("database.port", 1, 65535)
name: @validate.required("database.name")
user: @validate.required("database.user")
password: @validate.required("database.password")
```

2. **Use environment-specific validation**
```tsk
# Environment-specific validation
production_password: @validate.conditional("production_password", {
    "condition": "$environment == 'production'",
    "rule": "password",
    "options": {"min_length": 12}
})
```

## 📚 Next Steps

1. **Master validation syntax** - Understand all validation types and options
2. **Implement custom validators** - Create domain-specific validation rules
3. **Use cross-field validation** - Validate relationships between fields
4. **Add validation testing** - Create comprehensive validation test suites
5. **Optimize validation performance** - Use efficient validation strategies

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang validation in Java! From basic validation to complex cross-field rules, you can ensure configuration integrity and data quality with comprehensive validation systems. 
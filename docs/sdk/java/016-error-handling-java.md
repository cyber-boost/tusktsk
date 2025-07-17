# ☕ TuskLang Java Error Handling Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang error handling in Java with comprehensive coverage of error types, Java integration patterns, exception handling, and best practices for robust configuration management.

## 🎯 Error Handling Basics

### Common Error Types

```tsk
# Syntax errors
app_name: "My App"  # Missing closing quote
version: 1.0.0      # Missing quotes for string
port: "8080"        # String instead of number

# Reference errors
database_host: $undefined_variable
api_url: $base_url + "/" + $missing_var

# Validation errors
email: "invalid-email"
port: 70000  # Out of range
password: "123"  # Too short
```

### Java Error Handling

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.errors.TuskLangException;
import org.tusklang.java.errors.ParseException;
import org.tusklang.java.errors.ValidationException;
import java.util.Map;

@TuskConfig
public class ErrorHandlingConfig {
    public String appName;
    public String version;
    public int port;
    public String email;
    public String password;
}

public class ErrorHandlingExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        try {
            // Parse configuration with error handling
            ErrorHandlingConfig config = parser.parseFile("config.tsk", ErrorHandlingConfig.class);
            System.out.println("Configuration loaded successfully");
            System.out.println("App: " + config.appName + " v" + config.version);
            
        } catch (ParseException e) {
            System.err.println("Parse error: " + e.getMessage());
            System.err.println("Line: " + e.getLineNumber());
            System.err.println("Column: " + e.getColumnNumber());
            System.err.println("Context: " + e.getContext());
            
        } catch (ValidationException e) {
            System.err.println("Validation error: " + e.getMessage());
            System.err.println("Field: " + e.getField());
            System.err.println("Value: " + e.getValue());
            System.err.println("Rule: " + e.getRule());
            
        } catch (TuskLangException e) {
            System.err.println("TuskLang error: " + e.getMessage());
            System.err.println("Error type: " + e.getErrorType());
            System.err.println("File: " + e.getFileName());
            
        } catch (Exception e) {
            System.err.println("Unexpected error: " + e.getMessage());
            e.printStackTrace();
        }
    }
}
```

## 🔧 Error Types and Handling

### Parse Errors

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.ParseException;
import java.util.Map;

public class ParseErrorHandling {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        try {
            // Parse with potential syntax errors
            Map<String, Object> config = parser.parseFile("config.tsk");
            
        } catch (ParseException e) {
            System.err.println("=== Parse Error ===");
            System.err.println("Message: " + e.getMessage());
            System.err.println("Line: " + e.getLineNumber());
            System.err.println("Column: " + e.getColumnNumber());
            System.err.println("Context: " + e.getContext());
            System.err.println("File: " + e.getFileName());
            
            // Get error details
            String errorLine = e.getErrorLine();
            String suggestion = e.getSuggestion();
            
            if (errorLine != null) {
                System.err.println("Error line: " + errorLine);
            }
            
            if (suggestion != null) {
                System.err.println("Suggestion: " + suggestion);
            }
            
            // Handle specific parse error types
            switch (e.getParseErrorType()) {
                case SYNTAX_ERROR:
                    System.err.println("Syntax error detected");
                    break;
                case MISSING_QUOTE:
                    System.err.println("Missing quote detected");
                    break;
                case INVALID_SECTION:
                    System.err.println("Invalid section syntax");
                    break;
                case UNEXPECTED_TOKEN:
                    System.err.println("Unexpected token");
                    break;
            }
        }
    }
}
```

### Validation Errors

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.ValidationException;
import org.tusklang.java.validation.ValidationResult;
import java.util.List;

public class ValidationErrorHandling {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        try {
            // Parse with validation
            ValidationResult result = parser.parseWithValidation("config.tsk", ErrorHandlingConfig.class);
            
            if (!result.isValid()) {
                System.err.println("=== Validation Errors ===");
                
                List<ValidationException> errors = result.getValidationExceptions();
                for (ValidationException error : errors) {
                    System.err.println("Field: " + error.getField());
                    System.err.println("Message: " + error.getMessage());
                    System.err.println("Value: " + error.getValue());
                    System.err.println("Rule: " + error.getRule());
                    System.err.println("---");
                }
                
                // Get error summary
                Map<String, Integer> errorSummary = result.getErrorSummary();
                System.err.println("Error summary: " + errorSummary);
                
                // Get errors by field
                List<ValidationException> portErrors = result.getValidationExceptionsForField("port");
                List<ValidationException> emailErrors = result.getValidationExceptionsForField("email");
                
                if (!portErrors.isEmpty()) {
                    System.err.println("Port errors: " + portErrors.size());
                }
                
                if (!emailErrors.isEmpty()) {
                    System.err.println("Email errors: " + emailErrors.size());
                }
            }
            
        } catch (ValidationException e) {
            System.err.println("=== Single Validation Error ===");
            System.err.println("Field: " + e.getField());
            System.err.println("Message: " + e.getMessage());
            System.err.println("Value: " + e.getValue());
            System.err.println("Rule: " + e.getRule());
            System.err.println("Severity: " + e.getSeverity());
        }
    }
}
```

### Reference Errors

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.ReferenceException;
import java.util.Map;

public class ReferenceErrorHandling {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        try {
            // Parse with potential reference errors
            Map<String, Object> config = parser.parseFile("config.tsk");
            
        } catch (ReferenceException e) {
            System.err.println("=== Reference Error ===");
            System.err.println("Message: " + e.getMessage());
            System.err.println("Variable: " + e.getVariableName());
            System.err.println("Context: " + e.getContext());
            System.err.println("Line: " + e.getLineNumber());
            
            // Handle specific reference error types
            switch (e.getReferenceErrorType()) {
                case UNDEFINED_VARIABLE:
                    System.err.println("Undefined variable: " + e.getVariableName());
                    System.err.println("Available variables: " + e.getAvailableVariables());
                    break;
                case CIRCULAR_REFERENCE:
                    System.err.println("Circular reference detected");
                    System.err.println("Reference chain: " + e.getReferenceChain());
                    break;
                case INVALID_REFERENCE:
                    System.err.println("Invalid reference syntax");
                    break;
            }
        }
    }
}
```

## 🔄 Exception Handling Patterns

### Try-Catch with Recovery

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.errors.TuskLangException;
import java.util.Map;

@TuskConfig
public class RecoveryConfig {
    public String appName;
    public String version;
    public int port;
    public DatabaseConfig database;
}

public class ErrorRecoveryExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        RecoveryConfig config = null;
        
        // Try to load primary configuration
        try {
            config = parser.parseFile("config.tsk", RecoveryConfig.class);
            System.out.println("Primary configuration loaded successfully");
            
        } catch (TuskLangException e) {
            System.err.println("Primary configuration failed: " + e.getMessage());
            
            // Try to load fallback configuration
            try {
                config = parser.parseFile("config.fallback.tsk", RecoveryConfig.class);
                System.out.println("Fallback configuration loaded successfully");
                
            } catch (TuskLangException fallbackError) {
                System.err.println("Fallback configuration also failed: " + fallbackError.getMessage());
                
                // Use default configuration
                config = createDefaultConfig();
                System.out.println("Using default configuration");
            }
        }
        
        // Use the configuration (either primary, fallback, or default)
        if (config != null) {
            System.out.println("App: " + config.appName + " v" + config.version);
            System.out.println("Port: " + config.port);
        }
    }
    
    private static RecoveryConfig createDefaultConfig() {
        RecoveryConfig config = new RecoveryConfig();
        config.appName = "Default App";
        config.version = "1.0.0";
        config.port = 8080;
        return config;
    }
}
```

### Error Collection and Reporting

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.ErrorCollector;
import org.tusklang.java.errors.TuskLangException;
import java.util.List;

public class ErrorCollectionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ErrorCollector errorCollector = new ErrorCollector();
        
        // Set up error collector
        parser.setErrorCollector(errorCollector);
        
        try {
            // Parse configuration (errors will be collected)
            Map<String, Object> config = parser.parseFile("config.tsk");
            
        } catch (TuskLangException e) {
            // Handle any uncaught errors
            errorCollector.addError(e);
        }
        
        // Report all collected errors
        List<TuskLangException> errors = errorCollector.getErrors();
        
        if (!errors.isEmpty()) {
            System.err.println("=== Error Report ===");
            System.err.println("Total errors: " + errors.size());
            
            for (TuskLangException error : errors) {
                System.err.println("Error: " + error.getMessage());
                System.err.println("Type: " + error.getClass().getSimpleName());
                System.err.println("File: " + error.getFileName());
                System.err.println("Line: " + error.getLineNumber());
                System.err.println("---");
            }
            
            // Generate error summary
            Map<String, Integer> errorSummary = errorCollector.getErrorSummary();
            System.err.println("Error summary: " + errorSummary);
            
            // Check if configuration is usable despite errors
            if (errorCollector.hasCriticalErrors()) {
                System.err.println("Critical errors detected - configuration may not be usable");
            } else {
                System.out.println("Configuration loaded with warnings");
            }
        } else {
            System.out.println("Configuration loaded successfully - no errors");
        }
    }
}
```

### Graceful Degradation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.errors.TuskLangException;
import java.util.Map;
import java.util.Optional;

@TuskConfig
public class GracefulConfig {
    public String appName;
    public String version;
    public Optional<Integer> port;
    public Optional<DatabaseConfig> database;
}

public class GracefulDegradationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        try {
            GracefulConfig config = parser.parseFile("config.tsk", GracefulConfig.class);
            
            // Use configuration with graceful degradation
            System.out.println("App: " + config.appName);
            System.out.println("Version: " + config.version);
            
            // Handle optional fields gracefully
            int port = config.port.orElse(8080);
            System.out.println("Port: " + port);
            
            if (config.database.isPresent()) {
                DatabaseConfig db = config.database.get();
                System.out.println("Database: " + db.host + ":" + db.port);
            } else {
                System.out.println("Database configuration not available - using defaults");
                // Use default database configuration
            }
            
        } catch (TuskLangException e) {
            System.err.println("Configuration error: " + e.getMessage());
            System.err.println("Using minimal configuration");
            
            // Create minimal working configuration
            GracefulConfig minimalConfig = createMinimalConfig();
            System.out.println("Minimal app: " + minimalConfig.appName);
        }
    }
    
    private static GracefulConfig createMinimalConfig() {
        GracefulConfig config = new GracefulConfig();
        config.appName = "Minimal App";
        config.version = "1.0.0";
        config.port = Optional.empty();
        config.database = Optional.empty();
        return config;
    }
}
```

## 🔧 Error Utilities

### Error Analysis

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.ErrorAnalyzer;
import org.tusklang.java.errors.TuskLangException;
import java.util.List;
import java.util.Map;

public class ErrorAnalysisExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ErrorAnalyzer analyzer = new ErrorAnalyzer();
        
        try {
            parser.parseFile("config.tsk");
            
        } catch (TuskLangException e) {
            // Analyze the error
            Map<String, Object> analysis = analyzer.analyzeError(e);
            
            System.out.println("=== Error Analysis ===");
            System.out.println("Error type: " + analysis.get("errorType"));
            System.out.println("Severity: " + analysis.get("severity"));
            System.out.println("Recoverable: " + analysis.get("recoverable"));
            System.out.println("Common cause: " + analysis.get("commonCause"));
            System.out.println("Suggested fix: " + analysis.get("suggestedFix"));
            
            // Get similar errors
            List<TuskLangException> similarErrors = analyzer.findSimilarErrors(e);
            if (!similarErrors.isEmpty()) {
                System.out.println("Similar errors found: " + similarErrors.size());
            }
            
            // Get error patterns
            Map<String, Integer> errorPatterns = analyzer.getErrorPatterns();
            System.out.println("Error patterns: " + errorPatterns);
        }
    }
}
```

### Error Recovery

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.ErrorRecovery;
import org.tusklang.java.errors.TuskLangException;
import java.util.Map;

public class ErrorRecoveryExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ErrorRecovery recovery = new ErrorRecovery();
        
        try {
            parser.parseFile("config.tsk");
            
        } catch (TuskLangException e) {
            // Attempt automatic recovery
            String recoveredContent = recovery.attemptRecovery(e);
            
            if (recoveredContent != null) {
                System.out.println("=== Automatic Recovery ===");
                System.out.println("Recovery successful");
                System.out.println("Recovered content:");
                System.out.println(recoveredContent);
                
                // Try to parse recovered content
                try {
                    Map<String, Object> config = parser.parse(recoveredContent);
                    System.out.println("Recovered configuration parsed successfully");
                    
                } catch (TuskLangException recoveryError) {
                    System.err.println("Recovery failed: " + recoveryError.getMessage());
                }
                
            } else {
                System.err.println("Automatic recovery not possible");
                System.err.println("Manual intervention required");
                
                // Provide manual recovery suggestions
                List<String> suggestions = recovery.getRecoverySuggestions(e);
                System.out.println("Recovery suggestions:");
                for (String suggestion : suggestions) {
                    System.out.println("  - " + suggestion);
                }
            }
        }
    }
}
```

## 🧪 Testing Error Handling

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import org.tusklang.java.errors.ParseException;
import org.tusklang.java.errors.ValidationException;
import org.tusklang.java.errors.ReferenceException;

class ErrorHandlingTest {
    
    private TuskLang parser;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
    }
    
    @Test
    void testParseErrorHandling() {
        String invalidContent = """
            app_name: "My App"  # Missing closing quote
            version: 1.0.0      # Missing quotes
            port: "8080"        # String instead of number
            """;
        
        ParseException exception = assertThrows(ParseException.class, () -> {
            parser.parse(invalidContent);
        });
        
        assertEquals("Missing closing quote", exception.getMessage());
        assertTrue(exception.getLineNumber() > 0);
        assertTrue(exception.getColumnNumber() > 0);
    }
    
    @Test
    void testValidationErrorHandling() {
        String invalidContent = """
            app_name: "Test App"
            version: "1.0.0"
            port: 70000  # Invalid port
            email: "invalid-email"  # Invalid email
            """;
        
        ValidationException exception = assertThrows(ValidationException.class, () -> {
            parser.parseWithValidation(invalidContent, ErrorHandlingConfig.class);
        });
        
        assertTrue(exception.getMessage().contains("Port must be between 1 and 65535"));
        assertEquals("port", exception.getField());
        assertEquals(70000, exception.getValue());
    }
    
    @Test
    void testReferenceErrorHandling() {
        String invalidContent = """
            app_name: "Test App"
            version: "1.0.0"
            port: $undefined_variable
            """;
        
        ReferenceException exception = assertThrows(ReferenceException.class, () -> {
            parser.parse(invalidContent);
        });
        
        assertEquals("undefined_variable", exception.getVariableName());
        assertTrue(exception.getMessage().contains("Undefined variable"));
    }
    
    @Test
    void testErrorRecovery() {
        String invalidContent = """
            app_name: "Test App"  # Missing closing quote
            version: "1.0.0"
            port: 8080
            """;
        
        try {
            parser.parse(invalidContent);
            fail("Should have thrown ParseException");
            
        } catch (ParseException e) {
            // Test error recovery
            ErrorRecovery recovery = new ErrorRecovery();
            String recoveredContent = recovery.attemptRecovery(e);
            
            assertNotNull(recoveredContent);
            assertTrue(recoveredContent.contains("Test App"));
            assertTrue(recoveredContent.contains("1.0.0"));
            assertTrue(recoveredContent.contains("8080"));
            
            // Verify recovered content can be parsed
            Map<String, Object> config = parser.parse(recoveredContent);
            assertEquals("Test App", config.get("app_name"));
            assertEquals("1.0.0", config.get("version"));
            assertEquals(8080, config.get("port"));
        }
    }
}
```

## 🔧 Troubleshooting

### Common Error Handling Issues

1. **Errors Not Caught**
```java
// Ensure proper exception hierarchy
try {
    config = parser.parseFile("config.tsk", Config.class);
} catch (TuskLangException e) {
    // Catches all TuskLang-specific exceptions
    System.err.println("TuskLang error: " + e.getMessage());
} catch (Exception e) {
    // Catches any other exceptions
    System.err.println("Unexpected error: " + e.getMessage());
}
```

2. **Error Recovery Not Working**
```java
// Ensure error recovery is properly configured
ErrorRecovery recovery = new ErrorRecovery();
recovery.setEnableAutoFix(true);
recovery.setMaxRecoveryAttempts(3);

String recoveredContent = recovery.attemptRecovery(exception);
```

3. **Error Analysis Issues**
```java
// Ensure error analyzer has proper context
ErrorAnalyzer analyzer = new ErrorAnalyzer();
analyzer.setIncludeContext(true);
analyzer.setIncludeSuggestions(true);

Map<String, Object> analysis = analyzer.analyzeError(exception);
```

## 📚 Best Practices

### Error Handling Strategy

1. **Fail fast for critical errors**
```java
// Critical configuration errors should fail immediately
try {
    config = parser.parseFile("config.tsk", Config.class);
} catch (ParseException e) {
    // Critical error - fail immediately
    throw new ConfigurationException("Critical configuration error", e);
}
```

2. **Graceful degradation for non-critical errors**
```java
// Non-critical errors should allow graceful degradation
try {
    config = parser.parseFile("config.tsk", Config.class);
} catch (ValidationException e) {
    // Non-critical error - use defaults
    config = createDefaultConfig();
    logger.warn("Using default configuration due to validation error: " + e.getMessage());
}
```

3. **Comprehensive error reporting**
```java
// Provide detailed error information
ErrorCollector collector = new ErrorCollector();
parser.setErrorCollector(collector);

try {
    config = parser.parseFile("config.tsk", Config.class);
} catch (TuskLangException e) {
    collector.addError(e);
}

// Report all errors
if (!collector.getErrors().isEmpty()) {
    generateErrorReport(collector.getErrors());
}
```

### Error Recovery Patterns

1. **Multiple fallback strategies**
```java
// Try multiple configuration sources
Config config = null;

// Try primary configuration
try {
    config = parser.parseFile("config.tsk", Config.class);
} catch (TuskLangException e) {
    // Try environment-specific configuration
    try {
        String env = System.getProperty("APP_ENV", "development");
        config = parser.parseFile("config." + env + ".tsk", Config.class);
    } catch (TuskLangException e2) {
        // Use default configuration
        config = createDefaultConfig();
    }
}
```

2. **Partial configuration loading**
```java
// Load configuration partially
Map<String, Object> partialConfig = new HashMap<>();

try {
    Map<String, Object> fullConfig = parser.parseFile("config.tsk");
    
    // Extract valid parts
    if (fullConfig.containsKey("app_name")) {
        partialConfig.put("app_name", fullConfig.get("app_name"));
    }
    if (fullConfig.containsKey("version")) {
        partialConfig.put("version", fullConfig.get("version"));
    }
    
} catch (TuskLangException e) {
    // Use partial configuration
    System.err.println("Partial configuration loaded: " + partialConfig);
}
```

## 📚 Next Steps

1. **Master error types** - Understand all TuskLang error types and their causes
2. **Implement error recovery** - Create robust error recovery mechanisms
3. **Use error analysis** - Leverage error analysis for debugging and improvement
4. **Add comprehensive testing** - Create thorough error handling test suites
5. **Optimize error reporting** - Implement effective error reporting and logging

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang error handling in Java! From basic error catching to sophisticated recovery mechanisms, you can build robust, fault-tolerant configuration systems that gracefully handle any error condition. 
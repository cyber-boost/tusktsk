# Syntax Errors in TuskLang - Java Edition

**"We don't bow to any king" - Error Handling with Java Power**

Understanding and handling syntax errors in TuskLang is crucial for building robust Java applications. The Java SDK provides comprehensive error handling and validation to catch and resolve syntax issues early.

## 🎯 Java Syntax Error Handling

### @TuskConfig Error Validation

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.exceptions.TuskSyntaxException;
import org.tusklang.java.validation.TuskValidator;
import org.tusklang.java.annotations.TuskValidate;

@TuskConfig
public class SyntaxErrorConfig {
    
    @TuskValidate("syntax")
    private String configContent;
    
    @TuskValidate("structure")
    private Map<String, Object> configStructure;
    
    private TuskValidator validator;
    private List<TuskSyntaxException> syntaxErrors;
    
    public SyntaxErrorConfig() {
        this.validator = new TuskValidator();
        this.syntaxErrors = new ArrayList<>();
    }
    
    // Syntax validation method
    public boolean validateSyntax(String tuskContent) {
        try {
            // Parse and validate TuskLang content
            validator.validateSyntax(tuskContent);
            return true;
        } catch (TuskSyntaxException e) {
            syntaxErrors.add(e);
            logSyntaxError(e);
            return false;
        }
    }
    
    // Structure validation method
    public boolean validateStructure(Map<String, Object> structure) {
        try {
            validator.validateStructure(structure);
            return true;
        } catch (TuskSyntaxException e) {
            syntaxErrors.add(e);
            logSyntaxError(e);
            return false;
        }
    }
    
    // Error logging
    private void logSyntaxError(TuskSyntaxException error) {
        System.err.println("Syntax Error: " + error.getMessage());
        System.err.println("Line: " + error.getLineNumber());
        System.err.println("Column: " + error.getColumnNumber());
        System.err.println("Context: " + error.getContext());
    }
    
    // Get all syntax errors
    public List<TuskSyntaxException> getSyntaxErrors() {
        return new ArrayList<>(syntaxErrors);
    }
    
    // Clear syntax errors
    public void clearSyntaxErrors() {
        syntaxErrors.clear();
    }
    
    // Getters and setters
    public String getConfigContent() { return configContent; }
    public void setConfigContent(String configContent) { this.configContent = configContent; }
    
    public Map<String, Object> getConfigStructure() { return configStructure; }
    public void setConfigStructure(Map<String, Object> configStructure) { this.configStructure = configStructure; }
}
```

### TuskLang Syntax Error Examples

```tusk
# Common syntax errors in TuskLang

# 1. Missing colon after key
# app_name = "My App"          # ERROR: Missing colon
app_name: string = "My App"     # CORRECT

# 2. Missing value after equals
# database_host: string =      # ERROR: Missing value
database_host: string = "localhost"  # CORRECT

# 3. Unclosed quotes
# message: string = "Hello     # ERROR: Unclosed quotes
message: string = "Hello World"       # CORRECT

# 4. Invalid type annotation
# port: invalid_type = 8080    # ERROR: Invalid type
port: number = 8080            # CORRECT

# 5. Malformed object syntax
# config: object = {           # ERROR: Missing closing brace
#     host: string = "localhost"
config: object = {
    host: string = "localhost"
}                              # CORRECT

# 6. Invalid array syntax
# items: array = [1, 2, 3,     # ERROR: Missing closing bracket
items: array = [1, 2, 3]       # CORRECT
```

## 🚨 Common Syntax Error Types

### Java Syntax Error Detection

```java
@TuskConfig
public class SyntaxErrorDetector {
    
    private static final List<String> VALID_TYPES = Arrays.asList(
        "string", "number", "boolean", "object", "array", "null"
    );
    
    // Detect missing colons
    public boolean detectMissingColon(String line) {
        return line.contains("=") && !line.contains(":");
    }
    
    // Detect missing values
    public boolean detectMissingValue(String line) {
        return line.trim().endsWith("=") || line.trim().endsWith(":");
    }
    
    // Detect unclosed quotes
    public boolean detectUnclosedQuotes(String content) {
        int singleQuotes = 0;
        int doubleQuotes = 0;
        
        for (char c : content.toCharArray()) {
            if (c == '\'') singleQuotes++;
            if (c == '"') doubleQuotes++;
        }
        
        return (singleQuotes % 2 != 0) || (doubleQuotes % 2 != 0);
    }
    
    // Detect invalid types
    public boolean detectInvalidType(String typeAnnotation) {
        return !VALID_TYPES.contains(typeAnnotation.toLowerCase());
    }
    
    // Detect malformed objects
    public boolean detectMalformedObject(String content) {
        int openBraces = 0;
        int closeBraces = 0;
        
        for (char c : content.toCharArray()) {
            if (c == '{') openBraces++;
            if (c == '}') closeBraces++;
        }
        
        return openBraces != closeBraces;
    }
    
    // Detect malformed arrays
    public boolean detectMalformedArray(String content) {
        int openBrackets = 0;
        int closeBrackets = 0;
        
        for (char c : content.toCharArray()) {
            if (c == '[') openBrackets++;
            if (c == ']') closeBrackets++;
        }
        
        return openBrackets != closeBrackets;
    }
    
    // Comprehensive syntax check
    public List<String> checkSyntax(String content) {
        List<String> errors = new ArrayList<>();
        String[] lines = content.split("\n");
        
        for (int i = 0; i < lines.length; i++) {
            String line = lines[i];
            int lineNumber = i + 1;
            
            if (detectMissingColon(line)) {
                errors.add("Line " + lineNumber + ": Missing colon after key");
            }
            
            if (detectMissingValue(line)) {
                errors.add("Line " + lineNumber + ": Missing value after equals");
            }
        }
        
        if (detectUnclosedQuotes(content)) {
            errors.add("Unclosed quotes detected");
        }
        
        if (detectMalformedObject(content)) {
            errors.add("Malformed object - mismatched braces");
        }
        
        if (detectMalformedArray(content)) {
            errors.add("Malformed array - mismatched brackets");
        }
        
        return errors;
    }
}
```

### TuskLang Syntax Error Patterns

```tusk
# Syntax error patterns and corrections

# PATTERN 1: Missing punctuation
# ERROR: app_name = "My App"
# ERROR: database_host: "localhost"
# ERROR: port: number
app_name: string = "My App"
database_host: string = "localhost"
port: number = 8080

# PATTERN 2: Invalid type annotations
# ERROR: port: int = 8080
# ERROR: enabled: bool = true
# ERROR: items: list = [1, 2, 3]
port: number = 8080
enabled: boolean = true
items: array = [1, 2, 3]

# PATTERN 3: Malformed objects
# ERROR: config: object = {
#     host: string = "localhost"
#     port: number = 8080
config: object = {
    host: string = "localhost"
    port: number = 8080
}

# PATTERN 4: Malformed arrays
# ERROR: servers: array = [
#     "server1"
#     "server2"
servers: array = [
    "server1"
    "server2"
]

# PATTERN 5: Invalid references
# ERROR: host: string = @invalid.reference
# ERROR: port: number = @database.port.invalid
host: string = @database.host
port: number = @database.port
```

## 🔧 Error Recovery and Fixing

### Java Error Recovery

```java
@TuskConfig
public class SyntaxErrorRecovery {
    
    private TuskValidator validator;
    private List<String> suggestions;
    
    public SyntaxErrorRecovery() {
        this.validator = new TuskValidator();
        this.suggestions = new ArrayList<>();
    }
    
    // Auto-fix common syntax errors
    public String autoFixSyntax(String content) {
        String fixed = content;
        
        // Fix missing colons
        fixed = fixMissingColons(fixed);
        
        // Fix missing values
        fixed = fixMissingValues(fixed);
        
        // Fix unclosed quotes
        fixed = fixUnclosedQuotes(fixed);
        
        // Fix invalid types
        fixed = fixInvalidTypes(fixed);
        
        return fixed;
    }
    
    // Fix missing colons
    private String fixMissingColons(String content) {
        // Replace "key = value" with "key: type = value"
        return content.replaceAll("(\\w+)\\s*=\\s*([^\\n]+)", "$1: string = $2");
    }
    
    // Fix missing values
    private String fixMissingValues(String content) {
        // Add default values for missing values
        content = content.replaceAll("(\\w+:\\s*\\w+)\\s*=\\s*$", "$1 = \"default\"");
        content = content.replaceAll("(\\w+:\\s*number)\\s*=\\s*$", "$1 = 0");
        content = content.replaceAll("(\\w+:\\s*boolean)\\s*=\\s*$", "$1 = false");
        return content;
    }
    
    // Fix unclosed quotes
    private String fixUnclosedQuotes(String content) {
        // Count quotes and add missing closing quotes
        String[] lines = content.split("\n");
        StringBuilder fixed = new StringBuilder();
        
        for (String line : lines) {
            int singleQuotes = countChar(line, '\'');
            int doubleQuotes = countChar(line, '"');
            
            if (singleQuotes % 2 != 0) {
                line += "'";
            }
            if (doubleQuotes % 2 != 0) {
                line += "\"";
            }
            
            fixed.append(line).append("\n");
        }
        
        return fixed.toString();
    }
    
    // Fix invalid types
    private String fixInvalidTypes(String content) {
        Map<String, String> typeMappings = Map.of(
            "int", "number",
            "bool", "boolean",
            "list", "array",
            "dict", "object",
            "str", "string"
        );
        
        for (Map.Entry<String, String> entry : typeMappings.entrySet()) {
            content = content.replaceAll(":\\s*" + entry.getKey() + "\\s*=", ": " + entry.getValue() + " =");
        }
        
        return content;
    }
    
    // Count character occurrences
    private int countChar(String str, char c) {
        int count = 0;
        for (char ch : str.toCharArray()) {
            if (ch == c) count++;
        }
        return count;
    }
    
    // Generate suggestions for errors
    public List<String> generateSuggestions(String error) {
        suggestions.clear();
        
        if (error.contains("Missing colon")) {
            suggestions.add("Add a colon (:) after the key name");
            suggestions.add("Example: 'app_name: string = \"My App\"'");
        }
        
        if (error.contains("Missing value")) {
            suggestions.add("Add a value after the equals sign");
            suggestions.add("Example: 'port: number = 8080'");
        }
        
        if (error.contains("Unclosed quotes")) {
            suggestions.add("Add closing quotes");
            suggestions.add("Example: 'message: string = \"Hello World\"'");
        }
        
        if (error.contains("Invalid type")) {
            suggestions.add("Use valid TuskLang types: string, number, boolean, object, array, null");
            suggestions.add("Example: 'enabled: boolean = true'");
        }
        
        return new ArrayList<>(suggestions);
    }
}
```

### TuskLang Error Recovery Examples

```tusk
# Error recovery examples

# ORIGINAL (with errors):
# app_name = "My App"
# database_host: "localhost"
# port: int = 8080
# enabled: bool = true
# config: object = {
#     host: string = "localhost"
#     port: number = 8080

# FIXED VERSION:
app_name: string = "My App"
database_host: string = "localhost"
port: number = 8080
enabled: boolean = true
config: object = {
    host: string = "localhost"
    port: number = 8080
}

# Common fixes applied:
# 1. Added missing colons
# 2. Added type annotations
# 3. Fixed invalid types (int -> number, bool -> boolean)
# 4. Added closing brace for object
```

## 🛡️ Validation and Prevention

### Java Syntax Validation

```java
@TuskConfig
public class SyntaxValidator {
    
    private static final Pattern VALID_KEY_PATTERN = Pattern.compile("^[a-zA-Z_][a-zA-Z0-9_]*$");
    private static final Pattern VALID_TYPE_PATTERN = Pattern.compile("^(string|number|boolean|object|array|null)$");
    
    // Validate key names
    public boolean validateKeyName(String key) {
        return VALID_KEY_PATTERN.matcher(key).matches();
    }
    
    // Validate type annotations
    public boolean validateTypeAnnotation(String type) {
        return VALID_TYPE_PATTERN.matcher(type.toLowerCase()).matches();
    }
    
    // Validate value format
    public boolean validateValueFormat(String value) {
        if (value == null || value.trim().isEmpty()) {
            return false;
        }
        
        // Check for proper string quotes
        if (value.startsWith("\"") && !value.endsWith("\"")) {
            return false;
        }
        
        if (value.startsWith("'") && !value.endsWith("'")) {
            return false;
        }
        
        return true;
    }
    
    // Validate line format
    public boolean validateLineFormat(String line) {
        line = line.trim();
        
        // Skip comments and empty lines
        if (line.isEmpty() || line.startsWith("#")) {
            return true;
        }
        
        // Check for proper key: type = value format
        String[] parts = line.split("=", 2);
        if (parts.length != 2) {
            return false;
        }
        
        String keyTypePart = parts[0].trim();
        String valuePart = parts[1].trim();
        
        // Validate key and type
        String[] keyType = keyTypePart.split(":", 2);
        if (keyType.length != 2) {
            return false;
        }
        
        String key = keyType[0].trim();
        String type = keyType[1].trim();
        
        return validateKeyName(key) && validateTypeAnnotation(type) && validateValueFormat(valuePart);
    }
    
    // Comprehensive validation
    public List<String> validateContent(String content) {
        List<String> errors = new ArrayList<>();
        String[] lines = content.split("\n");
        
        for (int i = 0; i < lines.length; i++) {
            String line = lines[i];
            int lineNumber = i + 1;
            
            if (!validateLineFormat(line)) {
                errors.add("Line " + lineNumber + ": Invalid format");
            }
        }
        
        return errors;
    }
}
```

### TuskLang Validation Examples

```tusk
# Validation examples

# VALID CONFIGURATION:
app_name: string = "My Application"
database_host: string = "localhost"
server_port: number = 8080
debug_mode: boolean = true
allowed_origins: array = [
    "https://app.example.com"
    "https://api.example.com"
]
database_config: object = {
    host: string = "localhost"
    port: number = 5432
    ssl: boolean = true
}

# INVALID CONFIGURATION (with errors):
# 1app_name: string = "My App"        # Invalid key (starts with number)
# app-name: string = "My App"          # Invalid key (contains hyphen)
# app_name: invalid_type = "My App"    # Invalid type
# app_name: string =                   # Missing value
# app_name: string = "Unclosed quotes  # Unclosed quotes
# config: object = {                   # Missing closing brace
#     host: string = "localhost"
```

## 🎯 Best Practices

### Error Prevention Guidelines

1. **Use proper syntax** from the start
2. **Validate configuration** before deployment
3. **Use IDE support** for syntax highlighting
4. **Test configurations** thoroughly
5. **Document syntax rules** for your team

### Performance Considerations

```java
// Efficient syntax checking
@TuskConfig
public class EfficientSyntaxChecker {
    
    private static final Set<String> VALID_TYPES = Set.of(
        "string", "number", "boolean", "object", "array", "null"
    );
    
    public boolean isValidType(String type) {
        return VALID_TYPES.contains(type.toLowerCase());
    }
    
    public boolean isValidKey(String key) {
        return key != null && key.matches("^[a-zA-Z_][a-zA-Z0-9_]*$");
    }
}
```

## 🚨 Troubleshooting

### Common Syntax Issues

1. **Missing colons**: Always use `key: type = value` format
2. **Invalid types**: Use only valid TuskLang types
3. **Unclosed quotes**: Ensure all strings are properly quoted
4. **Malformed objects**: Check brace matching
5. **Malformed arrays**: Check bracket matching

### Debug Syntax Issues

```java
// Debug syntax problems
public void debugSyntaxIssues(String content) {
    List<String> errors = checkSyntax(content);
    
    if (errors.isEmpty()) {
        System.out.println("No syntax errors found");
    } else {
        System.out.println("Syntax errors found:");
        for (String error : errors) {
            System.out.println("  - " + error);
        }
    }
}
```

## 🎯 Next Steps

1. **Review syntax rules** thoroughly
2. **Implement validation** in your applications
3. **Use error recovery** for user-friendly experiences
4. **Test error handling** with invalid configurations
5. **Document common errors** for your team

---

**Ready to master TuskLang syntax with Java power? Understanding and handling syntax errors is crucial for building robust applications. We don't bow to any king - especially not syntax errors!** 
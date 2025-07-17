# ☕ TuskLang Java Comments Guide

**"We don't bow to any king" - Java Edition**

Master TuskLang comments in Java with comprehensive coverage of comment syntax, documentation patterns, and best practices for creating clear, maintainable configuration files.

## 🎯 Comment Basics

### Single-Line Comments

```tsk
# This is a single-line comment
app_name: "My TuskLang App"  # Inline comment after value
version: "1.0.0"             # Version number

# Database configuration
[database]
host: "localhost"    # Database host
port: 5432          # Database port
name: "myapp"       # Database name
user: "postgres"    # Database user
password: "secret"  # Database password
```

### Multi-Line Comments

```tsk
# This is a multi-line comment
# that spans multiple lines
# to provide detailed documentation

app_name: "My TuskLang App"
version: "1.0.0"

# Server configuration section
# Contains all server-related settings
# including host, port, and SSL settings
[server]
host: "0.0.0.0"     # Bind to all interfaces
port: 8080          # HTTP port
ssl: false          # SSL disabled for development
```

### Block Comments

```tsk
/*
 * This is a block comment
 * that can span multiple lines
 * and provide detailed documentation
 * for complex configuration sections
 */

app_name: "My TuskLang App"
version: "1.0.0"

/*
 * Database Configuration
 * 
 * This section contains all database-related settings
 * including connection parameters, pooling, and SSL
 * configuration for secure database connections.
 */
[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"
```

## 🔧 Java Comment Integration

### Parsing Comments

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

@TuskConfig
public class CommentedConfig {
    public String appName;
    public String version;
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

public class CommentsExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Enable comment parsing
        parser.setPreserveComments(true);
        
        CommentedConfig config = parser.parseFile("config.tsk", CommentedConfig.class);
        
        // Access configuration with comments preserved
        System.out.println("App: " + config.appName + " v" + config.version);
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Server: " + config.server.host + ":" + config.server.port);
    }
}
```

### Comment Metadata

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.comments.CommentMetadata;
import java.util.Map;

public class CommentMetadataExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        parser.setPreserveComments(true);
        
        Map<String, Object> config = parser.parseFile("config.tsk");
        CommentMetadata metadata = parser.getCommentMetadata();
        
        // Access comment information
        System.out.println("Comments found: " + metadata.getCommentCount());
        System.out.println("Documentation lines: " + metadata.getDocumentationLines());
        
        // Get comments for specific keys
        String appNameComment = metadata.getComment("app_name");
        String dbHostComment = metadata.getComment("database.host");
        
        System.out.println("App name comment: " + appNameComment);
        System.out.println("Database host comment: " + dbHostComment);
    }
}
```

## 📝 Documentation Patterns

### Section Documentation

```tsk
# =============================================================================
# APPLICATION CONFIGURATION
# =============================================================================
# 
# This file contains the main configuration for the TuskLang application.
# It includes settings for the application, database, server, and security.
# 
# File: config.tsk
# Version: 1.0.0
# Last Updated: 2024-01-15
# =============================================================================

# Application Settings
# -------------------
# Basic application information and version details
app_name: "My TuskLang App"
version: "1.0.0"
debug: true

# Database Configuration
# ----------------------
# PostgreSQL database connection settings
# All database operations use these connection parameters
[database]
host: "localhost"    # Database server hostname
port: 5432          # PostgreSQL default port
name: "myapp"       # Database name
user: "postgres"    # Database username
password: "secret"  # Database password (use environment variable in production)

# Server Configuration
# --------------------
# HTTP server settings for the application
[server]
host: "0.0.0.0"     # Bind to all network interfaces
port: 8080          # HTTP port number
ssl: false          # SSL/TLS encryption (enable for production)
```

### Java Documentation Integration

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;
import java.util.Map;

/**
 * Main application configuration class.
 * 
 * This class represents the complete configuration for the TuskLang application,
 * including application settings, database configuration, and server settings.
 * 
 * @see DatabaseConfig
 * @see ServerConfig
 */
@TuskConfig
public class ApplicationConfig {
    /** Application name */
    public String appName;
    
    /** Application version */
    public String version;
    
    /** Debug mode flag */
    public boolean debug;
    
    /** Database configuration */
    public DatabaseConfig database;
    
    /** Server configuration */
    public ServerConfig server;
}

/**
 * Database configuration class.
 * 
 * Contains all database connection parameters and settings.
 */
@TuskConfig
public class DatabaseConfig {
    /** Database server hostname */
    public String host;
    
    /** Database server port */
    public int port;
    
    /** Database name */
    public String name;
    
    /** Database username */
    public String user;
    
    /** Database password */
    public String password;
}

/**
 * Server configuration class.
 * 
 * Contains HTTP server settings and configuration.
 */
@TuskConfig
public class ServerConfig {
    /** Server hostname or IP address */
    public String host;
    
    /** Server port number */
    public int port;
    
    /** SSL/TLS encryption flag */
    public boolean ssl;
}

public class DocumentationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        ApplicationConfig config = parser.parseFile("config.tsk", ApplicationConfig.class);
        
        // Use documented configuration
        System.out.println("Application: " + config.appName + " v" + config.version);
        System.out.println("Debug mode: " + config.debug);
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Server: " + config.server.host + ":" + config.server.port);
    }
}
```

## 🔄 Environment-Specific Comments

### Development Configuration

```tsk
# =============================================================================
# DEVELOPMENT CONFIGURATION
# =============================================================================
# 
# This configuration is used for development environments.
# It includes debug settings, local database, and development-specific features.
# 
# Environment: development
# Purpose: Local development and testing
# =============================================================================

# Development Settings
# -------------------
# Settings specific to development environment
app_name: "My TuskLang App (Dev)"
version: "1.0.0-dev"
debug: true          # Enable debug mode for development

# Development Database
# -------------------
# Local PostgreSQL database for development
[database]
host: "localhost"    # Local database server
port: 5432          # Default PostgreSQL port
name: "myapp_dev"   # Development database
user: "postgres"    # Local database user
password: "dev123"  # Development password (not secure)

# Development Server
# -----------------
# Local development server settings
[server]
host: "localhost"    # Local development server
port: 3000          # Development port
ssl: false          # No SSL for local development
```

### Production Configuration

```tsk
# =============================================================================
# PRODUCTION CONFIGURATION
# =============================================================================
# 
# This configuration is used for production environments.
# It includes security settings, production database, and performance optimizations.
# 
# Environment: production
# Purpose: Live production deployment
# Security: High (uses environment variables for sensitive data)
# =============================================================================

# Production Settings
# ------------------
# Settings optimized for production environment
app_name: "My TuskLang App"
version: "1.0.0"
debug: false         # Disable debug mode for production

# Production Database
# ------------------
# Production PostgreSQL database with security
[database]
host: @env("DB_HOST", "prod-db.example.com")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "myapp_prod")
user: @env("DB_USER", "app_user")
password: @env.secure("DB_PASSWORD")  # Secure password from environment

# Production Server
# ----------------
# Production server with SSL and security
[server]
host: "0.0.0.0"      # Bind to all interfaces
port: 443            # HTTPS port
ssl: true            # Enable SSL for production
```

### Java Environment-Specific Loading

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.config.TuskConfig;

@TuskConfig
public class EnvironmentConfig {
    public String appName;
    public String version;
    public boolean debug;
    public DatabaseConfig database;
    public ServerConfig server;
}

public class EnvironmentSpecificExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        
        // Determine environment
        String environment = System.getProperty("APP_ENV", "development");
        
        // Load environment-specific configuration
        EnvironmentConfig config = parser.parseFile(environment + ".tsk", EnvironmentConfig.class);
        
        System.out.println("Environment: " + environment);
        System.out.println("App: " + config.appName);
        System.out.println("Debug: " + config.debug);
        System.out.println("Database: " + config.database.host + ":" + config.database.port);
        System.out.println("Server: " + config.server.host + ":" + config.server.port);
    }
}
```

## 🔧 Comment Utilities

### Comment Extraction

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.comments.CommentExtractor;
import java.util.List;
import java.util.Map;

public class CommentExtractionExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        parser.setPreserveComments(true);
        
        // Parse configuration
        Map<String, Object> config = parser.parseFile("config.tsk");
        
        // Extract comments
        CommentExtractor extractor = new CommentExtractor();
        List<String> allComments = extractor.extractAllComments(parser.getRawContent());
        
        System.out.println("All comments found:");
        for (String comment : allComments) {
            System.out.println("  " + comment);
        }
        
        // Extract section comments
        Map<String, String> sectionComments = extractor.extractSectionComments(parser.getRawContent());
        
        System.out.println("Section comments:");
        for (Map.Entry<String, String> entry : sectionComments.entrySet()) {
            System.out.println("  " + entry.getKey() + ": " + entry.getValue());
        }
    }
}
```

### Comment Validation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.validation.CommentValidator;
import java.util.List;

public class CommentValidationExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        parser.setPreserveComments(true);
        
        // Parse configuration
        parser.parseFile("config.tsk");
        
        // Validate comments
        CommentValidator validator = new CommentValidator();
        List<String> validationErrors = validator.validateComments(parser.getRawContent());
        
        if (validationErrors.isEmpty()) {
            System.out.println("All comments are valid");
        } else {
            System.out.println("Comment validation errors:");
            for (String error : validationErrors) {
                System.err.println("  " + error);
            }
        }
        
        // Check for required documentation
        List<String> missingDocs = validator.findMissingDocumentation(parser.getRawContent());
        
        if (!missingDocs.isEmpty()) {
            System.out.println("Missing documentation for:");
            for (String item : missingDocs) {
                System.out.println("  " + item);
            }
        }
    }
}
```

## 📊 Comment Statistics

### Comment Analysis

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.comments.CommentAnalyzer;
import java.util.Map;

public class CommentAnalysisExample {
    public static void main(String[] args) {
        TuskLang parser = new TuskLang();
        parser.setPreserveComments(true);
        
        // Parse configuration
        parser.parseFile("config.tsk");
        
        // Analyze comments
        CommentAnalyzer analyzer = new CommentAnalyzer();
        Map<String, Object> stats = analyzer.analyzeComments(parser.getRawContent());
        
        System.out.println("Comment Statistics:");
        System.out.println("  Total lines: " + stats.get("totalLines"));
        System.out.println("  Comment lines: " + stats.get("commentLines"));
        System.out.println("  Documentation ratio: " + stats.get("documentationRatio") + "%");
        System.out.println("  Sections documented: " + stats.get("sectionsDocumented"));
        System.out.println("  Keys documented: " + stats.get("keysDocumented"));
        
        // Get comment quality score
        double qualityScore = analyzer.getCommentQualityScore(parser.getRawContent());
        System.out.println("  Comment quality score: " + qualityScore + "/10");
    }
}
```

## 🧪 Testing Comments

### Unit Tests

```java
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeEach;
import static org.junit.jupiter.api.Assertions.*;
import org.tusklang.java.TuskLang;
import org.tusklang.java.comments.CommentExtractor;
import java.util.List;

class CommentsTest {
    
    private TuskLang parser;
    private CommentExtractor extractor;
    
    @BeforeEach
    void setUp() {
        parser = new TuskLang();
        parser.setPreserveComments(true);
        extractor = new CommentExtractor();
    }
    
    @Test
    void testSingleLineComments() {
        String tskContent = """
            # This is a comment
            app_name: "Test App"  # Inline comment
            version: "1.0.0"
            """;
        
        List<String> comments = extractor.extractAllComments(tskContent);
        
        assertEquals(2, comments.size());
        assertTrue(comments.contains("This is a comment"));
        assertTrue(comments.contains("Inline comment"));
    }
    
    @Test
    void testMultiLineComments() {
        String tskContent = """
            # This is a multi-line comment
            # that spans multiple lines
            # for detailed documentation
            
            app_name: "Test App"
            version: "1.0.0"
            """;
        
        List<String> comments = extractor.extractAllComments(tskContent);
        
        assertEquals(3, comments.size());
        assertTrue(comments.contains("This is a multi-line comment"));
        assertTrue(comments.contains("that spans multiple lines"));
        assertTrue(comments.contains("for detailed documentation"));
    }
    
    @Test
    void testBlockComments() {
        String tskContent = """
            /*
             * This is a block comment
             * that spans multiple lines
             */
            
            app_name: "Test App"
            version: "1.0.0"
            """;
        
        List<String> comments = extractor.extractAllComments(tskContent);
        
        assertEquals(3, comments.size());
        assertTrue(comments.contains("This is a block comment"));
        assertTrue(comments.contains("that spans multiple lines"));
    }
    
    @Test
    void testSectionComments() {
        String tskContent = """
            # Database Configuration
            # ----------------------
            # PostgreSQL database settings
            [database]
            host: "localhost"
            port: 5432
            """;
        
        Map<String, String> sectionComments = extractor.extractSectionComments(tskContent);
        
        assertTrue(sectionComments.containsKey("database"));
        String dbComment = sectionComments.get("database");
        assertTrue(dbComment.contains("Database Configuration"));
        assertTrue(dbComment.contains("PostgreSQL database settings"));
    }
}
```

## 🔧 Troubleshooting

### Common Comment Issues

1. **Comment Not Preserved**
```java
// Enable comment preservation
TuskLang parser = new TuskLang();
parser.setPreserveComments(true);  // Must be set before parsing

Map<String, Object> config = parser.parseFile("config.tsk");
```

2. **Invalid Comment Syntax**
```tsk
# Correct comment syntax
app_name: "My App"  # This is correct

# Incorrect comment syntax (will cause parsing errors)
app_name: "My App"  /* This is incorrect */
app_name: "My App"  // This is also incorrect
```

3. **Missing Documentation**
```java
// Check for missing documentation
CommentValidator validator = new CommentValidator();
List<String> missingDocs = validator.findMissingDocumentation(content);

if (!missingDocs.isEmpty()) {
    System.out.println("Add documentation for: " + missingDocs);
}
```

## 📚 Best Practices

### Comment Guidelines

1. **Use descriptive comments**
```tsk
# Good: Descriptive comment
max_connections: 100  # Maximum database connections

# Bad: Obvious comment
max_connections: 100  # Number
```

2. **Document sections thoroughly**
```tsk
# Database Configuration
# ----------------------
# Contains all database connection parameters including host, port,
# credentials, and connection pooling settings. These settings are
# critical for application performance and reliability.
[database]
host: "localhost"
port: 5432
```

3. **Use consistent comment style**
```tsk
# Use consistent formatting for similar sections
# Application Settings
# -------------------
app_name: "My App"
version: "1.0.0"

# Database Settings
# ----------------
host: "localhost"
port: 5432
```

4. **Include environment-specific notes**
```tsk
# Development database (use production credentials in live environment)
[database]
host: "localhost"    # Local development server
password: "dev123"   # Development password (not secure)
```

## 📚 Next Steps

1. **Master comment syntax** - Understand all comment styles and patterns
2. **Implement documentation** - Add comprehensive documentation to configurations
3. **Use comment utilities** - Leverage comment extraction and analysis tools
4. **Validate comments** - Ensure comment quality and completeness
5. **Test comment handling** - Create comprehensive comment testing

---

**"We don't bow to any king"** - You now have complete mastery of TuskLang comments in Java! From basic comments to comprehensive documentation patterns, you can create clear, maintainable configuration files with proper documentation and validation. 
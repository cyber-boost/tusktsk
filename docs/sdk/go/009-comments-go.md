# 💬 TuskLang Go Comments Guide

**"We don't bow to any king" - Go Edition**

Master the art of commenting in TuskLang configuration files and Go code. This guide covers comment styles, best practices, documentation generation, and maintaining clean, readable code.

## 📝 TuskLang Comment Styles

### Single-Line Comments

```go
// config.tsk
[app]
name: "My Application"  // Application name
version: "1.0.0"        // Current version
debug: true             // Enable debug mode

[database]
host: "localhost"       // Database host
port: 5432              // Database port
name: "myapp"           // Database name
```

### Multi-Line Comments

```go
// config.tsk
/*
 * Application Configuration
 * This file contains all application settings
 * including database, server, and feature flags
 */

[app]
name: "My Application"
version: "1.0.0"

/*
 * Database Configuration
 * PostgreSQL connection settings
 * Used by the main application and workers
 */
[database]
host: "localhost"
port: 5432
name: "myapp"
```

### Section Comments

```go
// config.tsk
# ========================================
# APPLICATION CONFIGURATION
# ========================================
[app]
name: "My Application"
version: "1.0.0"

# ========================================
# DATABASE CONFIGURATION
# ========================================
[database]
host: "localhost"
port: 5432
name: "myapp"

# ========================================
# SERVER CONFIGURATION
# ========================================
[server]
host: "0.0.0.0"
port: 8080
```

### Inline Comments with Different Syntax

```go
// config.tsk
[app]
name: "My Application"  # Application name
version: "1.0.0"        # Current version
debug: true             # Enable debug mode

[database]
host: "localhost"       -- Database host
port: 5432              -- Database port
name: "myapp"           -- Database name

[server]
host: "0.0.0.0"         /* Server host */
port: 8080              /* Server port */
ssl: true               /* Enable SSL */
```

## 🔧 Go Code Comments

### Package Comments

```go
// Package config provides TuskLang configuration management for Go applications.
// It includes parsing, validation, and type-safe struct mapping capabilities.
//
// Example:
//
//	parser := tusklanggo.NewEnhancedParser()
//	data, err := parser.ParseFile("config.tsk")
//	if err != nil {
//	    log.Fatal(err)
//	}
//
//	var config AppConfig
//	err = tusklanggo.UnmarshalTSK(data, &config)
package config
```

### Function Comments

```go
// Load parses a TuskLang configuration file and returns a Config struct.
// The function supports environment variables, cross-file references,
// and database queries embedded in the configuration.
//
// Parameters:
//   - configPath: Path to the TSK configuration file
//
// Returns:
//   - *Config: Parsed configuration struct
//   - error: Parsing error if any
//
// Example:
//
//	config, err := Load("configs/main.tsk")
//	if err != nil {
//	    log.Fatal(err)
//	}
func Load(configPath string) (*Config, error) {
    // Implementation
}

// ParseFile parses a single TSK file and returns the raw data.
// This is a lower-level function that doesn't perform struct mapping.
//
// Parameters:
//   - filename: Path to the TSK file
//
// Returns:
//   - map[string]interface{}: Parsed configuration data
//   - error: Parsing error if any
func ParseFile(filename string) (map[string]interface{}, error) {
    // Implementation
}
```

### Type Comments

```go
// Config represents the main application configuration structure.
// It contains all sections from the TSK file mapped to Go structs.
type Config struct {
    App      AppConfig      `tsk:"app"`      // Application settings
    Database DatabaseConfig `tsk:"database"` // Database configuration
    Server   ServerConfig   `tsk:"server"`   // Server settings
    Features FeaturesConfig `tsk:"features"` // Feature flags
}

// AppConfig contains application-level configuration settings.
type AppConfig struct {
    Name        string `tsk:"name"`        // Application name
    Version     string `tsk:"version"`     // Application version
    Environment string `tsk:"environment"` // Current environment
    Debug       bool   `tsk:"debug"`       // Debug mode flag
}

// DatabaseConfig contains database connection settings.
type DatabaseConfig struct {
    Host     string `tsk:"host"`     // Database host
    Port     int    `tsk:"port"`     // Database port
    Name     string `tsk:"name"`     // Database name
    User     string `tsk:"user"`     // Database user
    Password string `tsk:"password"` // Database password
    SSL      bool   `tsk:"ssl"`      // SSL connection flag
}
```

### Field Comments

```go
type ServerConfig struct {
    Host    string `tsk:"host"`    // Server host (e.g., "localhost", "0.0.0.0")
    Port    int    `tsk:"port"`    // Server port (e.g., 8080, 443)
    SSL     bool   `tsk:"ssl"`     // Enable SSL/TLS encryption
    Debug   bool   `tsk:"debug"`   // Enable debug logging
    Workers int    `tsk:"workers"` // Number of worker goroutines
}
```

## 📚 Documentation Comments

### API Documentation

```go
// Parser provides TuskLang file parsing capabilities.
// It supports multiple syntax styles, environment variables,
// and cross-file references.
type Parser struct {
    // ... fields
}

// NewParser creates a new TuskLang parser instance.
// The parser is configured with default settings suitable
// for most applications.
func NewParser() *Parser {
    return &Parser{
        // ... initialization
    }
}

// ParseFile parses a TSK configuration file and returns the parsed data.
// The function automatically handles:
// - Environment variable substitution
// - Cross-file references
// - Database queries
// - @ operator evaluation
//
// Parameters:
//   - filename: Path to the TSK file to parse
//
// Returns:
//   - map[string]interface{}: Parsed configuration data
//   - error: Parsing error if any
//
// Example:
//
//	parser := NewParser()
//	data, err := parser.ParseFile("config.tsk")
//	if err != nil {
//	    log.Fatal(err)
//	}
//
//	fmt.Printf("App name: %s\n", data["app"].(map[string]interface{})["name"])
func (p *Parser) ParseFile(filename string) (map[string]interface{}, error) {
    // Implementation
}
```

### Example Documentation

```go
// Example_parseFile demonstrates how to parse a TSK configuration file.
func Example_parseFile() {
    // Create a new parser
    parser := NewParser()
    
    // Parse the configuration file
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        log.Fatal(err)
    }
    
    // Access configuration values
    appName := data["app"].(map[string]interface{})["name"]
    fmt.Printf("Application: %s\n", appName)
    
    // Output: Application: My App
}

// Example_unmarshal demonstrates struct mapping from TSK data.
func Example_unmarshal() {
    // Parse configuration
    parser := NewParser()
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        log.Fatal(err)
    }
    
    // Map to struct
    var config Config
    err = UnmarshalTSK(data, &config)
    if err != nil {
        log.Fatal(err)
    }
    
    fmt.Printf("App: %s v%s\n", config.App.Name, config.App.Version)
    
    // Output: App: My App v1.0.0
}
```

## 🎯 Comment Best Practices

### 1. TuskLang Configuration Comments

```go
// Good - Clear, descriptive comments
[app]
name: "My Application"  # Application name for display
version: "1.0.0"        # Semantic versioning
debug: true             # Enable debug logging

[database]
host: "localhost"       # Database server hostname
port: 5432              # PostgreSQL default port
name: "myapp"           # Database name for connection

# Bad - Obvious or redundant comments
[app]
name: "My Application"  # Name
version: "1.0.0"        # Version
debug: true             # Debug

[database]
host: "localhost"       # Host
port: 5432              # Port
name: "myapp"           # Name
```

### 2. Go Code Comments

```go
// Good - Explain the "why", not the "what"
// Use exponential backoff to handle temporary database connection issues
func (s *Service) connectWithRetry() error {
    // Implementation
}

// Bad - Commenting the obvious
// Loop through users
for _, user := range users {
    // Process user
    processUser(user)
}
```

### 3. Function Comments

```go
// Good - Complete function documentation
// ProcessUser handles user data processing and validation.
// It performs the following operations:
// 1. Validates user input
// 2. Sanitizes data
// 3. Saves to database
// 4. Sends notification email
//
// Parameters:
//   - user: User data to process
//
// Returns:
//   - error: Processing error if any
func ProcessUser(user *User) error {
    // Implementation
}

// Bad - Incomplete or unclear documentation
// Process user
func ProcessUser(user *User) error {
    // Implementation
}
```

### 4. Type Comments

```go
// Good - Explain the purpose and usage
// User represents a system user with authentication and profile data.
// It's used throughout the application for user management,
// authentication, and authorization.
type User struct {
    ID       int    `json:"id"`       // Unique user identifier
    Username string `json:"username"` // Login username
    Email    string `json:"email"`    // User email address
    Active   bool   `json:"active"`   // Account status
}

// Bad - Redundant or unclear
// User struct
type User struct {
    ID       int    `json:"id"`       // ID
    Username string `json:"username"` // Username
    Email    string `json:"email"`    // Email
    Active   bool   `json:"active"`   // Active
}
```

## 🔍 Comment Examples by Context

### Configuration Files

```go
// configs/main.tsk
# ========================================
# MAIN APPLICATION CONFIGURATION
# ========================================
# This file contains the primary configuration for the application.
# It references other configuration files for modular organization.

$app_name: "My TuskLang App"  # Application name variable
$version: "1.0.0"             # Application version variable
$environment: @env("APP_ENV", "development")  # Environment from env var

[app]
name: $app_name               # Use variable for consistency
version: $version             # Use variable for consistency
environment: $environment     # Dynamic environment setting

# Database configuration loaded from separate file
[database]
host: @configs/database.tsk.get("host")
port: @configs/database.tsk.get("port")
name: @configs/database.tsk.get("name")

# Server configuration with environment-specific settings
[server]
host: @if($environment == "production", "0.0.0.0", "localhost")
port: @if($environment == "production", 80, 8080)
ssl: @if($environment == "production", true, false)
```

### Go Source Code

```go
// internal/config/parser.go
package config

import (
    "fmt"
    "os"
    "github.com/tusklang/go"
)

// Parser provides TuskLang configuration parsing capabilities.
// It supports multiple syntax styles, environment variables,
// cross-file references, and database queries.
type Parser struct {
    debug bool
    // ... other fields
}

// NewParser creates a new parser instance with default settings.
// The parser is ready to parse TSK files immediately after creation.
func NewParser() *Parser {
    return &Parser{
        debug: false,
        // ... initialization
    }
}

// SetDebug enables or disables debug mode for the parser.
// When debug mode is enabled, the parser will output detailed
// information about the parsing process, including:
// - File loading steps
// - Variable substitutions
// - @ operator evaluations
// - Cross-file references
//
// Parameters:
//   - enabled: Whether to enable debug mode
func (p *Parser) SetDebug(enabled bool) {
    p.debug = enabled
}

// ParseFile parses a TSK configuration file and returns the parsed data.
// The function handles all TuskLang features including:
// - Multiple syntax styles (INI, JSON-like, XML-inspired)
// - Environment variable substitution
// - Cross-file references and imports
// - Database queries with @query operator
// - @ operator system (env, date, cache, etc.)
// - FUJSEN executable functions
//
// Parameters:
//   - filename: Path to the TSK file to parse
//
// Returns:
//   - map[string]interface{}: Parsed configuration data
//   - error: Parsing error if any
//
// Example:
//
//	parser := NewParser()
//	data, err := parser.ParseFile("config.tsk")
//	if err != nil {
//	    log.Fatal(err)
//	}
//
//	// Access configuration values
//	appName := data["app"].(map[string]interface{})["name"]
func (p *Parser) ParseFile(filename string) (map[string]interface{}, error) {
    // Check if file exists
    if _, err := os.Stat(filename); os.IsNotExist(err) {
        return nil, fmt.Errorf("configuration file not found: %s", filename)
    }
    
    // Parse the file
    // ... implementation details
    
    return data, nil
}
```

### Test Files

```go
// internal/config/parser_test.go
package config

import (
    "testing"
    "reflect"
)

// TestParseFile tests the ParseFile function with various inputs.
// It covers:
// - Valid TSK files
// - Invalid syntax
// - Missing files
// - Environment variables
// - Cross-file references
func TestParseFile(t *testing.T) {
    // Test cases
    tests := []struct {
        name     string
        filename string
        wantErr  bool
    }{
        {
            name:     "valid config",
            filename: "testdata/valid.tsk",
            wantErr:  false,
        },
        {
            name:     "invalid syntax",
            filename: "testdata/invalid.tsk",
            wantErr:  true,
        },
        {
            name:     "missing file",
            filename: "testdata/missing.tsk",
            wantErr:  true,
        },
    }
    
    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            parser := NewParser()
            _, err := parser.ParseFile(tt.filename)
            
            if (err != nil) != tt.wantErr {
                t.Errorf("ParseFile() error = %v, wantErr %v", err, tt.wantErr)
            }
        })
    }
}

// TestParseFileWithEnvironment tests parsing with environment variables.
// It verifies that @env operators work correctly and that
// environment variables are properly substituted.
func TestParseFileWithEnvironment(t *testing.T) {
    // Set up test environment
    os.Setenv("TEST_VAR", "test_value")
    defer os.Unsetenv("TEST_VAR")
    
    parser := NewParser()
    data, err := parser.ParseFile("testdata/env.tsk")
    
    if err != nil {
        t.Fatalf("ParseFile() error = %v", err)
    }
    
    // Verify environment variable substitution
    expected := "test_value"
    if got := data["test"].(map[string]interface{})["value"]; got != expected {
        t.Errorf("Environment variable substitution failed: got %v, want %v", got, expected)
    }
}
```

## 📊 Documentation Generation

### Godoc Comments

```go
// Package tusk provides TuskLang integration for Go applications.
//
// TuskLang is a revolutionary configuration language that supports
// database queries, environment variables, and executable functions
// directly in configuration files.
//
// Example:
//
//	package main
//
//	import (
//	    "fmt"
//	    "github.com/tusklang/go"
//	)
//
//	func main() {
//	    parser := tusklanggo.NewEnhancedParser()
//	    data, err := parser.ParseFile("config.tsk")
//	    if err != nil {
//	        log.Fatal(err)
//	    }
//
//	    var config Config
//	    err = tusklanggo.UnmarshalTSK(data, &config)
//	    if err != nil {
//	        log.Fatal(err)
//	    }
//
//	    fmt.Printf("App: %s v%s\n", config.App.Name, config.App.Version)
//	}
package tusk

// Config represents the main application configuration.
// It maps TSK file sections to Go structs for type-safe access.
type Config struct {
    App      AppConfig      `tsk:"app"`      // Application settings
    Database DatabaseConfig `tsk:"database"` // Database configuration
    Server   ServerConfig   `tsk:"server"`   // Server settings
}

// AppConfig contains application-level configuration.
type AppConfig struct {
    Name    string `tsk:"name"`    // Application name
    Version string `tsk:"version"` // Application version
    Debug   bool   `tsk:"debug"`   // Debug mode flag
}
```

### README Comments

```go
// README.md
# TuskLang Go SDK

A powerful Go SDK for TuskLang, the revolutionary configuration language that supports database queries, environment variables, and executable functions directly in configuration files.

## Features

- **Multiple Syntax Styles**: Support for INI, JSON-like, and XML-inspired syntax
- **Database Integration**: Direct SQL queries in configuration files
- **Environment Variables**: Dynamic configuration based on environment
- **Type Safety**: Automatic struct mapping with Go types
- **Performance**: High-performance parsing and caching
- **Extensibility**: Custom @ operators and FUJSEN functions

## Quick Start

```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    // Create parser
    parser := tusklanggo.NewEnhancedParser()
    
    // Parse configuration
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        log.Fatal(err)
    }
    
    // Map to struct
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        log.Fatal(err)
    }
    
    fmt.Printf("App: %s v%s\n", config.App.Name, config.App.Version)
}
```

## Documentation

- [Installation Guide](docs/go/001-installation-go.md)
- [Quick Start Guide](docs/go/002-quick-start-go.md)
- [Basic Syntax](docs/go/003-basic-syntax-go.md)
- [Database Integration](docs/go/004-database-integration-go.md)
- [Advanced Features](docs/go/005-advanced-features-go.md)

## License

MIT License - see LICENSE file for details.
```

## 📚 Summary

You've learned:

1. **TuskLang Comment Styles** - Single-line, multi-line, and section comments
2. **Go Code Comments** - Package, function, type, and field documentation
3. **Documentation Comments** - API documentation and examples
4. **Best Practices** - Writing clear, useful comments
5. **Context-Specific Examples** - Comments for different file types
6. **Documentation Generation** - Godoc and README comments
7. **Comment Guidelines** - When and how to comment effectively
8. **Maintenance** - Keeping comments up to date

## 🚀 Next Steps

Now that you understand commenting:

1. **Document Your Code** - Add comprehensive comments to your projects
2. **Generate Documentation** - Use godoc to create API documentation
3. **Maintain Comments** - Keep comments updated with code changes
4. **Review Comments** - Ensure comments add value and clarity
5. **Share Knowledge** - Use comments to help other developers

---

**"We don't bow to any king"** - Your TuskLang Go code is now properly documented and ready to be understood by developers around the world! 
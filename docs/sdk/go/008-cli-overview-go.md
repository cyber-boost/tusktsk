# 🖥️ TuskLang Go CLI Overview Guide

**"We don't bow to any king" - Go Edition**

Master the TuskLang CLI tools and create powerful command-line applications in Go. This guide covers CLI development, TuskLang CLI integration, and building sophisticated command-line tools.

## 🚀 TuskLang CLI Tools

### Core CLI Commands

```bash
# Parse TSK files
tusk parse config.tsk

# Validate syntax
tusk validate config.tsk

# Convert between formats
tusk convert config.tsk --format json
tusk convert config.tsk --format yaml
tusk convert config.tsk --format toml

# Generate code
tusk generate --type go config.tsk
tusk generate --type struct config.tsk

# Interactive shell
tusk shell config.tsk

# Version information
tusk version
tusk --help
```

### Advanced CLI Features

```bash
# Parse with environment variables
APP_ENV=production tusk parse config.tsk

# Parse with custom context
tusk parse config.tsk --context '{"user_id": 123}'

# Parse multiple files
tusk parse config1.tsk config2.tsk config3.tsk

# Output to file
tusk parse config.tsk --output parsed.json

# Debug mode
tusk parse config.tsk --debug

# Profile performance
tusk profile config.tsk
```

## 🔧 Building CLI Applications

### Basic CLI Application

```go
// cmd/cli/main.go
package main

import (
    "flag"
    "fmt"
    "os"
    "github.com/tusklang/go"
)

func main() {
    // Define command line flags
    configFile := flag.String("config", "config.tsk", "Configuration file path")
    environment := flag.String("env", "development", "Environment")
    debug := flag.Bool("debug", false, "Enable debug mode")
    output := flag.String("output", "", "Output file path")
    
    flag.Parse()
    
    // Set environment variable
    os.Setenv("APP_ENV", *environment)
    
    // Create parser
    parser := tusklanggo.NewEnhancedParser()
    if *debug {
        parser.SetDebug(true)
    }
    
    // Parse configuration
    data, err := parser.ParseFile(*configFile)
    if err != nil {
        fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
        os.Exit(1)
    }
    
    // Output results
    if *output != "" {
        // Write to file
        err = writeToFile(*output, data)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error writing output: %v\n", err)
            os.Exit(1)
        }
        fmt.Printf("Configuration written to %s\n", *output)
    } else {
        // Print to stdout
        printConfiguration(data)
    }
}

func printConfiguration(data map[string]interface{}) {
    fmt.Println("📋 Configuration:")
    for section, values := range data {
        fmt.Printf("\n[%s]\n", section)
        if sectionData, ok := values.(map[string]interface{}); ok {
            for key, value := range sectionData {
                fmt.Printf("  %s: %v\n", key, value)
            }
        }
    }
}

func writeToFile(filename string, data map[string]interface{}) error {
    // Implementation for writing to file
    return nil
}
```

### Advanced CLI with Subcommands

```go
// cmd/cli/main.go
package main

import (
    "fmt"
    "os"
    "github.com/spf13/cobra"
    "github.com/tusklang/go"
)

var rootCmd = &cobra.Command{
    Use:   "myapp",
    Short: "My TuskLang CLI Application",
    Long:  `A powerful CLI application built with TuskLang and Go.`,
}

var parseCmd = &cobra.Command{
    Use:   "parse [file]",
    Short: "Parse a TSK configuration file",
    Args:  cobra.ExactArgs(1),
    Run: func(cmd *cobra.Command, args []string) {
        configFile := args[0]
        debug, _ := cmd.Flags().GetBool("debug")
        output, _ := cmd.Flags().GetString("output")
        
        parser := tusklanggo.NewEnhancedParser()
        if debug {
            parser.SetDebug(true)
        }
        
        data, err := parser.ParseFile(configFile)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error: %v\n", err)
            os.Exit(1)
        }
        
        if output != "" {
            writeToFile(output, data)
        } else {
            printConfiguration(data)
        }
    },
}

var validateCmd = &cobra.Command{
    Use:   "validate [file]",
    Short: "Validate a TSK configuration file",
    Args:  cobra.ExactArgs(1),
    Run: func(cmd *cobra.Command, args []string) {
        configFile := args[0]
        
        parser := tusklanggo.NewEnhancedParser()
        _, err := parser.ParseFile(configFile)
        
        if err != nil {
            fmt.Fprintf(os.Stderr, "❌ Validation failed: %v\n", err)
            os.Exit(1)
        }
        
        fmt.Println("✅ Configuration is valid")
    },
}

var generateCmd = &cobra.Command{
    Use:   "generate [file]",
    Short: "Generate Go structs from TSK configuration",
    Args:  cobra.ExactArgs(1),
    Run: func(cmd *cobra.Command, args []string) {
        configFile := args[0]
        output, _ := cmd.Flags().GetString("output")
        
        parser := tusklanggo.NewEnhancedParser()
        data, err := parser.ParseFile(configFile)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error: %v\n", err)
            os.Exit(1)
        }
        
        structs := generateGoStructs(data)
        
        if output != "" {
            writeToFile(output, structs)
        } else {
            fmt.Println(structs)
        }
    },
}

func init() {
    // Add flags to parse command
    parseCmd.Flags().Bool("debug", false, "Enable debug mode")
    parseCmd.Flags().String("output", "", "Output file path")
    
    // Add flags to generate command
    generateCmd.Flags().String("output", "", "Output file path")
    
    // Add subcommands
    rootCmd.AddCommand(parseCmd)
    rootCmd.AddCommand(validateCmd)
    rootCmd.AddCommand(generateCmd)
}

func main() {
    if err := rootCmd.Execute(); err != nil {
        fmt.Fprintf(os.Stderr, "Error: %v\n", err)
        os.Exit(1)
    }
}

func generateGoStructs(data map[string]interface{}) string {
    // Implementation for generating Go structs
    return "// Generated Go structs..."
}
```

## 🛠️ CLI Development Tools

### Configuration Management CLI

```go
// cmd/config/main.go
package main

import (
    "fmt"
    "os"
    "github.com/spf13/cobra"
    "github.com/tusklang/go"
)

var configCmd = &cobra.Command{
    Use:   "config",
    Short: "Manage TuskLang configurations",
}

var getCmd = &cobra.Command{
    Use:   "get [key]",
    Short: "Get a configuration value",
    Args:  cobra.ExactArgs(1),
    Run: func(cmd *cobra.Command, args []string) {
        key := args[0]
        configFile, _ := cmd.Flags().GetString("file")
        
        parser := tusklanggo.NewEnhancedParser()
        data, err := parser.ParseFile(configFile)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error: %v\n", err)
            os.Exit(1)
        }
        
        value := getNestedValue(data, key)
        if value != nil {
            fmt.Println(value)
        } else {
            fmt.Fprintf(os.Stderr, "Key '%s' not found\n", key)
            os.Exit(1)
        }
    },
}

var setCmd = &cobra.Command{
    Use:   "set [key] [value]",
    Short: "Set a configuration value",
    Args:  cobra.ExactArgs(2),
    Run: func(cmd *cobra.Command, args []string) {
        key := args[0]
        value := args[1]
        configFile, _ := cmd.Flags().GetString("file")
        
        parser := tusklanggo.NewEnhancedParser()
        data, err := parser.ParseFile(configFile)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error: %v\n", err)
            os.Exit(1)
        }
        
        setNestedValue(data, key, value)
        
        err = writeTSKFile(configFile, data)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error writing file: %v\n", err)
            os.Exit(1)
        }
        
        fmt.Printf("Set %s = %s\n", key, value)
    },
}

var listCmd = &cobra.Command{
    Use:   "list",
    Short: "List all configuration keys",
    Run: func(cmd *cobra.Command, args []string) {
        configFile, _ := cmd.Flags().GetString("file")
        
        parser := tusklanggo.NewEnhancedParser()
        data, err := parser.ParseFile(configFile)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error: %v\n", err)
            os.Exit(1)
        }
        
        listKeys(data, "")
    },
}

func init() {
    configCmd.Flags().String("file", "config.tsk", "Configuration file path")
    getCmd.Flags().String("file", "config.tsk", "Configuration file path")
    setCmd.Flags().String("file", "config.tsk", "Configuration file path")
    listCmd.Flags().String("file", "config.tsk", "Configuration file path")
    
    configCmd.AddCommand(getCmd)
    configCmd.AddCommand(setCmd)
    configCmd.AddCommand(listCmd)
}

func getNestedValue(data map[string]interface{}, key string) interface{} {
    // Implementation for getting nested values
    return nil
}

func setNestedValue(data map[string]interface{}, key string, value interface{}) {
    // Implementation for setting nested values
}

func listKeys(data map[string]interface{}, prefix string) {
    // Implementation for listing keys
}

func writeTSKFile(filename string, data map[string]interface{}) error {
    // Implementation for writing TSK files
    return nil
}
```

### Database CLI Tools

```go
// cmd/db/main.go
package main

import (
    "fmt"
    "os"
    "github.com/spf13/cobra"
    "github.com/tusklang/go"
    "github.com/tusklang/go/adapters"
)

var dbCmd = &cobra.Command{
    Use:   "db",
    Short: "Database operations with TuskLang",
}

var queryCmd = &cobra.Command{
    Use:   "query [sql]",
    Short: "Execute a database query",
    Args:  cobra.MinimumNArgs(1),
    Run: func(cmd *cobra.Command, args []string) {
        sql := args[0]
        configFile, _ := cmd.Flags().GetString("config")
        
        // Load configuration
        parser := tusklanggo.NewEnhancedParser()
        data, err := parser.ParseFile(configFile)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error loading config: %v\n", err)
            os.Exit(1)
        }
        
        // Create database adapter
        dbConfig := data["database"].(map[string]interface{})
        adapter, err := adapters.NewPostgreSQLAdapter(adapters.PostgreSQLConfig{
            Host:     dbConfig["host"].(string),
            Port:     dbConfig["port"].(int),
            Database: dbConfig["name"].(string),
            User:     dbConfig["user"].(string),
            Password: dbConfig["password"].(string),
        })
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error connecting to database: %v\n", err)
            os.Exit(1)
        }
        
        // Execute query
        results, err := adapter.Query(sql)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error executing query: %v\n", err)
            os.Exit(1)
        }
        
        // Display results
        printResults(results)
    },
}

var migrateCmd = &cobra.Command{
    Use:   "migrate",
    Short: "Run database migrations",
    Run: func(cmd *cobra.Command, args []string) {
        configFile, _ := cmd.Flags().GetString("config")
        migrationsDir, _ := cmd.Flags().GetString("dir")
        
        // Load configuration
        parser := tusklanggo.NewEnhancedParser()
        data, err := parser.ParseFile(configFile)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error loading config: %v\n", err)
            os.Exit(1)
        }
        
        // Run migrations
        err = runMigrations(data, migrationsDir)
        if err != nil {
            fmt.Fprintf(os.Stderr, "Error running migrations: %v\n", err)
            os.Exit(1)
        }
        
        fmt.Println("✅ Migrations completed successfully")
    },
}

func init() {
    dbCmd.Flags().String("config", "config.tsk", "Configuration file path")
    queryCmd.Flags().String("config", "config.tsk", "Configuration file path")
    migrateCmd.Flags().String("config", "config.tsk", "Configuration file path")
    migrateCmd.Flags().String("dir", "migrations", "Migrations directory")
    
    dbCmd.AddCommand(queryCmd)
    dbCmd.AddCommand(migrateCmd)
}

func printResults(results []map[string]interface{}) {
    if len(results) == 0 {
        fmt.Println("No results")
        return
    }
    
    // Print headers
    headers := make([]string, 0)
    for key := range results[0] {
        headers = append(headers, key)
    }
    
    fmt.Printf("| %-20s |\n", "---")
    for _, header := range headers {
        fmt.Printf("| %-20s |", header)
    }
    fmt.Println()
    
    // Print data
    for _, row := range results {
        for _, header := range headers {
            fmt.Printf("| %-20v |", row[header])
        }
        fmt.Println()
    }
}

func runMigrations(data map[string]interface{}, migrationsDir string) error {
    // Implementation for running migrations
    return nil
}
```

## 🔍 CLI Testing and Validation

### CLI Testing Framework

```go
// cmd/test/main.go
package main

import (
    "fmt"
    "os"
    "github.com/spf13/cobra"
    "github.com/tusklang/go"
)

var testCmd = &cobra.Command{
    Use:   "test",
    Short: "Test TuskLang configurations",
}

var validateCmd = &cobra.Command{
    Use:   "validate [file]",
    Short: "Validate TSK syntax and structure",
    Args:  cobra.ExactArgs(1),
    Run: func(cmd *cobra.Command, args []string) {
        configFile := args[0]
        strict, _ := cmd.Flags().GetBool("strict")
        
        parser := tusklanggo.NewEnhancedParser()
        data, err := parser.ParseFile(configFile)
        
        if err != nil {
            fmt.Fprintf(os.Stderr, "❌ Validation failed: %v\n", err)
            os.Exit(1)
        }
        
        // Additional validation if strict mode
        if strict {
            err = validateStrict(data)
            if err != nil {
                fmt.Fprintf(os.Stderr, "❌ Strict validation failed: %v\n", err)
                os.Exit(1)
            }
        }
        
        fmt.Println("✅ Configuration is valid")
    },
}

var lintCmd = &cobra.Command{
    Use:   "lint [file]",
    Short: "Lint TSK files for best practices",
    Args:  cobra.ExactArgs(1),
    Run: func(cmd *cobra.Command, args []string) {
        configFile := args[0]
        
        issues := lintTSKFile(configFile)
        
        if len(issues) == 0 {
            fmt.Println("✅ No linting issues found")
        } else {
            fmt.Printf("⚠️  Found %d linting issues:\n", len(issues))
            for _, issue := range issues {
                fmt.Printf("  - %s\n", issue)
            }
            os.Exit(1)
        }
    },
}

var benchmarkCmd = &cobra.Command{
    Use:   "benchmark [file]",
    Short: "Benchmark TSK parsing performance",
    Args:  cobra.ExactArgs(1),
    Run: func(cmd *cobra.Command, args []string) {
        configFile := args[0]
        iterations, _ := cmd.Flags().GetInt("iterations")
        
        parser := tusklanggo.NewEnhancedParser()
        
        // Benchmark parsing
        start := time.Now()
        for i := 0; i < iterations; i++ {
            _, err := parser.ParseFile(configFile)
            if err != nil {
                fmt.Fprintf(os.Stderr, "Error during benchmark: %v\n", err)
                os.Exit(1)
            }
        }
        duration := time.Since(start)
        
        fmt.Printf("📊 Benchmark Results:\n")
        fmt.Printf("  Iterations: %d\n", iterations)
        fmt.Printf("  Total time: %v\n", duration)
        fmt.Printf("  Average time: %v\n", duration/time.Duration(iterations))
        fmt.Printf("  Parses per second: %.2f\n", float64(iterations)/duration.Seconds())
    },
}

func init() {
    validateCmd.Flags().Bool("strict", false, "Enable strict validation")
    benchmarkCmd.Flags().Int("iterations", 1000, "Number of iterations")
    
    testCmd.AddCommand(validateCmd)
    testCmd.AddCommand(lintCmd)
    testCmd.AddCommand(benchmarkCmd)
}

func validateStrict(data map[string]interface{}) error {
    // Implementation for strict validation
    return nil
}

func lintTSKFile(filename string) []string {
    // Implementation for linting
    return []string{}
}
```

## 🎯 CLI Best Practices

### 1. Command Structure

```go
// Good - Clear command hierarchy
myapp config get database.host
myapp config set database.host localhost
myapp db query "SELECT * FROM users"
myapp test validate config.tsk

// Bad - Unclear commands
myapp get database.host
myapp set database.host localhost
myapp query "SELECT * FROM users"
myapp validate config.tsk
```

### 2. Error Handling

```go
// Good - Proper error handling
if err != nil {
    fmt.Fprintf(os.Stderr, "Error: %v\n", err)
    os.Exit(1)
}

// Bad - Panic on error
if err != nil {
    panic(err)
}
```

### 3. Output Formatting

```go
// Good - Consistent output
fmt.Printf("✅ %s\n", message)
fmt.Printf("❌ %s\n", error)
fmt.Printf("⚠️  %s\n", warning)

// Bad - Inconsistent output
fmt.Println("Success:", message)
fmt.Println("Error:", error)
fmt.Println("Warning:", warning)
```

### 4. Configuration Management

```go
// Good - Use configuration files
configFile, _ := cmd.Flags().GetString("config")
parser := tusklanggo.NewEnhancedParser()
data, err := parser.ParseFile(configFile)

// Bad - Hardcoded values
host := "localhost"
port := 5432
```

## 📊 CLI Examples

### Complete CLI Application

```go
// main.go
package main

import (
    "fmt"
    "os"
    "github.com/spf13/cobra"
    "github.com/tusklang/go"
)

var rootCmd = &cobra.Command{
    Use:   "tuskapp",
    Short: "TuskLang CLI Application",
    Long:  `A powerful CLI application for managing TuskLang configurations.`,
}

var versionCmd = &cobra.Command{
    Use:   "version",
    Short: "Show version information",
    Run: func(cmd *cobra.Command, args []string) {
        fmt.Println("TuskApp v1.0.0")
        fmt.Println("Built with TuskLang Go SDK")
    },
}

func init() {
    rootCmd.AddCommand(versionCmd)
    rootCmd.AddCommand(configCmd)
    rootCmd.AddCommand(dbCmd)
    rootCmd.AddCommand(testCmd)
}

func main() {
    if err := rootCmd.Execute(); err != nil {
        fmt.Fprintf(os.Stderr, "Error: %v\n", err)
        os.Exit(1)
    }
}
```

### Usage Examples

```bash
# Basic usage
./tuskapp version
./tuskapp config get database.host
./tuskapp config set database.host localhost
./tuskapp db query "SELECT COUNT(*) FROM users"
./tuskapp test validate config.tsk

# With flags
./tuskapp config get database.host --file production.tsk
./tuskapp db query "SELECT * FROM users" --config staging.tsk
./tuskapp test benchmark config.tsk --iterations 5000

# Help
./tuskapp --help
./tuskapp config --help
./tuskapp db --help
./tuskapp test --help
```

## 📚 Summary

You've learned:

1. **TuskLang CLI Tools** - Core commands and advanced features
2. **CLI Application Development** - Building command-line tools with Go
3. **Subcommand Architecture** - Organizing complex CLI applications
4. **Configuration Management** - CLI tools for managing TSK files
5. **Database CLI Tools** - Database operations through CLI
6. **Testing and Validation** - CLI tools for testing configurations
7. **Best Practices** - Clean, maintainable CLI applications
8. **Real-World Examples** - Complete CLI application structure

## 🚀 Next Steps

Now that you understand CLI development:

1. **Build Your CLI** - Create custom CLI applications
2. **Integrate with TuskLang** - Use TuskLang in your CLI tools
3. **Add Testing** - Implement CLI testing frameworks
4. **Deploy CLI Tools** - Package and distribute your CLI
5. **Extend Functionality** - Add more advanced CLI features

---

**"We don't bow to any king"** - You now have the power to build sophisticated CLI applications that leverage the full potential of TuskLang in Go! 
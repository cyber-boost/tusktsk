# CLI Directives - Go

## 🎯 What Are CLI Directives?

CLI directives (`#cli`) in TuskLang allow you to define command-line interface commands, arguments, flags, and help text directly in your configuration files. They transform static config into executable CLI logic.

```go
// CLI directives define your entire command-line interface
type CLIConfig struct {
    Commands    map[string]string `tsk:"#cli_commands"`
    Arguments   map[string]string `tsk:"#cli_args"`
    Flags       map[string]string `tsk:"#cli_flags"`
    HelpText    string            `tsk:"#cli_help"`
}
```

## 🚀 Why CLI Directives Matter

### Traditional CLI Development
```go
// Old way - scattered across multiple files
func main() {
    app := cli.NewApp()
    app.Name = "myapp"
    app.Usage = "A sample application"
    
    // Commands defined in code
    app.Commands = []cli.Command{
        {
            Name:  "users",
            Usage: "Manage users",
            Subcommands: []cli.Command{
                {
                    Name:  "list",
                    Usage: "List all users",
                    Action: listUsers,
                },
                {
                    Name:  "create",
                    Usage: "Create a new user",
                    Action: createUser,
                },
            },
        },
    }
    
    app.Run(os.Args)
}
```

### TuskLang CLI Directives - Declarative & Dynamic
```tsk
# cli.tsk - Complete CLI definition
cli_commands: #cli("""
    users -> Manage users
        list -> List all users
        create -> Create a new user
        update -> Update a user
        delete -> Delete a user
    
    posts -> Manage posts
        list -> List all posts
        create -> Create a new post
        publish -> Publish a post
    
    config -> Manage configuration
        show -> Show current config
        set -> Set config value
        reset -> Reset to defaults
""")

cli_flags: #cli("""
    --verbose -> Enable verbose output
    --config -> Configuration file path
    --output -> Output format (json, yaml, table)
    --dry-run -> Show what would be done
""")

cli_help: #cli("""
    A powerful command-line tool for managing users and posts.
    
    Examples:
        myapp users list --output json
        myapp posts create --title "Hello" --content "World"
        myapp config set database.url sqlite://app.db
""")
```

## 📋 CLI Directive Types

### 1. **Command Directives** (`#cli_commands`)
- Command definitions and hierarchy
- Subcommand structure
- Command descriptions
- Action mappings

### 2. **Argument Directives** (`#cli_args`)
- Positional argument definitions
- Argument validation rules
- Default values
- Required vs optional arguments

### 3. **Flag Directives** (`#cli_flags`)
- Flag definitions and types
- Short and long flag names
- Flag descriptions
- Default values

### 4. **Help Directives** (`#cli_help`)
- Help text generation
- Usage examples
- Command descriptions
- Error messages

## 🔧 Basic CLI Directive Syntax

### Simple Command Definition
```tsk
# Basic command with description
list_users: #cli("users list -> List all users")
```

### Command with Arguments
```tsk
# Command with positional arguments
get_user: #cli("users get {id} -> Get user by ID")
```

### Command with Flags
```tsk
# Command with optional flags
create_user: #cli("users create --name {name} --email {email} -> Create a new user")
```

### Multiple Commands
```tsk
# Multiple commands in one directive
user_commands: #cli("""
    users list -> List all users
    users create -> Create a new user
    users update {id} -> Update a user
    users delete {id} -> Delete a user
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for CLI Directives
```go
type CLIConfig struct {
    // Command definitions
    Commands string `tsk:"#cli_commands"`
    
    // Argument definitions
    Arguments string `tsk:"#cli_args"`
    
    // Flag definitions
    Flags string `tsk:"#cli_flags"`
    
    // Help text
    HelpText string `tsk:"#cli_help"`
}
```

### CLI Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/urfave/cli/v2"
    "os"
)

func main() {
    // Load CLI configuration
    config := tusk.LoadConfig("cli.tsk")
    
    var cliConfig CLIConfig
    config.Unmarshal(&cliConfig)
    
    // Create CLI app from directives
    app := tusk.NewCLIApp(cliConfig)
    
    // Run the CLI application
    app.Run(os.Args)
}
```

### Command Handler Implementation
```go
package commands

import (
    "fmt"
    "github.com/urfave/cli/v2"
)

// User management commands
func ListUsers(c *cli.Context) error {
    users := []User{
        {ID: 1, Name: "Alice", Email: "alice@example.com"},
        {ID: 2, Name: "Bob", Email: "bob@example.com"},
    }
    
    // Format output based on flags
    outputFormat := c.String("output")
    switch outputFormat {
    case "json":
        return outputJSON(users)
    case "yaml":
        return outputYAML(users)
    default:
        return outputTable(users)
    }
}

func CreateUser(c *cli.Context) error {
    name := c.String("name")
    email := c.String("email")
    
    if name == "" || email == "" {
        return fmt.Errorf("name and email are required")
    }
    
    user := User{
        ID:    generateID(),
        Name:  name,
        Email: email,
    }
    
    // Create user logic
    if err := saveUser(user); err != nil {
        return fmt.Errorf("failed to create user: %v", err)
    }
    
    fmt.Printf("Created user: %s (%s)\n", user.Name, user.Email)
    return nil
}

func GetUser(c *cli.Context) error {
    userID := c.Args().Get(0)
    if userID == "" {
        return fmt.Errorf("user ID is required")
    }
    
    user := findUserByID(userID)
    if user == nil {
        return fmt.Errorf("user not found")
    }
    
    outputFormat := c.String("output")
    switch outputFormat {
    case "json":
        return outputJSON(user)
    case "yaml":
        return outputYAML(user)
    default:
        return outputTable([]User{user})
    }
}
```

## 🔄 Subcommand Structure

### Nested Command Definition
```tsk
# Nested command structure
nested_commands: #cli("""
    users -> Manage users
        list -> List all users
        create -> Create a new user
        update {id} -> Update a user
        delete {id} -> Delete a user
        
    posts -> Manage posts
        list -> List all posts
        create -> Create a new post
        publish {id} -> Publish a post
        delete {id} -> Delete a post
        
    config -> Manage configuration
        show -> Show current config
        set {key} {value} -> Set config value
        reset -> Reset to defaults
        export -> Export config to file
""")
```

### Go Subcommand Implementation
```go
package commands

import (
    "github.com/urfave/cli/v2"
)

// User subcommands
func UserCommands() []*cli.Command {
    return []*cli.Command{
        {
            Name:  "list",
            Usage: "List all users",
            Action: ListUsers,
            Flags: []cli.Flag{
                &cli.StringFlag{
                    Name:  "output",
                    Usage: "Output format (json, yaml, table)",
                    Value: "table",
                },
            },
        },
        {
            Name:  "create",
            Usage: "Create a new user",
            Action: CreateUser,
            Flags: []cli.Flag{
                &cli.StringFlag{
                    Name:     "name",
                    Usage:    "User name",
                    Required: true,
                },
                &cli.StringFlag{
                    Name:     "email",
                    Usage:    "User email",
                    Required: true,
                },
            },
        },
        {
            Name:  "update",
            Usage: "Update a user",
            Action: UpdateUser,
            Flags: []cli.Flag{
                &cli.StringFlag{
                    Name:  "name",
                    Usage: "New user name",
                },
                &cli.StringFlag{
                    Name:  "email",
                    Usage: "New user email",
                },
            },
        },
        {
            Name:  "delete",
            Usage: "Delete a user",
            Action: DeleteUser,
        },
    }
}

// Post subcommands
func PostCommands() []*cli.Command {
    return []*cli.Command{
        {
            Name:  "list",
            Usage: "List all posts",
            Action: ListPosts,
            Flags: []cli.Flag{
                &cli.StringFlag{
                    Name:  "output",
                    Usage: "Output format (json, yaml, table)",
                    Value: "table",
                },
                &cli.StringFlag{
                    Name:  "author",
                    Usage: "Filter by author",
                },
            },
        },
        {
            Name:  "create",
            Usage: "Create a new post",
            Action: CreatePost,
            Flags: []cli.Flag{
                &cli.StringFlag{
                    Name:     "title",
                    Usage:    "Post title",
                    Required: true,
                },
                &cli.StringFlag{
                    Name:     "content",
                    Usage:    "Post content",
                    Required: true,
                },
                &cli.StringFlag{
                    Name:  "author",
                    Usage: "Post author",
                },
            },
        },
        {
            Name:  "publish",
            Usage: "Publish a post",
            Action: PublishPost,
        },
        {
            Name:  "delete",
            Usage: "Delete a post",
            Action: DeletePost,
        },
    }
}
```

## 🛡️ Argument Validation

### Argument Validation Rules
```tsk
# Argument validation configuration
cli_validation: #cli("""
    user_id -> Must be a positive integer
    email -> Must be a valid email format
    password -> Must be at least 8 characters
    url -> Must be a valid URL
""")
```

### Go Validation Implementation
```go
package validation

import (
    "fmt"
    "net/mail"
    "net/url"
    "strconv"
    "strings"
)

// Validation functions
func ValidateUserID(userID string) error {
    id, err := strconv.Atoi(userID)
    if err != nil {
        return fmt.Errorf("user ID must be a number")
    }
    
    if id <= 0 {
        return fmt.Errorf("user ID must be positive")
    }
    
    return nil
}

func ValidateEmail(email string) error {
    if email == "" {
        return fmt.Errorf("email is required")
    }
    
    _, err := mail.ParseAddress(email)
    if err != nil {
        return fmt.Errorf("invalid email format")
    }
    
    return nil
}

func ValidatePassword(password string) error {
    if len(password) < 8 {
        return fmt.Errorf("password must be at least 8 characters")
    }
    
    if !strings.ContainsAny(password, "ABCDEFGHIJKLMNOPQRSTUVWXYZ") {
        return fmt.Errorf("password must contain at least one uppercase letter")
    }
    
    if !strings.ContainsAny(password, "abcdefghijklmnopqrstuvwxyz") {
        return fmt.Errorf("password must contain at least one lowercase letter")
    }
    
    if !strings.ContainsAny(password, "0123456789") {
        return fmt.Errorf("password must contain at least one number")
    }
    
    return nil
}

func ValidateURL(urlStr string) error {
    if urlStr == "" {
        return fmt.Errorf("URL is required")
    }
    
    _, err := url.ParseRequestURI(urlStr)
    if err != nil {
        return fmt.Errorf("invalid URL format")
    }
    
    return nil
}
```

## ⚡ Output Formatting

### Output Format Configuration
```tsk
# Output format configuration
cli_output: #cli("""
    json -> JSON format output
    yaml -> YAML format output
    table -> Table format output
    csv -> CSV format output
    xml -> XML format output
""")
```

### Go Output Implementation
```go
package output

import (
    "encoding/csv"
    "encoding/json"
    "encoding/xml"
    "fmt"
    "os"
    "text/tabwriter"
    "gopkg.in/yaml.v2"
)

// Output formatters
func OutputJSON(data interface{}) error {
    encoder := json.NewEncoder(os.Stdout)
    encoder.SetIndent("", "  ")
    return encoder.Encode(data)
}

func OutputYAML(data interface{}) error {
    encoder := yaml.NewEncoder(os.Stdout)
    defer encoder.Close()
    return encoder.Encode(data)
}

func OutputTable(headers []string, rows [][]string) error {
    w := tabwriter.NewWriter(os.Stdout, 0, 0, 2, ' ', 0)
    
    // Write headers
    for i, header := range headers {
        if i > 0 {
            fmt.Fprint(w, "\t")
        }
        fmt.Fprint(w, header)
    }
    fmt.Fprintln(w)
    
    // Write separator
    for i := range headers {
        if i > 0 {
            fmt.Fprint(w, "\t")
        }
        fmt.Fprint(w, "---")
    }
    fmt.Fprintln(w)
    
    // Write rows
    for _, row := range rows {
        for i, cell := range row {
            if i > 0 {
                fmt.Fprint(w, "\t")
            }
            fmt.Fprint(w, cell)
        }
        fmt.Fprintln(w)
    }
    
    return w.Flush()
}

func OutputCSV(headers []string, rows [][]string) error {
    w := csv.NewWriter(os.Stdout)
    defer w.Flush()
    
    // Write headers
    if err := w.Write(headers); err != nil {
        return err
    }
    
    // Write rows
    return w.WriteAll(rows)
}

func OutputXML(data interface{}) error {
    encoder := xml.NewEncoder(os.Stdout)
    encoder.Indent("", "  ")
    return encoder.Encode(data)
}
```

## 🔧 Error Handling

### CLI Error Configuration
```tsk
# CLI error handling configuration
cli_errors: #cli("""
    validation_error -> Validation failed
    not_found -> Resource not found
    permission_denied -> Permission denied
    network_error -> Network connection failed
    internal_error -> Internal application error
""")
```

### Go Error Handler Implementation
```go
package errors

import (
    "fmt"
    "os"
)

// CLI error types
type CLIError struct {
    Type    string
    Message string
    Details map[string]interface{}
}

func (e CLIError) Error() string {
    return e.Message
}

// Error handlers
func HandleValidationError(err error) {
    fmt.Fprintf(os.Stderr, "Validation error: %v\n", err)
    os.Exit(1)
}

func HandleNotFoundError(resource string) {
    fmt.Fprintf(os.Stderr, "Resource not found: %s\n", resource)
    os.Exit(1)
}

func HandlePermissionError(operation string) {
    fmt.Fprintf(os.Stderr, "Permission denied: %s\n", operation)
    os.Exit(1)
}

func HandleNetworkError(err error) {
    fmt.Fprintf(os.Stderr, "Network error: %v\n", err)
    os.Exit(1)
}

func HandleInternalError(err error) {
    fmt.Fprintf(os.Stderr, "Internal error: %v\n", err)
    os.Exit(1)
}
```

## 🎯 Real-World Example

### Complete CLI Configuration
```tsk
# cli-config.tsk - Complete CLI configuration

# Application metadata
app_name: #env("APP_NAME", "myapp")
app_version: #env("APP_VERSION", "1.0.0")
app_description: #env("APP_DESCRIPTION", "A powerful CLI tool")

# Command definitions
commands: #cli("""
    users -> Manage users
        list -> List all users
        create -> Create a new user
        update {id} -> Update a user
        delete {id} -> Delete a user
        search {query} -> Search users
    
    posts -> Manage posts
        list -> List all posts
        create -> Create a new post
        update {id} -> Update a post
        delete {id} -> Delete a post
        publish {id} -> Publish a post
        draft {id} -> Mark as draft
    
    config -> Manage configuration
        show -> Show current config
        set {key} {value} -> Set config value
        get {key} -> Get config value
        reset -> Reset to defaults
        export -> Export config to file
        import {file} -> Import config from file
    
    db -> Database operations
        migrate -> Run database migrations
        seed -> Seed database with data
        backup -> Create database backup
        restore {file} -> Restore from backup
        status -> Show database status
""")

# Global flags
global_flags: #cli("""
    --verbose -> Enable verbose output
    --quiet -> Suppress output
    --config -> Configuration file path
    --output -> Output format (json, yaml, table, csv)
    --dry-run -> Show what would be done
    --force -> Skip confirmation prompts
""")

# User command flags
user_flags: #cli("""
    --name -> User name
    --email -> User email
    --role -> User role (admin, user, guest)
    --active -> User active status
    --created-after -> Filter by creation date
    --limit -> Limit number of results
""")

# Post command flags
post_flags: #cli("""
    --title -> Post title
    --content -> Post content
    --author -> Post author
    --tags -> Post tags (comma-separated)
    --published -> Published status
    --category -> Post category
""")

# Help text
help_text: #cli("""
    A powerful command-line tool for managing users and posts.
    
    Examples:
        myapp users list --output json
        myapp users create --name "Alice" --email "alice@example.com"
        myapp posts create --title "Hello" --content "World" --author "Alice"
        myapp config set database.url sqlite://app.db
        myapp db migrate --dry-run
    
    For more information, visit: https://example.com/docs
""")

# Error messages
error_messages: #cli("""
    validation_error -> Validation failed: {details}
    not_found -> Resource not found: {resource}
    permission_denied -> Permission denied: {operation}
    network_error -> Network connection failed: {details}
    internal_error -> Internal application error: {details}
""")
```

### Go CLI Application Implementation
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/urfave/cli/v2"
    "os"
)

type CLIConfig struct {
    AppName        string `tsk:"app_name"`
    AppVersion     string `tsk:"app_version"`
    AppDescription string `tsk:"app_description"`
    Commands       string `tsk:"commands"`
    GlobalFlags    string `tsk:"global_flags"`
    UserFlags      string `tsk:"user_flags"`
    PostFlags      string `tsk:"post_flags"`
    HelpText       string `tsk:"help_text"`
    ErrorMessages  string `tsk:"error_messages"`
}

func main() {
    // Load CLI configuration
    config := tusk.LoadConfig("cli-config.tsk")
    
    var cliConfig CLIConfig
    if err := config.Unmarshal(&cliConfig); err != nil {
        log.Fatal("Failed to load CLI config:", err)
    }
    
    // Create CLI application from directives
    app := tusk.NewCLIApp(cliConfig)
    
    // Register command handlers
    registerCommandHandlers(app)
    
    // Run the CLI application
    if err := app.Run(os.Args); err != nil {
        log.Fatal("CLI application failed:", err)
    }
}

func registerCommandHandlers(app *cli.App) {
    // Register user commands
    app.Commands = append(app.Commands, commands.UserCommands()...)
    
    // Register post commands
    app.Commands = append(app.Commands, commands.PostCommands()...)
    
    // Register config commands
    app.Commands = append(app.Commands, commands.ConfigCommands()...)
    
    // Register database commands
    app.Commands = append(app.Commands, commands.DatabaseCommands()...)
}
```

## 🎯 Best Practices

### 1. **Use Consistent Command Patterns**
```tsk
# Consistent command patterns
command_patterns: #cli("""
    {resource} list -> List all {resource}
    {resource} create -> Create a new {resource}
    {resource} get {id} -> Get {resource} by ID
    {resource} update {id} -> Update {resource}
    {resource} delete {id} -> Delete {resource}
""")
```

### 2. **Implement Proper Validation**
```go
// Input validation for CLI commands
func validateUserInput(name, email string) error {
    if name == "" {
        return fmt.Errorf("name is required")
    }
    
    if email == "" {
        return fmt.Errorf("email is required")
    }
    
    if !isValidEmail(email) {
        return fmt.Errorf("invalid email format")
    }
    
    return nil
}
```

### 3. **Use Environment-Specific Configuration**
```tsk
# Different output formats for different environments
output_format: #if(
    #env("ENVIRONMENT") == "production",
    "json",
    "table"
)
```

### 4. **Implement Comprehensive Help**
```go
// Detailed help text for commands
func generateCommandHelp(command string) string {
    helpTexts := map[string]string{
        "users": `
Manage users in the system.

Commands:
  list     List all users
  create   Create a new user
  update   Update an existing user
  delete   Delete a user

Examples:
  myapp users list --output json
  myapp users create --name "Alice" --email "alice@example.com"
  myapp users update 1 --name "Alice Smith"
`,
        "posts": `
Manage posts in the system.

Commands:
  list     List all posts
  create   Create a new post
  update   Update an existing post
  delete   Delete a post
  publish  Publish a post

Examples:
  myapp posts list --author "Alice"
  myapp posts create --title "Hello" --content "World"
  myapp posts publish 1
`,
    }
    
    return helpTexts[command]
}
```

## 🎯 Summary

CLI directives in TuskLang provide a powerful, declarative way to define command-line interfaces. They enable:

- **Declarative command definitions** that are easy to understand and maintain
- **Consistent command patterns** across different resources
- **Built-in validation** and error handling
- **Flexible output formatting** for different use cases
- **Comprehensive help system** with examples and documentation

The Go SDK seamlessly integrates CLI directives with existing Go CLI frameworks, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore cron directives, middleware directives, and other specialized directive types in the following guides. 
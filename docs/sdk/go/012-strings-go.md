# 📝 TuskLang Go Strings Guide

**"We don't bow to any king" - Go Edition**

Master string handling in TuskLang and learn how to work with strings effectively in Go applications. This guide covers string syntax, interpolation, validation, and advanced string operations.

## 🎯 String Fundamentals

### Basic String Syntax

```go
// config.tsk
[strings]
simple: "Hello World"
quoted: "This is a 'quoted' string"
double_quoted: "This is a \"double quoted\" string"
escaped: "Line 1\nLine 2\tTabbed"
unicode: "Hello 世界"
empty: ""
multiline: """
This is a multiline string
that spans multiple lines
with proper formatting
"""
```

### Go Struct Mapping

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

type StringConfig struct {
    Simple      string `tsk:"simple"`       // Simple string
    Quoted      string `tsk:"quoted"`       // String with quotes
    DoubleQuoted string `tsk:"double_quoted"` // String with double quotes
    Escaped     string `tsk:"escaped"`      // String with escapes
    Unicode     string `tsk:"unicode"`      // Unicode string
    Empty       string `tsk:"empty"`        // Empty string
    Multiline   string `tsk:"multiline"`    // Multiline string
}

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var config StringConfig
    err = tusklanggo.UnmarshalTSK(data["strings"].(map[string]interface{}), &config)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Simple: %s\n", config.Simple)
    fmt.Printf("Quoted: %s\n", config.Quoted)
    fmt.Printf("Double Quoted: %s\n", config.DoubleQuoted)
    fmt.Printf("Escaped: %s\n", config.Escaped)
    fmt.Printf("Unicode: %s\n", config.Unicode)
    fmt.Printf("Empty: '%s'\n", config.Empty)
    fmt.Printf("Multiline:\n%s\n", config.Multiline)
}
```

## 🔗 String Interpolation

### Variable Interpolation

```go
// config.tsk
$app_name: "My Application"
$version: "1.0.0"
$environment: "production"

[app]
name: $app_name
version: $version
environment: $environment
full_name: "$app_name v$version"
description: "Running $app_name v$version in $environment mode"
```

### Environment Variable Interpolation

```go
// config.tsk
[environment]
app_name: @env("APP_NAME", "Default App")
user_name: @env("USER", "unknown")
home_dir: @env("HOME", "/home/user")
path: @env("PATH", "/usr/bin:/usr/local/bin")

[paths]
config_file: "$home_dir/.config/$app_name/config.tsk"
log_file: "/var/log/$app_name/app.log"
data_dir: "/var/lib/$app_name/data"
```

### Dynamic String Construction

```go
// config.tsk
[urls]
base_url: @env("BASE_URL", "https://api.example.com")
api_version: "v1"
endpoint: "users"

[api]
users_url: "$base_url/$api_version/$endpoint"
user_url: "$base_url/$api_version/$endpoint/{id}"
search_url: "$base_url/$api_version/$endpoint?q={query}"
```

```go
// main.go
type URLConfig struct {
    BaseURL    string `tsk:"base_url"`
    APIVersion string `tsk:"api_version"`
    Endpoint   string `tsk:"endpoint"`
}

type APIConfig struct {
    UsersURL  string `tsk:"users_url"`
    UserURL   string `tsk:"user_url"`
    SearchURL string `tsk:"search_url"`
}

type Config struct {
    URLs URLConfig `tsk:"urls"`
    API  APIConfig `tsk:"api"`
}
```

## 📊 String Validation

### Basic Validation

```go
// main.go
type ValidatedStringConfig struct {
    Email     string `tsk:"email" validate:"email"`
    URL       string `tsk:"url" validate:"url"`
    Phone     string `tsk:"phone" validate:"phone"`
    IPAddress string `tsk:"ip_address" validate:"ip"`
    UUID      string `tsk:"uuid" validate:"uuid"`
}

func validateStrings(config *ValidatedStringConfig) error {
    // Email validation
    if config.Email != "" && !isValidEmail(config.Email) {
        return fmt.Errorf("invalid email: %s", config.Email)
    }
    
    // URL validation
    if config.URL != "" && !isValidURL(config.URL) {
        return fmt.Errorf("invalid URL: %s", config.URL)
    }
    
    // Phone validation
    if config.Phone != "" && !isValidPhone(config.Phone) {
        return fmt.Errorf("invalid phone: %s", config.Phone)
    }
    
    // IP address validation
    if config.IPAddress != "" && !isValidIP(config.IPAddress) {
        return fmt.Errorf("invalid IP address: %s", config.IPAddress)
    }
    
    // UUID validation
    if config.UUID != "" && !isValidUUID(config.UUID) {
        return fmt.Errorf("invalid UUID: %s", config.UUID)
    }
    
    return nil
}

func isValidEmail(email string) bool {
    // Simple email validation
    return strings.Contains(email, "@") && strings.Contains(email, ".")
}

func isValidURL(url string) bool {
    return strings.HasPrefix(url, "http://") || strings.HasPrefix(url, "https://")
}

func isValidPhone(phone string) bool {
    // Simple phone validation
    return len(phone) >= 10 && len(phone) <= 15
}

func isValidIP(ip string) bool {
    // Simple IP validation
    parts := strings.Split(ip, ".")
    if len(parts) != 4 {
        return false
    }
    for _, part := range parts {
        if num, err := strconv.Atoi(part); err != nil || num < 0 || num > 255 {
            return false
        }
    }
    return true
}

func isValidUUID(uuid string) bool {
    // Simple UUID validation
    return len(uuid) == 36 && strings.Count(uuid, "-") == 4
}
```

### Custom Validation

```go
// main.go
type CustomStringConfig struct {
    Username   string `tsk:"username" validate:"min=3,max=20,alphanumeric"`
    Password   string `tsk:"password" validate:"min=8,complex"`
    DomainName string `tsk:"domain_name" validate:"domain"`
    FilePath   string `tsk:"file_path" validate:"filepath"`
}

func validateCustomStrings(config *CustomStringConfig) error {
    // Username validation
    if len(config.Username) < 3 || len(config.Username) > 20 {
        return fmt.Errorf("username must be between 3 and 20 characters")
    }
    
    if !isAlphanumeric(config.Username) {
        return fmt.Errorf("username must be alphanumeric")
    }
    
    // Password validation
    if len(config.Password) < 8 {
        return fmt.Errorf("password must be at least 8 characters")
    }
    
    if !isComplexPassword(config.Password) {
        return fmt.Errorf("password must contain uppercase, lowercase, number, and special character")
    }
    
    // Domain name validation
    if !isValidDomain(config.DomainName) {
        return fmt.Errorf("invalid domain name: %s", config.DomainName)
    }
    
    // File path validation
    if !isValidFilePath(config.FilePath) {
        return fmt.Errorf("invalid file path: %s", config.FilePath)
    }
    
    return nil
}

func isAlphanumeric(s string) bool {
    for _, r := range s {
        if !unicode.IsLetter(r) && !unicode.IsDigit(r) {
            return false
        }
    }
    return true
}

func isComplexPassword(password string) bool {
    hasUpper := false
    hasLower := false
    hasDigit := false
    hasSpecial := false
    
    for _, r := range password {
        switch {
        case unicode.IsUpper(r):
            hasUpper = true
        case unicode.IsLower(r):
            hasLower = true
        case unicode.IsDigit(r):
            hasDigit = true
        case unicode.IsPunct(r) || unicode.IsSymbol(r):
            hasSpecial = true
        }
    }
    
    return hasUpper && hasLower && hasDigit && hasSpecial
}

func isValidDomain(domain string) bool {
    // Simple domain validation
    if len(domain) == 0 || len(domain) > 253 {
        return false
    }
    
    parts := strings.Split(domain, ".")
    if len(parts) < 2 {
        return false
    }
    
    for _, part := range parts {
        if len(part) == 0 || len(part) > 63 {
            return false
        }
        if !isAlphanumeric(part) && !strings.Contains(part, "-") {
            return false
        }
    }
    
    return true
}

func isValidFilePath(path string) bool {
    // Simple file path validation
    if len(path) == 0 {
        return false
    }
    
    // Check for invalid characters
    invalidChars := []string{"<", ">", ":", "\"", "|", "?", "*"}
    for _, char := range invalidChars {
        if strings.Contains(path, char) {
            return false
        }
    }
    
    return true
}
```

## 🔧 String Operations

### String Manipulation

```go
// config.tsk
[strings]
original: "Hello World"
uppercase: @string.upper("Hello World")
lowercase: @string.lower("Hello World")
trimmed: @string.trim("  Hello World  ")
replaced: @string.replace("Hello World", "Hello", "Goodbye")
substring: @string.substring("Hello World", 0, 5)
```

### String Formatting

```go
// config.tsk
[formatted]
timestamp: @date.format(@date.now(), "Y-m-d H:i:s")
user_greeting: @string.format("Hello, {name}! Welcome to {app}", @user.name, @app.name)
file_path: @string.format("/var/log/{app}/{date}.log", @app.name, @date.format(@date.now(), "Y-m-d"))
api_url: @string.format("{base}/api/{version}/{endpoint}", @api.base_url, @api.version, @api.endpoint)
```

### String Concatenation

```go
// config.tsk
[concatenated]
full_name: @string.concat(@user.first_name, " ", @user.last_name)
file_path: @string.concat(@paths.base_dir, "/", @app.name, "/config.tsk")
api_endpoint: @string.concat(@api.base_url, "/", @api.version, "/", @api.resource)
log_message: @string.concat("[", @date.format(@date.now(), "Y-m-d H:i:s"), "] ", @log.level, ": ", @log.message)
```

## 🎯 Advanced String Features

### Template Strings

```go
// config.tsk
[template]
email_template: """
Dear {name},

Thank you for using {app_name} v{version}.

Your account has been successfully created.
Username: {username}
Email: {email}

Best regards,
The {app_name} Team
"""

log_template: """
[{timestamp}] {level}: {message}
File: {file}
Line: {line}
Function: {function}
"""

api_response_template: """
{
  "status": "{status}",
  "message": "{message}",
  "data": {data},
  "timestamp": "{timestamp}"
}
"""
```

### String Functions

```go
// config.tsk
[functions]
reverse_string: """
function reverse(str) {
    return str.split('').reverse().join('');
}
"""

capitalize_words: """
function capitalize(str) {
    return str.replace(/\\b\\w/g, function(l) {
        return l.toUpperCase();
    });
}
"""

slugify: """
function slugify(str) {
    return str.toLowerCase()
        .replace(/[^a-z0-9 -]/g, '')
        .replace(/\\s+/g, '-')
        .replace(/-+/g, '-')
        .trim('-');
}
"""

[processed]
reversed: @fujsen(reverse_string, "Hello World")
capitalized: @fujsen(capitalize_words, "hello world")
slug: @fujsen(slugify, "Hello World! This is a test.")
```

## 🔍 String Parsing

### CSV Parsing

```go
// config.tsk
[csv_data]
users: "john,doe,john@example.com,30\njane,smith,jane@example.com,25\nbob,johnson,bob@example.com,35"
headers: "first_name,last_name,email,age"
separator: ","
```

```go
// main.go
type CSVConfig struct {
    Users     string `tsk:"users"`
    Headers   string `tsk:"headers"`
    Separator string `tsk:"separator"`
}

func parseCSV(config *CSVConfig) ([][]string, error) {
    lines := strings.Split(config.Users, "\n")
    var result [][]string
    
    for _, line := range lines {
        if line == "" {
            continue
        }
        fields := strings.Split(line, config.Separator)
        result = append(result, fields)
    }
    
    return result, nil
}
```

### JSON String Parsing

```go
// config.tsk
[json_data]
user_json: '{"name": "John Doe", "email": "john@example.com", "age": 30}'
settings_json: '{"theme": "dark", "language": "en", "notifications": true}'
```

```go
// main.go
type JSONConfig struct {
    UserJSON     string `tsk:"user_json"`
    SettingsJSON string `tsk:"settings_json"`
}

type User struct {
    Name  string `json:"name"`
    Email string `json:"email"`
    Age   int    `json:"age"`
}

type Settings struct {
    Theme         string `json:"theme"`
    Language      string `json:"language"`
    Notifications bool   `json:"notifications"`
}

func parseJSONStrings(config *JSONConfig) (*User, *Settings, error) {
    var user User
    var settings Settings
    
    if err := json.Unmarshal([]byte(config.UserJSON), &user); err != nil {
        return nil, nil, fmt.Errorf("failed to parse user JSON: %w", err)
    }
    
    if err := json.Unmarshal([]byte(config.SettingsJSON), &settings); err != nil {
        return nil, nil, fmt.Errorf("failed to parse settings JSON: %w", err)
    }
    
    return &user, &settings, nil
}
```

## 🎯 Best Practices

### 1. String Validation

```go
// Good - Validate strings before use
func loadConfig(filename string) (*Config, error) {
    parser := tusklanggo.NewEnhancedParser()
    data, err := parser.ParseFile(filename)
    if err != nil {
        return nil, err
    }
    
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        return nil, err
    }
    
    // Validate strings
    if err := validateStrings(&config); err != nil {
        return nil, err
    }
    
    return &config, nil
}

# Bad - No validation
func loadConfig(filename string) (*Config, error) {
    // Load config without validation
    return config, nil
}
```

### 2. String Interpolation

```go
// Good - Use interpolation for dynamic strings
[app]
name: "My App"
version: "1.0.0"
full_name: "$app.name v$app.version"
description: "Running $app.name in $environment mode"

# Bad - Hardcoded strings
[app]
name: "My App"
version: "1.0.0"
full_name: "My App v1.0.0"
description: "Running My App in production mode"
```

### 3. String Safety

```go
// Good - Sanitize user input
func sanitizeString(input string) string {
    // Remove dangerous characters
    input = strings.ReplaceAll(input, "<", "&lt;")
    input = strings.ReplaceAll(input, ">", "&gt;")
    input = strings.ReplaceAll(input, "\"", "&quot;")
    input = strings.ReplaceAll(input, "'", "&#39;")
    
    return strings.TrimSpace(input)
}

# Bad - Use raw input
func processString(input string) string {
    return input // Dangerous!
}
```

### 4. String Performance

```go
// Good - Use strings.Builder for concatenation
func buildPath(base, app, filename string) string {
    var builder strings.Builder
    builder.WriteString(base)
    builder.WriteString("/")
    builder.WriteString(app)
    builder.WriteString("/")
    builder.WriteString(filename)
    return builder.String()
}

# Bad - String concatenation with +
func buildPath(base, app, filename string) string {
    return base + "/" + app + "/" + filename // Inefficient
}
```

## 📊 Complete Example

### Configuration File

```go
// config.tsk
# ========================================
# STRING CONFIGURATION
# ========================================
[app]
name: "My TuskLang App"
version: "1.0.0"
environment: @env("APP_ENV", "development")
full_name: "$app.name v$app.version"
description: "Running $app.name in $app.environment mode"

[user]
first_name: @env("USER_FIRST_NAME", "John")
last_name: @env("USER_LAST_NAME", "Doe")
email: @env("USER_EMAIL", "john@example.com")
full_name: @string.concat(@user.first_name, " ", @user.last_name)
greeting: @string.format("Hello, {name}! Welcome to {app}", @user.full_name, @app.name)

[paths]
base_dir: @env("BASE_DIR", "/var/lib/myapp")
config_dir: @string.concat(@paths.base_dir, "/config")
log_dir: @string.concat(@paths.base_dir, "/logs")
data_dir: @string.concat(@paths.base_dir, "/data")

[api]
base_url: @env("API_BASE_URL", "https://api.example.com")
version: "v1"
endpoint: "users"
full_url: @string.format("{base}/api/{version}/{endpoint}", @api.base_url, @api.version, @api.endpoint)

[email]
template: """
Dear {name},

Thank you for using {app_name} v{version}.

Your account has been successfully created.
Username: {username}
Email: {email}

Best regards,
The {app_name} Team
"""

[validation]
email_pattern: "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
phone_pattern: "^\\+?[1-9]\\d{1,14}$"
url_pattern: "^(https?://)?([\\da-z.-]+)\\.([a-z.]{2,6})([/\\w .-]*)*/?$"
```

### Go Application

```go
// main.go
package main

import (
    "fmt"
    "log"
    "regexp"
    "strings"
    "github.com/tusklang/go"
)

// Configuration structures
type AppConfig struct {
    Name        string `tsk:"name"`
    Version     string `tsk:"version"`
    Environment string `tsk:"environment"`
    FullName    string `tsk:"full_name"`
    Description string `tsk:"description"`
}

type UserConfig struct {
    FirstName string `tsk:"first_name"`
    LastName  string `tsk:"last_name"`
    Email     string `tsk:"email"`
    FullName  string `tsk:"full_name"`
    Greeting  string `tsk:"greeting"`
}

type PathConfig struct {
    BaseDir   string `tsk:"base_dir"`
    ConfigDir string `tsk:"config_dir"`
    LogDir    string `tsk:"log_dir"`
    DataDir   string `tsk:"data_dir"`
}

type APIConfig struct {
    BaseURL  string `tsk:"base_url"`
    Version  string `tsk:"version"`
    Endpoint string `tsk:"endpoint"`
    FullURL  string `tsk:"full_url"`
}

type EmailConfig struct {
    Template string `tsk:"template"`
}

type ValidationConfig struct {
    EmailPattern string `tsk:"email_pattern"`
    PhonePattern string `tsk:"phone_pattern"`
    URLPattern   string `tsk:"url_pattern"`
}

type Config struct {
    App        AppConfig        `tsk:"app"`
    User       UserConfig       `tsk:"user"`
    Paths      PathConfig       `tsk:"paths"`
    API        APIConfig        `tsk:"api"`
    Email      EmailConfig      `tsk:"email"`
    Validation ValidationConfig `tsk:"validation"`
}

func main() {
    // Load configuration
    config, err := loadConfig("config.tsk")
    if err != nil {
        log.Fatalf("Failed to load configuration: %v", err)
    }
    
    // Validate strings
    if err := validateConfigStrings(config); err != nil {
        log.Fatalf("String validation failed: %v", err)
    }
    
    // Use configuration
    fmt.Printf("🚀 %s\n", config.App.FullName)
    fmt.Printf("📝 %s\n", config.App.Description)
    fmt.Printf("👤 %s\n", config.User.Greeting)
    fmt.Printf("🌐 API: %s\n", config.API.FullURL)
    fmt.Printf("📁 Paths: %s, %s, %s\n", config.Paths.ConfigDir, config.Paths.LogDir, config.Paths.DataDir)
    
    // Process email template
    emailContent := processEmailTemplate(config.Email.Template, config.User, config.App)
    fmt.Printf("📧 Email Template:\n%s\n", emailContent)
}

func loadConfig(filename string) (*Config, error) {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile(filename)
    if err != nil {
        return nil, fmt.Errorf("failed to parse config file: %w", err)
    }
    
    var config Config
    err = tusklanggo.UnmarshalTSK(data, &config)
    if err != nil {
        return nil, fmt.Errorf("failed to unmarshal config: %w", err)
    }
    
    return &config, nil
}

func validateConfigStrings(config *Config) error {
    // Validate email
    if !isValidEmail(config.User.Email) {
        return fmt.Errorf("invalid email: %s", config.User.Email)
    }
    
    // Validate API URL
    if !isValidURL(config.API.BaseURL) {
        return fmt.Errorf("invalid API URL: %s", config.API.BaseURL)
    }
    
    // Validate paths
    if !isValidPath(config.Paths.BaseDir) {
        return fmt.Errorf("invalid base directory: %s", config.Paths.BaseDir)
    }
    
    return nil
}

func isValidEmail(email string) bool {
    pattern := `^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$`
    matched, _ := regexp.MatchString(pattern, email)
    return matched
}

func isValidURL(url string) bool {
    return strings.HasPrefix(url, "http://") || strings.HasPrefix(url, "https://")
}

func isValidPath(path string) bool {
    return len(path) > 0 && !strings.Contains(path, "..")
}

func processEmailTemplate(template string, user UserConfig, app AppConfig) string {
    // Simple template processing
    result := template
    result = strings.ReplaceAll(result, "{name}", user.FullName)
    result = strings.ReplaceAll(result, "{app_name}", app.Name)
    result = strings.ReplaceAll(result, "{version}", app.Version)
    result = strings.ReplaceAll(result, "{username}", user.FirstName)
    result = strings.ReplaceAll(result, "{email}", user.Email)
    return result
}
```

## 📚 Summary

You've learned:

1. **String Fundamentals** - Basic syntax and Go struct mapping
2. **String Interpolation** - Variable and environment variable interpolation
3. **String Validation** - Email, URL, phone, and custom validation
4. **String Operations** - Manipulation, formatting, and concatenation
5. **Advanced Features** - Template strings and FUJSEN functions
6. **String Parsing** - CSV and JSON string parsing
7. **Best Practices** - Validation, safety, and performance
8. **Complete Examples** - Real-world string handling

## 🚀 Next Steps

Now that you understand string handling:

1. **Implement Validation** - Add string validation to your applications
2. **Use Interpolation** - Leverage string interpolation for dynamic content
3. **Create Templates** - Build email and message templates
4. **Parse Data** - Handle CSV and JSON string data
5. **Optimize Performance** - Use efficient string operations

---

**"We don't bow to any king"** - You now have the power to handle strings effectively in your TuskLang Go applications! 
# Templates in TuskLang - Go Guide

## 🎯 **The Power of Dynamic Templates**

In TuskLang, templates aren't just static text—they're living, breathing configurations that adapt to your data. We don't bow to any king, especially not rigid, unchanging templates. TuskLang gives you the power to create dynamic templates that respond to your data and environment.

## 📋 **Table of Contents**
- [Understanding Templates in TuskLang](#understanding-templates-in-tusklang)
- [Template Syntax](#template-syntax)
- [Variable Substitution](#variable-substitution)
- [Conditional Templates](#conditional-templates)
- [Loop Templates](#loop-templates)
- [Go Integration](#go-integration)
- [Template Functions](#template-functions)
- [Performance Considerations](#performance-considerations)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding Templates in TuskLang**

TuskLang templates use dynamic variable substitution and logic:

```go
// TuskLang - Template configuration
[template_config]
welcome_message: "Hello, $name! Welcome to $app_name."
email_template: """
Dear $user_name,

Thank you for registering with $app_name.
Your account ID is: $user_id
Your registration date is: $registration_date

Best regards,
The $app_name Team
"""

api_response: {
    "status": "success",
    "message": "Hello, $name!",
    "timestamp": "$current_time",
    "user_id": "$user_id"
}
```

```go
// Go integration
type TemplateConfig struct {
    WelcomeMessage string                 `tsk:"welcome_message"`
    EmailTemplate  string                 `tsk:"email_template"`
    APIResponse    map[string]interface{} `tsk:"api_response"`
}
```

## 🎨 **Template Syntax**

### **Basic Variable Substitution**

```go
// TuskLang - Basic variable substitution
[basic_templates]
greeting: "Hello, $name!"
full_name: "$first_name $last_name"
email: "$username@$domain.com"
url: "https://$domain.com/$path"
```

```go
// Go - Basic template handling
type BasicTemplates struct {
    Greeting  string `tsk:"greeting"`
    FullName  string `tsk:"full_name"`
    Email     string `tsk:"email"`
    URL       string `tsk:"url"`
}

func (b *BasicTemplates) RenderGreeting(name string) string {
    return strings.ReplaceAll(b.Greeting, "$name", name)
}

func (b *BasicTemplates) RenderFullName(firstName, lastName string) string {
    result := strings.ReplaceAll(b.FullName, "$first_name", firstName)
    return strings.ReplaceAll(result, "$last_name", lastName)
}

func (b *BasicTemplates) RenderEmail(username, domain string) string {
    result := strings.ReplaceAll(b.Email, "$username", username)
    return strings.ReplaceAll(result, "$domain", domain)
}

func (b *BasicTemplates) RenderURL(domain, path string) string {
    result := strings.ReplaceAll(b.URL, "$domain", domain)
    return strings.ReplaceAll(result, "$path", path)
}
```

### **Complex Template Variables**

```go
// TuskLang - Complex template variables
[complex_templates]
user_profile: """
Name: $user.name
Email: $user.email
Age: $user.age
Location: $user.location.city, $user.location.country
"""

order_summary: """
Order ID: $order.id
Customer: $order.customer.name
Items: $order.items.count
Total: $order.total
Status: $order.status
"""
```

```go
// Go - Complex template handling
type ComplexTemplates struct {
    UserProfile  string `tsk:"user_profile"`
    OrderSummary string `tsk:"order_summary"`
}

type User struct {
    Name     string `json:"name"`
    Email    string `json:"email"`
    Age      int    `json:"age"`
    Location Location `json:"location"`
}

type Location struct {
    City    string `json:"city"`
    Country string `json:"country"`
}

type Order struct {
    ID       string   `json:"id"`
    Customer Customer `json:"customer"`
    Items    Items    `json:"items"`
    Total    float64  `json:"total"`
    Status   string   `json:"status"`
}

type Customer struct {
    Name string `json:"name"`
}

type Items struct {
    Count int `json:"count"`
}

func (c *ComplexTemplates) RenderUserProfile(user User) string {
    result := c.UserProfile
    
    result = strings.ReplaceAll(result, "$user.name", user.Name)
    result = strings.ReplaceAll(result, "$user.email", user.Email)
    result = strings.ReplaceAll(result, "$user.age", strconv.Itoa(user.Age))
    result = strings.ReplaceAll(result, "$user.location.city", user.Location.City)
    result = strings.ReplaceAll(result, "$user.location.country", user.Location.Country)
    
    return result
}

func (c *ComplexTemplates) RenderOrderSummary(order Order) string {
    result := c.OrderSummary
    
    result = strings.ReplaceAll(result, "$order.id", order.ID)
    result = strings.ReplaceAll(result, "$order.customer.name", order.Customer.Name)
    result = strings.ReplaceAll(result, "$order.items.count", strconv.Itoa(order.Items.Count))
    result = strings.ReplaceAll(result, "$order.total", fmt.Sprintf("%.2f", order.Total))
    result = strings.ReplaceAll(result, "$order.status", order.Status)
    
    return result
}
```

## 🔄 **Variable Substitution**

### **Simple Variable Replacement**

```go
// TuskLang - Simple variable replacement
[simple_variables]
app_name: "My Application"
version: "1.0.0"
environment: @env("ENVIRONMENT", "development")

welcome_message: "Welcome to $app_name v$version!"
status_message: "Running in $environment mode"
```

```go
// Go - Simple variable replacement
type SimpleVariables struct {
    AppName     string `tsk:"app_name"`
    Version     string `tsk:"version"`
    Environment string `tsk:"environment"`
    WelcomeMessage string `tsk:"welcome_message"`
    StatusMessage string `tsk:"status_message"`
}

func (s *SimpleVariables) RenderWelcomeMessage() string {
    result := strings.ReplaceAll(s.WelcomeMessage, "$app_name", s.AppName)
    return strings.ReplaceAll(result, "$version", s.Version)
}

func (s *SimpleVariables) RenderStatusMessage() string {
    return strings.ReplaceAll(s.StatusMessage, "$environment", s.Environment)
}
```

### **Nested Variable Substitution**

```go
// TuskLang - Nested variable substitution
[nested_variables]
database: {
    host: "localhost"
    port: 5432
    name: "app"
}

connection_string: "postgresql://$database.host:$database.port/$database.name"
api_endpoint: "https://$api.host:$api.port/$api.path"
```

```go
// Go - Nested variable substitution
type NestedVariables struct {
    Database DatabaseConfig `tsk:"database"`
    ConnectionString string `tsk:"connection_string"`
    APIEndpoint string `tsk:"api_endpoint"`
}

type DatabaseConfig struct {
    Host string `tsk:"host"`
    Port int    `tsk:"port"`
    Name string `tsk:"name"`
}

func (n *NestedVariables) RenderConnectionString() string {
    result := strings.ReplaceAll(n.ConnectionString, "$database.host", n.Database.Host)
    result = strings.ReplaceAll(result, "$database.port", strconv.Itoa(n.Database.Port))
    return strings.ReplaceAll(result, "$database.name", n.Database.Name)
}
```

### **Dynamic Variable Substitution**

```go
// TuskLang - Dynamic variable substitution
[dynamic_variables]
current_time: @date.now()
user_id: @env("USER_ID", "anonymous")
session_id: @uuid.generate()

dynamic_message: "Session $session_id for user $user_id at $current_time"
```

```go
// Go - Dynamic variable substitution
type DynamicVariables struct {
    CurrentTime   string `tsk:"current_time"`
    UserID        string `tsk:"user_id"`
    SessionID     string `tsk:"session_id"`
    DynamicMessage string `tsk:"dynamic_message"`
}

func (d *DynamicVariables) RenderDynamicMessage() string {
    result := strings.ReplaceAll(d.DynamicMessage, "$session_id", d.SessionID)
    result = strings.ReplaceAll(result, "$user_id", d.UserID)
    return strings.ReplaceAll(result, "$current_time", d.CurrentTime)
}
```

## 🔀 **Conditional Templates**

### **If-Else Templates**

```go
// TuskLang - Conditional templates
[conditional_templates]
user_greeting: @if(@env("USER_TYPE") == "admin",
    "Welcome, Administrator $name!",
    "Hello, $name!"
)

status_message: @if(@env("DEBUG") == "true",
    "Debug mode enabled. Log level: $log_level",
    "Production mode. Log level: $log_level"
)
```

```go
// Go - Conditional template handling
type ConditionalTemplates struct {
    UserGreeting string `tsk:"user_greeting"`
    StatusMessage string `tsk:"status_message"`
}

func (c *ConditionalTemplates) RenderUserGreeting(name, userType string) string {
    if userType == "admin" {
        return strings.ReplaceAll("Welcome, Administrator $name!", "$name", name)
    }
    return strings.ReplaceAll("Hello, $name!", "$name", name)
}

func (c *ConditionalTemplates) RenderStatusMessage(logLevel string, debug bool) string {
    if debug {
        result := strings.ReplaceAll("Debug mode enabled. Log level: $log_level", "$log_level", logLevel)
        return result
    }
    result := strings.ReplaceAll("Production mode. Log level: $log_level", "$log_level", logLevel)
    return result
}
```

### **Switch Templates**

```go
// TuskLang - Switch templates
[switch_templates]
environment_message: @switch(@env("ENVIRONMENT"),
    "production", "Running in production mode",
    "staging", "Running in staging mode",
    "development", "Running in development mode",
    "Unknown environment"
)

log_level_message: @switch(@env("LOG_LEVEL"),
    "debug", "Debug logging enabled",
    "info", "Info logging enabled",
    "warn", "Warning logging enabled",
    "error", "Error logging enabled",
    "Default logging level"
)
```

```go
// Go - Switch template handling
type SwitchTemplates struct {
    EnvironmentMessage string `tsk:"environment_message"`
    LogLevelMessage    string `tsk:"log_level_message"`
}

func (s *SwitchTemplates) RenderEnvironmentMessage(environment string) string {
    switch environment {
    case "production":
        return "Running in production mode"
    case "staging":
        return "Running in staging mode"
    case "development":
        return "Running in development mode"
    default:
        return "Unknown environment"
    }
}

func (s *SwitchTemplates) RenderLogLevelMessage(logLevel string) string {
    switch logLevel {
    case "debug":
        return "Debug logging enabled"
    case "info":
        return "Info logging enabled"
    case "warn":
        return "Warning logging enabled"
    case "error":
        return "Error logging enabled"
    default:
        return "Default logging level"
    }
}
```

## 🔁 **Loop Templates**

### **Array Loop Templates**

```go
// TuskLang - Array loop templates
[loop_templates]
user_list: @foreach(@env("USERS").split(","), "User: $item")
item_list: @foreach(@env("ITEMS").split(","), "- $item")
tag_list: @foreach(@env("TAGS").split(","), "#$item")
```

```go
// Go - Array loop template handling
type LoopTemplates struct {
    UserList string `tsk:"user_list"`
    ItemList string `tsk:"item_list"`
    TagList  string `tsk:"tag_list"`
}

func (l *LoopTemplates) RenderUserList(users []string) string {
    var result []string
    for _, user := range users {
        result = append(result, strings.ReplaceAll("User: $item", "$item", user))
    }
    return strings.Join(result, "\n")
}

func (l *LoopTemplates) RenderItemList(items []string) string {
    var result []string
    for _, item := range items {
        result = append(result, strings.ReplaceAll("- $item", "$item", item))
    }
    return strings.Join(result, "\n")
}

func (l *LoopTemplates) RenderTagList(tags []string) string {
    var result []string
    for _, tag := range tags {
        result = append(result, strings.ReplaceAll("#$item", "$item", tag))
    }
    return strings.Join(result, " ")
}
```

### **Object Loop Templates**

```go
// TuskLang - Object loop templates
[object_loops]
user_table: @foreach(@env("USERS"), """
| $item.name | $item.email | $item.role |
""")

config_summary: @foreach(@env("CONFIG"), """
$item.key: $item.value
""")
```

```go
// Go - Object loop template handling
type ObjectLoops struct {
    UserTable     string `tsk:"user_table"`
    ConfigSummary string `tsk:"config_summary"`
}

type User struct {
    Name  string `json:"name"`
    Email string `json:"email"`
    Role  string `json:"role"`
}

type ConfigItem struct {
    Key   string `json:"key"`
    Value string `json:"value"`
}

func (o *ObjectLoops) RenderUserTable(users []User) string {
    var result []string
    for _, user := range users {
        row := "| $item.name | $item.email | $item.role |"
        row = strings.ReplaceAll(row, "$item.name", user.Name)
        row = strings.ReplaceAll(row, "$item.email", user.Email)
        row = strings.ReplaceAll(row, "$item.role", user.Role)
        result = append(result, row)
    }
    return strings.Join(result, "\n")
}

func (o *ObjectLoops) RenderConfigSummary(config []ConfigItem) string {
    var result []string
    for _, item := range config {
        row := "$item.key: $item.value"
        row = strings.ReplaceAll(row, "$item.key", item.Key)
        row = strings.ReplaceAll(row, "$item.value", item.Value)
        result = append(result, row)
    }
    return strings.Join(result, "\n")
}
```

## 🔧 **Go Integration**

### **Template Engine**

```go
// Go - Template engine
type TemplateEngine struct {
    templates map[string]string
    variables map[string]interface{}
}

func NewTemplateEngine() *TemplateEngine {
    return &TemplateEngine{
        templates: make(map[string]string),
        variables: make(map[string]interface{}),
    }
}

func (t *TemplateEngine) RegisterTemplate(name, template string) {
    t.templates[name] = template
}

func (t *TemplateEngine) SetVariable(name string, value interface{}) {
    t.variables[name] = value
}

func (t *TemplateEngine) RenderTemplate(name string) (string, error) {
    template, exists := t.templates[name]
    if !exists {
        return "", fmt.Errorf("template '%s' not found", name)
    }
    
    return t.renderString(template), nil
}

func (t *TemplateEngine) renderString(input string) string {
    result := input
    
    // Replace variables
    for name, value := range t.variables {
        placeholder := "$" + name
        result = strings.ReplaceAll(result, placeholder, fmt.Sprintf("%v", value))
    }
    
    return result
}
```

### **Advanced Template Parser**

```go
// Go - Advanced template parser
type AdvancedTemplateParser struct {
    engine *TemplateEngine
}

func NewAdvancedTemplateParser() *AdvancedTemplateParser {
    return &AdvancedTemplateParser{
        engine: NewTemplateEngine(),
    }
}

func (a *AdvancedTemplateParser) ParseTemplate(template string, data map[string]interface{}) (string, error) {
    result := template
    
    // Parse nested variables (e.g., $user.name)
    result = a.parseNestedVariables(result, data)
    
    // Parse conditional statements
    result = a.parseConditionals(result, data)
    
    // Parse loops
    result = a.parseLoops(result, data)
    
    return result, nil
}

func (a *AdvancedTemplateParser) parseNestedVariables(template string, data map[string]interface{}) string {
    result := template
    
    // Find all variable patterns like $user.name
    re := regexp.MustCompile(`\$([a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*)`)
    matches := re.FindAllStringSubmatch(result, -1)
    
    for _, match := range matches {
        fullMatch := match[0]
        variablePath := match[1]
        
        value := a.getNestedValue(data, variablePath)
        result = strings.ReplaceAll(result, fullMatch, fmt.Sprintf("%v", value))
    }
    
    return result
}

func (a *AdvancedTemplateParser) getNestedValue(data map[string]interface{}, path string) interface{} {
    parts := strings.Split(path, ".")
    current := data
    
    for _, part := range parts {
        if val, exists := current[part]; exists {
            if nested, ok := val.(map[string]interface{}); ok {
                current = nested
            } else {
                return val
            }
        } else {
            return ""
        }
    }
    
    return current
}

func (a *AdvancedTemplateParser) parseConditionals(template string, data map[string]interface{}) string {
    // Parse @if statements
    re := regexp.MustCompile(`@if\(([^,]+),([^,]+),([^)]+)\)`)
    matches := re.FindAllStringSubmatch(template, -1)
    
    for _, match := range matches {
        condition := strings.TrimSpace(match[1])
        trueValue := strings.TrimSpace(match[2])
        falseValue := strings.TrimSpace(match[3])
        
        if a.evaluateCondition(condition, data) {
            template = strings.Replace(template, match[0], trueValue, 1)
        } else {
            template = strings.Replace(template, match[0], falseValue, 1)
        }
    }
    
    return template
}

func (a *AdvancedTemplateParser) evaluateCondition(condition string, data map[string]interface{}) bool {
    // Simple condition evaluation
    if strings.Contains(condition, "==") {
        parts := strings.Split(condition, "==")
        if len(parts) == 2 {
            left := strings.TrimSpace(parts[0])
            right := strings.TrimSpace(parts[1])
            
            leftValue := a.getNestedValue(data, left)
            rightValue := strings.Trim(right, "\"")
            
            return fmt.Sprintf("%v", leftValue) == rightValue
        }
    }
    
    return false
}

func (a *AdvancedTemplateParser) parseLoops(template string, data map[string]interface{}) string {
    // Parse @foreach statements
    re := regexp.MustCompile(`@foreach\(([^,]+),([^)]+)\)`)
    matches := re.FindAllStringSubmatch(template, -1)
    
    for _, match := range matches {
        arrayPath := strings.TrimSpace(match[1])
        itemTemplate := strings.TrimSpace(match[2])
        
        array := a.getNestedValue(data, arrayPath)
        if items, ok := array.([]interface{}); ok {
            var results []string
            for _, item := range items {
                itemData := map[string]interface{}{"item": item}
                rendered := a.parseNestedVariables(itemTemplate, itemData)
                results = append(results, rendered)
            }
            template = strings.Replace(template, match[0], strings.Join(results, "\n"), 1)
        }
    }
    
    return template
}
```

## 🚀 **Template Functions**

### **String Functions**

```go
// TuskLang - String functions in templates
[string_functions]
uppercase_name: @upper($name)
lowercase_email: @lower($email)
capitalized_title: @capitalize($title)
trimmed_text: @trim($text)
replaced_text: @replace($text, "old", "new")
```

```go
// Go - String function handling
type StringFunctions struct {
    UppercaseName   string `tsk:"uppercase_name"`
    LowercaseEmail  string `tsk:"lowercase_email"`
    CapitalizedTitle string `tsk:"capitalized_title"`
    TrimmedText     string `tsk:"trimmed_text"`
    ReplacedText    string `tsk:"replaced_text"`
}

func (s *StringFunctions) RenderUppercaseName(name string) string {
    return strings.ToUpper(name)
}

func (s *StringFunctions) RenderLowercaseEmail(email string) string {
    return strings.ToLower(email)
}

func (s *StringFunctions) RenderCapitalizedTitle(title string) string {
    return strings.Title(strings.ToLower(title))
}

func (s *StringFunctions) RenderTrimmedText(text string) string {
    return strings.TrimSpace(text)
}

func (s *StringFunctions) RenderReplacedText(text, old, new string) string {
    return strings.ReplaceAll(text, old, new)
}
```

### **Number Functions**

```go
// TuskLang - Number functions in templates
[number_functions]
formatted_price: @format.number($price, "currency")
rounded_value: @round($value, 2)
percentage: @format.percent($ratio)
scientific: @format.scientific($number)
```

```go
// Go - Number function handling
type NumberFunctions struct {
    FormattedPrice string `tsk:"formatted_price"`
    RoundedValue   string `tsk:"rounded_value"`
    Percentage     string `tsk:"percentage"`
    Scientific     string `tsk:"scientific"`
}

func (n *NumberFunctions) RenderFormattedPrice(price float64) string {
    return fmt.Sprintf("$%.2f", price)
}

func (n *NumberFunctions) RenderRoundedValue(value float64, decimals int) string {
    format := fmt.Sprintf("%%.%df", decimals)
    return fmt.Sprintf(format, value)
}

func (n *NumberFunctions) RenderPercentage(ratio float64) string {
    return fmt.Sprintf("%.1f%%", ratio*100)
}

func (n *NumberFunctions) RenderScientific(number float64) string {
    return fmt.Sprintf("%e", number)
}
```

### **Date Functions**

```go
// TuskLang - Date functions in templates
[date_functions]
formatted_date: @format.date($date, "Y-m-d")
relative_time: @date.relative($timestamp)
age_calculation: @date.age($birth_date)
time_ago: @date.ago($timestamp)
```

```go
// Go - Date function handling
type DateFunctions struct {
    FormattedDate string `tsk:"formatted_date"`
    RelativeTime  string `tsk:"relative_time"`
    AgeCalculation string `tsk:"age_calculation"`
    TimeAgo       string `tsk:"time_ago"`
}

func (d *DateFunctions) RenderFormattedDate(date time.Time) string {
    return date.Format("2006-01-02")
}

func (d *DateFunctions) RenderRelativeTime(timestamp int64) string {
    t := time.Unix(timestamp, 0)
    now := time.Now()
    diff := now.Sub(t)
    
    if diff < time.Minute {
        return "just now"
    } else if diff < time.Hour {
        minutes := int(diff.Minutes())
        return fmt.Sprintf("%d minutes ago", minutes)
    } else if diff < 24*time.Hour {
        hours := int(diff.Hours())
        return fmt.Sprintf("%d hours ago", hours)
    } else {
        days := int(diff.Hours() / 24)
        return fmt.Sprintf("%d days ago", days)
    }
}

func (d *DateFunctions) RenderAgeCalculation(birthDate time.Time) string {
    now := time.Now()
    age := now.Year() - birthDate.Year()
    if now.YearDay() < birthDate.YearDay() {
        age--
    }
    return fmt.Sprintf("%d years old", age)
}

func (d *DateFunctions) RenderTimeAgo(timestamp int64) string {
    return d.RenderRelativeTime(timestamp)
}
```

## ⚡ **Performance Considerations**

### **Template Caching**

```go
// Go - Template caching system
type TemplateCache struct {
    cache map[string]string
    mutex sync.RWMutex
    ttl   time.Duration
}

func NewTemplateCache(ttl time.Duration) *TemplateCache {
    return &TemplateCache{
        cache: make(map[string]string),
        ttl:   ttl,
    }
}

func (t *TemplateCache) Get(key string) (string, bool) {
    t.mutex.RLock()
    defer t.mutex.RUnlock()
    
    value, exists := t.cache[key]
    return value, exists
}

func (t *TemplateCache) Set(key, value string) {
    t.mutex.Lock()
    defer t.mutex.Unlock()
    
    t.cache[key] = value
    
    // Schedule cleanup
    go func() {
        time.Sleep(t.ttl)
        t.mutex.Lock()
        delete(t.cache, key)
        t.mutex.Unlock()
    }()
}

func (t *TemplateCache) Clear() {
    t.mutex.Lock()
    defer t.mutex.Unlock()
    
    t.cache = make(map[string]string)
}
```

### **Lazy Template Evaluation**

```go
// Go - Lazy template evaluation
type LazyTemplate struct {
    cache *TemplateCache
    parser *AdvancedTemplateParser
}

func NewLazyTemplate(ttl time.Duration) *LazyTemplate {
    return &LazyTemplate{
        cache:  NewTemplateCache(ttl),
        parser: NewAdvancedTemplateParser(),
    }
}

func (l *LazyTemplate) Render(template string, data map[string]interface{}) (string, error) {
    // Generate cache key
    cacheKey := l.generateCacheKey(template, data)
    
    // Check cache first
    if cached, exists := l.cache.Get(cacheKey); exists {
        return cached, nil
    }
    
    // Render template
    result, err := l.parser.ParseTemplate(template, data)
    if err != nil {
        return "", err
    }
    
    // Cache the result
    l.cache.Set(cacheKey, result)
    
    return result, nil
}

func (l *LazyTemplate) generateCacheKey(template string, data map[string]interface{}) string {
    // Simple cache key generation
    jsonData, _ := json.Marshal(data)
    return fmt.Sprintf("%s:%s", template, string(jsonData))
}
```

## 🌍 **Real-World Examples**

### **Email Template System**

```go
// TuskLang - Email template system
[email_templates]
welcome_email: """
Subject: Welcome to $app_name, $user_name!

Dear $user_name,

Welcome to $app_name! We're excited to have you on board.

Your account details:
- Username: $username
- Email: $email
- Account ID: $user_id
- Registration Date: $registration_date

If you have any questions, please don't hesitate to contact our support team.

Best regards,
The $app_name Team
"""

password_reset: """
Subject: Password Reset Request

Dear $user_name,

You requested a password reset for your $app_name account.

Click the following link to reset your password:
$reset_link

This link will expire in $expiry_hours hours.

If you didn't request this reset, please ignore this email.

Best regards,
The $app_name Team
"""
```

```go
// Go - Email template system
type EmailTemplates struct {
    WelcomeEmail   string `tsk:"welcome_email"`
    PasswordReset  string `tsk:"password_reset"`
}

type UserData struct {
    UserName         string `json:"user_name"`
    Username         string `json:"username"`
    Email            string `json:"email"`
    UserID           string `json:"user_id"`
    RegistrationDate string `json:"registration_date"`
}

type ResetData struct {
    UserName      string `json:"user_name"`
    ResetLink     string `json:"reset_link"`
    ExpiryHours   int    `json:"expiry_hours"`
}

func (e *EmailTemplates) RenderWelcomeEmail(user UserData, appName string) string {
    data := map[string]interface{}{
        "app_name":          appName,
        "user_name":         user.UserName,
        "username":          user.Username,
        "email":             user.Email,
        "user_id":           user.UserID,
        "registration_date": user.RegistrationDate,
    }
    
    parser := NewAdvancedTemplateParser()
    result, _ := parser.ParseTemplate(e.WelcomeEmail, data)
    return result
}

func (e *EmailTemplates) RenderPasswordReset(reset ResetData, appName string) string {
    data := map[string]interface{}{
        "app_name":      appName,
        "user_name":     reset.UserName,
        "reset_link":    reset.ResetLink,
        "expiry_hours":  reset.ExpiryHours,
    }
    
    parser := NewAdvancedTemplateParser()
    result, _ := parser.ParseTemplate(e.PasswordReset, data)
    return result
}
```

### **API Response Templates**

```go
// TuskLang - API response templates
[api_templates]
success_response: {
    "status": "success",
    "message": "$message",
    "data": $data,
    "timestamp": "$timestamp",
    "request_id": "$request_id"
}

error_response: {
    "status": "error",
    "message": "$message",
    "error_code": "$error_code",
    "timestamp": "$timestamp",
    "request_id": "$request_id"
}

user_response: {
    "status": "success",
    "data": {
        "user": {
            "id": "$user.id",
            "name": "$user.name",
            "email": "$user.email",
            "created_at": "$user.created_at"
        }
    },
    "timestamp": "$timestamp"
}
```

```go
// Go - API response template system
type APITemplates struct {
    SuccessResponse string `tsk:"success_response"`
    ErrorResponse   string `tsk:"error_response"`
    UserResponse    string `tsk:"user_response"`
}

type APIResponse struct {
    Status    string                 `json:"status"`
    Message   string                 `json:"message"`
    Data      interface{}            `json:"data,omitempty"`
    ErrorCode string                 `json:"error_code,omitempty"`
    Timestamp string                 `json:"timestamp"`
    RequestID string                 `json:"request_id"`
}

type User struct {
    ID        string `json:"id"`
    Name      string `json:"name"`
    Email     string `json:"email"`
    CreatedAt string `json:"created_at"`
}

func (a *APITemplates) RenderSuccessResponse(message string, data interface{}, requestID string) (*APIResponse, error) {
    templateData := map[string]interface{}{
        "message":    message,
        "data":       data,
        "timestamp":  time.Now().Format(time.RFC3339),
        "request_id": requestID,
    }
    
    parser := NewAdvancedTemplateParser()
    result, err := parser.ParseTemplate(a.SuccessResponse, templateData)
    if err != nil {
        return nil, err
    }
    
    var response APIResponse
    err = json.Unmarshal([]byte(result), &response)
    if err != nil {
        return nil, err
    }
    
    return &response, nil
}

func (a *APITemplates) RenderErrorResponse(message, errorCode, requestID string) (*APIResponse, error) {
    templateData := map[string]interface{}{
        "message":     message,
        "error_code":  errorCode,
        "timestamp":   time.Now().Format(time.RFC3339),
        "request_id":  requestID,
    }
    
    parser := NewAdvancedTemplateParser()
    result, err := parser.ParseTemplate(a.ErrorResponse, templateData)
    if err != nil {
        return nil, err
    }
    
    var response APIResponse
    err = json.Unmarshal([]byte(result), &response)
    if err != nil {
        return nil, err
    }
    
    return &response, nil
}

func (a *APITemplates) RenderUserResponse(user User, requestID string) (*APIResponse, error) {
    templateData := map[string]interface{}{
        "user": map[string]interface{}{
            "id":         user.ID,
            "name":       user.Name,
            "email":      user.Email,
            "created_at": user.CreatedAt,
        },
        "timestamp":  time.Now().Format(time.RFC3339),
        "request_id": requestID,
    }
    
    parser := NewAdvancedTemplateParser()
    result, err := parser.ParseTemplate(a.UserResponse, templateData)
    if err != nil {
        return nil, err
    }
    
    var response APIResponse
    err = json.Unmarshal([]byte(result), &response)
    if err != nil {
        return nil, err
    }
    
    return &response, nil
}
```

### **Configuration Template System**

```go
// TuskLang - Configuration template system
[config_templates]
nginx_config: """
server {
    listen $port;
    server_name $domain;
    
    location / {
        proxy_pass http://$backend_host:$backend_port;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    location /static {
        alias $static_path;
        expires $cache_expiry;
    }
}
"""

docker_compose: """
version: '3.8'
services:
  app:
    image: $app_image
    ports:
      - "$host_port:$container_port"
    environment:
      - DATABASE_URL=$database_url
      - API_KEY=$api_key
    volumes:
      - $data_path:/app/data
"""
```

```go
// Go - Configuration template system
type ConfigTemplates struct {
    NginxConfig  string `tsk:"nginx_config"`
    DockerCompose string `tsk:"docker_compose"`
}

type NginxConfig struct {
    Port         int    `json:"port"`
    Domain       string `json:"domain"`
    BackendHost  string `json:"backend_host"`
    BackendPort  int    `json:"backend_port"`
    StaticPath   string `json:"static_path"`
    CacheExpiry  string `json:"cache_expiry"`
}

type DockerComposeConfig struct {
    AppImage      string `json:"app_image"`
    HostPort      int    `json:"host_port"`
    ContainerPort int    `json:"container_port"`
    DatabaseURL   string `json:"database_url"`
    APIKey        string `json:"api_key"`
    DataPath      string `json:"data_path"`
}

func (c *ConfigTemplates) RenderNginxConfig(config NginxConfig) string {
    data := map[string]interface{}{
        "port":          config.Port,
        "domain":        config.Domain,
        "backend_host":  config.BackendHost,
        "backend_port":  config.BackendPort,
        "static_path":   config.StaticPath,
        "cache_expiry":  config.CacheExpiry,
    }
    
    parser := NewAdvancedTemplateParser()
    result, _ := parser.ParseTemplate(c.NginxConfig, data)
    return result
}

func (c *ConfigTemplates) RenderDockerCompose(config DockerComposeConfig) string {
    data := map[string]interface{}{
        "app_image":      config.AppImage,
        "host_port":      config.HostPort,
        "container_port": config.ContainerPort,
        "database_url":   config.DatabaseURL,
        "api_key":        config.APIKey,
        "data_path":      config.DataPath,
    }
    
    parser := NewAdvancedTemplateParser()
    result, _ := parser.ParseTemplate(c.DockerCompose, data)
    return result
}
```

## 🎯 **Best Practices**

### **1. Use Clear Variable Names**

```go
// ✅ Good - Clear variable names
[good_variables]
welcome_message: "Hello, $user_name! Welcome to $app_name."
email_subject: "Your order #$order_id has been shipped"
api_endpoint: "https://$api_host:$api_port/$api_path"

// ❌ Bad - Unclear variable names
[bad_variables]
message: "Hello, $n! Welcome to $a."
subject: "Your order #$o has been shipped"
endpoint: "https://$h:$p/$path"
```

### **2. Validate Template Variables**

```go
// ✅ Good - Validate template variables
func (t *TemplateEngine) ValidateTemplate(template string, data map[string]interface{}) error {
    re := regexp.MustCompile(`\$([a-zA-Z_][a-zA-Z0-9_]*)`)
    matches := re.FindAllStringSubmatch(template, -1)
    
    for _, match := range matches {
        variable := match[1]
        if _, exists := data[variable]; !exists {
            return fmt.Errorf("missing required variable: %s", variable)
        }
    }
    
    return nil
}

// ❌ Bad - No validation
func (t *TemplateEngine) RenderTemplate(template string, data map[string]interface{}) string {
    // No validation of required variables
    return t.renderString(template)
}
```

### **3. Cache Expensive Templates**

```go
// ✅ Good - Cache expensive templates
func (l *LazyTemplate) Render(template string, data map[string]interface{}) (string, error) {
    cacheKey := l.generateCacheKey(template, data)
    
    if cached, exists := l.cache.Get(cacheKey); exists {
        return cached, nil
    }
    
    result, err := l.parser.ParseTemplate(template, data)
    if err != nil {
        return "", err
    }
    
    l.cache.Set(cacheKey, result)
    return result, nil
}

// ❌ Bad - No caching
func (t *TemplateEngine) RenderTemplate(template string, data map[string]interface{}) string {
    // Always re-render template
    return t.renderString(template)
}
```

### **4. Use Type-Safe Template Data**

```go
// ✅ Good - Type-safe template data
type UserData struct {
    Name  string `json:"name"`
    Email string `json:"email"`
    Age   int    `json:"age"`
}

func (t *TemplateEngine) RenderUserTemplate(template string, user UserData) string {
    data := map[string]interface{}{
        "user_name": user.Name,
        "user_email": user.Email,
        "user_age": user.Age,
    }
    return t.renderString(template)
}

// ❌ Bad - Untyped template data
func (t *TemplateEngine) RenderTemplate(template string, data map[string]interface{}) string {
    // No type safety
    return t.renderString(template)
}
```

### **5. Document Template Variables**

```go
// ✅ Good - Documented template variables
[documented_templates]
# Welcome email template
# Variables:
#   $user_name - Full name of the user
#   $app_name - Name of the application
#   $registration_date - Date when user registered
welcome_email: """
Dear $user_name,

Welcome to $app_name!
You registered on $registration_date.

Best regards,
The $app_name Team
"""

# ❌ Bad - Undocumented template variables
[undocumented_templates]
welcome_email: """
Dear $user_name,

Welcome to $app_name!
You registered on $registration_date.

Best regards,
The $app_name Team
"""
```

---

**🎉 You've mastered templates in TuskLang with Go!**

Templates in TuskLang transform static text into dynamic, responsive content. With proper template handling, you can create flexible, maintainable systems that adapt to your data and requirements.

**Next Steps:**
- Explore [023-validation-go.md](023-validation-go.md) for configuration validation
- Master [024-security-go.md](024-security-go.md) for security features
- Dive into [025-testing-go.md](025-testing-go.md) for testing strategies

**Remember:** In TuskLang, templates aren't just text—they're living configurations that respond to your data. Use them wisely to create flexible, maintainable systems. 
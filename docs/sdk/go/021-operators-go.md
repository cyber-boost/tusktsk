# Operators in TuskLang - Go Guide

## 🎯 **The Power of the @ Operator System**

In TuskLang, operators aren't just symbols—they're the heartbeat of dynamic configuration. We don't bow to any king, especially not static configuration values. TuskLang's @ operator system gives you the power to make your configuration alive, responsive, and intelligent.

## 📋 **Table of Contents**
- [Understanding the @ Operator System](#understanding-the--operator-system)
- [Environment Operators](#environment-operators)
- [Date and Time Operators](#date-and-time-operators)
- [Database Operators](#database-operators)
- [File and HTTP Operators](#file-and-http-operators)
- [Validation Operators](#validation-operators)
- [Go Integration](#go-integration)
- [Performance Considerations](#performance-considerations)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding the @ Operator System**

TuskLang's @ operator system provides dynamic functionality directly in configuration:

```go
// TuskLang configuration with operators
[app_config]
api_key: @env("API_KEY", "default_key")
current_time: @date.now()
user_count: @query("SELECT COUNT(*) FROM users")
cache_data: @cache("5m", "expensive_operation")
encrypted_value: @encrypt("sensitive_data", "AES-256-GCM")
```

```go
// Go integration
type AppConfig struct {
    APIKey         string `tsk:"api_key"`
    CurrentTime    string `tsk:"current_time"`
    UserCount      int    `tsk:"user_count"`
    CacheData      string `tsk:"cache_data"`
    EncryptedValue string `tsk:"encrypted_value"`
}
```

## 🌍 **Environment Operators**

### **Basic Environment Variables**

```go
// TuskLang - Environment variable operators
[env_config]
database_url: @env("DATABASE_URL")
api_key: @env("API_KEY", "default_key")
debug_mode: @env("DEBUG", false)
port: @env("PORT", 8080)
```

```go
// Go - Environment operator handling
type EnvConfig struct {
    DatabaseURL string `tsk:"database_url"`
    APIKey      string `tsk:"api_key"`
    DebugMode   bool   `tsk:"debug_mode"`
    Port        int    `tsk:"port"`
}

func LoadEnvConfig() (*EnvConfig, error) {
    config := &EnvConfig{}
    err := tusk.ParseFile("config.tsk", config)
    if err != nil {
        return nil, fmt.Errorf("failed to load env config: %w", err)
    }
    return config, nil
}
```

### **Secure Environment Variables**

```go
// TuskLang - Secure environment variables
[secure_config]
secret_key: @env.secure("SECRET_KEY")
password: @env.secure("DB_PASSWORD")
token: @env.secure("API_TOKEN")
```

```go
// Go - Secure environment handling
type SecureConfig struct {
    SecretKey string `tsk:"secret_key"`
    Password  string `tsk:"password"`
    Token     string `tsk:"token"`
}

func (s *SecureConfig) ValidateSecrets() error {
    if s.SecretKey == "" {
        return errors.New("secret key is required")
    }
    if s.Password == "" {
        return errors.New("database password is required")
    }
    if s.Token == "" {
        return errors.New("API token is required")
    }
    return nil
}
```

### **Environment Variable Validation**

```go
// TuskLang - Environment variable validation
[validation_config]
required_vars: @env.required(["API_KEY", "DATABASE_URL"])
optional_vars: @env.optional(["DEBUG", "PORT"])
numeric_vars: @env.numeric(["PORT", "TIMEOUT"])
```

```go
// Go - Environment validation handling
type ValidationConfig struct {
    RequiredVars []string `tsk:"required_vars"`
    OptionalVars []string `tsk:"optional_vars"`
    NumericVars  []string `tsk:"numeric_vars"`
}

func (v *ValidationConfig) ValidateEnvironment() error {
    // Validate required variables
    for _, varName := range v.RequiredVars {
        if os.Getenv(varName) == "" {
            return fmt.Errorf("required environment variable %s is not set", varName)
        }
    }
    
    // Validate numeric variables
    for _, varName := range v.NumericVars {
        value := os.Getenv(varName)
        if value != "" {
            if _, err := strconv.Atoi(value); err != nil {
                return fmt.Errorf("environment variable %s must be numeric", varName)
            }
        }
    }
    
    return nil
}
```

## ⏰ **Date and Time Operators**

### **Current Date and Time**

```go
// TuskLang - Date and time operators
[time_config]
current_time: @date.now()
formatted_date: @date("Y-m-d H:i:s")
timestamp: @date.timestamp()
iso_date: @date.iso()
```

```go
// Go - Date and time handling
type TimeConfig struct {
    CurrentTime   string `tsk:"current_time"`
    FormattedDate string `tsk:"formatted_date"`
    Timestamp     int64  `tsk:"timestamp"`
    ISODate       string `tsk:"iso_date"`
}

func (t *TimeConfig) GetCurrentTime() time.Time {
    return time.Now()
}

func (t *TimeConfig) GetFormattedTime() string {
    return time.Now().Format("2006-01-02 15:04:05")
}

func (t *TimeConfig) GetTimestamp() int64 {
    return time.Now().Unix()
}
```

### **Date Calculations**

```go
// TuskLang - Date calculations
[date_calc]
tomorrow: @date.add("1d")
next_week: @date.add("7d")
next_month: @date.add("1M")
expiry_date: @date.add("30d")
```

```go
// Go - Date calculation handling
type DateCalc struct {
    Tomorrow   string `tsk:"tomorrow"`
    NextWeek   string `tsk:"next_week"`
    NextMonth  string `tsk:"next_month"`
    ExpiryDate string `tsk:"expiry_date"`
}

func (d *DateCalc) GetTomorrow() time.Time {
    return time.Now().AddDate(0, 0, 1)
}

func (d *DateCalc) GetNextWeek() time.Time {
    return time.Now().AddDate(0, 0, 7)
}

func (d *DateCalc) GetNextMonth() time.Time {
    return time.Now().AddDate(0, 1, 0)
}

func (d *DateCalc) GetExpiryDate() time.Time {
    return time.Now().AddDate(0, 0, 30)
}
```

### **Date Formatting**

```go
// TuskLang - Date formatting
[date_format]
short_date: @date.format("Y-m-d")
long_date: @date.format("F j, Y")
time_only: @date.format("H:i:s")
custom_format: @date.format("Y-m-d H:i:s T")
```

```go
// Go - Date formatting handling
type DateFormat struct {
    ShortDate    string `tsk:"short_date"`
    LongDate     string `tsk:"long_date"`
    TimeOnly     string `tsk:"time_only"`
    CustomFormat string `tsk:"custom_format"`
}

func (d *DateFormat) GetShortDate() string {
    return time.Now().Format("2006-01-02")
}

func (d *DateFormat) GetLongDate() string {
    return time.Now().Format("January 2, 2006")
}

func (d *DateFormat) GetTimeOnly() string {
    return time.Now().Format("15:04:05")
}

func (d *DateFormat) GetCustomFormat() string {
    return time.Now().Format("2006-01-02 15:04:05 MST")
}
```

## 🗃️ **Database Operators**

### **SQL Queries**

```go
// TuskLang - Database query operators
[database_config]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
latest_user: @query("SELECT name FROM users ORDER BY created_at DESC LIMIT 1")
user_stats: @query("SELECT COUNT(*) as total, SUM(active) as active FROM users")
```

```go
// Go - Database query handling
type DatabaseConfig struct {
    UserCount   int                    `tsk:"user_count"`
    ActiveUsers int                    `tsk:"active_users"`
    LatestUser  string                 `tsk:"latest_user"`
    UserStats   map[string]interface{} `tsk:"user_stats"`
}

func (d *DatabaseConfig) ExecuteQuery(query string) (interface{}, error) {
    // Execute database query
    // This is a simplified implementation
    db, err := sql.Open("postgres", d.getConnectionString())
    if err != nil {
        return nil, fmt.Errorf("failed to connect to database: %w", err)
    }
    defer db.Close()
    
    var result interface{}
    err = db.QueryRow(query).Scan(&result)
    if err != nil {
        return nil, fmt.Errorf("failed to execute query: %w", err)
    }
    
    return result, nil
}

func (d *DatabaseConfig) getConnectionString() string {
    // Return database connection string
    return "postgresql://user:pass@localhost/db"
}
```

### **Parameterized Queries**

```go
// TuskLang - Parameterized queries
[param_queries]
user_by_id: @query("SELECT * FROM users WHERE id = ?", @env("USER_ID"))
users_by_role: @query("SELECT * FROM users WHERE role = ?", @env("USER_ROLE"))
recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
```

```go
// Go - Parameterized query handling
type ParamQueries struct {
    UserByID     map[string]interface{} `tsk:"user_by_id"`
    UsersByRole  []map[string]interface{} `tsk:"users_by_role"`
    RecentOrders []map[string]interface{} `tsk:"recent_orders"`
}

func (p *ParamQueries) ExecuteParameterizedQuery(query string, args ...interface{}) (interface{}, error) {
    db, err := sql.Open("postgres", "postgresql://user:pass@localhost/db")
    if err != nil {
        return nil, fmt.Errorf("failed to connect to database: %w", err)
    }
    defer db.Close()
    
    rows, err := db.Query(query, args...)
    if err != nil {
        return nil, fmt.Errorf("failed to execute parameterized query: %w", err)
    }
    defer rows.Close()
    
    var results []map[string]interface{}
    columns, err := rows.Columns()
    if err != nil {
        return nil, fmt.Errorf("failed to get columns: %w", err)
    }
    
    for rows.Next() {
        values := make([]interface{}, len(columns))
        valuePtrs := make([]interface{}, len(columns))
        for i := range values {
            valuePtrs[i] = &values[i]
        }
        
        err := rows.Scan(valuePtrs...)
        if err != nil {
            return nil, fmt.Errorf("failed to scan row: %w", err)
        }
        
        row := make(map[string]interface{})
        for i, col := range columns {
            row[col] = values[i]
        }
        results = append(results, row)
    }
    
    return results, nil
}
```

### **Database Connection Operators**

```go
// TuskLang - Database connection operators
[db_connection]
connection_string: @db.connect("postgresql", @env("DB_HOST"), @env("DB_PORT"), @env("DB_NAME"))
pool_size: @db.pool_size(10)
timeout: @db.timeout(30)
ssl_mode: @db.ssl_mode("require")
```

```go
// Go - Database connection handling
type DBConnection struct {
    ConnectionString string `tsk:"connection_string"`
    PoolSize         int    `tsk:"pool_size"`
    Timeout          int    `tsk:"timeout"`
    SSLMode          string `tsk:"ssl_mode"`
}

func (d *DBConnection) GetConnection() (*sql.DB, error) {
    db, err := sql.Open("postgres", d.ConnectionString)
    if err != nil {
        return nil, fmt.Errorf("failed to open database connection: %w", err)
    }
    
    db.SetMaxOpenConns(d.PoolSize)
    db.SetConnMaxLifetime(time.Duration(d.Timeout) * time.Second)
    
    return db, nil
}
```

## 📁 **File and HTTP Operators**

### **File Operations**

```go
// TuskLang - File operations
[file_config]
config_content: @file.read("config.json")
log_content: @file.read("app.log", "last_100_lines")
file_size: @file.size("data.txt")
file_exists: @file.exists("config.tsk")
```

```go
// Go - File operation handling
type FileConfig struct {
    ConfigContent string `tsk:"config_content"`
    LogContent    string `tsk:"log_content"`
    FileSize      int64  `tsk:"file_size"`
    FileExists    bool   `tsk:"file_exists"`
}

func (f *FileConfig) ReadFile(path string) (string, error) {
    data, err := os.ReadFile(path)
    if err != nil {
        return "", fmt.Errorf("failed to read file %s: %w", path, err)
    }
    return string(data), nil
}

func (f *FileConfig) GetFileSize(path string) (int64, error) {
    info, err := os.Stat(path)
    if err != nil {
        return 0, fmt.Errorf("failed to get file info for %s: %w", path, err)
    }
    return info.Size(), nil
}

func (f *FileConfig) FileExists(path string) bool {
    _, err := os.Stat(path)
    return err == nil
}
```

### **HTTP Operations**

```go
// TuskLang - HTTP operations
[http_config]
api_response: @http.get("https://api.example.com/data")
post_result: @http.post("https://api.example.com/submit", {"key": "value"})
status_code: @http.status("https://api.example.com/health")
response_time: @http.time("https://api.example.com/data")
```

```go
// Go - HTTP operation handling
type HTTPConfig struct {
    APIResponse  string `tsk:"api_response"`
    PostResult   string `tsk:"post_result"`
    StatusCode   int    `tsk:"status_code"`
    ResponseTime int64  `tsk:"response_time"`
}

func (h *HTTPConfig) GetHTTP(url string) (string, error) {
    resp, err := http.Get(url)
    if err != nil {
        return "", fmt.Errorf("failed to make GET request: %w", err)
    }
    defer resp.Body.Close()
    
    body, err := io.ReadAll(resp.Body)
    if err != nil {
        return "", fmt.Errorf("failed to read response body: %w", err)
    }
    
    return string(body), nil
}

func (h *HTTPConfig) PostHTTP(url string, data map[string]interface{}) (string, error) {
    jsonData, err := json.Marshal(data)
    if err != nil {
        return "", fmt.Errorf("failed to marshal JSON: %w", err)
    }
    
    resp, err := http.Post(url, "application/json", bytes.NewBuffer(jsonData))
    if err != nil {
        return "", fmt.Errorf("failed to make POST request: %w", err)
    }
    defer resp.Body.Close()
    
    body, err := io.ReadAll(resp.Body)
    if err != nil {
        return "", fmt.Errorf("failed to read response body: %w", err)
    }
    
    return string(body), nil
}

func (h *HTTPConfig) GetHTTPStatus(url string) (int, error) {
    resp, err := http.Get(url)
    if err != nil {
        return 0, fmt.Errorf("failed to make GET request: %w", err)
    }
    defer resp.Body.Close()
    
    return resp.StatusCode, nil
}

func (h *HTTPConfig) GetHTTPResponseTime(url string) (time.Duration, error) {
    start := time.Now()
    
    resp, err := http.Get(url)
    if err != nil {
        return 0, fmt.Errorf("failed to make GET request: %w", err)
    }
    defer resp.Body.Close()
    
    return time.Since(start), nil
}
```

## ✅ **Validation Operators**

### **Input Validation**

```go
// TuskLang - Validation operators
[validation_config]
required_fields: @validate.required(["api_key", "database_url"])
email_validation: @validate.email(@env("ADMIN_EMAIL"))
url_validation: @validate.url(@env("API_BASE_URL"))
numeric_validation: @validate.numeric(@env("PORT"))
```

```go
// Go - Validation handling
type ValidationConfig struct {
    RequiredFields []string `tsk:"required_fields"`
    EmailValidation bool    `tsk:"email_validation"`
    URLValidation   bool    `tsk:"url_validation"`
    NumericValidation bool  `tsk:"numeric_validation"`
}

func (v *ValidationConfig) ValidateRequired(fields []string) error {
    for _, field := range fields {
        if os.Getenv(field) == "" {
            return fmt.Errorf("required field %s is missing", field)
        }
    }
    return nil
}

func (v *ValidationConfig) ValidateEmail(email string) bool {
    emailRegex := regexp.MustCompile(`^[^\s@]+@[^\s@]+\.[^\s@]+$`)
    return emailRegex.MatchString(email)
}

func (v *ValidationConfig) ValidateURL(url string) bool {
    _, err := url.Parse(url)
    return err == nil
}

func (v *ValidationConfig) ValidateNumeric(value string) bool {
    _, err := strconv.Atoi(value)
    return err == nil
}
```

### **Custom Validation**

```go
// TuskLang - Custom validation
[custom_validation]
password_strength: @validate.custom("password", "min_length:8,uppercase:true,number:true")
age_validation: @validate.range(@env("AGE"), 18, 100)
file_size: @validate.max_size("upload.txt", "10MB")
```

```go
// Go - Custom validation handling
type CustomValidation struct {
    PasswordStrength bool `tsk:"password_strength"`
    AgeValidation    bool `tsk:"age_validation"`
    FileSize         bool `tsk:"file_size"`
}

func (c *CustomValidation) ValidatePassword(password string) bool {
    if len(password) < 8 {
        return false
    }
    
    hasUpper := regexp.MustCompile(`[A-Z]`).MatchString(password)
    hasNumber := regexp.MustCompile(`[0-9]`).MatchString(password)
    
    return hasUpper && hasNumber
}

func (c *CustomValidation) ValidateAge(age int) bool {
    return age >= 18 && age <= 100
}

func (c *CustomValidation) ValidateFileSize(path string, maxSize string) bool {
    info, err := os.Stat(path)
    if err != nil {
        return false
    }
    
    // Parse max size (e.g., "10MB")
    size := info.Size()
    maxBytes := parseSize(maxSize)
    
    return size <= maxBytes
}

func parseSize(sizeStr string) int64 {
    // Simple size parser
    if strings.HasSuffix(sizeStr, "MB") {
        size, _ := strconv.Atoi(strings.TrimSuffix(sizeStr, "MB"))
        return int64(size * 1024 * 1024)
    }
    if strings.HasSuffix(sizeStr, "KB") {
        size, _ := strconv.Atoi(strings.TrimSuffix(sizeStr, "KB"))
        return int64(size * 1024)
    }
    size, _ := strconv.Atoi(sizeStr)
    return int64(size)
}
```

## 🔧 **Go Integration**

### **Operator Registry**

```go
// Go - Operator registry system
type OperatorRegistry struct {
    operators map[string]OperatorFunc
}

type OperatorFunc func(args ...interface{}) (interface{}, error)

func NewOperatorRegistry() *OperatorRegistry {
    return &OperatorRegistry{
        operators: make(map[string]OperatorFunc),
    }
}

func (o *OperatorRegistry) Register(name string, fn OperatorFunc) {
    o.operators[name] = fn
}

func (o *OperatorRegistry) Execute(name string, args ...interface{}) (interface{}, error) {
    fn, exists := o.operators[name]
    if !exists {
        return nil, fmt.Errorf("operator '%s' not found", name)
    }
    
    return fn(args...)
}

// Register common operators
func (o *OperatorRegistry) RegisterCommon() {
    o.Register("env", o.envOperator)
    o.Register("date.now", o.dateNowOperator)
    o.Register("query", o.queryOperator)
    o.Register("file.read", o.fileReadOperator)
    o.Register("http.get", o.httpGetOperator)
}

func (o *OperatorRegistry) envOperator(args ...interface{}) (interface{}, error) {
    if len(args) < 1 {
        return nil, errors.New("env operator requires at least one argument")
    }
    
    varName, ok := args[0].(string)
    if !ok {
        return nil, errors.New("env operator first argument must be string")
    }
    
    value := os.Getenv(varName)
    if value == "" && len(args) > 1 {
        return args[1], nil // Return default value
    }
    
    return value, nil
}

func (o *OperatorRegistry) dateNowOperator(args ...interface{}) (interface{}, error) {
    return time.Now().Format(time.RFC3339), nil
}

func (o *OperatorRegistry) queryOperator(args ...interface{}) (interface{}, error) {
    if len(args) < 1 {
        return nil, errors.New("query operator requires at least one argument")
    }
    
    query, ok := args[0].(string)
    if !ok {
        return nil, errors.New("query operator first argument must be string")
    }
    
    // Execute database query
    // This is a simplified implementation
    return "query_result", nil
}

func (o *OperatorRegistry) fileReadOperator(args ...interface{}) (interface{}, error) {
    if len(args) < 1 {
        return nil, errors.New("file.read operator requires at least one argument")
    }
    
    path, ok := args[0].(string)
    if !ok {
        return nil, errors.New("file.read operator first argument must be string")
    }
    
    data, err := os.ReadFile(path)
    if err != nil {
        return nil, fmt.Errorf("failed to read file %s: %w", path, err)
    }
    
    return string(data), nil
}

func (o *OperatorRegistry) httpGetOperator(args ...interface{}) (interface{}, error) {
    if len(args) < 1 {
        return nil, errors.New("http.get operator requires at least one argument")
    }
    
    url, ok := args[0].(string)
    if !ok {
        return nil, errors.New("http.get operator first argument must be string")
    }
    
    resp, err := http.Get(url)
    if err != nil {
        return nil, fmt.Errorf("failed to make GET request: %w", err)
    }
    defer resp.Body.Close()
    
    body, err := io.ReadAll(resp.Body)
    if err != nil {
        return nil, fmt.Errorf("failed to read response body: %w", err)
    }
    
    return string(body), nil
}
```

### **Operator Parser**

```go
// Go - Operator parser
type OperatorParser struct {
    registry *OperatorRegistry
}

func NewOperatorParser() *OperatorParser {
    registry := NewOperatorRegistry()
    registry.RegisterCommon()
    
    return &OperatorParser{
        registry: registry,
    }
}

func (o *OperatorParser) ParseOperators(config map[string]interface{}) error {
    for key, value := range config {
        if str, ok := value.(string); ok {
            if strings.HasPrefix(str, "@") {
                result, err := o.parseOperator(str)
                if err != nil {
                    return fmt.Errorf("failed to parse operator for key %s: %w", key, err)
                }
                config[key] = result
            }
        }
    }
    return nil
}

func (o *OperatorParser) parseOperator(expr string) (interface{}, error) {
    // Remove @ prefix
    expr = strings.TrimPrefix(expr, "@")
    
    // Parse operator name and arguments
    parts := strings.SplitN(expr, "(", 2)
    if len(parts) != 2 {
        return nil, fmt.Errorf("invalid operator syntax: %s", expr)
    }
    
    operatorName := parts[0]
    argsStr := strings.TrimSuffix(parts[1], ")")
    
    // Parse arguments
    args, err := o.parseArguments(argsStr)
    if err != nil {
        return nil, fmt.Errorf("failed to parse operator arguments: %w", err)
    }
    
    // Execute operator
    return o.registry.Execute(operatorName, args...)
}

func (o *OperatorParser) parseArguments(argsStr string) ([]interface{}, error) {
    // Simple argument parser
    // This is a simplified implementation
    var args []interface{}
    
    // Split by comma, but respect quoted strings
    parts := strings.Split(argsStr, ",")
    for _, part := range parts {
        part = strings.TrimSpace(part)
        
        // Remove quotes if present
        if strings.HasPrefix(part, "\"") && strings.HasSuffix(part, "\"") {
            part = strings.Trim(part, "\"")
        }
        
        args = append(args, part)
    }
    
    return args, nil
}
```

## ⚡ **Performance Considerations**

### **Operator Caching**

```go
// Go - Operator caching system
type OperatorCache struct {
    cache map[string]interface{}
    mutex sync.RWMutex
    ttl   time.Duration
}

func NewOperatorCache(ttl time.Duration) *OperatorCache {
    return &OperatorCache{
        cache: make(map[string]interface{}),
        ttl:   ttl,
    }
}

func (o *OperatorCache) Get(key string) (interface{}, bool) {
    o.mutex.RLock()
    defer o.mutex.RUnlock()
    
    value, exists := o.cache[key]
    return value, exists
}

func (o *OperatorCache) Set(key string, value interface{}) {
    o.mutex.Lock()
    defer o.mutex.Unlock()
    
    o.cache[key] = value
    
    // Schedule cleanup
    go func() {
        time.Sleep(o.ttl)
        o.mutex.Lock()
        delete(o.cache, key)
        o.mutex.Unlock()
    }()
}

func (o *OperatorCache) Clear() {
    o.mutex.Lock()
    defer o.mutex.Unlock()
    
    o.cache = make(map[string]interface{})
}
```

### **Lazy Operator Evaluation**

```go
// Go - Lazy operator evaluation
type LazyOperator struct {
    registry *OperatorRegistry
    cache    *OperatorCache
}

func NewLazyOperator(ttl time.Duration) *LazyOperator {
    registry := NewOperatorRegistry()
    registry.RegisterCommon()
    
    return &LazyOperator{
        registry: registry,
        cache:    NewOperatorCache(ttl),
    }
}

func (l *LazyOperator) Evaluate(expr string) (interface{}, error) {
    // Check cache first
    if cached, exists := l.cache.Get(expr); exists {
        return cached, nil
    }
    
    // Parse and execute operator
    if strings.HasPrefix(expr, "@") {
        result, err := l.parseOperator(expr)
        if err != nil {
            return nil, err
        }
        
        // Cache the result
        l.cache.Set(expr, result)
        return result, nil
    }
    
    return expr, nil
}

func (l *LazyOperator) parseOperator(expr string) (interface{}, error) {
    // Remove @ prefix
    expr = strings.TrimPrefix(expr, "@")
    
    // Parse operator name and arguments
    parts := strings.SplitN(expr, "(", 2)
    if len(parts) != 2 {
        return nil, fmt.Errorf("invalid operator syntax: %s", expr)
    }
    
    operatorName := parts[0]
    argsStr := strings.TrimSuffix(parts[1], ")")
    
    // Parse arguments
    args, err := l.parseArguments(argsStr)
    if err != nil {
        return nil, fmt.Errorf("failed to parse operator arguments: %w", err)
    }
    
    // Execute operator
    return l.registry.Execute(operatorName, args...)
}

func (l *LazyOperator) parseArguments(argsStr string) ([]interface{}, error) {
    var args []interface{}
    
    parts := strings.Split(argsStr, ",")
    for _, part := range parts {
        part = strings.TrimSpace(part)
        
        if strings.HasPrefix(part, "\"") && strings.HasSuffix(part, "\"") {
            part = strings.Trim(part, "\"")
        }
        
        args = append(args, part)
    }
    
    return args, nil
}
```

## 🌍 **Real-World Examples**

### **Dynamic API Configuration**

```go
// TuskLang - Dynamic API configuration
[api_config]
base_url: @env("API_BASE_URL", "https://api.example.com")
api_key: @env.secure("API_KEY")
timeout: @env("API_TIMEOUT", 30)
user_agent: @concat("MyApp/", @env("APP_VERSION", "1.0.0"))
request_id: @uuid.generate()
timestamp: @date.now()
```

```go
// Go - Dynamic API configuration handling
type APIConfig struct {
    BaseURL   string `tsk:"base_url"`
    APIKey    string `tsk:"api_key"`
    Timeout   int    `tsk:"timeout"`
    UserAgent string `tsk:"user_agent"`
    RequestID string `tsk:"request_id"`
    Timestamp string `tsk:"timestamp"`
}

func (a *APIConfig) MakeRequest(endpoint string, data interface{}) ([]byte, error) {
    client := &http.Client{
        Timeout: time.Duration(a.Timeout) * time.Second,
    }
    
    url := a.BaseURL + endpoint
    
    var body io.Reader
    if data != nil {
        jsonData, err := json.Marshal(data)
        if err != nil {
            return nil, fmt.Errorf("failed to marshal request data: %w", err)
        }
        body = bytes.NewBuffer(jsonData)
    }
    
    req, err := http.NewRequest("POST", url, body)
    if err != nil {
        return nil, fmt.Errorf("failed to create request: %w", err)
    }
    
    req.Header.Set("Authorization", "Bearer "+a.APIKey)
    req.Header.Set("User-Agent", a.UserAgent)
    req.Header.Set("X-Request-ID", a.RequestID)
    req.Header.Set("Content-Type", "application/json")
    
    resp, err := client.Do(req)
    if err != nil {
        return nil, fmt.Errorf("failed to make request: %w", err)
    }
    defer resp.Body.Close()
    
    responseBody, err := io.ReadAll(resp.Body)
    if err != nil {
        return nil, fmt.Errorf("failed to read response body: %w", err)
    }
    
    return responseBody, nil
}
```

### **Database-Driven Configuration**

```go
// TuskLang - Database-driven configuration
[db_config]
connection_string: @env("DATABASE_URL")
max_connections: @query("SELECT value FROM config WHERE key = 'max_connections'")
cache_ttl: @query("SELECT value FROM config WHERE key = 'cache_ttl'")
feature_flags: @query("SELECT key, value FROM feature_flags WHERE enabled = true")
user_preferences: @query("SELECT * FROM user_preferences WHERE user_id = ?", @env("USER_ID"))
```

```go
// Go - Database-driven configuration handling
type DBConfig struct {
    ConnectionString string                 `tsk:"connection_string"`
    MaxConnections   int                    `tsk:"max_connections"`
    CacheTTL         int                    `tsk:"cache_ttl"`
    FeatureFlags     map[string]interface{} `tsk:"feature_flags"`
    UserPreferences  map[string]interface{} `tsk:"user_preferences"`
}

func (d *DBConfig) GetConnection() (*sql.DB, error) {
    db, err := sql.Open("postgres", d.ConnectionString)
    if err != nil {
        return nil, fmt.Errorf("failed to open database connection: %w", err)
    }
    
    db.SetMaxOpenConns(d.MaxConnections)
    
    return db, nil
}

func (d *DBConfig) IsFeatureEnabled(feature string) bool {
    if flags, ok := d.FeatureFlags[feature]; ok {
        if enabled, ok := flags.(bool); ok {
            return enabled
        }
    }
    return false
}

func (d *DBConfig) GetUserPreference(key string) interface{} {
    if prefs, ok := d.UserPreferences[key]; ok {
        return prefs
    }
    return nil
}
```

### **File-Based Configuration with Validation**

```go
// TuskLang - File-based configuration with validation
[file_config]
config_file: @file.read("config.json")
config_valid: @validate.json(@file.read("config.json"))
file_size: @file.size("config.json")
last_modified: @file.modified("config.json")
backup_exists: @file.exists("config.json.backup")
```

```go
// Go - File-based configuration handling
type FileConfig struct {
    ConfigFile   string `tsk:"config_file"`
    ConfigValid  bool   `tsk:"config_valid"`
    FileSize     int64  `tsk:"file_size"`
    LastModified string `tsk:"last_modified"`
    BackupExists bool   `tsk:"backup_exists"`
}

func (f *FileConfig) LoadConfig() (map[string]interface{}, error) {
    if !f.ConfigValid {
        return nil, errors.New("configuration file is not valid JSON")
    }
    
    var config map[string]interface{}
    err := json.Unmarshal([]byte(f.ConfigFile), &config)
    if err != nil {
        return nil, fmt.Errorf("failed to parse configuration: %w", err)
    }
    
    return config, nil
}

func (f *FileConfig) CreateBackup() error {
    if f.BackupExists {
        return errors.New("backup already exists")
    }
    
    err := copyFile("config.json", "config.json.backup")
    if err != nil {
        return fmt.Errorf("failed to create backup: %w", err)
    }
    
    return nil
}

func copyFile(src, dst string) error {
    sourceFile, err := os.Open(src)
    if err != nil {
        return err
    }
    defer sourceFile.Close()
    
    destFile, err := os.Create(dst)
    if err != nil {
        return err
    }
    defer destFile.Close()
    
    _, err = io.Copy(destFile, sourceFile)
    return err
}
```

## 🎯 **Best Practices**

### **1. Use Appropriate Operators for the Task**

```go
// ✅ Good - Appropriate operator usage
[good_operators]
api_key: @env.secure("API_KEY")  # Secure environment variable
current_time: @date.now()        # Current timestamp
user_count: @query("SELECT COUNT(*) FROM users")  # Database query
config_data: @file.read("config.json")  # File reading

// ❌ Bad - Inappropriate operator usage
[bad_operators]
api_key: @file.read("/etc/api_key")  # Security risk
current_time: @env("CURRENT_TIME")   # Unnecessary environment variable
user_count: @env("USER_COUNT")       # Static value instead of dynamic query
config_data: @query("SELECT * FROM config")  # Database for file data
```

### **2. Cache Expensive Operations**

```go
// ✅ Good - Cached expensive operations
[good_caching]
user_stats: @cache("5m", @query("SELECT COUNT(*) FROM users"))
api_data: @cache("1h", @http.get("https://api.example.com/data"))
file_content: @cache("10m", @file.read("large_file.txt"))

// ❌ Bad - No caching for expensive operations
[bad_caching]
user_stats: @query("SELECT COUNT(*) FROM users")  # No caching
api_data: @http.get("https://api.example.com/data")  # No caching
file_content: @file.read("large_file.txt")  # No caching
```

### **3. Validate Operator Inputs**

```go
// ✅ Good - Validated operator inputs
[good_validation]
required_vars: @validate.required(["API_KEY", "DATABASE_URL"])
email_valid: @validate.email(@env("ADMIN_EMAIL"))
url_valid: @validate.url(@env("API_BASE_URL"))

// ❌ Bad - No validation
[bad_validation]
api_key: @env("API_KEY")  # No validation
admin_email: @env("ADMIN_EMAIL")  # No validation
api_url: @env("API_BASE_URL")  # No validation
```

### **4. Use Secure Operators for Sensitive Data**

```go
// ✅ Good - Secure operators for sensitive data
[good_security]
secret_key: @env.secure("SECRET_KEY")
encrypted_data: @encrypt("sensitive_value", "AES-256-GCM")
hashed_password: @hash("password", "bcrypt")

// ❌ Bad - Insecure handling of sensitive data
[bad_security]
secret_key: @env("SECRET_KEY")  # Not secure
sensitive_data: "plain_text_value"  # No encryption
password: "plain_text_password"  # No hashing
```

### **5. Document Operator Usage**

```go
// ✅ Good - Documented operator usage
[documented_operators]
# API configuration with secure key and dynamic URL
api_key: @env.secure("API_KEY")
api_url: @env("API_BASE_URL", "https://api.example.com")

# Database configuration with connection pooling
db_url: @env("DATABASE_URL")
max_connections: @env("DB_MAX_CONNECTIONS", 10)

# File configuration with validation
config_file: @file.read("config.json")
config_valid: @validate.json(@file.read("config.json"))

// ❌ Bad - Undocumented operators
[undocumented_operators]
api_key: @env.secure("API_KEY")
api_url: @env("API_BASE_URL", "https://api.example.com")
db_url: @env("DATABASE_URL")
max_connections: @env("DB_MAX_CONNECTIONS", 10)
config_file: @file.read("config.json")
config_valid: @validate.json(@file.read("config.json"))
```

---

**🎉 You've mastered operators in TuskLang with Go!**

Operators in TuskLang transform static configuration into dynamic, intelligent systems. With proper operator usage, you can build configurations that adapt, validate, and respond to their environment.

**Next Steps:**
- Explore [022-templates-go.md](022-templates-go.md) for dynamic templates
- Master [023-validation-go.md](023-validation-go.md) for configuration validation
- Dive into [024-security-go.md](024-security-go.md) for security features

**Remember:** In TuskLang, operators aren't just symbols—they're the heartbeat of dynamic configuration. Use them wisely to create intelligent, responsive systems. 
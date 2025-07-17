# 🔢 TuskLang Go Numbers Guide

**"We don't bow to any king" - Go Edition**

Master number handling in TuskLang and learn how to work with integers, floats, and mathematical operations in Go applications. This guide covers number syntax, validation, calculations, and best practices.

## 🎯 Number Fundamentals

### Basic Number Syntax

```go
// config.tsk
[numbers]
integer: 42
negative: -100
zero: 0
large: 999999999

[floats]
pi: 3.14159
negative_pi: -3.14159
zero_float: 0.0
scientific: 1.23e-4
large_float: 1.23e+10

[ports]
http_port: 80
https_port: 443
app_port: 8080
db_port: 5432
```

### Go Struct Mapping

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

type NumberConfig struct {
    Integer     int     `tsk:"integer"`      // Positive integer
    Negative    int     `tsk:"negative"`     // Negative integer
    Zero        int     `tsk:"zero"`         // Zero value
    Large       int64   `tsk:"large"`        // Large integer
}

type FloatConfig struct {
    Pi          float64 `tsk:"pi"`           // Pi constant
    NegativePi  float64 `tsk:"negative_pi"`  // Negative pi
    ZeroFloat   float64 `tsk:"zero_float"`   // Zero float
    Scientific  float64 `tsk:"scientific"`   // Scientific notation
    LargeFloat  float64 `tsk:"large_float"`  // Large float
}

type PortConfig struct {
    HTTPPort    int `tsk:"http_port"`    // HTTP port
    HTTPSPort   int `tsk:"https_port"`   // HTTPS port
    AppPort     int `tsk:"app_port"`     // Application port
    DBPort      int `tsk:"db_port"`      // Database port
}

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    var numberConfig NumberConfig
    err = tusklanggo.UnmarshalTSK(data["numbers"].(map[string]interface{}), &numberConfig)
    if err != nil {
        panic(err)
    }
    
    var floatConfig FloatConfig
    err = tusklanggo.UnmarshalTSK(data["floats"].(map[string]interface{}), &floatConfig)
    if err != nil {
        panic(err)
    }
    
    var portConfig PortConfig
    err = tusklanggo.UnmarshalTSK(data["ports"].(map[string]interface{}), &portConfig)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Integer: %d\n", numberConfig.Integer)
    fmt.Printf("Negative: %d\n", numberConfig.Negative)
    fmt.Printf("Large: %d\n", numberConfig.Large)
    fmt.Printf("Pi: %.5f\n", floatConfig.Pi)
    fmt.Printf("Scientific: %e\n", floatConfig.Scientific)
    fmt.Printf("HTTP Port: %d\n", portConfig.HTTPPort)
}
```

## 🔢 Number Types

### Integer Types

```go
// config.tsk
[integers]
int8_value: 127
int16_value: 32767
int32_value: 2147483647
int64_value: 9223372036854775807
uint8_value: 255
uint16_value: 65535
uint32_value: 4294967295
uint64_value: 18446744073709551615
```

```go
// main.go
type IntegerTypesConfig struct {
    Int8Value   int8   `tsk:"int8_value"`
    Int16Value  int16  `tsk:"int16_value"`
    Int32Value  int32  `tsk:"int32_value"`
    Int64Value  int64  `tsk:"int64_value"`
    Uint8Value  uint8  `tsk:"uint8_value"`
    Uint16Value uint16 `tsk:"uint16_value"`
    Uint32Value uint32 `tsk:"uint32_value"`
    Uint64Value uint64 `tsk:"uint64_value"`
}
```

### Float Types

```go
// config.tsk
[floats]
float32_value: 3.14159
float64_value: 3.141592653589793
precision: 0.12345678901234567890
```

```go
// main.go
type FloatTypesConfig struct {
    Float32Value float32 `tsk:"float32_value"`
    Float64Value float64 `tsk:"float64_value"`
    Precision    float64 `tsk:"precision"`
}
```

## 🔧 Mathematical Operations

### Basic Operations

```go
// config.tsk
[calculations]
addition: @math.add(10, 5)
subtraction: @math.sub(10, 5)
multiplication: @math.mul(10, 5)
division: @math.div(10, 5)
modulo: @math.mod(10, 3)
power: @math.pow(2, 10)
square_root: @math.sqrt(16)
absolute: @math.abs(-42)
```

### Advanced Operations

```go
// config.tsk
[advanced_math]
ceiling: @math.ceil(3.7)
floor: @math.floor(3.7)
round: @math.round(3.7)
min: @math.min(10, 5, 15, 2)
max: @math.max(10, 5, 15, 2)
average: @math.avg(10, 20, 30, 40, 50)
sum: @math.sum(1, 2, 3, 4, 5)
```

### Trigonometric Functions

```go
// config.tsk
[trigonometry]
sin_30: @math.sin(30)
cos_60: @math.cos(60)
tan_45: @math.tan(45)
asin_0_5: @math.asin(0.5)
acos_0_5: @math.acos(0.5)
atan_1: @math.atan(1)
```

```go
// main.go
type MathConfig struct {
    Addition      float64 `tsk:"addition"`
    Subtraction   float64 `tsk:"subtraction"`
    Multiplication float64 `tsk:"multiplication"`
    Division      float64 `tsk:"division"`
    Modulo        int     `tsk:"modulo"`
    Power         float64 `tsk:"power"`
    SquareRoot    float64 `tsk:"square_root"`
    Absolute      int     `tsk:"absolute"`
}

type AdvancedMathConfig struct {
    Ceiling float64 `tsk:"ceiling"`
    Floor   float64 `tsk:"floor"`
    Round   float64 `tsk:"round"`
    Min     float64 `tsk:"min"`
    Max     float64 `tsk:"max"`
    Average float64 `tsk:"average"`
    Sum     float64 `tsk:"sum"`
}

type TrigonometryConfig struct {
    Sin30  float64 `tsk:"sin_30"`
    Cos60  float64 `tsk:"cos_60"`
    Tan45  float64 `tsk:"tan_45"`
    Asin05 float64 `tsk:"asin_0_5"`
    Acos05 float64 `tsk:"acos_0_5"`
    Atan1  float64 `tsk:"atan_1"`
}
```

## 📊 Number Validation

### Range Validation

```go
// config.tsk
[validated]
port: 8080
timeout: 30
percentage: 75.5
temperature: 23.7
```

```go
// main.go
type ValidatedNumberConfig struct {
    Port        int     `tsk:"port" validate:"min=1,max=65535"`
    Timeout     int     `tsk:"timeout" validate:"min=1,max=3600"`
    Percentage  float64 `tsk:"percentage" validate:"min=0,max=100"`
    Temperature float64 `tsk:"temperature" validate:"min=-50,max=100"`
}

func validateNumbers(config *ValidatedNumberConfig) error {
    // Port validation
    if config.Port < 1 || config.Port > 65535 {
        return fmt.Errorf("port must be between 1 and 65535, got %d", config.Port)
    }
    
    // Timeout validation
    if config.Timeout < 1 || config.Timeout > 3600 {
        return fmt.Errorf("timeout must be between 1 and 3600 seconds, got %d", config.Timeout)
    }
    
    // Percentage validation
    if config.Percentage < 0 || config.Percentage > 100 {
        return fmt.Errorf("percentage must be between 0 and 100, got %.2f", config.Percentage)
    }
    
    // Temperature validation
    if config.Temperature < -50 || config.Temperature > 100 {
        return fmt.Errorf("temperature must be between -50 and 100°C, got %.2f", config.Temperature)
    }
    
    return nil
}
```

### Type Validation

```go
// main.go
func validateNumberType(value interface{}, expectedType string) error {
    switch expectedType {
    case "int":
        if _, ok := value.(int); !ok {
            return fmt.Errorf("expected int, got %T", value)
        }
    case "float64":
        if _, ok := value.(float64); !ok {
            return fmt.Errorf("expected float64, got %T", value)
        }
    case "positive":
        if num, ok := value.(int); ok {
            if num < 0 {
                return fmt.Errorf("expected positive number, got %d", num)
            }
        }
    case "non-zero":
        if num, ok := value.(int); ok {
            if num == 0 {
                return fmt.Errorf("expected non-zero number, got %d", num)
            }
        }
    }
    return nil
}
```

## 🔢 Dynamic Numbers

### Environment-Based Numbers

```go
// config.tsk
[environment]
port: @env("PORT", "8080")
timeout: @env("TIMEOUT", "30")
max_connections: @env("MAX_CONNECTIONS", "100")
cache_size: @env("CACHE_SIZE", "1024")
```

### Computed Numbers

```go
// config.tsk
[computed]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("24h"))
total_memory: @math.mul(@env("MEMORY_MB", "512"), 1024)
cpu_cores: @math.div(@env("CPU_MHZ", "2400"), 100)
```

### Conditional Numbers

```go
// config.tsk
[conditional]
port: @if(@env("ENVIRONMENT") == "production", 80, 8080)
workers: @if(@env("ENVIRONMENT") == "production", 8, 2)
timeout: @if(@env("ENVIRONMENT") == "production", 60, 30)
cache_size: @if(@env("ENVIRONMENT") == "production", 2048, 512)
```

```go
// main.go
type EnvironmentConfig struct {
    Port            int `tsk:"port"`
    Timeout         int `tsk:"timeout"`
    MaxConnections  int `tsk:"max_connections"`
    CacheSize       int `tsk:"cache_size"`
}

type ComputedConfig struct {
    UserCount   interface{} `tsk:"user_count"`
    ActiveUsers interface{} `tsk:"active_users"`
    TotalMemory int64       `tsk:"total_memory"`
    CPUCores    float64     `tsk:"cpu_cores"`
}

type ConditionalConfig struct {
    Port      int     `tsk:"port"`
    Workers   int     `tsk:"workers"`
    Timeout   int     `tsk:"timeout"`
    CacheSize int     `tsk:"cache_size"`
}
```

## 🎯 Number Formatting

### Currency Formatting

```go
// config.tsk
[pricing]
base_price: 99.99
tax_rate: 0.08
discount: 0.15
shipping: 5.99

[formatted]
price_with_tax: @math.add(@pricing.base_price, @math.mul(@pricing.base_price, @pricing.tax_rate))
final_price: @math.sub(@pricing.base_price, @math.mul(@pricing.base_price, @pricing.discount))
total_cost: @math.add(@pricing.base_price, @pricing.shipping)
```

### Percentage Formatting

```go
// config.tsk
[percentages]
success_rate: 95.5
error_rate: 4.5
uptime: 99.9
cpu_usage: 75.2
memory_usage: 60.8

[formatted]
success_percentage: @string.format("{:.1f}%", @percentages.success_rate)
error_percentage: @string.format("{:.1f}%", @percentages.error_rate)
uptime_percentage: @string.format("{:.1f}%", @percentages.uptime)
```

### Scientific Notation

```go
// config.tsk
[scientific]
avogadro: 6.022e23
planck: 6.626e-34
speed_of_light: 3e8
electron_mass: 9.109e-31

[formatted]
avogadro_formatted: @string.format("{:.3e}", @scientific.avogadro)
planck_formatted: @string.format("{:.3e}", @scientific.planck)
```

## 🔧 Number Functions

### Custom Number Functions

```go
// config.tsk
[functions]
calculate_tax: """
function calculateTax(amount, rate) {
    return amount * rate;
}
"""

calculate_discount: """
function calculateDiscount(amount, percentage) {
    return amount * (percentage / 100);
}
"""

calculate_compound_interest: """
function compoundInterest(principal, rate, time, compounds) {
    return principal * Math.pow(1 + rate / compounds, compounds * time);
}
"""

[calculated]
tax_amount: @fujsen(calculate_tax, @pricing.base_price, @pricing.tax_rate)
discount_amount: @fujsen(calculate_discount, @pricing.base_price, @pricing.discount)
future_value: @fujsen(calculate_compound_interest, 1000, 0.05, 10, 12)
```

### Statistical Functions

```go
// config.tsk
[statistics]
data_set: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

[functions]
calculate_mean: """
function mean(numbers) {
    return numbers.reduce((a, b) => a + b, 0) / numbers.length;
}
"""

calculate_median: """
function median(numbers) {
    const sorted = numbers.sort((a, b) => a - b);
    const mid = Math.floor(sorted.length / 2);
    return sorted.length % 2 === 0 ? 
        (sorted[mid - 1] + sorted[mid]) / 2 : 
        sorted[mid];
}
"""

calculate_standard_deviation: """
function standardDeviation(numbers) {
    const mean = numbers.reduce((a, b) => a + b, 0) / numbers.length;
    const squaredDiffs = numbers.map(x => Math.pow(x - mean, 2));
    const avgSquaredDiff = squaredDiffs.reduce((a, b) => a + b, 0) / numbers.length;
    return Math.sqrt(avgSquaredDiff);
}
"""

[calculated_stats]
mean: @fujsen(calculate_mean, @statistics.data_set)
median: @fujsen(calculate_median, @statistics.data_set)
std_dev: @fujsen(calculate_standard_deviation, @statistics.data_set)
```

## 🎯 Best Practices

### 1. Number Validation

```go
// Good - Validate numbers before use
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
    
    // Validate numbers
    if err := validateNumbers(&config); err != nil {
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

### 2. Type Safety

```go
// Good - Use appropriate types
type Config struct {
    Port        int     `tsk:"port"`        // Use int for ports
    Timeout     int     `tsk:"timeout"`     // Use int for timeouts
    Percentage  float64 `tsk:"percentage"`  // Use float64 for percentages
    Price       float64 `tsk:"price"`       // Use float64 for prices
}

# Bad - Using interface{} for everything
type Config struct {
    Port        interface{} `tsk:"port"`
    Timeout     interface{} `tsk:"timeout"`
    Percentage  interface{} `tsk:"percentage"`
    Price       interface{} `tsk:"price"`
}
```

### 3. Precision Handling

```go
// Good - Handle precision appropriately
func formatCurrency(amount float64) string {
    return fmt.Sprintf("$%.2f", amount)
}

func formatPercentage(value float64) string {
    return fmt.Sprintf("%.1f%%", value)
}

func formatScientific(value float64) string {
    return fmt.Sprintf("%.3e", value)
}

# Bad - No precision handling
func formatNumber(value float64) string {
    return fmt.Sprintf("%f", value) // Too many decimal places
}
```

### 4. Error Handling

```go
// Good - Handle number conversion errors
func safeGetInt(data map[string]interface{}, key string) (int, error) {
    value, exists := data[key]
    if !exists {
        return 0, fmt.Errorf("key '%s' not found", key)
    }
    
    switch v := value.(type) {
    case int:
        return v, nil
    case float64:
        return int(v), nil
    case string:
        if i, err := strconv.Atoi(v); err == nil {
            return i, nil
        }
        return 0, fmt.Errorf("key '%s' cannot be converted to int: %s", key, v)
    default:
        return 0, fmt.Errorf("key '%s' is not a number, got %T", key, value)
    }
}

# Bad - No error handling
func getInt(data map[string]interface{}, key string) int {
    return data[key].(int) // Will panic if wrong type
}
```

## 📊 Complete Example

### Configuration File

```go
// config.tsk
# ========================================
# NUMBER CONFIGURATION
# ========================================
[app]
port: @env("PORT", "8080")
timeout: @env("TIMEOUT", "30")
max_connections: @env("MAX_CONNECTIONS", "100")
debug_level: @env("DEBUG_LEVEL", "1")

[database]
port: @env("DB_PORT", "5432")
pool_size: @env("DB_POOL_SIZE", "10")
max_idle_connections: @env("DB_MAX_IDLE", "5")
connection_timeout: @env("DB_TIMEOUT", "30")

[performance]
cpu_cores: @env("CPU_CORES", "4")
memory_mb: @env("MEMORY_MB", "512")
cache_size_mb: @env("CACHE_SIZE_MB", "128")
max_file_size_mb: @env("MAX_FILE_SIZE_MB", "10")

[calculations]
tax_rate: 0.08
discount_rate: 0.15
shipping_cost: 5.99
base_price: 99.99

[computed]
total_memory_bytes: @math.mul(@performance.memory_mb, 1024)
cache_size_bytes: @math.mul(@performance.cache_size_mb, 1024)
max_file_size_bytes: @math.mul(@performance.max_file_size_mb, 1024)
price_with_tax: @math.add(@calculations.base_price, @math.mul(@calculations.base_price, @calculations.tax_rate))
final_price: @math.sub(@calculations.base_price, @math.mul(@calculations.base_price, @calculations.discount_rate))
total_cost: @math.add(@calculations.base_price, @calculations.shipping_cost)

[statistics]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE last_login > ?", @date.subtract("24h"))
total_orders: @query("SELECT COUNT(*) FROM orders")
total_revenue: @query("SELECT SUM(amount) FROM orders")

[functions]
calculate_percentage: """
function percentage(part, total) {
    return (part / total) * 100;
}
"""

calculate_average: """
function average(numbers) {
    return numbers.reduce((a, b) => a + b, 0) / numbers.length;
}
"""

[calculated_stats]
active_user_percentage: @fujsen(calculate_percentage, @statistics.active_users, @statistics.user_count)
average_order_value: @fujsen(calculate_average, @query("SELECT amount FROM orders"))
```

### Go Application

```go
// main.go
package main

import (
    "fmt"
    "log"
    "math"
    "strconv"
    "github.com/tusklang/go"
)

// Configuration structures
type AppConfig struct {
    Port            int `tsk:"port"`
    Timeout         int `tsk:"timeout"`
    MaxConnections  int `tsk:"max_connections"`
    DebugLevel      int `tsk:"debug_level"`
}

type DatabaseConfig struct {
    Port                 int `tsk:"port"`
    PoolSize             int `tsk:"pool_size"`
    MaxIdleConnections   int `tsk:"max_idle_connections"`
    ConnectionTimeout    int `tsk:"connection_timeout"`
}

type PerformanceConfig struct {
    CPUCores         int `tsk:"cpu_cores"`
    MemoryMB         int `tsk:"memory_mb"`
    CacheSizeMB      int `tsk:"cache_size_mb"`
    MaxFileSizeMB    int `tsk:"max_file_size_mb"`
}

type CalculationsConfig struct {
    TaxRate      float64 `tsk:"tax_rate"`
    DiscountRate float64 `tsk:"discount_rate"`
    ShippingCost float64 `tsk:"shipping_cost"`
    BasePrice    float64 `tsk:"base_price"`
}

type ComputedConfig struct {
    TotalMemoryBytes    int64   `tsk:"total_memory_bytes"`
    CacheSizeBytes      int64   `tsk:"cache_size_bytes"`
    MaxFileSizeBytes    int64   `tsk:"max_file_size_bytes"`
    PriceWithTax        float64 `tsk:"price_with_tax"`
    FinalPrice          float64 `tsk:"final_price"`
    TotalCost           float64 `tsk:"total_cost"`
}

type StatisticsConfig struct {
    UserCount    interface{} `tsk:"user_count"`
    ActiveUsers  interface{} `tsk:"active_users"`
    TotalOrders  interface{} `tsk:"total_orders"`
    TotalRevenue interface{} `tsk:"total_revenue"`
}

type CalculatedStatsConfig struct {
    ActiveUserPercentage interface{} `tsk:"active_user_percentage"`
    AverageOrderValue    interface{} `tsk:"average_order_value"`
}

type Config struct {
    App            AppConfig            `tsk:"app"`
    Database       DatabaseConfig       `tsk:"database"`
    Performance    PerformanceConfig    `tsk:"performance"`
    Calculations   CalculationsConfig   `tsk:"calculations"`
    Computed       ComputedConfig       `tsk:"computed"`
    Statistics     StatisticsConfig     `tsk:"statistics"`
    CalculatedStats CalculatedStatsConfig `tsk:"calculated_stats"`
}

func main() {
    // Load configuration
    config, err := loadConfig("config.tsk")
    if err != nil {
        log.Fatalf("Failed to load configuration: %v", err)
    }
    
    // Validate numbers
    if err := validateConfigNumbers(config); err != nil {
        log.Fatalf("Number validation failed: %v", err)
    }
    
    // Use configuration
    fmt.Printf("🚀 App Port: %d\n", config.App.Port)
    fmt.Printf("🗄️ Database Pool: %d connections\n", config.Database.PoolSize)
    fmt.Printf("⚡ Performance: %d cores, %d MB RAM\n", config.Performance.CPUCores, config.Performance.MemoryMB)
    fmt.Printf("💰 Pricing: Base $%.2f, With Tax $%.2f, Final $%.2f\n", 
        config.Calculations.BasePrice, config.Computed.PriceWithTax, config.Computed.FinalPrice)
    fmt.Printf("📊 Statistics: %v users, %v active (%.1f%%)\n", 
        config.Statistics.UserCount, config.Statistics.ActiveUsers, config.CalculatedStats.ActiveUserPercentage)
    
    // Format numbers
    fmt.Printf("💾 Memory: %s\n", formatBytes(config.Computed.TotalMemoryBytes))
    fmt.Printf("📁 Max File: %s\n", formatBytes(config.Computed.MaxFileSizeBytes))
    fmt.Printf("📈 Average Order: $%.2f\n", getFloatValue(config.CalculatedStats.AverageOrderValue))
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

func validateConfigNumbers(config *Config) error {
    // Validate app configuration
    if config.App.Port < 1 || config.App.Port > 65535 {
        return fmt.Errorf("app.port must be between 1 and 65535")
    }
    
    if config.App.Timeout < 1 || config.App.Timeout > 3600 {
        return fmt.Errorf("app.timeout must be between 1 and 3600 seconds")
    }
    
    // Validate database configuration
    if config.Database.Port < 1 || config.Database.Port > 65535 {
        return fmt.Errorf("database.port must be between 1 and 65535")
    }
    
    if config.Database.PoolSize < 1 || config.Database.PoolSize > 1000 {
        return fmt.Errorf("database.pool_size must be between 1 and 1000")
    }
    
    // Validate performance configuration
    if config.Performance.CPUCores < 1 || config.Performance.CPUCores > 64 {
        return fmt.Errorf("performance.cpu_cores must be between 1 and 64")
    }
    
    if config.Performance.MemoryMB < 64 || config.Performance.MemoryMB > 65536 {
        return fmt.Errorf("performance.memory_mb must be between 64 and 65536")
    }
    
    // Validate calculations
    if config.Calculations.TaxRate < 0 || config.Calculations.TaxRate > 1 {
        return fmt.Errorf("calculations.tax_rate must be between 0 and 1")
    }
    
    if config.Calculations.DiscountRate < 0 || config.Calculations.DiscountRate > 1 {
        return fmt.Errorf("calculations.discount_rate must be between 0 and 1")
    }
    
    return nil
}

func formatBytes(bytes int64) string {
    const unit = 1024
    if bytes < unit {
        return fmt.Sprintf("%d B", bytes)
    }
    div, exp := int64(unit), 0
    for n := bytes / unit; n >= unit; n /= unit {
        div *= unit
        exp++
    }
    return fmt.Sprintf("%.1f %cB", float64(bytes)/float64(div), "KMGTPE"[exp])
}

func getFloatValue(value interface{}) float64 {
    switch v := value.(type) {
    case float64:
        return v
    case int:
        return float64(v)
    case string:
        if f, err := strconv.ParseFloat(v, 64); err == nil {
            return f
        }
    }
    return 0.0
}
```

## 📚 Summary

You've learned:

1. **Number Fundamentals** - Basic syntax and Go struct mapping
2. **Number Types** - Integers, floats, and type-specific configurations
3. **Mathematical Operations** - Basic and advanced mathematical functions
4. **Number Validation** - Range and type validation
5. **Dynamic Numbers** - Environment-based and computed numbers
6. **Number Formatting** - Currency, percentage, and scientific notation
7. **Number Functions** - Custom mathematical and statistical functions
8. **Best Practices** - Validation, type safety, and error handling

## 🚀 Next Steps

Now that you understand number handling:

1. **Implement Validation** - Add number validation to your applications
2. **Use Mathematical Operations** - Leverage TuskLang's math functions
3. **Handle Precision** - Implement proper number formatting
4. **Create Calculations** - Build complex mathematical configurations
5. **Optimize Performance** - Use appropriate number types

---

**"We don't bow to any king"** - You now have the power to handle numbers effectively in your TuskLang Go applications! 
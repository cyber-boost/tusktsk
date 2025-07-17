# 🎛️ @ Operator System - TuskLang for C# - "The Power of @"

**Master the complete @ operator ecosystem - From environment variables to machine learning, unlock the full potential!**

The @ operator system is the heart of TuskLang's power. It transforms static configuration into dynamic, intelligent, and adaptive systems. Learn every operator and revolutionize your configuration.

## 🎯 @ Operator Philosophy

### "We Don't Bow to Any King"
- **Dynamic values** - Configuration that adapts to runtime conditions
- **Intelligent operations** - Machine learning and predictive capabilities
- **Secure handling** - Encrypted and secure data processing
- **Real-time integration** - Live data from databases, APIs, and systems

### Why @ Operators?
- **Environment awareness** - Adapt to different environments automatically
- **Security first** - Secure handling of sensitive data
- **Performance optimization** - Caching and intelligent operations
- **Real-time data** - Live integration with external systems

## 🔧 Environment and Configuration Operators

### @env - Environment Variables

```ini
# app.tsk - Environment variables
[basic_env]
app_name: @env("APP_NAME", "DefaultApp")
debug_mode: @env("DEBUG", "false")
port: @env("PORT", "8080")

[complex_env]
database_url: @env("DATABASE_URL", "postgresql://localhost:5432/myapp")
api_key: @env("API_KEY", "")
feature_flags: @env("FEATURE_FLAGS", "{}")
```

### @env.secure - Secure Environment Variables

```ini
# app.tsk - Secure environment variables
[security]
database_password: @env.secure("DB_PASSWORD")
jwt_secret: @env.secure("JWT_SECRET")
api_secret: @env.secure("API_SECRET")
master_key: @env.secure("MASTER_KEY")
```

### C# Environment Integration

```csharp
using TuskLang;

public class EnvironmentConfigurationService
{
    private readonly TuskLang _parser;
    private readonly Dictionary<string, string> _secureVariables;
    
    public EnvironmentConfigurationService()
    {
        _parser = new TuskLang();
        _secureVariables = new Dictionary<string, string>();
    }
    
    public void SetSecureVariable(string name, string value)
    {
        _secureVariables[name] = value;
    }
    
    public Dictionary<string, object> GetConfiguration(string filePath)
    {
        // Set environment variables
        Environment.SetEnvironmentVariable("APP_NAME", "MyAwesomeApp");
        Environment.SetEnvironmentVariable("DEBUG", "true");
        Environment.SetEnvironmentVariable("PORT", "8080");
        
        // Set secure variables
        SetSecureVariable("DB_PASSWORD", "secret123");
        SetSecureVariable("JWT_SECRET", "jwt-secret-key");
        
        return _parser.ParseFile(filePath);
    }
}
```

## 🗄️ Database Operators

### @query - Database Queries

```ini
# app.tsk - Database queries
[basic_queries]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > ?", @date.subtract("7d"))

[complex_queries]
user_stats: @query("""
    SELECT 
        COUNT(*) as total_users,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
        AVG(created_at) as avg_join_date
    FROM users
""")

top_products: @query("""
    SELECT name, COUNT(*) as sales
    FROM order_items
    GROUP BY name
    ORDER BY sales DESC
    LIMIT 10
""")
```

### @query.multi - Multiple Database Support

```ini
# app.tsk - Multiple databases
[multi_db]
users: @query.users("SELECT COUNT(*) FROM users")
analytics: @query.analytics("SELECT COUNT(*) FROM page_views")
cache: @query.cache("GET cache:stats")
```

### C# Database Integration

```csharp
using TuskLang;
using TuskLang.Adapters;

public class DatabaseConfigurationService
{
    private readonly TuskLang _parser;
    private readonly Dictionary<string, IDatabaseAdapter> _adapters;
    
    public DatabaseConfigurationService()
    {
        _parser = new TuskLang();
        _adapters = new Dictionary<string, IDatabaseAdapter>
        {
            ["users"] = new PostgreSQLAdapter(new PostgreSQLConfig
            {
                Host = "user-db.example.com",
                Database = "users"
            }),
            ["analytics"] = new MongoDBAdapter(new MongoDBConfig
            {
                ConnectionString = "mongodb://analytics-db.example.com:27017"
            }),
            ["cache"] = new RedisAdapter(new RedisConfig
            {
                Host = "cache.example.com",
                Port = 6379
            })
        };
        
        // Set up database routing
        foreach (var adapter in _adapters)
        {
            _parser.SetDatabaseAdapter(adapter.Key, adapter.Value);
        }
    }
    
    public Dictionary<string, object> GetConfiguration(string filePath)
    {
        return _parser.ParseFile(filePath);
    }
}
```

## 📅 Date and Time Operators

### @date - Date Operations

```ini
# app.tsk - Date operations
[date_basic]
current_time: @date.now()
current_date: @date.today()
formatted_date: @date("Y-m-d H:i:s")
iso_date: @date("c")

[date_operations]
yesterday: @date.subtract("1d")
last_week: @date.subtract("7d")
last_month: @date.subtract("1M")
next_hour: @date.add("1h")
next_year: @date.add("1Y")

[date_formatting]
custom_format: @date("F j, Y, g:i a")
unix_timestamp: @date("U")
rfc_2822: @date("r")
```

### C# Date Integration

```csharp
public class DateConfigurationService
{
    private readonly TuskLang _parser;
    
    public DateConfigurationService()
    {
        _parser = new TuskLang();
    }
    
    public Dictionary<string, object> GetConfiguration(string filePath)
    {
        var config = _parser.ParseFile(filePath);
        
        // Access date values
        var currentTime = config["date_basic"]["current_time"];
        var yesterday = config["date_operations"]["yesterday"];
        var customFormat = config["date_formatting"]["custom_format"];
        
        Console.WriteLine($"Current time: {currentTime}");
        Console.WriteLine($"Yesterday: {yesterday}");
        Console.WriteLine($"Custom format: {customFormat}");
        
        return config;
    }
}
```

## ⚡ Caching Operators

### @cache - Intelligent Caching

```ini
# app.tsk - Caching
[basic_cache]
expensive_query: @cache("5m", @query("SELECT COUNT(*) FROM large_table"))
api_response: @cache("30s", @http("GET", "https://api.example.com/data"))
ml_prediction: @cache("1m", @predict("server_load", @metrics("cpu_usage", 75)))

[advanced_cache]
# Cache with dependencies
user_data: @cache("1h", @query("SELECT * FROM users WHERE id = ?", $user_id), ["user_updates"])
product_data: @cache("30m", @query("SELECT * FROM products WHERE id = ?", $product_id), ["product_updates"])

# Cache with custom key
custom_cache: @cache("5m", @query("SELECT * FROM analytics"), "analytics_${date.today()}")

# Adaptive cache TTL
adaptive_cache: @cache(@if(@metrics("cpu_usage", 0) > 80, "1m", "5m"), @query("SELECT * FROM heavy_table"))
```

### C# Caching Integration

```csharp
using TuskLang;
using TuskLang.Caching;

public class CachedConfigurationService
{
    private readonly TuskLang _parser;
    private readonly ICacheProvider _cacheProvider;
    
    public CachedConfigurationService()
    {
        _parser = new TuskLang();
        _cacheProvider = new RedisCacheProvider(new RedisConfig
        {
            Host = "localhost",
            Port = 6379
        });
        _parser.SetCacheProvider(_cacheProvider);
    }
    
    public async Task<Dictionary<string, object>> GetConfigurationAsync(string filePath)
    {
        var config = _parser.ParseFile(filePath);
        
        // Monitor cache performance
        var stats = await _cacheProvider.GetStatsAsync();
        Console.WriteLine($"Cache hit rate: {stats.HitRate:P}");
        
        return config;
    }
    
    public async Task InvalidateCacheAsync(string pattern)
    {
        await _cacheProvider.InvalidateAsync(pattern);
    }
}
```

## 🧠 Machine Learning Operators

### @learn - Adaptive Learning

```ini
# app.tsk - Machine learning
[ml_learning]
optimal_cache_ttl: @learn("cache_ttl", "5m")
best_worker_count: @learn("worker_count", 4)
optimal_batch_size: @learn("batch_size", 100)
optimal_timeout: @learn("timeout", "30s")

[ml_features]
cache_ttl: $ml_learning.optimal_cache_ttl
worker_count: $ml_learning.best_worker_count
batch_size: $ml_learning.optimal_batch_size
timeout: $ml_learning.optimal_timeout
```

### @predict - Predictive Analytics

```ini
# app.tsk - Predictive analytics
[predictions]
predicted_load: @predict("server_load", @metrics("cpu_usage", 75))
predicted_memory: @predict("memory_usage", @metrics("memory_usage", 60))
predicted_requests: @predict("request_rate", @metrics("requests_per_second", 1000))
predicted_errors: @predict("error_rate", @metrics("error_rate", 2))

[auto_scaling]
scale_up: @if($predictions.predicted_load > 80, true, false)
target_workers: @if($predictions.predicted_load > 80, 8, 4)
memory_alert: @if($predictions.predicted_memory > 90, true, false)
```

### C# ML Integration

```csharp
using TuskLang;
using TuskLang.MachineLearning;

public class MLConfigurationService
{
    private readonly TuskLang _parser;
    private readonly IMLProvider _mlProvider;
    
    public MLConfigurationService()
    {
        _parser = new TuskLang();
        _mlProvider = new TuskMLProvider();
        _parser.SetMLProvider(_mlProvider);
    }
    
    public async Task<Dictionary<string, object>> GetMLConfigurationAsync(string filePath)
    {
        // Train models with historical data
        await TrainModelsAsync();
        
        return _parser.ParseFile(filePath);
    }
    
    private async Task TrainModelsAsync()
    {
        // Train cache TTL optimization
        await _mlProvider.TrainAsync("cache_ttl", new MLTrainingData
        {
            Features = new[] { "request_count", "response_time", "memory_usage" },
            Target = "optimal_ttl",
            HistoricalData = await GetHistoricalCacheDataAsync()
        });
        
        // Train worker count optimization
        await _mlProvider.TrainAsync("worker_count", new MLTrainingData
        {
            Features = new[] { "cpu_usage", "queue_length", "response_time" },
            Target = "optimal_workers",
            HistoricalData = await GetHistoricalWorkerDataAsync()
        });
    }
    
    private async Task<List<Dictionary<string, object>>> GetHistoricalCacheDataAsync()
    {
        // Return historical cache performance data
        return new List<Dictionary<string, object>>();
    }
    
    private async Task<List<Dictionary<string, object>>> GetHistoricalWorkerDataAsync()
    {
        // Return historical worker performance data
        return new List<Dictionary<string, object>>();
    }
}
```

## 🔐 Security Operators

### @encrypt - Data Encryption

```ini
# app.tsk - Encryption
[encryption]
connection_string: @encrypt("${user}:${password}@${host}:${port}/${database}", "AES-256-GCM")
api_key: @encrypt($api_key_raw, "AES-256-GCM")
session_secret: @encrypt($session_secret_raw, "ChaCha20-Poly1305")
jwt_secret: @encrypt($jwt_secret_raw, "AES-256-GCM")

[security_config]
encrypted_config: @encrypt($config_raw, "AES-256-GCM")
certificate_data: @encrypt($cert_raw, "ChaCha20-Poly1305")
```

### @validate - Input Validation

```ini
# app.tsk - Validation
[validation]
required_fields: @validate.required(["api_key", "database_url", "secret_key"])
email_validation: @validate.email($user_email)
url_validation: @validate.url($api_url)
numeric_validation: @validate.numeric($port)
boolean_validation: @validate.boolean($debug_mode)

[complex_validation]
password_strength: @validate.password($password, {
    min_length: 8,
    require_uppercase: true,
    require_lowercase: true,
    require_numbers: true,
    require_special: true
})
```

### C# Security Integration

```csharp
using TuskLang;
using TuskLang.Security;

public class SecureConfigurationService
{
    private readonly TuskLang _parser;
    private readonly IEncryptionProvider _encryptionProvider;
    private readonly IValidationProvider _validationProvider;
    
    public SecureConfigurationService(string masterKey)
    {
        _parser = new TuskLang();
        _encryptionProvider = new TuskEncryptionProvider(masterKey);
        _validationProvider = new TuskValidationProvider();
        
        _parser.SetEncryptionProvider(_encryptionProvider);
        _parser.SetValidationProvider(_validationProvider);
    }
    
    public Dictionary<string, object> GetSecureConfiguration(string filePath)
    {
        // Set sensitive environment variables
        Environment.SetEnvironmentVariable("api_key_raw", "your-actual-api-key");
        Environment.SetEnvironmentVariable("session_secret_raw", "your-session-secret");
        Environment.SetEnvironmentVariable("jwt_secret_raw", "your-jwt-secret");
        
        return _parser.ParseFile(filePath);
    }
    
    public string DecryptValue(string encryptedValue)
    {
        return _encryptionProvider.Decrypt(encryptedValue);
    }
    
    public bool ValidateInput(string input, string validationType)
    {
        return _validationProvider.Validate(input, validationType);
    }
}
```

## 🌐 HTTP and API Operators

### @http - HTTP Requests

```ini
# app.tsk - HTTP integration
[basic_http]
weather_data: @http("GET", "https://api.weatherapi.com/v1/current.json?key=${weather_api_key}&q=London")
stock_price: @http("GET", "https://api.stockapi.com/v1/quote?symbol=${stock_symbol}")
user_geolocation: @http("POST", "https://api.geolocation.com/locate", {
    "ip": @request.client_ip,
    "user_agent": @request.user_agent
})

[advanced_http]
api_config: @http("GET", "https://api.example.com/config", {
    "headers": {
        "Authorization": "Bearer ${api_token}",
        "Content-Type": "application/json"
    },
    "timeout": "30s"
})

webhook_data: @http("POST", "https://webhook.example.com/notify", {
    "body": {
        "event": "config_updated",
        "timestamp": @date.now(),
        "data": $config_data
    },
    "headers": {
        "X-Webhook-Signature": @encrypt($webhook_payload, "HMAC-SHA256")
    }
})
```

### C# HTTP Integration

```csharp
using TuskLang;
using TuskLang.Http;

public class HttpConfigurationService
{
    private readonly TuskLang _parser;
    private readonly IHttpProvider _httpProvider;
    
    public HttpConfigurationService()
    {
        _parser = new TuskLang();
        _httpProvider = new TuskHttpProvider(new HttpClient());
        _parser.SetHttpProvider(_httpProvider);
    }
    
    public async Task<Dictionary<string, object>> GetHttpConfigurationAsync(string filePath)
    {
        // Set request context
        var requestContext = new RequestContext
        {
            ClientIp = "192.168.1.100",
            UserAgent = "Mozilla/5.0...",
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer token123"
            }
        };
        
        _parser.SetRequestContext(requestContext);
        
        return _parser.ParseFile(filePath);
    }
}
```

## 📊 Metrics Operators

### @metrics - System Metrics

```ini
# app.tsk - Metrics
[system_metrics]
cpu_usage: @metrics("cpu_usage", 0)
memory_usage: @metrics("memory_usage", 0)
disk_usage: @metrics("disk_usage", 0)
network_io: @metrics("network_io", 0)

[application_metrics]
request_count: @metrics("requests_per_second", 0)
response_time: @metrics("average_response_time", 0)
error_rate: @metrics("error_rate", 0)
active_users: @metrics("concurrent_users", 0)

[alerts]
high_cpu: @if($system_metrics.cpu_usage > 80, true, false)
high_memory: @if($system_metrics.memory_usage > 90, true, false)
high_error_rate: @if($application_metrics.error_rate > 5, true, false)
```

### C# Metrics Integration

```csharp
using TuskLang;
using TuskLang.Metrics;

public class MetricsConfigurationService
{
    private readonly TuskLang _parser;
    private readonly IMetricsCollector _metricsCollector;
    
    public MetricsConfigurationService()
    {
        _parser = new TuskLang();
        _metricsCollector = new PrometheusMetricsCollector();
        _parser.SetMetricsCollector(_metricsCollector);
    }
    
    public async Task<Dictionary<string, object>> GetMetricsConfigurationAsync(string filePath)
    {
        // Collect current metrics
        var metrics = await _metricsCollector.CollectAsync();
        _parser.SetMetricsContext(metrics);
        
        return _parser.ParseFile(filePath);
    }
    
    public async Task StartMetricsCollectionAsync()
    {
        await _metricsCollector.StartAsync();
        
        // Collect metrics every 30 seconds
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                var metrics = await _metricsCollector.CollectAsync();
                _parser.SetMetricsContext(metrics);
            }
        });
    }
}
```

## 📁 File System Operators

### @file - File Operations

```ini
# app.tsk - File operations
[file_operations]
config_content: @file.read("config.json")
log_content: @file.read("/var/log/app.log", "last_100_lines")
certificate: @file.read("/etc/ssl/cert.pem")

[file_checks]
config_exists: @file.exists("config.json")
log_writable: @file.writable("/var/log/app.log")
cert_valid: @file.valid("/etc/ssl/cert.pem")

[file_operations]
backup_config: @file.copy("config.json", "config.backup.json")
create_log: @file.write("/var/log/app.log", "Application started at ${date.now()}")
```

### C# File Operations

```csharp
using TuskLang;
using TuskLang.FileSystem;

public class FileConfigurationService
{
    private readonly TuskLang _parser;
    private readonly IFileProvider _fileProvider;
    
    public FileConfigurationService()
    {
        _parser = new TuskLang();
        _fileProvider = new TuskFileProvider();
        _parser.SetFileProvider(_fileProvider);
    }
    
    public Dictionary<string, object> GetFileConfiguration(string filePath)
    {
        return _parser.ParseFile(filePath);
    }
    
    public async Task<string> ReadFileAsync(string path)
    {
        return await _fileProvider.ReadAsync(path);
    }
    
    public async Task WriteFileAsync(string path, string content)
    {
        await _fileProvider.WriteAsync(path, content);
    }
    
    public bool FileExists(string path)
    {
        return _fileProvider.Exists(path);
    }
}
```

## 🔄 Conditional and Logic Operators

### @if - Conditional Logic

```ini
# app.tsk - Conditional logic
$environment: @env("APP_ENV", "development")
$debug: @env("DEBUG", "false")
$load: @metrics("cpu_usage", 0)

[conditional_config]
log_level: @if($environment == "production", "error", "debug")
cache_enabled: @if($environment == "production", true, false)
worker_count: @if($load > 80, 8, 4)

[complex_conditionals]
debug_mode: @if($environment == "development" || $debug == "true", true, false)
ssl_required: @if($environment == "production", true, false)
log_format: @if($environment == "production", "json", "text")
```

### @switch - Switch Statements

```ini
# app.tsk - Switch statements
$environment: @env("APP_ENV", "development")

[switch_config]
database_host: @switch($environment, {
    "development": "localhost",
    "staging": "staging-db.example.com",
    "production": "prod-db.example.com"
})

log_level: @switch($environment, {
    "development": "debug",
    "staging": "info",
    "production": "error"
})
```

### C# Conditional Processing

```csharp
public class ConditionalConfigurationService
{
    private readonly TuskLang _parser;
    private readonly IMetricsCollector _metricsCollector;
    
    public ConditionalConfigurationService()
    {
        _parser = new TuskLang();
        _metricsCollector = new SystemMetricsCollector();
        _parser.SetMetricsCollector(_metricsCollector);
    }
    
    public async Task<Dictionary<string, object>> GetConditionalConfigurationAsync(string filePath)
    {
        // Set environment variables
        Environment.SetEnvironmentVariable("APP_ENV", "production");
        Environment.SetEnvironmentVariable("DEBUG", "true");
        
        // Collect metrics
        var metrics = await _metricsCollector.CollectAsync();
        _parser.SetMetricsContext(metrics);
        
        return _parser.ParseFile(filePath);
    }
}
```

## 🔧 Custom Operators

### Creating Custom @ Operators

```csharp
using TuskLang;
using TuskLang.Operators;

public class CustomOperatorProvider : IOperatorProvider
{
    public object Execute(string operatorName, object[] parameters)
    {
        return operatorName switch
        {
            "custom.hash" => HashValue(parameters[0].ToString()),
            "custom.validate" => ValidateInput(parameters[0].ToString()),
            "custom.transform" => TransformData(parameters[0]),
            "custom.format" => FormatData(parameters[0], parameters[1].ToString()),
            _ => throw new ArgumentException($"Unknown operator: {operatorName}")
        };
    }
    
    private string HashValue(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
    
    private bool ValidateInput(string input)
    {
        return !string.IsNullOrEmpty(input) && input.Length >= 8;
    }
    
    private object TransformData(object data)
    {
        return data.ToString().ToUpper();
    }
    
    private string FormatData(object data, string format)
    {
        return string.Format(format, data);
    }
}

// Usage in TSK file
var tskContent = @"
[security]
password_hash: @custom.hash($password)
is_valid: @custom.validate($input)
transformed: @custom.transform($data)
formatted: @custom.format($value, ""Value: {0}"")
";
```

## 🧪 Testing @ Operators

### Comprehensive Testing

```csharp
using Xunit;
using TuskLang;
using TuskLang.MachineLearning;
using TuskLang.Security;
using TuskLang.Caching;

public class OperatorTests
{
    [Fact]
    public void TestEnvironmentOperators()
    {
        // Arrange
        var parser = new TuskLang();
        Environment.SetEnvironmentVariable("TEST_VAR", "test_value");
        
        // Act
        var tskContent = @"
[env_test]
basic: @env(""TEST_VAR"", ""default"")
default: @env(""MISSING_VAR"", ""default_value"")
";
        var config = parser.Parse(tskContent);
        
        // Assert
        Assert.Equal("test_value", config["env_test"]["basic"]);
        Assert.Equal("default_value", config["env_test"]["default"]);
    }
    
    [Fact]
    public void TestDateOperators()
    {
        // Arrange
        var parser = new TuskLang();
        
        // Act
        var tskContent = @"
[date_test]
now: @date.now()
today: @date.today()
yesterday: @date.subtract(""1d"")
formatted: @date(""Y-m-d"")
";
        var config = parser.Parse(tskContent);
        
        // Assert
        Assert.NotNull(config["date_test"]["now"]);
        Assert.NotNull(config["date_test"]["today"]);
        Assert.NotNull(config["date_test"]["yesterday"]);
        Assert.NotNull(config["date_test"]["formatted"]);
    }
    
    [Fact]
    public void TestConditionalOperators()
    {
        // Arrange
        var parser = new TuskLang();
        Environment.SetEnvironmentVariable("ENV", "production");
        
        // Act
        var tskContent = @"
[conditional_test]
log_level: @if($ENV == ""production"", ""error"", ""debug"")
cache_enabled: @if($ENV == ""production"", true, false)
";
        var config = parser.Parse(tskContent);
        
        // Assert
        Assert.Equal("error", config["conditional_test"]["log_level"]);
        Assert.True(Convert.ToBoolean(config["conditional_test"]["cache_enabled"]));
    }
    
    [Fact]
    public void TestCustomOperators()
    {
        // Arrange
        var parser = new TuskLang();
        var customProvider = new CustomOperatorProvider();
        parser.SetCustomOperatorProvider(customProvider);
        
        // Act
        var tskContent = @"
[custom_test]
hash: @custom.hash(""test"")
valid: @custom.validate(""password123"")
transformed: @custom.transform(""hello"")
";
        var config = parser.Parse(tskContent);
        
        // Assert
        Assert.NotNull(config["custom_test"]["hash"]);
        Assert.True(Convert.ToBoolean(config["custom_test"]["valid"]));
        Assert.Equal("HELLO", config["custom_test"]["transformed"]);
    }
}
```

## 🎯 Best Practices

### 1. Use Appropriate Operators

```ini
# Good: Use secure operators for sensitive data
[security]
password: @env.secure("DB_PASSWORD")
api_key: @encrypt($api_key_raw, "AES-256-GCM")

# Good: Use caching for expensive operations
[performance]
expensive_query: @cache("5m", @query("SELECT COUNT(*) FROM large_table"))
api_response: @cache("30s", @http("GET", "https://api.example.com/data"))
```

### 2. Handle Errors Gracefully

```ini
# Good: Provide fallbacks for all operators
[robust_config]
database_host: @env("DB_HOST", "localhost")
api_key: @env("API_KEY", "")
cache_ttl: @cache("5m", @query("SELECT value FROM config"), "default_value")
```

### 3. Optimize Performance

```ini
# Good: Use appropriate cache TTLs
[optimized]
frequent_data: @cache("30s", @query("SELECT * FROM frequent_table"))
rare_data: @cache("1h", @query("SELECT * FROM rare_table"))
static_data: @cache("24h", @query("SELECT * FROM static_table"))
```

### 4. Security First

```ini
# Good: Always encrypt sensitive data
[secure]
connection_string: @encrypt("${user}:${password}@${host}", "AES-256-GCM")
session_secret: @env.secure("SESSION_SECRET")
api_key: @env.secure("API_KEY")
```

## 🎉 You're Ready!

You've mastered the complete @ operator system! You can now:

- ✅ **Use all environment operators** - @env, @env.secure
- ✅ **Execute database queries** - @query, @query.multi
- ✅ **Work with dates and times** - @date operations
- ✅ **Implement intelligent caching** - @cache with advanced features
- ✅ **Integrate machine learning** - @learn, @predict
- ✅ **Secure your data** - @encrypt, @validate
- ✅ **Connect to external APIs** - @http with advanced features
- ✅ **Monitor your systems** - @metrics integration
- ✅ **Manipulate files** - @file operations
- ✅ **Create custom operators** - Extend TuskLang with your own operators

## 🔥 What's Next?

Ready to optimize and deploy? Explore:

1. **[Performance Optimization](007-performance-csharp.md)** - Production optimization strategies
2. **[Production Deployment](008-production-csharp.md)** - Deploy to production environments
3. **[Best Practices](009-best-practices-csharp.md)** - Production best practices and patterns

---

**"We don't bow to any king" - Your operators, your power, your rules.**

Master the @ operator system and unlock unlimited configuration possibilities! 🚀 
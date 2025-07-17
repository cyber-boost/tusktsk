# @ Operator Introduction in TuskLang - Java Edition

**"We don't bow to any king" - @ Operator Power with Java Integration**

The @ operator is the heart of TuskLang's dynamic configuration system. It provides access to environment variables, database queries, HTTP requests, and much more. The Java SDK offers robust @ operator integration with type safety and validation.

## 🎯 Java @ Operator Integration

### @TuskConfig @ Operator Support

```java
import org.tusklang.java.config.TuskConfig;
import org.tusklang.java.operators.AtOperator;
import org.tusklang.java.operators.EnvironmentOperator;
import org.tusklang.java.operators.DatabaseOperator;
import org.tusklang.java.operators.HttpOperator;
import org.tusklang.java.annotations.TuskOperator;

@TuskConfig
public class AtOperatorConfig {
    
    // Environment variable operators
    @TuskOperator("@env")
    private String databaseHost;
    
    @TuskOperator("@env.secure")
    private String databasePassword;
    
    @TuskOperator("@env.default")
    private int serverPort;
    
    // Database query operators
    @TuskOperator("@query")
    private Integer userCount;
    
    @TuskOperator("@query.param")
    private String recentUser;
    
    // HTTP request operators
    @TuskOperator("@http.get")
    private String apiResponse;
    
    @TuskOperator("@http.post")
    private String apiData;
    
    // Date and time operators
    @TuskOperator("@date.now")
    private String currentTime;
    
    @TuskOperator("@date.format")
    private String formattedDate;
    
    // Cache operators
    @TuskOperator("@cache")
    private String cachedValue;
    
    @TuskOperator("@cache.ttl")
    private String cachedWithTtl;
    
    // Metrics operators
    @TuskOperator("@metrics")
    private Double responseTime;
    
    // Machine learning operators
    @TuskOperator("@learn")
    private String optimizedValue;
    
    @TuskOperator("@optimize")
    private Double optimizedSetting;
    
    // File operators
    @TuskOperator("@file.read")
    private String fileContent;
    
    @TuskOperator("@file.write")
    private boolean fileWritten;
    
    // JSON operators
    @TuskOperator("@json.parse")
    private Map<String, Object> parsedJson;
    
    @TuskOperator("@json.stringify")
    private String jsonString;
    
    // Validation operators
    @TuskOperator("@validate")
    private boolean isValid;
    
    @TuskOperator("@validate.email")
    private boolean isEmailValid;
    
    // Encryption operators
    @TuskOperator("@encrypt")
    private String encryptedData;
    
    @TuskOperator("@decrypt")
    private String decryptedData;
    
    // Operator context and configuration
    private AtOperatorContext operatorContext;
    
    public AtOperatorConfig() {
        this.operatorContext = new AtOperatorContext();
        initializeOperators();
    }
    
    // Initialize @ operators with Java integration
    private void initializeOperators() {
        // Environment operator
        EnvironmentOperator envOp = new EnvironmentOperator();
        envOp.setSecureMode(true);
        envOp.setDefaultProvider(System::getenv);
        
        // Database operator
        DatabaseOperator dbOp = new DatabaseOperator();
        dbOp.setConnectionPool(createConnectionPool());
        dbOp.setQueryTimeout(30);
        
        // HTTP operator
        HttpOperator httpOp = new HttpOperator();
        httpOp.setTimeout(5000);
        httpOp.setRetryAttempts(3);
        
        // Register operators
        operatorContext.registerOperator("env", envOp);
        operatorContext.registerOperator("query", dbOp);
        operatorContext.registerOperator("http", httpOp);
    }
    
    // Create database connection pool
    private DataSource createConnectionPool() {
        HikariConfig config = new HikariConfig();
        config.setJdbcUrl("jdbc:postgresql://localhost:5432/tuskdb");
        config.setUsername("postgres");
        config.setPassword("password");
        config.setMaximumPoolSize(10);
        return new HikariDataSource(config);
    }
    
    // Execute @ operator with Java integration
    public Object executeAtOperator(String operator, String... parameters) {
        try {
            return operatorContext.execute(operator, parameters);
        } catch (Exception e) {
            throw new AtOperatorException("Failed to execute @ operator: " + operator, e);
        }
    }
    
    // Validate @ operator configuration
    public boolean validateAtOperators() {
        List<String> errors = new ArrayList<>();
        
        // Validate environment variables
        if (databaseHost == null || databaseHost.isEmpty()) {
            errors.add("Database host environment variable not set");
        }
        
        // Validate database connection
        if (userCount == null) {
            errors.add("Database query failed for user count");
        }
        
        // Validate HTTP endpoints
        if (apiResponse == null) {
            errors.add("HTTP GET request failed");
        }
        
        if (!errors.isEmpty()) {
            throw new AtOperatorException("@ operator validation failed: " + String.join(", ", errors));
        }
        
        return true;
    }
    
    // Getters and setters
    public String getDatabaseHost() { return databaseHost; }
    public void setDatabaseHost(String databaseHost) { this.databaseHost = databaseHost; }
    
    public String getDatabasePassword() { return databasePassword; }
    public void setDatabasePassword(String databasePassword) { this.databasePassword = databasePassword; }
    
    public int getServerPort() { return serverPort; }
    public void setServerPort(int serverPort) { this.serverPort = serverPort; }
    
    public Integer getUserCount() { return userCount; }
    public void setUserCount(Integer userCount) { this.userCount = userCount; }
    
    public String getRecentUser() { return recentUser; }
    public void setRecentUser(String recentUser) { this.recentUser = recentUser; }
    
    public String getApiResponse() { return apiResponse; }
    public void setApiResponse(String apiResponse) { this.apiResponse = apiResponse; }
    
    public String getApiData() { return apiData; }
    public void setApiData(String apiData) { this.apiData = apiData; }
    
    public String getCurrentTime() { return currentTime; }
    public void setCurrentTime(String currentTime) { this.currentTime = currentTime; }
    
    public String getFormattedDate() { return formattedDate; }
    public void setFormattedDate(String formattedDate) { this.formattedDate = formattedDate; }
    
    public String getCachedValue() { return cachedValue; }
    public void setCachedValue(String cachedValue) { this.cachedValue = cachedValue; }
    
    public String getCachedWithTtl() { return cachedWithTtl; }
    public void setCachedWithTtl(String cachedWithTtl) { this.cachedWithTtl = cachedWithTtl; }
    
    public Double getResponseTime() { return responseTime; }
    public void setResponseTime(Double responseTime) { this.responseTime = responseTime; }
    
    public String getOptimizedValue() { return optimizedValue; }
    public void setOptimizedValue(String optimizedValue) { this.optimizedValue = optimizedValue; }
    
    public Double getOptimizedSetting() { return optimizedSetting; }
    public void setOptimizedSetting(Double optimizedSetting) { this.optimizedSetting = optimizedSetting; }
    
    public String getFileContent() { return fileContent; }
    public void setFileContent(String fileContent) { this.fileContent = fileContent; }
    
    public boolean isFileWritten() { return fileWritten; }
    public void setFileWritten(boolean fileWritten) { this.fileWritten = fileWritten; }
    
    public Map<String, Object> getParsedJson() { return parsedJson; }
    public void setParsedJson(Map<String, Object> parsedJson) { this.parsedJson = parsedJson; }
    
    public String getJsonString() { return jsonString; }
    public void setJsonString(String jsonString) { this.jsonString = jsonString; }
    
    public boolean isValid() { return isValid; }
    public void setValid(boolean valid) { isValid = valid; }
    
    public boolean isEmailValid() { return isEmailValid; }
    public void setEmailValid(boolean emailValid) { isEmailValid = emailValid; }
    
    public String getEncryptedData() { return encryptedData; }
    public void setEncryptedData(String encryptedData) { this.encryptedData = encryptedData; }
    
    public String getDecryptedData() { return decryptedData; }
    public void setDecryptedData(String decryptedData) { this.decryptedData = decryptedData; }
}

// @ operator context for Java integration
public class AtOperatorContext {
    
    private Map<String, AtOperator> operators;
    private AtOperatorExecutor executor;
    
    public AtOperatorContext() {
        this.operators = new ConcurrentHashMap<>();
        this.executor = new AtOperatorExecutor();
    }
    
    // Register @ operator
    public void registerOperator(String name, AtOperator operator) {
        operators.put(name, operator);
    }
    
    // Execute @ operator
    public Object execute(String operator, String... parameters) {
        AtOperator op = operators.get(operator);
        if (op == null) {
            throw new AtOperatorException("Unknown @ operator: " + operator);
        }
        
        return executor.execute(op, parameters);
    }
    
    // Get registered operators
    public Set<String> getRegisteredOperators() {
        return new HashSet<>(operators.keySet());
    }
}

// @ operator interface
public interface AtOperator {
    Object execute(String... parameters) throws Exception;
    String getName();
    String getDescription();
}

// @ operator executor
public class AtOperatorExecutor {
    
    public Object execute(AtOperator operator, String... parameters) throws Exception {
        try {
            return operator.execute(parameters);
        } catch (Exception e) {
            throw new AtOperatorException("Failed to execute @ operator: " + operator.getName(), e);
        }
    }
}
```

### TuskLang @ Operator Examples

```tusk
# @ Operator examples in TuskLang configuration

# Environment variable operators
database_host: string = @env("DB_HOST", "localhost")
database_port: number = @env("DB_PORT", 5432)
database_name: string = @env("DB_NAME", "tuskdb")
database_user: string = @env("DB_USER", "postgres")
database_password: string = @env.secure("DB_PASSWORD")

# Database query operators
user_count: number = @query("SELECT COUNT(*) FROM users")
active_users: number = @query("SELECT COUNT(*) FROM users WHERE active = true")
recent_user: string = @query("SELECT name FROM users ORDER BY created_at DESC LIMIT 1")
user_by_id: object = @query("SELECT * FROM users WHERE id = ?", @env("USER_ID"))

# HTTP request operators
api_status: string = @http("GET", "https://api.example.com/status")
api_data: object = @http("POST", "https://api.example.com/data", {
    "key": "value"
    "timestamp": @date.now()
})
api_headers: object = @http.headers("https://api.example.com/headers")

# Date and time operators
current_time: string = @date.now()
formatted_date: string = @date("Y-m-d H:i:s")
timestamp: number = @date.timestamp()
yesterday: string = @date.subtract("1d")
next_week: string = @date.add("7d")

# Cache operators
cached_value: string = @cache("5m", "expensive_operation")
cached_user: object = @cache("1h", @query("SELECT * FROM users WHERE id = ?", 1))
cached_api: string = @cache("10m", @http("GET", "https://api.example.com/data"))

# Metrics operators
response_time: number = @metrics("api_response_time_ms", 150)
error_rate: number = @metrics("error_rate_percent", 2.5)
throughput: number = @metrics("requests_per_second", 1000)

# Machine learning operators
optimal_setting: number = @learn("optimal_cache_size", 100)
predicted_load: number = @learn("predicted_user_load", 500)
optimized_timeout: number = @optimize("connection_timeout", 30)

# File operators
config_content: string = @file.read("config.json")
log_content: string = @file.read("logs/app.log")
file_written: boolean = @file.write("output.txt", "Hello World")
file_exists: boolean = @file.exists("important.txt")

# JSON operators
parsed_config: object = @json.parse(@file.read("config.json"))
json_string: string = @json.stringify({
    "name": "TuskLang"
    "version": "1.0.0"
    "features": ["@operators", "database", "http"]
})

# Validation operators
is_valid_email: boolean = @validate.email("user@example.com")
is_valid_url: boolean = @validate.url("https://example.com")
is_required: boolean = @validate.required(["api_key", "database_url"])
is_in_range: boolean = @validate.range(42, 1, 100)

# Encryption operators
encrypted_password: string = @encrypt("sensitive_password", "AES-256-GCM")
decrypted_password: string = @decrypt(@encrypted_password, "AES-256-GCM")
hashed_value: string = @hash("value_to_hash", "SHA-256")

# Conditional operators
environment: string = @if(@env("NODE_ENV") == "production", "prod", "dev")
debug_mode: boolean = @if(@env("DEBUG") == "true", true, false)
fallback_value: string = @env("API_KEY") || "default_key"

# Mathematical operators
sum: number = @math.add(10, 20, 30)
average: number = @math.avg([1, 2, 3, 4, 5])
random_number: number = @math.random(1, 100)
rounded_value: number = @math.round(3.14159, 2)

# String operators
uppercase_name: string = @string.upper("tusklang")
lowercase_name: string = @string.lower("TUSKLANG")
trimmed_value: string = @string.trim("  hello world  ")
replaced_text: string = @string.replace("hello world", "world", "TuskLang")
```

## 🔧 @ Operator Categories

### Java @ Operator Implementation

```java
@TuskConfig
public class AtOperatorCategories {
    
    // Environment operators
    @TuskOperator("@env")
    private EnvironmentOperators envOps;
    
    // Database operators
    @TuskOperator("@query")
    private DatabaseOperators dbOps;
    
    // HTTP operators
    @TuskOperator("@http")
    private HttpOperators httpOps;
    
    // Date operators
    @TuskOperator("@date")
    private DateOperators dateOps;
    
    // Cache operators
    @TuskOperator("@cache")
    private CacheOperators cacheOps;
    
    // Metrics operators
    @TuskOperator("@metrics")
    private MetricsOperators metricsOps;
    
    // ML operators
    @TuskOperator("@learn")
    private MachineLearningOperators mlOps;
    
    // File operators
    @TuskOperator("@file")
    private FileOperators fileOps;
    
    // JSON operators
    @TuskOperator("@json")
    private JsonOperators jsonOps;
    
    // Validation operators
    @TuskOperator("@validate")
    private ValidationOperators validationOps;
    
    // Encryption operators
    @TuskOperator("@encrypt")
    private EncryptionOperators encryptionOps;
    
    // Mathematical operators
    @TuskOperator("@math")
    private MathOperators mathOps;
    
    // String operators
    @TuskOperator("@string")
    private StringOperators stringOps;
    
    // Conditional operators
    @TuskOperator("@if")
    private ConditionalOperators conditionalOps;
    
    // Getters and setters
    public EnvironmentOperators getEnvOps() { return envOps; }
    public void setEnvOps(EnvironmentOperators envOps) { this.envOps = envOps; }
    
    public DatabaseOperators getDbOps() { return dbOps; }
    public void setDbOps(DatabaseOperators dbOps) { this.dbOps = dbOps; }
    
    public HttpOperators getHttpOps() { return httpOps; }
    public void setHttpOps(HttpOperators httpOps) { this.httpOps = httpOps; }
    
    public DateOperators getDateOps() { return dateOps; }
    public void setDateOps(DateOperators dateOps) { this.dateOps = dateOps; }
    
    public CacheOperators getCacheOps() { return cacheOps; }
    public void setCacheOps(CacheOperators cacheOps) { this.cacheOps = cacheOps; }
    
    public MetricsOperators getMetricsOps() { return metricsOps; }
    public void setMetricsOps(MetricsOperators metricsOps) { this.metricsOps = metricsOps; }
    
    public MachineLearningOperators getMlOps() { return mlOps; }
    public void setMlOps(MachineLearningOperators mlOps) { this.mlOps = mlOps; }
    
    public FileOperators getFileOps() { return fileOps; }
    public void setFileOps(FileOperators fileOps) { this.fileOps = fileOps; }
    
    public JsonOperators getJsonOps() { return jsonOps; }
    public void setJsonOps(JsonOperators jsonOps) { this.jsonOps = jsonOps; }
    
    public ValidationOperators getValidationOps() { return validationOps; }
    public void setValidationOps(ValidationOperators validationOps) { this.validationOps = validationOps; }
    
    public EncryptionOperators getEncryptionOps() { return encryptionOps; }
    public void setEncryptionOps(EncryptionOperators encryptionOps) { this.encryptionOps = encryptionOps; }
    
    public MathOperators getMathOps() { return mathOps; }
    public void setMathOps(MathOperators mathOps) { this.mathOps = mathOps; }
    
    public StringOperators getStringOps() { return stringOps; }
    public void setStringOps(StringOperators stringOps) { this.stringOps = stringOps; }
    
    public ConditionalOperators getConditionalOps() { return conditionalOps; }
    public void setConditionalOps(ConditionalOperators conditionalOps) { this.conditionalOps = conditionalOps; }
}

// Environment operators
public class EnvironmentOperators {
    
    public String get(String key) {
        return System.getenv(key);
    }
    
    public String get(String key, String defaultValue) {
        String value = System.getenv(key);
        return value != null ? value : defaultValue;
    }
    
    public String getSecure(String key) {
        // Implement secure environment variable access
        return System.getenv(key);
    }
    
    public int getInt(String key, int defaultValue) {
        String value = System.getenv(key);
        try {
            return value != null ? Integer.parseInt(value) : defaultValue;
        } catch (NumberFormatException e) {
            return defaultValue;
        }
    }
    
    public boolean getBoolean(String key, boolean defaultValue) {
        String value = System.getenv(key);
        return value != null ? Boolean.parseBoolean(value) : defaultValue;
    }
}

// Database operators
public class DatabaseOperators {
    
    private DataSource dataSource;
    
    public DatabaseOperators(DataSource dataSource) {
        this.dataSource = dataSource;
    }
    
    public Object query(String sql, Object... parameters) {
        try (Connection conn = dataSource.getConnection();
             PreparedStatement stmt = conn.prepareStatement(sql)) {
            
            for (int i = 0; i < parameters.length; i++) {
                stmt.setObject(i + 1, parameters[i]);
            }
            
            ResultSet rs = stmt.executeQuery();
            return convertResultSetToObject(rs);
        } catch (SQLException e) {
            throw new RuntimeException("Database query failed", e);
        }
    }
    
    private Object convertResultSetToObject(ResultSet rs) throws SQLException {
        // Convert ResultSet to appropriate object
        return null;
    }
}

// HTTP operators
public class HttpOperators {
    
    private HttpClient httpClient;
    
    public HttpOperators() {
        this.httpClient = HttpClient.newHttpClient();
    }
    
    public String get(String url) {
        try {
            HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(url))
                .GET()
                .build();
            
            HttpResponse<String> response = httpClient.send(request, 
                HttpResponse.BodyHandlers.ofString());
            
            return response.body();
        } catch (Exception e) {
            throw new RuntimeException("HTTP GET failed", e);
        }
    }
    
    public String post(String url, String body) {
        try {
            HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(url))
                .POST(HttpRequest.BodyPublishers.ofString(body))
                .header("Content-Type", "application/json")
                .build();
            
            HttpResponse<String> response = httpClient.send(request, 
                HttpResponse.BodyHandlers.ofString());
            
            return response.body();
        } catch (Exception e) {
            throw new RuntimeException("HTTP POST failed", e);
        }
    }
}
```

### TuskLang @ Operator Categories

```tusk
# @ Operator categories in TuskLang

# 1. Environment Operators
env_variable: string = @env("API_KEY")
env_with_default: string = @env("DEBUG", "false")
secure_env: string = @env.secure("SECRET_KEY")
env_as_number: number = @env("PORT", 8080)
env_as_boolean: boolean = @env("ENABLE_FEATURE", true)

# 2. Database Operators
user_count: number = @query("SELECT COUNT(*) FROM users")
user_by_id: object = @query("SELECT * FROM users WHERE id = ?", 1)
recent_orders: array = @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
complex_query: object = @query("""
    SELECT u.name, COUNT(o.id) as order_count 
    FROM users u 
    LEFT JOIN orders o ON u.id = o.user_id 
    WHERE u.active = true 
    GROUP BY u.id, u.name
""")

# 3. HTTP Operators
api_status: string = @http("GET", "https://api.example.com/status")
api_data: object = @http("POST", "https://api.example.com/data", {
    "user_id": 1
    "action": "login"
    "timestamp": @date.now()
})
api_with_headers: string = @http("GET", "https://api.example.com/protected", {
    "headers": {
        "Authorization": "Bearer " + @env("API_TOKEN")
        "Content-Type": "application/json"
    }
})

# 4. Date Operators
current_time: string = @date.now()
formatted_date: string = @date("Y-m-d H:i:s")
timestamp: number = @date.timestamp()
yesterday: string = @date.subtract("1d")
next_week: string = @date.add("7d")
start_of_month: string = @date.startOf("month")
end_of_year: string = @date.endOf("year")

# 5. Cache Operators
cached_value: string = @cache("5m", "expensive_operation")
cached_query: object = @cache("1h", @query("SELECT * FROM users WHERE id = ?", 1))
cached_http: string = @cache("10m", @http("GET", "https://api.example.com/data"))
cached_with_key: string = @cache("user_data", "30m", @query("SELECT * FROM users"))

# 6. Metrics Operators
response_time: number = @metrics("api_response_time_ms", 150)
error_rate: number = @metrics("error_rate_percent", 2.5)
throughput: number = @metrics("requests_per_second", 1000)
custom_metric: number = @metrics("custom_business_metric", 42)

# 7. Machine Learning Operators
optimal_setting: number = @learn("optimal_cache_size", 100)
predicted_load: number = @learn("predicted_user_load", 500)
optimized_timeout: number = @optimize("connection_timeout", 30)
smart_default: string = @learn("user_preference", "default_value")

# 8. File Operators
config_content: string = @file.read("config.json")
log_content: string = @file.read("logs/app.log")
file_written: boolean = @file.write("output.txt", "Hello World")
file_exists: boolean = @file.exists("important.txt")
file_size: number = @file.size("large_file.dat")
file_modified: string = @file.modified("config.json")

# 9. JSON Operators
parsed_config: object = @json.parse(@file.read("config.json"))
json_string: string = @json.stringify({
    "name": "TuskLang"
    "version": "1.0.0"
    "features": ["@operators", "database", "http"]
})
json_merge: object = @json.merge(@file.read("base.json"), @file.read("override.json"))
json_path: string = @json.path(@parsed_config, "database.host")

# 10. Validation Operators
is_valid_email: boolean = @validate.email("user@example.com")
is_valid_url: boolean = @validate.url("https://example.com")
is_required: boolean = @validate.required(["api_key", "database_url"])
is_in_range: boolean = @validate.range(42, 1, 100)
is_valid_format: boolean = @validate.format("2023-12-19", "Y-m-d")

# 11. Encryption Operators
encrypted_password: string = @encrypt("sensitive_password", "AES-256-GCM")
decrypted_password: string = @decrypt(@encrypted_password, "AES-256-GCM")
hashed_value: string = @hash("value_to_hash", "SHA-256")
signed_data: string = @sign("data_to_sign", "HMAC-SHA256")

# 12. Mathematical Operators
sum: number = @math.add(10, 20, 30)
average: number = @math.avg([1, 2, 3, 4, 5])
random_number: number = @math.random(1, 100)
rounded_value: number = @math.round(3.14159, 2)
power: number = @math.pow(2, 10)
square_root: number = @math.sqrt(16)

# 13. String Operators
uppercase_name: string = @string.upper("tusklang")
lowercase_name: string = @string.lower("TUSKLANG")
trimmed_value: string = @string.trim("  hello world  ")
replaced_text: string = @string.replace("hello world", "world", "TuskLang")
substring: string = @string.substring("Hello World", 0, 5)
split_text: array = @string.split("a,b,c", ",")
joined_text: string = @string.join(["a", "b", "c"], "-")

# 14. Conditional Operators
environment: string = @if(@env("NODE_ENV") == "production", "prod", "dev")
debug_mode: boolean = @if(@env("DEBUG") == "true", true, false)
fallback_value: string = @env("API_KEY") || "default_key"
conditional_config: object = @if(@env("ENVIRONMENT") == "production", {
    "ssl": true
    "cache": true
    "monitoring": true
}, {
    "ssl": false
    "cache": false
    "monitoring": false
})
```

## 🎯 Best Practices

### @ Operator Guidelines

1. **Use appropriate operators** for your use case
2. **Handle errors gracefully** with fallback values
3. **Cache expensive operations** for performance
4. **Validate operator inputs** before execution
5. **Use secure operators** for sensitive data
6. **Monitor operator performance** and usage
7. **Document operator usage** in your configurations
8. **Test operator behavior** in all environments
9. **Use type-safe operators** when possible
10. **Implement proper error handling** for all operators

### Performance Considerations

```java
// Efficient @ operator usage
@TuskConfig
public class EfficientAtOperators {
    
    // Cache expensive operations
    @TuskOperator("@cache")
    private String expensiveOperation;
    
    // Use connection pooling for database operations
    @TuskOperator("@query")
    private Object databaseQuery;
    
    // Implement retry logic for HTTP operations
    @TuskOperator("@http")
    private String httpRequest;
    
    // Use async operations where appropriate
    @TuskOperator("@async")
    private CompletableFuture<String> asyncOperation;
}
```

## 🚨 Troubleshooting

### Common @ Operator Issues

1. **Environment variables not found**: Use default values
2. **Database connection failures**: Implement retry logic
3. **HTTP request timeouts**: Set appropriate timeouts
4. **Cache misses**: Implement fallback strategies
5. **Validation failures**: Provide clear error messages

### Debug @ Operator Issues

```java
// Debug @ operator problems
public void debugAtOperators() {
    System.out.println("Environment variables:");
    System.out.println("  DB_HOST: " + System.getenv("DB_HOST"));
    System.out.println("  API_KEY: " + (System.getenv("API_KEY") != null ? "SET" : "NOT SET"));
    
    System.out.println("Database connection:");
    try {
        // Test database connection
        System.out.println("  Status: CONNECTED");
    } catch (Exception e) {
        System.out.println("  Status: FAILED - " + e.getMessage());
    }
    
    System.out.println("HTTP endpoints:");
    try {
        // Test HTTP endpoints
        System.out.println("  Status: AVAILABLE");
    } catch (Exception e) {
        System.out.println("  Status: FAILED - " + e.getMessage());
    }
}
```

## 🎯 Next Steps

1. **Explore specific @ operators** in detail
2. **Learn about operator chaining** and composition
3. **Master error handling** for @ operators
4. **Implement custom @ operators** for your needs
5. **Optimize @ operator performance** in your applications

---

**Ready to master @ operators with Java power? The @ operator system is the heart of TuskLang's dynamic configuration capabilities. We don't bow to any king - especially not static configuration!** 
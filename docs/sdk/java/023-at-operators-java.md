# @ Operators in TuskLang for Java Applications

This guide covers comprehensive usage of @ operators in TuskLang for Java applications, including all operator types, patterns, and integration strategies.

## Table of Contents

- [Overview](#overview)
- [Environment Variables (@env)](#environment-variables-env)
- [Date and Time (@date)](#date-and-time-date)
- [Caching (@cache)](#caching-cache)
- [HTTP Requests (@http)](#http-requests-http)
- [Database Queries (@query)](#database-queries-query)
- [File Operations (@file)](#file-operations-file)
- [Encryption (@encrypt)](#encryption-encrypt)
- [Metrics (@metrics)](#metrics-metrics)
- [Machine Learning (@learn)](#machine-learning-learn)
- [Custom @ Operators](#custom--operators)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

@ operators in TuskLang provide powerful dynamic configuration capabilities, allowing real-time data integration, external service calls, and advanced processing directly in configuration files.

### @ Operator Categories

```java
// Categories of @ operators
public enum AtOperatorCategory {
    ENVIRONMENT,    // Environment variables and system properties
    TIME,           // Date and time operations
    CACHE,          // Caching and storage operations
    NETWORK,        // HTTP requests and external APIs
    DATABASE,       // Database queries and operations
    FILE,           // File system operations
    SECURITY,       // Encryption and security operations
    MONITORING,     // Metrics and monitoring
    AI_ML,          // Machine learning and AI operations
    CUSTOM          // Custom operator implementations
}
```

## Environment Variables (@env)

Access and manage environment variables and system properties.

### Basic Environment Variable Access

```java
import com.tusklang.operators.EnvOperator;

public class EnvironmentVariableExample {
    
    public void useEnvironmentVariables() {
        // Basic environment variable access
        String config = """
            [app]
            name = MyApplication
            environment = @env(NODE_ENV, production)
            api_key = @env(API_KEY)
            debug_mode = @env(DEBUG_MODE, false)
            port = @env(PORT, 8080)
            """;
        
        // Configure environment operator
        EnvOperator envOp = new EnvOperator();
        envOp.setCaseSensitive(false);
        envOp.setDefaultProvider(new DefaultValueProvider());
        
        // Parse configuration
        TuskLangConfig config = parser.parse(config);
        
        // Access resolved values
        String environment = config.getString("app.environment");
        String apiKey = config.getString("app.api_key");
        boolean debugMode = config.getBoolean("app.debug_mode");
        int port = config.getInt("app.port");
        
        System.out.println("Environment: " + environment);
        System.out.println("API Key: " + apiKey);
        System.out.println("Debug Mode: " + debugMode);
        System.out.println("Port: " + port);
    }
}
```

### Advanced Environment Operations

```java
public class AdvancedEnvironmentExample {
    
    public void advancedEnvironmentOperations() {
        // Advanced environment operations
        String config = """
            [app]
            # Secure environment variable access
            secret_key = @env.secure(SECRET_KEY)
            
            # Environment variable with validation
            database_url = @env.validate(DB_URL, "jdbc:.*")
            
            # Multiple environment variables
            config_path = @env.join(PATH, ":", "/config")
            
            # Environment variable with transformation
            app_name = @env.transform(APP_NAME, "uppercase")
            
            # Conditional environment variable
            feature_flag = @env.if(ENABLE_FEATURE, true, false)
            """;
        
        // Configure advanced environment operator
        EnvOperator envOp = new EnvOperator();
        envOp.enableSecureAccess(true);
        envOp.enableValidation(true);
        envOp.enableTransformation(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced operations
        String secretKey = result.getString("app.secret_key");
        String databaseUrl = result.getString("app.database_url");
        String configPath = result.getString("app.config_path");
        String appName = result.getString("app.app_name");
        boolean featureFlag = result.getBoolean("app.feature_flag");
    }
}
```

## Date and Time (@date)

Perform date and time operations in configuration.

### Basic Date Operations

```java
import com.tusklang.operators.DateOperator;

public class DateOperatorExample {
    
    public void useDateOperators() {
        // Basic date operations
        String config = """
            [app]
            current_time = @date(now)
            current_date = @date(today)
            timestamp = @date(timestamp)
            
            # Formatted dates
            formatted_date = @date(format, "Y-m-d H:i:s")
            iso_date = @date(iso)
            
            # Specific dates
            start_date = @date(2024-01-01)
            end_date = @date(2024-12-31)
            """;
        
        // Configure date operator
        DateOperator dateOp = new DateOperator();
        dateOp.setDefaultTimezone("UTC");
        dateOp.setDefaultFormat("Y-m-d H:i:s");
        
        // Parse configuration
        TuskLangConfig result = parser.parse(config);
        
        // Access date values
        String currentTime = result.getString("app.current_time");
        String currentDate = result.getString("app.current_date");
        long timestamp = result.getLong("app.timestamp");
        String formattedDate = result.getString("app.formatted_date");
        
        System.out.println("Current time: " + currentTime);
        System.out.println("Current date: " + currentDate);
        System.out.println("Timestamp: " + timestamp);
        System.out.println("Formatted date: " + formattedDate);
    }
}
```

### Advanced Date Operations

```java
public class AdvancedDateExample {
    
    public void advancedDateOperations() {
        // Advanced date operations
        String config = """
            [app]
            # Date arithmetic
            tomorrow = @date(add, 1 day)
            next_week = @date(add, 7 days)
            next_month = @date(add, 1 month)
            
            # Date comparison
            is_weekend = @date(is_weekend)
            is_business_day = @date(is_business_day)
            
            # Date ranges
            quarter_start = @date(quarter_start)
            quarter_end = @date(quarter_end)
            
            # Custom date calculations
            age = @date(diff, 1990-01-01, years)
            days_until_deadline = @date(diff, 2024-12-31, days)
            """;
        
        // Configure advanced date operator
        DateOperator dateOp = new DateOperator();
        dateOp.enableArithmetic(true);
        dateOp.enableComparison(true);
        dateOp.enableCustomCalculations(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced date operations
        String tomorrow = result.getString("app.tomorrow");
        boolean isWeekend = result.getBoolean("app.is_weekend");
        String quarterStart = result.getString("app.quarter_start");
        int age = result.getInt("app.age");
    }
}
```

## Caching (@cache)

Implement caching operations for improved performance.

### Basic Caching

```java
import com.tusklang.operators.CacheOperator;

public class CacheOperatorExample {
    
    public void useCacheOperators() {
        // Basic caching operations
        String config = """
            [app]
            # Simple cache operations
            cached_value = @cache(get, "user_preferences")
            cache_key = @cache(key, "users", 300)
            
            # Cache with TTL
            user_data = @cache(get, "user_data", 3600)
            
            # Cache with default value
            settings = @cache(get_or_default, "app_settings", "default_settings")
            """;
        
        // Configure cache operator
        CacheOperator cacheOp = new CacheOperator();
        cacheOp.setDefaultTtl(300); // 5 minutes
        cacheOp.setCacheProvider(new RedisCacheProvider());
        
        // Parse configuration
        TuskLangConfig result = parser.parse(config);
        
        // Access cached values
        String cachedValue = result.getString("app.cached_value");
        String cacheKey = result.getString("app.cache_key");
        String userData = result.getString("app.user_data");
        String settings = result.getString("app.settings");
    }
}
```

### Advanced Caching

```java
public class AdvancedCacheExample {
    
    public void advancedCacheOperations() {
        // Advanced caching operations
        String config = """
            [app]
            # Cache with complex keys
            user_cache_key = @cache(key, "user", "${user_id}", "profile")
            
            # Cache invalidation
            cache_invalidated = @cache(invalidate, "user_data")
            
            # Cache statistics
            cache_stats = @cache(stats)
            
            # Cache with conditions
            conditional_cache = @cache.if("feature_enabled", "feature_data")
            
            # Cache with fallback
            fallback_cache = @cache.fallback("primary_cache", "backup_cache")
            """;
        
        // Configure advanced cache operator
        CacheOperator cacheOp = new CacheOperator();
        cacheOp.enableStatistics(true);
        cacheOp.enableConditionalCaching(true);
        cacheOp.enableFallbackCaching(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced cache operations
        String userCacheKey = result.getString("app.user_cache_key");
        boolean cacheInvalidated = result.getBoolean("app.cache_invalidated");
        Map<String, Object> cacheStats = result.getMap("app.cache_stats");
    }
}
```

## HTTP Requests (@http)

Make HTTP requests to external APIs and services.

### Basic HTTP Operations

```java
import com.tusklang.operators.HttpOperator;

public class HttpOperatorExample {
    
    public void useHttpOperators() {
        // Basic HTTP operations
        String config = """
            [app]
            # Simple GET request
            api_data = @http(GET, "https://api.example.com/data")
            
            # POST request with data
            post_result = @http(POST, "https://api.example.com/submit", {
                "name": "John",
                "email": "john@example.com"
            })
            
            # HTTP request with headers
            auth_data = @http(GET, "https://api.example.com/user", {
                "headers": {
                    "Authorization": "Bearer ${api_token}"
                }
            })
            """;
        
        // Configure HTTP operator
        HttpOperator httpOp = new HttpOperator();
        httpOp.setDefaultTimeout(5000);
        httpOp.setDefaultRetries(3);
        httpOp.enableCaching(true);
        
        // Parse configuration
        TuskLangConfig result = parser.parse(config);
        
        // Access HTTP results
        String apiData = result.getString("app.api_data");
        String postResult = result.getString("app.post_result");
        String authData = result.getString("app.auth_data");
    }
}
```

### Advanced HTTP Operations

```java
public class AdvancedHttpExample {
    
    public void advancedHttpOperations() {
        // Advanced HTTP operations
        String config = """
            [app]
            # HTTP request with authentication
            secure_data = @http.auth(GET, "https://api.example.com/secure", {
                "username": "${api_username}",
                "password": "${api_password}"
            })
            
            # HTTP request with custom timeout
            slow_api = @http.timeout(GET, "https://slow-api.example.com", 30000)
            
            # HTTP request with retry logic
            retry_api = @http.retry(GET, "https://unreliable-api.example.com", {
                "max_retries": 5,
                "backoff": "exponential"
            })
            
            # HTTP request with caching
            cached_api = @http.cache(GET, "https://api.example.com/data", 3600)
            """;
        
        // Configure advanced HTTP operator
        HttpOperator httpOp = new HttpOperator();
        httpOp.enableAuthentication(true);
        httpOp.enableCustomTimeouts(true);
        httpOp.enableRetryLogic(true);
        httpOp.enableResponseCaching(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced HTTP results
        String secureData = result.getString("app.secure_data");
        String slowApiData = result.getString("app.slow_api");
        String retryApiData = result.getString("app.retry_api");
        String cachedApiData = result.getString("app.cached_api");
    }
}
```

## Database Queries (@query)

Execute database queries directly in configuration.

### Basic Database Queries

```java
import com.tusklang.operators.QueryOperator;

public class QueryOperatorExample {
    
    public void useQueryOperators() {
        // Basic database queries
        String config = """
            [app]
            # Simple SELECT query
            user_count = @query("SELECT COUNT(*) FROM users")
            
            # Query with parameters
            user_by_id = @query("SELECT * FROM users WHERE id = ?", "${user_id}")
            
            # Query with multiple parameters
            users_by_status = @query("SELECT * FROM users WHERE status = ? AND active = ?", 
                                   "active", true)
            
            # Query with connection string
            db_data = @query("SELECT * FROM config", "jdbc:postgresql://localhost:5432/myapp")
            """;
        
        // Configure query operator
        QueryOperator queryOp = new QueryOperator();
        queryOp.setDefaultConnection("jdbc:postgresql://localhost:5432/myapp");
        queryOp.setDefaultTimeout(30);
        queryOp.enableConnectionPooling(true);
        
        // Parse configuration
        TuskLangConfig result = parser.parse(config);
        
        // Access query results
        int userCount = result.getInt("app.user_count");
        Map<String, Object> userById = result.getMap("app.user_by_id");
        List<Map<String, Object>> usersByStatus = result.getList("app.users_by_status");
        List<Map<String, Object>> dbData = result.getList("app.db_data");
    }
}
```

### Advanced Database Operations

```java
public class AdvancedQueryExample {
    
    public void advancedQueryOperations() {
        // Advanced database operations
        String config = """
            [app]
            # Transaction query
            transaction_result = @query.transaction([
                "INSERT INTO users (name, email) VALUES (?, ?)",
                "UPDATE user_count SET count = count + 1"
            ], ["John Doe", "john@example.com"])
            
            # Stored procedure call
            proc_result = @query.procedure("get_user_stats", "${user_id}")
            
            # Query with result mapping
            mapped_users = @query.map("SELECT id, name, email FROM users", {
                "id": "user_id",
                "name": "user_name",
                "email": "user_email"
            })
            
            # Query with caching
            cached_query = @query.cache("SELECT * FROM config", 300)
            """;
        
        // Configure advanced query operator
        QueryOperator queryOp = new QueryOperator();
        queryOp.enableTransactions(true);
        queryOp.enableStoredProcedures(true);
        queryOp.enableResultMapping(true);
        queryOp.enableQueryCaching(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced query results
        boolean transactionResult = result.getBoolean("app.transaction_result");
        Map<String, Object> procResult = result.getMap("app.proc_result");
        List<Map<String, Object>> mappedUsers = result.getList("app.mapped_users");
        List<Map<String, Object>> cachedQuery = result.getList("app.cached_query");
    }
}
```

## File Operations (@file)

Perform file system operations in configuration.

### Basic File Operations

```java
import com.tusklang.operators.FileOperator;

public class FileOperatorExample {
    
    public void useFileOperators() {
        // Basic file operations
        String config = """
            [app]
            # Read file content
            config_data = @file.read("/config/data.json")
            
            # Check file existence
            file_exists = @file.exists("/config/settings.tsk")
            
            # Get file information
            file_info = @file.info("/config/app.tsk")
            
            # List directory contents
            config_files = @file.list("/config")
            """;
        
        // Configure file operator
        FileOperator fileOp = new FileOperator();
        fileOp.setBasePath("/config");
        fileOp.setAllowedExtensions(Arrays.asList(".tsk", ".json", ".yaml"));
        fileOp.enableSecurityChecks(true);
        
        // Parse configuration
        TuskLangConfig result = parser.parse(config);
        
        // Access file operation results
        String configData = result.getString("app.config_data");
        boolean fileExists = result.getBoolean("app.file_exists");
        Map<String, Object> fileInfo = result.getMap("app.file_info");
        List<String> configFiles = result.getList("app.config_files");
    }
}
```

### Advanced File Operations

```java
public class AdvancedFileExample {
    
    public void advancedFileOperations() {
        // Advanced file operations
        String config = """
            [app]
            # File with encoding
            utf8_file = @file.read("/config/data.txt", "UTF-8")
            
            # File with line filtering
            filtered_file = @file.read_lines("/config/log.txt", {
                "filter": "ERROR",
                "limit": 100
            })
            
            # File search
            search_results = @file.search("/config", "*.tsk", "database")
            
            # File monitoring
            file_changed = @file.watch("/config/app.tsk")
            """;
        
        // Configure advanced file operator
        FileOperator fileOp = new FileOperator();
        fileOp.enableEncodingSupport(true);
        fileOp.enableLineFiltering(true);
        fileOp.enableFileSearch(true);
        fileOp.enableFileWatching(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced file results
        String utf8File = result.getString("app.utf8_file");
        List<String> filteredFile = result.getList("app.filtered_file");
        List<String> searchResults = result.getList("app.search_results");
        boolean fileChanged = result.getBoolean("app.file_changed");
    }
}
```

## Encryption (@encrypt)

Perform encryption and decryption operations.

### Basic Encryption

```java
import com.tusklang.operators.EncryptOperator;

public class EncryptOperatorExample {
    
    public void useEncryptOperators() {
        // Basic encryption operations
        String config = """
            [app]
            # Encrypt data
            encrypted_password = @encrypt("sensitive_password", "AES-256-GCM")
            
            # Decrypt data
            decrypted_data = @decrypt("${encrypted_data}", "AES-256-GCM")
            
            # Hash data
            hashed_password = @hash("user_password", "SHA-256")
            
            # Generate encryption key
            encryption_key = @encrypt.key("AES-256-GCM")
            """;
        
        // Configure encryption operator
        EncryptOperator encryptOp = new EncryptOperator();
        encryptOp.setDefaultAlgorithm("AES-256-GCM");
        encryptOp.setKeyProvider(new SecureKeyProvider());
        encryptOp.enableSecureRandom(true);
        
        // Parse configuration
        TuskLangConfig result = parser.parse(config);
        
        // Access encryption results
        String encryptedPassword = result.getString("app.encrypted_password");
        String decryptedData = result.getString("app.decrypted_data");
        String hashedPassword = result.getString("app.hashed_password");
        String encryptionKey = result.getString("app.encryption_key");
    }
}
```

### Advanced Encryption

```java
public class AdvancedEncryptExample {
    
    public void advancedEncryptOperations() {
        // Advanced encryption operations
        String config = """
            [app]
            # Encrypt with custom key
            custom_encrypted = @encrypt.with_key("sensitive_data", "${custom_key}")
            
            # Encrypt with salt
            salted_hash = @hash.salt("password", "${salt}")
            
            # Encrypt file
            encrypted_file = @encrypt.file("/config/secrets.txt", "AES-256-GCM")
            
            # Digital signature
            signature = @encrypt.sign("data_to_sign", "${private_key}")
            """;
        
        // Configure advanced encryption operator
        EncryptOperator encryptOp = new EncryptOperator();
        encryptOp.enableCustomKeys(true);
        encryptOp.enableSalting(true);
        encryptOp.enableFileEncryption(true);
        encryptOp.enableDigitalSignatures(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced encryption results
        String customEncrypted = result.getString("app.custom_encrypted");
        String saltedHash = result.getString("app.salted_hash");
        String encryptedFile = result.getString("app.encrypted_file");
        String signature = result.getString("app.signature");
    }
}
```

## Metrics (@metrics)

Collect and report metrics and monitoring data.

### Basic Metrics

```java
import com.tusklang.operators.MetricsOperator;

public class MetricsOperatorExample {
    
    public void useMetricsOperators() {
        // Basic metrics operations
        String config = """
            [app]
            # Record metric
            response_time = @metrics(record, "api_response_time", 150)
            
            # Increment counter
            request_count = @metrics(increment, "api_requests")
            
            # Set gauge
            active_users = @metrics(gauge, "active_users", 1250)
            
            # Get metric value
            current_metric = @metrics(get, "api_response_time")
            """;
        
        // Configure metrics operator
        MetricsOperator metricsOp = new MetricsOperator();
        metricsOp.setMetricsProvider(new PrometheusMetricsProvider());
        metricsOp.setDefaultNamespace("myapp");
        metricsOp.enableAutoReporting(true);
        
        // Parse configuration
        TuskLangConfig result = parser.parse(config);
        
        // Access metrics results
        double responseTime = result.getDouble("app.response_time");
        long requestCount = result.getLong("app.request_count");
        int activeUsers = result.getInt("app.active_users");
        double currentMetric = result.getDouble("app.current_metric");
    }
}
```

### Advanced Metrics

```java
public class AdvancedMetricsExample {
    
    public void advancedMetricsOperations() {
        // Advanced metrics operations
        String config = """
            [app]
            # Metric with labels
            labeled_metric = @metrics.with_labels("api_requests", {
                "endpoint": "/api/users",
                "method": "GET",
                "status": "200"
            })
            
            # Metric histogram
            histogram_metric = @metrics.histogram("response_time", 150, {
                "buckets": [50, 100, 200, 500, 1000]
            })
            
            # Metric summary
            summary_metric = @metrics.summary("data_size", 1024, {
                "quantiles": [0.5, 0.9, 0.99]
            })
            
            # Custom metric
            custom_metric = @metrics.custom("business_metric", "revenue", 50000)
            """;
        
        // Configure advanced metrics operator
        MetricsOperator metricsOp = new MetricsOperator();
        metricsOp.enableLabels(true);
        metricsOp.enableHistograms(true);
        metricsOp.enableSummaries(true);
        metricsOp.enableCustomMetrics(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced metrics results
        long labeledMetric = result.getLong("app.labeled_metric");
        double histogramMetric = result.getDouble("app.histogram_metric");
        double summaryMetric = result.getDouble("app.summary_metric");
        double customMetric = result.getDouble("app.custom_metric");
    }
}
```

## Machine Learning (@learn)

Integrate machine learning capabilities in configuration.

### Basic Machine Learning

```java
import com.tusklang.operators.LearnOperator;

public class LearnOperatorExample {
    
    public void useLearnOperators() {
        // Basic machine learning operations
        String config = """
            [app]
            # Predict value
            predicted_value = @learn(predict, "price_model", {
                "features": [100, 5, 2020]
            })
            
            # Train model
            model_trained = @learn(train, "user_model", {
                "data": "${training_data}",
                "algorithm": "random_forest"
            })
            
            # Get model accuracy
            model_accuracy = @learn(accuracy, "price_model")
            
            # Feature importance
            feature_importance = @learn(importance, "user_model")
            """;
        
        // Configure learning operator
        LearnOperator learnOp = new LearnOperator();
        learnOp.setModelProvider(new ScikitLearnProvider());
        learnOp.setDefaultAlgorithm("random_forest");
        learnOp.enableAutoTraining(true);
        
        // Parse configuration
        TuskLangConfig result = parser.parse(config);
        
        // Access learning results
        double predictedValue = result.getDouble("app.predicted_value");
        boolean modelTrained = result.getBoolean("app.model_trained");
        double modelAccuracy = result.getDouble("app.model_accuracy");
        Map<String, Double> featureImportance = result.getMap("app.feature_importance");
    }
}
```

### Advanced Machine Learning

```java
public class AdvancedLearnExample {
    
    public void advancedLearnOperations() {
        // Advanced machine learning operations
        String config = """
            [app]
            # Hyperparameter optimization
            optimized_model = @learn.optimize("price_model", {
                "param_grid": {
                    "n_estimators": [100, 200, 300],
                    "max_depth": [10, 20, 30]
                },
                "cv_folds": 5
            })
            
            # Model ensemble
            ensemble_prediction = @learn.ensemble([
                "model1", "model2", "model3"
            ], "voting")
            
            # Feature engineering
            engineered_features = @learn.features("user_data", {
                "transformations": ["scaling", "encoding", "selection"]
            })
            
            # Model deployment
            deployed_model = @learn.deploy("price_model", "production")
            """;
        
        // Configure advanced learning operator
        LearnOperator learnOp = new LearnOperator();
        learnOp.enableHyperparameterOptimization(true);
        learnOp.enableEnsembleMethods(true);
        learnOp.enableFeatureEngineering(true);
        learnOp.enableModelDeployment(true);
        
        // Parse with advanced operations
        TuskLangConfig result = parser.parse(config);
        
        // Access advanced learning results
        boolean optimizedModel = result.getBoolean("app.optimized_model");
        double ensemblePrediction = result.getDouble("app.ensemble_prediction");
        List<Double> engineeredFeatures = result.getList("app.engineered_features");
        boolean deployedModel = result.getBoolean("app.deployed_model");
    }
}
```

## Custom @ Operators

Create custom @ operators for specific use cases.

### Custom Operator Implementation

```java
public class CustomOperatorExample {
    
    public void implementCustomOperator() {
        // Custom operator implementation
        CustomOperator customOp = new CustomOperator();
        
        // Register custom operators
        customOp.registerOperator("weather", new WeatherOperator());
        customOp.registerOperator("currency", new CurrencyOperator());
        customOp.registerOperator("geolocation", new GeolocationOperator());
        
        // Use custom operators in configuration
        String config = """
            [app]
            # Custom weather operator
            current_weather = @weather(current, "New York")
            
            # Custom currency operator
            exchange_rate = @currency(convert, "USD", "EUR", 100)
            
            # Custom geolocation operator
            location_info = @geolocation(lookup, "192.168.1.1")
            """;
        
        // Parse with custom operators
        TuskLangConfig result = parser.parse(config);
        
        // Access custom operator results
        Map<String, Object> weather = result.getMap("app.current_weather");
        double exchangeRate = result.getDouble("app.exchange_rate");
        Map<String, Object> locationInfo = result.getMap("app.location_info");
    }
}

// Custom weather operator implementation
class WeatherOperator implements AtOperator {
    
    @Override
    public String execute(String operation, String... args) {
        switch (operation) {
            case "current":
                return getCurrentWeather(args[0]);
            case "forecast":
                return getWeatherForecast(args[0], Integer.parseInt(args[1]));
            default:
                throw new IllegalArgumentException("Unknown weather operation: " + operation);
        }
    }
    
    private String getCurrentWeather(String city) {
        // Implementation for getting current weather
        return "{\"temperature\": 22, \"condition\": \"sunny\"}";
    }
    
    private String getWeatherForecast(String city, int days) {
        // Implementation for getting weather forecast
        return "{\"forecast\": \"sunny for " + days + " days\"}";
    }
}
```

## Best Practices

Follow these best practices for effective @ operator usage.

### Performance Optimization

```java
public class PerformanceOptimizationExample {
    
    public void optimizePerformance() {
        // Performance optimization strategies
        AtOperatorOptimizer optimizer = new AtOperatorOptimizer();
        
        // Enable caching for expensive operations
        optimizer.enableCaching(true);
        optimizer.setCacheTtl(300); // 5 minutes
        
        // Enable parallel execution
        optimizer.enableParallelExecution(true);
        optimizer.setMaxConcurrency(10);
        
        // Enable lazy evaluation
        optimizer.enableLazyEvaluation(true);
        
        // Configure timeout for external operations
        optimizer.setDefaultTimeout(5000); // 5 seconds
        
        // Use optimized configuration
        String config = """
            [app]
            # Cached expensive operations
            cached_data = @cache(@http(GET, "https://slow-api.example.com"), 3600)
            
            # Parallel operations
            parallel_data = @parallel([
                @http(GET, "https://api1.example.com"),
                @http(GET, "https://api2.example.com"),
                @http(GET, "https://api3.example.com")
            ])
            """;
        
        TuskLangConfig result = optimizer.parse(config);
    }
}
```

### Security Best Practices

```java
public class SecurityBestPracticesExample {
    
    public void implementSecurityBestPractices() {
        // Security best practices
        AtOperatorSecurity security = new AtOperatorSecurity();
        
        // Enable input validation
        security.enableInputValidation(true);
        
        // Enable output sanitization
        security.enableOutputSanitization(true);
        
        // Configure allowed operations
        security.setAllowedOperations(Arrays.asList("env", "date", "cache"));
        
        // Configure restricted operations
        security.setRestrictedOperations(Arrays.asList("file", "system"));
        
        // Enable audit logging
        security.enableAuditLogging(true);
        
        // Use secure configuration
        String config = """
            [app]
            # Secure environment variable access
            api_key = @env.secure(API_KEY)
            
            # Validated input
            user_input = @validate(input, "${user_provided_input}", "email")
            
            # Sanitized output
            safe_output = @sanitize("${potentially_unsafe_data}")
            """;
        
        TuskLangConfig result = security.parse(config);
    }
}
```

## Troubleshooting

Common issues and solutions for @ operators.

### Common Issues

```java
public class TroubleshootingExample {
    
    public void troubleshootOperators() {
        // Troubleshooting common issues
        AtOperatorTroubleshooter troubleshooter = new AtOperatorTroubleshooter();
        
        // Check operator availability
        List<String> availableOperators = troubleshooter.getAvailableOperators();
        System.out.println("Available operators: " + availableOperators);
        
        // Check operator configuration
        OperatorConfiguration config = troubleshooter.getOperatorConfiguration();
        System.out.println("Operator configuration: " + config);
        
        // Test operator functionality
        OperatorTestResult testResult = troubleshooter.testOperator("env");
        if (!testResult.isSuccessful()) {
            System.out.println("Operator test failed: " + testResult.getError());
        }
        
        // Diagnose performance issues
        PerformanceDiagnostic perfDiagnostic = troubleshooter.diagnosePerformance();
        System.out.println("Performance issues: " + perfDiagnostic.getIssues());
    }
}
```

This comprehensive @ operators guide provides everything needed to effectively use TuskLang's powerful @ operator system in Java applications, enabling dynamic, real-time configuration capabilities. 
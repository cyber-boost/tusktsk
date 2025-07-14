# 🟨 TuskLang JavaScript Advanced Features Guide

**"We don't bow to any king" - JavaScript Edition**

Master the most powerful features of TuskLang: @ operators, FUJSEN (executable functions), machine learning, metrics, and advanced configuration patterns.

## ⚡ @ Operator System

The @ operator system is the heart of TuskLang's power. It provides dynamic, executable functionality directly in your configuration files.

### 🔧 Environment Variables

```javascript
const TuskLang = require('tusklang');

const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
    
    # Basic environment variables
    debug: @env("DEBUG", false)
    port: @env("PORT", 3000)
    host: @env("HOST", "localhost")
    
    # Secure environment variables (encrypted)
    api_key: @env.secure("API_KEY")
    database_password: @env.secure("DB_PASSWORD")
    jwt_secret: @env.secure("JWT_SECRET")
    
    # Environment-specific configuration
    environment: @env("NODE_ENV", "development")
    log_level: @if(@env("NODE_ENV") == "production", "error", "debug")
    
    # Complex environment logic
    database_url: @if(@env("NODE_ENV") == "production",
        @env("DATABASE_URL"),
        "postgresql://localhost:5432/myapp_dev"
    )
}

# Environment variable validation
validation {
    required_vars: @validate.required([
        "API_KEY",
        "DB_PASSWORD", 
        "JWT_SECRET"
    ])
    
    # Type validation
    port_number: @validate.integer(@env("PORT", "3000"), 1, 65535)
    debug_mode: @validate.boolean(@env("DEBUG", "false"))
    timeout_ms: @validate.number(@env("TIMEOUT", "5000"), 100, 60000)
}
`);

// Set environment variables
process.env.API_KEY = "your-secret-api-key";
process.env.DB_PASSWORD = "your-secure-password";
process.env.JWT_SECRET = "your-jwt-secret";
process.env.NODE_ENV = "production";

const result = TuskLang.parse(config);
console.log(`App: ${result.app.name} on port ${result.app.port}`);
console.log(`Environment: ${result.app.environment}`);
console.log(`Database: ${result.app.database_url}`);
```

### 📅 Date and Time Operations

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
    
    # Current timestamp
    started_at: @date.now()
    current_time: @date.format("Y-m-d H:i:s")
    unix_timestamp: @date.unix()
    
    # Date calculations
    one_week_ago: @date.subtract("7d")
    one_month_ago: @date.subtract("1M")
    one_year_ago: @date.subtract("1y")
    
    # Future dates
    one_week_from_now: @date.add("7d")
    one_month_from_now: @date.add("1M")
    
    # Date formatting
    formatted_date: @date.format("Y-m-d H:i:s")
    iso_date: @date.format("c")
    custom_format: @date.format("l, F j, Y")
    
    # Date comparisons
    is_weekend: @date.is_weekend()
    is_holiday: @date.is_holiday()
    day_of_week: @date.day_of_week()
    day_of_year: @date.day_of_year()
}

# Database queries with date operations
database {
    recent_users: @query("SELECT * FROM users WHERE created_at > ?", @date.subtract("7d"))
    today_orders: @query("SELECT * FROM orders WHERE DATE(created_at) = ?", @date.format("Y-m-d"))
    weekly_stats: @query("""
        SELECT 
            DATE(created_at) as date,
            COUNT(*) as count
        FROM posts 
        WHERE created_at >= ? AND created_at < ?
        GROUP BY DATE(created_at)
        ORDER BY date
    """, @date.subtract("7d"), @date.now())
    
    # Expiring sessions
    expired_sessions: @query("""
        SELECT * FROM sessions 
        WHERE expires_at < ?
    """, @date.now())
    
    # Birthday notifications
    upcoming_birthdays: @query("""
        SELECT * FROM users 
        WHERE DATE_FORMAT(birthday, '%m-%d') 
        BETWEEN DATE_FORMAT(?, '%m-%d') 
        AND DATE_FORMAT(?, '%m-%d')
    """, @date.now(), @date.add("7d"))
}
`);
```

### 💾 Caching System

```javascript
const TuskLang = require('tusklang');
const RedisAdapter = require('tusklang/adapters/redis');

// Create Redis adapter for caching
const redis = new RedisAdapter({
    host: 'localhost',
    port: 6379
});

const tsk = new TuskLang.Enhanced();
tsk.setCacheAdapter(redis);

const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# Caching configuration
cache {
    # Basic caching
    user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
    active_users: @cache("1m", @query("SELECT COUNT(*) FROM users WHERE active = true"))
    
    # Cached expensive operations
    popular_posts: @cache("1h", @query("""
        SELECT p.*, u.name as author_name, COUNT(c.id) as comment_count
        FROM posts p
        JOIN users u ON p.user_id = u.id
        LEFT JOIN comments c ON p.id = c.post_id
        GROUP BY p.id, p.title, p.content, p.created_at, u.name
        ORDER BY comment_count DESC, p.created_at DESC
        LIMIT 20
    """))
    
    # Conditional caching
    user_profile: @if(@cache.exists("user:${user_id}"),
        @cache.get("user:${user_id}"),
        @cache.set("user:${user_id}", @query("SELECT * FROM users WHERE id = ?", @request.user_id), "30m")
    )
    
    # Cache with dependencies
    user_posts: @cache.with_deps("user_posts:${user_id}", ["user:${user_id}"], "15m",
        @query("SELECT * FROM posts WHERE user_id = ? ORDER BY created_at DESC", @request.user_id)
    )
    
    # Cache warming
    warm_cache: @cache.warm([
        "user_count",
        "active_users", 
        "popular_posts"
    ])
    
    # Cache invalidation
    invalidate_user: @cache.invalidate("user:${user_id}")
    invalidate_pattern: @cache.invalidate_pattern("user:*")
}

# Performance monitoring with caching
performance {
    # Cache hit rates
    cache_hit_rate: @cache.stats("hit_rate")
    cache_miss_rate: @cache.stats("miss_rate")
    
    # Cache size
    cache_size: @cache.stats("size")
    cache_keys: @cache.stats("keys")
    
    # Cache performance
    avg_get_time: @cache.stats("avg_get_time")
    avg_set_time: @cache.stats("avg_set_time")
}
`);
```

### 📊 Metrics and Monitoring

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# Metrics collection
metrics {
    # Application metrics
    app_start_time: @metrics.timer("app.start_time")
    request_count: @metrics.counter("requests.total")
    active_users: @metrics.gauge("users.active")
    
    # Database metrics
    db_query_time: @metrics.timer("database.query_time")
    db_connection_count: @metrics.gauge("database.connections")
    db_error_count: @metrics.counter("database.errors")
    
    # Cache metrics
    cache_hit_rate: @metrics.gauge("cache.hit_rate")
    cache_miss_rate: @metrics.gauge("cache.miss_rate")
    
    # Business metrics
    orders_per_minute: @metrics.rate("orders.per_minute")
    revenue_per_hour: @metrics.rate("revenue.per_hour")
    user_registrations: @metrics.counter("users.registrations")
    
    # Custom metrics
    custom_metric: @metrics.custom("my_custom_metric", 42)
    
    # Histogram for response times
    response_time: @metrics.histogram("response_time_ms", 150)
    
    # Summary for complex metrics
    order_value: @metrics.summary("order_value_usd", 29.99)
}

# Real-time monitoring
monitoring {
    # System health
    cpu_usage: @metrics.system("cpu_usage")
    memory_usage: @metrics.system("memory_usage")
    disk_usage: @metrics.system("disk_usage")
    
    # Application health
    response_time: @metrics.timer("app.response_time")
    error_rate: @metrics.rate("app.errors")
    throughput: @metrics.rate("app.requests_per_second")
    
    # Alerting thresholds
    high_cpu_alert: @if(@metrics.system("cpu_usage") > 80, true, false)
    low_memory_alert: @if(@metrics.system("memory_usage") < 10, true, false)
    high_error_rate: @if(@metrics.rate("app.errors") > 0.05, true, false)
}
`);
```

### 🧠 Machine Learning Integration

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# Machine learning features
ml {
    # Learning from user behavior
    optimal_cache_ttl: @learn("cache_ttl", "5m", {
        context: "user_activity",
        metric: "cache_hit_rate"
    })
    
    # Dynamic rate limiting
    optimal_rate_limit: @learn("rate_limit", 100, {
        context: "server_load",
        metric: "response_time"
    })
    
    # Personalized recommendations
    user_recommendations: @learn("user_recommendations", [], {
        context: "user_preferences",
        metric: "engagement_rate"
    })
    
    # Auto-scaling decisions
    optimal_workers: @learn("worker_count", 4, {
        context: "traffic_pattern",
        metric: "throughput"
    })
    
    # A/B testing
    feature_flag: @learn("new_feature_enabled", false, {
        context: "user_segment",
        metric: "conversion_rate"
    })
}

# Optimization engine
optimization {
    # Performance optimization
    optimal_query_timeout: @optimize("query_timeout", 5000, {
        constraint: "response_time < 1000ms",
        metric: "throughput"
    })
    
    # Resource optimization
    optimal_memory_limit: @optimize("memory_limit", "512MB", {
        constraint: "cost < $100/month",
        metric: "performance_score"
    })
    
    # Cache optimization
    optimal_cache_size: @optimize("cache_size", "1GB", {
        constraint: "memory_usage < 80%",
        metric: "cache_hit_rate"
    })
}
`);
```

## 🚀 FUJSEN - Executable Functions

FUJSEN (Functional JavaScript Engine) allows you to embed executable JavaScript functions directly in your configuration files.

### Basic FUJSEN Functions

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# Basic function execution
processing {
    # Simple function
    double_value: """function(x) { return x * 2; }"""
    
    # Function with parameters
    calculate_total: """function(price, tax_rate) { 
        return price * (1 + tax_rate); 
    }"""
    
    # Complex function
    format_currency: """function(amount, currency = 'USD') {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: currency
        }).format(amount);
    }"""
    
    # Array processing
    filter_active_users: """function(users) {
        return users.filter(user => user.active);
    }"""
    
    # Object transformation
    transform_user: """function(user) {
        return {
            id: user.id,
            name: user.first_name + ' ' + user.last_name,
            email: user.email,
            is_active: user.active
        };
    }"""
}

# Execute functions with data
execution {
    # Execute with static data
    doubled: @fujsen(processing.double_value, 42)
    total_price: @fujsen(processing.calculate_total, 29.99, 0.08)
    formatted_price: @fujsen(processing.format_currency, 29.99, 'EUR')
    
    # Execute with database data
    active_user_count: @fujsen(processing.filter_active_users, 
        @query("SELECT * FROM users")
    ).length
    
    # Execute with complex data
    transformed_users: @fujsen(processing.transform_user,
        @query("SELECT * FROM users WHERE id = ?", @request.user_id)
    )
}
`);
```

### Advanced FUJSEN Patterns

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# Advanced function patterns
advanced_functions {
    # Async function
    fetch_user_data: """async function(userId) {
        const response = await fetch(`https://api.example.com/users/${userId}`);
        return await response.json();
    }"""
    
    # Error handling
    safe_divide: """function(a, b) {
        try {
            if (b === 0) throw new Error('Division by zero');
            return a / b;
        } catch (error) {
            console.error('Division error:', error.message);
            return null;
        }
    }"""
    
    # Validation function
    validate_email: """function(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }"""
    
    # Data transformation pipeline
    process_user_data: """function(users) {
        return users
            .filter(user => user.active)
            .map(user => ({
                id: user.id,
                name: user.first_name + ' ' + user.last_name,
                email: user.email,
                created_date: new Date(user.created_at).toLocaleDateString()
            }))
            .sort((a, b) => a.name.localeCompare(b.name));
    }"""
    
    # Business logic
    calculate_discount: """function(order_total, user_type) {
        let discount = 0;
        
        if (user_type === 'premium') {
            discount = order_total * 0.15;
        } else if (user_type === 'vip') {
            discount = order_total * 0.25;
        } else if (order_total > 100) {
            discount = order_total * 0.10;
        }
        
        return Math.min(discount, 50); // Max discount $50
    }"""
    
    # Configuration generation
    generate_config: """function(environment) {
        const configs = {
            development: {
                debug: true,
                log_level: 'debug',
                cache_ttl: '5m'
            },
            staging: {
                debug: false,
                log_level: 'info',
                cache_ttl: '15m'
            },
            production: {
                debug: false,
                log_level: 'error',
                cache_ttl: '1h'
            }
        };
        
        return configs[environment] || configs.development;
    }"""
}

# Execute advanced functions
execution {
    # Async execution
    user_data: @fujsen(advanced_functions.fetch_user_data, @request.user_id)
    
    # Safe operations
    safe_result: @fujsen(advanced_functions.safe_divide, 10, 2)
    
    # Validation
    is_valid_email: @fujsen(advanced_functions.validate_email, @request.email)
    
    # Data processing
    processed_users: @fujsen(advanced_functions.process_user_data,
        @query("SELECT * FROM users")
    )
    
    # Business calculations
    discount_amount: @fujsen(advanced_functions.calculate_discount, 
        @request.order_total, 
        @request.user_type
    )
    
    # Dynamic configuration
    env_config: @fujsen(advanced_functions.generate_config, @env("NODE_ENV"))
}
`);
```

### FUJSEN with Database Integration

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# Database processing functions
database_functions {
    # User analytics
    user_analytics: """function(userId) {
        const user = this.query('SELECT * FROM users WHERE id = ?', userId);
        const posts = this.query('SELECT * FROM posts WHERE user_id = ?', userId);
        const orders = this.query('SELECT * FROM orders WHERE user_id = ?', userId);
        
        return {
            user: user,
            post_count: posts.length,
            total_spent: orders.reduce((sum, order) => sum + order.amount, 0),
            avg_order_value: orders.length > 0 ? 
                orders.reduce((sum, order) => sum + order.amount, 0) / orders.length : 0,
            last_activity: posts.length > 0 ? 
                Math.max(...posts.map(p => new Date(p.created_at))) : null
        };
    }"""
    
    # Complex reporting
    generate_report: """function(startDate, endDate) {
        const users = this.query(`
            SELECT 
                u.id, u.name, u.email,
                COUNT(p.id) as post_count,
                SUM(o.amount) as total_spent,
                MAX(p.created_at) as last_post
            FROM users u
            LEFT JOIN posts p ON u.id = p.user_id
            LEFT JOIN orders o ON u.id = o.user_id
            WHERE u.created_at BETWEEN ? AND ?
            GROUP BY u.id, u.name, u.email
            ORDER BY total_spent DESC
        `, startDate, endDate);
        
        const summary = {
            total_users: users.length,
            total_posts: users.reduce((sum, user) => sum + user.post_count, 0),
            total_revenue: users.reduce((sum, user) => sum + (user.total_spent || 0), 0),
            avg_revenue_per_user: users.length > 0 ? 
                users.reduce((sum, user) => sum + (user.total_spent || 0), 0) / users.length : 0
        };
        
        return { users, summary };
    }"""
    
    # Data migration
    migrate_user_data: """function(oldUserId, newUserId) {
        // Update posts
        this.query('UPDATE posts SET user_id = ? WHERE user_id = ?', newUserId, oldUserId);
        
        // Update orders
        this.query('UPDATE orders SET user_id = ? WHERE user_id = ?', newUserId, oldUserId);
        
        // Update comments
        this.query('UPDATE comments SET user_id = ? WHERE user_id = ?', newUserId, oldUserId);
        
        // Delete old user
        this.query('DELETE FROM users WHERE id = ?', oldUserId);
        
        return { success: true, migrated_user_id: newUserId };
    }"""
}

# Execute database functions
execution {
    # User analytics
    user_stats: @fujsen(database_functions.user_analytics, @request.user_id)
    
    # Generate report
    monthly_report: @fujsen(database_functions.generate_report, 
        @date.subtract("30d"), 
        @date.now()
    )
    
    # Data migration (with validation)
    migration_result: @if(@request.confirm_migration,
        @fujsen(database_functions.migrate_user_data, 
            @request.old_user_id, 
            @request.new_user_id
        ),
        { success: false, reason: "Migration not confirmed" }
    )
}
`);
```

## 🔗 Advanced @ Operator Patterns

### Conditional Logic

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# Advanced conditional logic
conditional {
    # Environment-based configuration
    database_config: @if(@env("NODE_ENV") == "production", {
        host: "prod-db.example.com",
        ssl: true,
        pool_size: 20,
        timeout: 5000
    }, {
        host: "localhost",
        ssl: false,
        pool_size: 5,
        timeout: 30000
    })
    
    # Feature flags
    features: @if(@env("ENABLE_NEW_FEATURES") == "true", {
        new_ui: true,
        advanced_search: true,
        real_time_updates: true
    }, {
        new_ui: false,
        advanced_search: false,
        real_time_updates: false
    })
    
    # User role-based access
    user_permissions: @if(@request.user_role == "admin", {
        can_delete: true,
        can_edit: true,
        can_view_all: true
    }, @if(@request.user_role == "moderator", {
        can_delete: false,
        can_edit: true,
        can_view_all: true
    }, {
        can_delete: false,
        can_edit: false,
        can_view_all: false
    }))
    
    # Dynamic rate limiting
    rate_limit: @if(@metrics.system("cpu_usage") > 80, 50,
        @if(@metrics.system("cpu_usage") > 60, 100, 200)
    )
}
`);
```

### File Operations

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# File operations
files {
    # Read configuration files
    global_config: @file.read("config/global.json")
    user_config: @file.read("config/users.json")
    
    # Read and parse JSON
    settings: @file.json("config/settings.json")
    translations: @file.json("locales/en.json")
    
    # File existence checks
    config_exists: @file.exists("config/app.json")
    log_file_exists: @file.exists("logs/app.log")
    
    # File information
    config_size: @file.size("config/app.json")
    log_size: @file.size("logs/app.log")
    config_modified: @file.modified("config/app.json")
    
    # Directory operations
    config_files: @file.list("config/")
    log_files: @file.list("logs/")
    
    # File content validation
    is_valid_json: @file.is_json("config/settings.json")
    is_valid_yaml: @file.is_yaml("config/database.yml")
}
`);
```

### HTTP Operations

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# HTTP operations
http {
    # GET requests
    api_status: @http.get("https://api.example.com/status")
    user_data: @http.get("https://api.example.com/users/${user_id}")
    
    # POST requests
    create_user: @http.post("https://api.example.com/users", {
        name: @request.user_name,
        email: @request.user_email
    })
    
    # PUT requests
    update_user: @http.put("https://api.example.com/users/${user_id}", {
        name: @request.user_name,
        email: @request.user_email
    })
    
    # DELETE requests
    delete_user: @http.delete("https://api.example.com/users/${user_id}")
    
    # Custom headers
    authenticated_request: @http.get("https://api.example.com/protected", {
        headers: {
            "Authorization": "Bearer " + @env("API_TOKEN"),
            "Content-Type": "application/json"
        }
    })
    
    # Cached HTTP requests
    cached_api_data: @cache("5m", @http.get("https://api.example.com/data"))
    
    # Error handling
    safe_api_call: @http.safe("https://api.example.com/data", {
        timeout: 5000,
        retries: 3,
        fallback: { data: [] }
    })
}
`);
```

## 📚 Next Steps

1. **[Framework Integration](006-framework-integration-javascript.md)** - Use with Express.js, Next.js, and more
2. **[Production Deployment](007-production-deployment-javascript.md)** - Deploy with confidence
3. **[Performance Optimization](008-performance-optimization-javascript.md)** - Optimize your applications
4. **[Security Best Practices](009-security-best-practices-javascript.md)** - Secure your applications

## 🎉 You've Mastered Advanced Features!

You now understand:
- ✅ **@ Operator System** - Environment variables, dates, caching, metrics
- ✅ **FUJSEN Functions** - Executable JavaScript in configuration
- ✅ **Machine Learning** - @learn and @optimize operators
- ✅ **Advanced Patterns** - Conditional logic, file operations, HTTP requests
- ✅ **Database Integration** - Complex queries with @ operators
- ✅ **Performance Monitoring** - Metrics and real-time monitoring
- ✅ **Caching Strategies** - Advanced caching patterns

**Ready to integrate with frameworks and deploy to production? Let's continue!** 
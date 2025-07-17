# ⚡ TuskLang Bash @cache Function Guide

**"We don't bow to any king" – Cache is your configuration's performance booster.**

The @cache function in TuskLang is your performance powerhouse, enabling intelligent caching, data persistence, and optimization directly within your configuration files. Whether you're caching expensive operations, storing computed results, or optimizing database queries, @cache provides the speed and efficiency to make your configurations lightning fast.

## 🎯 What is @cache?
The @cache function provides caching operations in TuskLang. It offers:
- **Data caching** - Cache expensive operations and computed results
- **Time-based expiration** - Set cache expiration times
- **Key-based storage** - Store and retrieve data by keys
- **Performance optimization** - Speed up repeated operations
- **Memory management** - Efficient cache storage and cleanup

## 📝 Basic @cache Syntax

### Simple Caching
```ini
[simple_caching]
# Cache a simple value
cached_value: @cache("5m", "expensive_calculation")
cached_string: @cache("1h", "Hello World")
cached_number: @cache("30m", 42)

# Cache with custom key
user_count: @cache("10m", "user_count", @sql("SELECT COUNT(*) FROM users"))
api_response: @cache("5m", "api_data", @http("GET", "https://api.example.com/data"))
```

### Time-Based Caching
```ini
[time_based_caching]
# Different cache durations
short_cache: @cache("30s", "short_data", "temporary_value")
medium_cache: @cache("5m", "medium_data", "medium_duration_value")
long_cache: @cache("1h", "long_data", "long_duration_value")
day_cache: @cache("24h", "daily_data", "daily_value")
week_cache: @cache("7d", "weekly_data", "weekly_value")
```

### Complex Data Caching
```ini
[complex_caching]
# Cache complex data structures
$user_data: {
    "id": 123,
    "name": "John Doe",
    "email": "john@example.com",
    "preferences": {
        "theme": "dark",
        "language": "en"
    }
}

cached_user: @cache("1h", "user_123", $user_data)

# Cache computed results
$expensive_calculation: @sql("""
    SELECT 
        u.name,
        COUNT(o.id) as order_count,
        SUM(o.amount) as total_spent
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    WHERE u.created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
    GROUP BY u.id, u.name
    ORDER BY total_spent DESC
""")

cached_analytics: @cache("15m", "user_analytics", $expensive_calculation)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > cache-quickstart.tsk << 'EOF'
[basic_caching]
# Cache simple values
cached_message: @cache("5m", "greeting", "Hello from TuskLang!")
cached_number: @cache("10m", "random_number", 42)

# Cache computed values
$expensive_operation: @math.sqrt(123456789)
cached_result: @cache("1h", "sqrt_result", $expensive_operation)

[data_caching]
# Cache database queries
$user_query: "SELECT COUNT(*) FROM users WHERE status = 'active'"
cached_user_count: @cache("5m", "active_users", @sql($user_query))

# Cache API responses
$api_url: "https://api.example.com/weather"
cached_weather: @cache("30m", "weather_data", @http("GET", $api_url))

[complex_caching]
# Cache complex data
$user_profile: {
    "id": 1,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "last_login": @date("Y-m-d H:i:s"),
    "preferences": {
        "theme": "dark",
        "notifications": true
    }
}

cached_profile: @cache("1h", "user_profile_1", $user_profile)

# Cache with dynamic keys
$user_id: 123
$cache_key: @string.concat("user_", $user_id, "_data")
cached_user_data: @cache("30m", $cache_key, "User data for ID: " + $user_id)

[cache_management]
# Check cache status
cache_info: {
    "greeting_exists": @cache.exists("greeting"),
    "sqrt_result_exists": @cache.exists("sqrt_result"),
    "weather_data_exists": @cache.exists("weather_data")
}

# Clear specific caches
clear_greeting: @cache.clear("greeting")
clear_all: @cache.clear_all()
EOF

config=$(tusk_parse cache-quickstart.tsk)

echo "=== Basic Caching ==="
echo "Cached Message: $(tusk_get "$config" basic_caching.cached_message)"
echo "Cached Number: $(tusk_get "$config" basic_caching.cached_number)"
echo "Cached Result: $(tusk_get "$config" basic_caching.cached_result)"

echo ""
echo "=== Data Caching ==="
echo "Cached User Count: $(tusk_get "$config" data_caching.cached_user_count)"
echo "Cached Weather: $(tusk_get "$config" data_caching.cached_weather)"

echo ""
echo "=== Complex Caching ==="
echo "Cached Profile: $(tusk_get "$config" complex_caching.cached_profile)"
echo "Cached User Data: $(tusk_get "$config" complex_caching.cached_user_data)"

echo ""
echo "=== Cache Management ==="
echo "Cache Info: $(tusk_get "$config" cache_management.cache_info)"
```

## 🔗 Real-World Use Cases

### 1. Database Query Caching
```ini
[database_caching]
# Cache expensive database queries
$query_cache: {
    "user_count": @cache("5m", "db_user_count", @sql("SELECT COUNT(*) FROM users")),
    "active_users": @cache("10m", "db_active_users", @sql("SELECT COUNT(*) FROM users WHERE status = 'active'")),
    "recent_orders": @cache("15m", "db_recent_orders", @sql("""
        SELECT COUNT(*), SUM(amount) 
        FROM orders 
        WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    """)),
    "top_products": @cache("30m", "db_top_products", @sql("""
        SELECT p.name, COUNT(oi.id) as sales_count
        FROM products p
        JOIN order_items oi ON p.id = oi.product_id
        GROUP BY p.id, p.name
        ORDER BY sales_count DESC
        LIMIT 10
    """))
}

# Cache user-specific data
$user_cache: {
    "profile": @cache("1h", @string.concat("user_profile_", $user_id), @sql("SELECT * FROM users WHERE id = ?", $user_id)),
    "orders": @cache("30m", @string.concat("user_orders_", $user_id), @sql("SELECT * FROM orders WHERE user_id = ? ORDER BY created_at DESC", $user_id)),
    "preferences": @cache("2h", @string.concat("user_prefs_", $user_id), @sql("SELECT * FROM user_preferences WHERE user_id = ?", $user_id))
}

# Cache analytics data
$analytics_cache: {
    "daily_revenue": @cache("1h", "analytics_daily_revenue", @sql("""
        SELECT DATE(created_at) as date, SUM(amount) as revenue
        FROM orders
        WHERE status = 'completed'
        AND created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
        GROUP BY DATE(created_at)
        ORDER BY date DESC
    """)),
    "customer_segments": @cache("2h", "analytics_customer_segments", @sql("""
        SELECT 
            CASE 
                WHEN total_spent >= 1000 THEN 'VIP'
                WHEN total_spent >= 500 THEN 'Premium'
                WHEN total_spent >= 100 THEN 'Regular'
                ELSE 'New'
            END as segment,
            COUNT(*) as customer_count
        FROM (
            SELECT u.id, SUM(o.amount) as total_spent
            FROM users u
            JOIN orders o ON u.id = o.user_id
            WHERE o.status = 'completed'
            GROUP BY u.id
        ) as customer_totals
        GROUP BY segment
    """))
}
```

### 2. API Response Caching
```ini
[api_caching]
# Cache external API responses
$api_cache: {
    "weather_data": @cache("30m", "weather_api", @http("GET", "https://api.weatherapi.com/v1/current.json?key=" + @env("WEATHER_API_KEY") + "&q=London")),
    "currency_rates": @cache("1h", "currency_api", @http("GET", "https://api.exchangerate-api.com/v4/latest/USD")),
    "news_feed": @cache("15m", "news_api", @http("GET", "https://newsapi.org/v2/top-headlines?country=us&apiKey=" + @env("NEWS_API_KEY"))),
    "stock_prices": @cache("5m", "stock_api", @http("GET", "https://api.stockdata.org/v1/data/quote?symbols=AAPL,GOOGL,MSFT&api_token=" + @env("STOCK_API_KEY")))
}

# Cache with conditional logic
$smart_api_cache: {
    "user_geolocation": @cache("1h", @string.concat("geo_", $user_ip), @http("GET", "https://ipapi.co/" + $user_ip + "/json/")),
    "product_recommendations": @cache("2h", @string.concat("recommendations_", $user_id), @http("POST", "https://api.recommendations.com/predict", {
        "user_id": $user_id,
        "preferences": $user_preferences
    })),
    "search_results": @cache("10m", @string.concat("search_", @string.hash($search_query)), @http("GET", "https://api.search.com?q=" + @url.encode($search_query)))
}

# Cache API rate limiting
$rate_limited_cache: {
    "github_user": @cache("1h", @string.concat("github_", $username), @http("GET", "https://api.github.com/users/" + $username, {
        "Authorization": "token " + @env("GITHUB_TOKEN")
    })),
    "twitter_feed": @cache("15m", @string.concat("twitter_", $user_id), @http("GET", "https://api.twitter.com/2/users/" + $user_id + "/tweets", {
        "Authorization": "Bearer " + @env("TWITTER_BEARER_TOKEN")
    }))
}
```

### 3. Configuration Caching
```ini
[config_caching]
# Cache configuration data
$config_cache: {
    "app_settings": @cache("1h", "app_config", @file.read("config/app.json")),
    "feature_flags": @cache("30m", "feature_flags", @file.read("config/features.json")),
    "environment_vars": @cache("5m", "env_vars", {
        "database_url": @env("DATABASE_URL"),
        "api_keys": {
            "weather": @env("WEATHER_API_KEY"),
            "news": @env("NEWS_API_KEY"),
            "stocks": @env("STOCK_API_KEY")
        },
        "app_config": {
            "debug": @env("DEBUG", "false"),
            "log_level": @env("LOG_LEVEL", "info")
        }
    })
}

# Cache computed configurations
$computed_config: {
    "database_pool": @cache("1h", "db_pool_config", {
        "max_connections": @math.min(100, @math.floor(@system.cpu_count * 10)),
        "timeout": @math.max(30, @system.memory_gb * 2),
        "retry_attempts": 3
    }),
    "cache_settings": @cache("2h", "cache_config", {
        "memory_limit": @math.floor(@system.memory_gb * 0.1) + "GB",
        "ttl_default": "1h",
        "cleanup_interval": "30m"
    }),
    "performance_tuning": @cache("1h", "perf_config", {
        "worker_processes": @system.cpu_count,
        "max_requests": @math.floor(@system.memory_gb * 1000),
        "buffer_size": @math.floor(@system.memory_gb * 1024 * 1024)
    })
}
```

### 4. Session and User Data Caching
```ini
[session_caching]
# Cache user sessions and data
$session_cache: {
    "user_session": @cache("2h", @string.concat("session_", $session_id), {
        "user_id": $user_id,
        "username": $username,
        "permissions": $user_permissions,
        "last_activity": @date("Y-m-d H:i:s"),
        "ip_address": $client_ip
    }),
    "user_preferences": @cache("1h", @string.concat("prefs_", $user_id), {
        "theme": $user_theme,
        "language": $user_language,
        "notifications": $notification_settings,
        "timezone": $user_timezone
    }),
    "user_permissions": @cache("30m", @string.concat("perms_", $user_id), @sql("""
        SELECT p.name, p.description
        FROM user_roles ur
        JOIN role_permissions rp ON ur.role_id = rp.role_id
        JOIN permissions p ON rp.permission_id = p.id
        WHERE ur.user_id = ?
    """, $user_id))
}

# Cache shopping cart and checkout data
$shopping_cache: {
    "cart_items": @cache("1h", @string.concat("cart_", $user_id), @sql("""
        SELECT ci.*, p.name, p.price, p.image_url
        FROM cart_items ci
        JOIN products p ON ci.product_id = p.id
        WHERE ci.user_id = ?
    """, $user_id)),
    "checkout_data": @cache("30m", @string.concat("checkout_", $user_id), {
        "shipping_address": $shipping_address,
        "billing_address": $billing_address,
        "payment_method": $payment_method,
        "discount_code": $discount_code
    })
}
```

## 🧠 Advanced @cache Patterns

### Conditional Caching
```ini
[conditional_caching]
# Cache based on conditions
$smart_caching: {
    "expensive_operation": @if(@cache.exists("expensive_result"), 
        @cache.get("expensive_result"), 
        @cache("1h", "expensive_result", @expensive_calculation)
    ),
    "conditional_data": @if($user_is_premium,
        @cache("2h", "premium_data", @get_premium_data()),
        @cache("30m", "basic_data", @get_basic_data())
    ),
    "environment_specific": @if(@env("ENVIRONMENT") == "production",
        @cache("1h", "prod_data", @get_production_data()),
        @cache("5m", "dev_data", @get_development_data())
    )
}

# Cache with fallback
$fallback_caching: {
    "api_data": @cache.get_or_set("api_response", "5m", @http("GET", $api_url)),
    "database_result": @cache.get_or_set("db_query", "10m", @sql($query)),
    "file_content": @cache.get_or_set("file_data", "1h", @file.read($file_path))
}
```

### Cache Invalidation
```ini
[cache_invalidation]
# Smart cache invalidation
$cache_management: {
    "clear_user_cache": @cache.clear(@string.concat("user_", $user_id, "_*")),
    "clear_expired": @cache.clear_expired(),
    "clear_pattern": @cache.clear_pattern("temp_*"),
    "clear_all_user": @cache.clear_pattern("user_*"),
    "clear_api_cache": @cache.clear_pattern("api_*")
}

# Cache warming
$cache_warming: {
    "warm_user_data": @cache("1h", @string.concat("user_", $user_id), @get_user_data($user_id)),
    "warm_analytics": @cache("2h", "analytics_data", @generate_analytics()),
    "warm_config": @cache("1h", "app_config", @load_app_config())
}
```

### Performance Optimization
```ini
[performance_optimization]
# Optimize cache performance
$optimized_caching: {
    "frequently_accessed": @cache("5m", "frequent_data", $data, {
        "priority": "high",
        "compression": true,
        "persistent": true
    }),
    "large_data": @cache("1h", "large_dataset", $large_data, {
        "compression": true,
        "chunked": true,
        "max_size": "100MB"
    }),
    "critical_data": @cache("30m", "critical_info", $critical_data, {
        "persistent": true,
        "backup": true,
        "replication": true
    })
}

# Cache with metadata
$metadata_caching: {
    "data_with_meta": @cache("1h", "data_key", $data, {
        "created_at": @date("Y-m-d H:i:s"),
        "source": "database",
        "version": "1.0",
        "tags": ["user_data", "analytics"]
    })
}
```

## 🛡️ Security & Performance Notes
- **Cache security:** Validate cached data and prevent cache poisoning
- **Memory management:** Monitor cache memory usage and implement cleanup
- **Cache invalidation:** Implement proper cache invalidation strategies
- **Data consistency:** Ensure cached data remains consistent with source
- **Performance monitoring:** Track cache hit rates and performance metrics
- **Storage limits:** Set appropriate cache size limits and cleanup policies

## 🐞 Troubleshooting
- **Cache misses:** Check cache keys and expiration times
- **Memory issues:** Monitor cache size and implement cleanup
- **Stale data:** Implement proper cache invalidation
- **Performance problems:** Optimize cache keys and data structures
- **Storage issues:** Check cache storage configuration and permissions

## 💡 Best Practices
- **Use meaningful keys:** Create descriptive and unique cache keys
- **Set appropriate TTL:** Choose cache expiration times based on data volatility
- **Implement cache warming:** Pre-populate cache with frequently accessed data
- **Monitor cache performance:** Track hit rates and response times
- **Use cache patterns:** Implement cache-aside, write-through, or write-behind patterns
- **Handle cache failures:** Implement fallback mechanisms for cache failures

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@sql Function](052-at-sql-function-bash.md)
- [@http Function](025-at-http-function-bash.md)
- [Performance Optimization](095-performance-optimization-bash.md)
- [Memory Management](096-memory-management-bash.md)

---

**Master @cache in TuskLang and wield the power of performance optimization in your configurations. ⚡** 
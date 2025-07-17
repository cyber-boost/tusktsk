# ⚡ TuskLang Bash @cache Function Guide

**"We don't bow to any king" – Caching is your configuration's memory.**

The @cache function in TuskLang is your performance optimization powerhouse, enabling intelligent caching of expensive operations, database queries, and computed values directly within your configuration files. Whether you're optimizing database access, reducing API calls, or improving response times, @cache provides the speed and efficiency to make your configurations lightning fast.

## 🎯 What is @cache?
The @cache function provides intelligent caching capabilities in TuskLang. It provides:
- **Time-based caching** - Cache values for specified durations
- **Key-based caching** - Cache with custom cache keys
- **Automatic invalidation** - Smart cache expiration and cleanup
- **Performance optimization** - Reduce expensive operations
- **Memory management** - Efficient cache storage and cleanup

## 📝 Basic @cache Syntax

### Simple Caching
```ini
[simple_caching]
# Cache expensive operations
expensive_calculation: @cache("5m", @math(complex_formula))
database_query: @cache("10m", @query("SELECT COUNT(*) FROM large_table"))
api_response: @cache("1h", @http("GET", "https://api.example.com/data"))
```

### Key-Based Caching
```ini
[key_caching]
# Cache with custom keys
user_count: @cache("user_count_5m", "5m", @query("SELECT COUNT(*) FROM users"))
server_status: @cache("server_status_1m", "1m", @shell("curl -s https://api.example.com/health"))
config_hash: @cache("config_hash_1h", "1h", @shell("sha256sum /etc/app/config.tsk"))
```

### Conditional Caching
```ini
[conditional_caching]
# Cache based on conditions
$environment: @env("APP_ENV", "development")
cached_query: @if($environment == "production", 
    @cache("30m", @query("SELECT * FROM production_data")),
    @query("SELECT * FROM development_data")
)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > cache-quickstart.tsk << 'EOF'
[basic_caching]
# Basic caching examples
system_info: @cache("5m", @shell("uname -a"))
disk_usage: @cache("2m", @shell("df -h /"))
memory_usage: @cache("1m", @shell("free -h"))
current_time: @cache("30s", @date("Y-m-d H:i:s"))

[expensive_operations]
# Cache expensive calculations
complex_math: @cache("10m", @math(2^20 + 3^15 + 5^10))
database_stats: @cache("5m", @query("SELECT COUNT(*) as total_users, AVG(age) as avg_age FROM users"))
file_analysis: @cache("1h", @shell("find /var/log -name '*.log' | wc -l"))

[api_caching]
# Cache API responses
weather_data: @cache("15m", @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London"))
currency_rates: @cache("1h", @http("GET", "https://api.exchangerate-api.com/v4/latest/USD"))
github_repos: @cache("30m", @http("GET", "https://api.github.com/users/octocat/repos"))

[conditional_caching]
# Environment-based caching
environment: @env("APP_ENV", "development")
cached_config: @if($environment == "production",
    @cache("1h", @shell("cat /etc/production/config.tsk")),
    @shell("cat /etc/development/config.tsk")
)
EOF

config=$(tusk_parse cache-quickstart.tsk)

echo "=== Basic Caching ==="
echo "System Info: $(tusk_get "$config" basic_caching.system_info)"
echo "Disk Usage: $(tusk_get "$config" basic_caching.disk_usage)"
echo "Memory Usage: $(tusk_get "$config" basic_caching.memory_usage)"
echo "Current Time: $(tusk_get "$config" basic_caching.current_time)"

echo ""
echo "=== Expensive Operations ==="
echo "Complex Math: $(tusk_get "$config" expensive_operations.complex_math)"
echo "Database Stats: $(tusk_get "$config" expensive_operations.database_stats)"
echo "File Analysis: $(tusk_get "$config" expensive_operations.file_analysis)"

echo ""
echo "=== API Caching ==="
echo "Weather Data: $(tusk_get "$config" api_caching.weather_data)"
echo "Currency Rates: $(tusk_get "$config" api_caching.currency_rates)"
echo "GitHub Repos: $(tusk_get "$config" api_caching.github_repos)"

echo ""
echo "=== Conditional Caching ==="
echo "Environment: $(tusk_get "$config" conditional_caching.environment)"
echo "Cached Config: $(tusk_get "$config" conditional_caching.cached_config)"
```

## 🔗 Real-World Use Cases

### 1. Database Query Optimization
```ini
[database_optimization]
# Cache expensive database queries
user_statistics: @cache("5m", @query("""
    SELECT 
        COUNT(*) as total_users,
        COUNT(CASE WHEN last_login >= DATE_SUB(NOW(), INTERVAL 7 DAY) THEN 1 END) as active_users,
        AVG(age) as average_age,
        COUNT(CASE WHEN subscription_type = 'premium' THEN 1 END) as premium_users
    FROM users 
    WHERE active = 1
"""))

order_analytics: @cache("10m", @query("""
    SELECT 
        DATE(created_at) as order_date,
        COUNT(*) as total_orders,
        SUM(amount) as total_revenue,
        AVG(amount) as average_order
    FROM orders 
    WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
    GROUP BY DATE(created_at)
    ORDER BY order_date DESC
"""))

system_metrics: @cache("2m", @query("""
    SELECT 
        'database_connections' as metric,
        COUNT(*) as value
    FROM information_schema.processlist
    UNION ALL
    SELECT 
        'slow_queries' as metric,
        COUNT(*) as value
    FROM mysql.slow_log
    WHERE start_time >= DATE_SUB(NOW(), INTERVAL 1 HOUR)
"""))
```

### 2. API Response Caching
```ini
[api_caching]
# Cache external API responses
$api_key: @env("WEATHER_API_KEY")
$city: @env("DEFAULT_CITY", "London")

weather_forecast: @cache("30m", @http("GET", @string.format("https://api.weatherapi.com/v1/forecast.json?key={key}&q={city}&days=7", {
    "key": $api_key,
    "city": $city
})))

currency_exchange: @cache("1h", @http("GET", "https://api.exchangerate-api.com/v4/latest/USD"))

github_user_info: @cache("15m", @http("GET", @string.format("https://api.github.com/users/{username}", {
    "username": @env("GITHUB_USERNAME", "octocat")
})))

# Cache with custom headers
authenticated_api: @cache("5m", @http("GET", "https://api.example.com/data", {
    "headers": {
        "Authorization": @string.format("Bearer {token}", {"token": @env("API_TOKEN")}),
        "Content-Type": "application/json"
    }
}))
```

### 3. System Monitoring and Metrics
```ini
[system_monitoring]
# Cache system metrics
cpu_usage: @cache("30s", @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1"))
memory_usage: @cache("30s", @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'"))
disk_usage: @cache("2m", @shell("df -h / | awk 'NR==2{print $5}' | cut -d'%' -f1"))
load_average: @cache("1m", @shell("uptime | awk -F'load average:' '{print $2}'"))

# Network metrics
network_connections: @cache("1m", @shell("netstat -an | wc -l"))
active_ports: @cache("5m", @shell("netstat -tlnp | grep LISTEN | wc -l"))
bandwidth_usage: @cache("30s", @shell("cat /proc/net/dev | grep eth0 | awk '{print $2, $10}'"))

# Application metrics
process_count: @cache("30s", @shell("ps aux | wc -l"))
log_file_size: @cache("5m", @shell("du -sh /var/log/app.log | cut -f1"))
error_count: @cache("1m", @shell("grep -c 'ERROR' /var/log/app.log"))
```

### 4. Configuration and File Caching
```ini
[configuration_caching]
# Cache configuration files
main_config_hash: @cache("1h", @shell("sha256sum /etc/app/config.tsk"))
env_config_hash: @cache("30m", @shell("sha256sum /etc/app/" + @env("APP_ENV") + ".tsk"))

# Cache file contents
nginx_config: @cache("5m", @shell("cat /etc/nginx/nginx.conf"))
ssl_cert_info: @cache("1h", @shell("openssl x509 -in /etc/ssl/certs/app.crt -text -noout"))
log_rotation_config: @cache("10m", @shell("cat /etc/logrotate.d/app"))

# Cache directory listings
log_files: @cache("2m", @shell("ls -la /var/log/app/"))
config_files: @cache("5m", @shell("find /etc/app -name '*.tsk' -type f"))
backup_files: @cache("10m", @shell("ls -la /var/backups/"))
```

## 🧠 Advanced @cache Patterns

### Intelligent Cache Invalidation
```bash
#!/bin/bash
source tusk-bash.sh

cat > intelligent-cache.tsk << 'EOF'
[intelligent_caching]
# Cache with intelligent invalidation
$cache_version: @env("CACHE_VERSION", "1.0")
$data_version: @shell("cat /var/cache/data_version.txt 2>/dev/null || echo '1.0'")

# Version-based cache invalidation
user_data: @cache("user_data_v" + $cache_version + "_1h", "1h", @query("SELECT * FROM users"))

# File modification-based caching
config_file: @cache("config_" + @shell("stat -c %Y /etc/app/config.tsk") + "_5m", "5m", @shell("cat /etc/app/config.tsk"))

# Time-based cache with fallback
current_time: @cache("time_30s", "30s", @date("Y-m-d H:i:s"))
cached_time: @if(@cache.exists("time_30s"), @cache.get("time_30s"), @date("Y-m-d H:i:s"))

# Multi-level caching
raw_data: @cache("raw_data_10m", "10m", @query("SELECT * FROM raw_data"))
processed_data: @cache("processed_data_5m", "5m", @array.map(@cache.get("raw_data_10m"), "process_item(item)"))
EOF

config=$(tusk_parse intelligent-cache.tsk)
echo "User Data: $(tusk_get "$config" intelligent_caching.user_data)"
echo "Config File: $(tusk_get "$config" intelligent_caching.config_file)"
echo "Current Time: $(tusk_get "$config" intelligent_caching.current_time)"
echo "Processed Data: $(tusk_get "$config" intelligent_caching.processed_data)"
```

### Cache Performance Optimization
```ini
[performance_optimization]
# Optimize cache performance
$cache_strategy: @env("CACHE_STRATEGY", "aggressive")

# Aggressive caching for production
production_cache: @if($cache_strategy == "aggressive", {
    "user_data": @cache("1h", @query("SELECT * FROM users")),
    "analytics": @cache("30m", @query("SELECT * FROM analytics")),
    "config": @cache("5m", @shell("cat /etc/app/config.tsk"))
}, {
    "user_data": @cache("5m", @query("SELECT * FROM users")),
    "analytics": @cache("10m", @query("SELECT * FROM analytics")),
    "config": @cache("1m", @shell("cat /etc/app/config.tsk"))
})

# Cache warming
warm_cache: @array.map(@array(["users", "analytics", "config"]), {
    "key": "warm_" + item,
    "data": @cache("warm_" + item + "_1h", "1h", @query("SELECT * FROM " + item))
})

# Cache statistics
cache_stats: @cache("cache_stats_5m", "5m", {
    "total_caches": @cache.count(),
    "memory_usage": @cache.memory_usage(),
    "hit_rate": @cache.hit_rate(),
    "miss_rate": @cache.miss_rate()
})
```

### Distributed Caching
```ini
[distributed_caching]
# Distributed cache configuration
$cache_backend: @env("CACHE_BACKEND", "redis")
$redis_host: @env("REDIS_HOST", "localhost")
$redis_port: @env("REDIS_PORT", "6379")

# Redis-based caching
redis_cache: @if($cache_backend == "redis", {
    "user_data": @cache.redis("user_data_1h", "1h", @query("SELECT * FROM users"), {
        "host": $redis_host,
        "port": $redis_port,
        "db": 0
    }),
    "session_data": @cache.redis("session_data_30m", "30m", @query("SELECT * FROM sessions"), {
        "host": $redis_host,
        "port": $redis_port,
        "db": 1
    })
}, {
    "user_data": @cache("user_data_1h", "1h", @query("SELECT * FROM users")),
    "session_data": @cache("session_data_30m", "30m", @query("SELECT * FROM sessions"))
})

# Cache synchronization
sync_cache: @cache.sync("shared_data_5m", "5m", @query("SELECT * FROM shared_data"), {
    "nodes": @array(["node1", "node2", "node3"]),
    "strategy": "write_through"
})
```

## 🛡️ Security & Performance Notes
- **Cache security:** Be careful with sensitive data in cache - use encryption for sensitive values
- **Memory management:** Monitor cache memory usage to prevent memory exhaustion
- **Cache invalidation:** Implement proper cache invalidation strategies
- **Performance monitoring:** Track cache hit rates and performance metrics
- **Cache poisoning:** Validate cached data to prevent cache poisoning attacks

## 🐞 Troubleshooting
- **Cache misses:** Check cache keys and expiration times
- **Memory issues:** Monitor cache memory usage and implement cleanup
- **Stale data:** Implement proper cache invalidation strategies
- **Performance problems:** Optimize cache keys and expiration times
- **Cache corruption:** Implement cache validation and recovery mechanisms

## 💡 Best Practices
- **Choose appropriate TTL:** Set cache expiration times based on data volatility
- **Use meaningful keys:** Create descriptive cache keys for easy debugging
- **Monitor cache performance:** Track hit rates and memory usage
- **Implement cache warming:** Pre-populate cache with frequently accessed data
- **Handle cache failures:** Implement fallback mechanisms when cache is unavailable
- **Validate cached data:** Ensure cached data is still valid and accurate

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [Performance Optimization](095-performance-optimization-bash.md)
- [Database Integration](076-database-integration-bash.md)
- [HTTP Operations](040-at-http-function-bash.md)

---

**Master @cache in TuskLang and accelerate your configurations with intelligent caching. ⚡** 
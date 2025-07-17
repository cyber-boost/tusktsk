# 🗄️ TuskLang Bash @query Function Guide

**"We don't bow to any king" – Database queries in configuration, the revolutionary way.**

The @query function in TuskLang is the game-changing feature that brings database power directly into your configuration files. Whether you're building dynamic dashboards, creating data-driven applications, or implementing real-time configuration, @query transforms your static configs into living, breathing systems that respond to your data.

## 🎯 What is @query?
The @query function executes database queries and returns results directly in your configuration. It provides:
- **Real-time data** - Live database information in your configs
- **Dynamic configuration** - Configs that adapt to your data
- **Database integration** - Direct access to SQL databases
- **Performance** - Optimized query execution and caching
- **Security** - Parameterized queries and connection management

## 📝 Basic @query Syntax

### Simple Queries
```ini
[basic]
# Count records
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
total_orders: @query("SELECT COUNT(*) FROM orders")
```

### Single Value Queries
```ini
[single_values]
# Get single values
latest_user: @query("SELECT username FROM users ORDER BY created_at DESC LIMIT 1")
system_status: @query("SELECT status FROM system_health WHERE id = 1")
config_value: @query("SELECT value FROM settings WHERE key = 'debug_mode'")
```

### Complex Queries
```ini
[complex]
# Aggregations and calculations
avg_order_value: @query("SELECT AVG(total) FROM orders WHERE status = 'completed'")
total_revenue: @query("SELECT SUM(total) FROM orders WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)")
user_growth: @query("SELECT COUNT(*) FROM users WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

# Set up database connection (example with PostgreSQL)
export DB_HOST="localhost"
export DB_PORT="5432"
export DB_NAME="tuskapp"
export DB_USER="postgres"
export DB_PASSWORD="password"

cat > query-quickstart.tsk << 'EOF'
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "tuskapp")
user: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD", "")

[user_statistics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
new_users_today: @query("SELECT COUNT(*) FROM users WHERE DATE(created_at) = CURDATE()")
premium_users: @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")

[system_metrics]
total_orders: @query("SELECT COUNT(*) FROM orders")
pending_orders: @query("SELECT COUNT(*) FROM orders WHERE status = 'pending'")
completed_orders: @query("SELECT COUNT(*) FROM orders WHERE status = 'completed'")
total_revenue: @query("SELECT COALESCE(SUM(total), 0) FROM orders WHERE status = 'completed'")

[feature_flags]
debug_mode: @query("SELECT enabled FROM feature_flags WHERE name = 'debug_mode'")
new_ui_enabled: @query("SELECT enabled FROM feature_flags WHERE name = 'new_ui'")
maintenance_mode: @query("SELECT active FROM maintenance_mode WHERE id = 1")
EOF

config=$(tusk_parse query-quickstart.tsk)

echo "=== User Statistics ==="
echo "Total Users: $(tusk_get "$config" user_statistics.total_users)"
echo "Active Users: $(tusk_get "$config" user_statistics.active_users)"
echo "New Users Today: $(tusk_get "$config" user_statistics.new_users_today)"
echo "Premium Users: $(tusk_get "$config" user_statistics.premium_users)"

echo ""
echo "=== System Metrics ==="
echo "Total Orders: $(tusk_get "$config" system_metrics.total_orders)"
echo "Pending Orders: $(tusk_get "$config" system_metrics.pending_orders)"
echo "Completed Orders: $(tusk_get "$config" system_metrics.completed_orders)"
echo "Total Revenue: $(tusk_get "$config" system_metrics.total_revenue)"

echo ""
echo "=== Feature Flags ==="
echo "Debug Mode: $(tusk_get "$config" feature_flags.debug_mode)"
echo "New UI Enabled: $(tusk_get "$config" feature_flags.new_ui_enabled)"
echo "Maintenance Mode: $(tusk_get "$config" feature_flags.maintenance_mode)"
```

## 🔗 Real-World Use Cases

### 1. Dynamic Dashboard Configuration
```ini
[dashboard]
# Real-time dashboard metrics
total_customers: @query("SELECT COUNT(*) FROM customers")
active_subscriptions: @query("SELECT COUNT(*) FROM subscriptions WHERE status = 'active'")
monthly_revenue: @query("SELECT SUM(amount) FROM transactions WHERE MONTH(created_at) = MONTH(NOW())")
system_uptime: @query("SELECT TIMESTAMPDIFF(SECOND, last_restart, NOW()) FROM system_status WHERE id = 1")

# Performance metrics
avg_response_time: @query("SELECT AVG(response_time) FROM api_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")
error_rate: @query("SELECT (COUNT(*) * 100.0 / (SELECT COUNT(*) FROM api_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR))) FROM api_logs WHERE status_code >= 400 AND created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")

# User activity
concurrent_users: @query("SELECT COUNT(DISTINCT user_id) FROM sessions WHERE last_activity >= DATE_SUB(NOW(), INTERVAL 5 MINUTE)")
top_pages: @query("SELECT page_url, COUNT(*) as visits FROM page_views WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 DAY) GROUP BY page_url ORDER BY visits DESC LIMIT 5")
```

### 2. Feature Flag Management
```ini
[feature_flags]
# Dynamic feature flags from database
new_ui_enabled: @query("SELECT enabled FROM feature_flags WHERE name = 'new_ui' AND environment = @env('APP_ENV')")
beta_features: @query("SELECT enabled FROM feature_flags WHERE name = 'beta_features' AND user_group = @env('USER_GROUP')")
maintenance_mode: @query("SELECT active FROM maintenance_mode WHERE id = 1")

# A/B testing configuration
ab_test_group: @query("SELECT test_group FROM ab_tests WHERE user_id = @env('USER_ID') AND test_name = 'new_checkout'")
ab_test_percentage: @query("SELECT percentage FROM ab_test_config WHERE test_name = 'new_checkout'")

# Gradual rollouts
rollout_percentage: @query("SELECT rollout_percentage FROM feature_rollouts WHERE feature_name = 'new_api' AND environment = @env('APP_ENV')")
```

### 3. User-Specific Configuration
```ini
[user_config]
# User preferences and settings
user_preferences: @query("SELECT preferences FROM user_settings WHERE user_id = @env('USER_ID')")
subscription_level: @query("SELECT subscription_type FROM subscriptions WHERE user_id = @env('USER_ID') AND status = 'active'")
permissions: @query("SELECT permission_name FROM user_permissions WHERE user_id = @env('USER_ID')")

# Personalization
recommended_items: @query("SELECT item_id FROM recommendations WHERE user_id = @env('USER_ID') ORDER BY score DESC LIMIT 10")
recent_activity: @query("SELECT activity_type, created_at FROM user_activity WHERE user_id = @env('USER_ID') ORDER BY created_at DESC LIMIT 5")
```

### 4. System Health and Monitoring
```ini
[health_monitoring]
# Database health
db_connections: @query("SELECT COUNT(*) FROM information_schema.processlist")
slow_queries: @query("SELECT COUNT(*) FROM mysql.slow_log WHERE start_time >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")
table_sizes: @query("SELECT table_name, ROUND(((data_length + index_length) / 1024 / 1024), 2) AS 'Size (MB)' FROM information_schema.tables WHERE table_schema = DATABASE() ORDER BY (data_length + index_length) DESC")

# Application health
error_count: @query("SELECT COUNT(*) FROM error_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")
success_rate: @query("SELECT (COUNT(*) * 100.0 / (SELECT COUNT(*) FROM api_requests WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR))) FROM api_requests WHERE status_code < 400 AND created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")

# Resource usage
cache_hit_rate: @query("SELECT (cache_hits * 100.0 / (cache_hits + cache_misses)) FROM cache_stats WHERE id = 1")
queue_length: @query("SELECT COUNT(*) FROM job_queue WHERE status = 'pending'")
```

## 🧠 Advanced @query Patterns

### Parameterized Queries
```bash
#!/bin/bash
source tusk-bash.sh

cat > parameterized-queries.tsk << 'EOF'
[parameterized]
# Use environment variables in queries
user_id: @env("USER_ID", "1")
environment: @env("APP_ENV", "development")

# Parameterized user queries
user_info: @query("SELECT username, email, created_at FROM users WHERE id = $user_id")
user_orders: @query("SELECT COUNT(*) FROM orders WHERE user_id = $user_id AND status = 'completed'")
user_preferences: @query("SELECT setting_value FROM user_settings WHERE user_id = $user_id AND setting_key = 'theme'")

# Environment-specific queries
feature_flags: @query("SELECT name, enabled FROM feature_flags WHERE environment = '$environment'")
system_config: @query("SELECT config_value FROM system_config WHERE environment = '$environment' AND config_key = 'debug_mode'")
EOF

config=$(tusk_parse parameterized-queries.tsk)
echo "User Info: $(tusk_get "$config" parameterized.user_info)"
echo "User Orders: $(tusk_get "$config" parameterized.user_orders)"
echo "Feature Flags: $(tusk_get "$config" parameterized.feature_flags)"
```

### Conditional Queries
```ini
[conditional_queries]
# Conditional query execution based on environment
environment: @env("APP_ENV", "development")

# Different queries for different environments
user_count: @if($environment == "production", 
    @query("SELECT COUNT(*) FROM users WHERE active = 1"),
    @query("SELECT COUNT(*) FROM users")
)

# Conditional feature flags
debug_mode: @if($environment == "development",
    @query("SELECT enabled FROM feature_flags WHERE name = 'debug_mode' AND environment = 'development'"),
    false
)

# Environment-specific metrics
performance_metrics: @if($environment == "production",
    @query("SELECT AVG(response_time) FROM production_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)"),
    @query("SELECT AVG(response_time) FROM development_logs WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")
)
```

### Caching and Performance
```ini
[performance]
# Cache expensive queries
expensive_stats: @cache("5m", @query("SELECT COUNT(*), AVG(amount), SUM(amount) FROM large_transactions WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)"))

# Optimized queries with indexes
user_lookup: @query("SELECT username, email FROM users WHERE id = @env('USER_ID')")  # Assuming id is indexed

# Aggregated data
daily_stats: @cache("1h", @query("SELECT DATE(created_at) as date, COUNT(*) as count, SUM(amount) as total FROM orders WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY) GROUP BY DATE(created_at)"))

# Real-time vs cached data
live_user_count: @query("SELECT COUNT(*) FROM users WHERE last_activity >= DATE_SUB(NOW(), INTERVAL 5 MINUTE)")
cached_user_count: @cache("1m", @query("SELECT COUNT(*) FROM users"))
```

## 🛡️ Security & Performance Notes
- **SQL injection prevention:** Always use parameterized queries and validate inputs
- **Connection pooling:** Use connection pooling for better performance
- **Query optimization:** Ensure queries use proper indexes and are optimized
- **Caching:** Use @cache for expensive queries to improve performance
- **Error handling:** Always handle database connection and query errors gracefully

## 🐞 Troubleshooting
- **Connection errors:** Check database connection settings and credentials
- **Query timeouts:** Optimize slow queries or increase timeout settings
- **Permission issues:** Ensure database user has proper permissions
- **Data type mismatches:** Handle data type conversions properly
- **Memory usage:** Be careful with large result sets

## 💡 Best Practices
- **Use parameterized queries:** Always use parameterized queries to prevent SQL injection
- **Optimize queries:** Ensure queries are optimized and use proper indexes
- **Cache expensive queries:** Use @cache for queries that are slow or resource-intensive
- **Handle errors:** Implement proper error handling for database operations
- **Monitor performance:** Monitor query performance and optimize as needed
- **Use transactions:** Use transactions for operations that modify data

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@env Operator](025-at-env-function-bash.md)
- [Database Integration](004-database-integration-bash.md)
- [Security](105-security-bash.md)

---

**Master @query in TuskLang and bring your database to life in your configurations. 🗄️** 
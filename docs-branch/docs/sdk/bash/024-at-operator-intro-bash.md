# ⚡ TuskLang Bash @ Operator Introduction Guide

**"We don't bow to any king" – @ operators are your configuration's superpowers.**

The @ operator system in TuskLang is the revolutionary feature that transforms static configuration into dynamic, intelligent, and powerful systems. Whether you're accessing environment variables, executing shell commands, querying databases, or performing complex operations, @ operators give your TuskLang configurations the power to adapt, learn, and respond to real-world conditions.

## 🎯 What are @ Operators?
@ operators are special functions that extend TuskLang beyond static configuration. They provide:
- **Dynamic values** - Environment variables, system information
- **External integration** - Database queries, API calls, file operations
- **Intelligent behavior** - Machine learning, optimization, caching
- **Real-time processing** - Date/time operations, mathematical functions
- **Security features** - Encryption, validation, secure environment access

## 📝 Basic @ Operator Syntax

### Environment Variables
```ini
[basic]
# Access environment variables
api_key: @env("API_KEY")
database_url: @env("DATABASE_URL", "default_url")
debug_mode: @env("DEBUG", false)
```

### Shell Commands
```ini
[system]
# Execute shell commands
hostname: @shell("hostname")
current_time: @shell("date '+%Y-%m-%d %H:%M:%S'")
disk_usage: @shell("df -h / | tail -1 | awk '{print $5}'")
```

### Database Queries
```ini
[database]
# Query databases directly
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
last_login: @query("SELECT MAX(last_login) FROM users")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > at-operator-quickstart.tsk << 'EOF'
[application]
name: "TuskApp"
version: "2.1.0"
environment: @env("APP_ENV", "development")

[system_info]
hostname: @shell("hostname")
current_time: @date.now()
uptime: @shell("uptime -p")
memory_usage: @shell("free -m | awk 'NR==2{printf \"%.1f%%\", $3*100/$2}'")

[database]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")

[security]
api_key: @env.secure("API_KEY")
encrypted_data: @encrypt("sensitive_information", "AES-256-GCM")
EOF

config=$(tusk_parse at-operator-quickstart.tsk)

echo "Application: $(tusk_get "$config" application.name) v$(tusk_get "$config" application.version)"
echo "Environment: $(tusk_get "$config" application.environment)"
echo "Hostname: $(tusk_get "$config" system_info.hostname)"
echo "Current Time: $(tusk_get "$config" system_info.current_time)"
echo "Uptime: $(tusk_get "$config" system_info.uptime)"
echo "Memory Usage: $(tusk_get "$config" system_info.memory_usage)"
echo "User Count: $(tusk_get "$config" database.user_count)"
echo "Active Users: $(tusk_get "$config" database.active_users)"
```

## 🔗 Real-World Use Cases

### 1. Dynamic Configuration Management
```ini
[dynamic_config]
# Environment-based configuration
environment: @env("APP_ENV", "development")
debug_mode: @if($environment == "development", true, false)
log_level: @if($debug_mode, "debug", "info")

# System-adaptive settings
cpu_cores: @shell("nproc")
max_workers: @math($cpu_cores * 2)
memory_gb: @shell("free -g | awk 'NR==2{print $2}'")
max_memory_mb: @math($memory_gb * 1024 * 0.8)
```

### 2. Database-Driven Configuration
```ini
[database_config]
# Real-time database statistics
total_users: @query("SELECT COUNT(*) FROM users")
active_sessions: @query("SELECT COUNT(*) FROM sessions WHERE expires_at > NOW()")
system_status: @query("SELECT status FROM system_health WHERE id = 1")

# Dynamic feature flags
feature_new_ui: @query("SELECT enabled FROM feature_flags WHERE name = 'new_ui'")
feature_analytics: @query("SELECT enabled FROM feature_flags WHERE name = 'analytics'")
maintenance_mode: @query("SELECT active FROM maintenance_mode WHERE id = 1")
```

### 3. System Monitoring and Health Checks
```ini
[monitoring]
# System health metrics
cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage: @shell("free | awk 'NR==2{printf \"%.1f\", $3*100/$2}'")
disk_usage: @shell("df / | awk 'NR==2{print $5}' | sed 's/%//'")
load_average: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")

# Service status checks
database_status: @shell("pg_isready -h localhost -p 5432 >/dev/null 2>&1 && echo 'healthy' || echo 'unhealthy'")
redis_status: @shell("redis-cli ping >/dev/null 2>&1 && echo 'healthy' || echo 'unhealthy'")
nginx_status: @shell("systemctl is-active nginx >/dev/null 2>&1 && echo 'active' || echo 'inactive'")
```

### 4. Security and Authentication
```ini
[security]
# Secure environment access
encryption_key: @env.secure("ENCRYPTION_KEY")
api_secret: @env.secure("API_SECRET")
database_password: @env.secure("DB_PASSWORD")

# Encrypted configuration
sensitive_config: @encrypt("{\"internal_key\": \"secret_value\"}", "AES-256-GCM")
user_token: @encrypt(@env("USER_TOKEN"), "AES-256-GCM")

# Validation
@validate.required(["encryption_key", "api_secret", "database_password"])
```

## 🧠 Advanced @ Operator Patterns

### Chaining and Composition
```ini
[advanced]
# Chain multiple operations
base_url: @env("API_BASE_URL", "https://api.example.com")
api_version: @env("API_VERSION", "v1")
full_url: @string.concat($base_url, "/api/", $api_version)

# Conditional operations
environment: @env("APP_ENV", "development")
cache_ttl: @if($environment == "production", 3600, 300)
log_level: @if($environment == "production", "error", "debug")

# Mathematical operations
cpu_count: @shell("nproc")
optimal_workers: @math($cpu_count * 2 + 1)
memory_mb: @shell("free -m | awk 'NR==2{print $2}'")
max_memory_usage: @math($memory_mb * 0.8)
```

### Caching and Performance
```ini
[performance]
# Cache expensive operations
expensive_query: @cache("5m", @query("SELECT COUNT(*) FROM large_table"))
system_info: @cache("1m", @shell("uname -a"))
api_response: @cache("30s", @http("GET", "https://api.example.com/status"))

# Optimize with learning
optimal_setting: @learn("cache_size", @math($memory_mb * 0.1))
performance_metric: @metrics("response_time_ms", @shell("curl -w '%{time_total}' -o /dev/null -s https://api.example.com"))
```

### Error Handling and Fallbacks
```ini
[robust]
# Provide fallbacks for all operations
database_host: @env("DB_HOST", "localhost")
api_timeout: @env("API_TIMEOUT", "30")
file_path: @env("CONFIG_PATH", "/etc/app/config.tsk")

# Safe shell operations
safe_command: @shell("ls /var/log/*.log 2>/dev/null || echo 'No log files found'")
safe_query: @query("SELECT COUNT(*) FROM users") || 0
safe_file_read: @file.read("config.json") || "{}"
```

## 🛡️ Security & Performance Notes
- **Shell command security:** Always validate and sanitize inputs for @shell operations
- **Database security:** Use parameterized queries and proper authentication
- **Environment variables:** Use @env.secure for sensitive data
- **Caching:** Use @cache for expensive operations to improve performance
- **Validation:** Always validate @ operator outputs before using them

## 🐞 Troubleshooting
- **Permission errors:** Ensure proper permissions for shell commands and file operations
- **Connection failures:** Handle database and API connection errors gracefully
- **Timeout issues:** Set appropriate timeouts for external operations
- **Memory usage:** Be careful with large @shell outputs that could consume memory

## 💡 Best Practices
- **Use fallbacks:** Always provide default values for @env operations
- **Cache expensive operations:** Use @cache for operations that are slow or resource-intensive
- **Validate outputs:** Check @ operator results before using them in critical operations
- **Error handling:** Implement proper error handling for all @ operator usage
- **Security first:** Use secure variants (@env.secure, @encrypt) for sensitive data

## 🔗 Cross-References
- [@env Operator](025-at-env-function-bash.md)
- [@shell Operator](026-at-shell-function-bash.md)
- [@query Operator](027-at-query-function-bash.md)
- [@file Operator](056-file-function-bash.md)

---

**Master @ operators in TuskLang and unlock the full power of dynamic configuration. ⚡** 
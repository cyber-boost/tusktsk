# 🚀 TuskLang Bash Quick Start Guide

**"We don't bow to any king" - Get up and running in minutes**

Welcome to the fastest way to learn TuskLang for Bash! This guide will have you writing powerful, dynamic configurations in minutes, not hours. Say goodbye to static config files and hello to configuration with a heartbeat.

## ⚡ Your First TuskLang Script

### Hello World in 30 Seconds

```bash
#!/bin/bash
source tusk-bash.sh

# Create your first TuskLang configuration
cat > hello.tsk << 'EOF'
$greeting: "Hello, TuskLang!"
$user: @env("USER", "World")

[app]
name: "My First TuskApp"
version: "1.0.0"
message: "${greeting} Welcome, ${user}!"

[server]
host: "localhost"
port: @if(@env("DEBUG") == "true", 3000, 80)
EOF

# Parse and use the configuration
config=$(tusk_parse hello.tsk)

# Display results
echo "App: $(tusk_get "$config" app.name)"
echo "Message: $(tusk_get "$config" app.message)"
echo "Server: $(tusk_get "$config" server.host):$(tusk_get "$config" server.port)"
```

**Run it:**
```bash
chmod +x hello.sh
./hello.sh
```

## 🎯 Core Concepts in 5 Minutes

### 1. Variables and Interpolation

```bash
#!/bin/bash
source tusk-bash.sh

cat > variables.tsk << 'EOF'
# Global variables (use $ prefix)
$app_name: "TuskApp"
$version: "2.1.0"
$environment: @env("APP_ENV", "development")

# Sections (use [section] syntax)
[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
url: "http://${host}:${port}"

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
connection_string: "postgresql://${host}:${port}/${app_name}"
EOF

config=$(tusk_parse variables.tsk)

echo "Server URL: $(tusk_get "$config" server.url)"
echo "Database: $(tusk_get "$config" database.connection_string)"
```

### 2. Environment Integration

```bash
#!/bin/bash
source tusk-bash.sh

# Set environment variables
export APP_ENV="production"
export DB_HOST="db.example.com"
export DB_PORT="5432"

cat > env-integration.tsk << 'EOF'
[config]
environment: @env("APP_ENV", "development")
debug: @env("DEBUG", "false")
api_key: @env("API_KEY")

[validation]
@validate.required(["api_key"])
EOF

config=$(tusk_parse env-integration.tsk)
echo "Environment: $(tusk_get "$config" config.environment)"
echo "Debug: $(tusk_get "$config" config.debug)"
```

### 3. Conditional Logic

```bash
#!/bin/bash
source tusk-bash.sh

cat > conditions.tsk << 'EOF'
$environment: @env("APP_ENV", "development")

[logging]
level: @if($environment == "production", "error", "debug")
file: @if($environment == "production", "/var/log/app.log", "console")

[security]
ssl: @if($environment == "production", true, false)
cors: @if($environment == "production", {
    origin: ["https://myapp.com"],
    credentials: true
}, {
    origin: "*",
    credentials: false
})
EOF

config=$(tusk_parse conditions.tsk)
echo "Log Level: $(tusk_get "$config" logging.level)"
echo "SSL Enabled: $(tusk_get "$config" security.ssl)"
```

## 🔗 Cross-File Communication

### Linking Multiple Configurations

```bash
#!/bin/bash
source tusk-bash.sh

# Create base configuration
cat > base.tsk << 'EOF'
$app_name: "TuskApp"
$version: "2.1.0"

[database]
host: "localhost"
port: 5432
EOF

# Create environment-specific config
cat > production.tsk << 'EOF'
$environment: "production"

[database]
host: "db.production.com"
port: 5432
ssl: true

[server]
workers: 4
timeout: 30
EOF

# Create main configuration that references others
cat > main.tsk << 'EOF'
# Import base configuration
@import "base.tsk"

# Override with environment-specific settings
@import "${environment}.tsk"

[app]
full_name: "${app_name} v${version}"
environment: $environment
EOF

# Parse with environment context
export environment="production"
config=$(tusk_parse main.tsk)

echo "App: $(tusk_get "$config" app.full_name)"
echo "Database: $(tusk_get "$config" database.host)"
echo "Workers: $(tusk_get "$config" server.workers)"
```

## 🗃️ Database Integration

### Real-Time Database Queries

```bash
#!/bin/bash
source tusk-bash.sh

# Set up database connection
tusk_set_db_adapter "sqlite" "/tmp/test.db"

# Create test database
sqlite3 /tmp/test.db << 'EOF'
CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, email TEXT);
INSERT INTO users (name, email) VALUES ('Alice', 'alice@example.com');
INSERT INTO users (name, email) VALUES ('Bob', 'bob@example.com');
EOF

cat > db-config.tsk << 'EOF'
[users]
total_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE name IS NOT NULL")
user_list: @query("SELECT name, email FROM users")

[stats]
user_count: @query("SELECT COUNT(*) FROM users")
EOF

config=$(tusk_parse db-config.tsk)

echo "Total Users: $(tusk_get "$config" users.total_count)"
echo "Active Users: $(tusk_get "$config" users.active_users)"
echo "User List: $(tusk_get "$config" users.user_list)"
```

## ⚡ @ Operator System

### Powerful Dynamic Operations

```bash
#!/bin/bash
source tusk-bash.sh

cat > operators.tsk << 'EOF'
[system]
current_time: @date.now()
formatted_date: @date("Y-m-d H:i:s")
random_id: @uuid.generate()

[files]
config_content: @file.read("config.json")
file_exists: @file.exists("important.txt")

[cache]
expensive_data: @cache("5m", "expensive_operation")
user_profile: @cache("1h", "user_data", @env("USER_ID"))

[api]
weather: @http("GET", "https://api.weatherapi.com/v1/current.json?key=${api_key}&q=London")
EOF

# Set API key
export api_key="your_api_key_here"

config=$(tusk_parse operators.tsk)

echo "Current Time: $(tusk_get "$config" system.current_time)"
echo "Random ID: $(tusk_get "$config" system.random_id)"
echo "File Exists: $(tusk_get "$config" files.file_exists)"
```

## 🛠️ Shell Integration

### Seamless Bash Integration

```bash
#!/bin/bash
source tusk-bash.sh

cat > shell-integration.tsk << 'EOF'
[system]
cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")
disk_usage: @shell("df / | tail -1 | awk '{print $5}' | sed 's/%//'")
uptime: @shell("uptime | awk -F'up' '{print $2}' | awk '{print $1}'")

[processes]
running_processes: @shell("ps aux | wc -l")
docker_containers: @shell("docker ps --format 'table {{.Names}}\t{{.Status}}' 2>/dev/null || echo 'Docker not available'")
EOF

config=$(tusk_parse shell-integration.tsk)

echo "CPU Usage: $(tusk_get "$config" system.cpu_usage)%"
echo "Memory Usage: $(tusk_get "$config" system.memory_usage)%"
echo "Disk Usage: $(tusk_get "$config" system.disk_usage)%"
echo "Uptime: $(tusk_get "$config" system.uptime)"
echo "Running Processes: $(tusk_get "$config" processes.running_processes)"
```

## 🔒 Security Features

### Secure Configuration Management

```bash
#!/bin/bash
source tusk-bash.sh

cat > secure-config.tsk << 'EOF'
[secrets]
# Encrypt sensitive data
password: @encrypt("my-secret-password", "AES-256-GCM")
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")

[ssl]
private_key: @file.encrypt("private.key", "AES-256-GCM")
certificate: @file.encrypt("certificate.crt", "AES-256-GCM")

[validation]
# Validate required environment variables
@validate.required(["API_KEY", "DATABASE_URL"])
EOF

# Set required environment variables
export API_KEY="your-secret-api-key"
export DATABASE_URL="postgresql://user:pass@localhost/db"

config=$(tusk_parse secure-config.tsk)

# Decrypt when needed
password=$(tusk_decrypt "$(tusk_get "$config" secrets.password)")
echo "Decrypted password: $password"
```

## 🚀 Advanced Features

### Executable Functions (FUJSEN)

```bash
#!/bin/bash
source tusk-bash.sh

cat > fujsen-config.tsk << 'EOF'
[processing]
# JavaScript functions embedded in configuration
calculate_tax: """function calculate(amount) { 
    return amount * 0.1; 
}"""

format_currency: """function format(amount) { 
    return '$' + amount.toFixed(2); 
}"""

process_data: """function process(data) { 
    return data.map(item => item.toUpperCase()); 
}"""
EOF

config=$(tusk_parse fujsen-config.tsk)

# Execute functions
tax=$(tusk_execute_fujsen "$(tusk_get "$config" processing.calculate_tax)" 100)
formatted=$(tusk_execute_fujsen "$(tusk_get "$config" processing.format_currency)" 99.99)

echo "Tax on $100: $tax"
echo "Formatted: $formatted"
```

## 📊 Real-World Example: System Monitor

### Complete Monitoring Script

```bash
#!/bin/bash
source tusk-bash.sh

cat > monitor.tsk << 'EOF'
$app_name: "System Monitor"
$version: "1.0.0"

[monitoring]
interval: @env("MONITOR_INTERVAL", "30")
log_file: "/var/log/system-monitor.log"

[metrics]
cpu_threshold: @env("CPU_THRESHOLD", "80")
memory_threshold: @env("MEMORY_THRESHOLD", "85")
disk_threshold: @env("DISK_THRESHOLD", "90")

[alerts]
cpu_alert: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1") > $cpu_threshold
memory_alert: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'") > $memory_threshold
disk_alert: @shell("df / | tail -1 | awk '{print $5}' | sed 's/%//'") > $disk_threshold

[actions]
send_alert: """function send(message) {
    echo \"[$(date)] ALERT: $message\" >> $log_file;
    curl -X POST -H 'Content-Type: application/json' -d '{\"message\":\"'$message'\"}' https://hooks.slack.com/services/YOUR/WEBHOOK/URL;
}"""
EOF

# Monitoring loop
monitor_system() {
    config=$(tusk_parse monitor.tsk)
    
    # Check CPU
    if [[ $(tusk_get "$config" alerts.cpu_alert) == "true" ]]; then
        tusk_execute_fujsen "$(tusk_get "$config" actions.send_alert)" "High CPU usage detected!"
    fi
    
    # Check memory
    if [[ $(tusk_get "$config" alerts.memory_alert) == "true" ]]; then
        tusk_execute_fujsen "$(tusk_get "$config" actions.send_alert)" "High memory usage detected!"
    fi
    
    # Check disk
    if [[ $(tusk_get "$config" alerts.disk_alert) == "true" ]]; then
        tusk_execute_fujsen "$(tusk_get "$config" actions.send_alert)" "High disk usage detected!"
    fi
}

# Run monitoring
interval=$(tusk_get "$(tusk_parse monitor.tsk)" monitoring.interval)
echo "Starting system monitor (interval: ${interval}s)"

while true; do
    monitor_system
    sleep $interval
done
```

## 🎯 What You've Learned

In this quick start guide, you've mastered:

✅ **Basic TuskLang syntax** - Variables, sections, and interpolation  
✅ **Environment integration** - Dynamic configuration based on environment  
✅ **Conditional logic** - Smart configuration decisions  
✅ **Cross-file communication** - Modular configuration management  
✅ **Database integration** - Real-time data in configuration  
✅ **@ Operator system** - Powerful dynamic operations  
✅ **Shell integration** - Seamless Bash workflow integration  
✅ **Security features** - Encrypted and validated configuration  
✅ **Executable functions** - JavaScript in configuration  
✅ **Real-world applications** - Complete monitoring system  

## 🚀 Next Steps

Ready to dive deeper? Continue your TuskLang journey:

1. **Master Basic Syntax** → [003-basic-syntax-bash.md](003-basic-syntax-bash.md)
2. **Database Integration** → [004-database-integration-bash.md](004-database-integration-bash.md)
3. **Advanced Features** → [005-advanced-features-bash.md](005-advanced-features-bash.md)

## 💡 Pro Tips

- **Use `@env()` for all external configuration** - Never hardcode values
- **Leverage conditional logic** - One config file for all environments
- **Cache expensive operations** - Use `@cache()` for performance
- **Validate early** - Use `@validate.required()` for critical variables
- **Encrypt secrets** - Always use `@encrypt()` for sensitive data

---

**You're now ready to revolutionize your shell scripting with TuskLang! 🐚** 
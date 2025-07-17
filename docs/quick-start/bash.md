# 🐚 TuskLang Bash SDK - Tusk Me Hard

**"We don't bow to any king" - Bash Edition**

The TuskLang Bash SDK provides seamless integration with shell scripts, system administration, and DevOps workflows. Execute TuskLang configurations directly from the command line with full shell integration.

## 🚀 Quick Start

### Installation

```bash
# Download the TuskLang Bash SDK
curl -sSL https://bash.tusklang.org/install.sh | bash

# Or download manually
wget https://github.com/bgengs/tusklang/releases/latest/download/tusk-bash.sh
chmod +x tusk-bash.sh
sudo mv tusk-bash.sh /usr/local/bin/tusk

# Verify installation
tusk --version
```

### One-Line Install

```bash
# Direct install
curl -sSL https://bash.tusklang.org | bash

# Or with wget
wget -qO- https://bash.tusklang.org | bash
```

## 🎯 Core Features

### 1. Shell Integration with Full Environment Access
```bash
#!/bin/bash
source tusk-bash.sh

# Parse TuskLang configuration
config=$(tusk_parse config.tsk)

# Access values directly in shell
echo "Database host: $(tusk_get "$config" database.host)"
echo "Server port: $(tusk_get "$config" server.port)"

# Use in shell conditionals
if [[ $(tusk_get "$config" debug) == "true" ]]; then
    echo "Debug mode enabled"
fi
```

### 2. Database Integration with Multiple Adapters
```bash
#!/bin/bash
source tusk-bash.sh

# Configure database adapters
tusk_set_db_adapter "sqlite" "/path/to/database.db"
tusk_set_db_adapter "postgresql" "host=localhost port=5432 dbname=myapp user=postgres password=secret"

# TSK file with database queries
cat > config.tsk << 'EOF'
[database]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
EOF

# Parse and execute
config=$(tusk_parse config.tsk)
echo "Total users: $(tusk_get "$config" database.user_count)"
```

### 3. System Administration Features
```bash
#!/bin/bash
source tusk-bash.sh

# System monitoring configuration
cat > monitoring.tsk << 'EOF'
[system]
cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")
disk_usage: @shell("df / | tail -1 | awk '{print $5}' | sed 's/%//'")
load_average: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")

[alerts]
cpu_alert: cpu_usage > 80
memory_alert: memory_usage > 85
disk_alert: disk_usage > 90
EOF

# Monitor system in real-time
while true; do
    config=$(tusk_parse monitoring.tsk)
    
    if [[ $(tusk_get "$config" alerts.cpu_alert) == "true" ]]; then
        echo "WARNING: High CPU usage detected!"
        # Send alert
    fi
    
    sleep 30
done
```

### 4. @ Operator System
```bash
#!/bin/bash
source tusk-bash.sh

# Advanced @ operators
cat > advanced.tsk << 'EOF'
[api]
endpoint: @env("API_ENDPOINT", "https://api.example.com")
api_key: @env("API_KEY")

[cache]
data: @cache("5m", "expensive_operation")
user_data: @cache("1h", "user_profile", @request.user_id)

[processing]
timestamp: @date.now()
random_id: @uuid.generate()
file_content: @file.read("config.json")
EOF

# Execute with context
context='{"request": {"user_id": 123}, "cache_value": "cached_data"}'
result=$(tusk_execute_operators advanced.tsk "$context")
```

## 🔧 Advanced Usage

### 1. Cross-File Communication
```bash
#!/bin/bash
source tusk-bash.sh

# main.tsk
cat > main.tsk << 'EOF'
$app_name: "MyApp"
$version: "1.0.0"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
EOF

# config.tsk
cat > config.tsk << 'EOF'
db_host: "localhost"
db_port: 5432
db_name: "myapp"
EOF

# Link files and parse
tusk_link_file "config.tsk" "$(cat config.tsk)"
result=$(tusk_parse main.tsk)
echo "Database host: $(tusk_get "$result" database.host)"
```

### 2. Global Variables and Interpolation
```bash
#!/bin/bash
source tusk-bash.sh

cat > app.tsk << 'EOF'
$app_name: "MyApp"
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)
debug: @if($environment != "production", true, false)

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"
EOF

# Parse with environment
export APP_ENV="production"
result=$(tusk_parse app.tsk)
echo "Server port: $(tusk_get "$result" server.port)"
```

### 3. Conditional Logic
```bash
#!/bin/bash
source tusk-bash.sh

cat > deployment.tsk << 'EOF'
$environment: @env("DEPLOY_ENV", "development")

[logging]
level: @if($environment == "production", "error", "debug")
format: @if($environment == "production", "json", "text")
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

# Deploy based on environment
export DEPLOY_ENV="production"
config=$(tusk_parse deployment.tsk)

if [[ $(tusk_get "$config" security.ssl) == "true" ]]; then
    echo "Configuring SSL for production..."
    # SSL setup commands
fi
```

### 4. Array and Object Operations
```bash
#!/bin/bash
source tusk-bash.sh

cat > users.tsk << 'EOF'
[users]
admin_users: ["alice", "bob", "charlie"]
roles: {
    admin: ["read", "write", "delete"],
    user: ["read", "write"],
    guest: ["read"]
}

[permissions]
user_permissions: @array.merge(roles.admin, roles.user)
all_permissions: @array.unique(@array.merge(roles.admin, roles.user, roles.guest))
EOF

config=$(tusk_parse users.tsk)

# Access arrays and objects
admin_users=$(tusk_get "$config" users.admin_users)
echo "Admin users: $admin_users"

# Check if user is admin
user="alice"
if tusk_array_contains "$admin_users" "$user"; then
    echo "$user is an admin"
fi
```

## 🛠️ CLI Tools

### 1. Command Line Interface
```bash
# Parse and display configuration
tusk parse config.tsk

# Get specific value
tusk get config.tsk database.host

# Validate configuration
tusk validate config.tsk

# Execute with context
tusk exec config.tsk --context '{"user_id": 123}'

# Watch for changes
tusk watch config.tsk --callback "echo 'Config changed!'"

# Generate shell script
tusk generate-script config.tsk > deploy.sh
```

### 2. Shell Functions
```bash
#!/bin/bash
source tusk-bash.sh

# Load configuration into shell variables
tusk_load_config config.tsk

# Now use variables directly
echo "Database: $DATABASE_HOST:$DATABASE_PORT"
echo "Server: $SERVER_HOST:$SERVER_PORT"

# Export to environment
tusk_export_env config.tsk
echo "Exported variables: $API_KEY"
```

### 3. Integration with Other Tools
```bash
#!/bin/bash
source tusk-bash.sh

# Docker integration
cat > docker.tsk << 'EOF'
[container]
name: "myapp"
image: "myapp:latest"
ports: ["8080:8080"]
environment: {
    DATABASE_URL: @env("DATABASE_URL"),
    API_KEY: @env("API_KEY")
}
EOF

# Generate docker-compose.yml
tusk generate-docker-compose docker.tsk > docker-compose.yml

# Kubernetes integration
tusk generate-k8s deployment.tsk > deployment.yaml
```

## 🔒 Security Features

### 1. Secure Configuration Management
```bash
#!/bin/bash
source tusk-bash.sh

# Encrypt sensitive values
cat > secrets.tsk << 'EOF'
[database]
password: @encrypt("my-secret-password", "AES-256-GCM")
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")

[ssl]
private_key: @file.encrypt("private.key", "AES-256-GCM")
certificate: @file.encrypt("certificate.crt", "AES-256-GCM")
EOF

# Decrypt when needed
config=$(tusk_parse secrets.tsk)
db_password=$(tusk_decrypt "$(tusk_get "$config" database.password)")
```

### 2. Environment Variable Security
```bash
#!/bin/bash
source tusk-bash.sh

# Secure environment loading
cat > secure.tsk << 'EOF'
[secrets]
# Only load from secure environment
api_key: @env.secure("API_KEY")
database_password: @env.secure("DB_PASSWORD")

# Validate required variables
@validate.required(["api_key", "database_password"])
EOF

# Load from .env file securely
tusk_load_env .env
config=$(tusk_parse secure.tsk)
```

## ⚡ Performance Optimization

### 1. Caching and Lazy Loading
```bash
#!/bin/bash
source tusk-bash.sh

cat > optimized.tsk << 'EOF'
[expensive_operations]
user_stats: @cache("5m", @query("SELECT COUNT(*) FROM users"))
system_metrics: @cache("30s", @shell("top -bn1 | grep 'Cpu(s)'"))
api_data: @cache("1h", @http("GET", "https://api.example.com/data"))

[lazy_loading]
heavy_config: @lazy("config/heavy.tsk")
user_data: @lazy("users/" + @request.user_id + ".tsk")
EOF

# Parse with caching
config=$(tusk_parse optimized.tsk)

# Access cached values
echo "User count: $(tusk_get "$config" expensive_operations.user_stats)"
```

### 2. Parallel Processing
```bash
#!/bin/bash
source tusk-bash.sh

cat > parallel.tsk << 'EOF'
[parallel_tasks]
task1: @parallel(@shell("sleep 2 && echo 'Task 1 done'"))
task2: @parallel(@shell("sleep 3 && echo 'Task 2 done'"))
task3: @parallel(@shell("sleep 1 && echo 'Task 3 done'"))

[results]
all_tasks: @parallel.wait([task1, task2, task3])
EOF

# Execute parallel tasks
config=$(tusk_parse parallel.tsk)
results=$(tusk_get "$config" results.all_tasks)
echo "All tasks completed: $results"
```

## 🌐 Web Framework Integration

### 1. CGI Scripts
```bash
#!/bin/bash
source tusk-bash.sh

# CGI script with TuskLang
cat > webapp.cgi << 'EOF'
#!/bin/bash
source tusk-bash.sh

# Load configuration
config=$(tusk_parse config.tsk)

# Set content type
echo "Content-Type: text/html"
echo ""

# Generate HTML
cat << HTML
<!DOCTYPE html>
<html>
<head>
    <title>$(tusk_get "$config" app.name)</title>
</head>
<body>
    <h1>Welcome to $(tusk_get "$config" app.name)</h1>
    <p>Version: $(tusk_get "$config" app.version)</p>
    <p>Environment: $(tusk_get "$config" app.environment)</p>
</body>
</html>
HTML
EOF

chmod +x webapp.cgi
```

### 2. FastCGI Integration
```bash
#!/bin/bash
source tusk-bash.sh

# FastCGI script
cat > fastcgi.sh << 'EOF'
#!/bin/bash
source tusk-bash.sh

# Load configuration once
CONFIG=$(tusk_parse config.tsk)

# FastCGI loop
while true; do
    # Read request
    read -r method path protocol
    
    # Parse request
    case "$method" in
        GET)
            case "$path" in
                /api/users)
                    user_count=$(tusk_get "$CONFIG" database.user_count)
                    echo "Content-Type: application/json"
                    echo ""
                    echo "{\"users\": $user_count}"
                    ;;
                *)
                    echo "Status: 404 Not Found"
                    echo "Content-Type: text/plain"
                    echo ""
                    echo "Not Found"
                    ;;
            esac
            ;;
    esac
done
EOF
```

## 🧪 Testing

### 1. Unit Testing
```bash
#!/bin/bash
source tusk-bash.sh

# Test configuration
cat > test_config.tsk << 'EOF'
[test]
value: 42
string: "hello"
array: [1, 2, 3]
object: {key: "value"}
EOF

# Run tests
echo "Running TuskLang tests..."

# Test parsing
config=$(tusk_parse test_config.tsk)
if [[ $(tusk_get "$config" test.value) == "42" ]]; then
    echo "✓ Value test passed"
else
    echo "✗ Value test failed"
    exit 1
fi

# Test string
if [[ $(tusk_get "$config" test.string) == "hello" ]]; then
    echo "✓ String test passed"
else
    echo "✗ String test failed"
    exit 1
fi

echo "All tests passed!"
```

### 2. Integration Testing
```bash
#!/bin/bash
source tusk-bash.sh

# Integration test
cat > integration_test.sh << 'EOF'
#!/bin/bash
source tusk-bash.sh

# Test database integration
cat > db_test.tsk << 'TUSK'
[database]
test_query: @query("SELECT 1 as result")
TUSK

# Run test
config=$(tusk_parse db_test.tsk)
result=$(tusk_get "$config" database.test_query)

if [[ "$result" == "1" ]]; then
    echo "✓ Database integration test passed"
else
    echo "✗ Database integration test failed"
    exit 1
fi
EOF

chmod +x integration_test.sh
./integration_test.sh
```

## 📦 Migration from Other Config Formats

### 1. From JSON
```bash
#!/bin/bash
source tusk-bash.sh

# Convert JSON to TuskLang
cat > convert_json.sh << 'EOF'
#!/bin/bash

# JSON file
cat > config.json << 'JSON'
{
    "database": {
        "host": "localhost",
        "port": 5432
    },
    "server": {
        "host": "0.0.0.0",
        "port": 8080
    }
}
JSON

# Convert to TuskLang
tusk convert-json config.json > config.tsk

# Verify conversion
echo "Converted configuration:"
cat config.tsk
EOF
```

### 2. From YAML
```bash
#!/bin/bash
source tusk-bash.sh

# Convert YAML to TuskLang
cat > convert_yaml.sh << 'EOF'
#!/bin/bash

# YAML file
cat > config.yaml << 'YAML'
database:
  host: localhost
  port: 5432
server:
  host: 0.0.0.0
  port: 8080
YAML

# Convert to TuskLang
tusk convert-yaml config.yaml > config.tsk

# Verify conversion
echo "Converted configuration:"
cat config.tsk
EOF
```

## 🚀 Deployment Strategies

### 1. Docker Deployment
```bash
#!/bin/bash
source tusk-bash.sh

# Docker configuration
cat > docker.tsk << 'EOF'
[container]
name: "tusk-app"
image: "tusk-app:latest"
ports: ["8080:8080"]

[environment]
NODE_ENV: @env("NODE_ENV", "production")
DATABASE_URL: @env("DATABASE_URL")
API_KEY: @env("API_KEY")

[volumes]
config: "/app/config"
logs: "/app/logs"
EOF

# Generate Dockerfile
tusk generate-dockerfile docker.tsk > Dockerfile

# Build and run
docker build -t tusk-app .
docker run -d --name tusk-app -p 8080:8080 tusk-app
```

### 2. Kubernetes Deployment
```bash
#!/bin/bash
source tusk-bash.sh

# Kubernetes configuration
cat > k8s.tsk << 'EOF'
[deployment]
name: "tusk-app"
replicas: @env("REPLICAS", 3)
image: "tusk-app:latest"

[resources]
cpu: "500m"
memory: "512Mi"

[environment]
NODE_ENV: "production"
DATABASE_URL: @secret("database-url")
API_KEY: @secret("api-key")
EOF

# Generate Kubernetes manifests
tusk generate-k8s k8s.tsk > deployment.yaml
kubectl apply -f deployment.yaml
```

## 📊 Performance Benchmarks

### 1. Parsing Performance
```bash
#!/bin/bash
source tusk-bash.sh

# Benchmark parsing
echo "Benchmarking TuskLang parsing..."

# Create test file
cat > benchmark.tsk << 'EOF'
[test]
value1: 1
value2: 2
value3: 3
# ... 1000 more values
EOF

# Run benchmark
start_time=$(date +%s%N)
for i in {1..1000}; do
    config=$(tusk_parse benchmark.tsk)
done
end_time=$(date +%s%N)

duration=$((end_time - start_time))
echo "Parsed 1000 files in ${duration}ns"
echo "Average: $((duration / 1000))ns per file"
```

## 🔧 Troubleshooting

### 1. Common Issues
```bash
#!/bin/bash
source tusk-bash.sh

# Debug mode
export TUSK_DEBUG=1
config=$(tusk_parse config.tsk)

# Verbose parsing
tusk parse config.tsk --verbose

# Validate syntax
tusk validate config.tsk

# Check dependencies
tusk check-deps
```

### 2. Error Handling
```bash
#!/bin/bash
source tusk-bash.sh

# Safe parsing with error handling
parse_config() {
    local file="$1"
    local config
    
    if config=$(tusk_parse "$file" 2>&1); then
        echo "$config"
    else
        echo "Error parsing $file: $config" >&2
        return 1
    fi
}

# Usage
config=$(parse_config config.tsk) || exit 1
```

## 📚 Resources

### 1. Documentation
- [Official Documentation](https://tusklang.org/docs/bash)
- [API Reference](https://tusklang.org/docs/bash/api)
- [Examples](https://tusklang.org/docs/bash/examples)

### 2. Community
- [GitHub Repository](https://github.com/bgengs/tusklang)
- [Issues](https://github.com/bgengs/tusklang/issues)
- [Discussions](https://github.com/bgengs/tusklang/discussions)

### 3. Examples
- [Basic Examples](https://github.com/bgengs/tusklang/tree/main/examples/bash)
- [Advanced Examples](https://github.com/bgengs/tusklang/tree/main/examples/bash/advanced)
- [Integration Examples](https://github.com/bgengs/tusklang/tree/main/examples/bash/integration)

---

**"We don't bow to any king" - TuskLang Bash SDK**

*Ready to revolutionize your shell scripting with intelligent configuration?* 
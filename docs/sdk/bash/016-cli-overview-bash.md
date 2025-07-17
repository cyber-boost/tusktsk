# 🖥️ TuskLang Bash CLI Overview Guide

**"We don't bow to any king" – Command line power at your fingertips.**

The TuskLang CLI is your gateway to powerful configuration management, validation, and automation. Whether you're parsing configs, validating syntax, or building deployment pipelines, the CLI provides the tools you need to work efficiently with TuskLang.

## 🎯 CLI Capabilities
The TuskLang CLI provides:
- **Configuration parsing and validation**
- **Environment management**
- **Database operations**
- **File operations**
- **System integration**
- **Automation support**

## 📝 Basic CLI Commands

### Parse Configuration
```bash
#!/bin/bash
source tusk-bash.sh

# Parse a configuration file
config=$(tusk_parse config.tsk)

# Get specific values
host=$(tusk_get "$config" server.host)
port=$(tusk_get "$config" server.port)

echo "Server: $host:$port"
```

### Validate Configuration
```bash
#!/bin/bash
source tusk-bash.sh

# Validate configuration syntax
if tusk_validate config.tsk; then
    echo "Configuration is valid"
else
    echo "Configuration has errors"
    exit 1
fi
```

### Environment Operations
```bash
#!/bin/bash
source tusk-bash.sh

# Get environment variable with fallback
api_key=$(tusk_env "API_KEY" "default_key")
echo "API Key: $api_key"

# Set environment variable
tusk_env_set "DEBUG" "true"
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

# Create a simple configuration
cat > app.tsk << 'EOF'
[app]
name: "TuskApp"
version: "2.1.0"

[server]
host: "localhost"
port: 8080
debug: true
EOF

# Parse and use the configuration
config=$(tusk_parse app.tsk)

# Extract values
app_name=$(tusk_get "$config" app.name)
app_version=$(tusk_get "$config" app.version)
server_host=$(tusk_get "$config" server.host)
server_port=$(tusk_get "$config" server.port)

# Use in your application
echo "Starting $app_name v$app_version"
echo "Server will run on $server_host:$server_port"

if [[ $(tusk_get "$config" server.debug) == "true" ]]; then
    echo "Debug mode enabled"
fi
```

## 🔗 Real-World Use Cases

### 1. Deployment Script
```bash
#!/bin/bash
source tusk-bash.sh

# Load deployment configuration
deploy_config=$(tusk_parse deploy.tsk)

# Extract deployment settings
environment=$(tusk_get "$deploy_config" deploy.environment)
servers=$(tusk_get "$deploy_config" deploy.servers)
backup_enabled=$(tusk_get "$deploy_config" deploy.backup)

echo "Deploying to $environment environment"

# Backup if enabled
if [[ "$backup_enabled" == "true" ]]; then
    echo "Creating backup..."
    # Backup logic here
fi

# Deploy to servers
for server in $servers; do
    echo "Deploying to $server"
    # Deployment logic here
done
```

### 2. System Monitoring
```bash
#!/bin/bash
source tusk-bash.sh

# Load monitoring configuration
monitor_config=$(tusk_parse monitor.tsk)

# Get monitoring settings
check_interval=$(tusk_get "$monitor_config" monitor.interval)
alert_threshold=$(tusk_get "$monitor_config" monitor.alert_threshold)
notification_email=$(tusk_get "$monitor_config" monitor.notification_email)

# Monitor system
while true; do
    cpu_usage=$(top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1)
    
    if (( $(echo "$cpu_usage > $alert_threshold" | bc -l) )); then
        echo "High CPU usage: ${cpu_usage}%"
        # Send alert logic here
    fi
    
    sleep "$check_interval"
done
```

### 3. Database Operations
```bash
#!/bin/bash
source tusk-bash.sh

# Load database configuration
db_config=$(tusk_parse database.tsk)

# Extract database settings
db_host=$(tusk_get "$db_config" database.host)
db_port=$(tusk_get "$db_config" database.port)
db_name=$(tusk_get "$db_config" database.name)
db_user=$(tusk_get "$db_config" database.user)

# Execute database query
query="SELECT COUNT(*) FROM users"
result=$(tusk_query "$db_config" "$query")
echo "User count: $result"
```

## 🧠 Advanced CLI Operations

### Configuration Merging
```bash
#!/bin/bash
source tusk-bash.sh

# Load multiple configurations
base_config=$(tusk_parse config/base.tsk)
env_config=$(tusk_parse config/production.tsk)

# Merge configurations (environment overrides base)
merged_config=$(tusk_merge "$base_config" "$env_config")

# Use merged configuration
app_name=$(tusk_get "$merged_config" app.name)
server_host=$(tusk_get "$merged_config" server.host)
```

### Dynamic Configuration Generation
```bash
#!/bin/bash
source tusk-bash.sh

# Generate configuration based on environment
generate_config() {
    local env="$1"
    
    cat > "config_$env.tsk" << EOF
[app]
name: "TuskApp"
environment: "$env"

[server]
host: $(tusk_env "SERVER_HOST" "localhost")
port: $(tusk_env "SERVER_PORT" "8080")
debug: $(tusk_env "DEBUG" "false")
EOF
}

# Generate configs for different environments
generate_config "development"
generate_config "staging"
generate_config "production"
```

### Error Handling
```bash
#!/bin/bash
source tusk-bash.sh

# Robust configuration loading with error handling
load_config() {
    local config_file="$1"
    
    if [[ ! -f "$config_file" ]]; then
        echo "Error: Configuration file $config_file not found"
        return 1
    fi
    
    if ! tusk_validate "$config_file"; then
        echo "Error: Invalid configuration in $config_file"
        return 1
    fi
    
    local config=$(tusk_parse "$config_file")
    echo "$config"
}

# Usage with error handling
if config=$(load_config "app.tsk"); then
    echo "Configuration loaded successfully"
    app_name=$(tusk_get "$config" app.name)
    echo "App: $app_name"
else
    echo "Failed to load configuration"
    exit 1
fi
```

## 🛡️ Security & Performance Notes
- **File permissions:** Ensure configuration files have appropriate permissions.
- **Environment variables:** Use environment variables for sensitive data.
- **Validation:** Always validate configuration before use.
- **Error handling:** Implement proper error handling for CLI operations.

## 🐞 Troubleshooting
- **Command not found:** Ensure tusk-bash.sh is sourced correctly.
- **Parse errors:** Check configuration syntax and file paths.
- **Permission denied:** Verify file permissions and access rights.
- **Environment issues:** Check environment variable availability.

## 💡 Best Practices
- **Always validate:** Validate configuration before using it.
- **Error handling:** Implement proper error handling for all CLI operations.
- **Documentation:** Document your CLI usage patterns.
- **Testing:** Test CLI operations in different environments.
- **Security:** Use secure methods for handling sensitive data.

## 🔗 Cross-References
- [Installation](001-installation-bash.md)
- [Quick Start](002-quick-start-bash.md)
- [Basic Syntax](003-basic-syntax-bash.md)

---

**Master the TuskLang CLI and unlock the full power of configuration management. 🖥️** 
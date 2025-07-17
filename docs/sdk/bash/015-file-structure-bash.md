# 📁 TuskLang Bash File Structure Guide

**"We don't bow to any king" – Structure your configuration like a pro.**

File structure in TuskLang is about organization, maintainability, and scalability. Whether you're building a simple script or a complex enterprise system, proper file structure makes your TuskLang configurations powerful, readable, and maintainable.

## 🎯 Why File Structure Matters
Good file structure provides:
- **Organization:** Logical grouping of related configuration
- **Maintainability:** Easy to find and modify settings
- **Scalability:** Support for growing applications
- **Reusability:** Share common configurations
- **Security:** Proper separation of sensitive data

## 📝 Basic File Structure

### Simple Single File
```ini
# config.tsk
[app]
name: "MyApp"
version: "1.0.0"

[server]
host: "localhost"
port: 8080

[database]
host: "db.local"
port: 5432
```

### Multi-File Structure
```
project/
├── config/
│   ├── main.tsk
│   ├── database.tsk
│   ├── server.tsk
│   └── security.tsk
├── environments/
│   ├── development.tsk
│   ├── staging.tsk
│   └── production.tsk
└── scripts/
    └── deploy.sh
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

# Create directory structure
mkdir -p config environments scripts

# Main configuration
cat > config/main.tsk << 'EOF'
[app]
name: "TuskApp"
version: "2.1.0"
environment: @env("APP_ENV", "development")
EOF

# Environment-specific config
cat > environments/development.tsk << 'EOF'
[server]
host: "localhost"
port: 8080
debug: true
EOF

# Load and merge configurations
main_config=$(tusk_parse config/main.tsk)
env_config=$(tusk_parse environments/development.tsk)

echo "App: $(tusk_get "$main_config" app.name) v$(tusk_get "$main_config" app.version)"
echo "Server: $(tusk_get "$env_config" server.host):$(tusk_get "$env_config" server.port)"
```

## 🔗 Real-World Use Cases

### 1. Enterprise Application Structure
```
enterprise-app/
├── config/
│   ├── main.tsk              # Core application settings
│   ├── database.tsk          # Database configuration
│   ├── api.tsk              # API settings
│   ├── security.tsk         # Security and authentication
│   ├── monitoring.tsk       # Logging and monitoring
│   └── features.tsk         # Feature flags
├── environments/
│   ├── development.tsk      # Development environment
│   ├── staging.tsk          # Staging environment
│   └── production.tsk       # Production environment
├── secrets/
│   ├── .env                 # Environment variables
│   └── .gitignore           # Ignore sensitive files
└── scripts/
    ├── deploy.sh            # Deployment script
    ├── backup.sh            # Backup script
    └── monitor.sh           # Monitoring script
```

### 2. Microservices Structure
```
microservices/
├── api-gateway/
│   ├── config.tsk
│   └── routes.tsk
├── user-service/
│   ├── config.tsk
│   └── database.tsk
├── order-service/
│   ├── config.tsk
│   └── database.tsk
└── shared/
    ├── common.tsk
    └── security.tsk
```

### 3. Configuration Loading Script
```bash
#!/bin/bash
source tusk-bash.sh

# Load configuration hierarchy
load_config() {
    local env=${1:-development}
    local config_dir="config"
    
    # Load main config
    local main_config=$(tusk_parse "$config_dir/main.tsk")
    
    # Load environment-specific config
    local env_config=$(tusk_parse "$config_dir/environments/$env.tsk")
    
    # Merge configurations (environment overrides main)
    echo "Loaded configuration for environment: $env"
    echo "App: $(tusk_get "$main_config" app.name)"
    echo "Server: $(tusk_get "$env_config" server.host)"
}

# Usage
load_config "production"
```

## 🧠 Advanced File Structure Patterns

### Configuration Inheritance
```ini
# config/base.tsk
[app]
name: "TuskApp"
version: "2.1.0"

[server]
timeout: 30
retries: 3

# config/production.tsk
@include "base.tsk"

[server]
host: "0.0.0.0"
port: 80
timeout: 60  # Override base timeout
```

### Modular Configuration
```ini
# config/database.tsk
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "tuskapp")

# config/security.tsk
[security]
encryption_key: @env.secure("ENCRYPTION_KEY")
session_secret: @env.secure("SESSION_SECRET")

# config/main.tsk
@include "database.tsk"
@include "security.tsk"

[app]
name: "TuskApp"
```

### Dynamic File Loading
```bash
#!/bin/bash
source tusk-bash.sh

# Load all config files in a directory
load_config_directory() {
    local dir="$1"
    local merged_config=""
    
    for file in "$dir"/*.tsk; do
        if [[ -f "$file" ]]; then
            local file_config=$(tusk_parse "$file")
            merged_config="$merged_config $file_config"
        fi
    done
    
    echo "$merged_config"
}

# Usage
config=$(load_config_directory "config")
```

## 🛡️ Security & Performance Notes
- **Separate secrets:** Keep sensitive data in separate files with proper permissions.
- **Environment isolation:** Use different files for different environments.
- **File permissions:** Set appropriate file permissions (600 for sensitive configs).
- **Backup strategy:** Include configuration files in your backup strategy.

## 🐞 Troubleshooting
- **File not found:** Check file paths and permissions.
- **Include errors:** Ensure included files exist and are accessible.
- **Environment confusion:** Use clear naming conventions for environment files.
- **Permission denied:** Check file and directory permissions.

## 💡 Best Practices
- **Consistent naming:** Use consistent naming conventions across your project.
- **Environment separation:** Keep environment-specific configs separate.
- **Documentation:** Document your file structure and configuration hierarchy.
- **Version control:** Include configuration templates in version control, exclude secrets.
- **Backup:** Regularly backup your configuration files.

## 🔗 Cross-References
- [Basic Syntax](003-basic-syntax-bash.md)
- [References](019-references-bash.md)
- [Best Practices](023-best-practices-bash.md)

---

**Structure your TuskLang configurations for success and scalability. 📁** 
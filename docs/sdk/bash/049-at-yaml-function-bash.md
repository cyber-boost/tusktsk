# 📝 TuskLang Bash @yaml Function Guide

**"We don't bow to any king" – YAML is your configuration's elegance.**

The @yaml function in TuskLang is your YAML processing powerhouse, enabling dynamic YAML parsing, generation, and manipulation directly within your configuration files. Whether you're working with Kubernetes manifests, Docker Compose files, or application configurations, @yaml provides the flexibility and power to handle structured data with elegance.

## 🎯 What is @yaml?
The @yaml function provides YAML processing capabilities in TuskLang. It offers:
- **YAML parsing** - Parse YAML strings and files
- **YAML generation** - Create YAML documents dynamically
- **YAML transformation** - Transform YAML data between formats
- **Configuration management** - Manage complex configuration structures
- **Template processing** - Process YAML templates with dynamic data

## 📝 Basic @yaml Syntax

### Simple YAML Parsing
```ini
[simple_yaml]
# Parse YAML string
$yaml_data: |
  name: John
  email: john@example.com
  age: 30
  active: true
parsed_yaml: @yaml.parse($yaml_data)
user_name: @yaml.get($parsed_yaml, "name")
user_email: @yaml.get($parsed_yaml, "email")
user_age: @yaml.get($parsed_yaml, "age")
```

### YAML File Processing
```ini
[yaml_file_processing]
# Parse YAML file
config_yaml: @yaml.parse(@file.read("/etc/app/config.yaml"))
api_config: @yaml.get($config_yaml, "api")
database_config: @yaml.get($config_yaml, "database")
```

### YAML Generation
```ini
[yaml_generation]
# Generate YAML dynamically
$user_data: {
    "name": "Alice",
    "email": "alice@example.com",
    "age": 30,
    "roles": ["admin", "user"]
}
user_yaml: @yaml.generate($user_data)

# Generate complex YAML
$config_data: {
    "database": {
        "host": "localhost",
        "port": 3306,
        "name": "app_db"
    },
    "api": {
        "url": "https://api.example.com",
        "timeout": 30
    }
}
config_yaml: @yaml.generate($config_data)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > yaml-quickstart.tsk << 'EOF'
[yaml_parsing]
# Parse YAML data
$yaml_string: |
  app:
    name: TuskLang
    version: 2.1.0
  database:
    host: localhost
    port: 3306
  api:
    url: https://api.example.com
    timeout: 30
parsed_config: @yaml.parse($yaml_string)

# Extract data
app_name: @yaml.get($parsed_config, "app.name")
app_version: @yaml.get($parsed_config, "app.version")
db_host: @yaml.get($parsed_config, "database.host")
api_url: @yaml.get($parsed_config, "api.url")

[yaml_generation]
# Generate YAML
$user_info: {
    "name": "John Doe",
    "email": "john@example.com",
    "role": "admin",
    "permissions": ["read", "write", "delete"]
}
user_yaml: @yaml.generate($user_info)

# Generate nested YAML
$nested_data: {
    "server": {
        "name": "web-server-01",
        "ip": "192.168.1.100",
        "services": {
            "web": {"port": 80, "enabled": true},
            "ssl": {"port": 443, "enabled": true}
        }
    }
}
server_yaml: @yaml.generate($nested_data)

[yaml_transformation]
# Transform YAML data
$source_yaml: |
  users:
    - id: 1
      name: Alice
      email: alice@example.com
    - id: 2
      name: Bob
      email: bob@example.com
transformed_yaml: @yaml.transform($source_yaml, "users[*]", {
    "template": {
        "person_id": "$.id",
        "full_name": "$.name",
        "contact": "$.email"
    }
})
EOF

config=$(tusk_parse yaml-quickstart.tsk)

echo "=== YAML Parsing ==="
echo "App Name: $(tusk_get "$config" yaml_parsing.app_name)"
echo "App Version: $(tusk_get "$config" yaml_parsing.app_version)"
echo "Database Host: $(tusk_get "$config" yaml_parsing.db_host)"
echo "API URL: $(tusk_get "$config" yaml_parsing.api_url)"

echo ""
echo "=== YAML Generation ==="
echo "User YAML: $(tusk_get "$config" yaml_generation.user_yaml)"
echo "Server YAML: $(tusk_get "$config" yaml_generation.server_yaml)"

echo ""
echo "=== YAML Transformation ==="
echo "Transformed YAML: $(tusk_get "$config" yaml_transformation.transformed_yaml)"
```

## 🔗 Real-World Use Cases

### 1. Kubernetes Configuration Management
```ini
[kubernetes_config]
# Generate Kubernetes deployment YAML
$deployment_config: {
    "apiVersion": "apps/v1",
    "kind": "Deployment",
    "metadata": {
        "name": "tusklang-app",
        "namespace": "default",
        "labels": {
            "app": "tusklang",
            "version": "2.1.0"
        }
    },
    "spec": {
        "replicas": @env("REPLICAS", "3"),
        "selector": {
            "matchLabels": {"app": "tusklang"}
        },
        "template": {
            "metadata": {
                "labels": {"app": "tusklang"}
            },
            "spec": {
                "containers": [{
                    "name": "tusklang",
                    "image": @env("IMAGE", "tusklang:latest"),
                    "ports": [{"containerPort": 8080}],
                    "env": [
                        {"name": "DATABASE_URL", "value": @env("DATABASE_URL")},
                        {"name": "API_KEY", "value": @env("API_KEY")}
                    ]
                }]
            }
        }
    }
}

deployment_yaml: @yaml.generate($deployment_config)
@file.write("/var/k8s/deployment.yaml", $deployment_yaml)

# Generate service YAML
$service_config: {
    "apiVersion": "v1",
    "kind": "Service",
    "metadata": {
        "name": "tusklang-service",
        "namespace": "default"
    },
    "spec": {
        "selector": {"app": "tusklang"},
        "ports": [{
            "protocol": "TCP",
            "port": 80,
            "targetPort": 8080
        }],
        "type": "LoadBalancer"
    }
}

service_yaml: @yaml.generate($service_config)
@file.write("/var/k8s/service.yaml", $service_yaml)
```

### 2. Docker Compose Configuration
```ini
[docker_compose]
# Generate Docker Compose configuration
$compose_config: {
    "version": "3.8",
    "services": {
        "app": {
            "build": ".",
            "ports": ["8080:8080"],
            "environment": [
                "DATABASE_URL=mysql://user:pass@db:3306/app",
                "REDIS_URL=redis://redis:6379"
            ],
            "depends_on": ["db", "redis"],
            "volumes": ["app_data:/var/app/data"]
        },
        "db": {
            "image": "mysql:8.0",
            "environment": [
                "MYSQL_ROOT_PASSWORD=" + @env("DB_ROOT_PASSWORD"),
                "MYSQL_DATABASE=app"
            ],
            "volumes": ["db_data:/var/lib/mysql"]
        },
        "redis": {
            "image": "redis:7-alpine",
            "ports": ["6379:6379"],
            "volumes": ["redis_data:/data"]
        }
    },
    "volumes": {
        "app_data": null,
        "db_data": null,
        "redis_data": null
    }
}

compose_yaml: @yaml.generate($compose_config)
@file.write("docker-compose.yml", $compose_yaml)
```

### 3. Application Configuration Management
```ini
[app_configuration]
# Parse application configuration
$app_yaml: @yaml.parse(@file.read("/etc/app/config.yaml"))

# Extract configuration sections
$app_config: {
    "name": @yaml.get($app_yaml, "app.name"),
    "version": @yaml.get($app_yaml, "app.version"),
    "environment": @yaml.get($app_yaml, "app.environment")
}

$database_config: {
    "host": @yaml.get($app_yaml, "database.host"),
    "port": @yaml.get($app_yaml, "database.port"),
    "name": @yaml.get($app_yaml, "database.name"),
    "credentials": {
        "username": @yaml.get($app_yaml, "database.credentials.username"),
        "password": @yaml.get($app_yaml, "database.credentials.password")
    }
}

# Generate updated configuration
$updated_config: {
    "app": $app_config,
    "database": $database_config,
    "timestamp": @date("Y-m-d H:i:s")
}

updated_yaml: @yaml.generate($updated_config)
@file.write("/etc/app/updated-config.yaml", $updated_yaml)
```

### 4. CI/CD Pipeline Configuration
```ini
[cicd_configuration]
# Generate GitHub Actions workflow
$workflow_config: {
    "name": "TuskLang CI/CD",
    "on": {
        "push": {"branches": ["main", "develop"]},
        "pull_request": {"branches": ["main"]}
    },
    "jobs": {
        "test": {
            "runs-on": "ubuntu-latest",
            "steps": [
                {
                    "name": "Checkout code",
                    "uses": "actions/checkout@v3"
                },
                {
                    "name": "Setup Node.js",
                    "uses": "actions/setup-node@v3",
                    "with": {"node-version": "18"}
                },
                {
                    "name": "Install dependencies",
                    "run": "npm install"
                },
                {
                    "name": "Run tests",
                    "run": "npm test"
                }
            ]
        },
        "deploy": {
            "runs-on": "ubuntu-latest",
            "needs": "test",
            "if": "github.ref == 'refs/heads/main'",
            "steps": [
                {
                    "name": "Deploy to production",
                    "run": "echo 'Deploying to production...'"
                }
            ]
        }
    }
}

workflow_yaml: @yaml.generate($workflow_config)
@file.write(".github/workflows/ci-cd.yml", $workflow_yaml)
```

## 🧠 Advanced @yaml Patterns

### YAML Template Processing
```ini
[yaml_templates]
# Process YAML templates with dynamic data
$template_yaml: |
  apiVersion: v1
  kind: ConfigMap
  metadata:
    name: {{ .AppName }}-config
    namespace: {{ .Namespace }}
  data:
    DATABASE_URL: {{ .DatabaseURL }}
    API_KEY: {{ .APIKey }}
    ENVIRONMENT: {{ .Environment }}

# Template variables
$template_vars: {
    "AppName": "tusklang",
    "Namespace": @env("NAMESPACE", "default"),
    "DatabaseURL": @env("DATABASE_URL"),
    "APIKey": @env("API_KEY"),
    "Environment": @env("ENVIRONMENT", "production")
}

# Process template
processed_yaml: @yaml.process_template($template_yaml, $template_vars)
@file.write("/var/k8s/configmap.yaml", $processed_yaml)
```

### YAML Data Validation
```ini
[yaml_validation]
# Validate YAML against schema
$yaml_document: @file.read("/var/config/app-config.yaml")
$schema_file: @file.read("/etc/schemas/app-schema.yaml")

# Validate YAML structure
validation_result: @yaml.validate($yaml_document, $schema_file)

# Handle validation errors
@if($validation_result.valid, {
    "action": "process_yaml",
    "data": @yaml.parse($yaml_document)
}, {
    "action": "handle_validation_errors",
    "errors": $validation_result.errors,
    "timestamp": @date("Y-m-d H:i:s")
})
```

### YAML Configuration Inheritance
```ini
[yaml_inheritance]
# Base configuration
$base_config: {
    "app": {
        "name": "tusklang",
        "version": "2.1.0"
    },
    "database": {
        "host": "localhost",
        "port": 3306
    }
}

# Environment-specific overrides
$dev_config: {
    "database": {
        "host": "dev-db.example.com",
        "port": 3306
    },
    "logging": {
        "level": "debug"
    }
}

$prod_config: {
    "database": {
        "host": "prod-db.example.com",
        "port": 3306
    },
    "logging": {
        "level": "info"
    }
}

# Merge configurations
$environment: @env("ENVIRONMENT", "development")
$final_config: @if($environment == "production", 
    @yaml.merge($base_config, $prod_config),
    @yaml.merge($base_config, $dev_config)
)

final_yaml: @yaml.generate($final_config)
@file.write("/etc/app/config-" + $environment + ".yaml", $final_yaml)
```

## 🛡️ Security & Performance Notes
- **YAML injection:** Validate and sanitize YAML input to prevent injection attacks
- **Memory usage:** Monitor memory consumption when processing large YAML files
- **Schema validation:** Always validate YAML against schemas when possible
- **Template security:** Sanitize template variables to prevent code injection
- **File permissions:** Ensure proper file permissions for YAML operations
- **Performance optimization:** Use efficient YAML processing for large files

## 🐞 Troubleshooting
- **Parsing errors:** Check YAML syntax and indentation
- **Schema validation:** Ensure YAML conforms to expected schema
- **Template errors:** Verify template syntax and variable substitution
- **Memory issues:** Optimize YAML processing for large files
- **Encoding problems:** Ensure proper character encoding (UTF-8)

## 💡 Best Practices
- **Use schemas:** Always validate YAML against schemas when possible
- **Proper indentation:** Maintain consistent YAML indentation
- **Template security:** Sanitize template variables
- **Error handling:** Implement proper error handling for YAML operations
- **Document structure:** Document YAML structure and expected format
- **Version control:** Use version control for YAML configurations

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@file Function](038-at-file-function-bash.md)
- [@env Function](026-at-env-function-bash.md)
- [Configuration Management](093-configuration-management-bash.md)
- [Container Orchestration](103-container-orchestration-bash.md)

---

**Master @yaml in TuskLang and structure your configurations with elegance. 📝** 
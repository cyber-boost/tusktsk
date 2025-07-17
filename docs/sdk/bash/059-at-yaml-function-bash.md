# 📋 TuskLang Bash @yaml Function Guide

**"We don't bow to any king" – YAML is your configuration's human-readable format.**

The @yaml function in TuskLang is your YAML manipulation powerhouse, enabling dynamic YAML operations, configuration parsing, and format conversion directly within your configuration files. Whether you're parsing Kubernetes manifests, Docker Compose files, or application configurations, @yaml provides the flexibility and power to work with YAML data seamlessly.

## 🎯 What is @yaml?
The @yaml function provides YAML operations in TuskLang. It offers:
- **YAML parsing** - Parse YAML strings into data structures
- **YAML generation** - Create YAML from data structures
- **YAML validation** - Validate YAML syntax and structure
- **YAML transformation** - Transform and manipulate YAML data
- **YAML formatting** - Format YAML with proper indentation and structure

## 📝 Basic @yaml Syntax

### YAML Parsing
```ini
[yaml_parsing]
# Parse simple YAML strings
$simple_yaml: |
  name: John Doe
  age: 30
  city: New York
parsed_simple: @yaml.parse($simple_yaml)

$array_yaml: |
  - apple
  - banana
  - cherry
parsed_array: @yaml.parse($array_yaml)

$nested_yaml: |
  user:
    name: Alice
    profile:
      age: 25
      city: San Francisco
parsed_nested: @yaml.parse($nested_yaml)

# Parse YAML with validation
$valid_yaml: |
  valid: true
  data: test
validated_yaml: @yaml.parse($valid_yaml, true)

# Parse YAML with error handling
$invalid_yaml: |
  invalid: true
  missing: quote
safe_parse: @yaml.parse($invalid_yaml, false, {"error": "Invalid YAML"})
```

### YAML Generation
```ini
[yaml_generation]
# Generate YAML from data structures
$user_data: {
    "id": 123,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "preferences": {
        "theme": "dark",
        "notifications": true
    }
}

user_yaml: @yaml.generate($user_data)
user_yaml_pretty: @yaml.generate($user_data, true)

# Generate YAML arrays
$services: [
    {"name": "web", "port": 80, "image": "nginx:latest"},
    {"name": "api", "port": 3000, "image": "node:16"},
    {"name": "db", "port": 5432, "image": "postgres:13"}
]
services_yaml: @yaml.generate($services, true)

# Generate YAML with custom options
$config_data: {
    "database": {
        "host": "localhost",
        "port": 3306
    },
    "api": {
        "url": "https://api.example.com"
    }
}

config_yaml: @yaml.generate($config_data, true, {
    "indent": 4,
    "sort_keys": true,
    "explicit_start": true
})
```

### YAML Validation
```ini
[yaml_validation]
# Validate YAML syntax
$valid_yaml_string: |
  name: John
  age: 30
is_valid: @yaml.validate($valid_yaml_string)

$invalid_yaml_string: |
  name: John
  age: 30
  city: New York
is_invalid: @yaml.validate($invalid_yaml_string)

# Validate YAML structure
$yaml_schema: {
    "type": "object",
    "properties": {
        "name": {"type": "string"},
        "age": {"type": "number"},
        "email": {"type": "string", "format": "email"}
    },
    "required": ["name", "age"]
}

$test_data: {
    "name": "Alice",
    "age": 25,
    "email": "alice@example.com"
}

schema_validation: @yaml.validate_schema($test_data, $yaml_schema)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > yaml-quickstart.tsk << 'EOF'
[yaml_parsing]
# Parse YAML data
$docker_compose: |
  version: '3.8'
  services:
    web:
      image: nginx:latest
      ports:
        - "80:80"
    api:
      image: node:16
      ports:
        - "3000:3000"
parsed_compose: @yaml.parse($docker_compose)

$kubernetes_pod: |
  apiVersion: v1
  kind: Pod
  metadata:
    name: my-pod
  spec:
    containers:
    - name: my-container
      image: nginx:latest
parsed_pod: @yaml.parse($kubernetes_pod)

[yaml_generation]
# Generate YAML from data
$app_config: {
    "app": {
        "name": "MyApp",
        "version": "1.0.0",
        "environment": "production"
    },
    "database": {
        "host": "localhost",
        "port": 5432,
        "name": "myapp_db"
    },
    "features": [
        "authentication",
        "caching",
        "logging"
    ]
}

app_yaml: @yaml.generate($app_config)
app_yaml_pretty: @yaml.generate($app_config, true)

[yaml_validation]
# Validate YAML
$test_yaml: |
  name: Charlie
  age: 30
  email: charlie@example.com
is_valid: @yaml.validate($test_yaml)

$invalid_yaml: |
  name: David
  age: 35
  email: david@example.com
is_invalid: @yaml.validate($invalid_yaml)

[yaml_transformation]
# Transform YAML data
$k8s_resources: {
    "pods": [
        {"name": "web-pod", "image": "nginx", "status": "running"},
        {"name": "api-pod", "image": "node", "status": "running"},
        {"name": "db-pod", "image": "postgres", "status": "pending"}
    ]
}

running_pods: @yaml.filter($k8s_resources.pods, "item.status == 'running'")
pod_names: @yaml.map($k8s_resources.pods, "item.name")
pod_count: @yaml.length($k8s_resources.pods)
EOF

config=$(tusk_parse yaml-quickstart.tsk)

echo "=== YAML Parsing ==="
echo "Parsed Compose: $(tusk_get "$config" yaml_parsing.parsed_compose)"
echo "Parsed Pod: $(tusk_get "$config" yaml_parsing.parsed_pod)"

echo ""
echo "=== YAML Generation ==="
echo "App YAML: $(tusk_get "$config" yaml_generation.app_yaml)"
echo "Pretty YAML: $(tusk_get "$config" yaml_generation.app_yaml_pretty)"

echo ""
echo "=== YAML Validation ==="
echo "Is Valid: $(tusk_get "$config" yaml_validation.is_valid)"
echo "Is Invalid: $(tusk_get "$config" yaml_validation.is_invalid)"

echo ""
echo "=== YAML Transformation ==="
echo "Running Pods: $(tusk_get "$config" yaml_transformation.running_pods)"
echo "Pod Names: $(tusk_get "$config" yaml_transformation.pod_names)"
echo "Pod Count: $(tusk_get "$config" yaml_transformation.pod_count)"
```

## 🔗 Real-World Use Cases

### 1. Kubernetes Configuration Management
```ini
[kubernetes_config]
# Manage Kubernetes configurations with YAML
$k8s_templates: {
    "deployment": {
        "apiVersion": "apps/v1",
        "kind": "Deployment",
        "metadata": {
            "name": "my-app",
            "labels": {
                "app": "my-app"
            }
        },
        "spec": {
            "replicas": 3,
            "selector": {
                "matchLabels": {
                    "app": "my-app"
                }
            },
            "template": {
                "metadata": {
                    "labels": {
                        "app": "my-app"
                    }
                },
                "spec": {
                    "containers": [
                        {
                            "name": "my-app",
                            "image": "my-app:latest",
                            "ports": [
                                {
                                    "containerPort": 8080
                                }
                            ]
                        }
                    ]
                }
            }
        }
    },
    "service": {
        "apiVersion": "v1",
        "kind": "Service",
        "metadata": {
            "name": "my-app-service"
        },
        "spec": {
            "selector": {
                "app": "my-app"
            },
            "ports": [
                {
                    "protocol": "TCP",
                    "port": 80,
                    "targetPort": 8080
                }
            ],
            "type": "LoadBalancer"
        }
    }
}

# Generate Kubernetes YAML
$k8s_generation: {
    "deployment_yaml": @yaml.generate($k8s_templates.deployment, true),
    "service_yaml": @yaml.generate($k8s_templates.service, true),
    "combined_yaml": @yaml.generate($k8s_templates, true)
}

# Parse existing Kubernetes configurations
$existing_k8s_yaml: |
  apiVersion: v1
  kind: ConfigMap
  metadata:
    name: app-config
  data:
    DATABASE_URL: postgresql://localhost:5432/myapp
    API_KEY: ${API_KEY}
    LOG_LEVEL: info

parsed_configmap: @yaml.parse($existing_k8s_yaml)

# Transform Kubernetes configurations
$k8s_transformation: {
    "update_replicas": @yaml.set($k8s_templates.deployment, "spec.replicas", 5),
    "add_environment": @yaml.set($k8s_templates.deployment, "spec.template.spec.containers[0].env", [
        {"name": "NODE_ENV", "value": "production"},
        {"name": "PORT", "value": "8080"}
    ]),
    "add_resources": @yaml.set($k8s_templates.deployment, "spec.template.spec.containers[0].resources", {
        "requests": {"memory": "128Mi", "cpu": "100m"},
        "limits": {"memory": "256Mi", "cpu": "200m"}
    })
}
```

### 2. Docker Compose Configuration
```ini
[docker_compose]
# Manage Docker Compose configurations
$compose_templates: {
    "web_app": {
        "version": "3.8",
        "services": {
            "web": {
                "image": "nginx:latest",
                "ports": ["80:80"],
                "volumes": ["./nginx.conf:/etc/nginx/nginx.conf"],
                "environment": {
                    "NGINX_HOST": "localhost",
                    "NGINX_PORT": "80"
                }
            },
            "api": {
                "image": "node:16",
                "ports": ["3000:3000"],
                "volumes": ["./app:/app"],
                "environment": {
                    "NODE_ENV": "development",
                    "PORT": "3000"
                },
                "depends_on": ["db"]
            },
            "db": {
                "image": "postgres:13",
                "ports": ["5432:5432"],
                "environment": {
                    "POSTGRES_DB": "myapp",
                    "POSTGRES_USER": "postgres",
                    "POSTGRES_PASSWORD": "password"
                },
                "volumes": ["postgres_data:/var/lib/postgresql/data"]
            }
        },
        "volumes": {
            "postgres_data": null
        }
    }
}

# Generate Docker Compose YAML
$compose_generation: {
    "web_app_yaml": @yaml.generate($compose_templates.web_app, true),
    "services_only": @yaml.generate($compose_templates.web_app.services, true)
}

# Parse existing Docker Compose files
$existing_compose: |
  version: '3.8'
  services:
    redis:
      image: redis:alpine
      ports:
        - "6379:6379"
    cache:
      image: memcached:alpine
      ports:
        - "11211:11211"

parsed_compose: @yaml.parse($existing_compose)

# Merge Docker Compose configurations
$merged_compose: @yaml.merge($compose_templates.web_app, $parsed_compose)
merged_compose_yaml: @yaml.generate($merged_compose, true)
```

### 3. Application Configuration Management
```ini
[app_config]
# Manage application configurations with YAML
$app_config_templates: {
    "development": {
        "app": {
            "name": "MyApp",
            "version": "1.0.0",
            "debug": true,
            "log_level": "debug"
        },
        "database": {
            "host": "localhost",
            "port": 5432,
            "name": "myapp_dev",
            "pool_size": 5
        },
        "redis": {
            "host": "localhost",
            "port": 6379,
            "db": 0
        },
        "features": {
            "authentication": true,
            "caching": true,
            "logging": true,
            "monitoring": false
        }
    },
    "production": {
        "app": {
            "name": "MyApp",
            "version": "1.0.0",
            "debug": false,
            "log_level": "info"
        },
        "database": {
            "host": "@env('DB_HOST')",
            "port": "@env('DB_PORT', '5432')",
            "name": "@env('DB_NAME')",
            "pool_size": 20
        },
        "redis": {
            "host": "@env('REDIS_HOST')",
            "port": "@env('REDIS_PORT', '6379')",
            "db": 0
        },
        "features": {
            "authentication": true,
            "caching": true,
            "logging": true,
            "monitoring": true
        }
    }
}

# Generate application configuration YAML
$app_config_generation: {
    "dev_config": @yaml.generate($app_config_templates.development, true),
    "prod_config": @yaml.generate($app_config_templates.production, true)
}

# Parse configuration from YAML
$config_yaml_string: |
  app:
    name: MyApp
    version: 1.0.0
    debug: false
  database:
    host: prod.example.com
    port: 5432
    name: myapp_prod
  features:
    authentication: true
    caching: true

parsed_config: @yaml.parse($config_yaml_string)

# Validate configuration structure
$config_schema: {
    "type": "object",
    "properties": {
        "app": {
            "type": "object",
            "properties": {
                "name": {"type": "string"},
                "version": {"type": "string"},
                "debug": {"type": "boolean"}
            },
            "required": ["name", "version"]
        },
        "database": {
            "type": "object",
            "properties": {
                "host": {"type": "string"},
                "port": {"type": "number"},
                "name": {"type": "string"}
            },
            "required": ["host", "name"]
        }
    },
    "required": ["app", "database"]
}

config_validation: @yaml.validate_schema($parsed_config, $config_schema)
```

### 4. CI/CD Pipeline Configuration
```ini
[ci_cd_config]
# Manage CI/CD pipeline configurations
$pipeline_templates: {
    "github_actions": {
        "name": "CI/CD Pipeline",
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
                        "with": {
                            "node-version": "16"
                        }
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
            "build": {
                "runs-on": "ubuntu-latest",
                "needs": ["test"],
                "steps": [
                    {
                        "name": "Build application",
                        "run": "npm run build"
                    },
                    {
                        "name": "Build Docker image",
                        "run": "docker build -t myapp:${{ github.sha }} ."
                    }
                ]
            }
        }
    }
}

# Generate CI/CD YAML
$ci_cd_generation: {
    "github_actions_yaml": @yaml.generate($pipeline_templates.github_actions, true),
    "test_job_only": @yaml.generate($pipeline_templates.github_actions.jobs.test, true)
}

# Parse existing pipeline configurations
$existing_pipeline: |
  name: Deploy to Production
  on:
    push:
      branches: [main]
  jobs:
    deploy:
      runs-on: ubuntu-latest
      steps:
      - name: Deploy to production
        run: |
          echo "Deploying to production..."
          kubectl apply -f k8s/

parsed_pipeline: @yaml.parse($existing_pipeline)

# Merge pipeline configurations
$merged_pipeline: @yaml.merge($pipeline_templates.github_actions, $parsed_pipeline)
merged_pipeline_yaml: @yaml.generate($merged_pipeline, true)
```

## 🧠 Advanced @yaml Patterns

### YAML Schema Validation
```ini
[yaml_schema]
# Define YAML schemas
$yaml_schemas: {
    "kubernetes_deployment": {
        "type": "object",
        "properties": {
            "apiVersion": {"type": "string", "pattern": "^apps/v1$"},
            "kind": {"type": "string", "pattern": "^Deployment$"},
            "metadata": {
                "type": "object",
                "properties": {
                    "name": {"type": "string", "minLength": 1},
                    "labels": {"type": "object"}
                },
                "required": ["name"]
            },
            "spec": {
                "type": "object",
                "properties": {
                    "replicas": {"type": "number", "minimum": 1},
                    "selector": {"type": "object"},
                    "template": {"type": "object"}
                },
                "required": ["replicas", "selector", "template"]
            }
        },
        "required": ["apiVersion", "kind", "metadata", "spec"]
    },
    "docker_compose": {
        "type": "object",
        "properties": {
            "version": {"type": "string"},
            "services": {
                "type": "object",
                "additionalProperties": {
                    "type": "object",
                    "properties": {
                        "image": {"type": "string"},
                        "ports": {"type": "array"},
                        "environment": {"type": "object"}
                    }
                }
            }
        },
        "required": ["services"]
    }
}

# Validate YAML against schemas
$validation_examples: {
    "valid_deployment": {
        "apiVersion": "apps/v1",
        "kind": "Deployment",
        "metadata": {
            "name": "my-app",
            "labels": {"app": "my-app"}
        },
        "spec": {
            "replicas": 3,
            "selector": {"matchLabels": {"app": "my-app"}},
            "template": {
                "metadata": {"labels": {"app": "my-app"}},
                "spec": {"containers": []}
            }
        }
    },
    "invalid_deployment": {
        "apiVersion": "v1",
        "kind": "Pod",
        "metadata": {"name": ""},
        "spec": {}
    }
}

$schema_validation_results: {
    "valid_deployment_result": @yaml.validate_schema($validation_examples.valid_deployment, $yaml_schemas.kubernetes_deployment),
    "invalid_deployment_result": @yaml.validate_schema($validation_examples.invalid_deployment, $yaml_schemas.kubernetes_deployment)
}
```

### YAML Transformation and Mapping
```ini
[yaml_transformation]
# Advanced YAML transformation
$transformation_functions: {
    "flatten_yaml": @function.create("data, prefix", """
        var flattened = {};
        prefix = prefix || '';
        
        for (var key in data) {
            var new_key = prefix ? prefix + '.' + key : key;
            
            if (typeof data[key] === 'object' && data[key] !== null && !Array.isArray(data[key])) {
                var nested = @function.call('flatten_yaml', data[key], new_key);
                for (var nested_key in nested) {
                    flattened[nested_key] = nested[nested_key];
                }
            } else {
                flattened[new_key] = data[key];
            }
        }
        
        return flattened;
    """),
    
    "unflatten_yaml": @function.create("data", """
        var unflattened = {};
        
        for (var key in data) {
            var keys = key.split('.');
            var current = unflattened;
            
            for (var i = 0; i < keys.length - 1; i++) {
                if (!current[keys[i]]) {
                    current[keys[i]] = {};
                }
                current = current[keys[i]];
            }
            
            current[keys[keys.length - 1]] = data[key];
        }
        
        return unflattened;
    """),
    
    "yaml_diff": @function.create("yaml1, yaml2", """
        var data1 = @yaml.parse(yaml1);
        var data2 = @yaml.parse(yaml2);
        
        var diff = {};
        
        for (var key in data1) {
            if (!data2.hasOwnProperty(key)) {
                diff[key] = {'removed': data1[key]};
            } else if (@yaml.generate(data1[key]) !== @yaml.generate(data2[key])) {
                diff[key] = {
                    'old': data1[key],
                    'new': data2[key]
                };
            }
        }
        
        for (var key in data2) {
            if (!data1.hasOwnProperty(key)) {
                diff[key] = {'added': data2[key]};
            }
        }
        
        return diff;
    """)
}

# Use transformation functions
$transformation_examples: {
    "nested_data": {
        "user": {
            "profile": {
                "name": "Alice",
                "age": 25
            },
            "settings": {
                "theme": "dark"
            }
        }
    },
    
    "flattened_data": @function.call("flatten_yaml", $transformation_examples.nested_data),
    "unflattened_data": @function.call("unflatten_yaml", $transformation_examples.flattened_data)
}
```

## 🛡️ Security & Performance Notes
- **YAML injection:** Validate and sanitize YAML data to prevent injection attacks
- **Memory usage:** Monitor YAML size for large data structures
- **Parsing performance:** Use efficient YAML parsing for large datasets
- **Schema validation:** Always validate YAML against schemas for critical data
- **Error handling:** Implement proper error handling for YAML parsing failures
- **Data validation:** Validate YAML structure and content before processing

## 🐞 Troubleshooting
- **YAML parsing errors:** Check YAML syntax and validate structure
- **Memory issues:** Monitor YAML size and implement chunking for large data
- **Schema validation failures:** Review schema definitions and data structure
- **Performance problems:** Optimize YAML operations for large datasets
- **Encoding issues:** Ensure proper character encoding for YAML data

## 💡 Best Practices
- **Use schemas:** Always define and validate YAML schemas for critical data
- **Handle errors:** Implement proper error handling for YAML operations
- **Optimize size:** Minimize YAML size by removing unnecessary fields
- **Validate input:** Always validate YAML input before processing
- **Use pretty printing:** Use pretty printing for human-readable YAML
- **Document structure:** Document YAML structure and field meanings

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@json Function](058-at-json-function-bash.md)
- [@validate Function](036-at-validate-function-bash.md)
- [Configuration Management](115-configuration-management-bash.md)
- [Container Orchestration](116-container-orchestration-bash.md)

---

**Master @yaml in TuskLang and wield the power of human-readable configuration in your setups. 📋** 
# ⚙️ TuskLang Bash @toml Function Guide

**"We don't bow to any king" – TOML is your configuration's structured format.**

The @toml function in TuskLang is your TOML manipulation powerhouse, enabling dynamic TOML operations, configuration parsing, and format conversion directly within your configuration files. Whether you're parsing Rust Cargo.toml files, Python pyproject.toml, or application configurations, @toml provides the flexibility and power to work with TOML data seamlessly.

## 🎯 What is @toml?
The @toml function provides TOML operations in TuskLang. It offers:
- **TOML parsing** - Parse TOML strings into data structures
- **TOML generation** - Create TOML from data structures
- **TOML validation** - Validate TOML syntax and structure
- **TOML transformation** - Transform and manipulate TOML data
- **TOML formatting** - Format TOML with proper structure and comments

## 📝 Basic @toml Syntax

### TOML Parsing
```ini
[toml_parsing]
# Parse simple TOML strings
$simple_toml: |
  name = "John Doe"
  age = 30
  city = "New York"
parsed_simple: @toml.parse($simple_toml)

$array_toml: |
  fruits = ["apple", "banana", "cherry"]
  numbers = [1, 2, 3, 4, 5]
parsed_array: @toml.parse($array_toml)

$nested_toml: |
  [user]
  name = "Alice"
  [user.profile]
  age = 25
  city = "San Francisco"
parsed_nested: @toml.parse($nested_toml)

# Parse TOML with validation
$valid_toml: |
  valid = true
  data = "test"
validated_toml: @toml.parse($valid_toml, true)

# Parse TOML with error handling
$invalid_toml: |
  invalid = true
  missing = quote
safe_parse: @toml.parse($invalid_toml, false, {"error": "Invalid TOML"})
```

### TOML Generation
```ini
[toml_generation]
# Generate TOML from data structures
$user_data: {
    "id": 123,
    "name": "Alice Johnson",
    "email": "alice@example.com",
    "preferences": {
        "theme": "dark",
        "notifications": true
    }
}

user_toml: @toml.generate($user_data)
user_toml_pretty: @toml.generate($user_data, true)

# Generate TOML with sections
$config_data: {
    "database": {
        "host": "localhost",
        "port": 3306,
        "name": "tusklang"
    },
    "api": {
        "url": "https://api.example.com",
        "timeout": 30
    }
}

config_toml: @toml.generate($config_data, true)

# Generate TOML with custom options
$app_config: {
    "app": {
        "name": "MyApp",
        "version": "1.0.0",
        "debug": false
    },
    "features": ["auth", "caching", "logging"]
}

app_toml: @toml.generate($app_config, true, {
    "indent": 4,
    "sort_keys": true,
    "add_comments": true
})
```

### TOML Validation
```ini
[toml_validation]
# Validate TOML syntax
$valid_toml_string: |
  name = "John"
  age = 30
is_valid: @toml.validate($valid_toml_string)

$invalid_toml_string: |
  name = "John"
  age = 30
  city = "New York"
is_invalid: @toml.validate($invalid_toml_string)

# Validate TOML structure
$toml_schema: {
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

schema_validation: @toml.validate_schema($test_data, $toml_schema)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > toml-quickstart.tsk << 'EOF'
[toml_parsing]
# Parse TOML data
$cargo_toml: |
  [package]
  name = "my-app"
  version = "1.0.0"
  authors = ["Alice <alice@example.com>"]
  
  [dependencies]
  serde = "1.0"
  tokio = { version = "1.0", features = ["full"] }
  
  [dev-dependencies]
  assert = "2.0"
parsed_cargo: @toml.parse($cargo_toml)

$pyproject_toml: |
  [build-system]
  requires = ["setuptools>=45", "wheel"]
  build-backend = "setuptools.build_meta"
  
  [project]
  name = "my-python-app"
  version = "1.0.0"
  description = "A Python application"
parsed_pyproject: @toml.parse($pyproject_toml)

[toml_generation]
# Generate TOML from data
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
    "features": ["authentication", "caching", "logging"]
}

app_toml: @toml.generate($app_config)
app_toml_pretty: @toml.generate($app_config, true)

[toml_validation]
# Validate TOML
$test_toml: |
  name = "Charlie"
  age = 30
  email = "charlie@example.com"
is_valid: @toml.validate($test_toml)

$invalid_toml: |
  name = "David"
  age = 35
  email = "david@example.com"
is_invalid: @toml.validate($invalid_toml)

[toml_transformation]
# Transform TOML data
$config_data: {
    "services": [
        {"name": "web", "port": 80, "enabled": true},
        {"name": "api", "port": 3000, "enabled": true},
        {"name": "db", "port": 5432, "enabled": false}
    ]
}

enabled_services: @toml.filter($config_data.services, "item.enabled == true")
service_names: @toml.map($config_data.services, "item.name")
service_count: @toml.length($config_data.services)
EOF

config=$(tusk_parse toml-quickstart.tsk)

echo "=== TOML Parsing ==="
echo "Parsed Cargo: $(tusk_get "$config" toml_parsing.parsed_cargo)"
echo "Parsed PyProject: $(tusk_get "$config" toml_parsing.parsed_pyproject)"

echo ""
echo "=== TOML Generation ==="
echo "App TOML: $(tusk_get "$config" toml_generation.app_toml)"
echo "Pretty TOML: $(tusk_get "$config" toml_generation.app_toml_pretty)"

echo ""
echo "=== TOML Validation ==="
echo "Is Valid: $(tusk_get "$config" toml_validation.is_valid)"
echo "Is Invalid: $(tusk_get "$config" toml_validation.is_invalid)"

echo ""
echo "=== TOML Transformation ==="
echo "Enabled Services: $(tusk_get "$config" toml_transformation.enabled_services)"
echo "Service Names: $(tusk_get "$config" toml_transformation.service_names)"
echo "Service Count: $(tusk_get "$config" toml_transformation.service_count)"
```

## 🔗 Real-World Use Cases

### 1. Rust Project Configuration
```ini
[rust_config]
# Manage Rust project configurations
$cargo_templates: {
    "library": {
        "package": {
            "name": "my-library",
            "version": "1.0.0",
            "edition": "2021",
            "authors": ["Alice <alice@example.com>"],
            "description": "A Rust library",
            "license": "MIT"
        },
        "lib": {
            "name": "my_library",
            "crate-type": ["lib"]
        },
        "dependencies": {
            "serde": "1.0",
            "tokio": {"version": "1.0", "features": ["full"]}
        },
        "dev-dependencies": {
            "assert": "2.0"
        }
    },
    "binary": {
        "package": {
            "name": "my-app",
            "version": "1.0.0",
            "edition": "2021"
        },
        "bin": [
            {"name": "my-app", "path": "src/main.rs"}
        ],
        "dependencies": {
            "clap": {"version": "4.0", "features": ["derive"]},
            "tokio": {"version": "1.0", "features": ["full"]}
        }
    }
}

# Generate Cargo.toml files
$cargo_generation: {
    "library_toml": @toml.generate($cargo_templates.library, true),
    "binary_toml": @toml.generate($cargo_templates.binary, true)
}

# Parse existing Cargo.toml
$existing_cargo: |
  [package]
  name = "existing-app"
  version = "0.1.0"
  edition = "2021"
  
  [dependencies]
  serde = "1.0"
  
  [features]
  default = ["std"]
  std = []

parsed_cargo: @toml.parse($existing_cargo)

# Update Cargo.toml
$updated_cargo: @toml.merge($existing_cargo, {
    "dependencies": {
        "tokio": {"version": "1.0", "features": ["full"]}
    },
    "dev-dependencies": {
        "assert": "2.0"
    }
})
updated_cargo_toml: @toml.generate($updated_cargo, true)
```

### 2. Python Project Configuration
```ini
[python_config]
# Manage Python project configurations
$pyproject_templates: {
    "standard": {
        "build-system": {
            "requires": ["setuptools>=45", "wheel"],
            "build-backend": "setuptools.build_meta"
        },
        "project": {
            "name": "my-python-app",
            "version": "1.0.0",
            "description": "A Python application",
            "authors": [{"name": "Alice", "email": "alice@example.com"}],
            "license": {"text": "MIT"},
            "requires-python": ">=3.8",
            "dependencies": [
                "requests>=2.25.0",
                "click>=8.0.0"
            ]
        },
        "project.optional-dependencies": {
            "dev": [
                "pytest>=6.0.0",
                "black>=21.0.0",
                "flake8>=3.8.0"
            ]
        }
    },
    "poetry": {
        "tool.poetry": {
            "name": "my-poetry-app",
            "version": "1.0.0",
            "description": "A Poetry application",
            "authors": ["Alice <alice@example.com>"],
            "dependencies": {
                "python": "^3.8",
                "requests": "^2.25.0"
            },
            "dev-dependencies": {
                "pytest": "^6.0.0"
            }
        },
        "tool.poetry.scripts": {
            "my-app": "my_app:main"
        }
    }
}

# Generate pyproject.toml files
$pyproject_generation: {
    "standard_toml": @toml.generate($pyproject_templates.standard, true),
    "poetry_toml": @toml.generate($pyproject_templates.poetry, true)
}

# Parse existing pyproject.toml
$existing_pyproject: |
  [project]
  name = "existing-python-app"
  version = "0.1.0"
  description = "An existing Python app"
  
  [project.dependencies]
  requests = ">=2.25.0"

parsed_pyproject: @toml.parse($existing_pyproject)

# Add new dependencies
$updated_pyproject: @toml.merge($existing_pyproject, {
    "project.dependencies": {
        "click": ">=8.0.0",
        "pydantic": ">=1.8.0"
    },
    "project.optional-dependencies": {
        "dev": ["pytest>=6.0.0", "black>=21.0.0"]
    }
})
updated_pyproject_toml: @toml.generate($updated_pyproject, true)
```

### 3. Application Configuration Management
```ini
[app_config]
# Manage application configurations with TOML
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
        "features": ["authentication", "caching", "logging"]
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
        "features": ["authentication", "caching", "logging", "monitoring"]
    }
}

# Generate application configuration TOML
$app_config_generation: {
    "dev_config": @toml.generate($app_config_templates.development, true),
    "prod_config": @toml.generate($app_config_templates.production, true)
}

# Parse configuration from TOML
$config_toml_string: |
  [app]
  name = "MyApp"
  version = "1.0.0"
  debug = false
  
  [database]
  host = "prod.example.com"
  port = 5432
  name = "myapp_prod"
  
  [features]
  authentication = true
  caching = true

parsed_config: @toml.parse($config_toml_string)

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

config_validation: @toml.validate_schema($parsed_config, $config_schema)
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
            }
        }
    }
}

# Generate CI/CD TOML
$ci_cd_generation: {
    "github_actions_toml": @toml.generate($pipeline_templates.github_actions, true)
}

# Parse existing pipeline configurations
$existing_pipeline: |
  [deploy]
  name = "Deploy to Production"
  on.push.branches = ["main"]
  
  [deploy.jobs.deploy]
  runs-on = "ubuntu-latest"
  
  [[deploy.jobs.deploy.steps]]
  name = "Deploy to production"
  run = "kubectl apply -f k8s/"

parsed_pipeline: @toml.parse($existing_pipeline)

# Merge pipeline configurations
$merged_pipeline: @toml.merge($pipeline_templates.github_actions, $parsed_pipeline)
merged_pipeline_toml: @toml.generate($merged_pipeline, true)
```

## 🧠 Advanced @toml Patterns

### TOML Schema Validation
```ini
[toml_schema]
# Define TOML schemas
$toml_schemas: {
    "cargo_package": {
        "type": "object",
        "properties": {
            "package": {
                "type": "object",
                "properties": {
                    "name": {"type": "string", "minLength": 1},
                    "version": {"type": "string", "pattern": "^\\d+\\.\\d+\\.\\d+$"},
                    "edition": {"type": "string", "enum": ["2015", "2018", "2021"]}
                },
                "required": ["name", "version"]
            },
            "dependencies": {"type": "object"}
        },
        "required": ["package"]
    },
    "pyproject": {
        "type": "object",
        "properties": {
            "project": {
                "type": "object",
                "properties": {
                    "name": {"type": "string"},
                    "version": {"type": "string"},
                    "description": {"type": "string"}
                },
                "required": ["name", "version"]
            }
        },
        "required": ["project"]
    }
}

# Validate TOML against schemas
$validation_examples: {
    "valid_cargo": {
        "package": {
            "name": "my-app",
            "version": "1.0.0",
            "edition": "2021"
        },
        "dependencies": {
            "serde": "1.0"
        }
    },
    "invalid_cargo": {
        "package": {
            "name": "",
            "version": "invalid"
        }
    }
}

$schema_validation_results: {
    "valid_cargo_result": @toml.validate_schema($validation_examples.valid_cargo, $toml_schemas.cargo_package),
    "invalid_cargo_result": @toml.validate_schema($validation_examples.invalid_cargo, $toml_schemas.cargo_package)
}
```

## 🛡️ Security & Performance Notes
- **TOML injection:** Validate and sanitize TOML data to prevent injection attacks
- **Memory usage:** Monitor TOML size for large data structures
- **Parsing performance:** Use efficient TOML parsing for large datasets
- **Schema validation:** Always validate TOML against schemas for critical data
- **Error handling:** Implement proper error handling for TOML parsing failures
- **Data validation:** Validate TOML structure and content before processing

## 🐞 Troubleshooting
- **TOML parsing errors:** Check TOML syntax and validate structure
- **Memory issues:** Monitor TOML size and implement chunking for large data
- **Schema validation failures:** Review schema definitions and data structure
- **Performance problems:** Optimize TOML operations for large datasets
- **Encoding issues:** Ensure proper character encoding for TOML data

## 💡 Best Practices
- **Use schemas:** Always define and validate TOML schemas for critical data
- **Handle errors:** Implement proper error handling for TOML operations
- **Optimize size:** Minimize TOML size by removing unnecessary fields
- **Validate input:** Always validate TOML input before processing
- **Use proper formatting:** Use consistent formatting for readability
- **Document structure:** Document TOML structure and field meanings

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@yaml Function](059-at-yaml-function-bash.md)
- [@json Function](058-at-json-function-bash.md)
- [Configuration Management](115-configuration-management-bash.md)
- [Project Configuration](117-project-configuration-bash.md)

---

**Master @toml in TuskLang and wield the power of structured configuration in your projects. ⚙️** 
# TuskLang for AI: Complete Technical Reference

## Overview
TuskLang is a next-generation configuration language that combines flexible syntax with powerful runtime operators. This document provides comprehensive information for AI systems to understand and work with TuskLang.

## Core Concepts

### 1. Syntax Flexibility
TuskLang accepts multiple syntax styles within the same file:

```tsk
# INI-style with brackets
[section]
key = value

# JSON-style with braces
object {
    key: "value"
    nested { inner: true }
}

# XML-style with angle brackets
component >
    property: "value"
<

# Key-value supports both : and =
name: "value"
name = "value"
```

### 2. @ Operators System
The @ symbol invokes runtime operations that execute during parsing:

#### Core @ Operators:
```tsk
# Environment variables
port = @env("PORT", 8080)                    # With default
api_key = @env("API_KEY")                    # Required

# Database queries
users = @query("SELECT * FROM users")
count = @query("SELECT COUNT(*) FROM logs")

# Date/time operations
now = @date("Y-m-d H:i:s")
timestamp = @date("U")

# File operations
config = @file("config.json")               # Read file
content = @file.read("data.txt")
@file.write("output.txt", data)

# HTTP requests
response = @http.get("https://api.example.com")
data = @http.post(url, { body: payload })

# Caching
cached = @cache("5m", expensive_operation)   # Cache for 5 minutes

# JSON operations
parsed = @json.parse(raw_string)
stringified = @json.stringify(object)

# Machine learning
optimized = @optimize("key", default_value)
learned = @learn("pattern", training_data)
```

### 3. Variable System

#### Variable Scopes:
```tsk
# Global variables (start with $)
$global_api_key = "secret"

# Section-local variables
[database]
host = "localhost"
connection_string = "mysql://" + host

# Variable reference
[app]
db_host = database.host              # References section variable
api_key = $global_api_key           # References global variable
```

### 4. Value Types

```tsk
# Primitives
string = "text"
number = 42
float = 3.14
boolean = true
null_value = null

# Arrays
list = ["item1", "item2", "item3"]
numbers = [1, 2, 3]

# Objects
person = {
    name: "John"
    age: 30
}

# Multiline strings
description = """
This is a multiline
string value
"""
```

### 5. Conditional Logic

```tsk
# Ternary operator
port = is_production ? 443 : 8080

# Conditional with @ operators
workers = @env("CPU_COUNT") > 4 ? 8 : 4

# Complex conditions
cache_enabled = @env("NODE_ENV") == "production" && @env("CACHE_ENABLED") != "false"
```

## SDK Architecture

### Parser Implementation
Each SDK implements:
1. **Lexer**: Tokenizes .tsk files
2. **Parser**: Builds Abstract Syntax Tree
3. **Evaluator**: Executes @ operators
4. **Compiler**: Converts to .pnt binary format

### Binary Format (.pnt)
- MessagePack serialization
- 85% performance improvement
- Automatic compilation from .tsk
- Cross-language compatible

### CLI Commands
All SDKs provide these commands:
```bash
tusk parse <file>           # Parse and validate
tusk compile <file>         # Compile to .pnt
tusk run <file>            # Execute with @ operators
tusk validate <file>       # Syntax check
tusk format <file>         # Auto-format
tusk watch <file>          # Hot-reload on changes
```

## Advanced Features

### 1. Cross-File References
```tsk
# main.tsk
database = @file("database.tsk")
api_host = database.production.host

# Using peanut system
global_config = @peanut.get("globals.api_version")
```

### 2. FUJSEN (Functions in JSON)
```tsk
# Executable functions in config
validation = {
    isEmail: "(value) => /^[^@]+@[^@]+\.[^@]+$/.test(value)"
    minLength: "(value, min) => value.length >= min"
}
```

### 3. Hierarchical Configuration
```
/project
  ├── peanut.tsk          # Root config
  ├── src/
  │   └── peanut.tsk      # Inherits from root
  └── tests/
      └── peanut.tsk      # Overrides for tests
```

### 4. Type Validation
```tsk
[schema]
port = {
    type: "number"
    min: 1
    max: 65535
}

[config]
port = @env("PORT") | validate(schema.port)
```

## Language-Specific Features

### PHP
```php
$parser = new TuskLang\Parser();
$config = $parser->parse('config.tsk');
$value = $config->get('section.key');
```

### JavaScript
```javascript
import { TuskLang } from 'tusklang';
const config = await TuskLang.parse('config.tsk');
const value = config.get('section.key');
```

### Python
```python
from tusklang import TuskLang
config = TuskLang.parse('config.tsk')
value = config['section']['key']
```

### Go
```go
import "github.com/tuskphp/tusklang-go"
config, _ := tusklang.Parse("config.tsk")
value := config.Get("section.key")
```

## Best Practices

### 1. Organization
```tsk
# Group related settings
[database]
host = @env("DB_HOST", "localhost")
port = @env("DB_PORT", 5432)

[cache]
driver = "redis"
ttl = "5m"
```

### 2. Environment-Aware
```tsk
[app]
debug = @env("NODE_ENV") != "production"
log_level = debug ? "verbose" : "error"
```

### 3. Performance
```tsk
# Cache expensive operations
user_permissions = @cache("10m", @query("SELECT * FROM permissions"))

# Compile to .pnt for production
# tusk compile config.tsk
```

### 4. Security
```tsk
# Never hardcode secrets
api_key = @env("API_KEY")  # Required env var

# Use file permissions
secrets = @file("/secure/secrets.tsk", { mode: "0600" })
```

## Error Handling

### Parser Errors
- Syntax errors show line/column
- Type mismatches are caught early
- Missing @ operator handlers are reported

### Runtime Errors
- Failed @ operations return null by default
- Use fallbacks: `@env("KEY") || "default"`
- Validation with schemas

## Integration Examples

### Web Framework
```tsk
[server]
host = @env("HOST", "0.0.0.0")
port = @env("PORT", 3000)

[database]
url = @env("DATABASE_URL")
pool_size = @optimize("db_pool", 10)

[features]
enabled = @query("SELECT name FROM features WHERE active = 1")
```

### CI/CD Pipeline
```tsk
[build]
version = @env("CI_COMMIT_TAG", @date("Y.m.d"))
environment = @env("CI_ENVIRONMENT_NAME", "development")

[deploy]
replicas = environment == "production" ? 3 : 1
memory = @optimize("container_memory", "512Mi")
```

## Quick Reference

### File Extensions
- `.tsk` - Source files
- `.pnt` - Compiled binary
- `.peanuts` - Legacy config

### Reserved Keywords
- @ - Operator prefix
- $ - Global variable prefix
- true, false, null - Literals

### Escape Sequences
- `\"` - Quote in string
- `\\` - Backslash
- `\n` - Newline
- `\@` - Literal @

## SDK Installation

```bash
# PHP
composer require tusklang/tusklang

# JavaScript
npm install tusklang

# Python
pip install tusklang

# Ruby
gem install tusk_lang

# Rust
cargo add tusklang

# Go
go get github.com/tuskphp/tusklang-go

# Java
# Add to pom.xml or build.gradle

# C#
dotnet add package TuskLang.CSharp

# Bash
curl -sSL https://tusklang.org/install.sh | bash
```

## Summary
TuskLang revolutionizes configuration by combining flexible syntax with runtime intelligence through @ operators. It's designed for modern applications that need dynamic, environment-aware configuration with the power of a full programming language when needed.
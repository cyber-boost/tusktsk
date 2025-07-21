# 🏗️ TuskLang Bash Objects Guide

**"We don't bow to any king" – Objects are your configuration's backbone.**

Objects in TuskLang are structured data containers that bring organization and power to your configurations. Whether you're modeling complex systems, building nested configurations, or creating reusable components, TuskLang objects provide the structure you need.

## 🎯 What are Objects?
Objects are key-value collections that can contain:
- Simple values (strings, numbers, booleans)
- Nested objects
- Arrays
- Dynamic values from @ operators
- Mixed data types

## 📝 Syntax Styles
TuskLang supports multiple object syntaxes:

```ini
# INI-style
[server]
host: "web1.example.com"
port: 8080
ssl: true
```

```json
# JSON-like
{
  "database": {
    "host": "db.example.com",
    "port": 5432,
    "ssl": true
  }
}
```

```xml
# XML-inspired
<config>
  <api>
    <endpoint>https://api.example.com</endpoint>
    <timeout>30</timeout>
  </api>
</config>
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > objects-quickstart.tsk << 'EOF'
[app]
server: {
    host: "localhost",
    port: 8080,
    ssl: true
}
database: {
    host: "db.local",
    port: 5432,
    name: "myapp"
}
EOF

config=$(tusk_parse objects-quickstart.tsk)
echo "Server: $(tusk_get "$config" app.server.host):$(tusk_get "$config" app.server.port)"
echo "Database: $(tusk_get "$config" app.database.host):$(tusk_get "$config" app.database.port)"
```

## 🔗 Real-World Use Cases

### 1. Server Configuration
```ini
[infrastructure]
web_server: {
    host: "web1.prod",
    port: 80,
    ssl: true,
    workers: 4
}
```

### 2. User Profiles
```ini
[users]
admin: {
    name: "Alice",
    email: "alice@example.com",
    roles: ["admin", "user"],
    permissions: {
        read: true,
        write: true,
        delete: false
    }
}
```

### 3. Dynamic Objects
```ini
[monitoring]
system_info: {
    hostname: @shell("hostname"),
    uptime: @shell("uptime -p"),
    load: @shell("uptime | awk -F'load average:' '{print $2}'")
}
```

### 4. Object Processing in Bash
```bash
#!/bin/bash
source tusk-bash.sh

cat > objects-processing.tsk << 'EOF'
[deploy]
target: {
    name: "production",
    servers: ["web1", "web2"],
    config: {
        workers: 4,
        timeout: 30
    }
}
EOF

config=$(tusk_parse objects-processing.tsk)
target_name=$(tusk_get "$config" deploy.target.name)
workers=$(tusk_get "$config" deploy.target.config.workers)
echo "Deploying to $target_name with $workers workers"
```

## 🧠 Advanced Object Operations

### Nested Objects
```ini
[complex]
api: {
    v1: {
        endpoint: "/api/v1",
        methods: ["GET", "POST"],
        auth: {
            required: true,
            type: "bearer"
        }
    },
    v2: {
        endpoint: "/api/v2",
        methods: ["GET", "POST", "PUT", "DELETE"],
        auth: {
            required: true,
            type: "oauth2"
        }
    }
}
```

### Object Functions
```ini
[data]
user: {
    name: "Bob",
    age: 30,
    active: true
}
keys: @object.keys($user)
values: @object.values($user)
has_name: @object.has($user, "name")
```

### Dynamic Object Creation
```ini
[config]
environment: @env("APP_ENV", "development")
settings: @if($environment == "production", {
    debug: false,
    log_level: "error",
    cache: true
}, {
    debug: true,
    log_level: "debug",
    cache: false
})
```

### Object Merging
```ini
[base]
defaults: {
    timeout: 30,
    retries: 3,
    ssl: true
}

[override]
production: {
    timeout: 60,
    workers: 4
}

[final]
config: @object.merge($defaults, $production)
```

## 🛡️ Security & Performance Notes
- **Object depth:** Deeply nested objects can impact parsing performance; keep nesting reasonable.
- **Sensitive data:** Never store secrets directly in objects; use @env or @encrypt.
- **Validation:** Use @validate.object() to ensure object structure and required fields.

## 🐞 Troubleshooting
- **Access errors:** Use dot notation for nested access (e.g., `object.nested.key`).
- **Missing keys:** Check for key existence with @object.has() before accessing.
- **Type mismatches:** Objects can contain mixed types; ensure your Bash logic handles this.

## 💡 Best Practices
- Use descriptive object names that reflect the data structure.
- Group related configuration into logical objects.
- Document object structure and required fields.
- Use consistent naming conventions for object keys.
- Validate object structure with @validate.object().

## 🔗 Cross-References
- [Arrays](012-arrays-bash.md)
- [Key-Value Basics](007-key-value-basics-bash.md)
- [Object Operations](067-object-operations-bash.md)

---

**Master objects in TuskLang and structure your configurations like a pro. 🏗️** 
# 📋 TuskLang Bash Arrays Guide

**"We don't bow to any king" – Arrays are your data's playground.**

Arrays in TuskLang are powerful, flexible collections that integrate seamlessly with Bash. Whether you're managing lists of servers, processing configuration sets, or building dynamic automation, TuskLang arrays are your go-to for structured data.

## 🎯 What are Arrays?
Arrays are ordered collections of values. In TuskLang, arrays can contain:
- Strings, numbers, booleans
- Mixed types
- Nested arrays
- Dynamic values from @ operators

## 📝 Syntax Styles
TuskLang supports multiple array syntaxes:

```ini
# INI-style
servers: ["web1", "web2", "web3"]
```

```json
# JSON-like
{
  "ports": [80, 443, 8080],
  "features": ["ssl", "caching", "logging"]
}
```

```xml
# XML-inspired
<config>
  <admins>["alice", "bob", "charlie"]</admins>
</config>
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > arrays-quickstart.tsk << 'EOF'
[app]
servers: ["web1.example.com", "web2.example.com"]
ports: [80, 443, 8080]
features: ["ssl", "caching", "logging"]
EOF

config=$(tusk_parse arrays-quickstart.tsk)
echo "Servers: $(tusk_get "$config" app.servers)"
echo "Ports: $(tusk_get "$config" app.ports)"
echo "Features: $(tusk_get "$config" app.features)"
```

## 🔗 Real-World Use Cases

### 1. Server Lists
```ini
[infrastructure]
web_servers: ["web1.prod", "web2.prod", "web3.prod"]
db_servers: ["db1.prod", "db2.prod"]
```

### 2. Feature Flags
```ini
[features]
enabled: ["new_ui", "api_v2", "analytics"]
disabled: ["legacy_auth", "old_dashboard"]
```

### 3. Dynamic Arrays
```ini
[monitoring]
metrics: @shell("ls /var/log/*.log | head -5")
```

### 4. Array Processing in Bash
```bash
#!/bin/bash
source tusk-bash.sh

cat > arrays-processing.tsk << 'EOF'
[deploy]
targets: ["staging", "production"]
EOF

config=$(tusk_parse arrays-processing.tsk)
targets=$(tusk_get "$config" deploy.targets)

# Convert array string to Bash array
IFS=',' read -ra TARGET_ARRAY <<< "$targets"
for target in "${TARGET_ARRAY[@]}"; do
  echo "Deploying to: $target"
done
```

## 🧠 Advanced Array Operations

### Array Functions
```ini
[data]
numbers: [1, 2, 3, 4, 5]
sum: @array.sum($numbers)
count: @array.length($numbers)
max: @array.max($numbers)
min: @array.min($numbers)
```

### Array Filtering
```ini
[users]
all_users: ["alice", "bob", "charlie", "dave"]
admins: @array.filter($all_users, "contains('admin')")
```

### Array Mapping
```ini
[urls]
domains: ["example.com", "test.com"]
https_urls: @array.map($domains, "https://" + $item)
```

### Nested Arrays
```ini
[config]
server_groups: [
  ["web1", "web2"],
  ["db1", "db2"],
  ["cache1", "cache2"]
]
```

## 🛡️ Security & Performance Notes
- **Array size limits:** Large arrays can impact performance; consider pagination for massive datasets.
- **Input validation:** Always validate array contents, especially when using @shell or @env.
- **Memory usage:** Arrays are loaded into memory; be mindful of size in resource-constrained environments.

## 🐞 Troubleshooting
- **Array parsing errors:** Check for proper comma separation and balanced brackets.
- **Empty arrays:** Use `[]` for empty arrays, not `[""]` (which creates an array with one empty string).
- **Mixed types:** Arrays can contain mixed types, but ensure your Bash logic handles this correctly.

## 💡 Best Practices
- Use descriptive array names that indicate the collection's purpose.
- Prefer arrays over comma-separated strings for structured data.
- Use @array functions for common operations (sum, filter, map).
- Document array structure and expected contents.
- Validate array contents with @validate.array() where appropriate.

## 🔗 Cross-References
- [Objects](013-objects-bash.md)
- [String Operations](065-string-operations-bash.md)
- [Array Operations](066-array-operations-bash.md)

---

**Master arrays in TuskLang and organize your data like a pro. 📋** 
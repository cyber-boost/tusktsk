# 🔗 TuskLang Bash References Guide

**"We don't bow to any king" – References are your configuration's network.**

References in TuskLang allow you to create connections between values, build dynamic relationships, and avoid duplication. Whether you're linking related settings, creating computed values, or building complex configurations, references provide the flexibility to build interconnected, maintainable systems.

## 🎯 What are References?
References allow you to refer to other values within your configuration. In TuskLang, references use the `$` prefix and enable:
- **Value reuse** - Reference the same value multiple times
- **Computed values** - Build values from other values
- **Dynamic relationships** - Create interconnected configurations
- **Maintainability** - Change once, update everywhere

## 📝 Syntax Styles

### Basic References
```ini
[basic]
$base_url: "https://api.example.com"
$api_version: "v1"
full_url: "${base_url}/api/${api_version}"
```

### JSON-like References
```json
{
  "config": {
    "$protocol": "https",
    "$domain": "example.com",
    "url": "${protocol}://${domain}"
  }
}
```

### XML-inspired References
```xml
<config>
  <vars>
    <protocol>https</protocol>
    <domain>example.com</domain>
  </vars>
  <url>${protocol}://${domain}</url>
</config>
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > references-quickstart.tsk << 'EOF'
[app]
$app_name: "TuskApp"
$version: "2.1.0"
$environment: @env("APP_ENV", "development")

# Use references to build values
full_name: "${app_name} v${version}"
log_file: "/var/log/${app_name}.log"
config_path: "/etc/${app_name}/${environment}.tsk"

# Computed values using references
$base_port: 8000
$port_offset: 80
server_port: $base_port + $port_offset
EOF

config=$(tusk_parse references-quickstart.tsk)
echo "Full Name: $(tusk_get "$config" app.full_name)"
echo "Log File: $(tusk_get "$config" app.log_file)"
echo "Config Path: $(tusk_get "$config" app.config_path)"
echo "Server Port: $(tusk_get "$config" app.server_port)"
```

## 🔗 Real-World Use Cases

### 1. URL Construction
```ini
[api]
$protocol: "https"
$domain: "api.example.com"
$version: "v1"
base_url: "${protocol}://${domain}"
full_url: "${base_url}/api/${version}"
users_endpoint: "${full_url}/users"
posts_endpoint: "${full_url}/posts"
```

### 2. File Path Management
```ini
[paths]
$app_name: "myapp"
$environment: @env("APP_ENV", "development")
$base_dir: "/var/www"

# Build paths using references
app_dir: "${base_dir}/${app_name}"
config_dir: "${app_dir}/config"
log_dir: "${app_dir}/logs"
config_file: "${config_dir}/${environment}.tsk"
log_file: "${log_dir}/${app_name}.log"
```

### 3. Database Configuration
```ini
[database]
$host: @env("DB_HOST", "localhost")
$port: @env("DB_PORT", "5432")
$name: @env("DB_NAME", "myapp")
$user: @env("DB_USER", "postgres")

# Build connection strings
connection_string: "postgresql://${user}@${host}:${port}/${name}"
dsn: "pgsql:host=${host};port=${port};dbname=${name}"
```

### 4. Server Configuration
```ini
[server]
$host: "localhost"
$port: 8080
$ssl: true

# Computed values
protocol: @if($ssl, "https", "http")
url: "${protocol}://${host}:${port}"
ssl_cert: @if($ssl, "/etc/ssl/certs/server.crt", null)
ssl_key: @if($ssl, "/etc/ssl/private/server.key", null)
```

## 🧠 Advanced Reference Patterns

### Nested References
```ini
[complex]
$base: {
    "protocol": "https",
    "domain": "example.com",
    "port": 443
}

# Reference nested values
protocol: $base.protocol
domain: $base.domain
url: "${base.protocol}://${base.domain}:${base.port}"
```

### Conditional References
```ini
[conditional]
$environment: @env("APP_ENV", "development")
$dev_host: "localhost"
$prod_host: "0.0.0.0"

# Conditional references
host: @if($environment == "production", $prod_host, $dev_host)
port: @if($environment == "production", 80, 8080)
debug: @if($environment == "production", false, true)
```

### Circular Reference Prevention
```ini
[safe]
$base_url: "https://api.example.com"
$api_path: "/api/v1"

# Safe references (no circular dependencies)
users_url: "${base_url}${api_path}/users"
posts_url: "${base_url}${api_path}/posts"

# Avoid this pattern (circular reference):
# $url: "${base_url}${api_path}"
# $base_url: "${url}/base"
```

### Reference Chains
```ini
[chains]
$app_name: "TuskApp"
$version: "2.1.0"
$environment: @env("APP_ENV", "development")

# Build reference chains
$app_id: "${app_name}-${version}"
$log_prefix: "${app_id}-${environment}"
log_file: "/var/log/${log_prefix}.log"
error_log: "/var/log/${log_prefix}-error.log"
```

## 🛡️ Security & Performance Notes
- **Reference validation:** Ensure referenced values exist before using them.
- **Circular references:** Avoid circular reference patterns that can cause infinite loops.
- **Performance:** Deep reference chains can impact parsing performance.
- **Security:** Be careful with references that include user input or external data.

## 🐞 Troubleshooting
- **Undefined references:** Check that all referenced variables are defined before use.
- **Circular references:** Look for reference loops that can cause parsing errors.
- **Type mismatches:** Ensure referenced values have compatible types for operations.
- **Scope issues:** Verify references are accessible in the current scope.

## 💡 Best Practices
- **Define before use:** Always define referenced values before using them.
- **Use descriptive names:** Choose clear, descriptive names for referenced values.
- **Document relationships:** Document the relationships between referenced values.
- **Test references:** Test reference chains to ensure they work as expected.
- **Avoid deep nesting:** Keep reference chains reasonably shallow for maintainability.

## 🔗 Cross-References
- [Variables](020-variable-naming-bash.md)
- [Key-Value Basics](007-key-value-basics-bash.md)
- [Conditional Logic](060-conditional-logic-bash.md)

---

**Master references in TuskLang and build interconnected, maintainable configurations. 🔗** 
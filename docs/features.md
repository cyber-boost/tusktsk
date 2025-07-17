# TuskLang Features

## Overview

TuskLang provides a comprehensive set of features designed for modern configuration management. This document covers all major features with examples and use cases.

## 🔐 Security Features

### Digital Signatures

TuskLang supports digital signatures to ensure configuration integrity and authenticity.

#### Ed25519 Signatures (Recommended)
```python
from tusklang import TuskConfig, SecurityConfig

# Configure Ed25519 signatures
security = SecurityConfig(
    signature_algorithm='ed25519',
    require_signature=True
)

config = TuskConfig(security=security)
config.set("app.name", "Secure Application")
config.set("secrets.api_key", "sk-1234567890abcdef")

# Save with signature
config.save("signed-config.pnt")
```

#### RSA Signatures
```python
# Configure RSA signatures
security = SecurityConfig(
    signature_algorithm='rsa2048',
    require_signature=True
)

config = TuskConfig(security=security)
config.save("rsa-signed-config.pnt")
```

#### Signature Verification
```python
# Load and verify signature
config = TuskConfig.load("signed-config.pnt", verify=True)
print("Signature verified successfully!")
```

### Encryption

TuskLang provides multiple encryption algorithms for sensitive data protection.

#### AES-256-GCM Encryption
```python
from tusklang import TuskConfig, SecurityConfig

# Configure AES encryption
security = SecurityConfig(
    encryption_algorithm='aes256_gcm',
    require_encryption=True
)

config = TuskConfig(security=security)
config.set("database.password", "super-secret-password")
config.set("api.secret_key", "sk-abcdef123456")

# Save with encryption
config.save("encrypted-config.pnt", password="my-secret-password")
```

#### ChaCha20-Poly1305 Encryption
```python
# Configure ChaCha20 encryption
security = SecurityConfig(
    encryption_algorithm='chacha20_poly1305',
    require_encryption=True
)

config = TuskConfig(security=security)
config.save("chacha20-config.pnt", password="my-secret")
```

#### Decryption
```python
# Load encrypted configuration
config = TuskConfig.load("encrypted-config.pnt", password="my-secret-password")
password = config.get("database.password")
```

### Key Management

TuskLang provides comprehensive key management capabilities.

#### Automatic Key Generation
```bash
# Generate keys automatically
tusk keys generate

# List available keys
tusk keys list

# Export public key
tusk keys export --public --output public.pem
```

#### Manual Key Management
```python
from tusklang import KeyManager

# Generate new key pair
key_manager = KeyManager()
private_key, public_key = key_manager.generate_ed25519()

# Import existing keys
key_manager.import_private_key("private.pem")
key_manager.import_public_key("public.pem")
```

## 📊 Performance Features

### Compression

TuskLang supports multiple compression algorithms to reduce file sizes.

#### LZ4 Compression (Fast)
```python
from tusklang import TuskConfig, CompressionConfig

# Configure LZ4 compression
compression = CompressionConfig(algorithm='lz4', level=1)
config = TuskConfig(compression=compression)

# Large configuration data
for i in range(10000):
    config.set(f"data.item_{i}", f"value_{i}")

# Save with compression
config.save("compressed-config.pnt")
```

#### Zstandard Compression (Balanced)
```python
# Configure Zstandard compression
compression = CompressionConfig(algorithm='zstd', level=3)
config = TuskConfig(compression=compression)
config.save("zstd-config.pnt")
```

#### Gzip Compression (Maximum)
```python
# Configure Gzip compression
compression = CompressionConfig(algorithm='gzip', level=9)
config = TuskConfig(compression=compression)
config.save("gzip-config.pnt")
```

### Streaming Support

For large configurations, TuskLang provides streaming capabilities.

```python
from tusklang import StreamingConfig

# Load large configuration in chunks
config = StreamingConfig("large-config.pnt")

# Load only specific sections
config.load_section("app")
config.load_section("database")

# Process data in streams
for section in config.stream_sections():
    print(f"Processing section: {section.name}")
    for key, value in section.items():
        process_item(key, value)
```

### Caching

TuskLang includes intelligent caching for improved performance.

```python
from tusklang import TuskConfig, CacheConfig

# Configure caching
cache = CacheConfig(
    enabled=True,
    max_size=1000,
    ttl=300  # 5 minutes
)

config = TuskConfig(cache=cache)

# Frequently accessed values are cached
for _ in range(1000):
    value = config.get("app.name")  # Cached after first access
```

## 🔍 Validation Features

### Schema Validation

TuskLang provides powerful schema validation capabilities.

#### Basic Schema
```python
from tusklang import TuskConfig, Schema

# Define schema
schema = Schema({
    "app": {
        "name": {"type": "string", "required": True},
        "version": {"type": "string", "pattern": r"^\d+\.\d+\.\d+$"},
        "debug": {"type": "boolean", "default": False}
    },
    "database": {
        "host": {"type": "string", "required": True},
        "port": {"type": "integer", "min": 1, "max": 65535},
        "name": {"type": "string", "required": True}
    }
})

config = TuskConfig(schema=schema)

# This will validate against schema
config.set("app.name", "My App")
config.set("app.version", "1.0.0")
config.set("database.host", "localhost")
config.set("database.port", 5432)
config.set("database.name", "myapp")
```

#### Advanced Schema
```python
# Advanced schema with complex validation
schema = Schema({
    "api": {
        "endpoints": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "path": {"type": "string", "pattern": r"^/[a-zA-Z0-9/_-]+$"},
                    "method": {"type": "string", "enum": ["GET", "POST", "PUT", "DELETE"]},
                    "timeout": {"type": "integer", "min": 1, "max": 300}
                },
                "required": ["path", "method"]
            }
        }
    },
    "security": {
        "allowed_origins": {
            "type": "array",
            "items": {"type": "string", "format": "uri"}
        },
        "rate_limit": {
            "type": "object",
            "properties": {
                "requests_per_minute": {"type": "integer", "min": 1},
                "burst_size": {"type": "integer", "min": 1}
            }
        }
    }
})
```

### Custom Validators

TuskLang supports custom validation functions.

```python
from tusklang import TuskConfig, Schema, Validator

# Custom validator function
def validate_email(value):
    import re
    pattern = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
    if not re.match(pattern, value):
        raise ValueError(f"Invalid email format: {value}")
    return value

# Custom validator for port numbers
def validate_port(value):
    if not isinstance(value, int) or value < 1 or value > 65535:
        raise ValueError(f"Port must be between 1 and 65535: {value}")
    return value

# Schema with custom validators
schema = Schema({
    "admin": {
        "email": {"type": "string", "validator": validate_email},
        "port": {"type": "integer", "validator": validate_port}
    }
})

config = TuskConfig(schema=schema)
config.set("admin.email", "admin@example.com")
config.set("admin.port", 8080)
```

## 📝 Audit Logging

### Comprehensive Event Tracking

TuskLang provides detailed audit logging for security and compliance.

```python
from tusklang import AuditLogger, AuditConfig

# Configure audit logging
audit_config = AuditConfig(
    storage_type='database',
    storage_path='/var/log/tusklang/audit',
    retention_days=90,
    compression=True
)

audit_logger = AuditLogger(audit_config)

# Log configuration access
audit_logger.log_event(
    user_id='user123',
    session_id='session456',
    action='read',
    resource='/config/app.pnt',
    resource_type='file',
    resource_id='app.pnt',
    details={'section': 'database'},
    ip_address='192.168.1.100',
    user_agent='Mozilla/5.0...',
    success=True
)

# Log configuration modification
audit_logger.log_event(
    user_id='user123',
    session_id='session456',
    action='write',
    resource='/config/app.pnt',
    resource_type='file',
    resource_id='app.pnt',
    details={'changes': ['database.host', 'database.port']},
    ip_address='192.168.1.100',
    user_agent='Mozilla/5.0...',
    success=True
)
```

### Multiple Storage Backends

Audit logs can be stored in various backends.

#### File Storage
```python
audit_config = AuditConfig(
    storage_type='file',
    storage_path='/var/log/tusklang/audit',
    rotation_size='100MB',
    rotation_count=10
)
```

#### Database Storage
```python
audit_config = AuditConfig(
    storage_type='database',
    connection_string='sqlite:///audit.db',
    table_name='audit_logs'
)
```

#### Syslog Integration
```python
audit_config = AuditConfig(
    storage_type='syslog',
    facility='local0',
    tag='tusklang'
)
```

## 🔄 Integration Features

### Environment Variables

TuskLang can integrate with environment variables.

```python
from tusklang import TuskConfig

config = TuskConfig()

# Load from environment variables
config.load_from_env(prefix='APP_')

# Environment variables:
# APP_DATABASE_HOST=localhost
# APP_DATABASE_PORT=5432
# APP_API_KEY=secret123

# Access as normal
host = config.get("database.host")  # "localhost"
port = config.get("database.port")  # 5432
```

### Configuration Merging

TuskLang supports merging multiple configuration sources.

```python
from tusklang import TuskConfig

# Base configuration
base_config = TuskConfig()
base_config.set("app.name", "My App")
base_config.set("app.version", "1.0.0")

# Environment-specific configuration
env_config = TuskConfig()
env_config.set("database.host", "prod-db.example.com")
env_config.set("app.debug", False)

# Merge configurations
merged_config = base_config.merge(env_config)

# Result: app.name="My App", app.version="1.0.0", 
#         database.host="prod-db.example.com", app.debug=False
```

### Hot Reloading

TuskLang supports live configuration updates.

```python
from tusklang import TuskConfig, FileWatcher

config = TuskConfig.load("app.pnt")

# Set up file watcher
watcher = FileWatcher("app.pnt")

def on_config_change(event):
    print(f"Configuration changed: {event}")
    config.reload()
    print("Configuration reloaded")

watcher.on_change(on_config_change)
watcher.start()

# Configuration changes are automatically detected and reloaded
```

## 🌐 Cross-Platform Features

### Language Interoperability

TuskLang configurations can be shared between different programming languages.

```python
# Python: Create configuration
from tusklang import TuskConfig
config = TuskConfig()
config.set("app.name", "My App")
config.save("shared-config.pnt")
```

```go
// Go: Load same configuration
package main

import "github.com/tusklang/go-sdk"

func main() {
    config, err := tusklang.LoadConfig("shared-config.pnt")
    if err != nil {
        log.Fatal(err)
    }
    
    appName := config.GetString("app.name", "")
    fmt.Println("App name:", appName)
}
```

```rust
// Rust: Load same configuration
use tusklang::Config;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let config = Config::load("shared-config.pnt")?;
    let app_name: String = config.get("app.name")?;
    println!("App name: {}", app_name);
    Ok(())
}
```

### Platform-Specific Optimizations

TuskLang automatically optimizes for different platforms.

```python
from tusklang import TuskConfig, PlatformConfig

# Platform-specific configuration
platform_config = PlatformConfig()

# Windows optimizations
if platform_config.is_windows():
    config.set("paths.separator", "\\")
    config.set("encoding", "cp1252")

# Unix optimizations
elif platform_config.is_unix():
    config.set("paths.separator", "/")
    config.set("encoding", "utf-8")

# macOS optimizations
elif platform_config.is_macos():
    config.set("paths.separator", "/")
    config.set("encoding", "utf-8")
    config.set("keychain.enabled", True)
```

## 🚀 Advanced Features

### Plugin System

TuskLang supports plugins for extensibility.

```python
from tusklang import TuskConfig, Plugin

# Custom plugin
class EncryptionPlugin(Plugin):
    def __init__(self, key):
        self.key = key
    
    def pre_save(self, config, path):
        # Encrypt before saving
        encrypted_data = self.encrypt(config.to_dict())
        return encrypted_data
    
    def post_load(self, config, data):
        # Decrypt after loading
        decrypted_data = self.decrypt(data)
        config.from_dict(decrypted_data)

# Use plugin
config = TuskConfig()
config.add_plugin(EncryptionPlugin("my-secret-key"))
config.save("plugin-config.pnt")
```

### Template System

TuskLang supports configuration templates.

```python
from tusklang import TuskConfig, Template

# Create template
template = Template({
    "app": {
        "name": "{{ app_name }}",
        "version": "{{ version }}",
        "environment": "{{ env }}"
    },
    "database": {
        "host": "{{ db_host }}",
        "port": "{{ db_port }}",
        "name": "{{ db_name }}"
    }
})

# Render template
config_data = template.render({
    "app_name": "My Application",
    "version": "1.0.0",
    "env": "production",
    "db_host": "prod-db.example.com",
    "db_port": 5432,
    "db_name": "myapp_prod"
})

config = TuskConfig()
config.from_dict(config_data)
config.save("templated-config.pnt")
```

### Metrics and Monitoring

TuskLang provides built-in metrics for monitoring.

```python
from tusklang import TuskConfig, Metrics

# Enable metrics
metrics = Metrics(enabled=True)
config = TuskConfig(metrics=metrics)

# Metrics are automatically collected
config.set("app.name", "My App")
config.save("app.pnt")

# Access metrics
print(f"Operations: {metrics.get_operation_count()}")
print(f"Load time: {metrics.get_average_load_time()}ms")
print(f"Save time: {metrics.get_average_save_time()}ms")
print(f"Memory usage: {metrics.get_memory_usage()}MB")
```

---

*For more detailed examples and advanced usage patterns, see the [Examples Guide](examples.md)* 
# TuskLang API Reference

## Overview

TuskLang provides consistent APIs across multiple programming languages. This document covers the core API patterns and language-specific implementations.

## Core Concepts

### Configuration Object
The main interface for working with TuskLang configurations.

### Key-Value Storage
TuskLang uses dot notation for hierarchical key-value storage:
- `app.name` - Application name
- `database.host` - Database host
- `api.endpoints.users` - Nested configuration

### Security Features
- **Encryption**: AES-256-GCM, ChaCha20-Poly1305
- **Signatures**: Ed25519, RSA-2048
- **Validation**: Schema validation and constraints

## Python SDK

### Installation
```python
pip install tusklang
```

### Basic Usage
```python
from tusklang import TuskConfig

# Create configuration
config = TuskConfig()

# Set values
config.set("app.name", "My Application")
config.set("app.version", "1.0.0")
config.set("database.host", "localhost")
config.set("database.port", 5432)

# Get values
app_name = config.get("app.name")
db_port = config.get("database.port", default=5432)

# Save configuration
config.save("app.pnt")

# Load configuration
loaded_config = TuskConfig.load("app.pnt")
```

### Advanced Features

#### Schema Validation
```python
from tusklang import TuskConfig, Schema

schema = Schema({
    "app": {
        "name": {"type": "string", "required": True},
        "version": {"type": "string", "pattern": r"^\d+\.\d+\.\d+$"},
        "debug": {"type": "boolean", "default": False}
    },
    "database": {
        "host": {"type": "string", "required": True},
        "port": {"type": "integer", "min": 1, "max": 65535}
    }
})

config = TuskConfig(schema=schema)
config.set("app.name", "My App")
config.set("app.version", "1.0.0")
config.set("database.host", "localhost")
config.set("database.port", 5432)
```

#### Security Configuration
```python
from tusklang import TuskConfig, SecurityConfig

security = SecurityConfig(
    signature_algorithm='ed25519',
    encryption_algorithm='aes256_gcm',
    require_signature=True,
    require_encryption=True
)

config = TuskConfig(security=security)
config.set("secrets.api_key", "sk-1234567890abcdef")
config.save("secure-config.pnt", password="my-secret")
```

#### Audit Logging
```python
from tusklang import AuditLogger, AuditConfig

audit_config = AuditConfig(
    storage_type='database',
    storage_path='/var/log/tusklang/audit'
)

audit_logger = AuditLogger(audit_config)
audit_logger.log_event(
    user_id='user123',
    action='read',
    resource='/config/app.pnt',
    success=True
)
```

### API Reference

#### TuskConfig Class

##### Constructor
```python
TuskConfig(
    schema: Optional[Schema] = None,
    security: Optional[SecurityConfig] = None,
    audit_logger: Optional[AuditLogger] = None
)
```

##### Methods

###### set(key: str, value: Any) -> None
Set a configuration value.

```python
config.set("app.name", "My Application")
config.set("database.port", 5432)
config.set("features.enabled", True)
```

###### get(key: str, default: Any = None) -> Any
Get a configuration value.

```python
app_name = config.get("app.name")
db_port = config.get("database.port", default=5432)
debug_mode = config.get("app.debug", default=False)
```

###### has(key: str) -> bool
Check if a key exists.

```python
if config.has("database.password"):
    password = config.get("database.password")
```

###### delete(key: str) -> bool
Delete a configuration key.

```python
config.delete("old.setting")
```

###### keys(prefix: str = "") -> List[str]
Get all keys with optional prefix.

```python
all_keys = config.keys()
app_keys = config.keys("app.")
```

###### to_dict() -> Dict[str, Any]
Convert configuration to dictionary.

```python
config_dict = config.to_dict()
```

###### from_dict(data: Dict[str, Any]) -> None
Load configuration from dictionary.

```python
config.from_dict({
    "app": {"name": "My App", "version": "1.0.0"},
    "database": {"host": "localhost", "port": 5432}
})
```

###### save(path: str, **kwargs) -> None
Save configuration to file.

```python
# Basic save
config.save("app.pnt")

# Save with encryption
config.save("secure.pnt", encrypt=True, password="secret")

# Save with signature
config.save("signed.pnt", sign=True)
```

###### load(path: str, **kwargs) -> TuskConfig
Load configuration from file.

```python
# Basic load
config = TuskConfig.load("app.pnt")

# Load with password
config = TuskConfig.load("secure.pnt", password="secret")

# Load with verification
config = TuskConfig.load("signed.pnt", verify=True)
```

#### Schema Class

##### Constructor
```python
Schema(definition: Dict[str, Any])
```

##### Schema Definition
```python
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
```

##### Supported Types
- `string`: String values
- `integer`: Integer values
- `float`: Floating point values
- `boolean`: Boolean values
- `array`: Array values
- `object`: Object values

##### Validation Rules
- `required`: Key must be present
- `default`: Default value if not present
- `pattern`: Regex pattern for strings
- `min`/`max`: Numeric range validation
- `enum`: Allowed values list

#### SecurityConfig Class

##### Constructor
```python
SecurityConfig(
    signature_algorithm: str = 'ed25519',
    encryption_algorithm: str = 'aes256_gcm',
    key_derivation: str = 'pbkdf2',
    hash_algorithm: str = 'sha256',
    key_size: int = 256,
    iterations: int = 100000,
    require_signature: bool = False,
    require_encryption: bool = False
)
```

##### Supported Algorithms
- **Signature**: `ed25519`, `rsa2048`, `rsa4096`
- **Encryption**: `aes256_gcm`, `chacha20_poly1305`
- **Key Derivation**: `pbkdf2`, `argon2`
- **Hash**: `sha256`, `sha512`, `blake2b`

## Go SDK

### Installation
```go
go get github.com/tusklang/go-sdk
```

### Basic Usage
```go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go-sdk"
)

func main() {
    // Create configuration
    config := tusklang.NewConfig()

    // Set values
    config.Set("app.name", "My Application")
    config.Set("app.version", "1.0.0")
    config.Set("database.host", "localhost")
    config.Set("database.port", 5432)

    // Get values
    appName := config.Get("app.name")
    dbPort := config.GetInt("database.port", 5432)

    // Save configuration
    err := config.Save("app.pnt")
    if err != nil {
        log.Fatal(err)
    }

    // Load configuration
    loadedConfig, err := tusklang.LoadConfig("app.pnt")
    if err != nil {
        log.Fatal(err)
    }
}
```

### API Reference

#### Config Struct

##### Methods

###### Set(key string, value interface{}) error
Set a configuration value.

```go
err := config.Set("app.name", "My Application")
if err != nil {
    log.Fatal(err)
}
```

###### Get(key string) interface{}
Get a configuration value.

```go
appName := config.Get("app.name")
```

###### GetString(key string, defaultValue string) string
Get a string value with default.

```go
appName := config.GetString("app.name", "Default App")
```

###### GetInt(key string, defaultValue int) int
Get an integer value with default.

```go
port := config.GetInt("database.port", 5432)
```

###### GetBool(key string, defaultValue bool) bool
Get a boolean value with default.

```go
debug := config.GetBool("app.debug", false)
```

###### Has(key string) bool
Check if a key exists.

```go
if config.Has("database.password") {
    password := config.GetString("database.password", "")
}
```

###### Delete(key string) error
Delete a configuration key.

```go
err := config.Delete("old.setting")
```

###### Keys(prefix string) []string
Get all keys with optional prefix.

```go
allKeys := config.Keys("")
appKeys := config.Keys("app.")
```

###### ToMap() map[string]interface{}
Convert configuration to map.

```go
configMap := config.ToMap()
```

###### FromMap(data map[string]interface{}) error
Load configuration from map.

```go
data := map[string]interface{}{
    "app": map[string]interface{}{
        "name": "My App",
        "version": "1.0.0",
    },
    "database": map[string]interface{}{
        "host": "localhost",
        "port": 5432,
    },
}
err := config.FromMap(data)
```

###### Save(path string, options ...SaveOption) error
Save configuration to file.

```go
// Basic save
err := config.Save("app.pnt")

// Save with encryption
err := config.Save("secure.pnt", tusklang.WithEncryption("secret"))

// Save with signature
err := config.Save("signed.pnt", tusklang.WithSignature())
```

###### Load(path string, options ...LoadOption) (*Config, error)
Load configuration from file.

```go
// Basic load
config, err := tusklang.LoadConfig("app.pnt")

// Load with password
config, err := tusklang.LoadConfig("secure.pnt", tusklang.WithPassword("secret"))

// Load with verification
config, err := tusklang.LoadConfig("signed.pnt", tusklang.WithVerification())
```

## Rust SDK

### Installation
```toml
# Cargo.toml
[dependencies]
tusklang = "1.0.0"
```

### Basic Usage
```rust
use tusklang::{Config, ConfigBuilder};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Create configuration
    let mut config = Config::new();

    // Set values
    config.set("app.name", "My Application")?;
    config.set("app.version", "1.0.0")?;
    config.set("database.host", "localhost")?;
    config.set("database.port", 5432)?;

    // Get values
    let app_name: String = config.get("app.name")?;
    let db_port: i32 = config.get_or("database.port", 5432)?;

    // Save configuration
    config.save("app.pnt")?;

    // Load configuration
    let loaded_config = Config::load("app.pnt")?;

    Ok(())
}
```

### API Reference

#### Config Struct

##### Methods

###### set<K, V>(key: K, value: V) -> Result<(), Error>
Set a configuration value.

```rust
config.set("app.name", "My Application")?;
config.set("database.port", 5432)?;
```

###### get<T>(key: &str) -> Result<T, Error>
Get a configuration value.

```rust
let app_name: String = config.get("app.name")?;
let port: i32 = config.get("database.port")?;
```

###### get_or<T>(key: &str, default: T) -> Result<T, Error>
Get a value with default.

```rust
let port: i32 = config.get_or("database.port", 5432)?;
let debug: bool = config.get_or("app.debug", false)?;
```

###### has(key: &str) -> bool
Check if a key exists.

```rust
if config.has("database.password") {
    let password: String = config.get("database.password")?;
}
```

###### delete(key: &str) -> Result<(), Error>
Delete a configuration key.

```rust
config.delete("old.setting")?;
```

###### keys(&self, prefix: &str) -> Vec<String>
Get all keys with optional prefix.

```rust
let all_keys = config.keys("");
let app_keys = config.keys("app.");
```

###### to_map(&self) -> HashMap<String, Value>
Convert configuration to map.

```rust
let config_map = config.to_map();
```

###### from_map(data: HashMap<String, Value>) -> Result<(), Error>
Load configuration from map.

```rust
let mut data = HashMap::new();
data.insert("app.name".to_string(), Value::String("My App".to_string()));
config.from_map(data)?;
```

###### save(&self, path: &str) -> Result<(), Error>
Save configuration to file.

```rust
// Basic save
config.save("app.pnt")?;

// Save with encryption
config.save_with_encryption("secure.pnt", "secret")?;

// Save with signature
config.save_with_signature("signed.pnt")?;
```

###### load(path: &str) -> Result<Config, Error>
Load configuration from file.

```rust
// Basic load
let config = Config::load("app.pnt")?;

// Load with password
let config = Config::load_with_password("secure.pnt", "secret")?;

// Load with verification
let config = Config::load_with_verification("signed.pnt")?;
```

## JavaScript SDK

### Installation
```bash
npm install tusklang
```

### Basic Usage
```javascript
import { TuskConfig } from 'tusklang';

// Create configuration
const config = new TuskConfig();

// Set values
config.set("app.name", "My Application");
config.set("app.version", "1.0.0");
config.set("database.host", "localhost");
config.set("database.port", 5432);

// Get values
const appName = config.get("app.name");
const dbPort = config.get("database.port", 5432);

// Save configuration
await config.save("app.pnt");

// Load configuration
const loadedConfig = await TuskConfig.load("app.pnt");
```

### API Reference

#### TuskConfig Class

##### Methods

###### set(key: string, value: any): void
Set a configuration value.

```javascript
config.set("app.name", "My Application");
config.set("database.port", 5432);
```

###### get(key: string, defaultValue?: any): any
Get a configuration value.

```javascript
const appName = config.get("app.name");
const port = config.get("database.port", 5432);
```

###### has(key: string): boolean
Check if a key exists.

```javascript
if (config.has("database.password")) {
    const password = config.get("database.password");
}
```

###### delete(key: string): boolean
Delete a configuration key.

```javascript
config.delete("old.setting");
```

###### keys(prefix?: string): string[]
Get all keys with optional prefix.

```javascript
const allKeys = config.keys();
const appKeys = config.keys("app.");
```

###### toObject(): object
Convert configuration to object.

```javascript
const configObj = config.toObject();
```

###### fromObject(data: object): void
Load configuration from object.

```javascript
config.fromObject({
    app: { name: "My App", version: "1.0.0" },
    database: { host: "localhost", port: 5432 }
});
```

###### save(path: string, options?: SaveOptions): Promise<void>
Save configuration to file.

```javascript
// Basic save
await config.save("app.pnt");

// Save with encryption
await config.save("secure.pnt", { encrypt: true, password: "secret" });

// Save with signature
await config.save("signed.pnt", { sign: true });
```

###### load(path: string, options?: LoadOptions): Promise<TuskConfig>
Load configuration from file.

```javascript
// Basic load
const config = await TuskConfig.load("app.pnt");

// Load with password
const config = await TuskConfig.load("secure.pnt", { password: "secret" });

// Load with verification
const config = await TuskConfig.load("signed.pnt", { verify: true });
```

## Error Handling

### Common Errors

#### Configuration Errors
- `KeyNotFoundError`: Requested key doesn't exist
- `ValidationError`: Value doesn't match schema
- `TypeError`: Value type doesn't match expected type

#### File Errors
- `FileNotFoundError`: Configuration file doesn't exist
- `PermissionError`: Insufficient permissions
- `CorruptionError`: File is corrupted or invalid

#### Security Errors
- `EncryptionError`: Encryption/decryption failed
- `SignatureError`: Digital signature verification failed
- `KeyError`: Cryptographic key error

### Error Handling Examples

#### Python
```python
from tusklang import TuskConfig, KeyNotFoundError, ValidationError

try:
    config = TuskConfig.load("app.pnt")
    value = config.get("missing.key")
except KeyNotFoundError:
    print("Key not found")
except ValidationError as e:
    print(f"Validation error: {e}")
```

#### Go
```go
config, err := tusklang.LoadConfig("app.pnt")
if err != nil {
    switch {
    case errors.Is(err, tusklang.ErrKeyNotFound):
        log.Println("Key not found")
    case errors.Is(err, tusklang.ErrValidation):
        log.Println("Validation error")
    default:
        log.Fatal(err)
    }
}
```

#### Rust
```rust
match config.get::<String>("missing.key") {
    Ok(value) => println!("Value: {}", value),
    Err(Error::KeyNotFound(_)) => println!("Key not found"),
    Err(Error::Validation(_)) => println!("Validation error"),
    Err(e) => return Err(e),
}
```

## Performance Considerations

### Memory Usage
- TuskLang uses efficient data structures
- Large configurations are loaded incrementally
- Memory usage scales linearly with configuration size

### File I/O
- Binary format provides fast read/write operations
- Compression reduces file size by 75% on average
- Streaming support for large configurations

### Security Overhead
- Encryption adds ~10-20% overhead
- Digital signatures add ~5-10% overhead
- Performance impact scales with configuration size

## Best Practices

### Configuration Structure
- Use hierarchical keys for organization
- Keep related settings grouped together
- Use consistent naming conventions

### Security
- Always encrypt sensitive data
- Use strong passwords for encryption
- Verify digital signatures when loading
- Rotate keys regularly

### Performance
- Load configurations once and reuse
- Use schema validation for type safety
- Minimize configuration file size
- Use compression for large configurations

---

*For more examples and advanced usage, see the [Examples Guide](examples.md)* 
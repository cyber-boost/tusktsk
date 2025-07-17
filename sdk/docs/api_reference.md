# TuskLang SDK API Reference

Complete API documentation for the TuskLang SDK across all supported programming languages.

## Table of Contents

- [Core Protection API](#core-protection-api)
- [License Management API](#license-management-api)
- [Anti-Tampering API](#anti-tampering-api)
- [Usage Tracking API](#usage-tracking-api)
- [Authentication API](#authentication-api)
- [Multi-Language Support](#multi-language-support)
- [Error Handling](#error-handling)
- [Configuration](#configuration)

## Core Protection API

The core protection API provides fundamental security features including encryption, integrity verification, and license validation.

### Initialization

#### Python
```python
from tusk import initialize_protection, get_protection

# Initialize global protection instance
initialize_protection(license_key: str, api_key: str) -> TuskProtection

# Get global protection instance
get_protection() -> TuskProtection
```

#### JavaScript
```javascript
const { initializeProtection, getProtection } = require('tusklang-sdk');

// Initialize global protection instance
initializeProtection(licenseKey: string, apiKey: string): TuskProtection

// Get global protection instance
getProtection(): TuskProtection
```

#### Rust
```rust
use tusklang_sdk::{initialize_protection, get_protection};

// Initialize global protection instance
initialize_protection(license_key: String, api_key: String) -> TuskProtection

// Get global protection instance
get_protection() -> TuskProtection
```

### Core Methods

#### `validate_license() -> bool`

Validates the license key integrity and format.

**Returns:** `bool` - True if license is valid, false otherwise

**Example:**
```python
protection = get_protection()
if protection.validate_license():
    print("License is valid")
else:
    print("License is invalid")
```

#### `encrypt_data(data: str) -> str`

Encrypts sensitive data using AES-256-GCM encryption.

**Parameters:**
- `data` (str): Data to encrypt

**Returns:** `str` - Encrypted data

**Example:**
```python
protection = get_protection()
encrypted = protection.encrypt_data("sensitive information")
print(f"Encrypted: {encrypted}")
```

#### `decrypt_data(encrypted_data: str) -> str`

Decrypts previously encrypted data.

**Parameters:**
- `encrypted_data` (str): Data to decrypt

**Returns:** `str` - Decrypted data

**Example:**
```python
protection = get_protection()
decrypted = protection.decrypt_data(encrypted)
print(f"Decrypted: {decrypted}")
```

#### `verify_integrity(data: str, signature: str) -> bool`

Verifies data integrity using HMAC-SHA256 signatures.

**Parameters:**
- `data` (str): Data to verify
- `signature` (str): Expected signature

**Returns:** `bool` - True if integrity is verified, false otherwise

**Example:**
```python
protection = get_protection()
signature = protection.generate_signature("important data")
if protection.verify_integrity("important data", signature):
    print("Data integrity verified")
```

#### `generate_signature(data: str) -> str`

Generates HMAC-SHA256 signature for data.

**Parameters:**
- `data` (str): Data to sign

**Returns:** `str` - Generated signature

**Example:**
```python
protection = get_protection()
signature = protection.generate_signature("data to sign")
print(f"Signature: {signature}")
```

#### `track_usage(operation: str, success: bool)`

Tracks usage metrics for operations.

**Parameters:**
- `operation` (str): Operation name
- `success` (bool): Whether operation succeeded

**Example:**
```python
protection = get_protection()
protection.track_usage("api_call", True)
protection.track_usage("feature_used", True)
```

#### `get_metrics() -> dict`

Returns usage metrics and statistics.

**Returns:** `dict` - Metrics including api_calls, errors, session_id, uptime

**Example:**
```python
protection = get_protection()
metrics = protection.get_metrics()
print(f"API calls: {metrics['api_calls']}")
print(f"Errors: {metrics['errors']}")
print(f"Uptime: {metrics['uptime']} seconds")
```

#### `obfuscate_code(code: str) -> str`

Obfuscates code for protection.

**Parameters:**
- `code` (str): Code to obfuscate

**Returns:** `str` - Obfuscated code

**Example:**
```python
protection = get_protection()
obfuscated = protection.obfuscate_code("print('Hello World')")
```

#### `detect_tampering() -> bool`

Detects if the SDK has been tampered with.

**Returns:** `bool` - True if tampering detected, false otherwise

**Example:**
```python
protection = get_protection()
if protection.detect_tampering():
    print("Tampering detected!")
```

#### `report_violation(violation_type: str, details: str) -> dict`

Reports security violations.

**Parameters:**
- `violation_type` (str): Type of violation
- `details` (str): Violation details

**Returns:** `dict` - Violation report

**Example:**
```python
protection = get_protection()
violation = protection.report_violation(
    "unauthorized_access",
    "Invalid API key used"
)
```

## License Management API

The license management API provides comprehensive license validation, expiration checking, and permission management.

### Initialization

#### Python
```python
from tusk import initialize_license, get_license

# Initialize global license instance
initialize_license(license_key: str, api_key: str) -> TuskLicense

# Get global license instance
get_license() -> TuskLicense
```

### Core Methods

#### `validate_license_key() -> dict`

Validates license key format and checksum.

**Returns:** `dict` - Validation result with valid flag and error message

**Example:**
```python
license = get_license()
result = license.validate_license_key()
if result["valid"]:
    print("License key is valid")
else:
    print(f"License key error: {result['error']}")
```

#### `verify_license_server(server_url: str = None) -> dict`

Verifies license with remote server.

**Parameters:**
- `server_url` (str, optional): Custom server URL

**Returns:** `dict` - Server verification result

**Example:**
```python
license = get_license()
result = license.verify_license_server()
if result.get("valid"):
    print("Server verification successful")
```

#### `check_license_expiration() -> dict`

Checks if license is expired or expiring soon.

**Returns:** `dict` - Expiration information

**Example:**
```python
license = get_license()
expiration = license.check_license_expiration()
if expiration["expired"]:
    print("License has expired")
else:
    print(f"Days remaining: {expiration['days_remaining']}")
```

#### `validate_license_permissions(feature: str) -> dict`

Validates if license allows specific feature.

**Parameters:**
- `feature` (str): Feature to check

**Returns:** `dict` - Permission validation result

**Example:**
```python
license = get_license()
result = license.validate_license_permissions("premium")
if result["allowed"]:
    print("Premium feature available")
else:
    print("Premium license required")
```

#### `get_license_info() -> dict`

Returns comprehensive license information.

**Returns:** `dict` - License information including validation, expiration, cache status

**Example:**
```python
license = get_license()
info = license.get_license_info()
print(f"License key: {info['license_key']}")
print(f"Validation: {info['validation']['valid']}")
print(f"Expiration: {info['expiration']['expired']}")
```

## Anti-Tampering API

The anti-tampering API provides code obfuscation, integrity checks, and tamper detection.

### Initialization

#### Python
```python
from tusk import initialize_anti_tamper, get_anti_tamper

# Initialize global anti-tamper instance
initialize_anti_tamper(secret_key: str) -> TuskAntiTamper

# Get global anti-tamper instance
get_anti_tamper() -> TuskAntiTamper
```

### Core Methods

#### `calculate_file_hash(file_path: str) -> str`

Calculates SHA256 hash of file.

**Parameters:**
- `file_path` (str): Path to file

**Returns:** `str` - File hash

**Example:**
```python
anti_tamper = get_anti_tamper()
file_hash = anti_tamper.calculate_file_hash("/path/to/file.py")
print(f"File hash: {file_hash}")
```

#### `verify_file_integrity(file_path: str, expected_hash: str) -> bool`

Verifies file integrity against expected hash.

**Parameters:**
- `file_path` (str): Path to file
- `expected_hash` (str): Expected file hash

**Returns:** `bool` - True if integrity verified, false otherwise

**Example:**
```python
anti_tamper = get_anti_tamper()
if anti_tamper.verify_file_integrity("/path/to/file.py", expected_hash):
    print("File integrity verified")
```

#### `obfuscate_code(code: str, level: int = 2) -> str`

Obfuscates code with specified level.

**Parameters:**
- `code` (str): Code to obfuscate
- `level` (int): Obfuscation level (0-3)

**Returns:** `str` - Obfuscated code

**Example:**
```python
anti_tamper = get_anti_tamper()
obfuscated = anti_tamper.obfuscate_code("def hello(): print('world')", level=3)
```

#### `deobfuscate_code(obfuscated_code: str) -> str`

Deobfuscates previously obfuscated code.

**Parameters:**
- `obfuscated_code` (str): Obfuscated code

**Returns:** `str` - Deobfuscated code

**Example:**
```python
anti_tamper = get_anti_tamper()
original = anti_tamper.deobfuscate_code(obfuscated_code)
```

#### `protect_function(func: callable, obfuscation_level: int = 2) -> callable`

Protects a function with anti-tampering.

**Parameters:**
- `func` (callable): Function to protect
- `obfuscation_level` (int): Obfuscation level

**Returns:** `callable` - Protected function

**Example:**
```python
anti_tamper = get_anti_tamper()

@anti_tamper.protect_function("sensitive_function", 2)
def sensitive_function():
    return "sensitive data"

# Function is now protected
result = sensitive_function()
```

#### `self_check() -> bool`

Performs self-integrity check.

**Returns:** `bool` - True if self-check passes, false otherwise

**Example:**
```python
anti_tamper = get_anti_tamper()
if anti_tamper.self_check():
    print("Self-check passed")
else:
    print("Self-check failed")
```

#### `detect_tampering() -> dict`

Detects various types of tampering.

**Returns:** `dict` - Tampering detection results

**Example:**
```python
anti_tamper = get_anti_tamper()
tampering = anti_tamper.detect_tampering()
if tampering["file_tampering"]:
    print("File tampering detected")
if tampering["debugger_detected"]:
    print("Debugger detected")
```

#### `get_tamper_detections() -> list`

Returns list of tampering detections.

**Returns:** `list` - Tampering detection history

**Example:**
```python
anti_tamper = get_anti_tamper()
detections = anti_tamper.get_tamper_detections()
for detection in detections:
    print(f"Detection: {detection['type']} at {detection['timestamp']}")
```

## Usage Tracking API

The usage tracking API provides silent analytics and compliance monitoring.

### Initialization

#### Python
```python
from tusk import initialize_usage_tracker, get_usage_tracker

# Initialize global usage tracker instance
initialize_usage_tracker(api_key: str, endpoint: str = None) -> TuskUsageTracker

# Get global usage tracker instance
get_usage_tracker() -> TuskUsageTracker
```

### Core Methods

#### `track_event(event_type: str, event_data: dict, user_id: str = None)`

Tracks a usage event.

**Parameters:**
- `event_type` (str): Type of event
- `event_data` (dict): Event data
- `user_id` (str, optional): User identifier

**Example:**
```python
tracker = get_usage_tracker()
tracker.track_event("user_login", {"method": "oauth"}, "user123")
tracker.track_event("feature_used", {"feature": "premium"}, "user123")
```

#### `track_api_call(endpoint: str, method: str, status_code: int, duration: float)`

Tracks API call metrics.

**Parameters:**
- `endpoint` (str): API endpoint
- `method` (str): HTTP method
- `status_code` (int): HTTP status code
- `duration` (float): Request duration in seconds

**Example:**
```python
tracker = get_usage_tracker()
tracker.track_api_call("/api/data", "GET", 200, 0.15)
tracker.track_api_call("/api/users", "POST", 201, 0.25)
```

#### `track_error(error_type: str, error_message: str, stack_trace: str = None)`

Tracks error occurrences.

**Parameters:**
- `error_type` (str): Type of error
- `error_message` (str): Error message
- `stack_trace` (str, optional): Stack trace

**Example:**
```python
tracker = get_usage_tracker()
tracker.track_error("validation_error", "Invalid input", "Traceback...")
```

#### `track_feature_usage(feature: str, success: bool, metadata: dict = None)`

Tracks feature usage.

**Parameters:**
- `feature` (str): Feature name
- `success` (bool): Whether feature usage succeeded
- `metadata` (dict, optional): Additional metadata

**Example:**
```python
tracker = get_usage_tracker()
tracker.track_feature_usage("premium_feature", True, {"plan": "enterprise"})
```

#### `track_performance(operation: str, duration: float, memory_usage: float = None)`

Tracks performance metrics.

**Parameters:**
- `operation` (str): Operation name
- `duration` (float): Operation duration in seconds
- `memory_usage` (float, optional): Memory usage in bytes

**Example:**
```python
tracker = get_usage_tracker()
tracker.track_performance("data_processing", 1.5, 1024000)
```

#### `track_security_event(event_type: str, severity: str, details: dict)`

Tracks security events.

**Parameters:**
- `event_type` (str): Security event type
- `severity` (str): Event severity (low, medium, high, critical)
- `details` (dict): Event details

**Example:**
```python
tracker = get_usage_tracker()
tracker.track_security_event("unauthorized_access", "high", {"ip": "192.168.1.1"})
```

#### `get_usage_summary() -> dict`

Returns usage summary and statistics.

**Returns:** `dict` - Usage summary

**Example:**
```python
tracker = get_usage_tracker()
summary = tracker.get_usage_summary()
print(f"Total events: {summary['total_events']}")
print(f"API calls: {summary['api_calls']}")
print(f"Errors: {summary['errors']}")
print(f"Unique users: {summary['unique_users']}")
```

#### `get_events_by_type(event_type: str, limit: int = 100) -> list`

Returns events of specific type.

**Parameters:**
- `event_type` (str): Event type to filter
- `limit` (int): Maximum number of events to return

**Returns:** `list` - Filtered events

**Example:**
```python
tracker = get_usage_tracker()
api_events = tracker.get_events_by_type("api_call", 50)
for event in api_events:
    print(f"API call: {event.event_data['endpoint']}")
```

#### `get_user_events(user_id: str, limit: int = 100) -> list`

Returns events for specific user.

**Parameters:**
- `user_id` (str): User identifier
- `limit` (int): Maximum number of events to return

**Returns:** `list` - User events

**Example:**
```python
tracker = get_usage_tracker()
user_events = tracker.get_user_events("user123", 50)
for event in user_events:
    print(f"Event: {event.event_type}")
```

#### `flush_events()`

Manually flushes events to server.

**Example:**
```python
tracker = get_usage_tracker()
tracker.flush_events()
```

#### `set_enabled(enabled: bool)`

Enables or disables usage tracking.

**Parameters:**
- `enabled` (bool): Whether to enable tracking

**Example:**
```python
tracker = get_usage_tracker()
tracker.set_enabled(False)  # Disable tracking
```

## Authentication API

The authentication API provides API key management and secure token generation.

### Initialization

#### Python
```python
from tusk import initialize_auth, get_auth

# Initialize global auth instance
initialize_auth(master_key: str) -> TuskAuth

# Get global auth instance
get_auth() -> TuskAuth
```

### Core Methods

#### `generate_api_key(user_id: str, permissions: list, expires_in: int = None) -> str`

Generates a new API key.

**Parameters:**
- `user_id` (str): User identifier
- `permissions` (list): List of permissions
- `expires_in` (int, optional): Expiration time in seconds

**Returns:** `str` - Generated API key

**Example:**
```python
auth = get_auth()
api_key = auth.generate_api_key("user123", ["read", "write"], 86400)
print(f"API Key: {api_key}")
```

#### `validate_api_key(api_key: str) -> dict`

Validates an API key.

**Parameters:**
- `api_key` (str): API key to validate

**Returns:** `dict` - Validation result with user_id and permissions

**Example:**
```python
auth = get_auth()
result = auth.validate_api_key(api_key)
if result:
    print(f"User: {result['user_id']}")
    print(f"Permissions: {result['permissions']}")
else:
    print("Invalid API key")
```

#### `revoke_api_key(key_id: str) -> bool`

Revokes an API key.

**Parameters:**
- `key_id` (str): Key identifier

**Returns:** `bool` - True if revoked successfully

**Example:**
```python
auth = get_auth()
if auth.revoke_api_key("key123"):
    print("API key revoked")
```

#### `generate_auth_token(user_id: str, permissions: list, expires_in: int = None) -> str`

Generates an authentication token.

**Parameters:**
- `user_id` (str): User identifier
- `permissions` (list): List of permissions
- `expires_in` (int, optional): Expiration time in seconds

**Returns:** `str` - Generated token

**Example:**
```python
auth = get_auth()
token = auth.generate_auth_token("user123", ["read", "write"], 3600)
print(f"Token: {token}")
```

#### `validate_auth_token(token: str) -> dict`

Validates an authentication token.

**Parameters:**
- `token` (str): Token to validate

**Returns:** `dict` - Validation result with user_id and permissions

**Example:**
```python
auth = get_auth()
result = auth.validate_auth_token(token)
if result:
    print(f"User: {result['user_id']}")
    print(f"Permissions: {result['permissions']}")
else:
    print("Invalid token")
```

#### `revoke_auth_token(token: str) -> bool`

Revokes an authentication token.

**Parameters:**
- `token` (str): Token to revoke

**Returns:** `bool` - True if revoked successfully

**Example:**
```python
auth = get_auth()
if auth.revoke_auth_token(token):
    print("Token revoked")
```

#### `check_permission(user_id: str, permission: str, auth_data: dict) -> bool`

Checks if user has specific permission.

**Parameters:**
- `user_id` (str): User identifier
- `permission` (str): Permission to check
- `auth_data` (dict): Authentication data

**Returns:** `bool` - True if user has permission

**Example:**
```python
auth = get_auth()
auth_data = {"permissions": ["read", "write"]}
if auth.check_permission("user123", "write", auth_data):
    print("User has write permission")
```

#### `encrypt_sensitive_data(data: str) -> str`

Encrypts sensitive data.

**Parameters:**
- `data` (str): Data to encrypt

**Returns:** `str` - Encrypted data

**Example:**
```python
auth = get_auth()
encrypted = auth.encrypt_sensitive_data("sensitive information")
```

#### `decrypt_sensitive_data(encrypted_data: str) -> str`

Decrypts sensitive data.

**Parameters:**
- `encrypted_data` (str): Data to decrypt

**Returns:** `str` - Decrypted data

**Example:**
```python
auth = get_auth()
decrypted = auth.decrypt_sensitive_data(encrypted)
```

#### `rotate_keys() -> list`

Rotates expired keys.

**Returns:** `list` - List of rotated key IDs

**Example:**
```python
auth = get_auth()
rotated = auth.rotate_keys()
print(f"Rotated {len(rotated)} keys")
```

#### `get_auth_stats() -> dict`

Returns authentication statistics.

**Returns:** `dict` - Authentication statistics

**Example:**
```python
auth = get_auth()
stats = auth.get_auth_stats()
print(f"Active API keys: {stats['active_api_keys']}")
print(f"Active tokens: {stats['active_tokens']}")
```

## Multi-Language Support

The TuskLang SDK provides consistent APIs across all supported languages. Here are the language-specific variations:

### API Naming Conventions

| Language | Method Naming | Example |
|----------|---------------|---------|
| Python | snake_case | `validate_license()` |
| JavaScript | camelCase | `validateLicense()` |
| Rust | snake_case | `validate_license()` |
| Go | PascalCase | `ValidateLicense()` |
| Java | camelCase | `validateLicense()` |
| C# | PascalCase | `ValidateLicense()` |
| Ruby | snake_case | `validate_license()` |
| PHP | camelCase | `validateLicense()` |
| Bash | snake_case with prefix | `tusk_validate_license()` |

### Language-Specific Examples

#### Go
```go
package main

import "github.com/tusklang/sdk"

func main() {
    // Initialize protection
    protection := sdk.InitializeProtection("license-key", "api-key")
    
    // Validate license
    if protection.ValidateLicense() {
        fmt.Println("License valid!")
    }
}
```

#### Java
```java
import org.tusklang.sdk.TuskProtection;

public class Main {
    public static void main(String[] args) {
        // Initialize protection
        TuskProtection protection = TuskProtection.initializeProtection("license-key", "api-key");
        
        // Validate license
        if (protection.validateLicense()) {
            System.out.println("License valid!");
        }
    }
}
```

#### C#
```csharp
using TuskLang.SDK;

class Program {
    static void Main() {
        // Initialize protection
        var protection = TuskProtection.InitializeProtection("license-key", "api-key");
        
        // Validate license
        if (protection.ValidateLicense()) {
            Console.WriteLine("License valid!");
        }
    }
}
```

## Error Handling

The TuskLang SDK provides comprehensive error handling across all languages.

### Common Error Types

- **LicenseError**: License validation failures
- **EncryptionError**: Encryption/decryption failures
- **IntegrityError**: Data integrity verification failures
- **TamperingError**: Tampering detection errors
- **AuthenticationError**: Authentication failures
- **NetworkError**: Network communication errors

### Error Handling Examples

#### Python
```python
from tusk import TuskProtectionError, TuskLicenseError

try:
    protection = get_protection()
    if not protection.validate_license():
        raise TuskLicenseError("Invalid license")
except TuskLicenseError as e:
    print(f"License error: {e}")
except TuskProtectionError as e:
    print(f"Protection error: {e}")
```

#### JavaScript
```javascript
const { TuskProtectionError, TuskLicenseError } = require('tusklang-sdk');

try {
    const protection = getProtection();
    if (!protection.validateLicense()) {
        throw new TuskLicenseError('Invalid license');
    }
} catch (error) {
    if (error instanceof TuskLicenseError) {
        console.log(`License error: ${error.message}`);
    } else if (error instanceof TuskProtectionError) {
        console.log(`Protection error: ${error.message}`);
    }
}
```

## Configuration

The TuskLang SDK supports various configuration options.

### Environment Variables

```bash
# Required
TUSK_LICENSE_KEY=your-license-key
TUSK_API_KEY=your-api-key

# Optional
TUSK_ENDPOINT=https://api.tusklang.org/v1
TUSK_LOG_LEVEL=info
TUSK_ENABLE_TRACKING=true
TUSK_BATCH_SIZE=50
TUSK_FLUSH_INTERVAL=300
```

### Configuration Files

#### YAML Configuration
```yaml
# tusklang.yaml
license:
  key: your-license-key
  api_key: your-api-key
  endpoint: https://api.tusklang.org/v1

protection:
  enable_obfuscation: true
  obfuscation_level: 2
  self_check_interval: 300

tracking:
  enabled: true
  batch_size: 50
  flush_interval: 300

auth:
  master_key: your-master-key
  token_expiry: 3600
  key_expiry: 2592000
```

#### JSON Configuration
```json
{
  "license": {
    "key": "your-license-key",
    "api_key": "your-api-key",
    "endpoint": "https://api.tusklang.org/v1"
  },
  "protection": {
    "enable_obfuscation": true,
    "obfuscation_level": 2,
    "self_check_interval": 300
  },
  "tracking": {
    "enabled": true,
    "batch_size": 50,
    "flush_interval": 300
  },
  "auth": {
    "master_key": "your-master-key",
    "token_expiry": 3600,
    "key_expiry": 2592000
  }
}
```

---

For more detailed information, see the [Security Guide](security_guide.md) and [Examples](examples.md). 
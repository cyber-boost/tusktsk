# 🔐 TuskLang Bash @encrypt Function Guide

**"We don't bow to any king" – Encryption is your configuration's fortress.**

The @encrypt function in TuskLang is your security fortress, enabling robust encryption, decryption, and cryptographic operations directly within your configuration files. Whether you're securing sensitive data, implementing secure communication, or protecting configuration secrets, @encrypt provides the cryptographic strength and flexibility to keep your data safe.

## 🎯 What is @encrypt?
The @encrypt function provides cryptographic operations in TuskLang. It offers:
- **Data encryption** - Encrypt sensitive data with various algorithms
- **Data decryption** - Decrypt encrypted data securely
- **Key management** - Generate and manage encryption keys
- **Hash functions** - Create secure hashes and checksums
- **Digital signatures** - Sign and verify data integrity

## 📝 Basic @encrypt Syntax

### Simple Encryption
```ini
[simple_encryption]
# Basic encryption operations
encrypted_data: @encrypt("sensitive_data", "AES-256-GCM")
encrypted_password: @encrypt(@env("PASSWORD"), "AES-256-CBC")
encrypted_config: @encrypt($config_data, "ChaCha20-Poly1305")
```

### Key Management
```ini
[key_management]
# Generate encryption keys
encryption_key: @encrypt.generate_key("AES-256")
public_key: @encrypt.generate_keypair("RSA-2048").public
private_key: @encrypt.generate_keypair("RSA-2048").private

# Use existing keys
$master_key: @env("MASTER_KEY")
encrypted_with_master: @encrypt("data", "AES-256-GCM", $master_key)
```

### Hash Functions
```ini
[hash_functions]
# Create secure hashes
password_hash: @encrypt.hash("user_password", "bcrypt")
file_hash: @encrypt.hash(@file.read("/path/to/file"), "SHA-256")
data_hash: @encrypt.hash($sensitive_data, "SHA-512")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > encrypt-quickstart.tsk << 'EOF'
[basic_encryption]
# Basic encryption
sensitive_data: "Hello, TuskLang!"
encrypted_data: @encrypt($sensitive_data, "AES-256-GCM")
decrypted_data: @encrypt.decrypt($encrypted_data, "AES-256-GCM")

[key_management]
# Generate keys
encryption_key: @encrypt.generate_key("AES-256")
keypair: @encrypt.generate_keypair("RSA-2048")

[hash_operations]
# Hash functions
password: "my_secure_password"
password_hash: @encrypt.hash($password, "bcrypt")
data_hash: @encrypt.hash("Hello, World!", "SHA-256")

[secure_storage]
# Secure data storage
$config_data: {"api_key": "secret123", "database_url": "mysql://user:pass@localhost/db"}
encrypted_config: @encrypt($config_data, "AES-256-GCM")
config_hash: @encrypt.hash($config_data, "SHA-256")
EOF

config=$(tusk_parse encrypt-quickstart.tsk)

echo "=== Basic Encryption ==="
echo "Original Data: $(tusk_get "$config" basic_encryption.sensitive_data)"
echo "Encrypted Data: $(tusk_get "$config" basic_encryption.encrypted_data)"
echo "Decrypted Data: $(tusk_get "$config" basic_encryption.decrypted_data)"

echo ""
echo "=== Key Management ==="
echo "Encryption Key: $(tusk_get "$config" key_management.encryption_key)"
echo "Keypair: $(tusk_get "$config" key_management.keypair)"

echo ""
echo "=== Hash Operations ==="
echo "Password Hash: $(tusk_get "$config" hash_operations.password_hash)"
echo "Data Hash: $(tusk_get "$config" hash_operations.data_hash)"

echo ""
echo "=== Secure Storage ==="
echo "Encrypted Config: $(tusk_get "$config" secure_storage.encrypted_config)"
echo "Config Hash: $(tusk_get "$config" secure_storage.config_hash)"
```

## 🔗 Real-World Use Cases

### 1. Secure Configuration Management
```ini
[secure_config]
# Encrypt sensitive configuration data
$sensitive_config: {
    "database_password": @env("DB_PASSWORD"),
    "api_secret": @env("API_SECRET"),
    "ssl_private_key": @env("SSL_PRIVATE_KEY"),
    "encryption_key": @env("ENCRYPTION_KEY")
}

# Encrypt entire configuration
encrypted_config: @encrypt($sensitive_config, "AES-256-GCM")

# Store encrypted config securely
@file.write("/etc/app/encrypted-config.enc", $encrypted_config, {"mode": "0600"})

# Decrypt when needed
decrypted_config: @encrypt.decrypt(@file.read("/etc/app/encrypted-config.enc"), "AES-256-GCM")

# Verify configuration integrity
config_hash: @encrypt.hash($sensitive_config, "SHA-256")
stored_hash: @encrypt.hash($decrypted_config, "SHA-256")
config_valid: $config_hash == $stored_hash
```

### 2. Password and Credential Management
```ini
[credential_management]
# Secure password storage
$user_credentials: {
    "admin_password": @encrypt.hash("admin123", "bcrypt"),
    "api_key": @encrypt.hash(@env("API_KEY"), "SHA-256"),
    "database_password": @encrypt.hash(@env("DB_PASSWORD"), "bcrypt")
}

# Encrypt credentials for storage
encrypted_credentials: @encrypt($user_credentials, "AES-256-GCM")

# Verify password
$input_password: @env("INPUT_PASSWORD")
password_verified: @encrypt.verify($input_password, $user_credentials.admin_password, "bcrypt")

# Secure credential rotation
$new_credentials: {
    "admin_password": @encrypt.hash("new_admin123", "bcrypt"),
    "api_key": @encrypt.hash(@env("NEW_API_KEY"), "SHA-256")
}

# Rotate credentials securely
@if($password_verified, {
    "action": "rotate_credentials",
    "new_credentials": @encrypt($new_credentials, "AES-256-GCM"),
    "timestamp": @date("Y-m-d H:i:s")
}, "access_denied")
```

### 3. Secure Communication and API Security
```ini
[api_security]
# API request signing
$api_request: {
    "method": "POST",
    "endpoint": "/api/users",
    "data": {"name": "John", "email": "john@example.com"},
    "timestamp": @date("U")
}

# Sign API request
$private_key: @env("API_PRIVATE_KEY")
request_signature: @encrypt.sign($api_request, "RSA-SHA256", $private_key)

# Verify API response
$api_response: @http("POST", "https://api.example.com/users", {
    "headers": {
        "Authorization": "Bearer " + @env("API_TOKEN"),
        "X-Signature": $request_signature,
        "Content-Type": "application/json"
    },
    "body": $api_request
})

response_verified: @encrypt.verify_signature($api_response.body, $api_response.headers.X-Signature, "RSA-SHA256", @env("API_PUBLIC_KEY"))
```

### 4. File and Data Security
```ini
[file_security]
# Encrypt sensitive files
$sensitive_files: ["/etc/app/secrets.conf", "/var/log/secure.log", "/tmp/sensitive-data.txt"]

# Encrypt each file
@array.for_each($sensitive_files, {
    "file": item,
    "encrypted": @encrypt(@file.read(item), "AES-256-GCM"),
    "hash": @encrypt.hash(@file.read(item), "SHA-256")
}, {
    "action": "encrypt_file",
    "source": item.file,
    "destination": item.file + ".enc",
    "encrypted_data": item.encrypted,
    "file_hash": item.hash
})

# Secure backup with encryption
$backup_data: {
    "database_dump": @shell("mysqldump --all-databases"),
    "config_files": @file.read("/etc/app/config.tsk"),
    "timestamp": @date("Y-m-d H:i:s")
}

encrypted_backup: @encrypt($backup_data, "AES-256-GCM")
backup_hash: @encrypt.hash($backup_data, "SHA-256")

# Store encrypted backup
@file.write("/var/backups/secure-backup-" + @date("Y-m-d-H-i-s") + ".enc", $encrypted_backup)
@file.write("/var/backups/backup-hash-" + @date("Y-m-d-H-i-s") + ".txt", $backup_hash)
```

## 🧠 Advanced @encrypt Patterns

### Multi-Layer Encryption
```ini
[multi_layer_encryption]
# Implement multi-layer encryption
$sensitive_data: "Ultra-sensitive information"

# Layer 1: AES-256-GCM
layer1_encrypted: @encrypt($sensitive_data, "AES-256-GCM")

# Layer 2: ChaCha20-Poly1305
layer2_encrypted: @encrypt($layer1_encrypted, "ChaCha20-Poly1305")

# Layer 3: RSA encryption
$rsa_keypair: @encrypt.generate_keypair("RSA-4096")
final_encrypted: @encrypt($layer2_encrypted, "RSA-OAEP", $rsa_keypair.public)

# Decryption process (reverse order)
layer3_decrypted: @encrypt.decrypt($final_encrypted, "RSA-OAEP", $rsa_keypair.private)
layer2_decrypted: @encrypt.decrypt($layer3_decrypted, "ChaCha20-Poly1305")
layer1_decrypted: @encrypt.decrypt($layer2_decrypted, "AES-256-GCM")
```

### Key Rotation and Management
```ini
[key_rotation]
# Implement key rotation system
$key_rotation_config: {
    "rotation_interval": "30d",
    "key_lifetime": "90d",
    "algorithm": "AES-256-GCM"
}

# Generate new keys
$current_keys: {
    "primary": @encrypt.generate_key("AES-256"),
    "secondary": @encrypt.generate_key("AES-256"),
    "backup": @encrypt.generate_key("AES-256")
}

# Encrypt with multiple keys for rotation
$data_to_encrypt: "Sensitive data requiring key rotation"
encrypted_with_primary: @encrypt($data_to_encrypt, "AES-256-GCM", $current_keys.primary)
encrypted_with_secondary: @encrypt($data_to_encrypt, "AES-256-GCM", $current_keys.secondary)

# Key rotation logic
$key_age: @date.diff(@date("Y-m-d"), @env("KEY_CREATED_DATE"))
rotation_needed: @validate.greater_than($key_age, 30)

@if($rotation_needed, {
    "action": "rotate_keys",
    "new_primary": @encrypt.generate_key("AES-256"),
    "new_secondary": $current_keys.primary,
    "new_backup": $current_keys.secondary,
    "rotation_date": @date("Y-m-d H:i:s")
}, "keys_current")
```

### Secure Random Generation
```ini
[secure_random]
# Generate secure random data
$random_data: {
    "session_token": @encrypt.random_bytes(32),
    "api_key": @encrypt.random_bytes(64),
    "encryption_iv": @encrypt.random_bytes(16),
    "salt": @encrypt.random_bytes(32)
}

# Generate cryptographically secure tokens
$secure_tokens: {
    "jwt_secret": @encrypt.random_bytes(64),
    "csrf_token": @encrypt.random_bytes(32),
    "refresh_token": @encrypt.random_bytes(128)
}

# Use random data for encryption
$data: "Data to encrypt with random IV"
$iv: @encrypt.random_bytes(16)
encrypted_with_iv: @encrypt($data, "AES-256-CBC", @env("ENCRYPTION_KEY"), $iv)
```

## 🛡️ Security & Performance Notes
- **Key management:** Store encryption keys securely and rotate regularly
- **Algorithm selection:** Use strong, modern encryption algorithms
- **Performance impact:** Consider performance implications of encryption operations
- **Key storage:** Never store encryption keys in plain text
- **Random generation:** Use cryptographically secure random number generators
- **Key rotation:** Implement regular key rotation policies

## 🐞 Troubleshooting
- **Key not found:** Ensure encryption keys are properly stored and accessible
- **Decryption failures:** Verify encryption/decryption algorithms match
- **Performance issues:** Optimize encryption operations for large datasets
- **Key corruption:** Implement key backup and recovery procedures
- **Algorithm compatibility:** Ensure encryption algorithms are supported

## 💡 Best Practices
- **Use strong algorithms:** Prefer AES-256-GCM, ChaCha20-Poly1305, RSA-2048+
- **Secure key storage:** Store keys in secure key management systems
- **Regular rotation:** Implement automatic key rotation policies
- **Audit encryption:** Log and monitor encryption operations
- **Test thoroughly:** Test encryption/decryption in staging environments
- **Document procedures:** Document encryption procedures and key management

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@env Function](026-at-env-function-bash.md)
- [@file Function](038-at-file-function-bash.md)
- [Security Best Practices](099-security-best-practices-bash.md)
- [Key Management](101-key-management-bash.md)

---

**Master @encrypt in TuskLang and build an impenetrable security fortress. 🔐** 
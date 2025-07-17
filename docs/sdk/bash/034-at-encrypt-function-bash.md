# 🔒 TuskLang Bash @encrypt Function Guide

**"We don't bow to any king" – Encryption is your configuration's shield.**

The @encrypt function in TuskLang is your security powerhouse, enabling robust encryption of sensitive data, secrets, and configuration values directly within your configuration files. Whether you're protecting API keys, encrypting database credentials, or securing user data, @encrypt provides the cryptographic strength and flexibility to keep your configurations safe.

## 🎯 What is @encrypt?
The @encrypt function provides encryption capabilities in TuskLang. It offers:
- **Symmetric encryption** - AES, ChaCha20, and more
- **Asymmetric encryption** - RSA, ECC (where supported)
- **Configurable algorithms** - Choose your cipher and mode
- **Key management** - Use environment variables, files, or KMS
- **Seamless integration** - Encrypt/decrypt on the fly in config

## 📝 Basic @encrypt Syntax

### Simple Encryption
```ini
[simple_encryption]
# Encrypt a value with a passphrase
api_key_encrypted: @encrypt("my-very-secret-api-key", "AES-256-GCM", "$ENCRYPTION_KEY")
db_password_encrypted: @encrypt("supersecretpassword", "AES-256-CBC", @env("DB_ENCRYPTION_KEY"))
```

### Decryption
```ini
[decryption]
# Decrypt a value
api_key: @decrypt($api_key_encrypted, "AES-256-GCM", "$ENCRYPTION_KEY")
db_password: @decrypt($db_password_encrypted, "AES-256-CBC", @env("DB_ENCRYPTION_KEY"))
```

### File Encryption
```ini
[file_encryption]
# Encrypt a file's contents
config_file_encrypted: @encrypt(@file.read("/etc/app/config.tsk"), "AES-256-GCM", @env("CONFIG_KEY"))

# Decrypt file contents
decrypted_config: @decrypt($config_file_encrypted, "AES-256-GCM", @env("CONFIG_KEY"))
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > encrypt-quickstart.tsk << 'EOF'
[encryption_demo]
# Encrypt and decrypt secrets
$encryption_key: @env("ENCRYPTION_KEY", "defaultkey1234567890")

secret_message: "The eagle flies at midnight."
encrypted_message: @encrypt($secret_message, "AES-256-GCM", $encryption_key)
decrypted_message: @decrypt($encrypted_message, "AES-256-GCM", $encryption_key)

# Encrypt API key
api_key: @env("API_KEY", "my-api-key-123")
api_key_encrypted: @encrypt($api_key, "AES-256-GCM", $encryption_key)
api_key_decrypted: @decrypt($api_key_encrypted, "AES-256-GCM", $encryption_key)
EOF

config=$(tusk_parse encrypt-quickstart.tsk)

echo "=== Encryption Demo ==="
echo "Secret Message: $(tusk_get "$config" encryption_demo.secret_message)"
echo "Encrypted Message: $(tusk_get "$config" encryption_demo.encrypted_message)"
echo "Decrypted Message: $(tusk_get "$config" encryption_demo.decrypted_message)"

echo ""
echo "API Key: $(tusk_get "$config" encryption_demo.api_key)"
echo "API Key (Encrypted): $(tusk_get "$config" encryption_demo.api_key_encrypted)"
echo "API Key (Decrypted): $(tusk_get "$config" encryption_demo.api_key_decrypted)"
```

## 🔗 Real-World Use Cases

### 1. Secure API Key and Secret Management
```ini
[api_secrets]
# Encrypt API keys and secrets
$api_key: @env.secure("API_KEY")
$encryption_key: @env("ENCRYPTION_KEY")

api_key_encrypted: @encrypt($api_key, "AES-256-GCM", $encryption_key)
api_key_decrypted: @decrypt($api_key_encrypted, "AES-256-GCM", $encryption_key)

# Use encrypted API key in HTTP requests
api_response: @http("GET", "https://api.example.com/data", {
    "headers": {
        "Authorization": @string.format("Bearer {key}", {"key": $api_key_decrypted})
    }
})
```

### 2. Encrypted Database Credentials
```ini
[database_credentials]
# Encrypt and decrypt database credentials
$db_user: @env("DB_USER", "admin")
$db_password: @env.secure("DB_PASSWORD")
$db_encryption_key: @env("DB_ENCRYPTION_KEY")

db_password_encrypted: @encrypt($db_password, "AES-256-GCM", $db_encryption_key)
db_password_decrypted: @decrypt($db_password_encrypted, "AES-256-GCM", $db_encryption_key)

# Build connection string
connection_string: @string.format("postgresql://{user}:{password}@localhost:5432/appdb", {
    "user": $db_user,
    "password": $db_password_decrypted
})
```

### 3. Secure File and Configuration Storage
```ini
[file_security]
# Encrypt configuration files
$config_contents: @file.read("/etc/app/config.tsk")
$config_key: @env("CONFIG_KEY")

config_encrypted: @encrypt($config_contents, "AES-256-GCM", $config_key)
config_decrypted: @decrypt($config_encrypted, "AES-256-GCM", $config_key)

# Store encrypted config to file
@file.write("/etc/app/config.tsk.enc", $config_encrypted)

# Read and decrypt config
$encrypted_config: @file.read("/etc/app/config.tsk.enc")
config_plain: @decrypt($encrypted_config, "AES-256-GCM", $config_key)
```

### 4. Encrypted Environment Variables
```ini
[env_encryption]
# Encrypt and decrypt environment variables
$raw_secret: @env("RAW_SECRET", "supersecret")
$env_key: @env("ENV_KEY", "envkey9876543210")

encrypted_secret: @encrypt($raw_secret, "AES-256-GCM", $env_key)
decrypted_secret: @decrypt($encrypted_secret, "AES-256-GCM", $env_key)

# Use decrypted secret in application
app_secret: $decrypted_secret
```

## 🧠 Advanced @encrypt Patterns

### Asymmetric Encryption (RSA)
```ini
[asymmetric_encryption]
# Encrypt with RSA public key
$public_key: @file.read("/etc/keys/public.pem")
$private_key: @file.read("/etc/keys/private.pem")

message: "Confidential data for recipient."
encrypted_message: @encrypt($message, "RSA-OAEP", $public_key)
decrypted_message: @decrypt($encrypted_message, "RSA-OAEP", $private_key)
```

### Hybrid Encryption
```ini
[hybrid_encryption]
# Hybrid encryption: encrypt data with symmetric key, encrypt key with public key
$data: "Sensitive payload"
$symmetric_key: @crypto.random_bytes(32)
$public_key: @file.read("/etc/keys/public.pem")
$private_key: @file.read("/etc/keys/private.pem")

# Encrypt data
encrypted_data: @encrypt($data, "AES-256-GCM", $symmetric_key)
# Encrypt symmetric key
encrypted_key: @encrypt($symmetric_key, "RSA-OAEP", $public_key)

# Decrypt symmetric key
decrypted_key: @decrypt($encrypted_key, "RSA-OAEP", $private_key)
# Decrypt data
decrypted_data: @decrypt($encrypted_data, "AES-256-GCM", $decrypted_key)
```

### Key Management with KMS
```ini
[kms_integration]
# Use a cloud KMS for key management
$kms_key: @kms.get_key("projects/my-project/locations/global/keyRings/my-keyring/cryptoKeys/my-key")

# Encrypt and decrypt with KMS key
secret_data: "Cloud-managed secret"
encrypted_data: @encrypt($secret_data, "AES-256-GCM", $kms_key)
decrypted_data: @decrypt($encrypted_data, "AES-256-GCM", $kms_key)
```

## 🛡️ Security & Performance Notes
- **Key management:** Store encryption keys securely (env vars, KMS, files with strict permissions)
- **Algorithm selection:** Use strong, modern algorithms (AES-256-GCM, RSA-OAEP)
- **Never hardcode secrets:** Avoid hardcoding keys or secrets in config files
- **Access control:** Restrict access to encrypted files and keys
- **Performance:** Symmetric encryption is fast; asymmetric is slower but more secure for key exchange
- **Audit:** Regularly audit encrypted data and key usage

## 🐞 Troubleshooting
- **Decryption errors:** Ensure correct key, algorithm, and mode are used
- **Key mismatch:** Use the same key for encryption and decryption (or correct key pair for asymmetric)
- **Corrupted data:** Validate encrypted data integrity (use authenticated modes like GCM)
- **Permission issues:** Check file and key permissions
- **Algorithm support:** Ensure your environment supports the chosen algorithm

## 💡 Best Practices
- **Rotate keys regularly:** Change encryption keys periodically
- **Use environment variables or KMS:** Never store keys in plaintext
- **Prefer authenticated encryption:** Use GCM or CCM modes for integrity
- **Document encryption flows:** Clearly document what is encrypted and how
- **Test decryption:** Always verify that encrypted data can be decrypted
- **Limit access:** Restrict who/what can access keys and encrypted data

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@env.secure Function](026-at-env-function-bash.md)
- [File Operations](041-at-file-function-bash.md)
- [Security Best Practices](099-security-best-practices-bash.md)

---

**Master @encrypt in TuskLang and protect your configurations with world-class security. 🔒** 
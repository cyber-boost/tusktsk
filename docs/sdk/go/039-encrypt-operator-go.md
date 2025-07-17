# @encrypt Operator in TuskLang - Go Guide

## 🔐 **Crypto Power: @encrypt Operator Unleashed**

TuskLang's `@encrypt` operator is your security superweapon. We don't bow to any king—especially not to plaintext secrets. Here's how to use `@encrypt` in Go projects to secure sensitive data with military-grade encryption.

## 📋 **Table of Contents**
- [What is @encrypt?](#what-is-encrypt)
- [Basic Usage](#basic-usage)
- [Encryption Algorithms](#encryption-algorithms)
- [Key Management](#key-management)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🛡️ **What is @encrypt?**

The `@encrypt` operator encrypts and decrypts sensitive data directly in your config. No more plaintext secrets—just pure, encrypted security.

## 🛠️ **Basic Usage**

```go
[secrets]
api_key: @encrypt("supersecret", "AES-256-GCM")
password: @encrypt("mypassword", "ChaCha20-Poly1305")
token: @encrypt("jwt_token", "AES-256-CBC")
```

## 🔐 **Encryption Algorithms**

### **AES-256-GCM**
```go
[secure]
sensitive_data: @encrypt("secret_value", "AES-256-GCM")
```

### **ChaCha20-Poly1305**
```go
[modern]
password: @encrypt("user_password", "ChaCha20-Poly1305")
```

### **AES-256-CBC**
```go
[legacy]
token: @encrypt("api_token", "AES-256-CBC")
```

## 🔑 **Key Management**

```go
[keys]
encryption_key: @env.secure("ENCRYPTION_KEY")
master_key: @file.secure("keys/master.key")
```

## 🔗 **Go Integration**

```go
apiKey := config.GetString("api_key") // Automatically decrypted
password := config.GetString("password") // Automatically decrypted
```

### **Manual Encryption**
```go
import "crypto/aes"
import "crypto/cipher"

key := []byte("32-byte-key-for-aes-256")
plaintext := []byte("secret data")

block, err := aes.NewCipher(key)
if err != nil {
    log.Fatal(err)
}

gcm, err := cipher.NewGCM(block)
if err != nil {
    log.Fatal(err)
}

nonce := make([]byte, gcm.NonceSize())
ciphertext := gcm.Seal(nonce, nonce, plaintext, nil)
```

## 🥇 **Best Practices**
- Use AES-256-GCM for new applications
- Store encryption keys securely
- Rotate keys regularly
- Never log encrypted data
- Use hardware security modules when possible

---

**TuskLang: Military-grade encryption with @encrypt.** 
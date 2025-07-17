# 🔒 Security Best Practices in TuskLang Ruby SDK

**"We don't bow to any king" – Security is your shield.**

TuskLang for Ruby empowers you to build secure, auditable, and robust configurations. Whether you're working in Rails, Jekyll, or DevOps, these best practices will help you protect your secrets, validate your logic, and defend your systems from misconfiguration and attack.

---

## 🛡️ Why Security Matters in TuskLang
- **Dynamic configs**: Logic and operators can introduce risk if not controlled
- **Secrets management**: API keys, credentials, and tokens often live in config
- **Cross-environment safety**: Prevent leaks between dev, test, and prod

---

## 🔑 Core Security Features

### 1. Secure Environment Variables
Use `@env.secure` to fetch secrets safely:

```ini
[api]
api_key: @env.secure("API_KEY")
```

**Ruby Usage:**
```ruby
require 'tusk_lang'
config = TuskLang::TSK.from_file('config.tsk')
api_key = config.get_value('api', 'api_key')
```

### 2. Encryption with @encrypt
Encrypt sensitive values in config:

```ini
[secrets]
enc_password: @encrypt("supersecret", "AES-256-GCM")
```

### 3. Validation and Type Safety
Validate required fields and types:

```ini
[validation]
require_api_key: @validate.required(["api_key"])
```

### 4. Access Control
Restrict config logic by environment:

```ini
[access]
admin_only: @if(@env("USER_ROLE") == "admin", "allowed", "denied")
```

---

## 🚂 Rails & Jekyll Integration

### Rails: Secure Config Loading
- Never commit secrets to source control
- Use ENV variables and @env.secure in TSK
- Integrate with Rails credentials for extra safety

```ruby
api_key = Rails.application.credentials.dig(:api, :key)
```

### Jekyll: Safe Static Builds
- Never expose secrets in public TSK files
- Use @env.secure for all sensitive data

---

## 🧩 Advanced Security Patterns

### 1. Secret Rotation
Rotate secrets without redeploying:

```ini
[rotation]
current_key: @env.secure("API_KEY_CURRENT")
next_key: @env.secure("API_KEY_NEXT")
```

### 2. Audit Logging
Log access to sensitive config values:

```ini
[audit]
log_access: @metrics("config_access", @date("U"))
```

### 3. FUJSEN Sandboxing
Restrict what embedded JS can access:

```ini
[fujsen]
safe_fujsen: """
function safe(data) {
  // No network, no file system
  return data.id;
}
"""
```

---

## 🚨 Troubleshooting
- **Secrets in logs?** Audit all debug output, never print secrets
- **Config leaks?** Use @env.secure and never hardcode sensitive values
- **FUJSEN exploits?** Sandbox all embedded JS

---

## ⚡ Security & Performance Notes
- **Security**: Always use @env.secure and @encrypt for secrets
- **Performance**: Encryption adds overhead—use only where needed
- **Best Practice**: Regularly audit configs and rotate secrets

---

## 🏆 Best Practices
- Never commit secrets to source control
- Use @env.secure everywhere
- Validate all required fields
- Sandbox FUJSEN logic
- Document your security patterns

---

**Master security in TuskLang Ruby and defend your kingdom. 🔒** 
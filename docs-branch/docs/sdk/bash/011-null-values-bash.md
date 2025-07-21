# 🕳️ TuskLang Bash Null Values Guide

**"We don't bow to any king" – Null is not nothing, it's a choice.**

Null values are essential for expressing absence, defaults, and optionality in TuskLang. Whether you’re handling missing data, resetting configuration, or building robust Bash automations, TuskLang’s null system is explicit, safe, and powerful.

## 🎯 What is Null?
A null value represents the intentional absence of a value. In TuskLang, `null` is a first-class citizen:
- Used for unset or missing configuration
- Signals "no value" or "reset to default"
- Enables safe fallback and conditional logic

## 📝 Syntax Styles
TuskLang supports several ways to express null:

```ini
# INI-style
api_key: null
```

```json
# JSON-like
{
  "token": null
}
```

```xml
# XML-inspired
<settings>
  <secret>null</secret>
</settings>
```

You can also use empty values (interpreted as null in some contexts):

```ini
username:
password: ""
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > nulls-quickstart.tsk << 'EOF'
[auth]
api_key: null
refresh_token:
user_email: ""
EOF

config=$(tusk_parse nulls-quickstart.tsk)
echo "API Key: $(tusk_get "$config" auth.api_key)"
echo "Refresh Token: $(tusk_get "$config" auth.refresh_token)"
echo "User Email: '$(tusk_get "$config" auth.user_email)'"
```

## 🔗 Real-World Use Cases

### 1. Optional Configuration
```ini
[database]
password: @env("DB_PASSWORD", null)
```

### 2. Resetting Values
```ini
[cache]
last_cleared: null
```

### 3. Fallback Logic
```ini
[api]
endpoint: @env("API_URL", null)
url: @if($endpoint == null, "https://default.example.com", $endpoint)
```

### 4. Null in Bash Logic
```bash
#!/bin/bash
source tusk-bash.sh

cat > nulls-bash.tsk << 'EOF'
[ops]
backup_path: null
EOF

config=$(tusk_parse nulls-bash.tsk)
if [[ -z $(tusk_get "$config" ops.backup_path) || $(tusk_get "$config" ops.backup_path) == "null" ]]; then
  echo "No backup path set. Using default."
else
  echo "Backup path: $(tusk_get "$config" ops.backup_path)"
fi
```

## 🧠 Advanced Null Handling

### Null Coalescing
```ini
[settings]
log_dir: @env("LOG_DIR", null) ?? "/var/log/app"
```

### Conditional Defaults
```ini
[service]
timeout: @if($custom_timeout == null, 30, $custom_timeout)
```

### Null in Arrays/Objects
```ini
[users]
admins: ["alice", null, "bob"]
```

## 🛡️ Security & Performance Notes
- **Never use null for secrets in production.** Always require explicit values for sensitive data.
- **Null as a signal:** Use null to trigger resets or fallbacks, but document this behavior.
- **Performance:** Null checks are fast; use them for safe defaults and error handling.

## 🐞 Troubleshooting
- **Unexpected "null" strings:** If you see the literal string `"null"`, check for quotes or type mismatches.
- **Empty vs null:** `""` (empty string) and `null` are different; use `null` for absence, `""` for blank.
- **Null propagation:** Chained nulls can propagate; use coalescing (`??`) to provide safe defaults.

## 💡 Best Practices
- Use `null` for missing/optional values, not for errors.
- Always check for null before using a value.
- Document when null triggers fallback or reset logic.
- Use `@validate.required(["key"])` to enforce non-null where needed.
- Prefer explicit null over empty string for clarity.

## 🔗 Cross-References
- [Key-Value Basics](007-key-value-basics-bash.md)
- [Conditional Logic](060-conditional-logic-bash.md)
- [Validation](023-best-practices-bash.md)

---

**Master null handling in TuskLang and build robust, fault-tolerant Bash automations. 🕳️** 
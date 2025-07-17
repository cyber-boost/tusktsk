# 🟢 TuskLang Bash Booleans Guide

**"We don't bow to any king" – Boolean logic, the TuskLang way.**

Booleans are the backbone of conditional logic in TuskLang. Whether you’re toggling features, controlling flow, or enforcing security, TuskLang’s boolean system is powerful, flexible, and integrates seamlessly with Bash workflows.

## 🎯 What is a Boolean?
A boolean is a value that is either `true` or `false`. In TuskLang, booleans can be expressed in multiple ways, and are used for:
- Feature flags
- Conditional configuration
- Security checks
- Environment toggles
- System health monitoring

## 📝 Syntax Styles
TuskLang supports several boolean representations:

```ini
# INI-style
feature_enabled: true
maintenance_mode: false
```

```json
# JSON-like
{
  "debug": true,
  "production": false
}
```

```xml
# XML-inspired
<settings>
  <logging>true</logging>
  <readonly>false</readonly>
</settings>
```

You can also use `yes`/`no`, `on`/`off`, and `1`/`0` (all are interpreted as booleans):

```ini
logging: yes
readonly: off
cache: 1
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > booleans-quickstart.tsk << 'EOF'
[flags]
debug: true
maintenance: no
feature_x: on
feature_y: 0
EOF

config=$(tusk_parse booleans-quickstart.tsk)
echo "Debug: $(tusk_get "$config" flags.debug)"
echo "Maintenance: $(tusk_get "$config" flags.maintenance)"
echo "Feature X: $(tusk_get "$config" flags.feature_x)"
echo "Feature Y: $(tusk_get "$config" flags.feature_y)"
```

## 🔗 Real-World Use Cases

### 1. Feature Flags
```ini
[features]
new_ui: @env("ENABLE_NEW_UI", false)
```

### 2. Security Toggles
```ini
[security]
require_2fa: @if($environment == "production", true, false)
```

### 3. Maintenance Mode
```ini
[ops]
maintenance: @env("MAINTENANCE_MODE", false)
```

### 4. Conditional Logic in Bash
```bash
#!/bin/bash
source tusk-bash.sh

cat > booleans-logic.tsk << 'EOF'
[ops]
maintenance: @env("MAINTENANCE_MODE", false)
EOF

config=$(tusk_parse booleans-logic.tsk)
if [[ $(tusk_get "$config" ops.maintenance) == "true" ]]; then
  echo "Site is in maintenance mode."
else
  echo "Site is live!"
fi
```

## 🧠 Advanced Boolean Logic

### Chained Conditions
```ini
[deploy]
can_deploy: @if($user_is_admin && !$maintenance, true, false)
```

### Boolean Expressions
```ini
[security]
allow_login: @if($account_active && !$locked, true, false)
```

### Environment-Driven Booleans
```ini
[env]
prod: @env("APP_ENV", "development") == "production"
```

## 🛡️ Security & Performance Notes
- **Never use booleans for secrets.** Use them for toggles, not for storing sensitive data.
- **Short-circuit logic:** TuskLang evaluates boolean expressions efficiently, so use `&&` and `||` for performance.
- **Environment safety:** Always default to `false` for security toggles unless explicitly enabled.

## 🐞 Troubleshooting
- **Unexpected string values:** If you see `"yes"` or `"no"` as strings, check for missing type conversion or quotes.
- **Case sensitivity:** TuskLang is case-insensitive for booleans (`True`, `TRUE`, `true` all work).
- **Falsy values:** `0`, `no`, `off`, `false` are all interpreted as `false`.

## 💡 Best Practices
- Use `true`/`false` for clarity.
- Default to `false` for safety.
- Use environment variables for runtime toggles.
- Document all feature flags and toggles.
- Validate boolean values with `@validate.type("flag", "bool")`.

## 🔗 Cross-References
- [Conditional Logic](060-conditional-logic-bash.md)
- [Environment Variables](044-at-env-variables-bash.md)
- [Security Best Practices](096-security-best-practices-bash.md)

---

**Master boolean logic in TuskLang and control your Bash-powered world with confidence. 🟢** 
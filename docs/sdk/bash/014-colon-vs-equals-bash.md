# ⚖️ TuskLang Bash Colon vs Equals Guide

**"We don't bow to any king" – Choose your syntax, own your style.**

TuskLang's revolutionary flexibility lets you choose between `:` and `=` for assignment. Whether you prefer traditional INI-style colons or modern equals signs, TuskLang adapts to your coding style while maintaining full functionality.

## 🎯 The Great Syntax Debate
TuskLang supports both assignment operators:
- `:` - Traditional INI-style assignment
- `=` - Modern equals assignment

Both are functionally identical, but offer different aesthetic and workflow preferences.

## 📝 Syntax Comparison

### Colon Syntax (Traditional)
```ini
# INI-style with colons
[server]
host: "localhost"
port: 8080
debug: true
```

### Equals Syntax (Modern)
```ini
# Modern equals syntax
[server]
host = "localhost"
port = 8080
debug = true
```

### Mixed Syntax (Your Choice)
```ini
# Mix and match - TuskLang doesn't care
[config]
traditional: "value"
modern = "value"
mixed: "value" = "also works"
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > syntax-comparison.tsk << 'EOF'
[traditional]
host: "localhost"
port: 8080

[modern]
host = "localhost"
port = 8080

[mixed]
host: "localhost"
port = 8080
EOF

config=$(tusk_parse syntax-comparison.tsk)
echo "Traditional: $(tusk_get "$config" traditional.host):$(tusk_get "$config" traditional.port)"
echo "Modern: $(tusk_get "$config" modern.host):$(tusk_get "$config" modern.port)"
echo "Mixed: $(tusk_get "$config" mixed.host):$(tusk_get "$config" mixed.port)"
```

## 🔗 Real-World Use Cases

### 1. Team Preferences
```ini
# Alice prefers colons
[alice_config]
api_key: @env("API_KEY")
timeout: 30

# Bob prefers equals
[bob_config]
api_key = @env("API_KEY")
timeout = 30
```

### 2. Legacy Migration
```ini
# Migrating from INI files
[legacy]
old_style: "value"

# New TuskLang style
[modern]
new_style = "value"
```

### 3. Mixed Environments
```ini
[development]
debug: true
log_level = "debug"

[production]
debug = false
log_level: "error"
```

### 4. Syntax in Bash Scripts
```bash
#!/bin/bash
source tusk-bash.sh

# Generate config with preferred syntax
if [[ "$PREFERRED_SYNTAX" == "colon" ]]; then
    cat > config.tsk << 'EOF'
[app]
host: "localhost"
port: 8080
EOF
else
    cat > config.tsk << 'EOF'
[app]
host = "localhost"
port = 8080
EOF
fi

config=$(tusk_parse config.tsk)
echo "Using $PREFERRED_SYNTAX syntax: $(tusk_get "$config" app.host)"
```

## 🧠 Advanced Syntax Patterns

### Complex Objects
```ini
# Colon style
[server_colon]
config: {
    host: "localhost",
    port: 8080,
    ssl: true
}

# Equals style
[server_equals]
config = {
    host = "localhost",
    port = 8080,
    ssl = true
}
```

### Arrays and Nested Structures
```ini
# Mixed syntax in complex structures
[complex]
servers: ["web1", "web2"]
database = {
    host: "db.local",
    port = 5432
}
```

### Dynamic Values
```ini
# Both work with @ operators
[dynamic_colon]
api_key: @env("API_KEY")
timestamp: @date.now()

[dynamic_equals]
api_key = @env("API_KEY")
timestamp = @date.now()
```

## 🛡️ Security & Performance Notes
- **No performance difference:** Both syntaxes parse at identical speeds.
- **Security equivalence:** Both syntaxes handle sensitive data identically.
- **Validation:** Both syntaxes work with all validation operators.

## 🐞 Troubleshooting
- **Mixed syntax errors:** Ensure consistent spacing around operators.
- **Parsing issues:** Both syntaxes are equally robust; issues are usually content-related.
- **Editor support:** Some editors may prefer one syntax over the other for highlighting.

## 💡 Best Practices
- **Choose one style per project:** Consistency improves readability.
- **Team agreement:** Establish syntax preferences in your team's style guide.
- **Legacy compatibility:** Use colons when migrating from INI files.
- **Modern projects:** Consider equals for new projects with modern tooling.
- **Document your choice:** Include syntax preference in project documentation.

## 🔗 Cross-References
- [Basic Syntax](003-basic-syntax-bash.md)
- [Key-Value Basics](007-key-value-basics-bash.md)
- [Best Practices](023-best-practices-bash.md)

---

**Choose your syntax, own your style, and let TuskLang adapt to you. ⚖️** 
# 🟨 Comments in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Learn how to use comments in TuskLang configuration files for JavaScript projects. Comments help document your config, disable lines, and clarify intent.

## 💬 Single-Line Comments

TuskLang supports `#`, `//`, and `;` for single-line comments.

```ini
# This is a comment
// This is also a comment
; This is another comment
app {
    name: "MyApp" # Inline comment
}
```

## 📝 Multi-Line Comments

Use triple quotes for block comments:

```ini
"""
This is a multi-line comment.
It can span several lines.
Useful for documentation.
"""

app {
    name: "MyApp"
}
```

## 🧑‍💻 JavaScript Example: Annotating Config

```javascript
// config/app.tsk
app {
    name: "MyApp" // Application name
    version: "1.0.0" # Version number
    // port: 3000 (disabled for now)
    environment: @env("NODE_ENV", "development")
}

# Database settings
// Use PostgreSQL in production
; Use SQLite for local development
```

## 🚦 Best Practices
- Use comments to explain non-obvious logic
- Document environment-specific overrides
- Disable features by commenting out lines
- Use block comments for section headers

## 🛡️ Security Note
Never put secrets in comments—comments may be exposed in version control.

## 🧪 Testing Comments
Comments are ignored by the TuskLang parser and do not affect runtime behavior.

## 📚 Next Steps
- [Key-Value Basics](012-key-value-basics-javascript.md)
- [Syntax Errors](029-syntax-errors-javascript.md)

**Ready to document your TuskLang configs like a pro!** 
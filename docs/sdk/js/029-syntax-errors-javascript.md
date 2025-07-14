# 🟨 Syntax Errors in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Identify and fix syntax errors in TuskLang configs for JavaScript. Avoid common mistakes and debug like a pro.

## ❌ Common Syntax Errors

- Missing colons or equals
- Unclosed braces or brackets
- Invalid characters in keys
- Using reserved keywords
- Incorrect indentation in multiline values
- Unescaped quotes in strings

## 🧑‍💻 JavaScript Example: Debugging Syntax Errors

```ini
# Bad:
app {
    name "MyApp"   # Missing colon
    version: 1.0.0
    port: 3000
}

# Good:
app {
    name: "MyApp"
    version: 1.0.0
    port: 3000
}
```

## 🛠️ Error Messages
TuskLang provides clear error messages with line numbers and hints.

```bash
Error: Syntax error in config/app.tsk at line 2: Expected ':' or '=' after key 'name'
```

## 🚦 Best Practices
- Validate configs before deploying
- Use a linter or IDE extension for TuskLang
- Keep configs under version control

## 🛡️ Security Note
Never ignore syntax errors in production configs.

## 📚 Next Steps
- [Best Practices](030-best-practices-javascript.md)
- [Multiline Values](024-multiline-values-javascript.md)

**Ready to squash syntax bugs in TuskLang!** 
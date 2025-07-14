# 🟨 @ Operator Introduction in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Meet the @ operator—TuskLang's superpower for dynamic configuration in JavaScript. The @ operator lets you execute functions, access environment variables, and perform dynamic operations right in your config files.

## ⚡ What is the @ Operator?

The @ operator is TuskLang's way of executing functions and accessing dynamic data within configuration files. Think of it as having JavaScript superpowers in your config.

## 🧑‍💻 JavaScript Example: Basic @ Operator Usage

```javascript
// config/app.tsk
app {
    name: "MyApp"
    port: @env("PORT", 3000)
    debug: @if(@env("NODE_ENV") == "development", true, false)
    timestamp: @date.now()
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.port); // 3000 (or PORT env var)
console.log(config.app.debug); // true/false based on NODE_ENV
console.log(config.app.timestamp); // Current timestamp
```

## 🔧 Common @ Operators

- `@env()` - Access environment variables
- `@date.now()` - Get current timestamp
- `@if()` - Conditional logic
- `@query()` - Database queries
- `@cache()` - Caching operations
- `@http()` - HTTP requests
- `@file.read()` - Read files
- `@metrics()` - Application metrics

## 🚦 Best Practices
- Use @ operators for dynamic configuration
- Provide fallback values for @env operations
- Cache expensive operations with @cache

## 🛡️ Security Note
Never use @ operators with untrusted input—validate all external data.

## 📚 Next Steps
- [@ Variable Reference](032-at-variable-reference-javascript.md)
- [@ Variable Fallback](033-at-variable-fallback-javascript.md)

**Ready to supercharge your configs with @ operators!** 
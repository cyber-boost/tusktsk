# 🟨 Key-Value Basics in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Master the basics of key-value pairs in TuskLang configuration for JavaScript projects. This is the foundation of all TuskLang files.

## 🔑 Key-Value Syntax

```ini
app_name: "MyApp"
port = 3000
version: 1.0.0
```

All of these are valid. TuskLang supports both `:` and `=` as separators.

## 🧑‍💻 JavaScript Example: Loading Config

```javascript
// config/app.tsk
app_name: "MyApp"
port: 3000

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app_name); // "MyApp"
console.log(config.port); // 3000
```

## 🗂️ Nested Keys (Sections)

```ini
app {
    name: "MyApp"
    version: "1.0.0"
    port: 3000
}
```

## 📋 Arrays and Objects

```ini
features: ["auth", "api", "cache"]
database {
    host: "localhost"
    port: 5432
}
```

## 🧪 Type Inference
TuskLang automatically infers types: numbers, strings, booleans, arrays, objects.

## 🚦 Best Practices
- Use clear, descriptive keys
- Prefer lowercase and underscores for keys
- Group related settings in sections

## 🛡️ Security Note
Never store secrets in plain text—use `@env.secure` for sensitive values.

## 📚 Next Steps
- [Colon vs Equals](013-colon-vs-equals-javascript.md)
- [Strings](014-strings-javascript.md)

**Ready to build robust TuskLang configs!** 
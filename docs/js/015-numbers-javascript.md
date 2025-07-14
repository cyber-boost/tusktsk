# 🟨 Numbers in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Work with numbers in TuskLang configs for JavaScript. Numbers are automatically detected and type-safe.

## 🔢 Number Syntax

```ini
port: 3000
version: 1.0
max_users: 10000
```

## 🧑‍💻 JavaScript Example: Using Numbers

```javascript
// config/app.tsk
app {
    port: 3000
    version: 1.0
    max_users: 10000
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.port); // 3000
console.log(config.app.version); // 1.0
console.log(config.app.max_users); // 10000
```

## ➕ Math Operations

```ini
math {
    sum: 2 + 2
    product: 6 * 7
    ratio: 10 / 2
}
```

## 🚦 Best Practices
- Use numbers for ports, limits, and calculations
- Avoid quotes around numbers unless you want a string

## 🛡️ Security Note
Validate numeric input to prevent injection.

## 📚 Next Steps
- [Booleans](016-booleans-javascript.md)
- [Null Values](017-null-values-javascript.md)

**Ready to crunch numbers in TuskLang!** 
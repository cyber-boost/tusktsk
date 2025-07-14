# 🟨 Typed Values in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

TuskLang supports explicit types for values in your JavaScript configs. Get type safety and clarity.

## 🏷️ Typed Value Syntax

```ini
port: 3000 # number
debug: true # boolean
api_key: null # null
features: ["auth", "api"] # array
settings: { theme: "dark", version: 2 } # object
```

## 🧑‍💻 JavaScript Example: Using Typed Values

```javascript
// config/app.tsk
app {
    port: 3000 # number
    debug: true # boolean
    api_key: null # null
    features: ["auth", "api"] # array
    settings: { theme: "dark", version: 2 } # object
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(typeof config.app.port); // 'number'
console.log(typeof config.app.debug); // 'boolean'
console.log(config.app.api_key === null); // true
console.log(Array.isArray(config.app.features)); // true
console.log(typeof config.app.settings); // 'object'
```

## 🧪 Type Inference
TuskLang infers types automatically, but you can annotate for clarity.

## 🚦 Best Practices
- Annotate types for complex configs
- Use arrays/objects for structured data

## 🛡️ Security Note
Validate types at runtime for critical logic.

## 📚 Next Steps
- [References](026-references-javascript.md)
- [Variable Naming](027-variable-naming-javascript.md)

**Ready to type your configs like a pro!** 
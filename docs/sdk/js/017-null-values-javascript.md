# 🟨 Null Values in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Handle null and empty values in TuskLang configs for JavaScript. Nulls are explicit and type-safe.

## 🕳️ Null Syntax

```ini
api_key: null
optional_field = NULL
missing_value:
```

TuskLang treats `null`, `NULL`, or an empty value as null.

## 🧑‍💻 JavaScript Example: Using Nulls

```javascript
// config/app.tsk
app {
    api_key: null
    optional_field: NULL
    missing_value:
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.api_key); // null
console.log(config.app.optional_field); // null
console.log(config.app.missing_value); // null
```

## 🔄 Null Coalescing

```ini
api_key: @env("API_KEY", null)
final_key: $api_key ?? "default-key"
```

## 🚦 Best Practices
- Use null for optional or missing values
- Avoid using empty strings for nulls

## 🛡️ Security Note
Never treat null as a valid credential or secret.

## 📚 Next Steps
- [Multiline Values](024-multiline-values-javascript.md)
- [Typed Values](025-typed-values-javascript.md)

**Ready to handle nulls in TuskLang!** 
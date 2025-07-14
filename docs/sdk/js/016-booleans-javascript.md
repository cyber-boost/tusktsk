# 🟨 Booleans in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Work with booleans in TuskLang configs for JavaScript. Booleans are type-safe and support conditional logic.

## 🔘 Boolean Syntax

```ini
enabled: true
debug = false
is_admin: yes
is_guest: no
```

TuskLang recognizes `true`, `false`, `yes`, `no` (case-insensitive).

## 🧑‍💻 JavaScript Example: Using Booleans

```javascript
// config/app.tsk
app {
    enabled: true
    debug: false
    is_admin: yes
    is_guest: no
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.enabled); // true
console.log(config.app.debug); // false
console.log(config.app.is_admin); // true
console.log(config.app.is_guest); // false
```

## 🔄 Conditional Logic

```ini
settings {
    show_logs: @if(@env("NODE_ENV") == "development", true, false)
}
```

## 🚦 Best Practices
- Use booleans for feature flags and toggles
- Prefer `true`/`false` for clarity

## 🛡️ Security Note
Never use booleans to hide security-critical features—enforce on the backend.

## 📚 Next Steps
- [Null Values](017-null-values-javascript.md)
- [Multiline Values](024-multiline-values-javascript.md)

**Ready to toggle features in TuskLang!** 
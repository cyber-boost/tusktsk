# 🟨 Colon vs Equals in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

TuskLang lets you use either `:` or `=` as a key-value separator. Choose your style—TuskLang doesn't care.

## ➡️ Syntax Examples

```ini
# Both are valid
app_name: "MyApp"
port = 3000
version: "1.0.0"
```

## 🧑‍💻 JavaScript Example: Mixing Styles

```ini
app {
    name: "MyApp"
    version = "1.0.0"
    port: 3000
}
```

```javascript
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.name); // "MyApp"
console.log(config.app.version); // "1.0.0"
console.log(config.app.port); // 3000
```

## 📝 When to Use Each
- `:` is more common in YAML/INI
- `=` is familiar from .env files
- Use whichever fits your team's style

## 🚦 Best Practices
- Be consistent within a file or section
- Use `:` for nested objects/sections
- Use `=` for flat key-value pairs

## 🛡️ Security Note
Separators do not affect security—use `@env.secure` for secrets.

## 📚 Next Steps
- [Strings](014-strings-javascript.md)
- [Numbers](015-numbers-javascript.md)

**Ready to write configs your way!** 
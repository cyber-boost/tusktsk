# 🟨 References in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Reference other values and sections in TuskLang configs for JavaScript. Build DRY, maintainable configs.

## 🔗 Reference Syntax

```ini
app_name: "MyApp"
welcome_message: "Welcome to $app_name!"

app {
    name: "MyApp"
    version: "1.0.0"
    full: "$name v$version"
}
```

## 🧑‍💻 JavaScript Example: Using References

```javascript
// config/app.tsk
app {
    name: "MyApp"
    version: "1.0.0"
    full: "$name v$version"
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.full); // "MyApp v1.0.0"
```

## 🔄 Cross-File References

```ini
# config/global.tsk
company: "TuskLang Inc."

# config/app.tsk
import: "./global.tsk"
welcome: "Welcome to $company!"
```

## 🚦 Best Practices
- Use references for repeated values
- Avoid circular references

## 🛡️ Security Note
Do not reference secrets in public configs.

## 📚 Next Steps
- [Variable Naming](027-variable-naming-javascript.md)
- [Reserved Keywords](028-reserved-keywords-javascript.md)

**Ready to reference like a TuskLang pro!** 
# 🟨 Strings in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Work with strings in TuskLang configs for JavaScript. Strings are flexible, powerful, and support interpolation.

## 📝 String Syntax

```ini
name: "MyApp"
welcome: 'Welcome to TuskLang!'
description: """Multi-line
string support"""
```

## 🧑‍💻 JavaScript Example: Using Strings

```javascript
// config/app.tsk
app {
    name: "MyApp"
    description: "A blazing fast JS app"
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.name); // "MyApp"
console.log(config.app.description); // "A blazing fast JS app"
```

## 🔄 String Interpolation

```ini
user {
    first: "Jane"
    last: "Doe"
    full: "$first $last"
}
```

## 🧪 Escaping Characters

```ini
path: "C:\\Users\\Jane\\Documents"
quote: "She said, \"Hello!\""
```

## 🚦 Best Practices
- Use double quotes for interpolation
- Use triple quotes for multi-line strings
- Escape special characters as needed

## 🛡️ Security Note
Sanitize user input before using in configs.

## 📚 Next Steps
- [Numbers](015-numbers-javascript.md)
- [Booleans](016-booleans-javascript.md)

**Ready to master strings in TuskLang!** 
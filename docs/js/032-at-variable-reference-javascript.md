# 🟨 @ Variable Reference in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Reference variables and values using the @ operator in TuskLang configs for JavaScript. Access dynamic data and build flexible configurations.

## 🔗 @ Variable Reference Syntax

```ini
app_name: "MyApp"
welcome: "Welcome to @app_name!"

user {
    first: "Jane"
    last: "Doe"
    full: "@first @last"
}
```

## 🧑‍💻 JavaScript Example: Using @ Variable References

```javascript
// config/app.tsk
app {
    name: "MyApp"
    version: "1.0.0"
    full_name: "@name v@version"
}

user {
    first: "Jane"
    last: "Doe"
    email: "jane@example.com"
    display_name: "@first @last (@email)"
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.full_name); // "MyApp v1.0.0"
console.log(config.user.display_name); // "Jane Doe (jane@example.com)"
```

## 🔄 Cross-Section References

```ini
app {
    name: "MyApp"
    port: 3000
}

server {
    host: "localhost"
    url: "http://@server.host:@app.port"
}
```

## 🚦 Best Practices
- Use @ references for dynamic values
- Avoid circular references
- Reference values from other sections when needed

## 🛡️ Security Note
Be careful when referencing user input—validate all referenced values.

## 📚 Next Steps
- [@ Variable Fallback](033-at-variable-fallback-javascript.md)
- [@ Request Object](034-at-request-object-javascript.md)

**Ready to reference variables dynamically in TuskLang!** 
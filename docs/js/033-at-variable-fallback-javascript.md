# 🟨 @ Variable Fallback in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Provide fallback values when variables are missing or undefined in TuskLang configs for JavaScript. Build robust, fault-tolerant configurations.

## 🛡️ @ Variable Fallback Syntax

```ini
# Using ?? operator for fallback
api_key: @env("API_KEY") ?? "default-key"
port: @env("PORT") ?? 3000

# Using || operator for fallback
debug: @env("DEBUG") || false
host: @env("HOST") || "localhost"
```

## 🧑‍💻 JavaScript Example: Using @ Variable Fallbacks

```javascript
// config/app.tsk
app {
    name: "MyApp"
    port: @env("PORT") ?? 3000
    debug: @env("DEBUG") || false
    api_key: @env("API_KEY") ?? "default-key"
    host: @env("HOST") || "localhost"
}

database {
    host: @env("DB_HOST") ?? "localhost"
    port: @env("DB_PORT") ?? 5432
    name: @env("DB_NAME") ?? "myapp"
    user: @env("DB_USER") ?? "postgres"
    password: @env.secure("DB_PASSWORD") ?? null
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.port); // 3000 (or PORT env var)
console.log(config.app.debug); // false (or DEBUG env var)
console.log(config.database.host); // "localhost" (or DB_HOST env var)
```

## 🔄 Nested Fallbacks

```ini
settings {
    theme: @env("THEME") ?? @env("DEFAULT_THEME") ?? "light"
    timeout: @env("TIMEOUT") ?? @env("DEFAULT_TIMEOUT") ?? 5000
}
```

## 🚦 Best Practices
- Always provide sensible fallbacks
- Use `??` for null/undefined fallbacks
- Use `||` for falsy value fallbacks
- Document fallback values

## 🛡️ Security Note
Never use fallbacks for security-critical values like passwords or API keys.

## 📚 Next Steps
- [@ Request Object](034-at-request-object-javascript.md)
- [@ Request Method](035-at-request-method-javascript.md)

**Ready to build fault-tolerant configs in TuskLang!** 
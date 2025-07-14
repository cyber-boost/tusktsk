# 🟨 Variable Naming in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Follow best practices for variable naming in TuskLang configs for JavaScript. Consistent naming improves readability and maintainability.

## 🏷️ Naming Conventions

- Use lowercase and underscores: `app_name`, `db_host`
- Avoid spaces and special characters
- Use descriptive names
- For sections, use singular nouns: `database`, `server`, `user`

## 🧑‍💻 JavaScript Example: Naming Variables

```ini
app_name: "MyApp"
db_host: "localhost"
user_id: 42
```

```javascript
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app_name); // "MyApp"
console.log(config.db_host); // "localhost"
console.log(config.user_id); // 42
```

## 🚦 Best Practices
- Be consistent across files
- Avoid reserved keywords (see next section)
- Use clear, unambiguous names

## 🛡️ Security Note
Never use variable names that reveal sensitive logic or secrets.

## 📚 Next Steps
- [Reserved Keywords](028-reserved-keywords-javascript.md)
- [Syntax Errors](029-syntax-errors-javascript.md)

**Ready to name your configs like a pro!** 
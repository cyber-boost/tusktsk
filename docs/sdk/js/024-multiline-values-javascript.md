# 🟨 Multiline Values in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Store and use multiline values in TuskLang configs for JavaScript. Great for descriptions, SQL, or code blocks.

## 📝 Multiline String Syntax

```ini
description: """
This is a multiline string.
It can span several lines.
Great for documentation!
"""

sql_query: '''
SELECT *
FROM users
WHERE active = true
'''
```

TuskLang supports both triple double quotes (`"""`) and triple single quotes (`'''`).

## 🧑‍💻 JavaScript Example: Using Multiline Values

```javascript
// config/app.tsk
app {
    description: """
        This app is powered by TuskLang.
        It supports multiline values!
    """
    sql_query: '''
        SELECT * FROM users WHERE active = true
    '''
}

// index.js
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config/app.tsk');
console.log(config.app.description);
console.log(config.app.sql_query);
```

## 🚦 Best Practices
- Use multiline values for docs, SQL, or code
- Indent consistently for readability

## 🛡️ Security Note
Never store secrets in plain text, even in multiline blocks.

## 📚 Next Steps
- [Typed Values](025-typed-values-javascript.md)
- [References](026-references-javascript.md)

**Ready to document richly in TuskLang!** 
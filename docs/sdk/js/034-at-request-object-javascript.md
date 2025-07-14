# 🟨 @ Request Object in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Access HTTP request data using the @ request object in TuskLang configs for JavaScript. Build dynamic, request-aware configurations.

## 🌐 @ Request Object Syntax

```ini
# Access request properties
user_agent: @request.headers["user-agent"]
method: @request.method
url: @request.url
ip: @request.ip
```

## 🧑‍💻 JavaScript Example: Using @ Request Object

```javascript
// config/api.tsk
api {
    user_agent: @request.headers["user-agent"]
    method: @request.method
    url: @request.url
    ip: @request.ip
    host: @request.headers["host"]
    referer: @request.headers["referer"]
}

// Express.js middleware
app.use(async (req, res, next) => {
    try {
        const tsk = new TuskLang.Enhanced();
        const config = await tsk.parse(TuskLang.parseFile('config/api.tsk'), { request: req });
        req.apiConfig = config;
        next();
    } catch (error) {
        console.error('Config parsing error:', error);
        next(error);
    }
});

// Route handler
app.get('/api/data', (req, res) => {
    console.log(req.apiConfig.api.user_agent);
    console.log(req.apiConfig.api.method);
    res.json({ success: true });
});
```

## 🔍 Available Request Properties

- `@request.method` - HTTP method (GET, POST, etc.)
- `@request.url` - Request URL
- `@request.ip` - Client IP address
- `@request.headers` - Request headers
- `@request.body` - Request body (for POST/PUT)
- `@request.query` - Query parameters
- `@request.params` - Route parameters

## 🚦 Best Practices
- Validate request data before using
- Sanitize user input
- Use request data for dynamic configuration

## 🛡️ Security Note
Never trust request data without validation—always sanitize user input.

## 📚 Next Steps
- [@ Request Method](035-at-request-method-javascript.md)
- [@ Request Query](036-at-request-query-javascript.md)

**Ready to build request-aware configs in TuskLang!** 
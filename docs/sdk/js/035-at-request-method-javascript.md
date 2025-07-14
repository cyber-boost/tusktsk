# 🟨 @ Request Method in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Access and work with HTTP request methods using @ request.method in TuskLang configs for JavaScript. Build method-aware configurations.

## 🔧 @ Request Method Syntax

```ini
# Access request method
method: @request.method

# Conditional logic based on method
is_get: @if(@request.method == "GET", true, false)
is_post: @if(@request.method == "POST", true, false)
```

## 🧑‍💻 JavaScript Example: Using @ Request Method

```javascript
// config/api.tsk
api {
    method: @request.method
    is_get: @if(@request.method == "GET", true, false)
    is_post: @if(@request.method == "POST", true, false)
    is_put: @if(@request.method == "PUT", true, false)
    is_delete: @if(@request.method == "DELETE", true, false)
    
    # Method-specific settings
    cache_enabled: @if(@request.method == "GET", true, false)
    requires_auth: @if(@request.method == "POST" || @request.method == "PUT" || @request.method == "DELETE", true, false)
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
app.all('/api/data', (req, res) => {
    console.log('Request method:', req.apiConfig.api.method);
    console.log('Is GET request:', req.apiConfig.api.is_get);
    console.log('Cache enabled:', req.apiConfig.api.cache_enabled);
    console.log('Requires auth:', req.apiConfig.api.requires_auth);
    
    res.json({ 
        method: req.apiConfig.api.method,
        cacheEnabled: req.apiConfig.api.cache_enabled,
        requiresAuth: req.apiConfig.api.requires_auth
    });
});
```

## 🔍 Supported HTTP Methods

- `GET` - Retrieve data
- `POST` - Create data
- `PUT` - Update data
- `DELETE` - Delete data
- `PATCH` - Partial update
- `HEAD` - Headers only
- `OPTIONS` - Preflight request

## 🚦 Best Practices
- Use method-aware configuration for security
- Enable caching only for GET requests
- Require authentication for state-changing methods

## 🛡️ Security Note
Always validate request methods and implement proper authorization.

## 📚 Next Steps
- [@ Request Query](036-at-request-query-javascript.md)
- [@ Request Post](037-at-request-post-javascript.md)

**Ready to build method-aware configs in TuskLang!** 
# 🟨 @ Request Headers in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Access and work with HTTP request headers using @ request.headers in TuskLang configs for JavaScript. Build header-aware configurations.

## 📋 @ Request Headers Syntax

```ini
# Access headers
user_agent: @request.headers["user-agent"]
accept: @request.headers["accept"]
authorization: @request.headers["authorization"]

# With fallbacks
user_agent: @request.headers["user-agent"] ?? "Unknown"
accept: @request.headers["accept"] ?? "*/*"
```

## 🧑‍💻 JavaScript Example: Using @ Request Headers

```javascript
// config/headers.tsk
headers {
    # Common headers
    user_agent: @request.headers["user-agent"] ?? "Unknown"
    accept: @request.headers["accept"] ?? "*/*"
    authorization: @request.headers["authorization"] ?? ""
    content_type: @request.headers["content-type"] ?? "application/json"
    host: @request.headers["host"] ?? ""
    referer: @request.headers["referer"] ?? ""
    
    # Browser detection
    is_mobile: @if(@user_agent contains "Mobile", true, false)
    is_bot: @if(@user_agent contains "bot" || @user_agent contains "crawler", true, false)
    
    # Authentication
    has_auth: @if(@authorization != "", true, false)
    auth_type: @if(@authorization starts_with "Bearer", "Bearer", @if(@authorization starts_with "Basic", "Basic", "None"))
    
    # Content negotiation
    accepts_json: @if(@accept contains "application/json", true, false)
    accepts_xml: @if(@accept contains "application/xml", true, false)
}

// Express.js middleware
app.use(async (req, res, next) => {
    try {
        const tsk = new TuskLang.Enhanced();
        const config = await tsk.parse(TuskLang.parseFile('config/headers.tsk'), { request: req });
        req.headerConfig = config;
        next();
    } catch (error) {
        console.error('Config parsing error:', error);
        next(error);
    }
});

// Route handler
app.get('/api/data', (req, res) => {
    const { headers } = req.headerConfig;
    
    // Set response based on headers
    if (headers.accepts_json) {
        res.setHeader('Content-Type', 'application/json');
        res.json({ 
            userAgent: headers.user_agent,
            isMobile: headers.is_mobile,
            isBot: headers.is_bot,
            hasAuth: headers.has_auth,
            authType: headers.auth_type
        });
    } else if (headers.accepts_xml) {
        res.setHeader('Content-Type', 'application/xml');
        res.send(`<response><userAgent>${headers.user_agent}</userAgent></response>`);
    } else {
        res.setHeader('Content-Type', 'text/plain');
        res.send(`User Agent: ${headers.user_agent}`);
    }
});
```

## 🔍 Common Headers

- `user-agent` - Browser/client information
- `accept` - Accepted content types
- `authorization` - Authentication credentials
- `content-type` - Request content type
- `host` - Request host
- `referer` - Referring page

## 🚦 Best Practices
- Always provide fallbacks for headers
- Validate and sanitize header values
- Use headers for content negotiation and authentication

## 🛡️ Security Note
Never trust header values without validation—headers can be spoofed.

## 📚 Next Steps
- [@ Session Variables](039-at-session-variables-javascript.md)
- [@ Cookie Variables](040-at-cookie-variables-javascript.md)

**Ready to build header-aware configs in TuskLang!** 
# 🟨 @ Session Variables in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Access and work with session variables using @ session in TuskLang configs for JavaScript. Build session-aware configurations.

## 🔐 @ Session Variables Syntax

```ini
# Access session variables
user_id: @session.user_id
username: @session.username
is_logged_in: @session.is_logged_in

# With fallbacks
user_id: @session.user_id ?? null
username: @session.username ?? "Guest"
is_logged_in: @session.is_logged_in ?? false
```

## 🧑‍💻 JavaScript Example: Using @ Session Variables

```javascript
// config/session.tsk
session {
    # Session data
    user_id: @session.user_id ?? null
    username: @session.username ?? "Guest"
    is_logged_in: @session.is_logged_in ?? false
    role: @session.role ?? "guest"
    last_activity: @session.last_activity ?? null
    
    # User preferences
    theme: @session.theme ?? "light"
    language: @session.language ?? "en"
    timezone: @session.timezone ?? "UTC"
    
    # Authentication status
    is_admin: @if(@role == "admin", true, false)
    is_user: @if(@role == "user", true, false)
    is_guest: @if(@role == "guest", true, false)
    
    # Session management
    session_age: @if(@last_activity != null, @date.diff(@last_activity, @date.now()), 0)
    is_expired: @if(@session_age > 3600, true, false) # 1 hour
}

// Express.js with session middleware
const session = require('express-session');

app.use(session({
    secret: 'your-secret-key',
    resave: false,
    saveUninitialized: false,
    cookie: { secure: process.env.NODE_ENV === 'production' }
}));

app.use(async (req, res, next) => {
    try {
        const tsk = new TuskLang.Enhanced();
        const config = await tsk.parse(TuskLang.parseFile('config/session.tsk'), { session: req.session });
        req.sessionConfig = config;
        next();
    } catch (error) {
        console.error('Config parsing error:', error);
        next(error);
    }
});

// Route handler
app.get('/api/profile', (req, res) => {
    const { session } = req.sessionConfig;
    
    if (!session.is_logged_in) {
        return res.status(401).json({ error: 'Not authenticated' });
    }
    
    if (session.is_expired) {
        return res.status(401).json({ error: 'Session expired' });
    }
    
    res.json({
        userId: session.user_id,
        username: session.username,
        role: session.role,
        isAdmin: session.is_admin,
        theme: session.theme,
        language: session.language,
        timezone: session.timezone
    });
});

// Login route
app.post('/api/login', (req, res) => {
    // Authenticate user...
    req.session.user_id = 123;
    req.session.username = "john_doe";
    req.session.is_logged_in = true;
    req.session.role = "user";
    req.session.last_activity = new Date().toISOString();
    
    res.json({ success: true });
});
```

## 🔍 Session Variable Access

- `@session.variable_name` - Access specific session variable
- `@session.variable_name ?? default` - With fallback
- `@session` - All session data

## 🚦 Best Practices
- Always provide fallbacks for session variables
- Validate session data before using
- Use sessions for user authentication and preferences

## 🛡️ Security Note
Never store sensitive data in sessions—use secure session storage and implement proper session management.

## 📚 Next Steps
- [@ Cookie Variables](040-at-cookie-variables-javascript.md)
- [@ JSON Function](041-at-json-function-javascript.md)

**Ready to build session-aware configs in TuskLang!** 
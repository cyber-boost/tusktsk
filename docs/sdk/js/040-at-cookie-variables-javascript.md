# 🟨 @ Cookie Variables in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Access and work with cookie variables using @ cookie in TuskLang configs for JavaScript. Build cookie-aware configurations.

## 🍪 @ Cookie Variables Syntax

```ini
# Access cookie variables
user_preference: @cookie.user_preference
session_id: @cookie.session_id
theme: @cookie.theme

# With fallbacks
user_preference: @cookie.user_preference ?? "default"
session_id: @cookie.session_id ?? null
theme: @cookie.theme ?? "light"
```

## 🧑‍💻 JavaScript Example: Using @ Cookie Variables

```javascript
// config/cookies.tsk
cookies {
    # Cookie data
    user_preference: @cookie.user_preference ?? "default"
    session_id: @cookie.session_id ?? null
    theme: @cookie.theme ?? "light"
    language: @cookie.language ?? "en"
    timezone: @cookie.timezone ?? "UTC"
    
    # User preferences
    is_dark_mode: @if(@theme == "dark", true, false)
    is_english: @if(@language == "en", true, false)
    
    # Session management
    has_session: @if(@session_id != null, true, false)
    
    # Analytics
    tracking_id: @cookie.tracking_id ?? null
    has_tracking: @if(@tracking_id != null, true, false)
}

// Express.js with cookie parser
const cookieParser = require('cookie-parser');

app.use(cookieParser());

app.use(async (req, res, next) => {
    try {
        const tsk = new TuskLang.Enhanced();
        const config = await tsk.parse(TuskLang.parseFile('config/cookies.tsk'), { cookie: req.cookies });
        req.cookieConfig = config;
        next();
    } catch (error) {
        console.error('Config parsing error:', error);
        next(error);
    }
});

// Route handler
app.get('/api/preferences', (req, res) => {
    const { cookies } = req.cookieConfig;
    
    res.json({
        theme: cookies.theme,
        language: cookies.language,
        timezone: cookies.timezone,
        isDarkMode: cookies.is_dark_mode,
        isEnglish: cookies.is_english,
        hasSession: cookies.has_session,
        hasTracking: cookies.has_tracking
    });
});

// Set cookie route
app.post('/api/preferences', (req, res) => {
    const { theme, language, timezone } = req.body;
    
    // Set cookies
    res.cookie('theme', theme, { 
        maxAge: 30 * 24 * 60 * 60 * 1000, // 30 days
        httpOnly: false,
        secure: process.env.NODE_ENV === 'production'
    });
    
    res.cookie('language', language, { 
        maxAge: 30 * 24 * 60 * 60 * 1000,
        httpOnly: false,
        secure: process.env.NODE_ENV === 'production'
    });
    
    res.cookie('timezone', timezone, { 
        maxAge: 30 * 24 * 60 * 60 * 1000,
        httpOnly: false,
        secure: process.env.NODE_ENV === 'production'
    });
    
    res.json({ success: true });
});
```

## 🔍 Cookie Variable Access

- `@cookie.cookie_name` - Access specific cookie
- `@cookie.cookie_name ?? default` - With fallback
- `@cookie` - All cookies

## 🚦 Best Practices
- Always provide fallbacks for cookie variables
- Validate cookie data before using
- Use cookies for user preferences and non-sensitive data

## 🛡️ Security Note
Never store sensitive data in cookies—use secure, httpOnly cookies for session management.

## 📚 Next Steps
- [@ JSON Function](041-at-json-function-javascript.md)
- [@ Render Function](042-at-render-function-javascript.md)

**Ready to build cookie-aware configs in TuskLang!** 
# 🟨 @ Request Post in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Access and work with POST request body data using @ request.post in TuskLang configs for JavaScript. Build dynamic, form-aware configurations.

## 📝 @ Request Post Syntax

```ini
# Access POST data
name: @request.post.name
email: @request.post.email
age: @request.post.age

# With fallbacks
name: @request.post.name ?? ""
email: @request.post.email ?? ""
age: @request.post.age ?? 0
```

## 🧑‍💻 JavaScript Example: Using @ Request Post

```javascript
// config/form.tsk
form {
    # POST data
    name: @request.post.name ?? ""
    email: @request.post.email ?? ""
    age: @request.post.age ?? 0
    message: @request.post.message ?? ""
    
    # Validation
    name_required: @if(@name == "", false, true)
    email_valid: @if(@email contains "@", true, false)
    age_valid: @if(@age > 0 && @age < 120, true, false)
    
    # Form processing
    is_valid: @if(@name_required && @email_valid && @age_valid, true, false)
}

// Express.js middleware
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

app.use(async (req, res, next) => {
    try {
        const tsk = new TuskLang.Enhanced();
        const config = await tsk.parse(TuskLang.parseFile('config/form.tsk'), { request: req });
        req.formConfig = config;
        next();
    } catch (error) {
        console.error('Config parsing error:', error);
        next(error);
    }
});

// Form submission handler
app.post('/api/contact', async (req, res) => {
    const { form } = req.formConfig;
    
    if (!form.is_valid) {
        return res.status(400).json({
            error: 'Invalid form data',
            validation: {
                name: form.name_required,
                email: form.email_valid,
                age: form.age_valid
            }
        });
    }
    
    // Process valid form data
    const contactData = {
        name: form.name,
        email: form.email,
        age: form.age,
        message: form.message
    };
    
    // Save to database...
    res.json({ success: true, data: contactData });
});
```

## 🔍 POST Data Access

- `@request.post.field_name` - Access specific field
- `@request.post.field_name ?? default` - With fallback
- `@request.post` - All POST data

## 🚦 Best Practices
- Always provide fallbacks for POST data
- Validate and sanitize all form inputs
- Use POST data for form processing and validation

## 🛡️ Security Note
Never trust POST data without validation—always sanitize user input and implement CSRF protection.

## 📚 Next Steps
- [@ Request Headers](038-at-request-headers-javascript.md)
- [@ Session Variables](039-at-session-variables-javascript.md)

**Ready to build form-aware configs in TuskLang!** 
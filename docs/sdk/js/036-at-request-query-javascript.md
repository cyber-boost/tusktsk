# 🟨 @ Request Query in TuskLang for JavaScript

**"We don't bow to any king" - JavaScript Edition**

Access and work with URL query parameters using @ request.query in TuskLang configs for JavaScript. Build dynamic, query-aware configurations.

## 🔍 @ Request Query Syntax

```ini
# Access query parameters
page: @request.query.page
limit: @request.query.limit
search: @request.query.search

# With fallbacks
page: @request.query.page ?? 1
limit: @request.query.limit ?? 10
```

## 🧑‍💻 JavaScript Example: Using @ Request Query

```javascript
// config/api.tsk
api {
    # Query parameters
    page: @request.query.page ?? 1
    limit: @request.query.limit ?? 10
    search: @request.query.search ?? ""
    sort: @request.query.sort ?? "created_at"
    order: @request.query.order ?? "desc"
    
    # Pagination
    offset: (@page - 1) * @limit
    
    # Search configuration
    search_enabled: @if(@search != "", true, false)
    search_fields: ["name", "email", "description"]
    
    # Sorting
    sort_field: @if(@sort == "name", "name", @if(@sort == "email", "email", "created_at"))
    sort_order: @if(@order == "asc", "ASC", "DESC")
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
app.get('/api/users', async (req, res) => {
    const { api } = req.apiConfig;
    
    // Build query based on config
    const query = `
        SELECT * FROM users 
        WHERE (@search_enabled ? name ILIKE ? OR email ILIKE ? : 1=1)
        ORDER BY @sort_field @sort_order
        LIMIT @limit OFFSET @offset
    `;
    
    const params = api.search_enabled ? 
        [`%${api.search}%`, `%${api.search}%`] : [];
    
    // Execute query...
    res.json({ 
        page: api.page,
        limit: api.limit,
        search: api.search,
        sort: api.sort_field,
        order: api.sort_order
    });
});
```

## 🔍 Query Parameter Access

- `@request.query.param_name` - Access specific parameter
- `@request.query.param_name ?? default` - With fallback
- `@request.query` - All query parameters

## 🚦 Best Practices
- Always provide fallbacks for query parameters
- Validate and sanitize query parameters
- Use query parameters for filtering and pagination

## 🛡️ Security Note
Never trust query parameters without validation—always sanitize user input.

## 📚 Next Steps
- [@ Request Post](037-at-request-post-javascript.md)
- [@ Request Headers](038-at-request-headers-javascript.md)

**Ready to build query-aware configs in TuskLang!** 
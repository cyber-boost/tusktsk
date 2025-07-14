# #web - Web Endpoint Directive

The `#web` directive creates web endpoints for handling HTTP requests and rendering HTML responses, perfect for building web applications and sites.

## Basic Syntax

```tusk
# Simple web page
#web / {
    @render("home.tusk", {
        title: "Welcome",
        message: "Hello, World!"
    })
}

# Direct HTML response
#web /about {
    @response.body: """
    <html>
        <body>
            <h1>About Us</h1>
            <p>Welcome to our site!</p>
        </body>
    </html>
    """
}

# Dynamic content
#web /time {
    current_time: @date("Y-m-d H:i:s")
    @render("time.tusk", {time: current_time})
}
```

## Route Parameters

```tusk
# Single parameter
#web /user/{username} {
    user: @User.findBy("username", @params.username)
    
    if (!user) {
        @response.status: 404
        @render("errors/404.tusk", {
            message: "User not found"
        })
        return
    }
    
    @render("profile.tusk", {user: user})
}

# Multiple parameters
#web /blog/{year}/{month}/{slug} {
    post: @Post.where("year", @params.year)
                .where("month", @params.month)
                .where("slug", @params.slug)
                .first()
    
    @render("blog/post.tusk", {post: post})
}

# Optional parameters
#web /products/{category?} {
    products: @params.category 
        ? @Product.where("category", @params.category).get()
        : @Product.all()
    
    @render("products.tusk", {
        products: products,
        category: @params.category
    })
}

# Wildcard routes
#web /docs/{path*} {
    # Matches /docs/getting-started/installation
    doc_path: @params.path  # "getting-started/installation"
    @render_documentation(doc_path)
}
```

## HTTP Methods

```tusk
# GET request (default)
#web /contact {
    @render("contact-form.tusk")
}

# POST request
#web /contact method: POST {
    # Process form submission
    data: @request.post
    
    # Validate
    errors: @validate(data, {
        name: "required",
        email: "required|email",
        message: "required|min:10"
    })
    
    if (errors) {
        @render("contact-form.tusk", {
            errors: errors,
            old: data
        })
        return
    }
    
    # Send email
    @send_contact_email(data)
    
    # Redirect with success message
    @session.flash: "Thank you for your message!"
    @redirect("/contact")
}

# Multiple methods
#web /resource method: [GET, POST] {
    if (@request.method == "GET") {
        @render("form.tusk")
    } else {
        # Process POST
        @process_form(@request.post)
        @redirect("/success")
    }
}
```

## Templates and Rendering

```tusk
# Basic template rendering
#web /dashboard {
    user: @auth.user
    stats: @get_user_stats(user.id)
    
    @render("dashboard.tusk", {
        user: user,
        stats: stats,
        active_page: "dashboard"
    })
}

# Layout inheritance
#web /page {
    @render("pages/content.tusk", {
        layout: "layouts/main.tusk",
        title: "Page Title",
        content: @get_page_content()
    })
}

# Conditional rendering
#web /profile {
    if (!@auth.check()) {
        @redirect("/login")
        return
    }
    
    if (@auth.user.is_admin) {
        @render("admin/profile.tusk", {user: @auth.user})
    } else {
        @render("user/profile.tusk", {user: @auth.user})
    }
}

# Render with different engines
#web /markdown {
    content: @file.read("content.md")
    html: @markdown_to_html(content)
    
    @render("wrapper.tusk", {
        content: html,
        render_as_html: true
    })
}
```

## Form Handling

```tusk
# Display form
#web /register {
    @render("auth/register.tusk", {
        countries: @Country.all(),
        old: @session.old_input || {}
    })
}

# Process form submission
#web /register method: POST {
    data: @request.post
    
    # Validate
    validation: @validate(data, {
        username: "required|alpha_num|unique:users",
        email: "required|email|unique:users",
        password: "required|min:8|confirmed",
        country: "required|exists:countries,code"
    })
    
    if (!validation.passes) {
        @session.errors: validation.errors
        @session.old_input: data
        @redirect("/register")
        return
    }
    
    # Create user
    user: @User.create(validation.data)
    
    # Auto login
    @auth.login(user)
    
    # Redirect to dashboard
    @redirect("/dashboard")
}

# File upload form
#web /upload method: POST {
    file: @request.files.document
    
    if (!file) {
        @session.error: "Please select a file"
        @redirect("/upload-form")
        return
    }
    
    # Validate file
    if (!@in_array(file.extension, ["pdf", "doc", "docx"])) {
        @session.error: "Invalid file type"
        @redirect("/upload-form")
        return
    }
    
    # Store file
    path: @storage.put("documents", file)
    
    # Save to database
    @Document.create({
        name: file.name,
        path: path,
        size: file.size,
        user_id: @auth.id
    })
    
    @session.success: "File uploaded successfully"
    @redirect("/documents")
}
```

## Session and Cookies

```tusk
# Session management
#web /preferences method: POST {
    # Store in session
    @session.preferences: @request.post
    @session.flash("success", "Preferences updated")
    
    @redirect("/settings")
}

# Flash messages
#web /settings {
    @render("settings.tusk", {
        preferences: @session.preferences || @default_preferences(),
        flash: @session.pull("flash")  # Get and remove
    })
}

# Cookie handling
#web /theme/{theme} {
    if (@in_array(@params.theme, ["light", "dark", "auto"])) {
        @cookie.set("theme", @params.theme, {
            expires: 365 * 24 * 60 * 60,  # 1 year
            path: "/",
            secure: true,
            httponly: true
        })
    }
    
    @redirect(@request.headers.referer || "/")
}
```

## Authentication

```tusk
# Login page
#web /login {
    if (@auth.check()) {
        @redirect("/dashboard")
        return
    }
    
    @render("auth/login.tusk", {
        error: @session.pull("error"),
        intended: @session.get("url.intended")
    })
}

# Login processing
#web /login method: POST {
    credentials: {
        email: @request.post.email,
        password: @request.post.password
    }
    
    if (@auth.attempt(credentials, @request.post.remember)) {
        # Regenerate session
        @session.regenerate()
        
        # Redirect to intended URL or dashboard
        intended: @session.pull("url.intended", "/dashboard")
        @redirect(intended)
    } else {
        @session.flash("error", "Invalid credentials")
        @redirect("/login")
    }
}

# Protected routes
#web /admin/* middleware: [auth, admin] {
    # All /admin/* routes require authentication and admin role
    @handle_admin_request()
}

# Logout
#web /logout method: POST {
    @auth.logout()
    @session.invalidate()
    @redirect("/")
}
```

## Static Assets

```tusk
# Serve static files
#web /assets/{file*} {
    file_path: @public_path("assets/" + @params.file)
    
    if (!@file.exists(file_path)) {
        @response.status: 404
        return
    }
    
    # Set appropriate headers
    mime_type: @mime_type(file_path)
    @response.headers: {
        "Content-Type": mime_type,
        "Cache-Control": "public, max-age=31536000",
        "ETag": @md5_file(file_path)
    }
    
    # Check if not modified
    if (@request.headers["if-none-match"] == @response.headers.ETag) {
        @response.status: 304
        return
    }
    
    @response.body: @file.read(file_path)
}

# Asset versioning
#web /build/{hash}/{file*} {
    # Strip hash from URL and serve file
    @serve_static("build/" + @params.file, {
        cache: "1 year",
        immutable: true
    })
}
```

## AJAX Handling

```tusk
# Detect AJAX requests
#web /content/{section} {
    content: @get_section_content(@params.section)
    
    if (@request.is_ajax()) {
        # Return just the content for AJAX
        @response.body: content
    } else {
        # Return full page for regular requests
        @render("layout.tusk", {
            content: content,
            section: @params.section
        })
    }
}

# JSON responses for AJAX
#web /search {
    query: @request.query.q
    
    if (@request.wants_json()) {
        results: @search(query)
        @json(results)
    } else {
        @render("search.tusk", {
            query: query,
            results: @search(query)
        })
    }
}
```

## Error Handling

```tusk
# Custom error pages
#web /error/{code} {
    @response.status: @params.code
    
    @render("errors/" + @params.code + ".tusk", {
        message: @get_error_message(@params.code)
    })
}

# 404 handler
#web * {
    # Catch-all route (must be last)
    @response.status: 404
    @render("errors/404.tusk", {
        path: @request.path
    })
}

# Error handling in routes
#web /risky {
    try {
        data: @risky_operation()
        @render("success.tusk", {data: data})
        
    } catch (Exception e) {
        @log.error("Route error", {
            path: @request.path,
            error: e.message
        })
        
        @response.status: 500
        @render("errors/500.tusk", {
            message: @env.debug ? e.message : "Something went wrong"
        })
    }
}
```

## Middleware

```tusk
# Apply middleware to routes
#web /account/* middleware: [auth, verified] {
    # All account routes require auth and email verification
}

# Inline middleware logic
#web /premium {
    # Check subscription
    if (!@auth.check() || !@auth.user.has_active_subscription()) {
        @session.put("url.intended", @request.url)
        @redirect("/subscribe")
        return
    }
    
    @render("premium/content.tusk")
}

# Group middleware
#group middleware: [web, auth] {
    #web /dashboard {
        @render("dashboard.tusk")
    }
    
    #web /profile {
        @render("profile.tusk")
    }
}
```

## Response Types

```tusk
# HTML response (default)
#web /page {
    @render("page.tusk")
}

# File download
#web /download/{file} {
    file_path: @storage_path("downloads/" + @params.file)
    
    if (!@file.exists(file_path)) {
        abort(404)
    }
    
    @response.download(file_path, "document.pdf")
}

# Streaming response
#web /stream {
    @response.stream(() => {
        for (i: 0; i < 100; i++) {
            @response.write("Data chunk " + i + "\n")
            @flush()
            @sleep(0.1)
        }
    })
}

# Redirect response
#web /old-page {
    @redirect("/new-page", 301)  # Permanent redirect
}
```

## Best Practices

1. **Use semantic routes** - Make URLs meaningful and RESTful
2. **Validate all input** - Never trust user input
3. **Handle errors gracefully** - Provide helpful error pages
4. **Use CSRF protection** - Protect against cross-site attacks
5. **Implement proper authentication** - Secure sensitive pages
6. **Cache when possible** - Improve performance
7. **Follow PRG pattern** - POST-Redirect-GET for forms
8. **Use middleware** - Keep routes clean

## Related Topics

- `hash-api-directive` - API endpoints
- `templates` - Template rendering
- `forms` - Form handling
- `authentication` - User authentication
- `middleware` - Request middleware
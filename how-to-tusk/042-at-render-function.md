# @render() - Template Rendering

The `@render()` function loads and processes TuskLang templates, enabling dynamic content generation with template inheritance and partials.

## Basic Syntax

```tusk
# Render a template
output: @render("templates/page.tusk")

# Render with data
output: @render("templates/user.tusk", {
    name: "John Doe"
    email: "john@example.com"
})

# Render inline template
output: @render(template_string, data, {inline: true})
```

## Template Files

```tusk
# templates/layout.tusk
<!DOCTYPE html>
<html>
<head>
    <title>{@title|"Default Title"}</title>
    <meta charset="utf-8">
    {@head_extra}
</head>
<body>
    <header>
        {@render("partials/header.tusk", {user: @user})}
    </header>
    
    <main>
        {@content}
    </main>
    
    <footer>
        {@render("partials/footer.tusk")}
    </footer>
</body>
</html>

# templates/page.tusk
@extend("layout.tusk", {
    title: @page_title
    head_extra: '<link rel="stylesheet" href="/css/page.css">'
    content: @page_content
})
```

## Passing Data

```tusk
# Controller
#web /users/{id} {
    user: @query("SELECT * FROM users WHERE id = ?", [@id])
    posts: @query("SELECT * FROM posts WHERE user_id = ?", [@id])
    
    @render("templates/profile.tusk", {
        user: @user
        posts: @posts
        current_user: @session.user
        is_owner: @session.user.id == @id
    })
}

# templates/profile.tusk
<div class="profile">
    <h1>{@user.name}</h1>
    <p>{@user.bio}</p>
    
    @if(@is_owner) {
        <a href="/profile/edit">Edit Profile</a>
    }
    
    <div class="posts">
        @foreach(@posts as @post) {
            {@render("partials/post.tusk", {post: @post})}
        }
    </div>
</div>
```

## Template Inheritance

```tusk
# Base template: templates/base.tusk
<!DOCTYPE html>
<html>
<head>
    <title>{@block('title')}Default Title{@endblock}</title>
    {@block('head')}{@endblock}
</head>
<body>
    {@block('content')}{@endblock}
    
    <script src="/js/app.js"></script>
    {@block('scripts')}{@endblock}
</body>
</html>

# Child template: templates/home.tusk
@extends('base.tusk')

@block('title')Home Page{@endblock}

@block('head')
    <link rel="stylesheet" href="/css/home.css">
{@endblock}

@block('content')
    <h1>Welcome Home</h1>
    <p>This is the home page content.</p>
{@endblock}

@block('scripts')
    <script src="/js/home.js"></script>
{@endblock}
```

## Partials and Components

```tusk
# Reusable card component
# templates/components/card.tusk
<div class="card {@class}">
    @if(@image) {
        <img src="{@image}" alt="{@title}">
    }
    <div class="card-body">
        <h3>{@title}</h3>
        <p>{@description}</p>
        @if(@actions) {
            <div class="card-actions">
                {@actions}
            </div>
        }
    </div>
</div>

# Using the component
products: @query("SELECT * FROM products LIMIT 6")

<div class="product-grid">
    @foreach(@products as @product) {
        {@render("components/card.tusk", {
            title: @product.name
            description: @product.description
            image: @product.image_url
            class: "product-card"
            actions: '<button onclick="addToCart(' + @product.id + ')">Add to Cart</button>'
        })}
    }
</div>
```

## Conditional Rendering

```tusk
# Render different templates based on conditions
#web /content/{type}/{id} {
    content: @query("SELECT * FROM content WHERE id = ?", [@id])
    
    # Choose template based on type
    template: @switch(@type) {
        case "article": "templates/article.tusk"
        case "video": "templates/video.tusk"
        case "gallery": "templates/gallery.tusk"
        default: "templates/generic.tusk"
    }
    
    @render(@template, {
        content: @content
        type: @type
    })
}

# In template
@if(@user.role == "admin") {
    {@render("partials/admin-tools.tusk")}
} elseif(@user.role == "editor") {
    {@render("partials/editor-tools.tusk")}
}
```

## Loop Rendering

```tusk
# Render collections
# templates/list.tusk
<ul class="{@list_class|'default-list'}">
    @foreach(@items as @index => @item) {
        <li class="{@item_class} {@index % 2 ? 'odd' : 'even'}">
            {@render(@item_template|"partials/list-item.tusk", {
                item: @item
                index: @index
                is_first: @index == 0
                is_last: @index == @count(@items) - 1
            })}
        </li>
    }
</ul>

# Using the list
categories: @query("SELECT * FROM categories ORDER BY name")

{@render("templates/list.tusk", {
    items: @categories
    list_class: "category-list"
    item_class: "category-item"
    item_template: "partials/category.tusk"
})}
```

## Email Templates

```tusk
# Send email with template
#api /send-welcome-email {
    user_id: @request.post.user_id
    user: @query("SELECT * FROM users WHERE id = ?", [@user_id])
    
    # Render email template
    email_html: @render("emails/welcome.tusk", {
        user: @user
        activation_link: @base_url + "/activate/" + @user.activation_token
        year: @date("Y")
    })
    
    # Send email
    @send_email({
        to: @user.email
        subject: "Welcome to " + @app_name
        html: @email_html
        text: @strip_tags(@email_html)
    })
}

# emails/welcome.tusk
@extends('emails/layout.tusk')

@block('content')
    <h1>Welcome, {@user.name}!</h1>
    <p>Thank you for joining {@app_name}.</p>
    <p>Please activate your account by clicking the link below:</p>
    <a href="{@activation_link}" class="button">Activate Account</a>
{@endblock}
```

## Caching Rendered Templates

```tusk
# Cache rendered output
#web /expensive-page {
    cache_key: "page:expensive:" + @request.uri
    cached: @cache.get(@cache_key)
    
    @if(@cached) {
        @output(@cached)
    } else {
        # Expensive data operations
        data: @complex_data_calculation()
        
        # Render template
        html: @render("templates/expensive.tusk", {data: @data})
        
        # Cache for 1 hour
        @cache.set(@cache_key, @html, 3600)
        
        @output(@html)
    }
}
```

## Error Handling

```tusk
# Safe template rendering
try_render: {
    template_path: "templates/" + @page_name + ".tusk"
    
    @if(@file_exists(@template_path)) {
        @try {
            output: @render(@template_path, @page_data)
        } catch (error) {
            @log_error("Template render failed", {
                template: @template_path
                error: @error
            })
            output: @render("templates/error.tusk", {
                message: "Page temporarily unavailable"
            })
        }
    } else {
        output: @render("templates/404.tusk")
    }
}
```

## Dynamic Template Loading

```tusk
# Plugin system
#web /plugin/{plugin}/{page} {
    # Load plugin template
    plugin_path: "plugins/" + @plugin + "/templates/" + @page + ".tusk"
    
    @if(@file_exists(@plugin_path) && @is_plugin_active(@plugin)) {
        # Load plugin configuration
        config: @load_plugin_config(@plugin)
        
        @render(@plugin_path, {
            config: @config
            user: @session.user
            params: @request.all
        })
    } else {
        @response.status: 404
        @render("templates/404.tusk", {
            message: "Plugin or page not found"
        })
    }
}
```

## Template Helpers

```tusk
# Register template helpers
template_helpers: {
    # Format currency
    money: (@amount) => {
        "$" + @number_format(@amount, 2)
    }
    
    # Format date
    date_format: (@date, @format) => {
        @strftime(@format, @strtotime(@date))
    }
    
    # Truncate text
    truncate: (@text, @length: 100) => {
        @strlen(@text) > @length 
            ? @substr(@text, 0, @length) + "..."
            : @text
    }
}

# Use in template
<p>Price: {@money(@product.price)}</p>
<p>Posted: {@date_format(@post.created_at, "%B %d, %Y")}</p>
<p>{@truncate(@article.content, 200)}</p>
```

## Best Practices

1. **Organize templates logically** - Use folders for different types
2. **Use partials for reusability** - Don't repeat yourself
3. **Pass only needed data** - Don't send entire objects if not needed
4. **Escape output by default** - Prevent XSS attacks
5. **Cache expensive renders** - Improve performance
6. **Handle missing templates** - Graceful error handling

## Related Functions

- `@include()` - Include raw files
- `@render_string()` - Render string templates
- `@extends()` - Template inheritance
- `@block()` - Define template blocks
- `@escape()` - Escape HTML output
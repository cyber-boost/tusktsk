# @ Operator Nesting

TuskLang allows sophisticated nesting of @ operators, enabling complex data transformations and operations while maintaining readability.

## Basic Nesting

```tusk
# Nested property access
city: @users[@index].address.city

# Nested function calls
result: @calculate(@sum(@array.map(x => @x.value)))

# Mixed nesting
formatted: @upper(@trim(@user.name|@default_name))
```

## Array and Object Nesting

```tusk
# Nested array operations
matrix_sum: @array.map(row => @sum(@row.filter(x => @x > 0)))

# Deep object creation
user_profile: {
    name: @user.name
    stats: {
        posts: @count(@posts.where("user_id", @user.id))
        comments: @count(@comments.where("user_id", @user.id))
        likes: @sum(@posts.map(p => @p.likes))
    }
    recent_activity: @activities
        .where("user_id", @user.id)
        .orderBy("created_at", "desc")
        .limit(5)
        .map(a => {
            type: @a.type
            timestamp: @format_date(@a.created_at)
            details: @json_decode(@a.data)
        })
}
```

## Conditional Nesting

```tusk
# Nested ternary operators
status: @user.active ? 
    (@user.verified ? "active-verified" : "active-unverified") : 
    (@user.suspended ? "suspended" : "inactive")

# Nested if conditions
access_level: @if(@user.is_admin) {
    "admin"
} elseif(@user.roles.includes("editor")) {
    @if(@user.department == "news") {
        "news-editor"
    } else {
        "general-editor"
    }
} else {
    @user.is_premium ? "premium-user" : "basic-user"
}

# Complex condition nesting
can_edit: @user && (
    @user.id == @post.author_id || 
    @user.roles.some(r => @allowed_roles.includes(@r)) ||
    (@user.is_moderator && @post.status != "locked")
)
```

## Function Composition Nesting

```tusk
# Nested function composition
process_data: (data) => {
    return @validate(
        @transform(
            @normalize(
                @clean(@data)
            )
        )
    )
}

# Using pipe for clarity
process_data_clear: (data) => {
    return @data
        |> @clean()
        |> @normalize()
        |> @transform()
        |> @validate()
}

# Nested map/filter/reduce
result: @users
    .map(u => {
        user: @u
        orders: @orders.filter(o => @o.user_id == @u.id)
        total: @orders.reduce((sum, o) => @sum + @o.total, 0)
    })
    .filter(u => @u.total > 1000)
    .sort((a, b) => @b.total - @a.total)
```

## Query Nesting

```tusk
# Subqueries in TuskLang
high_value_customers: @query("
    SELECT * FROM users 
    WHERE id IN (?)
", [
    @query("
        SELECT user_id 
        FROM orders 
        GROUP BY user_id 
        HAVING SUM(total) > ?
    ", [1000])
])

# Nested query building
users_with_recent_orders: @User
    .whereIn("id", 
        @Order
            .select("user_id")
            .where("created_at", ">", @last_month)
            .where("status", "completed")
            .distinct()
    )
    .with(["profile", "preferences"])
    .get()
```

## Template Nesting

```tusk
# Nested template rendering
page_html: @render("layouts/main.tusk", {
    title: @page.title
    content: @render("pages/" + @page.template + ".tusk", {
        page: @page
        widgets: @page.widgets.map(w => 
            @render("widgets/" + @w.type + ".tusk", @w.data)
        )
    })
    sidebar: @render("partials/sidebar.tusk", {
        menu: @build_menu(@user.role)
        recent: @get_recent_items(@user.id)
    })
})

# Nested component rendering
form_field: @render("form/field.tusk", {
    label: @field.label
    input: @render("form/inputs/" + @field.type + ".tusk", {
        name: @field.name
        value: @old(@field.name)|@field.default
        attributes: @merge(@field.attributes, {
            class: @errors.has(@field.name) ? "error" : ""
        })
    })
    error: @errors.has(@field.name) ? 
        @render("form/error.tusk", {message: @errors.first(@field.name)}) : 
        ""
})
```

## Cache Nesting

```tusk
# Nested cache operations
user_dashboard: @cache.remember("dashboard:" + @user.id, 3600, () => {
    return {
        profile: @cache.remember("user:" + @user.id, 7200, () => 
            @User.with(["profile", "settings"]).find(@user.id)
        )
        stats: @cache.remember("stats:" + @user.id, 1800, () => {
            posts: @count(@Post.where("user_id", @user.id))
            comments: @count(@Comment.where("user_id", @user.id))
            likes: @cache.remember("likes:" + @user.id, 900, () =>
                @Like.where("user_id", @user.id).count()
            )
            return {posts: @posts, comments: @comments, likes: @likes}
        })
        recent_activity: @get_recent_activity(@user.id)
    }
})
```

## Error Handling Nesting

```tusk
# Nested try-catch blocks
process_payment: (order) => {
    @try {
        payment_result: @try {
            return @payment_gateway.charge({
                amount: @order.total
                currency: @order.currency
                source: @order.payment_token
            })
        } catch (PaymentException e) {
            # Try backup payment method
            @try {
                return @backup_gateway.charge(@order)
            } catch (BackupException be) {
                @log.error("All payment methods failed", {
                    primary_error: @e.message
                    backup_error: @be.message
                })
                @throw new PaymentFailedException("Payment processing failed")
            }
        }
        
        # Process successful payment
        @order.markAsPaid(@payment_result.transaction_id)
        
    } catch (Exception e) {
        @handle_payment_failure(@order, @e)
    }
}
```

## Async Operation Nesting

```tusk
# Nested async operations
load_user_data: async (user_id) => {
    user: await @User.find(@user_id)
    
    # Parallel nested loading
    [posts, comments, likes]: await @Promise.all([
        @Post.where("user_id", @user.id).get(),
        @Comment.where("user_id", @user.id).get(),
        @Like.where("user_id", @user.id).get()
    ])
    
    # Process nested data
    enhanced_posts: await @Promise.all(
        @posts.map(async (post) => {
            post_likes: await @Like.where("post_id", @post.id).count()
            post_comments: await @Comment.where("post_id", @post.id)
                .with("author")
                .limit(3)
                .get()
            
            return {
                ...@post
                likes_count: @post_likes
                recent_comments: @post_comments
                author: @user
            }
        })
    )
    
    return {
        user: @user
        posts: @enhanced_posts
        total_comments: @comments.length
        total_likes: @likes.length
    }
}
```

## Validation Nesting

```tusk
# Nested validation rules
validation_rules: {
    user: {
        name: "required|string|max:255"
        email: "required|email|unique:users"
        profile: {
            bio: "nullable|string|max:500"
            avatar: "nullable|image|max:2048"
            social: {
                twitter: "nullable|regex:/^@[A-Za-z0-9_]+$/"
                github: "nullable|url"
                website: "nullable|url"
            }
        }
        preferences: {
            notifications: {
                email: "boolean"
                push: "boolean"
                frequency: "in:instant,daily,weekly"
            }
        }
    }
}

# Nested validation execution
validate_nested: (data) => {
    errors: {}
    
    @foreach(@validation_rules.user as @field => @rules) {
        @if(@is_array(@rules)) {
            # Nested validation
            @foreach(@rules as @subfield => @subrules) {
                value: @data[@field][@subfield]
                result: @validate(@value, @subrules)
                @if(!@result.passes) {
                    errors[@field + "." + @subfield]: @result.errors
                }
            }
        } else {
            # Direct validation
            result: @validate(@data[@field], @rules)
            @if(!@result.passes) {
                errors[@field]: @result.errors
            }
        }
    }
    
    return @empty(@errors) ? {passes: true} : {passes: false, errors: @errors}
}
```

## Dynamic Property Nesting

```tusk
# Dynamic nested property access
get_nested_value: (object, path) => {
    segments: @explode(".", @path)
    
    return @segments.reduce((current, segment) => {
        return @current?[@segment]
    }, @object)
}

# Usage
value: @get_nested_value(@user, "profile.settings.theme.color")

# Dynamic nested property setting
set_nested_value: (object, path, value) => {
    segments: @explode(".", @path)
    last: @array_pop(@segments)
    
    current: @object
    @foreach(@segments as @segment) {
        @if(!@isset(@current[@segment])) {
            current[@segment]: {}
        }
        current: @current[@segment]
    }
    
    current[@last]: @value
    return @object
}
```

## Performance Considerations

```tusk
# Optimize nested operations
# Bad: Multiple nested queries
users_inefficient: @users.map(u => {
    posts: @Post.where("user_id", @u.id).get()  # N+1 problem
    return {...@u, posts: @posts}
})

# Good: Eager loading
users_efficient: @User.with("posts").get()

# Bad: Deep nesting with repeated calculations
result_bad: @data.map(d => 
    @expensive_calc(@expensive_calc(@expensive_calc(@d)))
)

# Good: Calculate once and reuse
result_good: @data.map(d => {
    step1: @expensive_calc(@d)
    step2: @expensive_calc(@step1)
    step3: @expensive_calc(@step2)
    return @step3
})
```

## Best Practices

1. **Limit nesting depth** - Too deep becomes unreadable
2. **Use intermediate variables** - Break complex nesting into steps
3. **Consider performance** - Nested loops and queries can be expensive
4. **Handle null values** - Use optional chaining (?.) for safety
5. **Extract complex logic** - Move deeply nested code to functions
6. **Document complex nesting** - Add comments explaining the logic

## Related Features

- `@pipe()` - Linear function composition
- `@compose()` - Function composition
- `@tap()` - Debugging nested operations
- `@get()` - Safe nested property access
- `@set()` - Safe nested property setting
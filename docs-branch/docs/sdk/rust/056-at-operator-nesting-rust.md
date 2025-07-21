# 🪆 @ Operator Nesting in Rust

TuskLang allows sophisticated nesting of @ operators in Rust, enabling complex data transformations and operations while maintaining readability and type safety.

## Basic Nesting

```rust
// Nested property access
let city = @users[@index].address.city;

// Nested function calls
let result = @calculate(@sum(@array.iter().map(|x| x.value)));

// Mixed nesting
let formatted = @upper(@trim(@user.name.as_deref().unwrap_or(@default_name)));
```

## Array and Object Nesting

```rust
// Nested array operations
let matrix_sum = @array
    .iter()
    .map(|row| row.iter().filter(|&&x| x > 0).sum::<i32>())
    .sum::<i32>();

// Deep object creation
let user_profile = UserProfile {
    name: @user.name.clone(),
    stats: UserStats {
        posts: @posts.iter().filter(|p| p.user_id == @user.id).count(),
        comments: @comments.iter().filter(|c| c.user_id == @user.id).count(),
        likes: @posts.iter().map(|p| p.likes).sum(),
    },
    recent_activity: @activities
        .iter()
        .filter(|a| a.user_id == @user.id)
        .sorted_by(|a, b| b.created_at.cmp(&a.created_at))
        .take(5)
        .map(|a| Activity {
            activity_type: a.activity_type.clone(),
            timestamp: @format_date(a.created_at),
            details: serde_json::from_str(&a.data).unwrap_or_default(),
        })
        .collect::<Vec<_>>(),
};
```

## Conditional Nesting

```rust
// Nested ternary operators
let status = if @user.active {
    if @user.verified {
        "active-verified"
    } else {
        "active-unverified"
    }
} else {
    if @user.suspended {
        "suspended"
    } else {
        "inactive"
    }
};

// Nested if conditions
let access_level = if @user.is_admin {
    "admin"
} else if @user.roles.contains(&"editor".to_string()) {
    if @user.department == "news" {
        "news-editor"
    } else {
        "general-editor"
    }
} else {
    if @user.is_premium {
        "premium-user"
    } else {
        "basic-user"
    }
};

// Complex condition nesting
let can_edit = @user.is_some() && (
    @user.as_ref().unwrap().id == @post.author_id ||
    @user.as_ref().unwrap().roles.iter().any(|r| @allowed_roles.contains(r)) ||
    (@user.as_ref().unwrap().is_moderator && @post.status != "locked")
);
```

## Function Composition Nesting

```rust
// Nested function composition
fn process_data(data: &str) -> Result<String, Box<dyn std::error::Error>> {
    Ok(@validate(
        @transform(
            @normalize(
                @clean(data)?
            )?
        )?
    )?)
}

// Using pipe for clarity
fn process_data_clear(data: &str) -> Result<String, Box<dyn std::error::Error>> {
    Ok(data
        .pipe(|d| @clean(d))
        .pipe(|d| @normalize(d))
        .pipe(|d| @transform(d))
        .pipe(|d| @validate(d))?)
}

// Nested map/filter/reduce
let result = @users
    .iter()
    .map(|u| {
        let orders: Vec<_> = @orders.iter().filter(|o| o.user_id == u.id).collect();
        let total: f64 = orders.iter().map(|o| o.total).sum();
        UserWithOrders {
            user: u.clone(),
            orders,
            total,
        }
    })
    .filter(|u| u.total > 1000.0)
    .sorted_by(|a, b| b.total.partial_cmp(&a.total).unwrap())
    .collect::<Vec<_>>();
```

## Query Nesting

```rust
// Subqueries in TuskLang
let high_value_customers = @query("
    SELECT * FROM users 
    WHERE id IN (?)
", vec![
    @query("
        SELECT user_id 
        FROM orders 
        GROUP BY user_id 
        HAVING SUM(total) > ?
    ", vec![1000.0])
])?;

// Nested query building
let users_with_recent_orders = @User
    .where_in("id", 
        @Order
            .select("user_id")
            .where("created_at", ">", @last_month)
            .where("status", "completed")
            .distinct()
    )
    .with(vec!["profile", "preferences"])
    .get()?;
```

## Template Nesting

```rust
// Nested template rendering
let page_html = @render("layouts/main.tusk", serde_json::json!({
    "title": @page.title,
    "content": @render(&format!("pages/{}.tusk", @page.template), serde_json::json!({
        "page": @page,
        "widgets": @page.widgets.iter().map(|w| 
            @render(&format!("widgets/{}.tusk", w.widget_type), &w.data)
        ).collect::<Vec<_>>(),
    })),
    "sidebar": @render("partials/sidebar.tusk", serde_json::json!({
        "menu": @build_menu(@user.role),
        "recent": @get_recent_items(@user.id),
    })),
}))?;

// Nested component rendering
let form_field = @render("form/field.tusk", serde_json::json!({
    "label": @field.label,
    "input": @render(&format!("form/inputs/{}.tusk", @field.field_type), serde_json::json!({
        "name": @field.name,
        "value": @old(@field.name).unwrap_or_else(|| @field.default.clone()),
        "attributes": @merge(@field.attributes, serde_json::json!({
            "class": if @errors.has(@field.name) { "error" } else { "" }
        })),
    })),
    "error": if @errors.has(@field.name) {
        @render("form/error.tusk", serde_json::json!({
            "message": @errors.first(@field.name)
        }))
    } else {
        "".to_string()
    },
}))?;
```

## Cache Nesting

```rust
// Nested cache operations
let user_dashboard = @cache.remember(&format!("dashboard:{}", @user.id), Duration::from_secs(3600), || {
    UserDashboard {
        profile: @cache.remember(&format!("user:{}", @user.id), Duration::from_secs(7200), || 
            @User.with(vec!["profile", "settings"]).find(@user.id)
        ),
        stats: @cache.remember(&format!("stats:{}", @user.id), Duration::from_secs(1800), || {
            let posts = @Post.iter().filter(|p| p.user_id == @user.id).count();
            let comments = @Comment.iter().filter(|c| c.user_id == @user.id).count();
            let likes = @cache.remember(&format!("likes:{}", @user.id), Duration::from_secs(900), || {
                @Like.iter().filter(|l| l.user_id == @user.id).count()
            });
            UserStats { posts, comments, likes }
        }),
        recent_activity: @get_recent_activity(@user.id),
    }
});
```

## Error Handling Nesting

```rust
// Nested Result handling
fn process_payment(order: &Order) -> Result<PaymentResult, Box<dyn std::error::Error>> {
    let payment_result = @try {
        @payment_gateway.charge(PaymentRequest {
            amount: order.total,
            currency: order.currency.clone(),
            customer_id: order.customer_id,
        })?
    } catch |error| {
        @log("Payment failed: {}", error);
        return Err(error.into());
    };

    @try {
        @update_order_status(order.id, "paid")?;
        @send_confirmation_email(order.customer_id)?;
        Ok(payment_result)
    } catch |error| {
        @log("Post-payment processing failed: {}", error);
        @rollback_payment(payment_result.transaction_id)?;
        Err(error.into())
    }
}
```

## Async Nesting

```rust
// Nested async operations
async fn process_user_data(user_id: u32) -> Result<UserData, Box<dyn std::error::Error>> {
    let user_data = @try {
        let user = @fetch_user(user_id).await?;
        let posts = @fetch_user_posts(user_id).await?;
        let comments = @fetch_user_comments(user_id).await?;
        
        UserData {
            user,
            posts,
            comments,
        }
    } catch |error| {
        @log("Failed to fetch user data: {}", error);
        return Err(error.into());
    };

    @try {
        @process_and_validate(user_data).await?;
        @save_to_database(user_data).await?;
        Ok(user_data)
    } catch |error| {
        @log("Failed to process user data: {}", error);
        Err(error.into())
    }
}
```

## Database Transaction Nesting

```rust
// Nested database transactions
fn create_user_with_profile(user_data: UserData, profile_data: ProfileData) -> Result<User, Box<dyn std::error::Error>> {
    @db.transaction(|tx| {
        let user = @try {
            tx.table("users")
                .insert(user_data)?
        } catch |error| {
            @log("Failed to create user: {}", error);
            return Err(error);
        };

        let profile = @try {
            tx.table("profiles")
                .insert(ProfileData {
                    user_id: user.id,
                    ..profile_data
                })?
        } catch |error| {
            @log("Failed to create profile: {}", error);
            return Err(error);
        };

        @try {
            tx.table("user_preferences")
                .insert(UserPreferences {
                    user_id: user.id,
                    theme: "default".to_string(),
                    notifications: true,
                })?;
            Ok(user)
        } catch |error| {
            @log("Failed to create preferences: {}", error);
            Err(error)
        }
    })
}
```

## Configuration Nesting

```rust
// Nested configuration loading
fn load_application_config() -> Result<AppConfig, Box<dyn std::error::Error>> {
    let base_config = @config.load("base.tusk")?;
    
    let env_config = @try {
        @config.load(&format!("{}.tusk", @env.get("APP_ENV").unwrap_or("development")))?
    } catch |_| {
        @log("Environment config not found, using defaults");
        Config::default()
    };

    let user_config = @try {
        @config.load("user.tusk")?
    } catch |_| {
        @log("User config not found, using defaults");
        Config::default()
    };

    Ok(AppConfig {
        base: base_config,
        environment: env_config,
        user: user_config,
    })
}
```

## Validation Nesting

```rust
// Nested validation
fn validate_user_registration(data: &UserRegistrationData) -> Result<(), ValidationErrors> {
    let mut errors = ValidationErrors::new();

    // Basic validation
    @validate_field(&data.name, "name", &mut errors, |name| {
        if name.is_empty() {
            Err("Name is required".to_string())
        } else if name.len() > 255 {
            Err("Name too long".to_string())
        } else {
            Ok(())
        }
    });

    // Email validation
    @validate_field(&data.email, "email", &mut errors, |email| {
        if !@is_valid_email(email) {
            Err("Invalid email format".to_string())
        } else if @email_exists(email) {
            Err("Email already registered".to_string())
        } else {
            Ok(())
        }
    });

    // Password validation
    @validate_field(&data.password, "password", &mut errors, |password| {
        if password.len() < 8 {
            Err("Password too short".to_string())
        } else if !@password_meets_complexity(password) {
            Err("Password doesn't meet complexity requirements".to_string())
        } else {
            Ok(())
        }
    });

    if errors.is_empty() {
        Ok(())
    } else {
        Err(errors)
    }
}
```

## API Response Nesting

```rust
// Nested API response building
fn build_user_response(user: &User) -> serde_json::Value {
    serde_json::json!({
        "user": {
            "id": user.id,
            "name": user.name,
            "email": user.email,
            "profile": @build_profile_response(&user.profile),
            "stats": @build_stats_response(&user.stats),
            "recent_activity": @build_activity_response(&user.recent_activity),
        },
        "meta": {
            "generated_at": @format_datetime(chrono::Utc::now()),
            "version": @get_api_version(),
        }
    })
}

fn build_profile_response(profile: &Profile) -> serde_json::Value {
    serde_json::json!({
        "bio": profile.bio,
        "avatar": @build_avatar_response(&profile.avatar),
        "social_links": @build_social_links_response(&profile.social_links),
    })
}
```

## Best Practices

### 1. Limit Nesting Depth
```rust
// Avoid deep nesting
// Bad - too deep
let result = @data
    .iter()
    .map(|item| {
        item.process()
            .and_then(|processed| {
                processed.validate()
                    .and_then(|valid| {
                        valid.save()
                            .and_then(|saved| {
                                saved.notify()
                            })
                    })
            })
    })
    .collect::<Vec<_>>();

// Good - break into functions
fn process_item(item: &DataItem) -> Result<ProcessedItem, Box<dyn std::error::Error>> {
    let processed = item.process()?;
    let valid = processed.validate()?;
    let saved = valid.save()?;
    saved.notify()?;
    Ok(saved)
}

let result = @data
    .iter()
    .map(|item| process_item(item))
    .collect::<Vec<_>>();
```

### 2. Use Early Returns
```rust
// Use early returns to reduce nesting
fn process_user(user: &User) -> Result<UserData, Box<dyn std::error::Error>> {
    if !user.is_active() {
        return Err("User is not active".into());
    }

    if user.is_suspended() {
        return Err("User is suspended".into());
    }

    // Main processing logic
    let data = @process_user_data(user)?;
    Ok(data)
}
```

### 3. Leverage Rust's Type System
```rust
// Use Rust's type system for safety
fn get_user_city(user: &User) -> Option<&str> {
    user.address
        .as_ref()?
        .city
        .as_deref()
}
```

### 4. Use Builder Pattern for Complex Objects
```rust
// Use builder pattern for complex nested objects
let user_profile = UserProfileBuilder::new()
    .name(@user.name.clone())
    .stats(|builder| {
        builder
            .posts(@count_user_posts(@user.id))
            .comments(@count_user_comments(@user.id))
            .likes(@sum_user_likes(@user.id))
    })
    .recent_activity(@get_recent_activity(@user.id))
    .build()?;
```

The @ operator nesting in Rust provides powerful composition capabilities while maintaining Rust's safety guarantees and performance characteristics. 
# @ Operator Chaining

TuskLang supports elegant chaining of @ operators, allowing you to compose complex operations in a readable, fluent manner.

## Basic Chaining

```tusk
# Simple property chaining
user_city: @user.address.city

# Method chaining
formatted: @string.trim().lower().replace(" ", "-")

# Mixed chaining
result: @query("SELECT * FROM users").first().name.upper()
```

## Null-Safe Chaining

```tusk
# Safe navigation operator
city: @user?.address?.city

# With fallback
city: @user?.address?.city|"Unknown"

# Multiple levels
manager_email: @employee?.department?.manager?.email|"no-manager@example.com"

# Array access
first_tag: @post?.tags?[0]?.name
```

## Collection Chaining

```tusk
# Array operations
result: @users
    .filter(u => u.active)
    .map(u => u.email)
    .unique()
    .sort()

# More complex example
top_customers: @orders
    .groupBy("customer_id")
    .map((customer_id, orders) => {
        id: @customer_id
        total: @orders.sum("total")
        count: @orders.count()
    })
    .sortBy("total", "desc")
    .limit(10)

# With conditions
active_premium_users: @users
    .where("status", "active")
    .where("subscription", "premium")
    .orderBy("created_at", "desc")
    .get()
```

## Query Builder Chaining

```tusk
# Database query chaining
users: @db.table("users")
    .select(["id", "name", "email"])
    .where("active", true)
    .where("created_at", ">", @last_week)
    .whereIn("role", ["admin", "editor"])
    .orderBy("name")
    .limit(50)
    .get()

# With joins
orders: @db.table("orders")
    .join("users", "orders.user_id", "users.id")
    .join("products", "orders.product_id", "products.id")
    .select([
        "orders.*",
        "users.name as customer_name",
        "products.name as product_name"
    ])
    .where("orders.status", "completed")
    .whereBetween("orders.created_at", [@start_date, @end_date])
    .get()
```

## Transform Chains

```tusk
# String transformations
slug: @title
    .trim()
    .lower()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "")

# Date transformations  
formatted_date: @user.created_at
    .toDate()
    .format("Y-m-d")
    .replace("-", "/")

# Number formatting
price_display: @product.price
    .round(2)
    .format(",")
    .prepend("$")
```

## Conditional Chaining

```tusk
# Chain with conditions
query: @db.table("products")
    .when(@category, (q) => q.where("category_id", @category))
    .when(@min_price, (q) => q.where("price", ">=", @min_price))
    .when(@search, (q) => q.where("name", "like", "%" + @search + "%"))
    .when(@sort == "price", (q) => q.orderBy("price", @direction))
    .get()

# Conditional method calls
result: @data
    .if(@should_filter, filter(x => x.active))
    .if(@should_sort, sort())
    .if(@limit, take(@limit))
```

## Pipeline Chaining

```tusk
# Unix-style pipeline
result: @input
    |> @trim()
    |> @split(",")
    |> @map(x => @int(x))
    |> @filter(x => x > 0)
    |> @sum()

# With custom functions
process_text: (text) => @text
    |> @normalize_whitespace()
    |> @remove_punctuation()
    |> @tokenize()
    |> @remove_stopwords()
    |> @stem()

# Data processing pipeline
report: @raw_data
    |> @validate()
    |> @clean()
    |> @transform()
    |> @aggregate()
    |> @format()
```

## Async Chain Operations

```tusk
# Async/await chaining
result: await @fetch(url)
    .then(response => response.json())
    .then(data => @process(data))
    .then(processed => @save(processed))
    .catch(error => @log_error(error))

# Parallel operations
results: await @Promise.all([
    @fetch_user_data(id),
    @fetch_user_posts(id),
    @fetch_user_stats(id)
]).then(([user, posts, stats]) => {
    user: @user
    posts: @posts
    stats: @stats
})
```

## Cache Chaining

```tusk
# Cache with chaining
user: @cache
    .remember("user:" + @id, 3600)
    .get(() => @db.table("users").find(@id))
    .with(["posts", "comments"])
    .append({calculated_field: @calculate_something()})

# Tagged cache chaining
@cache
    .tags(["users", "user:" + @id])
    .put("user_profile:" + @id, @profile, 3600)
```

## Validation Chaining

```tusk
# Input validation chains
validation: @validator
    .input(@request.all())
    .rules({
        name: "required|string|max:255"
        email: "required|email|unique:users"
        age: "required|integer|min:18"
    })
    .validate()
    .onFail(errors => @response.json({errors}, 422))
    .onPass(data => @create_user(data))
```

## HTTP Client Chaining

```tusk
# HTTP request building
response: @http
    .post("https://api.example.com/users")
    .headers({
        "Authorization": "Bearer " + @token
        "Content-Type": "application/json"
    })
    .timeout(5000)
    .retry(3)
    .body({
        name: @name
        email: @email
    })
    .send()

# Response handling
data: @response
    .ensureSuccess()
    .json()
    .get("data")
    .map(item => @transform(item))
```

## Form Builder Chaining

```tusk
# Form building
form: @form("user")
    .method("POST")
    .action("/users")
    .field("name")
        .type("text")
        .required()
        .placeholder("Enter name")
    .field("email")
        .type("email")
        .required()
        .validate("email")
    .field("role")
        .type("select")
        .options(@roles)
        .default("user")
    .csrf()
    .render()
```

## Event Chaining

```tusk
# Event emitter chaining
@events
    .on("user.created", @send_welcome_email)
    .on("user.created", @track_analytics)
    .on("user.created", @update_stats)
    .emit("user.created", {user: @new_user})

# Promise-based events
result: @events
    .emit_async("process.start", {id: @id})
    .then(() => @do_processing())
    .then(() => @events.emit_async("process.complete", {id: @id}))
```

## Error Handling in Chains

```tusk
# Try-catch chaining
result: @try_chain()
    .attempt(() => @risky_operation())
    .catch(DatabaseException, e => @handle_db_error(e))
    .catch(NetworkException, e => @retry_operation())
    .catch(e => @log_error(e))
    .finally(() => @cleanup())

# Safe chaining with defaults
value: @deep_nested_value()
    ?.property
    ?.subproperty
    ?.method()
    .or(@default_value)
```

## Custom Chain Methods

```tusk
# Extend objects with chainable methods
String.prototype.slugify: () => {
    return @this
        .trim()
        .lower()
        .replace(/[^\w\s-]/g, "")
        .replace(/\s+/g, "-")
}

# Usage
slug: @title.slugify()

# Create chainable wrapper
chainable: (value) => {
    return {
        value: @value
        
        map: (fn) => @chainable(@fn(@value))
        filter: (fn) => @chainable(@fn(@value) ? @value : null)
        tap: (fn) => { @fn(@value); return @this }
        get: () => @value
    }
}

# Use custom chainable
result: @chainable(@data)
    .tap(d => @log("Processing", d))
    .map(d => @transform(d))
    .tap(d => @log("Transformed", d))
    .get()
```

## Best Practices

1. **Keep chains readable** - Break long chains into multiple lines
2. **Use null-safe operators** - Prevent errors with ?. operator
3. **Handle errors appropriately** - Add catch() for async chains
4. **Don't over-chain** - Sometimes separate statements are clearer
5. **Create reusable chains** - Extract common patterns into functions
6. **Document complex chains** - Add comments for clarity

## Related Features

- `@pipe()` - Function composition
- `@tap()` - Side effects in chains
- `@when()` - Conditional chaining
- `@unless()` - Negative conditional chaining
- `@collect()` - Collection wrapper
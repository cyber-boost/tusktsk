# @tusk{} - TuskLang Active Record ORM

The `@tusk{}` object provides an elegant Active Record pattern for database operations, making it easy to work with database records as objects.

## Basic Syntax

```tusk
# Define a model
User: @tusk{
    table: "users"
    primary_key: "id"
    fillable: ["name", "email", "password"]
    hidden: ["password", "remember_token"]
    timestamps: true
}

# Create new record
user: @User.create({
    name: "John Doe"
    email: "john@example.com"
    password: @hash_password("secret")
})

# Find record
user: @User.find(123)

# Update record
user.name: "Jane Doe"
user.save()
```

## Model Definition

```tusk
# Complete model example
Product: @tusk{
    table: "products"
    primary_key: "id"
    
    # Mass assignment protection
    fillable: ["name", "description", "price", "category_id"]
    guarded: ["id", "created_at", "updated_at"]
    
    # Hide sensitive fields
    hidden: ["cost", "supplier_id"]
    
    # Automatic timestamps
    timestamps: true  # created_at, updated_at
    
    # Soft deletes
    soft_deletes: true  # deleted_at
    
    # Type casting
    casts: {
        price: "float"
        active: "boolean"
        metadata: "json"
        published_at: "datetime"
    }
    
    # Default values
    defaults: {
        active: true
        price: 0.00
        views: 0
    }
}
```

## CRUD Operations

```tusk
# Create
# Method 1: Using create()
product: @Product.create({
    name: "Laptop"
    description: "High-performance laptop"
    price: 999.99
    category_id: 5
})

# Method 2: Using new() and save()
product: @Product.new()
product.name: "Tablet"
product.price: 299.99
product.save()

# Read
# Find by primary key
product: @Product.find(1)

# Find with exception if not found
product: @Product.findOrFail(1)

# Find by attributes
user: @User.findBy("email", "john@example.com")

# First record
first_user: @User.first()

# Update
# Method 1: Direct update
product: @Product.find(1)
product.price: 899.99
product.save()

# Method 2: Mass update
@Product.update(1, {
    price: 899.99
    description: "Updated description"
})

# Delete
# Soft delete (if enabled)
product.delete()

# Force delete
product.forceDelete()

# Restore soft deleted
product.restore()
```

## Querying

```tusk
# All records
products: @Product.all()

# Where conditions
active_products: @Product.where("active", true).get()

# Multiple conditions
products: @Product
    .where("price", ">", 100)
    .where("category_id", 5)
    .get()

# Or where
products: @Product
    .where("category_id", 1)
    .orWhere("category_id", 2)
    .get()

# Where in
products: @Product.whereIn("id", [1, 2, 3, 4, 5]).get()

# Where between
products: @Product.whereBetween("price", [100, 500]).get()

# Like queries
products: @Product.where("name", "like", "%laptop%").get()

# Ordering
products: @Product.orderBy("price", "desc").get()

# Limiting
top_products: @Product.orderBy("sales", "desc").limit(10).get()

# Pagination
page: @request.get.page|1
products: @Product.paginate(20, @page)
```

## Relationships

```tusk
# Define relationships
User: @tusk{
    table: "users"
    
    # Has many posts
    posts: @hasMany("Post", "user_id")
    
    # Has one profile
    profile: @hasOne("Profile", "user_id")
    
    # Belongs to many roles
    roles: @belongsToMany("Role", "user_roles", "user_id", "role_id")
}

Post: @tusk{
    table: "posts"
    
    # Belongs to user
    user: @belongsTo("User", "user_id")
    
    # Has many comments
    comments: @hasMany("Comment", "post_id")
    
    # Has many tags through pivot
    tags: @belongsToMany("Tag", "post_tags", "post_id", "tag_id")
}

# Using relationships
user: @User.find(1)
user_posts: user.posts()

# Eager loading
users: @User.with(["posts", "profile"]).get()

# Lazy eager loading
user: @User.find(1)
user.load("posts")

# Query relationships
users_with_posts: @User.has("posts").get()
users_with_many_posts: @User.has("posts", ">", 5).get()
```

## Scopes

```tusk
# Define scopes
Product: @tusk{
    table: "products"
    
    # Scope methods
    scopes: {
        active: (query) => {
            query.where("active", true)
        }
        
        expensive: (query) => {
            query.where("price", ">", 1000)
        }
        
        inCategory: (query, category_id) => {
            query.where("category_id", @category_id)
        }
    }
}

# Using scopes
active_products: @Product.active().get()
expensive_active: @Product.active().expensive().get()
category_products: @Product.inCategory(5).active().get()
```

## Accessors and Mutators

```tusk
User: @tusk{
    table: "users"
    
    # Accessors (getters)
    accessors: {
        full_name: (user) => {
            return @user.first_name + " " + @user.last_name
        }
        
        avatar_url: (user) => {
            return @user.avatar ?: "/images/default-avatar.png"
        }
    }
    
    # Mutators (setters)
    mutators: {
        password: (value) => {
            return @hash_password(@value)
        }
        
        email: (value) => {
            return @strtolower(@value)
        }
    }
}

# Usage
user: @User.find(1)
name: user.full_name  # Accessor
user.password: "newpass"  # Mutator applies hash
```

## Events

```tusk
# Model events
User: @tusk{
    table: "users"
    
    # Event handlers
    events: {
        creating: (user) => {
            user.uuid: @generate_uuid()
            user.api_key: @generate_api_key()
        }
        
        created: (user) => {
            @send_welcome_email(@user)
            @log_activity("user_created", @user.id)
        }
        
        updating: (user) => {
            user.updated_by: @session.user_id
        }
        
        deleting: (user) => {
            # Clean up related data
            @delete_user_files(@user.id)
        }
    }
}
```

## Validation

```tusk
# Model validation
User: @tusk{
    table: "users"
    
    # Validation rules
    rules: {
        name: "required|string|max:255"
        email: "required|email|unique:users,email"
        password: "required|min:8"
        age: "integer|min:18|max:120"
    }
    
    # Custom validation messages
    messages: {
        email.unique: "This email is already registered"
        age.min: "You must be at least 18 years old"
    }
}

# Validate before saving
user: @User.new({
    name: @request.post.name
    email: @request.post.email
    password: @request.post.password
})

@if(user.validate()) {
    user.save()
    success: "User created successfully"
} else {
    errors: user.errors()
}
```

## Advanced Queries

```tusk
# Raw expressions
products: @Product
    .select(["*", @raw("(price * 0.9) as sale_price")])
    .where(@raw("YEAR(created_at) = ?", [@current_year]))
    .get()

# Joins
orders: @Order
    .join("users", "orders.user_id", "=", "users.id")
    .join("products", "orders.product_id", "=", "products.id")
    .select([
        "orders.*",
        "users.name as user_name",
        "products.name as product_name"
    ])
    .get()

# Aggregates
stats: {
    total: @Product.count()
    average_price: @Product.average("price")
    max_price: @Product.max("price")
    min_price: @Product.min("price")
    total_value: @Product.sum("price * stock")
}

# Group by
sales_by_category: @Product
    .select(["category_id", @raw("SUM(sales) as total_sales")])
    .groupBy("category_id")
    .having("total_sales", ">", 1000)
    .get()
```

## Caching

```tusk
# Cache query results
Product: @tusk{
    table: "products"
    
    # Cache configuration
    cache: {
        enabled: true
        ttl: 3600  # 1 hour
        prefix: "product"
    }
}

# Cached queries
products: @Product.remember(3600).where("active", true).get()

# Clear cache on update
Product: @tusk{
    events: {
        saved: (product) => {
            @cache.forget("products:all")
            @cache.forget("product:" + @product.id)
        }
    }
}
```

## Batch Operations

```tusk
# Insert multiple records
products_data: [
    {name: "Product 1", price: 10.00},
    {name: "Product 2", price: 20.00},
    {name: "Product 3", price: 30.00}
]

@Product.insert(@products_data)

# Update multiple records
@Product
    .whereIn("id", [1, 2, 3])
    .update({
        active: false,
        updated_at: @now()
    })

# Delete multiple records
@Product.destroy([4, 5, 6])

# Chunk processing for large datasets
@Product.chunk(100, (products) => {
    @foreach(@products as @product) {
        # Process each product
        @process_product(@product)
    }
})
```

## Best Practices

1. **Use fillable/guarded** - Protect against mass assignment
2. **Hide sensitive data** - Use hidden array for passwords, etc.
3. **Validate data** - Always validate before saving
4. **Use relationships** - More efficient than manual joins
5. **Cache frequently accessed data** - Improve performance
6. **Use scopes** - Make queries reusable and readable

## Related Features

- `@query()` - Raw database queries
- `@transaction()` - Database transactions
- `@cache` - Query caching
- `@validate()` - Data validation
- `@paginate()` - Result pagination
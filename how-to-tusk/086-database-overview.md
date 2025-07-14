# Database Overview in TuskLang

TuskLang provides a powerful and intuitive database abstraction layer with an elegant ORM (Object-Relational Mapping) system that makes working with databases a pleasure.

## Database Connection

```tusk
# Database configuration in config.tsk
config: {
    database: {
        default: "mysql"
        connections: {
            mysql: {
                driver: "mysql"
                host: @env("DB_HOST", "localhost")
                port: @env("DB_PORT", 3306)
                database: @env("DB_DATABASE", "myapp")
                username: @env("DB_USERNAME", "root")
                password: @env("DB_PASSWORD", "")
                charset: "utf8mb4"
                collation: "utf8mb4_unicode_ci"
                prefix: ""
                strict: true
                engine: "InnoDB"
            }
            
            postgres: {
                driver: "pgsql"
                host: @env("PG_HOST", "localhost")
                port: @env("PG_PORT", 5432)
                database: @env("PG_DATABASE", "myapp")
                username: @env("PG_USERNAME", "postgres")
                password: @env("PG_PASSWORD", "")
                charset: "utf8"
                prefix: ""
                schema: "public"
                sslmode: "prefer"
            }
            
            sqlite: {
                driver: "sqlite"
                database: @env("SQLITE_DATABASE", "database/database.sqlite")
                prefix: ""
                foreign_key_constraints: true
            }
        }
    }
}
```

## Basic Database Operations

```tusk
# Direct database queries
users: @db.query("SELECT * FROM users WHERE active = ?", [true])

# Insert data
@db.insert("INSERT INTO users (name, email) VALUES (?, ?)", [
    "John Doe",
    "john@example.com"
])

# Update data
affected: @db.update(
    "UPDATE users SET last_login = ? WHERE id = ?",
    [@now(), user_id]
)

# Delete data
@db.delete("DELETE FROM sessions WHERE expired_at < ?", [@now()])

# Transaction
@db.transaction((tx) => {
    tx.insert("INSERT INTO orders (user_id, total) VALUES (?, ?)", [user.id, total])
    order_id: tx.lastInsertId()
    
    for (item in cart_items) {
        tx.insert(
            "INSERT INTO order_items (order_id, product_id, quantity) VALUES (?, ?, ?)",
            [order_id, item.product_id, item.quantity]
        )
    }
    
    tx.update("UPDATE inventory SET stock = stock - ? WHERE product_id = ?", [
        item.quantity,
        item.product_id
    ])
})
```

## Query Builder

```tusk
# Fluent query builder
users: @db.table("users")
    .where("active", true)
    .where("created_at", ">", @days_ago(30))
    .orderBy("name")
    .limit(10)
    .get()

# Complex queries
results: @db.table("posts")
    .select("posts.*", "users.name as author_name")
    .join("users", "posts.user_id", "=", "users.id")
    .leftJoin("categories", "posts.category_id", "=", "categories.id")
    .where("posts.published", true)
    .whereIn("posts.status", ["active", "featured"])
    .whereBetween("posts.created_at", [start_date, end_date])
    .groupBy("posts.id")
    .having("COUNT(comments.id)", ">", 5)
    .orderBy("posts.created_at", "desc")
    .paginate(20)

# Aggregates
count: @db.table("users").count()
avg_age: @db.table("users").avg("age")
max_price: @db.table("products").max("price")
total: @db.table("orders").sum("total")
```

## Model System

```tusk
# Define a model
class User extends Model {
    table: "users"
    
    # Attributes
    fillable: ["name", "email", "password"]
    hidden: ["password", "remember_token"]
    casts: {
        email_verified_at: "datetime"
        is_active: "boolean"
    }
    
    # Relationships
    posts() {
        return @hasMany(Post)
    }
    
    profile() {
        return @hasOne(Profile)
    }
    
    roles() {
        return @belongsToMany(Role, "user_roles")
    }
    
    # Scopes
    scopeActive(query) {
        return query.where("is_active", true)
    }
    
    # Accessors
    getFullNameAttribute() {
        return this.first_name + " " + this.last_name
    }
    
    # Mutators
    setPasswordAttribute(value) {
        this.attributes["password"] = @bcrypt(value)
    }
}

# Using models
user: @User.find(1)
users: @User.where("active", true).get()
newUser: @User.create({
    name: "Jane Doe",
    email: "jane@example.com",
    password: "secret"
})
```

## Database Migrations

```tusk
# Create migration
#migration create_users_table {
    up() {
        @schema.create("users", (table) => {
            table.id()
            table.string("name")
            table.string("email").unique()
            table.timestamp("email_verified_at").nullable()
            table.string("password")
            table.rememberToken()
            table.timestamps()
            
            table.index(["email"])
        })
    }
    
    down() {
        @schema.dropIfExists("users")
    }
}

# Run migrations
@migrate.run()
@migrate.rollback()
@migrate.fresh()
```

## Database Features

### Connection Management
```tusk
# Use specific connection
users: @db.connection("postgres").table("users").get()

# Multiple database operations
@db.connection("analytics").table("events").insert(event_data)
@db.connection("main").table("users").update(user_data)
```

### Raw Expressions
```tusk
# Raw expressions in queries
users: @db.table("users")
    .select(@db.raw("COUNT(*) as user_count, DATE(created_at) as date"))
    .whereRaw("age > ? AND status = ?", [18, "active"])
    .groupBy(@db.raw("DATE(created_at)"))
    .get()
```

### Database Events
```tusk
# Listen to query events
@db.listen((query) => {
    @log.debug("Query executed", {
        sql: query.sql,
        bindings: query.bindings,
        time: query.time
    })
})
```

## Performance Optimization

```tusk
# Eager loading
users: @User.with(["posts", "posts.comments"]).get()

# Chunk processing
@User.chunk(100, (users) => {
    for (user in users) {
        @process_user(user)
    }
})

# Query caching
users: @cache.remember("all_users", 3600, () => {
    return @User.all()
})
```

## Database Security

```tusk
# Parameterized queries (automatic with query builder)
user: @db.table("users")
    .where("email", unsafe_email)  # Automatically escaped
    .first()

# Manual escaping
safe_string: @db.escape(unsafe_input)

# Prepared statements
stmt: @db.prepare("SELECT * FROM users WHERE id = ?")
user: stmt.execute([user_id])
```

## Testing Database

```tusk
# Database testing utilities
#test "User can be created" {
    # Use database transactions for tests
    @db.beginTransaction()
    
    user: @User.create({
        name: "Test User",
        email: "test@example.com"
    })
    
    @assert.equals(user.name, "Test User")
    @assert.true(@User.where("email", "test@example.com").exists())
    
    @db.rollback()  # Clean up
}

# Database factories
factory User {
    name: @faker.name()
    email: @faker.unique().email()
    password: @bcrypt("password")
    created_at: @faker.dateTimeBetween("-1 year", "now")
}

# Seeding
seeder UserSeeder {
    run() {
        @factory(User).count(50).create()
    }
}
```

## Best Practices

1. **Use the ORM** - Prefer models over raw queries
2. **Parameterize queries** - Never concatenate user input
3. **Use transactions** - For related operations
4. **Index properly** - Add indexes for frequently queried columns
5. **Eager load relationships** - Avoid N+1 queries
6. **Use migrations** - Version control your schema
7. **Handle connections** - Close connections properly
8. **Monitor queries** - Log slow queries in production

## Related Topics

- `query-builder` - Fluent query interface
- `orm-models` - Model definition and usage
- `migrations` - Database schema versioning
- `relationships` - Model relationships
- `database-transactions` - Transaction handling
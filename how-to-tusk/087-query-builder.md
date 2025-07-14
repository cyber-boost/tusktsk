# Query Builder in TuskLang

The TuskLang query builder provides a fluent, intuitive interface for creating and executing database queries. It uses method chaining to construct complex queries in a readable way.

## Basic Query Building

```tusk
# Select queries
users: @db.table("users").get()

# Select specific columns
users: @db.table("users")
    .select("id", "name", "email")
    .get()

# First record
user: @db.table("users")
    .where("email", "john@example.com")
    .first()

# Single column value
email: @db.table("users")
    .where("id", 1)
    .value("email")

# Pluck column values
emails: @db.table("users")
    .where("active", true)
    .pluck("email")

# Key-value pairs
user_names: @db.table("users")
    .pluck("name", "id")  # {1: "John", 2: "Jane", ...}
```

## Where Clauses

```tusk
# Basic where
users: @db.table("users")
    .where("status", "active")
    .get()

# Where with operator
users: @db.table("users")
    .where("age", ">", 18)
    .where("created_at", "<=", @now())
    .get()

# Multiple where conditions
users: @db.table("users")
    .where([
        ["status", "=", "active"],
        ["age", ">", 18],
        ["email_verified", "=", true]
    ])
    .get()

# Or where
users: @db.table("users")
    .where("role", "admin")
    .orWhere("role", "moderator")
    .get()

# Where in
users: @db.table("users")
    .whereIn("id", [1, 2, 3, 4, 5])
    .get()

# Where not in
users: @db.table("users")
    .whereNotIn("status", ["banned", "suspended"])
    .get()

# Where null
users: @db.table("users")
    .whereNull("deleted_at")
    .get()

# Where not null
users: @db.table("users")
    .whereNotNull("email_verified_at")
    .get()

# Where between
orders: @db.table("orders")
    .whereBetween("total", [100, 500])
    .get()

# Where date
orders: @db.table("orders")
    .whereDate("created_at", "2024-01-15")
    .get()

# Where month/year/day
birthdays: @db.table("users")
    .whereMonth("birthday", 12)
    .whereDay("birthday", 25)
    .get()
```

## Advanced Where Clauses

```tusk
# Where exists
users: @db.table("users")
    .whereExists((query) => {
        query.select("*")
            .from("orders")
            .whereColumn("orders.user_id", "users.id")
    })
    .get()

# Where column comparison
users: @db.table("users")
    .whereColumn("first_name", "last_name")
    .get()

# Complex where groups
users: @db.table("users")
    .where("active", true)
    .where((query) => {
        query.where("age", ">", 18)
            .orWhere("parental_consent", true)
    })
    .get()

# Where JSON contains
users: @db.table("users")
    .whereJsonContains("preferences->notifications", "email")
    .get()

# Where JSON length
users: @db.table("users")
    .whereJsonLength("tags", ">", 5)
    .get()

# Full text search
articles: @db.table("articles")
    .whereFullText(["title", "content"], "TuskLang database")
    .get()
```

## Joins

```tusk
# Inner join
users: @db.table("users")
    .join("contacts", "users.id", "=", "contacts.user_id")
    .select("users.*", "contacts.phone")
    .get()

# Left join
posts: @db.table("posts")
    .leftJoin("users", "posts.user_id", "=", "users.id")
    .select("posts.*", "users.name as author")
    .get()

# Right join
categories: @db.table("categories")
    .rightJoin("posts", "categories.id", "=", "posts.category_id")
    .get()

# Cross join
colors: @db.table("colors")
    .crossJoin("sizes")
    .get()

# Advanced join clauses
orders: @db.table("orders")
    .join("users", (join) => {
        join.on("orders.user_id", "=", "users.id")
            .where("users.active", true)
            .orOn("orders.guest_email", "=", "users.email")
    })
    .get()

# Multiple joins
data: @db.table("posts")
    .join("users", "posts.user_id", "=", "users.id")
    .join("categories", "posts.category_id", "=", "categories.id")
    .leftJoin("comments", "posts.id", "=", "comments.post_id")
    .select(
        "posts.title",
        "users.name as author",
        "categories.name as category",
        @db.raw("COUNT(comments.id) as comment_count")
    )
    .groupBy("posts.id")
    .get()
```

## Ordering and Grouping

```tusk
# Order by
users: @db.table("users")
    .orderBy("name")
    .get()

# Order by descending
users: @db.table("users")
    .orderBy("created_at", "desc")
    .get()

# Multiple order by
users: @db.table("users")
    .orderBy("last_name")
    .orderBy("first_name")
    .get()

# Latest/oldest
latest_posts: @db.table("posts").latest().limit(5).get()
oldest_posts: @db.table("posts").oldest("published_at").limit(5).get()

# Random order
random_user: @db.table("users").inRandomOrder().first()

# Group by
sales: @db.table("orders")
    .select("user_id", @db.raw("SUM(total) as total_sales"))
    .groupBy("user_id")
    .get()

# Having clause
high_value_customers: @db.table("orders")
    .select("user_id", @db.raw("SUM(total) as total_spent"))
    .groupBy("user_id")
    .having("total_spent", ">", 1000)
    .get()
```

## Pagination

```tusk
# Simple pagination
users: @db.table("users").paginate(15)

# Pagination with custom page
page: @request.query.page || 1
users: @db.table("users").paginate(15, page)

# Simple paginate (more efficient)
users: @db.table("users").simplePaginate(15)

# Cursor pagination
users: @db.table("users")
    .orderBy("id")
    .cursorPaginate(15)

# Offset and limit
users: @db.table("users")
    .offset(20)
    .limit(10)
    .get()

# Skip and take (aliases)
users: @db.table("users")
    .skip(20)
    .take(10)
    .get()
```

## Aggregates

```tusk
# Count
total_users: @db.table("users").count()
active_users: @db.table("users").where("active", true).count()

# Count distinct
unique_emails: @db.table("users").count("email", true)

# Sum
total_revenue: @db.table("orders").sum("total")

# Average
average_age: @db.table("users").avg("age")
average_order: @db.table("orders").average("total")  # Alias

# Min/Max
min_price: @db.table("products").min("price")
max_price: @db.table("products").max("price")

# Aggregate with conditions
avg_active_user_age: @db.table("users")
    .where("active", true)
    .avg("age")

# Multiple aggregates
stats: @db.table("products")
    .select(
        @db.raw("COUNT(*) as total"),
        @db.raw("AVG(price) as avg_price"),
        @db.raw("MIN(price) as min_price"),
        @db.raw("MAX(price) as max_price")
    )
    .first()
```

## Inserts

```tusk
# Single insert
@db.table("users").insert({
    name: "John Doe",
    email: "john@example.com",
    created_at: @now()
})

# Insert with ID return
user_id: @db.table("users").insertGetId({
    name: "Jane Doe",
    email: "jane@example.com"
})

# Multiple inserts
@db.table("users").insert([
    {name: "User 1", email: "user1@example.com"},
    {name: "User 2", email: "user2@example.com"},
    {name: "User 3", email: "user3@example.com"}
])

# Insert or ignore
@db.table("users").insertOrIgnore({
    email: "existing@example.com",
    name: "New User"
})

# Upsert
@db.table("users").upsert(
    [{email: "john@example.com", name: "John Updated"}],
    ["email"],  # Unique keys
    ["name"]    # Columns to update
)
```

## Updates

```tusk
# Update records
@db.table("users")
    .where("id", 1)
    .update({
        name: "Updated Name",
        updated_at: @now()
    })

# Increment/Decrement
@db.table("users")
    .where("id", 1)
    .increment("points", 10)

@db.table("products")
    .where("id", 5)
    .decrement("stock", 1)

# Update with additional columns
@db.table("users")
    .where("id", 1)
    .increment("points", 10, {
        updated_at: @now()
    })

# Update or insert
@db.table("users").updateOrInsert(
    {email: "john@example.com"},  # Search conditions
    {name: "John Doe", active: true}  # Values
)

# JSON updates
@db.table("users")
    .where("id", 1)
    .update({
        "preferences->theme": "dark",
        "preferences->notifications->email": true
    })
```

## Deletes

```tusk
# Delete records
@db.table("users")
    .where("active", false)
    .delete()

# Soft deletes (if model uses SoftDeletes)
@db.table("posts")
    .where("id", 1)
    .update({deleted_at: @now()})

# Truncate table
@db.table("temp_data").truncate()
```

## Raw Queries

```tusk
# Raw expressions in select
users: @db.table("users")
    .select(
        "name",
        @db.raw("CONCAT(first_name, ' ', last_name) as full_name"),
        @db.raw("YEAR(CURDATE()) - YEAR(birth_date) as age")
    )
    .get()

# Raw where clauses
users: @db.table("users")
    .whereRaw("age > ? AND status = ?", [18, "active"])
    .get()

# Raw order by
users: @db.table("users")
    .orderByRaw("FIELD(status, 'premium', 'active', 'inactive')")
    .get()

# Completely raw queries
results: @db.select("SELECT * FROM users WHERE id = :id", {id: 1})
@db.statement("DROP TABLE old_table")
```

## Query Debugging

```tusk
# Get SQL query
query: @db.table("users")
    .where("active", true)
    .orderBy("name")

sql: query.toSql()
bindings: query.getBindings()

# Dump and die
@db.table("users")
    .where("active", true)
    .dd()  # Dumps query and stops execution

# Dump and continue
@db.table("users")
    .where("active", true)
    .dump()  # Dumps query but continues
    .get()

# Explain query
explanation: @db.table("users")
    .where("active", true)
    .explain()
```

## Advanced Features

```tusk
# Subqueries
users: @db.table("users")
    .select("*")
    .whereIn("id", (query) => {
        query.select("user_id")
            .from("orders")
            .where("total", ">", 100)
    })
    .get()

# Conditional clauses
users: @db.table("users")
    .when(search_term, (query, value) => {
        query.where("name", "like", "%" + value + "%")
    })
    .when(only_active, (query) => {
        query.where("active", true)
    })
    .get()

# Unions
first: @db.table("users").where("active", true)
second: @db.table("users").where("premium", true)

users: first.union(second).get()

# Clone query
base_query: @db.table("users").where("active", true)

admins: base_query.clone().where("role", "admin").get()
moderators: base_query.clone().where("role", "moderator").get()
```

## Best Practices

1. **Use parameter binding** - Never concatenate values into queries
2. **Select only needed columns** - Don't use SELECT * in production
3. **Use indexes** - Ensure WHERE columns are indexed
4. **Limit results** - Always paginate large datasets
5. **Avoid N+1 queries** - Use joins or eager loading
6. **Use transactions** - For multiple related operations
7. **Monitor slow queries** - Log queries over threshold
8. **Cache when possible** - Cache expensive queries

## Related Topics

- `database-overview` - Database configuration
- `orm-models` - Using Eloquent models
- `database-transactions` - Transaction handling
- `raw-queries` - Raw SQL execution
- `query-optimization` - Performance tips
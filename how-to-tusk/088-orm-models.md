# ORM Models in TuskLang

TuskLang's ORM (Object-Relational Mapping) provides an elegant ActiveRecord implementation for working with your database. Each database table has a corresponding "Model" which is used to interact with that table.

## Defining Models

```tusk
# Basic model definition
class User extends Model {
    # Table name (optional, defaults to plural of class name)
    table: "users"
    
    # Primary key (optional, defaults to 'id')
    primaryKey: "id"
    
    # Indicates if the model should be timestamped
    timestamps: true
    
    # The attributes that are mass assignable
    fillable: ["name", "email", "password"]
    
    # The attributes that should be hidden for arrays/JSON
    hidden: ["password", "remember_token"]
    
    # The attributes that should be cast
    casts: {
        email_verified_at: "datetime",
        is_admin: "boolean",
        preferences: "array",
        metadata: "json"
    }
    
    # Default attribute values
    attributes: {
        is_active: true,
        role: "user"
    }
}
```

## Basic CRUD Operations

```tusk
# Create
# Method 1: Using create
user: @User.create({
    name: "John Doe",
    email: "john@example.com",
    password: "secret"
})

# Method 2: Using save
user: new User()
user.name: "Jane Doe"
user.email: "jane@example.com"
user.password: "secret"
user.save()

# Method 3: Using fill
user: new User()
user.fill({
    name: "Bob Smith",
    email: "bob@example.com"
}).save()

# Read
# Find by primary key
user: @User.find(1)
users: @User.find([1, 2, 3])  # Multiple

# Find with exception if not found
user: @User.findOrFail(1)

# First record
user: @User.first()
user: @User.where("email", "john@example.com").first()

# All records
users: @User.all()

# Update
# Method 1: Update on instance
user: @User.find(1)
user.name: "Updated Name"
user.save()

# Method 2: Mass update
user.update({
    name: "New Name",
    email: "newemail@example.com"
})

# Method 3: Update multiple
@User.where("active", false)
    .update({last_notified_at: @now()})

# Delete
# Delete single
user: @User.find(1)
user.delete()

# Delete multiple
@User.where("created_at", "<", @days_ago(365)).delete()

# Soft delete (if model uses SoftDeletes)
user.delete()  # Sets deleted_at timestamp
user.forceDelete()  # Permanent delete
```

## Querying Models

```tusk
# Where conditions
users: @User.where("status", "active").get()
users: @User.where("age", ">", 18).get()

# Multiple conditions
users: @User.where("status", "active")
    .where("age", ">", 18)
    .get()

# Or conditions
users: @User.where("role", "admin")
    .orWhere("role", "moderator")
    .get()

# Advanced queries
users: @User.whereIn("id", [1, 2, 3])
    .whereNotNull("email_verified_at")
    .orderBy("created_at", "desc")
    .limit(10)
    .get()

# Conditional queries
users: @User
    .when(request.active_only, (query) => {
        query.where("active", true)
    })
    .when(request.search, (query, search) => {
        query.where("name", "like", "%" + search + "%")
    })
    .get()
```

## Eloquent Collections

```tusk
# Collections provide powerful array-like methods
users: @User.all()

# Filter
active_users: users.filter(user => user.active)

# Map
user_names: users.map(user => user.name)

# Reduce
total_age: users.reduce((sum, user) => sum + user.age, 0)

# Pluck
emails: users.pluck("email")

# Group by
users_by_role: users.groupBy("role")

# Sort
sorted_users: users.sortBy("created_at")

# Chunk
users.chunk(100).each(chunk => {
    @process_users(chunk)
})
```

## Model Relationships

```tusk
class User extends Model {
    # One to One
    profile() {
        return @hasOne(Profile)
    }
    
    # One to Many
    posts() {
        return @hasMany(Post)
    }
    
    # Many to Many
    roles() {
        return @belongsToMany(Role, "user_roles")
            .withPivot("assigned_at")
            .withTimestamps()
    }
    
    # Has Many Through
    comments() {
        return @hasManyThrough(Comment, Post)
    }
    
    # Polymorphic Relations
    notifications() {
        return @morphMany(Notification, "notifiable")
    }
}

class Post extends Model {
    # Inverse relationships
    author() {
        return @belongsTo(User, "user_id")
    }
    
    comments() {
        return @hasMany(Comment)
    }
    
    tags() {
        return @belongsToMany(Tag)
    }
}

# Using relationships
user: @User.find(1)
posts: user.posts  # Lazy load
user: @User.with("posts").find(1)  # Eager load

# Query relationships
users_with_posts: @User.has("posts").get()
users_with_many_posts: @User.has("posts", ">", 5).get()
users_without_posts: @User.doesntHave("posts").get()

# Query relationship existence with conditions
users: @User.whereHas("posts", (query) => {
    query.where("published", true)
}).get()
```

## Eager Loading

```tusk
# Basic eager loading
users: @User.with("posts").get()

# Multiple relationships
users: @User.with(["posts", "profile"]).get()

# Nested eager loading
users: @User.with("posts.comments").get()

# Conditional eager loading
users: @User.with(["posts" => (query) => {
    query.where("published", true)
}]).get()

# Lazy eager loading
users: @User.all()
users.load("posts")  # Load after retrieval

# Count eager loading
users: @User.withCount("posts").get()
// Access via user.posts_count
```

## Model Scopes

```tusk
class User extends Model {
    # Local scopes
    scopeActive(query) {
        return query.where("active", true)
    }
    
    scopeOfRole(query, role) {
        return query.where("role", role)
    }
    
    scopeRegisteredBetween(query, start, end) {
        return query.whereBetween("created_at", [start, end])
    }
    
    # Global scopes
    boot() {
        super.boot()
        
        # Apply to all queries
        @addGlobalScope("active", (query) => {
            query.where("active", true)
        })
    }
}

# Using scopes
active_users: @User.active().get()
admins: @User.ofRole("admin").get()
recent: @User.registeredBetween(start_date, end_date).get()

# Combining scopes
active_admins: @User.active().ofRole("admin").get()

# Remove global scopes
all_users: @User.withoutGlobalScope("active").get()
```

## Accessors and Mutators

```tusk
class User extends Model {
    # Accessor - modify attribute when retrieving
    getFullNameAttribute() {
        return this.first_name + " " + this.last_name
    }
    
    getAgeAttribute() {
        return @date_diff(this.birth_date, @now(), "years")
    }
    
    # Mutator - modify attribute when setting
    setPasswordAttribute(value) {
        this.attributes.password: @bcrypt(value)
    }
    
    setEmailAttribute(value) {
        this.attributes.email: value.toLowerCase()
    }
    
    # Date mutators
    dates: ["created_at", "updated_at", "published_at"]
    
    # Custom date format
    dateFormat: "Y-m-d H:i:s"
}

# Using accessors
user: @User.find(1)
full_name: user.full_name  # Calls getFullNameAttribute()
age: user.age

# Using mutators
user.password: "newsecret"  # Automatically hashed
user.email: "JOHN@EXAMPLE.COM"  # Stored as lowercase
```

## Model Events

```tusk
class User extends Model {
    # Boot method for registering events
    boot() {
        super.boot()
        
        # Before creating
        @creating((user) => {
            user.uuid: @generate_uuid()
            user.api_token: @generate_token()
        })
        
        # After creating
        @created((user) => {
            @send_welcome_email(user)
            @create_default_settings(user)
        })
        
        # Before saving
        @saving((user) => {
            user.slug: @slugify(user.name)
        })
        
        # After deleting
        @deleted((user) => {
            @cleanup_user_data(user)
        })
    }
}

# Available events:
# - creating, created
# - updating, updated  
# - saving, saved
# - deleting, deleted
# - restoring, restored (soft deletes)

# Using observers
class UserObserver {
    creating(user) {
        @log.info("Creating user: " + user.email)
    }
    
    updated(user) {
        if (user.isDirty("email")) {
            @send_email_change_notification(user)
        }
    }
    
    deleting(user) {
        if (user.posts().count() > 0) {
            throw "Cannot delete user with posts"
        }
    }
}

# Register observer
@User.observe(UserObserver)
```

## Advanced Model Features

```tusk
# Soft deletes
class Post extends Model {
    use SoftDeletes
    
    dates: ["deleted_at"]
}

# Query soft deleted models
all_posts: @Post.withTrashed().get()
only_trashed: @Post.onlyTrashed().get()

# Restore soft deleted
post: @Post.withTrashed().find(1)
post.restore()

# Model replication
user: @User.find(1)
new_user: user.replicate()
new_user.email: "new@example.com"
new_user.save()

# Model comparison
if (user.is(another_user)) {
    // Same model instance
}

if (user.isNot(another_user)) {
    // Different model instances
}

# Dirty checking
user: @User.find(1)
user.name: "New Name"

if (user.isDirty()) {
    // Model has unsaved changes
    dirty_fields: user.getDirty()
}

if (user.isDirty("name")) {
    original: user.getOriginal("name")
}

# Mass assignment protection
user: @User.find(1)
user.forceFill({
    name: "Admin Override",
    is_admin: true  // Even if not in fillable
})
```

## Model Serialization

```tusk
# Convert to array
user: @User.find(1)
array: user.toArray()

# Convert to JSON
json: user.toJson()

# Customize serialization
class User extends Model {
    # Hide attributes
    hidden: ["password", "remember_token"]
    
    # Make visible conditionally
    makeVisible(attributes) {
        if (@auth.user?.is_admin) {
            return super.makeVisible(["email", "phone"])
        }
        return this
    }
    
    # Append custom attributes
    appends: ["full_name", "age"]
    
    # Custom serialization
    toArray() {
        data: super.toArray()
        
        // Add computed fields
        data.posts_count: this.posts().count()
        data.is_premium: this.isPremium()
        
        return data
    }
}
```

## Best Practices

1. **Use mass assignment protection** - Define fillable attributes
2. **Leverage relationships** - Define and use proper relationships
3. **Use scopes** - Encapsulate common queries
4. **Implement accessors/mutators** - Handle data transformation
5. **Use eager loading** - Prevent N+1 queries
6. **Handle model events** - Centralize business logic
7. **Use transactions** - Ensure data integrity
8. **Cache expensive operations** - Improve performance

## Related Topics

- `database-overview` - Database setup and configuration
- `query-builder` - Query builder methods
- `relationships` - Detailed relationship types
- `model-events` - Model lifecycle events
- `model-factories` - Testing with factories
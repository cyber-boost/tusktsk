# Model Relationships in TuskLang

TuskLang's ORM provides a rich set of relationship types to express how your database tables relate to each other. Relationships are defined as methods on your model classes.

## One To One Relationships

```tusk
# User has one Profile
class User extends Model {
    profile() {
        return @hasOne(Profile)
    }
    
    # With custom foreign key
    address() {
        return @hasOne(Address, "user_id")
    }
    
    # With custom foreign and local keys
    passport() {
        return @hasOne(Passport, "citizen_id", "national_id")
    }
}

# Inverse relationship
class Profile extends Model {
    user() {
        return @belongsTo(User)
    }
    
    # With custom keys
    owner() {
        return @belongsTo(User, "user_id", "id")
    }
}

# Usage
user: @User.find(1)
profile: user.profile  # Lazy load

# Eager loading
user: @User.with("profile").find(1)

# Create related model
user.profile().create({
    bio: "Software developer",
    website: "example.com"
})

# Update related model
user.profile().update({
    bio: "Senior software developer"
})
```

## One To Many Relationships

```tusk
# User has many Posts
class User extends Model {
    posts() {
        return @hasMany(Post)
    }
    
    # With constraints
    publishedPosts() {
        return @hasMany(Post).where("published", true)
    }
    
    # With ordering
    recentPosts() {
        return @hasMany(Post)
            .orderBy("created_at", "desc")
            .limit(5)
    }
}

# Inverse relationship
class Post extends Model {
    author() {
        return @belongsTo(User, "user_id")
    }
}

# Usage
user: @User.find(1)
posts: user.posts  # Collection of posts

# Add conditions
recent_posts: user.posts.where("created_at", ">", @days_ago(7)).get()

# Count related
post_count: user.posts.count()

# Create related
new_post: user.posts().create({
    title: "New Post",
    content: "Post content..."
})

# Create multiple
user.posts().createMany([
    {title: "Post 1", content: "Content 1"},
    {title: "Post 2", content: "Content 2"}
])

# Attach existing
existing_post: @Post.find(5)
user.posts().save(existing_post)
```

## Many To Many Relationships

```tusk
# User belongs to many Roles
class User extends Model {
    roles() {
        return @belongsToMany(Role)
        # Uses 'user_roles' pivot table
    }
    
    # With custom pivot table
    groups() {
        return @belongsToMany(Group, "group_members")
    }
    
    # With custom keys
    projects() {
        return @belongsToMany(
            Project,
            "project_members",  # Pivot table
            "member_id",        # Foreign key on pivot
            "project_id",       # Related key on pivot
            "id",               # Local key
            "id"                # Related key
        )
    }
    
    # With pivot data
    teams() {
        return @belongsToMany(Team)
            .withPivot(["role", "joined_at"])
            .withTimestamps()
            .wherePivot("active", true)
    }
}

# Inverse relationship
class Role extends Model {
    users() {
        return @belongsToMany(User)
    }
}

# Usage
user: @User.find(1)
roles: user.roles

# Attach relationships
user.roles().attach(role_id)
user.roles().attach(role_id, {expires_at: @days_from_now(30)})
user.roles().attach({
    1: {expires_at: @days_from_now(30)},
    2: {expires_at: @days_from_now(60)}
})

# Detach relationships
user.roles().detach(role_id)
user.roles().detach([1, 2, 3])
user.roles().detach()  # Detach all

# Sync relationships
user.roles().sync([1, 2, 3])  # Only these roles
user.roles().sync({
    1: {expires_at: @days_from_now(30)},
    2: {expires_at: @days_from_now(60)}
})
user.roles().syncWithoutDetaching([1, 2])  # Add without removing

# Toggle relationships
user.roles().toggle([1, 2, 3])  # Attach if not attached, detach if attached

# Access pivot data
for (role in user.roles) {
    role_name: role.name
    assigned_at: role.pivot.created_at
    expires_at: role.pivot.expires_at
    role_in_team: role.pivot.role
}

# Query pivot table
active_roles: user.roles()
    .wherePivot("active", true)
    .wherePivot("expires_at", ">", @now())
    .get()
```

## Has One/Many Through

```tusk
# Country has many Posts through Users
class Country extends Model {
    posts() {
        return @hasManyThrough(Post, User)
    }
    
    # With custom keys
    comments() {
        return @hasManyThrough(
            Comment,           # Final model
            User,             # Intermediate model
            "country_id",     # Foreign key on intermediate
            "user_id",        # Foreign key on final
            "id",             # Local key
            "id"              # Local key on intermediate
        )
    }
}

# Mechanic has one Car Owner through Car
class Mechanic extends Model {
    carOwner() {
        return @hasOneThrough(Owner, Car)
    }
}

# Usage
country: @Country.find(1)
posts: country.posts  # All posts by users in this country

# With conditions
recent_posts: country.posts()
    .where("published", true)
    .orderBy("created_at", "desc")
    .get()
```

## Polymorphic Relationships

```tusk
# One-to-Many Polymorphic
# Multiple models can have comments
class Comment extends Model {
    # Get the parent commentable model
    commentable() {
        return @morphTo()
    }
}

class Post extends Model {
    comments() {
        return @morphMany(Comment, "commentable")
    }
}

class Video extends Model {
    comments() {
        return @morphMany(Comment, "commentable")
    }
}

# Usage
post: @Post.find(1)
comments: post.comments

video: @Video.find(1)
comments: video.comments

# Create polymorphic relation
post.comments().create({
    body: "Great post!"
})

# Get parent from polymorphic model
comment: @Comment.find(1)
parent: comment.commentable  # Post or Video instance

# Many-to-Many Polymorphic
# Multiple models can have tags
class Tag extends Model {
    posts() {
        return @morphedByMany(Post, "taggable")
    }
    
    videos() {
        return @morphedByMany(Video, "taggable")
    }
}

class Post extends Model {
    tags() {
        return @morphToMany(Tag, "taggable")
    }
}

class Video extends Model {
    tags() {
        return @morphToMany(Tag, "taggable")
    }
}

# One-to-One Polymorphic
class Image extends Model {
    imageable() {
        return @morphTo()
    }
}

class User extends Model {
    image() {
        return @morphOne(Image, "imageable")
    }
}

class Post extends Model {
    image() {
        return @morphOne(Image, "imageable")
    }
}
```

## Eager Loading

```tusk
# Basic eager loading
users: @User.with("posts").get()

# Multiple relationships
users: @User.with(["posts", "profile"]).get()

# Nested eager loading
users: @User.with("posts.comments").get()

# Eager load with constraints
users: @User.with(["posts" => (query) => {
    query.where("published", true)
        .orderBy("created_at", "desc")
}]).get()

# Eager load counts
users: @User.withCount("posts").get()
// Access via user.posts_count

# Multiple counts with constraints
users: @User.withCount([
    "posts",
    "posts as published_posts_count" => (query) => {
        query.where("published", true)
    }
]).get()

# Lazy eager loading
users: @User.all()
users.load("posts")  # Load after retrieval

# Conditional eager loading
users: @User.all()
if (include_posts) {
    users.load("posts")
}
```

## Querying Relationships

```tusk
# Query existence
users_with_posts: @User.has("posts").get()
users_with_many_posts: @User.has("posts", ">", 5).get()
users_without_posts: @User.doesntHave("posts").get()

# Query with conditions
users: @User.whereHas("posts", (query) => {
    query.where("published", true)
}).get()

# Count constraint
users: @User.whereHas("posts", (query) => {
    query.where("published", true)
}, ">", 10).get()

# Or conditions
users: @User.whereHas("posts")
    .orWhereHas("comments")
    .get()

# Nested relationship queries
users: @User.whereHas("posts.comments", (query) => {
    query.where("approved", true)
}).get()

# Query missing relationships
users: @User.whereDoesntHave("posts", (query) => {
    query.where("published", true)
}).get()

# Polymorphic queries
comments: @Comment.whereHasMorph(
    "commentable",
    [Post, Video],
    (query) => {
        query.where("active", true)
    }
).get()
```

## Advanced Relationship Features

```tusk
# Default models
class User extends Model {
    profile() {
        return @hasOne(Profile).withDefault()
    }
    
    settings() {
        return @hasOne(Settings).withDefault({
            theme: "light",
            notifications: true
        })
    }
}

# Touching parent timestamps
class Comment extends Model {
    touches: ["post"]  # Update post's updated_at when comment changes
    
    post() {
        return @belongsTo(Post)
    }
}

# Counting related models
class Post extends Model {
    boot() {
        super.boot()
        
        # Automatically maintain comment count
        @hasMany(Comment).countingTo("comment_count")
    }
}

# Custom relationship classes
class HasManyPremium extends HasMany {
    construct() {
        super.construct()
        @where("is_premium", true)
    }
}

class User extends Model {
    premiumPosts() {
        return new HasManyPremium(Post, "user_id")
    }
}

# Relationship events
user.posts().creating((post) => {
    post.slug: @slugify(post.title)
})

user.roles().attaching((role_id, pivot_data) => {
    @log("Attaching role", {user: user.id, role: role_id})
})

user.roles().detached((role_ids) => {
    @cache.forget("user." + user.id + ".permissions")
})
```

## Performance Optimization

```tusk
# Prevent N+1 queries
# Bad
users: @User.all()
for (user in users) {
    posts: user.posts  # N+1 query problem
}

# Good
users: @User.with("posts").get()
for (user in users) {
    posts: user.posts  # Already loaded
}

# Optimize counts
# Bad
users: @User.all()
for (user in users) {
    count: user.posts.count()  # N queries
}

# Good
users: @User.withCount("posts").get()
for (user in users) {
    count: user.posts_count  # Already loaded
}

# Chunk large datasets
@User.with("posts").chunk(100, (users) => {
    for (user in users) {
        @process_user(user)
    }
})

# Use existence queries instead of count
# Bad
if (user.posts.count() > 0) {
    // Has posts
}

# Good
if (user.posts.exists()) {
    // Has posts
}
```

## Best Practices

1. **Always define inverse relationships** - Makes queries more flexible
2. **Use eager loading** - Prevent N+1 query problems
3. **Name relationships clearly** - Use descriptive, intuitive names
4. **Add constraints to relationships** - Filter data at the relationship level
5. **Use withDefault() for optional relationships** - Avoid null checks
6. **Index foreign keys** - Improve join performance
7. **Use existence queries** - More efficient than counting
8. **Monitor queries** - Watch for N+1 problems in logs

## Related Topics

- `orm-models` - Model basics
- `eager-loading` - Loading strategies
- `query-optimization` - Performance tips
- `polymorphic-relations` - Advanced polymorphism
- `pivot-tables` - Many-to-many details
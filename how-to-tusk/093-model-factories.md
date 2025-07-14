# Model Factories in TuskLang

Model factories provide a convenient way to generate fake data for your models. They're essential for testing, seeding databases, and creating sample data during development.

## Defining Factories

```tusk
# Basic factory definition
#factory User {
    name: @faker.name()
    email: @faker.unique().safeEmail()
    password: @bcrypt("password")
    remember_token: @str_random(60)
    created_at: @faker.dateTimeBetween("-1 year", "now")
    updated_at: @now()
}

# Factory with computed attributes
#factory Product {
    name: @faker.productName()
    slug: (attributes) => @str_slug(attributes.name)
    description: @faker.paragraph()
    price: @faker.randomFloat(2, 10, 1000)
    cost: (attributes) => attributes.price * 0.6
    sku: @faker.unique().bothify("???-#####")
    stock: @faker.numberBetween(0, 100)
    
    # Conditional attributes
    sale_price: (attributes) => {
        if (@faker.boolean(0.3)) {  # 30% on sale
            return attributes.price * @faker.randomFloat(2, 0.7, 0.9)
        }
        return null
    }
}

# Factory with relationships
#factory Post {
    title: @faker.sentence()
    content: @faker.paragraphs(5, true)
    published: @faker.boolean(0.8)
    
    # Belongs to relationship
    user_id: @factory(User)
    
    # Or use existing model
    category_id: () => @Category.inRandomOrder().first()?.id || @factory(Category)
}
```

## Factory States

```tusk
#factory User {
    # Default attributes
    name: @faker.name()
    email: @faker.unique().safeEmail()
    password: @bcrypt("password")
    active: true
    role: "user"
    
    # Define states
    state("admin") {
        role: "admin"
        permissions: ["*"]
        email: (attributes) => "admin-" + attributes.email
    }
    
    state("inactive") {
        active: false
        deactivated_at: @faker.dateTimeBetween("-30 days", "now")
    }
    
    state("premium") {
        subscription_type: "premium"
        subscription_expires: @faker.dateTimeBetween("now", "+1 year")
        credit_limit: @faker.numberBetween(5000, 50000)
    }
    
    state("verified") {
        email_verified_at: @faker.dateTimeBetween("-6 months", "now")
        phone_verified: true
    }
    
    # Multiple states can be combined
    state("banned") {
        banned: true
        banned_at: @now()
        ban_reason: @faker.randomElement([
            "Terms violation",
            "Spam",
            "Abusive behavior"
        ])
    }
}

# Using states
admin: @factory(User).state("admin").create()
inactive_users: @factory(User).state("inactive").count(5).create()
premium_admin: @factory(User).states(["admin", "premium"]).create()
```

## Factory Sequences

```tusk
#factory User {
    # Auto-incrementing sequences
    username: @faker.sequence((n) => "user" + n)
    employee_id: @faker.sequence((n) => "EMP" + @str_pad(n, 5, "0", STR_PAD_LEFT))
    
    # Custom sequence starting point
    customer_number: @faker.sequence((n) => n + 1000)
    
    # Reset sequences
    email: @faker.sequence((n) => {
        if (n > 1000) {
            @faker.resetSequence("email")
            n: 1
        }
        return "test" + n + "@example.com"
    })
}

# Named sequences shared across factories
#factory Order {
    order_number: @faker.sequence("order_number", (n) => {
        year: @date("Y")
        return year + "-" + @str_pad(n, 6, "0", STR_PAD_LEFT)
    })
}

#factory Invoice {
    invoice_number: @faker.sequence("invoice_number", (n) => {
        return "INV-" + @date("Ym") + "-" + @str_pad(n, 4, "0", STR_PAD_LEFT)
    })
}
```

## Factory Callbacks

```tusk
#factory User {
    name: @faker.name()
    email: @faker.unique().safeEmail()
    
    # Configure callback
    configure() {
        # Runs when factory is defined
        @faker.seed(12345)  # Consistent random data
    }
    
    # After making (before create)
    afterMaking(user, faker) {
        # Set computed values
        user.display_name: user.display_name || user.name
        user.slug: @str_slug(user.name)
    }
    
    # After creating (after save)
    afterCreating(user, faker) {
        # Create related models
        user.profile().create({
            bio: faker.paragraph(),
            avatar: faker.imageUrl(200, 200, "people", true, user.name)
        })
        
        # Assign roles
        if (user.role == "admin") {
            admin_role: @Role.where("name", "admin").first()
            user.roles().attach(admin_role)
        }
        
        # Send notifications
        if (@env.factory_notifications) {
            @event("user.created", user)
        }
    }
}

# State-specific callbacks
#factory Post {
    title: @faker.sentence()
    content: @faker.paragraphs(3, true)
    
    state("published") {
        published: true
        published_at: @faker.dateTimeBetween("-6 months", "now")
        
        afterCreating(post, faker) {
            # Generate SEO data for published posts
            post.seo().create({
                meta_title: post.title,
                meta_description: @str_limit(post.content, 160),
                keywords: faker.words(5).join(", ")
            })
            
            # Generate social media previews
            @queue("generate_social_previews", post)
        }
    }
}
```

## Complex Factory Relationships

```tusk
#factory User {
    name: @faker.name()
    email: @faker.unique().safeEmail()
    
    # Has many relationship
    afterCreating(user, faker) {
        # Create related posts
        @factory(Post)
            .count(faker.numberBetween(0, 10))
            .create({user_id: user.id})
    }
}

# Using has() method
users_with_posts: @factory(User)
    .count(5)
    .has(@factory(Post).count(3))  # Each user has 3 posts
    .create()

# Nested relationships
users: @factory(User)
    .count(3)
    .has(
        @factory(Post)
            .count(5)
            .has(@factory(Comment).count(10))  # Each post has 10 comments
    )
    .create()

# For() method for belongs-to
posts: @factory(Post)
    .count(10)
    .for(@factory(User).state("premium"))  # All posts belong to premium user
    .create()

# Many-to-many relationships
#factory User {
    afterCreating(user, faker) {
        # Attach random roles
        roles: @Role.inRandomOrder().limit(faker.numberBetween(1, 3)).get()
        user.roles().attach(roles)
        
        # Attach with pivot data
        teams: @factory(Team).count(2).create()
        for (team in teams) {
            user.teams().attach(team, {
                role: faker.randomElement(["member", "leader", "admin"]),
                joined_at: faker.dateTimeBetween("-1 year", "now")
            })
        }
    }
}
```

## Factory Inheritance

```tusk
# Base factory
#factory Person {
    first_name: @faker.firstName()
    last_name: @faker.lastName()
    birth_date: @faker.dateTimeBetween("-80 years", "-18 years")
    phone: @faker.phoneNumber()
    address: @faker.streetAddress()
    city: @faker.city()
    country: @faker.country()
}

# Extend base factory
#factory Employee extends Person {
    employee_id: @faker.unique().numerify("EMP-####")
    department: @faker.randomElement(["Sales", "Engineering", "HR", "Marketing"])
    position: @faker.jobTitle()
    salary: @faker.numberBetween(30000, 150000)
    hired_at: @faker.dateTimeBetween("-5 years", "now")
    
    state("executive") {
        department: "Executive"
        position: @faker.randomElement(["CEO", "CTO", "CFO", "COO"])
        salary: @faker.numberBetween(200000, 500000)
    }
}

#factory Customer extends Person {
    customer_number: @faker.unique().numerify("CUST-######")
    credit_limit: @faker.numberBetween(1000, 50000)
    preferred_payment: @faker.randomElement(["credit_card", "paypal", "bank_transfer"])
    registered_at: @faker.dateTimeBetween("-3 years", "now")
}
```

## Advanced Factory Usage

```tusk
# Raw attributes (not creating model)
user_data: @factory(User).raw()  # Returns array
user_data: @factory(User).count(5).raw()  # Returns array of arrays

# Make models without persisting
user: @factory(User).make()  # Returns model instance without saving
users: @factory(User).count(5).make()  # Returns collection

# Custom connection
user: @factory(User).connection("mysql").create()

# Recursive factories
#factory Category {
    name: @faker.word()
    slug: (attr) => @str_slug(attr.name)
    
    # Create with children
    afterCreating(category, faker) {
        if (faker.boolean(0.5)) {  # 50% have children
            child_count: faker.numberBetween(1, 5)
            
            for (i in 1..child_count) {
                @factory(Category).create({
                    parent_id: category.id
                })
            }
        }
    }
}

# Conditional factory logic
#factory Order {
    status: @faker.randomElement(["pending", "processing", "completed", "cancelled"])
    total: @faker.randomFloat(2, 50, 5000)
    
    afterMaking(order, faker) {
        # Set date based on status
        match order.status {
            "completed" => {
                order.completed_at: faker.dateTimeBetween("-30 days", "now")
                order.shipped_at: faker.dateTimeBetween("-35 days", order.completed_at)
            }
            "cancelled" => {
                order.cancelled_at: faker.dateTimeBetween("-30 days", "now")
                order.cancellation_reason: faker.sentence()
            }
            "processing" => {
                order.processing_started_at: faker.dateTimeBetween("-2 days", "now")
            }
        }
    }
}
```

## Factory Macros and Helpers

```tusk
# Define factory macros
@Factory.macro("withAddress", () => {
    return @tap((factory) => {
        factory.merge({
            address_line_1: @faker.streetAddress(),
            address_line_2: @faker.boolean(0.3) ? @faker.secondaryAddress() : null,
            city: @faker.city(),
            state: @faker.stateAbbr(),
            postal_code: @faker.postcode(),
            country: @faker.countryCode()
        })
    })
})

@Factory.macro("withPhone", (type = "mobile") => {
    return @tap((factory) => {
        factory.merge({
            phone_type: type,
            phone_number: type == "mobile" ? @faker.cellNumber() : @faker.phoneNumber(),
            phone_verified: @faker.boolean(0.8)
        })
    })
})

# Use macros
user: @factory(User)
    .withAddress()
    .withPhone("mobile")
    .create()

# Factory helper functions
helpers: {
    generate_username: (first_name, last_name) => {
        base: @str_lower(first_name + "." + last_name)
        suffix: @faker.numberBetween(1, 999)
        return base + suffix
    }
    
    generate_company_email: (name, company) => {
        first: @str_lower(@str_before(name, " "))
        domain: @str_slug(company)
        return first + "@" + domain + ".com"
    }
}

#factory Employee {
    first_name: @faker.firstName()
    last_name: @faker.lastName()
    company: @faker.company()
    
    username: (attr) => helpers.generate_username(attr.first_name, attr.last_name)
    email: (attr) => helpers.generate_company_email(attr.first_name + " " + attr.last_name, attr.company)
}
```

## Testing with Factories

```tusk
#test "User can create posts" {
    # Create test data
    user: @factory(User).create()
    
    # Act
    post: user.posts().create({
        title: "Test Post",
        content: "Test content"
    })
    
    # Assert
    @assert.equals(post.user_id, user.id)
    @assert.equals(user.posts.count(), 1)
}

#test "Premium users have higher limits" {
    regular_user: @factory(User).create()
    premium_user: @factory(User).state("premium").create()
    
    @assert.true(premium_user.credit_limit > regular_user.credit_limit)
    @assert.notNull(premium_user.subscription_expires)
}

# Test helper using factories
testAs(user_factory, callback) {
    user: user_factory.create()
    @actingAs(user)
    callback(user)
}

#test "Admin can access dashboard" {
    testAs(@factory(User).state("admin"), (admin) => {
        response: @get("/admin/dashboard")
        response.assertOk()
        response.assertSee("Admin Dashboard")
    })
}
```

## Best Practices

1. **Use faker for realistic data** - Makes testing more reliable
2. **Define states for variations** - Avoid duplicating factories
3. **Keep factories focused** - One factory per model
4. **Use callbacks wisely** - Don't create too many related models
5. **Make data deterministic when needed** - Use seeds for consistent tests
6. **Extract common patterns** - Use inheritance or macros
7. **Document complex factories** - Explain relationships and states
8. **Clean up after tests** - Use database transactions in tests

## Related Topics

- `faker-methods` - Available faker methods
- `database-seeding` - Using factories in seeders
- `testing` - Testing with factories
- `model-relationships` - Defining relationships
- `factory-patterns` - Advanced patterns
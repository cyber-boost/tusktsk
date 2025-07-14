# Database Seeding in TuskLang

Database seeding allows you to populate your database with test data. This is useful for development, testing, and setting up initial application data.

## Creating Seeders

```tusk
# Basic seeder structure
#seeder UserSeeder {
    run() {
        # Create multiple users
        for (i in 1..10) {
            @User.create({
                name: "User " + i,
                email: "user" + i + "@example.com",
                password: @bcrypt("password")
            })
        }
    }
}

# Seeder with relationships
#seeder BlogSeeder {
    run() {
        users: @User.all()
        
        for (user in users) {
            # Create posts for each user
            post_count: @random(1, 5)
            
            for (i in 1..post_count) {
                post: user.posts().create({
                    title: "Post " + i + " by " + user.name,
                    content: @faker.paragraphs(3),
                    published: @random_bool(0.8)  # 80% published
                })
                
                # Create comments
                comment_count: @random(0, 10)
                for (j in 1..comment_count) {
                    commenter: users.random()
                    post.comments().create({
                        user_id: commenter.id,
                        body: @faker.sentence()
                    })
                }
            }
        }
    }
}
```

## Using Faker for Realistic Data

```tusk
#seeder CustomerSeeder {
    run() {
        for (i in 1..100) {
            customer: @Customer.create({
                # Personal info
                first_name: @faker.firstName(),
                last_name: @faker.lastName(),
                email: @faker.unique().safeEmail(),
                phone: @faker.phoneNumber(),
                
                # Address
                address: @faker.streetAddress(),
                city: @faker.city(),
                state: @faker.state(),
                country: @faker.country(),
                postal_code: @faker.postcode(),
                
                # Additional data
                company: @faker.company(),
                website: @faker.url(),
                bio: @faker.paragraph(),
                avatar: @faker.imageUrl(200, 200, "people"),
                
                # Dates
                birth_date: @faker.dateTimeBetween("-60 years", "-18 years"),
                joined_at: @faker.dateTimeBetween("-2 years", "now"),
                
                # Status
                is_active: @faker.boolean(0.9),  # 90% active
                credit_limit: @faker.randomFloat(2, 1000, 50000)
            })
            
            # Create related data
            @seed_customer_orders(customer)
        }
    }
    
    seed_customer_orders(customer) {
        order_count: @faker.numberBetween(0, 20)
        
        for (i in 1..order_count) {
            order: customer.orders().create({
                order_number: @faker.unique().numerify("ORD-####-####"),
                status: @faker.randomElement(["pending", "processing", "shipped", "delivered", "cancelled"]),
                total: 0,  # Will calculate
                ordered_at: @faker.dateTimeBetween(customer.joined_at, "now")
            })
            
            # Add order items
            item_count: @faker.numberBetween(1, 5)
            total: 0
            
            for (j in 1..item_count) {
                product: @Product.inRandomOrder().first()
                quantity: @faker.numberBetween(1, 3)
                price: product.price
                
                order.items().create({
                    product_id: product.id,
                    quantity: quantity,
                    price: price,
                    total: price * quantity
                })
                
                total += price * quantity
            }
            
            order.update({total: total})
        }
    }
}
```

## Model Factories

```tusk
# Define a factory
#factory User {
    name: @faker.name()
    email: @faker.unique().safeEmail()
    email_verified_at: @faker.optional(0.7).dateTime()  # 70% verified
    password: @bcrypt("password")
    remember_token: @str_random(60)
    created_at: @faker.dateTimeBetween("-1 year", "now")
    
    # States
    state("admin") {
        role: "admin"
        email_verified_at: @now()
    }
    
    state("unverified") {
        email_verified_at: null
    }
    
    state("suspended") {
        suspended_at: @now()
        suspension_reason: @faker.sentence()
    }
    
    # After creating hook
    afterCreating(user) {
        user.profile().create(@factory(Profile).make())
    }
}

#factory Post {
    title: @faker.sentence()
    slug: @faker.slug()
    content: @faker.paragraphs(5, true)
    excerpt: @faker.paragraph()
    published: @faker.boolean(0.8)
    published_at: @faker.optional(0.8).dateTimeBetween("-6 months", "now")
    views: @faker.numberBetween(0, 10000)
    
    # Relationships
    user_id: @factory(User)
    category_id: @factory(Category)
    
    # States
    state("draft") {
        published: false
        published_at: null
    }
    
    state("popular") {
        views: @faker.numberBetween(10000, 100000)
        comments_count: @faker.numberBetween(50, 200)
    }
}

# Using factories in seeders
#seeder FactorySeeder {
    run() {
        # Create single
        user: @factory(User).create()
        
        # Create multiple
        users: @factory(User).count(50).create()
        
        # Create with state
        admin: @factory(User).state("admin").create()
        
        # Create with attributes
        john: @factory(User).create({
            name: "John Doe",
            email: "john@example.com"
        })
        
        # Create with relationships
        users_with_posts: @factory(User)
            .count(10)
            .has(@factory(Post).count(5))  # Each user has 5 posts
            .create()
        
        # Complex relationships
        @factory(Post)
            .count(20)
            .state("popular")
            .has(@factory(Comment).count(10))  # Each post has 10 comments
            .for(@factory(User).state("admin"))  # All posts by admin user
            .create()
        
        # Make without persisting
        user_data: @factory(User).make()  # Returns attributes array
        custom_user: @User.create({...user_data, custom_field: "value"})
    }
}
```

## Conditional and Smart Seeding

```tusk
#seeder SmartSeeder {
    run() {
        # Only seed if empty
        if (@User.count() == 0) {
            @call(UserSeeder)
        }
        
        # Conditional seeding based on environment
        if (@env.app_env == "local") {
            @seed_test_data()
        } else if (@env.app_env == "staging") {
            @seed_demo_data()
        }
        
        # Ensure minimum data exists
        @ensure_roles_exist()
        @ensure_admin_user()
        @ensure_categories()
    }
    
    ensure_roles_exist() {
        roles: ["admin", "editor", "user"]
        
        for (role_name in roles) {
            @Role.firstOrCreate(
                {name: role_name},
                {
                    name: role_name,
                    description: @str_title(role_name) + " role"
                }
            )
        }
    }
    
    ensure_admin_user() {
        admin: @User.where("email", "admin@example.com").first()
        
        if (!admin) {
            admin: @User.create({
                name: "Admin User",
                email: "admin@example.com",
                password: @bcrypt(@env("ADMIN_PASSWORD", "secret")),
                email_verified_at: @now()
            })
            
            admin_role: @Role.where("name", "admin").first()
            admin.roles().attach(admin_role)
        }
    }
    
    ensure_categories() {
        categories: [
            {name: "Technology", slug: "technology", color: "#3498db"},
            {name: "Business", slug: "business", color: "#2ecc71"},
            {name: "Health", slug: "health", color: "#e74c3c"},
            {name: "Education", slug: "education", color: "#f39c12"},
            {name: "Entertainment", slug: "entertainment", color: "#9b59b6"}
        ]
        
        for (cat in categories) {
            @Category.firstOrCreate(
                {slug: cat.slug},
                cat
            )
        }
    }
}
```

## CSV and JSON Data Import

```tusk
#seeder ImportSeeder {
    run() {
        # Import from CSV
        @import_from_csv("database/seeds/data/products.csv")
        
        # Import from JSON
        @import_from_json("database/seeds/data/categories.json")
        
        # Import from API
        @import_from_api("https://api.example.com/sample-data")
    }
    
    import_from_csv(file_path) {
        csv: @file.read_csv(file_path, {headers: true})
        
        for (row in csv) {
            @Product.updateOrCreate(
                {sku: row.sku},
                {
                    name: row.name,
                    description: row.description,
                    price: @float(row.price),
                    stock: @int(row.stock),
                    category: row.category
                }
            )
        }
    }
    
    import_from_json(file_path) {
        data: @file.json(file_path)
        
        for (item in data.categories) {
            category: @Category.create({
                name: item.name,
                slug: item.slug,
                parent_id: item.parent_id
            })
            
            # Import subcategories recursively
            if (item.children) {
                @import_subcategories(category, item.children)
            }
        }
    }
    
    import_from_api(url) {
        response: @http.get(url)
        
        if (response.successful()) {
            data: response.json()
            
            for (user in data.users) {
                @User.firstOrCreate(
                    {email: user.email},
                    {
                        name: user.name,
                        email: user.email,
                        external_id: user.id
                    }
                )
            }
        }
    }
}
```

## Performance Optimization

```tusk
#seeder LargeDataSeeder {
    run() {
        # Disable query logging for performance
        @db.disableQueryLog()
        
        # Chunk inserts for large datasets
        users: []
        
        for (i in 1..100000) {
            users.push({
                name: @faker.name(),
                email: @faker.unique().email(),
                password: @bcrypt("password"),
                created_at: @now(),
                updated_at: @now()
            })
            
            # Insert in chunks
            if (users.length >= 1000) {
                @User.insert(users)
                users: []  # Reset array
                
                # Show progress
                if (i % 10000 == 0) {
                    @console.info("Inserted " + i + " users...")
                }
            }
        }
        
        # Insert remaining
        if (users.length > 0) {
            @User.insert(users)
        }
        
        # Re-enable query logging
        @db.enableQueryLog()
    }
    
    # Use database transactions for consistency
    run_with_transaction() {
        @db.transaction((tx) => {
            # All operations in transaction
            for (i in 1..1000) {
                user: tx.table("users").insertGetId({
                    name: "User " + i,
                    email: "user" + i + "@example.com"
                })
                
                tx.table("profiles").insert({
                    user_id: user,
                    bio: @faker.paragraph()
                })
            }
        })
    }
}
```

## Seeder Management

```tusk
# Master seeder
#seeder DatabaseSeeder {
    run() {
        # Call seeders in order
        @call([
            RoleSeeder,
            UserSeeder,
            CategorySeeder,
            ProductSeeder,
            OrderSeeder
        ])
        
        # Conditional seeding
        if (@env.seed_test_data) {
            @call(TestDataSeeder)
        }
        
        # Environment-specific
        seeders: match @env.app_env {
            "local" => [DevelopmentSeeder]
            "staging" => [StagingSeeder]
            "production" => [ProductionSeeder]
            _ => []
        }
        
        @call(seeders)
    }
    
    # Call seeder with progress
    call_with_progress(seeder_class, count) {
        @console.info("Running " + seeder_class.name + "...")
        
        progress: @console.progress(count)
        
        seeder: new seeder_class()
        seeder.on("progress", (current) => {
            progress.advance()
        })
        
        seeder.run()
        progress.finish()
    }
}

# Running seeders via CLI
@cli.command("db:seed", (args) => {
    seeder_class: args.class || "DatabaseSeeder"
    
    @console.info("Seeding database...")
    start: @microtime(true)
    
    seeder: @resolve(seeder_class)
    seeder.run()
    
    duration: @microtime(true) - start
    @console.success("Database seeding completed in " + duration + "s")
})
```

## Testing with Seeders

```tusk
# Test-specific seeder
#seeder TestSeeder {
    run() {
        # Create predictable test data
        @User.create({
            id: 1,
            name: "Test User",
            email: "test@example.com",
            password: @bcrypt("password")
        })
        
        @Product.create({
            id: 1,
            name: "Test Product",
            price: 99.99,
            stock: 100
        })
    }
}

# Use in tests
#test "Order creation" {
    # Seed test data
    @seed(TestSeeder)
    
    user: @User.find(1)
    product: @Product.find(1)
    
    order: @Order.create({
        user_id: user.id,
        total: product.price
    })
    
    @assert.equals(order.user_id, 1)
    @assert.equals(order.total, 99.99)
}

# Refresh database between tests
#test "Fresh database" {
    @refresh_database()
    @seed(TestSeeder)
    
    # Test with fresh seeded data
}
```

## Best Practices

1. **Use factories for flexibility** - Define reusable data patterns
2. **Make seeders idempotent** - Running twice shouldn't duplicate data
3. **Use faker for realistic data** - Better testing and demos
4. **Chunk large inserts** - Prevent memory issues
5. **Environment-specific seeding** - Different data for dev/staging/prod
6. **Document seed data** - Explain what each seeder creates
7. **Version control seeders** - Track changes over time
8. **Test seeders** - Ensure they work correctly

## Related Topics

- `model-factories` - Factory pattern details
- `faker-library` - Fake data generation
- `database-testing` - Testing with seed data
- `migrations` - Schema management
- `data-import` - Importing external data
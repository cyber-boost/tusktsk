# Testing Framework in TuskLang

TuskLang includes a comprehensive testing framework that makes it easy to write and run tests for your applications, ensuring code quality and preventing regressions.

## Writing Tests

```tusk
# Basic test structure
#test "User can be created" {
    user: @User.create({
        name: "John Doe",
        email: "john@example.com"
    })
    
    @assert.equals(user.name, "John Doe")
    @assert.equals(user.email, "john@example.com")
    @assert.true(user.exists)
}

# Async tests
#test "API returns user data" async {
    response: await @http.get("/api/user/1")
    
    @assert.equals(response.status, 200)
    @assert.hasProperty(response.json(), "name")
    @assert.equals(response.json().id, 1)
}

# Test with setup and teardown
#test "Order calculation" {
    # Setup
    @beforeEach(() => {
        @db.beginTransaction()
        
        this.user: @factory(User).create()
        this.products: @factory(Product).count(3).create()
    })
    
    # Teardown
    @afterEach(() => {
        @db.rollback()
    })
    
    # Test
    order: @Order.create({
        user_id: this.user.id,
        items: this.products.map(p => ({product_id: p.id, quantity: 2}))
    })
    
    expected_total: this.products.sum(p => p.price * 2)
    @assert.equals(order.total, expected_total)
}
```

## Test Suites and Organization

```tusk
# Group related tests
#suite "User Authentication" {
    # Suite-level setup
    @beforeAll(() => {
        this.auth_service: new AuthService()
        this.test_users: @factory(User).count(5).create()
    })
    
    @afterAll(() => {
        @cleanup_test_data()
    })
    
    #test "Valid credentials authenticate successfully" {
        user: this.test_users[0]
        result: this.auth_service.attempt({
            email: user.email,
            password: "password"
        })
        
        @assert.true(result.success)
        @assert.equals(result.user.id, user.id)
    }
    
    #test "Invalid credentials fail authentication" {
        result: this.auth_service.attempt({
            email: "wrong@example.com",
            password: "wrongpassword"
        })
        
        @assert.false(result.success)
        @assert.equals(result.error, "Invalid credentials")
    }
    
    #test "Account lockout after failed attempts" {
        user: this.test_users[1]
        
        # Make multiple failed attempts
        for (i in 1..5) {
            this.auth_service.attempt({
                email: user.email,
                password: "wrong"
            })
        }
        
        # Should be locked out
        result: this.auth_service.attempt({
            email: user.email,
            password: "password"  # Even correct password fails
        })
        
        @assert.false(result.success)
        @assert.equals(result.error, "Account locked")
    }
}

# Nested test suites
#suite "E-commerce" {
    #suite "Shopping Cart" {
        #test "Add items to cart" {
            cart: new Cart()
            product: @factory(Product).create()
            
            cart.add(product, 2)
            
            @assert.equals(cart.count(), 1)
            @assert.equals(cart.quantity(product), 2)
        }
        
        #test "Remove items from cart" {
            // Test implementation
        }
    }
    
    #suite "Checkout Process" {
        #test "Calculate shipping" {
            // Test implementation
        }
        
        #test "Apply discounts" {
            // Test implementation
        }
    }
}
```

## Assertions

```tusk
# Basic assertions
@assert.true(value)
@assert.false(value)
@assert.equals(actual, expected)
@assert.notEquals(actual, expected)
@assert.strictEquals(actual, expected)  # === comparison
@assert.null(value)
@assert.notNull(value)
@assert.undefined(value)
@assert.notUndefined(value)

# Type assertions
@assert.isString(value)
@assert.isNumber(value)
@assert.isBoolean(value)
@assert.isArray(value)
@assert.isObject(value)
@assert.isFunction(value)
@assert.instanceOf(object, Class)

# Comparison assertions
@assert.greaterThan(actual, expected)
@assert.greaterThanOrEqual(actual, expected)
@assert.lessThan(actual, expected)
@assert.lessThanOrEqual(actual, expected)
@assert.between(value, min, max)

# String assertions
@assert.contains(haystack, needle)
@assert.notContains(haystack, needle)
@assert.startsWith(string, prefix)
@assert.endsWith(string, suffix)
@assert.matches(string, pattern)  # Regex

# Array/Object assertions
@assert.includes(array, item)
@assert.notIncludes(array, item)
@assert.hasProperty(object, property)
@assert.hasProperties(object, [prop1, prop2])
@assert.deepEquals(actual, expected)  # Deep comparison
@assert.isEmpty(collection)
@assert.isNotEmpty(collection)
@assert.lengthOf(collection, length)

# Exception assertions
@assert.throws(() => {
    @risky_operation()
})

@assert.throws(() => {
    @risky_operation()
}, "Expected error message")

@assert.throws(() => {
    @risky_operation()
}, ErrorClass)

@assert.doesNotThrow(() => {
    @safe_operation()
})

# Async assertions
@assert.rejects(async () => {
    await @failing_async_operation()
})

@assert.resolves(async () => {
    await @successful_async_operation()
})

# Custom assertions
@assert.custom(value, (v) => {
    return v > 0 && v < 100
}, "Value must be between 0 and 100")
```

## Mocking and Stubbing

```tusk
# Create mocks
#test "Service uses repository" {
    # Create mock
    mock_repo: @mock(UserRepository)
    
    # Set expectations
    mock_repo.expects("find")
        .withArgs(1)
        .once()
        .returns({id: 1, name: "Test User"})
    
    # Use mock
    service: new UserService(mock_repo)
    user: service.getUser(1)
    
    # Verify expectations
    @assert.equals(user.name, "Test User")
    mock_repo.verify()  # Verify all expectations met
}

# Partial mocks (spies)
#test "Method calls tracked" {
    service: new EmailService()
    
    # Spy on method
    spy: @spy(service, "send")
    
    # Use service normally
    service.sendWelcomeEmail("user@example.com")
    service.sendPasswordReset("user@example.com")
    
    # Verify calls
    @assert.equals(spy.callCount(), 2)
    @assert.true(spy.calledWith("user@example.com"))
    @assert.equals(spy.firstCall().args[0], "user@example.com")
}

# Stub external services
#test "API client handles responses" {
    # Stub HTTP requests
    @stub(@http, "get")
        .withArgs("/api/users")
        .returns({
            status: 200,
            json: () => [{id: 1, name: "User 1"}]
        })
    
    client: new ApiClient()
    users: await client.getUsers()
    
    @assert.lengthOf(users, 1)
    @assert.equals(users[0].name, "User 1")
}

# Time mocking
#test "Cache expires after TTL" {
    # Freeze time
    @clock.freeze("2024-01-01 12:00:00")
    
    cache: new Cache()
    cache.put("key", "value", 3600)  # 1 hour TTL
    
    # Advance time
    @clock.advance(3601)  # 1 hour + 1 second
    
    @assert.null(cache.get("key"))
    
    # Restore real time
    @clock.restore()
}
```

## HTTP Testing

```tusk
# Test HTTP endpoints
#test "GET /users returns user list" {
    response: @get("/users")
    
    response.assertOk()
    response.assertJson()
    response.assertJsonCount(10)
    response.assertJsonStructure([
        "*" => ["id", "name", "email"]
    ])
}

#test "POST /users creates user" {
    response: @post("/users", {
        name: "New User",
        email: "new@example.com",
        password: "secret123"
    })
    
    response.assertCreated()
    response.assertJson({
        name: "New User",
        email: "new@example.com"
    })
    
    # Verify in database
    @assertDatabaseHas("users", {
        email: "new@example.com"
    })
}

# Test with authentication
#test "Protected route requires auth" {
    # Without auth
    response: @get("/admin/dashboard")
    response.assertUnauthorized()
    
    # With auth
    user: @factory(User).create({role: "admin"})
    response: @actingAs(user).get("/admin/dashboard")
    response.assertOk()
    response.assertSee("Admin Dashboard")
}

# Test file uploads
#test "File upload endpoint" {
    file: @UploadedFile.fake().image("avatar.jpg", 100, 100)
    
    response: @post("/upload", {
        avatar: file
    })
    
    response.assertOk()
    response.assertJson({
        filename: "avatar.jpg",
        size: file.size()
    })
    
    # Verify file stored
    @assertFileExists("uploads/avatar.jpg")
}

# Test API with headers
#test "API requires valid token" {
    token: "Bearer valid-api-token"
    
    response: @withHeaders({
        "Authorization": token,
        "Accept": "application/json"
    }).get("/api/protected")
    
    response.assertOk()
}
```

## Database Testing

```tusk
# Database assertions
#test "User registration saves to database" {
    # Start with clean state
    @assertDatabaseCount("users", 0)
    
    # Register user
    @post("/register", {
        name: "Test User",
        email: "test@example.com",
        password: "password123"
    })
    
    # Verify database state
    @assertDatabaseHas("users", {
        email: "test@example.com"
    })
    
    @assertDatabaseMissing("users", {
        email: "other@example.com"
    })
    
    @assertDatabaseCount("users", 1)
}

# Test transactions
#test "Failed payment rolls back order" {
    @expectsTransaction(() => {
        order: @Order.create({
            user_id: 1,
            total: 100
        })
        
        # This will fail and trigger rollback
        @PaymentService.charge(order, invalid_card)
    })
    
    # Verify rollback
    @assertDatabaseCount("orders", 0)
}

# Seeded database testing
#test "Query scope returns correct results" {
    # Seed test data
    @seed(UserSeeder, {count: 10})
    @factory(User).state("inactive").count(5).create()
    
    # Test scope
    active_users: @User.active().get()
    
    @assert.lengthOf(active_users, 10)
}
```

## Test Doubles and Fakes

```tusk
# Create test doubles
class FakeMailer extends Mailer {
    sent: []
    
    send(to, subject, body) {
        this.sent.push({to, subject, body})
        return true
    }
    
    assertSent(to, subject = null) {
        found: this.sent.find(mail => {
            return mail.to === to && 
                   (!subject || mail.subject === subject)
        })
        
        @assert.notNull(found, `Mail not sent to ${to}`)
    }
    
    assertNotSent(to) {
        found: this.sent.find(mail => mail.to === to)
        @assert.null(found, `Mail was sent to ${to}`)
    }
    
    assertSentCount(count) {
        @assert.equals(this.sent.length, count)
    }
}

# Use fake in tests
#test "Welcome email sent on registration" {
    fake_mailer: new FakeMailer()
    @app.instance(Mailer, fake_mailer)
    
    # Register user
    @post("/register", {
        email: "new@example.com",
        name: "New User"
    })
    
    # Verify email sent
    fake_mailer.assertSent("new@example.com", "Welcome to our app!")
    fake_mailer.assertSentCount(1)
}

# In-memory implementations
class InMemoryCache extends Cache {
    data: {}
    
    get(key) {
        return this.data[key]?.value || null
    }
    
    put(key, value, ttl) {
        this.data[key] = {
            value,
            expires: Date.now() + ttl * 1000
        }
    }
    
    forget(key) {
        delete this.data[key]
    }
    
    flush() {
        this.data = {}
    }
}
```

## Browser Testing

```tusk
# Browser automation tests
#test "User can complete checkout" browser {
    # Visit page
    @visit("/shop")
    
    # Interact with page
    @click("Add to Cart").on(".product:first-child")
    @click("Checkout")
    
    # Fill form
    @type("Test User").into("#name")
    @type("test@example.com").into("#email")
    @type("4242 4242 4242 4242").into("#card-number")
    
    # Submit
    @click("Complete Order")
    
    # Assert results
    @assertSee("Order Confirmed")
    @assertUrlIs("/order-success")
    @assertPresent(".order-number")
}

# JavaScript testing
#test "Modal opens and closes" browser {
    @visit("/page-with-modal")
    
    # Assert hidden initially
    @assertNotVisible("#modal")
    
    # Open modal
    @click("[data-open-modal]")
    @assertVisible("#modal")
    
    # Close modal
    @click("[data-close-modal]")
    @assertNotVisible("#modal")
    
    # Test escape key
    @click("[data-open-modal]")
    @press("Escape")
    @assertNotVisible("#modal")
}

# Wait for dynamic content
#test "Ajax content loads" browser {
    @visit("/ajax-page")
    
    @click("#load-more")
    
    # Wait for content
    @waitFor(".new-content", 5000)
    @assertSee("Loaded content")
    
    # Wait for text
    @waitForText("Loading complete")
    
    # Wait for custom condition
    @waitUntil(() => {
        return @elements(".item").length >= 10
    }, 10000)
}
```

## Test Configuration

```tusk
# test.config.tsk
{
    # Test directories
    test_paths: [
        "tests/unit",
        "tests/integration",
        "tests/browser"
    ],
    
    # Test environment
    environment: {
        APP_ENV: "testing",
        DB_CONNECTION: "sqlite",
        DB_DATABASE: ":memory:",
        CACHE_DRIVER: "array",
        QUEUE_DRIVER: "sync"
    },
    
    # Coverage settings
    coverage: {
        enabled: true,
        include: ["app/**"],
        exclude: ["app/tests/**", "app/vendor/**"],
        reporters: ["html", "text", "clover"],
        threshold: {
            statements: 80,
            branches: 75,
            functions: 80,
            lines: 80
        }
    },
    
    # Parallel execution
    parallel: {
        enabled: true,
        workers: 4
    },
    
    # Retry flaky tests
    retry: {
        times: 3,
        delay: 1000
    },
    
    # Global timeout
    timeout: 30000,
    
    # Before/after hooks
    setup: "tests/setup.tsk",
    teardown: "tests/teardown.tsk"
}
```

## Running Tests

```tusk
# CLI commands
# Run all tests
@test

# Run specific file
@test tests/unit/UserTest.tsk

# Run specific suite
@test --suite "User Authentication"

# Run with pattern
@test --grep "login"

# Run with coverage
@test --coverage

# Run in watch mode
@test --watch

# Run specific test method
@test tests/UserTest.tsk::test_user_creation

# Parallel execution
@test --parallel --workers=8

# With specific environment
@test --env=testing.ci
```

## Best Practices

1. **Write tests first** - TDD improves design
2. **Keep tests focused** - One assertion per test
3. **Use descriptive names** - Test names should explain what they test
4. **Mock external services** - Tests should be fast and isolated
5. **Clean up after tests** - Don't leave test data
6. **Test edge cases** - Not just happy paths
7. **Maintain test coverage** - Aim for 80%+ coverage
8. **Run tests frequently** - Catch issues early

## Related Topics

- `mocking` - Mock object patterns
- `test-doubles` - Fakes, stubs, spies
- `browser-testing` - Browser automation
- `continuous-integration` - CI/CD setup
- `test-driven-development` - TDD practices
# @ Session Variables - Java Edition

The `@session` object provides access to session variables in Java applications. This guide covers session management, state persistence, user sessions, and implementing robust session-based logic using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 Session Variables Overview

### Accessing Session Variables in Java

```java
@RestController
@RequestMapping("/api")
public class SessionController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @GetMapping("/profile")
    public ResponseEntity<?> getProfile(HttpSession session) {
        // TuskLang handles session variable processing
        return tuskConfig.getSessionHandler().getUserProfile(session);
    }
    
    @PostMapping("/login")
    public ResponseEntity<?> login(@RequestBody LoginRequest request, HttpSession session) {
        // TuskLang handles session creation and management
        return tuskConfig.getSessionHandler().handleLogin(request, session);
    }
    
    @PostMapping("/logout")
    public ResponseEntity<?> logout(HttpSession session) {
        // TuskLang handles session cleanup
        return tuskConfig.getSessionHandler().handleLogout(session);
    }
}
```

```tusk
# app.tsk - Session handling configuration
session_handlers: {
    get_user_profile: @lambda({
        # Access session variables
        user_id = @session.get("user_id")
        
        @if(!user_id, {
            return: {
                status: 401
                body: { error: "User not authenticated" }
            }
        })
        
        # Get user profile via Java service
        user_profile = @java.invoke("UserService", "getProfile", user_id)
        
        return: {
            status: 200
            body: { profile: user_profile }
        }
    }),
    
    handle_login: @lambda({
        # Validate login credentials
        login_validation = @java.invoke("AuthService", "validateLogin", {
            username: @request.post.username
            password: @request.post.password
        })
        
        @if(!login_validation.valid, {
            return: {
                status: 401
                body: { error: "Invalid credentials" }
            }
        })
        
        # Set session variables
        @session.set("user_id", login_validation.user.id)
        @session.set("username", login_validation.user.username)
        @session.set("email", login_validation.user.email)
        @session.set("roles", login_validation.user.roles)
        @session.set("login_time", @time.now())
        @session.set("last_activity", @time.now())
        
        # Track login via Java service
        @java.invoke("SessionTrackingService", "trackLogin", login_validation.user.id)
        
        return: {
            status: 200
            body: { 
                message: "Login successful",
                user: login_validation.user
            }
        }
    }),
    
    handle_logout: @lambda({
        # Get user ID before clearing session
        user_id = @session.get("user_id")
        
        # Track logout via Java service
        @if(user_id, {
            @java.invoke("SessionTrackingService", "trackLogout", user_id)
        })
        
        # Clear all session variables
        @session.clear()
        
        # Invalidate session
        @session.invalidate()
        
        return: {
            status: 200
            body: { message: "Logout successful" }
        }
    })
}
```

## 🔐 Session Management

### Session Creation and Validation

```tusk
# Session management with Java integration
session_management: {
    # Create new session
    create_session: @lambda({
        # Generate session ID
        session_id = @java.invoke("SessionService", "generateSessionId")
        
        # Set session metadata
        @session.set("session_id", session_id)
        @session.set("created_at", @time.now())
        @session.set("last_activity", @time.now())
        @session.set("ip_address", @request.ip)
        @session.set("user_agent", @request.headers["User-Agent"])
        
        # Initialize session state
        @session.set("authenticated", false)
        @session.set("login_attempts", 0)
        @session.set("csrf_token", @java.invoke("SecurityService", "generateCsrfToken"))
        
        return: {
            session_id: session_id
            created_at: @session.get("created_at")
        }
    }),
    
    # Validate session
    validate_session: @lambda({
        session_id = @session.get("session_id")
        
        @if(!session_id, {
            return: { valid: false, error: "No session ID" }
        })
        
        # Validate session via Java service
        validation_result = @java.invoke("SessionService", "validateSession", session_id)
        
        @if(!validation_result.valid, {
            # Clear invalid session
            @session.clear()
            return: { valid: false, error: validation_result.error }
        })
        
        # Update last activity
        @session.set("last_activity", @time.now())
        
        return: {
            valid: true
            user_id: @session.get("user_id")
            authenticated: @session.get("authenticated")
        }
    }),
    
    # Refresh session
    refresh_session: @lambda({
        # Validate current session
        validation = @validate_session()
        
        @if(!validation.valid, {
            return: { success: false, error: validation.error }
        })
        
        # Extend session timeout
        @session.set("last_activity", @time.now())
        @session.set("expires_at", @time.add(@time.now(), "30m"))
        
        # Update session in Java service
        @java.invoke("SessionService", "extendSession", @session.get("session_id"))
        
        return: { success: true, expires_at: @session.get("expires_at") }
    })
}
```

### User Session State

```tusk
# User session state management with Java integration
user_session_state: {
    # Set user session
    set_user_session: @lambda(user_data, {
        # Set user information
        @session.set("user_id", user_data.id)
        @session.set("username", user_data.username)
        @session.set("email", user_data.email)
        @session.set("full_name", user_data.full_name)
        @session.set("roles", user_data.roles)
        @session.set("permissions", user_data.permissions)
        
        # Set authentication state
        @session.set("authenticated", true)
        @session.set("login_time", @time.now())
        @session.set("last_activity", @time.now())
        @session.set("session_expires", @time.add(@time.now(), "2h"))
        
        # Set user preferences
        @session.set("preferences", user_data.preferences)
        @session.set("theme", user_data.theme ?? "default")
        @session.set("language", user_data.language ?? "en")
        @session.set("timezone", user_data.timezone ?? "UTC")
        
        # Track session creation via Java service
        @java.invoke("SessionTrackingService", "createUserSession", user_data.id)
    }),
    
    # Get user session info
    get_user_session: @lambda({
        user_id = @session.get("user_id")
        
        @if(!user_id, {
            return: { authenticated: false, user: null }
        })
        
        # Check if session is expired
        expires_at = @session.get("session_expires")
        @if(expires_at && @time.now() > expires_at, {
            @session.clear()
            return: { authenticated: false, user: null, error: "Session expired" }
        })
        
        # Get user info from Java service
        user_info = @java.invoke("UserService", "getUserById", user_id)
        
        return: {
            authenticated: true
            user: {
                id: user_id
                username: @session.get("username")
                email: @session.get("email")
                full_name: @session.get("full_name")
                roles: @session.get("roles")
                permissions: @session.get("permissions")
                preferences: @session.get("preferences")
                theme: @session.get("theme")
                language: @session.get("language")
                timezone: @session.get("timezone")
                login_time: @session.get("login_time")
                last_activity: @session.get("last_activity")
            }
        }
    }),
    
    # Update user session
    update_user_session: @lambda(updates, {
        # Update session variables
        @each(@keys(updates), @lambda(key, {
            @session.set(key, updates[key])
        }))
        
        # Update last activity
        @session.set("last_activity", @time.now())
        
        # Update session in Java service
        @java.invoke("SessionService", "updateSession", @session.get("session_id"), updates)
    })
}
```

## 🛒 Shopping Cart Sessions

### E-commerce Session Management

```tusk
# Shopping cart session management with Java integration
shopping_cart_sessions: {
    # Initialize shopping cart
    init_cart: @lambda({
        # Create empty cart
        cart = {
            items: []
            total_items: 0
            subtotal: 0.0
            tax: 0.0
            shipping: 0.0
            total: 0.0
            currency: "USD"
            created_at: @time.now()
            updated_at: @time.now()
        }
        
        @session.set("cart", cart)
        @session.set("cart_id", @java.invoke("CartService", "createCart"))
        
        return: cart
    }),
    
    # Add item to cart
    add_to_cart: @lambda(product_data, {
        cart = @session.get("cart") ?? @init_cart()
        
        # Check if item already exists
        existing_item = @find(cart.items, @lambda(item, item.product_id == product_data.id))
        
        @if(existing_item, {
            # Update quantity
            existing_item.quantity = existing_item.quantity + product_data.quantity
            existing_item.updated_at = @time.now()
        }, {
            # Add new item
            new_item = {
                product_id: product_data.id
                name: product_data.name
                price: product_data.price
                quantity: product_data.quantity
                added_at: @time.now()
                updated_at: @time.now()
            }
            @push(cart.items, new_item)
        })
        
        # Recalculate totals
        cart = @recalculate_cart_totals(cart)
        @session.set("cart", cart)
        
        # Update cart in Java service
        @java.invoke("CartService", "updateCart", @session.get("cart_id"), cart)
        
        return: cart
    }),
    
    # Remove item from cart
    remove_from_cart: @lambda(product_id, {
        cart = @session.get("cart")
        
        @if(!cart, return: { error: "No cart found" })
        
        # Remove item
        cart.items = @filter(cart.items, @lambda(item, item.product_id != product_id))
        
        # Recalculate totals
        cart = @recalculate_cart_totals(cart)
        @session.set("cart", cart)
        
        # Update cart in Java service
        @java.invoke("CartService", "updateCart", @session.get("cart_id"), cart)
        
        return: cart
    }),
    
    # Recalculate cart totals
    recalculate_cart_totals: @lambda(cart, {
        # Calculate subtotal
        subtotal = @sum(@map(cart.items, @lambda(item, item.price * item.quantity)))
        
        # Calculate tax (example: 8.5%)
        tax = subtotal * 0.085
        
        # Calculate shipping (example: free over $50)
        shipping = subtotal >= 50.0 ? 0.0 : 5.99
        
        # Calculate total
        total = subtotal + tax + shipping
        
        # Update cart
        cart.subtotal = subtotal
        cart.tax = tax
        cart.shipping = shipping
        cart.total = total
        cart.total_items = @sum(@map(cart.items, @lambda(item, item.quantity)))
        cart.updated_at = @time.now()
        
        return: cart
    }),
    
    # Clear cart
    clear_cart: @lambda({
        @session.remove("cart")
        @session.remove("cart_id")
        
        return: { message: "Cart cleared" }
    })
}
```

## 🔄 Multi-Step Form Sessions

### Form State Persistence

```tusk
# Multi-step form session management with Java integration
form_sessions: {
    # Initialize form session
    init_form_session: @lambda(form_type, {
        form_session = {
            type: form_type
            current_step: 1
            total_steps: 0
            data: {}
            validation_errors: {}
            started_at: @time.now()
            last_activity: @time.now()
        }
        
        @session.set("form_session", form_session)
        
        return: form_session
    }),
    
    # Save form step data
    save_form_step: @lambda(step_data, {
        form_session = @session.get("form_session")
        
        @if(!form_session, {
            return: { error: "No form session found" }
        })
        
        # Validate step data
        validation = @java.invoke("FormValidator", "validateStep", form_session.type, form_session.current_step, step_data)
        
        @if(!validation.valid, {
            form_session.validation_errors = validation.errors
            @session.set("form_session", form_session)
            
            return: { 
                success: false, 
                errors: validation.errors 
            }
        })
        
        # Save step data
        form_session.data = @merge(form_session.data, step_data)
        form_session.current_step = form_session.current_step + 1
        form_session.last_activity = @time.now()
        form_session.validation_errors = {}
        
        @session.set("form_session", form_session)
        
        return: { 
            success: true, 
            current_step: form_session.current_step,
            total_steps: form_session.total_steps
        }
    }),
    
    # Get form progress
    get_form_progress: @lambda({
        form_session = @session.get("form_session")
        
        @if(!form_session, {
            return: { error: "No form session found" }
        })
        
        return: {
            type: form_session.type
            current_step: form_session.current_step
            total_steps: form_session.total_steps
            progress_percentage: (form_session.current_step - 1) / form_session.total_steps * 100
            started_at: form_session.started_at
            last_activity: form_session.last_activity
        }
    }),
    
    # Complete form
    complete_form: @lambda({
        form_session = @session.get("form_session")
        
        @if(!form_session, {
            return: { error: "No form session found" }
        })
        
        # Process form data via Java service
        result = @java.invoke("FormProcessor", "processForm", form_session.type, form_session.data)
        
        # Clear form session
        @session.remove("form_session")
        
        return: {
            success: true,
            result: result
        }
    })
}
```

## 🔧 Java Service Integration

### Session Management Service

```java
@Service
public class SessionManagementService {
    
    @Autowired
    private UserService userService;
    
    @Autowired
    private SessionRepository sessionRepository;
    
    @Autowired
    private SessionTrackingService sessionTrackingService;
    
    public ResponseEntity<?> getUserProfile(HttpSession session) {
        String userId = (String) session.getAttribute("user_id");
        
        if (userId == null) {
            return ResponseEntity.status(401)
                .body(Map.of("error", "User not authenticated"));
        }
        
        UserProfile profile = userService.getProfile(userId);
        return ResponseEntity.ok(Map.of("profile", profile));
    }
    
    public ResponseEntity<?> handleLogin(LoginRequest request, HttpSession session) {
        // Validate login credentials
        LoginValidationResult validation = userService.validateLogin(request);
        
        if (!validation.isValid()) {
            return ResponseEntity.status(401)
                .body(Map.of("error", "Invalid credentials"));
        }
        
        // Set session attributes
        session.setAttribute("user_id", validation.getUser().getId());
        session.setAttribute("username", validation.getUser().getUsername());
        session.setAttribute("email", validation.getUser().getEmail());
        session.setAttribute("roles", validation.getUser().getRoles());
        session.setAttribute("login_time", LocalDateTime.now());
        session.setAttribute("last_activity", LocalDateTime.now());
        
        // Track login
        sessionTrackingService.trackLogin(validation.getUser().getId());
        
        return ResponseEntity.ok(Map.of(
            "message", "Login successful",
            "user", validation.getUser()
        ));
    }
    
    public ResponseEntity<?> handleLogout(HttpSession session) {
        String userId = (String) session.getAttribute("user_id");
        
        if (userId != null) {
            sessionTrackingService.trackLogout(userId);
        }
        
        session.invalidate();
        
        return ResponseEntity.ok(Map.of("message", "Logout successful"));
    }
    
    public String generateSessionId() {
        return UUID.randomUUID().toString();
    }
    
    public SessionValidationResult validateSession(String sessionId) {
        Session session = sessionRepository.findBySessionId(sessionId);
        
        if (session == null) {
            return new SessionValidationResult(false, "Session not found");
        }
        
        if (session.isExpired()) {
            return new SessionValidationResult(false, "Session expired");
        }
        
        return new SessionValidationResult(true, null);
    }
    
    public void extendSession(String sessionId) {
        Session session = sessionRepository.findBySessionId(sessionId);
        if (session != null) {
            session.setLastActivity(LocalDateTime.now());
            session.setExpiresAt(LocalDateTime.now().plusMinutes(30));
            sessionRepository.save(session);
        }
    }
}
```

### Session Tracking Service

```java
@Service
public class SessionTrackingService {
    
    @Autowired
    private SessionEventRepository sessionEventRepository;
    
    public void trackLogin(String userId) {
        SessionEvent event = new SessionEvent();
        event.setUserId(userId);
        event.setEventType("LOGIN");
        event.setTimestamp(LocalDateTime.now());
        event.setIpAddress(getClientIpAddress());
        event.setUserAgent(getUserAgent());
        
        sessionEventRepository.save(event);
    }
    
    public void trackLogout(String userId) {
        SessionEvent event = new SessionEvent();
        event.setUserId(userId);
        event.setEventType("LOGOUT");
        event.setTimestamp(LocalDateTime.now());
        event.setIpAddress(getClientIpAddress());
        event.setUserAgent(getUserAgent());
        
        sessionEventRepository.save(event);
    }
    
    public void createUserSession(String userId) {
        SessionEvent event = new SessionEvent();
        event.setUserId(userId);
        event.setEventType("SESSION_CREATED");
        event.setTimestamp(LocalDateTime.now());
        event.setIpAddress(getClientIpAddress());
        event.setUserAgent(getUserAgent());
        
        sessionEventRepository.save(event);
    }
    
    private String getClientIpAddress() {
        // Implementation to get client IP address
        return "127.0.0.1";
    }
    
    private String getUserAgent() {
        // Implementation to get user agent
        return "Unknown";
    }
}
```

## 🚀 Advanced Session Features

### Session Analytics

```tusk
# Session analytics with Java integration
session_analytics: {
    # Track session metrics
    track_session_metrics: @lambda({
        # Extract session metrics
        session_metrics = {
            session_id: @session.get("session_id")
            user_id: @session.get("user_id")
            session_duration: @time.diff(@session.get("created_at"), @time.now())
            last_activity: @session.get("last_activity")
            page_views: @session.get("page_views") ?? 0
            actions_performed: @session.get("actions_performed") ?? 0
            timestamp: @time.now()
        }
        
        # Send metrics to Java analytics service
        @java.invoke("SessionAnalyticsService", "track", session_metrics)
    }),
    
    # Analyze session patterns
    analyze_session_patterns: @lambda({
        # Get session analysis from Java service
        analysis = @java.invoke("SessionAnalysisService", "analyze", @session.get("session_id"))
        
        # Log insights
        @if(analysis.insights, {
            @log.info("Session insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

### Session Security

```tusk
# Session security with Java integration
session_security: {
    # Validate session security
    validate_session_security: @lambda({
        # Check session hijacking protection
        security_checks = {
            ip_address: @session.get("ip_address") == @request.ip
            user_agent: @session.get("user_agent") == @request.headers["User-Agent"]
            session_age: @time.diff(@session.get("created_at"), @time.now()) < "24h"
            last_activity: @time.diff(@session.get("last_activity"), @time.now()) < "30m"
        }
        
        # Validate security via Java service
        security_validation = @java.invoke("SessionSecurityService", "validate", security_checks)
        
        @if(!security_validation.secure, {
            # Invalidate suspicious session
            @session.clear()
            @session.invalidate()
            
            return: { secure: false, action: "session_invalidated" }
        })
        
        return: { secure: true }
    }),
    
    # Regenerate session ID
    regenerate_session_id: @lambda({
        # Generate new session ID
        new_session_id = @java.invoke("SessionService", "generateSessionId")
        
        # Update session ID
        @session.set("session_id", new_session_id)
        @session.set("regenerated_at", @time.now())
        
        # Update session in Java service
        @java.invoke("SessionService", "regenerateSession", new_session_id)
        
        return: { new_session_id: new_session_id }
    })
}
```

## 🔒 Security Considerations

### Session Security Patterns

```tusk
# Session security configuration
session_security_patterns: {
    # Session configuration
    session_config: {
        max_session_age: "24h"
        max_idle_time: "30m"
        regenerate_id_interval: "15m"
        secure_cookies: true
        http_only_cookies: true
        same_site_policy: "strict"
    },
    
    # Session validation
    session_validation: {
        validate_ip_address: true
        validate_user_agent: true
        validate_session_age: true
        validate_idle_time: true
        block_suspicious_sessions: true
    },
    
    # Session logging
    session_logging: {
        enabled: true
        log_level: "info"
        log_session_creation: true
        log_session_destruction: true
        log_session_access: false
        mask_sensitive_data: true
    }
}
```

## 🧪 Testing Session Handlers

### Session Testing Configuration

```tusk
# Session testing configuration
session_testing: {
    # Test cases for session management
    session_test_cases: [
        {
            name: "create_session"
            action: "create_session"
            expected: { has_session_id: true, created_at: "not_null" }
        },
        {
            name: "set_user_session"
            action: "set_user_session"
            user_data: { id: "123", username: "testuser", email: "test@example.com" }
            expected: { authenticated: true, user_id: "123" }
        },
        {
            name: "validate_session"
            action: "validate_session"
            expected: { valid: true, authenticated: true }
        },
        {
            name: "clear_session"
            action: "clear_session"
            expected: { session_cleared: true }
        }
    ],
    
    # Test cases for shopping cart
    cart_test_cases: [
        {
            name: "init_cart"
            action: "init_cart"
            expected: { cart_created: true, total_items: 0 }
        },
        {
            name: "add_to_cart"
            action: "add_to_cart"
            product_data: { id: "1", name: "Product 1", price: 10.0, quantity: 2 }
            expected: { total_items: 2, subtotal: 20.0 }
        },
        {
            name: "remove_from_cart"
            action: "remove_from_cart"
            product_id: "1"
            expected: { total_items: 0, subtotal: 0.0 }
        }
    ]
}
```

## 🚀 Best Practices

### Session Handling Best Practices

1. **Use Java Services**: Delegate session management to Java services for better maintainability
2. **Implement Proper Security**: Use secure session management practices
3. **Handle Session Expiration**: Implement proper session timeout and cleanup
4. **Validate Session Data**: Always validate session data before use
5. **Use Session Analytics**: Track session patterns for optimization
6. **Implement Session Regeneration**: Regenerate session IDs periodically
7. **Handle Errors Gracefully**: Return appropriate error messages for session issues
8. **Clean Up Sessions**: Properly clean up sessions on logout or expiration

### Common Patterns

```tusk
# Common session handling patterns
common_patterns: {
    # Authentication patterns
    auth_patterns: {
        login_session: "User login with session creation"
        logout_session: "User logout with session cleanup"
        session_validation: "Session validation on each request"
        session_refresh: "Session refresh to extend timeout"
    },
    
    # State management patterns
    state_patterns: {
        shopping_cart: "Shopping cart state persistence"
        form_wizard: "Multi-step form state management"
        user_preferences: "User preference storage"
        application_state: "Application state persistence"
    },
    
    # Security patterns
    security_patterns: {
        session_hijacking_protection: "Protection against session hijacking"
        session_fixation_protection: "Protection against session fixation"
        session_regeneration: "Periodic session ID regeneration"
        session_timeout: "Automatic session timeout and cleanup"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle session variables with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're managing user sessions, shopping carts, or form state, TuskLang provides the tools you need to handle session data efficiently and securely. 
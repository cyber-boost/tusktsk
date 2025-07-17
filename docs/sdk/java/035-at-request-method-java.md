# @ Request Method - Java Edition

The `@request.method` property provides access to the HTTP method of the current request in Java applications. This guide covers how to handle different HTTP methods and implement RESTful patterns using TuskLang with Spring Boot and Java enterprise patterns.

## 🎯 HTTP Methods Overview

### Accessing the Method in Java

```java
// TuskLang configuration with Java integration
@RestController
@RequestMapping("/api")
public class UserController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    // TuskLang handles method routing
    @RequestMapping(value = "/users", method = {RequestMethod.GET, RequestMethod.POST, RequestMethod.PUT, RequestMethod.DELETE})
    public ResponseEntity<?> handleUserRequest(HttpServletRequest request) {
        // TuskLang configuration determines response based on method
        String method = request.getMethod();
        
        // Use TuskLang for method-based logic
        return tuskConfig.getMethodHandler(method).handle(request);
    }
}
```

```tusk
# app.tsk - Method-based routing configuration
method_handlers: {
    GET: @lambda({
        # List or retrieve resources
        return: @java.invoke("UserService", "getUsers", @request.query)
    }),
    POST: @lambda({
        # Create new resource
        return: @java.invoke("UserService", "createUser", @request.body)
    }),
    PUT: @lambda({
        # Replace entire resource
        return: @java.invoke("UserService", "updateUser", @request.params.id, @request.body)
    }),
    PATCH: @lambda({
        # Update specific fields
        return: @java.invoke("UserService", "patchUser", @request.params.id, @request.body)
    }),
    DELETE: @lambda({
        # Remove resource
        return: @java.invoke("UserService", "deleteUser", @request.params.id)
    })
}

# Route to appropriate handler
handle_request = @lambda({
    handler = @method_handlers[@request.method]
    
    @if(!handler, {
        return: {
            status: 405
            body: { error: "Method not allowed" }
            headers: { "Allow": @join(@keys(@method_handlers), ", ") }
        }
    })
    
    return: @handler()
})
```

### Standard HTTP Methods with Java Integration

```tusk
# HTTP method definitions with Java service mappings
http_methods: {
    GET: {
        description: "Retrieve resource(s)"
        java_service: "UserService"
        java_method: "getUsers"
        cacheable: true
        idempotent: true
    },
    POST: {
        description: "Create new resource"
        java_service: "UserService"
        java_method: "createUser"
        cacheable: false
        idempotent: false
    },
    PUT: {
        description: "Update entire resource"
        java_service: "UserService"
        java_method: "updateUser"
        cacheable: false
        idempotent: true
    },
    PATCH: {
        description: "Partial update"
        java_service: "UserService"
        java_method: "patchUser"
        cacheable: false
        idempotent: true
    },
    DELETE: {
        description: "Remove resource"
        java_service: "UserService"
        java_method: "deleteUser"
        cacheable: false
        idempotent: true
    }
}
```

## 🏗️ Method-Based Routing with Spring Boot

### Spring Boot Controller Integration

```java
@Service
public class TuskMethodHandler {
    
    @Autowired
    private UserService userService;
    
    @Autowired
    private OrderService orderService;
    
    public ResponseEntity<?> handleRequest(String method, HttpServletRequest request) {
        switch (method) {
            case "GET":
                return handleGet(request);
            case "POST":
                return handlePost(request);
            case "PUT":
                return handlePut(request);
            case "PATCH":
                return handlePatch(request);
            case "DELETE":
                return handleDelete(request);
            default:
                return ResponseEntity.status(405)
                    .header("Allow", "GET, POST, PUT, PATCH, DELETE")
                    .body(Map.of("error", "Method not allowed"));
        }
    }
    
    private ResponseEntity<?> handleGet(HttpServletRequest request) {
        String path = request.getRequestURI();
        if (path.startsWith("/api/users")) {
            return ResponseEntity.ok(userService.getUsers());
        } else if (path.startsWith("/api/orders")) {
            return ResponseEntity.ok(orderService.getOrders());
        }
        return ResponseEntity.notFound().build();
    }
    
    private ResponseEntity<?> handlePost(HttpServletRequest request) {
        // Implementation for POST requests
        return ResponseEntity.status(201).build();
    }
    
    // Other method handlers...
}
```

```tusk
# Method routing configuration
spring_integration: {
    # Map TuskLang methods to Spring services
    method_mappings: {
        GET: {
            service: "TuskMethodHandler"
            method: "handleGet"
            cache_strategy: "default"
        },
        POST: {
            service: "TuskMethodHandler"
            method: "handlePost"
            validation: "strict"
        },
        PUT: {
            service: "TuskMethodHandler"
            method: "handlePut"
            optimistic_locking: true
        },
        PATCH: {
            service: "TuskMethodHandler"
            method: "handlePatch"
            partial_update: true
        },
        DELETE: {
            service: "TuskMethodHandler"
            method: "handleDelete"
            soft_delete: true
        }
    }
}
```

### RESTful Resource Handling

```tusk
# Complete REST controller configuration
user_controller: {
    # GET /users - List all users
    # GET /users/:id - Get specific user
    GET: @lambda({
        @if(@request.params.id, {
            # Get single user via Java service
            user = @java.invoke("UserService", "findById", @request.params.id)
            
            @if(!user, {
                return: { 
                    status: 404, 
                    body: { error: "User not found" } 
                }
            })
            
            return: { body: { user: user } }
        }, {
            # List users with pagination via Java service
            page = @int(@request.query.page ?? 1)
            limit = @int(@request.query.limit ?? 20)
            
            result = @java.invoke("UserService", "findAllPaginated", page, limit)
            
            return: {
                body: {
                    users: result.users
                    pagination: result.pagination
                }
            }
        })
    }),
    
    # POST /users - Create new user
    POST: @lambda({
        # Validate via Java service
        validation_result = @java.invoke("UserValidator", "validateCreate", @request.body)
        
        @if(!validation_result.valid, {
            return: {
                status: 400
                body: { 
                    error: "Validation failed",
                    details: validation_result.errors
                }
            }
        })
        
        # Create user via Java service
        user = @java.invoke("UserService", "create", @request.body)
        
        return: {
            status: 201
            body: { user: user }
            headers: { "Location": "/users/${user.id}" }
        }
    }),
    
    # PUT /users/:id - Replace user
    PUT: @lambda({
        id = @request.params.id
        
        # Check if exists via Java service
        existing = @java.invoke("UserService", "findById", id)
        @if(!existing, {
            return: { status: 404, body: { error: "User not found" } }
        })
        
        # Replace entire record via Java service
        user = @java.invoke("UserService", "replace", id, @request.body)
        
        return: { body: { user: user } }
    }),
    
    # PATCH /users/:id - Partial update
    PATCH: @lambda({
        id = @request.params.id
        
        # Check if exists via Java service
        user = @java.invoke("UserService", "findById", id)
        @if(!user, {
            return: { status: 404, body: { error: "User not found" } }
        })
        
        # Partial update via Java service
        updated_user = @java.invoke("UserService", "patch", id, @request.body)
        
        return: { body: { user: updated_user } }
    }),
    
    # DELETE /users/:id - Remove user
    DELETE: @lambda({
        id = @request.params.id
        
        # Check if exists via Java service
        user = @java.invoke("UserService", "findById", id)
        @if(!user, {
            return: { status: 404, body: { error: "User not found" } }
        })
        
        # Soft delete via Java service
        @java.invoke("UserService", "delete", id)
        
        return: { status: 204 }
    })
}
```

## 🔧 Java Service Integration

### Service Layer Implementation

```java
@Service
@Transactional
public class UserService {
    
    @Autowired
    private UserRepository userRepository;
    
    @Autowired
    private UserValidator userValidator;
    
    public List<User> getUsers() {
        return userRepository.findAll();
    }
    
    public Page<User> findAllPaginated(int page, int limit) {
        Pageable pageable = PageRequest.of(page - 1, limit);
        return userRepository.findAll(pageable);
    }
    
    public User findById(Long id) {
        return userRepository.findById(id)
            .orElse(null);
    }
    
    public User create(UserCreateRequest request) {
        // Validation
        ValidationResult validation = userValidator.validateCreate(request);
        if (!validation.isValid()) {
            throw new ValidationException(validation.getErrors());
        }
        
        // Create user
        User user = new User();
        user.setName(request.getName());
        user.setEmail(request.getEmail());
        user.setPasswordHash(passwordEncoder.encode(request.getPassword()));
        user.setCreatedAt(LocalDateTime.now());
        
        return userRepository.save(user);
    }
    
    public User replace(Long id, UserUpdateRequest request) {
        User existing = findById(id);
        if (existing == null) {
            throw new UserNotFoundException("User not found");
        }
        
        // Replace entire record
        existing.setName(request.getName());
        existing.setEmail(request.getEmail());
        existing.setPasswordHash(passwordEncoder.encode(request.getPassword()));
        existing.setUpdatedAt(LocalDateTime.now());
        
        return userRepository.save(existing);
    }
    
    public User patch(Long id, Map<String, Object> updates) {
        User user = findById(id);
        if (user == null) {
            throw new UserNotFoundException("User not found");
        }
        
        // Apply partial updates
        if (updates.containsKey("name")) {
            user.setName((String) updates.get("name"));
        }
        if (updates.containsKey("email")) {
            user.setEmail((String) updates.get("email"));
        }
        user.setUpdatedAt(LocalDateTime.now());
        
        return userRepository.save(user);
    }
    
    public void delete(Long id) {
        User user = findById(id);
        if (user == null) {
            throw new UserNotFoundException("User not found");
        }
        
        // Soft delete
        user.setDeletedAt(LocalDateTime.now());
        user.setActive(false);
        userRepository.save(user);
    }
}
```

### Validation Service

```java
@Component
public class UserValidator {
    
    public ValidationResult validateCreate(UserCreateRequest request) {
        List<String> errors = new ArrayList<>();
        
        if (request.getName() == null || request.getName().trim().isEmpty()) {
            errors.add("Name is required");
        }
        
        if (request.getEmail() == null || !isValidEmail(request.getEmail())) {
            errors.add("Valid email is required");
        }
        
        if (request.getPassword() == null || request.getPassword().length() < 8) {
            errors.add("Password must be at least 8 characters");
        }
        
        return new ValidationResult(errors.isEmpty(), errors);
    }
    
    private boolean isValidEmail(String email) {
        return email.matches("^[A-Za-z0-9+_.-]+@(.+)$");
    }
}
```

## 🚀 Advanced Method Handling

### Method-Based Caching

```tusk
# Method-specific caching configuration
method_caching: {
    GET: {
        enabled: true
        ttl: "5m"
        cache_key: @lambda({
            return: "users:${@request.params.id ?? 'list'}:${@request.query.page ?? 1}"
        })
    },
    POST: {
        enabled: false
        invalidate_patterns: ["users:*"]
    },
    PUT: {
        enabled: false
        invalidate_patterns: ["users:${@request.params.id}"]
    },
    PATCH: {
        enabled: false
        invalidate_patterns: ["users:${@request.params.id}"]
    },
    DELETE: {
        enabled: false
        invalidate_patterns: ["users:*"]
    }
}
```

### Method-Based Security

```tusk
# Method-specific security configuration
method_security: {
    GET: {
        authentication: "optional"
        authorization: "read"
        rate_limit: "100/minute"
    },
    POST: {
        authentication: "required"
        authorization: "create"
        rate_limit: "10/minute"
        csrf_protection: true
    },
    PUT: {
        authentication: "required"
        authorization: "update"
        rate_limit: "20/minute"
        csrf_protection: true
    },
    PATCH: {
        authentication: "required"
        authorization: "update"
        rate_limit: "20/minute"
        csrf_protection: true
    },
    DELETE: {
        authentication: "required"
        authorization: "delete"
        rate_limit: "5/minute"
        csrf_protection: true
    }
}
```

### Method-Based Logging

```tusk
# Method-specific logging configuration
method_logging: {
    GET: {
        level: "info"
        log_request: false
        log_response: false
        log_timing: true
    },
    POST: {
        level: "info"
        log_request: true
        log_response: false
        log_timing: true
        mask_fields: ["password", "credit_card"]
    },
    PUT: {
        level: "info"
        log_request: true
        log_response: false
        log_timing: true
        mask_fields: ["password"]
    },
    PATCH: {
        level: "info"
        log_request: true
        log_response: false
        log_timing: true
        mask_fields: ["password"]
    },
    DELETE: {
        level: "warn"
        log_request: true
        log_response: false
        log_timing: true
    }
}
```

## 🔄 Method Chaining and Composition

### Method Handler Composition

```tusk
# Composable method handlers
method_handler_factory: {
    # Base handler with common functionality
    base_handler: @lambda({
        # Log request
        @log.info("${@request.method} ${@request.path}")
        
        # Check authentication
        auth_result = @java.invoke("SecurityService", "authenticate", @request)
        @if(!auth_result.authenticated, {
            return: { status: 401, body: { error: "Unauthorized" } }
        })
        
        # Check authorization
        authz_result = @java.invoke("SecurityService", "authorize", @request.method, @request.path, auth_result.user)
        @if(!authz_result.authorized, {
            return: { status: 403, body: { error: "Forbidden" } }
        })
        
        # Execute business logic
        return: @execute_business_logic()
    }),
    
    # Method-specific handlers
    get_handler: @lambda({
        # Add GET-specific logic
        @cache.get(@generate_cache_key())
        
        # Execute base handler
        return: @base_handler()
    }),
    
    post_handler: @lambda({
        # Add POST-specific logic
        @validate.request_body(@request.body)
        
        # Execute base handler
        return: @base_handler()
    }),
    
    put_handler: @lambda({
        # Add PUT-specific logic
        @validate.resource_exists(@request.params.id)
        
        # Execute base handler
        return: @base_handler()
    }),
    
    patch_handler: @lambda({
        # Add PATCH-specific logic
        @validate.partial_update(@request.body)
        
        # Execute base handler
        return: @base_handler()
    }),
    
    delete_handler: @lambda({
        # Add DELETE-specific logic
        @validate.resource_exists(@request.params.id)
        
        # Execute base handler
        return: @base_handler()
    })
}
```

## 🎯 Performance Optimization

### Method-Specific Performance Tuning

```tusk
# Performance configuration per method
method_performance: {
    GET: {
        # Optimize for read operations
        connection_pool: "read_pool"
        query_timeout: "30s"
        result_cache: true
        compression: true
    },
    POST: {
        # Optimize for write operations
        connection_pool: "write_pool"
        query_timeout: "60s"
        result_cache: false
        compression: false
        batch_size: 100
    },
    PUT: {
        # Optimize for update operations
        connection_pool: "write_pool"
        query_timeout: "45s"
        result_cache: false
        compression: false
        optimistic_locking: true
    },
    PATCH: {
        # Optimize for partial updates
        connection_pool: "write_pool"
        query_timeout: "30s"
        result_cache: false
        compression: false
        partial_update: true
    },
    DELETE: {
        # Optimize for delete operations
        connection_pool: "write_pool"
        query_timeout: "30s"
        result_cache: false
        compression: false
        soft_delete: true
    }
}
```

## 🔒 Security Considerations

### Method-Specific Security Patterns

```tusk
# Security configuration per method
method_security_patterns: {
    GET: {
        # Read operations - minimal security overhead
        input_validation: "basic"
        output_sanitization: "html_entities"
        sql_injection_protection: true
        xss_protection: true
    },
    POST: {
        # Create operations - strict validation
        input_validation: "strict"
        output_sanitization: "full"
        sql_injection_protection: true
        xss_protection: true
        csrf_protection: true
        content_type_validation: true
        file_upload_scanning: true
    },
    PUT: {
        # Update operations - strict validation
        input_validation: "strict"
        output_sanitization: "full"
        sql_injection_protection: true
        xss_protection: true
        csrf_protection: true
        ownership_validation: true
    },
    PATCH: {
        # Partial update operations - field-specific validation
        input_validation: "field_specific"
        output_sanitization: "full"
        sql_injection_protection: true
        xss_protection: true
        csrf_protection: true
        ownership_validation: true
    },
    DELETE: {
        # Delete operations - confirmation required
        input_validation: "basic"
        output_sanitization: "none"
        sql_injection_protection: true
        xss_protection: false
        csrf_protection: true
        ownership_validation: true
        confirmation_required: true
    }
}
```

## 🧪 Testing Method Handlers

### Unit Testing Configuration

```tusk
# Testing configuration for method handlers
method_testing: {
    GET: {
        test_cases: [
            {
                name: "get_user_by_id"
                request: { method: "GET", path: "/users/1" }
                expected: { status: 200, body_contains: "user" }
            },
            {
                name: "get_user_not_found"
                request: { method: "GET", path: "/users/999" }
                expected: { status: 404 }
            },
            {
                name: "get_users_list"
                request: { method: "GET", path: "/users" }
                expected: { status: 200, body_contains: "users" }
            }
        ]
    },
    POST: {
        test_cases: [
            {
                name: "create_user_valid"
                request: { 
                    method: "POST", 
                    path: "/users",
                    body: { name: "John Doe", email: "john@example.com", password: "password123" }
                }
                expected: { status: 201, body_contains: "user" }
            },
            {
                name: "create_user_invalid"
                request: { 
                    method: "POST", 
                    path: "/users",
                    body: { name: "", email: "invalid-email" }
                }
                expected: { status: 400, body_contains: "error" }
            }
        ]
    }
}
```

## 🚀 Best Practices

### Method Handler Best Practices

1. **Use Java Services**: Delegate business logic to Java services for better maintainability
2. **Implement Proper Validation**: Use Java validation services for input validation
3. **Handle Errors Gracefully**: Return appropriate HTTP status codes and error messages
4. **Use Caching Strategically**: Cache GET requests, invalidate on modifications
5. **Implement Security**: Use method-specific security configurations
6. **Log Appropriately**: Log different levels based on method type
7. **Optimize Performance**: Use method-specific performance configurations
8. **Test Thoroughly**: Create comprehensive test cases for each method

### Common Patterns

```tusk
# Common method handling patterns
common_patterns: {
    # CRUD operations
    crud_operations: {
        GET: "Read operations - fast, cacheable, safe"
        POST: "Create operations - validation, idempotency, location header"
        PUT: "Replace operations - full update, idempotent, optimistic locking"
        PATCH: "Partial update - field-specific, idempotent, validation"
        DELETE: "Remove operations - soft delete, confirmation, cascade"
    },
    
    # REST conventions
    rest_conventions: {
        GET: "Safe, idempotent, cacheable"
        POST: "Not safe, not idempotent, not cacheable"
        PUT: "Not safe, idempotent, not cacheable"
        PATCH: "Not safe, idempotent, not cacheable"
        DELETE: "Not safe, idempotent, not cacheable"
    },
    
    # Response patterns
    response_patterns: {
        GET: { status: 200, body: "resource or collection" }
        POST: { status: 201, headers: { "Location": "resource_url" } }
        PUT: { status: 200, body: "updated resource" }
        PATCH: { status: 200, body: "patched resource" }
        DELETE: { status: 204, body: "empty" }
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle HTTP methods with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're building RESTful APIs, microservices, or complex web applications, TuskLang provides the tools you need to handle HTTP methods efficiently and securely. 
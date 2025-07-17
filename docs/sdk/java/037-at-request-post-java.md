# @ Request POST - Java Edition

The `@request.post` object provides access to POST request data in Java applications. This guide covers handling form data, JSON payloads, file uploads, and implementing robust POST request processing using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 POST Request Overview

### Accessing POST Data in Java

```java
@RestController
@RequestMapping("/api")
public class UserController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @PostMapping("/users")
    public ResponseEntity<?> createUser(@RequestBody UserCreateRequest request) {
        // TuskLang handles POST data processing and validation
        return tuskConfig.getPostHandler().handleUserCreation(request);
    }
    
    @PostMapping("/upload")
    public ResponseEntity<?> uploadFile(@RequestParam("file") MultipartFile file) {
        // TuskLang handles file upload processing
        return tuskConfig.getPostHandler().handleFileUpload(file);
    }
}
```

```tusk
# app.tsk - POST request handling configuration
post_handlers: {
    user_creation: @lambda({
        # Extract and validate POST data
        user_data = {
            name: @request.post.name
            email: @request.post.email
            password: @request.post.password
            age: @int(@request.post.age ?? "0")
            active: @request.post.active == "true"
        }
        
        # Validate required fields
        validation_result = @java.invoke("UserValidator", "validateCreate", user_data)
        
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
        user = @java.invoke("UserService", "create", user_data)
        
        return: {
            status: 201
            body: { user: user }
            headers: { "Location": "/users/${user.id}" }
        }
    }),
    
    file_upload: @lambda({
        # Handle file upload
        file_data = {
            name: @request.post.file.name
            size: @int(@request.post.file.size)
            type: @request.post.file.type
            content: @request.post.file.content
        }
        
        # Validate file
        file_validation = @java.invoke("FileValidator", "validate", file_data)
        
        @if(!file_validation.valid, {
            return: {
                status: 400
                body: { error: file_validation.errors }
            }
        })
        
        # Process file via Java service
        result = @java.invoke("FileService", "upload", file_data)
        
        return: {
            status: 201
            body: { file: result }
        }
    })
}
```

## 📝 Form Data Handling

### Traditional Form Processing

```tusk
# Form data handling with Java integration
form_handlers: {
    # Handle traditional HTML forms
    contact_form: @lambda({
        # Extract form data
        form_data = {
            name: @request.post.name
            email: @request.post.email
            subject: @request.post.subject
            message: @request.post.message
            newsletter: @request.post.newsletter == "on"
        }
        
        # Validate form data
        validation = @java.invoke("ContactFormValidator", "validate", form_data)
        
        @if(!validation.valid, {
            return: {
                status: 400
                body: { 
                    error: "Form validation failed",
                    fields: validation.field_errors
                }
            }
        })
        
        # Process form via Java service
        result = @java.invoke("ContactService", "submit", form_data)
        
        return: {
            status: 200
            body: { 
                message: "Form submitted successfully",
                id: result.id
            }
        }
    }),
    
    # Handle multi-step forms
    multi_step_form: @lambda({
        step = @int(@request.post.step ?? "1")
        
        @switch(step, {
            1: {
                # Step 1: Basic information
                step_data = {
                    first_name: @request.post.first_name
                    last_name: @request.post.last_name
                    email: @request.post.email
                }
                
                validation = @java.invoke("Step1Validator", "validate", step_data)
                
                @if(!validation.valid, {
                    return: { status: 400, body: { error: validation.errors } }
                })
                
                # Store step data in session
                @session.set("step1_data", step_data)
                
                return: { 
                    status: 200, 
                    body: { 
                        message: "Step 1 completed",
                        next_step: 2
                    }
                }
            },
            2: {
                # Step 2: Address information
                step1_data = @session.get("step1_data")
                step2_data = {
                    street: @request.post.street
                    city: @request.post.city
                    state: @request.post.state
                    zip_code: @request.post.zip_code
                }
                
                validation = @java.invoke("Step2Validator", "validate", step2_data)
                
                @if(!validation.valid, {
                    return: { status: 400, body: { error: validation.errors } }
                })
                
                # Combine data and create user
                user_data = @merge(step1_data, step2_data)
                user = @java.invoke("UserService", "create", user_data)
                
                # Clear session data
                @session.remove("step1_data")
                
                return: { 
                    status: 201, 
                    body: { 
                        message: "Registration completed",
                        user: user
                    }
                }
            }
        })
    })
}
```

## 📄 JSON Payload Processing

### JSON Request Handling

```tusk
# JSON payload processing with Java integration
json_handlers: {
    # Handle JSON API requests
    api_user_create: @lambda({
        # Parse JSON payload
        json_data = @json.parse(@request.post.raw)
        
        # Validate JSON structure
        schema_validation = @java.invoke("JsonSchemaValidator", "validate", json_data, "user_create_schema")
        
        @if(!schema_validation.valid, {
            return: {
                status: 400
                body: { 
                    error: "Invalid JSON schema",
                    details: schema_validation.errors
                }
            }
        })
        
        # Transform JSON to Java object
        user_data = @java.invoke("DataTransformer", "jsonToUser", json_data)
        
        # Create user via Java service
        user = @java.invoke("UserService", "create", user_data)
        
        return: {
            status: 201
            body: { user: user }
            headers: { 
                "Location": "/api/users/${user.id}",
                "Content-Type": "application/json"
            }
        }
    }),
    
    # Handle batch operations
    batch_operations: @lambda({
        # Parse batch JSON
        batch_data = @json.parse(@request.post.raw)
        
        # Validate batch structure
        @if(!@isArray(batch_data.operations), {
            return: { status: 400, body: { error: "Invalid batch format" } }
        })
        
        # Process batch operations
        results = []
        errors = []
        
        @each(batch_data.operations, @lambda(operation, index, {
            try {
                result = @java.invoke("BatchProcessor", "process", operation)
                @push(results, { index: index, success: true, data: result })
            }, {
                @push(errors, { index: index, error: operation.error })
            }
        }))
        
        return: {
            status: @len(errors) > 0 ? 207 : 200
            body: {
                results: results
                errors: errors
                total: @len(batch_data.operations)
                successful: @len(results)
                failed: @len(errors)
            }
        }
    })
}
```

## 📁 File Upload Processing

### File Upload Handling

```tusk
# File upload processing with Java integration
file_handlers: {
    # Handle single file upload
    single_file_upload: @lambda({
        # Extract file data
        file_info = {
            name: @request.post.file.name
            size: @int(@request.post.file.size)
            type: @request.post.file.type
            content: @request.post.file.content
            checksum: @request.post.file.checksum
        }
        
        # Validate file
        file_validation = @java.invoke("FileValidator", "validateUpload", file_info)
        
        @if(!file_validation.valid, {
            return: {
                status: 400
                body: { error: file_validation.errors }
            }
        })
        
        # Process file via Java service
        upload_result = @java.invoke("FileService", "upload", file_info)
        
        return: {
            status: 201
            body: {
                file: upload_result
                url: upload_result.url
                size: upload_result.size
            }
        }
    }),
    
    # Handle multiple file uploads
    multiple_file_upload: @lambda({
        # Extract multiple files
        files = @request.post.files
        
        # Validate all files
        validation_results = []
        @each(files, @lambda(file, {
            validation = @java.invoke("FileValidator", "validateUpload", file)
            @push(validation_results, validation)
        }))
        
        # Check for validation errors
        errors = @filter(validation_results, @lambda(v, !v.valid))
        @if(@len(errors) > 0, {
            return: {
                status: 400
                body: { errors: errors }
            }
        })
        
        # Upload all files
        upload_results = []
        @each(files, @lambda(file, {
            result = @java.invoke("FileService", "upload", file)
            @push(upload_results, result)
        }))
        
        return: {
            status: 201
            body: {
                files: upload_results
                total_files: @len(upload_results)
                total_size: @sum(@map(upload_results, @lambda(r, r.size)))
            }
        }
    }),
    
    # Handle chunked file uploads
    chunked_upload: @lambda({
        # Extract chunk data
        chunk_data = {
            file_id: @request.post.file_id
            chunk_number: @int(@request.post.chunk_number)
            total_chunks: @int(@request.post.total_chunks)
            chunk_size: @int(@request.post.chunk_size)
            chunk_data: @request.post.chunk_data
            checksum: @request.post.checksum
        }
        
        # Process chunk via Java service
        chunk_result = @java.invoke("ChunkedUploadService", "processChunk", chunk_data)
        
        @if(chunk_result.complete, {
            # All chunks received, assemble file
            final_file = @java.invoke("ChunkedUploadService", "assembleFile", chunk_data.file_id)
            
            return: {
                status: 201
                body: {
                    message: "File upload completed",
                    file: final_file
                }
            }
        }, {
            return: {
                status: 200
                body: {
                    message: "Chunk received",
                    chunks_received: chunk_result.chunks_received,
                    total_chunks: chunk_data.total_chunks
                }
            }
        })
    })
}
```

## 🔧 Java Service Integration

### POST Processing Service

```java
@Service
@Transactional
public class PostProcessingService {
    
    @Autowired
    private UserService userService;
    
    @Autowired
    private FileService fileService;
    
    @Autowired
    private ValidationService validationService;
    
    public ResponseEntity<?> handleUserCreation(UserCreateRequest request) {
        // Validate request
        ValidationResult validation = validationService.validateUserCreation(request);
        if (!validation.isValid()) {
            return ResponseEntity.badRequest()
                .body(Map.of("error", "Validation failed", "details", validation.getErrors()));
        }
        
        // Create user
        User user = userService.create(request);
        
        return ResponseEntity.status(201)
            .header("Location", "/api/users/" + user.getId())
            .body(Map.of("user", user));
    }
    
    public ResponseEntity<?> handleFileUpload(MultipartFile file) {
        // Validate file
        FileValidationResult validation = validationService.validateFile(file);
        if (!validation.isValid()) {
            return ResponseEntity.badRequest()
                .body(Map.of("error", validation.getErrors()));
        }
        
        // Upload file
        FileUploadResult result = fileService.upload(file);
        
        return ResponseEntity.status(201)
            .body(Map.of("file", result));
    }
    
    public ResponseEntity<?> handleFormSubmission(Map<String, String> formData) {
        // Validate form data
        ValidationResult validation = validationService.validateForm(formData);
        if (!validation.isValid()) {
            return ResponseEntity.badRequest()
                .body(Map.of("error", "Form validation failed", "fields", validation.getFieldErrors()));
        }
        
        // Process form
        FormSubmissionResult result = processForm(formData);
        
        return ResponseEntity.ok()
            .body(Map.of("message", "Form submitted successfully", "id", result.getId()));
    }
    
    private FormSubmissionResult processForm(Map<String, String> formData) {
        // Process form data based on type
        String formType = formData.get("form_type");
        
        switch (formType) {
            case "contact":
                return processContactForm(formData);
            case "registration":
                return processRegistrationForm(formData);
            case "feedback":
                return processFeedbackForm(formData);
            default:
                throw new IllegalArgumentException("Unknown form type: " + formType);
        }
    }
}
```

### Validation Service

```java
@Service
public class ValidationService {
    
    public ValidationResult validateUserCreation(UserCreateRequest request) {
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
    
    public FileValidationResult validateFile(MultipartFile file) {
        List<String> errors = new ArrayList<>();
        
        if (file.isEmpty()) {
            errors.add("File is empty");
        }
        
        if (file.getSize() > 10 * 1024 * 1024) { // 10MB limit
            errors.add("File size exceeds 10MB limit");
        }
        
        String contentType = file.getContentType();
        if (contentType == null || !isAllowedContentType(contentType)) {
            errors.add("File type not allowed");
        }
        
        return new FileValidationResult(errors.isEmpty(), errors);
    }
    
    public ValidationResult validateForm(Map<String, String> formData) {
        List<String> errors = new ArrayList<>();
        Map<String, String> fieldErrors = new HashMap<>();
        
        // Validate required fields
        String[] requiredFields = {"name", "email"};
        for (String field : requiredFields) {
            if (!formData.containsKey(field) || formData.get(field).trim().isEmpty()) {
                fieldErrors.put(field, "This field is required");
                errors.add(field + " is required");
            }
        }
        
        // Validate email format
        if (formData.containsKey("email") && !isValidEmail(formData.get("email"))) {
            fieldErrors.put("email", "Invalid email format");
            errors.add("Invalid email format");
        }
        
        return new ValidationResult(errors.isEmpty(), errors, fieldErrors);
    }
    
    private boolean isValidEmail(String email) {
        return email.matches("^[A-Za-z0-9+_.-]+@(.+)$");
    }
    
    private boolean isAllowedContentType(String contentType) {
        return Arrays.asList("image/jpeg", "image/png", "image/gif", "application/pdf")
            .contains(contentType);
    }
}
```

## 🚀 Advanced POST Features

### POST Request Caching

```tusk
# POST request caching with Java integration
post_caching: {
    # Cache POST responses for idempotent operations
    cache_post_response: @lambda({
        # Generate cache key based on POST data
        cache_key = @java.invoke("CacheKeyGenerator", "generateForPost", @request.post)
        
        # Check cache for idempotent operations
        cached_response = @cache.get(cache_key)
        @if(cached_response, return: cached_response)
        
        # Process POST request
        response = @process_post_request()
        
        # Cache response for idempotent operations
        @if(@is_idempotent_operation(@request.post), {
            @cache.set(cache_key, response, "1h")
        })
        
        return: response
    }),
    
    # Check if operation is idempotent
    is_idempotent_operation: @lambda(post_data, {
        # Operations that can be safely cached
        idempotent_operations = [
            "user_lookup"
            "data_validation"
            "calculation"
            "search"
        ]
        
        operation_type = post_data.operation_type
        return: @includes(idempotent_operations, operation_type)
    })
}
```

### POST Request Analytics

```tusk
# POST request analytics with Java integration
post_analytics: {
    # Track POST request metrics
    track_post_metrics: @lambda({
        # Extract metrics
        metrics = {
            endpoint: @request.path
            method: @request.method
            content_type: @request.headers["Content-Type"]
            content_length: @int(@request.headers["Content-Length"] ?? "0")
            user_agent: @request.headers["User-Agent"]
            ip_address: @request.ip
            timestamp: @time.now()
            processing_time: @request.processing_time
        }
        
        # Send metrics to Java analytics service
        @java.invoke("AnalyticsService", "trackPostRequest", metrics)
    }),
    
    # Analyze POST patterns
    analyze_post_patterns: @lambda({
        # Get POST analysis from Java service
        analysis = @java.invoke("PostAnalyticsService", "analyze", @request.post)
        
        # Log insights
        @if(analysis.insights, {
            @log.info("POST insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

## 🔒 Security Considerations

### POST Security Patterns

```tusk
# POST security configuration
post_security: {
    # Input validation
    input_validation: {
        max_post_size: "10MB"
        allowed_content_types: ["application/json", "application/x-www-form-urlencoded", "multipart/form-data"]
        block_sql_injection: true
        block_xss: true
        validate_json_schema: true
    },
    
    # Rate limiting
    rate_limiting: {
        enabled: true
        max_requests_per_minute: 60
        max_requests_per_hour: 1000
        block_on_exceed: true
    },
    
    # CSRF protection
    csrf_protection: {
        enabled: true
        token_validation: true
        token_refresh: true
    },
    
    # File upload security
    file_upload_security: {
        max_file_size: "10MB"
        allowed_extensions: [".jpg", ".png", ".gif", ".pdf"]
        scan_for_viruses: true
        validate_file_content: true
    }
}
```

## 🧪 Testing POST Handlers

### POST Testing Configuration

```tusk
# POST testing configuration
post_testing: {
    # Test cases for user creation
    user_creation_test_cases: [
        {
            name: "valid_user_creation"
            post_data: {
                name: "John Doe"
                email: "john@example.com"
                password: "password123"
                age: "30"
            }
            expected: { status: 201, has_user: true }
        },
        {
            name: "invalid_user_creation"
            post_data: {
                name: ""
                email: "invalid-email"
                password: "123"
            }
            expected: { status: 400, has_errors: true }
        },
        {
            name: "missing_required_fields"
            post_data: {
                name: "John Doe"
                # Missing email and password
            }
            expected: { status: 400, has_errors: true }
        }
    ],
    
    # Test cases for file upload
    file_upload_test_cases: [
        {
            name: "valid_file_upload"
            post_data: {
                file: {
                    name: "test.jpg"
                    size: "1024"
                    type: "image/jpeg"
                    content: "base64_encoded_content"
                }
            }
            expected: { status: 201, has_file: true }
        },
        {
            name: "invalid_file_type"
            post_data: {
                file: {
                    name: "test.exe"
                    size: "1024"
                    type: "application/x-executable"
                    content: "base64_encoded_content"
                }
            }
            expected: { status: 400, has_errors: true }
        }
    ]
}
```

## 🚀 Best Practices

### POST Handling Best Practices

1. **Use Java Services**: Delegate POST processing to Java services for better maintainability
2. **Implement Proper Validation**: Use Java validation services for input validation
3. **Handle File Uploads Securely**: Validate file types, sizes, and content
4. **Implement CSRF Protection**: Protect against cross-site request forgery
5. **Use Rate Limiting**: Prevent abuse with rate limiting
6. **Log POST Analytics**: Track POST patterns for optimization
7. **Handle Errors Gracefully**: Return appropriate HTTP status codes and error messages
8. **Validate JSON Schema**: Use schema validation for JSON payloads

### Common Patterns

```tusk
# Common POST handling patterns
common_patterns: {
    # Form processing patterns
    form_patterns: {
        contact_form: "Simple contact form with email validation"
        registration_form: "Multi-step registration with validation"
        feedback_form: "Feedback collection with rating system"
        survey_form: "Dynamic survey with conditional questions"
    },
    
    # File upload patterns
    file_patterns: {
        single_upload: "Single file upload with validation"
        multiple_upload: "Multiple file upload with progress"
        chunked_upload: "Large file upload in chunks"
        drag_drop_upload: "Drag and drop file upload"
    },
    
    # API patterns
    api_patterns: {
        rest_create: "RESTful resource creation"
        batch_operations: "Batch processing of multiple items"
        data_import: "Data import from various formats"
        webhook_receiver: "Webhook endpoint for external services"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle POST requests with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're processing forms, handling file uploads, or building RESTful APIs, TuskLang provides the tools you need to handle POST data efficiently and securely. 
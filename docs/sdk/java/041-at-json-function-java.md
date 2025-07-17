# @ JSON Function - Java Edition

The `@json` function provides JSON parsing and serialization capabilities in Java applications. This guide covers JSON processing, data transformation, API responses, and implementing robust JSON-based logic using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 JSON Function Overview

### JSON Processing in Java

```java
@RestController
@RequestMapping("/api")
public class JsonController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @PostMapping("/process-json")
    public ResponseEntity<?> processJson(@RequestBody String jsonData) {
        // TuskLang handles JSON processing and transformation
        return tuskConfig.getJsonHandler().processJsonData(jsonData);
    }
    
    @GetMapping("/generate-json")
    public ResponseEntity<?> generateJson() {
        // TuskLang handles JSON generation and serialization
        return tuskConfig.getJsonHandler().generateJsonResponse();
    }
    
    @PostMapping("/validate-json")
    public ResponseEntity<?> validateJson(@RequestBody String jsonData) {
        // TuskLang handles JSON validation and schema checking
        return tuskConfig.getJsonHandler().validateJsonData(jsonData);
    }
}
```

```tusk
# app.tsk - JSON handling configuration
json_handlers: {
    process_json_data: @lambda({
        # Parse JSON data
        json_data = @json.parse(@request.post.raw)
        
        # Validate JSON structure via Java service
        validation = @java.invoke("JsonValidationService", "validateStructure", json_data)
        
        @if(!validation.valid, {
            return: {
                status: 400
                body: { 
                    error: "Invalid JSON structure",
                    details: validation.errors
                }
            }
        })
        
        # Transform JSON data via Java service
        transformed_data = @java.invoke("JsonTransformationService", "transform", json_data)
        
        # Generate response JSON
        response_json = @json.stringify({
            success: true
            original_data: json_data
            transformed_data: transformed_data
            timestamp: @time.now()
        })
        
        return: {
            status: 200
            body: response_json
            headers: { "Content-Type": "application/json" }
        }
    }),
    
    generate_json_response: @lambda({
        # Generate complex JSON structure
        user_data = @java.invoke("UserService", "getCurrentUser")
        
        # Build JSON response
        response_data = {
            user: {
                id: user_data.id
                name: user_data.name
                email: user_data.email
                profile: {
                    avatar: user_data.avatar
                    bio: user_data.bio
                    preferences: user_data.preferences
                }
                settings: {
                    theme: user_data.theme
                    language: user_data.language
                    notifications: user_data.notifications
                }
            }
            metadata: {
                generated_at: @time.now()
                version: "1.0.0"
                api_version: "v2"
            }
        }
        
        # Serialize to JSON
        json_response = @json.stringify(response_data, {
            pretty: true
            indent: 2
        })
        
        return: {
            status: 200
            body: json_response
            headers: { "Content-Type": "application/json" }
        }
    }),
    
    validate_json_data: @lambda({
        # Parse and validate JSON
        json_data = @json.parse(@request.post.raw)
        
        # Validate against schema via Java service
        schema_validation = @java.invoke("JsonSchemaService", "validate", json_data, "user_schema")
        
        @if(!schema_validation.valid, {
            return: {
                status: 400
                body: {
                    error: "JSON schema validation failed",
                    violations: schema_validation.violations
                }
            }
        })
        
        # Additional business logic validation
        business_validation = @java.invoke("BusinessValidationService", "validate", json_data)
        
        @if(!business_validation.valid, {
            return: {
                status: 400
                body: {
                    error: "Business validation failed",
                    errors: business_validation.errors
                }
            }
        })
        
        return: {
            status: 200
            body: {
                message: "JSON validation successful",
                data: json_data
            }
        }
    })
}
```

## 🔄 JSON Parsing and Serialization

### Basic JSON Operations

```tusk
# Basic JSON operations with Java integration
json_operations: {
    # Parse JSON string
    parse_json: @lambda(json_string, {
        # Parse JSON with error handling
        try {
            parsed_data = @json.parse(json_string)
            return: { success: true, data: parsed_data }
        }, {
            return: { success: false, error: "Invalid JSON format" }
        }
    }),
    
    # Stringify object to JSON
    stringify_json: @lambda(data, options = {}, {
        # Default options
        default_options = {
            pretty: false
            indent: 2
            escape_unicode: true
            sort_keys: false
        }
        
        # Merge with provided options
        json_options = @merge(default_options, options)
        
        # Convert to JSON
        json_string = @json.stringify(data, json_options)
        
        return: json_string
    }),
    
    # Pretty print JSON
    pretty_print_json: @lambda(data, {
        return: @json.stringify(data, {
            pretty: true
            indent: 2
            escape_unicode: true
            sort_keys: true
        })
    }),
    
    # Minify JSON
    minify_json: @lambda(data, {
        return: @json.stringify(data, {
            pretty: false
            escape_unicode: true
            sort_keys: false
        })
    })
}
```

### Advanced JSON Processing

```tusk
# Advanced JSON processing with Java integration
advanced_json_processing: {
    # Deep merge JSON objects
    deep_merge_json: @lambda(obj1, obj2, {
        # Use Java service for deep merging
        merged_result = @java.invoke("JsonMergeService", "deepMerge", obj1, obj2)
        
        return: merged_result
    }),
    
    # Flatten JSON object
    flatten_json: @lambda(obj, prefix = "", {
        # Use Java service for flattening
        flattened_result = @java.invoke("JsonFlattenService", "flatten", obj, prefix)
        
        return: flattened_result
    }),
    
    # Unflatten JSON object
    unflatten_json: @lambda(flattened_obj, {
        # Use Java service for unflattening
        unflattened_result = @java.invoke("JsonFlattenService", "unflatten", flattened_obj)
        
        return: unflattened_result
    }),
    
    # Transform JSON structure
    transform_json: @lambda(data, transformation_rules, {
        # Use Java service for transformation
        transformed_result = @java.invoke("JsonTransformationService", "transform", data, transformation_rules)
        
        return: transformed_result
    }),
    
    # Filter JSON data
    filter_json: @lambda(data, filter_condition, {
        # Use Java service for filtering
        filtered_result = @java.invoke("JsonFilterService", "filter", data, filter_condition)
        
        return: filtered_result
    })
}
```

## 📊 JSON Data Transformation

### API Response Transformation

```tusk
# API response transformation with Java integration
api_response_transformation: {
    # Transform database result to API response
    transform_db_to_api: @lambda(db_result, {
        # Transform via Java service
        api_response = @java.invoke("ApiTransformationService", "transformDatabaseResult", db_result)
        
        # Add metadata
        response_with_metadata = {
            data: api_response
            metadata: {
                timestamp: @time.now()
                count: @len(api_response)
                pagination: {
                    page: @request.query.page ?? 1
                    limit: @request.query.limit ?? 20
                    total: db_result.total_count
                }
            }
        }
        
        return: @json.stringify(response_with_metadata, { pretty: true })
    }),
    
    # Transform user data for different contexts
    transform_user_data: @lambda(user_data, context, {
        # Transform based on context via Java service
        transformed_data = @java.invoke("UserTransformationService", "transformForContext", user_data, context)
        
        return: @json.stringify(transformed_data, { pretty: true })
    }),
    
    # Transform error response
    transform_error_response: @lambda(error_data, {
        # Standardize error format
        standardized_error = {
            error: {
                code: error_data.code ?? "UNKNOWN_ERROR"
                message: error_data.message ?? "An unknown error occurred"
                details: error_data.details ?? {}
                timestamp: @time.now()
                request_id: @request.headers["X-Request-ID"] ?? @generate_request_id()
            }
        }
        
        return: @json.stringify(standardized_error, { pretty: true })
    })
}
```

### Data Validation and Sanitization

```tusk
# JSON data validation and sanitization with Java integration
json_validation_sanitization: {
    # Validate JSON against schema
    validate_json_schema: @lambda(json_data, schema_name, {
        # Validate via Java service
        validation_result = @java.invoke("JsonSchemaService", "validate", json_data, schema_name)
        
        return: {
            valid: validation_result.valid
            errors: validation_result.errors
            warnings: validation_result.warnings
        }
    }),
    
    # Sanitize JSON data
    sanitize_json_data: @lambda(json_data, {
        # Sanitize via Java service
        sanitized_data = @java.invoke("JsonSanitizationService", "sanitize", json_data)
        
        return: sanitized_data
    }),
    
    # Validate and sanitize JSON
    validate_and_sanitize: @lambda(json_data, schema_name, {
        # First validate
        validation = @validate_json_schema(json_data, schema_name)
        
        @if(!validation.valid, {
            return: { success: false, errors: validation.errors }
        })
        
        # Then sanitize
        sanitized = @sanitize_json_data(json_data)
        
        return: { success: true, data: sanitized }
    }),
    
    # Transform and validate JSON
    transform_and_validate: @lambda(json_data, transformation_rules, schema_name, {
        # Transform first
        transformed = @transform_json(json_data, transformation_rules)
        
        # Then validate
        validation = @validate_json_schema(transformed, schema_name)
        
        @if(!validation.valid, {
            return: { success: false, errors: validation.errors }
        })
        
        return: { success: true, data: transformed }
    })
}
```

## 🔧 Java Service Integration

### JSON Processing Service

```java
@Service
public class JsonProcessingService {
    
    @Autowired
    private ObjectMapper objectMapper;
    
    @Autowired
    private JsonValidationService jsonValidationService;
    
    @Autowired
    private JsonTransformationService jsonTransformationService;
    
    public ResponseEntity<?> processJsonData(String jsonData) {
        try {
            // Parse JSON
            Object parsedData = objectMapper.readValue(jsonData, Object.class);
            
            // Validate structure
            ValidationResult validation = jsonValidationService.validateStructure(parsedData);
            if (!validation.isValid()) {
                return ResponseEntity.badRequest()
                    .body(Map.of("error", "Invalid JSON structure", "details", validation.getErrors()));
            }
            
            // Transform data
            Object transformedData = jsonTransformationService.transform(parsedData);
            
            // Generate response
            Map<String, Object> response = Map.of(
                "success", true,
                "original_data", parsedData,
                "transformed_data", transformedData,
                "timestamp", LocalDateTime.now()
            );
            
            String responseJson = objectMapper.writeValueAsString(response);
            
            return ResponseEntity.ok()
                .header("Content-Type", "application/json")
                .body(responseJson);
                
        } catch (Exception e) {
            return ResponseEntity.badRequest()
                .body(Map.of("error", "JSON processing failed", "details", e.getMessage()));
        }
    }
    
    public ResponseEntity<?> generateJsonResponse() {
        try {
            // Get user data
            User user = userService.getCurrentUser();
            
            // Build response structure
            Map<String, Object> response = buildUserResponse(user);
            
            // Serialize with pretty printing
            String jsonResponse = objectMapper.writerWithDefaultPrettyPrinter()
                .writeValueAsString(response);
            
            return ResponseEntity.ok()
                .header("Content-Type", "application/json")
                .body(jsonResponse);
                
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Failed to generate JSON response", "details", e.getMessage()));
        }
    }
    
    public ResponseEntity<?> validateJsonData(String jsonData) {
        try {
            // Parse JSON
            Object parsedData = objectMapper.readValue(jsonData, Object.class);
            
            // Validate against schema
            SchemaValidationResult schemaValidation = jsonValidationService.validateSchema(parsedData, "user_schema");
            if (!schemaValidation.isValid()) {
                return ResponseEntity.badRequest()
                    .body(Map.of("error", "JSON schema validation failed", "violations", schemaValidation.getViolations()));
            }
            
            // Business validation
            BusinessValidationResult businessValidation = businessValidationService.validate(parsedData);
            if (!businessValidation.isValid()) {
                return ResponseEntity.badRequest()
                    .body(Map.of("error", "Business validation failed", "errors", businessValidation.getErrors()));
            }
            
            return ResponseEntity.ok(Map.of(
                "message", "JSON validation successful",
                "data", parsedData
            ));
            
        } catch (Exception e) {
            return ResponseEntity.badRequest()
                .body(Map.of("error", "JSON validation failed", "details", e.getMessage()));
        }
    }
    
    private Map<String, Object> buildUserResponse(User user) {
        Map<String, Object> response = new HashMap<>();
        
        // User data
        Map<String, Object> userData = new HashMap<>();
        userData.put("id", user.getId());
        userData.put("name", user.getName());
        userData.put("email", user.getEmail());
        
        // Profile data
        Map<String, Object> profile = new HashMap<>();
        profile.put("avatar", user.getAvatar());
        profile.put("bio", user.getBio());
        profile.put("preferences", user.getPreferences());
        userData.put("profile", profile);
        
        // Settings data
        Map<String, Object> settings = new HashMap<>();
        settings.put("theme", user.getTheme());
        settings.put("language", user.getLanguage());
        settings.put("notifications", user.getNotifications());
        userData.put("settings", settings);
        
        response.put("user", userData);
        
        // Metadata
        Map<String, Object> metadata = new HashMap<>();
        metadata.put("generated_at", LocalDateTime.now());
        metadata.put("version", "1.0.0");
        metadata.put("api_version", "v2");
        response.put("metadata", metadata);
        
        return response;
    }
}
```

### JSON Transformation Service

```java
@Service
public class JsonTransformationService {
    
    @Autowired
    private ObjectMapper objectMapper;
    
    public Object transform(Object data) {
        // Apply transformation rules
        return applyTransformationRules(data);
    }
    
    public Object transform(Object data, Map<String, Object> transformationRules) {
        // Apply specific transformation rules
        return applyCustomTransformationRules(data, transformationRules);
    }
    
    public Object deepMerge(Object obj1, Object obj2) {
        try {
            // Convert to JsonNode for deep merging
            JsonNode node1 = objectMapper.valueToTree(obj1);
            JsonNode node2 = objectMapper.valueToTree(obj2);
            
            // Merge nodes
            JsonNode merged = mergeJsonNodes(node1, node2);
            
            // Convert back to object
            return objectMapper.treeToValue(merged, Object.class);
            
        } catch (Exception e) {
            throw new RuntimeException("Failed to merge JSON objects", e);
        }
    }
    
    public Map<String, Object> flatten(Object obj, String prefix) {
        Map<String, Object> flattened = new HashMap<>();
        flattenObject(obj, prefix, flattened);
        return flattened;
    }
    
    public Object unflatten(Map<String, Object> flattenedObj) {
        Map<String, Object> unflattened = new HashMap<>();
        
        for (Map.Entry<String, Object> entry : flattenedObj.entrySet()) {
            String key = entry.getKey();
            Object value = entry.getValue();
            
            String[] parts = key.split("\\.");
            Map<String, Object> current = unflattened;
            
            for (int i = 0; i < parts.length - 1; i++) {
                String part = parts[i];
                current = (Map<String, Object>) current.computeIfAbsent(part, k -> new HashMap<>());
            }
            
            current.put(parts[parts.length - 1], value);
        }
        
        return unflattened;
    }
    
    private Object applyTransformationRules(Object data) {
        // Apply default transformation rules
        // Implementation details...
        return data;
    }
    
    private Object applyCustomTransformationRules(Object data, Map<String, Object> rules) {
        // Apply custom transformation rules
        // Implementation details...
        return data;
    }
    
    private JsonNode mergeJsonNodes(JsonNode node1, JsonNode node2) {
        // Deep merge implementation
        // Implementation details...
        return node1;
    }
    
    private void flattenObject(Object obj, String prefix, Map<String, Object> flattened) {
        // Flatten object implementation
        // Implementation details...
    }
}
```

## 🚀 Advanced JSON Features

### JSON Caching

```tusk
# JSON caching with Java integration
json_caching: {
    # Cache JSON data
    cache_json_data: @lambda(key, data, ttl = "1h", {
        # Serialize data to JSON
        json_data = @json.stringify(data)
        
        # Cache JSON string
        @cache.set(key, json_data, ttl)
        
        return: { success: true, cached_key: key }
    }),
    
    # Retrieve cached JSON
    get_cached_json: @lambda(key, {
        # Get cached JSON string
        cached_json = @cache.get(key)
        
        @if(!cached_json, {
            return: { success: false, error: "No cached data found" }
        })
        
        # Parse JSON
        parsed_data = @json.parse(cached_json)
        
        return: { success: true, data: parsed_data }
    }),
    
    # Cache with compression
    cache_compressed_json: @lambda(key, data, ttl = "1h", {
        # Compress and serialize data
        compressed_json = @java.invoke("JsonCompressionService", "compress", data)
        
        # Cache compressed data
        @cache.set(key, compressed_json, ttl)
        
        return: { success: true, cached_key: key, compressed: true }
    }),
    
    # Retrieve compressed JSON
    get_compressed_json: @lambda(key, {
        # Get cached compressed data
        cached_data = @cache.get(key)
        
        @if(!cached_data, {
            return: { success: false, error: "No cached data found" }
        }
        
        # Decompress and parse
        decompressed_data = @java.invoke("JsonCompressionService", "decompress", cached_data)
        parsed_data = @json.parse(decompressed_data)
        
        return: { success: true, data: parsed_data }
    })
}
```

### JSON Analytics

```tusk
# JSON analytics with Java integration
json_analytics: {
    # Track JSON processing metrics
    track_json_metrics: @lambda({
        # Extract JSON metrics
        json_metrics = {
            processing_time: @request.processing_time
            json_size: @len(@request.post.raw)
            operation_type: "json_processing"
            timestamp: @time.now()
            user_id: @session.get("user_id")
        }
        
        # Send metrics to Java analytics service
        @java.invoke("JsonAnalyticsService", "track", json_metrics)
    }),
    
    # Analyze JSON patterns
    analyze_json_patterns: @lambda(json_data, {
        # Get JSON analysis from Java service
        analysis = @java.invoke("JsonAnalysisService", "analyze", json_data)
        
        # Log insights
        @if(analysis.insights, {
            @log.info("JSON insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

## 🔒 Security Considerations

### JSON Security Patterns

```tusk
# JSON security configuration
json_security_patterns: {
    # JSON validation
    json_validation: {
        max_json_size: "10MB"
        max_nesting_depth: 10
        allowed_data_types: ["string", "number", "boolean", "array", "object"]
        block_circular_references: true
        validate_schema: true
    },
    
    # JSON sanitization
    json_sanitization: {
        remove_script_tags: true
        escape_html_entities: true
        validate_urls: true
        sanitize_sql_injection: true
        block_xss_vectors: true
    },
    
    # JSON logging
    json_logging: {
        enabled: true
        log_level: "info"
        log_json_processing: true
        log_json_errors: true
        mask_sensitive_data: true
    }
}
```

## 🧪 Testing JSON Handlers

### JSON Testing Configuration

```tusk
# JSON testing configuration
json_testing: {
    # Test cases for JSON parsing
    json_parsing_test_cases: [
        {
            name: "valid_json_parsing"
            input: '{"name": "John", "age": 30}'
            expected: { success: true, has_name: true, has_age: true }
        },
        {
            name: "invalid_json_parsing"
            input: '{"name": "John", "age": 30,}'
            expected: { success: false, has_error: true }
        },
        {
            name: "complex_json_parsing"
            input: '{"user": {"name": "John", "profile": {"avatar": "url"}}}'
            expected: { success: true, has_nested_data: true }
        }
    ],
    
    # Test cases for JSON serialization
    json_serialization_test_cases: [
        {
            name: "basic_serialization"
            input: { name: "John", age: 30 }
            expected: { contains_name: true, contains_age: true }
        },
        {
            name: "pretty_serialization"
            input: { name: "John", age: 30 }
            options: { pretty: true, indent: 2 }
            expected: { is_pretty: true, has_indentation: true }
        }
    ]
}
```

## 🚀 Best Practices

### JSON Handling Best Practices

1. **Use Java Services**: Delegate JSON processing to Java services for better maintainability
2. **Implement Proper Validation**: Use JSON schema validation for data integrity
3. **Handle Errors Gracefully**: Provide meaningful error messages for JSON processing failures
4. **Use Pretty Printing**: Use pretty printing for debugging and readability
5. **Implement Caching**: Cache frequently processed JSON data for performance
6. **Sanitize JSON Data**: Always sanitize JSON data to prevent security vulnerabilities
7. **Handle Large JSON**: Implement streaming for large JSON files
8. **Use Compression**: Compress JSON data for storage and transmission

### Common Patterns

```tusk
# Common JSON handling patterns
common_patterns: {
    # API patterns
    api_patterns: {
        rest_api_response: "RESTful API response formatting"
        error_response: "Standardized error response format"
        pagination_response: "Paginated data response format"
        batch_response: "Batch operation response format"
    },
    
    # Data transformation patterns
    transformation_patterns: {
        database_to_api: "Database result to API response transformation"
        api_to_database: "API request to database format transformation"
        legacy_to_modern: "Legacy format to modern format transformation"
        internal_to_external: "Internal data to external API format transformation"
    },
    
    # Validation patterns
    validation_patterns: {
        schema_validation: "JSON schema validation and enforcement"
        business_validation: "Business logic validation of JSON data"
        security_validation: "Security-focused JSON validation"
        format_validation: "JSON format and structure validation"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle JSON processing with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're building APIs, transforming data, or implementing data validation, TuskLang provides the tools you need to handle JSON efficiently and securely. 
# @ Request Query - Java Edition

The `@request.query` object provides access to URL query parameters in Java applications. This guide covers parsing, validating, and working with query strings using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 Basic Query Access in Java

### Simple Parameters with Spring Boot

```java
@RestController
@RequestMapping("/api")
public class SearchController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @GetMapping("/search")
    public ResponseEntity<?> search(
            @RequestParam(required = false) String q,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "20") int limit,
            @RequestParam(required = false) String sort,
            @RequestParam(required = false) String order) {
        
        // TuskLang handles query parameter processing
        return tuskConfig.getQueryHandler().handleSearch(q, page, limit, sort, order);
    }
}
```

```tusk
# app.tsk - Query parameter handling configuration
query_handlers: {
    search: @lambda({
        # Extract and validate query parameters
        search_params = {
            query: @request.query.q ?? ""
            page: @int(@request.query.page ?? "1")
            limit: @int(@request.query.limit ?? "20")
            sort: @request.query.sort ?? "relevance"
            order: @request.query.order ?? "desc"
        }
        
        # Validate ranges
        validated_params = {
            page: @max(1, search_params.page)
            limit: @clamp(search_params.limit, 1, 100)
            query: @trim(search_params.query)
            sort: @validate_sort_field(search_params.sort)
            order: @validate_order_direction(search_params.order)
        }
        
        # Execute search via Java service
        result = @java.invoke("SearchService", "search", validated_params)
        
        return: {
            status: 200
            body: result
        }
    })
}
```

### Type Conversion with Java Integration

```tusk
# Type conversion utilities with Java validation
type_conversion: {
    # Safe integer conversion with Java validation
    safe_int: @lambda(value, default_value, {
        @if(!value, return: default_value)
        
        # Use Java for robust parsing
        result = @java.invoke("TypeConverter", "parseInt", value)
        return: result.success ? result.value : default_value
    }),
    
    # Safe boolean conversion
    safe_boolean: @lambda(value, default_value = false, {
        @if(!value, return: default_value)
        
        # Use Java for boolean parsing
        result = @java.invoke("TypeConverter", "parseBoolean", value)
        return: result.success ? result.value : default_value
    }),
    
    # Safe double conversion
    safe_double: @lambda(value, default_value, {
        @if(!value, return: default_value)
        
        # Use Java for double parsing
        result = @java.invoke("TypeConverter", "parseDouble", value)
        return: result.success ? result.value : default_value
    })
}

# Usage examples
query_processing: {
    # Process search parameters
    search_params = {
        page: @safe_int(@request.query.page, 1)
        limit: @safe_int(@request.query.limit, 20)
        include_archived: @safe_boolean(@request.query.archived, false)
        min_price: @safe_double(@request.query.min_price, 0.0)
        max_price: @safe_double(@request.query.max_price, 999999.99)
    }
}
```

## 🔧 Query Parameter Patterns

### Optional Parameters with Java Validation

```tusk
# Query parameter patterns with Java service integration
query_patterns: {
    # Handle missing parameters gracefully
    search_params: {
        query: @request.query.q ?? ""
        page: @int(@request.query.page ?? "1")
        limit: @int(@request.query.limit ?? "20")
        sort: @request.query.sort ?? "relevance"
        order: @request.query.order ?? "desc"
        filters: @request.query.filters ?? "all"
    },
    
    # Validate ranges with Java service
    validated_params: {
        page: @java.invoke("ValidationService", "validatePage", @request.query.page ?? "1")
        limit: @java.invoke("ValidationService", "validateLimit", @request.query.limit ?? "20")
        query: @java.invoke("ValidationService", "sanitizeQuery", @request.query.q ?? "")
    }
}
```

### Boolean Parameters with Java Parsing

```tusk
# Boolean parameter handling with Java integration
boolean_handling: {
    # Parse boolean parameters using Java service
    parse_boolean_params: @lambda({
        return: {
            include_archived: @java.invoke("BooleanParser", "parse", @request.query.archived, false)
            show_details: @java.invoke("BooleanParser", "parse", @request.query.details, true)
            debug_mode: @java.invoke("BooleanParser", "parse", @request.query.debug, false)
            verbose: @java.invoke("BooleanParser", "parse", @request.query.verbose, false)
        }
    })
}
```

## 📊 Array Parameters

### Multiple Values with Java Processing

```tusk
# Array parameter handling with Java integration
array_handling: {
    # Handle both single and multiple values
    ensure_array: @lambda(value, {
        @if(value == null, return: [])
        @if(@isArray(value), return: value)
        return: [value]
    }),
    
    # Process array parameters with Java service
    process_array_params: @lambda({
        # Handle tags parameter
        tags = @ensure_array(@request.query.tags)
        validated_tags = @java.invoke("TagService", "validateTags", tags)
        
        # Handle categories parameter
        categories = @ensure_array(@request.query.categories)
        validated_categories = @java.invoke("CategoryService", "validateCategories", categories)
        
        # Handle IDs parameter
        ids_string = @request.query.ids ?? ""
        ids = @java.invoke("IdParser", "parseIds", ids_string)
        
        return: {
            tags: validated_tags
            categories: validated_categories
            ids: ids
        }
    })
}
```

### Comma-Separated Values with Java Parsing

```tusk
# CSV parameter handling with Java service
csv_handling: {
    # Parse comma-separated values using Java
    parse_csv_params: @lambda({
        # Parse IDs
        ids_string = @request.query.ids ?? ""
        ids = @java.invoke("CsvParser", "parseIntegers", ids_string)
        
        # Parse categories
        categories_string = @request.query.categories ?? ""
        categories = @java.invoke("CsvParser", "parseStrings", categories_string)
        
        # Parse prices
        prices_string = @request.query.prices ?? ""
        prices = @java.invoke("CsvParser", "parseDoubles", prices_string)
        
        return: {
            ids: ids
            categories: categories
            prices: prices
        }
    })
}
```

## 🏗️ Complex Query Structures

### Nested Parameters with Java Processing

```tusk
# Complex query structure handling
complex_queries: {
    # Parse nested structures using Java service
    parse_nested_query: @lambda({
        # Parse filter parameters
        filters = @java.invoke("QueryParser", "parseNestedQuery", @request.query)
        
        # Extract specific filter components
        category_filter = filters.category ?? "all"
        price_filter = filters.price ?? { min: 0, max: 999999 }
        date_filter = filters.date ?? { from: null, to: null }
        
        return: {
            category: category_filter
            price: price_filter
            date: date_filter
        }
    }),
    
    # Common pattern: object-like parameters
    filters: {
        category: @request.query["filter[category]"] ?? "all"
        min_price: @java.invoke("TypeConverter", "parseDouble", @request.query["filter[price][min]"] ?? "0")
        max_price: @java.invoke("TypeConverter", "parseDouble", @request.query["filter[price][max]"] ?? "999999")
        date_from: @java.invoke("DateParser", "parse", @request.query["filter[date][from]"])
        date_to: @java.invoke("DateParser", "parse", @request.query["filter[date][to]"])
    }
}
```

### JSON in Query with Java Parsing

```tusk
# JSON parameter handling with Java service
json_handling: {
    # Parse JSON from query parameter using Java
    parse_json_param: @lambda(param_name, default_value = {}, {
        json_string = @request.query[param_name]
        @if(!json_string, return: default_value)
        
        # Use Java for robust JSON parsing
        result = @java.invoke("JsonParser", "parse", json_string)
        return: result.success ? result.data : default_value
    }),
    
    # Usage examples
    json_params: {
        data: @parse_json_param("data")
        config: @parse_json_param("config", { theme: "default" })
        options: @parse_json_param("options", {})
    }
}
```

## 🔄 Query String Processing

### Manual Query Parsing with Java

```tusk
# Query string processing with Java integration
query_processing: {
    # Parse query string manually using Java service
    parse_query_string: @lambda(query_string, {
        @if(!query_string, return: {})
        
        # Use Java for robust query parsing
        result = @java.invoke("QueryStringParser", "parse", query_string)
        return: result.parameters
    }),
    
    # Build query string from object using Java
    build_query_string: @lambda(params, {
        # Use Java for query string building
        result = @java.invoke("QueryStringBuilder", "build", params)
        return: result.query_string
    })
}
```

## 🛡️ Validation and Sanitization

### Query Validation with Java Services

```tusk
# Query validation with Java integration
query_validation: {
    # Validate search parameters
    validate_search_params: @lambda({
        # Validate query string
        query = @request.query.q ?? ""
        query_validation = @java.invoke("SearchValidator", "validateQuery", query)
        
        @if(!query_validation.valid, {
            return: {
                valid: false
                errors: query_validation.errors
            }
        })
        
        # Validate pagination
        page = @int(@request.query.page ?? "1")
        limit = @int(@request.query.limit ?? "20")
        pagination_validation = @java.invoke("PaginationValidator", "validate", page, limit)
        
        @if(!pagination_validation.valid, {
            return: {
                valid: false
                errors: pagination_validation.errors
            }
        })
        
        # Validate sort parameters
        sort = @request.query.sort ?? "relevance"
        order = @request.query.order ?? "desc"
        sort_validation = @java.invoke("SortValidator", "validate", sort, order)
        
        @if(!sort_validation.valid, {
            return: {
                valid: false
                errors: sort_validation.errors
            }
        })
        
        return: {
            valid: true
            params: {
                query: query_validation.sanitized_query
                page: pagination_validation.page
                limit: pagination_validation.limit
                sort: sort_validation.sort
                order: sort_validation.order
            }
        }
    })
}
```

## 🚀 Advanced Query Features

### Query Caching with Java Integration

```tusk
# Query caching with Java service integration
query_caching: {
    # Generate cache key for query parameters
    generate_cache_key: @lambda({
        # Create deterministic cache key
        key_components = [
            @request.path
            @request.query.q ?? ""
            @request.query.page ?? "1"
            @request.query.limit ?? "20"
            @request.query.sort ?? "relevance"
            @request.query.order ?? "desc"
        ]
        
        # Use Java for hash generation
        cache_key = @java.invoke("CacheKeyGenerator", "generate", key_components)
        return: cache_key
    }),
    
    # Cache query results
    cache_query_results: @lambda({
        cache_key = @generate_cache_key()
        
        # Check cache first
        cached_result = @cache.get(cache_key)
        @if(cached_result, return: cached_result)
        
        # Execute query via Java service
        result = @java.invoke("SearchService", "search", @request.query)
        
        # Cache result
        @cache.set(cache_key, result, "5m")
        
        return: result
    })
}
```

### Query Analytics with Java Services

```tusk
# Query analytics with Java integration
query_analytics: {
    # Track query metrics
    track_query_metrics: @lambda({
        # Extract query metrics
        metrics = {
            query: @request.query.q ?? ""
            page: @int(@request.query.page ?? "1")
            limit: @int(@request.query.limit ?? "20")
            sort: @request.query.sort ?? "relevance"
            order: @request.query.order ?? "desc"
            timestamp: @time.now()
            user_agent: @request.headers["User-Agent"] ?? ""
            ip_address: @request.ip
        }
        
        # Send metrics to Java analytics service
        @java.invoke("AnalyticsService", "trackSearchQuery", metrics)
    }),
    
    # Analyze query patterns
    analyze_query_patterns: @lambda({
        # Get query analysis from Java service
        analysis = @java.invoke("QueryAnalyticsService", "analyze", @request.query)
        
        # Log insights
        @if(analysis.insights, {
            @log.info("Query insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

## 🔧 Java Service Integration

### Query Processing Service

```java
@Service
public class QueryProcessingService {
    
    @Autowired
    private SearchService searchService;
    
    @Autowired
    private ValidationService validationService;
    
    @Autowired
    private CacheService cacheService;
    
    public SearchResult processSearchQuery(Map<String, String> queryParams) {
        // Validate query parameters
        ValidationResult validation = validationService.validateSearchParams(queryParams);
        if (!validation.isValid()) {
            throw new InvalidQueryException(validation.getErrors());
        }
        
        // Generate cache key
        String cacheKey = generateCacheKey(queryParams);
        
        // Check cache
        SearchResult cachedResult = cacheService.get(cacheKey);
        if (cachedResult != null) {
            return cachedResult;
        }
        
        // Execute search
        SearchResult result = searchService.search(validation.getValidatedParams());
        
        // Cache result
        cacheService.set(cacheKey, result, Duration.ofMinutes(5));
        
        return result;
    }
    
    private String generateCacheKey(Map<String, String> queryParams) {
        // Create deterministic cache key
        String key = String.format("search:%s:%s:%s:%s:%s",
            queryParams.getOrDefault("q", ""),
            queryParams.getOrDefault("page", "1"),
            queryParams.getOrDefault("limit", "20"),
            queryParams.getOrDefault("sort", "relevance"),
            queryParams.getOrDefault("order", "desc")
        );
        
        return DigestUtils.md5DigestAsHex(key.getBytes());
    }
}
```

### Validation Service

```java
@Service
public class ValidationService {
    
    public ValidationResult validateSearchParams(Map<String, String> queryParams) {
        List<String> errors = new ArrayList<>();
        Map<String, Object> validatedParams = new HashMap<>();
        
        // Validate query string
        String query = queryParams.getOrDefault("q", "");
        if (query.length() > 100) {
            errors.add("Query string too long (max 100 characters)");
        }
        validatedParams.put("query", sanitizeQuery(query));
        
        // Validate pagination
        int page = parseInt(queryParams.get("page"), 1);
        int limit = parseInt(queryParams.get("limit"), 20);
        
        if (page < 1) {
            errors.add("Page must be greater than 0");
        }
        if (limit < 1 || limit > 100) {
            errors.add("Limit must be between 1 and 100");
        }
        
        validatedParams.put("page", page);
        validatedParams.put("limit", limit);
        
        // Validate sort parameters
        String sort = queryParams.getOrDefault("sort", "relevance");
        String order = queryParams.getOrDefault("order", "desc");
        
        if (!isValidSortField(sort)) {
            errors.add("Invalid sort field");
        }
        if (!isValidOrderDirection(order)) {
            errors.add("Invalid order direction");
        }
        
        validatedParams.put("sort", sort);
        validatedParams.put("order", order);
        
        return new ValidationResult(errors.isEmpty(), errors, validatedParams);
    }
    
    private String sanitizeQuery(String query) {
        // Remove potentially dangerous characters
        return query.replaceAll("[<>\"']", "");
    }
    
    private int parseInt(String value, int defaultValue) {
        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException e) {
            return defaultValue;
        }
    }
    
    private boolean isValidSortField(String sort) {
        return Arrays.asList("relevance", "name", "date", "price").contains(sort);
    }
    
    private boolean isValidOrderDirection(String order) {
        return Arrays.asList("asc", "desc").contains(order.toLowerCase());
    }
}
```

## 🎯 Performance Optimization

### Query Performance Configuration

```tusk
# Query performance optimization
query_performance: {
    # Query result caching
    caching: {
        enabled: true
        ttl: "5m"
        max_results: 1000
        cache_key_generator: "md5"
    },
    
    # Query result pagination
    pagination: {
        default_page: 1
        default_limit: 20
        max_limit: 100
        page_size_options: [10, 20, 50, 100]
    },
    
    # Query result sorting
    sorting: {
        default_sort: "relevance"
        allowed_fields: ["name", "date", "price", "relevance"]
        default_order: "desc"
        allowed_orders: ["asc", "desc"]
    },
    
    # Query result filtering
    filtering: {
        max_filter_depth: 3
        allowed_operators: ["eq", "ne", "gt", "lt", "gte", "lte", "in", "nin", "like"]
        max_filter_values: 100
    }
}
```

## 🔒 Security Considerations

### Query Security Patterns

```tusk
# Query security configuration
query_security: {
    # Input validation
    input_validation: {
        max_query_length: 100
        allowed_characters: "a-zA-Z0-9\\s\\-_.,"
        block_sql_injection: true
        block_xss: true
    },
    
    # Rate limiting
    rate_limiting: {
        enabled: true
        max_requests_per_minute: 100
        max_requests_per_hour: 1000
        block_on_exceed: true
    },
    
    # Query logging
    query_logging: {
        enabled: true
        log_level: "info"
        mask_sensitive_data: true
        log_user_agent: true
        log_ip_address: true
    }
}
```

## 🧪 Testing Query Handlers

### Query Testing Configuration

```tusk
# Query testing configuration
query_testing: {
    # Test cases for search queries
    search_test_cases: [
        {
            name: "basic_search"
            query: { q: "java", page: "1", limit: "20" }
            expected: { status: 200, has_results: true }
        },
        {
            name: "empty_search"
            query: { q: "", page: "1", limit: "20" }
            expected: { status: 200, has_results: true }
        },
        {
            name: "invalid_page"
            query: { q: "java", page: "0", limit: "20" }
            expected: { status: 400, has_error: true }
        },
        {
            name: "invalid_limit"
            query: { q: "java", page: "1", limit: "1000" }
            expected: { status: 400, has_error: true }
        },
        {
            name: "invalid_sort"
            query: { q: "java", sort: "invalid_field" }
            expected: { status: 400, has_error: true }
        }
    ]
}
```

## 🚀 Best Practices

### Query Handling Best Practices

1. **Use Java Services**: Delegate query processing to Java services for better maintainability
2. **Implement Proper Validation**: Use Java validation services for input validation
3. **Cache Query Results**: Cache frequently requested queries for better performance
4. **Sanitize Input**: Always sanitize query parameters to prevent injection attacks
5. **Handle Errors Gracefully**: Return appropriate HTTP status codes and error messages
6. **Log Query Analytics**: Track query patterns for optimization
7. **Implement Rate Limiting**: Prevent abuse with rate limiting
8. **Use Pagination**: Always implement pagination for large result sets

### Common Patterns

```tusk
# Common query handling patterns
common_patterns: {
    # Search patterns
    search_patterns: {
        basic_search: "Simple text search with pagination"
        advanced_search: "Complex search with filters and sorting"
        faceted_search: "Search with category-based filtering"
        autocomplete: "Search with suggestions and completion"
    },
    
    # Filtering patterns
    filtering_patterns: {
        range_filters: "Numeric and date range filtering"
        category_filters: "Categorical data filtering"
        boolean_filters: "True/false condition filtering"
        text_filters: "Text-based pattern matching"
    },
    
    # Sorting patterns
    sorting_patterns: {
        relevance_sort: "Search relevance-based sorting"
        field_sort: "Specific field-based sorting"
        multi_field_sort: "Multiple field sorting"
        custom_sort: "Application-specific sorting logic"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle query parameters with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're building search APIs, filtering systems, or complex data queries, TuskLang provides the tools you need to process query parameters efficiently and securely. 
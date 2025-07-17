# @ Query Database - Java Edition

The `@query` function provides direct database querying capabilities in Java applications. This guide covers SQL queries, database operations, query optimization, and implementing robust database logic using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 Database Query Overview

### Database Operations in Java

```java
@RestController
@RequestMapping("/api")
public class DatabaseController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @GetMapping("/users")
    public ResponseEntity<?> getUsers() {
        // TuskLang handles database queries and processing
        return tuskConfig.getDatabaseHandler().getUsers();
    }
    
    @PostMapping("/user")
    public ResponseEntity<?> createUser(@RequestBody UserRequest request) {
        // TuskLang handles database operations
        return tuskConfig.getDatabaseHandler().createUser(request);
    }
    
    @GetMapping("/analytics")
    public ResponseEntity<?> getAnalytics() {
        // TuskLang handles complex database queries
        return tuskConfig.getDatabaseHandler().getAnalytics();
    }
}
```

```tusk
# app.tsk - Database handling configuration
database_handlers: {
    get_users: @lambda({
        # Build query parameters
        query_params = {
            page: @int(@request.query.page ?? "1")
            limit: @int(@request.query.limit ?? "20")
            search: @request.query.search ?? ""
            sort_by: @request.query.sort_by ?? "created_at"
            sort_order: @request.query.sort_order ?? "desc"
        }
        
        # Execute query via Java service
        users_result = @java.invoke("UserService", "getUsers", query_params)
        
        # Format response
        response_data = {
            users: users_result.users
            pagination: {
                page: query_params.page
                limit: query_params.limit
                total: users_result.total
                pages: @ceil(users_result.total / query_params.limit)
            }
            metadata: {
                query_time: users_result.query_time
                cached: users_result.cached
                timestamp: @time.now()
            }
        }
        
        return: {
            status: 200
            body: response_data
        }
    }),
    
    create_user: @lambda({
        # Validate user data
        validation = @java.invoke("UserValidator", "validateCreate", @request.post)
        
        @if(!validation.valid, {
            return: {
                status: 400
                body: { error: "Validation failed", details: validation.errors }
            }
        })
        
        # Prepare user data
        user_data = {
            name: @request.post.name
            email: @request.post.email
            password_hash: @java.invoke("SecurityService", "hashPassword", @request.post.password)
            created_at: @time.now()
            updated_at: @time.now()
        }
        
        # Execute database operation via Java service
        user_result = @java.invoke("UserService", "createUser", user_data)
        
        return: {
            status: 201
            body: { 
                user: user_result.user
                message: "User created successfully"
            }
        }
    }),
    
    get_analytics: @lambda({
        # Execute complex analytics queries via Java service
        analytics_data = @java.invoke("AnalyticsService", "getAnalytics", {
            date_from: @time.subtract(@time.now(), "30d")
            date_to: @time.now()
            user_id: @session.get("user_id")
        })
        
        return: {
            status: 200
            body: analytics_data
        }
    })
}
```

## 🔍 Basic Database Queries

### Simple Queries

```tusk
# Basic database queries with Java integration
basic_queries: {
    # Simple SELECT query
    simple_select: @lambda(table_name, conditions = {}, {
        # Build query via Java service
        query_result = @java.invoke("QueryBuilder", "select", {
            table: table_name
            conditions: conditions
            limit: 100
        })
        
        return: query_result
    }),
    
    # Query with parameters
    query_with_params: @lambda(sql, params = [], {
        # Execute parameterized query via Java service
        query_result = @java.invoke("DatabaseService", "executeQuery", {
            sql: sql
            parameters: params
            timeout: "30s"
        })
        
        return: query_result
    }),
    
    # Query with error handling
    safe_query: @lambda(sql, params = [], {
        try {
            query_result = @java.invoke("DatabaseService", "executeQuery", {
                sql: sql
                parameters: params
                timeout: "30s"
            })
            
            return: { success: true, data: query_result }
        }, {
            return: { 
                success: false, 
                error: "Query execution failed",
                fallback_data: @get_fallback_data()
            }
        }
    }),
    
    # Get fallback data
    get_fallback_data: @lambda({
        return: []
    })
}
```

### Advanced Queries

```tusk
# Advanced database queries with Java integration
advanced_queries: {
    # Complex JOIN query
    complex_join: @lambda({
        # Execute complex JOIN via Java service
        join_result = @java.invoke("QueryBuilder", "complexJoin", {
            tables: ["users", "orders", "products"]
            joins: [
                { type: "INNER", table: "orders", on: "users.id = orders.user_id" }
                { type: "LEFT", table: "products", on: "orders.product_id = products.id" }
            ]
            conditions: {
                "users.active": true
                "orders.status": "completed"
            }
            group_by: ["users.id", "users.name"]
            order_by: "total_amount DESC"
            limit: 50
        })
        
        return: join_result
    }),
    
    # Aggregation query
    aggregation_query: @lambda({
        # Execute aggregation via Java service
        aggregation_result = @java.invoke("QueryBuilder", "aggregate", {
            table: "orders"
            aggregations: [
                { function: "COUNT", column: "*", alias: "total_orders" }
                { function: "SUM", column: "amount", alias: "total_amount" }
                { function: "AVG", column: "amount", alias: "avg_amount" }
            ]
            conditions: {
                "created_at": { ">=": @time.subtract(@time.now(), "30d") }
            }
            group_by: ["status"]
        })
        
        return: aggregation_result
    }),
    
    # Subquery
    subquery: @lambda({
        # Execute subquery via Java service
        subquery_result = @java.invoke("QueryBuilder", "subquery", {
            main_query: {
                table: "users"
                select: ["id", "name", "email"]
                conditions: {
                    "id": {
                        "IN": {
                            subquery: {
                                table: "orders"
                                select: ["user_id"]
                                conditions: {
                                    "amount": { ">": 1000 }
                                }
                            }
                        }
                    }
                }
            }
        })
        
        return: subquery_result
    })
}
```

## 📊 Data Operations

### CRUD Operations

```tusk
# CRUD operations with Java integration
crud_operations: {
    # Create operation
    create_record: @lambda(table_name, data, {
        # Validate data via Java service
        validation = @java.invoke("DataValidator", "validateCreate", table_name, data)
        
        @if(!validation.valid, {
            return: { success: false, errors: validation.errors }
        })
        
        # Execute create via Java service
        create_result = @java.invoke("DatabaseService", "create", {
            table: table_name
            data: data
            return_id: true
        })
        
        return: { success: true, id: create_result.id, data: create_result.data }
    }),
    
    # Read operation
    read_record: @lambda(table_name, record_id, {
        # Execute read via Java service
        read_result = @java.invoke("DatabaseService", "read", {
            table: table_name
            id: record_id
            include_relations: true
        })
        
        @if(!read_result.found, {
            return: { success: false, error: "Record not found" }
        })
        
        return: { success: true, data: read_result.data }
    }),
    
    # Update operation
    update_record: @lambda(table_name, record_id, data, {
        # Validate data via Java service
        validation = @java.invoke("DataValidator", "validateUpdate", table_name, data)
        
        @if(!validation.valid, {
            return: { success: false, errors: validation.errors }
        })
        
        # Execute update via Java service
        update_result = @java.invoke("DatabaseService", "update", {
            table: table_name
            id: record_id
            data: data
            return_updated: true
        })
        
        return: { success: true, data: update_result.data }
    }),
    
    # Delete operation
    delete_record: @lambda(table_name, record_id, {
        # Check dependencies via Java service
        dependencies = @java.invoke("DatabaseService", "checkDependencies", table_name, record_id)
        
        @if(@len(dependencies) > 0, {
            return: { 
                success: false, 
                error: "Cannot delete record with dependencies",
                dependencies: dependencies
            }
        })
        
        # Execute delete via Java service
        delete_result = @java.invoke("DatabaseService", "delete", {
            table: table_name
            id: record_id
            soft_delete: true
        })
        
        return: { success: true, message: "Record deleted successfully" }
    })
}
```

### Batch Operations

```tusk
# Batch operations with Java integration
batch_operations: {
    # Batch insert
    batch_insert: @lambda(table_name, records, {
        # Validate batch data via Java service
        validation = @java.invoke("DataValidator", "validateBatch", table_name, records)
        
        @if(!validation.valid, {
            return: { success: false, errors: validation.errors }
        })
        
        # Execute batch insert via Java service
        batch_result = @java.invoke("DatabaseService", "batchInsert", {
            table: table_name
            records: records
            batch_size: 100
        })
        
        return: { 
            success: true, 
            inserted_count: batch_result.inserted_count,
            errors: batch_result.errors
        }
    }),
    
    # Batch update
    batch_update: @lambda(table_name, updates, {
        # Execute batch update via Java service
        batch_result = @java.invoke("DatabaseService", "batchUpdate", {
            table: table_name
            updates: updates
            batch_size: 50
        })
        
        return: { 
            success: true, 
            updated_count: batch_result.updated_count,
            errors: batch_result.errors
        }
    }),
    
    # Transaction
    transaction: @lambda(operations, {
        # Execute transaction via Java service
        transaction_result = @java.invoke("DatabaseService", "transaction", {
            operations: operations
            isolation_level: "READ_COMMITTED"
            timeout: "60s"
        })
        
        @if(!transaction_result.success, {
            return: { 
                success: false, 
                error: "Transaction failed",
                rollback_operations: transaction_result.rollback_operations
            }
        })
        
        return: { 
            success: true, 
            results: transaction_result.results
        }
    })
}
```

## 🔧 Java Service Integration

### Database Processing Service

```java
@Service
@Transactional
public class DatabaseProcessingService {
    
    @Autowired
    private UserService userService;
    
    @Autowired
    private DatabaseService databaseService;
    
    @Autowired
    private QueryBuilder queryBuilder;
    
    public ResponseEntity<?> getUsers(Map<String, Object> queryParams) {
        try {
            // Build query
            QueryResult result = userService.getUsers(queryParams);
            
            // Format response
            Map<String, Object> response = Map.of(
                "users", result.getUsers(),
                "pagination", buildPagination(queryParams, result.getTotal()),
                "metadata", buildMetadata(result)
            );
            
            return ResponseEntity.ok(response);
            
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Failed to get users", "details", e.getMessage()));
        }
    }
    
    public ResponseEntity<?> createUser(UserRequest request) {
        try {
            // Validate request
            ValidationResult validation = userService.validateCreate(request);
            if (!validation.isValid()) {
                return ResponseEntity.badRequest()
                    .body(Map.of("error", "Validation failed", "details", validation.getErrors()));
            }
            
            // Create user
            User user = userService.createUser(request);
            
            return ResponseEntity.status(201)
                .body(Map.of(
                    "user", user,
                    "message", "User created successfully"
                ));
                
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Failed to create user", "details", e.getMessage()));
        }
    }
    
    public ResponseEntity<?> getAnalytics(Map<String, Object> params) {
        try {
            // Get analytics data
            AnalyticsData analytics = analyticsService.getAnalytics(params);
            
            return ResponseEntity.ok(analytics);
            
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Failed to get analytics", "details", e.getMessage()));
        }
    }
    
    private Map<String, Object> buildPagination(Map<String, Object> queryParams, long total) {
        int page = (Integer) queryParams.get("page");
        int limit = (Integer) queryParams.get("limit");
        
        return Map.of(
            "page", page,
            "limit", limit,
            "total", total,
            "pages", (int) Math.ceil((double) total / limit)
        );
    }
    
    private Map<String, Object> buildMetadata(QueryResult result) {
        return Map.of(
            "query_time", result.getQueryTime(),
            "cached", result.isCached(),
            "timestamp", LocalDateTime.now()
        );
    }
}
```

### Database Service

```java
@Service
public class DatabaseService {
    
    @Autowired
    private JdbcTemplate jdbcTemplate;
    
    @Autowired
    private DataSource dataSource;
    
    public QueryResult executeQuery(Map<String, Object> params) {
        String sql = (String) params.get("sql");
        List<Object> parameters = (List<Object>) params.get("parameters");
        String timeout = (String) params.get("timeout");
        
        try {
            // Set timeout if specified
            if (timeout != null) {
                jdbcTemplate.setQueryTimeout(parseTimeout(timeout));
            }
            
            // Execute query
            List<Map<String, Object>> results = jdbcTemplate.queryForList(sql, parameters.toArray());
            
            return new QueryResult(true, results, null);
            
        } catch (Exception e) {
            return new QueryResult(false, null, e.getMessage());
        }
    }
    
    public CreateResult create(Map<String, Object> params) {
        String table = (String) params.get("table");
        Map<String, Object> data = (Map<String, Object>) params.get("data");
        boolean returnId = (Boolean) params.getOrDefault("return_id", false);
        
        try {
            // Build INSERT query
            String sql = buildInsertQuery(table, data.keySet());
            List<Object> values = new ArrayList<>(data.values());
            
            // Execute insert
            if (returnId) {
                KeyHolder keyHolder = new GeneratedKeyHolder();
                jdbcTemplate.update(connection -> {
                    PreparedStatement ps = connection.prepareStatement(sql, Statement.RETURN_GENERATED_KEYS);
                    for (int i = 0; i < values.size(); i++) {
                        ps.setObject(i + 1, values.get(i));
                    }
                    return ps;
                }, keyHolder);
                
                Long id = keyHolder.getKey().longValue();
                data.put("id", id);
                
                return new CreateResult(true, id, data, null);
            } else {
                int rowsAffected = jdbcTemplate.update(sql, values.toArray());
                return new CreateResult(true, null, data, null);
            }
            
        } catch (Exception e) {
            return new CreateResult(false, null, null, e.getMessage());
        }
    }
    
    public ReadResult read(Map<String, Object> params) {
        String table = (String) params.get("table");
        Object id = params.get("id");
        boolean includeRelations = (Boolean) params.getOrDefault("include_relations", false);
        
        try {
            // Build SELECT query
            String sql = "SELECT * FROM " + table + " WHERE id = ?";
            
            // Execute query
            List<Map<String, Object>> results = jdbcTemplate.queryForList(sql, id);
            
            if (results.isEmpty()) {
                return new ReadResult(false, null, "Record not found");
            }
            
            Map<String, Object> data = results.get(0);
            
            // Include relations if requested
            if (includeRelations) {
                data = includeRelations(table, data);
            }
            
            return new ReadResult(true, data, null);
            
        } catch (Exception e) {
            return new ReadResult(false, null, e.getMessage());
        }
    }
    
    public UpdateResult update(Map<String, Object> params) {
        String table = (String) params.get("table");
        Object id = params.get("id");
        Map<String, Object> data = (Map<String, Object>) params.get("data");
        boolean returnUpdated = (Boolean) params.getOrDefault("return_updated", false);
        
        try {
            // Build UPDATE query
            String sql = buildUpdateQuery(table, data.keySet());
            List<Object> values = new ArrayList<>(data.values());
            values.add(id);
            
            // Execute update
            int rowsAffected = jdbcTemplate.update(sql, values.toArray());
            
            if (rowsAffected == 0) {
                return new UpdateResult(false, null, "Record not found");
            }
            
            Map<String, Object> updatedData = returnUpdated ? read(Map.of("table", table, "id", id)).getData() : null;
            
            return new UpdateResult(true, updatedData, null);
            
        } catch (Exception e) {
            return new UpdateResult(false, null, e.getMessage());
        }
    }
    
    public DeleteResult delete(Map<String, Object> params) {
        String table = (String) params.get("table");
        Object id = params.get("id");
        boolean softDelete = (Boolean) params.getOrDefault("soft_delete", false);
        
        try {
            if (softDelete) {
                // Soft delete
                String sql = "UPDATE " + table + " SET deleted_at = ? WHERE id = ?";
                int rowsAffected = jdbcTemplate.update(sql, LocalDateTime.now(), id);
                
                if (rowsAffected == 0) {
                    return new DeleteResult(false, "Record not found");
                }
            } else {
                // Hard delete
                String sql = "DELETE FROM " + table + " WHERE id = ?";
                int rowsAffected = jdbcTemplate.update(sql, id);
                
                if (rowsAffected == 0) {
                    return new DeleteResult(false, "Record not found");
                }
            }
            
            return new DeleteResult(true, null);
            
        } catch (Exception e) {
            return new DeleteResult(false, e.getMessage());
        }
    }
    
    private String buildInsertQuery(String table, Set<String> columns) {
        String columnList = String.join(", ", columns);
        String valuePlaceholders = columns.stream()
            .map(col -> "?")
            .collect(Collectors.joining(", "));
        
        return "INSERT INTO " + table + " (" + columnList + ") VALUES (" + valuePlaceholders + ")";
    }
    
    private String buildUpdateQuery(String table, Set<String> columns) {
        String setClause = columns.stream()
            .map(col -> col + " = ?")
            .collect(Collectors.joining(", "));
        
        return "UPDATE " + table + " SET " + setClause + " WHERE id = ?";
    }
    
    private Map<String, Object> includeRelations(String table, Map<String, Object> data) {
        // Implementation to include related data
        // This would depend on your specific database schema and relationships
        return data;
    }
    
    private int parseTimeout(String timeout) {
        // Parse timeout string (e.g., "30s" -> 30)
        if (timeout.endsWith("s")) {
            return Integer.parseInt(timeout.substring(0, timeout.length() - 1));
        }
        return 30; // default
    }
}
```

## 🚀 Advanced Database Features

### Query Optimization

```tusk
# Query optimization with Java integration
query_optimization: {
    # Optimize query
    optimize_query: @lambda(sql, {
        # Optimize query via Java service
        optimized_result = @java.invoke("QueryOptimizer", "optimize", {
            sql: sql
            database_type: "postgresql"
            table_stats: true
        })
        
        return: optimized_result
    }),
    
    # Cache query results
    cache_query_results: @lambda(sql, params, cache_key, ttl = "5m", {
        # Check cache first
        cached_result = @cache.get(cache_key)
        
        @if(cached_result, {
            return: { success: true, data: cached_result, cached: true }
        })
        
        # Execute query
        query_result = @java.invoke("DatabaseService", "executeQuery", {
            sql: sql
            parameters: params
        })
        
        # Cache result
        @cache.set(cache_key, query_result.data, ttl)
        
        return: { success: true, data: query_result.data, cached: false }
    }),
    
    # Query with pagination
    paginated_query: @lambda(sql, params, page, limit, {
        # Execute paginated query via Java service
        paginated_result = @java.invoke("DatabaseService", "executePaginatedQuery", {
            sql: sql
            parameters: params
            page: page
            limit: limit
        })
        
        return: paginated_result
    })
}
```

### Database Analytics

```tusk
# Database analytics with Java integration
database_analytics: {
    # Track query metrics
    track_query_metrics: @lambda({
        # Extract query metrics
        query_metrics = {
            sql: @request.post.sql
            execution_time: @request.processing_time
            result_count: @len(@request.post.results ?? [])
            user_id: @session.get("user_id")
            timestamp: @time.now()
        }
        
        # Send metrics to Java analytics service
        @java.invoke("DatabaseAnalyticsService", "track", query_metrics)
    }),
    
    # Analyze query performance
    analyze_query_performance: @lambda(sql, {
        # Get query analysis from Java service
        analysis = @java.invoke("DatabaseAnalysisService", "analyze", sql)
        
        # Log insights
        @if(analysis.insights, {
            @log.info("Query insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

## 🔒 Security Considerations

### Database Security Patterns

```tusk
# Database security configuration
database_security_patterns: {
    # Query validation
    query_validation: {
        max_query_length: "10KB"
        allowed_tables: ["users", "orders", "products"]
        block_dangerous_keywords: ["DROP", "DELETE", "TRUNCATE"]
        validate_sql_syntax: true
        parameterize_queries: true
    },
    
    # Access control
    access_control: {
        require_authentication: true
        check_table_permissions: true
        validate_user_permissions: true
        audit_all_queries: true
    },
    
    # Query logging
    query_logging: {
        enabled: true
        log_level: "info"
        log_all_queries: true
        log_query_errors: true
        mask_sensitive_data: true
    }
}
```

## 🧪 Testing Database Handlers

### Database Testing Configuration

```tusk
# Database testing configuration
database_testing: {
    # Test cases for basic queries
    basic_query_test_cases: [
        {
            name: "simple_select_query"
            sql: "SELECT * FROM users WHERE active = ?"
            params: [true]
            expected: { success: true, has_results: true }
        },
        {
            name: "parameterized_query"
            sql: "SELECT * FROM users WHERE email = ?"
            params: ["test@example.com"]
            expected: { success: true, result_count: 1 }
        },
        {
            name: "invalid_query"
            sql: "SELECT * FROM nonexistent_table"
            params: []
            expected: { success: false, has_error: true }
        }
    ],
    
    # Test cases for CRUD operations
    crud_test_cases: [
        {
            name: "create_user"
            operation: "create"
            table: "users"
            data: { name: "John", email: "john@example.com" }
            expected: { success: true, has_id: true }
        },
        {
            name: "read_user"
            operation: "read"
            table: "users"
            id: 1
            expected: { success: true, has_data: true }
        },
        {
            name: "update_user"
            operation: "update"
            table: "users"
            id: 1
            data: { name: "John Updated" }
            expected: { success: true }
        }
    ]
}
```

## 🚀 Best Practices

### Database Handling Best Practices

1. **Use Java Services**: Delegate database operations to Java services for better maintainability
2. **Parameterize Queries**: Always use parameterized queries to prevent SQL injection
3. **Validate Data**: Validate all data before database operations
4. **Use Transactions**: Use transactions for multi-step operations
5. **Implement Caching**: Cache frequently accessed data for performance
6. **Monitor Performance**: Track query performance and optimize slow queries
7. **Handle Errors Gracefully**: Provide meaningful error messages for database failures
8. **Use Connection Pooling**: Implement proper connection pooling for scalability

### Common Patterns

```tusk
# Common database handling patterns
common_patterns: {
    # Query patterns
    query_patterns: {
        simple_select: "Basic SELECT queries with conditions"
        complex_join: "Multi-table JOIN queries"
        aggregation: "Aggregation queries with GROUP BY"
        subquery: "Queries with subqueries and nested logic"
    },
    
    # CRUD patterns
    crud_patterns: {
        create_operation: "Data creation with validation"
        read_operation: "Data retrieval with filtering"
        update_operation: "Data modification with constraints"
        delete_operation: "Data removal with dependencies"
    },
    
    # Performance patterns
    performance_patterns: {
        query_caching: "Caching frequently executed queries"
        pagination: "Implementing efficient pagination"
        indexing: "Using database indexes for performance"
        connection_pooling: "Managing database connections efficiently"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle database operations with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're building data-driven applications, implementing complex queries, or managing database transactions, TuskLang provides the tools you need to handle database operations efficiently and securely. 
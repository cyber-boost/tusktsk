# 🌐 API Design with TuskLang Java

**"We don't bow to any king" - API Design Java Edition**

TuskLang Java enables building well-designed APIs with RESTful principles, GraphQL support, and comprehensive API versioning that scales with your application needs.

## 🎯 API Design Architecture Overview

### API Configuration
```java
// api-design-app.tsk
[api_design]
name: "API Design TuskLang App"
version: "2.0.0"
paradigm: "api_first"
documentation: "openapi"

[rest_api]
base_path: "/api"
versioning: "url_path"
versions: ["v1", "v2", "v3"]
default_version: "v2"

endpoints: {
    users: {
        path: "/users"
        methods: ["GET", "POST", "PUT", "DELETE", "PATCH"]
        pagination: {
            enabled: true
            default_size: 20
            max_size: 100
            page_parameter: "page"
            size_parameter: "size"
        }
        filtering: {
            enabled: true
            fields: ["email", "status", "created_at"]
            operators: ["eq", "ne", "gt", "lt", "gte", "lte", "like", "in"]
        }
        sorting: {
            enabled: true
            fields: ["id", "email", "first_name", "last_name", "created_at"]
            default_sort: "created_at:desc"
        }
        caching: {
            enabled: true
            ttl: "5m"
            etag: true
            cache_control: "public, max-age=300"
        }
        rate_limiting: {
            enabled: true
            requests_per_minute: 100
            burst_size: 10
            user_based: true
        }
        security: {
            authentication: "jwt"
            authorization: "rbac"
            scopes: ["read:users", "write:users", "delete:users"]
        }
    }
    
    orders: {
        path: "/orders"
        methods: ["GET", "POST", "PUT", "DELETE", "PATCH"]
        pagination: {
            enabled: true
            default_size: 20
            max_size: 100
            page_parameter: "page"
            size_parameter: "size"
        }
        filtering: {
            enabled: true
            fields: ["user_id", "status", "total_amount", "created_at"]
            operators: ["eq", "ne", "gt", "lt", "gte", "lte", "like", "in"]
        }
        sorting: {
            enabled: true
            fields: ["id", "user_id", "status", "total_amount", "created_at"]
            default_sort: "created_at:desc"
        }
        caching: {
            enabled: true
            ttl: "2m"
            etag: true
            cache_control: "public, max-age=120"
        }
        rate_limiting: {
            enabled: true
            requests_per_minute: 200
            burst_size: 20
            user_based: true
        }
        security: {
            authentication: "jwt"
            authorization: "rbac"
            scopes: ["read:orders", "write:orders", "delete:orders"]
        }
    }
    
    products: {
        path: "/products"
        methods: ["GET", "POST", "PUT", "DELETE", "PATCH"]
        pagination: {
            enabled: true
            default_size: 50
            max_size: 200
            page_parameter: "page"
            size_parameter: "size"
        }
        filtering: {
            enabled: true
            fields: ["category", "price", "stock", "status", "created_at"]
            operators: ["eq", "ne", "gt", "lt", "gte", "lte", "like", "in", "between"]
        }
        sorting: {
            enabled: true
            fields: ["id", "name", "price", "stock", "created_at"]
            default_sort: "name:asc"
        }
        caching: {
            enabled: true
            ttl: "10m"
            etag: true
            cache_control: "public, max-age=600"
        }
        rate_limiting: {
            enabled: true
            requests_per_minute: 500
            burst_size: 50
            user_based: false
        }
        security: {
            authentication: "jwt"
            authorization: "rbac"
            scopes: ["read:products", "write:products", "delete:products"]
        }
    }
}

[graphql_api]
enabled: true
path: "/graphql"
playground: {
    enabled: true
    path: "/graphql-playground"
    settings: {
        theme: "dark"
        show_docs: true
        show_schema: true
    }
}

introspection: {
    enabled: true
    production: false
}

subscriptions: {
    enabled: true
    transport: "websocket"
    path: "/graphql-subscriptions"
}

rate_limiting: {
    enabled: true
    requests_per_minute: 1000
    complexity_limit: 1000
    depth_limit: 10
}

caching: {
    enabled: true
    ttl: "5m"
    strategy: "field_level"
}

[api_documentation]
openapi: {
    version: "3.0.3"
    title: "TuskLang API"
    description: "Comprehensive API for TuskLang applications"
    version: "2.0.0"
    contact: {
        name: "API Support"
        email: "api@tusklang.org"
        url: "https://tusklang.org/support"
    }
    license: {
        name: "MIT"
        url: "https://opensource.org/licenses/MIT"
    }
    servers: [
        {
            url: "https://api.tusklang.org"
            description: "Production server"
        },
        {
            url: "https://api-staging.tusklang.org"
            description: "Staging server"
        },
        {
            url: "http://localhost:8080"
            description: "Local development server"
        }
    ]
    paths: {
        generate_from_controllers: true
        include_examples: true
        include_schemas: true
    }
    security: {
        bearer_auth: {
            type: "http"
            scheme: "bearer"
            bearer_format: "JWT"
        }
        oauth2: {
            type: "oauth2"
            flows: {
                authorization_code: {
                    authorization_url: "https://auth.tusklang.org/oauth/authorize"
                    token_url: "https://auth.tusklang.org/oauth/token"
                    scopes: {
                        "read:users": "Read user information"
                        "write:users": "Create and update users"
                        "delete:users": "Delete users"
                        "read:orders": "Read order information"
                        "write:orders": "Create and update orders"
                        "delete:orders": "Delete orders"
                    }
                }
            }
        }
    }
}

[api_versioning]
strategy: "url_path"
versions: {
    v1: {
        status: "deprecated"
        sunset_date: "2024-12-31"
        migration_guide: "https://docs.tusklang.org/api/migration/v1-to-v2"
    }
    v2: {
        status: "current"
        release_date: "2023-01-01"
        features: ["enhanced_filtering", "improved_pagination", "graphql_support"]
    }
    v3: {
        status: "beta"
        release_date: "2024-01-01"
        features: ["real_time_subscriptions", "advanced_analytics", "ai_powered_search"]
    }
}

backward_compatibility: {
    enabled: true
    deprecated_features: {
        warning_header: true
        deprecation_notice: true
        migration_guide: true
    }
}

[api_monitoring]
metrics: {
    enabled: true
    endpoint: "/actuator/metrics"
    custom_metrics: [
        "request_duration",
        "response_size",
        "error_rate",
        "cache_hit_rate",
        "rate_limit_exceeded"
    ]
}

tracing: {
    enabled: true
    sampling_rate: 0.1
    correlation_id_header: "X-Correlation-ID"
    request_id_header: "X-Request-ID"
}

logging: {
    enabled: true
    level: "INFO"
    format: "json"
    include_headers: ["Authorization", "User-Agent", "X-Request-ID"]
    exclude_headers: ["Cookie", "Set-Cookie"]
    mask_sensitive_data: true
}

[api_security]
authentication: {
    jwt: {
        enabled: true
        secret: @env.secure("JWT_SECRET")
        algorithm: "HS256"
        expiration: "24h"
        refresh_expiration: "7d"
        issuer: @env("JWT_ISSUER", "tusklang-api")
        audience: @env("JWT_AUDIENCE", "tusklang-users")
    }
    oauth2: {
        enabled: true
        providers: {
            google: {
                client_id: @env.secure("GOOGLE_CLIENT_ID")
                client_secret: @env.secure("GOOGLE_CLIENT_SECRET")
                redirect_uri: @env("GOOGLE_REDIRECT_URI")
                scopes: ["openid", "email", "profile"]
            }
            github: {
                client_id: @env.secure("GITHUB_CLIENT_ID")
                client_secret: @env.secure("GITHUB_CLIENT_SECRET")
                redirect_uri: @env("GITHUB_REDIRECT_URI")
                scopes: ["read:user", "user:email"]
            }
        }
    }
}

authorization: {
    rbac: {
        enabled: true
        roles: [
            "admin",
            "user",
            "moderator",
            "readonly"
        ]
        permissions: {
            "admin": ["*"]
            "user": ["read:own", "write:own", "delete:own"]
            "moderator": ["read:all", "write:all", "delete:limited"]
            "readonly": ["read:all"]
        }
    }
}

cors: {
    enabled: true
    allowed_origins: [
        "https://app.tusklang.org",
        "https://admin.tusklang.org",
        "http://localhost:3000"
    ]
    allowed_methods: ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"]
    allowed_headers: ["Content-Type", "Authorization", "X-Request-ID"]
    allow_credentials: true
    max_age: 3600
}

rate_limiting: {
    enabled: true
    strategy: "token_bucket"
    default_limit: {
        requests_per_minute: 100
        burst_size: 10
    }
    user_limits: {
        authenticated: {
            requests_per_minute: 1000
            burst_size: 100
        }
        premium: {
            requests_per_minute: 5000
            burst_size: 500
        }
    }
    storage: "redis"
    key_resolver: "user_id"
}
```

## 🌐 RESTful API Implementation

### API Configuration
```java
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;
import org.tusklang.java.annotations.TuskConfig;
import java.util.List;

@Configuration
@TuskConfig
public class APIDesignConfiguration implements WebMvcConfigurer {
    
    private final APIDesignConfig apiDesignConfig;
    
    public APIDesignConfiguration(APIDesignConfig apiDesignConfig) {
        this.apiDesignConfig = apiDesignConfig;
    }
    
    @Bean
    public ApiVersioningInterceptor apiVersioningInterceptor() {
        return new ApiVersioningInterceptor(apiDesignConfig.getApiVersioning());
    }
    
    @Bean
    public RateLimitingInterceptor rateLimitingInterceptor() {
        return new RateLimitingInterceptor(apiDesignConfig.getApiSecurity().getRateLimiting());
    }
    
    @Bean
    public CachingInterceptor cachingInterceptor() {
        return new CachingInterceptor();
    }
    
    @Bean
    public CorrelationIdInterceptor correlationIdInterceptor() {
        return new CorrelationIdInterceptor(apiDesignConfig.getApiMonitoring().getTracing());
    }
    
    @Override
    public void addInterceptors(InterceptorRegistry registry) {
        registry.addInterceptor(correlationIdInterceptor());
        registry.addInterceptor(apiVersioningInterceptor());
        registry.addInterceptor(rateLimitingInterceptor());
        registry.addInterceptor(cachingInterceptor());
    }
}

@TuskConfig
public class APIDesignConfig {
    
    private String name;
    private String version;
    private String paradigm;
    private String documentation;
    private RestAPIConfig restApi;
    private GraphQLAPIConfig graphqlApi;
    private APIDocumentationConfig apiDocumentation;
    private APIVersioningConfig apiVersioning;
    private APIMonitoringConfig apiMonitoring;
    private APISecurityConfig apiSecurity;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public String getParadigm() { return paradigm; }
    public void setParadigm(String paradigm) { this.paradigm = paradigm; }
    
    public String getDocumentation() { return documentation; }
    public void setDocumentation(String documentation) { this.documentation = documentation; }
    
    public RestAPIConfig getRestApi() { return restApi; }
    public void setRestApi(RestAPIConfig restApi) { this.restApi = restApi; }
    
    public GraphQLAPIConfig getGraphqlApi() { return graphqlApi; }
    public void setGraphqlApi(GraphQLAPIConfig graphqlApi) { this.graphqlApi = graphqlApi; }
    
    public APIDocumentationConfig getApiDocumentation() { return apiDocumentation; }
    public void setApiDocumentation(APIDocumentationConfig apiDocumentation) { this.apiDocumentation = apiDocumentation; }
    
    public APIVersioningConfig getApiVersioning() { return apiVersioning; }
    public void setApiVersioning(APIVersioningConfig apiVersioning) { this.apiVersioning = apiVersioning; }
    
    public APIMonitoringConfig getApiMonitoring() { return apiMonitoring; }
    public void setApiMonitoring(APIMonitoringConfig apiMonitoring) { this.apiMonitoring = apiMonitoring; }
    
    public APISecurityConfig getApiSecurity() { return apiSecurity; }
    public void setApiSecurity(APISecurityConfig apiSecurity) { this.apiSecurity = apiSecurity; }
}

@TuskConfig
public class RestAPIConfig {
    
    private String basePath;
    private String versioning;
    private List<String> versions;
    private String defaultVersion;
    private Map<String, EndpointConfig> endpoints;
    
    // Getters and setters
    public String getBasePath() { return basePath; }
    public void setBasePath(String basePath) { this.basePath = basePath; }
    
    public String getVersioning() { return versioning; }
    public void setVersioning(String versioning) { this.versioning = versioning; }
    
    public List<String> getVersions() { return versions; }
    public void setVersions(List<String> versions) { this.versions = versions; }
    
    public String getDefaultVersion() { return defaultVersion; }
    public void setDefaultVersion(String defaultVersion) { this.defaultVersion = defaultVersion; }
    
    public Map<String, EndpointConfig> getEndpoints() { return endpoints; }
    public void setEndpoints(Map<String, EndpointConfig> endpoints) { this.endpoints = endpoints; }
}
```

### RESTful Controller Implementation
```java
import org.springframework.web.bind.annotation.*;
import org.springframework.http.ResponseEntity;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.validation.annotation.Validated;
import javax.validation.Valid;
import java.util.List;

@RestController
@RequestMapping("/api/v2/users")
@Validated
@ApiVersion("v2")
public class UserController {
    
    private final UserService userService;
    private final UserMapper userMapper;
    private final APIMonitoringConfig monitoringConfig;
    
    public UserController(UserService userService, 
                         UserMapper userMapper,
                         APIMonitoringConfig monitoringConfig) {
        this.userService = userService;
        this.userMapper = userMapper;
        this.monitoringConfig = monitoringConfig;
    }
    
    @GetMapping
    @ApiOperation("Get all users with pagination and filtering")
    @ApiResponses({
        @ApiResponse(code = 200, message = "Users retrieved successfully"),
        @ApiResponse(code = 400, message = "Invalid request parameters"),
        @ApiResponse(code = 401, message = "Unauthorized"),
        @ApiResponse(code = 403, message = "Forbidden")
    })
    public ResponseEntity<PageResponse<UserResponse>> getUsers(
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size,
            @RequestParam(required = false) String email,
            @RequestParam(required = false) String status,
            @RequestParam(required = false) String sort,
            @RequestParam(required = false) String order) {
        
        // Build filter criteria
        UserFilterCriteria criteria = UserFilterCriteria.builder()
            .email(email)
            .status(status)
            .build();
        
        // Build sort criteria
        Sort sortCriteria = Sort.by(Sort.Direction.fromString(order != null ? order : "desc"), 
            sort != null ? sort : "createdAt");
        
        // Get paginated users
        Page<User> users = userService.getUsers(criteria, 
            PageRequest.of(page, size, sortCriteria));
        
        // Map to response DTOs
        List<UserResponse> userResponses = users.getContent().stream()
            .map(userMapper::toResponse)
            .collect(Collectors.toList());
        
        // Build response
        PageResponse<UserResponse> response = PageResponse.<UserResponse>builder()
            .data(userResponses)
            .page(users.getNumber())
            .size(users.getSize())
            .totalElements(users.getTotalElements())
            .totalPages(users.getTotalPages())
            .hasNext(users.hasNext())
            .hasPrevious(users.hasPrevious())
            .build();
        
        return ResponseEntity.ok()
            .cacheControl(CacheControl.maxAge(Duration.ofMinutes(5)))
            .eTag(generateETag(response))
            .body(response);
    }
    
    @GetMapping("/{id}")
    @ApiOperation("Get user by ID")
    @ApiResponses({
        @ApiResponse(code = 200, message = "User retrieved successfully"),
        @ApiResponse(code = 404, message = "User not found"),
        @ApiResponse(code = 401, message = "Unauthorized"),
        @ApiResponse(code = 403, message = "Forbidden")
    })
    public ResponseEntity<UserResponse> getUserById(@PathVariable String id) {
        User user = userService.getUserById(id);
        UserResponse response = userMapper.toResponse(user);
        
        return ResponseEntity.ok()
            .cacheControl(CacheControl.maxAge(Duration.ofMinutes(10)))
            .eTag(generateETag(response))
            .body(response);
    }
    
    @PostMapping
    @ApiOperation("Create a new user")
    @ApiResponses({
        @ApiResponse(code = 201, message = "User created successfully"),
        @ApiResponse(code = 400, message = "Invalid request body"),
        @ApiResponse(code = 409, message = "User already exists"),
        @ApiResponse(code = 401, message = "Unauthorized"),
        @ApiResponse(code = 403, message = "Forbidden")
    })
    public ResponseEntity<UserResponse> createUser(@Valid @RequestBody CreateUserRequest request) {
        User user = userMapper.toEntity(request);
        User createdUser = userService.createUser(user);
        UserResponse response = userMapper.toResponse(createdUser);
        
        return ResponseEntity.status(HttpStatus.CREATED)
            .location(URI.create("/api/v2/users/" + createdUser.getId()))
            .body(response);
    }
    
    @PutMapping("/{id}")
    @ApiOperation("Update user by ID")
    @ApiResponses({
        @ApiResponse(code = 200, message = "User updated successfully"),
        @ApiResponse(code = 400, message = "Invalid request body"),
        @ApiResponse(code = 404, message = "User not found"),
        @ApiResponse(code = 401, message = "Unauthorized"),
        @ApiResponse(code = 403, message = "Forbidden")
    })
    public ResponseEntity<UserResponse> updateUser(
            @PathVariable String id,
            @Valid @RequestBody UpdateUserRequest request) {
        
        User user = userMapper.toEntity(request);
        user.setId(id);
        User updatedUser = userService.updateUser(user);
        UserResponse response = userMapper.toResponse(updatedUser);
        
        return ResponseEntity.ok()
            .eTag(generateETag(response))
            .body(response);
    }
    
    @PatchMapping("/{id}")
    @ApiOperation("Partially update user by ID")
    @ApiResponses({
        @ApiResponse(code = 200, message = "User updated successfully"),
        @ApiResponse(code = 400, message = "Invalid request body"),
        @ApiResponse(code = 404, message = "User not found"),
        @ApiResponse(code = 401, message = "Unauthorized"),
        @ApiResponse(code = 403, message = "Forbidden")
    })
    public ResponseEntity<UserResponse> patchUser(
            @PathVariable String id,
            @RequestBody Map<String, Object> updates) {
        
        User updatedUser = userService.patchUser(id, updates);
        UserResponse response = userMapper.toResponse(updatedUser);
        
        return ResponseEntity.ok()
            .eTag(generateETag(response))
            .body(response);
    }
    
    @DeleteMapping("/{id}")
    @ApiOperation("Delete user by ID")
    @ApiResponses({
        @ApiResponse(code = 204, message = "User deleted successfully"),
        @ApiResponse(code = 404, message = "User not found"),
        @ApiResponse(code = 401, message = "Unauthorized"),
        @ApiResponse(code = 403, message = "Forbidden")
    })
    public ResponseEntity<Void> deleteUser(@PathVariable String id) {
        userService.deleteUser(id);
        return ResponseEntity.noContent().build();
    }
    
    @GetMapping("/{id}/orders")
    @ApiOperation("Get orders for a specific user")
    @ApiResponses({
        @ApiResponse(code = 200, message = "Orders retrieved successfully"),
        @ApiResponse(code = 404, message = "User not found"),
        @ApiResponse(code = 401, message = "Unauthorized"),
        @ApiResponse(code = 403, message = "Forbidden")
    })
    public ResponseEntity<PageResponse<OrderResponse>> getUserOrders(
            @PathVariable String id,
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size) {
        
        Page<Order> orders = userService.getUserOrders(id, PageRequest.of(page, size));
        
        List<OrderResponse> orderResponses = orders.getContent().stream()
            .map(orderMapper::toResponse)
            .collect(Collectors.toList());
        
        PageResponse<OrderResponse> response = PageResponse.<OrderResponse>builder()
            .data(orderResponses)
            .page(orders.getNumber())
            .size(orders.getSize())
            .totalElements(orders.getTotalElements())
            .totalPages(orders.getTotalPages())
            .hasNext(orders.hasNext())
            .hasPrevious(orders.hasPrevious())
            .build();
        
        return ResponseEntity.ok()
            .cacheControl(CacheControl.maxAge(Duration.ofMinutes(2)))
            .eTag(generateETag(response))
            .body(response);
    }
    
    private String generateETag(Object object) {
        try {
            String json = objectMapper.writeValueAsString(object);
            return DigestUtils.md5DigestAsHex(json.getBytes());
        } catch (Exception e) {
            return UUID.randomUUID().toString();
        }
    }
}
```

## 🔍 GraphQL API Implementation

### GraphQL Configuration
```java
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import graphql.schema.GraphQLSchema;
import graphql.schema.idl.RuntimeWiring;
import graphql.schema.idl.SchemaGenerator;
import graphql.schema.idl.SchemaParser;

@Configuration
@TuskConfig
public class GraphQLConfiguration {
    
    private final GraphQLAPIConfig graphqlConfig;
    
    public GraphQLConfiguration(GraphQLAPIConfig graphqlConfig) {
        this.graphqlConfig = graphqlConfig;
    }
    
    @Bean
    public GraphQLSchema graphQLSchema() {
        SchemaParser schemaParser = new SchemaParser();
        SchemaGenerator schemaGenerator = new SchemaGenerator();
        
        // Load schema from file
        String schema = loadSchemaFromFile();
        TypeDefinitionRegistry typeRegistry = schemaParser.parse(schema);
        
        // Build runtime wiring
        RuntimeWiring runtimeWiring = buildRuntimeWiring();
        
        return schemaGenerator.makeExecutableSchema(typeRegistry, runtimeWiring);
    }
    
    @Bean
    public GraphQL graphQL(GraphQLSchema schema) {
        return GraphQL.newGraphQL(schema)
            .instrumentation(new TracingInstrumentation())
            .instrumentation(new DataLoaderInstrumentation())
            .build();
    }
    
    private RuntimeWiring buildRuntimeWiring() {
        return RuntimeWiring.newRuntimeWiring()
            .type("Query", typeWiring -> typeWiring
                .dataFetcher("users", userDataFetcher())
                .dataFetcher("user", userByIdDataFetcher())
                .dataFetcher("orders", orderDataFetcher())
                .dataFetcher("order", orderByIdDataFetcher())
                .dataFetcher("products", productDataFetcher())
                .dataFetcher("product", productByIdDataFetcher()))
            .type("User", typeWiring -> typeWiring
                .dataFetcher("orders", userOrdersDataFetcher())
                .dataFetcher("profile", userProfileDataFetcher()))
            .type("Order", typeWiring -> typeWiring
                .dataFetcher("user", orderUserDataFetcher())
                .dataFetcher("items", orderItemsDataFetcher()))
            .type("Product", typeWiring -> typeWiring
                .dataFetcher("category", productCategoryDataFetcher())
                .dataFetcher("reviews", productReviewsDataFetcher()))
            .type("Mutation", typeWiring -> typeWiring
                .dataFetcher("createUser", createUserDataFetcher())
                .dataFetcher("updateUser", updateUserDataFetcher())
                .dataFetcher("deleteUser", deleteUserDataFetcher())
                .dataFetcher("createOrder", createOrderDataFetcher())
                .dataFetcher("updateOrder", updateOrderDataFetcher())
                .dataFetcher("deleteOrder", deleteOrderDataFetcher()))
            .build();
    }
    
    private String loadSchemaFromFile() {
        // Load GraphQL schema from file
        return """
            type Query {
                users(page: Int, size: Int, email: String, status: String): UserPage!
                user(id: ID!): User
                orders(page: Int, size: Int, userId: String, status: String): OrderPage!
                order(id: ID!): Order
                products(page: Int, size: Int, category: String, priceRange: PriceRange): ProductPage!
                product(id: ID!): Product
            }
            
            type Mutation {
                createUser(input: CreateUserInput!): User!
                updateUser(id: ID!, input: UpdateUserInput!): User!
                deleteUser(id: ID!): Boolean!
                createOrder(input: CreateOrderInput!): Order!
                updateOrder(id: ID!, input: UpdateOrderInput!): Order!
                deleteOrder(id: ID!): Boolean!
            }
            
            type Subscription {
                userCreated: User!
                userUpdated: User!
                orderCreated: Order!
                orderUpdated: Order!
            }
            
            type User {
                id: ID!
                email: String!
                firstName: String!
                lastName: String!
                status: UserStatus!
                createdAt: DateTime!
                updatedAt: DateTime!
                orders: [Order!]!
                profile: UserProfile
            }
            
            type UserProfile {
                id: ID!
                bio: String
                avatar: String
                preferences: JSON
            }
            
            type Order {
                id: ID!
                userId: String!
                user: User!
                items: [OrderItem!]!
                totalAmount: Decimal!
                status: OrderStatus!
                createdAt: DateTime!
                updatedAt: DateTime!
            }
            
            type OrderItem {
                id: ID!
                productId: String!
                product: Product!
                quantity: Int!
                price: Decimal!
            }
            
            type Product {
                id: ID!
                name: String!
                description: String
                price: Decimal!
                stock: Int!
                category: Category!
                reviews: [Review!]!
                status: ProductStatus!
                createdAt: DateTime!
                updatedAt: DateTime!
            }
            
            type Category {
                id: ID!
                name: String!
                description: String
            }
            
            type Review {
                id: ID!
                userId: String!
                user: User!
                rating: Int!
                comment: String
                createdAt: DateTime!
            }
            
            type UserPage {
                data: [User!]!
                page: Int!
                size: Int!
                totalElements: Int!
                totalPages: Int!
                hasNext: Boolean!
                hasPrevious: Boolean!
            }
            
            type OrderPage {
                data: [Order!]!
                page: Int!
                size: Int!
                totalElements: Int!
                totalPages: Int!
                hasNext: Boolean!
                hasPrevious: Boolean!
            }
            
            type ProductPage {
                data: [Product!]!
                page: Int!
                size: Int!
                totalElements: Int!
                totalPages: Int!
                hasNext: Boolean!
                hasPrevious: Boolean!
            }
            
            input CreateUserInput {
                email: String!
                firstName: String!
                lastName: String!
                password: String!
            }
            
            input UpdateUserInput {
                firstName: String
                lastName: String
                status: UserStatus
            }
            
            input CreateOrderInput {
                userId: String!
                items: [CreateOrderItemInput!]!
            }
            
            input CreateOrderItemInput {
                productId: String!
                quantity: Int!
            }
            
            input UpdateOrderInput {
                status: OrderStatus
                items: [UpdateOrderItemInput!]
            }
            
            input UpdateOrderItemInput {
                id: ID!
                quantity: Int
            }
            
            input PriceRange {
                min: Decimal
                max: Decimal
            }
            
            enum UserStatus {
                ACTIVE
                INACTIVE
                SUSPENDED
                DELETED
            }
            
            enum OrderStatus {
                PENDING
                CONFIRMED
                SHIPPED
                DELIVERED
                CANCELLED
            }
            
            enum ProductStatus {
                ACTIVE
                INACTIVE
                OUT_OF_STOCK
                DISCONTINUED
            }
            
            scalar DateTime
            scalar Decimal
            scalar JSON
            """;
    }
}

@TuskConfig
public class GraphQLAPIConfig {
    
    private boolean enabled;
    private String path;
    private PlaygroundConfig playground;
    private IntrospectionConfig introspection;
    private SubscriptionsConfig subscriptions;
    private RateLimitingConfig rateLimiting;
    private CachingConfig caching;
    
    // Getters and setters
    public boolean isEnabled() { return enabled; }
    public void setEnabled(boolean enabled) { this.enabled = enabled; }
    
    public String getPath() { return path; }
    public void setPath(String path) { this.path = path; }
    
    public PlaygroundConfig getPlayground() { return playground; }
    public void setPlayground(PlaygroundConfig playground) { this.playground = playground; }
    
    public IntrospectionConfig getIntrospection() { return introspection; }
    public void setIntrospection(IntrospectionConfig introspection) { this.introspection = introspection; }
    
    public SubscriptionsConfig getSubscriptions() { return subscriptions; }
    public void setSubscriptions(SubscriptionsConfig subscriptions) { this.subscriptions = subscriptions; }
    
    public RateLimitingConfig getRateLimiting() { return rateLimiting; }
    public void setRateLimiting(RateLimitingConfig rateLimiting) { this.rateLimiting = rateLimiting; }
    
    public CachingConfig getCaching() { return caching; }
    public void setCaching(CachingConfig caching) { this.caching = caching; }
}
```

### GraphQL Data Fetchers
```java
import org.springframework.stereotype.Component;
import graphql.schema.DataFetcher;
import graphql.schema.DataFetchingEnvironment;
import java.util.List;
import java.util.Map;

@Component
public class UserDataFetcher {
    
    private final UserService userService;
    private final UserMapper userMapper;
    
    public UserDataFetcher(UserService userService, UserMapper userMapper) {
        this.userService = userService;
        this.userMapper = userMapper;
    }
    
    public DataFetcher<List<User>> getUsers() {
        return environment -> {
            // Extract arguments
            Integer page = environment.getArgument("page");
            Integer size = environment.getArgument("size");
            String email = environment.getArgument("email");
            String status = environment.getArgument("status");
            
            // Build filter criteria
            UserFilterCriteria criteria = UserFilterCriteria.builder()
                .email(email)
                .status(status)
                .build();
            
            // Get users
            Page<User> users = userService.getUsers(criteria, 
                PageRequest.of(page != null ? page : 0, size != null ? size : 20));
            
            return users.getContent();
        };
    }
    
    public DataFetcher<User> getUserById() {
        return environment -> {
            String id = environment.getArgument("id");
            return userService.getUserById(id);
        };
    }
    
    public DataFetcher<List<Order>> getUserOrders() {
        return environment -> {
            User user = environment.getSource();
            return userService.getUserOrders(user.getId(), PageRequest.of(0, 100)).getContent();
        };
    }
    
    public DataFetcher<UserProfile> getUserProfile() {
        return environment -> {
            User user = environment.getSource();
            return userService.getUserProfile(user.getId());
        };
    }
    
    public DataFetcher<User> createUser() {
        return environment -> {
            Map<String, Object> input = environment.getArgument("input");
            
            CreateUserRequest request = CreateUserRequest.builder()
                .email((String) input.get("email"))
                .firstName((String) input.get("firstName"))
                .lastName((String) input.get("lastName"))
                .password((String) input.get("password"))
                .build();
            
            User user = userMapper.toEntity(request);
            return userService.createUser(user);
        };
    }
    
    public DataFetcher<User> updateUser() {
        return environment -> {
            String id = environment.getArgument("id");
            Map<String, Object> input = environment.getArgument("input");
            
            UpdateUserRequest request = UpdateUserRequest.builder()
                .firstName((String) input.get("firstName"))
                .lastName((String) input.get("lastName"))
                .status(input.get("status") != null ? 
                    UserStatus.valueOf((String) input.get("status")) : null)
                .build();
            
            User user = userMapper.toEntity(request);
            user.setId(id);
            return userService.updateUser(user);
        };
    }
    
    public DataFetcher<Boolean> deleteUser() {
        return environment -> {
            String id = environment.getArgument("id");
            userService.deleteUser(id);
            return true;
        };
    }
}

@Component
public class OrderDataFetcher {
    
    private final OrderService orderService;
    private final OrderMapper orderMapper;
    private final UserService userService;
    
    public OrderDataFetcher(OrderService orderService, 
                           OrderMapper orderMapper,
                           UserService userService) {
        this.orderService = orderService;
        this.orderMapper = orderMapper;
        this.userService = userService;
    }
    
    public DataFetcher<List<Order>> getOrders() {
        return environment -> {
            Integer page = environment.getArgument("page");
            Integer size = environment.getArgument("size");
            String userId = environment.getArgument("userId");
            String status = environment.getArgument("status");
            
            OrderFilterCriteria criteria = OrderFilterCriteria.builder()
                .userId(userId)
                .status(status)
                .build();
            
            Page<Order> orders = orderService.getOrders(criteria, 
                PageRequest.of(page != null ? page : 0, size != null ? size : 20));
            
            return orders.getContent();
        };
    }
    
    public DataFetcher<Order> getOrderById() {
        return environment -> {
            String id = environment.getArgument("id");
            return orderService.getOrderById(id);
        };
    }
    
    public DataFetcher<User> getOrderUser() {
        return environment -> {
            Order order = environment.getSource();
            return userService.getUserById(order.getUserId());
        };
    }
    
    public DataFetcher<List<OrderItem>> getOrderItems() {
        return environment -> {
            Order order = environment.getSource();
            return orderService.getOrderItems(order.getId());
        };
    }
    
    public DataFetcher<Order> createOrder() {
        return environment -> {
            Map<String, Object> input = environment.getArgument("input");
            
            CreateOrderRequest request = CreateOrderRequest.builder()
                .userId((String) input.get("userId"))
                .items(parseOrderItems((List<Map<String, Object>>) input.get("items")))
                .build();
            
            Order order = orderMapper.toEntity(request);
            return orderService.createOrder(order);
        };
    }
    
    private List<CreateOrderItemRequest> parseOrderItems(List<Map<String, Object>> items) {
        return items.stream()
            .map(item -> CreateOrderItemRequest.builder()
                .productId((String) item.get("productId"))
                .quantity((Integer) item.get("quantity"))
                .build())
            .collect(Collectors.toList());
    }
}
```

## 🔧 Best Practices

### 1. RESTful API Design
- Use proper HTTP methods and status codes
- Implement consistent error handling
- Use pagination for large datasets
- Implement proper caching strategies

### 2. GraphQL Design
- Design schema for specific use cases
- Use DataLoader for N+1 query prevention
- Implement proper error handling
- Use subscriptions for real-time updates

### 3. API Versioning
- Use URL path versioning
- Maintain backward compatibility
- Provide migration guides
- Deprecate old versions gracefully

### 4. Security
- Implement proper authentication
- Use role-based authorization
- Validate all inputs
- Rate limit API requests

### 5. Documentation
- Use OpenAPI for REST documentation
- Provide GraphQL playground
- Include examples and schemas
- Maintain up-to-date documentation

## 🎯 Summary

TuskLang Java API design provides:

- **RESTful APIs**: Standard REST endpoints with pagination and filtering
- **GraphQL Support**: Flexible query language with subscriptions
- **API Versioning**: Proper versioning strategy with backward compatibility
- **Comprehensive Documentation**: OpenAPI and GraphQL playground
- **Security**: Authentication, authorization, and rate limiting

The combination of TuskLang's executable configuration with Java's API design capabilities creates a powerful platform for building scalable, maintainable, and well-documented APIs.

**"We don't bow to any king" - Build APIs that developers love to use!** 
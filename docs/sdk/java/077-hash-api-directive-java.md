# #api - API Directive (Java)

The `#api` directive provides enterprise-grade REST API capabilities for Java applications, enabling powerful API endpoints with Spring Boot integration and comprehensive request/response handling.

## Basic Syntax

```tusk
# Basic API endpoint
#api /users {
    users: @get_all_users()
    return {users: users}
}

# API with parameters
#api /users/{id} {
    user: @get_user(@params.id)
    return {user: user}
}

# API with query parameters
#api /search {
    query: @request.query.q
    results: @search_users(query)
    return {results: results, query: query}
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.ApiDirective;
import org.springframework.web.bind.annotation.*;
import org.springframework.stereotype.Controller;
import org.springframework.http.ResponseEntity;
import org.springframework.http.HttpStatus;

@RestController
public class ApiController {
    
    private final TuskLang tuskLang;
    private final ApiDirective apiDirective;
    private final UserService userService;
    private final SearchService searchService;
    
    public ApiController(TuskLang tuskLang, 
                        UserService userService,
                        SearchService searchService) {
        this.tuskLang = tuskLang;
        this.apiDirective = new ApiDirective();
        this.userService = userService;
        this.searchService = searchService;
    }
    
    // Basic API endpoint
    @GetMapping("/api/users")
    public ResponseEntity<Map<String, Object>> getAllUsers() {
        List<User> users = userService.getAllUsers();
        Map<String, Object> response = new HashMap<>();
        response.put("users", users);
        return ResponseEntity.ok(response);
    }
    
    // API with path parameters
    @GetMapping("/api/users/{id}")
    public ResponseEntity<Map<String, Object>> getUser(@PathVariable Long id) {
        User user = userService.getUserById(id);
        Map<String, Object> response = new HashMap<>();
        response.put("user", user);
        return ResponseEntity.ok(response);
    }
    
    // API with query parameters
    @GetMapping("/api/search")
    public ResponseEntity<Map<String, Object>> searchUsers(@RequestParam String q) {
        List<User> results = searchService.searchUsers(q);
        Map<String, Object> response = new HashMap<>();
        response.put("results", results);
        response.put("query", q);
        return ResponseEntity.ok(response);
    }
}
```

## API Configuration

```tusk
# Detailed API configuration
#api {
    route: "/api/data"
    method: "GET"
    auth: true
    rate_limit: 100
    cache: true
    cache_ttl: 300
} {
    data: @get_data()
    return {data: data}
}

# API with middleware
#api {
    route: "/api/protected"
    method: "POST"
    middleware: ["auth", "validation", "logging"]
    schema: "user_schema"
} {
    user_data: @request.body
    @validate_schema(user_data, schema)
    result: @create_user(user_data)
    return {result: result}
}

# API with conditions
#api {
    route: "/api/conditional"
    method: "GET"
    condition: @is_production()
    fallback: "/api/fallback"
} {
    data: @get_production_data()
    return {data: data}
}
```

## Java API Configuration

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import org.springframework.web.bind.annotation.*;
import java.util.Map;
import java.util.List;

@Component
@ConfigurationProperties(prefix = "tusk.api")
public class ApiConfig {
    
    private String defaultMethod = "GET";
    private boolean defaultAuth = false;
    private int defaultRateLimit = 1000;
    private boolean defaultCache = false;
    private int defaultCacheTtl = 300;
    
    private Map<String, ApiEndpointDefinition> endpoints;
    private List<String> globalMiddleware;
    private Map<String, String> schemas;
    
    // Getters and setters
    public String getDefaultMethod() { return defaultMethod; }
    public void setDefaultMethod(String defaultMethod) { this.defaultMethod = defaultMethod; }
    
    public boolean isDefaultAuth() { return defaultAuth; }
    public void setDefaultAuth(boolean defaultAuth) { this.defaultAuth = defaultAuth; }
    
    public int getDefaultRateLimit() { return defaultRateLimit; }
    public void setDefaultRateLimit(int defaultRateLimit) { this.defaultRateLimit = defaultRateLimit; }
    
    public boolean isDefaultCache() { return defaultCache; }
    public void setDefaultCache(boolean defaultCache) { this.defaultCache = defaultCache; }
    
    public int getDefaultCacheTtl() { return defaultCacheTtl; }
    public void setDefaultCacheTtl(int defaultCacheTtl) { this.defaultCacheTtl = defaultCacheTtl; }
    
    public Map<String, ApiEndpointDefinition> getEndpoints() { return endpoints; }
    public void setEndpoints(Map<String, ApiEndpointDefinition> endpoints) { this.endpoints = endpoints; }
    
    public List<String> getGlobalMiddleware() { return globalMiddleware; }
    public void setGlobalMiddleware(List<String> globalMiddleware) { this.globalMiddleware = globalMiddleware; }
    
    public Map<String, String> getSchemas() { return schemas; }
    public void setSchemas(Map<String, String> schemas) { this.schemas = schemas; }
    
    public static class ApiEndpointDefinition {
        private String route;
        private String method = "GET";
        private boolean auth;
        private int rateLimit;
        private boolean cache;
        private int cacheTtl;
        private List<String> middleware;
        private String schema;
        private String condition;
        private String fallback;
        
        // Getters and setters
        public String getRoute() { return route; }
        public void setRoute(String route) { this.route = route; }
        
        public String getMethod() { return method; }
        public void setMethod(String method) { this.method = method; }
        
        public boolean isAuth() { return auth; }
        public void setAuth(boolean auth) { this.auth = auth; }
        
        public int getRateLimit() { return rateLimit; }
        public void setRateLimit(int rateLimit) { this.rateLimit = rateLimit; }
        
        public boolean isCache() { return cache; }
        public void setCache(boolean cache) { this.cache = cache; }
        
        public int getCacheTtl() { return cacheTtl; }
        public void setCacheTtl(int cacheTtl) { this.cacheTtl = cacheTtl; }
        
        public List<String> getMiddleware() { return middleware; }
        public void setMiddleware(List<String> middleware) { this.middleware = middleware; }
        
        public String getSchema() { return schema; }
        public void setSchema(String schema) { this.schema = schema; }
        
        public String getCondition() { return condition; }
        public void setCondition(String condition) { this.condition = condition; }
        
        public String getFallback() { return fallback; }
        public void setFallback(String fallback) { this.fallback = fallback; }
    }
}

@RestController
@RequestMapping("/api")
public class ConfiguredApiController {
    
    private final ApiConfig config;
    private final DataService dataService;
    private final UserService userService;
    private final ValidationService validationService;
    
    public ConfiguredApiController(ApiConfig config,
                                 DataService dataService,
                                 UserService userService,
                                 ValidationService validationService) {
        this.config = config;
        this.dataService = dataService;
        this.userService = userService;
        this.validationService = validationService;
    }
    
    @GetMapping("/data")
    public ResponseEntity<Map<String, Object>> getData() {
        ApiConfig.ApiEndpointDefinition endpoint = config.getEndpoints().get("data");
        
        // Check authentication if required
        if (endpoint.isAuth() && !isAuthenticated()) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build();
        }
        
        // Get data
        Object data = dataService.getData();
        Map<String, Object> response = new HashMap<>();
        response.put("data", data);
        
        return ResponseEntity.ok(response);
    }
    
    @PostMapping("/protected")
    public ResponseEntity<Map<String, Object>> createUser(@RequestBody Map<String, Object> userData) {
        ApiConfig.ApiEndpointDefinition endpoint = config.getEndpoints().get("protected");
        
        try {
            // Validate schema if specified
            if (endpoint.getSchema() != null) {
                validationService.validateSchema(userData, endpoint.getSchema());
            }
            
            // Create user
            User user = userService.createUser(userData);
            Map<String, Object> response = new HashMap<>();
            response.put("result", user);
            
            return ResponseEntity.ok(response);
        } catch (ValidationException e) {
            return ResponseEntity.badRequest().body(Map.of("error", e.getMessage()));
        }
    }
    
    @GetMapping("/conditional")
    public ResponseEntity<Map<String, Object>> getConditionalData() {
        ApiConfig.ApiEndpointDefinition endpoint = config.getEndpoints().get("conditional");
        
        // Check condition
        if (endpoint.getCondition() != null && !evaluateCondition(endpoint.getCondition())) {
            // Redirect to fallback if specified
            if (endpoint.getFallback() != null) {
                return ResponseEntity.status(HttpStatus.FOUND)
                    .header("Location", endpoint.getFallback())
                    .build();
            }
            return ResponseEntity.notFound().build();
        }
        
        Object data = dataService.getProductionData();
        Map<String, Object> response = new HashMap<>();
        response.put("data", data);
        
        return ResponseEntity.ok(response);
    }
    
    private boolean isAuthenticated() {
        Authentication auth = SecurityContextHolder.getContext().getAuthentication();
        return auth != null && auth.isAuthenticated();
    }
    
    private boolean evaluateCondition(String condition) {
        // Simple condition evaluation
        return "production".equals(System.getProperty("spring.profiles.active"));
    }
}
```

## CRUD Operations

```tusk
# Create operation
#api /users method: "POST" {
    user_data: @request.body
    @validate_user_data(user_data)
    new_user: @create_user(user_data)
    return {user: new_user, status: "created"}
}

# Read operation
#api /users/{id} method: "GET" {
    user: @get_user(@params.id)
    if (!user) {
        @return_error(404, "User not found")
    }
    return {user: user}
}

# Update operation
#api /users/{id} method: "PUT" {
    user_data: @request.body
    @validate_user_data(user_data)
    updated_user: @update_user(@params.id, user_data)
    return {user: updated_user, status: "updated"}
}

# Delete operation
#api /users/{id} method: "DELETE" {
    @delete_user(@params.id)
    return {status: "deleted"}
}
```

## Java CRUD Operations

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.http.ResponseEntity;
import org.springframework.http.HttpStatus;
import javax.validation.Valid;

@RestController
@RequestMapping("/api/users")
public class UserApiController {
    
    private final UserService userService;
    private final ValidationService validationService;
    
    public UserApiController(UserService userService, ValidationService validationService) {
        this.userService = userService;
        this.validationService = validationService;
    }
    
    // Create operation
    @PostMapping
    public ResponseEntity<Map<String, Object>> createUser(@Valid @RequestBody UserCreateRequest userData) {
        try {
            // Validate user data
            validationService.validateUserData(userData);
            
            // Create user
            User newUser = userService.createUser(userData);
            
            Map<String, Object> response = new HashMap<>();
            response.put("user", newUser);
            response.put("status", "created");
            
            return ResponseEntity.status(HttpStatus.CREATED).body(response);
        } catch (ValidationException e) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("error", e.getMessage());
            return ResponseEntity.badRequest().body(errorResponse);
        }
    }
    
    // Read operation
    @GetMapping("/{id}")
    public ResponseEntity<Map<String, Object>> getUser(@PathVariable Long id) {
        try {
            User user = userService.getUserById(id);
            
            Map<String, Object> response = new HashMap<>();
            response.put("user", user);
            
            return ResponseEntity.ok(response);
        } catch (UserNotFoundException e) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("error", "User not found");
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(errorResponse);
        }
    }
    
    // Update operation
    @PutMapping("/{id}")
    public ResponseEntity<Map<String, Object>> updateUser(@PathVariable Long id,
                                                         @Valid @RequestBody UserUpdateRequest userData) {
        try {
            // Validate user data
            validationService.validateUserData(userData);
            
            // Update user
            User updatedUser = userService.updateUser(id, userData);
            
            Map<String, Object> response = new HashMap<>();
            response.put("user", updatedUser);
            response.put("status", "updated");
            
            return ResponseEntity.ok(response);
        } catch (ValidationException e) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("error", e.getMessage());
            return ResponseEntity.badRequest().body(errorResponse);
        } catch (UserNotFoundException e) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("error", "User not found");
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(errorResponse);
        }
    }
    
    // Delete operation
    @DeleteMapping("/{id}")
    public ResponseEntity<Map<String, Object>> deleteUser(@PathVariable Long id) {
        try {
            userService.deleteUser(id);
            
            Map<String, Object> response = new HashMap<>();
            response.put("status", "deleted");
            
            return ResponseEntity.ok(response);
        } catch (UserNotFoundException e) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("error", "User not found");
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(errorResponse);
        }
    }
}

@Service
public class UserService {
    
    private final UserRepository userRepository;
    
    public UserService(UserRepository userRepository) {
        this.userRepository = userRepository;
    }
    
    public User createUser(UserCreateRequest userData) {
        User user = new User();
        user.setName(userData.getName());
        user.setEmail(userData.getEmail());
        user.setCreatedAt(LocalDateTime.now());
        
        return userRepository.save(user);
    }
    
    public User getUserById(Long id) {
        return userRepository.findById(id)
            .orElseThrow(() -> new UserNotFoundException("User not found with id: " + id));
    }
    
    public User updateUser(Long id, UserUpdateRequest userData) {
        User user = getUserById(id);
        
        if (userData.getName() != null) {
            user.setName(userData.getName());
        }
        if (userData.getEmail() != null) {
            user.setEmail(userData.getEmail());
        }
        
        user.setUpdatedAt(LocalDateTime.now());
        return userRepository.save(user);
    }
    
    public void deleteUser(Long id) {
        User user = getUserById(id);
        userRepository.delete(user);
    }
    
    public List<User> getAllUsers() {
        return userRepository.findAll();
    }
}
```

## API Response Handling

```tusk
# Standard response format
#api /standard-response {
    data: @get_data()
    return {
        success: true,
        data: data,
        timestamp: @date.now(),
        version: "1.0.0"
    }
}

# Error response handling
#api /error-handling {
    try {
        data: @get_risky_data()
        return {success: true, data: data}
    } catch (error) {
        return {
            success: false,
            error: error.message,
            code: error.code || 500
        }
    }
}

# Paginated response
#api /paginated {
    page: @request.query.page || 1
    limit: @request.query.limit || 10
    offset: (page - 1) * limit
    
    data: @get_paginated_data(offset, limit)
    total: @get_total_count()
    
    return {
        success: true,
        data: data,
        pagination: {
            page: page,
            limit: limit,
            total: total,
            pages: Math.ceil(total / limit)
        }
    }
}
```

## Java API Response Handling

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.http.ResponseEntity;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;

@RestController
@RequestMapping("/api")
public class ResponseHandlingController {
    
    private final DataService dataService;
    private final RiskyDataService riskyDataService;
    
    public ResponseHandlingController(DataService dataService, RiskyDataService riskyDataService) {
        this.dataService = dataService;
        this.riskyDataService = riskyDataService;
    }
    
    @GetMapping("/standard-response")
    public ResponseEntity<ApiResponse<Object>> getStandardResponse() {
        Object data = dataService.getData();
        
        ApiResponse<Object> response = ApiResponse.<Object>builder()
            .success(true)
            .data(data)
            .timestamp(LocalDateTime.now())
            .version("1.0.0")
            .build();
        
        return ResponseEntity.ok(response);
    }
    
    @GetMapping("/error-handling")
    public ResponseEntity<ApiResponse<Object>> getErrorHandling() {
        try {
            Object data = riskyDataService.getRiskyData();
            
            ApiResponse<Object> response = ApiResponse.<Object>builder()
                .success(true)
                .data(data)
                .build();
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            ApiResponse<Object> response = ApiResponse.<Object>builder()
                .success(false)
                .error(e.getMessage())
                .code(500)
                .build();
            
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(response);
        }
    }
    
    @GetMapping("/paginated")
    public ResponseEntity<ApiResponse<Page<Object>>> getPaginatedData(
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "10") int limit) {
        
        Pageable pageable = PageRequest.of(page - 1, limit);
        Page<Object> data = dataService.getPaginatedData(pageable);
        
        PaginationInfo pagination = PaginationInfo.builder()
            .page(page)
            .limit(limit)
            .total(data.getTotalElements())
            .pages((int) Math.ceil((double) data.getTotalElements() / limit))
            .build();
        
        ApiResponse<Page<Object>> response = ApiResponse.<Page<Object>>builder()
            .success(true)
            .data(data)
            .pagination(pagination)
            .build();
        
        return ResponseEntity.ok(response);
    }
}

@Data
@Builder
public class ApiResponse<T> {
    private boolean success;
    private T data;
    private String error;
    private int code;
    private LocalDateTime timestamp;
    private String version;
    private PaginationInfo pagination;
}

@Data
@Builder
public class PaginationInfo {
    private int page;
    private int limit;
    private long total;
    private int pages;
}

@Service
public class DataService {
    
    private final DataRepository dataRepository;
    
    public DataService(DataRepository dataRepository) {
        this.dataRepository = dataRepository;
    }
    
    public Object getData() {
        return dataRepository.findAll();
    }
    
    public Page<Object> getPaginatedData(Pageable pageable) {
        return dataRepository.findAll(pageable);
    }
}

@Service
public class RiskyDataService {
    
    public Object getRiskyData() {
        // Simulate risky operation
        if (Math.random() < 0.3) {
            throw new RuntimeException("Random error occurred");
        }
        return "Risky data";
    }
}
```

## API Authentication

```tusk
# API with authentication
#api {
    route: "/api/protected"
    method: "GET"
    auth: true
    roles: ["user", "admin"]
} {
    user: @auth.user
    data: @get_user_data(user.id)
    return {data: data}
}

# API with API key
#api {
    route: "/api/external"
    method: "POST"
    auth: "api_key"
    rate_limit: 100
} {
    api_key: @request.headers.X-API-Key
    @validate_api_key(api_key)
    data: @process_external_request(@request.body)
    return {result: data}
}

# API with JWT
#api {
    route: "/api/jwt-protected"
    method: "GET"
    auth: "jwt"
    claims: ["user_id", "role"]
} {
    user_id: @auth.claims.user_id
    role: @auth.claims.role
    data: @get_jwt_protected_data(user_id, role)
    return {data: data}
}
```

## Java API Authentication

```java
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api")
public class AuthenticatedApiController {
    
    private final UserService userService;
    private final ExternalApiService externalApiService;
    private final JwtProtectedService jwtProtectedService;
    private final ApiKeyService apiKeyService;
    
    public AuthenticatedApiController(UserService userService,
                                    ExternalApiService externalApiService,
                                    JwtProtectedService jwtProtectedService,
                                    ApiKeyService apiKeyService) {
        this.userService = userService;
        this.externalApiService = externalApiService;
        this.jwtProtectedService = jwtProtectedService;
        this.apiKeyService = apiKeyService;
    }
    
    @GetMapping("/protected")
    @PreAuthorize("hasAnyRole('USER', 'ADMIN')")
    public ResponseEntity<Map<String, Object>> getProtectedData(@AuthenticationPrincipal User user) {
        Object data = userService.getUserData(user.getId());
        
        Map<String, Object> response = new HashMap<>();
        response.put("data", data);
        
        return ResponseEntity.ok(response);
    }
    
    @PostMapping("/external")
    public ResponseEntity<Map<String, Object>> processExternalRequest(
            @RequestBody Map<String, Object> requestBody,
            @RequestHeader("X-API-Key") String apiKey) {
        
        try {
            // Validate API key
            apiKeyService.validateApiKey(apiKey);
            
            // Process request
            Object result = externalApiService.processExternalRequest(requestBody);
            
            Map<String, Object> response = new HashMap<>();
            response.put("result", result);
            
            return ResponseEntity.ok(response);
        } catch (InvalidApiKeyException e) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("error", "Invalid API key");
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(errorResponse);
        }
    }
    
    @GetMapping("/jwt-protected")
    public ResponseEntity<Map<String, Object>> getJwtProtectedData(@AuthenticationPrincipal JwtUser jwtUser) {
        Object data = jwtProtectedService.getJwtProtectedData(jwtUser.getUserId(), jwtUser.getRole());
        
        Map<String, Object> response = new HashMap<>();
        response.put("data", data);
        
        return ResponseEntity.ok(response);
    }
}

@Service
public class ApiKeyService {
    
    private final ApiKeyRepository apiKeyRepository;
    
    public ApiKeyService(ApiKeyRepository apiKeyRepository) {
        this.apiKeyRepository = apiKeyRepository;
    }
    
    public void validateApiKey(String apiKey) {
        ApiKey key = apiKeyRepository.findByKeyAndActiveTrue(apiKey);
        if (key == null) {
            throw new InvalidApiKeyException("Invalid API key");
        }
        
        if (key.getExpiresAt() != null && key.getExpiresAt().isBefore(LocalDateTime.now())) {
            throw new InvalidApiKeyException("API key expired");
        }
    }
}

@Service
public class JwtProtectedService {
    
    public Object getJwtProtectedData(Long userId, String role) {
        // Process JWT protected data based on user ID and role
        return "JWT protected data for user " + userId + " with role " + role;
    }
}
```

## API Rate Limiting

```tusk
# API with rate limiting
#api {
    route: "/api/rate-limited"
    method: "GET"
    rate_limit: 100
    rate_window: 3600
} {
    data: @get_rate_limited_data()
    return {data: data}
}

# API with user-based rate limiting
#api {
    route: "/api/user-limited"
    method: "POST"
    rate_limit: {
        user: 50,
        guest: 10
    }
    rate_window: 3600
} {
    user_type: @auth.user ? "user" : "guest"
    data: @process_user_limited_request(@request.body)
    return {data: data}
}
```

## Java API Rate Limiting

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.web.servlet.HandlerInterceptor;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

@RestController
@RequestMapping("/api")
public class RateLimitedApiController {
    
    private final RateLimitService rateLimitService;
    private final DataService dataService;
    
    public RateLimitedApiController(RateLimitService rateLimitService, DataService dataService) {
        this.rateLimitService = rateLimitService;
        this.dataService = dataService;
    }
    
    @GetMapping("/rate-limited")
    public ResponseEntity<Map<String, Object>> getRateLimitedData(HttpServletRequest request) {
        String clientId = getClientId(request);
        
        if (!rateLimitService.checkLimit(clientId, 100, 3600)) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("error", "Rate limit exceeded");
            return ResponseEntity.status(HttpStatus.TOO_MANY_REQUESTS).body(errorResponse);
        }
        
        Object data = dataService.getRateLimitedData();
        Map<String, Object> response = new HashMap<>();
        response.put("data", data);
        
        return ResponseEntity.ok(response);
    }
    
    @PostMapping("/user-limited")
    public ResponseEntity<Map<String, Object>> processUserLimitedRequest(
            @RequestBody Map<String, Object> requestBody,
            @AuthenticationPrincipal User user,
            HttpServletRequest request) {
        
        String userType = user != null ? "user" : "guest";
        int limit = "user".equals(userType) ? 50 : 10;
        
        String clientId = getClientId(request);
        
        if (!rateLimitService.checkLimit(clientId, limit, 3600)) {
            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("error", "Rate limit exceeded for " + userType);
            return ResponseEntity.status(HttpStatus.TOO_MANY_REQUESTS).body(errorResponse);
        }
        
        Object data = dataService.processUserLimitedRequest(requestBody);
        Map<String, Object> response = new HashMap<>();
        response.put("data", data);
        
        return ResponseEntity.ok(response);
    }
    
    private String getClientId(HttpServletRequest request) {
        Authentication auth = SecurityContextHolder.getContext().getAuthentication();
        if (auth != null && auth.isAuthenticated()) {
            User user = (User) auth.getPrincipal();
            return "user:" + user.getId();
        }
        return "guest:" + request.getRemoteAddr();
    }
}

@Service
public class RateLimitService {
    
    private final RedisTemplate<String, String> redisTemplate;
    
    public RateLimitService(RedisTemplate<String, String> redisTemplate) {
        this.redisTemplate = redisTemplate;
    }
    
    public boolean checkLimit(String clientId, int limit, int window) {
        String key = "rate_limit:" + clientId;
        
        String current = redisTemplate.opsForValue().get(key);
        int currentCount = current != null ? Integer.parseInt(current) : 0;
        
        if (currentCount >= limit) {
            return false;
        }
        
        redisTemplate.opsForValue().increment(key);
        redisTemplate.expire(key, Duration.ofSeconds(window));
        
        return true;
    }
}
```

## API Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.context.TestPropertySource;

@WebMvcTest(ApiController.class)
@TestPropertySource(properties = {
    "tusk.api.default-method=GET",
    "tusk.api.default-auth=false"
})
public class ApiControllerTest {
    
    @Autowired
    private MockMvc mockMvc;
    
    @MockBean
    private UserService userService;
    
    @MockBean
    private SearchService searchService;
    
    @Test
    public void testGetAllUsers() throws Exception {
        List<User> users = Arrays.asList(
            new User(1L, "User 1"),
            new User(2L, "User 2")
        );
        
        when(userService.getAllUsers()).thenReturn(users);
        
        mockMvc.perform(get("/api/users"))
               .andExpect(status().isOk())
               .andExpect(jsonPath("$.users").isArray())
               .andExpect(jsonPath("$.users", hasSize(2)))
               .andExpect(jsonPath("$.users[0].name").value("User 1"));
    }
    
    @Test
    public void testGetUser() throws Exception {
        User user = new User(1L, "Test User");
        
        when(userService.getUserById(1L)).thenReturn(user);
        
        mockMvc.perform(get("/api/users/1"))
               .andExpect(status().isOk())
               .andExpect(jsonPath("$.user.name").value("Test User"));
    }
    
    @Test
    public void testGetUserNotFound() throws Exception {
        when(userService.getUserById(999L)).thenThrow(new UserNotFoundException("User not found"));
        
        mockMvc.perform(get("/api/users/999"))
               .andExpect(status().isNotFound())
               .andExpect(jsonPath("$.error").value("User not found"));
    }
    
    @Test
    public void testSearchUsers() throws Exception {
        List<User> results = Arrays.asList(
            new User(1L, "John Doe"),
            new User(2L, "Jane Smith")
        );
        
        when(searchService.searchUsers("john")).thenReturn(results);
        
        mockMvc.perform(get("/api/search").param("q", "john"))
               .andExpect(status().isOk())
               .andExpect(jsonPath("$.results").isArray())
               .andExpect(jsonPath("$.query").value("john"))
               .andExpect(jsonPath("$.results", hasSize(2)));
    }
    
    @Test
    public void testCreateUser() throws Exception {
        UserCreateRequest request = new UserCreateRequest("New User", "new@example.com");
        User createdUser = new User(1L, "New User");
        
        when(userService.createUser(request)).thenReturn(createdUser);
        
        mockMvc.perform(post("/api/users")
               .contentType(MediaType.APPLICATION_JSON)
               .content("{\"name\":\"New User\",\"email\":\"new@example.com\"}"))
               .andExpect(status().isCreated())
               .andExpect(jsonPath("$.user.name").value("New User"))
               .andExpect(jsonPath("$.status").value("created"));
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  api:
    default-method: "GET"
    default-auth: false
    default-rate-limit: 1000
    default-cache: false
    default-cache-ttl: 300
    
    endpoints:
      users:
        route: "/api/users"
        method: "GET"
        auth: false
        cache: true
        cache-ttl: 300
      
      protected:
        route: "/api/protected"
        method: "GET"
        auth: true
        middleware: ["auth", "validation"]
        roles: ["user", "admin"]
      
      external:
        route: "/api/external"
        method: "POST"
        auth: "api_key"
        rate-limit: 100
        middleware: ["api_key_validation"]
      
      rate-limited:
        route: "/api/rate-limited"
        method: "GET"
        rate-limit: 100
        rate-window: 3600
        cache: true
    
    global-middleware: ["logging", "cors"]
    
    schemas:
      user_schema: "schemas/user.json"
      product_schema: "schemas/product.json"

spring:
  security:
    oauth2:
      resourceserver:
        jwt:
          issuer-uri: https://auth.example.com
          jwk-set-uri: https://auth.example.com/.well-known/jwks.json
```

## Summary

The `#api` directive in TuskLang provides comprehensive REST API capabilities for Java applications. With Spring Boot integration, flexible configuration, authentication, rate limiting, and comprehensive testing support, you can implement sophisticated API endpoints that enhance your application's functionality.

Key features include:
- **Multiple API patterns**: CRUD operations, response handling, authentication
- **Spring Boot integration**: Seamless integration with Spring Boot REST framework
- **Flexible configuration**: Configurable endpoints with middleware and conditions
- **Authentication support**: Multiple authentication methods (JWT, API key, roles)
- **Rate limiting**: Built-in rate limiting capabilities
- **Response handling**: Standardized response formats and error handling
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade API capabilities that integrate seamlessly with Spring Boot applications while maintaining the simplicity and power of TuskLang's declarative syntax. 
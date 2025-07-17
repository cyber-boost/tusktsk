# #middleware - Middleware Directive (Java)

The `#middleware` directive provides enterprise-grade middleware capabilities for Java applications, enabling request/response processing, logging, validation, and custom business logic with Spring Boot integration.

## Basic Syntax

```tusk
# Basic middleware
#middleware {
    #api /endpoint {
        return @process_request()
    }
}

# Middleware with custom logic
#middleware {
    before: @log_request(@request)
    after: @log_response(@response)
} {
    #api /logged-endpoint {
        return @process_request()
    }
}

# Conditional middleware
#middleware if: @request.method == "POST" {
    #api /conditional-endpoint {
        return @process_post_request()
    }
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.MiddlewareDirective;
import org.springframework.web.bind.annotation.*;
import org.springframework.stereotype.Controller;
import org.springframework.web.servlet.HandlerInterceptor;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

@Controller
public class MiddlewareController {
    
    private final TuskLang tuskLang;
    private final MiddlewareDirective middlewareDirective;
    private final LoggingService loggingService;
    
    public MiddlewareController(TuskLang tuskLang, LoggingService loggingService) {
        this.tuskLang = tuskLang;
        this.middlewareDirective = new MiddlewareDirective();
        this.loggingService = loggingService;
    }
    
    // Basic middleware with Spring interceptors
    @GetMapping("/api/endpoint")
    public ResponseEntity<DataResponse> getData() {
        return ResponseEntity.ok(dataService.getData());
    }
    
    // Middleware with custom logic
    @PostMapping("/api/logged-endpoint")
    public ResponseEntity<DataResponse> getLoggedData(HttpServletRequest request,
                                                    HttpServletResponse response) {
        // Before middleware
        loggingService.logRequest(request);
        
        DataResponse data = dataService.getData();
        
        // After middleware
        loggingService.logResponse(response, data);
        
        return ResponseEntity.ok(data);
    }
    
    // Conditional middleware
    @RequestMapping(value = "/api/conditional-endpoint", method = {RequestMethod.GET, RequestMethod.POST})
    public ResponseEntity<DataResponse> getConditionalData(HttpServletRequest request) {
        if ("POST".equals(request.getMethod())) {
            // Apply POST-specific middleware
            validationService.validatePostRequest(request);
        }
        
        return ResponseEntity.ok(dataService.getData());
    }
}
```

## Middleware Configuration

```tusk
# Detailed middleware configuration
#middleware {
    before: [
        @log_request(),
        @validate_request(),
        @rate_limit_check()
    ]
    after: [
        @log_response(),
        @add_headers(),
        @cache_response()
    ]
    error: [
        @log_error(),
        @format_error_response()
    ]
} {
    #api /configured-endpoint {
        return @process_request()
    }
}

# Middleware with dependencies
#middleware {
    requires: ["auth", "logging"]
    optional: ["caching", "metrics"]
    order: ["auth", "logging", "caching", "metrics"]
} {
    #api /dependency-endpoint {
        return @process_request()
    }
}
```

## Java Middleware Configuration

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import org.springframework.context.annotation.Bean;
import org.springframework.web.servlet.config.annotation.InterceptorRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;
import java.util.List;
import java.util.Map;

@Component
@ConfigurationProperties(prefix = "tusk.middleware")
public class MiddlewareConfig {
    
    private List<String> globalBefore = List.of("logging", "validation");
    private List<String> globalAfter = List.of("logging", "headers");
    private List<String> globalError = List.of("error-logging", "error-formatting");
    
    private Map<String, MiddlewareDefinition> middlewares;
    private List<String> requiredMiddlewares;
    private List<String> optionalMiddlewares;
    private List<String> middlewareOrder;
    
    // Getters and setters
    public List<String> getGlobalBefore() { return globalBefore; }
    public void setGlobalBefore(List<String> globalBefore) { this.globalBefore = globalBefore; }
    
    public List<String> getGlobalAfter() { return globalAfter; }
    public void setGlobalAfter(List<String> globalAfter) { this.globalAfter = globalAfter; }
    
    public List<String> getGlobalError() { return globalError; }
    public void setGlobalError(List<String> globalError) { this.globalError = globalError; }
    
    public Map<String, MiddlewareDefinition> getMiddlewares() { return middlewares; }
    public void setMiddlewares(Map<String, MiddlewareDefinition> middlewares) { this.middlewares = middlewares; }
    
    public List<String> getRequiredMiddlewares() { return requiredMiddlewares; }
    public void setRequiredMiddlewares(List<String> requiredMiddlewares) { this.requiredMiddlewares = requiredMiddlewares; }
    
    public List<String> getOptionalMiddlewares() { return optionalMiddlewares; }
    public void setOptionalMiddlewares(List<String> optionalMiddlewares) { this.optionalMiddlewares = optionalMiddlewares; }
    
    public List<String> getMiddlewareOrder() { return middlewareOrder; }
    public void setMiddlewareOrder(List<String> middlewareOrder) { this.middlewareOrder = middlewareOrder; }
    
    public static class MiddlewareDefinition {
        private boolean enabled = true;
        private int order = 0;
        private List<String> dependencies;
        private Map<String, Object> config;
        
        // Getters and setters
        public boolean isEnabled() { return enabled; }
        public void setEnabled(boolean enabled) { this.enabled = enabled; }
        
        public int getOrder() { return order; }
        public void setOrder(int order) { this.order = order; }
        
        public List<String> getDependencies() { return dependencies; }
        public void setDependencies(List<String> dependencies) { this.dependencies = dependencies; }
        
        public Map<String, Object> getConfig() { return config; }
        public void setConfig(Map<String, Object> config) { this.config = config; }
    }
}

@Configuration
public class MiddlewareConfiguration implements WebMvcConfigurer {
    
    private final MiddlewareConfig config;
    private final List<HandlerInterceptor> interceptors;
    
    public MiddlewareConfiguration(MiddlewareConfig config, List<HandlerInterceptor> interceptors) {
        this.config = config;
        this.interceptors = interceptors;
    }
    
    @Override
    public void addInterceptors(InterceptorRegistry registry) {
        // Add global interceptors
        for (HandlerInterceptor interceptor : interceptors) {
            registry.addInterceptor(interceptor);
        }
        
        // Add configured interceptors
        if (config.getMiddlewares() != null) {
            config.getMiddlewares().entrySet().stream()
                .filter(entry -> entry.getValue().isEnabled())
                .sorted(Map.Entry.comparingByValue(Comparator.comparing(MiddlewareConfig.MiddlewareDefinition::getOrder)))
                .forEach(entry -> {
                    HandlerInterceptor interceptor = createInterceptor(entry.getKey(), entry.getValue());
                    registry.addInterceptor(interceptor);
                });
        }
    }
    
    private HandlerInterceptor createInterceptor(String name, MiddlewareConfig.MiddlewareDefinition definition) {
        // Create interceptor based on middleware type
        switch (name) {
            case "logging":
                return new LoggingInterceptor();
            case "validation":
                return new ValidationInterceptor();
            case "auth":
                return new AuthInterceptor();
            case "caching":
                return new CachingInterceptor();
            case "metrics":
                return new MetricsInterceptor();
            default:
                return new CustomInterceptor(name, definition);
        }
    }
}
```

## Logging Middleware

```tusk
# Request/response logging
#middleware {
    before: @log_request(@request.method, @request.path, @request.ip)
    after: @log_response(@response.status, @response.time)
} {
    #api /logged-endpoint {
        return @process_request()
    }
}

# Structured logging
#middleware {
    before: @log_structured({
        event: "request_start",
        method: @request.method,
        path: @request.path,
        user_id: @auth.user?.id,
        timestamp: @date.now()
    })
    after: @log_structured({
        event: "request_end",
        status: @response.status,
        duration: @response.duration,
        timestamp: @date.now()
    })
} {
    #api /structured-logged {
        return @process_request()
    }
}
```

## Java Logging Middleware

```java
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.time.Instant;
import java.util.Map;
import java.util.HashMap;

@Component
public class LoggingInterceptor implements HandlerInterceptor {
    
    private static final Logger logger = LoggerFactory.getLogger(LoggingInterceptor.class);
    private final ObjectMapper objectMapper;
    
    public LoggingInterceptor(ObjectMapper objectMapper) {
        this.objectMapper = objectMapper;
    }
    
    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) {
        long startTime = System.currentTimeMillis();
        request.setAttribute("startTime", startTime);
        
        Map<String, Object> logData = new HashMap<>();
        logData.put("event", "request_start");
        logData.put("method", request.getMethod());
        logData.put("path", request.getRequestURI());
        logData.put("ip", getClientIpAddress(request));
        logData.put("user_agent", request.getHeader("User-Agent"));
        logData.put("timestamp", Instant.now().toString());
        
        // Add user ID if authenticated
        Authentication auth = SecurityContextHolder.getContext().getAuthentication();
        if (auth != null && auth.isAuthenticated() && auth.getPrincipal() instanceof User) {
            User user = (User) auth.getPrincipal();
            logData.put("user_id", user.getId());
        }
        
        try {
            logger.info("Request: {}", objectMapper.writeValueAsString(logData));
        } catch (Exception e) {
            logger.warn("Failed to log request", e);
        }
        
        return true;
    }
    
    @Override
    public void afterCompletion(HttpServletRequest request, HttpServletResponse response, 
                              Object handler, Exception ex) {
        long startTime = (Long) request.getAttribute("startTime");
        long duration = System.currentTimeMillis() - startTime;
        
        Map<String, Object> logData = new HashMap<>();
        logData.put("event", "request_end");
        logData.put("method", request.getMethod());
        logData.put("path", request.getRequestURI());
        logData.put("status", response.getStatus());
        logData.put("duration", duration);
        logData.put("timestamp", Instant.now().toString());
        
        if (ex != null) {
            logData.put("error", ex.getMessage());
        }
        
        try {
            logger.info("Response: {}", objectMapper.writeValueAsString(logData));
        } catch (Exception e) {
            logger.warn("Failed to log response", e);
        }
    }
    
    private String getClientIpAddress(HttpServletRequest request) {
        String xForwardedFor = request.getHeader("X-Forwarded-For");
        if (xForwardedFor != null && !xForwardedFor.isEmpty()) {
            return xForwardedFor.split(",")[0].trim();
        }
        
        String xRealIp = request.getHeader("X-Real-IP");
        if (xRealIp != null && !xRealIp.isEmpty()) {
            return xRealIp;
        }
        
        return request.getRemoteAddr();
    }
}

@Service
public class LoggingService {
    
    private final Logger logger = LoggerFactory.getLogger(LoggingService.class);
    private final ObjectMapper objectMapper;
    
    public LoggingService(ObjectMapper objectMapper) {
        this.objectMapper = objectMapper;
    }
    
    public void logRequest(HttpServletRequest request) {
        Map<String, Object> logData = new HashMap<>();
        logData.put("event", "request");
        logData.put("method", request.getMethod());
        logData.put("path", request.getRequestURI());
        logData.put("ip", getClientIpAddress(request));
        logData.put("timestamp", Instant.now().toString());
        
        logger.info("Request: {}", logData);
    }
    
    public void logResponse(HttpServletResponse response, Object data) {
        Map<String, Object> logData = new HashMap<>();
        logData.put("event", "response");
        logData.put("status", response.getStatus());
        logData.put("timestamp", Instant.now().toString());
        
        logger.info("Response: {}", logData);
    }
    
    public void logStructured(Map<String, Object> data) {
        try {
            logger.info("Structured log: {}", objectMapper.writeValueAsString(data));
        } catch (Exception e) {
            logger.warn("Failed to log structured data", e);
        }
    }
    
    private String getClientIpAddress(HttpServletRequest request) {
        String xForwardedFor = request.getHeader("X-Forwarded-For");
        if (xForwardedFor != null && !xForwardedFor.isEmpty()) {
            return xForwardedFor.split(",")[0].trim();
        }
        
        String xRealIp = request.getHeader("X-Real-IP");
        if (xRealIp != null && !xRealIp.isEmpty()) {
            return xRealIp;
        }
        
        return request.getRemoteAddr();
    }
}
```

## Validation Middleware

```tusk
# Request validation
#middleware {
    before: @validate_request(@request.body, @request.headers)
} {
    #api /validated-endpoint {
        return @process_validated_request()
    }
}

# Schema validation
#middleware {
    before: @validate_schema(@request.body, "user_schema")
} {
    #api /schema-validated {
        return @process_schema_validated_request()
    }
}

# Custom validation
#middleware {
    before: @custom_validation(@request.body, @auth.user)
} {
    #api /custom-validated {
        return @process_custom_validated_request()
    }
}
```

## Java Validation Middleware

```java
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;
import org.springframework.validation.Validator;
import org.springframework.validation.BeanPropertyBindingResult;
import org.springframework.validation.Errors;
import javax.validation.ConstraintViolation;
import javax.validation.Validator as JavaxValidator;
import java.util.Set;

@Component
public class ValidationInterceptor implements HandlerInterceptor {
    
    private final Validator springValidator;
    private final JavaxValidator javaxValidator;
    private final SchemaValidator schemaValidator;
    
    public ValidationInterceptor(Validator springValidator,
                               JavaxValidator javaxValidator,
                               SchemaValidator schemaValidator) {
        this.springValidator = springValidator;
        this.javaxValidator = javaxValidator;
        this.schemaValidator = schemaValidator;
    }
    
    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) {
        // Validate request body
        if (request.getContentType() != null && request.getContentType().contains("application/json")) {
            try {
                String body = getRequestBody(request);
                if (body != null && !body.isEmpty()) {
                    validateRequestBody(body, request);
                }
            } catch (Exception e) {
                response.setStatus(400);
                response.getWriter().write("{\"error\": \"Invalid request body\"}");
                return false;
            }
        }
        
        // Validate headers
        validateHeaders(request);
        
        return true;
    }
    
    private void validateRequestBody(String body, HttpServletRequest request) {
        // JSON schema validation
        if (request.getRequestURI().contains("/schema-validated")) {
            schemaValidator.validate(body, "user_schema");
        }
        
        // Bean validation
        try {
            Object requestObject = objectMapper.readValue(body, Object.class);
            if (requestObject instanceof Validatable) {
                Validatable validatable = (Validatable) requestObject;
                Errors errors = new BeanPropertyBindingResult(validatable, "request");
                springValidator.validate(validatable, errors);
                
                if (errors.hasErrors()) {
                    throw new ValidationException("Validation failed", errors);
                }
            }
        } catch (Exception e) {
            throw new ValidationException("Failed to validate request body", e);
        }
    }
    
    private void validateHeaders(HttpServletRequest request) {
        // Validate required headers
        String authHeader = request.getHeader("Authorization");
        if (request.getRequestURI().contains("/protected") && authHeader == null) {
            throw new ValidationException("Authorization header required");
        }
        
        // Validate content type for POST requests
        if ("POST".equals(request.getMethod()) && 
            request.getContentType() == null) {
            throw new ValidationException("Content-Type header required for POST requests");
        }
    }
    
    private String getRequestBody(HttpServletRequest request) {
        try {
            return request.getReader().lines().collect(Collectors.joining());
        } catch (Exception e) {
            return null;
        }
    }
}

@Service
public class ValidationService {
    
    private final JavaxValidator javaxValidator;
    private final SchemaValidator schemaValidator;
    
    public ValidationService(JavaxValidator javaxValidator, SchemaValidator schemaValidator) {
        this.javaxValidator = javaxValidator;
        this.schemaValidator = schemaValidator;
    }
    
    public void validateRequest(Object request, Map<String, String> headers) {
        // Bean validation
        Set<ConstraintViolation<Object>> violations = javaxValidator.validate(request);
        if (!violations.isEmpty()) {
            throw new ValidationException("Validation failed", violations);
        }
        
        // Header validation
        validateHeaders(headers);
    }
    
    public void validateSchema(Object request, String schemaName) {
        schemaValidator.validate(request, schemaName);
    }
    
    public void customValidation(Object request, User user) {
        // Custom business logic validation
        if (request instanceof UserUpdateRequest) {
            UserUpdateRequest updateRequest = (UserUpdateRequest) request;
            
            // Check if user can update the target user
            if (!user.getId().equals(updateRequest.getUserId()) && !user.hasRole("ADMIN")) {
                throw new ValidationException("Insufficient permissions to update user");
            }
        }
    }
    
    private void validateHeaders(Map<String, String> headers) {
        // Validate required headers
        if (!headers.containsKey("Content-Type")) {
            throw new ValidationException("Content-Type header required");
        }
    }
}

@Component
public class SchemaValidator {
    
    private final ObjectMapper objectMapper;
    private final Map<String, JsonNode> schemas;
    
    public SchemaValidator(ObjectMapper objectMapper) {
        this.objectMapper = objectMapper;
        this.schemas = loadSchemas();
    }
    
    public void validate(Object data, String schemaName) {
        JsonNode schema = schemas.get(schemaName);
        if (schema == null) {
            throw new ValidationException("Schema not found: " + schemaName);
        }
        
        JsonNode dataNode = objectMapper.valueToTree(data);
        
        // Use JSON Schema validator
        JsonSchema jsonSchema = JsonSchemaFactory.byDefault().getJsonSchema(schema);
        ProcessingReport report = jsonSchema.validate(dataNode);
        
        if (!report.isSuccess()) {
            throw new ValidationException("Schema validation failed", report);
        }
    }
    
    private Map<String, JsonNode> loadSchemas() {
        Map<String, JsonNode> schemas = new HashMap<>();
        
        // Load schemas from resources
        try {
            Resource[] resources = new PathMatchingResourcePatternResolver()
                .getResources("classpath:schemas/*.json");
            
            for (Resource resource : resources) {
                String filename = resource.getFilename();
                String schemaName = filename.substring(0, filename.lastIndexOf('.'));
                JsonNode schema = objectMapper.readTree(resource.getInputStream());
                schemas.put(schemaName, schema);
            }
        } catch (Exception e) {
            throw new RuntimeException("Failed to load schemas", e);
        }
        
        return schemas;
    }
}
```

## Authentication Middleware

```tusk
# Authentication check
#middleware {
    before: @check_auth(@request.headers.Authorization)
} {
    #api /auth-protected {
        return @get_protected_data()
    }
}

# Role-based middleware
#middleware {
    before: @check_role(@auth.user.role, ["admin", "user"])
} {
    #api /role-protected {
        return @get_role_protected_data()
    }
}

# Permission-based middleware
#middleware {
    before: @check_permission(@auth.user.id, "read:data")
} {
    #api /permission-protected {
        return @get_permission_protected_data()
    }
}
```

## Java Authentication Middleware

```java
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;

@Component
public class AuthInterceptor implements HandlerInterceptor {
    
    private final JwtTokenProvider tokenProvider;
    private final UserService userService;
    private final PermissionService permissionService;
    
    public AuthInterceptor(JwtTokenProvider tokenProvider,
                          UserService userService,
                          PermissionService permissionService) {
        this.tokenProvider = tokenProvider;
        this.userService = userService;
        this.permissionService = permissionService;
    }
    
    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) {
        String authHeader = request.getHeader("Authorization");
        
        if (authHeader == null || !authHeader.startsWith("Bearer ")) {
            response.setStatus(401);
            response.getWriter().write("{\"error\": \"Authorization header required\"}");
            return false;
        }
        
        try {
            String token = authHeader.substring(7);
            Claims claims = tokenProvider.validateToken(token);
            
            // Set authentication in context
            User user = userService.findById(claims.get("userId", Long.class));
            UsernamePasswordAuthenticationToken authentication = 
                new UsernamePasswordAuthenticationToken(user, null, user.getAuthorities());
            SecurityContextHolder.getContext().setAuthentication(authentication);
            
            // Check role-based access
            if (request.getRequestURI().contains("/role-protected")) {
                if (!hasRequiredRole(user, List.of("ADMIN", "USER"))) {
                    response.setStatus(403);
                    response.getWriter().write("{\"error\": \"Insufficient role\"}");
                    return false;
                }
            }
            
            // Check permission-based access
            if (request.getRequestURI().contains("/permission-protected")) {
                if (!permissionService.hasPermission(user.getId(), "read:data")) {
                    response.setStatus(403);
                    response.getWriter().write("{\"error\": \"Insufficient permissions\"}");
                    return false;
                }
            }
            
            return true;
        } catch (Exception e) {
            response.setStatus(401);
            response.getWriter().write("{\"error\": \"Invalid token\"}");
            return false;
        }
    }
    
    private boolean hasRequiredRole(User user, List<String> requiredRoles) {
        return user.getAuthorities().stream()
            .anyMatch(authority -> requiredRoles.contains(authority.getAuthority().replace("ROLE_", "")));
    }
}

@Service
public class AuthService {
    
    private final JwtTokenProvider tokenProvider;
    private final UserService userService;
    
    public AuthService(JwtTokenProvider tokenProvider, UserService userService) {
        this.tokenProvider = tokenProvider;
        this.userService = userService;
    }
    
    public User checkAuth(String authHeader) {
        if (authHeader == null || !authHeader.startsWith("Bearer ")) {
            throw new AuthenticationException("Authorization header required");
        }
        
        String token = authHeader.substring(7);
        Claims claims = tokenProvider.validateToken(token);
        
        return userService.findById(claims.get("userId", Long.class));
    }
    
    public boolean checkRole(User user, List<String> requiredRoles) {
        return user.getAuthorities().stream()
            .anyMatch(authority -> requiredRoles.contains(authority.getAuthority().replace("ROLE_", "")));
    }
    
    public boolean checkPermission(Long userId, String permission) {
        return permissionService.hasPermission(userId, permission);
    }
}
```

## Caching Middleware

```tusk
# Response caching
#middleware {
    before: @check_cache(@request.path, @request.query)
    after: @cache_response(@response, "5m")
} {
    #api /cached-endpoint {
        return @get_cached_data()
    }
}

# Conditional caching
#middleware {
    before: @check_cache_if(@request.method == "GET")
    after: @cache_response_if(@response.status == 200)
} {
    #api /conditional-cached {
        return @get_conditional_cached_data()
    }
}
```

## Java Caching Middleware

```java
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;
import org.springframework.cache.CacheManager;
import org.springframework.cache.Cache;

@Component
public class CachingInterceptor implements HandlerInterceptor {
    
    private final CacheManager cacheManager;
    private final CacheKeyGenerator keyGenerator;
    
    public CachingInterceptor(CacheManager cacheManager, CacheKeyGenerator keyGenerator) {
        this.cacheManager = cacheManager;
        this.keyGenerator = keyGenerator;
    }
    
    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) {
        // Only cache GET requests
        if (!"GET".equals(request.getMethod())) {
            return true;
        }
        
        String cacheKey = keyGenerator.generateKey(request);
        Cache cache = cacheManager.getCache("api-cache");
        
        if (cache != null) {
            Cache.ValueWrapper cached = cache.get(cacheKey);
            if (cached != null) {
                try {
                    response.setContentType("application/json");
                    response.getWriter().write(objectMapper.writeValueAsString(cached.get()));
                    return false; // Stop processing, return cached response
                } catch (Exception e) {
                    // Continue with normal processing if cache serialization fails
                }
            }
        }
        
        return true;
    }
    
    @Override
    public void afterCompletion(HttpServletRequest request, HttpServletResponse response, 
                              Object handler, Exception ex) {
        // Only cache successful GET responses
        if (!"GET".equals(request.getMethod()) || response.getStatus() != 200) {
            return;
        }
        
        String cacheKey = keyGenerator.generateKey(request);
        Cache cache = cacheManager.getCache("api-cache");
        
        if (cache != null) {
            // Note: In a real implementation, you'd need to capture the response body
            // This is a simplified example
            cache.put(cacheKey, "cached-response");
        }
    }
}

@Service
public class CachingService {
    
    private final CacheManager cacheManager;
    private final CacheKeyGenerator keyGenerator;
    
    public CachingService(CacheManager cacheManager, CacheKeyGenerator keyGenerator) {
        this.cacheManager = cacheManager;
        this.keyGenerator = keyGenerator;
    }
    
    public Object checkCache(String path, Map<String, String> queryParams) {
        String cacheKey = keyGenerator.generateKey(path, queryParams);
        Cache cache = cacheManager.getCache("api-cache");
        
        if (cache != null) {
            Cache.ValueWrapper cached = cache.get(cacheKey);
            return cached != null ? cached.get() : null;
        }
        
        return null;
    }
    
    public void cacheResponse(Object response, String ttl) {
        // Implementation would depend on the specific caching strategy
        // This is a simplified example
    }
    
    public Object checkCacheIf(boolean condition) {
        return condition ? checkCache() : null;
    }
    
    public void cacheResponseIf(Object response, boolean condition) {
        if (condition) {
            cacheResponse(response, "5m");
        }
    }
}
```

## Metrics Middleware

```tusk
# Request metrics
#middleware {
    before: @start_metrics(@request.path, @request.method)
    after: @end_metrics(@response.status, @response.duration)
} {
    #api /metrics-tracked {
        return @get_metrics_data()
    }
}

# Custom metrics
#middleware {
    before: @increment_counter("api_requests", @request.path)
    after: @record_histogram("response_time", @response.duration)
} {
    #api /custom-metrics {
        return @get_custom_metrics_data()
    }
}
```

## Java Metrics Middleware

```java
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;
import io.micrometer.core.instrument.MeterRegistry;
import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.Timer;

@Component
public class MetricsInterceptor implements HandlerInterceptor {
    
    private final MeterRegistry meterRegistry;
    private final Map<String, Counter> counters;
    private final Map<String, Timer> timers;
    
    public MetricsInterceptor(MeterRegistry meterRegistry) {
        this.meterRegistry = meterRegistry;
        this.counters = new ConcurrentHashMap<>();
        this.timers = new ConcurrentHashMap<>();
    }
    
    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) {
        long startTime = System.currentTimeMillis();
        request.setAttribute("startTime", startTime);
        
        // Increment request counter
        String path = request.getRequestURI();
        String method = request.getMethod();
        
        Counter counter = counters.computeIfAbsent(
            "api_requests",
            k -> Counter.builder("api_requests")
                .tag("path", path)
                .tag("method", method)
                .register(meterRegistry)
        );
        counter.increment();
        
        return true;
    }
    
    @Override
    public void afterCompletion(HttpServletRequest request, HttpServletResponse response, 
                              Object handler, Exception ex) {
        long startTime = (Long) request.getAttribute("startTime");
        long duration = System.currentTimeMillis() - startTime;
        
        // Record response time
        String path = request.getRequestURI();
        String method = request.getMethod();
        int status = response.getStatus();
        
        Timer timer = timers.computeIfAbsent(
            "response_time",
            k -> Timer.builder("response_time")
                .tag("path", path)
                .tag("method", method)
                .tag("status", String.valueOf(status))
                .register(meterRegistry)
        );
        timer.record(duration, TimeUnit.MILLISECONDS);
        
        // Record error counter if there was an exception
        if (ex != null) {
            Counter errorCounter = counters.computeIfAbsent(
                "api_errors",
                k -> Counter.builder("api_errors")
                    .tag("path", path)
                    .tag("method", method)
                    .register(meterRegistry)
            );
            errorCounter.increment();
        }
    }
}

@Service
public class MetricsService {
    
    private final MeterRegistry meterRegistry;
    
    public MetricsService(MeterRegistry meterRegistry) {
        this.meterRegistry = meterRegistry;
    }
    
    public void startMetrics(String path, String method) {
        // Start timing
        Timer.Sample sample = Timer.start(meterRegistry);
        // Store sample in request attributes for later use
    }
    
    public void endMetrics(int status, long duration) {
        // End timing and record metrics
        Timer timer = Timer.builder("response_time")
            .tag("status", String.valueOf(status))
            .register(meterRegistry);
        timer.record(duration, TimeUnit.MILLISECONDS);
    }
    
    public void incrementCounter(String name, String... tags) {
        Counter counter = Counter.builder(name)
            .tags(tags)
            .register(meterRegistry);
        counter.increment();
    }
    
    public void recordHistogram(String name, double value, String... tags) {
        Timer timer = Timer.builder(name)
            .tags(tags)
            .register(meterRegistry);
        timer.record((long) value, TimeUnit.MILLISECONDS);
    }
}
```

## Custom Middleware

```tusk
# Custom middleware
#middleware {
    before: @custom_before_logic(@request)
    after: @custom_after_logic(@response)
    error: @custom_error_logic(@error)
} {
    #api /custom-middleware {
        return @process_with_custom_logic()
    }
}

# Middleware with configuration
#middleware {
    config: {
        feature_flag: "new_feature"
        timeout: 5000
        retries: 3
    }
    before: @configured_logic(@request, @config)
} {
    #api /configured-middleware {
        return @process_with_config()
    }
}
```

## Java Custom Middleware

```java
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;

@Component
public class CustomInterceptor implements HandlerInterceptor {
    
    private final String name;
    private final MiddlewareConfig.MiddlewareDefinition config;
    private final CustomLogicService customLogicService;
    
    public CustomInterceptor(String name, MiddlewareConfig.MiddlewareDefinition config,
                           CustomLogicService customLogicService) {
        this.name = name;
        this.config = config;
        this.customLogicService = customLogicService;
    }
    
    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) {
        try {
            // Execute custom before logic
            customLogicService.executeBeforeLogic(request, config);
            return true;
        } catch (Exception e) {
            // Execute custom error logic
            customLogicService.executeErrorLogic(e, request, response);
            return false;
        }
    }
    
    @Override
    public void afterCompletion(HttpServletRequest request, HttpServletResponse response, 
                              Object handler, Exception ex) {
        try {
            // Execute custom after logic
            customLogicService.executeAfterLogic(response, config);
        } catch (Exception e) {
            // Log error but don't fail the request
            logger.warn("Error in custom after logic", e);
        }
    }
}

@Service
public class CustomLogicService {
    
    private final FeatureFlagService featureFlagService;
    private final RetryService retryService;
    
    public CustomLogicService(FeatureFlagService featureFlagService, RetryService retryService) {
        this.featureFlagService = featureFlagService;
        this.retryService = retryService;
    }
    
    public void executeBeforeLogic(HttpServletRequest request, 
                                 MiddlewareConfig.MiddlewareDefinition config) {
        // Check feature flags
        if (config.getConfig() != null) {
            String featureFlag = (String) config.getConfig().get("feature_flag");
            if (featureFlag != null && !featureFlagService.isEnabled(featureFlag)) {
                throw new FeatureDisabledException("Feature " + featureFlag + " is disabled");
            }
        }
        
        // Apply custom logic based on configuration
        if (config.getConfig() != null) {
            Integer timeout = (Integer) config.getConfig().get("timeout");
            if (timeout != null) {
                // Set request timeout
                request.setAttribute("timeout", timeout);
            }
        }
    }
    
    public void executeAfterLogic(HttpServletResponse response,
                                MiddlewareConfig.MiddlewareDefinition config) {
        // Apply custom response logic
        if (config.getConfig() != null) {
            Integer retries = (Integer) config.getConfig().get("retries");
            if (retries != null) {
                response.setHeader("X-Retry-Count", String.valueOf(retries));
            }
        }
    }
    
    public void executeErrorLogic(Exception error, HttpServletRequest request,
                                HttpServletResponse response) {
        // Handle custom error logic
        if (error instanceof FeatureDisabledException) {
            response.setStatus(503);
            response.setHeader("X-Feature-Disabled", "true");
        } else {
            response.setStatus(500);
        }
    }
}
```

## Middleware Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.TestPropertySource;
import org.springframework.beans.factory.annotation.Autowired;

@SpringBootTest
@TestPropertySource(properties = {
    "tusk.middleware.global-before=logging,validation",
    "tusk.middleware.global-after=logging"
})
public class MiddlewareTest {
    
    @Autowired
    private MiddlewareController controller;
    
    @MockBean
    private LoggingService loggingService;
    
    @MockBean
    private ValidationService validationService;
    
    @Test
    public void testLoggingMiddleware() {
        // Test that logging middleware is applied
        ResponseEntity<DataResponse> response = controller.getLoggedData(mockRequest(), mockResponse());
        
        verify(loggingService).logRequest(any(HttpServletRequest.class));
        verify(loggingService).logResponse(any(HttpServletResponse.class), any(DataResponse.class));
    }
    
    @Test
    public void testValidationMiddleware() {
        // Test that validation middleware is applied
        HttpServletRequest request = mockRequest();
        request.setContentType("application/json");
        
        ResponseEntity<DataResponse> response = controller.getData();
        
        verify(validationService).validateRequest(any(), any());
    }
    
    @Test
    public void testConditionalMiddleware() {
        // Test conditional middleware
        HttpServletRequest getRequest = mockRequest();
        getRequest.setMethod("GET");
        
        HttpServletRequest postRequest = mockRequest();
        postRequest.setMethod("POST");
        
        // GET request should not trigger validation
        controller.getConditionalData(getRequest);
        verify(validationService, never()).validatePostRequest(any());
        
        // POST request should trigger validation
        controller.getConditionalData(postRequest);
        verify(validationService).validatePostRequest(postRequest);
    }
    
    private HttpServletRequest mockRequest() {
        return mock(HttpServletRequest.class);
    }
    
    private HttpServletResponse mockResponse() {
        return mock(HttpServletResponse.class);
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  middleware:
    global-before: ["logging", "validation"]
    global-after: ["logging", "headers"]
    global-error: ["error-logging", "error-formatting"]
    
    middlewares:
      logging:
        enabled: true
        order: 1
        config:
          level: "INFO"
          include-headers: true
      
      validation:
        enabled: true
        order: 2
        dependencies: ["logging"]
        config:
          strict-mode: true
          schema-validation: true
      
      auth:
        enabled: true
        order: 3
        dependencies: ["logging"]
        config:
          jwt-secret: "${JWT_SECRET}"
          token-expiration: 3600
      
      caching:
        enabled: true
        order: 4
        optional: true
        config:
          ttl: 300
          max-size: 1000
      
      metrics:
        enabled: true
        order: 5
        optional: true
        config:
          enabled-metrics: ["requests", "response-time", "errors"]
    
    required-middlewares: ["logging", "validation", "auth"]
    optional-middlewares: ["caching", "metrics"]
    middleware-order: ["logging", "validation", "auth", "caching", "metrics"]

spring:
  cache:
    type: redis
    redis:
      time-to-live: 300000
      cache-null-values: false
```

## Summary

The `#middleware` directive in TuskLang provides comprehensive middleware capabilities for Java applications. With Spring Boot integration, flexible configuration, and support for various middleware types, you can implement sophisticated request/response processing that enhances your application's functionality.

Key features include:
- **Multiple middleware types**: Logging, validation, authentication, caching, and metrics
- **Spring Boot integration**: Seamless integration with Spring Boot interceptors
- **Flexible configuration**: Configurable middleware with dependencies and ordering
- **Conditional middleware**: Apply middleware based on request conditions
- **Custom middleware**: Extensible middleware system for custom logic
- **Performance monitoring**: Built-in metrics and monitoring capabilities
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade middleware that integrates seamlessly with Spring Boot applications while maintaining the simplicity and power of TuskLang's declarative syntax. 
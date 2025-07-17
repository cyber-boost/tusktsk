# #rate_limit - Rate Limiting Directive (Java)

The `#rate_limit` directive provides enterprise-grade request throttling for Java applications, preventing abuse, protecting resources, and ensuring fair usage with Spring Boot integration.

## Basic Syntax

```tusk
# Basic rate limiting - 60 requests per minute
#rate_limit 60 {
    #api /search {
        results: @search(@request.query.q)
        return results
    }
}

# Custom window - 100 requests per hour
#rate_limit 100 per: "hour" {
    #api /data {
        return @fetch_data()
    }
}

# With custom key
#rate_limit 30 key: @request.ip {
    #web /download {
        @serve_file()
    }
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.RateLimitDirective;
import org.springframework.web.bind.annotation.*;
import org.springframework.stereotype.Controller;
import javax.servlet.http.HttpServletRequest;

@Controller
public class RateLimitedController {
    
    private final TuskLang tuskLang;
    private final RateLimitDirective rateLimiter;
    
    public RateLimitedController(TuskLang tuskLang) {
        this.tuskLang = tuskLang;
        this.rateLimiter = new RateLimitDirective();
    }
    
    // Basic rate limiting
    @GetMapping("/search")
    @RateLimit(limit = 60, window = 60) // 60 requests per minute
    public ResponseEntity<SearchResults> search(
            @RequestParam String q,
            HttpServletRequest request) {
        
        String clientId = request.getRemoteAddr();
        if (!rateLimiter.checkLimit(clientId, 60, 60)) {
            return ResponseEntity.status(429)
                .header("Retry-After", "60")
                .body(null);
        }
        
        SearchResults results = searchService.search(q);
        return ResponseEntity.ok(results);
    }
    
    // Custom rate limiting with Spring annotations
    @PostMapping("/api/data")
    @RateLimit(limit = 100, window = 3600, key = "#request.remoteAddr")
    public ResponseEntity<DataResponse> fetchData(HttpServletRequest request) {
        return ResponseEntity.ok(dataService.fetchData());
    }
}
```

## Rate Limit Configuration

```tusk
# Detailed configuration
#rate_limit {
    limit: 100           # Number of requests
    window: 3600         # Time window in seconds
    key: @auth.id || @request.ip  # Rate limit key
    message: "Too many requests"    # Error message
    response_code: 429   # HTTP status code
} {
    #api /endpoint {
        @process_request()
    }
}

# Per-minute limiting
#rate_limit 60 per: "minute" {
    #api /rapid-endpoint {
        return @quick_response()
    }
}

# Multiple windows
#rate_limit {
    limits: [
        {count: 10, window: 60},      # 10 per minute
        {count: 100, window: 3600},    # 100 per hour
        {count: 1000, window: 86400}   # 1000 per day
    ]
} {
    #api /tiered-limit {
        @handle_request()
    }
}
```

## Java Configuration Class

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import java.util.List;
import java.util.Map;

@Component
@ConfigurationProperties(prefix = "tusk.rate-limit")
public class RateLimitConfig {
    
    private int defaultLimit = 100;
    private int defaultWindow = 3600;
    private String defaultKey = "ip";
    private String defaultMessage = "Too many requests";
    private int defaultResponseCode = 429;
    
    private List<RateLimitTier> tiers;
    private Map<String, RateLimitRule> rules;
    
    // Getters and setters
    public int getDefaultLimit() { return defaultLimit; }
    public void setDefaultLimit(int defaultLimit) { this.defaultLimit = defaultLimit; }
    
    public int getDefaultWindow() { return defaultWindow; }
    public void setDefaultWindow(int defaultWindow) { this.defaultWindow = defaultWindow; }
    
    public String getDefaultKey() { return defaultKey; }
    public void setDefaultKey(String defaultKey) { this.defaultKey = defaultKey; }
    
    public String getDefaultMessage() { return defaultMessage; }
    public void setDefaultMessage(String defaultMessage) { this.defaultMessage = defaultMessage; }
    
    public int getDefaultResponseCode() { return defaultResponseCode; }
    public void setDefaultResponseCode(int defaultResponseCode) { this.defaultResponseCode = defaultResponseCode; }
    
    public List<RateLimitTier> getTiers() { return tiers; }
    public void setTiers(List<RateLimitTier> tiers) { this.tiers = tiers; }
    
    public Map<String, RateLimitRule> getRules() { return rules; }
    public void setRules(Map<String, RateLimitRule> rules) { this.rules = rules; }
    
    public static class RateLimitTier {
        private int count;
        private int window;
        private String name;
        
        // Getters and setters
        public int getCount() { return count; }
        public void setCount(int count) { this.count = count; }
        
        public int getWindow() { return window; }
        public void setWindow(int window) { this.window = window; }
        
        public String getName() { return name; }
        public void setName(String name) { this.name = name; }
    }
    
    public static class RateLimitRule {
        private int limit;
        private int window;
        private String key;
        private String message;
        private int responseCode;
        
        // Getters and setters
        public int getLimit() { return limit; }
        public void setLimit(int limit) { this.limit = limit; }
        
        public int getWindow() { return window; }
        public void setWindow(int window) { this.window = window; }
        
        public String getKey() { return key; }
        public void setKey(String key) { this.key = key; }
        
        public String getMessage() { return message; }
        public void setMessage(String message) { this.message = message; }
        
        public int getResponseCode() { return responseCode; }
        public void setResponseCode(int responseCode) { this.responseCode = responseCode; }
    }
}
```

## User-Based Rate Limiting

```tusk
# Different limits for different users
#rate_limit {
    limit: () => {
        if (!@auth.check()) return 20  # Guests
        if (@auth.user.isPremium()) return 1000  # Premium users
        return 100  # Regular users
    }
    window: 3600
    key: @auth.id || @request.ip
} {
    #api /user-endpoint {
        @process()
    }
}

# Role-based limits
#rate_limit {
    limits: {
        guest: {count: 10, window: 3600},
        user: {count: 100, window: 3600},
        premium: {count: 1000, window: 3600},
        admin: null  # No limit
    }
    role: @auth.user?.role || "guest"
} {
    #api /role-based {
        @handle()
    }
}
```

## Java User-Based Implementation

```java
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Service;

@Service
public class UserRateLimitService {
    
    private final RateLimitConfig config;
    private final RedisTemplate<String, String> redisTemplate;
    
    public UserRateLimitService(RateLimitConfig config, RedisTemplate<String, String> redisTemplate) {
        this.config = config;
        this.redisTemplate = redisTemplate;
    }
    
    public boolean checkUserLimit(String endpoint, HttpServletRequest request) {
        Authentication auth = SecurityContextHolder.getContext().getAuthentication();
        String key = generateKey(auth, request);
        
        RateLimitRule rule = getRuleForUser(auth, endpoint);
        if (rule == null) return true; // No limit
        
        return checkLimit(key, rule.getLimit(), rule.getWindow());
    }
    
    private RateLimitRule getRuleForUser(Authentication auth, String endpoint) {
        if (auth == null || !auth.isAuthenticated()) {
            return createRule(20, 3600); // Guest limit
        }
        
        User user = (User) auth.getPrincipal();
        
        if (user.hasRole("ADMIN")) {
            return null; // No limit for admins
        }
        
        if (user.hasRole("PREMIUM")) {
            return createRule(1000, 3600); // Premium limit
        }
        
        return createRule(100, 3600); // Regular user limit
    }
    
    private RateLimitRule createRule(int limit, int window) {
        RateLimitRule rule = new RateLimitRule();
        rule.setLimit(limit);
        rule.setWindow(window);
        rule.setMessage("Rate limit exceeded");
        rule.setResponseCode(429);
        return rule;
    }
    
    private String generateKey(Authentication auth, HttpServletRequest request) {
        if (auth != null && auth.isAuthenticated()) {
            User user = (User) auth.getPrincipal();
            return "rate_limit:" + user.getId() + ":" + request.getRequestURI();
        }
        return "rate_limit:" + request.getRemoteAddr() + ":" + request.getRequestURI();
    }
    
    private boolean checkLimit(String key, int limit, int window) {
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

## API Key Rate Limiting

```tusk
# Rate limit by API key
#rate_limit {
    key: @request.headers["X-API-Key"]
    limit: () => {
        api_key: @ApiKey.where("key", @request.headers["X-API-Key"]).first()
        return api_key?.rate_limit || 100
    }
    window: 3600
} {
    #api /external-api {
        @serve_api_request()
    }
}

# Tiered API limits
#rate_limit {
    key: @request.api_key
    limits: () => {
        tier: @get_api_tier(@request.api_key)
        
        return match tier {
            "free" => [{count: 100, window: 86400}]
            "basic" => [{count: 1000, window: 3600}]
            "pro" => [{count: 10000, window: 3600}]
            "enterprise" => []  # No limits
        }
    }
} {
    #api /v2/* {
        @handle_api_v2()
    }
}
```

## Java API Key Implementation

```java
import org.springframework.web.bind.annotation.*;
import org.springframework.http.HttpHeaders;
import javax.persistence.*;
import java.time.LocalDateTime;

@Entity
@Table(name = "api_keys")
public class ApiKey {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(unique = true, nullable = false)
    private String key;
    
    @Enumerated(EnumType.STRING)
    private ApiTier tier;
    
    private int rateLimit;
    private LocalDateTime expiresAt;
    private boolean active;
    
    // Getters and setters
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }
    
    public String getKey() { return key; }
    public void setKey(String key) { this.key = key; }
    
    public ApiTier getTier() { return tier; }
    public void setTier(ApiTier tier) { this.tier = tier; }
    
    public int getRateLimit() { return rateLimit; }
    public void setRateLimit(int rateLimit) { this.rateLimit = rateLimit; }
    
    public LocalDateTime getExpiresAt() { return expiresAt; }
    public void setExpiresAt(LocalDateTime expiresAt) { this.expiresAt = expiresAt; }
    
    public boolean isActive() { return active; }
    public void setActive(boolean active) { this.active = active; }
    
    public enum ApiTier {
        FREE(100, 86400),
        BASIC(1000, 3600),
        PRO(10000, 3600),
        ENTERPRISE(Integer.MAX_VALUE, 3600);
        
        private final int limit;
        private final int window;
        
        ApiTier(int limit, int window) {
            this.limit = limit;
            this.window = window;
        }
        
        public int getLimit() { return limit; }
        public int getWindow() { return window; }
    }
}

@Service
public class ApiKeyRateLimitService {
    
    private final ApiKeyRepository apiKeyRepository;
    private final RedisTemplate<String, String> redisTemplate;
    
    public ApiKeyRateLimitService(ApiKeyRepository apiKeyRepository, 
                                 RedisTemplate<String, String> redisTemplate) {
        this.apiKeyRepository = apiKeyRepository;
        this.redisTemplate = redisTemplate;
    }
    
    public boolean checkApiKeyLimit(String apiKey, String endpoint) {
        ApiKey key = apiKeyRepository.findByKeyAndActiveTrue(apiKey);
        if (key == null) {
            return false; // Invalid API key
        }
        
        if (key.getExpiresAt() != null && key.getExpiresAt().isBefore(LocalDateTime.now())) {
            return false; // Expired API key
        }
        
        String redisKey = "api_rate_limit:" + apiKey + ":" + endpoint;
        int limit = key.getTier().getLimit();
        int window = key.getTier().getWindow();
        
        return checkLimit(redisKey, limit, window);
    }
    
    private boolean checkLimit(String key, int limit, int window) {
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

@RestController
@RequestMapping("/api/v2")
public class ApiV2Controller {
    
    private final ApiKeyRateLimitService rateLimitService;
    
    public ApiV2Controller(ApiKeyRateLimitService rateLimitService) {
        this.rateLimitService = rateLimitService;
    }
    
    @GetMapping("/data")
    public ResponseEntity<ApiResponse> getData(
            @RequestHeader(HttpHeaders.AUTHORIZATION) String authHeader,
            HttpServletRequest request) {
        
        String apiKey = extractApiKey(authHeader);
        String endpoint = request.getRequestURI();
        
        if (!rateLimitService.checkApiKeyLimit(apiKey, endpoint)) {
            return ResponseEntity.status(429)
                .header("Retry-After", "3600")
                .body(new ApiResponse("Rate limit exceeded", null));
        }
        
        // Process request
        return ResponseEntity.ok(new ApiResponse("Success", dataService.getData()));
    }
    
    private String extractApiKey(String authHeader) {
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            return authHeader.substring(7);
        }
        return null;
    }
}
```

## Route-Specific Limits

```tusk
# Different limits for different routes
#rate_limit {
    limit: match @request.path {
        "/api/search" => 30
        "/api/heavy-operation" => 5
        "/api/lightweight" => 200
        _ => 60  # Default
    }
    window: 3600
} {
    #api /* {
        @route_handler()
    }
}

# Method-based limits
#rate_limit {
    limit: match @request.method {
        "GET" => 100
        "POST" => 50
        "PUT" => 50
        "DELETE" => 20
    }
    window: 3600
} {
    #api /resources {
        @handle_resource()
    }
}
```

## Java Route-Specific Implementation

```java
import org.springframework.web.bind.annotation.*;
import java.util.Map;
import java.util.HashMap;

@Service
public class RouteRateLimitService {
    
    private final Map<String, RateLimitRule> routeRules;
    private final RedisTemplate<String, String> redisTemplate;
    
    public RouteRateLimitService(RedisTemplate<String, String> redisTemplate) {
        this.redisTemplate = redisTemplate;
        this.routeRules = initializeRouteRules();
    }
    
    private Map<String, RateLimitRule> initializeRouteRules() {
        Map<String, RateLimitRule> rules = new HashMap<>();
        
        // Route-specific rules
        rules.put("/api/search", createRule(30, 3600));
        rules.put("/api/heavy-operation", createRule(5, 3600));
        rules.put("/api/lightweight", createRule(200, 3600));
        
        // Method-specific rules
        rules.put("GET:/api/resources", createRule(100, 3600));
        rules.put("POST:/api/resources", createRule(50, 3600));
        rules.put("PUT:/api/resources", createRule(50, 3600));
        rules.put("DELETE:/api/resources", createRule(20, 3600));
        
        return rules;
    }
    
    public boolean checkRouteLimit(String path, String method, String clientId) {
        String routeKey = method + ":" + path;
        RateLimitRule rule = routeRules.get(routeKey);
        
        if (rule == null) {
            rule = routeRules.get(path); // Try path-only match
        }
        
        if (rule == null) {
            rule = createRule(60, 3600); // Default rule
        }
        
        String redisKey = "route_rate_limit:" + clientId + ":" + routeKey;
        return checkLimit(redisKey, rule.getLimit(), rule.getWindow());
    }
    
    private RateLimitRule createRule(int limit, int window) {
        RateLimitRule rule = new RateLimitRule();
        rule.setLimit(limit);
        rule.setWindow(window);
        rule.setMessage("Rate limit exceeded for this route");
        rule.setResponseCode(429);
        return rule;
    }
    
    private boolean checkLimit(String key, int limit, int window) {
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

@RestController
public class RouteSpecificController {
    
    private final RouteRateLimitService rateLimitService;
    
    public RouteSpecificController(RouteRateLimitService rateLimitService) {
        this.rateLimitService = rateLimitService;
    }
    
    @GetMapping("/api/search")
    public ResponseEntity<SearchResults> search(
            @RequestParam String q,
            HttpServletRequest request) {
        
        String clientId = request.getRemoteAddr();
        if (!rateLimitService.checkRouteLimit("/api/search", "GET", clientId)) {
            return ResponseEntity.status(429)
                .header("Retry-After", "3600")
                .body(null);
        }
        
        return ResponseEntity.ok(searchService.search(q));
    }
    
    @PostMapping("/api/heavy-operation")
    public ResponseEntity<OperationResult> heavyOperation(
            @RequestBody OperationRequest request,
            HttpServletRequest httpRequest) {
        
        String clientId = httpRequest.getRemoteAddr();
        if (!rateLimitService.checkRouteLimit("/api/heavy-operation", "POST", clientId)) {
            return ResponseEntity.status(429)
                .header("Retry-After", "3600")
                .body(null);
        }
        
        return ResponseEntity.ok(operationService.process(request));
    }
}
```

## Cost-Based Rate Limiting

```tusk
# Point-based system
#rate_limit {
    points: 1000  # Total points per window
    window: 3600
    
    # Different operations cost different points
    cost: () => {
        return match @request.path {
            "/api/simple" => 1
            "/api/moderate" => 10
            "/api/expensive" => 100
            "/api/ai-generate" => 500
        }
    }
} {
    #api /* {
        @handle_with_cost()
    }
}

# Dynamic cost calculation
#rate_limit {
    points: 10000
    window: 86400  # Daily limit
    
    cost: () => {
        # Calculate based on request complexity
        base_cost: 1
        
        if (@request.query.include_relations) {
            base_cost *= 2
        }
        
        if (@request.query.limit > 100) {
            base_cost *= 3
        }
        
        return base_cost
    }
} {
    #api /flexible-endpoint {
        @process_flexible()
    }
}
```

## Java Cost-Based Implementation

```java
import org.springframework.stereotype.Service;
import java.util.Map;
import java.util.HashMap;

@Service
public class CostBasedRateLimitService {
    
    private final Map<String, Integer> operationCosts;
    private final RedisTemplate<String, String> redisTemplate;
    
    public CostBasedRateLimitService(RedisTemplate<String, String> redisTemplate) {
        this.redisTemplate = redisTemplate;
        this.operationCosts = initializeOperationCosts();
    }
    
    private Map<String, Integer> initializeOperationCosts() {
        Map<String, Integer> costs = new HashMap<>();
        costs.put("/api/simple", 1);
        costs.put("/api/moderate", 10);
        costs.put("/api/expensive", 100);
        costs.put("/api/ai-generate", 500);
        return costs;
    }
    
    public boolean checkCostBasedLimit(String path, String clientId, 
                                     Map<String, String> queryParams) {
        int cost = calculateCost(path, queryParams);
        String redisKey = "cost_rate_limit:" + clientId;
        
        String currentPoints = redisTemplate.opsForValue().get(redisKey);
        int remainingPoints = currentPoints != null ? Integer.parseInt(currentPoints) : 1000;
        
        if (remainingPoints < cost) {
            return false;
        }
        
        redisTemplate.opsForValue().decrement(redisKey, cost);
        redisTemplate.expire(redisKey, Duration.ofHours(1)); // 1 hour window
        
        return true;
    }
    
    private int calculateCost(String path, Map<String, String> queryParams) {
        int baseCost = operationCosts.getOrDefault(path, 1);
        
        // Dynamic cost calculation based on query parameters
        if (queryParams.containsKey("include_relations")) {
            baseCost *= 2;
        }
        
        String limitParam = queryParams.get("limit");
        if (limitParam != null) {
            try {
                int limit = Integer.parseInt(limitParam);
                if (limit > 100) {
                    baseCost *= 3;
                }
            } catch (NumberFormatException e) {
                // Use default cost
            }
        }
        
        return baseCost;
    }
}

@RestController
public class CostBasedController {
    
    private final CostBasedRateLimitService rateLimitService;
    
    public CostBasedController(CostBasedRateLimitService rateLimitService) {
        this.rateLimitService = rateLimitService;
    }
    
    @GetMapping("/api/simple")
    public ResponseEntity<SimpleResponse> simpleOperation(
            HttpServletRequest request) {
        
        String clientId = request.getRemoteAddr();
        Map<String, String> queryParams = extractQueryParams(request);
        
        if (!rateLimitService.checkCostBasedLimit("/api/simple", clientId, queryParams)) {
            return ResponseEntity.status(429)
                .header("Retry-After", "3600")
                .body(null);
        }
        
        return ResponseEntity.ok(new SimpleResponse("Success"));
    }
    
    @PostMapping("/api/ai-generate")
    public ResponseEntity<AiResponse> generateAiContent(
            @RequestBody AiRequest request,
            HttpServletRequest httpRequest) {
        
        String clientId = httpRequest.getRemoteAddr();
        Map<String, String> queryParams = extractQueryParams(httpRequest);
        
        if (!rateLimitService.checkCostBasedLimit("/api/ai-generate", clientId, queryParams)) {
            return ResponseEntity.status(429)
                .header("Retry-After", "3600")
                .body(null);
        }
        
        return ResponseEntity.ok(aiService.generate(request));
    }
    
    private Map<String, String> extractQueryParams(HttpServletRequest request) {
        Map<String, String> params = new HashMap<>();
        String queryString = request.getQueryString();
        if (queryString != null) {
            String[] pairs = queryString.split("&");
            for (String pair : pairs) {
                String[] keyValue = pair.split("=");
                if (keyValue.length == 2) {
                    params.put(keyValue[0], keyValue[1]);
                }
            }
        }
        return params;
    }
}
```

## Response Headers

```tusk
# Include rate limit headers
#rate_limit 100 per: "hour" headers: true {
    #api /with-headers {
        # Automatically adds:
        # X-RateLimit-Limit: 100
        # X-RateLimit-Remaining: 95
        # X-RateLimit-Reset: 1640995200
        
        return @data()
    }
}

# Custom headers
#rate_limit {
    limit: 60
    window: 3600
    headers: {
        limit: "X-Rate-Limit"
        remaining: "X-Rate-Remaining"
        reset: "X-Rate-Reset"
        retry_after: "Retry-After"  # On 429 response
    }
} {
    #api /custom-headers {
        @process()
    }
}
```

## Java Response Headers Implementation

```java
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;
import java.time.Instant;

@Service
public class RateLimitHeaderService {
    
    public void addRateLimitHeaders(HttpServletResponse response, 
                                  String clientId, 
                                  RateLimitRule rule) {
        
        String redisKey = "rate_limit:" + clientId;
        String current = redisTemplate.opsForValue().get(redisKey);
        int currentCount = current != null ? Integer.parseInt(current) : 0;
        
        int remaining = Math.max(0, rule.getLimit() - currentCount);
        long resetTime = Instant.now().plusSeconds(rule.getWindow()).getEpochSecond();
        
        response.setHeader("X-RateLimit-Limit", String.valueOf(rule.getLimit()));
        response.setHeader("X-RateLimit-Remaining", String.valueOf(remaining));
        response.setHeader("X-RateLimit-Reset", String.valueOf(resetTime));
    }
    
    public void addRetryAfterHeader(HttpServletResponse response, RateLimitRule rule) {
        response.setHeader("Retry-After", String.valueOf(rule.getWindow()));
    }
}

@RestController
public class HeaderController {
    
    private final RateLimitHeaderService headerService;
    private final RateLimitService rateLimitService;
    
    public HeaderController(RateLimitHeaderService headerService,
                           RateLimitService rateLimitService) {
        this.headerService = headerService;
        this.rateLimitService = rateLimitService;
    }
    
    @GetMapping("/api/with-headers")
    public ResponseEntity<DataResponse> getDataWithHeaders(
            HttpServletRequest request,
            HttpServletResponse response) {
        
        String clientId = request.getRemoteAddr();
        RateLimitRule rule = new RateLimitRule();
        rule.setLimit(100);
        rule.setWindow(3600);
        
        if (!rateLimitService.checkLimit(clientId, rule)) {
            headerService.addRetryAfterHeader(response, rule);
            return ResponseEntity.status(429).body(null);
        }
        
        headerService.addRateLimitHeaders(response, clientId, rule);
        return ResponseEntity.ok(new DataResponse("Success"));
    }
}
```

## Spring Boot Integration

```java
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.data.redis.serializer.StringRedisSerializer;

@SpringBootApplication
public class RateLimitApplication {
    
    public static void main(String[] args) {
        SpringApplication.run(RateLimitApplication.class, args);
    }
    
    @Bean
    public RedisTemplate<String, String> redisTemplate(RedisConnectionFactory connectionFactory) {
        RedisTemplate<String, String> template = new RedisTemplate<>();
        template.setConnectionFactory(connectionFactory);
        template.setKeySerializer(new StringRedisSerializer());
        template.setValueSerializer(new StringRedisSerializer());
        template.setHashKeySerializer(new StringRedisSerializer());
        template.setHashValueSerializer(new StringRedisSerializer());
        return template;
    }
    
    @Bean
    public RateLimitConfig rateLimitConfig() {
        return new RateLimitConfig();
    }
    
    @Bean
    public RateLimitService rateLimitService(RedisTemplate<String, String> redisTemplate,
                                           RateLimitConfig config) {
        return new RateLimitService(redisTemplate, config);
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  rate-limit:
    default-limit: 100
    default-window: 3600
    default-key: "ip"
    default-message: "Too many requests"
    default-response-code: 429
    
    tiers:
      - name: "guest"
        count: 10
        window: 3600
      - name: "user"
        count: 100
        window: 3600
      - name: "premium"
        count: 1000
        window: 3600
    
    rules:
      "/api/search":
        limit: 30
        window: 3600
        message: "Search rate limit exceeded"
      "/api/heavy-operation":
        limit: 5
        window: 3600
        message: "Heavy operation rate limit exceeded"
      "/api/lightweight":
        limit: 200
        window: 3600
        message: "Lightweight operation rate limit exceeded"

spring:
  redis:
    host: localhost
    port: 6379
    database: 0
```

## Performance Considerations

### Redis Optimization
```java
@Service
public class OptimizedRateLimitService {
    
    private final RedisTemplate<String, String> redisTemplate;
    
    public boolean checkLimitOptimized(String key, int limit, int window) {
        // Use Redis Lua script for atomic operations
        String luaScript = """
            local current = redis.call('GET', KEYS[1])
            local count = current and tonumber(current) or 0
            
            if count >= tonumber(ARGV[1]) then
                return 0
            end
            
            redis.call('INCR', KEYS[1])
            redis.call('EXPIRE', KEYS[1], ARGV[2])
            return 1
            """;
        
        DefaultRedisScript<Long> script = new DefaultRedisScript<>();
        script.setScriptText(luaScript);
        script.setResultType(Long.class);
        
        Long result = redisTemplate.execute(script, 
            Collections.singletonList(key), 
            String.valueOf(limit), 
            String.valueOf(window));
        
        return result != null && result == 1L;
    }
}
```

### Caching Strategy
```java
@Service
public class CachedRateLimitService {
    
    private final Cache<String, RateLimitInfo> localCache;
    private final RedisTemplate<String, String> redisTemplate;
    
    public CachedRateLimitService() {
        this.localCache = Caffeine.newBuilder()
            .maximumSize(10_000)
            .expireAfterWrite(Duration.ofMinutes(5))
            .build();
        this.redisTemplate = redisTemplate;
    }
    
    public boolean checkLimitWithCache(String key, int limit, int window) {
        // Check local cache first
        RateLimitInfo cached = localCache.getIfPresent(key);
        if (cached != null && !cached.isExpired()) {
            return cached.getCount() < limit;
        }
        
        // Check Redis
        boolean allowed = checkLimitOptimized(key, limit, window);
        
        // Update local cache
        RateLimitInfo info = new RateLimitInfo();
        info.setCount(allowed ? 1 : limit);
        info.setExpiresAt(Instant.now().plusSeconds(window));
        localCache.put(key, info);
        
        return allowed;
    }
}
```

## Security Best Practices

### Rate Limit Bypass Protection
```java
@Service
public class SecureRateLimitService {
    
    public boolean checkSecureLimit(HttpServletRequest request, RateLimitRule rule) {
        // Use multiple identifiers to prevent bypass
        String clientId = generateSecureClientId(request);
        
        // Check for proxy headers
        String realIp = getRealIpAddress(request);
        String forwardedFor = request.getHeader("X-Forwarded-For");
        String realIpHeader = request.getHeader("X-Real-IP");
        
        // Use the most reliable IP
        String finalIp = realIpHeader != null ? realIpHeader : 
                        forwardedFor != null ? forwardedFor.split(",")[0].trim() : 
                        realIp;
        
        String key = "secure_rate_limit:" + finalIp + ":" + request.getRequestURI();
        return checkLimit(key, rule.getLimit(), rule.getWindow());
    }
    
    private String generateSecureClientId(HttpServletRequest request) {
        // Combine multiple identifiers
        String ip = request.getRemoteAddr();
        String userAgent = request.getHeader("User-Agent");
        String sessionId = request.getSession().getId();
        
        return DigestUtils.sha256Hex(ip + userAgent + sessionId);
    }
    
    private String getRealIpAddress(HttpServletRequest request) {
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

## Testing Rate Limits

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.TestPropertySource;
import org.springframework.beans.factory.annotation.Autowired;

@SpringBootTest
@TestPropertySource(properties = {
    "tusk.rate-limit.default-limit=5",
    "tusk.rate-limit.default-window=60"
})
public class RateLimitTest {
    
    @Autowired
    private RateLimitService rateLimitService;
    
    @Test
    public void testBasicRateLimit() {
        String clientId = "test-client";
        
        // Should allow first 5 requests
        for (int i = 0; i < 5; i++) {
            assertTrue(rateLimitService.checkLimit(clientId, 5, 60));
        }
        
        // Should block the 6th request
        assertFalse(rateLimitService.checkLimit(clientId, 5, 60));
    }
    
    @Test
    public void testRateLimitReset() throws InterruptedException {
        String clientId = "test-client-reset";
        
        // Use a short window for testing
        assertTrue(rateLimitService.checkLimit(clientId, 1, 1));
        assertFalse(rateLimitService.checkLimit(clientId, 1, 1));
        
        // Wait for window to expire
        Thread.sleep(1100);
        
        // Should allow again
        assertTrue(rateLimitService.checkLimit(clientId, 1, 1));
    }
}
```

## Monitoring and Metrics

```java
import io.micrometer.core.instrument.MeterRegistry;
import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.Timer;

@Service
public class MonitoredRateLimitService {
    
    private final RateLimitService rateLimitService;
    private final Counter allowedRequests;
    private final Counter blockedRequests;
    private final Timer checkTimer;
    
    public MonitoredRateLimitService(RateLimitService rateLimitService,
                                   MeterRegistry meterRegistry) {
        this.rateLimitService = rateLimitService;
        this.allowedRequests = Counter.builder("rate_limit.requests.allowed")
            .description("Number of requests allowed by rate limiter")
            .register(meterRegistry);
        this.blockedRequests = Counter.builder("rate_limit.requests.blocked")
            .description("Number of requests blocked by rate limiter")
            .register(meterRegistry);
        this.checkTimer = Timer.builder("rate_limit.check.duration")
            .description("Time taken to check rate limit")
            .register(meterRegistry);
    }
    
    public boolean checkLimitWithMetrics(String clientId, RateLimitRule rule) {
        return checkTimer.record(() -> {
            boolean allowed = rateLimitService.checkLimit(clientId, rule);
            
            if (allowed) {
                allowedRequests.increment();
            } else {
                blockedRequests.increment();
            }
            
            return allowed;
        });
    }
}
```

## Summary

The `#rate_limit` directive in TuskLang provides powerful, flexible rate limiting capabilities for Java applications. With Spring Boot integration, Redis backend, and comprehensive configuration options, you can implement sophisticated rate limiting strategies that protect your APIs while maintaining excellent performance.

Key features include:
- **Multiple rate limiting strategies**: Basic, user-based, API key-based, and cost-based
- **Spring Boot integration**: Seamless integration with Spring Boot applications
- **Redis backend**: High-performance, distributed rate limiting
- **Flexible configuration**: Route-specific, method-specific, and dynamic limits
- **Security features**: Protection against bypass attempts and proxy handling
- **Monitoring**: Built-in metrics and monitoring capabilities
- **Testing support**: Comprehensive testing utilities

The Java implementation provides enterprise-grade rate limiting that scales with your application while maintaining the simplicity and power of TuskLang's declarative syntax. 
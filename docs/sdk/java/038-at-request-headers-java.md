# @ Request Headers - Java Edition

The `@request.headers` object provides access to HTTP request headers in Java applications. This guide covers header processing, authentication, content negotiation, and implementing robust header-based logic using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 HTTP Headers Overview

### Accessing Headers in Java

```java
@RestController
@RequestMapping("/api")
public class ApiController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @GetMapping("/data")
    public ResponseEntity<?> getData(HttpServletRequest request) {
        // TuskLang handles header processing and authentication
        return tuskConfig.getHeaderHandler().processRequest(request);
    }
    
    @PostMapping("/upload")
    public ResponseEntity<?> uploadFile(
            @RequestHeader("Content-Type") String contentType,
            @RequestHeader("Content-Length") long contentLength,
            @RequestHeader(value = "Authorization", required = false) String authorization) {
        
        // TuskLang handles header validation and processing
        return tuskConfig.getHeaderHandler().handleUpload(contentType, contentLength, authorization);
    }
}
```

```tusk
# app.tsk - Header processing configuration
header_handlers: {
    process_request: @lambda({
        # Extract and validate headers
        headers = {
            authorization: @request.headers["Authorization"]
            content_type: @request.headers["Content-Type"]
            user_agent: @request.headers["User-Agent"]
            accept: @request.headers["Accept"]
            accept_language: @request.headers["Accept-Language"]
            accept_encoding: @request.headers["Accept-Encoding"]
            content_length: @int(@request.headers["Content-Length"] ?? "0")
            x_requested_with: @request.headers["X-Requested-With"]
            x_api_key: @request.headers["X-API-Key"]
        }
        
        # Validate authentication
        auth_result = @java.invoke("AuthService", "validateHeaders", headers)
        
        @if(!auth_result.authenticated, {
            return: {
                status: 401
                body: { error: "Authentication required" }
                headers: { "WWW-Authenticate": "Bearer" }
            }
        })
        
        # Process request based on headers
        return: @process_request_with_headers(headers)
    }),
    
    handle_upload: @lambda({
        # Validate upload headers
        upload_validation = @java.invoke("UploadValidator", "validateHeaders", {
            content_type: @request.headers["Content-Type"]
            content_length: @int(@request.headers["Content-Length"] ?? "0")
            authorization: @request.headers["Authorization"]
        })
        
        @if(!upload_validation.valid, {
            return: {
                status: 400
                body: { error: upload_validation.errors }
            }
        })
        
        # Process upload
        return: @java.invoke("UploadService", "process", @request.post)
    })
}
```

## 🔐 Authentication Headers

### Bearer Token Authentication

```tusk
# Bearer token authentication with Java integration
bearer_auth: {
    # Extract and validate Bearer token
    validate_bearer_token: @lambda({
        auth_header = @request.headers["Authorization"]
        
        @if(!auth_header, {
            return: { authenticated: false, error: "No Authorization header" }
        })
        
        # Check Bearer prefix
        @if(!@startsWith(auth_header, "Bearer "), {
            return: { authenticated: false, error: "Invalid Authorization format" }
        })
        
        # Extract token
        token = @substring(auth_header, 7)
        
        # Validate token via Java service
        validation_result = @java.invoke("JwtService", "validateToken", token)
        
        return: {
            authenticated: validation_result.valid
            user: validation_result.user
            permissions: validation_result.permissions
            error: validation_result.error
        }
    }),
    
    # API key authentication
    validate_api_key: @lambda({
        api_key = @request.headers["X-API-Key"]
        
        @if(!api_key, {
            return: { authenticated: false, error: "No API key provided" }
        })
        
        # Validate API key via Java service
        validation_result = @java.invoke("ApiKeyService", "validate", api_key)
        
        return: {
            authenticated: validation_result.valid
            client: validation_result.client
            permissions: validation_result.permissions
            rate_limit: validation_result.rate_limit
            error: validation_result.error
        }
    })
}
```

### Custom Authentication Headers

```tusk
# Custom authentication headers with Java integration
custom_auth: {
    # Multi-factor authentication
    validate_mfa: @lambda({
        # Extract MFA headers
        mfa_headers = {
            user_token: @request.headers["X-User-Token"]
            mfa_code: @request.headers["X-MFA-Code"]
            device_id: @request.headers["X-Device-ID"]
            session_id: @request.headers["X-Session-ID"]
        }
        
        # Validate MFA via Java service
        mfa_result = @java.invoke("MfaService", "validate", mfa_headers)
        
        return: {
            authenticated: mfa_result.valid
            user: mfa_result.user
            session: mfa_result.session
            error: mfa_result.error
        }
    }),
    
    # OAuth token validation
    validate_oauth: @lambda({
        oauth_token = @request.headers["X-OAuth-Token"]
        
        @if(!oauth_token, {
            return: { authenticated: false, error: "No OAuth token provided" }
        })
        
        # Validate OAuth token via Java service
        oauth_result = @java.invoke("OAuthService", "validateToken", oauth_token)
        
        return: {
            authenticated: oauth_result.valid
            user: oauth_result.user
            scopes: oauth_result.scopes
            provider: oauth_result.provider
            error: oauth_result.error
        }
    })
}
```

## 📋 Content Negotiation

### Accept Headers Processing

```tusk
# Content negotiation with Java integration
content_negotiation: {
    # Process Accept header
    process_accept_header: @lambda({
        accept_header = @request.headers["Accept"] ?? "*/*"
        
        # Parse Accept header via Java service
        parsed_accept = @java.invoke("ContentNegotiationService", "parseAccept", accept_header)
        
        # Determine best response format
        best_format = @java.invoke("ContentNegotiationService", "getBestFormat", parsed_accept)
        
        return: {
            preferred_format: best_format.format
            quality: best_format.quality
            supported_formats: parsed_accept.supported
            fallback_format: "application/json"
        }
    }),
    
    # Process Accept-Language header
    process_language_header: @lambda({
        accept_language = @request.headers["Accept-Language"] ?? "en"
        
        # Parse language preferences via Java service
        language_prefs = @java.invoke("LocalizationService", "parseAcceptLanguage", accept_language)
        
        # Determine best language
        best_language = @java.invoke("LocalizationService", "getBestLanguage", language_prefs)
        
        return: {
            preferred_language: best_language.language
            quality: best_language.quality
            fallback_language: "en"
            supported_languages: language_prefs.supported
        }
    }),
    
    # Process Accept-Encoding header
    process_encoding_header: @lambda({
        accept_encoding = @request.headers["Accept-Encoding"] ?? "identity"
        
        # Parse encoding preferences via Java service
        encoding_prefs = @java.invoke("CompressionService", "parseAcceptEncoding", accept_encoding)
        
        # Determine best encoding
        best_encoding = @java.invoke("CompressionService", "getBestEncoding", encoding_prefs)
        
        return: {
            preferred_encoding: best_encoding.encoding
            quality: best_encoding.quality
            supported_encodings: encoding_prefs.supported
        }
    })
}
```

## 🔍 Request Analysis

### User Agent Analysis

```tusk
# User agent analysis with Java integration
user_agent_analysis: {
    # Analyze User-Agent header
    analyze_user_agent: @lambda({
        user_agent = @request.headers["User-Agent"] ?? ""
        
        # Parse User-Agent via Java service
        ua_analysis = @java.invoke("UserAgentService", "analyze", user_agent)
        
        return: {
            browser: ua_analysis.browser
            browser_version: ua_analysis.browser_version
            operating_system: ua_analysis.operating_system
            os_version: ua_analysis.os_version
            device_type: ua_analysis.device_type
            is_mobile: ua_analysis.is_mobile
            is_bot: ua_analysis.is_bot
            capabilities: ua_analysis.capabilities
        }
    }),
    
    # Bot detection
    detect_bot: @lambda({
        user_agent = @request.headers["User-Agent"] ?? ""
        
        # Detect bot via Java service
        bot_detection = @java.invoke("BotDetectionService", "detect", user_agent)
        
        return: {
            is_bot: bot_detection.is_bot
            bot_type: bot_detection.bot_type
            confidence: bot_detection.confidence
            should_block: bot_detection.should_block
        }
    })
}
```

### Request Origin Analysis

```tusk
# Request origin analysis with Java integration
origin_analysis: {
    # Analyze request origin
    analyze_origin: @lambda({
        origin_headers = {
            origin: @request.headers["Origin"]
            referer: @request.headers["Referer"]
            host: @request.headers["Host"]
            x_forwarded_for: @request.headers["X-Forwarded-For"]
            x_real_ip: @request.headers["X-Real-IP"]
            x_forwarded_proto: @request.headers["X-Forwarded-Proto"]
        }
        
        # Analyze origin via Java service
        origin_analysis = @java.invoke("OriginAnalysisService", "analyze", origin_headers)
        
        return: {
            client_ip: origin_analysis.client_ip
            proxy_chain: origin_analysis.proxy_chain
            is_secure: origin_analysis.is_secure
            origin_domain: origin_analysis.origin_domain
            referer_domain: origin_analysis.referer_domain
            is_same_origin: origin_analysis.is_same_origin
            risk_level: origin_analysis.risk_level
        }
    }),
    
    # CORS validation
    validate_cors: @lambda({
        cors_headers = {
            origin: @request.headers["Origin"]
            method: @request.method
            headers: @request.headers["Access-Control-Request-Headers"]
        }
        
        # Validate CORS via Java service
        cors_validation = @java.invoke("CorsService", "validate", cors_headers)
        
        return: {
            allowed: cors_validation.allowed
            allowed_origin: cors_validation.allowed_origin
            allowed_methods: cors_validation.allowed_methods
            allowed_headers: cors_validation.allowed_headers
            max_age: cors_validation.max_age
        }
    })
}
```

## 🔧 Java Service Integration

### Header Processing Service

```java
@Service
public class HeaderProcessingService {
    
    @Autowired
    private JwtService jwtService;
    
    @Autowired
    private ApiKeyService apiKeyService;
    
    @Autowired
    private ContentNegotiationService contentNegotiationService;
    
    @Autowired
    private UserAgentService userAgentService;
    
    public ResponseEntity<?> processRequest(HttpServletRequest request) {
        // Extract headers
        Map<String, String> headers = extractHeaders(request);
        
        // Validate authentication
        AuthenticationResult authResult = validateAuthentication(headers);
        if (!authResult.isAuthenticated()) {
            return ResponseEntity.status(401)
                .header("WWW-Authenticate", "Bearer")
                .body(Map.of("error", "Authentication required"));
        }
        
        // Process content negotiation
        ContentNegotiationResult contentResult = processContentNegotiation(headers);
        
        // Analyze user agent
        UserAgentAnalysis uaAnalysis = userAgentService.analyze(headers.get("User-Agent"));
        
        // Process request based on headers
        return processRequestWithHeaders(headers, authResult, contentResult, uaAnalysis);
    }
    
    private Map<String, String> extractHeaders(HttpServletRequest request) {
        Map<String, String> headers = new HashMap<>();
        
        Enumeration<String> headerNames = request.getHeaderNames();
        while (headerNames.hasMoreElements()) {
            String headerName = headerNames.nextElement();
            String headerValue = request.getHeader(headerName);
            headers.put(headerName, headerValue);
        }
        
        return headers;
    }
    
    private AuthenticationResult validateAuthentication(Map<String, String> headers) {
        // Check for Bearer token
        String authHeader = headers.get("Authorization");
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            return jwtService.validateToken(token);
        }
        
        // Check for API key
        String apiKey = headers.get("X-API-Key");
        if (apiKey != null) {
            return apiKeyService.validate(apiKey);
        }
        
        return new AuthenticationResult(false, "No authentication provided");
    }
    
    private ContentNegotiationResult processContentNegotiation(Map<String, String> headers) {
        String acceptHeader = headers.getOrDefault("Accept", "*/*");
        String acceptLanguage = headers.getOrDefault("Accept-Language", "en");
        String acceptEncoding = headers.getOrDefault("Accept-Encoding", "identity");
        
        return contentNegotiationService.process(acceptHeader, acceptLanguage, acceptEncoding);
    }
    
    private ResponseEntity<?> processRequestWithHeaders(
            Map<String, String> headers,
            AuthenticationResult authResult,
            ContentNegotiationResult contentResult,
            UserAgentAnalysis uaAnalysis) {
        
        // Process request based on headers
        if (uaAnalysis.isBot()) {
            return handleBotRequest(headers, authResult);
        }
        
        if (contentResult.getPreferredFormat().equals("application/json")) {
            return handleJsonRequest(headers, authResult);
        }
        
        return handleDefaultRequest(headers, authResult);
    }
}
```

### Authentication Service

```java
@Service
public class JwtService {
    
    @Value("${jwt.secret}")
    private String jwtSecret;
    
    public AuthenticationResult validateToken(String token) {
        try {
            Claims claims = Jwts.parser()
                .setSigningKey(jwtSecret)
                .parseClaimsJws(token)
                .getBody();
            
            String userId = claims.getSubject();
            List<String> permissions = claims.get("permissions", List.class);
            
            User user = userService.findById(userId);
            if (user == null) {
                return new AuthenticationResult(false, "User not found");
            }
            
            return new AuthenticationResult(true, user, permissions);
            
        } catch (Exception e) {
            return new AuthenticationResult(false, "Invalid token: " + e.getMessage());
        }
    }
}

@Service
public class ApiKeyService {
    
    @Autowired
    private ApiKeyRepository apiKeyRepository;
    
    public AuthenticationResult validate(String apiKey) {
        ApiKey key = apiKeyRepository.findByKey(apiKey);
        
        if (key == null) {
            return new AuthenticationResult(false, "Invalid API key");
        }
        
        if (!key.isActive()) {
            return new AuthenticationResult(false, "API key is inactive");
        }
        
        if (key.isExpired()) {
            return new AuthenticationResult(false, "API key has expired");
        }
        
        // Check rate limiting
        if (!checkRateLimit(key)) {
            return new AuthenticationResult(false, "Rate limit exceeded");
        }
        
        return new AuthenticationResult(true, key.getClient(), key.getPermissions());
    }
    
    private boolean checkRateLimit(ApiKey key) {
        // Implement rate limiting logic
        return true;
    }
}
```

## 🚀 Advanced Header Features

### Header-Based Caching

```tusk
# Header-based caching with Java integration
header_caching: {
    # Generate cache key based on headers
    generate_header_cache_key: @lambda({
        # Extract relevant headers for caching
        cache_headers = {
            authorization: @request.headers["Authorization"]
            accept: @request.headers["Accept"]
            accept_language: @request.headers["Accept-Language"]
            user_agent: @request.headers["User-Agent"]
            x_api_key: @request.headers["X-API-Key"]
        }
        
        # Generate cache key via Java service
        cache_key = @java.invoke("CacheKeyService", "generateFromHeaders", cache_headers)
        
        return: cache_key
    }),
    
    # Cache response based on headers
    cache_header_response: @lambda({
        cache_key = @generate_header_cache_key()
        
        # Check cache
        cached_response = @cache.get(cache_key)
        @if(cached_response, return: cached_response)
        
        # Process request
        response = @process_request()
        
        # Cache response
        @cache.set(cache_key, response, "5m")
        
        return: response
    })
}
```

### Header Analytics

```tusk
# Header analytics with Java integration
header_analytics: {
    # Track header metrics
    track_header_metrics: @lambda({
        # Extract header metrics
        header_metrics = {
            user_agent: @request.headers["User-Agent"]
            accept_language: @request.headers["Accept-Language"]
            accept_encoding: @request.headers["Accept-Encoding"]
            content_type: @request.headers["Content-Type"]
            content_length: @int(@request.headers["Content-Length"] ?? "0")
            origin: @request.headers["Origin"]
            referer: @request.headers["Referer"]
            timestamp: @time.now()
            ip_address: @request.ip
        }
        
        # Send metrics to Java analytics service
        @java.invoke("HeaderAnalyticsService", "track", header_metrics)
    }),
    
    # Analyze header patterns
    analyze_header_patterns: @lambda({
        # Get header analysis from Java service
        analysis = @java.invoke("HeaderAnalysisService", "analyze", @request.headers)
        
        # Log insights
        @if(analysis.insights, {
            @log.info("Header insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

## 🔒 Security Considerations

### Header Security Patterns

```tusk
# Header security configuration
header_security: {
    # Header validation
    header_validation: {
        max_header_size: "8KB"
        allowed_headers: ["Authorization", "Content-Type", "Accept", "User-Agent"]
        block_suspicious_headers: true
        validate_header_format: true
    },
    
    # Rate limiting based on headers
    header_rate_limiting: {
        enabled: true
        max_requests_per_minute: 100
        max_requests_per_hour: 1000
        block_on_exceed: true
        rate_limit_by_ip: true
        rate_limit_by_user_agent: false
    },
    
    # Header logging
    header_logging: {
        enabled: true
        log_level: "info"
        mask_sensitive_headers: ["Authorization", "X-API-Key"]
        log_user_agent: true
        log_ip_address: true
    }
}
```

## 🧪 Testing Header Handlers

### Header Testing Configuration

```tusk
# Header testing configuration
header_testing: {
    # Test cases for authentication headers
    auth_test_cases: [
        {
            name: "valid_bearer_token"
            headers: { "Authorization": "Bearer valid_token_here" }
            expected: { authenticated: true, has_user: true }
        },
        {
            name: "invalid_bearer_token"
            headers: { "Authorization": "Bearer invalid_token" }
            expected: { authenticated: false, has_error: true }
        },
        {
            name: "valid_api_key"
            headers: { "X-API-Key": "valid_api_key_here" }
            expected: { authenticated: true, has_client: true }
        },
        {
            name: "missing_auth"
            headers: {}
            expected: { authenticated: false, has_error: true }
        }
    ],
    
    # Test cases for content negotiation
    content_negotiation_test_cases: [
        {
            name: "json_preference"
            headers: { "Accept": "application/json, text/plain;q=0.9" }
            expected: { preferred_format: "application/json" }
        },
        {
            name: "xml_preference"
            headers: { "Accept": "application/xml, application/json;q=0.8" }
            expected: { preferred_format: "application/xml" }
        },
        {
            name: "language_preference"
            headers: { "Accept-Language": "es-ES,es;q=0.9,en;q=0.8" }
            expected: { preferred_language: "es-ES" }
        }
    ]
}
```

## 🚀 Best Practices

### Header Handling Best Practices

1. **Use Java Services**: Delegate header processing to Java services for better maintainability
2. **Implement Proper Validation**: Use Java validation services for header validation
3. **Handle Authentication Securely**: Implement proper token validation and API key management
4. **Use Content Negotiation**: Respect client preferences for content type and language
5. **Implement Rate Limiting**: Use headers for rate limiting and abuse prevention
6. **Log Header Analytics**: Track header patterns for optimization and security
7. **Handle Errors Gracefully**: Return appropriate HTTP status codes and error messages
8. **Validate Headers**: Always validate and sanitize header values

### Common Patterns

```tusk
# Common header handling patterns
common_patterns: {
    # Authentication patterns
    auth_patterns: {
        bearer_token: "JWT token authentication with validation"
        api_key: "API key authentication with rate limiting"
        oauth_token: "OAuth token validation with scopes"
        mfa_token: "Multi-factor authentication with device validation"
    },
    
    # Content negotiation patterns
    content_patterns: {
        accept_header: "Content type negotiation based on Accept header"
        language_negotiation: "Language preference based on Accept-Language"
        encoding_negotiation: "Compression based on Accept-Encoding"
        charset_negotiation: "Character encoding based on Accept-Charset"
    },
    
    # Security patterns
    security_patterns: {
        cors_validation: "Cross-origin request validation"
        origin_validation: "Request origin analysis and validation"
        bot_detection: "Bot detection based on User-Agent"
        rate_limiting: "Rate limiting based on client headers"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle HTTP headers with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're implementing authentication, content negotiation, or security validation, TuskLang provides the tools you need to process headers efficiently and securely. 
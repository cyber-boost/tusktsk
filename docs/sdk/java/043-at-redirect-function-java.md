# @ Redirect Function - Java Edition

The `@redirect` function provides HTTP redirection capabilities in Java applications. This guide covers URL redirection, response handling, redirect chains, and implementing robust redirect logic using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 Redirect Function Overview

### HTTP Redirection in Java

```java
@RestController
@RequestMapping("/api")
public class RedirectController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @GetMapping("/redirect")
    public ResponseEntity<?> handleRedirect(@RequestParam String target) {
        // TuskLang handles redirect logic and processing
        return tuskConfig.getRedirectHandler().processRedirect(target);
    }
    
    @PostMapping("/redirect-after-action")
    public ResponseEntity<?> redirectAfterAction(@RequestBody ActionRequest request) {
        // TuskLang handles post-action redirection
        return tuskConfig.getRedirectHandler().redirectAfterAction(request);
    }
    
    @GetMapping("/smart-redirect")
    public ResponseEntity<?> smartRedirect(HttpServletRequest request) {
        // TuskLang handles intelligent redirection based on context
        return tuskConfig.getRedirectHandler().smartRedirect(request);
    }
}
```

```tusk
# app.tsk - Redirect handling configuration
redirect_handlers: {
    process_redirect: @lambda({
        target_url = @request.query.target ?? "/"
        
        # Validate redirect URL via Java service
        validation = @java.invoke("RedirectService", "validateUrl", target_url)
        
        @if(!validation.valid, {
            return: {
                status: 400
                body: { error: "Invalid redirect URL" }
            }
        })
        
        # Check redirect permissions via Java service
        permissions = @java.invoke("RedirectService", "checkPermissions", target_url, @session.get("user_id"))
        
        @if(!permissions.allowed, {
            return: {
                status: 403
                body: { error: "Redirect not allowed" }
            }
        })
        
        # Perform redirect
        redirect_result = @redirect(target_url, {
            status: 302
            preserve_query: true
            add_timestamp: true
        })
        
        return: redirect_result
    }),
    
    redirect_after_action: @lambda({
        # Process action via Java service
        action_result = @java.invoke("ActionService", "processAction", @request.post)
        
        @if(!action_result.success, {
            return: {
                status: 400
                body: { error: action_result.error }
            }
        })
        
        # Determine redirect URL based on action result
        redirect_url = @java.invoke("RedirectService", "getRedirectUrl", {
            action: @request.post.action
            result: action_result
            user_id: @session.get("user_id")
        })
        
        # Add success message to session
        @session.set("success_message", action_result.message)
        
        # Perform redirect
        redirect_result = @redirect(redirect_url, {
            status: 302
            preserve_query: false
            add_timestamp: true
        })
        
        return: redirect_result
    }),
    
    smart_redirect: @lambda({
        # Analyze request context via Java service
        context_analysis = @java.invoke("RedirectService", "analyzeContext", {
            path: @request.path
            method: @request.method
            user_agent: @request.headers["User-Agent"]
            referer: @request.headers["Referer"]
            user_id: @session.get("user_id")
        })
        
        # Determine optimal redirect URL
        optimal_url = @java.invoke("RedirectService", "getOptimalRedirect", context_analysis)
        
        # Perform smart redirect
        redirect_result = @redirect(optimal_url, {
            status: 302
            preserve_query: true
            add_timestamp: true
            add_analytics: true
        })
        
        return: redirect_result
    })
}
```

## 🔄 Basic Redirect Operations

### Simple Redirects

```tusk
# Basic redirect operations with Java integration
basic_redirects: {
    # Simple redirect
    simple_redirect: @lambda(url, {
        # Basic redirect with default settings
        redirect_result = @redirect(url)
        
        return: redirect_result
    }),
    
    # Redirect with status code
    redirect_with_status: @lambda(url, status_code = 302, {
        # Redirect with specific status code
        redirect_result = @redirect(url, {
            status: status_code
        })
        
        return: redirect_result
    }),
    
    # Redirect with options
    redirect_with_options: @lambda(url, options = {}, {
        # Default options
        default_options = {
            status: 302
            preserve_query: false
            add_timestamp: false
            add_analytics: false
        }
        
        # Merge with provided options
        redirect_options = @merge(default_options, options)
        
        # Perform redirect
        redirect_result = @redirect(url, redirect_options)
        
        return: redirect_result
    }),
    
    # Conditional redirect
    conditional_redirect: @lambda(condition, true_url, false_url, {
        @if(condition, {
            return: @redirect(true_url)
        }, {
            return: @redirect(false_url)
        }
    })
}
```

### Advanced Redirect Features

```tusk
# Advanced redirect features with Java integration
advanced_redirects: {
    # Redirect with query parameters
    redirect_with_query: @lambda(base_url, query_params, {
        # Build URL with query parameters
        full_url = @java.invoke("UrlBuilder", "buildWithQuery", base_url, query_params)
        
        # Perform redirect
        redirect_result = @redirect(full_url)
        
        return: redirect_result
    }),
    
    # Redirect with session data
    redirect_with_session: @lambda(url, session_data, {
        # Store data in session
        @each(@keys(session_data), @lambda(key, {
            @session.set(key, session_data[key])
        }))
        
        # Perform redirect
        redirect_result = @redirect(url)
        
        return: redirect_result
    }),
    
    # Redirect chain
    redirect_chain: @lambda(urls, {
        # Validate redirect chain via Java service
        chain_validation = @java.invoke("RedirectService", "validateChain", urls)
        
        @if(!chain_validation.valid, {
            return: { error: "Invalid redirect chain" }
        })
        
        # Store chain in session
        @session.set("redirect_chain", urls)
        @session.set("current_chain_index", 0)
        
        # Start with first URL
        first_url = urls[0]
        redirect_result = @redirect(first_url)
        
        return: redirect_result
    }),
    
    # Redirect with analytics
    redirect_with_analytics: @lambda(url, analytics_data, {
        # Track redirect via Java service
        @java.invoke("AnalyticsService", "trackRedirect", {
            from_url: @request.path
            to_url: url
            user_id: @session.get("user_id")
            timestamp: @time.now()
            analytics_data: analytics_data
        })
        
        # Perform redirect
        redirect_result = @redirect(url)
        
        return: redirect_result
    })
}
```

## 🎯 Smart Redirect Logic

### Context-Aware Redirects

```tusk
# Smart redirect logic with Java integration
smart_redirects: {
    # User role-based redirect
    role_based_redirect: @lambda({
        user_id = @session.get("user_id")
        
        @if(!user_id, {
            return: @redirect("/login")
        })
        
        # Get user role via Java service
        user_role = @java.invoke("UserService", "getUserRole", user_id)
        
        # Redirect based on role
        @switch(user_role, {
            "admin": @redirect("/admin/dashboard")
            "manager": @redirect("/manager/dashboard")
            "user": @redirect("/user/dashboard")
            default: @redirect("/dashboard")
        })
    }),
    
    # Device-based redirect
    device_based_redirect: @lambda({
        user_agent = @request.headers["User-Agent"]
        
        # Detect device type via Java service
        device_type = @java.invoke("DeviceService", "detectDevice", user_agent)
        
        # Redirect based on device
        @switch(device_type, {
            "mobile": @redirect("/mobile")
            "tablet": @redirect("/tablet")
            "desktop": @redirect("/desktop")
            default: @redirect("/")
        })
    }),
    
    # Language-based redirect
    language_based_redirect: @lambda({
        accept_language = @request.headers["Accept-Language"]
        
        # Parse language preferences via Java service
        language_prefs = @java.invoke("LocalizationService", "parseAcceptLanguage", accept_language)
        
        # Get preferred language
        preferred_language = @java.invoke("LocalizationService", "getPreferredLanguage", language_prefs)
        
        # Redirect to localized version
        localized_url = @java.invoke("LocalizationService", "getLocalizedUrl", @request.path, preferred_language)
        
        return: @redirect(localized_url)
    }),
    
    # A/B test redirect
    ab_test_redirect: @lambda({
        user_id = @session.get("user_id")
        test_name = @request.query.test ?? "default"
        
        # Get A/B test variant via Java service
        variant = @java.invoke("AbTestService", "getVariant", user_id, test_name)
        
        # Redirect based on variant
        @switch(variant, {
            "A": @redirect("/version-a")
            "B": @redirect("/version-b")
            default: @redirect("/default")
        })
    })
}
```

### Business Logic Redirects

```tusk
# Business logic redirects with Java integration
business_redirects: {
    # Authentication redirect
    auth_redirect: @lambda({
        user_id = @session.get("user_id")
        
        @if(!user_id, {
            # Store intended destination
            @session.set("redirect_after_login", @request.path)
            return: @redirect("/login")
        })
        
        # Check if user needs to complete profile
        profile_complete = @java.invoke("UserService", "isProfileComplete", user_id)
        
        @if(!profile_complete, {
            return: @redirect("/complete-profile")
        }
        
        # Check if user needs to verify email
        email_verified = @java.invoke("UserService", "isEmailVerified", user_id)
        
        @if(!email_verified, {
            return: @redirect("/verify-email")
        }
        
        # User is authenticated and verified
        return: @redirect("/dashboard")
    }),
    
    # Payment redirect
    payment_redirect: @lambda({
        order_id = @request.query.order_id
        
        @if(!order_id, {
            return: @redirect("/orders")
        })
        
        # Get order status via Java service
        order_status = @java.invoke("OrderService", "getOrderStatus", order_id)
        
        # Redirect based on order status
        @switch(order_status, {
            "pending": @redirect("/payment?order_id=${order_id}")
            "paid": @redirect("/order-confirmation?order_id=${order_id}")
            "cancelled": @redirect("/order-cancelled?order_id=${order_id}")
            "refunded": @redirect("/order-refunded?order_id=${order_id}")
            default: @redirect("/orders")
        })
    }),
    
    # Feature flag redirect
    feature_flag_redirect: @lambda({
        feature_name = @request.query.feature
        user_id = @session.get("user_id")
        
        @if(!feature_name, {
            return: @redirect("/features")
        })
        
        # Check feature flag via Java service
        feature_enabled = @java.invoke("FeatureFlagService", "isEnabled", feature_name, user_id)
        
        @if(feature_enabled, {
            return: @redirect("/features/${feature_name}")
        }, {
            return: @redirect("/feature-unavailable?feature=${feature_name}")
        }
    })
}
```

## 🔧 Java Service Integration

### Redirect Processing Service

```java
@Service
public class RedirectProcessingService {
    
    @Autowired
    private RedirectService redirectService;
    
    @Autowired
    private UserService userService;
    
    @Autowired
    private ActionService actionService;
    
    public ResponseEntity<?> processRedirect(String target) {
        try {
            // Validate redirect URL
            RedirectValidationResult validation = redirectService.validateUrl(target);
            if (!validation.isValid()) {
                return ResponseEntity.badRequest()
                    .body(Map.of("error", "Invalid redirect URL"));
            }
            
            // Check permissions
            RedirectPermissionResult permissions = redirectService.checkPermissions(target, getCurrentUserId());
            if (!permissions.isAllowed()) {
                return ResponseEntity.status(403)
                    .body(Map.of("error", "Redirect not allowed"));
            }
            
            // Build redirect response
            String redirectUrl = buildRedirectUrl(target);
            
            return ResponseEntity.status(302)
                .header("Location", redirectUrl)
                .body(Map.of("redirect_url", redirectUrl));
                
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Redirect processing failed", "details", e.getMessage()));
        }
    }
    
    public ResponseEntity<?> redirectAfterAction(ActionRequest request) {
        try {
            // Process action
            ActionResult actionResult = actionService.processAction(request);
            if (!actionResult.isSuccess()) {
                return ResponseEntity.badRequest()
                    .body(Map.of("error", actionResult.getError()));
            }
            
            // Determine redirect URL
            String redirectUrl = redirectService.getRedirectUrl(buildRedirectContext(request, actionResult));
            
            // Add success message to session
            session.setAttribute("success_message", actionResult.getMessage());
            
            // Build redirect response
            String finalRedirectUrl = buildRedirectUrl(redirectUrl);
            
            return ResponseEntity.status(302)
                .header("Location", finalRedirectUrl)
                .body(Map.of("redirect_url", finalRedirectUrl));
                
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Post-action redirect failed", "details", e.getMessage()));
        }
    }
    
    public ResponseEntity<?> smartRedirect(HttpServletRequest request) {
        try {
            // Analyze context
            RedirectContext context = buildRedirectContext(request);
            ContextAnalysisResult analysis = redirectService.analyzeContext(context);
            
            // Get optimal redirect URL
            String optimalUrl = redirectService.getOptimalRedirect(analysis);
            
            // Build redirect response
            String finalRedirectUrl = buildRedirectUrl(optimalUrl);
            
            return ResponseEntity.status(302)
                .header("Location", finalRedirectUrl)
                .body(Map.of("redirect_url", finalRedirectUrl));
                
        } catch (Exception e) {
            return ResponseEntity.internalServerError()
                .body(Map.of("error", "Smart redirect failed", "details", e.getMessage()));
        }
    }
    
    private String getCurrentUserId() {
        return (String) session.getAttribute("user_id");
    }
    
    private String buildRedirectUrl(String target) {
        // Add timestamp if needed
        if (shouldAddTimestamp()) {
            target = target + (target.contains("?") ? "&" : "?") + "t=" + System.currentTimeMillis();
        }
        
        return target;
    }
    
    private boolean shouldAddTimestamp() {
        // Implementation to determine if timestamp should be added
        return false;
    }
    
    private RedirectContext buildRedirectContext(ActionRequest request, ActionResult actionResult) {
        RedirectContext context = new RedirectContext();
        context.setAction(request.getAction());
        context.setActionResult(actionResult);
        context.setUserId(getCurrentUserId());
        return context;
    }
    
    private RedirectContext buildRedirectContext(HttpServletRequest request) {
        RedirectContext context = new RedirectContext();
        context.setPath(request.getRequestURI());
        context.setMethod(request.getMethod());
        context.setUserAgent(request.getHeader("User-Agent"));
        context.setReferer(request.getHeader("Referer"));
        context.setUserId(getCurrentUserId());
        return context;
    }
}
```

### Redirect Service

```java
@Service
public class RedirectService {
    
    @Autowired
    private UserService userService;
    
    @Autowired
    private RedirectRepository redirectRepository;
    
    public RedirectValidationResult validateUrl(String url) {
        List<String> errors = new ArrayList<>();
        
        // Basic URL validation
        if (url == null || url.trim().isEmpty()) {
            errors.add("URL is required");
        }
        
        // Check for valid URL format
        try {
            new URL(url);
        } catch (MalformedURLException e) {
            errors.add("Invalid URL format");
        }
        
        // Check for allowed domains
        if (!isAllowedDomain(url)) {
            errors.add("Redirect to external domain not allowed");
        }
        
        // Check for redirect loops
        if (isRedirectLoop(url)) {
            errors.add("Redirect loop detected");
        }
        
        return new RedirectValidationResult(errors.isEmpty(), errors);
    }
    
    public RedirectPermissionResult checkPermissions(String url, String userId) {
        // Check if user has permission to redirect to this URL
        if (userId == null) {
            return new RedirectPermissionResult(false, "User not authenticated");
        }
        
        User user = userService.findById(userId);
        if (user == null) {
            return new RedirectPermissionResult(false, "User not found");
        }
        
        // Check user permissions
        if (!user.hasPermission("redirect")) {
            return new RedirectPermissionResult(false, "Insufficient permissions");
        }
        
        // Check URL-specific permissions
        if (url.startsWith("/admin") && !user.hasRole("ADMIN")) {
            return new RedirectPermissionResult(false, "Admin access required");
        }
        
        return new RedirectPermissionResult(true, null);
    }
    
    public String getRedirectUrl(RedirectContext context) {
        // Determine redirect URL based on context
        switch (context.getAction()) {
            case "login":
                return "/dashboard";
            case "logout":
                return "/login";
            case "create":
                return "/list";
            case "update":
                return "/view/" + context.getActionResult().getEntityId();
            case "delete":
                return "/list";
            default:
                return "/";
        }
    }
    
    public ContextAnalysisResult analyzeContext(RedirectContext context) {
        ContextAnalysisResult analysis = new ContextAnalysisResult();
        
        // Analyze user context
        if (context.getUserId() != null) {
            User user = userService.findById(context.getUserId());
            analysis.setUserRole(user.getRole());
            analysis.setUserPreferences(user.getPreferences());
        }
        
        // Analyze device context
        analysis.setDeviceType(detectDeviceType(context.getUserAgent()));
        
        // Analyze path context
        analysis.setPathType(detectPathType(context.getPath()));
        
        return analysis;
    }
    
    public String getOptimalRedirect(ContextAnalysisResult analysis) {
        // Determine optimal redirect based on analysis
        if (analysis.getUserRole() != null) {
            switch (analysis.getUserRole()) {
                case "ADMIN":
                    return "/admin/dashboard";
                case "MANAGER":
                    return "/manager/dashboard";
                case "USER":
                    return "/user/dashboard";
            }
        }
        
        return "/dashboard";
    }
    
    private boolean isAllowedDomain(String url) {
        // Check if URL is for allowed domain
        try {
            String host = new URL(url).getHost();
            return host.equals("localhost") || host.equals("tusklang.org");
        } catch (MalformedURLException e) {
            return false;
        }
    }
    
    private boolean isRedirectLoop(String url) {
        // Check for redirect loops
        // Implementation details...
        return false;
    }
    
    private String detectDeviceType(String userAgent) {
        // Detect device type from user agent
        if (userAgent == null) return "unknown";
        
        userAgent = userAgent.toLowerCase();
        if (userAgent.contains("mobile")) return "mobile";
        if (userAgent.contains("tablet")) return "tablet";
        return "desktop";
    }
    
    private String detectPathType(String path) {
        // Detect path type
        if (path == null) return "unknown";
        
        if (path.startsWith("/admin")) return "admin";
        if (path.startsWith("/api")) return "api";
        if (path.startsWith("/static")) return "static";
        return "content";
    }
}
```

## 🚀 Advanced Redirect Features

### Redirect Analytics

```tusk
# Redirect analytics with Java integration
redirect_analytics: {
    # Track redirect metrics
    track_redirect_metrics: @lambda({
        # Extract redirect metrics
        redirect_metrics = {
            from_url: @request.path
            to_url: @request.query.target
            redirect_type: "manual"
            user_id: @session.get("user_id")
            user_agent: @request.headers["User-Agent"]
            referer: @request.headers["Referer"]
            timestamp: @time.now()
            processing_time: @request.processing_time
        }
        
        # Send metrics to Java analytics service
        @java.invoke("RedirectAnalyticsService", "track", redirect_metrics)
    }),
    
    # Analyze redirect patterns
    analyze_redirect_patterns: @lambda(from_url, {
        # Get redirect analysis from Java service
        analysis = @java.invoke("RedirectAnalysisService", "analyze", from_url)
        
        # Log insights
        @if(analysis.insights, {
            @log.info("Redirect insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

### Redirect Caching

```tusk
# Redirect caching with Java integration
redirect_caching: {
    # Cache redirect decisions
    cache_redirect_decision: @lambda(context, redirect_url, ttl = "1h", {
        # Generate cache key
        cache_key = @java.invoke("CacheKeyService", "generateForRedirect", context)
        
        # Cache redirect decision
        @cache.set(cache_key, redirect_url, ttl)
        
        return: { success: true, cached_key: cache_key }
    }),
    
    # Get cached redirect
    get_cached_redirect: @lambda(context, {
        # Generate cache key
        cache_key = @java.invoke("CacheKeyService", "generateForRedirect", context)
        
        # Get cached redirect
        cached_redirect = @cache.get(cache_key)
        
        return: { success: true, redirect_url: cached_redirect }
    })
}
```

## 🔒 Security Considerations

### Redirect Security Patterns

```tusk
# Redirect security configuration
redirect_security_patterns: {
    # URL validation
    url_validation: {
        allowed_domains: ["localhost", "tusklang.org"]
        allowed_protocols: ["http", "https"]
        max_redirect_depth: 5
        block_external_redirects: true
        validate_redirect_chain: true
    },
    
    # Permission validation
    permission_validation: {
        require_authentication: true
        check_user_permissions: true
        validate_redirect_targets: true
        block_admin_redirects: false
    },
    
    # Redirect logging
    redirect_logging: {
        enabled: true
        log_level: "info"
        log_all_redirects: true
        log_redirect_errors: true
        mask_sensitive_data: true
    }
}
```

## 🧪 Testing Redirect Handlers

### Redirect Testing Configuration

```tusk
# Redirect testing configuration
redirect_testing: {
    # Test cases for basic redirects
    basic_redirect_test_cases: [
        {
            name: "simple_redirect"
            target: "/dashboard"
            expected: { status: 302, location: "/dashboard" }
        },
        {
            name: "redirect_with_query"
            target: "/search?q=test"
            expected: { status: 302, location: "/search?q=test" }
        },
        {
            name: "invalid_redirect"
            target: "javascript:alert('xss')"
            expected: { status: 400, has_error: true }
        }
    ],
    
    # Test cases for smart redirects
    smart_redirect_test_cases: [
        {
            name: "role_based_redirect_admin"
            user_role: "admin"
            expected: { status: 302, location: "/admin/dashboard" }
        },
        {
            name: "role_based_redirect_user"
            user_role: "user"
            expected: { status: 302, location: "/user/dashboard" }
        },
        {
            name: "device_based_redirect_mobile"
            user_agent: "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)"
            expected: { status: 302, location: "/mobile" }
        }
    ]
}
```

## 🚀 Best Practices

### Redirect Handling Best Practices

1. **Use Java Services**: Delegate redirect logic to Java services for better maintainability
2. **Validate URLs**: Always validate redirect URLs to prevent security issues
3. **Check Permissions**: Verify user permissions before allowing redirects
4. **Handle Redirect Loops**: Implement protection against redirect loops
5. **Use Analytics**: Track redirect patterns for optimization
6. **Implement Caching**: Cache redirect decisions for performance
7. **Handle Errors Gracefully**: Provide fallback redirects for failures
8. **Log Redirects**: Log redirect activities for security and debugging

### Common Patterns

```tusk
# Common redirect handling patterns
common_patterns: {
    # Authentication patterns
    auth_patterns: {
        login_redirect: "Redirect to login page when authentication required"
        logout_redirect: "Redirect after successful logout"
        session_expired_redirect: "Redirect when session expires"
        permission_denied_redirect: "Redirect when access is denied"
    },
    
    # Business logic patterns
    business_patterns: {
        post_action_redirect: "Redirect after completing an action"
        workflow_redirect: "Redirect through multi-step workflows"
        error_redirect: "Redirect to error pages"
        success_redirect: "Redirect to success pages"
    },
    
    # User experience patterns
    ux_patterns: {
        device_redirect: "Redirect based on device type"
        language_redirect: "Redirect based on language preference"
        ab_test_redirect: "Redirect based on A/B test variants"
        personalization_redirect: "Redirect based on user preferences"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle HTTP redirections with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're implementing authentication flows, business logic redirects, or smart user experience features, TuskLang provides the tools you need to handle redirects efficiently and securely. 
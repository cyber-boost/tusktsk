# @ Cookie Variables - Java Edition

The `@cookie` object provides access to HTTP cookies in Java applications. This guide covers cookie management, user preferences, tracking, and implementing robust cookie-based logic using TuskLang with Spring Boot integration and Java enterprise patterns.

## 🎯 Cookie Variables Overview

### Accessing Cookies in Java

```java
@RestController
@RequestMapping("/api")
public class CookieController {
    
    @Autowired
    private TuskConfig tuskConfig;
    
    @GetMapping("/preferences")
    public ResponseEntity<?> getPreferences(@CookieValue(value = "user_prefs", required = false) String userPrefs) {
        // TuskLang handles cookie processing and preferences
        return tuskConfig.getCookieHandler().getUserPreferences(userPrefs);
    }
    
    @PostMapping("/set-preference")
    public ResponseEntity<?> setPreference(@RequestBody PreferenceRequest request, HttpServletResponse response) {
        // TuskLang handles cookie creation and management
        return tuskConfig.getCookieHandler().setUserPreference(request, response);
    }
    
    @GetMapping("/track")
    public ResponseEntity<?> track(@CookieValue(value = "tracking_id", required = false) String trackingId, 
                                  HttpServletResponse response) {
        // TuskLang handles tracking cookie management
        return tuskConfig.getCookieHandler().handleTracking(trackingId, response);
    }
}
```

```tusk
# app.tsk - Cookie handling configuration
cookie_handlers: {
    get_user_preferences: @lambda({
        # Access cookie variables
        user_prefs_cookie = @cookie.get("user_prefs")
        
        @if(!user_prefs_cookie, {
            # Return default preferences
            return: {
                status: 200
                body: { 
                    preferences: {
                        theme: "default"
                        language: "en"
                        timezone: "UTC"
                        notifications: true
                    }
                }
            }
        })
        
        # Parse preferences from cookie
        preferences = @json.parse(user_prefs_cookie)
        
        # Validate preferences via Java service
        validated_prefs = @java.invoke("PreferenceService", "validatePreferences", preferences)
        
        return: {
            status: 200
            body: { preferences: validated_prefs }
        }
    }),
    
    set_user_preference: @lambda({
        # Extract preference data
        preference_data = {
            key: @request.post.key
            value: @request.post.value
            expires: @request.post.expires ?? "30d"
        }
        
        # Validate preference via Java service
        validation = @java.invoke("PreferenceService", "validatePreference", preference_data)
        
        @if(!validation.valid, {
            return: {
                status: 400
                body: { error: validation.errors }
            }
        })
        
        # Get current preferences
        current_prefs = @cookie.get("user_prefs")
        preferences = current_prefs ? @json.parse(current_prefs) : {}
        
        # Update preference
        preferences[preference_data.key] = preference_data.value
        
        # Set cookie with updated preferences
        @cookie.set("user_prefs", @json.stringify(preferences), {
            expires: preference_data.expires
            path: "/"
            secure: true
            httpOnly: false
            sameSite: "strict"
        })
        
        return: {
            status: 200
            body: { 
                message: "Preference updated",
                preferences: preferences
            }
        }
    }),
    
    handle_tracking: @lambda({
        tracking_id = @cookie.get("tracking_id")
        
        @if(!tracking_id, {
            # Generate new tracking ID
            tracking_id = @java.invoke("TrackingService", "generateTrackingId")
            
            # Set tracking cookie
            @cookie.set("tracking_id", tracking_id, {
                expires: "1y"
                path: "/"
                secure: true
                httpOnly: true
                sameSite: "strict"
            })
        })
        
        # Track visit via Java service
        @java.invoke("TrackingService", "trackVisit", tracking_id, {
            page: @request.path
            referer: @request.headers["Referer"]
            user_agent: @request.headers["User-Agent"]
            ip_address: @request.ip
            timestamp: @time.now()
        })
        
        return: {
            status: 200
            body: { tracking_id: tracking_id }
        }
    })
}
```

## 🍪 Cookie Management

### Cookie Creation and Validation

```tusk
# Cookie management with Java integration
cookie_management: {
    # Create secure cookie
    create_secure_cookie: @lambda(name, value, options = {}, {
        # Default cookie options
        default_options = {
            expires: "30d"
            path: "/"
            secure: true
            httpOnly: true
            sameSite: "strict"
            domain: null
        }
        
        # Merge with provided options
        cookie_options = @merge(default_options, options)
        
        # Validate cookie via Java service
        validation = @java.invoke("CookieService", "validateCookie", {
            name: name
            value: value
            options: cookie_options
        })
        
        @if(!validation.valid, {
            return: { success: false, error: validation.errors }
        })
        
        # Set cookie
        @cookie.set(name, value, cookie_options)
        
        return: { success: true, cookie_name: name }
    }),
    
    # Read cookie safely
    read_cookie_safely: @lambda(name, default_value = null, {
        cookie_value = @cookie.get(name)
        
        @if(!cookie_value, {
            return: default_value
        })
        
        # Validate cookie value via Java service
        validation = @java.invoke("CookieService", "validateCookieValue", name, cookie_value)
        
        @if(!validation.valid, {
            # Remove invalid cookie
            @cookie.remove(name)
            return: default_value
        })
        
        return: cookie_value
    }),
    
    # Update cookie
    update_cookie: @lambda(name, new_value, options = {}, {
        # Check if cookie exists
        current_value = @cookie.get(name)
        
        @if(!current_value, {
            return: { success: false, error: "Cookie not found" }
        })
        
        # Update cookie
        result = @create_secure_cookie(name, new_value, options)
        
        return: result
    }),
    
    # Delete cookie
    delete_cookie: @lambda(name, {
        # Remove cookie
        @cookie.remove(name)
        
        # Also remove via Java service for tracking
        @java.invoke("CookieService", "trackCookieRemoval", name)
        
        return: { success: true, message: "Cookie removed" }
    })
}
```

### User Preference Cookies

```tusk
# User preference cookies with Java integration
preference_cookies: {
    # Set user theme preference
    set_theme_preference: @lambda(theme, {
        # Validate theme
        valid_themes = ["light", "dark", "auto"]
        
        @if(!@includes(valid_themes, theme), {
            return: { success: false, error: "Invalid theme" }
        })
        
        # Set theme cookie
        @cookie.set("theme", theme, {
            expires: "1y"
            path: "/"
            secure: true
            httpOnly: false
            sameSite: "strict"
        })
        
        # Track preference change via Java service
        @java.invoke("PreferenceService", "trackThemeChange", theme)
        
        return: { success: true, theme: theme }
    }),
    
    # Set language preference
    set_language_preference: @lambda(language, {
        # Validate language
        valid_languages = ["en", "es", "fr", "de", "ja", "zh"]
        
        @if(!@includes(valid_languages, language), {
            return: { success: false, error: "Invalid language" }
        })
        
        # Set language cookie
        @cookie.set("language", language, {
            expires: "1y"
            path: "/"
            secure: true
            httpOnly: false
            sameSite: "strict"
        })
        
        # Track preference change via Java service
        @java.invoke("PreferenceService", "trackLanguageChange", language)
        
        return: { success: true, language: language }
    }),
    
    # Set timezone preference
    set_timezone_preference: @lambda(timezone, {
        # Validate timezone via Java service
        validation = @java.invoke("TimezoneService", "validateTimezone", timezone)
        
        @if(!validation.valid, {
            return: { success: false, error: "Invalid timezone" }
        })
        
        # Set timezone cookie
        @cookie.set("timezone", timezone, {
            expires: "1y"
            path: "/"
            secure: true
            httpOnly: false
            sameSite: "strict"
        })
        
        return: { success: true, timezone: timezone }
    }),
    
    # Get all user preferences
    get_all_preferences: @lambda({
        preferences = {
            theme: @read_cookie_safely("theme", "light")
            language: @read_cookie_safely("language", "en")
            timezone: @read_cookie_safely("timezone", "UTC")
            notifications: @read_cookie_safely("notifications", "true") == "true"
            analytics: @read_cookie_safely("analytics", "true") == "true"
        }
        
        return: preferences
    })
}
```

## 📊 Analytics and Tracking Cookies

### User Tracking

```tusk
# Analytics and tracking cookies with Java integration
tracking_cookies: {
    # Initialize user tracking
    init_user_tracking: @lambda({
        # Check for existing tracking ID
        tracking_id = @cookie.get("tracking_id")
        
        @if(!tracking_id, {
            # Generate new tracking ID
            tracking_id = @java.invoke("TrackingService", "generateTrackingId")
            
            # Set tracking cookie
            @cookie.set("tracking_id", tracking_id, {
                expires: "2y"
                path: "/"
                secure: true
                httpOnly: true
                sameSite: "strict"
            })
            
            # Initialize tracking via Java service
            @java.invoke("TrackingService", "initializeTracking", tracking_id, {
                first_visit: @time.now()
                user_agent: @request.headers["User-Agent"]
                ip_address: @request.ip
                referer: @request.headers["Referer"]
            })
        })
        
        return: { tracking_id: tracking_id }
    }),
    
    # Track page view
    track_page_view: @lambda({
        tracking_id = @cookie.get("tracking_id")
        
        @if(!tracking_id, {
            tracking_result = @init_user_tracking()
            tracking_id = tracking_result.tracking_id
        })
        
        # Track page view via Java service
        @java.invoke("TrackingService", "trackPageView", tracking_id, {
            page: @request.path
            title: @request.post.title ?? ""
            referer: @request.headers["Referer"]
            timestamp: @time.now()
            session_id: @session.get("session_id")
        })
        
        return: { success: true }
    }),
    
    # Track user action
    track_user_action: @lambda(action, data = {}, {
        tracking_id = @cookie.get("tracking_id")
        
        @if(!tracking_id, {
            return: { success: false, error: "No tracking ID" }
        })
        
        # Track action via Java service
        @java.invoke("TrackingService", "trackAction", tracking_id, {
            action: action
            data: data
            timestamp: @time.now()
            session_id: @session.get("session_id")
        })
        
        return: { success: true }
    }),
    
    # Get user analytics
    get_user_analytics: @lambda({
        tracking_id = @cookie.get("tracking_id")
        
        @if(!tracking_id, {
            return: { error: "No tracking data available" }
        })
        
        # Get analytics via Java service
        analytics = @java.invoke("AnalyticsService", "getUserAnalytics", tracking_id)
        
        return: analytics
    })
}
```

### Session Tracking

```tusk
# Session tracking cookies with Java integration
session_tracking: {
    # Track session start
    track_session_start: @lambda({
        session_id = @session.get("session_id")
        tracking_id = @cookie.get("tracking_id")
        
        @if(!session_id || !tracking_id, {
            return: { success: false, error: "Missing session or tracking data" }
        })
        
        # Track session start via Java service
        @java.invoke("SessionTrackingService", "trackSessionStart", {
            session_id: session_id
            tracking_id: tracking_id
            start_time: @time.now()
            user_agent: @request.headers["User-Agent"]
            ip_address: @request.ip
        })
        
        return: { success: true }
    }),
    
    # Track session activity
    track_session_activity: @lambda(activity_type, data = {}, {
        session_id = @session.get("session_id")
        tracking_id = @cookie.get("tracking_id")
        
        @if(!session_id || !tracking_id, {
            return: { success: false, error: "Missing session or tracking data" }
        })
        
        # Track activity via Java service
        @java.invoke("SessionTrackingService", "trackActivity", {
            session_id: session_id
            tracking_id: tracking_id
            activity_type: activity_type
            data: data
            timestamp: @time.now()
        })
        
        return: { success: true }
    }),
    
    # Track session end
    track_session_end: @lambda({
        session_id = @session.get("session_id")
        tracking_id = @cookie.get("tracking_id")
        
        @if(!session_id || !tracking_id, {
            return: { success: false, error: "Missing session or tracking data" }
        })
        
        # Track session end via Java service
        @java.invoke("SessionTrackingService", "trackSessionEnd", {
            session_id: session_id
            tracking_id: tracking_id
            end_time: @time.now()
            duration: @time.diff(@session.get("login_time"), @time.now())
        })
        
        return: { success: true }
    })
}
```

## 🔧 Java Service Integration

### Cookie Management Service

```java
@Service
public class CookieManagementService {
    
    @Autowired
    private PreferenceService preferenceService;
    
    @Autowired
    private TrackingService trackingService;
    
    @Autowired
    private CookieValidationService cookieValidationService;
    
    public ResponseEntity<?> getUserPreferences(String userPrefsCookie) {
        if (userPrefsCookie == null) {
            // Return default preferences
            Map<String, Object> defaultPrefs = Map.of(
                "theme", "default",
                "language", "en",
                "timezone", "UTC",
                "notifications", true
            );
            
            return ResponseEntity.ok(Map.of("preferences", defaultPrefs));
        }
        
        try {
            // Parse preferences from cookie
            ObjectMapper mapper = new ObjectMapper();
            Map<String, Object> preferences = mapper.readValue(userPrefsCookie, Map.class);
            
            // Validate preferences
            PreferenceValidationResult validation = preferenceService.validatePreferences(preferences);
            
            return ResponseEntity.ok(Map.of("preferences", validation.getValidatedPreferences()));
            
        } catch (Exception e) {
            return ResponseEntity.ok(Map.of("preferences", preferenceService.getDefaultPreferences()));
        }
    }
    
    public ResponseEntity<?> setUserPreference(PreferenceRequest request, HttpServletResponse response) {
        // Validate preference
        PreferenceValidationResult validation = preferenceService.validatePreference(request);
        
        if (!validation.isValid()) {
            return ResponseEntity.badRequest()
                .body(Map.of("error", validation.getErrors()));
        }
        
        // Get current preferences
        String currentPrefsCookie = getCookieValue("user_prefs");
        Map<String, Object> preferences = new HashMap<>();
        
        if (currentPrefsCookie != null) {
            try {
                ObjectMapper mapper = new ObjectMapper();
                preferences = mapper.readValue(currentPrefsCookie, Map.class);
            } catch (Exception e) {
                // Use empty preferences if parsing fails
            }
        }
        
        // Update preference
        preferences.put(request.getKey(), request.getValue());
        
        // Set cookie
        String cookieValue = new ObjectMapper().writeValueAsString(preferences);
        Cookie cookie = new Cookie("user_prefs", cookieValue);
        cookie.setMaxAge(30 * 24 * 60 * 60); // 30 days
        cookie.setPath("/");
        cookie.setSecure(true);
        cookie.setHttpOnly(false);
        
        response.addCookie(cookie);
        
        return ResponseEntity.ok(Map.of(
            "message", "Preference updated",
            "preferences", preferences
        ));
    }
    
    public ResponseEntity<?> handleTracking(String trackingId, HttpServletResponse response) {
        if (trackingId == null) {
            // Generate new tracking ID
            trackingId = trackingService.generateTrackingId();
            
            // Set tracking cookie
            Cookie cookie = new Cookie("tracking_id", trackingId);
            cookie.setMaxAge(365 * 24 * 60 * 60); // 1 year
            cookie.setPath("/");
            cookie.setSecure(true);
            cookie.setHttpOnly(true);
            
            response.addCookie(cookie);
            
            // Initialize tracking
            trackingService.initializeTracking(trackingId, getRequestInfo());
        }
        
        // Track visit
        trackingService.trackVisit(trackingId, getRequestInfo());
        
        return ResponseEntity.ok(Map.of("tracking_id", trackingId));
    }
    
    private String getCookieValue(String name) {
        // Implementation to get cookie value from request
        return null;
    }
    
    private Map<String, Object> getRequestInfo() {
        return Map.of(
            "page", getCurrentPage(),
            "referer", getReferer(),
            "userAgent", getUserAgent(),
            "ipAddress", getIpAddress(),
            "timestamp", LocalDateTime.now()
        );
    }
    
    private String getCurrentPage() {
        // Implementation to get current page
        return "/";
    }
    
    private String getReferer() {
        // Implementation to get referer
        return null;
    }
    
    private String getUserAgent() {
        // Implementation to get user agent
        return "Unknown";
    }
    
    private String getIpAddress() {
        // Implementation to get IP address
        return "127.0.0.1";
    }
}
```

### Preference Service

```java
@Service
public class PreferenceService {
    
    public PreferenceValidationResult validatePreference(PreferenceRequest request) {
        List<String> errors = new ArrayList<>();
        
        // Validate key
        if (request.getKey() == null || request.getKey().trim().isEmpty()) {
            errors.add("Preference key is required");
        }
        
        // Validate value
        if (request.getValue() == null) {
            errors.add("Preference value is required");
        }
        
        // Validate specific preferences
        switch (request.getKey()) {
            case "theme":
                if (!isValidTheme(request.getValue().toString())) {
                    errors.add("Invalid theme value");
                }
                break;
            case "language":
                if (!isValidLanguage(request.getValue().toString())) {
                    errors.add("Invalid language value");
                }
                break;
            case "timezone":
                if (!isValidTimezone(request.getValue().toString())) {
                    errors.add("Invalid timezone value");
                }
                break;
        }
        
        return new PreferenceValidationResult(errors.isEmpty(), errors);
    }
    
    public PreferenceValidationResult validatePreferences(Map<String, Object> preferences) {
        List<String> errors = new ArrayList<>();
        Map<String, Object> validatedPreferences = new HashMap<>();
        
        for (Map.Entry<String, Object> entry : preferences.entrySet()) {
            PreferenceRequest request = new PreferenceRequest();
            request.setKey(entry.getKey());
            request.setValue(entry.getValue());
            
            PreferenceValidationResult validation = validatePreference(request);
            if (validation.isValid()) {
                validatedPreferences.put(entry.getKey(), entry.getValue());
            } else {
                errors.addAll(validation.getErrors());
            }
        }
        
        return new PreferenceValidationResult(errors.isEmpty(), errors, validatedPreferences);
    }
    
    public Map<String, Object> getDefaultPreferences() {
        return Map.of(
            "theme", "default",
            "language", "en",
            "timezone", "UTC",
            "notifications", true,
            "analytics", true
        );
    }
    
    public void trackThemeChange(String theme) {
        // Track theme change for analytics
        // Implementation details...
    }
    
    public void trackLanguageChange(String language) {
        // Track language change for analytics
        // Implementation details...
    }
    
    private boolean isValidTheme(String theme) {
        return Arrays.asList("light", "dark", "auto").contains(theme);
    }
    
    private boolean isValidLanguage(String language) {
        return Arrays.asList("en", "es", "fr", "de", "ja", "zh").contains(language);
    }
    
    private boolean isValidTimezone(String timezone) {
        try {
            ZoneId.of(timezone);
            return true;
        } catch (DateTimeException e) {
            return false;
        }
    }
}
```

## 🚀 Advanced Cookie Features

### Cookie Analytics

```tusk
# Cookie analytics with Java integration
cookie_analytics: {
    # Track cookie usage
    track_cookie_usage: @lambda({
        # Extract cookie metrics
        cookie_metrics = {
            total_cookies: @len(@cookie.all())
            cookie_names: @keys(@cookie.all())
            user_prefs_cookie: @cookie.get("user_prefs") != null
            tracking_cookie: @cookie.get("tracking_id") != null
            session_cookie: @cookie.get("JSESSIONID") != null
            timestamp: @time.now()
        }
        
        # Send metrics to Java analytics service
        @java.invoke("CookieAnalyticsService", "track", cookie_metrics)
    }),
    
    # Analyze cookie patterns
    analyze_cookie_patterns: @lambda({
        # Get cookie analysis from Java service
        analysis = @java.invoke("CookieAnalysisService", "analyze", @cookie.all())
        
        # Log insights
        @if(analysis.insights, {
            @log.info("Cookie insights: ${analysis.insights}")
        })
        
        return: analysis
    })
}
```

### Cookie Security

```tusk
# Cookie security with Java integration
cookie_security: {
    # Validate cookie security
    validate_cookie_security: @lambda({
        # Check cookie security settings
        security_checks = {
            has_secure_cookies: @check_secure_cookies()
            has_http_only_cookies: @check_http_only_cookies()
            has_same_site_cookies: @check_same_site_cookies()
            has_valid_domains: @check_cookie_domains()
        }
        
        # Validate security via Java service
        security_validation = @java.invoke("CookieSecurityService", "validate", security_checks)
        
        @if(!security_validation.secure, {
            return: { secure: false, issues: security_validation.issues }
        })
        
        return: { secure: true }
    }),
    
    # Check secure cookies
    check_secure_cookies: @lambda({
        # Implementation to check if cookies are secure
        return: true
    }),
    
    # Check HTTP-only cookies
    check_http_only_cookies: @lambda({
        # Implementation to check if cookies are HTTP-only
        return: true
    }),
    
    # Check SameSite cookies
    check_same_site_cookies: @lambda({
        # Implementation to check if cookies have SameSite attribute
        return: true
    }),
    
    # Check cookie domains
    check_cookie_domains: @lambda({
        # Implementation to check if cookie domains are valid
        return: true
    })
}
```

## 🔒 Security Considerations

### Cookie Security Patterns

```tusk
# Cookie security configuration
cookie_security_patterns: {
    # Cookie configuration
    cookie_config: {
        secure_cookies: true
        http_only_cookies: true
        same_site_policy: "strict"
        max_cookie_age: "1y"
        domain_restriction: true
        path_restriction: "/"
    },
    
    # Cookie validation
    cookie_validation: {
        validate_cookie_names: true
        validate_cookie_values: true
        validate_cookie_size: true
        block_suspicious_cookies: true
    },
    
    # Cookie logging
    cookie_logging: {
        enabled: true
        log_level: "info"
        log_cookie_creation: true
        log_cookie_modification: true
        log_cookie_deletion: true
        mask_sensitive_data: true
    }
}
```

## 🧪 Testing Cookie Handlers

### Cookie Testing Configuration

```tusk
# Cookie testing configuration
cookie_testing: {
    # Test cases for cookie management
    cookie_test_cases: [
        {
            name: "create_secure_cookie"
            action: "create_secure_cookie"
            params: { name: "test_cookie", value: "test_value" }
            expected: { success: true, cookie_name: "test_cookie" }
        },
        {
            name: "read_cookie_safely"
            action: "read_cookie_safely"
            params: { name: "test_cookie", default_value: "default" }
            expected: { value: "test_value" }
        },
        {
            name: "delete_cookie"
            action: "delete_cookie"
            params: { name: "test_cookie" }
            expected: { success: true, message: "Cookie removed" }
        }
    ],
    
    # Test cases for preferences
    preference_test_cases: [
        {
            name: "set_theme_preference"
            action: "set_theme_preference"
            params: { theme: "dark" }
            expected: { success: true, theme: "dark" }
        },
        {
            name: "set_invalid_theme"
            action: "set_theme_preference"
            params: { theme: "invalid" }
            expected: { success: false, error: "Invalid theme" }
        },
        {
            name: "get_all_preferences"
            action: "get_all_preferences"
            expected: { has_theme: true, has_language: true, has_timezone: true }
        }
    ]
}
```

## 🚀 Best Practices

### Cookie Handling Best Practices

1. **Use Java Services**: Delegate cookie management to Java services for better maintainability
2. **Implement Proper Security**: Use secure cookie settings (secure, httpOnly, sameSite)
3. **Validate Cookie Data**: Always validate cookie values before use
4. **Handle Cookie Expiration**: Implement proper cookie expiration and cleanup
5. **Use Cookie Analytics**: Track cookie usage for optimization
6. **Implement Cookie Consent**: Respect user privacy preferences
7. **Handle Errors Gracefully**: Return appropriate error messages for cookie issues
8. **Clean Up Cookies**: Properly clean up cookies when no longer needed

### Common Patterns

```tusk
# Common cookie handling patterns
common_patterns: {
    # Preference patterns
    preference_patterns: {
        user_preferences: "User preference storage and retrieval"
        theme_preferences: "Theme and appearance preferences"
        language_preferences: "Language and localization preferences"
        notification_preferences: "Notification and alert preferences"
    },
    
    # Tracking patterns
    tracking_patterns: {
        user_tracking: "User behavior tracking and analytics"
        session_tracking: "Session-based tracking and monitoring"
        page_tracking: "Page view and navigation tracking"
        action_tracking: "User action and interaction tracking"
    },
    
    # Security patterns
    security_patterns: {
        secure_cookies: "Secure cookie configuration and validation"
        cookie_consent: "Cookie consent and privacy compliance"
        cookie_encryption: "Cookie value encryption and decryption"
        cookie_validation: "Cookie value validation and sanitization"
    }
}
```

---

**We don't bow to any king** - TuskLang Java Edition empowers you to handle cookie variables with enterprise-grade patterns, Spring Boot integration, and the flexibility to adapt to your preferred approach. Whether you're managing user preferences, implementing tracking, or handling session data, TuskLang provides the tools you need to handle cookies efficiently and securely. 
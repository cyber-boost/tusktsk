# #auth - Authentication Directive (Java)

The `#auth` directive provides enterprise-grade authentication and authorization capabilities for Java applications, enabling secure access control with Spring Security integration and comprehensive user management.

## Basic Syntax

```tusk
# Basic authentication
#auth {
    #api /protected {
        return @get_protected_data()
    }
}

# Role-based authorization
#auth roles: ["admin", "user"] {
    #api /admin-data {
        return @get_admin_data()
    }
}

# Custom authentication logic
#auth {
    check: @custom_auth_check(@request.headers.Authorization)
    redirect: "/login"
} {
    #web /secure-page {
        @render_secure_content()
    }
}
```

## Java Implementation

```java
import org.tusklang.java.TuskLang;
import org.tusklang.java.directives.AuthDirective;
import org.springframework.web.bind.annotation.*;
import org.springframework.stereotype.Controller;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.access.prepost.PreAuthorize;

@Controller
public class AuthenticatedController {
    
    private final TuskLang tuskLang;
    private final AuthDirective authDirective;
    private final DataService dataService;
    
    public AuthenticatedController(TuskLang tuskLang, DataService dataService) {
        this.tuskLang = tuskLang;
        this.authDirective = new AuthDirective();
        this.dataService = dataService;
    }
    
    // Basic authentication with Spring Security
    @GetMapping("/api/protected")
    @PreAuthorize("isAuthenticated()")
    public ResponseEntity<DataResponse> getProtectedData() {
        return ResponseEntity.ok(dataService.getProtectedData());
    }
    
    // Role-based authorization
    @GetMapping("/api/admin-data")
    @PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
    public ResponseEntity<AdminDataResponse> getAdminData() {
        return ResponseEntity.ok(dataService.getAdminData());
    }
    
    // Custom authentication logic
    @GetMapping("/secure-page")
    public ResponseEntity<String> getSecurePage(
            @RequestHeader("Authorization") String authHeader,
            HttpServletRequest request) {
        
        if (!authDirective.checkCustomAuth(authHeader)) {
            return ResponseEntity.status(401)
                .header("Location", "/login")
                .body("Unauthorized");
        }
        
        return ResponseEntity.ok("Secure content");
    }
}
```

## Authentication Configuration

```tusk
# Detailed authentication configuration
#auth {
    method: "jwt"           # Authentication method
    roles: ["user", "admin"] # Required roles
    permissions: ["read", "write"] # Required permissions
    redirect: "/login"      # Redirect URL for unauthenticated users
    error_code: 401         # HTTP status code for auth failures
} {
    #api /endpoint {
        @process_request()
    }
}

# Multiple authentication methods
#auth {
    methods: ["jwt", "session", "api_key"]
    fallback: "session"
    priority: ["jwt", "api_key", "session"]
} {
    #api /flexible-auth {
        @handle_request()
    }
}
```

## Java Authentication Configuration

```java
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.context.annotation.Bean;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import java.util.List;
import java.util.Map;

@Component
@ConfigurationProperties(prefix = "tusk.auth")
public class AuthConfig {
    
    private String defaultMethod = "jwt";
    private List<String> defaultRoles = List.of("USER");
    private List<String> defaultPermissions = List.of("READ");
    private String redirectUrl = "/login";
    private int errorCode = 401;
    
    private Map<String, AuthMethod> methods;
    private List<String> fallbackMethods;
    private List<String> priorityMethods;
    
    // Getters and setters
    public String getDefaultMethod() { return defaultMethod; }
    public void setDefaultMethod(String defaultMethod) { this.defaultMethod = defaultMethod; }
    
    public List<String> getDefaultRoles() { return defaultRoles; }
    public void setDefaultRoles(List<String> defaultRoles) { this.defaultRoles = defaultRoles; }
    
    public List<String> getDefaultPermissions() { return defaultPermissions; }
    public void setDefaultPermissions(List<String> defaultPermissions) { this.defaultPermissions = defaultPermissions; }
    
    public String getRedirectUrl() { return redirectUrl; }
    public void setRedirectUrl(String redirectUrl) { this.redirectUrl = redirectUrl; }
    
    public int getErrorCode() { return errorCode; }
    public void setErrorCode(int errorCode) { this.errorCode = errorCode; }
    
    public Map<String, AuthMethod> getMethods() { return methods; }
    public void setMethods(Map<String, AuthMethod> methods) { this.methods = methods; }
    
    public List<String> getFallbackMethods() { return fallbackMethods; }
    public void setFallbackMethods(List<String> fallbackMethods) { this.fallbackMethods = fallbackMethods; }
    
    public List<String> getPriorityMethods() { return priorityMethods; }
    public void setPriorityMethods(List<String> priorityMethods) { this.priorityMethods = priorityMethods; }
    
    public static class AuthMethod {
        private boolean enabled;
        private String secret;
        private int expiration;
        private String issuer;
        
        // Getters and setters
        public boolean isEnabled() { return enabled; }
        public void setEnabled(boolean enabled) { this.enabled = enabled; }
        
        public String getSecret() { return secret; }
        public void setSecret(String secret) { this.secret = secret; }
        
        public int getExpiration() { return expiration; }
        public void setExpiration(int expiration) { this.expiration = expiration; }
        
        public String getIssuer() { return issuer; }
        public void setIssuer(String issuer) { this.issuer = issuer; }
    }
}

@Configuration
@EnableWebSecurity
@EnableMethodSecurity
public class SecurityConfiguration {
    
    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http, AuthConfig authConfig) throws Exception {
        http
            .authorizeHttpRequests(authz -> authz
                .requestMatchers("/public/**").permitAll()
                .requestMatchers("/api/**").authenticated()
                .requestMatchers("/admin/**").hasRole("ADMIN")
                .anyRequest().authenticated()
            )
            .formLogin(form -> form
                .loginPage("/login")
                .defaultSuccessUrl("/dashboard")
                .failureUrl("/login?error=true")
            )
            .logout(logout -> logout
                .logoutUrl("/logout")
                .logoutSuccessUrl("/login")
                .invalidateHttpSession(true)
                .deleteCookies("JSESSIONID")
            )
            .exceptionHandling(ex -> ex
                .authenticationEntryPoint((request, response, authException) -> {
                    response.setStatus(authConfig.getErrorCode());
                    response.sendRedirect(authConfig.getRedirectUrl());
                })
            )
            .csrf(csrf -> csrf.disable());
        
        return http.build();
    }
    
    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }
    
    @Bean
    public JwtTokenProvider jwtTokenProvider(AuthConfig authConfig) {
        AuthConfig.AuthMethod jwtMethod = authConfig.getMethods().get("jwt");
        return new JwtTokenProvider(jwtMethod.getSecret(), jwtMethod.getExpiration());
    }
}
```

## JWT Authentication

```tusk
# JWT authentication
#auth method: "jwt" {
    #api /jwt-protected {
        return @get_jwt_data()
    }
}

# JWT with custom claims
#auth {
    method: "jwt"
    claims: {
        role: @auth.user.role
        permissions: @auth.user.permissions
        tenant: @auth.user.tenant
    }
} {
    #api /multi-tenant {
        return @get_tenant_data(@auth.user.tenant)
    }
}

# JWT refresh token
#auth {
    method: "jwt"
    refresh: true
    refresh_expiry: 604800  # 7 days
} {
    #api /refresh-token {
        return @refresh_jwt_token(@request.body.refresh_token)
    }
}
```

## Java JWT Implementation

```java
import io.jsonwebtoken.*;
import io.jsonwebtoken.security.Keys;
import org.springframework.stereotype.Service;
import java.security.Key;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;

@Service
public class JwtTokenProvider {
    
    private final Key secretKey;
    private final int expirationTime;
    private final int refreshExpirationTime;
    
    public JwtTokenProvider(String secret, int expirationTime) {
        this.secretKey = Keys.hmacShaKeyFor(secret.getBytes());
        this.expirationTime = expirationTime;
        this.refreshExpirationTime = expirationTime * 7; // 7 days
    }
    
    public String generateToken(User user) {
        Date now = new Date();
        Date expiryDate = new Date(now.getTime() + expirationTime * 1000L);
        
        Map<String, Object> claims = new HashMap<>();
        claims.put("userId", user.getId());
        claims.put("email", user.getEmail());
        claims.put("role", user.getRole());
        claims.put("permissions", user.getPermissions());
        claims.put("tenant", user.getTenant());
        
        return Jwts.builder()
            .setClaims(claims)
            .setSubject(user.getEmail())
            .setIssuedAt(now)
            .setExpiration(expiryDate)
            .signWith(secretKey, SignatureAlgorithm.HS512)
            .compact();
    }
    
    public String generateRefreshToken(User user) {
        Date now = new Date();
        Date expiryDate = new Date(now.getTime() + refreshExpirationTime * 1000L);
        
        return Jwts.builder()
            .setSubject(user.getEmail())
            .setIssuedAt(now)
            .setExpiration(expiryDate)
            .signWith(secretKey, SignatureAlgorithm.HS512)
            .compact();
    }
    
    public Claims validateToken(String token) {
        try {
            return Jwts.parserBuilder()
                .setSigningKey(secretKey)
                .build()
                .parseClaimsJws(token)
                .getBody();
        } catch (JwtException | IllegalArgumentException e) {
            throw new InvalidJwtTokenException("Invalid JWT token");
        }
    }
    
    public String refreshToken(String refreshToken) {
        try {
            Claims claims = Jwts.parserBuilder()
                .setSigningKey(secretKey)
                .build()
                .parseClaimsJws(refreshToken)
                .getBody();
            
            String email = claims.getSubject();
            User user = userService.findByEmail(email);
            
            return generateToken(user);
        } catch (JwtException e) {
            throw new InvalidJwtTokenException("Invalid refresh token");
        }
    }
}

@Component
public class JwtAuthenticationFilter extends OncePerRequestFilter {
    
    private final JwtTokenProvider tokenProvider;
    private final UserDetailsService userDetailsService;
    
    public JwtAuthenticationFilter(JwtTokenProvider tokenProvider,
                                 UserDetailsService userDetailsService) {
        this.tokenProvider = tokenProvider;
        this.userDetailsService = userDetailsService;
    }
    
    @Override
    protected void doFilterInternal(HttpServletRequest request,
                                  HttpServletResponse response,
                                  FilterChain filterChain) throws ServletException, IOException {
        
        String token = extractToken(request);
        
        if (token != null && tokenProvider.validateToken(token) != null) {
            Claims claims = tokenProvider.validateToken(token);
            String email = claims.getSubject();
            
            UserDetails userDetails = userDetailsService.loadUserByUsername(email);
            UsernamePasswordAuthenticationToken authentication = 
                new UsernamePasswordAuthenticationToken(userDetails, null, userDetails.getAuthorities());
            
            SecurityContextHolder.getContext().setAuthentication(authentication);
        }
        
        filterChain.doFilter(request, response);
    }
    
    private String extractToken(HttpServletRequest request) {
        String bearerToken = request.getHeader("Authorization");
        if (bearerToken != null && bearerToken.startsWith("Bearer ")) {
            return bearerToken.substring(7);
        }
        return null;
    }
}

@RestController
public class JwtAuthController {
    
    private final JwtTokenProvider tokenProvider;
    private final AuthenticationManager authenticationManager;
    private final UserService userService;
    
    public JwtAuthController(JwtTokenProvider tokenProvider,
                           AuthenticationManager authenticationManager,
                           UserService userService) {
        this.tokenProvider = tokenProvider;
        this.authenticationManager = authenticationManager;
        this.userService = userService;
    }
    
    @PostMapping("/api/auth/login")
    public ResponseEntity<AuthResponse> login(@RequestBody LoginRequest loginRequest) {
        try {
            Authentication authentication = authenticationManager.authenticate(
                new UsernamePasswordAuthenticationToken(
                    loginRequest.getEmail(), 
                    loginRequest.getPassword()
                )
            );
            
            SecurityContextHolder.getContext().setAuthentication(authentication);
            
            User user = userService.findByEmail(loginRequest.getEmail());
            String token = tokenProvider.generateToken(user);
            String refreshToken = tokenProvider.generateRefreshToken(user);
            
            return ResponseEntity.ok(new AuthResponse(token, refreshToken));
        } catch (AuthenticationException e) {
            return ResponseEntity.status(401).body(null);
        }
    }
    
    @PostMapping("/api/auth/refresh")
    public ResponseEntity<AuthResponse> refresh(@RequestBody RefreshRequest refreshRequest) {
        try {
            String newToken = tokenProvider.refreshToken(refreshRequest.getRefreshToken());
            return ResponseEntity.ok(new AuthResponse(newToken, refreshRequest.getRefreshToken()));
        } catch (InvalidJwtTokenException e) {
            return ResponseEntity.status(401).body(null);
        }
    }
}
```

## Role-Based Authorization

```tusk
# Simple role check
#auth roles: ["admin"] {
    #api /admin-only {
        return @admin_operation()
    }
}

# Multiple roles (OR logic)
#auth roles: ["admin", "moderator"] {
    #api /moderated-content {
        return @get_moderated_content()
    }
}

# Role hierarchy
#auth {
    roles: ["admin", "user"]
    hierarchy: {
        admin: ["user", "moderator"]
        moderator: ["user"]
        user: []
    }
} {
    #api /hierarchical-auth {
        return @process_with_hierarchy()
    }
}
```

## Java Role-Based Authorization

```java
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.access.hierarchicalroles.RoleHierarchy;
import org.springframework.stereotype.Service;

@Service
public class RoleBasedAuthService {
    
    private final RoleHierarchy roleHierarchy;
    
    public RoleBasedAuthService(RoleHierarchy roleHierarchy) {
        this.roleHierarchy = roleHierarchy;
    }
    
    public boolean hasRole(User user, String requiredRole) {
        Collection<? extends GrantedAuthority> authorities = user.getAuthorities();
        Collection<? extends GrantedAuthority> reachableAuthorities = 
            roleHierarchy.getReachableGrantedAuthorities(authorities);
        
        return reachableAuthorities.stream()
            .anyMatch(authority -> authority.getAuthority().equals("ROLE_" + requiredRole.toUpperCase()));
    }
    
    public boolean hasAnyRole(User user, List<String> requiredRoles) {
        return requiredRoles.stream().anyMatch(role -> hasRole(user, role));
    }
    
    public boolean hasAllRoles(User user, List<String> requiredRoles) {
        return requiredRoles.stream().allMatch(role -> hasRole(user, role));
    }
}

@RestController
public class RoleBasedController {
    
    private final RoleBasedAuthService authService;
    private final AdminService adminService;
    private final ModeratorService moderatorService;
    
    public RoleBasedController(RoleBasedAuthService authService,
                             AdminService adminService,
                             ModeratorService moderatorService) {
        this.authService = authService;
        this.adminService = adminService;
        this.moderatorService = moderatorService;
    }
    
    @GetMapping("/api/admin-only")
    @PreAuthorize("hasRole('ADMIN')")
    public ResponseEntity<AdminResponse> adminOnly(@AuthenticationPrincipal User user) {
        return ResponseEntity.ok(adminService.performAdminOperation());
    }
    
    @GetMapping("/api/moderated-content")
    @PreAuthorize("hasAnyRole('ADMIN', 'MODERATOR')")
    public ResponseEntity<ModeratedContentResponse> moderatedContent(@AuthenticationPrincipal User user) {
        return ResponseEntity.ok(moderatorService.getModeratedContent());
    }
    
    @PostMapping("/api/hierarchical-auth")
    @PreAuthorize("hasRole('ADMIN') or hasRole('USER')")
    public ResponseEntity<String> hierarchicalAuth(@AuthenticationPrincipal User user) {
        if (authService.hasRole(user, "ADMIN")) {
            return ResponseEntity.ok("Admin operation");
        } else if (authService.hasRole(user, "USER")) {
            return ResponseEntity.ok("User operation");
        }
        
        return ResponseEntity.status(403).body("Access denied");
    }
}

@Configuration
public class RoleHierarchyConfiguration {
    
    @Bean
    public RoleHierarchy roleHierarchy() {
        RoleHierarchyImpl roleHierarchy = new RoleHierarchyImpl();
        roleHierarchy.setHierarchy(
            "ROLE_ADMIN > ROLE_MODERATOR\n" +
            "ROLE_MODERATOR > ROLE_USER\n" +
            "ROLE_USER > ROLE_GUEST"
        );
        return roleHierarchy;
    }
}
```

## Permission-Based Authorization

```tusk
# Permission check
#auth permissions: ["read", "write"] {
    #api /permission-protected {
        return @permission_operation()
    }
}

# Resource-specific permissions
#auth {
    permissions: ["user:read", "user:write"]
    resource: @request.params.user_id
} {
    #api /user/{user_id} {
        return @get_user_data(@request.params.user_id)
    }
}

# Dynamic permissions
#auth {
    permissions: () => {
        return @get_user_permissions(@auth.user.id)
    }
} {
    #api /dynamic-permissions {
        return @process_with_dynamic_permissions()
    }
}
```

## Java Permission-Based Authorization

```java
import org.springframework.security.access.PermissionEvaluator;
import org.springframework.security.core.Authentication;
import org.springframework.stereotype.Service;

@Service
public class PermissionBasedAuthService {
    
    private final PermissionEvaluator permissionEvaluator;
    private final UserPermissionService permissionService;
    
    public PermissionBasedAuthService(PermissionEvaluator permissionEvaluator,
                                    UserPermissionService permissionService) {
        this.permissionEvaluator = permissionEvaluator;
        this.permissionService = permissionService;
    }
    
    public boolean hasPermission(User user, String permission) {
        return permissionService.hasPermission(user.getId(), permission);
    }
    
    public boolean hasPermission(User user, String permission, Object resource) {
        return permissionEvaluator.hasPermission(
            SecurityContextHolder.getContext().getAuthentication(),
            resource,
            permission
        );
    }
    
    public boolean hasAnyPermission(User user, List<String> permissions) {
        return permissions.stream().anyMatch(permission -> hasPermission(user, permission));
    }
    
    public boolean hasAllPermissions(User user, List<String> permissions) {
        return permissions.stream().allMatch(permission -> hasPermission(user, permission));
    }
    
    public List<String> getUserPermissions(Long userId) {
        return permissionService.getUserPermissions(userId);
    }
}

@RestController
public class PermissionBasedController {
    
    private final PermissionBasedAuthService authService;
    private final UserService userService;
    
    public PermissionBasedController(PermissionBasedAuthService authService,
                                   UserService userService) {
        this.authService = authService;
        this.userService = userService;
    }
    
    @GetMapping("/api/user/{userId}")
    @PreAuthorize("hasPermission(#userId, 'user:read')")
    public ResponseEntity<UserResponse> getUser(@PathVariable Long userId,
                                              @AuthenticationPrincipal User currentUser) {
        
        if (!authService.hasPermission(currentUser, "user:read", userId)) {
            return ResponseEntity.status(403).body(null);
        }
        
        return ResponseEntity.ok(userService.getUserById(userId));
    }
    
    @PutMapping("/api/user/{userId}")
    @PreAuthorize("hasPermission(#userId, 'user:write')")
    public ResponseEntity<UserResponse> updateUser(@PathVariable Long userId,
                                                 @RequestBody UserUpdateRequest request,
                                                 @AuthenticationPrincipal User currentUser) {
        
        if (!authService.hasPermission(currentUser, "user:write", userId)) {
            return ResponseEntity.status(403).body(null);
        }
        
        return ResponseEntity.ok(userService.updateUser(userId, request));
    }
    
    @GetMapping("/api/dynamic-permissions")
    public ResponseEntity<List<String>> getDynamicPermissions(@AuthenticationPrincipal User user) {
        List<String> permissions = authService.getUserPermissions(user.getId());
        return ResponseEntity.ok(permissions);
    }
}

@Component
public class CustomPermissionEvaluator implements PermissionEvaluator {
    
    private final UserPermissionService permissionService;
    
    public CustomPermissionEvaluator(UserPermissionService permissionService) {
        this.permissionService = permissionService;
    }
    
    @Override
    public boolean hasPermission(Authentication authentication, Object targetDomainObject, Object permission) {
        if (authentication == null || targetDomainObject == null || !(permission instanceof String)) {
            return false;
        }
        
        User user = (User) authentication.getPrincipal();
        String permissionString = (String) permission;
        
        if (targetDomainObject instanceof Long) {
            return permissionService.hasPermission(user.getId(), permissionString, (Long) targetDomainObject);
        }
        
        return permissionService.hasPermission(user.getId(), permissionString);
    }
    
    @Override
    public boolean hasPermission(Authentication authentication, Serializable targetId, String targetType, Object permission) {
        if (authentication == null || targetId == null || targetType == null || !(permission instanceof String)) {
            return false;
        }
        
        User user = (User) authentication.getPrincipal();
        String permissionString = (String) permission;
        
        return permissionService.hasPermission(user.getId(), permissionString, (Long) targetId);
    }
}
```

## Multi-Tenant Authentication

```tusk
# Tenant-based authentication
#auth {
    tenant: @request.headers.X-Tenant-ID
    check_tenant: true
} {
    #api /tenant-data {
        return @get_tenant_data(@auth.tenant)
    }
}

# Tenant isolation
#auth {
    tenant: @auth.user.tenant
    isolation: true
    cross_tenant_access: false
} {
    #api /isolated-data {
        return @get_isolated_data()
    }
}
```

## Java Multi-Tenant Implementation

```java
import org.springframework.stereotype.Service;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

@Service
public class MultiTenantAuthService {
    
    private final TenantService tenantService;
    
    public MultiTenantAuthService(TenantService tenantService) {
        this.tenantService = tenantService;
    }
    
    public String getCurrentTenant() {
        ServletRequestAttributes attributes = (ServletRequestAttributes) RequestContextHolder.getRequestAttributes();
        if (attributes != null) {
            HttpServletRequest request = attributes.getRequest();
            String tenantId = request.getHeader("X-Tenant-ID");
            
            if (tenantId != null) {
                return tenantId;
            }
        }
        
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        if (authentication != null && authentication.getPrincipal() instanceof User) {
            User user = (User) authentication.getPrincipal();
            return user.getTenant();
        }
        
        return null;
    }
    
    public boolean validateTenant(String tenantId) {
        return tenantService.isValidTenant(tenantId);
    }
    
    public boolean hasTenantAccess(User user, String resourceTenant) {
        // Check if user has access to the resource tenant
        if (user.getTenant().equals(resourceTenant)) {
            return true;
        }
        
        // Check for cross-tenant permissions
        return user.hasPermission("cross_tenant:access") && 
               user.hasPermission("tenant:" + resourceTenant + ":access");
    }
}

@RestController
public class MultiTenantController {
    
    private final MultiTenantAuthService tenantAuthService;
    private final TenantDataService tenantDataService;
    
    public MultiTenantController(MultiTenantAuthService tenantAuthService,
                               TenantDataService tenantDataService) {
        this.tenantAuthService = tenantAuthService;
        this.tenantDataService = tenantDataService;
    }
    
    @GetMapping("/api/tenant-data")
    public ResponseEntity<TenantDataResponse> getTenantData(@AuthenticationPrincipal User user) {
        String tenantId = tenantAuthService.getCurrentTenant();
        
        if (tenantId == null || !tenantAuthService.validateTenant(tenantId)) {
            return ResponseEntity.status(400).body(null);
        }
        
        if (!tenantAuthService.hasTenantAccess(user, tenantId)) {
            return ResponseEntity.status(403).body(null);
        }
        
        return ResponseEntity.ok(tenantDataService.getTenantData(tenantId));
    }
    
    @GetMapping("/api/isolated-data")
    public ResponseEntity<IsolatedDataResponse> getIsolatedData(@AuthenticationPrincipal User user) {
        String userTenant = user.getTenant();
        
        // Ensure data isolation
        return ResponseEntity.ok(tenantDataService.getIsolatedData(userTenant));
    }
}
```

## OAuth2 Integration

```tusk
# OAuth2 authentication
#auth {
    method: "oauth2"
    provider: "google"
    scopes: ["email", "profile"]
} {
    #api /oauth-protected {
        return @get_oauth_data()
    }
}

# Multiple OAuth providers
#auth {
    method: "oauth2"
    providers: ["google", "github", "facebook"]
    fallback: "session"
} {
    #api /multi-oauth {
        return @handle_multi_oauth()
    }
}
```

## Java OAuth2 Implementation

```java
import org.springframework.security.oauth2.client.authentication.OAuth2AuthenticationToken;
import org.springframework.security.oauth2.core.user.OAuth2User;
import org.springframework.stereotype.Service;

@Service
public class OAuth2AuthService {
    
    private final OAuth2UserService oauth2UserService;
    
    public OAuth2AuthService(OAuth2UserService oauth2UserService) {
        this.oauth2UserService = oauth2UserService;
    }
    
    public OAuth2User getOAuth2User(Authentication authentication) {
        if (authentication instanceof OAuth2AuthenticationToken) {
            OAuth2AuthenticationToken oauthToken = (OAuth2AuthenticationToken) authentication;
            return oauthToken.getPrincipal();
        }
        return null;
    }
    
    public String getOAuth2Provider(Authentication authentication) {
        if (authentication instanceof OAuth2AuthenticationToken) {
            OAuth2AuthenticationToken oauthToken = (OAuth2AuthenticationToken) authentication;
            return oauthToken.getAuthorizedClientRegistrationId();
        }
        return null;
    }
    
    public boolean hasOAuth2Scope(Authentication authentication, String scope) {
        OAuth2User oauth2User = getOAuth2User(authentication);
        if (oauth2User != null) {
            return oauth2User.getAuthorities().stream()
                .anyMatch(authority -> authority.getAuthority().equals("SCOPE_" + scope));
        }
        return false;
    }
}

@RestController
public class OAuth2Controller {
    
    private final OAuth2AuthService oauth2AuthService;
    private final OAuth2DataService oauth2DataService;
    
    public OAuth2Controller(OAuth2AuthService oauth2AuthService,
                          OAuth2DataService oauth2DataService) {
        this.oauth2AuthService = oauth2AuthService;
        this.oauth2DataService = oauth2DataService;
    }
    
    @GetMapping("/api/oauth-protected")
    public ResponseEntity<OAuth2DataResponse> getOAuthData(@AuthenticationPrincipal OAuth2User oauth2User) {
        String provider = oauth2AuthService.getOAuth2Provider(SecurityContextHolder.getContext().getAuthentication());
        
        if (provider == null) {
            return ResponseEntity.status(401).body(null);
        }
        
        return ResponseEntity.ok(oauth2DataService.getOAuthData(provider, oauth2User));
    }
    
    @GetMapping("/api/multi-oauth")
    public ResponseEntity<String> handleMultiOAuth(@AuthenticationPrincipal OAuth2User oauth2User) {
        Authentication authentication = SecurityContextHolder.getContext().getAuthentication();
        String provider = oauth2AuthService.getOAuth2Provider(authentication);
        
        if (provider == null) {
            // Fallback to session authentication
            return ResponseEntity.ok("Session authenticated");
        }
        
        return ResponseEntity.ok("OAuth2 authenticated via " + provider);
    }
}
```

## Authentication Testing

```java
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.security.test.context.support.WithUserDetails;
import org.springframework.test.context.TestPropertySource;
import org.springframework.beans.factory.annotation.Autowired;

@SpringBootTest
@TestPropertySource(properties = {
    "spring.security.user.name=test",
    "spring.security.user.password=test"
})
public class AuthTest {
    
    @Autowired
    private AuthenticatedController controller;
    
    @Test
    @WithMockUser(roles = "USER")
    public void testUserAccess() {
        ResponseEntity<DataResponse> response = controller.getProtectedData();
        assertEquals(200, response.getStatusCodeValue());
    }
    
    @Test
    @WithMockUser(roles = "ADMIN")
    public void testAdminAccess() {
        ResponseEntity<AdminDataResponse> response = controller.getAdminData();
        assertEquals(200, response.getStatusCodeValue());
    }
    
    @Test
    @WithMockUser(roles = "USER")
    public void testUserAccessDenied() {
        // This should be denied for USER role
        ResponseEntity<AdminDataResponse> response = controller.getAdminData();
        assertEquals(403, response.getStatusCodeValue());
    }
    
    @Test
    public void testUnauthenticatedAccess() {
        // This should redirect to login
        ResponseEntity<String> response = controller.getSecurePage("invalid-token", null);
        assertEquals(401, response.getStatusCodeValue());
    }
}
```

## Configuration Properties

```yaml
# application.yml
tusk:
  auth:
    default-method: "jwt"
    default-roles: ["USER"]
    default-permissions: ["READ"]
    redirect-url: "/login"
    error-code: 401
    
    methods:
      jwt:
        enabled: true
        secret: "your-secret-key-here"
        expiration: 3600
        issuer: "tusk-app"
      session:
        enabled: true
        timeout: 1800
      oauth2:
        enabled: true
        providers:
          google:
            client-id: "google-client-id"
            client-secret: "google-client-secret"
            scopes: ["email", "profile"]
          github:
            client-id: "github-client-id"
            client-secret: "github-client-secret"
            scopes: ["user:email"]
    
    fallback-methods: ["session"]
    priority-methods: ["jwt", "oauth2", "session"]

spring:
  security:
    oauth2:
      client:
        registration:
          google:
            client-id: ${tusk.auth.methods.oauth2.providers.google.client-id}
            client-secret: ${tusk.auth.methods.oauth2.providers.google.client-secret}
            scope:
              - email
              - profile
          github:
            client-id: ${tusk.auth.methods.oauth2.providers.github.client-id}
            client-secret: ${tusk.auth.methods.oauth2.providers.github.client-secret}
            scope:
              - user:email
```

## Summary

The `#auth` directive in TuskLang provides comprehensive authentication and authorization capabilities for Java applications. With Spring Security integration, JWT support, OAuth2 providers, and multi-tenant authentication, you can implement enterprise-grade security that scales with your application.

Key features include:
- **Multiple authentication methods**: JWT, session, OAuth2, and API key authentication
- **Spring Security integration**: Seamless integration with Spring Security framework
- **Role-based authorization**: Hierarchical role system with inheritance
- **Permission-based authorization**: Fine-grained permission control
- **Multi-tenant support**: Tenant isolation and cross-tenant access control
- **OAuth2 integration**: Support for multiple OAuth2 providers
- **JWT support**: Token-based authentication with refresh tokens
- **Testing support**: Comprehensive testing utilities with Spring Security Test

The Java implementation provides enterprise-grade authentication that integrates seamlessly with Spring Boot applications while maintaining the simplicity and power of TuskLang's declarative syntax. 
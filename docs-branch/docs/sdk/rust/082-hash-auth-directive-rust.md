# 🦀 #auth - Authentication and Authorization Directive - Rust Edition

**"We don't bow to any king" - Rust Edition**

The `#auth` directive in Rust creates robust authentication and authorization systems with zero-copy security, async support, and seamless integration with Rust's security ecosystem.

## Basic Syntax

```rust
use tusklang_rust::{parse, AuthDirective, SecurityContext};
use jsonwebtoken::{decode, encode, Header, Validation, DecodingKey};

// Simple authentication directive
let auth_config = r#"
#auth jwt {
    handler: "JwtAuthHandler::authenticate"
    strategy: "jwt"
    secret: @env.JWT_SECRET
    async: true
}
"#;

// Authorization directive with roles
let role_auth = r#"
#auth admin_only {
    handler: "RoleAuthHandler::authorize"
    roles: ["admin", "super_admin"]
    permissions: ["manage_users", "delete_data"]
    async: true
}
"#;

// Session-based authentication
let session_auth = r#"
#auth session {
    handler: "SessionAuthHandler::authenticate"
    strategy: "session"
    store: "redis"
    ttl: "24h"
    async: true
}
"#;
```

## JWT Authentication with Rust

```rust
use tusklang_rust::{AuthDirective, JwtAuth, JwtClaims};
use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

#[derive(Debug, Deserialize, Serialize)]
struct JwtClaims {
    sub: String,
    exp: i64,
    iat: i64,
    role: String,
    permissions: Vec<String>,
}

// JWT authentication with Rust types
let jwt_auth = r#"
#auth jwt_auth {
    handler: "JwtAuthHandler::authenticate"
    strategy: "jwt"
    
    config: {
        secret: @env.JWT_SECRET
        algorithm: "HS256"
        header: "Authorization"
        prefix: "Bearer "
        
        claims: {
            sub: "required"
            exp: "required"
            iat: "required"
            role: "optional"
            permissions: "optional"
        }
        
        validation: {
            exp_not_expired: true
            iat_not_future: true
            signature_valid: true
        }
    }
    
    on_success: {
        set_user: true
        set_claims: true
        set_permissions: true
        continue: true
    }
    
    on_failure: {
        status: 401
        message: "Invalid or expired token"
        redirect: "/login"
    }
}
"#;

// JWT token generation
let jwt_generate = r#"
#auth generate_jwt {
    handler: "JwtAuthHandler::generate_token"
    strategy: "jwt_generate"
    
    config: {
        secret: @env.JWT_SECRET
        algorithm: "HS256"
        expires_in: "24h"
        
        claims: {
            sub: "@user.id"
            role: "@user.role"
            permissions: "@user.permissions"
            custom: {
                email: "@user.email"
                name: "@user.name"
            }
        }
    }
    
    return: {
        token: "@generated_token"
        expires_at: "@expires_at"
        token_type: "Bearer"
    }
}
"#;
```

## Session-Based Authentication

```rust
use tusklang_rust::{AuthDirective, SessionAuth, SessionStore};
use std::collections::HashMap;

// Session authentication with Redis
let session_auth = r#"
#auth session_auth {
    handler: "SessionAuthHandler::authenticate"
    strategy: "session"
    
    config: {
        store: "redis"
        session_key: "session_id"
        ttl: "24h"
        
        cookie: {
            name: "session_id"
            secure: true
            http_only: true
            same_site: "strict"
            max_age: "86400"
        }
        
        exclude_paths: ["/login", "/register", "/public"]
        redirect_url: "/login"
    }
    
    validation: {
        session_exists: "required"
        session_not_expired: "required"
        user_exists: "required"
        user_active: "required"
    }
    
    on_success: {
        set_user: true
        set_session: true
        continue: true
    }
    
    on_failure: {
        clear_session: true
        redirect: "/login"
    }
}
"#;

// Session management
let session_manage = r#"
#auth session_manage {
    handler: "SessionAuthHandler::manage"
    strategy: "session_management"
    
    operations: {
        create: {
            user_id: "@user.id"
            data: {
                ip: "@request.ip"
                user_agent: "@request.headers.user_agent"
                created_at: "@now()"
            }
        }
        
        destroy: {
            session_id: "@session.id"
            reason: "@reason"
        }
        
        refresh: {
            session_id: "@session.id"
            extend_by: "1h"
        }
    }
}
"#;
```

## OAuth Authentication with Rust

```rust
use tusklang_rust::{AuthDirective, OAuthAuth, OAuthProvider};
use oauth2::{AuthorizationCode, TokenResponse};

// OAuth authentication with multiple providers
let oauth_auth = r#"
#auth oauth_auth {
    handler: "OAuthAuthHandler::authenticate"
    strategy: "oauth"
    
    config: {
        providers: {
            google: {
                client_id: @env.GOOGLE_CLIENT_ID
                client_secret: @env.GOOGLE_CLIENT_SECRET
                scopes: ["email", "profile"]
                auth_url: "https://accounts.google.com/oauth/authorize"
                token_url: "https://oauth2.googleapis.com/token"
                userinfo_url: "https://www.googleapis.com/oauth2/v2/userinfo"
            }
            
            github: {
                client_id: @env.GITHUB_CLIENT_ID
                client_secret: @env.GITHUB_CLIENT_SECRET
                scopes: ["user:email"]
                auth_url: "https://github.com/login/oauth/authorize"
                token_url: "https://github.com/login/oauth/access_token"
                userinfo_url: "https://api.github.com/user"
            }
            
            facebook: {
                client_id: @env.FACEBOOK_CLIENT_ID
                client_secret: @env.FACEBOOK_CLIENT_SECRET
                scopes: ["email", "public_profile"]
                auth_url: "https://www.facebook.com/dialog/oauth"
                token_url: "https://graph.facebook.com/oauth/access_token"
                userinfo_url: "https://graph.facebook.com/me"
            }
        }
        
        callback_url: "/auth/callback"
        success_redirect: "/dashboard"
        failure_redirect: "/login"
        
        user_mapping: {
            id: "@provider_user.id"
            email: "@provider_user.email"
            name: "@provider_user.name"
            avatar: "@provider_user.picture"
        }
    }
}
"#;
```

## Role-Based Access Control (RBAC)

```rust
use tusklang_rust::{AuthDirective, RbacAuth, RoleManager};
use std::collections::HashMap;

// RBAC authorization
let rbac_auth = r#"
#auth rbac_auth {
    handler: "RbacAuthHandler::authorize"
    strategy: "rbac"
    
    config: {
        roles: {
            admin: {
                permissions: ["manage_users", "delete_data", "view_logs", "manage_system"]
                inherits: ["moderator"]
            }
            
            moderator: {
                permissions: ["moderate_content", "ban_users", "view_reports"]
                inherits: ["user"]
            }
            
            user: {
                permissions: ["read_content", "create_posts", "edit_own_posts"]
                inherits: ["guest"]
            }
            
            guest: {
                permissions: ["read_public_content"]
            }
        }
        
        permissions: {
            manage_users: {
                description: "Manage user accounts"
                resources: ["users", "roles"]
                actions: ["create", "read", "update", "delete"]
            }
            
            delete_data: {
                description: "Delete any data"
                resources: ["*"]
                actions: ["delete"]
            }
            
            moderate_content: {
                description: "Moderate user content"
                resources: ["posts", "comments"]
                actions: ["read", "update", "delete"]
            }
        }
    }
    
    validation: {
        user_has_role: "required"
        user_has_permission: "required"
        resource_access_allowed: "required"
    }
    
    on_success: {
        continue: true
        log_access: true
    }
    
    on_failure: {
        status: 403
        message: "Access denied"
        log_attempt: true
    }
}
"#;
```

## Permission-Based Access Control (PBAC)

```rust
use tusklang_rust::{AuthDirective, PbacAuth, PermissionManager};

// PBAC authorization
let pbac_auth = r#"
#auth pbac_auth {
    handler: "PbacAuthHandler::authorize"
    strategy: "pbac"
    
    config: {
        permissions: {
            read_users: {
                description: "Read user information"
                resources: ["users"]
                actions: ["read"]
                conditions: {
                    own_data: "user.id == resource.owner_id"
                    admin_access: "user.role == 'admin'"
                }
            }
            
            write_users: {
                description: "Write user information"
                resources: ["users"]
                actions: ["create", "update"]
                conditions: {
                    own_data: "user.id == resource.owner_id"
                    admin_access: "user.role == 'admin'"
                    not_sensitive: "!resource.is_sensitive"
                }
            }
            
            delete_users: {
                description: "Delete user accounts"
                resources: ["users"]
                actions: ["delete"]
                conditions: {
                    admin_only: "user.role == 'admin'"
                    not_self: "user.id != resource.id"
                }
            }
        }
        
        policies: {
            user_management: {
                description: "User management policies"
                rules: [
                    "admin can manage all users"
                    "users can manage own profile"
                    "moderators can view user reports"
                ]
            }
        }
    }
    
    evaluation: {
        strategy: "deny_by_default"
        allow_if: "any_permission_granted"
        log_decisions: true
    }
}
"#;
```

## Multi-Factor Authentication (MFA)

```rust
use tusklang_rust::{AuthDirective, MfaAuth, MfaProvider};
use totp_rs::{Algorithm, TOTP};

// MFA authentication
let mfa_auth = r#"
#auth mfa_auth {
    handler: "MfaAuthHandler::authenticate"
    strategy: "mfa"
    
    config: {
        providers: {
            totp: {
                algorithm: "SHA1"
                digits: 6
                period: 30
                window: 1
            }
            
            sms: {
                provider: "twilio"
                template: "Your verification code is: {code}"
                expires_in: "5m"
            }
            
            email: {
                provider: "smtp"
                template: "email_verification.html"
                expires_in: "10m"
            }
        }
        
        setup: {
            required: true
            backup_codes: 10
            remember_device: "30d"
        }
        
        verification: {
            max_attempts: 3
            lockout_duration: "15m"
            require_backup: false
        }
    }
    
    flow: {
        step1: "primary_auth"
        step2: "mfa_verification"
        step3: "success"
    }
    
    on_success: {
        set_mfa_verified: true
        set_device_trusted: "@remember_device"
        continue: true
    }
    
    on_failure: {
        increment_attempts: true
        lockout_if_exceeded: true
        require_backup: "@attempts >= 2"
    }
}
"#;
```

## API Key Authentication

```rust
use tusklang_rust::{AuthDirective, ApiKeyAuth, KeyManager};

// API key authentication
let api_key_auth = r#"
#auth api_key_auth {
    handler: "ApiKeyAuthHandler::authenticate"
    strategy: "api_key"
    
    config: {
        header: "X-API-Key"
        query_param: "api_key"
        
        validation: {
            key_exists: "required"
            key_valid: "required"
            key_not_expired: "required"
            key_has_permissions: "required"
        }
        
        scopes: {
            read: ["GET"]
            write: ["POST", "PUT", "PATCH"]
            delete: ["DELETE"]
            admin: ["*"]
        }
        
        rate_limits: {
            default: "1000/hour"
            premium: "10000/hour"
            enterprise: "100000/hour"
        }
    }
    
    on_success: {
        set_api_key: true
        set_permissions: true
        set_rate_limit: true
        continue: true
    }
    
    on_failure: {
        status: 401
        message: "Invalid API key"
        log_attempt: true
    }
}
"#;
```

## Integration with Rust Web Frameworks

```rust
use actix_web::{web, App, HttpServer};
use tusklang_rust::{AuthDirective, ActixIntegration};

// Actix-web integration
async fn create_actix_app() -> App<()> {
    let auth_directives = parse(r#"
#auth jwt_auth {
    handler: "JwtAuthHandler::authenticate"
    strategy: "jwt"
    secret: @env.JWT_SECRET
    async: true
}

#auth admin_auth {
    handler: "RoleAuthHandler::authorize"
    roles: ["admin"]
    async: true
}
"#)?;
    
    App::new()
        .wrap(AuthDirective::create_actix_middleware(auth_directives))
}

// Axum integration
use axum::{Router, middleware as axum_middleware};
use tusklang_rust::AxumIntegration;

async fn create_axum_app() -> Router {
    let auth_directives = parse(r#"
#auth jwt_auth {
    handler: "JwtAuthHandler::authenticate"
    strategy: "jwt"
    secret: @env.JWT_SECRET
    async: true
}
"#)?;
    
    Router::new()
        .layer(AuthDirective::create_axum_layer(auth_directives))
}
```

## Testing Auth Directives with Rust

```rust
use tusklang_rust::{AuthDirectiveTester, TestAuth, TestUser};
use tokio::test;

#[tokio::test]
async fn test_jwt_auth_directive() {
    let auth_directive = r#"
#auth jwt_auth {
    handler: "JwtAuthHandler::authenticate"
    strategy: "jwt"
    secret: "test_secret"
    async: true
}
"#;
    
    let tester = AuthDirectiveTester::new();
    let token = generate_test_jwt("test_user", "user");
    
    let result = tester
        .test_auth_directive(auth_directive, &token)
        .execute()
        .await?;
    
    assert_eq!(result.status, "authenticated");
    assert_eq!(result.user_id, "test_user");
}

#[tokio::test]
async fn test_role_auth_directive() {
    let auth_directive = r#"
#auth admin_auth {
    handler: "RoleAuthHandler::authorize"
    roles: ["admin"]
    async: true
}
"#;
    
    let tester = AuthDirectiveTester::new();
    let admin_user = TestUser::new("admin_user", "admin");
    
    let result = tester
        .test_auth_directive(auth_directive, &admin_user)
        .execute()
        .await?;
    
    assert_eq!(result.status, "authorized");
    assert!(result.has_role("admin"));
}
```

## Security Best Practices with Rust

```rust
use tusklang_rust::{AuthDirective, SecurityValidator};
use std::collections::HashSet;

// Security validation for auth directives
struct AuthSecurityValidator {
    allowed_strategies: HashSet<String>,
    allowed_handlers: HashSet<String>,
    min_secret_length: usize,
    required_headers: HashSet<String>,
}

impl AuthSecurityValidator {
    fn validate_auth_directive(&self, directive: &AuthDirective) -> AuthDirectiveResult<()> {
        // Validate strategy
        if !self.allowed_strategies.contains(&directive.strategy) {
            return Err(AuthError::SecurityError(
                format!("Strategy not allowed: {}", directive.strategy)
            ));
        }
        
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(AuthError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate secret length
        if let Some(secret) = &directive.secret {
            if secret.len() < self.min_secret_length {
                return Err(AuthError::SecurityError(
                    format!("Secret too short: {}", secret.len())
                ));
            }
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust Auth Directives

```rust
// 1. Use strong typing for auth configurations
#[derive(Debug, Deserialize, Serialize)]
struct AuthDirectiveConfig {
    strategy: String,
    handler: String,
    secret: Option<String>,
    roles: Vec<String>,
    permissions: Vec<String>,
    async: bool,
}

// 2. Implement proper error handling
fn process_auth_directive_safe(directive: &str) -> Result<AuthDirective, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = AuthSecurityValidator::new();
    validator.validate_auth_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_auth_directive_async(directive: &AuthDirective) -> AuthDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging and monitoring
use tracing::{info, warn, error};

fn log_auth_execution(directive: &AuthDirective, result: &AuthDirectiveResult<()>) {
    match result {
        Ok(_) => info!("Auth directive executed successfully: {}", directive.strategy),
        Err(e) => error!("Auth directive execution failed: {} - {}", directive.strategy, e),
    }
}
```

## Next Steps

Now that you understand the `#auth` directive in Rust, explore other directive types:

- **[#cache Directive](./083-hash-cache-directive-rust.md)** - Caching strategies
- **[#rate-limit Directive](./084-hash-rate-limit-directive-rust.md)** - Rate limiting and throttling
- **[#custom Directives](./085-hash-custom-directives-rust.md)** - Building your own directives

**Ready to build secure authentication systems with Rust and TuskLang? Let's continue with the next directive!** 
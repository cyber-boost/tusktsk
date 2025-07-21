# Auth Directives in TuskLang - Bash Guide

## 🔐 **Revolutionary Authentication Configuration**

Auth directives in TuskLang transform your configuration files into intelligent authentication systems. No more separate auth frameworks or complex security configurations - everything lives in your TuskLang configuration with dynamic authentication methods, automatic session management, and intelligent security policies.

> **"We don't bow to any king"** - TuskLang auth directives break free from traditional authentication constraints and bring modern security capabilities to your Bash applications.

## 🚀 **Core Auth Directives**

### **Basic Auth Setup**
```bash
#auth: jwt                    # Authentication method
#auth-method: jwt             # Alternative syntax
#auth-secret: mysecretkey     # Secret key for JWT
#auth-algorithm: HS256        # JWT algorithm
#auth-expires: 3600           # Token expiration (seconds)
#auth-issuer: myapp           # Token issuer
#auth-audience: myclients     # Token audience
```

### **Advanced Auth Configuration**
```bash
#auth-provider: custom        # Auth provider (jwt, oauth, ldap, custom)
#auth-session: redis          # Session storage backend
#auth-session-ttl: 3600       # Session TTL (seconds)
#auth-password-hash: bcrypt   # Password hashing algorithm
#auth-rate-limit: 5/min       # Login rate limiting
#auth-mfa: totp               # Multi-factor authentication
#auth-roles: ["admin", "user"] # User roles
#auth-permissions: ["read", "write"] # User permissions
```

## 🔧 **Bash Auth Implementation**

### **Basic Auth Manager**
```bash
#!/bin/bash

# Load auth configuration
source <(tsk load auth.tsk)

# Auth configuration
AUTH_METHOD="${auth_method:-jwt}"
AUTH_SECRET="${auth_secret:-}"
AUTH_ALGORITHM="${auth_algorithm:-HS256}"
AUTH_EXPIRES="${auth_expires:-3600}"
AUTH_ISSUER="${auth_issuer:-tusk-app}"
AUTH_AUDIENCE="${auth_audience:-tusk-clients}"

# Auth manager
class AuthManager {
    constructor() {
        this.method = AUTH_METHOD
        this.secret = AUTH_SECRET
        this.algorithm = AUTH_ALGORITHM
        this.expires = AUTH_EXPIRES
        this.issuer = AUTH_ISSUER
        this.audience = AUTH_AUDIENCE
        this.sessions = new Map()
    }
    
    authenticate(credentials) {
        switch (this.method) {
            case 'jwt':
                return this.authenticateJWT(credentials)
            case 'oauth':
                return this.authenticateOAuth(credentials)
            case 'ldap':
                return this.authenticateLDAP(credentials)
            case 'api-key':
                return this.authenticateAPIKey(credentials)
            case 'basic':
                return this.authenticateBasic(credentials)
            default:
                throw new Error(`Unsupported auth method: ${this.method}`)
        }
    }
    
    generateToken(user) {
        const payload = {
            sub: user.id,
            name: user.name,
            email: user.email,
            roles: user.roles || [],
            permissions: user.permissions || [],
            iat: Math.floor(Date.now() / 1000),
            exp: Math.floor(Date.now() / 1000) + this.expires,
            iss: this.issuer,
            aud: this.audience
        }
        
        return this.signJWT(payload)
    }
    
    verifyToken(token) {
        try {
            const decoded = this.verifyJWT(token)
            return decoded
        } catch (error) {
            throw new Error(`Token verification failed: ${error.message}`)
        }
    }
    
    createSession(user, token) {
        const sessionId = this.generateSessionId()
        const session = {
            id: sessionId,
            userId: user.id,
            token: token,
            createdAt: new Date().toISOString(),
            expiresAt: new Date(Date.now() + this.expires * 1000).toISOString(),
            userAgent: this.getUserAgent(),
            ipAddress: this.getClientIP()
        }
        
        this.sessions.set(sessionId, session)
        return sessionId
    }
    
    getSession(sessionId) {
        const session = this.sessions.get(sessionId)
        if (!session) {
            return null
        }
        
        if (new Date() > new Date(session.expiresAt)) {
            this.sessions.delete(sessionId)
            return null
        }
        
        return session
    }
    
    revokeSession(sessionId) {
        this.sessions.delete(sessionId)
    }
    
    hasPermission(user, permission) {
        return user.permissions && user.permissions.includes(permission)
    }
    
    hasRole(user, role) {
        return user.roles && user.roles.includes(role)
    }
}

# Initialize auth manager
const authManager = new AuthManager()
```

### **JWT Authentication**
```bash
#!/bin/bash

# JWT authentication implementation
authenticate_jwt() {
    local token="$1"
    
    echo "Authenticating JWT token..."
    
    # Validate token format
    if ! validate_jwt_format "$token"; then
        echo "Invalid JWT format"
        return 1
    fi
    
    # Decode and verify token
    local payload=$(decode_jwt_payload "$token")
    if [[ $? -ne 0 ]]; then
        echo "Failed to decode JWT payload"
        return 1
    fi
    
    # Verify signature
    if ! verify_jwt_signature "$token" "$AUTH_SECRET"; then
        echo "JWT signature verification failed"
        return 1
    fi
    
    # Check expiration
    if ! check_jwt_expiration "$payload"; then
        echo "JWT token expired"
        return 1
    fi
    
    # Check issuer
    if ! check_jwt_issuer "$payload"; then
        echo "Invalid JWT issuer"
        return 1
    fi
    
    # Check audience
    if ! check_jwt_audience "$payload"; then
        echo "Invalid JWT audience"
        return 1
    fi
    
    echo "JWT authentication successful"
    return 0
}

validate_jwt_format() {
    local token="$1"
    
    # Check for three parts separated by dots
    if [[ ! "$token" =~ ^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$ ]]; then
        return 1
    fi
    
    return 0
}

decode_jwt_payload() {
    local token="$1"
    
    # Extract payload part
    local payload=$(echo "$token" | cut -d'.' -f2)
    
    # Add padding if needed
    local padding=$((4 - ${#payload} % 4))
    if [[ $padding -ne 4 ]]; then
        payload="${payload}${padding:0:$padding}"
    fi
    
    # Decode base64url
    echo "$payload" | tr '_-' '/+' | base64 -d 2>/dev/null
}

verify_jwt_signature() {
    local token="$1"
    local secret="$2"
    
    # Extract header and payload
    local header_payload=$(echo "$token" | cut -d'.' -f1,2)
    
    # Extract signature
    local signature=$(echo "$token" | cut -d'.' -f3)
    
    # Calculate expected signature
    local expected_signature=$(echo -n "$header_payload" | openssl dgst -sha256 -hmac "$secret" | cut -d' ' -f2)
    
    # Compare signatures
    if [[ "$signature" == "$expected_signature" ]]; then
        return 0
    fi
    
    return 1
}

check_jwt_expiration() {
    local payload="$1"
    
    # Extract expiration time
    local exp=$(echo "$payload" | jq -r '.exp // empty')
    
    if [[ -z "$exp" ]]; then
        return 1
    fi
    
    # Check if expired
    local current_time=$(date +%s)
    if [[ "$current_time" -gt "$exp" ]]; then
        return 1
    fi
    
    return 0
}

check_jwt_issuer() {
    local payload="$1"
    
    # Extract issuer
    local iss=$(echo "$payload" | jq -r '.iss // empty')
    
    if [[ -z "$iss" ]]; then
        return 1
    fi
    
    # Check if issuer matches
    if [[ "$iss" != "$AUTH_ISSUER" ]]; then
        return 1
    fi
    
    return 0
}

check_jwt_audience() {
    local payload="$1"
    
    # Extract audience
    local aud=$(echo "$payload" | jq -r '.aud // empty')
    
    if [[ -z "$aud" ]]; then
        return 1
    fi
    
    # Check if audience matches
    if [[ "$aud" != "$AUTH_AUDIENCE" ]]; then
        return 1
    fi
    
    return 0
}

generate_jwt_token() {
    local user_id="$1"
    local user_name="$2"
    local user_email="$3"
    local user_roles="$4"
    
    # Create header
    local header=$(echo '{"alg":"HS256","typ":"JWT"}' | base64 | tr -d '=' | tr '/+' '_-')
    
    # Create payload
    local current_time=$(date +%s)
    local expiration_time=$((current_time + AUTH_EXPIRES))
    
    local payload_data=$(cat << EOF
{
    "sub": "$user_id",
    "name": "$user_name",
    "email": "$user_email",
    "roles": [$user_roles],
    "iat": $current_time,
    "exp": $expiration_time,
    "iss": "$AUTH_ISSUER",
    "aud": "$AUTH_AUDIENCE"
}
EOF
)
    
    local payload=$(echo "$payload_data" | base64 | tr -d '=' | tr '/+' '_-')
    
    # Create signature
    local signature=$(echo -n "$header.$payload" | openssl dgst -sha256 -hmac "$AUTH_SECRET" | cut -d' ' -f2)
    
    # Combine parts
    echo "$header.$payload.$signature"
}
```

### **OAuth Authentication**
```bash
#!/bin/bash

# OAuth authentication implementation
authenticate_oauth() {
    local code="$1"
    local redirect_uri="$2"
    
    echo "Authenticating OAuth code..."
    
    # Exchange code for token
    local token_response=$(exchange_oauth_code "$code" "$redirect_uri")
    if [[ $? -ne 0 ]]; then
        echo "Failed to exchange OAuth code"
        return 1
    fi
    
    # Extract access token
    local access_token=$(echo "$token_response" | jq -r '.access_token // empty')
    if [[ -z "$access_token" ]]; then
        echo "No access token in response"
        return 1
    fi
    
    # Get user information
    local user_info=$(get_oauth_user_info "$access_token")
    if [[ $? -ne 0 ]]; then
        echo "Failed to get user information"
        return 1
    fi
    
    echo "OAuth authentication successful"
    echo "$user_info"
    return 0
}

exchange_oauth_code() {
    local code="$1"
    local redirect_uri="$2"
    
    # OAuth configuration
    local client_id="${oauth_client_id}"
    local client_secret="${oauth_client_secret}"
    local token_url="${oauth_token_url}"
    
    # Exchange code for token
    local response=$(curl -s -X POST "$token_url" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "grant_type=authorization_code" \
        -d "code=$code" \
        -d "redirect_uri=$redirect_uri" \
        -d "client_id=$client_id" \
        -d "client_secret=$client_secret")
    
    if [[ $? -eq 0 ]]; then
        echo "$response"
        return 0
    else
        return 1
    fi
}

get_oauth_user_info() {
    local access_token="$1"
    
    # OAuth configuration
    local user_info_url="${oauth_user_info_url}"
    
    # Get user information
    local response=$(curl -s -X GET "$user_info_url" \
        -H "Authorization: Bearer $access_token")
    
    if [[ $? -eq 0 ]]; then
        echo "$response"
        return 0
    else
        return 1
    fi
}
```

### **LDAP Authentication**
```bash
#!/bin/bash

# LDAP authentication implementation
authenticate_ldap() {
    local username="$1"
    local password="$2"
    
    echo "Authenticating with LDAP..."
    
    # LDAP configuration
    local ldap_server="${ldap_server:-ldap://localhost:389}"
    local ldap_base_dn="${ldap_base_dn}"
    local ldap_bind_dn="${ldap_bind_dn}"
    local ldap_bind_password="${ldap_bind_password}"
    
    # Search for user
    local user_dn=$(search_ldap_user "$username" "$ldap_base_dn" "$ldap_bind_dn" "$ldap_bind_password")
    if [[ $? -ne 0 ]]; then
        echo "User not found in LDAP"
        return 1
    fi
    
    # Authenticate user
    if authenticate_ldap_user "$user_dn" "$password"; then
        echo "LDAP authentication successful"
        
        # Get user attributes
        local user_attributes=$(get_ldap_user_attributes "$user_dn" "$ldap_bind_dn" "$ldap_bind_password")
        echo "$user_attributes"
        return 0
    else
        echo "LDAP authentication failed"
        return 1
    fi
}

search_ldap_user() {
    local username="$1"
    local base_dn="$2"
    local bind_dn="$3"
    local bind_password="$4"
    
    # Search for user by username
    local search_filter="(uid=$username)"
    
    # Use ldapsearch if available
    if command -v ldapsearch >/dev/null 2>&1; then
        local result=$(ldapsearch -H "$ldap_server" -D "$bind_dn" -w "$bind_password" \
            -b "$base_dn" "$search_filter" dn 2>/dev/null | grep "^dn:" | cut -d' ' -f2-)
        
        if [[ -n "$result" ]]; then
            echo "$result"
            return 0
        fi
    fi
    
    return 1
}

authenticate_ldap_user() {
    local user_dn="$1"
    local password="$2"
    
    # Try to bind with user credentials
    if command -v ldapsearch >/dev/null 2>&1; then
        ldapsearch -H "$ldap_server" -D "$user_dn" -w "$password" \
            -b "$user_dn" "(objectClass=*)" 1.1 >/dev/null 2>&1
        
        return $?
    fi
    
    return 1
}

get_ldap_user_attributes() {
    local user_dn="$1"
    local bind_dn="$2"
    local bind_password="$3"
    
    # Get user attributes
    if command -v ldapsearch >/dev/null 2>&1; then
        ldapsearch -H "$ldap_server" -D "$bind_dn" -w "$bind_password" \
            -b "$user_dn" "(objectClass=*)" cn mail uid 2>/dev/null
    fi
}
```

### **Session Management**
```bash
#!/bin/bash

# Session management implementation
class SessionManager {
    constructor() {
        this.backend = "${auth_session:-file}"
        this.ttl = "${auth_session_ttl:-3600}"
        this.sessions = new Map()
    }
    
    createSession(userId, token, options = {}) {
        const sessionId = this.generateSessionId()
        const session = {
            id: sessionId,
            userId: userId,
            token: token,
            createdAt: new Date().toISOString(),
            expiresAt: new Date(Date.now() + this.ttl * 1000).toISOString(),
            userAgent: options.userAgent || '',
            ipAddress: options.ipAddress || '',
            data: options.data || {}
        }
        
        this.saveSession(session)
        return sessionId
    }
    
    getSession(sessionId) {
        const session = this.loadSession(sessionId)
        if (!session) {
            return null
        }
        
        if (new Date() > new Date(session.expiresAt)) {
            this.deleteSession(sessionId)
            return null
        }
        
        return session
    }
    
    updateSession(sessionId, updates) {
        const session = this.getSession(sessionId)
        if (!session) {
            return false
        }
        
        Object.assign(session, updates)
        this.saveSession(session)
        return true
    }
    
    deleteSession(sessionId) {
        this.removeSession(sessionId)
    }
    
    cleanupExpiredSessions() {
        const now = new Date()
        for (const [sessionId, session] of this.sessions.entries()) {
            if (new Date(session.expiresAt) < now) {
                this.deleteSession(sessionId)
            }
        }
    }
    
    generateSessionId() {
        return crypto.randomBytes(32).toString('hex')
    }
    
    saveSession(session) {
        switch (this.backend) {
            case 'redis':
                this.saveToRedis(session)
                break
            case 'file':
                this.saveToFile(session)
                break
            case 'memory':
                this.sessions.set(session.id, session)
                break
        }
    }
    
    loadSession(sessionId) {
        switch (this.backend) {
            case 'redis':
                return this.loadFromRedis(sessionId)
            case 'file':
                return this.loadFromFile(sessionId)
            case 'memory':
                return this.sessions.get(sessionId)
        }
    }
    
    removeSession(sessionId) {
        switch (this.backend) {
            case 'redis':
                this.removeFromRedis(sessionId)
                break
            case 'file':
                this.removeFromFile(sessionId)
                break
            case 'memory':
                this.sessions.delete(sessionId)
                break
        }
    }
}

# Initialize session manager
const sessionManager = new SessionManager()
```

## 🎯 **Real-World Configuration Examples**

### **Complete Auth Configuration**
```bash
# auth-config.tsk
auth_config:
  method: jwt
  secret: "${JWT_SECRET}"
  algorithm: HS256
  expires: 3600
  issuer: "myapp"
  audience: "myclients"

#auth: jwt
#auth-method: jwt
#auth-secret: "${JWT_SECRET}"
#auth-algorithm: HS256
#auth-expires: 3600
#auth-issuer: myapp
#auth-audience: myclients

#auth-session: redis
#auth-session-ttl: 3600
#auth-password-hash: bcrypt
#auth-rate-limit: 5/min
#auth-mfa: totp

#auth-roles: ["admin", "user", "guest"]
#auth-permissions: ["read", "write", "delete", "admin"]

#auth-config:
#  jwt:
#    secret: "${JWT_SECRET}"
#    algorithm: HS256
#    expires: 3600
#    issuer: "myapp"
#    audience: "myclients"
#  session:
#    backend: "redis"
#    ttl: 3600
#    cookie_name: "session_id"
#    cookie_secure: true
#    cookie_http_only: true
#  password:
#    hash_algorithm: "bcrypt"
#    salt_rounds: 12
#    min_length: 8
#    require_special_chars: true
#  rate_limit:
#    login_attempts: 5
#    window: 300
#    lockout_duration: 900
#  mfa:
#    method: "totp"
#    issuer: "myapp"
#    digits: 6
#    period: 30
```

### **OAuth Configuration**
```bash
# oauth-config.tsk
oauth_config:
  provider: google
  client_id: "${GOOGLE_CLIENT_ID}"
  client_secret: "${GOOGLE_CLIENT_SECRET}"
  redirect_uri: "https://myapp.com/auth/callback"

#auth: oauth
#auth-provider: google
#auth-client-id: "${GOOGLE_CLIENT_ID}"
#auth-client-secret: "${GOOGLE_CLIENT_SECRET}"
#auth-redirect-uri: "https://myapp.com/auth/callback"

#auth-config:
#  oauth:
#    provider: "google"
#    client_id: "${GOOGLE_CLIENT_ID}"
#    client_secret: "${GOOGLE_CLIENT_SECRET}"
#    redirect_uri: "https://myapp.com/auth/callback"
#    authorization_url: "https://accounts.google.com/o/oauth2/auth"
#    token_url: "https://oauth2.googleapis.com/token"
#    user_info_url: "https://www.googleapis.com/oauth2/v2/userinfo"
#    scopes: ["openid", "email", "profile"]
#  session:
#    backend: "redis"
#    ttl: 3600
```

### **LDAP Configuration**
```bash
# ldap-config.tsk
ldap_config:
  server: "ldap://ldap.example.com:389"
  base_dn: "dc=example,dc=com"
  bind_dn: "cn=admin,dc=example,dc=com"
  bind_password: "${LDAP_BIND_PASSWORD}"

#auth: ldap
#auth-provider: ldap
#auth-server: "ldap://ldap.example.com:389"
#auth-base-dn: "dc=example,dc=com"
#auth-bind-dn: "cn=admin,dc=example,dc=com"
#auth-bind-password: "${LDAP_BIND_PASSWORD}"

#auth-config:
#  ldap:
#    server: "ldap://ldap.example.com:389"
#    base_dn: "dc=example,dc=com"
#    bind_dn: "cn=admin,dc=example,dc=com"
#    bind_password: "${LDAP_BIND_PASSWORD}"
#    user_search_filter: "(uid={username})"
#    group_search_filter: "(member={user_dn})"
#    attributes: ["cn", "mail", "uid", "memberOf"]
#  session:
#    backend: "file"
#    ttl: 1800
```

## 🚨 **Troubleshooting Auth Directives**

### **Common Issues and Solutions**

**1. JWT Token Issues**
```bash
# Debug JWT authentication
debug_jwt_auth() {
    local token="$1"
    
    echo "Debugging JWT authentication..."
    
    # Check token format
    if ! validate_jwt_format "$token"; then
        echo "✗ Invalid JWT format"
        return 1
    fi
    echo "✓ Valid JWT format"
    
    # Decode payload
    local payload=$(decode_jwt_payload "$token")
    if [[ $? -eq 0 ]]; then
        echo "✓ JWT payload decoded successfully"
        echo "Payload: $payload"
    else
        echo "✗ Failed to decode JWT payload"
        return 1
    fi
    
    # Check expiration
    if check_jwt_expiration "$payload"; then
        echo "✓ JWT token not expired"
    else
        echo "✗ JWT token expired"
        return 1
    fi
    
    # Check signature
    if verify_jwt_signature "$token" "$AUTH_SECRET"; then
        echo "✓ JWT signature valid"
    else
        echo "✗ JWT signature invalid"
        return 1
    fi
    
    echo "JWT authentication debug completed successfully"
}
```

**2. Session Issues**
```bash
# Debug session management
debug_session() {
    local session_id="$1"
    
    echo "Debugging session: $session_id"
    
    # Check session backend
    echo "Session backend: ${auth_session:-file}"
    
    # Try to load session
    local session=$(sessionManager.getSession(session_id))
    if [[ -n "$session" ]]; then
        echo "✓ Session found"
        echo "  User ID: ${session[userId]}"
        echo "  Created: ${session[createdAt]}"
        echo "  Expires: ${session[expiresAt]}"
    else
        echo "✗ Session not found or expired"
    fi
    
    # Check session storage
    case "${auth_session:-file}" in
        "redis")
            check_redis_session "$session_id"
            ;;
        "file")
            check_file_session "$session_id"
            ;;
        "memory")
            check_memory_session "$session_id"
            ;;
    esac
}

check_redis_session() {
    local session_id="$1"
    
    if command -v redis-cli >/dev/null 2>&1; then
        local session_data=$(redis-cli get "session:$session_id")
        if [[ -n "$session_data" ]]; then
            echo "✓ Session found in Redis"
        else
            echo "✗ Session not found in Redis"
        fi
    else
        echo "⚠ Redis CLI not available"
    fi
}

check_file_session() {
    local session_id="$1"
    local session_file="/tmp/sessions/$session_id.json"
    
    if [[ -f "$session_file" ]]; then
        echo "✓ Session file found: $session_file"
        echo "File size: $(stat -c %s "$session_file") bytes"
    else
        echo "✗ Session file not found: $session_file"
    fi
}
```

**3. OAuth Issues**
```bash
# Debug OAuth authentication
debug_oauth_auth() {
    local code="$1"
    
    echo "Debugging OAuth authentication..."
    
    # Check OAuth configuration
    echo "OAuth configuration:"
    echo "  Provider: ${oauth_provider}"
    echo "  Client ID: ${oauth_client_id:0:10}..."
    echo "  Redirect URI: ${oauth_redirect_uri}"
    echo "  Token URL: ${oauth_token_url}"
    
    # Test token exchange
    local token_response=$(exchange_oauth_code "$code" "${oauth_redirect_uri}")
    if [[ $? -eq 0 ]]; then
        echo "✓ Token exchange successful"
        echo "Response: $token_response"
    else
        echo "✗ Token exchange failed"
        return 1
    fi
    
    # Extract and verify access token
    local access_token=$(echo "$token_response" | jq -r '.access_token // empty')
    if [[ -n "$access_token" ]]; then
        echo "✓ Access token extracted"
        
        # Test user info endpoint
        local user_info=$(get_oauth_user_info "$access_token")
        if [[ $? -eq 0 ]]; then
            echo "✓ User info retrieved"
            echo "User info: $user_info"
        else
            echo "✗ Failed to get user info"
        fi
    else
        echo "✗ No access token in response"
    fi
}
```

## 🔒 **Security Best Practices**

### **Auth Security Checklist**
```bash
# Security validation
validate_auth_security() {
    echo "Validating auth security configuration..."
    
    # Check JWT configuration
    if [[ "$AUTH_METHOD" == "jwt" ]]; then
        if [[ -n "$AUTH_SECRET" ]]; then
            echo "✓ JWT secret configured"
            
            # Check secret strength
            if [[ ${#AUTH_SECRET} -ge 32 ]]; then
                echo "✓ JWT secret length adequate"
            else
                echo "⚠ JWT secret should be at least 32 characters"
            fi
        else
            echo "✗ JWT secret not configured"
        fi
        
        if [[ -n "$AUTH_ALGORITHM" ]]; then
            echo "✓ JWT algorithm configured: $AUTH_ALGORITHM"
        else
            echo "⚠ JWT algorithm not configured"
        fi
    fi
    
    # Check session configuration
    if [[ -n "${auth_session}" ]]; then
        echo "✓ Session backend configured: ${auth_session}"
        
        case "${auth_session}" in
            "redis")
                if command -v redis-cli >/dev/null 2>&1; then
                    echo "✓ Redis available for session storage"
                else
                    echo "⚠ Redis not available"
                fi
                ;;
            "file")
                local session_dir="/tmp/sessions"
                if [[ -d "$session_dir" ]] && [[ -w "$session_dir" ]]; then
                    echo "✓ Session directory writable: $session_dir"
                else
                    echo "⚠ Session directory not writable: $session_dir"
                fi
                ;;
        esac
    fi
    
    # Check rate limiting
    if [[ -n "${auth_rate_limit}" ]]; then
        echo "✓ Rate limiting configured: ${auth_rate_limit}"
    else
        echo "⚠ Rate limiting not configured"
    fi
    
    # Check password policy
    if [[ -n "${auth_password_hash}" ]]; then
        echo "✓ Password hashing configured: ${auth_password_hash}"
    else
        echo "⚠ Password hashing not configured"
    fi
    
    # Check MFA
    if [[ -n "${auth_mfa}" ]]; then
        echo "✓ MFA configured: ${auth_mfa}"
    else
        echo "⚠ MFA not configured"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Auth Performance Checklist**
```bash
# Performance validation
validate_auth_performance() {
    echo "Validating auth performance configuration..."
    
    # Check session TTL
    if [[ -n "${auth_session_ttl}" ]]; then
        echo "✓ Session TTL configured: ${auth_session_ttl}s"
        
        if [[ "${auth_session_ttl}" -gt 86400 ]]; then
            echo "⚠ Long session TTL may impact security"
        fi
    else
        echo "⚠ Session TTL not configured"
    fi
    
    # Check JWT expiration
    if [[ -n "$AUTH_EXPIRES" ]]; then
        echo "✓ JWT expiration configured: ${AUTH_EXPIRES}s"
        
        if [[ "$AUTH_EXPIRES" -gt 86400 ]]; then
            echo "⚠ Long JWT expiration may impact security"
        fi
    else
        echo "⚠ JWT expiration not configured"
    fi
    
    # Check session backend performance
    case "${auth_session:-file}" in
        "redis")
            echo "✓ Redis session backend (high performance)"
            ;;
        "memory")
            echo "✓ Memory session backend (fastest, not persistent)"
            ;;
        "file")
            echo "⚠ File session backend (slower, persistent)"
            ;;
    esac
    
    # Check caching
    if [[ -n "${auth_cache}" ]]; then
        echo "✓ Auth caching enabled: ${auth_cache}"
    else
        echo "⚠ Auth caching not configured"
    fi
}
```

## 🎯 **Next Steps**

- **Cache Directives**: Learn about caching-specific directives
- **Plugin Integration**: Explore auth plugins
- **Advanced Patterns**: Understand complex auth patterns
- **Testing Auth Directives**: Test auth functionality
- **Performance Tuning**: Optimize auth performance

---

**Auth directives transform your TuskLang configuration into a powerful authentication system. They bring modern security capabilities to your Bash applications with intelligent authentication methods, session management, and comprehensive security policies!** 
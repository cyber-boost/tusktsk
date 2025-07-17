# Security Best Practices in TuskLang - Bash Guide

## 🔒 **Revolutionary Security Configuration**

Security best practices in TuskLang transform your configuration files into intelligent security systems. No more separate security frameworks or complex security configurations - everything lives in your TuskLang configuration with dynamic security policies, automatic threat detection, and intelligent access control.

> **"We don't bow to any king"** - TuskLang security best practices break free from traditional security constraints and bring modern security capabilities to your Bash applications.

## 🚀 **Core Security Directives**

### **Basic Security Setup**
```bash
#security: enabled                  # Enable security features
#sec-enabled: true                 # Alternative syntax
#sec-encryption: AES-256-GCM       # Default encryption
#sec-authentication: jwt           # Authentication method
#sec-authorization: rbac           # Authorization method
#sec-audit: true                   # Enable audit logging
```

### **Advanced Security Configuration**
```bash
#sec-threat-detection: true        # Enable threat detection
#sec-vulnerability-scan: true      # Enable vulnerability scanning
#sec-secrets-management: true      # Enable secrets management
#sec-access-control: true          # Enable access control
#sec-compliance: gdpr              # Compliance framework
#sec-monitoring: true              # Enable security monitoring
```

## 🔧 **Bash Security Implementation**

### **Basic Security Manager**
```bash
#!/bin/bash

# Load security configuration
source <(tsk load security.tsk)

# Security configuration
SEC_ENABLED="${sec_enabled:-true}"
SEC_ENCRYPTION="${sec_encryption:-AES-256-GCM}"
SEC_AUTHENTICATION="${sec_authentication:-jwt}"
SEC_AUTHORIZATION="${sec_authorization:-rbac}"

# Security manager
class SecurityManager {
    constructor() {
        this.enabled = SEC_ENABLED
        this.encryption = SEC_ENCRYPTION
        this.authentication = SEC_AUTHENTICATION
        this.authorization = SEC_AUTHORIZATION
        this.threats = new Map()
        this.audit = []
        this.stats = {
            threats_detected: 0,
            access_denied: 0,
            authentication_failures: 0,
            encryption_operations: 0
        }
    }
    
    encrypt(data, key = null) {
        if (!this.enabled) return data
        
        const encryptionKey = key || this.getDefaultKey()
        
        try {
            const result = this.performEncryption(data, encryptionKey)
            this.stats.encryption_operations++
            this.logAudit('encrypt', { data_length: data.length, success: true })
            return result
        } catch (error) {
            this.logAudit('encrypt', { data_length: data.length, success: false, error: error.message })
            throw error
        }
    }
    
    decrypt(data, key = null) {
        if (!this.enabled) return data
        
        const encryptionKey = key || this.getDefaultKey()
        
        try {
            const result = this.performDecryption(data, encryptionKey)
            this.stats.encryption_operations++
            this.logAudit('decrypt', { data_length: data.length, success: true })
            return result
        } catch (error) {
            this.logAudit('decrypt', { data_length: data.length, success: false, error: error.message })
            throw error
        }
    }
    
    authenticate(credentials) {
        if (!this.enabled) return { success: true, user: 'anonymous' }
        
        try {
            const result = this.performAuthentication(credentials)
            
            if (result.success) {
                this.logAudit('authenticate', { user: result.user, success: true })
            } else {
                this.stats.authentication_failures++
                this.logAudit('authenticate', { credentials: credentials.username, success: false })
            }
            
            return result
        } catch (error) {
            this.stats.authentication_failures++
            this.logAudit('authenticate', { error: error.message, success: false })
            throw error
        }
    }
    
    authorize(user, resource, action) {
        if (!this.enabled) return { allowed: true }
        
        try {
            const result = this.performAuthorization(user, resource, action)
            
            if (!result.allowed) {
                this.stats.access_denied++
                this.logAudit('authorize', { user: user.id, resource, action, allowed: false })
            } else {
                this.logAudit('authorize', { user: user.id, resource, action, allowed: true })
            }
            
            return result
        } catch (error) {
            this.stats.access_denied++
            this.logAudit('authorize', { user: user.id, resource, action, error: error.message })
            throw error
        }
    }
    
    detectThreat(request) {
        if (!this.enabled) return { threat: false }
        
        const threats = this.analyzeThreats(request)
        
        if (threats.length > 0) {
            this.stats.threats_detected++
            this.logThreat(threats[0])
            return { threat: true, type: threats[0].type, severity: threats[0].severity }
        }
        
        return { threat: false }
    }
    
    scanVulnerabilities() {
        if (!this.enabled) return { vulnerabilities: [] }
        
        const vulnerabilities = this.performVulnerabilityScan()
        
        vulnerabilities.forEach(vuln => {
            this.logVulnerability(vuln)
        })
        
        return { vulnerabilities }
    }
    
    manageSecrets(operation, secret) {
        if (!this.enabled) return { success: true }
        
        try {
            const result = this.performSecretsManagement(operation, secret)
            this.logAudit('secrets_management', { operation, success: true })
            return result
        } catch (error) {
            this.logAudit('secrets_management', { operation, success: false, error: error.message })
            throw error
        }
    }
    
    logAudit(action, details) {
        const auditEntry = {
            timestamp: new Date().toISOString(),
            action,
            details,
            session_id: this.getSessionId()
        }
        
        this.audit.push(auditEntry)
        
        // Write to audit log file
        this.writeAuditLog(auditEntry)
    }
    
    logThreat(threat) {
        const threatEntry = {
            timestamp: new Date().toISOString(),
            type: threat.type,
            severity: threat.severity,
            source: threat.source,
            details: threat.details
        }
        
        this.threats.set(threat.id, threatEntry)
        
        // Write to threat log file
        this.writeThreatLog(threatEntry)
    }
    
    logVulnerability(vulnerability) {
        const vulnEntry = {
            timestamp: new Date().toISOString(),
            type: vulnerability.type,
            severity: vulnerability.severity,
            location: vulnerability.location,
            description: vulnerability.description
        }
        
        // Write to vulnerability log file
        this.writeVulnerabilityLog(vulnEntry)
    }
    
    getStats() {
        return { ...this.stats }
    }
    
    getAuditLog() {
        return [...this.audit]
    }
    
    getThreats() {
        return Array.from(this.threats.values())
    }
}

# Initialize security manager
const securityManager = new SecurityManager()
```

### **Encryption Implementation**
```bash
#!/bin/bash

# Encryption implementation
encryption_implementation() {
    local operation="$1"
    local data="$2"
    local key="$3"
    local algorithm="$4"
    
    case "$operation" in
        "encrypt")
            encrypt_data "$data" "$key" "$algorithm"
            ;;
        "decrypt")
            decrypt_data "$data" "$key" "$algorithm"
            ;;
        "generate-key")
            generate_encryption_key "$algorithm"
            ;;
        "hash")
            hash_data "$data" "$algorithm"
            ;;
        *)
            echo "Unknown encryption operation: $operation"
            return 1
            ;;
    esac
}

encrypt_data() {
    local data="$1"
    local key="$2"
    local algorithm="${3:-AES-256-GCM}"
    
    echo "Encrypting data with $algorithm..."
    
    case "$algorithm" in
        "AES-256-GCM")
            encrypt_aes_gcm "$data" "$key"
            ;;
        "ChaCha20-Poly1305")
            encrypt_chacha20 "$data" "$key"
            ;;
        "RSA-2048")
            encrypt_rsa "$data" "$key"
            ;;
        *)
            echo "Unsupported encryption algorithm: $algorithm"
            return 1
            ;;
    esac
}

encrypt_aes_gcm() {
    local data="$1"
    local key="$2"
    
    # Generate random IV
    local iv=$(openssl rand -hex 16)
    
    # Encrypt data
    local encrypted=$(echo -n "$data" | openssl enc -aes-256-gcm -a -A -K "$key" -iv "$iv" 2>/dev/null)
    
    if [[ $? -eq 0 ]]; then
        echo "$iv:$encrypted"
        return 0
    else
        echo "Encryption failed"
        return 1
    fi
}

decrypt_data() {
    local encrypted_data="$1"
    local key="$2"
    local algorithm="${3:-AES-256-GCM}"
    
    echo "Decrypting data with $algorithm..."
    
    case "$algorithm" in
        "AES-256-GCM")
            decrypt_aes_gcm "$encrypted_data" "$key"
            ;;
        "ChaCha20-Poly1305")
            decrypt_chacha20 "$encrypted_data" "$key"
            ;;
        "RSA-2048")
            decrypt_rsa "$encrypted_data" "$key"
            ;;
        *)
            echo "Unsupported decryption algorithm: $algorithm"
            return 1
            ;;
    esac
}

decrypt_aes_gcm() {
    local encrypted_data="$1"
    local key="$2"
    
    # Split IV and encrypted data
    IFS=':' read -r iv encrypted <<< "$encrypted_data"
    
    # Decrypt data
    local decrypted=$(echo "$encrypted" | openssl enc -aes-256-gcm -a -A -d -K "$key" -iv "$iv" 2>/dev/null)
    
    if [[ $? -eq 0 ]]; then
        echo "$decrypted"
        return 0
    else
        echo "Decryption failed"
        return 1
    fi
}

generate_encryption_key() {
    local algorithm="${1:-AES-256-GCM}"
    
    echo "Generating encryption key for $algorithm..."
    
    case "$algorithm" in
        "AES-256-GCM")
            # Generate 256-bit key
            openssl rand -hex 32
            ;;
        "ChaCha20-Poly1305")
            # Generate 256-bit key
            openssl rand -hex 32
            ;;
        "RSA-2048")
            # Generate RSA key pair
            openssl genrsa -out private_key.pem 2048
            openssl rsa -in private_key.pem -pubout -out public_key.pem
            echo "RSA key pair generated: private_key.pem, public_key.pem"
            ;;
        *)
            echo "Unsupported algorithm for key generation: $algorithm"
            return 1
            ;;
    esac
}

hash_data() {
    local data="$1"
    local algorithm="${2:-SHA-256}"
    
    echo "Hashing data with $algorithm..."
    
    case "$algorithm" in
        "SHA-256")
            echo -n "$data" | openssl dgst -sha256 -hex | cut -d' ' -f2
            ;;
        "SHA-512")
            echo -n "$data" | openssl dgst -sha512 -hex | cut -d' ' -f2
            ;;
        "bcrypt")
            echo -n "$data" | openssl passwd -6 -salt "$(openssl rand -base64 16)"
            ;;
        *)
            echo "Unsupported hashing algorithm: $algorithm"
            return 1
            ;;
    esac
}
```

### **Authentication Implementation**
```bash
#!/bin/bash

# Authentication implementation
authentication_implementation() {
    local operation="$1"
    local credentials="$2"
    local options="$3"
    
    case "$operation" in
        "authenticate")
            authenticate_user "$credentials" "$options"
            ;;
        "validate-token")
            validate_token "$credentials"
            ;;
        "generate-token")
            generate_token "$credentials" "$options"
            ;;
        "refresh-token")
            refresh_token "$credentials"
            ;;
        *)
            echo "Unknown authentication operation: $operation"
            return 1
            ;;
    esac
}

authenticate_user() {
    local credentials="$1"
    local options="$2"
    
    echo "Authenticating user..."
    
    # Parse credentials
    local username=$(echo "$credentials" | jq -r '.username // empty')
    local password=$(echo "$credentials" | jq -r '.password // empty')
    
    if [[ -z "$username" ]] || [[ -z "$password" ]]; then
        echo "Invalid credentials format"
        return 1
    fi
    
    # Check authentication method
    local auth_method="${sec_authentication:-jwt}"
    
    case "$auth_method" in
        "jwt")
            authenticate_jwt "$username" "$password"
            ;;
        "ldap")
            authenticate_ldap "$username" "$password"
            ;;
        "oauth")
            authenticate_oauth "$username" "$password"
            ;;
        "database")
            authenticate_database "$username" "$password"
            ;;
        *)
            echo "Unsupported authentication method: $auth_method"
            return 1
            ;;
    esac
}

authenticate_jwt() {
    local username="$1"
    local password="$2"
    
    # Validate credentials against user database
    local user=$(validate_user_credentials "$username" "$password")
    
    if [[ -n "$user" ]]; then
        # Generate JWT token
        local token=$(generate_jwt_token "$user")
        
        echo "Authentication successful"
        echo "Token: $token"
        return 0
    else
        echo "Authentication failed"
        return 1
    fi
}

validate_user_credentials() {
    local username="$1"
    local password="$2"
    
    # Hash password for comparison
    local hashed_password=$(hash_data "$password" "SHA-256")
    
    # Check against user database (simplified)
    local user_file="/etc/users/$username.json"
    
    if [[ -f "$user_file" ]]; then
        local stored_hash=$(jq -r '.password_hash' "$user_file")
        
        if [[ "$hashed_password" == "$stored_hash" ]]; then
            jq -r '.' "$user_file"
            return 0
        fi
    fi
    
    return 1
}

generate_jwt_token() {
    local user="$1"
    
    # JWT configuration
    local secret="${jwt_secret:-default_secret}"
    local algorithm="${jwt_algorithm:-HS256}"
    local expires="${jwt_expires:-3600}"
    
    # Create JWT payload
    local payload=$(cat << EOF
{
    "sub": "$(echo "$user" | jq -r '.id')",
    "name": "$(echo "$user" | jq -r '.name')",
    "email": "$(echo "$user" | jq -r '.email')",
    "iat": $(date +%s),
    "exp": $(($(date +%s) + expires)),
    "iss": "tusk-app"
}
EOF
)
    
    # Encode header
    local header=$(echo '{"alg":"HS256","typ":"JWT"}' | base64 | tr -d '=' | tr '/+' '_-')
    
    # Encode payload
    local encoded_payload=$(echo "$payload" | base64 | tr -d '=' | tr '/+' '_-')
    
    # Create signature
    local signature=$(echo -n "$header.$encoded_payload" | openssl dgst -sha256 -hmac "$secret" | cut -d' ' -f2)
    
    # Combine parts
    echo "$header.$encoded_payload.$signature"
}

validate_token() {
    local token="$1"
    
    echo "Validating JWT token..."
    
    # JWT configuration
    local secret="${jwt_secret:-default_secret}"
    
    # Split token parts
    IFS='.' read -r header payload signature <<< "$token"
    
    # Verify signature
    local expected_signature=$(echo -n "$header.$payload" | openssl dgst -sha256 -hmac "$secret" | cut -d' ' -f2)
    
    if [[ "$signature" != "$expected_signature" ]]; then
        echo "Invalid token signature"
        return 1
    fi
    
    # Decode payload
    local decoded_payload=$(echo "$payload" | tr '_-' '/+' | base64 -d 2>/dev/null)
    
    if [[ $? -ne 0 ]]; then
        echo "Invalid token payload"
        return 1
    fi
    
    # Check expiration
    local expiration=$(echo "$decoded_payload" | jq -r '.exp')
    local current_time=$(date +%s)
    
    if [[ "$current_time" -gt "$expiration" ]]; then
        echo "Token expired"
        return 1
    fi
    
    echo "Token valid"
    echo "$decoded_payload"
    return 0
}
```

### **Authorization Implementation**
```bash
#!/bin/bash

# Authorization implementation
authorization_implementation() {
    local operation="$1"
    local user="$2"
    local resource="$3"
    local action="$4"
    
    case "$operation" in
        "authorize")
            authorize_access "$user" "$resource" "$action"
            ;;
        "check-permission")
            check_permission "$user" "$resource" "$action"
            ;;
        "get-roles")
            get_user_roles "$user"
            ;;
        "assign-role")
            assign_role "$user" "$action"
            ;;
        *)
            echo "Unknown authorization operation: $operation"
            return 1
            ;;
    esac
}

authorize_access() {
    local user="$1"
    local resource="$2"
    local action="$3"
    
    echo "Authorizing access for user $(echo "$user" | jq -r '.id') to $resource:$action"
    
    # Check authorization method
    local auth_method="${sec_authorization:-rbac}"
    
    case "$auth_method" in
        "rbac")
            authorize_rbac "$user" "$resource" "$action"
            ;;
        "abac")
            authorize_abac "$user" "$resource" "$action"
            ;;
        "dac")
            authorize_dac "$user" "$resource" "$action"
            ;;
        *)
            echo "Unsupported authorization method: $auth_method"
            return 1
            ;;
    esac
}

authorize_rbac() {
    local user="$1"
    local resource="$2"
    local action="$3"
    
    # Get user roles
    local user_id=$(echo "$user" | jq -r '.id')
    local roles=$(get_user_roles "$user_id")
    
    # Check each role for permissions
    while IFS= read -r role; do
        if [[ -n "$role" ]]; then
            local permissions=$(get_role_permissions "$role")
            
            # Check if role has permission for resource:action
            if echo "$permissions" | grep -q "$resource:$action"; then
                echo "Access granted via role: $role"
                return 0
            fi
        fi
    done <<< "$roles"
    
    echo "Access denied: no role has permission for $resource:$action"
    return 1
}

get_user_roles() {
    local user_id="$1"
    
    # Get roles from user database
    local user_file="/etc/users/$user_id.json"
    
    if [[ -f "$user_file" ]]; then
        jq -r '.roles[]?' "$user_file" 2>/dev/null
    else
        echo "default"
    fi
}

get_role_permissions() {
    local role="$1"
    
    # Get permissions from role database
    local role_file="/etc/roles/$role.json"
    
    if [[ -f "$role_file" ]]; then
        jq -r '.permissions[]?' "$role_file" 2>/dev/null
    else
        # Default permissions for common roles
        case "$role" in
            "admin")
                echo "all:all"
                ;;
            "user")
                echo "profile:read"
                echo "profile:write"
                ;;
            "guest")
                echo "public:read"
                ;;
            *)
                echo ""
                ;;
        esac
    fi
}

check_permission() {
    local user="$1"
    local resource="$2"
    local action="$3"
    
    # Check if user has explicit permission
    local user_id=$(echo "$user" | jq -r '.id')
    local permissions_file="/etc/permissions/$user_id.json"
    
    if [[ -f "$permissions_file" ]]; then
        local explicit_permissions=$(jq -r '.permissions[]?' "$permissions_file" 2>/dev/null)
        
        if echo "$explicit_permissions" | grep -q "$resource:$action"; then
            echo "Explicit permission granted"
            return 0
        fi
    fi
    
    # Check role-based permissions
    authorize_rbac "$user" "$resource" "$action"
}
```

### **Threat Detection**
```bash
#!/bin/bash

# Threat detection implementation
threat_detection() {
    local operation="$1"
    local request="$2"
    
    case "$operation" in
        "analyze")
            analyze_threats "$request"
            ;;
        "detect-sql-injection")
            detect_sql_injection "$request"
            ;;
        "detect-xss")
            detect_xss "$request"
            ;;
        "detect-csrf")
            detect_csrf "$request"
            ;;
        "detect-brute-force")
            detect_brute_force "$request"
            ;;
        *)
            echo "Unknown threat detection operation: $operation"
            return 1
            ;;
    esac
}

analyze_threats() {
    local request="$1"
    
    echo "Analyzing request for threats..."
    
    local threats=()
    
    # Check for SQL injection
    if detect_sql_injection "$request"; then
        threats+=("sql_injection")
    fi
    
    # Check for XSS
    if detect_xss "$request"; then
        threats+=("xss")
    fi
    
    # Check for CSRF
    if detect_csrf "$request"; then
        threats+=("csrf")
    fi
    
    # Check for brute force
    if detect_brute_force "$request"; then
        threats+=("brute_force")
    fi
    
    # Check for path traversal
    if detect_path_traversal "$request"; then
        threats+=("path_traversal")
    fi
    
    # Check for command injection
    if detect_command_injection "$request"; then
        threats+=("command_injection")
    fi
    
    if [[ ${#threats[@]} -gt 0 ]]; then
        echo "Threats detected: ${threats[*]}"
        return 1
    else
        echo "No threats detected"
        return 0
    fi
}

detect_sql_injection() {
    local request="$1"
    
    # SQL injection patterns
    local patterns=(
        "'; DROP TABLE"
        "'; INSERT INTO"
        "'; UPDATE"
        "'; DELETE FROM"
        "'; SELECT"
        "UNION SELECT"
        "OR 1=1"
        "OR '1'='1"
        "'; --"
        "'; /*"
    )
    
    for pattern in "${patterns[@]}"; do
        if echo "$request" | grep -qi "$pattern"; then
            echo "SQL injection detected: $pattern"
            return 0
        fi
    done
    
    return 1
}

detect_xss() {
    local request="$1"
    
    # XSS patterns
    local patterns=(
        "<script>"
        "javascript:"
        "onload="
        "onerror="
        "onclick="
        "onmouseover="
        "alert("
        "confirm("
        "prompt("
        "document.cookie"
    )
    
    for pattern in "${patterns[@]}"; do
        if echo "$request" | grep -qi "$pattern"; then
            echo "XSS detected: $pattern"
            return 0
        fi
    done
    
    return 1
}

detect_csrf() {
    local request="$1"
    
    # CSRF detection (simplified)
    local referer=$(echo "$request" | grep -i "referer:" | cut -d' ' -f2)
    local origin=$(echo "$request" | grep -i "origin:" | cut -d' ' -f2)
    
    # Check if request has proper CSRF token
    local csrf_token=$(echo "$request" | grep -i "csrf-token:" | cut -d' ' -f2)
    
    if [[ -z "$csrf_token" ]]; then
        echo "CSRF protection missing: no CSRF token"
        return 0
    fi
    
    # Validate CSRF token (simplified)
    if ! validate_csrf_token "$csrf_token"; then
        echo "CSRF detected: invalid CSRF token"
        return 0
    fi
    
    return 1
}

detect_brute_force() {
    local request="$1"
    
    # Extract IP address
    local ip=$(echo "$request" | grep -oE "\b([0-9]{1,3}\.){3}[0-9]{1,3}\b" | head -1)
    
    if [[ -n "$ip" ]]; then
        # Check failed login attempts
        local failed_attempts=$(get_failed_attempts "$ip")
        
        if [[ "$failed_attempts" -gt 5 ]]; then
            echo "Brute force detected: $failed_attempts failed attempts from $ip"
            return 0
        fi
    fi
    
    return 1
}

detect_path_traversal() {
    local request="$1"
    
    # Path traversal patterns
    local patterns=(
        "\.\./"
        "\.\.\\"
        "\.\.%2f"
        "\.\.%5c"
        "\.\.%2e%2e"
        "\.\.%252e%252e"
    )
    
    for pattern in "${patterns[@]}"; do
        if echo "$request" | grep -qi "$pattern"; then
            echo "Path traversal detected: $pattern"
            return 0
        fi
    done
    
    return 1
}

detect_command_injection() {
    local request="$1"
    
    # Command injection patterns
    local patterns=(
        "; ls"
        "; cat"
        "; rm"
        "; wget"
        "; curl"
        "| ls"
        "| cat"
        "| rm"
        "`ls`"
        "\$\(ls\)"
    )
    
    for pattern in "${patterns[@]}"; do
        if echo "$request" | grep -qi "$pattern"; then
            echo "Command injection detected: $pattern"
            return 0
        fi
    done
    
    return 1
}

get_failed_attempts() {
    local ip="$1"
    
    # Count failed login attempts from log file
    local log_file="/var/log/auth.log"
    
    if [[ -f "$log_file" ]]; then
        grep "Failed password" "$log_file" | grep "$ip" | wc -l
    else
        echo "0"
    fi
}

validate_csrf_token() {
    local token="$1"
    
    # Simplified CSRF token validation
    # In production, use proper token validation
    
    if [[ -n "$token" ]] && [[ ${#token} -ge 32 ]]; then
        return 0
    else
        return 1
    fi
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Security Configuration**
```bash
# security-config.tsk
security_config:
  enabled: true
  encryption: AES-256-GCM
  authentication: jwt
  authorization: rbac

#security: enabled
#sec-enabled: true
#sec-encryption: AES-256-GCM
#sec-authentication: jwt
#sec-authorization: rbac

#sec-threat-detection: true
#sec-vulnerability-scan: true
#sec-secrets-management: true
#sec-access-control: true
#sec-compliance: gdpr
#sec-monitoring: true

#sec-config:
#  encryption:
#    algorithm: AES-256-GCM
#    key_rotation: true
#    key_rotation_interval: 30
#    key_storage: vault
#  authentication:
#    method: jwt
#    jwt_secret: "${JWT_SECRET}"
#    jwt_expires: 3600
#    jwt_refresh: true
#    mfa: true
#    mfa_method: totp
#  authorization:
#    method: rbac
#    roles:
#      - name: admin
#        permissions: ["all:all"]
#      - name: user
#        permissions: ["profile:read", "profile:write"]
#      - name: guest
#        permissions: ["public:read"]
#  threat_detection:
#    enabled: true
#    patterns:
#      - type: sql_injection
#        patterns: ["'; DROP TABLE", "UNION SELECT"]
#      - type: xss
#        patterns: ["<script>", "javascript:"]
#      - type: csrf
#        patterns: ["missing_csrf_token"]
#    actions:
#      - block_ip
#      - log_threat
#      - send_alert
#  vulnerability_scan:
#    enabled: true
#    schedule: "0 2 * * *"
#    tools: ["nmap", "nikto", "sqlmap"]
#    reporting: true
#  secrets_management:
#    enabled: true
#    backend: vault
#    encryption: true
#    rotation: true
#  access_control:
#    enabled: true
#    ip_whitelist: ["10.0.0.0/8", "192.168.1.0/24"]
#    ip_blacklist: ["192.168.1.100"]
#    rate_limiting: true
#    max_requests_per_minute: 100
#  compliance:
#    framework: gdpr
#    data_encryption: true
#    data_retention: 30
#    audit_logging: true
#    privacy_policy: true
#  monitoring:
#    enabled: true
#    metrics: ["authentication_failures", "access_denied", "threats_detected"]
#    alerts: true
#    dashboard: true
```

### **Application-Specific Security**
```bash
# app-security.tsk
app_config:
  name: "Secure Application"
  version: "2.0.0"

#sec-enabled: true
#sec-encryption: AES-256-GCM
#sec-authentication: jwt

#sec-config:
#  web_application:
#    encryption:
#      ssl: true
#      ssl_cert: "/etc/ssl/certs/app.crt"
#      ssl_key: "/etc/ssl/private/app.key"
#      hsts: true
#      csp: true
#    authentication:
#      method: jwt
#      session_timeout: 1800
#      max_sessions: 5
#      password_policy:
#        min_length: 12
#        require_uppercase: true
#        require_lowercase: true
#        require_numbers: true
#        require_special: true
#    authorization:
#      method: rbac
#      roles:
#        - name: admin
#          permissions: ["all:all"]
#        - name: manager
#          permissions: ["users:read", "users:write", "reports:read"]
#        - name: user
#          permissions: ["profile:read", "profile:write"]
#    threat_detection:
#      sql_injection: true
#      xss: true
#      csrf: true
#      brute_force: true
#      rate_limiting: true
#    data_protection:
#      encryption_at_rest: true
#      encryption_in_transit: true
#      data_masking: true
#      audit_logging: true
#  api_security:
#    authentication: api_key
#    rate_limiting: true
#    request_validation: true
#    response_validation: true
#    cors: true
#    cors_origins: ["https://app.example.com"]
```

## 🚨 **Troubleshooting Security Issues**

### **Common Issues and Solutions**

**1. Authentication Issues**
```bash
# Debug authentication
debug_authentication() {
    local user_id="$1"
    
    echo "Debugging authentication for user: $user_id"
    
    # Check user existence
    local user_file="/etc/users/$user_id.json"
    if [[ -f "$user_file" ]]; then
        echo "✓ User exists"
        
        # Check user status
        local status=$(jq -r '.status' "$user_file" 2>/dev/null)
        if [[ "$status" == "active" ]]; then
            echo "✓ User is active"
        else
            echo "✗ User is not active: $status"
        fi
        
        # Check password policy
        local password_hash=$(jq -r '.password_hash' "$user_file" 2>/dev/null)
        if [[ -n "$password_hash" ]]; then
            echo "✓ Password hash exists"
        else
            echo "✗ No password hash found"
        fi
    else
        echo "✗ User does not exist"
    fi
    
    # Check authentication method
    local auth_method="${sec_authentication:-jwt}"
    echo "Authentication method: $auth_method"
    
    # Check JWT configuration
    if [[ "$auth_method" == "jwt" ]]; then
        if [[ -n "${jwt_secret}" ]]; then
            echo "✓ JWT secret configured"
        else
            echo "⚠ JWT secret not configured"
        fi
        
        if [[ -n "${jwt_expires}" ]]; then
            echo "✓ JWT expiration configured: ${jwt_expires}s"
        else
            echo "⚠ JWT expiration not configured"
        fi
    fi
    
    # Check authentication logs
    local auth_log="/var/log/auth.log"
    if [[ -f "$auth_log" ]]; then
        echo "Recent authentication attempts:"
        grep "authentication" "$auth_log" | tail -5
    fi
}

debug_authorization() {
    local user_id="$1"
    local resource="$2"
    local action="$3"
    
    echo "Debugging authorization for user: $user_id, resource: $resource, action: $action"
    
    # Get user roles
    local roles=$(get_user_roles "$user_id")
    echo "User roles: $roles"
    
    # Check each role for permissions
    while IFS= read -r role; do
        if [[ -n "$role" ]]; then
            echo "Checking role: $role"
            local permissions=$(get_role_permissions "$role")
            echo "Role permissions: $permissions"
            
            if echo "$permissions" | grep -q "$resource:$action"; then
                echo "✓ Permission found in role: $role"
                return 0
            fi
        fi
    done <<< "$roles"
    
    echo "✗ No permission found for $resource:$action"
    return 1
}
```

**2. Encryption Issues**
```bash
# Debug encryption
debug_encryption() {
    local algorithm="${sec_encryption:-AES-256-GCM}"
    
    echo "Debugging encryption with algorithm: $algorithm"
    
    # Check if OpenSSL is available
    if command -v openssl >/dev/null 2>&1; then
        echo "✓ OpenSSL is available"
        
        # Test encryption/decryption
        local test_data="test_data_123"
        local test_key=$(openssl rand -hex 32)
        
        echo "Testing encryption/decryption..."
        
        case "$algorithm" in
            "AES-256-GCM")
                test_aes_encryption "$test_data" "$test_key"
                ;;
            "ChaCha20-Poly1305")
                test_chacha20_encryption "$test_data" "$test_key"
                ;;
            *)
                echo "⚠ Unsupported algorithm for testing: $algorithm"
                ;;
        esac
    else
        echo "✗ OpenSSL is not available"
    fi
    
    # Check key management
    if [[ -n "${encryption_key}" ]]; then
        echo "✓ Encryption key configured"
    else
        echo "⚠ Encryption key not configured"
    fi
}

test_aes_encryption() {
    local data="$1"
    local key="$2"
    
    # Generate IV
    local iv=$(openssl rand -hex 16)
    
    # Encrypt
    local encrypted=$(echo -n "$data" | openssl enc -aes-256-gcm -a -A -K "$key" -iv "$iv" 2>/dev/null)
    
    if [[ $? -eq 0 ]]; then
        echo "✓ Encryption successful"
        
        # Decrypt
        local decrypted=$(echo "$encrypted" | openssl enc -aes-256-gcm -a -A -d -K "$key" -iv "$iv" 2>/dev/null)
        
        if [[ $? -eq 0 ]] && [[ "$decrypted" == "$data" ]]; then
            echo "✓ Decryption successful"
        else
            echo "✗ Decryption failed"
        fi
    else
        echo "✗ Encryption failed"
    fi
}
```

## 🔒 **Security Best Practices**

### **Security Checklist**
```bash
# Security validation
validate_security_configuration() {
    echo "Validating security configuration..."
    
    # Check encryption configuration
    if [[ "${sec_encryption}" ]]; then
        echo "✓ Encryption configured: ${sec_encryption}"
        
        case "${sec_encryption}" in
            "AES-256-GCM"|"ChaCha20-Poly1305")
                echo "✓ Strong encryption algorithm"
                ;;
            *)
                echo "⚠ Consider using AES-256-GCM or ChaCha20-Poly1305"
                ;;
        esac
    else
        echo "⚠ Encryption not configured"
    fi
    
    # Check authentication configuration
    if [[ "${sec_authentication}" ]]; then
        echo "✓ Authentication configured: ${sec_authentication}"
        
        if [[ "${sec_authentication}" == "jwt" ]]; then
            if [[ -n "${jwt_secret}" ]]; then
                echo "✓ JWT secret configured"
                
                if [[ ${#jwt_secret} -ge 32 ]]; then
                    echo "✓ JWT secret length adequate"
                else
                    echo "⚠ JWT secret should be at least 32 characters"
                fi
            else
                echo "⚠ JWT secret not configured"
            fi
        fi
    else
        echo "⚠ Authentication not configured"
    fi
    
    # Check authorization configuration
    if [[ "${sec_authorization}" ]]; then
        echo "✓ Authorization configured: ${sec_authorization}"
    else
        echo "⚠ Authorization not configured"
    fi
    
    # Check threat detection
    if [[ "${sec_threat_detection}" == "true" ]]; then
        echo "✓ Threat detection enabled"
    else
        echo "⚠ Threat detection not enabled"
    fi
    
    # Check vulnerability scanning
    if [[ "${sec_vulnerability_scan}" == "true" ]]; then
        echo "✓ Vulnerability scanning enabled"
    else
        echo "⚠ Vulnerability scanning not enabled"
    fi
    
    # Check secrets management
    if [[ "${sec_secrets_management}" == "true" ]]; then
        echo "✓ Secrets management enabled"
    else
        echo "⚠ Secrets management not enabled"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Security Performance Checklist**
```bash
# Performance validation
validate_security_performance() {
    echo "Validating security performance configuration..."
    
    # Check encryption performance
    local encryption_algorithm="${sec_encryption:-AES-256-GCM}"
    case "$encryption_algorithm" in
        "AES-256-GCM")
            echo "✓ AES-256-GCM (hardware accelerated)"
            ;;
        "ChaCha20-Poly1305")
            echo "✓ ChaCha20-Poly1305 (fast software implementation)"
            ;;
        *)
            echo "⚠ Consider performance implications of $encryption_algorithm"
            ;;
    esac
    
    # Check authentication performance
    local auth_method="${sec_authentication:-jwt}"
    case "$auth_method" in
        "jwt")
            echo "✓ JWT (stateless, scalable)"
            ;;
        "session")
            echo "⚠ Session-based (requires server state)"
            ;;
        *)
            echo "⚠ Consider performance implications of $auth_method"
            ;;
    esac
    
    # Check threat detection performance
    if [[ "${sec_threat_detection}" == "true" ]]; then
        echo "✓ Threat detection enabled"
        
        # Check if threat detection is optimized
        if [[ "${sec_threat_detection_optimized}" == "true" ]]; then
            echo "✓ Threat detection optimized"
        else
            echo "⚠ Consider optimizing threat detection for performance"
        fi
    fi
    
    # Check monitoring overhead
    if [[ "${sec_monitoring}" == "true" ]]; then
        echo "✓ Security monitoring enabled"
        
        local monitoring_interval="${sec_monitoring_interval:-60}"
        if [[ "$monitoring_interval" -ge 30 ]]; then
            echo "✓ Monitoring interval reasonable: ${monitoring_interval}s"
        else
            echo "⚠ High-frequency monitoring may impact performance"
        fi
    fi
}
```

## 🎯 **Next Steps**

- **Compliance Frameworks**: Learn about security compliance
- **Plugin Integration**: Explore security plugins
- **Advanced Patterns**: Understand complex security patterns
- **Incident Response**: Implement security incident response
- **Security Testing**: Test security configurations

---

**Security best practices transform your TuskLang configuration into a secure system. They bring modern security capabilities to your Bash applications with intelligent threat detection, comprehensive access control, and robust encryption!** 
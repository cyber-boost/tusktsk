# 🟨 TuskLang JavaScript Security Best Practices Guide

**"We don't bow to any king" - JavaScript Edition**

Secure your TuskLang-powered JavaScript applications with industry-leading security practices. Learn authentication, authorization, data protection, and security monitoring.

## 🔐 Authentication & Authorization

### JWT-Based Authentication

```javascript
// security/authentication.js
const jwt = require('jsonwebtoken');
const bcrypt = require('bcrypt');
const TuskLang = require('tusklang');

class SecurityManager {
    constructor(tsk) {
        this.tsk = tsk;
        this.config = this.loadSecurityConfig();
    }

    async loadSecurityConfig() {
        return await this.tsk.parse(TuskLang.parse(`
            security {
                # JWT Configuration
                jwt {
                    secret: @env.secure("JWT_SECRET")
                    expires_in: "24h"
                    refresh_expires_in: "7d"
                    issuer: "myapp.com"
                    audience: "myapp-users"
                    
                    # Token rotation
                    rotation_enabled: true
                    rotation_interval: "12h"
                    max_refresh_tokens: 5
                }
                
                # Password Security
                password {
                    min_length: 12
                    require_uppercase: true
                    require_lowercase: true
                    require_numbers: true
                    require_special: true
                    bcrypt_rounds: 12
                    max_age_days: 90
                }
                
                # Session Management
                session {
                    max_sessions_per_user: 5
                    session_timeout: "2h"
                    idle_timeout: "30m"
                    secure_cookies: @if(@env("NODE_ENV") == "production", true, false)
                    http_only_cookies: true
                    same_site: "strict"
                }
                
                # Rate Limiting
                rate_limit {
                    login_attempts: 5
                    login_window: "15m"
                    api_requests: 100
                    api_window: "1h"
                    password_reset: 3
                    password_reset_window: "1h"
                }
                
                # Multi-Factor Authentication
                mfa {
                    enabled: true
                    required_for_admin: true
                    backup_codes_count: 10
                    totp_issuer: "MyApp"
                    totp_algorithm: "sha1"
                    totp_digits: 6
                    totp_period: 30
                }
            }
        `));
    }

    // Secure user authentication
    async authenticateUser(email, password, mfaCode = null) {
        try {
            const user = await this.tsk.parse(TuskLang.parse(`
                user_auth {
                    user: @query("""
                        SELECT id, email, password_hash, mfa_enabled, mfa_secret, 
                               failed_login_attempts, locked_until, last_login
                        FROM users 
                        WHERE email = ? AND active = true
                    """, "${email}")
                    
                    # Check if account is locked
                    is_locked: @if(@user.locked_until > @date.now(), true, false)
                    
                    # Check rate limiting
                    rate_limit_exceeded: @if(@user.failed_login_attempts >= ${this.config.security.rate_limit.login_attempts}, true, false)
                }
            `));

            if (!user.user_auth.user) {
                throw new Error('Invalid credentials');
            }

            if (user.user_auth.is_locked) {
                throw new Error('Account temporarily locked');
            }

            if (user.user_auth.rate_limit_exceeded) {
                throw new Error('Too many failed login attempts');
            }

            // Verify password
            const isValidPassword = await bcrypt.compare(password, user.user_auth.user.password_hash);
            if (!isValidPassword) {
                await this.recordFailedLogin(user.user_auth.user.id);
                throw new Error('Invalid credentials');
            }

            // Verify MFA if enabled
            if (user.user_auth.user.mfa_enabled) {
                if (!mfaCode) {
                    throw new Error('MFA code required');
                }
                
                const isValidMFA = await this.verifyMFA(user.user_auth.user.mfa_secret, mfaCode);
                if (!isValidMFA) {
                    await this.recordFailedLogin(user.user_auth.user.id);
                    throw new Error('Invalid MFA code');
                }
            }

            // Generate JWT token
            const token = await this.generateJWT(user.user_auth.user.id);
            
            // Update last login
            await this.updateLastLogin(user.user_auth.user.id);
            
            // Reset failed login attempts
            await this.resetFailedLogins(user.user_auth.user.id);

            return {
                token,
                user: {
                    id: user.user_auth.user.id,
                    email: user.user_auth.user.email,
                    mfa_enabled: user.user_auth.user.mfa_enabled
                }
            };
        } catch (error) {
            console.error('Authentication error:', error);
            throw error;
        }
    }

    // Generate secure JWT token
    async generateJWT(userId) {
        const payload = {
            sub: userId,
            iat: Math.floor(Date.now() / 1000),
            exp: Math.floor(Date.now() / 1000) + (24 * 60 * 60), // 24 hours
            iss: this.config.security.jwt.issuer,
            aud: this.config.security.jwt.audience,
            jti: this.generateTokenId()
        };

        return jwt.sign(payload, this.config.security.jwt.secret, {
            algorithm: 'HS256'
        });
    }

    // Verify JWT token
    async verifyJWT(token) {
        try {
            const decoded = jwt.verify(token, this.config.security.jwt.secret, {
                issuer: this.config.security.jwt.issuer,
                audience: this.config.security.jwt.audience,
                algorithms: ['HS256']
            });

            // Check if token is blacklisted
            const isBlacklisted = await this.isTokenBlacklisted(decoded.jti);
            if (isBlacklisted) {
                throw new Error('Token has been revoked');
            }

            return decoded;
        } catch (error) {
            console.error('JWT verification error:', error);
            throw new Error('Invalid token');
        }
    }

    // Record failed login attempt
    async recordFailedLogin(userId) {
        await this.tsk.parse(TuskLang.parse(`
            failed_login: @query("""
                UPDATE users 
                SET failed_login_attempts = failed_login_attempts + 1,
                    locked_until = CASE 
                        WHEN failed_login_attempts + 1 >= ${this.config.security.rate_limit.login_attempts}
                        THEN NOW() + INTERVAL '15 minutes'
                        ELSE locked_until
                    END
                WHERE id = ?
            """, ${userId})
        `));
    }

    // Reset failed login attempts
    async resetFailedLogins(userId) {
        await this.tsk.parse(TuskLang.parse(`
            reset_logins: @query("""
                UPDATE users 
                SET failed_login_attempts = 0, locked_until = NULL
                WHERE id = ?
            """, ${userId})
        `));
    }

    // Update last login timestamp
    async updateLastLogin(userId) {
        await this.tsk.parse(TuskLang.parse(`
            update_login: @query("""
                UPDATE users 
                SET last_login = NOW()
                WHERE id = ?
            """, ${userId})
        `));
    }

    // Generate secure token ID
    generateTokenId() {
        return require('crypto').randomBytes(32).toString('hex');
    }

    // Check if token is blacklisted
    async isTokenBlacklisted(tokenId) {
        const result = await this.tsk.parse(TuskLang.parse(`
            blacklist_check: @query("""
                SELECT COUNT(*) as count
                FROM blacklisted_tokens
                WHERE token_id = ?
            """, "${tokenId}")
        `));
        
        return result.blacklist_check.count > 0;
    }
}
```

### Role-Based Access Control (RBAC)

```javascript
// security/rbac.js
const TuskLang = require('tusklang');

class RBACManager {
    constructor(tsk) {
        this.tsk = tsk;
    }

    // Check user permissions
    async checkPermission(userId, resource, action) {
        try {
            const permissions = await this.tsk.parse(TuskLang.parse(`
                user_permissions {
                    # Get user roles
                    roles: @query("""
                        SELECT r.name, r.permissions
                        FROM user_roles ur
                        JOIN roles r ON ur.role_id = r.id
                        WHERE ur.user_id = ?
                    """, ${userId})
                    
                    # Check specific permission
                    has_permission: @query("""
                        SELECT COUNT(*) as count
                        FROM user_roles ur
                        JOIN roles r ON ur.role_id = r.id
                        WHERE ur.user_id = ? 
                        AND r.permissions @> ?
                    """, ${userId}, '{"${resource}": ["${action}"]}')
                }
            `));

            return permissions.user_permissions.has_permission.count > 0;
        } catch (error) {
            console.error('Permission check error:', error);
            return false;
        }
    }

    // Get user roles and permissions
    async getUserPermissions(userId) {
        try {
            const permissions = await this.tsk.parse(TuskLang.parse(`
                user_permissions {
                    roles: @query("""
                        SELECT r.name, r.description, r.permissions
                        FROM user_roles ur
                        JOIN roles r ON ur.role_id = r.id
                        WHERE ur.user_id = ?
                    """, ${userId})
                    
                    # Get all permissions for user
                    all_permissions: @query("""
                        SELECT DISTINCT jsonb_object_keys(r.permissions) as resource,
                               jsonb_array_elements_text(r.permissions->jsonb_object_keys(r.permissions)) as action
                        FROM user_roles ur
                        JOIN roles r ON ur.role_id = r.id
                        WHERE ur.user_id = ?
                    """, ${userId})
                }
            `));

            return permissions.user_permissions;
        } catch (error) {
            console.error('Get permissions error:', error);
            return { roles: [], all_permissions: [] };
        }
    }

    // Middleware for route protection
    requirePermission(resource, action) {
        return async (req, res, next) => {
            try {
                const userId = req.user?.sub;
                if (!userId) {
                    return res.status(401).json({ error: 'Authentication required' });
                }

                const hasPermission = await this.checkPermission(userId, resource, action);
                if (!hasPermission) {
                    return res.status(403).json({ error: 'Insufficient permissions' });
                }

                next();
            } catch (error) {
                console.error('Permission middleware error:', error);
                res.status(500).json({ error: 'Permission check failed' });
            }
        };
    }
}
```

## 🛡️ Data Protection

### Input Validation & Sanitization

```javascript
// security/validation.js
const TuskLang = require('tusklang');
const Joi = require('joi');
const DOMPurify = require('isomorphic-dompurify');

class SecurityValidator {
    constructor(tsk) {
        this.tsk = tsk;
        this.validationSchemas = this.loadValidationSchemas();
    }

    async loadValidationSchemas() {
        return await this.tsk.parse(TuskLang.parse(`
            validation {
                # User registration schema
                user_registration: {
                    email: "string|email|required|max:255",
                    password: "string|min:12|required|regex:/^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]/",
                    name: "string|required|max:100|regex:/^[a-zA-Z\\s]+$/",
                    age: "number|min:13|max:120"
                }
                
                # Post creation schema
                post_creation: {
                    title: "string|required|max:200|min:3",
                    content: "string|required|max:10000|min:10",
                    tags: "array|max:10",
                    is_public: "boolean"
                }
                
                # API request schema
                api_request: {
                    method: "string|in:GET,POST,PUT,DELETE|required",
                    path: "string|required|regex:/^[a-zA-Z0-9\\/\\-_\\.]+$/",
                    headers: "object|optional",
                    body: "object|optional"
                }
                
                # File upload schema
                file_upload: {
                    filename: "string|required|max:255|regex:/^[a-zA-Z0-9\\-_\\.]+$/",
                    mimetype: "string|required|in:image/jpeg,image/png,image/gif,application/pdf",
                    size: "number|max:10485760" # 10MB
                }
            }
        `));
    }

    // Validate input data
    async validateInput(data, schemaName) {
        try {
            const schema = this.validationSchemas.validation[schemaName];
            if (!schema) {
                throw new Error(`Unknown validation schema: ${schemaName}`);
            }

            // Convert TuskLang schema to Joi schema
            const joiSchema = this.convertToJoiSchema(schema);
            
            // Validate data
            const { error, value } = joiSchema.validate(data, {
                abortEarly: false,
                stripUnknown: true
            });

            if (error) {
                return {
                    isValid: false,
                    errors: error.details.map(detail => ({
                        field: detail.path.join('.'),
                        message: detail.message,
                        type: detail.type
                    }))
                };
            }

            // Sanitize data
            const sanitizedData = await this.sanitizeData(value, schemaName);

            return {
                isValid: true,
                data: sanitizedData
            };
        } catch (error) {
            console.error('Validation error:', error);
            return {
                isValid: false,
                errors: [{ field: 'general', message: 'Validation failed', type: 'error' }]
            };
        }
    }

    // Convert TuskLang schema to Joi schema
    convertToJoiSchema(schema) {
        const joiRules = {};
        
        for (const [field, rules] of Object.entries(schema)) {
            let joiRule = Joi.string();
            
            const ruleArray = rules.split('|');
            for (const rule of ruleArray) {
                if (rule === 'required') {
                    joiRule = joiRule.required();
                } else if (rule === 'optional') {
                    joiRule = joiRule.optional();
                } else if (rule === 'email') {
                    joiRule = joiRule.email();
                } else if (rule === 'boolean') {
                    joiRule = Joi.boolean();
                } else if (rule === 'number') {
                    joiRule = Joi.number();
                } else if (rule === 'array') {
                    joiRule = Joi.array();
                } else if (rule === 'object') {
                    joiRule = Joi.object();
                } else if (rule.startsWith('min:')) {
                    const min = parseInt(rule.split(':')[1]);
                    joiRule = joiRule.min(min);
                } else if (rule.startsWith('max:')) {
                    const max = parseInt(rule.split(':')[1]);
                    joiRule = joiRule.max(max);
                } else if (rule.startsWith('regex:')) {
                    const regex = rule.split(':')[1];
                    joiRule = joiRule.pattern(new RegExp(regex));
                } else if (rule.startsWith('in:')) {
                    const values = rule.split(':')[1].split(',');
                    joiRule = joiRule.valid(...values);
                }
            }
            
            joiRules[field] = joiRule;
        }
        
        return Joi.object(joiRules);
    }

    // Sanitize data
    async sanitizeData(data, schemaName) {
        const sanitized = {};
        
        for (const [key, value] of Object.entries(data)) {
            if (typeof value === 'string') {
                // Remove HTML tags and dangerous content
                sanitized[key] = DOMPurify.sanitize(value, {
                    ALLOWED_TAGS: [],
                    ALLOWED_ATTR: []
                });
                
                // Trim whitespace
                sanitized[key] = sanitized[key].trim();
                
                // Escape special characters for SQL
                sanitized[key] = this.escapeSQL(sanitized[key]);
            } else if (typeof value === 'object' && value !== null) {
                sanitized[key] = await this.sanitizeData(value, schemaName);
            } else {
                sanitized[key] = value;
            }
        }
        
        return sanitized;
    }

    // Escape SQL special characters
    escapeSQL(str) {
        if (typeof str !== 'string') return str;
        
        return str
            .replace(/'/g, "''")
            .replace(/\\/g, '\\\\')
            .replace(/\0/g, '\\0')
            .replace(/\n/g, '\\n')
            .replace(/\r/g, '\\r')
            .replace(/\x1a/g, '\\Z');
    }

    // Validate file upload
    async validateFileUpload(file) {
        try {
            const validation = await this.validateInput({
                filename: file.originalname,
                mimetype: file.mimetype,
                size: file.size
            }, 'file_upload');

            if (!validation.isValid) {
                return validation;
            }

            // Additional security checks
            const isVirusFree = await this.scanForViruses(file);
            if (!isVirusFree) {
                return {
                    isValid: false,
                    errors: [{ field: 'file', message: 'File appears to be malicious', type: 'security' }]
                };
            }

            return { isValid: true, data: file };
        } catch (error) {
            console.error('File validation error:', error);
            return {
                isValid: false,
                errors: [{ field: 'file', message: 'File validation failed', type: 'error' }]
            };
        }
    }

    // Scan file for viruses (placeholder)
    async scanForViruses(file) {
        // In production, integrate with antivirus service
        // For now, perform basic checks
        const dangerousExtensions = ['.exe', '.bat', '.cmd', '.scr', '.pif'];
        const hasDangerousExtension = dangerousExtensions.some(ext => 
            file.originalname.toLowerCase().endsWith(ext)
        );
        
        return !hasDangerousExtension;
    }
}
```

### Data Encryption

```javascript
// security/encryption.js
const crypto = require('crypto');
const TuskLang = require('tusklang');

class EncryptionManager {
    constructor(tsk) {
        this.tsk = tsk;
        this.config = this.loadEncryptionConfig();
    }

    async loadEncryptionConfig() {
        return await this.tsk.parse(TuskLang.parse(`
            encryption {
                # AES encryption settings
                aes {
                    algorithm: "aes-256-gcm"
                    key_length: 32
                    iv_length: 16
                    tag_length: 16
                }
                
                # RSA encryption settings
                rsa {
                    key_size: 2048
                    padding: "oaep"
                    hash: "sha256"
                }
                
                # Key management
                keys {
                    master_key: @env.secure("MASTER_ENCRYPTION_KEY")
                    key_rotation_days: 90
                    backup_keys: @env.secure("BACKUP_ENCRYPTION_KEYS")
                }
                
                # Data classification
                sensitive_fields: [
                    "password",
                    "credit_card",
                    "ssn",
                    "api_key",
                    "private_key",
                    "access_token"
                ]
            }
        `));
    }

    // Encrypt sensitive data
    async encryptData(data, fieldName = null) {
        try {
            // Check if field should be encrypted
            if (fieldName && this.config.encryption.sensitive_fields.includes(fieldName)) {
                return await this.encryptAES(data);
            }
            
            return data;
        } catch (error) {
            console.error('Encryption error:', error);
            throw new Error('Encryption failed');
        }
    }

    // Decrypt sensitive data
    async decryptData(encryptedData, fieldName = null) {
        try {
            // Check if field should be decrypted
            if (fieldName && this.config.encryption.sensitive_fields.includes(fieldName)) {
                return await this.decryptAES(encryptedData);
            }
            
            return encryptedData;
        } catch (error) {
            console.error('Decryption error:', error);
            throw new Error('Decryption failed');
        }
    }

    // AES encryption
    async encryptAES(data) {
        const algorithm = this.config.encryption.aes.algorithm;
        const key = Buffer.from(this.config.encryption.keys.master_key, 'hex');
        const iv = crypto.randomBytes(this.config.encryption.aes.iv_length);
        
        const cipher = crypto.createCipher(algorithm, key);
        cipher.setAAD(Buffer.from('additional-data', 'utf8'));
        
        let encrypted = cipher.update(data, 'utf8', 'hex');
        encrypted += cipher.final('hex');
        
        const tag = cipher.getAuthTag();
        
        return {
            encrypted: encrypted,
            iv: iv.toString('hex'),
            tag: tag.toString('hex'),
            algorithm: algorithm
        };
    }

    // AES decryption
    async decryptAES(encryptedData) {
        const algorithm = encryptedData.algorithm;
        const key = Buffer.from(this.config.encryption.keys.master_key, 'hex');
        const iv = Buffer.from(encryptedData.iv, 'hex');
        const tag = Buffer.from(encryptedData.tag, 'hex');
        
        const decipher = crypto.createDecipher(algorithm, key);
        decipher.setAAD(Buffer.from('additional-data', 'utf8'));
        decipher.setAuthTag(tag);
        
        let decrypted = decipher.update(encryptedData.encrypted, 'hex', 'utf8');
        decrypted += decipher.final('utf8');
        
        return decrypted;
    }

    // Hash sensitive data
    async hashData(data, salt = null) {
        try {
            if (!salt) {
                salt = crypto.randomBytes(16).toString('hex');
            }
            
            const hash = crypto.pbkdf2Sync(
                data,
                salt,
                10000, // iterations
                64,    // key length
                'sha512'
            ).toString('hex');
            
            return {
                hash: hash,
                salt: salt
            };
        } catch (error) {
            console.error('Hashing error:', error);
            throw new Error('Hashing failed');
        }
    }

    // Verify hash
    async verifyHash(data, hash, salt) {
        try {
            const computedHash = crypto.pbkdf2Sync(
                data,
                salt,
                10000, // iterations
                64,    // key length
                'sha512'
            ).toString('hex');
            
            return crypto.timingSafeEqual(
                Buffer.from(hash, 'hex'),
                Buffer.from(computedHash, 'hex')
            );
        } catch (error) {
            console.error('Hash verification error:', error);
            return false;
        }
    }
}
```

## 🚨 Security Monitoring

### Security Event Logging

```javascript
// security/monitoring.js
const TuskLang = require('tusklang');

class SecurityMonitor {
    constructor(tsk) {
        this.tsk = tsk;
        this.config = this.loadSecurityConfig();
    }

    async loadSecurityConfig() {
        return await this.tsk.parse(TuskLang.parse(`
            security_monitoring {
                # Event logging
                logging {
                    enabled: true
                    level: "info"
                    format: "json"
                    
                    # Security events to log
                    events: [
                        "login_success",
                        "login_failure",
                        "logout",
                        "permission_denied",
                        "data_access",
                        "data_modification",
                        "file_upload",
                        "api_access",
                        "suspicious_activity"
                    ]
                }
                
                # Alerting
                alerts {
                    enabled: true
                    
                    # Suspicious activity thresholds
                    failed_login_threshold: 5
                    failed_login_window: "15m"
                    
                    # Data access monitoring
                    unusual_data_access: true
                    data_access_threshold: 100
                    data_access_window: "1h"
                    
                    # API abuse detection
                    api_rate_limit_exceeded: true
                    api_abuse_threshold: 1000
                    api_abuse_window: "1h"
                }
                
                # IP blocking
                ip_blocking {
                    enabled: true
                    block_duration: "1h"
                    max_failed_attempts: 10
                    whitelist: @env("IP_WHITELIST", "").split(",")
                }
            }
        `));
    }

    // Log security event
    async logSecurityEvent(eventType, details) {
        try {
            await this.tsk.parse(TuskLang.parse(`
                security_log: @query("""
                    INSERT INTO security_events (
                        event_type, user_id, ip_address, user_agent, 
                        details, severity, created_at
                    ) VALUES (?, ?, ?, ?, ?, ?, NOW())
                """, "${eventType}", ${details.userId || null}, "${details.ipAddress}", 
                     "${details.userAgent}", '${JSON.stringify(details)}', "${details.severity || 'info'}")
            `));

            // Check for suspicious activity
            await this.checkSuspiciousActivity(eventType, details);
        } catch (error) {
            console.error('Security logging error:', error);
        }
    }

    // Check for suspicious activity
    async checkSuspiciousActivity(eventType, details) {
        try {
            if (eventType === 'login_failure') {
                await this.checkFailedLoginAttempts(details);
            } else if (eventType === 'data_access') {
                await this.checkUnusualDataAccess(details);
            } else if (eventType === 'api_access') {
                await this.checkAPIAbuse(details);
            }
        } catch (error) {
            console.error('Suspicious activity check error:', error);
        }
    }

    // Check failed login attempts
    async checkFailedLoginAttempts(details) {
        const recentFailures = await this.tsk.parse(TuskLang.parse(`
            recent_failures: @query("""
                SELECT COUNT(*) as count
                FROM security_events
                WHERE event_type = 'login_failure'
                AND ip_address = ?
                AND created_at > NOW() - INTERVAL '${this.config.security_monitoring.alerts.failed_login_window}'
            """, "${details.ipAddress}")
        `));

        if (recentFailures.recent_failures.count >= this.config.security_monitoring.alerts.failed_login_threshold) {
            await this.triggerAlert('multiple_failed_logins', {
                ipAddress: details.ipAddress,
                failureCount: recentFailures.recent_failures.count,
                threshold: this.config.security_monitoring.alerts.failed_login_threshold
            });

            // Block IP if enabled
            if (this.config.security_monitoring.ip_blocking.enabled) {
                await this.blockIP(details.ipAddress);
            }
        }
    }

    // Check unusual data access
    async checkUnusualDataAccess(details) {
        const recentAccess = await this.tsk.parse(TuskLang.parse(`
            recent_access: @query("""
                SELECT COUNT(*) as count
                FROM security_events
                WHERE event_type = 'data_access'
                AND user_id = ?
                AND created_at > NOW() - INTERVAL '${this.config.security_monitoring.alerts.data_access_window}'
            """, ${details.userId})
        `));

        if (recentAccess.recent_access.count >= this.config.security_monitoring.alerts.data_access_threshold) {
            await this.triggerAlert('unusual_data_access', {
                userId: details.userId,
                accessCount: recentAccess.recent_access.count,
                threshold: this.config.security_monitoring.alerts.data_access_threshold
            });
        }
    }

    // Check API abuse
    async checkAPIAbuse(details) {
        const recentAPI = await this.tsk.parse(TuskLang.parse(`
            recent_api: @query("""
                SELECT COUNT(*) as count
                FROM security_events
                WHERE event_type = 'api_access'
                AND ip_address = ?
                AND created_at > NOW() - INTERVAL '${this.config.security_monitoring.alerts.api_abuse_window}'
            """, "${details.ipAddress}")
        `));

        if (recentAPI.recent_api.count >= this.config.security_monitoring.alerts.api_abuse_threshold) {
            await this.triggerAlert('api_abuse', {
                ipAddress: details.ipAddress,
                apiCount: recentAPI.recent_api.count,
                threshold: this.config.security_monitoring.alerts.api_abuse_threshold
            });
        }
    }

    // Trigger security alert
    async triggerAlert(alertType, details) {
        try {
            await this.tsk.parse(TuskLang.parse(`
                security_alert: @query("""
                    INSERT INTO security_alerts (
                        alert_type, details, severity, status, created_at
                    ) VALUES (?, ?, 'high', 'active', NOW())
                """, "${alertType}", '${JSON.stringify(details)}')
            `));

            // Send notification
            await this.sendSecurityNotification(alertType, details);
        } catch (error) {
            console.error('Alert trigger error:', error);
        }
    }

    // Block IP address
    async blockIP(ipAddress) {
        try {
            await this.tsk.parse(TuskLang.parse(`
                block_ip: @query("""
                    INSERT INTO blocked_ips (ip_address, reason, blocked_until, created_at)
                    VALUES (?, 'Multiple failed login attempts', 
                           NOW() + INTERVAL '${this.config.security_monitoring.ip_blocking.block_duration}', NOW())
                    ON CONFLICT (ip_address) DO UPDATE SET
                        blocked_until = EXCLUDED.blocked_until,
                        updated_at = NOW()
                """, "${ipAddress}")
            `));

            console.log(`IP ${ipAddress} blocked for security reasons`);
        } catch (error) {
            console.error('IP blocking error:', error);
        }
    }

    // Check if IP is blocked
    async isIPBlocked(ipAddress) {
        try {
            const result = await this.tsk.parse(TuskLang.parse(`
                ip_block_check: @query("""
                    SELECT COUNT(*) as count
                    FROM blocked_ips
                    WHERE ip_address = ?
                    AND blocked_until > NOW()
                """, "${ipAddress}")
            `));

            return result.ip_block_check.count > 0;
        } catch (error) {
            console.error('IP block check error:', error);
            return false;
        }
    }

    // Send security notification
    async sendSecurityNotification(alertType, details) {
        // In production, integrate with notification service
        console.log(`SECURITY ALERT: ${alertType}`, details);
        
        // Example: Send email, Slack message, etc.
        // await this.sendEmail('security@myapp.com', `Security Alert: ${alertType}`, details);
        // await this.sendSlackMessage('#security', `Security Alert: ${alertType}`, details);
    }
}
```

## 📚 Next Steps

1. **[Testing Strategies](010-testing-strategies-javascript.md)** - Test your security measures
2. **[Scaling Applications](011-scaling-applications-javascript.md)** - Scale securely
3. **[Debugging Tools](012-debugging-tools-javascript.md)** - Debug security issues
4. **[Compliance Standards](013-compliance-standards-javascript.md)** - Meet compliance requirements

## 🎉 Security Best Practices Complete!

You now understand how to secure TuskLang applications with:
- ✅ **Authentication** - JWT-based authentication with MFA
- ✅ **Authorization** - Role-based access control
- ✅ **Data Protection** - Input validation and encryption
- ✅ **Security Monitoring** - Real-time security event logging
- ✅ **Threat Prevention** - IP blocking and abuse detection

**Ready to build secure, production-ready TuskLang applications!** 
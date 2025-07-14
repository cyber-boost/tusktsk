# TuskLang JavaScript Documentation: Advanced Security

## Overview

Advanced security in TuskLang provides comprehensive security features including encryption, authentication, authorization, and threat protection with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#security advanced
  encryption:
    algorithm: AES-256-GCM
    key_rotation: 30d
    key_storage: vault
    
  authentication:
    methods:
      - jwt
      - oauth2
      - api_key
      - session
    jwt:
      secret: @env.secure("JWT_SECRET")
      expires_in: 1h
      refresh_token: true
    oauth2:
      providers:
        - google
        - github
        - facebook
    
  authorization:
    rbac: true
    permissions:
      - read:users
      - write:users
      - admin:system
    roles:
      admin: ["*"]
      user: ["read:users", "write:own"]
      guest: ["read:public"]
    
  threat_protection:
    rate_limiting:
      window: 15m
      max_requests: 100
    sql_injection: true
    xss_protection: true
    csrf_protection: true
    input_validation: true
```

## JavaScript Integration

### Advanced Security Manager

```javascript
// advanced-security-manager.js
const crypto = require('crypto');
const jwt = require('jsonwebtoken');
const bcrypt = require('bcrypt');
const rateLimit = require('express-rate-limit');

class AdvancedSecurityManager {
  constructor(config) {
    this.config = config;
    this.encryptionManager = new EncryptionManager(config.encryption);
    this.authManager = new AuthenticationManager(config.authentication);
    this.authzManager = new AuthorizationManager(config.authorization);
    this.threatProtection = new ThreatProtection(config.threat_protection);
  }

  async initialize() {
    await this.encryptionManager.initialize();
    await this.authManager.initialize();
    await this.authzManager.initialize();
    await this.threatProtection.initialize();
  }

  async encrypt(data) {
    return await this.encryptionManager.encrypt(data);
  }

  async decrypt(encryptedData) {
    return await this.encryptionManager.decrypt(encryptedData);
  }

  async authenticate(credentials) {
    return await this.authManager.authenticate(credentials);
  }

  async authorize(user, resource, action) {
    return await this.authzManager.authorize(user, resource, action);
  }

  async validateInput(input, schema) {
    return await this.threatProtection.validateInput(input, schema);
  }
}

module.exports = AdvancedSecurityManager;
```

### Encryption Manager

```javascript
// encryption-manager.js
const crypto = require('crypto');

class EncryptionManager {
  constructor(config) {
    this.config = config;
    this.algorithm = config.algorithm || 'aes-256-gcm';
    this.keyRotationDays = this.parseTime(config.key_rotation || '30d');
    this.keys = new Map();
  }

  parseTime(timeStr) {
    const match = timeStr.match(/^(\d+)([dmh])$/);
    if (!match) return 30;
    const [, value, unit] = match;
    const multipliers = { d: 1, m: 30, h: 1/24 };
    return parseInt(value) * multipliers[unit];
  }

  async generateKey() {
    return crypto.randomBytes(32);
  }

  async encrypt(data) {
    const key = await this.generateKey();
    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipher(this.algorithm, key);
    
    let encrypted = cipher.update(JSON.stringify(data), 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    return {
      encrypted,
      key: key.toString('hex'),
      iv: iv.toString('hex'),
      algorithm: this.algorithm
    };
  }

  async decrypt(encryptedData) {
    const { encrypted, key, iv, algorithm } = encryptedData;
    const decipher = crypto.createDecipher(algorithm, Buffer.from(key, 'hex'));
    
    let decrypted = decipher.update(encrypted, 'hex', 'utf8');
    decrypted += decipher.final('utf8');
    
    return JSON.parse(decrypted);
  }
}

module.exports = EncryptionManager;
```

### Authentication Manager

```javascript
// authentication-manager.js
const jwt = require('jsonwebtoken');
const bcrypt = require('bcrypt');
const passport = require('passport');

class AuthenticationManager {
  constructor(config) {
    this.config = config;
    this.methods = config.methods || ['jwt'];
    this.jwtConfig = config.jwt || {};
    this.oauthConfig = config.oauth2 || {};
  }

  async authenticate(credentials) {
    for (const method of this.methods) {
      try {
        switch (method) {
          case 'jwt':
            return await this.authenticateJWT(credentials);
          case 'oauth2':
            return await this.authenticateOAuth2(credentials);
          case 'api_key':
            return await this.authenticateAPIKey(credentials);
          case 'session':
            return await this.authenticateSession(credentials);
        }
      } catch (error) {
        console.error(`Authentication method ${method} failed:`, error);
      }
    }
    
    throw new Error('All authentication methods failed');
  }

  async authenticateJWT(credentials) {
    const { token } = credentials;
    const decoded = jwt.verify(token, this.jwtConfig.secret);
    return { user: decoded, method: 'jwt' };
  }

  async authenticateOAuth2(credentials) {
    const { provider, code } = credentials;
    // OAuth2 implementation
    return { user: { id: 1, provider }, method: 'oauth2' };
  }

  async authenticateAPIKey(credentials) {
    const { apiKey } = credentials;
    // API key validation
    return { user: { id: 1, type: 'api' }, method: 'api_key' };
  }

  async authenticateSession(credentials) {
    const { sessionId } = credentials;
    // Session validation
    return { user: { id: 1, type: 'session' }, method: 'session' };
  }

  async generateToken(user) {
    return jwt.sign(user, this.jwtConfig.secret, {
      expiresIn: this.jwtConfig.expires_in || '1h'
    });
  }
}

module.exports = AuthenticationManager;
```

### Authorization Manager

```javascript
// authorization-manager.js
class AuthorizationManager {
  constructor(config) {
    this.config = config;
    this.rbac = config.rbac || false;
    this.permissions = config.permissions || [];
    this.roles = config.roles || {};
  }

  async authorize(user, resource, action) {
    if (!this.rbac) {
      return true; // No RBAC enabled
    }

    const userRole = user.role || 'guest';
    const rolePermissions = this.roles[userRole] || [];
    
    const requiredPermission = `${action}:${resource}`;
    
    return rolePermissions.includes(requiredPermission) || 
           rolePermissions.includes('*') ||
           rolePermissions.includes(`${action}:*`);
  }

  async checkPermission(user, permission) {
    const userRole = user.role || 'guest';
    const rolePermissions = this.roles[userRole] || [];
    
    return rolePermissions.includes(permission) || 
           rolePermissions.includes('*');
  }

  async assignRole(user, role) {
    if (!this.roles[role]) {
      throw new Error(`Role '${role}' not found`);
    }
    
    user.role = role;
    return user;
  }
}

module.exports = AuthorizationManager;
```

### Threat Protection

```javascript
// threat-protection.js
const rateLimit = require('express-rate-limit');
const helmet = require('helmet');
const xss = require('xss-clean');

class ThreatProtection {
  constructor(config) {
    this.config = config;
    this.rateLimiting = config.rate_limiting || {};
    this.protections = {
      sqlInjection: config.sql_injection !== false,
      xssProtection: config.xss_protection !== false,
      csrfProtection: config.csrf_protection !== false,
      inputValidation: config.input_validation !== false
    };
  }

  createRateLimiter() {
    return rateLimit({
      windowMs: this.parseTime(this.rateLimiting.window || '15m') * 60 * 1000,
      max: this.rateLimiting.max_requests || 100,
      message: 'Too many requests from this IP'
    });
  }

  parseTime(timeStr) {
    const match = timeStr.match(/^(\d+)([mh])$/);
    if (!match) return 15;
    const [, value, unit] = match;
    return parseInt(value) * (unit === 'h' ? 60 : 1);
  }

  async validateInput(input, schema) {
    if (!this.protections.inputValidation) {
      return input;
    }

    // Basic input validation
    for (const [key, value] of Object.entries(input)) {
      if (typeof value === 'string') {
        input[key] = this.sanitizeString(value);
      }
    }

    return input;
  }

  sanitizeString(str) {
    if (this.protections.xssProtection) {
      str = str.replace(/[<>]/g, '');
    }
    
    if (this.protections.sqlInjection) {
      str = str.replace(/['";\\]/g, '');
    }
    
    return str;
  }

  createSecurityMiddleware() {
    const middleware = [];
    
    if (this.protections.xssProtection) {
      middleware.push(xss());
    }
    
    middleware.push(helmet());
    middleware.push(this.createRateLimiter());
    
    return middleware;
  }
}

module.exports = ThreatProtection;
```

## TypeScript Implementation

```typescript
// advanced-security.types.ts
export interface SecurityConfig {
  encryption?: EncryptionConfig;
  authentication?: AuthenticationConfig;
  authorization?: AuthorizationConfig;
  threat_protection?: ThreatProtectionConfig;
}

export interface EncryptionConfig {
  algorithm?: string;
  key_rotation?: string;
  key_storage?: string;
}

export interface AuthenticationConfig {
  methods?: string[];
  jwt?: JWTConfig;
  oauth2?: OAuth2Config;
}

export interface JWTConfig {
  secret: string;
  expires_in?: string;
  refresh_token?: boolean;
}

export interface OAuth2Config {
  providers?: string[];
}

export interface AuthorizationConfig {
  rbac?: boolean;
  permissions?: string[];
  roles?: Record<string, string[]>;
}

export interface ThreatProtectionConfig {
  rate_limiting?: RateLimitConfig;
  sql_injection?: boolean;
  xss_protection?: boolean;
  csrf_protection?: boolean;
  input_validation?: boolean;
}

export interface RateLimitConfig {
  window?: string;
  max_requests?: number;
}

export interface SecurityManager {
  encrypt(data: any): Promise<any>;
  decrypt(encryptedData: any): Promise<any>;
  authenticate(credentials: any): Promise<any>;
  authorize(user: any, resource: string, action: string): Promise<boolean>;
  validateInput(input: any, schema: any): Promise<any>;
}

// advanced-security.ts
import { SecurityConfig, SecurityManager } from './advanced-security.types';

export class TypeScriptAdvancedSecurityManager implements SecurityManager {
  private config: SecurityConfig;

  constructor(config: SecurityConfig) {
    this.config = config;
  }

  async encrypt(data: any): Promise<any> {
    // Encryption implementation
    return { encrypted: 'data' };
  }

  async decrypt(encryptedData: any): Promise<any> {
    // Decryption implementation
    return { decrypted: 'data' };
  }

  async authenticate(credentials: any): Promise<any> {
    // Authentication implementation
    return { user: { id: 1 }, method: 'jwt' };
  }

  async authorize(user: any, resource: string, action: string): Promise<boolean> {
    // Authorization implementation
    return true;
  }

  async validateInput(input: any, schema: any): Promise<any> {
    // Input validation implementation
    return input;
  }
}
```

## Advanced Usage Scenarios

### Multi-Factor Authentication

```javascript
// mfa-manager.js
class MFAManager {
  constructor() {
    this.methods = ['totp', 'sms', 'email'];
  }

  async setupMFA(userId, method) {
    switch (method) {
      case 'totp':
        return await this.setupTOTP(userId);
      case 'sms':
        return await this.setupSMS(userId);
      case 'email':
        return await this.setupEmail(userId);
    }
  }

  async verifyMFA(userId, method, code) {
    switch (method) {
      case 'totp':
        return await this.verifyTOTP(userId, code);
      case 'sms':
        return await this.verifySMS(userId, code);
      case 'email':
        return await this.verifyEmail(userId, code);
    }
  }
}
```

### Role-Based Access Control

```javascript
// rbac-manager.js
class RBACManager {
  constructor() {
    this.roles = new Map();
    this.permissions = new Set();
  }

  addRole(role, permissions) {
    this.roles.set(role, permissions);
  }

  addPermission(permission) {
    this.permissions.add(permission);
  }

  async checkAccess(user, resource, action) {
    const userRole = user.role;
    const rolePermissions = this.roles.get(userRole) || [];
    
    const requiredPermission = `${action}:${resource}`;
    return rolePermissions.includes(requiredPermission);
  }
}
```

## Real-World Examples

### Express.js Security Middleware

```javascript
// express-security.js
const express = require('express');
const AdvancedSecurityManager = require('./advanced-security-manager');

class ExpressSecurity {
  constructor(app, config) {
    this.app = app;
    this.security = new AdvancedSecurityManager(config);
  }

  setupSecurityMiddleware() {
    const middleware = this.security.threatProtection.createSecurityMiddleware();
    
    middleware.forEach(m => this.app.use(m));
    
    this.app.use(async (req, res, next) => {
      try {
        // Validate input
        req.body = await this.security.validateInput(req.body);
        next();
      } catch (error) {
        res.status(400).json({ error: 'Invalid input' });
      }
    });
  }

  setupAuthMiddleware() {
    this.app.use(async (req, res, next) => {
      try {
        const token = req.headers.authorization?.replace('Bearer ', '');
        if (token) {
          const auth = await this.security.authenticate({ token });
          req.user = auth.user;
        }
        next();
      } catch (error) {
        res.status(401).json({ error: 'Unauthorized' });
      }
    });
  }
}
```

### API Security

```javascript
// api-security.js
class APISecurity {
  constructor(securityManager) {
    this.security = securityManager;
  }

  async secureEndpoint(req, res, next) {
    try {
      // Authenticate
      const auth = await this.security.authenticate(req.headers);
      req.user = auth.user;
      
      // Authorize
      const authorized = await this.security.authorize(
        req.user, 
        req.path, 
        req.method
      );
      
      if (!authorized) {
        return res.status(403).json({ error: 'Forbidden' });
      }
      
      next();
    } catch (error) {
      res.status(401).json({ error: 'Unauthorized' });
    }
  }
}
```

## Performance Considerations

### Security Performance Monitoring

```javascript
// security-performance-monitor.js
class SecurityPerformanceMonitor {
  constructor() {
    this.metrics = {
      authAttempts: 0,
      authFailures: 0,
      encryptionOperations: 0,
      decryptionOperations: 0
    };
  }

  recordAuthAttempt(success) {
    this.metrics.authAttempts++;
    if (!success) {
      this.metrics.authFailures++;
    }
  }

  recordEncryptionOperation() {
    this.metrics.encryptionOperations++;
  }

  recordDecryptionOperation() {
    this.metrics.decryptionOperations++;
  }

  getMetrics() {
    return {
      ...this.metrics,
      authSuccessRate: this.metrics.authAttempts > 0 
        ? ((this.metrics.authAttempts - this.metrics.authFailures) / this.metrics.authAttempts * 100).toFixed(2) + '%'
        : '0%'
    };
  }
}
```

## Security Notes

### Security Best Practices

```javascript
// security-best-practices.js
class SecurityBestPractices {
  constructor() {
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }

  validateSecurityConfig(config) {
    const issues = [];
    
    if (config.jwt?.secret === 'default_secret') {
      issues.push('JWT secret should not be default');
    }
    
    if (!config.threat_protection?.rate_limiting) {
      issues.push('Rate limiting should be enabled');
    }
    
    return issues;
  }

  sanitizeLogs(logs) {
    return logs.map(log => {
      const sanitized = { ...log };
      
      for (const pattern of this.sensitivePatterns) {
        if (sanitized.data && pattern.test(JSON.stringify(sanitized.data))) {
          sanitized.data = '[REDACTED]';
        }
      }
      
      return sanitized;
    });
  }
}
```

## Best Practices

### Security Configuration Management

```javascript
// security-config-manager.js
class SecurityConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No security configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.authentication?.methods?.length) {
      throw new Error('At least one authentication method is required');
    }
    
    return config;
  }
}
```

### Security Health Monitoring

```javascript
// security-health-monitor.js
class SecurityHealthMonitor {
  constructor(securityManager) {
    this.security = securityManager;
    this.metrics = {
      securityChecks: 0,
      violations: 0,
      avgResponseTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      
      // Test encryption
      const testData = { test: 'data' };
      const encrypted = await this.security.encrypt(testData);
      const decrypted = await this.security.decrypt(encrypted);
      
      const responseTime = Date.now() - start;
      
      this.metrics.securityChecks++;
      this.metrics.avgResponseTime = 
        (this.metrics.avgResponseTime * (this.metrics.securityChecks - 1) + responseTime) / this.metrics.securityChecks;
      
      return {
        status: 'healthy',
        responseTime,
        metrics: this.metrics
      };
    } catch (error) {
      this.metrics.violations++;
      return {
        status: 'unhealthy',
        error: error.message,
        metrics: this.metrics
      };
    }
  }
}
```

## Related Topics

- [@encrypt Operator](./28-tsklang-javascript-operator-encrypt.md)
- [@auth Operator](./29-tsklang-javascript-operator-auth.md)
- [@validate Operator](./30-tsklang-javascript-operator-validate.md)
- [@security Directive](./86-tsklang-javascript-directives-security.md) 
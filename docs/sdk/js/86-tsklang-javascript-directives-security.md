# TuskLang JavaScript Documentation: #security Directive

## Overview

The `#security` directive in TuskLang defines security configurations and policies, enabling declarative security management with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#security cors
  origin: ["https://app.com", "https://api.app.com"]
  methods: ["GET", "POST", "PUT", "DELETE"]
  headers: ["Content-Type", "Authorization"]
  credentials: true
  max_age: 86400

#security rate_limit
  window: 15m
  max_requests: 100
  strategy: sliding_window
  storage: redis://localhost:6379
  headers: true

#security helmet
  content_security_policy: true
  xss_protection: true
  no_sniff: true
  frameguard: deny
  hsts: true
  hsts_max_age: 31536000

#security jwt
  secret: ${JWT_SECRET}
  algorithm: HS256
  expires_in: 24h
  refresh_token: true
  blacklist: redis://localhost:6379

#security encryption
  algorithm: aes-256-gcm
  key: ${ENCRYPTION_KEY}
  iv_length: 16
  salt_rounds: 12
```

## JavaScript Integration

### CORS Security Handler

```javascript
// cors-security-handler.js
class CORSSecurityHandler {
  constructor(config) {
    this.config = config;
    this.origins = config.origin || ['*'];
    this.methods = config.methods || ['GET', 'POST', 'PUT', 'DELETE'];
    this.headers = config.headers || ['Content-Type', 'Authorization'];
    this.credentials = config.credentials || false;
    this.maxAge = config.max_age || 86400;
  }

  isOriginAllowed(origin) {
    if (this.origins.includes('*')) {
      return true;
    }
    
    return this.origins.includes(origin);
  }

  getCORSHeaders(origin) {
    const headers = {
      'Access-Control-Allow-Origin': this.isOriginAllowed(origin) ? origin : this.origins[0],
      'Access-Control-Allow-Methods': this.methods.join(', '),
      'Access-Control-Allow-Headers': this.headers.join(', '),
      'Access-Control-Max-Age': this.maxAge
    };

    if (this.credentials) {
      headers['Access-Control-Allow-Credentials'] = 'true';
    }

    return headers;
  }

  handlePreflight(req, res) {
    const origin = req.headers.origin;
    
    if (!this.isOriginAllowed(origin)) {
      res.status(403).json({ error: 'Origin not allowed' });
      return;
    }

    const headers = this.getCORSHeaders(origin);
    
    Object.entries(headers).forEach(([key, value]) => {
      res.set(key, value);
    });

    res.status(200).end();
  }

  handleRequest(req, res, next) {
    const origin = req.headers.origin;
    
    if (origin && this.isOriginAllowed(origin)) {
      const headers = this.getCORSHeaders(origin);
      
      Object.entries(headers).forEach(([key, value]) => {
        res.set(key, value);
      });
    }

    next();
  }

  createMiddleware() {
    return (req, res, next) => {
      if (req.method === 'OPTIONS') {
        this.handlePreflight(req, res);
      } else {
        this.handleRequest(req, res, next);
      }
    };
  }
}

module.exports = CORSSecurityHandler;
```

### Rate Limiting Security Handler

```javascript
// rate-limit-security-handler.js
const Redis = require('ioredis');

class RateLimitSecurityHandler {
  constructor(config) {
    this.config = config;
    this.window = this.parseTimeWindow(config.window || '15m');
    this.maxRequests = config.max_requests || 100;
    this.strategy = config.strategy || 'sliding_window';
    this.storage = config.storage ? new Redis(config.storage) : null;
    this.headers = config.headers !== false;
  }

  parseTimeWindow(windowStr) {
    const match = windowStr.match(/^(\d+)([smhd])$/);
    if (!match) return 15 * 60 * 1000; // Default 15 minutes

    const [, value, unit] = match;
    const multipliers = {
      s: 1000,
      m: 60 * 1000,
      h: 60 * 60 * 1000,
      d: 24 * 60 * 60 * 1000
    };

    return parseInt(value) * (multipliers[unit] || 60000);
  }

  getIdentifier(req) {
    // Use IP address as default identifier
    return req.ip || req.connection.remoteAddress || 'unknown';
  }

  async checkRateLimit(req) {
    const identifier = this.getIdentifier(req);
    const key = `rate_limit:${identifier}`;
    const now = Date.now();

    if (this.strategy === 'sliding_window') {
      return await this.slidingWindowStrategy(key, now);
    } else {
      return await this.fixedWindowStrategy(key, now);
    }
  }

  async slidingWindowStrategy(key, now) {
    if (!this.storage) {
      // Fallback to in-memory storage
      return this.slidingWindowMemory(key, now);
    }

    const pipeline = this.storage.pipeline();
    
    // Remove expired entries
    pipeline.zremrangebyscore(key, 0, now - this.window);
    
    // Count current requests
    pipeline.zcard(key);
    
    // Add current request
    pipeline.zadd(key, now, now.toString());
    
    // Set expiry
    pipeline.expire(key, Math.ceil(this.window / 1000));

    const results = await pipeline.exec();
    const currentCount = results[1][1];

    if (currentCount >= this.maxRequests) {
      return {
        allowed: false,
        remaining: 0,
        reset: now + this.window
      };
    }

    return {
      allowed: true,
      remaining: this.maxRequests - currentCount - 1,
      reset: now + this.window
    };
  }

  async fixedWindowStrategy(key, now) {
    const windowStart = Math.floor(now / this.window) * this.window;
    const windowKey = `${key}:${windowStart}`;

    if (!this.storage) {
      // Fallback to in-memory storage
      return this.fixedWindowMemory(windowKey);
    }

    const current = await this.storage.incr(windowKey);
    
    if (current === 1) {
      await this.storage.expire(windowKey, Math.ceil(this.window / 1000));
    }

    if (current > this.maxRequests) {
      return {
        allowed: false,
        remaining: 0,
        reset: windowStart + this.window
      };
    }

    return {
      allowed: true,
      remaining: this.maxRequests - current,
      reset: windowStart + this.window
    };
  }

  slidingWindowMemory(key, now) {
    if (!this.memoryStorage) {
      this.memoryStorage = new Map();
    }

    const requests = this.memoryStorage.get(key) || [];
    const validRequests = requests.filter(timestamp => timestamp > now - this.window);
    
    if (validRequests.length >= this.maxRequests) {
      return {
        allowed: false,
        remaining: 0,
        reset: now + this.window
      };
    }

    validRequests.push(now);
    this.memoryStorage.set(key, validRequests);

    return {
      allowed: true,
      remaining: this.maxRequests - validRequests.length,
      reset: now + this.window
    };
  }

  fixedWindowMemory(key) {
    if (!this.memoryStorage) {
      this.memoryStorage = new Map();
    }

    const current = (this.memoryStorage.get(key) || 0) + 1;
    this.memoryStorage.set(key, current);

    if (current > this.maxRequests) {
      return {
        allowed: false,
        remaining: 0,
        reset: Date.now() + this.window
      };
    }

    return {
      allowed: true,
      remaining: this.maxRequests - current,
      reset: Date.now() + this.window
    };
  }

  createMiddleware() {
    return async (req, res, next) => {
      try {
        const result = await this.checkRateLimit(req);
        
        if (this.headers) {
          res.set('X-RateLimit-Limit', this.maxRequests);
          res.set('X-RateLimit-Remaining', result.remaining);
          res.set('X-RateLimit-Reset', new Date(result.reset).toISOString());
        }

        if (!result.allowed) {
          res.status(429).json({
            error: 'Rate limit exceeded',
            retryAfter: Math.ceil((result.reset - Date.now()) / 1000)
          });
          return;
        }

        next();
      } catch (error) {
        console.error('Rate limiting error:', error);
        next();
      }
    };
  }
}

module.exports = RateLimitSecurityHandler;
```

### Helmet Security Handler

```javascript
// helmet-security-handler.js
class HelmetSecurityHandler {
  constructor(config) {
    this.config = config;
  }

  createMiddleware() {
    const middlewares = [];

    // Content Security Policy
    if (this.config.content_security_policy) {
      middlewares.push(this.contentSecurityPolicy());
    }

    // XSS Protection
    if (this.config.xss_protection) {
      middlewares.push(this.xssProtection());
    }

    // No Sniff
    if (this.config.no_sniff) {
      middlewares.push(this.noSniff());
    }

    // Frame Guard
    if (this.config.frameguard) {
      middlewares.push(this.frameGuard());
    }

    // HSTS
    if (this.config.hsts) {
      middlewares.push(this.hsts());
    }

    return middlewares;
  }

  contentSecurityPolicy() {
    const csp = {
      'default-src': ["'self'"],
      'script-src': ["'self'", "'unsafe-inline'"],
      'style-src': ["'self'", "'unsafe-inline'"],
      'img-src': ["'self'", 'data:', 'https:'],
      'connect-src': ["'self'"],
      'font-src': ["'self'"],
      'object-src': ["'none'"],
      'media-src': ["'self'"],
      'frame-src': ["'none'"]
    };

    const cspString = Object.entries(csp)
      .map(([key, values]) => `${key} ${values.join(' ')}`)
      .join('; ');

    return (req, res, next) => {
      res.set('Content-Security-Policy', cspString);
      next();
    };
  }

  xssProtection() {
    return (req, res, next) => {
      res.set('X-XSS-Protection', '1; mode=block');
      next();
    };
  }

  noSniff() {
    return (req, res, next) => {
      res.set('X-Content-Type-Options', 'nosniff');
      next();
    };
  }

  frameGuard() {
    const action = this.config.frameguard === 'deny' ? 'DENY' : 'SAMEORIGIN';
    
    return (req, res, next) => {
      res.set('X-Frame-Options', action);
      next();
    };
  }

  hsts() {
    const maxAge = this.config.hsts_max_age || 31536000;
    const includeSubDomains = this.config.hsts_include_subdomains !== false;
    
    let hstsValue = `max-age=${maxAge}`;
    if (includeSubDomains) {
      hstsValue += '; includeSubDomains';
    }
    if (this.config.hsts_preload) {
      hstsValue += '; preload';
    }

    return (req, res, next) => {
      res.set('Strict-Transport-Security', hstsValue);
      next();
    };
  }
}

module.exports = HelmetSecurityHandler;
```

### JWT Security Handler

```javascript
// jwt-security-handler.js
const jwt = require('jsonwebtoken');
const Redis = require('ioredis');

class JWTSecurityHandler {
  constructor(config) {
    this.config = config;
    this.secret = config.secret;
    this.algorithm = config.algorithm || 'HS256';
    this.expiresIn = config.expires_in || '24h';
    this.refreshToken = config.refresh_token || false;
    this.blacklist = config.blacklist ? new Redis(config.blacklist) : null;
  }

  generateToken(payload) {
    const token = jwt.sign(payload, this.secret, {
      algorithm: this.algorithm,
      expiresIn: this.expiresIn
    });

    if (this.refreshToken) {
      const refreshToken = jwt.sign(payload, this.secret, {
        algorithm: this.algorithm,
        expiresIn: '7d'
      });
      return { token, refreshToken };
    }

    return { token };
  }

  async verifyToken(token) {
    try {
      // Check blacklist
      if (this.blacklist) {
        const isBlacklisted = await this.blacklist.get(`blacklist:${token}`);
        if (isBlacklisted) {
          throw new Error('Token is blacklisted');
        }
      }

      const decoded = jwt.verify(token, this.secret, {
        algorithms: [this.algorithm]
      });
      return decoded;
    } catch (error) {
      throw new Error('Invalid token');
    }
  }

  async blacklistToken(token) {
    if (this.blacklist) {
      const decoded = jwt.decode(token);
      const ttl = decoded.exp - Math.floor(Date.now() / 1000);
      await this.blacklist.setex(`blacklist:${token}`, ttl, '1');
    }
  }

  async refreshToken(refreshToken) {
    try {
      const decoded = jwt.verify(refreshToken, this.secret, {
        algorithms: [this.algorithm]
      });
      
      delete decoded.exp;
      delete decoded.iat;
      
      return await this.generateToken(decoded);
    } catch (error) {
      throw new Error('Invalid refresh token');
    }
  }

  createMiddleware() {
    return async (req, res, next) => {
      try {
        const authHeader = req.headers.authorization;
        
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
          return res.status(401).json({ error: 'No token provided' });
        }

        const token = authHeader.substring(7);
        const decoded = await this.verifyToken(token);
        
        req.user = decoded;
        next();
      } catch (error) {
        res.status(401).json({ error: 'Invalid token' });
      }
    };
  }
}

module.exports = JWTSecurityHandler;
```

### Encryption Security Handler

```javascript
// encryption-security-handler.js
const crypto = require('crypto');
const bcrypt = require('bcrypt');

class EncryptionSecurityHandler {
  constructor(config) {
    this.config = config;
    this.algorithm = config.algorithm || 'aes-256-gcm';
    this.key = config.key;
    this.ivLength = config.iv_length || 16;
    this.saltRounds = config.salt_rounds || 12;
  }

  generateKey() {
    return crypto.randomBytes(32);
  }

  generateIV() {
    return crypto.randomBytes(this.ivLength);
  }

  encrypt(data) {
    const iv = this.generateIV();
    const cipher = crypto.createCipher(this.algorithm, this.key);
    
    let encrypted = cipher.update(data, 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    const authTag = cipher.getAuthTag();
    
    return {
      encrypted,
      iv: iv.toString('hex'),
      authTag: authTag.toString('hex')
    };
  }

  decrypt(encryptedData) {
    const decipher = crypto.createDecipher(this.algorithm, this.key);
    
    decipher.setAuthTag(Buffer.from(encryptedData.authTag, 'hex'));
    
    let decrypted = decipher.update(encryptedData.encrypted, 'hex', 'utf8');
    decrypted += decipher.final('utf8');
    
    return decrypted;
  }

  async hashPassword(password) {
    return await bcrypt.hash(password, this.saltRounds);
  }

  async verifyPassword(password, hash) {
    return await bcrypt.compare(password, hash);
  }

  generateRandomString(length = 32) {
    return crypto.randomBytes(length).toString('hex');
  }

  hashData(data, algorithm = 'sha256') {
    return crypto.createHash(algorithm).update(data).digest('hex');
  }

  hmacSign(data, secret) {
    return crypto.createHmac('sha256', secret).update(data).digest('hex');
  }

  verifyHmac(data, signature, secret) {
    const expectedSignature = this.hmacSign(data, secret);
    return crypto.timingSafeEqual(
      Buffer.from(signature, 'hex'),
      Buffer.from(expectedSignature, 'hex')
    );
  }
}

module.exports = EncryptionSecurityHandler;
```

## TypeScript Implementation

```typescript
// security-handler.types.ts
export interface SecurityConfig {
  origin?: string[];
  methods?: string[];
  headers?: string[];
  credentials?: boolean;
  max_age?: number;
  window?: string;
  max_requests?: number;
  strategy?: 'sliding_window' | 'fixed_window';
  storage?: string;
  headers?: boolean;
  content_security_policy?: boolean;
  xss_protection?: boolean;
  no_sniff?: boolean;
  frameguard?: 'deny' | 'sameorigin';
  hsts?: boolean;
  hsts_max_age?: number;
  hsts_include_subdomains?: boolean;
  hsts_preload?: boolean;
  secret?: string;
  algorithm?: string;
  expires_in?: string;
  refresh_token?: boolean;
  blacklist?: string;
  key?: string;
  iv_length?: number;
  salt_rounds?: number;
}

export interface RateLimitResult {
  allowed: boolean;
  remaining: number;
  reset: number;
}

export interface TokenResult {
  token: string;
  refreshToken?: string;
}

export interface EncryptedData {
  encrypted: string;
  iv: string;
  authTag: string;
}

export interface SecurityHandler {
  createMiddleware(): any;
}

// security-handler.ts
import { SecurityConfig, SecurityHandler, RateLimitResult, TokenResult, EncryptedData } from './security-handler.types';

export class TypeScriptSecurityHandler implements SecurityHandler {
  protected config: SecurityConfig;

  constructor(config: SecurityConfig) {
    this.config = config;
  }

  createMiddleware(): any {
    throw new Error('Method not implemented');
  }
}

export class TypeScriptCORSSecurityHandler extends TypeScriptSecurityHandler {
  private origins: string[];
  private methods: string[];
  private headers: string[];
  private credentials: boolean;
  private maxAge: number;

  constructor(config: SecurityConfig) {
    super(config);
    this.origins = config.origin || ['*'];
    this.methods = config.methods || ['GET', 'POST', 'PUT', 'DELETE'];
    this.headers = config.headers || ['Content-Type', 'Authorization'];
    this.credentials = config.credentials || false;
    this.maxAge = config.max_age || 86400;
  }

  private isOriginAllowed(origin: string): boolean {
    if (this.origins.includes('*')) {
      return true;
    }
    return this.origins.includes(origin);
  }

  private getCORSHeaders(origin: string): Record<string, string> {
    const headers: Record<string, string> = {
      'Access-Control-Allow-Origin': this.isOriginAllowed(origin) ? origin : this.origins[0],
      'Access-Control-Allow-Methods': this.methods.join(', '),
      'Access-Control-Allow-Headers': this.headers.join(', '),
      'Access-Control-Max-Age': this.maxAge.toString()
    };

    if (this.credentials) {
      headers['Access-Control-Allow-Credentials'] = 'true';
    }

    return headers;
  }

  createMiddleware() {
    return (req: any, res: any, next: any) => {
      const origin = req.headers.origin;
      
      if (origin && this.isOriginAllowed(origin)) {
        const headers = this.getCORSHeaders(origin);
        
        Object.entries(headers).forEach(([key, value]) => {
          res.set(key, value);
        });
      }

      if (req.method === 'OPTIONS') {
        res.status(200).end();
        return;
      }

      next();
    };
  }
}

export class TypeScriptRateLimitSecurityHandler extends TypeScriptSecurityHandler {
  private window: number;
  private maxRequests: number;
  private strategy: string;
  private storage: any;
  private headers: boolean;
  private memoryStorage: Map<string, any>;

  constructor(config: SecurityConfig) {
    super(config);
    this.window = this.parseTimeWindow(config.window || '15m');
    this.maxRequests = config.max_requests || 100;
    this.strategy = config.strategy || 'sliding_window';
    this.headers = config.headers !== false;
    this.memoryStorage = new Map();
  }

  private parseTimeWindow(windowStr: string): number {
    const match = windowStr.match(/^(\d+)([smhd])$/);
    if (!match) return 15 * 60 * 1000;

    const [, value, unit] = match;
    const multipliers: Record<string, number> = {
      s: 1000,
      m: 60 * 1000,
      h: 60 * 60 * 1000,
      d: 24 * 60 * 60 * 1000
    };

    return parseInt(value) * (multipliers[unit] || 60000);
  }

  private getIdentifier(req: any): string {
    return req.ip || req.connection.remoteAddress || 'unknown';
  }

  private async checkRateLimit(req: any): Promise<RateLimitResult> {
    const identifier = this.getIdentifier(req);
    const key = `rate_limit:${identifier}`;
    const now = Date.now();

    if (this.strategy === 'sliding_window') {
      return this.slidingWindowMemory(key, now);
    } else {
      return this.fixedWindowMemory(key, now);
    }
  }

  private slidingWindowMemory(key: string, now: number): RateLimitResult {
    const requests = this.memoryStorage.get(key) || [];
    const validRequests = requests.filter((timestamp: number) => timestamp > now - this.window);
    
    if (validRequests.length >= this.maxRequests) {
      return {
        allowed: false,
        remaining: 0,
        reset: now + this.window
      };
    }

    validRequests.push(now);
    this.memoryStorage.set(key, validRequests);

    return {
      allowed: true,
      remaining: this.maxRequests - validRequests.length,
      reset: now + this.window
    };
  }

  private fixedWindowMemory(key: string): RateLimitResult {
    const current = (this.memoryStorage.get(key) || 0) + 1;
    this.memoryStorage.set(key, current);

    if (current > this.maxRequests) {
      return {
        allowed: false,
        remaining: 0,
        reset: Date.now() + this.window
      };
    }

    return {
      allowed: true,
      remaining: this.maxRequests - current,
      reset: Date.now() + this.window
    };
  }

  createMiddleware() {
    return async (req: any, res: any, next: any) => {
      try {
        const result = await this.checkRateLimit(req);
        
        if (this.headers) {
          res.set('X-RateLimit-Limit', this.maxRequests.toString());
          res.set('X-RateLimit-Remaining', result.remaining.toString());
          res.set('X-RateLimit-Reset', new Date(result.reset).toISOString());
        }

        if (!result.allowed) {
          res.status(429).json({
            error: 'Rate limit exceeded',
            retryAfter: Math.ceil((result.reset - Date.now()) / 1000)
          });
          return;
        }

        next();
      } catch (error) {
        console.error('Rate limiting error:', error);
        next();
      }
    };
  }
}
```

## Advanced Usage Scenarios

### Multi-Layer Security

```javascript
// multi-layer-security.js
class MultiLayerSecurity {
  constructor(configs) {
    this.handlers = new Map();
    this.middlewares = [];
    
    this.initializeHandlers(configs);
  }

  initializeHandlers(configs) {
    if (configs.cors) {
      const CORSHandler = require('./cors-security-handler');
      this.handlers.set('cors', new CORSHandler(configs.cors));
    }

    if (configs.rate_limit) {
      const RateLimitHandler = require('./rate-limit-security-handler');
      this.handlers.set('rate_limit', new RateLimitHandler(configs.rate_limit));
    }

    if (configs.helmet) {
      const HelmetHandler = require('./helmet-security-handler');
      this.handlers.set('helmet', new HelmetHandler(configs.helmet));
    }

    if (configs.jwt) {
      const JWTHandler = require('./jwt-security-handler');
      this.handlers.set('jwt', new JWTHandler(configs.jwt));
    }
  }

  createSecurityMiddleware() {
    const middlewares = [];

    // Add helmet middlewares first
    if (this.handlers.has('helmet')) {
      middlewares.push(...this.handlers.get('helmet').createMiddleware());
    }

    // Add CORS
    if (this.handlers.has('cors')) {
      middlewares.push(this.handlers.get('cors').createMiddleware());
    }

    // Add rate limiting
    if (this.handlers.has('rate_limit')) {
      middlewares.push(this.handlers.get('rate_limit').createMiddleware());
    }

    // Add JWT authentication
    if (this.handlers.has('jwt')) {
      middlewares.push(this.handlers.get('jwt').createMiddleware());
    }

    return middlewares;
  }

  getHandler(type) {
    return this.handlers.get(type);
  }
}
```

### Security Policy Manager

```javascript
// security-policy-manager.js
class SecurityPolicyManager {
  constructor() {
    this.policies = new Map();
    this.environments = ['development', 'staging', 'production'];
  }

  definePolicy(name, policy) {
    this.policies.set(name, this.validatePolicy(policy));
  }

  getPolicy(name) {
    const policy = this.policies.get(name);
    if (!policy) {
      throw new Error(`Policy '${name}' not found`);
    }
    return policy;
  }

  validatePolicy(policy) {
    const required = ['cors', 'rate_limit', 'helmet'];
    const missing = required.filter(field => !policy[field]);
    
    if (missing.length > 0) {
      throw new Error(`Missing required policy fields: ${missing.join(', ')}`);
    }
    
    return policy;
  }

  getEnvironmentPolicy(environment) {
    if (!this.environments.includes(environment)) {
      throw new Error(`Invalid environment: ${environment}`);
    }
    
    const basePolicy = this.getPolicy('base');
    const envPolicy = this.getPolicy(environment);
    
    return this.mergePolicies(basePolicy, envPolicy);
  }

  mergePolicies(base, override) {
    const merged = { ...base };
    
    for (const [key, value] of Object.entries(override)) {
      if (typeof value === 'object' && !Array.isArray(value)) {
        merged[key] = this.mergePolicies(merged[key] || {}, value);
      } else {
        merged[key] = value;
      }
    }
    
    return merged;
  }
}

// Usage
const policyManager = new SecurityPolicyManager();

policyManager.definePolicy('base', {
  cors: {
    origin: ['https://app.com'],
    methods: ['GET', 'POST'],
    credentials: true
  },
  rate_limit: {
    window: '15m',
    max_requests: 100
  },
  helmet: {
    content_security_policy: true,
    xss_protection: true
  }
});

policyManager.definePolicy('production', {
  cors: {
    origin: ['https://app.com', 'https://api.app.com']
  },
  rate_limit: {
    max_requests: 50
  },
  helmet: {
    hsts: true,
    hsts_max_age: 31536000
  }
});
```

## Real-World Examples

### Express.js Security Setup

```javascript
// express-security-setup.js
const express = require('express');
const MultiLayerSecurity = require('./multi-layer-security');

class ExpressSecuritySetup {
  constructor(app, config) {
    this.app = app;
    this.security = new MultiLayerSecurity(config);
  }

  setupSecurity() {
    const middlewares = this.security.createSecurityMiddleware();
    
    // Apply security middlewares
    middlewares.forEach(middleware => {
      this.app.use(middleware);
    });

    // Additional security headers
    this.app.use((req, res, next) => {
      res.set('X-Powered-By', 'TuskLang');
      res.set('X-Content-Type-Options', 'nosniff');
      res.set('X-Frame-Options', 'DENY');
      next();
    });

    // Error handling for security violations
    this.app.use((error, req, res, next) => {
      if (error.name === 'UnauthorizedError') {
        res.status(401).json({ error: 'Authentication required' });
      } else if (error.name === 'RateLimitError') {
        res.status(429).json({ error: 'Too many requests' });
      } else {
        next(error);
      }
    });
  }

  setupRoutes() {
    // Protected routes
    this.app.use('/api', this.security.getHandler('jwt').createMiddleware());
    
    this.app.get('/api/protected', (req, res) => {
      res.json({ message: 'Protected resource', user: req.user });
    });

    // Public routes
    this.app.get('/api/public', (req, res) => {
      res.json({ message: 'Public resource' });
    });
  }
}

// Usage
const app = express();
const securityConfig = {
  cors: {
    origin: ['https://app.com'],
    credentials: true
  },
  rate_limit: {
    window: '15m',
    max_requests: 100
  },
  helmet: {
    content_security_policy: true,
    xss_protection: true
  },
  jwt: {
    secret: process.env.JWT_SECRET,
    expires_in: '24h'
  }
};

const securitySetup = new ExpressSecuritySetup(app, securityConfig);
securitySetup.setupSecurity();
securitySetup.setupRoutes();
```

### API Gateway Security

```javascript
// api-gateway-security.js
class APIGatewaySecurity {
  constructor(config) {
    this.config = config;
    this.security = new MultiLayerSecurity(config);
  }

  createGateway() {
    const express = require('express');
    const app = express();

    // Apply security middlewares
    const middlewares = this.security.createSecurityMiddleware();
    middlewares.forEach(middleware => app.use(middleware));

    // Request validation
    app.use(this.validateRequest.bind(this));

    // Rate limiting per API key
    app.use(this.apiKeyRateLimit.bind(this));

    // Logging
    app.use(this.logRequest.bind(this));

    return app;
  }

  validateRequest(req, res, next) {
    // Validate API key
    const apiKey = req.headers['x-api-key'];
    if (!apiKey) {
      return res.status(401).json({ error: 'API key required' });
    }

    // Validate request signature
    const signature = req.headers['x-signature'];
    if (signature) {
      const isValid = this.verifySignature(req, signature);
      if (!isValid) {
        return res.status(401).json({ error: 'Invalid signature' });
      }
    }

    next();
  }

  verifySignature(req, signature) {
    const crypto = require('crypto');
    const secret = this.config.api_secret;
    
    const data = req.method + req.url + JSON.stringify(req.body);
    const expectedSignature = crypto
      .createHmac('sha256', secret)
      .update(data)
      .digest('hex');
    
    return crypto.timingSafeEqual(
      Buffer.from(signature, 'hex'),
      Buffer.from(expectedSignature, 'hex')
    );
  }

  apiKeyRateLimit(req, res, next) {
    const apiKey = req.headers['x-api-key'];
    const rateLimitHandler = this.security.getHandler('rate_limit');
    
    // Override identifier to use API key
    const originalGetIdentifier = rateLimitHandler.getIdentifier;
    rateLimitHandler.getIdentifier = () => apiKey;
    
    rateLimitHandler.createMiddleware()(req, res, next);
    
    // Restore original method
    rateLimitHandler.getIdentifier = originalGetIdentifier;
  }

  logRequest(req, res, next) {
    const start = Date.now();
    
    res.on('finish', () => {
      const duration = Date.now() - start;
      console.log({
        method: req.method,
        url: req.url,
        status: res.statusCode,
        duration,
        ip: req.ip,
        userAgent: req.get('User-Agent'),
        apiKey: req.headers['x-api-key']?.substring(0, 8) + '...'
      });
    });
    
    next();
  }
}
```

## Performance Considerations

### Security Caching

```javascript
// security-cache.js
class SecurityCache {
  constructor() {
    this.cache = new Map();
    this.ttl = 300000; // 5 minutes
  }

  get(key) {
    const entry = this.cache.get(key);
    if (!entry) return null;
    
    if (Date.now() > entry.expires) {
      this.cache.delete(key);
      return null;
    }
    
    return entry.value;
  }

  set(key, value, ttl = this.ttl) {
    this.cache.set(key, {
      value,
      expires: Date.now() + ttl
    });
  }

  clear() {
    this.cache.clear();
  }

  cleanup() {
    const now = Date.now();
    for (const [key, entry] of this.cache.entries()) {
      if (now > entry.expires) {
        this.cache.delete(key);
      }
    }
  }
}
```

### Security Metrics

```javascript
// security-metrics.js
class SecurityMetrics {
  constructor() {
    this.metrics = {
      requests: 0,
      blocked: 0,
      rateLimited: 0,
      unauthorized: 0,
      errors: 0
    };
  }

  recordRequest() {
    this.metrics.requests++;
  }

  recordBlocked() {
    this.metrics.blocked++;
  }

  recordRateLimited() {
    this.metrics.rateLimited++;
  }

  recordUnauthorized() {
    this.metrics.unauthorized++;
  }

  recordError() {
    this.metrics.errors++;
  }

  getMetrics() {
    return {
      ...this.metrics,
      blockRate: this.metrics.requests > 0 ? 
        this.metrics.blocked / this.metrics.requests : 0,
      errorRate: this.metrics.requests > 0 ? 
        this.metrics.errors / this.metrics.requests : 0
    };
  }

  reset() {
    this.metrics = {
      requests: 0,
      blocked: 0,
      rateLimited: 0,
      unauthorized: 0,
      errors: 0
    };
  }
}
```

## Security Notes

### Input Validation

```javascript
// input-validation.js
class InputValidation {
  constructor() {
    this.validators = new Map();
  }

  addValidator(field, validator) {
    this.validators.set(field, validator);
  }

  validate(data) {
    const errors = [];
    
    for (const [field, validator] of this.validators.entries()) {
      if (data[field] !== undefined) {
        try {
          validator(data[field]);
        } catch (error) {
          errors.push({ field, error: error.message });
        }
      }
    }
    
    if (errors.length > 0) {
      throw new Error(`Validation failed: ${JSON.stringify(errors)}`);
    }
    
    return data;
  }

  sanitizeString(str) {
    return str.replace(/[<>]/g, '');
  }

  validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      throw new Error('Invalid email format');
    }
  }

  validatePassword(password) {
    if (password.length < 8) {
      throw new Error('Password must be at least 8 characters');
    }
    
    if (!/[A-Z]/.test(password)) {
      throw new Error('Password must contain uppercase letter');
    }
    
    if (!/[a-z]/.test(password)) {
      throw new Error('Password must contain lowercase letter');
    }
    
    if (!/\d/.test(password)) {
      throw new Error('Password must contain number');
    }
  }
}
```

### SQL Injection Prevention

```javascript
// sql-injection-prevention.js
class SQLInjectionPrevention {
  static validateQuery(sql) {
    const dangerousPatterns = [
      /DROP\s+TABLE/i,
      /DELETE\s+FROM/i,
      /UPDATE\s+.+\s+SET/i,
      /INSERT\s+INTO/i,
      /CREATE\s+TABLE/i,
      /ALTER\s+TABLE/i
    ];

    for (const pattern of dangerousPatterns) {
      if (pattern.test(sql)) {
        throw new Error('Potentially dangerous SQL operation detected');
      }
    }
  }

  static useParameterizedQueries(sql, params) {
    const paramCount = (sql.match(/\?/g) || []).length;
    if (paramCount !== params.length) {
      throw new Error('Parameter count mismatch');
    }
    
    return { sql, params };
  }

  static sanitizeInput(input) {
    if (typeof input === 'string') {
      return input.replace(/['";\\]/g, '');
    }
    return input;
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
      throw new Error(`No configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.cors) {
      throw new Error('CORS configuration is required');
    }
    
    if (!config.rate_limit) {
      throw new Error('Rate limiting configuration is required');
    }
    
    return config;
  }

  getCurrentConfig() {
    const environment = process.env.NODE_ENV || 'development';
    return this.getConfig(environment);
  }
}
```

### Security Health Monitoring

```javascript
// security-health-monitor.js
class SecurityHealthMonitor {
  constructor(securityHandlers) {
    this.handlers = securityHandlers;
    this.metrics = new SecurityMetrics();
  }

  async checkHealth() {
    const health = {
      status: 'healthy',
      checks: {}
    };

    // Check CORS
    if (this.handlers.cors) {
      health.checks.cors = { status: 'ok' };
    }

    // Check rate limiting
    if (this.handlers.rate_limit) {
      health.checks.rate_limit = { status: 'ok' };
    }

    // Check JWT
    if (this.handlers.jwt) {
      try {
        const testToken = this.handlers.jwt.generateToken({ test: true });
        await this.handlers.jwt.verifyToken(testToken.token);
        health.checks.jwt = { status: 'ok' };
      } catch (error) {
        health.checks.jwt = { status: 'error', error: error.message };
        health.status = 'unhealthy';
      }
    }

    health.metrics = this.metrics.getMetrics();
    return health;
  }

  getMetrics() {
    return this.metrics.getMetrics();
  }
}
```

## Related Topics

- [@secure Operator](./35-tsklang-javascript-operator-secure.md)
- [@auth Operator](./30-tsklang-javascript-operator-auth.md)
- [@encrypt Operator](./36-tsklang-javascript-operator-encrypt.md)
- [@hash Operator](./37-tsklang-javascript-operator-hash.md)
- [@auth Directive](./82-tsklang-javascript-directives-auth.md) 
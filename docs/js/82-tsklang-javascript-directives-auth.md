# TuskLang JavaScript Documentation: #auth Directive

## Overview

The `#auth` directive in TuskLang defines authentication and authorization patterns, enabling declarative security configuration with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#auth jwt
  secret: ${JWT_SECRET}
  expires_in: 24h
  refresh_token: true
  blacklist: redis://localhost:6379

#auth oauth2
  provider: google
  client_id: ${GOOGLE_CLIENT_ID}
  client_secret: ${GOOGLE_CLIENT_SECRET}
  redirect_uri: https://app.com/auth/callback
  scope: email profile

#auth api_key
  header: X-API-Key
  validate: ${API_KEY_VALIDATOR}
  rate_limit: 1000/hour
  cache: 300

#auth session
  store: redis://localhost:6379
  secret: ${SESSION_SECRET}
  expires_in: 7d
  secure: true
  http_only: true
```

## JavaScript Integration

### JWT Authentication Handler

```javascript
// jwt-auth-handler.js
const jwt = require('jsonwebtoken');
const Redis = require('ioredis');

class JWTAuthHandler {
  constructor(config) {
    this.secret = config.secret;
    this.expiresIn = config.expires_in || '24h';
    this.refreshToken = config.refresh_token || false;
    this.blacklist = config.blacklist ? new Redis(config.blacklist) : null;
  }

  async generateToken(payload) {
    const token = jwt.sign(payload, this.secret, {
      expiresIn: this.expiresIn
    });

    if (this.refreshToken) {
      const refreshToken = jwt.sign(payload, this.secret, {
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

      const decoded = jwt.verify(token, this.secret);
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
      const decoded = jwt.verify(refreshToken, this.secret);
      delete decoded.exp;
      delete decoded.iat;
      
      return await this.generateToken(decoded);
    } catch (error) {
      throw new Error('Invalid refresh token');
    }
  }
}

module.exports = JWTAuthHandler;
```

### OAuth2 Authentication Handler

```javascript
// oauth2-auth-handler.js
const axios = require('axios');
const crypto = require('crypto');

class OAuth2AuthHandler {
  constructor(config) {
    this.provider = config.provider;
    this.clientId = config.client_id;
    this.clientSecret = config.client_secret;
    this.redirectUri = config.redirect_uri;
    this.scope = config.scope;
    this.providers = this.initializeProviders();
  }

  initializeProviders() {
    return {
      google: {
        authUrl: 'https://accounts.google.com/o/oauth2/v2/auth',
        tokenUrl: 'https://oauth2.googleapis.com/token',
        userInfoUrl: 'https://www.googleapis.com/oauth2/v2/userinfo'
      },
      github: {
        authUrl: 'https://github.com/login/oauth/authorize',
        tokenUrl: 'https://github.com/login/oauth/access_token',
        userInfoUrl: 'https://api.github.com/user'
      },
      facebook: {
        authUrl: 'https://www.facebook.com/v12.0/dialog/oauth',
        tokenUrl: 'https://graph.facebook.com/v12.0/oauth/access_token',
        userInfoUrl: 'https://graph.facebook.com/me'
      }
    };
  }

  getAuthUrl(state) {
    const provider = this.providers[this.provider];
    if (!provider) {
      throw new Error(`Unsupported provider: ${this.provider}`);
    }

    const params = new URLSearchParams({
      client_id: this.clientId,
      redirect_uri: this.redirectUri,
      response_type: 'code',
      scope: this.scope,
      state: state
    });

    return `${provider.authUrl}?${params.toString()}`;
  }

  async exchangeCodeForToken(code) {
    const provider = this.providers[this.provider];
    
    const params = {
      client_id: this.clientId,
      client_secret: this.clientSecret,
      code: code,
      redirect_uri: this.redirectUri,
      grant_type: 'authorization_code'
    };

    const response = await axios.post(provider.tokenUrl, params, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded'
      }
    });

    return response.data;
  }

  async getUserInfo(accessToken) {
    const provider = this.providers[this.provider];
    
    const response = await axios.get(provider.userInfoUrl, {
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    });

    return response.data;
  }

  async authenticate(code) {
    const tokenData = await this.exchangeCodeForToken(code);
    const userInfo = await this.getUserInfo(tokenData.access_token);
    
    return {
      accessToken: tokenData.access_token,
      refreshToken: tokenData.refresh_token,
      user: userInfo
    };
  }
}

module.exports = OAuth2AuthHandler;
```

### API Key Authentication Handler

```javascript
// api-key-auth-handler.js
const crypto = require('crypto');
const Redis = require('ioredis');

class APIKeyAuthHandler {
  constructor(config) {
    this.header = config.header || 'X-API-Key';
    this.validator = config.validate;
    this.rateLimit = config.rate_limit;
    this.cache = config.cache ? new Redis(config.cache) : null;
    this.rateLimitCache = new Map();
  }

  async validateApiKey(apiKey) {
    // Check cache first
    if (this.cache) {
      const cached = await this.cache.get(`apikey:${apiKey}`);
      if (cached) {
        return JSON.parse(cached);
      }
    }

    // Validate API key
    let isValid = false;
    let user = null;

    if (typeof this.validator === 'function') {
      const result = await this.validator(apiKey);
      isValid = result.isValid;
      user = result.user;
    } else if (typeof this.validator === 'string') {
      // Database query
      const db = require('./database');
      user = await db.query(this.validator, [apiKey]);
      isValid = !!user;
    }

    // Cache result
    if (this.cache && isValid) {
      await this.cache.setex(`apikey:${apiKey}`, this.cache, JSON.stringify(user));
    }

    return { isValid, user };
  }

  async checkRateLimit(apiKey) {
    if (!this.rateLimit) return true;

    const [limit, period] = this.parseRateLimit(this.rateLimit);
    const key = `ratelimit:${apiKey}`;
    
    const current = await this.rateLimitCache.get(key) || 0;
    
    if (current >= limit) {
      return false;
    }

    await this.rateLimitCache.set(key, current + 1, period);
    return true;
  }

  parseRateLimit(rateLimit) {
    const match = rateLimit.match(/(\d+)\/(\w+)/);
    if (!match) return [1000, 3600]; // Default: 1000/hour

    const [, limit, period] = match;
    const periods = {
      second: 1,
      minute: 60,
      hour: 3600,
      day: 86400
    };

    return [parseInt(limit), periods[period] || 3600];
  }

  async authenticate(req) {
    const apiKey = req.headers[this.header.toLowerCase()];
    
    if (!apiKey) {
      throw new Error('API key required');
    }

    const { isValid, user } = await this.validateApiKey(apiKey);
    
    if (!isValid) {
      throw new Error('Invalid API key');
    }

    const rateLimitOk = await this.checkRateLimit(apiKey);
    if (!rateLimitOk) {
      throw new Error('Rate limit exceeded');
    }

    return user;
  }
}

module.exports = APIKeyAuthHandler;
```

### Session Authentication Handler

```javascript
// session-auth-handler.js
const Redis = require('ioredis');
const crypto = require('crypto');

class SessionAuthHandler {
  constructor(config) {
    this.store = config.store ? new Redis(config.store) : null;
    this.secret = config.secret;
    this.expiresIn = config.expires_in || '7d';
    this.secure = config.secure || false;
    this.httpOnly = config.http_only || true;
  }

  async createSession(user) {
    const sessionId = crypto.randomBytes(32).toString('hex');
    const sessionData = {
      userId: user.id,
      email: user.email,
      role: user.role,
      createdAt: Date.now()
    };

    const ttl = this.parseExpiresIn(this.expiresIn);
    
    if (this.store) {
      await this.store.setex(`session:${sessionId}`, ttl, JSON.stringify(sessionData));
    }

    return {
      sessionId,
      sessionData,
      cookie: this.generateCookie(sessionId, ttl)
    };
  }

  async getSession(sessionId) {
    if (!this.store) return null;

    const sessionData = await this.store.get(`session:${sessionId}`);
    return sessionData ? JSON.parse(sessionData) : null;
  }

  async updateSession(sessionId, updates) {
    if (!this.store) return false;

    const sessionData = await this.getSession(sessionId);
    if (!sessionData) return false;

    const updatedData = { ...sessionData, ...updates };
    const ttl = this.parseExpiresIn(this.expiresIn);
    
    await this.store.setex(`session:${sessionId}`, ttl, JSON.stringify(updatedData));
    return true;
  }

  async destroySession(sessionId) {
    if (!this.store) return false;

    await this.store.del(`session:${sessionId}`);
    return true;
  }

  generateCookie(sessionId, ttl) {
    const options = {
      httpOnly: this.httpOnly,
      secure: this.secure,
      maxAge: ttl * 1000,
      sameSite: 'strict'
    };

    return {
      name: 'sessionId',
      value: sessionId,
      options
    };
  }

  parseExpiresIn(expiresIn) {
    const match = expiresIn.match(/(\d+)([smhd])/);
    if (!match) return 604800; // Default: 7 days

    const [, value, unit] = match;
    const multipliers = {
      s: 1,
      m: 60,
      h: 3600,
      d: 86400
    };

    return parseInt(value) * (multipliers[unit] || 86400);
  }

  async authenticate(req) {
    const sessionId = req.cookies?.sessionId;
    
    if (!sessionId) {
      throw new Error('Session required');
    }

    const sessionData = await this.getSession(sessionId);
    
    if (!sessionData) {
      throw new Error('Invalid session');
    }

    return sessionData;
  }
}

module.exports = SessionAuthHandler;
```

## TypeScript Implementation

```typescript
// auth-handler.types.ts
export interface AuthConfig {
  secret?: string;
  expires_in?: string;
  refresh_token?: boolean;
  blacklist?: string;
  provider?: string;
  client_id?: string;
  client_secret?: string;
  redirect_uri?: string;
  scope?: string;
  header?: string;
  validate?: string | Function;
  rate_limit?: string;
  cache?: string | number;
  store?: string;
  secure?: boolean;
  http_only?: boolean;
}

export interface AuthResult {
  user: any;
  token?: string;
  refreshToken?: string;
  sessionId?: string;
}

export interface JWTPayload {
  userId: string;
  email: string;
  role: string;
  iat?: number;
  exp?: number;
}

export interface OAuth2Token {
  access_token: string;
  refresh_token?: string;
  expires_in: number;
  token_type: string;
}

export interface UserInfo {
  id: string;
  email: string;
  name: string;
  picture?: string;
}

// auth-handler.ts
import { AuthConfig, AuthResult, JWTPayload, OAuth2Token, UserInfo } from './auth-handler.types';

export class TypeScriptAuthHandler {
  private config: AuthConfig;
  private handlers: Map<string, any> = new Map();

  constructor(config: AuthConfig) {
    this.config = config;
    this.initializeHandlers();
  }

  private initializeHandlers(): void {
    this.handlers.set('jwt', new JWTAuthHandler(this.config));
    this.handlers.set('oauth2', new OAuth2AuthHandler(this.config));
    this.handlers.set('api_key', new APIKeyAuthHandler(this.config));
    this.handlers.set('session', new SessionAuthHandler(this.config));
  }

  async authenticate(type: string, credentials: any): Promise<AuthResult> {
    const handler = this.handlers.get(type);
    if (!handler) {
      throw new Error(`Unsupported auth type: ${type}`);
    }

    return await handler.authenticate(credentials);
  }

  async validate(type: string, token: string): Promise<any> {
    const handler = this.handlers.get(type);
    if (!handler) {
      throw new Error(`Unsupported auth type: ${type}`);
    }

    return await handler.verifyToken(token);
  }
}

class JWTAuthHandler {
  private secret: string;
  private expiresIn: string;
  private refreshToken: boolean;
  private blacklist: any;

  constructor(config: AuthConfig) {
    this.secret = config.secret || '';
    this.expiresIn = config.expires_in || '24h';
    this.refreshToken = config.refresh_token || false;
    this.blacklist = config.blacklist ? new (require('ioredis'))(config.blacklist) : null;
  }

  async generateToken(payload: JWTPayload): Promise<{ token: string; refreshToken?: string }> {
    const jwt = require('jsonwebtoken');
    
    const token = jwt.sign(payload, this.secret, {
      expiresIn: this.expiresIn
    });

    if (this.refreshToken) {
      const refreshToken = jwt.sign(payload, this.secret, {
        expiresIn: '7d'
      });
      return { token, refreshToken };
    }

    return { token };
  }

  async verifyToken(token: string): Promise<JWTPayload> {
    const jwt = require('jsonwebtoken');
    
    try {
      if (this.blacklist) {
        const isBlacklisted = await this.blacklist.get(`blacklist:${token}`);
        if (isBlacklisted) {
          throw new Error('Token is blacklisted');
        }
      }

      const decoded = jwt.verify(token, this.secret) as JWTPayload;
      return decoded;
    } catch (error) {
      throw new Error('Invalid token');
    }
  }
}

class OAuth2AuthHandler {
  private provider: string;
  private clientId: string;
  private clientSecret: string;
  private redirectUri: string;
  private scope: string;

  constructor(config: AuthConfig) {
    this.provider = config.provider || '';
    this.clientId = config.client_id || '';
    this.clientSecret = config.client_secret || '';
    this.redirectUri = config.redirect_uri || '';
    this.scope = config.scope || '';
  }

  async authenticate(code: string): Promise<AuthResult> {
    const tokenData = await this.exchangeCodeForToken(code);
    const userInfo = await this.getUserInfo(tokenData.access_token);
    
    return {
      user: userInfo,
      token: tokenData.access_token,
      refreshToken: tokenData.refresh_token
    };
  }

  private async exchangeCodeForToken(code: string): Promise<OAuth2Token> {
    const axios = require('axios');
    
    const params = {
      client_id: this.clientId,
      client_secret: this.clientSecret,
      code: code,
      redirect_uri: this.redirectUri,
      grant_type: 'authorization_code'
    };

    const response = await axios.post(this.getTokenUrl(), params, {
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded'
      }
    });

    return response.data;
  }

  private async getUserInfo(accessToken: string): Promise<UserInfo> {
    const axios = require('axios');
    
    const response = await axios.get(this.getUserInfoUrl(), {
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    });

    return response.data;
  }

  private getTokenUrl(): string {
    const urls = {
      google: 'https://oauth2.googleapis.com/token',
      github: 'https://github.com/login/oauth/access_token',
      facebook: 'https://graph.facebook.com/v12.0/oauth/access_token'
    };
    
    return urls[this.provider as keyof typeof urls] || '';
  }

  private getUserInfoUrl(): string {
    const urls = {
      google: 'https://www.googleapis.com/oauth2/v2/userinfo',
      github: 'https://api.github.com/user',
      facebook: 'https://graph.facebook.com/me'
    };
    
    return urls[this.provider as keyof typeof urls] || '';
  }
}
```

## Advanced Usage Scenarios

### Multi-Strategy Authentication

```javascript
// multi-auth-handler.js
class MultiAuthHandler {
  constructor(strategies) {
    this.strategies = new Map();
    strategies.forEach(strategy => {
      this.strategies.set(strategy.type, strategy.handler);
    });
  }

  async authenticate(credentials) {
    const { type, ...authData } = credentials;
    
    const strategy = this.strategies.get(type);
    if (!strategy) {
      throw new Error(`Unsupported auth strategy: ${type}`);
    }

    return await strategy.authenticate(authData);
  }

  async validate(token, type) {
    const strategy = this.strategies.get(type);
    if (!strategy) {
      throw new Error(`Unsupported auth strategy: ${type}`);
    }

    return await strategy.verifyToken(token);
  }
}

// Usage
const multiAuth = new MultiAuthHandler([
  { type: 'jwt', handler: new JWTAuthHandler(jwtConfig) },
  { type: 'oauth2', handler: new OAuth2AuthHandler(oauthConfig) },
  { type: 'api_key', handler: new APIKeyAuthHandler(apiKeyConfig) }
]);
```

### Role-Based Authorization

```javascript
// role-auth-handler.js
class RoleAuthHandler {
  constructor() {
    this.roles = new Map();
    this.permissions = new Map();
  }

  defineRole(role, permissions) {
    this.roles.set(role, permissions);
  }

  definePermission(permission, check) {
    this.permissions.set(permission, check);
  }

  async checkPermission(user, permission) {
    const userPermissions = this.roles.get(user.role) || [];
    
    if (!userPermissions.includes(permission)) {
      return false;
    }

    const check = this.permissions.get(permission);
    if (check) {
      return await check(user);
    }

    return true;
  }

  async authorize(user, requiredPermissions) {
    const checks = requiredPermissions.map(permission => 
      this.checkPermission(user, permission)
    );
    
    const results = await Promise.all(checks);
    return results.every(result => result);
  }
}

// Usage
const roleAuth = new RoleAuthHandler();

roleAuth.defineRole('admin', ['read', 'write', 'delete', 'manage_users']);
roleAuth.defineRole('user', ['read', 'write']);
roleAuth.defineRole('guest', ['read']);

roleAuth.definePermission('manage_users', async (user) => {
  return user.department === 'IT';
});

const isAuthorized = await roleAuth.authorize(user, ['manage_users']);
```

## Real-World Examples

### Express.js Middleware Integration

```javascript
// auth-middleware.js
const createAuthMiddleware = (authHandler) => {
  return async (req, res, next) => {
    try {
      const token = req.headers.authorization?.replace('Bearer ', '');
      
      if (!token) {
        return res.status(401).json({ error: 'No token provided' });
      }

      const user = await authHandler.verifyToken(token);
      req.user = user;
      next();
    } catch (error) {
      res.status(401).json({ error: 'Invalid token' });
    }
  };
};

// role-middleware.js
const createRoleMiddleware = (requiredRoles) => {
  return (req, res, next) => {
    if (!req.user) {
      return res.status(401).json({ error: 'Authentication required' });
    }

    if (!requiredRoles.includes(req.user.role)) {
      return res.status(403).json({ error: 'Insufficient permissions' });
    }

    next();
  };
};

// Usage
app.use('/api/admin', createAuthMiddleware(jwtAuth), createRoleMiddleware(['admin']));
```

### GraphQL Authentication

```javascript
// graphql-auth.js
const { ApolloServer } = require('apollo-server-express');

const createAuthContext = (authHandler) => {
  return async ({ req }) => {
    const token = req.headers.authorization?.replace('Bearer ', '');
    
    if (!token) {
      return { user: null };
    }

    try {
      const user = await authHandler.verifyToken(token);
      return { user };
    } catch (error) {
      return { user: null };
    }
  };
};

const authDirective = {
  name: 'auth',
  locations: ['FIELD_DEFINITION'],
  resolveField: async (parent, args, context) => {
    if (!context.user) {
      throw new Error('Authentication required');
    }
    return parent[fieldName];
  }
};

// Usage
const server = new ApolloServer({
  typeDefs,
  resolvers,
  context: createAuthContext(jwtAuth),
  schemaDirectives: {
    auth: authDirective
  }
});
```

## Performance Considerations

### Token Caching

```javascript
// token-cache.js
class TokenCache {
  constructor() {
    this.cache = new Map();
    this.stats = { hits: 0, misses: 0 };
  }

  async get(key) {
    const cached = this.cache.get(key);
    if (cached && cached.expires > Date.now()) {
      this.stats.hits++;
      return cached.data;
    }
    
    this.stats.misses++;
    return null;
  }

  set(key, data, ttl = 300000) {
    this.cache.set(key, {
      data,
      expires: Date.now() + ttl
    });
  }

  cleanup() {
    const now = Date.now();
    for (const [key, entry] of this.cache.entries()) {
      if (entry.expires < now) {
        this.cache.delete(key);
      }
    }
  }
}
```

### Rate Limiting

```javascript
// auth-rate-limiter.js
class AuthRateLimiter {
  constructor() {
    this.attempts = new Map();
    this.lockouts = new Map();
  }

  async checkRateLimit(identifier, maxAttempts = 5, lockoutTime = 900000) {
    const now = Date.now();
    
    // Check if locked out
    const lockout = this.lockouts.get(identifier);
    if (lockout && lockout > now) {
      throw new Error('Account temporarily locked');
    }

    // Get current attempts
    const attempts = this.attempts.get(identifier) || [];
    const recentAttempts = attempts.filter(time => time > now - 900000); // 15 minutes

    if (recentAttempts.length >= maxAttempts) {
      this.lockouts.set(identifier, now + lockoutTime);
      throw new Error('Too many failed attempts');
    }

    return true;
  }

  recordAttempt(identifier, success) {
    const attempts = this.attempts.get(identifier) || [];
    
    if (success) {
      // Clear attempts on successful login
      this.attempts.delete(identifier);
      this.lockouts.delete(identifier);
    } else {
      // Record failed attempt
      attempts.push(Date.now());
      this.attempts.set(identifier, attempts.slice(-10)); // Keep last 10 attempts
    }
  }
}
```

## Security Notes

### Token Security

```javascript
// token-security.js
class TokenSecurity {
  validateToken(token) {
    // Check token format
    if (!token || typeof token !== 'string') {
      return false;
    }

    // Check token length
    if (token.length < 32) {
      return false;
    }

    // Check for suspicious patterns
    if (token.includes('..') || token.includes('--')) {
      return false;
    }

    return true;
  }

  sanitizeToken(token) {
    return token.replace(/[^a-zA-Z0-9\-._~+/=]/g, '');
  }

  generateSecureToken() {
    const crypto = require('crypto');
    return crypto.randomBytes(32).toString('base64url');
  }
}
```

### Password Security

```javascript
// password-security.js
const bcrypt = require('bcrypt');

class PasswordSecurity {
  async hashPassword(password, rounds = 12) {
    return await bcrypt.hash(password, rounds);
  }

  async verifyPassword(password, hash) {
    return await bcrypt.compare(password, hash);
  }

  validatePassword(password) {
    const minLength = 8;
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumbers = /\d/.test(password);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);

    return password.length >= minLength && 
           hasUpperCase && 
           hasLowerCase && 
           hasNumbers && 
           hasSpecialChar;
  }
}
```

## Best Practices

### Authentication Flow

```javascript
// auth-flow.js
class AuthenticationFlow {
  constructor(authHandler, rateLimiter) {
    this.authHandler = authHandler;
    this.rateLimiter = rateLimiter;
  }

  async login(credentials) {
    try {
      // Check rate limit
      await this.rateLimiter.checkRateLimit(credentials.email);
      
      // Authenticate
      const result = await this.authHandler.authenticate(credentials);
      
      // Record successful attempt
      this.rateLimiter.recordAttempt(credentials.email, true);
      
      return result;
    } catch (error) {
      // Record failed attempt
      this.rateLimiter.recordAttempt(credentials.email, false);
      throw error;
    }
  }

  async logout(token) {
    if (this.authHandler.blacklistToken) {
      await this.authHandler.blacklistToken(token);
    }
    
    return { success: true };
  }
}
```

### Error Handling

```javascript
// auth-error-handler.js
class AuthErrorHandler {
  handleError(error) {
    console.error('Authentication error:', error);
    
    if (error.message.includes('Invalid token')) {
      return { status: 401, message: 'Invalid or expired token' };
    }
    
    if (error.message.includes('Rate limit')) {
      return { status: 429, message: 'Too many requests' };
    }
    
    if (error.message.includes('Insufficient permissions')) {
      return { status: 403, message: 'Access denied' };
    }
    
    return { status: 500, message: 'Internal server error' };
  }
}
```

## Related Topics

- [@auth Operator](./30-tsklang-javascript-operator-auth.md)
- [@secure Operator](./35-tsklang-javascript-operator-secure.md)
- [@route Directive](./81-tsklang-javascript-directives-route.md)
- [@middleware Directive](./80-tsklang-javascript-directives-middleware.md)
- [@api Directive](./76-tsklang-javascript-directives-api.md) 
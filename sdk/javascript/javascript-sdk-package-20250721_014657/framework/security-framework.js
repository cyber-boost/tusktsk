/**
 * TuskLang Advanced Security and Authentication Framework
 * Provides comprehensive security features and authentication mechanisms
 */

const crypto = require('crypto');

class SecurityManager {
  constructor(options = {}) {
    this.options = {
      algorithm: options.algorithm || 'aes-256-gcm',
      keyLength: options.keyLength || 32,
      saltRounds: options.saltRounds || 12,
      sessionTimeout: options.sessionTimeout || 3600000, // 1 hour
      maxLoginAttempts: options.maxLoginAttempts || 5,
      lockoutDuration: options.lockoutDuration || 900000, // 15 minutes
      ...options
    };
    
    this.users = new Map();
    this.sessions = new Map();
    this.tokens = new Map();
    this.failedAttempts = new Map();
    this.blacklistedTokens = new Set();
    this.securityPolicies = new Map();
    this.auditLog = [];
  }

  /**
   * Register a new user
   */
  async registerUser(username, password, metadata = {}) {
    if (this.users.has(username)) {
      throw new Error('User already exists');
    }

    const salt = crypto.randomBytes(16).toString('hex');
    const hashedPassword = await this.hashPassword(password, salt);
    
    const user = {
      id: this.generateUserId(),
      username,
      passwordHash: hashedPassword,
      salt,
      metadata: {
        createdAt: new Date().toISOString(),
        lastLogin: null,
        isActive: true,
        ...metadata
      }
    };

    this.users.set(username, user);
    this.auditLog.push({
      action: 'user_registered',
      username,
      timestamp: new Date().toISOString(),
      ip: metadata.ip || 'unknown'
    });

    return { userId: user.id, username };
  }

  /**
   * Authenticate user
   */
  async authenticateUser(username, password, metadata = {}) {
    // Check for account lockout
    if (this.isAccountLocked(username)) {
      throw new Error('Account is temporarily locked due to too many failed attempts');
    }

    const user = this.users.get(username);
    if (!user || !user.metadata.isActive) {
      this.recordFailedAttempt(username);
      throw new Error('Invalid credentials');
    }

    const isValid = await this.verifyPassword(password, user.passwordHash, user.salt);
    if (!isValid) {
      this.recordFailedAttempt(username);
      throw new Error('Invalid credentials');
    }

    // Clear failed attempts on successful login
    this.failedAttempts.delete(username);

    // Update user metadata
    user.metadata.lastLogin = new Date().toISOString();

    // Create session
    const session = await this.createSession(user.id, metadata);
    
    this.auditLog.push({
      action: 'user_login',
      username,
      sessionId: session.id,
      timestamp: new Date().toISOString(),
      ip: metadata.ip || 'unknown'
    });

    return session;
  }

  /**
   * Create a new session
   */
  async createSession(userId, metadata = {}) {
    const sessionId = this.generateSessionId();
    const token = this.generateToken();
    
    const session = {
      id: sessionId,
      userId,
      token,
      createdAt: Date.now(),
      expiresAt: Date.now() + this.options.sessionTimeout,
      metadata: {
        ip: metadata.ip || 'unknown',
        userAgent: metadata.userAgent || 'unknown',
        ...metadata
      }
    };

    this.sessions.set(sessionId, session);
    this.tokens.set(token, sessionId);

    return {
      sessionId,
      token,
      expiresAt: session.expiresAt
    };
  }

  /**
   * Validate session token
   */
  validateSession(token) {
    const sessionId = this.tokens.get(token);
    if (!sessionId) {
      return null;
    }

    const session = this.sessions.get(sessionId);
    if (!session || session.expiresAt < Date.now()) {
      this.invalidateSession(sessionId);
      return null;
    }

    // Check if token is blacklisted
    if (this.blacklistedTokens.has(token)) {
      this.invalidateSession(sessionId);
      return null;
    }

    return session;
  }

  /**
   * Invalidate session
   */
  invalidateSession(sessionId) {
    const session = this.sessions.get(sessionId);
    if (session) {
      this.tokens.delete(session.token);
      this.blacklistedTokens.add(session.token);
      this.sessions.delete(sessionId);
    }
  }

  /**
   * Logout user
   */
  logout(token) {
    const session = this.validateSession(token);
    if (session) {
      this.invalidateSession(session.id);
      this.auditLog.push({
        action: 'user_logout',
        userId: session.userId,
        sessionId: session.id,
        timestamp: new Date().toISOString()
      });
      return true;
    }
    return false;
  }

  /**
   * Hash password with salt
   */
  async hashPassword(password, salt) {
    return new Promise((resolve, reject) => {
      crypto.pbkdf2(password, salt, this.options.saltRounds, this.options.keyLength, 'sha512', (err, derivedKey) => {
        if (err) reject(err);
        else resolve(derivedKey.toString('hex'));
      });
    });
  }

  /**
   * Verify password
   */
  async verifyPassword(password, hash, salt) {
    const computedHash = await this.hashPassword(password, salt);
    return crypto.timingSafeEqual(Buffer.from(hash, 'hex'), Buffer.from(computedHash, 'hex'));
  }

  /**
   * Generate secure token
   */
  generateToken() {
    return crypto.randomBytes(32).toString('hex');
  }

  /**
   * Generate session ID
   */
  generateSessionId() {
    return crypto.randomBytes(16).toString('hex');
  }

  /**
   * Generate user ID
   */
  generateUserId() {
    return crypto.randomBytes(8).toString('hex');
  }

  /**
   * Record failed login attempt
   */
  recordFailedAttempt(username) {
    const attempts = this.failedAttempts.get(username) || { count: 0, firstAttempt: Date.now() };
    attempts.count++;
    attempts.lastAttempt = Date.now();
    this.failedAttempts.set(username, attempts);
  }

  /**
   * Check if account is locked
   */
  isAccountLocked(username) {
    const attempts = this.failedAttempts.get(username);
    if (!attempts) return false;

    const timeSinceFirstAttempt = Date.now() - attempts.firstAttempt;
    const isLocked = attempts.count >= this.options.maxLoginAttempts && 
                     timeSinceFirstAttempt < this.options.lockoutDuration;

    // Auto-unlock after lockout duration
    if (!isLocked && timeSinceFirstAttempt >= this.options.lockoutDuration) {
      this.failedAttempts.delete(username);
    }

    return isLocked;
  }

  /**
   * Get user information
   */
  getUserInfo(username) {
    const user = this.users.get(username);
    if (!user) return null;

    return {
      id: user.id,
      username: user.username,
      metadata: { ...user.metadata },
      isLocked: this.isAccountLocked(username)
    };
  }

  /**
   * Get active sessions
   */
  getActiveSessions() {
    const activeSessions = [];
    for (const [sessionId, session] of this.sessions) {
      if (session.expiresAt > Date.now()) {
        activeSessions.push({
          sessionId,
          userId: session.userId,
          createdAt: session.createdAt,
          expiresAt: session.expiresAt,
          metadata: session.metadata
        });
      }
    }
    return activeSessions;
  }

  /**
   * Clean up expired sessions
   */
  cleanupExpiredSessions() {
    const now = Date.now();
    const expiredSessions = [];

    for (const [sessionId, session] of this.sessions) {
      if (session.expiresAt < now) {
        expiredSessions.push(sessionId);
      }
    }

    expiredSessions.forEach(sessionId => {
      this.invalidateSession(sessionId);
    });

    return expiredSessions.length;
  }

  /**
   * Get audit log
   */
  getAuditLog(limit = 100) {
    return this.auditLog.slice(-limit);
  }

  /**
   * Add security policy
   */
  addSecurityPolicy(name, policy) {
    this.securityPolicies.set(name, policy);
  }

  /**
   * Validate against security policies
   */
  validateSecurityPolicies(action, data) {
    const violations = [];
    
    for (const [name, policy] of this.securityPolicies) {
      if (policy.appliesTo.includes(action)) {
        const result = policy.validate(data);
        if (!result.valid) {
          violations.push({ policy: name, reason: result.reason });
        }
      }
    }
    
    return violations;
  }

  /**
   * Get security statistics
   */
  getSecurityStats() {
    return {
      totalUsers: this.users.size,
      activeSessions: this.getActiveSessions().length,
      totalSessions: this.sessions.size,
      blacklistedTokens: this.blacklistedTokens.size,
      failedAttempts: this.failedAttempts.size,
      auditLogEntries: this.auditLog.length,
      securityPolicies: this.securityPolicies.size
    };
  }
}

class EncryptionManager {
  constructor(options = {}) {
    this.options = {
      algorithm: options.algorithm || 'aes-256-gcm',
      keyLength: options.keyLength || 32,
      ...options
    };
  }

  /**
   * Encrypt data
   */
  encrypt(data, key) {
    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipher(this.options.algorithm, key);
    
    let encrypted = cipher.update(data, 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    return {
      encrypted,
      iv: iv.toString('hex'),
      algorithm: this.options.algorithm
    };
  }

  /**
   * Decrypt data
   */
  decrypt(encryptedData, key, iv) {
    const decipher = crypto.createDecipher(this.options.algorithm, key);
    
    let decrypted = decipher.update(encryptedData, 'hex', 'utf8');
    decrypted += decipher.final('utf8');
    
    return decrypted;
  }

  /**
   * Generate encryption key
   */
  generateKey() {
    return crypto.randomBytes(this.options.keyLength).toString('hex');
  }

  /**
   * Hash data
   */
  hash(data, algorithm = 'sha256') {
    return crypto.createHash(algorithm).update(data).digest('hex');
  }

  /**
   * Generate HMAC
   */
  generateHMAC(data, secret) {
    return crypto.createHmac('sha256', secret).update(data).digest('hex');
  }
}

class RateLimiter {
  constructor(options = {}) {
    this.options = {
      windowMs: options.windowMs || 60000, // 1 minute
      maxRequests: options.maxRequests || 100,
      ...options
    };
    
    this.requests = new Map();
  }

  /**
   * Check if request is allowed
   */
  isAllowed(identifier) {
    const now = Date.now();
    const windowStart = now - this.options.windowMs;
    
    let userRequests = this.requests.get(identifier) || [];
    
    // Remove old requests outside the window
    userRequests = userRequests.filter(timestamp => timestamp > windowStart);
    
    if (userRequests.length >= this.options.maxRequests) {
      return false;
    }
    
    // Add current request
    userRequests.push(now);
    this.requests.set(identifier, userRequests);
    
    return true;
  }

  /**
   * Get remaining requests for identifier
   */
  getRemainingRequests(identifier) {
    const now = Date.now();
    const windowStart = now - this.options.windowMs;
    
    const userRequests = this.requests.get(identifier) || [];
    const validRequests = userRequests.filter(timestamp => timestamp > windowStart);
    
    return Math.max(0, this.options.maxRequests - validRequests.length);
  }

  /**
   * Reset rate limit for identifier
   */
  reset(identifier) {
    this.requests.delete(identifier);
  }
}

module.exports = {
  SecurityManager,
  EncryptionManager,
  RateLimiter
}; 
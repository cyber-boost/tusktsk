/**
 * TuskLang Advanced Security and Authentication System
 * Provides comprehensive security, authentication, and authorization capabilities
 */

const crypto = require('crypto');
const { EventEmitter } = require('events');

class SecurityManager {
  constructor(options = {}) {
    this.options = {
      algorithm: options.algorithm || 'aes-256-gcm',
      keyLength: options.keyLength || 32,
      saltLength: options.saltLength || 16,
      iterations: options.iterations || 100000,
      sessionTimeout: options.sessionTimeout || 3600000, // 1 hour
      maxLoginAttempts: options.maxLoginAttempts || 5,
      lockoutDuration: options.lockoutDuration || 900000, // 15 minutes
      ...options
    };
    
    this.users = new Map();
    this.sessions = new Map();
    this.tokens = new Map();
    this.failedAttempts = new Map();
    this.blacklistedIPs = new Set();
    this.eventEmitter = new EventEmitter();
    this.isRunning = false;
  }

  /**
   * Register a new user
   */
  async registerUser(username, password, userData = {}) {
    if (this.users.has(username)) {
      throw new Error('User already exists');
    }

    const salt = crypto.randomBytes(this.options.saltLength);
    const hash = await this.hashPassword(password, salt);
    
    const user = {
      username,
      passwordHash: hash,
      salt: salt.toString('hex'),
      userData,
      createdAt: Date.now(),
      lastLogin: null,
      isActive: true,
      permissions: userData.permissions || ['user'],
      metadata: {
        loginCount: 0,
        failedAttempts: 0,
        lastPasswordChange: Date.now()
      }
    };

    this.users.set(username, user);
    this.eventEmitter.emit('userRegistered', { username, userData });
    
    return { success: true, username };
  }

  /**
   * Authenticate user
   */
  async authenticateUser(username, password, ipAddress = null) {
    const user = this.users.get(username);
    if (!user || !user.isActive) {
      throw new Error('Invalid credentials');
    }

    // Check for account lockout
    if (this.isAccountLocked(username)) {
      throw new Error('Account temporarily locked due to too many failed attempts');
    }

    // Check IP blacklist
    if (ipAddress && this.blacklistedIPs.has(ipAddress)) {
      throw new Error('Access denied from this IP address');
    }

    // Verify password
    const salt = Buffer.from(user.salt, 'hex');
    const hash = await this.hashPassword(password, salt);
    
    if (hash !== user.passwordHash) {
      this.recordFailedAttempt(username, ipAddress);
      throw new Error('Invalid credentials');
    }

    // Reset failed attempts on successful login
    this.failedAttempts.delete(username);
    
    // Update user metadata
    user.lastLogin = Date.now();
    user.metadata.loginCount++;
    
    // Create session
    const session = this.createSession(username);
    
    this.eventEmitter.emit('userAuthenticated', { username, sessionId: session.id });
    
    return {
      success: true,
      sessionId: session.id,
      user: {
        username: user.username,
        permissions: user.permissions,
        userData: user.userData
      }
    };
  }

  /**
   * Hash password with salt
   */
  async hashPassword(password, salt) {
    return new Promise((resolve, reject) => {
      crypto.pbkdf2(password, salt, this.options.iterations, this.options.keyLength, 'sha512', (err, hash) => {
        if (err) reject(err);
        else resolve(hash.toString('hex'));
      });
    });
  }

  /**
   * Create a new session
   */
  createSession(username) {
    const sessionId = crypto.randomBytes(32).toString('hex');
    const session = {
      id: sessionId,
      username,
      createdAt: Date.now(),
      expiresAt: Date.now() + this.options.sessionTimeout,
      ipAddress: null,
      userAgent: null,
      isActive: true
    };

    this.sessions.set(sessionId, session);
    return session;
  }

  /**
   * Validate session
   */
  validateSession(sessionId) {
    const session = this.sessions.get(sessionId);
    
    if (!session || !session.isActive) {
      return null;
    }

    if (Date.now() > session.expiresAt) {
      this.sessions.delete(sessionId);
      return null;
    }

    // Extend session
    session.expiresAt = Date.now() + this.options.sessionTimeout;
    
    return session;
  }

  /**
   * Record failed login attempt
   */
  recordFailedAttempt(username, ipAddress = null) {
    if (!this.failedAttempts.has(username)) {
      this.failedAttempts.set(username, []);
    }

    const attempts = this.failedAttempts.get(username);
    attempts.push({
      timestamp: Date.now(),
      ipAddress
    });

    // Keep only recent attempts
    const cutoff = Date.now() - this.options.lockoutDuration;
    const recentAttempts = attempts.filter(attempt => attempt.timestamp > cutoff);
    this.failedAttempts.set(username, recentAttempts);

    // Update user metadata
    const user = this.users.get(username);
    if (user) {
      user.metadata.failedAttempts = recentAttempts.length;
    }

    this.eventEmitter.emit('failedLoginAttempt', { username, ipAddress, attemptCount: recentAttempts.length });
  }

  /**
   * Check if account is locked
   */
  isAccountLocked(username) {
    const attempts = this.failedAttempts.get(username) || [];
    const recentAttempts = attempts.filter(attempt => 
      attempt.timestamp > Date.now() - this.options.lockoutDuration
    );

    return recentAttempts.length >= this.options.maxLoginAttempts;
  }

  /**
   * Logout user
   */
  logoutUser(sessionId) {
    const session = this.sessions.get(sessionId);
    if (session) {
      session.isActive = false;
      this.sessions.delete(sessionId);
      this.eventEmitter.emit('userLoggedOut', { username: session.username, sessionId });
    }
    return true;
  }

  /**
   * Change password
   */
  async changePassword(username, currentPassword, newPassword) {
    const user = this.users.get(username);
    if (!user) {
      throw new Error('User not found');
    }

    // Verify current password
    const salt = Buffer.from(user.salt, 'hex');
    const currentHash = await this.hashPassword(currentPassword, salt);
    
    if (currentHash !== user.passwordHash) {
      throw new Error('Current password is incorrect');
    }

    // Hash new password
    const newSalt = crypto.randomBytes(this.options.saltLength);
    const newHash = await this.hashPassword(newPassword, newSalt);

    // Update user
    user.passwordHash = newHash;
    user.salt = newSalt.toString('hex');
    user.metadata.lastPasswordChange = Date.now();

    // Invalidate all sessions for this user
    for (const [sessionId, session] of this.sessions) {
      if (session.username === username) {
        this.sessions.delete(sessionId);
      }
    }

    this.eventEmitter.emit('passwordChanged', { username });
    return { success: true };
  }

  /**
   * Generate JWT token
   */
  generateToken(payload, secret, options = {}) {
    const header = {
      alg: 'HS256',
      typ: 'JWT'
    };

    const now = Math.floor(Date.now() / 1000);
    const tokenPayload = {
      ...payload,
      iat: now,
      exp: now + (options.expiresIn || 3600)
    };

    const encodedHeader = Buffer.from(JSON.stringify(header)).toString('base64url');
    const encodedPayload = Buffer.from(JSON.stringify(tokenPayload)).toString('base64url');
    
    const signature = crypto
      .createHmac('sha256', secret)
      .update(`${encodedHeader}.${encodedPayload}`)
      .digest('base64url');

    const token = `${encodedHeader}.${encodedPayload}.${signature}`;
    
    this.tokens.set(token, {
      payload: tokenPayload,
      createdAt: Date.now(),
      expiresAt: tokenPayload.exp * 1000
    });

    return token;
  }

  /**
   * Verify JWT token
   */
  verifyToken(token, secret) {
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new Error('Invalid token format');
    }

    const [encodedHeader, encodedPayload, signature] = parts;
    
    // Verify signature
    const expectedSignature = crypto
      .createHmac('sha256', secret)
      .update(`${encodedHeader}.${encodedPayload}`)
      .digest('base64url');

    if (signature !== expectedSignature) {
      throw new Error('Invalid token signature');
    }

    // Decode payload
    const payload = JSON.parse(Buffer.from(encodedPayload, 'base64url').toString());
    
    // Check expiration
    if (payload.exp && payload.exp < Math.floor(Date.now() / 1000)) {
      throw new Error('Token expired');
    }

    return payload;
  }

  /**
   * Blacklist IP address
   */
  blacklistIP(ipAddress, reason = 'Security violation') {
    this.blacklistedIPs.add(ipAddress);
    this.eventEmitter.emit('ipBlacklisted', { ipAddress, reason });
  }

  /**
   * Remove IP from blacklist
   */
  removeFromBlacklist(ipAddress) {
    this.blacklistedIPs.delete(ipAddress);
    this.eventEmitter.emit('ipRemovedFromBlacklist', { ipAddress });
  }

  /**
   * Get security statistics
   */
  getSecurityStats() {
    return {
      totalUsers: this.users.size,
      activeSessions: Array.from(this.sessions.values()).filter(s => s.isActive).length,
      activeTokens: this.tokens.size,
      blacklistedIPs: this.blacklistedIPs.size,
      lockedAccounts: Array.from(this.failedAttempts.entries())
        .filter(([_, attempts]) => attempts.length >= this.options.maxLoginAttempts).length
    };
  }

  /**
   * Cleanup expired sessions and tokens
   */
  cleanup() {
    const now = Date.now();
    let cleanedSessions = 0;
    let cleanedTokens = 0;

    // Cleanup expired sessions
    for (const [sessionId, session] of this.sessions) {
      if (now > session.expiresAt) {
        this.sessions.delete(sessionId);
        cleanedSessions++;
      }
    }

    // Cleanup expired tokens
    for (const [token, tokenData] of this.tokens) {
      if (now > tokenData.expiresAt) {
        this.tokens.delete(token);
        cleanedTokens++;
      }
    }

    if (cleanedSessions > 0 || cleanedTokens > 0) {
      this.eventEmitter.emit('cleanup', { cleanedSessions, cleanedTokens });
    }

    return { cleanedSessions, cleanedTokens };
  }
}

class AuthorizationManager {
  constructor() {
    this.roles = new Map();
    this.permissions = new Map();
    this.policies = new Map();
  }

  /**
   * Define a role
   */
  defineRole(roleName, permissions = []) {
    this.roles.set(roleName, {
      name: roleName,
      permissions: new Set(permissions),
      createdAt: Date.now()
    });
  }

  /**
   * Define a permission
   */
  definePermission(permissionName, description = '') {
    this.permissions.set(permissionName, {
      name: permissionName,
      description,
      createdAt: Date.now()
    });
  }

  /**
   * Check if user has permission
   */
  hasPermission(user, permission) {
    if (!user || !user.permissions) {
      return false;
    }

    // Check direct permissions
    if (user.permissions.includes(permission)) {
      return true;
    }

    // Check role-based permissions
    for (const roleName of user.permissions) {
      const role = this.roles.get(roleName);
      if (role && role.permissions.has(permission)) {
        return true;
      }
    }

    return false;
  }

  /**
   * Check if user has any of the permissions
   */
  hasAnyPermission(user, permissions) {
    return permissions.some(permission => this.hasPermission(user, permission));
  }

  /**
   * Check if user has all permissions
   */
  hasAllPermissions(user, permissions) {
    return permissions.every(permission => this.hasPermission(user, permission));
  }

  /**
   * Create access control policy
   */
  createPolicy(policyName, conditions) {
    this.policies.set(policyName, {
      name: policyName,
      conditions,
      createdAt: Date.now()
    });
  }

  /**
   * Evaluate policy
   */
  evaluatePolicy(policyName, context) {
    const policy = this.policies.get(policyName);
    if (!policy) {
      return false;
    }

    // Simple policy evaluation (can be extended)
    for (const [key, value] of Object.entries(policy.conditions)) {
      if (context[key] !== value) {
        return false;
      }
    }

    return true;
  }
}

class EncryptionManager {
  constructor(options = {}) {
    this.options = {
      algorithm: options.algorithm || 'aes-256-gcm',
      keyLength: options.keyLength || 32,
      ...options
    };
    
    this.keys = new Map();
  }

  /**
   * Generate encryption key
   */
  generateKey(keyId = null) {
    const key = crypto.randomBytes(this.options.keyLength);
    const id = keyId || crypto.randomBytes(16).toString('hex');
    
    this.keys.set(id, {
      key,
      createdAt: Date.now(),
      algorithm: this.options.algorithm
    });

    return id;
  }

  /**
   * Encrypt data
   */
  encrypt(data, keyId) {
    const keyData = this.keys.get(keyId);
    if (!keyData) {
      throw new Error('Key not found');
    }

    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipher(this.options.algorithm, keyData.key);
    
    let encrypted = cipher.update(data, 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    const authTag = cipher.getAuthTag();

    return {
      encrypted,
      iv: iv.toString('hex'),
      authTag: authTag.toString('hex'),
      algorithm: this.options.algorithm
    };
  }

  /**
   * Decrypt data
   */
  decrypt(encryptedData, keyId) {
    const keyData = this.keys.get(keyId);
    if (!keyData) {
      throw new Error('Key not found');
    }

    const decipher = crypto.createDecipher(this.options.algorithm, keyData.key);
    decipher.setAuthTag(Buffer.from(encryptedData.authTag, 'hex'));
    
    let decrypted = decipher.update(encryptedData.encrypted, 'hex', 'utf8');
    decrypted += decipher.final('utf8');
    
    return decrypted;
  }

  /**
   * Hash data
   */
  hash(data, algorithm = 'sha256') {
    return crypto.createHash(algorithm).update(data).digest('hex');
  }

  /**
   * Generate random string
   */
  generateRandomString(length = 32) {
    return crypto.randomBytes(length).toString('hex');
  }
}

module.exports = {
  SecurityManager,
  AuthorizationManager,
  EncryptionManager
}; 
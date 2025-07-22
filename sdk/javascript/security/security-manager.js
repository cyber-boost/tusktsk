/**
 * TuskLang Enhanced Security Manager
 * ==================================
 * Enterprise-grade security with advanced encryption, threat detection, and audit logging
 */

const crypto = require('crypto');
const bcrypt = require('bcryptjs');

class SecurityManager {
    constructor(options = {}) {
        this.options = {
            algorithm: options.algorithm || 'aes-256-gcm',
            keyLength: options.keyLength || 32,
            saltRounds: options.saltRounds || 12,
            sessionTimeout: options.sessionTimeout || 3600000, // 1 hour
            maxLoginAttempts: options.maxLoginAttempts || 5,
            lockoutDuration: options.lockoutDuration || 900000, // 15 minutes
            threatDetectionEnabled: options.threatDetectionEnabled !== false,
            auditLogEnabled: options.auditLogEnabled !== false,
            encryptionEnabled: options.encryptionEnabled !== false,
            ...options
        };
        
        this.users = new Map();
        this.sessions = new Map();
        this.tokens = new Map();
        this.failedAttempts = new Map();
        this.blacklistedTokens = new Set();
        this.securityPolicies = new Map();
        this.auditLog = [];
        this.threats = [];
        this.encryptionKeys = new Map();
        
        this.initializeSecurityPolicies();
        this.startSecurityMonitoring();
    }

    /**
     * Initialize default security policies
     */
    initializeSecurityPolicies() {
        // Password policy
        this.addSecurityPolicy('password', {
            name: 'Password Policy',
            appliesTo: ['register', 'change_password'],
            validate: (data) => {
                const password = data.password;
                if (!password || typeof password !== 'string') {
                    return { valid: false, reason: 'Password is required' };
                }
                
                if (password.length < 8) {
                    return { valid: false, reason: 'Password must be at least 8 characters' };
                }
                
                if (!/(?=.*[a-z])/.test(password)) {
                    return { valid: false, reason: 'Password must contain at least one lowercase letter' };
                }
                
                if (!/(?=.*[A-Z])/.test(password)) {
                    return { valid: false, reason: 'Password must contain at least one uppercase letter' };
                }
                
                if (!/(?=.*\d)/.test(password)) {
                    return { valid: false, reason: 'Password must contain at least one number' };
                }
                
                if (!/(?=.*[@$!%*?&])/.test(password)) {
                    return { valid: false, reason: 'Password must contain at least one special character' };
                }
                
                return { valid: true };
            }
        });

        // Username policy
        this.addSecurityPolicy('username', {
            name: 'Username Policy',
            appliesTo: ['register'],
            validate: (data) => {
                const username = data.username;
                if (!username || typeof username !== 'string') {
                    return { valid: false, reason: 'Username is required' };
                }
                
                if (username.length < 3 || username.length > 50) {
                    return { valid: false, reason: 'Username must be between 3 and 50 characters' };
                }
                
                if (!/^[a-zA-Z0-9_-]+$/.test(username)) {
                    return { valid: false, reason: 'Username can only contain letters, numbers, underscores, and hyphens' };
                }
                
                return { valid: true };
            }
        });

        // Rate limiting policy
        this.addSecurityPolicy('rate_limit', {
            name: 'Rate Limiting Policy',
            appliesTo: ['login', 'register'],
            validate: (data) => {
                const ip = data.ip;
                const attempts = this.failedAttempts.get(ip) || { count: 0, firstAttempt: Date.now() };
                
                if (attempts.count >= this.options.maxLoginAttempts) {
                    const timeSinceFirstAttempt = Date.now() - attempts.firstAttempt;
                    if (timeSinceFirstAttempt < this.options.lockoutDuration) {
                        return { valid: false, reason: 'Too many failed attempts, please try again later' };
                    }
                }
                
                return { valid: true };
            }
        });
    }

    /**
     * Start security monitoring
     */
    startSecurityMonitoring() {
        if (this.options.threatDetectionEnabled) {
            // Clean up expired sessions every 5 minutes
            setInterval(() => {
                this.cleanupExpiredSessions();
            }, 5 * 60 * 1000);

            // Clean up old audit logs every hour
            setInterval(() => {
                this.cleanupAuditLog();
            }, 60 * 60 * 1000);

            // Monitor for threats every 30 seconds
            setInterval(() => {
                this.detectThreats();
            }, 30 * 1000);
        }
    }

    /**
     * Register a new user with enhanced security
     */
    async registerUser(username, password, metadata = {}) {
        try {
            // Validate against security policies
            const violations = this.validateSecurityPolicies('register', { username, password, ...metadata });
            if (violations.length > 0) {
                throw new Error(`Security policy violations: ${violations.map(v => v.reason).join(', ')}`);
            }

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
                    failedAttempts: 0,
                    lastFailedAttempt: null,
                    ...metadata
                }
            };

            this.users.set(username, user);
            
            // Log security event
            this.logSecurityEvent('user_registered', {
                username,
                userId: user.id,
                ip: metadata.ip || 'unknown',
                userAgent: metadata.userAgent || 'unknown'
            });

            return { userId: user.id, username };
        } catch (error) {
            this.logSecurityEvent('user_registration_failed', {
                username,
                reason: error.message,
                ip: metadata.ip || 'unknown'
            });
            throw error;
        }
    }

    /**
     * Authenticate user with enhanced security
     */
    async authenticateUser(username, password, metadata = {}) {
        try {
            // Check for account lockout
            if (this.isAccountLocked(username)) {
                this.logSecurityEvent('login_blocked', {
                    username,
                    reason: 'Account locked',
                    ip: metadata.ip || 'unknown'
                });
                throw new Error('Account is temporarily locked due to too many failed attempts');
            }

            const user = this.users.get(username);
            if (!user || !user.metadata.isActive) {
                this.recordFailedAttempt(username, metadata);
                throw new Error('Invalid credentials');
            }

            const isValid = await this.verifyPassword(password, user.passwordHash, user.salt);
            if (!isValid) {
                this.recordFailedAttempt(username, metadata);
                throw new Error('Invalid credentials');
            }

            // Clear failed attempts on successful login
            this.failedAttempts.delete(username);
            user.metadata.failedAttempts = 0;
            user.metadata.lastFailedAttempt = null;

            // Update user metadata
            user.metadata.lastLogin = new Date().toISOString();

            // Create session
            const session = await this.createSession(user.id, metadata);
            
            this.logSecurityEvent('user_login', {
                username,
                userId: user.id,
                sessionId: session.id,
                ip: metadata.ip || 'unknown',
                userAgent: metadata.userAgent || 'unknown'
            });

            return session;
        } catch (error) {
            this.logSecurityEvent('login_failed', {
                username,
                reason: error.message,
                ip: metadata.ip || 'unknown'
            });
            throw error;
        }
    }

    /**
     * Create a new session with enhanced security
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
     * Validate session token with enhanced security
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

        // Check for suspicious activity
        if (this.isSuspiciousActivity(session)) {
            this.logSecurityEvent('suspicious_activity', {
                sessionId,
                userId: session.userId,
                reason: 'Suspicious session activity detected'
            });
        }

        return session;
    }

    /**
     * Invalidate session with enhanced security
     */
    invalidateSession(sessionId) {
        const session = this.sessions.get(sessionId);
        if (session) {
            this.tokens.delete(session.token);
            this.blacklistedTokens.add(session.token);
            this.sessions.delete(sessionId);
            
            this.logSecurityEvent('session_invalidated', {
                sessionId,
                userId: session.userId,
                reason: 'Session expired or invalidated'
            });
        }
    }

    /**
     * Logout user with enhanced security
     */
    logout(token) {
        const session = this.validateSession(token);
        if (session) {
            this.invalidateSession(session.id);
            this.logSecurityEvent('user_logout', {
                userId: session.userId,
                sessionId: session.id
            });
            return true;
        }
        return false;
    }

    /**
     * Hash password with enhanced security
     */
    async hashPassword(password, salt) {
        return new Promise((resolve, reject) => {
            bcrypt.hash(password, this.options.saltRounds, (err, hash) => {
                if (err) reject(err);
                else resolve(hash);
            });
        });
    }

    /**
     * Verify password with enhanced security
     */
    async verifyPassword(password, hash, salt) {
        return new Promise((resolve, reject) => {
            bcrypt.compare(password, hash, (err, result) => {
                if (err) reject(err);
                else resolve(result);
            });
        });
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
     * Record failed login attempt with enhanced tracking
     */
    recordFailedAttempt(username, metadata = {}) {
        const attempts = this.failedAttempts.get(username) || { count: 0, firstAttempt: Date.now() };
        attempts.count++;
        attempts.lastAttempt = Date.now();
        attempts.ip = metadata.ip || 'unknown';
        this.failedAttempts.set(username, attempts);

        // Update user metadata
        const user = this.users.get(username);
        if (user) {
            user.metadata.failedAttempts = attempts.count;
            user.metadata.lastFailedAttempt = new Date().toISOString();
        }

        this.logSecurityEvent('failed_login_attempt', {
            username,
            attemptCount: attempts.count,
            ip: metadata.ip || 'unknown',
            userAgent: metadata.userAgent || 'unknown'
        });
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
     * Log security event with enhanced logging
     */
    logSecurityEvent(action, data) {
        if (!this.options.auditLogEnabled) return;

        const event = {
            id: this.generateEventId(),
            action,
            timestamp: new Date().toISOString(),
            data,
            severity: this.getEventSeverity(action)
        };

        this.auditLog.push(event);

        // Keep only last 10000 events
        if (this.auditLog.length > 10000) {
            this.auditLog = this.auditLog.slice(-10000);
        }

        // Log to console in development
        if (process.env.NODE_ENV === 'development') {
            console.log('Security Event:', event);
        }
    }

    /**
     * Generate event ID
     */
    generateEventId() {
        return 'evt_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    /**
     * Get event severity
     */
    getEventSeverity(action) {
        const highSeverity = ['login_failed', 'suspicious_activity', 'security_violation'];
        const mediumSeverity = ['failed_login_attempt', 'account_locked', 'session_invalidated'];
        
        if (highSeverity.includes(action)) return 'high';
        if (mediumSeverity.includes(action)) return 'medium';
        return 'low';
    }

    /**
     * Get audit log
     */
    getAuditLog(limit = 100, severity = null) {
        let events = this.auditLog;
        
        if (severity) {
            events = events.filter(event => event.severity === severity);
        }
        
        return events.slice(-limit);
    }

    /**
     * Clean up old audit logs
     */
    cleanupAuditLog() {
        const oneWeekAgo = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
        this.auditLog = this.auditLog.filter(event => 
            new Date(event.timestamp) > oneWeekAgo
        );
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
     * Detect threats
     */
    detectThreats() {
        const threats = [];
        
        // Check for multiple failed attempts from same IP
        const ipAttempts = new Map();
        for (const [username, attempts] of this.failedAttempts) {
            const ip = attempts.ip;
            if (ip && ip !== 'unknown') {
                ipAttempts.set(ip, (ipAttempts.get(ip) || 0) + attempts.count);
            }
        }
        
        for (const [ip, count] of ipAttempts) {
            if (count > 20) {
                threats.push({
                    type: 'brute_force_attempt',
                    ip,
                    count,
                    timestamp: new Date().toISOString()
                });
            }
        }
        
        // Check for suspicious session activity
        for (const [sessionId, session] of this.sessions) {
            if (this.isSuspiciousActivity(session)) {
                threats.push({
                    type: 'suspicious_session',
                    sessionId,
                    userId: session.userId,
                    timestamp: new Date().toISOString()
                });
            }
        }
        
        this.threats.push(...threats);
        
        // Keep only last 100 threats
        if (this.threats.length > 100) {
            this.threats = this.threats.slice(-100);
        }
        
        return threats;
    }

    /**
     * Check for suspicious activity
     */
    isSuspiciousActivity(session) {
        // Check for rapid session creation
        const recentSessions = Array.from(this.sessions.values())
            .filter(s => s.userId === session.userId)
            .filter(s => Date.now() - s.createdAt < 60000); // Last minute
        
        return recentSessions.length > 5;
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
            securityPolicies: this.securityPolicies.size,
            threats: this.threats.length,
            lockedAccounts: Array.from(this.users.values())
                .filter(user => this.isAccountLocked(user.username)).length
        };
    }

    /**
     * Get threats
     */
    getThreats(limit = 10) {
        return this.threats.slice(-limit);
    }

    /**
     * Clear security data (for testing)
     */
    clear() {
        this.users.clear();
        this.sessions.clear();
        this.tokens.clear();
        this.failedAttempts.clear();
        this.blacklistedTokens.clear();
        this.auditLog = [];
        this.threats = [];
    }
}

module.exports = SecurityManager; 
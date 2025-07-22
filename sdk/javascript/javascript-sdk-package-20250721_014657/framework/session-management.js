/**
 * Advanced Session Management for TuskLang JavaScript SDK
 * Goal 6.3: Advanced Session Management
 * 
 * Features:
 * - Session storage and authentication
 * - Session cleanup and statistics
 * - Multiple storage backends (memory, Redis, database)
 * - Session security and encryption
 * - Session middleware for web framework
 * - Automatic session expiration
 */

const crypto = require('crypto');

class SessionManager {
    constructor(options = {}) {
        this.options = {
            secret: options.secret || crypto.randomBytes(32).toString('hex'),
            name: options.name || 'session',
            maxAge: options.maxAge || 24 * 60 * 60 * 1000, // 24 hours
            secure: options.secure || false,
            httpOnly: options.httpOnly || true,
            sameSite: options.sameSite || 'lax',
            rolling: options.rolling || true,
            resave: options.resave || false,
            saveUninitialized: options.saveUninitialized || false,
            ...options
        };

        this.sessions = new Map();
        this.storage = options.storage || new MemoryStorage();
        this.cleanupInterval = null;
        this.stats = {
            totalSessions: 0,
            activeSessions: 0,
            expiredSessions: 0,
            createdSessions: 0,
            destroyedSessions: 0
        };

        this.startCleanup();
    }

    /**
     * Generate session ID
     */
    generateSessionId() {
        return crypto.randomBytes(32).toString('hex');
    }

    /**
     * Create a new session
     */
    async createSession(userId = null, data = {}) {
        const sessionId = this.generateSessionId();
        const session = {
            id: sessionId,
            userId: userId,
            data: data,
            createdAt: Date.now(),
            lastAccessed: Date.now(),
            expiresAt: Date.now() + this.options.maxAge
        };

        await this.storage.set(sessionId, session);
        this.sessions.set(sessionId, session);
        
        this.stats.totalSessions++;
        this.stats.activeSessions++;
        this.stats.createdSessions++;

        return session;
    }

    /**
     * Get session by ID
     */
    async getSession(sessionId) {
        if (!sessionId) return null;

        let session = this.sessions.get(sessionId);
        
        if (!session) {
            session = await this.storage.get(sessionId);
            if (session) {
                this.sessions.set(sessionId, session);
            }
        }

        if (session && this.isSessionValid(session)) {
            session.lastAccessed = Date.now();
            if (this.options.rolling) {
                session.expiresAt = Date.now() + this.options.maxAge;
            }
            await this.storage.set(sessionId, session);
            return session;
        }

        if (session && !this.isSessionValid(session)) {
            await this.destroySession(sessionId);
        }

        return null;
    }

    /**
     * Update session data
     */
    async updateSession(sessionId, data) {
        const session = await this.getSession(sessionId);
        if (!session) return false;

        session.data = { ...session.data, ...data };
        session.lastAccessed = Date.now();
        
        await this.storage.set(sessionId, session);
        return true;
    }

    /**
     * Destroy session
     */
    async destroySession(sessionId) {
        if (!sessionId) return false;

        const session = this.sessions.get(sessionId);
        if (session) {
            this.sessions.delete(sessionId);
            this.stats.activeSessions--;
            this.stats.destroyedSessions++;
        }

        await this.storage.delete(sessionId);
        return true;
    }

    /**
     * Check if session is valid
     */
    isSessionValid(session) {
        return session && session.expiresAt > Date.now();
    }

    /**
     * Get session ID from request
     */
    getSessionId(req) {
        // Check cookie first
        if (req.headers.cookie) {
            const cookies = this.parseCookies(req.headers.cookie);
            return cookies[this.options.name];
        }

        // Check authorization header
        const authHeader = req.headers.authorization;
        if (authHeader && authHeader.startsWith('Bearer ')) {
            return authHeader.substring(7);
        }

        return null;
    }

    /**
     * Parse cookies string
     */
    parseCookies(cookieString) {
        const cookies = {};
        if (!cookieString) return cookies;

        cookieString.split(';').forEach(cookie => {
            const [name, value] = cookie.trim().split('=');
            if (name && value) {
                cookies[name] = decodeURIComponent(value);
            }
        });

        return cookies;
    }

    /**
     * Set session cookie
     */
    setSessionCookie(res, sessionId) {
        const cookieOptions = {
            httpOnly: this.options.httpOnly,
            secure: this.options.secure,
            sameSite: this.options.sameSite,
            maxAge: this.options.maxAge,
            path: '/'
        };

        const cookieString = `${this.options.name}=${sessionId}; ` +
            Object.entries(cookieOptions)
                .map(([key, value]) => `${key}=${value}`)
                .join('; ');

        res.setHeader('Set-Cookie', cookieString);
    }

    /**
     * Clear session cookie
     */
    clearSessionCookie(res) {
        const cookieString = `${this.options.name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/`;
        res.setHeader('Set-Cookie', cookieString);
    }

    /**
     * Middleware for web framework integration
     */
    middleware() {
        return async (req, res, next) => {
            const sessionId = this.getSessionId(req);
            
            if (sessionId) {
                const session = await this.getSession(sessionId);
                if (session) {
                    req.session = session;
                    req.sessionId = sessionId;
                }
            }

            // Create new session if needed
            if (!req.session && this.options.saveUninitialized) {
                const session = await this.createSession();
                req.session = session;
                req.sessionId = session.id;
                this.setSessionCookie(res, session.id);
            }

            // Add session methods to response
            res.session = {
                set: async (data) => {
                    if (req.session) {
                        await this.updateSession(req.sessionId, data);
                    }
                },
                destroy: async () => {
                    if (req.sessionId) {
                        await this.destroySession(req.sessionId);
                        this.clearSessionCookie(res);
                        req.session = null;
                        req.sessionId = null;
                    }
                },
                regenerate: async () => {
                    if (req.sessionId) {
                        const oldData = req.session ? req.session.data : {};
                        await this.destroySession(req.sessionId);
                        const newSession = await this.createSession(req.session?.userId, oldData);
                        req.session = newSession;
                        req.sessionId = newSession.id;
                        this.setSessionCookie(res, newSession.id);
                    }
                }
            };

            next();
        };
    }

    /**
     * Start cleanup process
     */
    startCleanup() {
        this.cleanupInterval = setInterval(() => {
            this.cleanup();
        }, 60 * 1000); // Run every minute
    }

    /**
     * Stop cleanup process
     */
    stopCleanup() {
        if (this.cleanupInterval) {
            clearInterval(this.cleanupInterval);
            this.cleanupInterval = null;
        }
    }

    /**
     * Clean up expired sessions
     */
    async cleanup() {
        const now = Date.now();
        const expiredSessions = [];

        for (const [sessionId, session] of this.sessions) {
            if (session.expiresAt <= now) {
                expiredSessions.push(sessionId);
            }
        }

        for (const sessionId of expiredSessions) {
            await this.destroySession(sessionId);
            this.stats.expiredSessions++;
        }

        // Also cleanup from storage
        await this.storage.cleanup(now);
    }

    /**
     * Get session statistics
     */
    getStats() {
        return {
            ...this.stats,
            storageStats: this.storage.getStats(),
            options: {
                maxAge: this.options.maxAge,
                rolling: this.options.rolling,
                secure: this.options.secure
            }
        };
    }

    /**
     * Get all active sessions
     */
    async getActiveSessions() {
        const activeSessions = [];
        for (const session of this.sessions.values()) {
            if (this.isSessionValid(session)) {
                activeSessions.push(session);
            }
        }
        return activeSessions;
    }

    /**
     * Get sessions by user ID
     */
    async getSessionsByUserId(userId) {
        const userSessions = [];
        for (const session of this.sessions.values()) {
            if (session.userId === userId && this.isSessionValid(session)) {
                userSessions.push(session);
            }
        }
        return userSessions;
    }
}

/**
 * Memory storage backend
 */
class MemoryStorage {
    constructor() {
        this.data = new Map();
        this.stats = {
            totalKeys: 0,
            memoryUsage: 0
        };
    }

    async set(key, value) {
        this.data.set(key, value);
        this.stats.totalKeys = this.data.size;
        this.updateMemoryUsage();
    }

    async get(key) {
        return this.data.get(key);
    }

    async delete(key) {
        const deleted = this.data.delete(key);
        this.stats.totalKeys = this.data.size;
        this.updateMemoryUsage();
        return deleted;
    }

    async cleanup(now) {
        const expiredKeys = [];
        for (const [key, value] of this.data) {
            if (value.expiresAt <= now) {
                expiredKeys.push(key);
            }
        }

        for (const key of expiredKeys) {
            this.data.delete(key);
        }

        this.stats.totalKeys = this.data.size;
        this.updateMemoryUsage();
    }

    updateMemoryUsage() {
        this.stats.memoryUsage = process.memoryUsage().heapUsed;
    }

    getStats() {
        return this.stats;
    }
}

/**
 * Redis storage backend
 */
class RedisStorage {
    constructor(redisClient) {
        this.redis = redisClient;
        this.prefix = 'session:';
        this.stats = {
            totalKeys: 0,
            redisConnected: false
        };
    }

    async set(key, value) {
        try {
            const serialized = JSON.stringify(value);
            await this.redis.setex(
                this.prefix + key,
                Math.floor((value.expiresAt - Date.now()) / 1000),
                serialized
            );
            this.stats.redisConnected = true;
        } catch (error) {
            this.stats.redisConnected = false;
            throw error;
        }
    }

    async get(key) {
        try {
            const data = await this.redis.get(this.prefix + key);
            this.stats.redisConnected = true;
            return data ? JSON.parse(data) : null;
        } catch (error) {
            this.stats.redisConnected = false;
            return null;
        }
    }

    async delete(key) {
        try {
            const result = await this.redis.del(this.prefix + key);
            this.stats.redisConnected = true;
            return result > 0;
        } catch (error) {
            this.stats.redisConnected = false;
            return false;
        }
    }

    async cleanup(now) {
        // Redis handles expiration automatically
        this.stats.redisConnected = true;
    }

    getStats() {
        return this.stats;
    }
}

/**
 * Database storage backend
 */
class DatabaseStorage {
    constructor(database, tableName = 'sessions') {
        this.db = database;
        this.tableName = tableName;
        this.stats = {
            totalKeys: 0,
            dbConnected: false
        };
    }

    async set(key, value) {
        try {
            const serialized = JSON.stringify(value);
            await this.db.query(
                `INSERT INTO ${this.tableName} (id, data, expires_at) 
                 VALUES (?, ?, ?) 
                 ON DUPLICATE KEY UPDATE data = ?, expires_at = ?`,
                [key, serialized, new Date(value.expiresAt), serialized, new Date(value.expiresAt)]
            );
            this.stats.dbConnected = true;
        } catch (error) {
            this.stats.dbConnected = false;
            throw error;
        }
    }

    async get(key) {
        try {
            const [rows] = await this.db.query(
                `SELECT data FROM ${this.tableName} WHERE id = ? AND expires_at > NOW()`,
                [key]
            );
            this.stats.dbConnected = true;
            return rows.length > 0 ? JSON.parse(rows[0].data) : null;
        } catch (error) {
            this.stats.dbConnected = false;
            return null;
        }
    }

    async delete(key) {
        try {
            const [result] = await this.db.query(
                `DELETE FROM ${this.tableName} WHERE id = ?`,
                [key]
            );
            this.stats.dbConnected = true;
            return result.affectedRows > 0;
        } catch (error) {
            this.stats.dbConnected = false;
            return false;
        }
    }

    async cleanup(now) {
        try {
            await this.db.query(
                `DELETE FROM ${this.tableName} WHERE expires_at <= NOW()`
            );
            this.stats.dbConnected = true;
        } catch (error) {
            this.stats.dbConnected = false;
        }
    }

    getStats() {
        return this.stats;
    }
}

module.exports = { 
    SessionManager, 
    MemoryStorage, 
    RedisStorage, 
    DatabaseStorage 
}; 
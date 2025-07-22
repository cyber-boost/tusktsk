/**
 * TuskLang Authentication Middleware
 * ==================================
 * JWT validation and role-based access control
 */

const jwt = require('jsonwebtoken');

/**
 * Require authentication middleware
 */
function requireAuth(req, res, next) {
    try {
        const token = extractToken(req);
        
        if (!token) {
            return res.status(401).json({
                error: 'Authentication required',
                message: 'No authentication token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        const session = req.security.validateSession(token);
        
        if (!session) {
            return res.status(401).json({
                error: 'Authentication failed',
                message: 'Invalid or expired session',
                timestamp: new Date().toISOString()
            });
        }

        // Add user information to request
        req.user = {
            id: decoded.userId,
            username: decoded.username,
            sessionId: decoded.sessionId
        };

        next();
    } catch (error) {
        return res.status(401).json({
            error: 'Authentication failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
}

/**
 * Optional authentication middleware
 */
function optionalAuth(req, res, next) {
    try {
        const token = extractToken(req);
        
        if (token) {
            const decoded = req.jwt.verify(token);
            const session = req.security.validateSession(token);
            
            if (session) {
                req.user = {
                    id: decoded.userId,
                    username: decoded.username,
                    sessionId: decoded.sessionId
                };
            }
        }

        next();
    } catch (error) {
        // Continue without authentication
        next();
    }
}

/**
 * Require specific role middleware
 */
function requireRole(role) {
    return (req, res, next) => {
        if (!req.user) {
            return res.status(401).json({
                error: 'Authentication required',
                message: 'User must be authenticated',
                timestamp: new Date().toISOString()
            });
        }

        // Get user info to check role
        const userInfo = req.security.getUserInfo(req.user.username);
        
        if (!userInfo || !userInfo.metadata.role) {
            return res.status(403).json({
                error: 'Insufficient permissions',
                message: 'User does not have required role',
                timestamp: new Date().toISOString()
            });
        }

        if (userInfo.metadata.role !== role) {
            return res.status(403).json({
                error: 'Insufficient permissions',
                message: `Required role: ${role}, User role: ${userInfo.metadata.role}`,
                timestamp: new Date().toISOString()
            });
        }

        next();
    };
}

/**
 * Require any of the specified roles middleware
 */
function requireAnyRole(roles) {
    return (req, res, next) => {
        if (!req.user) {
            return res.status(401).json({
                error: 'Authentication required',
                message: 'User must be authenticated',
                timestamp: new Date().toISOString()
            });
        }

        const userInfo = req.security.getUserInfo(req.user.username);
        
        if (!userInfo || !userInfo.metadata.role) {
            return res.status(403).json({
                error: 'Insufficient permissions',
                message: 'User does not have required role',
                timestamp: new Date().toISOString()
            });
        }

        if (!roles.includes(userInfo.metadata.role)) {
            return res.status(403).json({
                error: 'Insufficient permissions',
                message: `Required roles: ${roles.join(', ')}, User role: ${userInfo.metadata.role}`,
                timestamp: new Date().toISOString()
            });
        }

        next();
    };
}

/**
 * Require admin role middleware
 */
function requireAdmin(req, res, next) {
    return requireRole('admin')(req, res, next);
}

/**
 * Require user to be owner or admin middleware
 */
function requireOwnerOrAdmin(ownerIdField = 'userId') {
    return (req, res, next) => {
        if (!req.user) {
            return res.status(401).json({
                error: 'Authentication required',
                message: 'User must be authenticated',
                timestamp: new Date().toISOString()
            });
        }

        const userInfo = req.security.getUserInfo(req.user.username);
        
        // Admin can access anything
        if (userInfo && userInfo.metadata.role === 'admin') {
            return next();
        }

        // Check if user is the owner
        const ownerId = req.params[ownerIdField] || req.body[ownerIdField];
        
        if (ownerId && ownerId === req.user.id) {
            return next();
        }

        return res.status(403).json({
            error: 'Insufficient permissions',
            message: 'User must be owner or admin',
            timestamp: new Date().toISOString()
        });
    };
}

/**
 * Rate limiting middleware
 */
function rateLimit(options = {}) {
    const {
        windowMs = 15 * 60 * 1000, // 15 minutes
        maxRequests = 100,
        keyGenerator = (req) => req.ip || req.connection.remoteAddress
    } = options;

    const requests = new Map();

    return (req, res, next) => {
        const key = keyGenerator(req);
        const now = Date.now();
        const windowStart = now - windowMs;

        let userRequests = requests.get(key) || [];
        
        // Remove old requests outside the window
        userRequests = userRequests.filter(timestamp => timestamp > windowStart);
        
        if (userRequests.length >= maxRequests) {
            return res.status(429).json({
                error: 'Too many requests',
                message: `Rate limit exceeded. Maximum ${maxRequests} requests per ${windowMs / 1000 / 60} minutes.`,
                retryAfter: Math.ceil(windowMs / 1000),
                timestamp: new Date().toISOString()
            });
        }
        
        // Add current request
        userRequests.push(now);
        requests.set(key, userRequests);
        
        // Add rate limit headers
        res.setHeader('X-RateLimit-Limit', maxRequests);
        res.setHeader('X-RateLimit-Remaining', maxRequests - userRequests.length);
        res.setHeader('X-RateLimit-Reset', new Date(now + windowMs).toISOString());
        
        next();
    };
}

/**
 * Extract token from request
 */
function extractToken(req) {
    // Check Authorization header
    const authHeader = req.headers.authorization;
    if (authHeader && authHeader.startsWith('Bearer ')) {
        return authHeader.substring(7);
    }

    // Check query parameter
    if (req.query.token) {
        return req.query.token;
    }

    // Check cookie
    if (req.cookies && req.cookies.token) {
        return req.cookies.token;
    }

    return null;
}

/**
 * Validate token middleware
 */
function validateToken(req, res, next) {
    try {
        const token = extractToken(req);
        
        if (!token) {
            return res.status(401).json({
                error: 'Token required',
                message: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        req.token = decoded;
        
        next();
    } catch (error) {
        return res.status(401).json({
            error: 'Invalid token',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
}

/**
 * Check if user is authenticated
 */
function isAuthenticated(req) {
    return !!req.user;
}

/**
 * Check if user has role
 */
function hasRole(req, role) {
    if (!req.user) return false;
    
    const userInfo = req.security.getUserInfo(req.user.username);
    return userInfo && userInfo.metadata.role === role;
}

/**
 * Check if user has any of the specified roles
 */
function hasAnyRole(req, roles) {
    if (!req.user) return false;
    
    const userInfo = req.security.getUserInfo(req.user.username);
    return userInfo && roles.includes(userInfo.metadata.role);
}

/**
 * Check if user is admin
 */
function isAdmin(req) {
    return hasRole(req, 'admin');
}

/**
 * Check if user is owner or admin
 */
function isOwnerOrAdmin(req, ownerId) {
    if (!req.user) return false;
    
    if (isAdmin(req)) return true;
    
    return req.user.id === ownerId;
}

/**
 * Get current user middleware
 */
function getCurrentUser(req, res, next) {
    if (req.user) {
        const userInfo = req.security.getUserInfo(req.user.username);
        req.currentUser = userInfo;
    }
    next();
}

/**
 * Log authentication events middleware
 */
function logAuthEvents(req, res, next) {
    const originalEnd = res.end;
    
    res.end = function(chunk, encoding) {
        // Log authentication events
        if (req.user && res.statusCode >= 200 && res.statusCode < 300) {
            req.security.logSecurityEvent('api_access', {
                userId: req.user.id,
                username: req.user.username,
                method: req.method,
                url: req.url,
                statusCode: res.statusCode,
                ip: req.ip || req.connection.remoteAddress,
                userAgent: req.get('User-Agent')
            });
        }
        
        originalEnd.call(this, chunk, encoding);
    };
    
    next();
}

module.exports = {
    requireAuth,
    optionalAuth,
    requireRole,
    requireAnyRole,
    requireAdmin,
    requireOwnerOrAdmin,
    rateLimit,
    validateToken,
    extractToken,
    isAuthenticated,
    hasRole,
    hasAnyRole,
    isAdmin,
    isOwnerOrAdmin,
    getCurrentUser,
    logAuthEvents
}; 
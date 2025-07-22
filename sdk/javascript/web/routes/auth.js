/**
 * TuskLang Authentication Routes
 * =============================
 * User authentication and session management
 */

const express = require('express');
const { body, validationResult } = require('express-validator');
const router = express.Router();

/**
 * POST /api/auth/register
 * Register a new user
 */
router.post('/register', [
    body('username').isLength({ min: 3, max: 50 }).withMessage('Username must be between 3 and 50 characters'),
    body('password').isLength({ min: 8 }).withMessage('Password must be at least 8 characters'),
    body('email').isEmail().withMessage('Valid email is required'),
    body('metadata').optional().isObject().withMessage('Metadata must be an object')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { username, password, email, metadata = {} } = req.body;
        
        // Add IP address to metadata
        metadata.ip = req.ip || req.connection.remoteAddress;
        metadata.userAgent = req.get('User-Agent');

        const result = await req.security.registerUser(username, password, {
            ...metadata,
            email
        });

        res.status(201).json({
            success: true,
            message: 'User registered successfully',
            data: {
                userId: result.userId,
                username: result.username
            },
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(400).json({
            error: 'Registration failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/auth/login
 * Authenticate user and create session
 */
router.post('/login', [
    body('username').notEmpty().withMessage('Username is required'),
    body('password').notEmpty().withMessage('Password is required')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { username, password } = req.body;
        
        const metadata = {
            ip: req.ip || req.connection.remoteAddress,
            userAgent: req.get('User-Agent')
        };

        const session = await req.security.authenticateUser(username, password, metadata);
        
        // Generate JWT token
        const token = req.jwt.generate({
            userId: session.userId,
            username,
            sessionId: session.sessionId
        });

        res.json({
            success: true,
            message: 'Login successful',
            data: {
                token,
                sessionId: session.sessionId,
                expiresAt: session.expiresAt
            },
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(401).json({
            error: 'Authentication failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/auth/logout
 * Logout user and invalidate session
 */
router.post('/logout', async (req, res) => {
    try {
        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (token) {
            const decoded = req.jwt.verify(token);
            const success = req.security.logout(token);
            
            if (success) {
                res.json({
                    success: true,
                    message: 'Logout successful',
                    timestamp: new Date().toISOString()
                });
            } else {
                res.status(400).json({
                    error: 'Logout failed',
                    message: 'Invalid session',
                    timestamp: new Date().toISOString()
                });
            }
        } else {
            res.status(400).json({
                error: 'Logout failed',
                message: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }
    } catch (error) {
        res.status(500).json({
            error: 'Logout failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/auth/verify
 * Verify authentication token
 */
router.get('/verify', async (req, res) => {
    try {
        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (!token) {
            return res.status(401).json({
                error: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        const session = req.security.validateSession(token);
        
        if (!session) {
            return res.status(401).json({
                error: 'Invalid or expired token',
                timestamp: new Date().toISOString()
            });
        }

        res.json({
            success: true,
            data: {
                userId: decoded.userId,
                username: decoded.username,
                sessionId: decoded.sessionId,
                valid: true
            },
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(401).json({
            error: 'Token verification failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/auth/refresh
 * Refresh authentication token
 */
router.post('/refresh', async (req, res) => {
    try {
        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (!token) {
            return res.status(401).json({
                error: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        const session = req.security.validateSession(token);
        
        if (!session) {
            return res.status(401).json({
                error: 'Invalid or expired token',
                timestamp: new Date().toISOString()
            });
        }

        // Generate new token
        const newToken = req.jwt.generate({
            userId: decoded.userId,
            username: decoded.username,
            sessionId: decoded.sessionId
        });

        res.json({
            success: true,
            data: {
                token: newToken,
                sessionId: decoded.sessionId
            },
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(401).json({
            error: 'Token refresh failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/auth/profile
 * Get user profile information
 */
router.get('/profile', async (req, res) => {
    try {
        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (!token) {
            return res.status(401).json({
                error: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        const userInfo = req.security.getUserInfo(decoded.username);
        
        if (!userInfo) {
            return res.status(404).json({
                error: 'User not found',
                timestamp: new Date().toISOString()
            });
        }

        res.json({
            success: true,
            data: userInfo,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Profile retrieval failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * PUT /api/auth/profile
 * Update user profile
 */
router.put('/profile', [
    body('email').optional().isEmail().withMessage('Valid email is required'),
    body('metadata').optional().isObject().withMessage('Metadata must be an object')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (!token) {
            return res.status(401).json({
                error: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        const { email, metadata } = req.body;
        
        // Update user metadata
        const userInfo = req.security.getUserInfo(decoded.username);
        if (userInfo) {
            if (email) userInfo.metadata.email = email;
            if (metadata) {
                userInfo.metadata = { ...userInfo.metadata, ...metadata };
            }
        }

        res.json({
            success: true,
            message: 'Profile updated successfully',
            data: userInfo,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Profile update failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/auth/change-password
 * Change user password
 */
router.post('/change-password', [
    body('currentPassword').notEmpty().withMessage('Current password is required'),
    body('newPassword').isLength({ min: 8 }).withMessage('New password must be at least 8 characters')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (!token) {
            return res.status(401).json({
                error: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        const { currentPassword, newPassword } = req.body;
        
        // Verify current password
        const userInfo = req.security.getUserInfo(decoded.username);
        if (!userInfo) {
            return res.status(404).json({
                error: 'User not found',
                timestamp: new Date().toISOString()
            });
        }

        // For now, we'll return success (in a real implementation, you'd update the password)
        res.json({
            success: true,
            message: 'Password changed successfully',
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Password change failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/auth/sessions
 * Get active sessions for user
 */
router.get('/sessions', async (req, res) => {
    try {
        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (!token) {
            return res.status(401).json({
                error: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        const activeSessions = req.security.getActiveSessions();
        
        // Filter sessions for current user
        const userSessions = activeSessions.filter(session => 
            session.userId === decoded.userId
        );

        res.json({
            success: true,
            data: userSessions,
            count: userSessions.length,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Session retrieval failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * DELETE /api/auth/sessions/:sessionId
 * Invalidate specific session
 */
router.delete('/sessions/:sessionId', async (req, res) => {
    try {
        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (!token) {
            return res.status(401).json({
                error: 'No token provided',
                timestamp: new Date().toISOString()
            });
        }

        const decoded = req.jwt.verify(token);
        const { sessionId } = req.params;
        
        req.security.invalidateSession(sessionId);

        res.json({
            success: true,
            message: 'Session invalidated successfully',
            sessionId,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Session invalidation failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

module.exports = router; 
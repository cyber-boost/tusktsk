/**
 * TuskLang Admin Routes
 * ====================
 * Administrative functions and system management
 */

const express = require('express');
const { body, validationResult } = require('express-validator');
const router = express.Router();

/**
 * GET /api/admin/dashboard
 * Get admin dashboard data
 */
router.get('/dashboard', async (req, res) => {
    try {
        const stats = req.security.getSecurityStats();
        const serverStats = {
            uptime: process.uptime(),
            memory: process.memoryUsage(),
            environment: process.env.NODE_ENV || 'development',
            version: require('../../package.json').version
        };

        res.json({
            success: true,
            data: {
                security: stats,
                server: serverStats,
                timestamp: new Date().toISOString()
            }
        });
    } catch (error) {
        res.status(500).json({
            error: 'Dashboard data retrieval failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/admin/users
 * Get all users (admin only)
 */
router.get('/users', async (req, res) => {
    try {
        // In a real implementation, you'd get users from the security manager
        const users = []; // Placeholder for user list
        
        res.json({
            success: true,
            data: users,
            count: users.length,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'User retrieval failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/admin/sessions
 * Get all active sessions (admin only)
 */
router.get('/sessions', async (req, res) => {
    try {
        const activeSessions = req.security.getActiveSessions();
        
        res.json({
            success: true,
            data: activeSessions,
            count: activeSessions.length,
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
 * POST /api/admin/sessions/cleanup
 * Clean up expired sessions
 */
router.post('/sessions/cleanup', async (req, res) => {
    try {
        const cleanedCount = req.security.cleanupExpiredSessions();
        
        res.json({
            success: true,
            message: `Cleaned up ${cleanedCount} expired sessions`,
            cleanedCount,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Session cleanup failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/admin/audit
 * Get security audit log (admin only)
 */
router.get('/audit', [
    body('limit').optional().isInt({ min: 1, max: 1000 }).withMessage('Limit must be between 1 and 1000')
], async (req, res) => {
    try {
        const { limit = 100 } = req.query;
        const auditLog = req.security.getAuditLog(parseInt(limit));

        res.json({
            success: true,
            data: auditLog,
            count: auditLog.length,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Audit log retrieval failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/admin/system/restart
 * Restart the system (admin only)
 */
router.post('/system/restart', async (req, res) => {
    try {
        res.json({
            success: true,
            message: 'System restart initiated',
            timestamp: new Date().toISOString()
        });
        
        // In a real implementation, you'd trigger a system restart
        setTimeout(() => {
            process.exit(0);
        }, 1000);
    } catch (error) {
        res.status(500).json({
            error: 'System restart failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/admin/system/health
 * Get detailed system health information
 */
router.get('/system/health', async (req, res) => {
    try {
        const health = {
            status: 'healthy',
            timestamp: new Date().toISOString(),
            uptime: process.uptime(),
            memory: process.memoryUsage(),
            cpu: process.cpuUsage(),
            environment: process.env.NODE_ENV || 'development',
            version: require('../../package.json').version,
            platform: process.platform,
            nodeVersion: process.version
        };

        res.json({
            success: true,
            data: health
        });
    } catch (error) {
        res.status(500).json({
            error: 'Health check failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

module.exports = router; 
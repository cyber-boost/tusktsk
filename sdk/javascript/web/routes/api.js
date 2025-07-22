/**
 * TuskLang API Routes
 * ===================
 * RESTful API endpoints with TSK integration
 */

const express = require('express');
const { body, validationResult } = require('express-validator');
const router = express.Router();

/**
 * GET /api/v1/status
 * Get API status and health information
 */
router.get('/status', (req, res) => {
    res.json({
        status: 'operational',
        timestamp: new Date().toISOString(),
        version: require('../../package.json').version,
        features: {
            tsk: 'enabled',
            security: 'enabled',
            websockets: 'enabled',
            authentication: 'enabled'
        }
    });
});

/**
 * POST /api/v1/parse
 * Parse TSK configuration
 */
router.post('/parse', [
    body('content').isString().notEmpty().withMessage('TSK content is required'),
    body('options').optional().isObject()
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { content, options = {} } = req.body;
        const result = await req.tusk.parse(content, options);

        res.json({
            success: true,
            data: result,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Parse failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/v1/validate
 * Validate TSK configuration
 */
router.post('/validate', [
    body('content').isString().notEmpty().withMessage('TSK content is required')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { content } = req.body;
        const isValid = await req.tusk.validate(content);

        res.json({
            success: true,
            valid: isValid,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Validation failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/v1/compile
 * Compile TSK to binary format
 */
router.post('/compile', [
    body('content').isString().notEmpty().withMessage('TSK content is required'),
    body('target').optional().isString().withMessage('Target must be a string')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { content, target = 'binary' } = req.body;
        const result = await req.tusk.compile(content, { target });

        res.json({
            success: true,
            data: result,
            target,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Compilation failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/v1/execute
 * Execute TSK configuration
 */
router.post('/execute', [
    body('content').isString().notEmpty().withMessage('TSK content is required'),
    body('context').optional().isObject().withMessage('Context must be an object')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { content, context = {} } = req.body;
        const result = await req.tusk.execute(content, context);

        res.json({
            success: true,
            data: result,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Execution failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/v1/config/:key
 * Get configuration value by key
 */
router.get('/config/:key(*)', async (req, res) => {
    try {
        const { key } = req.params;
        const value = await req.peanut.get(key);

        res.json({
            success: true,
            key,
            value,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(404).json({
            error: 'Configuration not found',
            key: req.params.key,
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/v1/config/:key
 * Set configuration value by key
 */
router.post('/config/:key(*)', [
    body('value').notEmpty().withMessage('Value is required')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { key } = req.params;
        const { value } = req.body;
        
        await req.peanut.set(key, value);

        res.json({
            success: true,
            key,
            value,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Configuration update failed',
            key: req.params.key,
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/v1/database/status
 * Get database connection status
 */
router.get('/database/status', async (req, res) => {
    try {
        const status = await req.tusk.database.status();
        
        res.json({
            success: true,
            status,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Database status check failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/v1/database/query
 * Execute database query
 */
router.post('/database/query', [
    body('query').isString().notEmpty().withMessage('Query is required'),
    body('params').optional().isArray().withMessage('Params must be an array')
], async (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { query, params = [] } = req.body;
        const result = await req.tusk.database.query(query, params);

        res.json({
            success: true,
            data: result,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Database query failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/v1/security/stats
 * Get security statistics
 */
router.get('/security/stats', (req, res) => {
    try {
        const stats = req.security.getSecurityStats();
        
        res.json({
            success: true,
            stats,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Security stats failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * POST /api/v1/security/audit
 * Get security audit log
 */
router.post('/security/audit', [
    body('limit').optional().isInt({ min: 1, max: 1000 }).withMessage('Limit must be between 1 and 1000')
], (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { limit = 100 } = req.body;
        const auditLog = req.security.getAuditLog(limit);

        res.json({
            success: true,
            auditLog,
            count: auditLog.length,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Security audit failed',
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * GET /api/v1/features
 * Get available TSK features
 */
router.get('/features', (req, res) => {
    const features = req.tusk.getFeatures();
    
    res.json({
        success: true,
        features,
        count: features.length,
        timestamp: new Date().toISOString()
    });
});

/**
 * POST /api/v1/features/:feature
 * Enable/disable TSK feature
 */
router.post('/features/:feature', [
    body('enabled').isBoolean().withMessage('Enabled must be a boolean')
], (req, res) => {
    try {
        const errors = validationResult(req);
        if (!errors.isEmpty()) {
            return res.status(400).json({
                error: 'Validation failed',
                details: errors.array()
            });
        }

        const { feature } = req.params;
        const { enabled } = req.body;
        
        const result = req.tusk.setFeature(feature, enabled);

        res.json({
            success: true,
            feature,
            enabled,
            result,
            timestamp: new Date().toISOString()
        });
    } catch (error) {
        res.status(500).json({
            error: 'Feature toggle failed',
            feature: req.params.feature,
            message: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

module.exports = router; 
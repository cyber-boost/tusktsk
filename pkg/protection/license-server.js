const express = require('express');
const crypto = require('crypto');
const jwt = require('jsonwebtoken');
const rateLimit = require('express-rate-limit');
const helmet = require('helmet');
const cors = require('cors');

// License server implementation
class TuskLangLicenseServer {
    constructor() {
        this.app = express();
        this.licenses = new Map();
        this.usage = new Map();
        this.secretKey = process.env.JWT_SECRET || crypto.randomBytes(32).toString('hex');
        
        this.setupMiddleware();
        this.setupRoutes();
        this.loadLicenses();
    }
    
    setupMiddleware() {
        // Security middleware
        this.app.use(helmet());
        this.app.use(cors({
            origin: ['https://tuskt.sk', 'https://license.tuskt.sk'],
            credentials: true
        }));
        
        // Rate limiting
        const limiter = rateLimit({
            windowMs: 15 * 60 * 1000, // 15 minutes
            max: 100, // limit each IP to 100 requests per windowMs
            message: 'Too many requests from this IP'
        });
        this.app.use('/api/', limiter);
        
        // Body parsing
        this.app.use(express.json({ limit: '1mb' }));
        this.app.use(express.urlencoded({ extended: true }));
        
        // Request logging
        this.app.use((req, res, next) => {
            console.log(`${new Date().toISOString()} - ${req.method} ${req.path} - ${req.ip}`);
            next();
        });
    }
    
    setupRoutes() {
        // Health check
        this.app.get('/health', (req, res) => {
            res.json({ status: 'healthy', timestamp: new Date().toISOString() });
        });
        
        // License validation
        this.app.post('/api/v1/validate', this.validateLicense.bind(this));
        
        // License status check
        this.app.get('/api/v1/status/:licenseId', this.getLicenseStatus.bind(this));
        
        // Usage tracking
        this.app.post('/api/v1/usage', this.trackUsage.bind(this));
        
        // Installation tracking
        this.app.post('/api/v1/install', this.trackInstallation.bind(this));
        
        // Usage limits
        this.app.get('/api/v1/limits/:licenseId', this.getUsageLimits.bind(this));
        
        // Heartbeat
        this.app.post('/api/v1/heartbeat', this.heartbeat.bind(this));
        
        // Admin routes (protected)
        this.app.post('/api/v1/admin/licenses', this.createLicense.bind(this));
        this.app.delete('/api/v1/admin/licenses/:licenseId', this.revokeLicense.bind(this));
        this.app.get('/api/v1/admin/analytics', this.getAnalytics.bind(this));
    }
    
    loadLicenses() {
        // Load licenses from database/file
        // This is a simplified example - in production, use a real database
        
        const sampleLicenses = [
            {
                key: 'ABCD-1234-EFGH-5678',
                type: 'commercial',
                maxUsers: 10,
                maxApiCalls: 10000,
                expiresAt: new Date('2025-12-31'),
                features: ['parser', 'compiler', 'api'],
                status: 'active'
            },
            {
                key: 'WXYZ-9876-MNOP-5432',
                type: 'trial',
                maxUsers: 1,
                maxApiCalls: 1000,
                expiresAt: new Date('2025-02-15'),
                features: ['parser'],
                status: 'active'
            }
        ];
        
        sampleLicenses.forEach(license => {
            this.licenses.set(license.key, license);
        });
    }
    
    validateLicense(req, res) {
        try {
            const { key } = req.body;
            
            if (!key) {
                return res.status(400).json({
                    valid: false,
                    message: 'License key is required'
                });
            }
            
            const license = this.licenses.get(key);
            
            if (!license) {
                return res.json({
                    valid: false,
                    message: 'Invalid license key'
                });
            }
            
            // Check if license is expired
            if (license.expiresAt < new Date()) {
                return res.json({
                    valid: false,
                    message: 'License has expired'
                });
            }
            
            // Check if license is revoked
            if (license.status === 'revoked') {
                return res.json({
                    valid: false,
                    message: 'License has been revoked'
                });
            }
            
            // Generate JWT token for this validation
            const token = jwt.sign({
                licenseKey: key,
                type: license.type,
                features: license.features,
                exp: Math.floor(Date.now() / 1000) + (60 * 60) // 1 hour
            }, this.secretKey);
            
            res.json({
                valid: true,
                message: 'License is valid',
                license: {
                    type: license.type,
                    features: license.features,
                    expiresAt: license.expiresAt,
                    maxUsers: license.maxUsers,
                    maxApiCalls: license.maxApiCalls
                },
                token: token
            });
            
        } catch (error) {
            console.error('License validation error:', error);
            res.status(500).json({
                valid: false,
                message: 'Internal server error'
            });
        }
    }
    
    getLicenseStatus(req, res) {
        try {
            const { licenseId } = req.params;
            const license = this.licenses.get(licenseId);
            
            if (!license) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            // Get usage statistics
            const usage = this.usage.get(licenseId) || {
                apiCalls: 0,
                users: new Set(),
                lastUsed: null
            };
            
            res.json({
                license: {
                    key: licenseId,
                    type: license.type,
                    status: license.status,
                    expiresAt: license.expiresAt,
                    features: license.features
                },
                usage: {
                    apiCalls: usage.apiCalls,
                    userCount: usage.users.size,
                    lastUsed: usage.lastUsed
                },
                limits: {
                    maxUsers: license.maxUsers,
                    maxApiCalls: license.maxApiCalls
                }
            });
            
        } catch (error) {
            console.error('License status error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    trackUsage(req, res) {
        try {
            const { licenseKey, action, userId, metadata } = req.body;
            
            if (!licenseKey || !action) {
                return res.status(400).json({
                    error: 'License key and action are required'
                });
            }
            
            const license = this.licenses.get(licenseKey);
            if (!license) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            // Update usage statistics
            if (!this.usage.has(licenseKey)) {
                this.usage.set(licenseKey, {
                    apiCalls: 0,
                    users: new Set(),
                    lastUsed: null
                });
            }
            
            const usage = this.usage.get(licenseKey);
            usage.apiCalls++;
            if (userId) usage.users.add(userId);
            usage.lastUsed = new Date();
            
            // Check usage limits
            const withinLimits = usage.apiCalls <= license.maxApiCalls &&
                                usage.users.size <= license.maxUsers;
            
            res.json({
                success: true,
                withinLimits,
                usage: {
                    apiCalls: usage.apiCalls,
                    userCount: usage.users.size,
                    lastUsed: usage.lastUsed
                },
                limits: {
                    maxApiCalls: license.maxApiCalls,
                    maxUsers: license.maxUsers
                }
            });
            
        } catch (error) {
            console.error('Usage tracking error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    trackInstallation(req, res) {
        try {
            const { license_key, version, platform, arch, timestamp } = req.body;
            
            console.log(`Installation tracked: ${license_key} on ${platform}-${arch} (v${version})`);
            
            // Store installation data (in production, save to database)
            // This could be used for analytics and support
            
            res.json({
                success: true,
                message: 'Installation recorded'
            });
            
        } catch (error) {
            console.error('Installation tracking error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    getUsageLimits(req, res) {
        try {
            const { licenseId } = req.params;
            const license = this.licenses.get(licenseId);
            
            if (!license) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            const usage = this.usage.get(licenseId) || {
                apiCalls: 0,
                users: new Set()
            };
            
            res.json({
                current: {
                    apiCalls: usage.apiCalls,
                    userCount: usage.users.size
                },
                max: {
                    apiCalls: license.maxApiCalls,
                    users: license.maxUsers
                },
                remaining: {
                    apiCalls: Math.max(0, license.maxApiCalls - usage.apiCalls),
                    users: Math.max(0, license.maxUsers - usage.users.size)
                }
            });
            
        } catch (error) {
            console.error('Usage limits error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    heartbeat(req, res) {
        try {
            const { licenseKey } = req.body;
            
            if (!licenseKey) {
                return res.status(400).json({
                    error: 'License key is required'
                });
            }
            
            const license = this.licenses.get(licenseKey);
            if (!license) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            // Update last heartbeat
            if (!this.usage.has(licenseKey)) {
                this.usage.set(licenseKey, {});
            }
            
            const usage = this.usage.get(licenseKey);
            usage.lastHeartbeat = new Date();
            
            res.json({
                success: true,
                status: license.status,
                expiresAt: license.expiresAt
            });
            
        } catch (error) {
            console.error('Heartbeat error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    // Admin routes (protected with authentication)
    createLicense(req, res) {
        // In production, add authentication middleware
        try {
            const { type, maxUsers, maxApiCalls, features, expiresAt } = req.body;
            
            const licenseKey = this.generateLicenseKey();
            
            const license = {
                key: licenseKey,
                type,
                maxUsers,
                maxApiCalls,
                features,
                expiresAt: new Date(expiresAt),
                status: 'active',
                createdAt: new Date()
            };
            
            this.licenses.set(licenseKey, license);
            
            res.json({
                success: true,
                license: {
                    key: licenseKey,
                    type,
                    maxUsers,
                    maxApiCalls,
                    features,
                    expiresAt: license.expiresAt
                }
            });
            
        } catch (error) {
            console.error('Create license error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    revokeLicense(req, res) {
        try {
            const { licenseId } = req.params;
            const license = this.licenses.get(licenseId);
            
            if (!license) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            license.status = 'revoked';
            license.revokedAt = new Date();
            
            res.json({
                success: true,
                message: 'License revoked successfully'
            });
            
        } catch (error) {
            console.error('Revoke license error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    getAnalytics(req, res) {
        try {
            const analytics = {
                totalLicenses: this.licenses.size,
                activeLicenses: Array.from(this.licenses.values()).filter(l => l.status === 'active').length,
                revokedLicenses: Array.from(this.licenses.values()).filter(l => l.status === 'revoked').length,
                totalUsage: Array.from(this.usage.values()).reduce((sum, u) => sum + (u.apiCalls || 0), 0),
                licensesByType: {}
            };
            
            // Group licenses by type
            for (const license of this.licenses.values()) {
                analytics.licensesByType[license.type] = (analytics.licensesByType[license.type] || 0) + 1;
            }
            
            res.json(analytics);
            
        } catch (error) {
            console.error('Analytics error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    generateLicenseKey() {
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
        let key = '';
        
        for (let i = 0; i < 4; i++) {
            for (let j = 0; j < 4; j++) {
                key += chars.charAt(Math.floor(Math.random() * chars.length));
            }
            if (i < 3) key += '-';
        }
        
        return key;
    }
    
    start(port = 3000) {
        this.app.listen(port, () => {
            console.log(`TuskLang License Server running on port ${port}`);
            console.log(`Health check: http://localhost:${port}/health`);
        });
    }
}

// Export for use
module.exports = TuskLangLicenseServer;

// Start server if run directly
if (require.main === module) {
    const server = new TuskLangLicenseServer();
    server.start(process.env.PORT || 3000);
} 
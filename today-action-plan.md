# TuskLang Mother Database - TODAY Action Plan
## Immediate Implementation Using lic.tusklang.org API Endpoint

### 🎯 GOAL: Complete Mother Database & Critical Protection Infrastructure TODAY

**API Endpoint:** `https://lic.tusklang.org/api/v1`
**Database Connection:** (Backend only - not exposed to clients)
- Primary: `postgresql://tt_c3b2:Tusk83f8d5e5e2de70aed8a34bd9ef3277a4db4b6d5fLang@178.156.165.85:5432/tusklang_theory`
- Backup: `postgresql://tt_c3b2:Tusk83f8d5e5e2de70aed8a34bd9ef3277a4db4b6d5fLang@tonton.io:5432/tusklang_theory`

---

## 🚀 IMMEDIATE ACTION ITEMS (TODAY)

### HOUR 1-2: API Server Setup & Database Schema

#### 1.1 Set Up lic.tusklang.org Domain
```bash
# Configure DNS for lic.tusklang.org
# Point to your server IP where license API will run
```

#### 1.2 Create Database Schema (Backend Only)
```sql
-- Connect to database and execute:

-- License management table
CREATE TABLE IF NOT EXISTS licenses (
    id SERIAL PRIMARY KEY,
    license_key VARCHAR(255) UNIQUE NOT NULL,
    user_email VARCHAR(255),
    company_name VARCHAR(255),
    license_type VARCHAR(50) NOT NULL DEFAULT 'trial',
    max_users INTEGER DEFAULT 1,
    max_api_calls INTEGER DEFAULT 10000,
    features JSONB DEFAULT '["parser", "compiler"]',
    status VARCHAR(20) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT NOW(),
    expires_at TIMESTAMP DEFAULT (NOW() + INTERVAL '30 days'),
    revoked_at TIMESTAMP,
    last_used TIMESTAMP,
    hardware_fingerprint TEXT,
    ip_whitelist TEXT[]
);

-- Installation tracking table
CREATE TABLE IF NOT EXISTS installations (
    id SERIAL PRIMARY KEY,
    license_id INTEGER REFERENCES licenses(id) ON DELETE CASCADE,
    installation_id VARCHAR(255) UNIQUE NOT NULL,
    platform VARCHAR(50),
    architecture VARCHAR(50),
    version VARCHAR(50),
    ip_address INET,
    user_agent TEXT,
    installed_at TIMESTAMP DEFAULT NOW(),
    last_heartbeat TIMESTAMP DEFAULT NOW(),
    status VARCHAR(20) DEFAULT 'active',
    fingerprint_hash VARCHAR(64)
);

-- Usage tracking table
CREATE TABLE IF NOT EXISTS usage_logs (
    id SERIAL PRIMARY KEY,
    license_id INTEGER REFERENCES licenses(id) ON DELETE CASCADE,
    installation_id VARCHAR(255),
    action VARCHAR(100),
    metadata JSONB,
    timestamp TIMESTAMP DEFAULT NOW(),
    ip_address INET
);

-- Admin actions log
CREATE TABLE IF NOT EXISTS admin_actions (
    id SERIAL PRIMARY KEY,
    admin_user VARCHAR(255),
    action VARCHAR(100),
    target_license_id INTEGER REFERENCES licenses(id),
    details JSONB,
    timestamp TIMESTAMP DEFAULT NOW(),
    ip_address INET
);

-- API keys for admin access
CREATE TABLE IF NOT EXISTS api_keys (
    id SERIAL PRIMARY KEY,
    key_hash VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(255),
    permissions JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    last_used TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE
);
```

#### 1.3 Insert Initial Test Data
```sql
-- Insert test licenses
INSERT INTO licenses (license_key, user_email, company_name, license_type, max_users, max_api_calls, features) VALUES
('ABCD-1234-EFGH-5678', 'test@tusklang.org', 'Test Company', 'commercial', 10, 50000, '["parser", "compiler", "api", "ai"]'),
('WXYZ-9876-MNOP-5432', 'demo@tusklang.org', 'Demo Corp', 'trial', 1, 1000, '["parser"]'),
('TEST-0000-DEMO-9999', 'bernard@tusklang.org', 'CyberBoost LLC', 'commercial', 100, 100000, '["parser", "compiler", "api", "ai", "self_destruct"]');

-- Insert admin API key
INSERT INTO api_keys (key_hash, name, permissions) VALUES
(encode(sha256('admin-secret-key-2025'::bytea), 'hex'), 'Admin Key', '["read", "write", "admin"]');
```

### HOUR 3-4: License API Server Deployment

#### 2.1 Create Enhanced License API Server
```javascript
// Create /pkg/protection/license-api-server.js
const express = require('express');
const crypto = require('crypto');
const jwt = require('jsonwebtoken');
const rateLimit = require('express-rate-limit');
const helmet = require('helmet');
const cors = require('cors');
const { Pool } = require('pg');

class TuskLangLicenseAPI {
    constructor() {
        this.app = express();
        this.secretKey = process.env.JWT_SECRET || crypto.randomBytes(32).toString('hex');
        
        // Database connection
        this.db = new Pool({
            host: '178.156.165.85',
            port: 5432,
            database: 'tusklang_theory',
            user: 'tt_c3b2',
            password: 'Tusk83f8d5e5e2de70aed8a34bd9ef3277a4db4b6d5fLang',
            ssl: { rejectUnauthorized: false }
        });
        
        this.setupMiddleware();
        this.setupRoutes();
    }
    
    setupMiddleware() {
        // Security middleware
        this.app.use(helmet());
        this.app.use(cors({
            origin: ['https://tusklang.org', 'https://lic.tusklang.org'],
            credentials: true
        }));
        
        // Rate limiting
        const limiter = rateLimit({
            windowMs: 15 * 60 * 1000, // 15 minutes
            max: 100, // limit each IP to 100 requests per windowMs
            message: { error: 'Too many requests from this IP' }
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
            res.json({ 
                status: 'healthy', 
                timestamp: new Date().toISOString(),
                version: '2.0.0'
            });
        });
        
        // License validation
        this.app.post('/api/v1/validate', this.validateLicense.bind(this));
        
        // License status
        this.app.get('/api/v1/status/:licenseKey', this.getLicenseStatus.bind(this));
        
        // Installation tracking
        this.app.post('/api/v1/install', this.trackInstallation.bind(this));
        
        // Usage tracking
        this.app.post('/api/v1/usage', this.trackUsage.bind(this));
        
        // Heartbeat
        this.app.post('/api/v1/heartbeat', this.heartbeat.bind(this));
        
        // Admin routes (protected)
        this.app.use('/api/v1/admin', this.adminAuthMiddleware.bind(this));
        this.app.post('/api/v1/admin/licenses', this.createLicense.bind(this));
        this.app.put('/api/v1/admin/licenses/:licenseKey', this.updateLicense.bind(this));
        this.app.delete('/api/v1/admin/licenses/:licenseKey', this.revokeLicense.bind(this));
        this.app.get('/api/v1/admin/analytics', this.getAnalytics.bind(this));
        this.app.get('/api/v1/admin/installations', this.getInstallations.bind(this));
    }
    
    async validateLicense(req, res) {
        try {
            const { key } = req.body;
            
            if (!key) {
                return res.status(400).json({
                    valid: false,
                    message: 'License key is required'
                });
            }
            
            const result = await this.db.query(
                'SELECT * FROM licenses WHERE license_key = $1',
                [key]
            );
            
            if (result.rows.length === 0) {
                return res.json({
                    valid: false,
                    message: 'Invalid license key'
                });
            }
            
            const license = result.rows[0];
            
            // Check if license is expired
            if (license.expires_at < new Date()) {
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
            
            // Update last used
            await this.db.query(
                'UPDATE licenses SET last_used = NOW() WHERE id = $1',
                [license.id]
            );
            
            // Generate JWT token
            const token = jwt.sign({
                licenseKey: key,
                type: license.license_type,
                features: license.features,
                exp: Math.floor(Date.now() / 1000) + (60 * 60) // 1 hour
            }, this.secretKey);
            
            res.json({
                valid: true,
                message: 'License is valid',
                license: {
                    type: license.license_type,
                    features: license.features,
                    expiresAt: license.expires_at,
                    maxUsers: license.max_users,
                    maxApiCalls: license.max_api_calls
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
    
    async getLicenseStatus(req, res) {
        try {
            const { licenseKey } = req.params;
            
            const result = await this.db.query(
                'SELECT * FROM licenses WHERE license_key = $1',
                [licenseKey]
            );
            
            if (result.rows.length === 0) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            const license = result.rows[0];
            
            // Get usage statistics
            const usageResult = await this.db.query(
                'SELECT COUNT(*) as api_calls FROM usage_logs WHERE license_id = $1',
                [license.id]
            );
            
            const installationResult = await this.db.query(
                'SELECT COUNT(*) as installations FROM installations WHERE license_id = $1 AND status = $2',
                [license.id, 'active']
            );
            
            res.json({
                license: {
                    key: licenseKey,
                    type: license.license_type,
                    status: license.status,
                    expiresAt: license.expires_at,
                    features: license.features
                },
                usage: {
                    apiCalls: parseInt(usageResult.rows[0].api_calls),
                    installations: parseInt(installationResult.rows[0].installations),
                    lastUsed: license.last_used
                },
                limits: {
                    maxUsers: license.max_users,
                    maxApiCalls: license.max_api_calls
                }
            });
            
        } catch (error) {
            console.error('License status error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    async trackInstallation(req, res) {
        try {
            const { license_key, version, platform, arch, fingerprint, ip_address, user_agent } = req.body;
            
            // Get license ID
            const licenseResult = await this.db.query(
                'SELECT id FROM licenses WHERE license_key = $1',
                [license_key]
            );
            
            if (licenseResult.rows.length === 0) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            const licenseId = licenseResult.rows[0].id;
            
            // Insert installation record
            await this.db.query(
                `INSERT INTO installations 
                (license_id, installation_id, platform, architecture, version, ip_address, user_agent, fingerprint_hash)
                VALUES ($1, $2, $3, $4, $5, $6, $7, $8)`,
                [licenseId, fingerprint, platform, arch, version, ip_address, user_agent, fingerprint]
            );
            
            console.log(`Installation tracked: ${license_key} on ${platform}-${arch} (v${version})`);
            
            res.json({
                success: true,
                message: 'Installation recorded',
                installationId: fingerprint
            });
            
        } catch (error) {
            console.error('Installation tracking error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    async trackUsage(req, res) {
        try {
            const { license_key, action, metadata, installation_id } = req.body;
            
            // Get license ID
            const licenseResult = await this.db.query(
                'SELECT id FROM licenses WHERE license_key = $1',
                [license_key]
            );
            
            if (licenseResult.rows.length === 0) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            const licenseId = licenseResult.rows[0].id;
            
            // Insert usage log
            await this.db.query(
                `INSERT INTO usage_logs (license_id, installation_id, action, metadata, ip_address)
                VALUES ($1, $2, $3, $4, $5)`,
                [licenseId, installation_id, action, JSON.stringify(metadata), req.ip]
            );
            
            res.json({
                success: true,
                message: 'Usage tracked'
            });
            
        } catch (error) {
            console.error('Usage tracking error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    async heartbeat(req, res) {
        try {
            const { license_key, installation_id } = req.body;
            
            // Update last heartbeat
            await this.db.query(
                `UPDATE installations 
                SET last_heartbeat = NOW() 
                WHERE installation_id = $1`,
                [installation_id]
            );
            
            res.json({
                success: true,
                timestamp: new Date().toISOString()
            });
            
        } catch (error) {
            console.error('Heartbeat error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    // Admin middleware
    adminAuthMiddleware(req, res, next) {
        const apiKey = req.headers['x-api-key'];
        
        if (!apiKey) {
            return res.status(401).json({
                error: 'API key required'
            });
        }
        
        // Verify API key
        const keyHash = crypto.createHash('sha256').update(apiKey).digest('hex');
        
        this.db.query(
            'SELECT * FROM api_keys WHERE key_hash = $1 AND is_active = true',
            [keyHash]
        ).then(result => {
            if (result.rows.length === 0) {
                return res.status(401).json({
                    error: 'Invalid API key'
                });
            }
            
            req.adminKey = result.rows[0];
            next();
        }).catch(error => {
            console.error('API key verification error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        });
    }
    
    async createLicense(req, res) {
        try {
            const { user_email, company_name, license_type, max_users, max_api_calls, features } = req.body;
            
            // Generate license key
            const licenseKey = this.generateLicenseKey();
            
            // Insert license
            const result = await this.db.query(
                `INSERT INTO licenses 
                (license_key, user_email, company_name, license_type, max_users, max_api_calls, features)
                VALUES ($1, $2, $3, $4, $5, $6, $7)
                RETURNING *`,
                [licenseKey, user_email, company_name, license_type, max_users, max_api_calls, JSON.stringify(features)]
            );
            
            // Log admin action
            await this.db.query(
                `INSERT INTO admin_actions (admin_user, action, target_license_id, details, ip_address)
                VALUES ($1, $2, $3, $4, $5)`,
                [req.adminKey.name, 'create_license', result.rows[0].id, JSON.stringify(req.body), req.ip]
            );
            
            res.json({
                success: true,
                license: {
                    key: licenseKey,
                    type: license_type,
                    maxUsers: max_users,
                    maxApiCalls: max_api_calls,
                    features: features
                }
            });
            
        } catch (error) {
            console.error('Create license error:', error);
            res.status(500).json({
                error: 'Internal server error'
            });
        }
    }
    
    async revokeLicense(req, res) {
        try {
            const { licenseKey } = req.params;
            
            const result = await this.db.query(
                'UPDATE licenses SET status = $1, revoked_at = NOW() WHERE license_key = $2 RETURNING *',
                ['revoked', licenseKey]
            );
            
            if (result.rows.length === 0) {
                return res.status(404).json({
                    error: 'License not found'
                });
            }
            
            // Log admin action
            await this.db.query(
                `INSERT INTO admin_actions (admin_user, action, target_license_id, details, ip_address)
                VALUES ($1, $2, $3, $4, $5)`,
                [req.adminKey.name, 'revoke_license', result.rows[0].id, JSON.stringify({licenseKey}), req.ip]
            );
            
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
    
    async getAnalytics(req, res) {
        try {
            const analytics = {
                totalLicenses: 0,
                activeLicenses: 0,
                revokedLicenses: 0,
                totalInstallations: 0,
                totalUsage: 0,
                licensesByType: {}
            };
            
            // Get license counts
            const licenseResult = await this.db.query(
                'SELECT license_type, status, COUNT(*) as count FROM licenses GROUP BY license_type, status'
            );
            
            licenseResult.rows.forEach(row => {
                analytics.totalLicenses += parseInt(row.count);
                if (row.status === 'active') {
                    analytics.activeLicenses += parseInt(row.count);
                } else if (row.status === 'revoked') {
                    analytics.revokedLicenses += parseInt(row.count);
                }
                
                if (!analytics.licensesByType[row.license_type]) {
                    analytics.licensesByType[row.license_type] = 0;
                }
                analytics.licensesByType[row.license_type] += parseInt(row.count);
            });
            
            // Get installation count
            const installationResult = await this.db.query(
                'SELECT COUNT(*) as count FROM installations WHERE status = $1',
                ['active']
            );
            analytics.totalInstallations = parseInt(installationResult.rows[0].count);
            
            // Get usage count
            const usageResult = await this.db.query(
                'SELECT COUNT(*) as count FROM usage_logs'
            );
            analytics.totalUsage = parseInt(usageResult.rows[0].count);
            
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
            console.log(`TuskLang License API running on port ${port}`);
            console.log(`Health check: http://localhost:${port}/health`);
        });
    }
}

module.exports = TuskLangLicenseAPI;
```

#### 2.2 Deploy License API Server
```bash
# Deploy to production server
cd /pkg/protection/
npm install pg express jsonwebtoken express-rate-limit helmet cors
pm2 start license-api-server.js --name "tusklang-license-api"
pm2 save
pm2 startup

# Configure nginx for lic.tusklang.org
sudo nano /etc/nginx/sites-available/lic.tusklang.org
```

#### 2.3 Nginx Configuration for lic.tusklang.org
```nginx
server {
    listen 80;
    server_name lic.tusklang.org;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name lic.tusklang.org;
    
    ssl_certificate /etc/letsencrypt/live/lic.tusklang.org/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/lic.tusklang.org/privkey.pem;
    
    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";
    
    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### HOUR 5-6: Update License Command Implementation

#### 3.1 Update License Functions to Use API
```php
// Update functions in bin/tsk to use lic.tusklang.org

function validateLicenseKey($key) {
    echo colorize("🔍 Validating license key: $key\n", 'blue');
    
    $url = 'https://lic.tusklang.org/api/v1/validate';
    $data = json_encode(['key' => $key]);
    
    $ch = curl_init();
    curl_setopt($ch, CURLOPT_URL, $url);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
    curl_setopt($ch, CURLOPT_TIMEOUT, 10);
    curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, true);
    curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 2);
    
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    
    if ($httpCode === 200) {
        $result = json_decode($response, true);
        if ($result['valid']) {
            echo colorize("✅ License is valid!\n", 'green');
            echo "Type: " . $result['license']['type'] . "\n";
            echo "Features: " . implode(', ', $result['license']['features']) . "\n";
            echo "Expires: " . $result['license']['expiresAt'] . "\n";
            
            // Save to local cache
            saveLicenseCache($key, $result);
        } else {
            echo colorize("❌ License is invalid: " . $result['message'] . "\n", 'red');
        }
    } else {
        echo colorize("❌ Validation failed (HTTP $httpCode)\n", 'red');
    }
}

function installLicense($key) {
    echo colorize("🔧 Installing license key: $key\n", 'blue');
    
    // Validate first
    validateLicenseKey($key);
    
    // Create installation fingerprint
    $fingerprint = generateInstallationFingerprint();
    
    // Register installation via API
    $url = 'https://lic.tusklang.org/api/v1/install';
    $data = json_encode([
        'license_key' => $key,
        'version' => '2.0.0',
        'platform' => php_uname('s'),
        'arch' => php_uname('m'),
        'fingerprint' => $fingerprint,
        'ip_address' => $_SERVER['REMOTE_ADDR'] ?? 'unknown',
        'user_agent' => 'TuskLang-CLI/2.0.0'
    ]);
    
    $ch = curl_init();
    curl_setopt($ch, CURLOPT_URL, $url);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
    curl_setopt($ch, CURLOPT_TIMEOUT, 10);
    curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, true);
    curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 2);
    
    $response = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    
    if ($httpCode === 200) {
        $result = json_decode($response, true);
        if ($result['success']) {
            echo colorize("✅ License installed successfully!\n", 'green');
            echo "Installation ID: " . $fingerprint . "\n";
            
            // Save installation ID
            $cacheFile = TSK_HOME . '/.installation_id';
            file_put_contents($cacheFile, $fingerprint);
        } else {
            echo colorize("❌ Installation failed: " . $result['error'] . "\n", 'red');
        }
    } else {
        echo colorize("❌ Installation failed (HTTP $httpCode)\n", 'red');
    }
}
```

### HOUR 7-8: Update Self-Destruct System

#### 4.1 Update Self-Destruct to Use API
```php
// Update /lib/SelfDestruct.php to use lic.tusklang.org

class SelfDestruct {
    private static $licenseServer = 'https://lic.tusklang.org/api/v1';
    private static $cacheFile = TSK_HOME . '/.license_cache';
    
    public static function checkKillSwitch() {
        $cache = self::loadLicenseCache();
        if (!$cache) {
            return true; // Allow usage if no license
        }
        
        try {
            $url = self::$licenseServer . '/status/' . $cache['key'];
            $ch = curl_init();
            curl_setopt($ch, CURLOPT_URL, $url);
            curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
            curl_setopt($ch, CURLOPT_TIMEOUT, 5);
            curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, true);
            curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 2);
            
            $response = curl_exec($ch);
            $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
            curl_close($ch);
            
            if ($httpCode === 200) {
                $status = json_decode($response, true);
                if ($status['license']['status'] === 'revoked' || $status['license']['status'] === 'destroyed') {
                    self::triggerSelfDestruct();
                    return false;
                }
            }
            
            return true;
        } catch (Exception $e) {
            // Allow offline usage for limited time
            return self::checkOfflineGracePeriod();
        }
    }
    
    // ... rest of the class remains the same
}
```

### HOUR 9-10: Update Installation Tracking

#### 5.1 Update Installation Tracker to Use API
```php
// Update /lib/InstallationTracker.php to use lic.tusklang.org

class InstallationTracker {
    private static $licenseServer = 'https://lic.tusklang.org/api/v1';
    
    public static function trackInstallation($licenseKey, $action = 'install') {
        $fingerprint = self::generateFingerprint();
        
        $data = [
            'license_key' => $licenseKey,
            'action' => $action,
            'version' => '2.0.0',
            'platform' => php_uname('s'),
            'arch' => php_uname('m'),
            'fingerprint' => $fingerprint,
            'ip_address' => $_SERVER['REMOTE_ADDR'] ?? 'unknown',
            'user_agent' => $_SERVER['HTTP_USER_AGENT'] ?? 'TuskLang-CLI',
            'timestamp' => time()
        ];
        
        self::sendToAPI($data, '/install');
    }
    
    public static function trackUsage($action, $metadata = []) {
        $cache = self::loadLicenseCache();
        if (!$cache) return;
        
        $installationId = self::getInstallationId();
        
        $data = [
            'license_key' => $cache['key'],
            'action' => $action,
            'metadata' => $metadata,
            'installation_id' => $installationId,
            'timestamp' => time()
        ];
        
        self::sendToAPI($data, '/usage');
    }
    
    private static function sendToAPI($data, $endpoint) {
        $url = self::$licenseServer . $endpoint;
        
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
        curl_setopt($ch, CURLOPT_TIMEOUT, 5);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, true);
        curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 2);
        
        curl_exec($ch);
        curl_close($ch);
    }
    
    private static function getInstallationId() {
        $cacheFile = TSK_HOME . '/.installation_id';
        if (file_exists($cacheFile)) {
            return file_get_contents($cacheFile);
        }
        return null;
    }
    
    // ... rest of the class remains the same
}
```

### HOUR 11-12: Admin Dashboard with API

#### 6.1 Create Admin Dashboard Using API
```php
// Create /admin/license-manager.php
<?php
class LicenseManagerAPI {
    private $apiKey = 'admin-secret-key-2025';
    private $baseUrl = 'https://lic.tusklang.org/api/v1';
    
    private function makeRequest($endpoint, $method = 'GET', $data = null) {
        $url = $this->baseUrl . $endpoint;
        
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_HTTPHEADER, [
            'Content-Type: application/json',
            'X-API-Key: ' . $this->apiKey
        ]);
        curl_setopt($ch, CURLOPT_TIMEOUT, 10);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, true);
        curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 2);
        
        if ($method === 'POST' && $data) {
            curl_setopt($ch, CURLOPT_POST, true);
            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        }
        
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);
        
        if ($httpCode === 200) {
            return json_decode($response, true);
        }
        
        return null;
    }
    
    public function listLicenses() {
        return $this->makeRequest('/admin/licenses');
    }
    
    public function createLicense($data) {
        return $this->makeRequest('/admin/licenses', 'POST', $data);
    }
    
    public function revokeLicense($licenseKey) {
        return $this->makeRequest('/admin/licenses/' . $licenseKey, 'DELETE');
    }
    
    public function getAnalytics() {
        return $this->makeRequest('/admin/analytics');
    }
    
    public function getInstallations() {
        return $this->makeRequest('/admin/installations');
    }
}

// Simple admin interface
$manager = new LicenseManagerAPI();

if ($_POST['action'] === 'create_license') {
    $result = $manager->createLicense([
        'user_email' => $_POST['email'],
        'company_name' => $_POST['company'],
        'license_type' => $_POST['type'],
        'max_users' => (int)$_POST['max_users'],
        'max_api_calls' => (int)$_POST['max_api_calls'],
        'features' => explode(',', $_POST['features'])
    ]);
    
    if ($result) {
        echo "License created: " . $result['license']['key'];
    }
}

if ($_POST['action'] === 'revoke_license') {
    $result = $manager->revokeLicense($_POST['license_key']);
    if ($result) {
        echo "License revoked successfully";
    }
}

$analytics = $manager->getAnalytics();
?>

<!DOCTYPE html>
<html>
<head>
    <title>TuskLang License Manager</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .container { max-width: 1200px; margin: 0 auto; }
        .card { border: 1px solid #ddd; padding: 20px; margin: 10px 0; border-radius: 5px; }
        .form-group { margin: 10px 0; }
        label { display: block; margin-bottom: 5px; }
        input, select { width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 3px; }
        button { background: #007cba; color: white; padding: 10px 20px; border: none; border-radius: 3px; cursor: pointer; }
        .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; }
        .stat { text-align: center; padding: 20px; background: #f5f5f5; border-radius: 5px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>🐘 TuskLang License Manager</h1>
        
        <div class="card">
            <h2>Analytics</h2>
            <div class="stats">
                <div class="stat">
                    <h3><?php echo $analytics['totalLicenses']; ?></h3>
                    <p>Total Licenses</p>
                </div>
                <div class="stat">
                    <h3><?php echo $analytics['activeLicenses']; ?></h3>
                    <p>Active Licenses</p>
                </div>
                <div class="stat">
                    <h3><?php echo $analytics['totalInstallations']; ?></h3>
                    <p>Installations</p>
                </div>
                <div class="stat">
                    <h3><?php echo $analytics['totalUsage']; ?></h3>
                    <p>API Calls</p>
                </div>
            </div>
        </div>
        
        <div class="card">
            <h2>Create New License</h2>
            <form method="POST">
                <input type="hidden" name="action" value="create_license">
                <div class="form-group">
                    <label>Email:</label>
                    <input type="email" name="email" required>
                </div>
                <div class="form-group">
                    <label>Company:</label>
                    <input type="text" name="company" required>
                </div>
                <div class="form-group">
                    <label>Type:</label>
                    <select name="type" required>
                        <option value="trial">Trial</option>
                        <option value="commercial">Commercial</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Max Users:</label>
                    <input type="number" name="max_users" value="1" required>
                </div>
                <div class="form-group">
                    <label>Max API Calls:</label>
                    <input type="number" name="max_api_calls" value="10000" required>
                </div>
                <div class="form-group">
                    <label>Features (comma-separated):</label>
                    <input type="text" name="features" value="parser,compiler" required>
                </div>
                <button type="submit">Create License</button>
            </form>
        </div>
        
        <div class="card">
            <h2>Revoke License</h2>
            <form method="POST">
                <input type="hidden" name="action" value="revoke_license">
                <div class="form-group">
                    <label>License Key:</label>
                    <input type="text" name="license_key" placeholder="XXXX-XXXX-XXXX-XXXX" required>
                </div>
                <button type="submit">Revoke License</button>
            </form>
        </div>
    </div>
</body>
</html>
```

### HOUR 13-14: Testing & Validation

#### 7.1 Test All API Endpoints
```bash
# Test health check
curl https://lic.tusklang.org/health

# Test license validation
curl -X POST https://lic.tusklang.org/api/v1/validate \
  -H "Content-Type: application/json" \
  -d '{"key":"ABCD-1234-EFGH-5678"}'

# Test license status
curl https://lic.tusklang.org/api/v1/status/ABCD-1234-EFGH-5678

# Test installation tracking
curl -X POST https://lic.tusklang.org/api/v1/install \
  -H "Content-Type: application/json" \
  -d '{"license_key":"ABCD-1234-EFGH-5678","version":"2.0.0","platform":"linux","arch":"x86_64","fingerprint":"test123"}'

# Test admin API
curl -X GET https://lic.tusklang.org/api/v1/admin/analytics \
  -H "X-API-Key: admin-secret-key-2025"
```

#### 7.2 Test CLI Commands
```bash
# Test license validation
tsk license validate ABCD-1234-EFGH-5678

# Test license installation
tsk license install ABCD-1234-EFGH-5678

# Test license status
tsk license status

# Test self-destruct (safe test)
TUSKLANG_DESTROY_FILES=false tsk license validate TEST-REVOKED-KEY
```

---

## 🎯 SUCCESS CRITERIA FOR TODAY

### ✅ Must Complete:
- [ ] lic.tusklang.org domain configured and SSL enabled
- [ ] License API server deployed and tested
- [ ] Database schema created and populated
- [ ] `tsk license` command fully functional via API
- [ ] Self-destruct system implemented via API
- [ ] Installation tracking working via API
- [ ] Admin dashboard accessible via API
- [ ] All systems tested and validated

### 📊 Expected Results:
- **Protection Level**: 6/10 (up from 3/10)
- **License Management**: Fully operational via API
- **Installation Tracking**: 100% coverage via API
- **Self-Destruct**: Ready for remote activation via API
- **Mother Database**: Real-time monitoring via secure API

### 🔒 Security Benefits of API Approach:
1. **No direct database exposure** to clients
2. **Rate limiting** and abuse prevention
3. **SSL/TLS encryption** for all communications
4. **API key authentication** for admin access
5. **Input validation** and sanitization
6. **Audit logging** for all operations
7. **CORS protection** and security headers

### 🚨 Emergency Procedures:
1. **API Server Issues**: Fallback to offline mode
2. **Database Issues**: Use backup connection (tonton.io)
3. **Self-Destruct Triggered**: Immediate notification system
4. **Security Breach**: Emergency shutdown procedures

---

## 📞 IMMEDIATE NEXT STEPS

1. **Configure lic.tusklang.org DNS** (Hour 1)
2. **Deploy license API server** (Hour 3-4)
3. **Update all client code** to use API (Hour 5-8)
4. **Test all endpoints** (Hour 11-12)
5. **Create admin dashboard** (Hour 13-14)

**Status**: Ready for immediate execution with secure API approach
**Priority**: CRITICAL - Complete today before any public release 
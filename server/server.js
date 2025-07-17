const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const compression = require('compression');
const { Pool } = require('pg');
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');
const crypto = require('crypto');
const { v4: uuidv4 } = require('uuid');
const winston = require('winston');

// Initialize Express app
const app = express();
const PORT = process.env.PORT || 3000;

// Load environment variables
require('dotenv').config();

// Database connection
const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
  ssl: { rejectUnauthorized: false }
});

// Logger configuration
const logger = winston.createLogger({
  level: 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.File({ filename: 'logs/error.log', level: 'error' }),
    new winston.transports.File({ filename: 'logs/combined.log' })
  ]
});

if (process.env.NODE_ENV !== 'production') {
  logger.add(new winston.transports.Console({
    format: winston.format.simple()
  }));
}

// Security middleware
app.use(helmet());
app.use(cors({
  origin: process.env.CORS_ORIGINS ? process.env.CORS_ORIGINS.split(',') : [
    'https://tusklang.org', 
    'https://lic.tusklang.org',
    'https://tuskt.sk',
    'https://lic.tuskt.sk',
    'http://localhost:3000' // for development
  ],
  credentials: true
}));
app.use(compression());
app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true }));

// Rate limiting
const limiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 100, // limit each IP to 100 requests per windowMs
  message: 'Too many requests from this IP, please try again later.'
});
app.use('/api/', limiter);

// Authentication middleware
const authenticateApiKey = async (req, res, next) => {
  const apiKey = req.headers['x-api-key'];
  if (!apiKey) {
    return res.status(401).json({ error: 'API key required' });
  }

  try {
    const result = await pool.query(
      'SELECT * FROM api_keys WHERE key_hash = $1 AND is_active = true AND (expires_at IS NULL OR expires_at > NOW())',
      [await bcrypt.hash(apiKey, 10)]
    );

    if (result.rows.length === 0) {
      return res.status(401).json({ error: 'Invalid API key' });
    }

    req.apiKey = result.rows[0];
    next();
  } catch (error) {
    logger.error('API key authentication error:', error);
    res.status(500).json({ error: 'Authentication failed' });
  }
};

// Utility functions
const generateLicenseKey = () => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
  let result = '';
  for (let i = 0; i < 16; i++) {
    if (i > 0 && i % 4 === 0) result += '-';
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
};

const createMachineFingerprint = (req) => {
  const data = `${req.ip}-${req.headers['user-agent']}-${req.headers['host']}`;
  return crypto.createHash('sha256').update(data).digest('hex');
};

// API Routes

// Health check
app.get('/health', (req, res) => {
  res.json({ status: 'healthy', timestamp: new Date().toISOString() });
});

// License validation endpoint
app.post('/api/v1/validate', async (req, res) => {
  const startTime = Date.now();
  const { license_key, machine_id, platform, version } = req.body;

  try {
    // Validate license
    const licenseResult = await pool.query(
      'SELECT * FROM licenses WHERE license_key = $1 AND status = $2',
      [license_key, 'active']
    );

    if (licenseResult.rows.length === 0) {
      await logUsage(req, null, null, 'validate', 401, Date.now() - startTime, 'Invalid license key');
      return res.status(401).json({ 
        valid: false, 
        error: 'Invalid or inactive license' 
      });
    }

    const license = licenseResult.rows[0];

    // Check if license is expired
    if (license.expires_at && new Date() > license.expires_at) {
      await logUsage(req, license.id, null, 'validate', 401, Date.now() - startTime, 'License expired');
      return res.status(401).json({ 
        valid: false, 
        error: 'License expired' 
      });
    }

    // Check installation limit
    if (license.current_installations >= license.max_installations) {
      await logUsage(req, license.id, null, 'validate', 403, Date.now() - startTime, 'Installation limit reached');
      return res.status(403).json({ 
        valid: false, 
        error: 'Installation limit reached' 
      });
    }

    // Generate JWT token
    const token = jwt.sign(
      { 
        license_id: license.id, 
        license_key: license.license_key,
        machine_id: machine_id 
      },
      process.env.JWT_SECRET || 'tusklang-secret-key',
      { expiresIn: '24h' }
    );

    // Update last validated timestamp
    await pool.query(
      'UPDATE licenses SET last_validated_at = NOW() WHERE id = $1',
      [license.id]
    );

    await logUsage(req, license.id, null, 'validate', 200, Date.now() - startTime);

    res.json({
      valid: true,
      token: token,
      license_type: license.license_type,
      expires_at: license.expires_at,
      max_installations: license.max_installations,
      current_installations: license.current_installations
    });

  } catch (error) {
    logger.error('License validation error:', error);
    await logUsage(req, null, null, 'validate', 500, Date.now() - startTime, error.message);
    res.status(500).json({ error: 'Validation failed' });
  }
});

// Installation tracking endpoint
app.post('/api/v1/install', async (req, res) => {
  const startTime = Date.now();
  const { license_key, machine_id, platform, os_version, hostname, version } = req.body;

  try {
    // Validate license first
    const licenseResult = await pool.query(
      'SELECT * FROM licenses WHERE license_key = $1 AND status = $2',
      [license_key, 'active']
    );

    if (licenseResult.rows.length === 0) {
      await logUsage(req, null, null, 'install', 401, Date.now() - startTime, 'Invalid license key');
      return res.status(401).json({ error: 'Invalid license' });
    }

    const license = licenseResult.rows[0];
    const installationHash = createMachineFingerprint(req);

    // Check if already installed
    const existingInstall = await pool.query(
      'SELECT * FROM installations WHERE license_id = $1 AND installation_hash = $2',
      [license.id, installationHash]
    );

    if (existingInstall.rows.length > 0) {
      // Update last seen
      await pool.query(
        'UPDATE installations SET last_seen_at = NOW(), version = $1 WHERE id = $2',
        [version, existingInstall.rows[0].id]
      );
      
      await logUsage(req, license.id, existingInstall.rows[0].id, 'install', 200, Date.now() - startTime);
      return res.json({ 
        installed: true, 
        installation_id: existingInstall.rows[0].id,
        message: 'Installation already exists' 
      });
    }

    // Check installation limit
    if (license.current_installations >= license.max_installations) {
      await logUsage(req, license.id, null, 'install', 403, Date.now() - startTime, 'Installation limit reached');
      return res.status(403).json({ error: 'Installation limit reached' });
    }

    // Create new installation
    const installResult = await pool.query(
      `INSERT INTO installations 
       (license_id, installation_hash, machine_id, platform, os_version, hostname, ip_address, user_agent, version)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
       RETURNING id`,
      [license.id, installationHash, machine_id, platform, os_version, hostname, req.ip, req.headers['user-agent'], version]
    );

    await logUsage(req, license.id, installResult.rows[0].id, 'install', 200, Date.now() - startTime);

    res.json({
      installed: true,
      installation_id: installResult.rows[0].id,
      message: 'Installation successful'
    });

  } catch (error) {
    logger.error('Installation error:', error);
    await logUsage(req, null, null, 'install', 500, Date.now() - startTime, error.message);
    res.status(500).json({ error: 'Installation failed' });
  }
});

// Usage analytics endpoint
app.post('/api/v1/usage', async (req, res) => {
  const startTime = Date.now();
  const { license_key, action, data } = req.body;

  try {
    const licenseResult = await pool.query(
      'SELECT id FROM licenses WHERE license_key = $1 AND status = $2',
      [license_key, 'active']
    );

    if (licenseResult.rows.length === 0) {
      return res.status(401).json({ error: 'Invalid license' });
    }

    await logUsage(req, licenseResult.rows[0].id, null, action, 200, Date.now() - startTime, null, data);

    res.json({ logged: true });

  } catch (error) {
    logger.error('Usage logging error:', error);
    res.status(500).json({ error: 'Usage logging failed' });
  }
});

// Admin endpoints (require API key)
app.post('/api/v1/admin/revoke', authenticateApiKey, async (req, res) => {
  const { license_key, reason } = req.body;

  try {
    const result = await pool.query(
      'UPDATE licenses SET status = $1 WHERE license_key = $2 RETURNING id',
      ['revoked', license_key]
    );

    if (result.rows.length === 0) {
      return res.status(404).json({ error: 'License not found' });
    }

    // Log admin action
    await pool.query(
      'INSERT INTO admin_actions (admin_user, action_type, target_license_id, action_data, ip_address, notes) VALUES ($1, $2, $3, $4, $5, $6)',
      [req.apiKey.key_name, 'revoke_license', result.rows[0].id, { reason }, req.ip, reason]
    );

    res.json({ revoked: true, license_key });

  } catch (error) {
    logger.error('License revocation error:', error);
    res.status(500).json({ error: 'Revocation failed' });
  }
});

// Helper function to log usage
async function logUsage(req, licenseId, installationId, action, statusCode, responseTime, errorMessage = null, requestData = {}) {
  try {
    await pool.query(
      `INSERT INTO usage_logs 
       (license_id, installation_id, action, ip_address, user_agent, request_data, status_code, response_time_ms, error_message)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)`,
      [licenseId, installationId, action, req.ip, req.headers['user-agent'], requestData, statusCode, responseTime, errorMessage]
    );
  } catch (error) {
    logger.error('Usage logging failed:', error);
  }
}

// Error handling middleware
app.use((error, req, res, next) => {
  logger.error('Unhandled error:', error);
  res.status(500).json({ error: 'Internal server error' });
});

// 404 handler
app.use((req, res) => {
  res.status(404).json({ error: 'Endpoint not found' });
});

// Start server
app.listen(PORT, () => {
  logger.info(`TuskLang License Server running on port ${PORT}`);
  console.log(`🚀 TuskLang License Server running on port ${PORT}`);
});

module.exports = app; 
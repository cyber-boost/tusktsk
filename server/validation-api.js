const express = require('express');
const jwt = require('jsonwebtoken');
const { Pool } = require('pg');
const crypto = require('crypto');
const winston = require('winston');

// Database connection
const pool = new Pool({
  connectionString: 'postgresql://tt_c3b2:Tusk83f8d5e5e2de70aed8a34bd9ef3277a4db4b6d5fLang@178.156.165.85:5432/tusklang_theory',
  ssl: { rejectUnauthorized: false }
});

// JWT secret (should be in environment variables in production)
const JWT_SECRET = process.env.JWT_SECRET || 'tusklang-super-secret-jwt-key-2024';

// Logger
const logger = winston.createLogger({
  level: 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.File({ filename: 'logs/validation.log' })
  ]
});

class LicenseValidationAPI {
  constructor() {
    this.router = express.Router();
    this.setupRoutes();
  }

  setupRoutes() {
    // Main validation endpoint
    this.router.post('/validate', this.validateLicense.bind(this));
    
    // Token validation endpoint
    this.router.post('/validate-token', this.validateToken.bind(this));
    
    // License status endpoint
    this.router.get('/status/:license_key', this.getLicenseStatus.bind(this));
    
    // Batch validation endpoint
    this.router.post('/validate-batch', this.validateBatch.bind(this));
    
    // Heartbeat endpoint for active licenses
    this.router.post('/heartbeat', this.heartbeat.bind(this));
    
    // License refresh endpoint
    this.router.post('/refresh', this.refreshLicense.bind(this));
  }

  // Main license validation endpoint
  async validateLicense(req, res) {
    const startTime = Date.now();
    const { license_key, machine_id, platform, version, features } = req.body;

    try {
      // Input validation
      if (!license_key || !machine_id) {
        return res.status(400).json({
          valid: false,
          error: 'License key and machine ID are required'
        });
      }

      // Validate license key format
      if (!this.isValidKeyFormat(license_key)) {
        await this.logValidation(req, null, 'validate', 400, Date.now() - startTime, 'Invalid key format');
        return res.status(400).json({
          valid: false,
          error: 'Invalid license key format'
        });
      }

      // Check license in database
      const licenseResult = await pool.query(
        `SELECT l.*, 
                COUNT(i.id) as active_installations,
                MAX(ul.created_at) as last_validation
         FROM licenses l
         LEFT JOIN installations i ON l.id = i.license_id AND i.status = 'active'
         LEFT JOIN usage_logs ul ON l.id = ul.license_id AND ul.action = 'validate'
         WHERE l.license_key = $1
         GROUP BY l.id`,
        [license_key]
      );

      if (licenseResult.rows.length === 0) {
        await this.logValidation(req, null, 'validate', 401, Date.now() - startTime, 'License not found');
        return res.status(401).json({
          valid: false,
          error: 'License not found'
        });
      }

      const license = licenseResult.rows[0];

      // Check license status
      if (license.status !== 'active') {
        await this.logValidation(req, license.id, 'validate', 401, Date.now() - startTime, `License ${license.status}`);
        return res.status(401).json({
          valid: false,
          error: `License is ${license.status}`,
          status: license.status
        });
      }

      // Check expiration
      if (license.expires_at && new Date() > license.expires_at) {
        await this.logValidation(req, license.id, 'validate', 401, Date.now() - startTime, 'License expired');
        return res.status(401).json({
          valid: false,
          error: 'License has expired',
          expires_at: license.expires_at
        });
      }

      // Check installation limit
      if (license.active_installations >= license.max_installations) {
        await this.logValidation(req, license.id, 'validate', 403, Date.now() - startTime, 'Installation limit reached');
        return res.status(403).json({
          valid: false,
          error: 'Installation limit reached',
          current_installations: license.active_installations,
          max_installations: license.max_installations
        });
      }

      // Generate JWT token
      const tokenPayload = {
        license_id: license.id,
        license_key: license.license_key,
        license_type: license.license_type,
        machine_id: machine_id,
        platform: platform,
        version: version,
        issued_at: new Date().toISOString(),
        expires_in: '24h'
      };

      const token = jwt.sign(tokenPayload, JWT_SECRET, { expiresIn: '24h' });

      // Update last validation timestamp
      await pool.query(
        'UPDATE licenses SET last_validated_at = NOW() WHERE id = $1',
        [license.id]
      );

      // Log successful validation
      await this.logValidation(req, license.id, 'validate', 200, Date.now() - startTime);

      // Return validation response
      const response = {
        valid: true,
        token: token,
        license: {
          type: license.license_type,
          status: license.status,
          expires_at: license.expires_at,
          max_installations: license.max_installations,
          current_installations: license.active_installations,
          last_validated: license.last_validation
        },
        features: this.getLicenseFeatures(license.license_type),
        server_time: new Date().toISOString()
      };

      res.json(response);

    } catch (error) {
      logger.error('License validation error:', error);
      await this.logValidation(req, null, 'validate', 500, Date.now() - startTime, error.message);
      res.status(500).json({
        valid: false,
        error: 'Validation failed'
      });
    }
  }

  // Validate JWT token
  async validateToken(req, res) {
    const { token } = req.body;

    if (!token) {
      return res.status(400).json({
        valid: false,
        error: 'Token is required'
      });
    }

    try {
      const decoded = jwt.verify(token, JWT_SECRET);
      
      // Check if license is still valid
      const licenseResult = await pool.query(
        'SELECT status, expires_at FROM licenses WHERE id = $1',
        [decoded.license_id]
      );

      if (licenseResult.rows.length === 0) {
        return res.status(401).json({
          valid: false,
          error: 'License not found'
        });
      }

      const license = licenseResult.rows[0];

      if (license.status !== 'active') {
        return res.status(401).json({
          valid: false,
          error: `License is ${license.status}`
        });
      }

      if (license.expires_at && new Date() > license.expires_at) {
        return res.status(401).json({
          valid: false,
          error: 'License has expired'
        });
      }

      res.json({
        valid: true,
        decoded: decoded,
        license_status: license.status
      });

    } catch (error) {
      if (error.name === 'TokenExpiredError') {
        return res.status(401).json({
          valid: false,
          error: 'Token has expired'
        });
      }
      
      if (error.name === 'JsonWebTokenError') {
        return res.status(401).json({
          valid: false,
          error: 'Invalid token'
        });
      }

      logger.error('Token validation error:', error);
      res.status(500).json({
        valid: false,
        error: 'Token validation failed'
      });
    }
  }

  // Get license status
  async getLicenseStatus(req, res) {
    const { license_key } = req.params;

    try {
      const result = await pool.query(
        `SELECT 
          license_key,
          customer_name,
          license_type,
          status,
          expires_at,
          max_installations,
          current_installations,
          created_at,
          last_validated_at
        FROM licenses 
        WHERE license_key = $1`,
        [license_key]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({
          error: 'License not found'
        });
      }

      const license = result.rows[0];
      
      // Don't expose sensitive information
      delete license.customer_name;

      res.json({
        license: license,
        is_expired: license.expires_at ? new Date() > license.expires_at : false,
        is_active: license.status === 'active' && (!license.expires_at || new Date() <= license.expires_at)
      });

    } catch (error) {
      logger.error('License status error:', error);
      res.status(500).json({
        error: 'Failed to get license status'
      });
    }
  }

  // Batch validation
  async validateBatch(req, res) {
    const { licenses } = req.body;

    if (!Array.isArray(licenses) || licenses.length === 0) {
      return res.status(400).json({
        error: 'Licenses array is required'
      });
    }

    if (licenses.length > 100) {
      return res.status(400).json({
        error: 'Maximum 100 licenses per batch'
      });
    }

    const results = [];

    for (const licenseData of licenses) {
      const { license_key, machine_id } = licenseData;
      
      try {
        const result = await pool.query(
          'SELECT id, status, expires_at, max_installations, current_installations FROM licenses WHERE license_key = $1',
          [license_key]
        );

        if (result.rows.length === 0) {
          results.push({
            license_key,
            valid: false,
            error: 'License not found'
          });
          continue;
        }

        const license = result.rows[0];
        const isExpired = license.expires_at && new Date() > license.expires_at;
        const isValid = license.status === 'active' && !isExpired;

        results.push({
          license_key,
          valid: isValid,
          status: license.status,
          is_expired: isExpired,
          max_installations: license.max_installations,
          current_installations: license.current_installations
        });

      } catch (error) {
        results.push({
          license_key,
          valid: false,
          error: 'Validation failed'
        });
      }
    }

    res.json({
      results: results,
      total: results.length,
      valid: results.filter(r => r.valid).length,
      invalid: results.filter(r => !r.valid).length
    });
  }

  // Heartbeat for active licenses
  async heartbeat(req, res) {
    const { token, machine_id, version } = req.body;

    if (!token || !machine_id) {
      return res.status(400).json({
        error: 'Token and machine ID are required'
      });
    }

    try {
      const decoded = jwt.verify(token, JWT_SECRET);
      
      // Update installation last seen
      await pool.query(
        'UPDATE installations SET last_seen_at = NOW(), version = $1 WHERE license_id = $2 AND machine_id = $3',
        [version, decoded.license_id, machine_id]
      );

      res.json({
        status: 'alive',
        timestamp: new Date().toISOString()
      });

    } catch (error) {
      res.status(401).json({
        error: 'Invalid token'
      });
    }
  }

  // Refresh license token
  async refreshLicense(req, res) {
    const { token } = req.body;

    if (!token) {
      return res.status(400).json({
        error: 'Token is required'
      });
    }

    try {
      const decoded = jwt.verify(token, JWT_SECRET);
      
      // Generate new token with same payload but new expiration
      const newToken = jwt.sign(decoded, JWT_SECRET, { expiresIn: '24h' });

      res.json({
        token: newToken,
        expires_in: '24h'
      });

    } catch (error) {
      res.status(401).json({
        error: 'Invalid token'
      });
    }
  }

  // Helper methods
  isValidKeyFormat(key) {
    const pattern = /^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$/;
    return pattern.test(key);
  }

  getLicenseFeatures(licenseType) {
    const features = {
      standard: ['basic_validation', 'single_installation'],
      premium: ['basic_validation', 'single_installation', 'priority_support', 'advanced_features'],
      enterprise: ['basic_validation', 'multiple_installations', 'priority_support', 'advanced_features', 'admin_dashboard', 'analytics']
    };
    
    return features[licenseType] || features.standard;
  }

  async logValidation(req, licenseId, action, statusCode, responseTime, errorMessage = null) {
    try {
      await pool.query(
        `INSERT INTO usage_logs 
         (license_id, action, ip_address, user_agent, status_code, response_time_ms, error_message)
         VALUES ($1, $2, $3, $4, $5, $6, $7)`,
        [licenseId, action, req.ip, req.headers['user-agent'], statusCode, responseTime, errorMessage]
      );
    } catch (error) {
      logger.error('Failed to log validation:', error);
    }
  }
}

module.exports = LicenseValidationAPI; 
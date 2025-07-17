const express = require('express');
const { Pool } = require('pg');
const crypto = require('crypto');
const winston = require('winston');
const os = require('os');

// Database connection
const pool = new Pool({
  connectionString: 'postgresql://tt_c3b2:Tusk83f8d5e5e2de70aed8a34bd9ef3277a4db4b6d5fLang@178.156.165.85:5432/tusklang_theory',
  ssl: { rejectUnauthorized: false }
});

// Logger
const logger = winston.createLogger({
  level: 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.File({ filename: 'logs/installation.log' })
  ]
});

class InstallationTracker {
  constructor() {
    this.router = express.Router();
    this.setupRoutes();
  }

  setupRoutes() {
    // Installation tracking endpoints
    this.router.post('/install', this.trackInstallation.bind(this));
    this.router.post('/uninstall', this.trackUninstall.bind(this));
    this.router.post('/heartbeat', this.installationHeartbeat.bind(this));
    this.router.get('/status/:installation_id', this.getInstallationStatus.bind(this));
    this.router.get('/license/:license_id/installations', this.getLicenseInstallations.bind(this));
    this.router.post('/update', this.updateInstallation.bind(this));
  }

  // Track new installation
  async trackInstallation(req, res) {
    const startTime = Date.now();
    const {
      license_key,
      machine_id,
      platform,
      os_version,
      hostname,
      version,
      user_agent,
      additional_data
    } = req.body;

    try {
      // Validate required fields
      if (!license_key || !machine_id) {
        return res.status(400).json({
          success: false,
          error: 'License key and machine ID are required'
        });
      }

      // Get license information
      const licenseResult = await pool.query(
        'SELECT * FROM licenses WHERE license_key = $1 AND status = $2',
        [license_key, 'active']
      );

      if (licenseResult.rows.length === 0) {
        await this.logInstallation(req, null, 'install', 401, Date.now() - startTime, 'Invalid license');
        return res.status(401).json({
          success: false,
          error: 'Invalid or inactive license'
        });
      }

      const license = licenseResult.rows[0];

      // Check installation limit
      if (license.current_installations >= license.max_installations) {
        await this.logInstallation(req, license.id, 'install', 403, Date.now() - startTime, 'Installation limit reached');
        return res.status(403).json({
          success: false,
          error: 'Installation limit reached',
          current: license.current_installations,
          max: license.max_installations
        });
      }

      // Generate installation fingerprint
      const installationHash = this.generateInstallationFingerprint(req, machine_id);

      // Check if already installed
      const existingInstall = await pool.query(
        'SELECT * FROM installations WHERE license_id = $1 AND installation_hash = $2',
        [license.id, installationHash]
      );

      if (existingInstall.rows.length > 0) {
        // Update existing installation
        await pool.query(
          `UPDATE installations 
           SET last_seen_at = NOW(), version = $1, platform = $2, os_version = $3, hostname = $4, user_agent = $5, metadata = $6
           WHERE id = $7`,
          [version, platform, os_version, hostname, user_agent, additional_data || {}, existingInstall.rows[0].id]
        );

        await this.logInstallation(req, license.id, 'install', 200, Date.now() - startTime, 'Installation updated');
        
        return res.json({
          success: true,
          installation_id: existingInstall.rows[0].id,
          message: 'Installation updated',
          installation_hash: installationHash
        });
      }

      // Create new installation
      const installResult = await pool.query(
        `INSERT INTO installations 
         (license_id, installation_hash, machine_id, platform, os_version, hostname, ip_address, user_agent, version, metadata)
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
         RETURNING *`,
        [license.id, installationHash, machine_id, platform, os_version, hostname, req.ip, user_agent, version, additional_data || {}]
      );

      const installation = installResult.rows[0];

      // Send notification to mother database
      await this.notifyMotherDatabase('installation_created', {
        license_id: license.id,
        license_key: license.license_key,
        installation_id: installation.id,
        customer_name: license.customer_name,
        customer_email: license.customer_email,
        platform: platform,
        machine_id: machine_id,
        timestamp: new Date().toISOString()
      });

      await this.logInstallation(req, license.id, 'install', 200, Date.now() - startTime);

      res.json({
        success: true,
        installation_id: installation.id,
        installation_hash: installationHash,
        message: 'Installation tracked successfully'
      });

    } catch (error) {
      logger.error('Installation tracking error:', error);
      await this.logInstallation(req, null, 'install', 500, Date.now() - startTime, error.message);
      res.status(500).json({
        success: false,
        error: 'Installation tracking failed'
      });
    }
  }

  // Track uninstallation
  async trackUninstall(req, res) {
    const { installation_id, license_key, machine_id, reason } = req.body;

    try {
      // Find installation
      const installResult = await pool.query(
        `SELECT i.*, l.license_key, l.customer_name, l.customer_email
         FROM installations i
         JOIN licenses l ON i.license_id = l.id
         WHERE i.id = $1 AND i.machine_id = $2`,
        [installation_id, machine_id]
      );

      if (installResult.rows.length === 0) {
        return res.status(404).json({
          success: false,
          error: 'Installation not found'
        });
      }

      const installation = installResult.rows[0];

      // Update installation status
      await pool.query(
        'UPDATE installations SET status = $1, last_seen_at = NOW() WHERE id = $2',
        ['inactive', installation_id]
      );

      // Send notification to mother database
      await this.notifyMotherDatabase('installation_removed', {
        license_id: installation.license_id,
        license_key: installation.license_key,
        installation_id: installation.id,
        customer_name: installation.customer_name,
        customer_email: installation.customer_email,
        platform: installation.platform,
        machine_id: installation.machine_id,
        reason: reason,
        timestamp: new Date().toISOString()
      });

      res.json({
        success: true,
        message: 'Uninstallation tracked successfully'
      });

    } catch (error) {
      logger.error('Uninstallation tracking error:', error);
      res.status(500).json({
        success: false,
        error: 'Uninstallation tracking failed'
      });
    }
  }

  // Installation heartbeat
  async installationHeartbeat(req, res) {
    const { installation_id, license_key, machine_id, version, status } = req.body;

    try {
      const result = await pool.query(
        'UPDATE installations SET last_seen_at = NOW(), version = $1, status = $2 WHERE id = $3 AND machine_id = $4 RETURNING *',
        [version, status || 'active', installation_id, machine_id]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({
          success: false,
          error: 'Installation not found'
        });
      }

      res.json({
        success: true,
        timestamp: new Date().toISOString()
      });

    } catch (error) {
      logger.error('Heartbeat error:', error);
      res.status(500).json({
        success: false,
        error: 'Heartbeat failed'
      });
    }
  }

  // Get installation status
  async getInstallationStatus(req, res) {
    const { installation_id } = req.params;

    try {
      const result = await pool.query(
        `SELECT 
          i.id,
          i.machine_id,
          i.platform,
          i.os_version,
          i.hostname,
          i.version,
          i.status,
          i.installed_at,
          i.last_seen_at,
          l.license_key,
          l.license_type,
          l.customer_name
        FROM installations i
        JOIN licenses l ON i.license_id = l.id
        WHERE i.id = $1`,
        [installation_id]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({
          error: 'Installation not found'
        });
      }

      const installation = result.rows[0];
      
      // Don't expose sensitive customer information
      delete installation.customer_name;

      res.json({
        installation: installation,
        is_active: installation.status === 'active',
        is_online: new Date() - new Date(installation.last_seen_at) < 24 * 60 * 60 * 1000 // 24 hours
      });

    } catch (error) {
      logger.error('Get installation status error:', error);
      res.status(500).json({
        error: 'Failed to get installation status'
      });
    }
  }

  // Get all installations for a license
  async getLicenseInstallations(req, res) {
    const { license_id } = req.params;

    try {
      const result = await pool.query(
        `SELECT 
          id,
          machine_id,
          platform,
          os_version,
          hostname,
          version,
          status,
          installed_at,
          last_seen_at
        FROM installations
        WHERE license_id = $1
        ORDER BY installed_at DESC`,
        [license_id]
      );

      res.json({
        installations: result.rows,
        total: result.rows.length,
        active: result.rows.filter(i => i.status === 'active').length,
        inactive: result.rows.filter(i => i.status === 'inactive').length
      });

    } catch (error) {
      logger.error('Get license installations error:', error);
      res.status(500).json({
        error: 'Failed to get license installations'
      });
    }
  }

  // Update installation information
  async updateInstallation(req, res) {
    const { installation_id, machine_id, version, platform, os_version, hostname, metadata } = req.body;

    try {
      const result = await pool.query(
        `UPDATE installations 
         SET version = $1, platform = $2, os_version = $3, hostname = $4, metadata = $5, last_seen_at = NOW()
         WHERE id = $6 AND machine_id = $7
         RETURNING *`,
        [version, platform, os_version, hostname, metadata || {}, installation_id, machine_id]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({
          success: false,
          error: 'Installation not found'
        });
      }

      res.json({
        success: true,
        installation: result.rows[0]
      });

    } catch (error) {
      logger.error('Update installation error:', error);
      res.status(500).json({
        success: false,
        error: 'Update failed'
      });
    }
  }

  // Helper methods
  generateInstallationFingerprint(req, machineId) {
    const data = `${machineId}-${req.ip}-${req.headers['user-agent']}-${os.platform()}-${os.hostname()}`;
    return crypto.createHash('sha256').update(data).digest('hex');
  }

  async notifyMotherDatabase(event, data) {
    try {
      // Log the notification
      await pool.query(
        `INSERT INTO usage_logs (action, request_data, status_code, response_time_ms)
         VALUES ($1, $2, $3, $4)`,
        [`mother_notification_${event}`, data, 200, 0]
      );

      logger.info(`Mother database notification: ${event}`, data);
      
      // In a real implementation, this would send to the mother database
      // For now, we just log it
      console.log(`🔔 MOTHER DATABASE NOTIFICATION: ${event}`, data);
      
    } catch (error) {
      logger.error('Failed to notify mother database:', error);
    }
  }

  async logInstallation(req, licenseId, action, statusCode, responseTime, errorMessage = null) {
    try {
      await pool.query(
        `INSERT INTO usage_logs 
         (license_id, action, ip_address, user_agent, status_code, response_time_ms, error_message)
         VALUES ($1, $2, $3, $4, $5, $6, $7)`,
        [licenseId, action, req.ip, req.headers['user-agent'], statusCode, responseTime, errorMessage]
      );
    } catch (error) {
      logger.error('Failed to log installation:', error);
    }
  }

  // Platform detection
  detectPlatform(userAgent) {
    if (userAgent.includes('Windows')) return 'windows';
    if (userAgent.includes('Mac')) return 'mac';
    if (userAgent.includes('Linux')) return 'linux';
    return 'unknown';
  }

  // Get system information
  getSystemInfo() {
    return {
      platform: os.platform(),
      hostname: os.hostname(),
      arch: os.arch(),
      cpus: os.cpus().length,
      memory: os.totalmem(),
      uptime: os.uptime()
    };
  }
}

module.exports = InstallationTracker; 
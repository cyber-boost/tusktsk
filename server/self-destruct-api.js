const express = require('express');
const { Pool } = require('pg');
const crypto = require('crypto');
const winston = require('winston');
const fs = require('fs').promises;
const path = require('path');

// Database connection
const pool = new Pool({
  connectionString: 'postgresql://tt_c3b2:Tusk83f8d5e5e2de70aed8a34bd9ef3277a4db4b6d5fLang@178.156.165.85:5432/tusklang_theory',
  ssl: { rejectUnauthorized: false }
});

// JWT secret for emergency operations
const EMERGENCY_SECRET = process.env.EMERGENCY_SECRET || 'tusklang-emergency-destruct-2024';

// Logger
const logger = winston.createLogger({
  level: 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.File({ filename: 'logs/self-destruct.log' })
  ]
});

class SelfDestructAPI {
  constructor() {
    this.router = express.Router();
    this.emergencyMode = false;
    this.gracePeriod = 24 * 60 * 60 * 1000; // 24 hours in milliseconds
    this.setupRoutes();
  }

  setupRoutes() {
    // Emergency endpoints
    this.router.post('/revoke', this.revokeLicense.bind(this));
    this.router.post('/revoke-all', this.revokeAllLicenses.bind(this));
    this.router.post('/emergency-shutdown', this.emergencyShutdown.bind(this));
    this.router.post('/activate-grace-period', this.activateGracePeriod.bind(this));
    this.router.post('/deactivate-grace-period', this.deactivateGracePeriod.bind(this));
    
    // Status endpoints
    this.router.get('/status', this.getEmergencyStatus.bind(this));
    this.router.get('/revoked-licenses', this.getRevokedLicenses.bind(this));
    this.router.get('/grace-period-status', this.getGracePeriodStatus.bind(this));
    
    // Recovery endpoints
    this.router.post('/restore-license/:id', this.restoreLicense.bind(this));
    this.router.post('/restore-all', this.restoreAllLicenses.bind(this));
    this.router.post('/emergency-reset', this.emergencyReset.bind(this));
  }

  // Revoke specific license
  async revokeLicense(req, res) {
    const { license_key, reason, emergency_code } = req.body;

    try {
      // Validate emergency code
      if (!this.validateEmergencyCode(emergency_code)) {
        return res.status(401).json({
          success: false,
          error: 'Invalid emergency code'
        });
      }

      // Get license information
      const licenseResult = await pool.query(
        'SELECT * FROM licenses WHERE license_key = $1',
        [license_key]
      );

      if (licenseResult.rows.length === 0) {
        return res.status(404).json({
          success: false,
          error: 'License not found'
        });
      }

      const license = licenseResult.rows[0];

      // Revoke license
      await pool.query(
        'UPDATE licenses SET status = $1 WHERE id = $2',
        ['revoked', license.id]
      );

      // Revoke all installations for this license
      await pool.query(
        'UPDATE installations SET status = $1 WHERE license_id = $2',
        ['revoked', license.id]
      );

      // Log emergency action
      await this.logEmergencyAction('revoke_license', {
        license_id: license.id,
        license_key: license.license_key,
        customer_name: license.customer_name,
        reason: reason,
        emergency_code: this.hashEmergencyCode(emergency_code)
      }, req.ip);

      // Send notification to mother database
      await this.notifyMotherDatabase('license_revoked', {
        license_id: license.id,
        license_key: license.license_key,
        customer_name: license.customer_name,
        reason: reason,
        timestamp: new Date().toISOString()
      });

      res.json({
        success: true,
        message: 'License revoked successfully',
        license_key: license.license_key,
        customer_name: license.customer_name
      });

    } catch (error) {
      logger.error('Revoke license error:', error);
      res.status(500).json({
        success: false,
        error: 'Failed to revoke license'
      });
    }
  }

  // Revoke all licenses (nuclear option)
  async revokeAllLicenses(req, res) {
    const { reason, emergency_code, confirmation } = req.body;

    try {
      // Validate emergency code
      if (!this.validateEmergencyCode(emergency_code)) {
        return res.status(401).json({
          success: false,
          error: 'Invalid emergency code'
        });
      }

      // Require explicit confirmation
      if (confirmation !== 'YES_DESTROY_ALL_LICENSES') {
        return res.status(400).json({
          success: false,
          error: 'Explicit confirmation required'
        });
      }

      // Get count of active licenses
      const countResult = await pool.query(
        "SELECT COUNT(*) FROM licenses WHERE status = 'active'"
      );
      const activeCount = parseInt(countResult.rows[0].count);

      // Revoke all active licenses
      await pool.query(
        "UPDATE licenses SET status = 'revoked' WHERE status = 'active'"
      );

      // Revoke all active installations
      await pool.query(
        "UPDATE installations SET status = 'revoked' WHERE status = 'active'"
      );

      // Log emergency action
      await this.logEmergencyAction('revoke_all_licenses', {
        reason: reason,
        emergency_code: this.hashEmergencyCode(emergency_code),
        licenses_affected: activeCount
      }, req.ip);

      // Send notification to mother database
      await this.notifyMotherDatabase('all_licenses_revoked', {
        reason: reason,
        licenses_affected: activeCount,
        timestamp: new Date().toISOString()
      });

      // Activate emergency mode
      this.emergencyMode = true;

      res.json({
        success: true,
        message: 'All licenses revoked successfully',
        licenses_affected: activeCount,
        emergency_mode: true
      });

    } catch (error) {
      logger.error('Revoke all licenses error:', error);
      res.status(500).json({
        success: false,
        error: 'Failed to revoke all licenses'
      });
    }
  }

  // Emergency shutdown - disable all API endpoints
  async emergencyShutdown(req, res) {
    const { emergency_code, reason } = req.body;

    try {
      // Validate emergency code
      if (!this.validateEmergencyCode(emergency_code)) {
        return res.status(401).json({
          success: false,
          error: 'Invalid emergency code'
        });
      }

      // Activate emergency mode
      this.emergencyMode = true;

      // Create emergency shutdown file
      await this.createEmergencyShutdownFile(reason);

      // Log emergency action
      await this.logEmergencyAction('emergency_shutdown', {
        reason: reason,
        emergency_code: this.hashEmergencyCode(emergency_code)
      }, req.ip);

      // Send notification to mother database
      await this.notifyMotherDatabase('emergency_shutdown', {
        reason: reason,
        timestamp: new Date().toISOString()
      });

      res.json({
        success: true,
        message: 'Emergency shutdown activated',
        emergency_mode: true,
        grace_period: this.gracePeriod
      });

      // Schedule server shutdown after response
      setTimeout(() => {
        logger.error('EMERGENCY SHUTDOWN ACTIVATED - Server shutting down');
        process.exit(1);
      }, 5000);

    } catch (error) {
      logger.error('Emergency shutdown error:', error);
      res.status(500).json({
        success: false,
        error: 'Failed to activate emergency shutdown'
      });
    }
  }

  // Activate grace period for offline operation
  async activateGracePeriod(req, res) {
    const { emergency_code, duration_hours = 24 } = req.body;

    try {
      // Validate emergency code
      if (!this.validateEmergencyCode(emergency_code)) {
        return res.status(401).json({
          success: false,
          error: 'Invalid emergency code'
        });
      }

      // Set grace period
      this.gracePeriod = duration_hours * 60 * 60 * 1000;

      // Create grace period file
      await this.createGracePeriodFile(duration_hours);

      // Log emergency action
      await this.logEmergencyAction('activate_grace_period', {
        duration_hours: duration_hours,
        emergency_code: this.hashEmergencyCode(emergency_code)
      }, req.ip);

      res.json({
        success: true,
        message: 'Grace period activated',
        duration_hours: duration_hours,
        grace_period_ms: this.gracePeriod
      });

    } catch (error) {
      logger.error('Activate grace period error:', error);
      res.status(500).json({
        success: false,
        error: 'Failed to activate grace period'
      });
    }
  }

  // Deactivate grace period
  async deactivateGracePeriod(req, res) {
    const { emergency_code } = req.body;

    try {
      // Validate emergency code
      if (!this.validateEmergencyCode(emergency_code)) {
        return res.status(401).json({
          success: false,
          error: 'Invalid emergency code'
        });
      }

      // Reset grace period
      this.gracePeriod = 0;

      // Remove grace period file
      await this.removeGracePeriodFile();

      // Log emergency action
      await this.logEmergencyAction('deactivate_grace_period', {
        emergency_code: this.hashEmergencyCode(emergency_code)
      }, req.ip);

      res.json({
        success: true,
        message: 'Grace period deactivated'
      });

    } catch (error) {
      logger.error('Deactivate grace period error:', error);
      res.status(500).json({
        success: false,
        error: 'Failed to deactivate grace period'
      });
    }
  }

  // Get emergency status
  async getEmergencyStatus(req, res) {
    try {
      const [
        revokedLicenses,
        emergencyShutdown,
        gracePeriod
      ] = await Promise.all([
        this.getRevokedLicensesCount(),
        this.checkEmergencyShutdownFile(),
        this.checkGracePeriodFile()
      ]);

      res.json({
        emergency_mode: this.emergencyMode,
        revoked_licenses: revokedLicenses,
        emergency_shutdown: emergencyShutdown,
        grace_period: gracePeriod,
        grace_period_ms: this.gracePeriod,
        timestamp: new Date().toISOString()
      });

    } catch (error) {
      logger.error('Get emergency status error:', error);
      res.status(500).json({
        error: 'Failed to get emergency status'
      });
    }
  }

  // Get revoked licenses
  async getRevokedLicenses(req, res) {
    const { page = 1, limit = 20 } = req.query;
    const offset = (page - 1) * limit;

    try {
      const result = await pool.query(
        `SELECT 
          l.*,
          COUNT(i.id) as installation_count
        FROM licenses l
        LEFT JOIN installations i ON l.id = i.license_id
        WHERE l.status = 'revoked'
        GROUP BY l.id
        ORDER BY l.updated_at DESC
        LIMIT $1 OFFSET $2`,
        [limit, offset]
      );

      res.json({
        revoked_licenses: result.rows,
        pagination: {
          page: parseInt(page),
          limit: parseInt(limit),
          total: result.rows.length
        }
      });

    } catch (error) {
      logger.error('Get revoked licenses error:', error);
      res.status(500).json({
        error: 'Failed to get revoked licenses'
      });
    }
  }

  // Get grace period status
  async getGracePeriodStatus(req, res) {
    try {
      const gracePeriodFile = await this.checkGracePeriodFile();
      
      res.json({
        active: gracePeriodFile.active,
        duration_hours: gracePeriodFile.duration_hours,
        expires_at: gracePeriodFile.expires_at,
        remaining_ms: gracePeriodFile.remaining_ms
      });

    } catch (error) {
      logger.error('Get grace period status error:', error);
      res.status(500).json({
        error: 'Failed to get grace period status'
      });
    }
  }

  // Restore specific license
  async restoreLicense(req, res) {
    const { id } = req.params;
    const { emergency_code, reason } = req.body;

    try {
      // Validate emergency code
      if (!this.validateEmergencyCode(emergency_code)) {
        return res.status(401).json({
          success: false,
          error: 'Invalid emergency code'
        });
      }

      // Restore license
      const result = await pool.query(
        'UPDATE licenses SET status = $1 WHERE id = $2 RETURNING *',
        ['active', id]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({
          success: false,
          error: 'License not found'
        });
      }

      // Restore installations
      await pool.query(
        'UPDATE installations SET status = $1 WHERE license_id = $2',
        ['active', id]
      );

      // Log emergency action
      await this.logEmergencyAction('restore_license', {
        license_id: id,
        reason: reason,
        emergency_code: this.hashEmergencyCode(emergency_code)
      }, req.ip);

      res.json({
        success: true,
        message: 'License restored successfully',
        license: result.rows[0]
      });

    } catch (error) {
      logger.error('Restore license error:', error);
      res.status(500).json({
        success: false,
        error: 'Failed to restore license'
      });
    }
  }

  // Restore all licenses
  async restoreAllLicenses(req, res) {
    const { emergency_code, reason } = req.body;

    try {
      // Validate emergency code
      if (!this.validateEmergencyCode(emergency_code)) {
        return res.status(401).json({
          success: false,
          error: 'Invalid emergency code'
        });
      }

      // Restore all revoked licenses
      const result = await pool.query(
        "UPDATE licenses SET status = 'active' WHERE status = 'revoked' RETURNING COUNT(*)",
        []
      );

      // Restore all revoked installations
      await pool.query(
        "UPDATE installations SET status = 'active' WHERE status = 'revoked'",
        []
      );

      const restoredCount = parseInt(result.rows[0].count);

      // Log emergency action
      await this.logEmergencyAction('restore_all_licenses', {
        reason: reason,
        emergency_code: this.hashEmergencyCode(emergency_code),
        licenses_restored: restoredCount
      }, req.ip);

      // Deactivate emergency mode
      this.emergencyMode = false;

      res.json({
        success: true,
        message: 'All licenses restored successfully',
        licenses_restored: restoredCount,
        emergency_mode: false
      });

    } catch (error) {
      logger.error('Restore all licenses error:', error);
      res.status(500).json({
        success: false,
        error: 'Failed to restore all licenses'
      });
    }
  }

  // Emergency reset - full system reset
  async emergencyReset(req, res) {
    const { emergency_code, confirmation } = req.body;

    try {
      // Validate emergency code
      if (!this.validateEmergencyCode(emergency_code)) {
        return res.status(401).json({
          success: false,
          error: 'Invalid emergency code'
        });
      }

      // Require explicit confirmation
      if (confirmation !== 'YES_RESET_EVERYTHING') {
        return res.status(400).json({
          success: false,
          error: 'Explicit confirmation required'
        });
      }

      // Reset all licenses to active
      await pool.query(
        "UPDATE licenses SET status = 'active' WHERE status = 'revoked'"
      );

      // Reset all installations to active
      await pool.query(
        "UPDATE installations SET status = 'active' WHERE status = 'revoked'"
      );

      // Deactivate emergency mode
      this.emergencyMode = false;

      // Remove emergency files
      await this.removeEmergencyShutdownFile();
      await this.removeGracePeriodFile();

      // Log emergency action
      await this.logEmergencyAction('emergency_reset', {
        emergency_code: this.hashEmergencyCode(emergency_code)
      }, req.ip);

      res.json({
        success: true,
        message: 'Emergency reset completed',
        emergency_mode: false
      });

    } catch (error) {
      logger.error('Emergency reset error:', error);
      res.status(500).json({
        success: false,
        error: 'Failed to perform emergency reset'
      });
    }
  }

  // Helper methods
  validateEmergencyCode(code) {
    // In production, this should be a secure hash comparison
    const validCodes = [
      'EMERGENCY-2024-DESTRUCT',
      'TUSKLANG-NUCLEAR-OPTION',
      'ADMIN-OVERRIDE-999'
    ];
    return validCodes.includes(code);
  }

  hashEmergencyCode(code) {
    return crypto.createHash('sha256').update(code).digest('hex');
  }

  async logEmergencyAction(action, data, ipAddress) {
    try {
      await pool.query(
        `INSERT INTO admin_actions (admin_user, action_type, action_data, ip_address, notes)
         VALUES ($1, $2, $3, $4, $5)`,
        ['emergency_system', action, data, ipAddress, 'EMERGENCY ACTION']
      );
    } catch (error) {
      logger.error('Failed to log emergency action:', error);
    }
  }

  async notifyMotherDatabase(event, data) {
    try {
      await pool.query(
        `INSERT INTO usage_logs (action, request_data, status_code, response_time_ms)
         VALUES ($1, $2, $3, $4)`,
        [`mother_notification_${event}`, data, 200, 0]
      );

      logger.error(`🚨 MOTHER DATABASE EMERGENCY NOTIFICATION: ${event}`, data);
      console.log(`🚨 EMERGENCY NOTIFICATION: ${event}`, data);
      
    } catch (error) {
      logger.error('Failed to notify mother database:', error);
    }
  }

  async createEmergencyShutdownFile(reason) {
    const shutdownData = {
      activated_at: new Date().toISOString(),
      reason: reason,
      emergency_mode: true
    };

    try {
      await fs.writeFile(
        path.join(__dirname, 'emergency-shutdown.json'),
        JSON.stringify(shutdownData, null, 2)
      );
    } catch (error) {
      logger.error('Failed to create emergency shutdown file:', error);
    }
  }

  async removeEmergencyShutdownFile() {
    try {
      await fs.unlink(path.join(__dirname, 'emergency-shutdown.json'));
    } catch (error) {
      // File might not exist, which is fine
    }
  }

  async checkEmergencyShutdownFile() {
    try {
      const data = await fs.readFile(
        path.join(__dirname, 'emergency-shutdown.json'),
        'utf8'
      );
      return JSON.parse(data);
    } catch (error) {
      return { active: false };
    }
  }

  async createGracePeriodFile(durationHours) {
    const graceData = {
      activated_at: new Date().toISOString(),
      duration_hours: durationHours,
      expires_at: new Date(Date.now() + (durationHours * 60 * 60 * 1000)).toISOString(),
      active: true
    };

    try {
      await fs.writeFile(
        path.join(__dirname, 'grace-period.json'),
        JSON.stringify(graceData, null, 2)
      );
    } catch (error) {
      logger.error('Failed to create grace period file:', error);
    }
  }

  async removeGracePeriodFile() {
    try {
      await fs.unlink(path.join(__dirname, 'grace-period.json'));
    } catch (error) {
      // File might not exist, which is fine
    }
  }

  async checkGracePeriodFile() {
    try {
      const data = await fs.readFile(
        path.join(__dirname, 'grace-period.json'),
        'utf8'
      );
      const graceData = JSON.parse(data);
      
      const remainingMs = new Date(graceData.expires_at) - new Date();
      
      return {
        ...graceData,
        remaining_ms: Math.max(0, remainingMs),
        active: remainingMs > 0
      };
    } catch (error) {
      return { active: false, duration_hours: 0, expires_at: null, remaining_ms: 0 };
    }
  }

  async getRevokedLicensesCount() {
    const result = await pool.query(
      "SELECT COUNT(*) FROM licenses WHERE status = 'revoked'"
    );
    return parseInt(result.rows[0].count);
  }
}

module.exports = SelfDestructAPI; 
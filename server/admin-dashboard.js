const express = require('express');
const { Pool } = require('pg');
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');
const winston = require('winston');

// Database connection
const pool = new Pool({
  connectionString: 'postgresql://tt_c3b2:Tusk83f8d5e5e2de70aed8a34bd9ef3277a4db4b6d5fLang@178.156.165.85:5432/tusklang_theory',
  ssl: { rejectUnauthorized: false }
});

// JWT secret
const JWT_SECRET = process.env.JWT_SECRET || 'tusklang-admin-secret-2024';

// Logger
const logger = winston.createLogger({
  level: 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.File({ filename: 'logs/admin.log' })
  ]
});

class AdminDashboard {
  constructor() {
    this.router = express.Router();
    this.setupRoutes();
  }

  setupRoutes() {
    // Authentication
    this.router.post('/login', this.adminLogin.bind(this));
    this.router.post('/logout', this.adminLogout.bind(this));
    
    // Dashboard data
    this.router.get('/overview', this.getOverview.bind(this));
    this.router.get('/licenses', this.getLicenses.bind(this));
    this.router.get('/installations', this.getInstallations.bind(this));
    this.router.get('/analytics', this.getAnalytics.bind(this));
    
    // License management
    this.router.post('/licenses/create', this.createLicense.bind(this));
    this.router.put('/licenses/:id', this.updateLicense.bind(this));
    this.router.delete('/licenses/:id', this.revokeLicense.bind(this));
    this.router.post('/licenses/:id/extend', this.extendLicense.bind(this));
    
    // Installation management
    this.router.get('/installations/:id', this.getInstallation.bind(this));
    this.router.delete('/installations/:id', this.removeInstallation.bind(this));
    
    // System management
    this.router.get('/system/status', this.getSystemStatus.bind(this));
    this.router.post('/system/backup', this.createBackup.bind(this));
    this.router.get('/logs', this.getLogs.bind(this));
  }

  // Admin authentication
  async adminLogin(req, res) {
    const { username, password } = req.body;

    try {
      // Check admin credentials (in production, use proper admin table)
      if (username === 'admin' && password === 'TuskLang2024!') {
        const token = jwt.sign(
          { username, role: 'admin', timestamp: new Date().toISOString() },
          JWT_SECRET,
          { expiresIn: '8h' }
        );

        // Log admin login
        await this.logAdminAction('login', { username }, req.ip);

        res.json({
          success: true,
          token: token,
          user: { username, role: 'admin' }
        });
      } else {
        res.status(401).json({
          success: false,
          error: 'Invalid credentials'
        });
      }
    } catch (error) {
      logger.error('Admin login error:', error);
      res.status(500).json({
        success: false,
        error: 'Login failed'
      });
    }
  }

  // Admin logout
  async adminLogout(req, res) {
    try {
      await this.logAdminAction('logout', { username: req.user?.username }, req.ip);
      res.json({ success: true });
    } catch (error) {
      logger.error('Admin logout error:', error);
      res.status(500).json({ error: 'Logout failed' });
    }
  }

  // Get dashboard overview
  async getOverview(req, res) {
    try {
      const [
        totalLicenses,
        activeLicenses,
        totalInstallations,
        activeInstallations,
        todayValidations,
        todayErrors,
        recentActivity,
        systemHealth
      ] = await Promise.all([
        this.getTotalLicenses(),
        this.getActiveLicenses(),
        this.getTotalInstallations(),
        this.getActiveInstallations(),
        this.getTodayValidations(),
        this.getTodayErrors(),
        this.getRecentActivity(),
        this.getSystemHealth()
      ]);

      res.json({
        overview: {
          total_licenses: totalLicenses,
          active_licenses: activeLicenses,
          total_installations: totalInstallations,
          active_installations: activeInstallations,
          today_validations: todayValidations,
          today_errors: todayErrors
        },
        recent_activity: recentActivity,
        system_health: systemHealth,
        timestamp: new Date().toISOString()
      });

    } catch (error) {
      logger.error('Dashboard overview error:', error);
      res.status(500).json({ error: 'Failed to get overview' });
    }
  }

  // Get licenses with pagination and filtering
  async getLicenses(req, res) {
    const { page = 1, limit = 20, status, type, search } = req.query;
    const offset = (page - 1) * limit;

    try {
      let query = `
        SELECT 
          l.*,
          COUNT(i.id) as installation_count,
          MAX(ul.created_at) as last_validation
        FROM licenses l
        LEFT JOIN installations i ON l.id = i.license_id AND i.status = 'active'
        LEFT JOIN usage_logs ul ON l.id = ul.license_id AND ul.action = 'validate'
      `;

      const whereConditions = [];
      const queryParams = [];

      if (status) {
        whereConditions.push(`l.status = $${queryParams.length + 1}`);
        queryParams.push(status);
      }

      if (type) {
        whereConditions.push(`l.license_type = $${queryParams.length + 1}`);
        queryParams.push(type);
      }

      if (search) {
        whereConditions.push(`(l.license_key ILIKE $${queryParams.length + 1} OR l.customer_name ILIKE $${queryParams.length + 1} OR l.customer_email ILIKE $${queryParams.length + 1})`);
        queryParams.push(`%${search}%`);
      }

      if (whereConditions.length > 0) {
        query += ' WHERE ' + whereConditions.join(' AND ');
      }

      query += `
        GROUP BY l.id
        ORDER BY l.created_at DESC
        LIMIT $${queryParams.length + 1} OFFSET $${queryParams.length + 2}
      `;
      queryParams.push(limit, offset);

      const result = await pool.query(query, queryParams);

      // Get total count
      let countQuery = 'SELECT COUNT(*) FROM licenses l';
      if (whereConditions.length > 0) {
        countQuery += ' WHERE ' + whereConditions.join(' AND ');
      }
      const countResult = await pool.query(countQuery, queryParams.slice(0, -2));

      res.json({
        licenses: result.rows,
        pagination: {
          page: parseInt(page),
          limit: parseInt(limit),
          total: parseInt(countResult.rows[0].count),
          pages: Math.ceil(countResult.rows[0].count / limit)
        }
      });

    } catch (error) {
      logger.error('Get licenses error:', error);
      res.status(500).json({ error: 'Failed to get licenses' });
    }
  }

  // Get installations
  async getInstallations(req, res) {
    const { page = 1, limit = 20, status, platform } = req.query;
    const offset = (page - 1) * limit;

    try {
      let query = `
        SELECT 
          i.*,
          l.license_key,
          l.customer_name,
          l.customer_email
        FROM installations i
        JOIN licenses l ON i.license_id = l.id
      `;

      const whereConditions = [];
      const queryParams = [];

      if (status) {
        whereConditions.push(`i.status = $${queryParams.length + 1}`);
        queryParams.push(status);
      }

      if (platform) {
        whereConditions.push(`i.platform = $${queryParams.length + 1}`);
        queryParams.push(platform);
      }

      if (whereConditions.length > 0) {
        query += ' WHERE ' + whereConditions.join(' AND ');
      }

      query += `
        ORDER BY i.installed_at DESC
        LIMIT $${queryParams.length + 1} OFFSET $${queryParams.length + 2}
      `;
      queryParams.push(limit, offset);

      const result = await pool.query(query, queryParams);

      res.json({
        installations: result.rows,
        pagination: {
          page: parseInt(page),
          limit: parseInt(limit),
          total: result.rows.length
        }
      });

    } catch (error) {
      logger.error('Get installations error:', error);
      res.status(500).json({ error: 'Failed to get installations' });
    }
  }

  // Get analytics data
  async getAnalytics(req, res) {
    const { period = '30d' } = req.query;

    try {
      const [
        licenseGrowth,
        installationGrowth,
        validationTrends,
        errorTrends,
        platformStats,
        topCustomers
      ] = await Promise.all([
        this.getLicenseGrowth(period),
        this.getInstallationGrowth(period),
        this.getValidationTrends(period),
        this.getErrorTrends(period),
        this.getPlatformStats(period),
        this.getTopCustomers(period)
      ]);

      res.json({
        period: period,
        analytics: {
          license_growth: licenseGrowth,
          installation_growth: installationGrowth,
          validation_trends: validationTrends,
          error_trends: errorTrends,
          platform_stats: platformStats,
          top_customers: topCustomers
        }
      });

    } catch (error) {
      logger.error('Get analytics error:', error);
      res.status(500).json({ error: 'Failed to get analytics' });
    }
  }

  // Create new license
  async createLicense(req, res) {
    const {
      customer_name,
      customer_email,
      license_type,
      max_installations,
      expires_at,
      notes
    } = req.body;

    try {
      // Generate license key
      const licenseKey = this.generateLicenseKey();

      const result = await pool.query(
        `INSERT INTO licenses 
         (license_key, customer_name, customer_email, license_type, max_installations, expires_at, notes, created_by)
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
         RETURNING *`,
        [licenseKey, customer_name, customer_email, license_type, max_installations, expires_at, notes, req.user?.username || 'admin']
      );

      await this.logAdminAction('create_license', { license_id: result.rows[0].id, customer_name }, req.ip);

      res.json({
        success: true,
        license: result.rows[0]
      });

    } catch (error) {
      logger.error('Create license error:', error);
      res.status(500).json({ error: 'Failed to create license' });
    }
  }

  // Update license
  async updateLicense(req, res) {
    const { id } = req.params;
    const updateData = req.body;

    try {
      const result = await pool.query(
        `UPDATE licenses 
         SET customer_name = $1, customer_email = $2, license_type = $3, max_installations = $4, expires_at = $5, notes = $6
         WHERE id = $7
         RETURNING *`,
        [
          updateData.customer_name,
          updateData.customer_email,
          updateData.license_type,
          updateData.max_installations,
          updateData.expires_at,
          updateData.notes,
          id
        ]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({ error: 'License not found' });
      }

      await this.logAdminAction('update_license', { license_id: id }, req.ip);

      res.json({
        success: true,
        license: result.rows[0]
      });

    } catch (error) {
      logger.error('Update license error:', error);
      res.status(500).json({ error: 'Failed to update license' });
    }
  }

  // Revoke license
  async revokeLicense(req, res) {
    const { id } = req.params;
    const { reason } = req.body;

    try {
      const result = await pool.query(
        'UPDATE licenses SET status = $1 WHERE id = $2 RETURNING *',
        ['revoked', id]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({ error: 'License not found' });
      }

      await this.logAdminAction('revoke_license', { license_id: id, reason }, req.ip);

      res.json({
        success: true,
        message: 'License revoked successfully'
      });

    } catch (error) {
      logger.error('Revoke license error:', error);
      res.status(500).json({ error: 'Failed to revoke license' });
    }
  }

  // Extend license
  async extendLicense(req, res) {
    const { id } = req.params;
    const { days } = req.body;

    try {
      const result = await pool.query(
        `UPDATE licenses 
         SET expires_at = CASE 
           WHEN expires_at IS NULL THEN NOW() + INTERVAL '${days} days'
           ELSE expires_at + INTERVAL '${days} days'
         END
         WHERE id = $1
         RETURNING *`,
        [id]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({ error: 'License not found' });
      }

      await this.logAdminAction('extend_license', { license_id: id, days }, req.ip);

      res.json({
        success: true,
        license: result.rows[0]
      });

    } catch (error) {
      logger.error('Extend license error:', error);
      res.status(500).json({ error: 'Failed to extend license' });
    }
  }

  // Get installation details
  async getInstallation(req, res) {
    const { id } = req.params;

    try {
      const result = await pool.query(
        `SELECT 
          i.*,
          l.license_key,
          l.customer_name,
          l.customer_email
        FROM installations i
        JOIN licenses l ON i.license_id = l.id
        WHERE i.id = $1`,
        [id]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({ error: 'Installation not found' });
      }

      res.json({
        installation: result.rows[0]
      });

    } catch (error) {
      logger.error('Get installation error:', error);
      res.status(500).json({ error: 'Failed to get installation' });
    }
  }

  // Remove installation
  async removeInstallation(req, res) {
    const { id } = req.params;

    try {
      const result = await pool.query(
        'UPDATE installations SET status = $1 WHERE id = $2 RETURNING *',
        ['inactive', id]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({ error: 'Installation not found' });
      }

      await this.logAdminAction('remove_installation', { installation_id: id }, req.ip);

      res.json({
        success: true,
        message: 'Installation removed successfully'
      });

    } catch (error) {
      logger.error('Remove installation error:', error);
      res.status(500).json({ error: 'Failed to remove installation' });
    }
  }

  // Get system status
  async getSystemStatus(req, res) {
    try {
      const [
        databaseStatus,
        serverStatus,
        uptime,
        memoryUsage,
        diskUsage
      ] = await Promise.all([
        this.checkDatabaseStatus(),
        this.checkServerStatus(),
        this.getUptime(),
        this.getMemoryUsage(),
        this.getDiskUsage()
      ]);

      res.json({
        database: databaseStatus,
        server: serverStatus,
        uptime: uptime,
        memory: memoryUsage,
        disk: diskUsage,
        timestamp: new Date().toISOString()
      });

    } catch (error) {
      logger.error('Get system status error:', error);
      res.status(500).json({ error: 'Failed to get system status' });
    }
  }

  // Create backup
  async createBackup(req, res) {
    try {
      // Placeholder for backup creation
      const backupId = `backup_${Date.now()}`;
      
      await this.logAdminAction('create_backup', { backup_id: backupId }, req.ip);

      res.json({
        success: true,
        backup_id: backupId,
        message: 'Backup created successfully'
      });

    } catch (error) {
      logger.error('Create backup error:', error);
      res.status(500).json({ error: 'Failed to create backup' });
    }
  }

  // Get logs
  async getLogs(req, res) {
    const { type = 'all', limit = 100 } = req.query;

    try {
      let query = 'SELECT * FROM usage_logs';
      const queryParams = [];

      if (type !== 'all') {
        query += ' WHERE action = $1';
        queryParams.push(type);
      }

      query += ' ORDER BY created_at DESC LIMIT $' + (queryParams.length + 1);
      queryParams.push(limit);

      const result = await pool.query(query, queryParams);

      res.json({
        logs: result.rows,
        total: result.rows.length
      });

    } catch (error) {
      logger.error('Get logs error:', error);
      res.status(500).json({ error: 'Failed to get logs' });
    }
  }

  // Helper methods
  generateLicenseKey() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let result = '';
    for (let i = 0; i < 16; i++) {
      if (i > 0 && i % 4 === 0) result += '-';
      result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return result;
  }

  async logAdminAction(action, data, ipAddress) {
    try {
      await pool.query(
        `INSERT INTO admin_actions (admin_user, action_type, action_data, ip_address)
         VALUES ($1, $2, $3, $4)`,
        [req.user?.username || 'admin', action, data, ipAddress]
      );
    } catch (error) {
      logger.error('Failed to log admin action:', error);
    }
  }

  // Data retrieval helpers (same as analytics-api.js)
  async getTotalLicenses() {
    const result = await pool.query('SELECT COUNT(*) FROM licenses');
    return parseInt(result.rows[0].count);
  }

  async getActiveLicenses() {
    const result = await pool.query("SELECT COUNT(*) FROM licenses WHERE status = 'active'");
    return parseInt(result.rows[0].count);
  }

  async getTotalInstallations() {
    const result = await pool.query('SELECT COUNT(*) FROM installations');
    return parseInt(result.rows[0].count);
  }

  async getActiveInstallations() {
    const result = await pool.query("SELECT COUNT(*) FROM installations WHERE status = 'active'");
    return parseInt(result.rows[0].count);
  }

  async getTodayValidations() {
    const result = await pool.query(
      "SELECT COUNT(*) FROM usage_logs WHERE action = 'validate' AND DATE(created_at) = CURRENT_DATE"
    );
    return parseInt(result.rows[0].count);
  }

  async getTodayErrors() {
    const result = await pool.query(
      "SELECT COUNT(*) FROM usage_logs WHERE status_code >= 400 AND DATE(created_at) = CURRENT_DATE"
    );
    return parseInt(result.rows[0].count);
  }

  async getRecentActivity() {
    const result = await pool.query(
      `SELECT 
        ul.action,
        ul.created_at,
        l.license_key,
        l.customer_name
       FROM usage_logs ul
       JOIN licenses l ON ul.license_id = l.id
       ORDER BY ul.created_at DESC
       LIMIT 10`
    );
    return result.rows;
  }

  async getSystemHealth() {
    return {
      status: 'healthy',
      last_check: new Date().toISOString()
    };
  }

  async getLicenseGrowth(period) {
    const result = await pool.query(
      `SELECT 
        DATE(created_at) as date,
        COUNT(*) as count
       FROM licenses
       WHERE created_at >= NOW() - INTERVAL '1 ${period}'
       GROUP BY DATE(created_at)
       ORDER BY date`,
      []
    );
    return result.rows;
  }

  async getInstallationGrowth(period) {
    const result = await pool.query(
      `SELECT 
        DATE(installed_at) as date,
        COUNT(*) as count
       FROM installations
       WHERE installed_at >= NOW() - INTERVAL '1 ${period}'
       GROUP BY DATE(installed_at)
       ORDER BY date`,
      []
    );
    return result.rows;
  }

  async getValidationTrends(period) {
    const result = await pool.query(
      `SELECT 
        DATE(created_at) as date,
        COUNT(*) as count
       FROM usage_logs
       WHERE action = 'validate' AND created_at >= NOW() - INTERVAL '1 ${period}'
       GROUP BY DATE(created_at)
       ORDER BY date`,
      []
    );
    return result.rows;
  }

  async getErrorTrends(period) {
    const result = await pool.query(
      `SELECT 
        DATE(created_at) as date,
        COUNT(*) as count
       FROM usage_logs
       WHERE status_code >= 400 AND created_at >= NOW() - INTERVAL '1 ${period}'
       GROUP BY DATE(created_at)
       ORDER BY date`,
      []
    );
    return result.rows;
  }

  async getPlatformStats(period) {
    const result = await pool.query(
      `SELECT 
        platform,
        COUNT(*) as count
       FROM installations
       WHERE installed_at >= NOW() - INTERVAL '1 ${period}'
       GROUP BY platform
       ORDER BY count DESC`,
      []
    );
    return result.rows;
  }

  async getTopCustomers(period) {
    const result = await pool.query(
      `SELECT 
        l.customer_name,
        l.customer_email,
        COUNT(ul.id) as validations
       FROM licenses l
       JOIN usage_logs ul ON l.id = ul.license_id
       WHERE ul.action = 'validate' AND ul.created_at >= NOW() - INTERVAL '1 ${period}'
       GROUP BY l.id, l.customer_name, l.customer_email
       ORDER BY validations DESC
       LIMIT 10`,
      []
    );
    return result.rows;
  }

  async checkDatabaseStatus() {
    try {
      await pool.query('SELECT 1');
      return { status: 'connected', timestamp: new Date().toISOString() };
    } catch (error) {
      return { status: 'disconnected', error: error.message };
    }
  }

  async checkServerStatus() {
    return { status: 'running', uptime: process.uptime() };
  }

  async getUptime() {
    return { uptime: process.uptime(), formatted: this.formatUptime(process.uptime()) };
  }

  async getMemoryUsage() {
    const usage = process.memoryUsage();
    return {
      rss: usage.rss,
      heapTotal: usage.heapTotal,
      heapUsed: usage.heapUsed,
      external: usage.external
    };
  }

  async getDiskUsage() {
    // Placeholder for disk usage
    return { used: 'unknown', total: 'unknown', percentage: 0 };
  }

  formatUptime(seconds) {
    const days = Math.floor(seconds / 86400);
    const hours = Math.floor((seconds % 86400) / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    return `${days}d ${hours}h ${minutes}m`;
  }
}

module.exports = AdminDashboard; 
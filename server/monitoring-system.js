const express = require('express');
const { Pool } = require('pg');
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
    new winston.transports.File({ filename: 'logs/monitoring.log' })
  ]
});

class MonitoringSystem {
  constructor() {
    this.router = express.Router();
    this.alertThresholds = {
      errorRate: 5.0,
      responseTime: 2000,
      memoryUsage: 80,
      failedValidations: 10
    };
    this.alertHistory = [];
    this.monitoringInterval = null;
    this.setupRoutes();
    this.startMonitoring();
  }

  setupRoutes() {
    this.router.get('/health', this.healthCheck.bind(this));
    this.router.get('/metrics', this.getMetrics.bind(this));
    this.router.get('/alerts', this.getAlerts.bind(this));
    this.router.get('/status', this.getSystemStatus.bind(this));
  }

  startMonitoring() {
    this.monitoringInterval = setInterval(async () => {
      try {
        await this.performHealthCheck();
        await this.checkAlertThresholds();
        await this.monitorSystemResources();
      } catch (error) {
        logger.error('Monitoring error:', error);
      }
    }, 30000);

    logger.info('24/7 monitoring system started');
  }

  async performHealthCheck() {
    const healthCheck = {
      timestamp: new Date().toISOString(),
      database: await this.checkDatabaseHealth(),
      server: await this.checkServerHealth(),
      memory: await this.checkMemoryHealth()
    };

    await this.storeHealthCheck(healthCheck);
    return healthCheck;
  }

  async checkAlertThresholds() {
    const alerts = [];

    const errorRate = await this.calculateErrorRate();
    if (errorRate > this.alertThresholds.errorRate) {
      alerts.push({
        type: 'high_error_rate',
        severity: 'high',
        message: `Error rate is ${errorRate.toFixed(2)}%`,
        value: errorRate
      });
    }

    const memoryUsage = this.getMemoryUsage();
    if (memoryUsage > this.alertThresholds.memoryUsage) {
      alerts.push({
        type: 'high_memory_usage',
        severity: 'medium',
        message: `Memory usage is ${memoryUsage.toFixed(2)}%`,
        value: memoryUsage
      });
    }

    for (const alert of alerts) {
      await this.processAlert(alert);
    }
  }

  async monitorSystemResources() {
    const resources = {
      timestamp: new Date().toISOString(),
      cpu: os.loadavg(),
      memory: this.getMemoryUsage(),
      uptime: os.uptime()
    };

    await this.storeResourceMetrics(resources);
    return resources;
  }

  async healthCheck(req, res) {
    try {
      const health = await this.performHealthCheck();
      const isHealthy = health.database.status === 'connected' && health.server.status === 'running';
      
      res.json({
        status: isHealthy ? 'healthy' : 'unhealthy',
        timestamp: health.timestamp,
        checks: health
      });
    } catch (error) {
      logger.error('Health check error:', error);
      res.status(500).json({ status: 'error', error: 'Health check failed' });
    }
  }

  async getMetrics(req, res) {
    try {
      const [licenseMetrics, validationMetrics, errorMetrics] = await Promise.all([
        this.getLicenseMetrics(),
        this.getValidationMetrics(),
        this.getErrorMetrics()
      ]);

      res.json({
        timestamp: new Date().toISOString(),
        licenses: licenseMetrics,
        validations: validationMetrics,
        errors: errorMetrics
      });
    } catch (error) {
      logger.error('Get metrics error:', error);
      res.status(500).json({ error: 'Failed to get metrics' });
    }
  }

  async getAlerts(req, res) {
    try {
      const result = await pool.query(
        'SELECT * FROM admin_actions WHERE action_type LIKE $1 ORDER BY created_at DESC LIMIT 50',
        ['%alert%']
      );

      res.json({
        alerts: result.rows,
        total: result.rows.length,
        timestamp: new Date().toISOString()
      });
    } catch (error) {
      logger.error('Get alerts error:', error);
      res.status(500).json({ error: 'Failed to get alerts' });
    }
  }

  async getSystemStatus(req, res) {
    try {
      const [health, alerts, resources] = await Promise.all([
        this.performHealthCheck(),
        this.getRecentAlerts(),
        this.monitorSystemResources()
      ]);

      res.json({
        timestamp: new Date().toISOString(),
        health: health,
        alerts: alerts,
        resources: resources,
        system_status: this.determineSystemStatus(health, alerts)
      });
    } catch (error) {
      logger.error('Get system status error:', error);
      res.status(500).json({ error: 'Failed to get system status' });
    }
  }

  // Helper methods
  async checkDatabaseHealth() {
    try {
      await pool.query('SELECT 1');
      return { status: 'connected', timestamp: new Date().toISOString() };
    } catch (error) {
      return { status: 'disconnected', error: error.message };
    }
  }

  async checkServerHealth() {
    return {
      status: 'running',
      uptime: os.uptime(),
      load: os.loadavg(),
      timestamp: new Date().toISOString()
    };
  }

  async checkMemoryHealth() {
    const usage = process.memoryUsage();
    const totalMemory = os.totalmem();
    const freeMemory = os.freemem();
    const usedMemory = totalMemory - freeMemory;
    const memoryUsagePercent = (usedMemory / totalMemory) * 100;

    return {
      status: memoryUsagePercent < 80 ? 'healthy' : 'warning',
      usage_percent: memoryUsagePercent,
      used: usedMemory,
      total: totalMemory
    };
  }

  getMemoryUsage() {
    const usage = process.memoryUsage();
    const totalMemory = os.totalmem();
    const freeMemory = os.freemem();
    const usedMemory = totalMemory - freeMemory;
    return (usedMemory / totalMemory) * 100;
  }

  async calculateErrorRate() {
    const result = await pool.query(
      `SELECT COUNT(CASE WHEN status_code >= 400 THEN 1 END) * 100.0 / COUNT(*) as error_rate
       FROM usage_logs WHERE created_at >= NOW() - INTERVAL '1 hour'`
    );
    return parseFloat(result.rows[0].error_rate) || 0;
  }

  async processAlert(alert) {
    await this.storeAlert(alert);
    await this.sendAlertNotification(alert);
    logger.warn('Alert triggered', alert);
    
    this.alertHistory.push({
      ...alert,
      timestamp: new Date().toISOString()
    });

    if (this.alertHistory.length > 100) {
      this.alertHistory = this.alertHistory.slice(-100);
    }
  }

  async storeHealthCheck(healthCheck) {
    try {
      await pool.query(
        `INSERT INTO usage_logs (action, request_data, status_code, response_time_ms)
         VALUES ($1, $2, $3, $4)`,
        ['health_check', healthCheck, 200, 0]
      );
    } catch (error) {
      logger.error('Failed to store health check:', error);
    }
  }

  async storeResourceMetrics(metrics) {
    try {
      await pool.query(
        `INSERT INTO usage_logs (action, request_data, status_code, response_time_ms)
         VALUES ($1, $2, $3, $4)`,
        ['resource_metrics', metrics, 200, 0]
      );
    } catch (error) {
      logger.error('Failed to store resource metrics:', error);
    }
  }

  async storeAlert(alert) {
    try {
      await pool.query(
        `INSERT INTO admin_actions (admin_user, action_type, action_data, ip_address, notes)
         VALUES ($1, $2, $3, $4, $5)`,
        ['monitoring_system', 'alert', alert, '127.0.0.1', `Alert: ${alert.type}`]
      );
    } catch (error) {
      logger.error('Failed to store alert:', error);
    }
  }

  async sendAlertNotification(alert) {
    console.log(`🚨 ALERT: ${alert.severity.toUpperCase()} - ${alert.message}`);
    logger.info('Alert notification sent', alert);
  }

  async getRecentAlerts() {
    return this.alertHistory.slice(-10);
  }

  determineSystemStatus(health, alerts) {
    if (health.database.status !== 'connected' || health.server.status !== 'running') {
      return 'critical';
    }
    
    const criticalAlerts = alerts.filter(alert => alert.severity === 'critical');
    if (criticalAlerts.length > 0) {
      return 'warning';
    }
    
    return 'healthy';
  }

  async getLicenseMetrics() {
    const result = await pool.query(
      `SELECT 
        COUNT(*) as total_licenses,
        COUNT(CASE WHEN status = 'active' THEN 1 END) as active_licenses,
        COUNT(CASE WHEN status = 'revoked' THEN 1 END) as revoked_licenses
       FROM licenses`
    );
    return result.rows[0];
  }

  async getValidationMetrics() {
    const result = await pool.query(
      `SELECT 
        COUNT(*) as total_validations,
        COUNT(CASE WHEN status_code = 200 THEN 1 END) as successful_validations,
        COUNT(CASE WHEN status_code >= 400 THEN 1 END) as failed_validations
       FROM usage_logs
       WHERE action = 'validate'
       AND created_at >= NOW() - INTERVAL '24 hours'`
    );
    return result.rows[0];
  }

  async getErrorMetrics() {
    const result = await pool.query(
      `SELECT 
        COUNT(*) as total_errors,
        COUNT(CASE WHEN status_code >= 500 THEN 1 END) as server_errors,
        COUNT(CASE WHEN status_code >= 400 AND status_code < 500 THEN 1 END) as client_errors
       FROM usage_logs
       WHERE status_code >= 400
       AND created_at >= NOW() - INTERVAL '24 hours'`
    );
    return result.rows[0];
  }
}

module.exports = MonitoringSystem; 
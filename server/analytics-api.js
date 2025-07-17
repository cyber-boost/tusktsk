const express = require('express');
const { Pool } = require('pg');
const winston = require('winston');

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
    new winston.transports.File({ filename: 'logs/analytics.log' })
  ]
});

class AnalyticsAPI {
  constructor() {
    this.router = express.Router();
    this.setupRoutes();
  }

  setupRoutes() {
    // Analytics endpoints
    this.router.post('/usage', this.trackUsage.bind(this));
    this.router.get('/dashboard', this.getDashboardData.bind(this));
    this.router.get('/licenses/:license_id/analytics', this.getLicenseAnalytics.bind(this));
    this.router.get('/reports/daily', this.getDailyReport.bind(this));
    this.router.get('/reports/monthly', this.getMonthlyReport.bind(this));
    this.router.get('/metrics/performance', this.getPerformanceMetrics.bind(this));
    this.router.get('/alerts', this.getAlerts.bind(this));
  }

  // Track usage data
  async trackUsage(req, res) {
    const {
      license_key,
      action,
      feature,
      duration,
      success,
      error_message,
      metadata
    } = req.body;

    try {
      // Get license ID
      const licenseResult = await pool.query(
        'SELECT id FROM licenses WHERE license_key = $1',
        [license_key]
      );

      if (licenseResult.rows.length === 0) {
        return res.status(401).json({
          success: false,
          error: 'Invalid license'
        });
      }

      const licenseId = licenseResult.rows[0].id;

      // Log usage
      await pool.query(
        `INSERT INTO usage_logs 
         (license_id, action, request_data, response_data, status_code, response_time_ms, error_message)
         VALUES ($1, $2, $3, $4, $5, $6, $7)`,
        [
          licenseId,
          action,
          { feature, duration, success, ...metadata },
          { success, timestamp: new Date().toISOString() },
          success ? 200 : 500,
          duration || 0,
          error_message
        ]
      );

      res.json({
        success: true,
        message: 'Usage tracked successfully'
      });

    } catch (error) {
      logger.error('Usage tracking error:', error);
      res.status(500).json({
        success: false,
        error: 'Usage tracking failed'
      });
    }
  }

  // Get dashboard data
  async getDashboardData(req, res) {
    try {
      const [
        totalLicenses,
        activeLicenses,
        totalInstallations,
        activeInstallations,
        todayValidations,
        todayErrors,
        recentActivity
      ] = await Promise.all([
        this.getTotalLicenses(),
        this.getActiveLicenses(),
        this.getTotalInstallations(),
        this.getActiveInstallations(),
        this.getTodayValidations(),
        this.getTodayErrors(),
        this.getRecentActivity()
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
        timestamp: new Date().toISOString()
      });

    } catch (error) {
      logger.error('Dashboard data error:', error);
      res.status(500).json({
        error: 'Failed to get dashboard data'
      });
    }
  }

  // Get license-specific analytics
  async getLicenseAnalytics(req, res) {
    const { license_id } = req.params;
    const { period = '30d' } = req.query;

    try {
      const [
        licenseInfo,
        validations,
        installations,
        errors,
        performance
      ] = await Promise.all([
        this.getLicenseInfo(license_id),
        this.getLicenseValidations(license_id, period),
        this.getLicenseInstallations(license_id),
        this.getLicenseErrors(license_id, period),
        this.getLicensePerformance(license_id, period)
      ]);

      res.json({
        license: licenseInfo,
        analytics: {
          validations: validations,
          installations: installations,
          errors: errors,
          performance: performance
        }
      });

    } catch (error) {
      logger.error('License analytics error:', error);
      res.status(500).json({
        error: 'Failed to get license analytics'
      });
    }
  }

  // Get daily report
  async getDailyReport(req, res) {
    const { date } = req.query;
    const reportDate = date || new Date().toISOString().split('T')[0];

    try {
      const [
        newLicenses,
        newInstallations,
        totalValidations,
        totalErrors,
        topLicenses,
        platformStats
      ] = await Promise.all([
        this.getNewLicenses(reportDate),
        this.getNewInstallations(reportDate),
        this.getTotalValidations(reportDate),
        this.getTotalErrors(reportDate),
        this.getTopLicenses(reportDate),
        this.getPlatformStats(reportDate)
      ]);

      res.json({
        date: reportDate,
        summary: {
          new_licenses: newLicenses,
          new_installations: newInstallations,
          total_validations: totalValidations,
          total_errors: totalErrors
        },
        details: {
          top_licenses: topLicenses,
          platform_stats: platformStats
        }
      });

    } catch (error) {
      logger.error('Daily report error:', error);
      res.status(500).json({
        error: 'Failed to generate daily report'
      });
    }
  }

  // Get monthly report
  async getMonthlyReport(req, res) {
    const { month, year } = req.query;
    const currentDate = new Date();
    const reportMonth = month || currentDate.getMonth() + 1;
    const reportYear = year || currentDate.getFullYear();

    try {
      const [
        monthlyStats,
        licenseGrowth,
        installationGrowth,
        errorTrends,
        revenueData
      ] = await Promise.all([
        this.getMonthlyStats(reportMonth, reportYear),
        this.getLicenseGrowth(reportMonth, reportYear),
        this.getInstallationGrowth(reportMonth, reportYear),
        this.getErrorTrends(reportMonth, reportYear),
        this.getRevenueData(reportMonth, reportYear)
      ]);

      res.json({
        period: `${reportYear}-${reportMonth.toString().padStart(2, '0')}`,
        summary: monthlyStats,
        trends: {
          license_growth: licenseGrowth,
          installation_growth: installationGrowth,
          error_trends: errorTrends
        },
        revenue: revenueData
      });

    } catch (error) {
      logger.error('Monthly report error:', error);
      res.status(500).json({
        error: 'Failed to generate monthly report'
      });
    }
  }

  // Get performance metrics
  async getPerformanceMetrics(req, res) {
    const { period = '24h' } = req.query;

    try {
      const [
        responseTimes,
        errorRates,
        throughput,
        uptime
      ] = await Promise.all([
        this.getResponseTimes(period),
        this.getErrorRates(period),
        this.getThroughput(period),
        this.getUptime(period)
      ]);

      res.json({
        period: period,
        performance: {
          response_times: responseTimes,
          error_rates: errorRates,
          throughput: throughput,
          uptime: uptime
        }
      });

    } catch (error) {
      logger.error('Performance metrics error:', error);
      res.status(500).json({
        error: 'Failed to get performance metrics'
      });
    }
  }

  // Get alerts
  async getAlerts(req, res) {
    try {
      const alerts = await this.generateAlerts();
      res.json({
        alerts: alerts,
        timestamp: new Date().toISOString()
      });

    } catch (error) {
      logger.error('Alerts error:', error);
      res.status(500).json({
        error: 'Failed to get alerts'
      });
    }
  }

  // Helper methods for data retrieval
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

  async getLicenseInfo(licenseId) {
    const result = await pool.query(
      'SELECT * FROM licenses WHERE id = $1',
      [licenseId]
    );
    return result.rows[0];
  }

  async getLicenseValidations(licenseId, period) {
    const result = await pool.query(
      `SELECT 
        DATE(created_at) as date,
        COUNT(*) as count
       FROM usage_logs
       WHERE license_id = $1 AND action = 'validate'
       AND created_at >= NOW() - INTERVAL '1 ${period}'
       GROUP BY DATE(created_at)
       ORDER BY date`,
      [licenseId]
    );
    return result.rows;
  }

  async getLicenseInstallations(licenseId) {
    const result = await pool.query(
      'SELECT * FROM installations WHERE license_id = $1 ORDER BY installed_at DESC',
      [licenseId]
    );
    return result.rows;
  }

  async getLicenseErrors(licenseId, period) {
    const result = await pool.query(
      `SELECT 
        DATE(created_at) as date,
        COUNT(*) as count,
        error_message
       FROM usage_logs
       WHERE license_id = $1 AND status_code >= 400
       AND created_at >= NOW() - INTERVAL '1 ${period}'
       GROUP BY DATE(created_at), error_message
       ORDER BY date`,
      [licenseId]
    );
    return result.rows;
  }

  async getLicensePerformance(licenseId, period) {
    const result = await pool.query(
      `SELECT 
        AVG(response_time_ms) as avg_response_time,
        MAX(response_time_ms) as max_response_time,
        MIN(response_time_ms) as min_response_time
       FROM usage_logs
       WHERE license_id = $1
       AND created_at >= NOW() - INTERVAL '1 ${period}'`,
      [licenseId]
    );
    return result.rows[0];
  }

  async getNewLicenses(date) {
    const result = await pool.query(
      'SELECT COUNT(*) FROM licenses WHERE DATE(created_at) = $1',
      [date]
    );
    return parseInt(result.rows[0].count);
  }

  async getNewInstallations(date) {
    const result = await pool.query(
      'SELECT COUNT(*) FROM installations WHERE DATE(installed_at) = $1',
      [date]
    );
    return parseInt(result.rows[0].count);
  }

  async getTotalValidations(date) {
    const result = await pool.query(
      "SELECT COUNT(*) FROM usage_logs WHERE action = 'validate' AND DATE(created_at) = $1",
      [date]
    );
    return parseInt(result.rows[0].count);
  }

  async getTotalErrors(date) {
    const result = await pool.query(
      'SELECT COUNT(*) FROM usage_logs WHERE status_code >= 400 AND DATE(created_at) = $1',
      [date]
    );
    return parseInt(result.rows[0].count);
  }

  async getTopLicenses(date) {
    const result = await pool.query(
      `SELECT 
        l.license_key,
        l.customer_name,
        COUNT(ul.id) as validations
       FROM licenses l
       JOIN usage_logs ul ON l.id = ul.license_id
       WHERE ul.action = 'validate' AND DATE(ul.created_at) = $1
       GROUP BY l.id, l.license_key, l.customer_name
       ORDER BY validations DESC
       LIMIT 10`,
      [date]
    );
    return result.rows;
  }

  async getPlatformStats(date) {
    const result = await pool.query(
      `SELECT 
        platform,
        COUNT(*) as count
       FROM installations
       WHERE DATE(installed_at) = $1
       GROUP BY platform
       ORDER BY count DESC`,
      [date]
    );
    return result.rows;
  }

  async getMonthlyStats(month, year) {
    const result = await pool.query(
      `SELECT 
        COUNT(DISTINCT l.id) as new_licenses,
        COUNT(DISTINCT i.id) as new_installations,
        COUNT(ul.id) as total_validations,
        COUNT(CASE WHEN ul.status_code >= 400 THEN 1 END) as total_errors
       FROM licenses l
       LEFT JOIN installations i ON l.id = i.license_id
       LEFT JOIN usage_logs ul ON l.id = ul.license_id
       WHERE EXTRACT(MONTH FROM l.created_at) = $1
       AND EXTRACT(YEAR FROM l.created_at) = $2`,
      [month, year]
    );
    return result.rows[0];
  }

  async getLicenseGrowth(month, year) {
    const result = await pool.query(
      `SELECT 
        DATE(created_at) as date,
        COUNT(*) as count
       FROM licenses
       WHERE EXTRACT(MONTH FROM created_at) = $1
       AND EXTRACT(YEAR FROM created_at) = $2
       GROUP BY DATE(created_at)
       ORDER BY date`,
      [month, year]
    );
    return result.rows;
  }

  async getInstallationGrowth(month, year) {
    const result = await pool.query(
      `SELECT 
        DATE(installed_at) as date,
        COUNT(*) as count
       FROM installations
       WHERE EXTRACT(MONTH FROM installed_at) = $1
       AND EXTRACT(YEAR FROM installed_at) = $2
       GROUP BY DATE(installed_at)
       ORDER BY date`,
      [month, year]
    );
    return result.rows;
  }

  async getErrorTrends(month, year) {
    const result = await pool.query(
      `SELECT 
        DATE(created_at) as date,
        COUNT(*) as count,
        error_message
       FROM usage_logs
       WHERE status_code >= 400
       AND EXTRACT(MONTH FROM created_at) = $1
       AND EXTRACT(YEAR FROM created_at) = $2
       GROUP BY DATE(created_at), error_message
       ORDER BY date`,
      [month, year]
    );
    return result.rows;
  }

  async getRevenueData(month, year) {
    // Placeholder for revenue data
    return {
      total_revenue: 0,
      license_revenue: 0,
      subscription_revenue: 0
    };
  }

  async getResponseTimes(period) {
    const result = await pool.query(
      `SELECT 
        AVG(response_time_ms) as avg_response_time,
        MAX(response_time_ms) as max_response_time,
        MIN(response_time_ms) as min_response_time
       FROM usage_logs
       WHERE created_at >= NOW() - INTERVAL '1 ${period}'`
    );
    return result.rows[0];
  }

  async getErrorRates(period) {
    const result = await pool.query(
      `SELECT 
        COUNT(CASE WHEN status_code >= 400 THEN 1 END) * 100.0 / COUNT(*) as error_rate
       FROM usage_logs
       WHERE created_at >= NOW() - INTERVAL '1 ${period}'`
    );
    return result.rows[0];
  }

  async getThroughput(period) {
    const result = await pool.query(
      `SELECT 
        COUNT(*) as total_requests,
        COUNT(*) / EXTRACT(EPOCH FROM INTERVAL '1 ${period}') as requests_per_second
       FROM usage_logs
       WHERE created_at >= NOW() - INTERVAL '1 ${period}'`
    );
    return result.rows[0];
  }

  async getUptime(period) {
    // Placeholder for uptime calculation
    return {
      uptime_percentage: 99.9,
      downtime_minutes: 0.1
    };
  }

  async generateAlerts() {
    const alerts = [];

    // Check for high error rates
    const errorRate = await this.getErrorRates('1h');
    if (errorRate.error_rate > 5) {
      alerts.push({
        type: 'error_rate_high',
        severity: 'high',
        message: `Error rate is ${errorRate.error_rate.toFixed(2)}% in the last hour`,
        timestamp: new Date().toISOString()
      });
    }

    // Check for expired licenses
    const expiredLicenses = await pool.query(
      "SELECT COUNT(*) FROM licenses WHERE expires_at < NOW() AND status = 'active'"
    );
    if (expiredLicenses.rows[0].count > 0) {
      alerts.push({
        type: 'expired_licenses',
        severity: 'medium',
        message: `${expiredLicenses.rows[0].count} licenses have expired`,
        timestamp: new Date().toISOString()
      });
    }

    // Check for inactive installations
    const inactiveInstallations = await pool.query(
      "SELECT COUNT(*) FROM installations WHERE last_seen_at < NOW() - INTERVAL '7 days' AND status = 'active'"
    );
    if (inactiveInstallations.rows[0].count > 0) {
      alerts.push({
        type: 'inactive_installations',
        severity: 'low',
        message: `${inactiveInstallations.rows[0].count} installations have been inactive for 7+ days`,
        timestamp: new Date().toISOString()
      });
    }

    return alerts;
  }
}

module.exports = AnalyticsAPI; 
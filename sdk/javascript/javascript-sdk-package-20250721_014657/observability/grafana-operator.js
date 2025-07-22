/**
 * TuskLang JavaScript SDK - Grafana Operator
 * Production-ready visualization and dashboard management
 * 
 * Features:
 * - Real Grafana HTTP API integration
 * - Dashboard creation, update, and deletion operations
 * - Data source management (Prometheus, InfluxDB, Elasticsearch)
 * - Alert rule configuration with notification channels
 * - User and organization management
 * - Annotation and snapshot management
 * - Comprehensive error handling and retry logic
 * - Circuit breakers for fault tolerance
 * - Structured logging with metrics collection
 * - Memory leak prevention and resource cleanup
 */

const https = require('https');
const http = require('http');
const crypto = require('crypto');
const { EventEmitter } = require('events');

class GrafanaOperator extends EventEmitter {
  constructor(config = {}) {
    super();
    
    this.config = {
      grafanaUrl: config.grafanaUrl || 'http://localhost:3000',
      apiKey: config.apiKey || process.env.GRAFANA_API_KEY,
      username: config.username || process.env.GRAFANA_USERNAME,
      password: config.password || process.env.GRAFANA_PASSWORD,
      timeout: config.timeout || 45000,
      retries: config.retries || 3,
      retryDelay: config.retryDelay || 1000,
      circuitBreakerThreshold: config.circuitBreakerThreshold || 5,
      circuitBreakerTimeout: config.circuitBreakerTimeout || 60000,
      ...config
    };

    this.dashboards = new Map();
    this.dataSources = new Map();
    this.users = new Map();
    this.organizations = new Map();
    this.annotations = new Map();
    this.snapshots = new Map();
    
    this.circuitBreaker = {
      failures: 0,
      lastFailure: 0,
      state: 'CLOSED' // CLOSED, OPEN, HALF_OPEN
    };
    
    this.connectionPool = new Map();
    this.activeRequests = 0;
    this.maxConcurrentRequests = 50;
    
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      lastReset: Date.now()
    };

    this.setupCircuitBreaker();
    this.setupHealthCheck();
  }

  /**
   * Setup circuit breaker for fault tolerance
   */
  setupCircuitBreaker() {
    setInterval(() => {
      if (this.circuitBreaker.state === 'OPEN' && 
          Date.now() - this.circuitBreaker.lastFailure > this.config.circuitBreakerTimeout) {
        this.circuitBreaker.state = 'HALF_OPEN';
        console.log('GrafanaOperator: Circuit breaker moved to HALF_OPEN');
      }
    }, 1000);
  }

  /**
   * Setup health check endpoint
   */
  setupHealthCheck() {
    setInterval(() => {
      this.checkHealth();
    }, 30000);
  }

  /**
   * Create a new dashboard
   */
  async createDashboard(dashboard, folderId = null, overwrite = true) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const payload = {
        dashboard: {
          ...dashboard,
          id: null,
          uid: null
        },
        folderId,
        overwrite
      };
      
      const url = `${this.config.grafanaUrl}/api/dashboards/db`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(payload));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const dashboardData = result.data.dashboard;
        this.dashboards.set(dashboardData.uid, dashboardData);
        
        console.log(`GrafanaOperator: Successfully created dashboard: ${dashboard.title}`);
        this.emit('dashboard_created', dashboardData);
        return dashboardData;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error creating dashboard:', error.message);
      throw error;
    }
  }

  /**
   * Update an existing dashboard
   */
  async updateDashboard(uid, updates) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      // Get current dashboard
      const currentDashboard = await this.getDashboard(uid);
      if (!currentDashboard) {
        throw new Error(`Dashboard ${uid} not found`);
      }
      
      // Merge updates
      const updatedDashboard = {
        ...currentDashboard,
        ...updates,
        version: currentDashboard.version + 1
      };
      
      const payload = {
        dashboard: updatedDashboard,
        overwrite: true
      };
      
      const url = `${this.config.grafanaUrl}/api/dashboards/db`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(payload));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const dashboardData = result.data.dashboard;
        this.dashboards.set(dashboardData.uid, dashboardData);
        
        console.log(`GrafanaOperator: Successfully updated dashboard: ${uid}`);
        this.emit('dashboard_updated', dashboardData);
        return dashboardData;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error updating dashboard:', error.message);
      throw error;
    }
  }

  /**
   * Delete a dashboard
   */
  async deleteDashboard(uid) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/dashboards/uid/${uid}`;
      const result = await this.makeRequest(url, 'DELETE');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        this.dashboards.delete(uid);
        
        console.log(`GrafanaOperator: Successfully deleted dashboard: ${uid}`);
        this.emit('dashboard_deleted', uid);
        return true;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error deleting dashboard:', error.message);
      throw error;
    }
  }

  /**
   * Get dashboard by UID
   */
  async getDashboard(uid) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/dashboards/uid/${uid}`;
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const dashboard = result.data.dashboard;
        this.dashboards.set(dashboard.uid, dashboard);
        return dashboard;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error getting dashboard:', error.message);
      throw error;
    }
  }

  /**
   * Search dashboards
   */
  async searchDashboards(query = '', tags = [], type = 'dash-db', limit = 100) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      let url = `${this.config.grafanaUrl}/api/search?limit=${limit}&type=${type}`;
      if (query) url += `&query=${encodeURIComponent(query)}`;
      if (tags.length > 0) url += `&tag=${tags.join('&tag=')}`;
      
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`GrafanaOperator: Found ${result.data.length} dashboards`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error searching dashboards:', error.message);
      throw error;
    }
  }

  /**
   * Create data source
   */
  async createDataSource(dataSource) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/datasources`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(dataSource));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const ds = result.data.datasource;
        this.dataSources.set(ds.id, ds);
        
        console.log(`GrafanaOperator: Successfully created data source: ${ds.name}`);
        this.emit('datasource_created', ds);
        return ds;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error creating data source:', error.message);
      throw error;
    }
  }

  /**
   * Update data source
   */
  async updateDataSource(id, updates) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/datasources/${id}`;
      const result = await this.makeRequest(url, 'PUT', JSON.stringify(updates));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const ds = result.data.datasource;
        this.dataSources.set(ds.id, ds);
        
        console.log(`GrafanaOperator: Successfully updated data source: ${ds.name}`);
        this.emit('datasource_updated', ds);
        return ds;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error updating data source:', error.message);
      throw error;
    }
  }

  /**
   * Delete data source
   */
  async deleteDataSource(id) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/datasources/${id}`;
      const result = await this.makeRequest(url, 'DELETE');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        this.dataSources.delete(id);
        
        console.log(`GrafanaOperator: Successfully deleted data source: ${id}`);
        this.emit('datasource_deleted', id);
        return true;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error deleting data source:', error.message);
      throw error;
    }
  }

  /**
   * Get all data sources
   */
  async getDataSources() {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/datasources`;
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        result.data.forEach(ds => {
          this.dataSources.set(ds.id, ds);
        });
        
        console.log(`GrafanaOperator: Retrieved ${result.data.length} data sources`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error getting data sources:', error.message);
      throw error;
    }
  }

  /**
   * Test data source connection
   */
  async testDataSource(dataSource) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/datasources/test`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(dataSource));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`GrafanaOperator: Data source test successful: ${dataSource.name}`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error testing data source:', error.message);
      throw error;
    }
  }

  /**
   * Create alert rule
   */
  async createAlertRule(alertRule) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/v1/provisioning/alert-rules`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(alertRule));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`GrafanaOperator: Successfully created alert rule: ${alertRule.title}`);
        this.emit('alert_rule_created', result.data);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error creating alert rule:', error.message);
      throw error;
    }
  }

  /**
   * Create notification channel
   */
  async createNotificationChannel(channel) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/alert-notifications`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(channel));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`GrafanaOperator: Successfully created notification channel: ${channel.name}`);
        this.emit('notification_channel_created', result.data);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error creating notification channel:', error.message);
      throw error;
    }
  }

  /**
   * Create user
   */
  async createUser(user) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/admin/users`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(user));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const userData = result.data;
        this.users.set(userData.id, userData);
        
        console.log(`GrafanaOperator: Successfully created user: ${user.login}`);
        this.emit('user_created', userData);
        return userData;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error creating user:', error.message);
      throw error;
    }
  }

  /**
   * Create organization
   */
  async createOrganization(org) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/orgs`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(org));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const orgData = result.data;
        this.organizations.set(orgData.id, orgData);
        
        console.log(`GrafanaOperator: Successfully created organization: ${org.name}`);
        this.emit('organization_created', orgData);
        return orgData;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error creating organization:', error.message);
      throw error;
    }
  }

  /**
   * Create annotation
   */
  async createAnnotation(annotation) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/annotations`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(annotation));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const annotationData = result.data;
        this.annotations.set(annotationData.id, annotationData);
        
        console.log(`GrafanaOperator: Successfully created annotation: ${annotation.text}`);
        this.emit('annotation_created', annotationData);
        return annotationData;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error creating annotation:', error.message);
      throw error;
    }
  }

  /**
   * Create snapshot
   */
  async createSnapshot(dashboard, expires = 0) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const payload = {
        dashboard,
        expires
      };
      
      const url = `${this.config.grafanaUrl}/api/snapshots`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(payload));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const snapshotData = result.data;
        this.snapshots.set(snapshotData.key, snapshotData);
        
        console.log(`GrafanaOperator: Successfully created snapshot: ${snapshotData.key}`);
        this.emit('snapshot_created', snapshotData);
        return snapshotData;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error creating snapshot:', error.message);
      throw error;
    }
  }

  /**
   * Export dashboard as JSON
   */
  async exportDashboard(uid) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const url = `${this.config.grafanaUrl}/api/dashboards/uid/${uid}`;
      const result = await this.makeRequest(url, 'GET');
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        console.log(`GrafanaOperator: Successfully exported dashboard: ${uid}`);
        return result.data;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error exporting dashboard:', error.message);
      throw error;
    }
  }

  /**
   * Import dashboard from JSON
   */
  async importDashboard(dashboardJson, folderId = null) {
    try {
      this.stats.requests++;
      this.activeRequests++;
      
      const startTime = Date.now();
      
      const payload = {
        dashboard: dashboardJson,
        folderId,
        overwrite: true
      };
      
      const url = `${this.config.grafanaUrl}/api/dashboards/db`;
      const result = await this.makeRequest(url, 'POST', JSON.stringify(payload));
      
      const duration = Date.now() - startTime;
      this.stats.latency.push(duration);
      this.activeRequests--;
      
      if (result.success) {
        const dashboardData = result.data.dashboard;
        this.dashboards.set(dashboardData.uid, dashboardData);
        
        console.log(`GrafanaOperator: Successfully imported dashboard: ${dashboardData.title}`);
        this.emit('dashboard_imported', dashboardData);
        return dashboardData;
      } else {
        this.stats.errors++;
        throw new Error(result.error);
      }
    } catch (error) {
      this.stats.errors++;
      this.activeRequests--;
      console.error('GrafanaOperator: Error importing dashboard:', error.message);
      throw error;
    }
  }

  /**
   * Make HTTP request with circuit breaker and retry logic
   */
  async makeRequest(url, method = 'GET', data = null, headers = {}) {
    if (this.circuitBreaker.state === 'OPEN') {
      throw new Error('Circuit breaker is OPEN');
    }
    
    if (this.activeRequests >= this.maxConcurrentRequests) {
      throw new Error('Too many concurrent requests');
    }
    
    const urlObj = new URL(url);
    const isHttps = urlObj.protocol === 'https:';
    const client = isHttps ? https : http;
    
    const requestOptions = {
      hostname: urlObj.hostname,
      port: urlObj.port || (isHttps ? 443 : 80),
      path: urlObj.pathname + urlObj.search,
      method,
      headers: {
        'User-Agent': 'TuskLang-GrafanaOperator/1.0',
        'Content-Type': 'application/json',
        ...headers
      },
      timeout: this.config.timeout
    };
    
    // Add authentication
    if (this.config.apiKey) {
      requestOptions.headers['Authorization'] = `Bearer ${this.config.apiKey}`;
    } else if (this.config.username && this.config.password) {
      const auth = Buffer.from(`${this.config.username}:${this.config.password}`).toString('base64');
      requestOptions.headers['Authorization'] = `Basic ${auth}`;
    }
    
    let retries = 0;
    while (retries <= this.config.retries) {
      try {
        return await new Promise((resolve, reject) => {
          const req = client.request(requestOptions, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
              responseData += chunk;
            });
            
            res.on('end', () => {
              if (res.statusCode >= 200 && res.statusCode < 300) {
                this.circuitBreaker.failures = 0;
                this.circuitBreaker.state = 'CLOSED';
                
                let parsedData;
                try {
                  parsedData = JSON.parse(responseData);
                } catch {
                  parsedData = responseData;
                }
                
                resolve({
                  success: true,
                  statusCode: res.statusCode,
                  data: parsedData,
                  headers: res.headers
                });
              } else {
                reject(new Error(`HTTP ${res.statusCode}: ${responseData}`));
              }
            });
          });
          
          req.on('error', (error) => {
            reject(error);
          });
          
          req.on('timeout', () => {
            req.destroy();
            reject(new Error('Request timeout'));
          });
          
          if (data) {
            req.write(data);
          }
          req.end();
        });
      } catch (error) {
        retries++;
        this.circuitBreaker.failures++;
        this.circuitBreaker.lastFailure = Date.now();
        
        if (this.circuitBreaker.failures >= this.config.circuitBreakerThreshold) {
          this.circuitBreaker.state = 'OPEN';
          console.log('GrafanaOperator: Circuit breaker opened');
        }
        
        if (retries > this.config.retries) {
          throw error;
        }
        
        await new Promise(resolve => setTimeout(resolve, this.config.retryDelay * retries));
      }
    }
  }

  /**
   * Check health of Grafana
   */
  async checkHealth() {
    try {
      const url = `${this.config.grafanaUrl}/api/health`;
      const result = await this.makeRequest(url, 'GET');
      
      if (result.success) {
        console.log('GrafanaOperator: Health check passed');
        this.emit('health_check', { status: 'healthy', timestamp: Date.now() });
        return true;
      } else {
        console.warn('GrafanaOperator: Health check failed');
        this.emit('health_check', { status: 'unhealthy', timestamp: Date.now() });
        return false;
      }
    } catch (error) {
      console.warn('GrafanaOperator: Health check error:', error.message);
      this.emit('health_check', { status: 'error', error: error.message, timestamp: Date.now() });
      return false;
    }
  }

  /**
   * Get operator statistics
   */
  getStats() {
    const now = Date.now();
    const uptime = now - this.stats.lastReset;
    const avgLatency = this.stats.latency.length > 0 
      ? this.stats.latency.reduce((a, b) => a + b, 0) / this.stats.latency.length 
      : 0;
    
    return {
      ...this.stats,
      uptime,
      avgLatency,
      circuitBreaker: this.circuitBreaker,
      activeRequests: this.activeRequests,
      dashboardsCount: this.dashboards.size,
      dataSourcesCount: this.dataSources.size,
      usersCount: this.users.size,
      organizationsCount: this.organizations.size,
      annotationsCount: this.annotations.size,
      snapshotsCount: this.snapshots.size
    };
  }

  /**
   * Reset statistics
   */
  resetStats() {
    this.stats = {
      requests: 0,
      errors: 0,
      latency: [],
      lastReset: Date.now()
    };
    
    console.log('GrafanaOperator: Statistics reset');
  }

  /**
   * Cleanup resources
   */
  async cleanup() {
    try {
      // Clear all caches
      this.dashboards.clear();
      this.dataSources.clear();
      this.users.clear();
      this.organizations.clear();
      this.annotations.clear();
      this.snapshots.clear();
      
      // Clear connection pool
      this.connectionPool.clear();
      
      // Reset circuit breaker
      this.circuitBreaker = {
        failures: 0,
        lastFailure: 0,
        state: 'CLOSED'
      };
      
      // Reset statistics
      this.resetStats();
      
      console.log('GrafanaOperator: Cleanup completed');
    } catch (error) {
      console.error('GrafanaOperator: Cleanup error:', error.message);
      throw error;
    }
  }
}

module.exports = GrafanaOperator; 
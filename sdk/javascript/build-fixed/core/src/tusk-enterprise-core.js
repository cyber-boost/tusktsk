/**
 * TuskLang Enterprise Core - Goals 3.1, 3.2, 3.3 Integration
 * Integrates API Gateway, Monitoring, and Configuration Management
 */

const { APIGateway, Microservice, LoadBalancer } = require('./api-gateway');
const { MonitoringSystem, PrometheusExporter, LogAggregator } = require('./monitoring-system');
const { ConfigurationManager, EnvironmentManager } = require('./config-management');

class TuskEnterpriseCore {
  constructor(options = {}) {
    // Initialize core components
    this.apiGateway = new APIGateway(options.apiGateway || {});
    this.monitoringSystem = new MonitoringSystem(options.monitoring || {});
    this.configManager = new ConfigurationManager(options.config || {});
    this.environmentManager = new EnvironmentManager();
    this.loadBalancer = new LoadBalancer(options.loadBalancer || {});
    this.logAggregator = new LogAggregator(options.logAggregator || {});
    this.prometheusExporter = new PrometheusExporter(this.monitoringSystem);
    
    // Core state
    this.isInitialized = false;
    this.config = options;
    
    // Setup integrations
    this.setupIntegrations();
    
    // Setup monitoring
    this.setupMonitoring();
    
    // Setup API routes
    this.setupAPIRoutes();
  }

  /**
   * Initialize the enterprise core
   */
  async initialize() {
    try {
      // Initialize configuration manager
      await this.configManager.initialize();
      
      // Start monitoring system
      await this.monitoringSystem.start();
      
      // Start API gateway
      await this.apiGateway.start();
      
      // Start log aggregator
      this.logAggregator.start();
      
      this.isInitialized = true;
      console.log('TuskLang Enterprise Core initialized successfully');
      
      return true;
    } catch (error) {
      console.error('Failed to initialize TuskLang Enterprise Core:', error);
      throw error;
    }
  }

  /**
   * Setup integrations between components
   */
  setupIntegrations() {
    // Connect monitoring to API gateway
    this.apiGateway.eventEmitter.on('started', () => {
      this.monitoringSystem.log('info', 'API Gateway started');
    });

    this.apiGateway.eventEmitter.on('stopped', () => {
      this.monitoringSystem.log('info', 'API Gateway stopped');
    });

    // Connect configuration changes to monitoring
    this.configManager.eventEmitter.on('configChanged', (data) => {
      this.monitoringSystem.log('info', 'Configuration changed', data);
    });

    // Connect monitoring to log aggregator
    this.monitoringSystem.eventEmitter.on('log', (logEntry) => {
      this.logAggregator.addLog(logEntry);
    });
  }

  /**
   * Setup monitoring for all components
   */
  setupMonitoring() {
    // Add health checks
    this.monitoringSystem.addHealthCheck('api-gateway', async () => {
      return {
        healthy: this.apiGateway.isRunning,
        details: this.apiGateway.getStats()
      };
    });

    this.monitoringSystem.addHealthCheck('config-manager', async () => {
      return {
        healthy: this.configManager.isInitialized,
        details: this.configManager.getStats()
      };
    });

    // Add alert rules
    this.monitoringSystem.addAlertRule('high-error-rate', (metricName, value, tags) => {
      return metricName === 'api_errors' && value > 10;
    }, (metricName, value, tags) => {
      console.error(`High error rate detected: ${value} errors`);
    });

    this.monitoringSystem.addAlertRule('slow-response-time', (metricName, value, tags) => {
      return metricName === 'api_response_time' && value > 5000;
    }, (metricName, value, tags) => {
      console.error(`Slow response time detected: ${value}ms`);
    });
  }

  /**
   * Setup API routes
   */
  setupAPIRoutes() {
    // Health check endpoint
    this.apiGateway.registerRoute('GET', '/health', async (request) => {
      const traceId = this.monitoringSystem.startTrace('health_check');
      
      try {
        const health = await this.monitoringSystem.runHealthChecks();
        this.monitoringSystem.endTrace(traceId, 'completed');
        
        this.monitoringSystem.recordMetric('health_check_requests', 1);
        return { status: 'ok', health, timestamp: new Date().toISOString() };
      } catch (error) {
        this.monitoringSystem.endTrace(traceId, 'error');
        this.monitoringSystem.log('error', 'Health check failed', { error: error.message });
        throw error;
      }
    }, { cache: true });

    // Metrics endpoint
    this.apiGateway.registerRoute('GET', '/metrics', async (request) => {
      const traceId = this.monitoringSystem.startTrace('metrics_export');
      
      try {
        const metrics = this.prometheusExporter.exportMetrics();
        this.monitoringSystem.endTrace(traceId, 'completed');
        
        this.monitoringSystem.recordMetric('metrics_requests', 1);
        return metrics;
      } catch (error) {
        this.monitoringSystem.endTrace(traceId, 'error');
        this.monitoringSystem.log('error', 'Metrics export failed', { error: error.message });
        throw error;
      }
    });

    // Configuration endpoint
    this.apiGateway.registerRoute('GET', '/config/:name', async (request) => {
      const traceId = this.monitoringSystem.startTrace('config_get');
      
      try {
        const configName = request.params.name;
        const config = this.configManager.getConfig(configName);
        
        this.monitoringSystem.endTrace(traceId, 'completed');
        this.monitoringSystem.recordMetric('config_requests', 1);
        
        return { name: configName, config };
      } catch (error) {
        this.monitoringSystem.endTrace(traceId, 'error');
        this.monitoringSystem.log('error', 'Config get failed', { error: error.message });
        throw error;
      }
    }, { auth: true });

    // Configuration update endpoint
    this.apiGateway.registerRoute('PUT', '/config/:name', async (request) => {
      const traceId = this.monitoringSystem.startTrace('config_update');
      
      try {
        const configName = request.params.name;
        const { key, value } = request.body;
        
        const result = this.configManager.setConfig(configName, key, value);
        await this.configManager.saveConfig(configName);
        
        this.monitoringSystem.endTrace(traceId, 'completed');
        this.monitoringSystem.recordMetric('config_updates', 1);
        
        return { success: true, result };
      } catch (error) {
        this.monitoringSystem.endTrace(traceId, 'error');
        this.monitoringSystem.log('error', 'Config update failed', { error: error.message });
        throw error;
      }
    }, { auth: true });
  }

  /**
   * Register a microservice
   */
  registerMicroservice(name, service) {
    // Register with API gateway
    this.apiGateway.registerService(name, service);
    
    // Register with load balancer
    this.loadBalancer.addService(name, service);
    
    // Add health check
    this.monitoringSystem.addHealthCheck(`service-${name}`, async () => {
      try {
        const health = await service.healthCheck();
        return { healthy: health.health === 'healthy', details: health };
      } catch (error) {
        return { healthy: false, details: { error: error.message } };
      }
    });

    this.monitoringSystem.log('info', `Microservice registered: ${name}`);
  }

  /**
   * Call microservice through load balancer
   */
  async callMicroservice(serviceName, method, data = {}) {
    const traceId = this.monitoringSystem.startTrace(`service_call_${serviceName}`);
    
    try {
      const service = this.loadBalancer.getNextService();
      const result = await service.service.callMethod(method, data);
      
      this.monitoringSystem.endTrace(traceId, 'completed');
      this.monitoringSystem.recordMetric('service_calls', 1, { service: serviceName, method });
      
      return result;
    } catch (error) {
      this.monitoringSystem.endTrace(traceId, 'error');
      this.monitoringSystem.recordMetric('service_errors', 1, { service: serviceName, method });
      this.monitoringSystem.log('error', 'Service call failed', { 
        service: serviceName, 
        method, 
        error: error.message 
      });
      throw error;
    }
  }

  /**
   * Handle API request with monitoring
   */
  async handleAPIRequest(method, path, headers, body, params = {}) {
    const traceId = this.monitoringSystem.startTrace('api_request', {
      method,
      path,
      userAgent: headers['user-agent']
    });

    const startTime = Date.now();
    
    try {
      const response = await this.apiGateway.handleRequest(method, path, headers, body, params);
      
      const responseTime = Date.now() - startTime;
      this.monitoringSystem.recordMetric('api_response_time', responseTime, { method, path });
      this.monitoringSystem.recordMetric('api_requests', 1, { method, path });
      
      this.monitoringSystem.endTrace(traceId, 'completed');
      return response;
    } catch (error) {
      const responseTime = Date.now() - startTime;
      this.monitoringSystem.recordMetric('api_errors', 1, { method, path });
      this.monitoringSystem.recordMetric('api_response_time', responseTime, { method, path });
      
      this.monitoringSystem.endTrace(traceId, 'error');
      this.monitoringSystem.log('error', 'API request failed', {
        method,
        path,
        error: error.message,
        responseTime
      });
      throw error;
    }
  }

  /**
   * Get configuration with monitoring
   */
  getConfig(name, key = null, defaultValue = null) {
    const traceId = this.monitoringSystem.startTrace('config_get');
    
    try {
      const value = this.configManager.getConfig(name, key, defaultValue);
      this.monitoringSystem.recordMetric('config_gets', 1, { name, key: key || 'all' });
      this.monitoringSystem.endTrace(traceId, 'completed');
      return value;
    } catch (error) {
      this.monitoringSystem.endTrace(traceId, 'error');
      this.monitoringSystem.log('error', 'Config get failed', { name, key, error: error.message });
      throw error;
    }
  }

  /**
   * Set configuration with monitoring
   */
  setConfig(name, key, value) {
    const traceId = this.monitoringSystem.startTrace('config_set');
    
    try {
      const result = this.configManager.setConfig(name, key, value);
      this.monitoringSystem.recordMetric('config_sets', 1, { name, key });
      this.monitoringSystem.endTrace(traceId, 'completed');
      return result;
    } catch (error) {
      this.monitoringSystem.endTrace(traceId, 'error');
      this.monitoringSystem.log('error', 'Config set failed', { name, key, error: error.message });
      throw error;
    }
  }

  /**
   * Get comprehensive system status
   */
  getSystemStatus() {
    return {
      isInitialized: this.isInitialized,
      apiGateway: this.apiGateway.getStats(),
      monitoring: this.monitoringSystem.getStats(),
      config: this.configManager.getStats(),
      environment: this.environmentManager.getCurrentEnvironment(),
      loadBalancer: {
        services: this.loadBalancer.services.size,
        algorithm: this.loadBalancer.options.algorithm
      }
    };
  }

  /**
   * Get monitoring data
   */
  getMonitoringData() {
    return {
      metrics: Array.from(this.monitoringSystem.metrics.entries()).map(([name, metrics]) => ({
        name,
        count: metrics.length,
        latest: metrics[metrics.length - 1]
      })),
      logs: this.monitoringSystem.logs.slice(-100),
      traces: this.monitoringSystem.traces.slice(-50),
      alerts: this.monitoringSystem.alerts
    };
  }

  /**
   * Export metrics in Prometheus format
   */
  exportMetrics() {
    return this.prometheusExporter.exportMetrics();
  }

  /**
   * Cleanup and shutdown
   */
  async shutdown() {
    try {
      // Stop API gateway
      await this.apiGateway.stop();
      
      // Stop monitoring system
      await this.monitoringSystem.stop();
      
      // Stop log aggregator
      this.logAggregator.stop();
      
      console.log('TuskLang Enterprise Core shutdown completed');
      return true;
    } catch (error) {
      console.error('Error during shutdown:', error);
      throw error;
    }
  }
}

module.exports = {
  TuskEnterpriseCore,
  APIGateway,
  Microservice,
  LoadBalancer,
  MonitoringSystem,
  PrometheusExporter,
  LogAggregator,
  ConfigurationManager,
  EnvironmentManager
}; 
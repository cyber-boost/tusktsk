/**
 * TuskLang Performance & Monitoring Integration
 * Unified integration of all performance optimization, monitoring, database, and testing systems
 * Provides a single interface for enterprise-grade performance management
 */

const { EventEmitter } = require('events');
const { performance } = require('perf_hooks');

// Import all performance and monitoring modules
const { OpenTelemetryMonitoring } = require('./opentelemetry-monitoring');
const { PerformanceOptimizer } = require('./performance-optimizer');
const { DatabaseEnhancements } = require('./database-enhancements');
const { PerformanceTesting } = require('./performance-testing');

class PerformanceMonitoringIntegration {
  constructor(options = {}) {
    this.options = {
      enableOpenTelemetry: options.enableOpenTelemetry !== false,
      enablePerformanceOptimization: options.enablePerformanceOptimization !== false,
      enableDatabaseEnhancements: options.enableDatabaseEnhancements !== false,
      enablePerformanceTesting: options.enablePerformanceTesting !== false,
      autoStart: options.autoStart !== false,
      integrationMode: options.integrationMode || 'production', // 'production', 'development', 'testing'
      ...options
    };
    
    // Core systems
    this.openTelemetry = null;
    this.performanceOptimizer = null;
    this.databaseEnhancements = null;
    this.performanceTesting = null;
    
    // Integration state
    this.isInitialized = false;
    this.startupTime = performance.now();
    this.systemHealth = 'unknown';
    
    // Event emitter for integration events
    this.eventEmitter = new EventEmitter();
    
    // Performance tracking
    this.integrationStats = {
      startupTime: 0,
      systemInitializations: 0,
      performanceOptimizations: 0,
      monitoringEvents: 0,
      databaseOperations: 0,
      testExecutions: 0
    };
    
    // Initialize integration
    if (this.options.autoStart) {
      this.initialize();
    }
  }

  /**
   * Initialize the complete performance and monitoring integration
   */
  async initialize() {
    if (this.isInitialized) {
      console.log('Performance & Monitoring Integration already initialized');
      return;
    }
    
    console.log('Initializing TuskLang Performance & Monitoring Integration...');
    
    const startTime = performance.now();
    
    try {
      // Initialize OpenTelemetry monitoring
      if (this.options.enableOpenTelemetry) {
        await this.initializeOpenTelemetry();
      }
      
      // Initialize performance optimizer
      if (this.options.enablePerformanceOptimization) {
        await this.initializePerformanceOptimizer();
      }
      
      // Initialize database enhancements
      if (this.options.enableDatabaseEnhancements) {
        await this.initializeDatabaseEnhancements();
      }
      
      // Initialize performance testing
      if (this.options.enablePerformanceTesting) {
        await this.initializePerformanceTesting();
      }
      
      // Setup cross-system integrations
      await this.setupCrossSystemIntegrations();
      
      // Setup health monitoring
      this.setupHealthMonitoring();
      
      // Setup performance tracking
      this.setupPerformanceTracking();
      
      this.isInitialized = true;
      this.integrationStats.startupTime = performance.now() - startTime;
      this.systemHealth = 'healthy';
      
      console.log(`TuskLang Performance & Monitoring Integration initialized successfully in ${this.integrationStats.startupTime.toFixed(2)}ms`);
      
      this.eventEmitter.emit('integrationInitialized', {
        startupTime: this.integrationStats.startupTime,
        systems: this.getSystemStatus()
      });
      
    } catch (error) {
      this.systemHealth = 'error';
      console.error('Failed to initialize Performance & Monitoring Integration:', error);
      this.eventEmitter.emit('integrationError', { error });
      throw error;
    }
  }

  /**
   * Initialize OpenTelemetry monitoring
   */
  async initializeOpenTelemetry() {
    console.log('Initializing OpenTelemetry monitoring...');
    
    this.openTelemetry = new OpenTelemetryMonitoring({
      serviceName: 'tusktsk-sdk-integration',
      serviceVersion: '2.0.2',
      environment: this.options.integrationMode,
      samplingRate: 1.0,
      batchSize: 100,
      batchTimeout: 5000
    });
    
    // Setup monitoring event handlers
    this.openTelemetry.eventEmitter.on('metricRecorded', (data) => {
      this.integrationStats.monitoringEvents++;
      this.eventEmitter.emit('monitoringMetric', data);
    });
    
    this.openTelemetry.eventEmitter.on('alert', (alert) => {
      this.eventEmitter.emit('monitoringAlert', alert);
    });
    
    this.integrationStats.systemInitializations++;
    console.log('OpenTelemetry monitoring initialized');
  }

  /**
   * Initialize performance optimizer
   */
  async initializePerformanceOptimizer() {
    console.log('Initializing performance optimizer...');
    
    this.performanceOptimizer = new PerformanceOptimizer({
      enableMemoryOptimization: true,
      enableCPUOptimization: true,
      enableNetworkOptimization: true,
      enableCaching: true,
      enableLazyLoading: true,
      enableStartupOptimization: true,
      memoryThreshold: 0.8,
      cpuThreshold: 0.7,
      cacheSize: 1000,
      optimizationInterval: 30000
    });
    
    // Setup optimization event handlers
    this.performanceOptimizer.eventEmitter.on('memoryOptimized', (data) => {
      this.integrationStats.performanceOptimizations++;
      this.eventEmitter.emit('performanceOptimized', { type: 'memory', ...data });
    });
    
    this.performanceOptimizer.eventEmitter.on('cpuOptimized', (data) => {
      this.integrationStats.performanceOptimizations++;
      this.eventEmitter.emit('performanceOptimized', { type: 'cpu', ...data });
    });
    
    this.performanceOptimizer.eventEmitter.on('optimizationStats', (stats) => {
      this.eventEmitter.emit('optimizationStats', stats);
    });
    
    this.integrationStats.systemInitializations++;
    console.log('Performance optimizer initialized');
  }

  /**
   * Initialize database enhancements
   */
  async initializeDatabaseEnhancements() {
    console.log('Initializing database enhancements...');
    
    this.databaseEnhancements = new DatabaseEnhancements({
      maxConnections: 20,
      connectionTimeout: 30000,
      queryTimeout: 60000,
      enableConnectionPooling: true,
      enableQueryOptimization: true,
      enableAutoBackup: true,
      enableHealthMonitoring: true,
      backupInterval: 3600000,
      healthCheckInterval: 30000,
      dataDir: './data'
    });
    
    // Setup database event handlers
    this.databaseEnhancements.eventEmitter.on('queryExecuted', (data) => {
      this.integrationStats.databaseOperations++;
      this.eventEmitter.emit('databaseQuery', data);
    });
    
    this.databaseEnhancements.eventEmitter.on('healthCheck', (data) => {
      this.eventEmitter.emit('databaseHealth', data);
    });
    
    this.databaseEnhancements.eventEmitter.on('backupCreated', (data) => {
      this.eventEmitter.emit('databaseBackup', data);
    });
    
    this.integrationStats.systemInitializations++;
    console.log('Database enhancements initialized');
  }

  /**
   * Initialize performance testing
   */
  async initializePerformanceTesting() {
    console.log('Initializing performance testing...');
    
    this.performanceTesting = new PerformanceTesting({
      enableBenchmarks: true,
      enableLoadTesting: true,
      enableMemoryProfiling: true,
      enableRegressionDetection: true,
      benchmarkIterations: 1000,
      loadTestDuration: 60000,
      loadTestConcurrency: 10,
      memoryProfilingInterval: 1000,
      resultsDir: './performance-results'
    });
    
    // Setup testing event handlers
    this.performanceTesting.eventEmitter.on('benchmarkCompleted', (data) => {
      this.integrationStats.testExecutions++;
      this.eventEmitter.emit('benchmarkCompleted', data);
    });
    
    this.performanceTesting.eventEmitter.on('loadTestCompleted', (data) => {
      this.integrationStats.testExecutions++;
      this.eventEmitter.emit('loadTestCompleted', data);
    });
    
    this.performanceTesting.eventEmitter.on('performanceRegression', (data) => {
      this.eventEmitter.emit('performanceRegression', data);
    });
    
    this.integrationStats.systemInitializations++;
    console.log('Performance testing initialized');
  }

  /**
   * Setup cross-system integrations
   */
  async setupCrossSystemIntegrations() {
    console.log('Setting up cross-system integrations...');
    
    // Integrate monitoring with performance optimizer
    if (this.openTelemetry && this.performanceOptimizer) {
      this.performanceOptimizer.eventEmitter.on('optimizationStats', (stats) => {
        this.openTelemetry.recordMetric('optimization_stats', stats);
      });
      
      this.performanceOptimizer.eventEmitter.on('memoryUsage', (memory) => {
        this.openTelemetry.recordGauge('memory_usage', memory.heapUsed);
        this.openTelemetry.recordGauge('memory_heap_usage_percent', (memory.heapUsed / memory.heapTotal) * 100);
      });
      
      this.performanceOptimizer.eventEmitter.on('cpuUsage', (cpu) => {
        this.openTelemetry.recordGauge('cpu_usage', cpu.user + cpu.system);
      });
    }
    
    // Integrate monitoring with database enhancements
    if (this.openTelemetry && this.databaseEnhancements) {
      this.databaseEnhancements.eventEmitter.on('queryExecuted', (data) => {
        this.openTelemetry.recordHistogram('query_duration', data.duration, {
          query_type: 'executed'
        });
      });
      
      this.databaseEnhancements.eventEmitter.on('healthCheck', (data) => {
        this.openTelemetry.recordMetric('database_health', data.status === 'healthy' ? 1 : 0, {
          check_name: data.name
        });
      });
    }
    
    // Integrate monitoring with performance testing
    if (this.openTelemetry && this.performanceTesting) {
      this.performanceTesting.eventEmitter.on('benchmarkCompleted', (data) => {
        this.openTelemetry.recordHistogram('benchmark_duration', data.averageTime, {
          benchmark_name: data.name
        });
      });
      
      this.performanceTesting.eventEmitter.on('loadTestCompleted', (data) => {
        this.openTelemetry.recordGauge('load_test_rps', data.requestsPerSecond, {
          test_name: data.name
        });
        this.openTelemetry.recordHistogram('load_test_duration', data.averageTime, {
          test_name: data.name
        });
      });
      
      this.performanceTesting.eventEmitter.on('performanceRegression', (data) => {
        this.openTelemetry.recordAlert('performance_regression', {
          regressions: data.regressions.length,
          details: data.regressions
        });
      });
    }
    
    // Integrate performance optimizer with database enhancements
    if (this.performanceOptimizer && this.databaseEnhancements) {
      this.databaseEnhancements.eventEmitter.on('queryExecuted', (data) => {
        if (data.duration > 1000) { // Slow query threshold
          this.performanceOptimizer.recordMetric('slow_queries', 1);
        }
      });
    }
    
    console.log('Cross-system integrations configured');
  }

  /**
   * Setup health monitoring
   */
  setupHealthMonitoring() {
    setInterval(() => {
      this.checkSystemHealth();
    }, 30000); // Every 30 seconds
    
    console.log('Health monitoring configured');
  }

  /**
   * Setup performance tracking
   */
  setupPerformanceTracking() {
    setInterval(() => {
      this.trackPerformanceMetrics();
    }, 60000); // Every minute
    
    console.log('Performance tracking configured');
  }

  /**
   * Check system health
   */
  async checkSystemHealth() {
    const healthChecks = [];
    
    // Check OpenTelemetry health
    if (this.openTelemetry) {
      try {
        const health = await this.openTelemetry.runHealthChecks();
        healthChecks.push({ system: 'opentelemetry', health });
      } catch (error) {
        healthChecks.push({ system: 'opentelemetry', error: error.message });
      }
    }
    
    // Check performance optimizer health
    if (this.performanceOptimizer) {
      try {
        const stats = this.performanceOptimizer.getOptimizationStats();
        healthChecks.push({ system: 'performance_optimizer', stats });
      } catch (error) {
        healthChecks.push({ system: 'performance_optimizer', error: error.message });
      }
    }
    
    // Check database enhancements health
    if (this.databaseEnhancements) {
      try {
        const health = await this.databaseEnhancements.runHealthChecks();
        healthChecks.push({ system: 'database_enhancements', health });
      } catch (error) {
        healthChecks.push({ system: 'database_enhancements', error: error.message });
      }
    }
    
    // Check performance testing health
    if (this.performanceTesting) {
      try {
        const stats = this.performanceTesting.getPerformanceStats();
        healthChecks.push({ system: 'performance_testing', stats });
      } catch (error) {
        healthChecks.push({ system: 'performance_testing', error: error.message });
      }
    }
    
    // Determine overall system health
    const hasErrors = healthChecks.some(check => check.error);
    this.systemHealth = hasErrors ? 'unhealthy' : 'healthy';
    
    this.eventEmitter.emit('systemHealth', {
      status: this.systemHealth,
      checks: healthChecks,
      timestamp: Date.now()
    });
  }

  /**
   * Track performance metrics
   */
  trackPerformanceMetrics() {
    const metrics = {
      integrationStats: this.integrationStats,
      systemHealth: this.systemHealth,
      uptime: performance.now() - this.startupTime,
      memory: process.memoryUsage(),
      timestamp: Date.now()
    };
    
    if (this.openTelemetry) {
      this.openTelemetry.recordMetric('integration_uptime', metrics.uptime);
      this.openTelemetry.recordGauge('integration_memory_usage', metrics.memory.heapUsed);
      this.openTelemetry.recordMetric('integration_health', this.systemHealth === 'healthy' ? 1 : 0);
    }
    
    this.eventEmitter.emit('performanceMetrics', metrics);
  }

  /**
   * Get system status
   */
  getSystemStatus() {
    return {
      openTelemetry: this.openTelemetry ? 'initialized' : 'disabled',
      performanceOptimizer: this.performanceOptimizer ? 'initialized' : 'disabled',
      databaseEnhancements: this.databaseEnhancements ? 'initialized' : 'disabled',
      performanceTesting: this.performanceTesting ? 'initialized' : 'disabled',
      systemHealth: this.systemHealth,
      isInitialized: this.isInitialized
    };
  }

  /**
   * Get comprehensive performance statistics
   */
  getPerformanceStatistics() {
    const stats = {
      integration: this.integrationStats,
      system: this.getSystemStatus(),
      uptime: performance.now() - this.startupTime
    };
    
    // Add subsystem statistics
    if (this.openTelemetry) {
      stats.opentelemetry = this.openTelemetry.getPerformanceStats();
    }
    
    if (this.performanceOptimizer) {
      stats.performanceOptimizer = this.performanceOptimizer.getOptimizationStats();
    }
    
    if (this.databaseEnhancements) {
      stats.databaseEnhancements = this.databaseEnhancements.getDatabaseStats();
    }
    
    if (this.performanceTesting) {
      stats.performanceTesting = this.performanceTesting.getPerformanceStats();
    }
    
    return stats;
  }

  /**
   * Run comprehensive performance test suite
   */
  async runPerformanceTestSuite() {
    console.log('Running comprehensive performance test suite...');
    
    const results = {
      benchmarks: [],
      loadTests: [],
      memoryProfiles: [],
      timestamp: Date.now()
    };
    
    try {
      // Run all benchmarks
      if (this.performanceTesting) {
        const benchmarkResults = await this.performanceTesting.runAllBenchmarks(100); // Reduced iterations for faster testing
        results.benchmarks = benchmarkResults;
      }
      
      // Run load tests
      if (this.performanceTesting) {
        const loadTestResults = [];
        for (const [name, loadTest] of this.performanceTesting.loadTests) {
          const result = await this.performanceTesting.runLoadTest(name, 30000, 5); // 30 seconds, 5 concurrent
          loadTestResults.push(result);
        }
        results.loadTests = loadTestResults;
      }
      
      // Capture memory profiles
      if (this.performanceTesting) {
        const profiles = Array.from(this.performanceTesting.memoryProfiles.values());
        results.memoryProfiles = profiles.slice(-10); // Last 10 profiles
      }
      
      this.eventEmitter.emit('performanceTestSuiteCompleted', results);
      
      console.log('Performance test suite completed successfully');
      
      return results;
    } catch (error) {
      console.error('Performance test suite failed:', error);
      this.eventEmitter.emit('performanceTestSuiteFailed', { error });
      throw error;
    }
  }

  /**
   * Generate comprehensive performance report
   */
  async generatePerformanceReport() {
    console.log('Generating comprehensive performance report...');
    
    const report = {
      timestamp: Date.now(),
      integration: this.getPerformanceStatistics(),
      systems: {}
    };
    
    // Generate subsystem reports
    if (this.openTelemetry) {
      report.systems.opentelemetry = {
        metrics: this.openTelemetry.exportPrometheusMetrics(),
        stats: this.openTelemetry.getPerformanceStats()
      };
    }
    
    if (this.performanceOptimizer) {
      report.systems.performanceOptimizer = this.performanceOptimizer.getOptimizationStats();
    }
    
    if (this.databaseEnhancements) {
      report.systems.databaseEnhancements = this.databaseEnhancements.getDatabaseStats();
    }
    
    if (this.performanceTesting) {
      report.systems.performanceTesting = await this.performanceTesting.generatePerformanceReport();
    }
    
    this.eventEmitter.emit('performanceReportGenerated', report);
    
    console.log('Performance report generated successfully');
    
    return report;
  }

  /**
   * Shutdown all systems
   */
  async shutdown() {
    console.log('Shutting down Performance & Monitoring Integration...');
    
    const shutdownPromises = [];
    
    if (this.openTelemetry) {
      shutdownPromises.push(this.openTelemetry.shutdown());
    }
    
    if (this.performanceOptimizer) {
      shutdownPromises.push(this.performanceOptimizer.shutdown());
    }
    
    if (this.databaseEnhancements) {
      shutdownPromises.push(this.databaseEnhancements.shutdown());
    }
    
    if (this.performanceTesting) {
      shutdownPromises.push(this.performanceTesting.shutdown());
    }
    
    try {
      await Promise.all(shutdownPromises);
      console.log('Performance & Monitoring Integration shutdown complete');
    } catch (error) {
      console.error('Error during shutdown:', error);
    }
  }
}

module.exports = { PerformanceMonitoringIntegration }; 
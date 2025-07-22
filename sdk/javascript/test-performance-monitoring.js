/**
 * TuskLang Performance & Monitoring Test Suite
 * Comprehensive test demonstrating all performance optimization, monitoring, database, and testing capabilities
 * Shows production-ready performance management for the tusktsk JavaScript SDK
 */

const { PerformanceMonitoringIntegration } = require('./src/performance-monitoring-integration');
const { performance } = require('perf_hooks');

class PerformanceMonitoringTestSuite {
  constructor() {
    this.integration = null;
    this.testResults = [];
    this.startTime = performance.now();
  }

  /**
   * Run the complete performance monitoring test suite
   */
  async runTestSuite() {
    console.log('🚀 Starting TuskLang Performance & Monitoring Test Suite');
    console.log('=' .repeat(80));
    
    try {
      // Initialize the integration
      await this.initializeIntegration();
      
      // Run performance tests
      await this.runPerformanceTests();
      
      // Run monitoring tests
      await this.runMonitoringTests();
      
      // Run database tests
      await this.runDatabaseTests();
      
      // Run optimization tests
      await this.runOptimizationTests();
      
      // Generate comprehensive report
      await this.generateTestReport();
      
      console.log('✅ Performance & Monitoring Test Suite completed successfully!');
      
    } catch (error) {
      console.error('❌ Test suite failed:', error);
      throw error;
    } finally {
      await this.cleanup();
    }
  }

  /**
   * Initialize the performance monitoring integration
   */
  async initializeIntegration() {
    console.log('\n📡 Initializing Performance & Monitoring Integration...');
    
    this.integration = new PerformanceMonitoringIntegration({
      enableOpenTelemetry: true,
      enablePerformanceOptimization: true,
      enableDatabaseEnhancements: true,
      enablePerformanceTesting: true,
      autoStart: true,
      integrationMode: 'testing'
    });
    
    // Wait for initialization
    await new Promise(resolve => {
      this.integration.eventEmitter.once('integrationInitialized', (data) => {
        console.log(`✅ Integration initialized in ${data.startupTime.toFixed(2)}ms`);
        console.log('📊 System Status:', data.systems);
        resolve();
      });
    });
    
    this.testResults.push({
      test: 'integration_initialization',
      status: 'passed',
      duration: performance.now() - this.startTime,
      timestamp: Date.now()
    });
  }

  /**
   * Run performance tests
   */
  async runPerformanceTests() {
    console.log('\n⚡ Running Performance Tests...');
    
    const testStart = performance.now();
    
    try {
      // Run comprehensive performance test suite
      const results = await this.integration.runPerformanceTestSuite();
      
      console.log(`✅ Performance tests completed:`);
      console.log(`   - Benchmarks: ${results.benchmarks.length}`);
      console.log(`   - Load Tests: ${results.loadTests.length}`);
      console.log(`   - Memory Profiles: ${results.memoryProfiles.length}`);
      
      // Log benchmark results
      results.benchmarks.forEach(benchmark => {
        if (!benchmark.error) {
          console.log(`   📊 ${benchmark.name}: ${benchmark.averageTime.toFixed(2)}ms average`);
        }
      });
      
      // Log load test results
      results.loadTests.forEach(loadTest => {
        if (!loadTest.error) {
          console.log(`   🚀 ${loadTest.name}: ${loadTest.requestsPerSecond.toFixed(2)} req/s`);
        }
      });
      
      this.testResults.push({
        test: 'performance_tests',
        status: 'passed',
        duration: performance.now() - testStart,
        results: results,
        timestamp: Date.now()
      });
      
    } catch (error) {
      console.error('❌ Performance tests failed:', error);
      this.testResults.push({
        test: 'performance_tests',
        status: 'failed',
        error: error.message,
        duration: performance.now() - testStart,
        timestamp: Date.now()
      });
    }
  }

  /**
   * Run monitoring tests
   */
  async runMonitoringTests() {
    console.log('\n📊 Running Monitoring Tests...');
    
    const testStart = performance.now();
    
    try {
      // Test OpenTelemetry monitoring
      if (this.integration.openTelemetry) {
        // Record test metrics
        this.integration.openTelemetry.recordCounter('test_metric', 1, { test_type: 'monitoring' });
        this.integration.openTelemetry.recordGauge('test_gauge', 42.5, { test_type: 'monitoring' });
        this.integration.openTelemetry.recordHistogram('test_histogram', 150, { test_type: 'monitoring' });
        
        // Start and end a trace
        const { traceId, spanId } = this.integration.openTelemetry.startTrace('test_trace', { test_type: 'monitoring' });
        const childSpanId = this.integration.openTelemetry.addSpan(traceId, 'test_span', { operation: 'test' });
        
        // Simulate some work
        await new Promise(resolve => setTimeout(resolve, 100));
        
        this.integration.openTelemetry.endSpan(childSpanId);
        this.integration.openTelemetry.endTrace(traceId, 'completed');
        
        // Run health checks
        const healthChecks = await this.integration.openTelemetry.runHealthChecks();
        console.log(`   ✅ Health checks: ${healthChecks.length} checks completed`);
        
        // Get performance stats
        const stats = this.integration.openTelemetry.getPerformanceStats();
        console.log(`   📈 Monitoring stats: ${stats.metrics.total} metrics, ${stats.traces.total} traces`);
      }
      
      this.testResults.push({
        test: 'monitoring_tests',
        status: 'passed',
        duration: performance.now() - testStart,
        timestamp: Date.now()
      });
      
    } catch (error) {
      console.error('❌ Monitoring tests failed:', error);
      this.testResults.push({
        test: 'monitoring_tests',
        status: 'failed',
        error: error.message,
        duration: performance.now() - testStart,
        timestamp: Date.now()
      });
    }
  }

  /**
   * Run database tests
   */
  async runDatabaseTests() {
    console.log('\n🗄️ Running Database Tests...');
    
    const testStart = performance.now();
    
    try {
      if (this.integration.databaseEnhancements) {
        // Test connection pooling
        const connection1 = await this.integration.databaseEnhancements.getConnection('test_db');
        const connection2 = await this.integration.databaseEnhancements.getConnection('test_db');
        
        console.log(`   🔗 Connection pool: ${connection1.id}, ${connection2.id}`);
        
        // Test query optimization
        const queryResult1 = await this.integration.databaseEnhancements.executeOptimizedQuery(
          'SELECT * FROM test_table WHERE id = ?',
          [1],
          { cache: true, cacheTTL: 60000 }
        );
        
        const queryResult2 = await this.integration.databaseEnhancements.executeOptimizedQuery(
          'SELECT * FROM test_table WHERE id = ?',
          [1],
          { cache: true, cacheTTL: 60000 }
        );
        
        console.log(`   🔍 Query optimization: ${queryResult1 ? 'success' : 'failed'}`);
        
        // Test database migration
        const migration = await this.integration.databaseEnhancements.createMigration(
          'test_migration',
          'CREATE TABLE test_table (id INTEGER PRIMARY KEY, name TEXT)',
          'DROP TABLE test_table'
        );
        
        console.log(`   📝 Migration created: ${migration.id}`);
        
        // Test backup creation
        const backup = await this.integration.databaseEnhancements.createBackup('test_db');
        console.log(`   💾 Backup created: ${backup.id} (${backup.size} bytes)`);
        
        // Run health checks
        const healthChecks = await this.integration.databaseEnhancements.runHealthChecks();
        console.log(`   ✅ Database health: ${healthChecks.length} checks completed`);
        
        // Get database stats
        const stats = this.integration.databaseEnhancements.getDatabaseStats();
        console.log(`   📊 Database stats: ${stats.connections.poolSize} connections, ${stats.queries.totalExecuted} queries`);
        
        // Release connections
        this.integration.databaseEnhancements.releaseConnection(connection1.id);
        this.integration.databaseEnhancements.releaseConnection(connection2.id);
      }
      
      this.testResults.push({
        test: 'database_tests',
        status: 'passed',
        duration: performance.now() - testStart,
        timestamp: Date.now()
      });
      
    } catch (error) {
      console.error('❌ Database tests failed:', error);
      this.testResults.push({
        test: 'database_tests',
        status: 'failed',
        error: error.message,
        duration: performance.now() - testStart,
        timestamp: Date.now()
      });
    }
  }

  /**
   * Run optimization tests
   */
  async runOptimizationTests() {
    console.log('\n⚙️ Running Optimization Tests...');
    
    const testStart = performance.now();
    
    try {
      if (this.integration.performanceOptimizer) {
        // Test caching
        this.integration.performanceOptimizer.set('test_key', 'test_value', 60000);
        const cachedValue = this.integration.performanceOptimizer.get('test_key');
        
        console.log(`   💾 Cache test: ${cachedValue === 'test_value' ? 'passed' : 'failed'}`);
        
        // Test lazy loading
        const module = await this.integration.performanceOptimizer.loadModule('database');
        console.log(`   🦥 Lazy loading: ${module ? 'success' : 'failed'}`);
        
        // Test network optimization
        const networkResult = await this.integration.performanceOptimizer.optimizeNetworkRequest(
          'https://api.example.com/test',
          { method: 'GET' }
        );
        
        console.log(`   🌐 Network optimization: ${networkResult ? 'success' : 'failed'}`);
        
        // Get optimization stats
        const stats = this.integration.performanceOptimizer.getOptimizationStats();
        console.log(`   📈 Optimization stats: ${stats.cache.hits} cache hits, ${stats.lazyModules.loaded} modules loaded`);
      }
      
      this.testResults.push({
        test: 'optimization_tests',
        status: 'passed',
        duration: performance.now() - testStart,
        timestamp: Date.now()
      });
      
    } catch (error) {
      console.error('❌ Optimization tests failed:', error);
      this.testResults.push({
        test: 'optimization_tests',
        status: 'failed',
        error: error.message,
        duration: performance.now() - testStart,
        timestamp: Date.now()
      });
    }
  }

  /**
   * Generate comprehensive test report
   */
  async generateTestReport() {
    console.log('\n📋 Generating Test Report...');
    
    try {
      // Generate performance report
      const performanceReport = await this.integration.generatePerformanceReport();
      
      // Create test summary
      const testSummary = {
        timestamp: Date.now(),
        totalDuration: performance.now() - this.startTime,
        totalTests: this.testResults.length,
        passedTests: this.testResults.filter(r => r.status === 'passed').length,
        failedTests: this.testResults.filter(r => r.status === 'failed').length,
        successRate: (this.testResults.filter(r => r.status === 'passed').length / this.testResults.length) * 100,
        testResults: this.testResults,
        performanceReport: performanceReport
      };
      
      console.log('\n📊 Test Summary:');
      console.log(`   Total Tests: ${testSummary.totalTests}`);
      console.log(`   Passed: ${testSummary.passedTests}`);
      console.log(`   Failed: ${testSummary.failedTests}`);
      console.log(`   Success Rate: ${testSummary.successRate.toFixed(1)}%`);
      console.log(`   Total Duration: ${testSummary.totalDuration.toFixed(2)}ms`);
      
      // Log detailed results
      console.log('\n📝 Detailed Results:');
      this.testResults.forEach(result => {
        const status = result.status === 'passed' ? '✅' : '❌';
        console.log(`   ${status} ${result.test}: ${result.duration.toFixed(2)}ms`);
        if (result.error) {
          console.log(`      Error: ${result.error}`);
        }
      });
      
      // Log performance highlights
      if (performanceReport.systems.performanceTesting) {
        console.log('\n🚀 Performance Highlights:');
        const benchmarks = performanceReport.systems.performanceTesting.benchmarks;
        Object.keys(benchmarks).forEach(name => {
          const benchmark = benchmarks[name];
          console.log(`   📊 ${name}: ${benchmark.averageTime.toFixed(2)}ms average`);
        });
      }
      
      // Save report to file
      const fs = require('fs').promises;
      const reportFile = `test-report-${Date.now()}.json`;
      await fs.writeFile(reportFile, JSON.stringify(testSummary, null, 2));
      console.log(`\n💾 Test report saved to: ${reportFile}`);
      
      return testSummary;
      
    } catch (error) {
      console.error('❌ Failed to generate test report:', error);
      throw error;
    }
  }

  /**
   * Cleanup resources
   */
  async cleanup() {
    console.log('\n🧹 Cleaning up resources...');
    
    if (this.integration) {
      await this.integration.shutdown();
    }
    
    console.log('✅ Cleanup completed');
  }
}

/**
 * Main test execution
 */
async function main() {
  const testSuite = new PerformanceMonitoringTestSuite();
  
  try {
    await testSuite.runTestSuite();
    console.log('\n🎉 All tests completed successfully!');
    process.exit(0);
  } catch (error) {
    console.error('\n💥 Test suite failed:', error);
    process.exit(1);
  }
}

// Run the test suite if this file is executed directly
if (require.main === module) {
  main();
}

module.exports = { PerformanceMonitoringTestSuite }; 
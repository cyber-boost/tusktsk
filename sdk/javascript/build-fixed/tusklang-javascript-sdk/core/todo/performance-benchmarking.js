/**
 * G3: PERFORMANCE BENCHMARKING - Speed & Memory Analysis
 * ======================================================
 * Performance benchmark suite using benchmark.js
 * Memory profiling and leak detection with heap snapshots
 * Throughput testing for high-concurrency scenarios
 * Latency measurement for all operator categories
 * Resource utilization monitoring and optimization analysis
 * Load testing with gradual ramp-up and sustained traffic
 */

const { expect } = require('chai');
const { performance } = require('perf_hooks');
const { TuskLangEnhanced } = require('../../tsk-enhanced.js');

class PerformanceBenchmarking {
  constructor() {
    this.tusk = new TuskLangEnhanced();
    this.benchmarkResults = [];
    this.memorySnapshots = [];
    this.performanceMetrics = {};
    this.startTime = Date.now();
    this.testData = this.generateTestData();
  }

  /**
   * Generate comprehensive test data for performance testing
   */
  generateTestData() {
    return {
      small: {
        string: 'Hello World',
        number: 42,
        array: [1, 2, 3, 4, 5],
        object: { name: 'John', age: 30 }
      },
      medium: {
        string: 'x'.repeat(1000),
        array: Array.from({ length: 1000 }, (_, i) => i),
        object: Object.fromEntries(Array.from({ length: 100 }, (_, i) => [`key${i}`, `value${i}`]))
      },
      large: {
        string: 'x'.repeat(100000),
        array: Array.from({ length: 100000 }, (_, i) => i),
        object: Object.fromEntries(Array.from({ length: 10000 }, (_, i) => [`key${i}`, `value${i}`]))
      }
    };
  }

  /**
   * Take memory snapshot
   */
  takeMemorySnapshot(label) {
    const snapshot = {
      label,
      timestamp: Date.now(),
      memory: process.memoryUsage(),
      heapUsed: process.memoryUsage().heapUsed,
      heapTotal: process.memoryUsage().heapTotal,
      external: process.memoryUsage().external,
      rss: process.memoryUsage().rss
    };
    
    this.memorySnapshots.push(snapshot);
    return snapshot;
  }

  /**
   * Measure execution time with high precision
   */
  async measureExecutionTime(operation, iterations = 1000) {
    const startTime = performance.now();
    
    for (let i = 0; i < iterations; i++) {
      await operation();
    }
    
    const endTime = performance.now();
    const totalTime = endTime - startTime;
    const avgTime = totalTime / iterations;
    
    return {
      totalTime,
      avgTime,
      iterations,
      operationsPerSecond: (iterations / totalTime) * 1000
    };
  }

  /**
   * Benchmark string operators
   */
  async benchmarkStringOperators() {
    console.log('🔍 Benchmarking String Operators...');
    
    const stringData = this.testData.medium.string;
    const results = {};
    
    // Benchmark string concatenation
    const concatResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@string', {
        operation: 'concat',
        strings: [stringData, ' ', 'World']
      });
    });
    results.concat = concatResult;
    
    // Benchmark string replacement
    const replaceResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@string', {
        operation: 'replace',
        input: stringData,
        search: 'x',
        replace: 'y'
      });
    });
    results.replace = replaceResult;
    
    // Benchmark string splitting
    const splitResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@string', {
        operation: 'split',
        input: stringData,
        delimiter: 'x'
      });
    });
    results.split = splitResult;
    
    // Benchmark string case operations
    const caseResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@string', {
        operation: 'upper',
        input: stringData
      });
    });
    results.case = caseResult;
    
    this.performanceMetrics.string = results;
    return results;
  }

  /**
   * Benchmark array operators
   */
  async benchmarkArrayOperators() {
    console.log('🔍 Benchmarking Array Operators...');
    
    const arrayData = this.testData.medium.array;
    const results = {};
    
    // Benchmark for operator
    const forResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@for', {
        array: arrayData,
        template: 'Item: {item}'
      });
    });
    results.for = forResult;
    
    // Benchmark each operator
    const eachResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@each', {
        array: arrayData,
        action: 'multiply',
        factor: 2
      });
    });
    results.each = eachResult;
    
    // Benchmark filter operator
    const filterResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@filter', {
        array: arrayData,
        condition: 'item > 500'
      });
    });
    results.filter = filterResult;
    
    this.performanceMetrics.array = results;
    return results;
  }

  /**
   * Benchmark object operators
   */
  async benchmarkObjectOperators() {
    console.log('🔍 Benchmarking Object Operators...');
    
    const objectData = this.testData.medium.object;
    const results = {};
    
    // Benchmark JSON parsing
    const parseResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@json', {
        operation: 'parse',
        input: JSON.stringify(objectData)
      });
    });
    results.parse = parseResult;
    
    // Benchmark JSON stringifying
    const stringifyResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@json', {
        operation: 'stringify',
        input: objectData
      });
    });
    results.stringify = stringifyResult;
    
    // Benchmark template rendering
    const templateResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@template', {
        template: 'User: {name}, Age: {age}',
        data: objectData
      });
    });
    results.template = templateResult;
    
    this.performanceMetrics.object = results;
    return results;
  }

  /**
   * Benchmark security operators
   */
  async benchmarkSecurityOperators() {
    console.log('🔍 Benchmarking Security Operators...');
    
    const stringData = this.testData.small.string;
    const results = {};
    
    // Benchmark encryption
    const encryptResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@encrypt', {
        text: stringData,
        key: 'test-key-32-chars-long-secret',
        algorithm: 'aes-256-cbc'
      });
    });
    results.encrypt = encryptResult;
    
    // Benchmark decryption
    const encrypted = await this.tusk.executeOperator('@encrypt', {
      text: stringData,
      key: 'test-key-32-chars-long-secret',
      algorithm: 'aes-256-cbc'
    });
    
    const decryptResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@decrypt', {
        text: encrypted,
        key: 'test-key-32-chars-long-secret',
        algorithm: 'aes-256-cbc'
      });
    });
    results.decrypt = decryptResult;
    
    // Benchmark hashing
    const hashResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@hash', {
        text: stringData,
        algorithm: 'sha256'
      });
    });
    results.hash = hashResult;
    
    this.performanceMetrics.security = results;
    return results;
  }

  /**
   * Benchmark database operators
   */
  async benchmarkDatabaseOperators() {
    console.log('🔍 Benchmarking Database Operators...');
    
    const results = {};
    
    // Benchmark query execution
    const queryResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@query', {
        query: 'SELECT * FROM users LIMIT 10',
        database: 'test_db'
      });
    }, 100); // Fewer iterations for database operations
    results.query = queryResult;
    
    // Benchmark cache operations
    const cacheResult = await this.measureExecutionTime(async () => {
      await this.tusk.executeOperator('@cache', {
        operation: 'set',
        key: 'test-key',
        value: 'test-value'
      });
    });
    results.cache = cacheResult;
    
    this.performanceMetrics.database = results;
    return results;
  }

  /**
   * Memory leak detection test
   */
  async detectMemoryLeaks() {
    console.log('🔍 Detecting Memory Leaks...');
    
    const initialSnapshot = this.takeMemorySnapshot('initial');
    const results = [];
    
    // Run operations that might cause memory leaks
    for (let i = 0; i < 1000; i++) {
      await this.tusk.executeOperator('@string', {
        operation: 'concat',
        strings: ['Hello', ' ', 'World', ' ', i.toString()]
      });
      
      await this.tusk.executeOperator('@cache', {
        operation: 'set',
        key: `key-${i}`,
        value: `value-${i}`
      });
      
      if (i % 100 === 0) {
        const snapshot = this.takeMemorySnapshot(`iteration-${i}`);
        results.push({
          iteration: i,
          heapUsed: snapshot.heapUsed,
          heapTotal: snapshot.heapTotal,
          increase: snapshot.heapUsed - initialSnapshot.heapUsed
        });
      }
    }
    
    const finalSnapshot = this.takeMemorySnapshot('final');
    const totalIncrease = finalSnapshot.heapUsed - initialSnapshot.heapUsed;
    
    // Check for significant memory increase (>10MB)
    const memoryLeakDetected = totalIncrease > 10 * 1024 * 1024;
    
    this.performanceMetrics.memoryLeak = {
      detected: memoryLeakDetected,
      totalIncrease: totalIncrease,
      increaseMB: totalIncrease / (1024 * 1024),
      snapshots: results
    };
    
    return this.performanceMetrics.memoryLeak;
  }

  /**
   * Throughput testing for high-concurrency scenarios
   */
  async testThroughput() {
    console.log('🔍 Testing Throughput...');
    
    const concurrencyLevels = [1, 10, 50, 100, 200];
    const results = {};
    
    for (const concurrency of concurrencyLevels) {
      console.log(`  Testing concurrency level: ${concurrency}`);
      
      const startTime = performance.now();
      const promises = [];
      
      for (let i = 0; i < concurrency; i++) {
        promises.push(
          this.tusk.executeOperator('@string', {
            operation: 'concat',
            strings: ['Hello', ' ', 'World', ' ', i.toString()]
          })
        );
      }
      
      await Promise.all(promises);
      const endTime = performance.now();
      const duration = endTime - startTime;
      
      results[concurrency] = {
        concurrency,
        duration,
        operationsPerSecond: (concurrency / duration) * 1000,
        avgLatency: duration / concurrency
      };
    }
    
    this.performanceMetrics.throughput = results;
    return results;
  }

  /**
   * Latency measurement for all operator categories
   */
  async measureLatency() {
    console.log('🔍 Measuring Latency...');
    
    const operators = [
      { name: 'string', operator: '@string', params: { operation: 'concat', strings: ['Hello', ' ', 'World'] } },
      { name: 'json', operator: '@json', params: { operation: 'parse', input: '{"name": "John"}' } },
      { name: 'cache', operator: '@cache', params: { operation: 'set', key: 'test', value: 'value' } },
      { name: 'hash', operator: '@hash', params: { text: 'Hello', algorithm: 'sha256' } },
      { name: 'base64', operator: '@base64', params: { operation: 'encode', text: 'Hello' } },
      { name: 'template', operator: '@template', params: { template: 'Hello {name}', data: { name: 'World' } } }
    ];
    
    const results = {};
    
    for (const op of operators) {
      const latencies = [];
      
      // Measure latency 100 times for each operator
      for (let i = 0; i < 100; i++) {
        const startTime = performance.now();
        await this.tusk.executeOperator(op.operator, op.params);
        const endTime = performance.now();
        latencies.push(endTime - startTime);
      }
      
      // Calculate statistics
      const sorted = latencies.sort((a, b) => a - b);
      const min = sorted[0];
      const max = sorted[sorted.length - 1];
      const avg = latencies.reduce((a, b) => a + b, 0) / latencies.length;
      const median = sorted[Math.floor(sorted.length / 2)];
      const p95 = sorted[Math.floor(sorted.length * 0.95)];
      const p99 = sorted[Math.floor(sorted.length * 0.99)];
      
      results[op.name] = {
        min,
        max,
        avg,
        median,
        p95,
        p99,
        samples: latencies.length
      };
    }
    
    this.performanceMetrics.latency = results;
    return results;
  }

  /**
   * Resource utilization monitoring
   */
  async monitorResourceUtilization() {
    console.log('🔍 Monitoring Resource Utilization...');
    
    const startTime = Date.now();
    const measurements = [];
    
    // Monitor for 10 seconds
    const monitorInterval = setInterval(() => {
      const measurement = {
        timestamp: Date.now(),
        memory: process.memoryUsage(),
        cpu: process.cpuUsage(),
        uptime: process.uptime()
      };
      measurements.push(measurement);
    }, 100);
    
    // Run intensive operations during monitoring
    const operations = [];
    for (let i = 0; i < 1000; i++) {
      operations.push(
        this.tusk.executeOperator('@string', {
          operation: 'concat',
          strings: ['Hello', ' ', 'World', ' ', i.toString()]
        })
      );
    }
    
    await Promise.all(operations);
    
    setTimeout(() => {
      clearInterval(monitorInterval);
    }, 10000);
    
    // Calculate utilization statistics
    const memoryUtilization = measurements.map(m => ({
      timestamp: m.timestamp,
      heapUsed: m.memory.heapUsed,
      heapTotal: m.memory.heapTotal,
      external: m.memory.external,
      rss: m.memory.rss
    }));
    
    const cpuUtilization = measurements.map(m => ({
      timestamp: m.timestamp,
      user: m.cpu.user,
      system: m.cpu.system
    }));
    
    this.performanceMetrics.resourceUtilization = {
      memory: memoryUtilization,
      cpu: cpuUtilization,
      duration: Date.now() - startTime,
      measurements: measurements.length
    };
    
    return this.performanceMetrics.resourceUtilization;
  }

  /**
   * Load testing with gradual ramp-up and sustained traffic
   */
  async loadTest() {
    console.log('🔍 Running Load Test...');
    
    const phases = [
      { duration: 10000, users: 10, name: 'ramp-up' },
      { duration: 30000, users: 50, name: 'sustained' },
      { duration: 10000, users: 100, name: 'peak' },
      { duration: 10000, users: 25, name: 'ramp-down' }
    ];
    
    const results = [];
    
    for (const phase of phases) {
      console.log(`  Phase: ${phase.name} - ${phase.users} users for ${phase.duration}ms`);
      
      const phaseStart = Date.now();
      const phaseResults = [];
      
      // Create user simulation
      const userPromises = [];
      for (let i = 0; i < phase.users; i++) {
        userPromises.push(this.simulateUser(i, phase.duration));
      }
      
      const userResults = await Promise.all(userPromises);
      
      // Flatten results
      userResults.forEach(result => {
        phaseResults.push(...result);
      });
      
      const phaseEnd = Date.now();
      const phaseDuration = phaseEnd - phaseStart;
      
      // Calculate phase statistics
      const responseTimes = phaseResults.map(r => r.responseTime);
      const sorted = responseTimes.sort((a, b) => a - b);
      
      const phaseStats = {
        phase: phase.name,
        users: phase.users,
        duration: phaseDuration,
        requests: phaseResults.length,
        avgResponseTime: responseTimes.reduce((a, b) => a + b, 0) / responseTimes.length,
        minResponseTime: sorted[0],
        maxResponseTime: sorted[sorted.length - 1],
        medianResponseTime: sorted[Math.floor(sorted.length / 2)],
        p95ResponseTime: sorted[Math.floor(sorted.length * 0.95)],
        p99ResponseTime: sorted[Math.floor(sorted.length * 0.99)],
        requestsPerSecond: (phaseResults.length / phaseDuration) * 1000,
        errors: phaseResults.filter(r => r.error).length,
        successRate: ((phaseResults.length - phaseResults.filter(r => r.error).length) / phaseResults.length) * 100
      };
      
      results.push(phaseStats);
    }
    
    this.performanceMetrics.loadTest = results;
    return results;
  }

  /**
   * Simulate a single user for load testing
   */
  async simulateUser(userId, duration) {
    const results = [];
    const startTime = Date.now();
    
    while (Date.now() - startTime < duration) {
      const operationStart = performance.now();
      
      try {
        // Randomly choose an operation
        const operations = [
          { operator: '@string', params: { operation: 'concat', strings: ['Hello', ' ', 'User', ' ', userId.toString()] } },
          { operator: '@json', params: { operation: 'parse', input: `{"userId": ${userId}, "timestamp": ${Date.now()}}` } },
          { operator: '@cache', params: { operation: 'set', key: `user-${userId}-${Date.now()}`, value: 'test-value' } },
          { operator: '@hash', params: { text: `user-${userId}`, algorithm: 'sha256' } },
          { operator: '@template', params: { template: 'Hello {name}!', data: { name: `User${userId}` } } }
        ];
        
        const operation = operations[Math.floor(Math.random() * operations.length)];
        await this.tusk.executeOperator(operation.operator, operation.params);
        
        const operationEnd = performance.now();
        results.push({
          userId,
          operation: operation.operator,
          responseTime: operationEnd - operationStart,
          timestamp: Date.now(),
          error: null
        });
        
      } catch (error) {
        const operationEnd = performance.now();
        results.push({
          userId,
          operation: 'unknown',
          responseTime: operationEnd - operationStart,
          timestamp: Date.now(),
          error: error.message
        });
      }
      
      // Random delay between operations (10-100ms)
      await new Promise(resolve => setTimeout(resolve, Math.random() * 90 + 10));
    }
    
    return results;
  }

  /**
   * Run complete performance benchmarking suite
   */
  async runCompleteSuite() {
    console.log('🚀 Starting TuskLang Performance Benchmarking Suite...');
    
    try {
      // Take initial memory snapshot
      this.takeMemorySnapshot('benchmark-start');
      
      // Run all benchmarks
      await this.benchmarkStringOperators();
      await this.benchmarkArrayOperators();
      await this.benchmarkObjectOperators();
      await this.benchmarkSecurityOperators();
      await this.benchmarkDatabaseOperators();
      
      // Run advanced performance tests
      await this.detectMemoryLeaks();
      await this.testThroughput();
      await this.measureLatency();
      await this.monitorResourceUtilization();
      await this.loadTest();
      
      // Take final memory snapshot
      this.takeMemorySnapshot('benchmark-end');
      
      const report = this.generateBenchmarkReport();
      
      console.log('✅ Performance Benchmarking Suite completed successfully');
      return report;
      
    } catch (error) {
      console.error('❌ Performance Benchmarking Suite failed:', error);
      throw error;
    }
  }

  /**
   * Generate comprehensive benchmark report
   */
  generateBenchmarkReport() {
    const totalDuration = Date.now() - this.startTime;
    
    const report = {
      summary: {
        totalDuration: `${totalDuration}ms`,
        memorySnapshots: this.memorySnapshots.length,
        benchmarks: Object.keys(this.performanceMetrics).length
      },
      performance: this.performanceMetrics,
      memory: {
        snapshots: this.memorySnapshots,
        analysis: this.analyzeMemoryUsage()
      },
      recommendations: this.generateRecommendations(),
      timestamp: new Date().toISOString()
    };
    
    console.log('\n📊 PERFORMANCE BENCHMARKING REPORT');
    console.log('==================================');
    console.log(`Total Duration: ${totalDuration}ms`);
    console.log(`Memory Snapshots: ${this.memorySnapshots.length}`);
    console.log(`Benchmarks: ${Object.keys(this.performanceMetrics).length}`);
    
    // Display key performance metrics
    if (this.performanceMetrics.latency) {
      console.log('\n🏃 LATENCY METRICS (avg ms):');
      Object.entries(this.performanceMetrics.latency).forEach(([op, metrics]) => {
        console.log(`  ${op}: ${metrics.avg.toFixed(3)}ms (p95: ${metrics.p95.toFixed(3)}ms)`);
      });
    }
    
    if (this.performanceMetrics.throughput) {
      console.log('\n⚡ THROUGHPUT METRICS:');
      Object.entries(this.performanceMetrics.throughput).forEach(([concurrency, metrics]) => {
        console.log(`  ${concurrency} users: ${metrics.operationsPerSecond.toFixed(2)} ops/sec`);
      });
    }
    
    if (this.performanceMetrics.memoryLeak) {
      console.log('\n🧠 MEMORY LEAK ANALYSIS:');
      const leak = this.performanceMetrics.memoryLeak;
      console.log(`  Detected: ${leak.detected ? 'YES' : 'NO'}`);
      console.log(`  Total Increase: ${leak.increaseMB.toFixed(2)}MB`);
    }
    
    return report;
  }

  /**
   * Analyze memory usage patterns
   */
  analyzeMemoryUsage() {
    if (this.memorySnapshots.length < 2) return null;
    
    const first = this.memorySnapshots[0];
    const last = this.memorySnapshots[this.memorySnapshots.length - 1];
    
    return {
      totalIncrease: last.heapUsed - first.heapUsed,
      totalIncreaseMB: (last.heapUsed - first.heapUsed) / (1024 * 1024),
      peakUsage: Math.max(...this.memorySnapshots.map(s => s.heapUsed)),
      peakUsageMB: Math.max(...this.memorySnapshots.map(s => s.heapUsed)) / (1024 * 1024),
      averageUsage: this.memorySnapshots.reduce((sum, s) => sum + s.heapUsed, 0) / this.memorySnapshots.length,
      averageUsageMB: this.memorySnapshots.reduce((sum, s) => sum + s.heapUsed, 0) / this.memorySnapshots.length / (1024 * 1024)
    };
  }

  /**
   * Generate performance recommendations
   */
  generateRecommendations() {
    const recommendations = [];
    
    // Analyze latency
    if (this.performanceMetrics.latency) {
      Object.entries(this.performanceMetrics.latency).forEach(([op, metrics]) => {
        if (metrics.avg > 50) {
          recommendations.push(`Optimize ${op} operator - average latency ${metrics.avg.toFixed(3)}ms exceeds 50ms threshold`);
        }
        if (metrics.p95 > 100) {
          recommendations.push(`Investigate ${op} operator - 95th percentile latency ${metrics.p95.toFixed(3)}ms is too high`);
        }
      });
    }
    
    // Analyze throughput
    if (this.performanceMetrics.throughput) {
      const maxThroughput = Math.max(...Object.values(this.performanceMetrics.throughput).map(m => m.operationsPerSecond));
      if (maxThroughput < 1000) {
        recommendations.push(`Improve overall throughput - maximum ${maxThroughput.toFixed(2)} ops/sec is below 1000 ops/sec target`);
      }
    }
    
    // Analyze memory usage
    if (this.performanceMetrics.memoryLeak && this.performanceMetrics.memoryLeak.detected) {
      recommendations.push(`Fix memory leak - ${this.performanceMetrics.memoryLeak.increaseMB.toFixed(2)}MB increase detected`);
    }
    
    // Analyze load test results
    if (this.performanceMetrics.loadTest) {
      this.performanceMetrics.loadTest.forEach(phase => {
        if (phase.successRate < 95) {
          recommendations.push(`Improve reliability in ${phase.phase} phase - ${phase.successRate.toFixed(1)}% success rate below 95% target`);
        }
      });
    }
    
    return recommendations;
  }
}

module.exports = { PerformanceBenchmarking }; 
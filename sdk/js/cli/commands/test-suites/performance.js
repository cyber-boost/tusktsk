/**
 * Performance Test Suite for TuskLang CLI
 * ========================================
 * Benchmarks TuskLang operations
 */

const TuskLang = require('../../../index.js');
const PeanutConfig = require('../../../peanut-config.js');

async function runPerformanceTests() {
  const results = {
    total: 0,
    passed: 0,
    failed: 0,
    tests: []
  };

  // Test 1: Text parsing performance
  try {
    console.log('  🔄 Benchmarking text parsing...');
    
    const largeConfig = generateLargeConfig(1000); // 1000 lines
    const tusk = new TuskLang();
    
    const startTime = process.hrtime.bigint();
    
    for (let i = 0; i < 100; i++) {
      tusk.parse(largeConfig);
    }
    
    const endTime = process.hrtime.bigint();
    const duration = Number(endTime - startTime) / 1000000; // Convert to milliseconds
    const avgTime = duration / 100;
    
    console.log(`    Text parsing: ${avgTime.toFixed(2)}ms per parse (100 iterations)`);
    
    if (avgTime < 10) { // Should be under 10ms per parse
      results.passed++;
      results.tests.push({ name: 'Text parsing performance', status: 'passed', metric: `${avgTime.toFixed(2)}ms` });
    } else {
      results.failed++;
      results.tests.push({ name: 'Text parsing performance', status: 'failed', error: `Too slow: ${avgTime.toFixed(2)}ms` });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Text parsing performance', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 2: Binary loading performance
  try {
    console.log('  🔄 Benchmarking binary loading...');
    
    const peanutConfig = new PeanutConfig();
    const testConfig = generateLargeConfig(1000);
    
    // Create binary file
    const binaryFile = 'perf_test.pnt';
    peanutConfig.compileToBinary(testConfig, binaryFile);
    
    const startTime = process.hrtime.bigint();
    
    for (let i = 0; i < 100; i++) {
      peanutConfig.loadBinary(binaryFile);
    }
    
    const endTime = process.hrtime.bigint();
    const duration = Number(endTime - startTime) / 1000000;
    const avgTime = duration / 100;
    
    console.log(`    Binary loading: ${avgTime.toFixed(2)}ms per load (100 iterations)`);
    
    // Clean up
    const fs = require('fs');
    fs.unlinkSync(binaryFile);
    
    if (avgTime < 5) { // Should be under 5ms per load
      results.passed++;
      results.tests.push({ name: 'Binary loading performance', status: 'passed', metric: `${avgTime.toFixed(2)}ms` });
    } else {
      results.failed++;
      results.tests.push({ name: 'Binary loading performance', status: 'failed', error: `Too slow: ${avgTime.toFixed(2)}ms` });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Binary loading performance', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 3: FUJSEN execution performance
  try {
    console.log('  🔄 Benchmarking FUJSEN execution...');
    
    const { TSK } = require('../../../tsk.js');
    const tsk = new TSK();
    
    // Create a simple FUJSEN function
    tsk.setFujsen('math', 'add', function(a, b) {
      return a + b;
    });
    
    const startTime = process.hrtime.bigint();
    
    for (let i = 0; i < 10000; i++) {
      tsk.executeFujsen('math', 'add', i, i + 1);
    }
    
    const endTime = process.hrtime.bigint();
    const duration = Number(endTime - startTime) / 1000000;
    const avgTime = duration / 10000;
    
    console.log(`    FUJSEN execution: ${avgTime.toFixed(4)}ms per call (10,000 iterations)`);
    
    if (avgTime < 0.1) { // Should be under 0.1ms per call
      results.passed++;
      results.tests.push({ name: 'FUJSEN execution performance', status: 'passed', metric: `${avgTime.toFixed(4)}ms` });
    } else {
      results.failed++;
      results.tests.push({ name: 'FUJSEN execution performance', status: 'failed', error: `Too slow: ${avgTime.toFixed(4)}ms` });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'FUJSEN execution performance', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 4: Memory usage
  try {
    console.log('  🔄 Benchmarking memory usage...');
    
    const initialMemory = process.memoryUsage().heapUsed;
    const tusk = new TuskLang();
    
    // Parse large configuration multiple times
    const largeConfig = generateLargeConfig(5000); // 5000 lines
    
    for (let i = 0; i < 50; i++) {
      tusk.parse(largeConfig);
    }
    
    const finalMemory = process.memoryUsage().heapUsed;
    const memoryIncrease = (finalMemory - initialMemory) / 1024 / 1024; // MB
    
    console.log(`    Memory usage: ${memoryIncrease.toFixed(2)}MB increase`);
    
    if (memoryIncrease < 50) { // Should use less than 50MB additional memory
      results.passed++;
      results.tests.push({ name: 'Memory usage', status: 'passed', metric: `${memoryIncrease.toFixed(2)}MB` });
    } else {
      results.failed++;
      results.tests.push({ name: 'Memory usage', status: 'failed', error: `Too much memory: ${memoryIncrease.toFixed(2)}MB` });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Memory usage', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 5: Stringify performance
  try {
    console.log('  🔄 Benchmarking stringify performance...');
    
    const largeObject = generateLargeObject(1000); // 1000 properties
    const tusk = new TuskLang();
    
    const startTime = process.hrtime.bigint();
    
    for (let i = 0; i < 50; i++) {
      TuskLang.stringify(largeObject);
    }
    
    const endTime = process.hrtime.bigint();
    const duration = Number(endTime - startTime) / 1000000;
    const avgTime = duration / 50;
    
    console.log(`    Stringify: ${avgTime.toFixed(2)}ms per stringify (50 iterations)`);
    
    if (avgTime < 20) { // Should be under 20ms per stringify
      results.passed++;
      results.tests.push({ name: 'Stringify performance', status: 'passed', metric: `${avgTime.toFixed(2)}ms` });
    } else {
      results.failed++;
      results.tests.push({ name: 'Stringify performance', status: 'failed', error: `Too slow: ${avgTime.toFixed(2)}ms` });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Stringify performance', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 6: Database query performance (mock)
  try {
    console.log('  🔄 Benchmarking database query performance...');
    
    const tusk = new TuskLang();
    
    // Mock database adapter
    const mockAdapter = {
      async query(sql, params) {
        // Simulate database query time
        await new Promise(resolve => setTimeout(resolve, 1));
        return [{ count: 42 }];
      }
    };
    
    tusk.parser.setDatabaseAdapter(mockAdapter);
    
    const config = `
      user_count: @query("SELECT COUNT(*) FROM users")
    `;
    
    const startTime = process.hrtime.bigint();
    
    for (let i = 0; i < 10; i++) {
      await tusk.parser.parse(config);
    }
    
    const endTime = process.hrtime.bigint();
    const duration = Number(endTime - startTime) / 1000000;
    const avgTime = duration / 10;
    
    console.log(`    Database queries: ${avgTime.toFixed(2)}ms per query (10 iterations)`);
    
    if (avgTime < 50) { // Should be under 50ms per query (including mock delay)
      results.passed++;
      results.tests.push({ name: 'Database query performance', status: 'passed', metric: `${avgTime.toFixed(2)}ms` });
    } else {
      results.failed++;
      results.tests.push({ name: 'Database query performance', status: 'failed', error: `Too slow: ${avgTime.toFixed(2)}ms` });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Database query performance', status: 'failed', error: error.message });
    results.total++;
  }

  // Print test results
  console.log(`  Performance Tests: ${results.passed}/${results.total} passed`);
  for (const test of results.tests) {
    const status = test.status === 'passed' ? '✅' : '❌';
    console.log(`    ${status} ${test.name}${test.metric ? ` (${test.metric})` : ''}`);
    if (test.error) {
      console.log(`      Error: ${test.error}`);
    }
  }

  return results;
}

// Helper function to generate large configuration
function generateLargeConfig(lines) {
  let config = '';
  
  for (let i = 0; i < lines; i++) {
    if (i % 10 === 0) {
      config += `[section_${Math.floor(i / 10)}]\n`;
    }
    config += `key_${i}: "value_${i}"\n`;
  }
  
  return config;
}

// Helper function to generate large object
function generateLargeObject(properties) {
  const obj = {};
  
  for (let i = 0; i < properties; i++) {
    obj[`key_${i}`] = `value_${i}`;
  }
  
  return obj;
}

module.exports = runPerformanceTests; 
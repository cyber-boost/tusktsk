/**
 * Testing Commands for TuskLang CLI
 * ==================================
 * Implements all testing operations from the CLI specification
 */

const fs = require('fs').promises;
const path = require('path');
const TuskLang = require('../../index.js');

// Test suites
const testSuites = {
  parser: require('./test-suites/parser.js'),
  fujsen: require('./test-suites/fujsen.js'),
  sdk: require('./test-suites/sdk.js'),
  performance: require('./test-suites/performance.js')
};

// Main test runner
async function run(suite = 'all') {
  try {
    console.log(`🧪 Running TuskLang test suite: ${suite}`);
    
    const results = {
      total: 0,
      passed: 0,
      failed: 0,
      suites: {}
    };
    
    if (suite === 'all') {
      // Run all test suites
      for (const [suiteName, suiteRunner] of Object.entries(testSuites)) {
        console.log(`\n📍 Running ${suiteName} tests...`);
        const suiteResults = await suiteRunner();
        results.suites[suiteName] = suiteResults;
        results.total += suiteResults.total;
        results.passed += suiteResults.passed;
        results.failed += suiteResults.failed;
      }
    } else if (testSuites[suite]) {
      // Run specific test suite
      console.log(`\n📍 Running ${suite} tests...`);
      const suiteResults = await testSuites[suite]();
      results.suites[suite] = suiteResults;
      results.total = suiteResults.total;
      results.passed = suiteResults.passed;
      results.failed = suiteResults.failed;
    } else {
      throw new Error(`Unknown test suite: ${suite}`);
    }
    
    // Print summary
    console.log('\n📊 Test Results Summary:');
    console.log('========================');
    console.log(`Total Tests: ${results.total}`);
    console.log(`Passed: ${results.passed} ✅`);
    console.log(`Failed: ${results.failed} ❌`);
    console.log(`Success Rate: ${((results.passed / results.total) * 100).toFixed(1)}%`);
    
    if (results.failed > 0) {
      console.log('\n❌ Some tests failed. Check the output above for details.');
      return { success: false, results };
    } else {
      console.log('\n✅ All tests passed!');
      return { success: true, results };
    }
  } catch (error) {
    console.error('❌ Test execution failed:', error.message);
    return { success: false, error: error.message };
  }
}

module.exports = {
  run
}; 
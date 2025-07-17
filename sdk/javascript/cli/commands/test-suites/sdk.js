/**
 * SDK Test Suite for TuskLang CLI
 * ================================
 * Tests SDK-specific features
 */

const TuskLang = require('../../../index.js');
const PeanutConfig = require('../../../peanut-config.js');

async function runSdkTests() {
  const results = {
    total: 0,
    passed: 0,
    failed: 0,
    tests: []
  };

  // Test 1: Database adapter integration
  try {
    const tusk = new TuskLang();
    const SQLiteAdapter = require('../../../adapters/sqlite.js');
    const adapter = new SQLiteAdapter({ filename: ':memory:' });
    
    tusk.setDatabase({
      type: 'sqlite',
      filename: ':memory:'
    });
    
    // Test that database adapter is set
    if (tusk.parser.databaseAdapter) {
      results.passed++;
      results.tests.push({ name: 'Database adapter integration', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Database adapter integration', status: 'failed', error: 'Database adapter not set' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Database adapter integration', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 2: PeanutConfig integration
  try {
    const peanutConfig = new PeanutConfig();
    const config = peanutConfig.load(process.cwd());
    
    if (typeof config === 'object') {
      results.passed++;
      results.tests.push({ name: 'PeanutConfig integration', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'PeanutConfig integration', status: 'failed', error: 'Config not loaded correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'PeanutConfig integration', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 3: TuskLang stringify
  try {
    const data = {
      name: 'TestApp',
      version: '1.0.0',
      settings: {
        debug: true,
        port: 8080
      }
    };
    
    const tskString = TuskLang.stringify(data);
    
    if (tskString.includes('name: "TestApp"') && tskString.includes('debug: true')) {
      results.passed++;
      results.tests.push({ name: 'TuskLang stringify', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'TuskLang stringify', status: 'failed', error: 'Stringify not working correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'TuskLang stringify', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 4: Available adapters
  try {
    const adapters = TuskLang.getAvailableAdapters();
    
    if (Array.isArray(adapters) && adapters.length > 0) {
      results.passed++;
      results.tests.push({ name: 'Available adapters', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Available adapters', status: 'failed', error: 'No adapters available' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Available adapters', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 5: Enhanced parser features
  try {
    const parser = new TuskLang.TuskLangEnhanced();
    
    // Test global variables
    parser.globalVariables.testVar = 'test_value';
    
    if (parser.globalVariables.testVar === 'test_value') {
      results.passed++;
      results.tests.push({ name: 'Enhanced parser features', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Enhanced parser features', status: 'failed', error: 'Global variables not working' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Enhanced parser features', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 6: File parsing
  try {
    const tusk = new TuskLang();
    const testContent = `
      name: "FileTest"
      version: "1.0"
    `;
    
    const parsed = tusk.parse(testContent);
    
    if (parsed.name === 'FileTest' && parsed.version === '1.0') {
      results.passed++;
      results.tests.push({ name: 'File parsing', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'File parsing', status: 'failed', error: 'File content not parsed correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'File parsing', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 7: Binary compilation
  try {
    const peanutConfig = new PeanutConfig();
    const testConfig = {
      app: {
        name: 'TestApp',
        version: '1.0.0'
      }
    };
    
    const binaryFile = 'test_config.pnt';
    peanutConfig.compileToBinary(testConfig, binaryFile);
    
    // Check if binary file was created
    const fs = require('fs');
    if (fs.existsSync(binaryFile)) {
      // Clean up
      fs.unlinkSync(binaryFile);
      results.passed++;
      results.tests.push({ name: 'Binary compilation', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Binary compilation', status: 'failed', error: 'Binary file not created' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Binary compilation', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 8: Cross-file communication (mock)
  try {
    const parser = new TuskLang.TuskLangEnhanced();
    
    // Test cross-file get
    const result = parser.crossFileGet('test', 'key');
    
    // This should return null in test environment, which is expected
    if (result === null) {
      results.passed++;
      results.tests.push({ name: 'Cross-file communication', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Cross-file communication', status: 'failed', error: 'Cross-file not working as expected' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Cross-file communication', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 9: Cache operations
  try {
    const parser = new TuskLang.TuskLangEnhanced();
    
    // Test cache set/get
    const cacheKey = 'test_key';
    const cacheValue = 'test_value';
    parser.cache.set(cacheKey, cacheValue);
    
    const retrieved = parser.cache.get(cacheKey);
    
    if (retrieved === cacheValue) {
      results.passed++;
      results.tests.push({ name: 'Cache operations', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Cache operations', status: 'failed', error: 'Cache not working correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Cache operations', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 10: Error handling
  try {
    const tusk = new TuskLang();
    
    // Test invalid configuration
    const invalidConfig = `
      name: "Test"
      invalid_syntax: {
        missing_closing_brace
    `;
    
    let errorCaught = false;
    try {
      tusk.parse(invalidConfig);
    } catch (error) {
      errorCaught = true;
    }
    
    if (errorCaught) {
      results.passed++;
      results.tests.push({ name: 'Error handling', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Error handling', status: 'failed', error: 'Should have caught parsing error' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Error handling', status: 'failed', error: error.message });
    results.total++;
  }

  // Print test results
  console.log(`  SDK Tests: ${results.passed}/${results.total} passed`);
  for (const test of results.tests) {
    const status = test.status === 'passed' ? '✅' : '❌';
    console.log(`    ${status} ${test.name}`);
    if (test.error) {
      console.log(`      Error: ${test.error}`);
    }
  }

  return results;
}

module.exports = runSdkTests; 
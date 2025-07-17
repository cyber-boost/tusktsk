/**
 * Parser Test Suite for TuskLang CLI
 * ===================================
 * Tests TuskLang parsing functionality
 */

const TuskLang = require('../../../index.js');

async function runParserTests() {
  const results = {
    total: 0,
    passed: 0,
    failed: 0,
    tests: []
  };

  const tusk = new TuskLang();

  // Test 1: Basic parsing
  try {
    const config = `
      name: "TestApp"
      version: "1.0.0"
      debug: true
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.name === 'TestApp' && parsed.version === '1.0.0' && parsed.debug === true) {
      results.passed++;
      results.tests.push({ name: 'Basic parsing', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Basic parsing', status: 'failed', error: 'Values not parsed correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Basic parsing', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 2: Section parsing
  try {
    const config = `
      [database]
      host: "localhost"
      port: 5432
      
      [server]
      host: "0.0.0.0"
      port: 8080
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.database && parsed.database.host === 'localhost' && 
        parsed.server && parsed.server.host === '0.0.0.0') {
      results.passed++;
      results.tests.push({ name: 'Section parsing', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Section parsing', status: 'failed', error: 'Sections not parsed correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Section parsing', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 3: Variable references
  try {
    const config = `
      $app_name: "MyApp"
      $port: 3000
      
      name: $app_name
      server_port: $port
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.name === 'MyApp' && parsed.server_port === 3000) {
      results.passed++;
      results.tests.push({ name: 'Variable references', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Variable references', status: 'failed', error: 'Variables not resolved correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Variable references', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 4: Multiple syntax styles
  try {
    const config = `
      [database]
      host: "localhost"
      
      server {
        port: 8080
      }
      
      cache >
        ttl: 300
      <
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.database && parsed.server && parsed.cache) {
      results.passed++;
      results.tests.push({ name: 'Multiple syntax styles', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Multiple syntax styles', status: 'failed', error: 'Mixed syntax not parsed correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Multiple syntax styles', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 5: Arrays and objects
  try {
    const config = `
      servers: ["web1", "web2", "web3"]
      settings: {
        timeout: 30
        retries: 3
      }
    `;
    const parsed = tusk.parse(config);
    
    if (Array.isArray(parsed.servers) && parsed.servers.length === 3 &&
        parsed.settings && parsed.settings.timeout === 30) {
      results.passed++;
      results.tests.push({ name: 'Arrays and objects', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Arrays and objects', status: 'failed', error: 'Arrays/objects not parsed correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Arrays and objects', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 6: Date functions
  try {
    const config = `
      timestamp: @date("Y-m-d H:i:s")
      year: @date("Y")
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.timestamp && parsed.year) {
      results.passed++;
      results.tests.push({ name: 'Date functions', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Date functions', status: 'failed', error: 'Date functions not executed' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Date functions', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 7: Conditional expressions
  try {
    const config = `
      $env: "production"
      debug: $env == "development" ? true : false
      workers: $env == "production" ? 8 : 2
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.debug === false && parsed.workers === 8) {
      results.passed++;
      results.tests.push({ name: 'Conditional expressions', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Conditional expressions', status: 'failed', error: 'Conditionals not evaluated correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Conditional expressions', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 8: Ranges
  try {
    const config = `
      port_range: 8000-9000
      worker_range: 1-10
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.port_range && parsed.port_range.type === 'range' &&
        parsed.worker_range && parsed.worker_range.type === 'range') {
      results.passed++;
      results.tests.push({ name: 'Ranges', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Ranges', status: 'failed', error: 'Ranges not parsed correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Ranges', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 9: Environment variables
  try {
    const config = `
      node_env: @env("NODE_ENV", "development")
      api_key: @env("API_KEY", "default_key")
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.node_env && parsed.api_key) {
      results.passed++;
      results.tests.push({ name: 'Environment variables', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Environment variables', status: 'failed', error: 'Environment variables not resolved' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Environment variables', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 10: String concatenation
  try {
    const config = `
      $base: "TuskLang"
      $version: "2.0"
      full_name: $base + " v" + $version
    `;
    const parsed = tusk.parse(config);
    
    if (parsed.full_name === 'TuskLang v2.0') {
      results.passed++;
      results.tests.push({ name: 'String concatenation', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'String concatenation', status: 'failed', error: 'String concatenation not working' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'String concatenation', status: 'failed', error: error.message });
    results.total++;
  }

  // Print test results
  console.log(`  Parser Tests: ${results.passed}/${results.total} passed`);
  for (const test of results.tests) {
    const status = test.status === 'passed' ? '✅' : '❌';
    console.log(`    ${status} ${test.name}`);
    if (test.error) {
      console.log(`      Error: ${test.error}`);
    }
  }

  return results;
}

module.exports = runParserTests; 
/**
 * FUJSEN Test Suite for TuskLang CLI
 * ===================================
 * Tests FUJSEN operators and functions
 */

const { TSK } = require('../../../tsk.js');

async function runFujsenTests() {
  const results = {
    total: 0,
    passed: 0,
    failed: 0,
    tests: []
  };

  // Test 1: Basic FUJSEN function execution
  try {
    const tsk = new TSK();
    tsk.setFujsen('math', 'add', function(a, b) {
      return a + b;
    });
    
    const result = tsk.executeFujsen('math', 'add', 5, 3);
    
    if (result === 8) {
      results.passed++;
      results.tests.push({ name: 'Basic FUJSEN function', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Basic FUJSEN function', status: 'failed', error: 'Function returned wrong result' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Basic FUJSEN function', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 2: FUJSEN with context
  try {
    const tsk = new TSK();
    tsk.setFujsen('greeting', 'hello', function(name) {
      return this.prefix + ' ' + name + '!';
    });
    
    const context = { prefix: 'Hello' };
    const result = tsk.executeFujsenWithContext('greeting', 'hello', context, 'World');
    
    if (result === 'Hello World!') {
      results.passed++;
      results.tests.push({ name: 'FUJSEN with context', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'FUJSEN with context', status: 'failed', error: 'Context not applied correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'FUJSEN with context', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 3: Arrow function FUJSEN
  try {
    const tsk = new TSK();
    tsk.setFujsen('math', 'multiply', (a, b) => a * b);
    
    const result = tsk.executeFujsen('math', 'multiply', 4, 7);
    
    if (result === 28) {
      results.passed++;
      results.tests.push({ name: 'Arrow function FUJSEN', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Arrow function FUJSEN', status: 'failed', error: 'Arrow function not executed' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Arrow function FUJSEN', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 4: FUJSEN from TSK string
  try {
    const tskContent = `
      [calculator]
      add_fujsen = """
        function add(a, b) {
          return a + b;
        }
      """
      
      multiply_fujsen = """
        (a, b) => a * b
      """
    `;
    
    const tsk = TSK.fromString(tskContent);
    const addResult = tsk.executeFujsen('calculator', 'add_fujsen', 10, 5);
    const multiplyResult = tsk.executeFujsen('calculator', 'multiply_fujsen', 6, 8);
    
    if (addResult === 15 && multiplyResult === 48) {
      results.passed++;
      results.tests.push({ name: 'FUJSEN from TSK string', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'FUJSEN from TSK string', status: 'failed', error: 'TSK FUJSEN not parsed correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'FUJSEN from TSK string', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 5: Complex FUJSEN with error handling
  try {
    const tsk = new TSK();
    tsk.setFujsen('validation', 'validateEmail', function(email) {
      if (!email || typeof email !== 'string') {
        throw new Error('Invalid email format');
      }
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      return emailRegex.test(email);
    });
    
    const validResult = tsk.executeFujsen('validation', 'validateEmail', 'test@example.com');
    const invalidResult = tsk.executeFujsen('validation', 'validateEmail', 'invalid-email');
    
    if (validResult === true && invalidResult === false) {
      results.passed++;
      results.tests.push({ name: 'Complex FUJSEN validation', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Complex FUJSEN validation', status: 'failed', error: 'Validation logic incorrect' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Complex FUJSEN validation', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 6: FUJSEN caching
  try {
    const tsk = new TSK();
    let callCount = 0;
    
    tsk.setFujsen('counter', 'increment', function() {
      callCount++;
      return callCount;
    });
    
    // Execute multiple times
    tsk.executeFujsen('counter', 'increment');
    tsk.executeFujsen('counter', 'increment');
    const result = tsk.executeFujsen('counter', 'increment');
    
    if (result === 3) {
      results.passed++;
      results.tests.push({ name: 'FUJSEN caching', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'FUJSEN caching', status: 'failed', error: 'Caching not working correctly' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'FUJSEN caching', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 7: FUJSEN map retrieval
  try {
    const tsk = new TSK();
    tsk.setFujsen('math', 'add', (a, b) => a + b);
    tsk.setFujsen('math', 'subtract', (a, b) => a - b);
    tsk.setFujsen('math', 'multiply', (a, b) => a * b);
    
    const fujsenMap = tsk.getFujsenMap('math');
    
    if (fujsenMap.add && fujsenMap.subtract && fujsenMap.multiply) {
      const addResult = fujsenMap.add(5, 3);
      const subtractResult = fujsenMap.subtract(10, 4);
      const multiplyResult = fujsenMap.multiply(6, 7);
      
      if (addResult === 8 && subtractResult === 6 && multiplyResult === 42) {
        results.passed++;
        results.tests.push({ name: 'FUJSEN map retrieval', status: 'passed' });
      } else {
        results.failed++;
        results.tests.push({ name: 'FUJSEN map retrieval', status: 'failed', error: 'Map functions not working' });
      }
    } else {
      results.failed++;
      results.tests.push({ name: 'FUJSEN map retrieval', status: 'failed', error: 'FUJSEN map not retrieved' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'FUJSEN map retrieval', status: 'failed', error: error.message });
    results.total++;
  }

  // Test 8: Error handling for invalid FUJSEN
  try {
    const tsk = new TSK();
    tsk.setValue('bad', 'fujsen', 'this is not valid javascript {');
    
    let errorCaught = false;
    try {
      tsk.executeFujsen('bad', 'fujsen');
    } catch (error) {
      errorCaught = true;
    }
    
    if (errorCaught) {
      results.passed++;
      results.tests.push({ name: 'Invalid FUJSEN error handling', status: 'passed' });
    } else {
      results.failed++;
      results.tests.push({ name: 'Invalid FUJSEN error handling', status: 'failed', error: 'Should have thrown error' });
    }
    results.total++;
  } catch (error) {
    results.failed++;
    results.tests.push({ name: 'Invalid FUJSEN error handling', status: 'failed', error: error.message });
    results.total++;
  }

  // Print test results
  console.log(`  FUJSEN Tests: ${results.passed}/${results.total} passed`);
  for (const test of results.tests) {
    const status = test.status === 'passed' ? '✅' : '❌';
    console.log(`    ${status} ${test.name}`);
    if (test.error) {
      console.log(`      Error: ${test.error}`);
    }
  }

  return results;
}

module.exports = runFujsenTests; 
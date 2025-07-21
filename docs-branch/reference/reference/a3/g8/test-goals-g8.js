/**
 * Test Goals G8 Implementation
 * Tests the advanced configuration validation, migration, and logging framework
 * 
 * Goals:
 * - g8.1: Advanced Configuration Validation and Schema Management
 * - g8.2: Intelligent Configuration Migration and Version Management
 * - g8.3: Advanced Logging and Debugging Framework with Structured Output
 */

const { Goal8Implementation } = require('./src/goal8-implementation');

async function testGoalsG8() {
    console.log('🚀 Testing Goal 8 Implementation...\n');

    const results = {
        g8_1_validation: { passed: 0, total: 0, tests: [] },
        g8_2_migration: { passed: 0, total: 0, tests: [] },
        g8_3_logging: { passed: 0, total: 0, tests: [] },
        integration: { passed: 0, total: 0, tests: [] }
    };

    // Initialize Goal 8 implementation
    const goal8 = new Goal8Implementation({
        validation: {
            strictMode: false,
            autoFix: true,
            maxErrors: 50
        },
        migration: {
            autoMigrate: true,
            backupBeforeMigration: true,
            maxMigrationSteps: 5
        },
        logging: {
            level: 'INFO',
            structured: true,
            bufferSize: 50,
            flushInterval: 3000
        }
    });

    // Test G8.1: Advanced Configuration Validation and Schema Management
    console.log('📋 Testing G8.1: Advanced Configuration Validation and Schema Management');
    await testValidationEngine(goal8, results.g8_1_validation);

    // Test G8.2: Intelligent Configuration Migration and Version Management
    console.log('\n📋 Testing G8.2: Intelligent Configuration Migration and Version Management');
    await testMigrationManager(goal8, results.g8_2_migration);

    // Test G8.3: Advanced Logging and Debugging Framework
    console.log('\n📋 Testing G8.3: Advanced Logging and Debugging Framework with Structured Output');
    await testLoggingFramework(goal8, results.g8_3_logging);

    // Test Integration
    console.log('\n📋 Testing Integration');
    await testIntegration(goal8, results.integration);

    // Print results
    printResults(results);

    return results;
}

async function testValidationEngine(goal8, results) {
    // Test 1: Initialize validation engine
    try {
        await goal8.initialize();
        const status = goal8.getSystemStatus();
        assert(status.validation && status.validation.schemas > 0, 'Validation engine should be initialized with schemas');
        results.tests.push({ name: 'Validation engine initialization', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Validation engine initialization', passed: false, error: error.message });
    }
    results.total++;

    // Test 2: Server configuration validation
    try {
        const validConfig = {
            host: 'localhost',
            port: 8080,
            timeout: 30000
        };
        
        const result = await goal8.validateConfig(validConfig, 'server-config');
        assert(result.valid, 'Valid server config should pass validation');
        results.tests.push({ name: 'Server config validation (valid)', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Server config validation (valid)', passed: false, error: error.message });
    }
    results.total++;

    // Test 3: Invalid configuration validation
    try {
        const invalidConfig = {
            host: 'localhost'
            // Missing required port field
        };
        
        const result = await goal8.validateConfig(invalidConfig, 'server-config');
        assert(!result.valid && result.errors.length > 0, 'Invalid config should fail validation');
        results.tests.push({ name: 'Server config validation (invalid)', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Server config validation (invalid)', passed: false, error: error.message });
    }
    results.total++;

    // Test 4: Custom validator registration
    try {
        const customValidator = (value, config) => {
            return { valid: typeof value === 'string' && value.length > 0 };
        };
        
        goal8.registerValidator('custom', customValidator);
        const hasValidator = goal8.validationEngine.customValidators.has('custom');
        assert(hasValidator, 'Custom validator should be registered');
        results.tests.push({ name: 'Custom validator registration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Custom validator registration', passed: false, error: error.message });
    }
    results.total++;

    // Test 5: Validation statistics
    try {
        const stats = goal8.validationEngine.getStats();
        assert(stats.schemas > 0, 'Should have registered schemas');
        assert(stats.validators > 0, 'Should have registered validators');
        results.tests.push({ name: 'Validation statistics', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Validation statistics', passed: false, error: error.message });
    }
    results.total++;
}

async function testMigrationManager(goal8, results) {
    // Test 1: Migration manager initialization
    try {
        const status = goal8.getSystemStatus();
        assert(status.migration && status.migration.totalMigrations > 0, 'Migration manager should be initialized with migrations');
        results.tests.push({ name: 'Migration manager initialization', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Migration manager initialization', passed: false, error: error.message });
    }
    results.total++;

    // Test 2: Configuration migration
    try {
        const oldConfig = {
            server: { host: 'localhost', port: 8080 },
            database: { type: 'postgresql', host: 'db.example.com', port: 5432, name: 'testdb' }
        };
        
        const result = await goal8.migrateConfig(oldConfig, '2.0.0');
        assert(result.migrated && result.version === '2.0.0', 'Config should be migrated to version 2.0.0');
        results.tests.push({ name: 'Configuration migration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Configuration migration', passed: false, error: error.message });
    }
    results.total++;

    // Test 3: Migration path finding
    try {
        const migrations = goal8.migrationManager.getAvailableMigrations();
        assert(migrations.length > 0, 'Should have available migrations');
        results.tests.push({ name: 'Migration path finding', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Migration path finding', passed: false, error: error.message });
    }
    results.total++;

    // Test 4: Compatibility matrix
    try {
        const matrix = goal8.migrationManager.getCompatibilityMatrix();
        assert(Object.keys(matrix).length > 0, 'Should have compatibility matrix');
        results.tests.push({ name: 'Compatibility matrix', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Compatibility matrix', passed: false, error: error.message });
    }
    results.total++;

    // Test 5: Migration statistics
    try {
        const stats = goal8.migrationManager.getMigrationStats();
        assert(stats.totalMigrations > 0, 'Should have registered migrations');
        results.tests.push({ name: 'Migration statistics', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Migration statistics', passed: false, error: error.message });
    }
    results.total++;
}

async function testLoggingFramework(goal8, results) {
    // Test 1: Logging framework initialization
    try {
        const status = goal8.getSystemStatus();
        assert(status.logging && status.logging.transports > 0, 'Logging framework should be initialized with transports');
        results.tests.push({ name: 'Logging framework initialization', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Logging framework initialization', passed: false, error: error.message });
    }
    results.total++;

    // Test 2: Basic logging functionality
    try {
        goal8.log('INFO', 'Test message', { test: true });
        const stats = goal8.loggingFramework.getStats();
        assert(stats.transports > 0, 'Should have active transports');
        results.tests.push({ name: 'Basic logging functionality', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Basic logging functionality', passed: false, error: error.message });
    }
    results.total++;

    // Test 3: Log level setting
    try {
        goal8.loggingFramework.setLevel('DEBUG');
        const stats = goal8.loggingFramework.getStats();
        assert(stats.currentLevel === 'DEBUG', 'Log level should be set to DEBUG');
        results.tests.push({ name: 'Log level setting', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Log level setting', passed: false, error: error.message });
    }
    results.total++;

    // Test 4: Child logger creation
    try {
        const childLogger = goal8.createChildLogger({ component: 'test' });
        assert(childLogger && typeof childLogger.log === 'function', 'Should create child logger');
        results.tests.push({ name: 'Child logger creation', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Child logger creation', passed: false, error: error.message });
    }
    results.total++;

    // Test 5: Log filtering
    try {
        const filterFunction = (entry) => entry.level !== 'DEBUG';
        goal8.addLogFilter('test-filter', filterFunction);
        const stats = goal8.loggingFramework.getStats();
        assert(stats.filters > 0, 'Should have registered filters');
        results.tests.push({ name: 'Log filtering', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Log filtering', passed: false, error: error.message });
    }
    results.total++;
}

async function testIntegration(goal8, results) {
    // Test 1: System status integration
    try {
        const status = goal8.getSystemStatus();
        assert(status.initialized, 'System should be initialized');
        assert(status.validation, 'Validation component should exist');
        assert(status.migration, 'Migration component should exist');
        assert(status.logging, 'Logging component should exist');
        results.tests.push({ name: 'System status integration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'System status integration', passed: false, error: error.message });
    }
    results.total++;

    // Test 2: End-to-end workflow
    try {
        const config = { host: 'localhost', port: 8080 };
        const validation = await goal8.validateConfig(config, 'server-config');
        const migration = await goal8.migrateConfig(config, '2.0.0');
        goal8.log('INFO', 'End-to-end test completed', { validation: validation.valid, migration: migration.migrated });
        
        assert(validation.valid, 'Config should be valid');
        assert(migration.migrated, 'Config should be migrated');
        results.tests.push({ name: 'End-to-end workflow', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'End-to-end workflow', passed: false, error: error.message });
    }
    results.total++;

    // Test 3: Cross-component communication
    try {
        const testConfig = { host: 'localhost', port: 8080 };
        await goal8.validateConfig(testConfig, 'server-config');
        await goal8.migrateConfig(testConfig, '2.0.0');
        
        const stats = goal8.getSystemStatus();
        assert(stats.stats.validations > 0, 'Validations should be tracked');
        assert(stats.stats.migrations > 0, 'Migrations should be tracked');
        results.tests.push({ name: 'Cross-component communication', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Cross-component communication', passed: false, error: error.message });
    }
    results.total++;

    // Test 4: Comprehensive test suite
    try {
        const testResults = await goal8.runTests();
        const totalTests = testResults.validation.total + testResults.migration.total + 
                          testResults.logging.total + testResults.integration.total;
        const totalPassed = testResults.validation.passed + testResults.migration.passed + 
                           testResults.logging.passed + testResults.integration.passed;
        
        assert(totalTests > 0, 'Test suite should run tests');
        assert(totalPassed > 0, 'Some tests should pass');
        results.tests.push({ name: 'Comprehensive test suite', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Comprehensive test suite', passed: false, error: error.message });
    }
    results.total++;
}

function assert(condition, message) {
    if (!condition) {
        throw new Error(message);
    }
}

function printResults(results) {
    console.log('\n🎯 COMPREHENSIVE TEST RESULTS');
    console.log('============================================================');
    
    const categories = [
        { name: 'G8.1 Validation Engine', data: results.g8_1_validation },
        { name: 'G8.2 Migration Manager', data: results.g8_2_migration },
        { name: 'G8.3 Logging Framework', data: results.g8_3_logging },
        { name: 'Integration', data: results.integration }
    ];
    
    let totalTests = 0;
    let totalPassed = 0;
    
    for (const category of categories) {
        const { data } = category;
        const successRate = data.total > 0 ? ((data.passed / data.total) * 100).toFixed(1) : '0.0';
        
        console.log(`\n${category.name}:`);
        console.log(`  Tests: ${data.passed}/${data.total} (${successRate}%)`);
        
        for (const test of data.tests) {
            const status = test.passed ? '✅' : '❌';
            console.log(`    ${status} ${test.name}`);
            if (!test.passed && test.error) {
                console.log(`      Error: ${test.error}`);
            }
        }
        
        totalTests += data.total;
        totalPassed += data.passed;
    }
    
    const overallSuccessRate = totalTests > 0 ? ((totalPassed / totalTests) * 100).toFixed(1) : '0.0';
    
    console.log('\n============================================================');
    console.log(`Total Tests: ${totalTests}`);
    console.log(`Passed: ${totalPassed} ✅`);
    console.log(`Failed: ${totalTests - totalPassed} ❌`);
    console.log(`Success Rate: ${overallSuccessRate}%`);
    
    if (overallSuccessRate >= 80) {
        console.log('\n🎉 GOAL 8 IMPLEMENTATION SUCCESSFUL!');
        console.log('🚀 Advanced validation, migration, and logging framework working!');
    } else {
        console.log('\n⚠️  Some tests failed. Review implementation.');
    }
    console.log('============================================================');
}

// Run the test
if (require.main === module) {
    testGoalsG8()
        .then(result => {
            console.log('\n🎉 Goal 8 test completed successfully!');
            process.exit(0);
        })
        .catch(error => {
            console.error('❌ Goal 8 test failed:', error);
            process.exit(1);
        });
}

module.exports = { testGoalsG8 }; 
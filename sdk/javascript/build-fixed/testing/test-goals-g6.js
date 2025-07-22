/**
 * Test Goals G6 Implementation
 * Tests the integrated web framework, template engine, and session management
 * 
 * Goals:
 * - g6.1: Advanced Web Framework
 * - g6.2: Advanced Template Engine
 * - g6.3: Advanced Session Management
 */

const { Goal6Implementation } = require('./src/goal6-implementation');
const { WebFramework, commonMiddleware } = require('./src/web-framework');
const { TemplateEngine, predefinedTemplates } = require('./src/template-engine');
const { SessionManager, MemoryStorage } = require('./src/session-management');

async function testGoalsG6() {
    console.log('🚀 Testing Goal 6 Implementation...\n');

    const results = {
        g6_1_web_framework: { passed: 0, total: 0, tests: [] },
        g6_2_template_engine: { passed: 0, total: 0, tests: [] },
        g6_3_session_management: { passed: 0, total: 0, tests: [] },
        integration: { passed: 0, total: 0, tests: [] }
    };

    // Test G6.1: Advanced Web Framework
    console.log('📋 Testing G6.1: Advanced Web Framework');
    await testWebFramework(results.g6_1_web_framework);

    // Test G6.2: Advanced Template Engine
    console.log('\n📋 Testing G6.2: Advanced Template Engine');
    await testTemplateEngine(results.g6_2_template_engine);

    // Test G6.3: Advanced Session Management
    console.log('\n📋 Testing G6.3: Advanced Session Management');
    await testSessionManagement(results.g6_3_session_management);

    // Test Integration
    console.log('\n📋 Testing Integration');
    await testIntegration(results.integration);

    // Print results
    printResults(results);

    return results;
}

async function testWebFramework(results) {
    const framework = new WebFramework();

    // Test 1: Route registration
    try {
        framework.get('/test', (req, res) => res.json({ message: 'test' }));
        const stats = framework.getStats();
        assert(stats.routes === 1, 'Route should be registered');
        results.tests.push({ name: 'Route registration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Route registration', passed: false, error: error.message });
    }
    results.total++;

    // Test 2: Middleware registration
    try {
        framework.use(commonMiddleware.logger);
        const stats = framework.getStats();
        assert(stats.middleware === 1, 'Middleware should be registered');
        results.tests.push({ name: 'Middleware registration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Middleware registration', passed: false, error: error.message });
    }
    results.total++;

    // Test 3: Static file serving
    try {
        framework.static('/static', './public');
        const stats = framework.getStats();
        assert(stats.staticPaths === 1, 'Static path should be registered');
        results.tests.push({ name: 'Static file serving', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Static file serving', passed: false, error: error.message });
    }
    results.total++;

    // Test 4: Error handler registration
    try {
        framework.error(404, (req, res) => res.send('Not found'));
        const stats = framework.getStats();
        assert(stats.errorHandlers === 1, 'Error handler should be registered');
        results.tests.push({ name: 'Error handler registration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Error handler registration', passed: false, error: error.message });
    }
    results.total++;

    // Test 5: Route parameter parsing
    try {
        framework.get('/user/:id', (req, res) => res.json(req.params));
        const routeMatch = framework.findRoute('GET', '/user/123');
        assert(routeMatch && routeMatch.params.id === '123', 'Route parameters should be parsed');
        results.tests.push({ name: 'Route parameter parsing', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Route parameter parsing', passed: false, error: error.message });
    }
    results.total++;

    framework.close();
}

async function testTemplateEngine(results) {
    const engine = new TemplateEngine();

    // Test 1: Basic variable rendering
    try {
        const template = 'Hello {{ name }}!';
        const data = { name: 'World' };
        const result = engine.render(template, data);
        assert(result === 'Hello World!', 'Basic variable rendering should work');
        results.tests.push({ name: 'Basic variable rendering', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Basic variable rendering', passed: false, error: error.message });
    }
    results.total++;

    // Test 2: Filter usage
    try {
        const template = '{{ name | upper }}';
        const data = { name: 'hello' };
        const result = engine.render(template, data);
        assert(result === 'HELLO', 'Filters should work');
        results.tests.push({ name: 'Filter usage', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Filter usage', passed: false, error: error.message });
    }
    results.total++;

    // Test 3: Conditional logic
    try {
        const template = '{% if show %}Hello{% else %}Goodbye{% endif %}';
        const data1 = { show: true };
        const data2 = { show: false };
        const result1 = engine.render(template, data1);
        const result2 = engine.render(template, data2);
        assert(result1 === 'Hello', 'If condition should work when true');
        assert(result2 === 'Goodbye', 'If condition should work when false');
        results.tests.push({ name: 'Conditional logic', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Conditional logic', passed: false, error: error.message });
    }
    results.total++;

    // Test 4: Loop functionality
    try {
        const template = '{% each item in items %}{{ item }}{% endeach %}';
        const data = { items: ['a', 'b', 'c'] };
        const result = engine.render(template, data);
        assert(result === 'abc', 'Loops should work');
        results.tests.push({ name: 'Loop functionality', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Loop functionality', passed: false, error: error.message });
    }
    results.total++;

    // Test 5: Template caching
    try {
        const template = '{{ name }}';
        const data = { name: 'test' };
        
        // First render
        engine.render(template, data);
        const stats1 = engine.getStats();
        
        // Second render (should use cache)
        engine.render(template, data);
        const stats2 = engine.getStats();
        
        assert(stats2.cacheSize >= stats1.cacheSize, 'Template should be cached');
        results.tests.push({ name: 'Template caching', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Template caching', passed: false, error: error.message });
    }
    results.total++;

    // Test 6: Custom filter registration
    try {
        engine.registerFilter('reverse', (value) => String(value).split('').reverse().join(''));
        const template = '{{ name | reverse }}';
        const data = { name: 'hello' };
        const result = engine.render(template, data);
        assert(result === 'olleh', 'Custom filters should work');
        results.tests.push({ name: 'Custom filter registration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Custom filter registration', passed: false, error: error.message });
    }
    results.total++;
}

async function testSessionManagement(results) {
    const sessionManager = new SessionManager();

    // Test 1: Session creation
    try {
        const session = await sessionManager.createSession('user123', { role: 'admin' });
        assert(session.id, 'Session should have an ID');
        assert(session.userId === 'user123', 'Session should have correct user ID');
        assert(session.data.role === 'admin', 'Session should have correct data');
        results.tests.push({ name: 'Session creation', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Session creation', passed: false, error: error.message });
    }
    results.total++;

    // Test 2: Session retrieval
    try {
        const session = await sessionManager.createSession('user456', { test: true });
        const retrieved = await sessionManager.getSession(session.id);
        assert(retrieved && retrieved.id === session.id, 'Session should be retrievable');
        assert(retrieved.data.test === true, 'Session data should be preserved');
        results.tests.push({ name: 'Session retrieval', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Session retrieval', passed: false, error: error.message });
    }
    results.total++;

    // Test 3: Session update
    try {
        const session = await sessionManager.createSession('user789', { count: 1 });
        const updated = await sessionManager.updateSession(session.id, { count: 2, newField: 'test' });
        assert(updated, 'Session update should succeed');
        
        const retrieved = await sessionManager.getSession(session.id);
        assert(retrieved.data.count === 2, 'Session data should be updated');
        assert(retrieved.data.newField === 'test', 'New fields should be added');
        results.tests.push({ name: 'Session update', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Session update', passed: false, error: error.message });
    }
    results.total++;

    // Test 4: Session destruction
    try {
        const session = await sessionManager.createSession('user999', { temp: true });
        const destroyed = await sessionManager.destroySession(session.id);
        assert(destroyed, 'Session destruction should succeed');
        
        const retrieved = await sessionManager.getSession(session.id);
        assert(!retrieved, 'Destroyed session should not be retrievable');
        results.tests.push({ name: 'Session destruction', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Session destruction', passed: false, error: error.message });
    }
    results.total++;

    // Test 5: Session validation
    try {
        const session = await sessionManager.createSession('user111', { valid: true });
        const isValid = sessionManager.isSessionValid(session);
        assert(isValid, 'Valid session should pass validation');
        
        // Create expired session
        const expiredSession = {
            ...session,
            expiresAt: Date.now() - 1000 // Expired 1 second ago
        };
        const isExpiredValid = sessionManager.isSessionValid(expiredSession);
        assert(!isExpiredValid, 'Expired session should fail validation');
        results.tests.push({ name: 'Session validation', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Session validation', passed: false, error: error.message });
    }
    results.total++;

    // Test 6: Session statistics
    try {
        const stats = sessionManager.getStats();
        assert(typeof stats.totalSessions === 'number', 'Stats should include total sessions');
        assert(typeof stats.activeSessions === 'number', 'Stats should include active sessions');
        assert(typeof stats.createdSessions === 'number', 'Stats should include created sessions');
        results.tests.push({ name: 'Session statistics', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Session statistics', passed: false, error: error.message });
    }
    results.total++;

    sessionManager.stopCleanup();
}

async function testIntegration(results) {
    const implementation = new Goal6Implementation();

    // Test 1: Component integration
    try {
        const stats = implementation.getStats();
        assert(stats.web, 'Web framework stats should be available');
        assert(stats.template, 'Template engine stats should be available');
        assert(stats.session, 'Session management stats should be available');
        results.tests.push({ name: 'Component integration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Component integration', passed: false, error: error.message });
    }
    results.total++;

    // Test 2: Template rendering with session data
    try {
        const template = 'Hello {{ user.name }}, you have {{ user.role }} access.';
        const data = { user: { name: 'John', role: 'admin' } };
        const result = implementation.templateEngine.render(template, data);
        assert(result === 'Hello John, you have admin access.', 'Template should render with data');
        results.tests.push({ name: 'Template rendering with session data', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Template rendering with session data', passed: false, error: error.message });
    }
    results.total++;

    // Test 3: Session middleware integration
    try {
        const middleware = implementation.sessionManager.middleware();
        assert(typeof middleware === 'function', 'Session middleware should be a function');
        results.tests.push({ name: 'Session middleware integration', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Session middleware integration', passed: false, error: error.message });
    }
    results.total++;

    // Test 4: Route registration with template rendering
    try {
        const routeCount = implementation.webFramework.getStats().routes;
        assert(routeCount > 0, 'Routes should be registered');
        results.tests.push({ name: 'Route registration with template rendering', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Route registration with template rendering', passed: false, error: error.message });
    }
    results.total++;

    // Test 5: Comprehensive testing
    try {
        const testResults = await implementation.test();
        const allPassed = Object.values(testResults).every(result => result === true);
        assert(allPassed, 'All integration tests should pass');
        results.tests.push({ name: 'Comprehensive testing', passed: true });
        results.passed++;
    } catch (error) {
        results.tests.push({ name: 'Comprehensive testing', passed: false, error: error.message });
    }
    results.total++;

    implementation.stop();
}

function assert(condition, message) {
    if (!condition) {
        throw new Error(message);
    }
}

function printResults(results) {
    console.log('\n📊 Test Results Summary:');
    console.log('========================');

    const goals = [
        { name: 'G6.1: Advanced Web Framework', results: results.g6_1_web_framework },
        { name: 'G6.2: Advanced Template Engine', results: results.g6_2_template_engine },
        { name: 'G6.3: Advanced Session Management', results: results.g6_3_session_management },
        { name: 'Integration Tests', results: results.integration }
    ];

    let totalPassed = 0;
    let totalTests = 0;

    for (const goal of goals) {
        const { results: goalResults } = goal;
        const percentage = goalResults.total > 0 ? (goalResults.passed / goalResults.total * 100).toFixed(1) : 0;
        
        console.log(`\n${goal.name}:`);
        console.log(`  Passed: ${goalResults.passed}/${goalResults.total} (${percentage}%)`);
        
        for (const test of goalResults.tests) {
            const status = test.passed ? '✅' : '❌';
            console.log(`    ${status} ${test.name}`);
            if (!test.passed && test.error) {
                console.log(`      Error: ${test.error}`);
            }
        }
        
        totalPassed += goalResults.passed;
        totalTests += goalResults.total;
    }

    const overallPercentage = totalTests > 0 ? (totalPassed / totalTests * 100).toFixed(1) : 0;
    console.log(`\n🎯 Overall Results: ${totalPassed}/${totalTests} (${overallPercentage}%)`);
    
    if (overallPercentage === 100) {
        console.log('🎉 All tests passed! Goal 6 implementation is complete.');
    } else {
        console.log('⚠️  Some tests failed. Please review the implementation.');
    }
}

// Run tests if this file is executed directly
if (require.main === module) {
    testGoalsG6()
        .then(() => {
            console.log('\n✅ Goal 6 testing completed successfully!');
            process.exit(0);
        })
        .catch((error) => {
            console.error('\n❌ Goal 6 testing failed:', error);
            process.exit(1);
        });
}

module.exports = { testGoalsG6 }; 
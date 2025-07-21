/**
 * Test Suite for JavaScript Agent A3 Goal 5 Implementation
 * Validates all three goals: Advanced Data Processing, Workflow Orchestration, and Performance Monitoring
 */

const { Goal5Implementation, AdvancedDataProcessor, WorkflowOrchestrator, PerformanceMonitor } = require('./goal5-implementation');

class Goal5TestSuite {
    constructor() {
        this.tests = [];
        this.results = {
            passed: 0,
            failed: 0,
            total: 0
        };
    }

    // Add test to suite
    addTest(name, testFunction) {
        this.tests.push({ name, testFunction });
    }

    // Run all tests
    async runTests() {
        console.log('🚀 Starting Goal 5 Test Suite...\n');
        
        for (const test of this.tests) {
            try {
                console.log(`📋 Running: ${test.name}`);
                await test.testFunction();
                console.log(`✅ PASSED: ${test.name}\n`);
                this.results.passed++;
            } catch (error) {
                console.log(`❌ FAILED: ${test.name}`);
                console.log(`   Error: ${error.message}\n`);
                this.results.failed++;
            }
            this.results.total++;
        }

        this.printResults();
    }

    // Print test results
    printResults() {
        console.log('📊 Test Results Summary:');
        console.log(`   Total Tests: ${this.results.total}`);
        console.log(`   Passed: ${this.results.passed}`);
        console.log(`   Failed: ${this.results.failed}`);
        console.log(`   Success Rate: ${((this.results.passed / this.results.total) * 100).toFixed(2)}%`);
    }
}

// Create test suite
const testSuite = new Goal5TestSuite();

// Test 1: Goal 5.1 - Advanced Data Processing
testSuite.addTest('Goal 5.1 - Data Processing Basic Functionality', async () => {
    const processor = new AdvancedDataProcessor();
    
    // Register built-in processors
    const builtInProcessors = AdvancedDataProcessor.getBuiltInProcessors();
    Object.entries(builtInProcessors).forEach(([name, processorFunc]) => {
        processor.registerProcessor(name, processorFunc);
    });

    // Test data processing
    const testData = { Name: 'Test', Value: 42, Category: 'test' };
    const result = await processor.processData(testData, ['validate', 'transform', 'enrich']);

    // Validate results
    if (!result.name || result.name !== 'Test') throw new Error('Data transformation failed');
    if (!result.processedAt) throw new Error('Data enrichment failed');
    if (!result.version) throw new Error('Version not added');

    // Test analytics
    const analytics = processor.getAnalytics();
    if (analytics.totalProcessed !== 1) throw new Error('Analytics not tracking correctly');
    if (analytics.successRate !== 100) throw new Error('Success rate calculation error');
});

// Test 2: Goal 5.1 - Data Processing Error Handling
testSuite.addTest('Goal 5.1 - Data Processing Error Handling', async () => {
    const processor = new AdvancedDataProcessor();
    
    // Register built-in processors
    const builtInProcessors = AdvancedDataProcessor.getBuiltInProcessors();
    Object.entries(builtInProcessors).forEach(([name, processorFunc]) => {
        processor.registerProcessor(name, processorFunc);
    });

    // Test invalid data
    try {
        await processor.processData(null, ['validate']);
        throw new Error('Should have thrown error for null data');
    } catch (error) {
        if (!error.message.includes('Invalid data format')) {
            throw new Error('Wrong error message for invalid data');
        }
    }

    // Test non-existent processor
    try {
        await processor.processData({ test: 'data' }, ['nonexistent']);
        throw new Error('Should have thrown error for non-existent processor');
    } catch (error) {
        if (!error.message.includes('not found')) {
            throw new Error('Wrong error message for non-existent processor');
        }
    }

    // Check error analytics
    const analytics = processor.getAnalytics();
    if (analytics.totalErrors !== 2) throw new Error('Error tracking not working');
});

// Test 3: Goal 5.2 - Workflow Orchestration Basic
testSuite.addTest('Goal 5.2 - Workflow Orchestration Basic', async () => {
    const orchestrator = new WorkflowOrchestrator();
    
    // Define a simple workflow
    orchestrator.defineWorkflow('testWorkflow', [
        async (context) => {
            return { step1: 'completed', data: 'test' };
        },
        async (context) => {
            return { step2: 'completed', previousData: context.step_0_result.data };
        }
    ]);

    // Execute workflow
    const execution = await orchestrator.executeWorkflow('testWorkflow');

    // Validate execution
    if (execution.status !== 'completed') throw new Error('Workflow execution failed');
    if (execution.steps.length !== 2) throw new Error('Wrong number of steps executed');
    if (execution.steps[1].result.previousData !== 'test') throw new Error('Context passing failed');

    // Test workflow status
    const status = orchestrator.getWorkflowStatus('testWorkflow');
    if (status.totalExecutions !== 1) throw new Error('Execution tracking failed');
});

// Test 4: Goal 5.2 - Workflow Error Handling
testSuite.addTest('Goal 5.2 - Workflow Error Handling', async () => {
    const orchestrator = new WorkflowOrchestrator();
    
    // Define workflow with error
    orchestrator.defineWorkflow('errorWorkflow', [
        async (context) => {
            return { step1: 'ok' };
        },
        async (context) => {
            throw new Error('Intentional error');
        }
    ]);

    // Execute workflow and expect error
    try {
        await orchestrator.executeWorkflow('errorWorkflow');
        throw new Error('Should have thrown error');
    } catch (error) {
        if (!error.message.includes('Intentional error')) {
            throw new Error('Wrong error message');
        }
    }

    // Check workflow status
    const status = orchestrator.getWorkflowStatus('errorWorkflow');
    if (status.lastExecution.status !== 'failed') throw new Error('Failed status not recorded');
});

// Test 5: Goal 5.3 - Performance Monitoring Basic
testSuite.addTest('Goal 5.3 - Performance Monitoring Basic', async () => {
    const monitor = new PerformanceMonitor();
    
    // Track metrics
    monitor.trackMetric('testMetric', 100);
    monitor.trackMetric('testMetric', 200);
    monitor.trackMetric('testMetric', 150);

    // Get statistics
    const stats = monitor.getStatistics('testMetric');
    if (!stats) throw new Error('Statistics not generated');
    if (stats.count !== 3) throw new Error('Wrong metric count');
    if (stats.average !== 150) throw new Error('Wrong average calculation');
    if (stats.min !== 100) throw new Error('Wrong min value');
    if (stats.max !== 200) throw new Error('Wrong max value');
});

// Test 6: Goal 5.3 - Performance Thresholds and Alerts
testSuite.addTest('Goal 5.3 - Performance Thresholds and Alerts', async () => {
    const monitor = new PerformanceMonitor();
    
    // Set threshold
    monitor.setThreshold('testMetric', 100, 'gt');

    // Track metrics below and above threshold
    monitor.trackMetric('testMetric', 50);  // Should not alert
    monitor.trackMetric('testMetric', 150); // Should alert

    // Check alerts
    const alerts = monitor.getAlerts();
    if (alerts.length !== 1) throw new Error('Wrong number of alerts');
    if (alerts[0].value !== 150) throw new Error('Wrong alert value');
    if (alerts[0].operator !== 'gt') throw new Error('Wrong alert operator');
});

// Test 7: Goal 5.3 - Performance Optimizations
testSuite.addTest('Goal 5.3 - Performance Optimizations', async () => {
    const monitor = new PerformanceMonitor();
    
    // Register optimization
    monitor.registerOptimization('testOptimization', async (context) => {
        return { optimized: true, context };
    });

    // Apply optimizations
    const results = await monitor.applyOptimizations({ test: 'data' });
    if (results.length !== 1) throw new Error('Wrong number of optimization results');
    if (!results[0].success) throw new Error('Optimization failed');
    if (!results[0].result.optimized) throw new Error('Optimization result incorrect');
});

// Test 8: Complete Goal 5 Implementation Integration
testSuite.addTest('Goal 5 - Complete Integration Test', async () => {
    const goal5 = new Goal5Implementation();
    
    // Execute all goals
    const results = await goal5.executeAllGoals();

    // Validate results structure
    if (!results.goal51 || !results.goal52 || !results.goal53) {
        throw new Error('Missing goal results');
    }

    // Validate Goal 5.1 results
    if (!results.goal51.success) throw new Error('Goal 5.1 failed');
    if (!results.goal51.result) throw new Error('Goal 5.1 missing result');
    if (!results.goal51.analytics) throw new Error('Goal 5.1 missing analytics');

    // Validate Goal 5.2 results
    if (!results.goal52.success) throw new Error('Goal 5.2 failed');
    if (!results.goal52.execution) throw new Error('Goal 5.2 missing execution');
    if (results.goal52.execution.status !== 'completed') throw new Error('Goal 5.2 execution failed');

    // Validate Goal 5.3 results
    if (!results.goal53.success) throw new Error('Goal 5.3 failed');
    if (!results.goal53.statistics) throw new Error('Goal 5.3 missing statistics');
    if (!results.goal53.optimizationResults) throw new Error('Goal 5.3 missing optimization results');

    // Validate summary
    if (results.summary.totalGoals !== 3) throw new Error('Wrong total goals count');
    if (results.summary.completedGoals !== 3) throw new Error('Wrong completed goals count');
    if (results.summary.successRate !== 100) throw new Error('Wrong success rate');
});

// Test 9: System Status and Health Check
testSuite.addTest('Goal 5 - System Status Check', async () => {
    const goal5 = new Goal5Implementation();
    
    // Get system status
    const status = goal5.getSystemStatus();

    // Validate data processor status
    if (status.dataProcessor.registeredProcessors < 3) throw new Error('Not enough registered processors');
    if (!status.dataProcessor.analytics) throw new Error('Missing data processor analytics');

    // Validate workflow orchestrator status
    if (typeof status.workflowOrchestrator.definedWorkflows !== 'number') throw new Error('Invalid workflow count');
    if (typeof status.workflowOrchestrator.activeExecutions !== 'number') throw new Error('Invalid execution count');

    // Validate performance monitor status
    if (typeof status.performanceMonitor.trackedMetrics !== 'number') throw new Error('Invalid metrics count');
    if (typeof status.performanceMonitor.activeAlerts !== 'number') throw new Error('Invalid alerts count');
    if (status.performanceMonitor.optimizationStrategies < 2) throw new Error('Not enough optimization strategies');
});

// Test 10: Edge Cases and Stress Testing
testSuite.addTest('Goal 5 - Edge Cases and Stress Testing', async () => {
    const goal5 = new Goal5Implementation();
    
    // Test with large dataset
    const largeData = Array.from({ length: 1000 }, (_, i) => ({ id: i, value: `data_${i}` }));
    
    // Process large dataset
    for (let i = 0; i < 10; i++) {
        await goal5.dataProcessor.processData(largeData[i], ['validate', 'transform']);
    }

    // Check analytics
    const analytics = goal5.dataProcessor.getAnalytics();
    if (analytics.totalProcessed < 10) throw new Error('Large dataset processing failed');

    // Test performance under load
    for (let i = 0; i < 100; i++) {
        goal5.performanceMonitor.trackMetric('stressTest', Math.random() * 1000);
    }

    const stressStats = goal5.performanceMonitor.getStatistics('stressTest');
    if (!stressStats || stressStats.count < 100) throw new Error('Stress test failed');
});

// Run the test suite
if (require.main === module) {
    (async () => {
        try {
            await testSuite.runTests();
            
            // Exit with appropriate code
            if (testSuite.results.failed > 0) {
                process.exit(1);
            } else {
                console.log('\n🎉 All tests passed! Goal 5 implementation is working correctly.');
                process.exit(0);
            }
        } catch (error) {
            console.error('Test suite error:', error.message);
            process.exit(1);
        }
    })();
}

module.exports = { Goal5TestSuite }; 
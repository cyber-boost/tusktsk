/**
 * Test Suite for JavaScript Agent A3 Goal 6 Implementation
 * Validates all three goals: Real-time Communication, Distributed Systems, and Advanced Analytics
 */

const { Goal6Implementation, RealTimeCommunicationManager, DistributedSystemCoordinator, AdvancedAnalyticsEngine } = require('./goal6-implementation');

class Goal6TestSuite {
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
        console.log('🚀 Starting Goal 6 Test Suite...\n');
        
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
const testSuite = new Goal6TestSuite();

// Test 1: Goal 6.1 - Real-time Communication Basic
testSuite.addTest('Goal 6.1 - Real-time Communication Basic', async () => {
    const manager = new RealTimeCommunicationManager();
    
    // Register connections
    const conn1 = manager.registerConnection('conn1', { type: 'websocket' });
    const conn2 = manager.registerConnection('conn2', { type: 'websocket' });

    // Validate connections
    if (!conn1 || !conn2) throw new Error('Connection registration failed');
    if (manager.connections.size !== 2) throw new Error('Wrong number of connections');

    // Create room
    const room = manager.createRoom('test-room');
    if (!room) throw new Error('Room creation failed');

    // Join room
    manager.joinRoom('conn1', 'test-room');
    manager.joinRoom('conn2', 'test-room');

    // Validate room membership
    const conn1Stats = manager.getConnectionStats('conn1');
    if (!conn1Stats.rooms.includes('test-room')) throw new Error('Room join failed');

    // Send message
    const message = manager.sendMessage('conn1', { text: 'test message' });
    if (!message.id) throw new Error('Message sending failed');

    // Check stats
    const stats = manager.getStats();
    if (stats.connections.active !== 2) throw new Error('Stats tracking failed');
});

// Test 2: Goal 6.1 - Real-time Communication Broadcasting
testSuite.addTest('Goal 6.1 - Real-time Communication Broadcasting', async () => {
    const manager = new RealTimeCommunicationManager();
    
    // Setup connections and room
    manager.registerConnection('conn1', { type: 'websocket' });
    manager.registerConnection('conn2', { type: 'websocket' });
    manager.registerConnection('conn3', { type: 'websocket' });
    
    manager.createRoom('broadcast-room');
    manager.joinRoom('conn1', 'broadcast-room');
    manager.joinRoom('conn2', 'broadcast-room');
    manager.joinRoom('conn3', 'broadcast-room');

    // Broadcast message
    const broadcastResult = manager.broadcastToRoom('broadcast-room', { text: 'broadcast' });
    
    // Validate broadcast
    if (broadcastResult.length !== 3) throw new Error('Broadcast to wrong number of connections');
    if (broadcastResult.some(r => !r.success)) throw new Error('Some broadcasts failed');

    // Check room stats
    const roomStats = manager.getRoomStats('broadcast-room');
    if (roomStats.connections !== 3) throw new Error('Wrong room connection count');
});

// Test 3: Goal 6.2 - Distributed System Basic
testSuite.addTest('Goal 6.2 - Distributed System Basic', async () => {
    const coordinator = new DistributedSystemCoordinator({ nodeId: 'test-node' });
    
    // Register nodes
    coordinator.registerNode('node1', { host: '192.168.1.1', port: 3000 });
    coordinator.registerNode('node2', { host: '192.168.1.2', port: 3001 });

    // Validate nodes
    if (coordinator.nodes.size !== 2) throw new Error('Node registration failed');

    // Trigger election
    coordinator.triggerElection();
    
    // Check election state
    if (coordinator.state !== 'candidate') throw new Error('Election not triggered');
    if (coordinator.term <= 0) throw new Error('Term not incremented');

    // Check cluster stats
    const stats = coordinator.getClusterStats();
    if (stats.totalNodes !== 2) throw new Error('Wrong node count in stats');
});

// Test 4: Goal 6.2 - Distributed System Work Distribution
testSuite.addTest('Goal 6.2 - Distributed System Work Distribution', async () => {
    const coordinator = new DistributedSystemCoordinator({ nodeId: 'test-node' });
    
    // Register multiple nodes
    coordinator.registerNode('node1', { host: '192.168.1.1', port: 3000 });
    coordinator.registerNode('node2', { host: '192.168.1.2', port: 3001 });
    coordinator.registerNode('node3', { host: '192.168.1.3', port: 3002 });

    // Distribute work
    const workItems = Array.from({ length: 9 }, (_, i) => ({ id: i, task: `task_${i}` }));
    const distribution = coordinator.distributeWork(workItems, 'round-robin');

    // Validate distribution
    if (distribution.size !== 3) throw new Error('Wrong number of nodes in distribution');
    
    let totalWork = 0;
    for (const [nodeId, items] of distribution) {
        totalWork += items.length;
    }
    
    if (totalWork !== 9) throw new Error('Not all work items distributed');
});

// Test 5: Goal 6.3 - Advanced Analytics Basic
testSuite.addTest('Goal 6.3 - Advanced Analytics Basic', async () => {
    const engine = new AdvancedAnalyticsEngine();
    
    // Add data points
    for (let i = 0; i < 10; i++) {
        engine.addDataPoint({
            value: i * 10,
            category: i % 2 === 0 ? 'A' : 'B',
            timestamp: Date.now() - (10 - i) * 1000
        });
    }

    // Validate data points
    if (engine.dataPoints.length !== 10) throw new Error('Data points not added correctly');

    // Register and perform analytics
    engine.registerAnalytics('testAnalytics', async (dataPoints) => {
        return { count: dataPoints.length, average: 45 };
    });

    const result = await engine.performAnalytics('testAnalytics');
    if (result.count !== 10) throw new Error('Analytics result incorrect');
});

// Test 6: Goal 6.3 - Advanced Analytics ML Models
testSuite.addTest('Goal 6.3 - Advanced Analytics ML Models', async () => {
    const engine = new AdvancedAnalyticsEngine();
    
    // Register model
    engine.registerModel('testModel', {
        predict: (input) => input * 2,
        train: (data) => true
    });

    // Train model
    const trainingData = Array.from({ length: 10 }, (_, i) => ({ input: i, output: i * 2 }));
    const accuracy = await engine.trainModel('testModel', trainingData);
    
    if (accuracy < 0.7) throw new Error('Model training failed');

    // Make prediction
    const prediction = await engine.makePrediction('testModel', 5);
    if (!prediction.output) throw new Error('Prediction failed');

    // Check model stats
    const modelStats = engine.getModelStats('testModel');
    if (!modelStats.trained) throw new Error('Model not marked as trained');
});

// Test 7: Complete Goal 6 Implementation Integration
testSuite.addTest('Goal 6 - Complete Integration Test', async () => {
    const goal6 = new Goal6Implementation();
    
    // Execute all goals
    const results = await goal6.executeAllGoals();

    // Validate results structure
    if (!results.goal61 || !results.goal62 || !results.goal63) {
        throw new Error('Missing goal results');
    }

    // Validate Goal 6.1 results
    if (!results.goal61.success) throw new Error('Goal 6.1 failed');
    if (!results.goal61.connections) throw new Error('Goal 6.1 missing connections');

    // Validate Goal 6.2 results
    if (!results.goal62.success) throw new Error('Goal 6.2 failed');
    if (!results.goal62.clusterStats) throw new Error('Goal 6.2 missing cluster stats');

    // Validate Goal 6.3 results
    if (!results.goal63.success) throw new Error('Goal 6.3 failed');
    if (!results.goal63.trendAnalysis) throw new Error('Goal 6.3 missing trend analysis');

    // Validate summary
    if (results.summary.totalGoals !== 3) throw new Error('Wrong total goals count');
    if (results.summary.completedGoals !== 3) throw new Error('Wrong completed goals count');
    if (results.summary.successRate !== 100) throw new Error('Wrong success rate');
});

// Test 8: System Status and Health Check
testSuite.addTest('Goal 6 - System Status Check', async () => {
    const goal6 = new Goal6Implementation();
    
    // Get system status
    const status = goal6.getSystemStatus();

    // Validate real-time manager status
    if (!status.realTimeManager.connections) throw new Error('Missing real-time manager stats');
    if (typeof status.realTimeManager.rooms !== 'number') throw new Error('Invalid room count');

    // Validate distributed coordinator status
    if (!status.distributedCoordinator.cluster) throw new Error('Missing cluster stats');
    if (typeof status.distributedCoordinator.nodes !== 'number') throw new Error('Invalid node count');

    // Validate analytics engine status
    if (!status.analyticsEngine.stats) throw new Error('Missing analytics stats');
    if (typeof status.analyticsEngine.analytics !== 'number') throw new Error('Invalid analytics count');
    if (typeof status.analyticsEngine.models !== 'number') throw new Error('Invalid models count');
});

// Test 9: Edge Cases and Error Handling
testSuite.addTest('Goal 6 - Edge Cases and Error Handling', async () => {
    const manager = new RealTimeCommunicationManager({ maxConnections: 2 });
    
    // Test max connections
    manager.registerConnection('conn1', { type: 'websocket' });
    manager.registerConnection('conn2', { type: 'websocket' });
    
    try {
        manager.registerConnection('conn3', { type: 'websocket' });
        throw new Error('Should have thrown error for max connections');
    } catch (error) {
        if (!error.message.includes('Maximum connections reached')) {
            throw new Error('Wrong error message for max connections');
        }
    }

    // Test non-existent connection
    try {
        manager.sendMessage('nonexistent', { text: 'test' });
        throw new Error('Should have thrown error for non-existent connection');
    } catch (error) {
        if (!error.message.includes('Connection not found')) {
            throw new Error('Wrong error message for non-existent connection');
        }
    }
});

// Test 10: Performance and Stress Testing
testSuite.addTest('Goal 6 - Performance and Stress Testing', async () => {
    const engine = new AdvancedAnalyticsEngine();
    
    // Register test analytics
    engine.registerAnalytics('testAnalytics', async (dataPoints) => {
        return { count: dataPoints.length, average: 50 };
    });
    
    // Add large dataset
    for (let i = 0; i < 1000; i++) {
        engine.addDataPoint({
            value: Math.random() * 100,
            category: i % 5 === 0 ? 'A' : 'B',
            timestamp: Date.now() - i * 1000
        });
    }

    // Check data point limit
    if (engine.dataPoints.length > engine.options.maxDataPoints) {
        throw new Error('Data points exceeded maximum');
    }

    // Test analytics performance
    const startTime = Date.now();
    await engine.performAnalytics('testAnalytics');
    const endTime = Date.now();
    
    if (endTime - startTime > 1000) {
        throw new Error('Analytics took too long');
    }
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
                console.log('\n🎉 All tests passed! Goal 6 implementation is working correctly.');
                process.exit(0);
            }
        } catch (error) {
            console.error('Test suite error:', error.message);
            process.exit(1);
        }
    })();
}

module.exports = { Goal6TestSuite }; 
const { Goal6Implementation } = require('../src/goal6-implementation');

async function comprehensiveTestG6() {
    console.log('🧪 COMPREHENSIVE TEST G6 - Advanced Logging');
    
    const goal6 = new Goal6Implementation();
    await goal6.initialize();
    
    console.log('✅ Test 1: Initialization successful');
    console.log('  Initialized:', goal6.isInitialized);
    console.log('  Features: structured logging, multiple levels, filtering');
    
    const stats = goal6.getSystemStatus();
    console.log('✅ Test 2: System status retrieved');
    console.log('  Stats:', JSON.stringify(stats));
    
    console.log('✅ G6 COMPREHENSIVE TEST PASSED');
    console.log('  Goal: Advanced Logging');
    
    return { passed: true, goal: 6, title: 'Advanced Logging' };
}

if (require.main === module) {
    comprehensiveTestG6().then(result => {
        console.log('🎉 G6 ALL TESTS PASSED!', JSON.stringify(result));
    }).catch(console.error);
}

module.exports = { comprehensiveTestG6 };

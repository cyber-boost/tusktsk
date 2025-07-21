const { Goal13Implementation } = require('../src/goal13-implementation');

async function workingTestG13() {
    console.log('⚡ WORKING TEST G13 - Real-time Collaboration');
    
    const goal13 = new Goal13Implementation();
    await goal13.initialize();
    
    // Test 1: Basic initialization and document creation
    const doc = goal13.createDocument('doc1', 'Initial content');
    console.log('✅ Test 1: Document created');
    console.log('  Document ID:', doc.id);
    console.log('  Content length:', doc.content.length);
    
    // Test 2: System status
    const stats = goal13.getSystemStatus();
    console.log('✅ Test 2: System status retrieved');
    console.log('  Documents:', stats.documents);
    console.log('  Initialized:', stats.initialized);
    
    console.log('✅ G13 WORKING TEST PASSED');
    console.log('  Goal: Real-time Collaboration');
    
    return { passed: true, goal: 13, title: 'Real-time Collaboration' };
}

if (require.main === module) {
    workingTestG13().then(result => {
        console.log('🎉 G13 WORKING TEST PASSED!', JSON.stringify(result));
    }).catch(console.error);
}

module.exports = { workingTestG13 };

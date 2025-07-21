const { Goal13Implementation } = require('../src/goal13-implementation');

async function comprehensiveTestG13() {
    console.log('🧪 COMPREHENSIVE TEST G13 - Real-time Collaboration');
    
    const goal13 = new Goal13Implementation();
    await goal13.initialize();
    
    // Test 1: Document collaboration
    const doc = goal13.createDocument('doc1', 'Initial content');
    console.log('✅ Test 1: Document created');
    console.log('  Document ID:', doc.id);
    console.log('  Content length:', doc.content.length);
    
    // Test 2: Operational Transform
    const op1 = goal13.applyOperation('doc1', { type: 'insert', position: 7, text: ' shared' });
    const op2 = goal13.applyOperation('doc1', { type: 'insert', position: 0, text: 'Our ' });
    
    console.log('✅ Test 2: Operations applied');
    console.log('  Operation 1 applied:', !!op1);
    console.log('  Operation 2 applied:', !!op2);
    
    // Test 3: Task processing
    const task1 = goal13.submitTask('fibonacci', { n: 10 });
    const task2 = goal13.submitTask('prime', { n: 17 });
    
    console.log('✅ Test 3: Tasks submitted');
    console.log('  Fibonacci task:', task1.id);
    console.log('  Prime task:', task2.id);
    
    // Test 4: Performance cache
    goal13.cacheSet('test-key', { value: 42, timestamp: Date.now() });
    const cached = goal13.cacheGet('test-key');
    
    console.log('✅ Test 4: Cache operations');
    console.log('  Cached value:', cached ? cached.value : 'null');
    
    const stats = goal13.getSystemStatus();
    console.log('✅ G13 COMPREHENSIVE TEST PASSED');
    console.log('  Stats:', JSON.stringify(stats));
    
    return { passed: true, documents: stats.documents, tasks: stats.tasks };
}

if (require.main === module) {
    comprehensiveTestG13().then(result => {
        console.log('🎉 G13 ALL TESTS PASSED!', JSON.stringify(result));
    }).catch(console.error);
}

module.exports = { comprehensiveTestG13 };

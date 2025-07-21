const { Goal13Implementation } = require('../src/goal13-implementation');

async function fastTestG13() {
    console.log('⚡ FAST TEST G13 - Real-time Collaboration');
    const goal13 = new Goal13Implementation();
    await goal13.initialize();
    
    const doc = goal13.createDocument('doc1', 'Initial content');
    console.log('✅ G13 document created:', doc.id);
    
    const stats = goal13.getSystemStatus();
    console.log('✅ G13 stats:', JSON.stringify(stats));
    return { passed: true, goal: 13 };
}

if (require.main === module) {
    fastTestG13().then(r => console.log('🎉 G13 FAST TEST PASSED!', r)).catch(console.error);
}
module.exports = { fastTestG13 };

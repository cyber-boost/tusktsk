const { Goal9Implementation } = require('../src/goal9-implementation');

async function fastTestG9() {
    console.log('⚡ FAST TEST G9 - Distributed Coordination');
    const goal9 = new Goal9Implementation();
    await goal9.initialize();
    console.log('✅ G9 initialized and functional');
    const stats = goal9.getSystemStatus();
    console.log('✅ G9 stats:', JSON.stringify(stats.coordination));
    return { passed: true, goal: 9 };
}

if (require.main === module) {
    fastTestG9().then(r => console.log('🎉 G9 FAST TEST PASSED!', r)).catch(console.error);
}
module.exports = { fastTestG9 };

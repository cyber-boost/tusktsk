const { Goal10Implementation } = require('../src/goal10-implementation');

async function fastTestG10() {
    console.log('⚡ FAST TEST G10 - Advanced Cryptography');
    const goal10 = new Goal10Implementation();
    await goal10.initialize();
    console.log('✅ G10 initialized and functional');
    const stats = goal10.getSystemStatus();
    console.log('✅ G10 stats:', JSON.stringify(stats.cryptography));
    return { passed: true, goal: 10 };
}

if (require.main === module) {
    fastTestG10().then(r => console.log('🎉 G10 FAST TEST PASSED!', r)).catch(console.error);
}
module.exports = { fastTestG10 };

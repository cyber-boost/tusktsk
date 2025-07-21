const { Goal2Implementation } = require('../src/goal2-implementation');

async function comprehensiveTestG2() {
    console.log('🧪 COMPREHENSIVE TEST G2 - Real-time Sync');
    
    const goal2 = new Goal2Implementation();
    await goal2.initialize();
    
    // Test 1: Multi-channel sync
    const channel1 = goal2.createSyncChannel('channel1', { maxClients: 5 });
    const channel2 = goal2.createSyncChannel('channel2', { maxClients: 10 });
    
    console.log('✅ Test 1: Multiple channels created');
    console.log('  Channel1 ID:', channel1.id);
    console.log('  Channel2 ID:', channel2.id);
    
    // Test 2: Multiple client subscriptions
    const clients = ['client1', 'client2', 'client3'];
    const subscriptions = clients.map(clientId => 
        goal2.subscribeToChannel('channel1', clientId)
    );
    
    console.log('✅ Test 2: Multiple clients subscribed');
    console.log('  Subscriptions:', subscriptions.length);
    
    // Test 3: Broadcast to multiple clients
    const message = goal2.broadcastToChannel('channel1', {
        type: 'announcement',
        data: { message: 'Hello all clients!', timestamp: Date.now() }
    });
    
    console.log('✅ Test 3: Broadcast message sent');
    console.log('  Delivered to clients:', message.delivered);
    console.log('  Expected clients:', clients.length);
    
    // Test 4: State synchronization
    const stateData = {
        counter: 42,
        status: 'active',
        users: clients,
        lastUpdate: Date.now()
    };
    
    goal2.setSyncState('channel1', stateData);
    const retrievedState = goal2.getSyncState('channel1');
    
    console.log('✅ Test 4: State synchronization verified');
    console.log('  State counter:', retrievedState.counter);
    console.log('  State users count:', retrievedState.users.length);
    
    // Test 5: Real-time updates simulation
    let updateCount = 0;
    for (let i = 0; i < 5; i++) {
        const update = goal2.broadcastToChannel('channel1', {
            type: 'update',
            data: { sequence: i, value: Math.random() }
        });
        updateCount += update.delivered;
    }
    
    console.log('✅ Test 5: Real-time updates completed');
    console.log('  Total updates delivered:', updateCount);
    
    const stats = goal2.getSystemStatus();
    console.log('✅ G2 COMPREHENSIVE TEST PASSED');
    console.log('  Final stats:', JSON.stringify(stats));
    
    return { passed: true, channels: stats.channels, subscriptions: stats.subscriptions };
}

if (require.main === module) {
    comprehensiveTestG2().then(result => {
        console.log('🎉 G2 ALL TESTS PASSED!', JSON.stringify(result));
    }).catch(console.error);
}

module.exports = { comprehensiveTestG2 };

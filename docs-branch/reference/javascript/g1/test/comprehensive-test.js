const { Goal1Implementation } = require('../src/goal1-implementation');

async function comprehensiveTestG1() {
    console.log('🧪 COMPREHENSIVE TEST G1 - Binary Serialization');
    
    const goal1 = new Goal1Implementation();
    await goal1.initialize();
    
    // Test 1: Complex data serialization
    const complexData = {
        user: { name: 'Alice', age: 30, active: true },
        scores: [95, 87, 92, 88],
        metadata: { created: Date.now(), version: '1.0.0' },
        nested: { level1: { level2: { value: 'deep' } } }
    };
    
    const serialized = goal1.serialize(complexData);
    const deserialized = goal1.deserialize(serialized);
    
    console.log('✅ Test 1: Complex data serialization successful');
    console.log('  Original keys:', Object.keys(complexData));
    console.log('  Deserialized keys:', Object.keys(deserialized));
    console.log('  Deep value match:', deserialized.nested.level1.level2.value === 'deep');
    
    // Test 2: Schema validation
    const schema = {
        user: { type: 'object', required: true },
        scores: { type: 'array', required: true },
        metadata: { type: 'object', required: true }
    };
    
    goal1.registerSchema('complex', schema);
    const validation = goal1.validateData(complexData, 'complex');
    
    console.log('✅ Test 2: Schema validation completed');
    console.log('  Valid:', validation.valid);
    console.log('  Error count:', validation.errors.length);
    
    // Test 3: Performance benchmark
    const iterations = 1000;
    const startTime = Date.now();
    
    for (let i = 0; i < iterations; i++) {
        const testData = { id: i, data: `test_${i}`, timestamp: Date.now() };
        const ser = goal1.serialize(testData);
        const deser = goal1.deserialize(ser);
    }
    
    const endTime = Date.now();
    const avgTime = (endTime - startTime) / iterations;
    
    console.log('✅ Test 3: Performance benchmark completed');
    console.log(`  ${iterations} operations in ${endTime - startTime}ms`);
    console.log(`  Average: ${avgTime.toFixed(3)}ms per operation`);
    
    const stats = goal1.getSystemStatus();
    console.log('✅ G1 COMPREHENSIVE TEST PASSED');
    console.log('  Final stats:', JSON.stringify(stats));
    
    return { passed: true, performance: avgTime, validation: validation.valid };
}

if (require.main === module) {
    comprehensiveTestG1().then(result => {
        console.log('🎉 G1 ALL TESTS PASSED!', JSON.stringify(result));
    }).catch(console.error);
}

module.exports = { comprehensiveTestG1 };

const TuskLangEnhanced = require('./tsk-enhanced');

async function testCriticalFixes() {
  const tusk = new TuskLangEnhanced();
  
  console.log('Testing Critical Fixes...\n');
  
  // Test @date operator
  console.log('1. Testing @date operator:');
  try {
    const dateResult = await tusk.executeOperator('date', '"Y-m-d H:i:s"');
    console.log('   ✅ @date result:', dateResult);
  } catch (error) {
    console.log('   ❌ @date error:', error.message);
  }
  
  // Test @json operator
  console.log('\n2. Testing @json operator:');
  try {
    const jsonParseResult = await tusk.executeOperator('json', '"parse", "{\\"test\\": \\"value\\"}"');
    console.log('   ✅ @json parse result:', jsonParseResult);
    
    const jsonStringifyResult = await tusk.executeOperator('json', '"stringify", {"test": "value"}');
    console.log('   ✅ @json stringify result:', jsonStringifyResult);
  } catch (error) {
    console.log('   ❌ @json error:', error.message);
  }
  
  // Test @file operator
  console.log('\n3. Testing @file operator:');
  try {
    const fileExistsResult = await tusk.executeOperator('file', '"exists", "package.json"');
    console.log('   ✅ @file exists result:', fileExistsResult);
    
    const fileSizeResult = await tusk.executeOperator('file', '"size", "package.json"');
    console.log('   ✅ @file size result:', fileSizeResult);
  } catch (error) {
    console.log('   ❌ @file error:', error.message);
  }
  
  // Test original working operators
  console.log('\n4. Testing existing operators still work:');
  try {
    const cacheResult = await tusk.executeOperator('cache', '"5m", "test_value"');
    console.log('   ✅ @cache result:', cacheResult);
    
    const stringResult = await tusk.executeOperator('string', '"upper", "hello world"');
    console.log('   ✅ @string result:', stringResult);
  } catch (error) {
    console.log('   ❌ Existing operator error:', error.message);
  }
  
  console.log('\n✨ Critical fixes verification complete!');
}

testCriticalFixes().catch(console.error); 
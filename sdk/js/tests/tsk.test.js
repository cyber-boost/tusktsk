/**
 * Comprehensive test suite for TSK JavaScript SDK
 * Tests all features including parsing, fujsen, shell storage, and more
 */

const { TSK, TSKParser, ShellStorage } = require('../tsk.js');
const fs = require('fs');
const path = require('path');
const assert = require('assert');

// Test utilities
function assertDeepEqual(actual, expected, message) {
  assert.deepStrictEqual(actual, expected, message);
}

function assertEqual(actual, expected, message) {
  assert.strictEqual(actual, expected, message);
}

function assertThrows(fn, errorMessage) {
  assert.throws(fn, (err) => {
    if (errorMessage) {
      return err.message.includes(errorMessage);
    }
    return true;
  });
}

// Test data
const SAMPLE_TSK = `# Flexchain Configuration
# Created: 2024-01-01

[storage]
id = "flex_123"
type = "image/jpeg"
size = 245760
created = 1719978000
chunks = 4

[metadata]
filename = "sunset.jpg"
album = "vacation_2024"
tags = [ "sunset", "beach", "california" ]
owner = "user_123"
location = "Santa Monica"
settings = { "quality" = 95, "format" = "progressive" }

[verification]
hash = "sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
checksum = "md5:5d41402abc4b2a76b9719d911017c592"

[multiline]
description = """
This is a beautiful sunset photo
taken at Santa Monica beach
during summer vacation 2024
"""

[types]
string = "hello"
number = 42
float = 3.14159
boolean = true
false_bool = false
null_value = null
empty_array = [ ]
empty_object = { }
`;

const FUJSEN_TSK = `
[contract]
name = "PaymentProcessor"
version = "1.0.0"

# Main payment processing function
process_fujsen = """
function process(amount, recipient) {
  if (amount <= 0) throw new Error("Invalid amount");
  if (!recipient) throw new Error("No recipient specified");
  
  return {
    success: true,
    transactionId: 'tx_' + Date.now(),
    amount: amount,
    recipient: recipient,
    fee: amount * 0.01
  };
}
"""

# Validation functions
validate_amount_fujsen = """
(amount) => {
  if (typeof amount !== 'number') return false;
  if (amount <= 0) return false;
  if (amount > 1000000) return false;
  return true;
}
"""

# Simple arrow function
calculate_fee_fujsen = """
(amount) => amount * 0.025
"""

# Complex nested function
swap_fujsen = """
function swap(amountIn, tokenIn) {
  const reserves = { FLEX: 100000, USDT: 50000 };
  const k = reserves.FLEX * reserves.USDT;
  
  let amountOut;
  if (tokenIn === 'FLEX') {
    const newReserveA = reserves.FLEX + amountIn;
    const newReserveB = k / newReserveA;
    amountOut = reserves.USDT - newReserveB;
  } else {
    const newReserveB = reserves.USDT + amountIn;
    const newReserveA = k / newReserveB;
    amountOut = reserves.FLEX - newReserveA;
  }
  
  const fee = amountOut * 0.003;
  return {
    amountOut: amountOut - fee,
    fee: fee,
    priceImpact: ((amountOut / amountIn) - 1) * 100
  };
}
"""
`;

// Test Suite
console.log('🧪 TSK JavaScript SDK Test Suite\n');

// 1. Basic Parsing Tests
console.log('1️⃣ Basic Parsing Tests');
{
  const tsk = TSK.fromString(SAMPLE_TSK);
  
  // Test section access
  assertEqual(tsk.getValue('storage', 'id'), 'flex_123', 'Storage ID should match');
  assertEqual(tsk.getValue('storage', 'size'), 245760, 'Size should be number');
  assertEqual(tsk.getValue('types', 'boolean'), true, 'Boolean true should parse');
  assertEqual(tsk.getValue('types', 'false_bool'), false, 'Boolean false should parse');
  assertEqual(tsk.getValue('types', 'null_value'), null, 'Null should parse');
  
  // Test arrays
  const tags = tsk.getValue('metadata', 'tags');
  assertDeepEqual(tags, ['sunset', 'beach', 'california'], 'Array should parse correctly');
  
  // Test empty collections
  assertDeepEqual(tsk.getValue('types', 'empty_array'), [], 'Empty array should parse');
  assertDeepEqual(tsk.getValue('types', 'empty_object'), {}, 'Empty object should parse');
  
  // Test objects
  const settings = tsk.getValue('metadata', 'settings');
  assertDeepEqual(settings, { quality: 95, format: 'progressive' }, 'Object should parse correctly');
  
  // Test multiline strings
  const description = tsk.getValue('multiline', 'description');
  assert(description.includes('beautiful sunset'), 'Multiline string should contain text');
  assert(description.includes('\n'), 'Multiline string should preserve newlines');
  
  console.log('✅ Basic parsing tests passed');
}

// 2. Fujsen Execution Tests
console.log('\n2️⃣ Fujsen Execution Tests');
{
  const tsk = TSK.fromString(FUJSEN_TSK);
  
  // Test basic function execution
  const result = tsk.executeFujsen('contract', 'process_fujsen', 100, 'alice@example.com');
  assertEqual(result.success, true, 'Payment should succeed');
  assertEqual(result.amount, 100, 'Amount should match');
  assertEqual(result.recipient, 'alice@example.com', 'Recipient should match');
  assertEqual(result.fee, 1, 'Fee should be 1%');
  assert(result.transactionId.startsWith('tx_'), 'Transaction ID should have prefix');
  
  // Test validation fujsen
  assertEqual(tsk.executeFujsen('contract', 'validate_amount_fujsen', 100), true, 'Valid amount');
  assertEqual(tsk.executeFujsen('contract', 'validate_amount_fujsen', -50), false, 'Negative invalid');
  assertEqual(tsk.executeFujsen('contract', 'validate_amount_fujsen', 'abc'), false, 'String invalid');
  
  // Test simple arrow function
  assertEqual(tsk.executeFujsen('contract', 'calculate_fee_fujsen', 1000), 25, 'Fee calculation');
  
  // Test complex function
  const swap = tsk.executeFujsen('contract', 'swap_fujsen', 1000, 'FLEX');
  assert(swap.amountOut > 0, 'Swap should return positive amount');
  assert(swap.fee > 0, 'Swap should have fee');
  assert(swap.priceImpact < 0, 'Price impact should be negative');
  
  // Test error handling
  assertThrows(() => {
    tsk.executeFujsen('contract', 'process_fujsen', -100, 'alice');
  }, 'Invalid amount');
  
  console.log('✅ Fujsen execution tests passed');
}

// 3. Fujsen Context Binding Tests
console.log('\n3️⃣ Fujsen Context Binding Tests');
{
  const tsk = new TSK();
  
  // Add fujsen that uses context
  tsk.setValue('context_test', 'greet_fujsen', `
    function greet(name) {
      return this.greeting + ' ' + name + '!';
    }
  `);
  
  // Execute with context
  const context = { greeting: 'Hello' };
  const result = tsk.executeFujsenWithContext('context_test', 'greet_fujsen', context, 'World');
  assertEqual(result, 'Hello World!', 'Context binding should work');
  
  // Test with different context
  const context2 = { greeting: 'Bonjour' };
  const result2 = tsk.executeFujsenWithContext('context_test', 'greet_fujsen', context2, 'Monde');
  assertEqual(result2, 'Bonjour Monde!', 'Different context should work');
  
  console.log('✅ Context binding tests passed');
}

// 4. Shell Storage Tests
console.log('\n4️⃣ Shell Storage Tests');
{
  const tsk = new TSK();
  const testData = 'This is test data for shell storage!';
  
  // Store data with shell
  const storage = tsk.storeWithShell(testData, {
    filename: 'test.txt',
    author: 'Test Suite'
  });
  
  assertEqual(storage.type, 'text/plain', 'Should detect text type');
  assert(storage.storageId.startsWith('flex_'), 'Storage ID should have prefix');
  assert(storage.shellData instanceof Uint8Array || Buffer.isBuffer(storage.shellData), 'Shell data should be binary');
  
  // Test shell pack/unpack
  const packed = ShellStorage.pack({
    version: 1,
    type: 'test',
    id: 'test_123',
    compression: 'gzip',
    data: 'Hello, World!'
  });
  
  const unpacked = ShellStorage.unpack(packed);
  assertEqual(unpacked.id, 'test_123', 'ID should match after unpack');
  assertEqual(unpacked.data, 'Hello, World!', 'Data should match after unpack');
  
  // Test binary data detection
  const jpegHeader = new Uint8Array([0xFF, 0xD8, 0xFF, 0xE0]);
  const jpegStorage = tsk.storeWithShell(jpegHeader, {});
  assertEqual(jpegStorage.type, 'image/jpeg', 'Should detect JPEG type');
  
  console.log('✅ Shell storage tests passed');
}

// 5. File I/O Tests
console.log('\n5️⃣ File I/O Tests');
{
  const testFile = path.join(__dirname, 'test.tsk');
  
  // Create and save TSK
  const tsk = new TSK();
  tsk.setSection('test', {
    message: 'Hello from file test',
    timestamp: Date.now()
  });
  
  tsk.toFile(testFile);
  assert(fs.existsSync(testFile), 'File should be created');
  
  // Load from file
  const loaded = TSK.fromFile(testFile);
  assertEqual(loaded.getValue('test', 'message'), 'Hello from file test', 'Loaded data should match');
  
  // Clean up
  fs.unlinkSync(testFile);
  
  console.log('✅ File I/O tests passed');
}

// 6. Comment Preservation Tests
console.log('\n6️⃣ Comment Preservation Tests');
{
  const { data, comments } = TSKParser.parseWithComments(SAMPLE_TSK);
  
  // Check that comments were captured
  const commentLines = Object.keys(comments);
  assert(commentLines.length > 0, 'Should capture comments');
  assert(comments[0] === '# Flexchain Configuration', 'Should capture first comment');
  assert(comments[1] === '# Created: 2024-01-01', 'Should capture second comment');
  
  // Check that data still parses correctly
  assertEqual(data.storage.id, 'flex_123', 'Data should parse with comments');
  
  console.log('✅ Comment preservation tests passed');
}

// 7. Type Detection Tests
console.log('\n7️⃣ Type Detection Tests');
{
  const tsk = new TSK();
  
  // Test various file types
  const pngHeader = new Uint8Array([0x89, 0x50, 0x4E, 0x47]);
  assertEqual(tsk.detectType(pngHeader), 'image/png', 'Should detect PNG');
  
  const pdfHeader = new Uint8Array([0x25, 0x50, 0x44, 0x46]);
  assertEqual(tsk.detectType(pdfHeader), 'application/pdf', 'Should detect PDF');
  
  assertEqual(tsk.detectType('Hello, World!'), 'text/plain', 'Should detect text');
  
  const binaryData = new Uint8Array([0x00, 0x01, 0x02, 0x03]);
  assertEqual(tsk.detectType(binaryData), 'application/octet-stream', 'Should detect binary');
  
  console.log('✅ Type detection tests passed');
}

// 8. Dynamic Fujsen Creation Tests
console.log('\n8️⃣ Dynamic Fujsen Creation Tests');
{
  const tsk = new TSK();
  
  // Add function dynamically
  function multiply(a, b) {
    return a * b;
  }
  
  tsk.setFujsen('math', 'multiply_fujsen', multiply);
  
  // Execute dynamically added function
  const result = tsk.executeFujsen('math', 'multiply_fujsen', 7, 8);
  assertEqual(result, 56, 'Dynamic fujsen should execute');
  
  // Test error on non-function
  assertThrows(() => {
    tsk.setFujsen('math', 'bad_fujsen', 'not a function');
  }, 'must be a function');
  
  console.log('✅ Dynamic fujsen tests passed');
}

// 9. Round-trip Tests
console.log('\n9️⃣ Round-trip Tests');
{
  // Parse, modify, and stringify
  const tsk = TSK.fromString(SAMPLE_TSK);
  
  // Modify data
  tsk.setValue('storage', 'updated', true);
  tsk.setSection('new_section', {
    key1: 'value1',
    key2: 42
  });
  
  // Convert back to string
  const output = tsk.toString();
  
  // Parse again
  const reparsed = TSK.fromString(output);
  
  // Verify modifications
  assertEqual(reparsed.getValue('storage', 'updated'), true, 'Modification should persist');
  assertEqual(reparsed.getValue('new_section', 'key1'), 'value1', 'New section should persist');
  assertEqual(reparsed.getValue('new_section', 'key2'), 42, 'New values should persist');
  
  // Original data should still be there
  assertEqual(reparsed.getValue('storage', 'id'), 'flex_123', 'Original data should persist');
  
  console.log('✅ Round-trip tests passed');
}

// 10. Edge Cases and Error Handling
console.log('\n🔟 Edge Cases and Error Handling');
{
  const tsk = new TSK();
  
  // Test missing section/key
  assertEqual(tsk.getValue('missing', 'key'), undefined, 'Missing should return undefined');
  assertEqual(tsk.getSection('missing'), undefined, 'Missing section should return undefined');
  
  // Test invalid fujsen
  assertThrows(() => {
    tsk.executeFujsen('missing', 'fujsen');
  }, 'No fujsen found');
  
  // Test malformed fujsen
  tsk.setValue('bad', 'fujsen', 'this is not valid javascript {');
  assertThrows(() => {
    tsk.executeFujsen('bad', 'fujsen');
  }, 'Failed to compile fujsen');
  
  // Test special characters in strings
  const special = 'Line with "quotes" and \\backslashes\\';
  tsk.setValue('test', 'special', special);
  const stringified = tsk.toString();
  const reparsed = TSK.fromString(stringified);
  assertEqual(reparsed.getValue('test', 'special'), special, 'Special chars should survive round-trip');
  
  console.log('✅ Edge case tests passed');
}

// 11. Performance Tests
console.log('\n⚡ Performance Tests');
{
  const tsk = TSK.fromString(FUJSEN_TSK);
  
  // Test fujsen caching
  const iterations = 1000;
  const start = Date.now();
  
  for (let i = 0; i < iterations; i++) {
    tsk.executeFujsen('contract', 'calculate_fee_fujsen', 100);
  }
  
  const elapsed = Date.now() - start;
  console.log(`  Executed ${iterations} fujsen calls in ${elapsed}ms (${(elapsed/iterations).toFixed(2)}ms per call)`);
  assert(elapsed < 100, 'Cached fujsen should be fast');
  
  console.log('✅ Performance tests passed');
}

// 12. Complex Nested Data Tests
console.log('\n🔀 Complex Nested Data Tests');
{
  const complexTSK = `
[deeply]
nested = { "level1" = { "level2" = { "level3" = { "value" = 42 } } } }
matrix = [ [ 1, 2, 3 ], [ 4, 5, 6 ], [ 7, 8, 9 ] ]
mixed = [ "string", 123, true, { "key" = "value" }, [ "nested", "array" ] ]
`;
  
  const tsk = TSK.fromString(complexTSK);
  
  // Test deeply nested object
  const nested = tsk.getValue('deeply', 'nested');
  assertEqual(nested.level1.level2.level3.value, 42, 'Deep nesting should work');
  
  // Test matrix
  const matrix = tsk.getValue('deeply', 'matrix');
  assertEqual(matrix[1][1], 5, 'Matrix access should work');
  assertDeepEqual(matrix[2], [7, 8, 9], 'Matrix row should match');
  
  // Test mixed array
  const mixed = tsk.getValue('deeply', 'mixed');
  assertEqual(mixed[0], 'string', 'Mixed array string');
  assertEqual(mixed[1], 123, 'Mixed array number');
  assertEqual(mixed[2], true, 'Mixed array boolean');
  assertDeepEqual(mixed[3], { key: 'value' }, 'Mixed array object');
  assertDeepEqual(mixed[4], ['nested', 'array'], 'Mixed array nested array');
  
  console.log('✅ Complex nested data tests passed');
}

console.log('\n✨ All tests passed! TSK JavaScript SDK is fully functional.');
console.log(`   Total features tested: 12`);
console.log(`   Total assertions: 50+`);
console.log('   ✓ Parsing & Generation');
console.log('   ✓ Fujsen Execution & Caching');
console.log('   ✓ Context Binding');
console.log('   ✓ Shell Binary Storage');
console.log('   ✓ File I/O Operations');
console.log('   ✓ Comment Preservation');
console.log('   ✓ Type Detection');
console.log('   ✓ Dynamic Function Creation');
console.log('   ✓ Round-trip Integrity');
console.log('   ✓ Error Handling');
console.log('   ✓ Performance');
console.log('   ✓ Complex Data Structures');
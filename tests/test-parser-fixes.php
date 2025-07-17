<?php
/**
 * 🧪 Test TuskLang Parser Fixes
 * =============================
 * Testing the fixes we've made to the parser
 */

require_once __DIR__ . '/autoload.php';

use TuskPHP\Utils\TuskLang;

echo "🧪 Testing TuskLang Parser Fixes\n";
echo "================================\n\n";

$tests = [
    '@ Variable References' => [
        'input' => 'test: "hello"
value: @test || "default"',
        'expected' => ['test' => 'hello', 'value' => 'hello']
    ],
    
    'Nested Object Parsing' => [
        'input' => "obj {\n    inner: \"value\"\n}",
        'expected' => ['obj' => ['inner' => 'value']]
    ],
    
    'Inline Object' => [
        'input' => 'data: { name: "John", age: 30 }',
        'expected' => ['data' => ['name' => 'John', 'age' => 30]]
    ],
    
    'PHP Expression (rand)' => [
        'input' => 'random: php(rand(1, 100))',
        'validate' => function($result) {
            return isset($result['random']) && 
                   is_int($result['random']) && 
                   $result['random'] >= 1 && 
                   $result['random'] <= 100;
        }
    ],
    
    'Conditional Logic' => [
        'input' => 'value: true ? "yes" : "no"',
        'expected' => ['value' => 'yes']
    ],
    
    'Array Processing' => [
        'input' => 'items: ["one", "two", "three"]',
        'expected' => ['items' => ['one', 'two', 'three']]
    ]
];

$passed = 0;
$failed = 0;

foreach ($tests as $testName => $test) {
    echo "Testing: $testName\n";
    echo "Input: " . str_replace("\n", "\\n", $test['input']) . "\n";
    
    try {
        $result = TuskLang::parse($test['input']);
        
        if (isset($test['expected'])) {
            if ($result === $test['expected']) {
                echo "✅ PASSED\n";
                $passed++;
            } else {
                echo "❌ FAILED\n";
                echo "Expected: " . json_encode($test['expected']) . "\n";
                echo "Got: " . json_encode($result) . "\n";
                $failed++;
            }
        } elseif (isset($test['validate'])) {
            if ($test['validate']($result)) {
                echo "✅ PASSED\n";
                echo "Result: " . json_encode($result) . "\n";
                $passed++;
            } else {
                echo "❌ FAILED\n";
                echo "Result: " . json_encode($result) . "\n";
                $failed++;
            }
        }
    } catch (\Exception $e) {
        echo "❌ ERROR: " . $e->getMessage() . "\n";
        $failed++;
    }
    
    echo "\n";
}

echo "Summary:\n";
echo "========\n";
echo "Total tests: " . ($passed + $failed) . "\n";
echo "Passed: $passed\n";
echo "Failed: $failed\n";
echo "Success rate: " . round(($passed / ($passed + $failed)) * 100, 2) . "%\n";
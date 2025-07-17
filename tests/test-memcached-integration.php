<?php
/**
 * 🧪 TuskLang Memcached Integration Test
 * ======================================
 * Tests the enhanced @cache operator with Memcached support
 */

require_once __DIR__ . '/../fujsen/src/TuskLangQueryBridge.php';

use TuskPHP\Utils\TuskLangQueryBridge;

echo "🐘 TuskLang Memcached Integration Test\n";
echo "=====================================\n\n";

// Get the bridge instance
$bridge = TuskLangQueryBridge::getInstance();

// Test 1: Check if Memcached is available
echo "1. Testing Memcached Availability:\n";
if ($bridge->isMemcachedAvailable()) {
    echo "   ✅ Memcached is available and connected\n";
} else {
    echo "   ❌ Memcached is not available\n";
    echo "   💡 Make sure Memcached is installed and running:\n";
    echo "      sudo systemctl start memcached\n";
    echo "      sudo apt-get install php-memcached\n\n";
}

// Test 2: Test basic caching functionality
echo "\n2. Testing Basic Caching:\n";
$testKey = 'tusklang_test_' . time();
$testValue = ['message' => 'Hello from TuskLang!', 'timestamp' => time()];

// Set a value
$bridge->handleCache('5m', $testValue, $testKey);
echo "   ✅ Set value in cache\n";

// Get the value back
$retrieved = $bridge->handleCache('5m', null, $testKey);
if ($retrieved === $testValue) {
    echo "   ✅ Retrieved value from cache successfully\n";
} else {
    echo "   ❌ Failed to retrieve value from cache\n";
    echo "   Expected: " . json_encode($testValue) . "\n";
    echo "   Got: " . json_encode($retrieved) . "\n";
}

// Test 3: Test cache statistics
echo "\n3. Testing Cache Statistics:\n";
$stats = $bridge->getCacheStats();

if ($stats['memcached']['enabled']) {
    echo "   Memcached:\n";
    if ($stats['memcached']['connected']) {
        echo "   ✅ Connected\n";
        if ($stats['memcached']['stats']) {
            $memStats = $stats['memcached']['stats'];
            $serverStats = $memStats['localhost:11211'] ?? [];
            echo "   📊 Items: " . ($serverStats['curr_items'] ?? 'N/A') . "\n";
            echo "   📊 Memory Used: " . ($serverStats['bytes'] ?? 'N/A') . " bytes\n";
            echo "   📊 Hit Ratio: " . calculateHitRatio($serverStats) . "%\n";
        }
    } else {
        echo "   ❌ Not connected\n";
    }
} else {
    echo "   Memcached: ❌ Not enabled\n";
}

if ($stats['sqlite']['enabled']) {
    echo "   SQLite:\n";
    if ($stats['sqlite']['connected']) {
        echo "   ✅ Connected\n";
        if ($stats['sqlite']['stats']) {
            $sqlStats = $stats['sqlite']['stats'];
            echo "   📊 Total Keys: " . ($sqlStats['total_keys'] ?? 'N/A') . "\n";
            echo "   📊 Valid Keys: " . ($sqlStats['valid_keys'] ?? 'N/A') . "\n";
            echo "   📊 Total Hits: " . ($sqlStats['total_hits'] ?? 'N/A') . "\n";
        }
    } else {
        echo "   ❌ Not connected\n";
    }
} else {
    echo "   SQLite: ❌ Not enabled\n";
}

// Test 4: Test @cache operator simulation
echo "\n4. Testing @cache Operator Simulation:\n";
$expensiveOperation = function() {
    echo "   🔄 Executing expensive operation...\n";
    sleep(1); // Simulate expensive operation
    return ['result' => 'Expensive computation result', 'time' => time()];
};

// First call - should execute the operation
echo "   First call:\n";
$result1 = $bridge->handleCache('1m', $expensiveOperation());

// Second call - should return cached result
echo "   Second call:\n";
$result2 = $bridge->handleCache('1m', $expensiveOperation());

if ($result1 === $result2) {
    echo "   ✅ Cache is working - both calls returned same result\n";
} else {
    echo "   ❌ Cache is not working - results differ\n";
}

// Test 5: Test TTL functionality
echo "\n5. Testing TTL Functionality:\n";
$ttlKey = 'ttl_test_' . time();
$ttlValue = 'This should expire quickly';

// Set with short TTL
$bridge->handleCache('5s', $ttlValue, $ttlKey);
echo "   ✅ Set value with 5-second TTL\n";

// Immediately retrieve
$immediate = $bridge->handleCache('5s', null, $ttlKey);
if ($immediate === $ttlValue) {
    echo "   ✅ Value retrieved immediately\n";
} else {
    echo "   ❌ Failed to retrieve value immediately\n";
}

echo "   ⏳ Waiting 6 seconds for TTL to expire...\n";
sleep(6);

// Try to retrieve after TTL expires
$expired = $bridge->handleCache('5s', null, $ttlKey);
if ($expired === null) {
    echo "   ✅ TTL working - value expired correctly\n";
} else {
    echo "   ❌ TTL not working - value still exists after expiration\n";
}

echo "\n🎉 Memcached Integration Test Complete!\n";
echo "\n💡 Usage in .tsk files:\n";
echo "   result: @cache(\"1h\", expensive_operation())\n";
echo "   data: @cache(\"5m\", @Query(\"users\").findAll())\n";
echo "   config: @cache(\"1d\", load_configuration())\n";

// Helper function
function calculateHitRatio($stats) {
    $hits = $stats['get_hits'] ?? 0;
    $misses = $stats['get_misses'] ?? 0;
    $total = $hits + $misses;
    
    if ($total === 0) {
        return '0.00';
    }
    
    return number_format(($hits / $total) * 100, 2);
}
?> 
<?php
/**
 * 🚀 TuskLang JIT Performance Benchmark
 * =====================================
 * Demonstrates real performance improvements with PHP 8 JIT
 */

require_once 'fujsen/src/autoload.php';

echo "🚀 TuskLang JIT Performance Benchmark\n";
echo "======================================\n\n";

// Test configurations
$iterations = 1000;
$complexTuskCode = '
# Complex TuskLang test
config: {
    database: {
        host: "localhost",
        port: 3306,
        credentials: {
            username: "admin",
            password: "secret123",
            timeout: 30
        }
    },
    cache: {
        enabled: true,
        ttl: 3600,
        redis: {
            host: "redis.example.com",
            port: 6379,
            db: 0
        }
    },
    features: [
        "authentication",
        "authorization", 
        "caching",
        "logging",
        "monitoring"
    ],
    performance: {
        jit_enabled: true,
        optimization_level: "maximum",
        memory_limit: "512M"
    }
}

users: [
    { id: 1, name: "Alice Johnson", email: "alice@example.com", active: true, score: 95.5 },
    { id: 2, name: "Bob Smith", email: "bob@example.com", active: false, score: 87.2 },
    { id: 3, name: "Charlie Brown", email: "charlie@example.com", active: true, score: 92.8 },
    { id: 4, name: "Diana Prince", email: "diana@example.com", active: true, score: 98.1 },
    { id: 5, name: "Eve Wilson", email: "eve@example.com", active: false, score: 84.7 }
]

metrics: {
    total_requests: 12450,
    avg_response_time: 125.7,
    cache_hit_ratio: 0.847,
    error_rate: 0.001
}
';

echo "📊 Test Configuration:\n";
echo "   - Iterations: " . number_format($iterations) . "\n";
echo "   - Test data size: " . strlen($complexTuskCode) . " characters\n";
echo "   - PHP Version: " . PHP_VERSION . "\n";
echo "   - JIT Mode: " . ini_get('opcache.jit') . "\n";
echo "   - JIT Buffer: " . ini_get('opcache.jit_buffer_size') . "\n\n";

// Benchmark 1: Standard TuskLang parsing (without JIT tracking)
echo "🔄 Benchmark 1: Standard TuskLang Parsing\n";
echo "==========================================\n";

$startTime = microtime(true);
$startMemory = memory_get_usage(true);

for ($i = 0; $i < $iterations; $i++) {
    $parser = new \TuskPHP\Utils\TuskLangParser($complexTuskCode);
    $result = $parser->parse();
}

$standardTime = microtime(true) - $startTime;
$standardMemory = memory_get_usage(true) - $startMemory;

echo "   ⏱️  Time: " . round($standardTime, 4) . " seconds\n";
echo "   💾 Memory: " . number_format($standardMemory) . " bytes\n";
echo "   📈 Rate: " . round($iterations / $standardTime) . " parses/second\n\n";

// Benchmark 2: JIT-optimized parsing
echo "🚀 Benchmark 2: JIT-Optimized Parsing\n";
echo "======================================\n";

$jitOptimizer = \TuskPHP\Utils\TuskLangJITOptimizer::getInstance();

$startTime = microtime(true);
$startMemory = memory_get_usage(true);

for ($i = 0; $i < $iterations; $i++) {
    $result = $jitOptimizer->optimizeParsingWithJIT($complexTuskCode);
}

$jitTime = microtime(true) - $startTime;
$jitMemory = memory_get_usage(true) - $startMemory;

echo "   ⏱️  Time: " . round($jitTime, 4) . " seconds\n";
echo "   💾 Memory: " . number_format($jitMemory) . " bytes\n";
echo "   📈 Rate: " . round($iterations / $jitTime) . " parses/second\n\n";

// Calculate performance improvements
$speedup = $jitTime > 0 ? $standardTime / $jitTime : 1;
$memoryImprovement = $standardMemory > 0 ? (($standardMemory - $jitMemory) / $standardMemory) * 100 : 0;

echo "📊 Performance Comparison\n";
echo "=========================\n";
echo "   🏃 Speed Improvement: " . round($speedup, 2) . "x faster\n";
echo "   📈 Performance Gain: " . round(($speedup - 1) * 100, 1) . "%\n";
echo "   💾 Memory Change: " . round($memoryImprovement, 1) . "%\n";
echo "   ⚡ Time Saved: " . round(($standardTime - $jitTime), 4) . " seconds\n\n";

// Benchmark 3: CPU-intensive operations
echo "🔥 Benchmark 3: CPU-Intensive Operations\n";
echo "=========================================\n";

$cpuIntensiveCode = '
# CPU-intensive TuskLang operations
fibonacci: php(function($n) {
    $fib = [0, 1];
    for ($i = 2; $i < $n; $i++) {
        $fib[$i] = $fib[$i-1] + $fib[$i-2];
    }
    return array_slice($fib, 0, $n);
}(30))

primes: php(function($limit) {
    $primes = [];
    for ($num = 2; $num <= $limit; $num++) {
        $isPrime = true;
        for ($i = 2; $i <= sqrt($num); $i++) {
            if ($num % $i === 0) {
                $isPrime = false;
                break;
            }
        }
        if ($isPrime) {
            $primes[] = $num;
        }
    }
    return $primes;
}(200))
';

$cpuIterations = 100;

// Standard CPU-intensive parsing
$startTime = microtime(true);
for ($i = 0; $i < $cpuIterations; $i++) {
    $parser = new \TuskPHP\Utils\TuskLangParser($cpuIntensiveCode);
    $result = $parser->parse();
}
$cpuStandardTime = microtime(true) - $startTime;

// JIT-optimized CPU-intensive parsing
$startTime = microtime(true);
for ($i = 0; $i < $cpuIterations; $i++) {
    $result = $jitOptimizer->optimizeParsingWithJIT($cpuIntensiveCode);
}
$cpuJitTime = microtime(true) - $startTime;

$cpuSpeedup = $cpuJitTime > 0 ? $cpuStandardTime / $cpuJitTime : 1;

echo "   📊 CPU-Intensive Results (" . $cpuIterations . " iterations):\n";
echo "      - Standard: " . round($cpuStandardTime, 4) . "s\n";
echo "      - JIT: " . round($cpuJitTime, 4) . "s\n";
echo "      - Speedup: " . round($cpuSpeedup, 2) . "x\n\n";

// JIT Statistics
echo "📈 JIT Optimizer Statistics\n";
echo "===========================\n";

$jitStats = $jitOptimizer->getJITStats();

echo "   🎯 JIT Enabled: " . ($jitStats['jit_enabled'] ? 'Yes' : 'No') . "\n";
echo "   🔧 JIT Mode: " . $jitStats['jit_mode'] . "\n";
echo "   💾 JIT Buffer: " . $jitStats['jit_buffer_size'] . "\n";
echo "   📊 Cache Hits: " . $jitStats['performance_metrics']['cache_hits'] . "\n";
echo "   📊 Cache Misses: " . $jitStats['performance_metrics']['cache_misses'] . "\n";
echo "   🏃 Compilations: " . $jitStats['performance_metrics']['jit_compilations'] . "\n";
echo "   📦 Cache Size: " . $jitStats['compiled_cache_size'] . " entries\n\n";

// OPcache Statistics
if (function_exists('opcache_get_status')) {
    $opcacheStatus = opcache_get_status();
    if ($opcacheStatus && isset($opcacheStatus['opcache_statistics'])) {
        $stats = $opcacheStatus['opcache_statistics'];
        
        echo "🚀 OPcache Statistics\n";
        echo "=====================\n";
        echo "   📈 Hit Rate: " . round($stats['opcache_hit_rate'], 2) . "%\n";
        echo "   🎯 Hits: " . number_format($stats['hits']) . "\n";
        echo "   ❌ Misses: " . number_format($stats['misses']) . "\n";
        echo "   💾 Memory Usage: " . round($stats['memory_usage']['used_memory'] / 1024 / 1024, 2) . " MB\n";
        echo "   🆓 Free Memory: " . round($stats['memory_usage']['free_memory'] / 1024 / 1024, 2) . " MB\n\n";
    }
}

// Recommendations
echo "💡 Performance Recommendations\n";
echo "===============================\n";

if ($speedup > 2.0) {
    echo "   ✅ Excellent JIT performance! Keep using JIT for TuskLang.\n";
} elseif ($speedup > 1.5) {
    echo "   ✅ Good JIT performance. Consider using for production.\n";
} elseif ($speedup > 1.1) {
    echo "   ⚠️  Moderate JIT improvement. Profile your specific workload.\n";
} else {
    echo "   ⚠️  Limited JIT benefit. JIT works best with computational code.\n";
}

echo "   🎯 Use @cache() operators for expensive operations\n";
echo "   🎯 Use @optimize() for adaptive performance tuning\n";
echo "   🎯 Consider increasing JIT buffer size for large applications\n";
echo "   🎯 Monitor JIT compilation patterns in production\n\n";

echo "🎉 Benchmark completed successfully!\n";
echo "    TuskLang + PHP 8 JIT = " . round($speedup, 1) . "x faster! 🚀\n"; 
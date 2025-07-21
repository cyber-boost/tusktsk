<?php
/**
 * Test Implementation for Agent A8 Goals
 * ======================================
 * Tests the implementation of goals g1.1, g1.2, and g1.3
 */

// Include the implementation
require_once __DIR__ . '/implementation.php';

use TuskLang\AgentA8\AgentA8;

echo "🥜 TuskLang Agent A8 - Goal Implementation Test\n";
echo "================================================\n\n";

// Initialize agent
$agent = new AgentA8();

// Test individual goals
echo "Testing Goal 1.1: Enhanced PHP Operator Support\n";
echo "------------------------------------------------\n";
$goal1_1_result = $agent->executeGoal1_1();
echo "Result: " . ($goal1_1_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal1_1_result['success']) {
    echo "Operators registered: " . $goal1_1_result['operators_registered'] . "\n";
    echo "Execution time: " . round($goal1_1_result['execution_time'], 2) . "ms\n";
} else {
    echo "Error: " . $goal1_1_result['error'] . "\n";
}
echo "\n";

echo "Testing Goal 1.2: Advanced Error Handling and Logging\n";
echo "-----------------------------------------------------\n";
$goal1_2_result = $agent->executeGoal1_2();
echo "Result: " . ($goal1_2_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal1_2_result['success']) {
    echo "Log file: " . $goal1_2_result['log_file'] . "\n";
    echo "Log stats: " . json_encode($goal1_2_result['log_stats']) . "\n";
    echo "Execution time: " . round($goal1_2_result['execution_time'], 2) . "ms\n";
} else {
    echo "Error: " . $goal1_2_result['error'] . "\n";
}
echo "\n";

echo "Testing Goal 1.3: Performance Optimization Features\n";
echo "--------------------------------------------------\n";
$goal1_3_result = $agent->executeGoal1_3();
echo "Result: " . ($goal1_3_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal1_3_result['success']) {
    echo "Cache test: " . ($goal1_3_result['cache_test'] ? 'PASSED' : 'FAILED') . "\n";
    echo "Operation time: " . round($goal1_3_result['operation_time'], 2) . "ms\n";
    echo "Execution time: " . round($goal1_3_result['execution_time'], 2) . "ms\n";
} else {
    echo "Error: " . $goal1_3_result['error'] . "\n";
}
echo "\n";

// Test all goals together
echo "Testing All Goals Together\n";
echo "--------------------------\n";
$all_results = $agent->executeAllGoals();
echo "Overall Result: " . ($all_results['success'] ? 'SUCCESS' : 'FAILED') . "\n";
echo "Agent ID: " . $all_results['agent_id'] . "\n";
echo "Language: " . $all_results['language'] . "\n";
echo "Timestamp: " . $all_results['timestamp'] . "\n";

// Display final statistics
if (isset($all_results['final_stats'])) {
    echo "\nFinal Statistics:\n";
    echo "-----------------\n";
    $stats = $all_results['final_stats'];
    echo "Total execution time: " . round($stats['total_execution_time'], 4) . "s\n";
    echo "Current memory usage: " . $stats['current_memory_usage'] . "\n";
    echo "Peak memory usage: " . $stats['peak_memory_usage'] . "\n";
    echo "Cache entries: " . $stats['cache_entries'] . "\n";
}

// Display agent information
echo "\nAgent Information:\n";
echo "------------------\n";
$info = $agent->getInfo();
echo "Agent ID: " . $info['agent_id'] . "\n";
echo "Language: " . $info['language'] . "\n";
echo "Goals completed: " . implode(', ', $info['goals_completed']) . "\n";
echo "Features:\n";
foreach ($info['features'] as $feature) {
    echo "  - " . $feature . "\n";
}

echo "\n✅ Test completed successfully!\n"; 
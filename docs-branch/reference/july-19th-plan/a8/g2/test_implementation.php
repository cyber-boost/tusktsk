<?php
/**
 * Test Implementation for Agent A8 Goal G2
 * ========================================
 * Tests the implementation of goals g2.1, g2.2, and g2.3
 */

// Include the implementation
require_once __DIR__ . '/implementation.php';

use TuskLang\AgentA8\G2\AgentA8G2;

echo "🥜 TuskLang Agent A8 Goal G2 - Implementation Test\n";
echo "==================================================\n\n";

// Initialize agent
$agent = new AgentA8G2();

// Test individual goals
echo "Testing Goal 2.1: Advanced PHP Framework Integration\n";
echo "----------------------------------------------------\n";
$goal2_1_result = $agent->executeGoal2_1();
echo "Result: " . ($goal2_1_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal2_1_result['success']) {
    echo "Detected frameworks: " . count($goal2_1_result['detected_frameworks']) . "\n";
    echo "Configurations loaded: " . count($goal2_1_result['configurations']) . "\n";
    echo "Middleware registered: " . $goal2_1_result['middleware_count'] . "\n";
} else {
    echo "Error: " . $goal2_1_result['error'] . "\n";
}
echo "\n";

echo "Testing Goal 2.2: Database Connectivity and ORM Features\n";
echo "--------------------------------------------------------\n";
$goal2_2_result = $agent->executeGoal2_2();
echo "Result: " . ($goal2_2_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal2_2_result['success']) {
    echo "Database connection: " . ($goal2_2_result['connection_created'] ? 'CREATED' : 'FAILED') . "\n";
    echo "Sample query executed: " . ($goal2_2_result['sample_query_result'] ? 'SUCCESS' : 'FAILED') . "\n";
    echo "Model templates created: 2\n";
    echo "Query statistics: " . json_encode($goal2_2_result['query_stats']) . "\n";
} else {
    echo "Error: " . $goal2_2_result['error'] . "\n";
}
echo "\n";

echo "Testing Goal 2.3: API Development and RESTful Services\n";
echo "-----------------------------------------------------\n";
$goal2_3_result = $agent->executeGoal2_3();
echo "Result: " . ($goal2_3_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal2_3_result['success']) {
    echo "Routes registered: " . $goal2_3_result['routes_registered'] . "\n";
    echo "Sample responses: " . count($goal2_3_result['sample_responses']) . "\n";
    echo "API documentation generated: " . count($goal2_3_result['api_documentation']) . " endpoints\n";
    echo "Request statistics: " . json_encode($goal2_3_result['request_stats']) . "\n";
} else {
    echo "Error: " . $goal2_3_result['error'] . "\n";
}
echo "\n";

// Test all goals together
echo "Testing All Goals Together\n";
echo "--------------------------\n";
$all_results = $agent->executeAllGoals();
echo "Overall Result: " . ($all_results['success'] ? 'SUCCESS' : 'FAILED') . "\n";
echo "Agent ID: " . $all_results['agent_id'] . "\n";
echo "Language: " . $all_results['language'] . "\n";
echo "Goal ID: " . $all_results['goal_id'] . "\n";
echo "Timestamp: " . $all_results['timestamp'] . "\n";

// Display agent information
echo "\nAgent Information:\n";
echo "------------------\n";
$info = $agent->getInfo();
echo "Agent ID: " . $info['agent_id'] . "\n";
echo "Language: " . $info['language'] . "\n";
echo "Goal ID: " . $info['goal_id'] . "\n";
echo "Goals completed: " . implode(', ', $info['goals_completed']) . "\n";
echo "Features:\n";
foreach ($info['features'] as $feature) {
    echo "  - " . $feature . "\n";
}

echo "\n✅ Test completed successfully!\n"; 
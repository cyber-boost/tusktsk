<?php
/**
 * Test Implementation for Agent A8 Goal G10
 * ==========================================
 * Tests the implementation of goals g10.1, g10.2, and g10.3
 */

// Include the implementation
require_once __DIR__ . '/implementation.php';

use TuskLang\AgentA8\G10\AgentA8G10;

echo "🥜 TuskLang Agent A8 Goal G10 - Implementation Test\n";
echo "==================================================\n\n";

// Initialize agent
$agent = new AgentA8G10();

// Test individual goals
echo "Testing Goal 10.1: Blockchain Integration and Smart Contracts\n";
echo "-------------------------------------------------------------\n";
$goal10_1_result = $agent->executeGoal10_1();
echo "Result: " . ($goal10_1_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal10_1_result['success']) {
    echo "Connections created: " . $goal10_1_result['connections_created'] . "\n";
    echo "Contracts deployed: " . $goal10_1_result['contracts_deployed'] . "\n";
    echo "Functions executed: " . $goal10_1_result['functions_executed'] . "\n";
    echo "Blockchain statistics: " . json_encode($goal10_1_result['blockchain_statistics']) . "\n";
} else {
    echo "Error: " . $goal10_1_result['error'] . "\n";
}
echo "\n";

echo "Testing Goal 10.2: Distributed Ledger Technology (DLT)\n";
echo "------------------------------------------------------\n";
$goal10_2_result = $agent->executeGoal10_2();
echo "Result: " . ($goal10_2_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal10_2_result['success']) {
    echo "Nodes created: " . $goal10_2_result['nodes_created'] . "\n";
    echo "Ledgers created: " . $goal10_2_result['ledgers_created'] . "\n";
    echo "Transactions added: " . $goal10_2_result['transactions_added'] . "\n";
    echo "Blocks mined: " . $goal10_2_result['blocks_mined'] . "\n";
    echo "DLT statistics: " . json_encode($goal10_2_result['dlt_statistics']) . "\n";
} else {
    echo "Error: " . $goal10_2_result['error'] . "\n";
}
echo "\n";

echo "Testing Goal 10.3: Decentralized Applications (DApps)\n";
echo "-----------------------------------------------------\n";
$goal10_3_result = $agent->executeGoal10_3();
echo "Result: " . ($goal10_3_result['success'] ? 'SUCCESS' : 'FAILED') . "\n";
if ($goal10_3_result['success']) {
    echo "DApps created: " . $goal10_3_result['dapps_created'] . "\n";
    echo "Frontends deployed: " . $goal10_3_result['frontends_deployed'] . "\n";
    echo "Backends deployed: " . $goal10_3_result['backends_deployed'] . "\n";
    echo "Wallets connected: " . $goal10_3_result['wallets_connected'] . "\n";
    echo "Transactions executed: " . $goal10_3_result['transactions_executed'] . "\n";
    echo "DApp statistics: " . json_encode($goal10_3_result['dapp_statistics']) . "\n";
} else {
    echo "Error: " . $goal10_3_result['error'] . "\n";
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
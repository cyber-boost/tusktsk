<?php
/**
 * Simple Test for Agent A8
 */

require_once 'implementation.php';

use TuskLang\AgentA8\AgentA8;

echo "Starting Agent A8 test...\n";

try {
    $agent = new AgentA8();
    echo "Agent created successfully\n";
    
    $info = $agent->getInfo();
    echo "Agent ID: " . $info['agent_id'] . "\n";
    echo "Language: " . $info['language'] . "\n";
    echo "Goals: " . implode(', ', $info['goals_completed']) . "\n";
    
    echo "Testing individual goals...\n";
    
    // Test Goal 1.1
    $result1 = $agent->executeGoal1_1();
    echo "Goal 1.1: " . ($result1['success'] ? 'SUCCESS' : 'FAILED') . "\n";
    
    // Test Goal 1.2
    $result2 = $agent->executeGoal1_2();
    echo "Goal 1.2: " . ($result2['success'] ? 'SUCCESS' : 'FAILED') . "\n";
    
    // Test Goal 1.3
    $result3 = $agent->executeGoal1_3();
    echo "Goal 1.3: " . ($result3['success'] ? 'SUCCESS' : 'FAILED') . "\n";
    
    echo "All tests completed!\n";
    
} catch (Exception $e) {
    echo "Error: " . $e->getMessage() . "\n";
    echo "Stack trace: " . $e->getTraceAsString() . "\n";
} 
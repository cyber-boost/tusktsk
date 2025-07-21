<?php
require_once 'implementation.php';
use TuskLang\AgentA8\G23\AgentA8G23;

$agent = new AgentA8G23();
$result = $agent->executeAllGoals();

if ($result['success']) {
    echo "✅ G23 Tests Passed - Advanced Materials Complete!\n";
    exit(0);
} else {
    echo "❌ G23 Tests Failed\n";
    exit(1);
} 
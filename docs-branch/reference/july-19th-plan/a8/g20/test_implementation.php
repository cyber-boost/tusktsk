<?php
require_once 'implementation.php';
use TuskLang\AgentA8\G20\AgentA8G20;

$agent = new AgentA8G20();
$result = $agent->executeAllGoals();

if ($result['success']) {
    echo "✅ G20 Tests Passed - Digital Twins & Simulation Complete!\n";
    exit(0);
} else {
    echo "❌ G20 Tests Failed\n";
    exit(1);
} 
<?php
require_once 'implementation.php';
use TuskLang\AgentA8\G22\AgentA8G22;

$agent = new AgentA8G22();
$result = $agent->executeAllGoals();

if ($result['success']) {
    echo "✅ G22 Tests Passed - Space Technology Complete!\n";
    exit(0);
} else {
    echo "❌ G22 Tests Failed\n";
    exit(1);
} 
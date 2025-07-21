<?php
require_once 'implementation.php';
use TuskLang\AgentA8\G21\AgentA8G21;

$agent = new AgentA8G21();
$result = $agent->executeAllGoals();

if ($result['success']) {
    echo "✅ G21 Tests Passed - Bioinformatics Complete!\n";
    exit(0);
} else {
    echo "❌ G21 Tests Failed\n";
    exit(1);
} 
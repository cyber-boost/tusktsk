<?php
require_once 'implementation.php';
use TuskLang\AgentA8\G24\AgentA8G24;

$agent = new AgentA8G24();
$result = $agent->executeAllGoals();

if ($result['success']) {
    echo "✅ G24 Tests Passed - Climate Science Complete!\n";
    exit(0);
} else {
    echo "❌ G24 Tests Failed\n";
    exit(1);
} 
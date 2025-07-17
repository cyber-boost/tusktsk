<?php
/**
 * Direct API Test Script
 */

// Simulate request environment
$_SERVER['REQUEST_METHOD'] = 'GET';
$_SERVER['REQUEST_URI'] = '/echo';
$_GET = ['format' => 'json'];
$_POST = [];
$_SERVER['REMOTE_ADDR'] = '127.0.0.1';
$_SERVER['HTTP_USER_AGENT'] = 'FUJSEN-Test/1.0';

echo "🔥 FUJSEN API Direct Test\n";
echo "========================\n\n";

try {
    // Test 1: Echo endpoint
    echo "Test 1: Echo Endpoint\n";
    echo "--------------------\n";
    
    require_once 'api-router.php';
    
} catch (Exception $e) {
    echo "Error: " . $e->getMessage() . "\n";
    echo "Stack trace:\n" . $e->getTraceAsString() . "\n";
} 
<?php
/**
 * 🥜 TuskLang PHP Agent A8 - Goal G13 Test Implementation
 * ========================================================
 * Agent: a8
 * Goals: g13.1, g13.2, g13.3
 * Language: PHP
 * 
 * This file tests the three goals for the PHP agent g13:
 * - g13.1: Quantum Computing Algorithms and Simulation
 * - g13.2: Post-Quantum Cryptography and Security
 * - g13.3: Quantum-Resistant Communication Protocols
 */

require_once 'implementation.php';

use TuskLang\AgentA8\G13\AgentA8G13;
use TuskLang\AgentA8\G13\QuantumComputingManager;
use TuskLang\AgentA8\G13\PostQuantumCryptoManager;
use TuskLang\AgentA8\G13\QuantumResistantProtocolManager;

class AgentA8G13Test
{
    private AgentA8G13 $agent;
    private array $testResults = [];
    
    public function __construct()
    {
        $this->agent = new AgentA8G13();
    }
    
    public function runAllTests(): array
    {
        echo "🧪 Starting Agent A8 G13 Tests...\n";
        echo "================================\n\n";
        
        $this->testGoal13_1();
        $this->testGoal13_2();
        $this->testGoal13_3();
        $this->testIntegration();
        
        return $this->testResults;
    }
    
    private function testGoal13_1(): void
    {
        echo "⚛️  Testing Goal 13.1: Quantum Computing Algorithms and Simulation\n";
        echo "----------------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal13_1();
            
            $this->assert($result['success'], 'Goal 13.1 execution should succeed');
            $this->assert($result['circuits_created'] >= 2, 'Should create at least 2 quantum circuits');
            $this->assert($result['algorithms_implemented'] >= 2, 'Should implement at least 2 quantum algorithms');
            $this->assert($result['simulations_performed'] >= 2, 'Should perform at least 2 simulations');
            $this->assert(isset($result['quantum_statistics']), 'Should return quantum statistics');
            
            echo "✅ Goal 13.1 Tests Passed\n";
            $this->testResults['goal_13_1'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 13.1 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_13_1'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal13_2(): void
    {
        echo "🔐 Testing Goal 13.2: Post-Quantum Cryptography and Security\n";
        echo "-----------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal13_2();
            
            $this->assert($result['success'], 'Goal 13.2 execution should succeed');
            $this->assert($result['key_pairs_generated'] >= 3, 'Should generate at least 3 key pairs');
            $this->assert($result['encryptions_performed'] >= 1, 'Should perform at least 1 encryption');
            $this->assert($result['decryptions_performed'] >= 1, 'Should perform at least 1 decryption');
            $this->assert($result['signatures_created'] >= 1, 'Should create at least 1 signature');
            $this->assert($result['verifications_performed'] >= 1, 'Should perform at least 1 verification');
            $this->assert(isset($result['crypto_statistics']), 'Should return crypto statistics');
            
            echo "✅ Goal 13.2 Tests Passed\n";
            $this->testResults['goal_13_2'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 13.2 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_13_2'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testGoal13_3(): void
    {
        echo "🌐 Testing Goal 13.3: Quantum-Resistant Communication Protocols\n";
        echo "-------------------------------------------------------------\n";
        
        try {
            $result = $this->agent->executeGoal13_3();
            
            $this->assert($result['success'], 'Goal 13.3 execution should succeed');
            $this->assert($result['sessions_created'] >= 3, 'Should create at least 3 sessions');
            $this->assert($result['key_exchanges_performed'] >= 2, 'Should perform at least 2 key exchanges');
            $this->assert($result['messages_sent'] >= 2, 'Should send at least 2 messages');
            $this->assert($result['messages_received'] >= 1, 'Should receive at least 1 message');
            $this->assert(isset($result['protocol_statistics']), 'Should return protocol statistics');
            
            echo "✅ Goal 13.3 Tests Passed\n";
            $this->testResults['goal_13_3'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Goal 13.3 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_13_3'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function testIntegration(): void
    {
        echo "🔗 Testing Integration: All Goals Together\n";
        echo "------------------------------------------\n";
        
        try {
            $result = $this->agent->executeAllGoals();
            
            $this->assert($result['success'], 'Integration execution should succeed');
            $this->assert($result['agent_id'] === 'a8', 'Agent ID should be a8');
            $this->assert($result['language'] === 'PHP', 'Language should be PHP');
            $this->assert($result['goal_id'] === 'g13', 'Goal ID should be g13');
            $this->assert(isset($result['results']['goal_13_1']), 'Should include goal 13.1 results');
            $this->assert(isset($result['results']['goal_13_2']), 'Should include goal 13.2 results');
            $this->assert(isset($result['results']['goal_13_3']), 'Should include goal 13.3 results');
            
            echo "✅ Integration Tests Passed\n";
            $this->testResults['integration'] = ['status' => 'passed', 'result' => $result];
            
        } catch (Exception $e) {
            echo "❌ Integration Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['integration'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    public function testIndividualClasses(): void
    {
        echo "🏗️  Testing Individual Classes\n";
        echo "-----------------------------\n";
        
        // Test Quantum Computing Manager
        try {
            $quantumManager = new QuantumComputingManager();
            $circuit = $quantumManager->createQuantumCircuit('test_circuit', 2);
            $this->assert($circuit['success'], 'Quantum circuit creation should succeed');
            
            $gate = $quantumManager->addGate('test_circuit', 'H', ['qubits' => [0]]);
            $this->assert($gate['success'], 'Quantum gate addition should succeed');
            
            $simulation = $quantumManager->simulateCircuit('test_circuit', 100);
            $this->assert($simulation['success'], 'Quantum simulation should succeed');
            
            $algorithm = $quantumManager->implementAlgorithm('grover', ['num_qubits' => 2]);
            $this->assert($algorithm['success'], 'Quantum algorithm implementation should succeed');
            
            $stats = $quantumManager->getQuantumStats();
            $this->assert(isset($stats['total_circuits']), 'Should return quantum statistics');
            
            echo "✅ Quantum Computing Manager Tests Passed\n";
            $this->testResults['quantum_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Quantum Computing Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['quantum_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test Post-Quantum Crypto Manager
        try {
            $cryptoManager = new PostQuantumCryptoManager();
            $keyPair = $cryptoManager->generateKeyPair('lattice_based');
            $this->assert($keyPair['success'], 'Key pair generation should succeed');
            
            $message = "Test message";
            $encryption = $cryptoManager->encrypt($keyPair['key_pair']['id'], $message);
            $this->assert($encryption['success'], 'Encryption should succeed');
            
            $decryption = $cryptoManager->decrypt($keyPair['key_pair']['id'], $encryption['encryption']['ciphertext']);
            $this->assert($decryption['success'], 'Decryption should succeed');
            
            $signature = $cryptoManager->sign($keyPair['key_pair']['id'], $message);
            $this->assert($signature['success'], 'Signature creation should succeed');
            
            $verification = $cryptoManager->verify($keyPair['key_pair']['id'], $message, $signature['signature']['signature']);
            $this->assert($verification['success'], 'Signature verification should succeed');
            
            $stats = $cryptoManager->getCryptoStats();
            $this->assert(isset($stats['total_key_pairs']), 'Should return crypto statistics');
            
            echo "✅ Post-Quantum Crypto Manager Tests Passed\n";
            $this->testResults['crypto_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Post-Quantum Crypto Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['crypto_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        // Test Quantum-Resistant Protocol Manager
        try {
            $protocolManager = new QuantumResistantProtocolManager();
            $session = $protocolManager->createSession('quantum_key_distribution', ['participants' => ['Alice', 'Bob']]);
            $this->assert($session['success'], 'Session creation should succeed');
            
            $exchange = $protocolManager->performKeyExchange($session['session']['id'], 'lattice_based_kem');
            $this->assert($exchange['success'], 'Key exchange should succeed');
            
            $message = "Test quantum message";
            $sentMessage = $protocolManager->sendMessage($session['session']['id'], $message);
            $this->assert($sentMessage['success'], 'Message sending should succeed');
            
            $receivedMessage = $protocolManager->receiveMessage($session['session']['id'], $sentMessage['message']['encrypted_message']);
            $this->assert($receivedMessage['success'], 'Message receiving should succeed');
            
            $stats = $protocolManager->getProtocolStats();
            $this->assert(isset($stats['total_sessions']), 'Should return protocol statistics');
            
            echo "✅ Quantum-Resistant Protocol Manager Tests Passed\n";
            $this->testResults['protocol_manager'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Quantum-Resistant Protocol Manager Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['protocol_manager'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
        
        echo "\n";
    }
    
    private function assert(bool $condition, string $message): void
    {
        if (!$condition) {
            throw new Exception("Assertion failed: $message");
        }
    }
    
    public function getTestSummary(): array
    {
        $passed = 0;
        $failed = 0;
        
        foreach ($this->testResults as $test) {
            if ($test['status'] === 'passed') {
                $passed++;
            } else {
                $failed++;
            }
        }
        
        return [
            'total_tests' => count($this->testResults),
            'passed' => $passed,
            'failed' => $failed,
            'success_rate' => $passed / count($this->testResults) * 100,
            'results' => $this->testResults
        ];
    }
}

// Run tests if this file is executed directly
if (basename(__FILE__) === basename($_SERVER['SCRIPT_NAME'])) {
    $test = new AgentA8G13Test();
    $test->runAllTests();
    $test->testIndividualClasses();
    
    $summary = $test->getTestSummary();
    
    echo "📋 Test Summary\n";
    echo "===============\n";
    echo "Total Tests: {$summary['total_tests']}\n";
    echo "Passed: {$summary['passed']}\n";
    echo "Failed: {$summary['failed']}\n";
    echo "Success Rate: {$summary['success_rate']}%\n";
    
    if ($summary['failed'] === 0) {
        echo "\n🎉 All tests passed! Agent A8 G13 is working correctly.\n";
        exit(0);
    } else {
        echo "\n⚠️  Some tests failed. Please review the implementation.\n";
        exit(1);
    }
} 
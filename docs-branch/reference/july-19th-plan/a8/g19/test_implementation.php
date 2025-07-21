<?php
require_once 'implementation.php';

use TuskLang\AgentA8\G19\AgentA8G19;

class AgentA8G19Test
{
    private AgentA8G19 $agent;
    private array $testResults = [];
    
    public function __construct()
    {
        $this->agent = new AgentA8G19();
    }
    
    public function runAllTests(): array
    {
        echo "🧪 Starting Agent A8 G19 Tests...\n";
        
        $this->testGoal19_1();
        $this->testGoal19_2();
        $this->testGoal19_3();
        $this->testIntegration();
        
        return $this->testResults;
    }
    
    private function testGoal19_1(): void
    {
        echo "🖥️  Testing Goal 19.1: User Interface Systems\n";
        
        try {
            $result = $this->agent->executeGoal19_1();
            
            $this->assert($result['success'], 'Goal 19.1 should succeed');
            $this->assert($result['interfaces_created'] >= 2, 'Should create interfaces');
            $this->assert($result['components_added'] >= 3, 'Should add components');
            
            echo "✅ Goal 19.1 Tests Passed\n";
            $this->testResults['goal_19_1'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Goal 19.1 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_19_1'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
    }
    
    private function testGoal19_2(): void
    {
        echo "♿ Testing Goal 19.2: Accessibility Features\n";
        
        try {
            $result = $this->agent->executeGoal19_2();
            
            $this->assert($result['success'], 'Goal 19.2 should succeed');
            $this->assert($result['features_enabled'] >= 3, 'Should enable features');
            $this->assert($result['audits_performed'] >= 2, 'Should perform audits');
            
            echo "✅ Goal 19.2 Tests Passed\n";
            $this->testResults['goal_19_2'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Goal 19.2 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_19_2'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
    }
    
    private function testGoal19_3(): void
    {
        echo "📊 Testing Goal 19.3: Interaction Analytics\n";
        
        try {
            $result = $this->agent->executeGoal19_3();
            
            $this->assert($result['success'], 'Goal 19.3 should succeed');
            $this->assert($result['sessions_started'] >= 2, 'Should start sessions');
            $this->assert($result['events_tracked'] >= 3, 'Should track events');
            
            echo "✅ Goal 19.3 Tests Passed\n";
            $this->testResults['goal_19_3'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Goal 19.3 Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['goal_19_3'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
    }
    
    private function testIntegration(): void
    {
        echo "🔗 Testing Integration\n";
        
        try {
            $result = $this->agent->executeAllGoals();
            
            $this->assert($result['success'], 'Integration should succeed');
            $this->assert($result['agent_id'] === 'a8', 'Agent ID should be a8');
            $this->assert($result['goal_id'] === 'g19', 'Goal ID should be g19');
            
            echo "✅ Integration Tests Passed\n";
            $this->testResults['integration'] = ['status' => 'passed'];
            
        } catch (Exception $e) {
            echo "❌ Integration Tests Failed: " . $e->getMessage() . "\n";
            $this->testResults['integration'] = ['status' => 'failed', 'error' => $e->getMessage()];
        }
    }
    
    private function assert(bool $condition, string $message): void
    {
        if (!$condition) {
            throw new Exception("Assertion failed: $message");
        }
    }
    
    public function getTestSummary(): array
    {
        $passed = count(array_filter($this->testResults, fn($r) => $r['status'] === 'passed'));
        $failed = count($this->testResults) - $passed;
        
        return [
            'total_tests' => count($this->testResults),
            'passed' => $passed,
            'failed' => $failed,
            'success_rate' => $passed / count($this->testResults) * 100
        ];
    }
}

if (basename(__FILE__) === basename($_SERVER['SCRIPT_NAME'])) {
    $test = new AgentA8G19Test();
    $test->runAllTests();
    
    $summary = $test->getTestSummary();
    
    echo "\n📋 Test Summary: {$summary['passed']}/{$summary['total_tests']} passed ({$summary['success_rate']}%)\n";
    
    if ($summary['failed'] === 0) {
        echo "🎉 All tests passed! Agent A8 G19 completed successfully.\n";
        exit(0);
    } else {
        exit(1);
    }
} 
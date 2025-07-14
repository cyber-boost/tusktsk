<?php
/**
 * 🌐 FUJSEN Web Endpoints Test Suite
 * =================================
 * Tests .tsk files as HTTP endpoints
 * 
 * FUJSEN Sprint - Hour 4 Testing
 */

require_once __DIR__ . '/autoload.php';

use TuskPHP\Utils\TuskLangWebHandler;

class FujsenWebEndpointTester
{
    private $webHandler;
    private $testResults = [];
    
    public function __construct()
    {
        $this->webHandler = TuskLangWebHandler::getInstance();
    }
    
    /**
     * Run all web endpoint tests
     */
    public function runAllTests(): void
    {
        echo "🚀 FUJSEN Web Endpoints Test Suite\n";
        echo "==================================\n\n";
        
        // Test 1: Basic API endpoint parsing
        $this->testApiDirectiveParsing();
        
        // Test 2: Request data handling
        $this->testRequestDataHandling();
        
        // Test 3: JSON response generation
        $this->testJsonResponseGeneration();
        
        // Test 4: HTML response generation
        $this->testHtmlResponseGeneration();
        
        // Test 5: Web functions integration
        $this->testWebFunctionsIntegration();
        
        // Test 6: Error handling
        $this->testErrorHandling();
        
        // Print summary
        $this->printTestSummary();
    }
    
    /**
     * Test #!api directive parsing
     */
    private function testApiDirectiveParsing(): void
    {
        echo "🔍 Test 1: API Directive Parsing\n";
        echo "--------------------------------\n";
        
        // Test valid #!api directive
        $content1 = "#!api\ntest: \"value\"";
        $isApi1 = $this->isApiEndpoint($content1);
        $this->recordTest("Valid #!api directive", $isApi1, true);
        
        // Test invalid directive
        $content2 = "# Not an API\ntest: \"value\"";
        $isApi2 = $this->isApiEndpoint($content2);
        $this->recordTest("Invalid directive", $isApi2, false);
        
        // Test with comments before
        $content3 = "# Comment\n#!api\ntest: \"value\"";
        $isApi3 = $this->isApiEndpoint($content3);
        $this->recordTest("API directive with comments", $isApi3, true);
        
        echo "\n";
    }
    
    /**
     * Test request data handling
     */
    private function testRequestDataHandling(): void
    {
        echo "📡 Test 2: Request Data Handling\n";
        echo "-------------------------------\n";
        
        // Simulate request data
        $_SERVER['REQUEST_METHOD'] = 'POST';
        $_SERVER['REQUEST_URI'] = '/api/test?param=value';
        $_GET = ['param' => 'value'];
        $_POST = ['body_param' => 'body_value'];
        $_SERVER['REMOTE_ADDR'] = '127.0.0.1';
        $_SERVER['HTTP_USER_AGENT'] = 'FUJSEN-Test/1.0';
        
        // Create new handler instance to pick up new request data
        $handler = new TuskLangWebHandler();
        $requestData = $handler->getRequestData();
        
        $this->recordTest("Request method", $requestData['method'], 'POST');
        $this->recordTest("Request URI", $requestData['uri'], '/api/test?param=value');
        $this->recordTest("Query params", $requestData['query']['param'], 'value');
        $this->recordTest("Request IP", $requestData['ip'], '127.0.0.1');
        $this->recordTest("User agent", $requestData['user_agent'], 'FUJSEN-Test/1.0');
        
        echo "\n";
    }
    
    /**
     * Test JSON response generation
     */
    private function testJsonResponseGeneration(): void
    {
        echo "📄 Test 3: JSON Response Generation\n";
        echo "----------------------------------\n";
        
        $testData = ['message' => 'Hello FUJSEN!', 'status' => 'success'];
        
        // Test basic JSON response
        $jsonResponse = $this->webHandler->handleJson($testData);
        $this->recordTest("JSON response structure", isset($jsonResponse['__json_response']), true);
        $this->recordTest("JSON response data", $jsonResponse['data'], $testData);
        $this->recordTest("JSON response status", $jsonResponse['status'], 200);
        
        // Test JSON response with custom status
        $jsonResponse201 = $this->webHandler->handleJson($testData, 201);
        $this->recordTest("JSON custom status", $jsonResponse201['status'], 201);
        
        echo "\n";
    }
    
    /**
     * Test HTML response generation
     */
    private function testHtmlResponseGeneration(): void
    {
        echo "🌐 Test 4: HTML Response Generation\n";
        echo "----------------------------------\n";
        
        $template = "<h1>{{title}}</h1><p>{{content}}</p>";
        $data = ['title' => 'FUJSEN Test', 'content' => 'Hello World!'];
        
        $htmlResponse = $this->webHandler->handleRender($template, $data);
        $this->recordTest("HTML response structure", isset($htmlResponse['__html_response']), true);
        $this->recordTest("HTML template", $htmlResponse['template'], $template);
        $this->recordTest("HTML data", $htmlResponse['data'], $data);
        
        echo "\n";
    }
    
    /**
     * Test web functions integration
     */
    private function testWebFunctionsIntegration(): void
    {
        echo "⚙️ Test 5: Web Functions Integration\n";
        echo "-----------------------------------\n";
        
        try {
            // Create a simple .tsk content with web functions
            $tskContent = "#!api\n" .
                         "method: @request.method\n" .
                         "response: @json({\"test\": \"success\", \"method\": method})";
            
            // Test web parser
            $parser = new TuskLangWebParser($tskContent, $this->webHandler);
            $result = $parser->parse();
            
            $this->recordTest("Web parser creation", is_object($parser), true);
            $this->recordTest("Web functions parsing", isset($result['response']), true);
            
            // Test @request access
            $requestContent = "#!api\ntest: @request";
            $requestParser = new TuskLangWebParser($requestContent, $this->webHandler);
            $requestResult = $requestParser->parse();
            
            $this->recordTest("@request access", is_array($requestResult['test']), true);
            $this->recordTest("@request.method", $requestResult['test']['method'], 'POST');
            
        } catch (Exception $e) {
            $this->recordTest("Web functions integration", false, true, $e->getMessage());
        }
        
        echo "\n";
    }
    
    /**
     * Test error handling
     */
    private function testErrorHandling(): void
    {
        echo "🚨 Test 6: Error Handling\n";
        echo "-------------------------\n";
        
        // Test non-existent file
        ob_start();
        $this->webHandler->handleRequest('/nonexistent/file.tsk');
        $output = ob_get_clean();
        
        $this->recordTest("Non-existent file handling", !empty($output), true);
        
        // Test non-API file
        $nonApiFile = __DIR__ . '/test-non-api.tsk';
        file_put_contents($nonApiFile, "# Not an API\ntest: value");
        
        ob_start();
        $this->webHandler->handleRequest($nonApiFile);
        $output = ob_get_clean();
        
        $this->recordTest("Non-API file handling", !empty($output), true);
        
        // Clean up
        unlink($nonApiFile);
        
        echo "\n";
    }
    
    /**
     * Check if content has #!api directive
     */
    private function isApiEndpoint(string $content): bool
    {
        $lines = explode("\n", $content);
        foreach ($lines as $line) {
            $line = trim($line);
            if ($line === '#!api' || $line === '#!/api' || $line === '#! api') {
                return true;
            }
            if (!empty($line) && !str_starts_with($line, '#')) {
                break;
            }
        }
        return false;
    }
    
    /**
     * Record test result
     */
    private function recordTest(string $name, mixed $actual, mixed $expected, string $error = null): void
    {
        $passed = ($actual === $expected);
        $status = $passed ? "✅ PASS" : "❌ FAIL";
        
        echo "  $status: $name\n";
        
        if (!$passed) {
            echo "    Expected: " . json_encode($expected) . "\n";
            echo "    Actual: " . json_encode($actual) . "\n";
            if ($error) {
                echo "    Error: $error\n";
            }
        }
        
        $this->testResults[] = [
            'name' => $name,
            'passed' => $passed,
            'expected' => $expected,
            'actual' => $actual,
            'error' => $error
        ];
    }
    
    /**
     * Print test summary
     */
    private function printTestSummary(): void
    {
        $total = count($this->testResults);
        $passed = array_filter($this->testResults, fn($test) => $test['passed']);
        $passedCount = count($passed);
        $failedCount = $total - $passedCount;
        
        echo "📊 Test Summary\n";
        echo "==============\n";
        echo "Total Tests: $total\n";
        echo "Passed: $passedCount ✅\n";
        echo "Failed: $failedCount ❌\n";
        echo "Success Rate: " . round(($passedCount / $total) * 100, 2) . "%\n\n";
        
        if ($failedCount > 0) {
            echo "Failed Tests:\n";
            foreach ($this->testResults as $test) {
                if (!$test['passed']) {
                    echo "  - {$test['name']}\n";
                }
            }
        }
        
        echo ($failedCount === 0 ? "🎉 ALL TESTS PASSED!" : "⚠️ SOME TESTS FAILED") . "\n";
    }
}

// ========================================
// MAIN EXECUTION
// ========================================

if (basename($_SERVER['SCRIPT_NAME']) === 'test-web-endpoints.php') {
    $tester = new FujsenWebEndpointTester();
    $tester->runAllTests();
} 
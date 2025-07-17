# Advanced Testing in PHP with TuskLang

## Overview

TuskLang revolutionizes testing by making it configuration-driven, intelligent, and adaptive. This guide covers advanced testing patterns that leverage TuskLang's dynamic capabilities for comprehensive test automation and quality assurance.

## 🎯 Testing Architecture

### Testing Configuration

```ini
# testing-architecture.tsk
[testing_architecture]
strategy = "comprehensive_automation"
framework = "phpunit"
coverage_threshold = 90
parallel_execution = true

[testing_architecture.layers]
unit_tests = {
    enabled = true,
    framework = "phpunit",
    coverage = 95,
    parallel = true
}

integration_tests = {
    enabled = true,
    framework = "phpunit",
    database = "test_db",
    coverage = 85,
    parallel = false
}

api_tests = {
    enabled = true,
    framework = "behat",
    base_url = "http://localhost:8000",
    coverage = 80
}

ui_tests = {
    enabled = true,
    framework = "selenium",
    browser = "chrome",
    headless = true,
    coverage = 70
}

performance_tests = {
    enabled = true,
    framework = "k6",
    scenarios = ["load", "stress", "spike"],
    coverage = 60
}

security_tests = {
    enabled = true,
    framework = "zap",
    scan_types = ["active", "passive"],
    coverage = 75
}

[testing_architecture.environments]
development = {
    database = "dev_db",
    cache = "redis_dev",
    external_services = "mocks"
}

staging = {
    database = "staging_db",
    cache = "redis_staging",
    external_services = "sandbox"
}

production = {
    database = "prod_db",
    cache = "redis_prod",
    external_services = "live"
}

[testing_architecture.reporting]
coverage_reports = true
test_reports = true
performance_reports = true
security_reports = true
ci_integration = true
```

### Testing Manager Implementation

```php
<?php
// TestingManager.php
class TestingManager
{
    private $config;
    private $testRunner;
    private $coverageCollector;
    private $reportGenerator;
    private $ciIntegrator;
    
    public function __construct()
    {
        $this->config = new TuskConfig('testing-architecture.tsk');
        $this->testRunner = new TestRunner();
        $this->coverageCollector = new CoverageCollector();
        $this->reportGenerator = new ReportGenerator();
        $this->ciIntegrator = new CIIntegrator();
        $this->initializeTesting();
    }
    
    private function initializeTesting()
    {
        $strategy = $this->config->get('testing_architecture.strategy');
        
        switch ($strategy) {
            case 'comprehensive_automation':
                $this->initializeComprehensiveAutomation();
                break;
            case 'selective_testing':
                $this->initializeSelectiveTesting();
                break;
            case 'continuous_testing':
                $this->initializeContinuousTesting();
                break;
        }
    }
    
    public function runTestSuite($suite = 'all', $environment = 'development')
    {
        $startTime = microtime(true);
        $results = [];
        
        try {
            // Set up test environment
            $this->setupTestEnvironment($environment);
            
            // Run tests based on suite
            switch ($suite) {
                case 'unit':
                    $results['unit'] = $this->runUnitTests();
                    break;
                case 'integration':
                    $results['integration'] = $this->runIntegrationTests();
                    break;
                case 'api':
                    $results['api'] = $this->runAPITests();
                    break;
                case 'ui':
                    $results['ui'] = $this->runUITests();
                    break;
                case 'performance':
                    $results['performance'] = $this->runPerformanceTests();
                    break;
                case 'security':
                    $results['security'] = $this->runSecurityTests();
                    break;
                case 'all':
                    $results = $this->runAllTests();
                    break;
                default:
                    throw new InvalidArgumentException("Unknown test suite: {$suite}");
            }
            
            // Collect coverage
            $coverage = $this->collectCoverage($results);
            
            // Generate reports
            $reports = $this->generateReports($results, $coverage);
            
            // Integrate with CI
            $this->integrateWithCI($results, $reports);
            
            $duration = (microtime(true) - $startTime) * 1000;
            $this->logTestResults($suite, $results, $duration);
            
            return [
                'suite' => $suite,
                'results' => $results,
                'coverage' => $coverage,
                'reports' => $reports,
                'duration' => $duration
            ];
            
        } catch (Exception $e) {
            $this->handleTestError($suite, $e);
            throw $e;
        } finally {
            // Clean up test environment
            $this->cleanupTestEnvironment($environment);
        }
    }
    
    private function runUnitTests()
    {
        $unitConfig = $this->config->get('testing_architecture.layers.unit_tests');
        
        if (!$unitConfig['enabled']) {
            return ['status' => 'disabled'];
        }
        
        $testRunner = new PHPUnitRunner($unitConfig);
        
        $results = $testRunner->run([
            'parallel' => $unitConfig['parallel'],
            'coverage' => $unitConfig['coverage'],
            'test_path' => 'tests/unit'
        ]);
        
        // Check coverage threshold
        if ($results['coverage'] < $unitConfig['coverage']) {
            throw new TestException("Unit test coverage below threshold: {$results['coverage']}% < {$unitConfig['coverage']}%");
        }
        
        return $results;
    }
    
    private function runIntegrationTests()
    {
        $integrationConfig = $this->config->get('testing_architecture.layers.integration_tests');
        
        if (!$integrationConfig['enabled']) {
            return ['status' => 'disabled'];
        }
        
        $testRunner = new PHPUnitRunner($integrationConfig);
        
        $results = $testRunner->run([
            'parallel' => $integrationConfig['parallel'],
            'coverage' => $integrationConfig['coverage'],
            'test_path' => 'tests/integration',
            'database' => $integrationConfig['database']
        ]);
        
        return $results;
    }
    
    private function runAPITests()
    {
        $apiConfig = $this->config->get('testing_architecture.layers.api_tests');
        
        if (!$apiConfig['enabled']) {
            return ['status' => 'disabled'];
        }
        
        $testRunner = new BehatRunner($apiConfig);
        
        $results = $testRunner->run([
            'base_url' => $apiConfig['base_url'],
            'coverage' => $apiConfig['coverage'],
            'test_path' => 'tests/api'
        ]);
        
        return $results;
    }
    
    private function runUITests()
    {
        $uiConfig = $this->config->get('testing_architecture.layers.ui_tests');
        
        if (!$uiConfig['enabled']) {
            return ['status' => 'disabled'];
        }
        
        $testRunner = new SeleniumRunner($uiConfig);
        
        $results = $testRunner->run([
            'browser' => $uiConfig['browser'],
            'headless' => $uiConfig['headless'],
            'coverage' => $uiConfig['coverage'],
            'test_path' => 'tests/ui'
        ]);
        
        return $results;
    }
    
    private function runPerformanceTests()
    {
        $performanceConfig = $this->config->get('testing_architecture.layers.performance_tests');
        
        if (!$performanceConfig['enabled']) {
            return ['status' => 'disabled'];
        }
        
        $testRunner = new K6Runner($performanceConfig);
        
        $results = [];
        foreach ($performanceConfig['scenarios'] as $scenario) {
            $results[$scenario] = $testRunner->run([
                'scenario' => $scenario,
                'coverage' => $performanceConfig['coverage'],
                'test_path' => "tests/performance/{$scenario}"
            ]);
        }
        
        return $results;
    }
    
    private function runSecurityTests()
    {
        $securityConfig = $this->config->get('testing_architecture.layers.security_tests');
        
        if (!$securityConfig['enabled']) {
            return ['status' => 'disabled'];
        }
        
        $testRunner = new ZAPRunner($securityConfig);
        
        $results = [];
        foreach ($securityConfig['scan_types'] as $scanType) {
            $results[$scanType] = $testRunner->run([
                'scan_type' => $scanType,
                'coverage' => $securityConfig['coverage'],
                'test_path' => "tests/security/{$scanType}"
            ]);
        }
        
        return $results;
    }
    
    private function runAllTests()
    {
        $results = [];
        
        // Run tests in parallel where possible
        $parallelTests = ['unit'];
        $sequentialTests = ['integration', 'api', 'ui', 'performance', 'security'];
        
        // Run parallel tests
        foreach ($parallelTests as $testType) {
            $results[$testType] = $this->{"run" . ucfirst($testType) . "Tests"}();
        }
        
        // Run sequential tests
        foreach ($sequentialTests as $testType) {
            $results[$testType] = $this->{"run" . ucfirst($testType) . "Tests"}();
        }
        
        return $results;
    }
    
    private function collectCoverage($results)
    {
        $coverage = [];
        
        foreach ($results as $testType => $result) {
            if (isset($result['coverage'])) {
                $coverage[$testType] = $result['coverage'];
            }
        }
        
        // Calculate overall coverage
        $coverage['overall'] = $this->calculateOverallCoverage($coverage);
        
        return $coverage;
    }
    
    private function calculateOverallCoverage($coverage)
    {
        $totalCoverage = 0;
        $testCount = 0;
        
        foreach ($coverage as $testType => $coverageValue) {
            if ($testType !== 'overall') {
                $totalCoverage += $coverageValue;
                $testCount++;
            }
        }
        
        return $testCount > 0 ? $totalCoverage / $testCount : 0;
    }
    
    private function generateReports($results, $coverage)
    {
        $reports = [];
        $reportingConfig = $this->config->get('testing_architecture.reporting');
        
        if ($reportingConfig['test_reports']) {
            $reports['test'] = $this->reportGenerator->generateTestReport($results);
        }
        
        if ($reportingConfig['coverage_reports']) {
            $reports['coverage'] = $this->reportGenerator->generateCoverageReport($coverage);
        }
        
        if ($reportingConfig['performance_reports']) {
            $reports['performance'] = $this->reportGenerator->generatePerformanceReport($results);
        }
        
        if ($reportingConfig['security_reports']) {
            $reports['security'] = $this->reportGenerator->generateSecurityReport($results);
        }
        
        return $reports;
    }
    
    private function setupTestEnvironment($environment)
    {
        $envConfig = $this->config->get("testing_architecture.environments.{$environment}");
        
        // Set up database
        $this->setupTestDatabase($envConfig['database']);
        
        // Set up cache
        $this->setupTestCache($envConfig['cache']);
        
        // Set up external services
        $this->setupExternalServices($envConfig['external_services']);
    }
    
    private function cleanupTestEnvironment($environment)
    {
        $envConfig = $this->config->get("testing_architecture.environments.{$environment}");
        
        // Clean up database
        $this->cleanupTestDatabase($envConfig['database']);
        
        // Clean up cache
        $this->cleanupTestCache($envConfig['cache']);
        
        // Clean up external services
        $this->cleanupExternalServices($envConfig['external_services']);
    }
}
```

## 🧪 Unit Testing

### Unit Testing Configuration

```ini
# unit-testing.tsk
[unit_testing]
enabled = true
framework = "phpunit"
coverage_threshold = 95
parallel_execution = true

[unit_testing.coverage]
lines = true
functions = true
classes = true
branches = true

[unit_testing.mocking]
framework = "mockery"
auto_mock = true
mock_database = true
mock_external_services = true

[unit_testing.assertions]
custom_assertions = true
performance_assertions = true
memory_assertions = true

[unit_testing.reporting]
html_report = true
clover_report = true
text_report = true
```

### Unit Testing Implementation

```php
class UnitTestRunner
{
    private $config;
    private $testSuite;
    private $coverageCollector;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->testSuite = new PHPUnit\Framework\TestSuite();
        $this->coverageCollector = new CoverageCollector();
    }
    
    public function run($options = [])
    {
        $startTime = microtime(true);
        
        // Set up test environment
        $this->setupTestEnvironment($options);
        
        // Discover and load tests
        $tests = $this->discoverTests($options['test_path']);
        
        // Run tests
        $results = $this->runTests($tests, $options);
        
        // Collect coverage
        $coverage = $this->collectCoverage($results);
        
        // Generate reports
        $reports = $this->generateReports($results, $coverage);
        
        $duration = (microtime(true) - $startTime) * 1000;
        
        return [
            'tests_run' => $results['tests_run'],
            'tests_passed' => $results['tests_passed'],
            'tests_failed' => $results['tests_failed'],
            'tests_skipped' => $results['tests_skipped'],
            'coverage' => $coverage,
            'duration' => $duration,
            'reports' => $reports
        ];
    }
    
    private function discoverTests($testPath)
    {
        $tests = [];
        $iterator = new RecursiveIteratorIterator(
            new RecursiveDirectoryIterator($testPath)
        );
        
        foreach ($iterator as $file) {
            if ($file->isFile() && $file->getExtension() === 'php') {
                $tests[] = $file->getPathname();
            }
        }
        
        return $tests;
    }
    
    private function runTests($tests, $options)
    {
        $results = [
            'tests_run' => 0,
            'tests_passed' => 0,
            'tests_failed' => 0,
            'tests_skipped' => 0,
            'failures' => []
        ];
        
        if ($options['parallel']) {
            $results = $this->runTestsParallel($tests, $options);
        } else {
            $results = $this->runTestsSequential($tests, $options);
        }
        
        return $results;
    }
    
    private function runTestsParallel($tests, $options)
    {
        $processes = [];
        $results = [
            'tests_run' => 0,
            'tests_passed' => 0,
            'tests_failed' => 0,
            'tests_skipped' => 0,
            'failures' => []
        ];
        
        // Split tests into chunks
        $chunks = array_chunk($tests, ceil(count($tests) / 4)); // 4 parallel processes
        
        foreach ($chunks as $chunk) {
            $process = new Process(['php', 'vendor/bin/phpunit', ...$chunk]);
            $process->start();
            $processes[] = $process;
        }
        
        // Wait for all processes to complete
        foreach ($processes as $process) {
            $process->wait();
            
            $output = $process->getOutput();
            $exitCode = $process->getExitCode();
            
            // Parse output and aggregate results
            $chunkResults = $this->parseTestOutput($output);
            $this->aggregateResults($results, $chunkResults);
        }
        
        return $results;
    }
    
    private function runTestsSequential($tests, $options)
    {
        $results = [
            'tests_run' => 0,
            'tests_passed' => 0,
            'tests_failed' => 0,
            'tests_skipped' => 0,
            'failures' => []
        ];
        
        foreach ($tests as $test) {
            $testResult = $this->runSingleTest($test, $options);
            $this->aggregateResults($results, $testResult);
        }
        
        return $results;
    }
    
    private function runSingleTest($test, $options)
    {
        $process = new Process(['php', 'vendor/bin/phpunit', $test]);
        $process->run();
        
        $output = $process->getOutput();
        $exitCode = $process->getExitCode();
        
        return $this->parseTestOutput($output);
    }
    
    private function parseTestOutput($output)
    {
        // Parse PHPUnit output to extract test results
        $results = [
            'tests_run' => 0,
            'tests_passed' => 0,
            'tests_failed' => 0,
            'tests_skipped' => 0,
            'failures' => []
        ];
        
        // Extract test counts
        if (preg_match('/(\d+) tests?, (\d+) assertions?/', $output, $matches)) {
            $results['tests_run'] = (int) $matches[1];
        }
        
        if (preg_match('/(\d+) failures?/', $output, $matches)) {
            $results['tests_failed'] = (int) $matches[1];
        }
        
        if (preg_match('/(\d+) skipped/', $output, $matches)) {
            $results['tests_skipped'] = (int) $matches[1];
        }
        
        $results['tests_passed'] = $results['tests_run'] - $results['tests_failed'] - $results['tests_skipped'];
        
        return $results;
    }
    
    private function aggregateResults(&$aggregate, $results)
    {
        $aggregate['tests_run'] += $results['tests_run'];
        $aggregate['tests_passed'] += $results['tests_passed'];
        $aggregate['tests_failed'] += $results['tests_failed'];
        $aggregate['tests_skipped'] += $results['tests_skipped'];
        $aggregate['failures'] = array_merge($aggregate['failures'], $results['failures']);
    }
}

// Example Unit Test
class UserServiceTest extends PHPUnit\Framework\TestCase
{
    private $userService;
    private $userRepository;
    private $emailService;
    
    protected function setUp(): void
    {
        $this->userRepository = Mockery::mock(UserRepository::class);
        $this->emailService = Mockery::mock(EmailService::class);
        $this->userService = new UserService($this->userRepository, $this->emailService);
    }
    
    public function testCreateUser()
    {
        // Arrange
        $userData = [
            'email' => 'test@example.com',
            'name' => 'Test User',
            'password' => 'password123'
        ];
        
        $expectedUser = new User($userData);
        
        $this->userRepository
            ->shouldReceive('create')
            ->once()
            ->with(Mockery::type(User::class))
            ->andReturn($expectedUser);
        
        $this->emailService
            ->shouldReceive('sendWelcomeEmail')
            ->once()
            ->with($expectedUser);
        
        // Act
        $result = $this->userService->createUser($userData);
        
        // Assert
        $this->assertInstanceOf(User::class, $result);
        $this->assertEquals($userData['email'], $result->getEmail());
        $this->assertEquals($userData['name'], $result->getName());
    }
    
    public function testCreateUserWithInvalidEmail()
    {
        // Arrange
        $userData = [
            'email' => 'invalid-email',
            'name' => 'Test User',
            'password' => 'password123'
        ];
        
        // Act & Assert
        $this->expectException(InvalidArgumentException::class);
        $this->expectExceptionMessage('Invalid email format');
        
        $this->userService->createUser($userData);
    }
    
    protected function tearDown(): void
    {
        Mockery::close();
    }
}
```

## 🔗 Integration Testing

### Integration Testing Configuration

```ini
# integration-testing.tsk
[integration_testing]
enabled = true
framework = "phpunit"
database = "test_db"
coverage_threshold = 85

[integration_testing.database]
driver = "mysql"
host = "localhost"
port = 3306
database = "test_db"
username = "test_user"
password = "test_pass"

[integration_testing.fixtures]
auto_load = true
cleanup_after = true
transaction_rollback = true

[integration_testing.external_services]
mocking = true
sandbox_mode = true
timeout = 30
```

### Integration Testing Implementation

```php
class IntegrationTestRunner
{
    private $config;
    private $database;
    private $fixtureLoader;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->database = new DatabaseConnection($config['database']);
        $this->fixtureLoader = new FixtureLoader();
    }
    
    public function run($options = [])
    {
        $startTime = microtime(true);
        
        // Set up test database
        $this->setupTestDatabase();
        
        // Load fixtures
        $this->loadFixtures();
        
        // Run tests
        $results = $this->runIntegrationTests($options);
        
        // Clean up
        $this->cleanupTestDatabase();
        
        $duration = (microtime(true) - $startTime) * 1000;
        
        return [
            'tests_run' => $results['tests_run'],
            'tests_passed' => $results['tests_passed'],
            'tests_failed' => $results['tests_failed'],
            'duration' => $duration
        ];
    }
    
    private function setupTestDatabase()
    {
        $dbConfig = $this->config['database'];
        
        // Create test database if it doesn't exist
        $this->database->execute("CREATE DATABASE IF NOT EXISTS {$dbConfig['database']}");
        
        // Run migrations
        $this->runMigrations();
        
        // Set up test data
        $this->setupTestData();
    }
    
    private function loadFixtures()
    {
        if ($this->config['fixtures']['auto_load']) {
            $fixtures = $this->discoverFixtures();
            
            foreach ($fixtures as $fixture) {
                $this->fixtureLoader->load($fixture);
            }
        }
    }
    
    private function runIntegrationTests($options)
    {
        $tests = $this->discoverTests($options['test_path']);
        $results = [
            'tests_run' => 0,
            'tests_passed' => 0,
            'tests_failed' => 0
        ];
        
        foreach ($tests as $test) {
            $result = $this->runIntegrationTest($test);
            $this->aggregateResults($results, $result);
        }
        
        return $results;
    }
    
    private function runIntegrationTest($test)
    {
        // Start transaction
        $this->database->beginTransaction();
        
        try {
            $result = $this->executeTest($test);
            
            // Rollback transaction
            $this->database->rollback();
            
            return $result;
        } catch (Exception $e) {
            // Rollback transaction
            $this->database->rollback();
            throw $e;
        }
    }
}

// Example Integration Test
class UserIntegrationTest extends PHPUnit\Framework\TestCase
{
    private $userService;
    private $database;
    
    protected function setUp(): void
    {
        $this->database = new DatabaseConnection();
        $this->userService = new UserService(
            new UserRepository($this->database),
            new EmailService()
        );
    }
    
    public function testCreateAndRetrieveUser()
    {
        // Arrange
        $userData = [
            'email' => 'integration@example.com',
            'name' => 'Integration User',
            'password' => 'password123'
        ];
        
        // Act
        $createdUser = $this->userService->createUser($userData);
        $retrievedUser = $this->userService->getUserById($createdUser->getId());
        
        // Assert
        $this->assertNotNull($retrievedUser);
        $this->assertEquals($userData['email'], $retrievedUser->getEmail());
        $this->assertEquals($userData['name'], $retrievedUser->getName());
    }
    
    public function testUserAuthentication()
    {
        // Arrange
        $userData = [
            'email' => 'auth@example.com',
            'name' => 'Auth User',
            'password' => 'password123'
        ];
        
        $user = $this->userService->createUser($userData);
        
        // Act
        $authenticated = $this->userService->authenticate($userData['email'], $userData['password']);
        
        // Assert
        $this->assertTrue($authenticated);
    }
}
```

## 🌐 API Testing

### API Testing Configuration

```ini
# api-testing.tsk
[api_testing]
enabled = true
framework = "behat"
base_url = "http://localhost:8000"
coverage_threshold = 80

[api_testing.authentication]
type = "bearer"
token_url = "/auth/token"
client_id = @env("API_CLIENT_ID")
client_secret = @env("API_CLIENT_SECRET")

[api_testing.endpoints]
users = "/api/users"
posts = "/api/posts"
comments = "/api/comments"

[api_testing.test_data]
fixtures = "tests/api/fixtures"
generators = "tests/api/generators"
```

### API Testing Implementation

```php
class APITestRunner
{
    private $config;
    private $client;
    private $authenticator;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->client = new GuzzleHttp\Client([
            'base_uri' => $config['base_url']
        ]);
        $this->authenticator = new APIAuthenticator($config['authentication']);
    }
    
    public function run($options = [])
    {
        $startTime = microtime(true);
        
        // Authenticate
        $token = $this->authenticator->authenticate();
        
        // Run API tests
        $results = $this->runAPITests($token, $options);
        
        $duration = (microtime(true) - $startTime) * 1000;
        
        return [
            'tests_run' => $results['tests_run'],
            'tests_passed' => $results['tests_passed'],
            'tests_failed' => $results['tests_failed'],
            'coverage' => $results['coverage'],
            'duration' => $duration
        ];
    }
    
    private function runAPITests($token, $options)
    {
        $tests = $this->discoverAPITests($options['test_path']);
        $results = [
            'tests_run' => 0,
            'tests_passed' => 0,
            'tests_failed' => 0,
            'coverage' => []
        ];
        
        foreach ($tests as $test) {
            $result = $this->runAPITest($test, $token);
            $this->aggregateResults($results, $result);
        }
        
        return $results;
    }
    
    private function runAPITest($test, $token)
    {
        $scenario = $this->loadScenario($test);
        
        $results = [
            'endpoint' => $scenario['endpoint'],
            'method' => $scenario['method'],
            'status' => 'passed',
            'response_time' => 0,
            'status_code' => 0
        ];
        
        try {
            $startTime = microtime(true);
            
            $response = $this->makeRequest($scenario, $token);
            
            $results['response_time'] = (microtime(true) - $startTime) * 1000;
            $results['status_code'] = $response->getStatusCode();
            
            // Validate response
            $this->validateResponse($response, $scenario['expectations']);
            
        } catch (Exception $e) {
            $results['status'] = 'failed';
            $results['error'] = $e->getMessage();
        }
        
        return $results;
    }
    
    private function makeRequest($scenario, $token)
    {
        $options = [
            'headers' => [
                'Authorization' => "Bearer {$token}",
                'Content-Type' => 'application/json'
            ]
        ];
        
        if (isset($scenario['body'])) {
            $options['json'] = $scenario['body'];
        }
        
        return $this->client->request(
            $scenario['method'],
            $scenario['endpoint'],
            $options
        );
    }
    
    private function validateResponse($response, $expectations)
    {
        $statusCode = $response->getStatusCode();
        $body = json_decode($response->getBody(), true);
        
        // Validate status code
        if (isset($expectations['status_code'])) {
            $this->assertEquals($expectations['status_code'], $statusCode);
        }
        
        // Validate response body
        if (isset($expectations['body'])) {
            $this->validateResponseBody($body, $expectations['body']);
        }
        
        // Validate response headers
        if (isset($expectations['headers'])) {
            $this->validateResponseHeaders($response->getHeaders(), $expectations['headers']);
        }
    }
}

// Example API Test Scenario
class UserAPITest extends Behat\Behat\Context\Context
{
    private $response;
    private $client;
    
    public function __construct()
    {
        $this->client = new GuzzleHttp\Client([
            'base_uri' => 'http://localhost:8000'
        ]);
    }
    
    /**
     * @Given I have a valid API token
     */
    public function iHaveAValidApiToken()
    {
        $response = $this->client->post('/auth/token', [
            'json' => [
                'client_id' => getenv('API_CLIENT_ID'),
                'client_secret' => getenv('API_CLIENT_SECRET')
            ]
        ]);
        
        $data = json_decode($response->getBody(), true);
        $this->token = $data['access_token'];
    }
    
    /**
     * @When I create a new user
     */
    public function iCreateANewUser()
    {
        $this->response = $this->client->post('/api/users', [
            'headers' => [
                'Authorization' => "Bearer {$this->token}",
                'Content-Type' => 'application/json'
            ],
            'json' => [
                'email' => 'test@example.com',
                'name' => 'Test User',
                'password' => 'password123'
            ]
        ]);
    }
    
    /**
     * @Then the response should have status code :statusCode
     */
    public function theResponseShouldHaveStatusCode($statusCode)
    {
        $this->assertEquals($statusCode, $this->response->getStatusCode());
    }
    
    /**
     * @Then the response should contain user data
     */
    public function theResponseShouldContainUserData()
    {
        $data = json_decode($this->response->getBody(), true);
        
        $this->assertArrayHasKey('id', $data);
        $this->assertArrayHasKey('email', $data);
        $this->assertArrayHasKey('name', $data);
        $this->assertEquals('test@example.com', $data['email']);
        $this->assertEquals('Test User', $data['name']);
    }
}
```

## 📋 Best Practices

### Testing Best Practices

1. **Test Pyramid**: More unit tests, fewer integration tests, even fewer UI tests
2. **Test Isolation**: Each test should be independent
3. **Meaningful Names**: Use descriptive test names
4. **Arrange-Act-Assert**: Structure tests clearly
5. **Mock External Dependencies**: Don't test external services in unit tests
6. **Test Data Management**: Use fixtures and factories
7. **Continuous Testing**: Run tests on every commit
8. **Coverage Goals**: Aim for high coverage but focus on quality

### Integration Examples

```php
// Testing Integration
class TestingIntegration
{
    private $testingManager;
    private $unitRunner;
    private $integrationRunner;
    private $apiRunner;
    
    public function __construct()
    {
        $this->testingManager = new TestingManager();
        $this->unitRunner = new UnitTestRunner();
        $this->integrationRunner = new IntegrationTestRunner();
        $this->apiRunner = new APITestRunner();
    }
    
    public function runFullTestSuite($environment = 'development')
    {
        return $this->testingManager->runTestSuite('all', $environment);
    }
    
    public function runUnitTests()
    {
        return $this->unitRunner->run();
    }
    
    public function runIntegrationTests()
    {
        return $this->integrationRunner->run();
    }
    
    public function runAPITests()
    {
        return $this->apiRunner->run();
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Slow Tests**: Optimize test execution, use parallel testing
2. **Flaky Tests**: Ensure test isolation, fix timing issues
3. **High Memory Usage**: Clean up resources, use data providers
4. **Coverage Gaps**: Add missing test cases
5. **CI Failures**: Debug environment differences

### Debug Configuration

```ini
# debug-testing.tsk
[debug]
enabled = true
log_level = "verbose"
trace_testing = true

[debug.output]
console = true
file = "/var/log/tusk-testing-debug.log"
```

This comprehensive testing system leverages TuskLang's configuration-driven approach to create intelligent, adaptive testing solutions that ensure code quality and reliability across all application layers. 
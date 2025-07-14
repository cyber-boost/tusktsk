# 🧪 Advanced Testing Strategies with TuskLang & PHP

## Introduction
Testing is the foundation of reliable, maintainable applications. TuskLang and PHP let you implement sophisticated testing strategies with config-driven test suites, automated testing, and comprehensive coverage that ensures code quality and reliability.

## Key Features
- **Unit testing with PHPUnit**
- **Integration testing**
- **End-to-end testing**
- **Test data management**
- **Mocking and stubbing**
- **Test automation and CI/CD**

## Example: Testing Configuration
```ini
[testing]
framework: phpunit
coverage: @go("testing.ConfigureCoverage")
data: @go("testing.ConfigureTestData")
automation: @go("testing.ConfigureAutomation")
reporting: @go("testing.ConfigureReporting")
```

## PHP: Test Suite Implementation
```php
<?php

namespace App\Testing;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class TestSuite
{
    private $config;
    private $runner;
    private $coverage;
    private $reporter;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->runner = new TestRunner();
        $this->coverage = new CoverageCollector();
        $this->reporter = new TestReporter();
    }
    
    public function runTests($suite = 'all')
    {
        $startTime = microtime(true);
        
        try {
            // Setup test environment
            $this->setupTestEnvironment();
            
            // Run tests based on suite
            switch ($suite) {
                case 'unit':
                    $results = $this->runUnitTests();
                    break;
                case 'integration':
                    $results = $this->runIntegrationTests();
                    break;
                case 'e2e':
                    $results = $this->runE2ETests();
                    break;
                case 'all':
                    $results = $this->runAllTests();
                    break;
                default:
                    throw new \Exception("Unknown test suite: {$suite}");
            }
            
            // Collect coverage
            $coverage = $this->coverage->collect();
            
            // Generate report
            $report = $this->reporter->generateReport($results, $coverage);
            
            $duration = (microtime(true) - $startTime) * 1000;
            
            // Record metrics
            Metrics::record("test_execution_duration", $duration, [
                "suite" => $suite,
                "tests_count" => $results['total'],
                "passed" => $results['passed'],
                "failed" => $results['failed']
            ]);
            
            return $report;
            
        } catch (\Exception $e) {
            Metrics::record("test_execution_errors", 1, [
                "suite" => $suite,
                "error" => get_class($e)
            ]);
            
            throw $e;
        } finally {
            $this->cleanupTestEnvironment();
        }
    }
    
    private function setupTestEnvironment()
    {
        // Set test environment variables
        putenv('APP_ENV=testing');
        putenv('DB_DATABASE=test_database');
        
        // Setup test database
        $this->setupTestDatabase();
        
        // Clear caches
        $this->clearCaches();
    }
    
    private function setupTestDatabase()
    {
        $pdo = new PDO(Env::get('TEST_DB_DSN'));
        
        // Create test database if it doesn't exist
        $pdo->exec("CREATE DATABASE IF NOT EXISTS test_database");
        $pdo->exec("USE test_database");
        
        // Run migrations
        $this->runMigrations($pdo);
        
        // Seed test data
        $this->seedTestData($pdo);
    }
    
    private function runMigrations($pdo)
    {
        $migrations = $this->config->get('testing.migrations', []);
        
        foreach ($migrations as $migration) {
            $sql = file_get_contents($migration);
            $pdo->exec($sql);
        }
    }
    
    private function seedTestData($pdo)
    {
        $seeders = $this->config->get('testing.seeders', []);
        
        foreach ($seeders as $seeder) {
            $seederInstance = new $seeder();
            $seederInstance->run($pdo);
        }
    }
    
    private function clearCaches()
    {
        $caches = $this->config->get('testing.caches_to_clear', []);
        
        foreach ($caches as $cache) {
            switch ($cache) {
                case 'redis':
                    $redis = new Redis();
                    $redis->connect(Env::get('REDIS_HOST', 'localhost'));
                    $redis->flushAll();
                    break;
                case 'file':
                    $this->clearFileCache();
                    break;
            }
        }
    }
    
    private function cleanupTestEnvironment()
    {
        // Clean up test database
        $this->cleanupTestDatabase();
        
        // Restore environment
        putenv('APP_ENV');
        putenv('DB_DATABASE');
    }
    
    private function cleanupTestDatabase()
    {
        $pdo = new PDO(Env::get('TEST_DB_DSN'));
        $pdo->exec("DROP DATABASE IF EXISTS test_database");
    }
}

class TestRunner
{
    private $config;
    
    public function __construct()
    {
        $this->config = new Config();
    }
    
    public function runUnitTests()
    {
        $command = $this->config->get('testing.unit.command', 'vendor/bin/phpunit');
        $args = $this->config->get('testing.unit.args', ['--testsuite=unit']);
        
        return $this->executeTestCommand($command, $args);
    }
    
    public function runIntegrationTests()
    {
        $command = $this->config->get('testing.integration.command', 'vendor/bin/phpunit');
        $args = $this->config->get('testing.integration.args', ['--testsuite=integration']);
        
        return $this->executeTestCommand($command, $args);
    }
    
    public function runE2ETests()
    {
        $command = $this->config->get('testing.e2e.command', 'vendor/bin/behat');
        $args = $this->config->get('testing.e2e.args', []);
        
        return $this->executeTestCommand($command, $args);
    }
    
    private function executeTestCommand($command, $args)
    {
        $process = new Process(array_merge([$command], $args));
        $process->run();
        
        return [
            'success' => $process->isSuccessful(),
            'output' => $process->getOutput(),
            'error' => $process->getErrorOutput(),
            'exit_code' => $process->getExitCode()
        ];
    }
}
```

## Unit Testing
```php
<?php

namespace App\Testing\Unit;

use TuskLang\Config;
use PHPUnit\Framework\TestCase;

class UserServiceTest extends TestCase
{
    private $config;
    private $userService;
    private $userRepository;
    
    protected function setUp(): void
    {
        $this->config = new Config();
        $this->userRepository = $this->createMock(UserRepository::class);
        $this->userService = new UserService($this->userRepository);
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
            ->expects($this->once())
            ->method('save')
            ->with($this->callback(function($user) use ($userData) {
                return $user->getEmail() === $userData['email'] &&
                       $user->getName() === $userData['name'];
            }))
            ->willReturn($expectedUser);
        
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
        $this->expectException(InvalidEmailException::class);
        $this->userService->createUser($userData);
    }
    
    public function testCreateUserWithDuplicateEmail()
    {
        // Arrange
        $userData = [
            'email' => 'existing@example.com',
            'name' => 'Test User',
            'password' => 'password123'
        ];
        
        $this->userRepository
            ->expects($this->once())
            ->method('findByEmail')
            ->with($userData['email'])
            ->willReturn(new User($userData));
        
        // Act & Assert
        $this->expectException(DuplicateEmailException::class);
        $this->userService->createUser($userData);
    }
    
    public function testUpdateUser()
    {
        // Arrange
        $userId = 1;
        $updateData = [
            'name' => 'Updated Name',
            'email' => 'updated@example.com'
        ];
        
        $existingUser = new User([
            'id' => $userId,
            'name' => 'Original Name',
            'email' => 'original@example.com'
        ]);
        
        $this->userRepository
            ->expects($this->once())
            ->method('findById')
            ->with($userId)
            ->willReturn($existingUser);
        
        $this->userRepository
            ->expects($this->once())
            ->method('save')
            ->with($this->callback(function($user) use ($updateData) {
                return $user->getName() === $updateData['name'] &&
                       $user->getEmail() === $updateData['email'];
            }))
            ->willReturn($existingUser);
        
        // Act
        $result = $this->userService->updateUser($userId, $updateData);
        
        // Assert
        $this->assertInstanceOf(User::class, $result);
        $this->assertEquals($updateData['name'], $result->getName());
        $this->assertEquals($updateData['email'], $result->getEmail());
    }
    
    public function testDeleteUser()
    {
        // Arrange
        $userId = 1;
        
        $this->userRepository
            ->expects($this->once())
            ->method('delete')
            ->with($userId)
            ->willReturn(true);
        
        // Act
        $result = $this->userService->deleteUser($userId);
        
        // Assert
        $this->assertTrue($result);
    }
}

class UserService
{
    private $userRepository;
    
    public function __construct(UserRepository $userRepository)
    {
        $this->userRepository = $userRepository;
    }
    
    public function createUser(array $userData)
    {
        // Validate email
        if (!filter_var($userData['email'], FILTER_VALIDATE_EMAIL)) {
            throw new InvalidEmailException("Invalid email format");
        }
        
        // Check for duplicate email
        $existingUser = $this->userRepository->findByEmail($userData['email']);
        if ($existingUser) {
            throw new DuplicateEmailException("Email already exists");
        }
        
        // Create user
        $user = new User($userData);
        return $this->userRepository->save($user);
    }
    
    public function updateUser($userId, array $updateData)
    {
        $user = $this->userRepository->findById($userId);
        
        if (!$user) {
            throw new UserNotFoundException("User not found");
        }
        
        // Update user properties
        foreach ($updateData as $property => $value) {
            $setter = 'set' . ucfirst($property);
            if (method_exists($user, $setter)) {
                $user->$setter($value);
            }
        }
        
        return $this->userRepository->save($user);
    }
    
    public function deleteUser($userId)
    {
        return $this->userRepository->delete($userId);
    }
}
```

## Integration Testing
```php
<?php

namespace App\Testing\Integration;

use TuskLang\Config;
use PHPUnit\Framework\TestCase;

class UserControllerIntegrationTest extends TestCase
{
    private $config;
    private $app;
    private $pdo;
    
    protected function setUp(): void
    {
        $this->config = new Config();
        $this->app = new Application();
        $this->pdo = new PDO(Env::get('TEST_DB_DSN'));
        
        // Setup test database
        $this->setupTestDatabase();
    }
    
    protected function tearDown(): void
    {
        $this->cleanupTestDatabase();
    }
    
    public function testCreateUserEndpoint()
    {
        // Arrange
        $userData = [
            'email' => 'test@example.com',
            'name' => 'Test User',
            'password' => 'password123'
        ];
        
        // Act
        $response = $this->app->post('/api/users', $userData);
        
        // Assert
        $this->assertEquals(201, $response->getStatusCode());
        
        $responseData = json_decode($response->getContent(), true);
        $this->assertEquals($userData['email'], $responseData['email']);
        $this->assertEquals($userData['name'], $responseData['name']);
        
        // Verify database
        $stmt = $this->pdo->prepare("SELECT * FROM users WHERE email = ?");
        $stmt->execute([$userData['email']]);
        $user = $stmt->fetch();
        
        $this->assertNotNull($user);
        $this->assertEquals($userData['name'], $user['name']);
    }
    
    public function testGetUserEndpoint()
    {
        // Arrange
        $user = $this->createTestUser();
        
        // Act
        $response = $this->app->get("/api/users/{$user['id']}");
        
        // Assert
        $this->assertEquals(200, $response->getStatusCode());
        
        $responseData = json_decode($response->getContent(), true);
        $this->assertEquals($user['email'], $responseData['email']);
        $this->assertEquals($user['name'], $responseData['name']);
    }
    
    public function testUpdateUserEndpoint()
    {
        // Arrange
        $user = $this->createTestUser();
        $updateData = [
            'name' => 'Updated Name',
            'email' => 'updated@example.com'
        ];
        
        // Act
        $response = $this->app->put("/api/users/{$user['id']}", $updateData);
        
        // Assert
        $this->assertEquals(200, $response->getStatusCode());
        
        $responseData = json_decode($response->getContent(), true);
        $this->assertEquals($updateData['name'], $responseData['name']);
        $this->assertEquals($updateData['email'], $responseData['email']);
        
        // Verify database
        $stmt = $this->pdo->prepare("SELECT * FROM users WHERE id = ?");
        $stmt->execute([$user['id']]);
        $updatedUser = $stmt->fetch();
        
        $this->assertEquals($updateData['name'], $updatedUser['name']);
        $this->assertEquals($updateData['email'], $updatedUser['email']);
    }
    
    public function testDeleteUserEndpoint()
    {
        // Arrange
        $user = $this->createTestUser();
        
        // Act
        $response = $this->app->delete("/api/users/{$user['id']}");
        
        // Assert
        $this->assertEquals(204, $response->getStatusCode());
        
        // Verify database
        $stmt = $this->pdo->prepare("SELECT * FROM users WHERE id = ?");
        $stmt->execute([$user['id']]);
        $deletedUser = $stmt->fetch();
        
        $this->assertNull($deletedUser);
    }
    
    private function createTestUser()
    {
        $userData = [
            'email' => 'test@example.com',
            'name' => 'Test User',
            'password_hash' => password_hash('password123', PASSWORD_DEFAULT)
        ];
        
        $stmt = $this->pdo->prepare("
            INSERT INTO users (email, name, password_hash, created_at)
            VALUES (?, ?, ?, NOW())
        ");
        
        $stmt->execute([
            $userData['email'],
            $userData['name'],
            $userData['password_hash']
        ]);
        
        $userData['id'] = $this->pdo->lastInsertId();
        return $userData;
    }
    
    private function setupTestDatabase()
    {
        // Create users table
        $this->pdo->exec("
            CREATE TABLE IF NOT EXISTS users (
                id INT AUTO_INCREMENT PRIMARY KEY,
                email VARCHAR(255) UNIQUE NOT NULL,
                name VARCHAR(255) NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            )
        ");
    }
    
    private function cleanupTestDatabase()
    {
        $this->pdo->exec("DROP TABLE IF EXISTS users");
    }
}
```

## End-to-End Testing
```php
<?php

namespace App\Testing\E2E;

use TuskLang\Config;
use Behat\Behat\Context\Context;
use Behat\Gherkin\Node\PyStringNode;
use Behat\Gherkin\Node\TableNode;

class UserManagementContext implements Context
{
    private $config;
    private $client;
    private $response;
    private $userData;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->client = new HttpClient();
    }
    
    /**
     * @Given I am on the user registration page
     */
    public function iAmOnTheUserRegistrationPage()
    {
        $this->response = $this->client->get('/register');
        $this->assertEquals(200, $this->response->getStatusCode());
    }
    
    /**
     * @When I fill in the registration form with:
     */
    public function iFillInTheRegistrationFormWith(TableNode $table)
    {
        $this->userData = $table->getRowsHash();
    }
    
    /**
     * @When I submit the registration form
     */
    public function iSubmitTheRegistrationForm()
    {
        $this->response = $this->client->post('/register', $this->userData);
    }
    
    /**
     * @Then I should be redirected to the login page
     */
    public function iShouldBeRedirectedToTheLoginPage()
    {
        $this->assertEquals(302, $this->response->getStatusCode());
        $this->assertContains('/login', $this->response->getHeader('Location'));
    }
    
    /**
     * @Then I should see a success message
     */
    public function iShouldSeeASuccessMessage()
    {
        $this->assertContains('Registration successful', $this->response->getContent());
    }
    
    /**
     * @Given I am logged in as :email
     */
    public function iAmLoggedInAs($email)
    {
        $loginData = [
            'email' => $email,
            'password' => 'password123'
        ];
        
        $this->response = $this->client->post('/login', $loginData);
        $this->assertEquals(302, $this->response->getStatusCode());
    }
    
    /**
     * @When I visit the user profile page
     */
    public function iVisitTheUserProfilePage()
    {
        $this->response = $this->client->get('/profile');
        $this->assertEquals(200, $this->response->getStatusCode());
    }
    
    /**
     * @Then I should see my profile information
     */
    public function iShouldSeeMyProfileInformation()
    {
        $this->assertContains('Profile Information', $this->response->getContent());
    }
    
    /**
     * @When I update my profile with:
     */
    public function iUpdateMyProfileWith(TableNode $table)
    {
        $updateData = $table->getRowsHash();
        $this->response = $this->client->put('/profile', $updateData);
    }
    
    /**
     * @Then my profile should be updated
     */
    public function myProfileShouldBeUpdated()
    {
        $this->assertEquals(200, $this->response->getStatusCode());
        $this->assertContains('Profile updated successfully', $this->response->getContent());
    }
}

class HttpClient
{
    private $baseUrl;
    private $cookies = [];
    
    public function __construct()
    {
        $this->baseUrl = Env::get('TEST_BASE_URL', 'http://localhost:8000');
    }
    
    public function get($path)
    {
        $url = $this->baseUrl . $path;
        
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_FOLLOWLOCATION, false);
        curl_setopt($ch, CURLOPT_COOKIE, $this->formatCookies());
        
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $headers = curl_getinfo($ch, CURLINFO_HEADER_OUT);
        
        curl_close($ch);
        
        return new HttpResponse($httpCode, $response, $headers);
    }
    
    public function post($path, $data)
    {
        $url = $this->baseUrl . $path;
        
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query($data));
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_FOLLOWLOCATION, false);
        curl_setopt($ch, CURLOPT_COOKIE, $this->formatCookies());
        
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $headers = curl_getinfo($ch, CURLINFO_HEADER_OUT);
        
        curl_close($ch);
        
        return new HttpResponse($httpCode, $response, $headers);
    }
    
    public function put($path, $data)
    {
        $url = $this->baseUrl . $path;
        
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'PUT');
        curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query($data));
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_FOLLOWLOCATION, false);
        curl_setopt($ch, CURLOPT_COOKIE, $this->formatCookies());
        
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $headers = curl_getinfo($ch, CURLINFO_HEADER_OUT);
        
        curl_close($ch);
        
        return new HttpResponse($httpCode, $response, $headers);
    }
    
    private function formatCookies()
    {
        $cookies = [];
        foreach ($this->cookies as $name => $value) {
            $cookies[] = "{$name}={$value}";
        }
        return implode('; ', $cookies);
    }
}

class HttpResponse
{
    private $statusCode;
    private $content;
    private $headers;
    
    public function __construct($statusCode, $content, $headers)
    {
        $this->statusCode = $statusCode;
        $this->content = $content;
        $this->headers = $headers;
    }
    
    public function getStatusCode()
    {
        return $this->statusCode;
    }
    
    public function getContent()
    {
        return $this->content;
    }
    
    public function getHeader($name)
    {
        // Parse headers and return specific header
        $headerLines = explode("\n", $this->headers);
        foreach ($headerLines as $line) {
            if (stripos($line, $name . ':') === 0) {
                return trim(substr($line, strlen($name) + 1));
            }
        }
        return null;
    }
}
```

## Test Data Management
```php
<?php

namespace App\Testing\Data;

use TuskLang\Config;

class TestDataManager
{
    private $config;
    private $factories = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadFactories();
    }
    
    public function createUser($attributes = [])
    {
        $factory = $this->factories['user'] ?? new UserFactory();
        
        $defaultAttributes = [
            'email' => 'test@example.com',
            'name' => 'Test User',
            'password' => 'password123'
        ];
        
        $attributes = array_merge($defaultAttributes, $attributes);
        
        return $factory->create($attributes);
    }
    
    public function createOrder($attributes = [])
    {
        $factory = $this->factories['order'] ?? new OrderFactory();
        
        $defaultAttributes = [
            'user_id' => $this->createUser()->getId(),
            'total' => 100.00,
            'status' => 'pending'
        ];
        
        $attributes = array_merge($defaultAttributes, $attributes);
        
        return $factory->create($attributes);
    }
    
    public function createProduct($attributes = [])
    {
        $factory = $this->factories['product'] ?? new ProductFactory();
        
        $defaultAttributes = [
            'name' => 'Test Product',
            'price' => 50.00,
            'description' => 'Test product description'
        ];
        
        $attributes = array_merge($defaultAttributes, $attributes);
        
        return $factory->create($attributes);
    }
    
    public function seedDatabase()
    {
        $seeders = $this->config->get('testing.seeders', []);
        
        foreach ($seeders as $seeder) {
            $seederInstance = new $seeder();
            $seederInstance->run();
        }
    }
    
    public function cleanupDatabase()
    {
        $tables = $this->config->get('testing.cleanup_tables', []);
        
        $pdo = new PDO(Env::get('TEST_DB_DSN'));
        
        foreach ($tables as $table) {
            $pdo->exec("DELETE FROM {$table}");
        }
    }
    
    private function loadFactories()
    {
        $factories = $this->config->get('testing.factories', []);
        
        foreach ($factories as $name => $factory) {
            $this->factories[$name] = new $factory();
        }
    }
}

class UserFactory
{
    public function create($attributes = [])
    {
        $user = new User($attributes);
        
        // Save to database if needed
        if (isset($attributes['save']) && $attributes['save']) {
            $repository = new UserRepository();
            $user = $repository->save($user);
        }
        
        return $user;
    }
    
    public function createMany($count, $attributes = [])
    {
        $users = [];
        
        for ($i = 0; $i < $count; $i++) {
            $userAttributes = array_merge($attributes, [
                'email' => "test{$i}@example.com",
                'name' => "Test User {$i}"
            ]);
            
            $users[] = $this->create($userAttributes);
        }
        
        return $users;
    }
}
```

## Best Practices
- **Write tests first (TDD)**
- **Use descriptive test names**
- **Keep tests simple and focused**
- **Use appropriate test data**
- **Mock external dependencies**
- **Maintain test coverage**

## Performance Optimization
- **Use test data factories**
- **Optimize test database setup**
- **Use parallel test execution**
- **Cache test dependencies**

## Security Considerations
- **Use separate test databases**
- **Sanitize test data**
- **Mock sensitive operations**
- **Validate test inputs**

## Troubleshooting
- **Monitor test execution time**
- **Check test coverage reports**
- **Verify test data integrity**
- **Debug failing tests**

## Conclusion
TuskLang + PHP = testing strategies that are comprehensive, reliable, and maintainable. Build applications that you can trust. 
# Advanced Testing Strategies

TuskLang enables PHP developers to implement comprehensive testing strategies with confidence. This guide covers advanced testing patterns, test automation, and quality assurance practices.

## Table of Contents
- [Test-Driven Development](#test-driven-development)
- [Behavior-Driven Development](#behavior-driven-development)
- [Property-Based Testing](#property-based-testing)
- [Contract Testing](#contract-testing)
- [Performance Testing](#performance-testing)
- [Security Testing](#security-testing)
- [Best Practices](#best-practices)

## Test-Driven Development

```php
// config/testing.tsk
testing = {
    framework = "phpunit"
    coverage = {
        enabled = true
        threshold = 80
        exclude = ["vendor/*", "tests/*"]
    }
    
    environments = {
        unit = {
            database = "sqlite"
            cache = "array"
            queue = "sync"
        }
        integration = {
            database = "mysql"
            cache = "redis"
            queue = "redis"
        }
        acceptance = {
            database = "mysql"
            cache = "redis"
            queue = "redis"
            external_services = true
        }
    }
    
    mocking = {
        enabled = true
        auto_mock = true
        strict_mocking = true
    }
}
```

## Behavior-Driven Development

```php
<?php
// tests/Feature/UserRegistrationTest.php

namespace Tests\Feature;

use Tests\TestCase;
use App\Domain\Entities\User;
use App\Application\Services\UserService;

class UserRegistrationTest extends TestCase
{
    private UserService $userService;
    
    protected function setUp(): void
    {
        parent::setUp();
        $this->userService = $this->app->make(UserService::class);
    }
    
    /**
     * @test
     * @group user-registration
     */
    public function user_can_register_with_valid_credentials()
    {
        // Given a valid email and password
        $email = 'test@example.com';
        $password = 'SecurePassword123!';
        
        // When the user registers
        $userId = $this->userService->registerUser($email, $password);
        
        // Then the user should be created successfully
        $this->assertNotNull($userId);
        
        // And the user should be in pending status
        $user = $this->userService->findById($userId);
        $this->assertEquals('pending', $user->getStatus());
        
        // And a welcome email should be sent
        $this->assertEmailSent($email, 'Welcome to our platform');
    }
    
    /**
     * @test
     * @group user-registration
     */
    public function user_cannot_register_with_existing_email()
    {
        // Given an existing user
        $existingEmail = 'existing@example.com';
        $this->userService->registerUser($existingEmail, 'password123');
        
        // When trying to register with the same email
        // Then an exception should be thrown
        $this->expectException(\DomainException::class);
        $this->expectExceptionMessage('User with this email already exists');
        
        $this->userService->registerUser($existingEmail, 'password123');
    }
    
    /**
     * @test
     * @group user-registration
     */
    public function user_cannot_register_with_weak_password()
    {
        // Given a weak password
        $email = 'test@example.com';
        $weakPassword = '123';
        
        // When trying to register
        // Then an exception should be thrown
        $this->expectException(\DomainException::class);
        $this->expectExceptionMessage('Password is too weak');
        
        $this->userService->registerUser($email, $weakPassword);
    }
}

// tests/Unit/Domain/UserTest.php

namespace Tests\Unit\Domain;

use Tests\TestCase;
use App\Domain\Entities\User;
use App\Domain\ValueObjects\Email;
use App\Domain\ValueObjects\Password;
use App\Domain\ValueObjects\UserId;

class UserTest extends TestCase
{
    /**
     * @test
     */
    public function user_can_be_created_with_valid_data()
    {
        // Given valid user data
        $userId = new UserId();
        $email = new Email('test@example.com');
        $password = new Password('SecurePassword123!');
        
        // When creating a user
        $user = new User($userId, $email, $password);
        
        // Then the user should have correct properties
        $this->assertEquals($userId, $user->getId());
        $this->assertEquals($email, $user->getEmail());
        $this->assertEquals('pending', $user->getStatus());
        
        // And domain events should be raised
        $this->assertCount(1, $user->getDomainEvents());
        $this->assertInstanceOf(UserCreated::class, $user->getDomainEvents()[0]);
    }
    
    /**
     * @test
     */
    public function user_can_be_activated()
    {
        // Given a pending user
        $user = $this->createPendingUser();
        
        // When activating the user
        $user->activate();
        
        // Then the status should be active
        $this->assertEquals('active', $user->getStatus());
        
        // And a domain event should be raised
        $this->assertInstanceOf(UserActivated::class, $user->getDomainEvents()[1]);
    }
    
    /**
     * @test
     */
    public function user_cannot_be_activated_twice()
    {
        // Given an active user
        $user = $this->createActiveUser();
        
        // When trying to activate again
        // Then an exception should be thrown
        $this->expectException(\DomainException::class);
        $this->expectExceptionMessage('User is already active');
        
        $user->activate();
    }
    
    private function createPendingUser(): User
    {
        return new User(
            new UserId(),
            new Email('test@example.com'),
            new Password('SecurePassword123!')
        );
    }
    
    private function createActiveUser(): User
    {
        $user = $this->createPendingUser();
        $user->activate();
        return $user;
    }
}
```

## Property-Based Testing

```php
<?php
// tests/Property/UserPropertyTest.php

namespace Tests\Property;

use Tests\TestCase;
use Eris\Generator;
use Eris\TestTrait;
use App\Domain\Entities\User;
use App\Domain\ValueObjects\Email;
use App\Domain\ValueObjects\Password;

class UserPropertyTest extends TestCase
{
    use TestTrait;
    
    /**
     * @test
     */
    public function user_creation_is_idempotent()
    {
        $this->forAll(
            Generator\email(),
            Generator\string()
        )->then(function($email, $password) {
            $user1 = new User(new UserId(), new Email($email), new Password($password));
            $user2 = new User(new UserId(), new Email($email), new Password($password));
            
            $this->assertEquals($user1->getEmail(), $user2->getEmail());
            $this->assertEquals($user1->getStatus(), $user2->getStatus());
        });
    }
    
    /**
     * @test
     */
    public function user_activation_always_succeeds_for_pending_users()
    {
        $this->forAll(
            Generator\email(),
            Generator\string()
        )->then(function($email, $password) {
            $user = new User(new UserId(), new Email($email), new Password($password));
            
            $user->activate();
            
            $this->assertEquals('active', $user->getStatus());
        });
    }
    
    /**
     * @test
     */
    public function user_deactivation_always_succeeds()
    {
        $this->forAll(
            Generator\email(),
            Generator\string()
        )->then(function($email, $password) {
            $user = new User(new UserId(), new Email($email), new Password($password));
            
            $user->deactivate();
            
            $this->assertEquals('inactive', $user->getStatus());
        });
    }
}
```

## Contract Testing

```php
<?php
// tests/Contract/UserServiceContractTest.php

namespace Tests\Contract;

use Tests\TestCase;
use App\Application\Services\UserService;
use App\Domain\Repositories\UserRepositoryInterface;

class UserServiceContractTest extends TestCase
{
    private UserService $userService;
    private UserRepositoryInterface $userRepository;
    
    protected function setUp(): void
    {
        parent::setUp();
        $this->userService = $this->app->make(UserService::class);
        $this->userRepository = $this->app->make(UserRepositoryInterface::class);
    }
    
    /**
     * @test
     */
    public function user_service_contract_is_satisfied()
    {
        // Given a user service
        $userService = $this->userService;
        
        // When registering a user
        $userId = $userService->registerUser('test@example.com', 'password123');
        
        // Then the user should be saved in the repository
        $user = $this->userRepository->findById($userId);
        $this->assertNotNull($user);
        $this->assertEquals('test@example.com', $user->getEmail());
        
        // And the user should be findable by email
        $userByEmail = $this->userRepository->findByEmail('test@example.com');
        $this->assertNotNull($userByEmail);
        $this->assertEquals($userId, $userByEmail->getId());
    }
    
    /**
     * @test
     */
    public function user_service_maintains_data_consistency()
    {
        // Given a registered user
        $userId = $this->userService->registerUser('test@example.com', 'password123');
        
        // When updating the user
        $this->userService->updateUser($userId, 'new@example.com', 'newpassword123');
        
        // Then the repository should reflect the changes
        $user = $this->userRepository->findById($userId);
        $this->assertEquals('new@example.com', $user->getEmail());
        
        // And the user should still be findable by the new email
        $userByEmail = $this->userRepository->findByEmail('new@example.com');
        $this->assertNotNull($userByEmail);
        $this->assertEquals($userId, $userByEmail->getId());
    }
}

// tests/Contract/ExternalApiContractTest.php

namespace Tests\Contract;

use Tests\TestCase;
use App\Infrastructure\External\PaymentApiClient;
use App\Infrastructure\External\EmailApiClient;

class ExternalApiContractTest extends TestCase
{
    /**
     * @test
     */
    public function payment_api_contract_is_satisfied()
    {
        // Given a payment API client
        $paymentClient = $this->app->make(PaymentApiClient::class);
        
        // When processing a payment
        $paymentData = [
            'amount' => 100.00,
            'currency' => 'USD',
            'card_number' => '4111111111111111',
            'expiry_month' => 12,
            'expiry_year' => 2025,
            'cvv' => '123'
        ];
        
        $result = $paymentClient->processPayment($paymentData);
        
        // Then the response should match the contract
        $this->assertArrayHasKey('transaction_id', $result);
        $this->assertArrayHasKey('status', $result);
        $this->assertArrayHasKey('amount', $result);
        $this->assertEquals(100.00, $result['amount']);
    }
    
    /**
     * @test
     */
    public function email_api_contract_is_satisfied()
    {
        // Given an email API client
        $emailClient = $this->app->make(EmailApiClient::class);
        
        // When sending an email
        $emailData = [
            'to' => 'test@example.com',
            'subject' => 'Test Email',
            'body' => 'This is a test email'
        ];
        
        $result = $emailClient->sendEmail($emailData);
        
        // Then the response should match the contract
        $this->assertArrayHasKey('message_id', $result);
        $this->assertArrayHasKey('status', $result);
        $this->assertEquals('sent', $result['status']);
    }
}
```

## Performance Testing

```php
<?php
// tests/Performance/UserServicePerformanceTest.php

namespace Tests\Performance;

use Tests\TestCase;
use App\Application\Services\UserService;

class UserServicePerformanceTest extends TestCase
{
    private UserService $userService;
    
    protected function setUp(): void
    {
        parent::setUp();
        $this->userService = $this->app->make(UserService::class);
    }
    
    /**
     * @test
     */
    public function user_registration_performance_is_acceptable()
    {
        $startTime = microtime(true);
        
        // Perform user registration
        $this->userService->registerUser('test@example.com', 'password123');
        
        $endTime = microtime(true);
        $executionTime = ($endTime - $startTime) * 1000; // Convert to milliseconds
        
        // Assert performance is within acceptable limits
        $this->assertLessThan(100, $executionTime, 'User registration took too long');
    }
    
    /**
     * @test
     */
    public function user_lookup_performance_is_acceptable()
    {
        // Create a user first
        $userId = $this->userService->registerUser('test@example.com', 'password123');
        
        $startTime = microtime(true);
        
        // Perform user lookup
        $user = $this->userService->findById($userId);
        
        $endTime = microtime(true);
        $executionTime = ($endTime - $startTime) * 1000;
        
        // Assert performance is within acceptable limits
        $this->assertLessThan(50, $executionTime, 'User lookup took too long');
        $this->assertNotNull($user);
    }
    
    /**
     * @test
     */
    public function bulk_user_operations_performance_is_acceptable()
    {
        $userCount = 100;
        $startTime = microtime(true);
        
        // Create multiple users
        for ($i = 0; $i < $userCount; $i++) {
            $this->userService->registerUser("user{$i}@example.com", 'password123');
        }
        
        $endTime = microtime(true);
        $executionTime = ($endTime - $startTime) * 1000;
        
        // Assert performance is within acceptable limits
        $this->assertLessThan(5000, $executionTime, 'Bulk user creation took too long');
    }
}

// tests/Performance/DatabasePerformanceTest.php

namespace Tests\Performance;

use Tests\TestCase;
use Illuminate\Support\Facades\DB;

class DatabasePerformanceTest extends TestCase
{
    /**
     * @test
     */
    public function database_query_performance_is_acceptable()
    {
        $startTime = microtime(true);
        
        // Perform a complex query
        $users = DB::table('users')
            ->join('user_profiles', 'users.id', '=', 'user_profiles.user_id')
            ->where('users.status', 'active')
            ->where('user_profiles.verified', true)
            ->select(['users.*', 'user_profiles.bio'])
            ->get();
        
        $endTime = microtime(true);
        $executionTime = ($endTime - $startTime) * 1000;
        
        // Assert performance is within acceptable limits
        $this->assertLessThan(200, $executionTime, 'Database query took too long');
    }
    
    /**
     * @test
     */
    public function database_transaction_performance_is_acceptable()
    {
        $startTime = microtime(true);
        
        // Perform a transaction
        DB::transaction(function() {
            // Create user
            $userId = DB::table('users')->insertGetId([
                'email' => 'test@example.com',
                'password' => 'hashed_password',
                'created_at' => now()
            ]);
            
            // Create user profile
            DB::table('user_profiles')->insert([
                'user_id' => $userId,
                'bio' => 'Test bio',
                'created_at' => now()
            ]);
        });
        
        $endTime = microtime(true);
        $executionTime = ($endTime - $startTime) * 1000;
        
        // Assert performance is within acceptable limits
        $this->assertLessThan(150, $executionTime, 'Database transaction took too long');
    }
}
```

## Security Testing

```php
<?php
// tests/Security/UserAuthenticationSecurityTest.php

namespace Tests\Security;

use Tests\TestCase;
use App\Application\Services\UserService;
use App\Infrastructure\Services\PasswordHasher;

class UserAuthenticationSecurityTest extends TestCase
{
    private UserService $userService;
    private PasswordHasher $passwordHasher;
    
    protected function setUp(): void
    {
        parent::setUp();
        $this->userService = $this->app->make(UserService::class);
        $this->passwordHasher = $this->app->make(PasswordHasher::class);
    }
    
    /**
     * @test
     */
    public function passwords_are_properly_hashed()
    {
        // Given a user registration
        $email = 'test@example.com';
        $password = 'SecurePassword123!';
        
        $userId = $this->userService->registerUser($email, $password);
        $user = $this->userService->findById($userId);
        
        // Then the password should be hashed
        $this->assertNotEquals($password, $user->getPassword());
        $this->assertTrue($this->passwordHasher->verify($password, $user->getPassword()));
    }
    
    /**
     * @test
     */
    public function sql_injection_is_prevented()
    {
        // Given malicious input
        $maliciousEmail = "'; DROP TABLE users; --";
        $password = 'password123';
        
        // When trying to register with malicious input
        // Then it should be handled safely
        $userId = $this->userService->registerUser($maliciousEmail, $password);
        
        // And the user should be created (with sanitized input)
        $user = $this->userService->findById($userId);
        $this->assertNotNull($user);
        
        // And the database should still be intact
        $userCount = DB::table('users')->count();
        $this->assertGreaterThan(0, $userCount);
    }
    
    /**
     * @test
     */
    public function xss_attacks_are_prevented()
    {
        // Given malicious input with XSS
        $maliciousName = '<script>alert("XSS")</script>';
        $email = 'test@example.com';
        $password = 'password123';
        
        // When registering a user
        $userId = $this->userService->registerUser($email, $password);
        $this->userService->updateUser($userId, $maliciousName);
        
        // Then the malicious content should be sanitized
        $user = $this->userService->findById($userId);
        $this->assertNotContains('<script>', $user->getName());
        $this->assertNotContains('alert', $user->getName());
    }
    
    /**
     * @test
     */
    public function csrf_protection_is_enabled()
    {
        // Given a request without CSRF token
        $response = $this->post('/api/users', [
            'email' => 'test@example.com',
            'password' => 'password123'
        ]);
        
        // Then the request should be rejected
        $this->assertEquals(419, $response->getStatusCode());
    }
    
    /**
     * @test
     */
    public function rate_limiting_is_enforced()
    {
        // Given multiple rapid requests
        for ($i = 0; $i < 10; $i++) {
            $response = $this->post('/api/users', [
                'email' => "test{$i}@example.com",
                'password' => 'password123'
            ]);
        }
        
        // Then the requests should be rate limited
        $response = $this->post('/api/users', [
            'email' => 'test11@example.com',
            'password' => 'password123'
        ]);
        
        $this->assertEquals(429, $response->getStatusCode());
    }
}
```

## Best Practices

```php
// config/testing-best-practices.tsk

testing_best_practices = {
    test_design = {
        use_descriptive_names = true
        follow_aaa_pattern = true
        test_one_thing_per_test = true
        use_proper_assertions = true
    }
    
    test_organization = {
        group_related_tests = true
        use_test_suites = true
        organize_by_feature = true
        use_proper_namespacing = true
    }
    
    test_data = {
        use_factories = true
        create_minimal_data = true
        use_fixtures = true
        avoid_test_interdependence = true
    }
    
    mocking = {
        mock_external_dependencies = true
        use_proper_mock_objects = true
        verify_mock_interactions = true
        avoid_over_mocking = true
    }
    
    performance = {
        run_performance_tests = true
        set_performance_baselines = true
        monitor_test_execution_time = true
        optimize_slow_tests = true
    }
    
    security = {
        test_security_measures = true
        validate_input_handling = true
        test_authentication = true
        test_authorization = true
    }
    
    automation = {
        run_tests_automatically = true
        integrate_with_ci_cd = true
        generate_test_reports = true
        fail_build_on_test_failure = true
    }
}

// Example usage in PHP
class TestingBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Use descriptive test names
        $this->test('user_can_register_with_valid_credentials');
        
        // 2. Follow AAA pattern (Arrange, Act, Assert)
        // Arrange
        $email = 'test@example.com';
        $password = 'SecurePassword123!';
        
        // Act
        $userId = $this->userService->registerUser($email, $password);
        
        // Assert
        $this->assertNotNull($userId);
        
        // 3. Use factories for test data
        $user = UserFactory::create(['email' => 'test@example.com']);
        
        // 4. Mock external dependencies
        $this->mock(EmailService::class)->shouldReceive('send')->once();
        
        // 5. Test security measures
        $this->testSqlInjectionProtection();
        $this->testXssProtection();
        
        // 6. Monitor performance
        $this->assertExecutionTimeLessThan(100, function() {
            $this->userService->registerUser('test@example.com', 'password123');
        });
        
        // 7. Generate test reports
        $this->generateTestReport();
    }
    
    private function testSqlInjectionProtection(): void
    {
        $maliciousInput = "'; DROP TABLE users; --";
        
        $this->expectException(\InvalidArgumentException::class);
        $this->userService->registerUser($maliciousInput, 'password123');
    }
    
    private function testXssProtection(): void
    {
        $maliciousInput = '<script>alert("XSS")</script>';
        
        $user = $this->userService->registerUser('test@example.com', 'password123');
        $this->userService->updateUser($user->getId(), $maliciousInput);
        
        $updatedUser = $this->userService->findById($user->getId());
        $this->assertNotContains('<script>', $updatedUser->getName());
    }
    
    private function assertExecutionTimeLessThan(int $milliseconds, callable $callback): void
    {
        $startTime = microtime(true);
        $callback();
        $executionTime = (microtime(true) - $startTime) * 1000;
        
        $this->assertLessThan($milliseconds, $executionTime);
    }
    
    private function generateTestReport(): void
    {
        $coverage = $this->getTestCoverage();
        $this->logger->info('Test coverage', ['coverage' => $coverage]);
    }
}
```

This comprehensive guide covers advanced testing strategies in TuskLang with PHP integration. The testing system is designed to be comprehensive, reliable, and maintainable while maintaining the rebellious spirit of TuskLang development. 
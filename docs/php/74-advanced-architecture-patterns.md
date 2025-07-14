# Advanced Architecture Patterns

TuskLang enables PHP developers to build sophisticated architectural patterns with confidence. This guide covers advanced architecture patterns, system design principles, and integration strategies.

## Table of Contents
- [Clean Architecture](#clean-architecture)
- [Hexagonal Architecture](#hexagonal-architecture)
- [Event-Driven Architecture](#event-driven-architecture)
- [Microservices Architecture](#microservices-architecture)
- [CQRS Architecture](#cqrs-architecture)
- [Best Practices](#best-practices)

## Clean Architecture

```php
// config/clean-architecture.tsk
clean_architecture = {
    layers = {
        domain = {
            entities = ["User", "Order", "Product"]
            value_objects = ["Email", "Money", "UserId"]
            domain_services = ["UserRegistration", "OrderProcessing"]
            repositories = ["UserRepository", "OrderRepository"]
        }
        
        application = {
            use_cases = ["CreateUser", "ProcessOrder", "GetUserProfile"]
            services = ["UserService", "OrderService", "EmailService"]
            commands = ["CreateUserCommand", "ProcessOrderCommand"]
            queries = ["GetUserQuery", "GetOrderQuery"]
        }
        
        infrastructure = {
            repositories = ["UserRepositoryImpl", "OrderRepositoryImpl"]
            external_services = ["EmailService", "PaymentService"]
            database = ["MySQLConnection", "RedisConnection"]
            frameworks = ["Laravel", "Symfony"]
        }
        
        presentation = {
            controllers = ["UserController", "OrderController"]
            views = ["UserView", "OrderView"]
            api_resources = ["UserResource", "OrderResource"]
        }
    }
    
    dependencies = {
        domain_independent = true
        application_independent = true
        infrastructure_dependent = true
        presentation_dependent = true
    }
}
```

## Hexagonal Architecture

```php
<?php
// app/Domain/Entities/User.php

namespace App\Domain\Entities;

use TuskLang\Domain\Entity;
use TuskLang\Domain\ValueObject;

class User extends Entity
{
    private UserId $id;
    private Email $email;
    private Password $password;
    private UserStatus $status;
    private \DateTimeImmutable $createdAt;
    
    public function __construct(UserId $id, Email $email, Password $password)
    {
        $this->id = $id;
        $this->email = $email;
        $this->password = $password;
        $this->status = UserStatus::PENDING();
        $this->createdAt = new \DateTimeImmutable();
        
        $this->addDomainEvent(new UserCreated($this->id, $this->email));
    }
    
    public function activate(): void
    {
        $this->status = UserStatus::ACTIVE();
        $this->addDomainEvent(new UserActivated($this->id));
    }
    
    public function deactivate(): void
    {
        $this->status = UserStatus::INACTIVE();
        $this->addDomainEvent(new UserDeactivated($this->id));
    }
    
    public function changePassword(Password $newPassword): void
    {
        $this->password = $newPassword;
        $this->addDomainEvent(new UserPasswordChanged($this->id));
    }
    
    public function getId(): UserId
    {
        return $this->id;
    }
    
    public function getEmail(): Email
    {
        return $this->email;
    }
    
    public function getStatus(): UserStatus
    {
        return $this->status;
    }
    
    public function getCreatedAt(): \DateTimeImmutable
    {
        return $this->createdAt;
    }
}

// app/Domain/Repositories/UserRepositoryInterface.php

namespace App\Domain\Repositories;

use App\Domain\Entities\User;
use App\Domain\ValueObjects\UserId;
use App\Domain\ValueObjects\Email;

interface UserRepositoryInterface
{
    public function findById(UserId $id): ?User;
    public function findByEmail(Email $email): ?User;
    public function save(User $user): void;
    public function delete(UserId $id): void;
    public function findAll(): array;
}

// app/Application/Services/UserService.php

namespace App\Application\Services;

use App\Domain\Entities\User;
use App\Domain\Repositories\UserRepositoryInterface;
use App\Domain\Services\PasswordHasherInterface;
use App\Domain\Services\EmailServiceInterface;

class UserService
{
    public function __construct(
        private UserRepositoryInterface $userRepository,
        private PasswordHasherInterface $passwordHasher,
        private EmailServiceInterface $emailService
    ) {}
    
    public function registerUser(string $email, string $password): UserId
    {
        $emailVO = new Email($email);
        $passwordVO = new Password($password);
        $userId = new UserId();
        
        // Check if user already exists
        if ($this->userRepository->findByEmail($emailVO)) {
            throw new \DomainException('User with this email already exists');
        }
        
        // Hash password
        $hashedPassword = $this->passwordHasher->hash($passwordVO);
        
        // Create user
        $user = new User($userId, $emailVO, $hashedPassword);
        
        // Save user
        $this->userRepository->save($user);
        
        // Send welcome email
        $this->emailService->sendWelcomeEmail($emailVO);
        
        return $userId;
    }
    
    public function activateUser(UserId $userId): void
    {
        $user = $this->userRepository->findById($userId);
        
        if (!$user) {
            throw new \DomainException('User not found');
        }
        
        $user->activate();
        $this->userRepository->save($user);
    }
    
    public function getUserById(UserId $userId): ?User
    {
        return $this->userRepository->findById($userId);
    }
    
    public function getUserByEmail(string $email): ?User
    {
        $emailVO = new Email($email);
        return $this->userRepository->findByEmail($emailVO);
    }
}

// app/Infrastructure/Repositories/UserRepositoryImpl.php

namespace App\Infrastructure\Repositories;

use App\Domain\Entities\User;
use App\Domain\Repositories\UserRepositoryInterface;
use App\Domain\ValueObjects\UserId;
use App\Domain\ValueObjects\Email;
use TuskLang\Database\Connection;

class UserRepositoryImpl implements UserRepositoryInterface
{
    public function __construct(private Connection $connection) {}
    
    public function findById(UserId $id): ?User
    {
        $result = $this->connection->select(
            "SELECT * FROM users WHERE id = ?",
            [$id->getValue()]
        );
        
        if (empty($result)) {
            return null;
        }
        
        return $this->mapToEntity($result[0]);
    }
    
    public function findByEmail(Email $email): ?User
    {
        $result = $this->connection->select(
            "SELECT * FROM users WHERE email = ?",
            [$email->getValue()]
        );
        
        if (empty($result)) {
            return null;
        }
        
        return $this->mapToEntity($result[0]);
    }
    
    public function save(User $user): void
    {
        $data = [
            'id' => $user->getId()->getValue(),
            'email' => $user->getEmail()->getValue(),
            'password' => $user->getPassword()->getValue(),
            'status' => $user->getStatus()->getValue(),
            'created_at' => $user->getCreatedAt()->format('Y-m-d H:i:s')
        ];
        
        $existing = $this->findById($user->getId());
        
        if ($existing) {
            $this->connection->update('users', $data, ['id' => $user->getId()->getValue()]);
        } else {
            $this->connection->insert('users', $data);
        }
    }
    
    public function delete(UserId $id): void
    {
        $this->connection->delete('users', ['id' => $id->getValue()]);
    }
    
    public function findAll(): array
    {
        $results = $this->connection->select("SELECT * FROM users");
        
        return array_map([$this, 'mapToEntity'], $results);
    }
    
    private function mapToEntity(array $data): User
    {
        $userId = new UserId($data['id']);
        $email = new Email($data['email']);
        $password = new Password($data['password']);
        
        $user = new User($userId, $email, $password);
        
        // Set additional properties
        $user->setStatus(UserStatus::fromValue($data['status']));
        $user->setCreatedAt(new \DateTimeImmutable($data['created_at']));
        
        return $user;
    }
}
```

## Event-Driven Architecture

```php
<?php
// app/Domain/Events/UserCreated.php

namespace App\Domain\Events;

use TuskLang\EventSourcing\DomainEvent;

class UserCreated implements DomainEvent
{
    public function __construct(
        public readonly string $userId,
        public readonly string $email,
        public readonly \DateTimeImmutable $occurredAt
    ) {}
    
    public function getAggregateId(): string
    {
        return $this->userId;
    }
    
    public function getOccurredAt(): \DateTimeImmutable
    {
        return $this->occurredAt;
    }
}

// app/Infrastructure/EventBus/EventBus.php

namespace App\Infrastructure\EventBus;

use TuskLang\Events\EventDispatcher;
use TuskLang\Events\EventSubscriber;

class EventBus
{
    private EventDispatcher $dispatcher;
    private array $subscribers = [];
    
    public function __construct(EventDispatcher $dispatcher)
    {
        $this->dispatcher = $dispatcher;
    }
    
    public function publish(DomainEvent $event): void
    {
        $this->dispatcher->dispatch($event);
        
        // Also publish to external systems if configured
        $this->publishToExternalSystems($event);
    }
    
    public function subscribe(string $eventType, callable $handler): void
    {
        $this->subscribers[$eventType][] = $handler;
        $this->dispatcher->listen($eventType, $handler);
    }
    
    public function subscribeToAll(EventSubscriber $subscriber): void
    {
        $subscriber->subscribe($this);
    }
    
    private function publishToExternalSystems(DomainEvent $event): void
    {
        $config = TuskLang::load('event_driven_architecture');
        
        if (isset($config['external_publishers'])) {
            foreach ($config['external_publishers'] as $publisher) {
                $this->publishToSystem($event, $publisher);
            }
        }
    }
    
    private function publishToSystem(DomainEvent $event, array $publisher): void
    {
        $client = new \GuzzleHttp\Client();
        
        try {
            $client->post($publisher['url'], [
                'json' => [
                    'event_type' => get_class($event),
                    'data' => $event,
                    'timestamp' => $event->getOccurredAt()->format('c')
                ],
                'headers' => $publisher['headers'] ?? []
            ]);
        } catch (\Exception $e) {
            // Log failed external publishing
            error_log("Failed to publish event to {$publisher['url']}: " . $e->getMessage());
        }
    }
}

// app/Infrastructure/EventHandlers/UserEventHandler.php

namespace App\Infrastructure\EventHandlers;

use App\Domain\Events\UserCreated;
use App\Domain\Events\UserActivated;
use App\Infrastructure\Services\EmailService;
use App\Infrastructure\Services\AnalyticsService;

class UserEventHandler
{
    public function __construct(
        private EmailService $emailService,
        private AnalyticsService $analyticsService
    ) {}
    
    public function onUserCreated(UserCreated $event): void
    {
        // Send welcome email
        $this->emailService->sendWelcomeEmail($event->email);
        
        // Track user registration
        $this->analyticsService->trackUserRegistration($event->userId);
        
        // Update user count metrics
        $this->analyticsService->incrementUserCount();
    }
    
    public function onUserActivated(UserActivated $event): void
    {
        // Send activation confirmation
        $this->emailService->sendActivationConfirmation($event->userId);
        
        // Track user activation
        $this->analyticsService->trackUserActivation($event->userId);
    }
}
```

## Microservices Architecture

```php
<?php
// app/Infrastructure/Microservices/ServiceRegistry.php

namespace App\Infrastructure\Microservices;

use TuskLang\ServiceDiscovery\ServiceRegistryInterface;

class ServiceRegistry implements ServiceRegistryInterface
{
    private array $services = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
    }
    
    public function register(string $name, string $url, array $metadata = []): void
    {
        $this->services[$name] = [
            'url' => $url,
            'metadata' => $metadata,
            'health_check' => $this->performHealthCheck($url),
            'last_updated' => time()
        ];
    }
    
    public function getService(string $name): ?array
    {
        return $this->services[$name] ?? null;
    }
    
    public function getAllServices(): array
    {
        return $this->services;
    }
    
    public function getHealthyServices(): array
    {
        return array_filter($this->services, function($service) {
            return $service['health_check']['healthy'] ?? false;
        });
    }
    
    public function deregister(string $name): void
    {
        unset($this->services[$name]);
    }
    
    private function performHealthCheck(string $url): array
    {
        $client = new \GuzzleHttp\Client(['timeout' => 5]);
        
        try {
            $response = $client->get($url . '/health');
            return [
                'healthy' => $response->getStatusCode() === 200,
                'response_time' => $response->getHeader('X-Response-Time')[0] ?? null
            ];
        } catch (\Exception $e) {
            return [
                'healthy' => false,
                'error' => $e->getMessage()
            ];
        }
    }
}

// app/Infrastructure/Microservices/ServiceClient.php

namespace App\Infrastructure\Microservices;

use TuskLang\ServiceDiscovery\ServiceRegistryInterface;

class ServiceClient
{
    private ServiceRegistryInterface $registry;
    private array $config;
    
    public function __construct(ServiceRegistryInterface $registry, array $config)
    {
        $this->registry = $registry;
        $this->config = $config;
    }
    
    public function call(string $serviceName, string $method, string $endpoint, array $data = []): array
    {
        $service = $this->registry->getService($serviceName);
        
        if (!$service) {
            throw new \RuntimeException("Service '{$serviceName}' not found");
        }
        
        $url = $service['url'] . $endpoint;
        
        $client = new \GuzzleHttp\Client([
            'timeout' => $this->config['timeout'] ?? 30,
            'headers' => $this->config['headers'] ?? []
        ]);
        
        try {
            $response = $client->request($method, $url, [
                'json' => $data
            ]);
            
            return json_decode($response->getBody(), true);
        } catch (\Exception $e) {
            throw new \RuntimeException("Service call failed: " . $e->getMessage());
        }
    }
    
    public function callAsync(string $serviceName, string $method, string $endpoint, array $data = []): \GuzzleHttp\Promise\PromiseInterface
    {
        $service = $this->registry->getService($serviceName);
        
        if (!$service) {
            throw new \RuntimeException("Service '{$serviceName}' not found");
        }
        
        $url = $service['url'] . $endpoint;
        
        $client = new \GuzzleHttp\Client([
            'timeout' => $this->config['timeout'] ?? 30,
            'headers' => $this->config['headers'] ?? []
        ]);
        
        return $client->requestAsync($method, $url, [
            'json' => $data
        ]);
    }
}
```

## CQRS Architecture

```php
<?php
// app/Application/Commands/CreateUserCommand.php

namespace App\Application\Commands;

use TuskLang\CQRS\Command;

class CreateUserCommand implements Command
{
    public function __construct(
        public readonly string $email,
        public readonly string $password,
        public readonly string $name
    ) {}
}

// app/Application/Commands/CreateUserHandler.php

namespace App\Application\Commands;

use App\Application\Services\UserService;
use App\Domain\ValueObjects\UserId;

class CreateUserHandler
{
    public function __construct(private UserService $userService) {}
    
    public function handle(CreateUserCommand $command): UserId
    {
        return $this->userService->registerUser(
            $command->email,
            $command->password
        );
    }
}

// app/Application/Queries/GetUserQuery.php

namespace App\Application\Queries;

use TuskLang\CQRS\Query;

class GetUserQuery implements Query
{
    public function __construct(public readonly string $userId) {}
}

// app/Application/Queries/GetUserHandler.php

namespace App\Application\Queries;

use App\Infrastructure\ReadModels\UserReadModel;

class GetUserHandler
{
    public function __construct(private UserReadModel $readModel) {}
    
    public function handle(GetUserQuery $query): ?array
    {
        return $this->readModel->findById($query->userId);
    }
}

// app/Infrastructure/CommandBus/CommandBus.php

namespace App\Infrastructure\CommandBus;

use TuskLang\CQRS\CommandBus as TuskLangCommandBus;

class CommandBus implements TuskLangCommandBus
{
    private array $handlers = [];
    
    public function register(string $commandClass, callable $handler): void
    {
        $this->handlers[$commandClass] = $handler;
    }
    
    public function dispatch(Command $command): mixed
    {
        $commandClass = get_class($command);
        
        if (!isset($this->handlers[$commandClass])) {
            throw new \RuntimeException("No handler registered for {$commandClass}");
        }
        
        return $this->handlers[$commandClass]($command);
    }
}

// app/Infrastructure/QueryBus/QueryBus.php

namespace App\Infrastructure\QueryBus;

use TuskLang\CQRS\QueryBus as TuskLangQueryBus;

class QueryBus implements TuskLangQueryBus
{
    private array $handlers = [];
    
    public function register(string $queryClass, callable $handler): void
    {
        $this->handlers[$queryClass] = $handler;
    }
    
    public function dispatch(Query $query): mixed
    {
        $queryClass = get_class($query);
        
        if (!isset($this->handlers[$queryClass])) {
            throw new \RuntimeException("No handler registered for {$queryClass}");
        }
        
        return $this->handlers[$queryClass]($query);
    }
}
```

## Best Practices

```php
// config/architecture-best-practices.tsk
architecture_best_practices = {
    design_principles = {
        single_responsibility = true
        open_closed_principle = true
        liskov_substitution = true
        interface_segregation = true
        dependency_inversion = true
    }
    
    architecture_patterns = {
        use_clean_architecture = true
        implement_hexagonal_architecture = true
        use_event_driven_architecture = true
        implement_cqrs = true
        use_microservices = true
    }
    
    dependency_management = {
        use_dependency_injection = true
        follow_inversion_of_control = true
        use_interface_abstraction = true
        minimize_coupling = true
    }
    
    testing = {
        test_architecture_layers = true
        use_mock_objects = true
        test_integration = true
        maintain_test_coverage = true
    }
    
    performance = {
        optimize_critical_paths = true
        use_caching_strategies = true
        implement_lazy_loading = true
        monitor_performance = true
    }
    
    security = {
        implement_security_layers = true
        validate_all_inputs = true
        use_secure_communication = true
        audit_architecture = true
    }
}

// Example usage in PHP
class ArchitectureBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Use dependency injection
        $container = new Container();
        $container->register(UserService::class, function() {
            return new UserService(
                $container->get(UserRepositoryInterface::class),
                $container->get(PasswordHasherInterface::class),
                $container->get(EmailServiceInterface::class)
            );
        });
        
        // 2. Follow SOLID principles
        $userService = $container->get(UserService::class);
        
        // 3. Use CQRS for complex operations
        $commandBus = $container->get(CommandBus::class);
        $queryBus = $container->get(QueryBus::class);
        
        // 4. Implement event sourcing for audit trails
        $eventStore = $container->get(EventStore::class);
        
        // 5. Use reactive patterns for async operations
        $eventStream = $container->get(EventStream::class);
        
        // 6. Monitor and log
        $this->logger->info('Architecture pattern implemented', [
            'pattern' => 'Clean Architecture',
            'layers' => ['Domain', 'Application', 'Infrastructure', 'Presentation'],
            'principles' => ['SOLID', 'DRY', 'KISS']
        ]);
    }
}
```

This comprehensive guide covers advanced architecture patterns in TuskLang with PHP integration. The architecture system is designed to be maintainable, scalable, and robust while maintaining the rebellious spirit of TuskLang development. 
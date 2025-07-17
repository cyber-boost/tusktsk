# Advanced Design Patterns

TuskLang enables PHP developers to implement sophisticated design patterns with elegance and power. This guide covers advanced design patterns, architectural principles, and integration strategies.

## Table of Contents
- [Domain-Driven Design](#domain-driven-design)
- [Hexagonal Architecture](#hexagonal-architecture)
- [Command Query Responsibility Segregation](#command-query-responsibility-segregation)
- [Event Sourcing](#event-sourcing)
- [Microservices Patterns](#microservices-patterns)
- [Reactive Patterns](#reactive-patterns)
- [Best Practices](#best-practices)

## Domain-Driven Design

```php
// config/ddd.tsk
domain_driven_design = {
    bounded_contexts = {
        user_management = {
            domain_events = ["UserCreated", "UserUpdated", "UserDeleted"]
            aggregates = ["User", "UserProfile"]
            value_objects = ["Email", "Password", "UserId"]
            repositories = ["UserRepository"]
        }
        order_management = {
            domain_events = ["OrderCreated", "OrderItemAdded", "OrderCompleted"]
            aggregates = ["Order", "OrderItem"]
            value_objects = ["OrderId", "Money", "Quantity"]
            repositories = ["OrderRepository"]
        }
    }
    
    domain_services = {
        user_registration = {
            dependencies = ["UserRepository", "EmailService", "PasswordHasher"]
            invariants = ["email_must_be_unique", "password_must_be_strong"]
        }
        order_processing = {
            dependencies = ["OrderRepository", "InventoryService", "PaymentService"]
            invariants = ["items_must_be_in_stock", "payment_must_be_valid"]
        }
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
    
    public function __construct(UserId $id, Email $email, Password $password)
    {
        $this->id = $id;
        $this->email = $email;
        $this->password = $password;
        $this->status = UserStatus::PENDING();
        
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
}
```

## Command Query Responsibility Segregation

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

## Event Sourcing

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

// app/Domain/Aggregates/UserAggregate.php

namespace App\Domain\Aggregates;

use TuskLang\EventSourcing\AggregateRoot;
use App\Domain\Events\UserCreated;
use App\Domain\Events\UserActivated;
use App\Domain\Events\UserDeactivated;

class UserAggregate extends AggregateRoot
{
    private string $id;
    private string $email;
    private string $status = 'pending';
    
    public static function create(string $id, string $email): self
    {
        $aggregate = new self();
        $aggregate->apply(new UserCreated($id, $email, new \DateTimeImmutable()));
        return $aggregate;
    }
    
    public function activate(): void
    {
        $this->apply(new UserActivated($this->id, new \DateTimeImmutable()));
    }
    
    public function deactivate(): void
    {
        $this->apply(new UserDeactivated($this->id, new \DateTimeImmutable()));
    }
    
    protected function applyUserCreated(UserCreated $event): void
    {
        $this->id = $event->userId;
        $this->email = $event->email;
    }
    
    protected function applyUserActivated(UserActivated $event): void
    {
        $this->status = 'active';
    }
    
    protected function applyUserDeactivated(UserDeactivated $event): void
    {
        $this->status = 'inactive';
    }
}
```

## Microservices Patterns

```php
<?php
// app/Infrastructure/Microservices/ServiceDiscovery.php

namespace App\Infrastructure\Microservices;

use TuskLang\ServiceDiscovery\ServiceRegistry;

class ServiceDiscovery
{
    private ServiceRegistry $registry;
    private array $config;
    
    public function __construct(ServiceRegistry $registry, array $config)
    {
        $this->registry = $registry;
        $this->config = $config;
    }
    
    public function getServiceUrl(string $serviceName): string
    {
        $service = $this->registry->getService($serviceName);
        
        if (!$service) {
            throw new \RuntimeException("Service {$serviceName} not found");
        }
        
        return $service->getUrl();
    }
    
    public function registerService(string $name, string $url, array $metadata = []): void
    {
        $this->registry->register($name, $url, $metadata);
    }
}

// app/Infrastructure/Microservices/CircuitBreaker.php

namespace App\Infrastructure\Microservices;

class CircuitBreaker
{
    private int $failureCount = 0;
    private int $threshold;
    private int $timeout;
    private bool $open = false;
    private int $lastFailureTime = 0;
    
    public function __construct(int $threshold = 5, int $timeout = 30)
    {
        $this->threshold = $threshold;
        $this->timeout = $timeout;
    }
    
    public function call(callable $serviceCall)
    {
        if ($this->open && (time() - $this->lastFailureTime) < $this->timeout) {
            throw new \RuntimeException('Circuit breaker is open');
        }
        
        try {
            $result = $serviceCall();
            $this->failureCount = 0;
            $this->open = false;
            return $result;
        } catch (\Exception $e) {
            $this->failureCount++;
            $this->lastFailureTime = time();
            
            if ($this->failureCount >= $this->threshold) {
                $this->open = true;
            }
            
            throw $e;
        }
    }
}
```

## Reactive Patterns

```php
<?php
// app/Infrastructure/Reactive/EventStream.php

namespace App\Infrastructure\Reactive;

use React\EventLoop\LoopInterface;
use React\Stream\ReadableStreamInterface;

class EventStream
{
    private LoopInterface $loop;
    private array $subscribers = [];
    
    public function __construct(LoopInterface $loop)
    {
        $this->loop = $loop;
    }
    
    public function subscribe(string $eventType, callable $handler): void
    {
        if (!isset($this->subscribers[$eventType])) {
            $this->subscribers[$eventType] = [];
        }
        
        $this->subscribers[$eventType][] = $handler;
    }
    
    public function publish(string $eventType, array $data): void
    {
        if (isset($this->subscribers[$eventType])) {
            foreach ($this->subscribers[$eventType] as $handler) {
                $this->loop->addTimer(0, function() use ($handler, $data) {
                    $handler($data);
                });
            }
        }
    }
    
    public function createStream(string $eventType): ReadableStreamInterface
    {
        return new EventReadableStream($this, $eventType);
    }
}

// app/Infrastructure/Reactive/EventReadableStream.php

namespace App\Infrastructure\Reactive;

use React\Stream\ReadableStream;

class EventReadableStream extends ReadableStream
{
    private EventStream $eventStream;
    private string $eventType;
    
    public function __construct(EventStream $eventStream, string $eventType)
    {
        parent::__construct();
        $this->eventStream = $eventStream;
        $this->eventType = $eventType;
        
        $this->eventStream->subscribe($eventType, function($data) {
            $this->emit('data', [$data]);
        });
    }
}
```

## Best Practices

```php
// config/design-patterns-best-practices.tsk

design_patterns_best_practices = {
    domain_design = {
        use_ubiquitous_language = true
        define_bounded_contexts = true
        model_aggregates_properly = true
        use_value_objects = true
    }
    
    architecture = {
        follow_solid_principles = true
        use_dependency_injection = true
        separate_concerns = true
        maintain_low_coupling = true
    }
    
    patterns = {
        use_appropriate_patterns = true
        avoid_over_engineering = true
        keep_patterns_simple = true
        document_pattern_usage = true
    }
    
    testing = {
        test_domain_logic = true
        use_mock_objects = true
        test_integration = true
        maintain_test_coverage = true
    }
    
    performance = {
        optimize_critical_paths = true
        use_caching_strategies = true
        monitor_performance = true
        profile_regularly = true
    }
}

// Example usage in PHP
class DesignPatternsBestPractices
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
        $this->logger->info('Design pattern implemented', [
            'pattern' => 'CQRS',
            'operation' => 'CreateUser'
        ]);
    }
}
```

This comprehensive guide covers advanced design patterns in TuskLang with PHP integration. The design patterns are implemented with elegance, maintainability, and performance in mind while maintaining the rebellious spirit of TuskLang development. 
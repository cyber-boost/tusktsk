# Advanced Design Patterns

TuskLang enables PHP developers to implement sophisticated design patterns with elegance and power. This guide covers advanced design patterns, architectural solutions, and implementation strategies.

## Table of Contents
- [Creational Patterns](#creational-patterns)
- [Structural Patterns](#structural-patterns)
- [Behavioral Patterns](#behavioral-patterns)
- [Concurrency Patterns](#concurrency-patterns)
- [Integration Patterns](#integration-patterns)
- [Best Practices](#best-practices)

## Creational Patterns

```php
// config/creational-patterns.tsk
creational_patterns = {
    factory_pattern = {
        enabled = true
        types = ["simple_factory", "factory_method", "abstract_factory"]
        use_cases = ["database_connections", "payment_processors", "notification_services"]
    }
    
    builder_pattern = {
        enabled = true
        use_cases = ["complex_objects", "query_builders", "configuration_objects"]
        fluent_interface = true
    }
    
    singleton_pattern = {
        enabled = true
        use_cases = ["database_connections", "cache_managers", "configuration_managers"]
        thread_safe = true
    }
    
    prototype_pattern = {
        enabled = true
        use_cases = ["object_cloning", "template_objects", "cached_objects"]
        deep_cloning = true
    }
    
    object_pool_pattern = {
        enabled = true
        use_cases = ["database_connections", "http_clients", "expensive_objects"]
        pool_size = 10
        max_wait_time = 30
    }
}
```

```php
<?php
// app/DesignPatterns/Creational/AbstractFactory.php

namespace App\DesignPatterns\Creational;

use TuskLang\Factory\AbstractFactoryInterface;

abstract class AbstractFactory implements AbstractFactoryInterface
{
    abstract public function createDatabaseConnection(): DatabaseConnectionInterface;
    abstract public function createCacheManager(): CacheManagerInterface;
    abstract public function createLogger(): LoggerInterface;
    
    public static function create(string $type): AbstractFactory
    {
        return match($type) {
            'mysql' => new MySQLFactory(),
            'postgresql' => new PostgreSQLFactory(),
            'redis' => new RedisFactory(),
            default => throw new \InvalidArgumentException("Unknown factory type: {$type}")
        };
    }
}

class MySQLFactory extends AbstractFactory
{
    public function createDatabaseConnection(): DatabaseConnectionInterface
    {
        return new MySQLConnection();
    }
    
    public function createCacheManager(): CacheManagerInterface
    {
        return new MemcachedManager();
    }
    
    public function createLogger(): LoggerInterface
    {
        return new FileLogger();
    }
}

class PostgreSQLFactory extends AbstractFactory
{
    public function createDatabaseConnection(): DatabaseConnectionInterface
    {
        return new PostgreSQLConnection();
    }
    
    public function createCacheManager(): CacheManagerInterface
    {
        return new RedisManager();
    }
    
    public function createLogger(): LoggerInterface
    {
        return new DatabaseLogger();
    }
}

// app/DesignPatterns/Creational/Builder.php

namespace App\DesignPatterns\Creational;

class QueryBuilder
{
    private string $table;
    private array $select = ['*'];
    private array $where = [];
    private array $join = [];
    private array $orderBy = [];
    private ?int $limit = null;
    private ?int $offset = null;
    
    public function table(string $table): self
    {
        $this->table = $table;
        return $this;
    }
    
    public function select(array $columns): self
    {
        $this->select = $columns;
        return $this;
    }
    
    public function where(string $column, string $operator, $value): self
    {
        $this->where[] = [$column, $operator, $value];
        return $this;
    }
    
    public function join(string $table, string $first, string $operator, string $second): self
    {
        $this->join[] = [$table, $first, $operator, $second];
        return $this;
    }
    
    public function orderBy(string $column, string $direction = 'ASC'): self
    {
        $this->orderBy[] = [$column, $direction];
        return $this;
    }
    
    public function limit(int $limit): self
    {
        $this->limit = $limit;
        return $this;
    }
    
    public function offset(int $offset): self
    {
        $this->offset = $offset;
        return $this;
    }
    
    public function build(): string
    {
        $sql = "SELECT " . implode(', ', $this->select) . " FROM {$this->table}";
        
        if (!empty($this->join)) {
            foreach ($this->join as [$table, $first, $operator, $second]) {
                $sql .= " JOIN {$table} ON {$first} {$operator} {$second}";
            }
        }
        
        if (!empty($this->where)) {
            $conditions = [];
            foreach ($this->where as [$column, $operator, $value]) {
                $conditions[] = "{$column} {$operator} " . $this->quoteValue($value);
            }
            $sql .= " WHERE " . implode(' AND ', $conditions);
        }
        
        if (!empty($this->orderBy)) {
            $orders = [];
            foreach ($this->orderBy as [$column, $direction]) {
                $orders[] = "{$column} {$direction}";
            }
            $sql .= " ORDER BY " . implode(', ', $orders);
        }
        
        if ($this->limit !== null) {
            $sql .= " LIMIT {$this->limit}";
        }
        
        if ($this->offset !== null) {
            $sql .= " OFFSET {$this->offset}";
        }
        
        return $sql;
    }
    
    private function quoteValue($value): string
    {
        if (is_string($value)) {
            return "'" . addslashes($value) . "'";
        }
        return (string) $value;
    }
}

// app/DesignPatterns/Creational/ObjectPool.php

namespace App\DesignPatterns\Creational;

class ObjectPool
{
    private array $pool = [];
    private array $inUse = [];
    private int $maxSize;
    private int $maxWaitTime;
    
    public function __construct(int $maxSize = 10, int $maxWaitTime = 30)
    {
        $this->maxSize = $maxSize;
        $this->maxWaitTime = $maxWaitTime;
    }
    
    public function acquire(): mixed
    {
        $startTime = time();
        
        while (time() - $startTime < $this->maxWaitTime) {
            if (!empty($this->pool)) {
                $object = array_pop($this->pool);
                $this->inUse[] = $object;
                return $object;
            }
            
            if (count($this->inUse) < $this->maxSize) {
                $object = $this->createObject();
                $this->inUse[] = $object;
                return $object;
            }
            
            usleep(100000); // Sleep for 100ms
        }
        
        throw new \RuntimeException('Timeout waiting for available object');
    }
    
    public function release(mixed $object): void
    {
        $key = array_search($object, $this->inUse, true);
        
        if ($key !== false) {
            unset($this->inUse[$key]);
            $this->pool[] = $object;
        }
    }
    
    protected function createObject(): mixed
    {
        // Override in subclasses
        throw new \RuntimeException('createObject() must be implemented');
    }
    
    public function getPoolSize(): int
    {
        return count($this->pool);
    }
    
    public function getInUseSize(): int
    {
        return count($this->inUse);
    }
}

class DatabaseConnectionPool extends ObjectPool
{
    protected function createObject(): DatabaseConnection
    {
        return new DatabaseConnection();
    }
}
```

## Structural Patterns

```php
// config/structural-patterns.tsk
structural_patterns = {
    adapter_pattern = {
        enabled = true
        use_cases = ["legacy_systems", "third_party_apis", "different_interfaces"]
        bidirectional = true
    }
    
    bridge_pattern = {
        enabled = true
        use_cases = ["abstraction_implementation", "platform_independence", "feature_extensions"]
        multiple_implementations = true
    }
    
    composite_pattern = {
        enabled = true
        use_cases = ["file_systems", "ui_components", "organizational_structures"]
        tree_structure = true
    }
    
    decorator_pattern = {
        enabled = true
        use_cases = ["caching", "logging", "authentication", "validation"]
        dynamic_composition = true
    }
    
    facade_pattern = {
        enabled = true
        use_cases = ["complex_subsystems", "api_simplification", "legacy_integration"]
        simplified_interface = true
    }
    
    flyweight_pattern = {
        enabled = true
        use_cases = ["memory_optimization", "shared_objects", "caching"]
        intrinsic_extrinsic_state = true
    }
    
    proxy_pattern = {
        enabled = true
        use_cases = ["lazy_loading", "access_control", "caching", "logging"]
        virtual_proxy = true
        protection_proxy = true
        remote_proxy = true
    }
}
```

```php
<?php
// app/DesignPatterns/Structural/Adapter.php

namespace App\DesignPatterns\Structural;

interface PaymentProcessorInterface
{
    public function processPayment(float $amount, string $currency): bool;
    public function refundPayment(string $transactionId): bool;
}

class StripePaymentProcessor implements PaymentProcessorInterface
{
    public function processPayment(float $amount, string $currency): bool
    {
        // Stripe-specific implementation
        return true;
    }
    
    public function refundPayment(string $transactionId): bool
    {
        // Stripe-specific implementation
        return true;
    }
}

class PayPalPaymentProcessor implements PaymentProcessorInterface
{
    public function processPayment(float $amount, string $currency): bool
    {
        // PayPal-specific implementation
        return true;
    }
    
    public function refundPayment(string $transactionId): bool
    {
        // PayPal-specific implementation
        return true;
    }
}

// Legacy payment system
class LegacyPaymentSystem
{
    public function charge(float $amount, string $currency): bool
    {
        // Legacy implementation
        return true;
    }
    
    public function reverse(string $transactionId): bool
    {
        // Legacy implementation
        return true;
    }
}

// Adapter for legacy system
class LegacyPaymentAdapter implements PaymentProcessorInterface
{
    public function __construct(private LegacyPaymentSystem $legacySystem) {}
    
    public function processPayment(float $amount, string $currency): bool
    {
        return $this->legacySystem->charge($amount, $currency);
    }
    
    public function refundPayment(string $transactionId): bool
    {
        return $this->legacySystem->reverse($transactionId);
    }
}

// app/DesignPatterns/Structural/Decorator.php

namespace App\DesignPatterns\Structural;

interface CoffeeInterface
{
    public function getCost(): float;
    public function getDescription(): string;
}

class SimpleCoffee implements CoffeeInterface
{
    public function getCost(): float
    {
        return 2.0;
    }
    
    public function getDescription(): string
    {
        return 'Simple coffee';
    }
}

abstract class CoffeeDecorator implements CoffeeInterface
{
    protected CoffeeInterface $coffee;
    
    public function __construct(CoffeeInterface $coffee)
    {
        $this->coffee = $coffee;
    }
    
    public function getCost(): float
    {
        return $this->coffee->getCost();
    }
    
    public function getDescription(): string
    {
        return $this->coffee->getDescription();
    }
}

class MilkDecorator extends CoffeeDecorator
{
    public function getCost(): float
    {
        return parent::getCost() + 0.5;
    }
    
    public function getDescription(): string
    {
        return parent::getDescription() . ', milk';
    }
}

class SugarDecorator extends CoffeeDecorator
{
    public function getCost(): float
    {
        return parent::getCost() + 0.2;
    }
    
    public function getDescription(): string
    {
        return parent::getDescription() . ', sugar';
    }
}

class WhipDecorator extends CoffeeDecorator
{
    public function getCost(): float
    {
        return parent::getCost() + 0.7;
    }
    
    public function getDescription(): string
    {
        return parent::getDescription() . ', whip';
    }
}

// app/DesignPatterns/Structural/Proxy.php

namespace App\DesignPatterns\Structural;

interface ImageInterface
{
    public function display(): void;
}

class RealImage implements ImageInterface
{
    private string $filename;
    
    public function __construct(string $filename)
    {
        $this->filename = $filename;
        $this->loadFromDisk();
    }
    
    public function display(): void
    {
        echo "Displaying {$this->filename}\n";
    }
    
    private function loadFromDisk(): void
    {
        echo "Loading {$this->filename} from disk\n";
    }
}

class ProxyImage implements ImageInterface
{
    private ?RealImage $realImage = null;
    private string $filename;
    
    public function __construct(string $filename)
    {
        $this->filename = $filename;
    }
    
    public function display(): void
    {
        if ($this->realImage === null) {
            $this->realImage = new RealImage($this->filename);
        }
        $this->realImage->display();
    }
}
```

## Behavioral Patterns

```php
// config/behavioral-patterns.tsk
behavioral_patterns = {
    chain_of_responsibility = {
        enabled = true
        use_cases = ["request_processing", "error_handling", "middleware_pipelines"]
        dynamic_chain = true
    }
    
    command_pattern = {
        enabled = true
        use_cases = ["undo_redo", "queuing_operations", "remote_execution"]
        command_history = true
    }
    
    interpreter_pattern = {
        enabled = true
        use_cases = ["language_parsing", "expression_evaluation", "configuration_parsing"]
        abstract_syntax_tree = true
    }
    
    iterator_pattern = {
        enabled = true
        use_cases = ["collection_traversal", "custom_iteration", "lazy_loading"]
        multiple_iterators = true
    }
    
    mediator_pattern = {
        enabled = true
        use_cases = ["component_communication", "event_coordination", "system_decoupling"]
        centralized_control = true
    }
    
    memento_pattern = {
        enabled = true
        use_cases = ["undo_redo", "state_restoration", "checkpoint_system"]
        state_serialization = true
    }
    
    observer_pattern = {
        enabled = true
        use_cases = ["event_handling", "model_view_communication", "notification_systems"]
        push_pull_models = true
    }
    
    state_pattern = {
        enabled = true
        use_cases = ["state_machines", "workflow_management", "behavior_changes"]
        state_transitions = true
    }
    
    strategy_pattern = {
        enabled = true
        use_cases = ["algorithm_selection", "payment_methods", "sorting_strategies"]
        runtime_selection = true
    }
    
    template_method_pattern = {
        enabled = true
        use_cases = ["algorithm_frameworks", "code_reuse", "standardized_processes"]
        hook_methods = true
    }
    
    visitor_pattern = {
        enabled = true
        use_cases = ["object_structure_operations", "type_safe_operations", "separation_of_concerns"]
        double_dispatch = true
    }
}
```

```php
<?php
// app/DesignPatterns/Behavioral/ChainOfResponsibility.php

namespace App\DesignPatterns\Behavioral;

abstract class Handler
{
    protected ?Handler $nextHandler = null;
    
    public function setNext(Handler $handler): Handler
    {
        $this->nextHandler = $handler;
        return $handler;
    }
    
    abstract public function handle(Request $request): Response;
    
    protected function handleNext(Request $request): Response
    {
        if ($this->nextHandler !== null) {
            return $this->nextHandler->handle($request);
        }
        
        return new Response('No handler found', 404);
    }
}

class AuthenticationHandler extends Handler
{
    public function handle(Request $request): Response
    {
        if (!$this->isAuthenticated($request)) {
            return new Response('Unauthorized', 401);
        }
        
        return $this->handleNext($request);
    }
    
    private function isAuthenticated(Request $request): bool
    {
        return $request->hasHeader('Authorization');
    }
}

class AuthorizationHandler extends Handler
{
    public function handle(Request $request): Response
    {
        if (!$this->isAuthorized($request)) {
            return new Response('Forbidden', 403);
        }
        
        return $this->handleNext($request);
    }
    
    private function isAuthorized(Request $request): bool
    {
        return $request->getUserRole() === 'admin';
    }
}

class ValidationHandler extends Handler
{
    public function handle(Request $request): Response
    {
        if (!$this->isValid($request)) {
            return new Response('Bad Request', 400);
        }
        
        return $this->handleNext($request);
    }
    
    private function isValid(Request $request): bool
    {
        return !empty($request->getData());
    }
}

// app/DesignPatterns/Behavioral/Command.php

namespace App\DesignPatterns\Behavioral;

interface CommandInterface
{
    public function execute(): void;
    public function undo(): void;
}

class Light
{
    public function turnOn(): void
    {
        echo "Light is on\n";
    }
    
    public function turnOff(): void
    {
        echo "Light is off\n";
    }
}

class LightOnCommand implements CommandInterface
{
    public function __construct(private Light $light) {}
    
    public function execute(): void
    {
        $this->light->turnOn();
    }
    
    public function undo(): void
    {
        $this->light->turnOff();
    }
}

class LightOffCommand implements CommandInterface
{
    public function __construct(private Light $light) {}
    
    public function execute(): void
    {
        $this->light->turnOff();
    }
    
    public function undo(): void
    {
        $this->light->turnOn();
    }
}

class RemoteControl
{
    private array $commands = [];
    private array $undoCommands = [];
    
    public function setCommand(int $slot, CommandInterface $command): void
    {
        $this->commands[$slot] = $command;
    }
    
    public function pressButton(int $slot): void
    {
        if (isset($this->commands[$slot])) {
            $this->commands[$slot]->execute();
            $this->undoCommands[] = $this->commands[$slot];
        }
    }
    
    public function pressUndo(): void
    {
        if (!empty($this->undoCommands)) {
            $command = array_pop($this->undoCommands);
            $command->undo();
        }
    }
}

// app/DesignPatterns/Behavioral/Observer.php

namespace App\DesignPatterns\Behavioral;

interface ObserverInterface
{
    public function update(string $message): void;
}

interface SubjectInterface
{
    public function attach(ObserverInterface $observer): void;
    public function detach(ObserverInterface $observer): void;
    public function notify(string $message): void;
}

class NewsAgency implements SubjectInterface
{
    private array $observers = [];
    
    public function attach(ObserverInterface $observer): void
    {
        $this->observers[] = $observer;
    }
    
    public function detach(ObserverInterface $observer): void
    {
        $key = array_search($observer, $this->observers, true);
        if ($key !== false) {
            unset($this->observers[$key]);
        }
    }
    
    public function notify(string $message): void
    {
        foreach ($this->observers as $observer) {
            $observer->update($message);
        }
    }
    
    public function publishNews(string $news): void
    {
        $this->notify($news);
    }
}

class NewsChannel implements ObserverInterface
{
    private string $name;
    
    public function __construct(string $name)
    {
        $this->name = $name;
    }
    
    public function update(string $message): void
    {
        echo "{$this->name} received news: {$message}\n";
    }
}

// app/DesignPatterns/Behavioral/Strategy.php

namespace App\DesignPatterns\Behavioral;

interface PaymentStrategyInterface
{
    public function pay(float $amount): bool;
}

class CreditCardPayment implements PaymentStrategyInterface
{
    public function pay(float $amount): bool
    {
        echo "Paid {$amount} using Credit Card\n";
        return true;
    }
}

class PayPalPayment implements PaymentStrategyInterface
{
    public function pay(float $amount): bool
    {
        echo "Paid {$amount} using PayPal\n";
        return true;
    }
}

class BitcoinPayment implements PaymentStrategyInterface
{
    public function pay(float $amount): bool
    {
        echo "Paid {$amount} using Bitcoin\n";
        return true;
    }
}

class ShoppingCart
{
    private PaymentStrategyInterface $paymentStrategy;
    
    public function setPaymentStrategy(PaymentStrategyInterface $strategy): void
    {
        $this->paymentStrategy = $strategy;
    }
    
    public function checkout(float $amount): bool
    {
        return $this->paymentStrategy->pay($amount);
    }
}
```

## Concurrency Patterns

```php
// config/concurrency-patterns.tsk
concurrency_patterns = {
    active_object_pattern = {
        enabled = true
        use_cases = ["async_operations", "thread_safety", "method_execution"]
        message_queuing = true
    }
    
    balking_pattern = {
        enabled = true
        use_cases = ["resource_management", "state_validation", "conditional_execution"]
        state_checking = true
    }
    
    double_checked_locking = {
        enabled = true
        use_cases = ["singleton_initialization", "lazy_loading", "thread_safe_creation"]
        volatile_variables = true
    }
    
    guarded_suspension = {
        enabled = true
        use_cases = ["producer_consumer", "resource_waiting", "condition_synchronization"]
        condition_variables = true
    }
    
    leader_follower_pattern = {
        enabled = true
        use_cases = ["thread_pools", "event_handling", "load_distribution"]
        thread_coordination = true
    }
    
    monitor_pattern = {
        enabled = true
        use_cases = ["shared_resource_protection", "synchronization", "mutual_exclusion"]
        condition_variables = true
    }
    
    reactor_pattern = {
        enabled = true
        use_cases = ["event_handling", "non_blocking_io", "scalable_servers"]
        event_demultiplexing = true
    }
    
    thread_pool_pattern = {
        enabled = true
        use_cases = ["task_execution", "resource_management", "performance_optimization"]
        work_stealing = true
    }
}
```

## Integration Patterns

```php
// config/integration-patterns.tsk
integration_patterns = {
    adapter_pattern = {
        enabled = true
        use_cases = ["legacy_systems", "third_party_apis", "interface_compatibility"]
        bidirectional = true
    }
    
    bridge_pattern = {
        enabled = true
        use_cases = ["abstraction_implementation", "platform_independence", "feature_extensions"]
        multiple_implementations = true
    }
    
    facade_pattern = {
        enabled = true
        use_cases = ["complex_subsystems", "api_simplification", "legacy_integration"]
        simplified_interface = true
    }
    
    mediator_pattern = {
        enabled = true
        use_cases = ["component_communication", "event_coordination", "system_decoupling"]
        centralized_control = true
    }
    
    proxy_pattern = {
        enabled = true
        use_cases = ["remote_access", "caching", "access_control", "logging"]
        virtual_proxy = true
        protection_proxy = true
        remote_proxy = true
    }
}
```

## Best Practices

```php
// config/design-patterns-best-practices.tsk
design_patterns_best_practices = {
    pattern_selection = {
        understand_problem_domain = true
        choose_appropriate_pattern = true
        avoid_over_engineering = true
        consider_maintainability = true
    }
    
    implementation = {
        follow_pattern_structure = true
        maintain_single_responsibility = true
        use_dependency_injection = true
        implement_proper_abstractions = true
    }
    
    testing = {
        test_pattern_behavior = true
        use_mock_objects = true
        test_pattern_interactions = true
        maintain_test_coverage = true
    }
    
    documentation = {
        document_pattern_usage = true
        explain_pattern_rationale = true
        provide_usage_examples = true
        maintain_pattern_catalog = true
    }
    
    performance = {
        consider_pattern_overhead = true
        optimize_critical_paths = true
        use_lazy_loading = true
        implement_caching = true
    }
    
    security = {
        validate_pattern_inputs = true
        implement_access_controls = true
        use_secure_communication = true
        audit_pattern_usage = true
    }
}

// Example usage in PHP
class DesignPatternsBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Use appropriate patterns for the problem
        $factory = AbstractFactory::create('mysql');
        $connection = $factory->createDatabaseConnection();
        
        // 2. Implement decorator pattern for cross-cutting concerns
        $coffee = new SimpleCoffee();
        $coffee = new MilkDecorator($coffee);
        $coffee = new SugarDecorator($coffee);
        
        // 3. Use command pattern for undo/redo functionality
        $remote = new RemoteControl();
        $light = new Light();
        $remote->setCommand(1, new LightOnCommand($light));
        $remote->pressButton(1);
        $remote->pressUndo();
        
        // 4. Implement observer pattern for event handling
        $newsAgency = new NewsAgency();
        $channel1 = new NewsChannel('CNN');
        $channel2 = new NewsChannel('BBC');
        $newsAgency->attach($channel1);
        $newsAgency->attach($channel2);
        $newsAgency->publishNews('Breaking news!');
        
        // 5. Use strategy pattern for algorithm selection
        $cart = new ShoppingCart();
        $cart->setPaymentStrategy(new CreditCardPayment());
        $cart->checkout(100.0);
        
        // 6. Implement chain of responsibility for request processing
        $authHandler = new AuthenticationHandler();
        $authzHandler = new AuthorizationHandler();
        $validationHandler = new ValidationHandler();
        
        $authHandler->setNext($authzHandler)->setNext($validationHandler);
        
        // 7. Use object pool for expensive resources
        $pool = new DatabaseConnectionPool(10, 30);
        $connection = $pool->acquire();
        // Use connection...
        $pool->release($connection);
        
        // 8. Log and monitor pattern usage
        $this->logger->info('Design patterns implemented', [
            'patterns' => ['Factory', 'Decorator', 'Command', 'Observer', 'Strategy'],
            'benefits' => ['Maintainability', 'Flexibility', 'Reusability', 'Testability']
        ]);
    }
}
```

This comprehensive guide covers advanced design patterns in TuskLang with PHP integration. The design patterns system is designed to be flexible, maintainable, and powerful while maintaining the rebellious spirit of TuskLang development. 
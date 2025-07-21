# Advanced Dependency Injection

TuskLang enables PHP developers to implement sophisticated dependency injection patterns with elegance and power. This guide covers advanced DI patterns, container management, and service lifecycle strategies.

## Table of Contents
- [Container Configuration](#container-configuration)
- [Service Registration](#service-registration)
- [Lifecycle Management](#lifecycle-management)
- [Circular Dependencies](#circular-dependencies)
- [Lazy Loading](#lazy-loading)
- [Best Practices](#best-practices)

## Container Configuration

```php
// config/dependency-injection.tsk
dependency_injection = {
    container = {
        type = "psr_container"
        auto_wiring = true
        reflection_cache = true
        parameter_resolution = true
    }
    
    services = {
        scopes = {
            singleton = true
            transient = true
            request = true
            session = true
        }
        
        factories = {
            enabled = true
            auto_discovery = true
            custom_factories = true
        }
        
        decorators = {
            enabled = true
            automatic_decorators = true
            custom_decorators = true
        }
    }
    
    configuration = {
        config_files = ["services.tsk", "factories.tsk", "decorators.tsk"]
        auto_scan_directories = ["app/Services", "app/Repositories", "app/Handlers"]
        environment_specific = true
        validation = true
    }
    
    performance = {
        service_cache = true
        reflection_cache = true
        parameter_cache = true
        optimization_level = "high"
    }
}
```

## Service Registration

```php
<?php
// app/Infrastructure/DI/Container.php

namespace App\Infrastructure\DI;

use TuskLang\DI\ContainerInterface;
use TuskLang\DI\ServiceDefinition;
use TuskLang\DI\ServiceProviderInterface;
use ReflectionClass;
use ReflectionParameter;

class Container implements ContainerInterface
{
    private array $services = [];
    private array $instances = [];
    private array $factories = [];
    private array $decorators = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->loadConfiguration();
    }
    
    public function register(string $id, $concrete = null, array $options = []): ServiceDefinition
    {
        $definition = new ServiceDefinition($id, $concrete, $options);
        $this->services[$id] = $definition;
        
        return $definition;
    }
    
    public function get(string $id): mixed
    {
        if (isset($this->instances[$id])) {
            return $this->instances[$id];
        }
        
        if (!isset($this->services[$id])) {
            throw new \RuntimeException("Service '{$id}' not found");
        }
        
        $definition = $this->services[$id];
        $instance = $this->resolve($definition);
        
        if ($definition->isSingleton()) {
            $this->instances[$id] = $instance;
        }
        
        return $instance;
    }
    
    public function has(string $id): bool
    {
        return isset($this->services[$id]) || isset($this->instances[$id]);
    }
    
    public function registerFactory(string $id, callable $factory): void
    {
        $this->factories[$id] = $factory;
    }
    
    public function registerDecorator(string $id, callable $decorator): void
    {
        $this->decorators[$id] = $decorator;
    }
    
    public function registerServiceProvider(ServiceProviderInterface $provider): void
    {
        $provider->register($this);
    }
    
    private function resolve(ServiceDefinition $definition): mixed
    {
        $concrete = $definition->getConcrete();
        
        if (is_callable($concrete)) {
            return $this->resolveCallable($concrete, $definition->getArguments());
        }
        
        if (is_string($concrete)) {
            return $this->resolveClass($concrete, $definition->getArguments());
        }
        
        return $concrete;
    }
    
    private function resolveClass(string $class, array $arguments = []): object
    {
        $reflection = new ReflectionClass($class);
        
        if (!$reflection->isInstantiable()) {
            throw new \RuntimeException("Class '{$class}' is not instantiable");
        }
        
        $constructor = $reflection->getConstructor();
        
        if ($constructor === null) {
            return new $class();
        }
        
        $parameters = $this->resolveParameters($constructor->getParameters(), $arguments);
        
        return $reflection->newInstanceArgs($parameters);
    }
    
    private function resolveCallable(callable $callable, array $arguments = []): mixed
    {
        if (is_array($callable)) {
            $reflection = new \ReflectionMethod($callable[0], $callable[1]);
        } else {
            $reflection = new \ReflectionFunction($callable);
        }
        
        $parameters = $this->resolveParameters($reflection->getParameters(), $arguments);
        
        return call_user_func_array($callable, $parameters);
    }
    
    private function resolveParameters(array $parameters, array $arguments = []): array
    {
        $resolved = [];
        
        foreach ($parameters as $parameter) {
            $name = $parameter->getName();
            
            if (isset($arguments[$name])) {
                $resolved[] = $arguments[$name];
                continue;
            }
            
            if (isset($arguments[$parameter->getPosition()])) {
                $resolved[] = $arguments[$parameter->getPosition()];
                continue;
            }
            
            if ($parameter->isDefaultValueAvailable()) {
                $resolved[] = $parameter->getDefaultValue();
                continue;
            }
            
            $type = $parameter->getType();
            
            if ($type && !$type->isBuiltin()) {
                $typeName = $type->getName();
                
                if ($this->has($typeName)) {
                    $resolved[] = $this->get($typeName);
                    continue;
                }
                
                if (class_exists($typeName)) {
                    $resolved[] = $this->resolveClass($typeName);
                    continue;
                }
            }
            
            throw new \RuntimeException("Cannot resolve parameter '{$name}'");
        }
        
        return $resolved;
    }
    
    private function loadConfiguration(): void
    {
        if (isset($this->config['services'])) {
            foreach ($this->config['services'] as $id => $service) {
                $this->registerServiceFromConfig($id, $service);
            }
        }
        
        if (isset($this->config['factories'])) {
            foreach ($this->config['factories'] as $id => $factory) {
                $this->registerFactoryFromConfig($id, $factory);
            }
        }
        
        if (isset($this->config['decorators'])) {
            foreach ($this->config['decorators'] as $id => $decorator) {
                $this->registerDecoratorFromConfig($id, $decorator);
            }
        }
    }
    
    private function registerServiceFromConfig(string $id, array $service): void
    {
        $definition = $this->register($id, $service['class'] ?? null);
        
        if (isset($service['arguments'])) {
            $definition->withArguments($service['arguments']);
        }
        
        if (isset($service['calls'])) {
            foreach ($service['calls'] as $call) {
                $definition->addMethodCall($call['method'], $call['arguments'] ?? []);
            }
        }
        
        if (isset($service['tags'])) {
            foreach ($service['tags'] as $tag) {
                $definition->addTag($tag);
            }
        }
        
        if (isset($service['scope'])) {
            $definition->withScope($service['scope']);
        }
    }
    
    private function registerFactoryFromConfig(string $id, array $factory): void
    {
        $factoryCallable = $factory['factory'];
        
        if (is_string($factoryCallable)) {
            $factoryCallable = [$this->get($factoryCallable), 'create'];
        }
        
        $this->registerFactory($id, $factoryCallable);
    }
    
    private function registerDecoratorFromConfig(string $id, array $decorator): void
    {
        $decoratorCallable = $decorator['decorator'];
        
        if (is_string($decoratorCallable)) {
            $decoratorCallable = [$this->get($decoratorCallable), 'decorate'];
        }
        
        $this->registerDecorator($id, $decoratorCallable);
    }
}

// app/Infrastructure/DI/ServiceDefinition.php

namespace App\Infrastructure\DI;

class ServiceDefinition
{
    private string $id;
    private mixed $concrete;
    private array $arguments = [];
    private array $methodCalls = [];
    private array $tags = [];
    private string $scope = 'singleton';
    private bool $lazy = false;
    
    public function __construct(string $id, mixed $concrete = null, array $options = [])
    {
        $this->id = $id;
        $this->concrete = $concrete ?? $id;
        
        if (isset($options['arguments'])) {
            $this->arguments = $options['arguments'];
        }
        
        if (isset($options['scope'])) {
            $this->scope = $options['scope'];
        }
        
        if (isset($options['lazy'])) {
            $this->lazy = $options['lazy'];
        }
    }
    
    public function withArguments(array $arguments): self
    {
        $this->arguments = $arguments;
        return $this;
    }
    
    public function addMethodCall(string $method, array $arguments = []): self
    {
        $this->methodCalls[] = ['method' => $method, 'arguments' => $arguments];
        return $this;
    }
    
    public function addTag(string $tag): self
    {
        $this->tags[] = $tag;
        return $this;
    }
    
    public function withScope(string $scope): self
    {
        $this->scope = $scope;
        return $this;
    }
    
    public function asLazy(): self
    {
        $this->lazy = true;
        return $this;
    }
    
    public function getId(): string
    {
        return $this->id;
    }
    
    public function getConcrete(): mixed
    {
        return $this->concrete;
    }
    
    public function getArguments(): array
    {
        return $this->arguments;
    }
    
    public function getMethodCalls(): array
    {
        return $this->methodCalls;
    }
    
    public function getTags(): array
    {
        return $this->tags;
    }
    
    public function getScope(): string
    {
        return $this->scope;
    }
    
    public function isLazy(): bool
    {
        return $this->lazy;
    }
    
    public function isSingleton(): bool
    {
        return $this->scope === 'singleton';
    }
}

// app/Infrastructure/DI/ServiceProvider.php

namespace App\Infrastructure\DI;

use TuskLang\DI\ServiceProviderInterface;

class ServiceProvider implements ServiceProviderInterface
{
    public function register(ContainerInterface $container): void
    {
        // Register core services
        $container->register('config', Config::class)
            ->withArguments(['config/app.tsk']);
        
        $container->register('logger', Logger::class)
            ->withArguments(['@config'])
            ->addTag('monitoring');
        
        $container->register('database', DatabaseConnection::class)
            ->withArguments(['@config'])
            ->addTag('persistence');
        
        // Register repositories
        $container->register('user_repository', UserRepository::class)
            ->withArguments(['@database'])
            ->addTag('repository');
        
        $container->register('order_repository', OrderRepository::class)
            ->withArguments(['@database'])
            ->addTag('repository');
        
        // Register services
        $container->register('user_service', UserService::class)
            ->withArguments(['@user_repository', '@logger'])
            ->addTag('service');
        
        $container->register('order_service', OrderService::class)
            ->withArguments(['@order_repository', '@user_service', '@logger'])
            ->addTag('service');
        
        // Register controllers
        $container->register('user_controller', UserController::class)
            ->withArguments(['@user_service'])
            ->addTag('controller');
        
        $container->register('order_controller', OrderController::class)
            ->withArguments(['@order_service'])
            ->addTag('controller');
        
        // Register factories
        $container->registerFactory('email_service', function(ContainerInterface $container) {
            $config = $container->get('config');
            return new EmailService($config->get('email'));
        });
        
        // Register decorators
        $container->registerDecorator('user_service', function($service, ContainerInterface $container) {
            return new CachedUserService($service, $container->get('cache'));
        });
    }
}
```

## Lifecycle Management

```php
<?php
// app/Infrastructure/DI/LifecycleManager.php

namespace App\Infrastructure\DI;

use TuskLang\DI\LifecycleInterface;

class LifecycleManager
{
    private array $lifecycleServices = [];
    private array $startupCallbacks = [];
    private array $shutdownCallbacks = [];
    
    public function registerLifecycleService(string $id, LifecycleInterface $service): void
    {
        $this->lifecycleServices[$id] = $service;
    }
    
    public function addStartupCallback(callable $callback): void
    {
        $this->startupCallbacks[] = $callback;
    }
    
    public function addShutdownCallback(callable $callback): void
    {
        $this->shutdownCallbacks[] = $callback;
    }
    
    public function startup(): void
    {
        foreach ($this->startupCallbacks as $callback) {
            $callback();
        }
        
        foreach ($this->lifecycleServices as $service) {
            $service->startup();
        }
    }
    
    public function shutdown(): void
    {
        foreach ($this->lifecycleServices as $service) {
            $service->shutdown();
        }
        
        foreach ($this->shutdownCallbacks as $callback) {
            $callback();
        }
    }
}

// app/Infrastructure/DI/LifecycleInterface.php

namespace App\Infrastructure\DI;

interface LifecycleInterface
{
    public function startup(): void;
    public function shutdown(): void;
}

// app/Infrastructure/Services/DatabaseService.php

namespace App\Infrastructure\Services;

use App\Infrastructure\DI\LifecycleInterface;

class DatabaseService implements LifecycleInterface
{
    private $connection;
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
    }
    
    public function startup(): void
    {
        $this->connection = new \PDO(
            $this->config['dsn'],
            $this->config['username'],
            $this->config['password'],
            $this->config['options'] ?? []
        );
        
        $this->connection->setAttribute(\PDO::ATTR_ERRMODE, \PDO::ERRMODE_EXCEPTION);
    }
    
    public function shutdown(): void
    {
        if ($this->connection) {
            $this->connection = null;
        }
    }
    
    public function getConnection(): \PDO
    {
        return $this->connection;
    }
}
```

## Circular Dependencies

```php
<?php
// app/Infrastructure/DI/CircularDependencyResolver.php

namespace App\Infrastructure\DI;

class CircularDependencyResolver
{
    private array $resolving = [];
    private array $resolved = [];
    
    public function resolve(string $id, callable $resolver): mixed
    {
        if (isset($this->resolved[$id])) {
            return $this->resolved[$id];
        }
        
        if (isset($this->resolving[$id])) {
            throw new \RuntimeException("Circular dependency detected for service '{$id}'");
        }
        
        $this->resolving[$id] = true;
        
        try {
            $instance = $resolver();
            $this->resolved[$id] = $instance;
            return $instance;
        } finally {
            unset($this->resolving[$id]);
        }
    }
    
    public function detectCircularDependencies(array $dependencies): array
    {
        $cycles = [];
        $visited = [];
        $recStack = [];
        
        foreach ($dependencies as $service => $deps) {
            if (!isset($visited[$service])) {
                $this->dfs($service, $dependencies, $visited, $recStack, $cycles);
            }
        }
        
        return $cycles;
    }
    
    private function dfs(string $service, array $dependencies, array &$visited, array &$recStack, array &$cycles): void
    {
        $visited[$service] = true;
        $recStack[$service] = true;
        
        if (isset($dependencies[$service])) {
            foreach ($dependencies[$service] as $dep) {
                if (!isset($visited[$dep])) {
                    $this->dfs($dep, $dependencies, $visited, $recStack, $cycles);
                } elseif (isset($recStack[$dep])) {
                    $cycles[] = $this->findCycle($service, $dep, $dependencies);
                }
            }
        }
        
        unset($recStack[$service]);
    }
    
    private function findCycle(string $start, string $end, array $dependencies): array
    {
        $cycle = [$start];
        $current = $start;
        
        while ($current !== $end) {
            foreach ($dependencies[$current] as $dep) {
                if (isset($dependencies[$dep])) {
                    $current = $dep;
                    $cycle[] = $dep;
                    break;
                }
            }
        }
        
        return $cycle;
    }
}
```

## Lazy Loading

```php
<?php
// app/Infrastructure/DI/LazyService.php

namespace App\Infrastructure\DI;

class LazyService
{
    private mixed $instance = null;
    private callable $factory;
    private array $arguments;
    
    public function __construct(callable $factory, array $arguments = [])
    {
        $this->factory = $factory;
        $this->arguments = $arguments;
    }
    
    public function getInstance(): mixed
    {
        if ($this->instance === null) {
            $this->instance = call_user_func_array($this->factory, $this->arguments);
        }
        
        return $this->instance;
    }
    
    public function __call(string $method, array $arguments): mixed
    {
        $instance = $this->getInstance();
        
        if (method_exists($instance, $method)) {
            return $instance->$method(...$arguments);
        }
        
        throw new \RuntimeException("Method '{$method}' not found on lazy service");
    }
}

// app/Infrastructure/DI/LazyServiceProxy.php

namespace App\Infrastructure\DI;

class LazyServiceProxy
{
    private string $serviceId;
    private ContainerInterface $container;
    private mixed $instance = null;
    
    public function __construct(string $serviceId, ContainerInterface $container)
    {
        $this->serviceId = $serviceId;
        $this->container = $container;
    }
    
    public function __call(string $method, array $arguments): mixed
    {
        if ($this->instance === null) {
            $this->instance = $this->container->get($this->serviceId);
        }
        
        if (method_exists($this->instance, $method)) {
            return $this->instance->$method(...$arguments);
        }
        
        throw new \RuntimeException("Method '{$method}' not found on service '{$this->serviceId}'");
    }
    
    public function __get(string $property): mixed
    {
        if ($this->instance === null) {
            $this->instance = $this->container->get($this->serviceId);
        }
        
        if (property_exists($this->instance, $property)) {
            return $this->instance->$property;
        }
        
        throw new \RuntimeException("Property '{$property}' not found on service '{$this->serviceId}'");
    }
    
    public function __set(string $property, mixed $value): void
    {
        if ($this->instance === null) {
            $this->instance = $this->container->get($this->serviceId);
        }
        
        if (property_exists($this->instance, $property)) {
            $this->instance->$property = $value;
            return;
        }
        
        throw new \RuntimeException("Property '{$property}' not found on service '{$this->serviceId}'");
    }
}
```

## Best Practices

```php
// config/di-best-practices.tsk
di_best_practices = {
    service_registration = {
        use_interface_abstraction = true
        register_services_by_interface = true
        use_service_providers = true
        organize_services_by_module = true
    }
    
    dependency_management = {
        minimize_dependencies = true
        use_constructor_injection = true
        avoid_property_injection = true
        use_method_injection_sparingly = true
    }
    
    lifecycle_management = {
        manage_service_lifecycles = true
        implement_startup_shutdown = true
        handle_circular_dependencies = true
        use_lazy_loading = true
    }
    
    performance = {
        cache_service_definitions = true
        optimize_resolution = true
        use_compiled_containers = true
        minimize_reflection_usage = true
    }
    
    testing = {
        use_mock_services = true
        test_service_configuration = true
        verify_dependency_resolution = true
        test_service_lifecycles = true
    }
    
    security = {
        validate_service_configuration = true
        restrict_service_access = true
        audit_service_usage = true
        implement_service_isolation = true
    }
}

// Example usage in PHP
class DependencyInjectionBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Configure container
        $config = TuskLang::load('dependency_injection');
        $container = new Container($config);
        
        // 2. Register service provider
        $provider = new ServiceProvider();
        $container->registerServiceProvider($provider);
        
        // 3. Register lifecycle services
        $lifecycleManager = new LifecycleManager();
        $lifecycleManager->registerLifecycleService('database', new DatabaseService($config['database']));
        
        // 4. Handle circular dependencies
        $circularResolver = new CircularDependencyResolver();
        $cycles = $circularResolver->detectCircularDependencies($this->getDependencies());
        
        if (!empty($cycles)) {
            $this->logger->warning('Circular dependencies detected', ['cycles' => $cycles]);
        }
        
        // 5. Use lazy loading for expensive services
        $container->register('expensive_service', ExpensiveService::class)
            ->asLazy();
        
        // 6. Register decorators for cross-cutting concerns
        $container->registerDecorator('user_service', function($service, $container) {
            return new CachedUserService($service, $container->get('cache'));
        });
        
        // 7. Start lifecycle services
        $lifecycleManager->startup();
        
        // 8. Use services
        $userService = $container->get('user_service');
        $orderService = $container->get('order_service');
        
        // 9. Shutdown lifecycle services
        register_shutdown_function([$lifecycleManager, 'shutdown']);
        
        // 10. Log and monitor
        $this->logger->info('Dependency injection configured', [
            'services' => count($container->getServices()),
            'lifecycle_services' => count($lifecycleManager->getLifecycleServices()),
            'circular_dependencies' => count($cycles)
        ]);
    }
    
    private function getDependencies(): array
    {
        return [
            'user_service' => ['user_repository', 'logger'],
            'order_service' => ['order_repository', 'user_service', 'logger'],
            'user_repository' => ['database'],
            'order_repository' => ['database']
        ];
    }
}
```

This comprehensive guide covers advanced dependency injection in TuskLang with PHP integration. The DI system is designed to be flexible, performant, and maintainable while maintaining the rebellious spirit of TuskLang development. 
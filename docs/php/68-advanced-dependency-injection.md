# Advanced Dependency Injection

TuskLang provides sophisticated dependency injection capabilities that go beyond basic container management. This guide covers advanced DI patterns, service lifecycle management, and integration with PHP frameworks.

## Table of Contents
- [Service Container Configuration](#service-container-configuration)
- [Service Lifecycle Management](#service-lifecycle-management)
- [Factory Patterns](#factory-patterns)
- [Lazy Loading](#lazy-loading)
- [Service Decorators](#service-decorators)
- [Conditional Services](#conditional-services)
- [Best Practices](#best-practices)

## Service Container Configuration

```php
// config/container.tsk
dependency_injection = {
    services = {
        user_repository = {
            class = "App\\Infrastructure\\Repositories\\UserRepository"
            arguments = ["@database_connection"]
            tags = ["repository", "user"]
        }
        
        user_service = {
            class = "App\\Application\\Services\\UserService"
            arguments = ["@user_repository", "@password_hasher", "@email_service"]
            tags = ["service", "user"]
        }
        
        password_hasher = {
            class = "App\\Infrastructure\\Services\\PasswordHasher"
            arguments = ["@config.password_algo"]
            tags = ["service", "security"]
        }
        
        email_service = {
            class = "App\\Infrastructure\\Services\\EmailService"
            arguments = ["@mailer", "@config.email_settings"]
            tags = ["service", "communication"]
        }
        
        database_connection = {
            class = "App\\Infrastructure\\Database\\Connection"
            arguments = ["@config.database"]
            tags = ["database"]
        }
    }
    
    parameters = {
        password_algo = "PASSWORD_ARGON2ID"
        email_settings = {
            from_address = "noreply@example.com"
            from_name = "My Application"
        }
    }
    
    aliases = {
        "App\\Domain\\Repositories\\UserRepositoryInterface" = "@user_repository"
        "App\\Domain\\Services\\PasswordHasherInterface" = "@password_hasher"
        "App\\Domain\\Services\\EmailServiceInterface" = "@email_service"
    }
}
```

## Service Lifecycle Management

```php
<?php
// app/Container/AdvancedContainer.php

namespace App\Container;

use TuskLang\Container\ContainerInterface;
use TuskLang\Container\ServiceDefinition;

class AdvancedContainer implements ContainerInterface
{
    private array $services = [];
    private array $instances = [];
    private array $aliases = [];
    private array $tags = [];
    private array $parameters = [];
    
    public function __construct(array $config)
    {
        $this->loadConfiguration($config);
    }
    
    public function get(string $id): object
    {
        // Check if service is already instantiated
        if (isset($this->instances[$id])) {
            return $this->instances[$id];
        }
        
        // Check aliases
        if (isset($this->aliases[$id])) {
            return $this->get($this->aliases[$id]);
        }
        
        // Get service definition
        if (!isset($this->services[$id])) {
            throw new \RuntimeException("Service '{$id}' not found");
        }
        
        $definition = $this->services[$id];
        $instance = $this->createInstance($definition);
        
        // Store singleton instances
        if ($definition['singleton'] ?? true) {
            $this->instances[$id] = $instance;
        }
        
        return $instance;
    }
    
    public function has(string $id): bool
    {
        return isset($this->services[$id]) || isset($this->aliases[$id]);
    }
    
    public function set(string $id, object $service): void
    {
        $this->instances[$id] = $service;
    }
    
    public function getByTag(string $tag): array
    {
        $services = [];
        
        foreach ($this->tags[$tag] ?? [] as $serviceId) {
            $services[] = $this->get($serviceId);
        }
        
        return $services;
    }
    
    public function getParameter(string $name): mixed
    {
        if (!isset($this->parameters[$name])) {
            throw new \RuntimeException("Parameter '{$name}' not found");
        }
        
        return $this->parameters[$name];
    }
    
    private function loadConfiguration(array $config): void
    {
        $this->services = $config['services'] ?? [];
        $this->aliases = $config['aliases'] ?? [];
        $this->parameters = $config['parameters'] ?? [];
        
        // Build tag index
        foreach ($this->services as $id => $definition) {
            $tags = $definition['tags'] ?? [];
            foreach ($tags as $tag) {
                if (!isset($this->tags[$tag])) {
                    $this->tags[$tag] = [];
                }
                $this->tags[$tag][] = $id;
            }
        }
    }
    
    private function createInstance(array $definition): object
    {
        $class = $definition['class'];
        $arguments = $this->resolveArguments($definition['arguments'] ?? []);
        
        return new $class(...$arguments);
    }
    
    private function resolveArguments(array $arguments): array
    {
        $resolved = [];
        
        foreach ($arguments as $argument) {
            if (is_string($argument) && str_starts_with($argument, '@')) {
                $serviceId = substr($argument, 1);
                $resolved[] = $this->get($serviceId);
            } elseif (is_string($argument) && str_starts_with($argument, '%')) {
                $paramName = substr($argument, 1, -1);
                $resolved[] = $this->getParameter($paramName);
            } else {
                $resolved[] = $argument;
            }
        }
        
        return $resolved;
    }
}
```

## Factory Patterns

```php
<?php
// app/Container/ServiceFactory.php

namespace App\Container;

class ServiceFactory
{
    private ContainerInterface $container;
    
    public function __construct(ContainerInterface $container)
    {
        $this->container = $container;
    }
    
    public function createUserService(): UserService
    {
        return new UserService(
            $this->container->get('user_repository'),
            $this->container->get('password_hasher'),
            $this->container->get('email_service')
        );
    }
    
    public function createOrderService(): OrderService
    {
        return new OrderService(
            $this->container->get('order_repository'),
            $this->container->get('payment_service'),
            $this->container->get('inventory_service')
        );
    }
    
    public function createDatabaseConnection(array $config): Connection
    {
        return new Connection(
            $config['host'],
            $config['port'],
            $config['database'],
            $config['username'],
            $config['password']
        );
    }
    
    public function createCacheService(string $driver, array $config): CacheInterface
    {
        switch ($driver) {
            case 'redis':
                return new RedisCache($config);
            case 'memcached':
                return new MemcachedCache($config);
            default:
                return new FileCache($config);
        }
    }
}

// app/Container/AbstractFactory.php

namespace App\Container;

abstract class AbstractFactory
{
    protected ContainerInterface $container;
    
    public function __construct(ContainerInterface $container)
    {
        $this->container = $container;
    }
    
    abstract public function create(string $type, array $config = []): object;
}

class RepositoryFactory extends AbstractFactory
{
    public function create(string $type, array $config = []): RepositoryInterface
    {
        $connection = $this->container->get('database_connection');
        
        switch ($type) {
            case 'user':
                return new UserRepository($connection);
            case 'order':
                return new OrderRepository($connection);
            case 'product':
                return new ProductRepository($connection);
            default:
                throw new \InvalidArgumentException("Unknown repository type: {$type}");
        }
    }
}
```

## Lazy Loading

```php
<?php
// app/Container/LazyService.php

namespace App\Container;

class LazyService
{
    private ContainerInterface $container;
    private string $serviceId;
    private ?object $instance = null;
    
    public function __construct(ContainerInterface $container, string $serviceId)
    {
        $this->container = $container;
        $this->serviceId = $serviceId;
    }
    
    public function __call(string $method, array $arguments)
    {
        if ($this->instance === null) {
            $this->instance = $this->container->get($this->serviceId);
        }
        
        return $this->instance->$method(...$arguments);
    }
    
    public function getInstance(): object
    {
        if ($this->instance === null) {
            $this->instance = $this->container->get($this->serviceId);
        }
        
        return $this->instance;
    }
}

// app/Container/LazyServiceProxy.php

namespace App\Container;

class LazyServiceProxy
{
    private \Closure $factory;
    private ?object $instance = null;
    
    public function __construct(\Closure $factory)
    {
        $this->factory = $factory;
    }
    
    public function __call(string $method, array $arguments)
    {
        if ($this->instance === null) {
            $this->instance = ($this->factory)();
        }
        
        return $this->instance->$method(...$arguments);
    }
    
    public function getInstance(): object
    {
        if ($this->instance === null) {
            $this->instance = ($this->factory)();
        }
        
        return $this->instance;
    }
}
```

## Service Decorators

```php
<?php
// app/Container/ServiceDecorator.php

namespace App\Container;

class ServiceDecorator
{
    private ContainerInterface $container;
    
    public function __construct(ContainerInterface $container)
    {
        $this->container = $container;
    }
    
    public function decorate(string $serviceId, callable $decorator): void
    {
        $originalService = $this->container->get($serviceId);
        $decoratedService = $decorator($originalService);
        
        $this->container->set($serviceId, $decoratedService);
    }
    
    public function addLogging(string $serviceId): void
    {
        $this->decorate($serviceId, function($service) {
            return new LoggingDecorator($service, $this->container->get('logger'));
        });
    }
    
    public function addCaching(string $serviceId): void
    {
        $this->decorate($serviceId, function($service) {
            return new CachingDecorator($service, $this->container->get('cache'));
        });
    }
    
    public function addMetrics(string $serviceId): void
    {
        $this->decorate($serviceId, function($service) {
            return new MetricsDecorator($service, $this->container->get('metrics'));
        });
    }
}

// app/Container/Decorators/LoggingDecorator.php

namespace App\Container\Decorators;

class LoggingDecorator
{
    private object $service;
    private LoggerInterface $logger;
    
    public function __construct(object $service, LoggerInterface $logger)
    {
        $this->service = $service;
        $this->logger = $logger;
    }
    
    public function __call(string $method, array $arguments)
    {
        $startTime = microtime(true);
        
        try {
            $result = $this->service->$method(...$arguments);
            
            $this->logger->info("Method {$method} completed successfully", [
                'method' => $method,
                'duration' => microtime(true) - $startTime
            ]);
            
            return $result;
        } catch (\Exception $e) {
            $this->logger->error("Method {$method} failed", [
                'method' => $method,
                'error' => $e->getMessage(),
                'duration' => microtime(true) - $startTime
            ]);
            
            throw $e;
        }
    }
}

// app/Container/Decorators/CachingDecorator.php

namespace App\Container\Decorators;

class CachingDecorator
{
    private object $service;
    private CacheInterface $cache;
    private int $ttl;
    
    public function __construct(object $service, CacheInterface $cache, int $ttl = 3600)
    {
        $this->service = $service;
        $this->cache = $cache;
        $this->ttl = $ttl;
    }
    
    public function __call(string $method, array $arguments)
    {
        $cacheKey = $this->generateCacheKey($method, $arguments);
        
        // Try to get from cache first
        $cached = $this->cache->get($cacheKey);
        if ($cached !== null) {
            return $cached;
        }
        
        // Call original service
        $result = $this->service->$method(...$arguments);
        
        // Cache the result
        $this->cache->set($cacheKey, $result, $this->ttl);
        
        return $result;
    }
    
    private function generateCacheKey(string $method, array $arguments): string
    {
        return md5(get_class($this->service) . $method . serialize($arguments));
    }
}
```

## Conditional Services

```php
<?php
// app/Container/ConditionalServiceProvider.php

namespace App\Container;

class ConditionalServiceProvider
{
    private ContainerInterface $container;
    private array $conditions = [];
    
    public function __construct(ContainerInterface $container)
    {
        $this->container = $container;
    }
    
    public function registerConditional(string $serviceId, callable $condition, callable $factory): void
    {
        $this->conditions[$serviceId] = [
            'condition' => $condition,
            'factory' => $factory
        ];
    }
    
    public function getConditional(string $serviceId): object
    {
        if (!isset($this->conditions[$serviceId])) {
            throw new \RuntimeException("Conditional service '{$serviceId}' not found");
        }
        
        $condition = $this->conditions[$serviceId]['condition'];
        $factory = $this->conditions[$serviceId]['factory'];
        
        if ($condition($this->container)) {
            return $factory($this->container);
        }
        
        throw new \RuntimeException("Condition not met for service '{$serviceId}'");
    }
    
    public function registerEnvironmentBased(string $serviceId, array $environments, callable $factory): void
    {
        $this->registerConditional($serviceId, function($container) use ($environments) {
            $env = $container->getParameter('app.env');
            return in_array($env, $environments);
        }, $factory);
    }
    
    public function registerFeatureBased(string $serviceId, string $feature, callable $factory): void
    {
        $this->registerConditional($serviceId, function($container) use ($feature) {
            return $container->get('feature_flags')->isEnabled($feature);
        }, $factory);
    }
}

// Example usage
$provider = new ConditionalServiceProvider($container);

// Environment-based service
$provider->registerEnvironmentBased('cache_service', ['production', 'staging'], function($container) {
    return new RedisCache($container->getParameter('redis.config'));
});

// Feature-based service
$provider->registerFeatureBased('analytics_service', 'advanced_analytics', function($container) {
    return new AdvancedAnalyticsService($container->get('analytics_client'));
});
```

## Best Practices

```php
// config/di-best-practices.tsk

dependency_injection_best_practices = {
    service_design = {
        use_interfaces = true
        keep_services_small = true
        follow_single_responsibility = true
        use_constructor_injection = true
    }
    
    container_management = {
        use_auto_wiring = true
        configure_services_declaratively = true
        use_service_tags = true
        implement_lazy_loading = true
    }
    
    lifecycle_management = {
        manage_singletons_properly = true
        handle_service_disposal = true
        use_factory_patterns = true
        implement_service_decorators = true
    }
    
    testing = {
        use_mock_objects = true
        test_service_integration = true
        verify_service_configuration = true
        test_conditional_services = true
    }
    
    performance = {
        optimize_service_creation = true
        use_lazy_loading = true
        implement_service_caching = true
        monitor_service_usage = true
    }
}

// Example usage in PHP
class DependencyInjectionBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Use interfaces for loose coupling
        $container = new AdvancedContainer($this->loadConfig());
        
        // 2. Register services with proper lifecycle
        $container->register('user_service', UserService::class)
            ->addArgument('@user_repository')
            ->addArgument('@password_hasher')
            ->addTag('service')
            ->setPublic(true);
        
        // 3. Use lazy loading for expensive services
        $lazyService = new LazyService($container, 'expensive_service');
        
        // 4. Decorate services for cross-cutting concerns
        $decorator = new ServiceDecorator($container);
        $decorator->addLogging('user_service');
        $decorator->addCaching('user_service');
        
        // 5. Use conditional services for environment-specific behavior
        $provider = new ConditionalServiceProvider($container);
        $provider->registerEnvironmentBased('cache_service', ['production'], function($container) {
            return new RedisCache($container->getParameter('redis.config'));
        });
        
        // 6. Monitor and optimize
        $this->logger->info('Dependency injection configured', [
            'services' => count($container->getServices()),
            'tags' => $container->getTags()
        ]);
    }
    
    private function loadConfig(): array
    {
        return TuskLang::load('dependency_injection');
    }
}
```

This comprehensive guide covers advanced dependency injection patterns in TuskLang with PHP integration. The DI system is designed to be flexible, performant, and maintainable while maintaining the rebellious spirit of TuskLang development. 
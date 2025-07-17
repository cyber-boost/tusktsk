# Advanced Dependency Injection in PHP with TuskLang

## Overview

TuskLang revolutionizes dependency injection by making it configuration-driven, intelligent, and adaptive. This guide covers advanced dependency injection patterns that leverage TuskLang's dynamic capabilities for comprehensive service management and object composition.

## 🎯 Dependency Injection Architecture

### Dependency Injection Configuration

```ini
# dependency-injection.tsk
[dependency_injection]
strategy = "constructor_injection"
container = "autowiring"
lifetime = "singleton"

[dependency_injection.container]
autowiring = {
    enabled = true,
    reflection = true,
    type_hinting = true
}

service_registry = {
    enabled = true,
    auto_discovery = true,
    lazy_loading = true
}

[dependency_injection.lifetimes]
singleton = {
    enabled = true,
    thread_safe = true,
    lazy_initialization = true
}

transient = {
    enabled = true,
    instance_per_request = true
}

scoped = {
    enabled = true,
    request_scope = true,
    session_scope = true
}

[dependency_injection.interception]
aspect_oriented = {
    enabled = true,
    logging = true,
    caching = true,
    validation = true
}

decorators = {
    enabled = true,
    dynamic_wrapping = true
}

[dependency_injection.validation]
circular_dependencies = true
missing_dependencies = true
interface_compliance = true
```

### Dependency Injection Container Implementation

```php
<?php
// DependencyInjectionContainer.php
class DependencyInjectionContainer
{
    private $config;
    private $bindings = [];
    private $instances = [];
    private $resolvers = [];
    private $interceptors = [];
    
    public function __construct()
    {
        $this->config = new TuskConfig('dependency-injection.tsk');
        $this->initializeContainer();
    }
    
    private function initializeContainer()
    {
        $strategy = $this->config->get('dependency_injection.strategy');
        
        switch ($strategy) {
            case 'constructor_injection':
                $this->initializeConstructorInjection();
                break;
            case 'property_injection':
                $this->initializePropertyInjection();
                break;
            case 'method_injection':
                $this->initializeMethodInjection();
                break;
        }
    }
    
    public function bind($abstract, $concrete = null, $lifetime = 'singleton')
    {
        if ($concrete === null) {
            $concrete = $abstract;
        }
        
        $this->bindings[$abstract] = [
            'concrete' => $concrete,
            'lifetime' => $lifetime,
            'resolved' => false
        ];
    }
    
    public function singleton($abstract, $concrete = null)
    {
        $this->bind($abstract, $concrete, 'singleton');
    }
    
    public function transient($abstract, $concrete = null)
    {
        $this->bind($abstract, $concrete, 'transient');
    }
    
    public function scoped($abstract, $concrete = null)
    {
        $this->bind($abstract, $concrete, 'scoped');
    }
    
    public function resolve($abstract, $parameters = [])
    {
        // Check if already resolved
        if (isset($this->instances[$abstract])) {
            return $this->instances[$abstract];
        }
        
        // Get binding
        $binding = $this->getBinding($abstract);
        
        if (!$binding) {
            throw new BindingNotFoundException("No binding found for {$abstract}");
        }
        
        // Resolve concrete implementation
        $instance = $this->resolveConcrete($binding, $parameters);
        
        // Apply lifetime management
        $this->applyLifetime($abstract, $instance, $binding['lifetime']);
        
        // Apply interceptors
        $instance = $this->applyInterceptors($instance, $abstract);
        
        return $instance;
    }
    
    public function make($abstract, $parameters = [])
    {
        return $this->resolve($abstract, $parameters);
    }
    
    public function call($callback, $parameters = [])
    {
        if (is_array($callback)) {
            $callback[0] = $this->resolve($callback[0]);
        }
        
        $dependencies = $this->resolveDependencies($callback, $parameters);
        
        return call_user_func_array($callback, $dependencies);
    }
    
    public function has($abstract)
    {
        return isset($this->bindings[$abstract]) || class_exists($abstract);
    }
    
    public function unbind($abstract)
    {
        unset($this->bindings[$abstract]);
        unset($this->instances[$abstract]);
    }
    
    public function flush()
    {
        $this->bindings = [];
        $this->instances = [];
        $this->resolvers = [];
    }
    
    private function getBinding($abstract)
    {
        // Check explicit bindings
        if (isset($this->bindings[$abstract])) {
            return $this->bindings[$abstract];
        }
        
        // Check if class exists for auto-binding
        if (class_exists($abstract)) {
            return [
                'concrete' => $abstract,
                'lifetime' => 'transient',
                'resolved' => false
            ];
        }
        
        return null;
    }
    
    private function resolveConcrete($binding, $parameters)
    {
        $concrete = $binding['concrete'];
        
        // If concrete is a closure
        if (is_callable($concrete) && !is_string($concrete)) {
            return $concrete($this, $parameters);
        }
        
        // If concrete is a class name
        if (is_string($concrete) && class_exists($concrete)) {
            return $this->build($concrete, $parameters);
        }
        
        // If concrete is an instance
        if (is_object($concrete)) {
            return $concrete;
        }
        
        throw new ResolutionException("Unable to resolve concrete for binding");
    }
    
    private function build($concrete, $parameters = [])
    {
        $reflector = new ReflectionClass($concrete);
        
        if (!$reflector->isInstantiable()) {
            throw new ResolutionException("Class {$concrete} is not instantiable");
        }
        
        $constructor = $reflector->getConstructor();
        
        if (!$constructor) {
            return new $concrete();
        }
        
        $dependencies = $this->resolveDependencies($constructor, $parameters);
        
        return $reflector->newInstanceArgs($dependencies);
    }
    
    private function resolveDependencies($callback, $parameters = [])
    {
        if (is_array($callback)) {
            $reflector = new ReflectionMethod($callback[0], $callback[1]);
        } else {
            $reflector = new ReflectionFunction($callback);
        }
        
        $dependencies = [];
        
        foreach ($reflector->getParameters() as $parameter) {
            $dependency = $this->resolveParameter($parameter, $parameters);
            $dependencies[] = $dependency;
        }
        
        return $dependencies;
    }
    
    private function resolveParameter($parameter, $parameters)
    {
        $name = $parameter->getName();
        
        // Check if parameter is provided explicitly
        if (array_key_exists($name, $parameters)) {
            return $parameters[$name];
        }
        
        // Check if parameter has a default value
        if ($parameter->isDefaultValueAvailable()) {
            return $parameter->getDefaultValue();
        }
        
        // Check if parameter is a class
        $type = $parameter->getType();
        if ($type && !$type->isBuiltin()) {
            $className = $type->getName();
            return $this->resolve($className);
        }
        
        // Check if parameter is optional
        if ($parameter->isOptional()) {
            return null;
        }
        
        throw new ResolutionException("Unable to resolve parameter: {$name}");
    }
    
    private function applyLifetime($abstract, $instance, $lifetime)
    {
        switch ($lifetime) {
            case 'singleton':
                $this->instances[$abstract] = $instance;
                break;
            case 'scoped':
                $this->instances[$abstract] = $instance;
                break;
            case 'transient':
                // Don't store instance
                break;
        }
    }
    
    private function applyInterceptors($instance, $abstract)
    {
        $interceptors = $this->getInterceptors($abstract);
        
        foreach ($interceptors as $interceptor) {
            $instance = $interceptor->intercept($instance);
        }
        
        return $instance;
    }
    
    private function getInterceptors($abstract)
    {
        $interceptors = [];
        
        // Get configured interceptors
        $interceptorConfig = $this->config->get('dependency_injection.interception');
        
        if ($interceptorConfig['aspect_oriented']['enabled']) {
            $interceptors[] = new AspectOrientedInterceptor($interceptorConfig['aspect_oriented']);
        }
        
        if ($interceptorConfig['decorators']['enabled']) {
            $interceptors[] = new DecoratorInterceptor($interceptorConfig['decorators']);
        }
        
        return $interceptors;
    }
    
    public function registerInterceptor($interceptor)
    {
        $this->interceptors[] = $interceptor;
    }
    
    public function getBindings()
    {
        return $this->bindings;
    }
    
    public function getInstances()
    {
        return $this->instances;
    }
}
```

## 🔧 Service Registration

### Service Registration Configuration

```ini
# service-registration.tsk
[service_registration]
enabled = true
auto_discovery = true
namespace_scanning = true

[service_registration.services]
database = {
    connection = "App\\Services\\DatabaseConnection",
    repository = "App\\Services\\DatabaseRepository",
    migration = "App\\Services\\DatabaseMigration"
}

cache = {
    redis = "App\\Services\\RedisCache",
    memory = "App\\Services\\MemoryCache",
    file = "App\\Services\\FileCache"
}

email = {
    smtp = "App\\Services\\SmtpEmailService",
    sendgrid = "App\\Services\\SendGridEmailService",
    mailgun = "App\\Services\\MailgunEmailService"
}

payment = {
    stripe = "App\\Services\\StripePaymentService",
    paypal = "App\\Services\\PayPalPaymentService",
    square = "App\\Services\\SquarePaymentService"
}

[service_registration.lifetimes]
singleton_services = [
    "database.connection",
    "cache.redis",
    "email.smtp"
]

transient_services = [
    "database.repository",
    "payment.stripe"
]

scoped_services = [
    "user.session",
    "request.context"
]
```

### Service Registration Implementation

```php
class ServiceRegistration
{
    private $config;
    private $container;
    
    public function __construct(DependencyInjectionContainer $container)
    {
        $this->config = new TuskConfig('service-registration.tsk');
        $this->container = $container;
    }
    
    public function registerServices()
    {
        $services = $this->config->get('service_registration.services');
        
        foreach ($services as $category => $serviceMap) {
            $this->registerServiceCategory($category, $serviceMap);
        }
        
        $this->registerLifetimes();
    }
    
    private function registerServiceCategory($category, $serviceMap)
    {
        foreach ($serviceMap as $name => $className) {
            $abstract = "{$category}.{$name}";
            $this->container->bind($abstract, $className);
        }
    }
    
    private function registerLifetimes()
    {
        $lifetimes = $this->config->get('service_registration.lifetimes');
        
        // Register singleton services
        foreach ($lifetimes['singleton_services'] as $service) {
            $this->container->singleton($service);
        }
        
        // Register transient services
        foreach ($lifetimes['transient_services'] as $service) {
            $this->container->transient($service);
        }
        
        // Register scoped services
        foreach ($lifetimes['scoped_services'] as $service) {
            $this->container->scoped($service);
        }
    }
    
    public function autoDiscoverServices($namespace, $directory)
    {
        if (!$this->config->get('service_registration.auto_discovery')) {
            return;
        }
        
        $classes = $this->scanDirectory($directory);
        
        foreach ($classes as $class) {
            if ($this->shouldRegisterService($class)) {
                $this->registerDiscoveredService($class);
            }
        }
    }
    
    private function scanDirectory($directory)
    {
        $classes = [];
        $iterator = new RecursiveIteratorIterator(
            new RecursiveDirectoryIterator($directory)
        );
        
        foreach ($iterator as $file) {
            if ($file->isFile() && $file->getExtension() === 'php') {
                $className = $this->extractClassName($file->getPathname());
                if ($className) {
                    $classes[] = $className;
                }
            }
        }
        
        return $classes;
    }
    
    private function extractClassName($filePath)
    {
        $content = file_get_contents($filePath);
        
        if (preg_match('/class\s+(\w+)/', $content, $matches)) {
            return $matches[1];
        }
        
        return null;
    }
    
    private function shouldRegisterService($class)
    {
        $reflection = new ReflectionClass($class);
        
        // Check if class has service attributes or implements service interface
        return $reflection->implementsInterface('ServiceInterface') ||
               $reflection->hasAttribute('Service');
    }
    
    private function registerDiscoveredService($class)
    {
        $reflection = new ReflectionClass($class);
        
        // Determine lifetime from attributes or default
        $lifetime = $this->determineLifetime($reflection);
        
        // Register service
        $this->container->bind($class, $class, $lifetime);
    }
    
    private function determineLifetime($reflection)
    {
        if ($reflection->hasAttribute('Singleton')) {
            return 'singleton';
        }
        
        if ($reflection->hasAttribute('Transient')) {
            return 'transient';
        }
        
        if ($reflection->hasAttribute('Scoped')) {
            return 'scoped';
        }
        
        return 'transient'; // Default
    }
}

// Service Attributes
#[Attribute]
class Service
{
    public function __construct(
        public string $name = '',
        public string $lifetime = 'transient'
    ) {}
}

#[Attribute]
class Singleton {}

#[Attribute]
class Transient {}

#[Attribute]
class Scoped {}

// Example Service Implementation
#[Service('database.connection', 'singleton')]
class DatabaseConnection implements ServiceInterface
{
    private $connection;
    
    public function __construct()
    {
        $this->connection = new PDO(
            'mysql:host=localhost;dbname=test',
            'username',
            'password'
        );
    }
    
    public function getConnection()
    {
        return $this->connection;
    }
}

#[Service('cache.redis', 'singleton')]
class RedisCache implements CacheInterface
{
    private $redis;
    
    public function __construct()
    {
        $this->redis = new Redis();
        $this->redis->connect('localhost', 6379);
    }
    
    public function get($key)
    {
        return $this->redis->get($key);
    }
    
    public function set($key, $value, $ttl = null)
    {
        if ($ttl) {
            return $this->redis->setex($key, $ttl, $value);
        }
        
        return $this->redis->set($key, $value);
    }
}
```

## 🔄 Auto-Wiring

### Auto-Wiring Configuration

```ini
# auto-wiring.tsk
[auto_wiring]
enabled = true
reflection = true
type_hinting = true

[auto_wiring.resolution]
constructor_injection = true
property_injection = false
method_injection = false

[auto_wiring.interfaces]
mapping = {
    "App\\Interfaces\\CacheInterface" = "App\\Services\\RedisCache",
    "App\\Interfaces\\EmailInterface" = "App\\Services\\SmtpEmailService",
    "App\\Interfaces\\PaymentInterface" = "App\\Services\\StripePaymentService"
}

[auto_wiring.attributes]
service = "Service"
inject = "Inject"
autowired = "Autowired"
```

### Auto-Wiring Implementation

```php
class AutoWiring
{
    private $config;
    private $container;
    private $interfaceMap;
    
    public function __construct(DependencyInjectionContainer $container)
    {
        $this->config = new TuskConfig('auto-wiring.tsk');
        $this->container = $container;
        $this->interfaceMap = $this->config->get('auto_wiring.interfaces.mapping');
    }
    
    public function resolve($abstract, $parameters = [])
    {
        if (!$this->config->get('auto_wiring.enabled')) {
            return $this->container->resolve($abstract, $parameters);
        }
        
        // Check if it's an interface
        if (interface_exists($abstract)) {
            $concrete = $this->resolveInterface($abstract);
            if ($concrete) {
                return $this->container->resolve($concrete, $parameters);
            }
        }
        
        // Check if it's a class
        if (class_exists($abstract)) {
            return $this->autoWireClass($abstract, $parameters);
        }
        
        throw new ResolutionException("Unable to resolve: {$abstract}");
    }
    
    private function resolveInterface($interface)
    {
        // Check explicit interface mapping
        if (isset($this->interfaceMap[$interface])) {
            return $this->interfaceMap[$interface];
        }
        
        // Try to find implementation by convention
        $implementation = $this->findImplementationByConvention($interface);
        if ($implementation) {
            return $implementation;
        }
        
        return null;
    }
    
    private function findImplementationByConvention($interface)
    {
        $interfaceName = class_basename($interface);
        $namespace = dirname($interface);
        
        // Try common naming conventions
        $candidates = [
            str_replace('Interface', '', $interfaceName),
            $interfaceName . 'Impl',
            $interfaceName . 'Service'
        ];
        
        foreach ($candidates as $candidate) {
            $className = $namespace . '\\' . $candidate;
            if (class_exists($className)) {
                return $className;
            }
        }
        
        return null;
    }
    
    private function autoWireClass($className, $parameters = [])
    {
        $reflection = new ReflectionClass($className);
        
        if (!$reflection->isInstantiable()) {
            throw new ResolutionException("Class {$className} is not instantiable");
        }
        
        $constructor = $reflection->getConstructor();
        
        if (!$constructor) {
            return new $className();
        }
        
        $dependencies = $this->resolveConstructorDependencies($constructor, $parameters);
        
        return $reflection->newInstanceArgs($dependencies);
    }
    
    private function resolveConstructorDependencies($constructor, $parameters)
    {
        $dependencies = [];
        
        foreach ($constructor->getParameters() as $parameter) {
            $dependency = $this->resolveParameter($parameter, $parameters);
            $dependencies[] = $dependency;
        }
        
        return $dependencies;
    }
    
    private function resolveParameter($parameter, $parameters)
    {
        $name = $parameter->getName();
        
        // Check explicit parameters
        if (array_key_exists($name, $parameters)) {
            return $parameters[$name];
        }
        
        // Check default value
        if ($parameter->isDefaultValueAvailable()) {
            return $parameter->getDefaultValue();
        }
        
        // Check type hint
        $type = $parameter->getType();
        if ($type && !$type->isBuiltin()) {
            $className = $type->getName();
            return $this->resolve($className);
        }
        
        // Check if optional
        if ($parameter->isOptional()) {
            return null;
        }
        
        throw new ResolutionException("Unable to resolve parameter: {$name}");
    }
    
    public function autoWireProperties($instance)
    {
        if (!$this->config->get('auto_wiring.resolution.property_injection')) {
            return $instance;
        }
        
        $reflection = new ReflectionClass($instance);
        
        foreach ($reflection->getProperties() as $property) {
            if ($this->shouldAutoWireProperty($property)) {
                $this->autoWireProperty($instance, $property);
            }
        }
        
        return $instance;
    }
    
    private function shouldAutoWireProperty($property)
    {
        // Check for Inject attribute
        if ($property->hasAttribute('Inject')) {
            return true;
        }
        
        // Check for Autowired attribute
        if ($property->hasAttribute('Autowired')) {
            return true;
        }
        
        return false;
    }
    
    private function autoWireProperty($instance, $property)
    {
        $property->setAccessible(true);
        
        $type = $property->getType();
        if ($type && !$type->isBuiltin()) {
            $className = $type->getName();
            $dependency = $this->resolve($className);
            $property->setValue($instance, $dependency);
        }
    }
}

// Auto-Wiring Attributes
#[Attribute]
class Inject
{
    public function __construct(
        public string $service = ''
    ) {}
}

#[Attribute]
class Autowired {}

// Example Auto-Wired Class
class UserService
{
    private CacheInterface $cache;
    private EmailInterface $email;
    private PaymentInterface $payment;
    
    public function __construct(
        CacheInterface $cache,
        EmailInterface $email,
        PaymentInterface $payment
    ) {
        $this->cache = $cache;
        $this->email = $email;
        $this->payment = $payment;
    }
    
    public function createUser($userData)
    {
        // Use injected dependencies
        $user = $this->saveUser($userData);
        
        $this->cache->set("user:{$user['id']}", $user);
        $this->email->sendWelcomeEmail($user['email']);
        
        return $user;
    }
    
    public function processPayment($userId, $amount)
    {
        $user = $this->cache->get("user:{$userId}");
        
        if (!$user) {
            throw new UserNotFoundException("User not found");
        }
        
        return $this->payment->process($user, $amount);
    }
}
```

## 🔍 Service Locator

### Service Locator Configuration

```ini
# service-locator.tsk
[service_locator]
enabled = true
fallback_container = true
service_resolution = true

[service_locator.services]
aliases = {
    "db" = "database.connection",
    "cache" = "cache.redis",
    "mail" = "email.smtp",
    "pay" = "payment.stripe"
}

[service_locator.resolution]
priority = [
    "explicit_binding",
    "alias_resolution",
    "auto_discovery",
    "fallback_container"
]
```

### Service Locator Implementation

```php
class ServiceLocator
{
    private $config;
    private $container;
    private $aliases;
    
    public function __construct(DependencyInjectionContainer $container)
    {
        $this->config = new TuskConfig('service-locator.tsk');
        $this->container = $container;
        $this->aliases = $this->config->get('service_locator.services.aliases');
    }
    
    public function get($service)
    {
        $resolutionPriority = $this->config->get('service_locator.resolution.priority');
        
        foreach ($resolutionPriority as $method) {
            $instance = $this->resolveByMethod($service, $method);
            if ($instance) {
                return $instance;
            }
        }
        
        throw new ServiceNotFoundException("Service not found: {$service}");
    }
    
    public function has($service)
    {
        try {
            $this->get($service);
            return true;
        } catch (ServiceNotFoundException $e) {
            return false;
        }
    }
    
    private function resolveByMethod($service, $method)
    {
        switch ($method) {
            case 'explicit_binding':
                return $this->resolveExplicitBinding($service);
            case 'alias_resolution':
                return $this->resolveAlias($service);
            case 'auto_discovery':
                return $this->resolveAutoDiscovery($service);
            case 'fallback_container':
                return $this->resolveFallbackContainer($service);
            default:
                return null;
        }
    }
    
    private function resolveExplicitBinding($service)
    {
        if ($this->container->has($service)) {
            return $this->container->resolve($service);
        }
        
        return null;
    }
    
    private function resolveAlias($service)
    {
        if (isset($this->aliases[$service])) {
            $aliasedService = $this->aliases[$service];
            return $this->container->resolve($aliasedService);
        }
        
        return null;
    }
    
    private function resolveAutoDiscovery($service)
    {
        // Try to resolve by class name
        if (class_exists($service)) {
            return $this->container->resolve($service);
        }
        
        // Try to resolve by interface
        if (interface_exists($service)) {
            return $this->resolveInterface($service);
        }
        
        return null;
    }
    
    private function resolveFallbackContainer($service)
    {
        if (!$this->config->get('service_locator.fallback_container')) {
            return null;
        }
        
        try {
            return $this->container->resolve($service);
        } catch (Exception $e) {
            return null;
        }
    }
    
    private function resolveInterface($interface)
    {
        // Try to find implementation
        $implementation = $this->findImplementation($interface);
        
        if ($implementation) {
            return $this->container->resolve($implementation);
        }
        
        return null;
    }
    
    private function findImplementation($interface)
    {
        // Look for implementation in same namespace
        $namespace = dirname($interface);
        $interfaceName = class_basename($interface);
        
        $candidates = [
            str_replace('Interface', '', $interfaceName),
            $interfaceName . 'Impl',
            $interfaceName . 'Service'
        ];
        
        foreach ($candidates as $candidate) {
            $className = $namespace . '\\' . $candidate;
            if (class_exists($className)) {
                return $className;
            }
        }
        
        return null;
    }
    
    public function registerAlias($alias, $service)
    {
        $this->aliases[$alias] = $service;
    }
    
    public function getAliases()
    {
        return $this->aliases;
    }
}

// Example Service Locator Usage
class Application
{
    private $locator;
    
    public function __construct()
    {
        $container = new DependencyInjectionContainer();
        $this->locator = new ServiceLocator($container);
        
        // Register services
        $this->registerServices();
    }
    
    private function registerServices()
    {
        $container = $this->locator->container;
        
        // Register core services
        $container->singleton('database.connection', DatabaseConnection::class);
        $container->singleton('cache.redis', RedisCache::class);
        $container->singleton('email.smtp', SmtpEmailService::class);
        $container->singleton('payment.stripe', StripePaymentService::class);
    }
    
    public function handleRequest($request)
    {
        // Use service locator to get dependencies
        $db = $this->locator->get('db');
        $cache = $this->locator->get('cache');
        $email = $this->locator->get('mail');
        $payment = $this->locator->get('pay');
        
        // Process request with services
        return $this->processRequest($request, $db, $cache, $email, $payment);
    }
    
    private function processRequest($request, $db, $cache, $email, $payment)
    {
        // Business logic using injected services
        $user = $db->findUser($request['user_id']);
        
        if (!$user) {
            throw new UserNotFoundException();
        }
        
        $cache->set("user:{$user['id']}", $user);
        
        if ($request['action'] === 'payment') {
            $result = $payment->process($user, $request['amount']);
            $email->sendPaymentConfirmation($user['email'], $result);
        }
        
        return ['success' => true];
    }
}
```

## 📋 Best Practices

### Dependency Injection Best Practices

1. **Constructor Injection**: Prefer constructor injection over property injection
2. **Interface Segregation**: Depend on interfaces, not concrete classes
3. **Single Responsibility**: Each service should have one responsibility
4. **Lifetime Management**: Choose appropriate lifetimes (singleton, transient, scoped)
5. **Circular Dependencies**: Avoid circular dependencies
6. **Service Registration**: Register services explicitly when possible
7. **Testing**: Make services easily testable through dependency injection
8. **Documentation**: Document service dependencies and lifetimes

### Integration Examples

```php
// Dependency Injection Integration
class DependencyInjectionIntegration
{
    private $container;
    private $serviceRegistration;
    private $autoWiring;
    private $serviceLocator;
    
    public function __construct()
    {
        $this->container = new DependencyInjectionContainer();
        $this->serviceRegistration = new ServiceRegistration($this->container);
        $this->autoWiring = new AutoWiring($this->container);
        $this->serviceLocator = new ServiceLocator($this->container);
        
        $this->initialize();
    }
    
    private function initialize()
    {
        // Register services
        $this->serviceRegistration->registerServices();
        
        // Auto-discover services
        $this->serviceRegistration->autoDiscoverServices(
            'App\\Services',
            __DIR__ . '/Services'
        );
    }
    
    public function resolve($abstract, $parameters = [])
    {
        return $this->autoWiring->resolve($abstract, $parameters);
    }
    
    public function get($service)
    {
        return $this->serviceLocator->get($service);
    }
    
    public function has($service)
    {
        return $this->serviceLocator->has($service);
    }
    
    public function call($callback, $parameters = [])
    {
        return $this->container->call($callback, $parameters);
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Circular Dependencies**: Use lazy loading or redesign architecture
2. **Missing Dependencies**: Ensure all dependencies are registered
3. **Performance Issues**: Use appropriate lifetimes and lazy loading
4. **Testing Difficulties**: Use interfaces and dependency injection for testability
5. **Service Resolution**: Check service registration and auto-wiring configuration

### Debug Configuration

```ini
# debug-dependency-injection.tsk
[debug]
enabled = true
log_level = "verbose"
trace_resolution = true

[debug.output]
console = true
file = "/var/log/tusk-dependency-injection-debug.log"
```

This comprehensive dependency injection system leverages TuskLang's configuration-driven approach to create intelligent, adaptive service management solutions that ensure maintainable, testable, and scalable application architecture. 
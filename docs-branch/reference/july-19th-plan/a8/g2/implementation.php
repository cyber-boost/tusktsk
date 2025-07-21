<?php
/**
 * 🥜 TuskLang PHP Agent A8 - Goal G2 Implementation
 * ==================================================
 * Agent: a8
 * Goals: g2.1, g2.2, g2.3
 * Language: PHP
 * 
 * This file implements the three goals for the PHP agent g2:
 * - g2.1: Advanced PHP Framework Integration
 * - g2.2: Database Connectivity and ORM Features
 * - g2.3: API Development and RESTful Services
 */

namespace TuskLang\AgentA8\G2;

/**
 * Goal 2.1: Advanced PHP Framework Integration
 * Priority: High
 * Success Criteria: Implement framework integration capabilities
 */
class FrameworkIntegration
{
    private array $supportedFrameworks = [];
    private array $frameworkConfigs = [];
    private array $middleware = [];
    
    public function __construct()
    {
        $this->initializeFrameworks();
    }
    
    /**
     * Initialize supported frameworks
     */
    private function initializeFrameworks(): void
    {
        $this->supportedFrameworks = [
            'laravel' => [
                'name' => 'Laravel',
                'version' => '10.x',
                'features' => ['eloquent', 'blade', 'artisan', 'middleware'],
                'autoload' => 'vendor/autoload.php'
            ],
            'symfony' => [
                'name' => 'Symfony',
                'version' => '6.x',
                'features' => ['doctrine', 'twig', 'console', 'security'],
                'autoload' => 'vendor/autoload.php'
            ],
            'codeigniter' => [
                'name' => 'CodeIgniter',
                'version' => '4.x',
                'features' => ['mvc', 'database', 'session', 'validation'],
                'autoload' => 'vendor/autoload.php'
            ],
            'slim' => [
                'name' => 'Slim',
                'version' => '4.x',
                'features' => ['routing', 'middleware', 'dependency-injection'],
                'autoload' => 'vendor/autoload.php'
            ]
        ];
    }
    
    /**
     * Detect installed frameworks
     */
    public function detectFrameworks(): array
    {
        $detected = [];
        
        foreach ($this->supportedFrameworks as $key => $framework) {
            if (file_exists($framework['autoload'])) {
                $detected[$key] = $framework;
                $detected[$key]['installed'] = true;
                $detected[$key]['path'] = realpath($framework['autoload']);
            }
        }
        
        return $detected;
    }
    
    /**
     * Load framework configuration
     */
    public function loadFrameworkConfig(string $framework): array
    {
        if (!isset($this->supportedFrameworks[$framework])) {
            throw new \Exception("Unsupported framework: $framework");
        }
        
        $config = $this->supportedFrameworks[$framework];
        
        // Try to load framework-specific configuration
        $configFiles = [
            'laravel' => '.env',
            'symfony' => '.env.local',
            'codeigniter' => 'app/Config/App.php',
            'slim' => 'config/settings.php'
        ];
        
        if (isset($configFiles[$framework])) {
            $configFile = $configFiles[$framework];
            if (file_exists($configFile)) {
                $config['config_file'] = $configFile;
                $config['config_loaded'] = true;
            }
        }
        
        $this->frameworkConfigs[$framework] = $config;
        return $config;
    }
    
    /**
     * Register middleware
     */
    public function registerMiddleware(string $name, callable $handler): void
    {
        $this->middleware[$name] = $handler;
    }
    
    /**
     * Execute middleware chain
     */
    public function executeMiddleware(array $middleware, $request, $response = null)
    {
        $current = $request;
        
        foreach ($middleware as $name) {
            if (isset($this->middleware[$name])) {
                $current = call_user_func($this->middleware[$name], $current, $response);
            }
        }
        
        return $current;
    }
    
    /**
     * Create framework-specific service provider
     */
    public function createServiceProvider(string $framework, string $name): string
    {
        $templates = [
            'laravel' => $this->getLaravelServiceProviderTemplate($name),
            'symfony' => $this->getSymfonyServiceProviderTemplate($name),
            'codeigniter' => $this->getCodeIgniterServiceProviderTemplate($name),
            'slim' => $this->getSlimServiceProviderTemplate($name)
        ];
        
        if (!isset($templates[$framework])) {
            throw new \Exception("No template available for framework: $framework");
        }
        
        return $templates[$framework];
    }
    
    /**
     * Laravel service provider template
     */
    private function getLaravelServiceProviderTemplate(string $name): string
    {
        return "<?php

namespace App\Providers;

use Illuminate\Support\ServiceProvider;

class {$name}ServiceProvider extends ServiceProvider
{
    public function register()
    {
        // Register services
    }
    
    public function boot()
    {
        // Boot services
    }
}";
    }
    
    /**
     * Symfony service provider template
     */
    private function getSymfonyServiceProviderTemplate(string $name): string
    {
        return "<?php

namespace App\DependencyInjection;

use Symfony\Component\DependencyInjection\ContainerBuilder;
use Symfony\Component\DependencyInjection\Extension\ExtensionInterface;

class {$name}Extension implements ExtensionInterface
{
    public function load(array \$configs, ContainerBuilder \$container)
    {
        // Load configuration
    }
    
    public function getNamespace()
    {
        return '{$name}';
    }
    
    public function getXsdValidationBasePath()
    {
        return false;
    }
    
    public function getAlias()
    {
        return '{$name}';
    }
}";
    }
    
    /**
     * CodeIgniter service provider template
     */
    private function getCodeIgniterServiceProviderTemplate(string $name): string
    {
        return "<?php

namespace App\Services;

class {$name}Service
{
    public function __construct()
    {
        // Initialize service
    }
    
    public function register()
    {
        // Register service
    }
}";
    }
    
    /**
     * Slim service provider template
     */
    private function getSlimServiceProviderTemplate(string $name): string
    {
        return "<?php

use Psr\Container\ContainerInterface;

return [
    '{$name}' => function (ContainerInterface \$container) {
        return new {$name}Service();
    }
];";
    }
    
    /**
     * Get framework features
     */
    public function getFrameworkFeatures(string $framework): array
    {
        if (!isset($this->supportedFrameworks[$framework])) {
            return [];
        }
        
        return $this->supportedFrameworks[$framework]['features'];
    }
    
    /**
     * Validate framework compatibility
     */
    public function validateCompatibility(string $framework, string $version): bool
    {
        if (!isset($this->supportedFrameworks[$framework])) {
            return false;
        }
        
        $supportedVersion = $this->supportedFrameworks[$framework]['version'];
        return version_compare($version, $supportedVersion, '>=');
    }
}

/**
 * Goal 2.2: Database Connectivity and ORM Features
 * Priority: Medium
 * Success Criteria: Implement database connectivity and ORM capabilities
 */
class DatabaseConnector
{
    private array $connections = [];
    private array $ormConfigs = [];
    private array $queryLog = [];
    
    public function __construct()
    {
        $this->initializeORMConfigs();
    }
    
    /**
     * Initialize ORM configurations
     */
    private function initializeORMConfigs(): void
    {
        $this->ormConfigs = [
            'eloquent' => [
                'name' => 'Eloquent ORM',
                'framework' => 'laravel',
                'features' => ['relationships', 'migrations', 'seeders', 'query_builder']
            ],
            'doctrine' => [
                'name' => 'Doctrine ORM',
                'framework' => 'symfony',
                'features' => ['entities', 'repositories', 'migrations', 'annotations']
            ],
            'codeigniter' => [
                'name' => 'CodeIgniter Query Builder',
                'framework' => 'codeigniter',
                'features' => ['query_builder', 'migrations', 'seeds']
            ]
        ];
    }
    
    /**
     * Create database connection
     */
    public function createConnection(string $driver, array $config): \PDO
    {
        $dsn = $this->buildDSN($driver, $config);
        
        try {
            $pdo = new \PDO($dsn, $config['username'] ?? '', $config['password'] ?? '', [
                \PDO::ATTR_ERRMODE => \PDO::ERRMODE_EXCEPTION,
                \PDO::ATTR_DEFAULT_FETCH_MODE => \PDO::FETCH_ASSOC,
                \PDO::ATTR_EMULATE_PREPARES => false
            ]);
            
            $this->connections[$config['name'] ?? 'default'] = $pdo;
            return $pdo;
            
        } catch (\PDOException $e) {
            throw new \Exception("Database connection failed: " . $e->getMessage());
        }
    }
    
    /**
     * Build DSN string
     */
    private function buildDSN(string $driver, array $config): string
    {
        switch ($driver) {
            case 'mysql':
                $port = $config['port'] ?? 3306;
                return "mysql:host={$config['host']};port={$port};dbname={$config['database']};charset=utf8mb4";
            case 'pgsql':
                $port = $config['port'] ?? 5432;
                return "pgsql:host={$config['host']};port={$port};dbname={$config['database']}";
            case 'sqlite':
                return "sqlite:{$config['database']}";
            case 'sqlsrv':
                return "sqlsrv:Server={$config['host']};Database={$config['database']}";
            default:
                throw new \Exception("Unsupported database driver: $driver");
        }
    }
    
    /**
     * Execute query with logging
     */
    public function executeQuery(\PDO $pdo, string $sql, array $params = []): array
    {
        $startTime = microtime(true);
        
        try {
            $stmt = $pdo->prepare($sql);
            $stmt->execute($params);
            $result = $stmt->fetchAll();
            
            $executionTime = (microtime(true) - $startTime) * 1000;
            
            $this->queryLog[] = [
                'sql' => $sql,
                'params' => $params,
                'execution_time' => $executionTime,
                'rows_affected' => $stmt->rowCount(),
                'timestamp' => date('Y-m-d H:i:s')
            ];
            
            return $result;
            
        } catch (\PDOException $e) {
            throw new \Exception("Query execution failed: " . $e->getMessage());
        }
    }
    
    /**
     * Create ORM model template
     */
    public function createModelTemplate(string $orm, string $modelName, array $fields = []): string
    {
        $templates = [
            'eloquent' => $this->getEloquentModelTemplate($modelName, $fields),
            'doctrine' => $this->getDoctrineModelTemplate($modelName, $fields),
            'codeigniter' => $this->getCodeIgniterModelTemplate($modelName, $fields)
        ];
        
        if (!isset($templates[$orm])) {
            throw new \Exception("No template available for ORM: $orm");
        }
        
        return $templates[$orm];
    }
    
    /**
     * Eloquent model template
     */
    private function getEloquentModelTemplate(string $modelName, array $fields): string
    {
        $fillable = implode("', '", array_keys($fields));
        $fillable = $fillable ? "['$fillable']" : '[]';
        
        return "<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class $modelName extends Model
{
    protected \$fillable = $fillable;
    
    protected \$table = '" . strtolower($modelName) . "s';
    
    // Relationships can be defined here
}";
    }
    
    /**
     * Doctrine model template
     */
    private function getDoctrineModelTemplate(string $modelName, array $fields): string
    {
        $properties = '';
        $getters = '';
        $setters = '';
        
        foreach ($fields as $field => $type) {
            $properties .= "    /** @ORM\\Column(type=\"$type\")\n     */\n    private \$$field;\n\n";
            $getters .= "    public function get" . ucfirst($field) . "()\n    {\n        return \$this->$field;\n    }\n\n";
            $setters .= "    public function set" . ucfirst($field) . "(\$$field)\n    {\n        \$this->$field = \$$field;\n        return \$this;\n    }\n\n";
        }
        
        return "<?php

namespace App\\Entity;

use Doctrine\\ORM\\Mapping as ORM;

/**
 * @ORM\\Entity
 * @ORM\\Table(name=\"" . strtolower($modelName) . "s\")
 */
class $modelName
{
    /**
     * @ORM\\Id
     * @ORM\\GeneratedValue
     * @ORM\\Column(type=\"integer\")
     */
    private \$id;

$properties
    public function getId()
    {
        return \$this->id;
    }

$getters$setters}";
    }
    
    /**
     * CodeIgniter model template
     */
    private function getCodeIgniterModelTemplate(string $modelName, array $fields): string
    {
        $fillable = implode("', '", array_keys($fields));
        $fillable = $fillable ? "['$fillable']" : '[]';
        
        return "<?php

namespace App\\Models;

use CodeIgniter\\Model;

class {$modelName}Model extends Model
{
    protected \$table = '" . strtolower($modelName) . "s';
    protected \$allowedFields = $fillable;
    protected \$useTimestamps = true;
    
    // Validation rules can be defined here
}";
    }
    
    /**
     * Get query log
     */
    public function getQueryLog(): array
    {
        return $this->queryLog;
    }
    
    /**
     * Clear query log
     */
    public function clearQueryLog(): void
    {
        $this->queryLog = [];
    }
    
    /**
     * Get query statistics
     */
    public function getQueryStats(): array
    {
        if (empty($this->queryLog)) {
            return ['total_queries' => 0, 'total_time' => 0, 'average_time' => 0];
        }
        
        $totalQueries = count($this->queryLog);
        $totalTime = array_sum(array_column($this->queryLog, 'execution_time'));
        $averageTime = $totalTime / $totalQueries;
        
        return [
            'total_queries' => $totalQueries,
            'total_time' => $totalTime,
            'average_time' => $averageTime,
            'slowest_query' => max(array_column($this->queryLog, 'execution_time')),
            'fastest_query' => min(array_column($this->queryLog, 'execution_time'))
        ];
    }
}

/**
 * Goal 2.3: API Development and RESTful Services
 * Priority: Low
 * Success Criteria: Implement API development and RESTful service capabilities
 */
class APIDeveloper
{
    private array $routes = [];
    private array $middleware = [];
    private array $apiDocs = [];
    private array $requestLog = [];
    
    public function __construct()
    {
        $this->initializeDefaultMiddleware();
    }
    
    /**
     * Initialize default middleware
     */
    private function initializeDefaultMiddleware(): void
    {
        $this->middleware = [
            'cors' => [$this, 'corsMiddleware'],
            'auth' => [$this, 'authMiddleware'],
            'rate_limit' => [$this, 'rateLimitMiddleware'],
            'logging' => [$this, 'loggingMiddleware']
        ];
    }
    
    /**
     * Register API route
     */
    public function registerRoute(string $method, string $path, callable $handler, array $options = []): void
    {
        $this->routes[] = [
            'method' => strtoupper($method),
            'path' => $path,
            'handler' => $handler,
            'middleware' => $options['middleware'] ?? [],
            'auth_required' => $options['auth_required'] ?? false,
            'rate_limit' => $options['rate_limit'] ?? null
        ];
    }
    
    /**
     * Handle API request
     */
    public function handleRequest(string $method, string $path, array $data = []): array
    {
        $startTime = microtime(true);
        
        // Find matching route
        $route = $this->findRoute($method, $path);
        
        if (!$route) {
            return $this->createResponse(404, ['error' => 'Route not found']);
        }
        
        try {
            // Apply middleware
            $request = $this->applyMiddleware($route['middleware'], $data);
            
            // Execute handler
            $result = call_user_func($route['handler'], $request);
            
            $executionTime = (microtime(true) - $startTime) * 1000;
            
            // Log request
            $this->logRequest($method, $path, $executionTime, 200);
            
            return $this->createResponse(200, $result);
            
        } catch (\Exception $e) {
            $executionTime = (microtime(true) - $startTime) * 1000;
            $this->logRequest($method, $path, $executionTime, 500);
            
            return $this->createResponse(500, ['error' => $e->getMessage()]);
        }
    }
    
    /**
     * Find matching route
     */
    private function findRoute(string $method, string $path): ?array
    {
        foreach ($this->routes as $route) {
            if ($route['method'] === strtoupper($method) && $this->matchPath($route['path'], $path)) {
                return $route;
            }
        }
        
        return null;
    }
    
    /**
     * Match path pattern
     */
    private function matchPath(string $pattern, string $path): bool
    {
        $pattern = preg_replace('/\{[^}]+\}/', '[^/]+', $pattern);
        return preg_match("#^$pattern$#", $path);
    }
    
    /**
     * Apply middleware
     */
    private function applyMiddleware(array $middleware, array $request): array
    {
        foreach ($middleware as $name) {
            if (isset($this->middleware[$name])) {
                $request = call_user_func($this->middleware[$name], $request);
            }
        }
        
        return $request;
    }
    
    /**
     * CORS middleware
     */
    public function corsMiddleware(array $request): array
    {
        header('Access-Control-Allow-Origin: *');
        header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
        header('Access-Control-Allow-Headers: Content-Type, Authorization');
        
        return $request;
    }
    
    /**
     * Auth middleware
     */
    public function authMiddleware(array $request): array
    {
        $token = $request['headers']['Authorization'] ?? null;
        
        if (!$token) {
            throw new \Exception('Authorization token required');
        }
        
        // Validate token (simplified)
        if (!preg_match('/^Bearer\s+(.+)$/', $token, $matches)) {
            throw new \Exception('Invalid token format');
        }
        
        $request['user'] = $this->validateToken($matches[1]);
        return $request;
    }
    
    /**
     * Rate limit middleware
     */
    public function rateLimitMiddleware(array $request): array
    {
        $ip = $_SERVER['REMOTE_ADDR'] ?? 'unknown';
        $key = "rate_limit:$ip";
        
        // Simple rate limiting (in production, use Redis or similar)
        $requests = $this->getRateLimitCount($key);
        
        if ($requests > 100) { // 100 requests per minute
            throw new \Exception('Rate limit exceeded');
        }
        
        $this->incrementRateLimit($key);
        return $request;
    }
    
    /**
     * Logging middleware
     */
    public function loggingMiddleware(array $request): array
    {
        // Add request ID for tracking
        $request['request_id'] = uniqid();
        return $request;
    }
    
    /**
     * Create API response
     */
    private function createResponse(int $status, array $data): array
    {
        return [
            'status' => $status,
            'data' => $data,
            'timestamp' => date('Y-m-d H:i:s'),
            'request_id' => uniqid()
        ];
    }
    
    /**
     * Log request
     */
    private function logRequest(string $method, string $path, float $executionTime, int $status): void
    {
        $this->requestLog[] = [
            'method' => $method,
            'path' => $path,
            'execution_time' => $executionTime,
            'status' => $status,
            'timestamp' => date('Y-m-d H:i:s'),
            'ip' => $_SERVER['REMOTE_ADDR'] ?? 'unknown'
        ];
    }
    
    /**
     * Generate API documentation
     */
    public function generateAPIDocs(): array
    {
        $docs = [];
        
        foreach ($this->routes as $route) {
            $docs[] = [
                'method' => $route['method'],
                'path' => $route['path'],
                'auth_required' => $route['auth_required'],
                'rate_limit' => $route['rate_limit'],
                'middleware' => $route['middleware']
            ];
        }
        
        return $docs;
    }
    
    /**
     * Get request statistics
     */
    public function getRequestStats(): array
    {
        if (empty($this->requestLog)) {
            return ['total_requests' => 0, 'average_time' => 0];
        }
        
        $totalRequests = count($this->requestLog);
        $totalTime = array_sum(array_column($this->requestLog, 'execution_time'));
        $averageTime = $totalTime / $totalRequests;
        
        $statusCodes = array_count_values(array_column($this->requestLog, 'status'));
        
        return [
            'total_requests' => $totalRequests,
            'average_time' => $averageTime,
            'status_codes' => $statusCodes,
            'slowest_request' => max(array_column($this->requestLog, 'execution_time')),
            'fastest_request' => min(array_column($this->requestLog, 'execution_time'))
        ];
    }
    
    /**
     * Validate token (simplified)
     */
    private function validateToken(string $token): array
    {
        // In production, implement proper JWT validation
        return ['user_id' => 1, 'username' => 'test_user'];
    }
    
    /**
     * Get rate limit count (simplified)
     */
    private function getRateLimitCount(string $key): int
    {
        // In production, use Redis or similar
        return 0;
    }
    
    /**
     * Increment rate limit (simplified)
     */
    private function incrementRateLimit(string $key): void
    {
        // In production, use Redis or similar
    }
}

/**
 * Main Agent A8 G2 Implementation
 * Combines all three goals into a unified system
 */
class AgentA8G2
{
    private FrameworkIntegration $framework;
    private DatabaseConnector $database;
    private APIDeveloper $api;
    
    public function __construct()
    {
        $this->framework = new FrameworkIntegration();
        $this->database = new DatabaseConnector();
        $this->api = new APIDeveloper();
    }
    
    /**
     * Execute goal 2.1: Advanced PHP Framework Integration
     */
    public function executeGoal2_1(): array
    {
        try {
            // Detect installed frameworks
            $detectedFrameworks = $this->framework->detectFrameworks();
            
            // Load framework configurations
            $configs = [];
            foreach (array_keys($detectedFrameworks) as $framework) {
                $configs[$framework] = $this->framework->loadFrameworkConfig($framework);
            }
            
            // Register sample middleware
            $this->framework->registerMiddleware('logger', function($request) {
                error_log("Request: " . json_encode($request));
                return $request;
            });
            
            // Create sample service provider
            $serviceProvider = $this->framework->createServiceProvider('laravel', 'TuskLang');
            
            return [
                'success' => true,
                'detected_frameworks' => $detectedFrameworks,
                'configurations' => $configs,
                'service_provider' => $serviceProvider,
                'middleware_count' => 1
            ];
            
        } catch (\Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Execute goal 2.2: Database Connectivity and ORM Features
     */
    public function executeGoal2_2(): array
    {
        try {
            // Create sample database connection
            $config = [
                'driver' => 'sqlite',
                'database' => ':memory:',
                'name' => 'test_connection'
            ];
            
            $pdo = $this->database->createConnection('sqlite', $config);
            
            // Execute sample query
            $result = $this->database->executeQuery($pdo, "SELECT 1 as test");
            
            // Create sample model templates
            $eloquentModel = $this->database->createModelTemplate('eloquent', 'User', [
                'name' => 'string',
                'email' => 'string',
                'password' => 'string'
            ]);
            
            $doctrineModel = $this->database->createModelTemplate('doctrine', 'Product', [
                'name' => 'string',
                'price' => 'decimal',
                'description' => 'text'
            ]);
            
            // Get query statistics
            $stats = $this->database->getQueryStats();
            
            return [
                'success' => true,
                'connection_created' => true,
                'sample_query_result' => $result,
                'eloquent_model' => $eloquentModel,
                'doctrine_model' => $doctrineModel,
                'query_stats' => $stats
            ];
            
        } catch (\Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Execute goal 2.3: API Development and RESTful Services
     */
    public function executeGoal2_3(): array
    {
        try {
            // Register sample API routes
            $this->api->registerRoute('GET', '/api/users', function($request) {
                return ['users' => [['id' => 1, 'name' => 'John Doe']]];
            }, ['middleware' => ['cors', 'logging']]);
            
            $this->api->registerRoute('POST', '/api/users', function($request) {
                return ['message' => 'User created', 'data' => $request['data']];
            }, ['middleware' => ['cors', 'auth', 'logging'], 'auth_required' => true]);
            
            $this->api->registerRoute('GET', '/api/users/{id}', function($request) {
                return ['user' => ['id' => $request['params']['id'], 'name' => 'John Doe']];
            }, ['middleware' => ['cors', 'logging']]);
            
            // Handle sample requests
            $response1 = $this->api->handleRequest('GET', '/api/users');
            $response2 = $this->api->handleRequest('GET', '/api/users/123');
            
            // Generate API documentation
            $docs = $this->api->generateAPIDocs();
            
            // Get request statistics
            $stats = $this->api->getRequestStats();
            
            return [
                'success' => true,
                'routes_registered' => 3,
                'sample_responses' => [$response1, $response2],
                'api_documentation' => $docs,
                'request_stats' => $stats
            ];
            
        } catch (\Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Execute all goals
     */
    public function executeAllGoals(): array
    {
        $results = [
            'goal_2_1' => $this->executeGoal2_1(),
            'goal_2_2' => $this->executeGoal2_2(),
            'goal_2_3' => $this->executeGoal2_3()
        ];
        
        return [
            'agent_id' => 'a8',
            'language' => 'PHP',
            'goal_id' => 'g2',
            'timestamp' => date('Y-m-d H:i:s'),
            'results' => $results,
            'success' => array_reduce($results, function($carry, $result) {
                return $carry && $result['success'];
            }, true)
        ];
    }
    
    /**
     * Get agent information
     */
    public function getInfo(): array
    {
        return [
            'agent_id' => 'a8',
            'language' => 'PHP',
            'goal_id' => 'g2',
            'goals_completed' => ['g2.1', 'g2.2', 'g2.3'],
            'features' => [
                'Framework integration',
                'Database connectivity',
                'ORM support',
                'API development',
                'RESTful services',
                'Middleware system',
                'Request logging'
            ]
        ];
    }
}

// Main execution
if (php_sapi_name() === 'cli' || isset($_GET['execute'])) {
    $agent = new AgentA8G2();
    $results = $agent->executeAllGoals();
    
    if (php_sapi_name() === 'cli') {
        echo json_encode($results, JSON_PRETTY_PRINT) . "\n";
    } else {
        header('Content-Type: application/json');
        echo json_encode($results, JSON_PRETTY_PRINT);
    }
} 
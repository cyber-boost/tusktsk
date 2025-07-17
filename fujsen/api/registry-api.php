<?php
/**
 * TuskLang Package Registry API Handler
 * RESTful API for package management operations
 */

require_once __DIR__ . '/../src/RegistryManager.php';
require_once __DIR__ . '/../src/CDNManager.php';

use TuskPHP\Registry\RegistryManager;
use TuskPHP\Registry\CDNManager;

// Load configuration
$config = [
    'registry' => [
        'name' => 'TuskLang Package Registry',
        'version' => '1.0.0',
        'base_url' => 'https://tusklang.org/packages'
    ],
    'storage' => [
        'database' => 'postgresql://registry:tusklang@localhost/tusklang_registry',
        'cache' => 'redis://localhost:6379/1',
        'file_storage' => '/var/registry/packages'
    ],
    'cdn' => [
        'edge_nodes' => [
            'us-east-1.tusklang.org',
            'us-west-2.tusklang.org',
            'eu-west-1.tusklang.org',
            'ap-southeast-1.tusklang.org'
        ],
        'sync_interval' => '5m',
        'compression' => true
    ],
    'security' => [
        'signing_required' => true,
        'verification_enabled' => true,
        'rate_limit' => 1000,
        'rate_window' => '1h'
    ]
];

// Initialize managers
$registryManager = new RegistryManager($config);
$cdnManager = new CDNManager($config);

// Set response headers
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization');

// Handle preflight requests
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

// Rate limiting
if (!checkRateLimit()) {
    http_response_code(429);
    echo json_encode(['error' => 'Rate limit exceeded']);
    exit;
}

// Parse request
$method = $_SERVER['REQUEST_METHOD'];
$path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
$pathParts = explode('/', trim($path, '/'));

// Route the request
try {
    $response = routeRequest($method, $pathParts);
    echo json_encode($response);
} catch (Exception $e) {
    http_response_code(500);
    echo json_encode(['error' => $e->getMessage()]);
}

/**
 * Route request to appropriate handler
 */
function routeRequest(string $method, array $pathParts): array {
    global $registryManager, $cdnManager;
    
    // Remove 'api' and 'v1' from path
    if (isset($pathParts[0]) && $pathParts[0] === 'api') {
        array_shift($pathParts);
    }
    if (isset($pathParts[0]) && $pathParts[0] === 'v1') {
        array_shift($pathParts);
    }
    
    $resource = $pathParts[0] ?? '';
    $action = $pathParts[1] ?? '';
    $identifier = $pathParts[2] ?? '';
    
    switch ($resource) {
        case 'packages':
            return handlePackages($method, $action, $identifier);
            
        case 'search':
            return handleSearch($method, $action, $identifier);
            
        case 'cdn':
            return handleCDN($method, $action, $identifier);
            
        case 'health':
            return handleHealth();
            
        default:
            throw new Exception('Resource not found');
    }
}

/**
 * Handle package operations
 */
function handlePackages(string $method, string $action, string $identifier): array {
    global $registryManager, $cdnManager;
    
    switch ($method) {
        case 'POST':
            if ($action === 'upload') {
                return handlePackageUpload();
            }
            break;
            
        case 'GET':
            if ($action === 'download' && $identifier) {
                return handlePackageDownload($identifier);
            } elseif ($action === 'metadata' && $identifier) {
                return handlePackageMetadata($identifier);
            } elseif ($action === 'versions' && $identifier) {
                return handlePackageVersions($identifier);
            } elseif ($action === 'dependencies' && $identifier) {
                $version = $_GET['version'] ?? null;
                return handlePackageDependencies($identifier, $version);
            }
            break;
            
        case 'DELETE':
            if ($action === 'delete' && $identifier) {
                return handlePackageDelete($identifier);
            }
            break;
    }
    
    throw new Exception('Invalid package operation');
}

/**
 * Handle package upload
 */
function handlePackageUpload(): array {
    global $registryManager, $cdnManager;
    
    // Validate file upload
    if (!isset($_FILES['package'])) {
        throw new Exception('No package file uploaded');
    }
    
    $file = $_FILES['package'];
    if ($file['error'] !== UPLOAD_ERR_OK) {
        throw new Exception('File upload failed: ' . $file['error']);
    }
    
    // Validate metadata
    $metadata = json_decode($_POST['metadata'] ?? '{}', true);
    if (!isset($metadata['name']) || !isset($metadata['version'])) {
        throw new Exception('Package name and version are required');
    }
    
    $name = $metadata['name'];
    $version = $metadata['version'];
    $filePath = $file['tmp_name'];
    
    // Upload to registry
    $result = $registryManager->uploadPackage($name, $version, $filePath, $metadata);
    
    if ($result['success']) {
        // Distribute to CDN
        $cdnResult = $cdnManager->distributePackage($name, $version, $result['file_path']);
        
        return [
            'success' => true,
            'message' => 'Package uploaded and distributed successfully',
            'package_id' => $result['package_id'],
            'version_id' => $result['version_id'],
            'cdn_distribution' => $cdnResult
        ];
    } else {
        throw new Exception($result['error']);
    }
}

/**
 * Handle package download
 */
function handlePackageDownload(string $packageName): array {
    global $registryManager, $cdnManager;
    
    $version = $_GET['version'] ?? null;
    
    // Get package from registry
    $result = $registryManager->downloadPackage($packageName, $version);
    
    if ($result['success']) {
        // Get optimal CDN node
        $optimalNode = $cdnManager->getOptimalEdgeNode();
        $cdnUrl = "https://$optimalNode/packages/$packageName/" . ($version ?: 'latest') . '.tar.gz';
        
        return [
            'success' => true,
            'download_url' => $cdnUrl,
            'file_size' => $result['file_size'],
            'checksum' => $result['checksum'],
            'metadata' => $result['metadata']
        ];
    } else {
        throw new Exception($result['error']);
    }
}

/**
 * Handle package metadata
 */
function handlePackageMetadata(string $packageName): array {
    global $registryManager;
    
    $result = $registryManager->getPackageMetadata($packageName);
    
    if ($result['success']) {
        return $result;
    } else {
        throw new Exception($result['error']);
    }
}

/**
 * Handle package versions
 */
function handlePackageVersions(string $packageName): array {
    global $registryManager;
    
    $metadata = $registryManager->getPackageMetadata($packageName);
    
    if ($metadata['success']) {
        return [
            'success' => true,
            'package' => $packageName,
            'versions' => $metadata['metadata']['versions'] ?? []
        ];
    } else {
        throw new Exception($metadata['error']);
    }
}

/**
 * Handle package dependencies
 */
function handlePackageDependencies(string $packageName, ?string $version): array {
    global $registryManager;
    
    if (!$version) {
        $metadata = $registryManager->getPackageMetadata($packageName);
        if (!$metadata['success']) {
            throw new Exception($metadata['error']);
        }
        $version = $metadata['metadata']['versions'][0] ?? '1.0.0';
    }
    
    $result = $registryManager->resolveDependencies($packageName, $version);
    
    if ($result['success']) {
        return $result;
    } else {
        throw new Exception($result['error']);
    }
}

/**
 * Handle package deletion
 */
function handlePackageDelete(string $packageName): array {
    global $registryManager, $cdnManager;
    
    $version = $_GET['version'] ?? null;
    
    // Delete from CDN first
    if ($version) {
        $cdnResult = $cdnManager->purgePackage($packageName, $version);
    }
    
    // TODO: Implement package deletion from registry
    // This would require additional methods in RegistryManager
    
    return [
        'success' => true,
        'message' => 'Package deletion initiated',
        'cdn_purge' => $cdnResult ?? null
    ];
}

/**
 * Handle search operations
 */
function handleSearch(string $method, string $action, string $identifier): array {
    global $registryManager;
    
    if ($method !== 'GET') {
        throw new Exception('Search only supports GET method');
    }
    
    $query = $_GET['q'] ?? $identifier;
    $limit = (int)($_GET['limit'] ?? 20);
    $offset = (int)($_GET['offset'] ?? 0);
    
    if (!$query) {
        throw new Exception('Search query is required');
    }
    
    $result = $registryManager->searchPackages($query, $limit, $offset);
    
    if ($result['success']) {
        return $result;
    } else {
        throw new Exception($result['error']);
    }
}

/**
 * Handle CDN operations
 */
function handleCDN(string $method, string $action, string $identifier): array {
    global $cdnManager;
    
    switch ($method) {
        case 'GET':
            if ($action === 'stats') {
                return $cdnManager->getCDNStats();
            } elseif ($action === 'availability' && $identifier) {
                $version = $_GET['version'] ?? null;
                return $cdnManager->checkPackageAvailability($identifier, $version);
            }
            break;
            
        case 'DELETE':
            if ($action === 'purge' && $identifier) {
                $version = $_GET['version'] ?? null;
                return $cdnManager->purgePackage($identifier, $version);
            }
            break;
    }
    
    throw new Exception('Invalid CDN operation');
}

/**
 * Handle health check
 */
function handleHealth(): array {
    global $registryManager, $cdnManager;
    
    $registryHealth = checkRegistryHealth();
    $cdnHealth = checkCDNHealth();
    
    $overallHealth = $registryHealth['healthy'] && $cdnHealth['healthy'];
    
    return [
        'status' => $overallHealth ? 'healthy' : 'unhealthy',
        'timestamp' => date('Y-m-d H:i:s'),
        'registry' => $registryHealth,
        'cdn' => $cdnHealth
    ];
}

/**
 * Check registry health
 */
function checkRegistryHealth(): array {
    global $registryManager;
    
    try {
        // Try to get a simple metadata query
        $result = $registryManager->getPackageMetadata('test-package');
        return [
            'healthy' => true,
            'message' => 'Registry operational'
        ];
    } catch (Exception $e) {
        return [
            'healthy' => false,
            'message' => $e->getMessage()
        ];
    }
}

/**
 * Check CDN health
 */
function checkCDNHealth(): array {
    global $cdnManager;
    
    try {
        $stats = $cdnManager->getCDNStats();
        return [
            'healthy' => true,
            'message' => 'CDN operational',
            'edge_nodes' => $stats['stats']['edge_nodes']
        ];
    } catch (Exception $e) {
        return [
            'healthy' => false,
            'message' => $e->getMessage()
        ];
    }
}

/**
 * Simple rate limiting
 */
function checkRateLimit(): bool {
    global $config;
    
    $clientIP = $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1';
    $redis = new Redis();
    $redis->connect('localhost', 6379);
    $redis->select(3); // Use database 3 for rate limiting
    
    $key = "rate_limit:$clientIP";
    $current = $redis->incr($key);
    
    if ($current === 1) {
        $redis->expire($key, 3600); // 1 hour window
    }
    
    $limit = $config['security']['rate_limit'] ?? 1000;
    
    return $current <= $limit;
} 
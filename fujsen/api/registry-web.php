<?php
/**
 * TuskLang Package Registry Web Interface
 * User-friendly web interface for package management
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
    ]
];

// Initialize managers
$registryManager = new RegistryManager($config);
$cdnManager = new CDNManager($config);

// Get current page and action
$page = $_GET['page'] ?? 'home';
$action = $_GET['action'] ?? '';
$package = $_GET['package'] ?? '';
$version = $_GET['version'] ?? '';

// Handle AJAX requests
if (isset($_GET['ajax'])) {
    header('Content-Type: application/json');
    
    switch ($_GET['ajax']) {
        case 'search':
            $query = $_GET['q'] ?? '';
            $limit = (int)($_GET['limit'] ?? 20);
            $offset = (int)($_GET['offset'] ?? 0);
            
            $result = $registryManager->searchPackages($query, $limit, $offset);
            echo json_encode($result);
            exit;
            
        case 'package_info':
            $packageName = $_GET['package'] ?? '';
            $result = $registryManager->getPackageMetadata($packageName);
            echo json_encode($result);
            exit;
            
        case 'cdn_stats':
            $result = $cdnManager->getCDNStats();
            echo json_encode($result);
            exit;
    }
}

// Handle form submissions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (isset($_POST['action'])) {
        switch ($_POST['action']) {
            case 'upload':
                handlePackageUpload();
                break;
        }
    }
}

function handlePackageUpload() {
    global $registryManager, $cdnManager;
    
    if (!isset($_FILES['package_file']) || $_FILES['package_file']['error'] !== UPLOAD_ERR_OK) {
        $error = 'No package file uploaded or upload failed';
        return;
    }
    
    $metadata = [
        'name' => $_POST['package_name'] ?? '',
        'version' => $_POST['package_version'] ?? '',
        'description' => $_POST['package_description'] ?? '',
        'author' => $_POST['package_author'] ?? '',
        'license' => $_POST['package_license'] ?? 'MIT',
        'homepage' => $_POST['package_homepage'] ?? '',
        'repository' => $_POST['package_repository'] ?? ''
    ];
    
    if (empty($metadata['name']) || empty($metadata['version'])) {
        $error = 'Package name and version are required';
        return;
    }
    
    $result = $registryManager->uploadPackage(
        $metadata['name'],
        $metadata['version'],
        $_FILES['package_file']['tmp_name'],
        $metadata
    );
    
    if ($result['success']) {
        // Distribute to CDN
        $cdnResult = $cdnManager->distributePackage(
            $metadata['name'],
            $metadata['version'],
            $result['file_path']
        );
        
        $success = 'Package uploaded and distributed successfully!';
    } else {
        $error = $result['error'];
    }
}

?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>TuskLang Package Registry</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
        }
        
        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
        }
        
        .header {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            border-radius: 15px;
            padding: 20px;
            margin-bottom: 30px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
        }
        
        .header h1 {
            color: #4a5568;
            font-size: 2.5em;
            margin-bottom: 10px;
            text-align: center;
        }
        
        .header p {
            color: #718096;
            text-align: center;
            font-size: 1.1em;
        }
        
        .nav {
            display: flex;
            justify-content: center;
            gap: 20px;
            margin-top: 20px;
        }
        
        .nav a {
            text-decoration: none;
            color: #4a5568;
            padding: 10px 20px;
            border-radius: 25px;
            background: rgba(255, 255, 255, 0.8);
            transition: all 0.3s ease;
        }
        
        .nav a:hover {
            background: #667eea;
            color: white;
            transform: translateY(-2px);
        }
        
        .main-content {
            background: rgba(255, 255, 255, 0.95);
            backdrop-filter: blur(10px);
            border-radius: 15px;
            padding: 30px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
        }
        
        .search-section {
            margin-bottom: 30px;
        }
        
        .search-box {
            display: flex;
            gap: 10px;
            margin-bottom: 20px;
        }
        
        .search-box input {
            flex: 1;
            padding: 15px;
            border: 2px solid #e2e8f0;
            border-radius: 10px;
            font-size: 16px;
            transition: border-color 0.3s ease;
        }
        
        .search-box input:focus {
            outline: none;
            border-color: #667eea;
        }
        
        .search-box button {
            padding: 15px 30px;
            background: #667eea;
            color: white;
            border: none;
            border-radius: 10px;
            cursor: pointer;
            font-size: 16px;
            transition: background 0.3s ease;
        }
        
        .search-box button:hover {
            background: #5a67d8;
        }
        
        .packages-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }
        
        .package-card {
            background: white;
            border-radius: 15px;
            padding: 20px;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
            transition: transform 0.3s ease, box-shadow 0.3s ease;
        }
        
        .package-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 8px 30px rgba(0, 0, 0, 0.15);
        }
        
        .package-name {
            font-size: 1.3em;
            font-weight: bold;
            color: #2d3748;
            margin-bottom: 5px;
        }
        
        .package-version {
            color: #667eea;
            font-weight: 500;
            margin-bottom: 10px;
        }
        
        .package-description {
            color: #718096;
            margin-bottom: 15px;
            line-height: 1.5;
        }
        
        .package-meta {
            display: flex;
            justify-content: space-between;
            align-items: center;
            font-size: 0.9em;
            color: #a0aec0;
        }
        
        .package-actions {
            display: flex;
            gap: 10px;
            margin-top: 15px;
        }
        
        .btn {
            padding: 8px 16px;
            border: none;
            border-radius: 6px;
            cursor: pointer;
            text-decoration: none;
            font-size: 14px;
            transition: all 0.3s ease;
        }
        
        .btn-primary {
            background: #667eea;
            color: white;
        }
        
        .btn-primary:hover {
            background: #5a67d8;
        }
        
        .btn-secondary {
            background: #e2e8f0;
            color: #4a5568;
        }
        
        .btn-secondary:hover {
            background: #cbd5e0;
        }
        
        .upload-form {
            background: white;
            border-radius: 15px;
            padding: 30px;
            margin-top: 20px;
        }
        
        .form-group {
            margin-bottom: 20px;
        }
        
        .form-group label {
            display: block;
            margin-bottom: 5px;
            font-weight: 500;
            color: #4a5568;
        }
        
        .form-group input,
        .form-group textarea {
            width: 100%;
            padding: 12px;
            border: 2px solid #e2e8f0;
            border-radius: 8px;
            font-size: 16px;
            transition: border-color 0.3s ease;
        }
        
        .form-group input:focus,
        .form-group textarea:focus {
            outline: none;
            border-color: #667eea;
        }
        
        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
        }
        
        .alert {
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        
        .alert-success {
            background: #c6f6d5;
            color: #22543d;
            border: 1px solid #9ae6b4;
        }
        
        .alert-error {
            background: #fed7d7;
            color: #742a2a;
            border: 1px solid #feb2b2;
        }
        
        .loading {
            text-align: center;
            padding: 40px;
            color: #718096;
        }
        
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }
        
        .stat-card {
            background: white;
            border-radius: 10px;
            padding: 20px;
            text-align: center;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
        }
        
        .stat-number {
            font-size: 2em;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 5px;
        }
        
        .stat-label {
            color: #718096;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>📦 TuskLang Package Registry</h1>
            <p>The central hub for TuskLang packages with global CDN distribution</p>
            
            <div class="nav">
                <a href="?page=home">🏠 Home</a>
                <a href="?page=search">🔍 Search</a>
                <a href="?page=upload">📤 Upload</a>
                <a href="?page=stats">📊 Statistics</a>
            </div>
        </div>
        
        <div class="main-content">
            <?php if (isset($success)): ?>
                <div class="alert alert-success"><?php echo htmlspecialchars($success); ?></div>
            <?php endif; ?>
            
            <?php if (isset($error)): ?>
                <div class="alert alert-error"><?php echo htmlspecialchars($error); ?></div>
            <?php endif; ?>
            
            <?php if ($page === 'home' || $page === 'search'): ?>
                <div class="search-section">
                    <div class="search-box">
                        <input type="text" id="searchInput" placeholder="Search packages..." value="<?php echo htmlspecialchars($_GET['q'] ?? ''); ?>">
                        <button onclick="searchPackages()">🔍 Search</button>
                    </div>
                </div>
                
                <div id="packagesContainer">
                    <div class="loading">Loading packages...</div>
                </div>
            <?php elseif ($page === 'upload'): ?>
                <h2>📤 Upload Package</h2>
                <form method="POST" enctype="multipart/form-data" class="upload-form">
                    <input type="hidden" name="action" value="upload">
                    
                    <div class="form-row">
                        <div class="form-group">
                            <label for="package_name">Package Name *</label>
                            <input type="text" id="package_name" name="package_name" required>
                        </div>
                        
                        <div class="form-group">
                            <label for="package_version">Version *</label>
                            <input type="text" id="package_version" name="package_version" required>
                        </div>
                    </div>
                    
                    <div class="form-group">
                        <label for="package_description">Description</label>
                        <textarea id="package_description" name="package_description" rows="3"></textarea>
                    </div>
                    
                    <div class="form-row">
                        <div class="form-group">
                            <label for="package_author">Author</label>
                            <input type="text" id="package_author" name="package_author">
                        </div>
                        
                        <div class="form-group">
                            <label for="package_license">License</label>
                            <input type="text" id="package_license" name="package_license" value="MIT">
                        </div>
                    </div>
                    
                    <div class="form-row">
                        <div class="form-group">
                            <label for="package_homepage">Homepage</label>
                            <input type="url" id="package_homepage" name="package_homepage">
                        </div>
                        
                        <div class="form-group">
                            <label for="package_repository">Repository</label>
                            <input type="url" id="package_repository" name="package_repository">
                        </div>
                    </div>
                    
                    <div class="form-group">
                        <label for="package_file">Package File *</label>
                        <input type="file" id="package_file" name="package_file" accept=".tar.gz,.tgz,.zip" required>
                    </div>
                    
                    <button type="submit" class="btn btn-primary">📤 Upload Package</button>
                </form>
            <?php elseif ($page === 'stats'): ?>
                <h2>📊 Registry Statistics</h2>
                <div id="statsContainer">
                    <div class="loading">Loading statistics...</div>
                </div>
            <?php endif; ?>
        </div>
    </div>
    
    <script>
        // Load packages on page load
        document.addEventListener('DOMContentLoaded', function() {
            if (window.location.search.includes('page=home') || window.location.search.includes('page=search')) {
                searchPackages();
            } else if (window.location.search.includes('page=stats')) {
                loadStats();
            }
        });
        
        // Search packages
        function searchPackages() {
            const query = document.getElementById('searchInput').value;
            const container = document.getElementById('packagesContainer');
            
            container.innerHTML = '<div class="loading">Searching packages...</div>';
            
            fetch(`?ajax=search&q=${encodeURIComponent(query)}&limit=20`)
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        displayPackages(data.results);
                    } else {
                        container.innerHTML = `<div class="alert alert-error">Error: ${data.error}</div>`;
                    }
                })
                .catch(error => {
                    container.innerHTML = `<div class="alert alert-error">Error: ${error.message}</div>`;
                });
        }
        
        // Display packages
        function displayPackages(packages) {
            const container = document.getElementById('packagesContainer');
            
            if (packages.length === 0) {
                container.innerHTML = '<div class="loading">No packages found.</div>';
                return;
            }
            
            const packagesHtml = packages.map(package => `
                <div class="package-card">
                    <div class="package-name">${package.name}</div>
                    <div class="package-version">v${package.version}</div>
                    <div class="package-description">${package.description || 'No description available'}</div>
                    <div class="package-meta">
                        <span>👤 ${package.author || 'Unknown'}</span>
                        <span>📥 ${package.downloads || 0} downloads</span>
                    </div>
                    <div class="package-actions">
                        <a href="?page=package&package=${encodeURIComponent(package.name)}" class="btn btn-primary">📋 Details</a>
                        <a href="?ajax=download&package=${encodeURIComponent(package.name)}&version=${encodeURIComponent(package.version)}" class="btn btn-secondary">📥 Download</a>
                    </div>
                </div>
            `).join('');
            
            container.innerHTML = `<div class="packages-grid">${packagesHtml}</div>`;
        }
        
        // Load statistics
        function loadStats() {
            const container = document.getElementById('statsContainer');
            
            fetch('?ajax=cdn_stats')
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        displayStats(data.stats);
                    } else {
                        container.innerHTML = `<div class="alert alert-error">Error: ${data.error}</div>`;
                    }
                })
                .catch(error => {
                    container.innerHTML = `<div class="alert alert-error">Error: ${error.message}</div>`;
                });
        }
        
        // Display statistics
        function displayStats(stats) {
            const container = document.getElementById('statsContainer');
            
            const statsHtml = `
                <div class="stats-grid">
                    <div class="stat-card">
                        <div class="stat-number">${stats.edge_nodes}</div>
                        <div class="stat-label">Edge Nodes</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-number">${stats.total_packages.toLocaleString()}</div>
                        <div class="stat-label">Total Packages</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-number">${formatBytes(stats.total_size)}</div>
                        <div class="stat-label">Total Size</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-number">${stats.performance.response_time_avg}ms</div>
                        <div class="stat-label">Avg Response Time</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-number">${stats.performance.throughput} MB/s</div>
                        <div class="stat-label">Throughput</div>
                    </div>
                    <div class="stat-card">
                        <div class="stat-number">${stats.performance.cache_hit_rate}%</div>
                        <div class="stat-label">Cache Hit Rate</div>
                    </div>
                </div>
                
                <h3 style="margin-top: 30px; margin-bottom: 15px;">Edge Node Status</h3>
                <div class="stats-grid">
                    ${Object.entries(stats.sync_status).map(([node, status]) => `
                        <div class="stat-card">
                            <div class="stat-number">${status.packages}</div>
                            <div class="stat-label">${node}</div>
                            <div style="font-size: 0.8em; color: #a0aec0; margin-top: 5px;">
                                Last sync: ${status.last_sync}
                            </div>
                        </div>
                    `).join('')}
                </div>
            `;
            
            container.innerHTML = statsHtml;
        }
        
        // Format bytes
        function formatBytes(bytes, decimals = 2) {
            if (bytes === 0) return '0 Bytes';
            
            const k = 1024;
            const dm = decimals < 0 ? 0 : decimals;
            const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
            
            const i = Math.floor(Math.log(bytes) / Math.log(k));
            
            return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
        }
        
        // Search on Enter key
        document.getElementById('searchInput')?.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchPackages();
            }
        });
    </script>
</body>
</html> 
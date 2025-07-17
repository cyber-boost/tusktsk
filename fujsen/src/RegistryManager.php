<?php
/**
 * TuskLang Package Registry Manager
 * Central package management system with version control and dependency resolution
 */

namespace TuskPHP\Registry;

use PDO;
use Redis;
use Exception;

class RegistryManager {
    private PDO $db;
    private Redis $cache;
    private string $storagePath;
    private array $config;
    
    public function __construct(array $config) {
        $this->config = $config;
        $this->initializeDatabase();
        $this->initializeCache();
        $this->storagePath = $config['storage']['file_storage'] ?? '/var/registry/packages';
        $this->ensureStorageDirectory();
    }
    
    /**
     * Initialize database connection
     */
    private function initializeDatabase(): void {
        $dsn = $this->config['storage']['database'];
        $this->db = new PDO($dsn);
        $this->db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        $this->createTables();
    }
    
    /**
     * Initialize Redis cache
     */
    private function initializeCache(): void {
        $redisUrl = $this->config['storage']['cache'];
        $this->cache = new Redis();
        $this->cache->connect('localhost', 6379);
        $this->cache->select(1);
    }
    
    /**
     * Create database tables if they don't exist
     */
    private function createTables(): void {
        $sql = "
            CREATE TABLE IF NOT EXISTS packages (
                id SERIAL PRIMARY KEY,
                name VARCHAR(255) UNIQUE NOT NULL,
                description TEXT,
                author VARCHAR(255),
                license VARCHAR(100),
                homepage VARCHAR(500),
                repository VARCHAR(500),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            
            CREATE TABLE IF NOT EXISTS package_versions (
                id SERIAL PRIMARY KEY,
                package_id INTEGER REFERENCES packages(id),
                version VARCHAR(100) NOT NULL,
                file_path VARCHAR(500) NOT NULL,
                file_size BIGINT NOT NULL,
                checksum VARCHAR(64) NOT NULL,
                signature TEXT,
                dependencies JSONB,
                metadata JSONB,
                downloads INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(package_id, version)
            );
            
            CREATE TABLE IF NOT EXISTS package_dependencies (
                id SERIAL PRIMARY KEY,
                package_id INTEGER REFERENCES packages(id),
                version_id INTEGER REFERENCES package_versions(id),
                dependency_name VARCHAR(255) NOT NULL,
                version_constraint VARCHAR(100) NOT NULL,
                dependency_type VARCHAR(50) DEFAULT 'runtime'
            );
            
            CREATE TABLE IF NOT EXISTS package_search (
                id SERIAL PRIMARY KEY,
                package_id INTEGER REFERENCES packages(id),
                search_vector tsvector,
                keywords TEXT[]
            );
            
            CREATE INDEX IF NOT EXISTS idx_package_versions_package_id ON package_versions(package_id);
            CREATE INDEX IF NOT EXISTS idx_package_versions_version ON package_versions(version);
            CREATE INDEX IF NOT EXISTS idx_package_dependencies_package_id ON package_dependencies(package_id);
            CREATE INDEX IF NOT EXISTS idx_package_search_vector ON package_search USING gin(search_vector);
        ";
        
        $this->db->exec($sql);
    }
    
    /**
     * Ensure storage directory exists
     */
    private function ensureStorageDirectory(): void {
        if (!is_dir($this->storagePath)) {
            mkdir($this->storagePath, 0755, true);
        }
    }
    
    /**
     * Upload a new package version
     */
    public function uploadPackage(string $name, string $version, string $filePath, array $metadata = []): array {
        try {
            // Validate package file
            if (!file_exists($filePath)) {
                throw new Exception("Package file not found: $filePath");
            }
            
            $fileSize = filesize($filePath);
            $checksum = hash_file('sha256', $filePath);
            
            // Check if package exists
            $packageId = $this->getPackageId($name);
            if (!$packageId) {
                $packageId = $this->createPackage($name, $metadata);
            }
            
            // Check if version already exists
            if ($this->versionExists($packageId, $version)) {
                throw new Exception("Version $version already exists for package $name");
            }
            
            // Store file
            $storedPath = $this->storePackageFile($name, $version, $filePath);
            
            // Create version record
            $versionId = $this->createVersion($packageId, $version, $storedPath, $fileSize, $checksum, $metadata);
            
            // Process dependencies
            if (isset($metadata['dependencies'])) {
                $this->processDependencies($versionId, $metadata['dependencies']);
            }
            
            // Update search index
            $this->updateSearchIndex($packageId, $metadata);
            
            // Invalidate cache
            $this->cache->del("package:$name", "package:$name:$version");
            
            return [
                'success' => true,
                'package_id' => $packageId,
                'version_id' => $versionId,
                'file_path' => $storedPath,
                'checksum' => $checksum
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    /**
     * Download a package version
     */
    public function downloadPackage(string $name, string $version = null): array {
        try {
            $packageId = $this->getPackageId($name);
            if (!$packageId) {
                throw new Exception("Package not found: $name");
            }
            
            // Get latest version if not specified
            if (!$version) {
                $version = $this->getLatestVersion($packageId);
            }
            
            $versionData = $this->getVersionData($packageId, $version);
            if (!$versionData) {
                throw new Exception("Version not found: $name@$version");
            }
            
            // Increment download count
            $this->incrementDownloads($versionData['id']);
            
            // Return file path and metadata
            return [
                'success' => true,
                'file_path' => $versionData['file_path'],
                'file_size' => $versionData['file_size'],
                'checksum' => $versionData['checksum'],
                'metadata' => $versionData['metadata']
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    /**
     * Search packages
     */
    public function searchPackages(string $query, int $limit = 20, int $offset = 0): array {
        try {
            $sql = "
                SELECT p.*, pv.version, pv.downloads
                FROM packages p
                JOIN package_versions pv ON p.id = pv.package_id
                JOIN package_search ps ON p.id = ps.package_id
                WHERE ps.search_vector @@ plainto_tsquery('english', ?)
                ORDER BY pv.downloads DESC, p.updated_at DESC
                LIMIT ? OFFSET ?
            ";
            
            $stmt = $this->db->prepare($sql);
            $stmt->execute([$query, $limit, $offset]);
            
            $results = [];
            while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
                $results[] = $row;
            }
            
            return [
                'success' => true,
                'results' => $results,
                'total' => count($results)
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    /**
     * Resolve package dependencies
     */
    public function resolveDependencies(string $name, string $version): array {
        try {
            $packageId = $this->getPackageId($name);
            if (!$packageId) {
                throw new Exception("Package not found: $name");
            }
            
            $versionId = $this->getVersionId($packageId, $version);
            if (!$versionId) {
                throw new Exception("Version not found: $name@$version");
            }
            
            $dependencies = $this->getDependencies($versionId);
            $resolved = [];
            
            foreach ($dependencies as $dep) {
                $resolved[] = [
                    'name' => $dep['dependency_name'],
                    'version' => $this->resolveVersionConstraint($dep['dependency_name'], $dep['version_constraint']),
                    'type' => $dep['dependency_type']
                ];
            }
            
            return [
                'success' => true,
                'dependencies' => $resolved
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    /**
     * Get package metadata
     */
    public function getPackageMetadata(string $name): array {
        try {
            $cacheKey = "package:$name";
            $cached = $this->cache->get($cacheKey);
            
            if ($cached) {
                return json_decode($cached, true);
            }
            
            $sql = "
                SELECT p.*, 
                       array_agg(DISTINCT pv.version) as versions,
                       SUM(pv.downloads) as total_downloads
                FROM packages p
                LEFT JOIN package_versions pv ON p.id = pv.package_id
                WHERE p.name = ?
                GROUP BY p.id
            ";
            
            $stmt = $this->db->prepare($sql);
            $stmt->execute([$name]);
            $result = $stmt->fetch(PDO::FETCH_ASSOC);
            
            if (!$result) {
                throw new Exception("Package not found: $name");
            }
            
            $metadata = [
                'name' => $result['name'],
                'description' => $result['description'],
                'author' => $result['author'],
                'license' => $result['license'],
                'homepage' => $result['homepage'],
                'repository' => $result['repository'],
                'versions' => $result['versions'] ? explode(',', $result['versions']) : [],
                'total_downloads' => (int)$result['total_downloads'],
                'created_at' => $result['created_at'],
                'updated_at' => $result['updated_at']
            ];
            
            // Cache for 1 hour
            $this->cache->setex($cacheKey, 3600, json_encode($metadata));
            
            return [
                'success' => true,
                'metadata' => $metadata
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    // Helper methods
    private function getPackageId(string $name): ?int {
        $stmt = $this->db->prepare("SELECT id FROM packages WHERE name = ?");
        $stmt->execute([$name]);
        return $stmt->fetchColumn() ?: null;
    }
    
    private function createPackage(string $name, array $metadata): int {
        $sql = "
            INSERT INTO packages (name, description, author, license, homepage, repository)
            VALUES (?, ?, ?, ?, ?, ?)
            RETURNING id
        ";
        
        $stmt = $this->db->prepare($sql);
        $stmt->execute([
            $name,
            $metadata['description'] ?? null,
            $metadata['author'] ?? null,
            $metadata['license'] ?? null,
            $metadata['homepage'] ?? null,
            $metadata['repository'] ?? null
        ]);
        
        return $stmt->fetchColumn();
    }
    
    private function versionExists(int $packageId, string $version): bool {
        $stmt = $this->db->prepare("SELECT 1 FROM package_versions WHERE package_id = ? AND version = ?");
        $stmt->execute([$packageId, $version]);
        return (bool)$stmt->fetchColumn();
    }
    
    private function storePackageFile(string $name, string $version, string $filePath): string {
        $packageDir = $this->storagePath . '/' . $name;
        if (!is_dir($packageDir)) {
            mkdir($packageDir, 0755, true);
        }
        
        $storedPath = $packageDir . '/' . $version . '.tar.gz';
        copy($filePath, $storedPath);
        
        return $storedPath;
    }
    
    private function createVersion(int $packageId, string $version, string $filePath, int $fileSize, string $checksum, array $metadata): int {
        $sql = "
            INSERT INTO package_versions (package_id, version, file_path, file_size, checksum, metadata)
            VALUES (?, ?, ?, ?, ?, ?)
            RETURNING id
        ";
        
        $stmt = $this->db->prepare($sql);
        $stmt->execute([
            $packageId,
            $version,
            $filePath,
            $fileSize,
            $checksum,
            json_encode($metadata)
        ]);
        
        return $stmt->fetchColumn();
    }
    
    private function processDependencies(int $versionId, array $dependencies): void {
        $sql = "
            INSERT INTO package_dependencies (package_id, version_id, dependency_name, version_constraint, dependency_type)
            VALUES (?, ?, ?, ?, ?)
        ";
        
        $stmt = $this->db->prepare($sql);
        
        foreach ($dependencies as $dep) {
            $stmt->execute([
                $versionId,
                $versionId,
                $dep['name'],
                $dep['version'] ?? '*',
                $dep['type'] ?? 'runtime'
            ]);
        }
    }
    
    private function updateSearchIndex(int $packageId, array $metadata): void {
        $keywords = [];
        if (isset($metadata['description'])) {
            $keywords[] = $metadata['description'];
        }
        if (isset($metadata['keywords'])) {
            $keywords = array_merge($keywords, $metadata['keywords']);
        }
        
        $searchText = implode(' ', $keywords);
        
        $sql = "
            INSERT INTO package_search (package_id, search_vector, keywords)
            VALUES (?, to_tsvector('english', ?), ?)
            ON CONFLICT (package_id) DO UPDATE SET
                search_vector = to_tsvector('english', ?),
                keywords = ?
        ";
        
        $stmt = $this->db->prepare($sql);
        $stmt->execute([$packageId, $searchText, $keywords, $searchText, $keywords]);
    }
    
    private function getLatestVersion(int $packageId): string {
        $stmt = $this->db->prepare("
            SELECT version FROM package_versions 
            WHERE package_id = ? 
            ORDER BY created_at DESC 
            LIMIT 1
        ");
        $stmt->execute([$packageId]);
        return $stmt->fetchColumn() ?: '1.0.0';
    }
    
    private function getVersionData(int $packageId, string $version): ?array {
        $stmt = $this->db->prepare("
            SELECT * FROM package_versions 
            WHERE package_id = ? AND version = ?
        ");
        $stmt->execute([$packageId, $version]);
        return $stmt->fetch(PDO::FETCH_ASSOC) ?: null;
    }
    
    private function incrementDownloads(int $versionId): void {
        $stmt = $this->db->prepare("
            UPDATE package_versions 
            SET downloads = downloads + 1 
            WHERE id = ?
        ");
        $stmt->execute([$versionId]);
    }
    
    private function getVersionId(int $packageId, string $version): ?int {
        $stmt = $this->db->prepare("
            SELECT id FROM package_versions 
            WHERE package_id = ? AND version = ?
        ");
        $stmt->execute([$packageId, $version]);
        return $stmt->fetchColumn() ?: null;
    }
    
    private function getDependencies(int $versionId): array {
        $stmt = $this->db->prepare("
            SELECT * FROM package_dependencies 
            WHERE version_id = ?
        ");
        $stmt->execute([$versionId]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }
    
    private function resolveVersionConstraint(string $packageName, string $constraint): string {
        // Simple version resolution - in production, use semantic versioning library
        $stmt = $this->db->prepare("
            SELECT version FROM package_versions pv
            JOIN packages p ON pv.package_id = p.id
            WHERE p.name = ?
            ORDER BY created_at DESC
            LIMIT 1
        ");
        $stmt->execute([$packageName]);
        return $stmt->fetchColumn() ?: '1.0.0';
    }
} 
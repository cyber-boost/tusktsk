<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\FileSystem;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\DirectoryOperationException;
use TuskLang\SDK\SystemOperations\Monitoring\DirectoryMonitor;
use TuskLang\SDK\SystemOperations\Sync\CrossPlatformSync;
use TuskLang\SDK\SystemOperations\Archive\IntelligentArchiver;

/**
 * Advanced Directory Operations Operator with Cross-Platform Support
 * 
 * Features:
 * - Cross-platform directory synchronization
 * - Real-time directory monitoring with events
 * - Directory tree operations with performance optimization
 * - Recursive directory operations with progress tracking
 * - Intelligent directory cleanup and archiving
 * 
 * @package TuskLang\SDK\SystemOperations\FileSystem
 * @version 1.0.0
 * @author TuskLang AI System
 */
class DirectoryOperator extends BaseOperator implements OperatorInterface
{
    private DirectoryMonitor $monitor;
    private CrossPlatformSync $syncEngine;
    private IntelligentArchiver $archiver;
    private array $activeOperations = [];
    private array $watchedDirectories = [];
    private int $maxConcurrentOps = 10;

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        
        $this->monitor = new DirectoryMonitor($config['monitor_config'] ?? []);
        $this->syncEngine = new CrossPlatformSync($config['sync_config'] ?? []);
        $this->archiver = new IntelligentArchiver($config['archive_config'] ?? []);
        $this->maxConcurrentOps = $config['max_concurrent_ops'] ?? 10;
        
        $this->initializeOperator();
    }

    /**
     * Create directory with intelligent structure optimization
     */
    public function create(string $dirpath, array $options = []): bool
    {
        try {
            $this->validateDirectoryPath($dirpath);
            
            if (is_dir($dirpath)) {
                return true; // Already exists
            }

            $permissions = $options['permissions'] ?? 0755;
            $recursive = $options['recursive'] ?? true;
            
            $operationId = $this->startOperation('create', $dirpath);
            
            // Optimize directory structure based on intended use
            if (isset($options['optimize_for'])) {
                $permissions = $this->optimizePermissions($options['optimize_for'], $permissions);
            }

            $success = mkdir($dirpath, $permissions, $recursive);
            
            if ($success) {
                // Apply additional optimizations
                if ($options['enable_monitoring'] ?? false) {
                    $this->enableMonitoring($dirpath, $options['monitor_events'] ?? ['all']);
                }
                
                if ($options['sync_enabled'] ?? false) {
                    $this->syncEngine->addDirectory($dirpath, $options['sync_targets'] ?? []);
                }
                
                $this->completeOperation($operationId, true);
                $this->logOperation('create_success', $dirpath, ['permissions' => $permissions]);
                return true;
            } else {
                $this->completeOperation($operationId, false);
                throw new DirectoryOperationException("Failed to create directory: {$dirpath}");
            }

        } catch (\Exception $e) {
            if (isset($operationId)) {
                $this->completeOperation($operationId, false);
            }
            $this->logOperation('create_error', $dirpath, ['error' => $e->getMessage()]);
            throw new DirectoryOperationException("Directory creation failed: {$dirpath}. Error: " . $e->getMessage());
        }
    }

    /**
     * List directory contents with advanced filtering and sorting
     */
    public function listContents(string $dirpath, array $options = []): array
    {
        try {
            $this->validateDirectoryPath($dirpath);
            
            if (!is_dir($dirpath)) {
                throw new DirectoryOperationException("Directory does not exist: {$dirpath}");
            }

            $recursive = $options['recursive'] ?? false;
            $includeHidden = $options['include_hidden'] ?? false;
            $sortBy = $options['sort_by'] ?? 'name';
            $sortOrder = $options['sort_order'] ?? 'asc';
            $filters = $options['filters'] ?? [];

            $operationId = $this->startOperation('list', $dirpath);
            
            if ($recursive) {
                $contents = $this->listContentsRecursive($dirpath, $includeHidden, $filters);
            } else {
                $contents = $this->listContentsFlat($dirpath, $includeHidden, $filters);
            }

            // Apply sorting
            $contents = $this->sortContents($contents, $sortBy, $sortOrder);
            
            // Apply additional metadata
            if ($options['include_metadata'] ?? false) {
                $contents = $this->enhanceWithMetadata($contents);
            }

            $this->completeOperation($operationId, true);
            $this->logOperation('list_success', $dirpath, ['count' => count($contents)]);
            
            return $contents;

        } catch (\Exception $e) {
            if (isset($operationId)) {
                $this->completeOperation($operationId, false);
            }
            $this->logOperation('list_error', $dirpath, ['error' => $e->getMessage()]);
            throw new DirectoryOperationException("Directory listing failed: {$dirpath}. Error: " . $e->getMessage());
        }
    }

    /**
     * Copy directory with progress tracking and optimization
     */
    public function copy(string $source, string $destination, array $options = []): array
    {
        try {
            $this->validateDirectoryPath($source);
            $this->validateDirectoryPath($destination);
            
            if (!is_dir($source)) {
                throw new DirectoryOperationException("Source directory does not exist: {$source}");
            }

            $operationId = $this->startOperation('copy', $source, $destination);
            $progressCallback = $options['progress_callback'] ?? null;
            $overwrite = $options['overwrite'] ?? false;
            $preservePermissions = $options['preserve_permissions'] ?? true;
            
            // Get total file count for progress tracking
            $totalFiles = $this->countFilesRecursive($source);
            $copiedFiles = 0;
            $errors = [];

            // Create destination directory
            if (!is_dir($destination)) {
                mkdir($destination, 0755, true);
            }

            // Copy contents recursively
            $iterator = new \RecursiveIteratorIterator(
                new \RecursiveDirectoryIterator($source, \RecursiveDirectoryIterator::SKIP_DOTS),
                \RecursiveIteratorIterator::SELF_FIRST
            );

            foreach ($iterator as $item) {
                $sourcePath = $item->getRealPath();
                $relativePath = substr($sourcePath, strlen($source) + 1);
                $destPath = $destination . DIRECTORY_SEPARATOR . $relativePath;

                try {
                    if ($item->isDir()) {
                        if (!is_dir($destPath)) {
                            mkdir($destPath, $item->getPerms(), true);
                        }
                        if ($preservePermissions) {
                            chmod($destPath, $item->getPerms());
                        }
                    } else {
                        if (!$overwrite && file_exists($destPath)) {
                            continue; // Skip existing files
                        }
                        
                        copy($sourcePath, $destPath);
                        
                        if ($preservePermissions) {
                            chmod($destPath, $item->getPerms());
                        }
                        
                        $copiedFiles++;
                        
                        // Update progress
                        if ($progressCallback && $totalFiles > 0) {
                            $progress = ($copiedFiles / $totalFiles) * 100;
                            call_user_func($progressCallback, $progress, $copiedFiles, $totalFiles, $relativePath);
                        }
                    }
                } catch (\Exception $e) {
                    $errors[] = [
                        'file' => $relativePath,
                        'error' => $e->getMessage()
                    ];
                }
            }

            $result = [
                'copied_files' => $copiedFiles,
                'total_files' => $totalFiles,
                'errors' => $errors,
                'success' => empty($errors) || count($errors) < ($totalFiles * 0.1) // Success if < 10% errors
            ];

            $this->completeOperation($operationId, $result['success']);
            $this->logOperation('copy_success', $source, [
                'destination' => $destination,
                'copied_files' => $copiedFiles,
                'errors' => count($errors)
            ]);

            return $result;

        } catch (\Exception $e) {
            if (isset($operationId)) {
                $this->completeOperation($operationId, false);
            }
            $this->logOperation('copy_error', $source, [
                'destination' => $destination,
                'error' => $e->getMessage()
            ]);
            throw new DirectoryOperationException("Directory copy failed from {$source} to {$destination}. Error: " . $e->getMessage());
        }
    }

    /**
     * Move directory with atomic operation support
     */
    public function move(string $source, string $destination, array $options = []): bool
    {
        try {
            $this->validateDirectoryPath($source);
            $this->validateDirectoryPath($destination);
            
            if (!is_dir($source)) {
                throw new DirectoryOperationException("Source directory does not exist: {$source}");
            }

            $operationId = $this->startOperation('move', $source, $destination);
            
            // Check if simple rename is possible (same filesystem)
            if ($options['try_rename'] ?? true) {
                if (@rename($source, $destination)) {
                    $this->completeOperation($operationId, true);
                    $this->logOperation('move_success_rename', $source, ['destination' => $destination]);
                    return true;
                }
            }

            // Fall back to copy + delete
            $copyResult = $this->copy($source, $destination, [
                'overwrite' => $options['overwrite'] ?? false,
                'preserve_permissions' => $options['preserve_permissions'] ?? true,
                'progress_callback' => $options['progress_callback'] ?? null
            ]);

            if ($copyResult['success']) {
                $deleteResult = $this->delete($source, [
                    'force' => true,
                    'progress_callback' => $options['progress_callback'] ?? null
                ]);

                if ($deleteResult['success']) {
                    $this->completeOperation($operationId, true);
                    $this->logOperation('move_success_copy_delete', $source, ['destination' => $destination]);
                    return true;
                } else {
                    // Copy succeeded but delete failed - log warning
                    $this->logOperation('move_partial_success', $source, [
                        'destination' => $destination,
                        'warning' => 'Source directory not deleted'
                    ]);
                    $this->completeOperation($operationId, false);
                    return false;
                }
            } else {
                $this->completeOperation($operationId, false);
                throw new DirectoryOperationException("Move operation failed: Copy phase unsuccessful");
            }

        } catch (\Exception $e) {
            if (isset($operationId)) {
                $this->completeOperation($operationId, false);
            }
            $this->logOperation('move_error', $source, [
                'destination' => $destination,
                'error' => $e->getMessage()
            ]);
            throw new DirectoryOperationException("Directory move failed from {$source} to {$destination}. Error: " . $e->getMessage());
        }
    }

    /**
     * Delete directory with progress tracking and safety checks
     */
    public function delete(string $dirpath, array $options = []): array
    {
        try {
            $this->validateDirectoryPath($dirpath);
            
            if (!is_dir($dirpath)) {
                return ['success' => true, 'message' => 'Directory does not exist'];
            }

            $operationId = $this->startOperation('delete', $dirpath);
            $force = $options['force'] ?? false;
            $progressCallback = $options['progress_callback'] ?? null;
            $secureDelete = $options['secure_delete'] ?? false;

            // Safety checks
            if (!$force) {
                $safetyCheck = $this->performSafetyCheck($dirpath);
                if (!$safetyCheck['safe']) {
                    throw new DirectoryOperationException("Safety check failed: " . $safetyCheck['reason']);
                }
            }

            // Count files for progress tracking
            $totalFiles = $this->countFilesRecursive($dirpath);
            $deletedFiles = 0;
            $errors = [];

            // Delete contents recursively (reverse order)
            $iterator = new \RecursiveIteratorIterator(
                new \RecursiveDirectoryIterator($dirpath, \RecursiveDirectoryIterator::SKIP_DOTS),
                \RecursiveIteratorIterator::CHILD_FIRST
            );

            foreach ($iterator as $item) {
                try {
                    $itemPath = $item->getRealPath();
                    
                    if ($item->isDir()) {
                        rmdir($itemPath);
                    } else {
                        if ($secureDelete) {
                            $this->secureDeleteFile($itemPath);
                        } else {
                            unlink($itemPath);
                        }
                        
                        $deletedFiles++;
                        
                        // Update progress
                        if ($progressCallback && $totalFiles > 0) {
                            $progress = ($deletedFiles / $totalFiles) * 100;
                            $relativePath = substr($itemPath, strlen($dirpath) + 1);
                            call_user_func($progressCallback, $progress, $deletedFiles, $totalFiles, $relativePath);
                        }
                    }
                } catch (\Exception $e) {
                    $errors[] = [
                        'file' => $item->getRealPath(),
                        'error' => $e->getMessage()
                    ];
                }
            }

            // Delete the directory itself
            if (empty($errors) || $force) {
                rmdir($dirpath);
            }

            $result = [
                'deleted_files' => $deletedFiles,
                'total_files' => $totalFiles,
                'errors' => $errors,
                'success' => empty($errors)
            ];

            $this->completeOperation($operationId, $result['success']);
            $this->logOperation('delete_success', $dirpath, [
                'deleted_files' => $deletedFiles,
                'errors' => count($errors)
            ]);

            return $result;

        } catch (\Exception $e) {
            if (isset($operationId)) {
                $this->completeOperation($operationId, false);
            }
            $this->logOperation('delete_error', $dirpath, ['error' => $e->getMessage()]);
            throw new DirectoryOperationException("Directory deletion failed: {$dirpath}. Error: " . $e->getMessage());
        }
    }

    /**
     * Synchronize directories across platforms
     */
    public function synchronize(string $source, array $targets, array $options = []): array
    {
        try {
            $this->validateDirectoryPath($source);
            
            if (!is_dir($source)) {
                throw new DirectoryOperationException("Source directory does not exist: {$source}");
            }

            $operationId = $this->startOperation('sync', $source);
            $results = [];

            foreach ($targets as $target) {
                try {
                    $syncResult = $this->syncEngine->synchronize($source, $target, $options);
                    $results[$target] = $syncResult;
                } catch (\Exception $e) {
                    $results[$target] = [
                        'success' => false,
                        'error' => $e->getMessage()
                    ];
                }
            }

            $overallSuccess = !empty($results) && count(array_filter($results, fn($r) => $r['success'])) > 0;
            
            $this->completeOperation($operationId, $overallSuccess);
            $this->logOperation('sync_complete', $source, [
                'targets' => $targets,
                'results' => $results
            ]);

            return $results;

        } catch (\Exception $e) {
            if (isset($operationId)) {
                $this->completeOperation($operationId, false);
            }
            $this->logOperation('sync_error', $source, [
                'targets' => $targets,
                'error' => $e->getMessage()
            ]);
            throw new DirectoryOperationException("Directory synchronization failed: {$source}. Error: " . $e->getMessage());
        }
    }

    /**
     * Enable real-time monitoring for directory
     */
    public function enableMonitoring(string $dirpath, array $events = ['all']): bool
    {
        try {
            $this->validateDirectoryPath($dirpath);
            
            if (!is_dir($dirpath)) {
                throw new DirectoryOperationException("Directory does not exist: {$dirpath}");
            }

            $success = $this->monitor->watchDirectory($dirpath, $events, function($event) {
                $this->handleMonitorEvent($event);
            });

            if ($success) {
                $this->watchedDirectories[$dirpath] = [
                    'events' => $events,
                    'started' => microtime(true)
                ];
                
                $this->logOperation('monitoring_enabled', $dirpath, ['events' => $events]);
                return true;
            }

            return false;

        } catch (\Exception $e) {
            $this->logOperation('monitoring_error', $dirpath, ['error' => $e->getMessage()]);
            throw new DirectoryOperationException("Failed to enable monitoring for: {$dirpath}. Error: " . $e->getMessage());
        }
    }

    /**
     * Disable monitoring for directory
     */
    public function disableMonitoring(string $dirpath): bool
    {
        try {
            $success = $this->monitor->unwatchDirectory($dirpath);
            
            if ($success && isset($this->watchedDirectories[$dirpath])) {
                unset($this->watchedDirectories[$dirpath]);
                $this->logOperation('monitoring_disabled', $dirpath);
                return true;
            }

            return false;

        } catch (\Exception $e) {
            $this->logOperation('monitoring_disable_error', $dirpath, ['error' => $e->getMessage()]);
            throw new DirectoryOperationException("Failed to disable monitoring for: {$dirpath}. Error: " . $e->getMessage());
        }
    }

    /**
     * Archive directory with intelligent compression
     */
    public function archive(string $dirpath, string $archivePath, array $options = []): bool
    {
        try {
            $this->validateDirectoryPath($dirpath);
            
            if (!is_dir($dirpath)) {
                throw new DirectoryOperationException("Directory does not exist: {$dirpath}");
            }

            $operationId = $this->startOperation('archive', $dirpath);
            
            $success = $this->archiver->createArchive($dirpath, $archivePath, $options);
            
            $this->completeOperation($operationId, $success);
            
            if ($success) {
                $this->logOperation('archive_success', $dirpath, [
                    'archive_path' => $archivePath,
                    'compression' => $options['compression'] ?? 'auto'
                ]);
            } else {
                $this->logOperation('archive_failed', $dirpath, ['archive_path' => $archivePath]);
            }

            return $success;

        } catch (\Exception $e) {
            if (isset($operationId)) {
                $this->completeOperation($operationId, false);
            }
            $this->logOperation('archive_error', $dirpath, [
                'archive_path' => $archivePath,
                'error' => $e->getMessage()
            ]);
            throw new DirectoryOperationException("Directory archiving failed: {$dirpath}. Error: " . $e->getMessage());
        }
    }

    /**
     * Get comprehensive directory statistics
     */
    public function getStatistics(string $dirpath): array
    {
        try {
            $this->validateDirectoryPath($dirpath);
            
            if (!is_dir($dirpath)) {
                throw new DirectoryOperationException("Directory does not exist: {$dirpath}");
            }

            $stats = [
                'path' => $dirpath,
                'total_files' => 0,
                'total_directories' => 0,
                'total_size' => 0,
                'largest_file' => null,
                'smallest_file' => null,
                'file_types' => [],
                'permissions' => sprintf('%o', fileperms($dirpath) & 0777),
                'created' => filectime($dirpath),
                'modified' => filemtime($dirpath),
                'depth' => 0,
                'monitoring_enabled' => isset($this->watchedDirectories[$dirpath]),
                'sync_targets' => $this->syncEngine->getTargets($dirpath) ?? []
            ];

            $iterator = new \RecursiveIteratorIterator(
                new \RecursiveDirectoryIterator($dirpath, \RecursiveDirectoryIterator::SKIP_DOTS)
            );

            foreach ($iterator as $item) {
                if ($item->isFile()) {
                    $size = $item->getSize();
                    $stats['total_files']++;
                    $stats['total_size'] += $size;
                    
                    // Track largest/smallest files
                    if (!$stats['largest_file'] || $size > $stats['largest_file']['size']) {
                        $stats['largest_file'] = ['path' => $item->getRealPath(), 'size' => $size];
                    }
                    if (!$stats['smallest_file'] || $size < $stats['smallest_file']['size']) {
                        $stats['smallest_file'] = ['path' => $item->getRealPath(), 'size' => $size];
                    }
                    
                    // Track file types
                    $extension = strtolower($item->getExtension());
                    $stats['file_types'][$extension] = ($stats['file_types'][$extension] ?? 0) + 1;
                    
                    // Track depth
                    $depth = substr_count(str_replace($dirpath, '', $item->getRealPath()), DIRECTORY_SEPARATOR);
                    $stats['depth'] = max($stats['depth'], $depth);
                    
                } elseif ($item->isDir()) {
                    $stats['total_directories']++;
                }
            }

            $this->logOperation('statistics_generated', $dirpath, [
                'files' => $stats['total_files'],
                'directories' => $stats['total_directories'],
                'size' => $stats['total_size']
            ]);

            return $stats;

        } catch (\Exception $e) {
            $this->logOperation('statistics_error', $dirpath, ['error' => $e->getMessage()]);
            throw new DirectoryOperationException("Failed to get directory statistics: {$dirpath}. Error: " . $e->getMessage());
        }
    }

    // Private helper methods

    private function listContentsFlat(string $dirpath, bool $includeHidden, array $filters): array
    {
        $contents = [];
        $handle = opendir($dirpath);
        
        if (!$handle) {
            throw new DirectoryOperationException("Cannot open directory: {$dirpath}");
        }

        while (($item = readdir($handle)) !== false) {
            if ($item === '.' || $item === '..') {
                continue;
            }
            
            if (!$includeHidden && $item[0] === '.') {
                continue;
            }

            $itemPath = $dirpath . DIRECTORY_SEPARATOR . $item;
            $itemInfo = $this->getItemInfo($itemPath);
            
            // Apply filters
            if ($this->passesFilters($itemInfo, $filters)) {
                $contents[] = $itemInfo;
            }
        }

        closedir($handle);
        return $contents;
    }

    private function listContentsRecursive(string $dirpath, bool $includeHidden, array $filters): array
    {
        $contents = [];
        
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($dirpath, \RecursiveDirectoryIterator::SKIP_DOTS)
        );

        foreach ($iterator as $item) {
            if (!$includeHidden && basename($item->getRealPath())[0] === '.') {
                continue;
            }

            $itemInfo = $this->getItemInfo($item->getRealPath());
            
            // Apply filters
            if ($this->passesFilters($itemInfo, $filters)) {
                $contents[] = $itemInfo;
            }
        }

        return $contents;
    }

    private function getItemInfo(string $path): array
    {
        $stat = stat($path);
        return [
            'name' => basename($path),
            'path' => $path,
            'type' => is_dir($path) ? 'directory' : 'file',
            'size' => $stat['size'],
            'permissions' => sprintf('%o', $stat['mode'] & 0777),
            'owner' => $stat['uid'],
            'group' => $stat['gid'],
            'created' => $stat['ctime'],
            'modified' => $stat['mtime'],
            'accessed' => $stat['atime']
        ];
    }

    private function passesFilters(array $itemInfo, array $filters): bool
    {
        foreach ($filters as $filter => $value) {
            switch ($filter) {
                case 'type':
                    if ($itemInfo['type'] !== $value) {
                        return false;
                    }
                    break;
                case 'min_size':
                    if ($itemInfo['size'] < $value) {
                        return false;
                    }
                    break;
                case 'max_size':
                    if ($itemInfo['size'] > $value) {
                        return false;
                    }
                    break;
                case 'extension':
                    $ext = pathinfo($itemInfo['path'], PATHINFO_EXTENSION);
                    if (strtolower($ext) !== strtolower($value)) {
                        return false;
                    }
                    break;
                case 'name_pattern':
                    if (!preg_match($value, $itemInfo['name'])) {
                        return false;
                    }
                    break;
            }
        }
        return true;
    }

    private function sortContents(array $contents, string $sortBy, string $sortOrder): array
    {
        usort($contents, function($a, $b) use ($sortBy, $sortOrder) {
            $valueA = $a[$sortBy] ?? '';
            $valueB = $b[$sortBy] ?? '';
            
            $comparison = 0;
            if (is_numeric($valueA) && is_numeric($valueB)) {
                $comparison = $valueA <=> $valueB;
            } else {
                $comparison = strcasecmp($valueA, $valueB);
            }
            
            return $sortOrder === 'desc' ? -$comparison : $comparison;
        });

        return $contents;
    }

    private function enhanceWithMetadata(array $contents): array
    {
        foreach ($contents as &$item) {
            if ($item['type'] === 'file') {
                $item['mime_type'] = mime_content_type($item['path']) ?: 'unknown';
                $item['is_readable'] = is_readable($item['path']);
                $item['is_writable'] = is_writable($item['path']);
                $item['is_executable'] = is_executable($item['path']);
            } else {
                $item['is_readable'] = is_readable($item['path']);
                $item['is_writable'] = is_writable($item['path']);
                $item['file_count'] = $this->countFilesRecursive($item['path']);
            }
        }

        return $contents;
    }

    private function countFilesRecursive(string $dirpath): int
    {
        if (!is_dir($dirpath)) {
            return 0;
        }

        $count = 0;
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($dirpath, \RecursiveDirectoryIterator::SKIP_DOTS)
        );

        foreach ($iterator as $item) {
            if ($item->isFile()) {
                $count++;
            }
        }

        return $count;
    }

    private function performSafetyCheck(string $dirpath): array
    {
        // Implement safety checks for directory deletion
        $safePaths = ['/tmp', '/var/tmp', sys_get_temp_dir()];
        $dangerousPaths = ['/', '/home', '/usr', '/var', '/etc', '/bin', '/sbin'];

        foreach ($dangerousPaths as $dangerous) {
            if (strpos($dirpath, $dangerous) === 0 && strlen($dirpath) <= strlen($dangerous) + 10) {
                return ['safe' => false, 'reason' => 'Directory is in protected system path'];
            }
        }

        // Check if directory contains important files
        $importantPatterns = ['*.conf', '*.config', '*.ini', '*.key', '*.pem'];
        foreach ($importantPatterns as $pattern) {
            if (!empty(glob($dirpath . '/' . $pattern))) {
                return ['safe' => false, 'reason' => 'Directory contains important configuration files'];
            }
        }

        return ['safe' => true, 'reason' => 'Safety checks passed'];
    }

    private function secureDeleteFile(string $filepath): bool
    {
        // Secure file deletion with multiple overwrites
        $fileSize = filesize($filepath);
        if ($fileSize === false) {
            return unlink($filepath);
        }

        $handle = fopen($filepath, 'r+b');
        if (!$handle) {
            return unlink($filepath);
        }

        // Overwrite with random data
        for ($pass = 0; $pass < 3; $pass++) {
            rewind($handle);
            for ($i = 0; $i < $fileSize; $i += 1024) {
                $randomData = random_bytes(min(1024, $fileSize - $i));
                fwrite($handle, $randomData);
            }
            fflush($handle);
        }

        fclose($handle);
        return unlink($filepath);
    }

    private function optimizePermissions(string $optimizeFor, int $defaultPermissions): int
    {
        switch ($optimizeFor) {
            case 'web':
                return 0755; // Web accessible
            case 'secure':
                return 0700; // Owner only
            case 'shared':
                return 0775; // Group writable
            case 'readonly':
                return 0555; // Read and execute only
            default:
                return $defaultPermissions;
        }
    }

    private function handleMonitorEvent(array $event): void
    {
        // Handle directory monitoring events
        $this->logOperation('monitor_event', $event['path'] ?? '', $event);
        
        // Can trigger additional actions based on event type
        switch ($event['type'] ?? '') {
            case 'file_created':
                // Handle file creation
                break;
            case 'file_deleted':
                // Handle file deletion
                break;
            case 'file_modified':
                // Handle file modification
                break;
            case 'directory_created':
                // Handle directory creation
                break;
            case 'directory_deleted':
                // Handle directory deletion
                break;
        }
    }

    private function startOperation(string $operation, string $path, string $destination = null): string
    {
        $operationId = uniqid('dir_op_', true);
        $this->activeOperations[$operationId] = [
            'operation' => $operation,
            'path' => $path,
            'destination' => $destination,
            'started' => microtime(true),
            'completed' => false
        ];

        return $operationId;
    }

    private function completeOperation(string $operationId, bool $success): void
    {
        if (isset($this->activeOperations[$operationId])) {
            $this->activeOperations[$operationId]['completed'] = true;
            $this->activeOperations[$operationId]['success'] = $success;
            $this->activeOperations[$operationId]['duration'] = microtime(true) - $this->activeOperations[$operationId]['started'];
        }
    }

    private function validateDirectoryPath(string $dirpath): void
    {
        if (empty($dirpath)) {
            throw new DirectoryOperationException("Directory path cannot be empty");
        }

        // Path traversal protection
        if (strpos($dirpath, '..') !== false) {
            throw new DirectoryOperationException("Path traversal not allowed: {$dirpath}");
        }

        // Additional security checks can be added here
    }

    private function initializeOperator(): void
    {
        // Initialize monitoring system
        $this->monitor->initialize();
        
        // Initialize sync engine
        $this->syncEngine->initialize();
        
        // Initialize archiver
        $this->archiver->initialize();
        
        $this->logOperation('directory_operator_initialized', '', [
            'max_concurrent_ops' => $this->maxConcurrentOps
        ]);
    }

    private function logOperation(string $operation, string $path, array $context = []): void
    {
        // Log operation for monitoring and debugging
        $logData = [
            'operation' => $operation,
            'path' => $path,
            'timestamp' => microtime(true),
            'context' => $context
        ];
        
        // Would integrate with logging system
        error_log("DirectoryOperator: " . json_encode($logData));
    }
} 
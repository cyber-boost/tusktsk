<?php
/**
 * File Operator
 * 
 * File manipulation operator for reading, writing, and processing files
 * with various formats and encodings.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskPHP\CoreOperators;

/**
 * File Operator
 * 
 * Provides file manipulation capabilities including reading, writing,
 * and processing files with various formats and encodings.
 */
class FileOperator extends BaseOperator
{
    public function __construct()
    {
        $this->version = '2.0.0';
        $this->requiredFields = ['path'];
        $this->optionalFields = [
            'action', 'content', 'encoding', 'format', 'mode',
            'create', 'overwrite', 'backup'
        ];
        
        $this->defaultConfig = [
            'action' => 'read',
            'encoding' => 'UTF-8',
            'format' => 'text',
            'mode' => 'r',
            'create' => false,
            'overwrite' => false,
            'backup' => false
        ];
    }
    
    public function getName(): string
    {
        return 'file';
    }
    
    protected function getDescription(): string
    {
        return 'File manipulation operator for reading, writing, and processing files with various formats and encodings';
    }
    
    protected function getExamples(): array
    {
        return [
            'read' => '@file({path: "/etc/config.json", format: "json"})',
            'write' => '@file({path: "logs/app.log", action: "write", content: "log message"})',
            'append' => '@file({path: "data.txt", action: "append", content: "new line"})',
            'exists' => '@file({path: "config.ini", action: "exists"})',
            'delete' => '@file({path: "temp.txt", action: "delete"})',
            'simple' => '@file("/etc/hosts")'
        ];
    }
    
    protected function getErrorCodes(): array
    {
        return array_merge(parent::getErrorCodes(), [
            'FILE_NOT_FOUND' => 'File not found',
            'FILE_NOT_READABLE' => 'File is not readable',
            'FILE_NOT_WRITABLE' => 'File is not writable',
            'INVALID_FORMAT' => 'Invalid file format',
            'ENCODING_ERROR' => 'Encoding error',
            'PERMISSION_DENIED' => 'Permission denied'
        ]);
    }
    
    /**
     * Execute file operator
     */
    protected function executeOperator(array $config, array $context): mixed
    {
        $path = $this->resolveVariable($config['path'], $context);
        $action = $config['action'];
        $encoding = $config['encoding'];
        $format = $config['format'];
        
        // Validate path
        $this->validatePath($path);
        
        switch ($action) {
            case 'read':
                return $this->readFile($path, $encoding, $format);
            case 'write':
                return $this->writeFile($path, $config, $context);
            case 'append':
                return $this->appendFile($path, $config, $context);
            case 'exists':
                return $this->fileExists($path);
            case 'delete':
                return $this->deleteFile($path);
            case 'info':
                return $this->getFileInfo($path);
            default:
                throw new \InvalidArgumentException("Unknown file action: {$action}");
        }
    }
    
    /**
     * Validate file path
     */
    private function validatePath(string $path): void
    {
        // Check for directory traversal attempts
        if (strpos($path, '..') !== false) {
            throw new \InvalidArgumentException("Invalid file path: {$path}");
        }
        
        // Check for absolute paths outside allowed directories
        if (strpos($path, '/') === 0) {
            $allowedDirs = ['/etc', '/var', '/tmp', '/home'];
            $allowed = false;
            
            foreach ($allowedDirs as $dir) {
                if (strpos($path, $dir) === 0) {
                    $allowed = true;
                    break;
                }
            }
            
            if (!$allowed) {
                throw new \InvalidArgumentException("Access denied to path: {$path}");
            }
        }
    }
    
    /**
     * Read file
     */
    private function readFile(string $path, string $encoding, string $format): mixed
    {
        if (!file_exists($path)) {
            throw new \RuntimeException("File not found: {$path}");
        }
        
        if (!is_readable($path)) {
            throw new \RuntimeException("File is not readable: {$path}");
        }
        
        $content = file_get_contents($path);
        if ($content === false) {
            throw new \RuntimeException("Failed to read file: {$path}");
        }
        
        // Convert encoding if needed
        if ($encoding !== 'UTF-8') {
            $content = mb_convert_encoding($content, 'UTF-8', $encoding);
        }
        
        // Process format
        return $this->processFormat($content, $format);
    }
    
    /**
     * Write file
     */
    private function writeFile(string $path, array $config, array $context): bool
    {
        $content = $this->resolveVariable($config['content'], $context);
        $create = $config['create'];
        $overwrite = $config['overwrite'];
        $backup = $config['backup'];
        
        // Check if file exists
        if (file_exists($path)) {
            if (!$overwrite) {
                throw new \RuntimeException("File already exists and overwrite is disabled: {$path}");
            }
            
            // Create backup if requested
            if ($backup) {
                $backupPath = $path . '.backup.' . time();
                if (!copy($path, $backupPath)) {
                    throw new \RuntimeException("Failed to create backup: {$path}");
                }
            }
        } else {
            if (!$create) {
                throw new \RuntimeException("File does not exist and create is disabled: {$path}");
            }
            
            // Create directory if needed
            $dir = dirname($path);
            if (!is_dir($dir)) {
                if (!mkdir($dir, 0755, true)) {
                    throw new \RuntimeException("Failed to create directory: {$dir}");
                }
            }
        }
        
        // Convert content to string if needed
        if (!is_string($content)) {
            $content = $this->formatContent($content, $config['format']);
        }
        
        // Write file
        $result = file_put_contents($path, $content, LOCK_EX);
        if ($result === false) {
            throw new \RuntimeException("Failed to write file: {$path}");
        }
        
        $this->log('info', 'File written', [
            'path' => $path,
            'size' => $result,
            'backup' => $backup
        ]);
        
        return true;
    }
    
    /**
     * Append to file
     */
    private function appendFile(string $path, array $config, array $context): bool
    {
        $content = $this->resolveVariable($config['content'], $context);
        
        // Convert content to string if needed
        if (!is_string($content)) {
            $content = $this->formatContent($content, $config['format']);
        }
        
        // Add newline if content doesn't end with one
        if (!str_ends_with($content, "\n")) {
            $content .= "\n";
        }
        
        // Append to file
        $result = file_put_contents($path, $content, FILE_APPEND | LOCK_EX);
        if ($result === false) {
            throw new \RuntimeException("Failed to append to file: {$path}");
        }
        
        $this->log('info', 'File appended', [
            'path' => $path,
            'size' => $result
        ]);
        
        return true;
    }
    
    /**
     * Check if file exists
     */
    private function fileExists(string $path): bool
    {
        return file_exists($path);
    }
    
    /**
     * Delete file
     */
    private function deleteFile(string $path): bool
    {
        if (!file_exists($path)) {
            return false;
        }
        
        if (!is_writable($path)) {
            throw new \RuntimeException("File is not writable: {$path}");
        }
        
        $result = unlink($path);
        if (!$result) {
            throw new \RuntimeException("Failed to delete file: {$path}");
        }
        
        $this->log('info', 'File deleted', ['path' => $path]);
        
        return true;
    }
    
    /**
     * Get file information
     */
    private function getFileInfo(string $path): array
    {
        if (!file_exists($path)) {
            throw new \RuntimeException("File not found: {$path}");
        }
        
        $stat = stat($path);
        
        return [
            'path' => $path,
            'size' => $stat['size'],
            'modified' => $stat['mtime'],
            'created' => $stat['ctime'],
            'permissions' => substr(sprintf('%o', $stat['mode']), -4),
            'readable' => is_readable($path),
            'writable' => is_writable($path),
            'executable' => is_executable($path),
            'type' => filetype($path)
        ];
    }
    
    /**
     * Process content format
     */
    private function processFormat(string $content, string $format): mixed
    {
        switch ($format) {
            case 'json':
                $decoded = json_decode($content, true);
                if (json_last_error() !== JSON_ERROR_NONE) {
                    throw new \RuntimeException("Invalid JSON format: " . json_last_error_msg());
                }
                return $decoded;
                
            case 'yaml':
                if (function_exists('yaml_parse')) {
                    $decoded = yaml_parse($content);
                    if ($decoded === false) {
                        throw new \RuntimeException("Invalid YAML format");
                    }
                    return $decoded;
                } else {
                    throw new \RuntimeException("YAML extension not available");
                }
                
            case 'ini':
                $decoded = parse_ini_string($content, true);
                if ($decoded === false) {
                    throw new \RuntimeException("Invalid INI format");
                }
                return $decoded;
                
            case 'csv':
                return $this->parseCsv($content);
                
            case 'lines':
                return explode("\n", trim($content));
                
            case 'text':
            default:
                return $content;
        }
    }
    
    /**
     * Format content for writing
     */
    private function formatContent(mixed $content, string $format): string
    {
        switch ($format) {
            case 'json':
                return json_encode($content, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);
                
            case 'yaml':
                if (function_exists('yaml_emit')) {
                    return yaml_emit($content);
                } else {
                    throw new \RuntimeException("YAML extension not available");
                }
                
            case 'ini':
                return $this->formatIni($content);
                
            case 'csv':
                return $this->formatCsv($content);
                
            case 'lines':
                if (is_array($content)) {
                    return implode("\n", $content);
                }
                return (string)$content;
                
            case 'text':
            default:
                return (string)$content;
        }
    }
    
    /**
     * Parse CSV content
     */
    private function parseCsv(string $content): array
    {
        $lines = explode("\n", trim($content));
        $result = [];
        
        foreach ($lines as $line) {
            if (trim($line) !== '') {
                $result[] = str_getcsv($line);
            }
        }
        
        return $result;
    }
    
    /**
     * Format CSV content
     */
    private function formatCsv(array $content): string
    {
        $result = '';
        
        foreach ($content as $row) {
            $result .= implode(',', array_map('strval', $row)) . "\n";
        }
        
        return $result;
    }
    
    /**
     * Format INI content
     */
    private function formatIni(array $content): string
    {
        $result = '';
        
        foreach ($content as $section => $values) {
            if (is_array($values)) {
                $result .= "[{$section}]\n";
                foreach ($values as $key => $value) {
                    $result .= "{$key} = " . (is_bool($value) ? ($value ? '1' : '0') : $value) . "\n";
                }
                $result .= "\n";
            } else {
                $result .= "{$section} = " . (is_bool($values) ? ($values ? '1' : '0') : $values) . "\n";
            }
        }
        
        return $result;
    }
    
    /**
     * Custom validation
     */
    protected function customValidate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Validate format
        if (isset($config['format'])) {
            $validFormats = ['text', 'json', 'yaml', 'ini', 'csv', 'lines'];
            if (!in_array($config['format'], $validFormats)) {
                $errors[] = "Invalid format: {$config['format']}";
            }
        }
        
        // Validate encoding
        if (isset($config['encoding'])) {
            if (!in_array($config['encoding'], mb_list_encodings())) {
                $warnings[] = "Unknown encoding: {$config['encoding']}";
            }
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
} 
<?php
/**
 * 🐘 TuskLang SDK Operation Validator
 * ===================================
 * Validates operations across all SDKs with runtime license checking
 */

namespace TuskPHP\License;

class SDKOperationValidator
{
    private static $operations = [
        'parse' => ['php', 'js', 'python', 'rust'],
        'compile' => ['php', 'js', 'python', 'rust'],
        'validate' => ['php', 'js', 'python', 'rust'],
        'execute' => ['php', 'js', 'python', 'rust'],
        'web_request' => ['web'],
        'api_call' => ['api'],
        'database_operation' => ['db'],
        'file_operation' => ['file']
    ];
    
    /**
     * Validate operation for specific SDK
     */
    public static function validate(string $operation, string $sdkType = 'php'): bool
    {
        // Check if operation is supported for SDK type
        if (!isset(self::$operations[$operation]) || !in_array($sdkType, self::$operations[$operation])) {
            self::logViolation("Unsupported operation: {$operation} for SDK: {$sdkType}");
            return false;
        }
        
        // Use runtime validator
        return RuntimeLicenseValidator::validateForOperation($operation, $sdkType);
    }
    
    /**
     * Execute operation with validation
     */
    public static function execute(string $operation, callable $callback, string $sdkType = 'php')
    {
        if (!self::validate($operation, $sdkType)) {
            throw new \Exception("Operation '{$operation}' not allowed for SDK '{$sdkType}'");
        }
        
        return $callback();
    }
    
    /**
     * Log violation
     */
    private static function logViolation(string $reason): void
    {
        RuntimeLicenseValidator::getInstance()->logViolation($reason);
    }
    
    /**
     * Get supported operations for SDK
     */
    public static function getSupportedOperations(string $sdkType): array
    {
        $supported = [];
        foreach (self::$operations as $operation => $sdks) {
            if (in_array($sdkType, $sdks)) {
                $supported[] = $operation;
            }
        }
        return $supported;
    }
    
    /**
     * Check if operation is supported
     */
    public static function isOperationSupported(string $operation, string $sdkType): bool
    {
        return isset(self::$operations[$operation]) && in_array($sdkType, self::$operations[$operation]);
    }
} 
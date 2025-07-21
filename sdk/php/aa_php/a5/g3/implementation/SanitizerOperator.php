<?php

declare(strict_types=1);

namespace TuskLang\A5\G3;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * SanitizerOperator - Advanced data sanitization and cleaning operations
 * 
 * Provides comprehensive data sanitization including XSS protection,
 * SQL injection prevention, input cleaning, and format normalization.
 */
class SanitizerOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'sanitizer';
    }

    public function getDescription(): string 
    {
        return 'Advanced data sanitization and cleaning operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'clean', 'escape_html', 'strip_tags', 'sanitize_string', 'sanitize_email',
            'sanitize_url', 'sanitize_filename', 'remove_special_chars', 'normalize_whitespace',
            'sanitize_sql', 'sanitize_array', 'deep_clean', 'custom_sanitize'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'clean' => $this->cleanValue($params['value'] ?? '', $params['type'] ?? 'string'),
            'escape_html' => $this->escapeHtml($params['value'] ?? ''),
            'strip_tags' => $this->stripTags($params['value'] ?? '', $params['allowed_tags'] ?? ''),
            'sanitize_string' => $this->sanitizeString($params['value'] ?? '', $params['options'] ?? []),
            'sanitize_email' => $this->sanitizeEmail($params['value'] ?? ''),
            'sanitize_url' => $this->sanitizeUrl($params['value'] ?? ''),
            'sanitize_filename' => $this->sanitizeFilename($params['value'] ?? ''),
            'remove_special_chars' => $this->removeSpecialChars($params['value'] ?? '', $params['keep'] ?? ''),
            'normalize_whitespace' => $this->normalizeWhitespace($params['value'] ?? ''),
            'sanitize_sql' => $this->sanitizeSql($params['value'] ?? ''),
            'sanitize_array' => $this->sanitizeArray($params['array'] ?? [], $params['rules'] ?? []),
            'deep_clean' => $this->deepClean($params['data'] ?? [], $params['options'] ?? []),
            'custom_sanitize' => $this->customSanitize($params['value'] ?? '', $params['sanitizer'] ?? null),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * General purpose value cleaner
     */
    private function cleanValue(mixed $value, string $type = 'string'): mixed
    {
        if ($value === null) {
            return null;
        }

        return match($type) {
            'string' => $this->sanitizeString((string) $value),
            'html' => $this->escapeHtml((string) $value),
            'email' => $this->sanitizeEmail((string) $value),
            'url' => $this->sanitizeUrl((string) $value),
            'filename' => $this->sanitizeFilename((string) $value),
            'int', 'integer' => $this->sanitizeInteger($value),
            'float' => $this->sanitizeFloat($value),
            'boolean' => $this->sanitizeBoolean($value),
            'json' => $this->sanitizeJson((string) $value),
            default => $this->sanitizeString((string) $value)
        };
    }

    /**
     * Escape HTML entities to prevent XSS
     */
    private function escapeHtml(string $value): string
    {
        return htmlspecialchars($value, ENT_QUOTES | ENT_HTML5, 'UTF-8');
    }

    /**
     * Strip HTML and PHP tags
     */
    private function stripTags(string $value, string $allowedTags = ''): string
    {
        return strip_tags($value, $allowedTags);
    }

    /**
     * Comprehensive string sanitization
     */
    private function sanitizeString(string $value, array $options = []): string
    {
        // Trim whitespace
        $cleaned = trim($value);
        
        // Normalize line endings
        $cleaned = str_replace(["\r\n", "\r"], "\n", $cleaned);
        
        // Remove null bytes
        $cleaned = str_replace("\0", '', $cleaned);
        
        // Apply options
        if ($options['remove_html'] ?? false) {
            $cleaned = $this->stripTags($cleaned);
        }
        
        if ($options['escape_html'] ?? true) {
            $cleaned = $this->escapeHtml($cleaned);
        }
        
        if ($options['normalize_whitespace'] ?? false) {
            $cleaned = $this->normalizeWhitespace($cleaned);
        }
        
        if ($options['max_length'] ?? null) {
            $cleaned = mb_substr($cleaned, 0, $options['max_length'], 'UTF-8');
        }
        
        return $cleaned;
    }

    /**
     * Sanitize email address
     */
    private function sanitizeEmail(string $email): string
    {
        $email = trim(strtolower($email));
        $email = filter_var($email, FILTER_SANITIZE_EMAIL);
        
        // Additional validation
        if (filter_var($email, FILTER_VALIDATE_EMAIL) === false) {
            return '';
        }
        
        return $email;
    }

    /**
     * Sanitize URL
     */
    private function sanitizeUrl(string $url): string
    {
        $url = trim($url);
        
        // Add protocol if missing
        if (!preg_match('/^https?:\/\//', $url) && !empty($url)) {
            $url = 'http://' . $url;
        }
        
        $url = filter_var($url, FILTER_SANITIZE_URL);
        
        // Validate URL
        if (filter_var($url, FILTER_VALIDATE_URL) === false) {
            return '';
        }
        
        return $url;
    }

    /**
     * Sanitize filename for safe filesystem usage
     */
    private function sanitizeFilename(string $filename): string
    {
        // Remove path traversal attempts
        $filename = basename($filename);
        
        // Remove dangerous characters
        $filename = preg_replace('/[^\w\-_\.]/', '_', $filename);
        
        // Remove multiple underscores
        $filename = preg_replace('/_+/', '_', $filename);
        
        // Remove leading/trailing underscores and dots
        $filename = trim($filename, '_.');
        
        // Ensure filename is not empty
        if (empty($filename)) {
            $filename = 'file_' . uniqid();
        }
        
        // Limit length
        if (strlen($filename) > 255) {
            $extension = pathinfo($filename, PATHINFO_EXTENSION);
            $name = pathinfo($filename, PATHINFO_FILENAME);
            $filename = mb_substr($name, 0, 250 - strlen($extension), 'UTF-8') . '.' . $extension;
        }
        
        return $filename;
    }

    /**
     * Remove special characters
     */
    private function removeSpecialChars(string $value, string $keep = ''): string
    {
        // Create pattern to keep specified characters
        $keepPattern = preg_quote($keep, '/');
        $pattern = '/[^\w\s' . $keepPattern . ']/u';
        
        return preg_replace($pattern, '', $value);
    }

    /**
     * Normalize whitespace
     */
    private function normalizeWhitespace(string $value): string
    {
        // Replace multiple whitespace with single space
        $value = preg_replace('/\s+/', ' ', $value);
        
        // Trim
        return trim($value);
    }

    /**
     * Sanitize for SQL queries (basic - use prepared statements instead)
     */
    private function sanitizeSql(string $value): string
    {
        // This is basic sanitization - prepared statements are preferred
        $value = str_replace(['"', "'", '\\', "\0", "\n", "\r", "\x1a"], 
                           ['""', "''", '\\\\', '', '', '', ''], $value);
        
        return $value;
    }

    /**
     * Sanitize array of values
     */
    private function sanitizeArray(array $array, array $rules = []): array
    {
        $sanitized = [];
        
        foreach ($array as $key => $value) {
            $sanitizedKey = $this->sanitizeString((string) $key);
            
            if (is_array($value)) {
                $sanitized[$sanitizedKey] = $this->sanitizeArray($value, $rules);
            } else {
                $rule = $rules[$key] ?? $rules['*'] ?? 'string';
                $sanitized[$sanitizedKey] = $this->cleanValue($value, $rule);
            }
        }
        
        return $sanitized;
    }

    /**
     * Deep clean complex data structures
     */
    private function deepClean(mixed $data, array $options = []): mixed
    {
        if (is_array($data)) {
            $cleaned = [];
            foreach ($data as $key => $value) {
                $cleanKey = $this->sanitizeString((string) $key);
                $cleaned[$cleanKey] = $this->deepClean($value, $options);
            }
            return $cleaned;
        }
        
        if (is_object($data)) {
            $cleaned = new \stdClass();
            foreach (get_object_vars($data) as $key => $value) {
                $cleanKey = $this->sanitizeString((string) $key);
                $cleaned->$cleanKey = $this->deepClean($value, $options);
            }
            return $cleaned;
        }
        
        if (is_string($data)) {
            return $this->sanitizeString($data, $options);
        }
        
        return $data;
    }

    /**
     * Apply custom sanitization function
     */
    private function customSanitize(mixed $value, ?callable $sanitizer): mixed
    {
        if ($sanitizer === null) {
            throw new InvalidArgumentException('Custom sanitizer function is required');
        }
        
        try {
            return $sanitizer($value);
        } catch (\Exception $e) {
            throw new InvalidArgumentException('Custom sanitizer failed: ' . $e->getMessage());
        }
    }

    /**
     * Sanitize integer value
     */
    private function sanitizeInteger(mixed $value): int
    {
        if (is_int($value)) {
            return $value;
        }
        
        if (is_string($value)) {
            $value = trim($value);
            if (ctype_digit($value) || (str_starts_with($value, '-') && ctype_digit(substr($value, 1)))) {
                return (int) $value;
            }
        }
        
        return filter_var($value, FILTER_SANITIZE_NUMBER_INT);
    }

    /**
     * Sanitize float value
     */
    private function sanitizeFloat(mixed $value): float
    {
        if (is_float($value)) {
            return $value;
        }
        
        $sanitized = filter_var($value, FILTER_SANITIZE_NUMBER_FLOAT, FILTER_FLAG_ALLOW_FRACTION | FILTER_FLAG_ALLOW_THOUSAND);
        
        return (float) $sanitized;
    }

    /**
     * Sanitize boolean value
     */
    private function sanitizeBoolean(mixed $value): bool
    {
        if (is_bool($value)) {
            return $value;
        }
        
        if (is_string($value)) {
            $value = strtolower(trim($value));
            return in_array($value, ['true', '1', 'yes', 'on'], true);
        }
        
        return (bool) $value;
    }

    /**
     * Sanitize JSON string
     */
    private function sanitizeJson(string $value): string
    {
        $value = trim($value);
        
        // Validate JSON
        json_decode($value);
        if (json_last_error() !== JSON_ERROR_NONE) {
            return '{}';
        }
        
        return $value;
    }

    /**
     * Remove invisible characters
     */
    private function removeInvisibleChars(string $value): string
    {
        $nonPrintables = [];
        
        // Build array of non-printable characters
        for ($i = 0; $i < 32; $i++) {
            if ($i !== 9 && $i !== 10 && $i !== 13) { // Keep tab, line feed, carriage return
                $nonPrintables[] = chr($i);
            }
        }
        
        return str_replace($nonPrintables, '', $value);
    }

    /**
     * Sanitize for specific contexts
     */
    private function sanitizeForContext(string $value, string $context): string
    {
        return match($context) {
            'html_attribute' => htmlspecialchars($value, ENT_QUOTES | ENT_HTML5, 'UTF-8'),
            'javascript' => json_encode($value, JSON_HEX_TAG | JSON_HEX_APOS | JSON_HEX_QUOT | JSON_HEX_AMP),
            'css' => preg_replace('/[^\w\-]/', '', $value),
            'url_parameter' => urlencode($value),
            'shell_command' => escapeshellarg($value),
            'regex' => preg_quote($value, '/'),
            default => $this->sanitizeString($value)
        };
    }

    /**
     * Advanced HTML sanitization with whitelist
     */
    private function sanitizeHtmlAdvanced(string $html, array $allowedTags = [], array $allowedAttributes = []): string
    {
        // This is a basic implementation - consider using HTMLPurifier for production
        $allowedTagsStr = '<' . implode('><', $allowedTags) . '>';
        $cleaned = strip_tags($html, $allowedTagsStr);
        
        // Remove dangerous attributes (basic approach)
        $dangerousAttrs = ['onload', 'onclick', 'onmouseover', 'onfocus', 'onerror', 'javascript:', 'data:'];
        
        foreach ($dangerousAttrs as $attr) {
            $cleaned = preg_replace('/' . preg_quote($attr, '/') . '[^>]*>/i', '>', $cleaned);
        }
        
        return $cleaned;
    }

    /**
     * Sanitize phone number
     */
    private function sanitizePhoneNumber(string $phone): string
    {
        // Remove all non-digit characters except + and -
        $phone = preg_replace('/[^\d\+\-]/', '', $phone);
        
        // Basic formatting validation
        if (strlen($phone) < 7) {
            return '';
        }
        
        return $phone;
    }

    /**
     * Sanitize credit card number (for logging - never store full numbers)
     */
    private function sanitizeCreditCard(string $cardNumber): string
    {
        $cardNumber = preg_replace('/\D/', '', $cardNumber);
        
        if (strlen($cardNumber) < 13 || strlen($cardNumber) > 19) {
            return 'INVALID';
        }
        
        // Mask all but last 4 digits
        return str_repeat('*', strlen($cardNumber) - 4) . substr($cardNumber, -4);
    }

    /**
     * Remove potential XSS vectors
     */
    private function removeXssVectors(string $value): string
    {
        $xssPatterns = [
            '/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/mi',
            '/<iframe\b[^<]*(?:(?!<\/iframe>)<[^<]*)*<\/iframe>/mi',
            '/<object\b[^<]*(?:(?!<\/object>)<[^<]*)*<\/object>/mi',
            '/<embed\b[^>]*>/mi',
            '/<applet\b[^<]*(?:(?!<\/applet>)<[^<]*)*<\/applet>/mi',
            '/javascript:/mi',
            '/vbscript:/mi',
            '/data:text\/html/mi',
            '/on\w+\s*=/mi'
        ];
        
        foreach ($xssPatterns as $pattern) {
            $value = preg_replace($pattern, '', $value);
        }
        
        return $value;
    }

    /**
     * Normalize Unicode characters
     */
    private function normalizeUnicode(string $value): string
    {
        if (!class_exists('Normalizer')) {
            return $value;
        }
        
        return \Normalizer::normalize($value, \Normalizer::FORM_C);
    }
} 
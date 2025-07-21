<?php

declare(strict_types=1);

namespace TuskLang\A5\G3;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * InputCleanOperator - Security-focused input cleaning and XSS protection
 * 
 * Provides advanced security-focused input cleaning including XSS prevention,
 * injection protection, content security, and comprehensive threat mitigation.
 */
class InputCleanOperator extends CoreOperator
{
    private array $xssPatterns;
    private array $sqlInjectionPatterns;
    private array $allowedHtmlTags;
    private array $securityConfig;

    public function __construct()
    {
        $this->initializeSecurityPatterns();
        $this->initializeConfig();
    }

    public function getName(): string
    {
        return 'input_clean';
    }

    public function getDescription(): string 
    {
        return 'Security-focused input cleaning and XSS protection';
    }

    public function getSupportedActions(): array
    {
        return [
            'clean_xss', 'prevent_sql_injection', 'sanitize_user_input', 'clean_html',
            'remove_scripts', 'clean_attributes', 'validate_csrf', 'clean_uploads',
            'detect_threats', 'security_scan', 'content_filter', 'deep_security_clean'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'clean_xss' => $this->cleanXSS($params['input'] ?? '', $params['level'] ?? 'strict'),
            'prevent_sql_injection' => $this->preventSqlInjection($params['input'] ?? ''),
            'sanitize_user_input' => $this->sanitizeUserInput($params['input'] ?? [], $params['rules'] ?? []),
            'clean_html' => $this->cleanHtml($params['html'] ?? '', $params['config'] ?? []),
            'remove_scripts' => $this->removeScripts($params['input'] ?? ''),
            'clean_attributes' => $this->cleanAttributes($params['html'] ?? '', $params['allowed'] ?? []),
            'validate_csrf' => $this->validateCsrf($params['token'] ?? '', $params['session_token'] ?? ''),
            'clean_uploads' => $this->cleanUploads($params['file_info'] ?? []),
            'detect_threats' => $this->detectThreats($params['input'] ?? ''),
            'security_scan' => $this->securityScan($params['data'] ?? []),
            'content_filter' => $this->contentFilter($params['content'] ?? '', $params['filters'] ?? []),
            'deep_security_clean' => $this->deepSecurityClean($params['data'] ?? [], $params['config'] ?? []),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Clean XSS vectors with configurable security level
     */
    private function cleanXSS(string $input, string $level = 'strict'): array
    {
        $original = $input;
        $cleaned = $input;
        $threats = [];

        // Remove null bytes
        $cleaned = str_replace("\0", '', $cleaned);

        // Detect and remove XSS patterns
        foreach ($this->xssPatterns[$level] ?? $this->xssPatterns['strict'] as $pattern => $replacement) {
            if (preg_match($pattern, $cleaned)) {
                $threats[] = [
                    'type' => 'xss',
                    'pattern' => $pattern,
                    'severity' => $this->getPatternSeverity($pattern)
                ];
            }
            $cleaned = preg_replace($pattern, $replacement, $cleaned);
        }

        // Additional XSS cleaning
        $cleaned = $this->removeEventHandlers($cleaned);
        $cleaned = $this->removeJavascriptProtocol($cleaned);
        $cleaned = $this->removeDataUrls($cleaned);

        return [
            'original' => $original,
            'cleaned' => $cleaned,
            'threats_found' => count($threats),
            'threats' => $threats,
            'security_level' => $level,
            'is_safe' => empty($threats)
        ];
    }

    /**
     * Prevent SQL injection attacks
     */
    private function preventSqlInjection(string $input): array
    {
        $original = $input;
        $cleaned = $input;
        $threats = [];

        foreach ($this->sqlInjectionPatterns as $pattern => $description) {
            if (preg_match($pattern, $input)) {
                $threats[] = [
                    'type' => 'sql_injection',
                    'pattern' => $pattern,
                    'description' => $description,
                    'severity' => 'high'
                ];
            }
        }

        // Basic SQL injection prevention (use prepared statements in production)
        $cleaned = addslashes($cleaned);
        $cleaned = htmlspecialchars($cleaned, ENT_QUOTES, 'UTF-8');

        return [
            'original' => $original,
            'cleaned' => $cleaned,
            'threats_found' => count($threats),
            'threats' => $threats,
            'is_safe' => empty($threats),
            'recommendation' => 'Use prepared statements for database queries'
        ];
    }

    /**
     * Comprehensive user input sanitization
     */
    private function sanitizeUserInput(array $input, array $rules = []): array
    {
        $sanitized = [];
        $threats = [];
        $errors = [];

        foreach ($input as $field => $value) {
            $fieldRules = $rules[$field] ?? $rules['*'] ?? ['xss', 'sql', 'trim'];
            
            try {
                $result = $this->applySanitizationRules($value, $fieldRules);
                $sanitized[$field] = $result['cleaned'];
                
                if (!empty($result['threats'])) {
                    $threats[$field] = $result['threats'];
                }
            } catch (\Exception $e) {
                $errors[$field] = $e->getMessage();
                $sanitized[$field] = '';
            }
        }

        return [
            'sanitized' => $sanitized,
            'threats' => $threats,
            'errors' => $errors,
            'total_threats' => array_sum(array_map('count', $threats)),
            'is_safe' => empty($threats)
        ];
    }

    /**
     * Clean HTML with whitelist approach
     */
    private function cleanHtml(string $html, array $config = []): array
    {
        $allowedTags = $config['allowed_tags'] ?? $this->allowedHtmlTags;
        $allowedAttributes = $config['allowed_attributes'] ?? [];
        
        $original = $html;
        $cleaned = $html;
        $threats = [];

        // Remove dangerous tags
        $dangerousTags = ['script', 'iframe', 'object', 'embed', 'applet', 'form', 'meta', 'link'];
        foreach ($dangerousTags as $tag) {
            if (stripos($cleaned, "<{$tag}") !== false) {
                $threats[] = [
                    'type' => 'dangerous_tag',
                    'tag' => $tag,
                    'severity' => 'high'
                ];
            }
        }

        // Strip all tags except allowed ones
        $allowedTagsString = '<' . implode('><', $allowedTags) . '>';
        $cleaned = strip_tags($cleaned, $allowedTagsString);

        // Clean attributes
        $cleaned = $this->cleanHtmlAttributes($cleaned, $allowedAttributes);

        return [
            'original' => $original,
            'cleaned' => $cleaned,
            'threats_found' => count($threats),
            'threats' => $threats,
            'allowed_tags' => $allowedTags,
            'is_safe' => empty($threats)
        ];
    }

    /**
     * Remove all script tags and JavaScript
     */
    private function removeScripts(string $input): array
    {
        $original = $input;
        $threats = [];

        // Detect script tags
        if (preg_match_all('/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/mi', $input, $matches)) {
            $threats = array_merge($threats, array_map(fn($match) => [
                'type' => 'script_tag',
                'content' => $match,
                'severity' => 'critical'
            ], $matches[0]));
        }

        // Remove script tags
        $cleaned = preg_replace('/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/mi', '', $input);
        
        // Remove javascript: protocols
        $cleaned = preg_replace('/javascript:/i', '', $cleaned);
        
        // Remove vbscript: protocols
        $cleaned = preg_replace('/vbscript:/i', '', $cleaned);

        return [
            'original' => $original,
            'cleaned' => $cleaned,
            'threats_found' => count($threats),
            'threats' => $threats,
            'is_safe' => empty($threats)
        ];
    }

    /**
     * Clean HTML attributes
     */
    private function cleanAttributes(string $html, array $allowedAttributes = []): string
    {
        // Remove event handlers
        $html = preg_replace('/\s*on\w+\s*=\s*["\'][^"\']*["\']?/i', '', $html);
        
        // Remove style attributes if not allowed
        if (!in_array('style', $allowedAttributes)) {
            $html = preg_replace('/\s*style\s*=\s*["\'][^"\']*["\']?/i', '', $html);
        }
        
        // Remove dangerous attributes
        $dangerousAttrs = ['formaction', 'action', 'background', 'codebase', 'dynsrc', 'lowsrc'];
        foreach ($dangerousAttrs as $attr) {
            $html = preg_replace("/\s*{$attr}\s*=\s*[\"'][^\"']*[\"']?/i", '', $html);
        }

        return $html;
    }

    /**
     * Validate CSRF token
     */
    private function validateCsrf(string $token, string $sessionToken): array
    {
        $isValid = !empty($token) && !empty($sessionToken) && hash_equals($sessionToken, $token);
        
        return [
            'valid' => $isValid,
            'token_provided' => !empty($token),
            'session_token_exists' => !empty($sessionToken),
            'security_level' => $isValid ? 'high' : 'critical_risk'
        ];
    }

    /**
     * Clean file upload information
     */
    private function cleanUploads(array $fileInfo): array
    {
        $threats = [];
        $cleaned = $fileInfo;

        // Check file extension
        $filename = $fileInfo['name'] ?? '';
        $extension = strtolower(pathinfo($filename, PATHINFO_EXTENSION));
        
        $dangerousExtensions = ['php', 'phtml', 'php3', 'php4', 'php5', 'phar', 'exe', 'bat', 'cmd', 'scr', 'vbs', 'js'];
        if (in_array($extension, $dangerousExtensions)) {
            $threats[] = [
                'type' => 'dangerous_file_extension',
                'extension' => $extension,
                'severity' => 'critical'
            ];
        }

        // Clean filename
        $cleaned['name'] = $this->sanitizeFilename($filename);
        
        // Check MIME type
        $mimeType = $fileInfo['type'] ?? '';
        if (!$this->isAllowedMimeType($mimeType)) {
            $threats[] = [
                'type' => 'suspicious_mime_type',
                'mime_type' => $mimeType,
                'severity' => 'high'
            ];
        }

        return [
            'original' => $fileInfo,
            'cleaned' => $cleaned,
            'threats_found' => count($threats),
            'threats' => $threats,
            'is_safe' => empty($threats)
        ];
    }

    /**
     * Detect various security threats
     */
    private function detectThreats(string $input): array
    {
        $threats = [];
        
        // XSS detection
        foreach ($this->xssPatterns['strict'] as $pattern => $replacement) {
            if (preg_match($pattern, $input)) {
                $threats[] = [
                    'type' => 'xss',
                    'severity' => 'high',
                    'pattern' => $pattern
                ];
            }
        }
        
        // SQL injection detection
        foreach ($this->sqlInjectionPatterns as $pattern => $description) {
            if (preg_match($pattern, $input)) {
                $threats[] = [
                    'type' => 'sql_injection',
                    'severity' => 'critical',
                    'description' => $description
                ];
            }
        }
        
        // Path traversal detection
        if (preg_match('/\.\.[\/\\\\]/', $input)) {
            $threats[] = [
                'type' => 'path_traversal',
                'severity' => 'high',
                'description' => 'Potential directory traversal attempt'
            ];
        }
        
        // Command injection detection
        $cmdPatterns = ['/[;&|`]/', '/\$\(/', '/`[^`]+`/'];
        foreach ($cmdPatterns as $pattern) {
            if (preg_match($pattern, $input)) {
                $threats[] = [
                    'type' => 'command_injection',
                    'severity' => 'critical',
                    'pattern' => $pattern
                ];
            }
        }

        return [
            'input' => $input,
            'threats_found' => count($threats),
            'threats' => $threats,
            'risk_level' => $this->calculateRiskLevel($threats),
            'is_safe' => empty($threats)
        ];
    }

    /**
     * Comprehensive security scan
     */
    private function securityScan(array $data): array
    {
        $results = [];
        $totalThreats = 0;
        $highestRisk = 'low';

        foreach ($data as $key => $value) {
            if (is_string($value)) {
                $scanResult = $this->detectThreats($value);
                $results[$key] = $scanResult;
                $totalThreats += $scanResult['threats_found'];
                
                if ($scanResult['risk_level'] === 'critical') {
                    $highestRisk = 'critical';
                } elseif ($scanResult['risk_level'] === 'high' && $highestRisk !== 'critical') {
                    $highestRisk = 'high';
                }
            } elseif (is_array($value)) {
                $nestedResult = $this->securityScan($value);
                $results[$key] = $nestedResult;
                $totalThreats += $nestedResult['total_threats'];
            }
        }

        return [
            'results' => $results,
            'total_threats' => $totalThreats,
            'highest_risk_level' => $highestRisk,
            'is_safe' => $totalThreats === 0,
            'scan_timestamp' => time()
        ];
    }

    /**
     * Content filtering with custom filters
     */
    private function contentFilter(string $content, array $filters): array
    {
        $filtered = $content;
        $applied = [];

        foreach ($filters as $filterName => $filterConfig) {
            $result = $this->applyContentFilter($filtered, $filterName, $filterConfig);
            $filtered = $result['content'];
            $applied[$filterName] = $result;
        }

        return [
            'original' => $content,
            'filtered' => $filtered,
            'filters_applied' => array_keys($applied),
            'filter_results' => $applied
        ];
    }

    /**
     * Deep security cleaning for complex data structures
     */
    private function deepSecurityClean(mixed $data, array $config = []): array
    {
        if (is_string($data)) {
            return $this->cleanXSS($data, $config['xss_level'] ?? 'strict');
        }
        
        if (is_array($data)) {
            $cleaned = [];
            $threats = [];
            
            foreach ($data as $key => $value) {
                $cleanKey = $this->sanitizeKey($key);
                $result = $this->deepSecurityClean($value, $config);
                
                $cleaned[$cleanKey] = $result['cleaned'] ?? $result;
                
                if (isset($result['threats']) && !empty($result['threats'])) {
                    $threats[$cleanKey] = $result['threats'];
                }
            }
            
            return [
                'cleaned' => $cleaned,
                'threats' => $threats,
                'is_safe' => empty($threats)
            ];
        }
        
        return ['cleaned' => $data, 'threats' => [], 'is_safe' => true];
    }

    /**
     * Initialize security patterns
     */
    private function initializeSecurityPatterns(): void
    {
        $this->xssPatterns = [
            'strict' => [
                '/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/mi' => '',
                '/javascript:/i' => '',
                '/vbscript:/i' => '',
                '/onload\s*=/i' => '',
                '/onerror\s*=/i' => '',
                '/onclick\s*=/i' => '',
                '/onmouseover\s*=/i' => '',
                '/onfocus\s*=/i' => '',
                '/onblur\s*=/i' => '',
                '/onchange\s*=/i' => '',
                '/onsubmit\s*=/i' => '',
                '/<iframe\b[^<]*(?:(?!<\/iframe>)<[^<]*)*<\/iframe>/mi' => '',
                '/<object\b[^<]*(?:(?!<\/object>)<[^<]*)*<\/object>/mi' => '',
                '/<embed\b[^>]*>/mi' => '',
                '/<applet\b[^<]*(?:(?!<\/applet>)<[^<]*)*<\/applet>/mi' => '',
                '/data:text\/html/i' => '',
            ],
            'moderate' => [
                '/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/mi' => '',
                '/javascript:/i' => '',
                '/vbscript:/i' => '',
                '/onload\s*=/i' => '',
                '/onerror\s*=/i' => '',
            ]
        ];

        $this->sqlInjectionPatterns = [
            '/(\s*(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s)/i' => 'SQL keyword injection',
            '/(\s*(or|and)\s+[\'"]?\s*[\'"]?\s*[=])/i' => 'Boolean-based injection',
            '/([\'"];?\s*(drop|delete|update|insert)\s)/i' => 'Destructive SQL injection',
            '/(\/\*.*?\*\/)/i' => 'SQL comment injection',
            '/(-{2}.*$)/m' => 'SQL line comment injection',
            '/(\s*[\'"]?\s*[=]\s*[\'"]?\s*(or|and))/i' => 'Conditional injection',
        ];
    }

    /**
     * Initialize configuration
     */
    private function initializeConfig(): void
    {
        $this->allowedHtmlTags = ['p', 'br', 'strong', 'em', 'b', 'i', 'u', 'span', 'div'];
        
        $this->securityConfig = [
            'max_input_length' => 10000,
            'allow_html' => false,
            'strict_mode' => true,
            'log_threats' => true
        ];
    }

    /**
     * Apply sanitization rules to value
     */
    private function applySanitizationRules(mixed $value, array $rules): array
    {
        $cleaned = $value;
        $threats = [];

        foreach ($rules as $rule) {
            switch ($rule) {
                case 'xss':
                    $result = $this->cleanXSS((string) $cleaned);
                    $cleaned = $result['cleaned'];
                    $threats = array_merge($threats, $result['threats']);
                    break;
                    
                case 'sql':
                    $result = $this->preventSqlInjection((string) $cleaned);
                    $cleaned = $result['cleaned'];
                    $threats = array_merge($threats, $result['threats']);
                    break;
                    
                case 'trim':
                    $cleaned = is_string($cleaned) ? trim($cleaned) : $cleaned;
                    break;
                    
                case 'html_entities':
                    $cleaned = htmlentities((string) $cleaned, ENT_QUOTES, 'UTF-8');
                    break;
            }
        }

        return [
            'cleaned' => $cleaned,
            'threats' => $threats
        ];
    }

    /**
     * Remove event handlers from HTML
     */
    private function removeEventHandlers(string $html): string
    {
        return preg_replace('/\s*on\w+\s*=\s*["\'][^"\']*["\']?/i', '', $html);
    }

    /**
     * Remove javascript: protocol
     */
    private function removeJavascriptProtocol(string $input): string
    {
        return preg_replace('/javascript:/i', '', $input);
    }

    /**
     * Remove data URLs
     */
    private function removeDataUrls(string $input): string
    {
        return preg_replace('/data:(?:text\/html|application\/javascript)/i', '', $input);
    }

    /**
     * Get pattern severity level
     */
    private function getPatternSeverity(string $pattern): string
    {
        $criticalPatterns = ['/script/', '/javascript/', '/vbscript/'];
        $highPatterns = ['/onload/', '/onerror/', '/iframe/'];
        
        foreach ($criticalPatterns as $critical) {
            if (str_contains($pattern, $critical)) {
                return 'critical';
            }
        }
        
        foreach ($highPatterns as $high) {
            if (str_contains($pattern, $high)) {
                return 'high';
            }
        }
        
        return 'medium';
    }

    /**
     * Clean HTML attributes
     */
    private function cleanHtmlAttributes(string $html, array $allowedAttributes): string
    {
        // This is a simplified implementation
        return preg_replace('/\s*on\w+\s*=\s*["\'][^"\']*["\']?/i', '', $html);
    }

    /**
     * Sanitize filename
     */
    private function sanitizeFilename(string $filename): string
    {
        $filename = basename($filename);
        $filename = preg_replace('/[^\w\-_\.]/', '_', $filename);
        return substr($filename, 0, 255);
    }

    /**
     * Check if MIME type is allowed
     */
    private function isAllowedMimeType(string $mimeType): bool
    {
        $allowedTypes = [
            'image/jpeg', 'image/png', 'image/gif', 'image/webp',
            'text/plain', 'application/pdf', 'application/zip'
        ];
        
        return in_array($mimeType, $allowedTypes);
    }

    /**
     * Calculate risk level from threats
     */
    private function calculateRiskLevel(array $threats): string
    {
        if (empty($threats)) {
            return 'low';
        }
        
        foreach ($threats as $threat) {
            if ($threat['severity'] === 'critical') {
                return 'critical';
            }
        }
        
        foreach ($threats as $threat) {
            if ($threat['severity'] === 'high') {
                return 'high';
            }
        }
        
        return 'medium';
    }

    /**
     * Apply content filter
     */
    private function applyContentFilter(string $content, string $filterName, array $config): array
    {
        return match($filterName) {
            'profanity' => $this->filterProfanity($content, $config),
            'length' => $this->filterLength($content, $config),
            'encoding' => $this->filterEncoding($content, $config),
            default => ['content' => $content, 'applied' => false]
        };
    }

    /**
     * Filter profanity (basic implementation)
     */
    private function filterProfanity(string $content, array $config): array
    {
        $words = $config['words'] ?? [];
        $replacement = $config['replacement'] ?? '***';
        
        $filtered = $content;
        foreach ($words as $word) {
            $filtered = str_ireplace($word, $replacement, $filtered);
        }
        
        return ['content' => $filtered, 'applied' => $filtered !== $content];
    }

    /**
     * Filter content length
     */
    private function filterLength(string $content, array $config): array
    {
        $maxLength = $config['max_length'] ?? 1000;
        $truncated = strlen($content) > $maxLength;
        
        return [
            'content' => substr($content, 0, $maxLength),
            'applied' => $truncated
        ];
    }

    /**
     * Filter encoding
     */
    private function filterEncoding(string $content, array $config): array
    {
        $targetEncoding = $config['encoding'] ?? 'UTF-8';
        $converted = mb_convert_encoding($content, $targetEncoding);
        
        return [
            'content' => $converted,
            'applied' => $converted !== $content
        ];
    }

    /**
     * Sanitize array key
     */
    private function sanitizeKey(string $key): string
    {
        return preg_replace('/[^a-zA-Z0-9_\-]/', '_', $key);
    }
} 
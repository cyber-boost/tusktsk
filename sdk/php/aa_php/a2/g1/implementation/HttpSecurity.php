<?php

namespace TuskLang\Communication\Http;

/**
 * HTTP Security utilities
 */
class HttpSecurity
{
    /**
     * Validate URL for security issues
     */
    public static function validateUrl(string $url): bool
    {
        // Check for valid URL format
        if (!filter_var($url, FILTER_VALIDATE_URL)) {
            return false;
        }
        
        $parts = parse_url($url);
        
        // Prevent local file access
        if (isset($parts['scheme']) && in_array($parts['scheme'], ['file', 'ftp'])) {
            return false;
        }
        
        // Prevent localhost/private IP access in production
        if (isset($parts['host'])) {
            $ip = gethostbyname($parts['host']);
            if (filter_var($ip, FILTER_VALIDATE_IP, FILTER_FLAG_NO_PRIV_RANGE | FILTER_FLAG_NO_RES_RANGE) === false) {
                return false;
            }
        }
        
        return true;
    }

    /**
     * Sanitize input data
     */
    public static function sanitizeInput($data): mixed
    {
        if (is_string($data)) {
            return htmlspecialchars($data, ENT_QUOTES, 'UTF-8');
        }
        
        if (is_array($data)) {
            return array_map([self::class, 'sanitizeInput'], $data);
        }
        
        return $data;
    }

    /**
     * Generate CSRF token
     */
    public static function generateCsrfToken(): string
    {
        return bin2hex(random_bytes(32));
    }

    /**
     * Validate CSRF token
     */
    public static function validateCsrfToken(string $token, string $sessionToken): bool
    {
        return hash_equals($sessionToken, $token);
    }

    /**
     * Rate limiting implementation
     */
    public static function checkRateLimit(string $identifier, int $limit, int $window = 60): bool
    {
        $key = 'rate_limit_' . md5($identifier);
        $file = sys_get_temp_dir() . '/' . $key;
        
        $now = time();
        $requests = [];
        
        if (file_exists($file)) {
            $data = json_decode(file_get_contents($file), true);
            if ($data && is_array($data)) {
                $requests = array_filter($data, fn($time) => $now - $time < $window);
            }
        }
        
        if (count($requests) >= $limit) {
            return false;
        }
        
        $requests[] = $now;
        file_put_contents($file, json_encode($requests));
        
        return true;
    }

    /**
     * Generate secure headers
     */
    public static function getSecurityHeaders(array $config = []): array
    {
        return [
            'X-Content-Type-Options' => 'nosniff',
            'X-Frame-Options' => $config['frame_options'] ?? 'DENY',
            'X-XSS-Protection' => '1; mode=block',
            'Strict-Transport-Security' => 'max-age=31536000; includeSubDomains; preload',
            'Content-Security-Policy' => $config['csp'] ?? "default-src 'self'; script-src 'self' 'unsafe-inline'",
            'Referrer-Policy' => 'strict-origin-when-cross-origin',
            'Permissions-Policy' => 'geolocation=(), microphone=(), camera=()',
        ];
    }

    /**
     * Validate and sanitize headers
     */
    public static function sanitizeHeaders(array $headers): array
    {
        $sanitized = [];
        
        foreach ($headers as $key => $value) {
            // Remove dangerous headers
            $key = strtolower($key);
            if (in_array($key, ['host', 'content-length', 'transfer-encoding'])) {
                continue;
            }
            
            // Sanitize header values
            $value = preg_replace('/[\r\n]/', '', $value);
            $sanitized[$key] = $value;
        }
        
        return $sanitized;
    }

    /**
     * Detect and prevent SSRF attacks
     */
    public static function preventSSRF(string $url): bool
    {
        $parts = parse_url($url);
        
        if (!$parts || !isset($parts['host'])) {
            return false;
        }
        
        $host = $parts['host'];
        $ip = gethostbyname($host);
        
        // Block private/reserved IP ranges
        $privateRanges = [
            '10.0.0.0/8',
            '172.16.0.0/12',
            '192.168.0.0/16',
            '127.0.0.0/8',
            '169.254.0.0/16',
            '::1/128',
            'fc00::/7',
            'fe80::/10'
        ];
        
        foreach ($privateRanges as $range) {
            if (self::ipInRange($ip, $range)) {
                return false;
            }
        }
        
        return true;
    }

    private static function ipInRange(string $ip, string $range): bool
    {
        if (strpos($range, '/') === false) {
            return $ip === $range;
        }
        
        [$subnet, $bits] = explode('/', $range);
        
        if (filter_var($ip, FILTER_VALIDATE_IP, FILTER_FLAG_IPV4)) {
            $ip = ip2long($ip);
            $subnet = ip2long($subnet);
            $mask = -1 << (32 - $bits);
            return ($ip & $mask) === ($subnet & $mask);
        }
        
        // IPv6 handling would go here
        return false;
    }
} 
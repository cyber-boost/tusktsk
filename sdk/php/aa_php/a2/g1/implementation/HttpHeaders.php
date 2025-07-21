<?php

namespace TuskLang\Communication\Http;

/**
 * HTTP Headers management
 */
class HttpHeaders
{
    private array $defaults = [];

    public function setDefaults(array $headers): self
    {
        $this->defaults = $headers;
        return $this;
    }

    public function getDefaults(): array
    {
        return $this->defaults;
    }

    public function addDefault(string $key, string $value): self
    {
        $this->defaults[$key] = $value;
        return $this;
    }

    public function merge(array $headers): array
    {
        return array_merge($this->defaults, $headers);
    }

    /**
     * Parse content type and extract charset
     */
    public static function parseContentType(string $contentType): array
    {
        $parts = explode(';', $contentType);
        $type = trim($parts[0]);
        $params = [];
        
        for ($i = 1; $i < count($parts); $i++) {
            $param = explode('=', trim($parts[$i]), 2);
            if (count($param) === 2) {
                $params[trim($param[0])] = trim($param[1]);
            }
        }
        
        return ['type' => $type, 'params' => $params];
    }

    /**
     * Build authorization header
     */
    public static function buildAuthHeader(string $type, array $credentials): string
    {
        switch (strtolower($type)) {
            case 'basic':
                return 'Basic ' . base64_encode($credentials['username'] . ':' . $credentials['password']);
            
            case 'bearer':
                return 'Bearer ' . $credentials['token'];
            
            case 'digest':
                return 'Digest ' . http_build_query($credentials, '', ', ');
            
            default:
                throw new \InvalidArgumentException('Unsupported auth type: ' . $type);
        }
    }

    /**
     * Security headers for responses
     */
    public static function getSecurityHeaders(): array
    {
        return [
            'X-Content-Type-Options' => 'nosniff',
            'X-Frame-Options' => 'DENY',
            'X-XSS-Protection' => '1; mode=block',
            'Strict-Transport-Security' => 'max-age=31536000; includeSubDomains',
            'Content-Security-Policy' => "default-src 'self'",
            'Referrer-Policy' => 'strict-origin-when-cross-origin'
        ];
    }

    /**
     * CORS headers
     */
    public static function getCorsHeaders(array $config = []): array
    {
        return [
            'Access-Control-Allow-Origin' => $config['origin'] ?? '*',
            'Access-Control-Allow-Methods' => $config['methods'] ?? 'GET, POST, PUT, DELETE, OPTIONS',
            'Access-Control-Allow-Headers' => $config['headers'] ?? 'Content-Type, Authorization',
            'Access-Control-Max-Age' => $config['max_age'] ?? '3600'
        ];
    }
} 
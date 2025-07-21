<?php

namespace TuskLang\Communication\Http;

/**
 * HTTP Authentication management
 */
class HttpAuth
{
    private ?string $type = null;
    private array $credentials = [];

    public function configure(string $type, array $credentials): self
    {
        $this->type = strtolower($type);
        $this->credentials = $credentials;
        return $this;
    }

    public function getHeaders(HttpRequest $request): array
    {
        if ($this->type === null) {
            return [];
        }

        $headers = [];
        
        switch ($this->type) {
            case 'basic':
                $headers['Authorization'] = $this->buildBasicAuth();
                break;
                
            case 'bearer':
                $headers['Authorization'] = $this->buildBearerAuth();
                break;
                
            case 'digest':
                $headers['Authorization'] = $this->buildDigestAuth($request);
                break;
                
            case 'oauth':
                $headers['Authorization'] = $this->buildOAuthAuth($request);
                break;
                
            case 'api_key':
                $headers = $this->buildApiKeyAuth();
                break;
        }
        
        return $headers;
    }

    private function buildBasicAuth(): string
    {
        if (!isset($this->credentials['username']) || !isset($this->credentials['password'])) {
            throw new \InvalidArgumentException('Basic auth requires username and password');
        }
        
        $encoded = base64_encode($this->credentials['username'] . ':' . $this->credentials['password']);
        return 'Basic ' . $encoded;
    }

    private function buildBearerAuth(): string
    {
        if (!isset($this->credentials['token'])) {
            throw new \InvalidArgumentException('Bearer auth requires token');
        }
        
        return 'Bearer ' . $this->credentials['token'];
    }

    private function buildDigestAuth(HttpRequest $request): string
    {
        $required = ['username', 'password', 'realm', 'nonce', 'uri', 'algorithm'];
        foreach ($required as $field) {
            if (!isset($this->credentials[$field])) {
                throw new \InvalidArgumentException('Digest auth requires: ' . implode(', ', $required));
            }
        }
        
        $a1 = md5($this->credentials['username'] . ':' . $this->credentials['realm'] . ':' . $this->credentials['password']);
        $a2 = md5($request->getMethod() . ':' . $this->credentials['uri']);
        $response = md5($a1 . ':' . $this->credentials['nonce'] . ':' . $a2);
        
        $parts = [
            'username="' . $this->credentials['username'] . '"',
            'realm="' . $this->credentials['realm'] . '"',
            'nonce="' . $this->credentials['nonce'] . '"',
            'uri="' . $this->credentials['uri'] . '"',
            'algorithm=' . $this->credentials['algorithm'],
            'response="' . $response . '"'
        ];
        
        if (isset($this->credentials['opaque'])) {
            $parts[] = 'opaque="' . $this->credentials['opaque'] . '"';
        }
        
        return 'Digest ' . implode(', ', $parts);
    }

    private function buildOAuthAuth(HttpRequest $request): string
    {
        $required = ['consumer_key', 'consumer_secret', 'token', 'token_secret'];
        foreach ($required as $field) {
            if (!isset($this->credentials[$field])) {
                throw new \InvalidArgumentException('OAuth 1.0 requires: ' . implode(', ', $required));
            }
        }
        
        $timestamp = time();
        $nonce = bin2hex(random_bytes(16));
        
        $params = [
            'oauth_consumer_key' => $this->credentials['consumer_key'],
            'oauth_token' => $this->credentials['token'],
            'oauth_signature_method' => 'HMAC-SHA1',
            'oauth_timestamp' => $timestamp,
            'oauth_nonce' => $nonce,
            'oauth_version' => '1.0'
        ];
        
        // Create signature
        $baseString = $request->getMethod() . '&' . 
                     rawurlencode($request->getUrl()) . '&' . 
                     rawurlencode(http_build_query($params));
                     
        $signingKey = rawurlencode($this->credentials['consumer_secret']) . '&' . 
                     rawurlencode($this->credentials['token_secret']);
                     
        $signature = base64_encode(hash_hmac('sha1', $baseString, $signingKey, true));
        $params['oauth_signature'] = $signature;
        
        $headerParts = [];
        foreach ($params as $key => $value) {
            $headerParts[] = $key . '="' . rawurlencode($value) . '"';
        }
        
        return 'OAuth ' . implode(', ', $headerParts);
    }

    private function buildApiKeyAuth(): array
    {
        if (!isset($this->credentials['key']) || !isset($this->credentials['value'])) {
            throw new \InvalidArgumentException('API key auth requires key and value');
        }
        
        return [$this->credentials['key'] => $this->credentials['value']];
    }
} 
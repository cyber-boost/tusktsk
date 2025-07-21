<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * JWT Operator for creating, validating, and manipulating JSON Web Tokens
 */
class JwtOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'create';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'create':
                    return $this->createJwt($data, $options);
                case 'verify':
                    return $this->verifyJwt($data, $options);
                case 'decode':
                    return $this->decodeJwt($data, $options);
                case 'refresh':
                    return $this->refreshJwt($data, $options);
                case 'validate':
                    return $this->validateJwt($data, $options);
                case 'extract':
                    return $this->extractClaims($data, $options);
                default:
                    throw new \Exception("Unknown JWT action: $action");
            }
        } catch (\Exception $e) {
            error_log("JWT Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Create JWT token
     */
    private function createJwt(array $payload, array $options): string
    {
        $secret = $options['secret'] ?? '';
        $algorithm = $options['algorithm'] ?? 'HS256';
        $header = $options['header'] ?? [];
        $expiresIn = $options['expiresIn'] ?? 3600; // 1 hour default
        $notBefore = $options['notBefore'] ?? 0;
        $issuedAt = $options['issuedAt'] ?? time();

        if (empty($secret)) {
            throw new \Exception("JWT secret is required");
        }

        // Prepare header
        $jwtHeader = array_merge([
            'typ' => 'JWT',
            'alg' => $algorithm
        ], $header);

        // Prepare payload with standard claims
        $jwtPayload = array_merge([
            'iat' => $issuedAt,
            'nbf' => $notBefore,
            'exp' => $issuedAt + $expiresIn
        ], $payload);

        // Encode header and payload
        $headerEncoded = $this->base64UrlEncode(json_encode($jwtHeader));
        $payloadEncoded = $this->base64UrlEncode(json_encode($jwtPayload));

        // Create signature
        $signature = $this->createSignature($headerEncoded, $payloadEncoded, $secret, $algorithm);

        return $headerEncoded . '.' . $payloadEncoded . '.' . $signature;
    }

    /**
     * Verify JWT token
     */
    private function verifyJwt(string $token, array $options): array
    {
        $secret = $options['secret'] ?? '';
        $algorithms = $options['algorithms'] ?? ['HS256'];
        $verifyExpiration = $options['verifyExpiration'] ?? true;
        $verifyNotBefore = $options['verifyNotBefore'] ?? true;
        $verifyIssuedAt = $options['verifyIssuedAt'] ?? false;

        if (empty($secret)) {
            throw new \Exception("JWT secret is required for verification");
        }

        $parts = explode('.', $token);
        if (count($parts) !== 3) {
            throw new \Exception("Invalid JWT format");
        }

        list($headerEncoded, $payloadEncoded, $signature) = $parts;

        // Decode header and payload
        $header = json_decode($this->base64UrlDecode($headerEncoded), true);
        $payload = json_decode($this->base64UrlDecode($payloadEncoded), true);

        if (!$header || !$payload) {
            throw new \Exception("Invalid JWT encoding");
        }

        // Verify algorithm
        $algorithm = $header['alg'] ?? '';
        if (!in_array($algorithm, $algorithms)) {
            throw new \Exception("Algorithm not allowed: $algorithm");
        }

        // Verify signature
        $expectedSignature = $this->createSignature($headerEncoded, $payloadEncoded, $secret, $algorithm);
        if (!hash_equals($signature, $expectedSignature)) {
            throw new \Exception("Invalid JWT signature");
        }

        // Verify claims
        $errors = [];
        $currentTime = time();

        if ($verifyExpiration && isset($payload['exp'])) {
            if ($payload['exp'] < $currentTime) {
                $errors[] = "Token has expired";
            }
        }

        if ($verifyNotBefore && isset($payload['nbf'])) {
            if ($payload['nbf'] > $currentTime) {
                $errors[] = "Token not yet valid";
            }
        }

        if ($verifyIssuedAt && isset($payload['iat'])) {
            if ($payload['iat'] > $currentTime) {
                $errors[] = "Token issued in the future";
            }
        }

        return [
            'valid' => empty($errors),
            'errors' => $errors,
            'header' => $header,
            'payload' => $payload,
            'signature' => $signature
        ];
    }

    /**
     * Decode JWT token without verification
     */
    private function decodeJwt(string $token, array $options): array
    {
        $parts = explode('.', $token);
        if (count($parts) !== 3) {
            throw new \Exception("Invalid JWT format");
        }

        list($headerEncoded, $payloadEncoded, $signature) = $parts;

        $header = json_decode($this->base64UrlDecode($headerEncoded), true);
        $payload = json_decode($this->base64UrlDecode($payloadEncoded), true);

        if (!$header || !$payload) {
            throw new \Exception("Invalid JWT encoding");
        }

        return [
            'header' => $header,
            'payload' => $payload,
            'signature' => $signature,
            'verified' => false
        ];
    }

    /**
     * Refresh JWT token
     */
    private function refreshJwt(string $token, array $options): string
    {
        $secret = $options['secret'] ?? '';
        $newExpiresIn = $options['expiresIn'] ?? 3600;
        $allowExpired = $options['allowExpired'] ?? false;

        if (empty($secret)) {
            throw new \Exception("JWT secret is required");
        }

        // Decode the token
        $decoded = $this->decodeJwt($token, $options);
        $payload = $decoded['payload'];

        // Check if token is expired (unless allowed)
        if (!$allowExpired && isset($payload['exp']) && $payload['exp'] < time()) {
            throw new \Exception("Cannot refresh expired token");
        }

        // Update timestamps
        $currentTime = time();
        $payload['iat'] = $currentTime;
        $payload['exp'] = $currentTime + $newExpiresIn;

        // Remove old signature from payload if present
        unset($payload['sig']);

        // Create new token
        return $this->createJwt($payload, array_merge($options, [
            'issuedAt' => $currentTime,
            'expiresIn' => $newExpiresIn
        ]));
    }

    /**
     * Validate JWT token structure and claims
     */
    private function validateJwt(string $token, array $options): array
    {
        $requiredClaims = $options['requiredClaims'] ?? [];
        $allowedIssuers = $options['allowedIssuers'] ?? [];
        $allowedAudiences = $options['allowedAudiences'] ?? [];
        $maxAge = $options['maxAge'] ?? null;

        try {
            $decoded = $this->decodeJwt($token, $options);
            $payload = $decoded['payload'];
            $header = $decoded['header'];

            $errors = [];
            $warnings = [];

            // Check required claims
            foreach ($requiredClaims as $claim) {
                if (!isset($payload[$claim])) {
                    $errors[] = "Required claim '$claim' is missing";
                }
            }

            // Check issuer
            if (!empty($allowedIssuers) && isset($payload['iss'])) {
                if (!in_array($payload['iss'], $allowedIssuers)) {
                    $errors[] = "Issuer not allowed: " . $payload['iss'];
                }
            }

            // Check audience
            if (!empty($allowedAudiences) && isset($payload['aud'])) {
                $audience = is_array($payload['aud']) ? $payload['aud'] : [$payload['aud']];
                $intersection = array_intersect($audience, $allowedAudiences);
                if (empty($intersection)) {
                    $errors[] = "Audience not allowed: " . implode(', ', $audience);
                }
            }

            // Check max age
            if ($maxAge !== null && isset($payload['iat'])) {
                $age = time() - $payload['iat'];
                if ($age > $maxAge) {
                    $warnings[] = "Token is older than maximum age ($maxAge seconds)";
                }
            }

            // Check for suspicious claims
            if (isset($payload['exp']) && $payload['exp'] > time() + 86400 * 365) {
                $warnings[] = "Token expires more than 1 year in the future";
            }

            return [
                'valid' => empty($errors),
                'errors' => $errors,
                'warnings' => $warnings,
                'header' => $header,
                'payload' => $payload,
                'claims' => $this->extractClaims($token, $options)
            ];
        } catch (\Exception $e) {
            return [
                'valid' => false,
                'errors' => [$e->getMessage()],
                'warnings' => [],
                'header' => null,
                'payload' => null,
                'claims' => []
            ];
        }
    }

    /**
     * Extract claims from JWT token
     */
    private function extractClaims(string $token, array $options): array
    {
        $decoded = $this->decodeJwt($token, $options);
        $payload = $decoded['payload'];

        $claims = [];
        $standardClaims = ['iss', 'sub', 'aud', 'exp', 'nbf', 'iat', 'jti'];

        foreach ($payload as $claim => $value) {
            $claims[$claim] = [
                'value' => $value,
                'type' => in_array($claim, $standardClaims) ? 'standard' : 'custom',
                'description' => $this->getClaimDescription($claim)
            ];
        }

        return $claims;
    }

    /**
     * Create JWT signature
     */
    private function createSignature(string $header, string $payload, string $secret, string $algorithm): string
    {
        $data = $header . '.' . $payload;

        switch ($algorithm) {
            case 'HS256':
                return $this->base64UrlEncode(hash_hmac('sha256', $data, $secret, true));
            case 'HS384':
                return $this->base64UrlEncode(hash_hmac('sha384', $data, $secret, true));
            case 'HS512':
                return $this->base64UrlEncode(hash_hmac('sha512', $data, $secret, true));
            case 'RS256':
                return $this->base64UrlEncode($this->rsaSign($data, $secret, 'sha256'));
            case 'RS384':
                return $this->base64UrlEncode($this->rsaSign($data, $secret, 'sha384'));
            case 'RS512':
                return $this->base64UrlEncode($this->rsaSign($data, $secret, 'sha512'));
            default:
                throw new \Exception("Unsupported algorithm: $algorithm");
        }
    }

    /**
     * RSA signature (placeholder for RSA algorithms)
     */
    private function rsaSign(string $data, string $privateKey, string $algorithm): string
    {
        // This would implement RSA signing if needed
        throw new \Exception("RSA signing not implemented");
    }

    /**
     * Base64 URL encode
     */
    private function base64UrlEncode(string $data): string
    {
        return rtrim(strtr(base64_encode($data), '+/', '-_'), '=');
    }

    /**
     * Base64 URL decode
     */
    private function base64UrlDecode(string $data): string
    {
        $data = strtr($data, '-_', '+/');
        $remainder = strlen($data) % 4;
        if ($remainder) {
            $data .= str_repeat('=', 4 - $remainder);
        }
        return base64_decode($data);
    }

    /**
     * Get claim description
     */
    private function getClaimDescription(string $claim): string
    {
        $descriptions = [
            'iss' => 'Issuer - identifies the principal that issued the JWT',
            'sub' => 'Subject - identifies the principal that is the subject of the JWT',
            'aud' => 'Audience - identifies the recipients that the JWT is intended for',
            'exp' => 'Expiration Time - identifies the expiration time on or after which the JWT must not be accepted for processing',
            'nbf' => 'Not Before - identifies the time before which the JWT must not be accepted for processing',
            'iat' => 'Issued At - identifies the time at which the JWT was issued',
            'jti' => 'JWT ID - provides a unique identifier for the JWT'
        ];

        return $descriptions[$claim] ?? 'Custom claim';
    }

    /**
     * Generate secure random secret
     */
    public function generateSecret(int $length = 32): string
    {
        return bin2hex(random_bytes($length));
    }

    /**
     * Get supported algorithms
     */
    public function getSupportedAlgorithms(): array
    {
        return [
            'HS256' => 'HMAC SHA-256',
            'HS384' => 'HMAC SHA-384',
            'HS512' => 'HMAC SHA-512',
            'RS256' => 'RSA SHA-256',
            'RS384' => 'RSA SHA-384',
            'RS512' => 'RSA SHA-512'
        ];
    }

    /**
     * Parse JWT token into components
     */
    public function parseToken(string $token): array
    {
        $parts = explode('.', $token);
        if (count($parts) !== 3) {
            throw new \Exception("Invalid JWT format");
        }

        return [
            'header' => $this->base64UrlDecode($parts[0]),
            'payload' => $this->base64UrlDecode($parts[1]),
            'signature' => $parts[2],
            'raw' => [
                'header' => $parts[0],
                'payload' => $parts[1],
                'signature' => $parts[2]
            ]
        ];
    }
} 
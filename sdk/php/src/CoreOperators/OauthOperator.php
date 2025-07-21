<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * OAuth Operator for OAuth 2.0 authentication flows
 */
class OauthOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'authorize';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'authorize':
                    return $this->authorize($data, $options);
                case 'token':
                    return $this->getToken($data, $options);
                case 'refresh':
                    return $this->refreshToken($data, $options);
                case 'revoke':
                    return $this->revokeToken($data, $options);
                case 'validate':
                    return $this->validateToken($data, $options);
                case 'userinfo':
                    return $this->getUserInfo($data, $options);
                case 'introspect':
                    return $this->introspectToken($data, $options);
                default:
                    throw new \Exception("Unknown OAuth action: $action");
            }
        } catch (\Exception $e) {
            error_log("OAuth Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Generate authorization URL
     */
    private function authorize(array $authData, array $options): array
    {
        $clientId = $options['clientId'] ?? '';
        $redirectUri = $options['redirectUri'] ?? '';
        $scope = $options['scope'] ?? '';
        $state = $options['state'] ?? $this->generateState();
        $responseType = $options['responseType'] ?? 'code';
        $authorizationUrl = $options['authorizationUrl'] ?? '';

        if (empty($clientId) || empty($redirectUri) || empty($authorizationUrl)) {
            throw new \Exception("Client ID, redirect URI, and authorization URL are required");
        }

        $params = [
            'client_id' => $clientId,
            'redirect_uri' => $redirectUri,
            'response_type' => $responseType,
            'state' => $state
        ];

        if (!empty($scope)) {
            $params['scope'] = $scope;
        }

        // Add additional parameters
        foreach ($authData as $key => $value) {
            if (!in_array($key, ['client_id', 'redirect_uri', 'response_type', 'state', 'scope'])) {
                $params[$key] = $value;
            }
        }

        $url = $authorizationUrl . '?' . http_build_query($params);

        return [
            'authorizationUrl' => $url,
            'state' => $state,
            'params' => $params
        ];
    }

    /**
     * Exchange authorization code for token
     */
    private function getToken(array $tokenData, array $options): array
    {
        $code = $tokenData['code'] ?? '';
        $clientId = $options['clientId'] ?? '';
        $clientSecret = $options['clientSecret'] ?? '';
        $redirectUri = $options['redirectUri'] ?? '';
        $tokenUrl = $options['tokenUrl'] ?? '';
        $grantType = $options['grantType'] ?? 'authorization_code';

        if (empty($code) || empty($clientId) || empty($clientSecret) || empty($tokenUrl)) {
            throw new \Exception("Code, client ID, client secret, and token URL are required");
        }

        $postData = [
            'grant_type' => $grantType,
            'client_id' => $clientId,
            'client_secret' => $clientSecret,
            'code' => $code,
            'redirect_uri' => $redirectUri
        ];

        // Add additional parameters
        foreach ($tokenData as $key => $value) {
            if (!in_array($key, ['code', 'grant_type', 'client_id', 'client_secret', 'redirect_uri'])) {
                $postData[$key] = $value;
            }
        }

        $response = $this->makeTokenRequest($tokenUrl, $postData);

        return [
            'success' => isset($response['access_token']),
            'token' => $response,
            'error' => isset($response['error']) ? $response['error'] : null
        ];
    }

    /**
     * Refresh access token
     */
    private function refreshToken(array $refreshData, array $options): array
    {
        $refreshToken = $refreshData['refreshToken'] ?? '';
        $clientId = $options['clientId'] ?? '';
        $clientSecret = $options['clientSecret'] ?? '';
        $tokenUrl = $options['tokenUrl'] ?? '';

        if (empty($refreshToken) || empty($clientId) || empty($clientSecret) || empty($tokenUrl)) {
            throw new \Exception("Refresh token, client ID, client secret, and token URL are required");
        }

        $postData = [
            'grant_type' => 'refresh_token',
            'client_id' => $clientId,
            'client_secret' => $clientSecret,
            'refresh_token' => $refreshToken
        ];

        $response = $this->makeTokenRequest($tokenUrl, $postData);

        return [
            'success' => isset($response['access_token']),
            'token' => $response,
            'error' => isset($response['error']) ? $response['error'] : null
        ];
    }

    /**
     * Revoke token
     */
    private function revokeToken(array $revokeData, array $options): array
    {
        $token = $revokeData['token'] ?? '';
        $clientId = $options['clientId'] ?? '';
        $clientSecret = $options['clientSecret'] ?? '';
        $revokeUrl = $options['revokeUrl'] ?? '';
        $tokenType = $options['tokenType'] ?? 'access_token';

        if (empty($token) || empty($revokeUrl)) {
            throw new \Exception("Token and revoke URL are required");
        }

        $postData = [
            'token' => $token,
            'token_type_hint' => $tokenType
        ];

        if (!empty($clientId)) {
            $postData['client_id'] = $clientId;
        }

        if (!empty($clientSecret)) {
            $postData['client_secret'] = $clientSecret;
        }

        $response = $this->makeRevokeRequest($revokeUrl, $postData);

        return [
            'success' => $response['success'],
            'error' => $response['error'] ?? null
        ];
    }

    /**
     * Validate token
     */
    private function validateToken(string $token, array $options): array
    {
        $clientId = $options['clientId'] ?? '';
        $introspectUrl = $options['introspectUrl'] ?? '';

        if (empty($token)) {
            throw new \Exception("Token is required");
        }

        if (!empty($introspectUrl)) {
            // Use token introspection if available
            return $this->introspectToken($token, $options);
        }

        // Basic validation - check if token is not expired
        $tokenData = $this->parseToken($token);
        
        if (!$tokenData) {
            return [
                'valid' => false,
                'error' => 'Invalid token format'
            ];
        }

        $currentTime = time();
        $expiresAt = $tokenData['exp'] ?? null;

        if ($expiresAt && $expiresAt < $currentTime) {
            return [
                'valid' => false,
                'error' => 'Token has expired'
            ];
        }

        return [
            'valid' => true,
            'tokenData' => $tokenData
        ];
    }

    /**
     * Get user info
     */
    private function getUserInfo(string $token, array $options): array
    {
        $userInfoUrl = $options['userInfoUrl'] ?? '';

        if (empty($token) || empty($userInfoUrl)) {
            throw new \Exception("Token and user info URL are required");
        }

        $headers = [
            'Authorization: Bearer ' . $token,
            'Content-Type: application/json'
        ];

        $response = $this->makeApiRequest($userInfoUrl, 'GET', [], $headers);

        return [
            'success' => $response['success'],
            'userInfo' => $response['data'],
            'error' => $response['error'] ?? null
        ];
    }

    /**
     * Introspect token
     */
    private function introspectToken(string $token, array $options): array
    {
        $clientId = $options['clientId'] ?? '';
        $clientSecret = $options['clientSecret'] ?? '';
        $introspectUrl = $options['introspectUrl'] ?? '';

        if (empty($token) || empty($introspectUrl)) {
            throw new \Exception("Token and introspect URL are required");
        }

        $postData = [
            'token' => $token
        ];

        if (!empty($clientId)) {
            $postData['client_id'] = $clientId;
        }

        if (!empty($clientSecret)) {
            $postData['client_secret'] = $clientSecret;
        }

        $response = $this->makeIntrospectRequest($introspectUrl, $postData);

        return [
            'success' => $response['success'],
            'introspection' => $response['data'],
            'error' => $response['error'] ?? null
        ];
    }

    /**
     * Generate random state parameter
     */
    private function generateState(): string
    {
        return bin2hex(random_bytes(16));
    }

    /**
     * Make token request
     */
    private function makeTokenRequest(string $url, array $postData): array
    {
        $headers = [
            'Content-Type: application/x-www-form-urlencoded'
        ];

        $response = $this->makeApiRequest($url, 'POST', $postData, $headers);

        if ($response['success']) {
            return json_decode($response['data'], true) ?: [];
        }

        return [
            'error' => $response['error'] ?? 'Token request failed'
        ];
    }

    /**
     * Make revoke request
     */
    private function makeRevokeRequest(string $url, array $postData): array
    {
        $headers = [
            'Content-Type: application/x-www-form-urlencoded'
        ];

        $response = $this->makeApiRequest($url, 'POST', $postData, $headers);

        return [
            'success' => $response['success'],
            'error' => $response['error'] ?? null
        ];
    }

    /**
     * Make introspect request
     */
    private function makeIntrospectRequest(string $url, array $postData): array
    {
        $headers = [
            'Content-Type: application/x-www-form-urlencoded'
        ];

        $response = $this->makeApiRequest($url, 'POST', $postData, $headers);

        return [
            'success' => $response['success'],
            'data' => $response['success'] ? (json_decode($response['data'], true) ?: []) : null,
            'error' => $response['error'] ?? null
        ];
    }

    /**
     * Make API request
     */
    private function makeApiRequest(string $url, string $method, array $data, array $headers): array
    {
        $ch = curl_init();

        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_FOLLOWLOCATION, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, 30);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);

        if ($method === 'POST') {
            curl_setopt($ch, CURLOPT_POST, true);
            if (!empty($data)) {
                curl_setopt($ch, CURLOPT_POSTFIELDS, http_build_query($data));
            }
        }

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $error = curl_error($ch);
        curl_close($ch);

        if ($error) {
            return [
                'success' => false,
                'error' => $error
            ];
        }

        if ($httpCode >= 200 && $httpCode < 300) {
            return [
                'success' => true,
                'data' => $response,
                'httpCode' => $httpCode
            ];
        }

        return [
            'success' => false,
            'error' => "HTTP $httpCode: $response",
            'httpCode' => $httpCode
        ];
    }

    /**
     * Parse JWT token (basic implementation)
     */
    private function parseToken(string $token): ?array
    {
        $parts = explode('.', $token);
        if (count($parts) !== 3) {
            return null;
        }

        $payload = $parts[1];
        $payload = strtr($payload, '-_', '+/');
        $remainder = strlen($payload) % 4;
        if ($remainder) {
            $payload .= str_repeat('=', 4 - $remainder);
        }

        $decoded = base64_decode($payload);
        if ($decoded === false) {
            return null;
        }

        return json_decode($decoded, true) ?: null;
    }

    /**
     * Get OAuth provider configuration
     */
    public function getProviderConfig(string $provider): array
    {
        $providers = [
            'google' => [
                'authorizationUrl' => 'https://accounts.google.com/o/oauth2/auth',
                'tokenUrl' => 'https://oauth2.googleapis.com/token',
                'userInfoUrl' => 'https://www.googleapis.com/oauth2/v2/userinfo',
                'revokeUrl' => 'https://oauth2.googleapis.com/revoke'
            ],
            'facebook' => [
                'authorizationUrl' => 'https://www.facebook.com/v12.0/dialog/oauth',
                'tokenUrl' => 'https://graph.facebook.com/v12.0/oauth/access_token',
                'userInfoUrl' => 'https://graph.facebook.com/me',
                'revokeUrl' => 'https://graph.facebook.com/v12.0/me/permissions'
            ],
            'github' => [
                'authorizationUrl' => 'https://github.com/login/oauth/authorize',
                'tokenUrl' => 'https://github.com/login/oauth/access_token',
                'userInfoUrl' => 'https://api.github.com/user',
                'revokeUrl' => null
            ],
            'linkedin' => [
                'authorizationUrl' => 'https://www.linkedin.com/oauth/v2/authorization',
                'tokenUrl' => 'https://www.linkedin.com/oauth/v2/accessToken',
                'userInfoUrl' => 'https://api.linkedin.com/v2/me',
                'revokeUrl' => null
            ]
        ];

        return $providers[$provider] ?? [];
    }

    /**
     * Generate PKCE code verifier and challenge
     */
    public function generatePkce(): array
    {
        $codeVerifier = bin2hex(random_bytes(32));
        $codeChallenge = rtrim(strtr(base64_encode(hash('sha256', $codeVerifier, true)), '+/', '-_'), '=');

        return [
            'codeVerifier' => $codeVerifier,
            'codeChallenge' => $codeChallenge
        ];
    }

    /**
     * Validate OAuth callback
     */
    public function validateCallback(array $callbackData, string $expectedState): array
    {
        $state = $callbackData['state'] ?? '';
        $code = $callbackData['code'] ?? '';
        $error = $callbackData['error'] ?? '';

        if (!empty($error)) {
            return [
                'valid' => false,
                'error' => $error
            ];
        }

        if (empty($code)) {
            return [
                'valid' => false,
                'error' => 'Authorization code is missing'
            ];
        }

        if ($state !== $expectedState) {
            return [
                'valid' => false,
                'error' => 'State parameter mismatch'
            ];
        }

        return [
            'valid' => true,
            'code' => $code,
            'state' => $state
        ];
    }
} 
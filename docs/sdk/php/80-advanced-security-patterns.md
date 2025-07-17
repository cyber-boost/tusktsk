# Advanced Security Patterns

TuskLang enables PHP developers to implement sophisticated security patterns with confidence. This guide covers advanced security strategies, authentication, authorization, and security best practices.

## Table of Contents
- [Security Configuration](#security-configuration)
- [Authentication Systems](#authentication-systems)
- [Authorization Patterns](#authorization-patterns)
- [Data Protection](#data-protection)
- [Security Monitoring](#security-monitoring)
- [Best Practices](#best-practices)

## Security Configuration

```php
// config/security.tsk
security = {
    authentication = {
        provider = "jwt"
        algorithm = "RS256"
        secret_key = "@env.secure('JWT_SECRET_KEY')"
        public_key = "@env.secure('JWT_PUBLIC_KEY')"
        private_key = "@env.secure('JWT_PRIVATE_KEY')"
        
        jwt = {
            issuer = "@env('APP_NAME')"
            audience = "@env('APP_AUDIENCE')"
            expiration = "1h"
            refresh_expiration = "7d"
            clock_skew = 30
        }
        
        oauth = {
            providers = {
                google = {
                    client_id = "@env.secure('GOOGLE_CLIENT_ID')"
                    client_secret = "@env.secure('GOOGLE_CLIENT_SECRET')"
                    redirect_uri = "@env('GOOGLE_REDIRECT_URI')"
                }
                github = {
                    client_id = "@env.secure('GITHUB_CLIENT_ID')"
                    client_secret = "@env.secure('GITHUB_CLIENT_SECRET')"
                    redirect_uri = "@env('GITHUB_REDIRECT_URI')"
                }
            }
        }
        
        mfa = {
            enabled = true
            methods = ["totp", "sms", "email"]
            backup_codes = true
            remember_device = true
        }
    }
    
    authorization = {
        provider = "rbac"
        cache_enabled = true
        cache_ttl = 300
        
        roles = {
            admin = {
                permissions = ["*"]
                inherits = []
            }
            user = {
                permissions = ["read:own", "write:own", "delete:own"]
                inherits = ["guest"]
            }
            guest = {
                permissions = ["read:public"]
                inherits = []
            }
        }
        
        policies = {
            resource_based = true
            attribute_based = true
            time_based = true
            location_based = true
        }
    }
    
    encryption = {
        algorithm = "AES-256-GCM"
        key_rotation = true
        key_rotation_interval = "30d"
        
        keys = {
            current = "@env.secure('ENCRYPTION_KEY_CURRENT')"
            previous = "@env.secure('ENCRYPTION_KEY_PREVIOUS')"
            next = "@env.secure('ENCRYPTION_KEY_NEXT')"
        }
        
        at_rest = {
            enabled = true
            database_fields = ["password", "ssn", "credit_card"]
            file_encryption = true
        }
        
        in_transit = {
            enabled = true
            tls_version = "1.3"
            cipher_suites = ["TLS_AES_256_GCM_SHA384", "TLS_CHACHA20_POLY1305_SHA256"]
        }
    }
    
    input_validation = {
        enabled = true
        sanitization = true
        whitelist_approach = true
        
        rules = {
            email = {
                pattern = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
                max_length = 254
            }
            password = {
                min_length = 12
                require_uppercase = true
                require_lowercase = true
                require_numbers = true
                require_special = true
                prevent_common = true
            }
            username = {
                pattern = "^[a-zA-Z0-9_-]{3,20}$"
                reserved_names = ["admin", "root", "system"]
            }
        }
    }
    
    rate_limiting = {
        enabled = true
        provider = "redis"
        
        limits = {
            authentication = {
                attempts = 5
                window = "15m"
                block_duration = "1h"
            }
            api_requests = {
                requests = 1000
                window = "1h"
                burst = 50
            }
            file_uploads = {
                requests = 10
                window = "1h"
                max_size = "10MB"
            }
        }
    }
    
    audit_logging = {
        enabled = true
        events = [
            "authentication.success",
            "authentication.failure",
            "authorization.grant",
            "authorization.deny",
            "data.access",
            "data.modification",
            "configuration.change"
        ]
        
        storage = {
            type = "database"
            retention = "7y"
            encryption = true
        }
    }
    
    threat_protection = {
        sql_injection = {
            enabled = true
            prepared_statements = true
            input_validation = true
            waf_rules = true
        }
        
        xss = {
            enabled = true
            output_encoding = true
            content_security_policy = true
            xss_protection_header = true
        }
        
        csrf = {
            enabled = true
            token_validation = true
            same_site_cookies = true
        }
        
        file_upload = {
            enabled = true
            allowed_types = ["jpg", "png", "pdf", "doc"]
            max_size = "5MB"
            virus_scanning = true
        }
    }
}
```

## Authentication Systems

```php
<?php
// app/Infrastructure/Security/Authentication/JwtAuthenticator.php

namespace App\Infrastructure\Security\Authentication;

use TuskLang\Security\AuthenticatorInterface;
use TuskLang\Security\TokenInterface;
use TuskLang\Security\UserInterface;
use Firebase\JWT\JWT;
use Firebase\JWT\Key;

class JwtAuthenticator implements AuthenticatorInterface
{
    private array $config;
    private array $keys;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->keys = [
            'current' => $config['authentication']['private_key'],
            'public' => $config['authentication']['public_key']
        ];
    }
    
    public function authenticate(array $credentials): ?UserInterface
    {
        if (!isset($credentials['token'])) {
            return null;
        }
        
        try {
            $payload = JWT::decode(
                $credentials['token'],
                new Key($this->keys['public'], $this->config['authentication']['algorithm'])
            );
            
            return $this->createUserFromPayload($payload);
        } catch (\Exception $e) {
            return null;
        }
    }
    
    public function createToken(UserInterface $user): TokenInterface
    {
        $payload = [
            'iss' => $this->config['authentication']['jwt']['issuer'],
            'aud' => $this->config['authentication']['jwt']['audience'],
            'iat' => time(),
            'exp' => time() + $this->parseDuration($this->config['authentication']['jwt']['expiration']),
            'sub' => $user->getId(),
            'email' => $user->getEmail(),
            'roles' => $user->getRoles()
        ];
        
        $token = JWT::encode($payload, $this->keys['current'], $this->config['authentication']['algorithm']);
        
        return new JwtToken($token, $payload);
    }
    
    public function refreshToken(TokenInterface $token): TokenInterface
    {
        $payload = $token->getPayload();
        
        // Remove old timestamps
        unset($payload['iat'], $payload['exp']);
        
        // Add new timestamps
        $payload['iat'] = time();
        $payload['exp'] = time() + $this->parseDuration($this->config['authentication']['jwt']['refresh_expiration']);
        
        $newToken = JWT::encode($payload, $this->keys['current'], $this->config['authentication']['algorithm']);
        
        return new JwtToken($newToken, $payload);
    }
    
    public function validateToken(TokenInterface $token): bool
    {
        try {
            $payload = JWT::decode(
                $token->getToken(),
                new Key($this->keys['public'], $this->config['authentication']['algorithm'])
            );
            
            return $payload->exp > time();
        } catch (\Exception $e) {
            return false;
        }
    }
    
    private function createUserFromPayload(object $payload): UserInterface
    {
        return new AuthenticatedUser(
            $payload->sub,
            $payload->email,
            $payload->roles ?? []
        );
    }
    
    private function parseDuration(string $duration): int
    {
        $value = (int) $duration;
        $unit = substr($duration, -1);
        
        return match($unit) {
            's' => $value,
            'm' => $value * 60,
            'h' => $value * 3600,
            'd' => $value * 86400,
            default => $value
        };
    }
}

// app/Infrastructure/Security/Authentication/MfaManager.php

namespace App\Infrastructure\Security\Authentication;

use TuskLang\Security\MfaManagerInterface;
use RobThree\Auth\TwoFactorAuth;

class MfaManager implements MfaManagerInterface
{
    private TwoFactorAuth $tfa;
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->tfa = new TwoFactorAuth($config['authentication']['jwt']['issuer']);
    }
    
    public function generateSecret(): string
    {
        return $this->tfa->createSecret();
    }
    
    public function generateQrCode(string $secret, string $email): string
    {
        return $this->tfa->getQRCodeImageAsDataUri($email, $secret);
    }
    
    public function verifyCode(string $secret, string $code): bool
    {
        return $this->tfa->verifyCode($secret, $code);
    }
    
    public function generateBackupCodes(): array
    {
        $codes = [];
        for ($i = 0; $i < 10; $i++) {
            $codes[] = $this->generateBackupCode();
        }
        return $codes;
    }
    
    public function verifyBackupCode(array $storedCodes, string $code): bool
    {
        $index = array_search($code, $storedCodes);
        if ($index !== false) {
            unset($storedCodes[$index]);
            return true;
        }
        return false;
    }
    
    private function generateBackupCode(): string
    {
        return strtoupper(substr(md5(uniqid()), 0, 8));
    }
}
```

## Authorization Patterns

```php
<?php
// app/Infrastructure/Security/Authorization/RbacAuthorizer.php

namespace App\Infrastructure\Security\Authorization;

use TuskLang\Security\AuthorizerInterface;
use TuskLang\Security\UserInterface;
use TuskLang\Security\PermissionInterface;

class RbacAuthorizer implements AuthorizerInterface
{
    private array $roles = [];
    private array $permissions = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->loadRoles();
    }
    
    public function hasPermission(UserInterface $user, string $permission): bool
    {
        $userRoles = $user->getRoles();
        
        foreach ($userRoles as $role) {
            if ($this->roleHasPermission($role, $permission)) {
                return true;
            }
        }
        
        return false;
    }
    
    public function hasRole(UserInterface $user, string $role): bool
    {
        return in_array($role, $user->getRoles());
    }
    
    public function canAccessResource(UserInterface $user, string $resource, string $action): bool
    {
        $permission = "{$action}:{$resource}";
        return $this->hasPermission($user, $permission);
    }
    
    public function canAccessOwnResource(UserInterface $user, string $resource, string $action, string $resourceId): bool
    {
        // Check if user owns the resource
        if ($this->isResourceOwner($user, $resource, $resourceId)) {
            return $this->hasPermission($user, "{$action}:own");
        }
        
        return $this->hasPermission($user, "{$action}:{$resource}");
    }
    
    public function filterResources(UserInterface $user, array $resources, string $action): array
    {
        return array_filter($resources, function($resource) use ($user, $action) {
            return $this->canAccessResource($user, $resource['type'], $action);
        });
    }
    
    private function roleHasPermission(string $role, string $permission): bool
    {
        if (!isset($this->roles[$role])) {
            return false;
        }
        
        $rolePermissions = $this->roles[$role]['permissions'];
        
        // Check for wildcard permission
        if (in_array('*', $rolePermissions)) {
            return true;
        }
        
        // Check exact permission
        if (in_array($permission, $rolePermissions)) {
            return true;
        }
        
        // Check inherited roles
        $inheritedRoles = $this->roles[$role]['inherits'] ?? [];
        foreach ($inheritedRoles as $inheritedRole) {
            if ($this->roleHasPermission($inheritedRole, $permission)) {
                return true;
            }
        }
        
        return false;
    }
    
    private function isResourceOwner(UserInterface $user, string $resource, string $resourceId): bool
    {
        // Implementation depends on your resource ownership model
        // This is a simplified example
        $resourceData = $this->getResourceData($resource, $resourceId);
        return $resourceData['owner_id'] === $user->getId();
    }
    
    private function getResourceData(string $resource, string $resourceId): array
    {
        // Implementation to fetch resource data
        return ['owner_id' => 'user123'];
    }
    
    private function loadRoles(): void
    {
        $this->roles = $this->config['authorization']['roles'];
    }
}

// app/Infrastructure/Security/Authorization/AttributeBasedAuthorizer.php

namespace App\Infrastructure\Security\Authorization;

use TuskLang\Security\AuthorizerInterface;
use TuskLang\Security\UserInterface;

class AttributeBasedAuthorizer implements AuthorizerInterface
{
    private array $policies = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->loadPolicies();
    }
    
    public function hasPermission(UserInterface $user, string $permission): bool
    {
        return $this->evaluatePolicy($user, $permission);
    }
    
    public function canAccessResource(UserInterface $user, string $resource, string $action): bool
    {
        $context = [
            'user' => $user,
            'resource' => $resource,
            'action' => $action,
            'time' => time(),
            'location' => $this->getUserLocation($user)
        ];
        
        return $this->evaluatePolicyWithContext($user, "{$action}:{$resource}", $context);
    }
    
    public function canAccessWithConditions(UserInterface $user, string $permission, array $conditions): bool
    {
        $context = array_merge($conditions, [
            'user' => $user,
            'time' => time()
        ]);
        
        return $this->evaluatePolicyWithContext($user, $permission, $context);
    }
    
    private function evaluatePolicy(UserInterface $user, string $permission): bool
    {
        foreach ($this->policies as $policy) {
            if ($this->matchesPolicy($policy, $user, $permission)) {
                return $policy['effect'] === 'allow';
            }
        }
        
        return false;
    }
    
    private function evaluatePolicyWithContext(UserInterface $user, string $permission, array $context): bool
    {
        foreach ($this->policies as $policy) {
            if ($this->matchesPolicyWithContext($policy, $user, $permission, $context)) {
                return $policy['effect'] === 'allow';
            }
        }
        
        return false;
    }
    
    private function matchesPolicy(array $policy, UserInterface $user, string $permission): bool
    {
        // Check permission match
        if (!$this->matchesPattern($permission, $policy['permission'])) {
            return false;
        }
        
        // Check user attributes
        if (isset($policy['user_attributes'])) {
            foreach ($policy['user_attributes'] as $attribute => $value) {
                if (!$this->matchesUserAttribute($user, $attribute, $value)) {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    private function matchesPolicyWithContext(array $policy, UserInterface $user, string $permission, array $context): bool
    {
        if (!$this->matchesPolicy($policy, $user, $permission)) {
            return false;
        }
        
        // Check context conditions
        if (isset($policy['conditions'])) {
            foreach ($policy['conditions'] as $condition) {
                if (!$this->evaluateCondition($condition, $context)) {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    private function matchesPattern(string $value, string $pattern): bool
    {
        return fnmatch($pattern, $value);
    }
    
    private function matchesUserAttribute(UserInterface $user, string $attribute, mixed $value): bool
    {
        $userValue = $this->getUserAttribute($user, $attribute);
        return $userValue === $value;
    }
    
    private function evaluateCondition(array $condition, array $context): bool
    {
        $operator = $condition['operator'];
        $left = $this->resolveValue($condition['left'], $context);
        $right = $this->resolveValue($condition['right'], $context);
        
        return match($operator) {
            'eq' => $left === $right,
            'ne' => $left !== $right,
            'gt' => $left > $right,
            'gte' => $left >= $right,
            'lt' => $left < $right,
            'lte' => $left <= $right,
            'in' => in_array($left, $right),
            'not_in' => !in_array($left, $right),
            default => false
        };
    }
    
    private function resolveValue(mixed $value, array $context): mixed
    {
        if (is_string($value) && str_starts_with($value, '$')) {
            $path = substr($value, 1);
            return $this->getNestedValue($context, $path);
        }
        
        return $value;
    }
    
    private function getNestedValue(array $array, string $path): mixed
    {
        $keys = explode('.', $path);
        $current = $array;
        
        foreach ($keys as $key) {
            if (!isset($current[$key])) {
                return null;
            }
            $current = $current[$key];
        }
        
        return $current;
    }
    
    private function getUserAttribute(UserInterface $user, string $attribute): mixed
    {
        return match($attribute) {
            'id' => $user->getId(),
            'email' => $user->getEmail(),
            'roles' => $user->getRoles(),
            'department' => $user->getDepartment(),
            default => null
        };
    }
    
    private function getUserLocation(UserInterface $user): string
    {
        // Implementation to get user location
        return 'US';
    }
    
    private function loadPolicies(): void
    {
        $this->policies = [
            [
                'effect' => 'allow',
                'permission' => 'read:*',
                'user_attributes' => ['roles' => ['admin']]
            ],
            [
                'effect' => 'allow',
                'permission' => 'read:own',
                'user_attributes' => ['roles' => ['user']]
            ],
            [
                'effect' => 'allow',
                'permission' => 'write:documents',
                'conditions' => [
                    [
                        'operator' => 'gte',
                        'left' => '$time',
                        'right' => 1609459200 // 2021-01-01
                    ]
                ]
            ]
        ];
    }
}
```

## Data Protection

```php
<?php
// app/Infrastructure/Security/Encryption/EncryptionManager.php

namespace App\Infrastructure\Security\Encryption;

use TuskLang\Security\EncryptionManagerInterface;

class EncryptionManager implements EncryptionManagerInterface
{
    private array $config;
    private array $keys;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->keys = [
            'current' => $config['encryption']['keys']['current'],
            'previous' => $config['encryption']['keys']['previous'],
            'next' => $config['encryption']['keys']['next']
        ];
    }
    
    public function encrypt(string $data): string
    {
        $algorithm = $this->config['encryption']['algorithm'];
        $key = $this->keys['current'];
        
        $iv = random_bytes(openssl_cipher_iv_length($algorithm));
        $encrypted = openssl_encrypt($data, $algorithm, $key, OPENSSL_RAW_DATA, $iv);
        
        $tag = '';
        if (str_contains($algorithm, 'GCM')) {
            $encrypted = openssl_encrypt($data, $algorithm, $key, OPENSSL_RAW_DATA, $iv, $tag);
        }
        
        $result = base64_encode($iv . $encrypted);
        if ($tag) {
            $result .= '.' . base64_encode($tag);
        }
        
        return $result;
    }
    
    public function decrypt(string $encryptedData): string
    {
        $algorithm = $this->config['encryption']['algorithm'];
        
        $parts = explode('.', $encryptedData);
        $data = base64_decode($parts[0]);
        $tag = isset($parts[1]) ? base64_decode($parts[1]) : '';
        
        $ivLength = openssl_cipher_iv_length($algorithm);
        $iv = substr($data, 0, $ivLength);
        $encrypted = substr($data, $ivLength);
        
        // Try current key first
        $decrypted = $this->decryptWithKey($encrypted, $algorithm, $this->keys['current'], $iv, $tag);
        if ($decrypted !== false) {
            return $decrypted;
        }
        
        // Try previous key
        if ($this->keys['previous']) {
            $decrypted = $this->decryptWithKey($encrypted, $algorithm, $this->keys['previous'], $iv, $tag);
            if ($decrypted !== false) {
                return $decrypted;
            }
        }
        
        throw new \RuntimeException('Failed to decrypt data');
    }
    
    private function decryptWithKey(string $encrypted, string $algorithm, string $key, string $iv, string $tag): string|false
    {
        if (str_contains($algorithm, 'GCM')) {
            return openssl_decrypt($encrypted, $algorithm, $key, OPENSSL_RAW_DATA, $iv, $tag);
        }
        
        return openssl_decrypt($encrypted, $algorithm, $key, OPENSSL_RAW_DATA, $iv);
    }
    
    public function rotateKeys(): void
    {
        // Generate new key
        $newKey = random_bytes(32);
        
        // Update keys
        $this->keys['previous'] = $this->keys['current'];
        $this->keys['current'] = $newKey;
        $this->keys['next'] = random_bytes(32);
        
        // Re-encrypt sensitive data with new key
        $this->reencryptData();
    }
    
    private function reencryptData(): void
    {
        // Implementation to re-encrypt all sensitive data with new key
        // This would typically involve a background job
    }
    
    public function hashPassword(string $password): string
    {
        return password_hash($password, PASSWORD_ARGON2ID, [
            'memory_cost' => 65536,
            'time_cost' => 4,
            'threads' => 3
        ]);
    }
    
    public function verifyPassword(string $password, string $hash): bool
    {
        return password_verify($password, $hash);
    }
    
    public function generateSecureToken(): string
    {
        return bin2hex(random_bytes(32));
    }
}

// app/Infrastructure/Security/Validation/InputValidator.php

namespace App\Infrastructure\Security\Validation;

use TuskLang\Security\InputValidatorInterface;

class InputValidator implements InputValidatorInterface
{
    private array $config;
    private array $rules;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->rules = $config['input_validation']['rules'];
    }
    
    public function validateEmail(string $email): bool
    {
        $rule = $this->rules['email'];
        
        if (strlen($email) > $rule['max_length']) {
            return false;
        }
        
        return preg_match('/' . $rule['pattern'] . '/', $email) === 1;
    }
    
    public function validatePassword(string $password): array
    {
        $rule = $this->rules['password'];
        $errors = [];
        
        if (strlen($password) < $rule['min_length']) {
            $errors[] = "Password must be at least {$rule['min_length']} characters long";
        }
        
        if ($rule['require_uppercase'] && !preg_match('/[A-Z]/', $password)) {
            $errors[] = 'Password must contain at least one uppercase letter';
        }
        
        if ($rule['require_lowercase'] && !preg_match('/[a-z]/', $password)) {
            $errors[] = 'Password must contain at least one lowercase letter';
        }
        
        if ($rule['require_numbers'] && !preg_match('/[0-9]/', $password)) {
            $errors[] = 'Password must contain at least one number';
        }
        
        if ($rule['require_special'] && !preg_match('/[^A-Za-z0-9]/', $password)) {
            $errors[] = 'Password must contain at least one special character';
        }
        
        if ($rule['prevent_common'] && $this->isCommonPassword($password)) {
            $errors[] = 'Password is too common';
        }
        
        return $errors;
    }
    
    public function validateUsername(string $username): array
    {
        $rule = $this->rules['username'];
        $errors = [];
        
        if (!preg_match('/' . $rule['pattern'] . '/', $username)) {
            $errors[] = 'Username format is invalid';
        }
        
        if (in_array(strtolower($username), $rule['reserved_names'])) {
            $errors[] = 'Username is reserved';
        }
        
        return $errors;
    }
    
    public function sanitizeInput(string $input): string
    {
        // Remove null bytes
        $input = str_replace("\0", '', $input);
        
        // Convert special characters to HTML entities
        $input = htmlspecialchars($input, ENT_QUOTES, 'UTF-8');
        
        // Remove potentially dangerous characters
        $input = preg_replace('/[<>"\']/', '', $input);
        
        return trim($input);
    }
    
    public function validateFileUpload(array $file): array
    {
        $errors = [];
        $config = $this->config['threat_protection']['file_upload'];
        
        // Check file size
        if ($file['size'] > $this->parseFileSize($config['max_size'])) {
            $errors[] = 'File size exceeds maximum allowed size';
        }
        
        // Check file type
        $extension = strtolower(pathinfo($file['name'], PATHINFO_EXTENSION));
        if (!in_array($extension, $config['allowed_types'])) {
            $errors[] = 'File type not allowed';
        }
        
        // Check MIME type
        $finfo = finfo_open(FILEINFO_MIME_TYPE);
        $mimeType = finfo_file($finfo, $file['tmp_name']);
        finfo_close($finfo);
        
        $allowedMimeTypes = [
            'jpg' => 'image/jpeg',
            'png' => 'image/png',
            'pdf' => 'application/pdf',
            'doc' => 'application/msword'
        ];
        
        if (!isset($allowedMimeTypes[$extension]) || $allowedMimeTypes[$extension] !== $mimeType) {
            $errors[] = 'File MIME type does not match extension';
        }
        
        return $errors;
    }
    
    private function isCommonPassword(string $password): bool
    {
        $commonPasswords = [
            'password', '123456', 'qwerty', 'admin', 'letmein',
            'welcome', 'monkey', 'dragon', 'master', 'football'
        ];
        
        return in_array(strtolower($password), $commonPasswords);
    }
    
    private function parseFileSize(string $size): int
    {
        $value = (int) $size;
        $unit = strtoupper(substr($size, -2));
        
        return match($unit) {
            'KB' => $value * 1024,
            'MB' => $value * 1024 * 1024,
            'GB' => $value * 1024 * 1024 * 1024,
            default => $value
        };
    }
}
```

## Security Monitoring

```php
<?php
// app/Infrastructure/Security/Monitoring/SecurityMonitor.php

namespace App\Infrastructure\Security\Monitoring;

use TuskLang\Security\SecurityMonitorInterface;
use TuskLang\Logging\LoggerInterface;

class SecurityMonitor implements SecurityMonitorInterface
{
    private LoggerInterface $logger;
    private array $config;
    private array $threatIndicators = [];
    
    public function __construct(LoggerInterface $logger, array $config)
    {
        $this->logger = $logger;
        $this->config = $config;
    }
    
    public function logAuthenticationAttempt(string $username, bool $success, array $context = []): void
    {
        $event = [
            'event_type' => $success ? 'authentication.success' : 'authentication.failure',
            'username' => $username,
            'ip_address' => $_SERVER['REMOTE_ADDR'] ?? 'unknown',
            'user_agent' => $_SERVER['HTTP_USER_AGENT'] ?? 'unknown',
            'timestamp' => date('c'),
            'context' => $context
        ];
        
        $this->logger->info('Authentication attempt', $event);
        
        if (!$success) {
            $this->analyzeThreat($event);
        }
    }
    
    public function logAuthorizationAttempt(string $user, string $permission, bool $granted): void
    {
        $event = [
            'event_type' => $granted ? 'authorization.grant' : 'authorization.deny',
            'user' => $user,
            'permission' => $permission,
            'ip_address' => $_SERVER['REMOTE_ADDR'] ?? 'unknown',
            'timestamp' => date('c')
        ];
        
        $this->logger->info('Authorization attempt', $event);
        
        if (!$granted) {
            $this->analyzeThreat($event);
        }
    }
    
    public function logDataAccess(string $user, string $resource, string $action): void
    {
        $event = [
            'event_type' => 'data.access',
            'user' => $user,
            'resource' => $resource,
            'action' => $action,
            'ip_address' => $_SERVER['REMOTE_ADDR'] ?? 'unknown',
            'timestamp' => date('c')
        ];
        
        $this->logger->info('Data access', $event);
    }
    
    public function logDataModification(string $user, string $resource, string $action, array $changes): void
    {
        $event = [
            'event_type' => 'data.modification',
            'user' => $user,
            'resource' => $resource,
            'action' => $action,
            'changes' => $changes,
            'ip_address' => $_SERVER['REMOTE_ADDR'] ?? 'unknown',
            'timestamp' => date('c')
        ];
        
        $this->logger->info('Data modification', $event);
    }
    
    public function logSecurityEvent(string $eventType, array $data): void
    {
        $event = array_merge($data, [
            'event_type' => $eventType,
            'ip_address' => $_SERVER['REMOTE_ADDR'] ?? 'unknown',
            'timestamp' => date('c')
        ]);
        
        $this->logger->warning('Security event', $event);
        $this->analyzeThreat($event);
    }
    
    private function analyzeThreat(array $event): void
    {
        $threatLevel = $this->calculateThreatLevel($event);
        
        if ($threatLevel > 0.7) {
            $this->triggerAlert($event, $threatLevel);
        }
        
        $this->threatIndicators[] = [
            'event' => $event,
            'threat_level' => $threatLevel,
            'timestamp' => time()
        ];
        
        // Clean old indicators
        $this->cleanOldIndicators();
    }
    
    private function calculateThreatLevel(array $event): float
    {
        $threatLevel = 0.0;
        
        // Failed authentication attempts
        if ($event['event_type'] === 'authentication.failure') {
            $recentFailures = $this->countRecentFailures($event['ip_address'], 300); // 5 minutes
            $threatLevel += min($recentFailures * 0.2, 1.0);
        }
        
        // Authorization denials
        if ($event['event_type'] === 'authorization.deny') {
            $recentDenials = $this->countRecentDenials($event['user'], 3600); // 1 hour
            $threatLevel += min($recentDenials * 0.3, 1.0);
        }
        
        // Suspicious patterns
        if ($this->isSuspiciousPattern($event)) {
            $threatLevel += 0.5;
        }
        
        return min($threatLevel, 1.0);
    }
    
    private function countRecentFailures(string $ipAddress, int $window): int
    {
        $cutoff = time() - $window;
        return count(array_filter($this->threatIndicators, function($indicator) use ($ipAddress, $cutoff) {
            return $indicator['event']['event_type'] === 'authentication.failure' &&
                   $indicator['event']['ip_address'] === $ipAddress &&
                   $indicator['timestamp'] >= $cutoff;
        }));
    }
    
    private function countRecentDenials(string $user, int $window): int
    {
        $cutoff = time() - $window;
        return count(array_filter($this->threatIndicators, function($indicator) use ($user, $cutoff) {
            return $indicator['event']['event_type'] === 'authorization.deny' &&
                   $indicator['event']['user'] === $user &&
                   $indicator['timestamp'] >= $cutoff;
        }));
    }
    
    private function isSuspiciousPattern(array $event): bool
    {
        // Check for SQL injection attempts
        if (isset($event['username']) && $this->containsSqlInjection($event['username'])) {
            return true;
        }
        
        // Check for XSS attempts
        if (isset($event['user_agent']) && $this->containsXss($event['user_agent'])) {
            return true;
        }
        
        // Check for rapid requests
        if ($this->isRapidRequest($event['ip_address'])) {
            return true;
        }
        
        return false;
    }
    
    private function containsSqlInjection(string $input): bool
    {
        $patterns = [
            '/union\s+select/i',
            '/drop\s+table/i',
            '/insert\s+into/i',
            '/delete\s+from/i',
            '/update\s+set/i',
            '/or\s+1\s*=\s*1/i',
            '/;\s*$/'
        ];
        
        foreach ($patterns as $pattern) {
            if (preg_match($pattern, $input)) {
                return true;
            }
        }
        
        return false;
    }
    
    private function containsXss(string $input): bool
    {
        $patterns = [
            '/<script/i',
            '/javascript:/i',
            '/on\w+\s*=/i',
            '/<iframe/i',
            '/<object/i'
        ];
        
        foreach ($patterns as $pattern) {
            if (preg_match($pattern, $input)) {
                return true;
            }
        }
        
        return false;
    }
    
    private function isRapidRequest(string $ipAddress): bool
    {
        $recentRequests = count(array_filter($this->threatIndicators, function($indicator) use ($ipAddress) {
            return $indicator['event']['ip_address'] === $ipAddress &&
                   $indicator['timestamp'] >= time() - 60; // Last minute
        }));
        
        return $recentRequests > 100; // More than 100 requests per minute
    }
    
    private function triggerAlert(array $event, float $threatLevel): void
    {
        $alert = [
            'type' => 'security_threat',
            'threat_level' => $threatLevel,
            'event' => $event,
            'timestamp' => date('c')
        ];
        
        $this->logger->critical('Security threat detected', $alert);
        
        // Send notification
        $this->sendSecurityNotification($alert);
    }
    
    private function sendSecurityNotification(array $alert): void
    {
        // Implementation to send security notifications
        // This could be email, SMS, Slack, etc.
    }
    
    private function cleanOldIndicators(): void
    {
        $cutoff = time() - 86400; // 24 hours
        $this->threatIndicators = array_filter($this->threatIndicators, function($indicator) use ($cutoff) {
            return $indicator['timestamp'] >= $cutoff;
        });
    }
}
```

## Best Practices

```php
// config/security-best-practices.tsk
security_best_practices = {
    authentication = {
        use_strong_passwords = true
        implement_mfa = true
        use_secure_session_management = true
        implement_account_lockout = true
    }
    
    authorization = {
        use_principle_of_least_privilege = true
        implement_role_based_access = true
        use_attribute_based_access = true
        audit_access_decisions = true
    }
    
    data_protection = {
        encrypt_sensitive_data = true
        use_secure_key_management = true
        implement_data_classification = true
        use_secure_communication = true
    }
    
    input_validation = {
        validate_all_inputs = true
        use_whitelist_approach = true
        sanitize_output = true
        prevent_injection_attacks = true
    }
    
    monitoring = {
        log_security_events = true
        monitor_for_threats = true
        implement_alerting = true
        conduct_security_audits = true
    }
    
    compliance = {
        follow_owasp_guidelines = true
        implement_gdpr_compliance = true
        use_secure_coding_practices = true
        conduct_penetration_testing = true
    }
}

// Example usage in PHP
class SecurityBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Configure security
        $config = TuskLang::load('security');
        $authenticator = new JwtAuthenticator($config['authentication']);
        $authorizer = new RbacAuthorizer($config['authorization']);
        $encryptionManager = new EncryptionManager($config['encryption']);
        $inputValidator = new InputValidator($config['input_validation']);
        $securityMonitor = new SecurityMonitor($this->logger, $config);
        
        // 2. Implement authentication
        $user = $authenticator->authenticate(['token' => $this->getAuthToken()]);
        if (!$user) {
            $securityMonitor->logAuthenticationAttempt('unknown', false);
            throw new \RuntimeException('Authentication failed');
        }
        
        $securityMonitor->logAuthenticationAttempt($user->getEmail(), true);
        
        // 3. Implement authorization
        if (!$authorizer->hasPermission($user, 'read:users')) {
            $securityMonitor->logAuthorizationAttempt($user->getEmail(), 'read:users', false);
            throw new \RuntimeException('Access denied');
        }
        
        $securityMonitor->logAuthorizationAttempt($user->getEmail(), 'read:users', true);
        
        // 4. Validate input
        $input = $this->getInput();
        $sanitizedInput = $inputValidator->sanitizeInput($input);
        
        if (!$inputValidator->validateEmail($sanitizedInput['email'])) {
            throw new \RuntimeException('Invalid email format');
        }
        
        // 5. Protect sensitive data
        $sensitiveData = $sanitizedInput['ssn'];
        $encryptedData = $encryptionManager->encrypt($sensitiveData);
        
        // 6. Log security events
        $securityMonitor->logDataAccess($user->getEmail(), 'users', 'read');
        $securityMonitor->logDataModification($user->getEmail(), 'users', 'update', $sanitizedInput);
        
        // 7. Monitor for threats
        $threatLevel = $securityMonitor->getThreatLevel();
        if ($threatLevel > 0.8) {
            $this->logger->critical('High threat level detected', ['level' => $threatLevel]);
        }
        
        // 8. Log and monitor
        $this->logger->info('Security measures implemented', [
            'user' => $user->getEmail(),
            'permissions' => $user->getRoles(),
            'threat_level' => $threatLevel,
            'encryption_enabled' => $config['encryption']['enabled']
        ]);
    }
    
    private function getAuthToken(): ?string
    {
        return $_SERVER['HTTP_AUTHORIZATION'] ?? null;
    }
    
    private function getInput(): array
    {
        return $_POST;
    }
}
```

This comprehensive guide covers advanced security patterns in TuskLang with PHP integration. The security system is designed to be robust, compliant, and protective while maintaining the rebellious spirit of TuskLang development. 
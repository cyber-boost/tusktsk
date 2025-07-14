# 🔒 Advanced Security Patterns with TuskLang & PHP

## Introduction
Security is the foundation of trustworthy applications. TuskLang and PHP let you implement sophisticated security patterns with config-driven authentication, authorization, encryption, and protection mechanisms that defend against modern threats.

## Key Features
- **Multi-factor authentication**
- **Role-based access control**
- **Encryption and key management**
- **Input validation and sanitization**
- **CSRF and XSS protection**
- **SQL injection prevention**

## Example: Security Configuration
```ini
[security]
auth: @go("security.ConfigureAuth")
encryption: @go("security.ConfigureEncryption")
validation: @go("security.ConfigureValidation")
csrf: @go("security.ConfigureCSRF")
xss: @go("security.ConfigureXSS")
```

## PHP: Authentication System
```php
<?php

namespace App\Security;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class AuthenticationManager
{
    private $config;
    private $providers = [];
    private $session;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->session = new SessionManager();
        $this->loadProviders();
    }
    
    public function authenticate($credentials, $provider = 'default')
    {
        if (!isset($this->providers[$provider])) {
            throw new \Exception("Authentication provider not found: {$provider}");
        }
        
        $startTime = microtime(true);
        
        try {
            $user = $this->providers[$provider]->authenticate($credentials);
            
            if ($user) {
                $this->createSession($user);
                $this->recordLogin($user);
                
                $duration = (microtime(true) - $startTime) * 1000;
                Metrics::record("auth_success", 1, [
                    "provider" => $provider,
                    "duration" => $duration
                ]);
                
                return $user;
            }
            
            $this->recordFailedLogin($credentials);
            
            Metrics::record("auth_failure", 1, [
                "provider" => $provider
            ]);
            
            return null;
            
        } catch (\Exception $e) {
            Metrics::record("auth_error", 1, [
                "provider" => $provider,
                "error" => get_class($e)
            ]);
            
            throw $e;
        }
    }
    
    public function isAuthenticated()
    {
        return $this->session->has('user_id');
    }
    
    public function getCurrentUser()
    {
        if (!$this->isAuthenticated()) {
            return null;
        }
        
        $userId = $this->session->get('user_id');
        return $this->loadUser($userId);
    }
    
    public function logout()
    {
        $this->session->destroy();
        Metrics::record("auth_logout", 1);
    }
    
    private function loadProviders()
    {
        $providers = $this->config->get('security.auth.providers', []);
        
        foreach ($providers as $name => $config) {
            $this->providers[$name] = new $config['class']($config);
        }
    }
    
    private function createSession($user)
    {
        $this->session->set('user_id', $user->getId());
        $this->session->set('user_role', $user->getRole());
        $this->session->set('auth_time', time());
        
        // Regenerate session ID for security
        $this->session->regenerateId();
    }
    
    private function recordLogin($user)
    {
        $pdo = new PDO(Env::get('DB_DSN'));
        $stmt = $pdo->prepare("
            INSERT INTO login_logs (user_id, ip_address, user_agent, created_at)
            VALUES (?, ?, ?, NOW())
        ");
        
        $stmt->execute([
            $user->getId(),
            $_SERVER['REMOTE_ADDR'] ?? '',
            $_SERVER['HTTP_USER_AGENT'] ?? ''
        ]);
    }
    
    private function recordFailedLogin($credentials)
    {
        $pdo = new PDO(Env::get('DB_DSN'));
        $stmt = $pdo->prepare("
            INSERT INTO failed_login_logs (email, ip_address, user_agent, created_at)
            VALUES (?, ?, ?, NOW())
        ");
        
        $stmt->execute([
            $credentials['email'] ?? '',
            $_SERVER['REMOTE_ADDR'] ?? '',
            $_SERVER['HTTP_USER_AGENT'] ?? ''
        ]);
    }
}

class DatabaseAuthProvider
{
    private $config;
    private $pdo;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->pdo = new PDO(Env::get('DB_DSN'));
    }
    
    public function authenticate($credentials)
    {
        $email = $credentials['email'] ?? '';
        $password = $credentials['password'] ?? '';
        
        $stmt = $this->pdo->prepare("
            SELECT id, email, password_hash, role, status
            FROM users
            WHERE email = ? AND status = 'active'
        ");
        
        $stmt->execute([$email]);
        $user = $stmt->fetch();
        
        if ($user && password_verify($password, $user['password_hash'])) {
            return new User($user);
        }
        
        return null;
    }
}

class OAuthAuthProvider
{
    private $config;
    private $client;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->client = new OAuthClient($config);
    }
    
    public function authenticate($credentials)
    {
        $token = $credentials['token'] ?? '';
        
        $userData = $this->client->getUserInfo($token);
        
        if ($userData) {
            return $this->findOrCreateUser($userData);
        }
        
        return null;
    }
    
    private function findOrCreateUser($userData)
    {
        $pdo = new PDO(Env::get('DB_DSN'));
        
        $stmt = $pdo->prepare("
            SELECT id, email, role, status
            FROM users
            WHERE oauth_id = ?
        ");
        
        $stmt->execute([$userData['id']]);
        $user = $stmt->fetch();
        
        if ($user) {
            return new User($user);
        }
        
        // Create new user
        $stmt = $pdo->prepare("
            INSERT INTO users (oauth_id, email, name, role, status, created_at)
            VALUES (?, ?, ?, 'user', 'active', NOW())
        ");
        
        $stmt->execute([
            $userData['id'],
            $userData['email'],
            $userData['name']
        ]);
        
        return new User([
            'id' => $pdo->lastInsertId(),
            'email' => $userData['email'],
            'role' => 'user',
            'status' => 'active'
        ]);
    }
}
```

## Authorization System
```php
<?php

namespace App\Security\Authorization;

use TuskLang\Config;

class AuthorizationManager
{
    private $config;
    private $policies = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadPolicies();
    }
    
    public function can($user, $action, $resource = null)
    {
        $role = $user->getRole();
        $policies = $this->getPoliciesForRole($role);
        
        foreach ($policies as $policy) {
            if ($policy->matches($action, $resource)) {
                return $policy->evaluate($user, $action, $resource);
            }
        }
        
        return false;
    }
    
    public function authorize($user, $action, $resource = null)
    {
        if (!$this->can($user, $action, $resource)) {
            throw new AccessDeniedException("Access denied for action: {$action}");
        }
    }
    
    private function loadPolicies()
    {
        $policies = $this->config->get('security.authorization.policies', []);
        
        foreach ($policies as $policy) {
            $this->policies[] = new Policy($policy);
        }
    }
    
    private function getPoliciesForRole($role)
    {
        return array_filter($this->policies, function($policy) use ($role) {
            return $policy->appliesToRole($role);
        });
    }
}

class Policy
{
    private $config;
    
    public function __construct($config)
    {
        $this->config = $config;
    }
    
    public function matches($action, $resource)
    {
        $actions = $this->config['actions'] ?? [];
        $resources = $this->config['resources'] ?? [];
        
        $actionMatch = empty($actions) || in_array($action, $actions);
        $resourceMatch = empty($resources) || in_array($resource, $resources);
        
        return $actionMatch && $resourceMatch;
    }
    
    public function appliesToRole($role)
    {
        $roles = $this->config['roles'] ?? [];
        return empty($roles) || in_array($role, $roles);
    }
    
    public function evaluate($user, $action, $resource)
    {
        $condition = $this->config['condition'] ?? null;
        
        if ($condition) {
            return $this->evaluateCondition($condition, $user, $action, $resource);
        }
        
        return $this->config['allow'] ?? false;
    }
    
    private function evaluateCondition($condition, $user, $action, $resource)
    {
        switch ($condition['type']) {
            case 'owner':
                return $this->isOwner($user, $resource);
                
            case 'custom':
                return $this->evaluateCustomCondition($condition, $user, $action, $resource);
                
            default:
                return false;
        }
    }
    
    private function isOwner($user, $resource)
    {
        if (!$resource || !method_exists($resource, 'getOwnerId')) {
            return false;
        }
        
        return $resource->getOwnerId() === $user->getId();
    }
    
    private function evaluateCustomCondition($condition, $user, $action, $resource)
    {
        $callback = $condition['callback'];
        return call_user_func($callback, $user, $action, $resource);
    }
}

class RoleBasedAccessControl
{
    private $config;
    private $roles = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadRoles();
    }
    
    public function hasPermission($user, $permission)
    {
        $role = $user->getRole();
        $permissions = $this->getPermissionsForRole($role);
        
        return in_array($permission, $permissions);
    }
    
    public function getPermissionsForRole($role)
    {
        return $this->roles[$role]['permissions'] ?? [];
    }
    
    private function loadRoles()
    {
        $this->roles = $this->config->get('security.rbac.roles', []);
    }
}
```

## Encryption and Key Management
```php
<?php

namespace App\Security\Encryption;

use TuskLang\Config;

class EncryptionManager
{
    private $config;
    private $algorithms = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadAlgorithms();
    }
    
    public function encrypt($data, $algorithm = 'aes-256-gcm')
    {
        if (!isset($this->algorithms[$algorithm])) {
            throw new \Exception("Encryption algorithm not found: {$algorithm}");
        }
        
        return $this->algorithms[$algorithm]->encrypt($data);
    }
    
    public function decrypt($data, $algorithm = 'aes-256-gcm')
    {
        if (!isset($this->algorithms[$algorithm])) {
            throw new \Exception("Encryption algorithm not found: {$algorithm}");
        }
        
        return $this->algorithms[$algorithm]->decrypt($data);
    }
    
    private function loadAlgorithms()
    {
        $this->algorithms['aes-256-gcm'] = new AES256GCM();
        $this->algorithms['aes-256-cbc'] = new AES256CBC();
        $this->algorithms['chacha20-poly1305'] = new ChaCha20Poly1305();
    }
}

class AES256GCM
{
    private $key;
    
    public function __construct()
    {
        $this->key = Env::secure('ENCRYPTION_KEY');
    }
    
    public function encrypt($data)
    {
        $iv = random_bytes(16);
        $tag = '';
        
        $encrypted = openssl_encrypt(
            $data,
            'aes-256-gcm',
            $this->key,
            OPENSSL_RAW_DATA,
            $iv,
            $tag
        );
        
        return base64_encode($iv . $tag . $encrypted);
    }
    
    public function decrypt($data)
    {
        $data = base64_decode($data);
        
        $iv = substr($data, 0, 16);
        $tag = substr($data, 16, 16);
        $encrypted = substr($data, 32);
        
        return openssl_decrypt(
            $encrypted,
            'aes-256-gcm',
            $this->key,
            OPENSSL_RAW_DATA,
            $iv,
            $tag
        );
    }
}

class KeyManager
{
    private $config;
    private $keys = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadKeys();
    }
    
    public function getKey($purpose)
    {
        if (!isset($this->keys[$purpose])) {
            throw new \Exception("Key not found for purpose: {$purpose}");
        }
        
        return $this->keys[$purpose];
    }
    
    public function rotateKey($purpose)
    {
        $newKey = $this->generateKey();
        $this->keys[$purpose] = $newKey;
        
        // Store new key securely
        $this->storeKey($purpose, $newKey);
        
        return $newKey;
    }
    
    private function generateKey()
    {
        return random_bytes(32);
    }
    
    private function loadKeys()
    {
        $keys = $this->config->get('security.encryption.keys', []);
        
        foreach ($keys as $purpose => $key) {
            $this->keys[$purpose] = $this->loadKey($purpose);
        }
    }
    
    private function loadKey($purpose)
    {
        // Load key from secure storage (e.g., environment variable, key management service)
        return Env::secure("KEY_{$purpose}");
    }
    
    private function storeKey($purpose, $key)
    {
        // Store key in secure storage
        // This would typically use a key management service
    }
}
```

## Input Validation and Sanitization
```php
<?php

namespace App\Security\Validation;

use TuskLang\Config;

class InputValidator
{
    private $config;
    private $validators = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadValidators();
    }
    
    public function validate($data, $rules)
    {
        $errors = [];
        
        foreach ($rules as $field => $fieldRules) {
            $value = $this->getNestedValue($data, $field);
            $fieldErrors = $this->validateField($field, $value, $fieldRules);
            
            if (!empty($fieldErrors)) {
                $errors[$field] = $fieldErrors;
            }
        }
        
        return $errors;
    }
    
    public function sanitize($data, $rules)
    {
        $sanitized = [];
        
        foreach ($rules as $field => $fieldRules) {
            $value = $this->getNestedValue($data, $field);
            $sanitized[$field] = $this->sanitizeField($value, $fieldRules);
        }
        
        return $sanitized;
    }
    
    private function validateField($field, $value, $rules)
    {
        $errors = [];
        
        foreach ($rules as $rule) {
            $validator = $this->getValidator($rule['type']);
            
            if (!$validator->validate($value, $rule['params'] ?? [])) {
                $errors[] = $rule['message'] ?? "Validation failed for {$field}";
            }
        }
        
        return $errors;
    }
    
    private function sanitizeField($value, $rules)
    {
        foreach ($rules as $rule) {
            if (isset($rule['sanitize'])) {
                $sanitizer = $this->getSanitizer($rule['sanitize']);
                $value = $sanitizer->sanitize($value, $rule['params'] ?? []);
            }
        }
        
        return $value;
    }
    
    private function loadValidators()
    {
        $this->validators['email'] = new EmailValidator();
        $this->validators['url'] = new URLValidator();
        $this->validators['integer'] = new IntegerValidator();
        $this->validators['string'] = new StringValidator();
        $this->validators['file'] = new FileValidator();
    }
    
    private function getValidator($type)
    {
        if (!isset($this->validators[$type])) {
            throw new \Exception("Validator not found: {$type}");
        }
        
        return $this->validators[$type];
    }
    
    private function getSanitizer($type)
    {
        switch ($type) {
            case 'html':
                return new HTMLSanitizer();
            case 'sql':
                return new SQLSanitizer();
            case 'xss':
                return new XSSSanitizer();
            default:
                throw new \Exception("Sanitizer not found: {$type}");
        }
    }
}

class XSSSanitizer
{
    public function sanitize($data, $params = [])
    {
        if (is_string($data)) {
            return htmlspecialchars($data, ENT_QUOTES, 'UTF-8');
        }
        
        if (is_array($data)) {
            return array_map([$this, 'sanitize'], $data);
        }
        
        return $data;
    }
}

class SQLSanitizer
{
    public function sanitize($data, $params = [])
    {
        // This is a basic example - in practice, use prepared statements
        if (is_string($data)) {
            return addslashes($data);
        }
        
        return $data;
    }
}
```

## CSRF Protection
```php
<?php

namespace App\Security\CSRF;

use TuskLang\Config;

class CSRFProtection
{
    private $config;
    private $session;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->session = new SessionManager();
    }
    
    public function generateToken()
    {
        $token = bin2hex(random_bytes(32));
        $this->session->set('csrf_token', $token);
        
        return $token;
    }
    
    public function validateToken($token)
    {
        $storedToken = $this->session->get('csrf_token');
        
        if (!$storedToken || !$token) {
            return false;
        }
        
        return hash_equals($storedToken, $token);
    }
    
    public function getTokenField()
    {
        $token = $this->generateToken();
        return "<input type=\"hidden\" name=\"csrf_token\" value=\"{$token}\">";
    }
    
    public function validateRequest()
    {
        if ($_SERVER['REQUEST_METHOD'] === 'GET') {
            return true;
        }
        
        $token = $_POST['csrf_token'] ?? $_SERVER['HTTP_X_CSRF_TOKEN'] ?? null;
        
        if (!$this->validateToken($token)) {
            throw new CSRFException("Invalid CSRF token");
        }
        
        return true;
    }
}
```

## Best Practices
- **Use strong authentication methods**
- **Implement proper authorization**
- **Encrypt sensitive data**
- **Validate all inputs**
- **Use HTTPS everywhere**
- **Implement rate limiting**

## Performance Optimization
- **Cache authentication results**
- **Use efficient encryption**
- **Optimize validation rules**
- **Monitor security events**

## Security Considerations
- **Use secure random generators**
- **Implement proper session management**
- **Use prepared statements**
- **Validate file uploads**

## Troubleshooting
- **Monitor authentication logs**
- **Check authorization policies**
- **Verify encryption keys**
- **Test security measures**

## Conclusion
TuskLang + PHP = security that's robust, configurable, and battle-tested. Build applications that users can trust. 
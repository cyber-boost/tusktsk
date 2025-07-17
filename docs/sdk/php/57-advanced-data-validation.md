# Advanced Data Validation

TuskLang provides sophisticated data validation capabilities that go far beyond simple type checking. This guide covers advanced validation patterns, custom validators, and integration with PHP frameworks.

## Table of Contents

- [Configuration-Driven Validation](#configuration-driven-validation)
- [Custom Validators](#custom-validators)
- [Cross-Field Validation](#cross-field-validation)
- [Async Validation](#async-validation)
- [Validation Pipelines](#validation-pipelines)
- [Database-Driven Validation](#database-driven-validation)
- [API Validation](#api-validation)
- [Security Validation](#security-validation)
- [Performance Optimization](#performance-optimization)
- [Best Practices](#best-practices)

## Configuration-Driven Validation

Define validation rules in TuskLang configuration files:

```php
// config/validation.tsk
validation_rules = {
    user_registration = {
        email = {
            required = true
            type = "email"
            unique = "users.email"
            max_length = 255
            pattern = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
        }
        password = {
            required = true
            min_length = 8
            max_length = 128
            pattern = "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{8,}$"
            strength = {
                min_uppercase = 1
                min_lowercase = 1
                min_digits = 1
                min_special = 1
            }
        }
        username = {
            required = true
            min_length = 3
            max_length = 50
            pattern = "^[a-zA-Z0-9_]+$"
            unique = "users.username"
            reserved_words = ["admin", "root", "system", "test"]
        }
        age = {
            required = true
            type = "integer"
            min = 13
            max = 120
            custom = "validate_age_with_country"
        }
    }
    
    product_creation = {
        name = {
            required = true
            min_length = 2
            max_length = 200
            sanitize = "strip_tags"
        }
        price = {
            required = true
            type = "decimal"
            min = 0.01
            max = 999999.99
            precision = 2
        }
        category_id = {
            required = true
            type = "integer"
            exists = "categories.id"
            active_only = true
        }
        tags = {
            type = "array"
            max_items = 10
            item_validation = {
                type = "string"
                min_length = 2
                max_length = 50
            }
        }
        metadata = {
            type = "json"
            schema = "product_metadata_schema"
            max_size = 1024
        }
    }
}
```

## Custom Validators

Create sophisticated custom validation logic:

```php
<?php
// app/Validators/AdvancedValidator.php

namespace App\Validators;

use TuskLang\Validation\ValidatorInterface;
use TuskLang\Validation\ValidationContext;
use TuskLang\Database\QueryBuilder;

class AdvancedValidator implements ValidatorInterface
{
    private QueryBuilder $db;
    private array $config;
    
    public function __construct(QueryBuilder $db, array $config)
    {
        $this->db = $db;
        $this->config = $config;
    }
    
    public function validateAgeWithCountry($value, $context): array
    {
        $country = $context->get('country', 'US');
        $minAge = $this->getMinimumAge($country);
        
        if ($value < $minAge) {
            return [
                'valid' => false,
                'message' => "Minimum age for {$country} is {$minAge} years"
            ];
        }
        
        return ['valid' => true];
    }
    
    public function validateUniqueWithConditions($value, $context): array
    {
        $table = $context->get('table');
        $column = $context->get('column');
        $excludeId = $context->get('exclude_id');
        $conditions = $context->get('conditions', []);
        
        $query = $this->db->table($table)
            ->where($column, $value);
            
        if ($excludeId) {
            $query->where('id', '!=', $excludeId);
        }
        
        foreach ($conditions as $field => $condition) {
            if (is_array($condition)) {
                $query->whereIn($field, $condition);
            } else {
                $query->where($field, $condition);
            }
        }
        
        $exists = $query->exists();
        
        return [
            'valid' => !$exists,
            'message' => $exists ? "Value already exists" : null
        ];
    }
    
    public function validateComplexPassword($value, $context): array
    {
        $config = $context->get('strength', []);
        $errors = [];
        
        // Check minimum length
        if (strlen($value) < ($config['min_length'] ?? 8)) {
            $errors[] = "Password must be at least " . ($config['min_length'] ?? 8) . " characters";
        }
        
        // Check character requirements
        if (($config['require_uppercase'] ?? true) && !preg_match('/[A-Z]/', $value)) {
            $errors[] = "Password must contain at least one uppercase letter";
        }
        
        if (($config['require_lowercase'] ?? true) && !preg_match('/[a-z]/', $value)) {
            $errors[] = "Password must contain at least one lowercase letter";
        }
        
        if (($config['require_digits'] ?? true) && !preg_match('/\d/', $value)) {
            $errors[] = "Password must contain at least one digit";
        }
        
        if (($config['require_special'] ?? true) && !preg_match('/[@$!%*?&]/', $value)) {
            $errors[] = "Password must contain at least one special character";
        }
        
        // Check against common passwords
        if ($this->isCommonPassword($value)) {
            $errors[] = "Password is too common, please choose a more unique password";
        }
        
        // Check for sequential characters
        if ($this->hasSequentialChars($value)) {
            $errors[] = "Password contains sequential characters";
        }
        
        return [
            'valid' => empty($errors),
            'message' => implode(', ', $errors)
        ];
    }
    
    public function validateJsonSchema($value, $context): array
    {
        $schema = $context->get('schema');
        $schemaData = $this->loadSchema($schema);
        
        if (!$schemaData) {
            return ['valid' => false, 'message' => "Schema '{$schema}' not found"];
        }
        
        $validator = new JsonSchemaValidator();
        $result = $validator->validate($value, $schemaData);
        
        return [
            'valid' => $result->isValid(),
            'message' => $result->getErrors()
        ];
    }
    
    private function getMinimumAge(string $country): int
    {
        $ages = [
            'US' => 13, 'CA' => 13, 'UK' => 13, 'AU' => 13,
            'DE' => 16, 'FR' => 15, 'JP' => 13, 'BR' => 13
        ];
        
        return $ages[$country] ?? 13;
    }
    
    private function isCommonPassword(string $password): bool
    {
        $commonPasswords = [
            'password', '123456', '123456789', 'qwerty',
            'abc123', 'password123', 'admin', 'letmein'
        ];
        
        return in_array(strtolower($password), $commonPasswords);
    }
    
    private function hasSequentialChars(string $password): bool
    {
        $sequences = ['123', 'abc', 'qwe', 'asd', 'zxc'];
        
        foreach ($sequences as $seq) {
            if (stripos($password, $seq) !== false) {
                return true;
            }
        }
        
        return false;
    }
    
    private function loadSchema(string $schemaName): ?array
    {
        $schemaPath = "schemas/{$schemaName}.json";
        
        if (!file_exists($schemaPath)) {
            return null;
        }
        
        return json_decode(file_get_contents($schemaPath), true);
    }
}
```

## Cross-Field Validation

Validate relationships between multiple fields:

```php
<?php
// app/Validators/CrossFieldValidator.php

namespace App\Validators;

class CrossFieldValidator
{
    public function validateDateRange($data, $context): array
    {
        $startDate = $data['start_date'] ?? null;
        $endDate = $data['end_date'] ?? null;
        
        if (!$startDate || !$endDate) {
            return ['valid' => true]; // Individual field validators handle required
        }
        
        $start = new DateTime($startDate);
        $end = new DateTime($endDate);
        
        if ($start >= $end) {
            return [
                'valid' => false,
                'message' => 'End date must be after start date'
            ];
        }
        
        // Check minimum duration
        $minDuration = $context->get('min_duration_days', 1);
        $duration = $start->diff($end)->days;
        
        if ($duration < $minDuration) {
            return [
                'valid' => false,
                'message' => "Minimum duration is {$minDuration} days"
            ];
        }
        
        return ['valid' => true];
    }
    
    public function validateConditionalRequired($data, $context): array
    {
        $field = $context->get('field');
        $conditionField = $context->get('condition_field');
        $conditionValue = $context->get('condition_value');
        
        $fieldValue = $data[$field] ?? null;
        $conditionFieldValue = $data[$conditionField] ?? null;
        
        if ($conditionFieldValue == $conditionValue && empty($fieldValue)) {
            return [
                'valid' => false,
                'message' => "Field '{$field}' is required when '{$conditionField}' is '{$conditionValue}'"
            ];
        }
        
        return ['valid' => true];
    }
    
    public function validateUniqueCombination($data, $context): array
    {
        $fields = $context->get('fields', []);
        $table = $context->get('table');
        $excludeId = $context->get('exclude_id');
        
        $values = [];
        foreach ($fields as $field) {
            $values[$field] = $data[$field] ?? null;
        }
        
        $query = $this->db->table($table);
        foreach ($values as $field => $value) {
            $query->where($field, $value);
        }
        
        if ($excludeId) {
            $query->where('id', '!=', $excludeId);
        }
        
        $exists = $query->exists();
        
        return [
            'valid' => !$exists,
            'message' => $exists ? 'Combination already exists' : null
        ];
    }
}
```

## Async Validation

Handle validation that requires external API calls:

```php
<?php
// app/Validators/AsyncValidator.php

namespace App\Validators;

use GuzzleHttp\Client;
use React\Promise\PromiseInterface;

class AsyncValidator
{
    private Client $httpClient;
    
    public function __construct(Client $httpClient)
    {
        $this->httpClient = $httpClient;
    }
    
    public function validateEmailDomain($email): PromiseInterface
    {
        $domain = substr(strrchr($email, "@"), 1);
        
        return $this->httpClient->getAsync("https://dns.google/resolve?name={$domain}&type=MX")
            ->then(
                function ($response) {
                    $data = json_decode($response->getBody(), true);
                    $hasMx = !empty($data['Answer'] ?? []);
                    
                    return [
                        'valid' => $hasMx,
                        'message' => $hasMx ? null : 'Invalid email domain'
                    ];
                },
                function ($exception) {
                    return [
                        'valid' => true, // Fail open for network issues
                        'message' => null
                    ];
                }
            );
    }
    
    public function validatePhoneNumber($phone, $country = 'US'): PromiseInterface
    {
        $apiKey = config('services.twilio.api_key');
        
        return $this->httpClient->getAsync("https://lookups.twilio.com/v1/PhoneNumbers/{$phone}", [
            'auth' => [$apiKey, '']
        ])
        ->then(
            function ($response) {
                $data = json_decode($response->getBody(), true);
                
                return [
                    'valid' => $data['valid'] ?? false,
                    'message' => $data['valid'] ? null : 'Invalid phone number',
                    'data' => $data
                ];
            },
            function ($exception) {
                return [
                    'valid' => true, // Fail open
                    'message' => null
                ];
            }
        );
    }
    
    public function validateAddress($address): PromiseInterface
    {
        $apiKey = config('services.google.maps_api_key');
        
        return $this->httpClient->getAsync("https://maps.googleapis.com/maps/api/geocode/json", [
            'query' => [
                'address' => $address,
                'key' => $apiKey
            ]
        ])
        ->then(
            function ($response) {
                $data = json_decode($response->getBody(), true);
                
                return [
                    'valid' => $data['status'] === 'OK',
                    'message' => $data['status'] === 'OK' ? null : 'Invalid address',
                    'data' => $data['results'][0] ?? null
                ];
            },
            function ($exception) {
                return [
                    'valid' => true, // Fail open
                    'message' => null
                ];
            }
        );
    }
}
```

## Validation Pipelines

Create complex validation workflows:

```php
<?php
// app/Validation/ValidationPipeline.php

namespace App\Validation;

class ValidationPipeline
{
    private array $validators = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
    }
    
    public function addValidator(string $name, callable $validator): self
    {
        $this->validators[$name] = $validator;
        return $this;
    }
    
    public function validate(array $data, string $ruleSet): ValidationResult
    {
        $rules = $this->config['validation_rules'][$ruleSet] ?? [];
        $errors = [];
        $warnings = [];
        
        // Phase 1: Basic validation
        foreach ($rules as $field => $fieldRules) {
            $fieldErrors = $this->validateField($data[$field] ?? null, $fieldRules, $data);
            if (!empty($fieldErrors)) {
                $errors[$field] = $fieldErrors;
            }
        }
        
        // Phase 2: Cross-field validation
        $crossFieldErrors = $this->validateCrossFields($data, $rules);
        $errors = array_merge($errors, $crossFieldErrors);
        
        // Phase 3: Business logic validation
        $businessErrors = $this->validateBusinessRules($data, $ruleSet);
        $errors = array_merge($errors, $businessErrors);
        
        // Phase 4: Async validation (if needed)
        if (!empty($errors)) {
            return new ValidationResult(false, $errors, $warnings);
        }
        
        $asyncErrors = $this->validateAsync($data, $rules);
        $errors = array_merge($errors, $asyncErrors);
        
        return new ValidationResult(empty($errors), $errors, $warnings);
    }
    
    private function validateField($value, array $rules, array $context): array
    {
        $errors = [];
        
        foreach ($rules as $rule => $config) {
            $validator = $this->validators[$rule] ?? null;
            
            if ($validator) {
                $result = $validator($value, new ValidationContext($config, $context));
                
                if (!$result['valid']) {
                    $errors[] = $result['message'];
                }
            }
        }
        
        return $errors;
    }
    
    private function validateCrossFields(array $data, array $rules): array
    {
        $errors = [];
        
        // Extract cross-field validation rules
        $crossFieldRules = array_filter($rules, function ($rule) {
            return isset($rule['cross_field']);
        });
        
        foreach ($crossFieldRules as $field => $rule) {
            $validator = $this->validators[$rule['cross_field']['type']] ?? null;
            
            if ($validator) {
                $result = $validator($data, new ValidationContext($rule['cross_field'], $data));
                
                if (!$result['valid']) {
                    $errors[$field] = [$result['message']];
                }
            }
        }
        
        return $errors;
    }
    
    private function validateBusinessRules(array $data, string $ruleSet): array
    {
        $errors = [];
        
        // Load business rules from TuskLang config
        $businessRules = $this->config['business_rules'][$ruleSet] ?? [];
        
        foreach ($businessRules as $rule) {
            $validator = $this->validators[$rule['type']] ?? null;
            
            if ($validator) {
                $result = $validator($data, new ValidationContext($rule, $data));
                
                if (!$result['valid']) {
                    $errors[$rule['field'] ?? 'general'] = [$result['message']];
                }
            }
        }
        
        return $errors;
    }
    
    private function validateAsync(array $data, array $rules): array
    {
        $errors = [];
        
        // Extract async validation rules
        $asyncRules = array_filter($rules, function ($rule) {
            return isset($rule['async']) && $rule['async'];
        });
        
        foreach ($asyncRules as $field => $rule) {
            $validator = $this->validators[$rule['type']] ?? null;
            
            if ($validator && method_exists($validator, 'validateAsync')) {
                $promise = $validator->validateAsync($data[$field] ?? null, new ValidationContext($rule, $data));
                
                // For simplicity, we'll wait for the result
                // In a real implementation, you might want to handle this differently
                $result = $promise->wait();
                
                if (!$result['valid']) {
                    $errors[$field] = [$result['message']];
                }
            }
        }
        
        return $errors;
    }
}

class ValidationResult
{
    public function __construct(
        public bool $isValid,
        public array $errors = [],
        public array $warnings = []
    ) {}
    
    public function hasErrors(): bool
    {
        return !empty($this->errors);
    }
    
    public function hasWarnings(): bool
    {
        return !empty($this->warnings);
    }
    
    public function getFirstError(): ?string
    {
        foreach ($this->errors as $fieldErrors) {
            if (is_array($fieldErrors)) {
                return $fieldErrors[0] ?? null;
            }
            return $fieldErrors;
        }
        return null;
    }
    
    public function toArray(): array
    {
        return [
            'valid' => $this->isValid,
            'errors' => $this->errors,
            'warnings' => $this->warnings
        ];
    }
}
```

## Database-Driven Validation

Use database queries for validation:

```php
<?php
// app/Validators/DatabaseValidator.php

namespace App\Validators;

use TuskLang\Database\QueryBuilder;

class DatabaseValidator
{
    private QueryBuilder $db;
    
    public function __construct(QueryBuilder $db)
    {
        $this->db = $db;
    }
    
    public function validateExists($value, $context): array
    {
        $table = $context->get('table');
        $column = $context->get('column', 'id');
        $conditions = $context->get('conditions', []);
        
        $query = $this->db->table($table)->where($column, $value);
        
        foreach ($conditions as $field => $condition) {
            if (is_array($condition)) {
                $query->whereIn($field, $condition);
            } else {
                $query->where($field, $condition);
            }
        }
        
        $exists = $query->exists();
        
        return [
            'valid' => $exists,
            'message' => $exists ? null : "Record not found in {$table}"
        ];
    }
    
    public function validateUnique($value, $context): array
    {
        $table = $context->get('table');
        $column = $context->get('column');
        $excludeId = $context->get('exclude_id');
        $conditions = $context->get('conditions', []);
        
        $query = $this->db->table($table)->where($column, $value);
        
        if ($excludeId) {
            $query->where('id', '!=', $excludeId);
        }
        
        foreach ($conditions as $field => $condition) {
            if (is_array($condition)) {
                $query->whereIn($field, $condition);
            } else {
                $query->where($field, $condition);
            }
        }
        
        $exists = $query->exists();
        
        return [
            'valid' => !$exists,
            'message' => $exists ? "Value already exists in {$table}" : null
        ];
    }
    
    public function validateForeignKey($value, $context): array
    {
        $table = $context->get('table');
        $column = $context->get('column', 'id');
        $activeOnly = $context->get('active_only', false);
        
        $query = $this->db->table($table)->where($column, $value);
        
        if ($activeOnly) {
            $query->where('active', true);
        }
        
        $exists = $query->exists();
        
        return [
            'valid' => $exists,
            'message' => $exists ? null : "Referenced record not found"
        ];
    }
    
    public function validateEnum($value, $context): array
    {
        $table = $context->get('table');
        $column = $context->get('column');
        
        // Get enum values from database
        $enumValues = $this->getEnumValues($table, $column);
        
        $valid = in_array($value, $enumValues);
        
        return [
            'valid' => $valid,
            'message' => $valid ? null : "Value must be one of: " . implode(', ', $enumValues)
        ];
    }
    
    private function getEnumValues(string $table, string $column): array
    {
        // This is a simplified example - you'd need to implement
        // the actual database-specific enum extraction logic
        $sql = "SHOW COLUMNS FROM {$table} LIKE '{$column}'";
        $result = $this->db->select($sql);
        
        if (empty($result)) {
            return [];
        }
        
        $type = $result[0]['Type'] ?? '';
        if (preg_match('/^enum\((.*)\)$/', $type, $matches)) {
            $values = str_getcsv($matches[1], ',', "'");
            return array_map('trim', $values);
        }
        
        return [];
    }
}
```

## API Validation

Validate API requests and responses:

```php
<?php
// app/Validation/ApiValidator.php

namespace App\Validation;

class ApiValidator
{
    private ValidationPipeline $pipeline;
    private array $schemas;
    
    public function __construct(ValidationPipeline $pipeline, array $schemas)
    {
        $this->pipeline = $pipeline;
        $this->schemas = $schemas;
    }
    
    public function validateRequest(array $data, string $endpoint): ValidationResult
    {
        $schema = $this->schemas[$endpoint]['request'] ?? null;
        
        if (!$schema) {
            return new ValidationResult(true);
        }
        
        return $this->pipeline->validate($data, $schema);
    }
    
    public function validateResponse(array $data, string $endpoint): ValidationResult
    {
        $schema = $this->schemas[$endpoint]['response'] ?? null;
        
        if (!$schema) {
            return new ValidationResult(true);
        }
        
        return $this->pipeline->validate($data, $schema);
    }
    
    public function validateHeaders(array $headers, string $endpoint): ValidationResult
    {
        $requiredHeaders = $this->schemas[$endpoint]['headers'] ?? [];
        $errors = [];
        
        foreach ($requiredHeaders as $header => $config) {
            $value = $headers[$header] ?? null;
            
            if (($config['required'] ?? false) && empty($value)) {
                $errors[$header] = ["Header '{$header}' is required"];
            }
            
            if (!empty($value) && isset($config['pattern'])) {
                if (!preg_match($config['pattern'], $value)) {
                    $errors[$header] = ["Header '{$header}' format is invalid"];
                }
            }
        }
        
        return new ValidationResult(empty($errors), $errors);
    }
    
    public function validateRateLimit(string $ip, string $endpoint): bool
    {
        $limits = $this->schemas[$endpoint]['rate_limit'] ?? null;
        
        if (!$limits) {
            return true;
        }
        
        $key = "rate_limit:{$ip}:{$endpoint}";
        $current = $this->redis->get($key) ?: 0;
        
        if ($current >= $limits['max_requests']) {
            return false;
        }
        
        $this->redis->incr($key);
        $this->redis->expire($key, $limits['window_seconds']);
        
        return true;
    }
}
```

## Security Validation

Implement security-focused validation:

```php
<?php
// app/Validators/SecurityValidator.php

namespace App\Validators;

class SecurityValidator
{
    public function validateXss($value, $context): array
    {
        if (!is_string($value)) {
            return ['valid' => true];
        }
        
        $dangerousPatterns = [
            '/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/mi',
            '/javascript:/i',
            '/on\w+\s*=/i',
            '/<iframe\b[^>]*>/i',
            '/<object\b[^>]*>/i',
            '/<embed\b[^>]*>/i'
        ];
        
        foreach ($dangerousPatterns as $pattern) {
            if (preg_match($pattern, $value)) {
                return [
                    'valid' => false,
                    'message' => 'Potentially dangerous content detected'
                ];
            }
        }
        
        return ['valid' => true];
    }
    
    public function validateSqlInjection($value, $context): array
    {
        if (!is_string($value)) {
            return ['valid' => true];
        }
        
        $sqlPatterns = [
            '/\b(union|select|insert|update|delete|drop|create|alter)\b/i',
            '/[\'";]/',
            '/--/',
            '/\/\*.*\*\//'
        ];
        
        foreach ($sqlPatterns as $pattern) {
            if (preg_match($pattern, $value)) {
                return [
                    'valid' => false,
                    'message' => 'Potentially dangerous SQL content detected'
                ];
            }
        }
        
        return ['valid' => true];
    }
    
    public function validateFileUpload($file, $context): array
    {
        $allowedTypes = $context->get('allowed_types', []);
        $maxSize = $context->get('max_size', 5242880); // 5MB default
        $allowedExtensions = $context->get('allowed_extensions', []);
        
        $errors = [];
        
        // Check file size
        if ($file['size'] > $maxSize) {
            $errors[] = "File size exceeds maximum allowed size";
        }
        
        // Check file type
        $finfo = finfo_open(FILEINFO_MIME_TYPE);
        $mimeType = finfo_file($finfo, $file['tmp_name']);
        finfo_close($finfo);
        
        if (!in_array($mimeType, $allowedTypes)) {
            $errors[] = "File type not allowed";
        }
        
        // Check file extension
        $extension = strtolower(pathinfo($file['name'], PATHINFO_EXTENSION));
        if (!in_array($extension, $allowedExtensions)) {
            $errors[] = "File extension not allowed";
        }
        
        // Check for malicious content
        $content = file_get_contents($file['tmp_name']);
        if ($this->containsMaliciousContent($content)) {
            $errors[] = "File contains potentially malicious content";
        }
        
        return [
            'valid' => empty($errors),
            'message' => empty($errors) ? null : implode(', ', $errors)
        ];
    }
    
    private function containsMaliciousContent(string $content): bool
    {
        $maliciousPatterns = [
            '/<\?php/i',
            '/<script/i',
            '/javascript:/i',
            '/vbscript:/i',
            '/onload=/i',
            '/onerror=/i'
        ];
        
        foreach ($maliciousPatterns as $pattern) {
            if (preg_match($pattern, $content)) {
                return true;
            }
        }
        
        return false;
    }
}
```

## Performance Optimization

Optimize validation performance:

```php
<?php
// app/Validation/ValidationCache.php

namespace App\Validation;

use Redis;

class ValidationCache
{
    private Redis $redis;
    private int $ttl;
    
    public function __construct(Redis $redis, int $ttl = 3600)
    {
        $this->redis = $redis;
        $this->ttl = $ttl;
    }
    
    public function getCachedValidation(string $key, array $data): ?ValidationResult
    {
        $cacheKey = $this->generateCacheKey($key, $data);
        $cached = $this->redis->get($cacheKey);
        
        if ($cached) {
            $decoded = json_decode($cached, true);
            return new ValidationResult(
                $decoded['valid'],
                $decoded['errors'] ?? [],
                $decoded['warnings'] ?? []
            );
        }
        
        return null;
    }
    
    public function cacheValidation(string $key, array $data, ValidationResult $result): void
    {
        $cacheKey = $this->generateCacheKey($key, $data);
        $this->redis->setex($cacheKey, $this->ttl, json_encode($result->toArray()));
    }
    
    private function generateCacheKey(string $key, array $data): string
    {
        $hash = md5(serialize($data));
        return "validation:{$key}:{$hash}";
    }
}

// Optimized validation pipeline with caching
class OptimizedValidationPipeline extends ValidationPipeline
{
    private ValidationCache $cache;
    
    public function __construct(array $config, ValidationCache $cache)
    {
        parent::__construct($config);
        $this->cache = $cache;
    }
    
    public function validate(array $data, string $ruleSet): ValidationResult
    {
        // Check cache first
        $cached = $this->cache->getCachedValidation($ruleSet, $data);
        if ($cached) {
            return $cached;
        }
        
        // Perform validation
        $result = parent::validate($data, $ruleSet);
        
        // Cache result
        $this->cache->cacheValidation($ruleSet, $data, $result);
        
        return $result;
    }
}
```

## Best Practices

Follow these best practices for robust validation:

```php
<?php
// config/validation-best-practices.tsk

validation_best_practices = {
    // Always validate on both client and server
    client_validation = {
        immediate_feedback = true
        progressive_validation = true
        real_time_validation = true
    }
    
    server_validation = {
        always_required = true
        fail_secure = true
        comprehensive_logging = true
    }
    
    // Use appropriate validation levels
    validation_levels = {
        basic = {
            type_checking = true
            required_fields = true
            length_limits = true
        }
        
        intermediate = {
            format_validation = true
            business_rules = true
            cross_field_validation = true
        }
        
        advanced = {
            security_validation = true
            async_validation = true
            performance_optimization = true
        }
    }
    
    // Error handling
    error_handling = {
        user_friendly_messages = true
        detailed_logging = true
        graceful_degradation = true
        retry_mechanisms = true
    }
    
    // Performance considerations
    performance = {
        caching_enabled = true
        lazy_validation = true
        batch_validation = true
        async_validation = true
    }
    
    // Security considerations
    security = {
        input_sanitization = true
        output_encoding = true
        rate_limiting = true
        audit_logging = true
    }
}

// Example usage in PHP
class ValidationBestPractices
{
    public function validateWithBestPractices(array $data, string $ruleSet): ValidationResult
    {
        // 1. Sanitize input first
        $sanitizedData = $this->sanitizeInput($data);
        
        // 2. Perform basic validation
        $basicResult = $this->validateBasic($sanitizedData, $ruleSet);
        if (!$basicResult->isValid()) {
            return $basicResult;
        }
        
        // 3. Perform business logic validation
        $businessResult = $this->validateBusinessLogic($sanitizedData, $ruleSet);
        if (!$businessResult->isValid()) {
            return $businessResult;
        }
        
        // 4. Perform security validation
        $securityResult = $this->validateSecurity($sanitizedData, $ruleSet);
        if (!$securityResult->isValid()) {
            return $securityResult;
        }
        
        // 5. Perform async validation if needed
        $asyncResult = $this->validateAsync($sanitizedData, $ruleSet);
        if (!$asyncResult->isValid()) {
            return $asyncResult;
        }
        
        return new ValidationResult(true);
    }
    
    private function sanitizeInput(array $data): array
    {
        $sanitized = [];
        
        foreach ($data as $key => $value) {
            if (is_string($value)) {
                $sanitized[$key] = htmlspecialchars($value, ENT_QUOTES, 'UTF-8');
            } elseif (is_array($value)) {
                $sanitized[$key] = $this->sanitizeInput($value);
            } else {
                $sanitized[$key] = $value;
            }
        }
        
        return $sanitized;
    }
}
```

This comprehensive guide covers advanced data validation patterns in TuskLang with PHP integration. The validation system is designed to be flexible, secure, and performant while maintaining the rebellious spirit of TuskLang development. 
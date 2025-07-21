# ✅ Advanced Data Validation with TuskLang & PHP

## Introduction
Data validation is the first line of defense in secure, reliable applications. TuskLang and PHP let you build sophisticated validation systems with config-driven rules, custom validators, and validation pipelines that ensure data integrity across your entire application.

## Key Features
- **Custom validation rules**
- **Validation pipelines**
- **Async validation**
- **Database-driven validation**
- **API validation**
- **Security validation**
- **Performance optimization**

## Example: Validation Configuration
```ini
[validation]
rules: @file.read("validation-rules.yaml")
custom_validators: @go("validation.LoadCustomValidators")
async_validation: @go("validation.ConfigureAsyncValidation")
security_checks: @go("validation.ConfigureSecurityChecks")
performance: @go("validation.ConfigurePerformance")
```

## PHP: Validation Engine Implementation
```php
<?php

namespace App\Validation;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class ValidationEngine
{
    private $config;
    private $validators = [];
    private $rules = [];
    private $pipelines = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadValidators();
        $this->loadRules();
        $this->loadPipelines();
    }
    
    public function validate($data, $rules, array $context = [])
    {
        $startTime = microtime(true);
        
        try {
            $result = new ValidationResult();
            
            foreach ($rules as $field => $fieldRules) {
                $fieldValue = $this->getNestedValue($data, $field);
                $fieldResult = $this->validateField($field, $fieldValue, $fieldRules, $context);
                
                if (!$fieldResult->isValid()) {
                    $result->addErrors($field, $fieldResult->getErrors());
                }
            }
            
            $duration = (microtime(true) - $startTime) * 1000;
            
            // Record metrics
            Metrics::record("validation_duration", $duration, [
                "rules_count" => count($rules),
                "success" => $result->isValid()
            ]);
            
            return $result;
            
        } catch (\Exception $e) {
            Metrics::record("validation_errors", 1, [
                "error_type" => get_class($e)
            ]);
            
            throw $e;
        }
    }
    
    public function validateField($field, $value, $rules, array $context = [])
    {
        $result = new ValidationResult();
        
        foreach ($rules as $rule) {
            $ruleName = $rule['rule'];
            $ruleParams = $rule['params'] ?? [];
            
            if (!$this->executeRule($ruleName, $value, $ruleParams, $context)) {
                $result->addError($this->formatErrorMessage($rule, $field, $value));
            }
        }
        
        return $result;
    }
    
    private function executeRule($ruleName, $value, $params, $context)
    {
        if (!isset($this->validators[$ruleName])) {
            throw new \Exception("Unknown validation rule: {$ruleName}");
        }
        
        $validator = $this->validators[$ruleName];
        
        return $validator->validate($value, $params, $context);
    }
    
    private function loadValidators()
    {
        $validators = $this->config->get('validation.validators', []);
        
        foreach ($validators as $name => $config) {
            $this->validators[$name] = new $config['class']();
        }
        
        // Load custom validators
        $customValidators = $this->config->get('validation.custom_validators', []);
        
        foreach ($customValidators as $name => $config) {
            $this->validators[$name] = new $config['class']();
        }
    }
    
    private function loadRules()
    {
        $this->rules = $this->config->get('validation.rules', []);
    }
    
    private function loadPipelines()
    {
        $this->pipelines = $this->config->get('validation.pipelines', []);
    }
    
    private function getNestedValue($data, $path)
    {
        $keys = explode('.', $path);
        $value = $data;
        
        foreach ($keys as $key) {
            if (!isset($value[$key])) {
                return null;
            }
            $value = $value[$key];
        }
        
        return $value;
    }
    
    private function formatErrorMessage($rule, $field, $value)
    {
        $message = $rule['message'] ?? "Validation failed for field {$field}";
        
        return str_replace(
            ['{field}', '{value}'],
            [$field, $value],
            $message
        );
    }
}

class ValidationResult
{
    private $errors = [];
    private $warnings = [];
    
    public function addError($error)
    {
        $this->errors[] = $error;
    }
    
    public function addErrors($field, $errors)
    {
        foreach ($errors as $error) {
            $this->errors[] = "{$field}: {$error}";
        }
    }
    
    public function addWarning($warning)
    {
        $this->warnings[] = $warning;
    }
    
    public function isValid()
    {
        return empty($this->errors);
    }
    
    public function getErrors()
    {
        return $this->errors;
    }
    
    public function getWarnings()
    {
        return $this->warnings;
    }
    
    public function toArray()
    {
        return [
            'valid' => $this->isValid(),
            'errors' => $this->errors,
            'warnings' => $this->warnings
        ];
    }
}
```

## Custom Validators
```php
<?php

namespace App\Validation\Validators;

use TuskLang\Config;

interface ValidatorInterface
{
    public function validate($value, array $params = [], array $context = []);
}

class RequiredValidator implements ValidatorInterface
{
    public function validate($value, array $params = [], array $context = [])
    {
        return $value !== null && $value !== '';
    }
}

class EmailValidator implements ValidatorInterface
{
    public function validate($value, array $params = [], array $context = [])
    {
        return filter_var($value, FILTER_VALIDATE_EMAIL) !== false;
    }
}

class MinLengthValidator implements ValidatorInterface
{
    public function validate($value, array $params = [], array $context = [])
    {
        $minLength = $params['min'] ?? 0;
        return strlen($value) >= $minLength;
    }
}

class MaxLengthValidator implements ValidatorInterface
{
    public function validate($value, array $params = [], array $context = [])
    {
        $maxLength = $params['max'] ?? PHP_INT_MAX;
        return strlen($value) <= $maxLength;
    }
}

class RegexValidator implements ValidatorInterface
{
    public function validate($value, array $params = [], array $context = [])
    {
        $pattern = $params['pattern'] ?? '';
        return preg_match($pattern, $value) === 1;
    }
}

class UniqueValidator implements ValidatorInterface
{
    private $pdo;
    
    public function __construct()
    {
        $this->pdo = new PDO(Env::get('DB_DSN'));
    }
    
    public function validate($value, array $params = [], array $context = [])
    {
        $table = $params['table'];
        $column = $params['column'];
        $excludeId = $params['exclude_id'] ?? null;
        
        $sql = "SELECT COUNT(*) FROM {$table} WHERE {$column} = ?";
        $bindings = [$value];
        
        if ($excludeId) {
            $sql .= " AND id != ?";
            $bindings[] = $excludeId;
        }
        
        $stmt = $this->pdo->prepare($sql);
        $stmt->execute($bindings);
        
        return $stmt->fetchColumn() === 0;
    }
}

class CustomValidator implements ValidatorInterface
{
    private $config;
    
    public function __construct()
    {
        $this->config = new Config();
    }
    
    public function validate($value, array $params = [], array $context = [])
    {
        $validatorName = $params['validator'];
        $validatorConfig = $this->config->get("validation.custom.{$validatorName}");
        
        if (!$validatorConfig) {
            throw new \Exception("Custom validator not found: {$validatorName}");
        }
        
        $callback = $validatorConfig['callback'];
        
        return call_user_func($callback, $value, $params, $context);
    }
}
```

## Validation Pipelines
```php
<?php

namespace App\Validation\Pipelines;

use TuskLang\Config;

class ValidationPipeline
{
    private $config;
    private $engine;
    private $pipelines = [];
    
    public function __construct(ValidationEngine $engine)
    {
        $this->config = new Config();
        $this->engine = $engine;
        $this->loadPipelines();
    }
    
    public function run($pipelineName, $data, array $context = [])
    {
        if (!isset($this->pipelines[$pipelineName])) {
            throw new \Exception("Pipeline not found: {$pipelineName}");
        }
        
        $pipeline = $this->pipelines[$pipelineName];
        $result = new ValidationResult();
        
        foreach ($pipeline['steps'] as $step) {
            $stepResult = $this->executeStep($step, $data, $context);
            
            if (!$stepResult->isValid()) {
                $result->merge($stepResult);
                
                // Check if we should stop on first error
                if ($step['stop_on_error'] ?? false) {
                    break;
                }
            }
        }
        
        return $result;
    }
    
    private function executeStep($step, $data, $context)
    {
        switch ($step['type']) {
            case 'validation':
                return $this->engine->validate($data, $step['rules'], $context);
                
            case 'transformation':
                return $this->transformData($data, $step['transformer']);
                
            case 'sanitization':
                return $this->sanitizeData($data, $step['sanitizer']);
                
            case 'custom':
                return $this->executeCustomStep($step, $data, $context);
                
            default:
                throw new \Exception("Unknown step type: {$step['type']}");
        }
    }
    
    private function transformData($data, $transformer)
    {
        $result = new ValidationResult();
        
        // Apply transformation
        $transformedData = call_user_func($transformer, $data);
        
        $result->setData($transformedData);
        return $result;
    }
    
    private function sanitizeData($data, $sanitizer)
    {
        $result = new ValidationResult();
        
        // Apply sanitization
        $sanitizedData = call_user_func($sanitizer, $data);
        
        $result->setData($sanitizedData);
        return $result;
    }
    
    private function executeCustomStep($step, $data, $context)
    {
        $callback = $step['callback'];
        
        return call_user_func($callback, $data, $context);
    }
    
    private function loadPipelines()
    {
        $this->pipelines = $this->config->get('validation.pipelines', []);
    }
}

class UserRegistrationPipeline
{
    public function __construct()
    {
        $this->pipeline = [
            'steps' => [
                [
                    'type' => 'sanitization',
                    'sanitizer' => [$this, 'sanitizeInput']
                ],
                [
                    'type' => 'validation',
                    'rules' => [
                        'email' => [
                            ['rule' => 'required'],
                            ['rule' => 'email']
                        ],
                        'password' => [
                            ['rule' => 'required'],
                            ['rule' => 'min_length', 'params' => ['min' => 8]],
                            ['rule' => 'regex', 'params' => ['pattern' => '/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/']]
                        ],
                        'username' => [
                            ['rule' => 'required'],
                            ['rule' => 'min_length', 'params' => ['min' => 3]],
                            ['rule' => 'max_length', 'params' => ['max' => 20]],
                            ['rule' => 'unique', 'params' => ['table' => 'users', 'column' => 'username']]
                        ]
                    ]
                ],
                [
                    'type' => 'transformation',
                    'transformer' => [$this, 'transformData']
                ]
            ]
        ];
    }
    
    public function sanitizeInput($data)
    {
        return [
            'email' => filter_var($data['email'], FILTER_SANITIZE_EMAIL),
            'username' => filter_var($data['username'], FILTER_SANITIZE_STRING),
            'password' => $data['password'] // Don't sanitize passwords
        ];
    }
    
    public function transformData($data)
    {
        return [
            'email' => strtolower($data['email']),
            'username' => strtolower($data['username']),
            'password' => password_hash($data['password'], PASSWORD_DEFAULT)
        ];
    }
}
```

## Async Validation
```php
<?php

namespace App\Validation\Async;

use TuskLang\Config;

class AsyncValidator
{
    private $config;
    private $queue;
    private $validators = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->queue = new MessageQueue();
        $this->loadAsyncValidators();
    }
    
    public function validateAsync($data, $rules, array $context = [])
    {
        $validationId = uniqid('validation_');
        
        // Queue validation job
        $this->queue->publish('validation', [
            'id' => $validationId,
            'data' => $data,
            'rules' => $rules,
            'context' => $context,
            'timestamp' => date('c')
        ]);
        
        return $validationId;
    }
    
    public function getValidationResult($validationId)
    {
        $result = $this->queue->get("validation_result:{$validationId}");
        
        if ($result) {
            return json_decode($result, true);
        }
        
        return ['status' => 'pending'];
    }
    
    public function processValidationJob($job)
    {
        $validationId = $job['id'];
        $data = $job['data'];
        $rules = $job['rules'];
        $context = $job['context'];
        
        try {
            $result = $this->validate($data, $rules, $context);
            
            // Store result
            $this->queue->set("validation_result:{$validationId}", json_encode([
                'status' => 'completed',
                'result' => $result->toArray(),
                'completed_at' => date('c')
            ]));
            
        } catch (\Exception $e) {
            $this->queue->set("validation_result:{$validationId}", json_encode([
                'status' => 'failed',
                'error' => $e->getMessage(),
                'failed_at' => date('c')
            ]));
        }
    }
    
    private function validate($data, $rules, $context)
    {
        $engine = new ValidationEngine();
        return $engine->validate($data, $rules, $context);
    }
    
    private function loadAsyncValidators()
    {
        $this->validators = $this->config->get('validation.async_validators', []);
    }
}
```

## Security Validation
```php
<?php

namespace App\Validation\Security;

use TuskLang\Config;

class SecurityValidator
{
    private $config;
    private $threatPatterns = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadThreatPatterns();
    }
    
    public function validateSecurity($data, array $context = [])
    {
        $result = new ValidationResult();
        
        foreach ($data as $field => $value) {
            if (is_string($value)) {
                $securityIssues = $this->checkForThreats($value, $field);
                
                foreach ($securityIssues as $issue) {
                    $result->addError("Security issue in {$field}: {$issue}");
                }
            }
        }
        
        return $result;
    }
    
    private function checkForThreats($value, $field)
    {
        $issues = [];
        
        foreach ($this->threatPatterns as $pattern) {
            if (preg_match($pattern['regex'], $value)) {
                $issues[] = $pattern['description'];
            }
        }
        
        return $issues;
    }
    
    private function loadThreatPatterns()
    {
        $this->threatPatterns = [
            [
                'name' => 'sql_injection',
                'regex' => '/(\b(union|select|insert|update|delete|drop|create|alter)\b)/i',
                'description' => 'Potential SQL injection detected'
            ],
            [
                'name' => 'xss',
                'regex' => '/(<script|javascript:|on\w+\s*=)/i',
                'description' => 'Potential XSS attack detected'
            ],
            [
                'name' => 'path_traversal',
                'regex' => '/(\.\.\/|\.\.\\\\)/',
                'description' => 'Potential path traversal attack detected'
            ],
            [
                'name' => 'command_injection',
                'regex' => '/(\b(cat|ls|rm|chmod|chown|wget|curl|nc|telnet)\b)/i',
                'description' => 'Potential command injection detected'
            ]
        ];
    }
    
    public function sanitizeInput($data)
    {
        if (is_string($data)) {
            return htmlspecialchars($data, ENT_QUOTES, 'UTF-8');
        }
        
        if (is_array($data)) {
            return array_map([$this, 'sanitizeInput'], $data);
        }
        
        return $data;
    }
    
    public function validateFileUpload($file)
    {
        $result = new ValidationResult();
        
        // Check file size
        $maxSize = $this->config->get('validation.file.max_size', 10 * 1024 * 1024); // 10MB
        if ($file['size'] > $maxSize) {
            $result->addError("File size exceeds maximum allowed size");
        }
        
        // Check file type
        $allowedTypes = $this->config->get('validation.file.allowed_types', ['jpg', 'png', 'pdf']);
        $extension = strtolower(pathinfo($file['name'], PATHINFO_EXTENSION));
        
        if (!in_array($extension, $allowedTypes)) {
            $result->addError("File type not allowed");
        }
        
        // Check for malicious content
        $content = file_get_contents($file['tmp_name']);
        if ($this->containsMaliciousContent($content)) {
            $result->addError("File contains potentially malicious content");
        }
        
        return $result;
    }
    
    private function containsMaliciousContent($content)
    {
        $maliciousPatterns = [
            '/<\?php/i',
            '/<script/i',
            '/javascript:/i'
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

## Best Practices
- **Validate all input data**
- **Use custom validators for business logic**
- **Implement validation pipelines**
- **Use async validation for heavy operations**
- **Implement security validation**
- **Cache validation results**

## Performance Optimization
- **Use validation caching**
- **Implement lazy validation**
- **Use async validation for heavy checks**
- **Optimize validation rules**

## Security Considerations
- **Validate file uploads**
- **Check for malicious content**
- **Implement rate limiting**
- **Use secure validation rules**

## Troubleshooting
- **Monitor validation performance**
- **Check validation error logs**
- **Verify validation rules**
- **Test edge cases**

## Conclusion
TuskLang + PHP = validation that's powerful, secure, and performant. Validate everything, trust nothing. 
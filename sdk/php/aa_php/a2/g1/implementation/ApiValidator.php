<?php

namespace TuskLang\Communication\Http;

/**
 * API Request Validator
 */
class ApiValidator
{
    /**
     * Validate request against rules
     */
    public function validate(ApiRequest $request, array $rules): array
    {
        $errors = [];
        $data = array_merge(
            $request->getQueryParams(),
            (array) $request->getData(),
            $request->getRouteParams()
        );
        
        foreach ($rules as $field => $ruleSet) {
            $fieldErrors = $this->validateField($field, $data[$field] ?? null, $ruleSet);
            if (!empty($fieldErrors)) {
                $errors[$field] = $fieldErrors;
            }
        }
        
        return $errors;
    }

    /**
     * Validate individual field
     */
    private function validateField(string $field, mixed $value, string|array $rules): array
    {
        $errors = [];
        $ruleList = is_string($rules) ? explode('|', $rules) : $rules;
        
        foreach ($ruleList as $rule) {
            $error = $this->applyRule($field, $value, $rule);
            if ($error) {
                $errors[] = $error;
            }
        }
        
        return $errors;
    }

    /**
     * Apply validation rule
     */
    private function applyRule(string $field, mixed $value, string $rule): ?string
    {
        // Parse rule and parameters
        $parts = explode(':', $rule, 2);
        $ruleName = $parts[0];
        $params = isset($parts[1]) ? explode(',', $parts[1]) : [];
        
        switch ($ruleName) {
            case 'required':
                return $this->validateRequired($field, $value);
                
            case 'string':
                return $this->validateString($field, $value);
                
            case 'integer':
            case 'int':
                return $this->validateInteger($field, $value);
                
            case 'numeric':
                return $this->validateNumeric($field, $value);
                
            case 'email':
                return $this->validateEmail($field, $value);
                
            case 'url':
                return $this->validateUrl($field, $value);
                
            case 'min':
                return $this->validateMin($field, $value, $params[0] ?? 0);
                
            case 'max':
                return $this->validateMax($field, $value, $params[0] ?? 100);
                
            case 'between':
                return $this->validateBetween($field, $value, $params[0] ?? 0, $params[1] ?? 100);
                
            case 'in':
                return $this->validateIn($field, $value, $params);
                
            case 'not_in':
                return $this->validateNotIn($field, $value, $params);
                
            case 'regex':
                return $this->validateRegex($field, $value, $params[0] ?? '');
                
            case 'alpha':
                return $this->validateAlpha($field, $value);
                
            case 'alpha_num':
                return $this->validateAlphaNum($field, $value);
                
            case 'alpha_dash':
                return $this->validateAlphaDash($field, $value);
                
            case 'json':
                return $this->validateJson($field, $value);
                
            case 'date':
                return $this->validateDate($field, $value);
                
            case 'boolean':
                return $this->validateBoolean($field, $value);
                
            case 'array':
                return $this->validateArray($field, $value);
                
            case 'file':
                return $this->validateFile($field, $value);
                
            case 'image':
                return $this->validateImage($field, $value);
                
            default:
                return null;
        }
    }

    private function validateRequired(string $field, mixed $value): ?string
    {
        if ($value === null || $value === '' || (is_array($value) && empty($value))) {
            return "The {$field} field is required.";
        }
        return null;
    }

    private function validateString(string $field, mixed $value): ?string
    {
        if ($value !== null && !is_string($value)) {
            return "The {$field} field must be a string.";
        }
        return null;
    }

    private function validateInteger(string $field, mixed $value): ?string
    {
        if ($value !== null && !is_int($value) && !ctype_digit((string) $value)) {
            return "The {$field} field must be an integer.";
        }
        return null;
    }

    private function validateNumeric(string $field, mixed $value): ?string
    {
        if ($value !== null && !is_numeric($value)) {
            return "The {$field} field must be numeric.";
        }
        return null;
    }

    private function validateEmail(string $field, mixed $value): ?string
    {
        if ($value !== null && !filter_var($value, FILTER_VALIDATE_EMAIL)) {
            return "The {$field} field must be a valid email address.";
        }
        return null;
    }

    private function validateUrl(string $field, mixed $value): ?string
    {
        if ($value !== null && !filter_var($value, FILTER_VALIDATE_URL)) {
            return "The {$field} field must be a valid URL.";
        }
        return null;
    }

    private function validateMin(string $field, mixed $value, mixed $min): ?string
    {
        if ($value === null) return null;
        
        if (is_string($value) && strlen($value) < (int) $min) {
            return "The {$field} field must be at least {$min} characters.";
        }
        
        if (is_numeric($value) && $value < $min) {
            return "The {$field} field must be at least {$min}.";
        }
        
        return null;
    }

    private function validateMax(string $field, mixed $value, mixed $max): ?string
    {
        if ($value === null) return null;
        
        if (is_string($value) && strlen($value) > (int) $max) {
            return "The {$field} field must not be greater than {$max} characters.";
        }
        
        if (is_numeric($value) && $value > $max) {
            return "The {$field} field must not be greater than {$max}.";
        }
        
        return null;
    }

    private function validateBetween(string $field, mixed $value, mixed $min, mixed $max): ?string
    {
        $minError = $this->validateMin($field, $value, $min);
        if ($minError) return $minError;
        
        $maxError = $this->validateMax($field, $value, $max);
        if ($maxError) return $maxError;
        
        return null;
    }

    private function validateIn(string $field, mixed $value, array $options): ?string
    {
        if ($value !== null && !in_array($value, $options)) {
            $optionsList = implode(', ', $options);
            return "The selected {$field} is invalid. Must be one of: {$optionsList}.";
        }
        return null;
    }

    private function validateNotIn(string $field, mixed $value, array $options): ?string
    {
        if ($value !== null && in_array($value, $options)) {
            return "The selected {$field} is invalid.";
        }
        return null;
    }

    private function validateRegex(string $field, mixed $value, string $pattern): ?string
    {
        if ($value !== null && !preg_match($pattern, (string) $value)) {
            return "The {$field} field format is invalid.";
        }
        return null;
    }

    private function validateAlpha(string $field, mixed $value): ?string
    {
        if ($value !== null && !ctype_alpha((string) $value)) {
            return "The {$field} field must only contain letters.";
        }
        return null;
    }

    private function validateAlphaNum(string $field, mixed $value): ?string
    {
        if ($value !== null && !ctype_alnum((string) $value)) {
            return "The {$field} field must only contain letters and numbers.";
        }
        return null;
    }

    private function validateAlphaDash(string $field, mixed $value): ?string
    {
        if ($value !== null && !preg_match('/^[a-zA-Z0-9_-]+$/', (string) $value)) {
            return "The {$field} field must only contain letters, numbers, dashes, and underscores.";
        }
        return null;
    }

    private function validateJson(string $field, mixed $value): ?string
    {
        if ($value !== null) {
            json_decode((string) $value);
            if (json_last_error() !== JSON_ERROR_NONE) {
                return "The {$field} field must be valid JSON.";
            }
        }
        return null;
    }

    private function validateDate(string $field, mixed $value): ?string
    {
        if ($value !== null && !strtotime((string) $value)) {
            return "The {$field} field must be a valid date.";
        }
        return null;
    }

    private function validateBoolean(string $field, mixed $value): ?string
    {
        if ($value !== null && !is_bool($value) && !in_array($value, [0, 1, '0', '1', 'true', 'false'])) {
            return "The {$field} field must be true or false.";
        }
        return null;
    }

    private function validateArray(string $field, mixed $value): ?string
    {
        if ($value !== null && !is_array($value)) {
            return "The {$field} field must be an array.";
        }
        return null;
    }

    private function validateFile(string $field, mixed $value): ?string
    {
        if ($value !== null && (!is_array($value) || !isset($value['tmp_name']))) {
            return "The {$field} field must be a file.";
        }
        return null;
    }

    private function validateImage(string $field, mixed $value): ?string
    {
        $fileError = $this->validateFile($field, $value);
        if ($fileError) return $fileError;
        
        if ($value !== null) {
            $imageInfo = getimagesize($value['tmp_name']);
            if (!$imageInfo) {
                return "The {$field} field must be an image.";
            }
        }
        
        return null;
    }
} 
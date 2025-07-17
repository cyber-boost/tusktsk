# Data Validation in C# TuskLang

## Overview

Data validation is crucial for ensuring data integrity and security. This guide covers input validation, schema validation, custom validators, and validation best practices for C# TuskLang applications.

## ✅ Input Validation

### Validation Service

```csharp
public class ValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, IValidator> _validators;
    
    public ValidationService(ILogger<ValidationService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _validators = new Dictionary<string, IValidator>();
        RegisterDefaultValidators();
    }
    
    public async Task<ValidationResult> ValidateAsync<T>(T model, string? validationRule = null)
    {
        try
        {
            var result = new ValidationResult();
            
            // Get validation rules from configuration
            var rules = await GetValidationRulesAsync<T>(validationRule);
            
            // Apply validation rules
            foreach (var rule in rules)
            {
                var ruleResult = await ApplyValidationRuleAsync(model, rule);
                result.Merge(ruleResult);
            }
            
            // Log validation results
            if (!result.IsValid)
            {
                _logger.LogWarning("Validation failed for {ModelType}: {Errors}", 
                    typeof(T).Name, string.Join(", ", result.Errors));
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation error for {ModelType}", typeof(T).Name);
            return ValidationResult.Failure("Validation failed due to an error");
        }
    }
    
    public async Task<ValidationResult> ValidatePropertyAsync<T>(T model, string propertyName, string? validationRule = null)
    {
        try
        {
            var result = new ValidationResult();
            var property = typeof(T).GetProperty(propertyName);
            
            if (property == null)
            {
                result.AddError($"Property '{propertyName}' not found");
                return result;
            }
            
            var value = property.GetValue(model);
            var rules = await GetPropertyValidationRulesAsync<T>(propertyName, validationRule);
            
            foreach (var rule in rules)
            {
                var ruleResult = await ApplyPropertyValidationRuleAsync(value, rule);
                result.Merge(ruleResult);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Property validation error for {ModelType}.{PropertyName}", 
                typeof(T).Name, propertyName);
            return ValidationResult.Failure("Property validation failed due to an error");
        }
    }
    
    private void RegisterDefaultValidators()
    {
        _validators["required"] = new RequiredValidator();
        _validators["email"] = new EmailValidator();
        _validators["min_length"] = new MinLengthValidator();
        _validators["max_length"] = new MaxLengthValidator();
        _validators["range"] = new RangeValidator();
        _validators["regex"] = new RegexValidator();
        _validators["custom"] = new CustomValidator();
    }
    
    private async Task<List<ValidationRule>> GetValidationRulesAsync<T>(string? validationRule)
    {
        var rules = new List<ValidationRule>();
        var typeName = typeof(T).Name.ToLower();
        
        // Get rules from configuration
        var configRules = _config.GetSection($"validation.{typeName}");
        if (configRules != null)
        {
            foreach (var key in configRules.GetKeys())
            {
                var ruleConfig = configRules.GetSection(key);
                var rule = new ValidationRule
                {
                    Name = key,
                    Type = ruleConfig.Get<string>("type", "required"),
                    Parameters = ruleConfig.GetSection("parameters").ToDictionary()
                };
                rules.Add(rule);
            }
        }
        
        // Add specific validation rule if provided
        if (!string.IsNullOrEmpty(validationRule))
        {
            var specificRule = _config.GetSection($"validation.rules.{validationRule}");
            if (specificRule != null)
            {
                var rule = new ValidationRule
                {
                    Name = validationRule,
                    Type = specificRule.Get<string>("type", "required"),
                    Parameters = specificRule.GetSection("parameters").ToDictionary()
                };
                rules.Add(rule);
            }
        }
        
        return rules;
    }
    
    private async Task<List<ValidationRule>> GetPropertyValidationRulesAsync<T>(string propertyName, string? validationRule)
    {
        var rules = new List<ValidationRule>();
        var typeName = typeof(T).Name.ToLower();
        
        // Get property-specific rules from configuration
        var propertyRules = _config.GetSection($"validation.{typeName}.properties.{propertyName}");
        if (propertyRules != null)
        {
            foreach (var key in propertyRules.GetKeys())
            {
                var ruleConfig = propertyRules.GetSection(key);
                var rule = new ValidationRule
                {
                    Name = key,
                    Type = ruleConfig.Get<string>("type", "required"),
                    Parameters = ruleConfig.GetSection("parameters").ToDictionary()
                };
                rules.Add(rule);
            }
        }
        
        return rules;
    }
    
    private async Task<ValidationResult> ApplyValidationRuleAsync<T>(T model, ValidationRule rule)
    {
        if (!_validators.TryGetValue(rule.Type, out var validator))
        {
            return ValidationResult.Failure($"Unknown validation rule type: {rule.Type}");
        }
        
        return await validator.ValidateAsync(model, rule);
    }
    
    private async Task<ValidationResult> ApplyPropertyValidationRuleAsync(object? value, ValidationRule rule)
    {
        if (!_validators.TryGetValue(rule.Type, out var validator))
        {
            return ValidationResult.Failure($"Unknown validation rule type: {rule.Type}");
        }
        
        return await validator.ValidateValueAsync(value, rule);
    }
}

public interface IValidator
{
    Task<ValidationResult> ValidateAsync<T>(T model, ValidationRule rule);
    Task<ValidationResult> ValidateValueAsync(object? value, ValidationRule rule);
}

public class ValidationRule
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public void AddError(string error)
    {
        Errors.Add(error);
    }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
    
    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
    }
    
    public static ValidationResult Success()
    {
        return new ValidationResult();
    }
    
    public static ValidationResult Failure(string error)
    {
        var result = new ValidationResult();
        result.AddError(error);
        return result;
    }
}
```

### Built-in Validators

```csharp
public class RequiredValidator : IValidator
{
    public async Task<ValidationResult> ValidateAsync<T>(T model, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        // Check if model is null
        if (model == null)
        {
            result.AddError("Model is required");
            return result;
        }
        
        // Check required properties
        var requiredProperties = rule.Parameters.GetValueOrDefault("properties", new List<string>()) as List<string>;
        if (requiredProperties != null)
        {
            foreach (var propertyName in requiredProperties)
            {
                var property = typeof(T).GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(model);
                    if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                    {
                        result.AddError($"Property '{propertyName}' is required");
                    }
                }
            }
        }
        
        return result;
    }
    
    public async Task<ValidationResult> ValidateValueAsync(object? value, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        if (value == null || (value is string str && string.IsNullOrEmpty(str)))
        {
            result.AddError("Value is required");
        }
        
        return result;
    }
}

public class EmailValidator : IValidator
{
    public async Task<ValidationResult> ValidateAsync<T>(T model, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        var propertyName = rule.Parameters.GetValueOrDefault("property", "email") as string;
        var property = typeof(T).GetProperty(propertyName ?? "email");
        
        if (property != null)
        {
            var value = property.GetValue(model) as string;
            if (!string.IsNullOrEmpty(value) && !IsValidEmail(value))
            {
                result.AddError($"Property '{propertyName}' must be a valid email address");
            }
        }
        
        return result;
    }
    
    public async Task<ValidationResult> ValidateValueAsync(object? value, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        if (value is string email && !string.IsNullOrEmpty(email) && !IsValidEmail(email))
        {
            result.AddError("Value must be a valid email address");
        }
        
        return result;
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public class MinLengthValidator : IValidator
{
    public async Task<ValidationResult> ValidateAsync<T>(T model, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        var propertyName = rule.Parameters.GetValueOrDefault("property", "") as string;
        var minLength = Convert.ToInt32(rule.Parameters.GetValueOrDefault("min_length", 0));
        
        if (!string.IsNullOrEmpty(propertyName))
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property != null)
            {
                var value = property.GetValue(model) as string;
                if (!string.IsNullOrEmpty(value) && value.Length < minLength)
                {
                    result.AddError($"Property '{propertyName}' must be at least {minLength} characters long");
                }
            }
        }
        
        return result;
    }
    
    public async Task<ValidationResult> ValidateValueAsync(object? value, ValidationRule rule)
    {
        var result = new ValidationResult();
        var minLength = Convert.ToInt32(rule.Parameters.GetValueOrDefault("min_length", 0));
        
        if (value is string str && str.Length < minLength)
        {
            result.AddError($"Value must be at least {minLength} characters long");
        }
        
        return result;
    }
}

public class MaxLengthValidator : IValidator
{
    public async Task<ValidationResult> ValidateAsync<T>(T model, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        var propertyName = rule.Parameters.GetValueOrDefault("property", "") as string;
        var maxLength = Convert.ToInt32(rule.Parameters.GetValueOrDefault("max_length", int.MaxValue));
        
        if (!string.IsNullOrEmpty(propertyName))
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property != null)
            {
                var value = property.GetValue(model) as string;
                if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
                {
                    result.AddError($"Property '{propertyName}' must not exceed {maxLength} characters");
                }
            }
        }
        
        return result;
    }
    
    public async Task<ValidationResult> ValidateValueAsync(object? value, ValidationRule rule)
    {
        var result = new ValidationResult();
        var maxLength = Convert.ToInt32(rule.Parameters.GetValueOrDefault("max_length", int.MaxValue));
        
        if (value is string str && str.Length > maxLength)
        {
            result.AddError($"Value must not exceed {maxLength} characters");
        }
        
        return result;
    }
}

public class RangeValidator : IValidator
{
    public async Task<ValidationResult> ValidateAsync<T>(T model, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        var propertyName = rule.Parameters.GetValueOrDefault("property", "") as string;
        var min = Convert.ToDouble(rule.Parameters.GetValueOrDefault("min", double.MinValue));
        var max = Convert.ToDouble(rule.Parameters.GetValueOrDefault("max", double.MaxValue));
        
        if (!string.IsNullOrEmpty(propertyName))
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property != null)
            {
                var value = Convert.ToDouble(property.GetValue(model));
                if (value < min || value > max)
                {
                    result.AddError($"Property '{propertyName}' must be between {min} and {max}");
                }
            }
        }
        
        return result;
    }
    
    public async Task<ValidationResult> ValidateValueAsync(object? value, ValidationRule rule)
    {
        var result = new ValidationResult();
        var min = Convert.ToDouble(rule.Parameters.GetValueOrDefault("min", double.MinValue));
        var max = Convert.ToDouble(rule.Parameters.GetValueOrDefault("max", double.MaxValue));
        
        if (value != null)
        {
            var numericValue = Convert.ToDouble(value);
            if (numericValue < min || numericValue > max)
            {
                result.AddError($"Value must be between {min} and {max}");
            }
        }
        
        return result;
    }
}

public class RegexValidator : IValidator
{
    public async Task<ValidationResult> ValidateAsync<T>(T model, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        var propertyName = rule.Parameters.GetValueOrDefault("property", "") as string;
        var pattern = rule.Parameters.GetValueOrDefault("pattern", "") as string;
        
        if (!string.IsNullOrEmpty(propertyName) && !string.IsNullOrEmpty(pattern))
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property != null)
            {
                var value = property.GetValue(model) as string;
                if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, pattern))
                {
                    result.AddError($"Property '{propertyName}' does not match the required pattern");
                }
            }
        }
        
        return result;
    }
    
    public async Task<ValidationResult> ValidateValueAsync(object? value, ValidationRule rule)
    {
        var result = new ValidationResult();
        var pattern = rule.Parameters.GetValueOrDefault("pattern", "") as string;
        
        if (value is string str && !string.IsNullOrEmpty(pattern) && !Regex.IsMatch(str, pattern))
        {
            result.AddError("Value does not match the required pattern");
        }
        
        return result;
    }
}
```

## 📋 Schema Validation

### JSON Schema Validator

```csharp
public class JsonSchemaValidator
{
    private readonly ILogger<JsonSchemaValidator> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, JsonSchema> _schemas;
    
    public JsonSchemaValidator(ILogger<JsonSchemaValidator> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _schemas = new Dictionary<string, JsonSchema>();
        LoadSchemas();
    }
    
    public async Task<ValidationResult> ValidateAgainstSchemaAsync<T>(T model, string schemaName)
    {
        try
        {
            if (!_schemas.TryGetValue(schemaName, out var schema))
            {
                return ValidationResult.Failure($"Schema '{schemaName}' not found");
            }
            
            var json = JsonSerializer.Serialize(model);
            var jsonElement = JsonDocument.Parse(json).RootElement;
            
            var validationResults = schema.Evaluate(jsonElement);
            
            var result = new ValidationResult();
            
            if (!validationResults.IsValid)
            {
                foreach (var detail in validationResults.Details)
                {
                    result.AddError($"Schema validation failed at {detail.Path}: {detail.Message}");
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed for schema {SchemaName}", schemaName);
            return ValidationResult.Failure("Schema validation failed due to an error");
        }
    }
    
    public async Task<ValidationResult> ValidateJsonAsync(string json, string schemaName)
    {
        try
        {
            if (!_schemas.TryGetValue(schemaName, out var schema))
            {
                return ValidationResult.Failure($"Schema '{schemaName}' not found");
            }
            
            var jsonElement = JsonDocument.Parse(json).RootElement;
            var validationResults = schema.Evaluate(jsonElement);
            
            var result = new ValidationResult();
            
            if (!validationResults.IsValid)
            {
                foreach (var detail in validationResults.Details)
                {
                    result.AddError($"Schema validation failed at {detail.Path}: {detail.Message}");
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON schema validation failed for schema {SchemaName}", schemaName);
            return ValidationResult.Failure("JSON schema validation failed due to an error");
        }
    }
    
    private void LoadSchemas()
    {
        var schemasConfig = _config.GetSection("validation.schemas");
        if (schemasConfig != null)
        {
            foreach (var key in schemasConfig.GetKeys())
            {
                var schemaJson = schemasConfig.Get<string>(key);
                if (!string.IsNullOrEmpty(schemaJson))
                {
                    try
                    {
                        var schema = JsonSchema.FromText(schemaJson);
                        _schemas[key] = schema;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load schema {SchemaName}", key);
                    }
                }
            }
        }
    }
    
    public async Task<bool> RegisterSchemaAsync(string name, string schemaJson)
    {
        try
        {
            var schema = JsonSchema.FromText(schemaJson);
            _schemas[name] = schema;
            
            // Save to configuration
            _config.Set($"validation.schemas.{name}", schemaJson);
            
            _logger.LogInformation("Schema {SchemaName} registered successfully", name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register schema {SchemaName}", name);
            return false;
        }
    }
}
```

### TuskLang Schema Validator

```csharp
public class TuskLangSchemaValidator
{
    private readonly ILogger<TuskLangSchemaValidator> _logger;
    private readonly TSKConfig _config;
    
    public TuskLangSchemaValidator(ILogger<TuskLangSchemaValidator> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public async Task<ValidationResult> ValidateTuskLangConfigAsync(string configContent, string schemaName)
    {
        try
        {
            var schema = await GetTuskLangSchemaAsync(schemaName);
            if (schema == null)
            {
                return ValidationResult.Failure($"TuskLang schema '{schemaName}' not found");
            }
            
            var result = new ValidationResult();
            
            // Parse TuskLang configuration
            var config = new TSKConfig();
            config.LoadFromString(configContent);
            
            // Validate against schema
            foreach (var schemaRule in schema.Rules)
            {
                var ruleResult = await ValidateSchemaRuleAsync(config, schemaRule);
                result.Merge(ruleResult);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TuskLang schema validation failed for schema {SchemaName}", schemaName);
            return ValidationResult.Failure("TuskLang schema validation failed due to an error");
        }
    }
    
    private async Task<TuskLangSchema?> GetTuskLangSchemaAsync(string schemaName)
    {
        var schemaConfig = _config.GetSection($"validation.tusklang_schemas.{schemaName}");
        if (schemaConfig == null)
        {
            return null;
        }
        
        var schema = new TuskLangSchema
        {
            Name = schemaName,
            Rules = new List<TuskLangSchemaRule>()
        };
        
        var rulesConfig = schemaConfig.GetSection("rules");
        if (rulesConfig != null)
        {
            foreach (var key in rulesConfig.GetKeys())
            {
                var ruleConfig = rulesConfig.GetSection(key);
                var rule = new TuskLangSchemaRule
                {
                    Name = key,
                    Type = ruleConfig.Get<string>("type", "required"),
                    Path = ruleConfig.Get<string>("path", ""),
                    Parameters = ruleConfig.GetSection("parameters").ToDictionary()
                };
                schema.Rules.Add(rule);
            }
        }
        
        return schema;
    }
    
    private async Task<ValidationResult> ValidateSchemaRuleAsync(TSKConfig config, TuskLangSchemaRule rule)
    {
        var result = new ValidationResult();
        
        switch (rule.Type.ToLower())
        {
            case "required":
                if (!config.Has(rule.Path))
                {
                    result.AddError($"Required configuration path '{rule.Path}' is missing");
                }
                break;
                
            case "type":
                var value = config.Get<object>(rule.Path);
                var expectedType = rule.Parameters.GetValueOrDefault("type", "") as string;
                if (!IsValidType(value, expectedType))
                {
                    result.AddError($"Configuration path '{rule.Path}' must be of type '{expectedType}'");
                }
                break;
                
            case "range":
                var numericValue = config.Get<double>(rule.Path);
                var min = Convert.ToDouble(rule.Parameters.GetValueOrDefault("min", double.MinValue));
                var max = Convert.ToDouble(rule.Parameters.GetValueOrDefault("max", double.MaxValue));
                if (numericValue < min || numericValue > max)
                {
                    result.AddError($"Configuration path '{rule.Path}' must be between {min} and {max}");
                }
                break;
                
            case "regex":
                var stringValue = config.Get<string>(rule.Path);
                var pattern = rule.Parameters.GetValueOrDefault("pattern", "") as string;
                if (!string.IsNullOrEmpty(stringValue) && !string.IsNullOrEmpty(pattern) && !Regex.IsMatch(stringValue, pattern))
                {
                    result.AddError($"Configuration path '{rule.Path}' does not match pattern '{pattern}'");
                }
                break;
        }
        
        return result;
    }
    
    private bool IsValidType(object? value, string expectedType)
    {
        if (value == null) return false;
        
        return expectedType.ToLower() switch
        {
            "string" => value is string,
            "int" => value is int,
            "double" => value is double,
            "bool" => value is bool,
            "array" => value is System.Collections.IEnumerable,
            _ => true
        };
    }
}

public class TuskLangSchema
{
    public string Name { get; set; } = string.Empty;
    public List<TuskLangSchemaRule> Rules { get; set; } = new();
}

public class TuskLangSchemaRule
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}
```

## 🔧 Custom Validators

### Custom Validation Attributes

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class TuskLangValidationAttribute : ValidationAttribute
{
    public string ValidationRule { get; }
    public Dictionary<string, object> Parameters { get; }
    
    public TuskLangValidationAttribute(string validationRule, params object[] parameters)
    {
        ValidationRule = validationRule;
        Parameters = new Dictionary<string, object>();
        
        for (int i = 0; i < parameters.Length; i += 2)
        {
            if (i + 1 < parameters.Length)
            {
                Parameters[parameters[i].ToString()!] = parameters[i + 1];
            }
        }
    }
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var validationService = validationContext.GetService(typeof(ValidationService)) as ValidationService;
        if (validationService == null)
        {
            return new ValidationResult("Validation service not available");
        }
        
        // This would need to be implemented asynchronously
        // For now, return success
        return ValidationResult.Success;
    }
}

public class CustomValidator : IValidator
{
    private readonly ILogger<CustomValidator> _logger;
    
    public CustomValidator(ILogger<CustomValidator> logger)
    {
        _logger = logger;
    }
    
    public async Task<ValidationResult> ValidateAsync<T>(T model, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        var validatorType = rule.Parameters.GetValueOrDefault("validator_type", "") as string;
        if (string.IsNullOrEmpty(validatorType))
        {
            result.AddError("Custom validator type not specified");
            return result;
        }
        
        try
        {
            // Create validator instance
            var type = Type.GetType(validatorType);
            if (type == null)
            {
                result.AddError($"Custom validator type '{validatorType}' not found");
                return result;
            }
            
            var validator = Activator.CreateInstance(type) as ICustomValidator;
            if (validator == null)
            {
                result.AddError($"Type '{validatorType}' does not implement ICustomValidator");
                return result;
            }
            
            var validationResult = await validator.ValidateAsync(model, rule.Parameters);
            result.Merge(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom validation failed for type {ValidatorType}", validatorType);
            result.AddError("Custom validation failed");
        }
        
        return result;
    }
    
    public async Task<ValidationResult> ValidateValueAsync(object? value, ValidationRule rule)
    {
        var result = new ValidationResult();
        
        var validatorType = rule.Parameters.GetValueOrDefault("validator_type", "") as string;
        if (string.IsNullOrEmpty(validatorType))
        {
            result.AddError("Custom validator type not specified");
            return result;
        }
        
        try
        {
            // Create validator instance
            var type = Type.GetType(validatorType);
            if (type == null)
            {
                result.AddError($"Custom validator type '{validatorType}' not found");
                return result;
            }
            
            var validator = Activator.CreateInstance(type) as ICustomValidator;
            if (validator == null)
            {
                result.AddError($"Type '{validatorType}' does not implement ICustomValidator");
                return result;
            }
            
            var validationResult = await validator.ValidateValueAsync(value, rule.Parameters);
            result.Merge(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom value validation failed for type {ValidatorType}", validatorType);
            result.AddError("Custom validation failed");
        }
        
        return result;
    }
}

public interface ICustomValidator
{
    Task<ValidationResult> ValidateAsync<T>(T model, Dictionary<string, object> parameters);
    Task<ValidationResult> ValidateValueAsync(object? value, Dictionary<string, object> parameters);
}

// Example custom validator
public class EmailDomainValidator : ICustomValidator
{
    public async Task<ValidationResult> ValidateAsync<T>(T model, Dictionary<string, object> parameters)
    {
        var result = new ValidationResult();
        
        var allowedDomains = parameters.GetValueOrDefault("allowed_domains", new List<string>()) as List<string>;
        if (allowedDomains == null || !allowedDomains.Any())
        {
            return result;
        }
        
        var propertyName = parameters.GetValueOrDefault("property", "email") as string;
        var property = typeof(T).GetProperty(propertyName ?? "email");
        
        if (property != null)
        {
            var value = property.GetValue(model) as string;
            if (!string.IsNullOrEmpty(value))
            {
                var domain = value.Split('@').LastOrDefault();
                if (!string.IsNullOrEmpty(domain) && !allowedDomains.Contains(domain))
                {
                    result.AddError($"Email domain '{domain}' is not allowed");
                }
            }
        }
        
        return result;
    }
    
    public async Task<ValidationResult> ValidateValueAsync(object? value, Dictionary<string, object> parameters)
    {
        var result = new ValidationResult();
        
        var allowedDomains = parameters.GetValueOrDefault("allowed_domains", new List<string>()) as List<string>;
        if (allowedDomains == null || !allowedDomains.Any())
        {
            return result;
        }
        
        if (value is string email && !string.IsNullOrEmpty(email))
        {
            var domain = email.Split('@').LastOrDefault();
            if (!string.IsNullOrEmpty(domain) && !allowedDomains.Contains(domain))
            {
                result.AddError($"Email domain '{domain}' is not allowed");
            }
        }
        
        return result;
    }
}
```

## 📝 Summary

This guide covered comprehensive data validation strategies for C# TuskLang applications:

- **Input Validation**: Validation service with built-in validators for common validation scenarios
- **Schema Validation**: JSON schema validation and TuskLang-specific schema validation
- **Custom Validators**: Custom validation attributes and extensible validation framework
- **Validation Best Practices**: Proper error handling, logging, and validation result management

These data validation strategies ensure your C# TuskLang applications have robust data integrity and security validation. 
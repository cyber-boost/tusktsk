# ✅ Data Validation - TuskLang for C# - "Validation Mastery"

**Master data validation with TuskLang in your C# applications!**

Data validation is crucial for data integrity and security. This guide covers input validation, schema validation, custom validators, and real-world validation scenarios for TuskLang in C# environments.

## 🎯 Validation Philosophy

### "We Don't Bow to Any King"
- **Validate everything** - Trust no input
- **Fail fast** - Catch errors early
- **Clear messages** - Provide actionable error messages
- **Type safety** - Use strong typing
- **Schema validation** - Validate against schemas

## 🔍 Input Validation

### Example: Input Validation Service
```csharp
// InputValidationService.cs
using System.Text.RegularExpressions;

public class InputValidationService
{
    private readonly TuskLang _parser;
    private readonly ILogger<InputValidationService> _logger;
    private readonly Dictionary<string, ValidationRule> _validationRules;
    
    public InputValidationService(ILogger<InputValidationService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        _validationRules = new Dictionary<string, ValidationRule>();
        
        LoadValidationRules();
    }
    
    private void LoadValidationRules()
    {
        var config = _parser.ParseFile("config/validation.tsk");
        var rules = config["validation_rules"] as Dictionary<string, object>;
        
        foreach (var rule in rules ?? new Dictionary<string, object>())
        {
            var ruleName = rule.Key;
            var ruleData = rule.Value as Dictionary<string, object>;
            
            if (ruleData != null)
            {
                _validationRules[ruleName] = new ValidationRule
                {
                    Name = ruleName,
                    Pattern = ruleData["pattern"].ToString(),
                    MinLength = int.Parse(ruleData["min_length"].ToString()),
                    MaxLength = int.Parse(ruleData["max_length"].ToString()),
                    Required = bool.Parse(ruleData["required"].ToString())
                };
            }
        }
        
        _logger.LogInformation("Loaded {Count} validation rules", _validationRules.Count);
    }
    
    public ValidationResult ValidateInput(string fieldName, string value, string ruleName)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            if (!_validationRules.ContainsKey(ruleName))
            {
                result.IsValid = false;
                result.Errors.Add($"Unknown validation rule: {ruleName}");
                return result;
            }
            
            var rule = _validationRules[ruleName];
            
            // Check if required
            if (rule.Required && string.IsNullOrWhiteSpace(value))
            {
                result.IsValid = false;
                result.Errors.Add($"{fieldName} is required");
                return result;
            }
            
            // Check length
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Length < rule.MinLength)
                {
                    result.IsValid = false;
                    result.Errors.Add($"{fieldName} must be at least {rule.MinLength} characters long");
                }
                
                if (value.Length > rule.MaxLength)
                {
                    result.IsValid = false;
                    result.Errors.Add($"{fieldName} must be no more than {rule.MaxLength} characters long");
                }
                
                // Check pattern
                if (!string.IsNullOrEmpty(rule.Pattern))
                {
                    var regex = new Regex(rule.Pattern);
                    if (!regex.IsMatch(value))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"{fieldName} format is invalid");
                    }
                }
            }
            
            if (result.IsValid)
            {
                _logger.LogDebug("Validation passed for field {FieldName} with rule {RuleName}", fieldName, ruleName);
            }
            else
            {
                _logger.LogWarning("Validation failed for field {FieldName} with rule {RuleName}: {Errors}", 
                    fieldName, ruleName, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation error for field {FieldName} with rule {RuleName}", fieldName, ruleName);
            result.IsValid = false;
            result.Errors.Add("Validation error occurred");
        }
        
        return result;
    }
    
    public ValidationResult ValidateUserInput(Dictionary<string, string> userData)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Validate email
        if (userData.ContainsKey("email"))
        {
            var emailResult = ValidateInput("Email", userData["email"], "email");
            if (!emailResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(emailResult.Errors);
            }
        }
        
        // Validate password
        if (userData.ContainsKey("password"))
        {
            var passwordResult = ValidateInput("Password", userData["password"], "password");
            if (!passwordResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(passwordResult.Errors);
            }
        }
        
        // Validate username
        if (userData.ContainsKey("username"))
        {
            var usernameResult = ValidateInput("Username", userData["username"], "username");
            if (!usernameResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(usernameResult.Errors);
            }
        }
        
        return result;
    }
}

public class ValidationRule
{
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public int MinLength { get; set; }
    public int MaxLength { get; set; }
    public bool Required { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
```

## 📋 Schema Validation

### Example: Schema Validation Service
```csharp
// SchemaValidationService.cs
public class SchemaValidationService
{
    private readonly TuskLang _parser;
    private readonly ILogger<SchemaValidationService> _logger;
    private readonly Dictionary<string, object> _schemas;
    
    public SchemaValidationService(ILogger<SchemaValidationService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        _schemas = new Dictionary<string, object>();
        
        LoadSchemas();
    }
    
    private void LoadSchemas()
    {
        var config = _parser.ParseFile("config/schemas.tsk");
        var schemas = config["schemas"] as Dictionary<string, object>;
        
        foreach (var schema in schemas ?? new Dictionary<string, object>())
        {
            _schemas[schema.Key] = schema.Value;
        }
        
        _logger.LogInformation("Loaded {Count} validation schemas", _schemas.Count);
    }
    
    public ValidationResult ValidateAgainstSchema(string schemaName, Dictionary<string, object> data)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            if (!_schemas.ContainsKey(schemaName))
            {
                result.IsValid = false;
                result.Errors.Add($"Schema '{schemaName}' not found");
                return result;
            }
            
            var schema = _schemas[schemaName] as Dictionary<string, object>;
            if (schema == null)
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid schema format for '{schemaName}'");
                return result;
            }
            
            // Validate required fields
            var requiredFields = schema["required"] as List<object>;
            foreach (var field in requiredFields ?? Enumerable.Empty<object>())
            {
                var fieldName = field.ToString();
                if (!data.ContainsKey(fieldName))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Required field '{fieldName}' is missing");
                }
            }
            
            // Validate field types
            var properties = schema["properties"] as Dictionary<string, object>;
            foreach (var property in properties ?? new Dictionary<string, object>())
            {
                var fieldName = property.Key;
                var fieldSchema = property.Value as Dictionary<string, object>;
                
                if (data.ContainsKey(fieldName) && fieldSchema != null)
                {
                    var fieldValidation = ValidateField(fieldName, data[fieldName], fieldSchema);
                    if (!fieldValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Errors.AddRange(fieldValidation.Errors);
                    }
                }
            }
            
            if (result.IsValid)
            {
                _logger.LogDebug("Schema validation passed for schema {SchemaName}", schemaName);
            }
            else
            {
                _logger.LogWarning("Schema validation failed for schema {SchemaName}: {Errors}", 
                    schemaName, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation error for schema {SchemaName}", schemaName);
            result.IsValid = false;
            result.Errors.Add("Schema validation error occurred");
        }
        
        return result;
    }
    
    private ValidationResult ValidateField(string fieldName, object value, Dictionary<string, object> schema)
    {
        var result = new ValidationResult { IsValid = true };
        
        try
        {
            var fieldType = schema["type"].ToString();
            
            switch (fieldType)
            {
                case "string":
                    if (value is not string stringValue)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Field '{fieldName}' must be a string");
                    }
                    else
                    {
                        // Validate string constraints
                        if (schema.ContainsKey("minLength"))
                        {
                            var minLength = int.Parse(schema["minLength"].ToString());
                            if (stringValue.Length < minLength)
                            {
                                result.IsValid = false;
                                result.Errors.Add($"Field '{fieldName}' must be at least {minLength} characters long");
                            }
                        }
                        
                        if (schema.ContainsKey("maxLength"))
                        {
                            var maxLength = int.Parse(schema["maxLength"].ToString());
                            if (stringValue.Length > maxLength)
                            {
                                result.IsValid = false;
                                result.Errors.Add($"Field '{fieldName}' must be no more than {maxLength} characters long");
                            }
                        }
                        
                        if (schema.ContainsKey("pattern"))
                        {
                            var pattern = schema["pattern"].ToString();
                            var regex = new Regex(pattern);
                            if (!regex.IsMatch(stringValue))
                            {
                                result.IsValid = false;
                                result.Errors.Add($"Field '{fieldName}' format is invalid");
                            }
                        }
                    }
                    break;
                    
                case "integer":
                    if (!int.TryParse(value.ToString(), out _))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Field '{fieldName}' must be an integer");
                    }
                    break;
                    
                case "boolean":
                    if (value is not bool)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Field '{fieldName}' must be a boolean");
                    }
                    break;
                    
                default:
                    result.IsValid = false;
                    result.Errors.Add($"Unknown field type '{fieldType}' for field '{fieldName}'");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Field validation error for field {FieldName}", fieldName);
            result.IsValid = false;
            result.Errors.Add($"Field validation error for '{fieldName}'");
        }
        
        return result;
    }
}
```

## 🛠️ Custom Validators

### Example: Custom Validation Service
```csharp
// CustomValidationService.cs
public class CustomValidationService
{
    private readonly TuskLang _parser;
    private readonly ILogger<CustomValidationService> _logger;
    private readonly Dictionary<string, Func<object, ValidationResult>> _customValidators;
    
    public CustomValidationService(ILogger<CustomValidationService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        _customValidators = new Dictionary<string, Func<object, ValidationResult>>();
        
        RegisterCustomValidators();
    }
    
    private void RegisterCustomValidators()
    {
        // Register email validator
        _customValidators["email"] = value =>
        {
            var result = new ValidationResult { IsValid = true };
            
            if (value is string email)
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(email))
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid email format");
                }
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add("Email must be a string");
            }
            
            return result;
        };
        
        // Register password strength validator
        _customValidators["password_strength"] = value =>
        {
            var result = new ValidationResult { IsValid = true };
            
            if (value is string password)
            {
                if (password.Length < 8)
                {
                    result.IsValid = false;
                    result.Errors.Add("Password must be at least 8 characters long");
                }
                
                if (!password.Any(char.IsUpper))
                {
                    result.IsValid = false;
                    result.Errors.Add("Password must contain at least one uppercase letter");
                }
                
                if (!password.Any(char.IsLower))
                {
                    result.IsValid = false;
                    result.Errors.Add("Password must contain at least one lowercase letter");
                }
                
                if (!password.Any(char.IsDigit))
                {
                    result.IsValid = false;
                    result.Errors.Add("Password must contain at least one digit");
                }
                
                if (!password.Any(c => !char.IsLetterOrDigit(c)))
                {
                    result.IsValid = false;
                    result.Errors.Add("Password must contain at least one special character");
                }
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add("Password must be a string");
            }
            
            return result;
        };
        
        // Register phone number validator
        _customValidators["phone_number"] = value =>
        {
            var result = new ValidationResult { IsValid = true };
            
            if (value is string phone)
            {
                var phoneRegex = new Regex(@"^\+?[\d\s\-\(\)]+$");
                if (!phoneRegex.IsMatch(phone))
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid phone number format");
                }
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add("Phone number must be a string");
            }
            
            return result;
        };
        
        _logger.LogInformation("Registered {Count} custom validators", _customValidators.Count);
    }
    
    public ValidationResult ValidateWithCustomValidator(string validatorName, object value)
    {
        if (!_customValidators.ContainsKey(validatorName))
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = { $"Custom validator '{validatorName}' not found" }
            };
        }
        
        try
        {
            var result = _customValidators[validatorName](value);
            
            if (result.IsValid)
            {
                _logger.LogDebug("Custom validation passed for validator {ValidatorName}", validatorName);
            }
            else
            {
                _logger.LogWarning("Custom validation failed for validator {ValidatorName}: {Errors}", 
                    validatorName, string.Join(", ", result.Errors));
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom validation error for validator {ValidatorName}", validatorName);
            return new ValidationResult
            {
                IsValid = false,
                Errors = { $"Custom validation error for '{validatorName}'" }
            };
        }
    }
    
    public void RegisterValidator(string name, Func<object, ValidationResult> validator)
    {
        _customValidators[name] = validator;
        _logger.LogInformation("Registered custom validator: {ValidatorName}", name);
    }
}
```

## 🛠️ Real-World Validation Scenarios
- **User registration**: Validate email, password, username
- **API input**: Validate request parameters and body
- **Configuration**: Validate configuration files
- **Database data**: Validate data before persistence

## 🧩 Best Practices
- Validate all input data
- Use clear error messages
- Implement custom validators for business rules
- Use schema validation for complex data structures
- Validate early and often

## 🏁 You're Ready!

You can now:
- Implement comprehensive input validation
- Use schema validation for complex data
- Create custom validators
- Follow validation best practices

**Next:** [Error Handling Patterns](032-error-handling-csharp.md)

---

**"We don't bow to any king" - Your validation mastery, your data integrity, your input excellence.**

Validate everything. Trust nothing. ✅ 
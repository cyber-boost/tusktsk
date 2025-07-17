# @validate Operator in TuskLang - Go Guide

## ✅ **Validation Power: @validate Operator Unleashed**

TuskLang's `@validate` operator is your data integrity superweapon. We don't bow to any king—especially not to invalid or unsafe data. Here's how to use `@validate` in Go projects to ensure your configuration is bulletproof.

## 📋 **Table of Contents**
- [What is @validate?](#what-is-validate)
- [Basic Usage](#basic-usage)
- [Validation Rules](#validation-rules)
- [Schema Validation](#schema-validation)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🛡️ **What is @validate?**

The `@validate` operator validates data directly in your config. No more runtime surprises—just pure, validated confidence.

## 🛠️ **Basic Usage**

```go
[validation]
api_key: @validate.required(["API_KEY", "DATABASE_URL"])
email: @validate.email(@env("ADMIN_EMAIL"))
url: @validate.url(@env("API_BASE_URL"))
port: @validate.range(@env("PORT"), 1, 65535)
```

## 📋 **Validation Rules**

### **Required Fields**
```go
[required]
fields: @validate.required(["API_KEY", "DB_PASSWORD", "JWT_SECRET"])
```

### **Type Validation**
```go
[types]
string_field: @validate.string(["APP_NAME"])
numeric_field: @validate.numeric(["PORT", "TIMEOUT"])
boolean_field: @validate.boolean(["DEBUG"])
```

### **Format Validation**
```go
[formats]
email: @validate.email(@env("ADMIN_EMAIL"))
url: @validate.url(@env("API_URL"))
ip: @validate.ip(@env("SERVER_IP"))
uuid: @validate.uuid(@env("SESSION_ID"))
```

### **Range Validation**
```go
[ranges]
port: @validate.range(@env("PORT"), 1, 65535)
timeout: @validate.range(@env("TIMEOUT"), 1, 300)
age: @validate.range(@env("AGE"), 18, 100)
```

## 📊 **Schema Validation**

```go
[user_schema]
schema: @validate.schema({
    "type": "object",
    "properties": {
        "name": {"type": "string", "minLength": 1},
        "email": {"type": "string", "format": "email"},
        "age": {"type": "integer", "minimum": 18, "maximum": 100}
    },
    "required": ["name", "email"]
})
```

## 🔗 **Go Integration**

```go
// Validation is automatic when accessing config values
apiKey, err := config.GetString("api_key")
if err != nil {
    log.Fatalf("Validation failed: %v", err)
}

// Manual validation
err = config.Validate()
if err != nil {
    log.Fatalf("Config validation failed: %v", err)
}
```

### **Custom Validation**
```go
type ConfigValidator struct {
    requiredFields []string
}

func (v *ConfigValidator) Validate(config map[string]interface{}) error {
    for _, field := range v.requiredFields {
        if _, exists := config[field]; !exists {
            return fmt.Errorf("required field '%s' is missing", field)
        }
    }
    return nil
}
```

## 🥇 **Best Practices**
- Validate all critical configuration values
- Use descriptive error messages
- Validate early in application startup
- Provide fallbacks for non-critical validation failures
- Log all validation errors

---

**TuskLang: Bulletproof validation with @validate.** 
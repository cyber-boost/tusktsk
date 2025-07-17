# Functions in TuskLang - Go Guide

## 🎯 **The Power of Executable Configuration**

In TuskLang, functions aren't just code—they're configuration with a heartbeat. We don't bow to any king, especially not static configuration files. TuskLang's FUJSEN (Function JavaScript) system lets you embed executable logic directly in your configuration, making it truly dynamic and powerful.

## 📋 **Table of Contents**
- [Understanding Functions in TuskLang](#understanding-functions-in-tusklang)
- [FUJSEN Function Syntax](#fujsen-function-syntax)
- [Go Integration](#go-integration)
- [Function Types and Patterns](#function-types-and-patterns)
- [Security and Validation](#security-and-validation)
- [Performance Considerations](#performance-considerations)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding Functions in TuskLang**

TuskLang's FUJSEN system allows you to embed JavaScript functions directly in configuration:

```go
// TuskLang configuration with functions
[data_processing]
transform_data: """function transform(data) { 
    return data.map(x => x * 2).filter(x => x > 10); 
}"""

validate_input: """function validate(input) { 
    return input.length > 0 && input.length < 100; 
}"""

[calculations]
calculate_tax: """function calculate(amount, rate) { 
    return amount * (rate / 100); 
}"""

format_currency: """function format(amount) { 
    return '$' + amount.toFixed(2); 
}"""
```

```go
// Go integration
type DataProcessing struct {
    TransformData string `tsk:"transform_data"`
    ValidateInput string `tsk:"validate_input"`
}

type Calculations struct {
    CalculateTax   string `tsk:"calculate_tax"`
    FormatCurrency string `tsk:"format_currency"`
}
```

## 🎨 **FUJSEN Function Syntax**

### **Basic Function Definition**

```go
// TuskLang - Basic function syntax
[basic_functions]
simple_function: """function simple() { 
    return "Hello, World!"; 
}"""

parameter_function: """function greet(name) { 
    return "Hello, " + name + "!"; 
}"""

return_function: """function add(a, b) { 
    return a + b; 
}"""
```

```go
// Go - Handling basic functions
type BasicFunctions struct {
    SimpleFunction   string `tsk:"simple_function"`
    ParameterFunction string `tsk:"parameter_function"`
    ReturnFunction   string `tsk:"return_function"`
}

func (b *BasicFunctions) ExecuteSimple() (string, error) {
    result, err := tusk.ExecuteFunction(b.SimpleFunction, "simple")
    if err != nil {
        return "", fmt.Errorf("failed to execute simple function: %w", err)
    }
    
    if str, ok := result.(string); ok {
        return str, nil
    }
    
    return "", errors.New("invalid result type from simple function")
}

func (b *BasicFunctions) ExecuteGreet(name string) (string, error) {
    result, err := tusk.ExecuteFunction(b.ParameterFunction, "greet", name)
    if err != nil {
        return "", fmt.Errorf("failed to execute greet function: %w", err)
    }
    
    if str, ok := result.(string); ok {
        return str, nil
    }
    
    return "", errors.New("invalid result type from greet function")
}

func (b *BasicFunctions) ExecuteAdd(a, b int) (int, error) {
    result, err := tusk.ExecuteFunction(b.ReturnFunction, "add", a, b)
    if err != nil {
        return 0, fmt.Errorf("failed to execute add function: %w", err)
    }
    
    if sum, ok := result.(int); ok {
        return sum, nil
    }
    
    return 0, errors.New("invalid result type from add function")
}
```

### **Complex Function Logic**

```go
// TuskLang - Complex function logic
[complex_functions]
data_filter: """function filter(data, criteria) {
    return data.filter(item => {
        if (criteria.type && item.type !== criteria.type) return false;
        if (criteria.minValue && item.value < criteria.minValue) return false;
        if (criteria.maxValue && item.value > criteria.maxValue) return false;
        return true;
    });
}"""

data_transform: """function transform(data, options) {
    return data.map(item => {
        const transformed = { ...item };
        
        if (options.uppercase && transformed.name) {
            transformed.name = transformed.name.toUpperCase();
        }
        
        if (options.multiply && transformed.value) {
            transformed.value = transformed.value * options.multiply;
        }
        
        if (options.addPrefix && transformed.id) {
            transformed.id = options.addPrefix + transformed.id;
        }
        
        return transformed;
    });
}"""
```

```go
// Go - Handling complex functions
type ComplexFunctions struct {
    DataFilter   string `tsk:"data_filter"`
    DataTransform string `tsk:"data_transform"`
}

type FilterCriteria struct {
    Type     string `json:"type,omitempty"`
    MinValue int    `json:"minValue,omitempty"`
    MaxValue int    `json:"maxValue,omitempty"`
}

type TransformOptions struct {
    Uppercase bool   `json:"uppercase,omitempty"`
    Multiply  int    `json:"multiply,omitempty"`
    AddPrefix string `json:"addPrefix,omitempty"`
}

func (c *ComplexFunctions) ExecuteFilter(data []map[string]interface{}, criteria FilterCriteria) ([]map[string]interface{}, error) {
    result, err := tusk.ExecuteFunction(c.DataFilter, "filter", data, criteria)
    if err != nil {
        return nil, fmt.Errorf("failed to execute filter function: %w", err)
    }
    
    if filtered, ok := result.([]map[string]interface{}); ok {
        return filtered, nil
    }
    
    return nil, errors.New("invalid result type from filter function")
}

func (c *ComplexFunctions) ExecuteTransform(data []map[string]interface{}, options TransformOptions) ([]map[string]interface{}, error) {
    result, err := tusk.ExecuteFunction(c.DataTransform, "transform", data, options)
    if err != nil {
        return nil, fmt.Errorf("failed to execute transform function: %w", err)
    }
    
    if transformed, ok := result.([]map[string]interface{}); ok {
        return transformed, nil
    }
    
    return nil, errors.New("invalid result type from transform function")
}
```

### **Async Functions**

```go
// TuskLang - Async function syntax
[async_functions]
fetch_data: """async function fetch(url) {
    const response = await fetch(url);
    return await response.json();
}"""

process_async: """async function process(data) {
    const results = [];
    for (const item of data) {
        const processed = await processItem(item);
        results.push(processed);
    }
    return results;
}"""
```

```go
// Go - Handling async functions
type AsyncFunctions struct {
    FetchData   string `tsk:"fetch_data"`
    ProcessAsync string `tsk:"process_async"`
}

func (a *AsyncFunctions) ExecuteFetch(url string) (map[string]interface{}, error) {
    result, err := tusk.ExecuteAsyncFunction(a.FetchData, "fetch", url)
    if err != nil {
        return nil, fmt.Errorf("failed to execute fetch function: %w", err)
    }
    
    if data, ok := result.(map[string]interface{}); ok {
        return data, nil
    }
    
    return nil, errors.New("invalid result type from fetch function")
}

func (a *AsyncFunctions) ExecuteProcess(data []map[string]interface{}) ([]map[string]interface{}, error) {
    result, err := tusk.ExecuteAsyncFunction(a.ProcessAsync, "process", data)
    if err != nil {
        return nil, fmt.Errorf("failed to execute process function: %w", err)
    }
    
    if processed, ok := result.([]map[string]interface{}); ok {
        return processed, nil
    }
    
    return nil, errors.New("invalid result type from process function")
}
```

## 🔧 **Go Integration**

### **Function Execution Engine**

```go
// Go - Function execution engine
type FunctionEngine struct {
    Functions map[string]string
}

func NewFunctionEngine() *FunctionEngine {
    return &FunctionEngine{
        Functions: make(map[string]string),
    }
}

func (f *FunctionEngine) RegisterFunction(name, code string) {
    f.Functions[name] = code
}

func (f *FunctionEngine) ExecuteFunction(name string, args ...interface{}) (interface{}, error) {
    code, exists := f.Functions[name]
    if !exists {
        return nil, fmt.Errorf("function '%s' not found", name)
    }
    
    // Execute the JavaScript function
    result, err := tusk.ExecuteFunction(code, name, args...)
    if err != nil {
        return nil, fmt.Errorf("failed to execute function '%s': %w", name, err)
    }
    
    return result, nil
}

func (f *FunctionEngine) ExecuteAsyncFunction(name string, args ...interface{}) (interface{}, error) {
    code, exists := f.Functions[name]
    if !exists {
        return nil, fmt.Errorf("function '%s' not found", name)
    }
    
    // Execute the async JavaScript function
    result, err := tusk.ExecuteAsyncFunction(code, name, args...)
    if err != nil {
        return nil, fmt.Errorf("failed to execute async function '%s': %w", name, err)
    }
    
    return result, nil
}
```

### **Type-Safe Function Wrappers**

```go
// Go - Type-safe function wrappers
type FunctionWrappers struct {
    Engine *FunctionEngine
}

func NewFunctionWrappers() *FunctionWrappers {
    return &FunctionWrappers{
        Engine: NewFunctionEngine(),
    }
}

func (f *FunctionWrappers) ExecuteStringFunction(name string, args ...interface{}) (string, error) {
    result, err := f.Engine.ExecuteFunction(name, args...)
    if err != nil {
        return "", err
    }
    
    if str, ok := result.(string); ok {
        return str, nil
    }
    
    return "", fmt.Errorf("function '%s' did not return a string", name)
}

func (f *FunctionWrappers) ExecuteIntFunction(name string, args ...interface{}) (int, error) {
    result, err := f.Engine.ExecuteFunction(name, args...)
    if err != nil {
        return 0, err
    }
    
    if num, ok := result.(int); ok {
        return num, nil
    }
    
    return 0, fmt.Errorf("function '%s' did not return an integer", name)
}

func (f *FunctionWrappers) ExecuteBoolFunction(name string, args ...interface{}) (bool, error) {
    result, err := f.Engine.ExecuteFunction(name, args...)
    if err != nil {
        return false, err
    }
    
    if b, ok := result.(bool); ok {
        return b, nil
    }
    
    return false, fmt.Errorf("function '%s' did not return a boolean", name)
}

func (f *FunctionWrappers) ExecuteArrayFunction(name string, args ...interface{}) ([]interface{}, error) {
    result, err := f.Engine.ExecuteFunction(name, args...)
    if err != nil {
        return nil, err
    }
    
    if arr, ok := result.([]interface{}); ok {
        return arr, nil
    }
    
    return nil, fmt.Errorf("function '%s' did not return an array", name)
}

func (f *FunctionWrappers) ExecuteMapFunction(name string, args ...interface{}) (map[string]interface{}, error) {
    result, err := f.Engine.ExecuteFunction(name, args...)
    if err != nil {
        return nil, err
    }
    
    if m, ok := result.(map[string]interface{}); ok {
        return m, nil
    }
    
    return nil, fmt.Errorf("function '%s' did not return a map", name)
}
```

### **Function Validation**

```go
// Go - Function validation system
type FunctionValidator struct {
    Functions map[string]FunctionSchema
}

type FunctionSchema struct {
    Name       string                 `json:"name"`
    Parameters []ParameterSchema      `json:"parameters"`
    ReturnType string                 `json:"returnType"`
    Required   bool                   `json:"required"`
    Validation map[string]interface{} `json:"validation,omitempty"`
}

type ParameterSchema struct {
    Name       string `json:"name"`
    Type       string `json:"type"`
    Required   bool   `json:"required"`
    Default    interface{} `json:"default,omitempty"`
}

func NewFunctionValidator() *FunctionValidator {
    return &FunctionValidator{
        Functions: make(map[string]FunctionSchema),
    }
}

func (f *FunctionValidator) RegisterSchema(name string, schema FunctionSchema) {
    f.Functions[name] = schema
}

func (f *FunctionValidator) ValidateFunction(name string, args ...interface{}) error {
    schema, exists := f.Functions[name]
    if !exists {
        return fmt.Errorf("function schema for '%s' not found", name)
    }
    
    // Validate parameter count
    if len(args) != len(schema.Parameters) {
        return fmt.Errorf("function '%s' expects %d parameters, got %d", name, len(schema.Parameters), len(args))
    }
    
    // Validate parameter types
    for i, param := range schema.Parameters {
        if i >= len(args) {
            if param.Required {
                return fmt.Errorf("required parameter '%s' missing for function '%s'", param.Name, name)
            }
            continue
        }
        
        if err := f.validateParameterType(param, args[i]); err != nil {
            return fmt.Errorf("parameter '%s' validation failed: %w", param.Name, err)
        }
    }
    
    return nil
}

func (f *FunctionValidator) validateParameterType(param ParameterSchema, value interface{}) error {
    switch param.Type {
    case "string":
        if _, ok := value.(string); !ok {
            return fmt.Errorf("expected string, got %T", value)
        }
    case "int":
        if _, ok := value.(int); !ok {
            return fmt.Errorf("expected int, got %T", value)
        }
    case "bool":
        if _, ok := value.(bool); !ok {
            return fmt.Errorf("expected bool, got %T", value)
        }
    case "array":
        if _, ok := value.([]interface{}); !ok {
            return fmt.Errorf("expected array, got %T", value)
        }
    case "object":
        if _, ok := value.(map[string]interface{}); !ok {
            return fmt.Errorf("expected object, got %T", value)
        }
    default:
        return fmt.Errorf("unknown parameter type: %s", param.Type)
    }
    
    return nil
}
```

## 🚀 **Function Types and Patterns**

### **Data Processing Functions**

```go
// TuskLang - Data processing functions
[data_processing]
filter_users: """function filter(users, criteria) {
    return users.filter(user => {
        if (criteria.age && user.age < criteria.age) return false;
        if (criteria.active && !user.active) return false;
        if (criteria.role && user.role !== criteria.role) return false;
        return true;
    });
}"""

sort_users: """function sort(users, field, direction) {
    return users.sort((a, b) => {
        const aVal = a[field];
        const bVal = b[field];
        
        if (direction === 'desc') {
            return bVal > aVal ? 1 : -1;
        }
        return aVal > bVal ? 1 : -1;
    });
}"""

group_users: """function group(users, field) {
    return users.reduce((groups, user) => {
        const key = user[field];
        if (!groups[key]) {
            groups[key] = [];
        }
        groups[key].push(user);
        return groups;
    }, {});
}"""
```

```go
// Go - Data processing function handlers
type DataProcessing struct {
    FilterUsers string `tsk:"filter_users"`
    SortUsers   string `tsk:"sort_users"`
    GroupUsers  string `tsk:"group_users"`
}

type User struct {
    ID     int    `json:"id"`
    Name   string `json:"name"`
    Age    int    `json:"age"`
    Active bool   `json:"active"`
    Role   string `json:"role"`
}

type FilterCriteria struct {
    Age    int    `json:"age,omitempty"`
    Active bool   `json:"active,omitempty"`
    Role   string `json:"role,omitempty"`
}

func (d *DataProcessing) FilterUsers(users []User, criteria FilterCriteria) ([]User, error) {
    result, err := tusk.ExecuteFunction(d.FilterUsers, "filter", users, criteria)
    if err != nil {
        return nil, fmt.Errorf("failed to filter users: %w", err)
    }
    
    if filtered, ok := result.([]User); ok {
        return filtered, nil
    }
    
    return nil, errors.New("invalid result type from filter function")
}

func (d *DataProcessing) SortUsers(users []User, field, direction string) ([]User, error) {
    result, err := tusk.ExecuteFunction(d.SortUsers, "sort", users, field, direction)
    if err != nil {
        return nil, fmt.Errorf("failed to sort users: %w", err)
    }
    
    if sorted, ok := result.([]User); ok {
        return sorted, nil
    }
    
    return nil, errors.New("invalid result type from sort function")
}

func (d *DataProcessing) GroupUsers(users []User, field string) (map[string][]User, error) {
    result, err := tusk.ExecuteFunction(d.GroupUsers, "group", users, field)
    if err != nil {
        return nil, fmt.Errorf("failed to group users: %w", err)
    }
    
    if grouped, ok := result.(map[string][]User); ok {
        return grouped, nil
    }
    
    return nil, errors.New("invalid result type from group function")
}
```

### **Validation Functions**

```go
// TuskLang - Validation functions
[validation]
validate_email: """function validate(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}"""

validate_password: """function validate(password) {
    if (password.length < 8) return false;
    if (!/[A-Z]/.test(password)) return false;
    if (!/[a-z]/.test(password)) return false;
    if (!/[0-9]/.test(password)) return false;
    return true;
}"""

validate_phone: """function validate(phone) {
    const phoneRegex = /^\+?[\d\s\-\(\)]+$/;
    return phoneRegex.test(phone) && phone.replace(/\D/g, '').length >= 10;
}"""
```

```go
// Go - Validation function handlers
type Validation struct {
    ValidateEmail    string `tsk:"validate_email"`
    ValidatePassword string `tsk:"validate_password"`
    ValidatePhone    string `tsk:"validate_phone"`
}

func (v *Validation) ValidateEmail(email string) (bool, error) {
    result, err := tusk.ExecuteFunction(v.ValidateEmail, "validate", email)
    if err != nil {
        return false, fmt.Errorf("failed to validate email: %w", err)
    }
    
    if valid, ok := result.(bool); ok {
        return valid, nil
    }
    
    return false, errors.New("invalid result type from email validation")
}

func (v *Validation) ValidatePassword(password string) (bool, error) {
    result, err := tusk.ExecuteFunction(v.ValidatePassword, "validate", password)
    if err != nil {
        return false, fmt.Errorf("failed to validate password: %w", err)
    }
    
    if valid, ok := result.(bool); ok {
        return valid, nil
    }
    
    return false, errors.New("invalid result type from password validation")
}

func (v *Validation) ValidatePhone(phone string) (bool, error) {
    result, err := tusk.ExecuteFunction(v.ValidatePhone, "validate", phone)
    if err != nil {
        return false, fmt.Errorf("failed to validate phone: %w", err)
    }
    
    if valid, ok := result.(bool); ok {
        return valid, nil
    }
    
    return false, errors.New("invalid result type from phone validation")
}
```

### **Calculation Functions**

```go
// TuskLang - Calculation functions
[calculations]
calculate_tax: """function calculate(amount, rate) {
    return amount * (rate / 100);
}"""

calculate_discount: """function calculate(amount, discountPercent) {
    return amount * (discountPercent / 100);
}"""

calculate_total: """function calculate(items) {
    return items.reduce((total, item) => {
        return total + (item.price * item.quantity);
    }, 0);
}"""

format_currency: """function format(amount, currency = 'USD') {
    const formatter = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency
    });
    return formatter.format(amount);
}"""
```

```go
// Go - Calculation function handlers
type Calculations struct {
    CalculateTax      string `tsk:"calculate_tax"`
    CalculateDiscount string `tsk:"calculate_discount"`
    CalculateTotal    string `tsk:"calculate_total"`
    FormatCurrency    string `tsk:"format_currency"`
}

type OrderItem struct {
    Name     string  `json:"name"`
    Price    float64 `json:"price"`
    Quantity int     `json:"quantity"`
}

func (c *Calculations) CalculateTax(amount float64, rate float64) (float64, error) {
    result, err := tusk.ExecuteFunction(c.CalculateTax, "calculate", amount, rate)
    if err != nil {
        return 0, fmt.Errorf("failed to calculate tax: %w", err)
    }
    
    if tax, ok := result.(float64); ok {
        return tax, nil
    }
    
    return 0, errors.New("invalid result type from tax calculation")
}

func (c *Calculations) CalculateDiscount(amount float64, discountPercent float64) (float64, error) {
    result, err := tusk.ExecuteFunction(c.CalculateDiscount, "calculate", amount, discountPercent)
    if err != nil {
        return 0, fmt.Errorf("failed to calculate discount: %w", err)
    }
    
    if discount, ok := result.(float64); ok {
        return discount, nil
    }
    
    return 0, errors.New("invalid result type from discount calculation")
}

func (c *Calculations) CalculateTotal(items []OrderItem) (float64, error) {
    result, err := tusk.ExecuteFunction(c.CalculateTotal, "calculate", items)
    if err != nil {
        return 0, fmt.Errorf("failed to calculate total: %w", err)
    }
    
    if total, ok := result.(float64); ok {
        return total, nil
    }
    
    return 0, errors.New("invalid result type from total calculation")
}

func (c *Calculations) FormatCurrency(amount float64, currency string) (string, error) {
    result, err := tusk.ExecuteFunction(c.FormatCurrency, "format", amount, currency)
    if err != nil {
        return "", fmt.Errorf("failed to format currency: %w", err)
    }
    
    if formatted, ok := result.(string); ok {
        return formatted, nil
    }
    
    return "", errors.New("invalid result type from currency formatting")
}
```

## 🛡️ **Security and Validation**

### **Function Security**

```go
// Go - Function security system
type FunctionSecurity struct {
    AllowedFunctions map[string]bool
    MaxExecutionTime time.Duration
    MaxMemoryUsage   int64
}

func NewFunctionSecurity() *FunctionSecurity {
    return &FunctionSecurity{
        AllowedFunctions: make(map[string]bool),
        MaxExecutionTime: 5 * time.Second,
        MaxMemoryUsage:   100 * 1024 * 1024, // 100MB
    }
}

func (f *FunctionSecurity) AllowFunction(name string) {
    f.AllowedFunctions[name] = true
}

func (f *FunctionSecurity) DenyFunction(name string) {
    f.AllowedFunctions[name] = false
}

func (f *FunctionSecurity) IsFunctionAllowed(name string) bool {
    allowed, exists := f.AllowedFunctions[name]
    return exists && allowed
}

func (f *FunctionSecurity) ExecuteWithSecurity(name, code string, args ...interface{}) (interface{}, error) {
    if !f.IsFunctionAllowed(name) {
        return nil, fmt.Errorf("function '%s' is not allowed", name)
    }
    
    // Execute with timeout and memory limits
    ctx, cancel := context.WithTimeout(context.Background(), f.MaxExecutionTime)
    defer cancel()
    
    resultChan := make(chan interface{}, 1)
    errorChan := make(chan error, 1)
    
    go func() {
        result, err := tusk.ExecuteFunction(code, name, args...)
        if err != nil {
            errorChan <- err
            return
        }
        resultChan <- result
    }()
    
    select {
    case result := <-resultChan:
        return result, nil
    case err := <-errorChan:
        return nil, err
    case <-ctx.Done():
        return nil, fmt.Errorf("function execution timed out after %v", f.MaxExecutionTime)
    }
}
```

### **Input Validation**

```go
// Go - Input validation for functions
type InputValidator struct {
    Schemas map[string]InputSchema
}

type InputSchema struct {
    Parameters []ParameterValidation `json:"parameters"`
    Required   []string             `json:"required"`
}

type ParameterValidation struct {
    Name     string      `json:"name"`
    Type     string      `json:"type"`
    Required bool        `json:"required"`
    Min      interface{} `json:"min,omitempty"`
    Max      interface{} `json:"max,omitempty"`
    Pattern  string      `json:"pattern,omitempty"`
}

func NewInputValidator() *InputValidator {
    return &InputValidator{
        Schemas: make(map[string]InputSchema),
    }
}

func (i *InputValidator) RegisterSchema(name string, schema InputSchema) {
    i.Schemas[name] = schema
}

func (i *InputValidator) ValidateInput(name string, args ...interface{}) error {
    schema, exists := i.Schemas[name]
    if !exists {
        return fmt.Errorf("input schema for function '%s' not found", name)
    }
    
    // Validate required parameters
    if len(args) < len(schema.Required) {
        return fmt.Errorf("function '%s' requires %d parameters, got %d", name, len(schema.Required), len(args))
    }
    
    // Validate each parameter
    for j, param := range schema.Parameters {
        if j >= len(args) {
            if param.Required {
                return fmt.Errorf("required parameter '%s' missing", param.Name)
            }
            continue
        }
        
        if err := i.validateParameter(param, args[j]); err != nil {
            return fmt.Errorf("parameter '%s' validation failed: %w", param.Name, err)
        }
    }
    
    return nil
}

func (i *InputValidator) validateParameter(param ParameterValidation, value interface{}) error {
    // Type validation
    switch param.Type {
    case "string":
        if str, ok := value.(string); ok {
            if param.Pattern != "" {
                matched, err := regexp.MatchString(param.Pattern, str)
                if err != nil {
                    return fmt.Errorf("invalid pattern: %w", err)
                }
                if !matched {
                    return fmt.Errorf("value does not match pattern")
                }
            }
            if param.Min != nil {
                if min, ok := param.Min.(int); ok && len(str) < min {
                    return fmt.Errorf("string length must be at least %d", min)
                }
            }
            if param.Max != nil {
                if max, ok := param.Max.(int); ok && len(str) > max {
                    return fmt.Errorf("string length must be at most %d", max)
                }
            }
        } else {
            return fmt.Errorf("expected string, got %T", value)
        }
    case "int":
        if num, ok := value.(int); ok {
            if param.Min != nil {
                if min, ok := param.Min.(int); ok && num < min {
                    return fmt.Errorf("value must be at least %d", min)
                }
            }
            if param.Max != nil {
                if max, ok := param.Max.(int); ok && num > max {
                    return fmt.Errorf("value must be at most %d", max)
                }
            }
        } else {
            return fmt.Errorf("expected int, got %T", value)
        }
    default:
        return fmt.Errorf("unknown parameter type: %s", param.Type)
    }
    
    return nil
}
```

## ⚡ **Performance Considerations**

### **Function Caching**

```go
// Go - Function caching system
type FunctionCache struct {
    cache map[string]interface{}
    mutex sync.RWMutex
    ttl   time.Duration
}

func NewFunctionCache(ttl time.Duration) *FunctionCache {
    return &FunctionCache{
        cache: make(map[string]interface{}),
        ttl:   ttl,
    }
}

func (f *FunctionCache) Get(key string) (interface{}, bool) {
    f.mutex.RLock()
    defer f.mutex.RUnlock()
    
    value, exists := f.cache[key]
    return value, exists
}

func (f *FunctionCache) Set(key string, value interface{}) {
    f.mutex.Lock()
    defer f.mutex.Unlock()
    
    f.cache[key] = value
    
    // Schedule cleanup
    go func() {
        time.Sleep(f.ttl)
        f.mutex.Lock()
        delete(f.cache, key)
        f.mutex.Unlock()
    }()
}

func (f *FunctionCache) Clear() {
    f.mutex.Lock()
    defer f.mutex.Unlock()
    
    f.cache = make(map[string]interface{})
}
```

### **Function Pooling**

```go
// Go - Function execution pool
type FunctionPool struct {
    workers chan struct{}
    cache   *FunctionCache
}

func NewFunctionPool(maxWorkers int, cacheTTL time.Duration) *FunctionPool {
    return &FunctionPool{
        workers: make(chan struct{}, maxWorkers),
        cache:   NewFunctionCache(cacheTTL),
    }
}

func (f *FunctionPool) Execute(name, code string, args ...interface{}) (interface{}, error) {
    // Check cache first
    cacheKey := f.generateCacheKey(name, args...)
    if cached, exists := f.cache.Get(cacheKey); exists {
        return cached, nil
    }
    
    // Acquire worker
    f.workers <- struct{}{}
    defer func() { <-f.workers }()
    
    // Execute function
    result, err := tusk.ExecuteFunction(code, name, args...)
    if err != nil {
        return nil, err
    }
    
    // Cache result
    f.cache.Set(cacheKey, result)
    
    return result, nil
}

func (f *FunctionPool) generateCacheKey(name string, args ...interface{}) string {
    // Simple cache key generation
    key := name
    for _, arg := range args {
        key += fmt.Sprintf(":%v", arg)
    }
    return key
}
```

## 🌍 **Real-World Examples**

### **E-commerce Price Calculation System**

```go
// TuskLang - E-commerce price calculation
[ecommerce]
calculate_price: """function calculate(item, user) {
    let basePrice = item.price;
    
    // Apply user discount
    if (user.discountPercent) {
        basePrice = basePrice * (1 - user.discountPercent / 100);
    }
    
    // Apply bulk discount
    if (item.quantity >= 10) {
        basePrice = basePrice * 0.9; // 10% bulk discount
    } else if (item.quantity >= 5) {
        basePrice = basePrice * 0.95; // 5% bulk discount
    }
    
    // Apply tax
    const taxRate = user.state === 'CA' ? 0.085 : 0.06;
    const tax = basePrice * taxRate;
    
    return {
        subtotal: basePrice,
        tax: tax,
        total: basePrice + tax
    };
}"""

apply_coupon: """function apply(coupon, total) {
    if (coupon.type === 'percentage') {
        return total * (1 - coupon.value / 100);
    } else if (coupon.type === 'fixed') {
        return Math.max(0, total - coupon.value);
    }
    return total;
}"""
```

```go
// Go - E-commerce price calculation handlers
type Ecommerce struct {
    CalculatePrice string `tsk:"calculate_price"`
    ApplyCoupon    string `tsk:"apply_coupon"`
}

type Item struct {
    ID       int     `json:"id"`
    Name     string  `json:"name"`
    Price    float64 `json:"price"`
    Quantity int     `json:"quantity"`
}

type User struct {
    ID               int     `json:"id"`
    Name             string  `json:"name"`
    State            string  `json:"state"`
    DiscountPercent  float64 `json:"discountPercent"`
}

type Coupon struct {
    Code  string  `json:"code"`
    Type  string  `json:"type"` // "percentage" or "fixed"
    Value float64 `json:"value"`
}

type PriceBreakdown struct {
    Subtotal float64 `json:"subtotal"`
    Tax      float64 `json:"tax"`
    Total    float64 `json:"total"`
}

func (e *Ecommerce) CalculatePrice(item Item, user User) (*PriceBreakdown, error) {
    result, err := tusk.ExecuteFunction(e.CalculatePrice, "calculate", item, user)
    if err != nil {
        return nil, fmt.Errorf("failed to calculate price: %w", err)
    }
    
    if breakdown, ok := result.(map[string]interface{}); ok {
        return &PriceBreakdown{
            Subtotal: breakdown["subtotal"].(float64),
            Tax:      breakdown["tax"].(float64),
            Total:    breakdown["total"].(float64),
        }, nil
    }
    
    return nil, errors.New("invalid result type from price calculation")
}

func (e *Ecommerce) ApplyCoupon(coupon Coupon, total float64) (float64, error) {
    result, err := tusk.ExecuteFunction(e.ApplyCoupon, "apply", coupon, total)
    if err != nil {
        return 0, fmt.Errorf("failed to apply coupon: %w", err)
    }
    
    if discounted, ok := result.(float64); ok {
        return discounted, nil
    }
    
    return 0, errors.New("invalid result type from coupon application")
}
```

### **Data Analytics Processing System**

```go
// TuskLang - Data analytics processing
[analytics]
aggregate_data: """function aggregate(data, groupBy, metrics) {
    const groups = {};
    
    data.forEach(item => {
        const key = item[groupBy];
        if (!groups[key]) {
            groups[key] = {
                count: 0,
                sum: 0,
                min: Infinity,
                max: -Infinity,
                values: []
            };
        }
        
        const value = item[metrics];
        groups[key].count++;
        groups[key].sum += value;
        groups[key].min = Math.min(groups[key].min, value);
        groups[key].max = Math.max(groups[key].max, value);
        groups[key].values.push(value);
    });
    
    // Calculate averages
    Object.keys(groups).forEach(key => {
        groups[key].average = groups[key].sum / groups[key].count;
    });
    
    return groups;
}"""

calculate_percentile: """function calculate(values, percentile) {
    const sorted = values.sort((a, b) => a - b);
    const index = Math.ceil((percentile / 100) * sorted.length) - 1;
    return sorted[index];
}"""
```

```go
// Go - Data analytics processing handlers
type Analytics struct {
    AggregateData     string `tsk:"aggregate_data"`
    CalculatePercentile string `tsk:"calculate_percentile"`
}

type DataPoint struct {
    ID    int     `json:"id"`
    Group string  `json:"group"`
    Value float64 `json:"value"`
    Date  string  `json:"date"`
}

type AggregationResult struct {
    Count    int       `json:"count"`
    Sum      float64   `json:"sum"`
    Average  float64   `json:"average"`
    Min      float64   `json:"min"`
    Max      float64   `json:"max"`
    Values   []float64 `json:"values"`
}

func (a *Analytics) AggregateData(data []DataPoint, groupBy, metrics string) (map[string]AggregationResult, error) {
    result, err := tusk.ExecuteFunction(a.AggregateData, "aggregate", data, groupBy, metrics)
    if err != nil {
        return nil, fmt.Errorf("failed to aggregate data: %w", err)
    }
    
    if aggregated, ok := result.(map[string]interface{}); ok {
        results := make(map[string]AggregationResult)
        for key, value := range aggregated {
            if group, ok := value.(map[string]interface{}); ok {
                results[key] = AggregationResult{
                    Count:   int(group["count"].(float64)),
                    Sum:     group["sum"].(float64),
                    Average: group["average"].(float64),
                    Min:     group["min"].(float64),
                    Max:     group["max"].(float64),
                    Values:  convertToFloatSlice(group["values"].([]interface{})),
                }
            }
        }
        return results, nil
    }
    
    return nil, errors.New("invalid result type from data aggregation")
}

func (a *Analytics) CalculatePercentile(values []float64, percentile float64) (float64, error) {
    result, err := tusk.ExecuteFunction(a.CalculatePercentile, "calculate", values, percentile)
    if err != nil {
        return 0, fmt.Errorf("failed to calculate percentile: %w", err)
    }
    
    if value, ok := result.(float64); ok {
        return value, nil
    }
    
    return 0, errors.New("invalid result type from percentile calculation")
}

func convertToFloatSlice(interfaceSlice []interface{}) []float64 {
    result := make([]float64, len(interfaceSlice))
    for i, v := range interfaceSlice {
        result[i] = v.(float64)
    }
    return result
}
```

## 🎯 **Best Practices**

### **1. Keep Functions Simple and Focused**

```go
// ✅ Good - Simple, focused functions
[good_functions]
validate_email: """function validate(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}"""

calculate_tax: """function calculate(amount, rate) {
    return amount * (rate / 100);
}"""

// ❌ Bad - Complex, multi-purpose functions
[bad_functions]
do_everything: """function process(data, options) {
    // 100+ lines of complex logic
    // Multiple responsibilities
    // Hard to test and maintain
}"""
```

### **2. Validate Inputs and Handle Errors**

```go
// ✅ Good - Input validation and error handling
[good_validation]
safe_calculation: """function calculate(amount, rate) {
    if (typeof amount !== 'number' || amount < 0) {
        throw new Error('Invalid amount');
    }
    if (typeof rate !== 'number' || rate < 0 || rate > 100) {
        throw new Error('Invalid rate');
    }
    return amount * (rate / 100);
}"""

// ❌ Bad - No validation
[bad_validation]
unsafe_calculation: """function calculate(amount, rate) {
    return amount * (rate / 100); // No validation
}"""
```

### **3. Use Descriptive Function Names**

```go
// ✅ Good - Descriptive names
[good_names]
validate_user_email: """function validate(email) { ... }"""
calculate_sales_tax: """function calculate(amount, rate) { ... }"""
format_currency_usd: """function format(amount) { ... }"""

// ❌ Bad - Unclear names
[bad_names]
func1: """function validate(email) { ... }"""
calc: """function calculate(amount, rate) { ... }"""
fmt: """function format(amount) { ... }"""
```

### **4. Cache Expensive Operations**

```go
// ✅ Good - Caching expensive operations
func (e *Ecommerce) CalculatePriceWithCache(item Item, user User) (*PriceBreakdown, error) {
    cacheKey := fmt.Sprintf("price:%d:%d", item.ID, user.ID)
    
    if cached, exists := e.cache.Get(cacheKey); exists {
        return cached.(*PriceBreakdown), nil
    }
    
    result, err := e.CalculatePrice(item, user)
    if err != nil {
        return nil, err
    }
    
    e.cache.Set(cacheKey, result)
    return result, nil
}

// ❌ Bad - No caching
func (e *Ecommerce) CalculatePrice(item Item, user User) (*PriceBreakdown, error) {
    // Always recalculates, even for same inputs
    return e.calculatePriceInternal(item, user)
}
```

### **5. Document Function Behavior**

```go
// ✅ Good - Documented functions
[documented_functions]
# Calculates sales tax based on amount and rate
# Returns: tax amount as float
calculate_tax: """function calculate(amount, rate) {
    return amount * (rate / 100);
}"""

# Validates email format using regex
# Returns: true if valid, false otherwise
validate_email: """function validate(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}"""

// ❌ Bad - Undocumented functions
[undocumented_functions]
calculate_tax: """function calculate(amount, rate) {
    return amount * (rate / 100);
}"""

validate_email: """function validate(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}"""
```

---

**🎉 You've mastered functions in TuskLang with Go!**

Functions in TuskLang transform static configuration into dynamic, executable systems. With proper function handling, you can build applications that process data, validate inputs, and perform complex calculations directly in your configuration.

**Next Steps:**
- Explore [020-imports-go.md](020-imports-go.md) for modular configuration
- Master [021-operators-go.md](021-operators-go.md) for advanced operators
- Dive into [022-templates-go.md](022-templates-go.md) for dynamic templates

**Remember:** In TuskLang, functions aren't just code—they're configuration with a heartbeat. Use them wisely to create powerful, dynamic systems. 
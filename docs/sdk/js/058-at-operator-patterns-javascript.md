# @operator Patterns - JavaScript

## Overview

The `@operator` patterns in TuskLang provide pre-built, reusable patterns for common data processing scenarios. This is essential for JavaScript applications that need to implement standard patterns like validation, transformation, caching, and error handling without reinventing the wheel.

## Basic Syntax

```tsk
# Common validation pattern
email_validation: @pattern("email_validation", {
  "input": @request.body.email,
  "rules": {"type": "email", "required": true}
})

# Data transformation pattern
user_data_transform: @pattern("user_transform", {
  "input": @json(@http("GET", "https://api.example.com/users")),
  "rules": {"map": {"id": "string", "name": "capitalize", "email": "lowercase"}}
})

# Caching pattern
api_cache_pattern: @pattern("api_cache", {
  "url": "https://api.example.com/data",
  "duration": "10m",
  "key": "api_data_$timestamp"
})
```

## JavaScript Integration

### Node.js Operator Patterns

```javascript
const tusk = require('tusklang');

// Load configuration with operator patterns
const config = tusk.load('patterns.tsk');

// Access pattern results
console.log(config.email_validation); // Validation result
console.log(config.user_data_transform); // Transformed user data
console.log(config.api_cache_pattern); // Cached API data

// Use patterns in application
const patternService = {
  async validateEmail(email) {
    return await tusk.pattern("email_validation", {
      input: email,
      rules: { type: "email", required: true }
    });
  },
  
  async transformUserData(userData) {
    return await tusk.pattern("user_transform", {
      input: userData,
      rules: { map: { id: "string", name: "capitalize", email: "lowercase" } }
    });
  },
  
  async cacheApiData(url, duration) {
    return await tusk.pattern("api_cache", {
      url: url,
      duration: duration,
      key: `api_data_${Date.now()}`
    });
  }
};
```

### Browser Operator Patterns

```javascript
// Load TuskLang configuration
const config = await tusk.load('patterns.tsk');

// Use patterns in frontend
class PatternProcessor {
  constructor() {
    this.validators = config.email_validation;
    this.transformers = config.user_data_transform;
  }
  
  async processWithPattern(patternName, data) {
    const result = await tusk.pattern(patternName, data);
    return result;
  }
  
  async validateInput(input, rules) {
    return await tusk.pattern("input_validation", {
      input: input,
      rules: rules
    });
  }
}
```

## Advanced Usage

### Built-in Patterns

```tsk
# Input validation pattern
input_validation: @pattern("input_validation", {
  "input": @request.body,
  "schema": {
    "type": "object",
    "required": ["email", "password"],
    "properties": {
      "email": {"type": "string", "format": "email"},
      "password": {"type": "string", "minLength": 8}
    }
  }
})

# Data transformation pattern
data_transformation: @pattern("data_transformation", {
  "input": @json(@http("GET", "https://api.example.com/data")),
  "transformations": [
    {"type": "filter", "criteria": {"status": "active"}},
    {"type": "map", "fields": {"id": "string", "name": "capitalize"}},
    {"type": "sort", "field": "name", "direction": "asc"},
    {"type": "limit", "count": 100}
  ]
})

# Error handling pattern
error_handling: @pattern("error_handling", {
  "operation": @http("GET", "https://api.example.com/data"),
  "retry": {"attempts": 3, "backoff": "exponential"},
  "fallback": @http("GET", "https://backup-api.example.com/data"),
  "logging": {"level": "error", "include_stack": true}
})
```

### Custom Pattern Definitions

```tsk
# Custom pattern definitions
custom_patterns: {
  "user_registration": @pattern.define({
    "steps": [
      {"type": "validate", "schema": {"type": "object", "required": ["email", "password", "name"]}},
      {"type": "transform", "rules": {"email": "lowercase", "name": "capitalize", "password": "hash"}},
      {"type": "validate", "rules": {"email": "email_format", "password": "min_length:8"}},
      {"type": "query", "sql": "INSERT INTO users (email, password, name) VALUES (?, ?, ?)", "params": ["$email", "$password", "$name"]},
      {"type": "cache", "key": "user_$id", "duration": "30m"}
    ]
  }),
  
  "api_response": @pattern.define({
    "steps": [
      {"type": "http", "method": "GET"},
      {"type": "json"},
      {"type": "validate", "schema": {"type": "object", "required": ["data", "status"]}},
      {"type": "transform", "rules": {"data": "extract", "format": "standard"}},
      {"type": "cache", "duration": "10m"}
    ]
  })
}
```

### Pattern Composition

```tsk
# Composed patterns
composed_patterns: {
  "user_workflow": @pattern.compose([
    @pattern("user_registration", {"input": @request.body}),
    @pattern("email_verification", {"user_id": "$user_id"}),
    @pattern("welcome_email", {"user": "$user"})
  ]),
  
  "data_pipeline": @pattern.compose([
    @pattern("data_fetch", {"url": "https://api.example.com/data"}),
    @pattern("data_validation", {"schema": "user_schema"}),
    @pattern("data_transformation", {"rules": "standard_transform"}),
    @pattern("data_storage", {"table": "users"})
  ])
}
```

## JavaScript Implementation

### Custom Pattern Manager

```javascript
class TuskPatternManager {
  constructor() {
    this.patterns = new Map();
    this.builtInPatterns = new Map();
    this.initializeBuiltInPatterns();
  }
  
  initializeBuiltInPatterns() {
    // Email validation pattern
    this.builtInPatterns.set("email_validation", {
      execute: async (params) => {
        const { input, rules } = params;
        
        // Validate email format
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(input)) {
          throw new Error("Invalid email format");
        }
        
        // Check if required
        if (rules.required && !input) {
          throw new Error("Email is required");
        }
        
        return { valid: true, email: input.toLowerCase() };
      }
    });
    
    // User transformation pattern
    this.builtInPatterns.set("user_transform", {
      execute: async (params) => {
        const { input, rules } = params;
        
        if (Array.isArray(input)) {
          return input.map(user => this.transformUser(user, rules));
        }
        
        return this.transformUser(input, rules);
      }
    });
    
    // API cache pattern
    this.builtInPatterns.set("api_cache", {
      execute: async (params) => {
        const { url, duration, key } = params;
        
        // Check cache first
        const cacheKey = key || `api_cache_${url}`;
        const cached = await this.getFromCache(cacheKey);
        if (cached) {
          return cached;
        }
        
        // Fetch data
        const response = await fetch(url);
        const data = await response.json();
        
        // Cache data
        await this.setInCache(cacheKey, data, duration);
        
        return data;
      }
    });
    
    // Input validation pattern
    this.builtInPatterns.set("input_validation", {
      execute: async (params) => {
        const { input, schema } = params;
        
        return await this.validateAgainstSchema(input, schema);
      }
    });
    
    // Data transformation pattern
    this.builtInPatterns.set("data_transformation", {
      execute: async (params) => {
        const { input, transformations } = params;
        
        let result = input;
        
        for (const transformation of transformations) {
          result = await this.applyTransformation(result, transformation);
        }
        
        return result;
      }
    });
    
    // Error handling pattern
    this.builtInPatterns.set("error_handling", {
      execute: async (params) => {
        const { operation, retry, fallback, logging } = params;
        
        let lastError;
        
        // Try operation with retries
        for (let i = 0; i < retry.attempts; i++) {
          try {
            return await this.executeOperation(operation);
          } catch (error) {
            lastError = error;
            
            // Log error
            if (logging) {
              await this.logError(error, logging);
            }
            
            // Try fallback on last attempt
            if (i === retry.attempts - 1 && fallback) {
              try {
                return await this.executeOperation(fallback);
              } catch (fallbackError) {
                throw new Error(`Both primary and fallback operations failed: ${lastError.message}, ${fallbackError.message}`);
              }
            }
            
            // Wait before retry
            if (i < retry.attempts - 1) {
              const delay = retry.backoff === 'exponential' ? Math.pow(2, i) * 1000 : 1000;
              await this.delay(delay);
            }
          }
        }
        
        throw lastError;
      }
    });
  }
  
  async executePattern(patternName, params) {
    // Check built-in patterns first
    if (this.builtInPatterns.has(patternName)) {
      const pattern = this.builtInPatterns.get(patternName);
      return await pattern.execute(params);
    }
    
    // Check custom patterns
    if (this.patterns.has(patternName)) {
      const pattern = this.patterns.get(patternName);
      return await this.executeCustomPattern(pattern, params);
    }
    
    throw new Error(`Pattern '${patternName}' not found`);
  }
  
  async executeCustomPattern(pattern, params) {
    const { steps } = pattern;
    let result = params.input || params;
    
    for (const step of steps) {
      result = await this.executeStep(step, result, params);
    }
    
    return result;
  }
  
  async executeStep(step, data, params) {
    const { type, ...stepParams } = step;
    
    switch (type) {
      case 'validate':
        return await this.executeValidation(stepParams, data);
      case 'transform':
        return await this.executeTransform(stepParams, data);
      case 'query':
        return await this.executeQuery(stepParams, data);
      case 'cache':
        return await this.executeCache(stepParams, data);
      case 'http':
        return await this.executeHttp(stepParams, data);
      case 'json':
        return await this.executeJson(stepParams, data);
      default:
        throw new Error(`Unknown step type: ${type}`);
    }
  }
  
  async executeValidation(params, data) {
    const { schema, rules } = params;
    
    if (schema) {
      const isValid = await this.validateAgainstSchema(data, schema);
      if (!isValid) {
        throw new Error(`Validation failed for schema: ${JSON.stringify(schema)}`);
      }
    }
    
    if (rules) {
      const isValid = await this.validateAgainstRules(data, rules);
      if (!isValid) {
        throw new Error(`Validation failed for rules: ${JSON.stringify(rules)}`);
      }
    }
    
    return data;
  }
  
  async executeTransform(params, data) {
    const { rules, map, extract, remove, add } = params;
    
    if (extract) {
      return data[extract];
    }
    
    if (map) {
      return this.applyMapTransform(data, map);
    }
    
    if (remove) {
      return this.applyRemoveTransform(data, remove);
    }
    
    if (add) {
      return this.applyAddTransform(data, add);
    }
    
    if (rules) {
      return await this.applyTransformRules(data, rules);
    }
    
    return data;
  }
  
  async executeQuery(params, data) {
    const { sql, params: queryParams } = params;
    
    // Replace placeholders with actual values
    const processedSql = this.replacePlaceholders(sql, data);
    const processedParams = this.processQueryParams(queryParams, data);
    
    // Execute query (simplified)
    return await this.executeDatabaseQuery(processedSql, processedParams);
  }
  
  async executeCache(params, data) {
    const { key, duration } = params;
    const cacheKey = this.replacePlaceholders(key, data);
    
    // Check cache first
    const cached = await this.getFromCache(cacheKey);
    if (cached) {
      return cached;
    }
    
    // Store in cache
    await this.setInCache(cacheKey, data, duration);
    return data;
  }
  
  async executeHttp(params, data) {
    const { method, url } = params;
    const processedUrl = this.replacePlaceholders(url, data);
    
    const response = await fetch(processedUrl, {
      method: method || 'GET'
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    return await response.text();
  }
  
  async executeJson(params, data) {
    if (typeof data === 'string') {
      return JSON.parse(data);
    }
    return data;
  }
  
  transformUser(user, rules) {
    const result = {};
    
    Object.keys(rules.map || {}).forEach(key => {
      const transformation = rules.map[key];
      
      switch (transformation) {
        case 'string':
          result[key] = String(user[key]);
          break;
        case 'capitalize':
          result[key] = String(user[key]).charAt(0).toUpperCase() + String(user[key]).slice(1).toLowerCase();
          break;
        case 'lowercase':
          result[key] = String(user[key]).toLowerCase();
          break;
        default:
          result[key] = user[key];
      }
    });
    
    return result;
  }
  
  applyMapTransform(data, map) {
    if (Array.isArray(data)) {
      return data.map(item => this.transformUser(item, { map }));
    }
    
    return this.transformUser(data, { map });
  }
  
  applyRemoveTransform(data, remove) {
    if (Array.isArray(data)) {
      return data.map(item => this.removeFields(item, remove));
    }
    
    return this.removeFields(data, remove);
  }
  
  removeFields(item, fields) {
    const result = { ...item };
    
    fields.forEach(field => {
      delete result[field];
    });
    
    return result;
  }
  
  applyAddTransform(data, add) {
    if (Array.isArray(data)) {
      return data.map(item => this.addFields(item, add));
    }
    
    return this.addFields(data, add);
  }
  
  addFields(item, fields) {
    const result = { ...item };
    
    Object.keys(fields).forEach(key => {
      const value = fields[key];
      
      if (typeof value === 'string' && value.startsWith('$')) {
        // Handle placeholders
        const placeholder = value.substring(1);
        result[key] = item[placeholder] || value;
      } else {
        result[key] = value;
      }
    });
    
    return result;
  }
  
  replacePlaceholders(template, data) {
    return template.replace(/\$(\w+)/g, (match, key) => {
      return data[key] || match;
    });
  }
  
  processQueryParams(params, data) {
    if (Array.isArray(params)) {
      return params.map(param => {
        if (typeof param === 'string' && param.startsWith('$')) {
          const key = param.substring(1);
          return data[key];
        }
        return param;
      });
    }
    
    return params;
  }
  
  async validateAgainstSchema(data, schema) {
    // Simplified schema validation
    if (schema.type === 'object') {
      if (typeof data !== 'object' || data === null) {
        return false;
      }
      
      if (schema.required) {
        for (const field of schema.required) {
          if (!(field in data)) {
            return false;
          }
        }
      }
      
      if (schema.properties) {
        for (const [field, fieldSchema] of Object.entries(schema.properties)) {
          if (data[field] !== undefined) {
            const fieldValid = await this.validateAgainstSchema(data[field], fieldSchema);
            if (!fieldValid) {
              return false;
            }
          }
        }
      }
    }
    
    return true;
  }
  
  async validateAgainstRules(data, rules) {
    // Simplified rules validation
    for (const [rule, value] of Object.entries(rules)) {
      switch (rule) {
        case 'email_format':
          const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
          if (!emailRegex.test(data)) {
            return false;
          }
          break;
        case 'min_length':
          if (String(data).length < value) {
            return false;
          }
          break;
        default:
          // Unknown rule
          break;
      }
    }
    
    return true;
  }
  
  async getFromCache(key) {
    // Simplified cache implementation
    const cached = localStorage.getItem(key);
    if (cached) {
      const { data, timestamp } = JSON.parse(cached);
      if (Date.now() - timestamp < 300000) { // 5 minutes
        return data;
      }
    }
    return null;
  }
  
  async setInCache(key, data, duration) {
    // Simplified cache implementation
    const cacheData = {
      data: data,
      timestamp: Date.now()
    };
    localStorage.setItem(key, JSON.stringify(cacheData));
  }
  
  async executeOperation(operation) {
    if (typeof operation === 'function') {
      return await operation();
    }
    
    if (operation.type === 'http') {
      return await this.executeHttp(operation, {});
    }
    
    throw new Error(`Unknown operation type: ${operation.type}`);
  }
  
  async logError(error, logging) {
    const logEntry = {
      level: logging.level || 'error',
      message: error.message,
      stack: logging.include_stack ? error.stack : undefined,
      timestamp: new Date().toISOString()
    };
    
    console.error('Pattern Error:', logEntry);
  }
  
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
  
  registerPattern(name, pattern) {
    this.patterns.set(name, pattern);
  }
}
```

### TypeScript Support

```typescript
interface PatternParams {
  [key: string]: any;
}

interface PatternStep {
  type: string;
  [key: string]: any;
}

interface CustomPattern {
  steps: PatternStep[];
}

interface BuiltInPattern {
  execute(params: PatternParams): Promise<any>;
}

class TypedPatternManager {
  private patterns: Map<string, CustomPattern>;
  private builtInPatterns: Map<string, BuiltInPattern>;
  
  constructor() {
    this.patterns = new Map();
    this.builtInPatterns = new Map();
    this.initializeBuiltInPatterns();
  }
  
  async executePattern(patternName: string, params: PatternParams): Promise<any> {
    // Implementation similar to JavaScript version
    return params;
  }
  
  registerPattern(name: string, pattern: CustomPattern): void {
    this.patterns.set(name, pattern);
  }
  
  private initializeBuiltInPatterns(): void {
    // Implementation similar to JavaScript version
  }
}
```

## Real-World Examples

### Common Validation Patterns

```tsk
# Common validation patterns
validation_patterns: {
  "user_registration": @pattern.define({
    "steps": [
      {"type": "validate", "schema": {"type": "object", "required": ["email", "password", "name"]}},
      {"type": "validate", "rules": {"email": "email_format", "password": "min_length:8", "name": "min_length:2"}},
      {"type": "transform", "rules": {"email": "lowercase", "name": "capitalize"}}
    ]
  }),
  
  "api_request": @pattern.define({
    "steps": [
      {"type": "validate", "schema": {"type": "object", "required": ["method", "url"]}},
      {"type": "validate", "rules": {"method": "allowed_methods", "url": "valid_url"}},
      {"type": "transform", "rules": {"sanitize": true}}
    ]
  }),
  
  "file_upload": @pattern.define({
    "steps": [
      {"type": "validate", "schema": {"type": "object", "required": ["file", "type", "size"]}},
      {"type": "validate", "rules": {"type": "allowed_types", "size": "max_size:10MB"}},
      {"type": "transform", "rules": {"compress": true, "encrypt": true}}
    ]
  })
}
```

### Data Processing Patterns

```tsk
# Data processing patterns
processing_patterns: {
  "user_data_processing": @pattern.define({
    "steps": [
      {"type": "transform", "rules": {"map": {"id": "string", "name": "capitalize", "email": "lowercase"}}},
      {"type": "transform", "rules": {"remove": ["password", "token", "secret"]}},
      {"type": "transform", "rules": {"add": {"processed_at": "@date.now()", "source": "api"}}},
      {"type": "validate", "schema": {"type": "object", "required": ["id", "name", "email"]}},
      {"type": "cache", "key": "user_$id", "duration": "10m"}
    ]
  }),
  
  "api_response_processing": @pattern.define({
    "steps": [
      {"type": "json"},
      {"type": "validate", "schema": {"type": "object", "required": ["data", "status"]}},
      {"type": "validate", "rules": {"status": "success"}},
      {"type": "transform", "rules": {"data": "extract"}},
      {"type": "transform", "rules": {"format": "standard"}},
      {"type": "cache", "duration": "5m"}
    ]
  })
}
```

### Error Handling Patterns

```tsk
# Error handling patterns
error_patterns: {
  "robust_api_call": @pattern.define({
    "steps": [
      {"type": "retry", "attempts": 3, "backoff": "exponential"},
      {"type": "http", "method": "GET"},
      {"type": "fallback", "operation": {"type": "http", "method": "GET", "url": "https://backup-api.example.com/data"}},
      {"type": "json"},
      {"type": "validate", "schema": {"type": "object"}},
      {"type": "cache", "duration": "10m"}
    ]
  }),
  
  "graceful_degradation": @pattern.define({
    "steps": [
      {"type": "http", "method": "GET", "url": "https://api.example.com/premium-data"},
      {"type": "fallback", "operation": {"type": "http", "method": "GET", "url": "https://api.example.com/basic-data"}},
      {"type": "json"},
      {"type": "transform", "rules": {"format": "standard"}},
      {"type": "cache", "duration": "10m"}
    ]
  })
}
```

## Performance Considerations

### Pattern Caching

```tsk
# Cached patterns
cached_patterns: {
  "static_data": @pattern.define({
    "steps": [
      {"type": "cache", "key": "static_data", "duration": "24h"},
      {"type": "http", "method": "GET", "url": "https://api.example.com/static-data"},
      {"type": "json"},
      {"type": "transform", "rules": {"format": "standard"}},
      {"type": "cache", "key": "static_data", "duration": "24h"}
    ]
  })
}
```

### Efficient Patterns

```javascript
// Implement efficient pattern execution with caching
class EfficientPatternManager extends TuskPatternManager {
  constructor() {
    super();
    this.resultCache = new Map();
  }
  
  async executePattern(patternName, params) {
    const cacheKey = this.generateResultCacheKey(patternName, params);
    
    // Check result cache
    if (this.resultCache.has(cacheKey)) {
      const cached = this.resultCache.get(cacheKey);
      if (Date.now() - cached.timestamp < 300000) { // 5 minutes
        return cached.result;
      }
    }
    
    const result = await super.executePattern(patternName, params);
    
    // Cache result
    this.resultCache.set(cacheKey, {
      result: result,
      timestamp: Date.now()
    });
    
    return result;
  }
  
  generateResultCacheKey(patternName, params) {
    return `pattern_${patternName}_${JSON.stringify(params)}`;
  }
}
```

## Security Notes

- **Input Validation**: Always validate input in patterns
- **Error Handling**: Implement proper error handling for each step
- **Data Sanitization**: Sanitize data in transformation steps

```javascript
// Secure pattern implementation
class SecurePatternManager extends TuskPatternManager {
  constructor() {
    super();
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }
  
  async executeStep(step, data, params) {
    // Sanitize sensitive data before processing
    if (step.type === 'http' || step.type === 'json') {
      data = this.sanitizeSensitiveData(data);
    }
    
    return await super.executeStep(step, data, params);
  }
  
  sanitizeSensitiveData(data) {
    if (typeof data === 'object') {
      const sanitized = {};
      
      Object.keys(data).forEach(key => {
        if (this.sensitivePatterns.some(pattern => pattern.test(key))) {
          sanitized[key] = '[REDACTED]';
        } else {
          sanitized[key] = this.sanitizeSensitiveData(data[key]);
        }
      });
      
      return sanitized;
    }
    
    return data;
  }
}
```

## Best Practices

1. **Reusability**: Create reusable patterns for common operations
2. **Validation**: Always validate input in patterns
3. **Error Handling**: Implement proper error handling for each step
4. **Caching**: Use caching strategically in patterns
5. **Testing**: Test patterns thoroughly
6. **Documentation**: Document all patterns and their purpose

## Next Steps

- Master [@operator optimization](./059-at-operator-optimization-javascript.md) for performance tuning
- Learn about [@operator testing](./060-at-operator-testing-javascript.md) for quality assurance
- Explore [@operator deployment](./061-at-operator-deployment-javascript.md) for production readiness 
# @operator Chaining - JavaScript

## Overview

The `@operator` chaining in TuskLang allows you to combine multiple operators in sequence to create complex, powerful operations. This is essential for JavaScript applications that need to perform sophisticated data transformations, validations, and processing pipelines in a clean, readable way.

## Basic Syntax

```tsk
# Simple operator chaining
processed_data: @json(@http("GET", "https://api.example.com/data")).users[0].name
cached_response: @cache("5m", @http("GET", "https://api.example.com/users"))
validated_input: @validate(@env("USER_INPUT"), {"type": "email"})

# Complex chaining
user_profile: @cache("10m", @json(@http("GET", "https://api.example.com/users/$user_id")).profile)
```

## JavaScript Integration

### Node.js Operator Chaining

```javascript
const tusk = require('tusklang');

// Load configuration with chained operators
const config = tusk.load('chaining.tsk');

// Access chained results
console.log(config.processed_data); // "John Doe"
console.log(config.cached_response); // Cached HTTP response
console.log(config.validated_input); // Validated email

// Use chained operations in application
const userService = {
  async getUserProfile(userId) {
    return await tusk.chain([
      tusk.http("GET", `https://api.example.com/users/${userId}`),
      tusk.json(),
      tusk.cache("10m"),
      tusk.validate({"type": "object"})
    ]);
  },
  
  async processUserData(userData) {
    return await tusk.chain([
      tusk.validate(userData, {"type": "object"}),
      tusk.transform(userData, {"sanitize": true}),
      tusk.cache("5m"),
      tusk.metrics("user_processing", 1)
    ]);
  }
};
```

### Browser Operator Chaining

```javascript
// Load TuskLang configuration
const config = await tusk.load('chaining.tsk');

// Use chained operations in frontend
class DataProcessor {
  constructor() {
    this.apiUrl = config.api_url;
  }
  
  async processData(data) {
    const result = await tusk.chain([
      tusk.validate(data, { type: "object" }),
      tusk.transform(data, { sanitize: true }),
      tusk.cache("5m"),
      tusk.metrics("data_processing", 1)
    ]);
    
    return result;
  }
  
  async fetchAndProcess(url) {
    return await tusk.chain([
      tusk.http("GET", url),
      tusk.json(),
      tusk.cache("10m"),
      tusk.transform(null, { format: "standard" })
    ]);
  }
}
```

## Advanced Usage

### Complex Data Pipelines

```tsk
# Multi-step data processing pipeline
data_pipeline: @chain([
  @http("GET", "https://api.example.com/raw-data"),
  @json(),
  @transform({"filter": "active_users", "sort": "name"}),
  @validate({"type": "array", "min_length": 1}),
  @cache("15m"),
  @metrics("data_pipeline", 1)
])

# Conditional chaining
conditional_pipeline: @env("NODE_ENV") == "production" ? 
  @chain([@http("GET", "https://prod-api.example.com/data"), @json(), @cache("1h")]) :
  @chain([@http("GET", "https://dev-api.example.com/data"), @json(), @cache("5m")])
```

### Error Handling in Chains

```tsk
# Chaining with error handling
robust_pipeline: @chain([
  @http("GET", "https://api.example.com/data"),
  @json(),
  @fallback(@env("BACKUP_DATA_URL")),
  @validate({"type": "object"}),
  @cache("10m")
])

# Chaining with retry logic
retry_pipeline: @chain([
  @retry(3, @http("GET", "https://api.example.com/data")),
  @json(),
  @cache("5m"),
  @metrics("api_success", 1)
])
```

### Performance Optimization Chains

```tsk
# Optimized data processing
optimized_pipeline: @chain([
  @cache("1h", @http("GET", "https://api.example.com/static-data")),
  @json(),
  @optimize({"compression": true, "minification": true}),
  @cache("24h")
])

# Parallel processing chain
parallel_pipeline: @chain([
  @parallel([
    @http("GET", "https://api.example.com/users"),
    @http("GET", "https://api.example.com/posts"),
    @http("GET", "https://api.example.com/comments")
  ]),
  @merge({"users": 0, "posts": 1, "comments": 2}),
  @cache("10m")
])
```

## JavaScript Implementation

### Custom Chain Manager

```javascript
class TuskChainManager {
  constructor() {
    this.operators = new Map();
    this.middleware = [];
    this.errorHandlers = new Map();
  }
  
  async execute(chain, initialData = null) {
    let result = initialData;
    const context = {
      startTime: Date.now(),
      steps: [],
      errors: []
    };
    
    try {
      for (let i = 0; i < chain.length; i++) {
        const operator = chain[i];
        const stepStart = Date.now();
        
        try {
          // Execute middleware before operator
          result = await this.executeMiddleware('before', operator, result, context);
          
          // Execute the operator
          result = await this.executeOperator(operator, result, context);
          
          // Execute middleware after operator
          result = await this.executeMiddleware('after', operator, result, context);
          
          // Record step
          context.steps.push({
            index: i,
            operator: operator.type || 'unknown',
            duration: Date.now() - stepStart,
            success: true
          });
          
        } catch (error) {
          // Handle operator error
          result = await this.handleOperatorError(operator, error, result, context);
          
          context.steps.push({
            index: i,
            operator: operator.type || 'unknown',
            duration: Date.now() - stepStart,
            success: false,
            error: error.message
          });
          
          context.errors.push(error);
        }
      }
      
      return result;
      
    } catch (error) {
      // Handle chain error
      return await this.handleChainError(error, context);
    }
  }
  
  async executeOperator(operator, data, context) {
    const operatorType = operator.type || operator.constructor.name;
    
    switch (operatorType) {
      case 'http':
        return await this.executeHttpOperator(operator, data);
      case 'json':
        return await this.executeJsonOperator(operator, data);
      case 'cache':
        return await this.executeCacheOperator(operator, data);
      case 'validate':
        return await this.executeValidateOperator(operator, data);
      case 'transform':
        return await this.executeTransformOperator(operator, data);
      case 'metrics':
        return await this.executeMetricsOperator(operator, data);
      case 'retry':
        return await this.executeRetryOperator(operator, data);
      case 'fallback':
        return await this.executeFallbackOperator(operator, data);
      case 'parallel':
        return await this.executeParallelOperator(operator, data);
      case 'merge':
        return await this.executeMergeOperator(operator, data);
      default:
        throw new Error(`Unknown operator type: ${operatorType}`);
    }
  }
  
  async executeHttpOperator(operator, data) {
    const { method, url, body, options } = operator;
    const response = await fetch(url, {
      method: method || 'GET',
      body: body,
      ...options
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    return await response.text();
  }
  
  async executeJsonOperator(operator, data) {
    if (typeof data === 'string') {
      return JSON.parse(data);
    }
    return data;
  }
  
  async executeCacheOperator(operator, data) {
    const { duration, key } = operator;
    const cacheKey = key || this.generateCacheKey(data);
    
    // Check cache first
    const cached = await this.getFromCache(cacheKey);
    if (cached) {
      return cached;
    }
    
    // Store in cache
    await this.setInCache(cacheKey, data, duration);
    return data;
  }
  
  async executeValidateOperator(operator, data) {
    const { schema, rules } = operator;
    
    if (schema) {
      // Validate against schema
      const isValid = await this.validateAgainstSchema(data, schema);
      if (!isValid) {
        throw new Error(`Validation failed for schema: ${schema}`);
      }
    }
    
    if (rules) {
      // Validate against rules
      const isValid = await this.validateAgainstRules(data, rules);
      if (!isValid) {
        throw new Error(`Validation failed for rules: ${JSON.stringify(rules)}`);
      }
    }
    
    return data;
  }
  
  async executeTransformOperator(operator, data) {
    const { rules, functions } = operator;
    
    if (rules) {
      return await this.applyTransformRules(data, rules);
    }
    
    if (functions) {
      return await this.applyTransformFunctions(data, functions);
    }
    
    return data;
  }
  
  async executeMetricsOperator(operator, data) {
    const { name, value, tags } = operator;
    await this.recordMetric(name, value || 1, tags);
    return data;
  }
  
  async executeRetryOperator(operator, data) {
    const { attempts, operator: targetOperator } = operator;
    let lastError;
    
    for (let i = 0; i < attempts; i++) {
      try {
        return await this.executeOperator(targetOperator, data);
      } catch (error) {
        lastError = error;
        if (i < attempts - 1) {
          await this.delay(Math.pow(2, i) * 1000); // Exponential backoff
        }
      }
    }
    
    throw lastError;
  }
  
  async executeFallbackOperator(operator, data) {
    try {
      return await this.executeOperator(operator.operator, data);
    } catch (error) {
      return await this.executeOperator(operator.fallback, data);
    }
  }
  
  async executeParallelOperator(operator, data) {
    const { operators } = operator;
    const promises = operators.map(op => this.executeOperator(op, data));
    return await Promise.all(promises);
  }
  
  async executeMergeOperator(operator, data) {
    const { mapping } = operator;
    const result = {};
    
    Object.keys(mapping).forEach(key => {
      const index = mapping[key];
      if (Array.isArray(data) && data[index]) {
        result[key] = data[index];
      }
    });
    
    return result;
  }
  
  async executeMiddleware(type, operator, data, context) {
    for (const middleware of this.middleware) {
      if (middleware.type === type) {
        data = await middleware.execute(operator, data, context);
      }
    }
    return data;
  }
  
  async handleOperatorError(operator, error, data, context) {
    const errorHandler = this.errorHandlers.get(operator.type);
    if (errorHandler) {
      return await errorHandler(operator, error, data, context);
    }
    throw error;
  }
  
  async handleChainError(error, context) {
    console.error('Chain execution failed:', error);
    console.error('Context:', context);
    throw error;
  }
  
  addMiddleware(middleware) {
    this.middleware.push(middleware);
  }
  
  addErrorHandler(operatorType, handler) {
    this.errorHandlers.set(operatorType, handler);
  }
  
  generateCacheKey(data) {
    return `chain_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
  
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
```

### TypeScript Support

```typescript
interface ChainOperator {
  type: string;
  [key: string]: any;
}

interface ChainContext {
  startTime: number;
  steps: Array<{
    index: number;
    operator: string;
    duration: number;
    success: boolean;
    error?: string;
  }>;
  errors: Error[];
}

interface Middleware {
  type: 'before' | 'after';
  execute(operator: ChainOperator, data: any, context: ChainContext): Promise<any>;
}

interface ErrorHandler {
  (operator: ChainOperator, error: Error, data: any, context: ChainContext): Promise<any>;
}

class TypedChainManager {
  private operators: Map<string, any>;
  private middleware: Middleware[];
  private errorHandlers: Map<string, ErrorHandler>;
  
  constructor() {
    this.operators = new Map();
    this.middleware = [];
    this.errorHandlers = new Map();
  }
  
  async execute(chain: ChainOperator[], initialData?: any): Promise<any> {
    // Implementation similar to JavaScript version
    return initialData;
  }
  
  async executeOperator(operator: ChainOperator, data: any, context: ChainContext): Promise<any> {
    // Implementation similar to JavaScript version
    return data;
  }
  
  addMiddleware(middleware: Middleware): void {
    this.middleware.push(middleware);
  }
  
  addErrorHandler(operatorType: string, handler: ErrorHandler): void {
    this.errorHandlers.set(operatorType, handler);
  }
}
```

## Real-World Examples

### API Data Processing Pipeline

```tsk
# Complete API data processing pipeline
api_pipeline: @chain([
  @http("GET", "https://api.example.com/users"),
  @json(),
  @validate({"type": "array", "min_length": 1}),
  @transform({"filter": "active", "sort": "name", "limit": 100}),
  @cache("10m"),
  @metrics("api_users_processed", 1)
])

# User authentication pipeline
auth_pipeline: @chain([
  @validate(@request.body, {"type": "object", "required": ["email", "password"]}),
  @transform({"sanitize": true, "hash_password": true}),
  @query("SELECT * FROM users WHERE email = ?", [$email]),
  @validate({"type": "array", "length": 1}),
  @transform({"extract": "user", "remove_password": true}),
  @cache("5m", "user_session_$user_id")
])
```

### Data Transformation Pipeline

```tsk
# Data transformation and validation
data_transformation: @chain([
  @json(@http("GET", "https://api.example.com/raw-data")),
  @validate({"type": "object", "required": ["items"]}),
  @transform({
    "items": "map",
    "transform": {
      "id": "string",
      "name": "capitalize",
      "email": "lowercase",
      "created_at": "date"
    }
  }),
  @validate({"type": "array", "each": {"type": "object"}}),
  @cache("15m")
])

# File processing pipeline
file_pipeline: @chain([
  @http("GET", "https://api.example.com/files/$file_id"),
  @transform({"decode": "base64", "parse": "json"}),
  @validate({"type": "object"}),
  @transform({"compress": true, "encrypt": true}),
  @cache("1h")
])
```

### Error Recovery Pipeline

```tsk
# Robust data fetching with fallbacks
robust_fetch: @chain([
  @retry(3, @http("GET", "https://primary-api.example.com/data")),
  @fallback(@http("GET", "https://backup-api.example.com/data")),
  @json(),
  @validate({"type": "object"}),
  @cache("5m")
])

# Graceful degradation pipeline
graceful_pipeline: @chain([
  @http("GET", "https://api.example.com/premium-data"),
  @fallback(@http("GET", "https://api.example.com/basic-data")),
  @json(),
  @transform({"format": "standard"}),
  @cache("10m")
])
```

## Performance Considerations

### Chain Optimization

```tsk
# Optimized chain with caching
optimized_chain: @chain([
  @cache("1h", @http("GET", "https://api.example.com/static-data")),
  @json(),
  @transform({"minify": true}),
  @cache("24h")
])
```

### Parallel Processing

```javascript
// Implement parallel processing for better performance
class ParallelChainManager extends TuskChainManager {
  async executeParallel(operators, data) {
    const promises = operators.map(operator => 
      this.executeOperator(operator, data)
    );
    
    return await Promise.all(promises);
  }
  
  async executeOptimized(chain, data) {
    // Group independent operators for parallel execution
    const groups = this.groupIndependentOperators(chain);
    
    let result = data;
    for (const group of groups) {
      if (group.length === 1) {
        result = await this.executeOperator(group[0], result);
      } else {
        result = await this.executeParallel(group, result);
      }
    }
    
    return result;
  }
  
  groupIndependentOperators(chain) {
    // Implementation to group operators that can run in parallel
    return [chain]; // Simplified for example
  }
}
```

## Security Notes

- **Input Validation**: Always validate input at the beginning of chains
- **Error Handling**: Implement proper error handling for each operator
- **Data Sanitization**: Sanitize data in transformation steps

```javascript
// Secure chain implementation
class SecureChainManager extends TuskChainManager {
  constructor() {
    super();
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }
  
  async executeOperator(operator, data, context) {
    // Sanitize sensitive data before processing
    if (operator.type === 'http' || operator.type === 'json') {
      data = this.sanitizeSensitiveData(data);
    }
    
    return await super.executeOperator(operator, data, context);
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

1. **Validation First**: Always validate input at the beginning of chains
2. **Error Handling**: Implement proper error handling for each operator
3. **Caching**: Use caching strategically to improve performance
4. **Monitoring**: Add metrics to track chain performance
5. **Testing**: Test each operator and chain combination thoroughly
6. **Documentation**: Document complex chains and their purpose

## Next Steps

- Master [@operator nesting](./056-at-operator-nesting-javascript.md) for advanced patterns
- Learn about [@operator composition](./057-at-operator-composition-javascript.md) for reusable components
- Explore [@operator patterns](./058-at-operator-patterns-javascript.md) for common use cases 
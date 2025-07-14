# @operator Composition - JavaScript

## Overview

The `@operator` composition in TuskLang allows you to create reusable, composable operator components that can be combined and reused across different parts of your application. This is essential for JavaScript applications that need to build maintainable, modular, and reusable data processing pipelines.

## Basic Syntax

```tsk
# Simple operator composition
user_validation: @compose([
  @validate({"type": "object", "required": ["email", "password"]}),
  @transform({"email": "lowercase", "password": "hash"}),
  @validate({"email": "email_format", "password": "min_length:8"})
])

# Composable data processing
data_processor: @compose([
  @json(),
  @validate({"type": "object"}),
  @transform({"format": "standard"}),
  @cache("10m")
])

# Reusable API operations
api_fetcher: @compose([
  @http("GET", "https://api.example.com/data"),
  @json(),
  @cache("5m")
])
```

## JavaScript Integration

### Node.js Operator Composition

```javascript
const tusk = require('tusklang');

// Load configuration with composed operators
const config = tusk.load('composition.tsk');

// Access composed results
console.log(config.user_validation); // Composed validation function
console.log(config.data_processor); // Composed data processor
console.log(config.api_fetcher); // Composed API fetcher

// Use composed operations in application
const userService = {
  async validateUser(userData) {
    return await tusk.compose([
      tusk.validate(userData, {"type": "object", "required": ["email", "password"]}),
      tusk.transform(userData, {"email": "lowercase", "password": "hash"}),
      tusk.validate(userData, {"email": "email_format", "password": "min_length:8"})
    ]);
  },
  
  async processUserData(data) {
    return await tusk.compose([
      tusk.json(data),
      tusk.validate(data, {"type": "object"}),
      tusk.transform(data, {"format": "standard"}),
      tusk.cache("10m", data)
    ]);
  }
};
```

### Browser Operator Composition

```javascript
// Load TuskLang configuration
const config = await tusk.load('composition.tsk');

// Use composed operations in frontend
class ComposableDataProcessor {
  constructor() {
    this.validators = config.user_validation;
    this.processors = config.data_processor;
  }
  
  async validateAndProcess(data) {
    const result = await tusk.compose([
      tusk.validate(data, { type: "object" }),
      tusk.transform(data, { sanitize: true }),
      tusk.cache("5m", data)
    ]);
    
    return result;
  }
  
  async createComposablePipeline(operations) {
    return await tusk.compose(operations);
  }
}
```

## Advanced Usage

### Complex Compositions

```tsk
# Complex data processing composition
complex_processor: @compose([
  @http("GET", "https://api.example.com/raw-data"),
  @json(),
  @validate({"type": "object", "required": ["items"]}),
  @transform({"items": "extract"}),
  @filter({"status": "active"}),
  @transform({"map": {"id": "string", "name": "capitalize"}}),
  @sort({"name": "asc"}),
  @limit(100),
  @cache("15m")
])

# Conditional composition
conditional_processor: @env("NODE_ENV") == "production" ? 
  @compose([@http("GET", "https://prod-api.example.com/data"), @json(), @cache("1h")]) :
  @compose([@http("GET", "https://dev-api.example.com/data"), @json(), @cache("5m")])

# Composable with error handling
robust_processor: @compose([
  @retry(3, @http("GET", "https://api.example.com/data")),
  @fallback(@http("GET", "https://backup-api.example.com/data")),
  @json(),
  @validate({"type": "object"}),
  @cache("10m")
])
```

### Reusable Compositions

```tsk
# Reusable validation compositions
validation_compositions: {
  "user_input": @compose([
    @validate({"type": "object", "required": ["email", "password"]}),
    @transform({"email": "lowercase", "password": "hash"}),
    @validate({"email": "email_format", "password": "min_length:8"})
  ]),
  
  "api_response": @compose([
    @validate({"type": "object", "required": ["data", "status"]}),
    @validate({"status": "success"}),
    @transform({"data": "extract"})
  ]),
  
  "file_upload": @compose([
    @validate({"type": "object", "required": ["file", "type"]}),
    @validate({"type": "allowed_types", "max_size": "10MB"}),
    @transform({"sanitize": true, "compress": true})
  ])
}

# Reusable transformation compositions
transformation_compositions: {
  "user_data": @compose([
    @transform({"map": {"id": "string", "name": "capitalize", "email": "lowercase"}}),
    @transform({"remove": ["password", "token"]}),
    @transform({"add": {"processed_at": "@date.now()"}})
  ]),
  
  "api_data": @compose([
    @transform({"format": "standard"}),
    @transform({"add": {"source": "api", "timestamp": "@date.now()"}}),
    @transform({"compress": true})
  ])
}
```

### Composition Patterns

```tsk
# Pipeline composition pattern
pipeline_composition: @compose([
  @stage("fetch", @http("GET", "https://api.example.com/data")),
  @stage("parse", @json()),
  @stage("validate", @validate({"type": "object"})),
  @stage("transform", @transform({"format": "standard"})),
  @stage("cache", @cache("10m"))
])

# Middleware composition pattern
middleware_composition: @compose([
  @middleware("auth", @validate({"auth": "required"})),
  @middleware("rate_limit", @rate_limit({"requests": 100, "window": "1m"})),
  @middleware("logging", @log({"level": "info"})),
  @handler("process_request")
])
```

## JavaScript Implementation

### Custom Composition Manager

```javascript
class TuskCompositionManager {
  constructor() {
    this.compositions = new Map();
    this.operators = new Map();
    this.middleware = new Map();
  }
  
  async compose(operations, initialData = null) {
    const compositionId = this.generateCompositionId(operations);
    
    // Check if composition already exists
    if (this.compositions.has(compositionId)) {
      const composition = this.compositions.get(compositionId);
      return await composition.execute(initialData);
    }
    
    // Create new composition
    const composition = new Composition(operations, this);
    this.compositions.set(compositionId, composition);
    
    return await composition.execute(initialData);
  }
  
  async executeComposition(name, data) {
    if (!this.compositions.has(name)) {
      throw new Error(`Composition '${name}' not found`);
    }
    
    const composition = this.compositions.get(name);
    return await composition.execute(data);
  }
  
  registerComposition(name, operations) {
    const composition = new Composition(operations, this);
    this.compositions.set(name, composition);
  }
  
  registerOperator(name, operator) {
    this.operators.set(name, operator);
  }
  
  registerMiddleware(name, middleware) {
    this.middleware.set(name, middleware);
  }
  
  generateCompositionId(operations) {
    return `comp_${JSON.stringify(operations)}`;
  }
}

class Composition {
  constructor(operations, manager) {
    this.operations = operations;
    this.manager = manager;
    this.cache = new Map();
  }
  
  async execute(initialData = null) {
    let result = initialData;
    const context = {
      startTime: Date.now(),
      operations: [],
      errors: []
    };
    
    try {
      for (let i = 0; i < this.operations.length; i++) {
        const operation = this.operations[i];
        const stepStart = Date.now();
        
        try {
          // Execute the operation
          result = await this.executeOperation(operation, result, context);
          
          // Record successful operation
          context.operations.push({
            index: i,
            operation: operation.type || 'unknown',
            duration: Date.now() - stepStart,
            success: true
          });
          
        } catch (error) {
          // Record failed operation
          context.operations.push({
            index: i,
            operation: operation.type || 'unknown',
            duration: Date.now() - stepStart,
            success: false,
            error: error.message
          });
          
          context.errors.push(error);
          
          // Handle operation error
          result = await this.handleOperationError(operation, error, result, context);
        }
      }
      
      return result;
      
    } catch (error) {
      // Handle composition error
      return await this.handleCompositionError(error, context);
    }
  }
  
  async executeOperation(operation, data, context) {
    const operationType = operation.type || operation.constructor.name;
    
    // Check if it's a registered operator
    if (this.manager.operators.has(operationType)) {
      const operator = this.manager.operators.get(operationType);
      return await operator.execute(operation, data, context);
    }
    
    // Check if it's a registered middleware
    if (this.manager.middleware.has(operationType)) {
      const middleware = this.manager.middleware.get(operationType);
      return await middleware.execute(operation, data, context);
    }
    
    // Execute built-in operations
    switch (operationType) {
      case 'http':
        return await this.executeHttpOperation(operation, data);
      case 'json':
        return await this.executeJsonOperation(operation, data);
      case 'validate':
        return await this.executeValidateOperation(operation, data);
      case 'transform':
        return await this.executeTransformOperation(operation, data);
      case 'cache':
        return await this.executeCacheOperation(operation, data);
      case 'filter':
        return await this.executeFilterOperation(operation, data);
      case 'sort':
        return await this.executeSortOperation(operation, data);
      case 'limit':
        return await this.executeLimitOperation(operation, data);
      case 'retry':
        return await this.executeRetryOperation(operation, data);
      case 'fallback':
        return await this.executeFallbackOperation(operation, data);
      case 'stage':
        return await this.executeStageOperation(operation, data);
      case 'middleware':
        return await this.executeMiddlewareOperation(operation, data);
      case 'handler':
        return await this.executeHandlerOperation(operation, data);
      default:
        throw new Error(`Unknown operation type: ${operationType}`);
    }
  }
  
  async executeHttpOperation(operation, data) {
    const { method, url, body, options } = operation;
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
  
  async executeJsonOperation(operation, data) {
    if (typeof data === 'string') {
      return JSON.parse(data);
    }
    return data;
  }
  
  async executeValidateOperation(operation, data) {
    const { schema, rules } = operation;
    
    if (schema) {
      const isValid = await this.validateAgainstSchema(data, schema);
      if (!isValid) {
        throw new Error(`Validation failed for schema: ${schema}`);
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
  
  async executeTransformOperation(operation, data) {
    const { rules, map, remove, add, extract } = operation;
    
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
  
  async executeCacheOperation(operation, data) {
    const { duration, key } = operation;
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
  
  async executeFilterOperation(operation, data) {
    const { criteria } = operation;
    
    if (Array.isArray(data)) {
      return data.filter(item => this.matchesCriteria(item, criteria));
    }
    
    return data;
  }
  
  async executeSortOperation(operation, data) {
    const { criteria } = operation;
    
    if (Array.isArray(data)) {
      return data.sort((a, b) => this.compareItems(a, b, criteria));
    }
    
    return data;
  }
  
  async executeLimitOperation(operation, data) {
    const { count } = operation;
    
    if (Array.isArray(data)) {
      return data.slice(0, count);
    }
    
    return data;
  }
  
  async executeRetryOperation(operation, data) {
    const { attempts, operation: targetOperation } = operation;
    let lastError;
    
    for (let i = 0; i < attempts; i++) {
      try {
        return await this.executeOperation(targetOperation, data);
      } catch (error) {
        lastError = error;
        if (i < attempts - 1) {
          await this.delay(Math.pow(2, i) * 1000); // Exponential backoff
        }
      }
    }
    
    throw lastError;
  }
  
  async executeFallbackOperation(operation, data) {
    try {
      return await this.executeOperation(operation.primary, data);
    } catch (error) {
      return await this.executeOperation(operation.fallback, data);
    }
  }
  
  async executeStageOperation(operation, data) {
    const { name, operation: stageOperation } = operation;
    
    // Execute stage operation
    const result = await this.executeOperation(stageOperation, data);
    
    // Log stage completion
    console.log(`Stage '${name}' completed`);
    
    return result;
  }
  
  async executeMiddlewareOperation(operation, data) {
    const { name, operation: middlewareOperation } = operation;
    
    // Execute middleware operation
    const result = await this.executeOperation(middlewareOperation, data);
    
    // Log middleware execution
    console.log(`Middleware '${name}' executed`);
    
    return result;
  }
  
  async executeHandlerOperation(operation, data) {
    const { name } = operation;
    
    // Execute handler (could be a function or another composition)
    if (typeof name === 'function') {
      return await name(data);
    }
    
    // Execute named handler
    return await this.manager.executeComposition(name, data);
  }
  
  applyMapTransform(data, map) {
    if (Array.isArray(data)) {
      return data.map(item => this.applyMapToItem(item, map));
    }
    
    return this.applyMapToItem(data, map);
  }
  
  applyMapToItem(item, map) {
    const result = {};
    
    Object.keys(map).forEach(key => {
      const transformation = map[key];
      
      switch (transformation) {
        case 'string':
          result[key] = String(item[key]);
          break;
        case 'capitalize':
          result[key] = String(item[key]).charAt(0).toUpperCase() + String(item[key]).slice(1).toLowerCase();
          break;
        case 'lowercase':
          result[key] = String(item[key]).toLowerCase();
          break;
        case 'hash':
          result[key] = this.hashValue(item[key]);
          break;
        default:
          result[key] = item[key];
      }
    });
    
    return result;
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
      
      if (typeof value === 'string' && value.startsWith('@')) {
        // Handle special values
        switch (value) {
          case '@date.now()':
            result[key] = Date.now();
            break;
          default:
            result[key] = value;
        }
      } else {
        result[key] = value;
      }
    });
    
    return result;
  }
  
  matchesCriteria(item, criteria) {
    return Object.keys(criteria).every(key => {
      return item[key] === criteria[key];
    });
  }
  
  compareItems(a, b, criteria) {
    for (const [key, direction] of Object.entries(criteria)) {
      const aVal = a[key];
      const bVal = b[key];
      
      if (aVal < bVal) {
        return direction === 'asc' ? -1 : 1;
      }
      if (aVal > bVal) {
        return direction === 'asc' ? 1 : -1;
      }
    }
    
    return 0;
  }
  
  hashValue(value) {
    // Simple hash implementation
    return btoa(String(value)).replace(/[^a-zA-Z0-9]/g, '');
  }
  
  generateCacheKey(data) {
    return `comp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
  
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
  
  async handleOperationError(operation, error, data, context) {
    // Default error handling - rethrow
    throw error;
  }
  
  async handleCompositionError(error, context) {
    console.error('Composition execution failed:', error);
    console.error('Context:', context);
    throw error;
  }
}
```

### TypeScript Support

```typescript
interface CompositionOperation {
  type: string;
  [key: string]: any;
}

interface CompositionContext {
  startTime: number;
  operations: Array<{
    index: number;
    operation: string;
    duration: number;
    success: boolean;
    error?: string;
  }>;
  errors: Error[];
}

interface Operator {
  execute(operation: CompositionOperation, data: any, context: CompositionContext): Promise<any>;
}

interface Middleware {
  execute(operation: CompositionOperation, data: any, context: CompositionContext): Promise<any>;
}

class TypedCompositionManager {
  private compositions: Map<string, Composition>;
  private operators: Map<string, Operator>;
  private middleware: Map<string, Middleware>;
  
  constructor() {
    this.compositions = new Map();
    this.operators = new Map();
    this.middleware = new Map();
  }
  
  async compose(operations: CompositionOperation[], initialData?: any): Promise<any> {
    // Implementation similar to JavaScript version
    return initialData;
  }
  
  async executeComposition(name: string, data?: any): Promise<any> {
    // Implementation similar to JavaScript version
    return data;
  }
  
  registerComposition(name: string, operations: CompositionOperation[]): void {
    // Implementation similar to JavaScript version
  }
  
  registerOperator(name: string, operator: Operator): void {
    this.operators.set(name, operator);
  }
  
  registerMiddleware(name: string, middleware: Middleware): void {
    this.middleware.set(name, middleware);
  }
}
```

## Real-World Examples

### Reusable Validation Compositions

```tsk
# Reusable validation compositions
validation_compositions: {
  "user_registration": @compose([
    @validate({"type": "object", "required": ["email", "password", "name"]}),
    @transform({"email": "lowercase", "name": "capitalize"}),
    @validate({"email": "email_format", "password": "min_length:8", "name": "min_length:2"})
  ]),
  
  "api_request": @compose([
    @validate({"type": "object", "required": ["method", "url"]}),
    @validate({"method": "allowed_methods", "url": "valid_url"}),
    @transform({"sanitize": true})
  ]),
  
  "file_upload": @compose([
    @validate({"type": "object", "required": ["file", "type", "size"]}),
    @validate({"type": "allowed_types", "size": "max_size:10MB"}),
    @transform({"compress": true, "encrypt": true})
  ])
}
```

### Data Processing Compositions

```tsk
# Data processing compositions
processing_compositions: {
  "user_data_processing": @compose([
    @transform({"map": {"id": "string", "name": "capitalize", "email": "lowercase"}}),
    @transform({"remove": ["password", "token", "secret"]}),
    @transform({"add": {"processed_at": "@date.now()", "source": "api"}}),
    @validate({"type": "object", "required": ["id", "name", "email"]}),
    @cache("10m")
  ]),
  
  "api_response_processing": @compose([
    @json(),
    @validate({"type": "object", "required": ["data", "status"]}),
    @validate({"status": "success"}),
    @transform({"data": "extract"}),
    @transform({"format": "standard"}),
    @cache("5m")
  ])
}
```

### Pipeline Compositions

```tsk
# Pipeline compositions
pipeline_compositions: {
  "data_pipeline": @compose([
    @stage("fetch", @http("GET", "https://api.example.com/data")),
    @stage("parse", @json()),
    @stage("validate", @validate({"type": "object"})),
    @stage("transform", @transform({"format": "standard"})),
    @stage("cache", @cache("15m"))
  ]),
  
  "user_pipeline": @compose([
    @middleware("auth", @validate({"auth": "required"})),
    @middleware("rate_limit", @rate_limit({"requests": 100, "window": "1m"})),
    @handler("process_user_request"),
    @middleware("logging", @log({"level": "info"}))
  ])
}
```

## Performance Considerations

### Composition Caching

```tsk
# Cached compositions
cached_compositions: {
  "static_data": @compose([
    @cache("24h", @http("GET", "https://api.example.com/static-data")),
    @json(),
    @transform({"format": "standard"})
  ]),
  
  "user_profile": @compose([
    @cache("1h", @http("GET", "https://api.example.com/users/$user_id")),
    @json(),
    @transform({"remove": ["password", "token"]}),
    @cache("10m")
  ])
}
```

### Efficient Compositions

```javascript
// Implement efficient composition with caching
class EfficientCompositionManager extends TuskCompositionManager {
  constructor() {
    super();
    this.resultCache = new Map();
  }
  
  async compose(operations, initialData = null) {
    const cacheKey = this.generateResultCacheKey(operations, initialData);
    
    // Check result cache
    if (this.resultCache.has(cacheKey)) {
      const cached = this.resultCache.get(cacheKey);
      if (Date.now() - cached.timestamp < 300000) { // 5 minutes
        return cached.result;
      }
    }
    
    const result = await super.compose(operations, initialData);
    
    // Cache result
    this.resultCache.set(cacheKey, {
      result: result,
      timestamp: Date.now()
    });
    
    return result;
  }
  
  generateResultCacheKey(operations, data) {
    return `result_${JSON.stringify(operations)}_${JSON.stringify(data)}`;
  }
}
```

## Security Notes

- **Input Validation**: Always validate input in compositions
- **Error Handling**: Implement proper error handling for each operation
- **Data Sanitization**: Sanitize data in transformation operations

```javascript
// Secure composition implementation
class SecureCompositionManager extends TuskCompositionManager {
  constructor() {
    super();
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }
  
  async executeOperation(operation, data, context) {
    // Sanitize sensitive data before processing
    if (operation.type === 'http' || operation.type === 'json') {
      data = this.sanitizeSensitiveData(data);
    }
    
    return await super.executeOperation(operation, data, context);
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

1. **Reusability**: Create reusable compositions for common operations
2. **Validation**: Always validate input in compositions
3. **Error Handling**: Implement proper error handling for each operation
4. **Caching**: Use caching strategically in compositions
5. **Testing**: Test compositions thoroughly
6. **Documentation**: Document all compositions and their purpose

## Next Steps

- Explore [@operator patterns](./058-at-operator-patterns-javascript.md) for common use cases
- Master [@operator optimization](./059-at-operator-optimization-javascript.md) for performance tuning
- Learn about [@operator testing](./060-at-operator-testing-javascript.md) for quality assurance 
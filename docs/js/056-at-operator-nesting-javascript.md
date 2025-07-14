# @operator Nesting - JavaScript

## Overview

The `@operator` nesting in TuskLang allows you to nest operators within each other to create complex, hierarchical operations. This is essential for JavaScript applications that need to perform sophisticated data transformations, conditional processing, and multi-level operations in a structured and maintainable way.

## Basic Syntax

```tsk
# Simple operator nesting
nested_data: @json(@http("GET", "https://api.example.com/data")).users[0].profile
cached_nested: @cache("10m", @json(@http("GET", "https://api.example.com/users")))
validated_nested: @validate(@json(@http("GET", "https://api.example.com/data")), {"type": "object"})

# Deep nesting
deep_nested: @cache("1h", @json(@http("GET", "https://api.example.com/data")).items[0].details.specifications)
```

## JavaScript Integration

### Node.js Operator Nesting

```javascript
const tusk = require('tusklang');

// Load configuration with nested operators
const config = tusk.load('nesting.tsk');

// Access nested results
console.log(config.nested_data); // User profile object
console.log(config.cached_nested); // Cached and parsed JSON
console.log(config.validated_nested); // Validated JSON data

// Use nested operations in application
const dataProcessor = {
  async getNestedUserData(userId) {
    return await tusk.nest([
      tusk.http("GET", `https://api.example.com/users/${userId}`),
      tusk.json(),
      tusk.validate({"type": "object"}),
      tusk.cache("10m")
    ]);
  },
  
  async processNestedData(data) {
    return await tusk.nest([
      tusk.validate(data, {"type": "object"}),
      tusk.transform(data, {"extract": "items"}),
      tusk.nest([
        tusk.filter({"active": true}),
        tusk.sort({"name": "asc"}),
        tusk.limit(10)
      ]),
      tusk.cache("5m")
    ]);
  }
};
```

### Browser Operator Nesting

```javascript
// Load TuskLang configuration
const config = await tusk.load('nesting.tsk');

// Use nested operations in frontend
class NestedDataProcessor {
  constructor() {
    this.apiUrl = config.api_url;
  }
  
  async processNestedData(data) {
    const result = await tusk.nest([
      tusk.validate(data, { type: "object" }),
      tusk.transform(data, { sanitize: true }),
      tusk.nest([
        tusk.filter({ status: "active" }),
        tusk.transform(null, { format: "standard" })
      ]),
      tusk.cache("5m")
    ]);
    
    return result;
  }
  
  async fetchNestedData(endpoint) {
    return await tusk.nest([
      tusk.http("GET", `${this.apiUrl}${endpoint}`),
      tusk.json(),
      tusk.nest([
        tusk.validate(null, { type: "object" }),
        tusk.transform(null, { extract: "data" })
      ]),
      tusk.cache("10m")
    ]);
  }
}
```

## Advanced Usage

### Multi-Level Nesting

```tsk
# Multi-level nested operations
multi_level_nested: @cache("1h", @json(@http("GET", "https://api.example.com/data")).items[0].details.specifications.technical.requirements)

# Conditional nesting
conditional_nested: @env("NODE_ENV") == "production" ? 
  @cache("24h", @json(@http("GET", "https://prod-api.example.com/data")).production_data) :
  @cache("5m", @json(@http("GET", "https://dev-api.example.com/data")).development_data)

# Nested with error handling
robust_nested: @fallback(
  @cache("10m", @json(@http("GET", "https://primary-api.example.com/data")).primary_data),
  @cache("5m", @json(@http("GET", "https://backup-api.example.com/data")).backup_data)
)
```

### Complex Nested Transformations

```tsk
# Complex nested data transformation
complex_nested: @chain([
  @http("GET", "https://api.example.com/raw-data"),
  @json(),
  @nest([
    @validate({"type": "object", "required": ["users"]}),
    @transform({"users": "extract"}),
    @nest([
      @filter({"status": "active"}),
      @transform({"map": {"id": "string", "name": "capitalize", "email": "lowercase"}}),
      @sort({"name": "asc"}),
      @limit(100)
    ]),
    @cache("15m")
  ]),
  @metrics("nested_processing", 1)
])

# Nested validation and transformation
nested_validation: @nest([
  @validate(@request.body, {"type": "object", "required": ["user"]}),
  @transform({"user": "extract"}),
  @nest([
    @validate(null, {"type": "object", "required": ["email", "password"]}),
    @transform({"email": "lowercase", "password": "hash"}),
    @validate(null, {"email": "email_format", "password": "min_length:8"})
  ]),
  @cache("5m", "user_validation_$email")
])
```

### Nested Caching Strategies

```tsk
# Hierarchical caching with nested operations
hierarchical_caching: @nest([
  @cache("24h", @http("GET", "https://api.example.com/static-data")),
  @json(),
  @nest([
    @cache("1h", @transform({"format": "standard"})),
    @nest([
      @cache("10m", @filter({"category": "featured"})),
      @sort({"priority": "desc"}),
      @limit(20)
    ])
  ])
])

# Nested cache with different strategies
nested_cache_strategies: @nest([
  @cache("1h", @http("GET", "https://api.example.com/users")),
  @json(),
  @nest([
    @cache("10m", @filter({"role": "admin"})),
    @nest([
      @cache("5m", @transform({"select": ["id", "name", "email"]})),
      @sort({"name": "asc"})
    ])
  ])
])
```

## JavaScript Implementation

### Custom Nesting Manager

```javascript
class TuskNestingManager {
  constructor() {
    this.operators = new Map();
    this.nestingLevel = 0;
    this.maxNestingLevel = 10;
  }
  
  async executeNested(operations, initialData = null) {
    if (this.nestingLevel >= this.maxNestingLevel) {
      throw new Error(`Maximum nesting level (${this.maxNestingLevel}) exceeded`);
    }
    
    this.nestingLevel++;
    let result = initialData;
    
    try {
      for (const operation of operations) {
        if (Array.isArray(operation)) {
          // Nested operation
          result = await this.executeNested(operation, result);
        } else {
          // Single operation
          result = await this.executeOperation(operation, result);
        }
      }
      
      return result;
    } finally {
      this.nestingLevel--;
    }
  }
  
  async executeOperation(operation, data) {
    const operationType = operation.type || operation.constructor.name;
    
    switch (operationType) {
      case 'http':
        return await this.executeHttpOperation(operation, data);
      case 'json':
        return await this.executeJsonOperation(operation, data);
      case 'cache':
        return await this.executeCacheOperation(operation, data);
      case 'validate':
        return await this.executeValidateOperation(operation, data);
      case 'transform':
        return await this.executeTransformOperation(operation, data);
      case 'filter':
        return await this.executeFilterOperation(operation, data);
      case 'sort':
        return await this.executeSortOperation(operation, data);
      case 'limit':
        return await this.executeLimitOperation(operation, data);
      case 'nest':
        return await this.executeNestOperation(operation, data);
      case 'fallback':
        return await this.executeFallbackOperation(operation, data);
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
    const { rules, extract, map, select } = operation;
    
    if (extract) {
      return data[extract];
    }
    
    if (map) {
      return this.applyMapTransform(data, map);
    }
    
    if (select) {
      return this.applySelectTransform(data, select);
    }
    
    if (rules) {
      return await this.applyTransformRules(data, rules);
    }
    
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
  
  async executeNestOperation(operation, data) {
    const { operations } = operation;
    return await this.executeNested(operations, data);
  }
  
  async executeFallbackOperation(operation, data) {
    try {
      return await this.executeOperation(operation.primary, data);
    } catch (error) {
      return await this.executeOperation(operation.fallback, data);
    }
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
  
  applySelectTransform(data, select) {
    if (Array.isArray(data)) {
      return data.map(item => this.selectFields(item, select));
    }
    
    return this.selectFields(data, select);
  }
  
  selectFields(item, select) {
    const result = {};
    
    select.forEach(field => {
      if (item.hasOwnProperty(field)) {
        result[field] = item[field];
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
    return `nest_${this.nestingLevel}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}
```

### TypeScript Support

```typescript
interface NestingOperation {
  type: string;
  [key: string]: any;
}

interface NestingContext {
  level: number;
  maxLevel: number;
  operations: NestingOperation[];
}

class TypedNestingManager {
  private operators: Map<string, any>;
  private nestingLevel: number;
  private maxNestingLevel: number;
  
  constructor() {
    this.operators = new Map();
    this.nestingLevel = 0;
    this.maxNestingLevel = 10;
  }
  
  async executeNested(operations: (NestingOperation | NestingOperation[])[], initialData?: any): Promise<any> {
    // Implementation similar to JavaScript version
    return initialData;
  }
  
  async executeOperation(operation: NestingOperation, data: any): Promise<any> {
    // Implementation similar to JavaScript version
    return data;
  }
  
  setMaxNestingLevel(level: number): void {
    this.maxNestingLevel = level;
  }
}
```

## Real-World Examples

### Complex Data Processing Pipeline

```tsk
# Complex nested data processing
complex_pipeline: @nest([
  @http("GET", "https://api.example.com/raw-data"),
  @json(),
  @nest([
    @validate({"type": "object", "required": ["items"]}),
    @transform({"items": "extract"}),
    @nest([
      @filter({"status": "active", "type": "user"}),
      @nest([
        @transform({"map": {"id": "string", "name": "capitalize", "email": "lowercase"}}),
        @sort({"name": "asc"}),
        @limit(50)
      ]),
      @cache("10m")
    ]),
    @nest([
      @filter({"status": "active", "type": "admin"}),
      @nest([
        @transform({"map": {"id": "string", "name": "capitalize", "role": "uppercase"}}),
        @sort({"role": "asc", "name": "asc"}),
        @limit(10)
      ]),
      @cache("5m")
    ])
  ]),
  @nest([
    @merge({"users": 0, "admins": 1}),
    @transform({"format": "standard"}),
    @cache("15m")
  ])
])
```

### User Authentication Pipeline

```tsk
# Nested user authentication
auth_pipeline: @nest([
  @validate(@request.body, {"type": "object", "required": ["credentials"]}),
  @transform({"credentials": "extract"}),
  @nest([
    @validate(null, {"type": "object", "required": ["email", "password"]}),
    @nest([
      @transform({"email": "lowercase", "password": "hash"}),
      @validate(null, {"email": "email_format", "password": "min_length:8"})
    ]),
    @query("SELECT * FROM users WHERE email = ? AND password = ?", [$email, $password]),
    @nest([
      @validate({"type": "array", "length": 1}),
      @transform({"extract": "user", "remove": ["password"]}),
      @nest([
        @validate(null, {"type": "object", "required": ["id", "email", "role"]}),
        @cache("30m", "user_session_$id")
      ])
    ])
  ])
])
```

### API Response Processing

```tsk
# Nested API response processing
api_processing: @nest([
  @http("GET", "https://api.example.com/data"),
  @json(),
  @nest([
    @validate({"type": "object", "required": ["response"]}),
    @transform({"response": "extract"}),
    @nest([
      @validate({"type": "object", "required": ["items", "metadata"]}),
      @nest([
        @transform({"items": "extract"}),
        @nest([
          @filter({"active": true}),
          @nest([
            @transform({"map": {"id": "string", "name": "capitalize"}}),
            @sort({"name": "asc"}),
            @limit(100)
          ]),
          @cache("10m")
        ]),
        @nest([
          @transform({"metadata": "extract"}),
          @nest([
            @validate({"type": "object"}),
            @transform({"format": "standard"}),
            @cache("5m")
          ])
        ])
      ]),
      @merge({"items": 0, "metadata": 1}),
      @cache("15m")
    ])
  ])
])
```

## Performance Considerations

### Nesting Optimization

```tsk
# Optimized nested operations
optimized_nested: @nest([
  @cache("1h", @http("GET", "https://api.example.com/static-data")),
  @json(),
  @nest([
    @cache("10m", @transform({"format": "standard"})),
    @nest([
      @cache("5m", @filter({"category": "featured"})),
      @sort({"priority": "desc"}),
      @limit(20)
    ])
  ])
])
```

### Efficient Nesting

```javascript
// Implement efficient nesting with caching
class EfficientNestingManager extends TuskNestingManager {
  constructor() {
    super();
    this.operationCache = new Map();
  }
  
  async executeNested(operations, initialData = null) {
    const cacheKey = this.generateOperationCacheKey(operations, initialData);
    
    // Check operation cache
    if (this.operationCache.has(cacheKey)) {
      const cached = this.operationCache.get(cacheKey);
      if (Date.now() - cached.timestamp < 60000) { // 1 minute cache
        return cached.result;
      }
    }
    
    const result = await super.executeNested(operations, initialData);
    
    // Cache result
    this.operationCache.set(cacheKey, {
      result: result,
      timestamp: Date.now()
    });
    
    return result;
  }
  
  generateOperationCacheKey(operations, data) {
    return `op_${JSON.stringify(operations)}_${JSON.stringify(data)}`;
  }
}
```

## Security Notes

- **Input Validation**: Validate input at each nesting level
- **Depth Limiting**: Limit nesting depth to prevent stack overflow
- **Data Sanitization**: Sanitize data at each transformation level

```javascript
// Secure nesting implementation
class SecureNestingManager extends TuskNestingManager {
  constructor() {
    super();
    this.maxNestingLevel = 5; // Reduced for security
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }
  
  async executeOperation(operation, data) {
    // Sanitize sensitive data before processing
    if (operation.type === 'http' || operation.type === 'json') {
      data = this.sanitizeSensitiveData(data);
    }
    
    return await super.executeOperation(operation, data);
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

1. **Depth Management**: Limit nesting depth to prevent complexity
2. **Validation**: Validate data at each nesting level
3. **Caching**: Use caching strategically in nested operations
4. **Error Handling**: Implement proper error handling for each level
5. **Testing**: Test nested operations thoroughly
6. **Documentation**: Document complex nested structures

## Next Steps

- Learn about [@operator composition](./057-at-operator-composition-javascript.md) for reusable components
- Explore [@operator patterns](./058-at-operator-patterns-javascript.md) for common use cases
- Master [@operator optimization](./059-at-operator-optimization-javascript.md) for performance tuning 
# @operator Testing - JavaScript

## Overview

The `@operator` testing in TuskLang provides comprehensive testing capabilities for operators, patterns, and configurations. This is essential for JavaScript applications that need to ensure reliability, validate functionality, and maintain quality across their TuskLang implementations.

## Basic Syntax

```tsk
# Simple operator testing
test_result: @test("email_validation", {
  "input": "test@example.com",
  "expected": {"valid": true, "email": "test@example.com"}
})

# Pattern testing
pattern_test: @test("user_registration_pattern", {
  "input": {"email": "user@example.com", "password": "password123", "name": "John Doe"},
  "expected": {"success": true, "user_id": "12345"}
})

# Configuration testing
config_test: @test("app_config", {
  "input": {"debug": true, "port": 3000},
  "expected": {"valid": true, "config": {"debug": true, "port": 3000}}
})
```

## JavaScript Integration

### Node.js Operator Testing

```javascript
const tusk = require('tusklang');

// Load configuration with testing
const config = tusk.load('testing.tsk');

// Access test results
console.log(config.test_result); // Test result object
console.log(config.pattern_test); // Pattern test result
console.log(config.config_test); // Configuration test result

// Use testing in application
const testingService = {
  async testEmailValidation(email) {
    return await tusk.test("email_validation", {
      input: email,
      expected: { valid: true, email: email.toLowerCase() }
    });
  },
  
  async testUserRegistration(userData) {
    return await tusk.test("user_registration_pattern", {
      input: userData,
      expected: { success: true, user_id: expect.any(String) }
    });
  },
  
  async testConfiguration(config) {
    return await tusk.test("app_config", {
      input: config,
      expected: { valid: true, config: config }
    });
  }
};
```

### Browser Operator Testing

```javascript
// Load TuskLang configuration
const config = await tusk.load('testing.tsk');

// Use testing in frontend
class TestingManager {
  constructor() {
    this.testResults = config.test_result;
    this.patternTests = config.pattern_test;
  }
  
  async runTest(testName, input, expected) {
    const result = await tusk.test(testName, {
      input: input,
      expected: expected
    });
    
    return result;
  }
  
  async runTestSuite(suiteName, tests) {
    const results = [];
    
    for (const test of tests) {
      const result = await this.runTest(suiteName, test.input, test.expected);
      results.push(result);
    }
    
    return results;
  }
}
```

## Advanced Usage

### Comprehensive Testing

```tsk
# Comprehensive operator testing
comprehensive_test: @test("comprehensive", {
  "test_cases": [
    {
      "name": "valid_email",
      "input": "test@example.com",
      "expected": {"valid": true, "email": "test@example.com"}
    },
    {
      "name": "invalid_email",
      "input": "invalid-email",
      "expected": {"valid": false, "error": "Invalid email format"}
    },
    {
      "name": "empty_email",
      "input": "",
      "expected": {"valid": false, "error": "Email is required"}
    }
  ],
  "options": {
    "timeout": 5000,
    "retries": 3,
    "parallel": true
  }
})

# Performance testing
performance_test: @test("performance", {
  "operation": @http("GET", "https://api.example.com/data"),
  "metrics": [
    {"name": "response_time", "max": 1000},
    {"name": "memory_usage", "max": 100},
    {"name": "throughput", "min": 100}
  ],
  "iterations": 100
})

# Integration testing
integration_test: @test("integration", {
  "pipeline": [
    @http("GET", "https://api.example.com/users"),
    @json(),
    @validate({"type": "array"}),
    @transform({"map": {"id": "string", "name": "capitalize"}}),
    @cache("5m")
  ],
  "expected": {"type": "array", "length": ">0"}
})
```

### Test Suites

```tsk
# Test suites
test_suites: {
  "validation_suite": @test.suite("validation", [
    @test("email_validation", {"input": "test@example.com", "expected": {"valid": true}}),
    @test("password_validation", {"input": "password123", "expected": {"valid": true}}),
    @test("name_validation", {"input": "John Doe", "expected": {"valid": true}})
  ]),
  
  "transformation_suite": @test.suite("transformation", [
    @test("user_transform", {"input": {"id": 1, "name": "john", "email": "JOHN@EXAMPLE.COM"}, "expected": {"id": "1", "name": "John", "email": "john@example.com"}}),
    @test("data_transform", {"input": [{"id": 1}, {"id": 2}], "expected": {"type": "array", "length": 2}})
  ]),
  
  "api_suite": @test.suite("api", [
    @test("api_fetch", {"input": "https://api.example.com/users", "expected": {"type": "array"}}),
    @test("api_cache", {"input": "https://api.example.com/data", "expected": {"cached": true}})
  ])
}
```

### Mock Testing

```tsk
# Mock testing
mock_test: @test("mock", {
  "mocks": {
    "http": {
      "GET https://api.example.com/users": {"status": 200, "body": [{"id": 1, "name": "John"}]},
      "POST https://api.example.com/users": {"status": 201, "body": {"id": 2, "name": "Jane"}}
    },
    "database": {
      "SELECT * FROM users": [{"id": 1, "name": "John"}],
      "INSERT INTO users": {"affected_rows": 1}
    }
  },
  "test_cases": [
    {
      "name": "fetch_users",
      "operation": @http("GET", "https://api.example.com/users"),
      "expected": {"type": "array", "length": 1}
    },
    {
      "name": "create_user",
      "operation": @http("POST", "https://api.example.com/users", {"name": "Jane"}),
      "expected": {"status": 201, "body": {"id": 2, "name": "Jane"}}
    }
  ]
})
```

## JavaScript Implementation

### Custom Testing Manager

```javascript
class TuskTestingManager {
  constructor() {
    this.tests = new Map();
    this.mocks = new Map();
    this.results = new Map();
  }
  
  async test(name, options) {
    const testId = this.generateTestId(name, options);
    
    // Check if test already exists
    if (this.tests.has(testId)) {
      const test = this.tests.get(testId);
      return await test.execute();
    }
    
    // Create new test
    const test = new Test(name, options, this);
    this.tests.set(testId, test);
    
    return await test.execute();
  }
  
  async testSuite(name, tests) {
    const results = [];
    
    for (const testConfig of tests) {
      const result = await this.test(name, testConfig);
      results.push(result);
    }
    
    return {
      suite: name,
      total: results.length,
      passed: results.filter(r => r.passed).length,
      failed: results.filter(r => !r.passed).length,
      results: results
    };
  }
  
  async comprehensiveTest(options) {
    const { test_cases, options: testOptions } = options;
    const results = [];
    
    for (const testCase of test_cases) {
      const result = await this.executeTestCase(testCase, testOptions);
      results.push(result);
    }
    
    return {
      total: results.length,
      passed: results.filter(r => r.passed).length,
      failed: results.filter(r => !r.passed).length,
      results: results
    };
  }
  
  async performanceTest(options) {
    const { operation, metrics, iterations } = options;
    const results = [];
    
    for (let i = 0; i < iterations; i++) {
      const startTime = Date.now();
      const startMemory = process.memoryUsage().heapUsed;
      
      try {
        const result = await this.executeOperation(operation);
        const endTime = Date.now();
        const endMemory = process.memoryUsage().heapUsed;
        
        results.push({
          iteration: i,
          response_time: endTime - startTime,
          memory_usage: endMemory - startMemory,
          success: true,
          result: result
        });
      } catch (error) {
        results.push({
          iteration: i,
          error: error.message,
          success: false
        });
      }
    }
    
    // Calculate metrics
    const avgResponseTime = results.filter(r => r.success).reduce((sum, r) => sum + r.response_time, 0) / results.filter(r => r.success).length;
    const avgMemoryUsage = results.filter(r => r.success).reduce((sum, r) => sum + r.memory_usage, 0) / results.filter(r => r.success).length;
    const throughput = results.filter(r => r.success).length / (avgResponseTime / 1000);
    
    // Check if metrics meet requirements
    const passed = metrics.every(metric => {
      const value = this.getMetricValue(metric.name, { avgResponseTime, avgMemoryUsage, throughput });
      
      if (metric.max !== undefined) {
        return value <= metric.max;
      }
      if (metric.min !== undefined) {
        return value >= metric.min;
      }
      return true;
    });
    
    return {
      passed: passed,
      metrics: {
        avg_response_time: avgResponseTime,
        avg_memory_usage: avgMemoryUsage,
        throughput: throughput
      },
      results: results
    };
  }
  
  async integrationTest(options) {
    const { pipeline, expected } = options;
    let result = null;
    
    try {
      // Execute pipeline
      for (const operation of pipeline) {
        result = await this.executeOperation(operation, result);
      }
      
      // Validate result against expected
      const passed = this.validateResult(result, expected);
      
      return {
        passed: passed,
        result: result,
        expected: expected
      };
    } catch (error) {
      return {
        passed: false,
        error: error.message,
        expected: expected
      };
    }
  }
  
  async mockTest(options) {
    const { mocks, test_cases } = options;
    
    // Setup mocks
    this.setupMocks(mocks);
    
    const results = [];
    
    try {
      // Execute test cases
      for (const testCase of test_cases) {
        const result = await this.executeTestCase(testCase);
        results.push(result);
      }
    } finally {
      // Cleanup mocks
      this.cleanupMocks();
    }
    
    return {
      total: results.length,
      passed: results.filter(r => r.passed).length,
      failed: results.filter(r => !r.passed).length,
      results: results
    };
  }
  
  async executeTestCase(testCase, options = {}) {
    const { name, input, expected, operation } = testCase;
    const timeout = options.timeout || 5000;
    const retries = options.retries || 1;
    
    for (let attempt = 0; attempt < retries; attempt++) {
      try {
        let result;
        
        if (operation) {
          result = await this.executeOperation(operation, input);
        } else {
          result = await this.executeTest(name, input);
        }
        
        const passed = this.validateResult(result, expected);
        
        return {
          name: name,
          passed: passed,
          result: result,
          expected: expected,
          attempt: attempt + 1
        };
      } catch (error) {
        if (attempt === retries - 1) {
          return {
            name: name,
            passed: false,
            error: error.message,
            expected: expected,
            attempt: attempt + 1
          };
        }
        
        // Wait before retry
        await this.delay(1000);
      }
    }
  }
  
  async executeTest(name, input) {
    // Execute test based on name
    switch (name) {
      case 'email_validation':
        return await this.validateEmail(input);
      case 'password_validation':
        return await this.validatePassword(input);
      case 'name_validation':
        return await this.validateName(input);
      case 'user_transform':
        return await this.transformUser(input);
      case 'data_transform':
        return await this.transformData(input);
      case 'api_fetch':
        return await this.fetchApi(input);
      case 'api_cache':
        return await this.cacheApi(input);
      default:
        throw new Error(`Unknown test: ${name}`);
    }
  }
  
  async executeOperation(operation, input) {
    const operationType = operation.type || operation.constructor.name;
    
    switch (operationType) {
      case 'http':
        return await this.executeHttpOperation(operation, input);
      case 'json':
        return await this.executeJsonOperation(operation, input);
      case 'validate':
        return await this.executeValidateOperation(operation, input);
      case 'transform':
        return await this.executeTransformOperation(operation, input);
      case 'cache':
        return await this.executeCacheOperation(operation, input);
      default:
        throw new Error(`Unknown operation type: ${operationType}`);
    }
  }
  
  async executeHttpOperation(operation, input) {
    const { method, url, body } = operation;
    
    // Check for mock
    const mockKey = `${method} ${url}`;
    if (this.mocks.has(mockKey)) {
      return this.mocks.get(mockKey);
    }
    
    const response = await fetch(url, {
      method: method || 'GET',
      body: body
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    return await response.json();
  }
  
  async executeJsonOperation(operation, input) {
    if (typeof input === 'string') {
      return JSON.parse(input);
    }
    return input;
  }
  
  async executeValidateOperation(operation, input) {
    const { schema, rules } = operation;
    
    if (schema) {
      const isValid = await this.validateAgainstSchema(input, schema);
      if (!isValid) {
        throw new Error(`Validation failed for schema: ${JSON.stringify(schema)}`);
      }
    }
    
    if (rules) {
      const isValid = await this.validateAgainstRules(input, rules);
      if (!isValid) {
        throw new Error(`Validation failed for rules: ${JSON.stringify(rules)}`);
      }
    }
    
    return { valid: true, data: input };
  }
  
  async executeTransformOperation(operation, input) {
    const { rules, map, remove, add } = operation;
    
    if (map) {
      return this.applyMapTransform(input, map);
    }
    
    if (remove) {
      return this.applyRemoveTransform(input, remove);
    }
    
    if (add) {
      return this.applyAddTransform(input, add);
    }
    
    if (rules) {
      return await this.applyTransformRules(input, rules);
    }
    
    return input;
  }
  
  async executeCacheOperation(operation, input) {
    const { key, duration } = operation;
    const cacheKey = key || this.generateCacheKey(input);
    
    // Check cache first
    const cached = await this.getFromCache(cacheKey);
    if (cached) {
      return { cached: true, data: cached };
    }
    
    // Store in cache
    await this.setInCache(cacheKey, input, duration);
    return { cached: false, data: input };
  }
  
  async validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const valid = emailRegex.test(email);
    
    if (!valid) {
      return { valid: false, error: "Invalid email format" };
    }
    
    if (!email) {
      return { valid: false, error: "Email is required" };
    }
    
    return { valid: true, email: email.toLowerCase() };
  }
  
  async validatePassword(password) {
    if (!password) {
      return { valid: false, error: "Password is required" };
    }
    
    if (password.length < 8) {
      return { valid: false, error: "Password must be at least 8 characters" };
    }
    
    return { valid: true, password: password };
  }
  
  async validateName(name) {
    if (!name) {
      return { valid: false, error: "Name is required" };
    }
    
    if (name.length < 2) {
      return { valid: false, error: "Name must be at least 2 characters" };
    }
    
    return { valid: true, name: name };
  }
  
  async transformUser(user) {
    const result = {};
    
    if (user.id) {
      result.id = String(user.id);
    }
    
    if (user.name) {
      result.name = String(user.name).charAt(0).toUpperCase() + String(user.name).slice(1).toLowerCase();
    }
    
    if (user.email) {
      result.email = String(user.email).toLowerCase();
    }
    
    return result;
  }
  
  async transformData(data) {
    if (Array.isArray(data)) {
      return { type: "array", length: data.length };
    }
    
    return { type: typeof data };
  }
  
  async fetchApi(url) {
    const response = await fetch(url);
    const data = await response.json();
    
    if (Array.isArray(data)) {
      return data;
    }
    
    throw new Error("Expected array response");
  }
  
  async cacheApi(url) {
    const cacheKey = `api_cache_${url}`;
    const cached = await this.getFromCache(cacheKey);
    
    if (cached) {
      return { cached: true, data: cached };
    }
    
    const response = await fetch(url);
    const data = await response.json();
    
    await this.setInCache(cacheKey, data, "5m");
    return { cached: false, data: data };
  }
  
  validateResult(result, expected) {
    if (typeof expected === 'object') {
      return Object.keys(expected).every(key => {
        const expectedValue = expected[key];
        const actualValue = result[key];
        
        if (typeof expectedValue === 'function') {
          return expectedValue(actualValue);
        }
        
        if (typeof expectedValue === 'string' && expectedValue.startsWith('>')) {
          const threshold = parseFloat(expectedValue.substring(1));
          return actualValue > threshold;
        }
        
        if (typeof expectedValue === 'string' && expectedValue.startsWith('<')) {
          const threshold = parseFloat(expectedValue.substring(1));
          return actualValue < threshold;
        }
        
        return actualValue === expectedValue;
      });
    }
    
    return result === expected;
  }
  
  validateAgainstSchema(data, schema) {
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
    }
    
    if (schema.type === 'array') {
      if (!Array.isArray(data)) {
        return false;
      }
      
      if (schema.length && data.length !== schema.length) {
        return false;
      }
      
      if (schema.length && typeof schema.length === 'string' && schema.length.startsWith('>')) {
        const threshold = parseInt(schema.length.substring(1));
        if (data.length <= threshold) {
          return false;
        }
      }
    }
    
    return true;
  }
  
  validateAgainstRules(data, rules) {
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
      result[key] = fields[key];
    });
    
    return result;
  }
  
  setupMocks(mocks) {
    Object.keys(mocks).forEach(category => {
      Object.keys(mocks[category]).forEach(key => {
        this.mocks.set(key, mocks[category][key]);
      });
    });
  }
  
  cleanupMocks() {
    this.mocks.clear();
  }
  
  getMetricValue(metricName, metrics) {
    switch (metricName) {
      case 'response_time':
        return metrics.avgResponseTime;
      case 'memory_usage':
        return metrics.avgMemoryUsage;
      case 'throughput':
        return metrics.throughput;
      default:
        return 0;
    }
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
  
  generateCacheKey(data) {
    return `test_cache_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
  
  generateTestId(name, options) {
    return `test_${name}_${JSON.stringify(options)}`;
  }
  
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

class Test {
  constructor(name, options, manager) {
    this.name = name;
    this.options = options;
    this.manager = manager;
  }
  
  async execute() {
    const { input, expected, test_cases, operation, metrics, iterations, pipeline, mocks } = this.options;
    
    if (test_cases) {
      return await this.manager.comprehensiveTest(this.options);
    }
    
    if (operation && metrics) {
      return await this.manager.performanceTest(this.options);
    }
    
    if (pipeline && expected) {
      return await this.manager.integrationTest(this.options);
    }
    
    if (mocks && test_cases) {
      return await this.manager.mockTest(this.options);
    }
    
    return await this.manager.executeTestCase({
      name: this.name,
      input: input,
      expected: expected,
      operation: operation
    });
  }
}
```

### TypeScript Support

```typescript
interface TestOptions {
  input?: any;
  expected?: any;
  test_cases?: Array<{
    name: string;
    input: any;
    expected: any;
    operation?: any;
  }>;
  operation?: any;
  metrics?: Array<{
    name: string;
    max?: number;
    min?: number;
  }>;
  iterations?: number;
  pipeline?: any[];
  mocks?: Record<string, Record<string, any>>;
  options?: {
    timeout?: number;
    retries?: number;
    parallel?: boolean;
  };
}

interface TestResult {
  name?: string;
  passed: boolean;
  result?: any;
  expected?: any;
  error?: string;
  attempt?: number;
}

interface TestSuiteResult {
  suite: string;
  total: number;
  passed: number;
  failed: number;
  results: TestResult[];
}

class TypedTestingManager {
  private tests: Map<string, Test>;
  private mocks: Map<string, any>;
  private results: Map<string, TestResult>;
  
  constructor() {
    this.tests = new Map();
    this.mocks = new Map();
    this.results = new Map();
  }
  
  async test(name: string, options: TestOptions): Promise<TestResult> {
    // Implementation similar to JavaScript version
    return { passed: true };
  }
  
  async testSuite(name: string, tests: TestOptions[]): Promise<TestSuiteResult> {
    // Implementation similar to JavaScript version
    return { suite: name, total: 0, passed: 0, failed: 0, results: [] };
  }
}
```

## Real-World Examples

### Validation Testing

```tsk
# Validation testing
validation_tests: {
  "email_validation": @test.suite("email_validation", [
    @test("email_validation", {"input": "test@example.com", "expected": {"valid": true, "email": "test@example.com"}}),
    @test("email_validation", {"input": "invalid-email", "expected": {"valid": false, "error": "Invalid email format"}}),
    @test("email_validation", {"input": "", "expected": {"valid": false, "error": "Email is required"}})
  ]),
  
  "password_validation": @test.suite("password_validation", [
    @test("password_validation", {"input": "password123", "expected": {"valid": true}}),
    @test("password_validation", {"input": "123", "expected": {"valid": false, "error": "Password must be at least 8 characters"}}),
    @test("password_validation", {"input": "", "expected": {"valid": false, "error": "Password is required"}})
  ])
}
```

### Transformation Testing

```tsk
# Transformation testing
transformation_tests: {
  "user_transform": @test.suite("user_transform", [
    @test("user_transform", {"input": {"id": 1, "name": "john", "email": "JOHN@EXAMPLE.COM"}, "expected": {"id": "1", "name": "John", "email": "john@example.com"}}),
    @test("user_transform", {"input": {"id": 2, "name": "JANE", "email": "jane@example.com"}, "expected": {"id": "2", "name": "Jane", "email": "jane@example.com"}})
  ]),
  
  "data_transform": @test.suite("data_transform", [
    @test("data_transform", {"input": [{"id": 1}, {"id": 2}], "expected": {"type": "array", "length": 2}}),
    @test("data_transform", {"input": {"name": "test"}, "expected": {"type": "object"}})
  ])
}
```

### API Testing

```tsk
# API testing
api_tests: {
  "api_integration": @test("integration", {
    "pipeline": [
      @http("GET", "https://api.example.com/users"),
      @json(),
      @validate({"type": "array"}),
      @transform({"map": {"id": "string", "name": "capitalize"}}),
      @cache("5m")
    ],
    "expected": {"type": "array", "length": ">0"}
  }),
  
  "api_performance": @test("performance", {
    "operation": @http("GET", "https://api.example.com/data"),
    "metrics": [
      {"name": "response_time", "max": 1000},
      {"name": "memory_usage", "max": 100},
      {"name": "throughput", "min": 100}
    ],
    "iterations": 100
  })
}
```

## Performance Considerations

### Test Caching

```tsk
# Cached test results
cached_tests: @test("cached", {
  "test_cases": [
    {"name": "static_validation", "input": "test@example.com", "expected": {"valid": true}}
  ],
  "options": {
    "cache_results": true,
    "cache_duration": "1h"
  }
})
```

### Efficient Testing

```javascript
// Implement efficient testing with caching
class EfficientTestingManager extends TuskTestingManager {
  constructor() {
    super();
    this.resultCache = new Map();
  }
  
  async test(name, options) {
    const cacheKey = this.generateResultCacheKey(name, options);
    
    // Check result cache
    if (this.resultCache.has(cacheKey)) {
      const cached = this.resultCache.get(cacheKey);
      if (Date.now() - cached.timestamp < 3600000) { // 1 hour
        return cached.result;
      }
    }
    
    const result = await super.test(name, options);
    
    // Cache result
    this.resultCache.set(cacheKey, {
      result: result,
      timestamp: Date.now()
    });
    
    return result;
  }
  
  generateResultCacheKey(name, options) {
    return `test_result_${name}_${JSON.stringify(options)}`;
  }
}
```

## Security Notes

- **Input Validation**: Always validate test inputs
- **Mock Security**: Ensure mocks don't expose sensitive data
- **Result Validation**: Validate test results before caching

```javascript
// Secure testing implementation
class SecureTestingManager extends TuskTestingManager {
  constructor() {
    super();
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }
  
  async test(name, options) {
    // Sanitize sensitive data in test options
    const sanitizedOptions = this.sanitizeTestOptions(options);
    
    const result = await super.test(name, sanitizedOptions);
    
    // Sanitize sensitive data in results
    result.result = this.sanitizeSensitiveData(result.result);
    
    return result;
  }
  
  sanitizeTestOptions(options) {
    const sanitized = { ...options };
    
    if (sanitized.input) {
      sanitized.input = this.sanitizeSensitiveData(sanitized.input);
    }
    
    if (sanitized.expected) {
      sanitized.expected = this.sanitizeSensitiveData(sanitized.expected);
    }
    
    return sanitized;
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

1. **Test Coverage**: Ensure comprehensive test coverage
2. **Mock Usage**: Use mocks for external dependencies
3. **Performance Testing**: Include performance tests for critical operations
4. **Result Validation**: Always validate test results
5. **Caching**: Cache test results when appropriate
6. **Documentation**: Document all tests and their purpose

## Next Steps

- Explore [@operator deployment](./061-at-operator-deployment-javascript.md) for production readiness
- Master [@operator monitoring](./062-at-operator-monitoring-javascript.md) for operational insights
- Learn about [@operator troubleshooting](./063-at-operator-troubleshooting-javascript.md) for debugging 
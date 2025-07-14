# TuskLang JavaScript Documentation: Advanced Error Handling

## Overview

Advanced error handling in TuskLang provides sophisticated error management, recovery strategies, and error reporting with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#error_handling advanced
  strategies:
    retry: true
    circuit_breaker: true
    fallback: true
    graceful_degradation: true
    
  retry:
    max_attempts: 3
    backoff_strategy: exponential
    base_delay: 1000ms
    max_delay: 10000ms
    
  circuit_breaker:
    failure_threshold: 5
    recovery_timeout: 60000ms
    half_open_state: true
    monitoring_window: 300000ms
    
  fallback:
    enabled: true
    strategies:
      - cache
      - default_value
      - alternative_service
      - degraded_mode
    
  logging:
    enabled: true
    level: error
    include_stack_trace: true
    include_context: true
    error_categories:
      - network
      - database
      - validation
      - business_logic
```

## JavaScript Integration

### Advanced Error Handler

```javascript
// advanced-error-handler.js
class AdvancedErrorHandler {
  constructor(config) {
    this.config = config;
    this.strategies = config.strategies || {};
    this.retry = config.retry || {};
    this.circuitBreaker = config.circuit_breaker || {};
    this.fallback = config.fallback || {};
    this.logging = config.logging || {};
    
    this.errorCounts = new Map();
    this.circuitBreakers = new Map();
    this.fallbackHandlers = new Map();
  }

  async handleError(error, context = {}) {
    const errorInfo = this.analyzeError(error);
    
    // Log error
    await this.logError(error, context, errorInfo);
    
    // Apply error handling strategies
    const result = await this.applyStrategies(error, context, errorInfo);
    
    return result;
  }

  analyzeError(error) {
    return {
      type: error.constructor.name,
      message: error.message,
      stack: error.stack,
      category: this.categorizeError(error),
      retryable: this.isRetryable(error),
      severity: this.calculateSeverity(error)
    };
  }

  categorizeError(error) {
    if (error.code === 'ECONNREFUSED' || error.code === 'ENOTFOUND') {
      return 'network';
    }
    
    if (error.code === 'ER_DUP_ENTRY' || error.code === 'ER_NO_SUCH_TABLE') {
      return 'database';
    }
    
    if (error.name === 'ValidationError' || error.name === 'TypeError') {
      return 'validation';
    }
    
    return 'business_logic';
  }

  isRetryable(error) {
    const retryableErrors = [
      'ECONNREFUSED',
      'ENOTFOUND',
      'ETIMEDOUT',
      'ECONNRESET',
      'ENETUNREACH'
    ];
    
    return retryableErrors.includes(error.code);
  }

  calculateSeverity(error) {
    if (error.fatal || error.critical) return 'critical';
    if (error.code === 'ECONNREFUSED') return 'high';
    if (error.name === 'ValidationError') return 'medium';
    return 'low';
  }

  async applyStrategies(error, context, errorInfo) {
    let result = null;
    
    // Try retry strategy
    if (this.strategies.retry && errorInfo.retryable) {
      result = await this.applyRetryStrategy(context.operation, context);
    }
    
    // Try circuit breaker
    if (this.strategies.circuit_breaker && !result) {
      result = await this.applyCircuitBreakerStrategy(context.operation, context);
    }
    
    // Try fallback
    if (this.strategies.fallback && !result) {
      result = await this.applyFallbackStrategy(context.operation, context);
    }
    
    // Try graceful degradation
    if (this.strategies.graceful_degradation && !result) {
      result = await this.applyGracefulDegradationStrategy(context.operation, context);
    }
    
    return result;
  }

  async applyRetryStrategy(operation, context) {
    const maxAttempts = this.retry.max_attempts || 3;
    const baseDelay = this.parseTime(this.retry.base_delay || '1000ms');
    const maxDelay = this.parseTime(this.retry.max_delay || '10000ms');
    const strategy = this.retry.backoff_strategy || 'exponential';
    
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        return await operation();
      } catch (error) {
        if (attempt === maxAttempts) {
          throw error;
        }
        
        const delay = this.calculateBackoffDelay(attempt, baseDelay, maxDelay, strategy);
        await this.sleep(delay);
      }
    }
  }

  calculateBackoffDelay(attempt, baseDelay, maxDelay, strategy) {
    let delay;
    
    switch (strategy) {
      case 'exponential':
        delay = baseDelay * Math.pow(2, attempt - 1);
        break;
      case 'linear':
        delay = baseDelay * attempt;
        break;
      case 'constant':
        delay = baseDelay;
        break;
      default:
        delay = baseDelay;
    }
    
    return Math.min(delay, maxDelay);
  }

  async applyCircuitBreakerStrategy(operation, context) {
    const key = this.getOperationKey(context);
    const circuitBreaker = this.getCircuitBreaker(key);
    
    if (circuitBreaker.state === 'open') {
      throw new Error('Circuit breaker is open');
    }
    
    try {
      const result = await operation();
      circuitBreaker.recordSuccess();
      return result;
    } catch (error) {
      circuitBreaker.recordFailure();
      throw error;
    }
  }

  getCircuitBreaker(key) {
    if (!this.circuitBreakers.has(key)) {
      const failureThreshold = this.circuitBreaker.failure_threshold || 5;
      const recoveryTimeout = this.parseTime(this.circuitBreaker.recovery_timeout || '60000ms');
      
      this.circuitBreakers.set(key, new CircuitBreaker(failureThreshold, recoveryTimeout));
    }
    
    return this.circuitBreakers.get(key);
  }

  async applyFallbackStrategy(operation, context) {
    const fallbackStrategies = this.fallback.strategies || [];
    
    for (const strategy of fallbackStrategies) {
      try {
        const result = await this.executeFallbackStrategy(strategy, context);
        if (result !== null) {
          return result;
        }
      } catch (error) {
        console.error(`Fallback strategy ${strategy} failed:`, error);
      }
    }
    
    throw new Error('All fallback strategies failed');
  }

  async executeFallbackStrategy(strategy, context) {
    switch (strategy) {
      case 'cache':
        return await this.getFromCache(context.cacheKey);
      case 'default_value':
        return context.defaultValue || null;
      case 'alternative_service':
        return await this.callAlternativeService(context);
      case 'degraded_mode':
        return await this.runInDegradedMode(context);
      default:
        return null;
    }
  }

  async applyGracefulDegradationStrategy(operation, context) {
    // Implement graceful degradation logic
    console.log('Running in graceful degradation mode');
    return { status: 'degraded', message: 'Service running with reduced functionality' };
  }

  async logError(error, context, errorInfo) {
    if (!this.logging.enabled) return;
    
    const logData = {
      timestamp: new Date().toISOString(),
      error: {
        type: errorInfo.type,
        message: errorInfo.message,
        category: errorInfo.category,
        severity: errorInfo.severity,
        retryable: errorInfo.retryable
      },
      context: context
    };
    
    if (this.logging.include_stack_trace) {
      logData.error.stack = errorInfo.stack;
    }
    
    console.error('Error logged:', logData);
  }

  parseTime(timeStr) {
    const match = timeStr.match(/^(\d+)([ms])$/);
    if (!match) return 1000; // Default 1 second

    const [, value, unit] = match;
    const multipliers = { 'ms': 1, 's': 1000 };
    return parseInt(value) * multipliers[unit];
  }

  getOperationKey(context) {
    return `${context.service || 'unknown'}-${context.operation || 'unknown'}`;
  }

  async sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  async getFromCache(key) {
    // Cache implementation
    return null;
  }

  async callAlternativeService(context) {
    // Alternative service implementation
    return null;
  }

  async runInDegradedMode(context) {
    // Degraded mode implementation
    return { mode: 'degraded' };
  }
}

module.exports = AdvancedErrorHandler;
```

### Circuit Breaker

```javascript
// circuit-breaker.js
class CircuitBreaker {
  constructor(failureThreshold, recoveryTimeout) {
    this.failureThreshold = failureThreshold;
    this.recoveryTimeout = recoveryTimeout;
    this.state = 'closed'; // closed, open, half-open
    this.failureCount = 0;
    this.lastFailureTime = null;
    this.successCount = 0;
  }

  recordSuccess() {
    this.failureCount = 0;
    this.successCount++;
    
    if (this.state === 'half-open' && this.successCount >= this.failureThreshold) {
      this.close();
    }
  }

  recordFailure() {
    this.failureCount++;
    this.lastFailureTime = Date.now();
    this.successCount = 0;
    
    if (this.failureCount >= this.failureThreshold) {
      this.open();
    }
  }

  open() {
    this.state = 'open';
    console.log('Circuit breaker opened');
  }

  close() {
    this.state = 'closed';
    this.failureCount = 0;
    this.successCount = 0;
    console.log('Circuit breaker closed');
  }

  halfOpen() {
    this.state = 'half-open';
    console.log('Circuit breaker half-open');
  }

  canExecute() {
    if (this.state === 'closed') {
      return true;
    }
    
    if (this.state === 'open') {
      if (Date.now() - this.lastFailureTime >= this.recoveryTimeout) {
        this.halfOpen();
        return true;
      }
      return false;
    }
    
    if (this.state === 'half-open') {
      return true;
    }
    
    return false;
  }

  getState() {
    return {
      state: this.state,
      failureCount: this.failureCount,
      successCount: this.successCount,
      lastFailureTime: this.lastFailureTime
    };
  }
}

module.exports = CircuitBreaker;
```

### Error Recovery Manager

```javascript
// error-recovery-manager.js
class ErrorRecoveryManager {
  constructor() {
    this.recoveryStrategies = new Map();
    this.recoveryHistory = [];
  }

  registerRecoveryStrategy(errorType, strategy) {
    this.recoveryStrategies.set(errorType, strategy);
  }

  async attemptRecovery(error, context) {
    const errorType = error.constructor.name;
    const strategy = this.recoveryStrategies.get(errorType);
    
    if (!strategy) {
      throw new Error(`No recovery strategy found for error type: ${errorType}`);
    }
    
    const recoveryAttempt = {
      timestamp: Date.now(),
      errorType: errorType,
      context: context,
      success: false
    };
    
    try {
      const result = await strategy(error, context);
      recoveryAttempt.success = true;
      recoveryAttempt.result = result;
      
      this.recoveryHistory.push(recoveryAttempt);
      return result;
    } catch (recoveryError) {
      recoveryAttempt.error = recoveryError.message;
      this.recoveryHistory.push(recoveryAttempt);
      throw recoveryError;
    }
  }

  getRecoveryHistory() {
    return this.recoveryHistory;
  }

  getRecoverySuccessRate() {
    if (this.recoveryHistory.length === 0) return 0;
    
    const successful = this.recoveryHistory.filter(attempt => attempt.success).length;
    return (successful / this.recoveryHistory.length * 100).toFixed(2) + '%';
  }
}

module.exports = ErrorRecoveryManager;
```

## TypeScript Implementation

```typescript
// advanced-error-handling.types.ts
export interface ErrorHandlingConfig {
  strategies?: ErrorStrategies;
  retry?: RetryConfig;
  circuit_breaker?: CircuitBreakerConfig;
  fallback?: FallbackConfig;
  logging?: ErrorLoggingConfig;
}

export interface ErrorStrategies {
  retry?: boolean;
  circuit_breaker?: boolean;
  fallback?: boolean;
  graceful_degradation?: boolean;
}

export interface RetryConfig {
  max_attempts?: number;
  backoff_strategy?: string;
  base_delay?: string;
  max_delay?: string;
}

export interface CircuitBreakerConfig {
  failure_threshold?: number;
  recovery_timeout?: string;
  half_open_state?: boolean;
  monitoring_window?: string;
}

export interface FallbackConfig {
  enabled?: boolean;
  strategies?: string[];
}

export interface ErrorLoggingConfig {
  enabled?: boolean;
  level?: string;
  include_stack_trace?: boolean;
  include_context?: boolean;
  error_categories?: string[];
}

export interface ErrorInfo {
  type: string;
  message: string;
  stack?: string;
  category: string;
  retryable: boolean;
  severity: string;
}

export interface ErrorHandler {
  handleError(error: Error, context?: any): Promise<any>;
  analyzeError(error: Error): ErrorInfo;
  applyStrategies(error: Error, context?: any, errorInfo?: ErrorInfo): Promise<any>;
}

// advanced-error-handling.ts
import { ErrorHandlingConfig, ErrorHandler, ErrorInfo } from './advanced-error-handling.types';

export class TypeScriptAdvancedErrorHandler implements ErrorHandler {
  private config: ErrorHandlingConfig;

  constructor(config: ErrorHandlingConfig) {
    this.config = config;
  }

  async handleError(error: Error, context: any = {}): Promise<any> {
    const errorInfo = this.analyzeError(error);
    return await this.applyStrategies(error, context, errorInfo);
  }

  analyzeError(error: Error): ErrorInfo {
    return {
      type: error.constructor.name,
      message: error.message,
      stack: error.stack,
      category: this.categorizeError(error),
      retryable: this.isRetryable(error),
      severity: this.calculateSeverity(error)
    };
  }

  async applyStrategies(error: Error, context: any = {}, errorInfo?: ErrorInfo): Promise<any> {
    // Strategy implementation
    return null;
  }

  private categorizeError(error: Error): string {
    return 'business_logic';
  }

  private isRetryable(error: Error): boolean {
    return false;
  }

  private calculateSeverity(error: Error): string {
    return 'medium';
  }
}
```

## Advanced Usage Scenarios

### Database Error Recovery

```javascript
// database-error-recovery.js
class DatabaseErrorRecovery {
  constructor(errorHandler) {
    this.errorHandler = errorHandler;
  }

  async executeQuery(query, params = []) {
    const context = {
      operation: 'database_query',
      query: query,
      params: params,
      service: 'database'
    };
    
    try {
      return await this.performQuery(query, params);
    } catch (error) {
      return await this.errorHandler.handleError(error, context);
    }
  }

  async performQuery(query, params) {
    // Database query implementation
    throw new Error('Database connection failed');
  }
}
```

### Network Error Recovery

```javascript
// network-error-recovery.js
class NetworkErrorRecovery {
  constructor(errorHandler) {
    this.errorHandler = errorHandler;
  }

  async makeRequest(url, options = {}) {
    const context = {
      operation: 'http_request',
      url: url,
      method: options.method || 'GET',
      service: 'network'
    };
    
    try {
      return await this.performRequest(url, options);
    } catch (error) {
      return await this.errorHandler.handleError(error, context);
    }
  }

  async performRequest(url, options) {
    // HTTP request implementation
    throw new Error('Network timeout');
  }
}
```

## Real-World Examples

### Express.js Error Handling

```javascript
// express-error-handling.js
const express = require('express');
const AdvancedErrorHandler = require('./advanced-error-handler');

class ExpressErrorHandling {
  constructor(app, config) {
    this.app = app;
    this.errorHandler = new AdvancedErrorHandler(config);
  }

  setupErrorHandling() {
    // Global error handler
    this.app.use(async (error, req, res, next) => {
      const context = {
        operation: 'http_request',
        method: req.method,
        url: req.url,
        userAgent: req.headers['user-agent'],
        ip: req.ip
      };
      
      try {
        const result = await this.errorHandler.handleError(error, context);
        res.status(500).json({
          error: 'Internal server error',
          message: result.message || 'An error occurred'
        });
      } catch (handledError) {
        res.status(500).json({
          error: 'Internal server error',
          message: 'An unexpected error occurred'
        });
      }
    });
    
    // 404 handler
    this.app.use((req, res) => {
      res.status(404).json({
        error: 'Not found',
        message: 'The requested resource was not found'
      });
    });
  }

  async handleAsyncError(operation, context = {}) {
    try {
      return await operation();
    } catch (error) {
      return await this.errorHandler.handleError(error, context);
    }
  }
}
```

### API Error Handling

```javascript
// api-error-handling.js
class APIErrorHandling {
  constructor(errorHandler) {
    this.errorHandler = errorHandler;
  }

  async handleAPIRequest(operation, context = {}) {
    try {
      return await operation();
    } catch (error) {
      const result = await this.errorHandler.handleError(error, context);
      
      if (result) {
        return result;
      }
      
      throw new Error('API request failed');
    }
  }

  createErrorResponse(error, context) {
    return {
      success: false,
      error: {
        type: error.constructor.name,
        message: error.message,
        code: error.code || 'UNKNOWN_ERROR'
      },
      timestamp: new Date().toISOString(),
      requestId: context.requestId
    };
  }
}
```

## Performance Considerations

### Error Handling Performance

```javascript
// error-handling-performance.js
class ErrorHandlingPerformance {
  constructor() {
    this.metrics = {
      errorHandlingCalls: 0,
      avgHandlingTime: 0,
      successRate: 0
    };
  }

  async measureErrorHandling(operation) {
    const start = Date.now();
    
    try {
      const result = await operation();
      this.recordSuccess(Date.now() - start);
      return result;
    } catch (error) {
      this.recordFailure(Date.now() - start);
      throw error;
    }
  }

  recordSuccess(duration) {
    this.metrics.errorHandlingCalls++;
    this.metrics.avgHandlingTime = 
      (this.metrics.avgHandlingTime * (this.metrics.errorHandlingCalls - 1) + duration) / this.metrics.errorHandlingCalls;
    
    const successCount = this.metrics.errorHandlingCalls - this.metrics.failures;
    this.metrics.successRate = (successCount / this.metrics.errorHandlingCalls * 100).toFixed(2);
  }

  recordFailure(duration) {
    this.metrics.errorHandlingCalls++;
    this.metrics.failures = (this.metrics.failures || 0) + 1;
    this.metrics.avgHandlingTime = 
      (this.metrics.avgHandlingTime * (this.metrics.errorHandlingCalls - 1) + duration) / this.metrics.errorHandlingCalls;
  }

  getMetrics() {
    return this.metrics;
  }
}
```

## Best Practices

### Error Configuration Management

```javascript
// error-config-manager.js
class ErrorConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No error handling configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.strategies || Object.keys(config.strategies).length === 0) {
      throw new Error('At least one error handling strategy must be enabled');
    }
    
    return config;
  }
}
```

### Error Health Monitoring

```javascript
// error-health-monitor.js
class ErrorHealthMonitor {
  constructor(errorHandler) {
    this.errorHandler = errorHandler;
    this.metrics = {
      healthChecks: 0,
      failures: 0,
      avgResponseTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      
      // Test error handling with a mock error
      const mockError = new Error('Test error');
      await this.errorHandler.handleError(mockError, { test: true });
      
      const responseTime = Date.now() - start;
      
      this.metrics.healthChecks++;
      this.metrics.avgResponseTime = 
        (this.metrics.avgResponseTime * (this.metrics.healthChecks - 1) + responseTime) / this.metrics.healthChecks;
      
      return {
        status: 'healthy',
        responseTime,
        metrics: this.metrics
      };
    } catch (error) {
      this.metrics.failures++;
      return {
        status: 'unhealthy',
        error: error.message,
        metrics: this.metrics
      };
    }
  }
}
```

## Related Topics

- [@error Operator](./57-tsklang-javascript-operator-error.md)
- [@retry Operator](./58-tsklang-javascript-operator-retry.md)
- [@fallback Operator](./61-tsklang-javascript-operator-fallback.md)
- [@log Operator](./47-tsklang-javascript-operator-log.md) 
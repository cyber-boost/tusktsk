# @learn Function - JavaScript

## Overview

The `@learn` function in TuskLang provides machine learning capabilities that allow your configuration to adapt and improve over time based on data patterns and user behavior. This revolutionary feature enables JavaScript applications to create intelligent, self-optimizing systems that learn from experience.

## Basic Syntax

```tsk
# Simple learning with default values
optimal_cache_duration: @learn("cache_duration", "5m", {"metric": "hit_rate", "target": 0.9})
optimal_batch_size: @learn("batch_size", 100, {"metric": "throughput", "target": "maximize"})

# Learning with constraints
optimal_timeout: @learn("api_timeout", 30000, {"metric": "response_time", "target": "minimize", "constraints": {"min": 1000, "max": 60000}})
optimal_threads: @learn("worker_threads", 4, {"metric": "cpu_usage", "target": "optimize", "constraints": {"min": 1, "max": 16}})
```

## JavaScript Integration

### Node.js Machine Learning Integration

```javascript
const tusk = require('tusklang');

// Load configuration with learning capabilities
const config = tusk.load('learning.tsk');

// Access learned values
console.log(config.optimal_cache_duration); // "7m" (learned from data)
console.log(config.optimal_batch_size); // 150 (learned from data)

// Use learned values in application
const cacheManager = {
  duration: config.optimal_cache_duration,
  batchSize: config.optimal_batch_size
};

// Dynamic learning
const performanceData = {
  responseTime: 125,
  throughput: 1000,
  errorRate: 0.01
};

await tusk.learn("api_config", {
  timeout: 30000,
  retries: 3,
  poolSize: 10
}, {
  metric: "response_time",
  target: "minimize",
  data: performanceData
});
```

### Browser Machine Learning Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('learning.tsk');

// Use learned values in frontend
class AdaptiveUI {
  constructor() {
    this.cacheDuration = config.optimal_cache_duration;
    this.batchSize = config.optimal_batch_size;
  }
  
  async adaptToUserBehavior(userData) {
    const learningResult = await tusk.learn("ui_config", {
      animationSpeed: 300,
      batchSize: 20,
      refreshInterval: 5000
    }, {
      metric: "user_satisfaction",
      target: "maximize",
      data: userData
    });
    
    this.applyLearnedSettings(learningResult);
  }
  
  applyLearnedSettings(settings) {
    // Apply learned settings to UI
    document.documentElement.style.setProperty('--animation-speed', `${settings.animationSpeed}ms`);
    this.batchSize = settings.batchSize;
    this.refreshInterval = settings.refreshInterval;
  }
}
```

## Advanced Usage

### Multi-Objective Learning

```tsk
# Learning with multiple objectives
optimal_config: @learn("app_config", {"cache_duration": "5m", "batch_size": 100}, {
  "metrics": [
    {"name": "response_time", "target": "minimize", "weight": 0.6},
    {"name": "memory_usage", "target": "minimize", "weight": 0.3},
    {"name": "throughput", "target": "maximize", "weight": 0.1}
  ]
})

# Adaptive thresholds
dynamic_threshold: @learn("error_threshold", 0.05, {
  "metric": "system_stability",
  "target": "optimize",
  "adaptation_rate": 0.1
})
```

### Context-Aware Learning

```tsk
# Learning based on context
contextual_config: @learn("api_config", {"timeout": 30000, "retries": 3}, {
  "context": {
    "time_of_day": "peak_hours",
    "user_type": "premium",
    "load_level": "high"
  },
  "metric": "user_satisfaction",
  "target": "maximize"
})

# Environment-specific learning
env_optimized: @learn("performance_config", {"cache_size": 1000, "pool_size": 10}, {
  "environment": "production",
  "metric": "response_time",
  "target": "minimize"
})
```

### Reinforcement Learning

```tsk
# Reinforcement learning for optimization
rl_optimizer: @learn("optimization_policy", {"action": "increase_cache", "probability": 0.5}, {
  "type": "reinforcement",
  "reward_function": "response_time_improvement",
  "exploration_rate": 0.1
})
```

## JavaScript Implementation

### Custom Learning Manager

```javascript
class TuskLearningManager {
  constructor() {
    this.models = new Map();
    this.dataCollectors = new Map();
    this.optimizers = new Map();
  }
  
  async learn(parameterName, defaultValue, options = {}) {
    const modelKey = this.generateModelKey(parameterName, options);
    
    // Get or create learning model
    let model = this.models.get(modelKey);
    if (!model) {
      model = this.createLearningModel(parameterName, defaultValue, options);
      this.models.set(modelKey, model);
    }
    
    // Collect data if provided
    if (options.data) {
      await this.collectData(modelKey, options.data);
    }
    
    // Update model and get optimal value
    const optimalValue = await this.updateModel(model, options);
    
    return optimalValue;
  }
  
  createLearningModel(parameterName, defaultValue, options) {
    const modelType = options.type || 'gradient_descent';
    
    switch (modelType) {
      case 'gradient_descent':
        return new GradientDescentModel(parameterName, defaultValue, options);
      case 'reinforcement':
        return new ReinforcementLearningModel(parameterName, defaultValue, options);
      case 'bayesian':
        return new BayesianOptimizationModel(parameterName, defaultValue, options);
      default:
        return new SimpleLearningModel(parameterName, defaultValue, options);
    }
  }
  
  async collectData(modelKey, data) {
    if (!this.dataCollectors.has(modelKey)) {
      this.dataCollectors.set(modelKey, []);
    }
    
    const collector = this.dataCollectors.get(modelKey);
    collector.push({
      ...data,
      timestamp: Date.now()
    });
    
    // Keep only recent data
    if (collector.length > 1000) {
      collector.splice(0, collector.length - 1000);
    }
  }
  
  async updateModel(model, options) {
    const data = this.dataCollectors.get(model.key) || [];
    
    if (data.length < 10) {
      // Not enough data, return default
      return model.defaultValue;
    }
    
    // Update model with collected data
    await model.update(data, options);
    
    // Get optimal value
    return await model.getOptimalValue(options);
  }
  
  generateModelKey(parameterName, options) {
    const context = options.context ? JSON.stringify(options.context) : '';
    const environment = options.environment || 'default';
    return `${parameterName}_${environment}_${context}`;
  }
}

class GradientDescentModel {
  constructor(parameterName, defaultValue, options) {
    this.key = parameterName;
    this.defaultValue = defaultValue;
    this.currentValue = defaultValue;
    this.learningRate = options.learning_rate || 0.01;
    this.metric = options.metric;
    this.target = options.target;
  }
  
  async update(data, options) {
    // Calculate gradient based on metric and target
    const gradient = this.calculateGradient(data, options);
    
    // Update current value
    if (this.target === 'minimize') {
      this.currentValue -= this.learningRate * gradient;
    } else if (this.target === 'maximize') {
      this.currentValue += this.learningRate * gradient;
    }
    
    // Apply constraints
    this.currentValue = this.applyConstraints(this.currentValue, options.constraints);
  }
  
  calculateGradient(data, options) {
    // Simple gradient calculation based on recent performance
    const recentData = data.slice(-10);
    const avgMetric = recentData.reduce((sum, d) => sum + d[this.metric], 0) / recentData.length;
    
    // Calculate gradient (simplified)
    return avgMetric * 0.1;
  }
  
  applyConstraints(value, constraints = {}) {
    if (constraints.min !== undefined) {
      value = Math.max(value, constraints.min);
    }
    if (constraints.max !== undefined) {
      value = Math.min(value, constraints.max);
    }
    return value;
  }
  
  async getOptimalValue(options) {
    return this.currentValue;
  }
}
```

### TypeScript Support

```typescript
interface LearningOptions {
  metric?: string;
  target?: 'minimize' | 'maximize' | 'optimize';
  constraints?: {
    min?: number;
    max?: number;
  };
  type?: 'gradient_descent' | 'reinforcement' | 'bayesian';
  learning_rate?: number;
  context?: Record<string, any>;
  environment?: string;
  data?: Record<string, any>;
}

interface LearningModel {
  key: string;
  defaultValue: any;
  update(data: any[], options: LearningOptions): Promise<void>;
  getOptimalValue(options: LearningOptions): Promise<any>;
}

class TypedLearningManager {
  private models: Map<string, LearningModel>;
  private dataCollectors: Map<string, any[]>;
  
  constructor() {
    this.models = new Map();
    this.dataCollectors = new Map();
  }
  
  async learn<T>(
    parameterName: string,
    defaultValue: T,
    options: LearningOptions = {}
  ): Promise<T> {
    const modelKey = this.generateModelKey(parameterName, options);
    
    let model = this.models.get(modelKey);
    if (!model) {
      model = this.createLearningModel(parameterName, defaultValue, options);
      this.models.set(modelKey, model);
    }
    
    if (options.data) {
      await this.collectData(modelKey, options.data);
    }
    
    const optimalValue = await this.updateModel(model, options);
    return optimalValue as T;
  }
  
  private createLearningModel<T>(
    parameterName: string,
    defaultValue: T,
    options: LearningOptions
  ): LearningModel {
    // Implementation similar to JavaScript version
    return new GradientDescentModel(parameterName, defaultValue, options);
  }
  
  private async collectData(modelKey: string, data: Record<string, any>): Promise<void> {
    // Implementation similar to JavaScript version
  }
  
  private async updateModel(model: LearningModel, options: LearningOptions): Promise<any> {
    // Implementation similar to JavaScript version
  }
  
  private generateModelKey(parameterName: string, options: LearningOptions): string {
    // Implementation similar to JavaScript version
    return parameterName;
  }
}
```

## Real-World Examples

### API Performance Optimization

```tsk
# Adaptive API configuration
adaptive_api_config: @learn("api_config", {"timeout": 30000, "retries": 3, "pool_size": 10}, {
  "metrics": [
    {"name": "response_time", "target": "minimize", "weight": 0.7},
    {"name": "error_rate", "target": "minimize", "weight": 0.3}
  ],
  "constraints": {
    "timeout": {"min": 1000, "max": 60000},
    "retries": {"min": 1, "max": 5},
    "pool_size": {"min": 5, "max": 50}
  }
})

# Load-based optimization
load_optimized: @learn("connection_pool", {"size": 10, "idle_timeout": 30000}, {
  "metric": "connection_efficiency",
  "target": "maximize",
  "context": {"load_level": "current"}
})
```

### User Experience Optimization

```tsk
# UI performance optimization
ui_optimization: @learn("ui_config", {"animation_duration": 300, "batch_size": 20}, {
  "metric": "user_satisfaction",
  "target": "maximize",
  "context": {
    "device_type": "mobile",
    "connection_speed": "slow"
  }
})

# Content recommendation
content_recommendation: @learn("recommendation_weights", {"recency": 0.3, "popularity": 0.4, "relevance": 0.3}, {
  "metric": "click_through_rate",
  "target": "maximize",
  "type": "reinforcement"
})
```

### System Resource Optimization

```tsk
# Memory optimization
memory_optimization: @learn("memory_config", {"cache_size": 1000, "gc_threshold": 0.8}, {
  "metric": "memory_efficiency",
  "target": "maximize",
  "constraints": {
    "cache_size": {"min": 100, "max": 10000},
    "gc_threshold": {"min": 0.5, "max": 0.95}
  }
})

# CPU optimization
cpu_optimization: @learn("cpu_config", {"thread_count": 4, "task_queue_size": 100}, {
  "metric": "cpu_utilization",
  "target": "optimize",
  "constraints": {
    "thread_count": {"min": 1, "max": 16},
    "task_queue_size": {"min": 10, "max": 1000}
  }
})
```

## Performance Considerations

### Learning Efficiency

```tsk
# Efficient learning with sampling
sampled_learning: @learn("efficient_config", {"batch_size": 100}, {
  "metric": "throughput",
  "target": "maximize",
  "sampling_rate": 0.1
})
```

### Model Persistence

```javascript
// Persist learned models for reuse
class PersistentLearningManager extends TuskLearningManager {
  constructor() {
    super();
    this.storage = new Map();
  }
  
  async saveModel(modelKey, model) {
    const modelData = {
      currentValue: model.currentValue,
      parameters: model.parameters,
      timestamp: Date.now()
    };
    
    this.storage.set(modelKey, modelData);
    
    // Save to persistent storage
    await this.saveToStorage(modelKey, modelData);
  }
  
  async loadModel(modelKey) {
    const savedData = await this.loadFromStorage(modelKey);
    if (savedData) {
      const model = this.models.get(modelKey);
      if (model) {
        model.currentValue = savedData.currentValue;
        model.parameters = savedData.parameters;
      }
    }
  }
}
```

## Security Notes

- **Data Privacy**: Ensure learning data doesn't contain sensitive information
- **Model Security**: Protect learned models from unauthorized access
- **Bias Prevention**: Monitor for bias in learned models

```javascript
// Secure learning implementation
class SecureLearningManager extends TuskLearningManager {
  constructor() {
    super();
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i
    ];
  }
  
  async learn(parameterName, defaultValue, options = {}) {
    // Sanitize data before learning
    if (options.data) {
      options.data = this.sanitizeData(options.data);
    }
    
    // Validate learning parameters
    this.validateLearningParameters(parameterName, options);
    
    return await super.learn(parameterName, defaultValue, options);
  }
  
  sanitizeData(data) {
    const sanitized = {};
    
    Object.keys(data).forEach(key => {
      if (!this.sensitivePatterns.some(pattern => pattern.test(key))) {
        sanitized[key] = data[key];
      }
    });
    
    return sanitized;
  }
  
  validateLearningParameters(parameterName, options) {
    // Validate that learning parameters are safe
    const allowedParameters = ['timeout', 'batch_size', 'cache_size', 'pool_size'];
    
    if (!allowedParameters.includes(parameterName)) {
      throw new Error(`Learning not allowed for parameter: ${parameterName}`);
    }
  }
}
```

## Best Practices

1. **Data Quality**: Ensure high-quality data for learning
2. **Model Validation**: Validate learned models before deployment
3. **Monitoring**: Monitor learning performance and model drift
4. **Constraints**: Always set appropriate constraints for learned values
5. **Fallbacks**: Provide fallback values when learning fails
6. **Documentation**: Document learning objectives and constraints

## Next Steps

- Explore [@optimize function](./049-at-optimize-function-javascript.md) for performance optimization
- Master [@http host](./050-at-http-host-javascript.md) for HTTP operations
- Learn about [@env variables](./051-at-env-variables-javascript.md) for environment management 
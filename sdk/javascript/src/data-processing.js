/**
 * TuskLang Advanced Data Processing and Analytics Engine
 * Provides comprehensive data processing, analytics, and machine learning capabilities
 */

const { EventEmitter } = require('events');

class DataProcessor {
  constructor(options = {}) {
    this.options = {
      batchSize: options.batchSize || 1000,
      maxWorkers: options.maxWorkers || 4,
      cacheSize: options.cacheSize || 10000,
      ...options
    };
    
    this.pipelines = new Map();
    this.transformers = new Map();
    this.analytics = new Map();
    this.cache = new Map();
    this.workers = [];
    this.eventEmitter = new EventEmitter();
    this.isRunning = false;
  }

  /**
   * Create a data processing pipeline
   */
  createPipeline(name, steps = []) {
    const pipeline = {
      name,
      steps: [...steps],
      config: {},
      metrics: {
        processed: 0,
        errors: 0,
        processingTime: 0
      }
    };

    this.pipelines.set(name, pipeline);
    return pipeline;
  }

  /**
   * Add step to pipeline
   */
  addPipelineStep(pipelineName, step) {
    const pipeline = this.pipelines.get(pipelineName);
    if (!pipeline) {
      throw new Error(`Pipeline not found: ${pipelineName}`);
    }

    pipeline.steps.push(step);
    return pipeline;
  }

  /**
   * Process data through pipeline
   */
  async processData(pipelineName, data, options = {}) {
    const pipeline = this.pipelines.get(pipelineName);
    if (!pipeline) {
      throw new Error(`Pipeline not found: ${pipelineName}`);
    }

    const startTime = Date.now();
    let result = data;

    try {
      for (const step of pipeline.steps) {
        result = await this.executeStep(step, result, options);
      }

      // Update metrics
      const processingTime = Date.now() - startTime;
      pipeline.metrics.processed++;
      pipeline.metrics.processingTime = 
        (pipeline.metrics.processingTime * (pipeline.metrics.processed - 1) + processingTime) / 
        pipeline.metrics.processed;

      this.eventEmitter.emit('dataProcessed', { pipelineName, result, processingTime });
      return result;
    } catch (error) {
      pipeline.metrics.errors++;
      this.eventEmitter.emit('processingError', { pipelineName, error });
      throw error;
    }
  }

  /**
   * Execute a processing step
   */
  async executeStep(step, data, options) {
    if (typeof step === 'function') {
      return await step(data, options);
    }

    if (typeof step === 'object' && step.type) {
      const transformer = this.transformers.get(step.type);
      if (transformer) {
        return await transformer(data, step.config || {}, options);
      }
    }

    throw new Error(`Invalid step: ${JSON.stringify(step)}`);
  }

  /**
   * Register a transformer
   */
  registerTransformer(name, transformer) {
    this.transformers.set(name, transformer);
  }

  /**
   * Process data in batches
   */
  async processBatch(pipelineName, dataArray, options = {}) {
    const results = [];
    const batchSize = options.batchSize || this.options.batchSize;

    for (let i = 0; i < dataArray.length; i += batchSize) {
      const batch = dataArray.slice(i, i + batchSize);
      const batchResult = await this.processData(pipelineName, batch, options);
      results.push(...batchResult);
    }

    return results;
  }

  /**
   * Get pipeline statistics
   */
  getPipelineStats(pipelineName) {
    const pipeline = this.pipelines.get(pipelineName);
    if (!pipeline) {
      return null;
    }

    return {
      name: pipeline.name,
      steps: pipeline.steps.length,
      metrics: { ...pipeline.metrics }
    };
  }

  /**
   * Clear pipeline cache
   */
  clearCache() {
    this.cache.clear();
  }
}

class AnalyticsEngine {
  constructor() {
    this.analytics = new Map();
    this.models = new Map();
    this.metrics = new Map();
  }

  /**
   * Register an analytics function
   */
  registerAnalytics(name, analyticsFunction) {
    this.analytics.set(name, analyticsFunction);
  }

  /**
   * Perform analytics on data
   */
  async analyze(data, analyticsName, options = {}) {
    const analytics = this.analytics.get(analyticsName);
    if (!analytics) {
      throw new Error(`Analytics not found: ${analyticsName}`);
    }

    const startTime = Date.now();
    const result = await analytics(data, options);
    const processingTime = Date.now() - startTime;

    // Store metrics
    if (!this.metrics.has(analyticsName)) {
      this.metrics.set(analyticsName, []);
    }
    this.metrics.get(analyticsName).push({
      timestamp: Date.now(),
      processingTime,
      dataSize: Array.isArray(data) ? data.length : 1
    });

    return result;
  }

  /**
   * Calculate basic statistics
   */
  calculateStatistics(data) {
    if (!Array.isArray(data) || data.length === 0) {
      return null;
    }

    const numericData = data.filter(x => typeof x === 'number');
    if (numericData.length === 0) {
      return null;
    }

    const sum = numericData.reduce((a, b) => a + b, 0);
    const mean = sum / numericData.length;
    const sorted = numericData.sort((a, b) => a - b);
    const median = sorted.length % 2 === 0 
      ? (sorted[sorted.length / 2 - 1] + sorted[sorted.length / 2]) / 2
      : sorted[Math.floor(sorted.length / 2)];

    const variance = numericData.reduce((acc, val) => acc + Math.pow(val - mean, 2), 0) / numericData.length;
    const stdDev = Math.sqrt(variance);

    return {
      count: numericData.length,
      sum,
      mean,
      median,
      min: Math.min(...numericData),
      max: Math.max(...numericData),
      variance,
      stdDev
    };
  }

  /**
   * Perform data aggregation
   */
  aggregate(data, groupBy, aggregations) {
    const groups = new Map();

    for (const item of data) {
      const groupKey = typeof groupBy === 'function' ? groupBy(item) : item[groupBy];
      
      if (!groups.has(groupKey)) {
        groups.set(groupKey, []);
      }
      groups.get(groupKey).push(item);
    }

    const result = [];
    for (const [key, groupData] of groups) {
      const aggregated = { group: key };
      
      for (const [field, operation] of Object.entries(aggregations)) {
        switch (operation) {
          case 'sum':
            aggregated[field] = groupData.reduce((sum, item) => sum + (item[field] || 0), 0);
            break;
          case 'avg':
            aggregated[field] = groupData.reduce((sum, item) => sum + (item[field] || 0), 0) / groupData.length;
            break;
          case 'count':
            aggregated[field] = groupData.length;
            break;
          case 'min':
            aggregated[field] = Math.min(...groupData.map(item => item[field] || Infinity));
            break;
          case 'max':
            aggregated[field] = Math.max(...groupData.map(item => item[field] || -Infinity));
            break;
        }
      }
      
      result.push(aggregated);
    }

    return result;
  }

  /**
   * Perform data filtering
   */
  filter(data, predicate) {
    if (typeof predicate === 'function') {
      return data.filter(predicate);
    }

    if (typeof predicate === 'object') {
      return data.filter(item => {
        for (const [key, value] of Object.entries(predicate)) {
          if (item[key] !== value) {
            return false;
          }
        }
        return true;
      });
    }

    return data;
  }

  /**
   * Perform data sorting
   */
  sort(data, key, order = 'asc') {
    const sorted = [...data];
    
    sorted.sort((a, b) => {
      const aVal = typeof key === 'function' ? key(a) : a[key];
      const bVal = typeof key === 'function' ? key(b) : b[key];
      
      if (order === 'desc') {
        return bVal > aVal ? 1 : bVal < aVal ? -1 : 0;
      }
      
      return aVal > bVal ? 1 : aVal < bVal ? -1 : 0;
    });

    return sorted;
  }

  /**
   * Get analytics metrics
   */
  getAnalyticsMetrics(analyticsName) {
    const metrics = this.metrics.get(analyticsName);
    if (!metrics || metrics.length === 0) {
      return null;
    }

    const processingTimes = metrics.map(m => m.processingTime);
    const dataSizes = metrics.map(m => m.dataSize);

    return {
      totalRuns: metrics.length,
      averageProcessingTime: processingTimes.reduce((a, b) => a + b, 0) / processingTimes.length,
      averageDataSize: dataSizes.reduce((a, b) => a + b, 0) / dataSizes.length,
      lastRun: metrics[metrics.length - 1]
    };
  }
}

class MachineLearningEngine {
  constructor() {
    this.models = new Map();
    this.trainingData = new Map();
    this.predictions = new Map();
  }

  /**
   * Register a machine learning model
   */
  registerModel(name, model) {
    this.models.set(name, {
      model,
      trained: false,
      accuracy: 0,
      lastTrained: null
    });
  }

  /**
   * Train a model
   */
  async trainModel(modelName, trainingData, options = {}) {
    const modelInfo = this.models.get(modelName);
    if (!modelInfo) {
      throw new Error(`Model not found: ${modelName}`);
    }

    const startTime = Date.now();
    
    try {
      if (modelInfo.model.train) {
        await modelInfo.model.train(trainingData, options);
      }

      modelInfo.trained = true;
      modelInfo.lastTrained = Date.now();
      modelInfo.accuracy = options.accuracy || 0;

      this.trainingData.set(modelName, {
        data: trainingData,
        timestamp: Date.now(),
        options
      });

      return {
        success: true,
        trainingTime: Date.now() - startTime,
        accuracy: modelInfo.accuracy
      };
    } catch (error) {
      throw new Error(`Training failed: ${error.message}`);
    }
  }

  /**
   * Make predictions using a model
   */
  async predict(modelName, data) {
    const modelInfo = this.models.get(modelName);
    if (!modelInfo) {
      throw new Error(`Model not found: ${modelName}`);
    }

    if (!modelInfo.trained) {
      throw new Error(`Model not trained: ${modelName}`);
    }

    try {
      const prediction = await modelInfo.model.predict(data);
      
      // Store prediction
      if (!this.predictions.has(modelName)) {
        this.predictions.set(modelName, []);
      }
      this.predictions.get(modelName).push({
        input: data,
        prediction,
        timestamp: Date.now()
      });

      return prediction;
    } catch (error) {
      throw new Error(`Prediction failed: ${error.message}`);
    }
  }

  /**
   * Evaluate model performance
   */
  async evaluateModel(modelName, testData) {
    const modelInfo = this.models.get(modelName);
    if (!modelInfo || !modelInfo.trained) {
      throw new Error(`Model not available for evaluation: ${modelName}`);
    }

    const predictions = [];
    const actuals = [];

    for (const item of testData) {
      try {
        const prediction = await this.predict(modelName, item.input);
        predictions.push(prediction);
        actuals.push(item.output);
      } catch (error) {
        console.warn(`Prediction failed for item: ${error.message}`);
      }
    }

    // Calculate accuracy
    let correct = 0;
    for (let i = 0; i < predictions.length; i++) {
      if (predictions[i] === actuals[i]) {
        correct++;
      }
    }

    const accuracy = predictions.length > 0 ? correct / predictions.length : 0;
    modelInfo.accuracy = accuracy;

    return {
      accuracy,
      totalPredictions: predictions.length,
      correctPredictions: correct,
      predictions,
      actuals
    };
  }

  /**
   * Get model information
   */
  getModelInfo(modelName) {
    const modelInfo = this.models.get(modelName);
    if (!modelInfo) {
      return null;
    }

    return {
      name: modelName,
      trained: modelInfo.trained,
      accuracy: modelInfo.accuracy,
      lastTrained: modelInfo.lastTrained,
      predictions: this.predictions.get(modelName)?.length || 0
    };
  }
}

// Built-in transformers
const builtInTransformers = {
  // Filter transformer
  filter: (data, config) => {
    const predicate = config.predicate || (() => true);
    return Array.isArray(data) ? data.filter(predicate) : data;
  },

  // Map transformer
  map: (data, config) => {
    const mapper = config.mapper || (x => x);
    return Array.isArray(data) ? data.map(mapper) : mapper(data);
  },

  // Reduce transformer
  reduce: (data, config) => {
    const reducer = config.reducer || ((acc, val) => acc + val);
    const initialValue = config.initialValue || 0;
    return Array.isArray(data) ? data.reduce(reducer, initialValue) : data;
  },

  // Sort transformer
  sort: (data, config) => {
    const key = config.key || null;
    const order = config.order || 'asc';
    
    if (!Array.isArray(data)) return data;
    
    return data.sort((a, b) => {
      const aVal = key ? a[key] : a;
      const bVal = key ? b[key] : b;
      
      if (order === 'desc') {
        return bVal > aVal ? 1 : bVal < aVal ? -1 : 0;
      }
      
      return aVal > bVal ? 1 : aVal < bVal ? -1 : 0;
    });
  },

  // Group transformer
  group: (data, config) => {
    const groupBy = config.groupBy || 'id';
    
    if (!Array.isArray(data)) return data;
    
    const groups = new Map();
    for (const item of data) {
      const key = typeof groupBy === 'function' ? groupBy(item) : item[groupBy];
      
      if (!groups.has(key)) {
        groups.set(key, []);
      }
      groups.get(key).push(item);
    }
    
    return Object.fromEntries(groups);
  }
};

module.exports = {
  DataProcessor,
  AnalyticsEngine,
  MachineLearningEngine,
  builtInTransformers
}; 
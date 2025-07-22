/**
 * TuskLang Performance Core - Goals 4.1, 4.2, 4.3 Integration
 * Integrates Data Processing, Workflow Orchestration, and Caching Systems
 */

const { DataProcessor, AnalyticsEngine, MachineLearningEngine, builtInTransformers } = require('./data-processing');
const { WorkflowEngine, TaskScheduler } = require('./workflow-orchestration');
const { CacheManager, PerformanceOptimizer, QueryOptimizer, builtInQueryOptimizations } = require('./caching-system');

class TuskPerformanceCore {
  constructor(options = {}) {
    // Initialize core components
    this.dataProcessor = new DataProcessor(options.dataProcessor || {});
    this.analyticsEngine = new AnalyticsEngine();
    this.mlEngine = new MachineLearningEngine();
    this.workflowEngine = new WorkflowEngine(options.workflow || {});
    this.taskScheduler = new TaskScheduler(options.scheduler || {});
    this.cacheManager = new CacheManager(options.cache || {});
    this.performanceOptimizer = new PerformanceOptimizer();
    this.queryOptimizer = new QueryOptimizer();
    
    // Core state
    this.isInitialized = false;
    this.config = options;
    
    // Setup integrations
    this.setupIntegrations();
    
    // Setup built-in optimizations
    this.setupOptimizations();
    
    // Setup built-in transformers
    this.setupTransformers();
  }

  /**
   * Initialize the performance core
   */
  async initialize() {
    try {
      // Create default caches
      this.cacheManager.createCache('data_processing', { ttl: 600000, maxSize: 1000, strategy: 'lru' });
      this.cacheManager.createCache('processed_data', { ttl: 300000, maxSize: 500, strategy: 'lru' });
      this.cacheManager.createCache('scheduled_tasks', { ttl: 300000, maxSize: 200, strategy: 'lru' });
      this.cacheManager.createCache('ml_models', { ttl: 3600000, maxSize: 100, strategy: 'lru' });
      
      // Start cache manager
      this.cacheManager.start();
      
      // Start task scheduler
      this.taskScheduler.start();
      
      this.isInitialized = true;
      console.log('TuskLang Performance Core initialized successfully');
      
      return true;
    } catch (error) {
      console.error('Failed to initialize TuskLang Performance Core:', error);
      throw error;
    }
  }

  /**
   * Setup integrations between components
   */
  setupIntegrations() {
    // Connect data processing to caching
    this.dataProcessor.eventEmitter.on('dataProcessed', (data) => {
      // Cache processed data
      if (data.result && typeof data.result === 'object') {
        this.cacheManager.set('processed_data', data.pipelineName, data.result, 300000);
      }
    });

    // Connect workflow to performance optimization
    this.workflowEngine.eventEmitter.on('workflowCompleted', (data) => {
      // Optimize workflow results
      this.performanceOptimizer.applyOptimization('workflow_results', data.result);
    });

    // Connect task scheduler to analytics
    this.taskScheduler.eventEmitter.on('jobCompleted', (data) => {
      this.analyticsEngine.analyze([data], 'job_completion_analytics');
    });
  }

  /**
   * Setup built-in optimizations
   */
  setupOptimizations() {
    // Register performance optimizations
    this.performanceOptimizer.registerOptimization('data_compression', async (data) => {
      // Simple data compression optimization
      if (Array.isArray(data) && data.length > 1000) {
        return data.slice(-1000); // Keep only last 1000 items
      }
      return data;
    });

    this.performanceOptimizer.registerOptimization('memory_cleanup', async (data) => {
      // Memory cleanup optimization
      if (typeof data === 'object' && data !== null) {
        // Remove null/undefined values
        return Object.fromEntries(
          Object.entries(data).filter(([_, value]) => value != null)
        );
      }
      return data;
    });

    // Register analytics functions
    this.analyticsEngine.registerAnalytics('workflow_performance_analytics', async (data) => {
      return { analyzed: true, count: data.length };
    });

    this.analyticsEngine.registerAnalytics('workflow_error_analytics', async (data) => {
      return { errors: data.length, analyzed: true };
    });

    this.analyticsEngine.registerAnalytics('job_completion_analytics', async (data) => {
      return { jobs: data.length, analyzed: true };
    });

    // Register query optimizations
    for (const [name, optimizer] of Object.entries(builtInQueryOptimizations)) {
      this.queryOptimizer.registerQueryOptimization(name, optimizer);
    }
  }

  /**
   * Setup built-in transformers
   */
  setupTransformers() {
    // Register built-in transformers
    for (const [name, transformer] of Object.entries(builtInTransformers)) {
      this.dataProcessor.registerTransformer(name, transformer);
    }
  }

  /**
   * Process data with caching and optimization
   */
  async processDataWithOptimization(pipelineName, data, options = {}) {
    // Check cache first
    const cacheKey = `${pipelineName}_${JSON.stringify(data).substring(0, 100)}`;
    const cached = this.cacheManager.get('data_processing', cacheKey);
    if (cached) {
      return cached;
    }

    // Apply performance optimizations
    let optimizedData = data;
    optimizedData = await this.performanceOptimizer.applyOptimization('data_compression', optimizedData);
    optimizedData = await this.performanceOptimizer.applyOptimization('memory_cleanup', optimizedData);

    // Process data
    const result = await this.dataProcessor.processData(pipelineName, optimizedData, options);

    // Cache result
    this.cacheManager.set('data_processing', cacheKey, result, 600000); // 10 minutes

    return result;
  }

  /**
   * Execute workflow with performance monitoring
   */
  async executeWorkflowWithMonitoring(workflowName, input = {}, options = {}) {
    const startTime = Date.now();

    try {
      // Apply input optimization
      const optimizedInput = await this.performanceOptimizer.applyOptimization('memory_cleanup', input);

      // Execute workflow
      const result = await this.workflowEngine.executeWorkflow(workflowName, optimizedInput, options);

      // Record analytics
      const executionTime = Date.now() - startTime;
      this.analyticsEngine.analyze([{
        workflowName,
        executionTime,
        inputSize: JSON.stringify(optimizedInput).length,
        resultSize: JSON.stringify(result).length
      }], 'workflow_performance_analytics');

      return result;
    } catch (error) {
      // Record error analytics
      this.analyticsEngine.analyze([{
        workflowName,
        error: error.message,
        executionTime: Date.now() - startTime
      }], 'workflow_error_analytics');
      throw error;
    }
  }

  /**
   * Schedule task with caching
   */
  scheduleTaskWithCaching(name, taskFunction, schedule, options = {}) {
    // Create cached task function
    const cachedTaskFunction = async () => {
      const cacheKey = `scheduled_task_${name}_${Date.now()}`;
      const cached = this.cacheManager.get('scheduled_tasks', cacheKey);
      
      if (cached) {
        return cached;
      }

      const result = await taskFunction();
      this.cacheManager.set('scheduled_tasks', cacheKey, result, 300000); // 5 minutes
      return result;
    };

    return this.taskScheduler.scheduleTask(name, cachedTaskFunction, schedule, options);
  }

  /**
   * Optimize query with caching
   */
  async optimizeQueryWithCaching(query, context = {}) {
    // Check query cache
    const cached = this.queryOptimizer.getCachedQueryResult(query);
    if (cached) {
      return cached;
    }

    // Optimize query
    const optimizedQuery = this.queryOptimizer.optimizeQuery(query, context);

    // Execute query (simulated)
    const result = await this.executeQuery(optimizedQuery);

    // Cache result
    this.queryOptimizer.cacheQueryResult(query, result);

    // Update statistics
    this.queryOptimizer.updateQueryStatistics(query, Date.now(), Array.isArray(result) ? result.length : 1);

    return result;
  }

  /**
   * Execute query (simulated)
   */
  async executeQuery(query) {
    // Simulate query execution
    await new Promise(resolve => setTimeout(resolve, Math.random() * 100));
    
    // Return mock data based on query
    if (query.limit) {
      return Array.from({ length: Math.min(query.limit, 100) }, (_, i) => ({
        id: i + 1,
        name: `Item ${i + 1}`,
        value: Math.random() * 1000
      }));
    }
    
    return Array.from({ length: 50 }, (_, i) => ({
      id: i + 1,
      name: `Item ${i + 1}`,
      value: Math.random() * 1000
    }));
  }

  /**
   * Train ML model with data processing
   */
  async trainModelWithDataProcessing(modelName, trainingData, options = {}) {
    // Create ML preprocessing pipeline if it doesn't exist
    if (!this.dataProcessor.pipelines.has('ml_preprocessing')) {
      this.dataProcessor.createPipeline('ml_preprocessing', [
        { type: 'filter', config: { predicate: (item) => item && typeof item === 'object' } },
        { type: 'map', config: { mapper: (item) => ({ ...item, processed: true }) } }
      ]);
    }

    // Process training data
    const processedData = await this.dataProcessor.processData('ml_preprocessing', trainingData, options);

    // Apply performance optimizations
    const optimizedData = await this.performanceOptimizer.applyOptimization('data_compression', processedData);

    // Train model
    const result = await this.mlEngine.trainModel(modelName, optimizedData, options);

    // Cache model info
    this.cacheManager.set('ml_models', modelName, {
      trained: true,
      lastTrained: Date.now(),
      accuracy: result.accuracy
    }, 3600000); // 1 hour

    return result;
  }

  /**
   * Get comprehensive performance statistics
   */
  getPerformanceStats() {
    return {
      isInitialized: this.isInitialized,
      dataProcessor: {
        pipelines: this.dataProcessor.pipelines.size,
        transformers: this.dataProcessor.transformers.size
      },
      workflowEngine: {
        workflows: this.workflowEngine.workflows.size,
        tasks: this.workflowEngine.tasks.size,
        executions: this.workflowEngine.executions.size
      },
      taskScheduler: {
        jobs: this.taskScheduler.jobs.size,
        runningJobs: this.taskScheduler.runningJobs.size
      },
      cacheManager: {
        caches: this.cacheManager.caches.size
      },
      performanceOptimizer: {
        optimizations: this.performanceOptimizer.optimizations.size
      },
      queryOptimizer: {
        optimizations: this.queryOptimizer.optimizations.size,
        cachedQueries: this.queryOptimizer.queryCache.size
      }
    };
  }

  /**
   * Get analytics data
   */
  getAnalyticsData() {
    return {
      metrics: Array.from(this.analyticsEngine.metrics.entries()).map(([name, metrics]) => ({
        name,
        count: metrics.length,
        latest: metrics[metrics.length - 1]
      })),
      mlModels: Array.from(this.mlEngine.models.entries()).map(([name, model]) => ({
        name,
        trained: model.trained,
        accuracy: model.accuracy,
        predictions: this.mlEngine.predictions.get(name)?.length || 0
      }))
    };
  }

  /**
   * Get cache statistics
   */
  getCacheStats() {
    const stats = {};
    for (const [cacheName, cache] of this.cacheManager.caches) {
      stats[cacheName] = this.cacheManager.getCacheStats(cacheName);
    }
    return stats;
  }

  /**
   * Get workflow statistics
   */
  getWorkflowStats() {
    const stats = {};
    for (const [workflowName, workflow] of this.workflowEngine.workflows) {
      stats[workflowName] = this.workflowEngine.getWorkflowStats(workflowName);
    }
    return stats;
  }

  /**
   * Cleanup and shutdown
   */
  async shutdown() {
    try {
      // Stop cache manager
      this.cacheManager.stop();
      
      // Stop task scheduler
      this.taskScheduler.stop();
      
      console.log('TuskLang Performance Core shutdown completed');
      return true;
    } catch (error) {
      console.error('Error during shutdown:', error);
      throw error;
    }
  }
}

module.exports = {
  TuskPerformanceCore,
  DataProcessor,
  AnalyticsEngine,
  MachineLearningEngine,
  WorkflowEngine,
  TaskScheduler,
  CacheManager,
  PerformanceOptimizer,
  QueryOptimizer,
  builtInTransformers,
  builtInQueryOptimizations
}; 
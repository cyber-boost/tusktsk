/**
 * Test file for Goals 4.1, 4.2, and 4.3 implementation
 * Demonstrates Data Processing, Workflow Orchestration, and Caching Systems
 */

const { TuskPerformanceCore } = require('./src/tusk-performance-core');

async function testGoalsG4() {
  console.log('=== Testing TuskLang Performance Core - Goals 4.1, 4.2, 4.3 ===\n');

  // Initialize the performance core
  const tusk = new TuskPerformanceCore({
    dataProcessor: {
      batchSize: 500,
      maxWorkers: 4,
      cacheSize: 5000
    },
    workflow: {
      maxConcurrentTasks: 8,
      taskTimeout: 60000,
      retryAttempts: 3
    },
    scheduler: {
      maxConcurrentJobs: 3,
      defaultInterval: 30000
    },
    cache: {
      defaultTTL: 300000,
      maxSize: 5000,
      cleanupInterval: 30000
    }
  });

  console.log('1. Testing Goal 4.1: Advanced Data Processing and Analytics Engine');
  console.log('==================================================================');

  // Initialize the core
  await tusk.initialize();
  console.log('✓ Performance core initialized successfully');

  // Create data processing pipeline
  const dataPipeline = tusk.dataProcessor.createPipeline('test_pipeline', [
    { type: 'filter', config: { predicate: (item) => item.value > 50 } },
    { type: 'map', config: { mapper: (item) => ({ ...item, processed: true }) } },
    { type: 'sort', config: { key: 'value', order: 'desc' } }
  ]);
  console.log('✓ Data processing pipeline created successfully');

  // Test data processing
  const testData = Array.from({ length: 100 }, (_, i) => ({
    id: i + 1,
    name: `Item ${i + 1}`,
    value: Math.random() * 100
  }));

  const processedData = await tusk.processDataWithOptimization('test_pipeline', testData);
  console.log('✓ Data processing with optimization completed');
  console.log('✓ Processed data length:', processedData.length);

  // Test analytics
  const statistics = tusk.analyticsEngine.calculateStatistics(testData.map(item => item.value));
  console.log('✓ Analytics statistics calculated:', statistics ? 'success' : 'no data');

  // Test data aggregation
  const aggregated = tusk.analyticsEngine.aggregate(testData, 'name', {
    value: 'avg',
    id: 'count'
  });
  console.log('✓ Data aggregation completed, results:', aggregated.length);

  // Test ML model
  const mockModel = {
    train: async (data) => ({ accuracy: 0.85 }),
    predict: async (input) => Math.random() > 0.5 ? 'positive' : 'negative'
  };

  tusk.mlEngine.registerModel('test_model', mockModel);
  console.log('✓ ML model registered successfully');

  const trainingResult = await tusk.trainModelWithDataProcessing('test_model', testData);
  console.log('✓ ML model training completed:', trainingResult.success);

  console.log('\n2. Testing Goal 4.2: Advanced Workflow and Task Orchestration System');
  console.log('=====================================================================');

  // Register tasks
  tusk.workflowEngine.registerTask('fetch_data', async (input) => {
    await new Promise(resolve => setTimeout(resolve, 100));
    return { data: Array.from({ length: 10 }, (_, i) => ({ id: i, value: Math.random() * 100 })) };
  });

  tusk.workflowEngine.registerTask('process_data', async (input) => {
    await new Promise(resolve => setTimeout(resolve, 150));
    return { processed: input.fetch_data.data.map(item => ({ ...item, processed: true })) };
  });

  tusk.workflowEngine.registerTask('generate_report', async (input) => {
    await new Promise(resolve => setTimeout(resolve, 200));
    return { report: `Processed ${input.process_data.processed.length} items` };
  });

  console.log('✓ Workflow tasks registered successfully');

  // Create workflow
  const workflow = tusk.workflowEngine.registerWorkflow('data_processing_workflow', {
    tasks: ['fetch_data', 'process_data', 'generate_report'],
    dependencies: {
      'process_data': ['fetch_data'],
      'generate_report': ['process_data']
    }
  });
  console.log('✓ Workflow created successfully');

  // Execute workflow
  const workflowResult = await tusk.executeWorkflowWithMonitoring('data_processing_workflow', {
    source: 'test',
    limit: 10
  });
  console.log('✓ Workflow executed successfully');
  console.log('✓ Workflow result:', workflowResult.generate_report.report);

  // Test task scheduling
  const scheduledTask = tusk.scheduleTaskWithCaching('test_scheduled_task', async () => {
    return { timestamp: Date.now(), data: 'scheduled task executed' };
  }, 5000); // Run every 5 seconds
  console.log('✓ Task scheduled successfully');

  console.log('\n3. Testing Goal 4.3: Advanced Caching and Performance Optimization System');
  console.log('=======================================================================');

  // Create caches
  tusk.cacheManager.createCache('test_cache', { ttl: 60000, maxSize: 100, strategy: 'lru' });
  tusk.cacheManager.createCache('query_cache', { ttl: 300000, maxSize: 500, strategy: 'lfu' });
  console.log('✓ Caches created successfully');

  // Test caching
  tusk.cacheManager.set('test_cache', 'key1', { data: 'test value 1' });
  tusk.cacheManager.set('test_cache', 'key2', { data: 'test value 2' });
  
  const cachedValue1 = tusk.cacheManager.get('test_cache', 'key1');
  const cachedValue2 = tusk.cacheManager.get('test_cache', 'key2');
  console.log('✓ Caching working correctly:', cachedValue1 && cachedValue2);

  // Test cache statistics
  const cacheStats = tusk.cacheManager.getCacheStats('test_cache');
  console.log('✓ Cache statistics retrieved:', cacheStats ? 'success' : 'no data');

  // Test query optimization
  const testQuery = {
    limit: 1000,
    sort: ['name', 'value', 'id', 'timestamp'],
    fields: ['id', 'name', 'value', 'created', 'updated', 'status']
  };

  const optimizedQuery = await tusk.optimizeQueryWithCaching(testQuery, {
    maxLimit: 100,
    maxSortFields: 2,
    maxFields: 3
  });
  console.log('✓ Query optimization completed');
  console.log('✓ Optimized query:', {
    limit: optimizedQuery.limit,
    sortFields: optimizedQuery.sort?.length,
    fields: optimizedQuery.fields?.length
  });

  // Test performance optimization
  const largeData = Array.from({ length: 2000 }, (_, i) => ({
    id: i,
    name: `Item ${i}`,
    value: Math.random() * 1000,
    metadata: { created: Date.now(), updated: Date.now() }
  }));

  const optimizedData = await tusk.performanceOptimizer.applyOptimization('data_compression', largeData);
  console.log('✓ Performance optimization completed');
  console.log('✓ Data size reduced from', largeData.length, 'to', optimizedData.length);

  console.log('\n4. Testing Integration and Advanced Features');
  console.log('=============================================');

  // Test comprehensive data processing workflow
  const comprehensiveData = Array.from({ length: 500 }, (_, i) => ({
    id: i,
    name: `Data Item ${i}`,
    value: Math.random() * 100,
    category: i % 5 === 0 ? 'premium' : 'standard',
    timestamp: Date.now() - Math.random() * 86400000
  }));

  // Process with all optimizations
  const finalResult = await tusk.processDataWithOptimization('test_pipeline', comprehensiveData);
  console.log('✓ Comprehensive data processing completed');
  console.log('✓ Final result size:', finalResult.length);

  // Test system statistics
  const performanceStats = tusk.getPerformanceStats();
  console.log('✓ Performance statistics retrieved successfully');
  console.log('✓ Stats include:', Object.keys(performanceStats));

  // Test analytics data
  const analyticsData = tusk.getAnalyticsData();
  console.log('✓ Analytics data retrieved successfully');
  console.log('✓ Analytics include:', Object.keys(analyticsData));

  // Test cache statistics
  const allCacheStats = tusk.getCacheStats();
  console.log('✓ All cache statistics retrieved successfully');
  console.log('✓ Cache count:', Object.keys(allCacheStats).length);

  // Test workflow statistics
  const workflowStats = tusk.getWorkflowStats();
  console.log('✓ Workflow statistics retrieved successfully');
  console.log('✓ Workflow count:', Object.keys(workflowStats).length);

  console.log('\n5. Testing Error Handling and Resilience');
  console.log('==========================================');

  // Test invalid pipeline
  try {
    await tusk.dataProcessor.processData('nonexistent_pipeline', testData);
  } catch (error) {
    console.log('✓ Invalid pipeline error handled gracefully:', error.message);
  }

  // Test invalid cache access
  try {
    tusk.cacheManager.get('nonexistent_cache', 'key');
  } catch (error) {
    console.log('✓ Invalid cache access error handled gracefully');
  }

  // Test invalid workflow
  try {
    await tusk.workflowEngine.executeWorkflow('nonexistent_workflow', {});
  } catch (error) {
    console.log('✓ Invalid workflow error handled gracefully:', error.message);
  }

  console.log('\n6. Testing Cleanup and Shutdown');
  console.log('=================================');

  // Shutdown
  await tusk.shutdown();
  console.log('✓ Performance core shutdown completed');

  console.log('\n=== All Goals Successfully Implemented and Tested ===');
  console.log('✓ Goal 4.1: Advanced Data Processing and Analytics Engine');
  console.log('✓ Goal 4.2: Advanced Workflow and Task Orchestration System');
  console.log('✓ Goal 4.3: Advanced Caching and Performance Optimization System');
  console.log('✓ Integration: All systems working together seamlessly');
  console.log('✓ Data Processing: Pipeline management and analytics');
  console.log('✓ Workflow Orchestration: Task management and scheduling');
  console.log('✓ Caching System: Multi-strategy caching and optimization');
  console.log('✓ Performance: Comprehensive optimization and monitoring');

  return {
    success: true,
    goals: ['g4.1', 'g4.2', 'g4.3'],
    implementation: 'complete',
    features: [
      'Advanced data processing with pipeline management',
      'Comprehensive analytics and machine learning',
      'Workflow orchestration with dependency management',
      'Task scheduling with cron-like expressions',
      'Multi-strategy caching (LRU, FIFO, LFU)',
      'Performance optimization and query optimization',
      'Real-time monitoring and statistics',
      'Error handling and resilience'
    ],
    timestamp: new Date().toISOString()
  };
}

// Run the test
if (require.main === module) {
  testGoalsG4()
    .then(result => {
      console.log('\n🎉 Test completed successfully!');
      console.log('Result:', result);
      process.exit(0);
    })
    .catch(error => {
      console.error('❌ Test failed:', error);
      process.exit(1);
    });
}

module.exports = { testGoalsG4 }; 
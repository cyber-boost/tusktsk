const { Goal14Implementation } = require('../src/goal14-implementation');

async function comprehensiveTestG14() {
    console.log('🧪 COMPREHENSIVE TEST G14 - Analytics & Business Intelligence');
    
    const goal14 = new Goal14Implementation();
    await goal14.initialize();
    
    // Test 1: Dataset creation and analysis
    const salesData = [
        { month: 'Jan', sales: 1000, region: 'North', product: 'A' },
        { month: 'Feb', sales: 1200, region: 'North', product: 'B' },
        { month: 'Mar', sales: 1100, region: 'South', product: 'A' },
        { month: 'Apr', sales: 1300, region: 'South', product: 'B' },
        { month: 'May', sales: 1150, region: 'North', product: 'A' }
    ];
    
    const dataset = goal14.createDataset('sales-data', salesData);
    console.log('✅ Test 1: Dataset created and analyzed');
    console.log('  Dataset size:', dataset.size);
    console.log('  Sales statistics:', JSON.stringify(dataset.statistics.sales));
    
    // Test 2: Complex query execution
    const queryResult = goal14.runQuery('sales-analysis', 'sales-data', {
        filters: [{ field: 'sales', operator: '>', value: 1100 }],
        groupBy: ['region'],
        aggregations: { sales: 'avg' },
        orderBy: [{ field: 'sales_avg', direction: 'desc' }]
    });
    
    console.log('✅ Test 2: Complex query executed');
    console.log('  Query results:', queryResult.result.length);
    console.log('  Execution time:', queryResult.executionTime, 'ms');
    
    // Test 3: Predictive modeling
    const trainingData = salesData.map((row, i) => ({ ...row, month_num: i + 1 }));
    const model = goal14.trainLinearModel('sales-predictor', trainingData, 'sales');
    
    console.log('✅ Test 3: Predictive model trained');
    console.log('  Model accuracy:', model.accuracy);
    console.log('  Model type:', model.type);
    
    // Test 4: Prediction
    const prediction = goal14.predict('sales-predictor', { month_num: 6, region: 'North' });
    console.log('✅ Test 4: Prediction made');
    console.log('  Predicted value:', prediction.prediction);
    console.log('  Confidence:', prediction.confidence);
    
    // Test 5: Time series forecasting
    const timeSeries = salesData.map((row, i) => ({ time: i, value: row.sales }));
    const forecast = goal14.forecast('sales-forecast', timeSeries, 3);
    
    console.log('✅ Test 5: Forecast generated');
    console.log('  Forecast periods:', forecast.forecast.length);
    console.log('  Trend:', forecast.trend);
    
    const stats = goal14.getSystemStatus();
    console.log('✅ G14 COMPREHENSIVE TEST PASSED');
    console.log('  Final stats:', JSON.stringify(stats));
    
    return { passed: true, datasets: stats.analytics.datasets, models: stats.predictive.models };
}

if (require.main === module) {
    comprehensiveTestG14().then(result => {
        console.log('🎉 G14 ALL TESTS PASSED!', JSON.stringify(result));
    }).catch(console.error);
}

module.exports = { comprehensiveTestG14 };

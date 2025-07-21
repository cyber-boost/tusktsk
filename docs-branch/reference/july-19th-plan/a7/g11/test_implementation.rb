#!/usr/bin/env ruby

require 'test/unit'
require_relative 'goal_implementation'

class TestETLPipeline < Test::Unit::TestCase
  def setup
    @pipeline = ETLPipeline.new
  end

  def test_pipeline_initialization
    assert_not_nil @pipeline.pipeline_id
    assert_equal [], @pipeline.stages
    assert_equal({}, @pipeline.data_sources)
    assert_equal({}, @pipeline.transformations)
    assert_equal({}, @pipeline.validators)
  end

  def test_add_data_source
    @pipeline.add_data_source('test_source', {
      type: 'file',
      path: '/test/path.csv',
      format: 'csv'
    })
    
    assert_equal 1, @pipeline.data_sources.length
    assert_equal 'file', @pipeline.data_sources['test_source'][:type]
    assert_equal '/test/path.csv', @pipeline.data_sources['test_source'][:path]
  end

  def test_add_transformation
    @pipeline.add_transformation('test_transform', {
      type: 'map',
      function: ->(item) { item.merge('processed' => true) }
    })
    
    assert_equal 1, @pipeline.transformations.length
    assert_equal 'map', @pipeline.transformations['test_transform'][:type]
  end

  def test_add_validator
    @pipeline.add_validator('test_validator', {
      type: 'schema',
      rules: { 'id' => { type: 'required' } }
    })
    
    assert_equal 1, @pipeline.validators.length
    assert_equal 'schema', @pipeline.validators['test_validator'][:type]
  end

  def test_add_stage
    @pipeline.add_stage({
      name: 'test_stage',
      data_source: 'test_source',
      transformations: ['test_transform'],
      validators: ['test_validator'],
      output: 'test_output'
    })
    
    assert_equal 1, @pipeline.stages.length
    assert_equal 'test_stage', @pipeline.stages.first[:name]
  end

  def test_transform_data_map
    @pipeline.add_transformation('double_value', {
      type: 'map',
      function: ->(item) { item.merge('value' => item['value'] * 2) }
    })
    
    data = [{'value' => 5}, {'value' => 10}]
    result = @pipeline.transform_data(data, 'double_value')
    
    assert_equal [{'value' => 10}, {'value' => 20}], result
  end

  def test_transform_data_filter
    @pipeline.add_transformation('filter_positive', {
      type: 'filter',
      function: ->(item) { item['value'] > 0 }
    })
    
    data = [{'value' => 5}, {'value' => -3}, {'value' => 10}]
    result = @pipeline.transform_data(data, 'filter_positive')
    
    assert_equal [{'value' => 5}, {'value' => 10}], result
  end

  def test_validate_data_schema
    @pipeline.add_validator('test_schema', {
      type: 'schema',
      rules: {
        'id' => { type: 'required' },
        'value' => { type: 'range', min: 0, max: 100 }
      }
    })
    
    data = [
      {'id' => 1, 'value' => 50},
      {'id' => 2, 'value' => 150}, # Invalid
      {'value' => 25} # Missing id
    ]
    
    result = @pipeline.validate_data(data, 'test_schema')
    
    assert_equal 1, result[:data].length
    assert_equal 2, result[:errors].length
    assert_equal [{'id' => 1, 'value' => 50}], result[:data]
  end

  def test_get_pipeline_stats
    @pipeline.add_data_source('source1', { type: 'file', path: '/test1.csv' })
    @pipeline.add_transformation('transform1', { type: 'map', function: ->(x) { x } })
    @pipeline.add_validator('validator1', { type: 'schema', rules: {} })
    @pipeline.add_stage({ name: 'stage1', data_source: 'source1' })
    
    stats = @pipeline.get_pipeline_stats
    
    assert_not_nil stats[:pipeline_id]
    assert_equal 1, stats[:stages_count]
    assert_equal 1, stats[:data_sources_count]
    assert_equal 1, stats[:transformations_count]
    assert_equal 1, stats[:validators_count]
  end
end

class TestStreamingAnalytics < Test::Unit::TestCase
  def setup
    @streaming = StreamingAnalytics.new
  end

  def test_streaming_initialization
    assert_not_nil @streaming.stream_id
    assert_equal({}, @streaming.processors)
    assert_equal({}, @streaming.windows)
    assert_equal({}, @streaming.aggregations)
  end

  def test_add_processor
    @streaming.add_processor('test_processor', {
      type: 'filter',
      function: ->(item) { item[:data]['value'] > 0 }
    })
    
    assert_equal 1, @streaming.processors.length
    assert_equal 'filter', @streaming.processors['test_processor'][:type]
  end

  def test_add_window
    @streaming.add_window('test_window', {
      type: 'time',
      size: 60,
      slide: 10
    })
    
    assert_equal 1, @streaming.windows.length
    assert_equal 'time', @streaming.windows['test_window'][:type]
    assert_equal 60, @streaming.windows['test_window'][:size]
  end

  def test_add_aggregation
    @streaming.add_aggregation('test_aggregation', {
      type: 'sum',
      field: 'value',
      window: 'test_window'
    })
    
    assert_equal 1, @streaming.aggregations.length
    assert_equal 'sum', @streaming.aggregations['test_aggregation'][:type]
  end

  def test_ingest_data
    @streaming.ingest_data({'value' => 100, 'timestamp' => '2023-01-01'})
    
    # Data should be in buffer
    stats = @streaming.get_stream_stats
    assert_equal 1, stats[:buffer_size]
  end

  def test_process_stream
    @streaming.add_processor('filter_positive', {
      type: 'filter',
      function: ->(item) { item[:data]['value'] > 0 }
    })
    
    @streaming.ingest_data({'value' => 100})
    @streaming.ingest_data({'value' => -50})
    @streaming.ingest_data({'value' => 200})
    
    result = @streaming.process_stream
    
    assert_equal 2, result.length # Only positive values
    assert_equal 100, result[0][:data]['value']
    assert_equal 200, result[1][:data]['value']
  end

  def test_get_window_data_time
    @streaming.add_window('hourly', { type: 'time', size: 3600 })
    
    # Add data with different timestamps
    @streaming.ingest_data({'value' => 100})
    sleep(0.1) # Small delay to ensure different timestamps
    
    window_data = @streaming.get_window_data('hourly')
    assert_equal 1, window_data.length
  end

  def test_calculate_aggregation
    @streaming.add_window('test_window', { type: 'count', size: 10 })
    @streaming.add_aggregation('sum_values', {
      type: 'sum',
      field: 'value',
      window: 'test_window'
    })
    
    @streaming.ingest_data({'value' => 100})
    @streaming.ingest_data({'value' => 200})
    @streaming.ingest_data({'value' => 300})
    
    result = @streaming.calculate_aggregation('sum_values')
    assert_equal 600, result
  end

  def test_calculate_aggregation_average
    @streaming.add_window('test_window', { type: 'count', size: 10 })
    @streaming.add_aggregation('avg_values', {
      type: 'average',
      field: 'value',
      window: 'test_window'
    })
    
    @streaming.ingest_data({'value' => 100})
    @streaming.ingest_data({'value' => 200})
    @streaming.ingest_data({'value' => 300})
    
    result = @streaming.calculate_aggregation('avg_values')
    assert_equal 200.0, result
  end

  def test_get_stream_stats
    @streaming.add_processor('proc1', { type: 'filter', function: ->(x) { x } })
    @streaming.add_window('win1', { type: 'time', size: 60 })
    @streaming.add_aggregation('agg1', { type: 'sum', field: 'value', window: 'win1' })
    @streaming.ingest_data({'value' => 100})
    
    stats = @streaming.get_stream_stats
    
    assert_not_nil stats[:stream_id]
    assert_equal 1, stats[:processors_count]
    assert_equal 1, stats[:windows_count]
    assert_equal 1, stats[:aggregations_count]
    assert_equal 1, stats[:buffer_size]
  end
end

class TestMLFramework < Test::Unit::TestCase
  def setup
    @ml_framework = MLFramework.new
  end

  def test_ml_framework_initialization
    assert_not_nil @ml_framework.framework_id
    assert_equal({}, @ml_framework.models)
    assert_equal({}, @ml_framework.datasets)
    assert_equal({}, @ml_framework.features)
  end

  def test_add_dataset
    @ml_framework.add_dataset('test_dataset', {
      data: [{'features' => [1, 2], 'target' => 10}],
      features: ['feature1', 'feature2'],
      target: 'target'
    })
    
    assert_equal 1, @ml_framework.datasets.length
    assert_equal 'test_dataset', @ml_framework.datasets.keys.first
  end

  def test_add_feature_engineering
    @ml_framework.add_feature_engineering('test_feature', {
      type: 'transform',
      function: ->(item) { item.merge('normalized' => item['value'].to_f / 100) }
    })
    
    assert_equal 1, @ml_framework.features.length
    assert_equal 'transform', @ml_framework.features['test_feature'][:type]
  end

  def test_add_model
    @ml_framework.add_model('test_model', {
      type: 'linear',
      algorithm: 'regression'
    })
    
    assert_equal 1, @ml_framework.models.length
    assert_equal 'linear', @ml_framework.models['test_model'][:type]
    assert_equal false, @ml_framework.models['test_model'][:trained]
  end

  def test_preprocess_data
    @ml_framework.add_dataset('test_dataset', {
      data: [
        {'features' => [1, 2], 'target' => 10},
        {'features' => [2, 3], 'target' => 20},
        {'features' => [3, 4], 'target' => 30},
        {'features' => [4, 5], 'target' => 40},
        {'features' => [5, 6], 'target' => 50}
      ],
      features: ['feature1', 'feature2'],
      target: 'target',
      split_ratio: 0.8
    })
    
    result = @ml_framework.preprocess_data('test_dataset')
    
    assert_not_nil result
    assert_equal 4, result[:train].length # 80% of 5
    assert_equal 1, result[:test].length  # 20% of 5
    assert_equal ['feature1', 'feature2'], result[:features]
    assert_equal 'target', result[:target]
  end

  def test_train_linear_model
    @ml_framework.add_dataset('test_dataset', {
      data: [
        {'features' => [1], 'target' => 10},
        {'features' => [2], 'target' => 20},
        {'features' => [3], 'target' => 30}
      ],
      features: ['feature1'],
      target: 'target'
    })
    
    @ml_framework.add_model('linear_model', {
      type: 'linear',
      algorithm: 'regression'
    })
    
    success = @ml_framework.train_model('linear_model', 'test_dataset')
    assert_equal true, success
    assert_equal true, @ml_framework.models['linear_model'][:trained]
    assert_not_nil @ml_framework.models['linear_model'][:coefficients]
  end

  def test_predict_linear_model
    @ml_framework.add_dataset('test_dataset', {
      data: [
        {'features' => [1], 'target' => 10},
        {'features' => [2], 'target' => 20},
        {'features' => [3], 'target' => 30}
      ],
      features: ['feature1'],
      target: 'target'
    })
    
    @ml_framework.add_model('linear_model', {
      type: 'linear',
      algorithm: 'regression'
    })
    
    @ml_framework.train_model('linear_model', 'test_dataset')
    
    prediction = @ml_framework.predict('linear_model', {'features' => [4]})
    assert_not_nil prediction
    assert prediction.is_a?(Numeric)
  end

  def test_train_decision_tree
    @ml_framework.add_dataset('test_dataset', {
      data: [
        {'feature1' => 1, 'feature2' => 10, 'target' => 'A'},
        {'feature1' => 2, 'feature2' => 20, 'target' => 'B'},
        {'feature1' => 1, 'feature2' => 15, 'target' => 'A'}
      ],
      features: ['feature1', 'feature2'],
      target: 'target'
    })
    
    @ml_framework.add_model('tree_model', {
      type: 'decision_tree',
      algorithm: 'classification'
    })
    
    success = @ml_framework.train_model('tree_model', 'test_dataset')
    assert_equal true, success
    assert_equal true, @ml_framework.models['tree_model'][:trained]
    assert_not_nil @ml_framework.models['tree_model'][:tree]
  end

  def test_predict_decision_tree
    @ml_framework.add_dataset('test_dataset', {
      data: [
        {'feature1' => 1, 'feature2' => 10, 'target' => 'A'},
        {'feature1' => 2, 'feature2' => 20, 'target' => 'B'},
        {'feature1' => 1, 'feature2' => 15, 'target' => 'A'}
      ],
      features: ['feature1', 'feature2'],
      target: 'target'
    })
    
    @ml_framework.add_model('tree_model', {
      type: 'decision_tree',
      algorithm: 'classification'
    })
    
    @ml_framework.train_model('tree_model', 'test_dataset')
    
    prediction = @ml_framework.predict('tree_model', {'feature1' => 1, 'feature2' => 12})
    assert_not_nil prediction
    assert ['A', 'B'].include?(prediction)
  end

  def test_evaluate_model
    @ml_framework.add_dataset('test_dataset', {
      data: [
        {'features' => [1], 'target' => 10},
        {'features' => [2], 'target' => 20},
        {'features' => [3], 'target' => 30},
        {'features' => [4], 'target' => 40},
        {'features' => [5], 'target' => 50}
      ],
      features: ['feature1'],
      target: 'target'
    })
    
    @ml_framework.add_model('linear_model', {
      type: 'linear',
      algorithm: 'regression'
    })
    
    @ml_framework.train_model('linear_model', 'test_dataset')
    
    metrics = @ml_framework.evaluate_model('linear_model', 'test_dataset')
    
    assert_not_nil metrics
    assert metrics[:mse].is_a?(Numeric)
    assert metrics[:mae].is_a?(Numeric)
    assert metrics[:rmse].is_a?(Numeric)
    assert metrics[:r_squared].is_a?(Numeric)
  end

  def test_get_framework_stats
    @ml_framework.add_dataset('dataset1', { data: [], features: [], target: 'target' })
    @ml_framework.add_feature_engineering('feature1', { type: 'transform', function: ->(x) { x } })
    @ml_framework.add_model('model1', { type: 'linear', algorithm: 'regression' })
    
    stats = @ml_framework.get_framework_stats
    
    assert_not_nil stats[:framework_id]
    assert_equal 1, stats[:models_count]
    assert_equal 1, stats[:datasets_count]
    assert_equal 1, stats[:features_count]
    assert_equal 0, stats[:trained_models]
  end
end

class TestAnalyticsFramework < Test::Unit::TestCase
  def setup
    @framework = AnalyticsFramework.new
  end

  def test_framework_initialization
    assert_not_nil @framework.framework_id
    assert_not_nil @framework.etl_pipeline
    assert_not_nil @framework.streaming_analytics
    assert_not_nil @framework.ml_framework
  end

  def test_create_data_pipeline
    etl_config = {
      data_sources: {
        'test_source' => { type: 'file', path: '/test.csv', format: 'csv' }
      },
      transformations: {
        'test_transform' => { type: 'map', function: ->(x) { x } }
      },
      validators: {
        'test_validator' => { type: 'schema', rules: {} }
      },
      stages: [
        { name: 'test_stage', data_source: 'test_source' }
      ]
    }
    
    @framework.create_data_pipeline(etl_config)
    
    assert_equal 1, @framework.etl_pipeline.data_sources.length
    assert_equal 1, @framework.etl_pipeline.transformations.length
    assert_equal 1, @framework.etl_pipeline.validators.length
    assert_equal 1, @framework.etl_pipeline.stages.length
  end

  def test_create_streaming_pipeline
    streaming_config = {
      processors: {
        'test_processor' => { type: 'filter', function: ->(x) { x } }
      },
      windows: {
        'test_window' => { type: 'time', size: 60 }
      },
      aggregations: {
        'test_aggregation' => { type: 'sum', field: 'value', window: 'test_window' }
      }
    }
    
    @framework.create_streaming_pipeline(streaming_config)
    
    assert_equal 1, @framework.streaming_analytics.processors.length
    assert_equal 1, @framework.streaming_analytics.windows.length
    assert_equal 1, @framework.streaming_analytics.aggregations.length
  end

  def test_create_ml_pipeline
    ml_config = {
      datasets: {
        'test_dataset' => { data: [], features: [], target: 'target' }
      },
      features: {
        'test_feature' => { type: 'transform', function: ->(x) { x } }
      },
      models: {
        'test_model' => { type: 'linear', algorithm: 'regression' }
      }
    }
    
    @framework.create_ml_pipeline(ml_config)
    
    assert_equal 1, @framework.ml_framework.datasets.length
    assert_equal 1, @framework.ml_framework.features.length
    assert_equal 1, @framework.ml_framework.models.length
  end

  def test_execute_full_pipeline
    # Create minimal pipeline configurations
    etl_config = {
      data_sources: {
        'test_source' => { type: 'file', path: '/test.csv', format: 'csv' }
      },
      stages: [
        { name: 'test_stage', data_source: 'test_source' }
      ]
    }
    
    streaming_config = {
      processors: {
        'test_processor' => { type: 'filter', function: ->(x) { x } }
      }
    }
    
    ml_config = {
      datasets: {
        'test_dataset' => {
          data: [{'features' => [1], 'target' => 10}],
          features: ['feature1'],
          target: 'target'
        }
      },
      models: {
        'test_model' => { type: 'linear', algorithm: 'regression' }
      }
    }
    
    @framework.create_data_pipeline(etl_config)
    @framework.create_streaming_pipeline(streaming_config)
    @framework.create_ml_pipeline(ml_config)
    
    results = @framework.execute_full_pipeline
    
    assert_not_nil results
    assert results[:etl_results].is_a?(Array)
    assert results[:stream_results].is_a?(Array)
    assert results[:ml_results].is_a?(Hash)
    assert_not_nil results[:timestamp]
  end

  def test_get_comprehensive_stats
    stats = @framework.get_comprehensive_stats
    
    assert_not_nil stats[:framework_id]
    assert_not_nil stats[:etl_stats]
    assert_not_nil stats[:streaming_stats]
    assert_not_nil stats[:ml_stats]
    assert_not_nil stats[:created_at]
  end
end

# Performance and Integration Tests
class TestPerformanceAndIntegration < Test::Unit::TestCase
  def test_large_dataset_processing
    framework = AnalyticsFramework.new
    
    # Create large dataset
    large_data = (1..1000).map do |i|
      {
        'id' => i,
        'value' => rand(1000),
        'category' => ['A', 'B', 'C'].sample,
        'timestamp' => Time.now - rand(86400)
      }
    end
    
    etl_config = {
      data_sources: {
        'large_source' => { type: 'file', path: '/large.csv', format: 'csv' }
      },
      transformations: {
        'filter_high_value' => {
          type: 'filter',
          function: ->(item) { item['value'] > 500 }
        },
        'add_processed_flag' => {
          type: 'map',
          function: ->(item) { item.merge('processed' => true) }
        }
      },
      stages: [
        {
          name: 'large_processing',
          data_source: 'large_source',
          transformations: ['filter_high_value', 'add_processed_flag']
        }
      ]
    }
    
    framework.create_data_pipeline(etl_config)
    
    # Simulate processing large dataset
    start_time = Time.now
    results = framework.execute_full_pipeline
    end_time = Time.now
    
    processing_time = end_time - start_time
    
    assert processing_time < 5.0, "Processing took too long: #{processing_time} seconds"
    assert_not_nil results
  end

  def test_streaming_performance
    streaming = StreamingAnalytics.new
    
    # Add multiple processors and aggregations
    10.times do |i|
      streaming.add_processor("processor_#{i}", {
        type: 'filter',
        function: ->(item) { item[:data]['value'] > i * 10 }
      })
    end
    
    streaming.add_window('minute', { type: 'time', size: 60 })
    streaming.add_aggregation('sum_values', {
      type: 'sum',
      field: 'value',
      window: 'minute'
    })
    
    # Ingest large amount of data
    start_time = Time.now
    1000.times do |i|
      streaming.ingest_data({'value' => rand(1000), 'id' => i})
    end
    
    result = streaming.process_stream
    end_time = Time.now
    
    processing_time = end_time - start_time
    
    assert processing_time < 2.0, "Streaming processing took too long: #{processing_time} seconds"
    assert result.is_a?(Array)
  end

  def test_ml_model_training_performance
    ml_framework = MLFramework.new
    
    # Create large training dataset
    training_data = (1..500).map do |i|
      {
        'features' => [i, i * 2, i * 3],
        'target' => i * 10 + rand(10)
      }
    end
    
    ml_framework.add_dataset('large_dataset', {
      data: training_data,
      features: ['feature1', 'feature2', 'feature3'],
      target: 'target'
    })
    
    ml_framework.add_model('linear_model', {
      type: 'linear',
      algorithm: 'regression'
    })
    
    # Test training performance
    start_time = Time.now
    success = ml_framework.train_model('linear_model', 'large_dataset')
    end_time = Time.now
    
    training_time = end_time - start_time
    
    assert_equal true, success
    assert training_time < 3.0, "Model training took too long: #{training_time} seconds"
    
    # Test prediction performance
    start_time = Time.now
    100.times do
      ml_framework.predict('linear_model', {'features' => [rand(100), rand(100), rand(100)]})
    end
    end_time = Time.now
    
    prediction_time = end_time - start_time
    
    assert prediction_time < 1.0, "Prediction took too long: #{prediction_time} seconds"
  end

  def test_end_to_end_integration
    framework = AnalyticsFramework.new
    
    # Create comprehensive pipeline
    etl_config = {
      data_sources: {
        'sales_data' => { type: 'file', path: '/sales.csv', format: 'csv' }
      },
      transformations: {
        'clean_data' => {
          type: 'filter',
          function: ->(item) { item['amount'] && item['amount'].to_f > 0 }
        },
        'calculate_total' => {
          type: 'map',
          function: ->(item) { item.merge('total' => item['amount'].to_f * item['quantity'].to_f) }
        }
      },
      stages: [
        {
          name: 'sales_processing',
          data_source: 'sales_data',
          transformations: ['clean_data', 'calculate_total']
        }
      ]
    }
    
    streaming_config = {
      processors: {
        'filter_high_value' => {
          type: 'filter',
          function: ->(item) { item[:data]['total'].to_f > 1000 }
        }
      },
      windows: {
        'hourly' => { type: 'time', size: 3600 }
      },
      aggregations: {
        'hourly_sales' => {
          type: 'sum',
          field: 'total',
          window: 'hourly'
        }
      }
    }
    
    ml_config = {
      datasets: {
        'sales_prediction' => {
          data: (1..100).map { |i| {'features' => [i, i * 2], 'target' => i * 10} },
          features: ['day', 'base_amount'],
          target: 'sales'
        }
      },
      models: {
        'sales_forecast' => {
          type: 'linear',
          algorithm: 'regression'
        }
      }
    }
    
    # Create all pipelines
    framework.create_data_pipeline(etl_config)
    framework.create_streaming_pipeline(streaming_config)
    framework.create_ml_pipeline(ml_config)
    
    # Execute full pipeline
    start_time = Time.now
    results = framework.execute_full_pipeline
    end_time = Time.now
    
    execution_time = end_time - start_time
    
    # Verify results
    assert_not_nil results
    assert results[:etl_results].is_a?(Array)
    assert results[:stream_results].is_a?(Array)
    assert results[:ml_results].is_a?(Hash)
    assert execution_time < 10.0, "End-to-end execution took too long: #{execution_time} seconds"
    
    # Verify comprehensive stats
    stats = framework.get_comprehensive_stats
    assert_not_nil stats[:framework_id]
    assert_equal 1, stats[:etl_stats][:stages_count]
    assert_equal 1, stats[:streaming_stats][:processors_count]
    assert_equal 1, stats[:ml_stats][:models_count]
  end
end

# Run all tests
if __FILE__ == $0
  puts "Running Goal 11 Tests..."
  puts "=" * 50
  
  # Run Test::Unit tests
  Test::Unit::AutoRunner.run
end 
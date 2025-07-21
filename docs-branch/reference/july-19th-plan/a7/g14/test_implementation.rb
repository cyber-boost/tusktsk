#!/usr/bin/env ruby

require 'test/unit'
require_relative 'goal_implementation'

class TestAdvancedCacheFramework < Test::Unit::TestCase
  def setup
    @cache = AdvancedCacheFramework.new(max_size: 10, default_ttl: 1)
  end

  def test_cache_set_get
    @cache.set('key1', 'value1')
    assert_equal 'value1', @cache.get('key1')
  end

  def test_cache_ttl
    @cache.set('key1', 'value1', 0.1)
    sleep(0.2)
    assert_nil @cache.get('key1')
  end

  def test_memoize
    counter = 0
    result = @cache.memoize('test') { counter += 1; 'result' }
    assert_equal 'result', result
    
    result2 = @cache.memoize('test') { counter += 1; 'result' }
    assert_equal 'result', result2
    assert_equal 1, counter # Should only execute once
  end

  def test_stats
    @cache.set('key1', 'value1')
    @cache.get('key1')
    @cache.get('nonexistent')
    
    stats = @cache.stats
    assert_equal 1, stats[:hits]
    assert_equal 1, stats[:misses]
  end
end

class TestPerformanceOptimizer < Test::Unit::TestCase
  def setup
    @optimizer = PerformanceOptimizer.new
  end

  def test_profile
    result = @optimizer.profile('test_op') { sleep(0.01); 'done' }
    assert_equal 'done', result
    
    report = @optimizer.get_performance_report
    assert report[:profiles].key?('test_op')
  end

  def test_detect_bottlenecks
    @optimizer.profile('slow_op') { sleep(0.2) }
    @optimizer.profile('fast_op') { 'quick' }
    
    bottlenecks = @optimizer.detect_bottlenecks
    assert_equal 1, bottlenecks.length
    assert_equal 'slow_op', bottlenecks.first[:operation]
  end

  def test_auto_tune
    best_config = @optimizer.auto_tune('test_tune') do |config|
      sleep(0.001 * config[:threads])
    end
    
    assert best_config.key?(:threads)
    assert best_config.key?(:batch_size)
  end
end

class TestCodeOptimizer < Test::Unit::TestCase
  def setup
    @optimizer = CodeOptimizer.new
  end

  def test_optimize_method
    optimized = @optimizer.optimize_method('test_method') { |x| x * 2 }
    result = optimized.call(5)
    assert_equal 10, result
    
    stats = @optimizer.get_optimization_stats
    assert_equal 1, stats[:optimized_methods]
  end

  def test_compile_to_bytecode
    bytecode = @optimizer.compile_to_bytecode('puts "hello"')
    assert bytecode.start_with?('BYTECODE_')
  end
end

class TestUnifiedPerformanceFramework < Test::Unit::TestCase
  def setup
    @framework = UnifiedPerformanceFramework.new
  end

  def test_optimize_operation
    result1 = @framework.optimize_operation('test') { 'computed' }
    result2 = @framework.optimize_operation('test') { 'computed' }
    
    assert_equal 'computed', result1
    assert_equal 'computed', result2
  end

  def test_comprehensive_optimization
    result = @framework.comprehensive_optimization
    assert result[:memory_optimized]
    assert result[:cache_cleaned]
  end

  def test_comprehensive_stats
    stats = @framework.get_comprehensive_stats
    assert stats.key?(:cache_stats)
    assert stats.key?(:performance_report)
    assert stats.key?(:optimization_stats)
  end
end

if __FILE__ == $0
  Test::Unit::AutoRunner.run
end 
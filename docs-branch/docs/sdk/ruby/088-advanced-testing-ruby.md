# 🧪 Advanced Testing with TuskLang Ruby

**"We don't bow to any king" - Ruby Edition**

Build sophisticated testing systems with TuskLang's advanced testing features. From property-based testing to performance testing, TuskLang provides the flexibility and power you need to ensure your Ruby applications are robust and reliable.

## 🚀 Quick Start

### Basic Testing Setup
```ruby
require 'tusklang'
require 'tusklang/testing'

# Initialize testing system
testing_system = TuskLang::Testing::System.new

# Configure testing
testing_system.configure do |config|
  config.default_framework = 'rspec'
  config.parallel_enabled = true
  config.coverage_enabled = true
  config.performance_enabled = true
end

# Register testing strategies
testing_system.register_strategy(:unit, TuskLang::Testing::Strategies::UnitTestingStrategy.new)
testing_system.register_strategy(:integration, TuskLang::Testing::Strategies::IntegrationTestingStrategy.new)
testing_system.register_strategy(:performance, TuskLang::Testing::Strategies::PerformanceTestingStrategy.new)
```

### TuskLang Configuration
```tsk
# config/testing.tsk
[testing]
enabled: true
default_framework: "rspec"
parallel_enabled: true
coverage_enabled: true
performance_enabled: true

[testing.frameworks]
rspec: {
    enabled: true,
    format: "documentation",
    color: true,
    require: ["spec_helper"]
}
minitest: {
    enabled: false,
    format: "pride",
    verbose: true
}
cucumber: {
    enabled: false,
    format: "pretty",
    tags: ["~@wip"]
}

[testing.coverage]
enabled: true
threshold: 80
formats: ["html", "json", "text"]
exclude: ["spec/**/*", "config/**/*", "vendor/**/*"]

[testing.performance]
enabled: true
thresholds: {
    response_time: "500ms",
    throughput: "1000rps",
    memory_usage: "512MB"
}
load_testing: {
    enabled: true,
    users: 100,
    duration: "5m",
    ramp_up: "1m"
}

[testing.databases]
test: {
    adapter: "sqlite3",
    database: "test.sqlite3",
    pool: 5
}
factory_bot: {
    enabled: true,
    factories_path: "spec/factories"
}
```

## 🎯 Core Features

### 1. Unit Testing Strategy
```ruby
require 'tusklang/testing'
require 'rspec'

class UnitTestingStrategy
  include TuskLang::Testing::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/testing.tsk')
    @coverage = setup_coverage
    @mocks = {}
  end
  
  def run_tests(test_files = nil)
    test_files ||= find_test_files
    
    results = {
      total: 0,
      passed: 0,
      failed: 0,
      skipped: 0,
      duration: 0,
      coverage: nil
    }
    
    start_time = Time.now
    
    if @config['testing']['parallel_enabled']
      results = run_parallel_tests(test_files)
    else
      results = run_sequential_tests(test_files)
    end
    
    results[:duration] = Time.now - start_time
    
    if @config['testing']['coverage']['enabled']
      results[:coverage] = generate_coverage_report
    end
    
    results
  end
  
  def mock(klass, method, return_value = nil, &block)
    mock_key = "#{klass}##{method}"
    
    if block_given?
      @mocks[mock_key] = block
    else
      @mocks[mock_key] = ->(*args) { return_value }
    end
  end
  
  def stub(klass, method, return_value = nil, &block)
    stub_key = "#{klass}##{method}"
    
    if block_given?
      @mocks[stub_key] = block
    else
      @mocks[stub_key] = ->(*args) { return_value }
    end
  end
  
  def assert_equal(expected, actual, message = nil)
    unless expected == actual
      raise AssertionError, message || "Expected #{expected}, got #{actual}"
    end
  end
  
  def assert_raises(exception_class, message = nil, &block)
    begin
      yield
      raise AssertionError, message || "Expected #{exception_class} to be raised"
    rescue exception_class
      # Expected exception raised
    end
  end
  
  private
  
  def find_test_files
    case @config['testing']['default_framework']
    when 'rspec'
      Dir.glob('spec/**/*_spec.rb')
    when 'minitest'
      Dir.glob('test/**/*_test.rb')
    else
      []
    end
  end
  
  def run_parallel_tests(test_files)
    require 'parallel'
    
    results = Parallel.map(test_files, in_threads: 4) do |file|
      run_single_test_file(file)
    end
    
    aggregate_results(results)
  end
  
  def run_sequential_tests(test_files)
    results = test_files.map do |file|
      run_single_test_file(file)
    end
    
    aggregate_results(results)
  end
  
  def run_single_test_file(file)
    case @config['testing']['default_framework']
    when 'rspec'
      run_rspec_file(file)
    when 'minitest'
      run_minitest_file(file)
    else
      { total: 0, passed: 0, failed: 0, skipped: 0 }
    end
  end
  
  def run_rspec_file(file)
    # Run RSpec file and capture results
    require 'rspec/core'
    
    config = RSpec.configuration
    world = RSpec.world
    
    # Configure RSpec
    config.color = @config['testing']['frameworks']['rspec']['color']
    config.format = @config['testing']['frameworks']['rspec']['format']
    
    # Run the file
    result = RSpec::Core::Runner.run([file])
    
    {
      total: world.example_count,
      passed: world.example_count - world.failed_example_count,
      failed: world.failed_example_count,
      skipped: world.pending_example_count
    }
  end
  
  def run_minitest_file(file)
    # Run Minitest file and capture results
    require 'minitest/autorun'
    
    # Load and run the test file
    load file
    
    # Capture results (this is simplified)
    {
      total: 0,
      passed: 0,
      failed: 0,
      skipped: 0
    }
  end
  
  def aggregate_results(results)
    {
      total: results.sum { |r| r[:total] },
      passed: results.sum { |r| r[:passed] },
      failed: results.sum { |r| r[:failed] },
      skipped: results.sum { |r| r[:skipped] }
    }
  end
  
  def setup_coverage
    return nil unless @config['testing']['coverage']['enabled']
    
    require 'simplecov'
    
    SimpleCov.start do
      coverage_dir 'coverage'
      add_filter '/spec/'
      add_filter '/config/'
      add_filter '/vendor/'
      
      @config['testing']['coverage']['formats'].each do |format|
        send(format)
      end
    end
  end
  
  def generate_coverage_report
    return nil unless @coverage
    
    coverage_data = SimpleCov.result
    coverage_percentage = coverage_data.covered_percent
    
    {
      percentage: coverage_percentage,
      covered_lines: coverage_data.covered_lines,
      total_lines: coverage_data.total_lines,
      files: coverage_data.files.map do |file|
        {
          name: file.filename,
          percentage: file.covered_percent,
          covered_lines: file.covered_lines,
          total_lines: file.total_lines
        }
      end
    }
  end
end
```

### 2. Integration Testing Strategy
```ruby
require 'tusklang/testing'
require 'capybara'

class IntegrationTestingStrategy
  include TuskLang::Testing::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/testing.tsk')
    setup_capybara
    setup_database
  end
  
  def run_integration_tests(test_files = nil)
    test_files ||= find_integration_test_files
    
    results = {
      total: 0,
      passed: 0,
      failed: 0,
      duration: 0
    }
    
    start_time = Time.now
    
    test_files.each do |file|
      result = run_single_integration_test(file)
      results[:total] += result[:total]
      results[:passed] += result[:passed]
      results[:failed] += result[:failed]
    end
    
    results[:duration] = Time.now - start_time
    results
  end
  
  def visit(path)
    Capybara.current_session.visit(path)
  end
  
  def click_on(text)
    Capybara.current_session.click_on(text)
  end
  
  def fill_in(field, with:)
    Capybara.current_session.fill_in(field, with: with)
  end
  
  def assert_text(text)
    expect(Capybara.current_session).to have_text(text)
  end
  
  def assert_current_path(path)
    expect(Capybara.current_session.current_path).to eq(path)
  end
  
  def create_test_data(data)
    case data
    when Hash
      create_test_record(data)
    when Array
      data.each { |record| create_test_record(record) }
    end
  end
  
  def cleanup_test_data
    # Clean up test data after each test
    DatabaseCleaner.clean_with(:truncation)
  end
  
  private
  
  def setup_capybara
    Capybara.configure do |config|
      config.default_driver = :selenium_chrome_headless
      config.default_max_wait_time = 5
      config.app_host = 'http://localhost:3000'
    end
  end
  
  def setup_database
    require 'database_cleaner'
    
    DatabaseCleaner.configure do |config|
      config.strategy = :truncation
      config.clean_with(:truncation)
    end
  end
  
  def find_integration_test_files
    Dir.glob('spec/integration/**/*_spec.rb')
  end
  
  def run_single_integration_test(file)
    # Run integration test file
    require 'rspec/core'
    
    config = RSpec.configuration
    world = RSpec.world
    
    # Configure for integration tests
    config.include Capybara::DSL, type: :feature
    
    # Run the file
    result = RSpec::Core::Runner.run([file])
    
    {
      total: world.example_count,
      passed: world.example_count - world.failed_example_count,
      failed: world.failed_example_count
    }
  end
  
  def create_test_record(data)
    model_class = data[:model] || data[:class]
    attributes = data.except(:model, :class)
    
    klass = model_class.to_s.constantize
    klass.create!(attributes)
  end
end
```

### 3. Performance Testing Strategy
```ruby
require 'tusklang/testing'
require 'benchmark'

class PerformanceTestingStrategy
  include TuskLang::Testing::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/testing.tsk')
    @thresholds = @config['testing']['performance']['thresholds']
    @load_config = @config['testing']['performance']['load_testing']
  end
  
  def run_performance_tests(test_files = nil)
    test_files ||= find_performance_test_files
    
    results = {
      total: 0,
      passed: 0,
      failed: 0,
      performance_data: {}
    }
    
    test_files.each do |file|
      result = run_single_performance_test(file)
      results[:total] += result[:total]
      results[:passed] += result[:passed]
      results[:failed] += result[:failed]
      results[:performance_data].merge!(result[:performance_data])
    end
    
    results
  end
  
  def benchmark(operation_name, iterations: 1000, &block)
    times = []
    
    iterations.times do
      start_time = Time.now
      block.call
      times << (Time.now - start_time) * 1000 # Convert to milliseconds
    end
    
    stats = calculate_stats(times)
    
    # Check against thresholds
    threshold = @thresholds[operation_name]
    if threshold && stats[:avg] > parse_duration(threshold)
      raise PerformanceThresholdExceeded, "#{operation_name} exceeded threshold: #{stats[:avg]}ms > #{threshold}"
    end
    
    stats
  end
  
  def load_test(endpoint, users: nil, duration: nil, ramp_up: nil)
    users ||= @load_config['users']
    duration ||= parse_duration(@load_config['duration'])
    ramp_up ||= parse_duration(@load_config['ramp_up'])
    
    results = {
      total_requests: 0,
      successful_requests: 0,
      failed_requests: 0,
      response_times: [],
      throughput: 0
    }
    
    start_time = Time.now
    
    # Simulate load testing
    threads = []
    users.times do |i|
      threads << Thread.new do
        sleep(i * ramp_up / users.to_f) # Ramp up
        
        while Time.now - start_time < duration
          request_start = Time.now
          
          begin
            response = make_request(endpoint)
            results[:successful_requests] += 1
          rescue => e
            results[:failed_requests] += 1
          end
          
          results[:response_times] << (Time.now - request_start) * 1000
          results[:total_requests] += 1
        end
      end
    end
    
    threads.each(&:join)
    
    # Calculate throughput
    actual_duration = Time.now - start_time
    results[:throughput] = results[:total_requests] / actual_duration
    
    # Check thresholds
    check_load_test_thresholds(results)
    
    results
  end
  
  def memory_test(&block)
    memory_before = get_memory_usage
    
    block.call
    
    memory_after = get_memory_usage
    memory_increase = memory_after - memory_before
    
    threshold = parse_memory(@thresholds['memory_usage'])
    
    if memory_increase > threshold
      raise MemoryThresholdExceeded, "Memory increase #{memory_increase}MB exceeded threshold #{threshold}MB"
    end
    
    {
      memory_before: memory_before,
      memory_after: memory_after,
      memory_increase: memory_increase
    }
  end
  
  private
  
  def find_performance_test_files
    Dir.glob('spec/performance/**/*_spec.rb')
  end
  
  def run_single_performance_test(file)
    # Run performance test file
    require 'rspec/core'
    
    config = RSpec.configuration
    world = RSpec.world
    
    # Configure for performance tests
    config.include self, type: :performance
    
    # Run the file
    result = RSpec::Core::Runner.run([file])
    
    {
      total: world.example_count,
      passed: world.example_count - world.failed_example_count,
      failed: world.failed_example_count,
      performance_data: {} # This would be populated with actual performance data
    }
  end
  
  def calculate_stats(times)
    sorted_times = times.sort
    
    {
      min: sorted_times.first,
      max: sorted_times.last,
      avg: times.sum / times.length,
      median: sorted_times[times.length / 2],
      p95: sorted_times[(times.length * 0.95).round],
      p99: sorted_times[(times.length * 0.99).round]
    }
  end
  
  def make_request(endpoint)
    require 'net/http'
    require 'uri'
    
    uri = URI(endpoint)
    response = Net::HTTP.get_response(uri)
    
    unless response.is_a?(Net::HTTPSuccess)
      raise "HTTP request failed: #{response.code}"
    end
    
    response
  end
  
  def check_load_test_thresholds(results)
    # Check response time threshold
    avg_response_time = results[:response_times].sum / results[:response_times].length
    response_time_threshold = parse_duration(@thresholds['response_time'])
    
    if avg_response_time > response_time_threshold
      raise PerformanceThresholdExceeded, "Average response time #{avg_response_time}ms exceeded threshold #{response_time_threshold}ms"
    end
    
    # Check throughput threshold
    throughput_threshold = parse_throughput(@thresholds['throughput'])
    
    if results[:throughput] < throughput_threshold
      raise PerformanceThresholdExceeded, "Throughput #{results[:throughput]}rps below threshold #{throughput_threshold}rps"
    end
  end
  
  def get_memory_usage
    if defined?(GC)
      stats = GC.stat
      (stats[:heap_allocated_pages] * stats[:heap_page_size] / 1024.0 / 1024.0).round(2)
    else
      0.0
    end
  end
  
  def parse_duration(duration_string)
    case duration_string
    when /(\d+)ms/
      $1.to_f
    when /(\d+)s/
      $1.to_f * 1000
    when /(\d+)m/
      $1.to_f * 60 * 1000
    else
      1000.0 # Default 1 second
    end
  end
  
  def parse_memory(memory_string)
    case memory_string
    when /(\d+)MB/i
      $1.to_f
    when /(\d+)GB/i
      $1.to_f * 1024
    else
      512.0 # Default 512MB
    end
  end
  
  def parse_throughput(throughput_string)
    case throughput_string
    when /(\d+)rps/
      $1.to_f
    else
      1000.0 # Default 1000 requests per second
    end
  end
end
```

### 4. Property-Based Testing Strategy
```ruby
require 'tusklang/testing'

class PropertyBasedTestingStrategy
  include TuskLang::Testing::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/testing.tsk')
    @generators = setup_generators
  end
  
  def property(description, &block)
    Property.new(description, block, @generators)
  end
  
  def for_all(*generators, &property_block)
    Property.new("Generated property", property_block, generators)
  end
  
  def integer(min: -1000, max: 1000)
    IntegerGenerator.new(min, max)
  end
  
  def string(min_length: 0, max_length: 100)
    StringGenerator.new(min_length, max_length)
  end
  
  def array(element_generator, min_size: 0, max_size: 10)
    ArrayGenerator.new(element_generator, min_size, max_size)
  end
  
  def hash(key_generator, value_generator, min_size: 0, max_size: 10)
    HashGenerator.new(key_generator, value_generator, min_size, max_size)
  end
  
  def one_of(*values)
    OneOfGenerator.new(values)
  end
  
  private
  
  def setup_generators
    {
      integer: ->(min: -1000, max: 1000) { IntegerGenerator.new(min, max) },
      string: ->(min_length: 0, max_length: 100) { StringGenerator.new(min_length, max_length) },
      array: ->(element_gen, min_size: 0, max_size: 10) { ArrayGenerator.new(element_gen, min_size, max_size) },
      hash: ->(key_gen, value_gen, min_size: 0, max_size: 10) { HashGenerator.new(key_gen, value_gen, min_size, max_size) }
    }
  end
end

class Property
  def initialize(description, property_block, generators)
    @description = description
    @property_block = property_block
    @generators = generators
  end
  
  def check(iterations: 100)
    results = {
      total: iterations,
      passed: 0,
      failed: 0,
      shrunk: nil
    }
    
    iterations.times do |i|
      begin
        # Generate test data
        test_data = generate_test_data
        
        # Run property
        result = @property_block.call(*test_data)
        
        if result
          results[:passed] += 1
        else
          results[:failed] += 1
          results[:shrunk] = shrink_test_data(test_data)
          break
        end
      rescue => e
        results[:failed] += 1
        results[:shrunk] = shrink_test_data(test_data, e)
        break
      end
    end
    
    results
  end
  
  private
  
  def generate_test_data
    @generators.map(&:generate)
  end
  
  def shrink_test_data(test_data, error = nil)
    # Implement test case shrinking
    # This is a simplified version
    test_data
  end
end

class IntegerGenerator
  def initialize(min, max)
    @min = min
    @max = max
  end
  
  def generate
    rand(@min..@max)
  end
end

class StringGenerator
  def initialize(min_length, max_length)
    @min_length = min_length
    @max_length = max_length
  end
  
  def generate
    length = rand(@min_length..@max_length)
    ('a'..'z').to_a.sample(length).join
  end
end

class ArrayGenerator
  def initialize(element_generator, min_size, max_size)
    @element_generator = element_generator
    @min_size = min_size
    @max_size = max_size
  end
  
  def generate
    size = rand(@min_size..@max_size)
    size.times.map { @element_generator.generate }
  end
end

class HashGenerator
  def initialize(key_generator, value_generator, min_size, max_size)
    @key_generator = key_generator
    @value_generator = value_generator
    @min_size = min_size
    @max_size = max_size
  end
  
  def generate
    size = rand(@min_size..@max_size)
    size.times.map { [@key_generator.generate, @value_generator.generate] }.to_h
  end
end

class OneOfGenerator
  def initialize(values)
    @values = values
  end
  
  def generate
    @values.sample
  end
end
```

### 5. Test Data Factory Strategy
```ruby
require 'tusklang/testing'
require 'factory_bot'

class TestDataFactoryStrategy
  include TuskLang::Testing::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/testing.tsk')
    setup_factory_bot
    load_factories
  end
  
  def create(model_name, attributes = {})
    FactoryBot.create(model_name, attributes)
  end
  
  def build(model_name, attributes = {})
    FactoryBot.build(model_name, attributes)
  end
  
  def build_stubbed(model_name, attributes = {})
    FactoryBot.build_stubbed(model_name, attributes)
  end
  
  def create_list(model_name, count, attributes = {})
    FactoryBot.create_list(model_name, count, attributes)
  end
  
  def build_list(model_name, count, attributes = {})
    FactoryBot.build_list(model_name, count, attributes)
  end
  
  def sequence(name, &block)
    FactoryBot.sequence(name, &block)
  end
  
  def trait(name, &block)
    FactoryBot.trait(name, &block)
  end
  
  private
  
  def setup_factory_bot
    FactoryBot.find_definitions
  end
  
  def load_factories
    factories_path = @config['testing']['databases']['factory_bot']['factories_path']
    Dir.glob("#{factories_path}/**/*.rb").each { |file| require file }
  end
end

# Example factory definitions
FactoryBot.define do
  factory :user do
    sequence(:email) { |n| "user#{n}@example.com" }
    password { "password123" }
    name { Faker::Name.name }
    
    trait :admin do
      role { "admin" }
    end
    
    trait :premium do
      subscription_type { "premium" }
    end
  end
  
  factory :order do
    user
    total { rand(10.0..1000.0).round(2) }
    status { "pending" }
    
    trait :completed do
      status { "completed" }
      completed_at { Time.now }
    end
    
    trait :cancelled do
      status { "cancelled" }
      cancelled_at { Time.now }
    end
  end
  
  factory :product do
    sequence(:name) { |n| "Product #{n}" }
    price { rand(1.0..100.0).round(2) }
    description { Faker::Lorem.paragraph }
    
    trait :out_of_stock do
      stock_quantity { 0 }
    end
    
    trait :on_sale do
      sale_price { price * 0.8 }
    end
  end
end
```

### 6. Test Configuration Strategy
```ruby
require 'tusklang/testing'

class TestConfigurationStrategy
  include TuskLang::Testing::Strategy
  
  def initialize
    @config = TuskLang.parse_file('config/testing.tsk')
    setup_test_environment
  end
  
  def setup_test_environment
    # Set test environment
    ENV['RAILS_ENV'] = 'test'
    ENV['RACK_ENV'] = 'test'
    
    # Configure database
    setup_test_database
    
    # Configure logging
    setup_test_logging
    
    # Configure caching
    setup_test_caching
  end
  
  def setup_test_database
    db_config = @config['testing']['databases']['test']
    
    ActiveRecord::Base.establish_connection(db_config)
    
    # Run migrations
    ActiveRecord::Migration.migrate
    
    # Setup database cleaner
    require 'database_cleaner'
    
    DatabaseCleaner.configure do |config|
      config.strategy = :truncation
      config.clean_with(:truncation)
    end
  end
  
  def setup_test_logging
    # Configure test logging
    Rails.logger = Logger.new(StringIO.new)
    Rails.logger.level = Logger::ERROR
  end
  
  def setup_test_caching
    # Configure test caching
    Rails.cache = ActiveSupport::Cache::MemoryStore.new
  end
  
  def before_each_test
    DatabaseCleaner.start
  end
  
  def after_each_test
    DatabaseCleaner.clean
  end
  
  def around_test(&block)
    before_each_test
    block.call
    after_each_test
  end
end
```

## 🔧 Advanced Configuration

### Test Reporting
```ruby
require 'tusklang/testing'

class TestReporter
  def initialize
    @config = TuskLang.parse_file('config/testing.tsk')
    @reports = []
  end
  
  def generate_report(test_results)
    report = {
      timestamp: Time.now.iso8601,
      summary: generate_summary(test_results),
      details: test_results,
      coverage: test_results[:coverage],
      performance: test_results[:performance_data]
    }
    
    @reports << report
    
    # Save report
    save_report(report)
    
    # Generate HTML report
    generate_html_report(report) if @config['testing']['reports']['html']['enabled']
    
    # Generate JSON report
    generate_json_report(report) if @config['testing']['reports']['json']['enabled']
    
    report
  end
  
  private
  
  def generate_summary(results)
    {
      total_tests: results[:total],
      passed_tests: results[:passed],
      failed_tests: results[:failed],
      skipped_tests: results[:skipped] || 0,
      success_rate: results[:total] > 0 ? (results[:passed].to_f / results[:total] * 100).round(2) : 0,
      duration: results[:duration]
    }
  end
  
  def save_report(report)
    reports_dir = @config['testing']['reports']['directory'] || 'test_reports'
    FileUtils.mkdir_p(reports_dir)
    
    filename = "test_report_#{Time.now.strftime('%Y%m%d_%H%M%S')}.json"
    File.write(File.join(reports_dir, filename), JSON.pretty_generate(report))
  end
  
  def generate_html_report(report)
    # Generate HTML report using a template
    template = File.read('templates/test_report.html.erb')
    html = ERB.new(template).result(binding)
    
    reports_dir = @config['testing']['reports']['directory'] || 'test_reports'
    filename = "test_report_#{Time.now.strftime('%Y%m%d_%H%M%S')}.html"
    File.write(File.join(reports_dir, filename), html)
  end
  
  def generate_json_report(report)
    reports_dir = @config['testing']['reports']['directory'] || 'test_reports'
    filename = "test_report_#{Time.now.strftime('%Y%m%d_%H%M%S')}.json"
    File.write(File.join(reports_dir, filename), JSON.pretty_generate(report))
  end
end
```

## 🚀 Performance Optimization

### Test Parallelization
```ruby
require 'tusklang/testing'

class TestParallelizer
  def initialize
    @config = TuskLang.parse_file('config/testing.tsk')
    @max_workers = @config['testing']['parallel']['max_workers'] || 4
  end
  
  def run_parallel_tests(test_files)
    require 'parallel'
    
    results = Parallel.map(test_files, in_threads: @max_workers) do |file|
      run_single_test_file(file)
    end
    
    aggregate_results(results)
  end
  
  private
  
  def run_single_test_file(file)
    # Implementation to run a single test file
    # This would depend on the testing framework being used
  end
  
  def aggregate_results(results)
    {
      total: results.sum { |r| r[:total] },
      passed: results.sum { |r| r[:passed] },
      failed: results.sum { |r| r[:failed] },
      skipped: results.sum { |r| r[:skipped] || 0 }
    }
  end
end
```

## 📊 Monitoring and Analytics

### Test Analytics
```ruby
require 'tusklang/testing'

class TestAnalytics
  def initialize
    @metrics = TuskLang::Metrics::Collector.new
  end
  
  def track_test_run(results)
    @metrics.increment("tests.runs.total")
    @metrics.increment("tests.total", results[:total])
    @metrics.increment("tests.passed", results[:passed])
    @metrics.increment("tests.failed", results[:failed])
    
    if results[:duration]
      @metrics.histogram("tests.duration", results[:duration])
    end
    
    if results[:coverage]
      @metrics.gauge("tests.coverage.percentage", results[:coverage][:percentage])
    end
  end
  
  def track_test_performance(operation, duration)
    @metrics.histogram("tests.performance.#{operation}", duration)
  end
  
  def get_test_stats
    {
      total_runs: @metrics.get("tests.runs.total"),
      total_tests: @metrics.get("tests.total"),
      success_rate: @metrics.get_rate("tests.passed", "tests.total"),
      average_duration: @metrics.get_average("tests.duration"),
      average_coverage: @metrics.get_average("tests.coverage.percentage")
    }
  end
end
```

This comprehensive testing system provides enterprise-grade testing features while maintaining the flexibility and power of TuskLang. The combination of multiple testing strategies, property-based testing, and performance optimizations creates a robust foundation for ensuring Ruby application quality. 
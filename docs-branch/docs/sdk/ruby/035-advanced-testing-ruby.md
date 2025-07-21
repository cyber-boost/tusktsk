# 🧪 TuskLang Ruby Advanced Testing Guide

**"We don't bow to any king" - Ruby Edition**

Master advanced testing strategies for TuskLang in Ruby. Learn property-based testing, mutation testing, and comprehensive test coverage.

## 🧬 Property-Based Testing

### 1. Rantly Integration
```ruby
# spec/property_spec.rb
require 'rantly/rspec_extensions'
require 'tusklang'

describe 'TuskLang Property Tests' do
  it 'parses any valid string config' do
    property_of { string }.check do |str|
      parser = TuskLang.new
      expect { parser.parse("$test: \"#{str}\"") }.not_to raise_error
    end
  end

  it 'handles nested objects correctly' do
    property_of { 
      sized(3) { 
        array(sized(2) { [string, string] }) 
      } 
    }.check do |nested_data|
      config_content = nested_data.map { |k, v| "[#{k}]\nvalue: \"#{v}\"" }.join("\n")
      parser = TuskLang.new
      result = parser.parse(config_content)
      expect(result).to be_a(Hash)
    end
  end

  it 'preserves data types' do
    property_of { 
      one_of(
        integer,
        float,
        string,
        boolean
      ) 
    }.check do |value|
      parser = TuskLang.new
      config = parser.parse("$test: #{value}")
      expect(config['test']).to eq(value)
    end
  end
end
```

### 2. PropCheck Integration
```ruby
# spec/propcheck_spec.rb
require 'propcheck'
require 'tusklang'

describe 'TuskLang PropCheck Tests' do
  it 'round-trips config data' do
    PropCheck.forall(
      PropCheck::Generators.hash(
        PropCheck::Generators.string,
        PropCheck::Generators.one_of(
          PropCheck::Generators.integer,
          PropCheck::Generators.string,
          PropCheck::Generators.boolean
        )
      )
    ) do |data|
      parser = TuskLang.new
      
      # Convert to TSK format
      tsk_content = data.map { |k, v| "$#{k}: #{v.inspect}" }.join("\n")
      
      # Parse and verify
      result = parser.parse(tsk_content)
      expect(result).to include(data)
    end
  end
end
```

## 🦠 Mutation Testing

### 1. Mutant Integration
```ruby
# spec/mutation_spec.rb
require 'tusklang'

describe 'TuskLang Mutation Tests' do
  it 'detects invalid syntax mutations' do
    parser = TuskLang.new
    
    # Original valid config
    valid_config = "$app_name: \"MyApp\"\n[database]\nhost: \"localhost\""
    
    # Mutated configs (should fail)
    invalid_configs = [
      "$app_name: MyApp\n[database]\nhost: \"localhost\"",  # Missing quotes
      "$app_name: \"MyApp\"\n[database\nhost: \"localhost\"",  # Missing bracket
      "$app_name: \"MyApp\"\n[database]\nhost: localhost",  # Missing quotes
    ]
    
    expect { parser.parse(valid_config) }.not_to raise_error
    
    invalid_configs.each do |invalid_config|
      expect { parser.parse(invalid_config) }.to raise_error(TuskLang::ParseError)
    end
  end
end
```

### 2. Custom Mutation Testing
```ruby
# lib/tusklang_mutation_tester.rb
class TuskLangMutationTester
  def self.mutate_config(config_content)
    mutations = []
    
    # Remove quotes from strings
    mutations << config_content.gsub(/"(.*?)"/, '\1')
    
    # Add extra brackets
    mutations << config_content.gsub(/\[(.*?)\]/, '[[\1]]')
    
    # Remove colons
    mutations << config_content.gsub(':', '')
    
    # Change section names
    mutations << config_content.gsub(/\[(.*?)\]/, '[mutated_\1]')
    
    mutations
  end

  def self.test_mutations(original_config, test_proc)
    parser = TuskLang.new
    
    # Test original config
    original_result = test_proc.call(parser.parse(original_config))
    
    # Test mutations
    mutations = mutate_config(original_config)
    mutation_results = []
    
    mutations.each do |mutation|
      begin
        result = test_proc.call(parser.parse(mutation))
        mutation_results << { mutation: mutation, result: result, passed: true }
      rescue => e
        mutation_results << { mutation: mutation, error: e.message, passed: false }
      end
    end
    
    {
      original_result: original_result,
      mutation_results: mutation_results,
      mutation_score: mutation_results.count { |r| !r[:passed] }.to_f / mutation_results.length
    }
  end
end
```

## 🔄 Integration Testing

### 1. Full Stack Integration
```ruby
# spec/integration/full_stack_spec.rb
require 'rails_helper'
require 'tusklang'

describe 'TuskLang Full Stack Integration' do
  before(:all) do
    # Load test database
    ActiveRecord::Base.establish_connection(adapter: 'sqlite3', database: ':memory:')
    load 'db/schema.rb'
  end

  it 'integrates with Rails models' do
    parser = TuskLang.new
    config = parser.parse_file('config/test_app.tsk')
    
    # Test database integration
    user_count = config['analytics']['user_count']
    expect(user_count).to be_a(Integer)
    
    # Test Rails model integration
    user = User.create!(email: 'test@example.com')
    expect(User.count).to eq(user_count + 1)
  end

  it 'integrates with Rails controllers' do
    parser = TuskLang.new
    config = parser.parse_file('config/test_app.tsk')
    
    # Test controller integration
    get '/api/users'
    expect(response).to have_http_status(200)
    
    response_data = JSON.parse(response.body)
    expect(response_data['config']['app_name']).to eq(config['app_name'])
  end

  it 'integrates with background jobs' do
    parser = TuskLang.new
    config = parser.parse_file('config/test_app.tsk')
    
    # Test job integration
    TestJob.perform_now
    expect(TestJob.config['job_settings']['enabled']).to eq(config['jobs']['test_job']['enabled'])
  end
end
```

## 🧪 Comprehensive Test Strategies

### 1. Test Configuration Factory
```ruby
# spec/support/config_factory.rb
class ConfigFactory
  def self.create_basic_config
    {
      'app_name' => 'TestApp',
      'version' => '1.0.0',
      'database' => {
        'host' => 'localhost',
        'port' => 5432,
        'name' => 'test_db'
      }
    }
  end

  def self.create_complex_config
    {
      'app_name' => 'ComplexApp',
      'version' => '2.0.0',
      'database' => {
        'host' => 'localhost',
        'port' => 5432,
        'name' => 'complex_db',
        'pool_size' => 10
      },
      'cache' => {
        'driver' => 'redis',
        'host' => 'localhost',
        'port' => 6379,
        'ttl' => '5m'
      },
      'features' => {
        'user_registration' => true,
        'email_verification' => true,
        'two_factor_auth' => false
      }
    }
  end

  def self.create_invalid_config
    {
      'app_name' => 'InvalidApp',
      'database' => {
        'host' => 'localhost',
        'port' => 'invalid_port',  # Should be integer
        'name' => nil  # Should not be nil
      }
    }
  end
end
```

### 2. Test Suite Organization
```ruby
# spec/tusklang_spec.rb
require 'spec_helper'
require 'tusklang'

describe 'TuskLang' do
  describe 'Parsing' do
    it 'parses basic configs' do
      parser = TuskLang.new
      config = parser.parse_file('spec/fixtures/basic_config.tsk')
      expect(config['app_name']).to eq('TestApp')
    end

    it 'parses complex configs' do
      parser = TuskLang.new
      config = parser.parse_file('spec/fixtures/complex_config.tsk')
      expect(config['features']['user_registration']).to be true
    end

    it 'handles invalid configs gracefully' do
      parser = TuskLang.new
      expect { parser.parse_file('spec/fixtures/invalid_config.tsk') }.to raise_error(TuskLang::ParseError)
    end
  end

  describe 'Validation' do
    it 'validates configs against schemas' do
      parser = TuskLang.new
      parser.schema_file = 'spec/fixtures/config_schema.tsk'
      expect(parser.validate_file('spec/fixtures/valid_config.tsk')).to be true
    end

    it 'rejects invalid configs' do
      parser = TuskLang.new
      parser.schema_file = 'spec/fixtures/config_schema.tsk'
      expect(parser.validate_file('spec/fixtures/invalid_config.tsk')).to be false
    end
  end

  describe 'Integration' do
    it 'works with Rails' do
      # Test Rails integration
      expect(Rails.application.config.tusk_config).to be_present
    end

    it 'works with databases' do
      parser = TuskLang.new
      config = parser.parse_file('spec/fixtures/db_config.tsk')
      expect(config['database']['host']).to eq('localhost')
    end

    it 'works with caching' do
      parser = TuskLang.new
      config = parser.parse_file('spec/fixtures/cache_config.tsk')
      expect(config['cache']['driver']).to eq('redis')
    end
  end
end
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/advanced_testing_service.rb
require 'tusklang'

class AdvancedTestingService
  def self.run_property_tests
    # Run property-based tests
    RSpec::Core::Runner.run(['spec/property_spec.rb'])
  end

  def self.run_mutation_tests
    # Run mutation tests
    original_config = File.read('config/app.tsk')
    test_proc = ->(config) { config['app_name'] == 'MyApp' }
    
    TuskLangMutationTester.test_mutations(original_config, test_proc)
  end

  def self.run_integration_tests
    # Run integration tests
    RSpec::Core::Runner.run(['spec/integration/'])
  end

  def self.generate_test_coverage_report
    # Generate test coverage report
    require 'simplecov'
    SimpleCov.start do
      add_filter '/spec/'
      add_filter '/config/'
    end
    
    # Run all tests
    RSpec::Core::Runner.run(['spec/'])
    
    # Generate report
    SimpleCov.result.format!
  end
end

# Usage
AdvancedTestingService.run_property_tests
mutation_results = AdvancedTestingService.run_mutation_tests
AdvancedTestingService.run_integration_tests
AdvancedTestingService.generate_test_coverage_report
```

## 🛡️ Best Practices
- Use property-based testing for config robustness.
- Implement mutation testing to ensure test quality.
- Test all integration points (Rails, databases, caching).
- Maintain comprehensive test coverage.
- Use test factories for consistent test data.

**Ready to test like a pro? Let's Tusk! 🚀** 
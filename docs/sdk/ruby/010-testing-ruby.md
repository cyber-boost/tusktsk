# 🧪 TuskLang Ruby Testing Guide

**"We don't bow to any king" - Ruby Edition**

Ensure your TuskLang configs are robust and error-free. Learn how to validate, test, and integrate TuskLang with Ruby's testing frameworks.

## 🚦 Config Validation

### 1. Syntax Validation
```ruby
# config/test.tsk
$app_name: "TestApp"
$version: "1.0.0"

[database]
host: "localhost"
port: 5432
```

```ruby
# test/validate_config.rb
require 'tusklang'

parser = TuskLang.new
if parser.validate_file('config/test.tsk')
  puts "✅ Config is valid!"
else
  puts "❌ Config has errors!"
end
```

### 2. Schema Validation
```ruby
# config/schema.tsk
[database]
host: string
port: integer
name: string
user: string
password: string
```

```ruby
# test/schema_validation.rb
require 'tusklang'

parser = TuskLang.new
parser.schema_file = 'config/schema.tsk'
if parser.validate_file('config/app.tsk')
  puts "✅ Schema is valid!"
else
  puts "❌ Schema validation failed!"
end
```

## 🧑‍💻 Test-Driven Config

### 1. RSpec Integration
```ruby
# spec/tusk_config_spec.rb
require 'tusklang'

describe 'TuskLang Config' do
  let(:parser) { TuskLang.new }
  let(:config) { parser.parse_file('config/app.tsk') }

  it 'has a valid app name' do
    expect(config['app_name']).to eq('MyApp')
  end

  it 'has a valid database host' do
    expect(config['database']['host']).to eq('localhost')
  end
end
```

### 2. Minitest Integration
```ruby
# test/tusk_config_test.rb
require 'minitest/autorun'
require 'tusklang'

class TuskConfigTest < Minitest::Test
  def setup
    @parser = TuskLang.new
    @config = @parser.parse_file('config/app.tsk')
  end

  def test_app_name
    assert_equal 'MyApp', @config['app_name']
  end

  def test_database_host
    assert_equal 'localhost', @config['database']['host']
  end
end
```

## 🛠️ Ruby Integration Example
```ruby
# test/integration_test.rb
require 'tusklang'

parser = TuskLang.new
config = parser.parse_file('config/app.tsk')

raise 'Invalid config' unless config['app_name']
puts "Config loaded: #{config['app_name']}"
```

## 🧪 Best Practices
- Always validate configs before deploying.
- Use schema files to enforce structure.
- Integrate config tests with CI/CD pipelines.
- Test all environment-specific configs.

## 🚨 Troubleshooting
- For validation errors, check syntax and required fields.
- For test failures, verify config values and test logic.

**Ready to test with confidence? Let's Tusk! 🚀** 
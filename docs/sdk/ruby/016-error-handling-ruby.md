# 🛑 TuskLang Ruby Error Handling Guide

**"We don't bow to any king" - Ruby Edition**

Build robust Ruby applications with TuskLang's advanced error handling. Validate configs, catch runtime errors, and enforce best practices for reliability.

## 🚦 Config Validation Errors

### 1. Syntax Errors
```ruby
# config/bad.tsk
$app_name: "MyApp"
$version: 1.0.0  # Missing quotes (should be string)

[database]
host: "localhost"
port: 5432
```

```ruby
# test/validate_config.rb
require 'tusklang'

parser = TuskLang.new
begin
  parser.parse_file('config/bad.tsk')
rescue TuskLang::ParseError => e
  puts "❌ Parse error: #{e.message}"
end
```

### 2. Schema Validation Errors
```ruby
# config/schema.tsk
[database]
host: string
port: integer
```

```ruby
# test/schema_validation.rb
require 'tusklang'

parser = TuskLang.new
parser.schema_file = 'config/schema.tsk'
begin
  parser.parse_file('config/app.tsk')
rescue TuskLang::SchemaError => e
  puts "❌ Schema error: #{e.message}"
end
```

## 🛠️ Runtime Error Handling

### 1. Rescue in Ruby Code
```ruby
# app/services/config_loader.rb
require 'tusklang'

class ConfigLoader
  def self.load(file)
    parser = TuskLang.new
    parser.parse_file(file)
  rescue TuskLang::ParseError, TuskLang::SchemaError => e
    Rails.logger.error("Config error: #{e.message}")
    nil
  end
end
```

### 2. Fallback Defaults
```ruby
# app/services/config_loader.rb
class ConfigLoader
  def self.load_with_fallback(file, fallback = {})
    parser = TuskLang.new
    parser.parse_file(file)
  rescue
    fallback
  end
end
```

## 🛡️ Best Practices
- Always validate configs before loading.
- Rescue and log all config-related errors.
- Provide sensible defaults for critical settings.
- Monitor error logs and set up alerts for failures.

**Ready to build unbreakable Ruby apps? Let's Tusk! 🚀** 
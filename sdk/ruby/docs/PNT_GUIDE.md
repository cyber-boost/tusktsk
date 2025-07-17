# 🥜 Peanut Binary Configuration Guide for Ruby

A comprehensive guide to using TuskLang's high-performance binary configuration system with Ruby.

## Table of Contents

1. [What is Peanut Configuration?](#what-is-peanut-configuration)
2. [Installation and Setup](#installation-and-setup)
3. [Quick Start](#quick-start)
4. [Core Concepts](#core-concepts)
5. [API Reference](#api-reference)
6. [Advanced Usage](#advanced-usage)
7. [Ruby-Specific Features](#ruby-specific-features)
8. [Integration Examples](#integration-examples)
9. [Binary Format Details](#binary-format-details)
10. [Performance Guide](#performance-guide)
11. [Troubleshooting](#troubleshooting)
12. [Migration Guide](#migration-guide)
13. [Complete Examples](#complete-examples)
14. [Quick Reference](#quick-reference)

## What is Peanut Configuration?

Peanut Configuration is TuskLang's high-performance binary configuration system that provides **85% faster loading** compared to text-based formats like JSON, YAML, or TOML. It combines the readability of human-friendly configuration files with the performance of binary serialization.

### Key Benefits

- **85% Performance Improvement**: Binary loading is significantly faster than text parsing
- **Type Safety**: Automatic type inference and validation
- **Hierarchical Loading**: CSS-like cascading configuration inheritance
- **Ruby Integration**: Native Ruby objects and idiomatic patterns
- **Cross-Platform**: Works on all Ruby platforms (MRI, JRuby, etc.)
- **Backward Compatible**: Supports existing text formats alongside binary

### File Types

- **`.peanuts`** - Human-readable configuration (recommended for development)
- **`.tsk`** - TuskLang syntax with advanced features
- **`.pnt`** - Compiled binary format (recommended for production)

## Installation and Setup

### Prerequisites

- Ruby 2.7 or higher
- TuskLang Ruby SDK installed

### Installing the SDK

```bash
# Add to your Gemfile
gem 'tusk_lang'

# Or install directly
gem install tusk_lang
```

### Importing PeanutConfig

```ruby
# Basic import
require 'tusk_lang'
require_relative 'lib/peanut_config'

# Or use the full module
include TuskLang
```

## Quick Start

### Your First Peanut Configuration

1. Create a `peanu.peanuts` file:

```ini
[app]
name: "My Ruby App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
timeout: 30.5

[database]
adapter: "postgresql"
host: "db.example.com"
pool: 5
```

2. Load the configuration:

```ruby
require 'tusk_lang'
require_relative 'lib/peanut_config'

# Load configuration
config = PeanutConfig.new
app_config = config.load('.')

puts "App: #{app_config['app']['name']}"
puts "Port: #{app_config['server']['port']}"
```

3. Access values:

```ruby
# Direct access
app_name = app_config['app']['name']
port = app_config['server']['port']

# Using the get method with defaults
timeout = config.get('server.timeout', 30, '.')
pool_size = config.get('database.pool', 10, '.')

puts "App: #{app_name}, Port: #{port}, Timeout: #{timeout}s"
```

## Core Concepts

### File Types

#### .peanuts (Human-Readable)
```ini
[app]
name: "My App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
```

#### .tsk (TuskLang Advanced)
```tsk
[app]
name = "My App"
version = "1.0.0"
debug = true

[server]
host = "localhost"
port = 8080
timeout = 30.5

[processing]
transform_fujsen = """
function transform(data) {
  return data.map(item => ({
    ...item,
    processed: true,
    timestamp: new Date().toISOString()
  }));
}
"""
```

#### .pnt (Binary Format)
```ruby
# Automatically generated from .peanuts or .tsk files
# 85% faster loading, smaller file size
```

### Hierarchical Loading

PeanutConfig uses CSS-like cascading where child configurations override parent configurations:

```
/opt/tsk_git/sdk-pnt-test/ruby/
├── peanu.tsk          # Root configuration
├── app/
│   ├── peanu.peanuts  # App-specific overrides
│   └── config/
│       └── peanu.pnt  # Environment-specific (binary)
```

```ruby
# Configuration hierarchy
config = PeanutConfig.new
hierarchy = config.find_config_hierarchy('./app/config')

hierarchy.each do |config_file|
  puts "#{config_file.path} (#{config_file.type})"
end

# Loads and merges all configurations
final_config = config.load('./app/config')
```

### Type System

PeanutConfig automatically infers types from your configuration:

```ruby
# Type inference examples
config_data = {
  'string_value' => 'hello',
  'number_value' => 42,
  'float_value' => 3.14,
  'boolean_value' => true,
  'array_value' => [1, 2, 3],
  'hash_value' => { 'key' => 'value' },
  'null_value' => nil
}

# All types are preserved
puts config_data['string_value'].class  # String
puts config_data['number_value'].class  # Integer
puts config_data['float_value'].class   # Float
puts config_data['boolean_value'].class # TrueClass
puts config_data['array_value'].class   # Array
puts config_data['hash_value'].class    # Hash
puts config_data['null_value'].class    # NilClass
```

## API Reference

### PeanutConfig Class

#### Constructor

```ruby
# Basic initialization
config = PeanutConfig.new

# With options
config = PeanutConfig.new(
  auto_compile: true,  # Auto-compile .peanuts to .pnt
  watch: true,         # Watch for file changes
  cache_enabled: true  # Enable caching
)
```

#### Methods

##### load(directory = nil)

Loads and merges configuration hierarchy.

```ruby
# Load from current directory
config_data = config.load

# Load from specific directory
config_data = config.load('./config')

# Load with custom path
config_data = config.load('/path/to/config')
```

**Parameters:**
- `directory` (String, optional): Directory to load from

**Returns:** Hash with merged configuration

**Example:**
```ruby
config = PeanutConfig.new
app_config = config.load('./app')

puts "App name: #{app_config['app']['name']}"
puts "Server port: #{app_config['server']['port']}"
```

##### get(key_path, default_value = nil, directory = nil)

Gets a specific configuration value by key path.

```ruby
# Get value with default
host = config.get('server.host', 'localhost', '.')

# Get nested value
timeout = config.get('server.timeout', 30, '.')

# Get array value
features = config.get('app.features', [], '.')
```

**Parameters:**
- `key_path` (String): Dot-separated key path (e.g., "server.host")
- `default_value` (Any, optional): Default value if key not found
- `directory` (String, optional): Directory to load from

**Returns:** Configuration value or default

**Example:**
```ruby
config = PeanutConfig.new

# Get server configuration
host = config.get('server.host', 'localhost', '.')
port = config.get('server.port', 8080, '.')
timeout = config.get('server.timeout', 30.0, '.')

puts "Server: #{host}:#{port} (timeout: #{timeout}s)"
```

##### compile_to_binary(config_data, output_path)

Compiles configuration data to binary format.

```ruby
# Compile configuration to binary
config = PeanutConfig.new
config_data = config.load('.')

config.compile_to_binary(config_data, 'config.pnt')
```

**Parameters:**
- `config_data` (Hash): Configuration data to compile
- `output_path` (String): Output file path

**Example:**
```ruby
config = PeanutConfig.new

# Load and compile
config_data = config.load('.')
config.compile_to_binary(config_data, 'production.pnt')

puts "Binary configuration compiled to production.pnt"
```

##### load_binary(file_path)

Loads configuration from binary file.

```ruby
# Load binary configuration
config = PeanutConfig.new
config_data = config.load_binary('config.pnt')
```

**Parameters:**
- `file_path` (String): Path to binary configuration file

**Returns:** Hash with configuration data

**Example:**
```ruby
config = PeanutConfig.new

begin
  config_data = config.load_binary('production.pnt')
  puts "Loaded binary configuration"
  puts "App: #{config_data['app']['name']}"
rescue => e
  puts "Error loading binary config: #{e.message}"
end
```

##### find_config_hierarchy(directory = nil)

Finds all configuration files in hierarchy.

```ruby
# Find configuration hierarchy
config = PeanutConfig.new
hierarchy = config.find_config_hierarchy('.')

hierarchy.each do |config_file|
  puts "#{config_file.path} (#{config_file.type})"
end
```

**Parameters:**
- `directory` (String, optional): Directory to search from

**Returns:** Array of configuration file objects

**Example:**
```ruby
config = PeanutConfig.new
hierarchy = config.find_config_hierarchy('./app')

puts "Configuration hierarchy:"
hierarchy.each do |file|
  puts "  #{file.path} (#{file.type})"
end
```

##### clear_cache

Clears the configuration cache.

```ruby
# Clear cache
config = PeanutConfig.new
config.clear_cache
```

**Example:**
```ruby
config = PeanutConfig.new

# Load configuration (cached)
config_data1 = config.load('.')

# Modify configuration file
File.write('peanu.peanuts', 'new_key: "new_value"')

# Clear cache and reload
config.clear_cache
config_data2 = config.load('.')

puts "Cache cleared, new value: #{config_data2['new_key']}"
```

##### parse_text_config(content)

Parses text configuration content.

```ruby
# Parse text configuration
config = PeanutConfig.new
content = <<~CONFIG
  [app]
  name: "My App"
  version: "1.0.0"
CONFIG

config_data = config.parse_text_config(content)
```

**Parameters:**
- `content` (String): Configuration text content

**Returns:** Hash with parsed configuration

**Example:**
```ruby
config = PeanutConfig.new

content = File.read('config.peanuts')
config_data = config.parse_text_config(content)

puts "Parsed configuration: #{config_data['app']['name']}"
```

## Advanced Usage

### File Watching

```ruby
require 'filewatcher'

config = PeanutConfig.new

# Watch for configuration changes
FileWatcher.new(['peanu.peanuts', 'peanu.tsk']).watch do |filename, event|
  puts "Configuration file changed: #{filename}"
  
  # Clear cache and reload
  config.clear_cache
  new_config = config.load('.')
  
  puts "Configuration reloaded"
end
```

### Custom Serialization

```ruby
# Custom type handling
class CustomConfig
  def initialize(data)
    @data = data
  end
  
  def to_hash
    @data
  end
  
  def self.from_hash(hash)
    new(hash)
  end
end

# Use with PeanutConfig
config = PeanutConfig.new
config_data = config.load('.')

custom_config = CustomConfig.from_hash(config_data)
```

### Performance Optimization

```ruby
# Singleton pattern for configuration
class AppConfig
  @@instance = nil
  @@config = nil
  
  def self.instance
    @@instance ||= new
  end
  
  def self.config
    @@config ||= load_config
  end
  
  private
  
  def self.load_config
    config = PeanutConfig.new
    config.load('.')
  end
end

# Usage
app_config = AppConfig.config
server_host = app_config['server']['host']
```

### Thread Safety

```ruby
require 'thread'

class ThreadSafeConfig
  def initialize
    @mutex = Mutex.new
    @config = nil
  end
  
  def get(key_path, default = nil)
    @mutex.synchronize do
      load_config if @config.nil?
      get_nested_value(@config, key_path, default)
    end
  end
  
  private
  
  def load_config
    config = PeanutConfig.new
    @config = config.load('.')
  end
  
  def get_nested_value(hash, key_path, default)
    keys = key_path.split('.')
    keys.inject(hash) { |h, k| h&.dig(k) } || default
  end
end

# Thread-safe usage
config = ThreadSafeConfig.new

Thread.new do
  host = config.get('server.host', 'localhost')
  puts "Thread 1: #{host}"
end

Thread.new do
  port = config.get('server.port', 8080)
  puts "Thread 2: #{port}"
end
```

## Ruby-Specific Features

### Rails Integration

```ruby
# config/application.rb
require 'tusk_lang'
require_relative '../lib/peanut_config'

class Application < Rails::Application
  # Load TuskLang configuration
  config_tsk = TuskLang::TSK.from_file(Rails.root.join('config', 'app.tsk'))
  
  # Apply settings
  config.app_name = config_tsk.get_value("app", "name")
  config.debug_mode = config_tsk.get_value("app", "debug")
  config.api_key = config_tsk.get_value("api", "key")
  
  # Database configuration
  config.database_config = config_tsk.get_value("database", {})
end
```

### Jekyll Integration

```ruby
# _plugins/tusk_lang.rb
require 'tusk_lang'
require_relative '../lib/peanut_config'

module Jekyll
  class TuskLangConfig
    def self.load_config
      config_file = File.join(Dir.pwd, '_config.tsk')
      return {} unless File.exist?(config_file)
      
      TuskLang::TSK.from_file(config_file).to_hash
    end
  end
end

# Extend Jekyll configuration
Jekyll::Hooks.register :site, :after_init do |site|
  tsk_config = Jekyll::TuskLangConfig.load_config
  
  # Merge TSK config with Jekyll config
  site.config.merge!(tsk_config)
end
```

### Gem Integration

```ruby
# lib/my_gem/configuration.rb
require 'tusk_lang'
require_relative '../peanut_config'

module MyGem
  class Configuration
    def self.load
      config = PeanutConfig.new
      config.load(File.dirname(__FILE__))
    end
    
    def self.get(key, default = nil)
      @config ||= load
      keys = key.split('.')
      keys.inject(@config) { |h, k| h&.dig(k) } || default
    end
  end
end
```

### CLI Integration

```ruby
#!/usr/bin/env ruby
require 'optparse'
require 'tusk_lang'
require_relative 'lib/peanut_config'

class CLI
  def self.run
    options = {}
    
    OptionParser.new do |opts|
      opts.on('--config=PATH') { |path| options[:config] = path }
      opts.on('--key=KEY') { |key| options[:key] = key }
    end.parse!
    
    config = PeanutConfig.new
    config_data = config.load(options[:config] || '.')
    
    if options[:key]
      value = config.get(options[:key], nil, options[:config] || '.')
      puts value
    else
      puts config_data.to_json
    end
  end
end

CLI.run if __FILE__ == $0
```

## Integration Examples

### Rails Application

```ruby
# config/peanu.peanuts
[app]
name: "My Rails App"
environment: "production"
debug: false

[database]
adapter: "postgresql"
host: "@env('DATABASE_HOST')"
port: 5432
database: "@env('DATABASE_NAME')"
username: "@env('DATABASE_USER')"
password: "@env('DATABASE_PASSWORD')"

[redis]
url: "@env('REDIS_URL')"
pool: 5

[api]
key: "@env('API_KEY')"
endpoint: "https://api.example.com"
```

```ruby
# config/application.rb
require 'tusk_lang'
require_relative '../lib/peanut_config'

class Application < Rails::Application
  # Load configuration
  config_loader = PeanutConfig.new
  app_config = config_loader.load(Rails.root.join('config'))
  
  # Apply configuration
  config.app_name = app_config['app']['name']
  config.debug_mode = app_config['app']['debug']
  
  # Database configuration
  config.database_config = app_config['database']
  
  # Redis configuration
  config.redis_url = app_config['redis']['url']
  config.redis_pool = app_config['redis']['pool']
  
  # API configuration
  config.api_key = app_config['api']['key']
  config.api_endpoint = app_config['api']['endpoint']
end
```

### Jekyll Site

```ruby
# _config.tsk
[site]
title: "My Jekyll Blog"
description: "Built with Jekyll and TuskLang"
url: "https://example.com"
author: "John Doe"

[build]
destination: "_site"
plugins: ["jekyll-feed", "jekyll-seo-tag"]

[content]
generate_posts_fujsen = """
function generate_posts() {
  return [
    {
      title: "First Post",
      date: "2024-01-15",
      slug: "first-post",
      content: "This is the first post content."
    },
    {
      title: "Second Post", 
      date: "2024-01-16",
      slug: "second-post",
      content: "This is the second post content."
    }
  ];
}
"""
```

```ruby
# _plugins/tusk_lang.rb
require 'tusk_lang'
require_relative '../lib/peanut_config'

module Jekyll
  class TuskLangConfig
    def self.load_config
      config_file = File.join(Dir.pwd, '_config.tsk')
      return {} unless File.exist?(config_file)
      
      TuskLang::TSK.from_file(config_file).to_hash
    end
  end
end

# Extend Jekyll configuration
Jekyll::Hooks.register :site, :after_init do |site|
  tsk_config = Jekyll::TuskLangConfig.load_config
  
  # Merge TSK config with Jekyll config
  site.config.merge!(tsk_config)
end

# Generate dynamic content
Jekyll::Hooks.register :site, :pre_render do |site|
  config = TuskLang::TSK.from_file(File.join(Dir.pwd, '_config.tsk'))
  
  # Generate posts from FUJSEN
  posts_data = config.execute_fujsen("content", "generate_posts")
  
  posts_data.each do |post|
    # Create Jekyll post
    site.posts.docs << Jekyll::Document.new(
      File.join(site.source, '_posts', "#{post['date']}-#{post['slug']}.md"),
      site: site,
      collection: site.posts
    )
  end
end
```

## Binary Format Details

### File Structure

| Offset | Size | Description |
|--------|------|-------------|
| 0 | 4 | Magic: "PNUT" |
| 4 | 4 | Version (Little-Endian) |
| 8 | 8 | Timestamp (Little-Endian) |
| 16 | 8 | SHA256 checksum |
| 24 | N | Serialized data |

### Serialization Format

Ruby uses Marshal for binary serialization, which provides:

- **Type Preservation**: All Ruby types are preserved
- **Object References**: Circular references are handled
- **Compression**: Automatic compression for large data
- **Security**: Safe deserialization with validation

```ruby
# Binary format example
config_data = {
  'app' => {
    'name' => 'My App',
    'version' => '1.0.0',
    'features' => ['feature1', 'feature2']
  },
  'server' => {
    'host' => 'localhost',
    'port' => 8080,
    'timeout' => 30.5
  }
}

# Compile to binary
config = PeanutConfig.new
config.compile_to_binary(config_data, 'config.pnt')

# Load from binary
loaded_config = config.load_binary('config.pnt')
puts loaded_config['app']['name']  # "My App"
```

### Validation

```ruby
# Validate binary file
config = PeanutConfig.new

begin
  config_data = config.load_binary('config.pnt')
  puts "✅ Binary file is valid"
rescue => e
  puts "❌ Binary file is invalid: #{e.message}"
end
```

## Performance Guide

### Benchmarks

```ruby
require 'benchmark'

# Benchmark text vs binary loading
config = PeanutConfig.new

# Text loading benchmark
text_time = Benchmark.realtime do
  100.times do
    config.load('.')
  end
end

# Binary loading benchmark
binary_time = Benchmark.realtime do
  100.times do
    config.load_binary('config.pnt')
  end
end

improvement = ((text_time - binary_time) / text_time * 100).round(1)

puts "Text loading: #{text_time.round(3)}s"
puts "Binary loading: #{binary_time.round(3)}s"
puts "Improvement: #{improvement}%"
```

### Best Practices

1. **Always use .pnt in production**
   ```ruby
   # Development
   config_data = config.load('.')
   
   # Production
   config_data = config.load_binary('production.pnt')
   ```

2. **Cache configuration objects**
   ```ruby
   class ConfigCache
     @@cache = {}
     
     def self.get(key)
       @@cache[key] ||= load_config(key)
     end
     
     private
     
     def self.load_config(key)
       config = PeanutConfig.new
       config.load(key)
     end
   end
   ```

3. **Use file watching wisely**
   ```ruby
   # Only watch in development
   if Rails.env.development?
     config.watch = true
   end
   ```

4. **Optimize for your use case**
   ```ruby
   # For frequent access
   config = PeanutConfig.new(auto_compile: true, cache_enabled: true)
   
   # For memory-constrained environments
   config = PeanutConfig.new(cache_enabled: false)
   ```

## Troubleshooting

### Common Issues

#### File Not Found

```ruby
# Problem: Configuration file not found
begin
  config = PeanutConfig.new
  config_data = config.load('./nonexistent')
rescue => e
  puts "Error: #{e.message}"
  puts "Solution: Create peanu.peanuts or peanu.tsk file"
end
```

**Solution:**
```ruby
# Create default configuration
File.write('peanu.peanuts', <<~CONFIG)
  [app]
  name: "Default App"
  version: "1.0.0"
CONFIG
```

#### Checksum Mismatch

```ruby
# Problem: Binary file corruption
begin
  config = PeanutConfig.new
  config_data = config.load_binary('corrupted.pnt')
rescue => e
  puts "Error: #{e.message}"
  puts "Solution: Recompile from source"
end
```

**Solution:**
```ruby
# Recompile from source
config = PeanutConfig.new
config_data = config.load('.')
config.compile_to_binary(config_data, 'config.pnt')
```

#### Performance Issues

```ruby
# Problem: Slow configuration loading
config = PeanutConfig.new

# Check if using binary format
if File.exist?('config.pnt')
  config_data = config.load_binary('config.pnt')  # Fast
else
  config_data = config.load('.')  # Slower
end
```

**Solution:**
```ruby
# Enable auto-compilation
config = PeanutConfig.new(auto_compile: true)
config_data = config.load('.')  # Automatically compiles to .pnt
```

### Debug Mode

```ruby
# Enable debug logging
config = PeanutConfig.new

# Set debug environment variable
ENV['PEANUT_DEBUG'] = 'true'

# Load with debug output
config_data = config.load('.')
```

## Migration Guide

### From JSON

```ruby
# Old JSON configuration
json_config = {
  "app" => {
    "name" => "My App",
    "version" => "1.0.0"
  },
  "server" => {
    "host" => "localhost",
    "port" => 8080
  }
}

# Convert to PeanutConfig
config = PeanutConfig.new

# Save as .peanuts
peanuts_content = <<~CONFIG
  [app]
  name: "#{json_config['app']['name']}"
  version: "#{json_config['app']['version']}"

  [server]
  host: "#{json_config['server']['host']}"
  port: #{json_config['server']['port']}
CONFIG

File.write('peanu.peanuts', peanuts_content)

# Load with PeanutConfig
config_data = config.load('.')
```

### From YAML

```ruby
require 'yaml'

# Old YAML configuration
yaml_content = <<~YAML
  app:
    name: My App
    version: 1.0.0
  server:
    host: localhost
    port: 8080
YAML

yaml_config = YAML.load(yaml_content)

# Convert to PeanutConfig
config = PeanutConfig.new

# Save as .peanuts
peanuts_content = <<~CONFIG
  [app]
  name: "#{yaml_config['app']['name']}"
  version: "#{yaml_config['app']['version']}"

  [server]
  host: "#{yaml_config['server']['host']}"
  port: #{yaml_config['server']['port']}
CONFIG

File.write('peanu.peanuts', peanuts_content)

# Load with PeanutConfig
config_data = config.load('.')
```

### From .env

```ruby
# Old .env file
env_content = <<~ENV
  APP_NAME=My App
  APP_VERSION=1.0.0
  SERVER_HOST=localhost
  SERVER_PORT=8080
ENV

# Convert to PeanutConfig
config = PeanutConfig.new

# Parse .env and convert
env_vars = {}
env_content.lines.each do |line|
  next if line.strip.empty? || line.start_with?('#')
  key, value = line.strip.split('=', 2)
  env_vars[key] = value
end

# Save as .peanuts
peanuts_content = <<~CONFIG
  [app]
  name: "#{env_vars['APP_NAME']}"
  version: "#{env_vars['APP_VERSION']}"

  [server]
  host: "#{env_vars['SERVER_HOST']}"
  port: #{env_vars['SERVER_PORT']}
CONFIG

File.write('peanu.peanuts', peanuts_content)

# Load with PeanutConfig
config_data = config.load('.')
```

## Complete Examples

### Web Application Configuration

**File Structure:**
```
my_app/
├── config/
│   ├── peanu.peanuts
│   ├── development.peanuts
│   ├── production.peanuts
│   └── database.peanuts
├── lib/
│   └── peanut_config.rb
├── app.rb
└── Gemfile
```

**config/peanu.peanuts:**
```ini
[app]
name: "My Web App"
version: "1.0.0"
environment: "development"

[server]
host: "localhost"
port: 8080
timeout: 30

[features]
debug: true
logging: true
cache: false
```

**config/development.peanuts:**
```ini
[app]
environment: "development"

[server]
port: 3000

[features]
debug: true
logging: true
cache: false
```

**config/production.peanuts:**
```ini
[app]
environment: "production"

[server]
host: "0.0.0.0"
port: 80

[features]
debug: false
logging: true
cache: true
```

**lib/peanut_config.rb:**
```ruby
require 'tusk_lang'
require_relative '../lib/peanut_config'

class AppConfig
  @@instance = nil
  @@config = nil
  
  def self.instance
    @@instance ||= new
  end
  
  def self.config
    @@config ||= load_config
  end
  
  def self.get(key_path, default = nil)
    config = self.config
    keys = key_path.split('.')
    keys.inject(config) { |h, k| h&.dig(k) } || default
  end
  
  private
  
  def self.load_config
    config = PeanutConfig.new(auto_compile: true)
    
    # Load based on environment
    env = ENV['RACK_ENV'] || 'development'
    config_path = File.join('config', env)
    
    config.load(config_path)
  end
end
```

**app.rb:**
```ruby
require 'sinatra'
require_relative 'lib/peanut_config'

class MyApp < Sinatra::Base
  configure do
    # Load configuration
    app_config = AppConfig.config
    
    # Apply configuration
    set :app_name, app_config['app']['name']
    set :port, app_config['server']['port']
    set :host, app_config['server']['host']
    set :timeout, app_config['server']['timeout']
    
    # Features
    set :debug, app_config['features']['debug']
    set :logging, app_config['features']['logging']
    set :cache, app_config['features']['cache']
  end
  
  get '/' do
    "Hello from #{settings.app_name}!"
  end
  
  get '/config' do
    AppConfig.config.to_json
  end
end

MyApp.run!
```

### Microservice Configuration

**File Structure:**
```
user_service/
├── config/
│   ├── peanu.peanuts
│   ├── database.peanuts
│   └── redis.peanuts
├── lib/
│   └── config.rb
├── service.rb
└── Gemfile
```

**config/peanu.peanuts:**
```ini
[service]
name: "User Service"
version: "1.0.0"
port: 8081

[database]
adapter: "postgresql"
host: "user-db"
port: 5432
database: "users"
pool: 10

[redis]
host: "redis"
port: 6379
db: 0

[api]
rate_limit: 1000
timeout: 30
```

**lib/config.rb:**
```ruby
require 'tusk_lang'
require_relative '../lib/peanut_config'

class ServiceConfig
  def self.load
    config = PeanutConfig.new
    config.load('.')
  end
  
  def self.database_config
    config = load
    {
      adapter: config['database']['adapter'],
      host: config['database']['host'],
      port: config['database']['port'],
      database: config['database']['database'],
      pool: config['database']['pool']
    }
  end
  
  def self.redis_config
    config = load
    {
      host: config['redis']['host'],
      port: config['redis']['port'],
      db: config['redis']['db']
    }
  end
end
```

**service.rb:**
```ruby
require 'sinatra'
require 'redis'
require 'pg'
require_relative 'lib/config'

class UserService < Sinatra::Base
  configure do
    # Load configuration
    config = ServiceConfig.load
    
    # Database connection
    db_config = ServiceConfig.database_config
    @db = PG.connect(db_config)
    
    # Redis connection
    redis_config = ServiceConfig.redis_config
    @redis = Redis.new(redis_config)
    
    # Service settings
    set :port, config['service']['port']
    set :rate_limit, config['api']['rate_limit']
    set :timeout, config['api']['timeout']
  end
  
  get '/users/:id' do
    user_id = params[:id]
    
    # Check cache
    cached = @redis.get("user:#{user_id}")
    return cached if cached
    
    # Query database
    result = @db.exec("SELECT * FROM users WHERE id = $1", [user_id])
    
    if result.any?
      user = result[0]
      @redis.setex("user:#{user_id}", 300, user.to_json)
      user.to_json
    else
      status 404
      { error: 'User not found' }.to_json
    end
  end
end

UserService.run!
```

### CLI Tool Configuration

**File Structure:**
```
my_cli/
├── config/
│   └── peanu.peanuts
├── lib/
│   └── config.rb
├── cli.rb
└── Gemfile
```

**config/peanu.peanuts:**
```ini
[cli]
name: "My CLI Tool"
version: "1.0.0"
description: "A powerful command-line tool"

[commands]
default: "help"
timeout: 30

[output]
format: "json"
color: true
verbose: false

[api]
endpoint: "https://api.example.com"
key: "@env('API_KEY')"
timeout: 30
```

**lib/config.rb:**
```ruby
require 'tusk_lang'
require_relative '../lib/peanut_config'

class CLIConfig
  @@config = nil
  
  def self.load
    @@config ||= PeanutConfig.new.load('.')
  end
  
  def self.get(key_path, default = nil)
    config = load
    keys = key_path.split('.')
    keys.inject(config) { |h, k| h&.dig(k) } || default
  end
  
  def self.api_config
    {
      endpoint: get('api.endpoint'),
      key: get('api.key'),
      timeout: get('api.timeout', 30)
    }
  end
  
  def self.output_config
    {
      format: get('output.format', 'json'),
      color: get('output.color', true),
      verbose: get('output.verbose', false)
    }
  end
end
```

**cli.rb:**
```ruby
#!/usr/bin/env ruby
require 'optparse'
require 'json'
require 'net/http'
require_relative 'lib/config'

class CLI
  def self.run
    options = {}
    
    OptionParser.new do |opts|
      opts.on('--format=FORMAT') { |f| options[:format] = f }
      opts.on('--verbose') { options[:verbose] = true }
      opts.on('--help') { show_help; exit 0 }
    end.parse!
    
    # Load configuration
    config = CLIConfig.load
    api_config = CLIConfig.api_config
    output_config = CLIConfig.output_config
    
    # Override with command line options
    output_config[:format] = options[:format] if options[:format]
    output_config[:verbose] = options[:verbose] if options[:verbose]
    
    # Execute command
    command = ARGV[0] || config['commands']['default']
    
    case command
    when 'help'
      show_help
    when 'status'
      show_status(api_config, output_config)
    else
      puts "Unknown command: #{command}"
      exit 1
    end
  end
  
  def self.show_status(api_config, output_config)
    uri = URI("#{api_config[:endpoint]}/status")
    
    begin
      response = Net::HTTP.get_response(uri)
      
      result = {
        status: response.code,
        endpoint: api_config[:endpoint],
        timestamp: Time.now.iso8601
      }
      
      if output_config[:format] == 'json'
        puts result.to_json
      else
        puts "Status: #{result[:status]}"
        puts "Endpoint: #{result[:endpoint]}"
        puts "Timestamp: #{result[:timestamp]}"
      end
    rescue => e
      puts "Error: #{e.message}"
      exit 1
    end
  end
  
  def self.show_help
    config = CLIConfig.load
    puts "#{config['cli']['name']} v#{config['cli']['version']}"
    puts config['cli']['description']
    puts
    puts "Usage: #{$0} [command] [options]"
    puts
    puts "Commands:"
    puts "  help     Show this help"
    puts "  status   Show API status"
  end
end

CLI.run if __FILE__ == $0
```

## Quick Reference

### Common Operations

```ruby
# Load config
config = PeanutConfig.new
config_data = config.load('.')

# Get value
value = config.get('key.path', defaultValue, '.')

# Compile to binary
config.compile_to_binary(config_data, 'config.pnt')

# Load binary
binary_config = config.load_binary('config.pnt')

# Watch for changes
config = PeanutConfig.new(watch: true)
config_data = config.load('.')
```

### Configuration Hierarchy

```ruby
# Find hierarchy
hierarchy = config.find_config_hierarchy('.')

# Load with hierarchy
config_data = config.load('.')

# Clear cache
config.clear_cache
```

### Type System

```ruby
# Supported types
config_data = {
  'string' => 'hello',
  'number' => 42,
  'float' => 3.14,
  'boolean' => true,
  'array' => [1, 2, 3],
  'hash' => { 'key' => 'value' },
  'null' => nil
}
```

### Performance Tips

```ruby
# Production setup
config = PeanutConfig.new(
  auto_compile: true,
  cache_enabled: true,
  watch: false
)

# Development setup
config = PeanutConfig.new(
  auto_compile: true,
  cache_enabled: true,
  watch: true
)
```

### Error Handling

```ruby
begin
  config = PeanutConfig.new
  config_data = config.load('.')
rescue => e
  puts "Configuration error: #{e.message}"
  # Use defaults or exit
end
```

This comprehensive guide provides everything you need to use Peanut Binary Configuration with Ruby effectively! 
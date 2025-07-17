# TuskLang Ruby SDK

The official TuskLang Configuration SDK for Ruby with full support for parsing, generating, and executing TSK files with FUJSEN (function serialization). Built for Rails applications, Jekyll static sites, and DevOps automation.

## 🚀 Features

- **Parse TSK Files**: Read and parse TOML-like TSK format
- **Generate TSK**: Create TSK files programmatically
- **FUJSEN Support**: Store and execute JavaScript functions within TSK files
- **@ Operator System**: Full FUJSEN intelligence operators
- **Rails Integration**: Application configuration and settings
- **Jekyll Support**: Static site generation
- **DevOps Automation**: Scriptable configuration with logic
- **Smart Contracts**: Perfect for blockchain and distributed applications
- **Complete CLI**: Universal command-line interface with all TuskLang commands

## 📦 Installation

### RubyGems
```bash
# Add to your Gemfile
gem 'tusk_lang'

# Or install directly
gem install tusk_lang
```

### Rails Integration
```ruby
# Gemfile
gem 'tusk_lang'

# config/application.rb
require 'tusk_lang'
```

### Jekyll Integration
```ruby
# _plugins/tusk_lang.rb
require 'tusk_lang'

# _config.tsk
[site]
title = "My Jekyll Site"
description = "Built with TuskLang"
```

## 🎯 Quick Start

### Basic Usage
```ruby
require 'tusk_lang'

# Parse TSK file
tsk = TuskLang::TSK.from_string(<<~TSK)
  [app]
  name = "My Application"
  version = "1.0.0"
  debug = true

  [config]
  port = 8080
  host = "localhost"
TSK

# Get values
app_name = tsk.get_value("app", "name")
port = tsk.get_value("config", "port")
puts "App: #{app_name}, Port: #{port}"
```

### FUJSEN Function Execution
```ruby
# TSK with FUJSEN function
tsk = TuskLang::TSK.from_string(<<~TSK)
  [calculator]
  add_fujsen = """
  function add(a, b) {
    return a + b;
  }
  """

  multiply_fujsen = """
  function multiply(a, b) {
    return a * b;
  }
  """
TSK

# Execute functions
sum = tsk.execute_fujsen("calculator", "add", 5, 3)
product = tsk.execute_fujsen("calculator", "multiply", 4, 7)
puts "Sum: #{sum}, Product: #{product}"
```

### @ Operator System
```ruby
# TSK with @ operators
tsk = TuskLang::TSK.from_string(<<~TSK)
  [api]
  endpoint = "@request('https://api.example.com/data')"
  cache_ttl = "@cache('5m', 'api_data')"
  timestamp = "@date('%Y-%m-%d %H:%M:%S')"
  user_count = "@Query('users').equalTo('status', 'active').count()"
TSK

# Execute operators
context = {
  'cache_value' => 'cached_data',
  'user_status' => 'active'
}

endpoint = tsk.execute_operators("@request('https://api.example.com/data')", context)
timestamp = tsk.execute_operators("@date('%Y-%m-%d %H:%M:%S')", context)
```

## 🖥️ Command Line Interface

The TuskLang Ruby SDK includes a complete CLI that implements the Universal CLI Command Specification.

### Installation
```bash
gem install tusk_lang
```

### Basic Usage
```bash
# Show help
tsk --help

# Show version
tsk --version

# Get help for specific command
tsk help db
```

### Database Commands
```bash
# Check database connection status
tsk db status

# Run migration file
tsk db migrate schema.sql

# Open interactive database console
tsk db console

# Backup database
tsk db backup [file]

# Restore from backup file
tsk db restore backup.sql

# Initialize SQLite database
tsk db init
```

### Development Commands
```bash
# Start development server (default: 8080)
tsk serve [port]

# Compile .tsk file to optimized format
tsk compile config.tsk

# Optimize .tsk file for production
tsk optimize config.tsk
```

### Testing Commands
```bash
# Run all test suites
tsk test all

# Run specific test suite
tsk test parser
tsk test fujsen
tsk test sdk
tsk test performance
```

### Service Commands
```bash
# Start all TuskLang services
tsk services start

# Stop all TuskLang services
tsk services stop

# Restart all services
tsk services restart

# Show status of all services
tsk services status
```

### Cache Commands
```bash
# Clear all caches
tsk cache clear

# Show cache status and statistics
tsk cache status

# Pre-warm caches
tsk cache warm

# Memcached operations
tsk cache memcached status
tsk cache memcached stats
tsk cache memcached flush
tsk cache memcached restart
tsk cache memcached test

# Show distributed cache status
tsk cache distributed
```

### Configuration Commands
```bash
# Get configuration value by path
tsk config get server.port

# Check configuration hierarchy
tsk config check [path]

# Validate entire configuration chain
tsk config validate [path]

# Auto-compile all peanu.tsk files
tsk config compile [path]

# Generate configuration documentation
tsk config docs [path]

# Clear configuration cache
tsk config clear-cache [path]

# Show configuration performance statistics
tsk config stats
```

### Binary Performance Commands
```bash
# Compile to binary format (.tskb)
tsk binary compile app.tsk

# Execute binary file directly
tsk binary execute app.tskb

# Compare binary vs text performance
tsk binary benchmark app.tsk

# Optimize binary for production
tsk binary optimize app.tsk
```

### Peanuts Commands
```bash
# Compile .peanuts to binary .pnt
tsk peanuts compile config.peanuts

# Auto-compile all peanuts files in directory
tsk peanuts auto-compile [dir]

# Load and display binary peanuts file
tsk peanuts load config.pnt
```

### CSS Commands
```bash
# Expand CSS shortcodes in file
tsk css expand input.css [output.css]

# Show all shortcode → property mappings
tsk css map
```

### AI Commands
```bash
# Query Claude AI with prompt
tsk ai claude "Explain TuskLang"

# Query ChatGPT with prompt
tsk ai chatgpt "How to use FUJSEN?"

# Query custom AI API endpoint
tsk ai custom https://api.example.com "Hello"

# Show current AI configuration
tsk ai config

# Interactive AI API key setup
tsk ai setup

# Test all configured AI connections
tsk ai test

# Get AI-powered auto-completion
tsk ai complete file.tsk [line] [column]

# Analyze file for errors and improvements
tsk ai analyze file.tsk

# Get performance optimization suggestions
tsk ai optimize file.tsk

# Scan for security vulnerabilities
tsk ai security file.tsk
```

### Utility Commands
```bash
# Parse and display TSK file contents
tsk parse config.tsk

# Validate TSK file syntax
tsk validate config.tsk

# Convert between formats
tsk convert -i input.tsk -o output.tsk

# Get specific value by key path
tsk get config.tsk app.name

# Set value by key path
tsk set config.tsk app.name "New Name"

# Show version information
tsk version

# Show help for command
tsk help [command]
```

### Global Options
```bash
# Enable verbose output
tsk --verbose parse config.tsk

# Suppress non-error output
tsk --quiet parse config.tsk

# Output in JSON format
tsk --json parse config.tsk

# Use alternate config file
tsk --config custom.tsk config get server.port
```

## 🚂 Rails Integration

### Application Configuration
```ruby
# config/application.rb
require 'tusk_lang'

class Application < Rails::Application
  # Load TSK configuration
  config_tsk = TuskLang::TSK.from_file(Rails.root.join('config', 'app.tsk'))
  
  # Apply settings
  config.app_name = config_tsk.get_value("app", "name")
  config.debug_mode = config_tsk.get_value("app", "debug")
  config.api_key = config_tsk.get_value("api", "key")
end
```

### Dynamic Configuration
```ruby
# config/app.tsk
[app]
name = "My Rails App"
debug = true

[api]
key = "@env('API_KEY')"
endpoint = "https://api.example.com"

[processing]
transform_fujsen = """
function transform(data) {
  return {
    processed: true,
    timestamp: new Date().toISOString(),
    data: data.map(item => ({
      id: item.id,
      value: item.value * 2,
      status: 'processed'
    }))
  };
}
"""
```

### Controller Usage
```ruby
# app/controllers/api_controller.rb
class ApiController < ApplicationController
  def process_data
    config = TuskLang::TSK.from_file(Rails.root.join('config', 'app.tsk'))
    
    # Process with FUJSEN
    result = config.execute_fujsen("processing", "transform", params[:data])
    
    render json: result
  end
end
```

## 📝 Jekyll Integration

### Site Configuration
```ruby
# _config.tsk
[site]
title = "My Jekyll Blog"
description = "Built with Jekyll and TuskLang"
url = "https://example.com"
author = "John Doe"

[build]
destination = "_site"
plugins = ["jekyll-feed", "jekyll-seo-tag"]

[theme]
name = "minima"
```

### Jekyll Plugin
```ruby
# _plugins/tusk_lang.rb
require 'tusk_lang'

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

### Dynamic Content Generation
```ruby
# _plugins/dynamic_content.rb
require 'tusk_lang'

module Jekyll
  class DynamicContentGenerator < Generator
    safe true
    priority :normal

    def generate(site)
      config = TuskLang::TSK.from_file(File.join(Dir.pwd, '_config.tsk'))
      
      # Generate dynamic pages
      generate_posts(site, config)
      generate_categories(site, config)
    end

    private

    def generate_posts(site, config)
      posts_data = config.execute_fujsen("content", "generate_posts")
      
      posts_data.each do |post|
        site.pages << Jekyll::Page.new(site, site.source, "_posts", "#{post['date']}-#{post['slug']}.md")
      end
    end
  end
end
```

## 🔧 DevOps Automation

### Deployment Scripts
```ruby
#!/usr/bin/env ruby
# deploy.rb

require 'tusk_lang'

# Load deployment configuration
config = TuskLang::TSK.from_file('deploy.tsk')

# Execute deployment logic
deploy_config = config.execute_fujsen("deploy", "prepare", {
  environment: ENV['RAILS_ENV'],
  branch: ENV['GIT_BRANCH']
})

puts "Deploying to #{deploy_config['target']}..."
```

### CI/CD Configuration
```ruby
# .github/workflows/deploy.tsk
[ci]
name = "Deploy Application"
on = ["push"]

[jobs]
build_fujsen = """
function build() {
  return {
    steps: [
      { name: 'Checkout', uses: 'actions/checkout@v2' },
      { name: 'Setup Ruby', uses: 'ruby/setup-ruby@v1', with: { 'ruby-version': '3.0' } },
      { name: 'Install dependencies', run: 'bundle install' },
      { name: 'Run tests', run: 'bundle exec rspec' },
      { name: 'Deploy', run: 'bundle exec cap production deploy' }
    ]
  };
}
"""

deploy_fujsen = """
function deploy(environment) {
  return {
    environment: environment,
    steps: [
      { name: 'Deploy to ' + environment, run: 'bundle exec cap ' + environment + ' deploy' }
    ]
  };
}
"""
```

### Infrastructure as Code
```ruby
# infrastructure.tsk
[aws]
region = "us-west-2"
vpc_id = "@env('VPC_ID')"

[ec2]
instance_type = "t3.micro"
ami_id = "ami-12345678"

[deploy]
deploy_fujsen = """
function deploy_infrastructure() {
  return {
    steps: [
      { action: 'create_vpc', params: { cidr: '10.0.0.0/16' } },
      { action: 'create_subnet', params: { cidr: '10.0.1.0/24' } },
      { action: 'launch_instance', params: { type: 't3.micro' } }
    ]
  };
}
"""
```

## 🔥 FUJSEN Examples

### Payment Processing Contract
```ruby
contract = TuskLang::TSK.from_string(<<~TSK)
  [contract]
  name = "PaymentProcessor"
  version = "1.0.0"

  process_fujsen = """
  function process(amount, recipient) {
    if (amount <= 0) throw new Error("Invalid amount");
    
    return {
      success: true,
      transactionId: 'tx_' + Date.now(),
      amount: amount,
      recipient: recipient,
      fee: amount * 0.01
    };
  }
  """

  validate_fujsen = """
  (amount) => amount > 0 && amount <= 1000000
  """
TSK

# Execute payment
payment = contract.execute_fujsen("contract", "process", 100.50, "alice@example.com")
is_valid = contract.execute_fujsen("contract", "validate", 500)
```

### DeFi Liquidity Pool
```ruby
pool = TuskLang::TSK.from_string(<<~TSK)
  [pool]
  token_a = "FLEX"
  token_b = "USDT"
  reserve_a = 100000
  reserve_b = 50000

  swap_fujsen = """
  function swap(amountIn, tokenIn) {
    const k = 100000 * 50000;
    const fee = amountIn * 0.003;
    const amountInWithFee = amountIn - fee;
    
    if (tokenIn === 'FLEX') {
      const amountOut = (amountInWithFee * 50000) / (100000 + amountInWithFee);
      return { 
        amountOut: amountOut,
        fee: fee,
        priceImpact: (amountIn / 100000) * 100
      };
    } else {
      const amountOut = (amountInWithFee * 100000) / (50000 + amountInWithFee);
      return { 
        amountOut: amountOut,
        fee: fee,
        priceImpact: (amountIn / 50000) * 100
      };
    }
  }
  """
TSK

# Execute swap
swap_result = pool.execute_fujsen("pool", "swap", 1000, "FLEX")
```

## 🛠️ Advanced Features

### Shell Storage (Binary Format)
```ruby
# Store data in binary format
data = "Hello, TuskLang!"
shell_data = TuskLang::ShellStorage.create_shell_data(data, "greeting")
binary = TuskLang::ShellStorage.pack(shell_data)

# Retrieve data
retrieved = TuskLang::ShellStorage.unpack(binary)
puts retrieved[:data] # "Hello, TuskLang!"

# Detect type
type = TuskLang::ShellStorage.detect_type(binary)
puts "Type: #{type}" # "text"
```

### Context Injection
```ruby
tsk = TuskLang::TSK.from_string(<<~TSK)
  [processor]
  transform_fujsen = """
  function transform(data) {
    return data.map(item => ({
      ...item,
      processed: true,
      processor: context.processor_name,
      timestamp: new Date().toISOString()
    }));
  }
  """
TSK

context = {
  'processor_name' => 'Ruby Processor v1.0',
  'environment' => 'production'
}

result = tsk.execute_fujsen_with_context("processor", "transform", context, data)
```

### @ Operator Examples
```ruby
# Database queries
query = tsk.execute_operators("@Query('users').equalTo('status', 'active').limit(10)")

# Caching
cached = tsk.execute_operators("@cache('5m', 'user_data', userData)")

# Metrics
metrics = tsk.execute_operators("@metrics('api_response_time', 150)")

# Conditional logic
result = tsk.execute_operators("@if(user.isPremium, 'premium', 'standard')")

# Date formatting
timestamp = tsk.execute_operators("@date('%Y-%m-%d %H:%M:%S')")

# Environment variables
api_key = tsk.execute_operators("@env('API_KEY')")

# FlexChain operations
balance = tsk.execute_operators("@flex('balance', '0x123...')")
transfer = tsk.execute_operators("@flex('transfer', 100, '0x123...', '0x456...')")
```

## 📚 TSK Format

TSK is a TOML-like format with enhanced features:

- **Sections**: `[section_name]`
- **Key-Value**: `key = value`
- **Types**: strings, numbers, booleans, arrays, objects, null
- **Multiline**: Triple quotes `"""`
- **Comments**: Lines starting with `#`
- **FUJSEN**: Function serialization in multiline strings
- **@ Operators**: Intelligence operators for dynamic content

## 🎯 Use Cases

1. **Configuration Files**: Human-readable app configuration
2. **Rails Applications**: Application settings and dynamic logic
3. **Jekyll Sites**: Static site configuration and content generation
4. **DevOps Scripts**: Automation and deployment configuration
5. **Smart Contracts**: Store executable code with metadata
6. **Data Exchange**: Type-safe data serialization
7. **API Definitions**: Function signatures and implementations
8. **Workflow Automation**: Scriptable configuration with logic

## 🌟 Why TuskLang Ruby?

- **Human-Readable**: Unlike JSON, designed for humans first
- **Executable**: FUJSEN makes configs programmable
- **Type-Safe**: No ambiguity in data types
- **Rails-Ready**: Perfect for Rails applications
- **Jekyll-Native**: Static site generation support
- **DevOps-Friendly**: Automation and scripting
- **Blockchain-Ready**: Perfect for smart contracts
- **Complete CLI**: Universal command-line interface
- **Simple**: Minimal syntax, maximum power

## 🔧 Requirements

- **Ruby 2.7** or higher
- **Rails 6.0** or higher (for Rails integration)
- **Jekyll 4.0** or higher (for Jekyll integration)

## 📄 License

Part of the Flexchain project - Blockchain with digital grace.

## 🚀 Getting Started

1. Add `gem 'tusk_lang'` to your Gemfile
2. Run `bundle install`
3. Start parsing and executing TSK files!
4. Explore FUJSEN functions and @ operators
5. Use the CLI for development and deployment
6. Integrate with Rails or Jekyll as needed

The Ruby SDK provides the same powerful features as the JavaScript/TypeScript, Python, and C# SDKs, with native Ruby performance and Rails/Jekyll ecosystem integration, plus a complete CLI implementation. 
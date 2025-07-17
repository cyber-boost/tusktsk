# 💎 TuskLang Ruby Installation Guide

**"We don't bow to any king" - Ruby Edition**

Welcome to the revolutionary TuskLang Ruby SDK. This guide will get you up and running with TuskLang in your Ruby environment, whether you're building a Rails application, a Sinatra API, or a standalone Ruby script.

## 🚀 Quick Installation

### Option 1: RubyGems (Recommended)
```bash
# Install the gem
gem install tusklang

# Or add to your Gemfile
gem 'tusklang', '~> 1.0'

# Then run
bundle install
```

### Option 2: One-Line Install
```bash
# Direct install script
curl -sSL https://ruby.tusklang.org | bash

# Alternative with wget
wget -qO- https://ruby.tusklang.org | bash
```

### Option 3: Manual Installation
```bash
# Clone the repository
git clone https://github.com/cyber-boost/tusktsk-ruby.git
cd tusklang-ruby

# Install dependencies
bundle install

# Build and install
gem build tusklang.gemspec
gem install tusklang-*.gem
```

## 🔧 Rails Integration

### Step 1: Add to Gemfile
```ruby
# Gemfile
source 'https://rubygems.org'

gem 'rails', '~> 7.0'
gem 'tusklang', '~> 1.0'

group :development, :test do
  gem 'rspec-rails'
  gem 'factory_bot_rails'
end
```

### Step 2: Configure Rails Application
```ruby
# config/application.rb
require 'tusklang'

module MyApp
  class Application < Rails::Application
    # Load TuskLang configuration
    config.tusk_config = TuskLang.parse_file('config/app.tsk')
    
    # Initialize TuskLang after Rails loads
    config.after_initialize do
      TuskLang.configure do |config|
        config.database_adapter = TuskLang::Adapters::SQLiteAdapter.new('db/development.sqlite3')
        config.cache = TuskLang::Cache::MemoryCache.new
        config.logger = Rails.logger
      end
    end
  end
end
```

### Step 3: Create Initializer
```ruby
# config/initializers/tusk.rb
Rails.application.config.after_initialize do
  # Configure TuskLang for Rails environment
  TuskLang.configure do |config|
    config.environment = Rails.env
    config.root_path = Rails.root
    
    # Database adapter based on environment
    case Rails.env
    when 'development'
      config.database_adapter = TuskLang::Adapters::SQLiteAdapter.new('db/development.sqlite3')
    when 'production'
      config.database_adapter = TuskLang::Adapters::PostgreSQLAdapter.new(
        host: ENV['DATABASE_HOST'],
        port: ENV['DATABASE_PORT'],
        database: ENV['DATABASE_NAME'],
        user: ENV['DATABASE_USER'],
        password: ENV['DATABASE_PASSWORD']
      )
    end
    
    # Cache configuration
    config.cache = TuskLang::Cache::RedisCache.new(
      host: ENV['REDIS_HOST'] || 'localhost',
      port: ENV['REDIS_PORT'] || 6379
    )
  end
end
```

### Step 4: Create Configuration Models
```ruby
# app/models/tusk_config.rb
class TuskConfig
  include TuskLang::Configurable
  
  attr_accessor :app_name, :version, :debug, :port
  attr_accessor :database, :server, :cache, :security
  
  def initialize
    @database = DatabaseConfig.new
    @server = ServerConfig.new
    @cache = CacheConfig.new
    @security = SecurityConfig.new
  end
end

class DatabaseConfig
  include TuskLang::Configurable
  
  attr_accessor :host, :port, :name, :user, :password, :pool_size
end

class ServerConfig
  include TuskLang::Configurable
  
  attr_accessor :host, :port, :ssl, :workers, :timeout
end

class CacheConfig
  include TuskLang::Configurable
  
  attr_accessor :driver, :host, :port, :ttl, :namespace
end

class SecurityConfig
  include TuskLang::Configurable
  
  attr_accessor :ssl_enabled, :cors_origins, :rate_limit, :encryption_key
end
```

### Step 5: Usage in Controllers
```ruby
# app/controllers/application_controller.rb
class ApplicationController < ActionController::Base
  def tusk_config
    Rails.application.config.tusk_config
  end
  
  def database_config
    tusk_config.database
  end
  
  def server_config
    tusk_config.server
  end
end

# app/controllers/api/v1/users_controller.rb
class Api::V1::UsersController < ApplicationController
  def index
    # Use TuskLang configuration
    config = tusk_config
    
    users = User.limit(config.server.workers * 100)
    
    render json: {
      users: users,
      config: {
        cache_ttl: config.cache.ttl,
        database_pool: config.database.pool_size
      }
    }
  end
end
```

## 🐳 Docker Integration

### Dockerfile
```dockerfile
FROM ruby:3.2-alpine

# Install system dependencies
RUN apk add --no-cache \
    build-base \
    postgresql-dev \
    sqlite-dev \
    redis

# Install TuskLang
RUN gem install tusklang

# Set working directory
WORKDIR /app

# Copy Gemfile
COPY Gemfile Gemfile.lock ./

# Install gems
RUN bundle install

# Copy application
COPY . .

# Expose port
EXPOSE 3000

# Start application
CMD ["rails", "server", "-b", "0.0.0.0"]
```

### Docker Compose
```yaml
# docker-compose.yml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "3000:3000"
    environment:
      - DATABASE_HOST=postgres
      - DATABASE_PORT=5432
      - DATABASE_NAME=myapp
      - DATABASE_USER=postgres
      - DATABASE_PASSWORD=secret
      - REDIS_HOST=redis
      - REDIS_PORT=6379
    depends_on:
      - postgres
      - redis
    volumes:
      - .:/app
      - bundle_cache:/usr/local/bundle

  postgres:
    image: postgres:15-alpine
    environment:
      - POSTGRES_DB=myapp
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=secret
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
  bundle_cache:
```

## 🔍 Verification

### Test Installation
```ruby
# test_tusk.rb
require 'tusklang'

# Test basic functionality
parser = TuskLang.new

tsk_content = <<~TSK
  $app_name: "TuskLang Ruby Test"
  $version: "1.0.0"
  
  [database]
  host: "localhost"
  port: 5432
  
  [server]
  host: "0.0.0.0"
  port: 8080
TSK

begin
  data = parser.parse(tsk_content)
  puts "✅ TuskLang installation successful!"
  puts "App name: #{data['app_name']}"
  puts "Database host: #{data['database']['host']}"
  puts "Server port: #{data['server']['port']}"
rescue => e
  puts "❌ TuskLang installation failed: #{e.message}"
  exit 1
end
```

### Rails Verification
```ruby
# lib/tasks/tusk.rake
namespace :tusk do
  desc "Verify TuskLang installation"
  task verify: :environment do
    puts "Verifying TuskLang installation..."
    
    begin
      config = Rails.application.config.tusk_config
      puts "✅ TuskLang configuration loaded successfully"
      puts "App name: #{config.app_name}"
      puts "Environment: #{TuskLang.config.environment}"
      
      # Test database adapter
      if TuskLang.config.database_adapter
        puts "✅ Database adapter configured"
      end
      
      # Test cache
      if TuskLang.config.cache
        puts "✅ Cache configured"
      end
      
    rescue => e
      puts "❌ TuskLang verification failed: #{e.message}"
      exit 1
    end
  end
  
  desc "Test TuskLang parsing"
  task test_parse: :environment do
    puts "Testing TuskLang parsing..."
    
    test_file = Rails.root.join('config', 'test.tsk')
    File.write(test_file, <<~TSK)
      $test: "Hello TuskLang!"
      [test_section]
      value: 42
      enabled: true
    TSK
    
    begin
      data = TuskLang.parse_file(test_file)
      puts "✅ Parsing successful"
      puts "Test value: #{data['test']}"
      puts "Section value: #{data['test_section']['value']}"
    rescue => e
      puts "❌ Parsing failed: #{e.message}"
    ensure
      File.delete(test_file) if File.exist?(test_file)
    end
  end
end
```

## 🔧 Configuration Files

### Basic Configuration
```ruby
# config/app.tsk
$app_name: "My Rails App"
$version: "1.0.0"
$environment: @env("RAILS_ENV", "development")

[database]
host: @env("DATABASE_HOST", "localhost")
port: @env("DATABASE_PORT", 5432)
name: @env("DATABASE_NAME", "myapp_#{environment}")
user: @env("DATABASE_USER", "postgres")
password: @env("DATABASE_PASSWORD", "")
pool_size: @if($environment == "production", 20, 5)

[server]
host: @env("SERVER_HOST", "0.0.0.0")
port: @env("SERVER_PORT", 3000)
ssl: @if($environment == "production", true, false)
workers: @if($environment == "production", 4, 1)
timeout: 30

[cache]
driver: @env("CACHE_DRIVER", "redis")
host: @env("REDIS_HOST", "localhost")
port: @env("REDIS_PORT", 6379)
ttl: "5m"
namespace: "#{app_name}_#{environment}"

[security]
ssl_enabled: @if($environment == "production", true, false)
cors_origins: @if($environment == "production", ["https://myapp.com"], ["*"])
rate_limit: @if($environment == "production", 100, 1000)
encryption_key: @env("ENCRYPTION_KEY", "")
```

### Environment-Specific Configurations
```ruby
# config/environments/development.tsk
$environment: "development"

[logging]
level: "debug"
format: "text"
file: "console"

[debug]
enabled: true
profiling: true
sql_logging: true

[development]
reload_on_change: true
hot_reload: true
error_pages: true
```

```ruby
# config/environments/production.tsk
$environment: "production"

[logging]
level: "error"
format: "json"
file: "/var/log/rails.log"

[performance]
compression: true
caching: true
cdn_enabled: true

[monitoring]
metrics_enabled: true
health_checks: true
error_tracking: true
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Gem Installation Fails
```bash
# Clear gem cache
gem cleanup
gem install tusklang --force

# Check Ruby version
ruby --version  # Should be 2.7+

# Install build tools
sudo apt-get install build-essential  # Ubuntu/Debian
sudo yum groupinstall "Development Tools"  # CentOS/RHEL
```

#### 2. Rails Integration Issues
```ruby
# Check if TuskLang is loaded
Rails.application.config.tusk_config

# Reload configuration
Rails.application.config.reload_configuration

# Check logs
tail -f log/development.log
```

#### 3. Database Connection Issues
```ruby
# Test database adapter
adapter = TuskLang.config.database_adapter
adapter.test_connection

# Check connection pool
adapter.pool_size
adapter.active_connections
```

#### 4. Cache Issues
```ruby
# Test cache
cache = TuskLang.config.cache
cache.set('test', 'value')
value = cache.get('test')
puts "Cache test: #{value}"
```

## 📚 Next Steps

Now that you have TuskLang installed in your Ruby environment, you can:

1. **Read the Quick Start Guide** - Learn basic syntax and concepts
2. **Explore Database Integration** - Connect to your databases
3. **Master @ Operators** - Use powerful built-in functions
4. **Build Advanced Configurations** - Create complex, dynamic configs
5. **Deploy to Production** - Scale your applications

## 🎯 Why TuskLang for Ruby?

- **Rails Integration**: Seamless integration with Rails applications
- **Database Queries in Config**: Direct database access in configuration files
- **Multiple Syntax Styles**: Support for `[]`, `{}`, and `<>` syntax
- **@ Operator System**: Powerful built-in functions for dynamic configuration
- **Cross-File Communication**: Link and reference multiple configuration files
- **Security Features**: Built-in validation, encryption, and security
- **Performance**: Optimized parsing and caching for production use

**Ready to revolutionize your Ruby configuration? Let's Tusk! 🚀** 
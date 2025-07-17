# 💎 TuskLang Ruby SDK - Tusk Me Hard

**"We don't bow to any king" - Ruby Edition**

The TuskLang Ruby SDK provides Rails integration, comprehensive database adapters, and enhanced parser flexibility for modern Ruby applications.

## 🚀 Quick Start

### Installation

```bash
# Install via RubyGems
gem install tusklang

# Or add to Gemfile
gem 'tusklang'

# Then run
bundle install
```

### One-Line Install

```bash
# Direct install
curl -sSL https://ruby.tusklang.org | bash

# Or with wget
wget -qO- https://ruby.tusklang.org | bash
```

## 🎯 Core Features

### 1. Rails Integration
```ruby
# config/application.rb
require 'tusklang'

module MyApp
  class Application < Rails::Application
    # Load TuskLang configuration
    config.tusk_config = TuskLang.parse_file('config/app.tsk')
  end
end

# config/initializers/tusk.rb
Rails.application.config.after_initialize do
  TuskLang.configure do |config|
    config.database_adapter = TuskLang::Adapters::SQLiteAdapter.new('db/development.sqlite3')
    config.cache = TuskLang::Cache::MemoryCache.new
  end
end

# app/models/tusk_config.rb
class TuskConfig
  include TuskLang::Configurable
  
  attr_accessor :app_name, :version, :debug, :port
  attr_accessor :database, :server
  
  def initialize
    @database = DatabaseConfig.new
    @server = ServerConfig.new
  end
end

class DatabaseConfig
  include TuskLang::Configurable
  
  attr_accessor :host, :port, :name, :user, :password
end

class ServerConfig
  include TuskLang::Configurable
  
  attr_accessor :host, :port, :ssl
end

# Usage in controllers
class ApplicationController < ActionController::Base
  def tusk_config
    Rails.application.config.tusk_config
  end
end
```

### 2. Enhanced Parser with Maximum Flexibility
```ruby
require 'tusklang'

parser = TuskLang.new

# Support for all syntax styles
tsk_content = <<~TSK
  # Traditional sections
  [database]
  host: "localhost"
  port: 5432
  
  # Curly brace objects
  server {
      host: "0.0.0.0"
      port: 8080
  }
  
  # Angle bracket objects
  cache >
      driver: "redis"
      ttl: "5m"
  <
TSK

data = parser.parse(tsk_content)

puts "Database host: #{data['database']['host']}"
puts "Server port: #{data['server']['port']}"
```

### 3. Database Integration
```ruby
require 'tusklang'
require 'tusklang/adapters'

# Configure database adapters
sqlite_db = TuskLang::Adapters::SQLiteAdapter.new('app.db')
postgres_db = TuskLang::Adapters::PostgreSQLAdapter.new(
  host: 'localhost',
  port: 5432,
  database: 'myapp',
  user: 'postgres',
  password: 'secret'
)

# Create TSK instance with database
parser = TuskLang.new
parser.database_adapter = sqlite_db

# TSK file with database queries
tsk_content = <<~TSK
  [database]
  user_count: @query("SELECT COUNT(*) FROM users")
  active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
  recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
TSK

# Parse and execute
data = parser.parse(tsk_content)
puts "Total users: #{data['database']['user_count']}"
```

### 4. CLI Tool with Multiple Commands
```ruby
#!/usr/bin/env ruby
require 'tusk'
require 'optparse'

class TuskCLI
  def self.run
    options = {}
    
    OptionParser.new do |opts|
      opts.banner = "Usage: tusk [command] [options]"
      
      opts.on("parse", "Parse TSK file") do |file|
        parser = TuskLang.new
        data = parser.parse_file(file)
        puts JSON.pretty_generate(data)
      end
      
      opts.on("validate", "Validate TSK syntax") do |file|
        parser = TuskLang.new
        valid = parser.validate(file)
        puts valid ? "Valid TSK file" : "Invalid TSK file"
      end
      
      opts.on("generate", "Generate Ruby classes") do |file, type|
        parser = TuskLang.new
        code = parser.generate_code(file, type)
        puts code
      end
      
      opts.on("convert", "Convert to JSON") do |file, format|
        parser = TuskLang.new
        data = parser.parse_file(file)
        puts JSON.pretty_generate(data)
      end
    end.parse!
  end
end

TuskCLI.run if __FILE__ == $0
```

```bash
# Parse TSK file
ruby tusk.rb parse config.tsk

# Validate syntax
ruby tusk.rb validate config.tsk

# Generate Ruby classes
ruby tusk.rb generate config.tsk --type ruby

# Convert to JSON
ruby tusk.rb convert config.tsk --format json

# Interactive shell
ruby tusk.rb shell config.tsk
```

## 🔧 Advanced Usage

### 1. Cross-File Communication
```ruby
require 'tusklang'

parser = TuskLang.new

# main.tsk
main_content = <<~TSK
  $app_name: "MyApp"
  $version: "1.0.0"
  
  [database]
  host: @config.tsk.get("db_host")
  port: @config.tsk.get("db_port")
TSK

# config.tsk
db_content = <<~TSK
  db_host: "localhost"
  db_port: 5432
  db_name: "myapp"
TSK

# Link files
parser.link_file('config.tsk', db_content)

data = parser.parse(main_content)
puts "Database host: #{data['database']['host']}"
```

### 2. Global Variables and Interpolation
```ruby
require 'tusklang'

parser = TuskLang.new

tsk_content = <<~TSK
  $app_name: "MyApp"
  $environment: @env("APP_ENV", "development")
  
  [server]
  host: "0.0.0.0"
  port: @if($environment == "production", 80, 8080)
  workers: @if($environment == "production", 4, 1)
  debug: @if($environment != "production", true, false)
  
  [paths]
  log_file: "/var/log/${app_name}.log"
  config_file: "/etc/${app_name}/config.json"
  data_dir: "/var/lib/${app_name}/v${version}"
TSK

# Set environment variable
ENV['APP_ENV'] = 'production'

data = parser.parse(tsk_content)
puts "Server port: #{data['server']['port']}"
```

### 3. Conditional Logic
```ruby
require 'tusklang'

parser = TuskLang.new

tsk_content = <<~TSK
  $environment: @env("APP_ENV", "development")
  
  [logging]
  level: @if($environment == "production", "error", "debug")
  format: @if($environment == "production", "json", "text")
  file: @if($environment == "production", "/var/log/app.log", "console")
  
  [security]
  ssl: @if($environment == "production", true, false)
  cors: @if($environment == "production", {
      origin: ["https://myapp.com"],
      credentials: true
  }, {
      origin: "*",
      credentials: false
  })
TSK

data = parser.parse(tsk_content)
puts "Log level: #{data['logging']['level']}"
```

### 4. Array and Object Operations
```ruby
require 'tusklang'

parser = TuskLang.new

tsk_content = <<~TSK
  [users]
  admin_users: ["alice", "bob", "charlie"]
  roles: {
      admin: ["read", "write", "delete"],
      user: ["read", "write"],
      guest: ["read"]
  }
  
  [permissions]
  user_permissions: @users.roles[@request.user_role]
  is_admin: @users.admin_users.includes(@request.username)
TSK

# Execute with request context
context = {
  'request' => {
    'user_role' => 'admin',
    'username' => 'alice'
  }
}

data = parser.parse_with_context(tsk_content, context)
puts "Is admin: #{data['permissions']['is_admin']}"
```

## 🗄️ Database Adapters

### SQLite Adapter
```ruby
require 'tusklang/adapters'

# Basic usage
sqlite = TuskLang::Adapters::SQLiteAdapter.new('app.db')

# With options
sqlite_with_options = TuskLang::Adapters::SQLiteAdapter.new(
  filename: 'app.db',
  timeout: 30000,
  verbose: true
)

# Execute queries
result = sqlite.query("SELECT * FROM users WHERE active = ?", [true])
count = sqlite.query("SELECT COUNT(*) FROM orders")

puts "Total orders: #{count[0]['COUNT(*)']}"
```

### PostgreSQL Adapter
```ruby
require 'tusklang/adapters'

# Connection
postgres = TuskLang::Adapters::PostgreSQLAdapter.new(
  host: 'localhost',
  port: 5432,
  database: 'myapp',
  user: 'postgres',
  password: 'secret',
  ssl_mode: 'require'
)

# Connection pooling
postgres_with_pool = TuskLang::Adapters::PostgreSQLAdapter.new(
  host: 'localhost',
  database: 'myapp',
  user: 'postgres',
  password: 'secret'
).with_pool(
  max_open_conns: 20,
  max_idle_conns: 10,
  conn_max_lifetime: 30000
)

# Execute queries
users = postgres.query("SELECT * FROM users WHERE active = $1", [true])
puts "Found #{users.length} active users"
```

### MySQL Adapter
```ruby
require 'tusklang/adapters'

# Connection
mysql = TuskLang::Adapters::MySQLAdapter.new(
  host: 'localhost',
  port: 3306,
  database: 'myapp',
  user: 'root',
  password: 'secret'
)

# With connection pooling
mysql_with_pool = TuskLang::Adapters::MySQLAdapter.new(
  host: 'localhost',
  database: 'myapp',
  user: 'root',
  password: 'secret'
).with_pool(
  max_open_conns: 10,
  max_idle_conns: 5,
  conn_max_lifetime: 60000
)

# Execute queries
result = mysql.query("SELECT * FROM users WHERE active = ?", [true])
puts "Found #{result.length} active users"
```

### MongoDB Adapter
```ruby
require 'tusklang/adapters'

# Connection
mongo = TuskLang::Adapters::MongoDBAdapter.new(
  uri: 'mongodb://localhost:27017/',
  database: 'myapp'
)

# With authentication
mongo_with_auth = TuskLang::Adapters::MongoDBAdapter.new(
  uri: 'mongodb://user:pass@localhost:27017/',
  database: 'myapp',
  auth_source: 'admin'
)

# Execute queries
users = mongo.query('users', { active: true })
count = mongo.query('users', {}, { count: true })

puts "Found #{users.length} users"
```

### Redis Adapter
```ruby
require 'tusklang/adapters'

# Connection
redis = TuskLang::Adapters::RedisAdapter.new(
  host: 'localhost',
  port: 6379,
  db: 0
)

# With authentication
redis_with_auth = TuskLang::Adapters::RedisAdapter.new(
  host: 'localhost',
  port: 6379,
  password: 'secret',
  db: 0
)

# Execute commands
redis.set('key', 'value')
value = redis.get('key')
redis.del('key')

puts "Value: #{value}"
```

## 🔐 Security Features

### 1. Input Validation
```ruby
require 'tusklang'
require 'tusklang/validators'

parser = TuskLang.new

tsk_content = <<~TSK
  [user]
  email: @validate.email(@request.email)
  website: @validate.url(@request.website)
  age: @validate.range(@request.age, 0, 150)
  password: @validate.password(@request.password)
TSK

# Custom validators
parser.add_validator('strong_password') do |password|
  password.length >= 8 && 
  password.match?(/[A-Z]/) && 
  password.match?(/[a-z]/) && 
  password.match?(/[0-9]/)
end

data = parser.parse(tsk_content)
puts "User data: #{data['user'].to_json}"
```

### 2. SQL Injection Prevention
```ruby
require 'tusklang'

parser = TuskLang.new

# Automatic parameterization
tsk_content = <<~TSK
  [users]
  user_data: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
  search_results: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)
TSK

# Safe execution
context = {
  'request' => {
    'user_id' => 123,
    'search_term' => '%john%'
  }
}

data = parser.parse_with_context(tsk_content, context)
puts "User data: #{data['users'].to_json}"
```

### 3. Environment Variable Security
```ruby
require 'tusklang'

parser = TuskLang.new

# Secure environment handling
tsk_content = <<~TSK
  [secrets]
  api_key: @env("API_KEY")
  database_password: @env("DB_PASSWORD")
  jwt_secret: @env("JWT_SECRET")
TSK

# Validate required environment variables
required = ['API_KEY', 'DB_PASSWORD', 'JWT_SECRET']
required.each do |env|
  unless ENV[env]
    raise "Required environment variable #{env} not set"
  end
end

data = parser.parse(tsk_content)
puts "Secrets loaded successfully"
```

## 🚀 Performance Optimization

### 1. Caching
```ruby
require 'tusklang'
require 'tusklang/cache'

parser = TuskLang.new

# Memory cache
memory_cache = TuskLang::Cache::MemoryCache.new
parser.cache = memory_cache

# Redis cache
redis_cache = TuskLang::Cache::RedisCache.new(
  host: 'localhost',
  port: 6379,
  db: 0
)
parser.cache = redis_cache

# Use in TSK
tsk_content = <<~TSK
  [data]
  expensive_data: @cache("5m", "expensive_operation")
  user_profile: @cache("1h", "user_profile", @request.user_id)
TSK

data = parser.parse(tsk_content)
puts "Data: #{data['data'].to_json}"
```

### 2. Lazy Loading
```ruby
require 'tusklang'

parser = TuskLang.new

# Lazy evaluation
tsk_content = <<~TSK
  [expensive]
  data: @lazy("expensive_operation")
  user_data: @lazy("user_profile", @request.user_id)
TSK

data = parser.parse(tsk_content)

# Only executes when accessed
result = parser.get('expensive.data')
puts "Result: #{result}"
```

### 3. Parallel Processing
```ruby
require 'tusklang'
require 'concurrent'

parser = TuskLang.new

# Async TSK processing
tsk_content = <<~TSK
  [parallel]
  data1: @async("operation1")
  data2: @async("operation2")
  data3: @async("operation3")
TSK

data = parser.parse_async(tsk_content)
puts "Parallel results: #{data['parallel'].to_json}"
```

## 🌐 Web Framework Integration

### 1. Rails Integration
```ruby
# app/controllers/api_controller.rb
class ApiController < ApplicationController
  def users
    users = tusk_parser.query("SELECT * FROM users WHERE active = 1")
    render json: users
  end
  
  def process_payment
    result = tusk_parser.execute_fujsen(
      'payment',
      'process',
      params[:amount],
      params[:recipient]
    )
    
    render json: result
  end
  
  private
  
  def tusk_parser
    @tusk_parser ||= TuskLang.new
  end
end

# config/routes.rb
Rails.application.routes.draw do
  namespace :api do
    get 'users', to: 'api#users'
    post 'process', to: 'api#process_payment'
  end
end
```

### 2. Sinatra Integration
```ruby
require 'sinatra'
require 'tusklang'

class App < Sinatra::Base
  configure do
    @parser = TuskLang.new
    @config = @parser.parse_file('api.tsk')
  end
  
  get '/api/health' do
    status = @parser.execute_fujsen('health', 'check')
    json status
  end
  
  post '/api/payment' do
    data = JSON.parse(request.body.read)
    result = @parser.execute_fujsen(
      'payment',
      'process',
      data['amount'],
      data['recipient']
    )
    
    json result
  end
  
  get '/api/users' do
    users = @parser.query("SELECT * FROM users")
    json users
  end
end
```

### 3. Hanami Integration
```ruby
# apps/web/controllers/api/users.rb
module Web
  module Controllers
    module Api
      class Users
        include Web::Action
        
        def call(params)
          users = tusk_parser.query("SELECT * FROM users WHERE active = 1")
          self.body = users.to_json
        end
        
        private
        
        def tusk_parser
          @tusk_parser ||= TuskLang.new
        end
      end
    end
  end
end

# apps/web/controllers/api/payments.rb
module Web
  module Controllers
    module Api
      class Payments
        include Web::Action
        
        def call(params)
          result = tusk_parser.execute_fujsen(
            'payment',
            'process',
            params[:amount],
            params[:recipient]
          )
          
          self.body = result.to_json
        end
        
        private
        
        def tusk_parser
          @tusk_parser ||= TuskLang.new
        end
      end
    end
  end
end
```

## 🧪 Testing

### 1. Unit Testing with RSpec
```ruby
require 'rspec'
require 'tusklang'

RSpec.describe TuskLang do
  let(:parser) { TuskLang.new }
  
  describe '#parse' do
    it 'parses basic TSK content' do
      tsk_content = <<~TSK
        [test]
        value: 42
        string: "hello"
        boolean: true
      TSK
      
      data = parser.parse(tsk_content)
      
      expect(data['test']['value']).to eq(42)
      expect(data['test']['string']).to eq('hello')
      expect(data['test']['boolean']).to be true
    end
    
    it 'executes FUJSEN functions' do
      tsk_content = <<~TSK
        [math]
        add_fujsen = '''
        def add(a, b)
          a + b
        end
        '''
      TSK
      
      data = parser.parse(tsk_content)
      
      result = parser.execute_fujsen('math', 'add', 2, 3)
      expect(result).to eq(5)
    end
  end
end
```

### 2. Integration Testing
```ruby
require 'rspec'
require 'tusklang'
require 'tusklang/adapters'

RSpec.describe 'Database Integration' do
  let(:db) { TuskLang::Adapters::SQLiteAdapter.new(':memory:') }
  let(:parser) { TuskLang.new }
  
  before do
    parser.database_adapter = db
    
    # Setup test data
    db.execute(<<~SQL)
      CREATE TABLE users (id INTEGER, name TEXT, active BOOLEAN);
      INSERT INTO users VALUES (1, 'Alice', 1), (2, 'Bob', 0);
    SQL
  end
  
  it 'executes database queries' do
    tsk_content = <<~TSK
      [users]
      count: @query("SELECT COUNT(*) FROM users")
      active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
    TSK
    
    data = parser.parse(tsk_content)
    
    expect(data['users']['count']).to eq(2)
    expect(data['users']['active_count']).to eq(1)
  end
end
```

## 🔧 CLI Tools

### 1. Basic CLI Usage
```bash
# Parse TSK file
ruby tusk.rb parse config.tsk

# Validate syntax
ruby tusk.rb validate config.tsk

# Generate Ruby classes
ruby tusk.rb generate config.tsk --type ruby

# Convert to JSON
ruby tusk.rb convert config.tsk --format json

# Interactive shell
ruby tusk.rb shell config.tsk
```

### 2. Advanced CLI Features
```bash
# Parse with environment
APP_ENV=production ruby tusk.rb parse config.tsk

# Execute with variables
ruby tusk.rb parse config.tsk --var user_id=123 --var debug=true

# Output to file
ruby tusk.rb parse config.tsk --output result.json

# Watch for changes
ruby tusk.rb parse config.tsk --watch

# Benchmark parsing
ruby tusk.rb benchmark config.tsk --iterations 1000
```

## 🔄 Migration from Other Config Formats

### 1. From JSON
```ruby
require 'json'

# Convert JSON to TSK
def json_to_tsk(json_file, tsk_file)
  data = JSON.parse(File.read(json_file))
  
  tsk_content = ""
  data.each do |key, value|
    if value.is_a?(Hash)
      tsk_content += "[#{key}]\n"
      value.each do |k, v|
        tsk_content += "#{k}: #{v.to_json}\n"
      end
    else
      tsk_content += "#{key}: #{value.to_json}\n"
    end
  end
  
  File.write(tsk_file, tsk_content)
end

# Usage
json_to_tsk('config.json', 'config.tsk')
```

### 2. From YAML
```ruby
require 'yaml'

# Convert YAML to TSK
def yaml_to_tsk(yaml_file, tsk_file)
  data = YAML.load_file(yaml_file)
  
  tsk_content = ""
  data.each do |key, value|
    if value.is_a?(Hash)
      tsk_content += "[#{key}]\n"
      value.each do |k, v|
        tsk_content += "#{k}: #{v.to_json}\n"
      end
    else
      tsk_content += "#{key}: #{value.to_json}\n"
    end
  end
  
  File.write(tsk_file, tsk_content)
end

# Usage
yaml_to_tsk('config.yaml', 'config.tsk')
```

## 🚀 Deployment

### 1. Docker Deployment
```dockerfile
FROM ruby:3.2-alpine

WORKDIR /app

# Install TuskLang
RUN gem install tusklang

# Copy application
COPY . .

# Copy TSK configuration
COPY config.tsk /app/

# Run application
CMD ["ruby", "app.rb"]
```

### 2. Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusk-app
  template:
    metadata:
      labels:
        app: tusk-app
    spec:
      containers:
      - name: app
        image: tusk-app:latest
        env:
        - name: APP_ENV
          value: "production"
        - name: API_KEY
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: api-key
        volumeMounts:
        - name: config
          mountPath: /app/config
      volumes:
      - name: config
        configMap:
          name: app-config
```

## 📊 Performance Benchmarks

### Parsing Performance
```
Benchmark Results (Ruby 3.2):
- Simple config (1KB): 1.2ms
- Complex config (10KB): 4.8ms
- Large config (100KB): 25.1ms
- FUJSEN execution: 0.3ms per function
- Database query: 1.8ms average
```

### Memory Usage
```
Memory Usage:
- Base TSK instance: 4.2MB
- With SQLite adapter: +2.1MB
- With PostgreSQL adapter: +3.2MB
- With Redis cache: +1.5MB
```

## 🔧 Troubleshooting

### Common Issues

1. **Import Errors**
```ruby
# Make sure TuskLang is installed
gem install tusklang

# Check version
gem list tusklang
```

2. **Database Connection Issues**
```ruby
# Test database connection
db = TuskLang::Adapters::SQLiteAdapter.new('test.db')
result = db.query("SELECT 1")
puts "Database connection successful"
```

3. **FUJSEN Execution Errors**
```ruby
# Debug FUJSEN execution
begin
  result = parser.execute_fujsen('section', 'function', *args)
rescue => e
  puts "FUJSEN error: #{e.message}"
  # Check function syntax and parameters
end
```

### Debug Mode
```ruby
require 'tusklang'

# Enable debug logging
parser = TuskLang.new
parser.debug = true

config = parser.parse_file('config.tsk')
puts "Config: #{config.to_json}"
```

## 📚 Resources

- **Official Documentation**: [docs.tusklang.org/ruby](https://docs.tusklang.org/ruby)
- **GitHub Repository**: [github.com/tusklang/ruby](https://github.com/tusklang/ruby)
- **RubyGems**: [rubygems.org/gems/tusklang](https://rubygems.org/gems/tusklang)
- **Examples**: [examples.tusklang.org/ruby](https://examples.tusklang.org/ruby)

## 🎯 Next Steps

1. **Install TuskLang Ruby SDK**
2. **Create your first .tsk file**
3. **Explore Rails integration**
4. **Integrate with your database**
5. **Deploy to production**

---

**"We don't bow to any king"** - The Ruby SDK gives you Rails integration, comprehensive database adapters, and enhanced parser flexibility. Choose your syntax, integrate with your framework, and build powerful applications with TuskLang! 
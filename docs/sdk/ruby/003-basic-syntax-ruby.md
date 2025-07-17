# 📝 TuskLang Ruby Basic Syntax Guide

**"We don't bow to any king" - Ruby Edition**

Master the flexible syntax of TuskLang in Ruby. TuskLang supports multiple syntax styles, so you can choose what feels most natural to you.

## 🎨 Multiple Syntax Styles

TuskLang supports three main syntax styles. Choose the one that fits your preferences:

### 1. Traditional INI-Style (Sections)
```ruby
# config/app.tsk
$app_name: "MyApp"
$version: "1.0.0"

[database]
host: "localhost"
port: 5432
name: "myapp"

[server]
host: "0.0.0.0"
port: 8080
debug: true

[logging]
level: "info"
format: "json"
file: "/var/log/app.log"
```

### 2. JSON-Like Objects (Curly Braces)
```ruby
# config/app.tsk
$app_name: "MyApp"
$version: "1.0.0"

database {
    host: "localhost"
    port: 5432
    name: "myapp"
}

server {
    host: "0.0.0.0"
    port: 8080
    debug: true
}

logging {
    level: "info"
    format: "json"
    file: "/var/log/app.log"
}
```

### 3. XML-Inspired (Angle Brackets)
```ruby
# config/app.tsk
$app_name: "MyApp"
$version: "1.0.0"

database >
    host: "localhost"
    port: 5432
    name: "myapp"
<

server >
    host: "0.0.0.0"
    port: 8080
    debug: true
<

logging >
    level: "info"
    format: "json"
    file: "/var/log/app.log"
<
```

## 🔤 Data Types

### 1. Strings
```ruby
# config/strings.tsk
$app_name: "My Awesome App"
$description: "A revolutionary application"
$version: "1.0.0"

[paths]
log_file: "/var/log/myapp.log"
config_dir: "/etc/myapp"
data_path: "/var/lib/myapp/data"

[urls]
api_base: "https://api.myapp.com"
webhook_url: "https://hooks.myapp.com/webhook"
docs_url: "https://docs.myapp.com"
```

### 2. Numbers
```ruby
# config/numbers.tsk
$version: 1.0
$build_number: 42

[server]
port: 8080
workers: 4
timeout: 30
max_connections: 1000

[database]
pool_size: 20
max_retries: 3
connection_timeout: 5000

[limits]
rate_limit: 1000
file_size_limit: 10485760
session_timeout: 3600
```

### 3. Booleans
```ruby
# config/booleans.tsk
$debug: true
$production: false

[server]
ssl_enabled: true
compression: true
caching: false

[database]
ssl_required: true
connection_pooling: true
auto_migrate: false

[features]
user_registration: true
email_verification: true
social_login: false
two_factor_auth: true
```

### 4. Arrays
```ruby
# config/arrays.tsk
$environments: ["development", "staging", "production"]
$supported_languages: ["en", "es", "fr", "de"]

[server]
allowed_hosts: ["localhost", "127.0.0.1", "myapp.com"]
cors_origins: ["https://myapp.com", "https://admin.myapp.com"]

[database]
read_replicas: ["db-replica-1", "db-replica-2", "db-replica-3"]
backup_schedules: ["daily", "weekly", "monthly"]

[features]
enabled_modules: ["users", "orders", "payments", "analytics"]
disabled_features: ["beta_chat", "experimental_api"]
```

### 5. Objects/Hashes
```ruby
# config/objects.tsk
$app_info: {
    name: "MyApp"
    version: "1.0.0"
    author: "John Doe"
    license: "MIT"
}

[server]
ssl_config: {
    enabled: true
    certificate: "/etc/ssl/certs/myapp.crt"
    private_key: "/etc/ssl/private/myapp.key"
    protocols: ["TLSv1.2", "TLSv1.3"]
}

[database]
connection_pool: {
    min_size: 5
    max_size: 20
    idle_timeout: 300
    max_lifetime: 3600
}

[email]
smtp_config: {
    host: "smtp.gmail.com"
    port: 587
    username: "noreply@myapp.com"
    password: @env("SMTP_PASSWORD")
    encryption: "tls"
}
```

## 🔗 Global Variables

### 1. Basic Global Variables
```ruby
# config/globals.tsk
$app_name: "MyApp"
$version: "1.0.0"
$environment: @env("RAILS_ENV", "development")
$debug: @env("DEBUG", "false")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)

[database]
host: @env("DATABASE_HOST", "localhost")
port: @env("DATABASE_PORT", 5432)
name: "#{app_name}_#{environment}"
```

### 2. Interpolation
```ruby
# config/interpolation.tsk
$app_name: "MyApp"
$version: "1.0.0"
$environment: @env("RAILS_ENV", "development")

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"

[urls]
api_base: "https://api.${app_name}.com"
webhook_url: "https://hooks.${app_name}.com/webhook"
docs_url: "https://docs.${app_name}.com"

[files]
backup_path: "/backups/${app_name}/${environment}"
temp_dir: "/tmp/${app_name}_${version}"
```

## 🔄 Cross-File References

### 1. Basic File References
```ruby
# config/main.tsk
$app_name: "MyApp"
$version: "1.0.0"

[database]
host: @config.database.tsk.get("host")
port: @config.database.tsk.get("port")
name: @config.database.tsk.get("name")

[server]
host: @config.server.tsk.get("host")
port: @config.server.tsk.get("port")
```

```ruby
# config/database.tsk
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DATABASE_PASSWORD")
```

```ruby
# config/server.tsk
host: "0.0.0.0"
port: 8080
ssl: false
workers: 1
```

### 2. Conditional File Loading
```ruby
# config/main.tsk
$environment: @env("RAILS_ENV", "development")

# Load environment-specific config
@if($environment == "production") {
    @include("config/production.tsk")
} @else {
    @include("config/development.tsk")
}

[database]
host: @config.database.tsk.get("host")
port: @config.database.tsk.get("port")
```

## 🎯 Ruby Integration Examples

### 1. Parsing Different Syntax Styles
```ruby
# app/services/config_parser.rb
require 'tusklang'

class ConfigParser
  def self.parse_ini_style
    content = <<~TSK
      $app_name: "MyApp"
      $version: "1.0.0"
      
      [database]
      host: "localhost"
      port: 5432
    TSK
    
    TuskLang.new.parse(content)
  end
  
  def self.parse_json_style
    content = <<~TSK
      $app_name: "MyApp"
      $version: "1.0.0"
      
      database {
          host: "localhost"
          port: 5432
      }
    TSK
    
    TuskLang.new.parse(content)
  end
  
  def self.parse_xml_style
    content = <<~TSK
      $app_name: "MyApp"
      $version: "1.0.0"
      
      database >
          host: "localhost"
          port: 5432
      <
    TSK
    
    TuskLang.new.parse(content)
  end
end

# Usage
ini_config = ConfigParser.parse_ini_style
json_config = ConfigParser.parse_json_style
xml_config = ConfigParser.parse_xml_style

puts "INI style: #{ini_config['database']['host']}"
puts "JSON style: #{json_config['database']['host']}"
puts "XML style: #{xml_config['database']['host']}"
```

### 2. Rails Model with TuskLang
```ruby
# app/models/application_config.rb
class ApplicationConfig
  include TuskLang::Configurable
  
  attr_accessor :app_name, :version, :environment
  attr_accessor :database, :server, :logging
  
  def initialize
    @database = DatabaseConfig.new
    @server = ServerConfig.new
    @logging = LoggingConfig.new
  end
  
  def self.load_from_file(file_path)
    parser = TuskLang.new
    data = parser.parse_file(file_path)
    
    config = new
    config.app_name = data['app_name']
    config.version = data['version']
    config.environment = data['environment']
    
    # Load nested configurations
    config.database.load_from_hash(data['database'])
    config.server.load_from_hash(data['server'])
    config.logging.load_from_hash(data['logging'])
    
    config
  end
end

class DatabaseConfig
  include TuskLang::Configurable
  
  attr_accessor :host, :port, :name, :user, :password
  
  def load_from_hash(hash)
    @host = hash['host']
    @port = hash['port']
    @name = hash['name']
    @user = hash['user']
    @password = hash['password']
  end
end

class ServerConfig
  include TuskLang::Configurable
  
  attr_accessor :host, :port, :ssl, :workers
  
  def load_from_hash(hash)
    @host = hash['host']
    @port = hash['port']
    @ssl = hash['ssl']
    @workers = hash['workers']
  end
end

class LoggingConfig
  include TuskLang::Configurable
  
  attr_accessor :level, :format, :file
  
  def load_from_hash(hash)
    @level = hash['level']
    @format = hash['format']
    @file = hash['file']
  end
end
```

### 3. Controller Usage
```ruby
# app/controllers/application_controller.rb
class ApplicationController < ActionController::Base
  def app_config
    @app_config ||= ApplicationConfig.load_from_file('config/app.tsk')
  end
  
  def database_config
    app_config.database
  end
  
  def server_config
    app_config.server
  end
  
  def logging_config
    app_config.logging
  end
end

# app/controllers/api/v1/users_controller.rb
class Api::V1::UsersController < ApplicationController
  def index
    config = app_config
    
    # Use configuration values
    users = User.limit(config.server.workers * 100)
    
    Rails.logger.log(config.logging.level, "Retrieved #{users.count} users")
    
    render json: {
      users: users,
      config: {
        app_name: config.app_name,
        version: config.version,
        database_host: config.database.host
      }
    }
  end
end
```

## 🔧 Advanced Syntax Features

### 1. Nested Objects
```ruby
# config/nested.tsk
$app_name: "MyApp"

[server]
ssl {
    enabled: true
    certificate: "/etc/ssl/certs/myapp.crt"
    private_key: "/etc/ssl/private/myapp.key"
    protocols: ["TLSv1.2", "TLSv1.3"]
}

database {
    primary {
        host: "db-primary.myapp.com"
        port: 5432
        name: "myapp"
    }
    replica {
        host: "db-replica.myapp.com"
        port: 5432
        name: "myapp"
    }
    pool {
        min_size: 5
        max_size: 20
        idle_timeout: 300
    }
}

email {
    smtp {
        host: "smtp.gmail.com"
        port: 587
        username: "noreply@myapp.com"
        password: @env("SMTP_PASSWORD")
    }
    templates {
        welcome: "emails/welcome.html.erb"
        reset_password: "emails/reset_password.html.erb"
        notification: "emails/notification.html.erb"
    }
}
```

### 2. Arrays of Objects
```ruby
# config/arrays_of_objects.tsk
[webhooks]
endpoints: [
    {
        url: "https://webhook1.com/notify"
        secret: @env("WEBHOOK_SECRET_1")
        events: ["user.created", "order.completed"]
    },
    {
        url: "https://webhook2.com/notify"
        secret: @env("WEBHOOK_SECRET_2")
        events: ["payment.processed"]
    }
]

[databases]
connections: [
    {
        name: "primary"
        host: "db-primary.myapp.com"
        port: 5432
        role: "master"
    },
    {
        name: "replica1"
        host: "db-replica1.myapp.com"
        port: 5432
        role: "slave"
    },
    {
        name: "replica2"
        host: "db-replica2.myapp.com"
        port: 5432
        role: "slave"
    }
]
```

## 🎯 Best Practices

### 1. File Organization
```ruby
# config/
# ├── app.tsk              # Main application config
# ├── database.tsk         # Database configuration
# ├── server.tsk           # Server configuration
# ├── email.tsk            # Email configuration
# ├── environments/
# │   ├── development.tsk
# │   ├── staging.tsk
# │   └── production.tsk
# └── features/
#     ├── users.tsk
#     ├── payments.tsk
#     └── analytics.tsk
```

### 2. Naming Conventions
```ruby
# Use descriptive names
$app_name: "MyApp"                    # ✅ Good
$app: "MyApp"                         # ❌ Too generic

# Use consistent naming
[database]                            # ✅ Consistent
[db]                                  # ❌ Inconsistent

# Use environment variables for secrets
password: @env("DATABASE_PASSWORD")   # ✅ Secure
password: "secret123"                 # ❌ Insecure
```

### 3. Comments and Documentation
```ruby
# Application Configuration
# This file contains the main application settings
$app_name: "MyApp"
$version: "1.0.0"

# Database Configuration
# Connection settings for the primary database
[database]
host: "localhost"        # Database host
port: 5432              # Database port
name: "myapp"           # Database name
user: "postgres"        # Database user
password: @env("DATABASE_PASSWORD")  # Database password (from environment)

# Server Configuration
# Web server settings
[server]
host: "0.0.0.0"         # Bind to all interfaces
port: 8080              # Server port
ssl: false              # SSL disabled for development
workers: 1              # Number of worker processes
```

## 🚀 Next Steps

Now that you understand the basic syntax, explore:

1. **@ Operators Guide** - Master the powerful built-in functions
2. **Database Integration** - Connect to your databases
3. **Advanced Features** - Caching, monitoring, and performance
4. **Security Features** - Validation and encryption
5. **Rails Integration** - Deep integration with Rails applications

**Ready to write revolutionary configurations? Let's Tusk! 🚀** 
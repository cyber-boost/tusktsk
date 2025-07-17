# 🚂 TuskLang Ruby Rails Integration Guide

**"We don't bow to any king" - Ruby Edition**

Seamlessly integrate TuskLang with your Rails applications. Load configs, override environments, and leverage advanced features for modern Ruby on Rails projects.

## 🚀 Basic Rails Integration

### 1. Add to Gemfile
```ruby
# Gemfile
gem 'tusklang', '~> 1.0'
```

### 2. Load Config in Application
```ruby
# config/application.rb
require 'tusklang'

module MyApp
  class Application < Rails::Application
    config.tusk_config = TuskLang.parse_file('config/app.tsk')
  end
end
```

### 3. Use Config in Controllers
```ruby
# app/controllers/application_controller.rb
class ApplicationController < ActionController::Base
  def tusk_config
    Rails.application.config.tusk_config
  end
end
```

## 🌱 Environment Overrides

### 1. Environment-Specific Configs
```ruby
# config/environments/production.tsk
$environment: "production"
[server]
host: "0.0.0.0"
port: 3000
ssl: true
workers: 4
```

### 2. Load Environment Config
```ruby
# config/initializers/tusk.rb
Rails.application.config.after_initialize do
  env = Rails.env
  config_file = "config/environments/#{env}.tsk"
  if File.exist?(config_file)
    TuskLang.parse_file(config_file)
  end
end
```

## 🛠️ Advanced Usage

### 1. Dynamic Config Reloading
```ruby
# config/initializers/tusk.rb
Rails.application.config.after_initialize do
  ActiveSupport::Reloader.to_prepare do
    Rails.application.config.tusk_config = TuskLang.parse_file('config/app.tsk')
  end
end
```

### 2. Using Config in Models
```ruby
# app/models/user.rb
class User < ApplicationRecord
  def self.config
    TuskLang.config
  end
end
```

### 3. Using Config in Jobs
```ruby
# app/jobs/notify_job.rb
class NotifyJob < ApplicationJob
  queue_as :default
  def perform(user_id)
    config = TuskLang.config
    # Use config values in job logic
  end
end
```

## 🛡️ Best Practices
- Use environment-specific configs for dev, test, prod.
- Reload configs on deploy or restart for fresh values.
- Use @env and @env.secure for secrets and credentials.
- Validate configs before loading in production.

**Ready to supercharge your Rails app? Let's Tusk! 🚀** 
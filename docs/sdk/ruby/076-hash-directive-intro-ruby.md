# 🎯 Hash Directives in TuskLang - Ruby Edition

**"We don't bow to any king" - Ruby Edition**

TuskLang's hash directives provide powerful configuration-driven capabilities that integrate seamlessly with Ruby's rich framework ecosystem, enabling dynamic routing, middleware configuration, and intelligent application behavior control.

## 🎯 Overview

Hash directives in TuskLang allow you to define application behavior, routing rules, middleware stacks, and configuration patterns directly in configuration files. When combined with Ruby's powerful framework capabilities, these directives become incredibly versatile for building sophisticated applications.

## 🚀 Understanding Hash Directives

### What Are Hash Directives?

Hash directives in TuskLang are special configuration patterns that use the `#` symbol to define application behavior, routing, middleware, and other framework-specific functionality. They provide a declarative way to configure your Ruby applications.

```ruby
# TuskLang configuration with hash directives
tsk_content = <<~TSK
  # Basic hash directive structure
  #web {
    host: "0.0.0.0"
    port: 3000
    ssl: true
  }
  
  #api {
    version: "v1"
    rate_limit: 1000
    auth_required: true
  }
  
  #cli {
    commands: ["serve", "migrate", "seed"]
    interactive: true
  }
  
  #cron {
    schedule: "0 0 * * *"
    job: "cleanup_old_data"
  }
TSK

# Ruby integration
require 'tusklang'

parser = TuskLang.new
config = parser.parse(tsk_content)

# Use in Ruby classes
class HashDirectiveProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def web_config
    @config['web']
  end
  
  def api_config
    @config['api']
  end
  
  def cli_config
    @config['cli']
  end
  
  def cron_config
    @config['cron']
  end
  
  def configure_application
    {
      web: {
        host: web_config['host'],
        port: web_config['port'],
        ssl: web_config['ssl']
      },
      api: {
        version: api_config['version'],
        rate_limit: api_config['rate_limit'],
        auth_required: api_config['auth_required']
      },
      cli: {
        commands: cli_config['commands'],
        interactive: cli_config['interactive']
      },
      cron: {
        schedule: cron_config['schedule'],
        job: cron_config['job']
      }
    }
  end
end

# Usage
processor = HashDirectiveProcessor.new(config)
puts processor.configure_application
```

## 🔧 Core Hash Directive Types

### Web Directives

```ruby
# TuskLang with web directives
tsk_content = <<~TSK
  #web {
    # Server configuration
    host: @env("HOST") || "0.0.0.0"
    port: @env("PORT") || 3000
    ssl: @env("SSL_ENABLED") == "true"
    ssl_cert: @env("SSL_CERT_PATH")
    ssl_key: @env("SSL_KEY_PATH")
    
    # Performance settings
    workers: @env("WEB_CONCURRENCY") || 4
    max_connections: @env("MAX_CONNECTIONS") || 1000
    timeout: @env("REQUEST_TIMEOUT") || 30
    
    # Security settings
    cors: {
      enabled: @env("CORS_ENABLED") == "true"
      origins: @env("CORS_ORIGINS").split(",")
      methods: ["GET", "POST", "PUT", "DELETE"]
    }
    
    # Static file serving
    static: {
      enabled: true
      path: "public"
      cache_control: "public, max-age=31536000"
    }
  }
TSK

# Ruby integration with web directives
class WebDirectiveProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_web_config
    @config['web']
  end
  
  def configure_web_server
    web_config = get_web_config
    
    {
      server: {
        host: web_config['host'],
        port: web_config['port'],
        ssl: web_config['ssl'],
        ssl_cert: web_config['ssl_cert'],
        ssl_key: web_config['ssl_key']
      },
      performance: {
        workers: web_config['workers'],
        max_connections: web_config['max_connections'],
        timeout: web_config['timeout']
      },
      security: {
        cors: web_config['cors']
      },
      static: web_config['static']
    }
  end
  
  def apply_web_configuration(app)
    config = configure_web_server
    
    # Apply server configuration
    app.config.host = config[:server][:host]
    app.config.port = config[:server][:port]
    
    # Apply SSL configuration
    if config[:server][:ssl]
      app.config.ssl_certificate = config[:server][:ssl_cert]
      app.config.ssl_key = config[:server][:ssl_key]
    end
    
    # Apply CORS configuration
    if config[:security][:cors][:enabled]
      app.use Rack::Cors do
        allow do
          origins config[:security][:cors][:origins]
          resource '*', methods: config[:security][:cors][:methods]
        end
      end
    end
    
    # Apply static file configuration
    if config[:static][:enabled]
      app.use Rack::Static, 
        urls: ['/assets'],
        root: config[:static][:path],
        header_rules: [[:all, {'Cache-Control' => config[:static][:cache_control]}]]
    end
  end
end
```

### API Directives

```ruby
# TuskLang with API directives
tsk_content = <<~TSK
  #api {
    # API configuration
    version: @env("API_VERSION") || "v1"
    base_path: "/api/#{@env("API_VERSION") || "v1"}"
    
    # Authentication
    auth: {
      required: @env("API_AUTH_REQUIRED") == "true"
      type: @env("API_AUTH_TYPE") || "jwt"
      secret: @env("API_AUTH_SECRET")
      expires_in: @env("API_AUTH_EXPIRES_IN") || "24h"
    }
    
    # Rate limiting
    rate_limit: {
      enabled: @env("API_RATE_LIMIT_ENABLED") == "true"
      requests_per_minute: @env("API_RATE_LIMIT_RPM") || 1000
      burst_size: @env("API_RATE_LIMIT_BURST") || 100
    }
    
    # Response formatting
    response: {
      format: @env("API_RESPONSE_FORMAT") || "json"
      include_metadata: @env("API_INCLUDE_METADATA") == "true"
      pagination: {
        enabled: true
        default_per_page: @env("API_DEFAULT_PER_PAGE") || 20
        max_per_page: @env("API_MAX_PER_PAGE") || 100
      }
    }
    
    # Documentation
    docs: {
      enabled: @env("API_DOCS_ENABLED") == "true"
      path: "/docs"
      swagger_ui: true
      openapi_version: "3.0.0"
    }
  }
TSK

# Ruby integration with API directives
class ApiDirectiveProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_api_config
    @config['api']
  end
  
  def configure_api
    api_config = get_api_config
    
    {
      version: api_config['version'],
      base_path: api_config['base_path'],
      auth: api_config['auth'],
      rate_limit: api_config['rate_limit'],
      response: api_config['response'],
      docs: api_config['docs']
    }
  end
  
  def apply_api_configuration(app)
    config = configure_api
    
    # Set API version and base path
    app.config.api_version = config[:version]
    app.config.api_base_path = config[:base_path]
    
    # Apply authentication middleware
    if config[:auth][:required]
      case config[:auth][:type]
      when 'jwt'
        app.use JWT::Auth, secret: config[:auth][:secret]
      when 'api_key'
        app.use ApiKeyAuth, secret: config[:auth][:secret]
      end
    end
    
    # Apply rate limiting
    if config[:rate_limit][:enabled]
      app.use Rack::Attack do
        throttle('api/ip', limit: config[:rate_limit][:requests_per_minute], period: 1.minute) do |req|
          req.ip if req.path.start_with?('/api/')
        end
      end
    end
    
    # Apply response formatting
    app.config.response_format = config[:response][:format]
    app.config.include_metadata = config[:response][:include_metadata]
    app.config.pagination = config[:response][:pagination]
    
    # Apply API documentation
    if config[:docs][:enabled]
      app.mount Swagger::Ui, at: config[:docs][:path]
    end
  end
end
```

### CLI Directives

```ruby
# TuskLang with CLI directives
tsk_content = <<~TSK
  #cli {
    # CLI configuration
    name: @env("CLI_NAME") || "tusk"
    version: @env("CLI_VERSION") || "1.0.0"
    description: @env("CLI_DESCRIPTION") || "TuskLang CLI Tool"
    
    # Commands
    commands: {
      serve: {
        description: "Start the web server"
        options: ["--host", "--port", "--ssl"]
        default_options: {
          host: "0.0.0.0"
          port: 3000
          ssl: false
        }
      }
      
      migrate: {
        description: "Run database migrations"
        options: ["--environment", "--version"]
        default_options: {
          environment: "development"
          version: "latest"
        }
      }
      
      seed: {
        description: "Seed the database"
        options: ["--environment", "--file"]
        default_options: {
          environment: "development"
          file: "db/seeds.rb"
        }
      }
      
      console: {
        description: "Start an interactive console"
        options: ["--environment"]
        default_options: {
          environment: "development"
        }
      }
    }
    
    # Interactive mode
    interactive: {
      enabled: @env("CLI_INTERACTIVE") == "true"
      prompt: "tusk> "
      history_file: "~/.tusk_history"
      auto_completion: true
    }
    
    # Output formatting
    output: {
      format: @env("CLI_OUTPUT_FORMAT") || "text"
      colors: @env("CLI_COLORS") == "true"
      verbose: @env("CLI_VERBOSE") == "true"
    }
  }
TSK

# Ruby integration with CLI directives
class CliDirectiveProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_cli_config
    @config['cli']
  end
  
  def configure_cli
    cli_config = get_cli_config
    
    {
      name: cli_config['name'],
      version: cli_config['version'],
      description: cli_config['description'],
      commands: cli_config['commands'],
      interactive: cli_config['interactive'],
      output: cli_config['output']
    }
  end
  
  def create_cli_application
    config = configure_cli
    
    # Create CLI application using Thor or similar
    cli = Class.new(Thor) do
      desc "serve", config[:commands]['serve']['description']
      option :host, default: config[:commands]['serve']['default_options']['host']
      option :port, default: config[:commands]['serve']['default_options']['port']
      option :ssl, type: :boolean, default: config[:commands]['serve']['default_options']['ssl']
      def serve
        puts "Starting server on #{options[:host]}:#{options[:port]}"
        # Server startup logic
      end
      
      desc "migrate", config[:commands]['migrate']['description']
      option :environment, default: config[:commands]['migrate']['default_options']['environment']
      option :version, default: config[:commands]['migrate']['default_options']['version']
      def migrate
        puts "Running migrations for #{options[:environment]}"
        # Migration logic
      end
      
      desc "seed", config[:commands]['seed']['description']
      option :environment, default: config[:commands]['seed']['default_options']['environment']
      option :file, default: config[:commands]['seed']['default_options']['file']
      def seed
        puts "Seeding database from #{options[:file]}"
        # Seeding logic
      end
      
      desc "console", config[:commands]['console']['description']
      option :environment, default: config[:commands]['console']['default_options']['environment']
      def console
        puts "Starting console for #{options[:environment]}"
        # Console logic
      end
    end
    
    cli
  end
  
  def setup_interactive_mode
    config = configure_cli
    
    if config[:interactive][:enabled]
      require 'readline'
      require 'irb'
      
      Readline.completion_proc = proc { |s| config[:commands].keys.grep(/^#{Regexp.escape(s)}/) }
      Readline.completion_append_character = " "
      
      IRB.conf[:PROMPT][:TUSK] = {
        PROMPT_I: config[:interactive][:prompt],
        PROMPT_N: config[:interactive][:prompt],
        PROMPT_S: config[:interactive][:prompt],
        PROMPT_C: config[:interactive][:prompt],
        RETURN: "=> %s\n"
      }
      
      IRB.conf[:PROMPT_MODE] = :TUSK
    end
  end
end
```

### Cron Directives

```ruby
# TuskLang with cron directives
tsk_content = <<~TSK
  #cron {
    # Cron job configuration
    jobs: {
      cleanup_old_data: {
        schedule: "0 2 * * *"
        description: "Clean up old data daily at 2 AM"
        enabled: @env("CRON_CLEANUP_ENABLED") == "true"
        timeout: @env("CRON_CLEANUP_TIMEOUT") || "30m"
        retries: @env("CRON_CLEANUP_RETRIES") || 3
        job_class: "CleanupOldDataJob"
      }
      
      backup_database: {
        schedule: "0 1 * * 0"
        description: "Backup database weekly on Sunday at 1 AM"
        enabled: @env("CRON_BACKUP_ENABLED") == "true"
        timeout: @env("CRON_BACKUP_TIMEOUT") || "1h"
        retries: @env("CRON_BACKUP_RETRIES") || 2
        job_class: "BackupDatabaseJob"
      }
      
      send_newsletter: {
        schedule: "0 9 * * 1"
        description: "Send newsletter every Monday at 9 AM"
        enabled: @env("CRON_NEWSLETTER_ENABLED") == "true"
        timeout: @env("CRON_NEWSLETTER_TIMEOUT") || "15m"
        retries: @env("CRON_NEWSLETTER_RETRIES") || 1
        job_class: "SendNewsletterJob"
      }
      
      health_check: {
        schedule: "*/5 * * * *"
        description: "Health check every 5 minutes"
        enabled: @env("CRON_HEALTH_CHECK_ENABLED") == "true"
        timeout: @env("CRON_HEALTH_CHECK_TIMEOUT") || "1m"
        retries: @env("CRON_HEALTH_CHECK_RETRIES") || 1
        job_class: "HealthCheckJob"
      }
    }
    
    # Global cron settings
    settings: {
      timezone: @env("CRON_TIMEZONE") || "UTC"
      log_level: @env("CRON_LOG_LEVEL") || "info"
      max_concurrent_jobs: @env("CRON_MAX_CONCURRENT") || 5
      job_queue: @env("CRON_JOB_QUEUE") || "default"
    }
  }
TSK

# Ruby integration with cron directives
class CronDirectiveProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_cron_config
    @config['cron']
  end
  
  def configure_cron_jobs
    cron_config = get_cron_config
    
    {
      jobs: cron_config['jobs'],
      settings: cron_config['settings']
    }
  end
  
  def setup_cron_scheduler
    config = configure_cron_jobs
    
    require 'rufus-scheduler'
    
    scheduler = Rufus::Scheduler.new
    
    # Set timezone
    scheduler.tz = config[:settings][:timezone]
    
    # Configure each job
    config[:jobs].each do |job_name, job_config|
      next unless job_config['enabled']
      
      scheduler.cron job_config['schedule'] do
        begin
          Rails.logger.info "Starting cron job: #{job_name}"
          
          # Execute the job
          job_class = job_config['job_class'].constantize
          job = job_class.new
          
          Timeout::timeout(job_config['timeout']) do
            job.perform
          end
          
          Rails.logger.info "Completed cron job: #{job_name}"
        rescue => e
          Rails.logger.error "Cron job #{job_name} failed: #{e.message}"
          
          # Retry logic
          retries = job_config['retries']
          if retries > 0
            Rails.logger.info "Retrying cron job #{job_name} (#{retries} retries left)"
            # Implement retry logic
          end
        end
      end
    end
    
    scheduler
  end
  
  def create_job_classes
    config = configure_cron_jobs
    
    config[:jobs].each do |job_name, job_config|
      next unless job_config['enabled']
      
      # Create job class dynamically
      job_class_name = job_config['job_class']
      
      unless Object.const_defined?(job_class_name)
        Object.const_set(job_class_name, Class.new do
          def perform
            # Default job implementation
            puts "Executing #{self.class.name}"
          end
        end)
      end
    end
  end
end
```

## 🎛️ Advanced Hash Directive Patterns

### Conditional Directives

```ruby
# TuskLang with conditional directives
tsk_content = <<~TSK
  # Conditional web configuration
  #web {
    host: @env("HOST") || "0.0.0.0"
    port: @env("PORT") || 3000
    
    # Environment-specific settings
    ssl: @when(@env("RAILS_ENV") == "production", true, false)
    workers: @when(@env("RAILS_ENV") == "production", 8, 2)
    
    # Feature flag based configuration
    cors: @when(@env("CORS_ENABLED") == "true", {
      enabled: true
      origins: @env("CORS_ORIGINS").split(",")
    }, {
      enabled: false
      origins: []
    })
  }
  
  # Conditional API configuration
  #api {
    version: @env("API_VERSION") || "v1"
    
    # Authentication based on environment
    auth: @when(@env("RAILS_ENV") == "production", {
      required: true
      type: "jwt"
      secret: @env("JWT_SECRET")
    }, {
      required: false
      type: "none"
      secret: ""
    })
    
    # Rate limiting based on environment
    rate_limit: @when(@env("RAILS_ENV") == "production", {
      enabled: true
      requests_per_minute: 1000
    }, {
      enabled: false
      requests_per_minute: 10000
    })
  }
TSK

# Ruby integration with conditional directives
class ConditionalDirectiveProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_conditional_web_config
    @config['web']
  end
  
  def get_conditional_api_config
    @config['api']
  end
  
  def apply_conditional_configuration(app)
    web_config = get_conditional_web_config
    api_config = get_conditional_api_config
    
    # Apply environment-specific web configuration
    app.config.host = web_config['host']
    app.config.port = web_config['port']
    app.config.ssl = web_config['ssl']
    app.config.workers = web_config['workers']
    
    # Apply conditional CORS
    if web_config['cors']['enabled']
      app.use Rack::Cors do
        allow do
          origins web_config['cors']['origins']
          resource '*', methods: [:get, :post, :put, :delete]
        end
      end
    end
    
    # Apply conditional API authentication
    if api_config['auth']['required']
      case api_config['auth']['type']
      when 'jwt'
        app.use JWT::Auth, secret: api_config['auth']['secret']
      end
    end
    
    # Apply conditional rate limiting
    if api_config['rate_limit']['enabled']
      app.use Rack::Attack do
        throttle('api/ip', limit: api_config['rate_limit']['requests_per_minute'], period: 1.minute) do |req|
          req.ip if req.path.start_with?('/api/')
        end
      end
    end
  end
end
```

### Nested Directives

```ruby
# TuskLang with nested directives
tsk_content = <<~TSK
  # Nested web configuration
  #web {
    server: {
      host: @env("HOST") || "0.0.0.0"
      port: @env("PORT") || 3000
      ssl: @env("SSL_ENABLED") == "true"
    }
    
    middleware: {
      cors: {
        enabled: @env("CORS_ENABLED") == "true"
        origins: @env("CORS_ORIGINS").split(",")
        methods: ["GET", "POST", "PUT", "DELETE"]
        headers: ["Content-Type", "Authorization"]
      }
      
      auth: {
        enabled: @env("AUTH_ENABLED") == "true"
        type: @env("AUTH_TYPE") || "jwt"
        secret: @env("AUTH_SECRET")
      }
      
      cache: {
        enabled: @env("CACHE_ENABLED") == "true"
        store: @env("CACHE_STORE") || "redis"
        ttl: @env("CACHE_TTL") || "1h"
      }
    }
    
    static: {
      enabled: @env("STATIC_ENABLED") == "true"
      path: @env("STATIC_PATH") || "public"
      cache_control: @env("STATIC_CACHE_CONTROL") || "public, max-age=31536000"
    }
  }
TSK

# Ruby integration with nested directives
class NestedDirectiveProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_nested_web_config
    @config['web']
  end
  
  def apply_nested_configuration(app)
    web_config = get_nested_web_config
    
    # Apply server configuration
    server_config = web_config['server']
    app.config.host = server_config['host']
    app.config.port = server_config['port']
    app.config.ssl = server_config['ssl']
    
    # Apply middleware configuration
    middleware_config = web_config['middleware']
    
    # CORS middleware
    if middleware_config['cors']['enabled']
      app.use Rack::Cors do
        allow do
          origins middleware_config['cors']['origins']
          resource '*', 
            methods: middleware_config['cors']['methods'],
            headers: middleware_config['cors']['headers']
        end
      end
    end
    
    # Authentication middleware
    if middleware_config['auth']['enabled']
      case middleware_config['auth']['type']
      when 'jwt'
        app.use JWT::Auth, secret: middleware_config['auth']['secret']
      when 'api_key'
        app.use ApiKeyAuth, secret: middleware_config['auth']['secret']
      end
    end
    
    # Cache middleware
    if middleware_config['cache']['enabled']
      case middleware_config['cache']['store']
      when 'redis'
        app.use Rack::Cache, metastore: 'redis://localhost:6379/0/metastore', entitystore: 'redis://localhost:6379/0/entitystore'
      when 'memory'
        app.use Rack::Cache, metastore: 'file:/tmp/rack_meta', entitystore: 'file:/tmp/rack_body'
      end
    end
    
    # Static file middleware
    static_config = web_config['static']
    if static_config['enabled']
      app.use Rack::Static, 
        urls: ['/assets'],
        root: static_config['path'],
        header_rules: [[:all, {'Cache-Control' => static_config['cache_control']}]]
    end
  end
end
```

## 🛡️ Error Handling and Validation

### Directive Validation

```ruby
# TuskLang with directive validation
tsk_content = <<~TSK
  # Validated web directive
  #web {
    host: @validate.host(@env("HOST") || "0.0.0.0")
    port: @validate.port(@env("PORT") || 3000)
    ssl: @validate.boolean(@env("SSL_ENABLED") == "true")
    
    # Validate nested configuration
    cors: @validate.object({
      enabled: @validate.boolean(@env("CORS_ENABLED") == "true")
      origins: @validate.array(@env("CORS_ORIGINS").split(","))
      methods: @validate.array(["GET", "POST", "PUT", "DELETE"])
    })
  }
  
  # Validated API directive
  #api {
    version: @validate.string(@env("API_VERSION") || "v1")
    
    auth: @validate.object({
      required: @validate.boolean(@env("API_AUTH_REQUIRED") == "true")
      type: @validate.enum(@env("API_AUTH_TYPE") || "jwt", ["jwt", "api_key", "oauth"])
      secret: @validate.required(@env("API_AUTH_SECRET"))
    })
    
    rate_limit: @validate.object({
      enabled: @validate.boolean(@env("API_RATE_LIMIT_ENABLED") == "true")
      requests_per_minute: @validate.number(@env("API_RATE_LIMIT_RPM") || 1000)
      burst_size: @validate.number(@env("API_RATE_LIMIT_BURST") || 100)
    })
  }
TSK

# Ruby integration with directive validation
class DirectiveValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_validated_web_config
    @config['web']
  end
  
  def get_validated_api_config
    @config['api']
  end
  
  def validate_configuration
    errors = []
    
    # Validate web configuration
    web_config = get_validated_web_config
    errors << "Invalid host: #{web_config['host']}" unless valid_host?(web_config['host'])
    errors << "Invalid port: #{web_config['port']}" unless valid_port?(web_config['port'])
    
    # Validate API configuration
    api_config = get_validated_api_config
    errors << "Invalid API version: #{api_config['version']}" unless valid_api_version?(api_config['version'])
    
    if api_config['auth']['required']
      errors << "Missing auth secret" if api_config['auth']['secret'].blank?
      errors << "Invalid auth type: #{api_config['auth']['type']}" unless valid_auth_type?(api_config['auth']['type'])
    end
    
    {
      valid: errors.empty?,
      errors: errors
    }
  end
  
  def apply_validated_configuration(app)
    validation = validate_configuration
    
    unless validation[:valid]
      raise ConfigurationError, "Invalid configuration: #{validation[:errors].join(', ')}"
    end
    
    # Apply validated configuration
    web_config = get_validated_web_config
    api_config = get_validated_api_config
    
    # Apply web configuration
    app.config.host = web_config['host']
    app.config.port = web_config['port']
    app.config.ssl = web_config['ssl']
    
    # Apply API configuration
    app.config.api_version = api_config['version']
    
    if api_config['auth']['required']
      case api_config['auth']['type']
      when 'jwt'
        app.use JWT::Auth, secret: api_config['auth']['secret']
      when 'api_key'
        app.use ApiKeyAuth, secret: api_config['auth']['secret']
      end
    end
  end
  
  private
  
  def valid_host?(host)
    host.match?(/^[\d\.]+$/) || host.match?(/^[a-zA-Z0-9\-\.]+$/)
  end
  
  def valid_port?(port)
    port.to_i.between?(1, 65535)
  end
  
  def valid_api_version?(version)
    version.match?(/^v\d+$/)
  end
  
  def valid_auth_type?(type)
    ['jwt', 'api_key', 'oauth'].include?(type)
  end
end

class ConfigurationError < StandardError; end
```

## 🚀 Performance Optimization

### Efficient Directive Processing

```ruby
# TuskLang with performance optimizations
tsk_content = <<~TSK
  # Optimized web directive
  #web {
    # Cached configuration
    host: @cache("1h", @env("HOST") || "0.0.0.0")
    port: @cache("1h", @env("PORT") || 3000)
    
    # Lazy loaded middleware
    middleware: @lazy(@load_middleware_config())
    
    # Conditional loading
    ssl_config: @when(@env("SSL_ENABLED") == "true", @load_ssl_config(), null)
  }
  
  # Optimized API directive
  #api {
    # Cached API configuration
    config: @cache("30m", @load_api_config())
    
    # Lazy loaded routes
    routes: @lazy(@load_api_routes())
    
    # Conditional features
    features: @when(@env("API_FEATURES_ENABLED") == "true", @load_api_features(), {})
  }
TSK

# Ruby integration with optimized directives
class OptimizedDirectiveProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_optimized_web_config
    @config['web']
  end
  
  def get_optimized_api_config
    @config['api']
  end
  
  def apply_optimized_configuration(app)
    web_config = get_optimized_web_config
    api_config = get_optimized_api_config
    
    # Apply cached web configuration
    app.config.host = Rails.cache.fetch("web_host", expires_in: 1.hour) { web_config['host'] }
    app.config.port = Rails.cache.fetch("web_port", expires_in: 1.hour) { web_config['port'] }
    
    # Apply lazy loaded middleware
    middleware_config = web_config['middleware']
    apply_middleware_lazily(app, middleware_config)
    
    # Apply conditional SSL configuration
    if web_config['ssl_config']
      apply_ssl_configuration(app, web_config['ssl_config'])
    end
    
    # Apply cached API configuration
    api_config_data = Rails.cache.fetch("api_config", expires_in: 30.minutes) { api_config['config'] }
    apply_api_configuration(app, api_config_data)
    
    # Apply lazy loaded routes
    routes_config = api_config['routes']
    apply_routes_lazily(app, routes_config)
    
    # Apply conditional API features
    if api_config['features']
      apply_api_features(app, api_config['features'])
    end
  end
  
  private
  
  def apply_middleware_lazily(app, middleware_config)
    # Load and apply middleware only when needed
    middleware_config.each do |middleware_name, config|
      next unless config['enabled']
      
      case middleware_name
      when 'cors'
        app.use Rack::Cors, config
      when 'auth'
        app.use AuthMiddleware, config
      when 'cache'
        app.use CacheMiddleware, config
      end
    end
  end
  
  def apply_ssl_configuration(app, ssl_config)
    app.config.ssl_certificate = ssl_config['cert']
    app.config.ssl_key = ssl_config['key']
  end
  
  def apply_api_configuration(app, api_config)
    app.config.api_version = api_config['version']
    app.config.api_base_path = api_config['base_path']
  end
  
  def apply_routes_lazily(app, routes_config)
    # Load routes only when needed
    routes_config.each do |route|
      app.get route['path'] do
        # Route handler
      end
    end
  end
  
  def apply_api_features(app, features_config)
    features_config.each do |feature_name, config|
      next unless config['enabled']
      
      case feature_name
      when 'rate_limiting'
        app.use RateLimitingMiddleware, config
      when 'caching'
        app.use ApiCachingMiddleware, config
      when 'monitoring'
        app.use ApiMonitoringMiddleware, config
      end
    end
  end
end
```

## 🎯 Best Practices

### 1. Use Descriptive Directive Names
```ruby
# Good
#web_server {
  host: "0.0.0.0"
  port: 3000
}

#api_gateway {
  version: "v1"
  auth_required: true
}

# Avoid
#web {
  host: "0.0.0.0"
  port: 3000
}

#api {
  version: "v1"
  auth_required: true
}
```

### 2. Validate All Directives
```ruby
# TuskLang with validation
tsk_content = <<~TSK
  # Always validate directives
  #web {
    host: @validate.host(@env("HOST") || "0.0.0.0")
    port: @validate.port(@env("PORT") || 3000)
  }
  
  #api {
    version: @validate.string(@env("API_VERSION") || "v1")
    auth: @validate.object(@load_auth_config())
  }
TSK
```

### 3. Use Caching for Expensive Operations
```ruby
# Cache expensive directive operations
expensive_config: @cache("10m", @load_complex_configuration())
```

### 4. Handle Edge Cases
```ruby
# TuskLang with edge case handling
tsk_content = <<~TSK
  # Handle missing environment variables
  #web {
    host: @env("HOST") || "0.0.0.0"
    port: @env("PORT") || 3000
    ssl: @env("SSL_ENABLED") == "true"
  }
  
  # Handle conditional features
  #api {
    features: @when(@env("API_FEATURES_ENABLED") == "true", @load_features(), {})
  }
TSK
```

## 🔧 Troubleshooting

### Common Hash Directive Issues

```ruby
# Issue: Invalid directive syntax
# Solution: Use proper hash directive syntax
tsk_content = <<~TSK
  [directive_syntax_fixes]
  # Correct syntax
  #web {
    host: "0.0.0.0"
    port: 3000
  }
  
  # Incorrect syntax
  # web {
  #   host: "0.0.0.0"
  #   port: 3000
  # }
TSK

# Issue: Missing validation
# Solution: Add proper validation
tsk_content = <<~TSK
  [validation_fixes]
  # Add validation to all directives
  #web {
    host: @validate.host(@env("HOST") || "0.0.0.0")
    port: @validate.port(@env("PORT") || 3000)
  }
TSK

# Issue: Performance problems
# Solution: Use caching and lazy loading
tsk_content = <<~TSK
  [performance_fixes]
  # Use caching for expensive operations
  config: @cache("5m", @load_expensive_config())
  
  # Use lazy loading for optional features
  features: @lazy(@load_optional_features())
TSK
```

## 🎯 Summary

TuskLang's hash directives provide powerful configuration-driven capabilities that integrate seamlessly with Ruby applications. By leveraging these directives, you can:

- **Define application behavior** declaratively in configuration files
- **Configure web servers, APIs, CLIs, and cron jobs** dynamically
- **Handle conditional configuration** based on environment and feature flags
- **Validate and optimize** directive processing
- **Build sophisticated applications** with minimal code

The Ruby integration makes these directives even more powerful by combining TuskLang's declarative syntax with Ruby's rich framework ecosystem and libraries.

**Remember**: TuskLang hash directives are designed to be expressive, performant, and Ruby-friendly. Use them to create dynamic, efficient, and maintainable application configurations.

**Key Takeaways**:
- Always validate hash directives before processing
- Use caching for expensive directive operations
- Handle conditional configuration gracefully
- Implement proper error handling for directive processing
- Combine with Ruby's framework libraries for advanced functionality 
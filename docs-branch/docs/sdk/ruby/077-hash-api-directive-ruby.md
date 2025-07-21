# Hash API Directive in TuskLang for Ruby

Welcome to the rebellious world of TuskLang's Hash API Directive! This is where we break free from the constraints of traditional API frameworks and embrace the power of declarative, configuration-driven API development. In Ruby, this means combining the elegance of TuskLang's hash directives with the expressiveness of Ruby's metaprogramming capabilities.

## What is the Hash API Directive?

The Hash API Directive (`#api`) is TuskLang's way of saying "enough with the boilerplate!" It allows you to define complete API endpoints, routes, middleware, and behaviors using simple hash configurations. In Ruby, this translates to powerful, declarative API definitions that can be processed, validated, and executed with minimal ceremony.

## Basic API Directive Syntax

```ruby
# Basic API endpoint definition
api_endpoint = {
  "#api" => {
    "method" => "GET",
    "path" => "/users/:id",
    "handler" => "UserController#show",
    "middleware" => ["auth", "rate_limit"],
    "validation" => {
      "id" => "integer|required"
    }
  }
}

# Ruby class to process the directive
class ApiDirectiveProcessor
  def initialize(config)
    @config = config
    @tusk_config = load_tusk_config
  end

  def process_api_directive
    api_config = @config["#api"]
    return nil unless api_config

    {
      method: api_config["method"]&.upcase,
      path: api_config["path"],
      handler: parse_handler(api_config["handler"]),
      middleware: Array(api_config["middleware"]),
      validation: api_config["validation"] || {},
      response_format: api_config["response_format"] || "json"
    }
  end

  private

  def parse_handler(handler_string)
    return nil unless handler_string
    
    controller, action = handler_string.split("#")
    {
      controller: controller,
      action: action
    }
  end

  def load_tusk_config
    # Load TuskLang configuration
    TuskConfig.load("peanu.tsk")
  end
end
```

## Advanced API Configuration

```ruby
# Comprehensive API configuration with TuskLang features
advanced_api_config = {
  "#api" => {
    "method" => "POST",
    "path" => "/api/v1/users",
    "handler" => "Api::V1::UsersController#create",
    "middleware" => ["cors", "auth", "rate_limit:100,1m"],
    "validation" => {
      "user.email" => "email|required|unique:users",
      "user.password" => "string|min:8|max:128",
      "user.profile.name" => "string|required|max:100"
    },
    "response_format" => "json",
    "status_codes" => {
      "201" => "User created successfully",
      "400" => "Validation failed",
      "409" => "Email already exists"
    },
    "documentation" => {
      "summary" => "Create a new user account",
      "description" => "Creates a new user with email verification",
      "tags" => ["users", "authentication"]
    }
  }
}

# Ruby processor for advanced configurations
class AdvancedApiProcessor < ApiDirectiveProcessor
  def process_advanced_config
    base_config = process_api_directive
    api_config = @config["#api"]
    
    base_config.merge({
      status_codes: api_config["status_codes"] || {},
      documentation: api_config["documentation"] || {},
      rate_limit: parse_rate_limit(api_config["middleware"]),
      validation_rules: build_validation_rules(api_config["validation"])
    })
  end

  private

  def parse_rate_limit(middleware)
    rate_limit_middleware = middleware.find { |m| m.start_with?("rate_limit:") }
    return nil unless rate_limit_middleware
    
    limits = rate_limit_middleware.split(":")[1]
    requests, period = limits.split(",")
    {
      requests: requests.to_i,
      period: parse_period(period)
    }
  end

  def parse_period(period)
    case period
    when /(\d+)s/ then $1.to_i
    when /(\d+)m/ then $1.to_i * 60
    when /(\d+)h/ then $1.to_i * 3600
    else 60 # default to 1 minute
    end
  end

  def build_validation_rules(validation_config)
    validation_config.transform_values do |rules|
      rules.split("|").map(&:strip)
    end
  end
end
```

## RESTful API Patterns

```ruby
# Complete RESTful API configuration
restful_api_config = {
  "#api" => {
    "base_path" => "/api/v1",
    "resources" => {
      "users" => {
        "index" => {
          "method" => "GET",
          "handler" => "UsersController#index",
          "middleware" => ["auth", "pagination"],
          "query_params" => ["page", "per_page", "search"]
        },
        "show" => {
          "method" => "GET",
          "path" => "/:id",
          "handler" => "UsersController#show",
          "middleware" => ["auth"]
        },
        "create" => {
          "method" => "POST",
          "handler" => "UsersController#create",
          "middleware" => ["auth", "validation"],
          "validation" => {
            "user.email" => "email|required|unique:users",
            "user.password" => "string|min:8"
          }
        },
        "update" => {
          "method" => "PUT",
          "path" => "/:id",
          "handler" => "UsersController#update",
          "middleware" => ["auth", "ownership"]
        },
        "destroy" => {
          "method" => "DELETE",
          "path" => "/:id",
          "handler" => "UsersController#destroy",
          "middleware" => ["auth", "admin"]
        }
      }
    }
  }
}

# Ruby RESTful API processor
class RestfulApiProcessor
  def initialize(config)
    @config = config["#api"]
  end

  def generate_routes
    base_path = @config["base_path"]
    routes = []

    @config["resources"].each do |resource_name, actions|
      actions.each do |action_name, action_config|
        route = build_route(base_path, resource_name, action_name, action_config)
        routes << route
      end
    end

    routes
  end

  private

  def build_route(base_path, resource, action, config)
    path = config["path"] || resource_path(resource, action)
    full_path = "#{base_path}#{path}"

    {
      method: config["method"],
      path: full_path,
      handler: config["handler"],
      middleware: Array(config["middleware"]),
      validation: config["validation"] || {},
      query_params: config["query_params"] || []
    }
  end

  def resource_path(resource, action)
    case action
    when "index" then "/#{resource}"
    when "create" then "/#{resource}"
    when "show" then "/#{resource}/:id"
    when "update" then "/#{resource}/:id"
    when "destroy" then "/#{resource}/:id"
    else "/#{resource}"
    end
  end
end
```

## API Versioning and Documentation

```ruby
# Versioned API with comprehensive documentation
versioned_api_config = {
  "#api" => {
    "version" => "v1",
    "base_url" => "https://api.example.com",
    "documentation" => {
      "title" => "Example API",
      "version" => "1.0.0",
      "description" => "A comprehensive API for managing resources",
      "contact" => {
        "name" => "API Support",
        "email" => "api@example.com"
      }
    },
    "endpoints" => {
      "users" => {
        "GET /users" => {
          "summary" => "List all users",
          "parameters" => {
            "page" => { "type" => "integer", "default" => 1 },
            "per_page" => { "type" => "integer", "default" => 20 }
          },
          "responses" => {
            "200" => { "description" => "List of users" },
            "401" => { "description" => "Unauthorized" }
          }
        },
        "POST /users" => {
          "summary" => "Create a new user",
          "request_body" => {
            "required" => true,
            "content" => {
              "application/json" => {
                "schema" => {
                  "type" => "object",
                  "properties" => {
                    "email" => { "type" => "string", "format" => "email" },
                    "password" => { "type" => "string", "minLength" => 8 }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}

# Ruby API documentation generator
class ApiDocumentationGenerator
  def initialize(config)
    @config = config["#api"]
  end

  def generate_openapi_spec
    {
      openapi: "3.0.0",
      info: build_info,
      servers: build_servers,
      paths: build_paths,
      components: build_components
    }
  end

  private

  def build_info
    doc = @config["documentation"]
    {
      title: doc["title"],
      version: doc["version"],
      description: doc["description"],
      contact: doc["contact"]
    }
  end

  def build_servers
    [
      {
        url: @config["base_url"],
        description: "Production server"
      }
    ]
  end

  def build_paths
    paths = {}
    
    @config["endpoints"].each do |resource, endpoints|
      endpoints.each do |path_method, config|
        method, path = path_method.split(" ")
        paths[path] ||= {}
        paths[path][method.downcase] = build_operation(config)
      end
    end

    paths
  end

  def build_operation(config)
    {
      summary: config["summary"],
      parameters: build_parameters(config["parameters"]),
      requestBody: build_request_body(config["request_body"]),
      responses: build_responses(config["responses"])
    }
  end

  def build_parameters(parameters)
    return [] unless parameters

    parameters.map do |name, spec|
      {
        name: name,
        in: "query",
        schema: { type: spec["type"] },
        default: spec["default"]
      }
    end
  end

  def build_request_body(request_body)
    return nil unless request_body

    {
      required: request_body["required"],
      content: request_body["content"]
    }
  end

  def build_responses(responses)
    return {} unless responses

    responses.transform_values do |response|
      { description: response["description"] }
    end
  end

  def build_components
    {
      securitySchemes: {
        bearerAuth: {
          type: "http",
          scheme: "bearer",
          bearerFormat: "JWT"
        }
      }
    }
  end
end
```

## Error Handling and Validation

```ruby
# API error handling configuration
error_handling_config = {
  "#api" => {
    "error_handling" => {
      "global_middleware" => ["error_handler", "cors"],
      "error_responses" => {
        "400" => {
          "message" => "Bad Request",
          "details" => "validation_errors"
        },
        "401" => {
          "message" => "Unauthorized",
          "details" => "authentication_required"
        },
        "403" => {
          "message" => "Forbidden",
          "details" => "insufficient_permissions"
        },
        "404" => {
          "message" => "Not Found",
          "details" => "resource_not_found"
        },
        "422" => {
          "message" => "Unprocessable Entity",
          "details" => "validation_failed"
        },
        "500" => {
          "message" => "Internal Server Error",
          "details" => "server_error"
        }
      },
      "validation" => {
        "strict_mode" => true,
        "custom_validators" => {
          "unique_email" => "EmailValidator#unique",
          "strong_password" => "PasswordValidator#strong"
        }
      }
    }
  }
}

# Ruby error handling processor
class ApiErrorHandler
  def initialize(config)
    @config = config["#api"]["error_handling"]
  end

  def handle_error(error, request)
    error_response = build_error_response(error)
    log_error(error, request)
    error_response
  end

  private

  def build_error_response(error)
    status_code = determine_status_code(error)
    error_config = @config["error_responses"][status_code.to_s]

    {
      status: status_code,
      error: {
        message: error_config["message"],
        details: error_config["details"],
        timestamp: Time.current.iso8601,
        request_id: SecureRandom.uuid
      }
    }
  end

  def determine_status_code(error)
    case error
    when ActiveRecord::RecordNotFound then 404
    when ActiveRecord::RecordInvalid then 422
    when ActionController::ParameterMissing then 400
    when ActionController::UnpermittedParameters then 400
    when ActionController::InvalidAuthenticityToken then 401
    else 500
    end
  end

  def log_error(error, request)
    Rails.logger.error({
      error: error.class.name,
      message: error.message,
      backtrace: error.backtrace.first(5),
      request: {
        method: request.method,
        path: request.path,
        params: request.params.except(:controller, :action),
        user_agent: request.user_agent,
        ip: request.remote_ip
      }
    })
  end
end
```

## Performance and Caching

```ruby
# API performance and caching configuration
performance_config = {
  "#api" => {
    "performance" => {
      "caching" => {
        "enabled" => true,
        "strategy" => "redis",
        "ttl" => 3600,
        "cache_keys" => {
          "users" => "users:list:{page}:{per_page}",
          "user" => "users:show:{id}",
          "user_posts" => "users:{id}:posts:{page}"
        }
      },
      "rate_limiting" => {
        "enabled" => true,
        "limits" => {
          "default" => "1000/hour",
          "authenticated" => "5000/hour",
          "admin" => "10000/hour"
        }
      },
      "compression" => {
        "enabled" => true,
        "algorithms" => ["gzip", "deflate"]
      },
      "pagination" => {
        "default_per_page" => 20,
        "max_per_page" => 100,
        "page_param" => "page",
        "per_page_param" => "per_page"
      }
    }
  }
}

# Ruby performance processor
class ApiPerformanceProcessor
  def initialize(config)
    @config = config["#api"]["performance"]
  end

  def apply_caching(response, cache_key)
    return response unless @config["caching"]["enabled"]

    cache_config = @config["caching"]
    Rails.cache.fetch(cache_key, expires_in: cache_config["ttl"]) do
      response
    end
  end

  def check_rate_limit(request, user)
    return true unless @config["rate_limiting"]["enabled"]

    limits = @config["rate_limiting"]["limits"]
    limit_key = determine_limit_key(user)
    limit_config = limits[limit_key] || limits["default"]

    RateLimiter.check(request.remote_ip, limit_config)
  end

  def apply_compression(response)
    return response unless @config["compression"]["enabled"]

    algorithms = @config["compression"]["algorithms"]
    # Apply compression based on accepted encoding
    response
  end

  def paginate_results(results, params)
    pagination_config = @config["pagination"]
    
    per_page = [
      params[pagination_config["per_page_param"]]&.to_i || pagination_config["default_per_page"],
      pagination_config["max_per_page"]
    ].min

    page = params[pagination_config["page_param"]]&.to_i || 1

    results.page(page).per(per_page)
  end

  private

  def determine_limit_key(user)
    return "admin" if user&.admin?
    return "authenticated" if user
    "default"
  end
end
```

## Testing and Validation

```ruby
# API testing configuration
testing_config = {
  "#api" => {
    "testing" => {
      "test_cases" => {
        "users" => {
          "GET /users" => {
            "valid_responses" => [200, 401],
            "test_data" => {
              "valid_user" => {
                "email" => "test@example.com",
                "password" => "password123"
              }
            }
          },
          "POST /users" => {
            "valid_responses" => [201, 400, 422],
            "test_scenarios" => [
              {
                "name" => "valid_user_creation",
                "data" => { "email" => "new@example.com", "password" => "password123" },
                "expected_status" => 201
              },
              {
                "name" => "invalid_email",
                "data" => { "email" => "invalid-email", "password" => "password123" },
                "expected_status" => 422
              }
            ]
          }
        }
      },
      "mock_data" => {
        "users" => [
          { "id" => 1, "email" => "user1@example.com", "name" => "User One" },
          { "id" => 2, "email" => "user2@example.com", "name" => "User Two" }
        ]
      }
    }
  }
}

# Ruby API testing framework
class ApiTestFramework
  def initialize(config)
    @config = config["#api"]["testing"]
  end

  def run_tests
    results = {}
    
    @config["test_cases"].each do |resource, endpoints|
      results[resource] = {}
      
      endpoints.each do |endpoint, test_config|
        results[resource][endpoint] = run_endpoint_tests(endpoint, test_config)
      end
    end

    results
  end

  def run_endpoint_tests(endpoint, config)
    method, path = endpoint.split(" ")
    tests = []

    # Run basic response tests
    config["valid_responses"].each do |status_code|
      test_result = test_response(method, path, status_code)
      tests << test_result
    end

    # Run scenario tests
    config["test_scenarios"]&.each do |scenario|
      test_result = run_scenario_test(method, path, scenario)
      tests << test_result
    end

    tests
  end

  def test_response(method, path, expected_status)
    response = make_request(method, path)
    
    {
      scenario: "status_#{expected_status}",
      expected_status: expected_status,
      actual_status: response.status,
      passed: response.status == expected_status,
      response_body: response.body
    }
  end

  def run_scenario_test(method, path, scenario)
    response = make_request(method, path, scenario["data"])
    
    {
      scenario: scenario["name"],
      expected_status: scenario["expected_status"],
      actual_status: response.status,
      passed: response.status == scenario["expected_status"],
      response_body: response.body
    }
  end

  private

  def make_request(method, path, data = nil)
    # Implementation would use Rack::Test or similar
    # This is a simplified example
    case method.upcase
    when "GET"
      # GET request
    when "POST"
      # POST request with data
    end
  end
end
```

## Integration with Ruby Frameworks

```ruby
# Rails integration example
class TuskApiController < ApplicationController
  include TuskApiProcessor

  def initialize
    @tusk_config = load_tusk_config
    @api_processor = ApiDirectiveProcessor.new(@tusk_config)
  end

  def process_request
    api_config = @api_processor.process_api_directive
    
    # Apply middleware
    apply_middleware(api_config[:middleware])
    
    # Validate request
    validate_request(api_config[:validation])
    
    # Execute handler
    execute_handler(api_config[:handler])
  end

  private

  def apply_middleware(middleware_list)
    middleware_list.each do |middleware|
      case middleware
      when "auth"
        authenticate_user!
      when /^rate_limit:(.+)$/
        apply_rate_limit($1)
      when "cors"
        set_cors_headers
      end
    end
  end

  def validate_request(validation_rules)
    validation_rules.each do |field, rules|
      validate_field(field, rules)
    end
  end

  def execute_handler(handler_config)
    controller_class = handler_config[:controller].constantize
    controller = controller_class.new
    controller.send(handler_config[:action])
  end

  def load_tusk_config
    TuskConfig.load("peanu.tsk")
  end
end

# Sinatra integration example
class TuskSinatraApp < Sinatra::Base
  include TuskApiProcessor

  configure do
    @tusk_config = TuskConfig.load("peanu.tsk")
    @api_processor = ApiDirectiveProcessor.new(@tusk_config)
  end

  before do
    process_api_directive
  end

  def process_api_directive
    api_config = @api_processor.process_api_directive
    return unless api_config

    # Apply middleware
    apply_sinatra_middleware(api_config[:middleware])
    
    # Set up validation
    setup_validation(api_config[:validation])
  end

  private

  def apply_sinatra_middleware(middleware_list)
    middleware_list.each do |middleware|
      case middleware
      when "auth"
        authenticate_user
      when "cors"
        enable_cors
      end
    end
  end

  def setup_validation(validation_rules)
    validation_rules.each do |field, rules|
      validate_field(field, rules)
    end
  end
end
```

## Best Practices and Patterns

```ruby
# Best practices for API directive usage
class ApiDirectiveBestPractices
  def self.validate_config(config)
    errors = []
    
    # Check required fields
    unless config["#api"]
      errors << "Missing #api directive"
      return errors
    end

    api_config = config["#api"]
    
    # Validate method
    unless %w[GET POST PUT DELETE PATCH].include?(api_config["method"])
      errors << "Invalid HTTP method: #{api_config['method']}"
    end

    # Validate path
    unless api_config["path"]&.start_with?("/")
      errors << "Path must start with /"
    end

    # Validate handler format
    if api_config["handler"] && !api_config["handler"].include?("#")
      errors << "Handler must be in format 'Controller#action'"
    end

    errors
  end

  def self.optimize_config(config)
    api_config = config["#api"]
    
    # Set defaults
    api_config["response_format"] ||= "json"
    api_config["middleware"] ||= []
    
    # Add common middleware
    unless api_config["middleware"].include?("cors")
      api_config["middleware"] << "cors"
    end

    config
  end

  def self.generate_documentation(config)
    api_config = config["#api"]
    
    {
      endpoint: "#{api_config['method']} #{api_config['path']}",
      handler: api_config['handler'],
      middleware: api_config['middleware'],
      validation: api_config['validation'],
      documentation: api_config['documentation']
    }
  end
end

# Usage example
config = {
  "#api" => {
    "method" => "POST",
    "path" => "/api/users",
    "handler" => "UsersController#create",
    "validation" => {
      "email" => "email|required",
      "password" => "string|min:8"
    }
  }
}

# Validate and optimize
errors = ApiDirectiveBestPractices.validate_config(config)
if errors.empty?
  optimized_config = ApiDirectiveBestPractices.optimize_config(config)
  documentation = ApiDirectiveBestPractices.generate_documentation(config)
  
  puts "Configuration is valid!"
  puts "Documentation: #{documentation}"
else
  puts "Configuration errors: #{errors}"
end
```

## Conclusion

The Hash API Directive in TuskLang represents a paradigm shift in API development. By combining declarative configuration with Ruby's powerful metaprogramming capabilities, you can create sophisticated, maintainable APIs with minimal boilerplate code.

Key benefits:
- **Declarative Configuration**: Define complete API behavior in simple hash structures
- **Ruby Integration**: Leverage Ruby's object-oriented and functional programming features
- **Validation and Error Handling**: Built-in support for comprehensive validation and error responses
- **Performance Optimization**: Automatic caching, rate limiting, and compression
- **Testing Support**: Integrated testing framework with mock data and scenarios
- **Documentation Generation**: Automatic OpenAPI specification generation

Remember, TuskLang is about breaking free from conventions and embracing the power of declarative, configuration-driven development. The Hash API Directive is your gateway to building APIs that are as expressive as they are powerful! 
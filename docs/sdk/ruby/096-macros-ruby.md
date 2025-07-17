# 💎 Macros in TuskLang - Ruby Edition

**"We don't bow to any king" - Metaprogramming with Ruby Power**

Macros in TuskLang provide powerful metaprogramming capabilities, allowing you to generate Ruby code at compile time and create domain-specific languages (DSLs). In Ruby, this integrates seamlessly with Rails, ActiveRecord, and provides advanced code generation patterns that go beyond traditional metaprogramming approaches.

## 🚀 Basic Macro Definition

### Simple Macros

```ruby
require 'tusklang'

# TuskLang configuration for basic macros
tsk_content = <<~TSK
  [macros]
  # Simple macro for logging
  @macro log_info(message) {
      return "Rails.logger.info('#{message}')"
  }
  
  # Macro with multiple parameters
  @macro debug_log(variable, label = null) {
      if (label) {
          return "Rails.logger.debug('#{label}: ' + #{variable}.inspect)"
      }
      return "Rails.logger.debug('#{variable}: ' + #{variable}.inspect)"
  }
  
  # Block macros for benchmarking
  @macro benchmark(block_name) {
      return """
          start_time = Time.current
          result = begin
              {block_name}
          end
          end_time = Time.current
          Rails.logger.info('Benchmark #{block_name}: ' + (end_time - start_time).inspect)
          result
      """
  }
  
  # Usage examples
  debug_user: @debug_log(user.name, "User Name")
  benchmark_operation: @benchmark {
      @expensive_operation()
  }
TSK

# Ruby implementation
class MacroManager
  include TuskLang::Macroable
  
  def execute_basic_macros
    tusk_config = Rails.application.config.tusk_config
    
    # Execute simple macros
    tusk_config.execute_macro('log_info', 'User created successfully')
    tusk_config.execute_macro('debug_log', 'user.name', 'User Name')
    
    # Execute benchmark macro
    result = tusk_config.execute_benchmark_macro('benchmark_operation') do
      expensive_operation
    end
    
    puts "Benchmark result: #{result}"
  end
  
  private
  
  def expensive_operation
    sleep(0.1) # Simulate expensive operation
    "Operation completed"
  end
end
```

### Code Generation Macros

```ruby
# TuskLang configuration for code generation
tsk_content = <<~TSK
  [code_generation]
  # Generate getter/setter methods
  @macro attr_accessor_ruby(name, type = "Object") {
      return """
          attr_accessor :#{name}
          
          def #{name}=(value)
              @#{name} = value
              self
          end
          
          def #{name}
              @#{name}
          end
      """
  }
  
  # Generate CRUD operations for ActiveRecord
  @macro crud_operations(model_name, table_name) {
      return """
          class #{model_name} < ApplicationRecord
              self.table_name = '#{table_name}'
              
              scope :active, -> { where(active: true) }
              scope :recent, -> { where('created_at > ?', 30.days.ago) }
              
              def self.find_active(id)
                  active.find(id)
              end
              
              def self.create_with_audit(attributes)
                  transaction do
                      record = create!(attributes)
                      AuditLog.create!(
                          action: 'create',
                          record_type: '#{model_name}',
                          record_id: record.id,
                          user_id: Current.user&.id
                      )
                      record
                  end
              end
              
              def update_with_audit(attributes)
                  transaction do
                      old_attributes = attributes_for_audit
                      result = update!(attributes)
                      AuditLog.create!(
                          action: 'update',
                          record_type: '#{model_name}',
                          record_id: id,
                          user_id: Current.user&.id,
                          changes: saved_changes
                      )
                      result
                  end
              end
              
              def destroy_with_audit
                  transaction do
                      AuditLog.create!(
                          action: 'destroy',
                          record_type: '#{model_name}',
                          record_id: id,
                          user_id: Current.user&.id
                      )
                      destroy!
                  end
              end
          end
      """
  }
  
  # Generate API endpoints
  @macro api_endpoints(resource_name) {
      return """
          class #{resource_name.capitalize}Controller < ApplicationController
              before_action :set_#{resource_name}, only: [:show, :update, :destroy]
              
              def index
                  @#{resource_name.pluralize} = #{resource_name.capitalize}.all
                  render json: @#{resource_name.pluralize}
              end
              
              def show
                  render json: @#{resource_name}
              end
              
              def create
                  @#{resource_name} = #{resource_name.capitalize}.create_with_audit(#{resource_name}_params)
                  if @#{resource_name}.save
                      render json: @#{resource_name}, status: :created
                  else
                      render json: @#{resource_name}.errors, status: :unprocessable_entity
                  end
              end
              
              def update
                  if @#{resource_name}.update_with_audit(#{resource_name}_params)
                      render json: @#{resource_name}
                  else
                      render json: @#{resource_name}.errors, status: :unprocessable_entity
                  end
              end
              
              def destroy
                  @#{resource_name}.destroy_with_audit
                  head :no_content
              end
              
              private
              
              def set_#{resource_name}
                  @#{resource_name} = #{resource_name.capitalize}.find(params[:id])
              end
              
              def #{resource_name}_params
                  params.require(:#{resource_name}).permit(:#{resource_name}_permitted_params)
              end
          end
      """
  }
TSK

# Ruby implementation for code generation
class CodeGenerator
  include TuskLang::Macroable
  
  def generate_model_code(model_name, table_name)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate CRUD operations
    model_code = tusk_config.execute_code_generation_macro('crud_operations', {
      model_name: model_name,
      table_name: table_name
    })
    
    # Write to file
    file_path = Rails.root.join('app', 'models', "#{model_name.underscore}.rb")
    File.write(file_path, model_code)
    
    puts "Generated model: #{file_path}"
  end
  
  def generate_controller_code(resource_name)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate API endpoints
    controller_code = tusk_config.execute_code_generation_macro('api_endpoints', {
      resource_name: resource_name
    })
    
    # Write to file
    file_path = Rails.root.join('app', 'controllers', "#{resource_name.pluralize}_controller.rb")
    File.write(file_path, controller_code)
    
    puts "Generated controller: #{file_path}"
  end
end
```

## 🔧 Advanced Macro Patterns

### AST Manipulation Macros

```ruby
# TuskLang configuration for AST manipulation
tsk_content = <<~TSK
  [ast_manipulation]
  # Macro for method memoization
  @macro memoize(method_name) {
      return """
          def #{method_name}(*args)
              cache_key = "#{method_name}:" + args.hash.to_s
              
              Rails.cache.fetch(cache_key, expires_in: 1.hour) do
                  super(*args)
              end
          end
      """
  }
  
  # Macro for automatic validation
  @macro validates_presence_of(field_name) {
      return """
          validates :#{field_name}, presence: true
          
          def #{field_name}_present?
              #{field_name}.present?
          end
      """
  }
  
  # Macro for automatic callbacks
  @macro before_save_callback(callback_name) {
      return """
          before_save :#{callback_name}
          
          private
          
          def #{callback_name}
              # Custom logic here
              true
          end
      """
  }
  
  # Macro for automatic associations
  @macro has_many_association(association_name, class_name = null) {
      if (class_name) {
          return "has_many :#{association_name}, class_name: '#{class_name}'"
      }
      return "has_many :#{association_name}"
  }
TSK

# Ruby implementation with AST manipulation
class ASTMacroManager
  include TuskLang::Macroable
  
  def apply_memoization_to_method(class_name, method_name)
    tusk_config = Rails.application.config.tusk_config
    
    # Apply memoization macro
    memoized_code = tusk_config.execute_ast_macro('memoize', {
      method_name: method_name
    })
    
    # Inject into class
    inject_code_into_class(class_name, memoized_code)
    
    puts "Applied memoization to #{class_name}##{method_name}"
  end
  
  def add_validations_to_model(model_name, validations)
    tusk_config = Rails.application.config.tusk_config
    
    validations.each do |validation_type, fields|
      fields.each do |field|
        validation_code = tusk_config.execute_ast_macro("validates_#{validation_type}_of", {
          field_name: field
        })
        
        inject_code_into_class(model_name, validation_code)
      end
    end
    
    puts "Added validations to #{model_name}"
  end
  
  private
  
  def inject_code_into_class(class_name, code)
    # Implementation for injecting code into existing classes
    # This would use Ruby's metaprogramming capabilities
    klass = class_name.constantize
    klass.class_eval(code)
  end
end
```

### Conditional Compilation Macros

```ruby
# TuskLang configuration for conditional compilation
tsk_content = <<~TSK
  [conditional_compilation]
  # Environment-specific code generation
  @macro env_specific(development_code, production_code) {
      env = @env("RAILS_ENV", "development")
      
      if (env == "development") {
          return development_code
      } else if (env == "production") {
          return production_code
      }
      
      return development_code
  }
  
  # Feature flag macros
  @macro feature_flag(flag_name, enabled_code, disabled_code = null) {
      flag_enabled = @feature_flag_enabled(flag_name)
      
      if (flag_enabled) {
          return enabled_code
      }
      
      return disabled_code || ""
  }
  
  # Debug mode macros
  @macro debug_only(code) {
      if (@env("RAILS_ENV") == "development") {
          return code
      }
      return ""
  }
  
  # Performance monitoring macros
  @macro performance_monitor(operation_name) {
      return """
          start_time = Time.current
          result = begin
              {operation_name}
          end
          end_time = Time.current
          
          if (end_time - start_time > 1.second)
              Rails.logger.warn("Slow operation: #{operation_name} took " + (end_time - start_time).inspect)
          end
          
          result
      """
  }
TSK

# Ruby implementation for conditional compilation
class ConditionalMacroManager
  include TuskLang::Macroable
  
  def generate_environment_specific_code
    tusk_config = Rails.application.config.tusk_config
    
    # Generate environment-specific code
    development_code = """
        def debug_info
            Rails.logger.debug("Debug info: " + inspect)
        end
    """
    
    production_code = """
        def debug_info
            # No debug info in production
        end
    """
    
    generated_code = tusk_config.execute_conditional_macro('env_specific', {
      development_code: development_code,
      production_code: production_code
    })
    
    puts "Generated environment-specific code: #{generated_code}"
  end
  
  def apply_feature_flag(flag_name, enabled_code, disabled_code = nil)
    tusk_config = Rails.application.config.tusk_config
    
    # Apply feature flag macro
    result_code = tusk_config.execute_conditional_macro('feature_flag', {
      flag_name: flag_name,
      enabled_code: enabled_code,
      disabled_code: disabled_code
    })
    
    puts "Applied feature flag #{flag_name}: #{result_code.present? ? 'enabled' : 'disabled'}"
  end
end
```

## 🏭 Rails Integration Macros

### ActiveRecord Macros

```ruby
# TuskLang configuration for ActiveRecord macros
tsk_content = <<~TSK
  [activerecord_macros]
  # Auto-generate scopes
  @macro auto_scopes(field_name) {
      return """
          scope :by_#{field_name}, ->(value) { where(#{field_name}: value) }
          scope :#{field_name}_not_null, -> { where.not(#{field_name}: nil) }
          scope :#{field_name}_null, -> { where(#{field_name}: nil) }
      """
  }
  
  # Auto-generate validations
  @macro auto_validations(field_name, validations = []) {
      validation_code = ""
      
      for (validation in validations) {
          if (validation == "presence") {
              validation_code += "validates :#{field_name}, presence: true\n"
          } else if (validation == "uniqueness") {
              validation_code += "validates :#{field_name}, uniqueness: true\n"
          } else if (validation == "email") {
              validation_code += "validates :#{field_name}, format: { with: URI::MailTo::EMAIL_REGEXP }\n"
          }
      }
      
      return validation_code
  }
  
  # Auto-generate callbacks
  @macro auto_callbacks(callback_type, callback_name) {
      return """
          #{callback_type} :#{callback_name}
          
          private
          
          def #{callback_name}
              # Custom logic for #{callback_name}
              true
          end
      """
  }
  
  # Auto-generate search methods
  @macro searchable(fields) {
      search_code = """
          def self.search(query)
              return all if query.blank?
              
              conditions = []
              values = []
      """
      
      for (field in fields) {
          search_code += """
              conditions << "#{field} LIKE ?"
              values << "%#{query}%"
          """
      }
      
      search_code += """
              where(conditions.join(" OR "), *values)
          end
      """
      
      return search_code
  }
TSK

# Ruby implementation for ActiveRecord macros
class ActiveRecordMacroManager
  include TuskLang::Macroable
  
  def add_auto_scopes_to_model(model_name, field_name)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate auto scopes
    scope_code = tusk_config.execute_activerecord_macro('auto_scopes', {
      field_name: field_name
    })
    
    # Apply to model
    apply_code_to_model(model_name, scope_code)
    
    puts "Added auto scopes to #{model_name} for field #{field_name}"
  end
  
  def add_auto_validations_to_model(model_name, field_name, validations)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate auto validations
    validation_code = tusk_config.execute_activerecord_macro('auto_validations', {
      field_name: field_name,
      validations: validations
    })
    
    # Apply to model
    apply_code_to_model(model_name, validation_code)
    
    puts "Added auto validations to #{model_name} for field #{field_name}"
  end
  
  def make_model_searchable(model_name, searchable_fields)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate search method
    search_code = tusk_config.execute_activerecord_macro('searchable', {
      fields: searchable_fields
    })
    
    # Apply to model
    apply_code_to_model(model_name, search_code)
    
    puts "Made #{model_name} searchable with fields: #{searchable_fields.join(', ')}"
  end
  
  private
  
  def apply_code_to_model(model_name, code)
    klass = model_name.constantize
    klass.class_eval(code)
  end
end
```

### Controller Macros

```ruby
# TuskLang configuration for controller macros
tsk_content = <<~TSK
  [controller_macros]
  # Auto-generate RESTful actions
  @macro restful_actions(resource_name) {
      return """
          def index
              @#{resource_name.pluralize} = #{resource_name.capitalize}.all
              respond_to do |format|
                  format.html
                  format.json { render json: @#{resource_name.pluralize} }
              end
          end
          
          def show
              @#{resource_name} = #{resource_name.capitalize}.find(params[:id])
              respond_to do |format|
                  format.html
                  format.json { render json: @#{resource_name} }
              end
          end
          
          def new
              @#{resource_name} = #{resource_name.capitalize}.new
          end
          
          def create
              @#{resource_name} = #{resource_name.capitalize}.new(#{resource_name}_params)
              if @#{resource_name}.save
                  redirect_to @#{resource_name}, notice: '#{resource_name.capitalize} was successfully created.'
              else
                  render :new
              end
          end
          
          def edit
              @#{resource_name} = #{resource_name.capitalize}.find(params[:id])
          end
          
          def update
              @#{resource_name} = #{resource_name.capitalize}.find(params[:id])
              if @#{resource_name}.update(#{resource_name}_params)
                  redirect_to @#{resource_name}, notice: '#{resource_name.capitalize} was successfully updated.'
              else
                  render :edit
              end
          end
          
          def destroy
              @#{resource_name} = #{resource_name.capitalize}.find(params[:id])
              @#{resource_name}.destroy
              redirect_to #{resource_name.pluralize}_url, notice: '#{resource_name.capitalize} was successfully destroyed.'
          end
          
          private
          
          def #{resource_name}_params
              params.require(:#{resource_name}).permit(:#{resource_name}_permitted_params)
          end
      """
  }
  
  # Auto-generate API actions
  @macro api_actions(resource_name) {
      return """
          def index
              @#{resource_name.pluralize} = #{resource_name.capitalize}.all
              render json: @#{resource_name.pluralize}
          end
          
          def show
              @#{resource_name} = #{resource_name.capitalize}.find(params[:id])
              render json: @#{resource_name}
          rescue ActiveRecord::RecordNotFound
              render json: { error: 'Not found' }, status: :not_found
          end
          
          def create
              @#{resource_name} = #{resource_name.capitalize}.new(#{resource_name}_params)
              if @#{resource_name}.save
                  render json: @#{resource_name}, status: :created
              else
                  render json: @#{resource_name}.errors, status: :unprocessable_entity
              end
          end
          
          def update
              @#{resource_name} = #{resource_name.capitalize}.find(params[:id])
              if @#{resource_name}.update(#{resource_name}_params)
                  render json: @#{resource_name}
              else
                  render json: @#{resource_name}.errors, status: :unprocessable_entity
              end
          rescue ActiveRecord::RecordNotFound
              render json: { error: 'Not found' }, status: :not_found
          end
          
          def destroy
              @#{resource_name} = #{resource_name.capitalize}.find(params[:id])
              @#{resource_name}.destroy
              head :no_content
          rescue ActiveRecord::RecordNotFound
              render json: { error: 'Not found' }, status: :not_found
          end
      """
  }
TSK

# Ruby implementation for controller macros
class ControllerMacroManager
  include TuskLang::Macroable
  
  def generate_restful_controller(resource_name)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate RESTful actions
    controller_code = tusk_config.execute_controller_macro('restful_actions', {
      resource_name: resource_name
    })
    
    # Create controller file
    file_path = Rails.root.join('app', 'controllers', "#{resource_name.pluralize}_controller.rb")
    controller_class = """
        class #{resource_name.pluralize.capitalize}Controller < ApplicationController
            #{controller_code}
        end
    """
    
    File.write(file_path, controller_class)
    
    puts "Generated RESTful controller: #{file_path}"
  end
  
  def generate_api_controller(resource_name)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate API actions
    api_code = tusk_config.execute_controller_macro('api_actions', {
      resource_name: resource_name
    })
    
    # Create API controller file
    file_path = Rails.root.join('app', 'controllers', 'api', "#{resource_name.pluralize}_controller.rb")
    api_controller_class = """
        class Api::#{resource_name.pluralize.capitalize}Controller < ApplicationController
            #{api_code}
            
            private
            
            def #{resource_name}_params
                params.require(:#{resource_name}).permit(:#{resource_name}_permitted_params)
            end
        end
    """
    
    File.write(file_path, api_controller_class)
    
    puts "Generated API controller: #{file_path}"
  end
end
```

## 🧪 Testing and Validation

### Macro Testing

```ruby
# TuskLang configuration for testing macros
tsk_content = <<~TSK
  [macro_testing]
  # Test macro expansion
  @macro test_macro_expansion(macro_name, input_params, expected_output) {
      expanded_code = @expand_macro(macro_name, input_params)
      
      if (expanded_code == expected_output) {
          return { success: true, message: "Macro expansion test passed" }
      } else {
          return { 
              success: false, 
              message: "Macro expansion test failed",
              expected: expected_output,
              actual: expanded_code
          }
      }
  }
  
  # Test macro execution
  @macro test_macro_execution(macro_name, input_params, expected_result) {
      result = @execute_macro(macro_name, input_params)
      
      if (result == expected_result) {
          return { success: true, message: "Macro execution test passed" }
      } else {
          return { 
              success: false, 
              message: "Macro execution test failed",
              expected: expected_result,
              actual: result
          }
      }
  }
  
  # Performance test for macros
  @macro test_macro_performance(macro_name, input_params, max_execution_time = "100ms") {
      start_time = @microtime(true)
      result = @execute_macro(macro_name, input_params)
      end_time = @microtime(true)
      
      execution_time = end_time - start_time
      
      if (execution_time <= max_execution_time) {
          return { 
              success: true, 
              message: "Macro performance test passed",
              execution_time: execution_time
          }
      } else {
          return { 
              success: false, 
              message: "Macro performance test failed",
              execution_time: execution_time,
              max_allowed: max_execution_time
          }
      }
  }
TSK

# Ruby implementation for macro testing
class MacroTester
  include TuskLang::Macroable
  
  def test_macro_expansion(macro_name, input_params, expected_output)
    tusk_config = Rails.application.config.tusk_config
    
    # Test macro expansion
    result = tusk_config.execute_macro_test('test_macro_expansion', {
      macro_name: macro_name,
      input_params: input_params,
      expected_output: expected_output
    })
    
    if result[:success]
      puts "✅ #{result[:message]}"
    else
      puts "❌ #{result[:message]}"
      puts "Expected: #{result[:expected]}"
      puts "Actual: #{result[:actual]}"
    end
  end
  
  def test_macro_performance(macro_name, input_params, max_execution_time = 0.1)
    tusk_config = Rails.application.config.tusk_config
    
    # Test macro performance
    result = tusk_config.execute_macro_test('test_macro_performance', {
      macro_name: macro_name,
      input_params: input_params,
      max_execution_time: max_execution_time
    })
    
    if result[:success]
      puts "✅ #{result[:message]} (#{result[:execution_time]}s)"
    else
      puts "❌ #{result[:message]}"
      puts "Execution time: #{result[:execution_time]}s"
      puts "Max allowed: #{result[:max_allowed]}s"
    end
  end
end

# RSpec tests for macros
RSpec.describe MacroManager, type: :model do
  let(:macro_manager) { MacroManager.new }
  
  describe '#execute_basic_macros' do
    it 'executes logging macros correctly' do
      expect {
        macro_manager.execute_basic_macros
      }.not_to raise_error
    end
  end
end

RSpec.describe CodeGenerator, type: :model do
  let(:code_generator) { CodeGenerator.new }
  
  describe '#generate_model_code' do
    it 'generates valid model code' do
      expect {
        code_generator.generate_model_code('Product', 'products')
      }.not_to raise_error
      
      # Verify file was created
      expect(File.exist?(Rails.root.join('app', 'models', 'product.rb'))).to be true
    end
  end
end
```

## 🔧 Rails Integration

### Rails Macro Configuration

```ruby
# config/initializers/tusk_macros.rb
Rails.application.config.after_initialize do
  TuskLang.configure do |config|
    # Configure macro settings
    config.macro_settings = {
      enable_code_generation: true,
      enable_ast_manipulation: true,
      enable_conditional_compilation: true,
      macro_cache_enabled: true,
      macro_cache_ttl: 1.hour
    }
    
    # Configure macro paths
    config.macro_paths = [
      Rails.root.join('app', 'macros'),
      Rails.root.join('lib', 'macros'),
      Rails.root.join('config', 'macros')
    ]
    
    # Configure macro security
    config.macro_security = {
      allow_file_operations: Rails.env.development?,
      allow_system_commands: false,
      allowed_macro_names: ['log_info', 'debug_log', 'benchmark']
    }
  end
end

# app/models/concerns/tusk_macroable.rb
module TuskMacroable
  extend ActiveSupport::Concern
  
  included do
    include TuskLang::Macroable
  end
  
  private
  
  def tusk_config
    Rails.application.config.tusk_config
  end
end
```

### Custom Rake Tasks

```ruby
# lib/tasks/macros.rake
namespace :macros do
  desc "Generate code using TuskLang macros"
  task generate: :environment do
    generator = CodeGenerator.new
    generator.generate_model_code('Product', 'products')
    generator.generate_controller_code('product')
    puts "Code generation completed"
  end
  
  desc "Test all macros"
  task test: :environment do
    tester = MacroTester.new
    
    # Test basic macros
    tester.test_macro_expansion('log_info', ['test message'], "Rails.logger.info('test message')")
    tester.test_macro_performance('benchmark', ['sleep(0.1)'], 0.2)
    
    puts "Macro testing completed"
  end
  
  desc "Apply ActiveRecord macros to models"
  task apply_activerecord: :environment do
    manager = ActiveRecordMacroManager.new
    
    # Apply macros to User model
    manager.add_auto_scopes_to_model('User', 'email')
    manager.add_auto_validations_to_model('User', 'email', ['presence', 'uniqueness', 'email'])
    manager.make_model_searchable('User', ['name', 'email'])
    
    puts "ActiveRecord macros applied"
  end
end
```

## 🎯 Summary

TuskLang's macro system in Ruby provides:

- **Powerful metaprogramming** capabilities with Ruby integration
- **Code generation** for models, controllers, and APIs
- **AST manipulation** for method memoization and validation
- **Conditional compilation** based on environment and feature flags
- **Rails integration** with ActiveRecord and controller macros
- **Testing frameworks** for macro validation and performance
- **Security features** for safe macro execution
- **Custom rake tasks** for macro management

The Ruby implementation maintains TuskLang's rebellious spirit while providing enterprise-grade metaprogramming capabilities that "don't bow to any king" - not even the constraints of traditional code generation patterns.

**Ready to revolutionize your Ruby application's metaprogramming with TuskLang?** 🚀 
# 💎 Metaprogramming in TuskLang - Ruby Edition

**"We don't bow to any king" - Dynamic Code Generation with Ruby Magic**

Metaprogramming in TuskLang provides powerful dynamic code generation capabilities, allowing you to create, modify, and extend Ruby classes and methods at runtime. In Ruby, this integrates seamlessly with Rails, ActiveRecord, and provides advanced metaprogramming patterns that leverage Ruby's dynamic nature.

## 🚀 Basic Metaprogramming

### Dynamic Method Creation

```ruby
require 'tusklang'

# TuskLang configuration for dynamic method creation
tsk_content = <<~TSK
  [metaprogramming]
  # Dynamic method generation
  @metaprogramming.generate_method(class_name, method_name, method_body) {
      return """
          class #{class_name}
              def #{method_name}
                  #{method_body}
              end
          end
      """
  }
  
  # Dynamic attribute accessors
  @metaprogramming.generate_accessors(class_name, attributes) {
      accessor_code = ""
      
      for (attr in attributes) {
          accessor_code += """
              attr_accessor :#{attr}
              
              def #{attr}=(value)
                  @#{attr} = value
                  self
              end
              
              def #{attr}
                  @#{attr}
              end
          """
      }
      
      return """
          class #{class_name}
              #{accessor_code}
          end
      """
  }
  
  # Dynamic validation methods
  @metaprogramming.generate_validations(class_name, validations) {
      validation_code = ""
      
      for (field, rules in validations) {
          for (rule in rules) {
              if (rule == "presence") {
                  validation_code += "validates :#{field}, presence: true\n"
              } else if (rule == "uniqueness") {
                  validation_code += "validates :#{field}, uniqueness: true\n"
              } else if (rule == "email") {
                  validation_code += "validates :#{field}, format: { with: URI::MailTo::EMAIL_REGEXP }\n"
              }
          }
      }
      
      return """
          class #{class_name}
              #{validation_code}
          end
      """
  }
TSK

# Ruby implementation
class MetaprogrammingManager
  include TuskLang::Metaprogrammable
  
  def create_dynamic_method(class_name, method_name, method_body)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate dynamic method
    method_code = tusk_config.execute_metaprogramming('generate_method', {
      class_name: class_name,
      method_name: method_name,
      method_body: method_body
    })
    
    # Apply to class
    apply_code_to_class(class_name, method_code)
    
    puts "Created dynamic method #{method_name} in #{class_name}"
  end
  
  def create_dynamic_accessors(class_name, attributes)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate accessors
    accessor_code = tusk_config.execute_metaprogramming('generate_accessors', {
      class_name: class_name,
      attributes: attributes
    })
    
    # Apply to class
    apply_code_to_class(class_name, accessor_code)
    
    puts "Created accessors for #{attributes.join(', ')} in #{class_name}"
  end
  
  def create_dynamic_validations(class_name, validations)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate validations
    validation_code = tusk_config.execute_metaprogramming('generate_validations', {
      class_name: class_name,
      validations: validations
    })
    
    # Apply to class
    apply_code_to_class(class_name, validation_code)
    
    puts "Created validations for #{class_name}"
  end
  
  private
  
  def apply_code_to_class(class_name, code)
    klass = class_name.constantize
    klass.class_eval(code)
  end
end
```

### Dynamic Class Creation

```ruby
# TuskLang configuration for dynamic class creation
tsk_content = <<~TSK
  [dynamic_classes]
  # Generate ActiveRecord models dynamically
  @metaprogramming.generate_model(model_name, table_name, attributes) {
      attribute_code = ""
      association_code = ""
      
      for (attr_name, attr_type in attributes) {
          if (attr_type == "association") {
              association_code += "has_many :#{attr_name}\n"
          } else {
              attribute_code += "attr_accessor :#{attr_name}\n"
          }
      }
      
      return """
          class #{model_name} < ApplicationRecord
              self.table_name = '#{table_name}'
              
              #{attribute_code}
              #{association_code}
              
              scope :active, -> { where(active: true) }
              scope :recent, -> { where('created_at > ?', 30.days.ago) }
              
              def self.find_by_custom_criteria(criteria)
                  where(criteria)
              end
              
              def custom_method
                  "Custom method for #{model_name}"
              end
          end
      """
  }
  
  # Generate service classes dynamically
  @metaprogramming.generate_service(service_name, methods) {
      method_code = ""
      
      for (method_name, method_body in methods) {
          method_code += """
              def #{method_name}
                  #{method_body}
              end
          """
      }
      
      return """
          class #{service_name}Service
              include TuskLang::Serviceable
              
              #{method_code}
              
              private
              
              def logger
                  Rails.logger
              end
          end
      """
  }
  
  # Generate controller classes dynamically
  @metaprogramming.generate_controller(controller_name, actions) {
      action_code = ""
      
      for (action_name, action_body in actions) {
          action_code += """
              def #{action_name}
                  #{action_body}
              end
          """
      }
      
      return """
          class #{controller_name}Controller < ApplicationController
              before_action :authenticate_user!
              
              #{action_code}
              
              private
              
              def set_resource
                  @resource = #{controller_name.singularize.capitalize}.find(params[:id])
              end
          end
      """
  }
TSK

# Ruby implementation for dynamic class creation
class DynamicClassGenerator
  include TuskLang::Metaprogrammable
  
  def generate_dynamic_model(model_name, table_name, attributes)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate model code
    model_code = tusk_config.execute_metaprogramming('generate_model', {
      model_name: model_name,
      table_name: table_name,
      attributes: attributes
    })
    
    # Write to file
    file_path = Rails.root.join('app', 'models', "#{model_name.underscore}.rb")
    File.write(file_path, model_code)
    
    puts "Generated dynamic model: #{file_path}"
  end
  
  def generate_dynamic_service(service_name, methods)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate service code
    service_code = tusk_config.execute_metaprogramming('generate_service', {
      service_name: service_name,
      methods: methods
    })
    
    # Write to file
    file_path = Rails.root.join('app', 'services', "#{service_name.underscore}_service.rb")
    File.write(file_path, service_code)
    
    puts "Generated dynamic service: #{file_path}"
  end
  
  def generate_dynamic_controller(controller_name, actions)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate controller code
    controller_code = tusk_config.execute_metaprogramming('generate_controller', {
      controller_name: controller_name,
      actions: actions
    })
    
    # Write to file
    file_path = Rails.root.join('app', 'controllers', "#{controller_name.underscore}_controller.rb")
    File.write(file_path, controller_code)
    
    puts "Generated dynamic controller: #{file_path}"
  end
end
```

## 🔧 Advanced Metaprogramming Patterns

### Method Missing and Dynamic Dispatch

```ruby
# TuskLang configuration for method missing
tsk_content = <<~TSK
  [method_missing]
  # Dynamic method dispatch
  @metaprogramming.method_missing_handler(class_name, pattern, handler) {
      return """
          class #{class_name}
              def method_missing(method_name, *args, &block)
                  if method_name.to_s.match(/#{pattern}/)
                      #{handler}
                  else
                      super
                  end
              end
              
              def respond_to_missing?(method_name, include_private = false)
                  method_name.to_s.match(/#{pattern}/) || super
              end
          end
      """
  }
  
  # Dynamic finder methods
  @metaprogramming.generate_finders(class_name, fields) {
      finder_code = ""
      
      for (field in fields) {
          finder_code += """
              def self.find_by_#{field}(value)
                  where(#{field}: value).first
              end
              
              def self.find_all_by_#{field}(value)
                  where(#{field}: value)
              end
          """
      }
      
      return """
          class #{class_name}
              #{finder_code}
          end
      """
  }
  
  # Dynamic scope generation
  @metaprogramming.generate_scopes(class_name, scopes) {
      scope_code = ""
      
      for (scope_name, scope_definition in scopes) {
          scope_code += """
              scope :#{scope_name}, -> { #{scope_definition} }
          """
      }
      
      return """
          class #{class_name}
              #{scope_code}
          end
      """
  }
TSK

# Ruby implementation for method missing
class MethodMissingManager
  include TuskLang::Metaprogrammable
  
  def add_method_missing_handler(class_name, pattern, handler)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate method missing handler
    handler_code = tusk_config.execute_metaprogramming('method_missing_handler', {
      class_name: class_name,
      pattern: pattern,
      handler: handler
    })
    
    # Apply to class
    apply_code_to_class(class_name, handler_code)
    
    puts "Added method missing handler to #{class_name}"
  end
  
  def generate_dynamic_finders(class_name, fields)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate finder methods
    finder_code = tusk_config.execute_metaprogramming('generate_finders', {
      class_name: class_name,
      fields: fields
    })
    
    # Apply to class
    apply_code_to_class(class_name, finder_code)
    
    puts "Generated finder methods for #{fields.join(', ')} in #{class_name}"
  end
  
  def generate_dynamic_scopes(class_name, scopes)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate scopes
    scope_code = tusk_config.execute_metaprogramming('generate_scopes', {
      class_name: class_name,
      scopes: scopes
    })
    
    # Apply to class
    apply_code_to_class(class_name, scope_code)
    
    puts "Generated scopes for #{class_name}"
  end
  
  private
  
  def apply_code_to_class(class_name, code)
    klass = class_name.constantize
    klass.class_eval(code)
  end
end
```

### Module and Concern Generation

```ruby
# TuskLang configuration for module generation
tsk_content = <<~TSK
  [module_generation]
  # Generate concerns dynamically
  @metaprogramming.generate_concern(concern_name, methods) {
      method_code = ""
      
      for (method_name, method_body in methods) {
          method_code += """
              def #{method_name}
                  #{method_body}
              end
          """
      }
      
      return """
          module #{concern_name}
              extend ActiveSupport::Concern
              
              included do
                  #{method_code}
              end
              
              class_methods do
                  def concern_class_method
                      "Class method from #{concern_name}"
                  end
              end
          end
      """
  }
  
  # Generate mixins dynamically
  @metaprogramming.generate_mixin(mixin_name, methods, class_methods = {}) {
      instance_method_code = ""
      class_method_code = ""
      
      for (method_name, method_body in methods) {
          instance_method_code += """
              def #{method_name}
                  #{method_body}
              end
          """
      }
      
      for (method_name, method_body in class_methods) {
          class_method_code += """
              def self.#{method_name}
                  #{method_body}
              end
          """
      }
      
      return """
          module #{mixin_name}
              #{instance_method_code}
              
              #{class_method_code}
          end
      """
  }
  
  # Generate traits dynamically
  @metaprogramming.generate_trait(trait_name, methods, validations = {}) {
      method_code = ""
      validation_code = ""
      
      for (method_name, method_body in methods) {
          method_code += """
              def #{method_name}
                  #{method_body}
              end
          """
      }
      
      for (field, rules in validations) {
          for (rule in rules) {
              validation_code += "validates :#{field}, #{rule}\n"
          }
      }
      
      return """
          module #{trait_name}
              extend ActiveSupport::Concern
              
              included do
                  #{validation_code}
              end
              
              #{method_code}
          end
      """
  }
TSK

# Ruby implementation for module generation
class ModuleGenerator
  include TuskLang::Metaprogrammable
  
  def generate_dynamic_concern(concern_name, methods)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate concern code
    concern_code = tusk_config.execute_metaprogramming('generate_concern', {
      concern_name: concern_name,
      methods: methods
    })
    
    # Write to file
    file_path = Rails.root.join('app', 'models', 'concerns', "#{concern_name.underscore}.rb")
    File.write(file_path, concern_code)
    
    puts "Generated dynamic concern: #{file_path}"
  end
  
  def generate_dynamic_mixin(mixin_name, methods, class_methods = {})
    tusk_config = Rails.application.config.tusk_config
    
    # Generate mixin code
    mixin_code = tusk_config.execute_metaprogramming('generate_mixin', {
      mixin_name: mixin_name,
      methods: methods,
      class_methods: class_methods
    })
    
    # Write to file
    file_path = Rails.root.join('lib', 'mixins', "#{mixin_name.underscore}.rb")
    File.write(file_path, mixin_code)
    
    puts "Generated dynamic mixin: #{file_path}"
  end
  
  def generate_dynamic_trait(trait_name, methods, validations = {})
    tusk_config = Rails.application.config.tusk_config
    
    # Generate trait code
    trait_code = tusk_config.execute_metaprogramming('generate_trait', {
      trait_name: trait_name,
      methods: methods,
      validations: validations
    })
    
    # Write to file
    file_path = Rails.root.join('app', 'models', 'traits', "#{trait_name.underscore}.rb")
    File.write(file_path, trait_code)
    
    puts "Generated dynamic trait: #{file_path}"
  end
end
```

## 🏭 Rails Integration Metaprogramming

### ActiveRecord Metaprogramming

```ruby
# TuskLang configuration for ActiveRecord metaprogramming
tsk_content = <<~TSK
  [activerecord_metaprogramming]
  # Dynamic association generation
  @metaprogramming.generate_associations(class_name, associations) {
      association_code = ""
      
      for (association_name, association_config in associations) {
          if (association_config.type == "has_many") {
              association_code += "has_many :#{association_name}"
          } else if (association_config.type == "belongs_to") {
              association_code += "belongs_to :#{association_name}"
          } else if (association_config.type == "has_one") {
              association_code += "has_one :#{association_name}"
          }
          
          if (association_config.class_name) {
              association_code += ", class_name: '#{association_config.class_name}'"
          }
          
          if (association_config.foreign_key) {
              association_code += ", foreign_key: '#{association_config.foreign_key}'"
          }
          
          association_code += "\n"
      }
      
      return """
          class #{class_name} < ApplicationRecord
              #{association_code}
          end
      """
  }
  
  # Dynamic callback generation
  @metaprogramming.generate_callbacks(class_name, callbacks) {
      callback_code = ""
      
      for (callback_type, callback_methods in callbacks) {
          for (callback_method in callback_methods) {
              callback_code += "#{callback_type} :#{callback_method}\n"
          }
      }
      
      return """
          class #{class_name} < ApplicationRecord
              #{callback_code}
              
              private
              
              def after_create_callback
                  # Custom after_create logic
              end
              
              def before_save_callback
                  # Custom before_save logic
              end
          end
      """
  }
  
  # Dynamic scope generation with parameters
  @metaprogramming.generate_parameterized_scopes(class_name, scopes) {
      scope_code = ""
      
      for (scope_name, scope_config in scopes) {
          if (scope_config.parameters) {
              scope_code += """
                  scope :#{scope_name}, ->(#{scope_config.parameters.join(', ')}) {
                      #{scope_config.definition}
                  }
              """
          } else {
              scope_code += """
                  scope :#{scope_name}, -> {
                      #{scope_config.definition}
                  }
              """
          }
      }
      
      return """
          class #{class_name} < ApplicationRecord
              #{scope_code}
          end
      """
  }
TSK

# Ruby implementation for ActiveRecord metaprogramming
class ActiveRecordMetaprogramming
  include TuskLang::Metaprogrammable
  
  def generate_dynamic_associations(class_name, associations)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate associations
    association_code = tusk_config.execute_metaprogramming('generate_associations', {
      class_name: class_name,
      associations: associations
    })
    
    # Apply to class
    apply_code_to_class(class_name, association_code)
    
    puts "Generated associations for #{class_name}"
  end
  
  def generate_dynamic_callbacks(class_name, callbacks)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate callbacks
    callback_code = tusk_config.execute_metaprogramming('generate_callbacks', {
      class_name: class_name,
      callbacks: callbacks
    })
    
    # Apply to class
    apply_code_to_class(class_name, callback_code)
    
    puts "Generated callbacks for #{class_name}"
  end
  
  def generate_parameterized_scopes(class_name, scopes)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate parameterized scopes
    scope_code = tusk_config.execute_metaprogramming('generate_parameterized_scopes', {
      class_name: class_name,
      scopes: scopes
    })
    
    # Apply to class
    apply_code_to_class(class_name, scope_code)
    
    puts "Generated parameterized scopes for #{class_name}"
  end
  
  private
  
  def apply_code_to_class(class_name, code)
    klass = class_name.constantize
    klass.class_eval(code)
  end
end
```

### Controller Metaprogramming

```ruby
# TuskLang configuration for controller metaprogramming
tsk_content = <<~TSK
  [controller_metaprogramming]
  # Dynamic action generation
  @metaprogramming.generate_actions(controller_name, actions) {
      action_code = ""
      
      for (action_name, action_config in actions) {
          action_code += """
              def #{action_name}
                  #{action_config.body}
              end
          """
          
          if (action_config.before_action) {
              action_code += "before_action :#{action_config.before_action}, only: [:#{action_name}]\n"
          }
      }
      
      return """
          class #{controller_name}Controller < ApplicationController
              #{action_code}
          end
      """
  }
  
  # Dynamic filter generation
  @metaprogramming.generate_filters(controller_name, filters) {
      filter_code = ""
      
      for (filter_type, filter_config in filters) {
          if (filter_config.only) {
              filter_code += "#{filter_type} :#{filter_config.method}, only: [:#{filter_config.only.join(', :')}]\n"
          } else if (filter_config.except) {
              filter_code += "#{filter_type} :#{filter_config.method}, except: [:#{filter_config.except.join(', :')}]\n"
          } else {
              filter_code += "#{filter_type} :#{filter_config.method}\n"
          }
      }
      
      return """
          class #{controller_name}Controller < ApplicationController
              #{filter_code}
              
              private
              
              def authenticate_user
                  # Authentication logic
              end
              
              def authorize_resource
                  # Authorization logic
              end
          end
      """
  }
  
  # Dynamic response format generation
  @metaprogramming.generate_response_formats(controller_name, formats) {
      format_code = ""
      
      for (action_name, format_config in formats) {
          format_code += """
              def #{action_name}
                  @resource = #{controller_name.singularize.capitalize}.find(params[:id])
                  
                  respond_to do |format|
          """
          
          for (format_type, response in format_config) {
              if (format_type == "html") {
                  format_code += "format.html { #{response} }\n"
              } else if (format_type == "json") {
                  format_code += "format.json { #{response} }\n"
              } else if (format_type == "xml") {
                  format_code += "format.xml { #{response} }\n"
              }
          }
          
          format_code += """
                  end
              end
          """
      }
      
      return """
          class #{controller_name}Controller < ApplicationController
              #{format_code}
          end
      """
  }
TSK

# Ruby implementation for controller metaprogramming
class ControllerMetaprogramming
  include TuskLang::Metaprogrammable
  
  def generate_dynamic_actions(controller_name, actions)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate actions
    action_code = tusk_config.execute_metaprogramming('generate_actions', {
      controller_name: controller_name,
      actions: actions
    })
    
    # Apply to controller
    apply_code_to_controller(controller_name, action_code)
    
    puts "Generated actions for #{controller_name}Controller"
  end
  
  def generate_dynamic_filters(controller_name, filters)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate filters
    filter_code = tusk_config.execute_metaprogramming('generate_filters', {
      controller_name: controller_name,
      filters: filters
    })
    
    # Apply to controller
    apply_code_to_controller(controller_name, filter_code)
    
    puts "Generated filters for #{controller_name}Controller"
  end
  
  def generate_response_formats(controller_name, formats)
    tusk_config = Rails.application.config.tusk_config
    
    # Generate response formats
    format_code = tusk_config.execute_metaprogramming('generate_response_formats', {
      controller_name: controller_name,
      formats: formats
    })
    
    # Apply to controller
    apply_code_to_controller(controller_name, format_code)
    
    puts "Generated response formats for #{controller_name}Controller"
  end
  
  private
  
  def apply_code_to_controller(controller_name, code)
    controller_class = "#{controller_name}Controller".constantize
    controller_class.class_eval(code)
  end
end
```

## 🧪 Testing and Validation

### Metaprogramming Testing

```ruby
# TuskLang configuration for testing metaprogramming
tsk_content = <<~TSK
  [metaprogramming_testing]
  # Test dynamic method generation
  @metaprogramming.test_method_generation(class_name, method_name, test_input, expected_output) {
      # Generate method
      method_code = @metaprogramming.generate_method(class_name, method_name, "return #{expected_output}")
      
      # Apply method
      @apply_code_to_class(class_name, method_code)
      
      # Test method
      result = @test_method_call(class_name, method_name, test_input)
      
      if (result == expected_output) {
          return { success: true, message: "Method generation test passed" }
      } else {
          return { 
              success: false, 
              message: "Method generation test failed",
              expected: expected_output,
              actual: result
          }
      }
  }
  
  # Test dynamic class generation
  @metaprogramming.test_class_generation(class_name, class_config, test_methods) {
      # Generate class
      class_code = @metaprogramming.generate_model(class_name, "test_table", class_config.attributes)
      
      # Write class to temporary file
      temp_file = @write_temp_file(class_name, class_code)
      
      # Load class
      @load_class_from_file(temp_file)
      
      # Test methods
      test_results = {}
      
      for (method_name, test_config in test_methods) {
          result = @test_method_call(class_name, method_name, test_config.input)
          test_results[method_name] = result == test_config.expected
      }
      
      # Cleanup
      @delete_temp_file(temp_file)
      
      return { success: true, results: test_results }
  }
  
  # Test performance of metaprogramming
  @metaprogramming.test_metaprogramming_performance(operation, iterations = 1000) {
      start_time = @microtime(true)
      
      for (i = 0; i < iterations; i++) {
          @execute_metaprogramming_operation(operation)
      }
      
      end_time = @microtime(true)
      execution_time = end_time - start_time
      
      return {
          success: true,
          iterations: iterations,
          execution_time: execution_time,
          average_time: execution_time / iterations
      }
  }
TSK

# Ruby implementation for metaprogramming testing
class MetaprogrammingTester
  include TuskLang::Metaprogrammable
  
  def test_method_generation(class_name, method_name, test_input, expected_output)
    tusk_config = Rails.application.config.tusk_config
    
    # Test method generation
    result = tusk_config.execute_metaprogramming_test('test_method_generation', {
      class_name: class_name,
      method_name: method_name,
      test_input: test_input,
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
  
  def test_class_generation(class_name, class_config, test_methods)
    tusk_config = Rails.application.config.tusk_config
    
    # Test class generation
    result = tusk_config.execute_metaprogramming_test('test_class_generation', {
      class_name: class_name,
      class_config: class_config,
      test_methods: test_methods
    })
    
    if result[:success]
      puts "✅ Class generation test passed"
      result[:results].each do |method_name, passed|
        status = passed ? "✅" : "❌"
        puts "#{status} #{method_name}"
      end
    else
      puts "❌ Class generation test failed"
    end
  end
  
  def test_metaprogramming_performance(operation, iterations = 1000)
    tusk_config = Rails.application.config.tusk_config
    
    # Test performance
    result = tusk_config.execute_metaprogramming_test('test_metaprogramming_performance', {
      operation: operation,
      iterations: iterations
    })
    
    puts "Performance test results:"
    puts "Iterations: #{result[:iterations]}"
    puts "Total time: #{result[:execution_time]}s"
    puts "Average time: #{result[:average_time]}s"
  end
end

# RSpec tests for metaprogramming
RSpec.describe MetaprogrammingManager, type: :model do
  let(:metaprogramming_manager) { MetaprogrammingManager.new }
  
  describe '#create_dynamic_method' do
    it 'creates methods dynamically' do
      expect {
        metaprogramming_manager.create_dynamic_method('TestClass', 'dynamic_method', 'return "test"')
      }.not_to raise_error
      
      # Test the generated method
      expect(TestClass.new.dynamic_method).to eq('test')
    end
  end
end

RSpec.describe DynamicClassGenerator, type: :model do
  let(:class_generator) { DynamicClassGenerator.new }
  
  describe '#generate_dynamic_model' do
    it 'generates valid ActiveRecord models' do
      attributes = {
        'name' => 'string',
        'email' => 'string',
        'posts' => 'association'
      }
      
      expect {
        class_generator.generate_dynamic_model('DynamicUser', 'dynamic_users', attributes)
      }.not_to raise_error
      
      # Verify file was created
      expect(File.exist?(Rails.root.join('app', 'models', 'dynamic_user.rb'))).to be true
    end
  end
end
```

## 🔧 Rails Integration

### Rails Metaprogramming Configuration

```ruby
# config/initializers/tusk_metaprogramming.rb
Rails.application.config.after_initialize do
  TuskLang.configure do |config|
    # Configure metaprogramming settings
    config.metaprogramming_settings = {
      enable_dynamic_method_creation: true,
      enable_dynamic_class_generation: true,
      enable_module_generation: true,
      enable_ast_manipulation: true,
      metaprogramming_cache_enabled: true,
      metaprogramming_cache_ttl: 1.hour
    }
    
    # Configure metaprogramming paths
    config.metaprogramming_paths = [
      Rails.root.join('app', 'models', 'dynamic'),
      Rails.root.join('app', 'controllers', 'dynamic'),
      Rails.root.join('app', 'services', 'dynamic'),
      Rails.root.join('lib', 'generated')
    ]
    
    # Configure metaprogramming security
    config.metaprogramming_security = {
      allow_file_operations: Rails.env.development?,
      allow_class_creation: true,
      allowed_method_names: ['dynamic_method', 'generated_method'],
      restricted_methods: ['system', 'eval', 'exec']
    }
  end
end

# app/models/concerns/tusk_metaprogrammable.rb
module TuskMetaprogrammable
  extend ActiveSupport::Concern
  
  included do
    include TuskLang::Metaprogrammable
  end
  
  private
  
  def tusk_config
    Rails.application.config.tusk_config
  end
end
```

### Custom Rake Tasks

```ruby
# lib/tasks/metaprogramming.rake
namespace :metaprogramming do
  desc "Generate dynamic classes using TuskLang metaprogramming"
  task generate: :environment do
    generator = DynamicClassGenerator.new
    
    # Generate dynamic model
    attributes = {
      'name' => 'string',
      'email' => 'string',
      'posts' => 'association'
    }
    generator.generate_dynamic_model('DynamicUser', 'dynamic_users', attributes)
    
    # Generate dynamic service
    methods = {
      'process_data' => 'data_processing_logic',
      'validate_input' => 'validation_logic'
    }
    generator.generate_dynamic_service('DataProcessor', methods)
    
    puts "Dynamic class generation completed"
  end
  
  desc "Test metaprogramming functionality"
  task test: :environment do
    tester = MetaprogrammingTester.new
    
    # Test method generation
    tester.test_method_generation('TestClass', 'test_method', [], 'test result')
    
    # Test class generation
    class_config = { attributes: { 'name' => 'string' } }
    test_methods = {
      'name' => { input: [], expected: 'test' }
    }
    tester.test_class_generation('GeneratedClass', class_config, test_methods)
    
    puts "Metaprogramming testing completed"
  end
  
  desc "Apply metaprogramming to existing models"
  task apply_to_models: :environment do
    manager = ActiveRecordMetaprogramming.new
    
    # Apply to User model
    associations = {
      'posts' => { type: 'has_many', class_name: 'Post' },
      'profile' => { type: 'has_one', class_name: 'Profile' }
    }
    manager.generate_dynamic_associations('User', associations)
    
    callbacks = {
      'before_save' => ['before_save_callback'],
      'after_create' => ['after_create_callback']
    }
    manager.generate_dynamic_callbacks('User', callbacks)
    
    puts "Metaprogramming applied to models"
  end
end
```

## 🎯 Summary

TuskLang's metaprogramming system in Ruby provides:

- **Dynamic method creation** with runtime code generation
- **Dynamic class generation** for models, services, and controllers
- **Method missing patterns** for flexible method dispatch
- **Module and concern generation** for code organization
- **ActiveRecord metaprogramming** for dynamic associations and callbacks
- **Controller metaprogramming** for dynamic actions and filters
- **Testing frameworks** for metaprogramming validation
- **Security features** for safe dynamic code generation
- **Custom rake tasks** for metaprogramming management

The Ruby implementation maintains TuskLang's rebellious spirit while providing enterprise-grade metaprogramming capabilities that "don't bow to any king" - not even the constraints of traditional static code patterns.

**Ready to revolutionize your Ruby application's dynamic code generation with TuskLang?** 🚀 
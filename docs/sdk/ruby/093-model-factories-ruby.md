# 💎 Model Factories in TuskLang - Ruby Edition

**"We don't bow to any king" - Factory Patterns with Ruby Grace**

Model factories in TuskLang provide a powerful, flexible way to create test data and seed your database. In Ruby, this integrates seamlessly with FactoryBot, RSpec, and provides advanced factory patterns that go beyond traditional approaches.

## 🚀 Basic Factory Definitions

### Simple Factory Creation

```ruby
require 'tusklang'

# TuskLang configuration for basic factories
tsk_content = <<~TSK
  [factories]
  # Basic user factory
  user_factory: @factory.define("user", {
      name: @faker.name(),
      email: @faker.email(),
      role: "user",
      status: "active",
      created_at: @now(),
      updated_at: @now()
  })
  
  # Factory with sequences
  user_with_sequence: @factory.define("user", {
      name: @sequence("User", (n) => "User #{n}"),
      email: @sequence("email", (n) => "user#{n}@example.com"),
      role: "user",
      status: "active"
  })
  
  # Factory with associations
  post_factory: @factory.define("post", {
      title: @faker.sentence(),
      content: @faker.paragraph(),
      author: @factory.association("user"),
      status: "published",
      published_at: @now()
  })
TSK

# Ruby implementation
class FactoryManager
  include TuskLang::Factoryable
  
  def create_basic_factories
    tusk_config = Rails.application.config.tusk_config
    
    # Register factories
    tusk_config.register_factory('user_factory')
    tusk_config.register_factory('user_with_sequence')
    tusk_config.register_factory('post_factory')
    
    puts "Basic factories registered successfully"
  end
end
```

### Factory with Traits

```ruby
# TuskLang configuration with traits
tsk_content = <<~TSK
  [factory_traits]
  # User factory with traits
  user_with_traits: @factory.define("user", {
      name: @faker.name(),
      email: @faker.email(),
      role: "user",
      status: "active"
  }).trait("admin", {
      role: "admin",
      permissions: ["read", "write", "delete"]
  }).trait("inactive", {
      status: "inactive",
      deactivated_at: @now()
  }).trait("premium", {
      subscription_type: "premium",
      subscription_expires_at: @date.add("1y")
  })
  
  # Post factory with traits
  post_with_traits: @factory.define("post", {
      title: @faker.sentence(),
      content: @faker.paragraph(),
      author: @factory.association("user"),
      status: "draft"
  }).trait("published", {
      status: "published",
      published_at: @now()
  }).trait("featured", {
      featured: true,
      featured_at: @now()
  }).trait("with_comments", {
      comments: @factory.association("comment", 3)
  })
TSK

# Ruby implementation with traits
class TraitFactoryManager
  include TuskLang::Factoryable
  
  def create_factories_with_traits
    tusk_config = Rails.application.config.tusk_config
    
    # Register factories with traits
    tusk_config.register_factory_with_traits('user_with_traits')
    tusk_config.register_factory_with_traits('post_with_traits')
    
    puts "Factories with traits registered successfully"
  end
end
```

## 🔧 Advanced Factory Patterns

### Nested Associations

```ruby
# TuskLang configuration for complex associations
tsk_content = <<~TSK
  [nested_factories]
  # User with nested associations
  user_with_nested: @factory.define("user", {
      name: @faker.name(),
      email: @faker.email(),
      profile: @factory.association("profile", {
          bio: @faker.paragraph(),
          avatar_url: @faker.image_url(),
          preferences: {
              theme: "dark",
              notifications: true
          }
      }),
      posts: @factory.association("post", 3, {
          status: "published",
          comments: @factory.association("comment", 2)
      })
  })
  
  # Category with nested products
  category_with_products: @factory.define("category", {
      name: @faker.word(),
      description: @faker.sentence(),
      products: @factory.association("product", 5, {
          reviews: @factory.association("review", 3, {
              rating: @random(1, 5),
              comment: @faker.paragraph()
          })
      })
  })
TSK

# Ruby implementation with nested associations
class NestedFactoryManager
  include TuskLang::Factoryable
  
  def create_nested_factories
    tusk_config = Rails.application.config.tusk_config
    
    # Register nested factories
    tusk_config.register_nested_factory('user_with_nested')
    tusk_config.register_nested_factory('category_with_products')
    
    puts "Nested factories registered successfully"
  end
end
```

### Dynamic Factory Generation

```ruby
# TuskLang configuration with dynamic factories
tsk_content = <<~TSK
  [dynamic_factories]
  # Generate factories dynamically
  generate_user_factories: @range(1, 5).map((i) => 
      @factory.define("user_type_#{i}", {
          name: @faker.name(),
          email: @faker.email(),
          role: "user",
          user_type: i,
          permissions: @if(i == 1, ["read"], @if(i == 2, ["read", "write"], ["read", "write", "delete"]))
      })
  )
  
  # Factory with conditional attributes
  conditional_user_factory: @factory.define("conditional_user", {
      name: @faker.name(),
      email: @faker.email(),
      role: @if(@request.role, @request.role, "user"),
      status: @if(@request.status, @request.status, "active"),
      created_at: @if(@request.created_at, @date.parse(@request.created_at), @now())
  })
TSK

# Ruby implementation with dynamic factories
class DynamicFactoryManager
  include TuskLang::Factoryable
  
  def generate_dynamic_factories
    tusk_config = Rails.application.config.tusk_config
    
    # Generate factories dynamically
    factories = tusk_config.execute_factory_generation('generate_user_factories')
    
    factories.each do |factory|
      tusk_config.register_factory(factory)
    end
    
    puts "Generated #{factories.length} dynamic factories"
  end
  
  def create_conditional_factory(attributes = {})
    tusk_config = Rails.application.config.tusk_config
    
    # Create factory with conditional attributes
    factory = tusk_config.create_conditional_factory('conditional_user_factory', attributes)
    
    factory
  end
end
```

## 🏭 FactoryBot Integration

### TuskLang with FactoryBot

```ruby
# TuskLang configuration using FactoryBot
tsk_content = <<~TSK
  [factorybot_integration]
  # Use FactoryBot factories
  factorybot_user: @factorybot("user").create({
      name: @faker.name(),
      email: @faker.email()
  })
  
  # Create multiple instances
  factorybot_users: @factorybot("user").createMany(10, {
      role: "user",
      status: "active"
  })
  
  # Use FactoryBot traits
  factorybot_admin: @factorybot("user", "admin").create({
      name: "Admin User",
      email: "admin@example.com"
  })
  
  # Use FactoryBot associations
  factorybot_user_with_posts: @factorybot("user").create({
      name: "Post User",
      posts: @factorybot("post").createMany(5)
  })
TSK

# Ruby implementation with FactoryBot integration
class FactoryBotIntegration
  include TuskLang::Factoryable
  
  def create_with_factorybot
    tusk_config = Rails.application.config.tusk_config
    
    # Create users using FactoryBot
    user = tusk_config.execute_factorybot('factorybot_user')
    users = tusk_config.execute_factorybot('factorybot_users')
    admin = tusk_config.execute_factorybot('factorybot_admin')
    user_with_posts = tusk_config.execute_factorybot('factorybot_user_with_posts')
    
    puts "Created #{users.length} users with FactoryBot"
    puts "Created admin: #{admin[:name]}"
    puts "Created user with posts: #{user_with_posts[:posts].length} posts"
  end
end

# FactoryBot factories (factories/users.rb)
FactoryBot.define do
  factory :user do
    sequence(:name) { |n| "User #{n}" }
    sequence(:email) { |n| "user#{n}@example.com" }
    role { "user" }
    status { "active" }
    
    trait :admin do
      role { "admin" }
      permissions { ["read", "write", "delete"] }
    end
    
    trait :inactive do
      status { "inactive" }
      deactivated_at { Time.current }
    end
    
    trait :premium do
      subscription_type { "premium" }
      subscription_expires_at { 1.year.from_now }
    end
    
    trait :with_posts do
      after(:create) do |user|
        create_list(:post, 3, author: user)
      end
    end
  end
  
  factory :post do
    sequence(:title) { |n| "Post #{n}" }
    content { "This is the content for post #{n}" }
    association :author, factory: :user
    status { "draft" }
    
    trait :published do
      status { "published" }
      published_at { Time.current }
    end
    
    trait :featured do
      featured { true }
      featured_at { Time.current }
    end
    
    trait :with_comments do
      after(:create) do |post|
        create_list(:comment, 3, post: post)
      end
    end
  end
end
```

## 🔄 Factory Callbacks and Hooks

### Callbacks in Factories

```ruby
# TuskLang configuration with callbacks
tsk_content = <<~TSK
  [factory_callbacks]
  # Factory with callbacks
  user_with_callbacks: @factory.define("user", {
      name: @faker.name(),
      email: @faker.email(),
      role: "user",
      status: "active"
  }).afterCreate((user) => {
      @log.info("User created: " + user.email)
      @factory.create("profile", {user_id: user.id})
  }).afterBuild((user) => {
      user.password = @faker.password()
  }).afterStub((user) => {
      user.id = @random(1, 1000)
  })
  
  # Factory with multiple callbacks
  post_with_callbacks: @factory.define("post", {
      title: @faker.sentence(),
      content: @faker.paragraph(),
      author: @factory.association("user"),
      status: "draft"
  }).afterCreate((post) => {
      @factory.create("view_count", {post_id: post.id, count: 0})
  }).afterCreate((post) => {
      @factory.create("post_metadata", {post_id: post.id, word_count: post.content.split(" ").length})
  })
TSK

# Ruby implementation with callbacks
class CallbackFactoryManager
  include TuskLang::Factoryable
  
  def create_factories_with_callbacks
    tusk_config = Rails.application.config.tusk_config
    
    # Register factories with callbacks
    tusk_config.register_factory_with_callbacks('user_with_callbacks')
    tusk_config.register_factory_with_callbacks('post_with_callbacks')
    
    puts "Factories with callbacks registered successfully"
  end
end
```

## 🧪 Testing with Factories

### Factory Testing Patterns

```ruby
# TuskLang configuration for testing
tsk_content = <<~TSK
  [test_factories]
  # Test scenarios with factories
  test_scenarios: {
      # Scenario 1: Basic user
      basic_user: @factory("user").create({
          name: "Test User",
          email: "test@example.com"
      }),
      
      # Scenario 2: Admin user
      admin_user: @factory("user", "admin").create({
          name: "Admin User",
          email: "admin@example.com"
      }),
      
      # Scenario 3: User with posts
      user_with_posts: @factory("user").create({
          name: "Post User",
          posts: @factory("post", "published").createMany(5)
      }),
      
      # Scenario 4: Complex scenario
      complex_scenario: @factory("user", "premium").create({
          name: "Premium User",
          posts: @factory("post", "featured").createMany(3, {
              comments: @factory("comment").createMany(2)
          })
      })
  }
  
  # Performance test data
  performance_data: {
      large_dataset: @factory("user").createMany(1000, {
          posts: @factory("post").createMany(@random(1, 10))
      })
  }
TSK

# Ruby implementation for testing
class TestFactoryManager
  include TuskLang::Factoryable
  
  def create_test_scenarios
    tusk_config = Rails.application.config.tusk_config
    
    scenarios = tusk_config.execute_test_factories('test_scenarios')
    
    # Return scenario data for testing
    {
      basic_user: scenarios[:basic_user],
      admin_user: scenarios[:admin_user],
      user_with_posts: scenarios[:user_with_posts],
      complex_scenario: scenarios[:complex_scenario]
    }
  end
  
  def create_performance_data
    tusk_config = Rails.application.config.tusk_config
    
    result = tusk_config.execute_test_factories('performance_data')
    
    puts "Created #{result[:large_dataset].length} users for performance testing"
  end
end

# RSpec tests using factories
RSpec.describe User, type: :model do
  let(:factory_manager) { TestFactoryManager.new }
  let(:scenarios) { factory_manager.create_test_scenarios }
  
  describe 'basic user' do
    let(:user) { scenarios[:basic_user] }
    
    it 'has basic attributes' do
      expect(user.name).to eq('Test User')
      expect(user.email).to eq('test@example.com')
      expect(user.role).to eq('user')
    end
  end
  
  describe 'admin user' do
    let(:user) { scenarios[:admin_user] }
    
    it 'has admin permissions' do
      expect(user.role).to eq('admin')
      expect(user.permissions).to include('read', 'write', 'delete')
    end
  end
  
  describe 'user with posts' do
    let(:user) { scenarios[:user_with_posts] }
    
    it 'has associated posts' do
      expect(user.posts.count).to eq(5)
      expect(user.posts.first.status).to eq('published')
    end
  end
end
```

## 🔧 Rails Integration

### Rails Factory Configuration

```ruby
# config/initializers/tusk_factories.rb
Rails.application.config.after_initialize do
  TuskLang.configure do |config|
    # Configure factory settings
    config.factory_settings = {
      default_strategy: :create,
      use_transactions: true,
      build_strategy: :build,
      create_strategy: :create,
      build_stubbed_strategy: :build_stubbed
    }
    
    # Configure factory paths
    config.factory_paths = [
      Rails.root.join('spec', 'factories'),
      Rails.root.join('test', 'factories'),
      Rails.root.join('lib', 'factories')
    ]
    
    # Configure factory associations
    config.factory_associations = {
      user: :user,
      post: :post,
      comment: :comment,
      category: :category,
      product: :product
    }
  end
end

# app/models/concerns/tusk_factoryable.rb
module TuskFactoryable
  extend ActiveSupport::Concern
  
  included do
    include TuskLang::Factoryable
  end
  
  private
  
  def tusk_config
    Rails.application.config.tusk_config
  end
end
```

### Custom Rake Tasks

```ruby
# lib/tasks/factories.rake
namespace :factories do
  desc "Generate factories from TuskLang configuration"
  task generate: :environment do
    generator = FactoryGenerator.new
    generator.generate_all
    puts "Factories generated successfully"
  end
  
  desc "Validate factory definitions"
  task validate: :environment do
    validator = FactoryValidator.new
    validator.validate_all
    puts "Factory validation completed"
  end
  
  desc "Create test data using factories"
  task create_test_data: :environment do
    manager = TestFactoryManager.new
    manager.create_test_scenarios
    puts "Test data created successfully"
  end
end

# Factory generator class
class FactoryGenerator
  include TuskLang::Factoryable
  
  def generate_all
    tusk_config = Rails.application.config.tusk_config
    
    # Generate factories from TuskLang configuration
    factories = tusk_config.get_all_factories
    
    factories.each do |factory_name, factory_config|
      generate_factory_file(factory_name, factory_config)
    end
  end
  
  private
  
  def generate_factory_file(factory_name, config)
    # Generate FactoryBot factory file
    factory_content = generate_factorybot_content(factory_name, config)
    
    file_path = Rails.root.join('spec', 'factories', "#{factory_name.pluralize}.rb")
    File.write(file_path, factory_content)
    
    puts "Generated factory file: #{file_path}"
  end
  
  def generate_factorybot_content(factory_name, config)
    # Convert TuskLang factory config to FactoryBot syntax
    # Implementation details...
  end
end
```

## 🚀 Performance Optimization

### Factory Performance Patterns

```ruby
# TuskLang configuration for performance
tsk_content = <<~TSK
  [performance_factories]
  # Optimized factory creation
  optimized_user_factory: @factory.define("optimized_user", {
      name: @faker.name(),
      email: @faker.email(),
      role: "user",
      status: "active"
  }).strategy("build_stubbed", {
      id: @random(1, 1000),
      created_at: @now(),
      updated_at: @now()
  })
  
  # Bulk factory creation
  bulk_user_factory: @factory.bulkCreate("user", 1000, {
      role: "user",
      status: "active"
  })
  
  # Lazy factory creation
  lazy_user_factory: @factory.lazy("user", {
      name: @faker.name(),
      email: @faker.email()
  })
TSK

# Ruby implementation with performance optimization
class PerformanceFactoryManager
  include TuskLang::Factoryable
  
  def create_optimized_factories
    tusk_config = Rails.application.config.tusk_config
    
    # Use optimized strategies
    stubbed_user = tusk_config.create_factory_with_strategy('optimized_user_factory', :build_stubbed)
    bulk_users = tusk_config.execute_bulk_factory('bulk_user_factory')
    lazy_user = tusk_config.create_lazy_factory('lazy_user_factory')
    
    puts "Created #{bulk_users.length} users in bulk"
    puts "Created stubbed user: #{stubbed_user[:id]}"
  end
end
```

## 🔒 Security and Validation

### Secure Factory Creation

```ruby
# TuskLang configuration with security
tsk_content = <<~TSK
  [secure_factories]
  # Secure user factory
  secure_user_factory: @factory.define("secure_user", {
      name: @sanitize.text(@faker.name()),
      email: @sanitize.email(@faker.email()),
      password: @encrypt(@faker.password(), "bcrypt"),
      role: @validate.role(@request.role, "user"),
      status: "active"
  })
  
  # Factory with validation
  validated_user_factory: @factory.define("validated_user", {
      name: @validate.required(@faker.name()),
      email: @validate.email(@faker.email()),
      role: @validate.choice(@request.role, ["user", "admin"], "user")
  }).validate((user) => {
      if (!user.email.includes("@")) {
          throw "Invalid email format"
      }
      if (user.name.length < 2) {
          throw "Name too short"
      }
  })
TSK

# Ruby implementation with security
class SecureFactoryManager
  include TuskLang::Factoryable
  
  def create_secure_factories
    tusk_config = Rails.application.config.tusk_config
    
    # Create secure factories
    secure_user = tusk_config.create_secure_factory('secure_user_factory')
    validated_user = tusk_config.create_validated_factory('validated_user_factory')
    
    puts "Created secure user: #{secure_user[:email]}"
    puts "Created validated user: #{validated_user[:name]}"
  end
end
```

## 🎯 Summary

TuskLang's factory system in Ruby provides:

- **Flexible factory definitions** with traits and associations
- **Nested associations** for complex data structures
- **Dynamic factory generation** based on conditions
- **FactoryBot integration** for seamless Rails testing
- **Callback support** for post-creation actions
- **Testing patterns** with scenario-based factories
- **Rails integration** with custom rake tasks
- **Performance optimization** with bulk and lazy creation
- **Security features** with validation and sanitization
- **Advanced patterns** for complex test scenarios

The Ruby implementation maintains TuskLang's rebellious spirit while providing enterprise-grade factory capabilities that "don't bow to any king" - not even the constraints of traditional factory patterns.

**Ready to revolutionize your Ruby application's test data creation with TuskLang?** 🚀 
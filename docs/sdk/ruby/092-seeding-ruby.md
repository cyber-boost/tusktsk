# 💎 Database Seeding in TuskLang - Ruby Edition

**"We don't bow to any king" - Seeding Data with Ruby Elegance**

Database seeding in TuskLang provides a powerful, flexible way to populate your database with initial data. In Ruby, this integrates seamlessly with Rails seeds, factories, and provides advanced seeding patterns that go beyond traditional approaches.

## 🚀 Basic Seeding

### Simple Data Seeding

```ruby
require 'tusklang'

# TuskLang configuration for basic seeding
tsk_content = <<~TSK
  [seeds]
  # Basic user seeding
  create_admin_user: @db.seed("users", {
      name: "Admin User",
      email: "admin@example.com",
      role: "admin",
      status: "active",
      created_at: @now(),
      updated_at: @now()
  })
  
  # Multiple users with array
  create_users: @db.seed("users", [
      {
          name: "John Doe",
          email: "john@example.com",
          role: "user",
          status: "active"
      },
      {
          name: "Jane Smith",
          email: "jane@example.com",
          role: "user",
          status: "active"
      }
  ])
TSK

# Ruby implementation
class DatabaseSeeder
  include TuskLang::Seedable
  
  def seed_basic_data
    tusk_config = Rails.application.config.tusk_config
    
    # Execute seeding operations
    admin_user = tusk_config.execute_seed('create_admin_user')
    users = tusk_config.execute_seed('create_users')
    
    puts "Created admin user: #{admin_user[:id]}"
    puts "Created #{users.length} users"
  end
end
```

### Conditional Seeding

```ruby
# TuskLang configuration with conditional logic
tsk_content = <<~TSK
  [conditional_seeds]
  # Seed based on environment
  seed_development_data: @if(@env("RAILS_ENV") == "development", {
      # Only seed in development
      create_test_users: @db.seed("users", [
          {name: "Test User 1", email: "test1@example.com"},
          {name: "Test User 2", email: "test2@example.com"}
      ]),
      
      create_test_posts: @db.seed("posts", [
          {title: "Test Post 1", content: "Test content"},
          {title: "Test Post 2", content: "More test content"}
      ])
  }, {})
  
  # Seed based on database state
  seed_if_empty: @if(@query("SELECT COUNT(*) FROM users") == 0, {
      create_default_users: @db.seed("users", [
          {name: "Default User", email: "default@example.com"}
      ])
  }, {})
TSK

# Ruby implementation with environment awareness
class EnvironmentSeeder
  include TuskLang::Seedable
  
  def seed_environment_data
    tusk_config = Rails.application.config.tusk_config
    
    # Execute environment-specific seeding
    result = tusk_config.execute_seed('seed_development_data')
    
    if Rails.env.development?
      puts "Development data seeded successfully"
    end
  end
  
  def seed_if_database_empty
    tusk_config = Rails.application.config.tusk_config
    
    result = tusk_config.execute_seed('seed_if_empty')
    
    if result[:create_default_users]
      puts "Default users created for empty database"
    end
  end
end
```

## 🔧 Advanced Seeding Patterns

### Relationship Seeding

```ruby
# TuskLang configuration for related data
tsk_content = <<~TSK
  [relationship_seeds]
  # Seed users with profiles
  create_users_with_profiles: @db.seed("users", [
      {
          name: "Alice Johnson",
          email: "alice@example.com",
          profile: {
              bio: "Software developer",
              avatar_url: "https://example.com/avatar1.jpg",
              preferences: {
                  theme: "dark",
                  notifications: true
              }
          }
      },
      {
          name: "Bob Wilson",
          email: "bob@example.com",
          profile: {
              bio: "Product manager",
              avatar_url: "https://example.com/avatar2.jpg",
              preferences: {
                  theme: "light",
                  notifications: false
              }
          }
      }
  ])
  
  # Seed categories with products
  create_categories_with_products: @db.seed("categories", [
      {
          name: "Electronics",
          description: "Electronic devices and gadgets",
          products: [
              {name: "Laptop", price: 999.99, stock: 10},
              {name: "Smartphone", price: 599.99, stock: 25},
              {name: "Tablet", price: 399.99, stock: 15}
          ]
      },
      {
          name: "Books",
          description: "Books and literature",
          products: [
              {name: "Programming Ruby", price: 49.99, stock: 50},
              {name: "Rails Guide", price: 29.99, stock: 100}
          ]
      }
  ])
TSK

# Ruby implementation with relationship handling
class RelationshipSeeder
  include TuskLang::Seedable
  
  def seed_users_with_profiles
    tusk_config = Rails.application.config.tusk_config
    
    result = tusk_config.execute_seed('create_users_with_profiles')
    
    puts "Created #{result.length} users with profiles"
  end
  
  def seed_categories_with_products
    tusk_config = Rails.application.config.tusk_config
    
    result = tusk_config.execute_seed('create_categories_with_products')
    
    puts "Created #{result.length} categories with products"
  end
end
```

### Dynamic Seeding with Functions

```ruby
# TuskLang configuration with dynamic data generation
tsk_content = <<~TSK
  [dynamic_seeds]
  # Generate random users
  generate_random_users: @db.seed("users", @range(1, 10).map((i) => ({
      name: "User " + i,
      email: "user" + i + "@example.com",
      role: @if(i == 1, "admin", "user"),
      status: "active",
      created_at: @date.subtract(@random(1, 30) + "d")
  })))
  
  # Generate posts with random content
  generate_random_posts: @db.seed("posts", @range(1, 20).map((i) => ({
      title: "Post " + i,
      content: "This is the content for post " + i + ". " + @random_text(100),
      author_id: @query("SELECT id FROM users ORDER BY RANDOM() LIMIT 1"),
      published_at: @date.subtract(@random(1, 60) + "d"),
      status: @if(@random(1, 10) > 2, "published", "draft")
  })))
  
  # Generate orders with realistic data
  generate_orders: @db.seed("orders", @range(1, 50).map((i) => ({
      order_number: "ORD-" + @date.format("YYYYMMDD") + "-" + @pad(i, 4),
      customer_id: @query("SELECT id FROM users ORDER BY RANDOM() LIMIT 1"),
      total_amount: @random(10.00, 500.00).toFixed(2),
      status: @random_choice(["pending", "processing", "shipped", "delivered"]),
      created_at: @date.subtract(@random(1, 90) + "d")
  })))
TSK

# Ruby implementation with dynamic seeding
class DynamicSeeder
  include TuskLang::Seedable
  
  def generate_test_data
    tusk_config = Rails.application.config.tusk_config
    
    # Generate various types of test data
    users = tusk_config.execute_seed('generate_random_users')
    posts = tusk_config.execute_seed('generate_random_posts')
    orders = tusk_config.execute_seed('generate_orders')
    
    puts "Generated #{users.length} users"
    puts "Generated #{posts.length} posts"
    puts "Generated #{orders.length} orders"
  end
end
```

## 🏭 Factory Integration

### TuskLang with FactoryBot

```ruby
# TuskLang configuration using factories
tsk_content = <<~TSK
  [factory_seeds]
  # Use FactoryBot factories
  create_users_with_factories: @factory("user").createMany(10, {
      role: "user",
      status: "active"
  })
  
  # Create users with specific traits
  create_admin_users: @factory("user", "admin").createMany(3)
  
  # Create users with associations
  create_users_with_posts: @factory("user").createMany(5, {
      posts: @factory("post").createMany(3)
  })
  
  # Create complex associations
  create_categories_with_products: @factory("category").createMany(3, {
      products: @factory("product").createMany(5, {
          reviews: @factory("review").createMany(2)
      })
  })
TSK

# Ruby implementation with FactoryBot integration
class FactorySeeder
  include TuskLang::Seedable
  
  def seed_with_factories
    tusk_config = Rails.application.config.tusk_config
    
    # Create users using factories
    users = tusk_config.execute_factory_seed('create_users_with_factories')
    admins = tusk_config.execute_factory_seed('create_admin_users')
    users_with_posts = tusk_config.execute_factory_seed('create_users_with_posts')
    
    puts "Created #{users.length} regular users"
    puts "Created #{admins.length} admin users"
    puts "Created #{users_with_posts.length} users with posts"
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
    status { "published" }
  end
end
```

## 🔄 Data Transformation and Cleanup

### Data Transformation During Seeding

```ruby
# TuskLang configuration with data transformation
tsk_content = <<~TSK
  [transformation_seeds]
  # Transform CSV data
  seed_from_csv: @db.seed("users", @csv("data/users.csv").map((row) => ({
      name: row.name,
      email: row.email.toLowerCase(),
      role: @if(row.role == "A", "admin", "user"),
      status: "active",
      created_at: @date.parse(row.created_at)
  })))
  
  # Transform JSON data
  seed_from_json: @db.seed("products", @json("data/products.json").map((product) => ({
      name: product.name,
      price: @parseFloat(product.price),
      category_id: @query("SELECT id FROM categories WHERE name = ?", product.category),
      stock: @parseInt(product.stock),
      sku: product.sku.toUpperCase()
  })))
  
  # Clean and validate data
  seed_clean_data: @db.seed("users", @csv("data/dirty_users.csv").filter((row) => 
      row.email && row.email.includes("@") && row.name && row.name.length > 0
  ).map((row) => ({
      name: row.name.trim(),
      email: row.email.trim().toLowerCase(),
      role: @validate.role(row.role, "user"),
      status: @validate.status(row.status, "active")
  })))
TSK

# Ruby implementation with data transformation
class DataTransformationSeeder
  include TuskLang::Seedable
  
  def seed_from_external_sources
    tusk_config = Rails.application.config.tusk_config
    
    # Seed from CSV
    csv_users = tusk_config.execute_seed('seed_from_csv')
    
    # Seed from JSON
    json_products = tusk_config.execute_seed('seed_from_json')
    
    # Seed clean data
    clean_users = tusk_config.execute_seed('seed_clean_data')
    
    puts "Imported #{csv_users.length} users from CSV"
    puts "Imported #{json_products.length} products from JSON"
    puts "Imported #{clean_users.length} clean users"
  end
end
```

## 🧪 Testing and Validation

### Seeding for Testing

```ruby
# TuskLang configuration for test data
tsk_content = <<~TSK
  [test_seeds]
  # Create test scenarios
  create_test_scenarios: {
      # Scenario 1: Basic user
      basic_user: @factory("user").create({
          name: "Test User",
          email: "test@example.com"
      }),
      
      # Scenario 2: User with posts
      user_with_posts: @factory("user").create({
          name: "Post User",
          posts: @factory("post").createMany(5)
      }),
      
      # Scenario 3: Admin with permissions
      admin_user: @factory("user", "admin").create({
          name: "Admin User",
          permissions: ["read", "write", "delete"]
      }),
      
      # Scenario 4: User with orders
      user_with_orders: @factory("user").create({
          name: "Order User",
          orders: @factory("order").createMany(3, {
              status: "completed",
              total_amount: @random(50.00, 200.00)
          })
      })
  }
  
  # Create performance test data
  create_performance_data: {
      large_dataset: @db.seed("users", @range(1, 1000).map((i) => ({
          name: "User " + i,
          email: "user" + i + "@example.com",
          posts: @factory("post").createMany(@random(1, 10))
      })))
  }
TSK

# Ruby implementation for testing
class TestDataSeeder
  include TuskLang::Seedable
  
  def create_test_scenarios
    tusk_config = Rails.application.config.tusk_config
    
    scenarios = tusk_config.execute_seed('create_test_scenarios')
    
    # Return scenario data for testing
    {
      basic_user: scenarios[:basic_user],
      user_with_posts: scenarios[:user_with_posts],
      admin_user: scenarios[:admin_user],
      user_with_orders: scenarios[:user_with_orders]
    }
  end
  
  def create_performance_data
    tusk_config = Rails.application.config.tusk_config
    
    result = tusk_config.execute_seed('create_performance_data')
    
    puts "Created #{result[:large_dataset].length} users for performance testing"
  end
end

# RSpec tests using seeded data
RSpec.describe User, type: :model do
  let(:seeder) { TestDataSeeder.new }
  let(:scenarios) { seeder.create_test_scenarios }
  
  describe 'basic user' do
    let(:user) { scenarios[:basic_user] }
    
    it 'has basic attributes' do
      expect(user.name).to eq('Test User')
      expect(user.email).to eq('test@example.com')
    end
  end
  
  describe 'user with posts' do
    let(:user) { scenarios[:user_with_posts] }
    
    it 'has associated posts' do
      expect(user.posts.count).to eq(5)
    end
  end
end
```

## 🔧 Rails Integration

### Rails Seeds with TuskLang

```ruby
# db/seeds.rb
require 'tusklang'

class RailsSeeder
  include TuskLang::Seedable
  
  def self.seed_all
    seeder = new
    seeder.seed_environment_data
    seeder.seed_development_data if Rails.env.development?
    seeder.seed_test_data if Rails.env.test?
  end
  
  def seed_environment_data
    tusk_config = Rails.application.config.tusk_config
    
    # Seed essential data for all environments
    tusk_config.execute_seed('create_admin_user')
    tusk_config.execute_seed('create_default_categories')
    tusk_config.execute_seed('create_system_settings')
  end
  
  def seed_development_data
    tusk_config = Rails.application.config.tusk_config
    
    # Seed development-specific data
    tusk_config.execute_seed('seed_development_data')
    tusk_config.execute_seed('generate_random_users')
    tusk_config.execute_seed('generate_random_posts')
  end
  
  def seed_test_data
    tusk_config = Rails.application.config.tusk_config
    
    # Seed test data
    tusk_config.execute_seed('create_test_scenarios')
  end
end

# Run seeds
RailsSeeder.seed_all
```

### Custom Rake Tasks

```ruby
# lib/tasks/seed.rake
namespace :db do
  namespace :seed do
    desc "Seed development data"
    task development: :environment do
      seeder = DevelopmentSeeder.new
      seeder.seed_all
      puts "Development data seeded successfully"
    end
    
    desc "Seed test data"
    task test: :environment do
      seeder = TestDataSeeder.new
      seeder.create_test_scenarios
      puts "Test data seeded successfully"
    end
    
    desc "Seed production data"
    task production: :environment do
      seeder = ProductionSeeder.new
      seeder.seed_essential_data
      puts "Production data seeded successfully"
    end
    
    desc "Reset and reseed database"
    task reset: :environment do
      Rake::Task['db:drop'].invoke
      Rake::Task['db:create'].invoke
      Rake::Task['db:migrate'].invoke
      Rake::Task['db:seed'].invoke
      puts "Database reset and reseeded"
    end
  end
end
```

## 🚀 Performance Optimization

### Bulk Seeding

```ruby
# TuskLang configuration for bulk operations
tsk_content = <<~TSK
  [bulk_seeds]
  # Bulk insert for performance
  bulk_create_users: @db.bulkSeed("users", @range(1, 10000).map((i) => ({
      name: "User " + i,
      email: "user" + i + "@example.com",
      role: "user",
      status: "active",
      created_at: @now(),
      updated_at: @now()
  })))
  
  # Bulk insert with batching
  bulk_create_posts: @db.bulkSeed("posts", @range(1, 50000).map((i) => ({
      title: "Post " + i,
      content: "Content for post " + i,
      author_id: @random(1, 1000),
      status: "published",
      created_at: @date.subtract(@random(1, 365) + "d")
  })), {batch_size: 1000})
TSK

# Ruby implementation with performance optimization
class PerformanceSeeder
  include TuskLang::Seedable
  
  def seed_large_datasets
    tusk_config = Rails.application.config.tusk_config
    
    # Use bulk operations for large datasets
    users = tusk_config.execute_bulk_seed('bulk_create_users')
    posts = tusk_config.execute_bulk_seed('bulk_create_posts')
    
    puts "Bulk created #{users.length} users"
    puts "Bulk created #{posts.length} posts"
  end
  
  def seed_with_progress
    tusk_config = Rails.application.config.tusk_config
    
    # Show progress for long-running operations
    tusk_config.execute_seed_with_progress('bulk_create_users') do |progress|
      puts "Progress: #{progress.percentage}% (#{progress.current}/#{progress.total})"
    end
  end
end
```

## 🔒 Security and Validation

### Secure Seeding

```ruby
# TuskLang configuration with security
tsk_content = <<~TSK
  [secure_seeds]
  # Secure admin creation
  create_secure_admin: @db.seed("users", {
      name: "System Admin",
      email: @env("ADMIN_EMAIL"),
      password: @encrypt(@env("ADMIN_PASSWORD"), "bcrypt"),
      role: "admin",
      status: "active",
      created_at: @now()
  })
  
  # Validate data before seeding
  create_validated_users: @db.seed("users", @csv("data/users.csv").filter((row) => 
      @validate.email(row.email) && @validate.required([row.name, row.email])
  ).map((row) => ({
      name: @sanitize.text(row.name),
      email: @sanitize.email(row.email),
      role: @validate.role(row.role, "user"),
      status: "active"
  })))
TSK

# Ruby implementation with security
class SecureSeeder
  include TuskLang::Seedable
  
  def create_secure_admin
    tusk_config = Rails.application.config.tusk_config
    
    # Create admin with secure credentials
    admin = tusk_config.execute_seed('create_secure_admin')
    
    puts "Secure admin created: #{admin[:email]}"
  end
  
  def create_validated_users
    tusk_config = Rails.application.config.tusk_config
    
    # Create users with validation
    users = tusk_config.execute_seed('create_validated_users')
    
    puts "Created #{users.length} validated users"
  end
end
```

## 🎯 Summary

TuskLang's seeding system in Ruby provides:

- **Flexible data seeding** with conditional logic and environment awareness
- **Relationship handling** for complex data structures
- **Factory integration** with FactoryBot and other factory libraries
- **Data transformation** from CSV, JSON, and other sources
- **Testing support** with scenario-based seeding
- **Rails integration** with custom rake tasks and seeds
- **Performance optimization** with bulk operations and batching
- **Security features** with validation and sanitization
- **Dynamic data generation** with random data and functions

The Ruby implementation maintains TuskLang's rebellious spirit while providing enterprise-grade seeding capabilities that "don't bow to any king" - not even the constraints of traditional database seeding patterns.

**Ready to revolutionize your Ruby application's data seeding with TuskLang?** 🚀 
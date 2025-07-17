# 📊 Array Operations in TuskLang - Ruby Edition

**"We don't bow to any king" - Ruby Edition**

TuskLang's array operations provide powerful data manipulation capabilities that integrate seamlessly with Ruby's rich array processing features, enabling dynamic data transformation, filtering, and intelligent collection management.

## 🎯 Overview

Array operations in TuskLang allow you to manipulate collections directly in configuration files, enabling dynamic data processing, filtering, and transformation. When combined with Ruby's powerful array manipulation capabilities, these operations become incredibly versatile.

## 🚀 Basic Array Operations

### Array Creation and Manipulation

```ruby
# TuskLang configuration with array operations
tsk_content = <<~TSK
  [array_operations]
  # Array creation
  user_ids: @query("SELECT id FROM users WHERE active = 1")
  feature_flags: @array(@env("FEATURE_FLAGS").split(","))
  allowed_domains: @array(["gmail.com", "yahoo.com", "hotmail.com"])
  
  # Array concatenation
  all_users: @concat(@query("SELECT id FROM active_users"), @query("SELECT id FROM pending_users"))
  combined_settings: @concat(@env("DEFAULT_SETTINGS").split(","), @env("CUSTOM_SETTINGS").split(","))
  
  # Array filtering
  active_user_ids: @filter(@query("SELECT id, status FROM users"), @equals(@item("status"), "active"))
  premium_users: @filter(@query("SELECT id, subscription_type FROM users"), @equals(@item("subscription_type"), "premium"))
TSK

# Ruby integration
require 'tusklang'

parser = TuskLang.new
config = parser.parse(tsk_content)

# Use in Ruby classes
class ArrayProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def user_ids
    @config['array_operations']['user_ids']
  end
  
  def feature_flags
    @config['array_operations']['feature_flags']
  end
  
  def allowed_domains
    @config['array_operations']['allowed_domains']
  end
  
  def all_users
    @config['array_operations']['all_users']
  end
  
  def active_user_ids
    @config['array_operations']['active_user_ids']
  end
  
  def process_user_data
    {
      total_users: all_users.length,
      active_users: active_user_ids.length,
      features_enabled: feature_flags.length,
      allowed_domains: allowed_domains
    }
  end
end

# Usage
processor = ArrayProcessor.new(config)
puts processor.process_user_data
```

### Array Access and Indexing

```ruby
# TuskLang with array indexing
tsk_content = <<~TSK
  [array_indexing]
  # Array access
  first_user: @first(@query("SELECT id FROM users ORDER BY created_at"))
  last_user: @last(@query("SELECT id FROM users ORDER BY created_at"))
  third_user: @at(@query("SELECT id FROM users ORDER BY created_at"), 2)
  
  # Array slicing
  recent_users: @slice(@query("SELECT id FROM users ORDER BY created_at DESC"), 0, 10)
  middle_users: @slice(@query("SELECT id FROM users ORDER BY created_at"), 10, 20)
  
  # Array length
  total_users: @length(@query("SELECT id FROM users"))
  active_count: @length(@filter(@query("SELECT id, status FROM users"), @equals(@item("status"), "active")))
TSK

# Ruby integration with array indexing
class ArrayIndexer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_first_user
    @config['array_indexing']['first_user']
  end
  
  def get_last_user
    @config['array_indexing']['last_user']
  end
  
  def get_third_user
    @config['array_indexing']['third_user']
  end
  
  def get_recent_users
    @config['array_indexing']['recent_users']
  end
  
  def get_middle_users
    @config['array_indexing']['middle_users']
  end
  
  def get_user_statistics
    {
      total_users: @config['array_indexing']['total_users'],
      active_count: @config['array_indexing']['active_count'],
      first_user_id: get_first_user,
      last_user_id: get_last_user,
      recent_users_count: get_recent_users.length
    }
  end
end
```

## 🔧 Advanced Array Operations

### Array Transformation and Mapping

```ruby
# TuskLang with array transformation
tsk_content = <<~TSK
  [array_transformation]
  # Array mapping
  user_names: @map(@query("SELECT id, first_name, last_name FROM users"), @concat(@item("first_name"), " ", @item("last_name")))
  user_emails: @map(@query("SELECT id, email FROM users"), @item("email"))
  user_ages: @map(@query("SELECT id, birth_date FROM users"), @date.diff(@item("birth_date"), "years"))
  
  # Array filtering with complex conditions
  premium_users: @filter(@query("SELECT id, subscription_type, created_at FROM users"), @and(@equals(@item("subscription_type"), "premium"), @greater_than(@date.diff(@item("created_at")), "30d")))
  recent_orders: @filter(@query("SELECT id, amount, created_at FROM orders"), @greater_than(@item("amount"), 100))
  
  # Array sorting
  users_by_name: @sort(@query("SELECT id, first_name, last_name FROM users"), @item("first_name"))
  orders_by_amount: @sort(@query("SELECT id, amount FROM orders"), @item("amount"), "desc")
  users_by_created: @sort(@query("SELECT id, created_at FROM users"), @item("created_at"), "desc")
TSK

# Ruby integration with array transformation
class ArrayTransformer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_user_names
    @config['array_transformation']['user_names']
  end
  
  def get_user_emails
    @config['array_transformation']['user_emails']
  end
  
  def get_user_ages
    @config['array_transformation']['user_ages']
  end
  
  def get_premium_users
    @config['array_transformation']['premium_users']
  end
  
  def get_recent_orders
    @config['array_transformation']['recent_orders']
  end
  
  def get_users_by_name
    @config['array_transformation']['users_by_name']
  end
  
  def get_orders_by_amount
    @config['array_transformation']['orders_by_amount']
  end
  
  def generate_user_report
    {
      total_users: get_user_names.length,
      premium_users: get_premium_users.length,
      recent_orders: get_recent_orders.length,
      average_age: get_user_ages.sum.to_f / get_user_ages.length,
      top_orders: get_orders_by_amount.first(5)
    }
  end
end
```

### Array Aggregation and Statistics

```ruby
# TuskLang with array aggregation
tsk_content = <<~TSK
  [array_aggregation]
  # Array statistics
  total_orders: @sum(@query("SELECT amount FROM orders"))
  average_order: @average(@query("SELECT amount FROM orders"))
  max_order: @max(@query("SELECT amount FROM orders"))
  min_order: @min(@query("SELECT amount FROM orders"))
  
  # Conditional aggregation
  premium_revenue: @sum(@filter(@query("SELECT amount, subscription_type FROM orders"), @equals(@item("subscription_type"), "premium")))
  recent_revenue: @sum(@filter(@query("SELECT amount, created_at FROM orders"), @greater_than(@date.diff(@item("created_at")), "30d")))
  
  # Array counting
  total_users: @count(@query("SELECT id FROM users"))
  active_users: @count(@filter(@query("SELECT id, status FROM users"), @equals(@item("status"), "active")))
  premium_users: @count(@filter(@query("SELECT id, subscription_type FROM users"), @equals(@item("subscription_type"), "premium")))
TSK

# Ruby integration with array aggregation
class ArrayAggregator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_order_statistics
    {
      total_orders: @config['array_aggregation']['total_orders'],
      average_order: @config['array_aggregation']['average_order'],
      max_order: @config['array_aggregation']['max_order'],
      min_order: @config['array_aggregation']['min_order']
    }
  end
  
  def get_revenue_breakdown
    {
      premium_revenue: @config['array_aggregation']['premium_revenue'],
      recent_revenue: @config['array_aggregation']['recent_revenue']
    }
  end
  
  def get_user_counts
    {
      total_users: @config['array_aggregation']['total_users'],
      active_users: @config['array_aggregation']['active_users'],
      premium_users: @config['array_aggregation']['premium_users']
    }
  end
  
  def generate_business_report
    {
      orders: get_order_statistics,
      revenue: get_revenue_breakdown,
      users: get_user_counts,
      generated_at: Time.current
    }
  end
end
```

## 🎛️ Array Search and Filtering

### Advanced Filtering Operations

```ruby
# TuskLang with advanced filtering
tsk_content = <<~TSK
  [advanced_filtering]
  # Complex filtering
  high_value_users: @filter(@query("SELECT id, total_spent, subscription_type FROM users"), @and(@greater_than(@item("total_spent"), 1000), @equals(@item("subscription_type"), "premium")))
  
  # Multiple condition filtering
  eligible_users: @filter(@query("SELECT id, age, subscription_type, created_at FROM users"), @and(@greater_than_or_equal(@item("age"), 18), @equals(@item("subscription_type"), "active"), @greater_than(@date.diff(@item("created_at")), "7d")))
  
  # Array search
  user_exists: @contains(@query("SELECT email FROM users"), @env("SEARCH_EMAIL"))
  domain_allowed: @contains(@allowed_domains, @substring(@env("USER_EMAIL"), @add(@index_of(@env("USER_EMAIL"), "@"), 1)))
  
  # Array intersection and union
  common_users: @intersection(@query("SELECT id FROM users"), @query("SELECT user_id FROM orders"))
  all_entities: @union(@query("SELECT id FROM users"), @query("SELECT id FROM products"))
TSK

# Ruby integration with advanced filtering
class AdvancedFilter
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_high_value_users
    @config['advanced_filtering']['high_value_users']
  end
  
  def get_eligible_users
    @config['advanced_filtering']['eligible_users']
  end
  
  def user_exists?(email)
    @config['advanced_filtering']['user_exists']
  end
  
  def domain_allowed?(email)
    @config['advanced_filtering']['domain_allowed']
  end
  
  def get_common_users
    @config['advanced_filtering']['common_users']
  end
  
  def get_all_entities
    @config['advanced_filtering']['all_entities']
  end
  
  def validate_user_registration(email)
    {
      user_exists: user_exists?(email),
      domain_allowed: domain_allowed?(email),
      can_register: !user_exists?(email) && domain_allowed?(email)
    }
  end
end
```

### Array Search and Lookup

```ruby
# TuskLang with array search
tsk_content = <<~TSK
  [array_search]
  # Array search operations
  user_index: @index_of(@query("SELECT email FROM users"), @env("SEARCH_EMAIL"))
  product_exists: @contains(@query("SELECT sku FROM products"), @env("PRODUCT_SKU"))
  
  # Array find operations
  user_by_email: @find(@query("SELECT id, email, name FROM users"), @equals(@item("email"), @env("SEARCH_EMAIL")))
  order_by_id: @find(@query("SELECT id, amount, status FROM orders"), @equals(@item("id"), @env("ORDER_ID")))
  
  # Array find with conditions
  active_user: @find(@query("SELECT id, email, status FROM users"), @and(@equals(@item("email"), @env("SEARCH_EMAIL")), @equals(@item("status"), "active")))
  recent_order: @find(@query("SELECT id, amount, created_at FROM orders"), @and(@equals(@item("id"), @env("ORDER_ID")), @greater_than(@date.diff(@item("created_at")), "7d")))
TSK

# Ruby integration with array search
class ArraySearcher
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def find_user_by_email(email)
    @config['array_search']['user_by_email']
  end
  
  def find_order_by_id(order_id)
    @config['array_search']['order_by_id']
  end
  
  def find_active_user(email)
    @config['array_search']['active_user']
  end
  
  def find_recent_order(order_id)
    @config['array_search']['recent_order']
  end
  
  def search_user(email)
    user = find_user_by_email(email)
    if user
      {
        found: true,
        user: user,
        is_active: find_active_user(email) != nil
      }
    else
      {
        found: false,
        user: nil,
        is_active: false
      }
    end
  end
end
```

## 🔄 Dynamic Array Operations

### Array Generation and Processing

```ruby
# TuskLang with dynamic array generation
tsk_content = <<~TSK
  [dynamic_arrays]
  # Dynamic array generation
  date_range: @range(@date.subtract("30d"), @date.now(), "1d")
  number_sequence: @range(1, 100, 1)
  user_ids_batch: @range(@query("SELECT MIN(id) FROM users"), @query("SELECT MAX(id) FROM users"), 100)
  
  # Array processing with conditions
  active_user_ids: @map(@filter(@query("SELECT id, status FROM users"), @equals(@item("status"), "active")), @item("id"))
  premium_emails: @map(@filter(@query("SELECT email, subscription_type FROM users"), @equals(@item("subscription_type"), "premium")), @item("email"))
  
  # Array chunking
  user_chunks: @chunk(@query("SELECT id FROM users"), 100)
  order_chunks: @chunk(@query("SELECT id FROM orders"), 50)
TSK

# Ruby integration with dynamic arrays
class DynamicArrayProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_date_range
    @config['dynamic_arrays']['date_range']
  end
  
  def get_number_sequence
    @config['dynamic_arrays']['number_sequence']
  end
  
  def get_user_ids_batch
    @config['dynamic_arrays']['user_ids_batch']
  end
  
  def get_active_user_ids
    @config['dynamic_arrays']['active_user_ids']
  end
  
  def get_premium_emails
    @config['dynamic_arrays']['premium_emails']
  end
  
  def get_user_chunks
    @config['dynamic_arrays']['user_chunks']
  end
  
  def process_in_batches
    chunks = get_user_chunks
    chunks.each_with_index do |chunk, index|
      Rails.logger.info "Processing chunk #{index + 1} of #{chunks.length}"
      # Process chunk
    end
  end
end
```

### Array Transformation Pipelines

```ruby
# TuskLang with transformation pipelines
tsk_content = <<~TSK
  [transformation_pipelines]
  # Multi-step array processing
  processed_users: @pipe(@query("SELECT id, email, status, created_at FROM users"), [
    @filter(@equals(@item("status"), "active")),
    @filter(@greater_than(@date.diff(@item("created_at")), "30d")),
    @map(@concat(@item("email"), " (", @item("id"), ")"))
  ])
  
  # Conditional array processing
  conditional_users: @when(@env("INCLUDE_INACTIVE") == "true", 
    @query("SELECT id, email FROM users"),
    @filter(@query("SELECT id, email, status FROM users"), @equals(@item("status"), "active"))
  )
  
  # Array reduction
  total_revenue: @reduce(@query("SELECT amount FROM orders"), @add(@accumulator, @item("amount")), 0)
  user_count_by_status: @group_by(@query("SELECT id, status FROM users"), @item("status"))
TSK

# Ruby integration with transformation pipelines
class TransformationPipeline
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_processed_users
    @config['transformation_pipelines']['processed_users']
  end
  
  def get_conditional_users
    @config['transformation_pipelines']['conditional_users']
  end
  
  def get_total_revenue
    @config['transformation_pipelines']['total_revenue']
  end
  
  def get_user_count_by_status
    @config['transformation_pipelines']['user_count_by_status']
  end
  
  def generate_analytics_report
    {
      processed_users: get_processed_users,
      conditional_users: get_conditional_users,
      total_revenue: get_total_revenue,
      user_distribution: get_user_count_by_status
    }
  end
end
```

## 🛡️ Array Validation and Error Handling

### Array Validation

```ruby
# TuskLang with array validation
tsk_content = <<~TSK
  [array_validation]
  # Array validation
  has_users: @is_not_empty(@query("SELECT id FROM users"))
  has_orders: @is_not_empty(@query("SELECT id FROM orders"))
  array_is_valid: @is_array(@env("USER_IDS").split(","))
  
  # Array length validation
  user_count_ok: @between(@length(@query("SELECT id FROM users")), 1, 10000)
  order_count_ok: @less_than_or_equal(@length(@query("SELECT id FROM orders")), 50000)
  
  # Array content validation
  all_emails_valid: @all(@map(@query("SELECT email FROM users"), @matches(@item("email"), "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")))
  all_ids_positive: @all(@map(@query("SELECT id FROM users"), @greater_than(@item("id"), 0)))
TSK

# Ruby integration with array validation
class ArrayValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_data_integrity
    errors = []
    
    unless @config['array_validation']['has_users']
      errors << "No users found in database"
    end
    
    unless @config['array_validation']['user_count_ok']
      errors << "User count is outside acceptable range"
    end
    
    unless @config['array_validation']['all_emails_valid']
      errors << "Some user emails are invalid"
    end
    
    unless @config['array_validation']['all_ids_positive']
      errors << "Some user IDs are not positive"
    end
    
    {
      valid: errors.empty?,
      errors: errors
    }
  end
  
  def validate_array_structure(array_data)
    {
      is_array: @config['array_validation']['array_is_valid'],
      has_content: array_data.any?,
      length_ok: array_data.length.between?(1, 1000)
    }
  end
end
```

## 🚀 Performance Optimization

### Efficient Array Operations

```ruby
# TuskLang with performance optimizations
tsk_content = <<~TSK
  [optimized_arrays]
  # Cached array operations
  cached_user_ids: @cache("5m", @query("SELECT id FROM users WHERE active = 1"))
  cached_order_totals: @cache("1m", @sum(@query("SELECT amount FROM orders WHERE created_at > ?", @date.subtract("1h"))))
  
  # Lazy array evaluation
  expensive_filter: @when(@env("DEBUG"), @filter(@query("SELECT * FROM users"), @complex_condition()))
  
  # Batch array processing
  user_batches: @batch(@query("SELECT id FROM users"), 100)
  order_batches: @batch(@query("SELECT id FROM orders"), 50)
  
  # Array streaming
  stream_users: @stream(@query("SELECT id, email FROM users"), 1000)
TSK

# Ruby integration with optimized arrays
class OptimizedArrayProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_cached_user_ids
    @config['optimized_arrays']['cached_user_ids']
  end
  
  def get_cached_order_totals
    @config['optimized_arrays']['cached_order_totals']
  end
  
  def get_user_batches
    @config['optimized_arrays']['user_batches']
  end
  
  def get_order_batches
    @config['optimized_arrays']['order_batches']
  end
  
  def process_efficiently
    Rails.cache.fetch("array_operations", expires_in: 5.minutes) do
      {
        user_ids: get_cached_user_ids,
        order_totals: get_cached_order_totals,
        user_batches: get_user_batches,
        order_batches: get_order_batches
      }
    end
  end
  
  def process_in_streams
    stream = @config['optimized_arrays']['stream_users']
    stream.each do |batch|
      # Process batch
      Rails.logger.info "Processed batch of #{batch.length} users"
    end
  end
end
```

## 🎯 Best Practices

### 1. Use Descriptive Array Names
```ruby
# Good
active_user_ids: @filter(@query("SELECT id, status FROM users"), @equals(@item("status"), "active"))
premium_user_emails: @map(@filter(@query("SELECT email, subscription_type FROM users"), @equals(@item("subscription_type"), "premium")), @item("email"))

# Avoid
ids: @filter(@query("SELECT id, status FROM users"), @equals(@item("status"), "active"))
emails: @map(@filter(@query("SELECT email, subscription_type FROM users"), @equals(@item("subscription_type"), "premium")), @item("email"))
```

### 2. Validate Arrays Before Processing
```ruby
# TuskLang with array validation
tsk_content = <<~TSK
  [validation_first]
  # Validate before processing
  safe_user_ids: @when(@is_not_empty(@query("SELECT id FROM users")), @query("SELECT id FROM users"), [])
  
  # Check array structure
  valid_user_data: @when(@is_array(@query("SELECT id, email FROM users")), @process_users(@query("SELECT id, email FROM users")), [])
TSK
```

### 3. Use Caching for Expensive Operations
```ruby
# Cache expensive array operations
expensive_filter: @cache("10m", @filter(@query("SELECT * FROM large_table"), @complex_condition()))
```

### 4. Handle Empty Arrays
```ruby
# TuskLang with empty array handling
tsk_content = <<~TSK
  [empty_arrays]
  # Handle empty arrays
  safe_user_list: @when(@is_not_empty(@query("SELECT id FROM users")), @query("SELECT id FROM users"), [])
  
  # Provide defaults
  default_settings: @when(@is_not_empty(@env("CUSTOM_SETTINGS").split(",")), @env("CUSTOM_SETTINGS").split(","), ["default1", "default2"])
TSK
```

## 🔧 Troubleshooting

### Common Array Issues

```ruby
# Issue: Empty arrays causing errors
# Solution: Check for empty arrays
tsk_content = <<~TSK
  [empty_array_fixes]
  # Check before processing
  safe_array: @when(@is_not_empty(@query("SELECT id FROM users")), @query("SELECT id FROM users"), [])
  
  # Provide fallback
  fallback_array: @coalesce(@query("SELECT id FROM users"), [])
TSK

# Issue: Large arrays causing performance problems
# Solution: Use batching and streaming
tsk_content = <<~TSK
  [performance_fixes]
  # Use batching
  batched_users: @batch(@query("SELECT id FROM users"), 100)
  
  # Use streaming
  stream_users: @stream(@query("SELECT id FROM users"), 1000)
  
  # Use caching
  cached_users: @cache("5m", @query("SELECT id FROM users"))
TSK

# Issue: Array type mismatches
# Solution: Ensure consistent types
tsk_content = <<~TSK
  [type_fixes]
  # Ensure consistent types
  string_ids: @map(@query("SELECT id FROM users"), @to_string(@item("id")))
  numeric_amounts: @map(@query("SELECT amount FROM orders"), @to_number(@item("amount")))
TSK
```

## 🎯 Summary

TuskLang's array operations provide powerful data manipulation capabilities that integrate seamlessly with Ruby applications. By leveraging these operations, you can:

- **Manipulate collections dynamically** in configuration files
- **Transform and filter data** efficiently
- **Perform complex aggregations** and statistics
- **Handle large datasets** with batching and streaming
- **Validate and process arrays** safely

The Ruby integration makes these operations even more powerful by combining TuskLang's declarative syntax with Ruby's rich array processing libraries and object-oriented features.

**Remember**: TuskLang array operations are designed to be expressive, performant, and Ruby-friendly. Use them to create dynamic, efficient, and maintainable data processing configurations.

**Key Takeaways**:
- Always validate arrays before processing
- Use caching for expensive array operations
- Handle empty arrays gracefully
- Use batching and streaming for large datasets
- Combine with Ruby's array libraries for advanced functionality 
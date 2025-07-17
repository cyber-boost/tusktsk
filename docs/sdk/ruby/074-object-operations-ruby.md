# 🏗️ Object Operations in TuskLang - Ruby Edition

**"We don't bow to any king" - Ruby Edition**

TuskLang's object operations provide powerful data structure manipulation capabilities that integrate seamlessly with Ruby's rich object-oriented features, enabling dynamic object creation, transformation, and intelligent data modeling.

## 🎯 Overview

Object operations in TuskLang allow you to manipulate complex data structures directly in configuration files, enabling dynamic object creation, property access, and transformation. When combined with Ruby's powerful object-oriented capabilities, these operations become incredibly versatile.

## 🚀 Basic Object Operations

### Object Creation and Access

```ruby
# TuskLang configuration with object operations
tsk_content = <<~TSK
  [object_operations]
  # Object creation
  user_config: {
    id: @env("USER_ID"),
    name: @env("USER_NAME"),
    email: @env("USER_EMAIL"),
    role: @env("USER_ROLE")
  }
  
  # Nested objects
  app_config: {
    database: {
      host: @env("DB_HOST"),
      port: @env("DB_PORT"),
      name: @env("DB_NAME"),
      user: @env("DB_USER")
    },
    api: {
      base_url: @env("API_BASE_URL"),
      version: @env("API_VERSION"),
      timeout: @env("API_TIMEOUT")
    }
  }
  
  # Object property access
  db_host: @get(@app_config, "database.host")
  api_version: @get(@app_config, "api.version")
  user_name: @get(@user_config, "name")
TSK

# Ruby integration
require 'tusklang'

parser = TuskLang.new
config = parser.parse(tsk_content)

# Use in Ruby classes
class ObjectProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def user_config
    @config['object_operations']['user_config']
  end
  
  def app_config
    @config['object_operations']['app_config']
  end
  
  def db_host
    @config['object_operations']['db_host']
  end
  
  def api_version
    @config['object_operations']['api_version']
  end
  
  def user_name
    @config['object_operations']['user_name']
  end
  
  def get_database_config
    app_config['database']
  end
  
  def get_api_config
    app_config['api']
  end
  
  def validate_config
    {
      user_valid: user_config['id'] && user_config['email'],
      database_valid: get_database_config['host'] && get_database_config['name'],
      api_valid: get_api_config['base_url'] && get_api_config['version']
    }
  end
end

# Usage
processor = ObjectProcessor.new(config)
puts processor.validate_config
```

### Object Merging and Cloning

```ruby
# TuskLang with object merging
tsk_content = <<~TSK
  [object_merging]
  # Object merging
  default_config: {
    timeout: 30,
    retries: 3,
    cache_enabled: true
  }
  
  custom_config: {
    timeout: @env("CUSTOM_TIMEOUT"),
    cache_enabled: @env("CACHE_ENABLED") == "true"
  }
  
  # Merge objects
  final_config: @merge(@default_config, @custom_config)
  
  # Deep merge
  base_settings: {
    logging: {
      level: "info",
      format: "json"
    },
    security: {
      ssl: true,
      cors: false
    }
  }
  
  override_settings: {
    logging: {
      level: @env("LOG_LEVEL")
    },
    security: {
      cors: @env("ENABLE_CORS") == "true"
    }
  }
  
  merged_settings: @deep_merge(@base_settings, @override_settings)
TSK

# Ruby integration with object merging
class ObjectMerger
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_final_config
    @config['object_merging']['final_config']
  end
  
  def get_merged_settings
    @config['object_merging']['merged_settings']
  end
  
  def get_default_config
    @config['object_merging']['default_config']
  end
  
  def get_custom_config
    @config['object_merging']['custom_config']
  end
  
  def apply_config_overrides(base_config, overrides)
    # Simulate deep merge in Ruby
    merged = base_config.deep_dup
    overrides.each do |key, value|
      if value.is_a?(Hash) && merged[key].is_a?(Hash)
        merged[key] = apply_config_overrides(merged[key], value)
      else
        merged[key] = value
      end
    end
    merged
  end
end
```

## 🔧 Advanced Object Operations

### Object Transformation and Mapping

```ruby
# TuskLang with object transformation
tsk_content = <<~TSK
  [object_transformation]
  # Object mapping
  user_objects: @map(@query("SELECT id, name, email, role FROM users"), {
    id: @item("id"),
    name: @item("name"),
    email: @item("email"),
    role: @item("role"),
    display_name: @concat(@item("name"), " (", @item("role"), ")")
  })
  
  # Object filtering
  active_users: @filter(@user_objects, @equals(@item("role"), "active"))
  admin_users: @filter(@user_objects, @equals(@item("role"), "admin"))
  
  # Object sorting
  users_by_name: @sort(@user_objects, @item("name"))
  users_by_role: @sort(@user_objects, @item("role"))
  
  # Object grouping
  users_by_role: @group_by(@user_objects, @item("role"))
TSK

# Ruby integration with object transformation
class ObjectTransformer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_user_objects
    @config['object_transformation']['user_objects']
  end
  
  def get_active_users
    @config['object_transformation']['active_users']
  end
  
  def get_admin_users
    @config['object_transformation']['admin_users']
  end
  
  def get_users_by_name
    @config['object_transformation']['users_by_name']
  end
  
  def get_users_by_role
    @config['object_transformation']['users_by_role']
  end
  
  def get_users_grouped_by_role
    @config['object_transformation']['users_by_role']
  end
  
  def generate_user_report
    {
      total_users: get_user_objects.length,
      active_users: get_active_users.length,
      admin_users: get_admin_users.length,
      users_by_role: get_users_grouped_by_role,
      sorted_users: get_users_by_name
    }
  end
end
```

### Object Validation and Schema

```ruby
# TuskLang with object validation
tsk_content = <<~TSK
  [object_validation]
  # Object schema validation
  user_schema: {
    id: "number",
    name: "string",
    email: "string",
    role: "string"
  }
  
  # Validate object against schema
  user_valid: @validate_schema(@user_config, @user_schema)
  
  # Required fields validation
  required_fields: ["id", "name", "email"]
  has_required_fields: @has_all_keys(@user_config, @required_fields)
  
  # Object property validation
  email_valid: @matches(@get(@user_config, "email"), "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")
  role_valid: @in_array(@get(@user_config, "role"), ["user", "admin", "moderator"])
  
  # Nested object validation
  config_valid: @and(
    @has_key(@app_config, "database"),
    @has_key(@app_config, "api"),
    @has_key(@get(@app_config, "database"), "host"),
    @has_key(@get(@app_config, "api"), "base_url")
  )
TSK

# Ruby integration with object validation
class ObjectValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_user_object(user_data)
    errors = []
    
    unless @config['object_validation']['user_valid']
      errors << "User object does not match schema"
    end
    
    unless @config['object_validation']['has_required_fields']
      errors << "User object missing required fields"
    end
    
    unless @config['object_validation']['email_valid']
      errors << "Invalid email format"
    end
    
    unless @config['object_validation']['role_valid']
      errors << "Invalid role value"
    end
    
    {
      valid: errors.empty?,
      errors: errors
    }
  end
  
  def validate_app_config
    @config['object_validation']['config_valid']
  end
  
  def validate_object_structure(obj, schema)
    schema.each do |key, expected_type|
      unless obj.key?(key)
        return false
      end
      
      actual_type = obj[key].class.name.downcase
      unless actual_type == expected_type
        return false
      end
    end
    true
  end
end
```

## 🎛️ Object Serialization and Deserialization

### JSON Operations

```ruby
# TuskLang with JSON operations
tsk_content = <<~TSK
  [json_operations]
  # Object to JSON
  user_json: @to_json(@user_config)
  app_config_json: @to_json(@app_config)
  
  # JSON to object
  parsed_user: @from_json(@env("USER_JSON"))
  parsed_config: @from_json(@file.read("config.json"))
  
  # JSON validation
  json_valid: @is_valid_json(@env("USER_JSON"))
  json_schema_valid: @validate_json_schema(@env("USER_JSON"), @user_schema)
  
  # JSON transformation
  user_response: @format_json({
    status: "success",
    data: @user_config,
    timestamp: @date.now(),
    version: @env("API_VERSION")
  })
TSK

# Ruby integration with JSON operations
class JsonProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_user_json
    @config['json_operations']['user_json']
  end
  
  def get_app_config_json
    @config['json_operations']['app_config_json']
  end
  
  def get_parsed_user
    @config['json_operations']['parsed_user']
  end
  
  def get_parsed_config
    @config['json_operations']['parsed_config']
  end
  
  def get_user_response
    JSON.parse(@config['json_operations']['user_response'])
  end
  
  def validate_json_input(json_string)
    {
      is_valid: @config['json_operations']['json_valid'],
      schema_valid: @config['json_operations']['json_schema_valid']
    }
  end
  
  def serialize_user_data(user_data)
    {
      json: @config['json_operations']['user_json'],
      response: get_user_response
    }
  end
end
```

### Object Cloning and Immutability

```ruby
# TuskLang with object cloning
tsk_content = <<~TSK
  [object_cloning]
  # Object cloning
  cloned_user: @clone(@user_config)
  cloned_config: @deep_clone(@app_config)
  
  # Immutable object creation
  immutable_user: @freeze(@user_config)
  immutable_config: @deep_freeze(@app_config)
  
  # Object with computed properties
  computed_user: @computed(@user_config, {
    full_name: @concat(@item("first_name"), " ", @item("last_name")),
    display_role: @upper(@item("role")),
    created_date: @date.format(@item("created_at"), "Y-m-d")
  })
  
  # Object transformation with preservation
  transformed_user: @transform(@user_config, {
    name: @upper(@item("name")),
    email: @lower(@item("email"))
  })
TSK

# Ruby integration with object cloning
class ObjectCloner
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_cloned_user
    @config['object_cloning']['cloned_user']
  end
  
  def get_cloned_config
    @config['object_cloning']['cloned_config']
  end
  
  def get_immutable_user
    @config['object_cloning']['immutable_user']
  end
  
  def get_computed_user
    @config['object_cloning']['computed_user']
  end
  
  def get_transformed_user
    @config['object_cloning']['transformed_user']
  end
  
  def create_immutable_copy(obj)
    # Create a frozen copy in Ruby
    obj.deep_dup.freeze
  end
  
  def add_computed_properties(obj)
    obj.dup.tap do |computed|
      computed[:full_name] = "#{obj[:first_name]} #{obj[:last_name]}"
      computed[:display_role] = obj[:role].upcase
      computed[:created_date] = obj[:created_at].strftime("%Y-%m-%d")
    end
  end
end
```

## 🔄 Dynamic Object Operations

### Object Factory and Builder Patterns

```ruby
# TuskLang with object factory patterns
tsk_content = <<~TSK
  [object_factory]
  # Object factory
  user_factory: @factory({
    id: @generate_id(),
    name: @env("USER_NAME"),
    email: @env("USER_EMAIL"),
    role: @env("USER_ROLE") || "user",
    created_at: @date.now(),
    status: "active"
  })
  
  # Conditional object creation
  conditional_user: @when(@env("IS_ADMIN") == "true", {
    id: @generate_id(),
    name: @env("USER_NAME"),
    email: @env("USER_EMAIL"),
    role: "admin",
    permissions: ["read", "write", "delete", "admin"]
  }, {
    id: @generate_id(),
    name: @env("USER_NAME"),
    email: @env("USER_EMAIL"),
    role: "user",
    permissions: ["read", "write"]
  })
  
  # Object builder pattern
  user_builder: @builder({
    base: {
      id: @generate_id(),
      created_at: @date.now(),
      status: "active"
    },
    with_name: @concat(@base, {name: @env("USER_NAME")}),
    with_email: @concat(@with_name, {email: @env("USER_EMAIL")}),
    with_role: @concat(@with_email, {role: @env("USER_ROLE") || "user"})
  })
TSK

# Ruby integration with object factory
class ObjectFactory
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def create_user
    @config['object_factory']['user_factory']
  end
  
  def create_conditional_user
    @config['object_factory']['conditional_user']
  end
  
  def build_user
    @config['object_factory']['user_builder']['with_role']
  end
  
  def generate_user_with_defaults
    {
      id: SecureRandom.uuid,
      name: ENV['USER_NAME'],
      email: ENV['USER_EMAIL'],
      role: ENV['USER_ROLE'] || 'user',
      created_at: Time.current,
      status: 'active'
    }
  end
  
  def create_user_with_permissions(is_admin)
    base_user = generate_user_with_defaults
    
    if is_admin
      base_user.merge({
        role: 'admin',
        permissions: ['read', 'write', 'delete', 'admin']
      })
    else
      base_user.merge({
        role: 'user',
        permissions: ['read', 'write']
      })
    end
  end
end
```

### Object Composition and Inheritance

```ruby
# TuskLang with object composition
tsk_content = <<~TSK
  [object_composition]
  # Object composition
  base_entity: {
    id: @generate_id(),
    created_at: @date.now(),
    updated_at: @date.now()
  }
  
  user_entity: @compose(@base_entity, {
    name: @env("USER_NAME"),
    email: @env("USER_EMAIL"),
    role: @env("USER_ROLE")
  })
  
  product_entity: @compose(@base_entity, {
    name: @env("PRODUCT_NAME"),
    price: @env("PRODUCT_PRICE"),
    category: @env("PRODUCT_CATEGORY")
  })
  
  # Object inheritance
  admin_user: @extend(@user_entity, {
    role: "admin",
    permissions: ["read", "write", "delete", "admin"],
    admin_level: @env("ADMIN_LEVEL") || 1
  })
  
  # Object mixins
  audit_mixin: {
    created_by: @env("CREATED_BY"),
    modified_by: @env("MODIFIED_BY"),
    audit_trail: @array([])
  }
  
  audited_user: @mixin(@user_entity, @audit_mixin)
TSK

# Ruby integration with object composition
class ObjectComposer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_base_entity
    @config['object_composition']['base_entity']
  end
  
  def get_user_entity
    @config['object_composition']['user_entity']
  end
  
  def get_product_entity
    @config['object_composition']['product_entity']
  end
  
  def get_admin_user
    @config['object_composition']['admin_user']
  end
  
  def get_audited_user
    @config['object_composition']['audited_user']
  end
  
  def create_base_entity
    {
      id: SecureRandom.uuid,
      created_at: Time.current,
      updated_at: Time.current
    }
  end
  
  def compose_entity(base, additional_props)
    base.merge(additional_props)
  end
  
  def extend_entity(base, extensions)
    base.merge(extensions)
  end
end
```

## 🛡️ Object Security and Validation

### Object Security

```ruby
# TuskLang with object security
tsk_content = <<~TSK
  [object_security]
  # Object sanitization
  sanitized_user: @sanitize_object(@user_config, ["id", "name", "email"], ["password", "token", "secret"])
  
  # Object encryption
  encrypted_user: @encrypt_object(@user_config, @env("ENCRYPTION_KEY"))
  decrypted_user: @decrypt_object(@encrypted_user, @env("ENCRYPTION_KEY"))
  
  # Object signing
  signed_user: @sign_object(@user_config, @env("SIGNING_KEY"))
  verified_user: @verify_object(@signed_user, @env("SIGNING_KEY"))
  
  # Object access control
  public_user: @public_fields(@user_config, ["id", "name", "role"])
  private_user: @private_fields(@user_config, ["email", "password", "token"])
TSK

# Ruby integration with object security
class ObjectSecurity
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_sanitized_user
    @config['object_security']['sanitized_user']
  end
  
  def get_encrypted_user
    @config['object_security']['encrypted_user']
  end
  
  def get_decrypted_user
    @config['object_security']['decrypted_user']
  end
  
  def get_signed_user
    @config['object_security']['signed_user']
  end
  
  def get_verified_user
    @config['object_security']['verified_user']
  end
  
  def get_public_user
    @config['object_security']['public_user']
  end
  
  def sanitize_object(obj, allowed_fields, sensitive_fields)
    sanitized = {}
    allowed_fields.each do |field|
      sanitized[field] = obj[field] if obj.key?(field)
    end
    sanitized
  end
  
  def encrypt_object(obj, key)
    # Implement encryption logic
    cipher = OpenSSL::Cipher.new('AES-256-GCM')
    cipher.encrypt
    cipher.key = key
    encrypted = cipher.update(obj.to_json) + cipher.final
    {
      data: Base64.strict_encode64(encrypted),
      iv: Base64.strict_encode64(cipher.random_iv),
      tag: Base64.strict_encode64(cipher.auth_tag)
    }
  end
end
```

## 🚀 Performance Optimization

### Efficient Object Operations

```ruby
# TuskLang with performance optimizations
tsk_content = <<~TSK
  [optimized_objects]
  # Cached object operations
  cached_user: @cache("5m", @user_config)
  cached_config: @cache("1h", @app_config)
  
  # Lazy object evaluation
  expensive_object: @when(@env("DEBUG"), @complex_object_creation())
  
  # Object pooling
  user_pool: @object_pool(@user_factory, 100)
  
  # Object streaming
  stream_objects: @stream_objects(@query("SELECT * FROM users"), 1000)
TSK

# Ruby integration with optimized objects
class OptimizedObjectProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_cached_user
    @config['optimized_objects']['cached_user']
  end
  
  def get_cached_config
    @config['optimized_objects']['cached_config']
  end
  
  def get_user_pool
    @config['optimized_objects']['user_pool']
  end
  
  def process_efficiently
    Rails.cache.fetch("object_operations", expires_in: 5.minutes) do
      {
        cached_user: get_cached_user,
        cached_config: get_cached_config,
        user_pool: get_user_pool
      }
    end
  end
  
  def process_object_stream
    stream = @config['optimized_objects']['stream_objects']
    stream.each do |batch|
      # Process batch of objects
      Rails.logger.info "Processed batch of #{batch.length} objects"
    end
  end
end
```

## 🎯 Best Practices

### 1. Use Descriptive Object Names
```ruby
# Good
user_configuration: {id: @env("USER_ID"), name: @env("USER_NAME")}
database_connection: {host: @env("DB_HOST"), port: @env("DB_PORT")}

# Avoid
config: {id: @env("USER_ID"), name: @env("USER_NAME")}
db: {host: @env("DB_HOST"), port: @env("DB_PORT")}
```

### 2. Validate Objects Before Processing
```ruby
# TuskLang with object validation
tsk_content = <<~TSK
  [validation_first]
  # Validate before processing
  safe_user: @when(@validate_schema(@user_config, @user_schema), @user_config, {})
  
  # Sanitize before use
  clean_object: @sanitize_object(@user_config, ["id", "name"], ["password"])
TSK
```

### 3. Use Caching for Expensive Operations
```ruby
# Cache expensive object operations
expensive_object: @cache("10m", @complex_object_creation())
```

### 4. Handle Object Edge Cases
```ruby
# TuskLang with edge case handling
tsk_content = <<~TSK
  [edge_cases]
  # Handle null objects
  safe_object: @coalesce(@user_config, {})
  
  # Handle missing properties
  safe_property: @get(@user_config, "name") || "Unknown"
  
  # Handle object type mismatches
  type_safe_object: @ensure_object(@user_data)
TSK
```

## 🔧 Troubleshooting

### Common Object Issues

```ruby
# Issue: Null object references
# Solution: Use safe object access
tsk_content = <<~TSK
  [null_object_fixes]
  # Safe object access
  safe_property: @get(@user_config, "name") || "Default"
  
  # Null coalescing
  safe_object: @coalesce(@user_config, {})
TSK

# Issue: Object type mismatches
# Solution: Ensure proper object types
tsk_content = <<~TSK
  [type_fixes]
  # Ensure object type
  safe_object: @ensure_object(@user_data)
  
  # Type conversion
  string_object: @to_object(@user_string)
TSK

# Issue: Object performance problems
# Solution: Use optimization techniques
tsk_content = <<~TSK
  [performance_fixes]
  # Use object caching
  cached_object: @cache("5m", @expensive_object_creation())
  
  # Use object pooling
  pooled_object: @object_pool(@object_factory, 100)
  
  # Use object streaming
  stream_objects: @stream_objects(@large_dataset, 1000)
TSK
```

## 🎯 Summary

TuskLang's object operations provide powerful data structure manipulation capabilities that integrate seamlessly with Ruby applications. By leveraging these operations, you can:

- **Manipulate complex objects** dynamically in configuration files
- **Create and transform objects** efficiently
- **Validate and secure objects** safely
- **Handle object composition** and inheritance patterns
- **Optimize object performance** with caching and pooling

The Ruby integration makes these operations even more powerful by combining TuskLang's declarative syntax with Ruby's rich object-oriented features and libraries.

**Remember**: TuskLang object operations are designed to be expressive, performant, and Ruby-friendly. Use them to create dynamic, efficient, and maintainable object processing configurations.

**Key Takeaways**:
- Always validate objects before processing
- Use caching for expensive object operations
- Handle null objects and edge cases gracefully
- Implement proper object security measures
- Combine with Ruby's object libraries for advanced functionality 
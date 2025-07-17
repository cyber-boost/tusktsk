# 📝 String Operations in TuskLang - Ruby Edition

**"We don't bow to any king" - Ruby Edition**

TuskLang's string operations provide powerful text manipulation capabilities that integrate seamlessly with Ruby's rich string processing features, enabling dynamic content generation, data transformation, and intelligent text processing.

## 🎯 Overview

String operations in TuskLang allow you to manipulate text directly in configuration files, enabling dynamic content generation, data formatting, and intelligent text processing. When combined with Ruby's powerful string manipulation capabilities, these operations become incredibly versatile.

## 🚀 Basic String Operations

### String Concatenation and Formatting

```ruby
# TuskLang configuration with string operations
tsk_content = <<~TSK
  [string_operations]
  # Basic concatenation
  full_name: @concat(@env("FIRST_NAME"), " ", @env("LAST_NAME"))
  api_url: @concat(@env("API_BASE_URL"), "/v", @env("API_VERSION"), "/users")
  
  # String formatting
  welcome_message: @format("Welcome, {name}! You have {count} unread messages.", @env("USER_NAME"), @query("SELECT COUNT(*) FROM messages WHERE user_id = ? AND read = 0", @env("USER_ID")))
  
  # Template strings
  email_template: @template("Hello {name}, your order #{order_id} has been shipped to {address}.", {
    name: @env("CUSTOMER_NAME"),
    order_id: @env("ORDER_ID"),
    address: @env("SHIPPING_ADDRESS")
  })
TSK

# Ruby integration
require 'tusklang'

parser = TuskLang.new
config = parser.parse(tsk_content)

# Use in Ruby classes
class StringProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def full_name
    @config['string_operations']['full_name']
  end
  
  def api_url
    @config['string_operations']['api_url']
  end
  
  def welcome_message
    @config['string_operations']['welcome_message']
  end
  
  def email_template
    @config['string_operations']['email_template']
  end
  
  def generate_personalized_content
    {
      greeting: welcome_message,
      api_endpoint: api_url,
      customer_name: full_name,
      notification: email_template
    }
  end
end

# Usage
processor = StringProcessor.new(config)
puts processor.generate_personalized_content
```

### String Case Operations

```ruby
# TuskLang with case manipulation
tsk_content = <<~TSK
  [case_operations]
  # Case conversion
  uppercase_name: @upper(@env("USER_NAME"))
  lowercase_email: @lower(@env("EMAIL"))
  title_case_title: @title(@env("PAGE_TITLE"))
  camel_case_variable: @camel(@env("VARIABLE_NAME"))
  snake_case_field: @snake(@env("FIELD_NAME"))
  
  # Case-sensitive comparisons
  exact_match: @equals_case_sensitive(@env("ENVIRONMENT"), "PRODUCTION")
  case_insensitive_match: @equals_ignore_case(@env("ENVIRONMENT"), "production")
  
  # Case validation
  is_uppercase: @is_upper(@env("API_KEY"))
  is_lowercase: @is_lower(@env("email"))
TSK

# Ruby integration with case operations
class CaseProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def format_user_data
    {
      display_name: @config['case_operations']['uppercase_name'],
      email_normalized: @config['case_operations']['lowercase_email'],
      page_title: @config['case_operations']['title_case_title'],
      variable_name: @config['case_operations']['camel_case_variable'],
      field_name: @config['case_operations']['snake_case_field']
    }
  end
  
  def validate_environment(env)
    if @config['case_operations']['exact_match']
      env == "PRODUCTION"
    elsif @config['case_operations']['case_insensitive_match']
      env.downcase == "production"
    else
      false
    end
  end
  
  def validate_api_key(api_key)
    @config['case_operations']['is_uppercase'] && api_key == api_key.upcase
  end
end
```

## 🔧 Advanced String Manipulation

### Pattern Matching and Replacement

```ruby
# TuskLang with pattern matching
tsk_content = <<~TSK
  [pattern_matching]
  # Regex matching
  valid_email: @matches(@env("EMAIL"), "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")
  valid_phone: @matches(@env("PHONE"), "^\\+?[1-9]\\d{1,14}$")
  valid_url: @matches(@env("WEBSITE"), "^https?://[^\\s/$.?#].[^\\s]*$")
  
  # String replacement
  clean_phone: @replace(@env("PHONE"), "[^0-9+]", "")
  normalized_email: @replace(@env("EMAIL"), "@gmail\\.com$", "@googlemail.com")
  sanitized_filename: @replace(@env("FILENAME"), "[^a-zA-Z0-9._-]", "_")
  
  # Multiple replacements
  clean_text: @replace_all(@env("USER_INPUT"), {
    "<script>": "",
    "javascript:": "",
    "onclick=": "data-click="
  })
TSK

# Ruby integration with pattern matching
class PatternMatcher
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_user_data
    {
      email_valid: @config['pattern_matching']['valid_email'],
      phone_valid: @config['pattern_matching']['valid_phone'],
      url_valid: @config['pattern_matching']['valid_url']
    }
  end
  
  def sanitize_data
    {
      clean_phone: @config['pattern_matching']['clean_phone'],
      normalized_email: @config['pattern_matching']['normalized_email'],
      safe_filename: @config['pattern_matching']['sanitized_filename']
    }
  end
  
  def validate_and_clean(user_data)
    errors = []
    
    unless @config['pattern_matching']['valid_email']
      errors << "Invalid email format"
    end
    
    unless @config['pattern_matching']['valid_phone']
      errors << "Invalid phone format"
    end
    
    {
      valid: errors.empty?,
      errors: errors,
      cleaned_data: sanitize_data
    }
  end
end
```

### String Extraction and Parsing

```ruby
# TuskLang with string extraction
tsk_content = <<~TSK
  [string_extraction]
  # Substring operations
  domain_name: @substring(@env("EMAIL"), @add(@index_of(@env("EMAIL"), "@"), 1))
  file_extension: @substring(@env("FILENAME"), @add(@last_index_of(@env("FILENAME"), "."), 1))
  
  # String splitting
  name_parts: @split(@env("FULL_NAME"), " ")
  url_parts: @split(@env("API_URL"), "/")
  query_params: @split(@env("QUERY_STRING"), "&")
  
  # String joining
  csv_line: @join(@query("SELECT name, email, phone FROM users WHERE id = ?", @env("USER_ID")), ",")
  path_components: @join([@env("BASE_PATH"), @env("VERSION"), @env("ENDPOINT")], "/")
  
  # String trimming
  clean_input: @trim(@env("USER_INPUT"))
  normalized_whitespace: @normalize_whitespace(@env("TEXT_CONTENT"))
TSK

# Ruby integration with string extraction
class StringExtractor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def extract_domain(email)
    @config['string_extraction']['domain_name']
  end
  
  def get_file_extension(filename)
    @config['string_extraction']['file_extension']
  end
  
  def parse_name(full_name)
    parts = @config['string_extraction']['name_parts']
    {
      first_name: parts[0],
      last_name: parts[1..-1].join(" ")
    }
  end
  
  def build_api_path
    @config['string_extraction']['path_components']
  end
  
  def clean_user_input
    @config['string_extraction']['clean_input']
  end
  
  def process_user_data(user_data)
    {
      domain: extract_domain(user_data[:email]),
      name_parts: parse_name(user_data[:full_name]),
      clean_input: clean_user_input,
      api_path: build_api_path
    }
  end
end
```

## 🎛️ String Validation and Sanitization

### Input Validation

```ruby
# TuskLang with input validation
tsk_content = <<~TSK
  [input_validation]
  # Length validation
  name_length_ok: @between(@length(@env("USER_NAME")), 2, 50)
  password_strong: @greater_than_or_equal(@length(@env("PASSWORD")), 8)
  description_short: @less_than_or_equal(@length(@env("DESCRIPTION")), 500)
  
  # Content validation
  contains_required: @contains_all(@env("USER_INPUT"), ["@", "."])
  contains_forbidden: @contains_any(@env("USER_INPUT"), ["<script>", "javascript:", "onclick="])
  starts_with_valid: @starts_with(@env("API_URL"), "https://")
  ends_with_valid: @ends_with(@env("FILENAME"), ".json")
  
  # Character validation
  alphanumeric_only: @matches(@env("USERNAME"), "^[a-zA-Z0-9_]+$")
  no_special_chars: @not_contains_any(@env("FILENAME"), ["/", "\\", ":", "*", "?", "\"", "<", ">", "|"])
TSK

# Ruby integration with input validation
class InputValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_user_input(user_data)
    errors = []
    
    unless @config['input_validation']['name_length_ok']
      errors << "Name must be between 2 and 50 characters"
    end
    
    unless @config['input_validation']['password_strong']
      errors << "Password must be at least 8 characters"
    end
    
    if @config['input_validation']['contains_forbidden']
      errors << "Input contains forbidden content"
    end
    
    unless @config['input_validation']['alphanumeric_only']
      errors << "Username must contain only alphanumeric characters and underscores"
    end
    
    {
      valid: errors.empty?,
      errors: errors
    }
  end
  
  def validate_file_upload(filename)
    errors = []
    
    unless @config['input_validation']['ends_with_valid']
      errors << "File must have .json extension"
    end
    
    if @config['input_validation']['no_special_chars']
      errors << "Filename contains invalid characters"
    end
    
    {
      valid: errors.empty?,
      errors: errors
    }
  end
end
```

### Data Sanitization

```ruby
# TuskLang with data sanitization
tsk_content = <<~TSK
  [data_sanitization]
  # HTML sanitization
  clean_html: @strip_tags(@env("HTML_CONTENT"))
  safe_html: @sanitize_html(@env("USER_HTML"), ["p", "br", "strong", "em"])
  
  # SQL injection prevention
  safe_sql_value: @escape_sql(@env("USER_INPUT"))
  safe_sql_like: @escape_sql_like(@env("SEARCH_TERM"))
  
  # XSS prevention
  safe_output: @escape_html(@env("USER_INPUT"))
  safe_js: @escape_javascript(@env("USER_INPUT"))
  safe_url: @escape_url(@env("USER_INPUT"))
  
  # Data normalization
  normalized_phone: @normalize_phone(@env("PHONE"))
  normalized_email: @normalize_email(@env("EMAIL"))
  normalized_username: @normalize_username(@env("USERNAME"))
TSK

# Ruby integration with data sanitization
class DataSanitizer
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def sanitize_user_input(user_data)
    {
      clean_html: @config['data_sanitization']['clean_html'],
      safe_output: @config['data_sanitization']['safe_output'],
      safe_js: @config['data_sanitization']['safe_js'],
      safe_url: @config['data_sanitization']['safe_url']
    }
  end
  
  def normalize_contact_info(contact_data)
    {
      phone: @config['data_sanitization']['normalized_phone'],
      email: @config['data_sanitization']['normalized_email'],
      username: @config['data_sanitization']['normalized_username']
    }
  end
  
  def prepare_for_database(user_input)
    {
      safe_sql_value: @config['data_sanitization']['safe_sql_value'],
      safe_sql_like: @config['data_sanitization']['safe_sql_like']
    }
  end
end
```

## 🔄 Dynamic String Generation

### Template Processing

```ruby
# TuskLang with template processing
tsk_content = <<~TSK
  [template_processing]
  # Dynamic templates
  welcome_email: @template("""
    Dear {name},
    
    Welcome to {app_name}! Your account has been successfully created.
    
    Account Details:
    - Username: {username}
    - Email: {email}
    - Created: {created_date}
    
    Best regards,
    The {app_name} Team
  """, {
    name: @env("USER_NAME"),
    app_name: @env("APP_NAME"),
    username: @env("USERNAME"),
    email: @env("EMAIL"),
    created_date: @date.format(@env("CREATED_AT"), "Y-m-d H:i:s")
  })
  
  # Conditional templates
  status_message: @template_if(@env("USER_STATUS") == "active", 
    "Welcome back, {name}! You have {unread_count} unread messages.",
    "Hello {name}, please activate your account to continue.",
    {
      name: @env("USER_NAME"),
      unread_count: @query("SELECT COUNT(*) FROM messages WHERE user_id = ? AND read = 0", @env("USER_ID"))
    }
  )
  
  # Loop templates
  user_list: @template_loop(@query("SELECT name, email FROM users LIMIT 10"), """
    - {name} ({email})
  """)
TSK

# Ruby integration with template processing
class TemplateProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def generate_welcome_email
    @config['template_processing']['welcome_email']
  end
  
  def get_status_message
    @config['template_processing']['status_message']
  end
  
  def get_user_list
    @config['template_processing']['user_list']
  end
  
  def process_all_templates
    {
      welcome_email: generate_welcome_email,
      status_message: get_status_message,
      user_list: get_user_list
    }
  end
end
```

### Dynamic Content Generation

```ruby
# TuskLang with dynamic content
tsk_content = <<~TSK
  [dynamic_content]
  # API response formatting
  api_response: @format_json({
    status: "success",
    data: {
      user_id: @env("USER_ID"),
      name: @env("USER_NAME"),
      email: @env("EMAIL"),
      created_at: @date.format(@env("CREATED_AT"), "Y-m-d\\TH:i:s\\Z"),
      last_login: @date.format(@query("SELECT last_login FROM users WHERE id = ?", @env("USER_ID")), "Y-m-d\\TH:i:s\\Z")
    },
    meta: {
      timestamp: @date.now(),
      version: @env("API_VERSION")
    }
  })
  
  # Log message formatting
  log_message: @format("User {user_id} ({email}) performed {action} at {timestamp}", 
    @env("USER_ID"), 
    @env("EMAIL"), 
    @env("ACTION"), 
    @date.format(@date.now(), "Y-m-d H:i:s")
  )
  
  # Error message formatting
  error_message: @format("Error {error_code}: {error_description} occurred while processing {operation}", 
    @env("ERROR_CODE"), 
    @env("ERROR_DESCRIPTION"), 
    @env("OPERATION")
  )
TSK

# Ruby integration with dynamic content
class DynamicContentGenerator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def generate_api_response
    JSON.parse(@config['dynamic_content']['api_response'])
  end
  
  def generate_log_message
    @config['dynamic_content']['log_message']
  end
  
  def generate_error_message
    @config['dynamic_content']['error_message']
  end
  
  def log_user_action(user_id, email, action)
    log_message = generate_log_message
    Rails.logger.info log_message
  end
  
  def handle_error(error_code, description, operation)
    error_message = generate_error_message
    Rails.logger.error error_message
  end
end
```

## 🛡️ String Security and Validation

### Security Validation

```ruby
# TuskLang with security validation
tsk_content = <<~TSK
  [security_validation]
  # Injection prevention
  no_sql_injection: @not_contains_any(@env("USER_INPUT"), ["'", "\"", ";", "--", "/*", "*/", "xp_", "sp_"])
  no_xss_attempt: @not_contains_any(@env("USER_INPUT"), ["<script", "javascript:", "onclick", "onload", "onerror"])
  no_path_traversal: @not_contains_any(@env("FILENAME"), ["..", "/", "\\", ":", "*", "?", "\"", "<", ">", "|"])
  
  # Content security
  safe_filename: @matches(@env("FILENAME"), "^[a-zA-Z0-9._-]+$")
  safe_username: @matches(@env("USERNAME"), "^[a-zA-Z0-9_]{3,20}$")
  safe_email: @matches(@env("EMAIL"), "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")
  
  # Length limits for security
  input_length_safe: @less_than_or_equal(@length(@env("USER_INPUT")), 1000)
  filename_length_safe: @less_than_or_equal(@length(@env("FILENAME")), 255)
TSK

# Ruby integration with security validation
class SecurityValidator
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def validate_input_security(user_input)
    errors = []
    
    if @config['security_validation']['no_sql_injection']
      errors << "Input contains potential SQL injection characters"
    end
    
    if @config['security_validation']['no_xss_attempt']
      errors << "Input contains potential XSS content"
    end
    
    unless @config['security_validation']['input_length_safe']
      errors << "Input exceeds maximum allowed length"
    end
    
    {
      secure: errors.empty?,
      errors: errors
    }
  end
  
  def validate_file_security(filename)
    errors = []
    
    if @config['security_validation']['no_path_traversal']
      errors << "Filename contains path traversal characters"
    end
    
    unless @config['security_validation']['safe_filename']
      errors << "Filename contains invalid characters"
    end
    
    unless @config['security_validation']['filename_length_safe']
      errors << "Filename exceeds maximum length"
    end
    
    {
      secure: errors.empty?,
      errors: errors
    }
  end
end
```

## 🚀 Performance Optimization

### Efficient String Operations

```ruby
# TuskLang with performance optimizations
tsk_content = <<~TSK
  [optimized_strings]
  # Cached string operations
  cached_user_greeting: @cache("5m", @format("Hello, {name}! You have {count} messages.", @env("USER_NAME"), @query("SELECT COUNT(*) FROM messages WHERE user_id = ?", @env("USER_ID"))))
  
  # Lazy string evaluation
  expensive_format: @when(@env("DEBUG"), @format("Detailed log: User {user_id} performed {action} with data {data}", @env("USER_ID"), @env("ACTION"), @json.encode(@env("REQUEST_DATA"))))
  
  # Batch string processing
  user_summaries: @batch_map(@query("SELECT id, name, email FROM users LIMIT 10"), @format("User {id}: {name} ({email})"))
  
  # String pooling
  common_messages: @string_pool([
    @format("Welcome, {name}!", @env("USER_NAME")),
    @format("Goodbye, {name}!", @env("USER_NAME")),
    @format("Hello, {name}!", @env("USER_NAME"))
  ])
TSK

# Ruby integration with optimized strings
class OptimizedStringProcessor
  def initialize(tusk_config)
    @config = tusk_config
  end
  
  def get_cached_greeting
    @config['optimized_strings']['cached_user_greeting']
  end
  
  def get_debug_message
    @config['optimized_strings']['expensive_format']
  end
  
  def get_user_summaries
    @config['optimized_strings']['user_summaries']
  end
  
  def get_common_messages
    @config['optimized_strings']['common_messages']
  end
  
  def process_efficiently
    Rails.cache.fetch("string_operations", expires_in: 5.minutes) do
      {
        greeting: get_cached_greeting,
        summaries: get_user_summaries,
        messages: get_common_messages
      }
    end
  end
end
```

## 🎯 Best Practices

### 1. Use Descriptive String Names
```ruby
# Good
user_welcome_message: @format("Welcome back, {name}!", @env("USER_NAME"))
api_error_response: @format_json({error: @env("ERROR_MESSAGE"), code: @env("ERROR_CODE")})

# Avoid
msg: @format("Welcome back, {name}!", @env("USER_NAME"))
err: @format_json({error: @env("ERROR_MESSAGE"), code: @env("ERROR_CODE")})
```

### 2. Validate Input Before Processing
```ruby
# TuskLang with input validation
tsk_content = <<~TSK
  [validation_first]
  # Validate before processing
  safe_user_input: @when(@is_safe(@env("USER_INPUT")), @process(@env("USER_INPUT")), "Invalid input")
  
  # Sanitize before use
  clean_template_data: @sanitize_all({
    name: @env("USER_NAME"),
    email: @env("EMAIL"),
    message: @env("USER_MESSAGE")
  })
TSK
```

### 3. Use Caching for Expensive Operations
```ruby
# Cache expensive string operations
expensive_format: @cache("10m", @format("Complex template with {data}", @complex_data_processing()))
```

### 4. Handle Edge Cases
```ruby
# TuskLang with edge case handling
tsk_content = <<~TSK
  [edge_cases]
  # Handle empty strings
  safe_concat: @concat(@env("FIRST_NAME") || "", " ", @env("LAST_NAME") || "")
  
  # Handle null values
  safe_format: @format("Hello, {name}!", @env("USER_NAME") || "Guest")
  
  # Handle encoding issues
  safe_encoding: @encode(@env("USER_INPUT"), "UTF-8")
TSK
```

## 🔧 Troubleshooting

### Common String Issues

```ruby
# Issue: Encoding problems
# Solution: Use proper encoding
tsk_content = <<~TSK
  [encoding_fixes]
  # Handle UTF-8 encoding
  safe_utf8: @encode(@env("USER_INPUT"), "UTF-8")
  
  # Handle special characters
  safe_special_chars: @escape_special(@env("USER_INPUT"))
TSK

# Issue: String concatenation with nil values
# Solution: Use safe concatenation
tsk_content = <<~TSK
  [safe_concatenation]
  # Safe concatenation
  full_name: @concat(@env("FIRST_NAME") || "", " ", @env("LAST_NAME") || "")
  
  # Safe formatting
  message: @format("Hello, {name}!", @env("USER_NAME") || "Guest")
TSK

# Issue: Template injection
# Solution: Use safe templates
tsk_content = <<~TSK
  [safe_templates]
  # Safe template processing
  safe_template: @template_safe(@env("TEMPLATE"), @env("DATA"))
  
  # Escaped output
  escaped_output: @escape_all(@env("USER_INPUT"))
TSK
```

## 🎯 Summary

TuskLang's string operations provide powerful text manipulation capabilities that integrate seamlessly with Ruby applications. By leveraging these operations, you can:

- **Manipulate text dynamically** in configuration files
- **Generate personalized content** with templates
- **Validate and sanitize user input** securely
- **Format data for different outputs** (JSON, HTML, etc.)
- **Handle complex string processing** efficiently

The Ruby integration makes these operations even more powerful by combining TuskLang's declarative syntax with Ruby's rich string processing libraries and object-oriented features.

**Remember**: TuskLang string operations are designed to be expressive, secure, and Ruby-friendly. Use them to create dynamic, safe, and maintainable text processing configurations.

**Key Takeaways**:
- Always validate and sanitize user input
- Use caching for expensive string operations
- Handle encoding issues properly
- Implement security measures for string processing
- Combine with Ruby's string libraries for advanced functionality 
# 🔍 TuskLang Bash @regex Function Guide

**"We don't bow to any king" – Regex is your configuration's pattern matcher.**

The @regex function in TuskLang is your pattern matching powerhouse, enabling dynamic regular expression operations, text processing, and data validation directly within your configuration files. Whether you're validating data, extracting information, or transforming text, @regex provides the precision and power to work with patterns seamlessly.

## 🎯 What is @regex?
The @regex function provides regular expression operations in TuskLang. It offers:
- **Pattern matching** - Match text against regular expressions
- **Text extraction** - Extract data using capture groups
- **Text replacement** - Replace text using regex patterns
- **Data validation** - Validate data formats and structures
- **Text transformation** - Transform text using regex operations

## 📝 Basic @regex Syntax

### Simple Pattern Matching
```ini
[simple_matching]
# Basic pattern matching
email_valid: @regex.match("user@example.com", "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
phone_valid: @regex.match("+1-555-123-4567", "^\+?[\d\s\-\(\)]+$")
url_valid: @regex.match("https://example.com", "^https?://[^\s/$.?#].[^\s]*$")

# Check if text contains pattern
contains_digit: @regex.test("Hello123World", "\d+")
contains_email: @regex.test("Contact us at user@example.com", "[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")
```

### Text Extraction
```ini
[text_extraction]
# Extract data using capture groups
$email_text: "Contact us at john.doe@example.com or jane.smith@company.org"
emails: @regex.extract($email_text, "([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})")

$phone_text: "Call us at +1-555-123-4567 or 555-987-6543"
phone_numbers: @regex.extract($phone_text, "(\+?[\d\s\-\(\)]+)")

# Extract specific groups
$url_text: "Visit https://example.com/path?param=value"
domain: @regex.extract($url_text, "https?://([^/\s]+)")
path: @regex.extract($url_text, "https?://[^/\s]+(/[^\s\?]*)")
```

### Text Replacement
```ini
[text_replacement]
# Replace text using regex
$sensitive_text: "API key: sk-1234567890abcdef, Secret: secret123"
masked_text: @regex.replace($sensitive_text, "(sk-[a-zA-Z0-9]+)", "***MASKED***")
masked_secrets: @regex.replace($sensitive_text, "(secret[a-zA-Z0-9]+)", "***SECRET***")

# Format phone numbers
$phone_text: "Call 5551234567 or 5559876543"
formatted_phones: @regex.replace($phone_text, "(\d{3})(\d{3})(\d{4})", "($1) $2-$3")

# Clean up text
$dirty_text: "  Hello   World  !  "
clean_text: @regex.replace($dirty_text, "\s+", " ")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > regex-quickstart.tsk << 'EOF'
[pattern_matching]
# Validate common patterns
email_pattern: "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
phone_pattern: "^\+?[\d\s\-\(\)]+$"
url_pattern: "^https?://[^\s/$.?#].[^\s]*$"

# Test validation
valid_email: @regex.match("user@example.com", $email_pattern)
valid_phone: @regex.match("+1-555-123-4567", $phone_pattern)
valid_url: @regex.match("https://example.com", $url_pattern)

[text_extraction]
# Extract information from text
$sample_text: "Contact John at john.doe@example.com or call +1-555-123-4567"
extracted_emails: @regex.extract($sample_text, "([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})")
extracted_phones: @regex.extract($sample_text, "(\+?[\d\s\-\(\)]+)")

[text_transformation]
# Transform text using regex
$input_text: "Hello   World  !  How   are   you?"
cleaned_text: @regex.replace($input_text, "\s+", " ")
uppercase_text: @regex.replace($cleaned_text, "([a-z])", @string.upper("$1"))

[data_validation]
# Validate data formats
$test_data: {
    "email": "test@example.com",
    "phone": "+1-555-123-4567",
    "url": "https://example.com"
}

validation_results: {
    "email_valid": @regex.match($test_data.email, $email_pattern),
    "phone_valid": @regex.match($test_data.phone, $phone_pattern),
    "url_valid": @regex.match($test_data.url, $url_pattern)
}
EOF

config=$(tusk_parse regex-quickstart.tsk)

echo "=== Pattern Matching ==="
echo "Valid Email: $(tusk_get "$config" pattern_matching.valid_email)"
echo "Valid Phone: $(tusk_get "$config" pattern_matching.valid_phone)"
echo "Valid URL: $(tusk_get "$config" pattern_matching.valid_url)"

echo ""
echo "=== Text Extraction ==="
echo "Extracted Emails: $(tusk_get "$config" text_extraction.extracted_emails)"
echo "Extracted Phones: $(tusk_get "$config" text_extraction.extracted_phones)"

echo ""
echo "=== Text Transformation ==="
echo "Cleaned Text: $(tusk_get "$config" text_transformation.cleaned_text)"
echo "Uppercase Text: $(tusk_get "$config" text_transformation.uppercase_text)"

echo ""
echo "=== Data Validation ==="
echo "Validation Results: $(tusk_get "$config" data_validation.validation_results)"
```

## 🔗 Real-World Use Cases

### 1. Data Validation and Sanitization
```ini
[data_validation]
# Comprehensive data validation
$validation_patterns: {
    "email": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
    "phone": "^\+?[\d\s\-\(\)]+$",
    "url": "^https?://[^\s/$.?#].[^\s]*$",
    "ipv4": "^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
    "credit_card": "^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|3[0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})$",
    "postal_code": "^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$",
    "date_iso": "^\d{4}-\d{2}-\d{2}$",
    "time_24h": "^([01]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$"
}

# Validate user input
$user_input: {
    "email": "user@example.com",
    "phone": "+1-555-123-4567",
    "url": "https://example.com",
    "ip": "192.168.1.1",
    "credit_card": "4111111111111111",
    "postal_code": "A1A 1A1",
    "date": "2024-01-15",
    "time": "14:30:00"
}

$validation_results: {
    "email_valid": @regex.match($user_input.email, $validation_patterns.email),
    "phone_valid": @regex.match($user_input.phone, $validation_patterns.phone),
    "url_valid": @regex.match($user_input.url, $validation_patterns.url),
    "ip_valid": @regex.match($user_input.ip, $validation_patterns.ipv4),
    "credit_card_valid": @regex.match($user_input.credit_card, $validation_patterns.credit_card),
    "postal_code_valid": @regex.match($user_input.postal_code, $validation_patterns.postal_code),
    "date_valid": @regex.match($user_input.date, $validation_patterns.date_iso),
    "time_valid": @regex.match($user_input.time, $validation_patterns.time_24h)
}

# Sanitize sensitive data
$sensitive_data: "API key: sk-1234567890abcdef, Secret: secret123, Token: tk_abcdef123456"
sanitized_data: @regex.replace($sensitive_data, "(sk-[a-zA-Z0-9]+|secret[a-zA-Z0-9]+|tk_[a-zA-Z0-9]+)", "***MASKED***")
```

### 2. Log Parsing and Analysis
```ini
[log_parsing]
# Parse log files using regex
$log_patterns: {
    "apache_log": "^(\S+) (\S+) (\S+) \[([\w:/]+\s[+\-]\d{4})\] \"(\S+) (\S+) (\S+)\" (\d{3}) (\d+)",
    "nginx_log": "^(\S+) - (\S+) \[([\w:/]+\s[+\-]\d{4})\] \"(\S+) (\S+) (\S+)\" (\d{3}) (\d+) \"([^\"]*)\" \"([^\"]*)\"",
    "syslog": "^(\w{3}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2}) (\S+) ([^:]+): (.+)$"
}

# Parse Apache access log
$apache_log_line: '192.168.1.100 - - [25/Dec/2024:10:30:45 +0000] "GET /api/users HTTP/1.1" 200 1234'
apache_parsed: @regex.extract($apache_log_line, $log_patterns.apache_log)

# Extract specific information
$log_data: {
    "ip_address": @regex.extract($apache_log_line, "^(\S+)")[0],
    "timestamp": @regex.extract($apache_log_line, "\[([\w:/]+\s[+\-]\d{4})\]")[0],
    "method": @regex.extract($apache_log_line, "\"(\S+)")[0],
    "path": @regex.extract($apache_log_line, "\"(\S+) (\S+)")[1],
    "status_code": @regex.extract($apache_log_line, "\" (\d{3})")[0],
    "response_size": @regex.extract($apache_log_line, "(\d{3}) (\d+)")[1]
}

# Analyze log patterns
$log_analysis: {
    "error_requests": @regex.extract($apache_log_line, "(\d{3})")[0] >= 400,
    "large_responses": @regex.extract($apache_log_line, "(\d{3}) (\d+)")[1] > 10000,
    "api_requests": @regex.test($apache_log_line, "/api/"),
    "static_requests": @regex.test($apache_log_line, "\.(css|js|png|jpg|gif)$")
}
```

### 3. Text Processing and Transformation
```ini
[text_processing]
# Process and transform text
$text_operations: {
    "normalize_whitespace": @regex.replace("  Hello   World  !  ", "\s+", " "),
    "remove_html_tags": @regex.replace("<p>Hello <b>World</b>!</p>", "<[^>]+>", ""),
    "extract_hashtags": @regex.extract("Hello #world #programming #tusklang", "#(\w+)"),
    "extract_mentions": @regex.extract("Hello @john @jane how are you?", "@(\w+)"),
    "format_phone": @regex.replace("5551234567", "(\d{3})(\d{3})(\d{4})", "($1) $2-$3"),
    "format_ssn": @regex.replace("123456789", "(\d{3})(\d{2})(\d{4})", "$1-$2-$3")
}

# Process markdown text
$markdown_text: "**Bold text** and *italic text* with `code` and [links](url)"
markdown_processing: {
    "bold_text": @regex.extract($markdown_text, "\*\*([^*]+)\*\*"),
    "italic_text": @regex.extract($markdown_text, "\*([^*]+)\*"),
    "code_text": @regex.extract($markdown_text, "`([^`]+)`"),
    "links": @regex.extract($markdown_text, "\[([^\]]+)\]\(([^)]+)\)")
}

# Clean and normalize text
$dirty_text: "  Hello   World  !  \n\n  How   are   you?  "
text_cleaning: {
    "normalized": @regex.replace($dirty_text, "\s+", " "),
    "trimmed": @regex.replace($dirty_text, "^\s+|\s+$", ""),
    "single_line": @regex.replace($dirty_text, "\n+", " "),
    "clean": @regex.replace(@regex.replace($dirty_text, "\s+", " "), "^\s+|\s+$", "")
}
```

### 4. Configuration Parsing
```ini
[config_parsing]
# Parse configuration files using regex
$config_patterns: {
    "ini_section": "^\[([^\]]+)\]$",
    "ini_key_value": "^([^=]+)=(.*)$",
    "env_variable": "^([A-Z_][A-Z0-9_]*)=(.*)$",
    "json_key_value": "\"([^\"]+)\":\s*\"([^\"]+)\"",
    "yaml_key_value": "^([^:]+):\s*(.+)$"
}

# Parse INI configuration
$ini_content: |
  [database]
  host=localhost
  port=3306
  name=tusklang
  
  [api]
  url=https://api.example.com
  timeout=30

ini_parsed: {
    "sections": @regex.extract($ini_content, $config_patterns.ini_section),
    "settings": @regex.extract($ini_content, $config_patterns.ini_key_value)
}

# Parse environment variables
$env_content: |
  DATABASE_HOST=localhost
  DATABASE_PORT=3306
  API_KEY=sk-1234567890abcdef
  DEBUG=true

env_parsed: {
    "variables": @regex.extract($env_content, $config_patterns.env_variable),
    "sensitive_data": @regex.extract($env_content, "([A-Z_]+)=(sk-[a-zA-Z0-9]+)")
}

# Extract configuration values
$config_extraction: {
    "db_host": @regex.extract($ini_content, "host=([^\n]+)")[0],
    "db_port": @regex.extract($ini_content, "port=([^\n]+)")[0],
    "api_url": @regex.extract($ini_content, "url=([^\n]+)")[0],
    "api_timeout": @regex.extract($ini_content, "timeout=([^\n]+)")[0]
}
```

## 🧠 Advanced @regex Patterns

### Complex Pattern Matching
```ini
[complex_patterns]
# Advanced regex patterns
$advanced_patterns: {
    "email_with_name": "^([^<]+)\s*<([^>]+)>$",
    "phone_with_ext": "^(\+?[\d\s\-\(\)]+)(?:\s*ext\.?\s*(\d+))?$",
    "credit_card_masked": "^(\d{4})[-\s]?(\d{4})[-\s]?(\d{4})[-\s]?(\d{4})$",
    "date_range": "^(\d{4}-\d{2}-\d{2})\s*to\s*(\d{4}-\d{2}-\d{2})$",
    "version_number": "^(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z0-9]+))?$",
    "ip_range": "^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s*-\s*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})$"
}

# Complex text processing
$complex_text: "Contact: John Doe <john.doe@example.com> ext. 123 or +1-555-123-4567"
complex_extraction: {
    "email_with_name": @regex.extract($complex_text, $advanced_patterns.email_with_name),
    "phone_with_ext": @regex.extract($complex_text, $advanced_patterns.phone_with_ext),
    "all_emails": @regex.extract($complex_text, "[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"),
    "all_phones": @regex.extract($complex_text, "\+?[\d\s\-\(\)]+")
}
```

### Conditional Pattern Matching
```ini
[conditional_patterns]
# Conditional regex matching
$conditional_validation: {
    "validate_format": @regex.test($input_text, $pattern),
    "extract_if_valid": @if(@regex.test($input_text, $pattern), 
        @regex.extract($input_text, $pattern), 
        []
    ),
    "transform_if_valid": @if(@regex.test($input_text, $pattern),
        @regex.replace($input_text, $pattern, $replacement),
        $input_text
    )
}

# Multi-step validation
$multi_step_validation: {
    "step1_format": @regex.test($email_input, "^[^@]+@[^@]+\.[^@]+$"),
    "step2_domain": @regex.test($email_input, "@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"),
    "step3_local": @regex.test($email_input, "^[a-zA-Z0-9._%+-]+@"),
    "all_valid": $multi_step_validation.step1_format && $multi_step_validation.step2_domain && $multi_step_validation.step3_local
}
```

### Performance Optimization
```ini
[performance_optimization]
# Optimize regex performance
$optimized_patterns: {
    "fast_email": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
    "fast_phone": "^\+?[\d\s\-\(\)]+$",
    "fast_url": "^https?://[^\s]+$",
    "fast_ip": "^(\d{1,3}\.){3}\d{1,3}$"
}

# Use compiled patterns for repeated operations
$compiled_patterns: {
    "email": @regex.compile("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"),
    "phone": @regex.compile("^\+?[\d\s\-\(\)]+$"),
    "url": @regex.compile("^https?://[^\s]+$")
}

# Batch processing with compiled patterns
$batch_processing: {
    "emails": @array.map($email_list, @regex.match(item, $compiled_patterns.email)),
    "phones": @array.map($phone_list, @regex.match(item, $compiled_patterns.phone)),
    "urls": @array.map($url_list, @regex.match(item, $compiled_patterns.url))
}
```

## 🛡️ Security & Performance Notes
- **Regex injection:** Validate and sanitize regex patterns to prevent injection attacks
- **Performance impact:** Use efficient patterns and avoid catastrophic backtracking
- **Pattern complexity:** Keep patterns simple and readable
- **Input validation:** Validate input before applying regex operations
- **Memory usage:** Monitor memory consumption for large text processing
- **Timeout protection:** Set appropriate timeouts for complex regex operations

## 🐞 Troubleshooting
- **Pattern not matching:** Check regex syntax and escape special characters
- **Performance issues:** Optimize patterns and avoid backtracking
- **Memory problems:** Use non-capturing groups and avoid excessive nesting
- **Encoding issues:** Ensure proper character encoding for Unicode patterns
- **Timeout errors:** Simplify complex patterns or increase timeout limits

## 💡 Best Practices
- **Use anchors:** Use ^ and $ for exact matching when appropriate
- **Escape special characters:** Escape regex metacharacters in literal text
- **Use non-capturing groups:** Use (?:...) for groups you don't need to capture
- **Test patterns:** Test regex patterns with various inputs
- **Document patterns:** Document complex regex patterns for maintainability
- **Use flags appropriately:** Use case-insensitive, multiline flags as needed

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@string Function](030-at-string-function-bash.md)
- [@validate Function](036-at-validate-function-bash.md)
- [Text Processing](106-text-processing-bash.md)
- [Data Validation](107-data-validation-bash.md)

---

**Master @regex in TuskLang and wield the power of pattern matching in your configurations. 🔍** 
# 🔧 Custom @ Operators in Rust

TuskLang allows you to create custom @ operators in Rust, extending the language with domain-specific functionality while maintaining type safety and performance.

## Basic Custom Operator Structure

```rust
// Define a custom operator trait
trait CustomOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>>;
    fn name(&self) -> &'static str;
    fn description(&self) -> &'static str;
}

// Implement a custom operator
struct UserCountOperator;

impl CustomOperator for UserCountOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.is_empty() {
            return Err("@user_count requires at least one argument".into());
        }
        
        let status = args[0].as_str().unwrap_or("active");
        let count = @query("SELECT COUNT(*) FROM users WHERE status = ?", vec![status])?;
        
        Ok(Value::Number(count as u64))
    }
    
    fn name(&self) -> &'static str {
        "user_count"
    }
    
    fn description(&self) -> &'static str {
        "Count users by status"
    }
}
```

## Registering Custom Operators

```rust
// Register custom operators
fn register_custom_operators(registry: &mut OperatorRegistry) {
    registry.register(Box::new(UserCountOperator));
    registry.register(Box::new(OrderTotalOperator));
    registry.register(Box::new(DataValidationOperator));
    registry.register(Box::new(CacheOperator));
    registry.register(Box::new(NotificationOperator));
}

// Usage in TuskLang
let active_users = @user_count("active");
let pending_users = @user_count("pending");
```

## Database Custom Operators

```rust
// Custom database operator
struct OrderTotalOperator;

impl CustomOperator for OrderTotalOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 1 {
            return Err("@order_total requires user_id".into());
        }
        
        let user_id = args[0].as_u64().unwrap_or(0);
        let total = @query(
            "SELECT SUM(total) FROM orders WHERE user_id = ? AND status = 'completed'",
            vec![user_id]
        )?;
        
        Ok(Value::Number(total as u64))
    }
    
    fn name(&self) -> &'static str {
        "order_total"
    }
    
    fn description(&self) -> &'static str {
        "Calculate total order value for a user"
    }
}

// Usage
let user_total = @order_total(@user.id);
let formatted_total = format!("${:.2}", user_total as f64 / 100.0);
```

## Validation Custom Operators

```rust
// Custom validation operator
struct DataValidationOperator;

impl CustomOperator for DataValidationOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 2 {
            return Err("@validate requires data and rules".into());
        }
        
        let data = &args[0];
        let rules = &args[1];
        
        let mut errors = Vec::new();
        
        // Validate email
        if let Some(email) = data.get("email").and_then(|e| e.as_str()) {
            if !@is_valid_email(email) {
                errors.push("Invalid email format".to_string());
            }
        }
        
        // Validate age
        if let Some(age) = data.get("age").and_then(|a| a.as_u64()) {
            if age < 18 {
                errors.push("User must be 18 or older".to_string());
            }
        }
        
        if errors.is_empty() {
            Ok(Value::Bool(true))
        } else {
            Ok(Value::Object(errors.into_iter().map(|e| (e, Value::Null)).collect()))
        }
    }
    
    fn name(&self) -> &'static str {
        "validate"
    }
    
    fn description(&self) -> &'static str {
        "Validate data against rules"
    }
}

// Usage
let validation_result = @validate(@user_data, @validation_rules);
if validation_result.is_object() {
    @log("Validation errors: {:?}", validation_result);
}
```

## Cache Custom Operators

```rust
// Custom cache operator
struct CacheOperator;

impl CustomOperator for CacheOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 2 {
            return Err("@cache requires key and value".into());
        }
        
        let key = args[0].as_str().unwrap_or("");
        let value = &args[1];
        let ttl = args.get(2).and_then(|t| t.as_u64()).unwrap_or(3600);
        
        @cache.put(key, value, Duration::from_secs(ttl))?;
        
        Ok(Value::Bool(true))
    }
    
    fn name(&self) -> &'static str {
        "cache"
    }
    
    fn description(&self) -> &'static str {
        "Cache a value with optional TTL"
    }
}

// Usage
@cache("user_profile", @user_profile, 7200);
let cached_profile = @cache.get("user_profile");
```

## Notification Custom Operators

```rust
// Custom notification operator
struct NotificationOperator;

impl CustomOperator for NotificationOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 3 {
            return Err("@notify requires user_id, message, and type".into());
        }
        
        let user_id = args[0].as_u64().unwrap_or(0);
        let message = args[1].as_str().unwrap_or("");
        let notification_type = args[2].as_str().unwrap_or("info");
        
        let notification = Notification {
            user_id,
            message: message.to_string(),
            notification_type: notification_type.to_string(),
            created_at: chrono::Utc::now(),
        };
        
        @save_notification(notification)?;
        @send_push_notification(user_id, message)?;
        
        Ok(Value::Bool(true))
    }
    
    fn name(&self) -> &'static str {
        "notify"
    }
    
    fn description(&self) -> &'static str {
        "Send notification to user"
    }
}

// Usage
@notify(@user.id, "Your order has been shipped!", "success");
```

## File System Custom Operators

```rust
// Custom file operator
struct FileOperator;

impl CustomOperator for FileOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 2 {
            return Err("@file requires action and path".into());
        }
        
        let action = args[0].as_str().unwrap_or("");
        let path = args[1].as_str().unwrap_or("");
        
        match action {
            "read" => {
                let content = std::fs::read_to_string(path)?;
                Ok(Value::String(content))
            }
            "write" => {
                if args.len() < 3 {
                    return Err("@file write requires content".into());
                }
                let content = args[2].as_str().unwrap_or("");
                std::fs::write(path, content)?;
                Ok(Value::Bool(true))
            }
            "exists" => {
                let exists = std::path::Path::new(path).exists();
                Ok(Value::Bool(exists))
            }
            _ => Err(format!("Unknown file action: {}", action).into())
        }
    }
    
    fn name(&self) -> &'static str {
        "file"
    }
    
    fn description(&self) -> &'static str {
        "File system operations"
    }
}

// Usage
let config_content = @file("read", "config.tusk");
@file("write", "log.txt", @log_content);
let file_exists = @file("exists", "data.json");
```

## HTTP Custom Operators

```rust
// Custom HTTP operator
struct HttpOperator;

impl CustomOperator for HttpOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 2 {
            return Err("@http requires method and url".into());
        }
        
        let method = args[0].as_str().unwrap_or("GET");
        let url = args[1].as_str().unwrap_or("");
        let body = args.get(2);
        let headers = args.get(3);
        
        let client = reqwest::Client::new();
        let mut request = match method {
            "GET" => client.get(url),
            "POST" => client.post(url),
            "PUT" => client.put(url),
            "DELETE" => client.delete(url),
            _ => return Err(format!("Unsupported HTTP method: {}", method).into())
        };
        
        if let Some(body_data) = body {
            request = request.json(body_data);
        }
        
        if let Some(header_data) = headers {
            if let Some(header_map) = header_data.as_object() {
                for (key, value) in header_map {
                    if let Some(value_str) = value.as_str() {
                        request = request.header(key, value_str);
                    }
                }
            }
        }
        
        let response = request.send().await?;
        let status = response.status().as_u16();
        let body = response.text().await?;
        
        Ok(serde_json::json!({
            "status": status,
            "body": body
        }))
    }
    
    fn name(&self) -> &'static str {
        "http"
    }
    
    fn description(&self) -> &'static str {
        "HTTP requests"
    }
}

// Usage
let api_response = @http("GET", "https://api.example.com/users");
let post_result = @http("POST", "https://api.example.com/users", @user_data, @headers);
```

## Encryption Custom Operators

```rust
// Custom encryption operator
struct EncryptionOperator;

impl CustomOperator for EncryptionOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 2 {
            return Err("@encrypt requires action and data".into());
        }
        
        let action = args[0].as_str().unwrap_or("");
        let data = args[1].as_str().unwrap_or("");
        let key = args.get(2).and_then(|k| k.as_str()).unwrap_or("default_key");
        
        match action {
            "encrypt" => {
                let encrypted = @encrypt_data(data, key)?;
                Ok(Value::String(encrypted))
            }
            "decrypt" => {
                let decrypted = @decrypt_data(data, key)?;
                Ok(Value::String(decrypted))
            }
            _ => Err(format!("Unknown encryption action: {}", action).into())
        }
    }
    
    fn name(&self) -> &'static str {
        "encrypt"
    }
    
    fn description(&self) -> &'static str {
        "Encryption and decryption operations"
    }
}

// Usage
let encrypted_password = @encrypt("encrypt", @user.password, @secret_key);
let decrypted_data = @encrypt("decrypt", @encrypted_data, @secret_key);
```

## Date/Time Custom Operators

```rust
// Custom date operator
struct DateOperator;

impl CustomOperator for DateOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 1 {
            return Err("@date requires action".into());
        }
        
        let action = args[0].as_str().unwrap_or("");
        
        match action {
            "now" => {
                let now = chrono::Utc::now();
                Ok(Value::String(now.to_rfc3339()))
            }
            "format" => {
                if args.len() < 3 {
                    return Err("@date format requires format string and date".into());
                }
                let format_str = args[1].as_str().unwrap_or("%Y-%m-%d");
                let date_str = args[2].as_str().unwrap_or("");
                
                let date = chrono::DateTime::parse_from_rfc3339(date_str)?;
                let formatted = date.format(format_str).to_string();
                Ok(Value::String(formatted))
            }
            "add_days" => {
                if args.len() < 3 {
                    return Err("@date add_days requires date and days".into());
                }
                let date_str = args[1].as_str().unwrap_or("");
                let days = args[2].as_i64().unwrap_or(0);
                
                let date = chrono::DateTime::parse_from_rfc3339(date_str)?;
                let new_date = date + chrono::Duration::days(days);
                Ok(Value::String(new_date.to_rfc3339()))
            }
            _ => Err(format!("Unknown date action: {}", action).into())
        }
    }
    
    fn name(&self) -> &'static str {
        "date"
    }
    
    fn description(&self) -> &'static str {
        "Date and time operations"
    }
}

// Usage
let current_time = @date("now");
let formatted_date = @date("format", "%Y-%m-%d", @user.created_at);
let future_date = @date("add_days", @current_date, 7);
```

## Math Custom Operators

```rust
// Custom math operator
struct MathOperator;

impl CustomOperator for MathOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 2 {
            return Err("@math requires operation and values".into());
        }
        
        let operation = args[0].as_str().unwrap_or("");
        let values: Vec<f64> = args[1..].iter()
            .filter_map(|v| v.as_f64())
            .collect();
        
        if values.is_empty() {
            return Err("No numeric values provided".into());
        }
        
        let result = match operation {
            "sum" => values.iter().sum(),
            "avg" => values.iter().sum::<f64>() / values.len() as f64,
            "min" => values.iter().fold(f64::INFINITY, |a, &b| a.min(b)),
            "max" => values.iter().fold(f64::NEG_INFINITY, |a, &b| a.max(b)),
            "round" => values[0].round(),
            "floor" => values[0].floor(),
            "ceil" => values[0].ceil(),
            _ => return Err(format!("Unknown math operation: {}", operation).into())
        };
        
        Ok(Value::Number(serde_json::Number::from_f64(result).unwrap()))
    }
    
    fn name(&self) -> &'static str {
        "math"
    }
    
    fn description(&self) -> &'static str {
        "Mathematical operations"
    }
}

// Usage
let total = @math("sum", @prices);
let average = @math("avg", @scores);
let rounded = @math("round", @price);
```

## String Custom Operators

```rust
// Custom string operator
struct StringOperator;

impl CustomOperator for StringOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.len() < 2 {
            return Err("@string requires operation and text".into());
        }
        
        let operation = args[0].as_str().unwrap_or("");
        let text = args[1].as_str().unwrap_or("");
        
        let result = match operation {
            "upper" => text.to_uppercase(),
            "lower" => text.to_lowercase(),
            "title" => text.split_whitespace()
                .map(|word| {
                    let mut chars = word.chars();
                    match chars.next() {
                        None => String::new(),
                        Some(first) => first.to_uppercase().chain(chars).collect(),
                    }
                })
                .collect::<Vec<_>>()
                .join(" "),
            "slug" => text
                .to_lowercase()
                .chars()
                .map(|c| if c.is_alphanumeric() { c } else { '-' })
                .collect::<String>()
                .trim_matches('-')
                .to_string(),
            "reverse" => text.chars().rev().collect(),
            "length" => return Ok(Value::Number(text.len() as u64)),
            _ => return Err(format!("Unknown string operation: {}", operation).into())
        };
        
        Ok(Value::String(result))
    }
    
    fn name(&self) -> &'static str {
        "string"
    }
    
    fn description(&self) -> &'static str {
        "String manipulation operations"
    }
}

// Usage
let upper_text = @string("upper", @user.name);
let slug = @string("slug", @post.title);
let text_length = @string("length", @content);
```

## Best Practices

### 1. Error Handling
```rust
// Always provide meaningful error messages
impl CustomOperator for MyOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        if args.is_empty() {
            return Err("@my_operator requires at least one argument".into());
        }
        
        // Validate argument types
        let user_id = args[0].as_u64()
            .ok_or("First argument must be a number")?;
        
        // Your logic here
        Ok(Value::Bool(true))
    }
}
```

### 2. Type Safety
```rust
// Use Rust's type system for safety
fn validate_args(args: &[Value], expected_types: &[&str]) -> Result<(), Box<dyn std::error::Error>> {
    if args.len() != expected_types.len() {
        return Err(format!("Expected {} arguments, got {}", expected_types.len(), args.len()).into());
    }
    
    for (arg, expected_type) in args.iter().zip(expected_types.iter()) {
        match *expected_type {
            "string" => { arg.as_str().ok_or("Expected string")?; }
            "number" => { arg.as_u64().ok_or("Expected number")?; }
            "bool" => { arg.as_bool().ok_or("Expected boolean")?; }
            _ => return Err(format!("Unknown type: {}", expected_type).into())
        }
    }
    
    Ok(())
}
```

### 3. Performance Considerations
```rust
// Use efficient data structures
impl CustomOperator for EfficientOperator {
    fn execute(&self, args: &[Value]) -> Result<Value, Box<dyn std::error::Error>> {
        // Use references to avoid cloning
        let data = args[0].as_str().unwrap_or("");
        
        // Use efficient algorithms
        let result = data
            .chars()
            .filter(|c| c.is_alphanumeric())
            .collect::<String>();
        
        Ok(Value::String(result))
    }
}
```

### 4. Documentation
```rust
// Provide comprehensive documentation
impl CustomOperator for DocumentedOperator {
    fn name(&self) -> &'static str {
        "documented"
    }
    
    fn description(&self) -> &'static str {
        "A well-documented custom operator that demonstrates best practices"
    }
    
    fn help(&self) -> &'static str {
        "Usage: @documented(arg1, arg2)\n\
         arg1: string - The input text\n\
         arg2: number - The processing limit\n\
         Returns: string - The processed result"
    }
}
```

### 5. Testing
```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_custom_operator() {
        let operator = MyCustomOperator;
        let args = vec![Value::String("test".to_string())];
        
        let result = operator.execute(&args);
        assert!(result.is_ok());
    }
}
```

Custom @ operators in Rust provide a powerful way to extend TuskLang with domain-specific functionality while maintaining Rust's safety and performance characteristics. 
# 🐘 @php Function in Rust

The `@php` function in TuskLang provides seamless integration between Rust and PHP, allowing you to execute PHP code from within Rust applications with full type safety and error handling.

## Basic Usage

```rust
// Basic PHP code execution
let php_result = @php("echo 'Hello from PHP!';")?;
let php_version = @php("echo PHP_VERSION;")?;
let php_info = @php("phpinfo();")?;

// Execute PHP with variables
let name = "Rust";
let php_greeting = @php(format!("echo 'Hello, {}!';", name))?;

// Return values from PHP
let php_sum = @php("return 2 + 3;")?;
let php_array = @php("return ['a', 'b', 'c'];")?;
```

## PHP Integration Architecture

```rust
// PHP integration manager
struct PhpIntegrationManager {
    php_context: PhpContext,
    variable_cache: std::collections::HashMap<String, Value>,
    function_registry: std::collections::HashMap<String, Box<dyn Fn(Vec<Value>) -> Result<Value, Box<dyn std::error::Error>>>>,
}

impl PhpIntegrationManager {
    fn new() -> Result<Self, Box<dyn std::error::Error>> {
        let php_context = PhpContext::new()?;
        
        Ok(Self {
            php_context,
            variable_cache: std::collections::HashMap::new(),
            function_registry: std::collections::HashMap::new(),
        })
    }
    
    // Execute PHP code with context
    fn execute_php(&mut self, code: &str) -> Result<Value, Box<dyn std::error::Error>> {
        // Prepare PHP context
        self.prepare_context()?;
        
        // Execute PHP code
        let result = self.php_context.execute(code)?;
        
        // Process result
        self.process_result(result)
    }
    
    // Execute PHP with variables
    fn execute_php_with_vars(&mut self, code: &str, variables: std::collections::HashMap<String, Value>) -> Result<Value, Box<dyn std::error::Error>> {
        // Set variables in PHP context
        for (name, value) in variables {
            self.php_context.set_variable(&name, value)?;
        }
        
        // Execute PHP code
        self.execute_php(code)
    }
    
    // Register Rust functions for PHP
    fn register_function(&mut self, name: &str, func: Box<dyn Fn(Vec<Value>) -> Result<Value, Box<dyn std::error::Error>>>) {
        self.function_registry.insert(name.to_string(), func);
    }
}

// PHP context wrapper
struct PhpContext {
    engine: php_engine::Engine,
    variables: std::collections::HashMap<String, Value>,
}

impl PhpContext {
    fn new() -> Result<Self, Box<dyn std::error::Error>> {
        let engine = php_engine::Engine::new()?;
        
        Ok(Self {
            engine,
            variables: std::collections::HashMap::new(),
        })
    }
    
    fn execute(&mut self, code: &str) -> Result<Value, Box<dyn std::error::Error>> {
        // Execute PHP code and capture output
        let result = self.engine.execute(code)?;
        Ok(result)
    }
    
    fn set_variable(&mut self, name: &str, value: Value) -> Result<(), Box<dyn std::error::Error>> {
        self.variables.insert(name.to_string(), value);
        Ok(())
    }
    
    fn get_variable(&self, name: &str) -> Option<&Value> {
        self.variables.get(name)
    }
}
```

## Data Type Conversion

```rust
// Data type conversion between Rust and PHP
struct PhpTypeConverter;

impl PhpTypeConverter {
    // Convert Rust types to PHP
    fn rust_to_php(value: &Value) -> Result<String, Box<dyn std::error::Error>> {
        match value {
            Value::String(s) => Ok(format!("'{}'", s.replace("'", "\\'"))),
            Value::Number(n) => Ok(n.to_string()),
            Value::Bool(b) => Ok(if *b { "true".to_string() } else { "false".to_string() }),
            Value::Array(arr) => {
                let elements: Vec<String> = arr.iter()
                    .map(|v| Self::rust_to_php(v))
                    .collect::<Result<Vec<_>, _>>()?;
                Ok(format!("[{}]", elements.join(", ")))
            }
            Value::Object(obj) => {
                let pairs: Vec<String> = obj.iter()
                    .map(|(k, v)| {
                        let key = format!("'{}'", k);
                        let value = Self::rust_to_php(v)?;
                        Ok(format!("{} => {}", key, value))
                    })
                    .collect::<Result<Vec<_>, _>>()?;
                Ok(format!("[{}]", pairs.join(", ")))
            }
            Value::Null => Ok("null".to_string()),
        }
    }
    
    // Convert PHP types to Rust
    fn php_to_rust(php_value: &str) -> Result<Value, Box<dyn std::error::Error>> {
        // Parse PHP output and convert to Rust Value
        if php_value == "null" {
            Ok(Value::Null)
        } else if php_value == "true" {
            Ok(Value::Bool(true))
        } else if php_value == "false" {
            Ok(Value::Bool(false))
        } else if let Ok(num) = php_value.parse::<f64>() {
            Ok(Value::Number(serde_json::Number::from_f64(num).unwrap()))
        } else if php_value.starts_with('"') && php_value.ends_with('"') {
            let s = &php_value[1..php_value.len()-1];
            Ok(Value::String(s.to_string()))
        } else if php_value.starts_with('[') && php_value.ends_with(']') {
            // Parse PHP array
            let content = &php_value[1..php_value.len()-1];
            let elements: Vec<Value> = content.split(',')
                .map(|s| Self::php_to_rust(s.trim()))
                .collect::<Result<Vec<_>, _>>()?;
            Ok(Value::Array(elements))
        } else {
            // Assume it's a string
            Ok(Value::String(php_value.to_string()))
        }
    }
}
```

## PHP Function Execution

```rust
// Execute PHP functions with parameters
fn execute_php_function(function_name: &str, params: Vec<Value>) -> Result<Value, Box<dyn std::error::Error>> {
    // Convert parameters to PHP format
    let php_params: Vec<String> = params.iter()
        .map(|p| PhpTypeConverter::rust_to_php(p))
        .collect::<Result<Vec<_>, _>>()?;
    
    // Build PHP function call
    let php_code = format!("{}({});", function_name, php_params.join(", "));
    
    // Execute PHP function
    @php(php_code)
}

// Usage examples
fn php_function_examples() -> Result<(), Box<dyn std::error::Error>> {
    // String functions
    let upper = execute_php_function("strtoupper", vec![Value::String("hello".to_string())])?;
    let length = execute_php_function("strlen", vec![Value::String("hello".to_string())])?;
    
    // Array functions
    let array = vec![Value::String("a".to_string()), Value::String("b".to_string())];
    let count = execute_php_function("count", vec![Value::Array(array)])?;
    
    // Math functions
    let sqrt = execute_php_function("sqrt", vec![Value::Number(16.0)])?;
    let round = execute_php_function("round", vec![Value::Number(3.14159)])?;
    
    Ok(())
}
```

## PHP Class Integration

```rust
// PHP class integration
struct PhpClassManager {
    class_registry: std::collections::HashMap<String, String>,
}

impl PhpClassManager {
    fn new() -> Self {
        Self {
            class_registry: std::collections::HashMap::new(),
        }
    }
    
    // Register PHP class
    fn register_class(&mut self, class_name: &str, php_code: &str) -> Result<(), Box<dyn std::error::Error>> {
        // Execute PHP class definition
        @php(php_code)?;
        self.class_registry.insert(class_name.to_string(), php_code.to_string());
        Ok(())
    }
    
    // Create PHP object
    fn create_object(&self, class_name: &str, constructor_params: Vec<Value>) -> Result<String, Box<dyn std::error::Error>> {
        let php_params: Vec<String> = constructor_params.iter()
            .map(|p| PhpTypeConverter::rust_to_php(p))
            .collect::<Result<Vec<_>, _>>()?;
        
        let php_code = format!("$obj = new {}({}); return $obj;", class_name, php_params.join(", "));
        @php(php_code)
    }
    
    // Call PHP object method
    fn call_method(&self, object_var: &str, method_name: &str, params: Vec<Value>) -> Result<Value, Box<dyn std::error::Error>> {
        let php_params: Vec<String> = params.iter()
            .map(|p| PhpTypeConverter::rust_to_php(p))
            .collect::<Result<Vec<_>, _>>()?;
        
        let php_code = format!("return ${}.{}({});", object_var, method_name, php_params.join(", "));
        @php(php_code)
    }
}

// Example PHP class usage
fn php_class_example() -> Result<(), Box<dyn std::error::Error>> {
    let mut class_manager = PhpClassManager::new();
    
    // Register a PHP class
    let php_class = r#"
        class Calculator {
            private $value;
            
            public function __construct($initial = 0) {
                $this->value = $initial;
            }
            
            public function add($number) {
                $this->value += $number;
                return $this->value;
            }
            
            public function getValue() {
                return $this->value;
            }
        }
    "#;
    
    class_manager.register_class("Calculator", php_class)?;
    
    // Create object
    let obj_var = class_manager.create_object("Calculator", vec![Value::Number(10.0)])?;
    
    // Call methods
    let result1 = class_manager.call_method(&obj_var, "add", vec![Value::Number(5.0)])?;
    let result2 = class_manager.call_method(&obj_var, "getValue", vec![])?;
    
    println!("Calculator result: {:?}", result1);
    println!("Calculator value: {:?}", result2);
    
    Ok(())
}
```

## PHP Template Engine

```rust
// PHP template engine integration
struct PhpTemplateEngine {
    template_cache: std::collections::HashMap<String, String>,
    variable_scope: std::collections::HashMap<String, Value>,
}

impl PhpTemplateEngine {
    fn new() -> Self {
        Self {
            template_cache: std::collections::HashMap::new(),
            variable_scope: std::collections::HashMap::new(),
        }
    }
    
    // Load PHP template
    fn load_template(&mut self, template_path: &str) -> Result<String, Box<dyn std::error::Error>> {
        if let Some(cached) = self.template_cache.get(template_path) {
            return Ok(cached.clone());
        }
        
        let template_content = std::fs::read_to_string(template_path)?;
        self.template_cache.insert(template_path.to_string(), template_content.clone());
        Ok(template_content)
    }
    
    // Render PHP template with variables
    fn render_template(&self, template_content: &str, variables: std::collections::HashMap<String, Value>) -> Result<String, Box<dyn std::error::Error>> {
        // Convert variables to PHP format
        let mut php_vars = String::new();
        for (name, value) in variables {
            let php_value = PhpTypeConverter::rust_to_php(&value)?;
            php_vars.push_str(&format!("${} = {};\n", name, php_value));
        }
        
        // Create PHP code with variables and template
        let php_code = format!("
            <?php
            {}
            ob_start();
            ?>
            {}
            <?php
            return ob_get_clean();
            ?>
        ", php_vars, template_content);
        
        @php(php_code)
    }
}

// Example template usage
fn php_template_example() -> Result<(), Box<dyn std::error::Error>> {
    let mut engine = PhpTemplateEngine::new();
    
    // PHP template content
    let template = r#"
        <html>
        <head><title><?php echo $title; ?></title></head>
        <body>
            <h1><?php echo $title; ?></h1>
            <p>Hello, <?php echo $name; ?>!</p>
            <ul>
                <?php foreach ($items as $item): ?>
                    <li><?php echo $item; ?></li>
                <?php endforeach; ?>
            </ul>
        </body>
        </html>
    "#;
    
    // Variables for template
    let mut variables = std::collections::HashMap::new();
    variables.insert("title".to_string(), Value::String("Welcome".to_string()));
    variables.insert("name".to_string(), Value::String("Rust".to_string()));
    variables.insert("items".to_string(), Value::Array(vec![
        Value::String("Item 1".to_string()),
        Value::String("Item 2".to_string()),
        Value::String("Item 3".to_string()),
    ]));
    
    // Render template
    let rendered = engine.render_template(template, variables)?;
    println!("Rendered template:\n{}", rendered);
    
    Ok(())
}
```

## PHP Session Management

```rust
// PHP session management
struct PhpSessionManager {
    session_id: Option<String>,
}

impl PhpSessionManager {
    fn new() -> Self {
        Self {
            session_id: None,
        }
    }
    
    // Start PHP session
    fn start_session(&mut self) -> Result<(), Box<dyn std::error::Error>> {
        @php("session_start();")?;
        self.session_id = Some(@php("return session_id();")?);
        Ok(())
    }
    
    // Set session variable
    fn set_session_var(&self, key: &str, value: &Value) -> Result<(), Box<dyn std::error::Error>> {
        let php_value = PhpTypeConverter::rust_to_php(value)?;
        let php_code = format!("$_SESSION['{}'] = {};", key, php_value);
        @php(php_code)
    }
    
    // Get session variable
    fn get_session_var(&self, key: &str) -> Result<Value, Box<dyn std::error::Error>> {
        let php_code = format!("return isset($_SESSION['{}']) ? $_SESSION['{}'] : null;", key, key);
        @php(php_code)
    }
    
    // Destroy session
    fn destroy_session(&mut self) -> Result<(), Box<dyn std::error::Error>> {
        @php("session_destroy();")?;
        self.session_id = None;
        Ok(())
    }
}

// Example session usage
fn php_session_example() -> Result<(), Box<dyn std::error::Error>> {
    let mut session_manager = PhpSessionManager::new();
    
    // Start session
    session_manager.start_session()?;
    
    // Set session variables
    session_manager.set_session_var("user_id", &Value::Number(123.0))?;
    session_manager.set_session_var("username", &Value::String("rust_user".to_string()))?;
    
    // Get session variables
    let user_id = session_manager.get_session_var("user_id")?;
    let username = session_manager.get_session_var("username")?;
    
    println!("User ID: {:?}", user_id);
    println!("Username: {:?}", username);
    
    // Destroy session
    session_manager.destroy_session()?;
    
    Ok(())
}
```

## Error Handling and Debugging

```rust
// PHP error handling and debugging
struct PhpErrorHandler {
    error_log: Vec<PhpError>,
    debug_mode: bool,
}

#[derive(Debug)]
struct PhpError {
    timestamp: chrono::DateTime<chrono::Utc>,
    error_type: String,
    message: String,
    file: Option<String>,
    line: Option<u32>,
}

impl PhpErrorHandler {
    fn new(debug_mode: bool) -> Self {
        Self {
            error_log: Vec::new(),
            debug_mode,
        }
    }
    
    // Execute PHP with error handling
    fn execute_with_error_handling(&mut self, php_code: &str) -> Result<Value, Box<dyn std::error::Error>> {
        // Set up error handling
        let error_handling_code = if self.debug_mode {
            r#"
                error_reporting(E_ALL);
                ini_set('display_errors', 1);
                set_error_handler(function($errno, $errstr, $errfile, $errline) {
                    throw new ErrorException($errstr, 0, $errno, $errfile, $errline);
                });
            "#
        } else {
            r#"
                error_reporting(0);
                ini_set('display_errors', 0);
            "#
        };
        
        // Execute PHP with error handling
        let full_code = format!("
            <?php
            {}
            try {{
                {}
            }} catch (Exception $e) {{
                return ['error' => $e->getMessage(), 'file' => $e->getFile(), 'line' => $e->getLine()];
            }}
            ?>
        ", error_handling_code, php_code);
        
        let result = @php(full_code)?;
        
        // Check for errors
        if let Value::Object(obj) = &result {
            if obj.contains_key("error") {
                let error = PhpError {
                    timestamp: chrono::Utc::now(),
                    error_type: "PHP Exception".to_string(),
                    message: obj.get("error").unwrap().as_str().unwrap_or("Unknown error").to_string(),
                    file: obj.get("file").and_then(|f| f.as_str()).map(|s| s.to_string()),
                    line: obj.get("line").and_then(|l| l.as_u64()).map(|l| l as u32),
                };
                
                self.error_log.push(error);
                return Err(format!("PHP error: {}", error.message).into());
            }
        }
        
        Ok(result)
    }
    
    // Get error log
    fn get_error_log(&self) -> &Vec<PhpError> {
        &self.error_log
    }
    
    // Clear error log
    fn clear_error_log(&mut self) {
        self.error_log.clear();
    }
}
```

## Best Practices

### 1. Always Handle PHP Errors
```rust
// Proper PHP error handling
fn safe_php_execution(php_code: &str) -> Result<Value, Box<dyn std::error::Error>> {
    let mut error_handler = PhpErrorHandler::new(true);
    
    match error_handler.execute_with_error_handling(php_code) {
        Ok(result) => Ok(result),
        Err(e) => {
            @log("PHP execution error: {}", e);
            Err(e)
        }
    }
}
```

### 2. Use Type-Safe Conversions
```rust
// Type-safe PHP integration
fn type_safe_php_call(function_name: &str, params: Vec<Value>) -> Result<Value, Box<dyn std::error::Error>> {
    // Validate parameters before sending to PHP
    for param in &params {
        match param {
            Value::String(s) => {
                if s.contains("'") || s.contains("\"") {
                    return Err("Unsafe string parameter".into());
                }
            }
            _ => {}
        }
    }
    
    execute_php_function(function_name, params)
}
```

### 3. Cache PHP Results
```rust
// Cache PHP execution results
fn cached_php_execution(php_code: &str, cache_key: &str) -> Result<Value, Box<dyn std::error::Error>> {
    // Check cache first
    if let Some(cached) = @cache.get(cache_key) {
        return Ok(cached);
    }
    
    // Execute PHP
    let result = @php(php_code)?;
    
    // Cache result
    @cache.put(cache_key, &result, std::time::Duration::from_secs(3600))?;
    
    Ok(result)
}
```

### 4. Validate PHP Output
```rust
// Validate PHP output
fn validate_php_output(result: &Value) -> Result<(), Box<dyn std::error::Error>> {
    match result {
        Value::String(s) => {
            // Check for potential security issues
            if s.contains("<script>") || s.contains("javascript:") {
                return Err("Potentially unsafe PHP output detected".into());
            }
        }
        _ => {}
    }
    
    Ok(())
}
```

### 5. Use PHP for Specific Tasks
```rust
// Use PHP for tasks it's well-suited for
fn php_specific_tasks() -> Result<(), Box<dyn std::error::Error>> {
    // String manipulation
    let formatted = @php("return number_format(1234567.89, 2, '.', ',');")?;
    
    // Date formatting
    let date = @php("return date('Y-m-d H:i:s');")?;
    
    // Array operations
    let sorted = @php("return sort(['c', 'a', 'b']);")?;
    
    println!("Formatted: {:?}", formatted);
    println!("Date: {:?}", date);
    println!("Sorted: {:?}", sorted);
    
    Ok(())
}
```

The `@php` function in Rust provides powerful integration capabilities while maintaining security and type safety. 
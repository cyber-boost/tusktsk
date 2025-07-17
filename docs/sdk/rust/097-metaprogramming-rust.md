# Metaprogramming in TuskLang for Rust

Metaprogramming in TuskLang for Rust enables you to write code that manipulates code, creating powerful abstractions and dynamic behavior while maintaining Rust's type safety and ownership guarantees.

## Reflection and Introspection

```rust
// Struct introspection
#[derive(Debug, Clone)]
struct User {
    pub name: String,
    pub email: String,
    password: String,
}

impl User {
    pub fn get_name(&self) -> &str { &self.name }
    pub fn set_name(&mut self, value: String) { self.name = value; }
    
    pub fn find_all() -> Result<Vec<Self>, Box<dyn std::error::Error>> {
        @db.table("users").get()
    }
}

// Get struct information
let user_struct = @reflect(User);

// Struct fields
let fields = user_struct.get_fields();
for field in fields {
    println!("Field: {:?}", {
        "name": field.name,
        "type": field.type_name,
        "visibility": field.visibility,
        "default_value": field.default_value
    });
}

// Struct methods
let methods = user_struct.get_methods();
for method in methods {
    println!("Method: {:?}", {
        "name": method.name,
        "parameters": method.parameters,
        "return_type": method.return_type,
        "is_static": method.is_static
    });
}

// Instance introspection
let user = User {
    name: "John".to_string(),
    email: "john@example.com".to_string(),
    password: "secret".to_string(),
};
let user_meta = @reflect(&user);

// Check if field exists
if user_meta.has_field("email") {
    let email_field = user_meta.get_field("email");
    let current_value = email_field.get_value();
    email_field.set_value("new@example.com".to_string());
}

// Call methods dynamically
let method = user_meta.get_method("get_name");
let result = method.invoke(&user, vec![]);

// Check type
if user_meta.is_instance_of::<User>() {
    println!("Is a User instance");
}
```

## Dynamic Method Creation

```rust
// Add methods at runtime
struct DynamicModel {
    attributes: std::collections::HashMap<String, Box<dyn std::any::Any + Send + Sync>>,
}

impl DynamicModel {
    fn new() -> Self {
        Self {
            attributes: std::collections::HashMap::new(),
        }
    }
    
    // Define getter/setter dynamically
    fn define_attribute<T: 'static + Send + Sync>(&mut self, name: &str, _type: std::marker::PhantomData<T>) {
        let name_upper = name.chars().next().unwrap().to_uppercase().collect::<String>() + &name[1..];
        
        // Create getter
        let getter_name = format!("get_{}", name_upper);
        self.define_method(&getter_name, move |this: &Self, _args: Vec<Box<dyn std::any::Any + Send + Sync>>| {
            if let Some(value) = this.attributes.get(name) {
                Ok(value.clone())
            } else {
                Err("Attribute not found".into())
            }
        });
        
        // Create setter with validation
        let setter_name = format!("set_{}", name_upper);
        let name_clone = name.to_string();
        self.define_method(&setter_name, move |this: &mut Self, args: Vec<Box<dyn std::any::Any + Send + Sync>>| {
            if let Some(value) = args.into_iter().next() {
                this.attributes.insert(name_clone.clone(), value);
                Ok(Box::new(()))
            } else {
                Err("No value provided".into())
            }
        });
    }
    
    fn define_method<F>(&mut self, name: &str, handler: F)
    where
        F: Fn(&Self, Vec<Box<dyn std::any::Any + Send + Sync>>) -> Result<Box<dyn std::any::Any + Send + Sync>, Box<dyn std::error::Error>> + 'static + Send + Sync,
    {
        // Store method in method registry
        @method_registry.insert(name.to_string(), Box::new(handler));
    }
    
    // Method missing handler
    fn call(&self, method: &str, args: Vec<Box<dyn std::any::Any + Send + Sync>>) -> Result<Box<dyn std::any::Any + Send + Sync>, Box<dyn std::error::Error>> {
        // Handle dynamic finders
        if method.starts_with("find_by_") {
            let field = method.strip_prefix("find_by_").unwrap();
            return @db.table(self.table_name)
                .where(field, args[0].downcast_ref::<String>().unwrap())
                .first();
        }
        
        // Handle dynamic scopes
        if method.starts_with("scope_") {
            let scope_name = method.strip_prefix("scope_").unwrap();
            if let Some(scope) = self.scopes.get(scope_name) {
                return scope(self, args);
            }
        }
        
        // Try registered methods
        if let Some(handler) = @method_registry.get(method) {
            return handler(self, args);
        }
        
        Err(format!("Method {} not found", method).into())
    }
}

// Usage
let mut model = DynamicModel::new();
model.define_attribute::<String>("title", std::marker::PhantomData);
model.define_attribute::<f64>("price", std::marker::PhantomData);

model.call("set_title", vec![Box::new("Product Name".to_string())])?;
model.call("set_price", vec![Box::new(29.99)])?;

// Dynamic finder
let product = model.call("find_by_title", vec![Box::new("Product Name".to_string())])?;
```

## Code Generation at Runtime

```rust
// Generate structs dynamically
fn create_model_struct(name: &str, fields: &[(&str, &str)]) -> Result<Box<dyn std::any::Any>, Box<dyn std::error::Error>> {
    // Build struct code
    let mut code = format!("pub struct {} {{\n", name);
    
    // Add fields
    for (field_name, field_type) in fields {
        code.push_str(&format!("    pub {}: {},\n", field_name, field_type));
    }
    
    // Add implementation
    code.push_str("}\n\n");
    code.push_str(&format!("impl {} {{\n", name));
    
    // Add constructor
    let mut constructor_params = Vec::new();
    let mut constructor_body = Vec::new();
    
    for (field_name, field_type) in fields {
        constructor_params.push(format!("{}: {}", field_name, field_type));
        constructor_body.push(format!("            {}: {},", field_name, field_name));
    }
    
    code.push_str(&format!("    pub fn new({}) -> Self {{\n", constructor_params.join(", ")));
    code.push_str(&format!("        Self {{\n"));
    code.push_str(&format!("{}\n", constructor_body.join("\n")));
    code.push_str("        }\n");
    code.push_str("    }\n");
    
    // Add validation rules
    code.push_str("    \n    pub fn rules() -> std::collections::HashMap<String, String> {\n");
    code.push_str("        let mut rules = std::collections::HashMap::new();\n");
    for (field_name, _) in fields {
        code.push_str(&format!("        rules.insert(\"{}\".to_string(), \"required\".to_string());\n", field_name));
    }
    code.push_str("        rules\n");
    code.push_str("    }\n");
    
    // Add relationships
    code.push_str("    \n    pub fn relationships() -> std::collections::HashMap<String, String> {\n");
    code.push_str("        let mut relationships = std::collections::HashMap::new();\n");
    for (field_name, field_type) in fields {
        if field_type.contains("_id") {
            let related_model = field_name.strip_suffix("_id").unwrap();
            let related_model_upper = related_model.chars().next().unwrap().to_uppercase().collect::<String>() + &related_model[1..];
            code.push_str(&format!("        relationships.insert(\"{}\".to_string(), \"belongs_to:{}\".to_string());\n", field_name, related_model_upper));
        }
    }
    code.push_str("        relationships\n");
    code.push_str("    }\n");
    
    code.push_str("}\n");
    
    // Evaluate the code
    @eval_rust_code(&code)
}

// Create a model dynamically
let ProductModel = create_model_struct("Product", &[
    ("name", "String"),
    ("price", "f64"),
    ("category_id", "u32"),
    ("tags", "Vec<String>")
])?;

// Use the generated struct
let product = ProductModel::new(
    "Dynamic Product".to_string(),
    29.99,
    1,
    vec!["electronics".to_string(), "gadgets".to_string()]
);
```

## Attribute Handlers

```rust
// Custom attribute system
struct AttributeHandler {
    handlers: std::collections::HashMap<String, Box<dyn Fn(&mut dyn std::any::Any, &str) -> Result<(), Box<dyn std::error::Error>> + Send + Sync>>,
}

impl AttributeHandler {
    fn new() -> Self {
        Self {
            handlers: std::collections::HashMap::new(),
        }
    }
    
    fn define<F>(&mut self, name: &str, handler: F)
    where
        F: Fn(&mut dyn std::any::Any, &str) -> Result<(), Box<dyn std::error::Error>> + 'static + Send + Sync,
    {
        self.handlers.insert(name.to_string(), Box::new(handler));
    }
    
    fn apply(&self, target: &mut dyn std::any::Any, attributes: &[(&str, &str)]) -> Result<(), Box<dyn std::error::Error>> {
        for (attr_name, attr_params) in attributes {
            if let Some(handler) = self.handlers.get(*attr_name) {
                handler(target, attr_params)?;
            }
        }
        Ok(())
    }
}

// Define custom attributes
let mut handler = AttributeHandler::new();

handler.define("Logged", |target, params| {
    // Parse params to get method name
    let method_name = params.trim();
    
    // Get the target as a mutable reference
    if let Some(target) = target.downcast_mut::<dyn std::any::Any>() {
        // This would require more sophisticated reflection
        // For now, we'll just log that we're applying the attribute
        println!("Applying Logged attribute to method: {}", method_name);
    }
    
    Ok(())
});

handler.define("Cached", |target, params| {
    let ttl = params.parse::<u64>().unwrap_or(3600);
    println!("Applying Cached attribute with TTL: {}", ttl);
    Ok(())
});

handler.define("Validated", |target, rules| {
    println!("Applying Validated attribute with rules: {}", rules);
    Ok(())
});

// Usage
struct UserService {
    db: Database,
    cache: Cache,
}

impl UserService {
    #[@Logged("get_user")]
    #[@Cached("3600")]
    #[@Validated("required|integer")]
    pub fn get_user(&self, id: u32) -> Result<User, Box<dyn std::error::Error>> {
        self.cache.remember(&format!("user.{}", id), 3600, || {
            self.db.table("users").find(id)
        })
    }
}

// Apply attributes
let mut service = UserService::new();
handler.apply(&mut service, &[
    ("Logged", "get_user"),
    ("Cached", "3600"),
    ("Validated", "required|integer")
])?;
```

## Dynamic Trait Implementation

```rust
// Dynamic trait implementation
trait Serializable {
    fn serialize(&self) -> Result<String, Box<dyn std::error::Error>>;
    fn deserialize(data: &str) -> Result<Self, Box<dyn std::error::Error>> where Self: Sized;
}

// Dynamic trait implementation macro
@macro implement_serializable(struct_name: &str, fields: &str) {
    let fields_map = @json_decode(fields);
    let mut serialize_code = String::new();
    let mut deserialize_code = String::new();
    
    serialize_code.push_str("let mut map = serde_json::Map::new();\n");
    for (field_name, _) in fields_map {
        serialize_code.push_str(&format!("map.insert(\"{}\".to_string(), serde_json::to_value(&self.{})?);\n", field_name, field_name));
    }
    serialize_code.push_str("Ok(serde_json::to_string(&map)?)");
    
    deserialize_code.push_str("let map: serde_json::Map<String, serde_json::Value> = serde_json::from_str(data)?;\n");
    let mut constructor_args = Vec::new();
    for (field_name, field_type) in fields_map {
        deserialize_code.push_str(&format!("let {}: {} = serde_json::from_value(map.get(\"{}\").unwrap().clone())?;\n", field_name, field_type, field_name));
        constructor_args.push(field_name);
    }
    deserialize_code.push_str(&format!("Ok(Self::new({}))", constructor_args.join(", ")));
    
    format!(r#"
        impl Serializable for {} {{
            fn serialize(&self) -> Result<String, Box<dyn std::error::Error>> {{
                {}
            }}
            
            fn deserialize(data: &str) -> Result<Self, Box<dyn std::error::Error>> {{
                {}
            }}
        }}
    "#, struct_name, serialize_code, deserialize_code)
}

// Usage
struct Product {
    name: String,
    price: f64,
    category_id: u32,
}

@implement_serializable("Product", r#"{
    "name": "String",
    "price": "f64",
    "category_id": "u32"
}"#)

// Now Product implements Serializable
let product = Product::new("Laptop".to_string(), 999.99, 1);
let serialized = product.serialize()?;
let deserialized = Product::deserialize(&serialized)?;
```

## Runtime Code Evaluation

```rust
// Safe code evaluation with sandboxing
struct CodeEvaluator {
    sandbox: Sandbox,
    allowed_modules: std::collections::HashSet<String>,
}

impl CodeEvaluator {
    fn new() -> Self {
        let mut allowed = std::collections::HashSet::new();
        allowed.insert("std::collections".to_string());
        allowed.insert("serde_json".to_string());
        allowed.insert("chrono".to_string());
        
        Self {
            sandbox: Sandbox::new(),
            allowed_modules: allowed,
        }
    }
    
    fn evaluate<T>(&self, code: &str) -> Result<T, Box<dyn std::error::Error>>
    where
        T: 'static + Send + Sync,
    {
        // Validate code safety
        self.validate_code(code)?;
        
        // Execute in sandbox
        let result = self.sandbox.execute(code)?;
        
        // Convert result to expected type
        Ok(result.downcast::<T>().map_err(|_| "Type conversion failed")?)
    }
    
    fn validate_code(&self, code: &str) -> Result<(), Box<dyn std::error::Error>> {
        // Check for unsafe operations
        let unsafe_patterns = [
            "unsafe",
            "std::ptr",
            "std::mem::transmute",
            "std::mem::forget",
        ];
        
        for pattern in &unsafe_patterns {
            if code.contains(pattern) {
                return Err(format!("Unsafe pattern '{}' not allowed", pattern).into());
            }
        }
        
        // Check module imports
        let import_pattern = r"use\s+([a-zA-Z_][a-zA-Z0-9_:]*)\s*;";
        for cap in regex::Regex::new(import_pattern).unwrap().captures_iter(code) {
            let module = &cap[1];
            if !self.allowed_modules.contains(module) {
                return Err(format!("Module '{}' not allowed", module).into());
            }
        }
        
        Ok(())
    }
}

// Usage
let evaluator = CodeEvaluator::new();

// Safe code evaluation
let result: Vec<i32> = evaluator.evaluate(r#"
    let mut numbers = vec![1, 2, 3, 4, 5];
    numbers.retain(|&x| x % 2 == 0);
    numbers
"#)?;

println!("Even numbers: {:?}", result);
```

## Dynamic Configuration

```rust
// Dynamic configuration system
struct DynamicConfig {
    values: std::collections::HashMap<String, Box<dyn std::any::Any + Send + Sync>>,
    validators: std::collections::HashMap<String, Box<dyn Fn(&Box<dyn std::any::Any + Send + Sync>) -> Result<(), Box<dyn std::error::Error>> + Send + Sync>>,
}

impl DynamicConfig {
    fn new() -> Self {
        Self {
            values: std::collections::HashMap::new(),
            validators: std::collections::HashMap::new(),
        }
    }
    
    fn set<T: 'static + Send + Sync>(&mut self, key: &str, value: T) -> Result<(), Box<dyn std::error::Error>> {
        let boxed_value = Box::new(value);
        
        // Run validator if exists
        if let Some(validator) = self.validators.get(key) {
            validator(&boxed_value)?;
        }
        
        self.values.insert(key.to_string(), boxed_value);
        Ok(())
    }
    
    fn get<T: 'static + Send + Sync>(&self, key: &str) -> Option<&T> {
        self.values.get(key)?.downcast_ref::<T>()
    }
    
    fn define_validator<F>(&mut self, key: &str, validator: F)
    where
        F: Fn(&Box<dyn std::any::Any + Send + Sync>) -> Result<(), Box<dyn std::error::Error>> + 'static + Send + Sync,
    {
        self.validators.insert(key.to_string(), Box::new(validator));
    }
    
    fn from_tusk_config(config: &str) -> Result<Self, Box<dyn std::error::Error>> {
        let mut dynamic_config = Self::new();
        let config_map = @json_decode(config);
        
        for (key, value) in config_map {
            match value.as_str() {
                Some(s) => dynamic_config.set(key, s.to_string())?,
                None => {
                    if let Some(i) = value.as_u64() {
                        dynamic_config.set(key, i as u32)?;
                    } else if let Some(f) = value.as_f64() {
                        dynamic_config.set(key, f)?;
                    } else if let Some(b) = value.as_bool() {
                        dynamic_config.set(key, b)?;
                    } else {
                        dynamic_config.set(key, value.to_string())?;
                    }
                }
            }
        }
        
        Ok(dynamic_config)
    }
}

// Usage with TuskLang configuration
let config = DynamicConfig::from_tusk_config(r#"{
    "database_url": "postgresql://localhost/mydb",
    "max_connections": 100,
    "debug_mode": true,
    "api_timeout": 30.5
}"#)?;

// Define validators
config.define_validator("max_connections", |value| {
    if let Some(connections) = value.downcast_ref::<u32>() {
        if *connections > 1000 {
            return Err("Too many connections".into());
        }
    }
    Ok(())
});

// Use configuration
let db_url: &String = config.get("database_url").unwrap();
let max_conn: &u32 = config.get("max_connections").unwrap();
let debug: &bool = config.get("debug_mode").unwrap();
```

## Metaprogramming Best Practices

### 1. Type Safety First

```rust
// Prefer compile-time metaprogramming over runtime
@macro type_safe_builder(struct_name: &str, fields: &str) {
    // Generate type-safe builder at compile time
    let fields_map = @json_decode(fields);
    let mut builder_methods = String::new();
    
    for (name, type_name) in fields_map {
        builder_methods.push_str(&format!(r#"
            pub fn {}(mut self, {}: {}) -> Self {{
                self.{} = Some({});
                self
            }}
        "#, name, name, type_name, name, name));
    }
    
    format!(r#"
        pub struct {}Builder {{
            {}
        }}
        
        impl {}Builder {{
            {}
        }}
    "#, struct_name, 
        fields_map.iter().map(|(name, type_name)| format!("{}: Option<{}>", name, type_name)).collect::<Vec<_>>().join(",\n            "),
        struct_name, builder_methods)
}
```

### 2. Error Handling

```rust
// Comprehensive error handling in metaprogramming
@macro safe_metaprogramming(code: &str, fallback: &str) {
    format!(r#"
        match std::panic::catch_unwind(|| {{
            {}
        }}) {{
            Ok(result) => result,
            Err(_) => {{
                @log.warn("Metaprogramming failed, using fallback");
                {}
            }}
        }}
    "#, code, fallback)
}
```

### 3. Performance Considerations

```rust
// Lazy evaluation for expensive metaprogramming
use std::sync::Once;

static INIT: Once = Once::new();
static mut METAPROGRAM_CACHE: Option<std::collections::HashMap<String, Box<dyn std::any::Any + Send + Sync>>> = None;

fn get_metaprogram_result(key: &str, generator: fn() -> Box<dyn std::any::Any + Send + Sync>) -> &'static Box<dyn std::any::Any + Send + Sync> {
    unsafe {
        INIT.call_once(|| {
            METAPROGRAM_CACHE = Some(std::collections::HashMap::new());
        });
        
        let cache = METAPROGRAM_CACHE.as_mut().unwrap();
        if !cache.contains_key(key) {
            cache.insert(key.to_string(), generator());
        }
        
        cache.get(key).unwrap()
    }
}
```

### 4. Testing Metaprogramming Code

```rust
// Test framework for metaprogramming
#[cfg(test)]
mod metaprogramming_tests {
    use super::*;
    
    #[test]
    fn test_dynamic_config() {
        let mut config = DynamicConfig::new();
        config.set("test_key", "test_value").unwrap();
        
        assert_eq!(config.get::<String>("test_key"), Some(&"test_value".to_string()));
    }
    
    #[test]
    fn test_code_evaluator() {
        let evaluator = CodeEvaluator::new();
        let result: i32 = evaluator.evaluate("2 + 2").unwrap();
        
        assert_eq!(result, 4);
    }
    
    #[test]
    fn test_unsafe_code_rejection() {
        let evaluator = CodeEvaluator::new();
        let result: Result<i32, _> = evaluator.evaluate("unsafe { std::ptr::null_mut() }");
        
        assert!(result.is_err());
    }
}
```

This comprehensive guide covers Rust-specific metaprogramming patterns, ensuring type safety, performance, and integration with Rust's ecosystem while maintaining the power and flexibility of TuskLang's metaprogramming capabilities. 
# Macros in TuskLang for Rust

Macros in TuskLang provide powerful metaprogramming capabilities for Rust applications, allowing you to generate code at compile time and create domain-specific languages (DSLs) that integrate seamlessly with Rust's type system and ownership model.

## Basic Macro Definition

```rust
// Simple macro
@macro hello(name: &str) {
    format!("println!(\"Hello, {}!\")", name)
}

// Usage
@hello("World")  // Expands to: println!("Hello, World!")

// Macro with multiple parameters
@macro debug(variable: &str, label: Option<&str>) {
    match label {
        Some(l) => format!("println!(\"{}\": {:?})", l, variable),
        None => format!("println!(\"{}\": {:?})", variable, variable)
    }
}

// Usage
@debug(user.name)  // println!("user.name": {:?})
@debug(total, Some("Total Amount"))  // println!("Total Amount": {:?})

// Block macros
@macro benchmark(code: &str) {
    format!(r#"
        let _benchmark_start = std::time::Instant::now();
        {code}
        let _benchmark_end = std::time::Instant::now();
        println!("Execution time: {:?}", _benchmark_end - _benchmark_start);
    "#)
}

// Usage
@benchmark {
    let result = expensive_operation();
    process_result(result);
}
```

## Code Generation Macros

```rust
// Generate getter/setter methods
@macro property(name: &str, type_name: &str) {
    format!(r#"
        pub fn get_{name}(&self) -> {type_name} {{
            self.{name}.clone()
        }}
        
        pub fn set_{name}(&mut self, value: {type_name}) {{
            self.{name} = value;
        }}
    "#)
}

// Usage in struct
struct User {
    @property("name", "String")
    @property("email", "String")
    @property("age", "u32")
}

// Generates:
// pub fn get_name(&self) -> String { self.name.clone() }
// pub fn set_name(&mut self, value: String) { self.name = value; }
// ... and so on

// Generate CRUD operations
@macro crud(model: &str, table: &str) {
    format!(r#"
        impl {model} {{
            pub fn all() -> Result<Vec<Self>, Box<dyn std::error::Error>> {{
                @db.table("{table}").get()
            }}
            
            pub fn find(id: u32) -> Result<Option<Self>, Box<dyn std::error::Error>> {{
                @db.table("{table}").where("id", id).first()
            }}
            
            pub fn save(&mut self) -> Result<u32, Box<dyn std::error::Error>> {{
                if let Some(id) = self.id {{
                    @db.table("{table}")
                        .where("id", id)
                        .update(self)
                }} else {{
                    self.id = Some(@db.table("{table}").insert_get_id(self)?);
                    Ok(self.id.unwrap())
                }}
            }}
            
            pub fn delete(&self) -> Result<bool, Box<dyn std::error::Error>> {{
                if let Some(id) = self.id {{
                    @db.table("{table}")
                        .where("id", id)
                        .delete()
                }} else {{
                    Ok(false)
                }}
            }}
        }}
    "#)
}

// Usage
struct Product {
    @crud("Product", "products")
}
```

## AST Manipulation Macros

```rust
// Macro that manipulates Abstract Syntax Tree
@macro memoize(method: &str) {
    format!(r#"
        use std::collections::HashMap;
        use std::sync::Mutex;
        use once_cell::sync::Lazy;
        
        static CACHE: Lazy<Mutex<HashMap<String, Box<dyn std::any::Any + Send + Sync>>>> = 
            Lazy::new(|| Mutex::new(HashMap::new()));
        
        pub fn {method}_memoized(&self, args: Vec<Box<dyn std::any::Any + Send + Sync>>) -> 
            Result<Box<dyn std::any::Any + Send + Sync>, Box<dyn std::error::Error>> {{
            
            let cache_key = format!("{method}:{{:?}}", args);
            
            if let Ok(cache) = CACHE.lock() {{
                if let Some(cached_result) = cache.get(&cache_key) {{
                    return Ok(cached_result.clone());
                }}
            }}
            
            let result = self.{method}(args)?;
            
            if let Ok(mut cache) = CACHE.lock() {{
                cache.insert(cache_key, result.clone());
            }}
            
            Ok(result)
        }}
    "#)
}

// Usage
struct Calculator {
    @memoize("fibonacci")
}

// Macro for automatic dependency injection
@macro inject(dependencies: &[(&str, &str)]) {
    let mut injections = String::new();
    
    for (name, type_name) in dependencies {
        injections.push_str(&format!("self.{} = @container.make::<{}>();\n", name, type_name));
    }
    
    format!(r#"
        pub fn new() -> Self {{
            let mut instance = Self::default();
            {injections}
            instance
        }}
    "#)
}

// Usage
struct UserService {
    @inject(&[("db", "Database"), ("cache", "Cache"), ("logger", "Logger")])
    
    pub fn get_user(&self, id: u32) -> Result<User, Box<dyn std::error::Error>> {
        self.cache.remember(&format!("user.{}", id), 3600, || {
            self.logger.info(&format!("Fetching user {}", id));
            self.db.table("users").find(id)
        })
    }
}
```

## Conditional Compilation Macros

```rust
// Environment-based code generation
@macro env_specific(code_map: &[(&str, &str)]) {
    let env = @compile_env("APP_ENV", "production");
    
    let mut code = String::new();
    for (env_name, env_code) in code_map {
        if *env_name == env {
            code = env_code.to_string();
            break;
        }
    }
    
    if code.is_empty() {
        code = code_map.iter()
            .find(|(name, _)| *name == "default")
            .map(|(_, code)| code.to_string())
            .unwrap_or_default();
    }
    
    code
}

// Usage
struct Logger {
    @env_specific(&[
        ("development", r#"
            pub fn log(&self, message: &str, context: Option<serde_json::Value>) {
                println!("[DEV] {} {:?}", message, context);
                if let Some(ctx) = context {
                    @file.append("debug.log", &serde_json::to_string(&ctx).unwrap());
                }
            }
        "#),
        
        ("production", r#"
            pub fn log(&self, message: &str, context: Option<serde_json::Value>) {
                @sentry.capture_message(message, context);
            }
        "#),
        
        ("testing", r#"
            pub fn log(&self, message: &str, context: Option<serde_json::Value>) {
                TEST_LOGS.lock().unwrap().push((message.to_string(), context));
            }
        "#)
    ])
}

// Feature flag macros
@macro feature(flag: &str, enabled_code: &str, disabled_code: &str) {
    if @compile_config(&format!("features.{}", flag), false) {
        enabled_code.to_string()
    } else {
        disabled_code.to_string()
    }
}

// Usage
struct PaymentProcessor {
    @feature("stripe_payment", r#"
        pub fn process_stripe_payment(&self, amount: u64, token: &str) -> Result<(), Box<dyn std::error::Error>> {
            @stripe.charges.create(amount, token, "usd")
        }
    "#, r#"
        pub fn process_stripe_payment(&self, _amount: u64, _token: &str) -> Result<(), Box<dyn std::error::Error>> {
            Err("Stripe payments not enabled".into())
        }
    "#)
    
    @feature("crypto_payment", r#"
        pub fn process_crypto_payment(&self, amount: u64, wallet: &str) -> Result<(), Box<dyn std::error::Error>> {
            @crypto.transfer(wallet, amount, "BTC")
        }
    "#, r#"
        pub fn process_crypto_payment(&self, _amount: u64, _wallet: &str) -> Result<(), Box<dyn std::error::Error>> {
            Err("Crypto payments not enabled".into())
        }
    "#)
}
```

## DSL Creation with Macros

```rust
// Create a routing DSL
@macro route(path: &str, options: &str) {
    let options_map = @json_decode(options);
    let method = options_map.get("method").unwrap_or(&"GET".to_string());
    let middleware = options_map.get("middleware").unwrap_or(&Vec::new());
    let name = options_map.get("name").unwrap_or(&path.replace("/", ".").trim_matches('.'));
    
    format!(r#"
        @router.{}(r"{}", {{
            middleware: {:?},
            name: "{}",
            handler: |request, response| {{
                {}
            }}
        }})
    "#, method.to_lowercase(), path, middleware, name, options_map.get("handler").unwrap_or(&"".to_string()))
}

// Usage
@route("/users", r#"{
    "method": "GET",
    "middleware": ["auth", "throttle:60,1"],
    "name": "users.index",
    "handler": "let users = @User.paginate(20); return @view('users.index', users);"
}"#)

// Create a validation DSL
@macro validate(rules: &str) {
    let rules_map = @json_decode(rules);
    let mut validations = String::new();
    
    for (field, rule) in rules_map {
        validations.push_str(&format!(r#"
            if !@validator.{}(&request.{}) {{
                errors.insert("{}".to_string(), @validator.get_error("{}", "{}"));
            }}
        "#, rule, field, field, rule, field));
    }
    
    format!(r#"
        let mut errors = std::collections::HashMap::new();
        {}
        
        if !errors.is_empty() {{
            return Err(@ValidationException::new(errors));
        }}
    "#, validations)
}

// Usage
fn create_user(request: &UserRequest) -> Result<User, Box<dyn std::error::Error>> {
    @validate(r#"{
        "name": "required|string|min:3",
        "email": "required|email|unique:users",
        "age": "required|integer|min:18"
    }"#)
    
    // Validation passed, create user
    @User.create(request)
}
```

## Compile-Time Optimization Macros

```rust
// Inline constant expressions
@macro const_eval(expression: &str) {
    let result = @compile_eval(expression);
    format!("{}", result)
}

// Usage
const CONFIG: Config = Config {
    max_items: @const_eval("1024 * 10"),  // Compiles to: 10240
    cache_ttl: @const_eval("60 * 60 * 24"),  // Compiles to: 86400
    api_version: @const_eval(r#""v" + 2 + "." + 1"#)  // Compiles to: "v2.1"
};

// Loop unrolling macro
@macro unroll(count: usize, code: &str) {
    let mut unrolled = String::new();
    
    for i in 0..count {
        let expanded = code.replace("{i}", &i.to_string());
        unrolled.push_str(&expanded);
        unrolled.push('\n');
    }
    
    unrolled
}

// Usage
fn process_pixels(image: &mut Image) {
    @unroll(4, r#"
        let pixel{i} = image.data[offset + {i}];
        image.data[offset + {i}] = transform(pixel{i});
    "#)
    // Generates:
    // let pixel0 = image.data[offset + 0];
    // image.data[offset + 0] = transform(pixel0);
    // let pixel1 = image.data[offset + 1];
    // image.data[offset + 1] = transform(pixel1);
    // ... etc
}
```

## Hygienic Macros

```rust
// Hygienic macro with gensym
@macro swap(a: &str, b: &str) {
    let temp_var = @gensym("temp");  // Generate unique variable name
    
    format!(r#"
        let {} = {};
        {} = {};
        {} = {};
    "#, temp_var, a, a, b, b, temp_var)
}

// Usage - no variable name conflicts
let x = 10;
let y = 20;
let temp = "don't conflict";

@swap(x, y)  // Works correctly, uses generated temp variable

// Macro with lexical scope preservation
@macro with_transaction(code: &str) {
    let tx_var = @gensym("tx");
    
    format!(r#"
        let {} = @db.begin_transaction()?;
        let result = (|| {{
            @bind_context(tx: &{});
            {}
        }})();
        
        match result {{
            Ok(val) => {{
                {}.commit()?;
                Ok(val)
            }},
            Err(e) => {{
                {}.rollback()?;
                Err(e)
            }}
        }}
    "#, tx_var, tx_var, code, tx_var, tx_var)
}
```

## Advanced Macro Patterns

```rust
// Recursive macros
@macro match(value: &str, cases: &str) {
    let cases_map = @json_decode(cases);
    let mut conditions = Vec::new();
    
    for (pattern, code) in cases_map {
        if pattern == "_" {
            conditions.push(format!("_ => {{ {} }}", code));
        } else {
            conditions.push(format!("{} => {{ {} }}", pattern, code));
        }
    }
    
    format!("match {} {{\n    {}\n}}", value, conditions.join(",\n    "))
}

// Macro composition
@macro compose(macros: &[&str]) {
    format!(r#"
        |code: &str| {{
            let mut result = code.to_string();
            {}
            result
        }}
    "#, macros.iter().rev().map(|m| format!("result = {}(&result);", m)).collect::<Vec<_>>().join("\n            "))
}

// Usage
@macro logged(code: &str) {
    format!("@log.info(\"Executing\"); {}", code)
}

@macro timed(code: &str) {
    format!("let start = std::time::Instant::now(); {}; @log.info(\"Time: {:?}\", start.elapsed())", code)
}

let composed = @compose(&["logged", "timed"]);

@composed {
    let result = complex_operation();
}

// Variadic macros
@macro pipeline(steps: &[&str]) {
    let mut code = String::from("let mut _pipeline_result = input;\n");
    
    for step in steps {
        code.push_str(&format!("_pipeline_result = {}(_pipeline_result);\n", step));
    }
    
    code.push_str("_pipeline_result");
    
    format!("|input| {{\n    {}\n}}", code)
}

// Usage
let transform = @pipeline(&["trim", "to_lowercase", "slugify", "|s| s.replace(\"-\", \"\")"]);

let slug = transform("  Hello WORLD!!!  ");  // "helloworld"
```

## Macro Debugging

```rust
// Debug macro expansion
@macro debug_expand(macro_call: &str) {
    let expanded = @expand_macro(macro_call);
    
    format!(r#"
        println!("Macro expansion:");
        println!("Original: {:?}", "{}");
        println!("Expanded: {}", "{}");
        {}
    "#, macro_call, expanded, expanded)
}

// Macro profiling
@macro profile_macro(name: &str, macro_def: &str) {
    format!(r#"
        @macro {}(...args) {{
            let _start = @compile_time();
            let _result = ({})(...args);
            let _duration = @compile_time() - _start;
            
            @compile_log(&format!("{} macro took {{:?}}ms", _duration));
            
            _result
        }}
    "#, name, macro_def, name)
}
```

## Rust-Specific Macro Features

### Type-Safe Macros

```rust
// Type-safe builder pattern
@macro builder(struct_name: &str, fields: &str) {
    let fields_map = @json_decode(fields);
    let mut builder_fields = String::new();
    let mut builder_methods = String::new();
    let mut struct_fields = String::new();
    
    for (name, type_name) in fields_map {
        builder_fields.push_str(&format!("    {}: Option<{}>,\n", name, type_name));
        builder_methods.push_str(&format!(r#"
            pub fn {}(mut self, {}: {}) -> Self {{
                self.{} = Some({});
                self
            }}
        "#, name, name, type_name, name, name));
        struct_fields.push_str(&format!("    pub {}: {},\n", name, type_name));
    }
    
    format!(r#"
        pub struct {}Builder {{
            {}
        }}
        
        impl {}Builder {{
            pub fn new() -> Self {{
                Self {{
                    {}
                }}
            }}
            
            {}
            
            pub fn build(self) -> Result<{}, Box<dyn std::error::Error>> {{
                Ok({} {{
                    {}
                }})
            }}
        }}
        
        pub struct {} {{
            {}
        }}
    "#, struct_name, builder_fields, struct_name, 
        fields_map.iter().map(|(name, _)| format!("{}: None", name)).collect::<Vec<_>>().join(",\n                    "),
        builder_methods, struct_name, struct_name,
        fields_map.iter().map(|(name, _)| format!("{}: self.{}.ok_or(\"Missing {}\")?", name, name, name)).collect::<Vec<_>>().join(",\n                    "),
        struct_name, struct_fields)
}

// Usage
struct User {
    @builder("User", r#"{
        "name": "String",
        "email": "String",
        "age": "u32"
    }"#)
}

// Generated builder
let user = UserBuilder::new()
    .name("John Doe".to_string())
    .email("john@example.com".to_string())
    .age(30)
    .build()?;
```

### Async Macro Support

```rust
// Async macro for Rust
@macro async_handler(handler_code: &str) {
    format!(r#"
        pub async fn handler(request: Request) -> Result<Response, Box<dyn std::error::Error + Send + Sync>> {{
            {}
        }}
    "#, handler_code)
}

// Usage
struct ApiHandler {
    @async_handler(r#"
        let user_id = request.path_params.get("id")
            .ok_or("Missing user ID")?
            .parse::<u32>()?;
        
        let user = @db.users.find(user_id).await?;
        Ok(Response::json(user))
    "#)
}
```

### Error Handling Macros

```rust
// Result wrapper macro
@macro result_wrap(code: &str) {
    format!(r#"
        match {} {{
            Ok(result) => result,
            Err(e) => {{
                @log.error(&format!("Error: {{:?}}", e));
                return Err(e.into());
            }}
        }}
    "#, code)
}

// Usage
fn process_data(data: &[u8]) -> Result<ProcessedData, Box<dyn std::error::Error>> {
    let parsed = @result_wrap(serde_json::from_slice::<RawData>(data));
    let validated = @result_wrap(validate_data(&parsed));
    let processed = @result_wrap(process_validated_data(&validated));
    
    Ok(processed)
}
```

## Performance Considerations

### Compile-Time vs Runtime Macros

```rust
// Compile-time macro (preferred for Rust)
@macro const_config(key: &str, default: &str) {
    let value = @compile_config(key, default);
    format!("const {}: &str = \"{}\";", key.to_uppercase(), value)
}

// Runtime macro (use sparingly)
@macro dynamic_config(key: &str, default: &str) {
    format!(r#"
        @env.get("{}").unwrap_or("{}")
    "#, key, default)
}

// Usage
@const_config("API_VERSION", "v1")  // Compile-time constant
let api_url = @dynamic_config("API_URL", "https://api.example.com");  // Runtime lookup
```

### Memory Safety Macros

```rust
// Safe string manipulation
@macro safe_string_ops(input: &str, operations: &[&str]) {
    let mut code = format!("let mut result = \"{}\".to_string();\n", input);
    
    for op in operations {
        code.push_str(&format!("result = {}(&result);\n", op));
    }
    
    code.push_str("result");
    code
}

// Usage
let processed = @safe_string_ops("  Hello World  ", &["trim", "to_lowercase", "replace_spaces"]);
```

## Best Practices for Rust Macros

1. **Type Safety**: Always prefer compile-time type checking over runtime
2. **Ownership**: Respect Rust's ownership rules in generated code
3. **Error Handling**: Use proper Result types and error propagation
4. **Performance**: Leverage compile-time evaluation when possible
5. **Documentation**: Document macro behavior and generated code
6. **Testing**: Test macro expansions with various inputs
7. **Hygiene**: Use gensym for variable names to avoid conflicts
8. **Composability**: Design macros to work well together

## Integration with Rust Ecosystem

```rust
// Serde integration macro
@macro serde_struct(struct_name: &str, fields: &str) {
    let fields_map = @json_decode(fields);
    let mut serde_fields = String::new();
    let mut struct_fields = String::new();
    
    for (name, type_name) in fields_map {
        serde_fields.push_str(&format!("    #[serde(rename = \"{}\")]\n", name));
        serde_fields.push_str(&format!("    pub {}: {},\n", name, type_name));
        struct_fields.push_str(&format!("    pub {}: {},\n", name, type_name));
    }
    
    format!(r#"
        #[derive(Serialize, Deserialize, Debug)]
        pub struct {} {{
            {}
        }}
        
        impl {} {{
            pub fn new({}) -> Self {{
                Self {{
                    {}
                }}
            }}
        }}
    "#, struct_name, serde_fields, struct_name,
        fields_map.iter().map(|(name, type_name)| format!("{}: {}", name, type_name)).collect::<Vec<_>>().join(", "),
        fields_map.iter().map(|(name, _)| format!("{}", name)).collect::<Vec<_>>().join(", "))
}

// Usage
struct ApiResponse {
    @serde_struct("User", r#"{
        "id": "u32",
        "name": "String",
        "email": "String"
    }"#)
}
```

This comprehensive guide covers Rust-specific macro patterns, ensuring type safety, performance, and integration with Rust's ecosystem while maintaining the power and flexibility of TuskLang's metaprogramming capabilities. 
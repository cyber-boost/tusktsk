# Metaprogramming in TuskLang

Metaprogramming allows you to write code that manipulates code, enabling powerful abstractions and dynamic behavior. TuskLang provides comprehensive metaprogramming capabilities.

## Reflection and Introspection

```tusk
# Class introspection
class User {
    name: string
    email: string
    private password: string
    
    getName() { return this.name }
    setName(value) { this.name = value }
    
    static findAll() { return @db.table("users").get() }
}

# Get class information
user_class: @reflect(User)

# Class properties
properties: user_class.getProperties()
for (prop in properties) {
    @console.log("Property:", {
        name: prop.name,
        type: prop.type,
        visibility: prop.visibility,
        value: prop.defaultValue
    })
}

# Class methods
methods: user_class.getMethods()
for (method in methods) {
    @console.log("Method:", {
        name: method.name,
        parameters: method.parameters,
        returnType: method.returnType,
        isStatic: method.isStatic
    })
}

# Instance introspection
user: new User()
user_meta: @reflect(user)

# Check if property exists
if (user_meta.hasProperty("email")) {
    email_prop: user_meta.getProperty("email")
    current_value: email_prop.getValue()
    email_prop.setValue("new@example.com")
}

# Call methods dynamically
method: user_meta.getMethod("getName")
result: method.invoke(user, [])

# Check inheritance
if (user_meta.isInstanceOf(User)) {
    @console.log("Is a User instance")
}
```

## Dynamic Method Creation

```tusk
# Add methods at runtime
class DynamicModel {
    constructor() {
        this._attributes: {}
    }
    
    # Define getter/setter dynamically
    defineAttribute(name, type = "mixed") {
        # Create getter
        this["get" + @str_studly(name)] = () => {
            return this._attributes[name]
        }
        
        # Create setter with validation
        this["set" + @str_studly(name)] = (value) => {
            if (!@validate_type(value, type)) {
                throw "Invalid type for " + name
            }
            this._attributes[name] = value
            return this
        }
    }
    
    # Method missing handler
    __call(method, args) {
        # Handle dynamic finders
        if (method.startsWith("findBy")) {
            field: @str_snake(method.substring(6))
            return @db.table(this.table)
                .where(field, args[0])
                .first()
        }
        
        # Handle dynamic scopes
        if (method.startsWith("scope")) {
            scope_name: method.substring(5)
            return this._scopes[scope_name]?.apply(this, args)
        }
        
        throw "Method " + method + " not found"
    }
}

# Usage
model: new DynamicModel()
model.defineAttribute("title", "string")
model.defineAttribute("price", "float")

model.setTitle("Product Name")
model.setPrice(29.99)

# Dynamic finder
product: model.findByTitle("Product Name")
```

## Code Generation at Runtime

```tusk
# Generate classes dynamically
function createModelClass(name, fields) {
    # Build class code
    code: "class " + name + " extends Model {\n"
    
    # Add properties
    for (field in fields) {
        code += "    " + field.name + ": " + field.type + "\n"
    }
    
    # Add validation rules
    code += "\n    rules() {\n        return {\n"
    for (field in fields) {
        if (field.validation) {
            code += "            " + field.name + ": '" + field.validation + "',\n"
        }
    }
    code += "        }\n    }\n"
    
    # Add relationships
    code += "\n    relationships() {\n        return {\n"
    for (field in fields) {
        if (field.relationship) {
            code += "            " + field.name + ": " + field.relationship + ",\n"
        }
    }
    code += "        }\n    }\n"
    
    code += "}\n"
    
    # Evaluate the code
    return @eval(code)
}

# Create a model dynamically
ProductModel: createModelClass("Product", [
    {name: "name", type: "string", validation: "required|min:3"},
    {name: "price", type: "float", validation: "required|numeric|min:0"},
    {name: "category_id", type: "int", relationship: "@belongsTo(Category)"},
    {name: "tags", type: "array", relationship: "@belongsToMany(Tag)"}
])

# Use the generated class
product: new ProductModel()
product.name: "Dynamic Product"
product.save()
```

## Attribute Handlers

```tusk
# Custom attribute system
class AttributeHandler {
    static handlers: {}
    
    static define(name, handler) {
        this.handlers[name] = handler
    }
    
    static apply(target, attributes) {
        for (attr in attributes) {
            if (this.handlers[attr.name]) {
                this.handlers[attr.name](target, attr.params)
            }
        }
    }
}

# Define custom attributes
AttributeHandler.define("Logged", (target, params) => {
    original_method: target[params.method]
    
    target[params.method] = function(...args) {
        @log.info("Calling " + params.method, {args})
        result: original_method.apply(this, args)
        @log.info("Result from " + params.method, {result})
        return result
    }
})

AttributeHandler.define("Cached", (target, params) => {
    original_method: target[params.method]
    ttl: params.ttl || 3600
    
    target[params.method] = function(...args) {
        cache_key: params.method + ":" + @json_encode(args)
        
        cached: @cache.get(cache_key)
        if (cached !== null) {
            return cached
        }
        
        result: original_method.apply(this, args)
        @cache.put(cache_key, result, ttl)
        return result
    }
})

# Apply attributes
class Service {
    @Logged(method: "process")
    @Cached(method: "process", ttl: 300)
    process(data) {
        # Expensive operation
        return @transform(data)
    }
}

# Attributes are applied at class definition
AttributeHandler.apply(Service.prototype, [
    {name: "Logged", params: {method: "process"}},
    {name: "Cached", params: {method: "process", ttl: 300}}
])
```

## Proxy Objects

```tusk
# Create proxy for object interception
class ModelProxy {
    constructor(target) {
        return new Proxy(target, {
            get: (obj, prop) => {
                # Log property access
                @metrics.increment("model.access", {prop})
                
                # Transform snake_case to camelCase
                if (prop.includes("_")) {
                    camel_prop: @str_camel(prop)
                    if (camel_prop in obj) {
                        return obj[camel_prop]
                    }
                }
                
                # Return original
                return obj[prop]
            },
            
            set: (obj, prop, value) => {
                # Validate before setting
                if (obj.rules && obj.rules[prop]) {
                    if (!@validate(value, obj.rules[prop])) {
                        throw "Validation failed for " + prop
                    }
                }
                
                # Track changes
                if (obj[prop] !== value) {
                    obj._changed = obj._changed || {}
                    obj._changed[prop] = {
                        from: obj[prop],
                        to: value
                    }
                }
                
                obj[prop] = value
                return true
            },
            
            has: (obj, prop) => {
                # Check both snake_case and camelCase
                return prop in obj || @str_camel(prop) in obj
            },
            
            deleteProperty: (obj, prop) => {
                # Prevent deletion of required fields
                if (obj.required && obj.required.includes(prop)) {
                    throw "Cannot delete required property: " + prop
                }
                delete obj[prop]
                return true
            }
        })
    }
}

# Usage
model: new ModelProxy({
    firstName: "John",
    lastName: "Doe",
    rules: {
        firstName: "string|min:2",
        email: "email"
    },
    required: ["firstName", "lastName"]
})

# Access with different naming conventions
name: model.first_name  # Works via proxy
email: model.email = "john@example.com"  # Validated via proxy
```

## Method Interception and AOP

```tusk
# Aspect-Oriented Programming
class AspectWeaver {
    static before(target, method, advice) {
        original: target[method]
        target[method] = function(...args) {
            advice.apply(this, args)
            return original.apply(this, args)
        }
    }
    
    static after(target, method, advice) {
        original: target[method]
        target[method] = function(...args) {
            result: original.apply(this, args)
            advice.call(this, result, ...args)
            return result
        }
    }
    
    static around(target, method, advice) {
        original: target[method]
        target[method] = function(...args) {
            return advice.call(this, original.bind(this), ...args)
        }
    }
    
    static throwing(target, method, advice) {
        original: target[method]
        target[method] = function(...args) {
            try {
                return original.apply(this, args)
            } catch (error) {
                return advice.call(this, error, ...args)
            }
        }
    }
}

# Apply aspects
class OrderService {
    createOrder(items, user) {
        # Business logic
        order: @Order.create({
            user_id: user.id,
            items: items,
            total: @calculateTotal(items)
        })
        return order
    }
}

# Add cross-cutting concerns
service: new OrderService()

# Logging aspect
AspectWeaver.before(service, "createOrder", (items, user) => {
    @log.info("Creating order", {user_id: user.id, item_count: items.length})
})

# Performance monitoring
AspectWeaver.around(service, "createOrder", (proceed, items, user) => {
    start: @microtime(true)
    result: proceed(items, user)
    duration: @microtime(true) - start
    @metrics.timing("order.create", duration)
    return result
})

# Error handling
AspectWeaver.throwing(service, "createOrder", (error, items, user) => {
    @log.error("Order creation failed", {error, user_id: user.id})
    @notify.admin("Order failure", {error, user})
    throw error
})
```

## Dynamic Type System

```tusk
# Runtime type checking and coercion
class TypeSystem {
    static types: {
        string: {
            check: (v) => typeof v === "string",
            coerce: (v) => String(v)
        },
        int: {
            check: (v) => Number.isInteger(v),
            coerce: (v) => parseInt(v)
        },
        float: {
            check: (v) => typeof v === "number",
            coerce: (v) => parseFloat(v)
        },
        bool: {
            check: (v) => typeof v === "boolean",
            coerce: (v) => !!v
        },
        array: {
            check: (v) => Array.isArray(v),
            coerce: (v) => Array.from(v)
        },
        object: {
            check: (v) => v && typeof v === "object" && !Array.isArray(v),
            coerce: (v) => Object(v)
        }
    }
    
    static define(name, definition) {
        this.types[name] = definition
    }
    
    static check(value, type) {
        if (this.types[type]) {
            return this.types[type].check(value)
        }
        return false
    }
    
    static coerce(value, type) {
        if (this.types[type]) {
            return this.types[type].coerce(value)
        }
        return value
    }
    
    static validate(value, type) {
        # Handle union types
        if (type.includes("|")) {
            types: type.split("|")
            return types.some(t => this.check(value, t.trim()))
        }
        
        # Handle nullable types
        if (type.endsWith("?")) {
            if (value === null || value === undefined) {
                return true
            }
            type: type.slice(0, -1)
        }
        
        # Handle array types
        if (type.endsWith("[]")) {
            if (!Array.isArray(value)) return false
            element_type: type.slice(0, -2)
            return value.every(item => this.validate(item, element_type))
        }
        
        return this.check(value, type)
    }
}

# Define custom types
TypeSystem.define("email", {
    check: (v) => /^[^@]+@[^@]+\.[^@]+$/.test(v),
    coerce: (v) => String(v).toLowerCase().trim()
})

TypeSystem.define("uuid", {
    check: (v) => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v),
    coerce: (v) => String(v).toLowerCase()
})

# Typed property decorator
function typed(type) {
    return (target, property) => {
        private_prop: "_" + property
        
        Object.defineProperty(target, property, {
            get() {
                return this[private_prop]
            },
            set(value) {
                if (!TypeSystem.validate(value, type)) {
                    throw `Invalid type for ${property}: expected ${type}`
                }
                this[private_prop] = TypeSystem.coerce(value, type)
            }
        })
    }
}

# Usage
class User {
    @typed("string")
    name: string
    
    @typed("email")
    email: string
    
    @typed("int?")
    age: int?
    
    @typed("string[]")
    tags: string[]
}
```

## Code Analysis and Transformation

```tusk
# AST manipulation
class CodeTransformer {
    static analyze(code) {
        ast: @parse_code(code)
        
        return {
            functions: @extract_functions(ast),
            variables: @extract_variables(ast),
            imports: @extract_imports(ast),
            complexity: @calculate_complexity(ast)
        }
    }
    
    static transform(code, transformations) {
        ast: @parse_code(code)
        
        for (transform in transformations) {
            ast: transform(ast)
        }
        
        return @generate_code(ast)
    }
    
    static instrument(code, callbacks) {
        ast: @parse_code(code)
        
        # Add instrumentation
        @walk_ast(ast, (node) => {
            if (node.type === "FunctionCall") {
                # Wrap function calls
                node.transform((call) => {
                    return {
                        type: "Block",
                        body: [
                            callbacks.beforeCall(call),
                            call,
                            callbacks.afterCall(call)
                        ]
                    }
                })
            }
        })
        
        return @generate_code(ast)
    }
}

# Usage - Code coverage tool
instrumented: CodeTransformer.instrument(source_code, {
    beforeCall: (call) => {
        return @ast.statement(
            "@coverage.mark('" + call.location + "', 'called')"
        )
    },
    afterCall: (call) => {
        return @ast.statement(
            "@coverage.mark('" + call.location + "', 'completed')"
        )
    }
})
```

## Self-Modifying Code

```tusk
# Class that modifies itself based on usage
class AdaptiveClass {
    static method_calls: {}
    
    constructor() {
        # Monitor method usage
        this._monitor_usage()
    }
    
    _monitor_usage() {
        for (method in @get_methods(this)) {
            if (method.startsWith("_")) continue
            
            original: this[method]
            this[method] = (...args) => {
                # Track usage
                AdaptiveClass.method_calls[method] = 
                    (AdaptiveClass.method_calls[method] || 0) + 1
                
                # Optimize hot paths
                if (AdaptiveClass.method_calls[method] > 100) {
                    this._optimize_method(method)
                }
                
                return original.apply(this, args)
            }
        }
    }
    
    _optimize_method(method) {
        # Replace with optimized version
        if (method === "calculate" && !this._optimized_calculate) {
            # JIT compile or cache results
            original: this[method]
            cache: {}
            
            this[method] = (input) => {
                key: @json_encode(input)
                if (cache[key]) {
                    return cache[key]
                }
                
                result: original.call(this, input)
                cache[key] = result
                return result
            }
            
            this._optimized_calculate = true
            @log.info("Optimized method: calculate")
        }
    }
    
    # Add methods dynamically based on usage patterns
    _adapt_interface() {
        # Analyze usage patterns
        patterns: @analyze_usage_patterns(AdaptiveClass.method_calls)
        
        # Create convenience methods
        for (pattern in patterns) {
            if (pattern.type === "sequential_calls") {
                # Create combined method
                this[pattern.suggested_name] = (...args) => {
                    result: args
                    for (method in pattern.methods) {
                        result = this[method](...result)
                    }
                    return result
                }
            }
        }
    }
}
```

## Best Practices

1. **Use metaprogramming judiciously** - It can make code harder to understand
2. **Document dynamic behavior** - Explain what happens at runtime
3. **Provide type hints** - Even for dynamic code
4. **Test thoroughly** - Dynamic code needs more testing
5. **Consider performance** - Runtime generation has overhead
6. **Maintain debugging support** - Dynamic code is harder to debug
7. **Use established patterns** - Follow known metaprogramming patterns
8. **Keep it simple** - Don't be too clever

## Related Topics

- `reflection` - Runtime introspection
- `dynamic-types` - Dynamic type system
- `code-generation` - Generating code
- `aspect-oriented` - AOP patterns
- `proxy-pattern` - Object proxies
# Macros in TuskLang

Macros in TuskLang provide powerful metaprogramming capabilities, allowing you to generate code at compile time and create domain-specific languages (DSLs).

## Basic Macro Definition

```tusk
# Simple macro
@macro hello(name) {
    return "@console.log('Hello, " + name + "!')"
}

# Usage
@hello("World")  # Expands to: @console.log('Hello, World!')

# Macro with multiple parameters
@macro debug(variable, label = null) {
    if (label) {
        return "@console.log('" + label + ":', " + variable + ")"
    }
    return "@console.log('" + variable + ":', " + variable + ")"
}

# Usage
@debug(user.name)  # @console.log('user.name:', user.name)
@debug(total, "Total Amount")  # @console.log('Total Amount:', total)

# Block macros
@macro benchmark(code) {
    return """
        _benchmark_start: @microtime(true)
        {code}
        _benchmark_end: @microtime(true)
        @console.log('Execution time:', _benchmark_end - _benchmark_start)
    """
}

# Usage
@benchmark {
    result: @expensive_operation()
    @process_result(result)
}
```

## Code Generation Macros

```tusk
# Generate getter/setter methods
@macro property(name, type = "mixed") {
    return """
        private _{name}: {type}
        
        get{name.capitalize()}() {
            return this._{name}
        }
        
        set{name.capitalize()}(value: {type}) {
            this._{name} = value
            return this
        }
    """
}

# Usage in class
class User {
    @property("name", "string")
    @property("email", "string")
    @property("age", "int")
}

# Generates:
# private _name: string
# getName() { return this._name }
# setName(value: string) { this._name = value; return this }
# ... and so on

# Generate CRUD operations
@macro crud(model, table) {
    return """
        static all() {
            return @db.table("{table}").get()
        }
        
        static find(id) {
            return @db.table("{table}").where("id", id).first()
        }
        
        save() {
            if (this.id) {
                return @db.table("{table}")
                    .where("id", this.id)
                    .update(this)
            } else {
                this.id = @db.table("{table}").insertGetId(this)
                return this.id
            }
        }
        
        delete() {
            return @db.table("{table}")
                .where("id", this.id)
                .delete()
        }
    """
}

# Usage
class Product {
    @crud("Product", "products")
}
```

## AST Manipulation Macros

```tusk
# Macro that manipulates Abstract Syntax Tree
@macro memoize(method) {
    # Parse the method AST
    ast: @parse_method(method)
    method_name: ast.name
    params: ast.params.join(", ")
    
    return """
        {method_name}({params}) {
            _cache_key: "{method_name}:" + @json_encode([{params}])
            
            if (@cache.has(_cache_key)) {
                return @cache.get(_cache_key)
            }
            
            _result: (() => {
                {ast.body}
            })()
            
            @cache.put(_cache_key, _result, 3600)
            return _result
        }
    """
}

# Usage
class Calculator {
    @memoize
    fibonacci(n) {
        if (n <= 1) return n
        return this.fibonacci(n - 1) + this.fibonacci(n - 2)
    }
}

# Macro for automatic dependency injection
@macro inject(dependencies) {
    injections: ""
    
    for (name, type in dependencies) {
        injections += "this.{name} = @container.make('{type}')\n"
    }
    
    return """
        constructor() {
            {injections}
        }
    """
}

# Usage
class UserService {
    @inject({
        db: "Database",
        cache: "Cache",
        logger: "Logger"
    })
    
    getUser(id) {
        return this.cache.remember("user." + id, 3600, () => {
            this.logger.info("Fetching user", {id})
            return this.db.table("users").find(id)
        })
    }
}
```

## Conditional Compilation Macros

```tusk
# Environment-based code generation
@macro env_specific(code_map) {
    env: @compile_env("APP_ENV", "production")
    
    if (code_map[env]) {
        return code_map[env]
    }
    
    return code_map.default || ""
}

# Usage
class Logger {
    @env_specific({
        development: """
            log(message, context = {}) {
                @console.log('[DEV]', message, context)
                @file.append('debug.log', @json_encode({message, context, time: @now()}))
            }
        """,
        
        production: """
            log(message, context = {}) {
                @sentry.captureMessage(message, context)
            }
        """,
        
        testing: """
            log(message, context = {}) {
                @test_logs.push({message, context})
            }
        """
    })
}

# Feature flag macros
@macro feature(flag, enabled_code, disabled_code = "") {
    if (@compile_config("features." + flag, false)) {
        return enabled_code
    }
    return disabled_code
}

# Usage
class PaymentProcessor {
    @feature("stripe_payment", """
        processStripePayment(amount, token) {
            return @stripe.charges.create({
                amount: amount,
                source: token,
                currency: 'usd'
            })
        }
    """)
    
    @feature("crypto_payment", """
        processCryptoPayment(amount, wallet) {
            return @crypto.transfer({
                to: wallet,
                amount: amount,
                coin: 'BTC'
            })
        }
    """, """
        processCryptoPayment(amount, wallet) {
            throw "Crypto payments not enabled"
        }
    """)
}
```

## DSL Creation with Macros

```tusk
# Create a routing DSL
@macro route(path, options = {}) {
    method: options.method || "GET"
    middleware: options.middleware || []
    name: options.name || path.replace("/", ".").trim(".")
    
    return """
        @router.{method.toLowerCase()}('{path}', {{
            middleware: {middleware},
            name: '{name}',
            handler: (request, response) => {{
                {options.handler}
            }}
        }})
    """
}

# Usage
@route("/users", {
    method: "GET",
    middleware: ["auth", "throttle:60,1"],
    name: "users.index",
    handler: """
        users: @User.paginate(20)
        return @view('users.index', {users})
    """
})

# Create a validation DSL
@macro validate(rules) {
    validations: []
    
    for (field, rule in rules) {
        validations.push("""
            if (!@validator.{rule}(@request.{field})) {
                errors.{field} = @validator.getError('{rule}', '{field}')
            }
        """)
    }
    
    return """
        errors: {}
        {validations.join("\n")}
        
        if (Object.keys(errors).length > 0) {
            throw @ValidationException(errors)
        }
    """
}

# Usage
function createUser(request) {
    @validate({
        name: "required|string|min:3",
        email: "required|email|unique:users",
        age: "required|integer|min:18"
    })
    
    # Validation passed, create user
    return @User.create(request)
}
```

## Compile-Time Optimization Macros

```tusk
# Inline constant expressions
@macro const_eval(expression) {
    # Evaluate at compile time
    result: @compile_eval(expression)
    return @stringify(result)
}

# Usage
config: {
    max_items: @const_eval(1024 * 10),  # Compiles to: 10240
    cache_ttl: @const_eval(60 * 60 * 24),  # Compiles to: 86400
    api_version: @const_eval("v" + 2 + "." + 1)  # Compiles to: "v2.1"
}

# Loop unrolling macro
@macro unroll(count, code) {
    unrolled: ""
    
    for (i in 0..count-1) {
        unrolled += code.replace(/\{i\}/g, i) + "\n"
    }
    
    return unrolled
}

# Usage
function processPixels(image) {
    @unroll(4, """
        pixel{i}: image.data[offset + {i}]
        image.data[offset + {i}] = transform(pixel{i})
    """)
    # Generates:
    # pixel0: image.data[offset + 0]
    # image.data[offset + 0] = transform(pixel0)
    # pixel1: image.data[offset + 1]
    # image.data[offset + 1] = transform(pixel1)
    # ... etc
}
```

## Hygienic Macros

```tusk
# Hygienic macro with gensym
@macro swap(a, b) {
    temp_var: @gensym("temp")  # Generate unique variable name
    
    return """
        {temp_var}: {a}
        {a} = {b}
        {b} = {temp_var}
    """
}

# Usage - no variable name conflicts
x: 10
y: 20
temp: "don't conflict"

@swap(x, y)  # Works correctly, uses generated temp variable

# Macro with lexical scope preservation
@macro with_transaction(code) {
    tx_var: @gensym("tx")
    
    return """
        {tx_var}: @db.beginTransaction()
        try {
            @bind_context(tx: {tx_var})
            {code}
            {tx_var}.commit()
        } catch (e) {
            {tx_var}.rollback()
            throw e
        }
    """
}
```

## Advanced Macro Patterns

```tusk
# Recursive macros
@macro match(value, cases) {
    conditions: []
    
    for (pattern, code in cases) {
        if (pattern == "_") {
            conditions.push("else { " + code + " }")
        } else {
            conditions.push("if ({value} == {pattern}) { " + code + " }")
        }
    }
    
    return conditions.join(" else ")
}

# Macro composition
@macro compose(...macros) {
    return (code) => {
        result: code
        for (macro in macros.reverse()) {
            result: macro(result)
        }
        return result
    }
}

# Usage
@macro logged(code) {
    return "@log.info('Executing'); " + code
}

@macro timed(code) {
    return "start: @now(); " + code + "; @log.info('Time:', @now() - start)"
}

composed: @compose(logged, timed)

@composed {
    result: @complex_operation()
}

# Variadic macros
@macro pipeline(...steps) {
    code: "_pipeline_result: input\n"
    
    for (step in steps) {
        code += "_pipeline_result = {step}(_pipeline_result)\n"
    }
    
    code += "return _pipeline_result"
    
    return "(input) => {\n" + code + "\n}"
}

# Usage
transform: @pipeline(
    "@trim",
    "@lowercase",
    "@slugify",
    "(s) => s.replace(/-+/g, '-')"
)

slug: transform("  Hello WORLD!!!  ")  # "hello-world"
```

## Macro Debugging

```tusk
# Debug macro expansion
@macro debug_expand(macro_call) {
    expanded: @expand_macro(macro_call)
    
    @console.log("Macro expansion:")
    @console.log("Original:", @stringify(macro_call))
    @console.log("Expanded:", expanded)
    
    return expanded
}

# Macro profiling
@macro profile_macro(name, macro_def) {
    return """
        @macro {name}(...args) {
            _start: @compile_time()
            _result: ({macro_def})(...args)
            _duration: @compile_time() - _start
            
            @compile_log('{name} macro took ' + _duration + 'ms')
            
            return _result
        }
    """
}
```

## Best Practices

1. **Keep macros simple** - Complex macros are hard to debug
2. **Use hygienic macros** - Avoid variable name conflicts
3. **Document macro behavior** - Explain what code is generated
4. **Validate macro inputs** - Check parameters at compile time
5. **Consider alternatives** - Sometimes functions are better
6. **Test macro expansion** - Verify generated code
7. **Use meaningful names** - Make macro purpose clear
8. **Limit macro scope** - Don't overuse macros

## Related Topics

- `metaprogramming` - Advanced metaprogramming
- `code-generation` - Code generation techniques
- `compile-time-evaluation` - Compile-time features
- `dsl-creation` - Domain-specific languages
- `macro-hygiene` - Hygienic macro patterns
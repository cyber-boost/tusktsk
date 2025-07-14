# @ Custom Operators

TuskLang allows you to define custom @ operators to extend the language with domain-specific functionality and create reusable abstractions.

## Defining Custom Operators

```tusk
# Basic custom operator
@define("uppercase", (value) => {
    return @strtoupper(@value)
})

# Usage
name: @uppercase("john doe")  # "JOHN DOE"

# Operator with multiple parameters
@define("between", (value, min, max) => {
    return @value >= @min && @value <= @max
})

# Usage
is_valid: @between(@age, 18, 65)
```

## Operator Namespaces

```tusk
# Create namespaced operators
@namespace("math") {
    # Define operators within namespace
    @define("factorial", (n) => {
        @if(@n <= 1) return 1
        return @n * @math.factorial(@n - 1)
    })
    
    @define("fibonacci", (n) => {
        @if(@n <= 1) return @n
        return @math.fibonacci(@n - 1) + @math.fibonacci(@n - 2)
    })
    
    @define("isPrime", (n) => {
        @if(@n <= 1) return false
        @for(i: 2; @i * @i <= @n; @i++) {
            @if(@n % @i == 0) return false
        }
        return true
    })
}

# Usage
result: @math.factorial(5)      # 120
fib: @math.fibonacci(10)        # 55
prime: @math.isPrime(17)        # true
```

## Chainable Operators

```tusk
# Create chainable custom operators
@define_chainable("query", {
    # Initialize with table
    constructor: (table) => {
        @this._table: @table
        @this._conditions: []
        @this._order: null
        @this._limit: null
    }
    
    # Chainable methods
    where: (field, operator, value) => {
        @this._conditions[]: {field: @field, op: @operator, val: @value}
        return @this
    }
    
    orderBy: (field, direction: "asc") => {
        @this._order: {field: @field, direction: @direction}
        return @this
    }
    
    limit: (count) => {
        @this._limit: @count
        return @this
    }
    
    # Execute query
    get: () => {
        sql: "SELECT * FROM " + @this._table
        
        @if(@count(@this._conditions) > 0) {
            conditions: @this._conditions.map(c => 
                @c.field + " " + @c.op + " ?"
            )
            sql: @sql + " WHERE " + @join(" AND ", @conditions)
        }
        
        @if(@this._order) {
            sql: @sql + " ORDER BY " + @this._order.field + " " + 
                 @this._order.direction
        }
        
        @if(@this._limit) {
            sql: @sql + " LIMIT " + @this._limit
        }
        
        params: @this._conditions.map(c => @c.val)
        return @execute_query(@sql, @params)
    }
})

# Usage
users: @query("users")
    .where("age", ">", 18)
    .where("status", "=", "active")
    .orderBy("created_at", "desc")
    .limit(10)
    .get()
```

## Property Operators

```tusk
# Define property-style operators
@define_property("age", {
    get: () => {
        @if(@this.birthdate) {
            return @calculate_age(@this.birthdate)
        }
        return null
    }
    
    set: (value) => {
        # Calculate birthdate from age
        @this.birthdate: @date("Y-m-d", @strtotime("-" + @value + " years"))
    }
})

# Usage in objects
person: {
    name: "John"
    birthdate: "1990-05-15"
    age: @age  # Calculates from birthdate
}

person.age: 25  # Sets birthdate accordingly
```

## Validation Operators

```tusk
# Custom validation operators
@namespace("validate") {
    @define("email", (value) => {
        pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/
        return @pattern.test(@value)
    })
    
    @define("phone", (value) => {
        # Remove non-digits
        digits: @preg_replace("/[^0-9]/", "", @value)
        return @strlen(@digits) == 10 || @strlen(@digits) == 11
    })
    
    @define("creditCard", (value) => {
        # Luhn algorithm
        digits: @preg_replace("/[^0-9]/", "", @value)
        sum: 0
        alternate: false
        
        @for(i: @strlen(@digits) - 1; @i >= 0; @i--) {
            n: @int(@digits[@i])
            @if(@alternate) {
                n: @n * 2
                @if(@n > 9) n: @n - 9
            }
            sum: @sum + @n
            alternate: !@alternate
        }
        
        return @sum % 10 == 0
    })
}

# Usage
is_valid_email: @validate.email("user@example.com")
is_valid_phone: @validate.phone("(555) 123-4567")
is_valid_card: @validate.creditCard("4532-1234-5678-9012")
```

## Type Conversion Operators

```tusk
# Custom type converters
@namespace("convert") {
    @define("money", (value, currency: "USD") => {
        amount: @float(@value)
        
        return {
            amount: @amount
            currency: @currency
            formatted: @format_currency(@amount, @currency)
            
            # Methods
            add: (other) => @convert.money(@amount + @other.amount, @currency)
            subtract: (other) => @convert.money(@amount - @other.amount, @currency)
            multiply: (factor) => @convert.money(@amount * @factor, @currency)
            
            # Conversion
            to: (target_currency) => {
                rate: @get_exchange_rate(@currency, @target_currency)
                return @convert.money(@amount * @rate, @target_currency)
            }
        }
    })
    
    @define("duration", (value, unit: "seconds") => {
        seconds: @convert_to_seconds(@value, @unit)
        
        return {
            seconds: @seconds
            minutes: @seconds / 60
            hours: @seconds / 3600
            days: @seconds / 86400
            
            formatted: @format_duration(@seconds)
            
            add: (other) => @convert.duration(@seconds + @other.seconds)
            subtract: (other) => @convert.duration(@seconds - @other.seconds)
        }
    })
}

# Usage
price: @convert.money(99.99)
total: @price.multiply(1.08)  # Add tax
euros: @total.to("EUR")

duration: @convert.duration(90, "minutes")
remaining: @duration.subtract(@convert.duration(30, "minutes"))
```

## Async Operators

```tusk
# Define async operators
@define_async("fetch_json", async (url, options: {}) => {
    response: await @fetch(@url, @options)
    
    @if(!@response.ok) {
        @throw new Error("HTTP " + @response.status)
    }
    
    return await @response.json()
})

@define_async("parallel", async (tasks) => {
    return await @Promise.all(@tasks)
})

# Usage
data: await @fetch_json("https://api.example.com/data")

results: await @parallel([
    @fetch_json("/api/users"),
    @fetch_json("/api/posts"),
    @fetch_json("/api/comments")
])
```

## Macro Operators

```tusk
# Define macro operators for code generation
@define_macro("crud", (model_name) => {
    # Generate CRUD operations
    return {
        create: (data) => @{model_name}.create(@data)
        read: (id) => @{model_name}.find(@id)
        update: (id, data) => @{model_name}.update(@id, @data)
        delete: (id) => @{model_name}.destroy(@id)
        list: (filters: {}) => @{model_name}.where(@filters).get()
    }
})

# Usage
UserCrud: @crud("User")
users: @UserCrud.list({active: true})
new_user: @UserCrud.create({name: "John", email: "john@example.com"})
```

## Domain-Specific Operators

```tusk
# E-commerce operators
@namespace("shop") {
    @define("calculateTax", (amount, location) => {
        rate: @get_tax_rate(@location)
        return @amount * @rate
    })
    
    @define("applyDiscount", (price, discount) => {
        @if(@discount.type == "percentage") {
            return @price * (1 - @discount.value / 100)
        } elseif(@discount.type == "fixed") {
            return @max(0, @price - @discount.value)
        }
        return @price
    })
    
    @define("calculateShipping", (items, destination) => {
        weight: @sum(@items.map(i => @i.weight * @i.quantity))
        distance: @calculate_distance(@destination)
        
        return @calculate_shipping_cost(@weight, @distance)
    })
}

# Usage
subtotal: 99.99
tax: @shop.calculateTax(@subtotal, @user.location)
discounted: @shop.applyDiscount(@subtotal, {type: "percentage", value: 10})
shipping: @shop.calculateShipping(@cart.items, @user.address)
```

## Operator Composition

```tusk
# Compose operators from existing ones
@define("process_order", @compose(
    @validate.order,
    @shop.calculateTotals,
    @payment.charge,
    @inventory.reserve,
    @notification.send
))

# Usage
result: @process_order(@order_data)

# Create operator pipelines
@define("sanitize_input", @pipeline(
    @trim,
    @strip_tags,
    @escape_html,
    @normalize_whitespace
))

# Usage
clean: @sanitize_input(@user_input)
```

## Testing Custom Operators

```tusk
# Test custom operators
@test("math operators") {
    @assert(@math.factorial(5) == 120)
    @assert(@math.fibonacci(10) == 55)
    @assert(@math.isPrime(17) == true)
    @assert(@math.isPrime(18) == false)
}

@test("validation operators") {
    @assert(@validate.email("test@example.com") == true)
    @assert(@validate.email("invalid-email") == false)
    @assert(@validate.phone("555-123-4567") == true)
}
```

## Registration and Discovery

```tusk
# Register operators globally
@register_operator("myapp", {
    version: "1.0.0"
    operators: {
        helper1: @helper1_function
        helper2: @helper2_function
    }
})

# Discover available operators
available: @discover_operators()
/* Output:
{
    core: [...],        # Built-in operators
    math: [...],        # Custom math namespace
    validate: [...],    # Custom validation namespace
    myapp: [...]       # Registered operators
}
*/

# Get operator info
info: @operator_info("math.factorial")
/* Output:
{
    name: "factorial"
    namespace: "math"
    parameters: ["n"]
    description: "Calculate factorial of n"
    examples: ["@math.factorial(5) // returns 120"]
}
*/
```

## Best Practices

1. **Use clear naming** - Operators should be self-documenting
2. **Follow conventions** - Stick to TuskLang naming patterns
3. **Add validation** - Validate inputs in custom operators
4. **Document thoroughly** - Include examples and parameter descriptions
5. **Test extensively** - Custom operators should be well-tested
6. **Consider performance** - Optimize frequently used operators

## Related Features

- `@define()` - Define custom functions
- `@namespace()` - Organize operators
- `@compose()` - Function composition
- `@extend()` - Extend existing operators
- `@override()` - Override built-in operators
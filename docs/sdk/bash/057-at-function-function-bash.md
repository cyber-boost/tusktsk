# ⚙️ TuskLang Bash @function Function Guide

**"We don't bow to any king" – Functions are your configuration's logic engine.**

The @function function in TuskLang is your dynamic logic powerhouse, enabling custom function creation, code execution, and programmatic behavior directly within your configuration files. Whether you're creating reusable logic, processing data with custom algorithms, or implementing complex business rules, @function provides the flexibility and power to make your configurations truly programmable.

## 🎯 What is @function?
The @function function provides function operations in TuskLang. It offers:
- **Function creation** - Define custom functions with parameters
- **Function execution** - Call functions with arguments
- **Code reuse** - Create reusable logic blocks
- **Dynamic behavior** - Implement conditional and iterative logic
- **Integration** - Connect with external systems and APIs

## 📝 Basic @function Syntax

### Function Creation
```ini
[function_creation]
# Create simple functions
greet: @function.create("name", "return 'Hello, ' + name + '!'")
calculate_area: @function.create("width, height", "return width * height")
format_currency: @function.create("amount, currency", "return currency + ' ' + amount.toFixed(2)")

# Create functions with multiple parameters
user_info: @function.create("name, age, city", """
    return {
        'name': name,
        'age': age,
        'city': city,
        'adult': age >= 18,
        'greeting': 'Hello, ' + name + ' from ' + city + '!'
    }
""")

# Create functions with default parameters
config_with_defaults: @function.create("host='localhost', port=3306, name='tusklang'", """
    return {
        'database': {
            'host': host,
            'port': port,
            'name': name,
            'url': 'mysql://' + host + ':' + port + '/' + name
        }
    }
""")
```

### Function Execution
```ini
[function_execution]
# Execute simple functions
greeting: @function.call("greet", "Alice")
area: @function.call("calculate_area", 10, 5)
price: @function.call("format_currency", 99.99, "USD")

# Execute complex functions
user_data: @function.call("user_info", "Bob", 25, "New York")
db_config: @function.call("config_with_defaults", "prod.example.com", 5432, "production_db")

# Execute functions with dynamic parameters
$user_name: "Charlie"
$user_age: 30
$user_city: "San Francisco"
dynamic_user: @function.call("user_info", $user_name, $user_age, $user_city)
```

### Advanced Function Patterns
```ini
[advanced_functions]
# Create functions with conditional logic
validate_user: @function.create("name, age, email", """
    var errors = [];
    if (!name || name.length < 2) {
        errors.push('Name must be at least 2 characters');
    }
    if (age < 0 || age > 150) {
        errors.push('Age must be between 0 and 150');
    }
    if (!email || !email.includes('@')) {
        errors.push('Valid email is required');
    }
    return {
        'valid': errors.length === 0,
        'errors': errors
    }
""")

# Create functions with loops and arrays
process_users: @function.create("users", """
    var results = [];
    for (var i = 0; i < users.length; i++) {
        var user = users[i];
        results.push({
            'id': user.id,
            'name': user.name,
            'status': user.age >= 18 ? 'adult' : 'minor',
            'greeting': 'Hello, ' + user.name + '!'
        });
    }
    return results;
""")

# Create functions with external API calls
fetch_weather: @function.create("city", """
    var api_url = 'https://api.weatherapi.com/v1/current.json?key=' + 
                  @env('WEATHER_API_KEY') + '&q=' + encodeURIComponent(city);
    var response = @http('GET', api_url);
    var data = JSON.parse(response);
    return {
        'city': city,
        'temperature': data.current.temp_c,
        'condition': data.current.condition.text,
        'humidity': data.current.humidity
    }
""")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > function-quickstart.tsk << 'EOF'
[basic_functions]
# Create simple utility functions
greet: @function.create("name", "return 'Hello, ' + name + '!'")
calculate_total: @function.create("items", """
    var total = 0;
    for (var i = 0; i < items.length; i++) {
        total += items[i].price * items[i].quantity;
    }
    return total;
""")

format_user: @function.create("name, age, email", """
    return {
        'display_name': name.toUpperCase(),
        'age_group': age < 18 ? 'minor' : age < 65 ? 'adult' : 'senior',
        'email_domain': email.split('@')[1],
        'greeting': 'Welcome, ' + name + '!'
    }
""")

[function_execution]
# Execute functions
greeting_message: @function.call("greet", "Alice")
user_info: @function.call("format_user", "Bob", 25, "bob@example.com")

# Execute with array data
$shopping_items: [
    {"name": "Apple", "price": 1.00, "quantity": 5},
    {"name": "Banana", "price": 0.50, "quantity": 10},
    {"name": "Orange", "price": 1.25, "quantity": 3}
]

total_cost: @function.call("calculate_total", $shopping_items)

[advanced_functions]
# Create validation function
validate_order: @function.create("order", """
    var errors = [];
    if (!order.customer_name) {
        errors.push('Customer name is required');
    }
    if (!order.items || order.items.length === 0) {
        errors.push('At least one item is required');
    }
    if (order.total < 0) {
        errors.push('Total cannot be negative');
    }
    return {
        'valid': errors.length === 0,
        'errors': errors,
        'order_id': order.id || @date.timestamp()
    }
""")

# Test validation
$test_order: {
    "customer_name": "Charlie",
    "items": [{"name": "Book", "price": 15.99, "quantity": 1}],
    "total": 15.99
}

validation_result: @function.call("validate_order", $test_order)
EOF

config=$(tusk_parse function-quickstart.tsk)

echo "=== Basic Functions ==="
echo "Greeting: $(tusk_get "$config" function_execution.greeting_message)"
echo "User Info: $(tusk_get "$config" function_execution.user_info)"
echo "Total Cost: $(tusk_get "$config" function_execution.total_cost)"

echo ""
echo "=== Advanced Functions ==="
echo "Validation Result: $(tusk_get "$config" advanced_functions.validation_result)"
```

## 🔗 Real-World Use Cases

### 1. Data Processing and Transformation
```ini
[data_processing]
# Create data processing functions
$data_functions: {
    "normalize_user_data": @function.create("users", """
        var normalized = [];
        for (var i = 0; i < users.length; i++) {
            var user = users[i];
            normalized.push({
                'id': parseInt(user.id) || 0,
                'name': user.name.trim().toLowerCase(),
                'email': user.email.toLowerCase(),
                'age': parseInt(user.age) || 0,
                'status': user.status || 'inactive',
                'created_at': user.created_at || @date('Y-m-d H:i:s')
            });
        }
        return normalized;
    """),
    
    "calculate_statistics": @function.create("data, field", """
        var values = data.map(function(item) { return parseFloat(item[field]) || 0; });
        var sum = values.reduce(function(a, b) { return a + b; }, 0);
        var avg = sum / values.length;
        var max = Math.max.apply(null, values);
        var min = Math.min.apply(null, values);
        
        return {
            'count': values.length,
            'sum': sum,
            'average': avg,
            'maximum': max,
            'minimum': min,
            'range': max - min
        };
    """),
    
    "filter_and_sort": @function.create("data, filter_field, filter_value, sort_field, sort_order", """
        var filtered = data.filter(function(item) {
            return item[filter_field] === filter_value;
        });
        
        filtered.sort(function(a, b) {
            var a_val = a[sort_field];
            var b_val = b[sort_field];
            if (sort_order === 'desc') {
                return b_val > a_val ? 1 : -1;
            } else {
                return a_val > b_val ? 1 : -1;
            }
        });
        
        return filtered;
    """)
}

# Process user data
$raw_user_data: [
    {"id": "1", "name": "  Alice  ", "email": "ALICE@EXAMPLE.COM", "age": "25", "status": "active"},
    {"id": "2", "name": "Bob", "email": "bob@example.com", "age": "30", "status": "inactive"},
    {"id": "3", "name": "Charlie", "email": "CHARLIE@EXAMPLE.COM", "age": "35", "status": "active"}
]

$processed_data: {
    "normalized_users": @function.call("normalize_user_data", $raw_user_data),
    "age_statistics": @function.call("calculate_statistics", $raw_user_data, "age"),
    "active_users": @function.call("filter_and_sort", $raw_user_data, "status", "active", "name", "asc")
}
```

### 2. Business Logic and Validation
```ini
[business_logic]
# Create business logic functions
$business_functions: {
    "calculate_discount": @function.create("order_total, customer_type, loyalty_years", """
        var discount = 0;
        
        // Base discount by customer type
        if (customer_type === 'vip') {
            discount += 15;
        } else if (customer_type === 'premium') {
            discount += 10;
        } else if (customer_type === 'regular') {
            discount += 5;
        }
        
        // Loyalty discount
        if (loyalty_years >= 5) {
            discount += 5;
        } else if (loyalty_years >= 2) {
            discount += 2;
        }
        
        // Volume discount
        if (order_total >= 1000) {
            discount += 10;
        } else if (order_total >= 500) {
            discount += 5;
        }
        
        return Math.min(discount, 30); // Cap at 30%
    """),
    
    "validate_inventory": @function.create("order_items, inventory", """
        var issues = [];
        var available = true;
        
        for (var i = 0; i < order_items.length; i++) {
            var item = order_items[i];
            var stock = inventory[item.product_id];
            
            if (!stock) {
                issues.push('Product ' + item.product_id + ' not found in inventory');
                available = false;
            } else if (stock.quantity < item.quantity) {
                issues.push('Insufficient stock for product ' + item.product_id + 
                          ' (requested: ' + item.quantity + ', available: ' + stock.quantity + ')');
                available = false;
            } else if (stock.quantity < item.quantity * 2) {
                issues.push('Low stock warning for product ' + item.product_id);
            }
        }
        
        return {
            'available': available,
            'issues': issues,
            'can_fulfill': available
        };
    """),
    
    "calculate_shipping": @function.create("items, destination, shipping_method", """
        var base_cost = 0;
        var total_weight = 0;
        
        // Calculate total weight
        for (var i = 0; i < items.length; i++) {
            total_weight += items[i].weight * items[i].quantity;
        }
        
        // Base shipping cost
        if (shipping_method === 'express') {
            base_cost = 25;
        } else if (shipping_method === 'standard') {
            base_cost = 10;
        } else {
            base_cost = 5; // economy
        }
        
        // Weight-based surcharge
        if (total_weight > 10) {
            base_cost += (total_weight - 10) * 2;
        }
        
        // Destination surcharge
        if (destination === 'international') {
            base_cost *= 3;
        } else if (destination === 'remote') {
            base_cost *= 1.5;
        }
        
        return {
            'base_cost': base_cost,
            'total_weight': total_weight,
            'estimated_days': shipping_method === 'express' ? 1 : 
                            shipping_method === 'standard' ? 3 : 7
        };
    """)
}

# Apply business logic
$order_data: {
    "items": [
        {"product_id": "P001", "quantity": 2, "weight": 0.5},
        {"product_id": "P002", "quantity": 1, "weight": 2.0}
    ],
    "customer_type": "premium",
    "loyalty_years": 3,
    "destination": "domestic",
    "shipping_method": "standard"
}

$business_results: {
    "discount_percentage": @function.call("calculate_discount", 750, $order_data.customer_type, $order_data.loyalty_years),
    "shipping_info": @function.call("calculate_shipping", $order_data.items, $order_data.destination, $order_data.shipping_method)
}
```

### 3. API Integration and External Services
```ini
[api_integration]
# Create API integration functions
$api_functions: {
    "fetch_user_data": @function.create("user_id", """
        var api_url = @env('API_BASE_URL') + '/users/' + user_id;
        var response = @http('GET', api_url, {
            'Authorization': 'Bearer ' + @env('API_TOKEN'),
            'Content-Type': 'application/json'
        });
        
        if (response.status === 200) {
            return JSON.parse(response.body);
        } else {
            return {
                'error': true,
                'message': 'Failed to fetch user data',
                'status': response.status
            };
        }
    """),
    
    "sync_database": @function.create("table_name, last_sync", """
        var sync_url = @env('SYNC_API_URL') + '/sync/' + table_name;
        var params = {
            'last_sync': last_sync || @date.sub(@date.now(), '1d'),
            'batch_size': 1000
        };
        
        var response = @http('POST', sync_url, {
            'Authorization': 'Bearer ' + @env('SYNC_TOKEN'),
            'Content-Type': 'application/json'
        }, JSON.stringify(params));
        
        if (response.status === 200) {
            var data = JSON.parse(response.body);
            return {
                'success': true,
                'records_processed': data.count,
                'last_sync': @date.now(),
                'data': data.records
            };
        } else {
            return {
                'success': false,
                'error': 'Sync failed',
                'status': response.status
            };
        }
    """),
    
    "send_notification": @function.create("user_id, message, type", """
        var notification_data = {
            'user_id': user_id,
            'message': message,
            'type': type || 'info',
            'timestamp': @date('Y-m-d H:i:s'),
            'priority': type === 'urgent' ? 'high' : 'normal'
        };
        
        var response = @http('POST', @env('NOTIFICATION_API_URL'), {
            'Authorization': 'Bearer ' + @env('NOTIFICATION_TOKEN'),
            'Content-Type': 'application/json'
        }, JSON.stringify(notification_data));
        
        return {
            'sent': response.status === 200,
            'message_id': response.status === 200 ? JSON.parse(response.body).id : null,
            'status': response.status
        };
    """)
}

# Use API functions
$api_operations: {
    "user_profile": @function.call("fetch_user_data", 123),
    "sync_users": @function.call("sync_database", "users", @date.sub(@date.now(), '7d')),
    "welcome_notification": @function.call("send_notification", 123, "Welcome to our platform!", "info")
}
```

### 4. Configuration Generation and Management
```ini
[config_generation]
# Create configuration generation functions
$config_functions: {
    "generate_server_config": @function.create("environment, server_count", """
        var config = {
            'environment': environment,
            'servers': [],
            'load_balancer': {
                'algorithm': 'round_robin',
                'health_check': '/health',
                'timeout': 30
            }
        };
        
        for (var i = 1; i <= server_count; i++) {
            config.servers.push({
                'id': 'server_' + i,
                'host': environment + '_server_' + i + '.example.com',
                'port': 8080,
                'weight': 1,
                'max_connections': 1000
            });
        }
        
        return config;
    """),
    
    "generate_database_config": @function.create("db_type, environment, connection_pool", """
        var base_config = {
            'type': db_type,
            'environment': environment,
            'connection_pool': connection_pool || 10
        };
        
        if (db_type === 'mysql') {
            base_config.driver = 'mysql2';
            base_config.port = 3306;
            base_config.charset = 'utf8mb4';
        } else if (db_type === 'postgresql') {
            base_config.driver = 'pg';
            base_config.port = 5432;
            base_config.ssl = environment === 'production';
        }
        
        base_config.host = @env(environment.toUpperCase() + '_DB_HOST');
        base_config.database = @env(environment.toUpperCase() + '_DB_NAME');
        base_config.username = @env(environment.toUpperCase() + '_DB_USER');
        base_config.password = @env(environment.toUpperCase() + '_DB_PASSWORD');
        
        return base_config;
    """),
    
    "generate_api_config": @function.create("version, features", """
        var config = {
            'version': version,
            'base_path': '/api/v' + version,
            'rate_limit': {
                'requests_per_minute': 100,
                'burst_size': 20
            },
            'cors': {
                'enabled': true,
                'origins': ['https://example.com', 'https://app.example.com']
            },
            'features': features || []
        };
        
        // Add feature-specific configurations
        if (features.includes('authentication')) {
            config.auth = {
                'type': 'jwt',
                'secret': @env('JWT_SECRET'),
                'expires_in': '24h'
            };
        }
        
        if (features.includes('caching')) {
            config.cache = {
                'enabled': true,
                'ttl': 300,
                'driver': 'redis'
            };
        }
        
        return config;
    """)
}

# Generate configurations
$generated_configs: {
    "production_servers": @function.call("generate_server_config", "production", 5),
    "mysql_config": @function.call("generate_database_config", "mysql", "production", 20),
    "api_config": @function.call("generate_api_config", "2", ["authentication", "caching", "logging"])
}
```

## 🧠 Advanced @function Patterns

### Function Composition and Chaining
```ini
[function_composition]
# Create composable functions
$composable_functions: {
    "pipe": @function.create("functions, initial_value", """
        var result = initial_value;
        for (var i = 0; i < functions.length; i++) {
            result = @function.call(functions[i], result);
        }
        return result;
    """),
    
    "compose": @function.create("functions", """
        return function(data) {
            var result = data;
            for (var i = functions.length - 1; i >= 0; i--) {
                result = @function.call(functions[i], result);
            }
            return result;
        };
    """),
    
    "curry": @function.create("fn, arity", """
        return function curried() {
            var args = Array.prototype.slice.call(arguments);
            if (args.length >= arity) {
                return @function.call(fn, args);
            }
            return function() {
                var more_args = Array.prototype.slice.call(arguments);
                return curried.apply(null, args.concat(more_args));
            };
        };
    """)
}

# Use function composition
$data_pipeline: @function.call("pipe", [
    "normalize_user_data",
    "calculate_statistics",
    "filter_and_sort"
], $raw_user_data)
```

### Error Handling and Recovery
```ini
[error_handling]
# Create error handling functions
$error_functions: {
    "safe_execute": @function.create("fn, args, fallback", """
        try {
            return @function.call(fn, args);
        } catch (error) {
            return fallback || {
                'error': true,
                'message': error.message,
                'fallback_value': null
            };
        }
    """),
    
    "retry": @function.create("fn, args, max_attempts, delay", """
        var attempts = 0;
        var last_error = null;
        
        while (attempts < max_attempts) {
            try {
                return @function.call(fn, args);
            } catch (error) {
                last_error = error;
                attempts++;
                if (attempts < max_attempts) {
                    @system.sleep(delay || 1000);
                }
            }
        }
        
        return {
            'error': true,
            'message': 'Max retry attempts exceeded',
            'last_error': last_error.message,
            'attempts': attempts
        };
    """),
    
    "validate_and_execute": @function.create("fn, args, validator", """
        var validation = @function.call(validator, args);
        if (validation.valid) {
            return @function.call(fn, args);
        } else {
            return {
                'error': true,
                'message': 'Validation failed',
                'errors': validation.errors
            };
        }
    """)
}
```

## 🛡️ Security & Performance Notes
- **Function validation:** Always validate function inputs and outputs
- **Code injection:** Prevent code injection by validating function parameters
- **Resource limits:** Set appropriate limits for function execution time and memory
- **Error handling:** Implement proper error handling for function failures
- **Function isolation:** Ensure functions don't have access to sensitive data
- **Performance monitoring:** Monitor function execution times and resource usage

## 🐞 Troubleshooting
- **Function not found:** Check function names and ensure they're properly defined
- **Parameter errors:** Validate function parameters and their types
- **Execution errors:** Implement proper error handling and logging
- **Performance issues:** Optimize function logic and implement caching
- **Memory problems:** Monitor function memory usage and implement cleanup

## 💡 Best Practices
- **Use meaningful names:** Create descriptive function names that explain their purpose
- **Validate inputs:** Always validate function parameters before processing
- **Handle errors:** Implement proper error handling and recovery mechanisms
- **Document functions:** Document complex functions with clear descriptions
- **Test thoroughly:** Test functions with various inputs and edge cases
- **Optimize performance:** Use efficient algorithms and implement caching where appropriate

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@string Function](030-at-string-function-bash.md)
- [@math Function](032-at-math-function-bash.md)
- [Custom Functions](111-custom-functions-bash.md)
- [Error Handling](112-error-handling-bash.md)

---

**Master @function in TuskLang and wield the power of dynamic logic in your configurations. ⚙️** 
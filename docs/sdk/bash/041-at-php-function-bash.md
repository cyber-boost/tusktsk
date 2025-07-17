# 🐘 TuskLang Bash @php Function Guide

**"We don't bow to any king" – PHP is your configuration's execution engine.**

The @php function in TuskLang is your PHP integration powerhouse, enabling dynamic PHP code execution, function calls, and complex logic directly within your configuration files. Whether you're processing data, calling PHP libraries, or implementing complex algorithms, @php provides the power and flexibility to leverage PHP's extensive ecosystem.

## 🎯 What is @php?
The @php function provides PHP code execution in TuskLang. It offers:
- **PHP code execution** - Execute PHP code and return results
- **Function calls** - Call PHP functions and methods
- **Library integration** - Use PHP libraries and frameworks
- **Complex logic** - Implement sophisticated algorithms and data processing
- **Seamless integration** - Bridge between TuskLang and PHP ecosystems

## 📝 Basic @php Syntax

### Simple PHP Execution
```ini
[simple_php]
# Execute basic PHP code
current_time: @php("date('Y-m-d H:i:s')")
memory_usage: @php("memory_get_usage(true)")
php_version: @php("PHP_VERSION")
random_number: @php("rand(1, 100)")
```

### PHP Function Calls
```ini
[php_functions]
# Call PHP built-in functions
array_count: @php("count([1, 2, 3, 4, 5])")
string_length: @php("strlen('Hello TuskLang')")
json_encode: @php("json_encode(['name' => 'TuskLang', 'version' => '2.1.0'])")
hash_value: @php("hash('sha256', 'TuskLang is awesome')")
```

### Complex PHP Logic
```ini
[complex_php]
# Execute complex PHP logic
$php_code: """
$data = ['a' => 1, 'b' => 2, 'c' => 3];
$result = array_map(function($value) {
    return $value * 2;
}, $data);
return json_encode($result);
"""
processed_data: @php($php_code)

# PHP with variables
$user_data: {"name": "Alice", "age": 30}
php_processing: @php(@string.format("""
$data = json_decode('{json_data}', true);
$data['processed'] = true;
$data['timestamp'] = date('Y-m-d H:i:s');
return json_encode($data);
""", {"json_data": @string.json_encode($user_data)}))
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > php-quickstart.tsk << 'EOF'
[php_demo]
# Basic PHP execution
current_time: @php("date('Y-m-d H:i:s')")
memory_usage: @php("memory_get_usage(true)")
php_version: @php("PHP_VERSION")

[php_functions]
# PHP function calls
array_sum: @php("array_sum([1, 2, 3, 4, 5])")
string_upper: @php("strtoupper('hello tusklang')")
json_data: @php("json_encode(['app' => 'TuskLang', 'version' => '2.1.0'])")

[complex_processing]
# Complex PHP processing
$data: [1, 2, 3, 4, 5]
processed_data: @php(@string.format("""
$input = json_decode('{input}', true);
$result = array_map(function($n) {
    return $n * $n + 1;
}, $input);
return json_encode($result);
""", {"input": @string.json_encode($data)}))

[php_libraries]
# Use PHP libraries (if available)
curl_info: @php("""
if (function_exists('curl_version')) {
    $info = curl_version();
    return json_encode($info);
} else {
    return 'cURL not available';
}
""")
EOF

config=$(tusk_parse php-quickstart.tsk)

echo "=== PHP Demo ==="
echo "Current Time: $(tusk_get "$config" php_demo.current_time)"
echo "Memory Usage: $(tusk_get "$config" php_demo.memory_usage)"
echo "PHP Version: $(tusk_get "$config" php_demo.php_version)"

echo ""
echo "=== PHP Functions ==="
echo "Array Sum: $(tusk_get "$config" php_functions.array_sum)"
echo "String Upper: $(tusk_get "$config" php_functions.string_upper)"
echo "JSON Data: $(tusk_get "$config" php_functions.json_data)"

echo ""
echo "=== Complex Processing ==="
echo "Processed Data: $(tusk_get "$config" complex_processing.processed_data)"

echo ""
echo "=== PHP Libraries ==="
echo "cURL Info: $(tusk_get "$config" php_libraries.curl_info)"
```

## 🔗 Real-World Use Cases

### 1. Data Processing and Transformation
```ini
[data_processing]
# Process complex data with PHP
$raw_data: @query("SELECT * FROM users WHERE active = 1")
processed_users: @php(@string.format("""
$users = json_decode('{users}', true);
$processed = [];
foreach ($users as $user) {
    $processed[] = [
        'id' => $user['id'],
        'name' => ucwords(strtolower($user['name'])),
        'email' => strtolower($user['email']),
        'age' => date('Y') - date('Y', strtotime($user['birth_date'])),
        'status' => $user['active'] ? 'active' : 'inactive'
    ];
}
return json_encode($processed);
""", {"users": @string.json_encode($raw_data)}))

# Advanced data filtering
filtered_data: @php(@string.format("""
$data = json_decode('{data}', true);
$filtered = array_filter($data, function($item) {
    return $item['age'] >= 18 && $item['status'] === 'active';
});
return json_encode(array_values($filtered));
""", {"data": @string.json_encode($processed_users)}))
```

### 2. File and Image Processing
```ini
[file_processing]
# Process files with PHP
file_info: @php(@string.format("""
$file = '{file_path}';
if (file_exists($file)) {
    $info = [
        'size' => filesize($file),
        'modified' => date('Y-m-d H:i:s', filemtime($file)),
        'permissions' => substr(sprintf('%o', fileperms($file)), -4),
        'hash' => hash_file('sha256', $file)
    ];
    return json_encode($info);
} else {
    return json_encode(['error' => 'File not found']);
}
""", {"file_path": "/etc/app/config.tsk"}))

# Image processing (if GD extension is available)
image_processing: @php("""
if (extension_loaded('gd')) {
    $image_path = '/var/www/uploads/image.jpg';
    if (file_exists($image_path)) {
        $info = getimagesize($image_path);
        return json_encode([
            'width' => $info[0],
            'height' => $info[1],
            'type' => $info[2],
            'mime' => $info['mime']
        ]);
    }
}
return json_encode(['error' => 'Image processing not available']);
""")
```

### 3. API Integration and HTTP Requests
```ini
[api_integration]
# Make HTTP requests with PHP
api_response: @php(@string.format("""
$url = '{api_url}';
$headers = [
    'Authorization: Bearer {api_key}',
    'Content-Type: application/json'
];

$context = stream_context_create([
    'http' => [
        'method' => 'GET',
        'header' => implode("\r\n", $headers),
        'timeout' => 30
    ]
]);

$response = file_get_contents($url, false, $context);
if ($response === false) {
    return json_encode(['error' => 'Request failed']);
}

return $response;
""", {
    "api_url": "https://api.example.com/data",
    "api_key": @env("API_KEY")
}))

# Process API response
processed_response: @php(@string.format("""
$response = json_decode('{response}', true);
if (isset($response['data'])) {
    $processed = [];
    foreach ($response['data'] as $item) {
        $processed[] = [
            'id' => $item['id'],
            'name' => htmlspecialchars($item['name']),
            'created' => date('Y-m-d H:i:s', strtotime($item['created_at']))
        ];
    }
    return json_encode($processed);
}
return json_encode(['error' => 'Invalid response format']);
""", {"response": $api_response}))
```

### 4. Complex Business Logic
```ini
[business_logic]
# Implement complex business rules
$order_data: @query("SELECT * FROM orders WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 DAY)")
order_analysis: @php(@string.format("""
$orders = json_decode('{orders}', true);
$analysis = [
    'total_orders' => count($orders),
    'total_revenue' => 0,
    'average_order' => 0,
    'top_products' => [],
    'hourly_distribution' => array_fill(0, 24, 0)
];

$product_counts = [];
foreach ($orders as $order) {
    $analysis['total_revenue'] += $order['amount'];
    $hour = (int)date('H', strtotime($order['created_at']));
    $analysis['hourly_distribution'][$hour]++;
    
    foreach ($order['items'] as $item) {
        $product_counts[$item['product_id']] = ($product_counts[$item['product_id']] ?? 0) + $item['quantity'];
    }
}

$analysis['average_order'] = $analysis['total_orders'] > 0 ? $analysis['total_revenue'] / $analysis['total_orders'] : 0;
arsort($product_counts);
$analysis['top_products'] = array_slice($product_counts, 0, 5, true);

return json_encode($analysis);
""", {"orders": @string.json_encode($order_data)}))

# Calculate discounts and promotions
discount_calculation: @php(@string.format("""
$order = json_decode('{order}', true);
$discount = 0;

// Loyalty discount
if ($order['customer_loyalty_years'] >= 2) {
    $discount += 0.05; // 5% loyalty discount
}

// Bulk discount
if ($order['total_items'] >= 10) {
    $discount += 0.10; // 10% bulk discount
}

// Seasonal discount
$month = (int)date('n');
if ($month === 12) { // December
    $discount += 0.15; // 15% holiday discount
}

// Apply maximum discount cap
$discount = min($discount, 0.25); // Max 25% total discount

$final_amount = $order['subtotal'] * (1 - $discount);
return json_encode([
    'original_amount' => $order['subtotal'],
    'discount_percentage' => $discount * 100,
    'discount_amount' => $order['subtotal'] * $discount,
    'final_amount' => $final_amount
]);
""", {"order": @string.json_encode({"subtotal": 100, "customer_loyalty_years": 3, "total_items": 15})}))
```

## 🧠 Advanced @php Patterns

### PHP Class Integration
```ini
[php_classes]
# Use PHP classes and objects
class_processing: @php("""
class DataProcessor {
    private $data;
    
    public function __construct($data) {
        $this->data = $data;
    }
    
    public function process() {
        $result = [];
        foreach ($this->data as $item) {
            $result[] = [
                'id' => $item['id'],
                'processed_name' => ucwords(strtolower($item['name'])),
                'hash' => hash('sha256', $item['email'])
            ];
        }
        return $result;
    }
}

$data = [['id' => 1, 'name' => 'john doe', 'email' => 'john@example.com']];
$processor = new DataProcessor($data);
return json_encode($processor->process());
""")
```

### Error Handling and Validation
```ini
[error_handling]
# Robust error handling in PHP
safe_processing: @php(@string.format("""
try {
    $data = json_decode('{data}', true);
    if (json_last_error() !== JSON_ERROR_NONE) {
        throw new Exception('Invalid JSON: ' . json_last_error_msg());
    }
    
    if (!is_array($data)) {
        throw new Exception('Data must be an array');
    }
    
    $result = [];
    foreach ($data as $item) {
        if (!isset($item['id']) || !isset($item['name'])) {
            continue; // Skip invalid items
        }
        
        $result[] = [
            'id' => (int)$item['id'],
            'name' => trim($item['name']),
            'valid' => true
        ];
    }
    
    return json_encode([
        'success' => true,
        'data' => $result,
        'count' => count($result)
    ]);
    
} catch (Exception $e) {
    return json_encode([
        'success' => false,
        'error' => $e->getMessage(),
        'data' => []
    ]);
}
""", {"data": '[{"id": 1, "name": "Alice"}, {"id": "invalid", "name": "Bob"}]'}))
```

### Performance Optimization
```ini
[performance_optimization]
# Optimized PHP processing
optimized_processing: @php(@string.format("""
$start_time = microtime(true);
$data = json_decode('{data}', true);

// Use generators for memory efficiency
function processData($data) {
    foreach ($data as $item) {
        yield [
            'id' => $item['id'],
            'processed' => hash('md5', $item['name'] . $item['email']),
            'timestamp' => time()
        ];
    }
}

$result = iterator_to_array(processData($data));
$execution_time = microtime(true) - $start_time;

return json_encode([
    'data' => $result,
    'execution_time' => round($execution_time, 4),
    'memory_usage' => memory_get_usage(true)
]);
""", {"data": @string.json_encode(@array.range(1, 1000))}))
```

## 🛡️ Security & Performance Notes
- **Code injection:** Never execute user-provided PHP code without validation
- **Memory usage:** Monitor memory consumption for large data processing
- **Execution time:** Set appropriate timeouts for PHP execution
- **Error handling:** Implement proper error handling and logging
- **Input validation:** Validate all inputs before passing to PHP
- **Security context:** Run PHP in appropriate security context

## 🐞 Troubleshooting
- **PHP not available:** Ensure PHP is installed and accessible
- **Memory limits:** Adjust PHP memory limits for large operations
- **Execution timeouts:** Configure appropriate timeout values
- **Error handling:** Implement proper error catching and reporting
- **Performance issues:** Optimize PHP code and use appropriate data structures

## 💡 Best Practices
- **Validate inputs:** Always validate data before PHP processing
- **Handle errors:** Implement comprehensive error handling
- **Optimize performance:** Use efficient PHP functions and data structures
- **Security first:** Never execute untrusted PHP code
- **Document code:** Document complex PHP logic and functions
- **Test thoroughly:** Test PHP code with various input scenarios

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@string Function](030-at-string-function-bash.md)
- [@query Function](027-at-query-function-bash.md)
- [Error Handling](062-error-handling-bash.md)
- [Performance Optimization](095-performance-optimization-bash.md)

---

**Master @php in TuskLang and unleash the power of PHP in your configurations. 🐘** 
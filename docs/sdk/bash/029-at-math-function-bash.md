# 🧮 TuskLang Bash @math Function Guide

**"We don't bow to any king" – Mathematics is your configuration's calculator.**

The @math function in TuskLang is your computational powerhouse, enabling dynamic calculations, formulas, and mathematical operations directly within your configuration files. Whether you're calculating resource allocations, performing statistical analysis, or building dynamic scaling systems, @math provides the precision and power to make your configurations truly intelligent.

## 🎯 What is @math?
The @math function performs mathematical operations and calculations in TuskLang. It provides:
- **Basic arithmetic** - Addition, subtraction, multiplication, division
- **Advanced functions** - Logarithms, trigonometry, statistical functions
- **Dynamic calculations** - Real-time computation based on variables
- **Precision** - High-precision floating-point arithmetic
- **Integration** - Seamless integration with other @ operators

## 📝 Basic @math Syntax

### Basic Arithmetic
```ini
[basic]
# Simple arithmetic operations
sum: @math(10 + 5)
difference: @math(20 - 7)
product: @math(6 * 8)
quotient: @math(100 / 4)
remainder: @math(17 % 5)
power: @math(2 ^ 10)
```

### Variable-Based Calculations
```ini
[variables]
# Use variables in calculations
$base_value: 100
$multiplier: 1.5
$tax_rate: 0.08

calculated_value: @math($base_value * $multiplier)
with_tax: @math($calculated_value * (1 + $tax_rate))
discount: @math($base_value * 0.15)
final_price: @math($base_value - $discount + ($base_value - $discount) * $tax_rate)
```

### Complex Expressions
```ini
[complex]
# Complex mathematical expressions
formula: @math((a + b) * c / d - e)
quadratic: @math((-b + sqrt(b^2 - 4*a*c)) / (2*a))
percentage: @math((part / total) * 100)
compound_interest: @math(principal * (1 + rate)^time)
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > math-quickstart.tsk << 'EOF'
[basic_calculations]
addition: @math(15 + 25)
subtraction: @math(50 - 12)
multiplication: @math(8 * 7)
division: @math(100 / 4)
modulo: @math(23 % 7)
exponentiation: @math(2 ^ 8)

[resource_calculations]
total_memory: @math(16 * 1024)  # 16GB in MB
available_memory: @math($total_memory * 0.8)  # 80% of total
cpu_cores: @math(4 * 2)  # 4 cores, 2 threads each
max_connections: @math($cpu_cores * 100)

[performance_metrics]
response_time_ms: @math(150 + 25)
throughput_rps: @math(1000 / $response_time_ms * 1000)
error_rate_percent: @math((5 / 1000) * 100)
success_rate_percent: @math(100 - $error_rate_percent)

[scaling_calculations]
base_load: @math(100)
peak_multiplier: @math(3.5)
peak_load: @math($base_load * $peak_multiplier)
buffer_capacity: @math($peak_load * 1.2)
EOF

config=$(tusk_parse math-quickstart.tsk)

echo "=== Basic Calculations ==="
echo "Addition: $(tusk_get "$config" basic_calculations.addition)"
echo "Subtraction: $(tusk_get "$config" basic_calculations.subtraction)"
echo "Multiplication: $(tusk_get "$config" basic_calculations.multiplication)"
echo "Division: $(tusk_get "$config" basic_calculations.division)"
echo "Modulo: $(tusk_get "$config" basic_calculations.modulo)"
echo "Exponentiation: $(tusk_get "$config" basic_calculations.exponentiation)"

echo ""
echo "=== Resource Calculations ==="
echo "Total Memory (MB): $(tusk_get "$config" resource_calculations.total_memory)"
echo "Available Memory (MB): $(tusk_get "$config" resource_calculations.available_memory)"
echo "CPU Cores: $(tusk_get "$config" resource_calculations.cpu_cores)"
echo "Max Connections: $(tusk_get "$config" resource_calculations.max_connections)"

echo ""
echo "=== Performance Metrics ==="
echo "Response Time (ms): $(tusk_get "$config" performance_metrics.response_time_ms)"
echo "Throughput (RPS): $(tusk_get "$config" performance_metrics.throughput_rps)"
echo "Error Rate (%): $(tusk_get "$config" performance_metrics.error_rate_percent)"
echo "Success Rate (%): $(tusk_get "$config" performance_metrics.success_rate_percent)"

echo ""
echo "=== Scaling Calculations ==="
echo "Base Load: $(tusk_get "$config" scaling_calculations.base_load)"
echo "Peak Load: $(tusk_get "$config" scaling_calculations.peak_load)"
echo "Buffer Capacity: $(tusk_get "$config" scaling_calculations.buffer_capacity)"
```

## 🔗 Real-World Use Cases

### 1. Resource Allocation and Scaling
```ini
[resource_allocation]
# Dynamic resource calculation based on system metrics
cpu_cores: @shell("nproc")
memory_gb: @shell("free -g | awk 'NR==2{print $2}'")
disk_gb: @shell("df -BG / | awk 'NR==2{print $2}' | sed 's/G//'")

# Calculate optimal resource allocation
max_workers: @math($cpu_cores * 2)
memory_per_worker: @math($memory_gb * 1024 / $max_workers)
disk_buffer: @math($disk_gb * 0.1)  # 10% buffer
cache_size: @math($memory_gb * 1024 * 0.3)  # 30% for cache

# Auto-scaling thresholds
cpu_threshold: @math(80)  # 80% CPU usage
memory_threshold: @math(85)  # 85% memory usage
scale_up_factor: @math(1.5)  # 50% increase
scale_down_factor: @math(0.8)  # 20% decrease
```

### 2. Performance Monitoring and Analytics
```ini
[performance_analytics]
# Real-time performance calculations
request_count: @query("SELECT COUNT(*) FROM api_requests WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")
error_count: @query("SELECT COUNT(*) FROM api_requests WHERE status_code >= 400 AND created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")
avg_response_time: @query("SELECT AVG(response_time) FROM api_requests WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR)")

# Calculate performance metrics
error_rate: @math(($error_count / $request_count) * 100)
success_rate: @math(100 - $error_rate)
requests_per_minute: @math($request_count / 60)
throughput: @math(1000 / $avg_response_time)  # requests per second

# Performance scoring
performance_score: @math(($success_rate * 0.4) + (min($throughput, 100) * 0.3) + (max(0, 100 - $avg_response_time) * 0.3))
health_status: @if($performance_score >= 80, "excellent", @if($performance_score >= 60, "good", @if($performance_score >= 40, "fair", "poor")))
```

### 3. Financial Calculations
```ini
[financial_calculations]
# Financial formulas and calculations
principal: @env("LOAN_AMOUNT", "10000")
annual_rate: @env("INTEREST_RATE", "0.05")
loan_years: @env("LOAN_TERM", "5")
monthly_payments: @env("MONTHLY_PAYMENTS", "12")

# Calculate loan payments
monthly_rate: @math($annual_rate / $monthly_payments)
total_payments: @math($loan_years * $monthly_payments)
monthly_payment: @math($principal * ($monthly_rate * (1 + $monthly_rate)^$total_payments) / ((1 + $monthly_rate)^$total_payments - 1))
total_interest: @math($monthly_payment * $total_payments - $principal)
total_cost: @math($principal + $total_interest)

# Investment calculations
initial_investment: @env("INVESTMENT_AMOUNT", "5000")
annual_return: @env("ANNUAL_RETURN_RATE", "0.07")
investment_years: @env("INVESTMENT_PERIOD", "10")
future_value: @math($initial_investment * (1 + $annual_return)^$investment_years)
total_return: @math($future_value - $initial_investment)
return_percentage: @math(($total_return / $initial_investment) * 100)
```

### 4. Statistical Analysis
```ini
[statistical_analysis]
# Statistical calculations for data analysis
sample_size: @query("SELECT COUNT(*) FROM user_activity WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)")
mean_value: @query("SELECT AVG(activity_duration) FROM user_activity WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)")
min_value: @query("SELECT MIN(activity_duration) FROM user_activity WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)")
max_value: @query("SELECT MAX(activity_duration) FROM user_activity WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)")

# Calculate statistical measures
range: @math($max_value - $min_value)
median_estimate: @math(($min_value + $max_value) / 2)
standard_deviation: @math(sqrt(($max_value - $mean_value)^2 + ($min_value - $mean_value)^2) / 2)
coefficient_of_variation: @math(($standard_deviation / $mean_value) * 100)

# Confidence intervals (simplified)
margin_of_error: @math(1.96 * $standard_deviation / sqrt($sample_size))
confidence_interval_lower: @math($mean_value - $margin_of_error)
confidence_interval_upper: @math($mean_value + $margin_of_error)
```

## 🧠 Advanced @math Patterns

### Conditional Calculations
```bash
#!/bin/bash
source tusk-bash.sh

cat > conditional-math.tsk << 'EOF'
[conditional_calculations]
# Environment-based calculations
environment: @env("APP_ENV", "development")
base_load: @env("BASE_LOAD", "100")

# Conditional scaling based on environment
production_multiplier: @math(3.0)
staging_multiplier: @math(1.5)
development_multiplier: @math(1.0)

# Dynamic load calculation
load_multiplier: @if($environment == "production", $production_multiplier, 
    @if($environment == "staging", $staging_multiplier, $development_multiplier))
scaled_load: @math($base_load * $load_multiplier)

# Time-based calculations
current_hour: @date("H")
peak_hour_multiplier: @math(2.0)
off_peak_multiplier: @math(0.5)

# Time-based scaling
time_multiplier: @if($current_hour >= 9 && $current_hour <= 17, $peak_hour_multiplier, $off_peak_multiplier)
final_load: @math($scaled_load * $time_multiplier)
EOF

config=$(tusk_parse conditional-math.tsk)
echo "Environment: $(tusk_get "$config" conditional_calculations.environment)"
echo "Base Load: $(tusk_get "$config" conditional_calculations.base_load)"
echo "Load Multiplier: $(tusk_get "$config" conditional_calculations.load_multiplier)"
echo "Scaled Load: $(tusk_get "$config" conditional_calculations.scaled_load)"
echo "Final Load: $(tusk_get "$config" conditional_calculations.final_load)"
```

### Complex Mathematical Functions
```ini
[advanced_functions]
# Trigonometric functions
angle_radians: @math(45 * pi / 180)
sine_value: @math(sin($angle_radians))
cosine_value: @math(cos($angle_radians))
tangent_value: @math(tan($angle_radians))

# Logarithmic functions
natural_log: @math(ln(100))
log_base_10: @math(log(100))
log_base_2: @math(log(100) / log(2))

# Exponential functions
e_power: @math(exp(1))
power_function: @math(pow(2, 10))
square_root: @math(sqrt(144))
cube_root: @math(pow(27, 1/3))

# Statistical functions
absolute_value: @math(abs(-15))
floor_function: @math(floor(3.7))
ceiling_function: @math(ceil(3.2))
round_function: @math(round(3.6))
```

### Performance Optimization
```ini
[optimization]
# Cache expensive calculations
expensive_calculation: @cache("5m", @math(complex_formula_with_multiple_operations))

# Batch calculations
batch_size: @math(1000)
total_items: @query("SELECT COUNT(*) FROM large_table")
total_batches: @math(ceil($total_items / $batch_size))
processing_time_per_batch: @math(30)  # seconds
total_processing_time: @math($total_batches * $processing_time_per_batch)

# Memory optimization
data_size_mb: @math(1024)
compression_ratio: @math(0.3)
compressed_size: @math($data_size_mb * $compression_ratio)
memory_savings: @math($data_size_mb - $compressed_size)
savings_percentage: @math(($memory_savings / $data_size_mb) * 100)
```

## 🛡️ Security & Performance Notes
- **Input validation:** Always validate mathematical inputs to prevent calculation errors
- **Division by zero:** Handle division by zero cases to prevent runtime errors
- **Precision:** Be aware of floating-point precision limitations for financial calculations
- **Performance:** Cache expensive mathematical operations to improve performance
- **Overflow:** Monitor for integer overflow in large calculations

## 🐞 Troubleshooting
- **Division by zero:** Check for zero denominators before division operations
- **Invalid inputs:** Validate that mathematical inputs are numeric
- **Precision issues:** Use appropriate precision for financial calculations
- **Performance problems:** Cache expensive calculations and optimize formulas
- **Overflow errors:** Use appropriate data types for large numbers

## 💡 Best Practices
- **Validate inputs:** Always validate mathematical inputs before calculations
- **Handle edge cases:** Account for division by zero, negative numbers, etc.
- **Use appropriate precision:** Choose the right precision for your use case
- **Cache expensive operations:** Cache complex calculations to improve performance
- **Document formulas:** Document complex mathematical formulas for maintainability
- **Test thoroughly:** Test mathematical operations with various inputs and edge cases

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@date Function](028-at-date-function-bash.md)
- [Numbers](009-numbers-bash.md)
- [Conditional Logic](060-conditional-logic-bash.md)

---

**Master @math in TuskLang and bring computational power to your configurations. 🧮** 
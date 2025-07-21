# 🔢 TuskLang Bash Numbers Guide

**"We don't bow to any king" - Master numerical operations in TuskLang**

Numbers are essential for configuration, calculations, and system monitoring in TuskLang. From simple integers to complex mathematical operations, TuskLang provides powerful numerical capabilities that integrate seamlessly with your Bash scripts.

## 🎯 Number Types

### Basic Numbers

```bash
#!/bin/bash
source tusk-bash.sh

cat > basic-numbers.tsk << 'EOF'
[numbers]
# Integers
port: 8080
workers: 4
timeout: 30
max_connections: 100

# Floating point numbers
cpu_threshold: 80.5
memory_limit: 512.0
load_average: 2.75
pi: 3.14159

# Scientific notation
large_number: 1.5e6
small_number: 2.3e-4
avogadro: 6.022e23

# Negative numbers
negative_temp: -15.5
negative_port: -1
EOF

config=$(tusk_parse basic-numbers.tsk)
echo "=== Basic Numbers ==="
echo "Port: $(tusk_get "$config" numbers.port)"
echo "CPU Threshold: $(tusk_get "$config" numbers.cpu_threshold)%"
echo "Large Number: $(tusk_get "$config" numbers.large_number)"
echo "Negative Temp: $(tusk_get "$config" numbers.negative_temp)°C"
```

### Dynamic Numbers

```bash
#!/bin/bash
source tusk-bash.sh

cat > dynamic-numbers.tsk << 'EOF'
[dynamic]
# Environment-based numbers
$port: @env("PORT", "8080")
$workers: @env("WORKERS", "4")
$timeout: @env("TIMEOUT", "30")

# System-based numbers
$cpu_cores: @shell("nproc")
$memory_gb: @shell("free -g | grep Mem | awk '{print $2}'")
$disk_gb: @shell("df -BG / | tail -1 | awk '{print $4}' | sed 's/G//'")

# Calculated numbers
total_memory_mb: $memory_gb * 1024
available_disk_percent: @shell("df / | tail -1 | awk '{print $5}' | sed 's/%//'")

# Conditional numbers
$environment: @env("APP_ENV", "development")
server_port: @if($environment == "production", 80, 8080)
worker_count: @if($environment == "production", 4, 1)
EOF

config=$(tusk_parse dynamic-numbers.tsk)
echo "=== Dynamic Numbers ==="
echo "Port: $(tusk_get "$config" dynamic.port)"
echo "CPU Cores: $(tusk_get "$config" dynamic.cpu_cores)"
echo "Memory GB: $(tusk_get "$config" dynamic.memory_gb)"
echo "Total Memory MB: $(tusk_get "$config" dynamic.total_memory_mb)"
echo "Server Port: $(tusk_get "$config" dynamic.server_port)"
```

## 🔧 Mathematical Operations

### Basic Arithmetic

```bash
#!/bin/bash
source tusk-bash.sh

cat > arithmetic.tsk << 'EOF'
[arithmetic]
# Basic operations
$base: 100
$multiplier: 2.5
$divisor: 4

# Addition and subtraction
sum: $base + 50
difference: $base - 25

# Multiplication and division
product: $base * $multiplier
quotient: $base / $divisor

# Complex calculations
$memory_gb: 8
$cpu_cores: 4
memory_per_core: $memory_gb / $cpu_cores
total_capacity: $memory_gb * $cpu_cores

# Percentage calculations
$total_users: 1000
$active_users: 750
active_percentage: ($active_users / $total_users) * 100

# Rounding
rounded_percentage: @math.round(active_percentage)
ceiling_value: @math.ceil(active_percentage)
floor_value: @math.floor(active_percentage)
EOF

config=$(tusk_parse arithmetic.tsk)
echo "=== Arithmetic Operations ==="
echo "Sum: $(tusk_get "$config" arithmetic.sum)"
echo "Product: $(tusk_get "$config" arithmetic.product)"
echo "Memory per Core: $(tusk_get "$config" arithmetic.memory_per_core)GB"
echo "Active Percentage: $(tusk_get "$config" arithmetic.active_percentage)%"
echo "Rounded: $(tusk_get "$config" arithmetic.rounded_percentage)%"
```

### Advanced Mathematics

```bash
#!/bin/bash
source tusk-bash.sh

cat > advanced-math.tsk << 'EOF'
[advanced]
# Power and roots
$base: 2
$exponent: 10
power_result: @math.pow($base, $exponent)
square_root: @math.sqrt(100)
cube_root: @math.cbrt(27)

# Trigonometric functions
$angle_degrees: 45
$angle_radians: @math.radians($angle_degrees)
sine: @math.sin($angle_radians)
cosine: @math.cos($angle_radians)
tangent: @math.tan($angle_radians)

# Logarithmic functions
$value: 100
natural_log: @math.ln($value)
log_base_10: @math.log10($value)
log_base_2: @math.log2($value)

# Statistical functions
$numbers: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
average: @math.avg($numbers)
sum: @math.sum($numbers)
min_value: @math.min($numbers)
max_value: @math.max($numbers)
EOF

config=$(tusk_parse advanced-math.tsk)
echo "=== Advanced Mathematics ==="
echo "Power Result: $(tusk_get "$config" advanced.power_result)"
echo "Square Root: $(tusk_get "$config" advanced.square_root)"
echo "Sine: $(tusk_get "$config" advanced.sine)"
echo "Natural Log: $(tusk_get "$config" advanced.natural_log)"
echo "Average: $(tusk_get "$config" advanced.average)"
```

## 📊 System Monitoring Numbers

### Performance Metrics

```bash
#!/bin/bash
source tusk-bash.sh

cat > performance-metrics.tsk << 'EOF'
[metrics]
# CPU metrics
$cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
$cpu_load: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")

# Memory metrics
$total_memory: @shell("free | grep Mem | awk '{print $2}'")
$used_memory: @shell("free | grep Mem | awk '{print $3}'")
$free_memory: @shell("free | grep Mem | awk '{print $4}'")
memory_usage_percent: ($used_memory / $total_memory) * 100

# Disk metrics
$total_disk: @shell("df / | tail -1 | awk '{print $2}'")
$used_disk: @shell("df / | tail -1 | awk '{print $3}'")
$available_disk: @shell("df / | tail -1 | awk '{print $4}'")
disk_usage_percent: ($used_disk / $total_disk) * 100

# Network metrics
$network_rx: @shell("cat /proc/net/dev | grep eth0 | awk '{print $2}'")
$network_tx: @shell("cat /proc/net/dev | grep eth0 | awk '{print $10}'")

# Process metrics
$process_count: @shell("ps aux | wc -l")
$thread_count: @shell("ps -eLf | wc -l")
EOF

config=$(tusk_parse performance-metrics.tsk)
echo "=== Performance Metrics ==="
echo "CPU Usage: $(tusk_get "$config" metrics.cpu_usage)%"
echo "CPU Load: $(tusk_get "$config" metrics.cpu_load)"
echo "Memory Usage: $(tusk_get "$config" metrics.memory_usage_percent)%"
echo "Disk Usage: $(tusk_get "$config" metrics.disk_usage_percent)%"
echo "Process Count: $(tusk_get "$config" metrics.process_count)"
```

### Threshold Monitoring

```bash
#!/bin/bash
source tusk-bash.sh

cat > threshold-monitoring.tsk << 'EOF'
[thresholds]
# Define thresholds
$cpu_threshold: 80
$memory_threshold: 85
$disk_threshold: 90

# Current metrics
$current_cpu: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
$current_memory: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")
$current_disk: @shell("df / | tail -1 | awk '{print $5}' | sed 's/%//'")

# Threshold checks
cpu_alert: $current_cpu > $cpu_threshold
memory_alert: $current_memory > $memory_threshold
disk_alert: $current_disk > $disk_threshold

# Severity levels
cpu_severity: @if($current_cpu > 95, "critical",
                  @if($current_cpu > $cpu_threshold, "warning", "normal"))

memory_severity: @if($current_memory > 95, "critical",
                     @if($current_memory > $memory_threshold, "warning", "normal"))

disk_severity: @if($current_disk > 95, "critical",
                   @if($current_disk > $disk_threshold, "warning", "normal"))
EOF

config=$(tusk_parse threshold-monitoring.tsk)
echo "=== Threshold Monitoring ==="
echo "Current CPU: $(tusk_get "$config" thresholds.current_cpu)%"
echo "CPU Alert: $(tusk_get "$config" thresholds.cpu_alert)"
echo "CPU Severity: $(tusk_get "$config" thresholds.cpu_severity)"
echo "Memory Alert: $(tusk_get "$config" thresholds.memory_alert)"
echo "Disk Alert: $(tusk_get "$config" thresholds.disk_alert)"
```

## 🔄 Number Validation

### Range and Type Validation

```bash
#!/bin/bash
source tusk-bash.sh

cat > number-validation.tsk << 'EOF'
[validation]
# Type validation
@validate.type("port", "int")
@validate.type("timeout", "float")
@validate.type("workers", "int")

# Range validation
@validate.range("port", 1, 65535)
@validate.range("timeout", 1, 300)
@validate.range("workers", 1, 32)

# Custom validation
@validate.custom("port", "port > 0 && port < 65536")
@validate.custom("timeout", "timeout > 0 && timeout <= 300")

# Values with validation
port: @int(8080)
timeout: @float(30.5)
workers: @int(4)

# Percentage validation
$cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
is_valid_cpu: $cpu_usage >= 0 && $cpu_usage <= 100

# Memory validation
$memory_usage: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")
is_valid_memory: $memory_usage >= 0 && $memory_usage <= 100
EOF

config=$(tusk_parse number-validation.tsk)
echo "=== Number Validation ==="
echo "Port: $(tusk_get "$config" validation.port)"
echo "Timeout: $(tusk_get "$config" validation.timeout)"
echo "Workers: $(tusk_get "$config" validation.workers)"
echo "Valid CPU: $(tusk_get "$config" validation.is_valid_cpu)"
echo "Valid Memory: $(tusk_get "$config" validation.is_valid_memory)"
```

## 🎯 What You've Learned

In this numbers guide, you've mastered:

✅ **Number types** - Integers, floats, and scientific notation  
✅ **Mathematical operations** - Basic arithmetic and advanced functions  
✅ **System monitoring** - Performance metrics and threshold monitoring  
✅ **Number validation** - Type checking and range validation  
✅ **Dynamic calculations** - Environment-based and computed numbers  

## 🚀 Next Steps

Ready to explore more TuskLang features?

1. **Booleans** → [010-booleans-bash.md](010-booleans-bash.md)
2. **Arrays** → [012-arrays-bash.md](012-arrays-bash.md)
3. **Objects** → [013-objects-bash.md](013-objects-bash.md)

## 💡 Pro Tips

- **Use appropriate types** - Choose int vs float based on your needs
- **Validate ranges** - Always validate number ranges for safety
- **Monitor performance** - Use numbers for system monitoring
- **Calculate dynamically** - Leverage computed values for flexibility
- **Handle precision** - Be careful with floating-point arithmetic

---

**Master numerical operations in TuskLang! 🔢** 
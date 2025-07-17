# Bash Cheat Sheet - TuskLang SDK

## Installation & Setup

```bash
# Make executable and install globally
chmod +x tsk.sh
sudo cp tsk.sh /usr/local/bin/tsk

# Source in scripts
source ./tsk.sh
```

## Basic Syntax & Data Types

### TSK File Format
```bash
# Comments
# This is a comment

# Global variables
$app_name: "My App"
$version: "1.0.0"

# Sections
[section_name]
key = "value"
number = 42
boolean = true
null_value = null

# Arrays
items = ["one", "two", "three"]
numbers = [1, 2, 3]

# Objects
config = { "key" = "value", "nested" = { "deep" = true } }

# Multiline strings
description = """
This is a multiline
string with formatting
"""
```

### Assignment Operators
```bash
# Traditional equals
host = "localhost"

# Colon syntax
port: 8080
name: $app_name
```

## Variables & Operators

### Global Variables
```bash
# Declare global variables
$app_name: "My Application"
$version: "2.0.0"
$debug_mode: true

# Reference in sections
[config]
name: $app_name
version: $version
```

### Section Variables
```bash
[server]
local_var: "section-specific"
another_var: local_var  # References local_var
```

### Operators
```bash
# Date operator
created_at: @date("Y-m-d H:i:s")
today: @date("Y-m-d")

# Environment operator with defaults
api_key: @env("API_KEY", "default-key")
debug: @env("DEBUG", "false")

# Range syntax
port_range: 8000-9000
```

## Control Structures

### If/Else Conditions
```bash
# In fujsen functions
if [[ "$amount" <= 0 ]]; then
    echo "Invalid amount"
    return 1
fi

# With bc for floating point
if (( $(echo "$amount > 1000000" | bc -l) )); then
    echo "Amount too large"
fi
```

### Loops
```bash
# For loop with array
for item in "${ARRAY[@]}"; do
    echo "Processing: $item"
done

# While loop
while IFS= read -r line; do
    echo "Line: $line"
done < file.tsk
```

## Functions & Fujsen

### Basic Fujsen
```bash
[validation]
check_amount_fujsen = """
function validate(amount) {
  if (amount <= 0) return false;
  if (amount > 1000000) return false;
  return true;
}
"""
```

### Arrow Functions
```bash
[payment]
process_fujsen = """
(amount, recipient) => {
  if (amount <= 0) throw new Error("Invalid amount");
  return {
    success: true,
    amount: amount,
    recipient: recipient,
    id: 'tx_' + Date.now()
  };
}
"""
```

### Native Bash Functions
```bash
[helpers]
calculate_fee_fujsen = """
_fujsen_calculate_fee() {
    local amount=$1
    echo $(echo "scale=2; $amount * 0.025" | bc)
}
_fujsen_calculate_fee
"""
```

## Data Structures

### Arrays
```bash
# Define array
features: ["auth", "api", "cache"]

# Parse array manually
items=$(tsk_get data items)
if [[ "$items" =~ ^\[(.+)\]$ ]]; then
    IFS=',' read -ra ARRAY <<< "${BASH_REMATCH[1]}"
    for item in "${ARRAY[@]}"; do
        item="${item// /}"  # Remove spaces
        item="${item//\"/}" # Remove quotes
        echo "Item: $item"
    done
fi
```

### Objects
```bash
# Curly brace objects
database {
    type: "sqlite"
    file: "./test.db"
    timeout: 5000
    enabled: true
}

# Angle bracket objects
cache >
    driver: "memory"
    ttl: "5m"
    max_size: "100MB"
<

# Traditional objects
config = { "key" = "value", "nested" = { "deep" = true } }
```

### Nested Structures
```bash
complex_config {
    nested {
        deep: "value"
        number: 42
        boolean: true
    }
    array: [1, 2, 3]
}
```

## Common Built-in Functions

### Core Functions
```bash
# Parse TSK content
tsk_parse config.tsk
tsk_parse << 'EOF'
[test]
value = "hello"
EOF

# Get values
value=$(tsk_get section key)
all_keys=$(tsk_get section)

# Set values
tsk_set section key "new_value"

# Save/Load files
tsk_save output.tsk
tsk_load input.tsk
```

### Fujsen Execution
```bash
# Execute fujsen
tsk_execute_fujsen section key arg1 arg2

# Execute with context
tsk_execute_fujsen_with_context section key "var1=value1; var2=value2" arg1 arg2

# Get fujsen map
tsk_get_fujsen_map section

# Set fujsen dynamically
tsk_set_fujsen section key function_name
```

### Shell Storage
```bash
# Store data with shell format
tsk_store_with_shell "data" "metadata=value"

# Retrieve from shell
tsk_retrieve_from_shell shell_data

# Detect content type
tsk_detect_type data
```

## File I/O Operations

### Reading Files
```bash
# Parse from file
tsk_parse config.tsk

# Parse from stdin
cat config.tsk | tsk_parse

# Parse with error handling
if tsk_parse config.tsk 2>/dev/null; then
    echo "Parse successful"
else
    echo "Parse failed"
fi
```

### Writing Files
```bash
# Save to file
tsk_stringify > output.tsk
tsk_save output.tsk

# Generate TSK from script
{
    echo "[generated]"
    echo "timestamp = $(date +%s)"
    echo "hostname = \"$(hostname)\""
} | tsk_stringify
```

### Piping Operations
```bash
# Chain operations
cat config.tsk | tsk_parse | grep "storage"

# Parse and filter
echo '[test]' | tsk_parse | grep "value"
```

## Error Handling

### Validation
```bash
# Check if section exists
if [[ -z "$(tsk_get missing_section)" ]]; then
    echo "Section not found"
fi

# Validate fujsen execution
if ! tsk_execute_fujsen section invalid_key 2>/dev/null; then
    echo "Fujsen execution failed"
fi
```

### Error Checking
```bash
# Parse with error checking
if tsk_parse invalid.tsk 2>/dev/null; then
    echo "Parse successful"
else
    echo "Parse failed"
fi

# Check return codes
tsk_execute_fujsen section key || echo "Function failed"
```

## CLI Commands

### Basic Commands
```bash
# Parse and show all data
./tsk.sh parse config.tsk

# Get specific value
./tsk.sh get config.tsk section key

# Set value and output new TSK
./tsk.sh set section key value config.tsk > new_config.tsk

# Execute fujsen function
./tsk.sh fujsen contract.tsk section key arg1 arg2

# Convert to TSK format
./tsk.sh stringify config.tsk
```

### Advanced Commands
```bash
# Store with shell format
./tsk.sh shell-store "data" "metadata=value" config.tsk

# Retrieve from shell
./tsk.sh shell-retrieve shell_data config.tsk

# Parse from stdin
echo '[test]' | ./tsk.sh parse
```

## Key Libraries & Modules

### Math Functions
```bash
# Math helpers
tsk_min 10 5 20  # Returns minimum
tsk_max 10 5 20  # Returns maximum

# Floating point with bc
result=$(echo "scale=2; $amount * 0.025" | bc)
```

### JavaScript to Bash Conversion
```bash
# Basic conversions in fujsen
console.log -> echo
const/let/var -> local
Date.now() -> $(date +%s%3N)
Math.min -> tsk_min
Math.max -> tsk_max
true/false -> 1/0
===/!== -> ==/!=
```

### String Manipulation
```bash
# Trim whitespace
value="${value#"${value%%[![:space:]]*}"}"
value="${value%"${value##*[![:space:]]}"}"

# Remove quotes
value="${value//\"/}"

# Escape characters
value="${value//\\/\\\\}"
value="${value//\"/\\\"}"
```

## Performance Tips

### Optimization
```bash
# Cache parsed data
tsk_parse config.tsk
# Use multiple times without re-parsing

# Use native Bash for performance
# Avoid subshells when possible
local result=$(calculation)  # Instead of $(calculation)

# Precompile fujsen
# Functions are cached after first compilation
```

### Memory Management
```bash
# Clear global state
TSK_DATA=()
FUJSEN_CACHE=()
TSK_COMMENTS=()

# Use local variables
local temp_var="value"
```

## Common Patterns

### Configuration Management
```bash
#!/bin/bash
source ./tsk.sh

# Load configuration
tsk_parse app_config.tsk

# Access values
db_host=$(tsk_get database host)
api_timeout=$(tsk_get api timeout)

# Validate configuration
if [[ -z "$db_host" ]]; then
    echo "Database host not configured"
    exit 1
fi
```

### Smart Contracts
```bash
# DeFi liquidity pool example
[pool]
swap_fujsen = """
_fujsen_swap() {
    local amount_in=$1
    local token_in=$2
    local reserve_a=100000
    local reserve_b=50000
    
    local k=$(echo "$reserve_a * $reserve_b" | bc)
    
    if [[ "$token_in" == "FLEX" ]]; then
        local new_reserve_a=$(echo "$reserve_a + $amount_in" | bc)
        local new_reserve_b=$(echo "scale=2; $k / $new_reserve_a" | bc)
        local amount_out=$(echo "scale=2; $reserve_b - $new_reserve_b" | bc)
    else
        local new_reserve_b=$(echo "$reserve_b + $amount_in" | bc)
        local new_reserve_a=$(echo "scale=2; $k / $new_reserve_b" | bc)
        local amount_out=$(echo "scale=2; $reserve_a - $new_reserve_a" | bc)
    fi
    
    local fee=$(echo "scale=2; $amount_out * 0.003" | bc)
    local final_amount=$(echo "scale=2; $amount_out - $fee" | bc)
    
    echo "{\"amount_out\": $final_amount, \"fee\": $fee}"
}
_fujsen_swap
"""
```

### Validation Patterns
```bash
[validators]
amount_fujsen = """
_validate_amount() {
    local amount=$1
    
    # Check if number
    if ! [[ "$amount" =~ ^[0-9]+\.?[0-9]*$ ]]; then
        echo '{"valid": false, "error": "Not a number"}'
        return
    fi
    
    # Check range
    if (( $(echo "$amount <= 0" | bc -l) )); then
        echo '{"valid": false, "error": "Must be positive"}'
        return
    fi
    
    if (( $(echo "$amount > 1000000" | bc -l) )); then
        echo '{"valid": false, "error": "Exceeds maximum"}'
        return
    fi
    
    echo '{"valid": true}'
}
_validate_amount
"""
```

## Limitations & Considerations

### Known Limitations
```bash
# Array/Object parsing: Basic support only
# Complex nested structures need manual parsing

# JavaScript conversion: Limited patterns
# Not all JS features are supported

# Type detection: Pattern matching only
# No runtime type checking

# Floating point: Requires bc
# Not all systems have bc installed
```

### Best Practices
```bash
# Always use strict mode
set -euo pipefail

# Validate inputs
if [[ -z "$1" ]]; then
    echo "Usage: $0 <config_file>"
    exit 1
fi

# Handle errors gracefully
tsk_parse "$1" || {
    echo "Failed to parse $1"
    exit 1
}

# Use meaningful variable names
local config_section="database"
local config_key="host"
``` 
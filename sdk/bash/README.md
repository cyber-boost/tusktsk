# TSK Bash SDK

A powerful parser and generator for TSK (TuskLang Configuration) format with fujsen (function serialization) support, implemented in pure Bash.

## Installation

```bash
# Make the script executable
chmod +x tsk.sh

# Copy to your PATH
sudo cp tsk.sh /usr/local/bin/tsk

# Or source it in your scripts
source ./tsk.sh
```

## Basic Usage

### Command Line Interface

```bash
# Parse TSK file and show all data
./tsk.sh parse config.tsk

# Get specific value
./tsk.sh get config.tsk storage id
./tsk.sh get config.tsk storage tags

# Set value and output new TSK
./tsk.sh set database host "localhost" config.tsk > new_config.tsk

# Execute fujsen function
./tsk.sh fujsen contract.tsk payment process 100 "alice@example.com"

# Convert to TSK format
./tsk.sh stringify config.tsk
```

### Using as a Library

```bash
#!/bin/bash

# Source the TSK library
source ./tsk.sh

# Example TSK content
cat > config.tsk << 'EOF'
[storage]
id = "flex_123"
type = "image/jpeg"
tags = [ "sunset", "beach" ]

[metadata]
author = "John Doe"
created = 1719978000
EOF

# Parse TSK file
tsk_parse config.tsk

# Get values
id=$(tsk_get storage id)
echo "Storage ID: $id"

# Get all keys in a section
echo "Storage keys:"
tsk_get storage

# Set new value
tsk_set metadata updated "$(date +%s)"

# Save to file
tsk_save updated_config.tsk
```

## Fujsen (Function Serialization)

Fujsen allows you to store and execute functions within TSK files - perfect for smart contracts!

### Storing Functions

```bash
# Create TSK with fujsen
cat > contract.tsk << 'EOF'
[validation]
check_amount_fujsen = """
function validate(amount) {
  if (amount <= 0) return false;
  if (amount > 1000000) return false;
  return true;
}
"""

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

[helpers]
calculate_fee_fujsen = """
amount=$1
echo $(echo "scale=2; $amount * 0.025" | bc)
"""
EOF
```

### Executing Fujsen

```bash
# Execute validation
./tsk.sh fujsen contract.tsk validation check_amount_fujsen 100
# Output: 1 (true)

./tsk.sh fujsen contract.tsk validation check_amount_fujsen -50
# Output: 0 (false)

# Execute payment processing
./tsk.sh fujsen contract.tsk payment process_fujsen 100 "alice@example.com"
# Output: JSON-like result

# Calculate fee
fee=$(./tsk.sh fujsen contract.tsk helpers calculate_fee_fujsen 1000)
echo "Fee: $fee"  # Fee: 25.00
```

### Direct Bash Functions

You can store native Bash functions:

```bash
cat > bash_contract.tsk << 'EOF'
[validators]
email_fujsen = """
_fujsen_validate_email() {
    local email="$1"
    if [[ "$email" =~ ^[^@]+@[^@]+\.[^@]+$ ]]; then
        echo "valid"
    else
        echo "invalid"
    fi
}
_fujsen_validate_email
"""

[processors]
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
EOF

# Execute
./tsk.sh fujsen bash_contract.tsk validators email_fujsen "test@example.com"
# Output: valid

./tsk.sh fujsen bash_contract.tsk processors swap_fujsen 1000 "FLEX"
# Output: {"amount_out": 493.52, "fee": 1.48}
```

## Data Types

TSK supports various data types:

```bash
cat > types.tsk << 'EOF'
[types]
string = "hello world"
number = 42
float = 3.14159
boolean = true
null_value = null
array = [ "one", "two", "three" ]
numbers = [ 1, 2, 3 ]
object = { "key" = "value", "nested" = { "deep" = true } }
multiline = """
This is a
multiline string
with multiple lines
"""
EOF

# Parse and access
tsk_parse types.tsk
echo "String: $(tsk_get types string)"
echo "Number: $(tsk_get types number)"
echo "Boolean: $(tsk_get types boolean)"
echo "Array: $(tsk_get types array)"
```

## Advanced Features

### Piping TSK Content

```bash
# Parse from stdin
echo '[test]
value = "hello"' | ./tsk.sh parse

# Chain operations
cat config.tsk | ./tsk.sh parse | grep "storage"

# Generate TSK from script
{
    echo "[generated]"
    echo "timestamp = $(date +%s)"
    echo "hostname = \"$(hostname)\""
} | ./tsk.sh stringify
```

### Working with Arrays

```bash
# TSK arrays are stored as strings, parse them manually
tsk_parse << 'EOF'
[data]
items = [ "apple", "banana", "cherry" ]
numbers = [ 10, 20, 30 ]
EOF

# Get array as string
items=$(tsk_get data items)
echo "Items: $items"

# Parse array (basic method)
if [[ "$items" =~ ^\[(.+)\]$ ]]; then
    IFS=',' read -ra ARRAY <<< "${BASH_REMATCH[1]}"
    for item in "${ARRAY[@]}"; do
        # Remove quotes and spaces
        item="${item// /}"
        item="${item//\"/}"
        echo "Item: $item"
    done
fi
```

### Error Handling

```bash
# Check if section exists
if [[ -z "$(tsk_get missing_section)" ]]; then
    echo "Section not found"
fi

# Validate fujsen execution
if ! ./tsk.sh fujsen contract.tsk payment invalid_fujsen 2>/dev/null; then
    echo "Fujsen execution failed"
fi

# Parse with error checking
if tsk_parse invalid.tsk 2>/dev/null; then
    echo "Parse successful"
else
    echo "Parse failed"
fi
```

## Complete Example

```bash
#!/bin/bash

source ./tsk.sh

# Create application configuration
cat > app_config.tsk << 'EOF'
[database]
host = "localhost"
port = 5432
name = "flexchain"
pool_size = 10

[api]
endpoints = { "main" = "https://api.flexchain.io", "backup" = "https://backup.flexchain.io" }
timeout = 30000
retry_attempts = 3

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

[contract]
transfer_fujsen = """
_transfer() {
    local from=$1
    local to=$2
    local amount=$3
    
    # Validate amount
    local validation=$(./tsk.sh fujsen app_config.tsk validators amount_fujsen "$amount")
    
    if [[ "$validation" =~ \"valid\":[[:space:]]*false ]]; then
        echo "Error: Invalid amount"
        return 1
    fi
    
    # Simulate transfer
    local tx_id="tx_$(date +%s%3N)"
    
    echo "{
        \"id\": \"$tx_id\",
        \"from\": \"$from\",
        \"to\": \"$to\",
        \"amount\": $amount,
        \"status\": \"completed\"
    }"
}
_transfer
"""
EOF

# Load configuration
tsk_parse app_config.tsk

# Access configuration
echo "Database Configuration:"
echo "  Host: $(tsk_get database host)"
echo "  Port: $(tsk_get database port)"
echo "  Name: $(tsk_get database name)"

# Execute validation
echo -e "\nValidating amounts:"
for amount in 100 -50 "abc" 2000000; do
    result=$(./tsk.sh fujsen app_config.tsk validators amount_fujsen "$amount")
    echo "  Amount $amount: $result"
done

# Execute transfer
echo -e "\nExecuting transfer:"
transfer_result=$(./tsk.sh fujsen app_config.tsk contract transfer_fujsen "alice" "bob" 500)
echo "$transfer_result" | jq . 2>/dev/null || echo "$transfer_result"

# Modify and save
tsk_set api timeout 60000
tsk_save app_config_updated.tsk
echo -e "\nConfiguration saved to app_config_updated.tsk"
```

## Performance Tips

1. **Cache parsed data**: Parse once, use multiple times
2. **Use native Bash**: For performance-critical fujsen, write native Bash
3. **Avoid subshells**: Use variables instead of command substitution when possible
4. **Precompile fujsen**: Store compiled Bash functions for reuse

## Limitations

1. **Array/Object parsing**: Basic support, complex nested structures need manual parsing
2. **JavaScript conversion**: Limited to basic patterns
3. **Type detection**: Relies on pattern matching
4. **Floating point**: Requires `bc` for calculations

## Why TSK in Bash?

- **Universal**: Bash is everywhere, no dependencies needed
- **Scriptable**: Perfect for automation and CI/CD
- **Integration**: Easy to use in existing shell scripts
- **Lightweight**: No runtime overhead
- **Portable**: Works on any Unix-like system
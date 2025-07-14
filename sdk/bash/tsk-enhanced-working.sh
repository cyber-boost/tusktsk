#!/bin/bash
# TSK (TuskLang Configuration) Parser and Generator for Bash
# Handles the TOML-like format with fujsen (function serialization) support

# Enable strict mode
set -euo pipefail

# Global variables for parser state
declare -A TSK_DATA
declare -A FUJSEN_CACHE
declare -A TSK_COMMENTS
CURRENT_SECTION=""
IN_MULTILINE=false
MULTILINE_KEY=""
MULTILINE_CONTENT=""
SHELL_MAGIC="FLEX"
SHELL_VERSION=1

# Shell storage functions
shell_pack() {
    local id="$1"
    local data="$2"
    local temp_file=$(mktemp)
    
    # Compress data
    echo -n "$data" | gzip > "$temp_file"
    local compressed_data=$(cat "$temp_file")
    rm -f "$temp_file"
    
    # Build binary format
    # Magic (4) + Version (1) + ID Length (4) + ID + Data Length (4) + Data
    printf "%s" "$SHELL_MAGIC" > "$temp_file"
    printf "\\x%02x" $SHELL_VERSION >> "$temp_file"
    
    # ID length (big-endian 32-bit)
    local id_len=${#id}
    printf "\\x%02x\\x%02x\\x%02x\\x%02x" \
        $((id_len >> 24 & 0xFF)) \
        $((id_len >> 16 & 0xFF)) \
        $((id_len >> 8 & 0xFF)) \
        $((id_len & 0xFF)) >> "$temp_file"
    
    # ID
    printf "%s" "$id" >> "$temp_file"
    
    # Data length
    local data_len=${#compressed_data}
    printf "\\x%02x\\x%02x\\x%02x\\x%02x" \
        $((data_len >> 24 & 0xFF)) \
        $((data_len >> 16 & 0xFF)) \
        $((data_len >> 8 & 0xFF)) \
        $((data_len & 0xFF)) >> "$temp_file"
    
    # Compressed data
    cat <<< "$compressed_data" >> "$temp_file"
    
    cat "$temp_file"
    rm -f "$temp_file"
}

shell_unpack() {
    local shell_data="$1"
    local temp_file=$(mktemp)
    
    echo -n "$shell_data" > "$temp_file"
    
    # Check magic
    local magic=$(dd if="$temp_file" bs=1 count=4 2>/dev/null)
    if [[ "$magic" != "$SHELL_MAGIC" ]]; then
        rm -f "$temp_file"
        echo "Error: Invalid shell format" >&2
        return 1
    fi
    
    # Read version
    local version=$(dd if="$temp_file" bs=1 skip=4 count=1 2>/dev/null | od -An -tu1)
    
    # Read ID length
    local id_len_bytes=$(dd if="$temp_file" bs=1 skip=5 count=4 2>/dev/null | od -An -tu1)
    local id_len=0
    for byte in $id_len_bytes; do
        id_len=$((id_len * 256 + byte))
    done
    
    # Read ID
    local id=$(dd if="$temp_file" bs=1 skip=9 count=$id_len 2>/dev/null)
    
    # Read data length
    local data_start=$((9 + id_len))
    local data_len_bytes=$(dd if="$temp_file" bs=1 skip=$data_start count=4 2>/dev/null | od -An -tu1)
    local data_len=0
    for byte in $data_len_bytes; do
        data_len=$((data_len * 256 + byte))
    done
    
    # Read and decompress data
    local compressed_start=$((data_start + 4))
    dd if="$temp_file" bs=1 skip=$compressed_start count=$data_len 2>/dev/null | gunzip
    
    rm -f "$temp_file"
}

# Parse TSK content from stdin or file
tsk_parse() {
    local input="${1:-}"
    local line_num=0
    
    # Reset global state
    TSK_DATA=()
    TSK_COMMENTS=()
    CURRENT_SECTION=""
    IN_MULTILINE=false
    MULTILINE_KEY=""
    MULTILINE_CONTENT=""
    
    # Read input
    while IFS= read -r line || [[ -n "$line" ]]; do
        ((line_num++))
        
        # Handle multiline strings
        if [[ "$IN_MULTILINE" == "true" ]]; then
            if [[ "$line" =~ ^[[:space:]]*\"\"\"[[:space:]]*$ ]]; then
                # End of multiline string
                TSK_DATA["${CURRENT_SECTION}.${MULTILINE_KEY}"]="$MULTILINE_CONTENT"
                IN_MULTILINE=false
                MULTILINE_KEY=""
                MULTILINE_CONTENT=""
                continue
            else
                # Accumulate multiline content
                if [[ -n "$MULTILINE_CONTENT" ]]; then
                    MULTILINE_CONTENT+=$'\n'
                fi
                MULTILINE_CONTENT+="$line"
                continue
            fi
        fi
        
        # Capture comments
        if [[ "$line" =~ ^[[:space:]]*# ]]; then
            TSK_COMMENTS[$line_num]="${line// /}"
            continue
        fi
        
        # Skip empty lines
        if [[ -z "${line// }" ]]; then
            continue
        fi
        
        # Section header
        if [[ "$line" =~ ^[[:space:]]*\[([^]]+)\][[:space:]]*$ ]]; then
            CURRENT_SECTION="${BASH_REMATCH[1]}"
            continue
        fi
        
        # Key-value pair
        if [[ "$line" =~ ^[[:space:]]*([^=]+)=[[:space:]]*(.*)$ ]]; then
            local key="${BASH_REMATCH[1]// /}"
            local value="${BASH_REMATCH[2]}"
            
            # Check for multiline string start
            if [[ "$value" =~ ^\"\"\"[[:space:]]*$ ]]; then
                IN_MULTILINE=true
                MULTILINE_KEY="$key"
                MULTILINE_CONTENT=""
                continue
            fi
            
            # Parse and store value
            local parsed_value=$(tsk_parse_value "$value")
            TSK_DATA["${CURRENT_SECTION}.${key}"]="$parsed_value"
        fi
    done < <(if [[ -n "$input" ]] && [[ -f "$input" ]]; then cat "$input"; else cat; fi)
}

# Parse a single value
tsk_parse_value() {
    local value="$1"
    
    # Trim whitespace
    value="${value#"${value%%[![:space:]]*}"}"
    value="${value%"${value##*[![:space:]]}"}"
    
    # Null
    if [[ "$value" == "null" ]]; then
        echo "null"
        return
    fi
    
    # Boolean
    if [[ "$value" == "true" ]] || [[ "$value" == "false" ]]; then
        echo "$value"
        return
    fi
    
    # Number (integer or float)
    if [[ "$value" =~ ^-?[0-9]+$ ]] || [[ "$value" =~ ^-?[0-9]+\.[0-9]+$ ]]; then
        echo "$value"
        return
    fi
    
    # String
    if [[ "$value" =~ ^\"(.*)\"$ ]]; then
        # Unescape quotes and backslashes
        local unescaped="${BASH_REMATCH[1]}"
        unescaped="${unescaped//\\\"/\"}"
        unescaped="${unescaped//\\\\/\\}"
        echo "$unescaped"
        return
    fi
    
    # Array
    if [[ "$value" =~ ^\[.*\]$ ]]; then
        echo "$value"
        return
    fi
    
    # Object
    if [[ "$value" =~ ^\{.*\}$ ]]; then
        echo "$value"
        return
    fi
    
    # Return as-is
    echo "$value"
}

# Get a value from parsed TSK data
tsk_get() {
    local section="$1"
    local key="${2:-}"
    
    if [[ -z "$key" ]]; then
        # Get all keys in section
        for k in "${!TSK_DATA[@]}"; do
            if [[ "$k" =~ ^${section}\. ]]; then
                echo "${k#${section}.}"
            fi
        done
    else
        # Get specific value
        echo "${TSK_DATA["${section}.${key}"]:-}"
    fi
}

# Set a value in TSK data
tsk_set() {
    local section="$1"
    local key="$2"
    local value="$3"
    
    TSK_DATA["${section}.${key}"]="$value"
}

# Generate TSK content from data
tsk_stringify() {
    echo "# Generated by Flexchain TSK Bash SDK"
    echo "# $(date -Iseconds)"
    echo
    
    # Get unique sections
    local sections=()
    for key in "${!TSK_DATA[@]}"; do
        local section="${key%%.*}"
        if [[ ! " ${sections[@]} " =~ " ${section} " ]]; then
            sections+=("$section")
        fi
    done
    
    # Output each section
    for section in "${sections[@]}"; do
        echo "[$section]"
        
        # Output key-value pairs for this section
        for key in "${!TSK_DATA[@]}"; do
            if [[ "$key" =~ ^${section}\.(.+)$ ]]; then
                local k="${BASH_REMATCH[1]}"
                local v="${TSK_DATA[$key]}"
                echo "$k = $(tsk_format_value "$v")"
            fi
        done
        echo
    done
}

# Format a value for TSK output
tsk_format_value() {
    local value="$1"
    
    # Null
    if [[ "$value" == "null" ]]; then
        echo "null"
        return
    fi
    
    # Boolean
    if [[ "$value" == "true" ]] || [[ "$value" == "false" ]]; then
        echo "$value"
        return
    fi
    
    # Number
    if [[ "$value" =~ ^-?[0-9]+$ ]] || [[ "$value" =~ ^-?[0-9]+\.[0-9]+$ ]]; then
        echo "$value"
        return
    fi
    
    # Array or Object (already formatted)
    if [[ "$value" =~ ^\[.*\]$ ]] || [[ "$value" =~ ^\{.*\}$ ]]; then
        echo "$value"
        return
    fi
    
    # Multiline string
    if [[ "$value" =~ $'\n' ]]; then
        echo '"""'
        echo "$value"
        echo '"""'
        return
    fi
    
    # Regular string
    value="${value//\\/\\\\}"
    value="${value//\"/\\\"}"
    echo "\"$value\""
}

# Execute fujsen (function stored in TSK)
tsk_execute_fujsen() {
    local section="$1"
    local key="${2:-fujsen}"
    shift 2
    local args=("$@")
    
    local fujsen_code="${TSK_DATA["${section}.${key}"]:-}"
    
    if [[ -z "$fujsen_code" ]]; then
        echo "Error: No fujsen found at ${section}.${key}" >&2
        return 1
    fi
    
    # Check cache
    local cache_key="${section}.${key}"
    if [[ -n "${FUJSEN_CACHE[$cache_key]:-}" ]]; then
        # Execute cached function
        eval "${FUJSEN_CACHE[$cache_key]}" "${args[@]}"
        return
    fi
    
    # Compile fujsen to bash function
    local compiled=$(tsk_compile_fujsen "$fujsen_code")
    FUJSEN_CACHE[$cache_key]="$compiled"
    
    # Execute
    eval "$compiled" "${args[@]}"
}

# Execute fujsen with context (variables)
tsk_execute_fujsen_with_context() {
    local section="$1"
    local key="$2"
    local context="$3"
    shift 3
    local args=("$@")
    
    local fujsen_code="${TSK_DATA["${section}.${key}"]:-}"
    
    if [[ -z "$fujsen_code" ]]; then
        echo "Error: No fujsen found at ${section}.${key}" >&2
        return 1
    fi
    
    # Compile function
    local cache_key="${section}.${key}"
    if [[ -z "${FUJSEN_CACHE[$cache_key]:-}" ]]; then
        FUJSEN_CACHE[$cache_key]=$(tsk_compile_fujsen "$fujsen_code")
    fi
    
    # Execute with context variables
    (
        # Set context variables in subshell
        eval "$context"
        # Execute function
        eval "${FUJSEN_CACHE[$cache_key]}" "${args[@]}"
    )
}

# Compile fujsen code to bash
tsk_compile_fujsen() {
    local code="$1"
    local func_name="_fujsen_$$_${RANDOM}"
    
    # Remove leading/trailing whitespace
    code="${code#"${code%%[![:space:]]*}"}"
    code="${code%"${code##*[![:space:]]}"}"
    
    # Check for function declaration
    if [[ "$code" =~ ^function[[:space:]]+([[:alnum:]_]+)[[:space:]]*\(([^)]*)\)[[:space:]]*\{(.*)\}$ ]]; then
        local name="${BASH_REMATCH[1]}"
        local params="${BASH_REMATCH[2]}"
        local body="${BASH_REMATCH[3]}"
        
        # Convert to bash function
        echo "${func_name}() {"
        
        # Parse parameters
        if [[ -n "$params" ]]; then
            local i=1
            IFS=',' read -ra PARAMS <<< "$params"
            for param in "${PARAMS[@]}"; do
                param="${param// /}"
                echo "  local $param=\"\${$i:-}\""
                ((i++))
            done
        fi
        
        # Convert body (basic JS to Bash)
        echo "  $(tsk_js_to_bash "$body")"
        echo "}"
        echo "${func_name}"
        return
    fi
    
    # Check for arrow function
    if [[ "$code" =~ ^\(([^)]*)\)[[:space:]]*=\>[[:space:]]*\{?(.*)\}?$ ]]; then
        local params="${BASH_REMATCH[1]}"
        local body="${BASH_REMATCH[2]}"
        
        echo "${func_name}() {"
        
        # Parse parameters
        if [[ -n "$params" ]]; then
            local i=1
            IFS=',' read -ra PARAMS <<< "$params"
            for param in "${PARAMS[@]}"; do
                param="${param// /}"
                echo "  local $param=\"\${$i:-}\""
                ((i++))
            done
        fi
        
        # Convert body
        if [[ "$body" =~ \{ ]]; then
            echo "  $(tsk_js_to_bash "$body")"
        else
            echo "  echo \$($(tsk_js_to_bash "$body"))"
        fi
        echo "}"
        echo "${func_name}"
        return
    fi
    
    # Direct bash code
    echo "$code"
}

# Basic JS to Bash conversion
tsk_js_to_bash() {
    local js="$1"
    local bash="$js"
    
    # Basic conversions
    bash="${bash//console.log/echo}"
    bash="${bash//const /local }"
    bash="${bash//let /local }"
    bash="${bash//var /local }"
    bash="${bash//new Error/echo}"
    bash="${bash//throw /return 1 # }"
    bash="${bash//Date.now()/\$(date +%s%3N)}"
    bash="${bash//Math.min/tsk_min}"
    bash="${bash//Math.max/tsk_max}"
    bash="${bash//true/1}"
    bash="${bash//false/0}"
    bash="${bash//===/==}"
    bash="${bash//!==/!=}"
    bash="${bash//||/ || }"
    bash="${bash//&&/ && }"
    
    # Return statement
    bash="${bash//return {/echo '{'}"
    bash="${bash//return/echo}"
    
    # Template literals (basic)
    bash=$(echo "$bash" | sed -E 's/`([^`]*)\$\{([^}]+)\}([^`]*)`/"\1${\2}\3"/g')
    
    echo "$bash"
}

# Math helper functions
tsk_min() {
    local min="$1"
    shift
    for n in "$@"; do
        if (( $(echo "$n < $min" | bc -l) )); then
            min="$n"
        fi
    done
    echo "$min"
}

tsk_max() {
    local max="$1"
    shift
    for n in "$@"; do
        if (( $(echo "$n > $max" | bc -l) )); then
            max="$n"
        fi
    done
    echo "$max"
}

# Save TSK data to file
tsk_save() {
    local file="$1"
    tsk_stringify > "$file"
}

# Load TSK data from file
tsk_load() {
    local file="$1"
    tsk_parse "$file"
}

# Store data with shell format
tsk_store_with_shell() {
    local data="$1"
    local metadata="${2:-}"
    
    # Generate storage ID
    local storage_id="flex_$(date +%s%3N)_$(openssl rand -hex 4)"
    
    # Detect type
    local content_type=$(tsk_detect_type "$data")
    local size=${#data}
    
    # Set storage section
    tsk_set "storage" "id" "$storage_id"
    tsk_set "storage" "type" "$content_type"
    tsk_set "storage" "size" "$size"
    tsk_set "storage" "created" "$(date +%s)"
    tsk_set "storage" "chunks" "$((size / 65536 + 1))"
    
    # Set metadata if provided
    if [[ -n "$metadata" ]]; then
        # Parse metadata as key=value pairs
        while IFS='=' read -r key value; do
            tsk_set "metadata" "$key" "$value"
        done <<< "$metadata"
    fi
    
    # Generate hash
    local hash=$(echo -n "$data" | sha256sum | cut -d' ' -f1)
    tsk_set "verification" "hash" "sha256:$hash"
    tsk_set "verification" "checksum" "sha256"
    
    # Pack shell data
    local shell_data=$(shell_pack "$storage_id" "$data")
    
    # Return results
    echo "storage_id=$storage_id"
    echo "type=$content_type"
    echo "size=$size"
    echo "shell_data_length=${#shell_data}"
}

# Retrieve data from shell storage
tsk_retrieve_from_shell() {
    local shell_data="$1"
    
    # Unpack shell data
    local data=$(shell_unpack "$shell_data")
    
    # Get metadata
    local storage_info=""
    for key in id type size created chunks; do
        local value=$(tsk_get "storage" "$key")
        if [[ -n "$value" ]]; then
            storage_info+="$key=$value "
        fi
    done
    
    echo "data=$data"
    echo "storage_info=$storage_info"
}

# Detect content type
tsk_detect_type() {
    local data="$1"
    
    # Check if it's binary by looking at first bytes
    local first_bytes=$(echo -n "$data" | od -An -tx1 -N16 | tr -d ' \n')
    
    # Common file signatures
    case "$first_bytes" in
        ffd8ff*) echo "image/jpeg" ;;
        89504e47*) echo "image/png" ;;
        25504446*) echo "application/pdf" ;;
        504b0304*) echo "application/zip" ;;
        *) 
            # Check if it's text
            if echo -n "$data" | grep -q '[^[:print:][:space:]]'; then
                echo "application/octet-stream"
            else
                echo "text/plain"
            fi
            ;;
    esac
}

# Add fujsen function dynamically
tsk_set_fujsen() {
    local section="$1"
    local key="${2:-fujsen}"
    local func_name="$3"
    
    # Get function definition
    local func_def=$(declare -f "$func_name")
    if [[ -z "$func_def" ]]; then
        echo "Error: Function $func_name not found" >&2
        return 1
    fi
    
    # Store function definition
    tsk_set "$section" "$key" "$func_def"
}

# Get all fujsen functions in a section
tsk_get_fujsen_map() {
    local section="$1"
    
    for key in "${!TSK_DATA[@]}"; do
        if [[ "$key" =~ ^${section}\.(.*fujsen)$ ]]; then
            local fujsen_key="${BASH_REMATCH[1]}"
            echo "$fujsen_key"
        fi
    done
}

# CLI interface
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    case "${1:-help}" in
        parse)
            tsk_parse "${2:-}"
            # Output all parsed data
            for key in "${!TSK_DATA[@]}"; do
                echo "$key = ${TSK_DATA[$key]}"
            done
            ;;
        get)
            tsk_parse "${2:-}"
            tsk_get "$3" "${4:-}"
            ;;
        set)
            tsk_parse "${5:-}"
            tsk_set "$2" "$3" "$4"
            tsk_stringify
            ;;
        fujsen)
            tsk_parse "${2:-}"
            tsk_execute_fujsen "$3" "${4:-fujsen}" "${@:5}"
            ;;
        shell-store)
            tsk_parse "${4:-}"
            tsk_store_with_shell "$2" "$3"
            ;;
        shell-retrieve)
            tsk_parse "${3:-}"
            tsk_retrieve_from_shell "$2"
            ;;
        stringify)
            tsk_parse "${2:-}"
            tsk_stringify
            ;;
        *)
            cat << EOF
TSK Bash SDK - TuskLang Configuration Parser

Usage:
  tsk.sh parse [file]                          Parse TSK file and show data
  tsk.sh get [file] section [key]              Get value from TSK
  tsk.sh set section key value [file]          Set value and output TSK
  tsk.sh fujsen [file] section [key] ...       Execute fujsen function
  tsk.sh shell-store data metadata [file]      Store data with shell format
  tsk.sh shell-retrieve shell_data [file]      Retrieve data from shell
  tsk.sh stringify [file]                      Convert to TSK format

Examples:
  # Parse TSK file
  ./tsk.sh parse config.tsk
  
  # Get specific value
  ./tsk.sh get config.tsk database host
  
  # Execute fujsen
  ./tsk.sh fujsen contract.tsk payment process 100 alice@example.com
  
  # Store with shell
  ./tsk.sh shell-store "Hello World" "author=test" config.tsk
  
  # Pipe TSK content
  echo '[test]' | ./tsk.sh parse
EOF
            ;;
    esac
fi
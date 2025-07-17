#!/bin/bash
# TuskLang Enhanced for Bash - The Freedom Parser
# ===============================================
# "We don't bow to any king" - Support ALL syntax styles
#
# Features:
# - Multiple grouping: [], {}, <>
# - $global vs section-local variables
# - Cross-file communication
# - Database queries (via CLI tools)
# - All @ operators
# - Maximum flexibility
#
# DEFAULT CONFIG: peanut.tsk (the bridge of language grace)

# Enable strict mode
set -euo pipefail

# Global variables for parser state
declare -A TSK_DATA
declare -A TSK_GLOBALS      # $variables
declare -A TSK_SECTION_VARS # section-local variables
declare -A TSK_CACHE
declare -A CROSS_FILE_CACHE
CURRENT_SECTION=""
IN_OBJECT=false
OBJECT_KEY=""
OBJECT_INDENT=0
PEANUT_LOADED=false

# Default peanut.tsk locations
PEANUT_LOCATIONS=(
    "./peanut.tsk"
    "../peanut.tsk"
    "../../peanut.tsk"
    "/etc/tusklang/peanut.tsk"
    "${TUSKLANG_CONFIG:-}"
)

# Load peanut.tsk if available
load_peanut() {
    if [[ "$PEANUT_LOADED" == "true" ]]; then
        return
    fi
    
    for location in "${PEANUT_LOCATIONS[@]}"; do
        if [[ -n "$location" && -f "$location" ]]; then
            echo "# Loading universal config from: $location" >&2
            parse_file "$location"
            PEANUT_LOADED=true
            return
        fi
    done
}

# Parse TuskLang value
parse_value() {
    local value="$1"
    local trimmed="${value#"${value%%[![:space:]]*}"}"
    trimmed="${trimmed%"${trimmed##*[![:space:]]}"}"
    
    # Remove optional semicolon at end
    trimmed="${trimmed%;}"
    
    # Basic types
    case "$trimmed" in
        "true") echo "true"; return ;;
        "false") echo "false"; return ;;
        "null") echo "null"; return ;;
    esac
    
    # Numbers
    if [[ "$trimmed" =~ ^-?[0-9]+$ ]]; then
        echo "$trimmed"
        return
    fi
    
    if [[ "$trimmed" =~ ^-?[0-9]+\.[0-9]+$ ]]; then
        echo "$trimmed"
        return
    fi
    
    # $variable references (global)
    if [[ "$trimmed" =~ ^\$([a-zA-Z_][a-zA-Z0-9_]*)$ ]]; then
        local var_name="${BASH_REMATCH[1]}"
        echo "${TSK_GLOBALS[$var_name]:-}"
        return
    fi
    
    # Section-local variable references
    if [[ -n "$CURRENT_SECTION" && "$trimmed" =~ ^[a-zA-Z_][a-zA-Z0-9_]*$ ]]; then
        local section_key="${CURRENT_SECTION}.${trimmed}"
        if [[ -n "${TSK_SECTION_VARS[$section_key]:-}" ]]; then
            echo "${TSK_SECTION_VARS[$section_key]}"
            return
        fi
    fi
    
    # @date function
    if [[ "$trimmed" =~ ^@date\([\'\"](.*)[\'\"]\)$ ]]; then
        local format="${BASH_REMATCH[1]}"
        # Convert PHP date format to date command format
        case "$format" in
            "Y") date +%Y ;;
            "Y-m-d") date +%Y-%m-%d ;;
            "Y-m-d H:i:s") date "+%Y-%m-%d %H:%M:%S" ;;
            "c") date -Iseconds ;;
            *) date ;;
        esac
        return
    fi
    
    # @env function
    if [[ "$trimmed" =~ ^@env\([\'\"](.*)[\'\"](,[[:space:]]*(.+))?\)$ ]]; then
        local env_var="${BASH_REMATCH[1]}"
        local default_val="${BASH_REMATCH[3]:-}"
        # Remove quotes from default value
        default_val="${default_val#[\"\']}"
        default_val="${default_val%[\"\']}"
        echo "${!env_var:-$default_val}"
        return
    fi
    
    # Ranges: 8000-9000
    if [[ "$trimmed" =~ ^([0-9]+)-([0-9]+)$ ]]; then
        echo "{\"min\":${BASH_REMATCH[1]},\"max\":${BASH_REMATCH[2]},\"type\":\"range\"}"
        return
    fi
    
    # Arrays
    if [[ "$trimmed" =~ ^\[.*\]$ ]]; then
        echo "$trimmed"
        return
    fi
    
    # Objects
    if [[ "$trimmed" =~ ^\{.*\}$ ]]; then
        echo "$trimmed"
        return
    fi
    
    # Cross-file references: @file.tsk.get('key')
    if [[ "$trimmed" =~ ^@([a-zA-Z0-9_-]+)\.tsk\.get\([\'\"](.*)[\'\"]\)$ ]]; then
        local file="${BASH_REMATCH[1]}"
        local key="${BASH_REMATCH[2]}"
        cross_file_get "$file" "$key"
        return
    fi
    
    # Cross-file set: @file.tsk.set('key', value)
    if [[ "$trimmed" =~ ^@([a-zA-Z0-9_-]+)\.tsk\.set\([\'\"](.*)[\'\"],[[:space:]]*(.+)\)$ ]]; then
        local file="${BASH_REMATCH[1]}"
        local key="${BASH_REMATCH[2]}"
        local val="${BASH_REMATCH[3]}"
        cross_file_set "$file" "$key" "$val"
        return
    fi
    
    # @query function (requires database CLI tools)
    if [[ "$trimmed" =~ ^@query\([\'\"](.*)[\'\"](.*)\)$ ]]; then
        local query="${BASH_REMATCH[1]}"
        execute_query "$query"
        return
    fi
    
    # @ operators (placeholders for now)
    if [[ "$trimmed" =~ ^@([a-zA-Z_][a-zA-Z0-9_]*)\((.*)\)$ ]]; then
        local operator="${BASH_REMATCH[1]}"
        local params="${BASH_REMATCH[2]}"
        execute_operator "$operator" "$params"
        return
    fi
    
    # String concatenation
    if [[ "$trimmed" =~ [[:space:]]\+[[:space:]] ]]; then
        local result=""
        IFS='+' read -ra parts <<< "$trimmed"
        for part in "${parts[@]}"; do
            local clean_part="${part#"${part%%[![:space:]]*}"}"
            clean_part="${clean_part%"${clean_part##*[![:space:]]}"}"
            clean_part="${clean_part#[\"\']}"
            clean_part="${clean_part%[\"\']}"
            local parsed_part=$(parse_value "$clean_part")
            result="${result}${parsed_part}"
        done
        echo "$result"
        return
    fi
    
    # Conditional/ternary
    if [[ "$trimmed" =~ (.+)[[:space:]]\?[[:space:]](.+)[[:space:]]:[[:space:]](.+) ]]; then
        local condition="${BASH_REMATCH[1]}"
        local true_val="${BASH_REMATCH[2]}"
        local false_val="${BASH_REMATCH[3]}"
        
        if evaluate_condition "$condition"; then
            parse_value "$true_val"
        else
            parse_value "$false_val"
        fi
        return
    fi
    
    # Remove quotes from strings
    if [[ "$trimmed" =~ ^[\"\'](.*)[\"\']$ ]]; then
        echo "${BASH_REMATCH[1]}"
        return
    fi
    
    # Return as-is
    echo "$trimmed"
}

# Evaluate conditions
evaluate_condition() {
    local condition="$1"
    
    # Simple equality check
    if [[ "$condition" =~ (.+)[[:space:]]==[[:space:]](.+) ]]; then
        local left=$(parse_value "${BASH_REMATCH[1]}")
        local right=$(parse_value "${BASH_REMATCH[2]}")
        [[ "$left" == "$right" ]]
        return $?
    fi
    
    # Not equal
    if [[ "$condition" =~ (.+)[[:space:]]!=[[:space:]](.+) ]]; then
        local left=$(parse_value "${BASH_REMATCH[1]}")
        local right=$(parse_value "${BASH_REMATCH[2]}")
        [[ "$left" != "$right" ]]
        return $?
    fi
    
    # Greater than
    if [[ "$condition" =~ (.+)[[:space:]]>[[:space:]](.+) ]]; then
        local left=$(parse_value "${BASH_REMATCH[1]}")
        local right=$(parse_value "${BASH_REMATCH[2]}")
        [[ "$left" -gt "$right" ]] 2>/dev/null || [[ "$left" > "$right" ]]
        return $?
    fi
    
    # Default: check if truthy
    local value=$(parse_value "$condition")
    [[ -n "$value" && "$value" != "false" && "$value" != "null" && "$value" != "0" ]]
}

# Cross-file get
cross_file_get() {
    local file="$1"
    local key="$2"
    local cache_key="${file}:${key}"
    
    # Check cache
    if [[ -n "${CROSS_FILE_CACHE[$cache_key]:-}" ]]; then
        echo "${CROSS_FILE_CACHE[$cache_key]}"
        return
    fi
    
    # Find file
    local filepath=""
    for dir in . ./config .. ../config; do
        if [[ -f "$dir/${file}.tsk" ]]; then
            filepath="$dir/${file}.tsk"
            break
        fi
    done
    
    if [[ -z "$filepath" ]]; then
        echo ""
        return
    fi
    
    # Parse file and get value
    local temp_section="$CURRENT_SECTION"
    CURRENT_SECTION=""
    
    # Save current state
    local -A temp_data
    for k in "${!TSK_DATA[@]}"; do
        temp_data["$k"]="${TSK_DATA[$k]}"
    done
    
    # Parse target file
    parse_file "$filepath"
    
    # Get value
    local value="${TSK_DATA[$key]:-}"
    
    # Restore state
    TSK_DATA=()
    for k in "${!temp_data[@]}"; do
        TSK_DATA["$k"]="${temp_data[$k]}"
    done
    CURRENT_SECTION="$temp_section"
    
    # Cache result
    CROSS_FILE_CACHE[$cache_key]="$value"
    
    echo "$value"
}

# Cross-file set
cross_file_set() {
    local file="$1"
    local key="$2"
    local value="$3"
    
    # For now, just update cache
    local cache_key="${file}:${key}"
    CROSS_FILE_CACHE[$cache_key]="$value"
    
    echo "$value"
}

# Execute database query (requires CLI tools)
execute_query() {
    local query="$1"
    
    # Load peanut.tsk to get database config
    load_peanut
    
    # Determine database type
    local db_type="${TSK_DATA[database.default]:-sqlite}"
    
    case "$db_type" in
        "sqlite")
            if command -v sqlite3 &> /dev/null; then
                local db_file="${TSK_DATA[database.sqlite.filename]:-./tusklang.db}"
                sqlite3 "$db_file" "$query" 2>/dev/null || echo "0"
            else
                echo "0"
            fi
            ;;
        "postgres"|"postgresql")
            if command -v psql &> /dev/null; then
                local host="${TSK_DATA[database.postgres.host]:-localhost}"
                local port="${TSK_DATA[database.postgres.port]:-5432}"
                local db="${TSK_DATA[database.postgres.database]:-tusklang}"
                local user="${TSK_DATA[database.postgres.user]:-postgres}"
                PGPASSWORD="${TSK_DATA[database.postgres.password]:-}" \
                    psql -h "$host" -p "$port" -U "$user" -d "$db" -t -c "$query" 2>/dev/null | tr -d ' \n' || echo "0"
            else
                echo "0"
            fi
            ;;
        "mysql")
            if command -v mysql &> /dev/null; then
                local host="${TSK_DATA[database.mysql.host]:-localhost}"
                local port="${TSK_DATA[database.mysql.port]:-3306}"
                local db="${TSK_DATA[database.mysql.database]:-tusklang}"
                local user="${TSK_DATA[database.mysql.user]:-root}"
                local pass="${TSK_DATA[database.mysql.password]:-}"
                mysql -h "$host" -P "$port" -u "$user" -p"$pass" -D "$db" -sN -e "$query" 2>/dev/null || echo "0"
            else
                echo "0"
            fi
            ;;
        *)
            echo "0"
            ;;
    esac
}

# Execute @ operators
execute_operator() {
    local operator="$1"
    local params="$2"
    
    case "$operator" in
        "cache")
            # Simple cache implementation
            local ttl_and_value="$params"
            if [[ "$ttl_and_value" =~ ^[\'\"](.*)[\'\"],[[:space:]]*(.+)$ ]]; then
                local ttl="${BASH_REMATCH[1]}"
                local value="${BASH_REMATCH[2]}"
                local parsed_value=$(parse_value "$value")
                # For now, just return the value
                echo "$parsed_value"
            else
                echo ""
            fi
            ;;
        "learn"|"optimize"|"metrics"|"feature")
            # Placeholders for advanced features
            echo "@${operator}(${params})"
            ;;
        *)
            echo "@${operator}(${params})"
            ;;
    esac
}

# Parse a line
parse_line() {
    local line="$1"
    local trimmed="${line#"${line%%[![:space:]]*}"}"
    trimmed="${trimmed%"${trimmed##*[![:space:]]}"}"
    
    # Skip empty lines and comments
    if [[ -z "$trimmed" || "$trimmed" =~ ^# ]]; then
        return
    fi
    
    # Remove optional semicolon
    trimmed="${trimmed%;}"
    
    # Check for section declaration []
    if [[ "$trimmed" =~ ^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$ ]]; then
        CURRENT_SECTION="${BASH_REMATCH[1]}"
        IN_OBJECT=false
        return
    fi
    
    # Check for angle bracket object >
    if [[ "$trimmed" =~ ^([a-zA-Z_][a-zA-Z0-9_]*)[[:space:]]*>$ ]]; then
        IN_OBJECT=true
        OBJECT_KEY="${BASH_REMATCH[1]}"
        OBJECT_INDENT=$((${#line} - ${#trimmed}))
        return
    fi
    
    # Check for closing angle bracket <
    if [[ "$trimmed" == "<" ]]; then
        IN_OBJECT=false
        OBJECT_KEY=""
        return
    fi
    
    # Check for curly brace object {
    if [[ "$trimmed" =~ ^([a-zA-Z_][a-zA-Z0-9_]*)[[:space:]]*\{$ ]]; then
        IN_OBJECT=true
        OBJECT_KEY="${BASH_REMATCH[1]}"
        return
    fi
    
    # Check for closing curly brace }
    if [[ "$trimmed" == "}" ]]; then
        IN_OBJECT=false
        OBJECT_KEY=""
        return
    fi
    
    # Parse key-value pairs (both : and = supported)
    if [[ "$trimmed" =~ ^([\$]?[a-zA-Z_][a-zA-Z0-9_-]*)[[:space:]]*[:=][[:space:]]*(.+)$ ]]; then
        local key="${BASH_REMATCH[1]}"
        local value="${BASH_REMATCH[2]}"
        local parsed_value=$(parse_value "$value")
        
        # Determine storage location
        local storage_key=""
        if [[ -n "$IN_OBJECT" && -n "$OBJECT_KEY" ]]; then
            if [[ -n "$CURRENT_SECTION" ]]; then
                storage_key="${CURRENT_SECTION}.${OBJECT_KEY}.${key}"
            else
                storage_key="${OBJECT_KEY}.${key}"
            fi
        elif [[ -n "$CURRENT_SECTION" ]]; then
            storage_key="${CURRENT_SECTION}.${key}"
        else
            storage_key="$key"
        fi
        
        # Store the value
        TSK_DATA["$storage_key"]="$parsed_value"
        
        # Handle global variables
        if [[ "$key" =~ ^\$(.+)$ ]]; then
            TSK_GLOBALS["${BASH_REMATCH[1]}"]="$parsed_value"
        elif [[ -n "$CURRENT_SECTION" && ! "$key" =~ ^\$ ]]; then
            # Store section-local variable
            TSK_SECTION_VARS["${CURRENT_SECTION}.${key}"]="$parsed_value"
        fi
    fi
}

# Parse a file
parse_file() {
    local file="$1"
    
    if [[ ! -f "$file" ]]; then
        echo "Error: File not found: $file" >&2
        return 1
    fi
    
    while IFS= read -r line || [[ -n "$line" ]]; do
        parse_line "$line"
    done < "$file"
}

# Parse from stdin
parse_stdin() {
    while IFS= read -r line; do
        parse_line "$line"
    done
}

# Get a value
tsk_get() {
    local key="$1"
    echo "${TSK_DATA[$key]:-}"
}

# Set a value
tsk_set() {
    local key="$1"
    local value="$2"
    TSK_DATA["$key"]="$value"
}

# List all keys
tsk_keys() {
    for key in "${!TSK_DATA[@]}"; do
        echo "$key"
    done | sort
}

# List all values
tsk_values() {
    for key in $(tsk_keys); do
        echo "$key: ${TSK_DATA[$key]}"
    done
}

# Export as environment variables
tsk_export() {
    local prefix="${1:-TSK_}"
    
    for key in "${!TSK_DATA[@]}"; do
        local var_name="${prefix}${key//[.-]/_}"
        var_name="${var_name^^}"  # Uppercase
        export "$var_name=${TSK_DATA[$key]}"
    done
}

# Usage
usage() {
    cat << EOF
TuskLang Enhanced for Bash - The Freedom Parser
===============================================

Usage: $0 [command] [options]

Commands:
    parse <file>     Parse a .tsk file
    get <key>        Get a value by key
    set <key> <val>  Set a value
    keys             List all keys
    values           List all key-value pairs
    export [prefix]  Export as environment variables

Options:
    -h, --help       Show this help message

Examples:
    $0 parse config.tsk
    $0 get database.host
    $0 set app.name "My App"
    $0 export TSK_

Features:
    - Multiple syntax styles: [], {}, <>
    - Global variables with \$
    - Cross-file references: @file.tsk.get()
    - Database queries: @query()
    - Date functions: @date()
    - Environment variables: @env()

Default config file: peanut.tsk
EOF
}

# Main
main() {
    local command="${1:-}"
    
    case "$command" in
        "parse")
            if [[ -n "${2:-}" ]]; then
                parse_file "$2"
            else
                parse_stdin
            fi
            ;;
        "get")
            if [[ -n "${2:-}" ]]; then
                tsk_get "$2"
            else
                echo "Error: Key required" >&2
                exit 1
            fi
            ;;
        "set")
            if [[ -n "${2:-}" && -n "${3:-}" ]]; then
                tsk_set "$2" "$3"
            else
                echo "Error: Key and value required" >&2
                exit 1
            fi
            ;;
        "keys")
            tsk_keys
            ;;
        "values")
            tsk_values
            ;;
        "export")
            tsk_export "${2:-TSK_}"
            ;;
        "-h"|"--help"|"")
            usage
            ;;
        *)
            echo "Error: Unknown command: $command" >&2
            usage
            exit 1
            ;;
    esac
}

# Run main if executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
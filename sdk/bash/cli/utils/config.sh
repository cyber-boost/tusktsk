#!/usr/bin/env bash

# TuskLang CLI Configuration Utilities
# ====================================
# Configuration management and PeanutConfig integration

set -euo pipefail

# Load helpers and output
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"
source "$(dirname "${BASH_SOURCE[0]}")/output.sh"

# Load the existing PeanutConfig
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
source "${SCRIPT_DIR}/peanut_config.sh"

# Configuration search paths
readonly CONFIG_SEARCH_PATHS=(
    "./peanu.pnt"
    "./peanu.tsk"
    "./peanu.peanuts"
    "../peanu.pnt"
    "../peanu.tsk"
    "../peanu.peanuts"
    "~/.tusklang/config.tsk"
    "/etc/tusklang/config.tsk"
)

# Global configuration cache
declare -A CONFIG_CACHE
declare -A CONFIG_LOADED

##
# Find configuration file in search paths
#
# @param $1 Optional specific path
# @return Path to configuration file or empty string
#
find_config_file() {
    local specific_path="${1:-}"
    
    # If specific path provided, check it first
    if [[ -n "$specific_path" ]]; then
        if file_exists "$specific_path"; then
            echo "$(get_absolute_path "$specific_path")"
            return 0
        fi
    fi
    
    # Check TSK_CONFIG_PATH environment variable
    if [[ -n "${TSK_CONFIG_PATH:-}" ]]; then
        if file_exists "$TSK_CONFIG_PATH"; then
            echo "$(get_absolute_path "$TSK_CONFIG_PATH")"
            return 0
        fi
    fi
    
    # Search in standard paths
    for path in "${CONFIG_SEARCH_PATHS[@]}"; do
        local expanded_path
        expanded_path=$(eval echo "$path")
        
        if file_exists "$expanded_path"; then
            echo "$(get_absolute_path "$expanded_path")"
            return 0
        fi
    done
    
    return 1
}

##
# Load configuration from file
#
# @param $1 Configuration file path
# @return 0 if loaded successfully, 1 if failed
#
load_config() {
    local config_file="$1"
    local abs_path
    
    abs_path=$(get_absolute_path "$config_file")
    
    # Check cache
    if [[ -n "${CONFIG_LOADED[$abs_path]:-}" ]]; then
        log_debug "Using cached config: $abs_path"
        return 0
    fi
    
    log_debug "Loading config: $abs_path"
    
    # Determine file type and load accordingly
    case "$abs_path" in
        *.pnt)
            load_binary_config "$abs_path"
            ;;
        *.tsk|*.peanuts)
            load_text_config "$abs_path"
            ;;
        *)
            log_error "Unsupported config file type: $abs_path"
            return 1
            ;;
    esac
    
    CONFIG_LOADED["$abs_path"]=1
    return 0
}

##
# Load binary configuration (.pnt file)
#
# @param $1 Binary config file path
#
load_binary_config() {
    local config_file="$1"
    
    log_debug "Loading binary config: $config_file"
    
    # Use PeanutConfig to load binary file
    local config_data
    config_data=$(peanut_load_binary "$config_file")
    
    # Parse the loaded data into our cache
    while IFS='=' read -r key value; do
        if [[ -n "$key" && ! "$key" =~ ^# ]]; then
            CONFIG_CACHE["$key"]="$value"
        fi
    done <<< "$config_data"
}

##
# Load text configuration (.tsk or .peanuts file)
#
# @param $1 Text config file path
#
load_text_config() {
    local config_file="$1"
    
    log_debug "Loading text config: $config_file"
    
    # Use PeanutConfig to load text file
    local config_data
    config_data=$(peanut_load "$(dirname "$config_file")")
    
    # Parse the loaded data into our cache
    while IFS='=' read -r key value; do
        if [[ -n "$key" && ! "$key" =~ ^# ]]; then
            CONFIG_CACHE["$key"]="$value"
        fi
    done <<< "$config_data"
}

##
# Get configuration value by key path
#
# @param $1 Key path (e.g., "server.host")
# @param $2 Default value (optional)
# @return Configuration value or default
#
get_config_value() {
    local key_path="$1"
    local default_value="${2:-}"
    
    # Try to load config if not already loaded
    if [[ ${#CONFIG_CACHE[@]} -eq 0 ]]; then
        local config_file
        if config_file=$(find_config_file); then
            load_config "$config_file"
        fi
    fi
    
    # Look for exact match
    if [[ -n "${CONFIG_CACHE[$key_path]:-}" ]]; then
        echo "${CONFIG_CACHE[$key_path]}"
        return 0
    fi
    
    # Look for section match
    for key in "${!CONFIG_CACHE[@]}"; do
        if [[ "$key" == *".$key_path" ]]; then
            echo "${CONFIG_CACHE[$key]}"
            return 0
        fi
    done
    
    # Return default if not found
    echo "$default_value"
}

##
# Set configuration value
#
# @param $1 Key path
# @param $2 Value
# @param $3 Config file path (optional)
#
set_config_value() {
    local key_path="$1"
    local value="$2"
    local config_file="${3:-}"
    
    # Find config file if not provided
    if [[ -z "$config_file" ]]; then
        if ! config_file=$(find_config_file); then
            # Create default config file
            config_file="./peanu.tsk"
            touch "$config_file"
        fi
    fi
    
    # Update cache
    CONFIG_CACHE["$key_path"]="$value"
    
    # Update file (simplified - in production you'd want more sophisticated updating)
    log_debug "Setting config value: $key_path = $value in $config_file"
    
    # For now, just append to file (this is simplified)
    echo "$key_path: \"$value\"" >> "$config_file"
}

##
# Check configuration hierarchy
#
# @param $1 Directory path (optional)
#
check_config_hierarchy() {
    local directory="${1:-.}"
    
    log_info "Checking configuration hierarchy for: $directory"
    
    # Use PeanutConfig to show hierarchy
    peanut_show_hierarchy "$directory"
}

##
# Validate configuration
#
# @param $1 Directory path (optional)
# @return 0 if valid, 1 if invalid
#
validate_config() {
    local directory="${1:-.}"
    local errors=0
    
    log_info "Validating configuration in: $directory"
    
    # Find all config files
    local config_files=()
    for path in "${CONFIG_SEARCH_PATHS[@]}"; do
        local expanded_path
        expanded_path=$(eval echo "$path")
        
        if file_exists "$expanded_path"; then
            config_files+=("$expanded_path")
        fi
    done
    
    # Validate each file
    for config_file in "${config_files[@]}"; do
        log_debug "Validating: $config_file"
        
        case "$config_file" in
            *.pnt)
                if ! validate_binary_config "$config_file"; then
                    ((errors++))
                fi
                ;;
            *.tsk|*.peanuts)
                if ! validate_text_config "$config_file"; then
                    ((errors++))
                fi
                ;;
        esac
    done
    
    if [[ $errors -eq 0 ]]; then
        print_success "Configuration validation passed"
        return 0
    else
        print_error "Configuration validation failed with $errors errors"
        return 1
    fi
}

##
# Validate binary configuration
#
# @param $1 Binary config file path
# @return 0 if valid, 1 if invalid
#
validate_binary_config() {
    local config_file="$1"
    
    # Check if file exists and is readable
    if ! file_exists "$config_file"; then
        print_error "Binary config file not found: $config_file"
        return 1
    fi
    
    # Check file size
    local file_size
    file_size=$(stat -c %s "$config_file" 2>/dev/null || stat -f %z "$config_file" 2>/dev/null || echo "0")
    
    if [[ $file_size -lt 24 ]]; then
        print_error "Binary config file too small: $config_file"
        return 1
    fi
    
    # Try to load it
    if ! load_binary_config "$config_file" 2>/dev/null; then
        print_error "Failed to load binary config: $config_file"
        return 1
    fi
    
    print_success "Binary config valid: $config_file"
    return 0
}

##
# Validate text configuration
#
# @param $1 Text config file path
# @return 0 if valid, 1 if invalid
#
validate_text_config() {
    local config_file="$1"
    
    # Check if file exists and is readable
    if ! file_exists "$config_file"; then
        print_error "Text config file not found: $config_file"
        return 1
    fi
    
    # Try to load it
    if ! load_text_config "$config_file" 2>/dev/null; then
        print_error "Failed to load text config: $config_file"
        return 1
    fi
    
    print_success "Text config valid: $config_file"
    return 0
}

##
# Compile configuration files
#
# @param $1 Directory path (optional)
#
compile_config() {
    local directory="${1:-.}"
    
    log_info "Compiling configuration files in: $directory"
    
    # Find all .tsk and .peanuts files
    local source_files=()
    while IFS= read -r -d '' file; do
        source_files+=("$file")
    done < <(find "$directory" -maxdepth 3 -name "*.tsk" -o -name "*.peanuts" -print0 2>/dev/null)
    
    local compiled=0
    local errors=0
    
    for source_file in "${source_files[@]}"; do
        local binary_file="${source_file%.*}.pnt"
        
        log_debug "Compiling: $source_file -> $binary_file"
        
        if peanut_compile_binary "$source_file" "$binary_file" 2>/dev/null; then
            ((compiled++))
            print_success "Compiled: $(basename "$source_file")"
        else
            ((errors++))
            print_error "Failed to compile: $(basename "$source_file")"
        fi
    done
    
    print_summary "Configuration compilation" "$compiled" "$errors"
}

##
# Generate configuration documentation
#
# @param $1 Directory path (optional)
# @param $2 Output file (optional)
#
generate_config_docs() {
    local directory="${1:-.}"
    local output_file="${2:-}"
    
    log_info "Generating configuration documentation for: $directory"
    
    # Load configuration
    local config_file
    if config_file=$(find_config_file); then
        load_config "$config_file"
    fi
    
    # Generate documentation
    local docs=""
    docs+="# TuskLang Configuration Documentation\n\n"
    docs+="Generated on: $(date)\n\n"
    
    # Group by sections
    local -A sections
    for key in "${!CONFIG_CACHE[@]}"; do
        local section="${key%%.*}"
        sections["$section"]=1
    done
    
    for section in "${!sections[@]}"; do
        docs+="## $section\n\n"
        
        for key in "${!CONFIG_CACHE[@]}"; do
            if [[ "$key" =~ ^${section}\. ]]; then
                local subkey="${key#${section}.}"
                local value="${CONFIG_CACHE[$key]}"
                docs+="- **$subkey**: $value\n"
            fi
        done
        
        docs+="\n"
    done
    
    # Output documentation
    if [[ -n "$output_file" ]]; then
        echo -e "$docs" > "$output_file"
        print_success "Documentation written to: $output_file"
    else
        echo -e "$docs"
    fi
}

##
# Clear configuration cache
#
# @param $1 Directory path (optional)
#
clear_config_cache() {
    local directory="${1:-.}"
    
    log_info "Clearing configuration cache for: $directory"
    
    # Clear global cache
    CONFIG_CACHE=()
    CONFIG_LOADED=()
    
    # Clear PeanutConfig cache
    PEANUT_CACHE=()
    
    print_success "Configuration cache cleared"
}

##
# Show configuration statistics
#
show_config_stats() {
    log_info "Configuration statistics"
    
    # Count loaded configs
    local loaded_count=${#CONFIG_LOADED[@]}
    local cached_count=${#CONFIG_CACHE[@]}
    local peanut_cache_count=${#PEANUT_CACHE[@]}
    
    # Show stats
    print_kv "Loaded configs" "$loaded_count"
    print_kv "Cached values" "$cached_count"
    print_kv "Peanut cache entries" "$peanut_cache_count"
    
    # Show search paths
    print_section "Search Paths"
    for path in "${CONFIG_SEARCH_PATHS[@]}"; do
        local expanded_path
        expanded_path=$(eval echo "$path")
        
        if file_exists "$expanded_path"; then
            print_success "Found: $expanded_path"
        else
            print_info "Not found: $expanded_path"
        fi
    done
}

##
# Get database configuration
#
# @return Database config as JSON
#
get_database_config() {
    local db_type
    local host
    local port
    local database
    local username
    local password
    
    db_type=$(get_config_value "database.type" "sqlite")
    host=$(get_config_value "database.host" "localhost")
    port=$(get_config_value "database.port" "")
    database=$(get_config_value "database.name" "tusklang")
    username=$(get_config_value "database.user" "")
    password=$(get_config_value "database.password" "")
    
    # Set default ports based on type
    if [[ -z "$port" ]]; then
        case "$db_type" in
            "postgres"|"postgresql")
                port="5432"
                ;;
            "mysql")
                port="3306"
                ;;
            "sqlite")
                port=""
                ;;
        esac
    fi
    
    cat << EOF
{
  "type": "$db_type",
  "host": "$host",
  "port": "$port",
  "database": "$database",
  "username": "$username",
  "password": "$password"
}
EOF
}

##
# Get server configuration
#
# @return Server config as JSON
#
get_server_config() {
    local host
    local port
    local workers
    local debug
    
    host=$(get_config_value "server.host" "localhost")
    port=$(get_config_value "server.port" "8080")
    workers=$(get_config_value "server.workers" "4")
    debug=$(get_config_value "server.debug" "false")
    
    cat << EOF
{
  "host": "$host",
  "port": $port,
  "workers": $workers,
  "debug": $debug
}
EOF
}

##
# Get cache configuration
#
# @return Cache config as JSON
#
get_cache_config() {
    local enabled
    local type
    local ttl
    local host
    local port
    
    enabled=$(get_config_value "cache.enabled" "true")
    type=$(get_config_value "cache.type" "memory")
    ttl=$(get_config_value "cache.ttl" "3600")
    host=$(get_config_value "cache.host" "localhost")
    port=$(get_config_value "cache.port" "11211")
    
    cat << EOF
{
  "enabled": $enabled,
  "type": "$type",
  "ttl": $ttl,
  "host": "$host",
  "port": $port
}
EOF
}

##
# Export configuration as environment variables
#
# @param $1 Prefix (default: TSK_)
#
export_config() {
    local prefix="${1:-TSK_}"
    
    log_info "Exporting configuration as environment variables with prefix: $prefix"
    
    local exported=0
    
    for key in "${!CONFIG_CACHE[@]}"; do
        local var_name="${prefix}${key//[.-]/_}"
        var_name="${var_name^^}"  # Uppercase
        local value="${CONFIG_CACHE[$key]}"
        
        export "$var_name=$value"
        ((exported++))
    done
    
    print_success "Exported $exported configuration variables"
}

##
# Show configuration help
#
show_config_help() {
    cat << EOF
🥜 Configuration Commands

Usage: tsk config <command> [options]

Commands:
  get <key.path> [dir]     Get configuration value by path
  check [path]             Check configuration hierarchy
  validate [path]          Validate entire configuration chain
  compile [path]           Auto-compile all peanu.tsk files to .pnt
  docs [path] [output]     Generate configuration documentation
  clear-cache [path]       Clear configuration cache
  stats                    Show configuration performance statistics

Examples:
  tsk config get server.port
  tsk config check .
  tsk config validate
  tsk config compile
  tsk config docs . config.md
  tsk config stats

Configuration Files:
  - peanu.pnt (binary format - fastest)
  - peanu.tsk (TuskLang syntax)
  - peanu.peanuts (simplified syntax)

Search Order:
  1. Command-line specified (--config)
  2. Current directory peanu.pnt/tsk/peanuts
  3. Parent directories
  4. ~/.tusklang/config.tsk
  5. /etc/tusklang/config.tsk
EOF
} 
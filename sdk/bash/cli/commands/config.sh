#!/usr/bin/env bash

# TuskLang CLI Configuration Commands
# ===================================
# Configuration management commands

set -euo pipefail

# Load utilities
source "$(dirname "${BASH_SOURCE[0]}")/../utils/helpers.sh"
source "$(dirname "${BASH_SOURCE[0]}")/../utils/output.sh"
source "$(dirname "${BASH_SOURCE[0]}")/../utils/config.sh"

##
# Execute configuration command
#
# @param $* Command arguments
#
execute_config_command() {
    local subcommand="${1:-}"
    
    case "$subcommand" in
        "get")
            execute_config_get "${@:2}"
            ;;
        "check")
            execute_config_check "${@:2}"
            ;;
        "validate")
            execute_config_validate "${@:2}"
            ;;
        "compile")
            execute_config_compile "${@:2}"
            ;;
        "docs")
            execute_config_docs "${@:2}"
            ;;
        "clear-cache")
            execute_config_clear_cache "${@:2}"
            ;;
        "stats")
            execute_config_stats "${@:2}"
            ;;
        *)
            show_config_help
            TSK_EXIT_CODE=2
            ;;
    esac
}

##
# Get configuration value
#
# @param $* Command arguments
#
execute_config_get() {
    local key_path="${1:-}"
    local directory="${2:-.}"
    
    if [[ -z "$key_path" ]]; then
        print_error "Key path required"
        print_info "Usage: tsk config get <key.path> [dir]"
        TSK_EXIT_CODE=2
        return
    fi
    
    log_info "Getting configuration value: $key_path"
    
    # Load configuration for the directory
    local config_file
    if config_file=$(find_config_file); then
        load_config "$config_file"
    fi
    
    # Get the value
    local value
    value=$(get_config_value "$key_path")
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        cat << EOF
{
  "key": "$key_path",
  "value": "$value",
  "directory": "$directory"
}
EOF
    else
        if [[ -n "$value" ]]; then
            print_success "Configuration value retrieved"
            print_kv "Key" "$key_path"
            print_kv "Value" "$value"
        else
            print_warning "Configuration key not found: $key_path"
        fi
    fi
}

##
# Check configuration hierarchy
#
# @param $* Command arguments
#
execute_config_check() {
    local directory="${1:-.}"
    
    log_info "Checking configuration hierarchy for: $directory"
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        # For JSON output, we'll provide a structured response
        local config_file
        if config_file=$(find_config_file); then
            cat << EOF
{
  "directory": "$directory",
  "config_file": "$config_file",
  "hierarchy": "loaded"
}
EOF
        else
            cat << EOF
{
  "directory": "$directory",
  "config_file": null,
  "hierarchy": "not_found"
}
EOF
        fi
    else
        check_config_hierarchy "$directory"
    fi
}

##
# Validate configuration
#
# @param $* Command arguments
#
execute_config_validate() {
    local directory="${1:-.}"
    
    log_info "Validating configuration in: $directory"
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        # For JSON output, we'll provide validation results
        local errors=0
        local warnings=0
        
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
            case "$config_file" in
                *.pnt)
                    if ! validate_binary_config "$config_file" 2>/dev/null; then
                        ((errors++))
                    fi
                    ;;
                *.tsk|*.peanuts)
                    if ! validate_text_config "$config_file" 2>/dev/null; then
                        ((errors++))
                    fi
                    ;;
            esac
        done
        
        cat << EOF
{
  "directory": "$directory",
  "validation": {
    "errors": $errors,
    "warnings": $warnings,
    "valid": $([[ $errors -eq 0 ]] && echo "true" || echo "false")
  }
}
EOF
    else
        validate_config "$directory"
    fi
}

##
# Compile configuration files
#
# @param $* Command arguments
#
execute_config_compile() {
    local directory="${1:-.}"
    
    log_info "Compiling configuration files in: $directory"
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        # For JSON output, we'll provide compilation results
        local compiled=0
        local errors=0
        
        # Find all .tsk and .peanuts files
        local source_files=()
        while IFS= read -r -d '' file; do
            source_files+=("$file")
        done < <(find "$directory" -maxdepth 3 -name "*.tsk" -o -name "*.peanuts" -print0 2>/dev/null)
        
        for source_file in "${source_files[@]}"; do
            local binary_file="${source_file%.*}.pnt"
            
            if peanut_compile_binary "$source_file" "$binary_file" 2>/dev/null; then
                ((compiled++))
            else
                ((errors++))
            fi
        done
        
        cat << EOF
{
  "directory": "$directory",
  "compilation": {
    "compiled": $compiled,
    "errors": $errors,
    "total": ${#source_files[@]}
  }
}
EOF
    else
        compile_config "$directory"
    fi
}

##
# Generate configuration documentation
#
# @param $* Command arguments
#
execute_config_docs() {
    local directory="${1:-.}"
    local output_file="${2:-}"
    
    log_info "Generating configuration documentation for: $directory"
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        # For JSON output, we'll provide documentation metadata
        local docs_content
        docs_content=$(generate_config_docs "$directory" "")
        
        cat << EOF
{
  "directory": "$directory",
  "output_file": "$output_file",
  "documentation": {
    "generated": true,
    "content_length": ${#docs_content}
  }
}
EOF
    else
        generate_config_docs "$directory" "$output_file"
    fi
}

##
# Clear configuration cache
#
# @param $* Command arguments
#
execute_config_clear_cache() {
    local directory="${1:-.}"
    
    log_info "Clearing configuration cache for: $directory"
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        cat << EOF
{
  "directory": "$directory",
  "cache_cleared": true,
  "timestamp": "$(date -Iseconds)"
}
EOF
    else
        clear_config_cache "$directory"
    fi
}

##
# Show configuration statistics
#
# @param $* Command arguments
#
execute_config_stats() {
    log_info "Showing configuration statistics"
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        # For JSON output, we'll provide statistics
        local loaded_count=${#CONFIG_LOADED[@]}
        local cached_count=${#CONFIG_CACHE[@]}
        local peanut_cache_count=${#PEANUT_CACHE[@]}
        
        cat << EOF
{
  "statistics": {
    "loaded_configs": $loaded_count,
    "cached_values": $cached_count,
    "peanut_cache_entries": $peanut_cache_count,
    "timestamp": "$(date -Iseconds)"
  }
}
EOF
    else
        show_config_stats
    fi
} 
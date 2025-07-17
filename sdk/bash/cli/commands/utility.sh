#!/usr/bin/env bash

# TuskLang CLI Utility Commands
# =============================
# Utility and helper commands

set -euo pipefail

# Load utilities
source "$(dirname "${BASH_SOURCE[0]}")/../utils/helpers.sh"
source "$(dirname "${BASH_SOURCE[0]}")/../utils/output.sh"
source "$(dirname "${BASH_SOURCE[0]}")/../utils/config.sh"

##
# Execute utility command
#
# @param $* Command arguments
#
execute_utility_command() {
    local subcommand="${1:-}"
    
    case "$subcommand" in
        "parse")
            execute_parse_command "${@:2}"
            ;;
        "validate")
            execute_validate_command "${@:2}"
            ;;
        "convert")
            execute_convert_command "${@:2}"
            ;;
        "get")
            execute_get_command "${@:2}"
            ;;
        "set")
            execute_set_command "${@:2}"
            ;;
        *)
            show_utility_help
            TSK_EXIT_CODE=2
            ;;
    esac
}

##
# Parse TSK file
#
# @param $* Command arguments
#
execute_parse_command() {
    local file="${1:-}"
    
    if [[ -z "$file" ]]; then
        print_error "File required"
        print_info "Usage: tsk parse <file>"
        TSK_EXIT_CODE=2
        return
    fi
    
    if ! file_exists "$file"; then
        print_error "File not found: $file"
        TSK_EXIT_CODE=3
        return
    fi
    
    log_info "Parsing TSK file: $file"
    print_running "Parsing file structure"
    
    # Load and parse the file
    if load_config "$file"; then
        print_success "File parsed successfully"
        
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            # Output as JSON
            echo "{"
            echo "  \"file\": \"$file\","
            echo "  \"parsed\": true,"
            echo "  \"sections\": ["
            
            local first=true
            for key in "${!CONFIG_CACHE[@]}"; do
                if [[ "$first" == "true" ]]; then
                    first=false
                else
                    echo ","
                fi
                echo "    \"$key\""
            done
            
            echo "  ]"
            echo "}"
        else
            # Output as formatted text
            print_kv "File" "$file"
            print_kv "Sections" "${#CONFIG_CACHE[@]}"
            
            # Group by sections
            local -A sections
            for key in "${!CONFIG_CACHE[@]}"; do
                local section="${key%%.*}"
                sections["$section"]=1
            done
            
            for section in "${!sections[@]}"; do
                print_subsection "$section"
                for key in "${!CONFIG_CACHE[@]}"; do
                    if [[ "$key" =~ ^${section}\. ]]; then
                        local subkey="${key#${section}.}"
                        local value="${CONFIG_CACHE[$key]}"
                        print_kv "  $subkey" "$value"
                    fi
                done
            done
        fi
    else
        print_error "Failed to parse file"
        TSK_EXIT_CODE=1
    fi
}

##
# Validate TSK file
#
# @param $* Command arguments
#
execute_validate_command() {
    local file="${1:-}"
    
    if [[ -z "$file" ]]; then
        print_error "File required"
        print_info "Usage: tsk validate <file>"
        TSK_EXIT_CODE=2
        return
    fi
    
    if ! file_exists "$file"; then
        print_error "File not found: $file"
        TSK_EXIT_CODE=3
        return
    fi
    
    log_info "Validating TSK file: $file"
    print_running "Validating file syntax and structure"
    
    # Validate the file
    if validate_text_config "$file"; then
        print_success "File validation passed"
        
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            cat << EOF
{
  "file": "$file",
  "valid": true,
  "errors": 0,
  "warnings": 0
}
EOF
        else
            print_kv "File" "$file"
            print_kv "Status" "valid"
            print_kv "Errors" "0"
        fi
    else
        print_error "File validation failed"
        
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            cat << EOF
{
  "file": "$file",
  "valid": false,
  "errors": 1,
  "warnings": 0
}
EOF
        fi
        
        TSK_EXIT_CODE=1
    fi
}

##
# Convert between formats
#
# @param $* Command arguments
#
execute_convert_command() {
    local input_file=""
    local output_file=""
    local input_format=""
    local output_format=""
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case "$1" in
            -i|--input)
                input_file="$2"
                shift 2
                ;;
            -o|--output)
                output_file="$2"
                shift 2
                ;;
            --from)
                input_format="$2"
                shift 2
                ;;
            --to)
                output_format="$2"
                shift 2
                ;;
            *)
                print_error "Unknown option: $1"
                print_info "Usage: tsk convert -i <in> -o <out> [--from <format>] [--to <format>]"
                TSK_EXIT_CODE=2
                return
                ;;
        esac
    done
    
    if [[ -z "$input_file" || -z "$output_file" ]]; then
        print_error "Input and output files required"
        print_info "Usage: tsk convert -i <in> -o <out> [--from <format>] [--to <format>]"
        TSK_EXIT_CODE=2
        return
    fi
    
    if ! file_exists "$input_file"; then
        print_error "Input file not found: $input_file"
        TSK_EXIT_CODE=3
        return
    fi
    
    log_info "Converting file: $input_file -> $output_file"
    print_running "Converting between formats"
    
    # Determine formats from file extensions if not specified
    if [[ -z "$input_format" ]]; then
        input_format="${input_file##*.}"
    fi
    
    if [[ -z "$output_format" ]]; then
        output_format="${output_file##*.}"
    fi
    
    # Perform conversion
    case "$input_format" in
        "tsk"|"peanuts")
            case "$output_format" in
                "pnt")
                    if peanut_compile_binary "$input_file" "$output_file"; then
                        print_success "Conversion completed successfully"
                        print_kv "Input" "$input_file ($input_format)"
                        print_kv "Output" "$output_file ($output_format)"
                    else
                        print_error "Conversion failed"
                        TSK_EXIT_CODE=1
                    fi
                    ;;
                *)
                    print_error "Unsupported output format: $output_format"
                    TSK_EXIT_CODE=2
                    ;;
            esac
            ;;
        *)
            print_error "Unsupported input format: $input_format"
            TSK_EXIT_CODE=2
            ;;
    esac
}

##
# Get value from TSK file
#
# @param $* Command arguments
#
execute_get_command() {
    local file="${1:-}"
    local key_path="${2:-}"
    
    if [[ -z "$file" || -z "$key_path" ]]; then
        print_error "File and key path required"
        print_info "Usage: tsk get <file> <key.path>"
        TSK_EXIT_CODE=2
        return
    fi
    
    if ! file_exists "$file"; then
        print_error "File not found: $file"
        TSK_EXIT_CODE=3
        return
    fi
    
    log_info "Getting value from file: $file, key: $key_path"
    
    # Load the file
    if load_config "$file"; then
        # Get the value
        local value
        value=$(get_config_value "$key_path")
        
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            cat << EOF
{
  "file": "$file",
  "key": "$key_path",
  "value": "$value",
  "found": $([[ -n "$value" ]] && echo "true" || echo "false")
}
EOF
        else
            if [[ -n "$value" ]]; then
                print_success "Value retrieved successfully"
                print_kv "File" "$file"
                print_kv "Key" "$key_path"
                print_kv "Value" "$value"
            else
                print_warning "Key not found: $key_path"
                print_kv "File" "$file"
                print_kv "Key" "$key_path"
                print_kv "Value" "(not found)"
            fi
        fi
    else
        print_error "Failed to load file"
        TSK_EXIT_CODE=1
    fi
}

##
# Set value in TSK file
#
# @param $* Command arguments
#
execute_set_command() {
    local file="${1:-}"
    local key_path="${2:-}"
    local value="${3:-}"
    
    if [[ -z "$file" || -z "$key_path" ]]; then
        print_error "File and key path required"
        print_info "Usage: tsk set <file> <key.path> <value>"
        TSK_EXIT_CODE=2
        return
    fi
    
    log_info "Setting value in file: $file, key: $key_path, value: $value"
    print_running "Setting configuration value"
    
    # Set the value
    if set_config_value "$key_path" "$value" "$file"; then
        print_success "Value set successfully"
        
        if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
            cat << EOF
{
  "file": "$file",
  "key": "$key_path",
  "value": "$value",
  "set": true
}
EOF
        else
            print_kv "File" "$file"
            print_kv "Key" "$key_path"
            print_kv "Value" "$value"
        fi
    else
        print_error "Failed to set value"
        TSK_EXIT_CODE=1
    fi
}

##
# Show utility help
#
show_utility_help() {
    cat << EOF
🛠️ Utility Commands

Usage: tsk <command> [options]

Commands:
  parse <file>               Parse TSK file and show structure
  validate <file>            Validate TSK file syntax
  convert -i <in> -o <out>   Convert between formats
  get <file> <key.path>      Get value from TSK file
  set <file> <key.path> <val> Set value in TSK file

Examples:
  tsk parse config.tsk
  tsk validate app.tsk
  tsk convert -i config.tsk -o config.pnt
  tsk get config.tsk server.port
  tsk set config.tsk server.port 8080

Supported Formats:
  - .tsk (TuskLang syntax)
  - .peanuts (simplified syntax)
  - .pnt (binary format)
EOF
} 
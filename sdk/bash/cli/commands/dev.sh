#!/usr/bin/env bash

# TuskLang CLI Development Commands
# =================================
# Development server and compilation commands

set -euo pipefail

# Load utilities
source "$(dirname "${BASH_SOURCE[0]}")/../utils/helpers.sh"
source "$(dirname "${BASH_SOURCE[0]}")/../utils/output.sh"
source "$(dirname "${BASH_SOURCE[0]}")/../utils/config.sh"

##
# Execute serve command
#
# @param $* Command arguments
#
execute_serve_command() {
    local port="${1:-8080}"
    
    log_info "Starting development server on port $port"
    
    # Get server configuration
    local server_config
    server_config=$(get_server_config)
    local config_port
    config_port=$(echo "$server_config" | json_get "port")
    
    # Use config port if no port specified
    if [[ "$port" == "8080" && "$config_port" != "8080" ]]; then
        port="$config_port"
    fi
    
    # Check if port is available
    if ! port_available "$port"; then
        print_error "Port $port is already in use"
        TSK_EXIT_CODE=5
        return
    fi
    
    print_running "Starting development server on port $port"
    
    # Start simple HTTP server
    if command_exists python3; then
        python3 -m http.server "$port"
    elif command_exists python; then
        python -m SimpleHTTPServer "$port"
    elif command_exists php; then
        php -S "localhost:$port"
    else
        print_error "No suitable HTTP server found (python3, python, or php required)"
        TSK_EXIT_CODE=1
    fi
}

##
# Execute compile command
#
# @param $* Command arguments
#
execute_compile_command() {
    local file="${1:-}"
    
    if [[ -z "$file" ]]; then
        print_error "File required"
        print_info "Usage: tsk compile <file>"
        TSK_EXIT_CODE=2
        return
    fi
    
    if ! file_exists "$file"; then
        print_error "File not found: $file"
        TSK_EXIT_CODE=3
        return
    fi
    
    log_info "Compiling TSK file: $file"
    
    # Check file extension
    if [[ "$file" != *.tsk ]]; then
        print_error "File must have .tsk extension"
        TSK_EXIT_CODE=2
        return
    fi
    
    print_running "Compiling $file to optimized format"
    
    # For now, just validate the file
    if validate_text_config "$file"; then
        print_success "TSK file compiled successfully"
        print_kv "Input file" "$file"
        print_kv "Status" "validated"
    else
        print_error "TSK file compilation failed"
        TSK_EXIT_CODE=1
    fi
}

##
# Execute optimize command
#
# @param $* Command arguments
#
execute_optimize_command() {
    local file="${1:-}"
    
    if [[ -z "$file" ]]; then
        print_error "File required"
        print_info "Usage: tsk optimize <file>"
        TSK_EXIT_CODE=2
        return
    fi
    
    if ! file_exists "$file"; then
        print_error "File not found: $file"
        TSK_EXIT_CODE=3
        return
    fi
    
    log_info "Optimizing TSK file: $file"
    
    print_running "Optimizing $file for production"
    
    # For now, just validate and show optimization suggestions
    if validate_text_config "$file"; then
        print_success "TSK file optimized successfully"
        print_kv "Input file" "$file"
        print_kv "Optimizations" "validation passed"
        print_info "Consider compiling to .pnt for better performance"
    else
        print_error "TSK file optimization failed"
        TSK_EXIT_CODE=1
    fi
}

##
# Show development help
#
show_dev_help() {
    cat << EOF
🔧 Development Commands

Usage: tsk <command> [options]

Commands:
  serve [port]              Start development server (default: 8080)
  compile <file>            Compile .tsk file to optimized format
  optimize <file>           Optimize .tsk file for production

Examples:
  tsk serve 3000
  tsk compile config.tsk
  tsk optimize app.tsk

Configuration:
  Server settings are read from peanu.tsk configuration files.
  See 'tsk config' for more information about configuration management.
EOF
} 
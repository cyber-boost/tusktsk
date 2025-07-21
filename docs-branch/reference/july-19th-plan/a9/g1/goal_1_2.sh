#!/bin/bash

# Goal 1.2 Implementation - Configuration Management System
# Priority: Medium
# Description: Goal 2 for Bash agent a9 goal 1

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_1_2"
CONFIG_DIR="/tmp/goal_1_2_config"
DEFAULT_CONFIG_FILE="$CONFIG_DIR/default.conf"
USER_CONFIG_FILE="$CONFIG_DIR/user.conf"
BACKUP_DIR="$CONFIG_DIR/backups"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

# Configuration management functions
create_default_config() {
    log_info "Creating default configuration"
    mkdir -p "$CONFIG_DIR"
    
    cat > "$DEFAULT_CONFIG_FILE" << 'EOF'
# Default Configuration for Goal 1.2
# This file contains default settings

# Application settings
APP_NAME="Goal 1.2 Config Manager"
APP_VERSION="1.0.0"
DEBUG_MODE=false

# File paths
LOG_DIR="/tmp/goal_1_2_logs"
DATA_DIR="/tmp/goal_1_2_data"
CACHE_DIR="/tmp/goal_1_2_cache"

# Performance settings
MAX_THREADS=4
TIMEOUT=30
RETRY_ATTEMPTS=3

# Security settings
ENABLE_ENCRYPTION=true
KEY_LENGTH=256
SESSION_TIMEOUT=3600

# Network settings
HOST="localhost"
PORT=8080
PROTOCOL="http"
EOF
    
    log_success "Default configuration created"
}

load_config() {
    local config_file="$1"
    if [[ -f "$config_file" ]]; then
        log_info "Loading configuration from $config_file"
        # Source the configuration file
        source "$config_file"
        log_success "Configuration loaded successfully"
        return 0
    else
        log_error "Configuration file $config_file not found"
        return 1
    fi
}

validate_config() {
    log_info "Validating configuration"
    
    # Check required variables
    local required_vars=("APP_NAME" "APP_VERSION" "MAX_THREADS" "TIMEOUT")
    local missing_vars=()
    
    for var in "${required_vars[@]}"; do
        if [[ -z "${!var:-}" ]]; then
            missing_vars+=("$var")
        fi
    done
    
    if [[ ${#missing_vars[@]} -gt 0 ]]; then
        log_error "Missing required configuration variables: ${missing_vars[*]}"
        return 1
    fi
    
    # Validate numeric values
    if ! [[ "$MAX_THREADS" =~ ^[0-9]+$ ]] || [[ $MAX_THREADS -lt 1 ]]; then
        log_error "Invalid MAX_THREADS value: $MAX_THREADS"
        return 1
    fi
    
    if ! [[ "$TIMEOUT" =~ ^[0-9]+$ ]] || [[ $TIMEOUT -lt 1 ]]; then
        log_error "Invalid TIMEOUT value: $TIMEOUT"
        return 1
    fi
    
    log_success "Configuration validation passed"
    return 0
}

backup_config() {
    log_info "Creating configuration backup"
    mkdir -p "$BACKUP_DIR"
    
    local timestamp=$(date '+%Y%m%d_%H%M%S')
    local backup_file="$BACKUP_DIR/config_backup_$timestamp.conf"
    
    if [[ -f "$USER_CONFIG_FILE" ]]; then
        cp "$USER_CONFIG_FILE" "$backup_file"
        log_success "Configuration backed up to $backup_file"
    else
        log_warning "No user configuration to backup"
    fi
}

create_user_config() {
    log_info "Creating user configuration"
    
    cat > "$USER_CONFIG_FILE" << 'EOF'
# User Configuration for Goal 1.2
# Override default settings here

# Application settings
APP_NAME="Custom Goal 1.2 Config Manager"
DEBUG_MODE=true

# Performance settings
MAX_THREADS=8
TIMEOUT=60

# Custom settings
CUSTOM_FEATURE=true
CUSTOM_TIMEOUT=120
EOF
    
    log_success "User configuration created"
}

display_config() {
    log_info "Current configuration:"
    echo "----------------------------------------"
    echo "APP_NAME: ${APP_NAME:-'Not set'}"
    echo "APP_VERSION: ${APP_VERSION:-'Not set'}"
    echo "DEBUG_MODE: ${DEBUG_MODE:-'Not set'}"
    echo "MAX_THREADS: ${MAX_THREADS:-'Not set'}"
    echo "TIMEOUT: ${TIMEOUT:-'Not set'}"
    echo "ENABLE_ENCRYPTION: ${ENABLE_ENCRYPTION:-'Not set'}"
    echo "CUSTOM_FEATURE: ${CUSTOM_FEATURE:-'Not set'}"
    echo "CUSTOM_TIMEOUT: ${CUSTOM_TIMEOUT:-'Not set'}"
    echo "----------------------------------------"
}

# Main execution function
main() {
    log_info "Starting Goal 1.2 implementation"
    
    # Create default configuration
    create_default_config
    
    # Load default configuration
    if ! load_config "$DEFAULT_CONFIG_FILE"; then
        log_error "Failed to load default configuration"
        exit 1
    fi
    
    # Display initial configuration
    display_config
    
    # Validate configuration
    if ! validate_config; then
        log_error "Configuration validation failed"
        exit 1
    fi
    
    # Create user configuration
    create_user_config
    
    # Backup existing configuration
    backup_config
    
    # Load user configuration (overrides defaults)
    if load_config "$USER_CONFIG_FILE"; then
        log_info "User configuration loaded and merged with defaults"
    else
        log_warning "Failed to load user configuration, using defaults only"
    fi
    
    # Display final configuration
    display_config
    
    # Validate final configuration
    if validate_config; then
        log_success "Final configuration validation passed"
    else
        log_error "Final configuration validation failed"
        exit 1
    fi
    
    # Simulate configuration usage
    log_info "Simulating configuration usage"
    for i in $(seq 1 "$MAX_THREADS"); do
        log_info "Processing thread $i with timeout $TIMEOUT seconds"
        sleep 0.1  # Simulate work
    done
    
    log_success "Goal 1.2 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi 
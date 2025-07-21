#!/bin/bash

# Goal 1.1 Implementation - Advanced Error Handling and Logging System
# Priority: High
# Description: Goal 1 for Bash agent a9 goal 1

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_1_1"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
ERROR_LOG="/tmp/${SCRIPT_NAME}_errors.log"
LOCK_FILE="/tmp/${SCRIPT_NAME}.lock"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE" "$ERROR_LOG"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

# File locking mechanism
acquire_lock() {
    if [[ -f "$LOCK_FILE" ]]; then
        local lock_pid=$(cat "$LOCK_FILE" 2>/dev/null || echo "")
        if [[ -n "$lock_pid" ]] && kill -0 "$lock_pid" 2>/dev/null; then
            log_error "Script is already running with PID $lock_pid"
            exit 1
        else
            log_warning "Removing stale lock file"
            rm -f "$LOCK_FILE"
        fi
    fi
    echo $$ > "$LOCK_FILE"
    log_info "Lock acquired"
}

release_lock() {
    rm -f "$LOCK_FILE"
    log_info "Lock released"
}

# Error handling
handle_error() {
    local exit_code=$?
    local line_number=$1
    log_error "Error occurred in line $line_number (exit code: $exit_code)"
    release_lock
    exit "$exit_code"
}

# Set up error handling
trap 'handle_error $LINENO' ERR
trap 'release_lock' EXIT

# Main execution function
main() {
    log_info "Starting Goal 1.1 implementation"
    
    # Validate environment
    if [[ ! -d "/tmp" ]]; then
        log_error "Required directory /tmp does not exist"
        exit 1
    fi
    
    # Create test data
    log_info "Creating test data structure"
    local test_dir="/tmp/goal_1_1_test"
    mkdir -p "$test_dir"
    
    # Simulate complex operation with error handling
    log_info "Performing complex file operations"
    
    # Create test files
    for i in {1..5}; do
        echo "Test data $i" > "$test_dir/file_$i.txt"
        if [[ $? -eq 0 ]]; then
            log_success "Created file_$i.txt"
        else
            log_error "Failed to create file_$i.txt"
        fi
    done
    
    # Perform data processing
    log_info "Processing data files"
    local processed_count=0
    for file in "$test_dir"/*.txt; do
        if [[ -f "$file" ]]; then
            # Simulate processing
            local processed_file="${file%.txt}_processed.txt"
            cp "$file" "$processed_file"
            if [[ $? -eq 0 ]]; then
                log_success "Processed $(basename "$file")"
                ((processed_count++))
            else
                log_error "Failed to process $(basename "$file")"
            fi
        fi
    done
    
    # Validate results
    log_info "Validating results"
    if [[ $processed_count -eq 5 ]]; then
        log_success "All files processed successfully"
    else
        log_warning "Only $processed_count out of 5 files processed"
    fi
    
    # Cleanup
    log_info "Cleaning up test data"
    rm -rf "$test_dir"
    
    log_success "Goal 1.1 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    acquire_lock
    main "$@"
fi 
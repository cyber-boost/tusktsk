#!/bin/bash

# Goal 5.1 Implementation - Security and Access Control System
# Priority: High
# Description: Goal 1 for Bash agent a9 goal 5

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_5_1"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
LOCK_FILE="/tmp/${SCRIPT_NAME}.lock"
RESULTS_DIR="/tmp/goal_5_1_results"
CONFIG_FILE="/tmp/goal_5_1_config.conf"
USERS_DB="/tmp/goal_5_1_results/users.db"
AUDIT_LOG="/tmp/goal_5_1_results/audit.log"

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
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
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

# Security functions
create_config() {
    log_info "Creating security configuration"
    mkdir -p "$RESULTS_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# Security and Access Control Configuration

# Password policy
MIN_PASSWORD_LENGTH=8
REQUIRE_UPPERCASE=true
REQUIRE_LOWERCASE=true
REQUIRE_NUMBERS=true
REQUIRE_SPECIAL_CHARS=true
PASSWORD_EXPIRY_DAYS=90

# Session settings
SESSION_TIMEOUT=3600
MAX_LOGIN_ATTEMPTS=3
LOCKOUT_DURATION=900

# Access control
DEFAULT_ROLE="user"
ADMIN_ROLE="admin"
GUEST_ROLE="guest"

# File permissions
SECURE_FILE_PERMS=600
SECURE_DIR_PERMS=700

# Audit settings
AUDIT_ENABLED=true
AUDIT_RETENTION_DAYS=30
LOG_FAILED_ATTEMPTS=true
LOG_SUCCESSFUL_LOGINS=true
EOF
    
    log_success "Configuration created"
}

# Password hashing function
hash_password() {
    local password="$1"
    local salt=$(openssl rand -hex 8)
    local hash=$(echo -n "${password}${salt}" | sha256sum | cut -d' ' -f1)
    echo "${hash}:${salt}"
}

# Password validation
validate_password() {
    local password="$1"
    
    # Check minimum length
    if [[ ${#password} -lt $MIN_PASSWORD_LENGTH ]]; then
        echo "Password must be at least $MIN_PASSWORD_LENGTH characters long"
        return 1
    fi
    
    # Check for uppercase
    if [[ "$REQUIRE_UPPERCASE" == "true" ]] && ! echo "$password" | grep -q '[A-Z]'; then
        echo "Password must contain at least one uppercase letter"
        return 1
    fi
    
    # Check for lowercase
    if [[ "$REQUIRE_LOWERCASE" == "true" ]] && ! echo "$password" | grep -q '[a-z]'; then
        echo "Password must contain at least one lowercase letter"
        return 1
    fi
    
    # Check for numbers
    if [[ "$REQUIRE_NUMBERS" == "true" ]] && ! echo "$password" | grep -q '[0-9]'; then
        echo "Password must contain at least one number"
        return 1
    fi
    
    # Check for special characters
    if [[ "$REQUIRE_SPECIAL_CHARS" == "true" ]] && ! echo "$password" | grep -q '[!@#$%^&*(),.?":{}|<>]'; then
        echo "Password must contain at least one special character"
        return 1
    fi
    
    return 0
}

# User management functions
create_user() {
    local username="$1"
    local password="$2"
    local role="${3:-$DEFAULT_ROLE}"
    
    # Validate username
    if [[ -z "$username" ]] || [[ ! "$username" =~ ^[a-zA-Z0-9_]+$ ]]; then
        log_error "Invalid username: $username"
        return 1
    fi
    
    # Check if user exists
    if grep -q "^${username}:" "$USERS_DB" 2>/dev/null; then
        log_error "User $username already exists"
        return 1
    fi
    
    # Validate password
    local password_error=$(validate_password "$password")
    if [[ $? -ne 0 ]]; then
        log_error "Password validation failed: $password_error"
        return 1
    fi
    
    # Hash password
    local hashed_password=$(hash_password "$password")
    local created_at=$(date '+%Y-%m-%d %H:%M:%S')
    local expiry_date=$(date -d "+$PASSWORD_EXPIRY_DAYS days" '+%Y-%m-%d')
    
    # Add user to database
    echo "${username}:${hashed_password}:${role}:${created_at}:${expiry_date}:0:0" >> "$USERS_DB"
    
    # Set secure permissions
    chmod 600 "$USERS_DB"
    
    log_success "User $username created with role $role"
    audit_log "USER_CREATED" "$username" "User account created"
}

# Authentication function
authenticate_user() {
    local username="$1"
    local password="$2"
    
    # Check if user exists
    local user_line=$(grep "^${username}:" "$USERS_DB" 2>/dev/null || echo "")
    if [[ -z "$user_line" ]]; then
        audit_log "LOGIN_FAILED" "$username" "User not found"
        return 1
    fi
    
    # Parse user data
    IFS=':' read -r stored_username stored_hash stored_role created_at expiry_date failed_attempts last_login <<< "$user_line"
    
    # Check if account is locked
    if [[ $failed_attempts -ge $MAX_LOGIN_ATTEMPTS ]]; then
        local lockout_time=$(($last_login + $LOCKOUT_DURATION))
        local current_time=$(date +%s 2>/dev/null || echo "0")
        if [[ $current_time -lt $lockout_time ]]; then
            audit_log "LOGIN_FAILED" "$username" "Account locked"
            return 1
        else
            # Reset failed attempts after lockout period
            failed_attempts=0
        fi
    fi
    
    # Verify password
    local salt=$(echo "$stored_hash" | cut -d':' -f2)
    local input_hash=$(echo -n "${password}${salt}" | sha256sum | cut -d' ' -f1)
    local stored_hash_only=$(echo "$stored_hash" | cut -d':' -f1)
    
    if [[ "$input_hash" == "$stored_hash_only" ]]; then
        # Successful login
        local current_time=$(date +%s 2>/dev/null || echo "0")
        sed -i "s/^${username}:.*$/${username}:${stored_hash}:${stored_role}:${created_at}:${expiry_date}:0:${current_time}/" "$USERS_DB"
        
        audit_log "LOGIN_SUCCESS" "$username" "Successful login"
        log_success "User $username authenticated successfully"
        return 0
    else
        # Failed login
        local new_failed_attempts=$((failed_attempts + 1))
        sed -i "s/^${username}:.*$/${username}:${stored_hash}:${stored_role}:${created_at}:${expiry_date}:${new_failed_attempts}:${last_login}/" "$USERS_DB"
        
        audit_log "LOGIN_FAILED" "$username" "Invalid password (attempt ${new_failed_attempts})"
        log_warning "Failed login attempt for user $username"
        return 1
    fi
}

# Authorization function
check_permission() {
    local username="$1"
    local required_role="$2"
    local action="$3"
    
    # Get user role
    local user_line=$(grep "^${username}:" "$USERS_DB" 2>/dev/null || echo "")
    if [[ -z "$user_line" ]]; then
        return 1
    fi
    
    IFS=':' read -r stored_username stored_hash stored_role created_at expiry_date failed_attempts last_login <<< "$user_line"
    
    # Check role hierarchy
    case "$required_role" in
        "guest")
            return 0  # Everyone can access guest level
            ;;
        "user")
            if [[ "$stored_role" == "user" ]] || [[ "$stored_role" == "admin" ]]; then
                return 0
            fi
            ;;
        "admin")
            if [[ "$stored_role" == "admin" ]]; then
                return 0
            fi
            ;;
    esac
    
    audit_log "PERMISSION_DENIED" "$username" "Access denied to $action (required: $required_role, user: $stored_role)"
    return 1
}

# Audit logging function
audit_log() {
    local event="$1"
    local username="$2"
    local details="$3"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local ip_address=$(hostname -I | awk '{print $1}')
    
    if [[ "$AUDIT_ENABLED" == "true" ]]; then
        echo "$timestamp|$event|$username|$ip_address|$details" >> "$AUDIT_LOG"
    fi
}

# User management interface
manage_users() {
    log_info "Starting user management interface"
    
    # Create sample users
    create_user "admin" "Admin123!" "admin"
    create_user "user1" "User123!" "user"
    create_user "guest1" "Guest123!" "guest"
    
    # Test authentication
    log_info "Testing authentication system"
    
    if authenticate_user "admin" "Admin123!"; then
        log_success "Admin authentication successful"
    else
        log_error "Admin authentication failed"
    fi
    
    if authenticate_user "user1" "User123!"; then
        log_success "User1 authentication successful"
    else
        log_error "User1 authentication failed"
    fi
    
    # Test authorization
    log_info "Testing authorization system"
    
    if check_permission "admin" "admin" "system_config"; then
        log_success "Admin has admin permissions"
    else
        log_error "Admin authorization failed"
    fi
    
    if check_permission "user1" "user" "read_data"; then
        log_success "User1 has user permissions"
    else
        log_error "User1 authorization failed"
    fi
    
    if check_permission "guest1" "guest" "public_access"; then
        log_success "Guest1 has guest permissions"
    else
        log_error "Guest1 authorization failed"
    fi
    
    # Test failed authentication
    log_info "Testing failed authentication"
    if ! authenticate_user "admin" "WrongPassword"; then
        log_success "Failed authentication properly rejected"
    else
        log_error "Failed authentication incorrectly accepted"
    fi
}

# Generate security report
generate_report() {
    log_info "Generating security report"
    local report_file="$RESULTS_DIR/security_report.txt"
    
    {
        echo "=========================================="
        echo "SECURITY AND ACCESS CONTROL REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== User Accounts ==="
        if [[ -f "$USERS_DB" ]]; then
            echo "Total users: $(wc -l < "$USERS_DB")"
            echo ""
            echo "User Details:"
            while IFS=':' read -r username hash role created expiry failed last; do
                echo "  Username: $username"
                echo "  Role: $role"
                echo "  Created: $created"
                echo "  Password expires: $expiry"
                echo "  Failed attempts: $failed"
                echo "  Last login: $(date -d "@$last" '+%Y-%m-%d %H:%M:%S' 2>/dev/null || echo 'Never')"
                echo ""
            done < "$USERS_DB"
        else
            echo "No users found"
        fi
        
        echo "=== Audit Log ==="
        if [[ -f "$AUDIT_LOG" ]]; then
            echo "Total audit entries: $(wc -l < "$AUDIT_LOG")"
            echo ""
            echo "Recent audit entries:"
            tail -10 "$AUDIT_LOG" | while IFS='|' read -r timestamp event username ip details; do
                echo "  $timestamp - $event - $username - $details"
            done
        else
            echo "No audit entries found"
        fi
        
        echo "=== Security Configuration ==="
        echo "Password minimum length: $MIN_PASSWORD_LENGTH"
        echo "Password expiry days: $PASSWORD_EXPIRY_DAYS"
        echo "Session timeout: $SESSION_TIMEOUT seconds"
        echo "Max login attempts: $MAX_LOGIN_ATTEMPTS"
        echo "Lockout duration: $LOCKOUT_DURATION seconds"
        echo "Audit enabled: $AUDIT_ENABLED"
        
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Security report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 5.1 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Initialize user database
    touch "$USERS_DB"
    chmod 600 "$USERS_DB"
    
    # Initialize audit log
    touch "$AUDIT_LOG"
    chmod 600 "$AUDIT_LOG"
    
    # Run user management and testing
    manage_users
    
    # Generate comprehensive report
    generate_report
    
    log_success "Goal 5.1 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    acquire_lock
    main "$@"
fi 
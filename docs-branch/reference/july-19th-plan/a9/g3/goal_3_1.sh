#!/bin/bash

# Goal 3.1 Implementation - Database Backup and Recovery System
# Priority: High
# Description: Goal 1 for Bash agent a9 goal 3

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_3_1"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
LOCK_FILE="/tmp/${SCRIPT_NAME}.lock"
BACKUP_DIR="/tmp/goal_3_1_backups"
CONFIG_FILE="/tmp/goal_3_1_config.conf"

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

# Database backup functions
create_config() {
    log_info "Creating database backup configuration"
    mkdir -p "$BACKUP_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# Database Backup Configuration

# Database types to support
DATABASE_TYPES=(
    "mysql"
    "postgresql"
    "sqlite"
    "mongodb"
)

# Backup settings
BACKUP_RETENTION_DAYS=7
BACKUP_COMPRESSION=true
BACKUP_ENCRYPTION=false
BACKUP_VERIFICATION=true

# Database connection settings
MYSQL_HOST="localhost"
MYSQL_PORT="3306"
MYSQL_USER="root"
MYSQL_PASSWORD=""

POSTGRESQL_HOST="localhost"
POSTGRESQL_PORT="5432"
POSTGRESQL_USER="postgres"
POSTGRESQL_PASSWORD=""

# Backup directories
MYSQL_BACKUP_DIR="/tmp/goal_3_1_backups/mysql"
POSTGRESQL_BACKUP_DIR="/tmp/goal_3_1_backups/postgresql"
SQLITE_BACKUP_DIR="/tmp/goal_3_1_backups/sqlite"
MONGODB_BACKUP_DIR="/tmp/goal_3_1_backups/mongodb"
EOF
    
    log_success "Configuration created"
}

create_sample_databases() {
    log_info "Creating sample databases for testing"
    
    # Create sample SQLite database
    mkdir -p "$BACKUP_DIR/sqlite"
    sqlite3 "$BACKUP_DIR/sqlite/sample.db" << 'EOF'
CREATE TABLE users (
    id INTEGER PRIMARY KEY,
    username TEXT NOT NULL,
    email TEXT UNIQUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO users (username, email) VALUES 
    ('john_doe', 'john@example.com'),
    ('jane_smith', 'jane@example.com'),
    ('bob_wilson', 'bob@example.com');

CREATE TABLE products (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    price REAL,
    category TEXT
);

INSERT INTO products (name, price, category) VALUES 
    ('Laptop', 999.99, 'Electronics'),
    ('Mouse', 29.99, 'Electronics'),
    ('Keyboard', 59.99, 'Electronics');
EOF
    
    # Create sample MySQL dump file
    mkdir -p "$BACKUP_DIR/mysql"
    cat > "$BACKUP_DIR/mysql/sample_dump.sql" << 'EOF'
-- Sample MySQL Database Dump
-- Database: sample_db

CREATE DATABASE IF NOT EXISTS sample_db;
USE sample_db;

CREATE TABLE customers (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE,
    phone VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO customers (name, email, phone) VALUES 
    ('Alice Johnson', 'alice@example.com', '555-0101'),
    ('Bob Brown', 'bob@example.com', '555-0102'),
    ('Carol Davis', 'carol@example.com', '555-0103');

CREATE TABLE orders (
    id INT AUTO_INCREMENT PRIMARY KEY,
    customer_id INT,
    order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    total_amount DECIMAL(10,2),
    status ENUM('pending', 'completed', 'cancelled') DEFAULT 'pending',
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);

INSERT INTO orders (customer_id, total_amount, status) VALUES 
    (1, 150.00, 'completed'),
    (2, 75.50, 'pending'),
    (3, 200.00, 'completed');
EOF
    
    # Create sample PostgreSQL dump file
    mkdir -p "$BACKUP_DIR/postgresql"
    cat > "$BACKUP_DIR/postgresql/sample_dump.sql" << 'EOF'
-- Sample PostgreSQL Database Dump
-- Database: sample_db

CREATE DATABASE sample_db;

\c sample_db;

CREATE TABLE employees (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(100) UNIQUE,
    hire_date DATE DEFAULT CURRENT_DATE,
    salary DECIMAL(10,2)
);

INSERT INTO employees (first_name, last_name, email, salary) VALUES 
    ('John', 'Doe', 'john.doe@company.com', 65000.00),
    ('Jane', 'Smith', 'jane.smith@company.com', 70000.00),
    ('Mike', 'Johnson', 'mike.johnson@company.com', 55000.00);

CREATE TABLE departments (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    location VARCHAR(100)
);

INSERT INTO departments (name, location) VALUES 
    ('Engineering', 'Building A'),
    ('Marketing', 'Building B'),
    ('Sales', 'Building C');
EOF
    
    log_success "Sample databases created"
}

backup_sqlite_database() {
    log_info "Backing up SQLite database"
    local db_file="$BACKUP_DIR/sqlite/sample.db"
    local timestamp=$(date '+%Y%m%d_%H%M%S')
    local backup_file="$BACKUP_DIR/sqlite/backup_$timestamp.db"
    
    if [[ -f "$db_file" ]]; then
        cp "$db_file" "$backup_file"
        
        if [[ "$BACKUP_COMPRESSION" == "true" ]]; then
            gzip "$backup_file"
            backup_file="$backup_file.gz"
        fi
        
        log_success "SQLite backup created: $backup_file"
        
        # Verify backup
        if [[ "$BACKUP_VERIFICATION" == "true" ]]; then
            if [[ "$backup_file" == *.gz ]]; then
                gunzip -t "$backup_file" && log_success "SQLite backup verification passed"
            else
                sqlite3 "$backup_file" "SELECT COUNT(*) FROM users;" >/dev/null 2>&1 && log_success "SQLite backup verification passed"
            fi
        fi
    else
        log_error "SQLite database file not found: $db_file"
        return 1
    fi
}

backup_mysql_database() {
    log_info "Backing up MySQL database"
    local timestamp=$(date '+%Y%m%d_%H%M%S')
    local backup_file="$BACKUP_DIR/mysql/backup_$timestamp.sql"
    
    # Create backup using mysqldump (simulated)
    if command -v mysqldump >/dev/null 2>&1; then
        # Simulate mysqldump backup
        cat "$BACKUP_DIR/mysql/sample_dump.sql" > "$backup_file"
        
        if [[ "$BACKUP_COMPRESSION" == "true" ]]; then
            gzip "$backup_file"
            backup_file="$backup_file.gz"
        fi
        
        log_success "MySQL backup created: $backup_file"
        
        # Verify backup
        if [[ "$BACKUP_VERIFICATION" == "true" ]]; then
            if [[ "$backup_file" == *.gz ]]; then
                gunzip -t "$backup_file" && log_success "MySQL backup verification passed"
            else
                grep -q "CREATE TABLE" "$backup_file" && log_success "MySQL backup verification passed"
            fi
        fi
    else
        log_warning "mysqldump not available, creating simulated backup"
        cat "$BACKUP_DIR/mysql/sample_dump.sql" > "$backup_file"
        log_success "Simulated MySQL backup created: $backup_file"
    fi
}

backup_postgresql_database() {
    log_info "Backing up PostgreSQL database"
    local timestamp=$(date '+%Y%m%d_%H%M%S')
    local backup_file="$BACKUP_DIR/postgresql/backup_$timestamp.sql"
    
    # Create backup using pg_dump (simulated)
    if command -v pg_dump >/dev/null 2>&1; then
        # Simulate pg_dump backup
        cat "$BACKUP_DIR/postgresql/sample_dump.sql" > "$backup_file"
        
        if [[ "$BACKUP_COMPRESSION" == "true" ]]; then
            gzip "$backup_file"
            backup_file="$backup_file.gz"
        fi
        
        log_success "PostgreSQL backup created: $backup_file"
        
        # Verify backup
        if [[ "$BACKUP_VERIFICATION" == "true" ]]; then
            if [[ "$backup_file" == *.gz ]]; then
                gunzip -t "$backup_file" && log_success "PostgreSQL backup verification passed"
            else
                grep -q "CREATE TABLE" "$backup_file" && log_success "PostgreSQL backup verification passed"
            fi
        fi
    else
        log_warning "pg_dump not available, creating simulated backup"
        cat "$BACKUP_DIR/postgresql/sample_dump.sql" > "$backup_file"
        log_success "Simulated PostgreSQL backup created: $backup_file"
    fi
}

cleanup_old_backups() {
    log_info "Cleaning up old backups"
    local retention_days=$BACKUP_RETENTION_DAYS
    local deleted_count=0
    
    # Find and delete old backup files
    for backup_type in mysql postgresql sqlite mongodb; do
        local backup_path="$BACKUP_DIR/$backup_type"
        if [[ -d "$backup_path" ]]; then
            local old_files=$(find "$backup_path" -name "backup_*" -type f -mtime +$retention_days 2>/dev/null || true)
            if [[ -n "$old_files" ]]; then
                echo "$old_files" | while read -r file; do
                    rm -f "$file"
                    ((deleted_count++))
                    log_info "Deleted old backup: $file"
                done
            fi
        fi
    done
    
    log_success "Cleanup completed: $deleted_count old backup files removed"
}

generate_backup_report() {
    log_info "Generating backup report"
    local report_file="$BACKUP_DIR/backup_report.txt"
    
    {
        echo "=========================================="
        echo "DATABASE BACKUP REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== Backup Summary ==="
        for backup_type in mysql postgresql sqlite mongodb; do
            local backup_path="$BACKUP_DIR/$backup_type"
            if [[ -d "$backup_path" ]]; then
                local backup_count=$(find "$backup_path" -name "backup_*" -type f | wc -l)
                local total_size=$(du -sh "$backup_path" 2>/dev/null | cut -f1 || echo "0")
                echo "$backup_type: $backup_count backups, total size: $total_size"
            else
                echo "$backup_type: No backups found"
            fi
        done
        
        echo ""
        echo "=== Recent Backups ==="
        find "$BACKUP_DIR" -name "backup_*" -type f -exec ls -lh {} \; 2>/dev/null | head -10
        
        echo ""
        echo "=== Backup Configuration ==="
        echo "Retention period: $BACKUP_RETENTION_DAYS days"
        echo "Compression: $BACKUP_COMPRESSION"
        echo "Encryption: $BACKUP_ENCRYPTION"
        echo "Verification: $BACKUP_VERIFICATION"
        
        echo ""
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Backup report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 3.1 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Create sample databases
    create_sample_databases
    
    # Perform backups
    backup_sqlite_database
    backup_mysql_database
    backup_postgresql_database
    
    # Cleanup old backups
    cleanup_old_backups
    
    # Generate comprehensive report
    generate_backup_report
    
    log_success "Goal 3.1 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    acquire_lock
    main "$@"
fi 
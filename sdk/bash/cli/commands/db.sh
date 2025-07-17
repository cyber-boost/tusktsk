#!/usr/bin/env bash

# TuskLang CLI Database Commands
# =============================
# Database management and operations

set -euo pipefail

# Load utilities
source "$(dirname "${BASH_SOURCE[0]}")/../utils/helpers.sh"
source "$(dirname "${BASH_SOURCE[0]}")/../utils/output.sh"
source "$(dirname "${BASH_SOURCE[0]}")/../utils/config.sh"

##
# Execute database command
#
# @param $* Command arguments
#
execute_db_command() {
    local subcommand="${1:-}"
    
    case "$subcommand" in
        "status")
            execute_db_status "${@:2}"
            ;;
        "migrate")
            execute_db_migrate "${@:2}"
            ;;
        "console")
            execute_db_console "${@:2}"
            ;;
        "backup")
            execute_db_backup "${@:2}"
            ;;
        "restore")
            execute_db_restore "${@:2}"
            ;;
        "init")
            execute_db_init "${@:2}"
            ;;
        *)
            show_db_help
            TSK_EXIT_CODE=2
            ;;
    esac
}

##
# Check database connection status
#
# @param $* Additional arguments
#
execute_db_status() {
    log_info "Checking database connection status"
    
    # Get database configuration
    local db_config
    db_config=$(get_database_config)
    
    if [[ $TSK_JSON_OUTPUT -eq 1 ]]; then
        echo "$db_config"
        return
    fi
    
    # Parse database type
    local db_type
    db_type=$(echo "$db_config" | json_get "type")
    
    case "$db_type" in
        "sqlite")
            check_sqlite_status "$db_config"
            ;;
        "postgres"|"postgresql")
            check_postgres_status "$db_config"
            ;;
        "mysql")
            check_mysql_status "$db_config"
            ;;
        *)
            print_error "Unsupported database type: $db_type"
            TSK_EXIT_CODE=1
            ;;
    esac
}

##
# Check SQLite database status
#
# @param $1 Database config JSON
#
check_sqlite_status() {
    local db_config="$1"
    local db_file
    db_file=$(echo "$db_config" | json_get "database")
    
    if [[ -z "$db_file" ]]; then
        db_file="./tusklang.db"
    fi
    
    if [[ -f "$db_file" ]]; then
        local file_size
        file_size=$(get_file_size "$db_file")
        local mtime
        mtime=$(get_file_mtime "$db_file")
        local formatted_time
        formatted_time=$(format_timestamp "$mtime")
        
        print_success "SQLite database connected"
        print_kv "Database file" "$db_file"
        print_kv "File size" "$file_size"
        print_kv "Last modified" "$formatted_time"
        
        # Test connection
        if command_exists sqlite3; then
            if sqlite3 "$db_file" "SELECT 1;" >/dev/null 2>&1; then
                print_success "Database connection test passed"
            else
                print_error "Database connection test failed"
                TSK_EXIT_CODE=1
            fi
        else
            print_warning "sqlite3 command not available for connection test"
        fi
    else
        print_error "SQLite database file not found: $db_file"
        TSK_EXIT_CODE=1
    fi
}

##
# Check PostgreSQL database status
#
# @param $1 Database config JSON
#
check_postgres_status() {
    local db_config="$1"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    print_info "Checking PostgreSQL connection..."
    
    if ! command_exists psql; then
        print_error "PostgreSQL client (psql) not found"
        TSK_EXIT_CODE=1
        return
    fi
    
    # Test connection
    local test_result
    test_result=$(PGPASSWORD="$password" psql -h "$host" -p "$port" -U "$username" -d "$database" -t -c "SELECT version();" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        print_success "PostgreSQL database connected"
        print_kv "Host" "$host"
        print_kv "Port" "$port"
        print_kv "Database" "$database"
        print_kv "Username" "$username"
        
        # Get additional info
        local version
        version=$(echo "$test_result" | head -n1 | tr -d ' ')
        print_kv "Version" "$version"
    else
        print_error "PostgreSQL connection failed"
        print_error "Error: $test_result"
        TSK_EXIT_CODE=1
    fi
}

##
# Check MySQL database status
#
# @param $1 Database config JSON
#
check_mysql_status() {
    local db_config="$1"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    print_info "Checking MySQL connection..."
    
    if ! command_exists mysql; then
        print_error "MySQL client (mysql) not found"
        TSK_EXIT_CODE=1
        return
    fi
    
    # Test connection
    local test_result
    test_result=$(mysql -h "$host" -P "$port" -u "$username" -p"$password" -D "$database" -sN -e "SELECT VERSION();" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        print_success "MySQL database connected"
        print_kv "Host" "$host"
        print_kv "Port" "$port"
        print_kv "Database" "$database"
        print_kv "Username" "$username"
        print_kv "Version" "$test_result"
    else
        print_error "MySQL connection failed"
        print_error "Error: $test_result"
        TSK_EXIT_CODE=1
    fi
}

##
# Run database migration
#
# @param $* Migration arguments
#
execute_db_migrate() {
    local migration_file="${1:-}"
    
    if [[ -z "$migration_file" ]]; then
        print_error "Migration file required"
        print_info "Usage: tsk db migrate <file>"
        TSK_EXIT_CODE=2
        return
    fi
    
    if ! file_exists "$migration_file"; then
        print_error "Migration file not found: $migration_file"
        TSK_EXIT_CODE=3
        return
    fi
    
    log_info "Running database migration: $migration_file"
    
    # Get database configuration
    local db_config
    db_config=$(get_database_config)
    local db_type
    db_type=$(echo "$db_config" | json_get "type")
    
    case "$db_type" in
        "sqlite")
            run_sqlite_migration "$migration_file" "$db_config"
            ;;
        "postgres"|"postgresql")
            run_postgres_migration "$migration_file" "$db_config"
            ;;
        "mysql")
            run_mysql_migration "$migration_file" "$db_config"
            ;;
        *)
            print_error "Unsupported database type: $db_type"
            TSK_EXIT_CODE=1
            ;;
    esac
}

##
# Run SQLite migration
#
# @param $1 Migration file
# @param $2 Database config
#
run_sqlite_migration() {
    local migration_file="$1"
    local db_config="$2"
    local db_file
    db_file=$(echo "$db_config" | json_get "database")
    
    if [[ -z "$db_file" ]]; then
        db_file="./tusklang.db"
    fi
    
    print_running "Running SQLite migration..."
    
    if command_exists sqlite3; then
        if sqlite3 "$db_file" < "$migration_file"; then
            print_success "SQLite migration completed successfully"
        else
            print_error "SQLite migration failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "sqlite3 command not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Run PostgreSQL migration
#
# @param $1 Migration file
# @param $2 Database config
#
run_postgres_migration() {
    local migration_file="$1"
    local db_config="$2"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    print_running "Running PostgreSQL migration..."
    
    if command_exists psql; then
        if PGPASSWORD="$password" psql -h "$host" -p "$port" -U "$username" -d "$database" -f "$migration_file"; then
            print_success "PostgreSQL migration completed successfully"
        else
            print_error "PostgreSQL migration failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "PostgreSQL client (psql) not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Run MySQL migration
#
# @param $1 Migration file
# @param $2 Database config
#
run_mysql_migration() {
    local migration_file="$1"
    local db_config="$2"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    print_running "Running MySQL migration..."
    
    if command_exists mysql; then
        if mysql -h "$host" -P "$port" -u "$username" -p"$password" -D "$database" < "$migration_file"; then
            print_success "MySQL migration completed successfully"
        else
            print_error "MySQL migration failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "MySQL client (mysql) not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Open interactive database console
#
# @param $* Additional arguments
#
execute_db_console() {
    log_info "Opening interactive database console"
    
    # Get database configuration
    local db_config
    db_config=$(get_database_config)
    local db_type
    db_type=$(echo "$db_config" | json_get "type")
    
    case "$db_type" in
        "sqlite")
            open_sqlite_console "$db_config"
            ;;
        "postgres"|"postgresql")
            open_postgres_console "$db_config"
            ;;
        "mysql")
            open_mysql_console "$db_config"
            ;;
        *)
            print_error "Unsupported database type: $db_type"
            TSK_EXIT_CODE=1
            ;;
    esac
}

##
# Open SQLite console
#
# @param $1 Database config
#
open_sqlite_console() {
    local db_config="$1"
    local db_file
    db_file=$(echo "$db_config" | json_get "database")
    
    if [[ -z "$db_file" ]]; then
        db_file="./tusklang.db"
    fi
    
    if command_exists sqlite3; then
        print_info "Opening SQLite console for: $db_file"
        sqlite3 "$db_file"
    else
        print_error "sqlite3 command not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Open PostgreSQL console
#
# @param $1 Database config
#
open_postgres_console() {
    local db_config="$1"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    if command_exists psql; then
        print_info "Opening PostgreSQL console for: $database@$host:$port"
        PGPASSWORD="$password" psql -h "$host" -p "$port" -U "$username" -d "$database"
    else
        print_error "PostgreSQL client (psql) not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Open MySQL console
#
# @param $1 Database config
#
open_mysql_console() {
    local db_config="$1"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    if command_exists mysql; then
        print_info "Opening MySQL console for: $database@$host:$port"
        mysql -h "$host" -P "$port" -u "$username" -p"$password" -D "$database"
    else
        print_error "MySQL client (mysql) not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Backup database
#
# @param $* Backup arguments
#
execute_db_backup() {
    local backup_file="${1:-}"
    
    if [[ -z "$backup_file" ]]; then
        local timestamp
        timestamp=$(date +%Y%m%d_%H%M%S)
        backup_file="tusklang_backup_${timestamp}.sql"
    fi
    
    log_info "Creating database backup: $backup_file"
    
    # Get database configuration
    local db_config
    db_config=$(get_database_config)
    local db_type
    db_type=$(echo "$db_config" | json_get "type")
    
    case "$db_type" in
        "sqlite")
            create_sqlite_backup "$backup_file" "$db_config"
            ;;
        "postgres"|"postgresql")
            create_postgres_backup "$backup_file" "$db_config"
            ;;
        "mysql")
            create_mysql_backup "$backup_file" "$db_config"
            ;;
        *)
            print_error "Unsupported database type: $db_type"
            TSK_EXIT_CODE=1
            ;;
    esac
}

##
# Create SQLite backup
#
# @param $1 Backup file
# @param $2 Database config
#
create_sqlite_backup() {
    local backup_file="$1"
    local db_config="$2"
    local db_file
    db_file=$(echo "$db_config" | json_get "database")
    
    if [[ -z "$db_file" ]]; then
        db_file="./tusklang.db"
    fi
    
    if ! file_exists "$db_file"; then
        print_error "SQLite database file not found: $db_file"
        TSK_EXIT_CODE=3
        return
    fi
    
    print_running "Creating SQLite backup..."
    
    if command_exists sqlite3; then
        if sqlite3 "$db_file" ".dump" > "$backup_file"; then
            local backup_size
            backup_size=$(get_file_size "$backup_file")
            print_success "SQLite backup created successfully"
            print_kv "Backup file" "$backup_file"
            print_kv "Backup size" "$backup_size"
        else
            print_error "SQLite backup failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "sqlite3 command not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Create PostgreSQL backup
#
# @param $1 Backup file
# @param $2 Database config
#
create_postgres_backup() {
    local backup_file="$1"
    local db_config="$2"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    print_running "Creating PostgreSQL backup..."
    
    if command_exists pg_dump; then
        if PGPASSWORD="$password" pg_dump -h "$host" -p "$port" -U "$username" -d "$database" > "$backup_file"; then
            local backup_size
            backup_size=$(get_file_size "$backup_file")
            print_success "PostgreSQL backup created successfully"
            print_kv "Backup file" "$backup_file"
            print_kv "Backup size" "$backup_size"
        else
            print_error "PostgreSQL backup failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "PostgreSQL client (pg_dump) not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Create MySQL backup
#
# @param $1 Backup file
# @param $2 Database config
#
create_mysql_backup() {
    local backup_file="$1"
    local db_config="$2"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    print_running "Creating MySQL backup..."
    
    if command_exists mysqldump; then
        if mysqldump -h "$host" -P "$port" -u "$username" -p"$password" "$database" > "$backup_file"; then
            local backup_size
            backup_size=$(get_file_size "$backup_file")
            print_success "MySQL backup created successfully"
            print_kv "Backup file" "$backup_file"
            print_kv "Backup size" "$backup_size"
        else
            print_error "MySQL backup failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "MySQL client (mysqldump) not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Restore database from backup
#
# @param $* Restore arguments
#
execute_db_restore() {
    local backup_file="${1:-}"
    
    if [[ -z "$backup_file" ]]; then
        print_error "Backup file required"
        print_info "Usage: tsk db restore <file>"
        TSK_EXIT_CODE=2
        return
    fi
    
    if ! file_exists "$backup_file"; then
        print_error "Backup file not found: $backup_file"
        TSK_EXIT_CODE=3
        return
    fi
    
    log_info "Restoring database from backup: $backup_file"
    
    # Get database configuration
    local db_config
    db_config=$(get_database_config)
    local db_type
    db_type=$(echo "$db_config" | json_get "type")
    
    case "$db_type" in
        "sqlite")
            restore_sqlite_backup "$backup_file" "$db_config"
            ;;
        "postgres"|"postgresql")
            restore_postgres_backup "$backup_file" "$db_config"
            ;;
        "mysql")
            restore_mysql_backup "$backup_file" "$db_config"
            ;;
        *)
            print_error "Unsupported database type: $db_type"
            TSK_EXIT_CODE=1
            ;;
    esac
}

##
# Restore SQLite backup
#
# @param $1 Backup file
# @param $2 Database config
#
restore_sqlite_backup() {
    local backup_file="$1"
    local db_config="$2"
    local db_file
    db_file=$(echo "$db_config" | json_get "database")
    
    if [[ -z "$db_file" ]]; then
        db_file="./tusklang.db"
    fi
    
    print_running "Restoring SQLite backup..."
    
    if command_exists sqlite3; then
        if sqlite3 "$db_file" < "$backup_file"; then
            print_success "SQLite backup restored successfully"
        else
            print_error "SQLite backup restore failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "sqlite3 command not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Restore PostgreSQL backup
#
# @param $1 Backup file
# @param $2 Database config
#
restore_postgres_backup() {
    local backup_file="$1"
    local db_config="$2"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    print_running "Restoring PostgreSQL backup..."
    
    if command_exists psql; then
        if PGPASSWORD="$password" psql -h "$host" -p "$port" -U "$username" -d "$database" < "$backup_file"; then
            print_success "PostgreSQL backup restored successfully"
        else
            print_error "PostgreSQL backup restore failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "PostgreSQL client (psql) not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Restore MySQL backup
#
# @param $1 Backup file
# @param $2 Database config
#
restore_mysql_backup() {
    local backup_file="$1"
    local db_config="$2"
    local host
    local port
    local database
    local username
    local password
    
    host=$(echo "$db_config" | json_get "host")
    port=$(echo "$db_config" | json_get "port")
    database=$(echo "$db_config" | json_get "database")
    username=$(echo "$db_config" | json_get "username")
    password=$(echo "$db_config" | json_get "password")
    
    print_running "Restoring MySQL backup..."
    
    if command_exists mysql; then
        if mysql -h "$host" -P "$port" -u "$username" -p"$password" -D "$database" < "$backup_file"; then
            print_success "MySQL backup restored successfully"
        else
            print_error "MySQL backup restore failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "MySQL client (mysql) not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Initialize SQLite database
#
# @param $* Additional arguments
#
execute_db_init() {
    log_info "Initializing SQLite database"
    
    # Get database configuration
    local db_config
    db_config=$(get_database_config)
    local db_type
    db_type=$(echo "$db_config" | json_get "type")
    
    if [[ "$db_type" != "sqlite" ]]; then
        print_error "Database init only supports SQLite, current type: $db_type"
        TSK_EXIT_CODE=1
        return
    fi
    
    local db_file
    db_file=$(echo "$db_config" | json_get "database")
    
    if [[ -z "$db_file" ]]; then
        db_file="./tusklang.db"
    fi
    
    print_running "Initializing SQLite database: $db_file"
    
    if command_exists sqlite3; then
        # Create basic schema
        local schema="
CREATE TABLE IF NOT EXISTS migrations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    filename TEXT NOT NULL,
    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS config (
    key TEXT PRIMARY KEY,
    value TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    level TEXT NOT NULL,
    message TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
"
        
        if echo "$schema" | sqlite3 "$db_file"; then
            local file_size
            file_size=$(get_file_size "$db_file")
            print_success "SQLite database initialized successfully"
            print_kv "Database file" "$db_file"
            print_kv "File size" "$file_size"
        else
            print_error "SQLite database initialization failed"
            TSK_EXIT_CODE=1
        fi
    else
        print_error "sqlite3 command not available"
        TSK_EXIT_CODE=1
    fi
}

##
# Show database help
#
show_db_help() {
    cat << EOF
🗄️ Database Commands

Usage: tsk db <command> [options]

Commands:
  status                    Check database connection status
  migrate <file>           Run migration file
  console                  Open interactive database console
  backup [file]            Backup database (default: tusklang_backup_TIMESTAMP.sql)
  restore <file>           Restore from backup file
  init                     Initialize SQLite database

Examples:
  tsk db status
  tsk db migrate schema.sql
  tsk db console
  tsk db backup my_backup.sql
  tsk db restore my_backup.sql
  tsk db init

Supported Databases:
  - SQLite (default)
  - PostgreSQL
  - MySQL

Configuration:
  Database settings are read from peanu.tsk configuration files.
  See 'tsk config' for more information about configuration management.
EOF
} 
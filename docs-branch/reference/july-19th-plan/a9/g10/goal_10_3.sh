#!/bin/bash

# MySQL Operator Implementation
# Provides MySQL database operations

# Global variables
MYSQL_HOST="localhost"
MYSQL_PORT="3306"
MYSQL_DATABASE=""
MYSQL_USERNAME=""
MYSQL_PASSWORD=""
MYSQL_SSL_MODE="PREFERRED"
MYSQL_TIMEOUT="30"
MYSQL_OUTPUT_FORMAT="json"

# Initialize MySQL operator
mysql_init() {
    local host="$1"
    local port="$2"
    local database="$3"
    local username="$4"
    local password="$5"
    local ssl_mode="$6"
    local output_format="$7"
    
    if [[ -z "$database" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Database name is required\"}"
        return 1
    fi
    
    MYSQL_HOST="${host:-localhost}"
    MYSQL_PORT="${port:-3306}"
    MYSQL_DATABASE="$database"
    MYSQL_USERNAME="$username"
    MYSQL_PASSWORD="$password"
    MYSQL_SSL_MODE="${ssl_mode:-PREFERRED}"
    MYSQL_OUTPUT_FORMAT="${output_format:-json}"
    
    echo "{\"status\":\"success\",\"message\":\"MySQL operator initialized\",\"database\":\"$database\",\"host\":\"$MYSQL_HOST\",\"port\":\"$MYSQL_PORT\"}"
}

# MySQL connection test
mysql_test_connection() {
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    if ! command -v mysql >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"MySQL client (mysql) not found\"}"
        return 1
    fi
    
    # Build connection options
    local mysql_options="-h $MYSQL_HOST -P $MYSQL_PORT -D $MYSQL_DATABASE"
    if [[ -n "$MYSQL_USERNAME" ]]; then
        mysql_options="$mysql_options -u $MYSQL_USERNAME"
    fi
    
    # Test connection
    local response=$(MYSQL_PWD="$MYSQL_PASSWORD" mysql $mysql_options -e "SELECT 1;" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"MySQL connection successful\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"MySQL connection failed\",\"error\":\"$response\"}"
        return 1
    fi
}

# MySQL execute query
mysql_query() {
    local query="$1"
    local output_format="$2"
    
    if [[ -z "$query" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Query is required\"}"
        return 1
    fi
    
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    if ! command -v mysql >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"MySQL client not found\"}"
        return 1
    fi
    
    output_format="${output_format:-$MYSQL_OUTPUT_FORMAT}"
    
    # Build mysql options
    local mysql_options="-h $MYSQL_HOST -P $MYSQL_PORT -D $MYSQL_DATABASE"
    if [[ -n "$MYSQL_USERNAME" ]]; then
        mysql_options="$mysql_options -u $MYSQL_USERNAME"
    fi
    
    # Set output format
    case "$output_format" in
        "json")
            mysql_options="$mysql_options -s -r --skip-column-names"
            ;;
        "csv")
            mysql_options="$mysql_options -s -r --skip-column-names --tab"
            ;;
        "table")
            mysql_options="$mysql_options"
            ;;
        *)
            mysql_options="$mysql_options -s -r --skip-column-names"
            ;;
    esac
    
    # Execute query
    local response=$(MYSQL_PWD="$MYSQL_PASSWORD" mysql $mysql_options -e "$query" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        if [[ "$output_format" == "json" ]]; then
            # Convert to JSON format
            local json_result="[]"
            if [[ -n "$response" ]]; then
                json_result=$(echo "$response" | awk -F'\t' 'BEGIN {print "["} {gsub(/"/, "\\\"", $0); if (NR>1) print ","; printf "[\""; for (i=1; i<=NF; i++) {gsub(/"/, "\\\"", $i); if (i>1) printf "\",\""; printf "%s", $i} print "\"]"} END {print "]"}')
            fi
            echo "{\"status\":\"success\",\"message\":\"Query executed successfully\",\"query\":\"$query\",\"result\":$json_result}"
        else
            echo "{\"status\":\"success\",\"message\":\"Query executed successfully\",\"query\":\"$query\",\"result\":\"$response\"}"
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Query execution failed\",\"query\":\"$query\",\"error\":\"$response\"}"
        return 1
    fi
}

# MySQL execute file
mysql_execute_file() {
    local file_path="$1"
    local output_format="$2"
    
    if [[ -z "$file_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File path is required\"}"
        return 1
    fi
    
    if [[ ! -f "$file_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File not found: $file_path\"}"
        return 1
    fi
    
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    if ! command -v mysql >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"MySQL client not found\"}"
        return 1
    fi
    
    output_format="${output_format:-$MYSQL_OUTPUT_FORMAT}"
    
    # Build mysql options
    local mysql_options="-h $MYSQL_HOST -P $MYSQL_PORT -D $MYSQL_DATABASE"
    if [[ -n "$MYSQL_USERNAME" ]]; then
        mysql_options="$mysql_options -u $MYSQL_USERNAME"
    fi
    
    # Set output format
    case "$output_format" in
        "json")
            mysql_options="$mysql_options -s -r --skip-column-names"
            ;;
        "csv")
            mysql_options="$mysql_options -s -r --skip-column-names --tab"
            ;;
        "table")
            mysql_options="$mysql_options"
            ;;
        *)
            mysql_options="$mysql_options -s -r --skip-column-names"
            ;;
    esac
    
    # Execute file
    local response=$(MYSQL_PWD="$MYSQL_PASSWORD" mysql $mysql_options < "$file_path" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"File executed successfully\",\"file\":\"$file_path\",\"result\":\"$response\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"File execution failed\",\"file\":\"$file_path\",\"error\":\"$response\"}"
        return 1
    fi
}

# MySQL backup database
mysql_backup() {
    local backup_path="$1"
    local format="$2"
    
    if [[ -z "$backup_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup path is required\"}"
        return 1
    fi
    
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    if ! command -v mysqldump >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"MySQL backup tool (mysqldump) not found\"}"
        return 1
    fi
    
    format="${format:-sql}"
    
    # Build mysqldump options
    local dump_options="-h $MYSQL_HOST -P $MYSQL_PORT -u $MYSQL_USERNAME"
    if [[ -n "$MYSQL_PASSWORD" ]]; then
        dump_options="$dump_options -p$MYSQL_PASSWORD"
    fi
    
    case "$format" in
        "sql")
            dump_options="$dump_options --single-transaction --routines --triggers"
            ;;
        "data_only")
            dump_options="$dump_options --no-create-info --single-transaction"
            ;;
        "structure_only")
            dump_options="$dump_options --no-data --routines --triggers"
            ;;
        *)
            dump_options="$dump_options --single-transaction --routines --triggers"
            ;;
    esac
    
    # Create backup
    local response=$(mysqldump $dump_options "$MYSQL_DATABASE" > "$backup_path" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        local file_size=$(stat -c%s "$backup_path" 2>/dev/null || echo "0")
        echo "{\"status\":\"success\",\"message\":\"Database backup created\",\"backup_path\":\"$backup_path\",\"format\":\"$format\",\"size_bytes\":$file_size}"
    else
        echo "{\"status\":\"error\",\"message\":\"Database backup failed\",\"backup_path\":\"$backup_path\",\"error\":\"$response\"}"
        return 1
    fi
}

# MySQL restore database
mysql_restore() {
    local backup_path="$1"
    local format="$2"
    
    if [[ -z "$backup_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup path is required\"}"
        return 1
    fi
    
    if [[ ! -f "$backup_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup file not found: $backup_path\"}"
        return 1
    fi
    
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    if ! command -v mysql >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"MySQL client not found\"}"
        return 1
    fi
    
    # Build mysql options
    local mysql_options="-h $MYSQL_HOST -P $MYSQL_PORT -u $MYSQL_USERNAME"
    if [[ -n "$MYSQL_PASSWORD" ]]; then
        mysql_options="$mysql_options -p$MYSQL_PASSWORD"
    fi
    
    # Restore database
    local response=$(MYSQL_PWD="$MYSQL_PASSWORD" mysql $mysql_options "$MYSQL_DATABASE" < "$backup_path" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Database restored successfully\",\"backup_path\":\"$backup_path\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Database restore failed\",\"backup_path\":\"$backup_path\",\"error\":\"$response\"}"
        return 1
    fi
}

# MySQL list tables
mysql_list_tables() {
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    local query="SHOW TABLES;"
    mysql_query "$query" "json"
}

# MySQL describe table
mysql_describe_table() {
    local table_name="$1"
    
    if [[ -z "$table_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Table name is required\"}"
        return 1
    fi
    
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    local query="DESCRIBE $table_name;"
    mysql_query "$query" "json"
}

# MySQL table row count
mysql_table_count() {
    local table_name="$1"
    
    if [[ -z "$table_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Table name is required\"}"
        return 1
    fi
    
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    local query="SELECT COUNT(*) as row_count FROM $table_name;"
    mysql_query "$query" "json"
}

# MySQL create table
mysql_create_table() {
    local table_name="$1"
    local columns="$2"
    
    if [[ -z "$table_name" || -z "$columns" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Table name and columns are required\"}"
        return 1
    fi
    
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    local query="CREATE TABLE IF NOT EXISTS $table_name ($columns);"
    mysql_query "$query" "json"
}

# MySQL drop table
mysql_drop_table() {
    local table_name="$1"
    
    if [[ -z "$table_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Table name is required\"}"
        return 1
    fi
    
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    local query="DROP TABLE IF EXISTS $table_name;"
    mysql_query "$query" "json"
}

# MySQL show databases
mysql_show_databases() {
    if ! command -v mysql >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"MySQL client not found\"}"
        return 1
    fi
    
    # Build mysql options
    local mysql_options="-h $MYSQL_HOST -P $MYSQL_PORT"
    if [[ -n "$MYSQL_USERNAME" ]]; then
        mysql_options="$mysql_options -u $MYSQL_USERNAME"
    fi
    
    local response=$(MYSQL_PWD="$MYSQL_PASSWORD" mysql $mysql_options -e "SHOW DATABASES;" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        # Convert to JSON format
        local json_result=$(echo "$response" | awk 'NR>1 {gsub(/"/, "\\\"", $0); if (NR>2) printf ","; printf "\"%s\"", $0} BEGIN {printf "["} END {print "]"}')
        echo "{\"status\":\"success\",\"message\":\"Databases listed\",\"databases\":$json_result}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to list databases\",\"error\":\"$response\"}"
        return 1
    fi
}

# MySQL show processes
mysql_show_processes() {
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    local query="SHOW PROCESSLIST;"
    mysql_query "$query" "json"
}

# MySQL show status
mysql_show_status() {
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    local query="SHOW STATUS;"
    mysql_query "$query" "json"
}

# MySQL show variables
mysql_show_variables() {
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    local query="SHOW VARIABLES;"
    mysql_query "$query" "json"
}

# Get MySQL configuration
mysql_config() {
    if [[ -z "$MYSQL_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"MySQL not initialized\"}"
        return 1
    fi
    
    echo "{\"status\":\"success\",\"config\":{\"host\":\"$MYSQL_HOST\",\"port\":\"$MYSQL_PORT\",\"database\":\"$MYSQL_DATABASE\",\"username\":\"$MYSQL_USERNAME\",\"ssl_mode\":\"$MYSQL_SSL_MODE\",\"output_format\":\"$MYSQL_OUTPUT_FORMAT\"}}"
}

# Main MySQL operator function
execute_mysql() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local host=$(echo "$params" | grep -o 'host=[^,]*' | cut -d'=' -f2)
            local port=$(echo "$params" | grep -o 'port=[^,]*' | cut -d'=' -f2)
            local database=$(echo "$params" | grep -o 'database=[^,]*' | cut -d'=' -f2)
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local password=$(echo "$params" | grep -o 'password=[^,]*' | cut -d'=' -f2)
            local ssl_mode=$(echo "$params" | grep -o 'ssl_mode=[^,]*' | cut -d'=' -f2)
            local output_format=$(echo "$params" | grep -o 'output_format=[^,]*' | cut -d'=' -f2)
            mysql_init "$host" "$port" "$database" "$username" "$password" "$ssl_mode" "$output_format"
            ;;
        "test")
            mysql_test_connection
            ;;
        "query")
            local query=$(echo "$params" | grep -o 'query=[^,]*' | cut -d'=' -f2)
            local output_format=$(echo "$params" | grep -o 'output_format=[^,]*' | cut -d'=' -f2)
            mysql_query "$query" "$output_format"
            ;;
        "execute_file")
            local file_path=$(echo "$params" | grep -o 'file_path=[^,]*' | cut -d'=' -f2)
            local output_format=$(echo "$params" | grep -o 'output_format=[^,]*' | cut -d'=' -f2)
            mysql_execute_file "$file_path" "$output_format"
            ;;
        "backup")
            local backup_path=$(echo "$params" | grep -o 'backup_path=[^,]*' | cut -d'=' -f2)
            local format=$(echo "$params" | grep -o 'format=[^,]*' | cut -d'=' -f2)
            mysql_backup "$backup_path" "$format"
            ;;
        "restore")
            local backup_path=$(echo "$params" | grep -o 'backup_path=[^,]*' | cut -d'=' -f2)
            local format=$(echo "$params" | grep -o 'format=[^,]*' | cut -d'=' -f2)
            mysql_restore "$backup_path" "$format"
            ;;
        "list_tables")
            mysql_list_tables
            ;;
        "describe_table")
            local table_name=$(echo "$params" | grep -o 'table_name=[^,]*' | cut -d'=' -f2)
            mysql_describe_table "$table_name"
            ;;
        "table_count")
            local table_name=$(echo "$params" | grep -o 'table_name=[^,]*' | cut -d'=' -f2)
            mysql_table_count "$table_name"
            ;;
        "create_table")
            local table_name=$(echo "$params" | grep -o 'table_name=[^,]*' | cut -d'=' -f2)
            local columns=$(echo "$params" | grep -o 'columns=[^,]*' | cut -d'=' -f2)
            mysql_create_table "$table_name" "$columns"
            ;;
        "drop_table")
            local table_name=$(echo "$params" | grep -o 'table_name=[^,]*' | cut -d'=' -f2)
            mysql_drop_table "$table_name"
            ;;
        "show_databases")
            mysql_show_databases
            ;;
        "show_processes")
            mysql_show_processes
            ;;
        "show_status")
            mysql_show_status
            ;;
        "show_variables")
            mysql_show_variables
            ;;
        "config")
            mysql_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, test, query, execute_file, backup, restore, list_tables, describe_table, table_count, create_table, drop_table, show_databases, show_processes, show_status, show_variables, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_mysql mysql_init mysql_test_connection mysql_query mysql_execute_file mysql_backup mysql_restore mysql_list_tables mysql_describe_table mysql_table_count mysql_create_table mysql_drop_table mysql_show_databases mysql_show_processes mysql_show_status mysql_show_variables mysql_config 
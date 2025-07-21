#!/bin/bash

# PostgreSQL Operator Implementation
# Provides PostgreSQL database operations

# Global variables
POSTGRES_HOST="localhost"
POSTGRES_PORT="5432"
POSTGRES_DATABASE=""
POSTGRES_USERNAME=""
POSTGRES_PASSWORD=""
POSTGRES_SSL_MODE="prefer"
POSTGRES_TIMEOUT="30"
POSTGRES_OUTPUT_FORMAT="json"

# Initialize PostgreSQL operator
postgresql_init() {
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
    
    POSTGRES_HOST="${host:-localhost}"
    POSTGRES_PORT="${port:-5432}"
    POSTGRES_DATABASE="$database"
    POSTGRES_USERNAME="$username"
    POSTGRES_PASSWORD="$password"
    POSTGRES_SSL_MODE="${ssl_mode:-prefer}"
    POSTGRES_OUTPUT_FORMAT="${output_format:-json}"
    
    echo "{\"status\":\"success\",\"message\":\"PostgreSQL operator initialized\",\"database\":\"$database\",\"host\":\"$POSTGRES_HOST\",\"port\":\"$POSTGRES_PORT\"}"
}

# PostgreSQL connection test
postgresql_test_connection() {
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    if ! command -v psql >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL client (psql) not found\"}"
        return 1
    fi
    
    # Build connection string
    local conn_string="postgresql://"
    if [[ -n "$POSTGRES_USERNAME" ]]; then
        conn_string="${conn_string}${POSTGRES_USERNAME}"
        if [[ -n "$POSTGRES_PASSWORD" ]]; then
            conn_string="${conn_string}:${POSTGRES_PASSWORD}"
        fi
        conn_string="${conn_string}@"
    fi
    conn_string="${conn_string}${POSTGRES_HOST}:${POSTGRES_PORT}/${POSTGRES_DATABASE}"
    
    # Test connection
    local response=$(PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USERNAME" -d "$POSTGRES_DATABASE" -c "SELECT 1;" 2>&1)
    
    if [[ $? -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"PostgreSQL connection successful\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL connection failed\",\"error\":\"$response\"}"
        return 1
    fi
}

# PostgreSQL execute query
postgresql_query() {
    local query="$1"
    local output_format="$2"
    
    if [[ -z "$query" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Query is required\"}"
        return 1
    fi
    
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    if ! command -v psql >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL client not found\"}"
        return 1
    fi
    
    output_format="${output_format:-$POSTGRES_OUTPUT_FORMAT}"
    
    # Build psql options
    local psql_options="-h $POSTGRES_HOST -p $POSTGRES_PORT -d $POSTGRES_DATABASE"
    if [[ -n "$POSTGRES_USERNAME" ]]; then
        psql_options="$psql_options -U $POSTGRES_USERNAME"
    fi
    
    # Set output format
    case "$output_format" in
        "json")
            psql_options="$psql_options -t -A -F, --tuples-only"
            ;;
        "csv")
            psql_options="$psql_options -A -F, --no-align"
            ;;
        "table")
            psql_options="$psql_options"
            ;;
        *)
            psql_options="$psql_options -t -A -F, --tuples-only"
            ;;
    esac
    
    # Execute query
    local response=$(PGPASSWORD="$POSTGRES_PASSWORD" psql $psql_options -c "$query" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        if [[ "$output_format" == "json" ]]; then
            # Convert to JSON format
            local json_result="[]"
            if [[ -n "$response" ]]; then
                json_result=$(echo "$response" | awk -F',' 'BEGIN {print "["} {gsub(/^[ \t]+|[ \t]+$/, "", $0); if (NR>1) print ","; printf "[\""; for (i=1; i<=NF; i++) {gsub(/"/, "\\\"", $i); gsub(/^[ \t]+|[ \t]+$/, "", $i); if (i>1) printf "\",\""; printf "%s", $i} print "\"]"} END {print "]"}')
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

# PostgreSQL execute file
postgresql_execute_file() {
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
    
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    if ! command -v psql >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL client not found\"}"
        return 1
    fi
    
    output_format="${output_format:-$POSTGRES_OUTPUT_FORMAT}"
    
    # Build psql options
    local psql_options="-h $POSTGRES_HOST -p $POSTGRES_PORT -d $POSTGRES_DATABASE"
    if [[ -n "$POSTGRES_USERNAME" ]]; then
        psql_options="$psql_options -U $POSTGRES_USERNAME"
    fi
    
    # Set output format
    case "$output_format" in
        "json")
            psql_options="$psql_options -t -A -F, --tuples-only"
            ;;
        "csv")
            psql_options="$psql_options -A -F, --no-align"
            ;;
        "table")
            psql_options="$psql_options"
            ;;
        *)
            psql_options="$psql_options -t -A -F, --tuples-only"
            ;;
    esac
    
    # Execute file
    local response=$(PGPASSWORD="$POSTGRES_PASSWORD" psql $psql_options -f "$file_path" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"File executed successfully\",\"file\":\"$file_path\",\"result\":\"$response\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"File execution failed\",\"file\":\"$file_path\",\"error\":\"$response\"}"
        return 1
    fi
}

# PostgreSQL backup database
postgresql_backup() {
    local backup_path="$1"
    local format="$2"
    
    if [[ -z "$backup_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup path is required\"}"
        return 1
    fi
    
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    if ! command -v pg_dump >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL backup tool (pg_dump) not found\"}"
        return 1
    fi
    
    format="${format:-custom}"
    
    # Build pg_dump options
    local dump_options="-h $POSTGRES_HOST -p $POSTGRES_PORT -d $POSTGRES_DATABASE"
    if [[ -n "$POSTGRES_USERNAME" ]]; then
        dump_options="$dump_options -U $POSTGRES_USERNAME"
    fi
    
    case "$format" in
        "custom")
            dump_options="$dump_options -Fc"
            ;;
        "plain")
            dump_options="$dump_options -Fp"
            ;;
        "directory")
            dump_options="$dump_options -Fd"
            ;;
        "tar")
            dump_options="$dump_options -Ft"
            ;;
        *)
            dump_options="$dump_options -Fc"
            ;;
    esac
    
    # Create backup
    local response=$(PGPASSWORD="$POSTGRES_PASSWORD" pg_dump $dump_options -f "$backup_path" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        local file_size=$(stat -c%s "$backup_path" 2>/dev/null || echo "0")
        echo "{\"status\":\"success\",\"message\":\"Database backup created\",\"backup_path\":\"$backup_path\",\"format\":\"$format\",\"size_bytes\":$file_size}"
    else
        echo "{\"status\":\"error\",\"message\":\"Database backup failed\",\"backup_path\":\"$backup_path\",\"error\":\"$response\"}"
        return 1
    fi
}

# PostgreSQL restore database
postgresql_restore() {
    local backup_path="$1"
    local format="$2"
    local clean="$3"
    
    if [[ -z "$backup_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup path is required\"}"
        return 1
    fi
    
    if [[ ! -f "$backup_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Backup file not found: $backup_path\"}"
        return 1
    fi
    
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    if ! command -v pg_restore >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL restore tool (pg_restore) not found\"}"
        return 1
    fi
    
    format="${format:-custom}"
    
    # Build pg_restore options
    local restore_options="-h $POSTGRES_HOST -p $POSTGRES_PORT -d $POSTGRES_DATABASE"
    if [[ -n "$POSTGRES_USERNAME" ]]; then
        restore_options="$restore_options -U $POSTGRES_USERNAME"
    fi
    
    if [[ "$clean" == "true" ]]; then
        restore_options="$restore_options --clean --if-exists"
    fi
    
    case "$format" in
        "custom")
            restore_options="$restore_options -Fc"
            ;;
        "plain")
            # For plain format, use psql instead of pg_restore
            local response=$(PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USERNAME" -d "$POSTGRES_DATABASE" -f "$backup_path" 2>&1)
            local exit_code=$?
            ;;
        "directory")
            restore_options="$restore_options -Fd"
            ;;
        "tar")
            restore_options="$restore_options -Ft"
            ;;
        *)
            restore_options="$restore_options -Fc"
            ;;
    esac
    
    # Restore database
    if [[ "$format" == "plain" ]]; then
        # Already executed above
        :
    else
        local response=$(PGPASSWORD="$POSTGRES_PASSWORD" pg_restore $restore_options "$backup_path" 2>&1)
        local exit_code=$?
    fi
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Database restored successfully\",\"backup_path\":\"$backup_path\",\"format\":\"$format\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Database restore failed\",\"backup_path\":\"$backup_path\",\"error\":\"$response\"}"
        return 1
    fi
}

# PostgreSQL list tables
postgresql_list_tables() {
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    local query="SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;"
    postgresql_query "$query" "json"
}

# PostgreSQL describe table
postgresql_describe_table() {
    local table_name="$1"
    
    if [[ -z "$table_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Table name is required\"}"
        return 1
    fi
    
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    local query="SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = '$table_name' AND table_schema = 'public' ORDER BY ordinal_position;"
    postgresql_query "$query" "json"
}

# PostgreSQL table row count
postgresql_table_count() {
    local table_name="$1"
    
    if [[ -z "$table_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Table name is required\"}"
        return 1
    fi
    
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    local query="SELECT COUNT(*) as row_count FROM $table_name;"
    postgresql_query "$query" "json"
}

# PostgreSQL create table
postgresql_create_table() {
    local table_name="$1"
    local columns="$2"
    
    if [[ -z "$table_name" || -z "$columns" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Table name and columns are required\"}"
        return 1
    fi
    
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    local query="CREATE TABLE IF NOT EXISTS $table_name ($columns);"
    postgresql_query "$query" "json"
}

# PostgreSQL drop table
postgresql_drop_table() {
    local table_name="$1"
    local cascade="$2"
    
    if [[ -z "$table_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Table name is required\"}"
        return 1
    fi
    
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    local query="DROP TABLE IF EXISTS $table_name"
    if [[ "$cascade" == "true" ]]; then
        query="$query CASCADE"
    fi
    query="$query;"
    
    postgresql_query "$query" "json"
}

# Get PostgreSQL configuration
postgresql_config() {
    if [[ -z "$POSTGRES_DATABASE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"PostgreSQL not initialized\"}"
        return 1
    fi
    
    echo "{\"status\":\"success\",\"config\":{\"host\":\"$POSTGRES_HOST\",\"port\":\"$POSTGRES_PORT\",\"database\":\"$POSTGRES_DATABASE\",\"username\":\"$POSTGRES_USERNAME\",\"ssl_mode\":\"$POSTGRES_SSL_MODE\",\"output_format\":\"$POSTGRES_OUTPUT_FORMAT\"}}"
}

# Main PostgreSQL operator function
execute_postgresql() {
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
            postgresql_init "$host" "$port" "$database" "$username" "$password" "$ssl_mode" "$output_format"
            ;;
        "test")
            postgresql_test_connection
            ;;
        "query")
            local query=$(echo "$params" | grep -o 'query=[^,]*' | cut -d'=' -f2)
            local output_format=$(echo "$params" | grep -o 'output_format=[^,]*' | cut -d'=' -f2)
            postgresql_query "$query" "$output_format"
            ;;
        "execute_file")
            local file_path=$(echo "$params" | grep -o 'file_path=[^,]*' | cut -d'=' -f2)
            local output_format=$(echo "$params" | grep -o 'output_format=[^,]*' | cut -d'=' -f2)
            postgresql_execute_file "$file_path" "$output_format"
            ;;
        "backup")
            local backup_path=$(echo "$params" | grep -o 'backup_path=[^,]*' | cut -d'=' -f2)
            local format=$(echo "$params" | grep -o 'format=[^,]*' | cut -d'=' -f2)
            postgresql_backup "$backup_path" "$format"
            ;;
        "restore")
            local backup_path=$(echo "$params" | grep -o 'backup_path=[^,]*' | cut -d'=' -f2)
            local format=$(echo "$params" | grep -o 'format=[^,]*' | cut -d'=' -f2)
            local clean=$(echo "$params" | grep -o 'clean=[^,]*' | cut -d'=' -f2)
            postgresql_restore "$backup_path" "$format" "$clean"
            ;;
        "list_tables")
            postgresql_list_tables
            ;;
        "describe_table")
            local table_name=$(echo "$params" | grep -o 'table_name=[^,]*' | cut -d'=' -f2)
            postgresql_describe_table "$table_name"
            ;;
        "table_count")
            local table_name=$(echo "$params" | grep -o 'table_name=[^,]*' | cut -d'=' -f2)
            postgresql_table_count "$table_name"
            ;;
        "create_table")
            local table_name=$(echo "$params" | grep -o 'table_name=[^,]*' | cut -d'=' -f2)
            local columns=$(echo "$params" | grep -o 'columns=[^,]*' | cut -d'=' -f2)
            postgresql_create_table "$table_name" "$columns"
            ;;
        "drop_table")
            local table_name=$(echo "$params" | grep -o 'table_name=[^,]*' | cut -d'=' -f2)
            local cascade=$(echo "$params" | grep -o 'cascade=[^,]*' | cut -d'=' -f2)
            postgresql_drop_table "$table_name" "$cascade"
            ;;
        "config")
            postgresql_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, test, query, execute_file, backup, restore, list_tables, describe_table, table_count, create_table, drop_table, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_postgresql postgresql_init postgresql_test_connection postgresql_query postgresql_execute_file postgresql_backup postgresql_restore postgresql_list_tables postgresql_describe_table postgresql_table_count postgresql_create_table postgresql_drop_table postgresql_config 
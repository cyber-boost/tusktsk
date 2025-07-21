#!/bin/bash

# Health Operator Implementation
# Provides system health checks and status monitoring

# Global variables
HEALTH_ENABLED="true"
HEALTH_CHECK_INTERVAL="300"
HEALTH_TIMEOUT="30"
HEALTH_RETRY_COUNT="3"
HEALTH_STORAGE_PATH="/tmp/health"
HEALTH_HISTORY_DAYS="7"

# Health status values
HEALTH_STATUS_HEALTHY="healthy"
HEALTH_STATUS_DEGRADED="degraded"
HEALTH_STATUS_UNHEALTHY="unhealthy"
HEALTH_STATUS_UNKNOWN="unknown"

# Initialize Health operator
health_init() {
    local enabled="$1"
    local check_interval="$2"
    local timeout="$3"
    local retry_count="$4"
    local storage_path="$5"
    local history_days="$6"
    
    HEALTH_ENABLED="${enabled:-true}"
    HEALTH_CHECK_INTERVAL="${check_interval:-300}"
    HEALTH_TIMEOUT="${timeout:-30}"
    HEALTH_RETRY_COUNT="${retry_count:-3}"
    HEALTH_STORAGE_PATH="${storage_path:-/tmp/health}"
    HEALTH_HISTORY_DAYS="${history_days:-7}"
    
    # Create storage directory if it doesn't exist
    mkdir -p "$HEALTH_STORAGE_PATH"
    
    echo "{\"status\":\"success\",\"message\":\"Health operator initialized\",\"enabled\":\"$HEALTH_ENABLED\",\"check_interval\":\"$HEALTH_CHECK_INTERVAL\",\"storage_path\":\"$HEALTH_STORAGE_PATH\"}"
}

# Check system health
health_check() {
    local check_type="$1"
    local threshold="$2"
    
    if [[ -z "$check_type" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Check type is required\"}"
        return 1
    fi
    
    if [[ "$HEALTH_ENABLED" != "true" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"Health checks are disabled\"}"
        return 0
    fi
    
    local timestamp=$(date +%s)
    local check_result=""
    local health_status="$HEALTH_STATUS_UNKNOWN"
    local details=""
    
    case "$check_type" in
        "system")
            check_result=$(health_check_system "$threshold")
            ;;
        "disk")
            check_result=$(health_check_disk "$threshold")
            ;;
        "memory")
            check_result=$(health_check_memory "$threshold")
            ;;
        "cpu")
            check_result=$(health_check_cpu "$threshold")
            ;;
        "network")
            check_result=$(health_check_network "$threshold")
            ;;
        "process")
            check_result=$(health_check_process "$threshold")
            ;;
        "service")
            local service_name="$3"
            check_result=$(health_check_service "$service_name" "$threshold")
            ;;
        "port")
            local port="$3"
            local host="$4"
            check_result=$(health_check_port "$port" "$host" "$threshold")
            ;;
        "url")
            local url="$3"
            check_result=$(health_check_url "$url" "$threshold")
            ;;
        "database")
            local db_type="$3"
            local db_config="$4"
            check_result=$(health_check_database "$db_type" "$db_config" "$threshold")
            ;;
        "custom")
            local command="$3"
            check_result=$(health_check_custom "$command" "$threshold")
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown check type: $check_type. Available types: system, disk, memory, cpu, network, process, service, port, url, database, custom\"}"
            return 1
            ;;
    esac
    
    # Parse check result
    if [[ $? -eq 0 ]]; then
        health_status=$(echo "$check_result" | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
        details=$(echo "$check_result" | grep -o '"details":"[^"]*"' | cut -d'"' -f4)
    fi
    
    # Store health check result
    local health_data="{\"timestamp\":$timestamp,\"check_type\":\"$check_type\",\"status\":\"$health_status\",\"details\":\"$details\"}"
    echo "$health_data" >> "$HEALTH_STORAGE_PATH/health_checks.log"
    
    echo "{\"status\":\"success\",\"message\":\"Health check completed\",\"check_type\":\"$check_type\",\"health_status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check system overall health
health_check_system() {
    local threshold="$1"
    threshold="${threshold:-80}"
    
    local cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
    local memory_usage=$(free | awk 'NR==2{printf "%.0f", $3*100/$2}')
    local disk_usage=$(df -h / | awk 'NR==2 {print $5}' | cut -d'%' -f1)
    local load_average=$(uptime | awk -F'load average:' '{print $2}' | awk '{print $1}' | cut -d',' -f1)
    
    local health_status="$HEALTH_STATUS_HEALTHY"
    local details="CPU: ${cpu_usage}%, Memory: ${memory_usage}%, Disk: ${disk_usage}%, Load: ${load_average}"
    
    # Check if any metric exceeds threshold
    if [[ $cpu_usage -gt $threshold ]] || [[ $memory_usage -gt $threshold ]] || [[ $disk_usage -gt $threshold ]]; then
        health_status="$HEALTH_STATUS_DEGRADED"
    fi
    
    if [[ $cpu_usage -gt 95 ]] || [[ $memory_usage -gt 95 ]] || [[ $disk_usage -gt 95 ]]; then
        health_status="$HEALTH_STATUS_UNHEALTHY"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check disk health
health_check_disk() {
    local threshold="$1"
    threshold="${threshold:-80}"
    
    local disk_usage=$(df -h / | awk 'NR==2 {print $5}' | cut -d'%' -f1)
    local inode_usage=$(df -i / | awk 'NR==2 {print $5}' | cut -d'%' -f1)
    
    local health_status="$HEALTH_STATUS_HEALTHY"
    local details="Disk usage: ${disk_usage}%, Inode usage: ${inode_usage}%"
    
    if [[ $disk_usage -gt $threshold ]] || [[ $inode_usage -gt $threshold ]]; then
        health_status="$HEALTH_STATUS_DEGRADED"
    fi
    
    if [[ $disk_usage -gt 95 ]] || [[ $inode_usage -gt 95 ]]; then
        health_status="$HEALTH_STATUS_UNHEALTHY"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check memory health
health_check_memory() {
    local threshold="$1"
    threshold="${threshold:-80}"
    
    local memory_usage=$(free | awk 'NR==2{printf "%.0f", $3*100/$2}')
    local swap_usage=$(free | awk 'NR==3{printf "%.0f", $3*100/$2}')
    
    local health_status="$HEALTH_STATUS_HEALTHY"
    local details="Memory usage: ${memory_usage}%, Swap usage: ${swap_usage}%"
    
    if [[ $memory_usage -gt $threshold ]]; then
        health_status="$HEALTH_STATUS_DEGRADED"
    fi
    
    if [[ $memory_usage -gt 95 ]] || [[ $swap_usage -gt 80 ]]; then
        health_status="$HEALTH_STATUS_UNHEALTHY"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check CPU health
health_check_cpu() {
    local threshold="$1"
    threshold="${threshold:-80}"
    
    local cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
    local load_average=$(uptime | awk -F'load average:' '{print $2}' | awk '{print $1}' | cut -d',' -f1)
    local cpu_cores=$(nproc)
    
    local health_status="$HEALTH_STATUS_HEALTHY"
    local details="CPU usage: ${cpu_usage}%, Load average: ${load_average}, Cores: ${cpu_cores}"
    
    if [[ $cpu_usage -gt $threshold ]]; then
        health_status="$HEALTH_STATUS_DEGRADED"
    fi
    
    if [[ $cpu_usage -gt 95 ]]; then
        health_status="$HEALTH_STATUS_UNHEALTHY"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check network health
health_check_network() {
    local threshold="$1"
    threshold="${threshold:-1000}"
    
    local connections=$(netstat -an | wc -l)
    local established=$(netstat -an | grep ESTABLISHED | wc -l)
    local listening=$(netstat -an | grep LISTEN | wc -l)
    
    local health_status="$HEALTH_STATUS_HEALTHY"
    local details="Total connections: ${connections}, Established: ${established}, Listening: ${listening}"
    
    if [[ $connections -gt $threshold ]]; then
        health_status="$HEALTH_STATUS_DEGRADED"
    fi
    
    if [[ $connections -gt $((threshold * 2)) ]]; then
        health_status="$HEALTH_STATUS_UNHEALTHY"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check process health
health_check_process() {
    local threshold="$1"
    threshold="${threshold:-1000}"
    
    local process_count=$(ps aux | wc -l)
    local zombie_count=$(ps aux | grep -c 'Z')
    local running_count=$(ps aux | grep -c 'R')
    
    local health_status="$HEALTH_STATUS_HEALTHY"
    local details="Total processes: ${process_count}, Zombies: ${zombie_count}, Running: ${running_count}"
    
    if [[ $process_count -gt $threshold ]] || [[ $zombie_count -gt 10 ]]; then
        health_status="$HEALTH_STATUS_DEGRADED"
    fi
    
    if [[ $zombie_count -gt 50 ]]; then
        health_status="$HEALTH_STATUS_UNHEALTHY"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check service health
health_check_service() {
    local service_name="$1"
    local threshold="$2"
    
    if [[ -z "$service_name" ]]; then
        echo "{\"status\":\"$HEALTH_STATUS_UNKNOWN\",\"details\":\"Service name not provided\"}"
        return 1
    fi
    
    local service_status="unknown"
    local health_status="$HEALTH_STATUS_UNKNOWN"
    local details="Service: $service_name"
    
    # Check if systemctl is available
    if command -v systemctl >/dev/null 2>&1; then
        if systemctl is-active --quiet "$service_name"; then
            service_status="active"
            health_status="$HEALTH_STATUS_HEALTHY"
        else
            service_status="inactive"
            health_status="$HEALTH_STATUS_UNHEALTHY"
        fi
    else
        # Fallback to service command
        if service "$service_name" status >/dev/null 2>&1; then
            service_status="running"
            health_status="$HEALTH_STATUS_HEALTHY"
        else
            service_status="stopped"
            health_status="$HEALTH_STATUS_UNHEALTHY"
        fi
    fi
    
    details="$details, Status: $service_status"
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check port health
health_check_port() {
    local port="$1"
    local host="$2"
    local threshold="$3"
    
    if [[ -z "$port" ]]; then
        echo "{\"status\":\"$HEALTH_STATUS_UNKNOWN\",\"details\":\"Port not provided\"}"
        return 1
    fi
    
    host="${host:-localhost}"
    threshold="${threshold:-5}"
    
    local health_status="$HEALTH_STATUS_UNKNOWN"
    local details="Port: $port, Host: $host"
    
    # Check if port is listening
    if netstat -tuln | grep -q ":$port "; then
        health_status="$HEALTH_STATUS_HEALTHY"
        details="$details, Status: listening"
    else
        health_status="$HEALTH_STATUS_UNHEALTHY"
        details="$details, Status: not listening"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check URL health
health_check_url() {
    local url="$1"
    local threshold="$2"
    
    if [[ -z "$url" ]]; then
        echo "{\"status\":\"$HEALTH_STATUS_UNKNOWN\",\"details\":\"URL not provided\"}"
        return 1
    fi
    
    threshold="${threshold:-5}"
    
    local health_status="$HEALTH_STATUS_UNKNOWN"
    local details="URL: $url"
    
    # Check URL with curl
    local response=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout $threshold "$url" 2>/dev/null)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        if [[ $response -ge 200 && $response -lt 400 ]]; then
            health_status="$HEALTH_STATUS_HEALTHY"
            details="$details, Status: $response"
        else
            health_status="$HEALTH_STATUS_DEGRADED"
            details="$details, Status: $response"
        fi
    else
        health_status="$HEALTH_STATUS_UNHEALTHY"
        details="$details, Error: connection failed"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check database health
health_check_database() {
    local db_type="$1"
    local db_config="$2"
    local threshold="$3"
    
    if [[ -z "$db_type" ]]; then
        echo "{\"status\":\"$HEALTH_STATUS_UNKNOWN\",\"details\":\"Database type not provided\"}"
        return 1
    fi
    
    threshold="${threshold:-5}"
    local health_status="$HEALTH_STATUS_UNKNOWN"
    local details="Database: $db_type"
    
    case "$db_type" in
        "postgresql")
            if command -v psql >/dev/null 2>&1; then
                local response=$(PGPASSWORD="$db_config" psql -h localhost -U postgres -d postgres -c "SELECT 1;" 2>/dev/null)
                if [[ $? -eq 0 ]]; then
                    health_status="$HEALTH_STATUS_HEALTHY"
                    details="$details, Status: connected"
                else
                    health_status="$HEALTH_STATUS_UNHEALTHY"
                    details="$details, Status: connection failed"
                fi
            fi
            ;;
        "mysql")
            if command -v mysql >/dev/null 2>&1; then
                local response=$(MYSQL_PWD="$db_config" mysql -h localhost -u root -e "SELECT 1;" 2>/dev/null)
                if [[ $? -eq 0 ]]; then
                    health_status="$HEALTH_STATUS_HEALTHY"
                    details="$details, Status: connected"
                else
                    health_status="$HEALTH_STATUS_UNHEALTHY"
                    details="$details, Status: connection failed"
                fi
            fi
            ;;
        "redis")
            if command -v redis-cli >/dev/null 2>&1; then
                local response=$(redis-cli ping 2>/dev/null)
                if [[ "$response" == "PONG" ]]; then
                    health_status="$HEALTH_STATUS_HEALTHY"
                    details="$details, Status: connected"
                else
                    health_status="$HEALTH_STATUS_UNHEALTHY"
                    details="$details, Status: connection failed"
                fi
            fi
            ;;
        *)
            health_status="$HEALTH_STATUS_UNKNOWN"
            details="$details, Error: unsupported database type"
            ;;
    esac
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Check custom health
health_check_custom() {
    local command="$1"
    local threshold="$2"
    
    if [[ -z "$command" ]]; then
        echo "{\"status\":\"$HEALTH_STATUS_UNKNOWN\",\"details\":\"Command not provided\"}"
        return 1
    fi
    
    threshold="${threshold:-5}"
    local health_status="$HEALTH_STATUS_UNKNOWN"
    local details="Command: $command"
    
    # Execute custom command with timeout
    local output=$(timeout $threshold bash -c "$command" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        health_status="$HEALTH_STATUS_HEALTHY"
        details="$details, Status: success"
    else
        health_status="$HEALTH_STATUS_UNHEALTHY"
        details="$details, Status: failed (exit code: $exit_code)"
    fi
    
    echo "{\"status\":\"$health_status\",\"details\":\"$details\"}"
}

# Get overall system health
health_status() {
    local check_types="$1"
    
    check_types="${check_types:-system,disk,memory,cpu}"
    
    local overall_status="$HEALTH_STATUS_HEALTHY"
    local check_results="[]"
    local count=0
    
    # Split check types
    IFS=',' read -ra CHECK_ARRAY <<< "$check_types"
    
    for check_type in "${CHECK_ARRAY[@]}"; do
        local result=$(health_check "$check_type")
        if [[ $? -eq 0 ]]; then
            local status=$(echo "$result" | grep -o '"health_status":"[^"]*"' | cut -d'"' -f4)
            local details=$(echo "$result" | grep -o '"details":"[^"]*"' | cut -d'"' -f4)
            
            if [[ $count -eq 0 ]]; then
                check_results="[{\"type\":\"$check_type\",\"status\":\"$status\",\"details\":\"$details\"}]"
            else
                check_results=$(echo "$check_results" | sed 's/\]$//')
                check_results="$check_results,{\"type\":\"$check_type\",\"status\":\"$status\",\"details\":\"$details\"}]"
            fi
            ((count++))
            
            # Update overall status
            if [[ "$status" == "$HEALTH_STATUS_UNHEALTHY" ]]; then
                overall_status="$HEALTH_STATUS_UNHEALTHY"
            elif [[ "$status" == "$HEALTH_STATUS_DEGRADED" && "$overall_status" != "$HEALTH_STATUS_UNHEALTHY" ]]; then
                overall_status="$HEALTH_STATUS_DEGRADED"
            fi
        fi
    done
    
    echo "{\"status\":\"success\",\"message\":\"System health status\",\"overall_status\":\"$overall_status\",\"checks\":$check_results}"
}

# Get health history
health_history() {
    local hours="$1"
    local check_type="$2"
    local output_file="$3"
    
    hours="${hours:-24}"
    local cutoff_time=$(date -d "$hours hours ago" +%s)
    local health_file="$HEALTH_STORAGE_PATH/health_checks.log"
    
    if [[ ! -f "$health_file" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"No health history found\",\"checks\":[]}"
        return 0
    fi
    
    local filtered_checks="[]"
    local count=0
    
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            local timestamp=$(echo "$line" | grep -o '"timestamp":[0-9]*' | cut -d':' -f2)
            local line_check_type=$(echo "$line" | grep -o '"check_type":"[^"]*"' | cut -d'"' -f4)
            
            if [[ -n "$timestamp" && $timestamp -ge $cutoff_time ]]; then
                if [[ -z "$check_type" || "$line_check_type" == "$check_type" ]]; then
                    if [[ $count -eq 0 ]]; then
                        filtered_checks="[$line"
                    else
                        filtered_checks="$filtered_checks,$line"
                    fi
                    ((count++))
                fi
            fi
        fi
    done < "$health_file"
    
    if [[ $count -gt 0 ]]; then
        filtered_checks="$filtered_checks]"
    fi
    
    # Save to output file if specified
    if [[ -n "$output_file" ]]; then
        echo "$filtered_checks" > "$output_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Health history retrieved\",\"hours\":$hours,\"check_type\":\"$check_type\",\"count\":$count,\"checks\":$filtered_checks}"
}

# Clean old health checks
health_cleanup() {
    local days="$1"
    
    days="${days:-$HEALTH_HISTORY_DAYS}"
    local cutoff_time=$(date -d "$days days ago" +%s)
    local health_file="$HEALTH_STORAGE_PATH/health_checks.log"
    local cleaned_count=0
    
    if [[ -f "$health_file" ]]; then
        local temp_file=$(mktemp)
        while IFS= read -r line; do
            if [[ -n "$line" ]]; then
                local timestamp=$(echo "$line" | grep -o '"timestamp":[0-9]*' | cut -d':' -f2)
                if [[ -n "$timestamp" && $timestamp -ge $cutoff_time ]]; then
                    echo "$line" >> "$temp_file"
                else
                    ((cleaned_count++))
                fi
            fi
        done < "$health_file"
        mv "$temp_file" "$health_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Health cleanup completed\",\"days\":$days,\"cleaned_checks\":$cleaned_count}"
}

# Get health configuration
health_config() {
    echo "{\"status\":\"success\",\"config\":{\"enabled\":\"$HEALTH_ENABLED\",\"check_interval\":\"$HEALTH_CHECK_INTERVAL\",\"timeout\":\"$HEALTH_TIMEOUT\",\"retry_count\":\"$HEALTH_RETRY_COUNT\",\"storage_path\":\"$HEALTH_STORAGE_PATH\",\"history_days\":\"$HEALTH_HISTORY_DAYS\"}}"
}

# Main Health operator function
execute_health() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local enabled=$(echo "$params" | grep -o 'enabled=[^,]*' | cut -d'=' -f2)
            local check_interval=$(echo "$params" | grep -o 'check_interval=[^,]*' | cut -d'=' -f2)
            local timeout=$(echo "$params" | grep -o 'timeout=[^,]*' | cut -d'=' -f2)
            local retry_count=$(echo "$params" | grep -o 'retry_count=[^,]*' | cut -d'=' -f2)
            local storage_path=$(echo "$params" | grep -o 'storage_path=[^,]*' | cut -d'=' -f2)
            local history_days=$(echo "$params" | grep -o 'history_days=[^,]*' | cut -d'=' -f2)
            health_init "$enabled" "$check_interval" "$timeout" "$retry_count" "$storage_path" "$history_days"
            ;;
        "check")
            local check_type=$(echo "$params" | grep -o 'check_type=[^,]*' | cut -d'=' -f2)
            local threshold=$(echo "$params" | grep -o 'threshold=[^,]*' | cut -d'=' -f2)
            local service_name=$(echo "$params" | grep -o 'service_name=[^,]*' | cut -d'=' -f2)
            local port=$(echo "$params" | grep -o 'port=[^,]*' | cut -d'=' -f2)
            local host=$(echo "$params" | grep -o 'host=[^,]*' | cut -d'=' -f2)
            local url=$(echo "$params" | grep -o 'url=[^,]*' | cut -d'=' -f2)
            local db_type=$(echo "$params" | grep -o 'db_type=[^,]*' | cut -d'=' -f2)
            local db_config=$(echo "$params" | grep -o 'db_config=[^,]*' | cut -d'=' -f2)
            local command=$(echo "$params" | grep -o 'command=[^,]*' | cut -d'=' -f2)
            
            case "$check_type" in
                "service")
                    health_check "$check_type" "$threshold" "$service_name"
                    ;;
                "port")
                    health_check "$check_type" "$threshold" "$port" "$host"
                    ;;
                "url")
                    health_check "$check_type" "$threshold" "$url"
                    ;;
                "database")
                    health_check "$check_type" "$threshold" "$db_type" "$db_config"
                    ;;
                "custom")
                    health_check "$check_type" "$threshold" "$command"
                    ;;
                *)
                    health_check "$check_type" "$threshold"
                    ;;
            esac
            ;;
        "status")
            local check_types=$(echo "$params" | grep -o 'check_types=[^,]*' | cut -d'=' -f2)
            health_status "$check_types"
            ;;
        "history")
            local hours=$(echo "$params" | grep -o 'hours=[^,]*' | cut -d'=' -f2)
            local check_type=$(echo "$params" | grep -o 'check_type=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            health_history "$hours" "$check_type" "$output_file"
            ;;
        "cleanup")
            local days=$(echo "$params" | grep -o 'days=[^,]*' | cut -d'=' -f2)
            health_cleanup "$days"
            ;;
        "config")
            health_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, check, status, history, cleanup, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_health health_init health_check health_check_system health_check_disk health_check_memory health_check_cpu health_check_network health_check_process health_check_service health_check_port health_check_url health_check_database health_check_custom health_status health_history health_cleanup health_config 
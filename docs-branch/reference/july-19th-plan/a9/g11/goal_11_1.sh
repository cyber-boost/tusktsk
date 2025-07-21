#!/bin/bash

# Metrics and Logs Operators Implementation
# Provides system monitoring and log management

# Global variables
METRICS_INTERVAL="60"
METRICS_RETENTION_DAYS="30"
METRICS_STORAGE_PATH="/tmp/metrics"
METRICS_FORMAT="json"
METRICS_ENABLED="true"

LOGS_PATH="/var/log"
LOGS_PATTERNS="*.log"
LOGS_MAX_SIZE="100MB"
LOGS_COMPRESSION="gzip"
LOGS_ROTATION="daily"
LOGS_ENABLED="true"

# Initialize Metrics operator
metrics_init() {
    local interval="$1"
    local retention_days="$2"
    local storage_path="$3"
    local format="$4"
    
    METRICS_INTERVAL="${interval:-60}"
    METRICS_RETENTION_DAYS="${retention_days:-30}"
    METRICS_STORAGE_PATH="${storage_path:-/tmp/metrics}"
    METRICS_FORMAT="${format:-json}"
    
    # Create storage directory if it doesn't exist
    mkdir -p "$METRICS_STORAGE_PATH"
    
    echo "{\"status\":\"success\",\"message\":\"Metrics operator initialized\",\"interval\":\"$METRICS_INTERVAL\",\"retention_days\":\"$METRICS_RETENTION_DAYS\",\"storage_path\":\"$METRICS_STORAGE_PATH\"}"
}

# Collect system metrics
metrics_collect() {
    local metric_type="$1"
    local output_file="$2"
    
    if [[ -z "$metric_type" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Metric type is required\"}"
        return 1
    fi
    
    local timestamp=$(date +%s)
    local metrics_data=""
    
    case "$metric_type" in
        "system")
            # System metrics
            local cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
            local memory_total=$(free -m | awk 'NR==2{printf "%.2f", $2/1024}')
            local memory_used=$(free -m | awk 'NR==2{printf "%.2f", $3/1024}')
            local memory_free=$(free -m | awk 'NR==2{printf "%.2f", $4/1024}')
            local disk_usage=$(df -h / | awk 'NR==2 {print $5}' | cut -d'%' -f1)
            local load_average=$(uptime | awk -F'load average:' '{print $2}' | awk '{print $1}' | cut -d',' -f1)
            
            metrics_data="{\"timestamp\":$timestamp,\"type\":\"system\",\"cpu_usage\":$cpu_usage,\"memory_total_gb\":$memory_total,\"memory_used_gb\":$memory_used,\"memory_free_gb\":$memory_free,\"disk_usage_percent\":$disk_usage,\"load_average\":$load_average}"
            ;;
        "process")
            # Process metrics
            local process_count=$(ps aux | wc -l)
            local zombie_count=$(ps aux | grep -c 'Z')
            local running_count=$(ps aux | grep -c 'R')
            local sleeping_count=$(ps aux | grep -c 'S')
            
            metrics_data="{\"timestamp\":$timestamp,\"type\":\"process\",\"total_processes\":$process_count,\"zombie_processes\":$zombie_count,\"running_processes\":$running_count,\"sleeping_processes\":$sleeping_count}"
            ;;
        "network")
            # Network metrics
            local bytes_in=$(cat /proc/net/dev | grep eth0 | awk '{print $2}')
            local bytes_out=$(cat /proc/net/dev | grep eth0 | awk '{print $10}')
            local connections=$(netstat -an | wc -l)
            local established=$(netstat -an | grep ESTABLISHED | wc -l)
            
            metrics_data="{\"timestamp\":$timestamp,\"type\":\"network\",\"bytes_in\":$bytes_in,\"bytes_out\":$bytes_out,\"total_connections\":$connections,\"established_connections\":$established}"
            ;;
        "disk")
            # Disk metrics
            local disk_io=$(iostat -x 1 1 | grep -A 1 'Device' | tail -1 | awk '{print $6}')
            local disk_util=$(iostat -x 1 1 | grep -A 1 'Device' | tail -1 | awk '{print $14}')
            local disk_read=$(cat /proc/diskstats | grep 'sda ' | awk '{print $6}')
            local disk_write=$(cat /proc/diskstats | grep 'sda ' | awk '{print $10}')
            
            metrics_data="{\"timestamp\":$timestamp,\"type\":\"disk\",\"io_operations\":$disk_io,\"utilization_percent\":$disk_util,\"sectors_read\":$disk_read,\"sectors_written\":$disk_write}"
            ;;
        "custom")
            # Custom metrics
            local custom_data="$3"
            if [[ -z "$custom_data" ]]; then
                echo "{\"status\":\"error\",\"message\":\"Custom data is required for custom metrics\"}"
                return 1
            fi
            metrics_data="{\"timestamp\":$timestamp,\"type\":\"custom\",\"data\":$custom_data}"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown metric type: $metric_type. Available types: system, process, network, disk, custom\"}"
            return 1
            ;;
    esac
    
    # Save metrics to file if specified
    if [[ -n "$output_file" ]]; then
        echo "$metrics_data" >> "$output_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Metrics collected\",\"type\":\"$metric_type\",\"data\":$metrics_data}"
}

# Get metrics history
metrics_history() {
    local metric_type="$1"
    local hours="$2"
    local output_file="$3"
    
    if [[ -z "$metric_type" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Metric type is required\"}"
        return 1
    fi
    
    hours="${hours:-24}"
    local cutoff_time=$(date -d "$hours hours ago" +%s)
    local history_file="$METRICS_STORAGE_PATH/${metric_type}_metrics.log"
    
    if [[ ! -f "$history_file" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"No metrics history found for type: $metric_type\"}"
        return 0
    fi
    
    local filtered_metrics="[]"
    local count=0
    
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            local timestamp=$(echo "$line" | grep -o '"timestamp":[0-9]*' | cut -d':' -f2)
            if [[ -n "$timestamp" && $timestamp -ge $cutoff_time ]]; then
                if [[ $count -eq 0 ]]; then
                    filtered_metrics="[$line"
                else
                    filtered_metrics="$filtered_metrics,$line"
                fi
                ((count++))
            fi
        fi
    done < "$history_file"
    
    if [[ $count -gt 0 ]]; then
        filtered_metrics="$filtered_metrics]"
    fi
    
    # Save to output file if specified
    if [[ -n "$output_file" ]]; then
        echo "$filtered_metrics" > "$output_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Metrics history retrieved\",\"type\":\"$metric_type\",\"hours\":$hours,\"count\":$count,\"data\":$filtered_metrics}"
}

# Clean old metrics
metrics_cleanup() {
    local days="$1"
    
    days="${days:-$METRICS_RETENTION_DAYS}"
    local cutoff_time=$(date -d "$days days ago" +%s)
    local cleaned_count=0
    
    # Clean up metrics files
    for file in "$METRICS_STORAGE_PATH"/*_metrics.log; do
        if [[ -f "$file" ]]; then
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
            done < "$file"
            mv "$temp_file" "$file"
        fi
    done
    
    echo "{\"status\":\"success\",\"message\":\"Metrics cleanup completed\",\"days\":$days,\"cleaned_entries\":$cleaned_count}"
}

# Initialize Logs operator
logs_init() {
    local path="$1"
    local patterns="$2"
    local max_size="$3"
    local compression="$4"
    local rotation="$5"
    
    LOGS_PATH="${path:-/var/log}"
    LOGS_PATTERNS="${patterns:-*.log}"
    LOGS_MAX_SIZE="${max_size:-100MB}"
    LOGS_COMPRESSION="${compression:-gzip}"
    LOGS_ROTATION="${rotation:-daily}"
    
    if [[ ! -d "$LOGS_PATH" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Logs directory not found: $LOGS_PATH\"}"
        return 1
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Logs operator initialized\",\"path\":\"$LOGS_PATH\",\"patterns\":\"$LOGS_PATTERNS\",\"max_size\":\"$LOGS_MAX_SIZE\"}"
}

# Search logs
logs_search() {
    local pattern="$1"
    local file_pattern="$2"
    local lines="$3"
    local output_file="$4"
    
    if [[ -z "$pattern" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Search pattern is required\"}"
        return 1
    fi
    
    file_pattern="${file_pattern:-$LOGS_PATTERNS}"
    lines="${lines:-100}"
    
    local search_results="[]"
    local total_matches=0
    
    # Find log files matching pattern
    while IFS= read -r -d '' file; do
        if [[ -f "$file" ]]; then
            local file_matches=$(grep -n "$pattern" "$file" | tail -n "$lines" | while IFS= read -r line; do
                local line_num=$(echo "$line" | cut -d':' -f1)
                local content=$(echo "$line" | cut -d':' -f2-)
                echo "{\"file\":\"$file\",\"line\":$line_num,\"content\":\"$content\"}"
            done)
            
            if [[ -n "$file_matches" ]]; then
                local file_count=$(echo "$file_matches" | wc -l)
                ((total_matches += file_count))
                
                if [[ "$search_results" == "[]" ]]; then
                    search_results="[$(echo "$file_matches" | tr '\n' ',' | sed 's/,$//')]"
                else
                    search_results=$(echo "$search_results" | sed 's/\]$//')
                    search_results="$search_results,$(echo "$file_matches" | tr '\n' ',' | sed 's/,$//')]"
                fi
            fi
        fi
    done < <(find "$LOGS_PATH" -name "$file_pattern" -type f -print0)
    
    # Save to output file if specified
    if [[ -n "$output_file" ]]; then
        echo "$search_results" > "$output_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Log search completed\",\"pattern\":\"$pattern\",\"total_matches\":$total_matches,\"results\":$search_results}"
}

# Analyze logs
logs_analyze() {
    local file_pattern="$1"
    local analysis_type="$2"
    local output_file="$3"
    
    file_pattern="${file_pattern:-$LOGS_PATTERNS}"
    
    if [[ -z "$analysis_type" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Analysis type is required\"}"
        return 1
    fi
    
    local analysis_data=""
    
    case "$analysis_type" in
        "error_count")
            # Count errors in logs
            local error_count=0
            while IFS= read -r -d '' file; do
                if [[ -f "$file" ]]; then
                    local file_errors=$(grep -i "error\|exception\|fail" "$file" | wc -l)
                    ((error_count += file_errors))
                fi
            done < <(find "$LOGS_PATH" -name "$file_pattern" -type f -print0)
            analysis_data="{\"type\":\"error_count\",\"total_errors\":$error_count}"
            ;;
        "top_ips")
            # Find top IP addresses
            local ip_counts=$(find "$LOGS_PATH" -name "$file_pattern" -type f -exec grep -oE '\b([0-9]{1,3}\.){3}[0-9]{1,3}\b' {} \; | sort | uniq -c | sort -nr | head -10 | while IFS= read -r count ip; do
                echo "{\"ip\":\"$ip\",\"count\":$count}"
            done)
            analysis_data="{\"type\":\"top_ips\",\"ips\":[$ip_counts]}"
            ;;
        "top_urls")
            # Find top URLs
            local url_counts=$(find "$LOGS_PATH" -name "$file_pattern" -type f -exec grep -oE 'GET [^ ]+' {} \; | sort | uniq -c | sort -nr | head -10 | while IFS= read -r count url; do
                echo "{\"url\":\"$url\",\"count\":$count}"
            done)
            analysis_data="{\"type\":\"top_urls\",\"urls\":[$url_counts]}"
            ;;
        "file_sizes")
            # Analyze log file sizes
            local size_data="[]"
            local total_size=0
            while IFS= read -r -d '' file; do
                if [[ -f "$file" ]]; then
                    local file_size=$(stat -c%s "$file")
                    local file_size_mb=$(echo "scale=2; $file_size / 1024 / 1024" | bc)
                    ((total_size += file_size))
                    
                    if [[ "$size_data" == "[]" ]]; then
                        size_data="[{\"file\":\"$file\",\"size_mb\":$file_size_mb}]"
                    else
                        size_data=$(echo "$size_data" | sed 's/\]$//')
                        size_data="$size_data,{\"file\":\"$file\",\"size_mb\":$file_size_mb}]"
                    fi
                fi
            done < <(find "$LOGS_PATH" -name "$file_pattern" -type f -print0)
            
            local total_size_mb=$(echo "scale=2; $total_size / 1024 / 1024" | bc)
            analysis_data="{\"type\":\"file_sizes\",\"total_size_mb\":$total_size_mb,\"files\":$size_data}"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown analysis type: $analysis_type. Available types: error_count, top_ips, top_urls, file_sizes\"}"
            return 1
            ;;
    esac
    
    # Save to output file if specified
    if [[ -n "$output_file" ]]; then
        echo "$analysis_data" > "$output_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Log analysis completed\",\"type\":\"$analysis_type\",\"data\":$analysis_data}"
}

# Rotate logs
logs_rotate() {
    local file_pattern="$1"
    local max_size="$2"
    
    file_pattern="${file_pattern:-$LOGS_PATTERNS}"
    max_size="${max_size:-$LOGS_MAX_SIZE}"
    
    local rotated_count=0
    
    # Convert max_size to bytes
    local max_size_bytes=0
    if [[ "$max_size" == *"MB" ]]; then
        max_size_bytes=$(echo "${max_size%MB} * 1024 * 1024" | bc)
    elif [[ "$max_size" == *"GB" ]]; then
        max_size_bytes=$(echo "${max_size%GB} * 1024 * 1024 * 1024" | bc)
    else
        max_size_bytes=$max_size
    fi
    
    while IFS= read -r -d '' file; do
        if [[ -f "$file" ]]; then
            local file_size=$(stat -c%s "$file")
            if [[ $file_size -gt $max_size_bytes ]]; then
                local timestamp=$(date +%Y%m%d_%H%M%S)
                local rotated_file="${file}.${timestamp}"
                
                # Rotate the file
                mv "$file" "$rotated_file"
                
                # Compress if enabled
                if [[ "$LOGS_COMPRESSION" == "gzip" ]]; then
                    gzip "$rotated_file"
                fi
                
                ((rotated_count++))
            fi
        fi
    done < <(find "$LOGS_PATH" -name "$file_pattern" -type f -print0)
    
    echo "{\"status\":\"success\",\"message\":\"Log rotation completed\",\"rotated_files\":$rotated_count,\"max_size\":\"$max_size\"}"
}

# Get metrics configuration
metrics_config() {
    echo "{\"status\":\"success\",\"config\":{\"interval\":\"$METRICS_INTERVAL\",\"retention_days\":\"$METRICS_RETENTION_DAYS\",\"storage_path\":\"$METRICS_STORAGE_PATH\",\"format\":\"$METRICS_FORMAT\"}}"
}

# Get logs configuration
logs_config() {
    echo "{\"status\":\"success\",\"config\":{\"path\":\"$LOGS_PATH\",\"patterns\":\"$LOGS_PATTERNS\",\"max_size\":\"$LOGS_MAX_SIZE\",\"compression\":\"$LOGS_COMPRESSION\",\"rotation\":\"$LOGS_ROTATION\"}}"
}

# Main Metrics operator function
execute_metrics() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local interval=$(echo "$params" | grep -o 'interval=[^,]*' | cut -d'=' -f2)
            local retention_days=$(echo "$params" | grep -o 'retention_days=[^,]*' | cut -d'=' -f2)
            local storage_path=$(echo "$params" | grep -o 'storage_path=[^,]*' | cut -d'=' -f2)
            local format=$(echo "$params" | grep -o 'format=[^,]*' | cut -d'=' -f2)
            metrics_init "$interval" "$retention_days" "$storage_path" "$format"
            ;;
        "collect")
            local metric_type=$(echo "$params" | grep -o 'metric_type=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            local custom_data=$(echo "$params" | grep -o 'custom_data=[^,]*' | cut -d'=' -f2)
            if [[ "$metric_type" == "custom" ]]; then
                metrics_collect "$metric_type" "$output_file" "$custom_data"
            else
                metrics_collect "$metric_type" "$output_file"
            fi
            ;;
        "history")
            local metric_type=$(echo "$params" | grep -o 'metric_type=[^,]*' | cut -d'=' -f2)
            local hours=$(echo "$params" | grep -o 'hours=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            metrics_history "$metric_type" "$hours" "$output_file"
            ;;
        "cleanup")
            local days=$(echo "$params" | grep -o 'days=[^,]*' | cut -d'=' -f2)
            metrics_cleanup "$days"
            ;;
        "config")
            metrics_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, collect, history, cleanup, config\"}"
            return 1
            ;;
    esac
}

# Main Logs operator function
execute_logs() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local path=$(echo "$params" | grep -o 'path=[^,]*' | cut -d'=' -f2)
            local patterns=$(echo "$params" | grep -o 'patterns=[^,]*' | cut -d'=' -f2)
            local max_size=$(echo "$params" | grep -o 'max_size=[^,]*' | cut -d'=' -f2)
            local compression=$(echo "$params" | grep -o 'compression=[^,]*' | cut -d'=' -f2)
            local rotation=$(echo "$params" | grep -o 'rotation=[^,]*' | cut -d'=' -f2)
            logs_init "$path" "$patterns" "$max_size" "$compression" "$rotation"
            ;;
        "search")
            local pattern=$(echo "$params" | grep -o 'pattern=[^,]*' | cut -d'=' -f2)
            local file_pattern=$(echo "$params" | grep -o 'file_pattern=[^,]*' | cut -d'=' -f2)
            local lines=$(echo "$params" | grep -o 'lines=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            logs_search "$pattern" "$file_pattern" "$lines" "$output_file"
            ;;
        "analyze")
            local file_pattern=$(echo "$params" | grep -o 'file_pattern=[^,]*' | cut -d'=' -f2)
            local analysis_type=$(echo "$params" | grep -o 'analysis_type=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            logs_analyze "$file_pattern" "$analysis_type" "$output_file"
            ;;
        "rotate")
            local file_pattern=$(echo "$params" | grep -o 'file_pattern=[^,]*' | cut -d'=' -f2)
            local max_size=$(echo "$params" | grep -o 'max_size=[^,]*' | cut -d'=' -f2)
            logs_rotate "$file_pattern" "$max_size"
            ;;
        "config")
            logs_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, search, analyze, rotate, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_metrics execute_logs metrics_init metrics_collect metrics_history metrics_cleanup logs_init logs_search logs_analyze logs_rotate metrics_config logs_config 
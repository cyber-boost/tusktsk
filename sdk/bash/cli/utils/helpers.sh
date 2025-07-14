#!/usr/bin/env bash

# TuskLang CLI Helpers
# ====================
# Common utility functions used across the CLI

set -euo pipefail

##
# Check if a command exists
#
# @param $1 Command name
# @return 0 if exists, 1 if not
#
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

##
# Check if a file exists and is readable
#
# @param $1 File path
# @return 0 if exists and readable, 1 if not
#
file_exists() {
    [[ -f "$1" && -r "$1" ]]
}

##
# Check if a directory exists and is readable
#
# @param $1 Directory path
# @return 0 if exists and readable, 1 if not
#
dir_exists() {
    [[ -d "$1" && -r "$1" ]]
}

##
# Get the absolute path of a file or directory
#
# @param $1 Path to resolve
# @return Absolute path
#
get_absolute_path() {
    local path="$1"
    
    if [[ -d "$path" ]]; then
        cd "$path" && pwd
    else
        cd "$(dirname "$path")" && echo "$(pwd)/$(basename "$path")"
    fi
}

##
# Create a temporary file with a specific extension
#
# @param $1 Extension (without dot)
# @return Path to temporary file
#
create_temp_file() {
    local extension="${1:-tmp}"
    mktemp --suffix=".$extension"
}

##
# Create a temporary directory
#
# @param $1 Optional prefix
# @return Path to temporary directory
#
create_temp_dir() {
    local prefix="${1:-tsk}"
    mktemp -d --tmpdir="${prefix}_XXXXXX"
}

##
# Safely remove a file if it exists
#
# @param $1 File path
#
safe_remove() {
    local file="$1"
    if [[ -f "$file" ]]; then
        rm -f "$file"
    fi
}

##
# Safely remove a directory if it exists
#
# @param $1 Directory path
#
safe_remove_dir() {
    local dir="$1"
    if [[ -d "$dir" ]]; then
        rm -rf "$dir"
    fi
}

##
# Get the current timestamp
#
# @return Unix timestamp
#
get_timestamp() {
    date +%s
}

##
# Get the current timestamp with milliseconds
#
# @return Unix timestamp with milliseconds
#
get_timestamp_ms() {
    date +%s%3N
}

##
# Format a timestamp as a human-readable date
#
# @param $1 Unix timestamp
# @param $2 Format string (optional, default: ISO format)
# @return Formatted date string
#
format_timestamp() {
    local timestamp="$1"
    local format="${2:-%Y-%m-%d %H:%M:%S}"
    
    if [[ "$timestamp" =~ ^[0-9]+$ ]]; then
        date -d "@$timestamp" "+$format" 2>/dev/null || date -r "$timestamp" "+$format" 2>/dev/null || echo "$timestamp"
    else
        echo "$timestamp"
    fi
}

##
# Generate a random string
#
# @param $1 Length (default: 8)
# @param $2 Character set (default: alphanumeric)
# @return Random string
#
random_string() {
    local length="${1:-8}"
    local charset="${2:-a-zA-Z0-9}"
    
    tr -dc "$charset" < /dev/urandom | head -c "$length"
}

##
# Generate a UUID (version 4)
#
# @return UUID string
#
generate_uuid() {
    if command_exists uuidgen; then
        uuidgen
    elif command_exists python3; then
        python3 -c "import uuid; print(str(uuid.uuid4()))"
    elif command_exists python; then
        python -c "import uuid; print(str(uuid.uuid4()))"
    else
        # Fallback: generate a pseudo-UUID
        printf "%04x%04x-%04x-%04x-%04x-%04x%04x%04x\n" \
            $RANDOM $RANDOM $RANDOM $RANDOM $RANDOM $RANDOM $RANDOM $RANDOM
    fi
}

##
# Check if a string is a valid JSON
#
# @param $1 JSON string
# @return 0 if valid, 1 if invalid
#
is_valid_json() {
    local json="$1"
    
    if command_exists jq; then
        echo "$json" | jq . >/dev/null 2>&1
    elif command_exists python3; then
        python3 -c "import json; json.loads('$json')" >/dev/null 2>&1
    elif command_exists python; then
        python -c "import json; json.loads('$json')" >/dev/null 2>&1
    else
        # Basic JSON validation (very simple)
        [[ "$json" =~ ^[{\[].*[}\]]$ ]]
    fi
}

##
# Parse JSON and extract a value using a simple path
#
# @param $1 JSON string
# @param $2 Path (e.g., "user.name")
# @return Extracted value or empty string
#
json_get() {
    local json="$1"
    local path="$2"
    
    if command_exists jq; then
        echo "$json" | jq -r ".$path" 2>/dev/null || echo ""
    elif command_exists python3; then
        python3 -c "
import json, sys
try:
    data = json.loads('$json')
    keys = '$path'.split('.')
    value = data
    for key in keys:
        value = value[key]
    print(value if value is not None else '')
except:
    print('')
" 2>/dev/null || echo ""
    else
        # Very basic fallback - just return empty
        echo ""
    fi
}

##
# Check if a port is available
#
# @param $1 Port number
# @return 0 if available, 1 if in use
#
port_available() {
    local port="$1"
    
    if command_exists netstat; then
        ! netstat -tuln 2>/dev/null | grep -q ":$port "
    elif command_exists ss; then
        ! ss -tuln 2>/dev/null | grep -q ":$port "
    elif command_exists lsof; then
        ! lsof -i ":$port" >/dev/null 2>&1
    else
        # Fallback: try to bind to the port
        timeout 1 bash -c "echo >/dev/tcp/localhost/$port" 2>/dev/null && return 1 || return 0
    fi
}

##
# Find an available port starting from a given port
#
# @param $1 Starting port (default: 8080)
# @param $2 Maximum attempts (default: 100)
# @return Available port number
#
find_available_port() {
    local start_port="${1:-8080}"
    local max_attempts="${2:-100}"
    local port="$start_port"
    local attempts=0
    
    while [[ $attempts -lt $max_attempts ]]; do
        if port_available "$port"; then
            echo "$port"
            return 0
        fi
        ((port++))
        ((attempts++))
    done
    
    return 1
}

##
# Check if a process is running by name
#
# @param $1 Process name
# @return 0 if running, 1 if not
#
process_running() {
    local process_name="$1"
    
    if command_exists pgrep; then
        pgrep -f "$process_name" >/dev/null 2>&1
    else
        ps aux | grep -v grep | grep -q "$process_name"
    fi
}

##
# Get process ID by name
#
# @param $1 Process name
# @return Process ID or empty string
#
get_process_id() {
    local process_name="$1"
    
    if command_exists pgrep; then
        pgrep -f "$process_name" 2>/dev/null || echo ""
    else
        ps aux | grep -v grep | grep "$process_name" | awk '{print $2}' | head -n1
    fi
}

##
# Kill a process by name
#
# @param $1 Process name
# @param $2 Signal (default: TERM)
# @return 0 if killed, 1 if not found
#
kill_process() {
    local process_name="$1"
    local signal="${2:-TERM}"
    local pid
    
    pid=$(get_process_id "$process_name")
    if [[ -n "$pid" ]]; then
        kill -"$signal" "$pid" 2>/dev/null
        return 0
    fi
    
    return 1
}

##
# Wait for a condition to be true
#
# @param $1 Condition command
# @param $2 Timeout in seconds (default: 30)
# @param $3 Interval in seconds (default: 1)
# @return 0 if condition met, 1 if timeout
#
wait_for() {
    local condition="$1"
    local timeout="${2:-30}"
    local interval="${3:-1}"
    local elapsed=0
    
    while [[ $elapsed -lt $timeout ]]; do
        if eval "$condition"; then
            return 0
        fi
        sleep "$interval"
        elapsed=$((elapsed + interval))
    done
    
    return 1
}

##
# Check if running as root
#
# @return 0 if root, 1 if not
#
is_root() {
    [[ $EUID -eq 0 ]]
}

##
# Check if running in a container
#
# @return 0 if in container, 1 if not
#
is_container() {
    [[ -f /.dockerenv ]] || grep -q 'docker\|lxc' /proc/1/cgroup 2>/dev/null
}

##
# Get system information
#
# @return System info as JSON
#
get_system_info() {
    local os_name
    local os_version
    local arch
    local kernel
    
    os_name=$(uname -s)
    os_version=$(uname -r)
    arch=$(uname -m)
    kernel=$(uname -r)
    
    cat << EOF
{
  "os": "$os_name",
  "version": "$os_version",
  "architecture": "$arch",
  "kernel": "$kernel",
  "hostname": "$(hostname)",
  "user": "$(whoami)",
  "is_root": $(is_root && echo "true" || echo "false"),
  "is_container": $(is_container && echo "true" || echo "false")
}
EOF
}

##
# Get memory usage information
#
# @return Memory info as JSON
#
get_memory_info() {
    if [[ -f /proc/meminfo ]]; then
        local total
        local available
        local used
        
        total=$(grep MemTotal /proc/meminfo | awk '{print $2}')
        available=$(grep MemAvailable /proc/meminfo | awk '{print $2}')
        used=$((total - available))
        
        cat << EOF
{
  "total_kb": $total,
  "available_kb": $available,
  "used_kb": $used,
  "total_mb": $((total / 1024)),
  "available_mb": $((available / 1024)),
  "used_mb": $((used / 1024))
}
EOF
    else
        echo '{"error": "Memory info not available"}'
    fi
}

##
# Get disk usage information
#
# @param $1 Path to check (default: current directory)
# @return Disk info as JSON
#
get_disk_info() {
    local path="${1:-.}"
    
    if command_exists df; then
        local info
        info=$(df -h "$path" | tail -n1)
        local filesystem
        local size
        local used
        local available
        local use_percent
        local mounted
        
        read -r filesystem size used available use_percent mounted <<< "$info"
        
        cat << EOF
{
  "filesystem": "$filesystem",
  "size": "$size",
  "used": "$used",
  "available": "$available",
  "use_percent": "$use_percent",
  "mounted": "$mounted"
}
EOF
    else
        echo '{"error": "Disk info not available"}'
    fi
}

##
# Validate an email address
#
# @param $1 Email address
# @return 0 if valid, 1 if invalid
#
is_valid_email() {
    local email="$1"
    local pattern='^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
    
    [[ "$email" =~ $pattern ]]
}

##
# Validate a URL
#
# @param $1 URL
# @return 0 if valid, 1 if invalid
#
is_valid_url() {
    local url="$1"
    local pattern='^https?://[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}(/.*)?$'
    
    [[ "$url" =~ $pattern ]]
}

##
# Get file size in human-readable format
#
# @param $1 File path
# @return Human-readable size
#
get_file_size() {
    local file="$1"
    
    if [[ -f "$file" ]]; then
        if command_exists numfmt; then
            numfmt --to=iec-i --suffix=B "$(stat -c %s "$file")"
        else
            local size
            size=$(stat -c %s "$file" 2>/dev/null || stat -f %z "$file" 2>/dev/null || echo "0")
            
            if [[ $size -gt 1073741824 ]]; then
                printf "%.1fGB" "$(echo "scale=1; $size / 1073741824" | bc -l)"
            elif [[ $size -gt 1048576 ]]; then
                printf "%.1fMB" "$(echo "scale=1; $size / 1048576" | bc -l)"
            elif [[ $size -gt 1024 ]]; then
                printf "%.1fKB" "$(echo "scale=1; $size / 1024" | bc -l)"
            else
                echo "${size}B"
            fi
        fi
    else
        echo "0B"
    fi
}

##
# Get file modification time
#
# @param $1 File path
# @return Modification timestamp
#
get_file_mtime() {
    local file="$1"
    
    if [[ -f "$file" ]]; then
        stat -c %Y "$file" 2>/dev/null || stat -f %m "$file" 2>/dev/null || echo "0"
    else
        echo "0"
    fi
}

##
# Check if a file has been modified recently
#
# @param $1 File path
# @param $2 Seconds threshold (default: 300)
# @return 0 if modified recently, 1 if not
#
file_modified_recently() {
    local file="$1"
    local threshold="${2:-300}"
    local mtime
    local current_time
    
    mtime=$(get_file_mtime "$file")
    current_time=$(get_timestamp)
    
    [[ $((current_time - mtime)) -lt $threshold ]]
}

##
# Create a backup of a file
#
# @param $1 File path
# @param $2 Backup suffix (default: .bak)
# @return Backup file path
#
create_backup() {
    local file="$1"
    local suffix="${2:-.bak}"
    local backup_file="${file}${suffix}"
    local counter=1
    
    # If backup already exists, create numbered backup
    while [[ -f "$backup_file" ]]; do
        backup_file="${file}${suffix}.${counter}"
        ((counter++))
    done
    
    cp "$file" "$backup_file"
    echo "$backup_file"
}

##
# Restore a file from backup
#
# @param $1 File path
# @param $2 Backup file path (optional, will auto-detect)
# @return 0 if restored, 1 if failed
#
restore_backup() {
    local file="$1"
    local backup_file="$2"
    
    if [[ -z "$backup_file" ]]; then
        # Auto-detect backup file
        if [[ -f "${file}.bak" ]]; then
            backup_file="${file}.bak"
        else
            # Find most recent backup
            backup_file=$(ls -t "${file}.bak."* 2>/dev/null | head -n1)
        fi
    fi
    
    if [[ -f "$backup_file" ]]; then
        cp "$backup_file" "$file"
        return 0
    fi
    
    return 1
} 
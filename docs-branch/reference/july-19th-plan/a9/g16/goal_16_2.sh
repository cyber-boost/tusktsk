#!/bin/bash

# Simplified Queue and Stream Operators
# Global variables
QUEUE_TYPE="memory"
QUEUE_NAME="default"
STREAM_TYPE="file"
STREAM_SOURCE=""

# Initialize Queue operator
queue_init() {
    local queue_type="$1"
    local queue_name="$2"
    QUEUE_TYPE="${queue_type:-memory}"
    QUEUE_NAME="${queue_name:-default}"
    echo "{\"status\":\"success\",\"message\":\"Queue operator initialized\",\"type\":\"$QUEUE_TYPE\",\"name\":\"$QUEUE_NAME\"}"
}

# Enqueue message
queue_enqueue() {
    local message="$1"
    local priority="$2"
    if [[ -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Message is required\"}"
        return 1
    fi
    local queue_file="/tmp/queue_${QUEUE_NAME}_memory"
    echo "$message" >> "$queue_file"
    echo "{\"status\":\"success\",\"message\":\"Message enqueued\",\"queue\":\"$QUEUE_NAME\"}"
}

# Dequeue message
queue_dequeue() {
    local queue_name="${1:-$QUEUE_NAME}"
    local queue_file="/tmp/queue_${queue_name}_memory"
    if [[ ! -f "$queue_file" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"Queue is empty\"}"
        return 1
    fi
    local message=$(head -n 1 "$queue_file" 2>/dev/null)
    if [[ -n "$message" ]]; then
        sed -i '1d' "$queue_file" 2>/dev/null
        echo "{\"status\":\"success\",\"message\":\"Message dequeued\",\"payload\":\"$message\"}"
    else
        echo "{\"status\":\"warning\",\"message\":\"Queue is empty\"}"
    fi
}

# Queue size
queue_size() {
    local queue_name="${1:-$QUEUE_NAME}"
    local queue_file="/tmp/queue_${queue_name}_memory"
    local size=0
    if [[ -f "$queue_file" ]]; then
        size=$(wc -l < "$queue_file")
    fi
    echo "{\"status\":\"success\",\"message\":\"Queue size retrieved\",\"size\":$size}"
}

# Clear queue
queue_clear() {
    local queue_name="${1:-$QUEUE_NAME}"
    local queue_file="/tmp/queue_${queue_name}_memory"
    rm -f "$queue_file"
    echo "{\"status\":\"success\",\"message\":\"Queue cleared\"}"
}

# Queue config
queue_config() {
    echo "{\"status\":\"success\",\"config\":{\"type\":\"$QUEUE_TYPE\",\"name\":\"$QUEUE_NAME\"}}"
}

# Initialize Stream operator
stream_init() {
    local stream_type="$1"
    local source="$2"
    STREAM_TYPE="${stream_type:-file}"
    STREAM_SOURCE="$source"
    echo "{\"status\":\"success\",\"message\":\"Stream operator initialized\",\"type\":\"$STREAM_TYPE\",\"source\":\"$STREAM_SOURCE\"}"
}

# Start stream
stream_start() {
    local processor="$1"
    local output="$2"
    if [[ -z "$STREAM_SOURCE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Stream source is required\"}"
        return 1
    fi
    if [[ "$STREAM_TYPE" == "file" && -f "$STREAM_SOURCE" ]]; then
        tail -f "$STREAM_SOURCE" &
        local pid=$!
        echo "$pid" > "/tmp/stream_${STREAM_SOURCE##*/}.pid"
        echo "{\"status\":\"success\",\"message\":\"Stream started\",\"pid\":$pid}"
    else
        echo "{\"status\":\"error\",\"message\":\"Stream source not found or unsupported type\"}"
        return 1
    fi
}

# Stop stream
stream_stop() {
    local pid_files=$(find /tmp -name "stream_*.pid" 2>/dev/null)
    local stopped=0
    for pid_file in $pid_files; do
        if [[ -f "$pid_file" ]]; then
            local pid=$(cat "$pid_file")
            if kill -0 "$pid" 2>/dev/null; then
                kill "$pid" 2>/dev/null
                ((stopped++))
            fi
            rm -f "$pid_file"
        fi
    done
    echo "{\"status\":\"success\",\"message\":\"Streams stopped\",\"count\":$stopped}"
}

# Stream status
stream_status() {
    local active=0
    local pid_files=$(find /tmp -name "stream_*.pid" 2>/dev/null)
    for pid_file in $pid_files; do
        if [[ -f "$pid_file" ]]; then
            local pid=$(cat "$pid_file")
            if kill -0 "$pid" 2>/dev/null; then
                ((active++))
            fi
        fi
    done
    echo "{\"status\":\"success\",\"message\":\"Stream status\",\"active_streams\":$active}"
}

# Stream config
stream_config() {
    echo "{\"status\":\"success\",\"config\":{\"type\":\"$STREAM_TYPE\",\"source\":\"$STREAM_SOURCE\"}}"
}

# Main Queue operator
execute_queue() {
    local action="$1"
    local params="$2"
    case "$action" in
        "init")
            local type=$(echo "$params" | grep -o 'type=[^,]*' | cut -d'=' -f2)
            local name=$(echo "$params" | grep -o 'name=[^,]*' | cut -d'=' -f2)
            queue_init "$type" "$name"
            ;;
        "enqueue")
            local message=$(echo "$params" | grep -o 'message=[^,]*' | cut -d'=' -f2-)
            local priority=$(echo "$params" | grep -o 'priority=[^,]*' | cut -d'=' -f2)
            queue_enqueue "$message" "$priority"
            ;;
        "dequeue")
            local queue=$(echo "$params" | grep -o 'queue=[^,]*' | cut -d'=' -f2)
            queue_dequeue "$queue"
            ;;
        "size")
            local queue=$(echo "$params" | grep -o 'queue=[^,]*' | cut -d'=' -f2)
            queue_size "$queue"
            ;;
        "clear")
            local queue=$(echo "$params" | grep -o 'queue=[^,]*' | cut -d'=' -f2)
            queue_clear "$queue"
            ;;
        "config")
            queue_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action\"}"
            return 1
            ;;
    esac
}

# Main Stream operator
execute_stream() {
    local action="$1"
    local params="$2"
    case "$action" in
        "init")
            local type=$(echo "$params" | grep -o 'type=[^,]*' | cut -d'=' -f2)
            local source=$(echo "$params" | grep -o 'source=[^,]*' | cut -d'=' -f2)
            stream_init "$type" "$source"
            ;;
        "start")
            local processor=$(echo "$params" | grep -o 'processor=[^,]*' | cut -d'=' -f2)
            local output=$(echo "$params" | grep -o 'output=[^,]*' | cut -d'=' -f2)
            stream_start "$processor" "$output"
            ;;
        "stop")
            stream_stop
            ;;
        "status")
            stream_status
            ;;
        "config")
            stream_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action\"}"
            return 1
            ;;
    esac
}

# Export functions
export -f execute_queue execute_stream queue_init queue_enqueue queue_dequeue queue_size queue_clear stream_init stream_start stream_stop stream_status 
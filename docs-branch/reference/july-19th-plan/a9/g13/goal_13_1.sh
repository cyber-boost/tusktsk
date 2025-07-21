#!/bin/bash

# DNS and Proxy Operators Implementation
# Provides DNS resolution and proxy management

# Global variables for DNS
DNS_SERVER="8.8.8.8"
DNS_TIMEOUT="5"
DNS_RETRIES="3"
DNS_CACHE_ENABLED="true"
DNS_CACHE_PATH="/tmp/dns_cache"

# Global variables for Proxy
PROXY_HOST=""
PROXY_PORT=""
PROXY_USERNAME=""
PROXY_PASSWORD=""
PROXY_TYPE="http"
PROXY_ENABLED="false"

# Initialize DNS operator
dns_init() {
    local server="$1"
    local timeout="$2"
    local retries="$3"
    local cache_enabled="$4"
    local cache_path="$5"
    
    DNS_SERVER="${server:-8.8.8.8}"
    DNS_TIMEOUT="${timeout:-5}"
    DNS_RETRIES="${retries:-3}"
    DNS_CACHE_ENABLED="${cache_enabled:-true}"
    DNS_CACHE_PATH="${cache_path:-/tmp/dns_cache}"
    
    # Create cache directory if enabled
    if [[ "$DNS_CACHE_ENABLED" == "true" ]]; then
        mkdir -p "$DNS_CACHE_PATH"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"DNS operator initialized\",\"server\":\"$DNS_SERVER\",\"timeout\":\"$DNS_TIMEOUT\",\"cache_enabled\":\"$DNS_CACHE_ENABLED\"}"
}

# DNS resolve
dns_resolve() {
    local hostname="$1"
    local record_type="$2"
    
    if [[ -z "$hostname" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Hostname is required\"}"
        return 1
    fi
    
    record_type="${record_type:-A}"
    
    # Check cache if enabled
    if [[ "$DNS_CACHE_ENABLED" == "true" ]]; then
        local cache_file="$DNS_CACHE_PATH/${hostname}_${record_type}.cache"
        if [[ -f "$cache_file" ]]; then
            local cache_timestamp=$(stat -c %Y "$cache_file")
            local current_time=$(date +%s)
            if [[ $((current_time - cache_timestamp)) -lt 3600 ]]; then  # 1 hour cache
                local cached_result=$(cat "$cache_file")
                echo "{\"status\":\"success\",\"message\":\"Resolved from cache\",\"hostname\":\"$hostname\",\"record_type\":\"$record_type\",\"addresses\":$cached_result,\"cached\":true}"
                return 0
            fi
        fi
    fi
    
    local addresses="[]"
    local attempt=0
    local success=false
    
    while [[ $attempt -lt $DNS_RETRIES ]]; do
        local result=$(dig +short +timeout=$DNS_TIMEOUT @$DNS_SERVER $hostname $record_type 2>/dev/null)
        if [[ -n "$result" ]]; then
            addresses="[$(echo "$result" | awk '{printf "\"%s\",", $0}' | sed 's/,$//')]"
            success=true
            break
        fi
        ((attempt++))
        sleep 1
    done
    
    if [[ "$success" == true ]]; then
        # Cache the result if enabled
        if [[ "$DNS_CACHE_ENABLED" == "true" ]]; then
            local cache_file="$DNS_CACHE_PATH/${hostname}_${record_type}.cache"
            echo "$addresses" > "$cache_file"
        fi
        echo "{\"status\":\"success\",\"message\":\"DNS resolved\",\"hostname\":\"$hostname\",\"record_type\":\"$record_type\",\"addresses\":$addresses,\"cached\":false}"
    else
        echo "{\"status\":\"error\",\"message\":\"DNS resolution failed after $DNS_RETRIES attempts\",\"hostname\":\"$hostname\",\"record_type\":\"$record_type\"}"
        return 1
    fi
}

# DNS flush cache
dns_flush_cache() {
    if [[ "$DNS_CACHE_ENABLED" != "true" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"DNS cache is not enabled\"}"
        return 0
    fi
    
    local flushed_count=0
    if [[ -d "$DNS_CACHE_PATH" ]]; then
        local files=$(find "$DNS_CACHE_PATH" -type f -name '*.cache' | wc -l)
        rm -f "$DNS_CACHE_PATH"/*.cache
        flushed_count=$files
    fi
    
    echo "{\"status\":\"success\",\"message\":\"DNS cache flushed\",\"flushed_entries\":$flushed_count}"
}

# Initialize Proxy operator
proxy_init() {
    local host="$1"
    local port="$2"
    local username="$3"
    local password="$4"
    local type="$5"
    local enabled="$6"
    
    PROXY_HOST="$host"
    PROXY_PORT="$port"
    PROXY_USERNAME="$username"
    PROXY_PASSWORD="$password"
    PROXY_TYPE="${type:-http}"
    PROXY_ENABLED="${enabled:-false}"
    
    if [[ -n "$host" && -z "$port" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Proxy port is required when host is specified\"}"
        return 1
    fi
    
    # Set environment variables if enabled
    if [[ "$PROXY_ENABLED" == "true" ]]; then
        proxy_set_env
    else
        proxy_unset_env
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Proxy operator initialized\",\"host\":\"$PROXY_HOST\",\"port\":\"$PROXY_PORT\",\"type\":\"$PROXY_TYPE\",\"enabled\":\"$PROXY_ENABLED\"}"
}

# Set proxy environment variables
proxy_set_env() {
    if [[ -z "$PROXY_HOST" || -z "$PROXY_PORT" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Proxy host and port must be initialized\"}"
        return 1
    fi
    
    local proxy_url="${PROXY_TYPE}://"
    if [[ -n "$PROXY_USERNAME" ]]; then
        proxy_url="${proxy_url}${PROXY_USERNAME}"
        if [[ -n "$PROXY_PASSWORD" ]]; then
            proxy_url="${proxy_url}:${PROXY_PASSWORD}"
        fi
        proxy_url="${proxy_url}@"
    fi
    proxy_url="${proxy_url}${PROXY_HOST}:${PROXY_PORT}"
    
    export http_proxy="$proxy_url"
    export https_proxy="$proxy_url"
    export HTTP_PROXY="$proxy_url"
    export HTTPS_PROXY="$proxy_url"
    
    echo "{\"status\":\"success\",\"message\":\"Proxy environment variables set\"}"
}

# Unset proxy environment variables
proxy_unset_env() {
    unset http_proxy https_proxy HTTP_PROXY HTTPS_PROXY
    echo "{\"status\":\"success\",\"message\":\"Proxy environment variables unset\"}"
}

# Test proxy connection
proxy_test() {
    local test_url="$1"
    
    test_url="${test_url:-http://www.google.com}"
    
    if [[ "$PROXY_ENABLED" != "true" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"Proxy is not enabled\"}"
        return 0
    fi
    
    local response=$(curl -s -o /dev/null -w "%{http_code}" --max-time $DNS_TIMEOUT --proxy "$http_proxy" "$test_url" 2>/dev/null)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 && $response -eq 200 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Proxy connection test successful\",\"test_url\":\"$test_url\",\"response_code\":$response}"
    else
        echo "{\"status\":\"error\",\"message\":\"Proxy connection test failed\",\"test_url\":\"$test_url\",\"error_code\":$exit_code,\"response_code\":$response}"
        return 1
    fi
}

# Get DNS configuration
dns_config() {
    echo "{\"status\":\"success\",\"config\":{\"server\":\"$DNS_SERVER\",\"timeout\":\"$DNS_TIMEOUT\",\"retries\":\"$DNS_RETRIES\",\"cache_enabled\":\"$DNS_CACHE_ENABLED\",\"cache_path\":\"$DNS_CACHE_PATH\"}}"
}

# Get Proxy configuration
proxy_config() {
    echo "{\"status\":\"success\",\"config\":{\"host\":\"$PROXY_HOST\",\"port\":\"$PROXY_PORT\",\"type\":\"$PROXY_TYPE\",\"enabled\":\"$PROXY_ENABLED\",\"username_set\":\"$([ -n "$PROXY_USERNAME" ] && echo true || echo false)\"}}"
}

# Main DNS operator function
execute_dns() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local server=$(echo "$params" | grep -o 'server=[^,]*' | cut -d'=' -f2)
            local timeout=$(echo "$params" | grep -o 'timeout=[^,]*' | cut -d'=' -f2)
            local retries=$(echo "$params" | grep -o 'retries=[^,]*' | cut -d'=' -f2)
            local cache_enabled=$(echo "$params" | grep -o 'cache_enabled=[^,]*' | cut -d'=' -f2)
            local cache_path=$(echo "$params" | grep -o 'cache_path=[^,]*' | cut -d'=' -f2)
            dns_init "$server" "$timeout" "$retries" "$cache_enabled" "$cache_path"
            ;;
        "resolve")
            local hostname=$(echo "$params" | grep -o 'hostname=[^,]*' | cut -d'=' -f2)
            local record_type=$(echo "$params" | grep -o 'record_type=[^,]*' | cut -d'=' -f2)
            dns_resolve "$hostname" "$record_type"
            ;;
        "flush_cache")
            dns_flush_cache
            ;;
        "config")
            dns_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, resolve, flush_cache, config\"}"
            return 1
            ;;
    esac
}

# Main Proxy operator function
execute_proxy() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local host=$(echo "$params" | grep -o 'host=[^,]*' | cut -d'=' -f2)
            local port=$(echo "$params" | grep -o 'port=[^,]*' | cut -d'=' -f2)
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local password=$(echo "$params" | grep -o 'password=[^,]*' | cut -d'=' -f2)
            local type=$(echo "$params" | grep -o 'type=[^,]*' | cut -d'=' -f2)
            local enabled=$(echo "$params" | grep -o 'enabled=[^,]*' | cut -d'=' -f2)
            proxy_init "$host" "$port" "$username" "$password" "$type" "$enabled"
            ;;
        "enable")
            PROXY_ENABLED="true"
            proxy_set_env
            ;;
        "disable")
            PROXY_ENABLED="false"
            proxy_unset_env
            ;;
        "test")
            local test_url=$(echo "$params" | grep -o 'test_url=[^,]*' | cut -d'=' -f2)
            proxy_test "$test_url"
            ;;
        "config")
            proxy_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, enable, disable, test, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_dns execute_proxy dns_init dns_resolve dns_flush_cache proxy_init proxy_set_env proxy_unset_env proxy_test dns_config proxy_config 
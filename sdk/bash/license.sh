#!/bin/bash

# TuskLang SDK License Validation Module
# Enterprise-grade license validation for Bash SDK

# Global variables
declare -g TUSK_LICENSE_KEY=""
declare -g TUSK_API_KEY=""
declare -g TUSK_SESSION_ID=""
declare -g TUSK_CACHE_DIR=""
declare -g TUSK_CACHE_FILE=""
declare -g TUSK_OFFLINE_CACHE=""

# Initialize license system
tusk_license_init() {
    local license_key="$1"
    local api_key="$2"
    local cache_dir="${3:-}"
    
    TUSK_LICENSE_KEY="$license_key"
    TUSK_API_KEY="$api_key"
    TUSK_SESSION_ID=$(uuidgen 2>/dev/null || cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "$(date +%s)-$$-$RANDOM")
    
    # Set up cache directory
    if [[ -n "$cache_dir" ]]; then
        TUSK_CACHE_DIR="$cache_dir"
    else
        TUSK_CACHE_DIR="${HOME}/.tusk/license_cache"
    fi
    
    mkdir -p "$TUSK_CACHE_DIR"
    
    # Generate cache file name based on license key hash
    local key_hash=$(echo -n "$license_key" | md5sum | cut -d' ' -f1)
    TUSK_CACHE_FILE="${TUSK_CACHE_DIR}/${key_hash}.cache"
    
    # Load offline cache if exists
    _load_offline_cache
}

# Validate license key format
tusk_validate_license_key() {
    local result=""
    
    if [[ -z "$TUSK_LICENSE_KEY" ]] || [[ ${#TUSK_LICENSE_KEY} -lt 32 ]]; then
        echo '{"valid":false,"error":"Invalid license key format"}'
        return 1
    fi
    
    if [[ ! "$TUSK_LICENSE_KEY" =~ ^TUSK- ]]; then
        echo '{"valid":false,"error":"Invalid license key prefix"}'
        return 1
    fi
    
    local checksum=$(echo -n "$TUSK_LICENSE_KEY" | sha256sum | cut -d' ' -f1)
    if [[ ! "$checksum" =~ ^tusk ]]; then
        echo '{"valid":false,"error":"Invalid license key checksum"}'
        return 1
    fi
    
    echo "{\"valid\":true,\"checksum\":\"$checksum\"}"
    return 0
}

# Verify license with server
tusk_verify_license_server() {
    local server_url="${1:-https://api.tusklang.org/v1/license}"
    local timestamp=$(date +%s)
    
    # Prepare data
    local data="{\"license_key\":\"$TUSK_LICENSE_KEY\",\"session_id\":\"$TUSK_SESSION_ID\",\"timestamp\":$timestamp}"
    
    # Generate signature
    local signature=$(echo -n "$data" | openssl dgst -sha256 -hmac "$TUSK_API_KEY" -binary | xxd -p -c 256)
    data=$(echo "$data" | jq -c ". + {signature: \"$signature\"}")
    
    # Make HTTP request with timeout
    local response=$(timeout 10 curl -s -w "\n%{http_code}" \
        -X POST "$server_url" \
        -H "Authorization: Bearer $TUSK_API_KEY" \
        -H "Content-Type: application/json" \
        -H "User-Agent: TuskLang-Bash-SDK/1.0.0" \
        -d "$data" 2>/dev/null)
    
    local curl_exit_code=$?
    
    # Extract body and status code
    local body=$(echo "$response" | sed '$d')
    local http_code=$(echo "$response" | tail -n1)
    
    # Check for timeout or network errors
    if [[ $curl_exit_code -eq 124 ]]; then
        echo "[TuskLicense] Request timeout" >&2
        _fallback_to_offline_cache "Request timeout"
        return
    elif [[ $curl_exit_code -ne 0 ]] || [[ -z "$http_code" ]]; then
        echo "[TuskLicense] Network error during license validation" >&2
        _fallback_to_offline_cache "Network error"
        return
    fi
    
    # Check HTTP status
    if [[ "$http_code" == "200" ]]; then
        # Validate JSON response
        if echo "$body" | jq -e . >/dev/null 2>&1; then
            # Update cache
            local cache_entry=$(jq -n \
                --argjson data "$body" \
                --arg timestamp "$timestamp" \
                --arg expires "$((timestamp + 3600))" \
                '{data: $data, timestamp: $timestamp, expires: $expires}')
            
            # Save to offline cache
            _save_offline_cache "$body"
            
            echo "$body"
        else
            echo "[TuskLicense] Invalid JSON response from server" >&2
            _fallback_to_offline_cache "Invalid response format"
        fi
    else
        echo "[TuskLicense] Server returned error: $http_code" >&2
        _fallback_to_offline_cache "Server error: $http_code"
    fi
}

# Check license expiration
tusk_check_license_expiration() {
    local parts=(${TUSK_LICENSE_KEY//-/ })
    if [[ ${#parts[@]} -lt 4 ]]; then
        echo '{"expired":true,"error":"Invalid license key format"}'
        return 1
    fi
    
    local expiration_str="${parts[-1]}"
    local expiration_timestamp=$((16#$expiration_str))
    local current_timestamp=$(date +%s)
    
    if [[ $expiration_timestamp -lt $current_timestamp ]]; then
        local days_overdue=$(( (current_timestamp - expiration_timestamp) / 86400 ))
        local expiration_date=$(date -d "@$expiration_timestamp" -Iseconds)
        echo "{\"expired\":true,\"expiration_date\":\"$expiration_date\",\"days_overdue\":$days_overdue}"
        return 1
    fi
    
    local days_remaining=$(( (expiration_timestamp - current_timestamp) / 86400 ))
    local expiration_date=$(date -d "@$expiration_timestamp" -Iseconds)
    local warning=false
    
    if [[ $days_remaining -le 30 ]]; then
        warning=true
    fi
    
    echo "{\"expired\":false,\"expiration_date\":\"$expiration_date\",\"days_remaining\":$days_remaining,\"warning\":$warning}"
    return 0
}

# Validate license permissions for a feature
tusk_validate_license_permissions() {
    local feature="$1"
    
    # For basic features, always allow
    case "$feature" in
        basic|core|standard)
            echo "{\"allowed\":true,\"feature\":\"$feature\"}"
            return 0
            ;;
        premium|enterprise)
            if [[ "$TUSK_LICENSE_KEY" =~ PREMIUM ]] || [[ "$TUSK_LICENSE_KEY" =~ ENTERPRISE ]]; then
                echo "{\"allowed\":true,\"feature\":\"$feature\"}"
                return 0
            else
                echo "{\"allowed\":false,\"feature\":\"$feature\",\"error\":\"Premium license required\"}"
                return 1
            fi
            ;;
        *)
            echo "{\"allowed\":false,\"feature\":\"$feature\",\"error\":\"Unknown feature\"}"
            return 1
            ;;
    esac
}

# Load offline cache
_load_offline_cache() {
    if [[ -f "$TUSK_CACHE_FILE" ]]; then
        local cache_content=$(cat "$TUSK_CACHE_FILE" 2>/dev/null)
        if [[ -n "$cache_content" ]]; then
            # Verify cache is valid JSON
            if echo "$cache_content" | jq -e . >/dev/null 2>&1; then
                # Verify the cache is for the correct license key
                local key_hash=$(echo -n "$TUSK_LICENSE_KEY" | sha256sum | cut -d' ' -f1)
                local cached_hash=$(echo "$cache_content" | jq -r '.license_key_hash')
                
                if [[ "$cached_hash" == "$key_hash" ]]; then
                    TUSK_OFFLINE_CACHE="$cache_content"
                    echo "[TuskLicense] Loaded offline license cache" >&2
                else
                    echo "[TuskLicense] Offline cache key mismatch" >&2
                    TUSK_OFFLINE_CACHE=""
                fi
            else
                echo "[TuskLicense] Invalid offline cache format" >&2
                TUSK_OFFLINE_CACHE=""
            fi
        fi
    fi
}

# Save offline cache
_save_offline_cache() {
    local license_data="$1"
    local key_hash=$(echo -n "$TUSK_LICENSE_KEY" | sha256sum | cut -d' ' -f1)
    local timestamp=$(date +%s)
    local expiration=$(tusk_check_license_expiration)
    
    local cache_data=$(jq -n \
        --arg hash "$key_hash" \
        --argjson data "$license_data" \
        --arg timestamp "$timestamp" \
        --argjson expiration "$expiration" \
        '{license_key_hash: $hash, license_data: $data, timestamp: $timestamp, expiration: $expiration}')
    
    echo "$cache_data" > "$TUSK_CACHE_FILE"
    TUSK_OFFLINE_CACHE="$cache_data"
    echo "[TuskLicense] Saved license data to offline cache" >&2
}

# Fallback to offline cache
_fallback_to_offline_cache() {
    local error_msg="$1"
    
    if [[ -n "$TUSK_OFFLINE_CACHE" ]]; then
        local timestamp=$(echo "$TUSK_OFFLINE_CACHE" | jq -r '.timestamp')
        local cache_age=$(( $(date +%s) - timestamp ))
        local cache_age_days=$(awk "BEGIN {printf \"%.1f\", $cache_age/86400}")
        
        # Check if cached license is not expired
        local expired=$(echo "$TUSK_OFFLINE_CACHE" | jq -r '.expiration.expired')
        
        if [[ "$expired" != "true" ]]; then
            echo "[TuskLicense] Using offline license cache (age: $cache_age_days days)" >&2
            
            # Return license data with offline mode info
            echo "$TUSK_OFFLINE_CACHE" | jq -c \
                --arg mode "true" \
                --arg days "$cache_age_days" \
                --arg warning "Operating in offline mode due to: $error_msg" \
                '.license_data + {offline_mode: $mode, cache_age_days: $days, warning: $warning}'
        else
            echo "{\"valid\":false,\"error\":\"License expired and server unreachable: $error_msg\",\"offline_cache_expired\":true}"
        fi
    else
        echo "{\"valid\":false,\"error\":\"No offline cache available: $error_msg\",\"offline_cache_missing\":true}"
    fi
}

# Get license info
tusk_get_license_info() {
    local validation=$(tusk_validate_license_key)
    local expiration=$(tusk_check_license_expiration)
    local masked_key="${TUSK_LICENSE_KEY:0:8}...${TUSK_LICENSE_KEY: -4}"
    
    jq -n \
        --arg key "$masked_key" \
        --arg session "$TUSK_SESSION_ID" \
        --argjson validation "$validation" \
        --argjson expiration "$expiration" \
        '{license_key: $key, session_id: $session, validation: $validation, expiration: $expiration}'
}

# Export functions for use in other scripts
export -f tusk_license_init
export -f tusk_validate_license_key
export -f tusk_verify_license_server
export -f tusk_check_license_expiration
export -f tusk_validate_license_permissions
export -f tusk_get_license_info
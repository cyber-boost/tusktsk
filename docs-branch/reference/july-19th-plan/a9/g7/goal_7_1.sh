#!/bin/bash

# JWT Operator Implementation
# Provides JWT token generation, validation, decoding, and management

# Global variables
JWT_SECRET_KEY=""
JWT_ALGORITHM="HS256"
JWT_EXPIRY="3600"  # 1 hour default
JWT_ISSUER="tusk-agent-a9"
JWT_AUDIENCE="tusk-api"

# Initialize JWT operator
jwt_init() {
    local secret_key="$1"
    local algorithm="$2"
    local expiry="$3"
    
    if [[ -n "$secret_key" ]]; then
        JWT_SECRET_KEY="$secret_key"
    else
        # Generate a random secret if none provided
        JWT_SECRET_KEY=$(openssl rand -base64 32 2>/dev/null || echo "default-secret-key-$(date +%s)")
    fi
    
    if [[ -n "$algorithm" ]]; then
        JWT_ALGORITHM="$algorithm"
    fi
    
    if [[ -n "$expiry" ]]; then
        JWT_EXPIRY="$expiry"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"JWT operator initialized\",\"algorithm\":\"$JWT_ALGORITHM\",\"expiry\":\"$JWT_EXPIRY\"}"
}

# Generate JWT token
jwt_generate() {
    local payload="$1"
    local custom_expiry="$2"
    
    if [[ -z "$payload" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Payload is required\"}"
        return 1
    fi
    
    local expiry_time=$JWT_EXPIRY
    if [[ -n "$custom_expiry" ]]; then
        expiry_time="$custom_expiry"
    fi
    
    local current_time=$(date +%s)
    local expiry_timestamp=$((current_time + expiry_time))
    
    # Create JWT payload
    local jwt_payload=$(cat <<EOF
{
  "iss": "$JWT_ISSUER",
  "aud": "$JWT_AUDIENCE",
  "iat": $current_time,
  "exp": $expiry_timestamp,
  "data": $payload
}
EOF
)
    
    # Base64 encode header
    local header=$(echo -n "{\"alg\":\"$JWT_ALGORITHM\",\"typ\":\"JWT\"}" | base64 -w 0 | tr -d '=' | tr '/+' '_-')
    
    # Base64 encode payload
    local encoded_payload=$(echo -n "$jwt_payload" | base64 -w 0 | tr -d '=' | tr '/+' '_-')
    
    # Create signature
    local signature=""
    if [[ "$JWT_ALGORITHM" == "HS256" ]]; then
        signature=$(echo -n "$header.$encoded_payload" | openssl dgst -sha256 -hmac "$JWT_SECRET_KEY" -binary | base64 -w 0 | tr -d '=' | tr '/+' '_-')
    elif [[ "$JWT_ALGORITHM" == "HS512" ]]; then
        signature=$(echo -n "$header.$encoded_payload" | openssl dgst -sha512 -hmac "$JWT_SECRET_KEY" -binary | base64 -w 0 | tr -d '=' | tr '/+' '_-')
    else
        echo "{\"status\":\"error\",\"message\":\"Unsupported algorithm: $JWT_ALGORITHM\"}"
        return 1
    fi
    
    local token="$header.$encoded_payload.$signature"
    echo "{\"status\":\"success\",\"token\":\"$token\",\"expires_at\":$expiry_timestamp}"
}

# Decode JWT token (without verification)
jwt_decode() {
    local token="$1"
    
    if [[ -z "$token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token is required\"}"
        return 1
    fi
    
    # Split token into parts
    local parts=(${token//./ })
    if [[ ${#parts[@]} -ne 3 ]]; then
        echo "{\"status\":\"error\",\"message\":\"Invalid JWT format\"}"
        return 1
    fi
    
    local header="${parts[0]}"
    local payload="${parts[1]}"
    local signature="${parts[2]}"
    
    # Decode header
    local decoded_header=$(echo "$header" | tr '_-' '/+' | base64 -d 2>/dev/null)
    if [[ $? -ne 0 ]]; then
        echo "{\"status\":\"error\",\"message\":\"Invalid header encoding\"}"
        return 1
    fi
    
    # Decode payload
    local decoded_payload=$(echo "$payload" | tr '_-' '/+' | base64 -d 2>/dev/null)
    if [[ $? -ne 0 ]]; then
        echo "{\"status\":\"error\",\"message\":\"Invalid payload encoding\"}"
        return 1
    fi
    
    echo "{\"status\":\"success\",\"header\":$decoded_header,\"payload\":$decoded_payload,\"signature\":\"$signature\"}"
}

# Verify JWT token
jwt_verify() {
    local token="$1"
    
    if [[ -z "$token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token is required\"}"
        return 1
    fi
    
    # First decode the token
    local decoded_result=$(jwt_decode "$token")
    if [[ $? -ne 0 ]]; then
        echo "$decoded_result"
        return 1
    fi
    
    # Extract expiration using a simpler approach
    local exp=$(echo "$decoded_result" | tr '\n' ' ' | grep -o '"exp":[[:space:]]*[0-9]*' | sed 's/"exp":[[:space:]]*//')
    local current_time=$(date +%s)
    
    if [[ -z "$exp" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token has no expiration claim\"}"
        return 1
    fi
    
    if [[ $current_time -gt $exp ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token has expired\",\"expired_at\":$exp,\"current_time\":$current_time}"
        return 1
    fi
    
    # Verify signature
    local parts=(${token//./ })
    local header="${parts[0]}"
    local payload_part="${parts[1]}"
    local signature="${parts[2]}"
    
    local expected_signature=""
    if [[ "$JWT_ALGORITHM" == "HS256" ]]; then
        expected_signature=$(echo -n "$header.$payload_part" | openssl dgst -sha256 -hmac "$JWT_SECRET_KEY" -binary | base64 -w 0 | tr -d '=' | tr '/+' '_-')
    elif [[ "$JWT_ALGORITHM" == "HS512" ]]; then
        expected_signature=$(echo -n "$header.$payload_part" | openssl dgst -sha512 -hmac "$JWT_SECRET_KEY" -binary | base64 -w 0 | tr -d '=' | tr '/+' '_-')
    fi
    
    if [[ "$signature" != "$expected_signature" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Invalid signature\"}"
        return 1
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Token is valid\"}"
}

# Refresh JWT token
jwt_refresh() {
    local token="$1"
    local new_expiry="$2"
    
    if [[ -z "$token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token is required\"}"
        return 1
    fi
    
    # First decode the token
    local decoded_result=$(jwt_decode "$token")
    if [[ $? -ne 0 ]]; then
        echo "$decoded_result"
        return 1
    fi
    
    # Extract payload data from the decoded result
    local payload_json=$(echo "$decoded_result" | sed 's/.*"payload"://' | sed 's/,"signature".*//')
    local data_field=$(echo "$payload_json" | sed 's/.*"data"://' | sed 's/,"[^"]*":[^,}]*//g')
    
    if [[ -z "$data_field" ]]; then
        echo "{\"status\":\"error\",\"message\":\"No data payload found in token\"}"
        return 1
    fi
    
    # Generate new token with same data
    jwt_generate "$data_field" "$new_expiry"
}

# Main JWT operator function
execute_jwt() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local secret_key=$(echo "$params" | grep -o 'secret_key=[^,]*' | cut -d'=' -f2)
            local algorithm=$(echo "$params" | grep -o 'algorithm=[^,]*' | cut -d'=' -f2)
            local expiry=$(echo "$params" | grep -o 'expiry=[^,]*' | cut -d'=' -f2)
            jwt_init "$secret_key" "$algorithm" "$expiry"
            ;;
        "generate")
            local payload=$(echo "$params" | grep -o 'payload=[^,]*' | cut -d'=' -f2)
            local expiry=$(echo "$params" | grep -o 'expiry=[^,]*' | cut -d'=' -f2)
            jwt_generate "$payload" "$expiry"
            ;;
        "decode")
            local token=$(echo "$params" | grep -o 'token=[^,]*' | cut -d'=' -f2)
            jwt_decode "$token"
            ;;
        "verify")
            local token=$(echo "$params" | grep -o 'token=[^,]*' | cut -d'=' -f2)
            jwt_verify "$token"
            ;;
        "refresh")
            local token=$(echo "$params" | grep -o 'token=[^,]*' | cut -d'=' -f2)
            local expiry=$(echo "$params" | grep -o 'expiry=[^,]*' | cut -d'=' -f2)
            jwt_refresh "$token" "$expiry"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, generate, decode, verify, refresh\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_jwt jwt_init jwt_generate jwt_decode jwt_verify jwt_refresh 
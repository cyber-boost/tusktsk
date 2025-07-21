#!/bin/bash

# OAuth 2.0 Operator Implementation
# Provides OAuth 2.0 authentication flows and token management

# Global variables
OAUTH_CLIENT_ID=""
OAUTH_CLIENT_SECRET=""
OAUTH_REDIRECT_URI=""
OAUTH_AUTH_URL=""
OAUTH_TOKEN_URL=""
OAUTH_SCOPE=""
OAUTH_STATE=""
OAUTH_ACCESS_TOKEN=""
OAUTH_REFRESH_TOKEN=""
OAUTH_TOKEN_TYPE=""
OAUTH_EXPIRES_IN=""

# Initialize OAuth operator
oauth_init() {
    local client_id="$1"
    local client_secret="$2"
    local redirect_uri="$3"
    local auth_url="$4"
    local token_url="$5"
    local scope="$6"
    
    if [[ -z "$client_id" || -z "$client_secret" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Client ID and Client Secret are required\"}"
        return 1
    fi
    
    OAUTH_CLIENT_ID="$client_id"
    OAUTH_CLIENT_SECRET="$client_secret"
    OAUTH_REDIRECT_URI="$redirect_uri"
    OAUTH_AUTH_URL="$auth_url"
    OAUTH_TOKEN_URL="$token_url"
    OAUTH_SCOPE="$scope"
    
    # Generate state parameter for CSRF protection
    OAUTH_STATE=$(openssl rand -hex 16 2>/dev/null || echo "state-$(date +%s)")
    
    echo "{\"status\":\"success\",\"message\":\"OAuth operator initialized\",\"client_id\":\"$client_id\",\"redirect_uri\":\"$redirect_uri\"}"
}

# Generate authorization URL for authorization_code flow
oauth_auth_url() {
    if [[ -z "$OAUTH_CLIENT_ID" || -z "$OAUTH_AUTH_URL" ]]; then
        echo "{\"status\":\"error\",\"message\":\"OAuth not initialized or auth URL not set\"}"
        return 1
    fi
    
    local auth_url="$OAUTH_AUTH_URL"
    auth_url="${auth_url}?client_id=${OAUTH_CLIENT_ID}"
    auth_url="${auth_url}&response_type=code"
    auth_url="${auth_url}&redirect_uri=${OAUTH_REDIRECT_URI}"
    auth_url="${auth_url}&state=${OAUTH_STATE}"
    
    if [[ -n "$OAUTH_SCOPE" ]]; then
        auth_url="${auth_url}&scope=${OAUTH_SCOPE}"
    fi
    
    echo "{\"status\":\"success\",\"auth_url\":\"$auth_url\",\"state\":\"$OAUTH_STATE\"}"
}

# Exchange authorization code for access token
oauth_exchange_code() {
    local auth_code="$1"
    local state="$2"
    
    if [[ -z "$auth_code" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Authorization code is required\"}"
        return 1
    fi
    
    if [[ -z "$OAUTH_TOKEN_URL" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token URL not configured\"}"
        return 1
    fi
    
    # Verify state parameter
    if [[ -n "$state" && "$state" != "$OAUTH_STATE" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Invalid state parameter\"}"
        return 1
    fi
    
    # Prepare token request
    local token_data="grant_type=authorization_code"
    token_data="${token_data}&client_id=${OAUTH_CLIENT_ID}"
    token_data="${token_data}&client_secret=${OAUTH_CLIENT_SECRET}"
    token_data="${token_data}&code=${auth_code}"
    token_data="${token_data}&redirect_uri=${OAUTH_REDIRECT_URI}"
    
    # Make token request
    local response=$(curl -s -X POST "$OAUTH_TOKEN_URL" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "$token_data" 2>/dev/null)
    
    if [[ $? -ne 0 ]]; then
        echo "{\"status\":\"error\",\"message\":\"Failed to exchange authorization code\"}"
        return 1
    fi
    
    # Parse response
    local access_token=$(echo "$response" | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4)
    local refresh_token=$(echo "$response" | grep -o '"refresh_token":"[^"]*"' | cut -d'"' -f4)
    local token_type=$(echo "$response" | grep -o '"token_type":"[^"]*"' | cut -d'"' -f4)
    local expires_in=$(echo "$response" | grep -o '"expires_in":[0-9]*' | cut -d':' -f2)
    
    if [[ -z "$access_token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Failed to extract access token from response\",\"response\":\"$response\"}"
        return 1
    fi
    
    # Store tokens
    OAUTH_ACCESS_TOKEN="$access_token"
    OAUTH_REFRESH_TOKEN="$refresh_token"
    OAUTH_TOKEN_TYPE="$token_type"
    OAUTH_EXPIRES_IN="$expires_in"
    
    echo "{\"status\":\"success\",\"access_token\":\"$access_token\",\"token_type\":\"$token_type\",\"expires_in\":$expires_in}"
}

# Client credentials flow
oauth_client_credentials() {
    if [[ -z "$OAUTH_CLIENT_ID" || -z "$OAUTH_CLIENT_SECRET" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Client ID and Client Secret are required\"}"
        return 1
    fi
    
    if [[ -z "$OAUTH_TOKEN_URL" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token URL not configured\"}"
        return 1
    fi
    
    # Prepare token request
    local token_data="grant_type=client_credentials"
    token_data="${token_data}&client_id=${OAUTH_CLIENT_ID}"
    token_data="${token_data}&client_secret=${OAUTH_CLIENT_SECRET}"
    
    if [[ -n "$OAUTH_SCOPE" ]]; then
        token_data="${token_data}&scope=${OAUTH_SCOPE}"
    fi
    
    # Make token request
    local response=$(curl -s -X POST "$OAUTH_TOKEN_URL" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "$token_data" 2>/dev/null)
    
    if [[ $? -ne 0 ]]; then
        echo "{\"status\":\"error\",\"message\":\"Failed to obtain client credentials token\"}"
        return 1
    fi
    
    # Parse response
    local access_token=$(echo "$response" | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4)
    local token_type=$(echo "$response" | grep -o '"token_type":"[^"]*"' | cut -d'"' -f4)
    local expires_in=$(echo "$response" | grep -o '"expires_in":[0-9]*' | cut -d':' -f2)
    
    if [[ -z "$access_token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Failed to extract access token from response\",\"response\":\"$response\"}"
        return 1
    fi
    
    # Store tokens
    OAUTH_ACCESS_TOKEN="$access_token"
    OAUTH_TOKEN_TYPE="$token_type"
    OAUTH_EXPIRES_IN="$expires_in"
    
    echo "{\"status\":\"success\",\"access_token\":\"$access_token\",\"token_type\":\"$token_type\",\"expires_in\":$expires_in}"
}

# Password flow (Resource Owner Password Credentials)
oauth_password() {
    local username="$1"
    local password="$2"
    
    if [[ -z "$username" || -z "$password" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Username and password are required\"}"
        return 1
    fi
    
    if [[ -z "$OAUTH_CLIENT_ID" || -z "$OAUTH_CLIENT_SECRET" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Client ID and Client Secret are required\"}"
        return 1
    fi
    
    if [[ -z "$OAUTH_TOKEN_URL" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token URL not configured\"}"
        return 1
    fi
    
    # Prepare token request
    local token_data="grant_type=password"
    token_data="${token_data}&client_id=${OAUTH_CLIENT_ID}"
    token_data="${token_data}&client_secret=${OAUTH_CLIENT_SECRET}"
    token_data="${token_data}&username=${username}"
    token_data="${token_data}&password=${password}"
    
    if [[ -n "$OAUTH_SCOPE" ]]; then
        token_data="${token_data}&scope=${OAUTH_SCOPE}"
    fi
    
    # Make token request
    local response=$(curl -s -X POST "$OAUTH_TOKEN_URL" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "$token_data" 2>/dev/null)
    
    if [[ $? -ne 0 ]]; then
        echo "{\"status\":\"error\",\"message\":\"Failed to obtain password grant token\"}"
        return 1
    fi
    
    # Parse response
    local access_token=$(echo "$response" | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4)
    local refresh_token=$(echo "$response" | grep -o '"refresh_token":"[^"]*"' | cut -d'"' -f4)
    local token_type=$(echo "$response" | grep -o '"token_type":"[^"]*"' | cut -d'"' -f4)
    local expires_in=$(echo "$response" | grep -o '"expires_in":[0-9]*' | cut -d':' -f2)
    
    if [[ -z "$access_token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Failed to extract access token from response\",\"response\":\"$response\"}"
        return 1
    fi
    
    # Store tokens
    OAUTH_ACCESS_TOKEN="$access_token"
    OAUTH_REFRESH_TOKEN="$refresh_token"
    OAUTH_TOKEN_TYPE="$token_type"
    OAUTH_EXPIRES_IN="$expires_in"
    
    echo "{\"status\":\"success\",\"access_token\":\"$access_token\",\"token_type\":\"$token_type\",\"expires_in\":$expires_in}"
}

# Refresh access token
oauth_refresh() {
    local refresh_token="$1"
    
    if [[ -z "$refresh_token" ]]; then
        refresh_token="$OAUTH_REFRESH_TOKEN"
    fi
    
    if [[ -z "$refresh_token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Refresh token is required\"}"
        return 1
    fi
    
    if [[ -z "$OAUTH_TOKEN_URL" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token URL not configured\"}"
        return 1
    fi
    
    # Prepare token request
    local token_data="grant_type=refresh_token"
    token_data="${token_data}&client_id=${OAUTH_CLIENT_ID}"
    token_data="${token_data}&client_secret=${OAUTH_CLIENT_SECRET}"
    token_data="${token_data}&refresh_token=${refresh_token}"
    
    # Make token request
    local response=$(curl -s -X POST "$OAUTH_TOKEN_URL" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "$token_data" 2>/dev/null)
    
    if [[ $? -ne 0 ]]; then
        echo "{\"status\":\"error\",\"message\":\"Failed to refresh token\"}"
        return 1
    fi
    
    # Parse response
    local access_token=$(echo "$response" | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4)
    local new_refresh_token=$(echo "$response" | grep -o '"refresh_token":"[^"]*"' | cut -d'"' -f4)
    local token_type=$(echo "$response" | grep -o '"token_type":"[^"]*"' | cut -d'"' -f4)
    local expires_in=$(echo "$response" | grep -o '"expires_in":[0-9]*' | cut -d':' -f2)
    
    if [[ -z "$access_token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Failed to extract access token from response\",\"response\":\"$response\"}"
        return 1
    fi
    
    # Store tokens
    OAUTH_ACCESS_TOKEN="$access_token"
    if [[ -n "$new_refresh_token" ]]; then
        OAUTH_REFRESH_TOKEN="$new_refresh_token"
    fi
    OAUTH_TOKEN_TYPE="$token_type"
    OAUTH_EXPIRES_IN="$expires_in"
    
    echo "{\"status\":\"success\",\"access_token\":\"$access_token\",\"token_type\":\"$token_type\",\"expires_in\":$expires_in}"
}

# Get current token info
oauth_token_info() {
    if [[ -z "$OAUTH_ACCESS_TOKEN" ]]; then
        echo "{\"status\":\"error\",\"message\":\"No access token available\"}"
        return 1
    fi
    
    echo "{\"status\":\"success\",\"access_token\":\"$OAUTH_ACCESS_TOKEN\",\"token_type\":\"$OAUTH_TOKEN_TYPE\",\"expires_in\":$OAUTH_EXPIRES_IN,\"has_refresh_token\":\"$([[ -n "$OAUTH_REFRESH_TOKEN" ]] && echo "true" || echo "false")\"}"
}

# Revoke token
oauth_revoke() {
    local token="$1"
    local token_type="$2"
    
    if [[ -z "$token" ]]; then
        token="$OAUTH_ACCESS_TOKEN"
    fi
    
    if [[ -z "$token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Token is required\"}"
        return 1
    fi
    
    # Note: This is a placeholder. Most OAuth providers have different revocation endpoints
    # and requirements. This would need to be customized per provider.
    echo "{\"status\":\"warning\",\"message\":\"Token revocation not implemented for this provider. Please use provider-specific revocation endpoint.\",\"token\":\"$token\"}"
}

# Main OAuth operator function
execute_oauth() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local client_id=$(echo "$params" | grep -o 'client_id=[^,]*' | cut -d'=' -f2)
            local client_secret=$(echo "$params" | grep -o 'client_secret=[^,]*' | cut -d'=' -f2)
            local redirect_uri=$(echo "$params" | grep -o 'redirect_uri=[^,]*' | cut -d'=' -f2)
            local auth_url=$(echo "$params" | grep -o 'auth_url=[^,]*' | cut -d'=' -f2)
            local token_url=$(echo "$params" | grep -o 'token_url=[^,]*' | cut -d'=' -f2)
            local scope=$(echo "$params" | grep -o 'scope=[^,]*' | cut -d'=' -f2)
            oauth_init "$client_id" "$client_secret" "$redirect_uri" "$auth_url" "$token_url" "$scope"
            ;;
        "auth_url")
            oauth_auth_url
            ;;
        "exchange_code")
            local auth_code=$(echo "$params" | grep -o 'auth_code=[^,]*' | cut -d'=' -f2)
            local state=$(echo "$params" | grep -o 'state=[^,]*' | cut -d'=' -f2)
            oauth_exchange_code "$auth_code" "$state"
            ;;
        "client_credentials")
            oauth_client_credentials
            ;;
        "password")
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local password=$(echo "$params" | grep -o 'password=[^,]*' | cut -d'=' -f2)
            oauth_password "$username" "$password"
            ;;
        "refresh")
            local refresh_token=$(echo "$params" | grep -o 'refresh_token=[^,]*' | cut -d'=' -f2)
            oauth_refresh "$refresh_token"
            ;;
        "token_info")
            oauth_token_info
            ;;
        "revoke")
            local token=$(echo "$params" | grep -o 'token=[^,]*' | cut -d'=' -f2)
            local token_type=$(echo "$params" | grep -o 'token_type=[^,]*' | cut -d'=' -f2)
            oauth_revoke "$token" "$token_type"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, auth_url, exchange_code, client_credentials, password, refresh, token_info, revoke\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_oauth oauth_init oauth_auth_url oauth_exchange_code oauth_client_credentials oauth_password oauth_refresh oauth_token_info oauth_revoke 
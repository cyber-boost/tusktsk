#!/bin/bash

# SMS Operator Implementation
# Provides SMS messaging via various providers (Twilio, AWS SNS, etc.)

# Global variables
SMS_PROVIDER=""
SMS_API_KEY=""
SMS_API_SECRET=""
SMS_FROM_NUMBER=""
SMS_ACCOUNT_SID=""
SMS_AUTH_TOKEN=""
SMS_AWS_REGION="us-east-1"
SMS_TIMEOUT="30"

# Initialize SMS operator
sms_init() {
    local provider="$1"
    local api_key="$2"
    local api_secret="$3"
    local from_number="$4"
    local account_sid="$5"
    local auth_token="$6"
    local aws_region="$7"
    
    if [[ -z "$provider" ]]; then
        echo "{\"status\":\"error\",\"message\":\"SMS provider is required\"}"
        return 1
    fi
    
    SMS_PROVIDER="$provider"
    
    case "$provider" in
        "twilio")
            if [[ -z "$account_sid" || -z "$auth_token" || -z "$from_number" ]]; then
                echo "{\"status\":\"error\",\"message\":\"Twilio requires account_sid, auth_token, and from_number\"}"
                return 1
            fi
            SMS_ACCOUNT_SID="$account_sid"
            SMS_AUTH_TOKEN="$auth_token"
            SMS_FROM_NUMBER="$from_number"
            ;;
        "aws")
            if [[ -z "$api_key" || -z "$api_secret" ]]; then
                echo "{\"status\":\"error\",\"message\":\"AWS requires api_key and api_secret\"}"
                return 1
            fi
            SMS_API_KEY="$api_key"
            SMS_API_SECRET="$api_secret"
            SMS_AWS_REGION="${aws_region:-us-east-1}"
            ;;
        "generic")
            if [[ -z "$api_key" || -z "$api_secret" ]]; then
                echo "{\"status\":\"error\",\"message\":\"Generic provider requires api_key and api_secret\"}"
                return 1
            fi
            SMS_API_KEY="$api_key"
            SMS_API_SECRET="$api_secret"
            SMS_FROM_NUMBER="$from_number"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported provider: $provider. Supported: twilio, aws, generic\"}"
            return 1
            ;;
    esac
    
    echo "{\"status\":\"success\",\"message\":\"SMS operator initialized\",\"provider\":\"$provider\"}"
}

# Send SMS via Twilio
sms_send_twilio() {
    local to_number="$1"
    local message="$2"
    local media_url="$3"
    
    if [[ -z "$to_number" || -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"To number and message are required\"}"
        return 1
    fi
    
    if [[ -z "$SMS_ACCOUNT_SID" || -z "$SMS_AUTH_TOKEN" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Twilio not properly initialized\"}"
        return 1
    fi
    
    # Prepare request data
    local request_data="From=${SMS_FROM_NUMBER}&To=${to_number}&Body=${message}"
    if [[ -n "$media_url" ]]; then
        request_data="${request_data}&MediaUrl=${media_url}"
    fi
    
    # Send SMS via Twilio API
    local response=$(curl -s --connect-timeout "$SMS_TIMEOUT" \
        -X POST "https://api.twilio.com/2010-04-01/Accounts/${SMS_ACCOUNT_SID}/Messages.json" \
        -u "${SMS_ACCOUNT_SID}:${SMS_AUTH_TOKEN}" \
        -d "$request_data" \
        -H "Content-Type: application/x-www-form-urlencoded" 2>&1)
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        # Parse Twilio response
        local sid=$(echo "$response" | grep -o '"sid":"[^"]*"' | cut -d'"' -f4)
        local status=$(echo "$response" | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
        
        if [[ -n "$sid" ]]; then
            echo "{\"status\":\"success\",\"message\":\"SMS sent via Twilio\",\"sid\":\"$sid\",\"status\":\"$status\",\"to\":\"$to_number\"}"
        else
            echo "{\"status\":\"error\",\"message\":\"Failed to parse Twilio response\",\"response\":\"$response\"}"
            return 1
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to send SMS via Twilio\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send SMS via AWS SNS
sms_send_aws() {
    local to_number="$1"
    local message="$2"
    local message_type="$3"
    
    if [[ -z "$to_number" || -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"To number and message are required\"}"
        return 1
    fi
    
    if [[ -z "$SMS_API_KEY" || -z "$SMS_API_SECRET" ]]; then
        echo "{\"status\":\"error\",\"message\":\"AWS not properly initialized\"}"
        return 1
    fi
    
    # Set AWS credentials
    export AWS_ACCESS_KEY_ID="$SMS_API_KEY"
    export AWS_SECRET_ACCESS_KEY="$SMS_API_SECRET"
    export AWS_DEFAULT_REGION="$SMS_AWS_REGION"
    
    # Send SMS via AWS CLI
    local aws_options="--message \"$message\" --phone-number \"$to_number\""
    if [[ -n "$message_type" ]]; then
        aws_options="$aws_options --message-attributes MessageType={DataType=String,StringValue=\"$message_type\"}"
    fi
    
    local response=$(aws sns publish $aws_options 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        # Parse AWS response
        local message_id=$(echo "$response" | grep -o '"MessageId":"[^"]*"' | cut -d'"' -f4)
        
        if [[ -n "$message_id" ]]; then
            echo "{\"status\":\"success\",\"message\":\"SMS sent via AWS SNS\",\"message_id\":\"$message_id\",\"to\":\"$to_number\"}"
        else
            echo "{\"status\":\"error\",\"message\":\"Failed to parse AWS response\",\"response\":\"$response\"}"
            return 1
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to send SMS via AWS SNS\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send SMS via generic HTTP API
sms_send_generic() {
    local to_number="$1"
    local message="$2"
    local api_url="$3"
    local method="$4"
    
    if [[ -z "$to_number" || -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"To number and message are required\"}"
        return 1
    fi
    
    if [[ -z "$SMS_API_KEY" || -z "$SMS_API_SECRET" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Generic provider not properly initialized\"}"
        return 1
    fi
    
    # Default API URL if not provided
    if [[ -z "$api_url" ]]; then
        api_url="https://api.sms-provider.com/send"
    fi
    
    # Default method
    method="${method:-POST}"
    
    # Prepare request
    local curl_options=""
    local request_data=""
    
    if [[ "$method" == "POST" ]]; then
        request_data="api_key=${SMS_API_KEY}&api_secret=${SMS_API_SECRET}&to=${to_number}&message=${message}&from=${SMS_FROM_NUMBER}"
        curl_options="-d \"$request_data\" -H \"Content-Type: application/x-www-form-urlencoded\""
    else
        curl_options="-G -d \"api_key=${SMS_API_KEY}\" -d \"api_secret=${SMS_API_SECRET}\" -d \"to=${to_number}\" -d \"message=${message}\" -d \"from=${SMS_FROM_NUMBER}\""
    fi
    
    # Send SMS
    local response=$(curl -s --connect-timeout "$SMS_TIMEOUT" \
        -X "$method" \
        $curl_options \
        "$api_url" 2>&1)
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"SMS sent via generic provider\",\"to\":\"$to_number\",\"response\":\"$response\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to send SMS via generic provider\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send SMS (main function)
sms_send() {
    local to_number="$1"
    local message="$2"
    local media_url="$3"
    local message_type="$4"
    local api_url="$5"
    local method="$6"
    
    if [[ -z "$to_number" || -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"To number and message are required\"}"
        return 1
    fi
    
    if [[ -z "$SMS_PROVIDER" ]]; then
        echo "{\"status\":\"error\",\"message\":\"SMS operator not initialized\"}"
        return 1
    fi
    
    case "$SMS_PROVIDER" in
        "twilio")
            sms_send_twilio "$to_number" "$message" "$media_url"
            ;;
        "aws")
            sms_send_aws "$to_number" "$message" "$message_type"
            ;;
        "generic")
            sms_send_generic "$to_number" "$message" "$api_url" "$method"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported provider: $SMS_PROVIDER\"}"
            return 1
            ;;
    esac
}

# Validate phone number
sms_validate_number() {
    local phone_number="$1"
    
    if [[ -z "$phone_number" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Phone number is required\"}"
        return 1
    fi
    
    # Remove all non-digit characters
    local clean_number=$(echo "$phone_number" | tr -cd '0-9')
    
    # Basic validation for different formats
    if [[ ${#clean_number} -ge 10 && ${#clean_number} -le 15 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Valid phone number\",\"original\":\"$phone_number\",\"clean\":\"$clean_number\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Invalid phone number format\",\"number\":\"$phone_number\"}"
        return 1
    fi
}

# Format phone number for specific provider
sms_format_number() {
    local phone_number="$1"
    local provider="$2"
    
    if [[ -z "$phone_number" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Phone number is required\"}"
        return 1
    fi
    
    # Remove all non-digit characters
    local clean_number=$(echo "$phone_number" | tr -cd '0-9')
    
    # Format based on provider
    case "$provider" in
        "twilio")
            # Twilio expects E.164 format (+1234567890)
            if [[ ${#clean_number} -eq 10 ]]; then
                clean_number="+1$clean_number"
            elif [[ ${#clean_number} -eq 11 && ${clean_number:0:1} -eq 1 ]]; then
                clean_number="+$clean_number"
            fi
            ;;
        "aws")
            # AWS SNS expects E.164 format
            if [[ ${#clean_number} -eq 10 ]]; then
                clean_number="+1$clean_number"
            elif [[ ${#clean_number} -eq 11 && ${clean_number:0:1} -eq 1 ]]; then
                clean_number="+$clean_number"
            fi
            ;;
        *)
            # Generic formatting
            if [[ ${#clean_number} -eq 10 ]]; then
                clean_number="+1$clean_number"
            fi
            ;;
    esac
    
    echo "{\"status\":\"success\",\"message\":\"Phone number formatted\",\"original\":\"$phone_number\",\"formatted\":\"$clean_number\"}"
}

# Get SMS configuration
sms_config() {
    if [[ -z "$SMS_PROVIDER" ]]; then
        echo "{\"status\":\"error\",\"message\":\"SMS operator not initialized\"}"
        return 1
    fi
    
    local config="{\"provider\":\"$SMS_PROVIDER\""
    
    case "$SMS_PROVIDER" in
        "twilio")
            config="${config},\"account_sid\":\"$SMS_ACCOUNT_SID\",\"from_number\":\"$SMS_FROM_NUMBER\""
            ;;
        "aws")
            config="${config},\"api_key\":\"$SMS_API_KEY\",\"region\":\"$SMS_AWS_REGION\""
            ;;
        "generic")
            config="${config},\"api_key\":\"$SMS_API_KEY\",\"from_number\":\"$SMS_FROM_NUMBER\""
            ;;
    esac
    
    config="${config}}"
    echo "{\"status\":\"success\",\"config\":$config}"
}

# Test SMS connection
sms_test() {
    if [[ -z "$SMS_PROVIDER" ]]; then
        echo "{\"status\":\"error\",\"message\":\"SMS operator not initialized\"}"
        return 1
    fi
    
    case "$SMS_PROVIDER" in
        "twilio")
            # Test Twilio API connection
            local response=$(curl -s --connect-timeout "$SMS_TIMEOUT" \
                -u "${SMS_ACCOUNT_SID}:${SMS_AUTH_TOKEN}" \
                "https://api.twilio.com/2010-04-01/Accounts/${SMS_ACCOUNT_SID}.json" 2>&1)
            
            if [[ $? -eq 0 && "$response" == *"friendly_name"* ]]; then
                echo "{\"status\":\"success\",\"message\":\"Twilio connection test successful\"}"
            else
                echo "{\"status\":\"error\",\"message\":\"Twilio connection test failed\",\"error\":\"$response\"}"
                return 1
            fi
            ;;
        "aws")
            # Test AWS credentials
            export AWS_ACCESS_KEY_ID="$SMS_API_KEY"
            export AWS_SECRET_ACCESS_KEY="$SMS_API_SECRET"
            export AWS_DEFAULT_REGION="$SMS_AWS_REGION"
            
            local response=$(aws sts get-caller-identity 2>&1)
            
            if [[ $? -eq 0 ]]; then
                echo "{\"status\":\"success\",\"message\":\"AWS connection test successful\"}"
            else
                echo "{\"status\":\"error\",\"message\":\"AWS connection test failed\",\"error\":\"$response\"}"
                return 1
            fi
            ;;
        "generic")
            echo "{\"status\":\"warning\",\"message\":\"Generic provider connection test not implemented\"}"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown provider: $SMS_PROVIDER\"}"
            return 1
            ;;
    esac
}

# Main SMS operator function
execute_sms() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local provider=$(echo "$params" | grep -o 'provider=[^,]*' | cut -d'=' -f2)
            local api_key=$(echo "$params" | grep -o 'api_key=[^,]*' | cut -d'=' -f2)
            local api_secret=$(echo "$params" | grep -o 'api_secret=[^,]*' | cut -d'=' -f2)
            local from_number=$(echo "$params" | grep -o 'from_number=[^,]*' | cut -d'=' -f2)
            local account_sid=$(echo "$params" | grep -o 'account_sid=[^,]*' | cut -d'=' -f2)
            local auth_token=$(echo "$params" | grep -o 'auth_token=[^,]*' | cut -d'=' -f2)
            local aws_region=$(echo "$params" | grep -o 'aws_region=[^,]*' | cut -d'=' -f2)
            sms_init "$provider" "$api_key" "$api_secret" "$from_number" "$account_sid" "$auth_token" "$aws_region"
            ;;
        "send")
            local to_number=$(echo "$params" | grep -o 'to_number=[^,]*' | cut -d'=' -f2)
            local message=$(echo "$params" | grep -o 'message=[^,]*' | cut -d'=' -f2)
            local media_url=$(echo "$params" | grep -o 'media_url=[^,]*' | cut -d'=' -f2)
            local message_type=$(echo "$params" | grep -o 'message_type=[^,]*' | cut -d'=' -f2)
            local api_url=$(echo "$params" | grep -o 'api_url=[^,]*' | cut -d'=' -f2)
            local method=$(echo "$params" | grep -o 'method=[^,]*' | cut -d'=' -f2)
            sms_send "$to_number" "$message" "$media_url" "$message_type" "$api_url" "$method"
            ;;
        "validate_number")
            local phone_number=$(echo "$params" | grep -o 'phone_number=[^,]*' | cut -d'=' -f2)
            sms_validate_number "$phone_number"
            ;;
        "format_number")
            local phone_number=$(echo "$params" | grep -o 'phone_number=[^,]*' | cut -d'=' -f2)
            local provider=$(echo "$params" | grep -o 'provider=[^,]*' | cut -d'=' -f2)
            sms_format_number "$phone_number" "$provider"
            ;;
        "config")
            sms_config
            ;;
        "test")
            sms_test
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, send, validate_number, format_number, config, test\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_sms sms_init sms_send sms_send_twilio sms_send_aws sms_send_generic sms_validate_number sms_format_number sms_config sms_test 
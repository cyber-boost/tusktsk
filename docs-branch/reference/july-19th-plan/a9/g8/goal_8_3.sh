#!/bin/bash

# Slack Operator Implementation
# Provides Slack integration and messaging via webhooks and API

# Global variables
SLACK_WEBHOOK_URL=""
SLACK_BOT_TOKEN=""
SLACK_USER_TOKEN=""
SLACK_DEFAULT_CHANNEL=""
SLACK_TIMEOUT="30"
SLACK_USERNAME="Tusk Agent"
SLACK_ICON_EMOJI=":robot_face:"

# Initialize Slack operator
slack_init() {
    local webhook_url="$1"
    local bot_token="$2"
    local user_token="$3"
    local default_channel="$4"
    local username="$5"
    local icon_emoji="$6"
    
    if [[ -z "$webhook_url" && -z "$bot_token" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Either webhook_url or bot_token is required\"}"
        return 1
    fi
    
    SLACK_WEBHOOK_URL="$webhook_url"
    SLACK_BOT_TOKEN="$bot_token"
    SLACK_USER_TOKEN="$user_token"
    SLACK_DEFAULT_CHANNEL="${default_channel:-general}"
    SLACK_USERNAME="${username:-Tusk Agent}"
    SLACK_ICON_EMOJI="${icon_emoji:-:robot_face:}"
    
    echo "{\"status\":\"success\",\"message\":\"Slack operator initialized\",\"default_channel\":\"$SLACK_DEFAULT_CHANNEL\"}"
}

# Send message via webhook
slack_send_webhook() {
    local channel="$1"
    local message="$2"
    local username="$3"
    local icon_emoji="$4"
    local attachments="$5"
    
    if [[ -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Message is required\"}"
        return 1
    fi
    
    if [[ -z "$SLACK_WEBHOOK_URL" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Webhook URL not configured\"}"
        return 1
    fi
    
    # Prepare payload
    local payload="{\"text\":\"$message\""
    
    if [[ -n "$channel" ]]; then
        payload="${payload},\"channel\":\"$channel\""
    fi
    
    if [[ -n "$username" ]]; then
        payload="${payload},\"username\":\"$username\""
    else
        payload="${payload},\"username\":\"$SLACK_USERNAME\""
    fi
    
    if [[ -n "$icon_emoji" ]]; then
        payload="${payload},\"icon_emoji\":\"$icon_emoji\""
    else
        payload="${payload},\"icon_emoji\":\"$SLACK_ICON_EMOJI\""
    fi
    
    if [[ -n "$attachments" ]]; then
        payload="${payload},\"attachments\":$attachments"
    fi
    
    payload="${payload}}"
    
    # Send message
    local response=$(curl -s --connect-timeout "$SLACK_TIMEOUT" \
        -X POST "$SLACK_WEBHOOK_URL" \
        -H "Content-Type: application/json" \
        -d "$payload" 2>&1)
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 && "$response" == "ok" ]]; then
        echo "{\"status\":\"success\",\"message\":\"Slack message sent via webhook\",\"channel\":\"$channel\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to send Slack message via webhook\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send message via API
slack_send_api() {
    local channel="$1"
    local message="$2"
    local thread_ts="$3"
    local attachments="$4"
    
    if [[ -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Message is required\"}"
        return 1
    fi
    
    if [[ -z "$SLACK_BOT_TOKEN" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Bot token not configured\"}"
        return 1
    fi
    
    # Prepare payload
    local payload="{\"channel\":\"$channel\",\"text\":\"$message\""
    
    if [[ -n "$thread_ts" ]]; then
        payload="${payload},\"thread_ts\":\"$thread_ts\""
    fi
    
    if [[ -n "$attachments" ]]; then
        payload="${payload},\"attachments\":$attachments"
    fi
    
    payload="${payload}}"
    
    # Send message via Slack API
    local response=$(curl -s --connect-timeout "$SLACK_TIMEOUT" \
        -X POST "https://slack.com/api/chat.postMessage" \
        -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
        -H "Content-Type: application/json" \
        -d "$payload" 2>&1)
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        # Parse response
        local ok=$(echo "$response" | grep -o '"ok":[^,]*' | cut -d':' -f2)
        local ts=$(echo "$response" | grep -o '"ts":"[^"]*"' | cut -d'"' -f4)
        
        if [[ "$ok" == "true" ]]; then
            echo "{\"status\":\"success\",\"message\":\"Slack message sent via API\",\"channel\":\"$channel\",\"ts\":\"$ts\"}"
        else
            local error=$(echo "$response" | grep -o '"error":"[^"]*"' | cut -d'"' -f4)
            echo "{\"status\":\"error\",\"message\":\"Slack API error\",\"error\":\"$error\"}"
            return 1
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to send Slack message via API\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send message (main function)
slack_send() {
    local channel="$1"
    local message="$2"
    local username="$3"
    local icon_emoji="$4"
    local attachments="$5"
    local thread_ts="$6"
    
    if [[ -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Message is required\"}"
        return 1
    fi
    
    # Use default channel if not specified
    if [[ -z "$channel" ]]; then
        channel="$SLACK_DEFAULT_CHANNEL"
    fi
    
    # Try webhook first, then API
    if [[ -n "$SLACK_WEBHOOK_URL" ]]; then
        slack_send_webhook "$channel" "$message" "$username" "$icon_emoji" "$attachments"
    elif [[ -n "$SLACK_BOT_TOKEN" ]]; then
        slack_send_api "$channel" "$message" "$thread_ts" "$attachments"
    else
        echo "{\"status\":\"error\",\"message\":\"No Slack configuration available\"}"
        return 1
    fi
}

# Send direct message
slack_send_dm() {
    local user_id="$1"
    local message="$2"
    local attachments="$3"
    
    if [[ -z "$user_id" || -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"User ID and message are required\"}"
        return 1
    fi
    
    if [[ -z "$SLACK_BOT_TOKEN" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Bot token required for direct messages\"}"
        return 1
    fi
    
    # Open DM channel
    local dm_response=$(curl -s --connect-timeout "$SLACK_TIMEOUT" \
        -X POST "https://slack.com/api/conversations.open" \
        -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
        -H "Content-Type: application/json" \
        -d "{\"users\":\"$user_id\"}" 2>&1)
    
    local dm_ok=$(echo "$dm_response" | grep -o '"ok":[^,]*' | cut -d':' -f2)
    local channel_id=$(echo "$dm_response" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    
    if [[ "$dm_ok" == "true" && -n "$channel_id" ]]; then
        # Send message to DM channel
        slack_send_api "$channel_id" "$message" "" "$attachments"
    else
        local error=$(echo "$dm_response" | grep -o '"error":"[^"]*"' | cut -d'"' -f4)
        echo "{\"status\":\"error\",\"message\":\"Failed to open DM channel\",\"error\":\"$error\"}"
        return 1
    fi
}

# Create rich message with attachments
slack_create_attachment() {
    local title="$1"
    local text="$2"
    local color="$3"
    local fields="$4"
    local image_url="$5"
    
    local attachment="{\"title\":\"$title\",\"text\":\"$text\""
    
    if [[ -n "$color" ]]; then
        attachment="${attachment},\"color\":\"$color\""
    fi
    
    if [[ -n "$fields" ]]; then
        attachment="${attachment},\"fields\":$fields"
    fi
    
    if [[ -n "$image_url" ]]; then
        attachment="${attachment},\"image_url\":\"$image_url\""
    fi
    
    attachment="${attachment}}"
    
    echo "[$attachment]"
}

# Create field for attachment
slack_create_field() {
    local title="$1"
    local value="$2"
    local short="$3"
    
    local field="{\"title\":\"$title\",\"value\":\"$value\""
    
    if [[ -n "$short" ]]; then
        field="${field},\"short\":$short"
    fi
    
    field="${field}}"
    
    echo "$field"
}

# Upload file to Slack
slack_upload_file() {
    local channel="$1"
    local file_path="$2"
    local title="$3"
    local comment="$4"
    
    if [[ -z "$file_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File path is required\"}"
        return 1
    fi
    
    if [[ ! -f "$file_path" ]]; then
        echo "{\"status\":\"error\",\"message\":\"File not found: $file_path\"}"
        return 1
    fi
    
    if [[ -z "$SLACK_BOT_TOKEN" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Bot token required for file uploads\"}"
        return 1
    fi
    
    # Use default channel if not specified
    if [[ -z "$channel" ]]; then
        channel="$SLACK_DEFAULT_CHANNEL"
    fi
    
    # Prepare upload data
    local upload_data=""
    upload_data="${upload_data} -F channels=$channel"
    upload_data="${upload_data} -F file=@$file_path"
    
    if [[ -n "$title" ]]; then
        upload_data="${upload_data} -F title=$title"
    fi
    
    if [[ -n "$comment" ]]; then
        upload_data="${upload_data} -F initial_comment=$comment"
    fi
    
    # Upload file
    local response=$(curl -s --connect-timeout "$SLACK_TIMEOUT" \
        -X POST "https://slack.com/api/files.upload" \
        -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
        $upload_data 2>&1)
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        local ok=$(echo "$response" | grep -o '"ok":[^,]*' | cut -d':' -f2)
        local file_id=$(echo "$response" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        
        if [[ "$ok" == "true" ]]; then
            echo "{\"status\":\"success\",\"message\":\"File uploaded to Slack\",\"file_id\":\"$file_id\",\"channel\":\"$channel\"}"
        else
            local error=$(echo "$response" | grep -o '"error":"[^"]*"' | cut -d'"' -f4)
            echo "{\"status\":\"error\",\"message\":\"Slack file upload error\",\"error\":\"$error\"}"
            return 1
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to upload file to Slack\",\"error\":\"$response\"}"
        return 1
    fi
}

# Get channel list
slack_list_channels() {
    if [[ -z "$SLACK_BOT_TOKEN" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Bot token required for channel listing\"}"
        return 1
    fi
    
    local response=$(curl -s --connect-timeout "$SLACK_TIMEOUT" \
        -X GET "https://slack.com/api/conversations.list" \
        -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
        -H "Content-Type: application/json" 2>&1)
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        local ok=$(echo "$response" | grep -o '"ok":[^,]*' | cut -d':' -f2)
        
        if [[ "$ok" == "true" ]]; then
            # Extract channel names
            local channels=$(echo "$response" | grep -o '"name":"[^"]*"' | cut -d'"' -f4 | tr '\n' ',' | sed 's/,$//')
            echo "{\"status\":\"success\",\"message\":\"Channels retrieved\",\"channels\":[$channels]}"
        else
            local error=$(echo "$response" | grep -o '"error":"[^"]*"' | cut -d'"' -f4)
            echo "{\"status\":\"error\",\"message\":\"Slack API error\",\"error\":\"$error\"}"
            return 1
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to get channel list\",\"error\":\"$response\"}"
        return 1
    fi
}

# Get user list
slack_list_users() {
    if [[ -z "$SLACK_BOT_TOKEN" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Bot token required for user listing\"}"
        return 1
    fi
    
    local response=$(curl -s --connect-timeout "$SLACK_TIMEOUT" \
        -X GET "https://slack.com/api/users.list" \
        -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
        -H "Content-Type: application/json" 2>&1)
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        local ok=$(echo "$response" | grep -o '"ok":[^,]*' | cut -d':' -f2)
        
        if [[ "$ok" == "true" ]]; then
            # Extract user IDs and names
            local users=$(echo "$response" | grep -o '"id":"[^"]*","name":"[^"]*"' | sed 's/"id":"\([^"]*\)","name":"\([^"]*\)"/{"id":"\1","name":"\2"}/g' | tr '\n' ',' | sed 's/,$//')
            echo "{\"status\":\"success\",\"message\":\"Users retrieved\",\"users\":[$users]}"
        else
            local error=$(echo "$response" | grep -o '"error":"[^"]*"' | cut -d'"' -f4)
            echo "{\"status\":\"error\",\"message\":\"Slack API error\",\"error\":\"$error\"}"
            return 1
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to get user list\",\"error\":\"$response\"}"
        return 1
    fi
}

# Get Slack configuration
slack_config() {
    local config="{\"default_channel\":\"$SLACK_DEFAULT_CHANNEL\",\"username\":\"$SLACK_USERNAME\",\"icon_emoji\":\"$SLACK_ICON_EMOJI\""
    
    if [[ -n "$SLACK_WEBHOOK_URL" ]]; then
        config="${config},\"webhook_configured\":true"
    else
        config="${config},\"webhook_configured\":false"
    fi
    
    if [[ -n "$SLACK_BOT_TOKEN" ]]; then
        config="${config},\"bot_token_configured\":true"
    else
        config="${config},\"bot_token_configured\":false"
    fi
    
    config="${config}}"
    echo "{\"status\":\"success\",\"config\":$config}"
}

# Test Slack connection
slack_test() {
    if [[ -n "$SLACK_WEBHOOK_URL" ]]; then
        # Test webhook
        local response=$(curl -s --connect-timeout "$SLACK_TIMEOUT" \
            -X POST "$SLACK_WEBHOOK_URL" \
            -H "Content-Type: application/json" \
            -d '{"text":"Test message from Tusk Agent"}' 2>&1)
        
        if [[ $? -eq 0 && "$response" == "ok" ]]; then
            echo "{\"status\":\"success\",\"message\":\"Slack webhook test successful\"}"
            return 0
        fi
    fi
    
    if [[ -n "$SLACK_BOT_TOKEN" ]]; then
        # Test API
        local response=$(curl -s --connect-timeout "$SLACK_TIMEOUT" \
            -X GET "https://slack.com/api/auth.test" \
            -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
            -H "Content-Type: application/json" 2>&1)
        
        local ok=$(echo "$response" | grep -o '"ok":[^,]*' | cut -d':' -f2)
        
        if [[ "$ok" == "true" ]]; then
            echo "{\"status\":\"success\",\"message\":\"Slack API test successful\"}"
            return 0
        fi
    fi
    
    echo "{\"status\":\"error\",\"message\":\"Slack connection test failed\"}"
    return 1
}

# Main Slack operator function
execute_slack() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local webhook_url=$(echo "$params" | grep -o 'webhook_url=[^,]*' | cut -d'=' -f2)
            local bot_token=$(echo "$params" | grep -o 'bot_token=[^,]*' | cut -d'=' -f2)
            local user_token=$(echo "$params" | grep -o 'user_token=[^,]*' | cut -d'=' -f2)
            local default_channel=$(echo "$params" | grep -o 'default_channel=[^,]*' | cut -d'=' -f2)
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local icon_emoji=$(echo "$params" | grep -o 'icon_emoji=[^,]*' | cut -d'=' -f2)
            slack_init "$webhook_url" "$bot_token" "$user_token" "$default_channel" "$username" "$icon_emoji"
            ;;
        "send")
            local channel=$(echo "$params" | grep -o 'channel=[^,]*' | cut -d'=' -f2)
            local message=$(echo "$params" | grep -o 'message=[^,]*' | cut -d'=' -f2)
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local icon_emoji=$(echo "$params" | grep -o 'icon_emoji=[^,]*' | cut -d'=' -f2)
            local attachments=$(echo "$params" | grep -o 'attachments=[^,]*' | cut -d'=' -f2)
            local thread_ts=$(echo "$params" | grep -o 'thread_ts=[^,]*' | cut -d'=' -f2)
            slack_send "$channel" "$message" "$username" "$icon_emoji" "$attachments" "$thread_ts"
            ;;
        "send_dm")
            local user_id=$(echo "$params" | grep -o 'user_id=[^,]*' | cut -d'=' -f2)
            local message=$(echo "$params" | grep -o 'message=[^,]*' | cut -d'=' -f2)
            local attachments=$(echo "$params" | grep -o 'attachments=[^,]*' | cut -d'=' -f2)
            slack_send_dm "$user_id" "$message" "$attachments"
            ;;
        "upload_file")
            local channel=$(echo "$params" | grep -o 'channel=[^,]*' | cut -d'=' -f2)
            local file_path=$(echo "$params" | grep -o 'file_path=[^,]*' | cut -d'=' -f2)
            local title=$(echo "$params" | grep -o 'title=[^,]*' | cut -d'=' -f2)
            local comment=$(echo "$params" | grep -o 'comment=[^,]*' | cut -d'=' -f2)
            slack_upload_file "$channel" "$file_path" "$title" "$comment"
            ;;
        "list_channels")
            slack_list_channels
            ;;
        "list_users")
            slack_list_users
            ;;
        "config")
            slack_config
            ;;
        "test")
            slack_test
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, send, send_dm, upload_file, list_channels, list_users, config, test\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_slack slack_init slack_send slack_send_webhook slack_send_api slack_send_dm slack_create_attachment slack_create_field slack_upload_file slack_list_channels slack_list_users slack_config slack_test 
#!/bin/bash

# Alerts Operator Implementation
# Provides alerting and notification management

# Global variables
ALERTS_ENABLED="true"
ALERTS_DEFAULT_CHANNEL="console"
ALERTS_RETRY_COUNT="3"
ALERTS_RETRY_DELAY="60"
ALERTS_STORAGE_PATH="/tmp/alerts"
ALERTS_HISTORY_DAYS="7"

# Alert severity levels
ALERT_CRITICAL="critical"
ALERT_HIGH="high"
ALERT_MEDIUM="medium"
ALERT_LOW="low"
ALERT_INFO="info"

# Initialize Alerts operator
alerts_init() {
    local enabled="$1"
    local default_channel="$2"
    local retry_count="$3"
    local retry_delay="$4"
    local storage_path="$5"
    local history_days="$6"
    
    ALERTS_ENABLED="${enabled:-true}"
    ALERTS_DEFAULT_CHANNEL="${default_channel:-console}"
    ALERTS_RETRY_COUNT="${retry_count:-3}"
    ALERTS_RETRY_DELAY="${retry_delay:-60}"
    ALERTS_STORAGE_PATH="${storage_path:-/tmp/alerts}"
    ALERTS_HISTORY_DAYS="${history_days:-7}"
    
    # Create storage directory if it doesn't exist
    mkdir -p "$ALERTS_STORAGE_PATH"
    
    echo "{\"status\":\"success\",\"message\":\"Alerts operator initialized\",\"enabled\":\"$ALERTS_ENABLED\",\"default_channel\":\"$ALERTS_DEFAULT_CHANNEL\",\"storage_path\":\"$ALERTS_STORAGE_PATH\"}"
}

# Create and send alert
alerts_send() {
    local message="$1"
    local severity="$2"
    local channel="$3"
    local title="$4"
    local tags="$5"
    
    if [[ -z "$message" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Alert message is required\"}"
        return 1
    fi
    
    if [[ "$ALERTS_ENABLED" != "true" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"Alerts are disabled\"}"
        return 0
    fi
    
    severity="${severity:-$ALERT_INFO}"
    channel="${channel:-$ALERTS_DEFAULT_CHANNEL}"
    title="${title:-Alert}"
    tags="${tags:-}"
    
    local timestamp=$(date +%s)
    local alert_id=$(uuidgen 2>/dev/null || echo "alert_${timestamp}")
    
    # Create alert data
    local alert_data="{\"id\":\"$alert_id\",\"timestamp\":$timestamp,\"severity\":\"$severity\",\"title\":\"$title\",\"message\":\"$message\",\"tags\":\"$tags\",\"channel\":\"$channel\"}"
    
    # Send alert based on channel
    local send_result=""
    case "$channel" in
        "console")
            send_result=$(alerts_send_console "$alert_data")
            ;;
        "email")
            send_result=$(alerts_send_email "$alert_data")
            ;;
        "slack")
            send_result=$(alerts_send_slack "$alert_data")
            ;;
        "webhook")
            send_result=$(alerts_send_webhook "$alert_data")
            ;;
        "file")
            send_result=$(alerts_send_file "$alert_data")
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown channel: $channel. Available channels: console, email, slack, webhook, file\"}"
            return 1
            ;;
    esac
    
    # Store alert
    echo "$alert_data" >> "$ALERTS_STORAGE_PATH/alerts.log"
    
    echo "{\"status\":\"success\",\"message\":\"Alert sent\",\"alert_id\":\"$alert_id\",\"channel\":\"$channel\",\"send_result\":$send_result}"
}

# Send alert to console
alerts_send_console() {
    local alert_data="$1"
    
    local severity=$(echo "$alert_data" | grep -o '"severity":"[^"]*"' | cut -d'"' -f4)
    local title=$(echo "$alert_data" | grep -o '"title":"[^"]*"' | cut -d'"' -f4)
    local message=$(echo "$alert_data" | grep -o '"message":"[^"]*"' | cut -d'"' -f4)
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    
    # Color coding based on severity
    local color=""
    case "$severity" in
        "critical")
            color="\033[1;31m"  # Red
            ;;
        "high")
            color="\033[0;31m"  # Red
            ;;
        "medium")
            color="\033[1;33m"  # Yellow
            ;;
        "low")
            color="\033[0;33m"  # Yellow
            ;;
        "info")
            color="\033[0;36m"  # Cyan
            ;;
        *)
            color="\033[0m"     # Default
            ;;
    esac
    
    echo -e "${color}[$timestamp] [$severity] $title: $message\033[0m" >&2
    
    echo "{\"status\":\"success\",\"message\":\"Alert sent to console\"}"
}

# Send alert via email
alerts_send_email() {
    local alert_data="$1"
    
    # Check if email operator is available
    if ! command -v execute_email >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Email operator not available\"}"
        return 1
    fi
    
    local severity=$(echo "$alert_data" | grep -o '"severity":"[^"]*"' | cut -d'"' -f4)
    local title=$(echo "$alert_data" | grep -o '"title":"[^"]*"' | cut -d'"' -f4)
    local message=$(echo "$alert_data" | grep -o '"message":"[^"]*"' | cut -d'"' -f4)
    
    # Get email configuration from environment or use defaults
    local to_email="${ALERT_EMAIL_TO:-admin@localhost}"
    local from_email="${ALERT_EMAIL_FROM:-alerts@localhost}"
    local subject="[ALERT] $title"
    
    local email_body="Alert Details:
Severity: $severity
Title: $title
Message: $message
Timestamp: $(date '+%Y-%m-%d %H:%M:%S')
Host: $(hostname)"
    
    # Send email
    local result=$(execute_email "send" "to=$to_email,from=$from_email,subject=$subject,body=$email_body")
    
    echo "$result"
}

# Send alert to Slack
alerts_send_slack() {
    local alert_data="$1"
    
    # Check if slack operator is available
    if ! command -v execute_slack >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"Slack operator not available\"}"
        return 1
    fi
    
    local severity=$(echo "$alert_data" | grep -o '"severity":"[^"]*"' | cut -d'"' -f4)
    local title=$(echo "$alert_data" | grep -o '"title":"[^"]*"' | cut -d'"' -f4)
    local message=$(echo "$alert_data" | grep -o '"message":"[^"]*"' | cut -d'"' -f4)
    
    # Get Slack configuration from environment
    local webhook_url="${ALERT_SLACK_WEBHOOK:-}"
    local channel="${ALERT_SLACK_CHANNEL:-#alerts}"
    
    if [[ -z "$webhook_url" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Slack webhook URL not configured\"}"
        return 1
    fi
    
    # Create Slack message
    local slack_message="*[$severity] $title*
$message
Host: $(hostname)
Time: $(date '+%Y-%m-%d %H:%M:%S')"
    
    # Send to Slack
    local result=$(execute_slack "send_webhook" "webhook_url=$webhook_url,channel=$channel,message=$slack_message")
    
    echo "$result"
}

# Send alert via webhook
alerts_send_webhook() {
    local alert_data="$1"
    
    local webhook_url="${ALERT_WEBHOOK_URL:-}"
    
    if [[ -z "$webhook_url" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Webhook URL not configured\"}"
        return 1
    fi
    
    # Send webhook
    local response=$(curl -s -X POST -H "Content-Type: application/json" -d "$alert_data" "$webhook_url" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Webhook sent successfully\",\"response\":\"$response\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Webhook failed\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send alert to file
alerts_send_file() {
    local alert_data="$1"
    
    local file_path="${ALERT_FILE_PATH:-$ALERTS_STORAGE_PATH/alerts.txt}"
    
    # Append to file
    echo "$alert_data" >> "$file_path"
    
    echo "{\"status\":\"success\",\"message\":\"Alert written to file\",\"file\":\"$file_path\"}"
}

# Create alert rule
alerts_create_rule() {
    local rule_name="$1"
    local condition="$2"
    local action="$3"
    local severity="$4"
    local enabled="$5"
    
    if [[ -z "$rule_name" || -z "$condition" || -z "$action" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Rule name, condition, and action are required\"}"
        return 1
    fi
    
    severity="${severity:-$ALERT_MEDIUM}"
    enabled="${enabled:-true}"
    
    local rule_data="{\"name\":\"$rule_name\",\"condition\":\"$condition\",\"action\":\"$action\",\"severity\":\"$severity\",\"enabled\":$enabled,\"created_at\":$(date +%s)}"
    
    # Store rule
    echo "$rule_data" >> "$ALERTS_STORAGE_PATH/rules.json"
    
    echo "{\"status\":\"success\",\"message\":\"Alert rule created\",\"rule_name\":\"$rule_name\"}"
}

# List alert rules
alerts_list_rules() {
    local rules_file="$ALERTS_STORAGE_PATH/rules.json"
    
    if [[ ! -f "$rules_file" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"No alert rules found\",\"rules\":[]}"
        return 0
    fi
    
    local rules="[]"
    local count=0
    
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            if [[ $count -eq 0 ]]; then
                rules="[$line"
            else
                rules="$rules,$line"
            fi
            ((count++))
        fi
    done < "$rules_file"
    
    if [[ $count -gt 0 ]]; then
        rules="$rules]"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Alert rules listed\",\"count\":$count,\"rules\":$rules}"
}

# Delete alert rule
alerts_delete_rule() {
    local rule_name="$1"
    
    if [[ -z "$rule_name" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Rule name is required\"}"
        return 1
    fi
    
    local rules_file="$ALERTS_STORAGE_PATH/rules.json"
    
    if [[ ! -f "$rules_file" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"No alert rules found\"}"
        return 0
    fi
    
    local temp_file=$(mktemp)
    local deleted=false
    
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            local name=$(echo "$line" | grep -o '"name":"[^"]*"' | cut -d'"' -f4)
            if [[ "$name" != "$rule_name" ]]; then
                echo "$line" >> "$temp_file"
            else
                deleted=true
            fi
        fi
    done < "$rules_file"
    
    mv "$temp_file" "$rules_file"
    
    if [[ "$deleted" == true ]]; then
        echo "{\"status\":\"success\",\"message\":\"Alert rule deleted\",\"rule_name\":\"$rule_name\"}"
    else
        echo "{\"status\":\"warning\",\"message\":\"Alert rule not found\",\"rule_name\":\"$rule_name\"}"
    fi
}

# Get alert history
alerts_history() {
    local hours="$1"
    local severity="$2"
    local output_file="$3"
    
    hours="${hours:-24}"
    local cutoff_time=$(date -d "$hours hours ago" +%s)
    local alerts_file="$ALERTS_STORAGE_PATH/alerts.log"
    
    if [[ ! -f "$alerts_file" ]]; then
        echo "{\"status\":\"warning\",\"message\":\"No alert history found\",\"alerts\":[]}"
        return 0
    fi
    
    local filtered_alerts="[]"
    local count=0
    
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            local timestamp=$(echo "$line" | grep -o '"timestamp":[0-9]*' | cut -d':' -f2)
            local alert_severity=$(echo "$line" | grep -o '"severity":"[^"]*"' | cut -d'"' -f4)
            
            if [[ -n "$timestamp" && $timestamp -ge $cutoff_time ]]; then
                if [[ -z "$severity" || "$alert_severity" == "$severity" ]]; then
                    if [[ $count -eq 0 ]]; then
                        filtered_alerts="[$line"
                    else
                        filtered_alerts="$filtered_alerts,$line"
                    fi
                    ((count++))
                fi
            fi
        fi
    done < "$alerts_file"
    
    if [[ $count -gt 0 ]]; then
        filtered_alerts="$filtered_alerts]"
    fi
    
    # Save to output file if specified
    if [[ -n "$output_file" ]]; then
        echo "$filtered_alerts" > "$output_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Alert history retrieved\",\"hours\":$hours,\"severity\":\"$severity\",\"count\":$count,\"alerts\":$filtered_alerts}"
}

# Clean old alerts
alerts_cleanup() {
    local days="$1"
    
    days="${days:-$ALERTS_HISTORY_DAYS}"
    local cutoff_time=$(date -d "$days days ago" +%s)
    local alerts_file="$ALERTS_STORAGE_PATH/alerts.log"
    local cleaned_count=0
    
    if [[ -f "$alerts_file" ]]; then
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
        done < "$alerts_file"
        mv "$temp_file" "$alerts_file"
    fi
    
    echo "{\"status\":\"success\",\"message\":\"Alert cleanup completed\",\"days\":$days,\"cleaned_alerts\":$cleaned_count}"
}

# Get alerts configuration
alerts_config() {
    echo "{\"status\":\"success\",\"config\":{\"enabled\":\"$ALERTS_ENABLED\",\"default_channel\":\"$ALERTS_DEFAULT_CHANNEL\",\"retry_count\":\"$ALERTS_RETRY_COUNT\",\"retry_delay\":\"$ALERTS_RETRY_DELAY\",\"storage_path\":\"$ALERTS_STORAGE_PATH\",\"history_days\":\"$ALERTS_HISTORY_DAYS\"}}"
}

# Main Alerts operator function
execute_alerts() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local enabled=$(echo "$params" | grep -o 'enabled=[^,]*' | cut -d'=' -f2)
            local default_channel=$(echo "$params" | grep -o 'default_channel=[^,]*' | cut -d'=' -f2)
            local retry_count=$(echo "$params" | grep -o 'retry_count=[^,]*' | cut -d'=' -f2)
            local retry_delay=$(echo "$params" | grep -o 'retry_delay=[^,]*' | cut -d'=' -f2)
            local storage_path=$(echo "$params" | grep -o 'storage_path=[^,]*' | cut -d'=' -f2)
            local history_days=$(echo "$params" | grep -o 'history_days=[^,]*' | cut -d'=' -f2)
            alerts_init "$enabled" "$default_channel" "$retry_count" "$retry_delay" "$storage_path" "$history_days"
            ;;
        "send")
            local message=$(echo "$params" | grep -o 'message=[^,]*' | cut -d'=' -f2)
            local severity=$(echo "$params" | grep -o 'severity=[^,]*' | cut -d'=' -f2)
            local channel=$(echo "$params" | grep -o 'channel=[^,]*' | cut -d'=' -f2)
            local title=$(echo "$params" | grep -o 'title=[^,]*' | cut -d'=' -f2)
            local tags=$(echo "$params" | grep -o 'tags=[^,]*' | cut -d'=' -f2)
            alerts_send "$message" "$severity" "$channel" "$title" "$tags"
            ;;
        "create_rule")
            local rule_name=$(echo "$params" | grep -o 'rule_name=[^,]*' | cut -d'=' -f2)
            local condition=$(echo "$params" | grep -o 'condition=[^,]*' | cut -d'=' -f2)
            local action=$(echo "$params" | grep -o 'action=[^,]*' | cut -d'=' -f2)
            local severity=$(echo "$params" | grep -o 'severity=[^,]*' | cut -d'=' -f2)
            local enabled=$(echo "$params" | grep -o 'enabled=[^,]*' | cut -d'=' -f2)
            alerts_create_rule "$rule_name" "$condition" "$action" "$severity" "$enabled"
            ;;
        "list_rules")
            alerts_list_rules
            ;;
        "delete_rule")
            local rule_name=$(echo "$params" | grep -o 'rule_name=[^,]*' | cut -d'=' -f2)
            alerts_delete_rule "$rule_name"
            ;;
        "history")
            local hours=$(echo "$params" | grep -o 'hours=[^,]*' | cut -d'=' -f2)
            local severity=$(echo "$params" | grep -o 'severity=[^,]*' | cut -d'=' -f2)
            local output_file=$(echo "$params" | grep -o 'output_file=[^,]*' | cut -d'=' -f2)
            alerts_history "$hours" "$severity" "$output_file"
            ;;
        "cleanup")
            local days=$(echo "$params" | grep -o 'days=[^,]*' | cut -d'=' -f2)
            alerts_cleanup "$days"
            ;;
        "config")
            alerts_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, send, create_rule, list_rules, delete_rule, history, cleanup, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_alerts alerts_init alerts_send alerts_send_console alerts_send_email alerts_send_slack alerts_send_webhook alerts_send_file alerts_create_rule alerts_list_rules alerts_delete_rule alerts_history alerts_cleanup alerts_config 
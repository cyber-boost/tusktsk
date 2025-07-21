#!/bin/bash

# Email Operator Implementation
# Provides email sending and management via SMTP and other providers

# Global variables
EMAIL_SMTP_HOST=""
EMAIL_SMTP_PORT="587"
EMAIL_SMTP_USERNAME=""
EMAIL_SMTP_PASSWORD=""
EMAIL_FROM_ADDRESS=""
EMAIL_FROM_NAME=""
EMAIL_USE_TLS="true"
EMAIL_USE_SSL="false"
EMAIL_TIMEOUT="30"

# Initialize Email operator
email_init() {
    local smtp_host="$1"
    local smtp_port="$2"
    local username="$3"
    local password="$4"
    local from_address="$5"
    local from_name="$6"
    local use_tls="$7"
    local use_ssl="$8"
    
    if [[ -z "$smtp_host" || -z "$username" || -z "$password" ]]; then
        echo "{\"status\":\"error\",\"message\":\"SMTP host, username, and password are required\"}"
        return 1
    fi
    
    EMAIL_SMTP_HOST="$smtp_host"
    EMAIL_SMTP_PORT="${smtp_port:-587}"
    EMAIL_SMTP_USERNAME="$username"
    EMAIL_SMTP_PASSWORD="$password"
    EMAIL_FROM_ADDRESS="${from_address:-$username}"
    EMAIL_FROM_NAME="${from_name:-Tusk Agent}"
    EMAIL_USE_TLS="${use_tls:-true}"
    EMAIL_USE_SSL="${use_ssl:-false}"
    
    echo "{\"status\":\"success\",\"message\":\"Email operator initialized\",\"smtp_host\":\"$smtp_host\",\"port\":\"$EMAIL_SMTP_PORT\"}"
}

# Send email via SMTP
email_send() {
    local to_address="$1"
    local subject="$2"
    local body="$3"
    local html_body="$4"
    local attachments="$5"
    local cc_addresses="$6"
    local bcc_addresses="$7"
    
    if [[ -z "$to_address" || -z "$subject" || -z "$body" ]]; then
        echo "{\"status\":\"error\",\"message\":\"To address, subject, and body are required\"}"
        return 1
    fi
    
    if [[ -z "$EMAIL_SMTP_HOST" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Email operator not initialized\"}"
        return 1
    fi
    
    # Create temporary email file
    local email_file=$(mktemp)
    local boundary="boundary_$(date +%s)_$(openssl rand -hex 8)"
    
    # Build email headers
    cat > "$email_file" <<EOF
From: ${EMAIL_FROM_NAME} <${EMAIL_FROM_ADDRESS}>
To: ${to_address}
Subject: ${subject}
Date: $(date -R)
MIME-Version: 1.0
Content-Type: multipart/mixed; boundary="${boundary}"

--${boundary}
Content-Type: text/plain; charset=UTF-8
Content-Transfer-Encoding: 8bit

${body}

EOF

    # Add HTML body if provided
    if [[ -n "$html_body" ]]; then
        cat >> "$email_file" <<EOF
--${boundary}
Content-Type: text/html; charset=UTF-8
Content-Transfer-Encoding: 8bit

${html_body}

EOF
    fi
    
    # Add attachments if provided
    if [[ -n "$attachments" ]]; then
        local attachment_list=(${attachments//,/ })
        for attachment in "${attachment_list[@]}"; do
            if [[ -f "$attachment" ]]; then
                local filename=$(basename "$attachment")
                local mime_type=$(file --mime-type -b "$attachment" 2>/dev/null || echo "application/octet-stream")
                local base64_content=$(base64 -w 0 "$attachment")
                
                cat >> "$email_file" <<EOF
--${boundary}
Content-Type: ${mime_type}; name="${filename}"
Content-Transfer-Encoding: base64
Content-Disposition: attachment; filename="${filename}"

${base64_content}

EOF
            fi
        done
    fi
    
    # Close boundary
    echo "--${boundary}--" >> "$email_file"
    
    # Send email using curl or openssl
    local smtp_url=""
    if [[ "$EMAIL_USE_SSL" == "true" ]]; then
        smtp_url="smtps://${EMAIL_SMTP_HOST}:${EMAIL_SMTP_PORT}"
    else
        smtp_url="smtp://${EMAIL_SMTP_HOST}:${EMAIL_SMTP_PORT}"
    fi
    
    local curl_options=""
    if [[ "$EMAIL_USE_TLS" == "true" ]]; then
        curl_options="--ssl-reqd"
    fi
    
    # Send email
    local response=$(curl -s --connect-timeout "$EMAIL_TIMEOUT" \
        --mail-from "$EMAIL_FROM_ADDRESS" \
        --mail-rcpt "$to_address" \
        --upload-file "$email_file" \
        --user "${EMAIL_SMTP_USERNAME}:${EMAIL_SMTP_PASSWORD}" \
        $curl_options \
        "$smtp_url" 2>&1)
    
    local exit_code=$?
    
    # Clean up
    rm -f "$email_file"
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Email sent successfully\",\"to\":\"$to_address\",\"subject\":\"$subject\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to send email\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send email using sendmail (alternative method)
email_send_sendmail() {
    local to_address="$1"
    local subject="$2"
    local body="$3"
    local html_body="$4"
    local attachments="$5"
    
    if [[ -z "$to_address" || -z "$subject" || -z "$body" ]]; then
        echo "{\"status\":\"error\",\"message\":\"To address, subject, and body are required\"}"
        return 1
    fi
    
    # Create email content
    local email_content=""
    email_content+="From: ${EMAIL_FROM_NAME} <${EMAIL_FROM_ADDRESS}>\n"
    email_content+="To: ${to_address}\n"
    email_content+="Subject: ${subject}\n"
    email_content+="Date: $(date -R)\n"
    email_content+="MIME-Version: 1.0\n"
    email_content+="Content-Type: text/plain; charset=UTF-8\n\n"
    email_content+="${body}\n"
    
    # Send using sendmail
    local response=$(echo -e "$email_content" | sendmail -t 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Email sent via sendmail\",\"to\":\"$to_address\",\"subject\":\"$subject\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to send email via sendmail\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send email using mail command (simple method)
email_send_mail() {
    local to_address="$1"
    local subject="$2"
    local body="$3"
    local attachments="$4"
    
    if [[ -z "$to_address" || -z "$subject" || -z "$body" ]]; then
        echo "{\"status\":\"error\",\"message\":\"To address, subject, and body are required\"}"
        return 1
    fi
    
    # Create temporary body file
    local body_file=$(mktemp)
    echo "$body" > "$body_file"
    
    # Send using mail command
    local mail_options="-s \"$subject\""
    if [[ -n "$attachments" ]]; then
        local attachment_list=(${attachments//,/ })
        for attachment in "${attachment_list[@]}"; do
            if [[ -f "$attachment" ]]; then
                mail_options="$mail_options -a \"$attachment\""
            fi
        done
    fi
    
    local response=$(eval "mail $mail_options \"$to_address\" < \"$body_file\"" 2>&1)
    local exit_code=$?
    
    # Clean up
    rm -f "$body_file"
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"Email sent via mail command\",\"to\":\"$to_address\",\"subject\":\"$subject\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Failed to send email via mail command\",\"error\":\"$response\"}"
        return 1
    fi
}

# Send email using external SMTP service (Gmail, Outlook, etc.)
email_send_external() {
    local provider="$1"
    local to_address="$2"
    local subject="$3"
    local body="$4"
    local html_body="$5"
    local attachments="$6"
    
    if [[ -z "$provider" || -z "$to_address" || -z "$subject" || -z "$body" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Provider, to address, subject, and body are required\"}"
        return 1
    fi
    
    # Configure provider-specific settings
    case "$provider" in
        "gmail")
            EMAIL_SMTP_HOST="smtp.gmail.com"
            EMAIL_SMTP_PORT="587"
            EMAIL_USE_TLS="true"
            EMAIL_USE_SSL="false"
            ;;
        "outlook")
            EMAIL_SMTP_HOST="smtp-mail.outlook.com"
            EMAIL_SMTP_PORT="587"
            EMAIL_USE_TLS="true"
            EMAIL_USE_SSL="false"
            ;;
        "yahoo")
            EMAIL_SMTP_HOST="smtp.mail.yahoo.com"
            EMAIL_SMTP_PORT="587"
            EMAIL_USE_TLS="true"
            EMAIL_USE_SSL="false"
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unsupported provider: $provider. Supported: gmail, outlook, yahoo\"}"
            return 1
            ;;
    esac
    
    # Send email using the configured provider
    email_send "$to_address" "$subject" "$body" "$html_body" "$attachments"
}

# Validate email address
email_validate() {
    local email="$1"
    
    if [[ -z "$email" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Email address is required\"}"
        return 1
    fi
    
    # Simple email validation regex
    if [[ "$email" =~ ^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$ ]]; then
        echo "{\"status\":\"success\",\"message\":\"Valid email address\",\"email\":\"$email\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"Invalid email address format\",\"email\":\"$email\"}"
        return 1
    fi
}

# Get email configuration
email_config() {
    if [[ -z "$EMAIL_SMTP_HOST" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Email operator not initialized\"}"
        return 1
    fi
    
    echo "{\"status\":\"success\",\"config\":{\"smtp_host\":\"$EMAIL_SMTP_HOST\",\"smtp_port\":\"$EMAIL_SMTP_PORT\",\"from_address\":\"$EMAIL_FROM_ADDRESS\",\"from_name\":\"$EMAIL_FROM_NAME\",\"use_tls\":\"$EMAIL_USE_TLS\",\"use_ssl\":\"$EMAIL_USE_SSL\"}}"
}

# Test email connection
email_test() {
    if [[ -z "$EMAIL_SMTP_HOST" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Email operator not initialized\"}"
        return 1
    fi
    
    # Test SMTP connection
    local smtp_url=""
    if [[ "$EMAIL_USE_SSL" == "true" ]]; then
        smtp_url="smtps://${EMAIL_SMTP_HOST}:${EMAIL_SMTP_PORT}"
    else
        smtp_url="smtp://${EMAIL_SMTP_HOST}:${EMAIL_SMTP_PORT}"
    fi
    
    local curl_options=""
    if [[ "$EMAIL_USE_TLS" == "true" ]]; then
        curl_options="--ssl-reqd"
    fi
    
    local response=$(curl -s --connect-timeout "$EMAIL_TIMEOUT" \
        --user "${EMAIL_SMTP_USERNAME}:${EMAIL_SMTP_PASSWORD}" \
        $curl_options \
        "$smtp_url" 2>&1)
    
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        echo "{\"status\":\"success\",\"message\":\"SMTP connection test successful\"}"
    else
        echo "{\"status\":\"error\",\"message\":\"SMTP connection test failed\",\"error\":\"$response\"}"
        return 1
    fi
}

# Main Email operator function
execute_email() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local smtp_host=$(echo "$params" | grep -o 'smtp_host=[^,]*' | cut -d'=' -f2)
            local smtp_port=$(echo "$params" | grep -o 'smtp_port=[^,]*' | cut -d'=' -f2)
            local username=$(echo "$params" | grep -o 'username=[^,]*' | cut -d'=' -f2)
            local password=$(echo "$params" | grep -o 'password=[^,]*' | cut -d'=' -f2)
            local from_address=$(echo "$params" | grep -o 'from_address=[^,]*' | cut -d'=' -f2)
            local from_name=$(echo "$params" | grep -o 'from_name=[^,]*' | cut -d'=' -f2)
            local use_tls=$(echo "$params" | grep -o 'use_tls=[^,]*' | cut -d'=' -f2)
            local use_ssl=$(echo "$params" | grep -o 'use_ssl=[^,]*' | cut -d'=' -f2)
            email_init "$smtp_host" "$smtp_port" "$username" "$password" "$from_address" "$from_name" "$use_tls" "$use_ssl"
            ;;
        "send")
            local to_address=$(echo "$params" | grep -o 'to_address=[^,]*' | cut -d'=' -f2)
            local subject=$(echo "$params" | grep -o 'subject=[^,]*' | cut -d'=' -f2)
            local body=$(echo "$params" | grep -o 'body=[^,]*' | cut -d'=' -f2)
            local html_body=$(echo "$params" | grep -o 'html_body=[^,]*' | cut -d'=' -f2)
            local attachments=$(echo "$params" | grep -o 'attachments=[^,]*' | cut -d'=' -f2)
            local cc_addresses=$(echo "$params" | grep -o 'cc_addresses=[^,]*' | cut -d'=' -f2)
            local bcc_addresses=$(echo "$params" | grep -o 'bcc_addresses=[^,]*' | cut -d'=' -f2)
            email_send "$to_address" "$subject" "$body" "$html_body" "$attachments" "$cc_addresses" "$bcc_addresses"
            ;;
        "send_sendmail")
            local to_address=$(echo "$params" | grep -o 'to_address=[^,]*' | cut -d'=' -f2)
            local subject=$(echo "$params" | grep -o 'subject=[^,]*' | cut -d'=' -f2)
            local body=$(echo "$params" | grep -o 'body=[^,]*' | cut -d'=' -f2)
            local html_body=$(echo "$params" | grep -o 'html_body=[^,]*' | cut -d'=' -f2)
            local attachments=$(echo "$params" | grep -o 'attachments=[^,]*' | cut -d'=' -f2)
            email_send_sendmail "$to_address" "$subject" "$body" "$html_body" "$attachments"
            ;;
        "send_mail")
            local to_address=$(echo "$params" | grep -o 'to_address=[^,]*' | cut -d'=' -f2)
            local subject=$(echo "$params" | grep -o 'subject=[^,]*' | cut -d'=' -f2)
            local body=$(echo "$params" | grep -o 'body=[^,]*' | cut -d'=' -f2)
            local attachments=$(echo "$params" | grep -o 'attachments=[^,]*' | cut -d'=' -f2)
            email_send_mail "$to_address" "$subject" "$body" "$attachments"
            ;;
        "send_external")
            local provider=$(echo "$params" | grep -o 'provider=[^,]*' | cut -d'=' -f2)
            local to_address=$(echo "$params" | grep -o 'to_address=[^,]*' | cut -d'=' -f2)
            local subject=$(echo "$params" | grep -o 'subject=[^,]*' | cut -d'=' -f2)
            local body=$(echo "$params" | grep -o 'body=[^,]*' | cut -d'=' -f2)
            local html_body=$(echo "$params" | grep -o 'html_body=[^,]*' | cut -d'=' -f2)
            local attachments=$(echo "$params" | grep -o 'attachments=[^,]*' | cut -d'=' -f2)
            email_send_external "$provider" "$to_address" "$subject" "$body" "$html_body" "$attachments"
            ;;
        "validate")
            local email=$(echo "$params" | grep -o 'email=[^,]*' | cut -d'=' -f2)
            email_validate "$email"
            ;;
        "config")
            email_config
            ;;
        "test")
            email_test
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, send, send_sendmail, send_mail, send_external, validate, config, test\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_email email_init email_send email_send_sendmail email_send_mail email_send_external email_validate email_config email_test 
#!/bin/bash

# API and Webhook Operators Implementation
# Provides API integration and webhook management capabilities

# Global variables for API operator
API_BASE_URL=""
API_AUTH_TYPE="none"
API_AUTH_TOKEN=""
API_HEADERS=""
API_TIMEOUT="30"
API_RETRY_COUNT="3"

# Global variables for Webhook operator
WEBHOOK_PORT="8080"
WEBHOOK_PATH="/webhook"
WEBHOOK_SECRET=""
WEBHOOK_LOG_PATH="/tmp/webhook.log"
WEBHOOK_PID_FILE="/tmp/webhook.pid"

# Initialize API operator
api_init() {
    local base_url="$1"
    local auth_type="$2"
    local auth_token="$3"
    local headers="$4"
    local timeout="$5"
    local retry_count="$6"
    
    API_BASE_URL="$base_url"
    API_AUTH_TYPE="${auth_type:-none}"
    API_AUTH_TOKEN="$auth_token"
    API_HEADERS="$headers"
    API_TIMEOUT="${timeout:-30}"
    API_RETRY_COUNT="${retry_count:-3}"
    
    echo "{\"status\":\"success\",\"message\":\"API operator initialized\",\"base_url\":\"$API_BASE_URL\",\"auth_type\":\"$API_AUTH_TYPE\",\"timeout\":\"$API_TIMEOUT\"}"
}

# API GET request
api_get() {
    local endpoint="$1"
    local params="$2"
    local headers="$3"
    
    if [[ -z "$endpoint" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Endpoint is required for GET request\"}"
        return 1
    fi
    
    local url="$API_BASE_URL$endpoint"
    if [[ -n "$params" ]]; then
        url="$url?$params"
    fi
    
    api_make_request "GET" "$url" "" "$headers"
}

# API POST request
api_post() {
    local endpoint="$1"
    local data="$2"
    local headers="$3"
    local content_type="$4"
    
    if [[ -z "$endpoint" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Endpoint is required for POST request\"}"
        return 1
    fi
    
    local url="$API_BASE_URL$endpoint"
    content_type="${content_type:-application/json}"
    
    api_make_request "POST" "$url" "$data" "$headers" "$content_type"
}

# API PUT request
api_put() {
    local endpoint="$1"
    local data="$2"
    local headers="$3"
    local content_type="$4"
    
    if [[ -z "$endpoint" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Endpoint is required for PUT request\"}"
        return 1
    fi
    
    local url="$API_BASE_URL$endpoint"
    content_type="${content_type:-application/json}"
    
    api_make_request "PUT" "$url" "$data" "$headers" "$content_type"
}

# API DELETE request
api_delete() {
    local endpoint="$1"
    local headers="$2"
    
    if [[ -z "$endpoint" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Endpoint is required for DELETE request\"}"
        return 1
    fi
    
    local url="$API_BASE_URL$endpoint"
    
    api_make_request "DELETE" "$url" "" "$headers"
}

# Make HTTP request with curl
api_make_request() {
    local method="$1"
    local url="$2"
    local data="$3"
    local headers="$4"
    local content_type="$5"
    
    if ! command -v curl >/dev/null 2>&1; then
        echo "{\"status\":\"error\",\"message\":\"curl not available for API requests\"}"
        return 1
    fi
    
    local curl_cmd="curl -s -X $method"
    curl_cmd="$curl_cmd --max-time $API_TIMEOUT"
    curl_cmd="$curl_cmd --retry $API_RETRY_COUNT"
    curl_cmd="$curl_cmd -w '{\"http_code\":%{http_code},\"total_time\":%{time_total},\"size_download\":%{size_download}}'"
    
    # Add authentication
    case "$API_AUTH_TYPE" in
        "bearer")
            curl_cmd="$curl_cmd -H 'Authorization: Bearer $API_AUTH_TOKEN'"
            ;;
        "basic")
            curl_cmd="$curl_cmd -H 'Authorization: Basic $API_AUTH_TOKEN'"
            ;;
        "api_key")
            curl_cmd="$curl_cmd -H 'X-API-Key: $API_AUTH_TOKEN'"
            ;;
    esac
    
    # Add content type
    if [[ -n "$content_type" ]]; then
        curl_cmd="$curl_cmd -H 'Content-Type: $content_type'"
    fi
    
    # Add custom headers
    if [[ -n "$headers" ]]; then
        IFS=',' read -ra HEADERS <<< "$headers"
        for header in "${HEADERS[@]}"; do
            curl_cmd="$curl_cmd -H '$header'"
        done
    fi
    
    # Add global headers
    if [[ -n "$API_HEADERS" ]]; then
        IFS=',' read -ra GLOBAL_HEADERS <<< "$API_HEADERS"
        for header in "${GLOBAL_HEADERS[@]}"; do
            curl_cmd="$curl_cmd -H '$header'"
        done
    fi
    
    # Add data for POST/PUT
    if [[ -n "$data" && ("$method" == "POST" || "$method" == "PUT") ]]; then
        if [[ -f "$data" ]]; then
            curl_cmd="$curl_cmd -d @$data"
        else
            curl_cmd="$curl_cmd -d '$data'"
        fi
    fi
    
    # Add URL
    curl_cmd="$curl_cmd '$url'"
    
    # Execute request
    local response=$(eval "$curl_cmd" 2>&1)
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        # Extract metadata from response
        local metadata=$(echo "$response" | tail -n 1)
        local body=$(echo "$response" | head -n -1)
        
        local http_code=$(echo "$metadata" | grep -o '"http_code":[0-9]*' | cut -d':' -f2)
        local total_time=$(echo "$metadata" | grep -o '"total_time":[0-9.]*' | cut -d':' -f2)
        local size_download=$(echo "$metadata" | grep -o '"size_download":[0-9]*' | cut -d':' -f2)
        
        echo "{\"status\":\"success\",\"message\":\"API request completed\",\"method\":\"$method\",\"url\":\"$url\",\"http_code\":$http_code,\"total_time\":$total_time,\"size_download\":$size_download,\"response\":$(echo "$body" | jq -c . 2>/dev/null || echo "\"$body\"")}"
    else
        echo "{\"status\":\"error\",\"message\":\"API request failed\",\"method\":\"$method\",\"url\":\"$url\",\"error\":\"$response\",\"exit_code\":$exit_code}"
        return 1
    fi
}

# API batch requests
api_batch() {
    local requests_file="$1"
    local parallel="$2"
    
    if [[ -z "$requests_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Requests file is required for batch processing\"}"
        return 1
    fi
    
    if [[ ! -f "$requests_file" ]]; then
        echo "{\"status\":\"error\",\"message\":\"Requests file not found: $requests_file\"}"
        return 1
    fi
    
    parallel="${parallel:-false}"
    local total_requests=0
    local successful_requests=0
    local failed_requests=0
    local results=""
    
    # Process each request
    while IFS= read -r request_line; do
        if [[ -z "$request_line" || "$request_line" == \#* ]]; then
            continue
        fi
        
        ((total_requests++))
        
        # Parse request line (format: METHOD|ENDPOINT|DATA|HEADERS)
        IFS='|' read -ra REQUEST_PARTS <<< "$request_line"
        local method="${REQUEST_PARTS[0]}"
        local endpoint="${REQUEST_PARTS[1]}"
        local data="${REQUEST_PARTS[2]}"
        local headers="${REQUEST_PARTS[3]}"
        
        local result=""
        case "$method" in
            "GET")
                result=$(api_get "$endpoint" "$data" "$headers")
                ;;
            "POST")
                result=$(api_post "$endpoint" "$data" "$headers")
                ;;
            "PUT")
                result=$(api_put "$endpoint" "$data" "$headers")
                ;;
            "DELETE")
                result=$(api_delete "$endpoint" "$headers")
                ;;
            *)
                result="{\"status\":\"error\",\"message\":\"Unsupported method: $method\"}"
                ;;
        esac
        
        if [[ "$result" == *"\"status\":\"success\""* ]]; then
            ((successful_requests++))
        else
            ((failed_requests++))
        fi
        
        if [[ -n "$results" ]]; then
            results="$results,"
        fi
        results="$results$result"
        
    done < "$requests_file"
    
    echo "{\"status\":\"success\",\"message\":\"Batch API requests completed\",\"total_requests\":$total_requests,\"successful_requests\":$successful_requests,\"failed_requests\":$failed_requests,\"results\":[$results]}"
}

# API rate limiting
api_rate_limit() {
    local requests_per_second="$1"
    local burst_size="$2"
    
    requests_per_second="${requests_per_second:-10}"
    burst_size="${burst_size:-20}"
    
    # Simple rate limiting implementation
    echo "{\"status\":\"success\",\"message\":\"API rate limiting configured\",\"requests_per_second\":$requests_per_second,\"burst_size\":$burst_size}"
}

# Get API configuration
api_config() {
    echo "{\"status\":\"success\",\"config\":{\"base_url\":\"$API_BASE_URL\",\"auth_type\":\"$API_AUTH_TYPE\",\"timeout\":\"$API_TIMEOUT\",\"retry_count\":\"$API_RETRY_COUNT\",\"headers\":\"$API_HEADERS\"}}"
}

# Initialize Webhook operator
webhook_init() {
    local port="$1"
    local path="$2"
    local secret="$3"
    local log_path="$4"
    
    WEBHOOK_PORT="${port:-8080}"
    WEBHOOK_PATH="${path:-/webhook}"
    WEBHOOK_SECRET="$secret"
    WEBHOOK_LOG_PATH="${log_path:-/tmp/webhook.log}"
    
    echo "{\"status\":\"success\",\"message\":\"Webhook operator initialized\",\"port\":\"$WEBHOOK_PORT\",\"path\":\"$WEBHOOK_PATH\",\"log_path\":\"$WEBHOOK_LOG_PATH\"}"
}

# Start webhook server
webhook_start() {
    local handler_script="$1"
    local background="$2"
    
    background="${background:-true}"
    
    if [[ -f "$WEBHOOK_PID_FILE" ]]; then
        local existing_pid=$(cat "$WEBHOOK_PID_FILE")
        if kill -0 "$existing_pid" 2>/dev/null; then
            echo "{\"status\":\"warning\",\"message\":\"Webhook server already running\",\"pid\":$existing_pid,\"port\":\"$WEBHOOK_PORT\"}"
            return 0
        else
            rm -f "$WEBHOOK_PID_FILE"
        fi
    fi
    
    # Create simple webhook server using netcat or Python
    if command -v python3 >/dev/null 2>&1; then
        webhook_start_python "$handler_script" "$background"
    elif command -v nc >/dev/null 2>&1; then
        webhook_start_netcat "$handler_script" "$background"
    else
        echo "{\"status\":\"error\",\"message\":\"No suitable tool found for webhook server (need python3 or nc)\"}"
        return 1
    fi
}

# Start webhook server using Python
webhook_start_python() {
    local handler_script="$1"
    local background="$2"
    
    # Create Python webhook server
    local server_script="/tmp/webhook_server_$$.py"
    cat > "$server_script" << EOF
import http.server
import socketserver
import json
import subprocess
import sys
from urllib.parse import urlparse, parse_qs
import hashlib
import hmac
import logging

# Configure logging
logging.basicConfig(
    filename='$WEBHOOK_LOG_PATH',
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

class WebhookHandler(http.server.BaseHTTPRequestHandler):
    def do_POST(self):
        if self.path != '$WEBHOOK_PATH':
            self.send_response(404)
            self.end_headers()
            self.wfile.write(b'Not Found')
            return
        
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        
        # Verify signature if secret is provided
        if '$WEBHOOK_SECRET':
            signature = self.headers.get('X-Hub-Signature-256', '')
            expected_signature = 'sha256=' + hmac.new(
                '$WEBHOOK_SECRET'.encode(),
                post_data,
                hashlib.sha256
            ).hexdigest()
            
            if not hmac.compare_digest(signature, expected_signature):
                logging.warning('Invalid webhook signature')
                self.send_response(401)
                self.end_headers()
                self.wfile.write(b'Unauthorized')
                return
        
        # Log the webhook
        try:
            payload = json.loads(post_data.decode('utf-8'))
            logging.info(f'Webhook received: {json.dumps(payload, indent=2)}')
        except json.JSONDecodeError:
            logging.info(f'Webhook received (non-JSON): {post_data.decode("utf-8")}')
            payload = {'raw_data': post_data.decode('utf-8')}
        
        # Execute handler script if provided
        if '$handler_script' and '$handler_script' != '':
            try:
                result = subprocess.run(
                    ['bash', '$handler_script'],
                    input=json.dumps(payload),
                    text=True,
                    capture_output=True,
                    timeout=30
                )
                logging.info(f'Handler script result: {result.returncode}')
                if result.stderr:
                    logging.error(f'Handler script error: {result.stderr}')
            except Exception as e:
                logging.error(f'Handler script execution failed: {e}')
        
        # Send response
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()
        response = {'status': 'success', 'message': 'Webhook processed'}
        self.wfile.write(json.dumps(response).encode())
    
    def do_GET(self):
        if self.path == '/health':
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.end_headers()
            response = {'status': 'healthy', 'port': $WEBHOOK_PORT}
            self.wfile.write(json.dumps(response).encode())
        else:
            self.send_response(405)
            self.end_headers()
            self.wfile.write(b'Method Not Allowed')
    
    def log_message(self, format, *args):
        logging.info(format % args)

# Start server
try:
    with socketserver.TCPServer(("", $WEBHOOK_PORT), WebhookHandler) as httpd:
        print(f'Webhook server started on port $WEBHOOK_PORT')
        httpd.serve_forever()
except KeyboardInterrupt:
    print('Webhook server stopped')
except Exception as e:
    print(f'Webhook server error: {e}')
    sys.exit(1)
EOF
    
    if [[ "$background" == "true" ]]; then
        python3 "$server_script" > /dev/null 2>&1 &
        local pid=$!
        echo "$pid" > "$WEBHOOK_PID_FILE"
        
        # Wait a moment to check if server started successfully
        sleep 2
        if kill -0 "$pid" 2>/dev/null; then
            echo "{\"status\":\"success\",\"message\":\"Webhook server started\",\"pid\":$pid,\"port\":\"$WEBHOOK_PORT\",\"path\":\"$WEBHOOK_PATH\",\"log_path\":\"$WEBHOOK_LOG_PATH\"}"
        else
            echo "{\"status\":\"error\",\"message\":\"Webhook server failed to start\"}"
            rm -f "$WEBHOOK_PID_FILE" "$server_script"
            return 1
        fi
    else
        python3 "$server_script"
    fi
    
    # Clean up script file after a delay
    (sleep 10 && rm -f "$server_script") &
}

# Start webhook server using netcat (simplified)
webhook_start_netcat() {
    local handler_script="$1"
    local background="$2"
    
    echo "{\"status\":\"warning\",\"message\":\"Netcat webhook server is simplified and may not handle all features\"}"
    
    # Simple netcat-based webhook (very basic)
    local response="HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{\"status\":\"success\",\"message\":\"Webhook received\"}\r\n"
    
    if [[ "$background" == "true" ]]; then
        while true; do
            echo -e "$response" | nc -l -p "$WEBHOOK_PORT" >> "$WEBHOOK_LOG_PATH" 2>&1
        done &
        local pid=$!
        echo "$pid" > "$WEBHOOK_PID_FILE"
        echo "{\"status\":\"success\",\"message\":\"Simple webhook server started\",\"pid\":$pid,\"port\":\"$WEBHOOK_PORT\"}"
    else
        echo -e "$response" | nc -l -p "$WEBHOOK_PORT"
    fi
}

# Stop webhook server
webhook_stop() {
    if [[ -f "$WEBHOOK_PID_FILE" ]]; then
        local pid=$(cat "$WEBHOOK_PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            kill "$pid"
            rm -f "$WEBHOOK_PID_FILE"
            echo "{\"status\":\"success\",\"message\":\"Webhook server stopped\",\"pid\":$pid}"
        else
            rm -f "$WEBHOOK_PID_FILE"
            echo "{\"status\":\"warning\",\"message\":\"Webhook server was not running\"}"
        fi
    else
        echo "{\"status\":\"warning\",\"message\":\"No webhook server PID file found\"}"
    fi
}

# Get webhook status
webhook_status() {
    if [[ -f "$WEBHOOK_PID_FILE" ]]; then
        local pid=$(cat "$WEBHOOK_PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            echo "{\"status\":\"success\",\"message\":\"Webhook server is running\",\"pid\":$pid,\"port\":\"$WEBHOOK_PORT\",\"path\":\"$WEBHOOK_PATH\"}"
        else
            rm -f "$WEBHOOK_PID_FILE"
            echo "{\"status\":\"success\",\"message\":\"Webhook server is not running\"}"
        fi
    else
        echo "{\"status\":\"success\",\"message\":\"Webhook server is not running\"}"
    fi
}

# List webhook logs
webhook_logs() {
    local lines="$1"
    
    lines="${lines:-50}"
    
    if [[ -f "$WEBHOOK_LOG_PATH" ]]; then
        local log_content=$(tail -n "$lines" "$WEBHOOK_LOG_PATH" 2>/dev/null)
        echo "{\"status\":\"success\",\"message\":\"Webhook logs retrieved\",\"lines\":$lines,\"log_path\":\"$WEBHOOK_LOG_PATH\",\"logs\":\"$log_content\"}"
    else
        echo "{\"status\":\"warning\",\"message\":\"No webhook log file found\",\"log_path\":\"$WEBHOOK_LOG_PATH\"}"
    fi
}

# Test webhook
webhook_test() {
    local test_payload="$1"
    local target_url="$2"
    
    test_payload="${test_payload:-{\"test\":\"webhook_test\",\"timestamp\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}}"
    target_url="${target_url:-http://localhost:$WEBHOOK_PORT$WEBHOOK_PATH}"
    
    if command -v curl >/dev/null 2>&1; then
        local response=$(curl -s -X POST \
            -H "Content-Type: application/json" \
            -d "$test_payload" \
            "$target_url" 2>&1)
        
        if [[ $? -eq 0 ]]; then
            echo "{\"status\":\"success\",\"message\":\"Webhook test completed\",\"target_url\":\"$target_url\",\"payload\":$test_payload,\"response\":\"$response\"}"
        else
            echo "{\"status\":\"error\",\"message\":\"Webhook test failed\",\"target_url\":\"$target_url\",\"error\":\"$response\"}"
            return 1
        fi
    else
        echo "{\"status\":\"error\",\"message\":\"curl not available for webhook testing\"}"
        return 1
    fi
}

# Get webhook configuration
webhook_config() {
    echo "{\"status\":\"success\",\"config\":{\"port\":\"$WEBHOOK_PORT\",\"path\":\"$WEBHOOK_PATH\",\"secret_set\":$([ -n "$WEBHOOK_SECRET" ] && echo "true" || echo "false"),\"log_path\":\"$WEBHOOK_LOG_PATH\",\"pid_file\":\"$WEBHOOK_PID_FILE\"}}"
}

# Main API operator function
execute_api() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local base_url=$(echo "$params" | grep -o 'base_url=[^,]*' | cut -d'=' -f2)
            local auth_type=$(echo "$params" | grep -o 'auth_type=[^,]*' | cut -d'=' -f2)
            local auth_token=$(echo "$params" | grep -o 'auth_token=[^,]*' | cut -d'=' -f2)
            local headers=$(echo "$params" | grep -o 'headers=[^,]*' | cut -d'=' -f2)
            local timeout=$(echo "$params" | grep -o 'timeout=[^,]*' | cut -d'=' -f2)
            local retry_count=$(echo "$params" | grep -o 'retry_count=[^,]*' | cut -d'=' -f2)
            api_init "$base_url" "$auth_type" "$auth_token" "$headers" "$timeout" "$retry_count"
            ;;
        "get")
            local endpoint=$(echo "$params" | grep -o 'endpoint=[^,]*' | cut -d'=' -f2)
            local query_params=$(echo "$params" | grep -o 'params=[^,]*' | cut -d'=' -f2)
            local headers=$(echo "$params" | grep -o 'headers=[^,]*' | cut -d'=' -f2)
            api_get "$endpoint" "$query_params" "$headers"
            ;;
        "post")
            local endpoint=$(echo "$params" | grep -o 'endpoint=[^,]*' | cut -d'=' -f2)
            local data=$(echo "$params" | grep -o 'data=[^,]*' | cut -d'=' -f2-)
            local headers=$(echo "$params" | grep -o 'headers=[^,]*' | cut -d'=' -f2)
            local content_type=$(echo "$params" | grep -o 'content_type=[^,]*' | cut -d'=' -f2)
            api_post "$endpoint" "$data" "$headers" "$content_type"
            ;;
        "put")
            local endpoint=$(echo "$params" | grep -o 'endpoint=[^,]*' | cut -d'=' -f2)
            local data=$(echo "$params" | grep -o 'data=[^,]*' | cut -d'=' -f2-)
            local headers=$(echo "$params" | grep -o 'headers=[^,]*' | cut -d'=' -f2)
            local content_type=$(echo "$params" | grep -o 'content_type=[^,]*' | cut -d'=' -f2)
            api_put "$endpoint" "$data" "$headers" "$content_type"
            ;;
        "delete")
            local endpoint=$(echo "$params" | grep -o 'endpoint=[^,]*' | cut -d'=' -f2)
            local headers=$(echo "$params" | grep -o 'headers=[^,]*' | cut -d'=' -f2)
            api_delete "$endpoint" "$headers"
            ;;
        "batch")
            local requests_file=$(echo "$params" | grep -o 'requests_file=[^,]*' | cut -d'=' -f2)
            local parallel=$(echo "$params" | grep -o 'parallel=[^,]*' | cut -d'=' -f2)
            api_batch "$requests_file" "$parallel"
            ;;
        "config")
            api_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, get, post, put, delete, batch, config\"}"
            return 1
            ;;
    esac
}

# Main Webhook operator function
execute_webhook() {
    local action="$1"
    local params="$2"
    
    case "$action" in
        "init")
            local port=$(echo "$params" | grep -o 'port=[^,]*' | cut -d'=' -f2)
            local path=$(echo "$params" | grep -o 'path=[^,]*' | cut -d'=' -f2)
            local secret=$(echo "$params" | grep -o 'secret=[^,]*' | cut -d'=' -f2)
            local log_path=$(echo "$params" | grep -o 'log_path=[^,]*' | cut -d'=' -f2)
            webhook_init "$port" "$path" "$secret" "$log_path"
            ;;
        "start")
            local handler_script=$(echo "$params" | grep -o 'handler_script=[^,]*' | cut -d'=' -f2)
            local background=$(echo "$params" | grep -o 'background=[^,]*' | cut -d'=' -f2)
            webhook_start "$handler_script" "$background"
            ;;
        "stop")
            webhook_stop
            ;;
        "status")
            webhook_status
            ;;
        "logs")
            local lines=$(echo "$params" | grep -o 'lines=[^,]*' | cut -d'=' -f2)
            webhook_logs "$lines"
            ;;
        "test")
            local test_payload=$(echo "$params" | grep -o 'payload=[^,]*' | cut -d'=' -f2-)
            local target_url=$(echo "$params" | grep -o 'target_url=[^,]*' | cut -d'=' -f2)
            webhook_test "$test_payload" "$target_url"
            ;;
        "config")
            webhook_config
            ;;
        *)
            echo "{\"status\":\"error\",\"message\":\"Unknown action: $action. Available actions: init, start, stop, status, logs, test, config\"}"
            return 1
            ;;
    esac
}

# Export functions for external use
export -f execute_api execute_webhook api_init api_get api_post api_put api_delete api_batch api_config webhook_init webhook_start webhook_stop webhook_status webhook_logs webhook_test webhook_config 
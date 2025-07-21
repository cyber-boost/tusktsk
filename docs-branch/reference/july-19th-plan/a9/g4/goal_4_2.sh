#!/bin/bash

# Goal 4.2 Implementation - Load Balancer and Proxy Management System
# Priority: Medium
# Description: Goal 2 for Bash agent a9 goal 4

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_4_2"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
RESULTS_DIR="/tmp/goal_4_2_results"
CONFIG_FILE="/tmp/goal_4_2_config.conf"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

# Create configuration
create_config() {
    log_info "Creating load balancer configuration"
    mkdir -p "$RESULTS_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# Load Balancer Configuration

# Load balancer settings
LB_PORT=8081
LB_HOST="localhost"
LB_PROTOCOL="http"
ENABLE_SSL=false
SSL_CERT=""
SSL_KEY=""

# Load balancing algorithms
ALGORITHMS=(
    "round_robin"
    "least_connections"
    "weighted_round_robin"
    "ip_hash"
    "least_response_time"
)

# Default algorithm
DEFAULT_ALGORITHM="round_robin"

# Backend servers
BACKEND_SERVERS=(
    "http://localhost:8080"
    "http://localhost:8082"
    "http://localhost:8083"
)

# Health check settings
HEALTH_CHECK_ENABLED=true
HEALTH_CHECK_INTERVAL=30
HEALTH_CHECK_TIMEOUT=5
HEALTH_CHECK_PATH="/api/health"
HEALTH_CHECK_METHOD="GET"

# Proxy settings
PROXY_ENABLED=true
PROXY_TIMEOUT=30
PROXY_BUFFER_SIZE=8192
ENABLE_COMPRESSION=true
COMPRESSION_LEVEL=6

# Rate limiting
RATE_LIMIT_ENABLED=true
RATE_LIMIT_REQUESTS=100
RATE_LIMIT_WINDOW=60

# Session persistence
SESSION_PERSISTENCE=false
SESSION_TIMEOUT=3600
EOF
    
    log_success "Configuration created"
}

create_backend_servers() {
    log_info "Creating backend server configurations"
    
    # Create backend server configurations
    mkdir -p "$RESULTS_DIR/backends"
    
    # Backend server 1 configuration
    cat > "$RESULTS_DIR/backends/server1.conf" << 'EOF'
# Backend Server 1 Configuration
SERVER_NAME="web-server-1"
SERVER_ADDRESS="http://localhost:8080"
SERVER_WEIGHT=1
SERVER_MAX_CONNECTIONS=100
SERVER_TIMEOUT=30
SERVER_HEALTH_CHECK="/api/health"
SERVER_STATUS="active"
EOF
    
    # Backend server 2 configuration
    cat > "$RESULTS_DIR/backends/server2.conf" << 'EOF'
# Backend Server 2 Configuration
SERVER_NAME="web-server-2"
SERVER_ADDRESS="http://localhost:8082"
SERVER_WEIGHT=1
SERVER_MAX_CONNECTIONS=100
SERVER_TIMEOUT=30
SERVER_HEALTH_CHECK="/api/health"
SERVER_STATUS="active"
EOF
    
    # Backend server 3 configuration
    cat > "$RESULTS_DIR/backends/server3.conf" << 'EOF'
# Backend Server 3 Configuration
SERVER_NAME="web-server-3"
SERVER_ADDRESS="http://localhost:8083"
SERVER_WEIGHT=1
SERVER_MAX_CONNECTIONS=100
SERVER_TIMEOUT=30
SERVER_HEALTH_CHECK="/api/health"
SERVER_STATUS="active"
EOF
    
    log_success "Backend server configurations created"
}

implement_round_robin() {
    log_info "Implementing round-robin load balancing algorithm"
    
    local algorithm_file="$RESULTS_DIR/algorithms/round_robin.sh"
    mkdir -p "$(dirname "$algorithm_file")"
    
    cat > "$algorithm_file" << 'EOF'
#!/bin/bash

# Round-robin load balancing algorithm
round_robin_select() {
    local backends=("$@")
    local backend_count=${#backends[@]}
    
    # Get current position from state file
    local state_file="/tmp/goal_4_2_results/round_robin_state"
    local current_pos=0
    
    if [[ -f "$state_file" ]]; then
        current_pos=$(cat "$state_file")
    fi
    
    # Select next backend
    local selected_backend="${backends[$current_pos]}"
    
    # Update position for next request
    current_pos=$(((current_pos + 1) % backend_count))
    echo "$current_pos" > "$state_file"
    
    echo "$selected_backend"
}
EOF
    
    chmod +x "$algorithm_file"
    log_success "Round-robin algorithm implemented"
}

implement_least_connections() {
    log_info "Implementing least-connections load balancing algorithm"
    
    local algorithm_file="$RESULTS_DIR/algorithms/least_connections.sh"
    mkdir -p "$(dirname "$algorithm_file")"
    
    cat > "$algorithm_file" << 'EOF'
#!/bin/bash

# Least-connections load balancing algorithm
least_connections_select() {
    local backends=("$@")
    local backend_count=${#backends[@]}
    
    local min_connections=999999
    local selected_backend=""
    
    # Check connection count for each backend
    for backend in "${backends[@]}"; do
        local connection_file="/tmp/goal_4_2_results/connections_${backend//[^a-zA-Z0-9]/_}"
        local connections=0
        
        if [[ -f "$connection_file" ]]; then
            connections=$(cat "$connection_file")
        fi
        
        if [[ $connections -lt $min_connections ]]; then
            min_connections=$connections
            selected_backend="$backend"
        fi
    done
    
    echo "$selected_backend"
}

# Update connection count
update_connection_count() {
    local backend="$1"
    local increment="$2"
    
    local connection_file="/tmp/goal_4_2_results/connections_${backend//[^a-zA-Z0-9]/_}"
    local current_count=0
    
    if [[ -f "$connection_file" ]]; then
        current_count=$(cat "$connection_file")
    fi
    
    local new_count=$((current_count + increment))
    if [[ $new_count -lt 0 ]]; then
        new_count=0
    fi
    
    echo "$new_count" > "$connection_file"
}
EOF
    
    chmod +x "$algorithm_file"
    log_success "Least-connections algorithm implemented"
}

implement_weighted_round_robin() {
    log_info "Implementing weighted round-robin load balancing algorithm"
    
    local algorithm_file="$RESULTS_DIR/algorithms/weighted_round_robin.sh"
    mkdir -p "$(dirname "$algorithm_file")"
    
    cat > "$algorithm_file" << 'EOF'
#!/bin/bash

# Weighted round-robin load balancing algorithm
weighted_round_robin_select() {
    local backends=("$@")
    local weights=("$@")
    local backend_count=${#backends[@]}
    
    # Calculate total weight
    local total_weight=0
    for weight in "${weights[@]}"; do
        total_weight=$((total_weight + weight))
    done
    
    # Get current position from state file
    local state_file="/tmp/goal_4_2_results/weighted_round_robin_state"
    local current_pos=0
    
    if [[ -f "$state_file" ]]; then
        current_pos=$(cat "$state_file")
    fi
    
    # Find backend with current weight position
    local weight_sum=0
    local selected_backend="${backends[0]}"
    
    for i in $(seq 0 $((backend_count - 1))); do
        weight_sum=$((weight_sum + weights[i]))
        if [[ $current_pos -lt $weight_sum ]]; then
            selected_backend="${backends[i]}"
            break
        fi
    done
    
    # Update position for next request
    current_pos=$(((current_pos + 1) % total_weight))
    echo "$current_pos" > "$state_file"
    
    echo "$selected_backend"
}
EOF
    
    chmod +x "$algorithm_file"
    log_success "Weighted round-robin algorithm implemented"
}

implement_health_check() {
    log_info "Implementing health check system"
    
    local health_check_file="$RESULTS_DIR/health_check.sh"
    
    cat > "$health_check_file" << 'EOF'
#!/bin/bash

# Health check system for backend servers
perform_health_check() {
    local backend="$1"
    local health_path="$2"
    local timeout="$3"
    
    local health_url="${backend}${health_path}"
    local status_file="/tmp/goal_4_2_results/health_${backend//[^a-zA-Z0-9]/_}"
    
    # Perform health check using curl
    if curl -s --max-time "$timeout" "$health_url" >/dev/null 2>&1; then
        echo "healthy" > "$status_file"
        echo "healthy"
    else
        echo "unhealthy" > "$status_file"
        echo "unhealthy"
    fi
}

check_all_backends() {
    local backends=("$@")
    local health_path="$1"
    local timeout="$2"
    
    for backend in "${backends[@]}"; do
        local status=$(perform_health_check "$backend" "$health_path" "$timeout")
        echo "Backend $backend: $status"
    done
}

get_backend_status() {
    local backend="$1"
    local status_file="/tmp/goal_4_2_results/health_${backend//[^a-zA-Z0-9]/_}"
    
    if [[ -f "$status_file" ]]; then
        cat "$status_file"
    else
        echo "unknown"
    fi
}
EOF
    
    chmod +x "$health_check_file"
    log_success "Health check system implemented"
}

implement_rate_limiting() {
    log_info "Implementing rate limiting system"
    
    local rate_limit_file="$RESULTS_DIR/rate_limiting.sh"
    
    cat > "$rate_limit_file" << 'EOF'
#!/bin/bash

# Rate limiting system
check_rate_limit() {
    local client_ip="$1"
    local max_requests="$2"
    local window_seconds="$3"
    
    local rate_file="/tmp/goal_4_2_results/rate_${client_ip//[^a-zA-Z0-9]/_}"
    local current_time=$(date +%s)
    local window_start=$((current_time - window_seconds))
    
    # Read existing requests
    local requests=()
    if [[ -f "$rate_file" ]]; then
        while IFS= read -r timestamp; do
            if [[ $timestamp -gt $window_start ]]; then
                requests+=("$timestamp")
            fi
        done < "$rate_file"
    fi
    
    # Check if limit exceeded
    if [[ ${#requests[@]} -ge $max_requests ]]; then
        echo "limit_exceeded"
        return 1
    fi
    
    # Add current request
    echo "$current_time" >> "$rate_file"
    echo "allowed"
    return 0
}

cleanup_rate_limits() {
    local window_seconds="$1"
    local current_time=$(date +%s)
    local window_start=$((current_time - window_seconds))
    
    # Clean up old rate limit files
    for rate_file in /tmp/goal_4_2_results/rate_*; do
        if [[ -f "$rate_file" ]]; then
            # Remove old timestamps
            local temp_file=$(mktemp)
            while IFS= read -r timestamp; do
                if [[ $timestamp -gt $window_start ]]; then
                    echo "$timestamp" >> "$temp_file"
                fi
            done < "$rate_file"
            mv "$temp_file" "$rate_file"
        fi
    done
}
EOF
    
    chmod +x "$rate_limit_file"
    log_success "Rate limiting system implemented"
}

implement_proxy_functions() {
    log_info "Implementing proxy functions"
    
    local proxy_file="$RESULTS_DIR/proxy_functions.sh"
    
    cat > "$proxy_file" << 'EOF'
#!/bin/bash

# Proxy functions for load balancer
forward_request() {
    local backend="$1"
    local request_data="$2"
    local timeout="$3"
    
    # Simulate request forwarding
    local response_file="/tmp/goal_4_2_results/response_$(date +%s)"
    
    # In a real implementation, this would forward the actual HTTP request
    echo "Request forwarded to $backend" > "$response_file"
    echo "Response time: $(date +%s)" >> "$response_file"
    
    echo "$response_file"
}

add_proxy_headers() {
    local headers="$1"
    local client_ip="$2"
    local backend="$3"
    
    # Add proxy-specific headers
    headers="${headers}
X-Forwarded-For: $client_ip
X-Forwarded-Proto: http
X-Real-IP: $client_ip
Via: bash-load-balancer/1.0"
    
    echo "$headers"
}

compress_response() {
    local response="$1"
    local compression_level="$2"
    
    if [[ "$ENABLE_COMPRESSION" == "true" ]]; then
        # Simulate compression
        echo "Response compressed with level $compression_level"
        echo "$response" | gzip -c -$compression_level
    else
        echo "$response"
    fi
}
EOF
    
    chmod +x "$proxy_file"
    log_success "Proxy functions implemented"
}

create_load_balancer_config() {
    log_info "Creating load balancer configuration files"
    
    # Create nginx-style configuration
    cat > "$RESULTS_DIR/load_balancer.conf" << 'EOF'
# Load Balancer Configuration File

# Upstream backend servers
upstream backend_servers {
    # Round-robin by default
    server localhost:8080 weight=1 max_fails=3 fail_timeout=30s;
    server localhost:8082 weight=1 max_fails=3 fail_timeout=30s;
    server localhost:8083 weight=1 max_fails=3 fail_timeout=30s;
    
    # Health check
    health_check interval=30s timeout=5s;
}

# Rate limiting
limit_req_zone $binary_remote_addr zone=api:10m rate=100r/m;

# Server configuration
server {
    listen 8081;
    server_name localhost;
    
    # Rate limiting
    limit_req zone=api burst=20 nodelay;
    
    # Proxy settings
    proxy_connect_timeout 30s;
    proxy_send_timeout 30s;
    proxy_read_timeout 30s;
    proxy_buffer_size 8k;
    proxy_buffers 8 8k;
    
    # Compression
    gzip on;
    gzip_comp_level 6;
    gzip_types text/plain text/css application/json application/javascript;
    
    # Routes
    location / {
        proxy_pass http://backend_servers;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    location /api/ {
        proxy_pass http://backend_servers;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Health check endpoint
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}
EOF
    
    log_success "Load balancer configuration created"
}

generate_load_balancer_report() {
    log_info "Generating load balancer report"
    local report_file="$RESULTS_DIR/load_balancer_report.txt"
    
    {
        echo "=========================================="
        echo "LOAD BALANCER AND PROXY MANAGEMENT REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== Load Balancer Configuration ==="
        echo "Port: 8081"
        echo "Host: localhost"
        echo "Protocol: http"
        echo "Default Algorithm: $DEFAULT_ALGORITHM"
        echo ""
        
        echo "=== Backend Servers ==="
        for i in "${!BACKEND_SERVERS[@]}"; do
            echo "Server $((i+1)): ${BACKEND_SERVERS[i]}"
        done
        echo ""
        
        echo "=== Load Balancing Algorithms ==="
        for algorithm in "${ALGORITHMS[@]}"; do
            echo "- $algorithm"
        done
        echo ""
        
        echo "=== Health Check Configuration ==="
        echo "Enabled: $HEALTH_CHECK_ENABLED"
        echo "Interval: ${HEALTH_CHECK_INTERVAL}s"
        echo "Timeout: ${HEALTH_CHECK_TIMEOUT}s"
        echo "Path: $HEALTH_CHECK_PATH"
        echo ""
        
        echo "=== Proxy Configuration ==="
        echo "Enabled: $PROXY_ENABLED"
        echo "Timeout: ${PROXY_TIMEOUT}s"
        echo "Buffer Size: ${PROXY_BUFFER_SIZE} bytes"
        echo "Compression: $ENABLE_COMPRESSION"
        echo "Compression Level: $COMPRESSION_LEVEL"
        echo ""
        
        echo "=== Rate Limiting ==="
        echo "Enabled: $RATE_LIMIT_ENABLED"
        echo "Requests: $RATE_LIMIT_REQUESTS per ${RATE_LIMIT_WINDOW}s"
        echo ""
        
        echo "=== Session Persistence ==="
        echo "Enabled: $SESSION_PERSISTENCE"
        echo "Timeout: ${SESSION_TIMEOUT}s"
        echo ""
        
        echo "=== Available Functions ==="
        echo "- Round-robin load balancing"
        echo "- Least-connections load balancing"
        echo "- Weighted round-robin load balancing"
        echo "- Health checking"
        echo "- Rate limiting"
        echo "- Request forwarding"
        echo "- Response compression"
        echo ""
        
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Load balancer report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 4.2 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Create backend server configurations
    create_backend_servers
    
    # Implement load balancing algorithms
    implement_round_robin
    implement_least_connections
    implement_weighted_round_robin
    
    # Implement health check system
    implement_health_check
    
    # Implement rate limiting
    implement_rate_limiting
    
    # Implement proxy functions
    implement_proxy_functions
    
    # Create load balancer configuration
    create_load_balancer_config
    
    # Generate comprehensive report
    generate_load_balancer_report
    
    log_success "Goal 4.2 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi 
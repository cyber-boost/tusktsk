#!/bin/bash

# Goal 2.1 Implementation - Network Monitoring and Connectivity Testing System
# Priority: High
# Description: Goal 1 for Bash agent a9 goal 2

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_2_1"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
LOCK_FILE="/tmp/${SCRIPT_NAME}.lock"
RESULTS_DIR="/tmp/goal_2_1_results"
CONFIG_FILE="/tmp/goal_2_1_config.conf"

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

# File locking mechanism
acquire_lock() {
    if [[ -f "$LOCK_FILE" ]]; then
        local lock_pid=$(cat "$LOCK_FILE" 2>/dev/null || echo "")
        if [[ -n "$lock_pid" ]] && kill -0 "$lock_pid" 2>/dev/null; then
            log_error "Script is already running with PID $lock_pid"
            exit 1
        else
            log_warning "Removing stale lock file"
            rm -f "$LOCK_FILE"
        fi
    fi
    echo $$ > "$LOCK_FILE"
    log_info "Lock acquired"
}

release_lock() {
    rm -f "$LOCK_FILE"
    log_info "Lock released"
}

# Error handling
handle_error() {
    local exit_code=$?
    local line_number=$1
    log_error "Error occurred in line $line_number (exit code: $exit_code)"
    release_lock
    exit "$exit_code"
}

# Set up error handling
trap 'handle_error $LINENO' ERR
trap 'release_lock' EXIT

# Network testing functions
create_config() {
    log_info "Creating network monitoring configuration"
    mkdir -p "$RESULTS_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# Network Monitoring Configuration
# Test targets for connectivity testing

# Primary DNS servers
PRIMARY_DNS="8.8.8.8"
SECONDARY_DNS="1.1.1.1"

# Test websites
TEST_SITES=(
    "google.com"
    "github.com"
    "stackoverflow.com"
    "wikipedia.org"
)

# Network interfaces to monitor
INTERFACES=(
    "lo"
    "eth0"
)

# Ping parameters
PING_COUNT=3
PING_TIMEOUT=5
EOF
    
    log_success "Configuration created"
}

test_dns_resolution() {
    log_info "Testing DNS resolution"
    local dns_results="$RESULTS_DIR/dns_test.txt"
    
    {
        echo "=== DNS Resolution Test Results ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Test primary DNS
        echo "Testing primary DNS ($PRIMARY_DNS):"
        if nslookup google.com "$PRIMARY_DNS" >/dev/null 2>&1; then
            echo "✅ Primary DNS resolution successful"
            echo "✅ Primary DNS resolution successful" >> "$dns_results"
        else
            echo "❌ Primary DNS resolution failed"
            echo "❌ Primary DNS resolution failed" >> "$dns_results"
        fi
        
        # Test secondary DNS
        echo "Testing secondary DNS ($SECONDARY_DNS):"
        if nslookup google.com "$SECONDARY_DNS" >/dev/null 2>&1; then
            echo "✅ Secondary DNS resolution successful"
            echo "✅ Secondary DNS resolution successful" >> "$dns_results"
        else
            echo "❌ Secondary DNS resolution failed"
            echo "❌ Secondary DNS resolution failed" >> "$dns_results"
        fi
        
        echo ""
    } | tee -a "$LOG_FILE"
    
    log_success "DNS resolution test completed"
}

test_connectivity() {
    log_info "Testing network connectivity"
    local connectivity_results="$RESULTS_DIR/connectivity_test.txt"
    
    {
        echo "=== Network Connectivity Test Results ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        local success_count=0
        local total_count=${#TEST_SITES[@]}
        
        for site in "${TEST_SITES[@]}"; do
            echo "Testing connectivity to $site:"
            if ping -c "$PING_COUNT" -W "$PING_TIMEOUT" "$site" >/dev/null 2>&1; then
                echo "✅ $site is reachable"
                echo "✅ $site is reachable" >> "$connectivity_results"
                ((success_count++))
            else
                echo "❌ $site is not reachable"
                echo "❌ $site is not reachable" >> "$connectivity_results"
            fi
        done
        
        echo ""
        echo "Connectivity Summary: $success_count/$total_count sites reachable"
        echo "Connectivity Summary: $success_count/$total_count sites reachable" >> "$connectivity_results"
        
        if [[ $success_count -eq $total_count ]]; then
            log_success "All test sites are reachable"
        elif [[ $success_count -gt 0 ]]; then
            log_warning "Partial connectivity: $success_count/$total_count sites reachable"
        else
            log_error "No connectivity to test sites"
        fi
        
    } | tee -a "$LOG_FILE"
}

monitor_interfaces() {
    log_info "Monitoring network interfaces"
    local interface_results="$RESULTS_DIR/interface_status.txt"
    
    {
        echo "=== Network Interface Status ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        for interface in "${INTERFACES[@]}"; do
            echo "Interface: $interface"
            if ip link show "$interface" >/dev/null 2>&1; then
                local status=$(ip link show "$interface" | grep -o "state [A-Z]*" | cut -d' ' -f2)
                local ip_addr=$(ip addr show "$interface" | grep -o "inet [0-9.]*" | head -1 | cut -d' ' -f2)
                
                echo "  Status: $status"
                echo "  IP Address: ${ip_addr:-'Not assigned'}"
                
                if [[ "$status" == "UP" ]]; then
                    echo "✅ $interface is UP" >> "$interface_results"
                else
                    echo "❌ $interface is DOWN" >> "$interface_results"
                fi
            else
                echo "  Status: Interface not found"
                echo "❌ $interface not found" >> "$interface_results"
            fi
            echo ""
        done
        
    } | tee -a "$LOG_FILE"
    
    log_success "Interface monitoring completed"
}

test_bandwidth() {
    log_info "Testing network bandwidth"
    local bandwidth_results="$RESULTS_DIR/bandwidth_test.txt"
    
    {
        echo "=== Bandwidth Test Results ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Test download speed using curl
        echo "Testing download speed:"
        local start_time=$(date +%s)
        if curl -s --max-time 10 -o /dev/null "https://speed.cloudflare.com/__down?bytes=1048576"; then
            local end_time=$(date +%s)
            local duration=$((end_time - start_time))
            local speed_mbps=$(echo "scale=2; 8 / $duration" | bc -l 2>/dev/null || echo "N/A")
            echo "✅ Download test completed in ${duration}s"
            echo "✅ Download test completed in ${duration}s" >> "$bandwidth_results"
            echo "  Estimated speed: ${speed_mbps} Mbps"
            echo "  Estimated speed: ${speed_mbps} Mbps" >> "$bandwidth_results"
        else
            echo "❌ Download test failed"
            echo "❌ Download test failed" >> "$bandwidth_results"
        fi
        
        echo ""
        
    } | tee -a "$LOG_FILE"
    
    log_success "Bandwidth test completed"
}

generate_report() {
    log_info "Generating comprehensive network report"
    local report_file="$RESULTS_DIR/network_report.txt"
    
    {
        echo "=========================================="
        echo "NETWORK MONITORING REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== DNS Resolution ==="
        if [[ -f "$RESULTS_DIR/dns_test.txt" ]]; then
            cat "$RESULTS_DIR/dns_test.txt"
        fi
        echo ""
        
        echo "=== Connectivity Test ==="
        if [[ -f "$RESULTS_DIR/connectivity_test.txt" ]]; then
            cat "$RESULTS_DIR/connectivity_test.txt"
        fi
        echo ""
        
        echo "=== Interface Status ==="
        if [[ -f "$RESULTS_DIR/interface_status.txt" ]]; then
            cat "$RESULTS_DIR/interface_status.txt"
        fi
        echo ""
        
        echo "=== Bandwidth Test ==="
        if [[ -f "$RESULTS_DIR/bandwidth_test.txt" ]]; then
            cat "$RESULTS_DIR/bandwidth_test.txt"
        fi
        echo ""
        
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Network report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 2.1 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Run network tests
    test_dns_resolution
    test_connectivity
    monitor_interfaces
    test_bandwidth
    
    # Generate comprehensive report
    generate_report
    
    log_success "Goal 2.1 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    acquire_lock
    main "$@"
fi 
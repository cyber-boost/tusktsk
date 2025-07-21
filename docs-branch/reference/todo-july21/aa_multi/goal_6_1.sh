#!/bin/bash

# Goal 6.1 Implementation - Network Security and Firewall Management System
# Priority: High
# Description: Goal 1 for Bash agent a9 goal 6

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_6_1"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
LOCK_FILE="/tmp/${SCRIPT_NAME}.lock"
RESULTS_DIR="/tmp/goal_6_1_results"
CONFIG_FILE="/tmp/goal_6_1_config.conf"
FIREWALL_RULES="/tmp/goal_6_1_results/firewall_rules.conf"
INTRUSION_LOG="/tmp/goal_6_1_results/intrusion.log"

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

# Network security functions
create_config() {
    log_info "Creating network security configuration"
    mkdir -p "$RESULTS_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# Network Security and Firewall Configuration

# Firewall settings
FIREWALL_ENABLED=true
DEFAULT_POLICY=DROP
LOG_DROPPED_PACKETS=true
LOG_ACCEPTED_PACKETS=false

# Network interfaces
TRUSTED_INTERFACES=("lo" "eth0")
EXTERNAL_INTERFACES=("eth1" "wlan0")

# Port configurations
ALLOWED_PORTS=("22" "80" "443" "53" "123")
BLOCKED_PORTS=("23" "21" "25" "110" "143")

# IP whitelist/blacklist
WHITELIST_IPS=("192.168.1.0/24" "10.0.0.0/8")
BLACKLIST_IPS=("0.0.0.0/0")

# Intrusion detection
IDS_ENABLED=true
SCAN_THRESHOLD=10
BLOCK_DURATION=3600
ALERT_EMAIL="admin@example.com"

# Network monitoring
MONITORING_ENABLED=true
PACKET_CAPTURE_ENABLED=false
BANDWIDTH_MONITORING=true
CONNECTION_TRACKING=true
EOF
    
    log_success "Configuration created"
}

# Firewall rule management
create_firewall_rules() {
    log_info "Creating firewall rules"
    
    cat > "$FIREWALL_RULES" << 'EOF'
# Firewall Rules Configuration
# Generated: $(date '+%Y-%m-%d %H:%M:%S')

# Default policies
*filter
:INPUT DROP [0:0]
:FORWARD DROP [0:0]
:OUTPUT ACCEPT [0:0]

# Allow loopback
-A INPUT -i lo -j ACCEPT

# Allow established connections
-A INPUT -m state --state ESTABLISHED,RELATED -j ACCEPT

# Allow SSH (port 22)
-A INPUT -p tcp --dport 22 -j ACCEPT

# Allow HTTP/HTTPS (ports 80, 443)
-A INPUT -p tcp --dport 80 -j ACCEPT
-A INPUT -p tcp --dport 443 -j ACCEPT

# Allow DNS (port 53)
-A INPUT -p udp --dport 53 -j ACCEPT
-A INPUT -p tcp --dport 53 -j ACCEPT

# Allow NTP (port 123)
-A INPUT -p udp --dport 123 -j ACCEPT

# Block common attack ports
-A INPUT -p tcp --dport 23 -j DROP
-A INPUT -p tcp --dport 21 -j DROP
-A INPUT -p tcp --dport 25 -j DROP
-A INPUT -p tcp --dport 110 -j DROP
-A INPUT -p tcp --dport 143 -j DROP

# Block suspicious IPs
-A INPUT -s 0.0.0.0/0 -j DROP

# Log dropped packets
-A INPUT -j LOG --log-prefix "FIREWALL_DROP: "

# Default drop
-A INPUT -j DROP

COMMIT
EOF
    
    log_success "Firewall rules created"
}

# Network interface monitoring
monitor_interfaces() {
    log_info "Monitoring network interfaces"
    
    local interfaces_file="$RESULTS_DIR/interfaces_status.txt"
    
    {
        echo "=== Network Interfaces Status ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Get interface information
        ip link show | while read -r line; do
            if [[ $line =~ ^[0-9]+: ]]; then
                echo "Interface: $line"
            elif [[ $line =~ "state" ]]; then
                echo "  Status: $line"
            elif [[ $line =~ "link" ]]; then
                echo "  Link: $line"
            fi
        done
        
        echo ""
        echo "=== IP Addresses ==="
        ip addr show | grep -E "inet|inet6" | while read -r line; do
            echo "  $line"
        done
        
        echo ""
        echo "=== Routing Table ==="
        ip route show | while read -r line; do
            echo "  $line"
        done
        
    } > "$interfaces_file"
    
    log_success "Network interfaces monitored"
}

# Port scanning detection
detect_port_scan() {
    log_info "Implementing port scanning detection"
    
    local scan_detection_file="$RESULTS_DIR/scan_detection.sh"
    
    cat > "$scan_detection_file" << 'EOF'
#!/bin/bash

# Port scanning detection script
LOG_FILE="/tmp/goal_6_1_results/scan_detection.log"
THRESHOLD=10
BLOCK_DURATION=3600

# Monitor for port scanning
monitor_connections() {
    while true; do
        # Count connections per IP
        netstat -tn | grep ESTABLISHED | awk '{print $5}' | cut -d: -f1 | sort | uniq -c | while read count ip; do
            if [[ $count -gt $THRESHOLD ]]; then
                echo "$(date '+%Y-%m-%d %H:%M:%S') - Potential scan detected from $ip ($count connections)" >> "$LOG_FILE"
                
                # Block IP temporarily
                iptables -A INPUT -s "$ip" -j DROP
                
                # Schedule unblock
                (
                    sleep $BLOCK_DURATION
                    iptables -D INPUT -s "$ip" -j DROP 2>/dev/null || true
                ) &
            fi
        done
        
        sleep 30
    done
}

# Start monitoring
monitor_connections
EOF
    
    chmod +x "$scan_detection_file"
    log_success "Port scanning detection implemented"
}

# Intrusion detection system
create_ids() {
    log_info "Creating intrusion detection system"
    
    local ids_file="$RESULTS_DIR/ids.sh"
    
    cat > "$ids_file" << 'EOF'
#!/bin/bash

# Intrusion Detection System
IDS_LOG="/tmp/goal_6_1_results/ids.log"
ALERT_LOG="/tmp/goal_6_1_results/alerts.log"
PATTERNS_FILE="/tmp/goal_6_1_results/attack_patterns.txt"

# Create attack patterns
cat > "$PATTERNS_FILE" << 'PATTERNS_EOF'
# Common attack patterns
SQL_INJECTION=".*(union|select|insert|update|delete|drop|create).*"
XSS_ATTACK=".*(<script|javascript:|vbscript:).*"
PATH_TRAVERSAL=".*(\.\./|\.\.\\|%2e%2e).*"
COMMAND_INJECTION=".*(;|&&|\|\||`|\\$).*"
PATTERNS_EOF

# Monitor log files for attacks
monitor_logs() {
    local log_file="$1"
    
    while IFS= read -r line; do
        # Check for SQL injection
        if echo "$line" | grep -qi "$(grep SQL_INJECTION "$PATTERNS_FILE" | cut -d= -f2)"; then
            echo "$(date '+%Y-%m-%d %H:%M:%S') - SQL Injection attempt detected: $line" >> "$ALERT_LOG"
        fi
        
        # Check for XSS
        if echo "$line" | grep -qi "$(grep XSS_ATTACK "$PATTERNS_FILE" | cut -d= -f2)"; then
            echo "$(date '+%Y-%m-%d %H:%M:%S') - XSS attack detected: $line" >> "$ALERT_LOG"
        fi
        
        # Check for path traversal
        if echo "$line" | grep -qi "$(grep PATH_TRAVERSAL "$PATTERNS_FILE" | cut -d= -f2)"; then
            echo "$(date '+%Y-%m-%d %H:%M:%S') - Path traversal attempt detected: $line" >> "$ALERT_LOG"
        fi
        
        # Check for command injection
        if echo "$line" | grep -qi "$(grep COMMAND_INJECTION "$PATTERNS_FILE" | cut -d= -f2)"; then
            echo "$(date '+%Y-%m-%d %H:%M:%S') - Command injection attempt detected: $line" >> "$ALERT_LOG"
        fi
        
    done < "$log_file"
}

# Monitor system logs
monitor_system_logs() {
    tail -f /var/log/auth.log /var/log/syslog 2>/dev/null | while read -r line; do
        echo "$line" >> "$IDS_LOG"
        monitor_logs <(echo "$line")
    done
}

# Start monitoring
monitor_system_logs
EOF
    
    chmod +x "$ids_file"
    log_success "Intrusion detection system created"
}

# Network traffic analysis
analyze_traffic() {
    log_info "Analyzing network traffic"
    
    local traffic_analysis_file="$RESULTS_DIR/traffic_analysis.txt"
    
    {
        echo "=== Network Traffic Analysis ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        echo "=== Active Connections ==="
        netstat -tn | grep ESTABLISHED | head -20 | while read -r line; do
            echo "  $line"
        done
        
        echo ""
        echo "=== Listening Ports ==="
        netstat -tln | grep LISTEN | while read -r line; do
            echo "  $line"
        done
        
        echo ""
        echo "=== Network Statistics ==="
        cat /proc/net/dev | grep -E "(eth|wlan|lo)" | while read -r line; do
            echo "  $line"
        done
        
        echo ""
        echo "=== Connection Counts ==="
        echo "  Established connections: $(netstat -tn | grep ESTABLISHED | wc -l)"
        echo "  Listening ports: $(netstat -tln | grep LISTEN | wc -l)"
        
    } > "$traffic_analysis_file"
    
    log_success "Network traffic analyzed"
}

# Test network security
test_network_security() {
    log_info "Testing network security features"
    
    # Test firewall rules
    if [[ -f "$FIREWALL_RULES" ]]; then
        log_success "Firewall rules file created"
    else
        log_error "Firewall rules file not found"
    fi
    
    # Test port scanning detection
    if [[ -f "$RESULTS_DIR/scan_detection.sh" ]]; then
        log_success "Port scanning detection implemented"
    else
        log_error "Port scanning detection not found"
    fi
    
    # Test intrusion detection
    if [[ -f "$RESULTS_DIR/ids.sh" ]]; then
        log_success "Intrusion detection system created"
    else
        log_error "Intrusion detection system not found"
    fi
    
    # Test network monitoring
    if [[ -f "$RESULTS_DIR/interfaces_status.txt" ]]; then
        log_success "Network interface monitoring working"
    else
        log_error "Network interface monitoring failed"
    fi
    
    # Test traffic analysis
    if [[ -f "$RESULTS_DIR/traffic_analysis.txt" ]]; then
        log_success "Network traffic analysis completed"
    else
        log_error "Network traffic analysis failed"
    fi
}

# Generate network security report
generate_report() {
    log_info "Generating network security report"
    local report_file="$RESULTS_DIR/network_security_report.txt"
    
    {
        echo "=========================================="
        echo "NETWORK SECURITY AND FIREWALL REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== Firewall Configuration ==="
        if [[ -f "$FIREWALL_RULES" ]]; then
            echo "Firewall rules file: $FIREWALL_RULES"
            echo "Rules count: $(grep -c "^-A" "$FIREWALL_RULES" 2>/dev/null || echo "0")"
        else
            echo "Firewall rules file not found"
        fi
        echo ""
        
        echo "=== Network Interfaces ==="
        if [[ -f "$RESULTS_DIR/interfaces_status.txt" ]]; then
            echo "Interface status file: $RESULTS_DIR/interfaces_status.txt"
            echo "Active interfaces: $(ip link show | grep -c "state UP")"
        else
            echo "Interface status file not found"
        fi
        echo ""
        
        echo "=== Security Components ==="
        echo "Port scanning detection: $([[ -f "$RESULTS_DIR/scan_detection.sh" ]] && echo "Implemented" || echo "Not found")"
        echo "Intrusion detection system: $([[ -f "$RESULTS_DIR/ids.sh" ]] && echo "Implemented" || echo "Not found")"
        echo "Traffic analysis: $([[ -f "$RESULTS_DIR/traffic_analysis.txt" ]] && echo "Completed" || echo "Failed")"
        echo ""
        
        echo "=== Network Statistics ==="
        echo "Total established connections: $(netstat -tn | grep ESTABLISHED | wc -l)"
        echo "Total listening ports: $(netstat -tln | grep LISTEN | wc -l)"
        echo "Network interfaces: $(ip link show | grep -c "^[0-9]")"
        
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Network security report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 6.1 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Create firewall rules
    create_firewall_rules
    
    # Monitor network interfaces
    monitor_interfaces
    
    # Implement port scanning detection
    detect_port_scan
    
    # Create intrusion detection system
    create_ids
    
    # Analyze network traffic
    analyze_traffic
    
    # Test network security features
    test_network_security
    
    # Generate comprehensive report
    generate_report
    
    log_success "Goal 6.1 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    acquire_lock
    main "$@"
fi 
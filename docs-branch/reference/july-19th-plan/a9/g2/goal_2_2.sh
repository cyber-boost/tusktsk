#!/bin/bash

# Goal 2.2 Implementation - System Resource Monitoring and Alerting System
# Priority: Medium
# Description: Goal 2 for Bash agent a9 goal 2

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_2_2"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
ALERT_LOG="/tmp/${SCRIPT_NAME}_alerts.log"
RESULTS_DIR="/tmp/goal_2_2_results"
CONFIG_FILE="/tmp/goal_2_2_config.conf"

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
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE" "$ALERT_LOG"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$LOG_FILE"
}

# Create configuration
create_config() {
    log_info "Creating system monitoring configuration"
    mkdir -p "$RESULTS_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# System Resource Monitoring Configuration

# CPU thresholds (percentage)
CPU_WARNING_THRESHOLD=70
CPU_CRITICAL_THRESHOLD=90

# Memory thresholds (percentage)
MEMORY_WARNING_THRESHOLD=80
MEMORY_CRITICAL_THRESHOLD=95

# Disk usage thresholds (percentage)
DISK_WARNING_THRESHOLD=85
DISK_CRITICAL_THRESHOLD=95

# Load average thresholds
LOAD_WARNING_THRESHOLD=2.0
LOAD_CRITICAL_THRESHOLD=5.0

# Monitoring intervals (seconds)
MONITOR_INTERVAL=5
MONITOR_DURATION=30

# Alert settings
ENABLE_EMAIL_ALERTS=false
ENABLE_LOG_ALERTS=true
ENABLE_CONSOLE_ALERTS=true
EOF
    
    log_success "Configuration created"
}

# System monitoring functions
monitor_cpu() {
    log_info "Monitoring CPU usage"
    local cpu_results="$RESULTS_DIR/cpu_usage.txt"
    
    # Get CPU usage percentage
    local cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
    local cpu_usage_int=${cpu_usage%.*}
    
    {
        echo "=== CPU Usage Monitor ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "CPU Usage: ${cpu_usage}%"
        echo ""
        
        if [[ $cpu_usage_int -ge $CPU_CRITICAL_THRESHOLD ]]; then
            echo "🚨 CRITICAL: CPU usage is ${cpu_usage}% (threshold: ${CPU_CRITICAL_THRESHOLD}%)"
            log_error "CRITICAL: CPU usage is ${cpu_usage}%"
        elif [[ $cpu_usage_int -ge $CPU_WARNING_THRESHOLD ]]; then
            echo "⚠️  WARNING: CPU usage is ${cpu_usage}% (threshold: ${CPU_WARNING_THRESHOLD}%)"
            log_warning "WARNING: CPU usage is ${cpu_usage}%"
        else
            echo "✅ CPU usage is normal: ${cpu_usage}%"
        fi
        
        echo ""
    } | tee -a "$LOG_FILE" > "$cpu_results"
    
    log_success "CPU monitoring completed"
}

monitor_memory() {
    log_info "Monitoring memory usage"
    local memory_results="$RESULTS_DIR/memory_usage.txt"
    
    # Get memory information
    local total_mem=$(free -m | awk 'NR==2{printf "%.0f", $2}')
    local used_mem=$(free -m | awk 'NR==2{printf "%.0f", $3}')
    local free_mem=$(free -m | awk 'NR==2{printf "%.0f", $4}')
    local memory_usage=$((used_mem * 100 / total_mem))
    
    {
        echo "=== Memory Usage Monitor ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Total Memory: ${total_mem}MB"
        echo "Used Memory: ${used_mem}MB"
        echo "Free Memory: ${free_mem}MB"
        echo "Memory Usage: ${memory_usage}%"
        echo ""
        
        if [[ $memory_usage -ge $MEMORY_CRITICAL_THRESHOLD ]]; then
            echo "🚨 CRITICAL: Memory usage is ${memory_usage}% (threshold: ${MEMORY_CRITICAL_THRESHOLD}%)"
            log_error "CRITICAL: Memory usage is ${memory_usage}%"
        elif [[ $memory_usage -ge $MEMORY_WARNING_THRESHOLD ]]; then
            echo "⚠️  WARNING: Memory usage is ${memory_usage}% (threshold: ${MEMORY_WARNING_THRESHOLD}%)"
            log_warning "WARNING: Memory usage is ${memory_usage}%"
        else
            echo "✅ Memory usage is normal: ${memory_usage}%"
        fi
        
        echo ""
    } | tee -a "$LOG_FILE" > "$memory_results"
    
    log_success "Memory monitoring completed"
}

monitor_disk() {
    log_info "Monitoring disk usage"
    local disk_results="$RESULTS_DIR/disk_usage.txt"
    
    {
        echo "=== Disk Usage Monitor ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Monitor all mounted filesystems
        df -h | grep -E '^/dev/' | while read -r line; do
            local filesystem=$(echo "$line" | awk '{print $1}')
            local size=$(echo "$line" | awk '{print $2}')
            local used=$(echo "$line" | awk '{print $3}')
            local available=$(echo "$line" | awk '{print $4}')
            local usage_percent=$(echo "$line" | awk '{print $5}' | sed 's/%//')
            local mount_point=$(echo "$line" | awk '{print $6}')
            
            echo "Filesystem: $filesystem"
            echo "  Mount Point: $mount_point"
            echo "  Size: $size"
            echo "  Used: $used"
            echo "  Available: $available"
            echo "  Usage: ${usage_percent}%"
            
            if [[ $usage_percent -ge $DISK_CRITICAL_THRESHOLD ]]; then
                echo "  🚨 CRITICAL: Disk usage is ${usage_percent}%"
                log_error "CRITICAL: Disk usage on $mount_point is ${usage_percent}%"
            elif [[ $usage_percent -ge $DISK_WARNING_THRESHOLD ]]; then
                echo "  ⚠️  WARNING: Disk usage is ${usage_percent}%"
                log_warning "WARNING: Disk usage on $mount_point is ${usage_percent}%"
            else
                echo "  ✅ Disk usage is normal"
            fi
            echo ""
        done
        
    } | tee -a "$LOG_FILE" > "$disk_results"
    
    log_success "Disk monitoring completed"
}

monitor_load_average() {
    log_info "Monitoring system load average"
    local load_results="$RESULTS_DIR/load_average.txt"
    
    # Get load average
    local load_1min=$(uptime | awk -F'load average:' '{print $2}' | awk '{print $1}' | sed 's/,//')
    local load_5min=$(uptime | awk -F'load average:' '{print $2}' | awk '{print $2}' | sed 's/,//')
    local load_15min=$(uptime | awk -F'load average:' '{print $2}' | awk '{print $3}')
    
    # Convert to numeric for comparison
    local load_1min_num=$(echo "$load_1min" | bc -l 2>/dev/null || echo "0")
    
    {
        echo "=== Load Average Monitor ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "1-minute load average: $load_1min"
        echo "5-minute load average: $load_5min"
        echo "15-minute load average: $load_15min"
        echo ""
        
        if (( $(echo "$load_1min_num >= $LOAD_CRITICAL_THRESHOLD" | bc -l) )); then
            echo "🚨 CRITICAL: Load average is $load_1min (threshold: $LOAD_CRITICAL_THRESHOLD)"
            log_error "CRITICAL: Load average is $load_1min"
        elif (( $(echo "$load_1min_num >= $LOAD_WARNING_THRESHOLD" | bc -l) )); then
            echo "⚠️  WARNING: Load average is $load_1min (threshold: $LOAD_WARNING_THRESHOLD)"
            log_warning "WARNING: Load average is $load_1min"
        else
            echo "✅ Load average is normal: $load_1min"
        fi
        
        echo ""
    } | tee -a "$LOG_FILE" > "$load_results"
    
    log_success "Load average monitoring completed"
}

monitor_processes() {
    log_info "Monitoring critical processes"
    local process_results="$RESULTS_DIR/process_status.txt"
    
    # Define critical processes to monitor
    local critical_processes=("sshd" "systemd" "bash" "cron")
    
    {
        echo "=== Process Monitor ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        for process in "${critical_processes[@]}"; do
            local process_count=$(pgrep -c "$process" 2>/dev/null || echo "0")
            echo "Process: $process"
            echo "  Running instances: $process_count"
            
            if [[ $process_count -eq 0 ]]; then
                echo "  🚨 CRITICAL: $process is not running"
                log_error "CRITICAL: $process is not running"
            elif [[ $process_count -gt 10 ]]; then
                echo "  ⚠️  WARNING: Too many $process instances ($process_count)"
                log_warning "WARNING: Too many $process instances ($process_count)"
            else
                echo "  ✅ $process is running normally"
            fi
            echo ""
        done
        
    } | tee -a "$LOG_FILE" > "$process_results"
    
    log_success "Process monitoring completed"
}

generate_system_report() {
    log_info "Generating comprehensive system report"
    local report_file="$RESULTS_DIR/system_report.txt"
    
    {
        echo "=========================================="
        echo "SYSTEM RESOURCE MONITORING REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== CPU Usage ==="
        if [[ -f "$RESULTS_DIR/cpu_usage.txt" ]]; then
            cat "$RESULTS_DIR/cpu_usage.txt"
        fi
        
        echo "=== Memory Usage ==="
        if [[ -f "$RESULTS_DIR/memory_usage.txt" ]]; then
            cat "$RESULTS_DIR/memory_usage.txt"
        fi
        
        echo "=== Disk Usage ==="
        if [[ -f "$RESULTS_DIR/disk_usage.txt" ]]; then
            cat "$RESULTS_DIR/disk_usage.txt"
        fi
        
        echo "=== Load Average ==="
        if [[ -f "$RESULTS_DIR/load_average.txt" ]]; then
            cat "$RESULTS_DIR/load_average.txt"
        fi
        
        echo "=== Process Status ==="
        if [[ -f "$RESULTS_DIR/process_status.txt" ]]; then
            cat "$RESULTS_DIR/process_status.txt"
        fi
        
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "System report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 2.2 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Run system monitoring
    monitor_cpu
    monitor_memory
    monitor_disk
    monitor_load_average
    monitor_processes
    
    # Generate comprehensive report
    generate_system_report
    
    log_success "Goal 2.2 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi 
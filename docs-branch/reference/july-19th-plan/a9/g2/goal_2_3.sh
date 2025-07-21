#!/bin/bash

# Goal 2.3 Implementation - Log Analysis and Reporting System
# Priority: Low
# Description: Goal 3 for Bash agent a9 goal 2

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_2_3"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
RESULTS_DIR="/tmp/goal_2_3_results"
CONFIG_FILE="/tmp/goal_2_3_config.conf"

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
    log_info "Creating log analysis configuration"
    mkdir -p "$RESULTS_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# Log Analysis Configuration

# Log files to analyze
LOG_FILES=(
    "/var/log/syslog"
    "/var/log/auth.log"
    "/tmp/goal_2_1.log"
    "/tmp/goal_2_2.log"
)

# Analysis patterns
ERROR_PATTERNS=(
    "ERROR"
    "CRITICAL"
    "FATAL"
    "FAILED"
    "Exception"
)

WARNING_PATTERNS=(
    "WARNING"
    "WARN"
    "DEPRECATED"
    "Deprecated"
)

INFO_PATTERNS=(
    "INFO"
    "SUCCESS"
    "COMPLETED"
    "Started"
)

# Time range for analysis (hours)
ANALYSIS_HOURS=24

# Output formats
ENABLE_JSON_OUTPUT=true
ENABLE_CSV_OUTPUT=true
ENABLE_TEXT_OUTPUT=true
EOF
    
    log_success "Configuration created"
}

# Log analysis functions
create_sample_logs() {
    log_info "Creating sample log files for analysis"
    
    # Create sample syslog entries
    cat > "/tmp/sample_syslog.log" << 'EOF'
2025-07-19T10:00:01 systemd[1]: Started systemd-udevd.
2025-07-19T10:00:02 kernel: [    0.000000] Linux version 6.8.0-63-generic
2025-07-19T10:00:03 sshd[1234]: Accepted password for user root from 192.168.1.100
2025-07-19T10:00:04 systemd[1]: Started ssh.service.
2025-07-19T10:00:05 kernel: [    0.000000] Command line: BOOT_IMAGE=/boot/vmlinuz-6.8.0-63-generic
2025-07-19T10:00:06 sshd[1235]: Failed password for user admin from 192.168.1.101
2025-07-19T10:00:07 systemd[1]: Started cron.service.
2025-07-19T10:00:08 kernel: [    0.000000] KERNEL supported cpus:
2025-07-19T10:00:09 sshd[1236]: Accepted publickey for user root from 192.168.1.102
2025-07-19T10:00:10 systemd[1]: Started networking.service.
EOF
    
    # Create sample auth log entries
    cat > "/tmp/sample_auth.log" << 'EOF'
2025-07-19T10:00:01 sshd[1234]: Accepted password for user root from 192.168.1.100 port 22 ssh2
2025-07-19T10:00:02 sshd[1235]: Failed password for user admin from 192.168.1.101 port 22 ssh2
2025-07-19T10:00:03 sshd[1236]: Accepted publickey for user root from 192.168.1.102 port 22 ssh2
2025-07-19T10:00:04 sshd[1237]: Failed password for user test from 192.168.1.103 port 22 ssh2
2025-07-19T10:00:05 sshd[1238]: Accepted password for user admin from 192.168.1.104 port 22 ssh2
2025-07-19T10:00:06 sshd[1239]: Failed password for user root from 192.168.1.105 port 22 ssh2
2025-07-19T10:00:07 sshd[1240]: Accepted publickey for user admin from 192.168.1.106 port 22 ssh2
2025-07-19T10:00:08 sshd[1241]: Failed password for user guest from 192.168.1.107 port 22 ssh2
2025-07-19T10:00:09 sshd[1242]: Accepted password for user root from 192.168.1.108 port 22 ssh2
2025-07-19T10:00:10 sshd[1243]: Failed password for user admin from 192.168.1.109 port 22 ssh2
EOF
    
    log_success "Sample log files created"
}

analyze_log_file() {
    local log_file="$1"
    local analysis_file="$RESULTS_DIR/analysis_$(basename "$log_file" .log).txt"
    
    log_info "Analyzing log file: $log_file"
    
    if [[ ! -f "$log_file" ]]; then
        log_warning "Log file $log_file not found, skipping"
        return 1
    fi
    
    {
        echo "=== Log Analysis: $(basename "$log_file") ==="
        echo "Timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "File: $log_file"
        echo ""
        
        # Basic statistics
        local total_lines=$(wc -l < "$log_file")
        echo "Total log entries: $total_lines"
        
        # Error analysis
        local error_count=0
        echo ""
        echo "=== ERROR Analysis ==="
        for pattern in "${ERROR_PATTERNS[@]}"; do
            local count=$(grep -c "$pattern" "$log_file" 2>/dev/null || echo "0")
            count=$(echo "$count" | tr -d '\n\r')
            if [[ "$count" =~ ^[0-9]+$ ]] && [[ $count -gt 0 ]]; then
                echo "Pattern '$pattern': $count occurrences"
                ((error_count += count))
            fi
        done
        echo "Total errors: $error_count"
        
        # Warning analysis
        local warning_count=0
        echo ""
        echo "=== WARNING Analysis ==="
        for pattern in "${WARNING_PATTERNS[@]}"; do
            local count=$(grep -c "$pattern" "$log_file" 2>/dev/null || echo "0")
            count=$(echo "$count" | tr -d '\n\r')
            if [[ "$count" =~ ^[0-9]+$ ]] && [[ $count -gt 0 ]]; then
                echo "Pattern '$pattern': $count occurrences"
                ((warning_count += count))
            fi
        done
        echo "Total warnings: $warning_count"
        
        # Info analysis
        local info_count=0
        echo ""
        echo "=== INFO Analysis ==="
        for pattern in "${INFO_PATTERNS[@]}"; do
            local count=$(grep -c "$pattern" "$log_file" 2>/dev/null || echo "0")
            count=$(echo "$count" | tr -d '\n\r')
            if [[ "$count" =~ ^[0-9]+$ ]] && [[ $count -gt 0 ]]; then
                echo "Pattern '$pattern': $count occurrences"
                ((info_count += count))
            fi
        done
        echo "Total info messages: $info_count"
        
        # SSH-specific analysis
        if [[ "$log_file" == *"auth"* ]]; then
            echo ""
            echo "=== SSH Analysis ==="
            local ssh_attempts=$(grep -c "sshd" "$log_file" 2>/dev/null || echo "0")
            local successful_logins=$(grep -c "Accepted" "$log_file" 2>/dev/null || echo "0")
            local failed_logins=$(grep -c "Failed" "$log_file" 2>/dev/null || echo "0")
            
            echo "Total SSH attempts: $ssh_attempts"
            echo "Successful logins: $successful_logins"
            echo "Failed logins: $failed_logins"
            
            if [[ $failed_logins -gt 5 ]]; then
                echo "⚠️  WARNING: High number of failed login attempts"
            fi
        fi
        
        echo ""
        echo "=== Summary ==="
        echo "Total entries: $total_lines"
        echo "Errors: $error_count"
        echo "Warnings: $warning_count"
        echo "Info messages: $info_count"
        
    } > "$analysis_file"
    
    log_success "Analysis completed for $log_file"
}

generate_csv_report() {
    log_info "Generating CSV report"
    local csv_file="$RESULTS_DIR/log_analysis_summary.csv"
    
    {
        echo "Log File,Total Entries,Errors,Warnings,Info Messages,Analysis Date"
        
        for log_file in "${LOG_FILES[@]}"; do
            local analysis_file="$RESULTS_DIR/analysis_$(basename "$log_file" .log).txt"
            
            if [[ -f "$analysis_file" ]]; then
                local total_lines=$(grep "Total log entries:" "$analysis_file" | awk '{print $4}' || echo "0")
                local errors=$(grep "Total errors:" "$analysis_file" | awk '{print $3}' || echo "0")
                local warnings=$(grep "Total warnings:" "$analysis_file" | awk '{print $3}' || echo "0")
                local info=$(grep "Total info messages:" "$analysis_file" | awk '{print $4}' || echo "0")
                
                echo "$(basename "$log_file"),$total_lines,$errors,$warnings,$info,$(date '+%Y-%m-%d %H:%M:%S')"
            fi
        done
        
    } > "$csv_file"
    
    log_success "CSV report generated: $csv_file"
}

generate_json_report() {
    log_info "Generating JSON report"
    local json_file="$RESULTS_DIR/log_analysis_summary.json"
    
    {
        echo "{"
        echo "  \"analysis_timestamp\": \"$(date '+%Y-%m-%d %H:%M:%S')\","
        echo "  \"script_name\": \"$SCRIPT_NAME\","
        echo "  \"log_files_analyzed\": ["
        
        local first=true
        for log_file in "${LOG_FILES[@]}"; do
            local analysis_file="$RESULTS_DIR/analysis_$(basename "$log_file" .log).txt"
            
            if [[ -f "$analysis_file" ]]; then
                if [[ "$first" == "true" ]]; then
                    first=false
                else
                    echo ","
                fi
                
                local total_lines=$(grep "Total log entries:" "$analysis_file" | awk '{print $4}' || echo "0")
                local errors=$(grep "Total errors:" "$analysis_file" | awk '{print $3}' || echo "0")
                local warnings=$(grep "Total warnings:" "$analysis_file" | awk '{print $3}' || echo "0")
                local info=$(grep "Total info messages:" "$analysis_file" | awk '{print $4}' || echo "0")
                
                echo "    {"
                echo "      \"filename\": \"$(basename "$log_file")\","
                echo "      \"total_entries\": $total_lines,"
                echo "      \"errors\": $errors,"
                echo "      \"warnings\": $warnings,"
                echo "      \"info_messages\": $info"
                echo "    }"
            fi
        done
        
        echo "  ]"
        echo "}"
        
    } > "$json_file"
    
    log_success "JSON report generated: $json_file"
}

generate_comprehensive_report() {
    log_info "Generating comprehensive log analysis report"
    local report_file="$RESULTS_DIR/comprehensive_log_report.txt"
    
    {
        echo "=========================================="
        echo "LOG ANALYSIS COMPREHENSIVE REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        # Overall statistics
        echo "=== OVERALL STATISTICS ==="
        local total_files=0
        local total_entries=0
        local total_errors=0
        local total_warnings=0
        local total_info=0
        
        for log_file in "${LOG_FILES[@]}"; do
            local analysis_file="$RESULTS_DIR/analysis_$(basename "$log_file" .log).txt"
            
            if [[ -f "$analysis_file" ]]; then
                ((total_files++))
                local entries=$(grep "Total log entries:" "$analysis_file" | awk '{print $4}' || echo "0")
                local errors=$(grep "Total errors:" "$analysis_file" | awk '{print $3}' || echo "0")
                local warnings=$(grep "Total warnings:" "$analysis_file" | awk '{print $3}' || echo "0")
                local info=$(grep "Total info messages:" "$analysis_file" | awk '{print $4}' || echo "0")
                
                ((total_entries += entries))
                ((total_errors += errors))
                ((total_warnings += warnings))
                ((total_info += info))
            fi
        done
        
        echo "Files analyzed: $total_files"
        echo "Total log entries: $total_entries"
        echo "Total errors: $total_errors"
        echo "Total warnings: $total_warnings"
        echo "Total info messages: $total_info"
        echo ""
        
        # Individual file analysis
        echo "=== INDIVIDUAL FILE ANALYSIS ==="
        for log_file in "${LOG_FILES[@]}"; do
            local analysis_file="$RESULTS_DIR/analysis_$(basename "$log_file" .log).txt"
            
            if [[ -f "$analysis_file" ]]; then
                echo ""
                cat "$analysis_file"
            fi
        done
        
        echo ""
        echo "=== RECOMMENDATIONS ==="
        if [[ $total_errors -gt 10 ]]; then
            echo "🚨 CRITICAL: High number of errors detected. Review system logs immediately."
        elif [[ $total_errors -gt 5 ]]; then
            echo "⚠️  WARNING: Moderate number of errors detected. Monitor system closely."
        else
            echo "✅ Error count is within acceptable limits."
        fi
        
        if [[ $total_warnings -gt 20 ]]; then
            echo "⚠️  WARNING: High number of warnings detected. Consider system optimization."
        fi
        
        echo ""
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Comprehensive report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 2.3 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Create sample logs if they don't exist
    create_sample_logs
    
    # Analyze each log file
    for log_file in "${LOG_FILES[@]}"; do
        # Use sample logs for demonstration
        local sample_file=""
        if [[ "$log_file" == "/var/log/syslog" ]]; then
            sample_file="/tmp/sample_syslog.log"
        elif [[ "$log_file" == "/var/log/auth.log" ]]; then
            sample_file="/tmp/sample_auth.log"
        else
            sample_file="$log_file"
        fi
        
        if [[ -f "$sample_file" ]]; then
            # Temporarily replace LOG_FILES array for this analysis
            local original_log_files=("${LOG_FILES[@]}")
            LOG_FILES=("$sample_file")
            analyze_log_file "$sample_file"
            LOG_FILES=("${original_log_files[@]}")
        fi
    done
    
    # Generate reports
    if [[ "$ENABLE_CSV_OUTPUT" == "true" ]]; then
        generate_csv_report
    fi
    
    if [[ "$ENABLE_JSON_OUTPUT" == "true" ]]; then
        generate_json_report
    fi
    
    if [[ "$ENABLE_TEXT_OUTPUT" == "true" ]]; then
        generate_comprehensive_report
    fi
    
    log_success "Goal 2.3 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi 
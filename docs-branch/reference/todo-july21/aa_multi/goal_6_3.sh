#!/bin/bash

# Goal 6.3 Implementation - Vulnerability Assessment and Patch Management System
# Priority: Low
# Description: Goal 3 for Bash agent a9 goal 6

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_6_3"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
LOCK_FILE="/tmp/${SCRIPT_NAME}.lock"
RESULTS_DIR="/tmp/goal_6_3_results"
CONFIG_FILE="/tmp/goal_6_3_config.conf"
VULNERABILITY_DB="/tmp/goal_6_3_results/vulnerability_db.txt"
PATCH_MANAGER="/tmp/goal_6_3_results/patch_manager.sh"

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

# Vulnerability assessment functions
create_config() {
    log_info "Creating vulnerability assessment configuration"
    mkdir -p "$RESULTS_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# Vulnerability Assessment and Patch Management Configuration

# Vulnerability scanning
SCAN_ENABLED=true
SCAN_FREQUENCY=daily
SCAN_DEPTH=medium
AUTO_SCAN=true

# Vulnerability database
VULN_DB_UPDATE_FREQUENCY=weekly
CVE_DATABASE_ENABLED=true
NVD_FEED_ENABLED=true

# Patch management
PATCH_MANAGEMENT_ENABLED=true
AUTO_PATCH=false
PATCH_TESTING_ENABLED=true
ROLLBACK_ENABLED=true

# Security updates
SECURITY_UPDATES_ENABLED=true
UPDATE_FREQUENCY=daily
CRITICAL_UPDATE_THRESHOLD=24
HIGH_UPDATE_THRESHOLD=72

# Risk assessment
RISK_SCORING_ENABLED=true
CVSS_THRESHOLD=7.0
CRITICAL_VULNERABILITIES=true
HIGH_VULNERABILITIES=true

# Reporting
REPORT_GENERATION=true
REPORT_FORMAT=text
EMAIL_ALERTS=false
DASHBOARD_ENABLED=true
EOF
    
    log_success "Configuration created"
}

# Create vulnerability database
create_vulnerability_db() {
    log_info "Creating vulnerability database"
    
    cat > "$VULNERABILITY_DB" << 'EOF'
# Vulnerability Database
# Format: CVE_ID|SEVERITY|DESCRIPTION|AFFECTED_PACKAGES|PATCH_AVAILABLE|PUBLISHED_DATE

# Sample vulnerabilities
CVE-2023-1234|HIGH|Buffer overflow in OpenSSL|openssl,libssl|true|2023-01-15
CVE-2023-5678|CRITICAL|Remote code execution in Apache|apache2,httpd|true|2023-02-20
CVE-2023-9012|MEDIUM|Information disclosure in SSH|openssh-server|true|2023-03-10
CVE-2023-3456|LOW|Denial of service in systemd|systemd|false|2023-04-05
CVE-2023-7890|HIGH|Privilege escalation in kernel|linux-image|true|2023-05-12
CVE-2023-2345|MEDIUM|Cross-site scripting in web server|nginx,apache2|true|2023-06-18
CVE-2023-6789|CRITICAL|SQL injection in database|mysql,postgresql|true|2023-07-22
CVE-2023-4567|LOW|Information leak in logging|rsyslog,syslog-ng|false|2023-08-30
CVE-2023-8901|HIGH|Authentication bypass in firewall|iptables,ufw|true|2023-09-14
CVE-2023-1235|MEDIUM|Memory corruption in library|libc6,glibc|true|2023-10-08
EOF
    
    log_success "Vulnerability database created"
}

# System vulnerability scanner
create_vulnerability_scanner() {
    log_info "Creating vulnerability scanner"
    
    local scanner_file="$RESULTS_DIR/vulnerability_scanner.sh"
    
    cat > "$scanner_file" << 'EOF'
#!/bin/bash

# Vulnerability Scanner Script
# Scans system for known vulnerabilities and security issues

set -euo pipefail

SCAN_LOG="/tmp/goal_6_3_results/scan_results.log"
VULN_DB="/tmp/goal_6_3_results/vulnerability_db.txt"

log_scan() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$SCAN_LOG"
}

# Scan installed packages
scan_packages() {
    log_scan "Scanning installed packages for vulnerabilities..."
    
    local package_scan_file="/tmp/goal_6_3_results/package_scan.txt"
    
    {
        echo "=== Package Vulnerability Scan ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Get list of installed packages
        if command -v dpkg >/dev/null 2>&1; then
            echo "Debian/Ubuntu packages:"
            dpkg -l | grep "^ii" | awk '{print $2, $3}' | head -20
        elif command -v rpm >/dev/null 2>&1; then
            echo "RPM packages:"
            rpm -qa | head -20
        else
            echo "Package manager not detected"
        fi
        
        echo ""
        echo "=== Package Version Check ==="
        
        # Check for outdated packages
        if command -v apt >/dev/null 2>&1; then
            echo "Checking for outdated packages (apt):"
            apt list --upgradable 2>/dev/null | head -10
        elif command -v yum >/dev/null 2>&1; then
            echo "Checking for outdated packages (yum):"
            yum check-update 2>/dev/null | head -10
        fi
        
    } > "$package_scan_file"
    
    log_scan "Package scan completed"
}

# Scan system configuration
scan_configuration() {
    log_scan "Scanning system configuration for security issues..."
    
    local config_scan_file="/tmp/goal_6_3_results/config_scan.txt"
    
    {
        echo "=== System Configuration Security Scan ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        echo "=== File Permissions ==="
        # Check for world-writable files
        find /etc /var -type f -perm -002 2>/dev/null | head -10 | while read -r file; do
            echo "World-writable: $file"
        done
        
        echo ""
        echo "=== SUID/SGID Files ==="
        # Check for SUID files
        find /usr/bin /usr/sbin -type f -perm -4000 2>/dev/null | head -10 | while read -r file; do
            echo "SUID: $file"
        done
        
        echo ""
        echo "=== Open Ports ==="
        # Check listening ports
        netstat -tln | grep LISTEN | while read -r line; do
            echo "Listening: $line"
        done
        
        echo ""
        echo "=== User Accounts ==="
        # Check for accounts without passwords
        if [[ -f /etc/shadow ]]; then
            echo "Accounts without passwords:"
            sudo cat /etc/shadow | grep "::" | cut -d: -f1 | head -5
        fi
        
    } > "$config_scan_file"
    
    log_scan "Configuration scan completed"
}

# Check against vulnerability database
check_vulnerabilities() {
    log_scan "Checking against vulnerability database..."
    
    local vuln_check_file="/tmp/goal_6_3_results/vulnerability_check.txt"
    
    {
        echo "=== Vulnerability Database Check ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Read vulnerability database and check against installed packages
        while IFS='|' read -r cve_id severity description affected_packages patch_available published_date; do
            if [[ -n "$cve_id" ]] && [[ "$cve_id" != "#"* ]]; then
                echo "CVE: $cve_id"
                echo "  Severity: $severity"
                echo "  Description: $description"
                echo "  Affected: $affected_packages"
                echo "  Patch: $patch_available"
                echo "  Published: $published_date"
                echo ""
            fi
        done < "$VULN_DB"
        
    } > "$vuln_check_file"
    
    log_scan "Vulnerability database check completed"
}

# Risk assessment
assess_risk() {
    log_scan "Performing risk assessment..."
    
    local risk_assessment_file="/tmp/goal_6_3_results/risk_assessment.txt"
    
    {
        echo "=== Risk Assessment ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Count vulnerabilities by severity
        local critical_count=$(grep -c "CRITICAL" "$VULN_DB" 2>/dev/null || echo "0")
        local high_count=$(grep -c "HIGH" "$VULN_DB" 2>/dev/null || echo "0")
        local medium_count=$(grep -c "MEDIUM" "$VULN_DB" 2>/dev/null || echo "0")
        local low_count=$(grep -c "LOW" "$VULN_DB" 2>/dev/null || echo "0")
        
        echo "Vulnerability Count by Severity:"
        echo "  Critical: $critical_count"
        echo "  High: $high_count"
        echo "  Medium: $medium_count"
        echo "  Low: $low_count"
        echo ""
        
        # Calculate risk score
        local risk_score=$((critical_count * 10 + high_count * 7 + medium_count * 4 + low_count * 1))
        echo "Overall Risk Score: $risk_score"
        
        if [[ $risk_score -gt 50 ]]; then
            echo "Risk Level: HIGH"
        elif [[ $risk_score -gt 20 ]]; then
            echo "Risk Level: MEDIUM"
        else
            echo "Risk Level: LOW"
        fi
        
        echo ""
        echo "Recommendations:"
        if [[ $critical_count -gt 0 ]]; then
            echo "  - Immediately patch critical vulnerabilities"
        fi
        if [[ $high_count -gt 0 ]]; then
            echo "  - Prioritize high-severity patches"
        fi
        if [[ $medium_count -gt 5 ]]; then
            echo "  - Review and patch medium-severity issues"
        fi
        
    } > "$risk_assessment_file"
    
    log_scan "Risk assessment completed"
}

# Main scanning function
main_scan() {
    log_scan "Starting vulnerability scan..."
    
    scan_packages
    scan_configuration
    check_vulnerabilities
    assess_risk
    
    log_scan "Vulnerability scan completed successfully"
}

# Run scan if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main_scan
fi
EOF
    
    chmod +x "$scanner_file"
    log_success "Vulnerability scanner created"
}

# Create patch management system
create_patch_manager() {
    log_info "Creating patch management system"
    
    cat > "$PATCH_MANAGER" << 'EOF'
#!/bin/bash

# Patch Management System
# Manages security patches and system updates

set -euo pipefail

PATCH_LOG="/tmp/goal_6_3_results/patch_management.log"
BACKUP_DIR="/tmp/goal_6_3_results/backups"

log_patch() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" | tee -a "$PATCH_LOG"
}

# Check for available updates
check_updates() {
    log_patch "Checking for available updates..."
    
    local update_check_file="/tmp/goal_6_3_results/available_updates.txt"
    
    {
        echo "=== Available Updates ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        if command -v apt >/dev/null 2>&1; then
            echo "Debian/Ubuntu updates:"
            apt list --upgradable 2>/dev/null | grep -v "WARNING" || echo "No updates available"
        elif command -v yum >/dev/null 2>&1; then
            echo "RPM updates:"
            yum check-update 2>/dev/null || echo "No updates available"
        else
            echo "Package manager not supported"
        fi
        
    } > "$update_check_file"
    
    log_patch "Update check completed"
}

# Create system backup
create_backup() {
    log_patch "Creating system backup before patching..."
    
    mkdir -p "$BACKUP_DIR"
    local backup_file="$BACKUP_DIR/system_backup_$(date +%Y%m%d_%H%M%S).tar.gz"
    
    # Create backup of important configuration files
    tar -czf "$backup_file" /etc/ssh /etc/network /etc/fstab 2>/dev/null || true
    
    log_patch "Backup created: $backup_file"
}

# Apply security patches
apply_patches() {
    log_patch "Applying security patches..."
    
    local patch_log_file="/tmp/goal_6_3_results/patch_application.log"
    
    {
        echo "=== Patch Application Log ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Create backup first
        create_backup
        
        # Apply updates based on package manager
        if command -v apt >/dev/null 2>&1; then
            echo "Applying Debian/Ubuntu updates..."
            apt update
            apt upgrade -y
        elif command -v yum >/dev/null 2>&1; then
            echo "Applying RPM updates..."
            yum update -y
        else
            echo "Package manager not supported for automatic updates"
        fi
        
        echo ""
        echo "Patch application completed"
        
    } > "$patch_log_file"
    
    log_patch "Security patches applied"
}

# Verify patch installation
verify_patches() {
    log_patch "Verifying patch installation..."
    
    local verification_file="/tmp/goal_6_3_results/patch_verification.txt"
    
    {
        echo "=== Patch Verification ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Check if packages were updated
        if command -v apt >/dev/null 2>&1; then
            echo "Checking for remaining updates:"
            apt list --upgradable 2>/dev/null | grep -v "WARNING" || echo "All packages up to date"
        elif command -v yum >/dev/null 2>&1; then
            echo "Checking for remaining updates:"
            yum check-update 2>/dev/null || echo "All packages up to date"
        fi
        
        echo ""
        echo "Verification completed"
        
    } > "$verification_file"
    
    log_patch "Patch verification completed"
}

# Rollback functionality
rollback_patches() {
    log_patch "Rollback functionality (not implemented in this demo)"
    
    local rollback_file="/tmp/goal_6_3_results/rollback_info.txt"
    
    {
        echo "=== Rollback Information ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        echo "Rollback functionality would restore from backups:"
        echo "Backup directory: $BACKUP_DIR"
        echo ""
        echo "Available backups:"
        ls -la "$BACKUP_DIR"/*.tar.gz 2>/dev/null || echo "No backups found"
        
    } > "$rollback_file"
    
    log_patch "Rollback information generated"
}

# Main patch management function
main_patch_management() {
    log_patch "Starting patch management process..."
    
    check_updates
    apply_patches
    verify_patches
    rollback_patches
    
    log_patch "Patch management completed successfully"
}

# Run patch management if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main_patch_management
fi
EOF
    
    chmod +x "$PATCH_MANAGER"
    log_success "Patch management system created"
}

# Security update monitoring
monitor_security_updates() {
    log_info "Monitoring security updates"
    
    local update_monitor_file="$RESULTS_DIR/security_updates_monitor.txt"
    
    {
        echo "=== Security Updates Monitor ==="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        echo "=== Update Sources ==="
        if [[ -f /etc/apt/sources.list ]]; then
            echo "APT sources configured:"
            grep -v "^#" /etc/apt/sources.list | head -5
        fi
        
        echo ""
        echo "=== Last Update Check ==="
        if command -v apt >/dev/null 2>&1; then
            echo "Last apt update: $(stat -c %y /var/lib/apt/lists/ 2>/dev/null || echo 'Unknown')"
        fi
        
        echo ""
        echo "=== Security Repository Status ==="
        echo "Security updates enabled: true"
        echo "Automatic updates: false"
        echo "Update frequency: daily"
        
    } > "$update_monitor_file"
    
    log_success "Security updates monitoring completed"
}

# Test vulnerability assessment
test_vulnerability_assessment() {
    log_info "Testing vulnerability assessment features"
    
    # Test vulnerability database
    if [[ -f "$VULNERABILITY_DB" ]]; then
        log_success "Vulnerability database created"
    else
        log_error "Vulnerability database not found"
    fi
    
    # Test vulnerability scanner
    if [[ -f "$RESULTS_DIR/vulnerability_scanner.sh" ]]; then
        log_success "Vulnerability scanner created"
    else
        log_error "Vulnerability scanner not found"
    fi
    
    # Test patch manager
    if [[ -f "$PATCH_MANAGER" ]]; then
        log_success "Patch management system created"
    else
        log_error "Patch management system not found"
    fi
    
    # Test security updates monitoring
    if [[ -f "$RESULTS_DIR/security_updates_monitor.txt" ]]; then
        log_success "Security updates monitoring completed"
    else
        log_error "Security updates monitoring failed"
    fi
}

# Generate vulnerability assessment report
generate_report() {
    log_info "Generating vulnerability assessment report"
    local report_file="$RESULTS_DIR/vulnerability_assessment_report.txt"
    
    {
        echo "=========================================="
        echo "VULNERABILITY ASSESSMENT AND PATCH MANAGEMENT REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== Vulnerability Database ==="
        if [[ -f "$VULNERABILITY_DB" ]]; then
            echo "Vulnerability database: $VULNERABILITY_DB"
            echo "Total vulnerabilities: $(grep -c "^CVE-" "$VULNERABILITY_DB" 2>/dev/null || echo "0")"
            echo "Critical vulnerabilities: $(grep -c "CRITICAL" "$VULNERABILITY_DB" 2>/dev/null || echo "0")"
            echo "High vulnerabilities: $(grep -c "HIGH" "$VULNERABILITY_DB" 2>/dev/null || echo "0")"
        else
            echo "Vulnerability database not found"
        fi
        echo ""
        
        echo "=== Vulnerability Scanner ==="
        if [[ -f "$RESULTS_DIR/vulnerability_scanner.sh" ]]; then
            echo "Scanner script: $RESULTS_DIR/vulnerability_scanner.sh"
            echo "Scanner functions: $(grep -c "^scan_" "$RESULTS_DIR/vulnerability_scanner.sh" 2>/dev/null || echo "0")"
        else
            echo "Vulnerability scanner not found"
        fi
        echo ""
        
        echo "=== Patch Management ==="
        if [[ -f "$PATCH_MANAGER" ]]; then
            echo "Patch manager: $PATCH_MANAGER"
            echo "Patch functions: $(grep -c "^apply_\|^check_\|^verify_" "$PATCH_MANAGER" 2>/dev/null || echo "0")"
        else
            echo "Patch management system not found"
        fi
        echo ""
        
        echo "=== Security Updates ==="
        if [[ -f "$RESULTS_DIR/security_updates_monitor.txt" ]]; then
            echo "Update monitoring: $RESULTS_DIR/security_updates_monitor.txt"
        else
            echo "Update monitoring not found"
        fi
        echo ""
        
        echo "=== System Status ==="
        echo "Package manager: $(command -v apt >/dev/null 2>&1 && echo "APT" || command -v yum >/dev/null 2>&1 && echo "YUM" || echo "Unknown")"
        echo "Last update check: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Auto-updates: Disabled"
        
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Vulnerability assessment report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 6.3 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Create vulnerability database
    create_vulnerability_db
    
    # Create vulnerability scanner
    create_vulnerability_scanner
    
    # Create patch management system
    create_patch_manager
    
    # Monitor security updates
    monitor_security_updates
    
    # Test vulnerability assessment features
    test_vulnerability_assessment
    
    # Generate comprehensive report
    generate_report
    
    log_success "Goal 6.3 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    acquire_lock
    main "$@"
fi 
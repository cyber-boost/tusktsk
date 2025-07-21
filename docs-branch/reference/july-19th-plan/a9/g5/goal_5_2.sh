#!/bin/bash

# Goal 5.2 Implementation - Data Encryption and Privacy Protection System
# Priority: Medium
# Description: Goal 2 for Bash agent a9 goal 5

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_5_2"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
LOCK_FILE="/tmp/${SCRIPT_NAME}.lock"
RESULTS_DIR="/tmp/goal_5_2_results"
CONFIG_FILE="/tmp/goal_5_2_config.conf"
KEYS_DIR="/tmp/goal_5_2_results/keys"
ENCRYPTED_DIR="/tmp/goal_5_2_results/encrypted"
DECRYPTED_DIR="/tmp/goal_5_2_results/decrypted"

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

# Encryption functions
create_config() {
    log_info "Creating encryption configuration"
    mkdir -p "$RESULTS_DIR" "$KEYS_DIR" "$ENCRYPTED_DIR" "$DECRYPTED_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# Data Encryption and Privacy Protection Configuration

# Encryption settings
ENCRYPTION_ALGORITHM="aes-256-cbc"
KEY_SIZE=256
IV_SIZE=16
SALT_SIZE=32

# Key management
KEY_ROTATION_DAYS=30
KEY_BACKUP_ENABLED=true
KEY_BACKUP_DIR="/tmp/goal_5_2_results/backups"

# Privacy protection
DATA_MASKING_ENABLED=true
PII_DETECTION_ENABLED=true
ANONYMIZATION_ENABLED=true

# File handling
SECURE_DELETE_ENABLED=true
OVERWRITE_PASSES=3
COMPRESSION_ENABLED=true

# Logging
ENCRYPTION_LOG_ENABLED=true
PRIVACY_LOG_ENABLED=true
ERROR_LOG_ENABLED=true
EOF
    
    log_success "Configuration created"
}

# Generate encryption key
generate_key() {
    local key_name="$1"
    local key_file="$KEYS_DIR/${key_name}.key"
    local iv_file="$KEYS_DIR/${key_name}.iv"
    local salt_file="$KEYS_DIR/${key_name}.salt"
    
    # Generate random key
    openssl rand -hex $((KEY_SIZE / 8)) > "$key_file"
    
    # Generate random IV
    openssl rand -hex $IV_SIZE > "$iv_file"
    
    # Generate random salt
    openssl rand -hex $SALT_SIZE > "$salt_file"
    
    # Set secure permissions
    chmod 600 "$key_file" "$iv_file" "$salt_file"
    
    log_success "Generated encryption key: $key_name"
}

# Encrypt file
encrypt_file() {
    local input_file="$1"
    local output_file="$2"
    local key_name="$3"
    
    local key_file="$KEYS_DIR/${key_name}.key"
    local iv_file="$KEYS_DIR/${key_name}.iv"
    
    if [[ ! -f "$key_file" ]] || [[ ! -f "$iv_file" ]]; then
        log_error "Encryption key not found: $key_name"
        return 1
    fi
    
    local key=$(cat "$key_file")
    local iv=$(cat "$iv_file")
    
    # Encrypt file
    if openssl enc -$ENCRYPTION_ALGORITHM -in "$input_file" -out "$output_file" \
        -K "$key" -iv "$iv" -salt 2>/dev/null; then
        log_success "Encrypted: $(basename "$input_file")"
        return 0
    else
        log_error "Encryption failed: $(basename "$input_file")"
        return 1
    fi
}

# Decrypt file
decrypt_file() {
    local input_file="$1"
    local output_file="$2"
    local key_name="$3"
    
    local key_file="$KEYS_DIR/${key_name}.key"
    local iv_file="$KEYS_DIR/${key_name}.iv"
    
    if [[ ! -f "$key_file" ]] || [[ ! -f "$iv_file" ]]; then
        log_error "Encryption key not found: $key_name"
        return 1
    fi
    
    local key=$(cat "$key_file")
    local iv=$(cat "$iv_file")
    
    # Decrypt file
    if openssl enc -$ENCRYPTION_ALGORITHM -d -in "$input_file" -out "$output_file" \
        -K "$key" -iv "$iv" -salt 2>/dev/null; then
        log_success "Decrypted: $(basename "$input_file")"
        return 0
    else
        log_error "Decryption failed: $(basename "$input_file")"
        return 1
    fi
}

# Data masking functions
mask_pii_data() {
    local input_text="$1"
    local masked_text="$input_text"
    
    # Mask email addresses
    masked_text=$(echo "$masked_text" | sed -E 's/[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/***@***.***/g')
    
    # Mask phone numbers
    masked_text=$(echo "$masked_text" | sed -E 's/\b[0-9]{3}[-.]?[0-9]{3}[-.]?[0-9]{4}\b/***-***-****/g')
    
    # Mask credit card numbers
    masked_text=$(echo "$masked_text" | sed -E 's/\b[0-9]{4}[- ]?[0-9]{4}[- ]?[0-9]{4}[- ]?[0-9]{4}\b/****-****-****-****/g')
    
    # Mask SSN
    masked_text=$(echo "$masked_text" | sed -E 's/\b[0-9]{3}-[0-9]{2}-[0-9]{4}\b/***-**-****/g')
    
    # Mask IP addresses
    masked_text=$(echo "$masked_text" | sed -E 's/\b[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\b/***.***.***.***/g')
    
    echo "$masked_text"
}

# Anonymize data
anonymize_data() {
    local input_file="$1"
    local output_file="$2"
    
    # Create anonymized version
    {
        echo "=== Anonymized Data ==="
        echo "Original file: $(basename "$input_file")"
        echo "Anonymized at: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        # Process each line
        while IFS= read -r line; do
            # Replace names with anonymous IDs
            line=$(echo "$line" | sed -E 's/\b[A-Z][a-z]+ [A-Z][a-z]+\b/Anonymous_User/g')
            
            # Mask PII data
            line=$(mask_pii_data "$line")
            
            echo "$line"
        done < "$input_file"
        
    } > "$output_file"
    
    log_success "Anonymized: $(basename "$input_file")"
}

# Secure file deletion
secure_delete() {
    local file_path="$1"
    
    if [[ ! -f "$file_path" ]]; then
        log_warning "File not found for secure deletion: $file_path"
        return 1
    fi
    
    local file_size=$(stat -c%s "$file_path")
    
    # Overwrite with random data multiple times
    for ((i=1; i<=OVERWRITE_PASSES; i++)); do
        # Overwrite with random data
        dd if=/dev/urandom of="$file_path" bs=1M count=$((file_size / 1048576 + 1)) 2>/dev/null
        
        # Overwrite with zeros
        dd if=/dev/zero of="$file_path" bs=1M count=$((file_size / 1048576 + 1)) 2>/dev/null
        
        # Overwrite with ones
        dd if=/dev/zero of="$file_path" bs=1M count=$((file_size / 1048576 + 1)) 2>/dev/null
    done
    
    # Remove the file
    rm -f "$file_path"
    
    log_success "Securely deleted: $(basename "$file_path")"
}

# Create sample data
create_sample_data() {
    log_info "Creating sample data for encryption testing"
    
    # Create sample text file with PII
    cat > "$RESULTS_DIR/sample_data.txt" << 'EOF'
Customer Information:
Name: John Doe
Email: john.doe@example.com
Phone: 555-123-4567
SSN: 123-45-6789
Credit Card: 4111-1111-1111-1111
IP Address: 192.168.1.100

Account Details:
Account ID: ACC001
Balance: $1,234.56
Last Login: 2025-07-19 10:30:00

Sensitive Notes:
This customer has special privileges and should be handled with care.
Contact information should be kept confidential.
EOF
    
    # Create sample CSV file
    cat > "$RESULTS_DIR/sample_data.csv" << 'EOF'
id,name,email,phone,ssn,credit_card,ip_address
1,John Doe,john.doe@example.com,555-123-4567,123-45-6789,4111-1111-1111-1111,192.168.1.100
2,Jane Smith,jane.smith@example.com,555-987-6543,987-65-4321,5555-5555-5555-4444,192.168.1.101
3,Bob Johnson,bob.johnson@example.com,555-456-7890,456-78-9012,4444-3333-2222-1111,192.168.1.102
EOF
    
    # Create sample JSON file
    cat > "$RESULTS_DIR/sample_data.json" << 'EOF'
{
  "customers": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john.doe@example.com",
      "phone": "555-123-4567",
      "ssn": "123-45-6789",
      "credit_card": "4111-1111-1111-1111",
      "ip_address": "192.168.1.100"
    },
    {
      "id": 2,
      "name": "Jane Smith",
      "email": "jane.smith@example.com",
      "phone": "555-987-6543",
      "ssn": "987-65-4321",
      "credit_card": "5555-5555-5555-4444",
      "ip_address": "192.168.1.101"
    }
  ]
}
EOF
    
    log_success "Sample data created"
}

# Test encryption system
test_encryption() {
    log_info "Testing encryption system"
    
    # Generate encryption keys
    generate_key "data_key"
    generate_key "backup_key"
    
    # Test file encryption/decryption
    local test_files=("sample_data.txt" "sample_data.csv" "sample_data.json")
    
    for file in "${test_files[@]}"; do
        local input_file="$RESULTS_DIR/$file"
        local encrypted_file="$ENCRYPTED_DIR/${file}.enc"
        local decrypted_file="$DECRYPTED_DIR/${file}"
        
        if [[ -f "$input_file" ]]; then
            # Encrypt file
            if encrypt_file "$input_file" "$encrypted_file" "data_key"; then
                # Decrypt file
                if decrypt_file "$encrypted_file" "$decrypted_file" "data_key"; then
                    # Verify integrity
                    if diff "$input_file" "$decrypted_file" >/dev/null 2>&1; then
                        log_success "Encryption/decryption test passed for $file"
                    else
                        log_error "Encryption/decryption integrity check failed for $file"
                    fi
                fi
            fi
        fi
    done
}

# Test privacy protection
test_privacy_protection() {
    log_info "Testing privacy protection features"
    
    # Test data masking
    local original_text="Contact John Doe at john.doe@example.com or call 555-123-4567"
    local masked_text=$(mask_pii_data "$original_text")
    
    log_info "Original: $original_text"
    log_info "Masked: $masked_text"
    
    # Test anonymization
    local input_file="$RESULTS_DIR/sample_data.txt"
    local anonymized_file="$RESULTS_DIR/sample_data_anonymized.txt"
    
    if [[ -f "$input_file" ]]; then
        anonymize_data "$input_file" "$anonymized_file"
        log_success "Anonymization test completed"
    fi
}

# Generate encryption report
generate_report() {
    log_info "Generating encryption and privacy report"
    local report_file="$RESULTS_DIR/encryption_report.txt"
    
    {
        echo "=========================================="
        echo "DATA ENCRYPTION AND PRIVACY PROTECTION REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== Encryption Configuration ==="
        echo "Algorithm: $ENCRYPTION_ALGORITHM"
        echo "Key size: $KEY_SIZE bits"
        echo "IV size: $IV_SIZE bytes"
        echo "Salt size: $SALT_SIZE bytes"
        echo ""
        
        echo "=== Generated Keys ==="
        if [[ -d "$KEYS_DIR" ]]; then
            echo "Keys directory: $KEYS_DIR"
            echo "Key files:"
            ls -la "$KEYS_DIR"/*.key 2>/dev/null | while read -r line; do
                echo "  $line"
            done
        fi
        echo ""
        
        echo "=== Encrypted Files ==="
        if [[ -d "$ENCRYPTED_DIR" ]]; then
            echo "Encrypted files:"
            ls -la "$ENCRYPTED_DIR"/*.enc 2>/dev/null | while read -r line; do
                echo "  $line"
            done
        fi
        echo ""
        
        echo "=== Decrypted Files ==="
        if [[ -d "$DECRYPTED_DIR" ]]; then
            echo "Decrypted files:"
            ls -la "$DECRYPTED_DIR"/* 2>/dev/null | while read -r line; do
                echo "  $line"
            done
        fi
        echo ""
        
        echo "=== Privacy Protection ==="
        echo "Data masking enabled: $DATA_MASKING_ENABLED"
        echo "PII detection enabled: $PII_DETECTION_ENABLED"
        echo "Anonymization enabled: $ANONYMIZATION_ENABLED"
        echo "Secure delete enabled: $SECURE_DELETE_ENABLED"
        echo ""
        
        echo "=== File Statistics ==="
        local total_files=$(find "$RESULTS_DIR" -type f | wc -l)
        local encrypted_files=$(find "$ENCRYPTED_DIR" -name "*.enc" 2>/dev/null | wc -l)
        local decrypted_files=$(find "$DECRYPTED_DIR" -type f 2>/dev/null | wc -l)
        echo "Total files: $total_files"
        echo "Encrypted files: $encrypted_files"
        echo "Decrypted files: $decrypted_files"
        
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Encryption report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 5.2 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Create sample data
    create_sample_data
    
    # Test encryption system
    test_encryption
    
    # Test privacy protection
    test_privacy_protection
    
    # Generate comprehensive report
    generate_report
    
    log_success "Goal 5.2 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    acquire_lock
    main "$@"
fi 
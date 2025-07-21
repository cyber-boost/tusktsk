#!/bin/bash

# Goal 3.2 Implementation - File Synchronization and Version Control System
# Priority: Medium
# Description: Goal 2 for Bash agent a9 goal 3

set -euo pipefail

# Configuration
SCRIPT_NAME="goal_3_2"
LOG_FILE="/tmp/${SCRIPT_NAME}.log"
RESULTS_DIR="/tmp/goal_3_2_results"
CONFIG_FILE="/tmp/goal_3_2_config.conf"

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
    log_info "Creating file synchronization configuration"
    mkdir -p "$RESULTS_DIR"
    
    cat > "$CONFIG_FILE" << 'EOF'
# File Synchronization Configuration

# Source and destination directories
SOURCE_DIR="/tmp/goal_3_2_source"
DEST_DIR="/tmp/goal_3_2_dest"
BACKUP_DIR="/tmp/goal_3_2_backups"

# Synchronization settings
SYNC_MODE="bidirectional"
CONFLICT_RESOLUTION="newest"
CREATE_BACKUPS=true
VERIFY_SYNC=true
DRY_RUN=false

# File patterns to include/exclude
INCLUDE_PATTERNS=(
    "*.txt"
    "*.sh"
    "*.conf"
    "*.json"
)

EXCLUDE_PATTERNS=(
    "*.tmp"
    "*.log"
    "*.bak"
    ".git/*"
)

# Version control settings
VERSION_CONTROL=true
MAX_VERSIONS=10
VERSION_PREFIX="v"
EOF
    
    log_success "Configuration created"
}

create_sample_files() {
    log_info "Creating sample files for synchronization"
    
    # Create source directory structure
    mkdir -p "$SOURCE_DIR/documents"
    mkdir -p "$SOURCE_DIR/scripts"
    mkdir -p "$SOURCE_DIR/config"
    
    # Create sample text files
    cat > "$SOURCE_DIR/documents/readme.txt" << 'EOF'
# Sample Document
This is a sample document for file synchronization testing.

Version: 1.0
Created: 2025-07-19
Author: System Administrator

This document contains important information about the project.
EOF
    
    cat > "$SOURCE_DIR/documents/changelog.txt" << 'EOF'
# Changelog

## Version 1.1 (2025-07-19)
- Added new features
- Fixed bugs
- Improved performance

## Version 1.0 (2025-07-18)
- Initial release
- Basic functionality
EOF
    
    # Create sample script files
    cat > "$SOURCE_DIR/scripts/backup.sh" << 'EOF'
#!/bin/bash
# Backup script for file synchronization

echo "Starting backup process..."
# Backup logic here
echo "Backup completed successfully"
EOF
    
    cat > "$SOURCE_DIR/scripts/restore.sh" << 'EOF'
#!/bin/bash
# Restore script for file synchronization

echo "Starting restore process..."
# Restore logic here
echo "Restore completed successfully"
EOF
    
    # Create sample config files
    cat > "$SOURCE_DIR/config/settings.json" << 'EOF'
{
  "application": "File Sync System",
  "version": "1.0.0",
  "settings": {
    "auto_sync": true,
    "backup_enabled": true,
    "max_versions": 10
  }
}
EOF
    
    cat > "$SOURCE_DIR/config/app.conf" << 'EOF'
# Application Configuration
APP_NAME=FileSync
APP_VERSION=1.0.0
DEBUG_MODE=false
LOG_LEVEL=INFO
EOF
    
    # Create some temporary files that should be excluded
    echo "temporary data" > "$SOURCE_DIR/temp.tmp"
    echo "log data" > "$SOURCE_DIR/debug.log"
    
    log_success "Sample files created"
}

calculate_file_hash() {
    local file_path="$1"
    if [[ -f "$file_path" ]]; then
        sha256sum "$file_path" | cut -d' ' -f1
    else
        echo ""
    fi
}

create_file_index() {
    local directory="$1"
    local index_file="$2"
    
    log_info "Creating file index for $directory"
    
    {
        echo "# File Index for $directory"
        echo "# Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo ""
        
        find "$directory" -type f | while read -r file; do
            local relative_path="${file#$directory/}"
            local hash=$(calculate_file_hash "$file")
            local size=$(stat -c%s "$file" 2>/dev/null || echo "0")
            local modified=$(stat -c%Y "$file" 2>/dev/null || echo "0")
            
            echo "$relative_path|$hash|$size|$modified"
        done
    } > "$index_file"
    
    log_success "File index created: $index_file"
}

synchronize_files() {
    log_info "Starting file synchronization"
    
    # Create destination directory
    mkdir -p "$DEST_DIR"
    
    # Create file indexes
    local source_index="$RESULTS_DIR/source_index.txt"
    local dest_index="$RESULTS_DIR/dest_index.txt"
    
    create_file_index "$SOURCE_DIR" "$source_index"
    create_file_index "$DEST_DIR" "$dest_index"
    
    # Synchronize files
    local sync_count=0
    local conflict_count=0
    
    while IFS='|' read -r relative_path hash size modified; do
        if [[ -n "$relative_path" ]] && [[ "$relative_path" != "#"* ]]; then
            local source_file="$SOURCE_DIR/$relative_path"
            local dest_file="$DEST_DIR/$relative_path"
            
            # Check if file should be excluded
            local should_exclude=false
            for pattern in "${EXCLUDE_PATTERNS[@]}"; do
                if [[ "$relative_path" == $pattern ]]; then
                    should_exclude=true
                    break
                fi
            done
            
            if [[ "$should_exclude" == "false" ]]; then
                # Create destination directory if needed
                local dest_dir=$(dirname "$dest_file")
                mkdir -p "$dest_dir"
                
                # Check if destination file exists
                if [[ -f "$dest_file" ]]; then
                    local dest_hash=$(calculate_file_hash "$dest_file")
                    local dest_modified=$(stat -c%Y "$dest_file" 2>/dev/null || echo "0")
                    
                    if [[ "$hash" != "$dest_hash" ]]; then
                        # Conflict detected
                        ((conflict_count++))
                        log_warning "Conflict detected: $relative_path"
                        
                        if [[ "$CONFLICT_RESOLUTION" == "newest" ]]; then
                            if [[ $modified -gt $dest_modified ]]; then
                                cp "$source_file" "$dest_file"
                                log_info "Resolved conflict: copied newer version of $relative_path"
                            else
                                log_info "Resolved conflict: kept existing version of $relative_path"
                            fi
                        fi
                    fi
                else
                    # New file, copy it
                    cp "$source_file" "$dest_file"
                    ((sync_count++))
                    log_info "Copied new file: $relative_path"
                fi
            fi
        fi
    done < "$source_index"
    
    log_success "Synchronization completed: $sync_count files copied, $conflict_count conflicts resolved"
}

create_version_backup() {
    if [[ "$CREATE_BACKUPS" == "true" ]]; then
        log_info "Creating version backup"
        
        local timestamp=$(date '+%Y%m%d_%H%M%S')
        local version_dir="$BACKUP_DIR/$VERSION_PREFIX$timestamp"
        
        mkdir -p "$version_dir"
        
        # Copy all files to version directory
        cp -r "$SOURCE_DIR"/* "$version_dir/" 2>/dev/null || true
        
        # Create version metadata
        cat > "$version_dir/version_info.txt" << EOF
Version: $VERSION_PREFIX$timestamp
Created: $(date '+%Y-%m-%d %H:%M:%S')
Source: $SOURCE_DIR
Description: Automatic version backup
EOF
        
        log_success "Version backup created: $version_dir"
        
        # Cleanup old versions
        cleanup_old_versions
    fi
}

cleanup_old_versions() {
    if [[ "$VERSION_CONTROL" == "true" ]]; then
        log_info "Cleaning up old versions"
        
        # Count existing versions
        local version_count=$(find "$BACKUP_DIR" -maxdepth 1 -type d -name "${VERSION_PREFIX}*" | wc -l)
        
        if [[ $version_count -gt $MAX_VERSIONS ]]; then
            local to_delete=$((version_count - MAX_VERSIONS))
            
            # Find oldest versions and delete them
            find "$BACKUP_DIR" -maxdepth 1 -type d -name "${VERSION_PREFIX}*" -printf '%T@ %p\n' | \
                sort -n | head -$to_delete | while read -r timestamp path; do
                    rm -rf "$path"
                    log_info "Deleted old version: $(basename "$path")"
                done
        fi
        
        log_success "Version cleanup completed"
    fi
}

verify_synchronization() {
    if [[ "$VERIFY_SYNC" == "true" ]]; then
        log_info "Verifying synchronization"
        
        local verification_passed=true
        local source_index="$RESULTS_DIR/source_index.txt"
        local dest_index="$RESULTS_DIR/dest_index.txt"
        
        # Recreate indexes after sync
        create_file_index "$SOURCE_DIR" "$source_index"
        create_file_index "$DEST_DIR" "$dest_index"
        
        # Compare files
        while IFS='|' read -r relative_path hash size modified; do
            if [[ -n "$relative_path" ]] && [[ "$relative_path" != "#"* ]]; then
                local source_file="$SOURCE_DIR/$relative_path"
                local dest_file="$DEST_DIR/$relative_path"
                
                # Check if file should be excluded
                local should_exclude=false
                for pattern in "${EXCLUDE_PATTERNS[@]}"; do
                    if [[ "$relative_path" == $pattern ]]; then
                        should_exclude=true
                        break
                    fi
                done
                
                if [[ "$should_exclude" == "false" ]]; then
                    if [[ -f "$source_file" ]] && [[ -f "$dest_file" ]]; then
                        local source_hash=$(calculate_file_hash "$source_file")
                        local dest_hash=$(calculate_file_hash "$dest_file")
                        
                        if [[ "$source_hash" != "$dest_hash" ]]; then
                            log_error "Verification failed: $relative_path"
                            verification_passed=false
                        fi
                    elif [[ -f "$source_file" ]] && [[ ! -f "$dest_file" ]]; then
                        log_error "Verification failed: $relative_path not copied"
                        verification_passed=false
                    fi
                fi
            fi
        done < "$source_index"
        
        if [[ "$verification_passed" == "true" ]]; then
            log_success "Synchronization verification passed"
        else
            log_error "Synchronization verification failed"
            return 1
        fi
    fi
}

generate_sync_report() {
    log_info "Generating synchronization report"
    local report_file="$RESULTS_DIR/sync_report.txt"
    
    {
        echo "=========================================="
        echo "FILE SYNCHRONIZATION REPORT"
        echo "=========================================="
        echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Script: $SCRIPT_NAME"
        echo ""
        
        echo "=== Synchronization Summary ==="
        local source_files=$(find "$SOURCE_DIR" -type f | wc -l)
        local dest_files=$(find "$DEST_DIR" -type f | wc -l)
        local backup_versions=$(find "$BACKUP_DIR" -maxdepth 1 -type d -name "${VERSION_PREFIX}*" | wc -l)
        
        echo "Source files: $source_files"
        echo "Destination files: $dest_files"
        echo "Backup versions: $backup_versions"
        echo ""
        
        echo "=== Configuration ==="
        echo "Sync mode: $SYNC_MODE"
        echo "Conflict resolution: $CONFLICT_RESOLUTION"
        echo "Create backups: $CREATE_BACKUPS"
        echo "Verify sync: $VERIFY_SYNC"
        echo "Version control: $VERSION_CONTROL"
        echo "Max versions: $MAX_VERSIONS"
        echo ""
        
        echo "=== Include Patterns ==="
        for pattern in "${INCLUDE_PATTERNS[@]}"; do
            echo "- $pattern"
        done
        echo ""
        
        echo "=== Exclude Patterns ==="
        for pattern in "${EXCLUDE_PATTERNS[@]}"; do
            echo "- $pattern"
        done
        echo ""
        
        echo "=== Recent Versions ==="
        find "$BACKUP_DIR" -maxdepth 1 -type d -name "${VERSION_PREFIX}*" -printf '%T@ %p\n' | \
            sort -n | tail -5 | while read -r timestamp path; do
                echo "- $(basename "$path") ($(date -d @${timestamp%.*} '+%Y-%m-%d %H:%M:%S'))"
            done
        
        echo ""
        echo "=========================================="
        echo "END OF REPORT"
        echo "=========================================="
        
    } > "$report_file"
    
    log_success "Synchronization report generated: $report_file"
}

# Main execution function
main() {
    log_info "Starting Goal 3.2 implementation"
    
    # Load configuration
    if [[ -f "$CONFIG_FILE" ]]; then
        source "$CONFIG_FILE"
    else
        create_config
        source "$CONFIG_FILE"
    fi
    
    # Create sample files
    create_sample_files
    
    # Create version backup
    create_version_backup
    
    # Synchronize files
    synchronize_files
    
    # Verify synchronization
    verify_synchronization
    
    # Generate comprehensive report
    generate_sync_report
    
    log_success "Goal 3.2 implementation completed successfully"
    return 0
}

# Entry point
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi 
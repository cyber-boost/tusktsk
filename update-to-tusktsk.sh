#!/bin/bash

# Update script to change all tusklang.org references to tuskt.sk
# This script is SAFE - it creates backups and shows preview before changes

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
TARGET_DIR="/opt/tusklang/show/live"
BACKUP_DIR="/opt/tusklang/backups/$(date +%Y%m%d_%H%M%S)"
TEMP_FILE="/tmp/tusktsk_update.tmp"

# Print functions
print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

# Create backup
create_backup() {
    print_info "Creating backup at $BACKUP_DIR..."
    mkdir -p "$BACKUP_DIR"
    
    # Use rsync to preserve all attributes
    rsync -av --quiet "$TARGET_DIR/" "$BACKUP_DIR/"
    
    print_success "Backup created successfully"
}

# Preview changes
preview_changes() {
    print_info "Analyzing files for tusklang.org references..."
    
    # Count occurrences
    local count=$(grep -r "tusklang\.org" "$TARGET_DIR" 2>/dev/null | wc -l)
    local file_count=$(grep -rl "tusklang\.org" "$TARGET_DIR" 2>/dev/null | wc -l)
    
    echo
    echo "📊 Summary:"
    echo "  - Files with changes: $file_count"
    echo "  - Total occurrences: $count"
    echo
    
    # Show sample changes
    print_info "Sample of changes that will be made:"
    echo
    print_warning "Special handling:"
    echo "  • docs.tusklang.org → tuskt.sk/documents"
    echo "  • examples.tusklang.org → tuskt.sk/documents"
    echo "  • All other *.tusklang.org → *.tuskt.sk"
    echo
    grep -r "tusklang\.org" "$TARGET_DIR" 2>/dev/null | head -10 | while IFS=: read -r file line; do
        echo "  📄 $(basename "$file")"
        echo "     Before: $line"
        # Apply the same replacements as in update_files
        local after=$(echo "$line" | sed -e 's|https\?://docs\.tusklang\.org|https://tuskt.sk/documents|g' \
                                        -e 's|https\?://examples\.tusklang\.org|https://tuskt.sk/documents|g' \
                                        -e 's|docs\.tusklang\.org|tuskt.sk/documents|g' \
                                        -e 's|examples\.tusklang\.org|tuskt.sk/documents|g' \
                                        -e 's/tusklang\.org/tuskt.sk/g')
        echo "     After:  $after"
        echo
    done
    
    echo "  ... and $(($count - 10)) more changes"
    echo
}

# Perform the update
update_files() {
    print_info "Updating all tusklang.org references to tuskt.sk..."
    
    # Find all files with tusklang.org
    local files=$(grep -rl "tusklang\.org" "$TARGET_DIR" 2>/dev/null)
    local total=$(echo "$files" | wc -l)
    local current=0
    
    echo "$files" | while read -r file; do
        if [ -f "$file" ]; then
            current=$((current + 1))
            
            # Create temp file with replacements
            # First replace docs and examples subdomains to main site/documents
            sed -e 's|https\?://docs\.tusklang\.org|https://tuskt.sk/documents|g' \
                -e 's|https\?://examples\.tusklang\.org|https://tuskt.sk/documents|g' \
                -e 's|docs\.tusklang\.org|tuskt.sk/documents|g' \
                -e 's|examples\.tusklang\.org|tuskt.sk/documents|g' \
                -e 's/tusklang\.org/tuskt.sk/g' "$file" > "$TEMP_FILE"
            
            # Preserve permissions
            cp -p "$file" "$file.bak"
            mv "$TEMP_FILE" "$file"
            rm -f "$file.bak"
            
            # Progress indicator
            if [ $((current % 10)) -eq 0 ]; then
                echo -ne "\r  Progress: $current/$total files processed"
            fi
        fi
    done
    
    echo -ne "\r"
    print_success "Updated $total files successfully"
}

# Verify changes
verify_changes() {
    print_info "Verifying changes..."
    
    # Check if any tusklang.org references remain
    local remaining=$(grep -r "tusklang\.org" "$TARGET_DIR" 2>/dev/null | wc -l)
    
    if [ "$remaining" -eq 0 ]; then
        print_success "All tusklang.org references have been updated to tuskt.sk"
    else
        print_warning "Found $remaining remaining tusklang.org references"
    fi
    
    # Check for tuskt.sk references
    local new_refs=$(grep -r "tuskt\.sk" "$TARGET_DIR" 2>/dev/null | wc -l)
    print_info "Found $new_refs tuskt.sk references"
}

# Main execution
main() {
    echo "=== TuskLang to TuskTsk Domain Update Script ==="
    echo
    
    # Check if target directory exists
    if [ ! -d "$TARGET_DIR" ]; then
        print_error "Directory $TARGET_DIR does not exist"
        exit 1
    fi
    
    # Preview changes
    preview_changes
    
    # Confirm with user
    echo
    read -p "Do you want to proceed with these changes? [y/N] " -r
    echo
    
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_warning "Operation cancelled"
        exit 0
    fi
    
    # Create backup
    create_backup
    
    # Perform update
    update_files
    
    # Verify
    verify_changes
    
    echo
    print_success "Domain update completed!"
    echo
    echo "📌 Next steps:"
    echo "  1. Test the website at https://tuskt.sk"
    echo "  2. Verify all installation scripts work"
    echo "  3. Check that all subdomains resolve correctly"
    echo
    echo "💾 Backup location: $BACKUP_DIR"
    echo "   To restore: rsync -av $BACKUP_DIR/ $TARGET_DIR/"
    echo
}

# Run main function
main
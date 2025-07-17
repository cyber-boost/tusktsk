#!/bin/bash

# Quick fix for pkg/ directory domains
# Updates tusklang.org to tuskt.sk in all pkg files

PKG_DIR="/opt/tsk_git/pkg"

echo "🔄 Fixing domains in pkg/ directory..."

# Find all files with tusklang.org in pkg
find "$PKG_DIR" -type f -exec grep -l "tusklang\.org" {} \; | while read file; do
    echo "Updating: $file"
    
    # Create backup
    cp "$file" "$file.bak"
    
    # Replace domains
    sed -i 's/tusklang\.org/tuskt.sk/g' "$file"
    
    # Remove backup if successful
    rm "$file.bak"
done

echo "✅ Domain updates complete in pkg/ directory"

# Show remaining count
remaining=$(grep -r "tusklang\.org" "$PKG_DIR" | wc -l)
echo "Remaining tusklang.org references: $remaining"
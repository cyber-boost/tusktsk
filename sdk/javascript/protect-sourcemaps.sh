#!/bin/bash
# TuskLang JavaScript Source Map Protection Script
# Protects source maps and debugging information

set -e

SDK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROTECTED_DIR="$SDK_DIR/protected-maps"

echo "🔒 TuskLang JavaScript Source Map Protection"
echo "============================================"

# Create protected directory
mkdir -p "$PROTECTED_DIR"

# Protect source maps
echo "🔒 Protecting source maps..."

# Find and protect .map files
find "$SDK_DIR" -name "*.map" -type f | while read -r mapfile; do
    echo "Protecting: $mapfile"
    
    # Create protected version
    protected_file="$PROTECTED_DIR/$(basename "$mapfile").protected"
    
    # Encrypt source map
    php -r "
    require_once '$SDK_DIR/../php/lib/SourceMapProtector.php';
    use TuskPHP\License\SourceMapProtector;
    SourceMapProtector::init();
    SourceMapProtector::protectSourceMap('$mapfile', '$protected_file');
    "
    
    # Remove original map file
    rm "$mapfile"
done

# Remove source map references from JS files
echo "🧹 Removing source map references..."

find "$SDK_DIR" -name "*.js" -type f | while read -r jsfile; do
    echo "Cleaning: $jsfile"
    
    # Remove source map comments
    sed -i '/\/\/# sourceMappingURL=/d' "$jsfile"
    sed -i '/\/\*# sourceMappingURL=.*\*\//d' "$jsfile"
    
    # Remove source URL comments
    sed -i '/\/\/# sourceURL=/d' "$jsfile"
done

# Remove debug information
echo "🗑️  Removing debug information..."

find "$SDK_DIR" -name "*.js" -type f | while read -r jsfile; do
    echo "Cleaning debug: $jsfile"
    
    # Remove console.log statements
    sed -i '/console\.(log|debug|info|warn|error)/d' "$jsfile"
    
    # Remove debugger statements
    sed -i '/debugger;/d' "$jsfile"
done

echo "✅ JavaScript source map protection complete"
echo "Protected maps: $PROTECTED_DIR" 
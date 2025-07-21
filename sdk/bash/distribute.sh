#!/bin/bash
# TuskLang Bash SDK Distribution Script
# =====================================
# Prepares the Bash SDK for GitHub distribution
# Version: 1.0.0

set -e

# Configuration
VERSION=$(cat VERSION 2>/dev/null || echo "1.0.0")
RELEASE_NAME="tusklang-bash-sdk-${VERSION}"
RELEASE_DIR="release"
DIST_DIR="${RELEASE_DIR}/${RELEASE_NAME}"

echo "Preparing TuskLang Bash SDK v$VERSION for distribution..."

# Clean previous release
rm -rf $RELEASE_DIR
mkdir -p $DIST_DIR

# Copy core files
echo "Copying core files..."
for file in *.sh; do
    if [ -f "$file" ]; then
        cp "$file" "$DIST_DIR/"
        echo "  Copied: $file"
    fi
done

if [ -d "sdk" ]; then
    cp -r sdk/ "$DIST_DIR/"
    echo "  Copied: sdk/ directory"
fi

for file in README.md LICENSE VERSION install.sh uninstall.sh; do
    if [ -f "$file" ]; then
        cp "$file" "$DIST_DIR/"
        echo "  Copied: $file"
    fi
done

# Copy documentation
if [ -d "docs" ]; then
    cp -r docs/ $DIST_DIR/
fi

# Copy examples
if [ -d "examples" ]; then
    cp -r examples/ $DIST_DIR/
fi

# Make scripts executable
chmod +x $DIST_DIR/*.sh
chmod +x $DIST_DIR/sdk/**/*.sh

# Create distribution archives
echo "Creating distribution archives..."
cd $RELEASE_DIR

# Create tar.gz archive
tar -czf "$RELEASE_NAME.tar.gz" "$RELEASE_NAME/"

# Create zip archive
zip -r "$RELEASE_NAME.zip" "$RELEASE_NAME/"

# Create checksums
echo "Generating checksums..."
sha256sum "$RELEASE_NAME.tar.gz" > "$RELEASE_NAME.tar.gz.sha256"
sha256sum "$RELEASE_NAME.zip" > "$RELEASE_NAME.zip.sha256"

cd ..

# Display results
echo ""
echo "Distribution packages created:"
echo "  - $RELEASE_DIR/$RELEASE_NAME.tar.gz"
echo "  - $RELEASE_DIR/$RELEASE_NAME.zip"
echo "  - $RELEASE_DIR/$RELEASE_NAME.tar.gz.sha256"
echo "  - $RELEASE_DIR/$RELEASE_NAME.zip.sha256"
echo ""
echo "Package contents:"
ls -la $DIST_DIR/
echo ""
echo "Ready for GitHub release!" 
#!/bin/bash
# Comprehensive Go SDK Deployment with License Fix

set -euo pipefail

# Store the original directory
ORIGINAL_DIR="$(pwd)"

echo "🚀 Deploying Go SDK with License Fix..."

# Step 1: Fix license issues
./fix-license-deployment.sh

# Step 2: Build the package
echo "📦 Building Go SDK..."
go build -o bin/tusktsk .

# Step 3: Test the package
echo "🧪 Testing Go SDK..."
go test ./...

# Step 4: Deploy license validation
echo "🔐 Deploying license validation..."
cd license 
if [ -f "./deploy-license.sh" ]; then
    ./deploy-license.sh
else
    echo "License deployment script not found, skipping..."
fi
cd "$ORIGINAL_DIR"

# Step 5: Create deployment package
echo "📦 Creating deployment package..."
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
PACKAGE_NAME="tusktsk-go-sdk-${TIMESTAMP}.tar.gz"

# Create a temporary directory for packaging
TEMP_DIR=$(mktemp -d)
mkdir -p "$TEMP_DIR/tusktsk-go-sdk"

# Copy files to temp directory
echo "📁 Copying files to package..."
cp -r bin/ "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "bin/ not found, skipping..."
cp LICENSE* "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "LICENSE files not found, skipping..."
cp go.mod go.sum "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "go.mod/go.sum not found, skipping..."
cp main.go "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "main.go not found, skipping..."
cp -r license/ "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "license/ not found, skipping..."
cp -r pkg/ "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "pkg/ not found, skipping..."
cp -r internal/ "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "internal/ not found, skipping..."
cp -r docs/ "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "docs/ not found, skipping..."
cp README.md "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "README.md not found, skipping..."
cp CONTRIBUTING.md "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "CONTRIBUTING.md not found, skipping..."
cp INSTALL.md "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "INSTALL.md not found, skipping..."
cp RELEASE_NOTES.md "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "RELEASE_NOTES.md not found, skipping..."
cp DOCKER.md "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "DOCKER.md not found, skipping..."
cp Makefile "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "Makefile not found, skipping..."
cp Dockerfile "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "Dockerfile not found, skipping..."
cp docker-compose.yml "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "docker-compose.yml not found, skipping..."
cp .dockerignore "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo ".dockerignore not found, skipping..."
cp test-config.pnt "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "test-config.pnt not found, skipping..."
cp -r testdata/ "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo "testdata/ not found, skipping..."
cp -r .github/ "$TEMP_DIR/tusktsk-go-sdk/" 2>/dev/null || echo ".github/ not found, skipping..."

# Create the tar.gz package
echo "📦 Creating tar.gz package..."
cd "$TEMP_DIR"
tar -czf "$PACKAGE_NAME" tusktsk-go-sdk/

# Move package to original directory
mv "$PACKAGE_NAME" "$ORIGINAL_DIR/"

# Cleanup
cd "$ORIGINAL_DIR"
rm -rf "$TEMP_DIR"

echo "✅ Go SDK deployment with license fix completed!"
echo "📦 Package created: $PACKAGE_NAME"
echo "📊 Package size: $(du -h "$PACKAGE_NAME" | cut -f1)" 
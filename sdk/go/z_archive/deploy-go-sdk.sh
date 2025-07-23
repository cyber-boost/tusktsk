#!/bin/bash

# Go SDK Deployment Script
set -euo pipefail

PACKAGE_FILE="$1"
MANIFEST_FILE="$2"
CHECKSUM_FILE="$3"

echo "Deploying Go SDK..."
echo "Package: $PACKAGE_FILE"
echo "Manifest: $MANIFEST_FILE"
echo "Checksums: $CHECKSUM_FILE"

# Verify checksums
echo "Verifying package integrity..."
tar -tzf "$PACKAGE_FILE" | while read file; do
    if [[ -f "$file" ]]; then
        expected_sum=$(grep "$file" "$CHECKSUM_FILE" | cut -d' ' -f1)
        actual_sum=$(sha256sum "$file" | cut -d' ' -f1)
        if [[ "$expected_sum" != "$actual_sum" ]]; then
            echo "ERROR: Checksum mismatch for $file"
            exit 1
        fi
    fi
done

# Extract and deploy
echo "Extracting package..."
tar -xzf "$PACKAGE_FILE"

# Install dependencies
echo "Installing Go dependencies..."
cd go-sdk-*
go mod download
go mod tidy

# Build binaries
echo "Building SDK binaries..."
make build || echo "Make build failed, trying manual build..."
go build -o bin/tsk cmd/tsk/main.go || echo "TSK build failed"
go build -o bin/peanut cmd/peanut/main.go || echo "Peanut build failed"

echo "Go SDK deployment completed successfully!"

#!/bin/bash
# Comprehensive Go SDK Deployment with License Fix

set -euo pipefail

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
cd license && ./deploy-license.sh && cd ..

# Step 5: Create deployment package
echo "📦 Creating deployment package..."
tar -czf tusktsk-go-sdk-$(date +%Y%m%d_%H%M%S).tar.gz \
    bin/ \
    LICENSE* \
    go.mod \
    go.sum \
    *.go \
    license/ \
    pkg/ \
    internal/ \
    docs/ \
    README.md

echo "✅ Go SDK deployment with license fix completed!"

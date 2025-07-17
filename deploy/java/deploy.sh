#!/bin/bash

# Java Package Deployment Script
# Handles compilation issues and Maven Central deployment

set -e

echo "☕ Deploying Java package to Maven Central..."

SDK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/sdk/java"
VERSION="2.0.1"

cd "$SDK_DIR"

# Check if GITHUB_TOKEN is set
if [ -z "$GITHUB_TOKEN" ]; then
    echo "❌ GITHUB_TOKEN not set. Please set it before running this script."
    exit 1
fi

# Clean previous builds
echo "🧹 Cleaning previous builds..."
mvn clean

# Compile and test
echo "🔨 Compiling and testing..."
mvn compile test

# Deploy to Maven Central
echo "📤 Deploying to Maven Central..."
mvn clean deploy -P release

echo "✅ Java package deployed successfully!"
echo "View at: https://search.maven.org/artifact/sk.tuskt/tusktsk" 
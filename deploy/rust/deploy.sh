#!/bin/bash

# Rust Package Deployment Script
# Handles license and category field issues for crates.io

set -e

echo "🦀 Deploying Rust package to crates.io..."

SDK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/sdk/rust"
VERSION="2.0.1"

cd "$SDK_DIR"

# Check if CARGO_REGISTRY_TOKEN is set
if [ -z "$CARGO_REGISTRY_TOKEN" ]; then
    echo "❌ CARGO_REGISTRY_TOKEN not set. Please set it before running this script."
    exit 1
fi

# Clean previous builds
echo "🧹 Cleaning previous builds..."
cargo clean

# Build and test
echo "🔨 Building and testing..."
cargo build --release
cargo test

# Publish to crates.io
echo "📤 Publishing to crates.io..."
cargo publish --allow-dirty

echo "✅ Rust package deployed successfully!"
echo "View at: https://crates.io/crates/tusktsk" 
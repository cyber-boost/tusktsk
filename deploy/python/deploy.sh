#!/bin/bash

# Python Package Deployment Script
# Handles the LICENSE file metadata issue with PyPI

set -e

echo "🐍 Deploying Python package to PyPI..."

SDK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/sdk/python"
VERSION="2.0.1"

cd "$SDK_DIR"

# Remove LICENSE file completely to avoid metadata issues
if [ -f "LICENSE" ]; then
    echo "📋 Removing LICENSE file to avoid PyPI metadata issues..."
    rm LICENSE
    LICENSE_REMOVED=true
else
    LICENSE_REMOVED=false
fi

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf dist build tusktsk.egg-info

# Build package
echo "🔨 Building package..."
python3 -m build --sdist --wheel

# Upload to PyPI
echo "📤 Uploading to PyPI..."
python3 -m twine upload dist/* --verbose

# Restore LICENSE file from root
if [ "$LICENSE_REMOVED" = true ]; then
    echo "📋 Restoring LICENSE file from root..."
    cp ../../LICENSE .
fi

echo "✅ Python package deployed successfully!"
echo "View at: https://pypi.org/project/tusktsk/$VERSION/" 
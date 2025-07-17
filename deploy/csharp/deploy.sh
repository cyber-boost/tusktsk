#!/bin/bash

# C# Package Deployment Script
# Handles syntax issues and NuGet deployment

set -e

echo "🔷 Deploying C# package to NuGet..."

SDK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/sdk/csharp"
VERSION="2.0.1"

cd "$SDK_DIR"

# Check if NUGET_API_KEY is set
if [ -z "$NUGET_API_KEY" ]; then
    echo "❌ NUGET_API_KEY not set. Please set it before running this script."
    exit 1
fi

# Clean previous builds
echo "🧹 Cleaning previous builds..."
dotnet clean

# Restore dependencies
echo "📦 Restoring dependencies..."
dotnet restore

# Build
echo "🔨 Building package..."
dotnet build -c Release

# Pack
echo "📦 Packing package..."
dotnet pack -c Release

# Push to NuGet
echo "📤 Pushing to NuGet..."
dotnet nuget push bin/Release/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json

echo "✅ C# package deployed successfully!"
echo "View at: https://www.nuget.org/packages/TuskTsk" 
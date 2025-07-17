#!/bin/bash

# TuskLang Multi-Package Manager Deployment Script
# This script orchestrates deployment to all package managers

set -e

echo "🚀 TuskLang Package Deployment Manager"
echo "====================================="

# Configuration
VERSION="2.0.0"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SDK_DIR="$ROOT_DIR/sdk"
PKG_DIR="$ROOT_DIR/pkg"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if running in dry-run mode
DRY_RUN=${DRY_RUN:-false}

# Function to check prerequisites
check_prerequisites() {
    echo "📋 Checking prerequisites..."
    
    # Check for required tools
    local required_tools=("git" "npm" "composer" "python3" "gem" "cargo" "go" "mvn" "dotnet")
    local missing_tools=()
    
    for tool in "${required_tools[@]}"; do
        if ! command -v "$tool" &> /dev/null; then
            missing_tools+=("$tool")
        fi
    done
    
    if [ ${#missing_tools[@]} -ne 0 ]; then
        echo -e "${RED}❌ Missing required tools: ${missing_tools[*]}${NC}"
        echo "Please install missing tools before proceeding."
        exit 1
    fi
    
    echo -e "${GREEN}✅ All prerequisites met${NC}"
}

# Function to deploy PHP package
deploy_php() {
    echo -e "\n${YELLOW}📦 Deploying PHP package to Packagist...${NC}"
    cd "$SDK_DIR/php"
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would submit to Packagist"
        composer validate
    else
        # Packagist uses webhooks, just need to tag and push
        echo "PHP packages are auto-synced via Packagist webhook"
    fi
}

# Function to deploy npm package
deploy_npm() {
    echo -e "\n${YELLOW}📦 Deploying JavaScript package to npm...${NC}"
    cd "$SDK_DIR/javascript"
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would run npm publish"
        npm pack --dry-run
    else
        npm publish
    fi
}

# Function to deploy Python package
deploy_python() {
    echo -e "\n${YELLOW}📦 Deploying Python package to PyPI...${NC}"
    cd "$SDK_DIR/python"
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would build and upload to PyPI"
        python3 -m build --sdist --wheel
    else
        python3 -m build --sdist --wheel
        python3 -m twine upload dist/*
    fi
}

# Function to deploy Ruby gem
deploy_ruby() {
    echo -e "\n${YELLOW}📦 Deploying Ruby gem to RubyGems...${NC}"
    cd "$SDK_DIR/ruby"
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would build and push gem"
        gem build tusk_lang.gemspec
    else
        gem build tusk_lang.gemspec
        gem push tusk_lang-*.gem
    fi
}

# Function to deploy Rust crate
deploy_rust() {
    echo -e "\n${YELLOW}📦 Deploying Rust crate to crates.io...${NC}"
    cd "$SDK_DIR/rust"
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would run cargo publish"
        cargo publish --dry-run
    else
        cargo publish
    fi
}

# Function to deploy Go module
deploy_go() {
    echo -e "\n${YELLOW}📦 Publishing Go module...${NC}"
    cd "$SDK_DIR/go"
    
    # Go modules are published by tagging
    echo "Go modules are published via git tags"
    echo "Ensure you've tagged with: git tag sdk/go/v$VERSION"
}

# Function to deploy Java package
deploy_java() {
    echo -e "\n${YELLOW}📦 Deploying Java package to Maven Central...${NC}"
    cd "$SDK_DIR/java"
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would deploy to Maven Central"
        mvn clean verify
    else
        mvn clean deploy -P release
    fi
}

# Function to deploy C# package
deploy_csharp() {
    echo -e "\n${YELLOW}📦 Deploying C# package to NuGet...${NC}"
    cd "$SDK_DIR/csharp"
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would pack and push to NuGet"
        dotnet pack -c Release
    else
        dotnet pack -c Release
        dotnet nuget push bin/Release/*.nupkg --source https://api.nuget.org/v3/index.json
    fi
}

# Main deployment flow
main() {
    check_prerequisites
    
    echo -e "\n🎯 Starting deployment process..."
    echo "Version: $VERSION"
    echo "Dry Run: $DRY_RUN"
    
    # Ask for confirmation
    if [ "$DRY_RUN" = false ]; then
        read -p "Are you sure you want to deploy to all package managers? (y/N) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            echo "Deployment cancelled."
            exit 1
        fi
    fi
    
    # Deploy to each package manager
    deploy_php
    deploy_npm
    deploy_python
    deploy_ruby
    deploy_rust
    deploy_go
    deploy_java
    deploy_csharp
    
    echo -e "\n${GREEN}✅ Deployment process completed!${NC}"
    echo "Please verify all packages are available on their respective registries."
}

# Run main function
main "$@"
#!/bin/bash

# TuskLang Multi-Package Manager Deployment Script
# This script orchestrates deployment to all package managers

set -e

echo "🚀 TuskLang Package Deployment Manager"
echo "====================================="

# Configuration
VERSION="2.0.1"
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

# Required environment variables for deployment:
#   COMPOSER_TOKEN      - Packagist (main_token)
#   RUBYGEMS_API_KEY    - RubyGems
#   TWINE_PASSWORD      - PyPI
#   NPM_TOKEN           - npm
#   GITHUB_TOKEN        - GitHub (for Go modules, etc.)
#   NUGET_API_KEY       - NuGet
#   CARGO_REGISTRY_TOKEN- crates.io
#
# Example usage:
#   export COMPOSER_TOKEN=...; export RUBYGEMS_API_KEY=...; ... ./pkg/deploy-all.sh
#

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
    if [ -z "$COMPOSER_TOKEN" ]; then
        echo -e "${RED}❌ COMPOSER_TOKEN not set. Skipping Packagist deployment.${NC}"
        return
    fi
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would submit to Packagist"
        COMPOSER_ALLOW_SUPERUSER=1 composer validate
    else
        COMPOSER_ALLOW_SUPERUSER=1 composer config --global --auth http-basic.repo.packagist.com token "$COMPOSER_TOKEN"
        COMPOSER_ALLOW_SUPERUSER=1 composer validate
        echo "PHP packages are auto-synced via Packagist webhook - ensure git tag is pushed"
    fi
}

# Function to deploy npm package
deploy_npm() {
    echo -e "\n${YELLOW}📦 Deploying JavaScript package to npm...${NC}"
    cd "$SDK_DIR/javascript"
    if [ -z "$NPM_TOKEN" ]; then
        echo -e "${RED}❌ NPM_TOKEN not set. Skipping npm deployment.${NC}"
        return
    fi
    echo "//registry.npmjs.org/:_authToken=$NPM_TOKEN" > ~/.npmrc
    
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
    if [ -z "$TWINE_PASSWORD" ]; then
        echo -e "${RED}❌ TWINE_PASSWORD not set. Skipping PyPI deployment.${NC}"
        return
    fi
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would build and upload to PyPI"
        python3 -m build --sdist --wheel
    else
        python3 -m build --sdist --wheel
        python3 -m twine upload dist/* --verbose
    fi
}

# Function to deploy Ruby gem
deploy_ruby() {
    echo -e "\n${YELLOW}📦 Deploying Ruby gem to RubyGems...${NC}"
    cd "$SDK_DIR/ruby"
    if [ -z "$RUBYGEMS_API_KEY" ]; then
        echo -e "${RED}❌ RUBYGEMS_API_KEY not set. Skipping RubyGems deployment.${NC}"
        return
    fi
    mkdir -p /root/.gem
    echo -e "---\n:rubygems_api_key: $RUBYGEMS_API_KEY\n" > /root/.gem/credentials
    chmod 600 /root/.gem/credentials
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would build and push gem"
        gem build tusk_lang.gemspec
    else
        gem build tusk_lang.gemspec
        gem push tusktsk-2.0.1.gem
    fi
}

# Function to deploy Rust crate
deploy_rust() {
    echo -e "\n${YELLOW}📦 Deploying Rust crate to crates.io...${NC}"
    cd "$SDK_DIR/rust"
    if [ -z "$CARGO_REGISTRY_TOKEN" ]; then
        echo -e "${RED}❌ CARGO_REGISTRY_TOKEN not set. Skipping crates.io deployment.${NC}"
        return
    fi
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would run cargo publish"
        cargo publish --dry-run --allow-dirty
    else
        cargo publish --allow-dirty
    fi
}

# Function to deploy Go module
deploy_go() {
    echo -e "\n${YELLOW}📦 Publishing Go module...${NC}"
    cd "$SDK_DIR/go"
    if [ -z "$GITHUB_TOKEN" ]; then
        echo -e "${RED}❌ GITHUB_TOKEN not set. Skipping Go module publish (requires git push).${NC}"
        return
    fi
    git config --global url."https://$GITHUB_TOKEN@github.com/".insteadOf "https://github.com/"
    
    # Go modules are published by tagging
    echo "Go modules are published via git tags"
    echo "Ensure you've tagged with: git tag sdk/go/v$VERSION"
    # Optionally push tags: git push --tags
}

# Function to deploy Java package
deploy_java() {
    echo -e "\n${YELLOW}📦 Deploying Java package to Maven Central...${NC}"
    cd "$SDK_DIR/java"
    if [ -z "$GITHUB_TOKEN" ]; then
        echo -e "${RED}❌ GITHUB_TOKEN not set. Skipping Maven deployment.${NC}"
        return
    fi
    
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
    if [ -z "$NUGET_API_KEY" ]; then
        echo -e "${RED}❌ NUGET_API_KEY not set. Skipping NuGet deployment.${NC}"
        return
    fi
    
    if [ "$DRY_RUN" = true ]; then
        echo "DRY RUN: Would pack and push to NuGet"
        dotnet pack -c Release
    else
        dotnet pack -c Release
        dotnet nuget push bin/Release/*.nupkg --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
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
    # deploy_php  # Skipped, already successful
    echo -e "\n${YELLOW}⚠️  Skipping PHP (Packagist) deployment: already successful.${NC}"
    # deploy_npm  # Skipped, already successful
    echo -e "\n${YELLOW}⚠️  Skipping JavaScript (npm) deployment: already successful.${NC}"
    deploy_python
    # deploy_ruby  # Skipped, already successful
    echo -e "\n${YELLOW}⚠️  Skipping Ruby (RubyGems) deployment: already successful.${NC}"
    deploy_rust
    deploy_go
    # deploy_java  # Skipped, has compilation issues
    echo -e "\n${YELLOW}⚠️  Skipping Java (Maven) deployment: has compilation issues.${NC}"
    # deploy_csharp  # Skipped, has compilation issues
    echo -e "\n${YELLOW}⚠️  Skipping C# (NuGet) deployment: has compilation issues.${NC}"
    
    echo -e "\n${GREEN}✅ Deployment process completed!${NC}"
    echo "Please verify all packages are available on their respective registries."
    echo -e "\n${YELLOW}📝 Notes:${NC}"
    echo "- Rust crates.io requires email verification. If deployment fails, verify your email at https://crates.io/settings/profile"
    echo "- Go modules require manual git tag push: git tag sdk/go/v$VERSION && git push --tags"
}

# Run main function
main "$@"
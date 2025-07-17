#!/bin/bash

# Master Deployment Coordinator
# Orchestrates deployment to all package managers

set -e

echo "🚀 TuskLang Master Deployment Coordinator"
echo "========================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

VERSION="2.0.1"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Deployment status tracking
declare -A deployment_status
deployment_status["php"]="skipped"
deployment_status["npm"]="skipped"
deployment_status["python"]="pending"
deployment_status["ruby"]="skipped"
deployment_status["rust"]="pending"
deployment_status["go"]="pending"
deployment_status["java"]="pending"
deployment_status["csharp"]="pending"

# Function to print status
print_status() {
    local package=$1
    local status=$2
    local color=$3
    echo -e "${color}${package}: ${status}${NC}"
}

# Function to deploy a package
deploy_package() {
    local package=$1
    local script_path="$SCRIPT_DIR/$package/deploy.sh"
    
    if [ ! -f "$script_path" ]; then
        echo -e "${RED}❌ No deployment script found for $package${NC}"
        deployment_status["$package"]="failed"
        return 1
    fi
    
    echo -e "\n${BLUE}📦 Deploying $package package...${NC}"
    
    if bash "$script_path"; then
        echo -e "${GREEN}✅ $package deployment successful!${NC}"
        deployment_status["$package"]="success"
    else
        echo -e "${RED}❌ $package deployment failed!${NC}"
        deployment_status["$package"]="failed"
        return 1
    fi
}

# Function to deploy Go module (special case)
deploy_go() {
    echo -e "\n${BLUE}🐹 Deploying Go module...${NC}"
    
    if [ -z "$GITHUB_TOKEN" ]; then
        echo -e "${RED}❌ GITHUB_TOKEN not set. Skipping Go deployment.${NC}"
        deployment_status["go"]="skipped"
        return
    fi
    
    cd "$(cd "$SCRIPT_DIR/../.." && pwd)/sdk/go"
    
    # Check if tag exists
    if git tag -l "sdk/go/v$VERSION" | grep -q "sdk/go/v$VERSION"; then
        echo -e "${YELLOW}⚠️  Tag sdk/go/v$VERSION already exists. Skipping.${NC}"
        deployment_status["go"]="skipped"
        return
    fi
    
    # Create and push tag
    git tag "sdk/go/v$VERSION"
    git push origin "sdk/go/v$VERSION"
    
    echo -e "${GREEN}✅ Go module deployed successfully!${NC}"
    deployment_status["go"]="success"
}

# Main deployment flow
main() {
    echo "Version: $VERSION"
    echo "Script Directory: $SCRIPT_DIR"
    
    # Check for required environment variables
    echo -e "\n${YELLOW}🔍 Checking environment variables...${NC}"
    
    local missing_vars=()
    
    if [ -z "$TWINE_PASSWORD" ]; then
        missing_vars+=("TWINE_PASSWORD (PyPI)")
    fi
    
    if [ -z "$CARGO_REGISTRY_TOKEN" ]; then
        missing_vars+=("CARGO_REGISTRY_TOKEN (crates.io)")
    fi
    
    if [ -z "$GITHUB_TOKEN" ]; then
        missing_vars+=("GITHUB_TOKEN (Go modules, Maven)")
    fi
    
    if [ -z "$NUGET_API_KEY" ]; then
        missing_vars+=("NUGET_API_KEY (NuGet)")
    fi
    
    if [ ${#missing_vars[@]} -ne 0 ]; then
        echo -e "${YELLOW}⚠️  Missing environment variables:${NC}"
        for var in "${missing_vars[@]}"; do
            echo -e "${YELLOW}   - $var${NC}"
        done
        echo -e "${YELLOW}   Packages requiring these tokens will be skipped.${NC}"
    fi
    
    # Ask for confirmation
    echo -e "\n${YELLOW}⚠️  Ready to deploy TuskLang v$VERSION to all package managers.${NC}"
    read -p "Are you sure you want to proceed? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Deployment cancelled."
        exit 1
    fi
    
    echo -e "\n${GREEN}🎯 Starting deployment process...${NC}"
    
    # Deploy packages
    deploy_package "python" || true
    deploy_package "rust" || true
    deploy_package "java" || true
    deploy_package "csharp" || true
    deploy_go || true
    
    # Print final status
    echo -e "\n${BLUE}📊 Deployment Summary:${NC}"
    echo "========================"
    
    for package in "${!deployment_status[@]}"; do
        case "${deployment_status[$package]}" in
            "success")
                print_status "$package" "✅ SUCCESS" "$GREEN"
                ;;
            "failed")
                print_status "$package" "❌ FAILED" "$RED"
                ;;
            "skipped")
                print_status "$package" "⏭️  SKIPPED" "$YELLOW"
                ;;
            *)
                print_status "$package" "❓ UNKNOWN" "$RED"
                ;;
        esac
    done
    
    # Count results
    local success_count=0
    local failed_count=0
    local skipped_count=0
    
    for status in "${deployment_status[@]}"; do
        case "$status" in
            "success") ((success_count++)) ;;
            "failed") ((failed_count++)) ;;
            "skipped") ((skipped_count++)) ;;
        esac
    done
    
    echo -e "\n${BLUE}📈 Results:${NC}"
    echo -e "${GREEN}✅ Successful: $success_count${NC}"
    echo -e "${RED}❌ Failed: $failed_count${NC}"
    echo -e "${YELLOW}⏭️  Skipped: $skipped_count${NC}"
    
    if [ $failed_count -eq 0 ]; then
        echo -e "\n${GREEN}🎉 All attempted deployments completed successfully!${NC}"
    else
        echo -e "\n${YELLOW}⚠️  Some deployments failed. Check the logs above for details.${NC}"
    fi
    
    echo -e "\n${BLUE}🔗 Package Links:${NC}"
    echo "Python: https://pypi.org/project/tusktsk/$VERSION/"
    echo "Rust: https://crates.io/crates/tusktsk"
    echo "Go: https://github.com/cyber-boost/tusktsk/releases/tag/sdk/go/v$VERSION"
    echo "Java: https://search.maven.org/artifact/sk.tuskt/tusktsk"
    echo "C#: https://www.nuget.org/packages/TuskTsk"
}

# Run main function
main "$@" 
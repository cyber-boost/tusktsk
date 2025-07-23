#!/bin/bash

# Go Module Publisher for GitHub Packages
# This script helps publish Go modules to GitHub Packages

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Configuration
MODULE_NAME="github.com/cyber-boost/tusktsk"
DEFAULT_VERSION="1.0.0"

# Check for help flag
if [[ "$1" == "--help" || "$1" == "-h" ]]; then
    echo "Usage: $0 [VERSION]"
    echo ""
    echo "Publishes the Go module to GitHub Packages"
    echo ""
    echo "Arguments:"
    echo "  VERSION    Version to publish (e.g., 1.0.0). Default: $DEFAULT_VERSION"
    echo ""
    echo "Examples:"
    echo "  $0              # Publish version $DEFAULT_VERSION"
    echo "  $0 1.0.0        # Publish version 1.0.0"
    echo "  $0 1.1.0        # Publish version 1.1.0"
    echo ""
    echo "The script will:"
    echo "  1. Validate the Go module"
    echo "  2. Run tests"
    echo "  3. Build the project"
    echo "  4. Create and push a git tag"
    echo "  5. Trigger GitHub Actions for publishing"
    exit 0
fi

# Get version from command line or use default
VERSION=${1:-$DEFAULT_VERSION}

log_info "Publishing Go module: $MODULE_NAME"
log_info "Version: v$VERSION"

# Validate version format
if [[ ! $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    log_error "Invalid version format. Use semantic versioning (e.g., 1.0.0)"
    exit 1
fi

# Check if we're in a git repository
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    log_error "Not in a git repository. Please run this from the project root."
    exit 1
fi

# Check if we have uncommitted changes
if ! git diff-index --quiet HEAD --; then
    log_warning "You have uncommitted changes. Please commit or stash them first."
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Validate go.mod
log_info "Validating go.mod file..."
if ! go mod download; then
    log_error "Failed to download dependencies"
    exit 1
fi

if ! go mod verify; then
    log_error "Failed to verify go.mod"
    exit 1
fi

go mod tidy

# Run tests
log_info "Running tests..."
if ! go test ./...; then
    log_error "Tests failed"
    exit 1
fi

# Build
log_info "Building..."
if ! go build -v ./...; then
    log_error "Build failed"
    exit 1
fi

# Check if tag already exists
if git rev-parse "v$VERSION" >/dev/null 2>&1; then
    log_warning "Tag v$VERSION already exists"
    read -p "Delete existing tag and recreate? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        git tag -d "v$VERSION"
        git push origin ":refs/tags/v$VERSION" 2>/dev/null || true
    else
        log_info "Using existing tag v$VERSION"
    fi
fi

# Create and push tag
log_info "Creating tag v$VERSION..."
git tag -a "v$VERSION" -m "Release v$VERSION"
git push origin "v$VERSION"

log_success "Tag v$VERSION created and pushed"

# Instructions for manual publishing
log_info "Next steps:"
echo "1. The GitHub Action will automatically trigger when the tag is pushed"
echo "2. Check the Actions tab in your GitHub repository"
echo "3. The module will be available at: $MODULE_NAME@v$VERSION"
echo ""
echo "To install the published module:"
echo "  go get $MODULE_NAME@v$VERSION"
echo ""
echo "To check if it's published:"
echo "  go list -m $MODULE_NAME@v$VERSION"

log_success "Go module publishing process initiated!" 
#!/bin/bash

# Go Module Verification Script
# This script verifies that the Go module is properly configured for publishing

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
EXPECTED_MODULE_PATH="github.com/cyber-boost/tusktsk"

echo "🔍 Verifying Go Module Configuration"
echo "====================================="

# Check 1: Verify go.mod exists and has correct module path
log_info "Checking go.mod file..."
if [[ ! -f "go.mod" ]]; then
    log_error "go.mod file not found"
    exit 1
fi

MODULE_PATH=$(grep "^module " go.mod | cut -d' ' -f2)
if [[ "$MODULE_PATH" != "$EXPECTED_MODULE_PATH" ]]; then
    log_error "Incorrect module path in go.mod: $MODULE_PATH (expected: $EXPECTED_MODULE_PATH)"
    exit 1
fi
log_success "go.mod file is correct"

# Check 2: Verify no old import paths exist
log_info "Checking for old import paths..."
OLD_IMPORTS=$(grep -r "github.com/cyber-boost/tusktsk/sdk/go" *.go pkg/*.go internal/*.go 2>/dev/null || true)
if [[ -n "$OLD_IMPORTS" ]]; then
    log_error "Found old import paths:"
    echo "$OLD_IMPORTS"
    exit 1
fi
log_success "No old import paths found"

# Check 3: Verify go mod tidy works
log_info "Running go mod tidy..."
if ! go mod tidy; then
    log_error "go mod tidy failed"
    exit 1
fi
log_success "go mod tidy completed successfully"

# Check 4: Verify build works
log_info "Building the project..."
if ! go build -v .; then
    log_error "Build failed"
    exit 1
fi
log_success "Build completed successfully"

# Check 5: Verify tests pass
log_info "Running tests..."
if ! go test ./...; then
    log_error "Tests failed"
    exit 1
fi
log_success "All tests passed"

# Check 6: Verify GitHub Actions workflow exists
log_info "Checking GitHub Actions workflow..."
if [[ ! -f ".github/workflows/publish.yml" ]]; then
    log_error "GitHub Actions workflow not found"
    exit 1
fi
log_success "GitHub Actions workflow exists"

# Check 7: Verify publishing script exists and is executable
log_info "Checking publishing script..."
if [[ ! -f "publish-go-module.sh" ]]; then
    log_error "Publishing script not found"
    exit 1
fi
if [[ ! -x "publish-go-module.sh" ]]; then
    log_error "Publishing script is not executable"
    exit 1
fi
log_success "Publishing script is ready"

# Check 8: Verify git repository
log_info "Checking git repository..."
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    log_error "Not in a git repository"
    exit 1
fi
log_success "Git repository is configured"

# Check 9: Verify remote origin
log_info "Checking git remote..."
if ! git remote get-url origin > /dev/null 2>&1; then
    log_warning "No origin remote configured"
else
    ORIGIN_URL=$(git remote get-url origin)
    log_success "Origin remote: $ORIGIN_URL"
fi

echo ""
echo "✅ All verifications passed!"
echo ""
echo "📦 Your Go module is ready for publishing:"
echo "   Module: $MODULE_NAME"
echo "   Path: $MODULE_PATH"
echo ""
echo "🚀 To publish:"
echo "   ./publish-go-module.sh 1.0.0"
echo ""
echo "📋 After publishing, users can install with:"
echo "   go get $MODULE_NAME@v1.0.0"
echo ""
echo "🎯 The module will appear in:"
echo "   - GitHub Packages tab"
echo "   - GitHub Actions for automated publishing"
echo "   - GitHub Releases for documentation" 
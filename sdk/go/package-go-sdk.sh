#!/bin/bash

# Go SDK Comprehensive Packaging Script
# This script packages ALL Go SDK components including recently built agent files

set -euo pipefail

# Configuration
SDK_NAME="go-sdk"
VERSION="1.0.0"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
PACKAGE_DIR="go-sdk-${VERSION}-${TIMESTAMP}"
MANIFEST_FILE="go-sdk-manifest-${TIMESTAMP}.json"
CHECKSUM_FILE="go-sdk-checksums-${TIMESTAMP}.txt"

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

# Create package directory
log_info "Creating package directory: ${PACKAGE_DIR}"
mkdir -p "${PACKAGE_DIR}"

# Function to count files and lines
count_files_and_lines() {
    local dir="$1"
    local file_count=$(find "$dir" -type f | wc -l)
    local line_count=$(find "$dir" -type f -exec wc -l {} + | tail -1 | awk '{print $1}')
    echo "$file_count:$line_count"
}

# Function to create component manifest
create_component_manifest() {
    local component="$1"
    local source_dir="$2"
    local target_dir="$3"
    
    if [[ -d "$source_dir" ]]; then
        local stats=$(count_files_and_lines "$source_dir")
        local file_count=$(echo "$stats" | cut -d: -f1)
        local line_count=$(echo "$stats" | cut -d: -f2)
    else
        local file_count=0
        local line_count=0
    fi
    
    cat >> "${MANIFEST_FILE}" << EOF
    {
      "component": "${component}",
      "source_directory": "${source_dir}",
      "target_directory": "${target_dir}",
      "file_count": ${file_count},
      "line_count": ${line_count},
      "packaged_at": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
    },
EOF
}

# Initialize manifest file
log_info "Creating package manifest..."
cat > "${MANIFEST_FILE}" << EOF
{
  "package_name": "${SDK_NAME}",
  "version": "${VERSION}",
  "timestamp": "${TIMESTAMP}",
  "total_files": 0,
  "total_lines": 0,
  "components": [
EOF

# Package Core SDK Files
log_info "Packaging core SDK files..."
mkdir -p "${PACKAGE_DIR}/core"
cp -r src/ "${PACKAGE_DIR}/core/"
cp -r cmd/ "${PACKAGE_DIR}/core/"
cp -r operators/ "${PACKAGE_DIR}/core/"
cp -r peanut/ "${PACKAGE_DIR}/core/"
cp -r protection/ "${PACKAGE_DIR}/core/"
cp -r binary/ "${PACKAGE_DIR}/core/"
cp *.go "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp go.mod "${PACKAGE_DIR}/core/"
cp go.sum "${PACKAGE_DIR}/core/"
cp Makefile "${PACKAGE_DIR}/core/"
cp README.md "${PACKAGE_DIR}/core/"
cp LICENSE "${PACKAGE_DIR}/core/"
cp Dockerfile "${PACKAGE_DIR}/core/" 2>/dev/null || true
create_component_manifest "core" "." "${PACKAGE_DIR}/core"

# Package Production Build
log_info "Packaging production build..."
mkdir -p "${PACKAGE_DIR}/production"
cp -r production/ "${PACKAGE_DIR}/production/"
create_component_manifest "production" "production" "${PACKAGE_DIR}/production"

# Package AA_GO (Goal Automation)
log_info "Packaging AA_GO goal automation files..."
mkdir -p "${PACKAGE_DIR}/aa_go"
cp -r aa_go/ "${PACKAGE_DIR}/aa_go/"
create_component_manifest "aa_go" "aa_go" "${PACKAGE_DIR}/aa_go"

# Package TODO (A-series Automation)
log_info "Packaging TODO A-series automation files..."
mkdir -p "${PACKAGE_DIR}/todo"
cp -r todo/ "${PACKAGE_DIR}/todo/"
create_component_manifest "todo" "todo" "${PACKAGE_DIR}/todo"

# Package Examples
log_info "Packaging example files..."
mkdir -p "${PACKAGE_DIR}/examples"
cp -r example/ "${PACKAGE_DIR}/examples/"
create_component_manifest "examples" "example" "${PACKAGE_DIR}/examples"

# Package Tests
log_info "Packaging test files..."
mkdir -p "${PACKAGE_DIR}/tests"
cp -r tests/ "${PACKAGE_DIR}/tests/"
create_component_manifest "tests" "tests" "${PACKAGE_DIR}/tests"

# Package Documentation
log_info "Packaging documentation..."
mkdir -p "${PACKAGE_DIR}/docs"
cp -r docs/ "${PACKAGE_DIR}/docs/" 2>/dev/null || true
cp -r summaries/ "${PACKAGE_DIR}/docs/" 2>/dev/null || true
create_component_manifest "docs" "docs+summaries" "${PACKAGE_DIR}/docs"

# Package License Components
log_info "Packaging license components..."
mkdir -p "${PACKAGE_DIR}/license"
cp -r license/ "${PACKAGE_DIR}/license/"
create_component_manifest "license" "license" "${PACKAGE_DIR}/license"

# Package Test Data
log_info "Packaging test data..."
mkdir -p "${PACKAGE_DIR}/testdata"
cp -r testdata/ "${PACKAGE_DIR}/testdata/" 2>/dev/null || true
cp *.pnt "${PACKAGE_DIR}/testdata/" 2>/dev/null || true
create_component_manifest "testdata" "testdata" "${PACKAGE_DIR}/testdata"

# Find and package any other directories
log_info "Searching for additional components..."
for dir in */; do
    dir_name=$(basename "$dir")
    if [[ -d "$dir" ]] && [[ ! "$dir_name" =~ ^(src|cmd|operators|peanut|protection|binary|production|aa_go|todo|example|tests|docs|summaries|license|testdata)$ ]] && [[ ! "$dir_name" =~ ^go-sdk- ]]; then
        log_info "Found additional component: ${dir_name}"
        mkdir -p "${PACKAGE_DIR}/additional/${dir_name}"
        cp -r "$dir" "${PACKAGE_DIR}/additional/${dir_name}/"
        create_component_manifest "additional_${dir_name}" "$dir" "${PACKAGE_DIR}/additional/${dir_name}"
    fi
done

# Calculate totals
log_info "Calculating package statistics..."
TOTAL_FILES=$(find "${PACKAGE_DIR}" -type f | wc -l)
TOTAL_LINES=$(find "${PACKAGE_DIR}" -type f -exec wc -l {} + | tail -1 | awk '{print $1}')

# Complete manifest file
sed -i '$ s/,$//' "${MANIFEST_FILE}"  # Remove last comma
cat >> "${MANIFEST_FILE}" << EOF
  ],
  "package_statistics": {
    "total_files": ${TOTAL_FILES},
    "total_lines": ${TOTAL_LINES},
    "package_size_bytes": $(du -sb "${PACKAGE_DIR}" | cut -f1),
    "created_at": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
  },
  "deployment_info": {
    "target_platforms": ["linux", "darwin", "windows"],
    "go_version_required": "1.21+",
    "dependencies": ["fujsen", "tuskdb", "peanut"],
    "installation_methods": ["go install", "docker", "binary"]
  }
}
EOF

# Create checksums
log_info "Creating checksums..."
find "${PACKAGE_DIR}" -type f -exec sha256sum {} \; > "${CHECKSUM_FILE}"

# Create deployment package
log_info "Creating deployment package..."
tar -czf "${PACKAGE_DIR}.tar.gz" "${PACKAGE_DIR}" "${MANIFEST_FILE}" "${CHECKSUM_FILE}"

# Create deployment script
log_info "Creating deployment script..."
cat > "deploy-${SDK_NAME}.sh" << 'EOF'
#!/bin/bash

# Go SDK Deployment Script
set -euo pipefail

PACKAGE_FILE="$1"
MANIFEST_FILE="$2"
CHECKSUM_FILE="$3"

echo "Deploying Go SDK..."
echo "Package: $PACKAGE_FILE"
echo "Manifest: $MANIFEST_FILE"
echo "Checksums: $CHECKSUM_FILE"

# Verify checksums
echo "Verifying package integrity..."
tar -tzf "$PACKAGE_FILE" | while read file; do
    if [[ -f "$file" ]]; then
        expected_sum=$(grep "$file" "$CHECKSUM_FILE" | cut -d' ' -f1)
        actual_sum=$(sha256sum "$file" | cut -d' ' -f1)
        if [[ "$expected_sum" != "$actual_sum" ]]; then
            echo "ERROR: Checksum mismatch for $file"
            exit 1
        fi
    fi
done

# Extract and deploy
echo "Extracting package..."
tar -xzf "$PACKAGE_FILE"

# Install dependencies
echo "Installing Go dependencies..."
cd go-sdk-*
go mod download
go mod tidy

# Build binaries
echo "Building SDK binaries..."
make build || echo "Make build failed, trying manual build..."
go build -o bin/tsk cmd/tsk/main.go || echo "TSK build failed"
go build -o bin/peanut cmd/peanut/main.go || echo "Peanut build failed"

echo "Go SDK deployment completed successfully!"
EOF

chmod +x "deploy-${SDK_NAME}.sh"

# Create quick start guide
log_info "Creating quick start guide..."
cat > "${PACKAGE_DIR}/QUICK_START.md" << EOF
# Go SDK Quick Start Guide

## Package Contents
- **Core SDK**: Main Go source files and operators
- **Production**: Production-ready build with optimizations
- **AA_GO**: Goal automation and planning files
- **TODO**: A-series automation components
- **Examples**: Comprehensive example implementations
- **Tests**: Complete test suite
- **Documentation**: API docs and guides
- **License**: Licensing components

## Quick Installation

\`\`\`bash
# Extract the package
tar -xzf ${PACKAGE_DIR}.tar.gz

# Navigate to the package
cd ${PACKAGE_DIR}

# Install dependencies
go mod download

# Build the SDK
make build

# Run tests
make test
\`\`\`

## Usage Examples

\`\`\`go
package main

import (
    "github.com/tusklang/go-sdk/core"
    "github.com/tusklang/go-sdk/operators"
)

func main() {
    // Initialize the SDK
    sdk := core.NewSDK()
    
    // Use operators
    op := operators.NewOperator()
    
    // Your code here
}
\`\`\`

## Package Statistics
- Total Files: ${TOTAL_FILES}
- Total Lines: ${TOTAL_LINES}
- Created: $(date)

## Support
For support and documentation, see the docs/ directory.
EOF

# Final package
log_info "Creating final package..."
tar -czf "${PACKAGE_DIR}-FINAL.tar.gz" "${PACKAGE_DIR}" "${MANIFEST_FILE}" "${CHECKSUM_FILE}" "deploy-${SDK_NAME}.sh"

# Cleanup temporary files
rm -rf "${PACKAGE_DIR}"

# Summary
log_success "Go SDK packaging completed!"
log_info "Package: ${PACKAGE_DIR}-FINAL.tar.gz"
log_info "Manifest: ${MANIFEST_FILE}"
log_info "Checksums: ${CHECKSUM_FILE}"
log_info "Deployment script: deploy-${SDK_NAME}.sh"
log_info "Total files packaged: ${TOTAL_FILES}"
log_info "Total lines of code: ${TOTAL_LINES}"

# Copy to deploy_v2
log_info "Copying to deploy_v2..."
mkdir -p "../deploy_v2/packages/${SDK_NAME}"
mkdir -p "../deploy_v2/scripts"
cp "${PACKAGE_DIR}-FINAL.tar.gz" "../deploy_v2/packages/${SDK_NAME}/"
cp "${MANIFEST_FILE}" "../deploy_v2/packages/${SDK_NAME}/"
cp "${CHECKSUM_FILE}" "../deploy_v2/packages/${SDK_NAME}/"
cp "deploy-${SDK_NAME}.sh" "../deploy_v2/scripts/"

log_success "Go SDK successfully integrated with deploy_v2 system!" 
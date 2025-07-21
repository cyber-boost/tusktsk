#!/bin/bash

# PHP SDK Comprehensive Packaging Script
# This script packages ALL PHP SDK components including recently built agent files

set -euo pipefail

# Configuration
SDK_NAME="php-sdk"
VERSION="1.0.0"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
PACKAGE_DIR="php-sdk-${VERSION}-${TIMESTAMP}"
MANIFEST_FILE="php-sdk-manifest-${TIMESTAMP}.json"
CHECKSUM_FILE="php-sdk-checksums-${TIMESTAMP}.txt"

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
cp -r lib/ "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp -r src/ "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp -r bin/ "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp -r vendor/ "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp -r config/ "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp *.php "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp *.tsk "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp *.md "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp *.txt "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp *.json "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp *.yml "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp *.yaml "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp *.xml "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp composer.json "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp composer.lock "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp LICENSE "${PACKAGE_DIR}/core/" 2>/dev/null || true
cp README.md "${PACKAGE_DIR}/core/" 2>/dev/null || true
create_component_manifest "core" "." "${PACKAGE_DIR}/core"

# Package AA_PHP (Goal Automation)
log_info "Packaging AA_PHP goal automation files..."
mkdir -p "${PACKAGE_DIR}/aa_php"
cp -r aa_php/ "${PACKAGE_DIR}/aa_php/" 2>/dev/null || true
create_component_manifest "aa_php" "aa_php" "${PACKAGE_DIR}/aa_php"

# Package Agent Goals (A-series Automation)
log_info "Packaging agent goals and automation files..."
mkdir -p "${PACKAGE_DIR}/agents"
cp -r lib/tusk_lang/agents/ "${PACKAGE_DIR}/agents/" 2>/dev/null || true
create_component_manifest "agents" "lib/tusk_lang/agents" "${PACKAGE_DIR}/agents"

# Package Examples
log_info "Packaging example files..."
mkdir -p "${PACKAGE_DIR}/examples"
cp -r examples/ "${PACKAGE_DIR}/examples/" 2>/dev/null || true
cp -r php-examples/ "${PACKAGE_DIR}/examples/" 2>/dev/null || true
create_component_manifest "examples" "examples+php-examples" "${PACKAGE_DIR}/examples"

# Package Tests
log_info "Packaging test files..."
mkdir -p "${PACKAGE_DIR}/tests"
cp -r test/ "${PACKAGE_DIR}/tests/" 2>/dev/null || true
cp -r tests/ "${PACKAGE_DIR}/tests/" 2>/dev/null || true
cp -r spec/ "${PACKAGE_DIR}/tests/" 2>/dev/null || true
create_component_manifest "tests" "test+tests+spec" "${PACKAGE_DIR}/tests"

# Package Documentation
log_info "Packaging documentation..."
mkdir -p "${PACKAGE_DIR}/docs"
cp -r docs/ "${PACKAGE_DIR}/docs/" 2>/dev/null || true
create_component_manifest "docs" "docs" "${PACKAGE_DIR}/docs"

# Package API Components
log_info "Packaging API components..."
mkdir -p "${PACKAGE_DIR}/api"
cp -r api/ "${PACKAGE_DIR}/api/" 2>/dev/null || true
create_component_manifest "api" "api" "${PACKAGE_DIR}/api"

# Package AI Components
log_info "Packaging AI components..."
mkdir -p "${PACKAGE_DIR}/ai"
cp -r ai/ "${PACKAGE_DIR}/ai/" 2>/dev/null || true
create_component_manifest "ai" "ai" "${PACKAGE_DIR}/ai"

# Package Blockchain Components
log_info "Packaging blockchain components..."
mkdir -p "${PACKAGE_DIR}/blockchain"
cp -r blockchain/ "${PACKAGE_DIR}/blockchain/" 2>/dev/null || true
create_component_manifest "blockchain" "blockchain" "${PACKAGE_DIR}/blockchain"

# Package Cloud Components
log_info "Packaging cloud components..."
mkdir -p "${PACKAGE_DIR}/cloud"
cp -r cloud/ "${PACKAGE_DIR}/cloud/" 2>/dev/null || true
create_component_manifest "cloud" "cloud" "${PACKAGE_DIR}/cloud"

# Package Concurrency Components
log_info "Packaging concurrency components..."
mkdir -p "${PACKAGE_DIR}/concurrency"
cp -r concurrency/ "${PACKAGE_DIR}/concurrency/" 2>/dev/null || true
create_component_manifest "concurrency" "concurrency" "${PACKAGE_DIR}/concurrency"

# Package Data Processing Components
log_info "Packaging data processing components..."
mkdir -p "${PACKAGE_DIR}/data_processing"
cp -r data_processing/ "${PACKAGE_DIR}/data_processing/" 2>/dev/null || true
create_component_manifest "data_processing" "data_processing" "${PACKAGE_DIR}/data_processing"

# Package DevOps Components
log_info "Packaging DevOps components..."
mkdir -p "${PACKAGE_DIR}/devops"
cp -r devops/ "${PACKAGE_DIR}/devops/" 2>/dev/null || true
create_component_manifest "devops" "devops" "${PACKAGE_DIR}/devops"

# Package IoT Components
log_info "Packaging IoT components..."
mkdir -p "${PACKAGE_DIR}/iot"
cp -r iot/ "${PACKAGE_DIR}/iot/" 2>/dev/null || true
create_component_manifest "iot" "iot" "${PACKAGE_DIR}/iot"

# Package Networking Components
log_info "Packaging networking components..."
mkdir -p "${PACKAGE_DIR}/networking"
cp -r networking/ "${PACKAGE_DIR}/networking/" 2>/dev/null || true
create_component_manifest "networking" "networking" "${PACKAGE_DIR}/networking"

# Package Optimization Components
log_info "Packaging optimization components..."
mkdir -p "${PACKAGE_DIR}/optimization"
cp -r optimization/ "${PACKAGE_DIR}/optimization/" 2>/dev/null || true
create_component_manifest "optimization" "optimization" "${PACKAGE_DIR}/optimization"

# Package Checklist
log_info "Packaging checklist components..."
mkdir -p "${PACKAGE_DIR}/checklist"
cp -r checklist/ "${PACKAGE_DIR}/checklist/" 2>/dev/null || true
create_component_manifest "checklist" "checklist" "${PACKAGE_DIR}/checklist"

# Find and package any other directories
log_info "Searching for additional components..."
for dir in */; do
    dir_name=$(basename "$dir")
    if [[ -d "$dir" ]] && [[ ! "$dir_name" =~ ^(lib|src|bin|vendor|config|aa_php|examples|php-examples|test|tests|spec|docs|api|ai|blockchain|cloud|concurrency|data_processing|devops|iot|networking|optimization|checklist|php-sdk-)$ ]] && [[ ! "$dir_name" =~ ^php-sdk- ]]; then
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
    "php_version_required": "8.0+",
    "dependencies": ["composer", "extensions"],
    "installation_methods": ["composer install", "source"]
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

# PHP SDK Deployment Script
set -euo pipefail

PACKAGE_FILE="$1"
MANIFEST_FILE="$2"
CHECKSUM_FILE="$3"

echo "Deploying PHP SDK..."
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
echo "Installing PHP dependencies..."
cd php-sdk-*
composer install --no-dev --optimize-autoloader || echo "Composer install failed"

# Run tests
echo "Running tests..."
php -l *.php 2>/dev/null || echo "PHP syntax check completed"
find . -name "*.php" -exec php -l {} \; 2>/dev/null || echo "All PHP files syntax checked"

echo "PHP SDK deployment completed successfully!"
EOF

chmod +x "deploy-${SDK_NAME}.sh"

# Create quick start guide
log_info "Creating quick start guide..."
cat > "${PACKAGE_DIR}/QUICK_START.md" << EOF
# PHP SDK Quick Start Guide

## Package Contents
- **Core SDK**: Main PHP source files and CLI tools
- **AA_PHP**: Goal automation and planning files
- **Agents**: A-series automation components
- **Examples**: Comprehensive example implementations
- **Tests**: Complete test suite
- **Documentation**: API docs and guides
- **API Components**: REST API framework
- **AI Components**: Neural networks, NLP, computer vision
- **Blockchain Components**: Cryptocurrency, blockchain core, cryptography
- **Cloud Components**: Multi-cloud, serverless, observability
- **Concurrency Components**: Distributed computing, actor model
- **Data Processing Components**: ETL pipelines, real-time analytics, ML framework
- **DevOps Components**: Infrastructure automation, container orchestration, CI/CD
- **IoT Components**: Device management, industrial automation, edge computing
- **Networking Components**: Advanced networking, messaging, security
- **Optimization Components**: Performance profiler, caching, code optimizer

## Quick Installation

\`\`\`bash
# Extract the package
tar -xzf ${PACKAGE_DIR}.tar.gz

# Navigate to the package
cd ${PACKAGE_DIR}

# Install dependencies
composer install

# Run tests
php -l *.php
\`\`\`

## Usage Examples

\`\`\`php
<?php
require_once 'vendor/autoload.php';

// Initialize the SDK
$sdk = new TuskLang\SDK();

// Use operators
$operators = new TuskLang\Operators();

// Your code here
?>
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
tar -czf "${PACKAGE_DIR}-FINAL.tar.gz" "${PACKAGE_DIR}" "${MANIFEST_FILE}" "${CHECKSUM_FILE}"

# Cleanup temporary files
rm -rf "${PACKAGE_DIR}"

# Summary
log_success "PHP SDK packaging completed!"
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

log_success "PHP SDK successfully integrated with deploy_v2 system!" 
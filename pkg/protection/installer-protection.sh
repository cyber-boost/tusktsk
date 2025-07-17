#!/bin/bash

# TuskLang Protected Installer
# This script demonstrates protection techniques for the installation process

set -e

# Configuration
VERSION="2.0.0"
LICENSE_SERVER="https://license.tuskt.sk/api/v1"
DOWNLOAD_BASE="https://downloads.tuskt.sk"
PUBLIC_KEY_URL="https://tuskt.sk/keys/tusklang-public.asc"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# License validation
validate_license() {
    local license_key="$1"
    
    if [[ -z "$license_key" ]]; then
        echo -e "${RED}ERROR: License key is required${NC}"
        echo "Please provide your TuskLang license key:"
        echo "export TUSKLANG_LICENSE=your-license-key"
        exit 1
    fi
    
    echo -e "${BLUE}Validating license key...${NC}"
    
    # Local validation (basic format check)
    if [[ ! "$license_key" =~ ^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$ ]]; then
        echo -e "${RED}ERROR: Invalid license key format${NC}"
        exit 1
    fi
    
    # Server validation
    local response=$(curl -s -w "%{http_code}" -X POST "$LICENSE_SERVER/validate" \
        -H "Content-Type: application/json" \
        -d "{\"key\":\"$license_key\"}")
    
    local http_code="${response: -3}"
    local body="${response%???}"
    
    if [[ "$http_code" != "200" ]]; then
        echo -e "${RED}ERROR: License validation failed (HTTP $http_code)${NC}"
        exit 1
    fi
    
    # Parse response
    local valid=$(echo "$body" | grep -o '"valid":[^,]*' | cut -d':' -f2)
    local message=$(echo "$body" | grep -o '"message":"[^"]*"' | cut -d'"' -f4)
    
    if [[ "$valid" != "true" ]]; then
        echo -e "${RED}ERROR: $message${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}License validated successfully${NC}"
}

# Download and verify package
download_package() {
    local platform="$1"
    local arch="$2"
    
    echo -e "${BLUE}Downloading TuskLang for $platform-$arch...${NC}"
    
    # Download package
    local package_url="$DOWNLOAD_BASE/v$VERSION/tusklang-$platform-$arch.tar.gz"
    local package_file="tusklang-$platform-$arch.tar.gz"
    
    if ! curl -L -o "$package_file" "$package_url"; then
        echo -e "${RED}ERROR: Failed to download package${NC}"
        exit 1
    fi
    
    # Download checksum
    local checksum_url="$DOWNLOAD_BASE/v$VERSION/tusklang-$platform-$arch.sha256"
    local expected_checksum=$(curl -s "$checksum_url")
    
    if [[ -z "$expected_checksum" ]]; then
        echo -e "${RED}ERROR: Failed to download checksum${NC}"
        exit 1
    fi
    
    # Verify checksum
    local actual_checksum=$(sha256sum "$package_file" | cut -d' ' -f1)
    
    if [[ "$actual_checksum" != "$expected_checksum" ]]; then
        echo -e "${RED}ERROR: Package integrity check failed${NC}"
        echo "Expected: $expected_checksum"
        echo "Actual:   $actual_checksum"
        exit 1
    fi
    
    echo -e "${GREEN}Package integrity verified${NC}"
}

# Verify GPG signature
verify_signature() {
    local package_file="$1"
    
    echo -e "${BLUE}Verifying package signature...${NC}"
    
    # Download public key
    if ! curl -O "$PUBLIC_KEY_URL"; then
        echo -e "${RED}ERROR: Failed to download public key${NC}"
        exit 1
    fi
    
    # Import public key
    if ! gpg --import tusklang-public.asc 2>/dev/null; then
        echo -e "${YELLOW}Warning: Failed to import public key, skipping signature verification${NC}"
        return 0
    fi
    
    # Download signature
    local sig_url="$DOWNLOAD_BASE/v$VERSION/$package_file.sig"
    if ! curl -O "$sig_url"; then
        echo -e "${YELLOW}Warning: Failed to download signature, skipping verification${NC}"
        return 0
    fi
    
    # Verify signature
    if ! gpg --verify "$package_file.sig" "$package_file" 2>/dev/null; then
        echo -e "${RED}ERROR: Package signature verification failed${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}Package signature verified${NC}"
}

# Install package
install_package() {
    local package_file="$1"
    local install_dir="$2"
    
    echo -e "${BLUE}Installing TuskLang...${NC}"
    
    # Create installation directory
    mkdir -p "$install_dir"
    
    # Extract package
    if ! tar -xzf "$package_file" -C "$install_dir"; then
        echo -e "${RED}ERROR: Failed to extract package${NC}"
        exit 1
    fi
    
    # Set permissions
    chmod +x "$install_dir/bin/tusk"
    
    # Create symlinks
    if [[ -w /usr/local/bin ]]; then
        ln -sf "$install_dir/bin/tusk" /usr/local/bin/tusk
        echo -e "${GREEN}Created symlink: /usr/local/bin/tusk${NC}"
    else
        echo -e "${YELLOW}Please add to PATH: $install_dir/bin${NC}"
    fi
    
    # Verify installation
    if "$install_dir/bin/tusk" --version; then
        echo -e "${GREEN}TuskLang installed successfully!${NC}"
    else
        echo -e "${RED}ERROR: Installation verification failed${NC}"
        exit 1
    fi
}

# Usage tracking
track_installation() {
    local license_key="$1"
    local platform="$2"
    local arch="$3"
    
    echo -e "${BLUE}Recording installation...${NC}"
    
    # Send installation event
    curl -s -X POST "$LICENSE_SERVER/install" \
        -H "Content-Type: application/json" \
        -d "{
            \"license_key\": \"$license_key\",
            \"version\": \"$VERSION\",
            \"platform\": \"$platform\",
            \"arch\": \"$arch\",
            \"timestamp\": $(date +%s)
        }" > /dev/null || true
    
    echo -e "${GREEN}Installation recorded${NC}"
}

# Main installation flow
main() {
    echo -e "${BLUE}🚀 TuskLang Protected Installer v$VERSION${NC}"
    echo "====================================="
    
    # Check for license key
    local license_key="$TUSKLANG_LICENSE"
    if [[ -z "$license_key" ]]; then
        echo -e "${YELLOW}License key not found in environment${NC}"
        read -p "Enter your TuskLang license key: " license_key
    fi
    
    # Validate license
    validate_license "$license_key"
    
    # Detect platform
    local platform=$(uname -s | tr '[:upper:]' '[:lower:]')
    local arch=$(uname -m)
    
    # Normalize architecture
    case "$arch" in
        x86_64) arch="amd64" ;;
        aarch64) arch="arm64" ;;
        armv7l) arch="arm" ;;
    esac
    
    echo -e "${BLUE}Detected platform: $platform-$arch${NC}"
    
    # Download and verify package
    local package_file="tusklang-$platform-$arch.tar.gz"
    download_package "$platform" "$arch"
    verify_signature "$package_file"
    
    # Install package
    local install_dir="/opt/tusklang"
    install_package "$package_file" "$install_dir"
    
    # Track installation
    track_installation "$license_key" "$platform" "$arch"
    
    # Cleanup
    rm -f "$package_file" "$package_file.sig" tusklang-public.asc
    
    echo -e "${GREEN}✅ Installation completed successfully!${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Run 'tusk --help' to see available commands"
    echo "2. Visit https://docs.tuskt.sk for documentation"
    echo "3. Join our community at https://github.com/cyber-boost/tusktsk"
}

# Run main function
main "$@" 
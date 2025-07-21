#!/bin/bash
# TuskLang Bash SDK Installer
# ===========================
# Version: 1.0.0

set -e

TUSKLANG_VERSION=$(cat VERSION 2>/dev/null || echo "1.0.0")
INSTALL_DIR="/opt/tusklang"
BIN_DIR="/usr/local/bin"

echo "Installing TuskLang Bash SDK v$TUSKLANG_VERSION..."

# Check if running as root or with sudo
if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root or with sudo"
   exit 1
fi

# Create installation directory
mkdir -p $INSTALL_DIR

# Copy all files to installation directory
cp -r * $INSTALL_DIR/ 2>/dev/null || true

# Make scripts executable
chmod +x $INSTALL_DIR/*.sh
chmod +x $INSTALL_DIR/sdk/**/*.sh

# Create symlinks
ln -sf $INSTALL_DIR/tsk.sh $BIN_DIR/tsk
ln -sf $INSTALL_DIR/tsk-enhanced.sh $BIN_DIR/tsk-enhanced
ln -sf $INSTALL_DIR/license.sh $BIN_DIR/license
ln -sf $INSTALL_DIR/peanut_config.sh $BIN_DIR/peanut-config
ln -sf $INSTALL_DIR/protection.sh $BIN_DIR/protection

# Create necessary directories
mkdir -p /tmp/tusklang/{packages,builds,credentials,platforms}

echo "TuskLang Bash SDK v$TUSKLANG_VERSION installed successfully!"
echo "Run 'tsk --help' to get started." 
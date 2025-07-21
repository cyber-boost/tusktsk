#!/bin/bash
# TuskLang Bash SDK Uninstaller
# =============================
# Version: 1.0.0

set -e

echo "Uninstalling TuskLang Bash SDK..."

# Check if running as root or with sudo
if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root or with sudo"
   exit 1
fi

# Remove symlinks
rm -f /usr/local/bin/tsk
rm -f /usr/local/bin/tsk-enhanced
rm -f /usr/local/bin/license
rm -f /usr/local/bin/peanut-config
rm -f /usr/local/bin/protection

# Remove installation directory
rm -rf /opt/tusklang

# Ask about temporary files
read -p "Remove temporary files? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    rm -rf /tmp/tusklang
    echo "Temporary files removed."
fi

echo "TuskLang Bash SDK uninstalled successfully!" 
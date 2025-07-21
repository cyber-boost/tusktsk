#!/bin/bash
# TuskLang PHP SDK Protection Encoder
# Encodes PHP files with IonCube/Zend Guard for protection

set -e

SDK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROTECTED_DIR="$SDK_DIR/protected"
ENCODER_DIR="$SDK_DIR/encoder"

echo "🐘 TuskLang PHP SDK Protection Encoder"
echo "======================================"

# Create protected directory
mkdir -p "$PROTECTED_DIR"
mkdir -p "$ENCODER_DIR"

# Check for IonCube Loader
if ! php -m | grep -q ionCube; then
    echo "⚠️  IonCube Loader not found, installing..."
    # Installation commands would go here
fi

# Check for Zend Guard
if ! php -m | grep -q "Zend Guard"; then
    echo "⚠️  Zend Guard not found, installing..."
    # Installation commands would go here
fi

# Encode main SDK files
echo "🔒 Encoding PHP SDK files..."

# Encode TuskLangProtected.php
if command -v ioncube_encoder &> /dev/null; then
    ioncube_encoder \
        --encode "$SDK_DIR/src/TuskLangProtected.php" \
        --output-dir "$PROTECTED_DIR" \
        --optimize max \
        --ignore-strict-warnings \
        --ignore-deprecated-warnings
elif command -v zendenc &> /dev/null; then
    zendenc \
        --encode "$SDK_DIR/src/TuskLangProtected.php" \
        --output-dir "$PROTECTED_DIR" \
        --optimize max
else
    echo "❌ No encoder found (ioncube_encoder or zendenc)"
    exit 1
fi

# Create loader script
cat > "$PROTECTED_DIR/loader.php" << 'EOF'
<?php
/**
 * TuskLang Protected SDK Loader
 * Loads encoded SDK files with license validation
 */

// Check for IonCube/Zend Guard
if (!extension_loaded('ionCube Loader') && !extension_loaded('Zend Guard Loader')) {
    die('TuskLang SDK requires IonCube Loader or Zend Guard Loader');
}

// Load protected SDK
require_once __DIR__ . '/TuskLangProtected.php';

// Initialize with license
$licenseKey = getenv('TUSKLANG_LICENSE') ?: null;
if (!TuskLang\SDK\TuskLangProtected::init($licenseKey)) {
    die('TuskLang SDK license validation failed');
}

echo "✅ TuskLang Protected SDK loaded successfully\n";
EOF

# Create installation script
cat > "$PROTECTED_DIR/install.sh" << 'EOF'
#!/bin/bash
# Install protected TuskLang PHP SDK

set -e

echo "🐘 Installing TuskLang Protected PHP SDK..."

# Copy to system location
sudo cp -r . /usr/local/lib/tusklang-php-protected/

# Set permissions
sudo chmod 755 /usr/local/lib/tusklang-php-protected/
sudo chmod 644 /usr/local/lib/tusklang-php-protected/*.php

# Create symlink
sudo ln -sf /usr/local/lib/tusklang-php-protected/loader.php /usr/local/bin/tusklang-php

echo "✅ TuskLang Protected PHP SDK installed"
echo "Usage: tusklang-php"
EOF

chmod +x "$PROTECTED_DIR/install.sh"

echo "✅ PHP SDK protection encoding complete"
echo "Protected files: $PROTECTED_DIR"
echo "Install with: cd $PROTECTED_DIR && ./install.sh" 
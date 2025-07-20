#!/bin/bash
# TuskLang Binary Obfuscation Script
# Applies advanced binary protection to all TuskLang executables

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROTECTED_DIR="$PROJECT_ROOT/protected-binaries"

echo "🔒 TuskLang Binary Obfuscation"
echo "==============================="

# Create protected directory
mkdir -p "$PROTECTED_DIR"

# Install required tools
echo "📦 Installing obfuscation tools..."

# Install UPX
if ! command -v upx &> /dev/null; then
    echo "Installing UPX..."
    if command -v apt-get &> /dev/null; then
        sudo apt-get update && sudo apt-get install -y upx
    elif command -v yum &> /dev/null; then
        sudo yum install -y upx
    elif command -v brew &> /dev/null; then
        brew install upx
    fi
fi

# Install GNU tools
if ! command -v strip &> /dev/null; then
    echo "Installing GNU Binutils..."
    if command -v apt-get &> /dev/null; then
        sudo apt-get install -y binutils
    elif command -v yum &> /dev/null; then
        sudo yum install -y binutils
    fi
fi

# Install patchelf
if ! command -v patchelf &> /dev/null; then
    echo "Installing PatchELF..."
    if command -v apt-get &> /dev/null; then
        sudo apt-get install -y patchelf
    elif command -v yum &> /dev/null; then
        sudo yum install -y patchelf
    fi
fi

# Obfuscate PHP binaries
echo "🔒 Obfuscating PHP binaries..."

if [ -f "$PROJECT_ROOT/bin/tsk" ]; then
    echo "Protecting: bin/tsk"
    cp "$PROJECT_ROOT/bin/tsk" "$PROTECTED_DIR/tsk-protected"
    
    # Apply PHP-specific protection
    php -r "
    require_once '$PROJECT_ROOT/lib/BinaryObfuscator.php';
    use TuskPHP\License\BinaryObfuscator;
    BinaryObfuscator::init('maximum');
    BinaryObfuscator::obfuscateBinary('$PROTECTED_DIR/tsk-protected');
    "
fi

if [ -f "$PROJECT_ROOT/bin/tusk-license" ]; then
    echo "Protecting: bin/tusk-license"
    cp "$PROJECT_ROOT/bin/tusk-license" "$PROTECTED_DIR/tusk-license-protected"
    
    php -r "
    require_once '$PROJECT_ROOT/lib/BinaryObfuscator.php';
    use TuskPHP\License\BinaryObfuscator;
    BinaryObfuscator::init('maximum');
    BinaryObfuscator::obfuscateBinary('$PROTECTED_DIR/tusk-license-protected');
    "
fi

# Obfuscate Rust binaries
echo "🔒 Obfuscating Rust binaries..."

if [ -f "$PROJECT_ROOT/sdk/rust/target/release/tusklang" ]; then
    echo "Protecting: sdk/rust/target/release/tusklang"
    cp "$PROJECT_ROOT/sdk/rust/target/release/tusklang" "$PROTECTED_DIR/tusklang-rust-protected"
    
    # Apply Rust-specific protection
    strip --strip-all "$PROTECTED_DIR/tusklang-rust-protected"
    upx --best --ultra-brute "$PROTECTED_DIR/tusklang-rust-protected"
fi

# Obfuscate Python binaries
echo "🔒 Obfuscating Python binaries..."

find "$PROJECT_ROOT/sdk/python" -name "*.pyc" -o -name "*.pyo" | while read -r pycfile; do
    echo "Protecting: $pycfile"
    cp "$pycfile" "$PROTECTED_DIR/$(basename "$pycfile").protected"
    
    # Apply Python-specific protection
    python3 -c "
import py_compile
import os
import sys
sys.path.append('$PROJECT_ROOT/lib')
from BinaryObfuscator import BinaryObfuscator
BinaryObfuscator.init('maximum')
BinaryObfuscator.obfuscateBinary('$PROTECTED_DIR/$(basename "$pycfile").protected')
"
done

# Obfuscate JavaScript bundles
echo "🔒 Obfuscating JavaScript bundles..."

find "$PROJECT_ROOT/sdk/js" -name "*.js" -size +100k | while read -r jsfile; do
    echo "Protecting: $jsfile"
    cp "$jsfile" "$PROTECTED_DIR/$(basename "$jsfile").protected"
    
    # Apply JavaScript-specific protection
    node -e "
const fs = require('fs');
const path = require('path');
const obfuscator = require('javascript-obfuscator');

const code = fs.readFileSync('$jsfile', 'utf8');
const obfuscatedCode = obfuscator.obfuscate(code, {
    compact: true,
    controlFlowFlattening: true,
    controlFlowFlatteningThreshold: 0.75,
    deadCodeInjection: true,
    deadCodeInjectionThreshold: 0.4,
    debugProtection: true,
    debugProtectionInterval: true,
    disableConsoleOutput: true,
    identifierNamesGenerator: 'hexadecimal',
    log: false,
    numbersToExpressions: true,
    renameGlobals: false,
    selfDefending: true,
    simplify: true,
    splitStrings: true,
    splitStringsChunkLength: 10,
    stringArray: true,
    stringArrayEncoding: ['base64'],
    stringArrayThreshold: 0.75,
    transformObjectKeys: true,
    unicodeEscapeSequence: false
}).getObfuscatedCode();

fs.writeFileSync('$PROTECTED_DIR/$(basename "$jsfile").protected', obfuscatedCode);
"
done

# Create installation script
cat > "$PROTECTED_DIR/install-protected.sh" << 'EOF'
#!/bin/bash
# Install protected TuskLang binaries

set -e

echo "🐘 Installing TuskLang Protected Binaries..."

# Copy to system location
sudo cp tsk-protected /usr/local/bin/tsk-protected
sudo cp tusk-license-protected /usr/local/bin/tusk-license-protected

# Set permissions
sudo chmod 755 /usr/local/bin/tsk-protected
sudo chmod 755 /usr/local/bin/tusk-license-protected

# Create symlinks
sudo ln -sf /usr/local/bin/tsk-protected /usr/local/bin/tsk
sudo ln -sf /usr/local/bin/tusk-license-protected /usr/local/bin/tusk-license

echo "✅ TuskLang Protected Binaries installed"
echo "Usage: tsk [command]"
echo "Usage: tusk-license [command]"
EOF

chmod +x "$PROTECTED_DIR/install-protected.sh"

echo "✅ Binary obfuscation complete"
echo "Protected binaries: $PROTECTED_DIR"
echo "Install with: cd $PROTECTED_DIR && ./install-protected.sh" 
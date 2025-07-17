#!/bin/bash
# TuskLang Rust SDK Protection Builder
# Builds protected SDK with binary obfuscation

set -e

SDK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROTECTED_DIR="$SDK_DIR/protected"
TARGET_DIR="$SDK_DIR/target"

echo "🐘 TuskLang Rust SDK Protection Builder"
echo "======================================="

# Create protected directory
mkdir -p "$PROTECTED_DIR"

# Build release version
echo "🔨 Building Rust SDK..."
cargo build --release

# Copy binary to protected directory
cp "$TARGET_DIR/release/tusklang" "$PROTECTED_DIR/tusklang-protected"

# Apply binary obfuscation
echo "🔒 Applying binary obfuscation..."

# Strip debug symbols
strip "$PROTECTED_DIR/tusklang-protected"

# Apply UPX compression (if available)
if command -v upx &> /dev/null; then
    echo "📦 Compressing binary with UPX..."
    upx --best "$PROTECTED_DIR/tusklang-protected"
fi

# Create protection wrapper
cat > "$PROTECTED_DIR/run-protected.sh" << 'EOF'
#!/bin/bash
# TuskLang Protected Rust SDK Runner

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BINARY="$SCRIPT_DIR/tusklang-protected"

# Check if binary exists
if [ ! -f "$BINARY" ]; then
    echo "❌ Protected binary not found"
    exit 1
fi

# Check if binary is executable
if [ ! -x "$BINARY" ]; then
    echo "❌ Binary is not executable"
    exit 1
fi

# Set license environment variable if provided
if [ -n "$TUSKLANG_LICENSE" ]; then
    export TUSKLANG_LICENSE="$TUSKLANG_LICENSE"
fi

# Run protected binary
echo "🔒 Running TuskLang Protected Rust SDK..."
exec "$BINARY" "$@"
EOF

chmod +x "$PROTECTED_DIR/run-protected.sh"

# Create installation script
cat > "$PROTECTED_DIR/install.sh" << 'EOF'
#!/bin/bash
# Install protected TuskLang Rust SDK

set -e

echo "🐘 Installing TuskLang Protected Rust SDK..."

# Copy to system location
sudo cp tusklang-protected /usr/local/bin/tusklang-protected
sudo cp run-protected.sh /usr/local/bin/tusklang-protected-run

# Set permissions
sudo chmod 755 /usr/local/bin/tusklang-protected
sudo chmod 755 /usr/local/bin/tusklang-protected-run

# Create symlink
sudo ln -sf /usr/local/bin/tusklang-protected-run /usr/local/bin/tusklang

echo "✅ TuskLang Protected Rust SDK installed"
echo "Usage: tusklang [command]"
echo "Or: tusklang-protected-run [command]"
EOF

chmod +x "$PROTECTED_DIR/install.sh"

echo "✅ Rust SDK protection build complete"
echo "Protected binary: $PROTECTED_DIR/tusklang-protected"
echo "Install with: cd $PROTECTED_DIR && ./install.sh" 
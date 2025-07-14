#!/usr/bin/env bash

# Test script for TuskLang CLI
# =============================

set -euo pipefail

# Get the directory of this script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Test the CLI
echo "🧪 Testing TuskLang CLI..."

# Test version
echo "Testing version command..."
"$SCRIPT_DIR/main.sh" --version

echo

# Test help
echo "Testing help command..."
"$SCRIPT_DIR/main.sh" --help

echo

# Test config commands
echo "Testing config commands..."
"$SCRIPT_DIR/main.sh" config stats

echo

# Test utility commands
echo "Testing utility commands..."
"$SCRIPT_DIR/main.sh" validate "$SCRIPT_DIR/../test-simple.tsk"

echo

echo "✅ CLI tests completed successfully!" 
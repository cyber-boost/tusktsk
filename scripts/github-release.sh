#!/bin/bash
# GitHub Release Script for TuskTsk Python SDK

set -e

VERSION="2.1.2"
REPO="cyber-boost/tusktsk"
RELEASE_PACKAGE="/opt/tsk_git/distributions/github/tusktsk-2.1.2-release.tar.gz"

echo "🚀 Creating GitHub release for TuskTsk v2.1.2..."

# Check if GitHub CLI is installed
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI (gh) is not installed"
    echo "Please install it from: https://cli.github.com/"
    exit 1
fi

# Check if user is authenticated
if ! gh auth status &> /dev/null; then
    echo "❌ Not authenticated with GitHub"
    echo "Please run: gh auth login"
    exit 1
fi

# Create release
echo "📦 Creating release..."
gh release create "v2.1.2" \
    --repo "" \
    --title "TuskTsk Python SDK v2.1.2" \
    --notes-file <(cat << 'RELEASE_NOTES'
# TuskTsk Python SDK v2.1.2

## 🎉 New Release

**Version**: 2.1.2  
**Release Date**: 2025-07-21 03:45:12 UTC

## 📦 Package Contents

- **Source Distribution**: tusktsk-2.1.2.tar.gz
- **Wheel Distribution**: tusktsk-2.1.2-py3-none-any.whl

## 🚀 Installation

### From PyPI
```bash
pip install tusktsk==2.1.2
```

### From Source
```bash
pip install tusktsk-2.1.2.tar.gz
```

### From Wheel
```bash
pip install tusktsk-2.1.2-py3-none-any.whl
```

## 📋 Features

- Advanced Python operators and integrations
- CLI tools with comprehensive commands
- Database adapters for multiple systems
- Enterprise-grade security features
- AI and machine learning engines

## 🔧 Dependencies

- Python >= 3.8
- msgpack >= 1.0.0
- watchdog >= 2.0.0

## 📚 Documentation

Visit https://tuskt.sk for full documentation.

## 🔗 Links

- [PyPI Package](https://pypi.org/project/tusktsk/)
- [Documentation](https://tuskt.sk)
- [GitHub Repository](https://github.com/cyber-boost/tusktsk)
RELEASE_NOTES
) \
    ""

echo "✅ GitHub release created successfully!"
echo "🔗 Release URL: https://github.com//releases/tag/v2.1.2"

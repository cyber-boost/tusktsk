# TuskLang Installation System Review

## Overview

This document reviews the TuskLang installation infrastructure found in the `/universe` and `/admin` directories, and provides recommendations for ensuring compatibility and consistency across all language SDKs.

## Current Installation Structure

### 1. Universe Directory (`/opt/tsk_git/universe/`)

The universe directory contains the dynamic custom install script builder for init.tusklang.org:

#### Install Scripts Per Language
Each language has 6 install script variants:
- `{language}-curl-install.sh` - Basic curl installation
- `{language}-wget-install.sh` - Basic wget installation
- `{language}-curl-install-adv.sh` - Advanced curl installation
- `{language}-wget-install-adv.sh` - Advanced wget installation
- `{language}-curl-install.sh.obf` - Obfuscated curl version
- `{language}-wget-install.sh.obf` - Obfuscated wget version

Languages covered:
- PHP
- JavaScript
- Python
- Go
- Rust
- C# (csharp)
- Ruby
- Java
- Bash

#### Common Features in Install Scripts
- Color-coded output with logging functions
- Human-friendly yes/no prompts (accepts typos!)
- ASCII art banner display
- Auto-detection of installation method (curl vs wget)
- Platform detection and compatibility checks

### 2. Admin Directory (`/opt/tsk_git/sdk-pnt-test/admin/`)

Contains build artifacts for different TuskLang versions:
- `tusklang-1.0.3/`
- `tusklang-1.0.4/`
- `tusklang-1.0.5/`

Each version contains:
- `/usr/local/lib/tusklang/` - Core library files
- `/install/` - Installation directory (currently empty)
- Database energy components
- FUJSEN service components

## Compatibility Requirements

### 1. Installation Script Standards

All installation scripts should:
- ✅ Support both curl and wget methods
- ✅ Auto-detect operating system and architecture
- ✅ Install language-specific dependencies
- ✅ Set up PATH and environment variables
- ✅ Create necessary directories
- ✅ Download and extract SDK files
- ✅ Install the `tsk` CLI command
- ✅ Verify installation success

### 2. Binary Distribution Format

Each language SDK should provide:
```
tusklang-{language}-{version}.tar.gz
├── bin/
│   └── tsk              # CLI executable
├── lib/
│   └── {language}/      # Language-specific libraries
├── docs/
│   ├── README.md
│   ├── CLI_USAGE.md
│   └── PNT_USAGE.md
├── examples/
│   ├── basic.tsk
│   └── peanu.peanuts
├── LICENSE
└── install.sh           # Post-extraction setup
```

### 3. Installation Paths

Standard installation locations:
- **System-wide**: `/usr/local/lib/tusklang/{language}/`
- **User-local**: `~/.tusklang/{language}/`
- **CLI Binary**: `/usr/local/bin/tsk` (symlink to language-specific)

### 4. Version Management

- Each SDK must support multiple versions installed simultaneously
- Version switching via `tsk use {version}`
- Default version stored in `~/.tusklang/default-version`

## Recommendations for SDK Compatibility

### 1. Create Build Scripts

For each language SDK, create a `build-dist.sh` script:

```bash
#!/bin/bash
# build-dist.sh for {language} SDK

VERSION=$(cat VERSION)
DIST_NAME="tusklang-{language}-${VERSION}"

# Create distribution directory
mkdir -p dist/${DIST_NAME}/{bin,lib,docs,examples}

# Copy files
cp -r src/* dist/${DIST_NAME}/lib/
cp bin/tsk dist/${DIST_NAME}/bin/
cp -r docs/* dist/${DIST_NAME}/docs/
cp -r examples/* dist/${DIST_NAME}/examples/
cp LICENSE README.md dist/${DIST_NAME}/

# Create tar.gz
cd dist
tar -czf ${DIST_NAME}.tar.gz ${DIST_NAME}
cd ..

echo "Created dist/${DIST_NAME}.tar.gz"
```

### 2. Standardize Install Scripts

Update all install scripts to:
1. Download the appropriate SDK tar.gz
2. Extract to temporary directory
3. Run language-specific setup
4. Create symlinks for CLI
5. Update shell configuration files
6. Verify installation

### 3. Create Manifest Files

Each SDK should include a `manifest.tsk` file:

```tsk
[sdk]
name: "tusklang-python"
version: "2.0.0"
language: "python"
min_language_version: "3.8"
cli_binary: "bin/tsk"

[dependencies]
msgpack: ">=1.0.0"
watchdog: ">=2.0.0"

[features]
peanut_binary: true
cli_commands: "all"
database_support: ["sqlite", "postgresql", "mysql"]
ai_integration: true

[installation]
paths: {
    system: "/usr/local/lib/tusklang/python"
    user: "~/.tusklang/python"
    bin: "/usr/local/bin/tsk"
}
```

### 4. Testing Installation Scripts

Create a test suite that verifies:
- Installation works on clean systems
- Upgrades preserve user data
- Uninstallation is clean
- Multiple SDK versions can coexist
- CLI commands work after installation

### 5. Documentation Updates

Each SDK needs:
- `docs/INSTALLATION.md` - Platform-specific install instructions
- `docs/CLI_USAGE.md` - Complete CLI command reference
- `docs/PNT_USAGE.md` - Peanut binary config usage guide
- `docs/TROUBLESHOOTING.md` - Common issues and solutions

## Next Steps

1. **Create tar.gz packages** for each SDK with standardized structure
2. **Update install scripts** to download and install these packages
3. **Test installation** on various platforms (Linux, macOS, Windows WSL)
4. **Update init.tusklang.org** to serve the new packages
5. **Create version management** system for SDK updates

## Security Considerations

- All packages should be signed with GPG keys
- Checksums should be provided for verification
- HTTPS should be enforced for all downloads
- Install scripts should validate downloads before execution

---

This review ensures that all TuskLang SDKs will have consistent, reliable installation experiences across all platforms and languages.
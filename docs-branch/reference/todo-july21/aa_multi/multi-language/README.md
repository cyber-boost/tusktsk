# TuskLang Multi-Language SDK

A unified coordination layer for managing and executing TuskLang operations across all supported programming languages.

## Overview

The TuskLang Multi-Language SDK provides a single interface to coordinate operations across Python, Rust, JavaScript, Ruby, C#, Go, PHP, Java, and Bash SDKs. It enables unified configuration management, package management, and command execution across all language implementations.

## Features

### 🚀 Unified CLI Coordinator
- Execute TuskLang commands across any supported language
- Run commands in all languages simultaneously
- Consistent interface regardless of underlying language

### ⚙️ Cross-Language Configuration Management
- Hierarchical configuration using `peanu.tsk` files
- Support for multiple config formats (TSK, PNT, JSON, YAML)
- Child directory configs override parent configs
- Language-specific configuration sections

### 📦 Multi-Language Package Management
- Install, update, and manage dependencies across all languages
- Dependency conflict detection and resolution
- Unified package file generation
- Language-specific package file support

## Quick Start

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd sdk/multi-language

# Make the CLI executable
chmod +x tsk

# Test the installation
./tsk status
```

### Basic Usage

```bash
# Show available languages
./tsk list

# Run a command in Python
./tsk run python version

# Run a command in all languages
./tsk run-all version

# Show SDK status
./tsk status
```

## Configuration

### Creating Configuration Files

The SDK supports multiple configuration file formats:

#### TSK Format (Recommended)
```ini
[global]
version = 1.0.0
environment = development
debug = false

[python]
executable = python3
version = 1.0.0

[rust]
executable = cargo
version = 1.0.0

[javascript]
executable = node
version = 1.0.0
```

#### JSON Format
```json
{
  "global": {
    "version": "1.0.0",
    "environment": "development"
  },
  "python": {
    "executable": "python3",
    "version": "1.0.0"
  }
}
```

### Configuration Hierarchy

The SDK searches for configuration files in the following order (child configs override parent):

1. Current directory: `peanu.tsk`, `peanu.pnt`, `.peanuts`
2. Parent directory: `peanu.tsk`, `peanu.pnt`, `.peanuts`
3. Continue up to root directory

### Configuration Management Commands

```bash
# Load and display current configuration
./tsk config load

# Validate configuration
./tsk config validate

# Generate configuration template
./tsk config template python

# Save configuration to file
./tsk config save my-config.tsk
```

## Package Management

### Installing Dependencies

```bash
# Install dependencies for Python
./tsk packages install python

# Install dependencies for all languages
./tsk packages install-all

# Install specific packages
./tsk packages install python requests flask
```

### Managing Packages

```bash
# List installed packages
./tsk packages list python

# Update packages
./tsk packages update python

# Get dependency graph
./tsk packages graph python

# Check for dependency conflicts
./tsk packages conflicts
```

## Supported Languages

| Language | Executable | Package Files | Status |
|----------|------------|---------------|---------|
| Python | python3 | requirements.txt, pyproject.toml, setup.py | ✅ |
| Rust | cargo | Cargo.toml | ✅ |
| JavaScript | node | package.json | ✅ |
| Ruby | ruby | Gemfile, gemspec | ✅ |
| C# | dotnet | *.csproj, packages.config | ✅ |
| Go | go | go.mod | ✅ |
| PHP | php | composer.json | ✅ |
| Java | java | pom.xml, build.gradle | ✅ |
| Bash | bash | requirements.sh | ✅ |

## Architecture

### Core Components

1. **MultiLanguageCoordinator** (`tsk_coordinator.py`)
   - Executes commands across language SDKs
   - Manages language-specific command building
   - Provides unified result format

2. **ConfigManager** (`config_manager.py`)
   - Handles hierarchical configuration loading
   - Supports multiple config file formats
   - Validates configuration integrity

3. **MultiLanguagePackageManager** (`package_manager.py`)
   - Manages dependencies across all languages
   - Detects and resolves conflicts
   - Provides unified package operations

### File Structure

```
sdk/multi-language/
├── tsk                     # Main CLI entry point
├── tsk_coordinator.py      # Command coordination
├── config_manager.py       # Configuration management
├── package_manager.py      # Package management
├── README.md              # This file
└── peanu.tsk              # Default configuration
```

## Advanced Usage

### Custom Language Support

To add support for a new language:

1. Update the `languages` dictionary in `MultiLanguageCoordinator`
2. Add language-specific command building logic
3. Update package management configuration
4. Add language to configuration templates

### Integration with Existing SDKs

The multi-language SDK is designed to work alongside existing language-specific SDKs:

```bash
# Use multi-language coordinator
./tsk run python version

# Or use language-specific SDK directly
cd ../python
python tsk.py version
```

### Error Handling

The SDK provides comprehensive error handling:

- Language SDK not found
- Command execution failures
- Configuration parsing errors
- Package installation issues
- Dependency conflicts

All errors are returned in a consistent JSON format with detailed error messages.

## Development

### Testing

```bash
# Test configuration management
./tsk config validate

# Test package management
./tsk packages conflicts

# Test command execution
./tsk run python --help
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Implement your changes
4. Add tests
5. Submit a pull request

## Troubleshooting

### Common Issues

1. **Language SDK not found**
   - Ensure the language SDK exists in the `../<language>` directory
   - Check that the executable is available in PATH

2. **Configuration not loading**
   - Verify configuration file syntax
   - Check file permissions
   - Use `./tsk config validate` to check for errors

3. **Package installation failures**
   - Ensure language-specific package managers are installed
   - Check network connectivity
   - Verify package names and versions

### Debug Mode

Enable debug logging by setting the `debug` configuration option to `true`:

```ini
[global]
debug = true
```

## License

This project is licensed under the same license as the main TuskLang project.

## Support

For support and questions:
- Check the troubleshooting section
- Review language-specific SDK documentation
- Open an issue on the project repository 
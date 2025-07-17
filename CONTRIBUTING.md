# Contributing to TuskLang

Thank you for your interest in contributing to TuskLang! This document provides guidelines and information for contributors.

## 🎯 How to Contribute

### Types of Contributions

We welcome contributions in many forms:

- **Code**: Bug fixes, new features, performance improvements
- **Documentation**: Guides, tutorials, API documentation
- **Testing**: Unit tests, integration tests, performance tests
- **Design**: UI/UX improvements, logos, website design
- **Community**: Answering questions, organizing events, moderation
- **Translation**: Documentation in other languages
- **Examples**: Sample code, demos, tutorials

### Before You Start

1. **Check Existing Issues**: Search for existing issues or discussions
2. **Read Documentation**: Familiarize yourself with the project
3. **Join Community**: Participate in discussions and ask questions
4. **Set Up Environment**: Follow the development setup guide

## 🛠️ Development Setup

### Prerequisites

- **Git**: Latest version
- **Python**: 3.8 or higher
- **Go**: 1.19 or higher (for Go SDK)
- **Rust**: 1.70 or higher (for Rust SDK)
- **Node.js**: 16 or higher (for JavaScript SDK)

### Getting Started

```bash
# Clone repository
git clone https://github.com/cyber-boost/tusktsk.git
cd tusklang

# Set up Python development
cd implementations/python
pip install -e ".[dev]"

# Set up Go development
cd ../go
go mod tidy

# Set up Rust development
cd ../rust
cargo build

# Run tests
cd ../python
pytest

cd ../go
go test ./...

cd ../rust
cargo test
```

### Development Tools

#### Python
```bash
# Install development dependencies
pip install -r requirements-dev.txt

# Run linting
black implementations/python/
flake8 implementations/python/
mypy implementations/python/

# Run tests
pytest tests/ -v

# Run benchmarks
python tools/performance_benchmark.py results/
```

#### Go
```bash
# Install development tools
go install golang.org/x/tools/cmd/goimports@latest
go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest

# Format code
goimports -w implementations/go/

# Run linting
golangci-lint run implementations/go/

# Run tests
go test ./implementations/go/... -v

# Run benchmarks
go test ./implementations/go/... -bench=.
```

#### Rust
```bash
# Format code
cargo fmt

# Run linting
cargo clippy

# Run tests
cargo test

# Run benchmarks
cargo bench
```

## 📝 Code Style Guidelines

### Python

#### Style Guide
- Follow [PEP 8](https://www.python.org/dev/peps/pep-0008/)
- Use [Black](https://black.readthedocs.io/) for code formatting
- Use [isort](https://pycqa.github.io/isort/) for import sorting
- Use [mypy](https://mypy.readthedocs.io/) for type checking

#### Code Example
```python
"""TuskLang configuration management module."""

from typing import Any, Dict, Optional
import logging

from .exceptions import ConfigurationError
from .validators import SchemaValidator

logger = logging.getLogger(__name__)


class TuskConfig:
    """Main configuration class for TuskLang."""

    def __init__(self, schema: Optional[SchemaValidator] = None) -> None:
        """Initialize configuration with optional schema validation.

        Args:
            schema: Optional schema validator for configuration validation.
        """
        self._data: Dict[str, Any] = {}
        self._schema = schema
        self._dirty = False

    def set(self, key: str, value: Any) -> None:
        """Set a configuration value.

        Args:
            key: Configuration key in dot notation.
            value: Configuration value.

        Raises:
            ConfigurationError: If validation fails.
        """
        if self._schema and not self._schema.validate(key, value):
            raise ConfigurationError(f"Invalid value for key '{key}'")

        self._data[key] = value
        self._dirty = True
        logger.debug("Set configuration key '%s'", key)

    def get(self, key: str, default: Optional[Any] = None) -> Any:
        """Get a configuration value.

        Args:
            key: Configuration key in dot notation.
            default: Default value if key not found.

        Returns:
            Configuration value or default.
        """
        return self._data.get(key, default)
```

### Go

#### Style Guide
- Follow [Effective Go](https://golang.org/doc/effective_go.html)
- Use [gofmt](https://golang.org/cmd/gofmt/) for formatting
- Use [golint](https://github.com/golang/lint) for linting
- Use [go vet](https://golang.org/cmd/vet/) for static analysis

#### Code Example
```go
// Package config provides configuration management for TuskLang.
package config

import (
	"errors"
	"fmt"
	"sync"
)

// ErrKeyNotFound is returned when a configuration key is not found.
var ErrKeyNotFound = errors.New("configuration key not found")

// Config represents a TuskLang configuration.
type Config struct {
	data   map[string]interface{}
	schema *Schema
	mutex  sync.RWMutex
}

// New creates a new configuration instance.
func New() *Config {
	return &Config{
		data: make(map[string]interface{}),
	}
}

// Set sets a configuration value.
func (c *Config) Set(key string, value interface{}) error {
	c.mutex.Lock()
	defer c.mutex.Unlock()

	if c.schema != nil {
		if err := c.schema.Validate(key, value); err != nil {
			return fmt.Errorf("validation failed for key '%s': %w", key, err)
		}
	}

	c.data[key] = value
	return nil
}

// Get retrieves a configuration value.
func (c *Config) Get(key string) (interface{}, error) {
	c.mutex.RLock()
	defer c.mutex.RUnlock()

	value, exists := c.data[key]
	if !exists {
		return nil, ErrKeyNotFound
	}

	return value, nil
}
```

### Rust

#### Style Guide
- Follow [Rust Style Guide](https://doc.rust-lang.org/1.0.0/style/style/naming/README.html)
- Use [rustfmt](https://github.com/rust-lang/rustfmt) for formatting
- Use [clippy](https://github.com/rust-lang/rust-clippy) for linting
- Use [cargo check](https://doc.rust-lang.org/cargo/commands/cargo-check.html) for compilation checks

#### Code Example
```rust
//! TuskLang configuration management module.

use std::collections::HashMap;
use std::sync::RwLock;

use serde::{Deserialize, Serialize};
use thiserror::Error;

/// Errors that can occur during configuration operations.
#[derive(Error, Debug)]
pub enum ConfigError {
    #[error("Key not found: {0}")]
    KeyNotFound(String),
    #[error("Validation failed: {0}")]
    ValidationError(String),
    #[error("Serialization error: {0}")]
    SerializationError(#[from] serde_json::Error),
}

/// Main configuration struct for TuskLang.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Config {
    data: RwLock<HashMap<String, serde_json::Value>>,
}

impl Config {
    /// Creates a new configuration instance.
    pub fn new() -> Self {
        Self {
            data: RwLock::new(HashMap::new()),
        }
    }

    /// Sets a configuration value.
    pub fn set<K, V>(&self, key: K, value: V) -> Result<(), ConfigError>
    where
        K: Into<String>,
        V: Serialize,
    {
        let key = key.into();
        let value = serde_json::to_value(value)?;
        
        if let Ok(mut data) = self.data.write() {
            data.insert(key, value);
        }
        
        Ok(())
    }

    /// Gets a configuration value.
    pub fn get<T>(&self, key: &str) -> Result<T, ConfigError>
    where
        T: for<'de> Deserialize<'de>,
    {
        if let Ok(data) = self.data.read() {
            if let Some(value) = data.get(key) {
                return T::deserialize(value.clone())
                    .map_err(|e| ConfigError::SerializationError(e));
            }
        }
        
        Err(ConfigError::KeyNotFound(key.to_string()))
    }
}
```

## 🧪 Testing Guidelines

### Test Structure

#### Python
```python
"""Tests for TuskConfig class."""

import pytest
from tusklang import TuskConfig, ConfigurationError


class TestTuskConfig:
    """Test cases for TuskConfig class."""

    def test_set_and_get(self):
        """Test setting and getting configuration values."""
        config = TuskConfig()
        
        config.set("app.name", "Test App")
        config.set("app.version", "1.0.0")
        
        assert config.get("app.name") == "Test App"
        assert config.get("app.version") == "1.0.0"

    def test_get_with_default(self):
        """Test getting values with default fallback."""
        config = TuskConfig()
        
        value = config.get("missing.key", default="default_value")
        assert value == "default_value"

    def test_validation_error(self):
        """Test validation error handling."""
        config = TuskConfig(schema=MockSchema())
        
        with pytest.raises(ConfigurationError):
            config.set("invalid.key", "invalid_value")

    @pytest.mark.performance
    def test_large_configuration(self):
        """Test performance with large configurations."""
        config = TuskConfig()
        
        # Add 10,000 configuration items
        for i in range(10000):
            config.set(f"item.{i}", f"value_{i}")
        
        # Verify all items are accessible
        for i in range(10000):
            assert config.get(f"item.{i}") == f"value_{i}"
```

#### Go
```go
package config

import (
	"testing"
)

func TestConfig_SetAndGet(t *testing.T) {
	config := New()

	err := config.Set("app.name", "Test App")
	if err != nil {
		t.Fatalf("Failed to set config: %v", err)
	}

	value, err := config.Get("app.name")
	if err != nil {
		t.Fatalf("Failed to get config: %v", err)
	}

	if value != "Test App" {
		t.Errorf("Expected 'Test App', got '%v'", value)
	}
}

func TestConfig_GetWithDefault(t *testing.T) {
	config := New()

	value := config.GetString("missing.key", "default_value")
	if value != "default_value" {
		t.Errorf("Expected 'default_value', got '%s'", value)
	}
}

func TestConfig_ValidationError(t *testing.T) {
	schema := &MockSchema{}
	config := NewWithSchema(schema)

	err := config.Set("invalid.key", "invalid_value")
	if err == nil {
		t.Error("Expected validation error")
	}
}

func BenchmarkConfig_Set(b *testing.B) {
	config := New()

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		config.Set(fmt.Sprintf("key.%d", i), fmt.Sprintf("value_%d", i))
	}
}
```

#### Rust
```rust
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_set_and_get() {
        let config = Config::new();
        
        config.set("app.name", "Test App").unwrap();
        config.set("app.version", "1.0.0").unwrap();
        
        let name: String = config.get("app.name").unwrap();
        let version: String = config.get("app.version").unwrap();
        
        assert_eq!(name, "Test App");
        assert_eq!(version, "1.0.0");
    }

    #[test]
    fn test_get_with_default() {
        let config = Config::new();
        
        let value: String = config.get("missing.key")
            .unwrap_or_else(|_| "default_value".to_string());
        
        assert_eq!(value, "default_value");
    }

    #[test]
    fn test_validation_error() {
        let config = Config::new();
        
        let result = config.set("invalid.key", "invalid_value");
        assert!(result.is_err());
    }

    #[bench]
    fn bench_set(bencher: &mut test::Bencher) {
        let config = Config::new();
        
        bencher.iter(|| {
            for i in 0..1000 {
                config.set(format!("key.{}", i), format!("value_{}", i)).unwrap();
            }
        });
    }
}
```

### Test Categories

#### Unit Tests
- Test individual functions and methods
- Mock external dependencies
- Test edge cases and error conditions
- Aim for >90% code coverage

#### Integration Tests
- Test component interactions
- Test end-to-end workflows
- Test with real dependencies
- Test performance characteristics

#### Performance Tests
- Benchmark critical operations
- Test memory usage
- Test scalability
- Monitor for regressions

## 📚 Documentation Guidelines

### Code Documentation

#### Python
```python
def secure_file_write(
    self, 
    path: Path, 
    data: bytes, 
    metadata: Optional[Dict[str, Any]] = None
) -> None:
    """Write data to a secure file with encryption and signature.
    
    This method writes data to a file with optional encryption and digital
    signature. The file format includes metadata, encryption layer, and
    integrity checks.
    
    Args:
        path: File path to write to.
        data: Binary data to write.
        metadata: Optional metadata dictionary.
        
    Raises:
        SecurityError: If encryption or signing fails.
        IOError: If file operations fail.
        
    Example:
        >>> validator = SecurityValidator()
        >>> validator.secure_file_write(
        ...     Path("secret.bin"),
        ...     b"sensitive data",
        ...     {"description": "API keys"}
        ... )
    """
```

#### Go
```go
// SecureFileWrite writes data to a secure file with encryption and signature.
//
// This method writes data to a file with optional encryption and digital
// signature. The file format includes metadata, encryption layer, and
// integrity checks.
//
// Parameters:
//   - path: File path to write to
//   - data: Binary data to write
//   - metadata: Optional metadata map
//
// Returns:
//   - error: SecurityError if encryption/signing fails, IOError if file operations fail
//
// Example:
//
//	validator := NewSecurityValidator()
//	err := validator.SecureFileWrite(
//		"secret.bin",
//		[]byte("sensitive data"),
//		map[string]interface{}{"description": "API keys"},
//	)
func (v *SecurityValidator) SecureFileWrite(
	path string,
	data []byte,
	metadata map[string]interface{},
) error {
```

#### Rust
```rust
/// Writes data to a secure file with encryption and signature.
///
/// This method writes data to a file with optional encryption and digital
/// signature. The file format includes metadata, encryption layer, and
/// integrity checks.
///
/// # Arguments
///
/// * `path` - File path to write to
/// * `data` - Binary data to write
/// * `metadata` - Optional metadata map
///
/// # Returns
///
/// Returns `Ok(())` on success, or an error if encryption/signing fails.
///
/// # Errors
///
/// * `SecurityError` - If encryption or signing fails
/// * `IOError` - If file operations fail
///
/// # Examples
///
/// ```
/// use std::collections::HashMap;
/// use tusklang::SecurityValidator;
///
/// let validator = SecurityValidator::new();
/// let mut metadata = HashMap::new();
/// metadata.insert("description".to_string(), "API keys".to_string());
///
/// validator.secure_file_write(
///     "secret.bin",
///     b"sensitive data",
///     Some(metadata)
/// )?;
/// ```
pub fn secure_file_write<P, M>(
    &self,
    path: P,
    data: &[u8],
    metadata: Option<M>,
) -> Result<(), SecurityError>
where
    P: AsRef<Path>,
    M: Serialize,
{
```

### API Documentation

#### Structure
- **Overview**: High-level description
- **Installation**: Setup instructions
- **Quick Start**: Basic usage examples
- **API Reference**: Complete method documentation
- **Examples**: Real-world use cases
- **Troubleshooting**: Common issues and solutions

#### Writing Style
- Use clear, concise language
- Include code examples
- Explain complex concepts
- Provide context and use cases
- Keep documentation up to date

## 🔄 Pull Request Process

### Before Submitting

1. **Fork the Repository**
   ```bash
   git clone https://github.com/your-username/tusklang.git
   cd tusklang
   git remote add upstream https://github.com/cyber-boost/tusktsk.git
   ```

2. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make Your Changes**
   - Follow coding standards
   - Write tests for new features
   - Update documentation
   - Test across platforms

4. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "feat: add new configuration validation feature

   - Add schema validation support
   - Implement custom validator interface
   - Add comprehensive test coverage
   - Update documentation with examples"
   ```

### Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

#### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes
- `refactor`: Code refactoring
- `test`: Test changes
- `chore`: Build or tool changes

#### Examples
```
feat(config): add schema validation support

- Implement SchemaValidator interface
- Add JSON Schema support
- Include comprehensive test coverage

Closes #123
```

```
fix(security): resolve encryption key derivation issue

- Fix PBKDF2 iteration count
- Add key validation
- Update security documentation

Fixes #456
```

### Submitting the PR

1. **Push Your Branch**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create Pull Request**
   - Use descriptive title
   - Fill out PR template
   - Link related issues
   - Request appropriate reviewers

3. **PR Template**
   ```markdown
   ## Description
   Brief description of changes

   ## Type of Change
   - [ ] Bug fix
   - [ ] New feature
   - [ ] Breaking change
   - [ ] Documentation update

   ## Testing
   - [ ] Unit tests pass
   - [ ] Integration tests pass
   - [ ] Performance tests pass
   - [ ] Manual testing completed

   ## Documentation
   - [ ] Code documented
   - [ ] API documentation updated
   - [ ] Examples provided
   - [ ] README updated if needed

   ## Checklist
   - [ ] Code follows style guidelines
   - [ ] Self-review completed
   - [ ] No breaking changes
   - [ ] All tests pass
   ```

### Review Process

1. **Automated Checks**
   - CI/CD pipeline runs tests
   - Code coverage is measured
   - Security scans are performed
   - Performance benchmarks run

2. **Code Review**
   - Maintainers review code
   - Community members can review
   - Address feedback and suggestions
   - Make requested changes

3. **Approval and Merge**
   - At least one maintainer approval required
   - All checks must pass
   - Documentation must be complete
   - Breaking changes require special approval

## 🏷️ Issue Guidelines

### Creating Issues

#### Bug Reports
```markdown
## Bug Description
Clear description of the bug

## Steps to Reproduce
1. Step one
2. Step two
3. Step three

## Expected Behavior
What should happen

## Actual Behavior
What actually happens

## Environment
- OS: Ubuntu 20.04
- Python: 3.9.0
- TuskLang: 1.0.0

## Additional Information
Screenshots, logs, etc.
```

#### Feature Requests
```markdown
## Feature Description
Clear description of the feature

## Use Case
Why this feature is needed

## Proposed Solution
How the feature should work

## Alternatives Considered
Other approaches considered

## Additional Information
Mockups, examples, etc.
```

### Issue Labels

- `bug`: Something isn't working
- `enhancement`: New feature or request
- `documentation`: Improvements to documentation
- `good first issue`: Good for newcomers
- `help wanted`: Extra attention needed
- `priority: high`: High priority issue
- `priority: low`: Low priority issue
- `python`, `go`, `rust`: Language-specific issues

## 🎯 Contribution Areas

### High Priority
- **Security**: Security improvements and vulnerability fixes
- **Performance**: Performance optimizations and benchmarks
- **Documentation**: API documentation and tutorials
- **Testing**: Test coverage and test infrastructure

### Medium Priority
- **Features**: New functionality and enhancements
- **Bug Fixes**: Bug reports and fixes
- **Tools**: CLI tools and utilities
- **Examples**: Sample code and demos

### Low Priority
- **Documentation**: Grammar and style improvements
- **Code Style**: Code formatting and style changes
- **Refactoring**: Code organization and cleanup
- **Examples**: Additional examples and tutorials

## 🏆 Recognition

### Contributor Recognition
- Contributors listed on project page
- GitHub profile shows contribution activity
- Community badges for different contribution types
- Recognition in release notes

### Profit Sharing
Active contributors may be eligible for profit sharing from commercial licensing revenue. See [License Documentation](docs/license.md) for details.

### Community Events
- Invitation to contributor events
- Speaking opportunities at conferences
- Recognition in community communications
- Networking opportunities

## 📞 Getting Help

### Questions and Support
- **GitHub Discussions**: [Discussions](https://github.com/cyber-boost/tusktsk/discussions)
- **Discord**: [Discord Community](https://discord.gg/tusklang)
- **Email**: [contributors@tusklang.org](mailto:contributors@tusklang.org)

### Mentorship
- **Mentorship Program**: Pair with experienced contributors
- **Code Reviews**: Get feedback on your code
- **Documentation**: Help with writing and reviewing docs
- **Testing**: Learn about testing best practices

---

**Thank you for contributing to TuskLang! Your contributions help make configuration management better for everyone.** 🚀

*Have questions? Reach out to us at [contributors@tusklang.org](mailto:contributors@tusklang.org)*
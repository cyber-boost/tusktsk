# Contributing to TuskLang Go SDK

Thank you for your interest in contributing to the TuskLang Go SDK! This document provides guidelines and information for contributors.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Release Process](#release-process)
- [Documentation](#documentation)
- [Support](#support)

## Code of Conduct

### Our Standards

- Use welcoming and inclusive language
- Be respectful of differing viewpoints and experiences
- Gracefully accept constructive criticism
- Focus on what is best for the community
- Show empathy towards other community members

### Enforcement

Instances of abusive, harassing, or otherwise unacceptable behavior may be reported by contacting the project team.

## Getting Started

### Prerequisites

- Go 1.22 or higher (1.23 recommended)
- Git
- Docker (optional)
- Make (optional)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:

```bash
git clone https://github.com/YOUR_USERNAME/tusktsk.git
cd tusktsk/sdk/go
```

3. Add the upstream repository:

```bash
git remote add upstream https://github.com/cyber-boost/tusktsk.git
```

## Development Setup

### 1. Install Dependencies

```bash
# Install Go dependencies
go mod download
go mod tidy

# Install development tools
go install golang.org/x/tools/cmd/goimports@latest
go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest
go install github.com/securecodewarrior/gosec/v2/cmd/gosec@latest
```

### 2. Development Environment

```bash
# Set up pre-commit hooks
cp .git/hooks/pre-commit.sample .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit

# Install development dependencies
make install-dev
```

### 3. Database Setup

```bash
# Start services with Docker Compose
docker-compose up -d

# Or install locally
# MySQL, Redis, MongoDB (see INSTALL.md)
```

## Coding Standards

### Go Code Style

1. **Follow Go conventions**: Use `gofmt` and `goimports`
2. **Use meaningful names**: Variables, functions, and packages
3. **Write clear comments**: Document exported functions and types
4. **Keep functions small**: Single responsibility principle
5. **Handle errors properly**: Don't ignore errors

### Code Formatting

```bash
# Format code
go fmt ./...

# Organize imports
goimports -w .

# Run linter
golangci-lint run
```

### File Organization

```
pkg/
├── core/          # Core SDK functionality
├── cli/           # Command-line interface
├── config/        # Configuration management
├── security/      # Security features
└── utils/         # Utility functions

internal/          # Private implementation
├── database/      # Database operations
├── cache/         # Caching layer
└── api/           # API handlers
```

### Naming Conventions

- **Packages**: Lowercase, single word
- **Functions**: MixedCaps or mixedCaps
- **Variables**: MixedCaps or mixedCaps
- **Constants**: MixedCaps
- **Types**: MixedCaps
- **Interfaces**: MixedCaps with 'er' suffix if appropriate

### Error Handling

```go
// Good
if err != nil {
    return fmt.Errorf("failed to process data: %w", err)
}

// Bad
if err != nil {
    return err
}
```

## Testing

### Running Tests

```bash
# Run all tests
go test ./...

# Run tests with coverage
go test -cover ./...

# Run tests with race detection
go test -race ./...

# Run tests with verbose output
go test -v ./...

# Run specific test
go test -v ./pkg/core -run TestFunctionName
```

### Writing Tests

1. **Test file naming**: `*_test.go`
2. **Test function naming**: `TestFunctionName`
3. **Table-driven tests**: For multiple test cases
4. **Mocking**: Use interfaces for testability
5. **Benchmarks**: For performance-critical code

### Example Test

```go
func TestProcessData(t *testing.T) {
    tests := []struct {
        name    string
        input   string
        want    string
        wantErr bool
    }{
        {
            name:    "valid input",
            input:   "test",
            want:    "processed_test",
            wantErr: false,
        },
        {
            name:    "empty input",
            input:   "",
            want:    "",
            wantErr: true,
        },
    }

    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            got, err := ProcessData(tt.input)
            if (err != nil) != tt.wantErr {
                t.Errorf("ProcessData() error = %v, wantErr %v", err, tt.wantErr)
                return
            }
            if got != tt.want {
                t.Errorf("ProcessData() = %v, want %v", got, tt.want)
            }
        })
    }
}
```

### Benchmarking

```go
func BenchmarkProcessData(b *testing.B) {
    for i := 0; i < b.N; i++ {
        ProcessData("test_data")
    }
}
```

## Pull Request Process

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

### 2. Make Changes

- Write code following the coding standards
- Add tests for new functionality
- Update documentation
- Update dependencies if needed

### 3. Commit Changes

```bash
# Stage changes
git add .

# Commit with descriptive message
git commit -m "feat: add new feature for data processing

- Add ProcessData function
- Add comprehensive tests
- Update documentation
- Fixes #123"
```

### 4. Push and Create PR

```bash
git push origin feature/your-feature-name
```

### 5. Pull Request Guidelines

- **Title**: Clear and descriptive
- **Description**: Explain what and why, not how
- **Related issues**: Link to relevant issues
- **Screenshots**: For UI changes
- **Tests**: Ensure all tests pass
- **Documentation**: Update relevant docs

### 6. Review Process

- Address review comments
- Keep commits atomic and focused
- Squash commits if requested
- Update PR description if needed

## Release Process

### Versioning

We follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Incompatible API changes
- **MINOR**: New functionality (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Steps

1. **Update version** in `go.mod`
2. **Create release branch**: `release/v1.2.3`
3. **Update changelog**: Document changes
4. **Run full test suite**: Ensure everything works
5. **Create tag**: `git tag -a v1.2.3 -m "Release v1.2.3"`
6. **Push tag**: `git push origin v1.2.3`
7. **Create GitHub release**: With release notes
8. **Merge to main**: After successful release

### Automated Release

The release process is automated via GitHub Actions:

```yaml
# .github/workflows/release.yml
on:
  push:
    tags:
      - 'v*'
```

## Documentation

### Documentation Standards

1. **README.md**: Project overview and quick start
2. **INSTALL.md**: Detailed installation instructions
3. **DOCKER.md**: Docker-specific documentation
4. **API documentation**: Code comments and examples
5. **CHANGELOG.md**: Version history and changes

### Writing Documentation

- **Clear and concise**: Avoid jargon
- **Examples**: Provide working code examples
- **Structure**: Use headers and lists
- **Links**: Link to related documentation
- **Updates**: Keep documentation current

### Code Comments

```go
// ProcessData processes the input string and returns the result.
// It validates the input and applies transformations based on the configuration.
// Returns an error if the input is invalid or processing fails.
func ProcessData(input string) (string, error) {
    // Implementation
}
```

## Security

### Security Guidelines

1. **Input validation**: Validate all inputs
2. **Error handling**: Don't expose sensitive information
3. **Dependencies**: Keep dependencies updated
4. **Secrets**: Never commit secrets to the repository
5. **Vulnerabilities**: Report security issues privately

### Security Reporting

For security issues, please email security@cyber-boost.com instead of creating a public issue.

## Performance

### Performance Guidelines

1. **Benchmarking**: Benchmark performance-critical code
2. **Profiling**: Use Go's profiling tools
3. **Memory usage**: Monitor memory allocation
4. **Concurrency**: Use goroutines appropriately
5. **Caching**: Implement caching where beneficial

### Performance Tools

```bash
# CPU profiling
go test -cpuprofile=cpu.prof -bench=.

# Memory profiling
go test -memprofile=mem.prof -bench=.

# Analyze profiles
go tool pprof cpu.prof
go tool pprof mem.prof
```

## Support

### Getting Help

1. **Documentation**: Check existing documentation
2. **Issues**: Search existing issues
3. **Discussions**: Use GitHub Discussions
4. **Email**: Contact the development team

### Communication Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General questions and discussions
- **Email**: Security issues and private matters
- **Slack**: Community discussions (if available)

### Issue Templates

Use the appropriate issue template:

- **Bug report**: For bugs and issues
- **Feature request**: For new features
- **Documentation**: For documentation improvements
- **Security**: For security issues

## Recognition

### Contributors

Contributors will be recognized in:

- **README.md**: List of contributors
- **Release notes**: Credit for contributions
- **GitHub**: Contributor statistics
- **Documentation**: Author attribution

### Contribution Levels

- **Contributor**: Any contribution
- **Maintainer**: Regular contributions and reviews
- **Core Maintainer**: Project leadership and direction

## License

By contributing to this project, you agree that your contributions will be licensed under the same license as the project (MIT License).

## Questions?

If you have questions about contributing, please:

1. Check this document first
2. Search existing issues and discussions
3. Create a new discussion or issue
4. Contact the development team

Thank you for contributing to TuskLang Go SDK! 
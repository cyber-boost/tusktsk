# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TuskLang is a next-generation configuration management system combining human-readable syntax with enterprise security, performance, and cross-platform support. It includes:
- A custom configuration language (.tsk files)
- Binary format (.pnt files) for optimized storage and performance
- Multi-language SDKs (Python, Go, JavaScript, PHP, Ruby, Rust, Bash, C#, Java)
- Enterprise features: OAuth2, digital signatures, encryption, audit logging
- Advanced operators: @variables, @graphql, @kubernetes, etc.

## Key Development Commands

### Python SDK
```bash
# Install dependencies
pip install -r sdk/python/requirements.txt

# Run tests
pytest sdk/python/

# Build binary
python sdk/python/tsk.py compile input.tsk -o output.pnt
```

### Go SDK
```bash
# Build CLI
cd sdk/go && make build

# Run tests
make test

# Run with coverage
make test-coverage

# Install globally
make install
```

### JavaScript SDK
```bash
# Install dependencies
cd sdk/js && npm install

# Run tests
npm test

# Build with webpack
npm run build
```

### PHP Development (Fujsen Server)
```bash
# Start Fujsen development server
cd fujsen && ./start-fujsen.sh

# Install dependencies
composer install

# Run PHP tests
php test-enhanced.php
```

### Rust SDK
```bash
# Build with Cargo
cd sdk/rust && cargo build

# Run tests
cargo test

# Run with optimizations
cargo build --release

# Lint and format
cargo clippy && cargo fmt
```

### C# SDK
```bash
# Build project
cd sdk/csharp && dotnet build

# Run tests
dotnet test

# Run CLI
dotnet run --project CLI
```

### Java SDK
```bash
# Build with Maven
cd sdk/java && mvn compile

# Run tests
mvn test

# Run comprehensive test suite
./run-all-tests.sh
```

### Ruby SDK
```bash
# Install dependencies
cd sdk/ruby && bundle install

# Run tests
bundle exec rspec

# Build gem
gem build tusk_lang.gemspec
```

### Bash SDK
```bash
# Run tests
cd sdk/bash && ./test-complete.sh

# Run enhanced tests
./tsk-enhanced-complete.sh
```

## Architecture Overview

### Core Components

1. **Language Parser & Compiler**
   - Parser implementations in each SDK (e.g., `sdk/python/tsk.py`, `sdk/go/parser.go`)
   - Enhanced parsers with advanced features (`tsk_enhanced.py`, `parser_enhanced.go`)
   - Binary format handlers for .pnt files

2. **Fujsen Runtime Server** (`fujsen/`)
   - PHP-based runtime for executing .tsk files
   - Supports web endpoints, API routes, database integration
   - Advanced features: JIT optimization, plugin system, GraphQL support

3. **Security Infrastructure**
   - Digital signatures (Ed25519, RSA-2048)
   - Encryption (AES-256-GCM, ChaCha20-Poly1305)
   - License validation and protection mechanisms
   - OAuth2 integration for enterprise authentication

4. **Package Registry System** (`registry/`, `fujsen/api/registry*`)
   - Secure package distribution
   - Version management and dependency resolution
   - CDN integration for global distribution

### File Types

- `.tsk` - TuskLang source files (human-readable configuration)
- `.pnt` - Compiled binary format (optimized for performance)
- `.peanuts` - Alternative configuration format
- `.shell` - Shell storage format for configuration

### Key Operators (@-syntax)

TuskLang uses @ operators for dynamic functionality. All SDKs now support 100+ operators:

#### Core Operators
- `@variable` - Variable references with fallback support
- `@env` - Environment variables
- `@request` - HTTP request data access
- `@session`, `@cookie` - Session and cookie management
- `@query` - Database operations
- `@cache` - Caching functionality

#### Communication Operators  
- `@graphql` - GraphQL integration
- `@grpc` - gRPC service calls
- `@websocket` - WebSocket connections
- `@email` - Email operations
- `@slack`, `@discord`, `@teams` - Messaging integrations

#### Database Operators
- `@mysql`, `@postgresql`, `@mongodb` - Database-specific operations
- `@redis` - Redis caching
- `@elasticsearch` - Search operations
- `@influxdb` - Time-series data

#### Cloud Operators
- `@aws`, `@azure`, `@gcp` - Cloud platform integration
- `@kubernetes` - K8s operator support
- `@docker` - Container operations

#### Security Operators
- `@encrypt`, `@decrypt` - Encryption operations
- `@jwt` - JWT token management
- `@oauth`, `@saml` - Authentication protocols
- `@vault` - HashiCorp Vault integration

#### AI/ML Operators
- `@ai` - AI model integration
- `@ml` - Machine learning operations
- `@nlp` - Natural language processing
- `@vision` - Computer vision

### Operator Registry System

Each SDK implements a standardized operator registry:
- **Go**: `sdk/go/operators/registry.go`
- **JavaScript**: Modular operator system in `src/`
- **Python**: `real_operators.py` with comprehensive implementations
- **C#**: `Operators/` directory with categorized implementations
- **Java**: Operator classes in main package
- **Ruby**: `lib/tusk_lang/operators/`
- **PHP**: `src/CoreOperators/` with 50+ operator implementations
- **Rust**: `src/operators/` with trait-based system

### Testing Strategy

1. **Unit Tests**: Each SDK has its own test suite
2. **Integration Tests**: `tests/` directory contains cross-SDK tests
3. **Cross-Language Tests**: `sdk/run-all-tests.sh` for SDK consistency
4. **Performance Tests**: `tools/performance_benchmark.py`
5. **Security Tests**: `security/` directory for vulnerability testing
6. **CLI Tests**: Each SDK includes comprehensive CLI testing with commands like `ai`, `binary`, `cache`, `config`, `db`, `dev`, `service`, `test`, `utility`

## CLI Architecture

Each SDK now includes a comprehensive CLI with standardized commands:

### Core CLI Commands
- `ai` - AI/ML integration commands
- `binary` - Binary format (.pnt) operations
- `cache` - Cache management
- `config` - Configuration management
- `db` - Database operations
- `dev` - Development tools
- `service` - Service management
- `test` - Testing utilities
- `utility` - General utilities

### CLI Implementation Locations
- **Go**: `sdk/go/cmd/tsk/` with Makefile automation
- **JavaScript**: `sdk/javascript/cli/` with npm scripts
- **Python**: `sdk/python/cli/` with Click framework
- **C#**: `sdk/csharp/CLI/` with .NET CLI
- **Java**: `sdk/java/src/main/java/` with Maven
- **Ruby**: `sdk/ruby/cli/` with Thor gem
- **Bash**: `sdk/bash/cli/` with pure shell
- **PHP**: `sdk/php/cli/` with Composer scripts
- **Rust**: `sdk/rust/src/commands/` with Clap

## Important Patterns

### Adding New Features
1. Implement in the base parser/compiler first
2. Add to enhanced versions for advanced functionality
3. Update all SDK implementations to maintain consistency
4. Add comprehensive tests for the new feature
5. Update documentation and cheat sheets
6. Add CLI command support across all SDKs

### Security Considerations
- All package distributions must be digitally signed
- Sensitive operations require license validation
- Binary obfuscation is applied to protect intellectual property
- Audit logging tracks all critical operations

### Performance Optimization
- Use binary .pnt format for production deployments (85% speed improvement)
- Enable JIT compilation in Fujsen for hot paths
- Implement caching at multiple levels
- Use compression for network transfers
- Each SDK includes `PeanutConfig` class for binary operations
- Cross-platform binary format compatibility
- Zero-copy parsing in Rust implementation
- WASM support for browser environments

## Development Workflow

1. **Feature Development**: Start with Python SDK (reference implementation)
2. **Cross-SDK Consistency**: Port features to all SDKs maintaining API compatibility
3. **Testing**: Write tests before implementing features
4. **Documentation**: Update relevant docs and cheat sheets
5. **Security Review**: All changes undergo security validation

## Common Issues & Solutions

### Binary Format Compatibility
- Version field in binary header ensures compatibility
- Use migration tools when format changes occur

### Cross-Platform Path Handling
- Always use absolute paths in tools
- Handle Windows/Unix path differences in SDKs

### Performance Bottlenecks
- Profile with `tools/performance_benchmark.py`
- Check JIT optimization in Fujsen logs
- Monitor memory usage for large configurations
# TuskLang Java CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang Java SDK.

## Quick Links

- [Installation](./installation.md)
- [Quick Start](./quickstart.md)
- [Command Reference](./commands/README.md)
- [Examples](./examples/README.md)
- [Troubleshooting](./troubleshooting.md)

## Command Categories

- [Database Commands](./commands/db/README.md) - Database operations
- [Development Commands](./commands/dev/README.md) - Development tools
- [Testing Commands](./commands/test/README.md) - Test execution
- [Service Commands](./commands/services/README.md) - Service management
- [Cache Commands](./commands/cache/README.md) - Cache operations
- [Configuration Commands](./commands/config/README.md) - Config management
- [Binary Commands](./commands/binary/README.md) - Binary compilation
- [Peanuts Commands](./commands/peanuts/README.md) - Peanut config
- [AI Commands](./commands/ai/README.md) - AI integrations
- [CSS Commands](./commands/css/README.md) - CSS utilities
- [License Commands](./commands/license/README.md) - License management
- [Utility Commands](./commands/utility/README.md) - General utilities

## Version

This documentation is for TuskLang Java SDK v2.0.0

## Overview

The TuskLang Java CLI provides a comprehensive command-line interface for managing TuskLang projects, configurations, and development workflows. Built with Java 8+ compatibility and Spring Boot integration, it offers enterprise-grade features for production environments.

## Key Features

- **Database Management**: SQLite, PostgreSQL, and MySQL support
- **Development Server**: Built-in HTTP server with hot reloading
- **Testing Framework**: Comprehensive test execution and reporting
- **Configuration Management**: Hierarchical .pnt configuration system
- **Binary Compilation**: High-performance .tsk to .pnt compilation
- **AI Integration**: Claude, ChatGPT, and custom AI model support
- **Cache Management**: Memcached and Redis operations
- **Service Orchestration**: Start, stop, and monitor services

## Installation

```bash
# Install via Maven
mvn install

# Or download JAR directly
curl -sSL https://tusklang.org/java/tsk.jar -o tsk.jar
chmod +x tsk.jar
```

## Quick Start

```bash
# Check installation
tsk version

# Initialize a new project
tsk init

# Start development server
tsk serve 8080

# Run tests
tsk test all

# Compile configuration
tsk config compile ./
```

## Java-Specific Features

- **Spring Boot Integration**: Automatic configuration loading
- **Maven/Gradle Support**: Build system integration
- **JVM Optimization**: Memory and performance tuning
- **Thread Safety**: Concurrent access patterns
- **Type Safety**: Generic-based configuration access

## Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk db help
tsk config help

# Version information
tsk version
```

## Examples

See the [Examples](./examples/README.md) section for complete workflow examples and integration patterns. 
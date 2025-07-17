# Development Commands

Development tools for TuskLang Go CLI, providing server management, compilation, and optimization capabilities.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk serve](./serve.md) | Start development server |
| [tsk compile](./compile.md) | Compile .tsk file |
| [tsk optimize](./optimize.md) | Optimize .tsk file |

## Common Use Cases

- **Local Development**: Start development servers for testing
- **Code Compilation**: Compile TuskLang files to binary format
- **Performance Optimization**: Optimize code for production
- **Hot Reloading**: Automatic reloading during development
- **Debugging**: Development tools and debugging support

## Go-Specific Notes

The Go CLI development commands leverage Go's strengths:

- **Fast Compilation**: Quick build times for development
- **Cross-Platform**: Works on Linux, macOS, and Windows
- **Hot Reloading**: Automatic server restart on file changes
- **Performance Profiling**: Built-in profiling capabilities
- **Debugging Support**: Integrated debugging tools

## Examples

### Development Workflow

```bash
# Start development server
tsk serve 8080

# Compile configuration
tsk compile config.tsk

# Optimize for production
tsk optimize config.tsk
```

### Server Management

```bash
# Start server with hot reloading
tsk serve --hot-reload 3000

# Start server with specific configuration
tsk serve --config dev.peanuts 8080
```

### Code Compilation

```bash
# Compile single file
tsk compile app.tsk

# Compile with optimization
tsk compile --optimize app.tsk

# Compile to specific output
tsk compile -o app.bin app.tsk
``` 
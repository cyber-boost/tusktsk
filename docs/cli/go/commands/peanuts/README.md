# Peanuts Commands

Peanut Binary Configuration commands for TuskLang Go CLI, providing high-performance binary configuration management.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk peanuts compile](./compile.md) | Compile .peanuts to binary .pnt |
| [tsk peanuts auto-compile](./auto-compile.md) | Auto-compile on file changes |
| [tsk peanuts load](./load.md) | Load binary peanuts file |

## Common Use Cases

- **Binary Compilation**: Convert text configs to binary format
- **Performance Optimization**: 85% faster loading with binary format
- **Production Deployment**: Use binary configs in production
- **Configuration Inspection**: Load and inspect binary configs
- **Automated Compilation**: Auto-compile on configuration changes

## Go-Specific Notes

The Go CLI peanuts commands leverage Go's binary serialization strengths:

- **Fast Deserialization**: Optimized binary format loading
- **Type Safety**: Maintains Go type system compatibility
- **Memory Efficiency**: Compact binary representation
- **Cross-Platform**: Consistent binary format across platforms
- **Struct Mapping**: Direct mapping to Go structs

## Examples

### Binary Compilation

```bash
# Compile configuration to binary
tsk peanuts compile config.peanuts

# Compile with custom output
tsk peanuts compile -o config.pnt config.tsk
```

### Binary Loading

```bash
# Load and display binary config
tsk peanuts load config.pnt

# Load with specific format
tsk peanuts load --format json config.pnt
```

### Auto-Compilation

```bash
# Auto-compile on changes
tsk peanuts auto-compile

# Auto-compile with specific directory
tsk peanuts auto-compile --watch ./config/
``` 
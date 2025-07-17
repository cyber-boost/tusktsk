# Development Commands

Development tools and utilities for TuskLang projects.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk serve](./serve.md) | Start development server |
| [tsk compile](./compile.md) | Compile .tsk file |
| [tsk optimize](./optimize.md) | Optimize .tsk file |

## Common Use Cases

- **Local Development**: Start development servers for testing
- **Code Compilation**: Compile TuskLang files to binary format
- **Performance Optimization**: Optimize code for production use
- **Development Workflow**: Streamline development processes

## Bash-Specific Notes

### Development Environment Setup
```bash
# Set development environment
export NODE_ENV="development"
export TSK_DEV_MODE="true"

# Enable auto-reload
export TSK_AUTO_RELOAD="true"
```

### Development Workflow
```bash
#!/bin/bash
# dev_workflow.sh

echo "🔧 Starting development workflow..."

# Load development configuration
tsk peanuts load peanu.development.peanuts

# Start development server
tsk serve 3000 &

# Watch for file changes
inotifywait -m -r -e modify src/ | while read path action file; do
    if [[ "$file" =~ \.tsk$ ]]; then
        echo "Recompiling $file..."
        tsk compile "$path$file"
    fi
done
```

## Related Commands

- [Database Commands](../db/README.md) - Database operations
- [Testing Commands](../test/README.md) - Test execution
- [Configuration Commands](../config/README.md) - Config management 
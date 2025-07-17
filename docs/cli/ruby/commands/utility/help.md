# tsk utility help

Show help information

## Synopsis

```bash
tsk utility help [options]
```

## Description

Show help information for the TuskLang Ruby SDK.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |

## Arguments

No arguments required.

## Examples

### Basic Usage
```bash
# show help information
tsk utility help
```

**Output:**
```
✅ Command executed successfully
```

## Ruby API Usage

```ruby
# Programmatic usage
require 'tusk_lang'

# Example implementation
def help
  puts "Executing help..."
  # Implementation here
end
```

## Output

### Success Output
```
✅ Command executed successfully
```

### Error Output
```
❌ Command failed: [error message]
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Command executed |
| 1 | Error - Command failed |

## Related Commands

- [tsk utility](./README.md) - Utility commands overview

## Notes

- This is a placeholder for the help command
- Replace with actual implementation details
- Add Ruby-specific examples and integration patterns

## See Also

- [Utility Commands Overview](./README.md)
- [Examples](../../examples/basic-usage.md)

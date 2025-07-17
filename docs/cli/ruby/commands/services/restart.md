# tsk services restart

Restart TuskLang services

## Synopsis

```bash
tsk services restart [options]
```

## Description

Restart tusklang services for the TuskLang Ruby SDK.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |

## Arguments

No arguments required.

## Examples

### Basic Usage
```bash
# restart tusklang services
tsk services restart
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
def restart
  puts "Executing restart..."
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

- [tsk services](./README.md) - Services commands overview

## Notes

- This is a placeholder for the restart command
- Replace with actual implementation details
- Add Ruby-specific examples and integration patterns

## See Also

- [Services Commands Overview](./README.md)
- [Examples](../../examples/basic-usage.md)

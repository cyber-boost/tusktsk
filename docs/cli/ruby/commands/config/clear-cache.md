# tsk config clear-cache

Clear configuration cache

## Synopsis

```bash
tsk config clear-cache [options]
```

## Description

Clear configuration cache for the TuskLang Ruby SDK.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |

## Arguments

No arguments required.

## Examples

### Basic Usage
```bash
# clear configuration cache
tsk config clear-cache
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
def clear_cache
  puts "Executing clear-cache..."
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

- [tsk config](./README.md) - Config commands overview

## Notes

- This is a placeholder for the clear-cache command
- Replace with actual implementation details
- Add Ruby-specific examples and integration patterns

## See Also

- [Config Commands Overview](./README.md)
- [Examples](../../examples/basic-usage.md)

# tsk cache warm

Warm up caches

## Synopsis

```bash
tsk cache warm [options]
```

## Description

Warm up caches for the TuskLang Ruby SDK.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |

## Arguments

No arguments required.

## Examples

### Basic Usage
```bash
# warm up caches
tsk cache warm
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
def warm
  puts "Executing warm..."
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

- [tsk cache](./README.md) - Cache commands overview

## Notes

- This is a placeholder for the warm command
- Replace with actual implementation details
- Add Ruby-specific examples and integration patterns

## See Also

- [Cache Commands Overview](./README.md)
- [Examples](../../examples/basic-usage.md)

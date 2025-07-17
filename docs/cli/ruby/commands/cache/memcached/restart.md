# tsk cache memcached restart

Restart Memcached service

## Synopsis

```bash
tsk cache memcached restart [options]
```

## Description

Restart memcached service for the TuskLang Ruby SDK.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |

## Arguments

No arguments required.

## Examples

### Basic Usage
```bash
# restart memcached service
tsk cache memcached restart
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

- [tsk cache memcached](./README.md) - Cache memcached commands overview

## Notes

- This is a placeholder for the restart command
- Replace with actual implementation details
- Add Ruby-specific examples and integration patterns

## See Also

- [Cache memcached Commands Overview](./README.md)
- [Examples](../../examples/basic-usage.md)

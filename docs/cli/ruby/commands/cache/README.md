# Cache Commands

Cache operations for TuskLang Ruby SDK.

## Available Commands

- [tsk cache clear](./clear.md) - Clear all caches
- [tsk cache status](./status.md) - Show cache status
- [tsk cache warm](./warm.md) - Warm up caches
- [tsk cache memcached](./memcached) - {"status.md"=>"Check Memcached status", "stats.md"=>"Show Memcached statistics", "flush.md"=>"Flush Memcached cache", "restart.md"=>"Restart Memcached service", "test.md"=>"Test Memcached connection"}

## Common Use Cases

- **Development**: Common development scenarios
- **Production**: Production deployment patterns
- **Debugging**: Troubleshooting and debugging
- **Integration**: Framework and tool integration

## Ruby Specific Notes

- Follows Ruby conventions and best practices
- Integrates with Rails, Jekyll, and other Ruby frameworks
- Provides Ruby API examples for programmatic usage
- Supports Ruby-specific configuration patterns

## Examples

```bash
# Basic usage examples
tsk cache --help
```

## Integration with Rails

```ruby
# Rails integration example
class Application < Rails::Application
  # TuskLang cache integration
end
```

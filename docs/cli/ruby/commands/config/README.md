# Config Commands

Configuration management for TuskLang Ruby SDK.

## Available Commands

- [tsk config get](./get.md) - Get configuration value by path
- [tsk config check](./check.md) - Check configuration hierarchy
- [tsk config validate](./validate.md) - Validate entire configuration chain
- [tsk config compile](./compile.md) - Auto-compile all peanu.tsk files
- [tsk config docs](./docs.md) - Generate configuration documentation
- [tsk config clear-cache](./clear-cache.md) - Clear configuration cache
- [tsk config stats](./stats.md) - Show configuration performance statistics

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
tsk config --help
```

## Integration with Rails

```ruby
# Rails integration example
class Application < Rails::Application
  # TuskLang config integration
end
```

# Db Commands

Database operations for TuskLang Ruby SDK.

## Available Commands

- [tsk db status](./status.md) - Check database connection status
- [tsk db migrate](./migrate.md) - Run migration file
- [tsk db console](./console.md) - Open interactive database console
- [tsk db backup](./backup.md) - Backup database
- [tsk db restore](./restore.md) - Restore from backup file
- [tsk db init](./init.md) - Initialize SQLite database

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
tsk db --help
```

## Integration with Rails

```ruby
# Rails integration example
class Application < Rails::Application
  # TuskLang db integration
end
```

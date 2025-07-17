#!/usr/bin/env ruby

# Generate complete CLI documentation for TuskLang Ruby SDK
# This script creates all missing command documentation files

require 'fileutils'

# Command structure from CLI_DOCS_STRUCTURE_PROMPT.md
COMMANDS = {
  'db' => {
    'README.md' => 'Database operations for TuskLang Ruby SDK.',
    'status.md' => 'Check database connection status',
    'migrate.md' => 'Run migration file',
    'console.md' => 'Open interactive database console',
    'backup.md' => 'Backup database',
    'restore.md' => 'Restore from backup file',
    'init.md' => 'Initialize SQLite database'
  },
  'dev' => {
    'README.md' => 'Development tools for TuskLang Ruby SDK.',
    'serve.md' => 'Start development server',
    'compile.md' => 'Compile TuskLang files',
    'optimize.md' => 'Optimize code and configuration'
  },
  'test' => {
    'README.md' => 'Test execution for TuskLang Ruby SDK.',
    'all.md' => 'Run all tests',
    'parser.md' => 'Test TuskLang parser',
    'fujsen.md' => 'Test FUJSEN functions',
    'sdk.md' => 'Test SDK functionality',
    'performance.md' => 'Run performance tests'
  },
  'services' => {
    'README.md' => 'Service management for TuskLang Ruby SDK.',
    'start.md' => 'Start TuskLang services',
    'stop.md' => 'Stop TuskLang services',
    'restart.md' => 'Restart TuskLang services',
    'status.md' => 'Check service status'
  },
  'cache' => {
    'README.md' => 'Cache operations for TuskLang Ruby SDK.',
    'clear.md' => 'Clear all caches',
    'status.md' => 'Show cache status',
    'warm.md' => 'Warm up caches',
    'memcached' => {
      'status.md' => 'Check Memcached status',
      'stats.md' => 'Show Memcached statistics',
      'flush.md' => 'Flush Memcached cache',
      'restart.md' => 'Restart Memcached service',
      'test.md' => 'Test Memcached connection'
    }
  },
  'config' => {
    'README.md' => 'Configuration management for TuskLang Ruby SDK.',
    'get.md' => 'Get configuration value by path',
    'check.md' => 'Check configuration hierarchy',
    'validate.md' => 'Validate entire configuration chain',
    'compile.md' => 'Auto-compile all peanu.tsk files',
    'docs.md' => 'Generate configuration documentation',
    'clear-cache.md' => 'Clear configuration cache',
    'stats.md' => 'Show configuration performance statistics'
  },
  'binary' => {
    'README.md' => 'Binary compilation for TuskLang Ruby SDK.',
    'compile.md' => 'Compile to binary format',
    'execute.md' => 'Execute binary file',
    'benchmark.md' => 'Benchmark binary performance',
    'optimize.md' => 'Optimize binary size and speed'
  },
  'peanuts' => {
    'README.md' => 'Peanut configuration for TuskLang Ruby SDK.',
    'compile.md' => 'Compile Peanut configuration',
    'auto-compile.md' => 'Auto-compile Peanut files',
    'load.md' => 'Load Peanut configuration'
  },
  'ai' => {
    'README.md' => 'AI integrations for TuskLang Ruby SDK.',
    'claude.md' => 'Interact with Claude AI',
    'chatgpt.md' => 'Interact with ChatGPT',
    'custom.md' => 'Use custom AI provider',
    'config.md' => 'Configure AI settings',
    'setup.md' => 'Setup AI API keys',
    'test.md' => 'Test AI connections',
    'complete.md' => 'Complete code with AI',
    'analyze.md' => 'Analyze code with AI',
    'optimize.md' => 'Optimize code with AI',
    'security.md' => 'Security analysis with AI'
  },
  'css' => {
    'README.md' => 'CSS utilities for TuskLang Ruby SDK.',
    'expand.md' => 'Expand CSS shorthand',
    'map.md' => 'Generate CSS source maps'
  },
  'license' => {
    'README.md' => 'License management for TuskLang Ruby SDK.',
    'check.md' => 'Check license status',
    'activate.md' => 'Activate license'
  },
  'utility' => {
    'README.md' => 'General utilities for TuskLang Ruby SDK.',
    'parse.md' => 'Parse TuskLang files',
    'validate.md' => 'Validate TuskLang syntax',
    'convert.md' => 'Convert between formats',
    'get.md' => 'Get value from configuration',
    'set.md' => 'Set value in configuration',
    'version.md' => 'Show version information',
    'help.md' => 'Show help information'
  }
}

def create_command_template(category, command, description)
  command_name = command.sub('.md', '')
  full_command = "tsk #{category} #{command_name}"
  
  <<~TEMPLATE
# #{full_command}

#{description}

## Synopsis

```bash
#{full_command} [options]
```

## Description

#{description.capitalize} for the TuskLang Ruby SDK.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |

## Arguments

No arguments required.

## Examples

### Basic Usage
```bash
# #{description.downcase}
#{full_command}
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
def #{command_name.gsub('-', '_')}
  puts "Executing #{command_name}..."
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

- [tsk #{category}](./README.md) - #{category.capitalize} commands overview

## Notes

- This is a placeholder for the #{command_name} command
- Replace with actual implementation details
- Add Ruby-specific examples and integration patterns

## See Also

- [#{category.capitalize} Commands Overview](./README.md)
- [Examples](../../examples/basic-usage.md)
TEMPLATE
end

def create_category_readme(category, description)
  commands = COMMANDS[category].keys.reject { |k| k == 'README.md' }
  
  command_links = commands.map do |cmd|
    cmd_name = cmd.sub('.md', '')
    "- [tsk #{category} #{cmd_name}](./#{cmd}) - #{COMMANDS[category][cmd]}"
  end.join("\n")
  
  <<~TEMPLATE
# #{category.capitalize} Commands

#{description}

## Available Commands

#{command_links}

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
tsk #{category} --help
```

## Integration with Rails

```ruby
# Rails integration example
class Application < Rails::Application
  # TuskLang #{category} integration
end
```
TEMPLATE
end

def create_file(path, content)
  FileUtils.mkdir_p(File.dirname(path))
  File.write(path, content)
  puts "Created: #{path}"
end

# Generate all command files
COMMANDS.each do |category, commands|
  commands.each do |command, description|
    if command == 'README.md'
      # Create category README
      path = "cli-docs/ruby/commands/#{category}/README.md"
      content = create_category_readme(category, description)
      create_file(path, content)
    elsif description.is_a?(Hash)
      # Handle nested commands (like cache/memcached)
      description.each do |subcommand, subdescription|
        path = "cli-docs/ruby/commands/#{category}/#{command}/#{subcommand}"
        content = create_command_template("#{category} #{command}", subcommand, subdescription)
        create_file(path, content)
      end
    else
      # Create individual command file
      path = "cli-docs/ruby/commands/#{category}/#{command}"
      content = create_command_template(category, command, description)
      create_file(path, content)
    end
  end
end

# Create additional documentation files
additional_files = {
  'cli-docs/ruby/examples/README.md' => <<~CONTENT,
# Examples

Complete working examples for TuskLang Ruby SDK.

## Available Examples

- [Basic Usage](./basic-usage.md) - Common usage patterns
- [Workflows](./workflows.md) - Complete workflows
- [Integrations](./integrations.md) - Framework integrations

## Getting Started

Start with the basic usage examples and work your way up to complex integrations.
CONTENT

  'cli-docs/ruby/examples/basic-usage.md' => <<~CONTENT,
# Basic Usage Examples

Common usage patterns for TuskLang Ruby SDK.

## Configuration Management

```bash
# Get configuration value
tsk config get server.port

# Check configuration
tsk config check

# Validate configuration
tsk config validate
```

## Database Operations

```bash
# Initialize database
tsk db init

# Check status
tsk db status

# Run migration
tsk db migrate schema.sql
```

## Development Workflow

```bash
# Start server
tsk serve 3000

# Run tests
tsk test all

# Compile configuration
tsk config compile
```
CONTENT

  'cli-docs/ruby/examples/workflows.md' => <<~CONTENT,
# Complete Workflows

End-to-end workflows for TuskLang Ruby SDK.

## Development Workflow

1. **Initialize Project**
   ```bash
   tsk db init
   tsk config check
   ```

2. **Development**
   ```bash
   tsk serve 3000
   tsk test all
   ```

3. **Deployment**
   ```bash
   tsk config compile
   tsk db backup
   ```

## Testing Workflow

1. **Run Tests**
   ```bash
   tsk test all
   tsk test parser
   tsk test performance
   ```

2. **Debug Issues**
   ```bash
   tsk db console
   tsk config validate
   ```
CONTENT

  'cli-docs/ruby/examples/integrations.md' => <<~CONTENT,
# Framework Integrations

Integration examples for popular Ruby frameworks.

## Rails Integration

```ruby
# config/application.rb
class Application < Rails::Application
  # Load TuskLang configuration
  config_loader = PeanutConfig.new
  app_config = config_loader.load(Rails.root.join('config'))
  
  # Apply configuration
  config.app_name = app_config['app']['name']
  config.debug_mode = app_config['app']['debug']
end
```

## Jekyll Integration

```ruby
# _plugins/tusk_lang.rb
Jekyll::Hooks.register :site, :after_init do |site|
  config = TuskLang::TSK.from_file('_config.tsk')
  site.config.merge!(config.to_hash)
end
```

## Sinatra Integration

```ruby
# app.rb
require 'sinatra'
require 'tusk_lang'

class MyApp < Sinatra::Base
  configure do
    config = TuskLang::TSK.from_file('config.tsk')
    set :port, config.get_value('server', 'port')
  end
end
```
CONTENT

  'cli-docs/ruby/troubleshooting.md' => <<~CONTENT,
# Troubleshooting

Common issues and solutions for TuskLang Ruby SDK.

## Common Issues

### Command Not Found
```bash
# Install the gem
gem install tusk_lang

# Verify installation
tsk --version
```

### Database Connection Failed
```bash
# Initialize database
tsk db init

# Check status
tsk db status
```

### Configuration Not Found
```bash
# Create configuration
echo '[app]' > peanu.peanuts
echo 'name: "My App"' >> peanu.peanuts

# Check configuration
tsk config check
```

### Permission Denied
```bash
# Fix permissions
sudo gem install tusk_lang
```

## Getting Help

```bash
# Show help
tsk --help

# Command-specific help
tsk help [command]

# Version information
tsk --version
```
CONTENT
}

additional_files.each do |path, content|
  create_file(path, content)
end

puts "\n✅ CLI documentation generation complete!"
puts "Generated #{COMMANDS.values.flatten.count} command files"
puts "Generated #{additional_files.count} additional documentation files" 
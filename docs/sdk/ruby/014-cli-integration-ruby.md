# 🖥️ TuskLang Ruby CLI Integration Guide

**"We don't bow to any king" - Ruby Edition**

Harness the power of TuskLang from the command line. Build custom CLI tools, automate tasks, and script your Ruby workflows with TuskLang configs.

## 🚀 CLI Basics

### 1. TuskLang CLI Tool
```bash
# Parse a TSK file
tusk parse config/app.tsk

# Validate a TSK file
tusk validate config/app.tsk

# Convert TSK to JSON
tusk convert config/app.tsk --format json

# Interactive shell
tusk shell config/app.tsk
```

### 2. Ruby CLI Scripting
```ruby
# bin/tusk_cli.rb
#!/usr/bin/env ruby
require 'tusklang'
require 'optparse'

options = {}
OptionParser.new do |opts|
  opts.banner = "Usage: tusk_cli.rb [options]"
  opts.on('-f', '--file FILE', 'TSK file to parse') { |v| options[:file] = v }
  opts.on('-v', '--validate', 'Validate TSK file') { options[:validate] = true }
  opts.on('-j', '--json', 'Output as JSON') { options[:json] = true }
end.parse!

parser = TuskLang.new
if options[:validate]
  puts parser.validate_file(options[:file]) ? "✅ Valid!" : "❌ Invalid!"
else
  data = parser.parse_file(options[:file])
  puts options[:json] ? JSON.pretty_generate(data) : data.inspect
end
```

## 🛠️ Custom CLI Commands

### 1. Define CLI Commands in Config
```ruby
# config/cli.tsk
cli {
  command "import_users" {
    script: "scripts/import_users.rb"
    schedule: "0 2 * * *"
    notify: true
  }
  command "cleanup" {
    script: "scripts/cleanup.rb"
    schedule: "@daily"
    notify: false
  }
}
```

### 2. Automate with Rake
```ruby
# lib/tasks/tusk.rake
namespace :tusk do
  desc "Import users from external source"
  task :import_users do
    system('ruby scripts/import_users.rb')
  end

  desc "Cleanup old data"
  task :cleanup do
    system('ruby scripts/cleanup.rb')
  end
end
```

## 🛡️ Best Practices
- Use OptionParser for flexible CLI arguments.
- Validate configs before running automation scripts.
- Use environment variables for secrets in CLI scripts.
- Log CLI actions for auditability.

**Ready to automate everything? Let's Tusk! 🚀** 
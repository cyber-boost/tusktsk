# 🏛️ TuskLang Ruby Legacy Migration Guide

**"We don't bow to any king" - Ruby Edition**

Migrate your legacy configs (YAML, JSON, ENV, INI) to TuskLang for a modern, unified Ruby configuration experience.

## 🔄 Supported Legacy Formats
- YAML (.yml, .yaml)
- JSON (.json)
- ENV (.env, environment variables)
- INI (.ini)

## 🚀 Migration Steps

### 1. Convert YAML to TuskLang
```bash
tusk convert legacy/config.yml --format tusk > config/app.tsk
```

### 2. Convert JSON to TuskLang
```bash
tusk convert legacy/config.json --format tusk > config/app.tsk
```

### 3. Convert INI to TuskLang
```bash
tusk convert legacy/config.ini --format tusk > config/app.tsk
```

### 4. Import ENV Variables
```ruby
# config/app.tsk
$api_key: @env("API_KEY")
$database_url: @env("DATABASE_URL")
```

## 🛠️ Ruby Conversion Tools

### 1. tusk convert CLI
```bash
tusk convert legacy/config.yml --format tusk > config/app.tsk
```

### 2. Ruby Script Example
```ruby
# scripts/convert_yaml_to_tusk.rb
require 'yaml'
require 'tusklang'

yaml_data = YAML.load_file('legacy/config.yml')
tusk_content = TuskLang::Converter.yaml_to_tusk(yaml_data)
File.write('config/app.tsk', tusk_content)
```

## 🛡️ Best Practices
- Validate converted configs before deploying.
- Use @env for secrets and dynamic values.
- Refactor repetitive patterns into global variables or snippets.
- Document migration steps for your team.

**Ready to leave legacy behind? Let's Tusk! 🚀** 
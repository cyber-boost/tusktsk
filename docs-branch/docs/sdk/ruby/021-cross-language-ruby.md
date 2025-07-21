# 🌍 TuskLang Ruby Cross-Language Compatibility Guide

**"We don't bow to any king" - Ruby Edition**

Share configs across Ruby, Python, JavaScript, Go, PHP, and more. TuskLang makes cross-language configuration seamless and powerful.

## 🔗 Sharing Configs Across Languages

### 1. Universal TSK Files
```ruby
# config/app.tsk
$app_name: "MyApp"
$version: "1.0.0"
[database]
host: "localhost"
port: 5432
```

- Use the same .tsk file in Ruby, Python, JS, Go, PHP, etc.
- All SDKs parse the same syntax and structure.

### 2. Language-Specific Overrides
```ruby
# config/app.tsk
$language: @env("LANGUAGE", "ruby")

@if($language == "python") {
  @include("config/python_overrides.tsk")
}
@if($language == "js") {
  @include("config/js_overrides.tsk")
}
```

### 3. Ruby Usage
```ruby
# app/services/cross_language_service.rb
require 'tusklang'

class CrossLanguageService
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('config/app.tsk')
  end
end

config = CrossLanguageService.load_config
puts "App Name: #{config['app_name']}"
```

### 4. Python Usage
```python
from tusklang import TuskLang
parser = TuskLang()
config = parser.parse_file('config/app.tsk')
print(config['app_name'])
```

### 5. JavaScript Usage
```js
const tusklang = require('tusklang');
const config = tusklang.parseFile('config/app.tsk');
console.log(config.app_name);
```

## 🛡️ Best Practices
- Use universal .tsk files for shared settings.
- Use language-specific includes for overrides.
- Validate configs in all environments.
- Document cross-language config structure for your team.

**Ready to unify your stack? Let's Tusk! 🚀** 
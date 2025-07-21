# ❓ TuskLang Ruby FAQ

**"We don't bow to any king" - Ruby Edition**

Answers to the most common questions about TuskLang in Ruby environments.

## ❓ Frequently Asked Questions

### 1. How do I install TuskLang for Ruby?
- Use `gem install tusklang` or add to your Gemfile.

### 2. How do I parse a TSK file?
```ruby
require 'tusklang'
parser = TuskLang.new
config = parser.parse_file('config/app.tsk')
```

### 3. How do I validate a config?
```ruby
parser.validate_file('config/app.tsk')
```

### 4. How do I use environment variables?
```ruby
$api_key: @env("API_KEY")
```

### 5. How do I connect to a database?
```ruby
adapter = TuskLang::Adapters::PostgreSQLAdapter.new(host: 'localhost', port: 5432, database: 'myapp', user: 'postgres', password: 'secret')
```

### 6. How do I cache values?
```ruby
cache = TuskLang::Cache::MemoryCache.new(max_size: 1000, ttl: 60)
cache.set('key', 'value')
```

### 7. How do I handle errors?
- Rescue `TuskLang::ParseError` and `TuskLang::SchemaError` in Ruby code.

### 8. How do I migrate from YAML/JSON/ENV?
- Use `tusk convert legacy/config.yml --format tusk > config/app.tsk`

### 9. How do I use TuskLang with Rails?
- Load config in `config/application.rb` and use in controllers/models/jobs.

### 10. Where can I find more examples?
- See `/docs/ruby/` and `/web/tusk-me-hard/ruby/` for full guides.

## 🛡️ Best Practices
- Always validate configs before deploying.
- Use @env.secure for secrets.
- Organize configs by domain and environment.
- Document config structure for your team.

**Still have questions? Let's Tusk! 🚀** 
# 📚 TuskLang Ruby API Reference

**"We don't bow to any king" - Ruby Edition**

A complete reference for the TuskLang Ruby SDK: classes, methods, config options, and usage examples.

## 🏛️ Core Classes

### 1. TuskLang
```ruby
parser = TuskLang.new
config = parser.parse_file('config/app.tsk')
```
- `parse_file(path)` — Parse a TSK file and return a config hash.
- `parse(content)` — Parse TSK content from a string.
- `validate_file(path)` — Validate a TSK file (returns true/false).
- `schema_file = path` — Set a schema for validation.

### 2. TuskLang::Configurable
```ruby
class MyConfig
  include TuskLang::Configurable
end
```
- Enables mapping TSK config to Ruby objects.

### 3. TuskLang::Adapters
- `SQLiteAdapter.new(filename)`
- `PostgreSQLAdapter.new(host:, port:, database:, user:, password:)`
- `MySQLAdapter.new(host:, port:, database:, user:, password:)`
- `MongoDBAdapter.new(uri:, database:)`
- `RedisAdapter.new(host:, port:, db:)`

### 4. TuskLang::Cache
- `MemoryCache.new(max_size: 1000, ttl: 60)`
- `RedisCache.new(host:, port:, db:)`
- `MemcachedCache.new(servers: [])`

## 🛠️ Methods & Usage

### 1. Parsing Configs
```ruby
parser = TuskLang.new
config = parser.parse_file('config/app.tsk')
```

### 2. Validating Configs
```ruby
parser = TuskLang.new
parser.schema_file = 'config/schema.tsk'
parser.validate_file('config/app.tsk')
```

### 3. Using Adapters
```ruby
sqlite = TuskLang::Adapters::SQLiteAdapter.new('app.db')
postgres = TuskLang::Adapters::PostgreSQLAdapter.new(host: 'localhost', port: 5432, database: 'myapp', user: 'postgres', password: 'secret')
```

### 4. Caching
```ruby
cache = TuskLang::Cache::MemoryCache.new(max_size: 1000, ttl: 60)
cache.set('key', 'value')
cache.get('key')
```

## 🛡️ Best Practices
- Always validate configs before use.
- Use adapters for database and cache integration.
- Map configs to Ruby objects for type safety.

**Ready to master the API? Let's Tusk! 🚀** 
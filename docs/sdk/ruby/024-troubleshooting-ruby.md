# 🛠️ TuskLang Ruby Troubleshooting Guide

**"We don't bow to any king" - Ruby Edition**

Solve common problems fast. This guide covers installation, config, database, cache, and integration issues for TuskLang in Ruby environments.

## 🚦 Installation Issues

### 1. Gem Installation Fails
- Run `gem cleanup` and `gem install tusklang --force`.
- Check Ruby version (`ruby --version` should be 2.7+).
- Install build tools: `sudo apt-get install build-essential` (Ubuntu/Debian).

### 2. Bundler Issues
- Run `bundle install` and check for gem conflicts.
- Update Bundler: `gem install bundler`.

## 📝 Config Errors

### 1. Syntax Errors
- Validate with `tusk validate config/app.tsk`.
- Check for missing quotes, brackets, or commas.

### 2. Schema Validation
- Set `parser.schema_file = 'config/schema.tsk'` and validate.

## 🗄️ Database Issues

### 1. Connection Errors
- Check adapter config (host, port, user, password).
- Test connection with `adapter.test_connection`.

### 2. Query Errors
- Use parameterized queries for safety.
- Check SQL syntax and table names.

## ⚡ Cache Issues

### 1. Redis/Memcached Errors
- Check host/port and credentials.
- Test with `cache.set('key', 'value')` and `cache.get('key')`.

## 🔗 Integration Issues

### 1. Rails Integration
- Ensure config is loaded in `config/application.rb`.
- Reload configs on deploy or restart.

### 2. API/HTTP Issues
- Check endpoint URLs and credentials.
- Handle HTTP errors in Ruby code.

## 🛡️ Best Practices
- Always validate configs before deploying.
- Use @env.secure for secrets.
- Log all errors and monitor for failures.

**Still stuck? Let's Tusk! 🚀** 
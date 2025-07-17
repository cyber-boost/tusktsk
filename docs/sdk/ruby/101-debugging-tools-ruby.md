# 🐞 Debugging Tools in TuskLang Ruby SDK

**"We don't bow to any king" – Debugging is your superpower.**

TuskLang for Ruby brings a new era of configuration debugging: dynamic, introspective, and deeply integrated with your Ruby and Rails workflows. Whether you're building a Rails app, a Jekyll site, or a DevOps pipeline, TuskLang's debugging tools help you catch issues early, understand complex config logic, and optimize your system with confidence.

---

## 🚦 Why Debugging Matters in TuskLang
- **Dynamic configs**: TuskLang configs can include logic, operators, and even embedded functions (FUJSEN)
- **Cross-file complexity**: Debugging means tracing values across multiple files and environments
- **Production safety**: Catch errors before they hit your users

---

## 🛠️ Core Debugging Features

### 1. Inline Debug Output
Use the `@debug` operator to print values during config evaluation:

```ini
[settings]
debug_mode: true
api_key: "supersecret"
current_env: @env("RACK_ENV", "development")

[debug]
print_env: @debug($current_env)
```

**Ruby Usage:**
```ruby
require 'tusk_lang'
config = TuskLang::TSK.from_file('config.tsk')
# Prints the current_env value to STDOUT during parsing
env = config.get_value('settings', 'current_env')
```

### 2. FUJSEN Function Debugging
Embed debug statements in your JavaScript (FUJSEN) functions:

```ini
[math]
double_fujsen: """
function double(x) {
  console.log('Doubling:', x);
  return x * 2;
}
"""
```

**Ruby Usage:**
```ruby
double = config.execute_fujsen('math', 'double', 21)
# Output: Doubling: 21
```

### 3. Operator Tracing
Trace the evaluation of complex @ operator chains:

```ini
[trace]
complex_value: @debug(@math(@date("U") * 2 + 7))
```

**Ruby Usage:**
```ruby
val = config.get_value('trace', 'complex_value')
# Prints intermediate and final values
```

### 4. Error Reporting
TuskLang Ruby SDK raises detailed exceptions for parse and runtime errors:

```ruby
begin
  config = TuskLang::TSK.from_string("""
    [broken]
    value: @math(1 / 0)
  """)
rescue TuskLang::Error => e
  puts "TuskLang Error: #{e.message}"
end
```

---

## 🚂 Rails & Jekyll Integration

### Rails: Debugging in Development
- Use `Rails.logger.debug` to capture TuskLang debug output
- Set `debug_mode: true` in your TSK config for verbose output

```ruby
if Rails.env.development?
  config = TuskLang::TSK.from_file('config/app.tsk', debug: true)
end
```

### Jekyll: Debugging Static Site Builds
- Print debug info during site generation
- Use `@debug` in `_config.tsk` to trace build variables

---

## 🧩 Advanced Debugging Patterns

### 1. Conditional Debugging
Only print debug info in certain environments:

```ini
[debug]
print_if_dev: @if(@env("RACK_ENV") == "development", @debug("Dev mode!"), "")
```

### 2. Cross-File Debugging
Trace values imported from other TSK files:

```ini
[import]
settings: @import("../shared/settings.tsk")
print_import: @debug($settings)
```

### 3. FUJSEN Exception Handling
Catch and log errors in embedded JS:

```ini
[fujsen]
robust_fujsen: """
function robust(x) {
  try {
    return riskyOperation(x);
  } catch (e) {
    console.log('FUJSEN error:', e);
    return null;
  }
}
"""
```

---

## 🚨 Troubleshooting
- **No debug output?** Ensure `debug_mode` is enabled and STDOUT is not redirected
- **Silent FUJSEN errors?** Use `console.log` in your JS and check Ruby logs
- **Operator chain fails?** Use `@debug` at each step to isolate the problem

---

## ⚡ Performance & Security Notes
- **Performance**: Remove or disable debug output in production for best speed
- **Security**: Never print secrets (API keys, passwords) in debug output
- **Best Practice**: Use environment checks to control debug verbosity

---

## 🏆 Best Practices
- Use `@debug` liberally in development, never in production
- Wrap all FUJSEN logic with try/catch and log errors
- Integrate with Rails/Jekyll logging for unified output
- Document all debug patterns in your team wiki

---

**Master debugging in TuskLang Ruby and make your configurations bulletproof. 🐞** 
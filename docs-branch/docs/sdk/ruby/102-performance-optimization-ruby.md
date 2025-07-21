# ⚡ Performance Optimization in TuskLang Ruby SDK

**"We don't bow to any king" – Speed is your birthright.**

TuskLang for Ruby empowers you to build lightning-fast, dynamic configurations that scale with your application. Whether you're optimizing Rails, Jekyll, or DevOps pipelines, these tools and patterns will help you squeeze every drop of performance from your TuskLang-powered systems.

---

## 🚀 Why Performance Matters in TuskLang
- **Dynamic logic**: Configs can include logic, operators, and embedded functions (FUJSEN)
- **Real-time evaluation**: Performance impacts app startup, request handling, and build times
- **Scalability**: Efficient configs scale better in distributed systems

---

## 🏎️ Core Performance Features

### 1. Caching with @cache Operator
Reduce redundant computation by caching expensive results:

```ini
[api]
cached_data: @cache("5m", @http("GET", "https://api.example.com/data"))
```

**Ruby Usage:**
```ruby
require 'tusk_lang'
config = TuskLang::TSK.from_file('config.tsk')
data = config.get_value('api', 'cached_data')
```

### 2. Lazy Evaluation
Only compute values when needed:

```ini
[settings]
lazy_value: @if($debug_mode, @expensive_op(), "")
```

### 3. FUJSEN Function Optimization
Write efficient JS for embedded logic:

```ini
[math]
fast_sum_fujsen: """
function fast_sum(arr) {
  let sum = 0;
  for (let i = 0; i < arr.length; i++) sum += arr[i];
  return sum;
}
"""
```

**Ruby Usage:**
```ruby
sum = config.execute_fujsen('math', 'fast_sum', [1,2,3,4,5])
```

### 4. Operator Short-Circuiting
Use logical operators to avoid unnecessary work:

```ini
[logic]
result: @if(@env("SKIP_HEAVY") == "true", "skipped", @heavy_op())
```

---

## 🚂 Rails & Jekyll Integration

### Rails: Boot-Time Optimization
- Preload and cache TSK configs at boot
- Use Rails.cache for cross-request caching

```ruby
Rails.application.config.to_prepare do
  $tsk_config ||= TuskLang::TSK.from_file('config/app.tsk')
end
```

### Jekyll: Build-Time Optimization
- Cache expensive config computations during site build
- Use @cache for API calls and data generation

---

## 🧩 Advanced Patterns

### 1. Batched Operations
Batch expensive operations in FUJSEN or Ruby:

```ini
[batch]
batch_fujsen: """
function batchProcess(items) {
  return items.map(item => process(item));
}
"""
```

### 2. Parallel Processing
Use Ruby threads or background jobs for heavy config tasks:

```ruby
threads = [1,2,3].map do |i|
  Thread.new { config.execute_fujsen('batch', 'batchProcess', data[i]) }
end
results = threads.map(&:value)
```

### 3. Profiling and Benchmarking
Profile config evaluation with Ruby's Benchmark module:

```ruby
require 'benchmark'
time = Benchmark.realtime do
  config.get_value('api', 'cached_data')
end
puts "Config fetch took #{time} seconds"
```

---

## 🚨 Troubleshooting
- **Slow config loads?** Profile with Benchmark, use @cache, and optimize FUJSEN
- **Memory bloat?** Avoid large in-memory configs, use lazy evaluation
- **API bottlenecks?** Cache all remote calls

---

## ⚡ Performance & Security Notes
- **Performance**: Use @cache and lazy evaluation for all expensive operations
- **Security**: Never cache secrets or sensitive data
- **Best Practice**: Profile regularly and optimize FUJSEN logic

---

## 🏆 Best Practices
- Cache aggressively, but invalidate wisely
- Profile both Ruby and FUJSEN logic
- Use environment checks to skip heavy work in dev/test
- Document all performance patterns for your team

---

**Master performance in TuskLang Ruby and build systems that fly. ⚡** 
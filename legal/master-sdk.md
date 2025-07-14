# 🐘 TuskLang Master SDK Documentation

**"We don't bow to any king" - The Freedom Configuration Language**

A comprehensive guide to all TuskLang SDK implementations across 9 programming languages and environments.

## 📋 Table of Contents

1. [Overview](#overview)
2. [Core Features](#core-features)
3. [SDK Implementations](#sdk-implementations)
4. [Language-Specific Features](#language-specific-features)
5. [Advanced Capabilities](#advanced-capabilities)
6. [Use Cases & Examples](#use-cases--examples)
7. [Performance & Benchmarks](#performance--benchmarks)
8. [Integration Patterns](#integration-patterns)

## 🌟 Overview

TuskLang is a modern configuration language designed for maximum flexibility and developer freedom. Each SDK provides:

- **Multiple Syntax Styles**: `[]`, `{}`, `<>` grouping - your choice!
- **Global Variables**: `$variables` accessible across all sections
- **Cross-File Communication**: `@file.tsk.get()` and `@file.tsk.set()`
- **Database Queries**: Query databases directly in config files
- **@ Operators**: Intelligence operators for dynamic content
- **peanut.tsk Integration**: Universal configuration file support
- **FUJSEN Support**: Function serialization and execution

## 🎯 Core Features

### Universal Syntax Support
All SDKs support these TuskLang syntax variations:

```tsk
# Traditional sections
[database]
host: "localhost"
port: 5432

# Curly brace objects
server {
    host: "0.0.0.0"
    port: 8080
}

# Angle bracket objects
cache >
    driver: "redis"
    ttl: "5m"
<
```

### Global Variables & Interpolation
```tsk
$app_name: "My Application"
$environment: @env("APP_ENV", "development")

server {
    workers: $environment == "production" ? 4 : 1
    log_level: $environment == "production" ? "error" : "debug"
}
```

### @ Operators System
```tsk
# Environment variables
api_key: @env("API_KEY")

# Date functions
timestamp: @date("Y-m-d H:i:s")

# Database queries
user_count: @query("SELECT COUNT(*) FROM users")

# Caching
cache_data: @cache("5m", "expensive_operation")

# Cross-file references
shared_config: @config.tsk.get("shared_setting")
```

## 🚀 SDK Implementations

### 1. 🐹 Go SDK (`/go`)

**Key Features:**
- **Type-safe struct mapping** with reflection
- **CLI tool** for parsing, validation, and codegen
- **Enhanced parser** with maximum syntax flexibility
- **peanut.tsk integration** with automatic loading
- **Database adapters** for SQLite, PostgreSQL, MySQL, MongoDB, Redis

**Strengths:**
- Production-ready with comprehensive error handling
- Excellent performance for high-throughput applications
- Strong type safety with Go struct mapping
- Docker/Kubernetes ready

**Example Usage:**
```go
type Config struct {
    AppName string `tsk:"app_name"`
    Version string `tsk:"version"`
    Port    int    `tsk:"port"`
    Debug   bool   `tsk:"debug"`
}

parser := tusklanggo.NewEnhancedParser()
data, _ := parser.ParseFile("config.tsk")
var config Config
tusklanggo.UnmarshalTSK(data, &config)
```

### 2. ☕ Java SDK (`/java`)

**Key Features:**
- **Simple, clean API** with minimal dependencies
- **Maven integration** for easy project inclusion
- **CLI tool** with pretty JSON output
- **Friendly error messages** and validation
- **Jackson integration** for JSON processing

**Strengths:**
- Enterprise-ready with familiar Java patterns
- Excellent for Spring Boot applications
- Strong validation and error handling
- Minimal learning curve

**Example Usage:**
```java
TuskLangParser parser = new TuskLangParser();
Map<String, Object> config = parser.parseFile("config.tsk");
String appName = (String) config.get("app_name");
boolean debug = (Boolean) config.get("debug");
```

### 3. 🟨 JavaScript/Node.js SDK (`/js`)

**Key Features:**
- **5 Database adapters** (SQLite, PostgreSQL, MySQL, MongoDB, Redis)
- **Enhanced parser** with maximum flexibility
- **TypeScript support** with full definitions
- **peanut.tsk integration** with automatic loading
- **Cross-file communication** capabilities

**Strengths:**
- Most feature-complete implementation
- Excellent for Node.js applications
- Rich ecosystem integration
- Database query support in config files

**Example Usage:**
```javascript
const TuskLang = require('tusklang');
const SQLiteAdapter = require('tusklang/adapters/sqlite');

const db = new SQLiteAdapter({ filename: './app.db' });
const parser = new TuskLang.Enhanced();
parser.setDatabaseAdapter(db);

const config = parser.parseFile('app.tsk');
// Config can contain: user_limit: @query("SELECT max_users FROM plans")
```

### 4. 🐘 PHP SDK (`/php`)

**Key Features:**
- **Composer integration** for easy installation
- **CLI tool** with multiple commands
- **peanut.tsk integration** with standard locations
- **Database integration** via PDO
- **System integration** with installed TuskLang tools

**Strengths:**
- Perfect for PHP applications and frameworks
- Easy integration with existing PHP codebases
- Strong CLI tooling
- Environment variable support

**Example Usage:**
```php
use TuskLang\Enhanced\TuskLangEnhanced;

$parser = new TuskLangEnhanced();
$config = $parser->parseFile('config.tsk');
$dbHost = $parser->get('database.host');

// Or use helper functions
$config = tsk_parse_file('config.tsk');
$parser = tsk_load_from_peanut();
```

### 5. 🐍 Python SDK (`/python`)

**Key Features:**
- **FUJSEN support** for function serialization
- **Smart contract capabilities** with executable code
- **Database adapters** for SQLite, PostgreSQL, MongoDB
- **JavaScript compatibility** for function conversion
- **Shell storage** for binary data handling

**Strengths:**
- Most advanced FUJSEN implementation
- Perfect for blockchain and smart contracts
- Excellent for data science and automation
- Rich ecosystem integration

**Example Usage:**
```python
from tsk import TSK

tsk = TSK.from_string("""
[contract]
process_fujsen = '''
def process(amount, recipient):
    if amount <= 0:
        raise ValueError("Invalid amount")
    return {
        'success': True,
        'amount': amount,
        'recipient': recipient
    }
'''
""")

result = tsk.execute_fujsen('contract', 'process', 100, 'alice@example.com')
```

### 6. 💎 Ruby SDK (`/ruby`)

**Key Features:**
- **Rails integration** for application configuration
- **Jekyll support** for static site generation
- **DevOps automation** with scriptable configuration
- **FUJSEN support** with JavaScript compatibility
- **@ Operator system** for dynamic content

**Strengths:**
- Perfect for Rails applications
- Excellent for static site generation
- Strong DevOps automation capabilities
- Ruby-native performance

**Example Usage:**
```ruby
require 'tusk_lang'

tsk = TuskLang::TSK.from_string(<<~TSK)
  [api]
  endpoint = "@request('https://api.example.com/data')"
  cache_ttl = "@cache('5m', 'api_data')"
TSK

context = { 'cache_value' => 'cached_data' }
endpoint = tsk.execute_operators("@request('https://api.example.com/data')", context)
```

### 7. 🦀 Rust SDK (`/rust`)

**Key Features:**
- **Ultra-fast parsing** with zero-copy operations
- **WebAssembly support** for browser environments
- **Type-safe configuration** with Serde integration
- **Comprehensive CLI** with multiple commands
- **Performance benchmarking** built-in

**Strengths:**
- Fastest parsing performance
- WebAssembly support for browsers
- Excellent for performance-critical applications
- Strong type safety

**Example Usage:**
```rust
use tusklang_rust::{parse, parse_into, Config};

#[derive(Deserialize)]
struct AppConfig {
    app_name: String,
    version: String,
    debug: bool,
    port: u16,
}

let config: AppConfig = parse_into(r#"
app_name: "My App"
version: "1.0.0"
debug: true
port: 8080
"#)?;
```

### 8. 🔷 C# SDK (`/csharp`)

**Key Features:**
- **Unity integration** for game development
- **Azure integration** for cloud applications
- **FUJSEN support** with JavaScript compatibility
- **Type safety** with automatic type detection
- **Cross-platform** .NET Standard compatibility

**Strengths:**
- Perfect for Unity game development
- Excellent for Azure cloud applications
- Strong .NET ecosystem integration
- Type-safe configuration

**Example Usage:**
```csharp
using TuskLang;

var tsk = TSK.FromString(@"
[game]
damage_calc_fujsen = """
function calculateDamage(attack, defense, weapon) {
    var baseDamage = attack * weapon.power;
    var reduction = defense * 0.1;
    return Math.max(1, baseDamage - reduction);
}
"""
");

var damage = tsk.ExecuteFujsen("game", "damage_calc", playerAttack, enemyDefense, weapon);
```

### 9. 🐚 Bash SDK (`/bash`)

**Key Features:**
- **Pure Bash implementation** with no dependencies
- **FUJSEN support** with native Bash functions
- **Command-line interface** for scripting
- **Universal compatibility** across Unix systems
- **Lightweight** with no runtime overhead

**Strengths:**
- Universal availability (Bash is everywhere)
- Perfect for automation and CI/CD
- No dependencies required
- Excellent for system administration

**Example Usage:**
```bash
#!/bin/bash
source ./tsk.sh

# Parse TSK file
tsk_parse config.tsk

# Get values
id=$(tsk_get storage id)
echo "Storage ID: $id"

# Execute fujsen
result=$(./tsk.sh fujsen contract.tsk payment process 100 "alice@example.com")
```

## 🔧 Language-Specific Features

### Database Integration

| SDK | SQLite | PostgreSQL | MySQL | MongoDB | Redis |
|-----|--------|------------|-------|---------|-------|
| Go | ✅ | ✅ | ✅ | ✅ | ✅ |
| Java | ❌ | ❌ | ❌ | ❌ | ❌ |
| JavaScript | ✅ | ✅ | ✅ | ✅ | ✅ |
| PHP | ✅ | ✅ | ✅ | ❌ | ❌ |
| Python | ✅ | ✅ | ❌ | ✅ | ❌ |
| Ruby | ❌ | ❌ | ❌ | ❌ | ❌ |
| Rust | ❌ | ❌ | ❌ | ❌ | ❌ |
| C# | ❌ | ❌ | ❌ | ❌ | ❌ |
| Bash | ❌ | ❌ | ❌ | ❌ | ❌ |

### FUJSEN Support

| SDK | JavaScript Functions | Python Functions | Bash Functions | Smart Contracts |
|-----|---------------------|------------------|----------------|-----------------|
| Go | ❌ | ❌ | ❌ | ❌ |
| Java | ❌ | ❌ | ❌ | ❌ |
| JavaScript | ✅ | ❌ | ❌ | ✅ |
| PHP | ❌ | ❌ | ❌ | ❌ |
| Python | ✅ | ✅ | ❌ | ✅ |
| Ruby | ✅ | ❌ | ❌ | ✅ |
| Rust | ❌ | ❌ | ❌ | ❌ |
| C# | ✅ | ❌ | ❌ | ✅ |
| Bash | ✅ | ❌ | ✅ | ✅ |

### CLI Tools

| SDK | Parse | Validate | Generate | Convert | Benchmark |
|-----|-------|----------|----------|---------|-----------|
| Go | ✅ | ✅ | ✅ | ❌ | ❌ |
| Java | ✅ | ✅ | ❌ | ❌ | ❌ |
| JavaScript | ❌ | ❌ | ❌ | ❌ | ❌ |
| PHP | ✅ | ✅ | ❌ | ❌ | ❌ |
| Python | ❌ | ❌ | ❌ | ❌ | ❌ |
| Ruby | ❌ | ❌ | ❌ | ❌ | ❌ |
| Rust | ✅ | ✅ | ✅ | ✅ | ✅ |
| C# | ❌ | ❌ | ❌ | ❌ | ❌ |
| Bash | ✅ | ❌ | ❌ | ❌ | ❌ |

## 🚀 Advanced Capabilities

### 1. Cross-File Communication
```tsk
# In main.tsk
shared_setting: @config.tsk.get("shared_value")
cache_data: @cache.tsk.set("key", "value")
```

### 2. Database Queries in Config
```tsk
# When database adapter is configured
user_limit: @query("SELECT max_users FROM plans WHERE active = 1")
active_sessions: @query("SELECT COUNT(*) FROM sessions WHERE expires > NOW()")
```

### 3. Conditional Expressions
```tsk
debug_mode: $environment == "development" ? true : false
max_connections: $environment == "production" ? 100 : 10
log_level: $environment == "production" ? "error" : "debug"
```

### 4. Range Syntax
```tsk
port_range: 8000-9000
allowed_ips: ["127.0.0.1", "192.168.1.0/24"]
```

### 5. String Concatenation
```tsk
full_name: $app_name + " v" + $version
log_file: "/var/log/" + $app_name + ".log"
```

## 🎯 Use Cases & Examples

### 1. Application Configuration
```tsk
# Universal app configuration
$app_name: "My Application"
$environment: @env("APP_ENV", "development")

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "myapp")

[server]
host: "0.0.0.0"
port: @env("PORT", 8080)
workers: $environment == "production" ? 4 : 1

[logging]
level: $environment == "production" ? "error" : "debug"
format: "json"
```

### 2. Smart Contracts
```tsk
[contract]
name: "PaymentProcessor"
version: "1.0.0"

process_fujsen: """
function process(amount, recipient) {
    if (amount <= 0) throw new Error("Invalid amount");
    
    return {
        success: true,
        transactionId: 'tx_' + Date.now(),
        amount: amount,
        recipient: recipient,
        fee: amount * 0.01
    };
}
"""

validate_fujsen: """
(amount) => amount > 0 && amount <= 1000000
"""
```

### 3. DevOps Automation
```tsk
[deploy]
environment: @env("DEPLOY_ENV", "staging")
region: @env("AWS_REGION", "us-west-2")

deploy_fujsen: """
function deploy() {
    return {
        steps: [
            { name: 'Checkout', uses: 'actions/checkout@v2' },
            { name: 'Setup Node.js', uses: 'actions/setup-node@v2' },
            { name: 'Install dependencies', run: 'npm install' },
            { name: 'Run tests', run: 'npm test' },
            { name: 'Deploy', run: 'npm run deploy' }
        ]
    };
}
"""
```

### 4. Game Development (Unity/C#)
```tsk
[player]
speed: 5.0
jump_force: 10.0
max_health: 100

[combat]
damage_calc_fujsen: """
function calculateDamage(attack, defense, weapon) {
    var baseDamage = attack * weapon.power;
    var reduction = defense * 0.1;
    return Math.max(1, baseDamage - reduction);
}
"""
```

## 📊 Performance & Benchmarks

### Parsing Performance (Rust SDK)
```
Running benchmark with 10000 iterations...
Results:
  Total time: 12.5ms
  Average time per parse: 1.25 μs
  Parses per second: 800,000
```

### Memory Usage Comparison
| SDK | Memory Usage | Startup Time | Runtime Overhead |
|-----|--------------|--------------|------------------|
| Rust | Very Low | Fast | Minimal |
| Go | Low | Fast | Low |
| C# | Low | Fast | Low |
| Java | Medium | Medium | Medium |
| Python | Medium | Medium | Medium |
| JavaScript | Medium | Fast | Medium |
| PHP | Medium | Fast | Medium |
| Ruby | Medium | Medium | Medium |
| Bash | Very Low | Fast | Minimal |

## 🔗 Integration Patterns

### 1. Framework Integration

**Rails (Ruby):**
```ruby
# config/application.rb
config_tsk = TuskLang::TSK.from_file(Rails.root.join('config', 'app.tsk'))
config.app_name = config_tsk.get_value("app", "name")
```

**Spring Boot (Java):**
```java
@Configuration
public class AppConfig {
    @Bean
    public Map<String, Object> tuskConfig() {
        TuskLangParser parser = new TuskLangParser();
        return parser.parseFile("config.tsk");
    }
}
```

**Express.js (JavaScript):**
```javascript
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('config.tsk');

app.listen(config.server.port, config.server.host);
```

### 2. Cloud Integration

**Azure Functions (C#):**
```csharp
public class ApiFunction
{
    [FunctionName("ProcessData")]
    public async Task<IActionResult> Run([HttpTrigger] HttpRequest req)
    {
        var config = TSK.FromFile("azure-config.tsk");
        var result = config.ExecuteFujsen("processing", "transform", req.Body);
        return new OkObjectResult(result);
    }
}
```

**AWS Lambda (JavaScript):**
```javascript
const TuskLang = require('tusklang');
const config = TuskLang.parseFile('lambda-config.tsk');

exports.handler = async (event) => {
    const result = config.executeFujsen('processing', 'transform', event);
    return { statusCode: 200, body: JSON.stringify(result) };
};
```

### 3. Container Integration

**Docker (Go):**
```dockerfile
FROM golang:1.21-alpine
COPY . /app
WORKDIR /app
RUN go build -o tusk-go main.go
CMD ["./tusk-go", "parse", "config.tsk"]
```

**Kubernetes (Rust):**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
spec:
  template:
    spec:
      containers:
      - name: app
        image: tusklang/app:latest
        command: ["tusk-rust", "parse", "/config/app.tsk"]
```

## 🌟 Why TuskLang?

### 1. **Maximum Flexibility**
- Multiple syntax styles to choose from
- No "one true way" - use what works for you
- "We don't bow to any king" philosophy

### 2. **Intelligent Configuration**
- Database queries in config files
- Dynamic content with @ operators
- Cross-file communication

### 3. **Executable Configuration**
- FUJSEN for function serialization
- Smart contracts and programmable configs
- Runtime execution capabilities

### 4. **Universal Compatibility**
- 9 language implementations
- Cross-platform support
- Framework integration

### 5. **Performance Optimized**
- Rust implementation for speed
- WebAssembly support
- Minimal overhead

## 🚀 Getting Started

1. **Choose your SDK** based on your language and requirements
2. **Install the SDK** using the appropriate package manager
3. **Create your first .tsk file** with basic configuration
4. **Explore advanced features** like @ operators and FUJSEN
5. **Integrate with your framework** or application
6. **Deploy and scale** with confidence

## 📚 Resources

- **Official Documentation**: [docs.tusklang.org](https://docs.tusklang.org)
- **GitHub Repository**: [github.com/tuskphp/tusklang](https://github.com/tuskphp/tusklang)
- **Community**: [community.tusklang.org](https://community.tusklang.org)
- **Examples**: [examples.tusklang.org](https://examples.tusklang.org)

---

**"We don't bow to any king"** - TuskLang gives developers the freedom to choose their preferred syntax while maintaining intelligent configuration capabilities across all major programming languages and platforms. 
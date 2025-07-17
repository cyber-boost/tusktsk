# TuskLang PNT Documentation Task - [LANGUAGE_NAME]

## Task Overview

Create comprehensive Peanut Binary Configuration (`.pnt`) usage documentation for the **[LANGUAGE_NAME]** TuskLang SDK. This documentation should be specific to [LANGUAGE_NAME] while following the universal template structure.

## Your Mission

Transform the generic PNT_USAGE_TEMPLATE.md into a language-specific guide by:
1. Replacing all placeholders with [LANGUAGE_NAME]-specific content
2. Adding language-specific import/usage examples
3. Including idiomatic code patterns
4. Highlighting language-specific optimizations

## File to Create

`/opt/tsk_git/sdk-pnt-test/[LANGUAGE_DIR]/docs/PNT_USAGE.md`

Where [LANGUAGE_DIR] is one of: js, go, java, python, ruby, rust, csharp, php, bash

## Required Replacements

### 1. Import Statements

Replace `[LANGUAGE_SPECIFIC_IMPORT]` with actual import code:

**Python:**
```python
from tusklang import PeanutConfig
# or
import tusklang.peanut_config as peanut
```

**JavaScript:**
```javascript
const { PeanutConfig } = require('tusklang');
// or ES6
import { PeanutConfig } from 'tusklang';
```

**Go:**
```go
import (
    "github.com/tusklang/go-sdk/peanut"
)
```

**Java:**
```java
import org.tusklang.PeanutConfig;
import org.tusklang.ConfigFile;
```

**Ruby:**
```ruby
require 'tusklang/peanut_config'
# or
require 'tusklang'
```

**Rust:**
```rust
use tusklang::peanut::PeanutConfig;
```

**C#:**
```csharp
using TuskLang;
using TuskLang.Configuration;
```

**PHP:**
```php
use TuskLang\PeanutConfig;
// or
require_once 'vendor/autoload.php';
```

### 2. Code Examples

Replace `[LANGUAGE_CODE]` with language-specific implementations:

**Loading Configuration - Python:**
```python
# Synchronous loading
config = PeanutConfig.load()

# From specific directory
config = PeanutConfig.load('/path/to/project')

# With options
config = PeanutConfig.load(
    directory='.',
    auto_compile=True,
    watch=True
)

# Get specific value
port = PeanutConfig.get('server.port', default=8080)
```

**Loading Configuration - JavaScript:**
```javascript
// Async loading
const config = await PeanutConfig.load();

// With callbacks
PeanutConfig.load('./', (err, config) => {
    if (err) throw err;
    console.log(config);
});

// Get specific value
const port = config.get('server.port', 8080);
```

**Loading Configuration - Go:**
```go
// Create instance
pc := peanut.New(true, true)

// Load configuration
config, err := pc.Load("./")
if err != nil {
    log.Fatal(err)
}

// Get value
port := pc.Get("server.port", 8080, "./")
```

### 3. Type-Safe Access Examples

Add language-specific type-safe access patterns:

**TypeScript:**
```typescript
interface ServerConfig {
    host: string;
    port: number;
    ssl: boolean;
}

const serverConfig = config.get<ServerConfig>('server');
```

**Java:**
```java
// Using generics
Integer port = config.get("server.port", Integer.class, 8080, "./");

// Custom config class
@ConfigurationProperties("server")
public class ServerConfig {
    private String host;
    private int port;
    // getters/setters
}
```

**C#:**
```csharp
// Using generics
var port = config.Get<int>("server.port", 8080);

// Using dynamics
dynamic serverConfig = config.Get("server");
string host = serverConfig.host;
```

### 4. Error Handling

Language-specific error handling patterns:

**Go:**
```go
config, err := pc.Load("./")
if err != nil {
    switch err {
    case peanut.ErrFileNotFound:
        // Create default config
    case peanut.ErrChecksumMismatch:
        // Regenerate binary
    default:
        return err
    }
}
```

**Rust:**
```rust
match PeanutConfig::load("./") {
    Ok(config) => {
        // Use config
    },
    Err(PeanutError::FileNotFound) => {
        // Handle missing file
    },
    Err(e) => {
        eprintln!("Error: {}", e);
    }
}
```

**Java:**
```java
try {
    Map<String, Object> config = peanutConfig.load("./");
} catch (FileNotFoundException e) {
    // Handle missing file
} catch (ChecksumMismatchException e) {
    // Handle corrupted file
} catch (IOException e) {
    // Handle other IO errors
}
```

### 5. Async/Await Examples

For languages that support async:

**JavaScript/TypeScript:**
```javascript
// Async/await pattern
async function loadConfig() {
    try {
        const config = await PeanutConfig.load();
        return config;
    } catch (error) {
        console.error('Failed to load config:', error);
    }
}

// Promise pattern
PeanutConfig.load()
    .then(config => {
        // Use config
    })
    .catch(error => {
        // Handle error
    });
```

**Python (asyncio):**
```python
import asyncio

async def load_config():
    config = await PeanutConfig.load_async()
    return config

# Run
config = asyncio.run(load_config())
```

**C# (async/await):**
```csharp
public async Task<Dictionary<string, object>> LoadConfigAsync()
{
    var config = await PeanutConfig.LoadAsync("./");
    return config;
}
```

### 6. Performance Optimization Tips

Language-specific performance tips:

**Go:**
- Use sync.Once for singleton pattern
- Leverage goroutines for file watching
- Use sync.Map for concurrent access

**Rust:**
- Use Arc<RwLock<Config>> for thread-safe access
- Leverage zero-copy deserialization
- Use lazy_static for global config

**Java:**
- Use ConcurrentHashMap for thread safety
- Implement caching with Caffeine or Guava
- Use CompletableFuture for async operations

### 7. Integration Examples

Show integration with popular frameworks:

**Python - Django:**
```python
# In settings.py
from tusklang import PeanutConfig

peanut = PeanutConfig.load(os.path.dirname(__file__))
DEBUG = peanut.get('django.debug', False)
DATABASES = peanut.get('django.databases', {})
```

**JavaScript - Express:**
```javascript
const express = require('express');
const { PeanutConfig } = require('tusklang');

const app = express();
const config = PeanutConfig.load();

app.listen(config.get('server.port', 3000));
```

**Ruby - Rails:**
```ruby
# In config/application.rb
require 'tusklang/peanut_config'

module MyApp
  class Application < Rails::Application
    config.peanut = PeanutConfig.load(Rails.root)
  end
end
```

## Language-Specific Sections

Add a section at the end for language-specific features:

### For Compiled Languages (Go, Rust, C++):
- Binary distribution considerations
- Static vs dynamic linking
- Cross-compilation notes

### For Interpreted Languages (Python, Ruby, JavaScript):
- Module distribution
- Dependency management
- Performance considerations

### For JVM Languages (Java, Kotlin, Scala):
- Classpath configuration
- JAR packaging
- JVM tuning for performance

## Quality Checklist

- [ ] All code examples compile/run without errors
- [ ] Import statements are correct for the language
- [ ] Error handling follows language conventions
- [ ] Performance tips are actionable
- [ ] Integration examples use popular frameworks
- [ ] Type safety is demonstrated where applicable
- [ ] Async patterns are shown where relevant

## Important Notes

1. **CRITICAL**: Ensure all examples use `.pnt` extension (not `.pntb`)
2. **Test all code examples** in the actual SDK
3. **Follow language naming conventions** consistently
4. **Include package manager** installation where applicable
5. **Show both simple and advanced usage**

Remember: This documentation will help developers integrate Peanut configuration into their [LANGUAGE_NAME] projects!
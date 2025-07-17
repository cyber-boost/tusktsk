# TuskLang C# SDK

The official TuskLang Configuration SDK for C# with full support for parsing, generating, and executing TSK files with FUJSEN (function serialization). Built for .NET applications, Unity game engine, and Azure cloud integration.

## 🚀 Features

- **Parse TSK Files**: Read and parse TOML-like TSK format
- **Generate TSK**: Create TSK files programmatically
- **FUJSEN Support**: Store and execute JavaScript functions within TSK files
- **@ Operator System**: Full FUJSEN intelligence operators
- **Type Safety**: Automatic type detection and preservation
- **Cross-Platform**: .NET Standard 2.0/2.1 compatible
- **Unity Ready**: Game engine integration
- **Azure Integration**: Cloud-native applications
- **Smart Contracts**: Perfect for blockchain and distributed applications

## 📦 Installation

### .NET Applications
```bash
# Add to your project
dotnet add package TuskLang.CSharp

# Or include the source files directly
```

### Unity Integration
1. Copy the C# SDK files to your Unity project's `Scripts` folder
2. Ensure .NET Standard 2.0 compatibility
3. Import the namespace: `using TuskLang;`

### Azure Functions
```csharp
// Add to your Azure Functions project
using TuskLang;
```

## 🎯 Quick Start

### Basic Usage
```csharp
using TuskLang;

// Parse TSK file
var tsk = TSK.FromString(@"
[app]
name = ""My Application""
version = ""1.0.0""
debug = true

[config]
port = 8080
host = ""localhost""
");

// Get values
var appName = tsk.GetValue("app", "name");
var port = tsk.GetValue("config", "port");
Console.WriteLine($"App: {appName}, Port: {port}");
```

### FUJSEN Function Execution
```csharp
// TSK with FUJSEN function
var tsk = TSK.FromString(@"
[calculator]
add_fujsen = """
function add(a, b) {
    return a + b;
}
"""

multiply_fujsen = """
function multiply(a, b) {
    return a * b;
}
"""
");

// Execute functions
var sum = tsk.ExecuteFujsen("calculator", "add", 5, 3);
var product = tsk.ExecuteFujsen("calculator", "multiply", 4, 7);
Console.WriteLine($"Sum: {sum}, Product: {product}");
```

### @ Operator System
```csharp
// TSK with @ operators
var tsk = TSK.FromString(@"
[api]
endpoint = ""@request('https://api.example.com/data')""
cache_ttl = ""@cache('5m', 'api_data')""
timestamp = ""@date('yyyy-MM-dd HH:mm:ss')""
user_count = ""@Query('users').equalTo('status', 'active').count()""
");

// Execute operators
var context = new Dictionary<string, object>
{
    ["cache_value"] = "cached_data",
    ["user_status"] = "active"
};

var endpoint = await tsk.ExecuteOperators("@request('https://api.example.com/data')", context);
var timestamp = await tsk.ExecuteOperators("@date('yyyy-MM-dd HH:mm:ss')", context);
```

## 🎮 Unity Integration

### Game Configuration
```csharp
using UnityEngine;
using TuskLang;

public class GameConfig : MonoBehaviour
{
    private TSK config;
    
    void Start()
    {
        // Load game configuration
        config = TSK.FromFile("game-config.tsk");
        
        // Apply settings
        var playerSpeed = (double)config.GetValue("player", "speed");
        var gravity = (double)config.GetValue("physics", "gravity");
        var musicVolume = (double)config.GetValue("audio", "music_volume");
        
        // Apply to game
        PlayerController.speed = (float)playerSpeed;
        Physics.gravity = new Vector3(0, (float)gravity, 0);
        AudioManager.SetMusicVolume((float)musicVolume);
    }
}
```

### Dynamic Game Logic
```csharp
// game-logic.tsk
var logic = TSK.FromString(@"
[combat]
damage_calc_fujsen = """
function calculateDamage(attack, defense, weapon) {
    var baseDamage = attack * weapon.power;
    var reduction = defense * 0.1;
    return Math.max(1, baseDamage - reduction);
}
"""

[ai]
behavior_fujsen = """
function decideAction(player, enemy) {
    if (player.health < 30) {
        return 'retreat';
    } else if (enemy.health < 20) {
        return 'attack';
    } else {
        return 'defend';
    }
}
"""
");

// Use in game
var damage = logic.ExecuteFujsen("combat", "damage_calc", playerAttack, enemyDefense, weapon);
var aiAction = logic.ExecuteFujsen("ai", "behavior", player, enemy);
```

## ☁️ Azure Integration

### Azure Functions Configuration
```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using TuskLang;

public class ApiFunction
{
    [FunctionName("ProcessData")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        ILogger log)
    {
        // Load configuration
        var config = TSK.FromFile("azure-config.tsk");
        
        // Get Azure-specific settings
        var connectionString = config.GetValue("azure", "storage_connection");
        var containerName = config.GetValue("azure", "blob_container");
        var maxRetries = (int)config.GetValue("azure", "max_retries");
        
        // Process with FUJSEN
        var result = config.ExecuteFujsen("processing", "transform", req.Body);
        
        return new OkObjectResult(result);
    }
}
```

### Azure Configuration File
```tsk
[azure]
storage_connection = "@env('AZURE_STORAGE_CONNECTION')"
blob_container = "data-container"
max_retries = 3
timeout = 30

[processing]
transform_fujsen = """
function transform(data) {
    return {
        processed: true,
        timestamp: new Date().toISOString(),
        data: data.map(item => ({
            id: item.id,
            value: item.value * 2,
            status: 'processed'
        }))
    };
}
"""

[monitoring]
metrics_fujsen = """
function trackMetrics(operation, duration) {
    return {
        operation: operation,
        duration: duration,
        timestamp: Date.now(),
        success: duration < 5000
    };
}
"""
```

## 🔥 FUJSEN Examples

### Payment Processing Contract
```csharp
var contract = TSK.FromString(@"
[contract]
name = ""PaymentProcessor""
version = ""1.0.0""

process_fujsen = """
function process(amount, recipient) {
    if (amount <= 0) throw new Error(""Invalid amount"");
    
    return {
        success: true,
        transactionId: 'tx_' + Date.now(),
        amount: amount,
        recipient: recipient,
        fee: amount * 0.01
    };
}
"""

validate_fujsen = """
(amount) => amount > 0 && amount <= 1000000
"""
");

// Execute payment
var payment = contract.ExecuteFujsen("contract", "process", 100.50, "alice@example.com");
var isValid = contract.ExecuteFujsen("contract", "validate", 500);
```

### DeFi Liquidity Pool
```csharp
var pool = TSK.FromString(@"
[pool]
token_a = ""FLEX""
token_b = ""USDT""
reserve_a = 100000
reserve_b = 50000

swap_fujsen = """
function swap(amountIn, tokenIn) {
    const k = 100000 * 50000;
    const fee = amountIn * 0.003;
    const amountInWithFee = amountIn - fee;
    
    if (tokenIn === 'FLEX') {
        const amountOut = (amountInWithFee * 50000) / (100000 + amountInWithFee);
        return { 
            amountOut: amountOut,
            fee: fee,
            priceImpact: (amountIn / 100000) * 100
        };
    } else {
        const amountOut = (amountInWithFee * 100000) / (50000 + amountInWithFee);
        return { 
            amountOut: amountOut,
            fee: fee,
            priceImpact: (amountIn / 50000) * 100
        };
    }
}
"""
");

// Execute swap
var swapResult = pool.ExecuteFujsen("pool", "swap", 1000, "FLEX");
```

## 🛠️ Advanced Features

### Shell Storage (Binary Format)
```csharp
// Store data in binary format
var data = "Hello, TuskLang!";
var shellData = ShellStorage.CreateShellData(data, "greeting");
var binary = ShellStorage.Pack(shellData);

// Retrieve data
var retrieved = ShellStorage.Unpack(binary);
Console.WriteLine(retrieved.Data); // "Hello, TuskLang!"

// Detect type
var type = ShellStorage.DetectType(binary);
Console.WriteLine($"Type: {type}"); // "text"
```

### Context Injection
```csharp
var tsk = TSK.FromString(@"
[processor]
transform_fujsen = """
function transform(data) {
    return data.map(item => ({
        ...item,
        processed: true,
        processor: context.processor_name,
        timestamp: new Date().toISOString()
    }));
}
"""
");

var context = new Dictionary<string, object>
{
    ["processor_name"] = "Azure Function v2.1",
    ["environment"] = "production"
};

var result = tsk.ExecuteFujsenWithContext("processor", "transform", context, data);
```

### @ Operator Examples
```csharp
// Database queries
var query = await tsk.ExecuteOperators("@Query('users').equalTo('status', 'active').limit(10)");

// Caching
var cached = await tsk.ExecuteOperators("@cache('5m', 'user_data', userData)");

// Metrics
var metrics = tsk.ExecuteOperators("@metrics('api_response_time', 150)");

// Conditional logic
var result = tsk.ExecuteOperators("@if(user.isPremium, 'premium', 'standard')");

// Date formatting
var timestamp = tsk.ExecuteOperators("@date('yyyy-MM-dd HH:mm:ss')");

// Environment variables
var apiKey = tsk.ExecuteOperators("@env('API_KEY')");

// FlexChain operations
var balance = await tsk.ExecuteOperators("@flex('balance', '0x123...')");
var transfer = await tsk.ExecuteOperators("@flex('transfer', 100, '0x456...', '0x789...')");
```

## 📚 TSK Format

TSK is a TOML-like format with enhanced features:

- **Sections**: `[section_name]`
- **Key-Value**: `key = value`
- **Types**: strings, numbers, booleans, arrays, objects, null
- **Multiline**: Triple quotes `"""`
- **Comments**: Lines starting with `#`
- **FUJSEN**: Function serialization in multiline strings
- **@ Operators**: Intelligence operators for dynamic content

## 🎯 Use Cases

1. **Configuration Files**: Human-readable app configuration
2. **Game Development**: Unity game settings and logic
3. **Cloud Applications**: Azure Functions and App Service configs
4. **Smart Contracts**: Store executable code with metadata
5. **Data Exchange**: Type-safe data serialization
6. **API Definitions**: Function signatures and implementations
7. **Workflow Automation**: Scriptable configuration with logic

## 🌟 Why TuskLang C#?

- **Human-Readable**: Unlike JSON, designed for humans first
- **Executable**: FUJSEN makes configs programmable
- **Type-Safe**: No ambiguity in data types
- **Unity-Ready**: Perfect for game development
- **Azure-Native**: Cloud integration built-in
- **Blockchain-Ready**: Perfect for smart contracts
- **Simple**: Minimal syntax, maximum power

## 🔧 Requirements

- **.NET Standard 2.0** or higher
- **Unity 2019.4** or higher (for Unity integration)
- **Azure Functions Runtime 3.0** or higher (for Azure integration)

## 📄 License

Part of the Flexchain project - Blockchain with digital grace.

## 🚀 Getting Started

1. Include the C# SDK files in your project
2. Add `using TuskLang;` to your files
3. Start parsing and executing TSK files!
4. Explore FUJSEN functions and @ operators
5. Integrate with Unity or Azure as needed

The C# SDK provides the same powerful features as the JavaScript/TypeScript and Python SDKs, with native C# performance and .NET ecosystem integration. 
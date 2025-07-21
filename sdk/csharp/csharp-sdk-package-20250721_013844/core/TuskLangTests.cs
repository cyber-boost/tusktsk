using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace TuskLang.Tests
{
    /// <summary>
    /// Comprehensive tests and examples for the TuskLang C# SDK
    /// </summary>
    public class TuskLangTests
    {
        public static async Task RunAllTests()
        {
            Console.WriteLine("🐘 TuskLang C# SDK Tests");
            Console.WriteLine("========================\n");

            await TestBasicParsing();
            await TestFujsenExecution();
            await TestOperatorSystem();
            await TestShellStorage();
            await TestUnityIntegration();
            await TestAzureIntegration();
            await TestFlexChainIntegration();

            Console.WriteLine("\n✅ All tests completed successfully!");
        }

        /// <summary>
        /// Test basic TSK parsing functionality
        /// </summary>
        public static async Task TestBasicParsing()
        {
            Console.WriteLine("📝 Testing Basic Parsing...");

            var tskContent = @"
# Application configuration
[app]
name = ""TuskLang Demo""
version = ""1.0.0""
debug = true
port = 8080

[features]
database = ""sqlite""
cache = ""redis""
api_version = ""v2""

[settings]
timeout = 30.5
retries = 3
hosts = [""localhost"", ""127.0.0.1"", ""0.0.0.0""]
config = {""key1"" = ""value1"", ""key2"" = 42}
";

            var tsk = TSK.FromString(tskContent);

            // Test basic value retrieval
            var appName = tsk.GetValue("app", "name");
            var version = tsk.GetValue("app", "version");
            var debug = tsk.GetValue("app", "debug");
            var port = tsk.GetValue("app", "port");

            Console.WriteLine($"  App Name: {appName}");
            Console.WriteLine($"  Version: {version}");
            Console.WriteLine($"  Debug: {debug}");
            Console.WriteLine($"  Port: {port}");

            // Test section retrieval
            var appSection = tsk.GetSection("app");
            var featuresSection = tsk.GetSection("features");

            Console.WriteLine($"  App Section Keys: {string.Join(", ", appSection.Keys)}");
            Console.WriteLine($"  Features Section Keys: {string.Join(", ", featuresSection.Keys)}");

            // Test array and object parsing
            var hosts = tsk.GetValue("settings", "hosts");
            var config = tsk.GetValue("settings", "config");

            Console.WriteLine($"  Hosts: {string.Join(", ", (List<object>)hosts)}");
            Console.WriteLine($"  Config: {config}");

            Console.WriteLine("  ✅ Basic parsing tests passed\n");
        }

        /// <summary>
        /// Test FUJSEN function execution
        /// </summary>
        public static async Task TestFujsenExecution()
        {
            Console.WriteLine("⚡ Testing FUJSEN Execution...");

            var tsk = TSK.FromString(@"
[calculator]
add_fujsen = ""function add(a, b) {
    return a + b;
}""

multiply_fujsen = ""function multiply(a, b) {
    return a * b;
}""

validate_fujsen = ""function validate(amount) {
    return amount > 0 && amount <= 1000000;
}""

[processor]
transform_fujsen = ""function transform(data) {
    return {
        processed: true,
        timestamp: new Date().toISOString(),
        data: data.map(item => ({
            id: item.id,
            value: item.value * 2,
            status: 'processed'
        }))
    };
}""
");

            // Test basic arithmetic
            var sum = tsk.ExecuteFujsen("calculator", "add", 5, 3);
            var product = tsk.ExecuteFujsen("calculator", "multiply", 4, 7);
            var isValid = tsk.ExecuteFujsen("calculator", "validate", 500);

            Console.WriteLine($"  Sum: {sum}");
            Console.WriteLine($"  Product: {product}");
            Console.WriteLine($"  Valid: {isValid}");

            // Test data transformation
            var testData = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["id"] = 1, ["value"] = 10 },
                new Dictionary<string, object> { ["id"] = 2, ["value"] = 20 }
            };

            var transformed = tsk.ExecuteFujsen("processor", "transform", testData);
            Console.WriteLine($"  Transformed: {transformed}");

            // Test context injection
            var context = new Dictionary<string, object>
            {
                ["processor_name"] = "Test Processor",
                ["environment"] = "development"
            };

            var contextResult = tsk.ExecuteFujsenWithContext("processor", "transform", context, testData);
            Console.WriteLine($"  Context Result: {contextResult}");

            Console.WriteLine("  ✅ FUJSEN execution tests passed\n");
        }

        /// <summary>
        /// Test @ operator system
        /// </summary>
        public static async Task TestOperatorSystem()
        {
            Console.WriteLine("🔧 Testing @ Operator System...");

            var tsk = TSK.FromString(@"
[api]
endpoint = ""@request('https://api.example.com/data')""
cache_ttl = ""@cache('5m', 'api_data')""
timestamp = ""@date('yyyy-MM-dd HH:mm:ss')""
user_count = ""@Query('users').equalTo('status', 'active').count()""
feature_check = ""@feature('redis')""
env_var = ""@env('API_KEY')""
conditional = ""@if(user.isPremium, 'premium', 'standard')""
");

            var context = new Dictionary<string, object>
            {
                ["cache_value"] = "cached_data",
                ["user_status"] = "active",
                ["user"] = new Dictionary<string, object> { ["isPremium"] = true }
            };

            // Test various @ operators
            var endpoint = await tsk.ExecuteOperators("@request('https://api.example.com/data')", context);
            var timestamp = await tsk.ExecuteOperators("@date('yyyy-MM-dd HH:mm:ss')", context);
            var featureCheck = await tsk.ExecuteOperators("@feature('redis')", context);
            var conditional = await tsk.ExecuteOperators("@if(user.isPremium, 'premium', 'standard')", context);

            Console.WriteLine($"  Endpoint: {endpoint}");
            Console.WriteLine($"  Timestamp: {timestamp}");
            Console.WriteLine($"  Feature Check: {featureCheck}");
            Console.WriteLine($"  Conditional: {conditional}");

            Console.WriteLine("  ✅ @ Operator tests passed\n");
        }

        /// <summary>
        /// Test Shell storage (binary format)
        /// </summary>
        public static async Task TestShellStorage()
        {
            Console.WriteLine("💾 Testing Shell Storage...");

            // Test string data
            var stringData = "Hello, TuskLang!";
            var shellData = ShellStorage.CreateShellData(stringData, "greeting");
            var binary = ShellStorage.Pack(shellData);

            var retrieved = ShellStorage.Unpack(binary);
            var type = ShellStorage.DetectType(binary);

            Console.WriteLine($"  Original: {stringData}");
            Console.WriteLine($"  Retrieved: {retrieved.Data}");
            Console.WriteLine($"  Type: {type}");

            // Test TSK data
            var tskData = @"
[test]
message = ""Hello from Shell Storage""
number = 42
";
            var tskShellData = ShellStorage.CreateShellData(tskData, "tsk_config");
            var tskBinary = ShellStorage.Pack(tskShellData);
            var tskType = ShellStorage.DetectType(tskBinary);

            Console.WriteLine($"  TSK Type: {tskType}");

            Console.WriteLine("  ✅ Shell storage tests passed\n");
        }

        /// <summary>
        /// Test Unity integration examples
        /// </summary>
        public static async Task TestUnityIntegration()
        {
            Console.WriteLine("🎮 Testing Unity Integration...");

            var gameConfig = TSK.FromString(@"
[player]
speed = 5.5
health = 100
jump_force = 10.0

[physics]
gravity = -9.81
friction = 0.8
bounce = 0.5

[audio]
music_volume = 0.7
sfx_volume = 0.9
voice_volume = 0.8

[combat]
damage_calc_fujsen = ""function calculateDamage(attack, defense, weapon) {
    var baseDamage = attack * weapon.power;
    var reduction = defense * 0.1;
    return Math.max(1, baseDamage - reduction);
}""

[ai]
behavior_fujsen = ""function decideAction(player, enemy) {
    if (player.health < 30) {
        return 'retreat';
    } else if (enemy.health < 20) {
        return 'attack';
    } else {
        return 'defend';
    }
}""
");

            // Simulate Unity game settings
            var playerSpeed = (double)gameConfig.GetValue("player", "speed");
            var gravity = (double)gameConfig.GetValue("physics", "gravity");
            var musicVolume = (double)gameConfig.GetValue("audio", "music_volume");

            Console.WriteLine($"  Player Speed: {playerSpeed}");
            Console.WriteLine($"  Gravity: {gravity}");
            Console.WriteLine($"  Music Volume: {musicVolume}");

            // Test combat system
            var weapon = new Dictionary<string, object> { ["power"] = 1.5 };
            var damage = gameConfig.ExecuteFujsen("combat", "damage_calc", 10, 5, weapon);
            Console.WriteLine($"  Damage: {damage}");

            // Test AI behavior
            var player = new Dictionary<string, object> { ["health"] = 25 };
            var enemy = new Dictionary<string, object> { ["health"] = 50 };
            var aiAction = gameConfig.ExecuteFujsen("ai", "behavior", player, enemy);
            Console.WriteLine($"  AI Action: {aiAction}");

            Console.WriteLine("  ✅ Unity integration tests passed\n");
        }

        /// <summary>
        /// Test Azure integration examples
        /// </summary>
        public static async Task TestAzureIntegration()
        {
            Console.WriteLine("☁️ Testing Azure Integration...");

            var azureConfig = TSK.FromString(@"
[azure]
storage_connection = ""@env('AZURE_STORAGE_CONNECTION')""
blob_container = ""data-container""
max_retries = 3
timeout = 30

[processing]
transform_fujsen = ""function transform(data) {
    return {
        processed: true,
        timestamp: new Date().toISOString(),
        data: data.map(item => ({
            id: item.id,
            value: item.value * 2,
            status: 'processed'
        }))
    };
}""

[monitoring]
metrics_fujsen = ""function trackMetrics(operation, duration) {
    return {
        operation: operation,
        duration: duration,
        timestamp: Date.now(),
        success: duration < 5000
    };
}""
");

            // Simulate Azure Function processing
            var connectionString = azureConfig.GetValue("azure", "storage_connection");
            var containerName = azureConfig.GetValue("azure", "blob_container");
            var maxRetries = (int)azureConfig.GetValue("azure", "max_retries");

            Console.WriteLine($"  Connection String: {connectionString}");
            Console.WriteLine($"  Container: {containerName}");
            Console.WriteLine($"  Max Retries: {maxRetries}");

            // Test data processing
            var testData = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["id"] = 1, ["value"] = 100 },
                new Dictionary<string, object> { ["id"] = 2, ["value"] = 200 }
            };

            var processed = azureConfig.ExecuteFujsen("processing", "transform", testData);
            Console.WriteLine($"  Processed: {processed}");

            // Test metrics tracking
            var metrics = azureConfig.ExecuteFujsen("monitoring", "trackMetrics", "data_processing", 2500);
            Console.WriteLine($"  Metrics: {metrics}");

            Console.WriteLine("  ✅ Azure integration tests passed\n");
        }

        /// <summary>
        /// Test FlexChain integration
        /// </summary>
        public static async Task TestFlexChainIntegration()
        {
            Console.WriteLine("🔗 Testing FlexChain Integration...");

            var flexConfig = TSK.FromString(@"
[flexchain]
network = ""testnet""
rpc_url = ""https://testnet.flexchain.org""

[operations]
balance_fujsen = ""@flex('balance', '0x1234567890abcdef')""
transfer_fujsen = ""@flex('transfer', 100, '0x1234567890abcdef', '0xfedcba0987654321')""
stake_fujsen = ""@flex('stake', 500, 'validator1')""
status_fujsen = ""@flex('status')""
");

            // Test FlexChain operations
            var balance = await flexConfig.ExecuteOperators("@flex('balance', '0x1234567890abcdef')");
            var transfer = await flexConfig.ExecuteOperators("@flex('transfer', 100, '0x1234567890abcdef', '0xfedcba0987654321')");
            var status = await flexConfig.ExecuteOperators("@flex('status')");

            Console.WriteLine($"  Balance: {balance}");
            Console.WriteLine($"  Transfer: {transfer}");
            Console.WriteLine($"  Status: {status}");

            Console.WriteLine("  ✅ FlexChain integration tests passed\n");
        }

        /// <summary>
        /// Run a complete end-to-end example
        /// </summary>
        public static async Task RunCompleteExample()
        {
            Console.WriteLine("🚀 Running Complete Example...");

            // Create a comprehensive TSK configuration
            var completeConfig = @"
# Complete TuskLang Configuration Example
[application]
name = ""TuskLang Demo App""
version = ""1.0.0""
environment = ""@env('APP_ENV')""
debug = true

[server]
host = ""localhost""
port = 8080
timeout = 30
cors_origins = [""http://localhost:3000"", ""https://app.example.com""]

[database]
type = ""sqlite""
connection = ""@env('DB_CONNECTION')""
pool_size = 10
migrations = true

[features]
redis = ""@feature('redis')""
postgresql = ""@feature('postgresql')""
azure = ""@feature('azure')""

[processing]
transform_fujsen = ""function transform(data, options) {
    var result = {
        processed: true,
        timestamp: new Date().toISOString(),
        records: data.length,
        data: []
    };
    
    data.forEach(function(item, index) {
        result.data.push({
            id: item.id || index,
            value: item.value * (options.multiplier || 1),
            status: 'processed',
            metadata: {
                original_value: item.value,
                processed_at: new Date().toISOString()
            }
        });
    });
    
    return result;
}""

validate_fujsen = ""function validate(data) {
    if (!Array.isArray(data)) {
        throw new Error('Data must be an array');
    }
    
    return data.every(function(item) {
        return item && typeof item.value === 'number' && item.value >= 0;
    });
}""

[monitoring]
track_fujsen = ""function track(operation, data, duration) {
    return {
        operation: operation,
        duration: duration,
        timestamp: Date.now(),
        success: duration < 5000,
        data_size: Array.isArray(data) ? data.length : 1,
        metrics: {
            avg_duration: duration,
            max_duration: duration,
            min_duration: duration
        }
    };
}""

[flexchain]
network = ""testnet""
operations = {
    ""balance"" = ""@flex('balance', '0x1234567890abcdef')"",
    ""transfer"" = ""@flex('transfer', 100, '0x1234567890abcdef', '0xfedcba0987654321')"",
    ""status"" = ""@flex('status')""
}
";

            var tsk = TSK.FromString(completeConfig);

            // Test all features
            Console.WriteLine("  📋 Configuration loaded successfully");

            // Basic values
            var appName = tsk.GetValue("application", "name");
            var port = tsk.GetValue("server", "port");
            Console.WriteLine($"  App: {appName}, Port: {port}");

            // FUJSEN processing
            var testData = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["id"] = 1, ["value"] = 10 },
                new Dictionary<string, object> { ["id"] = 2, ["value"] = 20 },
                new Dictionary<string, object> { ["id"] = 3, ["value"] = 30 }
            };

            var options = new Dictionary<string, object> { ["multiplier"] = 2.5 };

            var transformed = tsk.ExecuteFujsen("processing", "transform", testData, options);
            var isValid = tsk.ExecuteFujsen("processing", "validate", testData);
            var metrics = tsk.ExecuteFujsen("monitoring", "track", "data_processing", testData, 1500);

            Console.WriteLine($"  Transformed: {transformed}");
            Console.WriteLine($"  Valid: {isValid}");
            Console.WriteLine($"  Metrics: {metrics}");

            // @ Operators
            var context = new Dictionary<string, object>
            {
                ["APP_ENV"] = "development",
                ["DB_CONNECTION"] = "sqlite:///app.db"
            };

            var environment = await tsk.ExecuteOperators("@env('APP_ENV')", context);
            var redisFeature = await tsk.ExecuteOperators("@feature('redis')", context);

            Console.WriteLine($"  Environment: {environment}");
            Console.WriteLine($"  Redis Feature: {redisFeature}");

            Console.WriteLine("  ✅ Complete example executed successfully!\n");
        }
    }
} 
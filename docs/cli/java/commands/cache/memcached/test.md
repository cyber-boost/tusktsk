# tsk cache memcached test

Test Memcached connection and basic operations.

## Synopsis

```bash
tsk cache memcached test [OPTIONS]
```

## Description

The `tsk cache memcached test` command performs a comprehensive test of the Memcached connection and basic operations including set, get, delete, and performance tests.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --host | -H | Memcached host address | localhost |
| --port | -p | Memcached port number | 11211 |
| --timeout | -t | Connection timeout in seconds | 5 |
| --iterations | -i | Number of test iterations | 100 |

## Examples

### Basic Usage
```bash
# Test Memcached connection and operations
tsk cache memcached test
```

**Output:**
```
🧪 Memcached Connection Test
🔗 Testing connection to localhost:11211...
✅ Connection established

📝 Testing basic operations:
   Set operation: ✅ (2ms)
   Get operation: ✅ (1ms)
   Delete operation: ✅ (1ms)
   Increment operation: ✅ (1ms)
   Decrement operation: ✅ (1ms)

📊 Performance test (100 iterations):
   Average set time: 1.2ms
   Average get time: 0.8ms
   Average delete time: 0.7ms
   Total operations: 500
   Success rate: 100%

✅ All tests passed successfully
```

### Custom Host and Iterations
```bash
# Test remote server with more iterations
tsk cache memcached test --host 192.168.1.100 --iterations 1000
```

## Java API Usage

```java
import org.tusklang.cli.CacheCommands;

public class MemcachedTest {
    public void testConnection() {
        CacheCommands cache = new CacheCommands();
        
        // Basic test
        TestResult result = cache.memcachedTest();
        System.out.println("Tests passed: " + result.getPassedTests());
        System.out.println("Success rate: " + result.getSuccessRate() + "%");
        
        // Custom test
        TestResult custom = cache.memcachedTest("192.168.1.100", 11211, 1000);
        System.out.println("Custom test: " + custom.getAverageResponseTime() + "ms");
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - All tests passed |
| 1 | Error - Connection failed |
| 2 | Warning - Some tests failed |

## Related Commands

- [tsk cache memcached status](./status.md) - Check service status
- [tsk cache memcached stats](./stats.md) - Show statistics
- [tsk cache test](../test.md) - Test application cache 
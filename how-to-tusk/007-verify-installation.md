# Verifying Your TuskLang Installation

After installing TuskLang, it's important to verify that everything is working correctly. This guide will walk you through comprehensive verification steps.

## Basic Verification

### Check Version

The simplest verification is checking the installed version:

```bash
tusk --version
# Expected output: TuskLang 1.0.0 (or your installed version)
```

### Check Installation Path

Verify where TuskLang is installed:

```bash
# Unix/Linux/macOS
which tusk
# Expected: /usr/local/bin/tusk or similar

# Windows PowerShell
Get-Command tusk
# Expected: C:\Program Files\TuskLang\tusk.exe or similar
```

## Command Line Interface

### Help Command

Verify the CLI is responsive:

```bash
tusk --help
```

Expected output should show:
```
TuskLang - Configuration with Grace

Usage:
  tusk [command] [flags]

Commands:
  run       Execute a TuskLang file
  serve     Start a TuskLang server
  compile   Compile TuskLang to binary
  repl      Start interactive REPL
  check     Validate TuskLang syntax
  format    Format TuskLang files
  init      Initialize new project
  test      Run tests
  version   Show version information

Flags:
  -h, --help     Show help
  -v, --verbose  Verbose output
  --debug        Debug mode
```

### REPL Test

Start the interactive REPL:

```bash
tusk repl
```

You should see:
```
TuskLang REPL v1.0.0
Type 'help' for commands, 'exit' to quit
tusk>
```

Try some basic commands:
```tusk
tusk> name: "Test"
tusk> @print(name)
Test
tusk> exit
```

## Feature Verification

### Create Test File

Create a test file to verify file operations:

```bash
# Create test directory
mkdir tusk-test
cd tusk-test

# Create test file
cat > test.tsk << 'EOF'
# Test TuskLang file
app_name: "Verification Test"
version: "1.0.0"

# Test @ operator
timestamp: @request.timestamp
platform: @tusk.platform

# Test computation
result = 2 + 2

# Test object
config:
    debug: true
    port: 8080
EOF
```

### Run Test File

```bash
tusk run test.tsk
```

Expected output:
```
Running test.tsk...
✓ File parsed successfully
✓ All operations completed
```

### Syntax Check

Verify the syntax checker:

```bash
tusk check test.tsk
```

Expected output:
```
✓ test.tsk: Valid TuskLang syntax
  - 0 errors
  - 0 warnings
```

## Environment Verification

### Check Environment Variables

```bash
# Create env test file
cat > env-test.tsk << 'EOF'
# Environment test
home: @env.HOME || "not set"
path: @env.PATH || "not set"
tusk_home: @env.TUSK_HOME || "default"

# Print results
@print("Home: ${home}")
@print("Path exists: ${@if(path != 'not set', 'yes', 'no')}")
@print("TuskLang Home: ${tusk_home}")
EOF

tusk run env-test.tsk
```

### Shell Integration

Verify shell completion:

```bash
# Type 'tusk ' and press TAB
tusk [TAB]
# Should show available commands
```

## Plugin System

### List Installed Plugins

```bash
tusk plugin list
```

Expected output:
```
Installed plugins:
  - core (built-in)
  - http (built-in)
  - database (optional)
  - cache (optional)
```

### Install Test Plugin

```bash
tusk plugin install example
```

## Performance Test

### Create Performance Test

```bash
cat > perf-test.tsk << 'EOF'
# Performance test
start_time: @request.timestamp

# Create large array
data = @range(1, 10000)

# Process data
result = @map(data, @lambda(x, x * 2))

# Calculate execution time
end_time: @request.timestamp
duration = end_time - start_time

@print("Processed ${@len(result)} items in ${duration}ms")
EOF

tusk run perf-test.tsk
```

## Integration Tests

### HTTP Server Test

```bash
cat > server-test.tsk << 'EOF'
# Simple HTTP server
server:
    port: 8080
    host: "localhost"

routes:
    "/":
        response: "TuskLang server is running!"
    
    "/api/status":
        response: @json({
            status: "ok"
            version: @tusk.version
            timestamp: @request.timestamp
        })
EOF

# Start server in background
tusk serve server-test.tsk &
SERVER_PID=$!

# Wait for server to start
sleep 2

# Test server
curl http://localhost:8080/
curl http://localhost:8080/api/status

# Stop server
kill $SERVER_PID
```

## Debugging Verification

### Enable Debug Mode

```bash
# Run with debug output
tusk --debug run test.tsk
```

Expected debug output:
```
[DEBUG] Loading file: test.tsk
[DEBUG] Parsing TuskLang syntax
[DEBUG] Resolving @ operators
[DEBUG] Executing runtime operations
[DEBUG] Completed successfully
```

### Verbose Mode

```bash
tusk -v run test.tsk
```

## Common Issues and Solutions

### Command Not Found

If `tusk` command is not found:

**Linux/macOS:**
```bash
# Check PATH
echo $PATH

# Add to PATH if missing
export PATH="/usr/local/bin:$PATH"
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.bashrc
```

**Windows:**
```powershell
# Check PATH
$env:Path

# Restart terminal or run
refreshenv
```

### Permission Denied

```bash
# Fix permissions
chmod +x $(which tusk)

# Or reinstall with proper permissions
sudo tusk repair
```

### Missing Dependencies

```bash
# Check dependencies
tusk doctor
```

Expected output:
```
TuskLang Doctor
==============
✓ TuskLang binary: OK
✓ Configuration directory: OK
✓ Plugin directory: OK
✓ Cache directory: OK
✓ SSL certificates: OK
✓ Network connectivity: OK

All systems operational!
```

## Platform-Specific Verification

### Linux

```bash
# Check systemd integration
systemctl --user status tusklang

# Check package info
dpkg -l | grep tusklang  # Debian/Ubuntu
rpm -qa | grep tusklang  # RedHat/Fedora
```

### macOS

```bash
# Check Homebrew installation
brew list tusklang

# Check code signing
codesign -v $(which tusk)
```

### Windows

```powershell
# Check Windows service
Get-Service -Name "TuskLang*"

# Check registry
Get-ItemProperty HKLM:\Software\TuskLang
```

## Configuration Verification

### Global Configuration

```bash
# Check global config
tusk config show

# Test configuration loading
cat > config-test.tsk << 'EOF'
# Load global config
global_setting: @config.global_setting || "not set"

@print("Global setting: ${global_setting}")
EOF

tusk run config-test.tsk
```

## Advanced Verification

### Compile Test

```bash
# Test compilation
tusk compile test.tsk -o test-binary

# Run compiled binary
./test-binary  # Unix/Linux/macOS
test-binary.exe  # Windows
```

### Format Test

```bash
# Test formatter
tusk format test.tsk --check
```

### Benchmark

```bash
# Run built-in benchmark
tusk benchmark
```

Expected output:
```
TuskLang Benchmark Results
=========================
Parse simple file: 0.5ms
Parse complex file: 2.3ms
Execute @ operators: 1.2ms
HTTP server response: 0.8ms
Database query simulation: 3.1ms

Overall performance: Excellent
```

## Verification Checklist

Run through this checklist to ensure full functionality:

- [ ] `tusk --version` shows correct version
- [ ] `tusk --help` displays help information
- [ ] `tusk repl` starts interactive mode
- [ ] Can create and run .tsk files
- [ ] @ operators work correctly
- [ ] Environment variables are accessible
- [ ] Shell completion works
- [ ] Debug mode provides detailed output
- [ ] HTTP server can start and respond
- [ ] Syntax checking identifies errors
- [ ] Formatting works correctly
- [ ] Compilation produces working binaries

## Getting Help

If verification fails:

1. Check the [troubleshooting guide](https://tusklang.org/docs/troubleshooting)
2. Run `tusk doctor` for diagnostics
3. Check GitHub issues: https://github.com/tusklang/tusk/issues
4. Join the community: https://discord.gg/tusklang

## Next Steps

Now that you've verified your installation:
- Create your first project: [008-hello-world.md](008-hello-world.md)
- Learn the file structure: [009-file-structure.md](009-file-structure.md)
- Master the CLI: [010-cli-overview.md](010-cli-overview.md)
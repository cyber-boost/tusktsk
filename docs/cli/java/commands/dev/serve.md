# tsk serve

Start development server with hot reloading.

## Synopsis

```bash
tsk serve [port] [OPTIONS]
```

## Description

The `tsk serve` command starts a development HTTP server with hot reloading capabilities. It automatically detects file changes and restarts the server or reloads modules as needed.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --host | -H | Host address to bind to | localhost |
| --port | -p | Port number to listen on | 8080 |
| --watch | -w | Enable file watching | true |
| --open | -o | Open browser automatically | false |
| --verbose | -v | Show detailed server information | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| port | No | Port number (default: 8080) |

## Examples

### Basic Usage
```bash
# Start development server on default port
tsk serve
```

**Output:**
```
🚀 Development Server Starting
📁 Project root: /path/to/project
🔧 Configuration loaded from peanu.pnt
🌐 Server binding to: localhost:8080
👀 File watching: Enabled
🔄 Hot reload: Enabled

✅ Server started successfully
📊 Server information:
   - URL: http://localhost:8080
   - Process ID: 12345
   - Memory usage: 45.2MB
   - Uptime: 0s

🔍 Watching for file changes...
📝 Press Ctrl+C to stop server
```

### Custom Port and Host
```bash
# Start server on specific port and host
tsk serve 3000 --host 0.0.0.0
```

### Disable File Watching
```bash
# Start server without file watching
tsk serve --no-watch
```

## Java API Usage

```java
import org.tusklang.cli.DevelopmentCommands;

public class DevelopmentServer {
    public void startServer() {
        DevelopmentCommands dev = new DevelopmentCommands();
        
        // Start server
        ServerInfo info = dev.serve(8080);
        System.out.println("Server URL: " + info.getUrl());
        
        // Start with options
        ServerInfo custom = dev.serve(3000, "0.0.0.0", true);
        System.out.println("Custom server: " + custom.getUrl());
    }
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Server started |
| 1 | Error - Server failed to start |
| 2 | Warning - Port already in use |

## Related Commands

- [tsk db status](../db/status.md) - Check database status
- [tsk config check](../config/check.md) - Validate configuration
- [tsk test all](../test/all.md) - Run tests 
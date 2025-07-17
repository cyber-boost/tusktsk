# tsk serve

Start a development server for your TuskLang project.

## Synopsis
```bash
tsk serve [port]
```

## Description
Launches a local development server using the configuration in your project. Supports hot reload and custom port.

## Options
| Option | Description | Default |
|--------|-------------|---------|
| [port] | Port to listen on | 3000 |
| --help | Show help | - |
| --config | Use alternate config | peanu.peanuts |
| --verbose | Verbose output | false |

## Examples
```bash
tsk serve
# or specify port
tsk serve 4000
```

## JavaScript Integration
```javascript
const { execSync } = require('child_process');
execSync('tsk serve 3000', { stdio: 'inherit' });
``` 
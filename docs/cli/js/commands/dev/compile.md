# tsk compile

Compile a .tsk file to a validated configuration.

## Synopsis
```bash
tsk compile <file>
```

## Description
Compiles a TuskLang .tsk file, validates syntax, and outputs a ready-to-use configuration. Useful for preparing configs for production.

## Options
| Option | Description | Default |
|--------|-------------|---------|
| <file> | TSK file to compile | (required) |
| --help | Show help | - |
| --out | Output file | compiled.peanuts |

## Examples
```bash
tsk compile config.tsk
# Specify output file
tsk compile config.tsk --out myconfig.peanuts
```

## JavaScript Integration
```javascript
const { execSync } = require('child_process');
execSync('tsk compile config.tsk', { stdio: 'inherit' });
``` 
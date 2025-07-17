# tsk optimize

Optimize a .tsk file for production use.

## Synopsis
```bash
tsk optimize <file>
```

## Description
Optimizes a TuskLang .tsk file by removing unused keys, compressing output, and preparing for binary compilation.

## Options
| Option | Description | Default |
|--------|-------------|---------|
| <file> | TSK file to optimize | (required) |
| --help | Show help | - |
| --out | Output file | optimized.tsk |

## Examples
```bash
tsk optimize config.tsk
# Specify output file
tsk optimize config.tsk --out config.optimized.tsk
```

## JavaScript Integration
```javascript
const { execSync } = require('child_process');
execSync('tsk optimize config.tsk', { stdio: 'inherit' });
``` 
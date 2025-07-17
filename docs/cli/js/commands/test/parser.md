# tsk test parser

Test the TuskLang parser implementation.

## Synopsis
```bash
tsk test parser
```

## Description
Runs the parser test suite to validate syntax parsing and error handling.

## Options
| Option | Description | Default |
|--------|-------------|---------|
| --help | Show help | - |
| --json | Output in JSON format | false |
| --verbose | Verbose output | false |

## Examples
```bash
tsk test parser
```

## JavaScript Integration
```javascript
const { execSync } = require('child_process');
execSync('tsk test parser', { stdio: 'inherit' });
``` 
# tsk test fujsen

Test the FUJSEN operator implementation.

## Synopsis
```bash
tsk test fujsen
```

## Description
Runs the FUJSEN test suite to validate operator logic and edge cases.

## Options
| Option | Description | Default |
|--------|-------------|---------|
| --help | Show help | - |
| --json | Output in JSON format | false |
| --verbose | Verbose output | false |

## Examples
```bash
tsk test fujsen
```

## JavaScript Integration
```javascript
const { execSync } = require('child_process');
execSync('tsk test fujsen', { stdio: 'inherit' });
``` 
# tsk test all

Run all test suites for your TuskLang project.

## Synopsis
```bash
tsk test all
```

## Description
Runs all available test suites, including parser, FUJSEN, SDK, and performance tests.

## Options
| Option | Description | Default |
|--------|-------------|---------|
| --help | Show help | - |
| --json | Output in JSON format | false |
| --verbose | Verbose output | false |

## Examples
```bash
tsk test all
```

## JavaScript Integration
```javascript
const { execSync } = require('child_process');
execSync('tsk test all', { stdio: 'inherit' });
``` 
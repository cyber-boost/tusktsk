# tsk license check

Check license validity and status for the TuskLang PHP CLI.

## Synopsis

```bash
tsk license check [OPTIONS]
```

## Description

The `tsk license check` command validates the current license status and provides detailed information about license validity, expiration, features, and usage limits.

This command connects to TuskLang's license validation servers to verify the authenticity and current status of your license key.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--help` | `-h` | Show help for this command | - |
| `--verbose` | `-v` | Enable verbose output | false |
| `--json` | `-j` | Output in JSON format | false |
| `--features` | `-f` | List available features | false |
| `--offline` | `-o` | Check cached license only | false |
| `--force` | `-F` | Force revalidation | false |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| None | No | No arguments required |

## Examples

### Basic Usage

```bash
# Check license status
tsk license check
```

### Verbose Output

```bash
# Get detailed license information
tsk license check --verbose
```

### JSON Output

```bash
# Get machine-readable output
tsk license check --json
```

### Feature List

```bash
# List available features
tsk license check --features
```

### Offline Check

```bash
# Check cached license without network
tsk license check --offline
```

### Force Revalidation

```bash
# Force revalidation with license server
tsk license check --force
```

## PHP API Usage

```php
<?php
// Check license programmatically
function checkLicense($options = []) {
    $cmd = 'tsk license check';
    
    if (!empty($options['verbose'])) {
        $cmd .= ' --verbose';
    }
    
    if (!empty($options['json'])) {
        $cmd .= ' --json';
    }
    
    if (!empty($options['offline'])) {
        $cmd .= ' --offline';
    }
    
    $output = [];
    $returnCode = 0;
    
    exec($cmd . ' 2>&1', $output, $returnCode);
    
    if ($returnCode === 0) {
        if (!empty($options['json'])) {
            $jsonOutput = implode("\n", $output);
            return json_decode($jsonOutput, true);
        } else {
            return implode("\n", $output);
        }
    } else {
        throw new Exception("License check failed: " . implode("\n", $output));
    }
}

// Usage examples
$status = checkLicense(['json' => true]);
$verbose = checkLicense(['verbose' => true]);
$offline = checkLicense(['offline' => true]);
```

## Output

### Success Output

```
License Status: Valid
Type: Commercial
Expires: 2025-12-31
Features: enterprise,ai,advanced-cache,priority-support
Last Check: 2024-01-15T10:30:00Z
```

### JSON Output

```json
{
  "valid": true,
  "type": "commercial",
  "expires": "2025-12-31",
  "features": ["enterprise", "ai", "advanced-cache", "priority-support"],
  "last_check": "2024-01-15T10:30:00Z",
  "usage": {
    "current": 150,
    "limit": 1000
  }
}
```

### Error Output

```
License Status: Invalid
Error: License expired on 2024-01-01
Please activate a new license with: tsk license activate <key>
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | License is valid |
| 1 | License is invalid or expired |
| 2 | Network error during validation |
| 3 | Configuration error |
| 4 | Permission denied |

## Related Commands

- [tsk license activate](./activate.md) - Activate a license key
- [tsk config get](../config/get.md) - Get configuration values
- [tsk status](../utility/status.md) - Check system status

## Notes

- License validation requires internet connectivity unless using `--offline`
- Cached license information is used for offline checks
- Force revalidation bypasses cache and contacts license servers
- JSON output is useful for programmatic integration

## See Also

- [License Overview](./README.md)
- [Configuration Guide](../config/README.md)
- [Troubleshooting Guide](../../troubleshooting.md)

**Strong. Secure. Scalable.** 🐘 
# tsk license activate

Activate a license key for the TuskLang PHP CLI.

## Synopsis

```bash
tsk license activate <LICENSE_KEY> [OPTIONS]
```

## Description

The `tsk license activate` command activates a new license key for the TuskLang PHP CLI. This command validates the license key with TuskLang's license servers and stores it securely in the local configuration.

After activation, the license key is encrypted and stored locally. The system will automatically validate the license periodically and when using premium features.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--help` | `-h` | Show help for this command | - |
| `--verbose` | `-v` | Enable verbose output | false |
| `--json` | `-j` | Output in JSON format | false |
| `--force` | `-f` | Force activation even if license exists | false |
| `--offline` | `-o` | Activate offline (requires pre-validated key) | false |
| `--email` | `-e` | Associate email with license | - |

## Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `LICENSE_KEY` | Yes | The license key to activate |

## Examples

### Basic Usage

```bash
# Activate a license key
tsk license activate YOUR_LICENSE_KEY_HERE
```

### Verbose Activation

```bash
# Get detailed activation information
tsk license activate YOUR_LICENSE_KEY_HERE --verbose
```

### JSON Output

```bash
# Get machine-readable output
tsk license activate YOUR_LICENSE_KEY_HERE --json
```

### Force Activation

```bash
# Force activation even if license exists
tsk license activate NEW_LICENSE_KEY --force
```

### With Email Association

```bash
# Associate email with license
tsk license activate YOUR_LICENSE_KEY_HERE --email user@example.com
```

### Offline Activation

```bash
# Activate offline (requires pre-validated key)
tsk license activate YOUR_LICENSE_KEY_HERE --offline
```

## PHP API Usage

```php
<?php
// Activate license programmatically
function activateLicense($licenseKey, $options = []) {
    $cmd = "tsk license activate $licenseKey";
    
    if (!empty($options['verbose'])) {
        $cmd .= ' --verbose';
    }
    
    if (!empty($options['json'])) {
        $cmd .= ' --json';
    }
    
    if (!empty($options['force'])) {
        $cmd .= ' --force';
    }
    
    if (!empty($options['email'])) {
        $cmd .= " --email {$options['email']}";
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
        throw new Exception("License activation failed: " . implode("\n", $output));
    }
}

// Usage examples
$result = activateLicense('YOUR_LICENSE_KEY', ['json' => true]);
$verbose = activateLicense('YOUR_LICENSE_KEY', ['verbose' => true]);
$withEmail = activateLicense('YOUR_LICENSE_KEY', ['email' => 'user@example.com']);
```

## Output

### Success Output

```
License activated successfully!
Type: Commercial
Expires: 2025-12-31
Features: enterprise,ai,advanced-cache,priority-support
Email: user@example.com
```

### JSON Output

```json
{
  "success": true,
  "license": {
    "type": "commercial",
    "expires": "2025-12-31",
    "features": ["enterprise", "ai", "advanced-cache", "priority-support"],
    "email": "user@example.com"
  },
  "message": "License activated successfully"
}
```

### Error Output

```
License activation failed!
Error: Invalid license key
Please check your license key and try again.
```

### Verbose Output

```
Connecting to license server...
Validating license key...
Key format: Valid
Server response: Success
Storing encrypted license...
License activated successfully!
Type: Commercial
Expires: 2025-12-31
Features: enterprise,ai,advanced-cache,priority-support
Email: user@example.com
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | License activated successfully |
| 1 | Invalid license key |
| 2 | Network error during activation |
| 3 | License already exists (use --force to override) |
| 4 | Configuration error |
| 5 | Permission denied |

## Security Notes

- License keys are encrypted before storage
- Network communication uses HTTPS
- License validation occurs on TuskLang servers
- Offline activation requires pre-validated keys

## Related Commands

- [tsk license check](./check.md) - Check license status
- [tsk config set](../config/set.md) - Set configuration values
- [tsk status](../utility/status.md) - Check system status

## Notes

- License activation requires internet connectivity unless using `--offline`
- Force activation will replace any existing license
- Email association is optional but recommended for support
- Offline activation requires a pre-validated license key

## See Also

- [License Overview](./README.md)
- [Configuration Guide](../config/README.md)
- [Troubleshooting Guide](../../troubleshooting.md)

**Strong. Secure. Scalable.** 🐘 
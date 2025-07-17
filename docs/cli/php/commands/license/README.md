# License Commands - PHP CLI

License management and validation commands for TuskLang PHP CLI.

## Available Commands

| Command | Description |
|---------|-------------|
| [tsk license check](./check.md) | Check license validity and status |
| [tsk license activate](./activate.md) | Activate a license key |

## Common Use Cases

- **License Validation** - Verify license status and validity
- **License Activation** - Activate new license keys
- **Compliance Checking** - Ensure proper licensing for production use
- **Troubleshooting** - Diagnose license-related issues
- **Audit Support** - Generate license reports for compliance

## PHP-Specific Notes

### License Storage

Licenses are stored securely in the TuskLang configuration:

```php
<?php
// License configuration structure
$licenseConfig = [
    'license' => [
        'key' => 'encrypted_license_key',
        'type' => 'commercial|enterprise|community',
        'expires' => '2025-12-31',
        'features' => ['feature1', 'feature2'],
        'valid' => true,
        'last_check' => '2024-01-15T10:30:00Z'
    ]
];
```

### License Validation

The license system validates against TuskLang's license servers:

```php
<?php
// Example license validation
class LicenseValidator
{
    public function validateLicense($licenseKey)
    {
        $validationData = [
            'key' => $licenseKey,
            'version' => '2.0.0',
            'platform' => 'php',
            'timestamp' => time()
        ];
        
        // Send validation request to license server
        $response = $this->sendValidationRequest($validationData);
        
        return $response['valid'] ?? false;
    }
}
```

### Feature Gating

Licenses control access to premium features:

```php
<?php
// Feature gating based on license
class FeatureGate
{
    public function isFeatureEnabled($featureName)
    {
        $license = $this->getLicenseInfo();
        
        if (!$license['valid']) {
            return false;
        }
        
        return in_array($featureName, $license['features']);
    }
    
    public function requireFeature($featureName)
    {
        if (!$this->isFeatureEnabled($featureName)) {
            throw new LicenseException("Feature '$featureName' requires valid license");
        }
    }
}
```

## Examples

### Basic License Operations

```bash
# Check license status
tsk license check

# Activate a new license
tsk license activate YOUR_LICENSE_KEY

# Check with verbose output
tsk license check --verbose

# Get JSON output
tsk license check --json
```

### License Validation Workflow

```bash
# 1. Check current license
tsk license check

# 2. If invalid, activate new license
tsk license activate NEW_LICENSE_KEY

# 3. Verify activation
tsk license check

# 4. Check specific features
tsk license check --features
```

### Production License Setup

```bash
# 1. Validate license before deployment
tsk license check

# 2. Ensure all required features are available
tsk license check --features enterprise,ai,advanced-cache

# 3. Activate if needed
tsk license activate PRODUCTION_LICENSE_KEY

# 4. Final validation
tsk license check --verbose
```

## Error Handling

License commands provide detailed error information:

```bash
# Check for specific license errors
tsk license check --verbose

# Debug license issues
tsk --debug license check

# Get detailed error information
tsk license check --json
```

## License Types

### Community License

- **Features**: Basic functionality, limited features
- **Usage**: Development, testing, small projects
- **Limitations**: No premium features, usage limits

### Commercial License

- **Features**: Full functionality, premium features
- **Usage**: Production applications, commercial use
- **Benefits**: Priority support, advanced features

### Enterprise License

- **Features**: All features, unlimited usage
- **Usage**: Large organizations, enterprise deployments
- **Benefits**: Custom support, SLA guarantees

## Related Commands

- [Configuration Commands](../config/README.md) - License configuration management
- [Utility Commands](../utility/README.md) - License utility functions
- [AI Commands](../ai/README.md) - AI features (license-dependent)

## See Also

- [License Configuration Guide](../config/README.md)
- [Feature Management](../utility/README.md)
- [Troubleshooting Guide](../../troubleshooting.md)

**Strong. Secure. Scalable.** 🐘 
# Go Module Publishing Guide for GitHub Packages

This guide will help you successfully publish your Go module to GitHub Packages and resolve the common issues you've been experiencing.

## Quick Start

### 1. Publish a New Version

```bash
# Make the script executable (if not already)
chmod +x publish-go-module.sh

# Publish version 1.0.0
./publish-go-module.sh 1.0.0

# Or publish a different version
./publish-go-module.sh 1.1.0
```

### 2. Manual Publishing via GitHub Actions

1. Go to your GitHub repository
2. Click on "Actions" tab
3. Select "Publish Go Module to GitHub Packages"
4. Click "Run workflow"
5. Enter the version (e.g., 1.0.0)
6. Click "Run workflow"

## What Was Fixed

### 1. Module Path Correction
- **Before**: `github.com/cyber-boost/tusktsk/sdk/go`
- **After**: `github.com/cyber-boost/tusktsk`

The module path now matches your GitHub repository structure.

### 2. Workflow Improvements
- Proper Go module validation
- Test execution before publishing
- Better error handling
- Correct GitHub Packages configuration

### 3. Versioning Strategy
- Uses semantic versioning (v1.0.0, v1.1.0, etc.)
- Automatic tag creation
- Proper release management

## Installation Instructions

Once published, users can install your module:

```bash
# Install latest version
go get github.com/cyber-boost/tusktsk

# Install specific version
go get github.com/cyber-boost/tusktsk@v1.0.0

# Use in your Go code
import "github.com/cyber-boost/tusktsk"
```

## Troubleshooting Common Issues

### Issue 1: "Module not found" errors
**Solution**: Ensure the module path in `go.mod` matches your GitHub repository exactly.

### Issue 2: Authentication errors
**Solution**: The workflow uses `GITHUB_TOKEN` automatically. No additional setup needed.

### Issue 3: Version conflicts
**Solution**: Use semantic versioning and never reuse version numbers.

### Issue 4: Build failures
**Solution**: Run `go mod tidy` and `go test ./...` locally before publishing.

## Verification Steps

After publishing, verify the module is available:

```bash
# Check if module exists
go list -m github.com/cyber-boost/tusktsk@v1.0.0

# Try to download it
go get github.com/cyber-boost/tusktsk@v1.0.0

# Check available versions
go list -m -versions github.com/cyber-boost/tusktsk
```

## GitHub Packages Configuration

The module will appear in:
- **GitHub Repository**: Packages tab
- **Install URL**: `github.com/cyber-boost/tusktsk`
- **Registry**: GitHub Container Registry (ghcr.io)

## Best Practices

1. **Always test locally** before publishing
2. **Use semantic versioning** (MAJOR.MINOR.PATCH)
3. **Never delete published versions**
4. **Keep go.mod clean** with `go mod tidy`
5. **Document breaking changes** in release notes

## File Structure

```
.
├── go.mod                    # Module definition
├── go.sum                    # Dependency checksums
├── main.go                   # Main application
├── publish-go-module.sh      # Publishing script
├── .github/workflows/
│   └── publish.yml          # GitHub Actions workflow
└── GO_PACKAGES_GUIDE.md     # This guide
```

## Next Steps

1. Run `./publish-go-module.sh 1.0.0` to publish your first version
2. Check the GitHub Actions tab for progress
3. Verify the module appears in the Packages tab
4. Test installation with `go get github.com/cyber-boost/tusktsk@v1.0.0`

## Support

If you encounter issues:
1. Check the GitHub Actions logs
2. Verify your repository permissions
3. Ensure the module path is correct
4. Run the troubleshooting steps above

The module should now publish successfully to GitHub Packages without the email alerts and errors you were experiencing. 
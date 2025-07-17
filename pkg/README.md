# TuskLang Package Manager Deployment

This directory contains the deployment infrastructure for publishing TuskLang SDKs to various package managers.

## Structure

```
pkg/
├── DEPLOYMENT_PLAN.md    # Detailed deployment strategy
├── deploy-all.sh         # Main deployment script
├── README.md            # This file
├── composer/            # PHP/Packagist specific files
├── npm/                 # JavaScript/npm specific files
├── python/              # Python/PyPI specific files
├── ruby/                # Ruby/RubyGems specific files
├── rust/                # Rust/crates.io specific files
├── go/                  # Go modules specific files
├── maven/               # Java/Maven Central specific files
└── nuget/               # C#/NuGet specific files
```

## Quick Start

### Dry Run (Test deployment without publishing)
```bash
DRY_RUN=true ./deploy-all.sh
```

### Production Deployment
```bash
./deploy-all.sh
```

## Prerequisites

Before deploying, ensure you have:

1. **Package Manager Accounts**:
   - [ ] npm account for JavaScript
   - [ ] PyPI account for Python
   - [ ] RubyGems.org account for Ruby
   - [ ] crates.io account for Rust
   - [ ] Sonatype JIRA account for Maven Central
   - [ ] NuGet.org account for C#

2. **Authentication Configured**:
   ```bash
   # npm
   npm login
   
   # Python
   # Create ~/.pypirc with credentials
   
   # Ruby
   gem signin
   
   # Rust
   cargo login <token>
   
   # Maven
   # Configure ~/.m2/settings.xml
   
   # NuGet
   dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
   ```

3. **Tools Installed**:
   - git, npm, composer, python3, gem, cargo, go, mvn, dotnet

## Individual Package Deployment

To deploy a specific package only:

```bash
cd ~/pkg/<package-manager>/
./deploy.sh
```

## Version Management

All SDKs should be at the same version. Update version in:
- `composer.json` (PHP)
- `package.json` (JavaScript)
- `setup.py` or `pyproject.toml` (Python)
- `.gemspec` (Ruby)
- `Cargo.toml` (Rust)
- `go.mod` (Go)
- `pom.xml` (Java)
- `.csproj` (C#)

## Security Notes

- Never commit API keys or tokens
- Use environment variables for sensitive data
- Enable 2FA on all package manager accounts
- Sign packages where required (especially Maven Central)
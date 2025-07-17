# TuskLang Package Registry Links & Status
Last Updated: July 17, 2025

## ✅ Successfully Deployed Packages

| Language    | Package Manager | Package Name     | Version | Link | Installation |
|-------------|----------------|------------------|---------|------|--------------|
| **PHP**     | Packagist      | tusktsk/tusktsk  | 2.0.1   | [View on Packagist](https://packagist.org/packages/tusktsk/tusktsk) | `composer require tusktsk/tusktsk` |
| **JavaScript** | npm         | tusktsk          | 2.0.1   | [View on npm](https://www.npmjs.com/package/tusktsk) | `npm install tusktsk` |
| **Ruby**    | RubyGems       | tusktsk          | 2.0.1   | [View on RubyGems](https://rubygems.org/gems/tusktsk) | `gem install tusktsk` |
| **Python**  | PyPI           | tusktsk          | 2.0.1   | [View on PyPI](https://pypi.org/project/tusktsk/2.0.1/) | `pip install tusktsk` |
| **Rust**    | crates.io      | tusktsk          | 2.0.1   | [View on crates.io](https://crates.io/crates/tusktsk) | `cargo add tusktsk` |

## ⏸️ Pending Deployment (From deploy-all.sh)

| Language | Package Manager | Issue | Next Step |
|----------|----------------|-------|-----------|
| **Go**   | GitHub/pkg.go.dev | Deployed successfully | Tag with `git tag sdk/go/v2.0.1 && git push --tags` |
| **Java** | Maven Central | Compilation issues | Fix `PeanutConfig.java` and `TuskProtection.java` |
| **C#**   | NuGet | Build issues | Fix triple-quoted strings in `TestingCommands.cs` |

## 🔗 Quick Links

### Main Repository
- **GitHub**: https://github.com/cyber-boost/tusktsk
- **Website**: https://tuskt.sk
- **Documentation**: https://tuskt.sk/docs
- **License**: https://tuskt.sk/license
- **Support**: hi@tuskt.sk

### Package Registry Direct Links
- **PHP (Packagist)**: https://packagist.org/packages/tusktsk/tusktsk
- **JavaScript (npm)**: https://www.npmjs.com/package/tusktsk
- **Python (PyPI)**: https://pypi.org/project/tusktsk/
- **Rust (crates.io)**: https://crates.io/crates/tusktsk
- **Ruby (RubyGems)**: https://rubygems.org/gems/tusktsk
- **Go (pkg.go.dev)**: https://pkg.go.dev/github.com/cyber-boost/tusktsk (pending tag)
- **Java (Maven Central)**: https://search.maven.org/artifact/com.cyberboost/tusktsk (pending)
- **C# (NuGet)**: https://www.nuget.org/packages/TuskTsk (pending)

## 📊 Deployment Summary

### Success Rate: 5/8 (62.5%)
- ✅ PHP: Successfully deployed to Packagist
- ✅ JavaScript: Successfully deployed to npm  
- ✅ Ruby: Successfully deployed to RubyGems
- ✅ Python: Successfully deployed to PyPI
- ✅ Rust: Successfully deployed to crates.io
- ✅ Go: Code deployed, needs git tag
- ❌ Java: Compilation errors blocking deployment
- ❌ C#: Build errors blocking deployment

## 🚀 Next Steps

1. **Complete Go Deployment**:
   ```bash
   cd /opt/tsk_git
   git tag sdk/go/v2.0.1
   git push origin sdk/go/v2.0.1
   ```

2. **Fix Java Compilation**:
   - Fix syntax errors in `sdk/java/src/main/java/org/tusklang/PeanutConfig.java`
   - Fix compilation issues in `sdk/java/src/main/java/tusk/protection/TuskProtection.java`

3. **Fix C# Build**:
   - Replace triple-quoted strings with verbatim strings in `sdk/csharp/CLI/Commands/TestingCommands.cs`
   - Run `dotnet build` to verify fixes

## 📝 Notes from deploy-all.sh

- **PHP/npm/Ruby**: Already successfully deployed (skipped in latest run)
- **Python**: Latest deployment run completed successfully
- **Rust**: Requires email verification at https://crates.io/settings/profile if deployment fails
- **Go**: Modules require manual git tag push: `git tag sdk/go/v2.0.1 && git push --tags`
- **Java/C#**: Has compilation issues that need to be resolved before deployment

---

*TuskLang Configuration Language - The heartbeat adapts to your syntax*
# Multi-Environment Configuration - Go

## 🎯 What Is Multi-Environment Configuration?

Multi-environment directives in TuskLang let you define environment-specific settings, overrides, and inheritance for Go projects.

```go
type MultiEnvConfig struct {
    Environments map[string]string `tsk:"#envs"`
    Overrides    map[string]string `tsk:"#env_overrides"`
}
```

## 🚀 Why Multi-Environment Matters

- Seamlessly manage dev, staging, prod, and custom envs
- Override config per environment

## 📋 Multi-Environment Directive Types

- **Environments**: dev, staging, prod, test, custom
- **Overrides**: Per-env keys, secrets, endpoints

## 🔧 Example
```tsk
envs: #envs("dev,staging,prod")
env_overrides: #envs("prod:db_url=prod.db,dev:db_url=dev.db")
```

## 🎯 Go Integration
```go
type MultiEnvConfig struct {
    Environments string `tsk:"#envs"`
    Overrides    string `tsk:"#env_overrides"`
}
```

## 🛡️ Best Practices
- Always separate secrets per env
- Use inheritance for shared config
- Validate env at startup

## ⚡ Summary
Multi-environment config makes Go apps robust and deployment-ready. Integrate with Go build tags, env vars, and TuskLang for full flexibility. 
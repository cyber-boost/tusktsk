# @env Operator in TuskLang - Go Guide

## 🌱 **Environment Variables, Supercharged**

TuskLang’s `@env` operator is the ultimate bridge between your environment and your config. We don’t bow to any king—not even .env files. Here’s how to use `@env` in Go projects for secure, dynamic configuration.

## 📋 **Table of Contents**
- [What is @env?](#what-is-env)
- [Basic Usage](#basic-usage)
- [Default Values](#default-values)
- [Security Considerations](#security-considerations)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🧬 **What is @env?**

The `@env` operator injects environment variables directly into your config. No more static secrets or hardcoded values—just pure, dynamic power.

## 🛠️ **Basic Usage**

```go
[api]
api_key: @env("API_KEY")
```

## 🪙 **Default Values**

```go
[api]
timeout: @env("API_TIMEOUT", 30)
```

## 🔒 **Security Considerations**
- Use `@env.secure` for sensitive values
- Never commit secrets to version control
- Validate all required env vars at startup

## 🔗 **Go Integration**

```go
apiKey := config.GetString("api_key") // Loads from @env
```

### **Manual Fallback**
```go
val := os.Getenv("API_KEY")
if val == "" { val = "default" }
```

## 🥇 **Best Practices**
- Use `@env` for all environment-driven config
- Always provide a default value
- Validate presence of critical env vars

---

**TuskLang: No .env files. No leaks. Just pure environment power.** 
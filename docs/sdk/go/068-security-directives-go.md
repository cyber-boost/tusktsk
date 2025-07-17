# Security Directives - Go

## 🎯 What Are Security Directives?

Security directives (`#security`) in TuskLang let you define encryption, access control, compliance, and security policies in config files.

```go
type SecurityConfig struct {
    Encryption map[string]string `tsk:"#security_encryption"`
    Access     map[string]string `tsk:"#security_access"`
    Compliance map[string]string `tsk:"#security_compliance"`
}
```

## 🚀 Why Security Directives Matter

- Centralize security policies
- Enforce encryption, RBAC, audit, and compliance

## 📋 Security Directive Types

- **Encryption**: AES, GCM, key rotation, secrets
- **Access**: RBAC, ABAC, IP allow/deny, MFA
- **Compliance**: GDPR, HIPAA, PCI, audit logging

## 🔧 Example
```tsk
security_encryption: #security("algorithm:AES-256-GCM,key:#env('SEC_KEY'),rotate:30d")
security_access: #security("roles:admin,user,guest,ip_whitelist:[127.0.0.1]")
security_compliance: #security("gdpr:true,audit:true,retention:365d")
```

## 🎯 Go Integration
```go
type SecurityConfig struct {
    Encryption string `tsk:"#security_encryption"`
    Access     string `tsk:"#security_access"`
    Compliance string `tsk:"#security_compliance"`
}
```

## 🛡️ Best Practices
- Rotate keys regularly
- Log all access and changes
- Use least privilege

## ⚡ Summary
Security directives make Go apps robust, compliant, and audit-ready. Integrate with Go crypto, auth, and logging libraries for full-stack security. 
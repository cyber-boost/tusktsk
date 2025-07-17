# Audit Directives - Go

## 🎯 What Are Audit Directives?

Audit directives (`#audit`) in TuskLang let you define audit logging, compliance, and event tracking in Go projects.

```go
type AuditConfig struct {
    Events   map[string]string `tsk:"#audit_events"`
    Retention map[string]string `tsk:"#audit_retention"`
    Compliance map[string]string `tsk:"#audit_compliance"`
}
```

## 🚀 Why Audit Directives Matter

- Track sensitive actions
- Meet compliance (GDPR, HIPAA, PCI)

## 📋 Audit Directive Types

- **Events**: login, data access, config change
- **Retention**: 30d, 90d, 1y
- **Compliance**: GDPR, HIPAA, PCI

## 🔧 Example
```tsk
audit_events: #audit("login,logout,config_change")
audit_retention: #audit("90d")
audit_compliance: #audit("gdpr:true,hipaa:false")
```

## 🎯 Go Integration
```go
type AuditConfig struct {
    Events     string `tsk:"#audit_events"`
    Retention  string `tsk:"#audit_retention"`
    Compliance string `tsk:"#audit_compliance"`
}
```

## 🛡️ Best Practices
- Log all sensitive actions
- Retain logs per policy
- Encrypt audit logs

## ⚡ Summary
Audit directives make Go apps compliant and traceable. Integrate with Go log, SIEM, and cloud audit tools for best results. 
# Feature Flags - Go

## 🎯 What Are Feature Flags?

Feature flag directives (`#feature_flag`) in TuskLang let you define toggles, rollout strategies, and dynamic features in config files.

```go
type FeatureFlagConfig struct {
    Flags   map[string]bool `tsk:"#feature_flags"`
    Rollout map[string]string `tsk:"#feature_rollout"`
}
```

## 🚀 Why Feature Flags Matter

- Enable safe, dynamic releases
- Support A/B testing, gradual rollout, and canary

## 📋 Feature Flag Directive Types

- **Flags**: On/off, percentage, user targeting
- **Rollout**: Gradual, scheduled, conditional

## 🔧 Example
```tsk
feature_flags: #feature_flag("new_ui:true,search_v2:false")
feature_rollout: #feature_flag("new_ui:50%,search_v2:canary")
```

## 🎯 Go Integration
```go
type FeatureFlagConfig struct {
    Flags   string `tsk:"#feature_flags"`
    Rollout string `tsk:"#feature_rollout"`
}
```

## 🛡️ Best Practices
- Use for all risky features
- Monitor flag impact
- Remove stale flags

## ⚡ Summary
Feature flags make Go apps agile and experiment-friendly. Integrate with Go config, HTTP handlers, and monitoring for full control. 
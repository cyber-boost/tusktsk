# Scaling Directives - Go

## 🎯 What Are Scaling Directives?

Scaling directives (`#scaling`) in TuskLang let you define horizontal and vertical scaling, auto-scaling policies, and resource optimization directly in your config files.

```go
type ScalingConfig struct {
    Horizontal map[string]string `tsk:"#scaling_horizontal"`
    Vertical   map[string]string `tsk:"#scaling_vertical"`
    Policies   map[string]string `tsk:"#scaling_policies"`
}
```

## 🚀 Why Scaling Directives Matter

- Enable dynamic resource allocation
- Support for Kubernetes HPA/VPA, cloud scaling, and custom triggers

## 📋 Scaling Directive Types

- **Horizontal**: Replicas, min/max, CPU/memory triggers
- **Vertical**: Resource requests/limits, auto-tuning
- **Policies**: Custom rules, schedules, event-based scaling

## 🔧 Example
```tsk
scaling_horizontal: #scaling("min:2,max:10,target_cpu:70,target_memory:80")
scaling_vertical: #scaling("min_cpu:100m,max_cpu:2Gi,min_mem:256Mi,max_mem:8Gi")
scaling_policies: #scaling("""
    night -> min:1,max:3,schedule:"0 0 * * *"
    peak -> min:5,max:20,trigger:"load>80"
""")
```

## 🎯 Go Integration
```go
type ScalingConfig struct {
    Horizontal string `tsk:"#scaling_horizontal"`
    Vertical   string `tsk:"#scaling_vertical"`
    Policies   string `tsk:"#scaling_policies"`
}
```

## 🛡️ Best Practices
- Use environment-specific scaling
- Monitor scaling events
- Combine with monitoring/alerting

## ⚡ Summary
Scaling directives make Go apps cloud-native, elastic, and cost-efficient. Integrate with Kubernetes, cloud APIs, or custom orchestrators for full power. 
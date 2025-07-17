# Logging Directives - Go

## 🎯 What Are Logging Directives?

Logging directives (`#logging`) in TuskLang let you define log levels, structured logging, and output configuration in Go projects.

```go
type LoggingConfig struct {
    Levels   map[string]string `tsk:"#logging_levels"`
    Outputs  map[string]string `tsk:"#logging_outputs"`
    Format   map[string]string `tsk:"#logging_format"`
}
```

## 🚀 Why Logging Directives Matter

- Centralize log config
- Enable structured, filterable logs

## 📋 Logging Directive Types

- **Levels**: debug, info, warn, error, fatal
- **Outputs**: stdout, file, syslog, cloud
- **Format**: json, text, custom

## 🔧 Example
```tsk
logging_levels: #logging("default:info,db:warn,api:debug")
logging_outputs: #logging("stdout:true,file:/var/log/app.log")
logging_format: #logging("json:true,text:false")
```

## 🎯 Go Integration
```go
type LoggingConfig struct {
    Levels  string `tsk:"#logging_levels"`
    Outputs string `tsk:"#logging_outputs"`
    Format  string `tsk:"#logging_format"`
}
```

## 🛡️ Best Practices
- Use json for machine parsing
- Separate error logs
- Rotate files

## ⚡ Summary
Logging directives make Go apps observable and production-ready. Integrate with Go log, zap, or logrus for best results. 
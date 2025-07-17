# @file Operator in TuskLang - Go Guide

## 📁 **File Power: @file Operator Mastery**

TuskLang's `@file` operator is your file system rebellion. We don't bow to any king—not even the filesystem. Here's how to use `@file` in Go projects to read, write, and manipulate files directly from your configuration.

## 📋 **Table of Contents**
- [What is @file?](#what-is-file)
- [Basic Usage](#basic-usage)
- [File Operations](#file-operations)
- [Security Considerations](#security-considerations)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 📄 **What is @file?**

The `@file` operator provides direct file system access from your config. Read files, check existence, get file info—all without leaving your configuration.

## 🛠️ **Basic Usage**

```go
[files]
ssl_cert: @file.read("ssl/cert.pem")
config_json: @file.read("config.json")
file_exists: @file.exists("important.txt")
```

## 🔧 **File Operations**

### **Reading Files**
```go
[content]
template: @file.read("templates/email.html")
config: @file.read("config.json")
key: @file.read("ssl/private.key")
```

### **File Information**
```go
[info]
file_size: @file.size("large_file.dat")
file_modified: @file.modified("config.json")
file_exists: @file.exists("required.txt")
```

### **Directory Operations**
```go
[dirs]
file_count: @file.count("logs/")
file_list: @file.list("configs/")
```

## 🔒 **Security Considerations**
- Use `@file.secure` for sensitive files
- Validate file paths to prevent directory traversal
- Set appropriate file permissions
- Never read files with user input

## 🔗 **Go Integration**

```go
sslCert := config.GetString("ssl_cert")
template := config.GetString("template")
fileExists := config.GetBool("file_exists")
```

### **Manual File Operations**
```go
data, err := os.ReadFile("config.json")
if err != nil {
    log.Fatal(err)
}

info, err := os.Stat("file.txt")
if err != nil {
    log.Fatal(err)
}
```

## 🥇 **Best Practices**
- Use absolute paths for critical files
- Validate file content after reading
- Handle file not found errors gracefully
- Use file operations sparingly for performance

---

**TuskLang: File power at your fingertips with @file.** 
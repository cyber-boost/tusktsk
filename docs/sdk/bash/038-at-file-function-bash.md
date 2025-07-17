# 📁 TuskLang Bash @file Function Guide

**"We don't bow to any king" – Files are your configuration's storage.**

The @file function in TuskLang is your file system powerhouse, enabling dynamic file operations, content manipulation, and file-based configuration management directly within your configuration files. Whether you're reading configuration files, writing logs, or managing file-based data, @file provides the flexibility and power to interact with the file system seamlessly.

## 🎯 What is @file?
The @file function provides file system operations in TuskLang. It offers:
- **File reading** - Read file contents with various encodings
- **File writing** - Write content to files with permissions
- **File operations** - Copy, move, delete, and manage files
- **Directory operations** - List, create, and manage directories
- **File metadata** - Get file size, modification time, permissions

## 📝 Basic @file Syntax

### File Reading
```ini
[file_reading]
# Read file contents
config_content: @file.read("/etc/app/config.tsk")
log_content: @file.read("/var/log/app.log")
json_data: @file.read("/var/data/config.json")

# Read with encoding
utf8_content: @file.read("/var/data/utf8.txt", "UTF-8")
binary_content: @file.read("/var/data/binary.dat", "binary")
```

### File Writing
```ini
[file_writing]
# Write content to files
@file.write("/tmp/output.txt", "Hello, TuskLang!")
@file.write("/var/log/app.log", @string.format("Log entry: {time}", {"time": @date("Y-m-d H:i:s")}))

# Write with permissions
@file.write("/etc/app/config.tsk", $config_content, {"mode": "0644"})
@file.write("/var/log/secure.log", $log_entry, {"mode": "0600"})
```

### File Operations
```ini
[file_operations]
# Copy files
@file.copy("/etc/app/config.tsk", "/etc/app/config.tsk.backup")

# Move files
@file.move("/tmp/temp.txt", "/var/data/permanent.txt")

# Delete files
@file.delete("/tmp/old_file.txt")

# Check if file exists
config_exists: @file.exists("/etc/app/config.tsk")
log_exists: @file.exists("/var/log/app.log")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > file-quickstart.tsk << 'EOF'
[file_demo]
# Read and write files
$content: "Hello from TuskLang!\nThis is a test file."
@file.write("/tmp/tusk-test.txt", $content)

read_content: @file.read("/tmp/tusk-test.txt")
file_exists: @file.exists("/tmp/tusk-test.txt")

[file_operations]
# File operations
@file.copy("/tmp/tusk-test.txt", "/tmp/tusk-test-copy.txt")
copy_exists: @file.exists("/tmp/tusk-test-copy.txt")

# File metadata
file_size: @file.size("/tmp/tusk-test.txt")
file_modified: @file.modified("/tmp/tusk-test.txt")
file_permissions: @file.permissions("/tmp/tusk-test.txt")

[directory_operations]
# Directory operations
@file.mkdir("/tmp/tusk-demo")
dir_exists: @file.exists("/tmp/tusk-demo")

files_in_dir: @file.list("/tmp")
EOF

config=$(tusk_parse file-quickstart.tsk)

echo "=== File Demo ==="
echo "Read Content: $(tusk_get "$config" file_demo.read_content)"
echo "File Exists: $(tusk_get "$config" file_demo.file_exists)"

echo ""
echo "=== File Operations ==="
echo "Copy Exists: $(tusk_get "$config" file_operations.copy_exists)"
echo "File Size: $(tusk_get "$config" file_operations.file_size)"
echo "File Modified: $(tusk_get "$config" file_operations.file_modified)"
echo "File Permissions: $(tusk_get "$config" file_operations.file_permissions)"

echo ""
echo "=== Directory Operations ==="
echo "Dir Exists: $(tusk_get "$config" directory_operations.dir_exists)"
echo "Files in /tmp: $(tusk_get "$config" directory_operations.files_in_dir)"
```

## 🔗 Real-World Use Cases

### 1. Configuration File Management
```ini
[config_management]
# Read configuration files
main_config: @file.read("/etc/app/config.tsk")
env_config: @file.read("/etc/app/" + @env("APP_ENV") + ".tsk")

# Merge configurations
merged_config: @object.merge(@object.parse($main_config), @object.parse($env_config))

# Write merged configuration
@file.write("/etc/app/merged-config.tsk", @object.to_string($merged_config))

# Backup configuration
backup_timestamp: @date("Y-m-d-H-i-s")
@file.copy("/etc/app/config.tsk", "/var/backups/config-" + $backup_timestamp + ".tsk")

# Validate configuration file
config_valid: @file.exists("/etc/app/config.tsk") && @file.size("/etc/app/config.tsk") > 0
```

### 2. Log File Management
```ini
[log_management]
# Read and analyze log files
log_content: @file.read("/var/log/app.log")
error_count: @string.count($log_content, "ERROR")
warning_count: @string.count($log_content, "WARNING")

# Rotate log files
current_date: @date("Y-m-d")
@file.move("/var/log/app.log", "/var/log/app-" + $current_date + ".log")
@file.write("/var/log/app.log", "")

# Archive old logs
old_logs: @file.list("/var/log", "app-*.log")
@array.for_each($old_logs, @if(@file.older_than(item, "30d"), @file.delete(item), "keep"))

# Monitor log file size
log_size_mb: @math(@file.size("/var/log/app.log") / 1024 / 1024)
log_rotation_needed: @validate.greater_than($log_size_mb, 100)
```

### 3. Data File Processing
```ini
[data_processing]
# Process CSV files
csv_content: @file.read("/var/data/users.csv")
csv_lines: @array.split($csv_content, "\n")
csv_headers: @array.split(@array.first($csv_lines), ",")

# Process JSON files
json_content: @file.read("/var/data/config.json")
json_data: @object.parse($json_content)

# Process XML files
xml_content: @file.read("/var/data/data.xml")
xml_parsed: @xml.parse($xml_content)

# Write processed data
processed_data: @array.map($csv_lines, "process_csv_line(item)")
@file.write("/var/data/processed.json", @array.to_json($processed_data))
```

### 4. File-Based Caching
```ini
[file_caching]
# Implement file-based caching
$cache_dir: "/var/cache/app"
$cache_key: "user_data"
$cache_file: $cache_dir + "/" + $cache_key + ".cache"

# Check if cache exists and is fresh
cache_exists: @file.exists($cache_file)
cache_fresh: @file.younger_than($cache_file, "1h")

# Read from cache if available
cached_data: @if($cache_exists && $cache_fresh, 
    @file.read($cache_file), 
    "cache_miss"
)

# Write to cache
$new_data: @query("SELECT * FROM users WHERE active = 1")
@file.write($cache_file, @array.to_json($new_data))

# Clean old cache files
@file.mkdir($cache_dir)
old_cache_files: @file.list($cache_dir, "*.cache")
@array.for_each($old_cache_files, @if(@file.older_than(item, "24h"), @file.delete(item), "keep"))
```

## 🧠 Advanced @file Patterns

### File Monitoring and Watchdog
```ini
[file_monitoring]
# Monitor configuration files for changes
$config_files: ["/etc/app/config.tsk", "/etc/app/database.tsk", "/etc/app/api.tsk"]

# Get file modification times
file_mod_times: @array.map($config_files, {
    "file": item,
    "modified": @file.modified(item),
    "size": @file.size(item)
})

# Check for changes
$last_check_file: "/var/cache/last_check.json"
last_check: @if(@file.exists($last_check_file), @object.parse(@file.read($last_check_file)), {})

# Detect changes
changed_files: @array.filter($file_mod_times, 
    @object.get($last_check, item.file) != item.modified
)

# Update last check
@file.write($last_check_file, @object.to_json(@object.merge($last_check, 
    @object.from_array($file_mod_times, "file", "modified")
)))
```

### Secure File Operations
```ini
[secure_operations]
# Secure file operations with permissions
$secure_dir: "/var/secure"
$private_key: @env("PRIVATE_KEY")

# Create secure directory
@file.mkdir($secure_dir, {"mode": "0700"})

# Write sensitive data
@file.write($secure_dir + "/private.key", $private_key, {"mode": "0600"})

# Verify file security
key_file_secure: @file.permissions($secure_dir + "/private.key") == "0600"
dir_secure: @file.permissions($secure_dir) == "0700"

# Secure file deletion
@file.secure_delete($secure_dir + "/temp.key")
```

### File Backup and Recovery
```ini
[backup_recovery]
# Automated backup system
$backup_dir: "/var/backups"
$backup_files: ["/etc/app/config.tsk", "/var/data/database.sql", "/var/log/app.log"]

# Create backup
backup_timestamp: @date("Y-m-d-H-i-s")
@file.mkdir($backup_dir + "/" + $backup_timestamp)

# Backup each file
@array.for_each($backup_files, {
    "source": item,
    "dest": $backup_dir + "/" + $backup_timestamp + "/" + @file.basename(item)
}, @file.copy(item.source, item.dest))

# Clean old backups (keep last 7 days)
old_backups: @file.list($backup_dir, "*")
@array.for_each($old_backups, @if(@file.older_than(item, "7d"), @file.delete(item), "keep"))

# Recovery function
$recovery_timestamp: @env("RECOVERY_TIMESTAMP")
recovery_files: @if(@file.exists($backup_dir + "/" + $recovery_timestamp), 
    @file.list($backup_dir + "/" + $recovery_timestamp),
    []
)
```

## 🛡️ Security & Performance Notes
- **File permissions:** Always set appropriate file permissions for sensitive data
- **Path validation:** Validate file paths to prevent directory traversal attacks
- **Disk space:** Monitor disk usage when writing large files
- **File locking:** Use file locking for concurrent access to shared files
- **Backup strategy:** Implement regular backups for critical files
- **Error handling:** Handle file operation errors gracefully

## 🐞 Troubleshooting
- **Permission errors:** Check file and directory permissions
- **Disk space issues:** Monitor available disk space
- **File locking:** Handle concurrent file access
- **Encoding problems:** Specify correct file encoding for international content
- **Path issues:** Use absolute paths and validate file paths

## 💡 Best Practices
- **Use absolute paths:** Avoid relative paths for critical operations
- **Set appropriate permissions:** Use restrictive permissions for sensitive files
- **Handle errors:** Implement proper error handling for file operations
- **Monitor disk usage:** Track disk space usage
- **Backup regularly:** Implement automated backup strategies
- **Validate file content:** Verify file integrity after operations

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@string Function](030-at-string-function-bash.md)
- [@cache Function](033-at-cache-function-bash.md)
- [Error Handling](062-error-handling-bash.md)
- [Security Best Practices](099-security-best-practices-bash.md)

---

**Master @file in TuskLang and manage your file system with power and precision. 📁** 
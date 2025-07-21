# 🐚 TuskLang Bash @shell Function Guide

**"We don't bow to any king" – Shell is your configuration's command center.**

The @shell function in TuskLang is your system integration powerhouse, enabling direct shell command execution, system administration, and process management directly within your configuration files. Whether you're managing system resources, executing scripts, or automating system tasks, @shell provides the direct access and control to interact with your operating system.

## 🎯 What is @shell?
The @shell function provides shell command execution in TuskLang. It offers:
- **Command execution** - Execute shell commands and capture output
- **System administration** - Manage system resources and processes
- **Process control** - Start, stop, and monitor system processes
- **File operations** - Perform file system operations via shell
- **System monitoring** - Get real-time system information

## 📝 Basic @shell Syntax

### Simple Command Execution
```ini
[simple_commands]
# Execute basic shell commands
current_user: @shell("whoami")
hostname: @shell("hostname")
current_directory: @shell("pwd")
system_uptime: @shell("uptime")
```

### Command with Arguments
```ini
[command_arguments]
# Commands with arguments
file_list: @shell("ls -la /var/log")
process_count: @shell("ps aux | wc -l")
memory_info: @shell("free -h")
disk_usage: @shell("df -h /")
```

### Capturing Output
```ini
[output_capture]
# Capture and process command output
cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")
load_average: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > shell-quickstart.tsk << 'EOF'
[system_info]
# Basic system information
hostname: @shell("hostname")
os_info: @shell("uname -a")
current_user: @shell("whoami")
current_dir: @shell("pwd")

[system_metrics]
# System performance metrics
cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")
disk_usage: @shell("df -h / | awk 'NR==2{print $5}' | cut -d'%' -f1")
load_average: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")

[process_info]
# Process information
total_processes: @shell("ps aux | wc -l")
running_processes: @shell("ps aux | grep -v grep | grep -c '^[^ ]*'")
system_uptime: @shell("uptime -p")

[network_info]
# Network information
network_interfaces: @shell("ip addr show | grep '^[0-9]' | awk '{print $2}' | cut -d':' -f1")
active_connections: @shell("netstat -an | wc -l")
listening_ports: @shell("netstat -tlnp | grep LISTEN | wc -l")
EOF

config=$(tusk_parse shell-quickstart.tsk)

echo "=== System Info ==="
echo "Hostname: $(tusk_get "$config" system_info.hostname)"
echo "OS Info: $(tusk_get "$config" system_info.os_info)"
echo "Current User: $(tusk_get "$config" system_info.current_user)"
echo "Current Directory: $(tusk_get "$config" system_info.current_dir)"

echo ""
echo "=== System Metrics ==="
echo "CPU Usage: $(tusk_get "$config" system_metrics.cpu_usage)%"
echo "Memory Usage: $(tusk_get "$config" system_metrics.memory_usage)%"
echo "Disk Usage: $(tusk_get "$config" system_metrics.disk_usage)%"
echo "Load Average: $(tusk_get "$config" system_metrics.load_average)"

echo ""
echo "=== Process Info ==="
echo "Total Processes: $(tusk_get "$config" process_info.total_processes)"
echo "Running Processes: $(tusk_get "$config" process_info.running_processes)"
echo "System Uptime: $(tusk_get "$config" process_info.system_uptime)"

echo ""
echo "=== Network Info ==="
echo "Network Interfaces: $(tusk_get "$config" network_info.network_interfaces)"
echo "Active Connections: $(tusk_get "$config" network_info.active_connections)"
echo "Listening Ports: $(tusk_get "$config" network_info.listening_ports)"
```

## 🔗 Real-World Use Cases

### 1. System Monitoring and Health Checks
```ini
[system_monitoring]
# Comprehensive system monitoring
$system_health: {
    "cpu_usage": @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1"),
    "memory_usage": @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'"),
    "disk_usage": @shell("df -h / | awk 'NR==2{print $5}' | cut -d'%' -f1"),
    "load_average": @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'"),
    "swap_usage": @shell("free | grep Swap | awk '{printf \"%.2f\", $3/$2 * 100.0}'")
}

# Health score calculation
health_score: @math(100 - ($system_health.cpu_usage * 0.3 + $system_health.memory_usage * 0.3 + $system_health.disk_usage * 0.2 + $system_health.swap_usage * 0.2))

# System status
system_status: @if($health_score >= 90, "excellent", @if($health_score >= 70, "good", @if($health_score >= 50, "fair", "poor")))

# Alert conditions
critical_alerts: {
    "high_cpu": @validate.greater_than($system_health.cpu_usage, 90),
    "high_memory": @validate.greater_than($system_health.memory_usage, 95),
    "high_disk": @validate.greater_than($system_health.disk_usage, 95),
    "high_load": @validate.greater_than($system_health.load_average, 5)
}
```

### 2. Process Management
```ini
[process_management]
# Process monitoring and management
$process_info: {
    "total_processes": @shell("ps aux | wc -l"),
    "user_processes": @shell("ps -u $(whoami) | wc -l"),
    "system_processes": @shell("ps -e | wc -l"),
    "zombie_processes": @shell("ps aux | grep -c 'Z'")
}

# Service status checks
$services: ["nginx", "mysql", "redis", "apache2"]
$service_status: @array.map($services, {
    "service": item,
    "status": @shell("systemctl is-active " + item),
    "enabled": @shell("systemctl is-enabled " + item)
})

# Process monitoring
critical_processes: ["sshd", "systemd", "init"]
process_health: @array.map($critical_processes, {
    "process": item,
    "running": @shell("pgrep -c " + item),
    "status": @if(@shell("pgrep -c " + item) > 0, "running", "stopped")
})
```

### 3. File System Operations
```ini
[filesystem_operations]
# File system monitoring and management
$filesystem_info: {
    "disk_usage": @shell("df -h / | awk 'NR==2{print $5}' | cut -d'%' -f1"),
    "inode_usage": @shell("df -i / | awk 'NR==2{print $5}' | cut -d'%' -f1"),
    "largest_files": @shell("find /var/log -type f -exec ls -lh {} + | sort -k5 -hr | head -5"),
    "oldest_files": @shell("find /var/log -type f -printf '%T+ %p\n' | sort | head -5")
}

# Log file management
log_files: @shell("find /var/log -name '*.log' -type f")
log_sizes: @shell("find /var/log -name '*.log' -type f -exec ls -lh {} + | awk '{print $5, $9}'")

# Backup operations
backup_status: @shell("ls -la /var/backups/ | wc -l")
latest_backup: @shell("ls -t /var/backups/ | head -1")
backup_size: @shell("du -sh /var/backups/ | cut -f1")
```

### 4. Network and Security Monitoring
```ini
[network_monitoring]
# Network monitoring and security
$network_info: {
    "active_connections": @shell("netstat -an | wc -l"),
    "listening_ports": @shell("netstat -tlnp | grep LISTEN | wc -l"),
    "established_connections": @shell("netstat -an | grep ESTABLISHED | wc -l"),
    "foreign_connections": @shell("netstat -an | grep -v '127.0.0.1' | grep ESTABLISHED | wc -l")
}

# Security monitoring
$security_checks: {
    "failed_logins": @shell("grep 'Failed password' /var/log/auth.log | wc -l"),
    "successful_logins": @shell("grep 'Accepted password' /var/log/auth.log | wc -l"),
    "sudo_usage": @shell("grep 'sudo:' /var/log/auth.log | wc -l"),
    "ssh_connections": @shell("netstat -an | grep :22 | grep ESTABLISHED | wc -l")
}

# Network interface monitoring
interfaces: @shell("ip addr show | grep '^[0-9]' | awk '{print $2}' | cut -d':' -f1")
interface_status: @array.map($interfaces, {
    "interface": item,
    "status": @shell("ip link show " + item + " | grep -c UP"),
    "ip_address": @shell("ip addr show " + item + " | grep 'inet ' | awk '{print $2}' | cut -d'/' -f1")
})
```

## 🧠 Advanced @shell Patterns

### Automated System Maintenance
```ini
[system_maintenance]
# Automated system maintenance tasks
$maintenance_tasks: {
    "update_package_list": @shell("apt update -qq"),
    "upgrade_packages": @shell("apt upgrade -y -qq"),
    "clean_package_cache": @shell("apt clean -qq"),
    "remove_old_kernels": @shell("apt autoremove -y -qq"),
    "update_logrotate": @shell("logrotate -f /etc/logrotate.conf")
}

# Disk cleanup
$cleanup_tasks: {
    "clean_temp_files": @shell("find /tmp -type f -atime +7 -delete"),
    "clean_log_files": @shell("find /var/log -name '*.log.*' -type f -mtime +30 -delete"),
    "clean_old_backups": @shell("find /var/backups -type f -mtime +90 -delete")
}

# System optimization
$optimization_tasks: {
    "defrag_filesystem": @shell("e4defrag /"),
    "update_mlocate": @shell("updatedb"),
    "restart_services": @shell("systemctl restart nginx mysql redis")
}
```

### Real-Time Monitoring Scripts
```ini
[real_time_monitoring]
# Real-time monitoring with continuous updates
$monitoring_script: """
#!/bin/bash
while true; do
    echo "=== System Status $(date) ==="
    echo "CPU: $(top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1)%"
    echo "Memory: $(free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}')%"
    echo "Disk: $(df -h / | awk 'NR==2{print $5}' | cut -d'%' -f1)%"
    echo "Load: $(uptime | awk -F'load average:' '{print $2}' | awk '{print $1}')"
    sleep 30
done
"""

# Start monitoring
@shell("chmod +x /tmp/monitor.sh")
monitoring_pid: @shell("nohup /tmp/monitor.sh > /var/log/system_monitor.log 2>&1 & echo $!")
```

### Advanced Process Control
```ini
[process_control]
# Advanced process management
$process_control: {
    "start_service": @shell("systemctl start nginx"),
    "stop_service": @shell("systemctl stop nginx"),
    "restart_service": @shell("systemctl restart nginx"),
    "check_service": @shell("systemctl is-active nginx")
}

# Process monitoring with alerts
critical_processes: ["nginx", "mysql", "redis"]
process_monitoring: @array.map($critical_processes, {
    "process": item,
    "pid": @shell("pgrep " + item),
    "memory_usage": @shell("ps -p $(pgrep " + item + ") -o %mem --no-headers"),
    "cpu_usage": @shell("ps -p $(pgrep " + item + ") -o %cpu --no-headers"),
    "uptime": @shell("ps -p $(pgrep " + item + ") -o etime --no-headers")
})

# Automatic process recovery
@array.for_each($process_monitoring, @if(item.pid == "", {
    "action": "restart_process",
    "process": item.process,
    "command": @shell("systemctl restart " + item.process),
    "timestamp": @date("Y-m-d H:i:s")
}, "process_running"))
```

## 🛡️ Security & Performance Notes
- **Command injection:** Never execute user-provided commands without validation
- **Privilege escalation:** Be careful with commands that require elevated privileges
- **Resource usage:** Monitor system resources when executing intensive commands
- **Error handling:** Implement proper error handling for failed commands
- **Security context:** Run commands in appropriate security context
- **Command sanitization:** Sanitize all command inputs to prevent injection attacks

## 🐞 Troubleshooting
- **Permission denied:** Check user permissions for command execution
- **Command not found:** Ensure required commands are installed and in PATH
- **Timeout issues:** Set appropriate timeouts for long-running commands
- **Output parsing:** Handle command output parsing errors gracefully
- **Resource limits:** Monitor system resources during command execution

## 💡 Best Practices
- **Validate commands:** Always validate commands before execution
- **Use absolute paths:** Use absolute paths for critical system commands
- **Handle errors:** Implement comprehensive error handling
- **Monitor resources:** Monitor system resources during command execution
- **Security first:** Never execute untrusted commands
- **Document commands:** Document complex command sequences

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@metrics Function](040-at-metrics-function-bash.md)
- [Error Handling](062-error-handling-bash.md)
- [Security Best Practices](099-security-best-practices-bash.md)
- [System Administration](097-system-administration-bash.md)

---

**Master @shell in TuskLang and take direct control of your system operations. 🐚** 
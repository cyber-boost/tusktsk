# 🐚 TuskLang Bash @shell Function Guide

**"We don't bow to any king" – Shell commands are your system's voice.**

The @shell function in TuskLang is your bridge to the operating system, allowing you to execute shell commands and integrate their output directly into your configuration. Whether you're gathering system information, performing file operations, or running custom scripts, @shell provides the power to make your TuskLang configurations truly dynamic and system-aware.

## 🎯 What is @shell?
The @shell function executes shell commands and returns their output. It provides:
- **System integration** - Access to OS commands and utilities
- **Real-time data** - Dynamic system information and metrics
- **Automation** - Execute custom scripts and commands
- **Flexibility** - Any shell command can be executed
- **Integration** - Seamless integration with TuskLang configuration

## 📝 Basic @shell Syntax

### Simple Command Execution
```ini
[basic]
# Execute basic shell commands
hostname: @shell("hostname")
current_time: @shell("date '+%Y-%m-%d %H:%M:%S'")
user: @shell("whoami")
```

### Command with Arguments
```ini
[with_args]
# Commands with arguments and options
disk_usage: @shell("df -h / | tail -1 | awk '{print $5}'")
memory_usage: @shell("free -m | awk 'NR==2{printf \"%.1f%%\", $3*100/$2}'")
process_count: @shell("ps aux | wc -l")
```

### Complex Commands
```ini
[complex]
# Multi-command pipelines
system_info: @shell("uname -a")
uptime_info: @shell("uptime -p")
load_average: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > shell-quickstart.tsk << 'EOF'
[system_info]
hostname: @shell("hostname")
operating_system: @shell("uname -s")
kernel_version: @shell("uname -r")
architecture: @shell("uname -m")

[system_metrics]
cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage: @shell("free | awk 'NR==2{printf \"%.1f\", $3*100/$2}'")
disk_usage: @shell("df / | awk 'NR==2{print $5}' | sed 's/%//'")
load_average: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")

[network_info]
ip_address: @shell("hostname -I | awk '{print $1}'")
external_ip: @shell("curl -s ifconfig.me 2>/dev/null || echo 'unavailable'")
network_interfaces: @shell("ip link show | grep '^[0-9]' | awk -F': ' '{print $2}' | tr '\n' ','")

[service_status]
nginx_status: @shell("systemctl is-active nginx 2>/dev/null || echo 'not_installed'")
postgres_status: @shell("systemctl is-active postgresql 2>/dev/null || echo 'not_installed'")
redis_status: @shell("systemctl is-active redis 2>/dev/null || echo 'not_installed'")
EOF

config=$(tusk_parse shell-quickstart.tsk)

echo "=== System Information ==="
echo "Hostname: $(tusk_get "$config" system_info.hostname)"
echo "OS: $(tusk_get "$config" system_info.operating_system)"
echo "Kernel: $(tusk_get "$config" system_info.kernel_version)"
echo "Architecture: $(tusk_get "$config" system_info.architecture)"

echo ""
echo "=== System Metrics ==="
echo "CPU Usage: $(tusk_get "$config" system_metrics.cpu_usage)%"
echo "Memory Usage: $(tusk_get "$config" system_metrics.memory_usage)%"
echo "Disk Usage: $(tusk_get "$config" system_metrics.disk_usage)%"
echo "Load Average: $(tusk_get "$config" system_metrics.load_average)"

echo ""
echo "=== Network Information ==="
echo "IP Address: $(tusk_get "$config" network_info.ip_address)"
echo "External IP: $(tusk_get "$config" network_info.external_ip)"
echo "Network Interfaces: $(tusk_get "$config" network_info.network_interfaces)"

echo ""
echo "=== Service Status ==="
echo "Nginx: $(tusk_get "$config" service_status.nginx_status)"
echo "PostgreSQL: $(tusk_get "$config" service_status.postgres_status)"
echo "Redis: $(tusk_get "$config" service_status.redis_status)"
```

## 🔗 Real-World Use Cases

### 1. System Monitoring and Health Checks
```ini
[monitoring]
# Real-time system health metrics
cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage: @shell("free | awk 'NR==2{printf \"%.1f\", $3*100/$2}'")
disk_usage: @shell("df / | awk 'NR==2{print $5}' | sed 's/%//'")
load_average: @shell("uptime | awk -F'load average:' '{print $2}' | awk '{print $1}'")

# Service health checks
database_status: @shell("pg_isready -h localhost -p 5432 >/dev/null 2>&1 && echo 'healthy' || echo 'unhealthy'")
redis_status: @shell("redis-cli ping >/dev/null 2>&1 && echo 'healthy' || echo 'unhealthy'")
nginx_status: @shell("systemctl is-active nginx >/dev/null 2>&1 && echo 'active' || echo 'inactive'")

# Process monitoring
nginx_processes: @shell("pgrep -c nginx")
postgres_processes: @shell("pgrep -c postgres")
total_processes: @shell("ps aux | wc -l")
```

### 2. File System Operations
```ini
[filesystem]
# File and directory information
log_files: @shell("ls /var/log/*.log 2>/dev/null | wc -l")
log_directory_size: @shell("du -sh /var/log 2>/dev/null | awk '{print $1}'")
config_files: @shell("find /etc -name '*.conf' 2>/dev/null | wc -l")

# File permissions and ownership
config_permissions: @shell("ls -la /etc/nginx/nginx.conf 2>/dev/null | awk '{print $1}'")
config_owner: @shell("ls -la /etc/nginx/nginx.conf 2>/dev/null | awk '{print $3}'")

# Disk space monitoring
available_space: @shell("df -h / | awk 'NR==2{print $4}'")
largest_files: @shell("find /var/log -type f -exec du -h {} + 2>/dev/null | sort -hr | head -5 | awk '{print $2}'")
```

### 3. Network and Connectivity
```ini
[network]
# Network interface information
ip_address: @shell("hostname -I | awk '{print $1}'")
external_ip: @shell("curl -s ifconfig.me 2>/dev/null || echo 'unavailable'")
network_interfaces: @shell("ip link show | grep '^[0-9]' | awk -F': ' '{print $2}' | tr '\n' ','")

# Connectivity tests
internet_connectivity: @shell("ping -c 1 8.8.8.8 >/dev/null 2>&1 && echo 'connected' || echo 'disconnected'")
dns_resolution: @shell("nslookup google.com >/dev/null 2>&1 && echo 'working' || echo 'failed'")

# Port availability
port_80_status: @shell("netstat -tlnp | grep ':80 ' >/dev/null 2>&1 && echo 'in_use' || echo 'available'")
port_443_status: @shell("netstat -tlnp | grep ':443 ' >/dev/null 2>&1 && echo 'in_use' || echo 'available'")
```

### 4. Application Deployment and Management
```ini
[deployment]
# Application version and status
app_version: @shell("cat /var/www/app/version.txt 2>/dev/null || echo 'unknown'")
app_processes: @shell("pgrep -c app_process")
app_memory_usage: @shell("ps aux | grep app_process | grep -v grep | awk '{sum+=$6} END {print sum/1024 \"MB\"}'")

# Git repository status
git_branch: @shell("cd /var/www/app && git branch --show-current 2>/dev/null || echo 'not_git_repo'")
git_commit: @shell("cd /var/www/app && git rev-parse --short HEAD 2>/dev/null || echo 'unknown'")
git_status: @shell("cd /var/www/app && git status --porcelain 2>/dev/null | wc -l")

# Backup status
last_backup: @shell("ls -la /var/backups/ | tail -1 | awk '{print $6, $7}' 2>/dev/null || echo 'no_backups'")
backup_size: @shell("du -sh /var/backups/ 2>/dev/null | awk '{print $1}' || echo '0'")
```

## 🧠 Advanced @shell Patterns

### Error Handling and Fallbacks
```bash
#!/bin/bash
source tusk-bash.sh

cat > shell-error-handling.tsk << 'EOF'
[robust_shell]
# Commands with error handling and fallbacks
hostname: @shell("hostname 2>/dev/null || echo 'unknown'")
cpu_cores: @shell("nproc 2>/dev/null || echo '1'")
memory_gb: @shell("free -g | awk 'NR==2{print $2}' 2>/dev/null || echo '1'")

# Safe file operations
config_exists: @shell("test -f /etc/app/config.json && echo 'true' || echo 'false'")
log_file_size: @shell("wc -c < /var/log/app.log 2>/dev/null || echo '0'")

# Conditional command execution
service_status: @shell("if systemctl is-active nginx >/dev/null 2>&1; then echo 'active'; else echo 'inactive'; fi")
EOF

config=$(tusk_parse shell-error-handling.tsk)
echo "Hostname: $(tusk_get "$config" robust_shell.hostname)"
echo "CPU Cores: $(tusk_get "$config" robust_shell.cpu_cores)"
echo "Memory GB: $(tusk_get "$config" robust_shell.memory_gb)"
echo "Config Exists: $(tusk_get "$config" robust_shell.config_exists)"
echo "Service Status: $(tusk_get "$config" robust_shell.service_status)"
```

### Complex Command Chaining
```ini
[complex_commands]
# Multi-step operations
system_summary: @shell("echo 'Hostname: ' && hostname && echo 'Uptime: ' && uptime -p && echo 'Load: ' && uptime | awk -F'load average:' '{print $2}'")

# Data processing pipelines
top_processes: @shell("ps aux --sort=-%cpu | head -6 | awk '{print $2, $3, $11}' | tr '\n' ';'")

# Conditional logic in shell
environment_check: @shell("if [ -f /etc/environment ]; then echo 'environment_file_exists'; else echo 'no_environment_file'; fi")

# File content analysis
log_errors: @shell("grep -i error /var/log/app.log 2>/dev/null | wc -l || echo '0'")
recent_logs: @shell("tail -10 /var/log/app.log 2>/dev/null | tr '\n' ' ' || echo 'no_logs'")
```

### Performance Monitoring
```bash
#!/bin/bash
source tusk-bash.sh

cat > performance-monitoring.tsk << 'EOF'
[performance]
# CPU and memory metrics
cpu_usage_percent: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage_percent: @shell("free | awk 'NR==2{printf \"%.1f\", $3*100/$2}'")
swap_usage_percent: @shell("free | awk 'NR==3{printf \"%.1f\", $3*100/$2}'")

# Disk I/O
disk_read_mb: @shell("iostat -d | awk 'NR==4{print $3}' 2>/dev/null || echo '0'")
disk_write_mb: @shell("iostat -d | awk 'NR==4{print $4}' 2>/dev/null || echo '0'")

# Network I/O
network_rx_mb: @shell("cat /proc/net/dev | grep eth0 | awk '{print $2/1024/1024}' 2>/dev/null || echo '0'")
network_tx_mb: @shell("cat /proc/net/dev | grep eth0 | awk '{print $10/1024/1024}' 2>/dev/null || echo '0'")

# Process metrics
total_processes: @shell("ps aux | wc -l")
running_processes: @shell("ps aux | grep -v '^USER' | grep -v grep | wc -l")
EOF

config=$(tusk_parse performance-monitoring.tsk)
echo "CPU Usage: $(tusk_get "$config" performance.cpu_usage_percent)%"
echo "Memory Usage: $(tusk_get "$config" performance.memory_usage_percent)%"
echo "Swap Usage: $(tusk_get "$config" performance.swap_usage_percent)%"
echo "Total Processes: $(tusk_get "$config" performance.total_processes)"
echo "Running Processes: $(tusk_get "$config" performance.running_processes)"
```

## 🛡️ Security & Performance Notes
- **Command injection:** Always validate and sanitize inputs before using them in @shell commands
- **Permission issues:** Ensure proper permissions for executing shell commands
- **Resource usage:** Be careful with commands that consume significant resources
- **Timeout handling:** Some commands may hang; consider using timeouts
- **Error handling:** Always provide fallbacks for commands that may fail

## 🐞 Troubleshooting
- **Permission denied:** Check if the user has permission to execute the command
- **Command not found:** Ensure the command is available in the system PATH
- **Hanging commands:** Use timeouts or check for infinite loops
- **Output parsing:** Be careful with command output that may contain special characters
- **Environment issues:** Ensure the shell environment is properly set up

## 💡 Best Practices
- **Validate inputs:** Always validate inputs before using them in shell commands
- **Use fallbacks:** Provide fallback values for commands that may fail
- **Error handling:** Implement proper error handling for all shell operations
- **Security first:** Never execute user-provided input directly in shell commands
- **Performance:** Cache expensive shell operations when possible
- **Documentation:** Document the expected output format of shell commands

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@env Operator](025-at-env-function-bash.md)
- [Error Handling](022-error-handling-bash.md)
- [Security](105-security-bash.md)

---

**Master @shell in TuskLang and integrate your configuration with the power of the operating system. 🐚** 
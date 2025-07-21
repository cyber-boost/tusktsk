# Cron Directives in TuskLang - Bash Guide

## ⏰ **Revolutionary Scheduled Task Configuration**

Cron directives in TuskLang transform your configuration files into intelligent task schedulers. No more separate cron files or complex scheduling configurations - everything lives in your TuskLang configuration with dynamic scheduling, automatic task management, and intelligent error handling.

> **"We don't bow to any king"** - TuskLang cron directives break free from traditional cron constraints and bring modern scheduling capabilities to your Bash applications.

## 🚀 **Core Cron Directives**

### **Basic Cron Setup**
```bash
#cron: "0 */6 * * *"         # Schedule task every 6 hours
#cron-name: backup-task       # Task name for identification
#cron-description: Database backup # Task description
#cron-command: backup.sh      # Command to execute
#cron-user: www-data          # User to run task as
#cron-log: /var/log/cron.log  # Log file location
```

### **Advanced Cron Configuration**
```bash
#cron-enabled: true           # Enable/disable task
#cron-timeout: 300            # Task timeout in seconds
#cron-retries: 3              # Number of retry attempts
#cron-notify: admin@example.com # Email notifications
#cron-environment: production # Environment-specific settings
#cron-dependencies: ["db", "api"] # Task dependencies
```

## 🔧 **Bash Cron Implementation**

### **Basic Cron Manager**
```bash
#!/bin/bash

# Load cron configuration
source <(tsk load cron.tsk)

# Cron configuration
CRON_ENABLED="${cron_enabled:-true}"
CRON_LOG_FILE="${cron_log_file:-/var/log/tusk-cron.log}"
CRON_TIMEOUT="${cron_timeout:-300}"
CRON_RETRIES="${cron_retries:-3}"
CRON_NOTIFY_EMAIL="${cron_notify_email:-}"

# Cron manager
manage_cron_tasks() {
    local action="$1"
    local task_name="$2"
    
    case "$action" in
        "install")
            install_cron_tasks
            ;;
        "uninstall")
            uninstall_cron_tasks
            ;;
        "list")
            list_cron_tasks
            ;;
        "enable")
            enable_cron_task "$task_name"
            ;;
        "disable")
            disable_cron_task "$task_name"
            ;;
        "run")
            run_cron_task "$task_name"
            ;;
        "status")
            check_cron_status
            ;;
        *)
            echo "Unknown action: $action"
            show_cron_help
            ;;
    esac
}

# Install cron tasks
install_cron_tasks() {
    echo "Installing TuskLang cron tasks..."
    
    # Load all cron configurations
    local cron_tasks=($(find . -name "*.tsk" -exec grep -l "^#cron:" {} \;))
    
    for config_file in "${cron_tasks[@]}"; do
        echo "Processing cron configuration: $config_file"
        install_cron_from_config "$config_file"
    done
    
    echo "Cron tasks installation completed"
}

install_cron_from_config() {
    local config_file="$1"
    
    # Load configuration
    source <(tsk load "$config_file")
    
    # Extract cron information
    local cron_schedule=""
    local cron_name=""
    local cron_command=""
    local cron_user=""
    local cron_log=""
    
    # Parse cron directive
    while IFS= read -r line; do
        case "$line" in
            \#cron:*)
                cron_schedule="${line#\#cron: }"
                ;;
            \#cron-name:*)
                cron_name="${line#\#cron-name: }"
                ;;
            \#cron-command:*)
                cron_command="${line#\#cron-command: }"
                ;;
            \#cron-user:*)
                cron_user="${line#\#cron-user: }"
                ;;
            \#cron-log:*)
                cron_log="${line#\#cron-log: }"
                ;;
        esac
    done < "$config_file"
    
    # Validate required fields
    if [[ -z "$cron_schedule" ]] || [[ -z "$cron_command" ]]; then
        echo "Warning: Invalid cron configuration in $config_file"
        return 1
    fi
    
    # Set defaults
    cron_name="${cron_name:-$(basename "$config_file" .tsk)}"
    cron_user="${cron_user:-$(whoami)}"
    cron_log="${cron_log:-$CRON_LOG_FILE}"
    
    # Create cron entry
    create_cron_entry "$cron_schedule" "$cron_name" "$cron_command" "$cron_user" "$cron_log"
}

create_cron_entry() {
    local schedule="$1"
    local name="$2"
    local command="$3"
    local user="$4"
    local log_file="$5"
    
    # Validate cron schedule
    if ! validate_cron_schedule "$schedule"; then
        echo "Error: Invalid cron schedule: $schedule"
        return 1
    fi
    
    # Create full command with logging and error handling
    local full_command=$(build_cron_command "$command" "$name" "$log_file")
    
    # Add to crontab
    local cron_entry="$schedule $full_command"
    
    if [[ "$user" == "$(whoami)" ]]; then
        # Add to current user's crontab
        (crontab -l 2>/dev/null; echo "$cron_entry") | crontab -
        echo "Added cron task '$name' to current user's crontab"
    else
        # Add to specified user's crontab (requires sudo)
        if command -v sudo >/dev/null 2>&1; then
            sudo -u "$user" bash -c "(crontab -l 2>/dev/null; echo \"$cron_entry\") | crontab -"
            echo "Added cron task '$name' to $user's crontab"
        else
            echo "Error: Cannot add cron task for user $user (sudo not available)"
            return 1
        fi
    fi
}

validate_cron_schedule() {
    local schedule="$1"
    
    # Basic cron format validation: minute hour day month weekday
    if [[ "$schedule" =~ ^([0-9*/,-\s]+)\s+([0-9*/,-\s]+)\s+([0-9*/,-\s]+)\s+([0-9*/,-\s]+)\s+([0-9*/,-\s]+)$ ]]; then
        return 0
    fi
    
    return 1
}

build_cron_command() {
    local command="$1"
    local name="$2"
    local log_file="$3"
    
    # Create wrapper script for better error handling and logging
    local wrapper_script="/tmp/tusk-cron-$name.sh"
    
    cat > "$wrapper_script" << EOF
#!/bin/bash

# TuskLang Cron Task Wrapper
# Task: $name
# Generated: $(date)

set -e

# Set up logging
exec 1> >(tee -a "$log_file")
exec 2> >(tee -a "$log_file" >&2)

echo "\$(date): Starting cron task: $name"

# Set timeout
timeout $CRON_TIMEOUT bash -c "$command"

if [[ \$? -eq 0 ]]; then
    echo "\$(date): Cron task '$name' completed successfully"
else
    echo "\$(date): Cron task '$name' failed with exit code \$?"
    
    # Send notification if configured
    if [[ -n "$CRON_NOTIFY_EMAIL" ]]; then
        echo "Cron task '$name' failed at \$(date)" | mail -s "Cron Task Failure: $name" "$CRON_NOTIFY_EMAIL"
    fi
fi
EOF

    chmod +x "$wrapper_script"
    echo "$wrapper_script"
}
```

### **Advanced Task Management**
```bash
#!/bin/bash

# Advanced cron task management
class CronTaskManager {
    constructor() {
        this.tasks = {}
        this.log_file = "${CRON_LOG_FILE:-/var/log/tusk-cron.log}"
        this.timeout = "${CRON_TIMEOUT:-300}"
        this.retries = "${CRON_RETRIES:-3}"
    }
    
    addTask(name, schedule, command, options = {}) {
        this.tasks[name] = {
            schedule: schedule,
            command: command,
            enabled: options.enabled !== false,
            user: options.user || "$(whoami)",
            log_file: options.log_file || this.log_file,
            timeout: options.timeout || this.timeout,
            retries: options.retries || this.retries,
            dependencies: options.dependencies || [],
            notify_email: options.notify_email || "$CRON_NOTIFY_EMAIL",
            environment: options.environment || "production"
        }
    }
    
    installAll() {
        for (const [name, task] of Object.entries(this.tasks)) {
            if (task.enabled) {
                this.installTask(name, task)
            }
        }
    }
    
    installTask(name, task) {
        // Check dependencies
        if (!this.checkDependencies(task.dependencies)) {
            console.log(`Skipping task '${name}' - dependencies not met`)
            return
        }
        
        // Create cron entry
        const cronEntry = this.buildCronEntry(name, task)
        this.addToCrontab(cronEntry, task.user)
        
        console.log(`Installed cron task: ${name}`)
    }
    
    checkDependencies(dependencies) {
        for (const dep of dependencies) {
            if (!this.checkDependency(dep)) {
                return false
            }
        }
        return true
    }
    
    checkDependency(dependency) {
        // Check if service is running
        if (dependency.startsWith("service:")) {
            const service = dependency.split(":")[1]
            return this.isServiceRunning(service)
        }
        
        // Check if file exists
        if (dependency.startsWith("file:")) {
            const file = dependency.split(":")[1]
            return this.fileExists(file)
        }
        
        // Check if port is open
        if (dependency.startsWith("port:")) {
            const port = dependency.split(":")[1]
            return this.isPortOpen(port)
        }
        
        return true
    }
    
    buildCronEntry(name, task) {
        const wrapperScript = this.createWrapperScript(name, task)
        return `${task.schedule} ${wrapperScript}`
    }
    
    createWrapperScript(name, task) {
        const scriptPath = `/tmp/tusk-cron-${name}.sh`
        
        const script = `#!/bin/bash
# TuskLang Cron Task: ${name}
# Environment: ${task.environment}
# Generated: $(date)

set -e

# Set up logging
exec 1> >(tee -a "${task.log_file}")
exec 2> >(tee -a "${task.log_file}" >&2)

echo "\$(date): Starting cron task: ${name}"

# Set environment
export TUSK_ENV="${task.environment}"

# Retry logic
for attempt in \$(seq 1 ${task.retries}); do
    echo "\$(date): Attempt \$attempt of ${task.retries}"
    
    if timeout ${task.timeout} bash -c "${task.command}"; then
        echo "\$(date): Cron task '${name}' completed successfully"
        exit 0
    else
        echo "\$(date): Attempt \$attempt failed"
        if [[ \$attempt -lt ${task.retries} ]]; then
            sleep 5
        fi
    fi
done

echo "\$(date): Cron task '${name}' failed after ${task.retries} attempts"

# Send notification
if [[ -n "${task.notify_email}" ]]; then
    echo "Cron task '${name}' failed at \$(date)" | mail -s "Cron Task Failure: ${name}" "${task.notify_email}"
fi

exit 1`
        
        echo "$script" > "$scriptPath"
        chmod +x "$scriptPath"
        echo "$scriptPath"
    }
    
    addToCrontab(cronEntry, user) {
        if [[ "$user" == "$(whoami)" ]]; then
            (crontab -l 2>/dev/null; echo "$cronEntry") | crontab -
        else
            sudo -u "$user" bash -c "(crontab -l 2>/dev/null; echo \"$cronEntry\") | crontab -"
        fi
    }
}

# Usage example
const cronManager = new CronTaskManager()

cronManager.addTask("backup", "0 2 * * *", "backup.sh", {
    user: "www-data",
    log_file: "/var/log/backup.log",
    dependencies: ["service:postgresql", "file:/backup/destination"],
    notify_email: "admin@example.com"
})

cronManager.addTask("cleanup", "0 3 * * 0", "cleanup.sh", {
    timeout: 600,
    retries: 2,
    environment: "production"
})

cronManager.installAll()
```

### **Task Monitoring and Health Checks**
```bash
#!/bin/bash

# Cron task monitoring
monitor_cron_tasks() {
    local action="$1"
    local task_name="$2"
    
    case "$action" in
        "status")
            check_task_status "$task_name"
            ;;
        "health")
            check_task_health "$task_name"
            ;;
        "logs")
            show_task_logs "$task_name"
            ;;
        "metrics")
            show_task_metrics "$task_name"
            ;;
        *)
            echo "Unknown monitoring action: $action"
            ;;
    esac
}

check_task_status() {
    local task_name="$1"
    
    if [[ -z "$task_name" ]]; then
        # Check all tasks
        echo "Checking status of all cron tasks..."
        
        local tasks=($(get_cron_task_names))
        for task in "${tasks[@]}"; do
            check_single_task_status "$task"
        done
    else
        check_single_task_status "$task_name"
    fi
}

check_single_task_status() {
    local task_name="$1"
    
    echo "Task: $task_name"
    
    # Check if task is in crontab
    if crontab -l 2>/dev/null | grep -q "$task_name"; then
        echo "  Status: ✓ Installed in crontab"
    else
        echo "  Status: ✗ Not found in crontab"
    fi
    
    # Check last execution
    local last_run=$(get_last_execution "$task_name")
    if [[ -n "$last_run" ]]; then
        echo "  Last run: $last_run"
    else
        echo "  Last run: Never"
    fi
    
    # Check next execution
    local next_run=$(get_next_execution "$task_name")
    if [[ -n "$next_run" ]]; then
        echo "  Next run: $next_run"
    else
        echo "  Next run: Unknown"
    fi
    
    # Check if process is running
    if is_task_running "$task_name"; then
        echo "  Process: ✓ Currently running"
    else
        echo "  Process: ✗ Not running"
    fi
    
    echo ""
}

get_last_execution() {
    local task_name="$1"
    local log_file="${cron_log_file:-$CRON_LOG_FILE}"
    
    if [[ -f "$log_file" ]]; then
        grep "Starting cron task: $task_name" "$log_file" | tail -1 | cut -d' ' -f1-2
    fi
}

get_next_execution() {
    local task_name="$1"
    
    # This would require parsing the cron schedule
    # For now, return a placeholder
    echo "Calculating next execution time..."
}

is_task_running() {
    local task_name="$1"
    
    # Check for running processes
    if pgrep -f "tusk-cron-$task_name" >/dev/null; then
        return 0
    fi
    
    return 1
}

check_task_health() {
    local task_name="$1"
    
    echo "Health check for task: $task_name"
    
    # Check recent failures
    local recent_failures=$(get_recent_failures "$task_name")
    if [[ "$recent_failures" -gt 0 ]]; then
        echo "  ⚠ Recent failures: $recent_failures"
    else
        echo "  ✓ No recent failures"
    fi
    
    # Check execution time
    local avg_execution_time=$(get_avg_execution_time "$task_name")
    if [[ -n "$avg_execution_time" ]]; then
        echo "  Average execution time: ${avg_execution_time}s"
    fi
    
    # Check resource usage
    local memory_usage=$(get_memory_usage "$task_name")
    if [[ -n "$memory_usage" ]]; then
        echo "  Memory usage: $memory_usage"
    fi
    
    # Check dependencies
    check_task_dependencies "$task_name"
}

get_recent_failures() {
    local task_name="$1"
    local log_file="${cron_log_file:-$CRON_LOG_FILE}"
    local hours="${2:-24}"
    
    if [[ -f "$log_file" ]]; then
        grep "Cron task '$task_name' failed" "$log_file" | \
        awk -v hours="$hours" '
            BEGIN { count = 0 }
            {
                # Parse timestamp and check if within hours
                timestamp = $1 " " $2
                if (systime() - mktime(gensub(/[-:]/, " ", "g", timestamp)) < hours * 3600) {
                    count++
                }
            }
            END { print count }
        '
    else
        echo "0"
    fi
}

get_avg_execution_time() {
    local task_name="$1"
    local log_file="${cron_log_file:-$CRON_LOG_FILE}"
    
    if [[ -f "$log_file" ]]; then
        # This would require parsing start and end times
        # For now, return a placeholder
        echo "Calculating average execution time..."
    fi
}

check_task_dependencies() {
    local task_name="$1"
    
    # Load task configuration
    local config_file=$(find_task_config "$task_name")
    if [[ -f "$config_file" ]]; then
        source <(tsk load "$config_file")
        
        if [[ -n "$cron_dependencies" ]]; then
            echo "  Dependencies:"
            for dep in "${cron_dependencies[@]}"; do
                if check_dependency "$dep"; then
                    echo "    ✓ $dep"
                else
                    echo "    ✗ $dep"
                fi
            done
        fi
    fi
}

check_dependency() {
    local dependency="$1"
    
    case "$dependency" in
        service:*)
            local service="${dependency#service:}"
            systemctl is-active --quiet "$service"
            ;;
        file:*)
            local file="${dependency#file:}"
            [[ -f "$file" ]]
            ;;
        port:*)
            local port="${dependency#port:}"
            netstat -tuln | grep -q ":$port "
            ;;
        *)
            return 0
            ;;
    esac
}
```

## 📊 **Task Scheduling Examples**

### **Database Backup Tasks**
```bash
# backup-tasks.tsk
backup_config:
  database: postgresql
  backup_dir: /backups
  retention_days: 30

#cron: "0 2 * * *"
#cron-name: daily-backup
#cron-description: Daily database backup
#cron-command: "pg_dump -h localhost -U postgres mydb | gzip > /backups/db_$(date +%Y%m%d).sql.gz"
#cron-user: postgres
#cron-log: /var/log/backup.log
#cron-timeout: 1800
#cron-retries: 2
#cron-notify: admin@example.com
#cron-dependencies: ["service:postgresql"]

#cron: "0 3 * * 0"
#cron-name: weekly-backup
#cron-description: Weekly full backup
#cron-command: "tar -czf /backups/full_$(date +%Y%m%d).tar.gz /var/www /etc /home"
#cron-user: root
#cron-log: /var/log/backup.log
#cron-timeout: 3600
#cron-retries: 1
#cron-notify: admin@example.com
```

### **System Maintenance Tasks**
```bash
# maintenance-tasks.tsk
maintenance_config:
  log_retention_days: 7
  temp_cleanup_threshold: 1GB
  package_update_frequency: weekly

#cron: "0 4 * * *"
#cron-name: log-rotation
#cron-description: Rotate log files
#cron-command: "logrotate /etc/logrotate.conf"
#cron-user: root
#cron-log: /var/log/maintenance.log
#cron-timeout: 300

#cron: "0 5 * * *"
#cron-name: temp-cleanup
#cron-description: Clean temporary files
#cron-command: "find /tmp -type f -mtime +7 -delete && find /var/tmp -type f -mtime +7 -delete"
#cron-user: root
#cron-log: /var/log/maintenance.log
#cron-timeout: 600

#cron: "0 6 * * 0"
#cron-name: package-update
#cron-description: Update system packages
#cron-command: "apt update && apt upgrade -y"
#cron-user: root
#cron-log: /var/log/maintenance.log
#cron-timeout: 1800
#cron-notify: admin@example.com
```

### **Application-Specific Tasks**
```bash
# app-tasks.tsk
app_config:
  name: myapp
  environment: production
  data_dir: /var/www/myapp/data

#cron: "*/15 * * * *"
#cron-name: health-check
#cron-description: Application health check
#cron-command: "curl -f http://localhost:8080/health || echo 'Health check failed'"
#cron-user: www-data
#cron-log: /var/log/myapp/health.log
#cron-timeout: 60
#cron-retries: 3

#cron: "0 */6 * * *"
#cron-name: data-sync
#cron-description: Sync data with external API
#cron-command: "/var/www/myapp/scripts/sync-data.sh"
#cron-user: www-data
#cron-log: /var/log/myapp/sync.log
#cron-timeout: 900
#cron-dependencies: ["service:myapp", "port:8080"]

#cron: "0 1 * * *"
#cron-name: cache-clear
#cron-description: Clear application cache
#cron-command: "rm -rf /var/www/myapp/cache/*"
#cron-user: www-data
#cron-log: /var/log/myapp/cache.log
#cron-timeout: 300
```

## 🔄 **Dynamic Scheduling**

### **Conditional Task Execution**
```bash
#!/bin/bash

# Dynamic cron scheduling
setup_dynamic_cron() {
    local config_file="$1"
    
    # Load configuration
    source <(tsk load "$config_file")
    
    # Check if task should run based on conditions
    if should_run_task; then
        install_cron_task
    else
        echo "Task conditions not met, skipping installation"
    fi
}

should_run_task() {
    # Check environment
    if [[ -n "$cron_environment" ]] && [[ "$cron_environment" != "$TUSK_ENV" ]]; then
        return 1
    fi
    
    # Check system load
    if [[ -n "$cron_max_load" ]]; then
        local current_load=$(uptime | awk '{print $(NF-2)}' | sed 's/,//')
        if (( $(echo "$current_load > $cron_max_load" | bc -l) )); then
            return 1
        fi
    fi
    
    # Check disk space
    if [[ -n "$cron_min_disk_space" ]]; then
        local available_space=$(df / | awk 'NR==2 {print $4}')
        if [[ "$available_space" -lt "$cron_min_disk_space" ]]; then
            return 1
        fi
    fi
    
    # Check network connectivity
    if [[ -n "$cron_network_check" ]]; then
        if ! ping -c 1 "$cron_network_check" >/dev/null 2>&1; then
            return 1
        fi
    fi
    
    return 0
}

# Example configuration with conditions
cat > dynamic-tasks.tsk << 'EOF'
#cron: "0 */4 * * *"
#cron-name: backup-if-needed
#cron-description: Backup only if conditions are met
#cron-command: "backup.sh"
#cron-environment: production
#cron-max-load: 2.0
#cron-min-disk-space: 1073741824  # 1GB in bytes
#cron-network-check: 8.8.8.8
EOF
```

### **Load-Based Scheduling**
```bash
#!/bin/bash

# Load-based cron scheduling
setup_load_based_cron() {
    local base_schedule="$1"
    local max_load="$2"
    local command="$3"
    
    # Create adaptive schedule
    local adaptive_command="
        if [[ \$(uptime | awk '{print \$(NF-2)}' | sed 's/,//') -lt $max_load ]]; then
            $command
        else
            echo 'System load too high, skipping task'
        fi
    "
    
    # Install with base schedule
    create_cron_entry "$base_schedule" "load-adaptive-task" "$adaptive_command" "$(whoami)" "$CRON_LOG_FILE"
}

# Example usage
setup_load_based_cron "*/30 * * * *" 1.5 "heavy-processing.sh"
```

## 📈 **Task Analytics and Reporting**

### **Cron Task Analytics**
```bash
#!/bin/bash

# Cron task analytics
generate_cron_report() {
    local report_file="${1:-cron-report-$(date +%Y%m%d).txt}"
    
    echo "TuskLang Cron Task Report" > "$report_file"
    echo "Generated: $(date)" >> "$report_file"
    echo "==================================" >> "$report_file"
    echo "" >> "$report_file"
    
    # Get all cron tasks
    local tasks=($(get_cron_task_names))
    
    for task in "${tasks[@]}"; do
        echo "Task: $task" >> "$report_file"
        echo "  Schedule: $(get_task_schedule "$task")" >> "$report_file"
        echo "  Last run: $(get_last_execution "$task")" >> "$report_file"
        echo "  Success rate: $(get_success_rate "$task")%" >> "$report_file"
        echo "  Avg duration: $(get_avg_duration "$task")s" >> "$report_file"
        echo "  Recent failures: $(get_recent_failures "$task")" >> "$report_file"
        echo "" >> "$report_file"
    done
    
    echo "Report generated: $report_file"
}

get_success_rate() {
    local task_name="$1"
    local log_file="${cron_log_file:-$CRON_LOG_FILE}"
    local days="${2:-7}"
    
    if [[ -f "$log_file" ]]; then
        local total_runs=$(grep "Starting cron task: $task_name" "$log_file" | wc -l)
        local successful_runs=$(grep "Cron task '$task_name' completed successfully" "$log_file" | wc -l)
        
        if [[ "$total_runs" -gt 0 ]]; then
            echo $(( (successful_runs * 100) / total_runs ))
        else
            echo "0"
        fi
    else
        echo "0"
    fi
}

get_avg_duration() {
    local task_name="$1"
    local log_file="${cron_log_file:-$CRON_LOG_FILE}"
    
    if [[ -f "$log_file" ]]; then
        # This would require parsing start and end timestamps
        # For now, return a placeholder
        echo "Calculating average duration..."
    fi
}
```

## 🚨 **Troubleshooting Cron Directives**

### **Common Issues and Solutions**

**1. Task Not Running**
```bash
# Debug task execution
debug_task_execution() {
    local task_name="$1"
    
    echo "Debugging task: $task_name"
    
    # Check crontab
    echo "Crontab entries:"
    crontab -l 2>/dev/null | grep "$task_name" || echo "No entries found"
    
    # Check log file
    local log_file="${cron_log_file:-$CRON_LOG_FILE}"
    if [[ -f "$log_file" ]]; then
        echo "Recent log entries:"
        tail -20 "$log_file" | grep "$task_name" || echo "No log entries found"
    else
        echo "Log file not found: $log_file"
    fi
    
    # Check permissions
    echo "File permissions:"
    ls -la "/tmp/tusk-cron-$task_name.sh" 2>/dev/null || echo "Wrapper script not found"
    
    # Check user permissions
    echo "User permissions:"
    whoami
    id
}
```

**2. Task Timing Issues**
```bash
# Debug scheduling issues
debug_scheduling() {
    local task_name="$1"
    
    echo "Debugging scheduling for task: $task_name"
    
    # Check system time
    echo "System time: $(date)"
    echo "System timezone: $(timedatectl show --property=Timezone --value 2>/dev/null || echo 'Unknown')"
    
    # Check cron service
    echo "Cron service status:"
    systemctl status cron 2>/dev/null || echo "Cron service not found"
    
    # Check crontab syntax
    echo "Crontab syntax check:"
    crontab -l 2>/dev/null | while read -r line; do
        if [[ "$line" =~ ^[0-9*/,-\s]+ ]]; then
            echo "  $line"
        fi
    done
}
```

**3. Permission Issues**
```bash
# Debug permission issues
debug_permissions() {
    local task_name="$1"
    
    echo "Debugging permissions for task: $task_name"
    
    # Check file permissions
    local wrapper_script="/tmp/tusk-cron-$task_name.sh"
    if [[ -f "$wrapper_script" ]]; then
        echo "Wrapper script permissions:"
        ls -la "$wrapper_script"
        
        # Check if executable
        if [[ -x "$wrapper_script" ]]; then
            echo "✓ Wrapper script is executable"
        else
            echo "✗ Wrapper script is not executable"
        fi
    fi
    
    # Check user permissions
    echo "User permissions:"
    echo "  Current user: $(whoami)"
    echo "  User ID: $(id -u)"
    echo "  Groups: $(id -Gn)"
    
    # Check crontab permissions
    echo "Crontab permissions:"
    if crontab -l >/dev/null 2>&1; then
        echo "✓ Can read crontab"
    else
        echo "✗ Cannot read crontab"
    fi
}
```

## 🔒 **Security Best Practices**

### **Cron Security Checklist**
```bash
# Security validation
validate_cron_security() {
    echo "Validating cron security configuration..."
    
    # Check file permissions
    local cron_files=("/etc/crontab" "/var/spool/cron/crontabs/$(whoami)")
    
    for file in "${cron_files[@]}"; do
        if [[ -f "$file" ]]; then
            local perms=$(stat -c %a "$file")
            if [[ "$perms" == "600" ]]; then
                show_success "File permissions: $file ($perms)"
            else
                show_warning "File permissions should be 600, got: $file ($perms)"
            fi
        fi
    done
    
    # Check log file permissions
    local log_file="${cron_log_file:-$CRON_LOG_FILE}"
    if [[ -f "$log_file" ]]; then
        local perms=$(stat -c %a "$log_file")
        if [[ "$perms" == "600" ]]; then
            show_success "Log file permissions: $log_file ($perms)"
        else
            show_warning "Log file permissions should be 600, got: $log_file ($perms)"
        fi
    fi
    
    # Check for suspicious commands
    check_suspicious_commands
}

check_suspicious_commands() {
    echo "Checking for suspicious commands in crontab..."
    
    local suspicious_patterns=(
        "wget.*http"
        "curl.*http"
        "nc.*-l"
        "bash.*-i"
        "python.*-c"
        "perl.*-e"
    )
    
    crontab -l 2>/dev/null | while read -r line; do
        for pattern in "${suspicious_patterns[@]}"; do
            if echo "$line" | grep -q "$pattern"; then
                show_warning "Suspicious command found: $line"
            fi
        done
    done
}
```

## 📈 **Performance Optimization Tips**

### **Cron Performance Checklist**
```bash
# Performance validation
validate_cron_performance() {
    echo "Validating cron performance configuration..."
    
    # Check task overlap
    check_task_overlap
    
    # Check resource usage
    check_resource_usage
    
    # Check scheduling efficiency
    check_scheduling_efficiency
}

check_task_overlap() {
    echo "Checking for task overlap..."
    
    # Get all cron schedules
    local schedules=($(crontab -l 2>/dev/null | grep -E '^[0-9*/,-\s]+' | awk '{print $1 " " $2 " " $3 " " $4 " " $5}'))
    
    # Check for overlapping schedules
    for i in "${!schedules[@]}"; do
        for j in "${!schedules[@]}"; do
            if [[ $i -lt $j ]]; then
                if schedules_overlap "${schedules[$i]}" "${schedules[$j]}"; then
                    show_warning "Potential task overlap: ${schedules[$i]} and ${schedules[$j]}"
                fi
            fi
        done
    done
}

schedules_overlap() {
    local schedule1="$1"
    local schedule2="$2"
    
    # This is a simplified check - in practice, you'd need more sophisticated parsing
    if [[ "$schedule1" == "$schedule2" ]]; then
        return 0
    fi
    
    return 1
}
```

## 🎯 **Next Steps**

- **Middleware Directives**: Learn about middleware-specific directives
- **Plugin Integration**: Explore cron plugins
- **Advanced Scheduling**: Understand complex scheduling patterns
- **Testing Cron Directives**: Test cron functionality
- **Performance Tuning**: Optimize cron performance

---

**Cron directives transform your TuskLang configuration into a powerful task scheduler. They bring modern scheduling capabilities to your Bash applications with intelligent task management, monitoring, and error handling!** 
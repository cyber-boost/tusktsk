# Load Balancing in TuskLang - Bash Guide

## ⚖️ **Revolutionary Load Balancing Configuration**

Load balancing in TuskLang transforms your configuration files into intelligent, adaptive distribution systems. No more simple round-robin or static weights—everything lives in your TuskLang configuration with dynamic health checks, intelligent routing, and comprehensive load monitoring.

> **"We don't bow to any king"** – TuskLang load balancing breaks free from traditional distribution constraints and brings modern load management to your Bash applications.

## 🚀 **Core Load Balancing Directives**

### **Basic Load Balancing Setup**
```bash
#load-balancing: enabled              # Enable load balancing
#lb-enabled: true                    # Alternative syntax
#lb-algorithm: round-robin           # Load balancing algorithm
#lb-health-check: true               # Enable health checks
#lb-backup: true                     # Enable backup servers
#lb-sticky: true                     # Enable sticky sessions
```

### **Advanced Load Balancing Configuration**
```bash
#lb-adaptive: true                   # Enable adaptive load balancing
#lb-weighted: true                   # Enable weighted distribution
#lb-least-connections: true          # Enable least connections
#lb-response-time: true              # Enable response time routing
#lb-monitoring: true                 # Enable load balancer monitoring
#lb-failover: true                   # Enable automatic failover
```

## 🔧 **Bash Load Balancing Implementation**

### **Basic Load Balancer**
```bash
#!/bin/bash

# Load load balancing configuration
source <(tsk load load-balancing.tsk)

# Load balancing configuration
LB_ENABLED="${lb_enabled:-true}"
LB_ALGORITHM="${lb_algorithm:-round-robin}"
LB_HEALTH_CHECK="${lb_health_check:-true}"
LB_BACKUP="${lb_backup:-true}"
LB_STICKY="${lb_sticky:-true}"

# Load balancer
class LoadBalancer {
    constructor() {
        this.enabled = LB_ENABLED
        this.algorithm = LB_ALGORITHM
        this.healthCheck = LB_HEALTH_CHECK
        this.backup = LB_BACKUP
        this.sticky = LB_STICKY
        this.servers = []
        this.currentIndex = 0
        this.stats = {
            requests_forwarded: 0,
            health_checks: 0,
            failovers: 0
        }
    }
    
    addServer(server) {
        this.servers.push({
            ...server,
            health: 'unknown',
            lastCheck: 0,
            connections: 0,
            responseTime: 0
        })
    }
    
    getNextServer(client_id = null) {
        if (!this.enabled || this.servers.length === 0) return null
        
        // Filter healthy servers
        const healthyServers = this.servers.filter(server => server.health === 'healthy')
        
        if (healthyServers.length === 0) {
            // Use backup servers if available
            if (this.backup) {
                return this.servers.find(server => server.backup) || null
            }
            return null
        }
        
        // Apply load balancing algorithm
        switch (this.algorithm) {
            case 'round-robin':
                return this.roundRobin(healthyServers)
            case 'least-connections':
                return this.leastConnections(healthyServers)
            case 'weighted':
                return this.weighted(healthyServers)
            case 'response-time':
                return this.responseTime(healthyServers)
            default:
                return this.roundRobin(healthyServers)
        }
    }
    
    roundRobin(servers) {
        const server = servers[this.currentIndex % servers.length]
        this.currentIndex++
        return server
    }
    
    leastConnections(servers) {
        return servers.reduce((min, server) => 
            server.connections < min.connections ? server : min
        )
    }
    
    weighted(servers) {
        // Implementation for weighted distribution
        return servers[0]
    }
    
    responseTime(servers) {
        return servers.reduce((min, server) => 
            server.responseTime < min.responseTime ? server : min
        )
    }
    
    performHealthCheck() {
        if (!this.healthCheck) return
        
        this.servers.forEach(server => {
            const healthy = this.checkServerHealth(server)
            server.health = healthy ? 'healthy' : 'unhealthy'
            server.lastCheck = Date.now()
            this.stats.health_checks++
        })
    }
    
    checkServerHealth(server) {
        // Implementation for health check
        return true
    }
    
    getStats() {
        return { ...this.stats }
    }
}

# Initialize load balancer
const loadBalancer = new LoadBalancer()
```

### **Dynamic Load Balancing**
```bash
#!/bin/bash

# Dynamic load balancing
load_balance_request() {
    local client_id="$1"
    local request_type="${2:-http}"
    local algorithm="${lb_algorithm:-round-robin}"
    
    # Get available servers
    local servers=($(get_healthy_servers))
    
    if [[ ${#servers[@]} -eq 0 ]]; then
        echo "No healthy servers available"
        return 1
    fi
    
    # Select server based on algorithm
    local selected_server
    case "$algorithm" in
        "round-robin")
            selected_server=$(round_robin_select "${servers[@]}")
            ;;
        "least-connections")
            selected_server=$(least_connections_select "${servers[@]}")
            ;;
        "weighted")
            selected_server=$(weighted_select "${servers[@]}")
            ;;
        "response-time")
            selected_server=$(response_time_select "${servers[@]}")
            ;;
        *)
            selected_server=$(round_robin_select "${servers[@]}")
            ;;
    esac
    
    echo "$selected_server"
}

get_healthy_servers() {
    local servers_file="${lb_servers_file:-/etc/load-balancer/servers.conf}"
    local healthy_servers=()
    
    if [[ -f "$servers_file" ]]; then
        while IFS= read -r server_line; do
            local server="${server_line%:*}"
            local port="${server_line#*:}"
            
            if check_server_health "$server" "$port"; then
                healthy_servers+=("$server:$port")
            fi
        done < "$servers_file"
    fi
    
    echo "${healthy_servers[@]}"
}

check_server_health() {
    local server="$1"
    local port="${2:-80}"
    local timeout="${lb_health_check_timeout:-5}"
    
    # Try to connect to server
    if timeout "$timeout" bash -c "echo >/dev/tcp/$server/$port" 2>/dev/null; then
        return 0
    else
        return 1
    fi
}
```

### **Load Balancing Algorithms**
```bash
#!/bin/bash

# Round-robin load balancing
round_robin_select() {
    local servers=("$@")
    local index_file="/tmp/lb_round_robin_index"
    
    # Get current index
    local current_index=0
    if [[ -f "$index_file" ]]; then
        current_index=$(cat "$index_file")
    fi
    
    # Select server
    local selected_server="${servers[$current_index]}"
    
    # Update index
    local next_index=$(((current_index + 1) % ${#servers[@]}))
    echo "$next_index" > "$index_file"
    
    echo "$selected_server"
}

# Least connections load balancing
least_connections_select() {
    local servers=("$@")
    local min_connections=999999
    local selected_server=""
    
    for server in "${servers[@]}"; do
        local connections=$(get_server_connections "$server")
        
        if [[ $connections -lt $min_connections ]]; then
            min_connections=$connections
            selected_server="$server"
        fi
    done
    
    echo "$selected_server"
}

get_server_connections() {
    local server="$1"
    local connections_file="/tmp/lb_connections_${server//[:.]/_}"
    
    if [[ -f "$connections_file" ]]; then
        cat "$connections_file"
    else
        echo "0"
    fi
}

# Weighted load balancing
weighted_select() {
    local servers=("$@")
    local weights_file="${lb_weights_file:-/etc/load-balancer/weights.conf}"
    local total_weight=0
    local server_weights=()
    
    # Load weights
    if [[ -f "$weights_file" ]]; then
        while IFS= read -r weight_line; do
            local server="${weight_line%:*}"
            local weight="${weight_line#*:}"
            server_weights+=("$server:$weight")
            total_weight=$((total_weight + weight))
        done < "$weights_file"
    else
        # Default equal weights
        for server in "${servers[@]}"; do
            server_weights+=("$server:1")
            total_weight=$((total_weight + 1))
        done
    fi
    
    # Select server based on weight
    local random=$((RANDOM % total_weight))
    local current_weight=0
    
    for weight_entry in "${server_weights[@]}"; do
        local server="${weight_entry%:*}"
        local weight="${weight_entry#*:}"
        current_weight=$((current_weight + weight))
        
        if [[ $random -lt $current_weight ]]; then
            echo "$server"
            return 0
        fi
    done
    
    # Fallback to first server
    echo "${servers[0]}"
}

# Response time load balancing
response_time_select() {
    local servers=("$@")
    local min_response_time=999999
    local selected_server=""
    
    for server in "${servers[@]}"; do
        local response_time=$(measure_server_response_time "$server")
        
        if [[ $response_time -lt $min_response_time ]]; then
            min_response_time=$response_time
            selected_server="$server"
        fi
    done
    
    echo "$selected_server"
}

measure_server_response_time() {
    local server="$1"
    local timeout="${lb_response_timeout:-5}"
    
    # Measure response time using curl
    local start_time=$(date +%s%N)
    if curl -s --max-time "$timeout" "http://$server/health" >/dev/null 2>&1; then
        local end_time=$(date +%s%N)
        local response_time=$(((end_time - start_time) / 1000000)) # Convert to milliseconds
        echo "$response_time"
    else
        echo "999999" # High penalty for failed requests
    fi
}
```

### **Health Checking**
```bash
#!/bin/bash

# Health checking
perform_health_checks() {
    local servers_file="${lb_servers_file:-/etc/load-balancer/servers.conf}"
    local health_log="/var/log/load-balancer/health.log"
    
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Starting health checks" >> "$health_log"
    
    if [[ -f "$servers_file" ]]; then
        while IFS= read -r server_line; do
            local server="${server_line%:*}"
            local port="${server_line#*:}"
            
            if check_server_health "$server" "$port"; then
                mark_server_healthy "$server" "$port"
                echo "$(date '+%Y-%m-%d %H:%M:%S') - $server:$port is healthy" >> "$health_log"
            else
                mark_server_unhealthy "$server" "$port"
                echo "$(date '+%Y-%m-%d %H:%M:%S') - $server:$port is unhealthy" >> "$health_log"
            fi
        done < "$servers_file"
    fi
    
    echo "$(date '+%Y-%m-%d %H:%M:%S') - Health checks completed" >> "$health_log"
}

mark_server_healthy() {
    local server="$1"
    local port="$2"
    local health_file="/tmp/lb_health_${server//[:.]/_}"
    
    cat > "$health_file" << EOF
{
    "server": "$server",
    "port": "$port",
    "status": "healthy",
    "last_check": "$(date -Iseconds)",
    "response_time": $(measure_server_response_time "$server:$port")
}
EOF
}

mark_server_unhealthy() {
    local server="$1"
    local port="$2"
    local health_file="/tmp/lb_health_${server//[:.]/_}"
    
    cat > "$health_file" << EOF
{
    "server": "$server",
    "port": "$port",
    "status": "unhealthy",
    "last_check": "$(date -Iseconds)",
    "response_time": 999999
}
EOF
}
```

### **Sticky Sessions**
```bash
#!/bin/bash

# Sticky sessions
get_sticky_server() {
    local client_id="$1"
    local session_file="/tmp/lb_session_${client_id//[^a-zA-Z0-9]/_}"
    
    if [[ -f "$session_file" ]]; then
        local server=$(cat "$session_file")
        
        # Verify server is still healthy
        if check_server_health "${server%:*}" "${server#*:}"; then
            echo "$server"
            return 0
        else
            # Remove stale session
            rm -f "$session_file"
        fi
    fi
    
    return 1
}

set_sticky_server() {
    local client_id="$1"
    local server="$2"
    local session_file="/tmp/lb_session_${client_id//[^a-zA-Z0-9]/_}"
    local session_ttl="${lb_session_ttl:-3600}"
    
    cat > "$session_file" << EOF
{
    "client_id": "$client_id",
    "server": "$server",
    "created_at": "$(date -Iseconds)",
    "expires_at": "$(date -d "+$session_ttl seconds" -Iseconds)"
}
EOF
}

cleanup_expired_sessions() {
    local current_time=$(date +%s)
    
    find /tmp -name "lb_session_*" -type f -exec sh -c '
        for file do
            local expires_at=$(jq -r ".expires_at" "$file" 2>/dev/null)
            if [[ -n "$expires_at" ]]; then
                local expires_time=$(date -d "$expires_at" +%s 2>/dev/null || echo 0)
                if [[ $expires_time -lt $current_time ]]; then
                    rm -f "$file"
                fi
            fi
        done
    ' sh {} +
}
```

### **Load Balancer Monitoring**
```bash
#!/bin/bash

# Load balancer monitoring
monitor_load_balancer() {
    local monitoring_file="/var/log/load-balancer/monitoring.json"
    local servers_file="${lb_servers_file:-/etc/load-balancer/servers.conf}"
    
    # Collect monitoring data
    local total_servers=0
    local healthy_servers=0
    local total_connections=0
    local average_response_time=0
    
    if [[ -f "$servers_file" ]]; then
        while IFS= read -r server_line; do
            local server="${server_line%:*}"
            local port="${server_line#*:}"
            total_servers=$((total_servers + 1))
            
            if check_server_health "$server" "$port"; then
                healthy_servers=$((healthy_servers + 1))
            fi
            
            local connections=$(get_server_connections "$server:$port")
            total_connections=$((total_connections + connections))
        done < "$servers_file"
    fi
    
    # Calculate average response time
    if [[ $healthy_servers -gt 0 ]]; then
        local total_response_time=0
        local response_count=0
        
        while IFS= read -r server_line; do
            local server="${server_line%:*}"
            local port="${server_line#*:}"
            
            if check_server_health "$server" "$port"; then
                local response_time=$(measure_server_response_time "$server:$port")
                total_response_time=$((total_response_time + response_time))
                response_count=$((response_count + 1))
            fi
        done < "$servers_file"
        
        if [[ $response_count -gt 0 ]]; then
            average_response_time=$((total_response_time / response_count))
        fi
    fi
    
    # Generate monitoring report
    cat > "$monitoring_file" << EOF
{
    "timestamp": "$(date -Iseconds)",
    "total_servers": $total_servers,
    "healthy_servers": $healthy_servers,
    "unhealthy_servers": $((total_servers - healthy_servers)),
    "health_percentage": $((healthy_servers * 100 / total_servers)),
    "total_connections": $total_connections,
    "average_response_time_ms": $average_response_time
}
EOF
    
    echo "✓ Load balancer monitoring completed"
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Load Balancing Configuration**
```bash
# load-balancing-config.tsk
load_balancing_config:
  enabled: true
  algorithm: round-robin
  health_check: true
  backup: true
  sticky: true

#load-balancing: enabled
#lb-enabled: true
#lb-algorithm: round-robin
#lb-health-check: true
#lb-backup: true
#lb-sticky: true

#lb-adaptive: true
#lb-weighted: true
#lb-least-connections: true
#lb-response-time: true
#lb-monitoring: true
#lb-failover: true

#lb-config:
#  general:
#    algorithm: round-robin
#    health_check: true
#    backup: true
#    sticky: true
#  servers:
#    - "web1.example.com:80"
#    - "web2.example.com:80"
#    - "web3.example.com:80"
#    - "backup.example.com:80"
#  health_check:
#    enabled: true
#    interval: 30
#    timeout: 5
#    path: "/health"
#    expected_status: 200
#  algorithms:
#    round_robin:
#      enabled: true
#    least_connections:
#      enabled: true
#    weighted:
#      enabled: true
#      weights:
#        "web1.example.com:80": 3
#        "web2.example.com:80": 2
#        "web3.example.com:80": 1
#    response_time:
#      enabled: true
#      timeout: 5
#  sticky_sessions:
#    enabled: true
#    ttl: 3600
#    cookie_name: "lb_session"
#  monitoring:
#    enabled: true
#    interval: 60
#    metrics:
#      - "health_percentage"
#      - "total_connections"
#      - "average_response_time"
#  failover:
#    enabled: true
#    automatic: true
#    notification: true
```

### **Multi-Tier Load Balancing**
```bash
# multi-tier-load-balancing.tsk
multi_tier_load_balancing:
  tiers:
    - name: frontend
      algorithm: round-robin
      servers:
        - "frontend1.example.com:80"
        - "frontend2.example.com:80"
    - name: backend
      algorithm: least-connections
      servers:
        - "backend1.example.com:8080"
        - "backend2.example.com:8080"
    - name: database
      algorithm: weighted
      servers:
        - "db1.example.com:5432"
        - "db2.example.com:5432"

#lb-frontend: round-robin
#lb-backend: least-connections
#lb-database: weighted

#lb-config:
#  tiers:
#    frontend:
#      algorithm: round-robin
#      servers:
#        - "frontend1.example.com:80"
#        - "frontend2.example.com:80"
#      health_check: true
#    backend:
#      algorithm: least-connections
#      servers:
#        - "backend1.example.com:8080"
#        - "backend2.example.com:8080"
#      health_check: true
#    database:
#      algorithm: weighted
#      servers:
#        - "db1.example.com:5432"
#        - "db2.example.com:5432"
#      health_check: true
```

## 🚨 **Troubleshooting Load Balancing**

### **Common Issues and Solutions**

**1. Load Balancer Issues**
```bash
# Debug load balancer
debug_load_balancer() {
    echo "Debugging load balancer..."
    perform_health_checks
    monitor_load_balancer
    echo "Load balancer debug completed"
}
```

**2. Health Check Issues**
```bash
# Debug health checks
debug_health_checks() {
    local server="$1"
    local port="${2:-80}"
    echo "Debugging health check for $server:$port"
    check_server_health "$server" "$port"
}
```

## 🔒 **Security Best Practices**

### **Load Balancing Security Checklist**
```bash
# Security validation
validate_load_balancing_security() {
    echo "Validating load balancing security configuration..."
    # Check SSL termination
    if [[ "${lb_ssl_termination}" == "true" ]]; then
        echo "✓ SSL termination enabled"
    else
        echo "⚠ SSL termination not enabled"
    fi
    # Check access controls
    if [[ "${lb_access_controls}" == "true" ]]; then
        echo "✓ Load balancer access controls enabled"
    else
        echo "⚠ Load balancer access controls not enabled"
    fi
    # Check DDoS protection
    if [[ "${lb_ddos_protection}" == "true" ]]; then
        echo "✓ DDoS protection enabled"
    else
        echo "⚠ DDoS protection not enabled"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Load Balancing Performance Checklist**
```bash
# Performance validation
validate_load_balancing_performance() {
    echo "Validating load balancing performance configuration..."
    # Check health check interval
    local health_check_interval="${lb_health_check_interval:-30}" # seconds
    if [[ "$health_check_interval" -ge 10 ]]; then
        echo "✓ Reasonable health check interval ($health_check_interval s)"
    else
        echo "⚠ Frequent health checks may impact performance ($health_check_interval s)"
    fi
    # Check connection pooling
    if [[ "${lb_connection_pooling}" == "true" ]]; then
        echo "✓ Connection pooling enabled"
    else
        echo "⚠ Connection pooling not enabled"
    fi
    # Check adaptive load balancing
    if [[ "${lb_adaptive}" == "true" ]]; then
        echo "✓ Adaptive load balancing enabled"
    else
        echo "⚠ Adaptive load balancing not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **Load Balancer Optimization**: Learn about advanced load balancer optimization
- **Load Balancer Visualization**: Create load balancer visualization dashboards
- **Load Balancer Correlation**: Implement load balancer correlation and alerting
- **Load Balancer Compliance**: Set up load balancer compliance and auditing

---

**Load balancing transforms your TuskLang configuration into an intelligent, adaptive distribution system. It brings modern load management to your Bash applications with dynamic health checks, intelligent routing, and comprehensive monitoring!** 
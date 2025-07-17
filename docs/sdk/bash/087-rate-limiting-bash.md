# Rate Limiting in TuskLang - Bash Guide

## 🚦 **Revolutionary Rate Limiting Configuration**

Rate limiting in TuskLang transforms your configuration files into intelligent, adaptive throttling systems. No more simple counters or rigid limits—everything lives in your TuskLang configuration with dynamic rate calculation, intelligent burst handling, and comprehensive rate monitoring.

> **"We don't bow to any king"** – TuskLang rate limiting breaks free from traditional throttling constraints and brings modern rate management to your Bash applications.

## 🚀 **Core Rate Limiting Directives**

### **Basic Rate Limiting Setup**
```bash
#rate-limiting: enabled               # Enable rate limiting
#rate-enabled: true                  # Alternative syntax
#rate-window: 60                     # Time window (seconds)
#rate-limit: 100                     # Requests per window
#rate-burst: 20                      # Burst allowance
#rate-strategy: sliding              # Rate limiting strategy
```

### **Advanced Rate Limiting Configuration**
```bash
#rate-adaptive: true                 # Enable adaptive rate limiting
#rate-per-client: true               # Enable per-client limits
#rate-per-resource: true             # Enable per-resource limits
#rate-monitoring: true               # Enable rate monitoring
#rate-alerts: true                   # Enable rate alerts
#rate-backoff: exponential           # Rate limit backoff strategy
```

## 🔧 **Bash Rate Limiting Implementation**

### **Basic Rate Limiter**
```bash
#!/bin/bash

# Load rate limiting configuration
source <(tsk load rate-limiting.tsk)

# Rate limiting configuration
RATE_ENABLED="${rate_enabled:-true}"
RATE_WINDOW="${rate_window:-60}"
RATE_LIMIT="${rate_limit:-100}"
RATE_BURST="${rate_burst:-20}"
RATE_STRATEGY="${rate_strategy:-sliding}"

# Rate limiter
class RateLimiter {
    constructor() {
        this.enabled = RATE_ENABLED
        this.window = RATE_WINDOW
        this.limit = RATE_LIMIT
        this.burst = RATE_BURST
        this.strategy = RATE_STRATEGY
        this.requests = new Map()
        this.stats = {
            requests_allowed: 0,
            requests_denied: 0,
            rate_violations: 0
        }
    }
    
    checkRateLimit(client_id, resource = 'default') {
        if (!this.enabled) return { allowed: true }
        
        const key = `${client_id}:${resource}`
        const now = Date.now()
        
        // Get current requests for this client/resource
        let requests = this.requests.get(key) || []
        
        // Remove old requests outside the window
        requests = requests.filter(time => now - time < this.window * 1000)
        
        // Check if request is allowed
        const allowed = requests.length < this.limit + this.burst
        
        if (allowed) {
            requests.push(now)
            this.requests.set(key, requests)
            this.stats.requests_allowed++
        } else {
            this.stats.requests_denied++
            this.stats.rate_violations++
        }
        
        return {
            allowed,
            remaining: Math.max(0, this.limit - requests.length),
            reset_time: now + this.window * 1000
        }
    }
    
    getStats() {
        return { ...this.stats }
    }
    
    getClientStats(client_id) {
        const key = `${client_id}:default`
        const requests = this.requests.get(key) || []
        return {
            client_id,
            current_requests: requests.length,
            limit: this.limit,
            burst: this.burst
        }
    }
}

# Initialize rate limiter
const rateLimiter = new RateLimiter()
```

### **Dynamic Rate Limiting**
```bash
#!/bin/bash

# Dynamic rate limiting
check_rate_limit() {
    local client_id="$1"
    local resource="${2:-default}"
    local window="${rate_window:-60}"
    local limit="${rate_limit:-100}"
    local burst="${rate_burst:-20}"
    
    # Create rate limit file
    local rate_file="/tmp/rate_limit_${client_id}_${resource}.json"
    local current_time=$(date +%s)
    
    # Load existing rate data
    if [[ -f "$rate_file" ]]; then
        local requests=$(jq -r '.requests[]?' "$rate_file" 2>/dev/null)
        local request_count=$(echo "$requests" | wc -l)
    else
        local request_count=0
    fi
    
    # Check if within limit
    if [[ $request_count -lt $((limit + burst)) ]]; then
        # Add current request
        if [[ -f "$rate_file" ]]; then
            jq --arg time "$current_time" '.requests += [$time]' "$rate_file" > "${rate_file}.tmp" && mv "${rate_file}.tmp" "$rate_file"
        else
            cat > "$rate_file" << EOF
{
    "client_id": "$client_id",
    "resource": "$resource",
    "requests": ["$current_time"]
}
EOF
        fi
        
        echo "✓ Rate limit check passed"
        return 0
    else
        echo "✗ Rate limit exceeded"
        return 1
    fi
}

cleanup_rate_limits() {
    local window="${rate_window:-60}"
    local current_time=$(date +%s)
    local cutoff_time=$((current_time - window))
    
    # Clean up old rate limit files
    find /tmp -name "rate_limit_*.json" -type f -exec sh -c '
        for file do
            local requests=$(jq -r ".requests[]? | select(. >= $cutoff_time)" "$file" 2>/dev/null)
            if [[ -z "$requests" ]]; then
                rm -f "$file"
            else
                echo "$requests" | jq -R . | jq -s . | jq --arg client_id "$(jq -r .client_id "$file")" --arg resource "$(jq -r .resource "$file")" '{client_id: $client_id, resource: $resource, requests: .}' > "${file}.tmp" && mv "${file}.tmp" "$file"
            fi
        done
    ' sh {} +
    
    echo "✓ Rate limit cleanup completed"
}
```

### **Per-Client Rate Limiting**
```bash
#!/bin/bash

# Per-client rate limiting
check_client_rate_limit() {
    local client_id="$1"
    local client_ip="${2:-}"
    local user_agent="${3:-}"
    
    # Generate client identifier
    local client_key
    if [[ -n "$client_id" ]]; then
        client_key="$client_id"
    elif [[ -n "$client_ip" ]]; then
        client_key="$client_ip"
    else
        client_key="anonymous"
    fi
    
    # Check rate limit for client
    check_rate_limit "$client_key"
    
    # Log client request
    log_client_request "$client_key" "$client_ip" "$user_agent"
}

log_client_request() {
    local client_key="$1"
    local client_ip="$2"
    local user_agent="$3"
    
    local log_file="/var/log/rate_limiting.log"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    
    echo "$timestamp | $client_key | $client_ip | $user_agent" >> "$log_file"
}
```

### **Per-Resource Rate Limiting**
```bash
#!/bin/bash

# Per-resource rate limiting
check_resource_rate_limit() {
    local resource="$1"
    local client_id="${2:-default}"
    
    # Define resource-specific limits
    local resource_limits=(
        "api:100:60"
        "database:50:60"
        "file_upload:10:300"
        "email:5:3600"
    )
    
    # Find resource limit
    local limit_info
    for limit_entry in "${resource_limits[@]}"; do
        IFS=':' read -r res_name res_limit res_window <<< "$limit_entry"
        if [[ "$res_name" == "$resource" ]]; then
            limit_info="$res_limit:$res_window"
            break
        fi
    done
    
    if [[ -n "$limit_info" ]]; then
        IFS=':' read -r limit window <<< "$limit_info"
        check_rate_limit "$client_id" "$resource" "$window" "$limit"
    else
        # Use default limits
        check_rate_limit "$client_id" "$resource"
    fi
}
```

### **Adaptive Rate Limiting**
```bash
#!/bin/bash

# Adaptive rate limiting
adaptive_rate_limit() {
    local client_id="$1"
    local resource="${2:-default}"
    local current_load="${3:-0}"
    
    # Adjust rate limit based on system load
    local base_limit="${rate_limit:-100}"
    local adjusted_limit
    
    if [[ $current_load -gt 80 ]]; then
        # High load - reduce limit by 50%
        adjusted_limit=$((base_limit / 2))
    elif [[ $current_load -gt 60 ]]; then
        # Medium load - reduce limit by 25%
        adjusted_limit=$((base_limit * 3 / 4))
    else
        # Low load - use base limit
        adjusted_limit="$base_limit"
    fi
    
    echo "Adaptive rate limit: $adjusted_limit (load: ${current_load}%)"
    
    # Check rate limit with adjusted limit
    check_rate_limit "$client_id" "$resource" "${rate_window:-60}" "$adjusted_limit"
}
```

### **Rate Limit Monitoring**
```bash
#!/bin/bash

# Rate limit monitoring
monitor_rate_limits() {
    local monitoring_file="/var/log/rate_limit_monitoring.json"
    local current_time=$(date +%s)
    
    # Collect rate limit statistics
    local total_requests=0
    local denied_requests=0
    local active_clients=0
    
    # Count rate limit files
    local rate_files=(/tmp/rate_limit_*.json)
    active_clients=${#rate_files[@]}
    
    # Count total requests
    for file in "${rate_files[@]}"; do
        if [[ -f "$file" ]]; then
            local requests=$(jq -r '.requests | length' "$file" 2>/dev/null || echo 0)
            total_requests=$((total_requests + requests))
        fi
    done
    
    # Generate monitoring report
    cat > "$monitoring_file" << EOF
{
    "timestamp": "$current_time",
    "total_requests": $total_requests,
    "denied_requests": $denied_requests,
    "active_clients": $active_clients,
    "rate_limit_files": ${#rate_files[@]}
}
EOF
    
    echo "✓ Rate limit monitoring completed"
}

generate_rate_limit_report() {
    local report_file="/var/log/rate_limit_report.json"
    
    # Generate comprehensive report
    local report_data=()
    
    for file in /tmp/rate_limit_*.json; do
        if [[ -f "$file" ]]; then
            local client_id=$(jq -r '.client_id' "$file" 2>/dev/null)
            local resource=$(jq -r '.resource' "$file" 2>/dev/null)
            local requests=$(jq -r '.requests | length' "$file" 2>/dev/null || echo 0)
            
            report_data+=("{\"client_id\": \"$client_id\", \"resource\": \"$resource\", \"requests\": $requests}")
        fi
    done
    
    # Write report
    echo "[$(IFS=,; echo "${report_data[*]}")]" > "$report_file"
    
    echo "✓ Rate limit report generated: $report_file"
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Rate Limiting Configuration**
```bash
# rate-limiting-config.tsk
rate_limiting_config:
  enabled: true
  window: 60
  limit: 100
  burst: 20
  strategy: sliding

#rate-limiting: enabled
#rate-enabled: true
#rate-window: 60
#rate-limit: 100
#rate-burst: 20
#rate-strategy: sliding

#rate-adaptive: true
#rate-per-client: true
#rate-per-resource: true
#rate-monitoring: true
#rate-alerts: true
#rate-backoff: exponential

#rate-config:
#  general:
#    window: 60
#    limit: 100
#    burst: 20
#    strategy: sliding
#  per_client:
#    enabled: true
#    identification:
#      - "client_id"
#      - "ip_address"
#      - "user_agent"
#  per_resource:
#    enabled: true
#    resources:
#      api:
#        limit: 100
#        window: 60
#      database:
#        limit: 50
#        window: 60
#      file_upload:
#        limit: 10
#        window: 300
#      email:
#        limit: 5
#        window: 3600
#  adaptive:
#    enabled: true
#    load_thresholds:
#      high: 80
#      medium: 60
#    adjustments:
#      high_load: 0.5
#      medium_load: 0.75
#  monitoring:
#    enabled: true
#    interval: 60
#    metrics:
#      - "total_requests"
#      - "denied_requests"
#      - "active_clients"
#  alerts:
#    enabled: true
#    thresholds:
#      rate_violations: 10
#      denied_requests: 50
#    channels:
#      slack:
#        webhook: "${SLACK_WEBHOOK}"
#        channel: "#rate-limiting"
#      email:
#        recipients: ["ops@example.com"]
#        smtp_server: "smtp.example.com"
#  backoff:
#    strategy: exponential
#    base_delay: 1
#    max_delay: 60
```

### **Multi-Tier Rate Limiting**
```bash
# multi-tier-rate-limiting.tsk
multi_tier_rate_limiting:
  tiers:
    - name: free
      limit: 10
      window: 60
    - name: premium
      limit: 100
      window: 60
    - name: enterprise
      limit: 1000
      window: 60

#rate-free: 10:60
#rate-premium: 100:60
#rate-enterprise: 1000:60

#rate-config:
#  tiers:
#    free:
#      limit: 10
#      window: 60
#      burst: 5
#    premium:
#      limit: 100
#      window: 60
#      burst: 20
#    enterprise:
#      limit: 1000
#      window: 60
#      burst: 100
```

## 🚨 **Troubleshooting Rate Limiting**

### **Common Issues and Solutions**

**1. Rate Limit Issues**
```bash
# Debug rate limiting
debug_rate_limiting() {
    local client_id="$1"
    local resource="${2:-default}"
    echo "Debugging rate limiting for client: $client_id, resource: $resource"
    
    local rate_file="/tmp/rate_limit_${client_id}_${resource}.json"
    if [[ -f "$rate_file" ]]; then
        echo "Rate limit file contents:"
        cat "$rate_file"
    else
        echo "No rate limit file found"
    fi
}
```

**2. Rate Limit Monitoring Issues**
```bash
# Debug rate limit monitoring
debug_rate_limit_monitoring() {
    echo "Debugging rate limit monitoring..."
    monitor_rate_limits
    generate_rate_limit_report
    echo "Rate limit monitoring debug completed"
}
```

## 🔒 **Security Best Practices**

### **Rate Limiting Security Checklist**
```bash
# Security validation
validate_rate_limiting_security() {
    echo "Validating rate limiting security configuration..."
    # Check client identification
    if [[ "${rate_per_client}" == "true" ]]; then
        echo "✓ Per-client rate limiting enabled"
    else
        echo "⚠ Per-client rate limiting not enabled"
    fi
    # Check rate limit bypass protection
    if [[ "${rate_bypass_protection}" == "true" ]]; then
        echo "✓ Rate limit bypass protection enabled"
    else
        echo "⚠ Rate limit bypass protection not enabled"
    fi
    # Check rate limit monitoring
    if [[ "${rate_monitoring}" == "true" ]]; then
        echo "✓ Rate limit monitoring enabled"
    else
        echo "⚠ Rate limit monitoring not enabled"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Rate Limiting Performance Checklist**
```bash
# Performance validation
validate_rate_limiting_performance() {
    echo "Validating rate limiting performance configuration..."
    # Check window size
    local window="${rate_window:-60}" # seconds
    if [[ "$window" -ge 30 ]]; then
        echo "✓ Reasonable window size ($window s)"
    else
        echo "⚠ Small window size may impact performance ($window s)"
    fi
    # Check burst allowance
    local burst="${rate_burst:-20}"
    if [[ "$burst" -le 100 ]]; then
        echo "✓ Reasonable burst allowance ($burst)"
    else
        echo "⚠ High burst allowance may impact performance ($burst)"
    fi
    # Check adaptive rate limiting
    if [[ "${rate_adaptive}" == "true" ]]; then
        echo "✓ Adaptive rate limiting enabled"
    else
        echo "⚠ Adaptive rate limiting not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **Rate Limit Analysis**: Learn about advanced rate limit analysis
- **Rate Limit Visualization**: Create rate limit visualization dashboards
- **Rate Limit Correlation**: Implement rate limit correlation and alerting
- **Rate Limit Compliance**: Set up rate limit compliance and auditing

---

**Rate limiting transforms your TuskLang configuration into an intelligent, adaptive throttling system. It brings modern rate management to your Bash applications with dynamic limits, per-client/resource controls, and comprehensive monitoring!** 
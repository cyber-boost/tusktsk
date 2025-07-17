# Deployment Strategies in TuskLang - Bash Guide

## 🚀 **Revolutionary Deployment Configuration**

Deployment strategies in TuskLang transform your configuration files into intelligent deployment systems. No more separate deployment tools or complex CI/CD configurations - everything lives in your TuskLang configuration with dynamic deployment strategies, automatic rollbacks, and intelligent scaling.

> **"We don't bow to any king"** - TuskLang deployment strategies break free from traditional deployment constraints and bring modern deployment capabilities to your Bash applications.

## 🚀 **Core Deployment Directives**

### **Basic Deployment Setup**
```bash
#deploy: enabled                    # Enable deployment
#deploy-enabled: true              # Alternative syntax
#deploy-strategy: blue-green       # Deployment strategy
#deploy-environment: production    # Target environment
#deploy-rollback: automatic        # Rollback strategy
#deploy-monitoring: true           # Enable deployment monitoring
```

### **Advanced Deployment Configuration**
```bash
#deploy-health-check: true         # Enable health checks
#deploy-scaling: auto              # Auto-scaling strategy
#deploy-backup: true               # Enable backups before deployment
#deploy-notifications: slack       # Deployment notifications
#deploy-artifacts: true            # Enable artifact management
#deploy-security: true             # Enable security scanning
```

## 🔧 **Bash Deployment Implementation**

### **Basic Deployment Manager**
```bash
#!/bin/bash

# Load deployment configuration
source <(tsk load deployment.tsk)

# Deployment configuration
DEPLOY_ENABLED="${deploy_enabled:-true}"
DEPLOY_STRATEGY="${deploy_strategy:-blue-green}"
DEPLOY_ENVIRONMENT="${deploy_environment:-production}"
DEPLOY_ROLLBACK="${deploy_rollback:-automatic}"

# Deployment manager
class DeploymentManager {
    constructor() {
        this.enabled = DEPLOY_ENABLED
        this.strategy = DEPLOY_STRATEGY
        this.environment = DEPLOY_ENVIRONMENT
        this.rollback = DEPLOY_ROLLBACK
        this.deployments = new Map()
        this.stats = {
            total: 0,
            successful: 0,
            failed: 0,
            rollbacks: 0
        }
    }
    
    deploy(application, version, options = {}) {
        if (!this.enabled) {
            throw new Error("Deployment is disabled")
        }
        
        console.log(`Starting deployment of ${application} version ${version}`)
        
        const deploymentId = this.generateDeploymentId()
        const deployment = {
            id: deploymentId,
            application,
            version,
            strategy: this.strategy,
            environment: this.environment,
            startTime: new Date().toISOString(),
            status: 'in_progress',
            options
        }
        
        this.deployments.set(deploymentId, deployment)
        this.stats.total++
        
        try {
            const result = this.executeDeployment(deployment)
            
            if (result.success) {
                deployment.status = 'successful'
                deployment.endTime = new Date().toISOString()
                deployment.duration = Date.now() - new Date(deployment.startTime).getTime()
                this.stats.successful++
                
                console.log(`Deployment successful: ${deploymentId}`)
                this.sendNotification('success', deployment)
            } else {
                deployment.status = 'failed'
                deployment.error = result.error
                deployment.endTime = new Date().toISOString()
                this.stats.failed++
                
                console.log(`Deployment failed: ${deploymentId}`)
                this.sendNotification('failure', deployment)
                
                if (this.rollback === 'automatic') {
                    this.rollback(deploymentId)
                }
            }
            
            return result
        } catch (error) {
            deployment.status = 'error'
            deployment.error = error.message
            deployment.endTime = new Date().toISOString()
            this.stats.failed++
            
            console.log(`Deployment error: ${deploymentId}`)
            this.sendNotification('error', deployment)
            
            if (this.rollback === 'automatic') {
                this.rollback(deploymentId)
            }
            
            throw error
        }
    }
    
    executeDeployment(deployment) {
        switch (this.strategy) {
            case 'blue-green':
                return this.blueGreenDeploy(deployment)
            case 'rolling':
                return this.rollingDeploy(deployment)
            case 'canary':
                return this.canaryDeploy(deployment)
            case 'recreate':
                return this.recreateDeploy(deployment)
            default:
                return this.blueGreenDeploy(deployment)
        }
    }
    
    blueGreenDeploy(deployment) {
        console.log("Executing blue-green deployment...")
        
        // Create backup
        if (deployment.options.backup) {
            this.createBackup(deployment)
        }
        
        // Deploy to green environment
        const greenResult = this.deployToEnvironment('green', deployment)
        if (!greenResult.success) {
            return { success: false, error: greenResult.error }
        }
        
        // Run health checks
        const healthResult = this.runHealthChecks('green', deployment)
        if (!healthResult.success) {
            this.rollbackToEnvironment('green', deployment)
            return { success: false, error: healthResult.error }
        }
        
        // Switch traffic to green
        const switchResult = this.switchTraffic('green', deployment)
        if (!switchResult.success) {
            this.rollbackToEnvironment('green', deployment)
            return { success: false, error: switchResult.error }
        }
        
        // Decommission blue environment
        this.decommissionEnvironment('blue', deployment)
        
        return { success: true }
    }
    
    rollingDeploy(deployment) {
        console.log("Executing rolling deployment...")
        
        // Get current instances
        const instances = this.getInstances(deployment.application)
        
        // Deploy to instances one by one
        for (const instance of instances) {
            const result = this.deployToInstance(instance, deployment)
            if (!result.success) {
                return { success: false, error: result.error }
            }
            
            // Run health check
            const healthResult = this.runHealthCheck(instance, deployment)
            if (!healthResult.success) {
                this.rollbackInstance(instance, deployment)
                return { success: false, error: healthResult.error }
            }
        }
        
        return { success: true }
    }
    
    canaryDeploy(deployment) {
        console.log("Executing canary deployment...")
        
        // Deploy to canary environment
        const canaryResult = this.deployToEnvironment('canary', deployment)
        if (!canaryResult.success) {
            return { success: false, error: canaryResult.error }
        }
        
        // Run health checks
        const healthResult = this.runHealthChecks('canary', deployment)
        if (!healthResult.success) {
            this.rollbackToEnvironment('canary', deployment)
            return { success: false, error: healthResult.error }
        }
        
        // Monitor canary performance
        const monitorResult = this.monitorCanary(deployment)
        if (!monitorResult.success) {
            this.rollbackToEnvironment('canary', deployment)
            return { success: false, error: monitorResult.error }
        }
        
        // Deploy to production
        const prodResult = this.deployToEnvironment('production', deployment)
        if (!prodResult.success) {
            this.rollbackToEnvironment('canary', deployment)
            return { success: false, error: prodResult.error }
        }
        
        return { success: true }
    }
    
    recreateDeploy(deployment) {
        console.log("Executing recreate deployment...")
        
        // Stop current deployment
        const stopResult = this.stopDeployment(deployment.application)
        if (!stopResult.success) {
            return { success: false, error: stopResult.error }
        }
        
        // Deploy new version
        const deployResult = this.deployToEnvironment('production', deployment)
        if (!deployResult.success) {
            // Restart old deployment
            this.startDeployment(deployment.application)
            return { success: false, error: deployResult.error }
        }
        
        // Start new deployment
        const startResult = this.startDeployment(deployment.application)
        if (!startResult.success) {
            this.rollbackToEnvironment('production', deployment)
            return { success: false, error: startResult.error }
        }
        
        return { success: true }
    }
    
    rollback(deploymentId) {
        const deployment = this.deployments.get(deploymentId)
        if (!deployment) {
            throw new Error(`Deployment not found: ${deploymentId}`)
        }
        
        console.log(`Rolling back deployment: ${deploymentId}`)
        
        try {
            const result = this.executeRollback(deployment)
            
            if (result.success) {
                deployment.status = 'rolled_back'
                this.stats.rollbacks++
                console.log(`Rollback successful: ${deploymentId}`)
                this.sendNotification('rollback', deployment)
            } else {
                console.log(`Rollback failed: ${deploymentId}`)
                this.sendNotification('rollback_failure', deployment)
            }
            
            return result
        } catch (error) {
            console.log(`Rollback error: ${deploymentId}`)
            this.sendNotification('rollback_error', deployment)
            throw error
        }
    }
    
    generateDeploymentId() {
        return `deploy_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
    }
    
    sendNotification(type, deployment) {
        // Implementation for sending notifications
        console.log(`Notification: ${type} for deployment ${deployment.id}`)
    }
    
    getStats() {
        return { ...this.stats }
    }
    
    getDeployments() {
        return Array.from(this.deployments.values())
    }
}

# Initialize deployment manager
const deployManager = new DeploymentManager()
```

### **Blue-Green Deployment**
```bash
#!/bin/bash

# Blue-green deployment implementation
blue_green_deployment() {
    local application="$1"
    local version="$2"
    local options="$3"
    
    echo "Starting blue-green deployment for $application version $version"
    
    # Determine current active environment
    local current_env=$(get_active_environment "$application")
    local target_env=""
    
    if [[ "$current_env" == "blue" ]]; then
        target_env="green"
    else
        target_env="blue"
    fi
    
    echo "Current environment: $current_env"
    echo "Target environment: $target_env"
    
    # Create backup
    if [[ "$options" == *"backup"* ]]; then
        create_deployment_backup "$application" "$current_env"
    fi
    
    # Deploy to target environment
    local deploy_result=$(deploy_to_environment "$target_env" "$application" "$version")
    if [[ $? -ne 0 ]]; then
        echo "✗ Deployment to $target_env failed"
        return 1
    fi
    
    echo "✓ Deployment to $target_env successful"
    
    # Run health checks
    local health_result=$(run_health_checks "$target_env" "$application")
    if [[ $? -ne 0 ]]; then
        echo "✗ Health checks failed for $target_env"
        rollback_environment "$target_env" "$application"
        return 1
    fi
    
    echo "✓ Health checks passed for $target_env"
    
    # Switch traffic
    local switch_result=$(switch_traffic "$target_env" "$application")
    if [[ $? -ne 0 ]]; then
        echo "✗ Traffic switch failed"
        rollback_environment "$target_env" "$application"
        return 1
    fi
    
    echo "✓ Traffic switched to $target_env"
    
    # Decommission old environment
    decommission_environment "$current_env" "$application"
    
    echo "✓ Blue-green deployment completed successfully"
    return 0
}

deploy_to_environment() {
    local environment="$1"
    local application="$2"
    local version="$3"
    
    echo "Deploying $application version $version to $environment environment"
    
    # Create environment directory
    local env_dir="/var/www/$application/$environment"
    mkdir -p "$env_dir"
    
    # Download application package
    local package_url="${deploy_package_url:-https://packages.example.com}"
    local package_file="$env_dir/$application-$version.tar.gz"
    
    if ! curl -L "$package_url/$application-$version.tar.gz" -o "$package_file"; then
        echo "Failed to download package"
        return 1
    fi
    
    # Extract package
    if ! tar -xzf "$package_file" -C "$env_dir"; then
        echo "Failed to extract package"
        return 1
    fi
    
    # Set up application
    if ! setup_application "$env_dir" "$application" "$version"; then
        echo "Failed to set up application"
        return 1
    fi
    
    # Start application
    if ! start_application "$env_dir" "$application"; then
        echo "Failed to start application"
        return 1
    fi
    
    echo "✓ Deployment to $environment successful"
    return 0
}

run_health_checks() {
    local environment="$1"
    local application="$2"
    
    echo "Running health checks for $application in $environment environment"
    
    # Get application URL
    local app_url=$(get_application_url "$environment" "$application")
    
    # Check if application is responding
    local max_attempts=30
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        if curl -f -s "$app_url/health" >/dev/null 2>&1; then
            echo "✓ Health check passed on attempt $attempt"
            return 0
        fi
        
        echo "Health check attempt $attempt failed, retrying in 2 seconds..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    echo "✗ Health checks failed after $max_attempts attempts"
    return 1
}

switch_traffic() {
    local environment="$1"
    local application="$2"
    
    echo "Switching traffic to $environment environment for $application"
    
    # Update load balancer configuration
    local lb_config="/etc/nginx/sites-available/$application"
    local env_url=$(get_application_url "$environment" "$application")
    
    # Create new configuration
    cat > "$lb_config" << EOF
server {
    listen 80;
    server_name $application.example.com;
    
    location / {
        proxy_pass $env_url;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF
    
    # Test configuration
    if ! nginx -t; then
        echo "✗ Nginx configuration test failed"
        return 1
    fi
    
    # Reload nginx
    if ! systemctl reload nginx; then
        echo "✗ Failed to reload nginx"
        return 1
    fi
    
    echo "✓ Traffic switched to $environment"
    return 0
}

decommission_environment() {
    local environment="$1"
    local application="$2"
    
    echo "Decommissioning $environment environment for $application"
    
    # Stop application
    stop_application "/var/www/$application/$environment" "$application"
    
    # Archive old version
    local archive_dir="/var/archives/$application"
    mkdir -p "$archive_dir"
    
    local timestamp=$(date +%Y%m%d_%H%M%S)
    tar -czf "$archive_dir/$application-$environment-$timestamp.tar.gz" \
        -C "/var/www/$application" "$environment"
    
    # Remove old environment
    rm -rf "/var/www/$application/$environment"
    
    echo "✓ $environment environment decommissioned"
}

get_active_environment() {
    local application="$1"
    
    # Check which environment is currently active
    local blue_status=$(check_environment_status "blue" "$application")
    local green_status=$(check_environment_status "green" "$application")
    
    if [[ "$blue_status" == "active" ]]; then
        echo "blue"
    elif [[ "$green_status" == "active" ]]; then
        echo "green"
    else
        echo "blue"  # Default to blue
    fi
}

check_environment_status() {
    local environment="$1"
    local application="$2"
    
    local env_dir="/var/www/$application/$environment"
    
    if [[ -d "$env_dir" ]] && [[ -f "$env_dir/status" ]]; then
        cat "$env_dir/status"
    else
        echo "inactive"
    fi
}
```

### **Rolling Deployment**
```bash
#!/bin/bash

# Rolling deployment implementation
rolling_deployment() {
    local application="$1"
    local version="$2"
    local options="$3"
    
    echo "Starting rolling deployment for $application version $version"
    
    # Get current instances
    local instances=$(get_application_instances "$application")
    local total_instances=$(echo "$instances" | wc -l)
    local updated_instances=0
    
    echo "Found $total_instances instances to update"
    
    # Deploy to instances one by one
    while IFS= read -r instance; do
        if [[ -n "$instance" ]]; then
            echo "Updating instance: $instance"
            
            # Deploy to instance
            local deploy_result=$(deploy_to_instance "$instance" "$application" "$version")
            if [[ $? -ne 0 ]]; then
                echo "✗ Deployment to instance $instance failed"
                rollback_instance "$instance" "$application"
                return 1
            fi
            
            # Run health check
            local health_result=$(run_instance_health_check "$instance" "$application")
            if [[ $? -ne 0 ]]; then
                echo "✗ Health check failed for instance $instance"
                rollback_instance "$instance" "$application"
                return 1
            fi
            
            echo "✓ Instance $instance updated successfully"
            updated_instances=$((updated_instances + 1))
            
            # Wait between updates
            if [[ $updated_instances -lt $total_instances ]]; then
                echo "Waiting 30 seconds before next update..."
                sleep 30
            fi
        fi
    done <<< "$instances"
    
    echo "✓ Rolling deployment completed successfully"
    echo "Updated $updated_instances out of $total_instances instances"
    return 0
}

deploy_to_instance() {
    local instance="$1"
    local application="$2"
    local version="$3"
    
    echo "Deploying $application version $version to instance $instance"
    
    # Create deployment directory
    local deploy_dir="/var/www/$application/instances/$instance"
    mkdir -p "$deploy_dir"
    
    # Download application package
    local package_url="${deploy_package_url:-https://packages.example.com}"
    local package_file="$deploy_dir/$application-$version.tar.gz"
    
    if ! curl -L "$package_url/$application-$version.tar.gz" -o "$package_file"; then
        echo "Failed to download package"
        return 1
    fi
    
    # Stop application
    stop_instance_application "$instance" "$application"
    
    # Backup current version
    if [[ -d "$deploy_dir/current" ]]; then
        mv "$deploy_dir/current" "$deploy_dir/backup-$(date +%Y%m%d_%H%M%S)"
    fi
    
    # Extract new version
    if ! tar -xzf "$package_file" -C "$deploy_dir"; then
        echo "Failed to extract package"
        return 1
    fi
    
    # Set up application
    if ! setup_instance_application "$deploy_dir" "$application" "$version"; then
        echo "Failed to set up application"
        return 1
    fi
    
    # Start application
    if ! start_instance_application "$instance" "$application"; then
        echo "Failed to start application"
        return 1
    fi
    
    echo "✓ Deployment to instance $instance successful"
    return 0
}

run_instance_health_check() {
    local instance="$1"
    local application="$2"
    
    echo "Running health check for instance $instance"
    
    # Get instance URL
    local instance_url=$(get_instance_url "$instance" "$application")
    
    # Check if instance is responding
    local max_attempts=10
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        if curl -f -s "$instance_url/health" >/dev/null 2>&1; then
            echo "✓ Health check passed on attempt $attempt"
            return 0
        fi
        
        echo "Health check attempt $attempt failed, retrying in 5 seconds..."
        sleep 5
        attempt=$((attempt + 1))
    done
    
    echo "✗ Health check failed after $max_attempts attempts"
    return 1
}

rollback_instance() {
    local instance="$1"
    local application="$2"
    
    echo "Rolling back instance $instance"
    
    local deploy_dir="/var/www/$application/instances/$instance"
    
    # Stop current application
    stop_instance_application "$instance" "$application"
    
    # Restore backup
    local backup_dir=$(find "$deploy_dir" -name "backup-*" -type d | sort -r | head -1)
    if [[ -n "$backup_dir" ]]; then
        rm -rf "$deploy_dir/current"
        mv "$backup_dir" "$deploy_dir/current"
        
        # Start application
        start_instance_application "$instance" "$application"
        
        echo "✓ Instance $instance rolled back successfully"
    else
        echo "✗ No backup found for instance $instance"
        return 1
    fi
}

get_application_instances() {
    local application="$1"
    
    # Get instances from load balancer configuration
    local instances_file="/etc/loadbalancer/$application/instances"
    
    if [[ -f "$instances_file" ]]; then
        cat "$instances_file"
    else
        # Default instances
        echo "instance1.example.com"
        echo "instance2.example.com"
        echo "instance3.example.com"
    fi
}
```

### **Canary Deployment**
```bash
#!/bin/bash

# Canary deployment implementation
canary_deployment() {
    local application="$1"
    local version="$2"
    local options="$3"
    
    echo "Starting canary deployment for $application version $version"
    
    # Deploy to canary environment
    local canary_result=$(deploy_to_canary "$application" "$version")
    if [[ $? -ne 0 ]]; then
        echo "✗ Canary deployment failed"
        return 1
    fi
    
    echo "✓ Canary deployment successful"
    
    # Run health checks
    local health_result=$(run_canary_health_checks "$application")
    if [[ $? -ne 0 ]]; then
        echo "✗ Canary health checks failed"
        rollback_canary "$application"
        return 1
    fi
    
    echo "✓ Canary health checks passed"
    
    # Monitor canary performance
    local monitor_result=$(monitor_canary_performance "$application" "$options")
    if [[ $? -ne 0 ]]; then
        echo "✗ Canary performance monitoring failed"
        rollback_canary "$application"
        return 1
    fi
    
    echo "✓ Canary performance monitoring passed"
    
    # Deploy to production
    local prod_result=$(deploy_to_production "$application" "$version")
    if [[ $? -ne 0 ]]; then
        echo "✗ Production deployment failed"
        rollback_canary "$application"
        return 1
    fi
    
    echo "✓ Production deployment successful"
    
    # Clean up canary
    cleanup_canary "$application"
    
    echo "✓ Canary deployment completed successfully"
    return 0
}

deploy_to_canary() {
    local application="$1"
    local version="$2"
    
    echo "Deploying $application version $version to canary environment"
    
    # Create canary environment
    local canary_dir="/var/www/$application/canary"
    mkdir -p "$canary_dir"
    
    # Download application package
    local package_url="${deploy_package_url:-https://packages.example.com}"
    local package_file="$canary_dir/$application-$version.tar.gz"
    
    if ! curl -L "$package_url/$application-$version.tar.gz" -o "$package_file"; then
        echo "Failed to download package"
        return 1
    fi
    
    # Extract package
    if ! tar -xzf "$package_file" -C "$canary_dir"; then
        echo "Failed to extract package"
        return 1
    fi
    
    # Set up application
    if ! setup_canary_application "$canary_dir" "$application" "$version"; then
        echo "Failed to set up canary application"
        return 1
    fi
    
    # Start canary application
    if ! start_canary_application "$application"; then
        echo "Failed to start canary application"
        return 1
    fi
    
    # Route small percentage of traffic to canary
    route_traffic_to_canary "$application" 10  # 10% traffic
    
    echo "✓ Canary deployment successful"
    return 0
}

monitor_canary_performance() {
    local application="$1"
    local options="$2"
    
    echo "Monitoring canary performance for $application"
    
    # Set monitoring duration
    local duration="${canary_monitoring_duration:-300}"  # 5 minutes default
    local start_time=$(date +%s)
    local end_time=$((start_time + duration))
    
    # Performance thresholds
    local error_rate_threshold="${canary_error_rate_threshold:-5}"  # 5% default
    local response_time_threshold="${canary_response_time_threshold:-1000}"  # 1000ms default
    
    echo "Monitoring for ${duration} seconds..."
    echo "Error rate threshold: ${error_rate_threshold}%"
    echo "Response time threshold: ${response_time_threshold}ms"
    
    # Monitor performance
    local total_requests=0
    local error_requests=0
    local total_response_time=0
    
    while [[ $(date +%s) -lt $end_time ]]; do
        # Send test request
        local canary_url=$(get_canary_url "$application")
        local start_request=$(date +%s%N)
        
        local response=$(curl -s -w "%{http_code}" "$canary_url/test" 2>/dev/null)
        local http_code="${response: -3}"
        local response_time=$(( ( $(date +%s%N) - start_request ) / 1000000 ))
        
        total_requests=$((total_requests + 1))
        total_response_time=$((total_response_time + response_time))
        
        if [[ "$http_code" != "200" ]]; then
            error_requests=$((error_requests + 1))
        fi
        
        # Calculate current metrics
        local current_error_rate=0
        if [[ $total_requests -gt 0 ]]; then
            current_error_rate=$((error_requests * 100 / total_requests))
        fi
        
        local avg_response_time=0
        if [[ $total_requests -gt 0 ]]; then
            avg_response_time=$((total_response_time / total_requests))
        fi
        
        echo "Progress: $(( ( $(date +%s) - start_time ) * 100 / duration ))% - Error rate: ${current_error_rate}% - Avg response time: ${avg_response_time}ms"
        
        # Check thresholds
        if [[ $current_error_rate -gt $error_rate_threshold ]]; then
            echo "✗ Error rate threshold exceeded: ${current_error_rate}% > ${error_rate_threshold}%"
            return 1
        fi
        
        if [[ $avg_response_time -gt $response_time_threshold ]]; then
            echo "✗ Response time threshold exceeded: ${avg_response_time}ms > ${response_time_threshold}ms"
            return 1
        fi
        
        sleep 10
    done
    
    echo "✓ Canary performance monitoring passed"
    echo "Final metrics:"
    echo "  Total requests: $total_requests"
    echo "  Error rate: ${current_error_rate}%"
    echo "  Average response time: ${avg_response_time}ms"
    
    return 0
}

route_traffic_to_canary() {
    local application="$1"
    local percentage="$2"
    
    echo "Routing ${percentage}% of traffic to canary for $application"
    
    # Update load balancer configuration
    local lb_config="/etc/nginx/sites-available/$application"
    local prod_url=$(get_production_url "$application")
    local canary_url=$(get_canary_url "$application")
    
    # Create weighted configuration
    cat > "$lb_config" << EOF
upstream $application {
    server $prod_url weight=$((100 - percentage));
    server $canary_url weight=$percentage;
}

server {
    listen 80;
    server_name $application.example.com;
    
    location / {
        proxy_pass http://$application;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF
    
    # Test and reload configuration
    if nginx -t && systemctl reload nginx; then
        echo "✓ Traffic routing updated successfully"
        return 0
    else
        echo "✗ Failed to update traffic routing"
        return 1
    fi
}

cleanup_canary() {
    local application="$1"
    
    echo "Cleaning up canary environment for $application"
    
    # Stop canary application
    stop_canary_application "$application"
    
    # Remove canary traffic routing
    route_traffic_to_canary "$application" 0
    
    # Archive canary
    local archive_dir="/var/archives/$application"
    mkdir -p "$archive_dir"
    
    local timestamp=$(date +%Y%m%d_%H%M%S)
    tar -czf "$archive_dir/$application-canary-$timestamp.tar.gz" \
        -C "/var/www/$application" "canary"
    
    # Remove canary directory
    rm -rf "/var/www/$application/canary"
    
    echo "✓ Canary environment cleaned up"
}
```

## 🎯 **Real-World Configuration Examples**

### **Complete Deployment Configuration**
```bash
# deployment-config.tsk
deployment_config:
  enabled: true
  strategy: blue-green
  environment: production
  rollback: automatic

#deploy: enabled
#deploy-enabled: true
#deploy-strategy: blue-green
#deploy-environment: production
#deploy-rollback: automatic

#deploy-health-check: true
#deploy-scaling: auto
#deploy-backup: true
#deploy-notifications: slack
#deploy-artifacts: true
#deploy-security: true

#deploy-config:
#  strategies:
#    blue-green:
#      enabled: true
#      health_check_timeout: 300
#      traffic_switch_delay: 30
#    rolling:
#      enabled: true
#      max_unavailable: 1
#      max_surge: 1
#      update_period: 30
#    canary:
#      enabled: true
#      traffic_percentage: 10
#      monitoring_duration: 300
#      error_rate_threshold: 5
#      response_time_threshold: 1000
#  environments:
#    development:
#      url: "dev.example.com"
#      instances: 2
#      auto_scaling: false
#    staging:
#      url: "staging.example.com"
#      instances: 3
#      auto_scaling: true
#    production:
#      url: "prod.example.com"
#      instances: 5
#      auto_scaling: true
#  health_checks:
#    enabled: true
#    endpoint: "/health"
#    timeout: 30
#    interval: 10
#    max_attempts: 3
#  notifications:
#    slack:
#      webhook: "${SLACK_WEBHOOK}"
#      channel: "#deployments"
#    email:
#      recipients: ["ops@example.com"]
#      smtp_server: "smtp.example.com"
#  security:
#    enabled: true
#    vulnerability_scan: true
#    secrets_management: true
#    access_control: true
```

### **Multi-Environment Deployment**
```bash
# multi-env-deployment.tsk
multi_env_config:
  environments:
    - name: development
      strategy: rolling
      instances: 2
    - name: staging
      strategy: blue-green
      instances: 3
    - name: production
      strategy: canary
      instances: 5

#deploy-dev: rolling
#deploy-staging: blue-green
#deploy-prod: canary

#deploy-config:
#  environments:
#    development:
#      strategy: rolling
#      instances: 2
#      health_check: true
#      auto_rollback: true
#      notifications: false
#    staging:
#      strategy: blue-green
#      instances: 3
#      health_check: true
#      auto_rollback: true
#      notifications: true
#    production:
#      strategy: canary
#      instances: 5
#      health_check: true
#      auto_rollback: true
#      notifications: true
#      monitoring:
#        duration: 600
#        error_rate_threshold: 2
#        response_time_threshold: 500
#  pipeline:
#    stages:
#      - name: build
#        command: "make build"
#        artifacts: ["dist/"]
#      - name: test
#        command: "make test"
#        required: true
#      - name: security_scan
#        command: "make security-scan"
#        required: true
#      - name: deploy_dev
#        environment: development
#        auto_approve: true
#      - name: deploy_staging
#        environment: staging
#        auto_approve: false
#      - name: deploy_prod
#        environment: production
#        auto_approve: false
```

## 🚨 **Troubleshooting Deployment Strategies**

### **Common Issues and Solutions**

**1. Deployment Failures**
```bash
# Debug deployment issues
debug_deployment() {
    local deployment_id="$1"
    
    echo "Debugging deployment: $deployment_id"
    
    # Check deployment status
    local deployment=$(get_deployment_info "$deployment_id")
    if [[ -z "$deployment" ]]; then
        echo "✗ Deployment not found: $deployment_id"
        return 1
    fi
    
    echo "Deployment info:"
    echo "  Application: ${deployment[application]}"
    echo "  Version: ${deployment[version]}"
    echo "  Strategy: ${deployment[strategy]}"
    echo "  Status: ${deployment[status]}"
    echo "  Start time: ${deployment[start_time]}"
    
    # Check deployment logs
    local log_file="/var/log/deployments/$deployment_id.log"
    if [[ -f "$log_file" ]]; then
        echo "Deployment logs:"
        tail -20 "$log_file"
    else
        echo "⚠ No deployment logs found"
    fi
    
    # Check application status
    check_application_status "${deployment[application]}"
    
    # Check environment status
    check_environment_status "${deployment[strategy]}" "${deployment[application]}"
}

check_application_status() {
    local application="$1"
    
    echo "Checking application status: $application"
    
    # Check if application is running
    if systemctl is-active --quiet "$application"; then
        echo "✓ Application service is running"
    else
        echo "✗ Application service is not running"
    fi
    
    # Check application health
    local health_url=$(get_application_health_url "$application")
    if curl -f -s "$health_url" >/dev/null 2>&1; then
        echo "✓ Application health check passed"
    else
        echo "✗ Application health check failed"
    fi
    
    # Check application logs
    local log_file="/var/log/$application/application.log"
    if [[ -f "$log_file" ]]; then
        echo "Recent application logs:"
        tail -10 "$log_file"
    fi
}
```

**2. Rollback Issues**
```bash
# Debug rollback issues
debug_rollback() {
    local deployment_id="$1"
    
    echo "Debugging rollback for deployment: $deployment_id"
    
    # Check rollback status
    local rollback_status=$(get_rollback_status "$deployment_id")
    echo "Rollback status: $rollback_status"
    
    # Check backup availability
    local backup_available=$(check_backup_availability "$deployment_id")
    if [[ "$backup_available" == "true" ]]; then
        echo "✓ Backup available for rollback"
    else
        echo "✗ No backup available for rollback"
    fi
    
    # Check environment state
    local environment_state=$(get_environment_state "$deployment_id")
    echo "Environment state: $environment_state"
    
    # Check rollback logs
    local rollback_log="/var/log/deployments/rollback-$deployment_id.log"
    if [[ -f "$rollback_log" ]]; then
        echo "Rollback logs:"
        tail -20 "$rollback_log"
    else
        echo "⚠ No rollback logs found"
    fi
}
```

## 🔒 **Security Best Practices**

### **Deployment Security Checklist**
```bash
# Security validation
validate_deployment_security() {
    echo "Validating deployment security configuration..."
    
    # Check deployment permissions
    if [[ "${deploy_security_enabled}" == "true" ]]; then
        echo "✓ Deployment security enabled"
        
        # Check access control
        if [[ -n "${deploy_access_control}" ]]; then
            echo "✓ Access control configured"
        else
            echo "⚠ Access control not configured"
        fi
        
        # Check secrets management
        if [[ "${deploy_secrets_management}" == "true" ]]; then
            echo "✓ Secrets management enabled"
        else
            echo "⚠ Secrets management not enabled"
        fi
    else
        echo "⚠ Deployment security not enabled"
    fi
    
    # Check vulnerability scanning
    if [[ "${deploy_vulnerability_scan}" == "true" ]]; then
        echo "✓ Vulnerability scanning enabled"
    else
        echo "⚠ Vulnerability scanning not enabled"
    fi
    
    # Check backup security
    if [[ "${deploy_backup}" == "true" ]]; then
        echo "✓ Deployment backups enabled"
        
        if [[ "${deploy_backup_encryption}" == "true" ]]; then
            echo "✓ Backup encryption enabled"
        else
            echo "⚠ Backup encryption not enabled"
        fi
    else
        echo "⚠ Deployment backups not enabled"
    fi
}
```

## 📈 **Performance Optimization Tips**

### **Deployment Performance Checklist**
```bash
# Performance validation
validate_deployment_performance() {
    echo "Validating deployment performance configuration..."
    
    # Check deployment strategy performance
    case "${deploy_strategy}" in
        "blue-green")
            echo "✓ Blue-green deployment (zero downtime)"
            ;;
        "rolling")
            echo "✓ Rolling deployment (minimal downtime)"
            ;;
        "canary")
            echo "✓ Canary deployment (risk mitigation)"
            ;;
        *)
            echo "⚠ Unknown deployment strategy: ${deploy_strategy}"
            ;;
    esac
    
    # Check health check configuration
    if [[ "${deploy_health_check}" == "true" ]]; then
        echo "✓ Health checks enabled"
        
        if [[ -n "${deploy_health_check_timeout}" ]]; then
            echo "  Health check timeout: ${deploy_health_check_timeout}s"
        else
            echo "⚠ Health check timeout not configured"
        fi
    else
        echo "⚠ Health checks not enabled"
    fi
    
    # Check auto-scaling
    if [[ "${deploy_scaling}" == "auto" ]]; then
        echo "✓ Auto-scaling enabled"
    else
        echo "⚠ Auto-scaling not enabled"
    fi
    
    # Check monitoring
    if [[ "${deploy_monitoring}" == "true" ]]; then
        echo "✓ Deployment monitoring enabled"
    else
        echo "⚠ Deployment monitoring not enabled"
    fi
}
```

## 🎯 **Next Steps**

- **Security Best Practices**: Learn about deployment security
- **Plugin Integration**: Explore deployment plugins
- **Advanced Patterns**: Understand complex deployment patterns
- **Continuous Deployment**: Implement continuous deployment strategies
- **Monitoring and Alerting**: Set up deployment monitoring

---

**Deployment strategies transform your TuskLang configuration into a powerful deployment system. They bring modern deployment capabilities to your Bash applications with intelligent deployment strategies, automatic rollbacks, and comprehensive monitoring!** 
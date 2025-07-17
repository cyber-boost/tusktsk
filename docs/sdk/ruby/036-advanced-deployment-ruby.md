# 🚀 TuskLang Ruby Advanced Deployment Guide

**"We don't bow to any king" - Ruby Edition**

Master advanced deployment strategies for TuskLang-powered Ruby applications. Learn blue/green, canary, rolling updates, and zero-downtime deployments.

## 🟦 Blue/Green Deployment

### 1. Strategy Overview
```ruby
# config/blue_green.tsk
[blue_green]
enabled: true
current_environment: @env("CURRENT_ENV", "blue")
switch_threshold: 0.95  # 95% success rate required

[environments]
blue {
    host: "blue.myapp.com"
    port: 3000
    database: "myapp_blue"
    redis: "redis-blue"
}
green {
    host: "green.myapp.com"
    port: 3001
    database: "myapp_green"
    redis: "redis-green"
}

[health_checks]
blue_health: @health.check("blue", @http("GET", "https://blue.myapp.com/health"))
green_health: @health.check("green", @http("GET", "https://green.myapp.com/health"))
```

### 2. Docker Compose Implementation
```yaml
# docker-compose.blue-green.yml
version: '3.8'
services:
  app-blue:
    image: myapp:blue
    environment:
      - CURRENT_ENV=blue
      - DATABASE_URL=postgresql://postgres:secret@postgres-blue:5432/myapp_blue
    ports:
      - "3000:3000"
    depends_on:
      - postgres-blue
      - redis-blue

  app-green:
    image: myapp:green
    environment:
      - CURRENT_ENV=green
      - DATABASE_URL=postgresql://postgres:secret@postgres-green:5432/myapp_green
    ports:
      - "3001:3000"
    depends_on:
      - postgres-green
      - redis-green

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - app-blue
      - app-green
```

### 3. Nginx Configuration
```nginx
# nginx.conf
upstream blue {
    server app-blue:3000;
}

upstream green {
    server app-green:3000;
}

server {
    listen 80;
    
    location / {
        # Switch between blue and green based on environment variable
        proxy_pass http://$current_upstream;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## 🟨 Canary Deployment

### 1. Strategy Configuration
```ruby
# config/canary.tsk
[canary]
enabled: true
traffic_percentage: @env("CANARY_TRAFFIC_PERCENTAGE", 10)
success_threshold: 0.98  # 98% success rate required
failure_threshold: 0.05  # 5% failure rate triggers rollback
evaluation_period: "5m"

[canary_metrics]
response_time: @metrics("canary_response_time_ms", @request.response_time)
error_rate: @metrics("canary_error_rate", @request.error_rate)
throughput: @metrics("canary_throughput", @request.requests_per_second)

[rollback_triggers]
high_error_rate: @alert("canary_error_rate > 0.05", {
    action: "rollback",
    severity: "critical"
})
high_response_time: @alert("canary_response_time_ms > 1000", {
    action: "rollback",
    severity: "warning"
})
```

### 2. Kubernetes Implementation
```yaml
# k8s/canary-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp-canary
spec:
  replicas: 1
  selector:
    matchLabels:
      app: myapp
      track: canary
  template:
    metadata:
      labels:
        app: myapp
        track: canary
    spec:
      containers:
      - name: myapp
        image: myapp:canary
        env:
        - name: CANARY_TRAFFIC_PERCENTAGE
          value: "10"
        - name: CANARY_METRICS_ENABLED
          value: "true"
        ports:
        - containerPort: 3000
        livenessProbe:
          httpGet:
            path: /health
            port: 3000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 3000
          initialDelaySeconds: 5
          periodSeconds: 5
```

### 3. Traffic Splitting
```yaml
# k8s/traffic-split.yaml
apiVersion: split.smi-spec.io/v1alpha3
kind: TrafficSplit
metadata:
  name: myapp-split
spec:
  service: myapp
  backends:
  - service: myapp-stable
    weight: 90
  - service: myapp-canary
    weight: 10
```

## 🔄 Rolling Updates

### 1. Strategy Configuration
```ruby
# config/rolling_update.tsk
[rolling_update]
enabled: true
max_surge: 1
max_unavailable: 0
min_ready_seconds: 30
progress_deadline_seconds: 600

[update_strategy]
type: "RollingUpdate"
rolling_update {
    max_surge: 1
    max_unavailable: 0
}

[health_checks]
pod_health: @health.check("pod", @http("GET", "/health"))
service_health: @health.check("service", @http("GET", "/ready"))
```

### 2. Kubernetes Rolling Update
```yaml
# k8s/rolling-update.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
    spec:
      containers:
      - name: myapp
        image: myapp:latest
        ports:
        - containerPort: 3000
        readinessProbe:
          httpGet:
            path: /ready
            port: 3000
          initialDelaySeconds: 5
          periodSeconds: 5
        livenessProbe:
          httpGet:
            path: /health
            port: 3000
          initialDelaySeconds: 30
          periodSeconds: 10
```

## ⚡ Zero-Downtime Deployment

### 1. Strategy Configuration
```ruby
# config/zero_downtime.tsk
[zero_downtime]
enabled: true
grace_period: "30s"
health_check_interval: "5s"
max_health_check_failures: 3

[deployment_steps]
pre_deploy: [
    "backup_database",
    "validate_config",
    "run_migrations"
]
deploy: [
    "start_new_instances",
    "wait_for_health",
    "switch_traffic",
    "stop_old_instances"
]
post_deploy: [
    "verify_deployment",
    "cleanup_backups"
]

[rollback]
enabled: true
automatic_rollback: true
rollback_threshold: 0.05  # 5% error rate triggers rollback
```

### 2. Capistrano Implementation
```ruby
# config/deploy.rb
set :application, 'myapp'
set :repo_url, 'git@github.com:user/myapp.git'
set :deploy_to, '/var/www/myapp'
set :keep_releases, 5
set :linked_files, fetch(:linked_files, []).push('config/app.tsk')
set :linked_dirs, fetch(:linked_dirs, []).push('log', 'tmp/pids', 'tmp/cache', 'tmp/sockets')

namespace :deploy do
  desc 'Restart application'
  task :restart do
    on roles(:app), in: :sequence, wait: 5 do
      execute :touch, release_path.join('tmp/restart.txt')
    end
  end

  desc 'Pre-deploy tasks'
  task :pre_deploy do
    on roles(:app) do
      within release_path do
        execute :rake, 'db:backup'
        execute :rake, 'tusk:validate'
        execute :rake, 'db:migrate'
      end
    end
  end

  desc 'Post-deploy tasks'
  task :post_deploy do
    on roles(:app) do
      within release_path do
        execute :rake, 'deploy:verify'
        execute :rake, 'deploy:cleanup'
      end
    end
  end

  after :publishing, :restart
  before :publishing, :pre_deploy
  after :publishing, :post_deploy
end
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/deployment_service.rb
require 'tusklang'

class DeploymentService
  def self.load_deployment_config
    parser = TuskLang.new
    parser.parse_file('config/deployment.tsk')
  end

  def self.execute_blue_green_deployment
    config = load_deployment_config
    
    if config['blue_green']['enabled']
      current_env = config['blue_green']['current_environment']
      target_env = current_env == 'blue' ? 'green' : 'blue'
      
      # Deploy to target environment
      deploy_to_environment(target_env)
      
      # Run health checks
      health_check = config['health_checks']["#{target_env}_health"]
      
      if health_check
        # Switch traffic
        switch_traffic_to(target_env)
        update_current_environment(target_env)
      else
        Rails.logger.error("Health check failed for #{target_env}")
        rollback_deployment(target_env)
      end
    end
  end

  def self.execute_canary_deployment
    config = load_deployment_config
    
    if config['canary']['enabled']
      traffic_percentage = config['canary']['traffic_percentage']
      
      # Deploy canary
      deploy_canary()
      
      # Monitor metrics
      monitor_canary_metrics(config['canary_metrics'])
      
      # Check rollback triggers
      check_rollback_triggers(config['rollback_triggers'])
    end
  end

  def self.execute_rolling_update
    config = load_deployment_config
    
    if config['rolling_update']['enabled']
      max_surge = config['rolling_update']['max_surge']
      max_unavailable = config['rolling_update']['max_unavailable']
      
      # Execute rolling update
      rolling_update(max_surge, max_unavailable)
    end
  end

  private

  def self.deploy_to_environment(environment)
    # Implementation for deploying to specific environment
    Rails.logger.info("Deploying to #{environment}")
  end

  def self.switch_traffic_to(environment)
    # Implementation for switching traffic
    Rails.logger.info("Switching traffic to #{environment}")
  end

  def self.update_current_environment(environment)
    # Update environment variable
    ENV['CURRENT_ENV'] = environment
  end

  def self.rollback_deployment(environment)
    # Implementation for rollback
    Rails.logger.error("Rolling back #{environment} deployment")
  end

  def self.deploy_canary
    # Implementation for canary deployment
    Rails.logger.info("Deploying canary")
  end

  def self.monitor_canary_metrics(metrics)
    # Implementation for monitoring canary metrics
    Rails.logger.info("Monitoring canary metrics")
  end

  def self.check_rollback_triggers(triggers)
    # Implementation for checking rollback triggers
    Rails.logger.info("Checking rollback triggers")
  end

  def self.rolling_update(max_surge, max_unavailable)
    # Implementation for rolling update
    Rails.logger.info("Executing rolling update")
  end
end

# Usage
DeploymentService.execute_blue_green_deployment
DeploymentService.execute_canary_deployment
DeploymentService.execute_rolling_update
```

## 🛡️ Best Practices
- Always validate configs before deployment.
- Use health checks to ensure deployment success.
- Implement automatic rollback for failed deployments.
- Monitor deployment metrics and performance.
- Test deployment strategies in staging environments.

**Ready to deploy like a pro? Let's Tusk! 🚀** 
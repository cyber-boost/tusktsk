# 📈 TuskLang Ruby Advanced Scaling Guide

**"We don't bow to any king" - Ruby Edition**

Scale TuskLang-powered Ruby applications to handle millions of requests. Master horizontal scaling, auto-scaling, and load balancing strategies.

## 🔄 Horizontal Scaling

### 1. Multi-Instance Configuration
```ruby
# config/horizontal_scaling.tsk
[horizontal_scaling]
enabled: true
instance_count: @env("INSTANCE_COUNT", 3)
instance_type: @env("INSTANCE_TYPE", "t3.medium")

[instances]
instance_1: {
    host: "app-1.myapp.com"
    port: 3000
    region: "us-east-1"
    zone: "us-east-1a"
}
instance_2: {
    host: "app-2.myapp.com"
    port: 3000
    region: "us-east-1"
    zone: "us-east-1b"
}
instance_3: {
    host: "app-3.myapp.com"
    port: 3000
    region: "us-east-1"
    zone: "us-east-1c"
}

[load_balancer]
type: "application"
algorithm: "round_robin"
health_check_path: "/health"
health_check_interval: "30s"
unhealthy_threshold: 2
healthy_threshold: 2
```

### 2. Database Scaling
```ruby
# config/database_scaling.tsk
[database_scaling]
enabled: true
read_replicas: @env("DB_READ_REPLICAS", 2)

[primary_db]
host: "db-primary.myapp.com"
port: 5432
name: "myapp"
role: "master"

[read_replicas]
replica_1: {
    host: "db-replica-1.myapp.com"
    port: 5432
    name: "myapp"
    role: "slave"
    lag_threshold: "5s"
}
replica_2: {
    host: "db-replica-2.myapp.com"
    port: 5432
    name: "myapp"
    role: "slave"
    lag_threshold: "5s"
}

[connection_pool]
primary_pool: {
    min_size: 5
    max_size: 20
    idle_timeout: "300s"
}
replica_pool: {
    min_size: 3
    max_size: 10
    idle_timeout: "300s"
}
```

## 🤖 Auto-Scaling

### 1. Auto-Scaling Configuration
```ruby
# config/auto_scaling.tsk
[auto_scaling]
enabled: true
min_instances: @env("MIN_INSTANCES", 2)
max_instances: @env("MAX_INSTANCES", 10)
target_cpu_utilization: 70
target_memory_utilization: 80
scale_up_cooldown: "300s"
scale_down_cooldown: "600s"

[scaling_metrics]
cpu_utilization: @metrics("cpu_utilization_percent", @system.cpu_usage)
memory_utilization: @metrics("memory_utilization_percent", @system.memory_usage)
request_count: @metrics("request_count_per_second", @request.count_per_second)
response_time: @metrics("response_time_ms", @request.response_time)
queue_length: @metrics("queue_length", @queue.length)

[scaling_policies]
scale_up: @alert("cpu_utilization_percent > 70 OR memory_utilization_percent > 80", {
    action: "scale_up",
    instances: 1
})
scale_down: @alert("cpu_utilization_percent < 30 AND memory_utilization_percent < 50", {
    action: "scale_down",
    instances: 1
})
```

### 2. Kubernetes HPA
```yaml
# k8s/horizontal-pod-autoscaler.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: myapp-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: myapp
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleUp:
      stabilizationWindowSeconds: 300
    scaleDown:
      stabilizationWindowSeconds: 600
```

## ⚖️ Load Balancing

### 1. Load Balancer Configuration
```ruby
# config/load_balancer.tsk
[load_balancer]
type: "application"
algorithm: "least_connections"
health_check {
    path: "/health"
    interval: "30s"
    timeout: "5s"
    healthy_threshold: 2
    unhealthy_threshold: 3
}

[backend_servers]
server_1: {
    host: "app-1.myapp.com"
    port: 3000
    weight: 1
    max_connections: 1000
}
server_2: {
    host: "app-2.myapp.com"
    port: 3000
    weight: 1
    max_connections: 1000
}
server_3: {
    host: "app-3.myapp.com"
    port: 3000
    weight: 1
    max_connections: 1000
}

[ssl]
enabled: true
certificate: "/etc/ssl/certs/myapp.crt"
private_key: "/etc/ssl/private/myapp.key"
protocols: ["TLSv1.2", "TLSv1.3"]
```

### 2. Nginx Load Balancer
```nginx
# nginx.conf
upstream backend {
    least_conn;
    server app-1.myapp.com:3000 max_conns=1000;
    server app-2.myapp.com:3000 max_conns=1000;
    server app-3.myapp.com:3000 max_conns=1000;
    
    keepalive 32;
}

server {
    listen 80;
    server_name myapp.com;
    
    location / {
        proxy_pass http://backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        proxy_connect_timeout 30s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;
    }
    
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}
```

## 🗄️ Database Scaling

### 1. Read Replica Configuration
```ruby
# config/database_scaling.tsk
[database_scaling]
enabled: true
read_replicas: @env("DB_READ_REPLICAS", 2)

[primary_db]
host: "db-primary.myapp.com"
port: 5432
name: "myapp"
user: "postgres"
password: @env.secure("DB_PASSWORD")

[read_replicas]
replica_1: {
    host: "db-replica-1.myapp.com"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env.secure("DB_PASSWORD")
    lag_threshold: "5s"
}
replica_2: {
    host: "db-replica-2.myapp.com"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env.secure("DB_PASSWORD")
    lag_threshold: "5s"
}

[query_routing]
# Route read queries to replicas
read_queries: [
    "SELECT",
    "SHOW",
    "DESCRIBE",
    "EXPLAIN"
]
# Route write queries to primary
write_queries: [
    "INSERT",
    "UPDATE",
    "DELETE",
    "CREATE",
    "ALTER",
    "DROP"
]
```

### 2. Database Connection Pooling
```ruby
# config/connection_pool.tsk
[connection_pool]
enabled: true

[primary_pool]
min_size: 5
max_size: 20
idle_timeout: "300s"
max_lifetime: "3600s"
check_interval: "60s"

[replica_pool]
min_size: 3
max_size: 10
idle_timeout: "300s"
max_lifetime: "3600s"
check_interval: "60s"

[pool_monitoring]
active_connections: @metrics("db_active_connections", @db.active_connections)
idle_connections: @metrics("db_idle_connections", @db.idle_connections)
waiting_connections: @metrics("db_waiting_connections", @db.waiting_connections)
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/scaling_service.rb
require 'tusklang'

class ScalingService
  def self.load_scaling_config
    parser = TuskLang.new
    parser.parse_file('config/scaling.tsk')
  end

  def self.get_available_instances
    config = load_scaling_config
    config['instances'].keys
  end

  def self.get_load_balanced_instance
    config = load_scaling_config
    instances = config['instances']
    
    # Simple round-robin load balancing
    current_instance = Rails.cache.read('current_instance') || 0
    instance_keys = instances.keys
    next_instance = instance_keys[current_instance % instance_keys.length]
    
    Rails.cache.write('current_instance', current_instance + 1)
    instances[next_instance]
  end

  def self.check_auto_scaling_metrics
    config = load_scaling_config
    
    if config['auto_scaling']['enabled']
      cpu_utilization = config['scaling_metrics']['cpu_utilization']
      memory_utilization = config['scaling_metrics']['memory_utilization']
      
      # Check scale up conditions
      if cpu_utilization > config['auto_scaling']['target_cpu_utilization'] ||
         memory_utilization > config['auto_scaling']['target_memory_utilization']
        scale_up_instances(1)
      end
      
      # Check scale down conditions
      if cpu_utilization < 30 && memory_utilization < 50
        scale_down_instances(1)
      end
    end
  end

  def self.get_database_connection(type = :primary)
    config = load_scaling_config
    
    case type
    when :primary
      db_config = config['primary_db']
    when :replica
      # Select least loaded replica
      replicas = config['read_replicas']
      db_config = select_least_loaded_replica(replicas)
    end
    
    establish_database_connection(db_config)
  end

  private

  def self.scale_up_instances(count)
    # Implementation for scaling up instances
    Rails.logger.info("Scaling up #{count} instances")
  end

  def self.scale_down_instances(count)
    # Implementation for scaling down instances
    Rails.logger.info("Scaling down #{count} instances")
  end

  def self.select_least_loaded_replica(replicas)
    # Implementation for selecting least loaded replica
    replicas.values.min_by { |replica| replica['active_connections'] || 0 }
  end

  def self.establish_database_connection(db_config)
    # Implementation for establishing database connection
    ActiveRecord::Base.establish_connection(
      adapter: 'postgresql',
      host: db_config['host'],
      port: db_config['port'],
      database: db_config['name'],
      username: db_config['user'],
      password: db_config['password']
    )
  end
end

# Usage
instances = ScalingService.get_available_instances
instance = ScalingService.get_load_balanced_instance
ScalingService.check_auto_scaling_metrics
db_connection = ScalingService.get_database_connection(:replica)
```

## 🛡️ Best Practices
- Use horizontal scaling for stateless applications.
- Implement auto-scaling based on metrics and demand.
- Use load balancing for high availability.
- Scale databases with read replicas and connection pooling.
- Monitor scaling metrics and performance.

**Ready to scale to infinity? Let's Tusk! 🚀** 
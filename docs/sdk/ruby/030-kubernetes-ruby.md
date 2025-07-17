# ☸️ TuskLang Ruby Kubernetes Guide

**"We don't bow to any king" - Ruby Edition**

Deploy TuskLang-powered Ruby apps to Kubernetes. Learn ConfigMaps, Secrets, Helm charts, and best practices for containerized Ruby applications.

## 🔧 ConfigMaps

### 1. TuskLang ConfigMap
```yaml
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: tusklang-config
data:
  app.tsk: |
    $app_name: "MyApp"
    $version: "1.0.0"
    
    [database]
    host: "postgres-service"
    port: 5432
    name: "myapp"
    
    [server]
    host: "0.0.0.0"
    port: 3000
    workers: 4
```

### 2. Mount ConfigMap in Pod
```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-ruby-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusklang-ruby-app
  template:
    metadata:
      labels:
        app: tusklang-ruby-app
    spec:
      containers:
      - name: app
        image: myapp:latest
        ports:
        - containerPort: 3000
        volumeMounts:
        - name: config-volume
          mountPath: /app/config
      volumes:
      - name: config-volume
        configMap:
          name: tusklang-config
```

## 🔐 Secrets

### 1. TuskLang Secret
```yaml
# k8s/secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: tusklang-secrets
type: Opaque
data:
  database-password: <base64-encoded-password>
  api-key: <base64-encoded-api-key>
```

### 2. Use Secrets in Config
```ruby
# config/app.tsk
[database]
host: "postgres-service"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DATABASE_PASSWORD")

[api]
key: @env("API_KEY")
```

### 3. Mount Secrets in Pod
```yaml
# k8s/deployment.yaml
spec:
  template:
    spec:
      containers:
      - name: app
        env:
        - name: DATABASE_PASSWORD
          valueFrom:
            secretKeyRef:
              name: tusklang-secrets
              key: database-password
        - name: API_KEY
          valueFrom:
            secretKeyRef:
              name: tusklang-secrets
              key: api-key
```

## 🎯 Helm Charts

### 1. Helm Chart Structure
```
myapp/
├── Chart.yaml
├── values.yaml
├── templates/
│   ├── configmap.yaml
│   ├── secret.yaml
│   ├── deployment.yaml
│   └── service.yaml
└── config/
    └── app.tsk
```

### 2. Values File
```yaml
# values.yaml
app:
  name: "MyApp"
  version: "1.0.0"

database:
  host: "postgres-service"
  port: 5432
  name: "myapp"

server:
  port: 3000
  workers: 4

replicaCount: 3
```

### 3. ConfigMap Template
```yaml
# templates/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: {{ include "myapp.fullname" . }}-config
data:
  app.tsk: |
    $app_name: "{{ .Values.app.name }}"
    $version: "{{ .Values.app.version }}"
    
    [database]
    host: "{{ .Values.database.host }}"
    port: {{ .Values.database.port }}
    name: "{{ .Values.database.name }}"
    
    [server]
    host: "0.0.0.0"
    port: {{ .Values.server.port }}
    workers: {{ .Values.server.workers }}
```

## 🛠️ Ruby Integration Example
```ruby
# app/services/k8s_config.rb
require 'tusklang'

class K8sConfig
  def self.load_config
    parser = TuskLang.new
    parser.parse_file('/app/config/app.tsk')
  end
end

# Usage in Rails app
config = K8sConfig.load_config
puts "App: #{config['app_name']} v#{config['version']}"
puts "Database: #{config['database']['host']}:#{config['database']['port']}"
```

## 🛡️ Best Practices
- Use ConfigMaps for non-sensitive configs.
- Use Secrets for sensitive data (passwords, API keys).
- Use Helm for templating and versioning.
- Monitor pod health and config changes.

**Ready to orchestrate with Kubernetes? Let's Tusk! 🚀** 
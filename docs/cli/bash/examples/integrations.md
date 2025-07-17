# Framework Integrations

Integration examples for popular frameworks and tools with the TuskLang Bash CLI.

## Web Application

### Express.js Integration
```bash
#!/bin/bash
# express_app.sh

echo "🚀 Starting Express.js application..."

# Load configuration
APP_NAME=$(tsk config get app.name --quiet)
SERVER_PORT=$(tsk config get server.port --quiet)
DB_PATH=$(tsk config get database.path --quiet)

# Check database status
if ! tsk db status --quiet; then
    echo "Initializing database..."
    tsk db init
fi

# Create Express.js app
cat > app.js << 'EOF'
const express = require('express');
const app = express();

// Load configuration from TuskLang
const { execSync } = require('child_process');
const config = {};

try {
    const output = execSync('tsk peanuts load peanu.pnt', { encoding: 'utf8' });
    output.split('\n').forEach(line => {
        const [key, value] = line.split('=');
        if (key && value) {
            config[key.trim()] = value.trim();
        }
    });
} catch (error) {
    console.error('Failed to load configuration:', error);
    process.exit(1);
}

const PORT = config['server.port'] || 3000;

app.get('/', (req, res) => {
    res.json({
        app: config['app.name'],
        version: config['app.version'],
        environment: config['app.environment']
    });
});

app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});
EOF

# Install dependencies
npm install express

# Start application
echo "Starting $APP_NAME on port $SERVER_PORT"
node app.js
```

### Python Flask Integration
```bash
#!/bin/bash
# flask_app.sh

echo "🐍 Starting Flask application..."

# Load configuration
APP_NAME=$(tsk config get app.name --quiet)
SERVER_PORT=$(tsk config get server.port --quiet)

# Create Flask app
cat > app.py << 'EOF'
from flask import Flask, jsonify
import subprocess
import os

app = Flask(__name__)

def load_config():
    """Load configuration from TuskLang"""
    try:
        result = subprocess.run(['tsk', 'peanuts', 'load', 'peanu.pnt'], 
                              capture_output=True, text=True, check=True)
        config = {}
        for line in result.stdout.split('\n'):
            if '=' in line:
                key, value = line.split('=', 1)
                config[key.strip()] = value.strip()
        return config
    except subprocess.CalledProcessError as e:
        print(f"Failed to load configuration: {e}")
        return {}

config = load_config()

@app.route('/')
def home():
    return jsonify({
        'app': config.get('app.name', 'Unknown'),
        'version': config.get('app.version', 'Unknown'),
        'environment': config.get('app.environment', 'Unknown')
    })

@app.route('/health')
def health():
    return jsonify({'status': 'healthy'})

if __name__ == '__main__':
    port = int(config.get('server.port', 3000))
    app.run(host='0.0.0.0', port=port)
EOF

# Install dependencies
pip install flask

# Start application
echo "Starting $APP_NAME on port $SERVER_PORT"
python app.py
```

## Microservices

### Docker Integration
```bash
#!/bin/bash
# docker_integration.sh

echo "🐳 Setting up Docker integration..."

# Create Dockerfile
cat > Dockerfile << 'EOF'
FROM ubuntu:20.04

# Install dependencies
RUN apt-get update && apt-get install -y \
    bash \
    curl \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Install TuskLang CLI
COPY cli/ /usr/local/bin/
RUN chmod +x /usr/local/bin/main.sh
RUN ln -s /usr/local/bin/main.sh /usr/local/bin/tsk

# Set working directory
WORKDIR /app

# Copy application files
COPY . .

# Load configuration
RUN tsk peanuts compile peanu.peanuts

# Expose port
EXPOSE 3000

# Start application
CMD ["tsk", "serve", "3000"]
EOF

# Create docker-compose.yml
cat > docker-compose.yml << 'EOF'
version: '3.8'

services:
  app:
    build: .
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=production
    volumes:
      - ./data:/app/data
    depends_on:
      - db
  
  db:
    image: postgres:13
    environment:
      POSTGRES_DB: myapp
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: secret
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
EOF

# Build and run
echo "Building Docker image..."
docker-compose build

echo "Starting services..."
docker-compose up -d
```

### Kubernetes Integration
```bash
#!/bin/bash
# kubernetes_integration.sh

echo "☸️  Setting up Kubernetes integration..."

# Create ConfigMap for configuration
cat > k8s-configmap.yaml << 'EOF'
apiVersion: v1
kind: ConfigMap
metadata:
  name: tusk-config
data:
  peanu.peanuts: |
    [app]
    name: "My TuskLang App"
    version: "1.0.0"
    
    [server]
    host: "0.0.0.0"
    port: 3000
    
    [database]
    type: "postgresql"
    host: "postgres-service"
    port: 5432
    name: "myapp"
EOF

# Create Deployment
cat > k8s-deployment.yaml << 'EOF'
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusk-app
  template:
    metadata:
      labels:
        app: tusk-app
    spec:
      containers:
      - name: tusk-app
        image: tusk-app:latest
        ports:
        - containerPort: 3000
        env:
        - name: NODE_ENV
          value: "production"
        volumeMounts:
        - name: config-volume
          mountPath: /app/config
      volumes:
      - name: config-volume
        configMap:
          name: tusk-config
EOF

# Create Service
cat > k8s-service.yaml << 'EOF'
apiVersion: v1
kind: Service
metadata:
  name: tusk-service
spec:
  selector:
    app: tusk-app
  ports:
  - port: 80
    targetPort: 3000
  type: LoadBalancer
EOF

# Apply configurations
echo "Applying Kubernetes configurations..."
kubectl apply -f k8s-configmap.yaml
kubectl apply -f k8s-deployment.yaml
kubectl apply -f k8s-service.yaml

echo "✅ Kubernetes integration completed"
```

## CLI Tools

### Git Hooks Integration
```bash
#!/bin/bash
# git_hooks.sh

echo "🔗 Setting up Git hooks integration..."

# Create pre-commit hook
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/bash

echo "🔍 Running pre-commit checks..."

# Validate configuration
if ! tsk config validate --quiet; then
    echo "❌ Configuration validation failed"
    exit 1
fi

# Run tests
if ! tsk test all --quiet; then
    echo "❌ Tests failed"
    exit 1
fi

# Check database migrations
if ! tsk db status --quiet; then
    echo "❌ Database not ready"
    exit 1
fi

echo "✅ Pre-commit checks passed"
EOF

# Create post-commit hook
cat > .git/hooks/post-commit << 'EOF'
#!/bin/bash

echo "📝 Running post-commit tasks..."

# Compile configuration
tsk peanuts compile peanu.peanuts

# Update version if needed
# tsk config set app.version "$(git describe --tags)"

echo "✅ Post-commit tasks completed"
EOF

# Make hooks executable
chmod +x .git/hooks/pre-commit
chmod +x .git/hooks/post-commit

echo "✅ Git hooks integration completed"
```

### CI/CD Pipeline Integration
```bash
#!/bin/bash
# cicd_integration.sh

echo "🔄 Setting up CI/CD integration..."

# Create GitHub Actions workflow
mkdir -p .github/workflows
cat > .github/workflows/ci.yml << 'EOF'
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup TuskLang
      run: |
        curl -sSL https://tusklang.org/install-bash.sh | bash
    
    - name: Validate Configuration
      run: tsk config validate
    
    - name: Run Tests
      run: tsk test all
    
    - name: Check Database
      run: tsk db status
    
    - name: Build
      run: |
        tsk peanuts compile peanu.peanuts
        tsk binary compile src/main.tsk
    
    - name: Performance Test
      run: tsk test performance

  deploy:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup TuskLang
      run: |
        curl -sSL https://tusklang.org/install-bash.sh | bash
    
    - name: Deploy
      run: |
        tsk services stop
        tsk db backup pre-deploy.sql
        tsk db migrate migrations/*.sql
        tsk services start
EOF

# Create Jenkins pipeline
cat > Jenkinsfile << 'EOF'
pipeline {
    agent any
    
    stages {
        stage('Setup') {
            steps {
                sh '''
                    curl -sSL https://tusklang.org/install-bash.sh | bash
                    tsk version
                '''
            }
        }
        
        stage('Validate') {
            steps {
                sh 'tsk config validate'
            }
        }
        
        stage('Test') {
            steps {
                sh 'tsk test all'
            }
        }
        
        stage('Build') {
            steps {
                sh '''
                    tsk peanuts compile peanu.peanuts
                    tsk binary compile src/main.tsk
                '''
            }
        }
        
        stage('Deploy') {
            when {
                branch 'main'
            }
            steps {
                sh '''
                    tsk services stop
                    tsk db backup pre-deploy.sql
                    tsk db migrate migrations/*.sql
                    tsk services start
                '''
            }
        }
    }
    
    post {
        always {
            sh 'tsk db status'
        }
        failure {
            sh 'tsk services stop'
        }
    }
}
EOF

echo "✅ CI/CD integration completed"
```

## Monitoring and Logging

### Prometheus Integration
```bash
#!/bin/bash
# prometheus_integration.sh

echo "📊 Setting up Prometheus integration..."

# Create metrics exporter
cat > metrics_exporter.sh << 'EOF'
#!/bin/bash

# Export TuskLang metrics for Prometheus

while true; do
    # Database metrics
    DB_STATUS=$(tsk db status --json 2>/dev/null)
    if [[ $? -eq 0 ]]; then
        echo "# HELP tusk_db_status Database connection status"
        echo "# TYPE tusk_db_status gauge"
        if echo "$DB_STATUS" | jq -e '.status == "connected"' > /dev/null; then
            echo "tusk_db_status 1"
        else
            echo "tusk_db_status 0"
        fi
    fi
    
    # Configuration metrics
    if tsk config validate --quiet 2>/dev/null; then
        echo "# HELP tusk_config_valid Configuration validity"
        echo "# TYPE tusk_config_valid gauge"
        echo "tusk_config_valid 1"
    else
        echo "tusk_config_valid 0"
    fi
    
    # Service metrics
    SERVICE_STATUS=$(tsk services status --json 2>/dev/null)
    if [[ $? -eq 0 ]]; then
        echo "# HELP tusk_services_running Number of running services"
        echo "# TYPE tusk_services_running gauge"
        RUNNING_SERVICES=$(echo "$SERVICE_STATUS" | jq '.services | map(select(.status == "running")) | length')
        echo "tusk_services_running $RUNNING_SERVICES"
    fi
    
    sleep 15
done
EOF

# Create Prometheus configuration
cat > prometheus.yml << 'EOF'
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'tusk-metrics'
    static_configs:
      - targets: ['localhost:9091']
    metrics_path: /metrics
EOF

# Make exporter executable
chmod +x metrics_exporter.sh

echo "✅ Prometheus integration completed"
```

### ELK Stack Integration
```bash
#!/bin/bash
# elk_integration.sh

echo "📈 Setting up ELK Stack integration..."

# Create log forwarder
cat > log_forwarder.sh << 'EOF'
#!/bin/bash

# Forward TuskLang logs to Elasticsearch

while true; do
    # Read latest logs
    if [[ -f ~/.cache/tsk/tsk.log ]]; then
        tail -f ~/.cache/tsk/tsk.log | while read line; do
            # Format log entry
            LOG_ENTRY=$(cat << JSON
{
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)",
    "level": "info",
    "message": "$line",
    "service": "tusk-cli",
    "host": "$(hostname)"
}
JSON
)
            
            # Send to Elasticsearch
            curl -X POST "http://elasticsearch:9200/tusk-logs/_doc" \
                -H "Content-Type: application/json" \
                -d "$LOG_ENTRY" \
                --silent > /dev/null
        done
    fi
    
    sleep 1
done
EOF

# Create Logstash configuration
cat > logstash.conf << 'EOF'
input {
    file {
        path => "/var/log/tusk/*.log"
        type => "tusk-logs"
    }
}

filter {
    if [type] == "tusk-logs" {
        grok {
            match => { "message" => "%{TIMESTAMP_ISO8601:timestamp} %{LOGLEVEL:level} %{GREEDYDATA:message}" }
        }
        date {
            match => [ "timestamp", "ISO8601" ]
        }
    }
}

output {
    elasticsearch {
        hosts => ["elasticsearch:9200"]
        index => "tusk-logs-%{+YYYY.MM.dd}"
    }
}
EOF

# Make forwarder executable
chmod +x log_forwarder.sh

echo "✅ ELK Stack integration completed"
```

## Best Practices

### 1. Environment-Specific Configuration
```bash
# Use different configs for different environments
if [[ "$NODE_ENV" == "production" ]]; then
    tsk peanuts load peanu.production.peanuts
elif [[ "$NODE_ENV" == "staging" ]]; then
    tsk peanuts load peanu.staging.peanuts
else
    tsk peanuts load peanu.development.peanuts
fi
```

### 2. Health Checks
```bash
# Implement health checks for containers
#!/bin/bash
# health_check.sh

# Check database
if ! tsk db status --quiet; then
    exit 1
fi

# Check configuration
if ! tsk config validate --quiet; then
    exit 1
fi

# Check services
if ! tsk services status --quiet; then
    exit 1
fi

exit 0
```

### 3. Graceful Shutdown
```bash
#!/bin/bash
# graceful_shutdown.sh

# Handle shutdown signals
trap 'shutdown' SIGTERM SIGINT

shutdown() {
    echo "Shutting down gracefully..."
    tsk services stop
    tsk db backup shutdown-backup.sql
    exit 0
}

# Main application loop
while true; do
    sleep 1
done
```

## Next Steps

These integrations provide a foundation for:
- [Basic Usage](./basic-usage.md) - Simple usage patterns
- [Complete Workflows](./workflows.md) - Advanced automation
- [Command Reference](../commands/README.md) - Detailed documentation 
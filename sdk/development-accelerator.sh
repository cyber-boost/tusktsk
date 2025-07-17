#!/bin/bash

# SDK Development Accelerator for Agent a1
# Provides instant SDK project creation with full CI/CD integration

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
REGISTRY_URL="${REGISTRY_URL:-http://localhost:8080}"
CDN_URL="${CDN_URL:-http://localhost:3000}"
PROJECT_ROOT="$(pwd)"

echo -e "${BLUE}🚀 TuskLang SDK Development Accelerator${NC}"
echo -e "${BLUE}==========================================${NC}"

# Function to create Go SDK project
create_go_sdk() {
    local sdk_name="$1"
    local sdk_dir="sdk/go/$sdk_name"
    
    echo -e "${YELLOW}Creating Go SDK: $sdk_name${NC}"
    
    mkdir -p "$sdk_dir"
    cd "$sdk_dir"
    
    # Initialize Go module
    go mod init "github.com/tusklang/$sdk_name"
    
    # Create project structure
    mkdir -p {cmd,internal,pkg,test,examples,docs}
    
    # Create main.go
    cat > cmd/main.go << 'EOF'
package main

import (
    "fmt"
    "log"
    "net/http"
    
    "github.com/tusklang/tsk-sdk-go/internal/client"
    "github.com/tusklang/tsk-sdk-go/internal/config"
)

func main() {
    cfg := config.Load()
    client := client.New(cfg)
    
    http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
        w.WriteHeader(http.StatusOK)
        w.Write([]byte("OK"))
    })
    
    log.Printf("Starting %s server on :8080", cfg.ServiceName)
    log.Fatal(http.ListenAndServe(":8080", nil))
}
EOF

    # Create go.mod with dependencies
    cat > go.mod << 'EOF'
module github.com/tusklang/tsk-sdk-go

go 1.21

require (
    github.com/gorilla/mux v1.8.0
    github.com/spf13/viper v1.16.0
    github.com/stretchr/testify v1.8.4
)
EOF

    # Create Dockerfile
    cat > Dockerfile << 'EOF'
FROM golang:1.21-alpine AS builder

WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download

COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o main cmd/main.go

FROM alpine:latest
RUN apk --no-cache add ca-certificates
WORKDIR /root/
COPY --from=builder /app/main .
EXPOSE 8080
CMD ["./main"]
EOF

    # Create GitHub Actions workflow
    mkdir -p .github/workflows
    cat > .github/workflows/ci.yml << 'EOF'
name: Go SDK CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Set up Go
      uses: actions/setup-go@v4
      with:
        go-version: 1.21
    - name: Test
      run: go test -v ./...
    - name: Build
      run: go build -v ./cmd/main.go
EOF

    # Create health check
    cat > internal/health/health.go << 'EOF'
package health

import (
    "context"
    "time"
)

type HealthChecker struct {
    checks map[string]Check
}

type Check func(ctx context.Context) error

func NewHealthChecker() *HealthChecker {
    return &HealthChecker{
        checks: make(map[string]Check),
    }
}

func (h *HealthChecker) AddCheck(name string, check Check) {
    h.checks[name] = check
}

func (h *HealthChecker) RunChecks(ctx context.Context) map[string]error {
    results := make(map[string]error)
    
    for name, check := range h.checks {
        ctx, cancel := context.WithTimeout(ctx, 5*time.Second)
        results[name] = check(ctx)
        cancel()
    }
    
    return results
}
EOF

    echo -e "${GREEN}✅ Go SDK project created: $sdk_dir${NC}"
}

# Function to create Rust SDK project
create_rust_sdk() {
    local sdk_name="$1"
    local sdk_dir="sdk/rust/$sdk_name"
    
    echo -e "${YELLOW}Creating Rust SDK: $sdk_name${NC}"
    
    mkdir -p "$sdk_dir"
    cd "$sdk_dir"
    
    # Initialize Rust project
    cargo init --name "$sdk_name" --lib
    
    # Create project structure
    mkdir -p {src,examples,tests,benches,docs}
    
    # Update Cargo.toml
    cat > Cargo.toml << 'EOF'
[package]
name = "tsk-sdk-rust"
version = "0.1.0"
edition = "2021"
authors = ["TuskLang Team <team@tusklang.org>"]
description = "TuskLang Rust SDK"
license = "MIT"
repository = "https://github.com/tusklang/tsk-sdk-rust"

[dependencies]
tokio = { version = "1.0", features = ["full"] }
serde = { version = "1.0", features = ["derive"] }
serde_json = "1.0"
reqwest = { version = "0.11", features = ["json"] }
thiserror = "1.0"
tracing = "0.1"
tracing-subscriber = "0.3"

[dev-dependencies]
tokio-test = "0.4"
EOF

    # Create lib.rs
    cat > src/lib.rs << 'EOF'
pub mod client;
pub mod config;
pub mod error;
pub mod health;

use error::SdkError;

pub type Result<T> = std::result::Result<T, SdkError>;

pub struct TuskSdk {
    client: client::Client,
}

impl TuskSdk {
    pub fn new(config: config::Config) -> Result<Self> {
        let client = client::Client::new(config)?;
        Ok(Self { client })
    }
    
    pub async fn health_check(&self) -> Result<()> {
        self.client.health_check().await
    }
}
EOF

    # Create client.rs
    cat > src/client.rs << 'EOF'
use crate::{config::Config, error::SdkError, Result};
use reqwest::Client as HttpClient;

pub struct Client {
    http_client: HttpClient,
    base_url: String,
}

impl Client {
    pub fn new(config: Config) -> Result<Self> {
        let http_client = HttpClient::new();
        Ok(Self {
            http_client,
            base_url: config.base_url,
        })
    }
    
    pub async fn health_check(&self) -> Result<()> {
        let url = format!("{}/health", self.base_url);
        let response = self.http_client.get(&url).send().await?;
        
        if response.status().is_success() {
            Ok(())
        } else {
            Err(SdkError::HealthCheckFailed)
        }
    }
}
EOF

    # Create Dockerfile
    cat > Dockerfile << 'EOF'
FROM rust:1.70 as builder

WORKDIR /usr/src/app
COPY . .
RUN cargo build --release

FROM debian:bullseye-slim
RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*
COPY --from=builder /usr/src/app/target/release/libtsk_sdk_rust.so /usr/local/lib/
COPY --from=builder /usr/src/app/target/release/tsk-sdk-rust /usr/local/bin/

EXPOSE 8080
CMD ["tsk-sdk-rust"]
EOF

    # Create GitHub Actions workflow
    mkdir -p .github/workflows
    cat > .github/workflows/ci.yml << 'EOF'
name: Rust SDK CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Install Rust
      uses: actions-rs/toolchain@v1
      with:
        toolchain: stable
    - name: Test
      run: cargo test
    - name: Build
      run: cargo build --release
EOF

    echo -e "${GREEN}✅ Rust SDK project created: $sdk_dir${NC}"
}

# Function to create Java SDK project
create_java_sdk() {
    local sdk_name="$1"
    local sdk_dir="sdk/java/$sdk_name"
    
    echo -e "${YELLOW}Creating Java SDK: $sdk_name${NC}"
    
    mkdir -p "$sdk_dir"
    cd "$sdk_dir"
    
    # Create Gradle project structure
    mkdir -p {src/main/java/com/tusklang/sdk,src/main/resources,src/test/java,src/test/resources}
    
    # Create build.gradle
    cat > build.gradle << 'EOF'
plugins {
    id 'java'
    id 'application'
    id 'com.github.johnrengelman.shadow' version '7.1.2'
}

group = 'com.tusklang'
version = '0.1.0'
sourceCompatibility = '17'

repositories {
    mavenCentral()
}

dependencies {
    implementation 'com.squareup.okhttp3:okhttp:4.11.0'
    implementation 'com.fasterxml.jackson.core:jackson-databind:2.15.2'
    implementation 'org.slf4j:slf4j-api:2.0.7'
    
    testImplementation 'org.junit.jupiter:junit-jupiter:5.9.3'
    testImplementation 'org.mockito:mockito-core:5.3.1'
}

application {
    mainClass = 'com.tusklang.sdk.TuskSdkApplication'
}

test {
    useJUnitPlatform()
}

shadowJar {
    archiveBaseName.set('tusk-sdk-java')
    archiveClassifier.set('')
    archiveVersion.set(project.version.toString())
}
EOF

    # Create main application class
    cat > src/main/java/com/tusklang/sdk/TuskSdkApplication.java << 'EOF'
package com.tusklang.sdk;

import com.tusklang.sdk.client.TuskClient;
import com.tusklang.sdk.config.SdkConfig;

public class TuskSdkApplication {
    public static void main(String[] args) {
        SdkConfig config = SdkConfig.builder()
            .baseUrl("http://localhost:8080")
            .timeout(30000)
            .build();
            
        TuskClient client = new TuskClient(config);
        
        try {
            boolean healthy = client.healthCheck();
            System.out.println("Health check: " + (healthy ? "PASSED" : "FAILED"));
        } catch (Exception e) {
            System.err.println("Health check failed: " + e.getMessage());
        }
    }
}
EOF

    # Create client class
    cat > src/main/java/com/tusklang/sdk/client/TuskClient.java << 'EOF'
package com.tusklang.sdk.client;

import com.tusklang.sdk.config.SdkConfig;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.Response;

import java.io.IOException;
import java.time.Duration;

public class TuskClient {
    private final OkHttpClient httpClient;
    private final SdkConfig config;
    
    public TuskClient(SdkConfig config) {
        this.config = config;
        this.httpClient = new OkHttpClient.Builder()
            .connectTimeout(Duration.ofMillis(config.getTimeout()))
            .readTimeout(Duration.ofMillis(config.getTimeout()))
            .writeTimeout(Duration.ofMillis(config.getTimeout()))
            .build();
    }
    
    public boolean healthCheck() throws IOException {
        Request request = new Request.Builder()
            .url(config.getBaseUrl() + "/health")
            .get()
            .build();
            
        try (Response response = httpClient.newCall(request).execute()) {
            return response.isSuccessful();
        }
    }
}
EOF

    # Create config class
    cat > src/main/java/com/tusklang/sdk/config/SdkConfig.java << 'EOF'
package com.tusklang.sdk.config;

public class SdkConfig {
    private final String baseUrl;
    private final int timeout;
    
    private SdkConfig(Builder builder) {
        this.baseUrl = builder.baseUrl;
        this.timeout = builder.timeout;
    }
    
    public String getBaseUrl() { return baseUrl; }
    public int getTimeout() { return timeout; }
    
    public static Builder builder() {
        return new Builder();
    }
    
    public static class Builder {
        private String baseUrl = "http://localhost:8080";
        private int timeout = 30000;
        
        public Builder baseUrl(String baseUrl) {
            this.baseUrl = baseUrl;
            return this;
        }
        
        public Builder timeout(int timeout) {
            this.timeout = timeout;
            return this;
        }
        
        public SdkConfig build() {
            return new SdkConfig(this);
        }
    }
}
EOF

    # Create Dockerfile
    cat > Dockerfile << 'EOF'
FROM gradle:7.6-jdk17 AS builder

WORKDIR /app
COPY . .
RUN gradle build --no-daemon

FROM openjdk:17-jre-slim
WORKDIR /app
COPY --from=builder /app/build/libs/tusk-sdk-java-*.jar app.jar

EXPOSE 8080
CMD ["java", "-jar", "app.jar"]
EOF

    # Create GitHub Actions workflow
    mkdir -p .github/workflows
    cat > .github/workflows/ci.yml << 'EOF'
name: Java SDK CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Set up JDK 17
      uses: actions/setup-java@v4
      with:
        java-version: '17'
        distribution: 'temurin'
    - name: Test
      run: ./gradlew test
    - name: Build
      run: ./gradlew build
EOF

    echo -e "${GREEN}✅ Java SDK project created: $sdk_dir${NC}"
}

# Function to create development environment
create_dev_environment() {
    local sdk_name="$1"
    local language="$2"
    
    echo -e "${YELLOW}Setting up development environment for $sdk_name ($language)${NC}"
    
    # Create development environment configuration
    cat > "sdk/$language/$sdk_name/dev-env.sh" << 'EOF'
#!/bin/bash

# Development Environment Setup
export SDK_NAME="tsk-sdk"
export REGISTRY_URL="http://localhost:8080"
export CDN_URL="http://localhost:3000"
export PROMETHEUS_URL="http://localhost:9090"
export GRAFANA_URL="http://localhost:3001"

# Start development services
echo "Starting development environment..."

# Start Redis for caching
docker run -d --name redis-dev -p 6379:6379 redis:7-alpine

# Start PostgreSQL for testing
docker run -d --name postgres-dev -p 5432:5432 \
  -e POSTGRES_DB=tusk_dev \
  -e POSTGRES_USER=tusk \
  -e POSTGRES_PASSWORD=tusk123 \
  postgres:15

# Start monitoring stack
docker-compose -f monitoring/docker-compose.yml up -d

echo "Development environment ready!"
echo "Registry: $REGISTRY_URL"
echo "CDN: $CDN_URL"
echo "Monitoring: $GRAFANA_URL"
EOF

    chmod +x "sdk/$language/$sdk_name/dev-env.sh"
    
    echo -e "${GREEN}✅ Development environment configured${NC}"
}

# Function to create monitoring integration
create_monitoring_integration() {
    local sdk_name="$1"
    local language="$2"
    
    echo -e "${YELLOW}Setting up monitoring integration for $sdk_name${NC}"
    
    # Create monitoring configuration
    cat > "sdk/$language/$sdk_name/monitoring.yml" << 'EOF'
# SDK Monitoring Configuration
metrics:
  enabled: true
  port: 9090
  path: /metrics

health_checks:
  - name: "sdk_connectivity"
    interval: 30s
    timeout: 5s
    url: "http://localhost:8080/health"
    
  - name: "registry_connectivity"
    interval: 60s
    timeout: 10s
    url: "http://localhost:8080/api/v1/health"

alerts:
  - name: "sdk_down"
    condition: "sdk_health_status == 0"
    severity: "critical"
    message: "SDK is not responding to health checks"
    
  - name: "registry_down"
    condition: "registry_health_status == 0"
    severity: "warning"
    message: "Package registry is not accessible"
EOF

    echo -e "${GREEN}✅ Monitoring integration configured${NC}"
}

# Main function
main() {
    local language="$1"
    local sdk_name="${2:-tsk-sdk}"
    
    case $language in
        "go")
            create_go_sdk "$sdk_name"
            create_dev_environment "$sdk_name" "go"
            create_monitoring_integration "$sdk_name" "go"
            ;;
        "rust")
            create_rust_sdk "$sdk_name"
            create_dev_environment "$sdk_name" "rust"
            create_monitoring_integration "$sdk_name" "rust"
            ;;
        "java")
            create_java_sdk "$sdk_name"
            create_dev_environment "$sdk_name" "java"
            create_monitoring_integration "$sdk_name" "java"
            ;;
        "all")
            create_go_sdk "$sdk_name-go"
            create_rust_sdk "$sdk_name-rust"
            create_java_sdk "$sdk_name-java"
            create_dev_environment "$sdk_name-go" "go"
            create_dev_environment "$sdk_name-rust" "rust"
            create_dev_environment "$sdk_name-java" "java"
            create_monitoring_integration "$sdk_name-go" "go"
            create_monitoring_integration "$sdk_name-rust" "rust"
            create_monitoring_integration "$sdk_name-java" "java"
            ;;
        *)
            echo -e "${RED}❌ Invalid language: $language${NC}"
            echo "Usage: $0 {go|rust|java|all} [sdk-name]"
            exit 1
            ;;
    esac
    
    echo -e "${GREEN}🎉 SDK development environment ready!${NC}"
    echo -e "${BLUE}Next steps:${NC}"
    echo "1. cd sdk/$language/$sdk_name"
    echo "2. ./dev-env.sh"
    echo "3. Start developing!"
}

# Check arguments
if [ $# -lt 1 ]; then
    echo -e "${RED}❌ Usage: $0 {go|rust|java|all} [sdk-name]${NC}"
    exit 1
fi

# Run main function
main "$@" 
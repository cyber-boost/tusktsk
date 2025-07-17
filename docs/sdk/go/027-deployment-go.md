# Deployment Strategies in TuskLang - Go Guide

## 🚀 **Deploy Like a Rebel: TuskLang in Production**

TuskLang isn’t just for local hacks—it’s built for the real world. We don’t bow to any king, especially not to clunky deployment pipelines. Here’s how to deploy TuskLang-powered Go apps with confidence, speed, and style.

## 📋 **Table of Contents**
- [Local Deployment](#local-deployment)
- [Cloud Deployment](#cloud-deployment)
- [Docker Integration](#docker-integration)
- [CI/CD Pipelines](#cicd-pipelines)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 🖥️ **Local Deployment**

### **Basic Local Run**

```go
// Go - Local run
func main() {
    config, err := tusklang.LoadConfig("peanu.tsk")
    if err != nil {
        log.Fatalf("Failed to load config: %v", err)
    }
    // ... start your app
}
```

### **Hot Reloading**

```go
// Go - Hot reload config
watcher := tusklang.NewWatcher("peanu.tsk")
watcher.OnChange(func() {
    log.Println("Config changed! Reloading...")
    // Reload logic here
})
watcher.Start()
```

## ☁️ **Cloud Deployment**

### **Environment-Specific Configs**

```go
// TuskLang - Environment configs
[env:production]
db_url: "postgres://prod-db"
[env:staging]
db_url: "postgres://staging-db"
```

```go
// Go - Load env-specific config
env := os.Getenv("APP_ENV")
config, _ := tusklang.LoadConfig("peanu.tsk")
dbURL := config.GetString("db_url", tusklang.WithEnv(env))
```

### **Cloud Providers**
- AWS: Store configs in S3, load at runtime
- GCP: Use Secret Manager for sensitive values
- Azure: Use Key Vault for secrets

## 🐳 **Docker Integration**

### **Dockerfile Example**

```dockerfile
FROM golang:1.21-alpine
WORKDIR /app
COPY . .
RUN go build -o myapp .
CMD ["./myapp"]
```

### **Mounting Configs**

```bash
docker run -v $(pwd)/peanu.tsk:/app/peanu.tsk myapp
```

### **Environment Variables**

```bash
docker run -e API_KEY=supersecret myapp
```

## 🔄 **CI/CD Pipelines**

### **GitHub Actions**

```yaml
name: Go CI
on: [push]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Set up Go
        uses: actions/setup-go@v4
        with:
          go-version: '1.21'
      - name: Build
        run: go build -v ./...
      - name: Test
        run: go test -v ./...
      - name: Lint
        run: golangci-lint run
```

### **Deploy to Cloud**
- Use `scp` or cloud CLI to upload configs
- Use secrets management for sensitive data

## 🔗 **Go Integration**

### **Config Injection**

```go
type AppConfig struct {
    DBURL string `tsk:"db_url"`
}

func main() {
    var cfg AppConfig
    tusklang.UnmarshalFile("peanu.tsk", &cfg)
    // Use cfg.DBURL
}
```

### **Health Checks**

```go
// Go - Health check endpoint
http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
    w.WriteHeader(http.StatusOK)
    w.Write([]byte("OK"))
})
```

## 🥇 **Best Practices**

- Use environment-specific configs for each stage
- Never bake secrets into images—use env vars or secret stores
- Automate tests and linting in CI
- Monitor config reloads in production

---

**Deploy TuskLang with Go: Fast, fearless, and always production-ready.** 
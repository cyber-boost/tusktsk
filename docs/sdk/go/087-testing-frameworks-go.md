# Testing Frameworks in TuskLang for Go

## Overview

Testing frameworks in TuskLang provide powerful testing configuration and automation capabilities directly in your configuration files. These features enable you to define sophisticated testing strategies, test environments, and automation workflows with Go integration for robust and reliable applications.

## Basic Testing Configuration

```go
// TuskLang testing configuration
testing: {
    frameworks: {
        unit: {
            enabled: true
            framework: "testify"
            coverage: {
                enabled: true
                threshold: 80
                exclude: ["vendor/*", "mocks/*"]
                output: "coverage.out"
                html: "coverage.html"
            }
            timeout: "30s"
            parallel: true
        }
        
        integration: {
            enabled: true
            framework: "testify"
            database: {
                type: "testcontainers"
                image: "postgres:13"
                setup_script: "./test/setup.sql"
                teardown_script: "./test/teardown.sql"
            }
            timeout: "5m"
            parallel: false
        }
        
        e2e: {
            enabled: true
            framework: "playwright"
            browser: {
                type: "chromium"
                headless: true
                slow_mo: 100
            }
            base_url: "@env('TEST_BASE_URL', 'http://localhost:8080')"
            timeout: "10m"
            parallel: 2
        }
        
        performance: {
            enabled: true
            framework: "k6"
            scenarios: {
                load_test: {
                    duration: "5m"
                    vus: 10
                    target: 100
                }
                
                stress_test: {
                    duration: "10m"
                    vus: 50
                    target: 500
                }
            }
        }
    }
    
    environments: {
        local: {
            database: "sqlite://:memory:"
            cache: "memory"
            external_apis: {
                mock: true
                responses: "./test/mocks/responses.json"
            }
        }
        
        staging: {
            database: "@env('STAGING_DB_URL')"
            cache: "redis://localhost:6379"
            external_apis: {
                mock: false
                base_url: "@env('STAGING_API_URL')"
            }
        }
        
        ci: {
            database: "postgresql://test:test@localhost:5432/test"
            cache: "redis://localhost:6379"
            external_apis: {
                mock: true
                responses: "./test/mocks/ci_responses.json"
            }
        }
    }
    
    reporting: {
        junit: {
            enabled: true
            output: "test-results.xml"
        }
        
        html: {
            enabled: true
            output: "test-report.html"
            template: "./test/templates/report.html"
        }
        
        coverage: {
            enabled: true
            output: "coverage-report.html"
            badge: true
        }
    }
    
    automation: {
        ci: {
            enabled: true
            triggers: ["push", "pull_request"]
            stages: ["unit", "integration", "e2e"]
            artifacts: ["coverage", "reports"]
        }
        
        scheduled: {
            enabled: true
            schedule: "0 2 * * *"
            tests: ["performance", "e2e"]
        }
    }
}
```

## Go Integration

```go
package main

import (
    "context"
    "fmt"
    "log"
    "os"
    "path/filepath"
    "time"
    
    "github.com/stretchr/testify/suite"
    "github.com/testcontainers/testcontainers-go"
    "github.com/testcontainers/testcontainers-go/wait"
    "github.com/tusklang/go-sdk"
)

type TestingConfig struct {
    Frameworks  map[string]FrameworkConfig `tsk:"frameworks"`
    Environments map[string]Environment     `tsk:"environments"`
    Reporting    ReportingConfig           `tsk:"reporting"`
    Automation   AutomationConfig          `tsk:"automation"`
}

type FrameworkConfig struct {
    Enabled bool                   `tsk:"enabled"`
    Framework string               `tsk:"framework"`
    Coverage  CoverageConfig       `tsk:"coverage"`
    Timeout   string               `tsk:"timeout"`
    Parallel  bool                 `tsk:"parallel"`
    Config    map[string]interface{} `tsk:",inline"`
}

type CoverageConfig struct {
    Enabled   bool     `tsk:"enabled"`
    Threshold int      `tsk:"threshold"`
    Exclude   []string `tsk:"exclude"`
    Output    string   `tsk:"output"`
    HTML      string   `tsk:"html"`
}

type Environment struct {
    Database     string                 `tsk:"database"`
    Cache        string                 `tsk:"cache"`
    ExternalAPIs map[string]interface{} `tsk:"external_apis"`
}

type ReportingConfig struct {
    JUnit    JUnitConfig    `tsk:"junit"`
    HTML     HTMLConfig     `tsk:"html"`
    Coverage CoverageReport `tsk:"coverage"`
}

type JUnitConfig struct {
    Enabled bool   `tsk:"enabled"`
    Output  string `tsk:"output"`
}

type HTMLConfig struct {
    Enabled  bool   `tsk:"enabled"`
    Output   string `tsk:"output"`
    Template string `tsk:"template"`
}

type CoverageReport struct {
    Enabled bool   `tsk:"enabled"`
    Output  string `tsk:"output"`
    Badge   bool   `tsk:"badge"`
}

type AutomationConfig struct {
    CI       CIConfig       `tsk:"ci"`
    Scheduled ScheduledConfig `tsk:"scheduled"`
}

type CIConfig struct {
    Enabled  bool     `tsk:"enabled"`
    Triggers []string `tsk:"triggers"`
    Stages   []string `tsk:"stages"`
    Artifacts []string `tsk:"artifacts"`
}

type ScheduledConfig struct {
    Enabled  bool   `tsk:"enabled"`
    Schedule string `tsk:"schedule"`
    Tests    []string `tsk:"tests"`
}

type TestManager struct {
    config      TestingConfig
    environment string
    containers  map[string]testcontainers.Container
    reports     *TestReporter
}

type TestReporter struct {
    config ReportingConfig
    results []TestResult
}

type TestResult struct {
    Name     string    `json:"name"`
    Status   string    `json:"status"`
    Duration time.Duration `json:"duration"`
    Error    string    `json:"error,omitempty"`
    Coverage float64   `json:"coverage,omitempty"`
}

func main() {
    // Load testing configuration
    config, err := tusk.LoadFile("testing-config.tsk")
    if err != nil {
        log.Fatalf("Error loading testing config: %v", err)
    }
    
    var testingConfig TestingConfig
    if err := config.Get("testing", &testingConfig); err != nil {
        log.Fatalf("Error parsing testing config: %v", err)
    }
    
    // Initialize test manager
    testManager := NewTestManager(testingConfig)
    defer testManager.Cleanup()
    
    // Run tests based on environment
    environment := os.Getenv("TEST_ENV")
    if environment == "" {
        environment = "local"
    }
    
    if err := testManager.RunTests(environment); err != nil {
        log.Fatalf("Error running tests: %v", err)
    }
}

func NewTestManager(config TestingConfig) *TestManager {
    return &TestManager{
        config:     config,
        containers: make(map[string]testcontainers.Container),
        reports:    NewTestReporter(config.Reporting),
    }
}

func (tm *TestManager) RunTests(environment string) error {
    tm.environment = environment
    
    // Setup test environment
    if err := tm.setupEnvironment(environment); err != nil {
        return err
    }
    
    // Run unit tests
    if unit, exists := tm.config.Frameworks["unit"]; exists && unit.Enabled {
        if err := tm.runUnitTests(unit); err != nil {
            return err
        }
    }
    
    // Run integration tests
    if integration, exists := tm.config.Frameworks["integration"]; exists && integration.Enabled {
        if err := tm.runIntegrationTests(integration); err != nil {
            return err
        }
    }
    
    // Run E2E tests
    if e2e, exists := tm.config.Frameworks["e2e"]; exists && e2e.Enabled {
        if err := tm.runE2ETests(e2e); err != nil {
            return err
        }
    }
    
    // Run performance tests
    if performance, exists := tm.config.Frameworks["performance"]; exists && performance.Enabled {
        if err := tm.runPerformanceTests(performance); err != nil {
            return err
        }
    }
    
    // Generate reports
    return tm.generateReports()
}

func (tm *TestManager) setupEnvironment(environment string) error {
    env, exists := tm.config.Environments[environment]
    if !exists {
        return fmt.Errorf("environment %s not found", environment)
    }
    
    // Setup database
    if err := tm.setupDatabase(env.Database); err != nil {
        return err
    }
    
    // Setup cache
    if err := tm.setupCache(env.Cache); err != nil {
        return err
    }
    
    // Setup external APIs
    if err := tm.setupExternalAPIs(env.ExternalAPIs); err != nil {
        return err
    }
    
    return nil
}

func (tm *TestManager) setupDatabase(databaseURL string) error {
    if databaseURL == "sqlite://:memory:" {
        // In-memory SQLite for local testing
        return nil
    }
    
    // Use testcontainers for other databases
    ctx := context.Background()
    
    req := testcontainers.ContainerRequest{
        Image:        "postgres:13",
        ExposedPorts: []string{"5432/tcp"},
        Env: map[string]string{
            "POSTGRES_DB":       "test",
            "POSTGRES_USER":     "test",
            "POSTGRES_PASSWORD": "test",
        },
        WaitingFor: wait.ForLog("database system is ready to accept connections"),
    }
    
    container, err := testcontainers.GenericContainer(ctx, testcontainers.GenericContainerRequest{
        ContainerRequest: req,
        Started:          true,
    })
    if err != nil {
        return err
    }
    
    tm.containers["database"] = container
    return nil
}

func (tm *TestManager) setupCache(cacheURL string) error {
    if cacheURL == "memory" {
        // In-memory cache for local testing
        return nil
    }
    
    // Use testcontainers for Redis
    ctx := context.Background()
    
    req := testcontainers.ContainerRequest{
        Image:        "redis:6-alpine",
        ExposedPorts: []string{"6379/tcp"},
        WaitingFor:   wait.ForLog("Ready to accept connections"),
    }
    
    container, err := testcontainers.GenericContainer(ctx, testcontainers.GenericContainerRequest{
        ContainerRequest: req,
        Started:          true,
    })
    if err != nil {
        return err
    }
    
    tm.containers["cache"] = container
    return nil
}

func (tm *TestManager) setupExternalAPIs(config map[string]interface{}) error {
    if mock, exists := config["mock"]; exists && mock.(bool) {
        // Setup mock responses
        if responses, exists := config["responses"]; exists {
            return tm.loadMockResponses(responses.(string))
        }
    }
    
    return nil
}

func (tm *TestManager) loadMockResponses(responsesFile string) error {
    // Load mock responses from file
    // This would implement mock response loading
    return nil
}

func (tm *TestManager) runUnitTests(config FrameworkConfig) error {
    log.Println("Running unit tests...")
    
    // Run Go tests with coverage
    cmd := exec.Command("go", "test", "./...", "-v", "-cover")
    
    if config.Coverage.Enabled {
        cmd.Args = append(cmd.Args, "-coverprofile="+config.Coverage.Output)
    }
    
    if config.Parallel {
        cmd.Args = append(cmd.Args, "-parallel=4")
    }
    
    output, err := cmd.CombinedOutput()
    if err != nil {
        log.Printf("Unit tests failed: %s", output)
        return err
    }
    
    log.Printf("Unit tests completed: %s", output)
    
    // Check coverage threshold
    if config.Coverage.Enabled {
        return tm.checkCoverage(config.Coverage)
    }
    
    return nil
}

func (tm *TestManager) runIntegrationTests(config FrameworkConfig) error {
    log.Println("Running integration tests...")
    
    // Run integration tests
    cmd := exec.Command("go", "test", "./integration/...", "-v", "-tags=integration")
    
    if config.Timeout != "" {
        timeout, _ := time.ParseDuration(config.Timeout)
        ctx, cancel := context.WithTimeout(context.Background(), timeout)
        defer cancel()
        cmd = exec.CommandContext(ctx, cmd.Path, cmd.Args[1:]...)
    }
    
    output, err := cmd.CombinedOutput()
    if err != nil {
        log.Printf("Integration tests failed: %s", output)
        return err
    }
    
    log.Printf("Integration tests completed: %s", output)
    return nil
}

func (tm *TestManager) runE2ETests(config FrameworkConfig) error {
    log.Println("Running E2E tests...")
    
    // Run E2E tests with Playwright
    cmd := exec.Command("npx", "playwright", "test")
    
    if config.Timeout != "" {
        timeout, _ := time.ParseDuration(config.Timeout)
        ctx, cancel := context.WithTimeout(context.Background(), timeout)
        defer cancel()
        cmd = exec.CommandContext(ctx, cmd.Path, cmd.Args[1:]...)
    }
    
    output, err := cmd.CombinedOutput()
    if err != nil {
        log.Printf("E2E tests failed: %s", output)
        return err
    }
    
    log.Printf("E2E tests completed: %s", output)
    return nil
}

func (tm *TestManager) runPerformanceTests(config FrameworkConfig) error {
    log.Println("Running performance tests...")
    
    // Run k6 performance tests
    for scenarioName, scenario := range config.Config["scenarios"].(map[string]interface{}) {
        log.Printf("Running scenario: %s", scenarioName)
        
        // Create k6 script
        script := tm.createK6Script(scenarioName, scenario.(map[string]interface{}))
        scriptFile := fmt.Sprintf("k6_%s.js", scenarioName)
        
        if err := os.WriteFile(scriptFile, []byte(script), 0644); err != nil {
            return err
        }
        
        cmd := exec.Command("k6", "run", scriptFile)
        output, err := cmd.CombinedOutput()
        if err != nil {
            log.Printf("Performance test failed: %s", output)
            return err
        }
        
        log.Printf("Performance test completed: %s", output)
    }
    
    return nil
}

func (tm *TestManager) createK6Script(scenarioName string, scenario map[string]interface{}) string {
    duration := scenario["duration"].(string)
    vus := scenario["vus"].(int)
    target := scenario["target"].(int)
    
    return fmt.Sprintf(`
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '%s', target: %d },
        { duration: '%s', target: %d },
        { duration: '%s', target: 0 },
    ],
    vus: %d,
};

export default function() {
    const response = http.get('http://localhost:8080/api/health');
    check(response, {
        'status is 200': (r) => r.status === 200,
    });
    sleep(1);
}
`, duration, target, duration, target, duration, vus)
}

func (tm *TestManager) checkCoverage(config CoverageConfig) error {
    // Parse coverage output
    data, err := os.ReadFile(config.Output)
    if err != nil {
        return err
    }
    
    // Calculate coverage percentage
    coverage := tm.calculateCoverage(string(data))
    
    if coverage < float64(config.Threshold) {
        return fmt.Errorf("coverage %.2f%% is below threshold %d%%", coverage, config.Threshold)
    }
    
    log.Printf("Coverage: %.2f%% (threshold: %d%%)", coverage, config.Threshold)
    return nil
}

func (tm *TestManager) calculateCoverage(data string) float64 {
    // Parse coverage data and calculate percentage
    // This is a simplified implementation
    return 85.5 // Example coverage percentage
}

func (tm *TestManager) generateReports() error {
    return tm.reports.Generate()
}

func (tm *TestManager) Cleanup() {
    // Stop testcontainers
    for _, container := range tm.containers {
        container.Terminate(context.Background())
    }
}

// TestReporter implementation
func NewTestReporter(config ReportingConfig) *TestReporter {
    return &TestReporter{
        config:  config,
        results: make([]TestResult, 0),
    }
}

func (tr *TestReporter) AddResult(result TestResult) {
    tr.results = append(tr.results, result)
}

func (tr *TestReporter) Generate() error {
    // Generate JUnit report
    if tr.config.JUnit.Enabled {
        if err := tr.generateJUnitReport(); err != nil {
            return err
        }
    }
    
    // Generate HTML report
    if tr.config.HTML.Enabled {
        if err := tr.generateHTMLReport(); err != nil {
            return err
        }
    }
    
    // Generate coverage report
    if tr.config.Coverage.Enabled {
        if err := tr.generateCoverageReport(); err != nil {
            return err
        }
    }
    
    return nil
}

func (tr *TestReporter) generateJUnitReport() error {
    // Generate JUnit XML report
    // This would implement JUnit XML generation
    return nil
}

func (tr *TestReporter) generateHTMLReport() error {
    // Generate HTML report
    // This would implement HTML report generation
    return nil
}

func (tr *TestReporter) generateCoverageReport() error {
    // Generate coverage report
    // This would implement coverage report generation
    return nil
}

// Example test suites
type UserServiceTestSuite struct {
    suite.Suite
    db *sql.DB
}

func (suite *UserServiceTestSuite) SetupSuite() {
    // Setup test database
    db, err := sql.Open("postgres", "postgres://test:test@localhost:5432/test?sslmode=disable")
    suite.Require().NoError(err)
    suite.db = db
}

func (suite *UserServiceTestSuite) TearDownSuite() {
    if suite.db != nil {
        suite.db.Close()
    }
}

func (suite *UserServiceTestSuite) TestCreateUser() {
    // Test user creation
    user := User{
        Name:  "Test User",
        Email: "test@example.com",
        Role:  "user",
    }
    
    // Test implementation
    suite.NotNil(user)
}

func (suite *UserServiceTestSuite) TestGetUserByID() {
    // Test user retrieval
    userID := 1
    
    // Test implementation
    suite.NotZero(userID)
}

func TestUserServiceSuite(t *testing.T) {
    suite.Run(t, new(UserServiceTestSuite))
}
```

## Advanced Testing Features

### Test Data Management

```go
// TuskLang configuration with test data management
testing: {
    test_data: {
        enabled: true
        fixtures: {
            users: "./test/fixtures/users.json"
            products: "./test/fixtures/products.json"
            orders: "./test/fixtures/orders.json"
        }
        
        factories: {
            user: {
                template: {
                    name: "{{.FirstName}} {{.LastName}}"
                    email: "{{.Email}}"
                    role: "user"
                }
                traits: {
                    admin: {
                        role: "admin"
                        permissions: ["read", "write", "delete"]
                    }
                    premium: {
                        role: "premium"
                        subscription: "premium"
                    }
                }
            }
        }
        
        seeding: {
            strategy: "truncate"
            order: ["users", "products", "orders"]
        }
    }
}
```

### Test Parallelization

```go
// TuskLang configuration with test parallelization
testing: {
    parallelization: {
        enabled: true
        strategies: {
            by_package: {
                enabled: true
                max_workers: 4
            }
            
            by_test: {
                enabled: true
                max_workers: 8
            }
        }
        
        isolation: {
            database: "per_test"
            cache: "per_test"
            ports: "dynamic"
        }
    }
}
```

## Performance Considerations

- **Test Isolation**: Ensure tests are properly isolated
- **Resource Management**: Manage test resources efficiently
- **Parallel Execution**: Use parallel execution for faster test runs
- **Caching**: Cache test dependencies and artifacts
- **Cleanup**: Properly cleanup test resources

## Security Notes

- **Test Data Security**: Ensure test data doesn't contain sensitive information
- **Environment Isolation**: Isolate test environments from production
- **Access Control**: Control access to test environments and data
- **Audit Logging**: Log test execution for security auditing
- **Secret Management**: Use secure methods for test credentials

## Best Practices

1. **Test Organization**: Organize tests logically and consistently
2. **Test Naming**: Use descriptive test names
3. **Test Independence**: Ensure tests are independent and repeatable
4. **Test Coverage**: Maintain adequate test coverage
5. **Test Performance**: Keep tests fast and efficient
6. **Documentation**: Document test strategies and procedures

## Integration Examples

### With Testify

```go
import (
    "testing"
    "github.com/stretchr/testify/suite"
    "github.com/tusklang/go-sdk"
)

func setupTestSuite(config tusk.Config) *TestSuite {
    var testingConfig TestingConfig
    config.Get("testing", &testingConfig)
    
    return &TestSuite{
        config: testingConfig,
    }
}

type TestSuite struct {
    suite.Suite
    config TestingConfig
}
```

### With Ginkgo

```go
import (
    "github.com/onsi/ginkgo/v2"
    "github.com/onsi/gomega"
    "github.com/tusklang/go-sdk"
)

func setupGinkgo(config tusk.Config) {
    var testingConfig TestingConfig
    config.Get("testing", &testingConfig)
    
    ginkgo.Describe("Application Tests", func() {
        ginkgo.BeforeEach(func() {
            // Setup test environment
        })
        
        ginkgo.AfterEach(func() {
            // Cleanup test environment
        })
        
        ginkgo.It("should work correctly", func() {
            gomega.Expect(true).To(gomega.BeTrue())
        })
    })
}
```

This comprehensive testing frameworks documentation provides Go developers with everything they need to build sophisticated testing systems using TuskLang's powerful configuration capabilities. 
# Testing in TuskLang - Go Guide

## 🎯 **The Power of Comprehensive Testing**

In TuskLang, testing isn't just about checking code—it's about ensuring your configuration is bulletproof. We don't bow to any king, especially not untested or unreliable configurations. TuskLang gives you the power to test your configuration thoroughly with comprehensive testing frameworks and validation.

## 📋 **Table of Contents**
- [Understanding Testing in TuskLang](#understanding-testing-in-tusklang)
- [Unit Testing](#unit-testing)
- [Integration Testing](#integration-testing)
- [Configuration Testing](#configuration-testing)
- [Go Integration](#go-integration)
- [Testing Patterns](#testing-patterns)
- [Performance Testing](#performance-testing)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)

## 🔍 **Understanding Testing in TuskLang**

TuskLang provides comprehensive testing capabilities for configuration validation:

```go
// TuskLang - Testing configuration
[testing_config]
unit_tests: @test.unit({
    "config_validation": true,
    "syntax_checking": true,
    "type_validation": true
})

integration_tests: @test.integration({
    "database_connection": true,
    "api_endpoints": true,
    "file_operations": true
})

performance_tests: @test.performance({
    "load_testing": true,
    "stress_testing": true,
    "benchmarking": true
})
```

```go
// Go integration
type TestingConfig struct {
    UnitTests       map[string]bool `tsk:"unit_tests"`
    IntegrationTests map[string]bool `tsk:"integration_tests"`
    PerformanceTests map[string]bool `tsk:"performance_tests"`
}
```

## 🧪 **Unit Testing**

### **Configuration Unit Tests**

```go
// TuskLang - Configuration unit tests
[unit_tests]
config_validation: @test.validate({
    "required_fields": ["api_key", "database_url"],
    "type_checking": true,
    "format_validation": true
})

syntax_tests: @test.syntax({
    "file_parsing": true,
    "cross_references": true,
    "operator_validation": true
})
```

```go
// Go - Configuration unit testing
type UnitTests struct {
    ConfigValidation map[string]interface{} `tsk:"config_validation"`
    SyntaxTests      map[string]bool        `tsk:"syntax_tests"`
}

type ConfigValidator struct {
    requiredFields []string
    typeChecking   bool
    formatValidation bool
}

func NewConfigValidator(required []string, typeCheck, formatCheck bool) *ConfigValidator {
    return &ConfigValidator{
        requiredFields:   required,
        typeChecking:     typeCheck,
        formatValidation: formatCheck,
    }
}

func (c *ConfigValidator) ValidateConfig(config map[string]interface{}) error {
    // Check required fields
    for _, field := range c.requiredFields {
        if _, exists := config[field]; !exists {
            return fmt.Errorf("required field '%s' is missing", field)
        }
    }
    
    // Type checking
    if c.typeChecking {
        if err := c.validateTypes(config); err != nil {
            return fmt.Errorf("type validation failed: %w", err)
        }
    }
    
    // Format validation
    if c.formatValidation {
        if err := c.validateFormats(config); err != nil {
            return fmt.Errorf("format validation failed: %w", err)
        }
    }
    
    return nil
}

func (c *ConfigValidator) validateTypes(config map[string]interface{}) error {
    // Validate specific field types
    if apiKey, exists := config["api_key"]; exists {
        if _, ok := apiKey.(string); !ok {
            return errors.New("api_key must be a string")
        }
    }
    
    if port, exists := config["port"]; exists {
        if _, ok := port.(int); !ok {
            return errors.New("port must be an integer")
        }
    }
    
    return nil
}

func (c *ConfigValidator) validateFormats(config map[string]interface{}) error {
    // Validate specific field formats
    if apiKey, exists := config["api_key"]; exists {
        if key, ok := apiKey.(string); ok {
            if len(key) < 32 {
                return errors.New("api_key must be at least 32 characters")
            }
        }
    }
    
    if port, exists := config["port"]; exists {
        if p, ok := port.(int); ok {
            if p < 1 || p > 65535 {
                return errors.New("port must be between 1 and 65535")
            }
        }
    }
    
    return nil
}
```

### **Syntax Testing**

```go
// Go - Syntax testing
type SyntaxTester struct {
    fileParsing     bool
    crossReferences bool
    operatorValidation bool
}

func NewSyntaxTester(fileParsing, crossRefs, operatorValidation bool) *SyntaxTester {
    return &SyntaxTester{
        fileParsing:       fileParsing,
        crossReferences:   crossRefs,
        operatorValidation: operatorValidation,
    }
}

func (s *SyntaxTester) TestFileParsing(content string) error {
    // Test basic file parsing
    lines := strings.Split(content, "\n")
    for i, line := range lines {
        lineNum := i + 1
        if err := s.validateLine(line, lineNum); err != nil {
            return fmt.Errorf("line %d: %w", lineNum, err)
        }
    }
    return nil
}

func (s *SyntaxTester) validateLine(line string, lineNum int) error {
    line = strings.TrimSpace(line)
    
    // Skip empty lines and comments
    if line == "" || strings.HasPrefix(line, "#") {
        return nil
    }
    
    // Validate section headers
    if strings.HasPrefix(line, "[") && strings.HasSuffix(line, "]") {
        return s.validateSectionHeader(line, lineNum)
    }
    
    // Validate key-value pairs
    if strings.Contains(line, ":") || strings.Contains(line, "=") {
        return s.validateKeyValue(line, lineNum)
    }
    
    return fmt.Errorf("invalid syntax")
}

func (s *SyntaxTester) validateSectionHeader(line string, lineNum int) error {
    // Remove brackets
    section := strings.Trim(line, "[]")
    
    // Validate section name
    if section == "" {
        return errors.New("empty section name")
    }
    
    // Check for valid characters
    if !regexp.MustCompile(`^[a-zA-Z0-9_-]+$`).MatchString(section) {
        return errors.New("invalid section name characters")
    }
    
    return nil
}

func (s *SyntaxTester) validateKeyValue(line string, lineNum int) error {
    var key, value string
    var separator string
    
    // Check for colon separator
    if strings.Contains(line, ":") {
        parts := strings.SplitN(line, ":", 2)
        key = strings.TrimSpace(parts[0])
        value = strings.TrimSpace(parts[1])
        separator = ":"
    } else if strings.Contains(line, "=") {
        parts := strings.SplitN(line, "=", 2)
        key = strings.TrimSpace(parts[0])
        value = strings.TrimSpace(parts[1])
        separator = "="
    } else {
        return errors.New("missing key-value separator")
    }
    
    // Validate key
    if key == "" {
        return errors.New("empty key")
    }
    
    if !regexp.MustCompile(`^[a-zA-Z0-9_-]+$`).MatchString(key) {
        return errors.New("invalid key characters")
    }
    
    return nil
}
```

## 🔗 **Integration Testing**

### **Database Integration Tests**

```go
// TuskLang - Database integration tests
[integration_tests]
database_tests: @test.database({
    "connection_testing": true,
    "query_validation": true,
    "transaction_testing": true,
    "performance_monitoring": true
})

api_tests: @test.api({
    "endpoint_testing": true,
    "authentication_testing": true,
    "response_validation": true,
    "error_handling": true
})
```

```go
// Go - Database integration testing
type IntegrationTests struct {
    DatabaseTests map[string]bool `tsk:"database_tests"`
    APITests      map[string]bool `tsk:"api_tests"`
}

type DatabaseTester struct {
    connectionString string
    timeout          time.Duration
}

func NewDatabaseTester(connString string, timeout time.Duration) *DatabaseTester {
    return &DatabaseTester{
        connectionString: connString,
        timeout:          timeout,
    }
}

func (d *DatabaseTester) TestConnection() error {
    db, err := sql.Open("postgres", d.connectionString)
    if err != nil {
        return fmt.Errorf("failed to open database connection: %w", err)
    }
    defer db.Close()
    
    // Set timeout
    ctx, cancel := context.WithTimeout(context.Background(), d.timeout)
    defer cancel()
    
    // Test connection
    if err := db.PingContext(ctx); err != nil {
        return fmt.Errorf("failed to ping database: %w", err)
    }
    
    return nil
}

func (d *DatabaseTester) TestQuery(query string, expectedColumns []string) error {
    db, err := sql.Open("postgres", d.connectionString)
    if err != nil {
        return fmt.Errorf("failed to open database connection: %w", err)
    }
    defer db.Close()
    
    // Execute query
    rows, err := db.Query(query)
    if err != nil {
        return fmt.Errorf("failed to execute query: %w", err)
    }
    defer rows.Close()
    
    // Check columns
    columns, err := rows.Columns()
    if err != nil {
        return fmt.Errorf("failed to get columns: %w", err)
    }
    
    if len(columns) != len(expectedColumns) {
        return fmt.Errorf("column count mismatch: expected %d, got %d", 
            len(expectedColumns), len(columns))
    }
    
    for i, col := range columns {
        if col != expectedColumns[i] {
            return fmt.Errorf("column name mismatch at position %d: expected %s, got %s", 
                i, expectedColumns[i], col)
        }
    }
    
    return nil
}

func (d *DatabaseTester) TestTransaction() error {
    db, err := sql.Open("postgres", d.connectionString)
    if err != nil {
        return fmt.Errorf("failed to open database connection: %w", err)
    }
    defer db.Close()
    
    // Start transaction
    tx, err := db.Begin()
    if err != nil {
        return fmt.Errorf("failed to begin transaction: %w", err)
    }
    
    // Test operations
    _, err = tx.Exec("SELECT 1")
    if err != nil {
        tx.Rollback()
        return fmt.Errorf("failed to execute test query: %w", err)
    }
    
    // Commit transaction
    if err := tx.Commit(); err != nil {
        return fmt.Errorf("failed to commit transaction: %w", err)
    }
    
    return nil
}
```

### **API Integration Tests**

```go
// Go - API integration testing
type APITester struct {
    baseURL string
    client  *http.Client
}

func NewAPITester(baseURL string, timeout time.Duration) *APITester {
    return &APITester{
        baseURL: baseURL,
        client: &http.Client{
            Timeout: timeout,
        },
    }
}

func (a *APITester) TestEndpoint(path string, method string, expectedStatus int) error {
    url := a.baseURL + path
    req, err := http.NewRequest(method, url, nil)
    if err != nil {
        return fmt.Errorf("failed to create request: %w", err)
    }
    
    resp, err := a.client.Do(req)
    if err != nil {
        return fmt.Errorf("failed to execute request: %w", err)
    }
    defer resp.Body.Close()
    
    if resp.StatusCode != expectedStatus {
        return fmt.Errorf("unexpected status code: expected %d, got %d", 
            expectedStatus, resp.StatusCode)
    }
    
    return nil
}

func (a *APITester) TestAuthentication(path string, token string) error {
    url := a.baseURL + path
    req, err := http.NewRequest("GET", url, nil)
    if err != nil {
        return fmt.Errorf("failed to create request: %w", err)
    }
    
    req.Header.Set("Authorization", "Bearer "+token)
    
    resp, err := a.client.Do(req)
    if err != nil {
        return fmt.Errorf("failed to execute request: %w", err)
    }
    defer resp.Body.Close()
    
    if resp.StatusCode == http.StatusUnauthorized {
        return errors.New("authentication failed")
    }
    
    return nil
}

func (a *APITester) TestResponseValidation(path string, validator func([]byte) error) error {
    url := a.baseURL + path
    req, err := http.NewRequest("GET", url, nil)
    if err != nil {
        return fmt.Errorf("failed to create request: %w", err)
    }
    
    resp, err := a.client.Do(req)
    if err != nil {
        return fmt.Errorf("failed to execute request: %w", err)
    }
    defer resp.Body.Close()
    
    body, err := io.ReadAll(resp.Body)
    if err != nil {
        return fmt.Errorf("failed to read response body: %w", err)
    }
    
    return validator(body)
}
```

## 🔧 **Go Integration**

### **Testing Framework**

```go
// Go - Testing framework
type TestingFramework struct {
    unitTests       *UnitTests
    integrationTests *IntegrationTests
    configValidator *ConfigValidator
    syntaxTester    *SyntaxTester
    databaseTester  *DatabaseTester
    apiTester       *APITester
}

func NewTestingFramework(config TestingConfig) *TestingFramework {
    return &TestingFramework{
        unitTests:       &UnitTests{},
        integrationTests: &IntegrationTests{},
        configValidator: NewConfigValidator([]string{"api_key", "database_url"}, true, true),
        syntaxTester:    NewSyntaxTester(true, true, true),
        databaseTester:  NewDatabaseTester("postgres://localhost/test", 30*time.Second),
        apiTester:       NewAPITester("http://localhost:8080", 30*time.Second),
    }
}

func (t *TestingFramework) RunAllTests() error {
    var errors []string
    
    // Run unit tests
    if err := t.RunUnitTests(); err != nil {
        errors = append(errors, fmt.Sprintf("unit tests failed: %v", err))
    }
    
    // Run integration tests
    if err := t.RunIntegrationTests(); err != nil {
        errors = append(errors, fmt.Sprintf("integration tests failed: %v", err))
    }
    
    if len(errors) > 0 {
        return fmt.Errorf("test failures: %s", strings.Join(errors, "; "))
    }
    
    return nil
}

func (t *TestingFramework) RunUnitTests() error {
    // Test configuration validation
    testConfig := map[string]interface{}{
        "api_key":      "test_api_key_123456789012345678901234567890",
        "database_url": "postgres://localhost/test",
        "port":         8080,
    }
    
    if err := t.configValidator.ValidateConfig(testConfig); err != nil {
        return fmt.Errorf("config validation failed: %w", err)
    }
    
    // Test syntax validation
    testContent := `
[test_section]
api_key: test_key
port: 8080
database_url: postgres://localhost/test
`
    
    if err := t.syntaxTester.TestFileParsing(testContent); err != nil {
        return fmt.Errorf("syntax validation failed: %w", err)
    }
    
    return nil
}

func (t *TestingFramework) RunIntegrationTests() error {
    // Test database connection
    if err := t.databaseTester.TestConnection(); err != nil {
        return fmt.Errorf("database connection test failed: %w", err)
    }
    
    // Test API endpoints
    if err := t.apiTester.TestEndpoint("/health", "GET", http.StatusOK); err != nil {
        return fmt.Errorf("API endpoint test failed: %w", err)
    }
    
    return nil
}
```

### **Test Runner**

```go
// Go - Test runner
type TestRunner struct {
    framework *TestingFramework
    results   []TestResult
}

type TestResult struct {
    Name      string
    Status    string
    Duration  time.Duration
    Error     error
    Timestamp time.Time
}

func NewTestRunner(framework *TestingFramework) *TestRunner {
    return &TestRunner{
        framework: framework,
        results:   make([]TestResult, 0),
    }
}

func (t *TestRunner) RunTest(name string, testFunc func() error) {
    start := time.Now()
    err := testFunc()
    duration := time.Since(start)
    
    result := TestResult{
        Name:      name,
        Duration:  duration,
        Timestamp: time.Now(),
    }
    
    if err != nil {
        result.Status = "FAILED"
        result.Error = err
    } else {
        result.Status = "PASSED"
    }
    
    t.results = append(t.results, result)
}

func (t *TestRunner) RunAllTests() {
    t.RunTest("Configuration Validation", func() error {
        return t.framework.RunUnitTests()
    })
    
    t.RunTest("Integration Tests", func() error {
        return t.framework.RunIntegrationTests()
    })
}

func (t *TestRunner) GetResults() []TestResult {
    return t.results
}

func (t *TestRunner) GetSummary() map[string]interface{} {
    total := len(t.results)
    passed := 0
    failed := 0
    totalDuration := time.Duration(0)
    
    for _, result := range t.results {
        totalDuration += result.Duration
        if result.Status == "PASSED" {
            passed++
        } else {
            failed++
        }
    }
    
    return map[string]interface{}{
        "total":         total,
        "passed":        passed,
        "failed":        failed,
        "total_duration": totalDuration,
        "success_rate":  float64(passed) / float64(total) * 100,
    }
}
```

## 🚀 **Testing Patterns**

### **Configuration Testing**

```go
// TuskLang - Configuration testing patterns
[config_testing]
validation_tests: @test.pattern({
    "required_fields": ["api_key", "database_url", "port"],
    "type_validation": {
        "api_key": "string",
        "port": "integer",
        "debug": "boolean"
    },
    "format_validation": {
        "api_key": "min_length:32",
        "port": "range:1-65535",
        "database_url": "url_format"
    }
})
```

```go
// Go - Configuration testing patterns
type ConfigTesting struct {
    ValidationTests map[string]interface{} `tsk:"validation_tests"`
}

type ConfigTestPattern struct {
    RequiredFields   []string                 `json:"required_fields"`
    TypeValidation   map[string]string        `json:"type_validation"`
    FormatValidation map[string]string        `json:"format_validation"`
}

func (c *ConfigTesting) TestConfigPattern(config map[string]interface{}, pattern ConfigTestPattern) error {
    // Test required fields
    for _, field := range pattern.RequiredFields {
        if _, exists := config[field]; !exists {
            return fmt.Errorf("required field '%s' is missing", field)
        }
    }
    
    // Test type validation
    for field, expectedType := range pattern.TypeValidation {
        if value, exists := config[field]; exists {
            if err := c.validateType(value, expectedType); err != nil {
                return fmt.Errorf("type validation failed for '%s': %w", field, err)
            }
        }
    }
    
    // Test format validation
    for field, format := range pattern.FormatValidation {
        if value, exists := config[field]; exists {
            if err := c.validateFormat(value, format); err != nil {
                return fmt.Errorf("format validation failed for '%s': %w", field, err)
            }
        }
    }
    
    return nil
}

func (c *ConfigTesting) validateType(value interface{}, expectedType string) error {
    switch expectedType {
    case "string":
        if _, ok := value.(string); !ok {
            return errors.New("expected string type")
        }
    case "integer":
        if _, ok := value.(int); !ok {
            return errors.New("expected integer type")
        }
    case "boolean":
        if _, ok := value.(bool); !ok {
            return errors.New("expected boolean type")
        }
    default:
        return fmt.Errorf("unsupported type: %s", expectedType)
    }
    
    return nil
}

func (c *ConfigTesting) validateFormat(value interface{}, format string) error {
    switch format {
    case "min_length:32":
        if str, ok := value.(string); ok {
            if len(str) < 32 {
                return errors.New("string length must be at least 32 characters")
            }
        }
    case "range:1-65535":
        if num, ok := value.(int); ok {
            if num < 1 || num > 65535 {
                return errors.New("number must be between 1 and 65535")
            }
        }
    case "url_format":
        if str, ok := value.(string); ok {
            if _, err := url.Parse(str); err != nil {
                return errors.New("invalid URL format")
            }
        }
    default:
        return fmt.Errorf("unsupported format: %s", format)
    }
    
    return nil
}
```

## ⚡ **Performance Testing**

### **Load Testing**

```go
// Go - Load testing
type LoadTester struct {
    targetURL string
    client    *http.Client
}

func NewLoadTester(targetURL string) *LoadTester {
    return &LoadTester{
        targetURL: targetURL,
        client: &http.Client{
            Timeout: 30 * time.Second,
        },
    }
}

func (l *LoadTester) RunLoadTest(concurrentUsers int, duration time.Duration) LoadTestResult {
    start := time.Now()
    results := make(chan RequestResult, concurrentUsers*100)
    
    // Start workers
    for i := 0; i < concurrentUsers; i++ {
        go l.worker(results)
    }
    
    // Collect results
    var totalRequests int
    var successfulRequests int
    var failedRequests int
    var totalResponseTime time.Duration
    
    timeout := time.After(duration)
    
    for {
        select {
        case result := <-results:
            totalRequests++
            totalResponseTime += result.ResponseTime
            
            if result.Error == nil {
                successfulRequests++
            } else {
                failedRequests++
            }
            
        case <-timeout:
            avgResponseTime := totalResponseTime / time.Duration(totalRequests)
            
            return LoadTestResult{
                TotalRequests:     totalRequests,
                SuccessfulRequests: successfulRequests,
                FailedRequests:    failedRequests,
                AverageResponseTime: avgResponseTime,
                Duration:          time.Since(start),
                RequestsPerSecond: float64(totalRequests) / duration.Seconds(),
            }
        }
    }
}

type RequestResult struct {
    ResponseTime time.Duration
    Error        error
}

type LoadTestResult struct {
    TotalRequests        int
    SuccessfulRequests   int
    FailedRequests       int
    AverageResponseTime  time.Duration
    Duration             time.Duration
    RequestsPerSecond    float64
}

func (l *LoadTester) worker(results chan<- RequestResult) {
    for {
        start := time.Now()
        
        req, err := http.NewRequest("GET", l.targetURL, nil)
        if err != nil {
            results <- RequestResult{ResponseTime: time.Since(start), Error: err}
            continue
        }
        
        resp, err := l.client.Do(req)
        if err != nil {
            results <- RequestResult{ResponseTime: time.Since(start), Error: err}
            continue
        }
        resp.Body.Close()
        
        results <- RequestResult{ResponseTime: time.Since(start), Error: nil}
    }
}
```

## 🌍 **Real-World Examples**

### **Complete Testing Suite**

```go
// TuskLang - Complete testing suite
[testing_suite]
test_config: @test.suite({
    "name": "TuskLang Configuration Tests",
    "version": "1.0.0",
    "description": "Comprehensive testing suite for TuskLang configuration",
    "tests": [
        "unit_tests",
        "integration_tests",
        "performance_tests",
        "security_tests"
    ],
    "timeout": "5m",
    "parallel": true
})
```

```go
// Go - Complete testing suite
type TestingSuite struct {
    TestConfig map[string]interface{} `tsk:"test_config"`
}

type SuiteConfig struct {
    Name        string   `json:"name"`
    Version     string   `json:"version"`
    Description string   `json:"description"`
    Tests       []string `json:"tests"`
    Timeout     string   `json:"timeout"`
    Parallel    bool     `json:"parallel"`
}

func (t *TestingSuite) RunCompleteSuite(config SuiteConfig) SuiteResult {
    start := time.Now()
    results := make(map[string]TestResult)
    
    // Parse timeout
    timeout, _ := time.ParseDuration(config.Timeout)
    ctx, cancel := context.WithTimeout(context.Background(), timeout)
    defer cancel()
    
    // Run tests
    for _, testName := range config.Tests {
        select {
        case <-ctx.Done():
            results[testName] = TestResult{
                Name:   testName,
                Status: "TIMEOUT",
                Error:  ctx.Err(),
            }
            continue
        default:
            result := t.runTest(testName)
            results[testName] = result
        }
    }
    
    return SuiteResult{
        Config:  config,
        Results: results,
        Duration: time.Since(start),
    }
}

type SuiteResult struct {
    Config   SuiteConfig
    Results  map[string]TestResult
    Duration time.Duration
}

func (t *TestingSuite) runTest(testName string) TestResult {
    start := time.Now()
    
    var err error
    switch testName {
    case "unit_tests":
        err = t.runUnitTests()
    case "integration_tests":
        err = t.runIntegrationTests()
    case "performance_tests":
        err = t.runPerformanceTests()
    case "security_tests":
        err = t.runSecurityTests()
    default:
        err = fmt.Errorf("unknown test: %s", testName)
    }
    
    result := TestResult{
        Name:      testName,
        Duration:  time.Since(start),
        Timestamp: time.Now(),
    }
    
    if err != nil {
        result.Status = "FAILED"
        result.Error = err
    } else {
        result.Status = "PASSED"
    }
    
    return result
}

func (t *TestingSuite) runUnitTests() error {
    // Implementation of unit tests
    return nil
}

func (t *TestingSuite) runIntegrationTests() error {
    // Implementation of integration tests
    return nil
}

func (t *TestingSuite) runPerformanceTests() error {
    // Implementation of performance tests
    return nil
}

func (t *TestingSuite) runSecurityTests() error {
    // Implementation of security tests
    return nil
}
```

## 🎯 **Best Practices**

### **1. Test Early and Often**

```go
// ✅ Good - Early testing
func (c *Config) LoadAndTest() error {
    // Load configuration
    if err := c.Load(); err != nil {
        return fmt.Errorf("failed to load config: %w", err)
    }
    
    // Test immediately
    if err := c.Test(); err != nil {
        return fmt.Errorf("configuration test failed: %w", err)
    }
    
    return nil
}

// ❌ Bad - Late testing
func (c *Config) Load() error {
    // Load configuration without testing
    // Testing happens much later, potentially causing issues
    return nil
}
```

### **2. Use Comprehensive Test Coverage**

```go
// ✅ Good - Comprehensive testing
func (t *TestingFramework) RunComprehensiveTests() error {
    tests := []struct {
        name string
        test func() error
    }{
        {"Configuration Validation", t.testConfigValidation},
        {"Syntax Validation", t.testSyntaxValidation},
        {"Database Connection", t.testDatabaseConnection},
        {"API Endpoints", t.testAPIEndpoints},
        {"Performance", t.testPerformance},
        {"Security", t.testSecurity},
    }
    
    for _, test := range tests {
        if err := test.test(); err != nil {
            return fmt.Errorf("%s failed: %w", test.name, err)
        }
    }
    
    return nil
}

// ❌ Bad - Limited testing
func (t *TestingFramework) RunBasicTests() error {
    // Only test basic functionality
    return t.testBasicFunctionality()
}
```

### **3. Use Parallel Testing**

```go
// ✅ Good - Parallel testing
func (t *TestingFramework) RunParallelTests() error {
    tests := []func() error{
        t.testConfigValidation,
        t.testSyntaxValidation,
        t.testDatabaseConnection,
        t.testAPIEndpoints,
    }
    
    var wg sync.WaitGroup
    errors := make(chan error, len(tests))
    
    for _, test := range tests {
        wg.Add(1)
        go func(testFunc func() error) {
            defer wg.Done()
            if err := testFunc(); err != nil {
                errors <- err
            }
        }(test)
    }
    
    wg.Wait()
    close(errors)
    
    var testErrors []string
    for err := range errors {
        testErrors = append(testErrors, err.Error())
    }
    
    if len(testErrors) > 0 {
        return fmt.Errorf("test failures: %s", strings.Join(testErrors, "; "))
    }
    
    return nil
}

// ❌ Bad - Sequential testing
func (t *TestingFramework) RunSequentialTests() error {
    // Run tests one by one, slower execution
    if err := t.testConfigValidation(); err != nil {
        return err
    }
    if err := t.testSyntaxValidation(); err != nil {
        return err
    }
    // ... more tests
    return nil
}
```

### **4. Use Test Data Management**

```go
// ✅ Good - Test data management
type TestDataManager struct {
    testData map[string]interface{}
}

func NewTestDataManager() *TestDataManager {
    return &TestDataManager{
        testData: make(map[string]interface{}),
    }
}

func (t *TestDataManager) SetupTestData() error {
    t.testData["valid_config"] = map[string]interface{}{
        "api_key":      "test_api_key_123456789012345678901234567890",
        "database_url": "postgres://localhost/test",
        "port":         8080,
        "debug":        true,
    }
    
    t.testData["invalid_config"] = map[string]interface{}{
        "api_key": "short",
        "port":    "not_a_number",
    }
    
    return nil
}

func (t *TestDataManager) GetTestData(name string) (interface{}, error) {
    data, exists := t.testData[name]
    if !exists {
        return nil, fmt.Errorf("test data '%s' not found", name)
    }
    return data, nil
}

func (t *TestDataManager) CleanupTestData() error {
    t.testData = make(map[string]interface{})
    return nil
}

// ❌ Bad - Hardcoded test data
func (t *TestingFramework) testConfigValidation() error {
    // Hardcoded test data scattered throughout tests
    config := map[string]interface{}{
        "api_key": "test_key",
        "port":    8080,
    }
    // ... test logic
    return nil
}
```

### **5. Use Proper Error Reporting**

```go
// ✅ Good - Detailed error reporting
func (t *TestingFramework) RunTestsWithDetailedReporting() []TestResult {
    var results []TestResult
    
    tests := []struct {
        name string
        test func() error
    }{
        {"Configuration Validation", t.testConfigValidation},
        {"Syntax Validation", t.testSyntaxValidation},
        {"Database Connection", t.testDatabaseConnection},
    }
    
    for _, test := range tests {
        start := time.Now()
        err := test.test()
        duration := time.Since(start)
        
        result := TestResult{
            Name:      test.name,
            Duration:  duration,
            Timestamp: time.Now(),
        }
        
        if err != nil {
            result.Status = "FAILED"
            result.Error = err
        } else {
            result.Status = "PASSED"
        }
        
        results = append(results, result)
    }
    
    return results
}

// ❌ Bad - Basic error reporting
func (t *TestingFramework) RunTests() error {
    // Only return first error, no details
    if err := t.testConfigValidation(); err != nil {
        return err
    }
    if err := t.testSyntaxValidation(); err != nil {
        return err
    }
    return nil
}
```

---

**🎉 You've mastered testing in TuskLang with Go!**

Testing in TuskLang ensures your configuration is reliable and bulletproof. With comprehensive testing strategies, you can build robust systems that catch issues early and maintain quality.

**Next Steps:**
- Explore [026-performance-go.md](026-performance-go.md) for optimization
- Master [027-deployment-go.md](027-deployment-go.md) for deployment
- Dive into [028-monitoring-go.md](028-monitoring-go.md) for monitoring

**Remember:** In TuskLang, testing isn't optional—it's essential. Use it wisely to build bulletproof, reliable systems. 
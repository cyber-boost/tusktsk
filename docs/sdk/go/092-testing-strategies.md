# Testing Strategies

TuskLang revolutionizes testing with its declarative approach and powerful test orchestration capabilities. This guide covers comprehensive testing strategies for Go applications.

## Testing Philosophy

### Test-Driven Development (TDD)
```go
// TDD approach with TuskLang
type Calculator struct {
    config *tusk.Config
}

func TestCalculator_Add(t *testing.T) {
    // Arrange
    calc := &Calculator{
        config: loadTestConfig(),
    }
    
    // Act
    result := calc.Add(2, 3)
    
    // Assert
    expected := 5
    if result != expected {
        t.Errorf("Add(2, 3) = %d; want %d", result, expected)
    }
}

func loadTestConfig() *tusk.Config {
    config, err := tusk.LoadConfig("test_config.tsk")
    if err != nil {
        panic(fmt.Sprintf("failed to load test config: %v", err))
    }
    return config
}
```

### Behavior-Driven Development (BDD)
```go
// BDD style testing with TuskLang
func TestUserRegistration_BDD(t *testing.T) {
    Given(t, "a new user wants to register")
    When(t, "they provide valid credentials")
    Then(t, "the registration should succeed")
    
    // Implementation
    user := &User{
        Email:    "test@example.com",
        Password: "securepassword",
    }
    
    result := RegisterUser(user)
    assert.True(t, result.Success)
    assert.NotEmpty(t, result.UserID)
}
```

## TuskLang Test Configuration

### Test Environment Setup
```tsk
# Test configuration
test_environment {
    # Database configuration
    database {
        type = "sqlite"
        dsn = ":memory:"
        migrations = "test_migrations"
    }
    
    # Mock services
    mocks {
        external_api = true
        payment_gateway = true
        email_service = true
    }
    
    # Test data
    fixtures {
        users = "testdata/users.json"
        products = "testdata/products.json"
        orders = "testdata/orders.json"
    }
    
    # Test settings
    settings {
        parallel_tests = true
        timeout = "30s"
        cleanup = true
        seed = 12345
    }
}
```

### Test Data Management
```tsk
# Test data configuration
test_data {
    # Factory definitions
    factories {
        user {
            email = "user@example.com"
            password = "password123"
            role = "user"
        }
        
        admin {
            email = "admin@example.com"
            password = "admin123"
            role = "admin"
        }
        
        product {
            name = "Test Product"
            price = 99.99
            category = "electronics"
        }
    }
    
    # Test scenarios
    scenarios {
        valid_registration {
            user_type = "user"
            expected_status = "success"
        }
        
        invalid_email {
            user_type = "user"
            email = "invalid-email"
            expected_status = "error"
        }
    }
}
```

## Go Testing Implementation

### Unit Testing
```go
// Unit testing with TuskLang configuration
type UserService struct {
    db     *sql.DB
    config *tusk.Config
}

func TestUserService_CreateUser(t *testing.T) {
    // Setup test database
    db, cleanup := setupTestDB(t)
    defer cleanup()
    
    // Load test configuration
    config := loadTestConfig(t)
    
    service := &UserService{
        db:     db,
        config: config,
    }
    
    tests := []struct {
        name    string
        user    *User
        wantErr bool
    }{
        {
            name: "valid user",
            user: &User{
                Email:    "test@example.com",
                Password: "password123",
            },
            wantErr: false,
        },
        {
            name: "invalid email",
            user: &User{
                Email:    "invalid-email",
                Password: "password123",
            },
            wantErr: true,
        },
    }
    
    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            user, err := service.CreateUser(tt.user)
            if (err != nil) != tt.wantErr {
                t.Errorf("CreateUser() error = %v, wantErr %v", err, tt.wantErr)
                return
            }
            
            if !tt.wantErr && user == nil {
                t.Error("CreateUser() returned nil user when no error expected")
            }
        })
    }
}

func setupTestDB(t *testing.T) (*sql.DB, func()) {
    db, err := sql.Open("sqlite3", ":memory:")
    if err != nil {
        t.Fatalf("failed to open test database: %v", err)
    }
    
    // Run migrations
    if err := runMigrations(db); err != nil {
        t.Fatalf("failed to run migrations: %v", err)
    }
    
    cleanup := func() {
        db.Close()
    }
    
    return db, cleanup
}
```

### Integration Testing
```go
// Integration testing with external services
type IntegrationTestSuite struct {
    server *httptest.Server
    db     *sql.DB
    config *tusk.Config
}

func TestIntegration_UserWorkflow(t *testing.T) {
    suite := setupIntegrationSuite(t)
    defer suite.cleanup()
    
    // Test complete user workflow
    t.Run("user registration and login", func(t *testing.T) {
        // Register user
        user := &User{
            Email:    "integration@example.com",
            Password: "password123",
        }
        
        resp, err := http.PostJSON(suite.server.URL+"/register", user)
        assert.NoError(t, err)
        assert.Equal(t, http.StatusCreated, resp.StatusCode)
        
        // Login user
        loginData := map[string]string{
            "email":    user.Email,
            "password": user.Password,
        }
        
        resp, err = http.PostJSON(suite.server.URL+"/login", loginData)
        assert.NoError(t, err)
        assert.Equal(t, http.StatusOK, resp.StatusCode)
        
        // Verify user in database
        var dbUser User
        err = suite.db.QueryRow("SELECT * FROM users WHERE email = ?", user.Email).Scan(&dbUser)
        assert.NoError(t, err)
        assert.Equal(t, user.Email, dbUser.Email)
    })
}

func setupIntegrationSuite(t *testing.T) *IntegrationTestSuite {
    // Setup test database
    db, err := sql.Open("sqlite3", ":memory:")
    if err != nil {
        t.Fatalf("failed to open test database: %v", err)
    }
    
    // Load integration test configuration
    config := loadIntegrationConfig(t)
    
    // Setup test server
    server := httptest.NewServer(setupTestHandler(db, config))
    
    return &IntegrationTestSuite{
        server: server,
        db:     db,
        config: config,
    }
}
```

### Performance Testing
```go
// Performance testing with benchmarks
func BenchmarkUserService_CreateUser(b *testing.B) {
    db, cleanup := setupTestDB(b)
    defer cleanup()
    
    config := loadTestConfig(b)
    service := &UserService{db: db, config: config}
    
    b.ResetTimer()
    for i := 0; i < b.N; i++ {
        user := &User{
            Email:    fmt.Sprintf("user%d@example.com", i),
            Password: "password123",
        }
        
        _, err := service.CreateUser(user)
        if err != nil {
            b.Fatalf("CreateUser failed: %v", err)
        }
    }
}

func BenchmarkConcurrentUserCreation(b *testing.B) {
    db, cleanup := setupTestDB(b)
    defer cleanup()
    
    config := loadTestConfig(b)
    service := &UserService{db: db, config: config}
    
    b.ResetTimer()
    b.RunParallel(func(pb *testing.PB) {
        i := 0
        for pb.Next() {
            user := &User{
                Email:    fmt.Sprintf("user%d@example.com", i),
                Password: "password123",
            }
            
            _, err := service.CreateUser(user)
            if err != nil {
                b.Fatalf("CreateUser failed: %v", err)
            }
            i++
        }
    })
}
```

## Advanced Testing Features

### Test Data Factories
```go
// Test data factories with TuskLang
type TestDataFactory struct {
    config *tusk.Config
}

func (tdf *TestDataFactory) CreateUser(overrides map[string]interface{}) *User {
    // Load base user template from config
    var baseUser User
    tdf.config.Get("test_data.factories.user", &baseUser)
    
    // Apply overrides
    if email, ok := overrides["email"].(string); ok {
        baseUser.Email = email
    }
    if password, ok := overrides["password"].(string); ok {
        baseUser.Password = password
    }
    if role, ok := overrides["role"].(string); ok {
        baseUser.Role = role
    }
    
    return &baseUser
}

func (tdf *TestDataFactory) CreateProduct(overrides map[string]interface{}) *Product {
    var baseProduct Product
    tdf.config.Get("test_data.factories.product", &baseProduct)
    
    // Apply overrides
    if name, ok := overrides["name"].(string); ok {
        baseProduct.Name = name
    }
    if price, ok := overrides["price"].(float64); ok {
        baseProduct.Price = price
    }
    
    return &baseProduct
}
```

### Mock Services
```go
// Mock services with TuskLang configuration
type MockServiceProvider struct {
    config *tusk.Config
    mocks  map[string]interface{}
}

func (msp *MockServiceProvider) SetupMocks() {
    // Setup mocks based on configuration
    if msp.config.GetBool("test_environment.mocks.external_api") {
        msp.mocks["external_api"] = NewMockExternalAPI()
    }
    
    if msp.config.GetBool("test_environment.mocks.payment_gateway") {
        msp.mocks["payment_gateway"] = NewMockPaymentGateway()
    }
    
    if msp.config.GetBool("test_environment.mocks.email_service") {
        msp.mocks["email_service"] = NewMockEmailService()
    }
}

func (msp *MockServiceProvider) GetMock(name string) interface{} {
    return msp.mocks[name]
}

type MockExternalAPI struct {
    responses map[string]interface{}
}

func (m *MockExternalAPI) Get(url string) ([]byte, error) {
    if response, exists := m.responses[url]; exists {
        return json.Marshal(response)
    }
    return nil, errors.New("mock not configured")
}
```

### Test Orchestration
```go
// Test orchestration with TuskLang
type TestOrchestrator struct {
    config *tusk.Config
    suite  *TestSuite
}

func (to *TestOrchestrator) RunTestSuite() error {
    // Load test configuration
    if err := to.loadTestConfig(); err != nil {
        return fmt.Errorf("failed to load test config: %w", err)
    }
    
    // Setup test environment
    if err := to.setupEnvironment(); err != nil {
        return fmt.Errorf("failed to setup environment: %w", err)
    }
    
    // Run tests
    if err := to.runTests(); err != nil {
        return fmt.Errorf("failed to run tests: %w", err)
    }
    
    // Cleanup
    if err := to.cleanup(); err != nil {
        return fmt.Errorf("failed to cleanup: %w", err)
    }
    
    return nil
}

func (to *TestOrchestrator) loadTestConfig() error {
    config, err := tusk.LoadConfig("test_config.tsk")
    if err != nil {
        return err
    }
    to.config = config
    return nil
}

func (to *TestOrchestrator) setupEnvironment() error {
    // Setup database
    if err := to.setupDatabase(); err != nil {
        return err
    }
    
    // Setup mocks
    if err := to.setupMocks(); err != nil {
        return err
    }
    
    // Load fixtures
    if err := to.loadFixtures(); err != nil {
        return err
    }
    
    return nil
}
```

## Test Reporting and Coverage

### Coverage Analysis
```go
// Test coverage analysis
type CoverageAnalyzer struct {
    config *tusk.Config
}

func (ca *CoverageAnalyzer) AnalyzeCoverage() (*CoverageReport, error) {
    // Run tests with coverage
    coverage, err := ca.runTestsWithCoverage()
    if err != nil {
        return nil, fmt.Errorf("failed to run tests with coverage: %w", err)
    }
    
    // Generate coverage report
    report := &CoverageReport{
        OverallCoverage: coverage.Overall,
        PackageCoverage: coverage.Packages,
        Timestamp:       time.Now(),
    }
    
    // Check coverage thresholds
    threshold := ca.config.GetFloat("test_settings.coverage_threshold", 80.0)
    if report.OverallCoverage < threshold {
        return report, fmt.Errorf("coverage below threshold: %.2f%% < %.2f%%", 
            report.OverallCoverage, threshold)
    }
    
    return report, nil
}

type CoverageReport struct {
    OverallCoverage float64
    PackageCoverage map[string]float64
    Timestamp       time.Time
}
```

### Test Reporting
```go
// Test reporting with multiple formats
type TestReporter struct {
    config *tusk.Config
}

func (tr *TestReporter) GenerateReport(results *TestResults) error {
    // Generate JSON report
    if tr.config.GetBool("test_reporting.json") {
        if err := tr.generateJSONReport(results); err != nil {
            return fmt.Errorf("failed to generate JSON report: %w", err)
        }
    }
    
    // Generate HTML report
    if tr.config.GetBool("test_reporting.html") {
        if err := tr.generateHTMLReport(results); err != nil {
            return fmt.Errorf("failed to generate HTML report: %w", err)
        }
    }
    
    // Generate JUnit XML report
    if tr.config.GetBool("test_reporting.junit") {
        if err := tr.generateJUnitReport(results); err != nil {
            return fmt.Errorf("failed to generate JUnit report: %w", err)
        }
    }
    
    return nil
}

func (tr *TestReporter) generateJSONReport(results *TestResults) error {
    data, err := json.MarshalIndent(results, "", "  ")
    if err != nil {
        return err
    }
    
    return os.WriteFile("test-results.json", data, 0644)
}

func (tr *TestReporter) generateHTMLReport(results *TestResults) error {
    // Generate HTML report using template
    template := tr.loadHTMLTemplate()
    
    file, err := os.Create("test-results.html")
    if err != nil {
        return err
    }
    defer file.Close()
    
    return template.Execute(file, results)
}
```

## Validation and Error Handling

### Test Validation
```go
// Validate test configuration
func ValidateTestConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("test config cannot be nil")
    }
    
    // Validate required sections
    requiredSections := []string{"test_environment", "test_data"}
    for _, section := range requiredSections {
        if !config.Has(section) {
            return fmt.Errorf("missing required section: %s", section)
        }
    }
    
    return nil
}
```

### Error Handling
```go
// Handle test errors gracefully
func handleTestError(err error, testName string) {
    log.Printf("Test %s failed: %v", testName, err)
    
    // Log additional context if available
    if testErr, ok := err.(*TestError); ok {
        log.Printf("Test context: %s", testErr.Context)
    }
}
```

## Performance Considerations

### Test Performance Optimization
```go
// Optimize test performance
type TestOptimizer struct {
    config *tusk.Config
}

func (to *TestOptimizer) OptimizeTestSuite() error {
    // Enable parallel testing
    if to.config.GetBool("test_settings.parallel_tests") {
        runtime.GOMAXPROCS(runtime.NumCPU())
    }
    
    // Setup test caching
    if to.config.GetBool("test_settings.cache_results") {
        to.setupTestCache()
    }
    
    // Optimize database connections
    if err := to.optimizeDatabase(); err != nil {
        return fmt.Errorf("failed to optimize database: %w", err)
    }
    
    return nil
}

func (to *TestOptimizer) setupTestCache() {
    // Setup test result caching
    cacheDir := to.config.GetString("test_settings.cache_dir", ".testcache")
    os.MkdirAll(cacheDir, 0755)
}
```

## Testing Notes

- **Test Isolation**: Each test should be independent and not affect others
- **Test Data**: Use factories and fixtures for consistent test data
- **Mocking**: Mock external dependencies to ensure test reliability
- **Coverage**: Maintain high test coverage for critical code paths
- **Performance**: Monitor test performance and optimize slow tests
- **Reporting**: Generate comprehensive test reports for analysis
- **CI/CD**: Integrate tests into continuous integration pipeline

## Best Practices

1. **Test Structure**: Follow AAA pattern (Arrange, Act, Assert)
2. **Test Naming**: Use descriptive test names that explain the scenario
3. **Test Data**: Use factories and builders for test data creation
4. **Mocking**: Mock external dependencies, not internal logic
5. **Coverage**: Aim for high coverage but focus on critical paths
6. **Performance**: Keep tests fast and efficient
7. **Maintenance**: Keep tests maintainable and up-to-date
8. **Documentation**: Document complex test scenarios and setup

## Integration with TuskLang

```go
// Load test configuration from TuskLang
func LoadTestConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load test config: %w", err)
    }
    
    // Validate test configuration
    if err := ValidateTestConfig(config); err != nil {
        return nil, fmt.Errorf("invalid test config: %w", err)
    }
    
    return config, nil
}
```

This testing strategies guide provides comprehensive testing approaches for your Go applications using TuskLang. Remember, good tests are the foundation of reliable software. 
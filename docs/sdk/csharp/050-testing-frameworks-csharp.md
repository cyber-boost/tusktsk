# Testing Frameworks in C# TuskLang

## Overview

Comprehensive testing is essential for maintaining code quality and ensuring application reliability. This guide covers unit testing, integration testing, end-to-end testing, and testing best practices for C# TuskLang applications.

## 🧪 Unit Testing

### Test Base Classes

```csharp
public abstract class TestBase
{
    protected readonly ITestOutputHelper _output;
    protected readonly TSKConfig _config;
    protected readonly Mock<ILogger> _mockLogger;
    
    protected TestBase(ITestOutputHelper output)
    {
        _output = output;
        _config = CreateTestConfiguration();
        _mockLogger = new Mock<ILogger>();
    }
    
    protected virtual TSKConfig CreateTestConfiguration()
    {
        var config = new TSKConfig();
        
        // Set up test configuration
        config.Set("database.connection_string", "Data Source=:memory:");
        config.Set("app.environment", "test");
        config.Set("logging.level", "Debug");
        config.Set("cache.enabled", "false");
        
        return config;
    }
    
    protected virtual void WriteLine(string message)
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
    
    protected virtual async Task<TSKConfig> LoadTestConfigurationAsync(string configPath)
    {
        var config = new TSKConfig();
        await config.LoadAsync(configPath);
        return config;
    }
}

public abstract class DatabaseTestBase : TestBase
{
    protected readonly IDbConnection _connection;
    protected readonly IDbTransaction _transaction;
    
    protected DatabaseTestBase(ITestOutputHelper output) : base(output)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _transaction = _connection.BeginTransaction();
        
        SetupDatabase();
    }
    
    protected virtual void SetupDatabase()
    {
        // Create test tables
        var createUsersTable = @"
            CREATE TABLE users (
                id TEXT PRIMARY KEY,
                email TEXT NOT NULL UNIQUE,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT
            )";
        
        var createEventsTable = @"
            CREATE TABLE events (
                id TEXT PRIMARY KEY,
                event_type TEXT NOT NULL,
                event_data TEXT NOT NULL,
                created_at TEXT NOT NULL,
                status TEXT NOT NULL
            )";
        
        _connection.Execute(createUsersTable);
        _connection.Execute(createEventsTable);
    }
    
    protected virtual async Task<User> CreateTestUserAsync(string email = "test@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        var query = @"
            INSERT INTO users (id, email, first_name, last_name, status, created_at)
            VALUES (@Id, @Email, @FirstName, @LastName, @Status, @CreatedAt)";
        
        await _connection.ExecuteAsync(query, user);
        return user;
    }
    
    public override void Dispose()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
```

### User Service Tests

```csharp
public class UserServiceTests : DatabaseTestBase
{
    private readonly UserService _userService;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IHealthCheckService> _mockHealthCheckService;
    
    public UserServiceTests(ITestOutputHelper output) : base(output)
    {
        _mockEventBus = new Mock<IEventBus>();
        _mockHealthCheckService = new Mock<IHealthCheckService>();
        
        var userRepository = new UserRepository(_connection);
        
        _userService = new UserService(
            _mockLogger.Object,
            _config,
            Mock.Of<IServiceProvider>(),
            _mockHealthCheckService.Object,
            userRepository,
            _mockEventBus.Object);
    }
    
    [Fact]
    public async Task CreateUserAsync_ValidRequest_ShouldCreateUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        
        _mockEventBus.Setup(x => x.PublishAsync(It.IsAny<UserCreatedEvent>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await _userService.CreateUserAsync(request);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.LastName, result.LastName);
        Assert.Equal(UserStatus.Active, result.Status);
        
        // Verify event was published
        _mockEventBus.Verify(x => x.PublishAsync(It.Is<UserCreatedEvent>(e => 
            e.Email == request.Email)), Times.Once);
        
        WriteLine($"Created user with ID: {result.Id}");
    }
    
    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ShouldThrowException()
    {
        // Arrange
        var existingUser = await CreateTestUserAsync("test@example.com");
        
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "Jane",
            LastName = "Doe"
        };
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.CreateUserAsync(request));
        
        Assert.Contains("already exists", exception.Message);
        WriteLine($"Expected exception caught: {exception.Message}");
    }
    
    [Fact]
    public async Task GetUserAsync_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var testUser = await CreateTestUserAsync();
        
        // Act
        var result = await _userService.GetUserAsync(testUser.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(testUser.Id, result.Id);
        Assert.Equal(testUser.Email, result.Email);
        Assert.Equal(testUser.FirstName, result.FirstName);
        Assert.Equal(testUser.LastName, result.LastName);
        
        WriteLine($"Retrieved user: {result.Email}");
    }
    
    [Fact]
    public async Task GetUserAsync_NonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var result = await _userService.GetUserAsync(nonExistentId);
        
        // Assert
        Assert.Null(result);
        WriteLine("Non-existent user correctly returned null");
    }
    
    [Fact]
    public async Task UpdateUserAsync_ExistingUser_ShouldUpdateUser()
    {
        // Arrange
        var testUser = await CreateTestUserAsync();
        
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };
        
        _mockEventBus.Setup(x => x.PublishAsync(It.IsAny<UserUpdatedEvent>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _userService.UpdateUserAsync(testUser.Id, request);
        
        // Assert
        var updatedUser = await _userService.GetUserAsync(testUser.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(request.FirstName, updatedUser.FirstName);
        Assert.Equal(request.LastName, updatedUser.LastName);
        
        // Verify event was published
        _mockEventBus.Verify(x => x.PublishAsync(It.Is<UserUpdatedEvent>(e => 
            e.UserId == testUser.Id)), Times.Once);
        
        WriteLine($"Updated user: {updatedUser.Email}");
    }
    
    [Fact]
    public async Task UpdateUserAsync_NonExistentUser_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _userService.UpdateUserAsync(nonExistentId, request));
        
        Assert.Contains("not found", exception.Message);
        WriteLine($"Expected exception caught: {exception.Message}");
    }
}
```

### Configuration Tests

```csharp
public class TSKConfigTests : TestBase
{
    public TSKConfigTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Get_ExistingKey_ShouldReturnValue()
    {
        // Arrange
        _config.Set("test.key", "test_value");
        
        // Act
        var result = _config.Get<string>("test.key");
        
        // Assert
        Assert.Equal("test_value", result);
        WriteLine($"Retrieved config value: {result}");
    }
    
    [Fact]
    public void Get_NonExistentKey_ShouldReturnDefault()
    {
        // Act
        var result = _config.Get<string>("non.existent.key", "default_value");
        
        // Assert
        Assert.Equal("default_value", result);
        WriteLine($"Retrieved default value: {result}");
    }
    
    [Fact]
    public void Get_WithTypeConversion_ShouldConvertValue()
    {
        // Arrange
        _config.Set("test.number", "42");
        _config.Set("test.boolean", "true");
        _config.Set("test.double", "3.14");
        
        // Act
        var number = _config.Get<int>("test.number");
        var boolean = _config.Get<bool>("test.boolean");
        var doubleValue = _config.Get<double>("test.double");
        
        // Assert
        Assert.Equal(42, number);
        Assert.True(boolean);
        Assert.Equal(3.14, doubleValue);
        
        WriteLine($"Converted values: {number}, {boolean}, {doubleValue}");
    }
    
    [Fact]
    public void Has_ExistingKey_ShouldReturnTrue()
    {
        // Arrange
        _config.Set("test.key", "value");
        
        // Act
        var result = _config.Has("test.key");
        
        // Assert
        Assert.True(result);
        WriteLine("Key exists: true");
    }
    
    [Fact]
    public void Has_NonExistentKey_ShouldReturnFalse()
    {
        // Act
        var result = _config.Has("non.existent.key");
        
        // Assert
        Assert.False(result);
        WriteLine("Key exists: false");
    }
    
    [Fact]
    public async Task LoadAsync_ValidFile_ShouldLoadConfiguration()
    {
        // Arrange
        var configPath = "test-config.tsk";
        var configContent = @"
            app.name = ""Test App""
            app.version = ""1.0.0""
            database.connection_string = ""test_connection""
        ";
        
        await File.WriteAllTextAsync(configPath, configContent);
        
        try
        {
            // Act
            await _config.LoadAsync(configPath);
            
            // Assert
            Assert.Equal("Test App", _config.Get<string>("app.name"));
            Assert.Equal("1.0.0", _config.Get<string>("app.version"));
            Assert.Equal("test_connection", _config.Get<string>("database.connection_string"));
            
            WriteLine("Configuration loaded successfully");
        }
        finally
        {
            File.Delete(configPath);
        }
    }
    
    [Fact]
    public void Set_NewKey_ShouldAddKey()
    {
        // Act
        _config.Set("new.key", "new_value");
        
        // Assert
        Assert.True(_config.Has("new.key"));
        Assert.Equal("new_value", _config.Get<string>("new.key"));
        
        WriteLine("New key added successfully");
    }
    
    [Fact]
    public void Set_ExistingKey_ShouldUpdateValue()
    {
        // Arrange
        _config.Set("existing.key", "old_value");
        
        // Act
        _config.Set("existing.key", "new_value");
        
        // Assert
        Assert.Equal("new_value", _config.Get<string>("existing.key"));
        
        WriteLine("Existing key updated successfully");
    }
}
```

## 🔗 Integration Testing

### Integration Test Base

```csharp
public abstract class IntegrationTestBase : TestBase
{
    protected readonly TestServer _server;
    protected readonly HttpClient _client;
    protected readonly IServiceProvider _serviceProvider;
    
    protected IntegrationTestBase(ITestOutputHelper output) : base(output)
    {
        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_config);
                ConfigureTestServices(services);
            });
        
        _server = new TestServer(builder);
        _client = _server.CreateClient();
        _serviceProvider = _server.Host.Services;
        
        SetupTestData();
    }
    
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override services for testing
        services.AddScoped<IDbConnection>(provider => 
            new SqliteConnection("Data Source=:memory:"));
        
        services.AddScoped<IEventBus, MockEventBus>();
        services.AddScoped<IEmailService, MockEmailService>();
    }
    
    protected virtual void SetupTestData()
    {
        // Setup test data in database
        using var scope = _serviceProvider.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        
        connection.Open();
        
        var createUsersTable = @"
            CREATE TABLE users (
                id TEXT PRIMARY KEY,
                email TEXT NOT NULL UNIQUE,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT
            )";
        
        connection.Execute(createUsersTable);
    }
    
    protected virtual async Task<User> CreateTestUserAsync(string email = "test@example.com")
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        await userRepository.CreateAsync(user);
        return user;
    }
    
    public override void Dispose()
    {
        _client?.Dispose();
        _server?.Dispose();
        base.Dispose();
    }
}

public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<UserService>();
        services.AddScoped<IEventBus, MockEventBus>();
        services.AddScoped<IEmailService, MockEmailService>();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

public class MockEventBus : IEventBus
{
    public List<object> PublishedEvents { get; } = new();
    
    public Task PublishAsync<T>(T @event) where T : class
    {
        PublishedEvents.Add(@event);
        return Task.CompletedTask;
    }
    
    public Task SubscribeAsync<T>(IEventHandler<T> handler) where T : class
    {
        return Task.CompletedTask;
    }
    
    public Task UnsubscribeAsync<T>(IEventHandler<T> handler) where T : class
    {
        return Task.CompletedTask;
    }
}

public class MockEmailService : IEmailService
{
    public List<EmailRequest> SentEmails { get; } = new();
    
    public Task SendEmailAsync(EmailRequest request)
    {
        SentEmails.Add(request);
        return Task.CompletedTask;
    }
    
    public Task SendWelcomeEmailAsync(string email)
    {
        SentEmails.Add(new EmailRequest
        {
            To = email,
            Subject = "Welcome",
            Body = "Welcome to our service!"
        });
        return Task.CompletedTask;
    }
}
```

### API Integration Tests

```csharp
public class UserControllerIntegrationTests : IntegrationTestBase
{
    public UserControllerIntegrationTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public async Task CreateUser_ValidRequest_ShouldReturnCreatedUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/users", content);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var createdUser = JsonSerializer.Deserialize<UserDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(createdUser);
        Assert.Equal(request.Email, createdUser.Email);
        Assert.Equal(request.FirstName, createdUser.FirstName);
        Assert.Equal(request.LastName, createdUser.LastName);
        
        WriteLine($"Created user via API: {createdUser.Id}");
    }
    
    [Fact]
    public async Task CreateUser_DuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var existingUser = await CreateTestUserAsync("test@example.com");
        
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "Jane",
            LastName = "Doe"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/users", content);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("already exists", responseContent);
        
        WriteLine("Duplicate email correctly rejected");
    }
    
    [Fact]
    public async Task GetUser_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var testUser = await CreateTestUserAsync();
        
        // Act
        var response = await _client.GetAsync($"/api/users/{testUser.Id}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(user);
        Assert.Equal(testUser.Id, user.Id);
        Assert.Equal(testUser.Email, user.Email);
        
        WriteLine($"Retrieved user via API: {user.Email}");
    }
    
    [Fact]
    public async Task GetUser_NonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var response = await _client.GetAsync($"/api/users/{nonExistentId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        WriteLine("Non-existent user correctly returned 404");
    }
    
    [Fact]
    public async Task UpdateUser_ExistingUser_ShouldUpdateUser()
    {
        // Arrange
        var testUser = await CreateTestUserAsync();
        
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PutAsync($"/api/users/{testUser.Id}", content);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify user was updated
        var getUserResponse = await _client.GetAsync($"/api/users/{testUser.Id}");
        var userContent = await getUserResponse.Content.ReadAsStringAsync();
        var updatedUser = JsonSerializer.Deserialize<UserDto>(userContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.NotNull(updatedUser);
        Assert.Equal(request.FirstName, updatedUser.FirstName);
        Assert.Equal(request.LastName, updatedUser.LastName);
        
        WriteLine($"Updated user via API: {updatedUser.Email}");
    }
    
    [Fact]
    public async Task CreateUser_ShouldPublishEvent()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        await _client.PostAsync("/api/users", content);
        
        // Assert
        using var scope = _serviceProvider.CreateScope();
        var eventBus = scope.ServiceProvider.GetRequiredService<MockEventBus>();
        
        Assert.Single(eventBus.PublishedEvents);
        Assert.IsType<UserCreatedEvent>(eventBus.PublishedEvents[0]);
        
        var userCreatedEvent = (UserCreatedEvent)eventBus.PublishedEvents[0];
        Assert.Equal(request.Email, userCreatedEvent.Email);
        
        WriteLine("User created event published successfully");
    }
}
```

## 🌐 End-to-End Testing

### E2E Test Base

```csharp
public abstract class E2ETestBase : TestBase
{
    protected readonly IWebDriver _driver;
    protected readonly WebDriverWait _wait;
    protected readonly string _baseUrl;
    
    protected E2ETestBase(ITestOutputHelper output) : base(output)
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        
        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        
        _baseUrl = _config.Get<string>("app.base_url", "http://localhost:5000");
    }
    
    protected virtual async Task NavigateToAsync(string path)
    {
        var url = $"{_baseUrl}{path}";
        _driver.Navigate().GoToUrl(url);
        await Task.Delay(1000); // Wait for page load
        
        WriteLine($"Navigated to: {url}");
    }
    
    protected virtual async Task<IWebElement> WaitForElementAsync(By by)
    {
        var element = _wait.Until(driver => driver.FindElement(by));
        await Task.Delay(500); // Small delay for stability
        return element;
    }
    
    protected virtual async Task ClickAsync(By by)
    {
        var element = await WaitForElementAsync(by);
        element.Click();
        await Task.Delay(500);
        
        WriteLine($"Clicked element: {by}");
    }
    
    protected virtual async Task TypeAsync(By by, string text)
    {
        var element = await WaitForElementAsync(by);
        element.Clear();
        element.SendKeys(text);
        await Task.Delay(500);
        
        WriteLine($"Typed '{text}' into element: {by}");
    }
    
    protected virtual async Task<string> GetTextAsync(By by)
    {
        var element = await WaitForElementAsync(by);
        var text = element.Text;
        
        WriteLine($"Got text '{text}' from element: {by}");
        return text;
    }
    
    protected virtual async Task<bool> ElementExistsAsync(By by)
    {
        try
        {
            await WaitForElementAsync(by);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }
    
    public override void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
        base.Dispose();
    }
}

public class UserManagementE2ETests : E2ETestBase
{
    public UserManagementE2ETests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public async Task CreateUser_ShouldDisplaySuccessMessage()
    {
        // Arrange
        await NavigateToAsync("/users/create");
        
        var email = $"test_{Guid.NewGuid()}@example.com";
        var firstName = "John";
        var lastName = "Doe";
        
        // Act
        await TypeAsync(By.Id("email"), email);
        await TypeAsync(By.Id("firstName"), firstName);
        await TypeAsync(By.Id("lastName"), lastName);
        await ClickAsync(By.Id("createButton"));
        
        // Assert
        var successMessage = await WaitForElementAsync(By.ClassName("success-message"));
        Assert.Contains("User created successfully", successMessage.Text);
        
        WriteLine("User creation E2E test passed");
    }
    
    [Fact]
    public async Task CreateUser_DuplicateEmail_ShouldDisplayErrorMessage()
    {
        // Arrange
        await NavigateToAsync("/users/create");
        
        var email = "duplicate@example.com";
        var firstName = "John";
        var lastName = "Doe";
        
        // Create user first time
        await TypeAsync(By.Id("email"), email);
        await TypeAsync(By.Id("firstName"), firstName);
        await TypeAsync(By.Id("lastName"), lastName);
        await ClickAsync(By.Id("createButton"));
        
        // Wait for success
        await WaitForElementAsync(By.ClassName("success-message"));
        
        // Navigate back to create form
        await NavigateToAsync("/users/create");
        
        // Act - Try to create duplicate
        await TypeAsync(By.Id("email"), email);
        await TypeAsync(By.Id("firstName"), "Jane");
        await TypeAsync(By.Id("lastName"), "Doe");
        await ClickAsync(By.Id("createButton"));
        
        // Assert
        var errorMessage = await WaitForElementAsync(By.ClassName("error-message"));
        Assert.Contains("already exists", errorMessage.Text);
        
        WriteLine("Duplicate email E2E test passed");
    }
    
    [Fact]
    public async Task ViewUser_ShouldDisplayUserDetails()
    {
        // Arrange
        var userId = await CreateTestUserViaApiAsync();
        await NavigateToAsync($"/users/{userId}");
        
        // Act & Assert
        var emailElement = await WaitForElementAsync(By.Id("userEmail"));
        var firstNameElement = await WaitForElementAsync(By.Id("userFirstName"));
        var lastNameElement = await WaitForElementAsync(By.Id("userLastName"));
        
        Assert.Equal("test@example.com", emailElement.Text);
        Assert.Equal("Test", firstNameElement.Text);
        Assert.Equal("User", lastNameElement.Text);
        
        WriteLine("User details E2E test passed");
    }
    
    [Fact]
    public async Task UpdateUser_ShouldUpdateAndDisplayChanges()
    {
        // Arrange
        var userId = await CreateTestUserViaApiAsync();
        await NavigateToAsync($"/users/{userId}/edit");
        
        var newFirstName = "Updated";
        var newLastName = "Name";
        
        // Act
        await TypeAsync(By.Id("firstName"), newFirstName);
        await TypeAsync(By.Id("lastName"), newLastName);
        await ClickAsync(By.Id("updateButton"));
        
        // Assert
        var successMessage = await WaitForElementAsync(By.ClassName("success-message"));
        Assert.Contains("User updated successfully", successMessage.Text);
        
        // Verify changes are displayed
        await NavigateToAsync($"/users/{userId}");
        var firstNameElement = await WaitForElementAsync(By.Id("userFirstName"));
        var lastNameElement = await WaitForElementAsync(By.Id("userLastName"));
        
        Assert.Equal(newFirstName, firstNameElement.Text);
        Assert.Equal(newLastName, lastNameElement.Text);
        
        WriteLine("User update E2E test passed");
    }
    
    private async Task<Guid> CreateTestUserViaApiAsync()
    {
        using var client = new HttpClient();
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync($"{_baseUrl}/api/users", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return user!.Id;
    }
}
```

## 📝 Summary

This guide covered comprehensive testing strategies for C# TuskLang applications:

- **Unit Testing**: Test base classes, service tests, and configuration tests with mocking
- **Integration Testing**: API integration tests with test servers and service overrides
- **End-to-End Testing**: Browser automation tests for complete user workflows
- **Testing Best Practices**: Proper test organization, mocking strategies, and assertions

These testing frameworks ensure your C# TuskLang applications are thoroughly tested and maintain high code quality throughout development. 
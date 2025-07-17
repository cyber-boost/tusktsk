# Testing Strategies in C# with TuskLang

## Overview

This guide covers comprehensive testing strategies for C# applications using TuskLang, including unit testing, integration testing, and test configuration patterns.

## Table of Contents

1. [Unit Testing](#unit-testing)
2. [Integration Testing](#integration-testing)
3. [Test Configuration](#test-configuration)
4. [TuskLang Integration](#tusklang-integration)

## Unit Testing

### Basic Unit Tests

```csharp
[TestClass]
public class UserServiceTests
{
    private UserService _userService;
    private Mock<IUserRepository> _mockRepository;
    private Mock<ILogger<UserService>> _mockLogger;
    private TuskLang _testConfig;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _testConfig = new TuskLang("test-config.tsk");
        
        _userService = new UserService(_mockRepository.Object, _mockLogger.Object, _testConfig);
    }

    [TestMethod]
    public async Task GetUserById_ValidId_ReturnsUser()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new User { Id = userId, Name = "Test User", Email = "test@example.com" };
        
        _mockRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedUser.Id, result.Id);
        Assert.AreEqual(expectedUser.Name, result.Name);
        Assert.AreEqual(expectedUser.Email, result.Email);
        
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [TestMethod]
    public async Task GetUserById_InvalidId_ReturnsNull()
    {
        // Arrange
        var userId = 999;
        
        _mockRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.IsNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [TestMethod]
    public async Task CreateUser_ValidRequest_ReturnsCreatedUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "newuser@example.com"
        };

        var expectedUser = new User
        {
            Id = 1,
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.CreateUserAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedUser.Id, result.Id);
        Assert.AreEqual(request.Name, result.Name);
        Assert.AreEqual(request.Email, result.Email);
        
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateUser_DuplicateEmail_ThrowsException()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "Test User",
            Email = "existing@example.com"
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(new User { Email = request.Email });

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _userService.CreateUserAsync(request));
        
        _mockRepository.Verify(r => r.GetByEmailAsync(request.Email), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }
}
```

### TuskLang Configuration Testing

```csharp
[TestClass]
public class TuskLangConfigurationTests
{
    private TuskLang _tuskLang;

    [TestInitialize]
    public void Setup()
    {
        // Create test configuration
        var testConfig = @"
app.name = TestApp
app.environment = Test
database.connectionString = TestConnection
api.baseUrl = https://test-api.example.com
api.timeoutSeconds = 30
";
        
        File.WriteAllText("test-config.tsk", testConfig);
        _tuskLang = new TuskLang("test-config.tsk");
    }

    [TestMethod]
    public void GetValue_ValidKey_ReturnsValue()
    {
        // Act
        var appName = _tuskLang.GetValue<string>("app.name");
        var environment = _tuskLang.GetValue<string>("app.environment");
        var timeout = _tuskLang.GetValue<int>("api.timeoutSeconds");

        // Assert
        Assert.AreEqual("TestApp", appName);
        Assert.AreEqual("Test", environment);
        Assert.AreEqual(30, timeout);
    }

    [TestMethod]
    public void GetValue_InvalidKey_ReturnsDefault()
    {
        // Act
        var value = _tuskLang.GetValue<string>("invalid.key", "default");

        // Assert
        Assert.AreEqual("default", value);
    }

    [TestMethod]
    public void SetValue_NewKey_SetsValue()
    {
        // Act
        _tuskLang.SetValue("test.key", "test-value");
        var value = _tuskLang.GetValue<string>("test.key");

        // Assert
        Assert.AreEqual("test-value", value);
    }

    [TestMethod]
    public void HasValue_ExistingKey_ReturnsTrue()
    {
        // Act
        var hasValue = _tuskLang.HasValue("app.name");

        // Assert
        Assert.IsTrue(hasValue);
    }

    [TestMethod]
    public void HasValue_NonExistingKey_ReturnsFalse()
    {
        // Act
        var hasValue = _tuskLang.HasValue("invalid.key");

        // Assert
        Assert.IsFalse(hasValue);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists("test-config.tsk"))
        {
            File.Delete("test-config.tsk");
        }
    }
}
```

## Integration Testing

### API Integration Tests

```csharp
[TestClass]
public class UsersApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private TuskLang _testConfig;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["app:environment"] = "Test",
                        ["database:connectionString"] = "TestConnection",
                        ["api:baseUrl"] = "https://test-api.example.com"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Replace real services with test doubles
                    services.AddScoped<IUserRepository, TestUserRepository>();
                    services.AddScoped<ITuskLang>(provider => _testConfig);
                });
            });

        _client = _factory.CreateClient();
        _testConfig = new TuskLang("test-config.tsk");
    }

    [TestMethod]
    public async Task GetUsers_ReturnsUsersList()
    {
        // Arrange
        var expectedUsers = new List<UserDto>
        {
            new() { Id = 1, Name = "User 1", Email = "user1@example.com" },
            new() { Id = 2, Name = "User 2", Email = "user2@example.com" }
        };

        // Act
        var response = await _client.GetAsync("/api/users");
        var content = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserDto>>(content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(users);
        Assert.AreEqual(expectedUsers.Count, users.Count);
    }

    [TestMethod]
    public async Task GetUser_ValidId_ReturnsUser()
    {
        // Arrange
        var userId = 1;

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}");
        var content = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserDto>(content);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
    }

    [TestMethod]
    public async Task GetUser_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUser_ValidRequest_ReturnsCreatedUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "New User",
            Email = "newuser@example.com"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/users", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserDto>(responseContent);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(user);
        Assert.AreEqual(request.Name, user.Name);
        Assert.AreEqual(request.Email, user.Email);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

### Database Integration Tests

```csharp
[TestClass]
public class DatabaseIntegrationTests
{
    private DbContext _context;
    private IUserRepository _repository;
    private TuskLang _testConfig;

    [TestInitialize]
    public void Setup()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
        _testConfig = new TuskLang("test-config.tsk");

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var users = new List<User>
        {
            new() { Id = 1, Name = "Test User 1", Email = "user1@example.com", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Test User 2", Email = "user2@example.com", CreatedAt = DateTime.UtcNow }
        };

        _context.Set<User>().AddRange(users);
        _context.SaveChanges();
    }

    [TestMethod]
    public async Task GetById_ExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = 1;

        // Act
        var user = await _repository.GetByIdAsync(userId);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
        Assert.AreEqual("Test User 1", user.Name);
    }

    [TestMethod]
    public async Task GetById_NonExistingUser_ReturnsNull()
    {
        // Arrange
        var userId = 999;

        // Act
        var user = await _repository.GetByIdAsync(userId);

        // Assert
        Assert.IsNull(user);
    }

    [TestMethod]
    public async Task Create_ValidUser_ReturnsCreatedUser()
    {
        // Arrange
        var newUser = new User
        {
            Name = "New User",
            Email = "newuser@example.com",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var createdUser = await _repository.CreateAsync(newUser);

        // Assert
        Assert.IsNotNull(createdUser);
        Assert.IsTrue(createdUser.Id > 0);
        Assert.AreEqual(newUser.Name, createdUser.Name);
        Assert.AreEqual(newUser.Email, createdUser.Email);
    }

    [TestMethod]
    public async Task Update_ExistingUser_UpdatesUser()
    {
        // Arrange
        var userId = 1;
        var user = await _repository.GetByIdAsync(userId);
        user.Name = "Updated User";

        // Act
        var updatedUser = await _repository.UpdateAsync(user);

        // Assert
        Assert.IsNotNull(updatedUser);
        Assert.AreEqual("Updated User", updatedUser.Name);
    }

    [TestMethod]
    public async Task Delete_ExistingUser_RemovesUser()
    {
        // Arrange
        var userId = 1;

        // Act
        var success = await _repository.DeleteAsync(userId);
        var deletedUser = await _repository.GetByIdAsync(userId);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNull(deletedUser);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context?.Dispose();
    }
}
```

## Test Configuration

### Test Configuration Management

```csharp
public class TestConfigurationManager
{
    private readonly Dictionary<string, string> _testSettings;
    private readonly string _configFilePath;

    public TestConfigurationManager()
    {
        _testSettings = new Dictionary<string, string>();
        _configFilePath = "test-config.tsk";
    }

    public TestConfigurationManager WithSetting(string key, string value)
    {
        _testSettings[key] = value;
        return this;
    }

    public TestConfigurationManager WithDatabaseConnection(string connectionString)
    {
        _testSettings["database.connectionString"] = connectionString;
        return this;
    }

    public TestConfigurationManager WithApiSettings(string baseUrl, string apiKey)
    {
        _testSettings["api.baseUrl"] = baseUrl;
        _testSettings["api.apiKey"] = apiKey;
        return this;
    }

    public TestConfigurationManager WithEnvironment(string environment)
    {
        _testSettings["app.environment"] = environment;
        return this;
    }

    public TuskLang Build()
    {
        var configContent = string.Join("\n", 
            _testSettings.Select(kvp => $"{kvp.Key} = {kvp.Value}"));
        
        File.WriteAllText(_configFilePath, configContent);
        
        return new TuskLang(_configFilePath);
    }

    public void Cleanup()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }
    }
}

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void TestConfiguration_WithCustomSettings_LoadsCorrectly()
    {
        // Arrange
        var configManager = new TestConfigurationManager()
            .WithEnvironment("Test")
            .WithDatabaseConnection("TestConnection")
            .WithApiSettings("https://test-api.example.com", "test-key");

        // Act
        var config = configManager.Build();

        // Assert
        Assert.AreEqual("Test", config.GetValue<string>("app.environment"));
        Assert.AreEqual("TestConnection", config.GetValue<string>("database.connectionString"));
        Assert.AreEqual("https://test-api.example.com", config.GetValue<string>("api.baseUrl"));
        Assert.AreEqual("test-key", config.GetValue<string>("api.apiKey"));

        // Cleanup
        configManager.Cleanup();
    }
}
```

## TuskLang Integration

### TuskLang Test Utilities

```csharp
public class TuskLangTestUtils
{
    public static TuskLang CreateTestConfig(Dictionary<string, object> settings = null)
    {
        var configContent = new StringBuilder();
        
        if (settings != null)
        {
            foreach (var (key, value) in settings)
            {
                configContent.AppendLine($"{key} = {value}");
            }
        }

        var configPath = $"test-config-{Guid.NewGuid()}.tsk";
        File.WriteAllText(configPath, configContent.ToString());
        
        return new TuskLang(configPath);
    }

    public static void CleanupTestConfig(TuskLang config)
    {
        if (config != null)
        {
            var configPath = config.GetType().GetField("_configPath", 
                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(config) as string;
            
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    public static TuskLang CreateMockConfig()
    {
        var settings = new Dictionary<string, object>
        {
            ["app.name"] = "TestApp",
            ["app.environment"] = "Test",
            ["database.connectionString"] = "TestConnection",
            ["api.baseUrl"] = "https://test-api.example.com",
            ["api.timeoutSeconds"] = 30,
            ["api.maxResults"] = 100,
            ["validation.enableStrict"] = true,
            ["retry.maxAttempts"] = 3
        };

        return CreateTestConfig(settings);
    }
}

[TestClass]
public class TuskLangIntegrationTests
{
    private TuskLang _testConfig;

    [TestInitialize]
    public void Setup()
    {
        _testConfig = TuskLangTestUtils.CreateMockConfig();
    }

    [TestMethod]
    public void TuskLang_Integration_WorksWithServices()
    {
        // Arrange
        var mockRepository = new Mock<IUserRepository>();
        var userService = new UserService(mockRepository.Object, null, _testConfig);

        // Act & Assert
        // The service should be able to use the TuskLang configuration
        Assert.IsNotNull(userService);
    }

    [TestMethod]
    public void TuskLang_Configuration_LoadsCorrectly()
    {
        // Act & Assert
        Assert.AreEqual("TestApp", _testConfig.GetValue<string>("app.name"));
        Assert.AreEqual("Test", _testConfig.GetValue<string>("app.environment"));
        Assert.AreEqual(30, _testConfig.GetValue<int>("api.timeoutSeconds"));
        Assert.AreEqual(100, _testConfig.GetValue<int>("api.maxResults"));
        Assert.IsTrue(_testConfig.GetValue<bool>("validation.enableStrict"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        TuskLangTestUtils.CleanupTestConfig(_testConfig);
    }
}
```

## Summary

This comprehensive testing strategies guide covers:

- **Unit Testing**: Isolated testing of individual components with mocking
- **Integration Testing**: End-to-end testing of API endpoints and database operations
- **Test Configuration**: Flexible test configuration management
- **TuskLang Integration**: Testing utilities and integration patterns

The patterns ensure comprehensive test coverage while maintaining test maintainability and reliability in C# applications using TuskLang. 
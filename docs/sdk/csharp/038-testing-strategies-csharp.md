# Testing Strategies in C# TuskLang

## Overview

Testing is crucial for ensuring the reliability and maintainability of TuskLang applications. This guide covers unit testing, integration testing, end-to-end testing, and configuration validation strategies for C# applications.

## 🧪 Unit Testing

### Service Layer Testing

```csharp
[TestClass]
public class UserServiceTests
{
    private Mock<IDbConnection> _mockConnection;
    private Mock<ILogger<UserService>> _mockLogger;
    private Mock<TSKConfig> _mockConfig;
    private Mock<IPasswordHasher> _mockPasswordHasher;
    private UserService _userService;
    
    [TestInitialize]
    public void Setup()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockConfig = new Mock<TSKConfig>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        
        _userService = new UserService(
            _mockConnection.Object,
            _mockLogger.Object,
            _mockConfig.Object,
            _mockPasswordHasher.Object);
    }
    
    [TestMethod]
    public async Task GetUsersAsync_ShouldReturnPaginatedUsers()
    {
        // Arrange
        var expectedUsers = new List<UserDto>
        {
            new UserDto { Id = 1, Email = "user1@example.com", FirstName = "John", LastName = "Doe" },
            new UserDto { Id = 2, Email = "user2@example.com", FirstName = "Jane", LastName = "Smith" }
        };
        
        var expectedTotalCount = 2;
        
        _mockConnection
            .Setup(c => c.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync(expectedTotalCount);
        
        _mockConnection
            .Setup(c => c.QueryAsync<UserDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, true, null, CommandType.Text))
            .ReturnsAsync(expectedUsers);
        
        // Act
        var result = await _userService.GetUsersAsync(1, 20, null);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedTotalCount, result.TotalCount);
        Assert.AreEqual(expectedUsers.Count, result.Items.Count);
        Assert.AreEqual(expectedUsers[0].Email, result.Items[0].Email);
        Assert.AreEqual(expectedUsers[1].Email, result.Items[1].Email);
    }
    
    [TestMethod]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new UserDto
        {
            Id = userId,
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        
        _mockConnection
            .Setup(c => c.QueryFirstOrDefaultAsync<UserDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await _userService.GetUserByIdAsync(userId);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedUser.Id, result.Id);
        Assert.AreEqual(expectedUser.Email, result.Email);
        Assert.AreEqual(expectedUser.FirstName, result.FirstName);
        Assert.AreEqual(expectedUser.LastName, result.LastName);
    }
    
    [TestMethod]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var userId = 999;
        
        _mockConnection
            .Setup(c => c.QueryFirstOrDefaultAsync<UserDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync((UserDto?)null);
        
        // Act
        var result = await _userService.GetUserByIdAsync(userId);
        
        // Assert
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public async Task CreateUserAsync_WithValidRequest_ShouldCreateUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "password123",
            FirstName = "New",
            LastName = "User"
        };
        
        var hashedPassword = "hashed_password_123";
        var expectedUser = new UserDto
        {
            Id = 1,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        _mockPasswordHasher
            .Setup(h => h.HashPassword(request.Password))
            .Returns(hashedPassword);
        
        _mockConnection
            .Setup(c => c.QueryFirstOrDefaultAsync<UserDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync((UserDto?)null); // No existing user
        
        _mockConnection
            .Setup(c => c.QueryFirstAsync<UserDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await _userService.CreateUserAsync(request);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedUser.Email, result.Email);
        Assert.AreEqual(expectedUser.FirstName, result.FirstName);
        Assert.AreEqual(expectedUser.LastName, result.LastName);
        
        _mockPasswordHasher.Verify(h => h.HashPassword(request.Password), Times.Once);
    }
    
    [TestMethod]
    public async Task CreateUserAsync_WithExistingEmail_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "existing@example.com",
            Password = "password123",
            FirstName = "New",
            LastName = "User"
        };
        
        var existingUser = new UserDto { Id = 1, Email = request.Email };
        
        _mockConnection
            .Setup(c => c.QueryFirstOrDefaultAsync<UserDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync(existingUser);
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ValidationException>(
            () => _userService.CreateUserAsync(request));
    }
    
    [TestMethod]
    public async Task UpdateUserAsync_WithValidRequest_ShouldUpdateUser()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };
        
        var existingUser = new UserDto
        {
            Id = userId,
            Email = "user@example.com",
            FirstName = "Original",
            LastName = "Name"
        };
        
        var updatedUser = new UserDto
        {
            Id = userId,
            Email = "user@example.com",
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        _mockConnection
            .Setup(c => c.QueryFirstOrDefaultAsync<UserDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync(existingUser);
        
        _mockConnection
            .Setup(c => c.QueryFirstOrDefaultAsync<UserDto>(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync(updatedUser);
        
        // Act
        var result = await _userService.UpdateUserAsync(userId, request);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(request.FirstName, result.FirstName);
        Assert.AreEqual(request.LastName, result.LastName);
    }
    
    [TestMethod]
    public async Task DeleteUserAsync_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1;
        
        _mockConnection
            .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync(1);
        
        // Act
        var result = await _userService.DeleteUserAsync(userId);
        
        // Assert
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public async Task DeleteUserAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999;
        
        _mockConnection
            .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), null, null, CommandType.Text))
            .ReturnsAsync(0);
        
        // Act
        var result = await _userService.DeleteUserAsync(userId);
        
        // Assert
        Assert.IsFalse(result);
    }
}
```

### Controller Testing

```csharp
[TestClass]
public class UsersControllerTests
{
    private Mock<IUserService> _mockUserService;
    private Mock<ILogger<UsersController>> _mockLogger;
    private Mock<TSKConfig> _mockConfig;
    private UsersController _controller;
    
    [TestInitialize]
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _mockConfig = new Mock<TSKConfig>();
        
        _controller = new UsersController(
            _mockUserService.Object,
            _mockLogger.Object,
            _mockConfig.Object);
    }
    
    [TestMethod]
    public async Task GetUsers_ShouldReturnOkResult()
    {
        // Arrange
        var users = new PaginatedResult<UserDto>
        {
            Items = new List<UserDto>
            {
                new UserDto { Id = 1, Email = "user1@example.com" },
                new UserDto { Id = 2, Email = "user2@example.com" }
            },
            TotalCount = 2
        };
        
        _mockConfig.Setup(c => c.Get<int>("api.max_page_size", 100)).Returns(100);
        _mockUserService.Setup(s => s.GetUsersAsync(1, 20, null)).ReturnsAsync(users);
        
        // Act
        var result = await _controller.GetUsers(1, 20, null);
        
        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        
        var response = okResult.Value as PaginatedResponse<UserDto>;
        Assert.IsNotNull(response);
        Assert.AreEqual(2, response.Data.Count);
        Assert.AreEqual(2, response.TotalCount);
    }
    
    [TestMethod]
    public async Task GetUser_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var userId = 1;
        var user = new UserDto
        {
            Id = userId,
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
        
        // Act
        var result = await _controller.GetUser(userId);
        
        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        
        var response = okResult.Value as UserDto;
        Assert.IsNotNull(response);
        Assert.AreEqual(userId, response.Id);
        Assert.AreEqual(user.Email, response.Email);
    }
    
    [TestMethod]
    public async Task GetUser_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var userId = 999;
        
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((UserDto?)null);
        
        // Act
        var result = await _controller.GetUser(userId);
        
        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.IsNotNull(notFoundResult);
        
        var response = notFoundResult.Value as ErrorResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual("User not found", response.Message);
    }
    
    [TestMethod]
    public async Task CreateUser_WithValidRequest_ShouldReturnCreatedResult()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "password123",
            FirstName = "New",
            LastName = "User"
        };
        
        var createdUser = new UserDto
        {
            Id = 1,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        _mockUserService.Setup(s => s.CreateUserAsync(request)).ReturnsAsync(createdUser);
        
        // Act
        var result = await _controller.CreateUser(request);
        
        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(createdResult);
        Assert.AreEqual(nameof(UsersController.GetUser), createdResult.ActionName);
        Assert.AreEqual(1, createdResult.RouteValues["id"]);
        
        var response = createdResult.Value as UserDto;
        Assert.IsNotNull(response);
        Assert.AreEqual(createdUser.Id, response.Id);
        Assert.AreEqual(createdUser.Email, response.Email);
    }
    
    [TestMethod]
    public async Task CreateUser_WithValidationException_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "existing@example.com",
            Password = "password123",
            FirstName = "New",
            LastName = "User"
        };
        
        _mockUserService
            .Setup(s => s.CreateUserAsync(request))
            .ThrowsAsync(new ValidationException("User with this email already exists"));
        
        // Act
        var result = await _controller.CreateUser(request);
        
        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        
        var response = badRequestResult.Value as ErrorResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual("User with this email already exists", response.Message);
    }
    
    [TestMethod]
    public async Task UpdateUser_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };
        
        var updatedUser = new UserDto
        {
            Id = userId,
            Email = "user@example.com",
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        _mockUserService.Setup(s => s.UpdateUserAsync(userId, request)).ReturnsAsync(updatedUser);
        
        // Act
        var result = await _controller.UpdateUser(userId, request);
        
        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        
        var response = okResult.Value as UserDto;
        Assert.IsNotNull(response);
        Assert.AreEqual(request.FirstName, response.FirstName);
        Assert.AreEqual(request.LastName, response.LastName);
    }
    
    [TestMethod]
    public async Task DeleteUser_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var userId = 1;
        
        _mockUserService.Setup(s => s.DeleteUserAsync(userId)).ReturnsAsync(true);
        
        // Act
        var result = await _controller.DeleteUser(userId);
        
        // Assert
        var noContentResult = result as NoContentResult;
        Assert.IsNotNull(noContentResult);
    }
    
    [TestMethod]
    public async Task DeleteUser_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var userId = 999;
        
        _mockUserService.Setup(s => s.DeleteUserAsync(userId)).ReturnsAsync(false);
        
        // Act
        var result = await _controller.DeleteUser(userId);
        
        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.IsNotNull(notFoundResult);
        
        var response = notFoundResult.Value as ErrorResponse;
        Assert.IsNotNull(response);
        Assert.AreEqual("User not found", response.Message);
    }
}
```

## 🔗 Integration Testing

### Database Integration Tests

```csharp
[TestClass]
public class UserServiceIntegrationTests
{
    private IDbConnection _connection;
    private UserService _userService;
    private TSKConfig _config;
    
    [TestInitialize]
    public void Setup()
    {
        // Use test database
        var connectionString = "Server=localhost;Database=myapp_test;User Id=test;Password=test;";
        _connection = new NpgsqlConnection(connectionString);
        
        _config = new TSKConfig();
        _config.Set("database.connection_string", connectionString);
        _config.Set("database.timeout", 30);
        
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<UserService>();
        
        var passwordHasher = new BCryptPasswordHasher();
        
        _userService = new UserService(_connection, logger, _config, passwordHasher);
        
        // Setup test database
        SetupTestDatabase();
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        CleanupTestDatabase();
        _connection?.Dispose();
    }
    
    [TestMethod]
    public async Task GetUsersAsync_ShouldReturnUsersFromDatabase()
    {
        // Arrange
        await InsertTestUsers();
        
        // Act
        var result = await _userService.GetUsersAsync(1, 10, null);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Items.Count > 0);
        Assert.IsTrue(result.TotalCount > 0);
    }
    
    [TestMethod]
    public async Task CreateUserAsync_ShouldCreateUserInDatabase()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "integration@example.com",
            Password = "password123",
            FirstName = "Integration",
            LastName = "Test"
        };
        
        // Act
        var result = await _userService.CreateUserAsync(request);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(request.Email, result.Email);
        Assert.AreEqual(request.FirstName, result.FirstName);
        Assert.AreEqual(request.LastName, result.LastName);
        
        // Verify user exists in database
        var userInDb = await GetUserFromDatabase(result.Id);
        Assert.IsNotNull(userInDb);
        Assert.AreEqual(request.Email, userInDb.Email);
    }
    
    [TestMethod]
    public async Task UpdateUserAsync_ShouldUpdateUserInDatabase()
    {
        // Arrange
        var user = await InsertTestUser();
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };
        
        // Act
        var result = await _userService.UpdateUserAsync(user.Id, request);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(request.FirstName, result.FirstName);
        Assert.AreEqual(request.LastName, result.LastName);
        
        // Verify user updated in database
        var userInDb = await GetUserFromDatabase(user.Id);
        Assert.IsNotNull(userInDb);
        Assert.AreEqual(request.FirstName, userInDb.FirstName);
        Assert.AreEqual(request.LastName, userInDb.LastName);
    }
    
    [TestMethod]
    public async Task DeleteUserAsync_ShouldDeleteUserFromDatabase()
    {
        // Arrange
        var user = await InsertTestUser();
        
        // Act
        var result = await _userService.DeleteUserAsync(user.Id);
        
        // Assert
        Assert.IsTrue(result);
        
        // Verify user deleted from database
        var userInDb = await GetUserFromDatabase(user.Id);
        Assert.IsNull(userInDb);
    }
    
    private async Task SetupTestDatabase()
    {
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                email VARCHAR(255) UNIQUE NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                first_name VARCHAR(255) NOT NULL,
                last_name VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP NOT NULL DEFAULT NOW()
            )";
        
        await _connection.ExecuteAsync(createTableSql);
    }
    
    private async Task CleanupTestDatabase()
    {
        await _connection.ExecuteAsync("DELETE FROM users");
    }
    
    private async Task InsertTestUsers()
    {
        var users = new[]
        {
            new { Email = "test1@example.com", PasswordHash = "hash1", FirstName = "Test1", LastName = "User" },
            new { Email = "test2@example.com", PasswordHash = "hash2", FirstName = "Test2", LastName = "User" },
            new { Email = "test3@example.com", PasswordHash = "hash3", FirstName = "Test3", LastName = "User" }
        };
        
        foreach (var user in users)
        {
            await _connection.ExecuteAsync(@"
                INSERT INTO users (email, password_hash, first_name, last_name)
                VALUES (@Email, @PasswordHash, @FirstName, @LastName)",
                user);
        }
    }
    
    private async Task<UserDto?> InsertTestUser()
    {
        var user = new
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            PasswordHash = "test_hash",
            FirstName = "Test",
            LastName = "User"
        };
        
        var id = await _connection.ExecuteScalarAsync<int>(@"
            INSERT INTO users (email, password_hash, first_name, last_name)
            VALUES (@Email, @PasswordHash, @FirstName, @LastName)
            RETURNING id",
            user);
        
        return new UserDto
        {
            Id = id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
    
    private async Task<UserDto?> GetUserFromDatabase(int id)
    {
        return await _connection.QueryFirstOrDefaultAsync<UserDto>(
            "SELECT id, email, first_name, last_name, created_at, updated_at FROM users WHERE id = @Id",
            new { Id = id });
    }
}
```

## 🌐 End-to-End Testing

### API End-to-End Tests

```csharp
[TestClass]
public class UsersApiEndToEndTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private TSKConfig _config;
    
    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Use test configuration
                    config.AddJsonFile("appsettings.test.json", optional: false);
                });
                
                builder.ConfigureServices(services =>
                {
                    // Replace services with test implementations
                    services.AddScoped<IDbConnection>(provider =>
                    {
                        var connectionString = "Server=localhost;Database=myapp_test;User Id=test;Password=test;";
                        return new NpgsqlConnection(connectionString);
                    });
                });
            });
        
        _client = _factory.CreateClient();
        
        _config = new TSKConfig();
        _config.LoadFile("config/test.tsk");
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
    
    [TestMethod]
    public async Task GetUsers_ShouldReturnUsers()
    {
        // Arrange
        await SetupTestData();
        
        // Act
        var response = await _client.GetAsync("/api/v1/users");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResponse<UserDto>>(content);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Data.Count > 0);
        Assert.IsTrue(result.TotalCount > 0);
    }
    
    [TestMethod]
    public async Task GetUser_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var user = await CreateTestUser();
        
        // Act
        var response = await _client.GetAsync($"/api/v1/users/{user.Id}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserDto>(content);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(user.Id, result.Id);
        Assert.AreEqual(user.Email, result.Email);
    }
    
    [TestMethod]
    public async Task GetUser_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users/999");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ErrorResponse>(content);
        
        Assert.IsNotNull(result);
        Assert.AreEqual("User not found", result.Message);
    }
    
    [TestMethod]
    public async Task CreateUser_WithValidRequest_ShouldCreateUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "password123",
            FirstName = "Test",
            LastName = "User"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/v1/users", content);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserDto>(responseContent);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(request.Email, result.Email);
        Assert.AreEqual(request.FirstName, result.FirstName);
        Assert.AreEqual(request.LastName, result.LastName);
        
        // Verify location header
        Assert.IsNotNull(response.Headers.Location);
        Assert.IsTrue(response.Headers.Location.ToString().Contains($"/api/v1/users/{result.Id}"));
    }
    
    [TestMethod]
    public async Task CreateUser_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "invalid-email",
            Password = "123", // Too short
            FirstName = "",
            LastName = ""
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/v1/users", content);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ErrorResponse>(responseContent);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Errors.Count > 0);
    }
    
    [TestMethod]
    public async Task UpdateUser_WithValidRequest_ShouldUpdateUser()
    {
        // Arrange
        var user = await CreateTestUser();
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PutAsync($"/api/v1/users/{user.Id}", content);
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserDto>(responseContent);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(request.FirstName, result.FirstName);
        Assert.AreEqual(request.LastName, result.LastName);
    }
    
    [TestMethod]
    public async Task DeleteUser_WithValidId_ShouldDeleteUser()
    {
        // Arrange
        var user = await CreateTestUser();
        
        // Act
        var response = await _client.DeleteAsync($"/api/v1/users/{user.Id}");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify user is deleted
        var getResponse = await _client.GetAsync($"/api/v1/users/{user.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
    private async Task SetupTestData()
    {
        // Setup test data in database
        // Implementation depends on your test data setup strategy
    }
    
    private async Task<UserDto> CreateTestUser()
    {
        var request = new CreateUserRequest
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "password123",
            FirstName = "Test",
            LastName = "User"
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/v1/users", content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserDto>(responseContent)!;
    }
}
```

## ✅ Configuration Validation Testing

### Configuration Tests

```csharp
[TestClass]
public class ConfigurationValidationTests
{
    private ConfigurationValidator _validator;
    private TSKConfig _config;
    
    [TestInitialize]
    public void Setup()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<ConfigurationValidator>();
        
        _validator = new ConfigurationValidator(logger);
        _config = new TSKConfig();
    }
    
    [TestMethod]
    public async Task ValidateConfiguration_WithValidConfig_ShouldPass()
    {
        // Arrange
        _config.Set("database.connection_string", "Server=localhost;Database=test;");
        _config.Set("database.timeout", 30);
        _config.Set("api.base_url", "https://api.example.com");
        _config.Set("api.timeout", 30);
        _config.Set("security.jwt_secret", "very_long_secret_key_for_jwt_signing_32_chars");
        _config.Set("security.encryption_key", "very_long_encryption_key_32_chars");
        _config.Set("logging.level", "Information");
        
        // Act
        var result = await _validator.ValidateConfigurationAsync(_config);
        
        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
    
    [TestMethod]
    public async Task ValidateConfiguration_WithMissingSections_ShouldFail()
    {
        // Arrange
        _config.Set("database.connection_string", "Server=localhost;Database=test;");
        // Missing api and security sections
        
        // Act
        var result = await _validator.ValidateConfigurationAsync(_config);
        
        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Missing required section: api")));
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Missing required section: security")));
    }
    
    [TestMethod]
    public async Task ValidateConfiguration_WithInvalidDatabaseConnection_ShouldFail()
    {
        // Arrange
        _config.Set("database.connection_string", "invalid_connection_string");
        _config.Set("database.timeout", 30);
        _config.Set("api.base_url", "https://api.example.com");
        _config.Set("api.timeout", 30);
        _config.Set("security.jwt_secret", "very_long_secret_key_for_jwt_signing_32_chars");
        _config.Set("security.encryption_key", "very_long_encryption_key_32_chars");
        _config.Set("logging.level", "Information");
        
        // Act
        var result = await _validator.ValidateConfigurationAsync(_config);
        
        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Database connection failed")));
    }
    
    [TestMethod]
    public async Task ValidateConfiguration_WithInvalidApiUrl_ShouldFail()
    {
        // Arrange
        _config.Set("database.connection_string", "Server=localhost;Database=test;");
        _config.Set("database.timeout", 30);
        _config.Set("api.base_url", "invalid_url");
        _config.Set("api.timeout", 30);
        _config.Set("security.jwt_secret", "very_long_secret_key_for_jwt_signing_32_chars");
        _config.Set("security.encryption_key", "very_long_encryption_key_32_chars");
        _config.Set("logging.level", "Information");
        
        // Act
        var result = await _validator.ValidateConfigurationAsync(_config);
        
        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("API base URL must be a valid absolute URL")));
    }
    
    [TestMethod]
    public async Task ValidateConfiguration_WithShortJwtSecret_ShouldFail()
    {
        // Arrange
        _config.Set("database.connection_string", "Server=localhost;Database=test;");
        _config.Set("database.timeout", 30);
        _config.Set("api.base_url", "https://api.example.com");
        _config.Set("api.timeout", 30);
        _config.Set("security.jwt_secret", "short");
        _config.Set("security.encryption_key", "very_long_encryption_key_32_chars");
        _config.Set("logging.level", "Information");
        
        // Act
        var result = await _validator.ValidateConfigurationAsync(_config);
        
        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("JWT secret must be at least 32 characters long")));
    }
    
    [TestMethod]
    public async Task ValidateConfiguration_WithInvalidTimeouts_ShouldFail()
    {
        // Arrange
        _config.Set("database.connection_string", "Server=localhost;Database=test;");
        _config.Set("database.timeout", 0); // Invalid
        _config.Set("api.base_url", "https://api.example.com");
        _config.Set("api.timeout", 500); // Invalid
        _config.Set("security.jwt_secret", "very_long_secret_key_for_jwt_signing_32_chars");
        _config.Set("security.encryption_key", "very_long_encryption_key_32_chars");
        _config.Set("logging.level", "Information");
        
        // Act
        var result = await _validator.ValidateConfigurationAsync(_config);
        
        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Database timeout must be between 1 and 300 seconds")));
        Assert.IsTrue(result.Errors.Any(e => e.Contains("API timeout must be between 1 and 300 seconds")));
    }
}
```

## 📝 Summary

This guide covered comprehensive testing strategies for C# TuskLang applications:

- **Unit Testing**: Service layer and controller testing with mocks and assertions
- **Integration Testing**: Database integration tests with real database connections
- **End-to-End Testing**: Full API testing with WebApplicationFactory
- **Configuration Validation Testing**: Testing configuration validation logic

These testing strategies ensure your C# TuskLang applications are reliable, maintainable, and thoroughly tested across all layers. 
# Testing Strategies in C# TuskLang

## Overview

Comprehensive testing strategies are essential for building reliable and maintainable applications. This guide covers test-driven development (TDD), behavior-driven development (BDD), testing patterns, and testing best practices for C# TuskLang applications.

## 🧪 Test-Driven Development (TDD)

### TDD Workflow Implementation

```csharp
public class TddWorkflow
{
    private readonly ILogger<TddWorkflow> _logger;
    private readonly TSKConfig _config;
    
    public TddWorkflow(ILogger<TddWorkflow> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
    }
    
    public async Task<TddResult> ExecuteTddCycleAsync<T>(TddScenario<T> scenario)
    {
        _logger.LogInformation("Starting TDD cycle for {ScenarioName}", scenario.Name);
        
        var result = new TddResult
        {
            ScenarioName = scenario.Name,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            // Step 1: Red - Write failing test
            _logger.LogInformation("Step 1: Writing failing test");
            var testResult = await WriteFailingTestAsync(scenario);
            result.TestPhase = testResult;
            
            if (!testResult.Success)
            {
                result.Status = TddStatus.Failed;
                result.ErrorMessage = "Failed to write test";
                return result;
            }
            
            // Step 2: Green - Write minimal implementation
            _logger.LogInformation("Step 2: Writing minimal implementation");
            var implementationResult = await WriteMinimalImplementationAsync(scenario);
            result.ImplementationPhase = implementationResult;
            
            if (!implementationResult.Success)
            {
                result.Status = TddStatus.Failed;
                result.ErrorMessage = "Failed to write implementation";
                return result;
            }
            
            // Step 3: Refactor - Improve code
            _logger.LogInformation("Step 3: Refactoring code");
            var refactorResult = await RefactorCodeAsync(scenario);
            result.RefactorPhase = refactorResult;
            
            if (!refactorResult.Success)
            {
                result.Status = TddStatus.Failed;
                result.ErrorMessage = "Failed to refactor code";
                return result;
            }
            
            result.Status = TddStatus.Success;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            
            _logger.LogInformation("TDD cycle completed successfully in {Duration}ms", result.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TDD cycle failed for {ScenarioName}", scenario.Name);
            result.Status = TddStatus.Failed;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private async Task<TddPhaseResult> WriteFailingTestAsync<T>(TddScenario<T> scenario)
    {
        try
        {
            var testCode = GenerateTestCode(scenario);
            var testPath = GetTestFilePath(scenario);
            
            await File.WriteAllTextAsync(testPath, testCode);
            
            // Run test to verify it fails
            var testResult = await RunTestAsync(testPath);
            
            if (testResult.Success)
            {
                return TddPhaseResult.Failure("Test should have failed but passed");
            }
            
            return TddPhaseResult.Success("Failing test written successfully");
        }
        catch (Exception ex)
        {
            return TddPhaseResult.Failure(ex.Message);
        }
    }
    
    private async Task<TddPhaseResult> WriteMinimalImplementationAsync<T>(TddScenario<T> scenario)
    {
        try
        {
            var implementationCode = GenerateMinimalImplementation(scenario);
            var implementationPath = GetImplementationFilePath(scenario);
            
            await File.WriteAllTextAsync(implementationPath, implementationCode);
            
            // Run test to verify it passes
            var testPath = GetTestFilePath(scenario);
            var testResult = await RunTestAsync(testPath);
            
            if (!testResult.Success)
            {
                return TddPhaseResult.Failure("Test should have passed but failed");
            }
            
            return TddPhaseResult.Success("Minimal implementation written successfully");
        }
        catch (Exception ex)
        {
            return TddPhaseResult.Failure(ex.Message);
        }
    }
    
    private async Task<TddPhaseResult> RefactorCodeAsync<T>(TddScenario<T> scenario)
    {
        try
        {
            var refactoredCode = await RefactorImplementationAsync(scenario);
            var implementationPath = GetImplementationFilePath(scenario);
            
            await File.WriteAllTextAsync(implementationPath, refactoredCode);
            
            // Run test to verify it still passes
            var testPath = GetTestFilePath(scenario);
            var testResult = await RunTestAsync(testPath);
            
            if (!testResult.Success)
            {
                return TddPhaseResult.Failure("Refactoring broke the test");
            }
            
            return TddPhaseResult.Success("Code refactored successfully");
        }
        catch (Exception ex)
        {
            return TddPhaseResult.Failure(ex.Message);
        }
    }
    
    private string GenerateTestCode<T>(TddScenario<T> scenario)
    {
        var testCode = new StringBuilder();
        
        testCode.AppendLine("using Xunit;");
        testCode.AppendLine("using System;");
        testCode.AppendLine("using System.Threading.Tasks;");
        testCode.AppendLine();
        testCode.AppendLine("namespace TuskLang.Tests");
        testCode.AppendLine("{");
        testCode.AppendLine($"    public class {scenario.Name}Tests");
        testCode.AppendLine("    {");
        
        foreach (var testCase in scenario.TestCases)
        {
            testCode.AppendLine($"        [Fact]");
            testCode.AppendLine($"        public async Task {testCase.Name}_Should{testCase.ExpectedBehavior}");
            testCode.AppendLine("        {");
            testCode.AppendLine($"            // Arrange");
            testCode.AppendLine($"            {testCase.Arrange}");
            testCode.AppendLine();
            testCode.AppendLine($"            // Act");
            testCode.AppendLine($"            {testCase.Act}");
            testCode.AppendLine();
            testCode.AppendLine($"            // Assert");
            testCode.AppendLine($"            {testCase.Assert}");
            testCode.AppendLine("        }");
            testCode.AppendLine();
        }
        
        testCode.AppendLine("    }");
        testCode.AppendLine("}");
        
        return testCode.ToString();
    }
    
    private string GenerateMinimalImplementation<T>(TddScenario<T> scenario)
    {
        var implementationCode = new StringBuilder();
        
        implementationCode.AppendLine("using System;");
        implementationCode.AppendLine("using System.Threading.Tasks;");
        implementationCode.AppendLine();
        implementationCode.AppendLine("namespace TuskLang.Core");
        implementationCode.AppendLine("{");
        implementationCode.AppendLine($"    public class {scenario.ClassName}");
        implementationCode.AppendLine("    {");
        
        foreach (var method in scenario.Methods)
        {
            implementationCode.AppendLine($"        public {method.ReturnType} {method.Name}({method.Parameters})");
            implementationCode.AppendLine("        {");
            implementationCode.AppendLine($"            {method.MinimalImplementation}");
            implementationCode.AppendLine("        }");
            implementationCode.AppendLine();
        }
        
        implementationCode.AppendLine("    }");
        implementationCode.AppendLine("}");
        
        return implementationCode.ToString();
    }
    
    private async Task<string> RefactorImplementationAsync<T>(TddScenario<T> scenario)
    {
        var implementationPath = GetImplementationFilePath(scenario);
        var currentCode = await File.ReadAllTextAsync(implementationPath);
        
        // Apply refactoring rules
        var refactoredCode = currentCode
            .Replace("// TODO: Implement", "")
            .Replace("return null;", "return new();")
            .Replace("throw new NotImplementedException();", "return default;");
        
        return refactoredCode;
    }
    
    private async Task<TestRunResult> RunTestAsync(string testPath)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"test {testPath} --verbosity quiet";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            
            process.Start();
            await process.WaitForExitAsync();
            
            return new TestRunResult
            {
                Success = process.ExitCode == 0,
                Output = await process.StandardOutput.ReadToEndAsync(),
                Error = await process.StandardError.ReadToEndAsync(),
                ExitCode = process.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new TestRunResult
            {
                Success = false,
                Error = ex.Message,
                ExitCode = -1
            };
        }
    }
    
    private string GetTestFilePath<T>(TddScenario<T> scenario)
    {
        return Path.Combine(_config.Get<string>("testing.test_directory", "Tests"), $"{scenario.Name}Tests.cs");
    }
    
    private string GetImplementationFilePath<T>(TddScenario<T> scenario)
    {
        return Path.Combine(_config.Get<string>("testing.source_directory", "src"), $"{scenario.ClassName}.cs");
    }
}

public class TddScenario<T>
{
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public List<TddTestCase> TestCases { get; set; } = new();
    public List<TddMethod> Methods { get; set; } = new();
}

public class TddTestCase
{
    public string Name { get; set; } = string.Empty;
    public string ExpectedBehavior { get; set; } = string.Empty;
    public string Arrange { get; set; } = string.Empty;
    public string Act { get; set; } = string.Empty;
    public string Assert { get; set; } = string.Empty;
}

public class TddMethod
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public string MinimalImplementation { get; set; } = string.Empty;
}

public class TddResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public TddStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public TddPhaseResult TestPhase { get; set; } = new();
    public TddPhaseResult ImplementationPhase { get; set; } = new();
    public TddPhaseResult RefactorPhase { get; set; } = new();
}

public class TddPhaseResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    
    public static TddPhaseResult Success(string message)
    {
        return new TddPhaseResult { Success = true, Message = message };
    }
    
    public static TddPhaseResult Failure(string message)
    {
        return new TddPhaseResult { Success = false, Message = message };
    }
}

public enum TddStatus
{
    Pending,
    Success,
    Failed
}

public class TestRunResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}
```

## 🎭 Behavior-Driven Development (BDD)

### BDD Framework

```csharp
public class BddFramework
{
    private readonly ILogger<BddFramework> _logger;
    private readonly TSKConfig _config;
    private readonly List<BddScenario> _scenarios;
    
    public BddFramework(ILogger<BddFramework> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _scenarios = new List<BddScenario>();
    }
    
    public void Given(string description, Action setup)
    {
        var scenario = GetCurrentScenario();
        scenario.GivenSteps.Add(new BddStep
        {
            Description = description,
            Action = setup
        });
    }
    
    public void When(string description, Action action)
    {
        var scenario = GetCurrentScenario();
        scenario.WhenSteps.Add(new BddStep
        {
            Description = description,
            Action = action
        });
    }
    
    public void Then(string description, Action assertion)
    {
        var scenario = GetCurrentScenario();
        scenario.ThenSteps.Add(new BddStep
        {
            Description = description,
            Action = assertion
        });
    }
    
    public void And(string description, Action action)
    {
        var scenario = GetCurrentScenario();
        var lastStep = scenario.ThenSteps.Any() ? scenario.ThenSteps.Last() :
                      scenario.WhenSteps.Any() ? scenario.WhenSteps.Last() :
                      scenario.GivenSteps.Last();
        
        lastStep.AndSteps.Add(new BddStep
        {
            Description = description,
            Action = action
        });
    }
    
    public async Task<BddResult> ExecuteScenarioAsync(string scenarioName)
    {
        var scenario = _scenarios.FirstOrDefault(s => s.Name == scenarioName);
        if (scenario == null)
        {
            throw new ArgumentException($"Scenario '{scenarioName}' not found");
        }
        
        _logger.LogInformation("Executing BDD scenario: {ScenarioName}", scenarioName);
        
        var result = new BddResult
        {
            ScenarioName = scenarioName,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            // Execute Given steps
            foreach (var step in scenario.GivenSteps)
            {
                await ExecuteStepAsync(step, "Given", result);
            }
            
            // Execute When steps
            foreach (var step in scenario.WhenSteps)
            {
                await ExecuteStepAsync(step, "When", result);
            }
            
            // Execute Then steps
            foreach (var step in scenario.ThenSteps)
            {
                await ExecuteStepAsync(step, "Then", result);
            }
            
            result.Status = BddStatus.Success;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            
            _logger.LogInformation("BDD scenario '{ScenarioName}' completed successfully", scenarioName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BDD scenario '{ScenarioName}' failed", scenarioName);
            result.Status = BddStatus.Failed;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private async Task ExecuteStepAsync(BddStep step, string stepType, BddResult result)
    {
        try
        {
            _logger.LogDebug("Executing {StepType} step: {Description}", stepType, step.Description);
            
            var stepStartTime = DateTime.UtcNow;
            
            // Execute main step
            step.Action();
            
            // Execute And steps
            foreach (var andStep in step.AndSteps)
            {
                andStep.Action();
            }
            
            var stepDuration = DateTime.UtcNow - stepStartTime;
            
            result.Steps.Add(new BddStepResult
            {
                StepType = stepType,
                Description = step.Description,
                Success = true,
                Duration = stepDuration
            });
            
            _logger.LogDebug("Step completed successfully in {Duration}ms", stepDuration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var stepResult = new BddStepResult
            {
                StepType = stepType,
                Description = step.Description,
                Success = false,
                ErrorMessage = ex.Message
            };
            
            result.Steps.Add(stepResult);
            throw;
        }
    }
    
    public void CreateScenario(string name, string description, Action scenarioDefinition)
    {
        var scenario = new BddScenario
        {
            Name = name,
            Description = description
        };
        
        _scenarios.Add(scenario);
        
        // Set current scenario context
        SetCurrentScenario(scenario);
        
        // Execute scenario definition
        scenarioDefinition();
        
        _logger.LogInformation("Created BDD scenario: {Name} - {Description}", name, description);
    }
    
    private BddScenario GetCurrentScenario()
    {
        var scenario = _scenarios.LastOrDefault();
        if (scenario == null)
        {
            throw new InvalidOperationException("No scenario context available. Call CreateScenario first.");
        }
        
        return scenario;
    }
    
    private void SetCurrentScenario(BddScenario scenario)
    {
        // This would typically use a context or thread-local storage
        // For simplicity, we'll use the last scenario in the list
    }
    
    public List<BddScenario> GetAllScenarios()
    {
        return new List<BddScenario>(_scenarios);
    }
    
    public async Task<List<BddResult>> ExecuteAllScenariosAsync()
    {
        var results = new List<BddResult>();
        
        foreach (var scenario in _scenarios)
        {
            var result = await ExecuteScenarioAsync(scenario.Name);
            results.Add(result);
        }
        
        return results;
    }
}

public class BddScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<BddStep> GivenSteps { get; set; } = new();
    public List<BddStep> WhenSteps { get; set; } = new();
    public List<BddStep> ThenSteps { get; set; } = new();
}

public class BddStep
{
    public string Description { get; set; } = string.Empty;
    public Action Action { get; set; } = () => { };
    public List<BddStep> AndSteps { get; set; } = new();
}

public class BddResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public BddStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<BddStepResult> Steps { get; set; } = new();
}

public class BddStepResult
{
    public string StepType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}

public enum BddStatus
{
    Pending,
    Success,
    Failed
}

// Example BDD usage
public class UserRegistrationBddTests
{
    private readonly BddFramework _bdd;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private CreateUserRequest _request = new();
    private UserDto? _createdUser;
    private Exception? _exception;
    
    public UserRegistrationBddTests(BddFramework bdd, IUserService userService, IEmailService emailService)
    {
        _bdd = bdd;
        _userService = userService;
        _emailService = emailService;
    }
    
    public void SetupScenarios()
    {
        _bdd.CreateScenario(
            "Successful User Registration",
            "As a new user, I want to register so that I can access the system",
            () =>
            {
                _bdd.Given("I have valid registration information", () =>
                {
                    _request = new CreateUserRequest
                    {
                        Email = "test@example.com",
                        FirstName = "John",
                        LastName = "Doe"
                    };
                });
                
                _bdd.When("I submit the registration form", async () =>
                {
                    _createdUser = await _userService.CreateUserAsync(_request);
                });
                
                _bdd.Then("my account should be created successfully", () =>
                {
                    Assert.NotNull(_createdUser);
                    Assert.Equal(_request.Email, _createdUser.Email);
                    Assert.Equal(_request.FirstName, _createdUser.FirstName);
                    Assert.Equal(_request.LastName, _createdUser.LastName);
                });
                
                _bdd.And("a welcome email should be sent", () =>
                {
                    // Verify email was sent
                    Assert.True(_emailService.WasEmailSent(_request.Email, "Welcome"));
                });
            });
        
        _bdd.CreateScenario(
            "Duplicate Email Registration",
            "As a user, I should not be able to register with an existing email",
            () =>
            {
                _bdd.Given("a user account already exists", async () =>
                {
                    await _userService.CreateUserAsync(new CreateUserRequest
                    {
                        Email = "existing@example.com",
                        FirstName = "Jane",
                        LastName = "Doe"
                    });
                });
                
                _bdd.And("I try to register with the same email", () =>
                {
                    _request = new CreateUserRequest
                    {
                        Email = "existing@example.com",
                        FirstName = "John",
                        LastName = "Smith"
                    };
                });
                
                _bdd.When("I submit the registration form", async () =>
                {
                    try
                    {
                        _createdUser = await _userService.CreateUserAsync(_request);
                    }
                    catch (Exception ex)
                    {
                        _exception = ex;
                    }
                });
                
                _bdd.Then("registration should be rejected", () =>
                {
                    Assert.NotNull(_exception);
                    Assert.IsType<ValidationException>(_exception);
                });
                
                _bdd.And("an appropriate error message should be shown", () =>
                {
                    Assert.Contains("already exists", _exception!.Message);
                });
            });
    }
}
```

## 🔄 Testing Patterns

### Test Data Builder Pattern

```csharp
public class TestDataBuilder
{
    private readonly ILogger<TestDataBuilder> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<Type, object> _builders;
    
    public TestDataBuilder(ILogger<TestDataBuilder> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _builders = new Dictionary<Type, object>();
        
        RegisterBuilders();
    }
    
    private void RegisterBuilders()
    {
        _builders[typeof(UserDto)] = new UserDtoBuilder();
        _builders[typeof(CreateUserRequest)] = new CreateUserRequestBuilder();
        _builders[typeof(UpdateUserRequest)] = new UpdateUserRequestBuilder();
    }
    
    public T Create<T>(Action<T>? configure = null) where T : class, new()
    {
        var builder = GetBuilder<T>();
        var instance = builder.Build();
        
        configure?.Invoke(instance);
        
        return instance;
    }
    
    public List<T> CreateMany<T>(int count, Action<T>? configure = null) where T : class, new()
    {
        var instances = new List<T>();
        
        for (int i = 0; i < count; i++)
        {
            var instance = Create<T>(configure);
            instances.Add(instance);
        }
        
        return instances;
    }
    
    private ITestDataBuilder<T> GetBuilder<T>() where T : class, new()
    {
        if (_builders.TryGetValue(typeof(T), out var builder))
        {
            return (ITestDataBuilder<T>)builder;
        }
        
        return new DefaultTestDataBuilder<T>();
    }
}

public interface ITestDataBuilder<T> where T : class, new()
{
    T Build();
}

public class DefaultTestDataBuilder<T> : ITestDataBuilder<T> where T : class, new()
{
    public T Build()
    {
        return new T();
    }
}

public class UserDtoBuilder : ITestDataBuilder<UserDto>
{
    private readonly Random _random = new();
    
    public UserDto Build()
    {
        return new UserDto
        {
            Id = Guid.NewGuid(),
            Email = $"user{_random.Next(1000)}@example.com",
            FirstName = GetRandomFirstName(),
            LastName = GetRandomLastName(),
            Status = GetRandomStatus()
        };
    }
    
    private string GetRandomFirstName()
    {
        var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Eve", "Frank" };
        return firstNames[_random.Next(firstNames.Length)];
    }
    
    private string GetRandomLastName()
    {
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        return lastNames[_random.Next(lastNames.Length)];
    }
    
    private UserStatus GetRandomStatus()
    {
        var statuses = Enum.GetValues<UserStatus>();
        return statuses[_random.Next(statuses.Length)];
    }
}

public class CreateUserRequestBuilder : ITestDataBuilder<CreateUserRequest>
{
    private readonly Random _random = new();
    
    public CreateUserRequest Build()
    {
        return new CreateUserRequest
        {
            Email = $"user{_random.Next(1000)}@example.com",
            FirstName = GetRandomFirstName(),
            LastName = GetRandomLastName()
        };
    }
    
    private string GetRandomFirstName()
    {
        var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Eve", "Frank" };
        return firstNames[_random.Next(firstNames.Length)];
    }
    
    private string GetRandomLastName()
    {
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        return lastNames[_random.Next(lastNames.Length)];
    }
}

public class UpdateUserRequestBuilder : ITestDataBuilder<UpdateUserRequest>
{
    private readonly Random _random = new();
    
    public UpdateUserRequest Build()
    {
        return new UpdateUserRequest
        {
            FirstName = GetRandomFirstName(),
            LastName = GetRandomLastName()
        };
    }
    
    private string GetRandomFirstName()
    {
        var firstNames = new[] { "John", "Jane", "Bob", "Alice", "Charlie", "Diana", "Eve", "Frank" };
        return firstNames[_random.Next(firstNames.Length)];
    }
    
    private string GetRandomLastName()
    {
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        return lastNames[_random.Next(lastNames.Length)];
    }
}
```

### Test Fixture Pattern

```csharp
public abstract class TestFixtureBase : IDisposable
{
    protected readonly ILogger _logger;
    protected readonly TSKConfig _config;
    protected readonly TestDataBuilder _dataBuilder;
    protected readonly IDbConnection _connection;
    protected readonly List<object> _createdEntities;
    
    protected TestFixtureBase(ILogger logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _dataBuilder = new TestDataBuilder(logger, config);
        _connection = new SqliteConnection("Data Source=:memory:");
        _createdEntities = new List<object>();
        
        SetupDatabase();
    }
    
    protected virtual void SetupDatabase()
    {
        _connection.Open();
        
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
        
        _connection.Execute(createUsersTable);
    }
    
    protected async Task<T> CreateTestEntityAsync<T>(Action<T>? configure = null) where T : class, new()
    {
        var entity = _dataBuilder.Create<T>(configure);
        await SaveEntityAsync(entity);
        _createdEntities.Add(entity);
        return entity;
    }
    
    protected async Task SaveEntityAsync<T>(T entity)
    {
        // Implementation would save entity to database
        await Task.CompletedTask;
    }
    
    protected async Task CleanupTestDataAsync()
    {
        foreach (var entity in _createdEntities)
        {
            await DeleteEntityAsync(entity);
        }
        
        _createdEntities.Clear();
    }
    
    protected async Task DeleteEntityAsync(object entity)
    {
        // Implementation would delete entity from database
        await Task.CompletedTask;
    }
    
    public virtual void Dispose()
    {
        CleanupTestDataAsync().Wait();
        _connection?.Dispose();
    }
}

public class UserServiceTestFixture : TestFixtureBase
{
    public IUserService UserService { get; }
    public IEmailService EmailService { get; }
    public IEventBus EventBus { get; }
    
    public UserServiceTestFixture(ILogger<UserServiceTestFixture> logger, TSKConfig config)
        : base(logger, config)
    {
        var userRepository = new UserRepository(_connection);
        EmailService = new MockEmailService();
        EventBus = new MockEventBus();
        
        UserService = new UserService(
            logger,
            config,
            Mock.Of<IServiceProvider>(),
            Mock.Of<IHealthCheckService>(),
            userRepository,
            EventBus);
    }
    
    public async Task<UserDto> CreateTestUserAsync(string? email = null)
    {
        return await CreateTestEntityAsync<CreateUserRequest>(request =>
        {
            if (email != null)
            {
                request.Email = email;
            }
        });
    }
    
    public async Task<List<UserDto>> CreateTestUsersAsync(int count)
    {
        var users = new List<UserDto>();
        
        for (int i = 0; i < count; i++)
        {
            var user = await CreateTestUserAsync();
            users.Add(user);
        }
        
        return users;
    }
}
```

## 📝 Summary

This guide covered comprehensive testing strategies for C# TuskLang applications:

- **Test-Driven Development (TDD)**: TDD workflow implementation with Red-Green-Refactor cycle
- **Behavior-Driven Development (BDD)**: BDD framework with Given-When-Then syntax
- **Testing Patterns**: Test data builder pattern and test fixture pattern
- **Testing Best Practices**: Proper test organization, data management, and cleanup

These testing strategies ensure your C# TuskLang applications have comprehensive test coverage and follow industry best practices for test-driven development. 
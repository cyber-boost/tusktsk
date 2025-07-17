# Functional Programming in C# with TuskLang

## Overview

Functional programming emphasizes immutability, pure functions, and declarative code. This guide covers how to implement functional programming patterns using C# and TuskLang configuration for building maintainable, testable, and predictable applications.

## Table of Contents

- [Functional Programming Concepts](#functional-programming-concepts)
- [TuskLang Functional Configuration](#tusklang-functional-configuration)
- [Pure Functions](#pure-functions)
- [C# Functional Example](#c-functional-example)
- [Immutability](#immutability)
- [Higher-Order Functions](#higher-order-functions)
- [Monads and Functors](#monads-and-functors)
- [Best Practices](#best-practices)

## Functional Programming Concepts

- **Pure Functions**: Functions with no side effects
- **Immutability**: Data that cannot be changed after creation
- **Higher-Order Functions**: Functions that take or return other functions
- **Composition**: Combining functions to create new functionality
- **Monads**: Wrapper types that handle effects and composition

## TuskLang Functional Configuration

```ini
# functional.tsk
[functional]
enabled = @env("FUNCTIONAL_ENABLED", "true")
immutability_enforced = @env("IMMUTABILITY_ENFORCED", "true")
pure_functions_only = @env("PURE_FUNCTIONS_ONLY", "false")
composition_enabled = @env("COMPOSITION_ENABLED", "true")

[validation]
input_validation = @env("INPUT_VALIDATION", "true")
output_validation = @env("OUTPUT_VALIDATION", "true")
contract_testing = @env("CONTRACT_TESTING", "true")

[monads]
option_enabled = @env("OPTION_MONAD_ENABLED", "true")
result_enabled = @env("RESULT_MONAD_ENABLED", "true")
either_enabled = @env("EITHER_MONAD_ENABLED", "true")
```

## Pure Functions

```csharp
public static class PureFunctions
{
    // Pure function - no side effects, same input always produces same output
    public static int Add(int a, int b) => a + b;
    
    public static string FormatName(string firstName, string lastName) 
        => $"{firstName} {lastName}".Trim();
    
    public static decimal CalculateTax(decimal amount, decimal rate) 
        => amount * (rate / 100m);
    
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        return email.Contains("@") && email.Contains(".");
    }
    
    public static List<int> FilterEvenNumbers(List<int> numbers)
        => numbers.Where(n => n % 2 == 0).ToList();
}
```

## C# Functional Example

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using TuskLang;

public static class FunctionalUserService
{
    private readonly static IConfiguration _config;

    static FunctionalUserService()
    {
        _config = TuskConfig.Load("functional.tsk");
    }

    // Pure function for user validation
    public static ValidationResult<User> ValidateUser(User user)
    {
        if (!bool.Parse(_config["validation:input_validation"]))
            return ValidationResult<User>.Success(user);

        var errors = new List<string>();

        if (string.IsNullOrEmpty(user.Email))
            errors.Add("Email is required");

        if (string.IsNullOrEmpty(user.Name))
            errors.Add("Name is required");

        if (user.Age < 0 || user.Age > 150)
            errors.Add("Age must be between 0 and 150");

        return errors.Any() 
            ? ValidationResult<User>.Failure(errors)
            : ValidationResult<User>.Success(user);
    }

    // Pure function for user transformation
    public static User TransformUser(User user, Func<User, User> transformer)
    {
        if (!bool.Parse(_config["functional:composition_enabled"]))
            return user;

        return transformer(user);
    }

    // Higher-order function for user processing
    public static Func<User, Result<User>> CreateUserProcessor(
        Func<User, ValidationResult<User>> validator,
        Func<User, User> transformer,
        Func<User, Task<Result<User>>> saver)
    {
        return async user =>
        {
            // Validate
            var validation = validator(user);
            if (!validation.IsSuccess)
                return Result<User>.Failure(validation.Errors);

            // Transform
            var transformedUser = transformer(validation.Value);

            // Save
            return await saver(transformedUser);
        };
    }

    // Composition example
    public static Func<User, Result<User>> ProcessUser = 
        CreateUserProcessor(
            ValidateUser,
            user => user with { Name = user.Name.ToUpper() },
            SaveUserAsync
        );
}

// Immutable data structures
public record User(
    string Id,
    string Name,
    string Email,
    int Age,
    DateTime CreatedOn)
{
    public User WithName(string newName) => this with { Name = newName };
    public User WithEmail(string newEmail) => this with { Email = newEmail };
    public User WithAge(int newAge) => this with { Age = newAge };
}

// Validation result monad
public class ValidationResult<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public List<string> Errors { get; }

    private ValidationResult(bool isSuccess, T value, List<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors ?? new List<string>();
    }

    public static ValidationResult<T> Success(T value) 
        => new(true, value, null);

    public static ValidationResult<T> Failure(List<string> errors) 
        => new(false, default, errors);

    public ValidationResult<TNew> Map<TNew>(Func<T, TNew> mapper)
        => IsSuccess 
            ? ValidationResult<TNew>.Success(mapper(Value))
            : ValidationResult<TNew>.Failure(Errors);

    public ValidationResult<TNew> Bind<TNew>(Func<T, ValidationResult<TNew>> binder)
        => IsSuccess 
            ? binder(Value)
            : ValidationResult<TNew>.Failure(Errors);
}

// Result monad for async operations
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }

    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        => IsSuccess 
            ? Result<TNew>.Success(mapper(Value))
            : Result<TNew>.Failure(Error);

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
        => IsSuccess 
            ? Result<TNew>.Success(await mapper(Value))
            : Result<TNew>.Failure(Error);

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
        => IsSuccess 
            ? binder(Value)
            : Result<TNew>.Failure(Error);

    public async Task<Result<TNew>> BindAsync<TNew>(Func<T, Task<Result<TNew>>> binder)
        => IsSuccess 
            ? await binder(Value)
            : Result<TNew>.Failure(Error);
}
```

## Immutability

```csharp
public static class ImmutabilityExamples
{
    // Immutable collections
    public static IReadOnlyList<int> CreateImmutableList(params int[] numbers)
        => numbers.ToList().AsReadOnly();

    public static IReadOnlyDictionary<string, string> CreateImmutableDictionary(
        params (string key, string value)[] pairs)
        => pairs.ToDictionary(p => p.key, p => p.value);

    // Immutable builder pattern
    public class UserBuilder
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private string _email = "";
        private int _age = 0;
        private DateTime _createdOn = DateTime.UtcNow;

        public UserBuilder WithName(string name)
        {
            var builder = Clone();
            builder._name = name;
            return builder;
        }

        public UserBuilder WithEmail(string email)
        {
            var builder = Clone();
            builder._email = email;
            return builder;
        }

        public UserBuilder WithAge(int age)
        {
            var builder = Clone();
            builder._age = age;
            return builder;
        }

        public User Build() => new(_id, _name, _email, _age, _createdOn);

        private UserBuilder Clone()
        {
            return new UserBuilder
            {
                _id = this._id,
                _name = this._name,
                _email = this._email,
                _age = this._age,
                _createdOn = this._createdOn
            };
        }
    }
}
```

## Higher-Order Functions

```csharp
public static class HigherOrderFunctions
{
    // Function composition
    public static Func<T, V> Compose<T, U, V>(Func<T, U> f, Func<U, V> g)
        => x => g(f(x));

    // Partial application
    public static Func<T2, TResult> Partial<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1)
        => arg2 => func(arg1, arg2);

    // Currying
    public static Func<T1, Func<T2, TResult>> Curry<T1, T2, TResult>(Func<T1, T2, TResult> func)
        => arg1 => arg2 => func(arg1, arg2);

    // Pipeline operator simulation
    public static T Pipe<T>(this T value, Func<T, T> func) => func(value);

    // Map over collections
    public static IEnumerable<TResult> Map<T, TResult>(this IEnumerable<T> items, Func<T, TResult> mapper)
        => items.Select(mapper);

    // Filter collections
    public static IEnumerable<T> Filter<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        => items.Where(predicate);

    // Reduce collections
    public static TAccumulate Reduce<T, TAccumulate>(
        this IEnumerable<T> items, 
        TAccumulate seed, 
        Func<TAccumulate, T, TAccumulate> reducer)
        => items.Aggregate(seed, reducer);
}
```

## Monads and Functors

```csharp
// Option monad
public class Option<T>
{
    private readonly T _value;
    private readonly bool _hasValue;

    private Option(T value, bool hasValue)
    {
        _value = value;
        _hasValue = hasValue;
    }

    public static Option<T> Some(T value) => new(value, true);
    public static Option<T> None<T>() => new(default(T), false);

    public bool IsSome => _hasValue;
    public bool IsNone => !_hasValue;

    public T Value => _hasValue ? _value : throw new InvalidOperationException("Option is None");

    public Option<TNew> Map<TNew>(Func<T, TNew> mapper)
        => _hasValue ? Option<TNew>.Some(mapper(_value)) : Option<TNew>.None<TNew>();

    public Option<TNew> Bind<TNew>(Func<T, Option<TNew>> binder)
        => _hasValue ? binder(_value) : Option<TNew>.None<TNew>();

    public T GetOrElse(T defaultValue) => _hasValue ? _value : defaultValue;

    public T GetOrElse(Func<T> defaultValueFactory) => _hasValue ? _value : defaultValueFactory();
}

// Either monad
public class Either<TLeft, TRight>
{
    private readonly TLeft _left;
    private readonly TRight _right;
    private readonly bool _isRight;

    private Either(TLeft left, TRight right, bool isRight)
    {
        _left = left;
        _right = right;
        _isRight = isRight;
    }

    public static Either<TLeft, TRight> Left(TLeft left) => new(left, default, false);
    public static Either<TLeft, TRight> Right(TRight right) => new(default, right, true);

    public bool IsRight => _isRight;
    public bool IsLeft => !_isRight;

    public TRight RightValue => _isRight ? _right : throw new InvalidOperationException("Either is Left");
    public TLeft LeftValue => !_isRight ? _left : throw new InvalidOperationException("Either is Right");

    public Either<TLeft, TNewRight> Map<TNewRight>(Func<TRight, TNewRight> mapper)
        => _isRight 
            ? Either<TLeft, TNewRight>.Right(mapper(_right))
            : Either<TLeft, TNewRight>.Left(_left);

    public Either<TLeft, TNewRight> Bind<TNewRight>(Func<TRight, Either<TLeft, TNewRight>> binder)
        => _isRight ? binder(_right) : Either<TLeft, TNewRight>.Left(_left);

    public T Match<T>(Func<TLeft, T> leftHandler, Func<TRight, T> rightHandler)
        => _isRight ? rightHandler(_right) : leftHandler(_left);
}

// Functor implementation
public static class FunctorExtensions
{
    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> mapper)
        => option.Map(mapper);

    public static Either<TLeft, TResult> Map<TLeft, TRight, TResult>(
        this Either<TLeft, TRight> either, 
        Func<TRight, TResult> mapper)
        => either.Map(mapper);

    public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> mapper)
        => result.Map(mapper);
}
```

## Best Practices

1. **Write pure functions whenever possible**
2. **Use immutable data structures**
3. **Leverage function composition**
4. **Use monads for error handling and optional values**
5. **Avoid side effects in pure functions**
6. **Use higher-order functions for code reuse**
7. **Test pure functions thoroughly**

## Conclusion

Functional programming with C# and TuskLang enables building maintainable, testable, and predictable applications. By leveraging TuskLang for configuration and functional patterns, you can create systems that are easier to reason about and maintain. 
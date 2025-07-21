using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace TuskLang
{
    /// <summary>
    /// Advanced error handling and recovery system for TuskLang C# SDK
    /// Implements retry mechanisms, circuit breakers, and graceful degradation
    /// </summary>
    public class ErrorHandling
    {
        private readonly Dictionary<string, CircuitBreaker> _circuitBreakers;
        private readonly Dictionary<string, RetryPolicy> _retryPolicies;
        private readonly List<ErrorHandler> _errorHandlers;
        private readonly ErrorMetrics _metrics;

        public ErrorHandling()
        {
            _circuitBreakers = new Dictionary<string, CircuitBreaker>();
            _retryPolicies = new Dictionary<string, RetryPolicy>();
            _errorHandlers = new List<ErrorHandler>();
            _metrics = new ErrorMetrics();
        }

        /// <summary>
        /// Execute operation with comprehensive error handling
        /// </summary>
        public async Task<T> ExecuteWithErrorHandling<T>(
            Func<Task<T>> operation,
            string operationName = null,
            Dictionary<string, object> context = null)
        {
            var opName = operationName ?? operation.Method.Name;
            var circuitBreaker = GetOrCreateCircuitBreaker(opName);
            var retryPolicy = GetOrCreateRetryPolicy(opName);

            try
            {
                // Check circuit breaker state
                if (circuitBreaker.IsOpen)
                {
                    throw new CircuitBreakerOpenException($"Circuit breaker for {opName} is open");
                }

                // Execute with retry policy
                var result = await retryPolicy.ExecuteAsync(operation);
                
                // Record success
                circuitBreaker.OnSuccess();
                _metrics.RecordSuccess(opName);
                
                return result;
            }
            catch (Exception ex)
            {
                // Record failure
                circuitBreaker.OnFailure();
                _metrics.RecordFailure(opName, ex);
                
                // Apply error handlers
                var handledResult = await ApplyErrorHandlers<T>(ex, context);
                if (handledResult.HasValue)
                {
                    return handledResult.Value;
                }
                
                // Re-throw if not handled
                throw;
            }
        }

        /// <summary>
        /// Add custom error handler
        /// </summary>
        public void AddErrorHandler(ErrorHandler handler)
        {
            _errorHandlers.Add(handler);
        }

        /// <summary>
        /// Configure retry policy for operation
        /// </summary>
        public void ConfigureRetryPolicy(string operationName, RetryPolicy policy)
        {
            _retryPolicies[operationName] = policy;
        }

        /// <summary>
        /// Configure circuit breaker for operation
        /// </summary>
        public void ConfigureCircuitBreaker(string operationName, CircuitBreaker circuitBreaker)
        {
            _circuitBreakers[operationName] = circuitBreaker;
        }

        private CircuitBreaker GetOrCreateCircuitBreaker(string operationName)
        {
            if (!_circuitBreakers.ContainsKey(operationName))
            {
                _circuitBreakers[operationName] = new CircuitBreaker();
            }
            return _circuitBreakers[operationName];
        }

        private RetryPolicy GetOrCreateRetryPolicy(string operationName)
        {
            if (!_retryPolicies.ContainsKey(operationName))
            {
                _retryPolicies[operationName] = new RetryPolicy();
            }
            return _retryPolicies[operationName];
        }

        private async Task<T?> ApplyErrorHandlers<T>(Exception ex, Dictionary<string, object> context)
        {
            foreach (var handler in _errorHandlers)
            {
                if (handler.CanHandle(ex))
                {
                    var result = await handler.HandleAsync<T>(ex, context);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return default(T);
        }

        /// <summary>
        /// Get error metrics
        /// </summary>
        public ErrorMetrics GetMetrics()
        {
            return _metrics;
        }
    }

    /// <summary>
    /// Circuit breaker pattern implementation
    /// </summary>
    public class CircuitBreaker
    {
        private CircuitState _state = CircuitState.Closed;
        private int _failureCount = 0;
        private DateTime _lastFailureTime;
        private readonly int _failureThreshold = 5;
        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1);

        public bool IsOpen => _state == CircuitState.Open;
        public bool IsHalfOpen => _state == CircuitState.HalfOpen;
        public bool IsClosed => _state == CircuitState.Closed;

        public void OnSuccess()
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
        }

        public void OnFailure()
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
            }
        }

        public void TryReset()
        {
            if (_state == CircuitState.Open && 
                DateTime.UtcNow - _lastFailureTime > _timeout)
            {
                _state = CircuitState.HalfOpen;
            }
        }
    }

    /// <summary>
    /// Retry policy with exponential backoff
    /// </summary>
    public class RetryPolicy
    {
        private readonly int _maxRetries = 3;
        private readonly TimeSpan _baseDelay = TimeSpan.FromSeconds(1);
        private readonly double _backoffMultiplier = 2.0;

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            var lastException = default(Exception);

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt == _maxRetries)
                    {
                        break;
                    }

                    var delay = TimeSpan.FromMilliseconds(
                        _baseDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt));
                    
                    await Task.Delay(delay);
                }
            }

            throw lastException;
        }
    }

    /// <summary>
    /// Error handler interface
    /// </summary>
    public abstract class ErrorHandler
    {
        public abstract bool CanHandle(Exception exception);
        public abstract Task<T?> HandleAsync<T>(Exception exception, Dictionary<string, object> context);
    }

    /// <summary>
    /// Error metrics collection
    /// </summary>
    public class ErrorMetrics
    {
        private readonly Dictionary<string, int> _successCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _failureCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, List<Exception>> _recentErrors = new Dictionary<string, List<Exception>>();

        public void RecordSuccess(string operationName)
        {
            if (!_successCounts.ContainsKey(operationName))
                _successCounts[operationName] = 0;
            _successCounts[operationName]++;
        }

        public void RecordFailure(string operationName, Exception exception)
        {
            if (!_failureCounts.ContainsKey(operationName))
                _failureCounts[operationName] = 0;
            _failureCounts[operationName]++;

            if (!_recentErrors.ContainsKey(operationName))
                _recentErrors[operationName] = new List<Exception>();
            
            _recentErrors[operationName].Add(exception);
            
            // Keep only last 10 errors
            if (_recentErrors[operationName].Count > 10)
            {
                _recentErrors[operationName].RemoveAt(0);
            }
        }

        public double GetSuccessRate(string operationName)
        {
            var total = GetTotalCount(operationName);
            if (total == 0) return 0.0;
            
            var success = _successCounts.GetValueOrDefault(operationName, 0);
            return (double)success / total;
        }

        public int GetTotalCount(string operationName)
        {
            return _successCounts.GetValueOrDefault(operationName, 0) + 
                   _failureCounts.GetValueOrDefault(operationName, 0);
        }

        public List<Exception> GetRecentErrors(string operationName)
        {
            return _recentErrors.GetValueOrDefault(operationName, new List<Exception>());
        }
    }

    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
} 
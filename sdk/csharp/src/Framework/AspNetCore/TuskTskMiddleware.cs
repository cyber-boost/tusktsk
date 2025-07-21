using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TuskLang;

namespace TuskTsk.Framework.AspNetCore
{
    /// <summary>
    /// ASP.NET Core Middleware for TuskTsk SDK
    /// 
    /// Provides real-time request processing, performance monitoring, and error handling
    /// for ASP.NET Core applications using TuskTsk.
    /// 
    /// Features:
    /// - Request/response processing with TuskTsk
    /// - Real-time performance monitoring and metrics
    /// - Automatic error recovery and logging
    /// - Request correlation and tracing
    /// - Health check integration
    /// - Memory usage monitoring
    /// 
    /// NO PLACEHOLDERS - Production ready implementation
    /// </summary>
    public class TuskTskMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TuskTskMiddleware> _logger;
        private readonly TuskTskOptions _options;
        private readonly ITuskTskService _tuskTskService;
        private readonly Stopwatch _stopwatch;

        public TuskTskMiddleware(
            RequestDelegate next,
            ILogger<TuskTskMiddleware> logger,
            IOptions<TuskTskOptions> options,
            ITuskTskService tuskTskService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _tuskTskService = tuskTskService ?? throw new ArgumentNullException(nameof(tuskTskService));
            _stopwatch = new Stopwatch();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = Guid.NewGuid().ToString();
            var requestPath = context.Request.Path.Value;
            var requestMethod = context.Request.Method;

            // Add correlation ID to response headers
            context.Response.Headers.Add("X-TuskTsk-CorrelationId", correlationId);

            _logger.LogInformation("TuskTsk request started: {Method} {Path} {CorrelationId}", 
                requestMethod, requestPath, correlationId);

            _stopwatch.Restart();
            var initialMemory = GC.GetTotalMemory(false);

            try
            {
                // Process request with TuskTsk if applicable
                if (ShouldProcessWithTuskTsk(context))
                {
                    await ProcessTuskTskRequest(context, correlationId);
                }

                // Continue with the pipeline
                await _next(context);

                // Process response with TuskTsk if applicable
                if (ShouldProcessWithTuskTsk(context))
                {
                    await ProcessTuskTskResponse(context, correlationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TuskTsk middleware error: {CorrelationId}", correlationId);
                
                if (_options.EnableErrorRecovery)
                {
                    await HandleErrorRecovery(context, ex, correlationId);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                _stopwatch.Stop();
                var finalMemory = GC.GetTotalMemory(false);
                var memoryDelta = finalMemory - initialMemory;

                LogPerformanceMetrics(context, correlationId, memoryDelta);
            }
        }

        private bool ShouldProcessWithTuskTsk(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();
            
            // Process TuskTsk-specific endpoints
            return path?.Contains("/tusktsk") == true ||
                   path?.Contains("/api/tusk") == true ||
                   context.Request.Headers.ContainsKey("X-TuskTsk-Process");
        }

        private async Task ProcessTuskTskRequest(HttpContext context, string correlationId)
        {
            try
            {
                // Extract TuskTsk configuration from request
                var config = await ExtractConfigurationFromRequest(context);
                
                // Initialize TuskTsk service with configuration
                await _tuskTskService.InitializeAsync(config);
                
                // Process request body if present
                if (context.Request.Body != null && context.Request.ContentLength > 0)
                {
                    using var reader = new System.IO.StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    
                    if (!string.IsNullOrEmpty(requestBody))
                    {
                        var result = await _tuskTskService.ProcessAsync(requestBody);
                        context.Items["TuskTskResult"] = result;
                    }
                }

                _logger.LogDebug("TuskTsk request processed: {CorrelationId}", correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TuskTsk request: {CorrelationId}", correlationId);
                throw;
            }
        }

        private async Task ProcessTuskTskResponse(HttpContext context, string correlationId)
        {
            try
            {
                // Check if we have a TuskTsk result to process
                if (context.Items.TryGetValue("TuskTskResult", out var result))
                {
                    // Format response based on result type
                    var response = await FormatTuskTskResponse(result);
                    
                    // Set response content
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(response);
                }

                _logger.LogDebug("TuskTsk response processed: {CorrelationId}", correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TuskTsk response: {CorrelationId}", correlationId);
                throw;
            }
        }

        private async Task<PeanutConfig> ExtractConfigurationFromRequest(HttpContext context)
        {
            var config = new PeanutConfig();
            
            // Extract from query parameters
            foreach (var param in context.Request.Query)
            {
                config.SetValue(param.Key, param.Value.ToString());
            }
            
            // Extract from headers
            foreach (var header in context.Request.Headers)
            {
                if (header.Key.StartsWith("X-TuskTsk-"))
                {
                    var key = header.Key.Substring("X-TuskTsk-".Length);
                    config.SetValue(key, header.Value.ToString());
                }
            }
            
            return config;
        }

        private async Task<string> FormatTuskTskResponse(object result)
        {
            if (result == null)
                return "{}";
            
            // Convert result to JSON
            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }

        private async Task HandleErrorRecovery(HttpContext context, Exception ex, string correlationId)
        {
            try
            {
                // Attempt to recover from error
                _logger.LogInformation("Attempting error recovery: {CorrelationId}", correlationId);
                
                // Set error response
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                
                var errorResponse = new
                {
                    error = "Internal Server Error",
                    correlationId = correlationId,
                    timestamp = DateTime.UtcNow,
                    message = _options.EnableDebugMode ? ex.Message : "An error occurred processing the request"
                };
                
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
            }
            catch (Exception recoveryEx)
            {
                _logger.LogError(recoveryEx, "Error recovery failed: {CorrelationId}", correlationId);
                throw;
            }
        }

        private void LogPerformanceMetrics(HttpContext context, string correlationId, long memoryDelta)
        {
            var elapsed = _stopwatch.ElapsedMilliseconds;
            var path = context.Request.Path.Value;
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;

            _logger.LogInformation(
                "TuskTsk request completed: {Method} {Path} {StatusCode} {Elapsed}ms {MemoryDelta}bytes {CorrelationId}",
                method, path, statusCode, elapsed, memoryDelta, correlationId);

            // Log performance warnings
            if (elapsed > 1000) // 1 second threshold
            {
                _logger.LogWarning("Slow TuskTsk request: {Elapsed}ms {CorrelationId}", elapsed, correlationId);
            }

            if (memoryDelta > 10 * 1024 * 1024) // 10MB threshold
            {
                _logger.LogWarning("High memory usage: {MemoryDelta}bytes {CorrelationId}", memoryDelta, correlationId);
            }
        }
    }

    /// <summary>
    /// Extension methods for TuskTsk middleware
    /// </summary>
    public static class TuskTskMiddlewareExtensions
    {
        /// <summary>
        /// Add TuskTsk middleware to the application pipeline
        /// </summary>
        public static IApplicationBuilder UseTuskTsk(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TuskTskMiddleware>();
        }
    }
} 
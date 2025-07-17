# 🌐 API Integration - TuskLang for C# - "Connect Everything"

**Master API integration with TuskLang in your C# applications!**

API integration is essential for modern applications. This guide covers REST APIs, GraphQL, gRPC, authentication, and real-world API integration scenarios for TuskLang in C# environments.

## 🔗 API Integration Philosophy

### "We Don't Bow to Any King"
- **Type-safe APIs** - Compile-time API validation
- **Performance optimized** - Efficient HTTP client usage
- **Resilient patterns** - Handle API failures gracefully
- **Authentication first** - Secure API communication
- **Real-time integration** - Live API data in configs

## 🌍 REST API Integration

### Example: REST API Client with TuskLang
```csharp
// RestApiService.cs
using System.Net.Http;
using System.Text.Json;

public class RestApiService
{
    private readonly HttpClient _httpClient;
    private readonly TuskLang _parser;
    private readonly ILogger<RestApiService> _logger;
    
    public RestApiService(HttpClient httpClient, ILogger<RestApiService> logger)
    {
        _httpClient = httpClient;
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<Dictionary<string, object>> LoadApiConfigurationAsync()
    {
        var config = new Dictionary<string, object>();
        
        // Load external API data
        config["weather_data"] = await GetWeatherDataAsync();
        config["currency_rates"] = await GetCurrencyRatesAsync();
        config["stock_prices"] = await GetStockPricesAsync();
        
        return config;
    }
    
    private async Task<Dictionary<string, object>> GetWeatherDataAsync()
    {
        try
        {
            var apiKey = Environment.GetEnvironmentVariable("WEATHER_API_KEY");
            var city = "New York";
            var url = $"https://api.weatherapi.com/v1/current.json?key={apiKey}&q={city}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            
            _logger.LogInformation("Weather data retrieved successfully for {City}", city);
            return weatherData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve weather data");
            return new Dictionary<string, object> { ["error"] = "Weather data unavailable" };
        }
    }
    
    private async Task<Dictionary<string, object>> GetCurrencyRatesAsync()
    {
        try
        {
            var apiKey = Environment.GetEnvironmentVariable("CURRENCY_API_KEY");
            var baseCurrency = "USD";
            var url = $"https://api.exchangerate-api.com/v4/latest/{baseCurrency}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var currencyData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            
            _logger.LogInformation("Currency rates retrieved successfully");
            return currencyData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve currency rates");
            return new Dictionary<string, object> { ["error"] = "Currency rates unavailable" };
        }
    }
    
    private async Task<Dictionary<string, object>> GetStockPricesAsync()
    {
        try
        {
            var apiKey = Environment.GetEnvironmentVariable("STOCK_API_KEY");
            var symbols = "AAPL,GOOGL,MSFT";
            var url = $"https://api.stockdata.org/v1/data/quote?symbols={symbols}&api_token={apiKey}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var stockData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            
            _logger.LogInformation("Stock prices retrieved successfully");
            return stockData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve stock prices");
            return new Dictionary<string, object> { ["error"] = "Stock prices unavailable" };
        }
    }
}
```

## 🔐 Authentication and Authorization

### Example: API Authentication Service
```csharp
// ApiAuthenticationService.cs
public class ApiAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiAuthenticationService> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiry;
    
    public ApiAuthenticationService(HttpClient httpClient, ILogger<ApiAuthenticationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<string> GetAccessTokenAsync()
    {
        // Check if token is still valid
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _accessToken;
        }
        
        // Get new token
        await RefreshAccessTokenAsync();
        return _accessToken;
    }
    
    private async Task RefreshAccessTokenAsync()
    {
        try
        {
            var clientId = Environment.GetEnvironmentVariable("API_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("API_CLIENT_SECRET");
            var tokenUrl = Environment.GetEnvironmentVariable("API_TOKEN_URL");
            
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });
            
            var response = await _httpClient.PostAsync(tokenUrl, tokenRequest);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);
            
            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300); // 5 minutes buffer
            
            _logger.LogInformation("Access token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh access token");
            throw;
        }
    }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
}
```

## 🔄 Resilient API Patterns

### Example: Circuit Breaker Pattern
```csharp
// CircuitBreakerService.cs
using Polly;
using Polly.CircuitBreaker;

public class CircuitBreakerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreaker;
    
    public CircuitBreakerService(HttpClient httpClient, ILogger<CircuitBreakerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _circuitBreaker = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) =>
                {
                    _logger.LogWarning("Circuit breaker opened for {Duration}", duration);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                }
            );
    }
    
    public async Task<Dictionary<string, object>> CallApiWithCircuitBreakerAsync(string url)
    {
        try
        {
            var response = await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _httpClient.GetAsync(url);
            });
            
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker is open, returning fallback data");
            return new Dictionary<string, object> { ["fallback"] = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API call failed");
            throw;
        }
    }
}
```

## 📊 Real-Time API Integration

### Example: WebSocket API Integration
```csharp
// WebSocketApiService.cs
using System.Net.WebSockets;
using System.Text;

public class WebSocketApiService
{
    private readonly ILogger<WebSocketApiService> _logger;
    private ClientWebSocket? _webSocket;
    
    public WebSocketApiService(ILogger<WebSocketApiService> logger)
    {
        _logger = logger;
    }
    
    public async Task ConnectToRealTimeApiAsync(string url)
    {
        _webSocket = new ClientWebSocket();
        
        try
        {
            await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
            _logger.LogInformation("Connected to WebSocket API: {Url}", url);
            
            // Start listening for messages
            _ = Task.Run(async () => await ListenForMessagesAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to WebSocket API");
            throw;
        }
    }
    
    private async Task ListenForMessagesAsync()
    {
        var buffer = new byte[1024];
        
        while (_webSocket?.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessRealTimeMessageAsync(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message");
                break;
            }
        }
    }
    
    private async Task ProcessRealTimeMessageAsync(string message)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
            
            // Update configuration with real-time data
            await UpdateConfigurationWithRealTimeDataAsync(data);
            
            _logger.LogDebug("Processed real-time message: {MessageType}", data.GetValueOrDefault("type"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process real-time message");
        }
    }
    
    private async Task UpdateConfigurationWithRealTimeDataAsync(Dictionary<string, object> data)
    {
        // Update TuskLang configuration with real-time data
        // This could involve updating a shared configuration object
        // or triggering a configuration reload
    }
}
```

## 🛠️ Real-World API Integration Scenarios
- **Payment processing**: Integrate with Stripe, PayPal APIs
- **Social media**: Connect to Twitter, Facebook APIs
- **Email services**: Integrate with SendGrid, Mailgun
- **Analytics**: Connect to Google Analytics, Mixpanel

## 🧩 Best Practices
- Use HTTP client factory for connection pooling
- Implement retry policies with exponential backoff
- Use circuit breakers for fault tolerance
- Cache API responses when appropriate
- Monitor API performance and errors

## 🏁 You're Ready!

You can now:
- Integrate REST APIs with C# TuskLang apps
- Implement secure authentication
- Use resilient patterns for API calls
- Connect to real-time APIs

**Next:** [Cloud Integration](026-cloud-integration-csharp.md)

---

**"We don't bow to any king" - Your API mastery, your integration excellence, your connectivity power.**

Connect everything. Integrate seamlessly. 🌐 
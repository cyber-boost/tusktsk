using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tusk.Protection
{
    /// <summary>
    /// TuskLang SDK License Validation Module
    /// Enterprise-grade license validation for C# SDK
    /// </summary>
    public class TuskLicense
    {
        private readonly string _licenseKey;
        private readonly string _apiKey;
        private readonly string _sessionId;
        private readonly Dictionary<string, LicenseCacheEntry> _licenseCache;
        private readonly List<ValidationAttempt> _validationHistory;
        private readonly List<ExpirationWarning> _expirationWarnings;
        private readonly HttpClient _httpClient;
        private readonly string _cacheDir;
        private readonly string _cacheFile;
        private OfflineCacheData? _offlineCache;
        private readonly ILogger<TuskLicense> _logger;

        public TuskLicense(string licenseKey, string apiKey, string? cacheDir = null, ILogger<TuskLicense>? logger = null)
        {
            _licenseKey = licenseKey;
            _apiKey = apiKey;
            _sessionId = Guid.NewGuid().ToString();
            _licenseCache = new Dictionary<string, LicenseCacheEntry>();
            _validationHistory = new List<ValidationAttempt>();
            _expirationWarnings = new List<ExpirationWarning>();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _logger = logger ?? new ConsoleLogger();

            // Set up offline cache directory
            if (cacheDir != null)
            {
                _cacheDir = cacheDir;
            }
            else
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                _cacheDir = Path.Combine(homeDir, ".tusk", "license_cache");
            }

            Directory.CreateDirectory(_cacheDir);

            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
                var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
                _cacheFile = Path.Combine(_cacheDir, $"{hashString}.cache");
            }

            // Load offline cache if exists
            LoadOfflineCache();
        }

        public ValidationResult ValidateLicenseKey()
        {
            try
            {
                if (string.IsNullOrEmpty(_licenseKey) || _licenseKey.Length < 32)
                {
                    return new ValidationResult { Valid = false, Error = "Invalid license key format" };
                }

                if (!_licenseKey.StartsWith("TUSK-"))
                {
                    return new ValidationResult { Valid = false, Error = "Invalid license key prefix" };
                }

                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(_licenseKey));
                    var checksum = BitConverter.ToString(hash).Replace("-", "").ToLower();

                    if (!checksum.StartsWith("tusk"))
                    {
                        return new ValidationResult { Valid = false, Error = "Invalid license key checksum" };
                    }

                    return new ValidationResult { Valid = true, Checksum = checksum };
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult { Valid = false, Error = ex.Message };
            }
        }

        public async Task<Dictionary<string, object>> VerifyLicenseServerAsync(string serverUrl = "https://api.tusklang.org/v1/license")
        {
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var data = new Dictionary<string, object>
                {
                    ["license_key"] = _licenseKey,
                    ["session_id"] = _sessionId,
                    ["timestamp"] = timestamp
                };

                // Generate signature
                var sortedData = JsonSerializer.Serialize(data.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiKey)))
                {
                    var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(sortedData));
                    data["signature"] = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();
                }

                var request = new HttpRequestMessage(HttpMethod.Post, serverUrl);
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                request.Headers.Add("User-Agent", "TuskLang-CSharp-SDK/1.0.0");
                request.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent) ?? new Dictionary<string, object>();

                    // Update in-memory cache
                    _licenseCache[_licenseKey] = new LicenseCacheEntry
                    {
                        Data = result,
                        Timestamp = timestamp,
                        Expires = timestamp + 3600 // Cache for 1 hour
                    };

                    // Update offline cache
                    await SaveOfflineCacheAsync(result);

                    return result;
                }
                else
                {
                    _logger.LogWarning($"Server returned error: {response.StatusCode}");
                    return await FallbackToOfflineCacheAsync($"Server error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"Network error during license validation: {ex.Message}");
                return await FallbackToOfflineCacheAsync($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("License validation request timeout");
                return await FallbackToOfflineCacheAsync("Request timeout");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during license validation: {ex.Message}");
                return await FallbackToOfflineCacheAsync(ex.Message);
            }
        }

        public ExpirationResult CheckLicenseExpiration()
        {
            try
            {
                var parts = _licenseKey.Split('-');
                if (parts.Length < 4)
                {
                    return new ExpirationResult { Expired = true, Error = "Invalid license key format" };
                }

                var expirationStr = parts[^1];
                if (!long.TryParse(expirationStr, System.Globalization.NumberStyles.HexNumber, null, out var expirationTimestamp))
                {
                    return new ExpirationResult { Expired = true, Error = "Invalid expiration timestamp" };
                }

                var expirationDate = DateTimeOffset.FromUnixTimeSeconds(expirationTimestamp);
                var currentDate = DateTimeOffset.UtcNow;

                if (expirationDate < currentDate)
                {
                    var daysOverdue = (currentDate - expirationDate).Days;
                    return new ExpirationResult
                    {
                        Expired = true,
                        ExpirationDate = expirationDate.ToString("O"),
                        DaysOverdue = daysOverdue
                    };
                }

                var daysRemaining = (expirationDate - currentDate).Days;

                if (daysRemaining <= 30)
                {
                    _expirationWarnings.Add(new ExpirationWarning
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        DaysRemaining = daysRemaining
                    });
                }

                return new ExpirationResult
                {
                    Expired = false,
                    ExpirationDate = expirationDate.ToString("O"),
                    DaysRemaining = daysRemaining,
                    Warning = daysRemaining <= 30
                };
            }
            catch (Exception ex)
            {
                return new ExpirationResult { Expired = true, Error = ex.Message };
            }
        }

        public PermissionResult ValidateLicensePermissions(string feature)
        {
            try
            {
                // Check cached license data
                if (_licenseCache.TryGetValue(_licenseKey, out var cacheEntry))
                {
                    if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < cacheEntry.Expires)
                    {
                        if (cacheEntry.Data.TryGetValue("features", out var featuresObj) && 
                            featuresObj is JsonElement featuresElement && 
                            featuresElement.ValueKind == JsonValueKind.Array)
                        {
                            var features = featuresElement.EnumerateArray().Select(f => f.GetString()).ToList();
                            if (features.Contains(feature))
                            {
                                return new PermissionResult { Allowed = true, Feature = feature };
                            }
                            else
                            {
                                return new PermissionResult { Allowed = false, Feature = feature, Error = "Feature not licensed" };
                            }
                        }
                    }
                }

                // Fallback to basic validation
                switch (feature)
                {
                    case "basic":
                    case "core":
                    case "standard":
                        return new PermissionResult { Allowed = true, Feature = feature };
                    case "premium":
                    case "enterprise":
                        if (_licenseKey.ToUpper().Contains("PREMIUM") || _licenseKey.ToUpper().Contains("ENTERPRISE"))
                        {
                            return new PermissionResult { Allowed = true, Feature = feature };
                        }
                        else
                        {
                            return new PermissionResult { Allowed = false, Feature = feature, Error = "Premium license required" };
                        }
                    default:
                        return new PermissionResult { Allowed = false, Feature = feature, Error = "Unknown feature" };
                }
            }
            catch (Exception ex)
            {
                return new PermissionResult { Allowed = false, Feature = feature, Error = ex.Message };
            }
        }

        public LicenseInfo GetLicenseInfo()
        {
            var validationResult = ValidateLicenseKey();
            var expirationResult = CheckLicenseExpiration();

            var info = new LicenseInfo
            {
                LicenseKey = $"{_licenseKey.Substring(0, Math.Min(8, _licenseKey.Length))}...{_licenseKey.Substring(Math.Max(0, _licenseKey.Length - 4))}",
                SessionId = _sessionId,
                Validation = validationResult,
                Expiration = expirationResult,
                CacheStatus = _licenseCache.ContainsKey(_licenseKey) ? "cached" : "not_cached",
                ValidationCount = _validationHistory.Count,
                Warnings = _expirationWarnings.Count
            };

            if (_licenseCache.TryGetValue(_licenseKey, out var cacheEntry))
            {
                info.CachedData = cacheEntry.Data;
                info.CacheAge = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - cacheEntry.Timestamp;
            }

            return info;
        }

        public void LogValidationAttempt(bool success, string details = "")
        {
            _validationHistory.Add(new ValidationAttempt
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Success = success,
                Details = details,
                SessionId = _sessionId
            });
        }

        private void LoadOfflineCache()
        {
            try
            {
                if (File.Exists(_cacheFile))
                {
                    var json = File.ReadAllText(_cacheFile);
                    var cached = JsonSerializer.Deserialize<OfflineCacheData>(json);

                    if (cached != null)
                    {
                        // Verify the cache is for the correct license key
                        using (var sha256 = SHA256.Create())
                        {
                            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(_licenseKey));
                            var keyHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

                            if (cached.LicenseKeyHash == keyHash)
                            {
                                _offlineCache = cached;
                                _logger.LogInformation("Loaded offline license cache");
                            }
                            else
                            {
                                _logger.LogWarning("Offline cache key mismatch");
                                _offlineCache = null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load offline cache: {ex.Message}");
                _offlineCache = null;
            }
        }

        private async Task SaveOfflineCacheAsync(Dictionary<string, object> licenseData)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(_licenseKey));
                    var keyHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

                    var cacheData = new OfflineCacheData
                    {
                        LicenseKeyHash = keyHash,
                        LicenseData = licenseData,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Expiration = CheckLicenseExpiration()
                    };

                    var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(_cacheFile, json);
                    _offlineCache = cacheData;
                    _logger.LogInformation("Saved license data to offline cache");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save offline cache: {ex.Message}");
            }
        }

        private async Task<Dictionary<string, object>> FallbackToOfflineCacheAsync(string errorMsg)
        {
            if (_offlineCache != null && _offlineCache.LicenseData != null)
            {
                var cacheAge = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _offlineCache.Timestamp;
                var cacheAgeDays = cacheAge / 86400.0;

                // Check if cached license is not expired
                if (!_offlineCache.Expiration.Expired)
                {
                    _logger.LogWarning($"Using offline license cache (age: {cacheAgeDays:F1} days)");
                    var result = new Dictionary<string, object>(_offlineCache.LicenseData)
                    {
                        ["offline_mode"] = true,
                        ["cache_age_days"] = cacheAgeDays,
                        ["warning"] = $"Operating in offline mode due to: {errorMsg}"
                    };
                    return result;
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        ["valid"] = false,
                        ["error"] = $"License expired and server unreachable: {errorMsg}",
                        ["offline_cache_expired"] = true
                    };
                }
            }
            else
            {
                return new Dictionary<string, object>
                {
                    ["valid"] = false,
                    ["error"] = $"No offline cache available: {errorMsg}",
                    ["offline_cache_missing"] = true
                };
            }
        }

        // Data classes
        public class ValidationResult
        {
            public bool Valid { get; set; }
            public string? Error { get; set; }
            public string? Checksum { get; set; }
        }

        public class ExpirationResult
        {
            public bool Expired { get; set; }
            public string? Error { get; set; }
            public string? ExpirationDate { get; set; }
            public long? DaysRemaining { get; set; }
            public long? DaysOverdue { get; set; }
            public bool? Warning { get; set; }
        }

        public class PermissionResult
        {
            public bool Allowed { get; set; }
            public string Feature { get; set; } = "";
            public string? Error { get; set; }
        }

        public class LicenseInfo
        {
            public string LicenseKey { get; set; } = "";
            public string SessionId { get; set; } = "";
            public ValidationResult Validation { get; set; } = new();
            public ExpirationResult Expiration { get; set; } = new();
            public string CacheStatus { get; set; } = "";
            public int ValidationCount { get; set; }
            public int Warnings { get; set; }
            public Dictionary<string, object>? CachedData { get; set; }
            public long? CacheAge { get; set; }
        }

        private class LicenseCacheEntry
        {
            public Dictionary<string, object> Data { get; set; } = new();
            public long Timestamp { get; set; }
            public long Expires { get; set; }
        }

        private class ValidationAttempt
        {
            public long Timestamp { get; set; }
            public bool Success { get; set; }
            public string Details { get; set; } = "";
            public string SessionId { get; set; } = "";
        }

        private class ExpirationWarning
        {
            public long Timestamp { get; set; }
            public long DaysRemaining { get; set; }
        }

        private class OfflineCacheData
        {
            public string LicenseKeyHash { get; set; } = "";
            public Dictionary<string, object> LicenseData { get; set; } = new();
            public long Timestamp { get; set; }
            public ExpirationResult Expiration { get; set; } = new();
        }

        // Simple console logger implementation
        private class ConsoleLogger : ILogger<TuskLicense>
        {
            public IDisposable BeginScope<TState>(TState state) => new NoopDisposable();
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Console.WriteLine($"[TuskLicense] [{logLevel}] {formatter(state, exception)}");
            }

            private class NoopDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
    }

    // Global license instance management
    public static class TuskLicenseManager
    {
        private static TuskLicense? _instance;
        private static readonly object _lock = new object();

        public static TuskLicense InitializeLicense(string licenseKey, string apiKey, string? cacheDir = null, ILogger<TuskLicense>? logger = null)
        {
            lock (_lock)
            {
                _instance = new TuskLicense(licenseKey, apiKey, cacheDir, logger);
                return _instance;
            }
        }

        public static TuskLicense GetLicense()
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("License not initialized. Call InitializeLicense() first.");
                }
                return _instance;
            }
        }
    }
}
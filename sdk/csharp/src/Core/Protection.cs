using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Tusk.Protection
{
    /// <summary>
    /// TuskLang SDK Protection Core Module
    /// Enterprise-grade protection for C# SDK
    /// </summary>
    public class TuskProtection
    {
        private readonly string _licenseKey;
        private readonly string _apiKey;
        private readonly string _sessionId;
        private readonly byte[] _encryptionKey;
        private readonly ConcurrentDictionary<string, string> _integrityChecks;
        private readonly UsageMetrics _usageMetrics;
        private readonly object _metricsLock = new object();

        public TuskProtection(string licenseKey, string apiKey)
        {
            _licenseKey = licenseKey ?? throw new ArgumentNullException(nameof(licenseKey));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _sessionId = Guid.NewGuid().ToString();
            _encryptionKey = DeriveKey(licenseKey);
            _integrityChecks = new ConcurrentDictionary<string, string>();
            _usageMetrics = new UsageMetrics();
        }

        private static byte[] DeriveKey(string password)
        {
            var salt = Encoding.UTF8.GetBytes("tusklang_protection_salt");
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32);
        }

        public bool ValidateLicense()
        {
            try
            {
                if (string.IsNullOrEmpty(_licenseKey) || _licenseKey.Length < 32)
                    return false;

                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(_licenseKey));
                var checksum = Convert.ToHexString(hash).ToLower();
                return checksum.StartsWith("tusk");
            }
            catch
            {
                return false;
            }
        }

        public string EncryptData(string data)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _encryptionKey;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var plaintext = Encoding.UTF8.GetBytes(data);
                var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

                var result = new byte[aes.IV.Length + ciphertext.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);

                return Convert.ToBase64String(result);
            }
            catch
            {
                return data;
            }
        }

        public string DecryptData(string encryptedData)
        {
            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedData);
                if (encryptedBytes.Length < 16)
                    return encryptedData;

                using var aes = Aes.Create();
                aes.Key = _encryptionKey;

                var iv = new byte[16];
                var ciphertext = new byte[encryptedBytes.Length - 16];
                Buffer.BlockCopy(encryptedBytes, 0, iv, 0, 16);
                Buffer.BlockCopy(encryptedBytes, 16, ciphertext, 0, ciphertext.Length);

                aes.IV = iv;
                using var decryptor = aes.CreateDecryptor();
                var plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                return Encoding.UTF8.GetString(plaintext);
            }
            catch
            {
                return encryptedData;
            }
        }

        public bool VerifyIntegrity(string data, string signature)
        {
            try
            {
                var expectedSignature = GenerateSignature(data);
                return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public string GenerateSignature(string data)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToHexString(hash).ToLower();
            }
            catch
            {
                return string.Empty;
            }
        }

        public void TrackUsage(string operation, bool success = true)
        {
            lock (_metricsLock)
            {
                _usageMetrics.ApiCalls++;
                if (!success)
                    _usageMetrics.Errors++;
            }
        }

        public Dictionary<string, object> GetMetrics()
        {
            lock (_metricsLock)
            {
                var uptime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _usageMetrics.StartTime;
                return new Dictionary<string, object>
                {
                    ["start_time"] = _usageMetrics.StartTime,
                    ["api_calls"] = _usageMetrics.ApiCalls,
                    ["errors"] = _usageMetrics.Errors,
                    ["session_id"] = _sessionId,
                    ["uptime"] = uptime
                };
            }
        }

        public string ObfuscateCode(string code)
        {
            var bytes = Encoding.UTF8.GetBytes(code);
            return Convert.ToBase64String(bytes);
        }

        public bool DetectTampering()
        {
            try
            {
                // In production, implement file integrity checks
                // For now, return true as placeholder
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Violation ReportViolation(string violationType, string details)
        {
            var violation = new Violation
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SessionId = _sessionId,
                ViolationType = violationType,
                Details = details,
                LicenseKeyPartial = _licenseKey.Length > 8 
                    ? _licenseKey.Substring(0, 8) + "..." 
                    : _licenseKey
            };

            Console.WriteLine($"SECURITY VIOLATION: {violation}");
            return violation;
        }

        // Inner classes
        public class UsageMetrics
        {
            public long StartTime { get; set; }
            public long ApiCalls { get; set; }
            public long Errors { get; set; }

            public UsageMetrics()
            {
                StartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                ApiCalls = 0;
                Errors = 0;
            }
        }

        public class Violation
        {
            public long Timestamp { get; set; }
            public string SessionId { get; set; }
            public string ViolationType { get; set; }
            public string Details { get; set; }
            public string LicenseKeyPartial { get; set; }

            public override string ToString()
            {
                return $"Violation{{Timestamp={Timestamp}, SessionId='{SessionId}', Type='{ViolationType}', Details='{Details}', LicenseKeyPartial='{LicenseKeyPartial}'}}";
            }
        }

        // Global protection instance
        private static volatile TuskProtection _protectionInstance;
        private static readonly object _instanceLock = new object();

        public static TuskProtection InitializeProtection(string licenseKey, string apiKey)
        {
            if (_protectionInstance == null)
            {
                lock (_instanceLock)
                {
                    if (_protectionInstance == null)
                    {
                        _protectionInstance = new TuskProtection(licenseKey, apiKey);
                    }
                }
            }
            return _protectionInstance;
        }

        public static TuskProtection GetProtection()
        {
            if (_protectionInstance == null)
            {
                throw new InvalidOperationException("Protection not initialized. Call InitializeProtection() first.");
            }
            return _protectionInstance;
        }
    }
} 
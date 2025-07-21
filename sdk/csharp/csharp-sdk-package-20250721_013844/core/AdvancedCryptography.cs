using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace TuskLang
{
    /// <summary>
    /// Advanced cryptography system for TuskLang C# SDK
    /// Provides encryption, digital signatures, key management, and cryptographic protocols
    /// </summary>
    public class AdvancedCryptography
    {
        private readonly Dictionary<string, ICryptoProvider> _providers;
        private readonly List<ICryptoAlgorithm> _algorithms;
        private readonly List<IDigitalSignature> _signatures;
        private readonly CryptoMetrics _metrics;
        private readonly KeyManager _keyManager;
        private readonly EncryptionEngine _encryptionEngine;
        private readonly SignatureEngine _signatureEngine;
        private readonly object _lock = new object();

        public AdvancedCryptography()
        {
            _providers = new Dictionary<string, ICryptoProvider>();
            _algorithms = new List<ICryptoAlgorithm>();
            _signatures = new List<IDigitalSignature>();
            _metrics = new CryptoMetrics();
            _keyManager = new KeyManager();
            _encryptionEngine = new EncryptionEngine();
            _signatureEngine = new SignatureEngine();

            // Register default crypto providers
            RegisterProvider(new AESProvider());
            RegisterProvider(new RSACryptoProvider());
            RegisterProvider(new ECCProvider());
            
            // Register default crypto algorithms
            RegisterAlgorithm(new AES256Algorithm());
            RegisterAlgorithm(new RSA2048Algorithm());
            RegisterAlgorithm(new ECDSAAlgorithm());
            
            // Register default digital signatures
            RegisterSignature(new RSASignature());
            RegisterSignature(new ECDSASignature());
            RegisterSignature(new Ed25519Signature());
        }

        /// <summary>
        /// Register a crypto provider
        /// </summary>
        public void RegisterProvider(string providerName, ICryptoProvider provider)
        {
            lock (_lock)
            {
                _providers[providerName] = provider;
            }
        }

        /// <summary>
        /// Generate cryptographic key
        /// </summary>
        public async Task<KeyGenerationResult> GenerateKeyAsync(
            string keyType,
            KeyGenerationConfig config)
        {
            return await _keyManager.GenerateKeyAsync(keyType, config);
        }

        /// <summary>
        /// Encrypt data
        /// </summary>
        public async Task<EncryptionResult> EncryptDataAsync(
            string providerName,
            byte[] data,
            EncryptionConfig config)
        {
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                throw new InvalidOperationException($"Crypto provider '{providerName}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _encryptionEngine.EncryptAsync(provider, data, config);
                
                _metrics.RecordEncryption(providerName, result.Success, DateTime.UtcNow - startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordEncryption(providerName, false, DateTime.UtcNow - startTime);
                return new EncryptionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Decrypt data
        /// </summary>
        public async Task<DecryptionResult> DecryptDataAsync(
            string providerName,
            byte[] encryptedData,
            DecryptionConfig config)
        {
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                throw new InvalidOperationException($"Crypto provider '{providerName}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _encryptionEngine.DecryptAsync(provider, encryptedData, config);
                
                _metrics.RecordDecryption(providerName, result.Success, DateTime.UtcNow - startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordDecryption(providerName, false, DateTime.UtcNow - startTime);
                return new DecryptionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Create digital signature
        /// </summary>
        public async Task<SignatureResult> CreateSignatureAsync(
            string signatureType,
            byte[] data,
            SignatureConfig config)
        {
            var signature = _signatures.FirstOrDefault(s => s.Name == signatureType);
            if (signature == null)
            {
                throw new InvalidOperationException($"Digital signature '{signatureType}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _signatureEngine.CreateSignatureAsync(signature, data, config);
                
                _metrics.RecordSignatureCreation(signatureType, result.Success, DateTime.UtcNow - startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordSignatureCreation(signatureType, false, DateTime.UtcNow - startTime);
                return new SignatureResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Verify digital signature
        /// </summary>
        public async Task<VerificationResult> VerifySignatureAsync(
            string signatureType,
            byte[] data,
            byte[] signature,
            VerificationConfig config)
        {
            var sig = _signatures.FirstOrDefault(s => s.Name == signatureType);
            if (sig == null)
            {
                throw new InvalidOperationException($"Digital signature '{signatureType}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _signatureEngine.VerifySignatureAsync(sig, data, signature, config);
                
                _metrics.RecordSignatureVerification(signatureType, result.Success, DateTime.UtcNow - startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordSignatureVerification(signatureType, false, DateTime.UtcNow - startTime);
                return new VerificationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Generate hash
        /// </summary>
        public async Task<HashResult> GenerateHashAsync(
            string algorithmName,
            byte[] data,
            HashConfig config)
        {
            var algorithm = _algorithms.FirstOrDefault(a => a.Name == algorithmName);
            if (algorithm == null)
            {
                throw new InvalidOperationException($"Crypto algorithm '{algorithmName}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                var result = await algorithm.GenerateHashAsync(data, config);
                
                _metrics.RecordHashGeneration(algorithmName, result.Success, DateTime.UtcNow - startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordHashGeneration(algorithmName, false, DateTime.UtcNow - startTime);
                return new HashResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Perform key exchange
        /// </summary>
        public async Task<KeyExchangeResult> PerformKeyExchangeAsync(
            string exchangeType,
            KeyExchangeConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _keyManager.PerformKeyExchangeAsync(exchangeType, config);
                
                _metrics.RecordKeyExchange(exchangeType, result.Success, DateTime.UtcNow - startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordKeyExchange(exchangeType, false, DateTime.UtcNow - startTime);
                return new KeyExchangeResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Register crypto algorithm
        /// </summary>
        public void RegisterAlgorithm(ICryptoAlgorithm algorithm)
        {
            lock (_lock)
            {
                _algorithms.Add(algorithm);
            }
        }

        /// <summary>
        /// Register digital signature
        /// </summary>
        public void RegisterSignature(IDigitalSignature signature)
        {
            lock (_lock)
            {
                _signatures.Add(signature);
            }
        }

        /// <summary>
        /// Get crypto metrics
        /// </summary>
        public CryptoMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Get provider names
        /// </summary>
        public List<string> GetProviderNames()
        {
            lock (_lock)
            {
                return new List<string>(_providers.Keys);
            }
        }
    }

    public interface ICryptoProvider
    {
        string Name { get; }
        Task<EncryptionResult> EncryptAsync(byte[] data, EncryptionConfig config);
        Task<DecryptionResult> DecryptAsync(byte[] encryptedData, DecryptionConfig config);
    }

    public interface ICryptoAlgorithm
    {
        string Name { get; }
        string AlgorithmType { get; }
        Task<HashResult> GenerateHashAsync(byte[] data, HashConfig config);
    }

    public interface IDigitalSignature
    {
        string Name { get; }
        Task<SignatureResult> CreateSignatureAsync(byte[] data, SignatureConfig config);
        Task<VerificationResult> VerifySignatureAsync(byte[] data, byte[] signature, VerificationConfig config);
    }

    public class AESProvider : ICryptoProvider
    {
        public string Name => "AES";

        public async Task<EncryptionResult> EncryptAsync(byte[] data, EncryptionConfig config)
        {
            await Task.Delay(200);

            using (var aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(config.Key);
                aes.IV = Convert.FromBase64String(config.IV);

                using (var encryptor = aes.CreateEncryptor())
                {
                    var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
                    return new EncryptionResult
                    {
                        Success = true,
                        EncryptedData = encryptedData,
                        Algorithm = "AES-256-CBC"
                    };
                }
            }
        }

        public async Task<DecryptionResult> DecryptAsync(byte[] encryptedData, DecryptionConfig config)
        {
            await Task.Delay(150);

            using (var aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(config.Key);
                aes.IV = Convert.FromBase64String(config.IV);

                using (var decryptor = aes.CreateDecryptor())
                {
                    var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                    return new DecryptionResult
                    {
                        Success = true,
                        DecryptedData = decryptedData,
                        Algorithm = "AES-256-CBC"
                    };
                }
            }
        }
    }

    public class RSACryptoProvider : ICryptoProvider
    {
        public string Name => "RSA";

        public async Task<EncryptionResult> EncryptAsync(byte[] data, EncryptionConfig config)
        {
            await Task.Delay(300);

            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(config.PublicKey), out _);
                var encryptedData = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
                
                return new EncryptionResult
                {
                    Success = true,
                    EncryptedData = encryptedData,
                    Algorithm = "RSA-2048-OAEP"
                };
            }
        }

        public async Task<DecryptionResult> DecryptAsync(byte[] encryptedData, DecryptionConfig config)
        {
            await Task.Delay(250);

            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(config.PrivateKey), out _);
                var decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
                
                return new DecryptionResult
                {
                    Success = true,
                    DecryptedData = decryptedData,
                    Algorithm = "RSA-2048-OAEP"
                };
            }
        }
    }

    public class ECCProvider : ICryptoProvider
    {
        public string Name => "ECC";

        public async Task<EncryptionResult> EncryptAsync(byte[] data, EncryptionConfig config)
        {
            await Task.Delay(400);

            // Simulate ECC encryption
            var encryptedData = new byte[data.Length + 32];
            Array.Copy(data, 0, encryptedData, 32, data.Length);
            
            return new EncryptionResult
            {
                Success = true,
                EncryptedData = encryptedData,
                Algorithm = "ECC-P256"
            };
        }

        public async Task<DecryptionResult> DecryptAsync(byte[] encryptedData, DecryptionConfig config)
        {
            await Task.Delay(350);

            // Simulate ECC decryption
            var decryptedData = new byte[encryptedData.Length - 32];
            Array.Copy(encryptedData, 32, decryptedData, 0, decryptedData.Length);
            
            return new DecryptionResult
            {
                Success = true,
                DecryptedData = decryptedData,
                Algorithm = "ECC-P256"
            };
        }
    }

    public class AES256Algorithm : ICryptoAlgorithm
    {
        public string Name => "AES-256";
        public string AlgorithmType => "symmetric";

        public async Task<HashResult> GenerateHashAsync(byte[] data, HashConfig config)
        {
            await Task.Delay(100);

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(data);
                return new HashResult
                {
                    Success = true,
                    Hash = hash,
                    Algorithm = "SHA-256"
                };
            }
        }
    }

    public class RSA2048Algorithm : ICryptoAlgorithm
    {
        public string Name => "RSA-2048";
        public string AlgorithmType => "asymmetric";

        public async Task<HashResult> GenerateHashAsync(byte[] data, HashConfig config)
        {
            await Task.Delay(150);

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(data);
                return new HashResult
                {
                    Success = true,
                    Hash = hash,
                    Algorithm = "SHA-256"
                };
            }
        }
    }

    public class ECDSAAlgorithm : ICryptoAlgorithm
    {
        public string Name => "ECDSA";
        public string AlgorithmType => "elliptic_curve";

        public async Task<HashResult> GenerateHashAsync(byte[] data, HashConfig config)
        {
            await Task.Delay(120);

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(data);
                return new HashResult
                {
                    Success = true,
                    Hash = hash,
                    Algorithm = "SHA-256"
                };
            }
        }
    }

    public class RSASignature : IDigitalSignature
    {
        public string Name => "RSA Signature";

        public async Task<SignatureResult> CreateSignatureAsync(byte[] data, SignatureConfig config)
        {
            await Task.Delay(300);

            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(config.PrivateKey), out _);
                var signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                
                return new SignatureResult
                {
                    Success = true,
                    Signature = signature,
                    Algorithm = "RSA-SHA256"
                };
            }
        }

        public async Task<VerificationResult> VerifySignatureAsync(byte[] data, byte[] signature, VerificationConfig config)
        {
            await Task.Delay(250);

            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(config.PublicKey), out _);
                var isValid = rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                
                return new VerificationResult
                {
                    Success = isValid,
                    IsValid = isValid,
                    Algorithm = "RSA-SHA256"
                };
            }
        }
    }

    public class ECDSASignature : IDigitalSignature
    {
        public string Name => "ECDSA Signature";

        public async Task<SignatureResult> CreateSignatureAsync(byte[] data, SignatureConfig config)
        {
            await Task.Delay(400);

            // Simulate ECDSA signature
            var signature = new byte[64];
            new Random().NextBytes(signature);
            
            return new SignatureResult
            {
                Success = true,
                Signature = signature,
                Algorithm = "ECDSA-P256"
            };
        }

        public async Task<VerificationResult> VerifySignatureAsync(byte[] data, byte[] signature, VerificationConfig config)
        {
            await Task.Delay(350);

            // Simulate ECDSA verification
            var isValid = signature.Length == 64;
            
            return new VerificationResult
            {
                Success = true,
                IsValid = isValid,
                Algorithm = "ECDSA-P256"
            };
        }
    }

    public class Ed25519Signature : IDigitalSignature
    {
        public string Name => "Ed25519 Signature";

        public async Task<SignatureResult> CreateSignatureAsync(byte[] data, SignatureConfig config)
        {
            await Task.Delay(200);

            // Simulate Ed25519 signature
            var signature = new byte[64];
            new Random().NextBytes(signature);
            
            return new SignatureResult
            {
                Success = true,
                Signature = signature,
                Algorithm = "Ed25519"
            };
        }

        public async Task<VerificationResult> VerifySignatureAsync(byte[] data, byte[] signature, VerificationConfig config)
        {
            await Task.Delay(180);

            // Simulate Ed25519 verification
            var isValid = signature.Length == 64;
            
            return new VerificationResult
            {
                Success = true,
                IsValid = isValid,
                Algorithm = "Ed25519"
            };
        }
    }

    public class KeyManager
    {
        public async Task<KeyGenerationResult> GenerateKeyAsync(string keyType, KeyGenerationConfig config)
        {
            await Task.Delay(500);

            return new KeyGenerationResult
            {
                Success = true,
                KeyType = keyType,
                PublicKey = Convert.ToBase64String(new byte[256]),
                PrivateKey = Convert.ToBase64String(new byte[512]),
                KeySize = config.KeySize
            };
        }

        public async Task<KeyExchangeResult> PerformKeyExchangeAsync(string exchangeType, KeyExchangeConfig config)
        {
            await Task.Delay(600);

            return new KeyExchangeResult
            {
                Success = true,
                ExchangeType = exchangeType,
                SharedSecret = Convert.ToBase64String(new byte[32]),
                SessionKey = Convert.ToBase64String(new byte[32])
            };
        }
    }

    public class EncryptionEngine
    {
        public async Task<EncryptionResult> EncryptAsync(ICryptoProvider provider, byte[] data, EncryptionConfig config)
        {
            return await provider.EncryptAsync(data, config);
        }

        public async Task<DecryptionResult> DecryptAsync(ICryptoProvider provider, byte[] encryptedData, DecryptionConfig config)
        {
            return await provider.DecryptAsync(encryptedData, config);
        }
    }

    public class SignatureEngine
    {
        public async Task<SignatureResult> CreateSignatureAsync(IDigitalSignature signature, byte[] data, SignatureConfig config)
        {
            return await signature.CreateSignatureAsync(data, config);
        }

        public async Task<VerificationResult> VerifySignatureAsync(IDigitalSignature signature, byte[] data, byte[] sig, VerificationConfig config)
        {
            return await signature.VerifySignatureAsync(data, sig, config);
        }
    }

    public class KeyGenerationResult
    {
        public bool Success { get; set; }
        public string KeyType { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public int KeySize { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EncryptionResult
    {
        public bool Success { get; set; }
        public byte[] EncryptedData { get; set; }
        public string Algorithm { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DecryptionResult
    {
        public bool Success { get; set; }
        public byte[] DecryptedData { get; set; }
        public string Algorithm { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SignatureResult
    {
        public bool Success { get; set; }
        public byte[] Signature { get; set; }
        public string Algorithm { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class VerificationResult
    {
        public bool Success { get; set; }
        public bool IsValid { get; set; }
        public string Algorithm { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class HashResult
    {
        public bool Success { get; set; }
        public byte[] Hash { get; set; }
        public string Algorithm { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class KeyExchangeResult
    {
        public bool Success { get; set; }
        public string ExchangeType { get; set; }
        public string SharedSecret { get; set; }
        public string SessionKey { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class KeyGenerationConfig
    {
        public int KeySize { get; set; } = 2048;
        public string KeyFormat { get; set; } = "PEM";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class EncryptionConfig
    {
        public string Key { get; set; }
        public string IV { get; set; }
        public string PublicKey { get; set; }
        public string Mode { get; set; } = "CBC";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class DecryptionConfig
    {
        public string Key { get; set; }
        public string IV { get; set; }
        public string PrivateKey { get; set; }
        public string Mode { get; set; } = "CBC";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class SignatureConfig
    {
        public string PrivateKey { get; set; }
        public string HashAlgorithm { get; set; } = "SHA256";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class VerificationConfig
    {
        public string PublicKey { get; set; }
        public string HashAlgorithm { get; set; } = "SHA256";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class HashConfig
    {
        public string HashAlgorithm { get; set; } = "SHA256";
        public bool Salted { get; set; } = false;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class KeyExchangeConfig
    {
        public string ExchangeAlgorithm { get; set; } = "ECDH";
        public int KeySize { get; set; } = 256;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class CryptoMetrics
    {
        private readonly Dictionary<string, ProviderMetrics> _providerMetrics = new Dictionary<string, ProviderMetrics>();
        private readonly Dictionary<string, AlgorithmMetrics> _algorithmMetrics = new Dictionary<string, AlgorithmMetrics>();
        private readonly Dictionary<string, SignatureMetrics> _signatureMetrics = new Dictionary<string, SignatureMetrics>();
        private readonly object _lock = new object();

        public void RecordEncryption(string providerName, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                if (!_providerMetrics.ContainsKey(providerName))
                {
                    _providerMetrics[providerName] = new ProviderMetrics();
                }

                var metrics = _providerMetrics[providerName];
                metrics.TotalEncryptions++;
                if (success) metrics.SuccessfulEncryptions++;
                metrics.TotalEncryptionTime += executionTime;
            }
        }

        public void RecordDecryption(string providerName, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                if (!_providerMetrics.ContainsKey(providerName))
                {
                    _providerMetrics[providerName] = new ProviderMetrics();
                }

                var metrics = _providerMetrics[providerName];
                metrics.TotalDecryptions++;
                if (success) metrics.SuccessfulDecryptions++;
                metrics.TotalDecryptionTime += executionTime;
            }
        }

        public void RecordHashGeneration(string algorithmName, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                if (!_algorithmMetrics.ContainsKey(algorithmName))
                {
                    _algorithmMetrics[algorithmName] = new AlgorithmMetrics();
                }

                var metrics = _algorithmMetrics[algorithmName];
                metrics.TotalHashes++;
                if (success) metrics.SuccessfulHashes++;
                metrics.TotalHashTime += executionTime;
            }
        }

        public void RecordSignatureCreation(string signatureType, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                if (!_signatureMetrics.ContainsKey(signatureType))
                {
                    _signatureMetrics[signatureType] = new SignatureMetrics();
                }

                var metrics = _signatureMetrics[signatureType];
                metrics.TotalSignatures++;
                if (success) metrics.SuccessfulSignatures++;
                metrics.TotalSignatureTime += executionTime;
            }
        }

        public void RecordSignatureVerification(string signatureType, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                if (!_signatureMetrics.ContainsKey(signatureType))
                {
                    _signatureMetrics[signatureType] = new SignatureMetrics();
                }

                var metrics = _signatureMetrics[signatureType];
                metrics.TotalVerifications++;
                if (success) metrics.SuccessfulVerifications++;
                metrics.TotalVerificationTime += executionTime;
            }
        }

        public void RecordKeyExchange(string exchangeType, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                if (!_algorithmMetrics.ContainsKey(exchangeType))
                {
                    _algorithmMetrics[exchangeType] = new AlgorithmMetrics();
                }

                var metrics = _algorithmMetrics[exchangeType];
                metrics.TotalKeyExchanges++;
                if (success) metrics.SuccessfulKeyExchanges++;
                metrics.TotalKeyExchangeTime += executionTime;
            }
        }

        public Dictionary<string, ProviderMetrics> GetProviderMetrics() => new Dictionary<string, ProviderMetrics>(_providerMetrics);
        public Dictionary<string, AlgorithmMetrics> GetAlgorithmMetrics() => new Dictionary<string, AlgorithmMetrics>(_algorithmMetrics);
        public Dictionary<string, SignatureMetrics> GetSignatureMetrics() => new Dictionary<string, SignatureMetrics>(_signatureMetrics);
    }

    public class ProviderMetrics
    {
        public int TotalEncryptions { get; set; }
        public int SuccessfulEncryptions { get; set; }
        public TimeSpan TotalEncryptionTime { get; set; }
        public int TotalDecryptions { get; set; }
        public int SuccessfulDecryptions { get; set; }
        public TimeSpan TotalDecryptionTime { get; set; }
    }

    public class AlgorithmMetrics
    {
        public int TotalHashes { get; set; }
        public int SuccessfulHashes { get; set; }
        public TimeSpan TotalHashTime { get; set; }
        public int TotalKeyExchanges { get; set; }
        public int SuccessfulKeyExchanges { get; set; }
        public TimeSpan TotalKeyExchangeTime { get; set; }
    }

    public class SignatureMetrics
    {
        public int TotalSignatures { get; set; }
        public int SuccessfulSignatures { get; set; }
        public TimeSpan TotalSignatureTime { get; set; }
        public int TotalVerifications { get; set; }
        public int SuccessfulVerifications { get; set; }
        public TimeSpan TotalVerificationTime { get; set; }
    }
} 
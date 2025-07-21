using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Numerics;

namespace TuskLang.Configuration
{
    /// <summary>
    /// Client for interacting with Peanuts service
    /// </summary>
    public class PeanutsClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public PeanutsClient(string apiKey, string baseUrl = "https://peanuts.tuskt.sk")
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        /// <summary>
        /// Get configuration from Peanuts
        /// </summary>
        public async Task<PeanutsConfiguration> GetConfigurationAsync(string configId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/config/{configId}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PeanutsConfiguration>(content) ?? new PeanutsConfiguration();
            }
            catch (Exception ex)
            {
                throw new PeanutsException($"Failed to get configuration {configId}", ex);
            }
        }

        /// <summary>
        /// Save configuration to Peanuts
        /// </summary>
        public async Task<bool> SaveConfigurationAsync(string configId, PeanutsConfiguration configuration)
        {
            try
            {
                var json = JsonSerializer.Serialize(configuration);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/config/{configId}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new PeanutsException($"Failed to save configuration {configId}", ex);
            }
        }

        /// <summary>
        /// Delete configuration from Peanuts
        /// </summary>
        public async Task<bool> DeleteConfigurationAsync(string configId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/config/{configId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new PeanutsException($"Failed to delete configuration {configId}", ex);
            }
        }

        /// <summary>
        /// List all configurations
        /// </summary>
        public async Task<List<PeanutsConfigurationInfo>> ListConfigurationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/config");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PeanutsConfigurationInfo>>(content) ?? new List<PeanutsConfigurationInfo>();
            }
            catch (Exception ex)
            {
                throw new PeanutsException("Failed to list configurations", ex);
            }
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public async Task<PeanutsValidationResult> ValidateConfigurationAsync(string configId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/config/{configId}/validate", null);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PeanutsValidationResult>(content) ?? new PeanutsValidationResult();
            }
            catch (Exception ex)
            {
                throw new PeanutsException($"Failed to validate configuration {configId}", ex);
            }
        }

        /// <summary>
        /// Get configuration history
        /// </summary>
        public async Task<List<PeanutsConfigurationVersion>> GetConfigurationHistoryAsync(string configId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/config/{configId}/history");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PeanutsConfigurationVersion>>(content) ?? new List<PeanutsConfigurationVersion>();
            }
            catch (Exception ex)
            {
                throw new PeanutsException($"Failed to get configuration history for {configId}", ex);
            }
        }

        /// <summary>
        /// Restore configuration version
        /// </summary>
        public async Task<bool> RestoreConfigurationVersionAsync(string configId, string versionId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/config/{configId}/restore/{versionId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new PeanutsException($"Failed to restore configuration version {versionId} for {configId}", ex);
            }
        }

        #region Blockchain Operations

        /// <summary>
        /// Get balance for a wallet address
        /// </summary>
        public async Task<BalanceResult> GetBalanceAsync(string address)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/blockchain/balance/{address}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<BalanceResponse>(content);
                return new BalanceResult
                {
                    IsSuccess = true,
                    Balance = result?.Balance ?? 0m,
                    Currency = result?.Currency ?? "PEANUTS",
                    Address = address,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new BalanceResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to get balance for {address}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Estimate gas cost for a transfer operation
        /// </summary>
        public async Task<TransferResult> EstimateTransferGasAsync(string from, string to, decimal amount, string gasPrice = null)
        {
            try
            {
                var request = new
                {
                    from = from,
                    to = to,
                    amount = amount,
                    gasPrice = gasPrice,
                    operation = "transfer"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/estimate-gas", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GasEstimateResponse>(responseContent);
                return new TransferResult
                {
                    IsSuccess = true,
                    GasEstimate = result?.GasEstimate ?? 0.001m,
                    GasPrice = gasPrice ?? "20",
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new TransferResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to estimate transfer gas from {from} to {to}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Execute a transfer operation
        /// </summary>
        public async Task<TransferResult> TransferAsync(string from, string to, decimal amount, string gasPrice = null, bool estimate = false, string privateKey = null)
        {
            try
            {
                if (estimate)
                {
                    return await EstimateTransferGasAsync(from, to, amount, gasPrice);
                }

                var request = new
                {
                    from = from,
                    to = to,
                    amount = amount,
                    gasPrice = gasPrice,
                    privateKey = privateKey,
                    operation = "transfer"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/transfer", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TransferResponse>(responseContent);
                return new TransferResult
                {
                    IsSuccess = result?.Success ?? false,
                    TransactionHash = result?.TransactionHash ?? string.Empty,
                    GasUsed = result?.GasUsed ?? 0m,
                    TotalCost = (result?.GasUsed ?? 0m) * (decimal.Parse(gasPrice ?? "20") / 1000000000m),
                    ErrorMessage = result?.Success == false ? "Transfer failed" : string.Empty
                };
            }
            catch (Exception ex)
            {
                return new TransferResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to transfer {amount} from {from} to {to}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Estimate gas cost for minting tokens
        /// </summary>
        public async Task<MintResult> EstimateMintGasAsync(decimal amount, string gasPrice = null)
        {
            try
            {
                var request = new
                {
                    amount = amount,
                    gasPrice = gasPrice,
                    operation = "mint"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/estimate-gas", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GasEstimateResponse>(responseContent);
                return new MintResult
                {
                    IsSuccess = true,
                    GasEstimate = result?.GasEstimate ?? 0.002m,
                    GasPrice = gasPrice ?? "20",
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new MintResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to estimate mint gas for {amount}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Mint new tokens
        /// </summary>
        public async Task<MintResult> MintAsync(string to, decimal amount, string gasPrice = null, bool estimate = false, string privateKey = null)
        {
            try
            {
                if (estimate)
                {
                    return await EstimateMintGasAsync(amount, gasPrice);
                }

                var request = new
                {
                    to = to,
                    amount = amount,
                    gasPrice = gasPrice,
                    privateKey = privateKey,
                    operation = "mint"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/mint", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MintResponse>(responseContent);
                return new MintResult
                {
                    IsSuccess = result?.Success ?? false,
                    TransactionHash = result?.TransactionHash ?? string.Empty,
                    GasUsed = result?.GasUsed ?? 0m,
                    ErrorMessage = result?.Success == false ? "Minting failed" : string.Empty
                };
            }
            catch (Exception ex)
            {
                return new MintResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to mint {amount} tokens: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Estimate gas cost for burning tokens
        /// </summary>
        public async Task<BurnResult> EstimateBurnGasAsync(decimal amount, string gasPrice = null)
        {
            try
            {
                var request = new
                {
                    amount = amount,
                    gasPrice = gasPrice,
                    operation = "burn"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/estimate-gas", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GasEstimateResponse>(responseContent);
                return new BurnResult
                {
                    IsSuccess = true,
                    GasEstimate = result?.GasEstimate ?? 0.0015m,
                    GasPrice = gasPrice ?? "20",
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new BurnResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to estimate burn gas for {amount}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Burn tokens
        /// </summary>
        public async Task<BurnResult> BurnAsync(string from, decimal amount, string gasPrice = null, bool estimate = false, string privateKey = null)
        {
            try
            {
                if (estimate)
                {
                    return await EstimateBurnGasAsync(amount, gasPrice);
                }

                var request = new
                {
                    from = from,
                    amount = amount,
                    gasPrice = gasPrice,
                    privateKey = privateKey,
                    operation = "burn"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/burn", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<BurnResponse>(responseContent);
                return new BurnResult
                {
                    IsSuccess = result?.Success ?? false,
                    TransactionHash = result?.TransactionHash ?? string.Empty,
                    GasUsed = result?.GasUsed ?? 0m,
                    ErrorMessage = result?.Success == false ? "Burning failed" : string.Empty
                };
            }
            catch (Exception ex)
            {
                return new BurnResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to burn {amount} tokens: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Estimate gas cost for staking tokens
        /// </summary>
        public async Task<StakeResult> EstimateStakeGasAsync(decimal amount, string gasPrice = null)
        {
            try
            {
                var request = new
                {
                    amount = amount,
                    gasPrice = gasPrice,
                    operation = "stake"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/estimate-gas", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GasEstimateResponse>(responseContent);
                return new StakeResult
                {
                    IsSuccess = true,
                    GasEstimate = result?.GasEstimate ?? 0.003m,
                    GasPrice = gasPrice ?? "20",
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new StakeResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to estimate stake gas for {amount}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Stake tokens
        /// </summary>
        public async Task<StakeResult> StakeAsync(string address, decimal amount, string gasPrice = null, bool estimate = false, string privateKey = null)
        {
            try
            {
                if (estimate)
                {
                    return await EstimateStakeGasAsync(amount, gasPrice);
                }

                var request = new
                {
                    address = address,
                    amount = amount,
                    gasPrice = gasPrice,
                    privateKey = privateKey,
                    operation = "stake"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/stake", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<StakeResponse>(responseContent);
                return new StakeResult
                {
                    IsSuccess = result?.Success ?? false,
                    TransactionHash = result?.TransactionHash ?? string.Empty,
                    GasUsed = result?.GasUsed ?? 0m,
                    ErrorMessage = result?.Success == false ? "Staking failed" : string.Empty
                };
            }
            catch (Exception ex)
            {
                return new StakeResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to stake {amount} tokens: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Estimate gas cost for unstaking tokens
        /// </summary>
        public async Task<UnstakeResult> EstimateUnstakeGasAsync(decimal amount, string gasPrice = null)
        {
            try
            {
                var request = new
                {
                    amount = amount,
                    gasPrice = gasPrice,
                    operation = "unstake"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/estimate-gas", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GasEstimateResponse>(responseContent);
                return new UnstakeResult
                {
                    IsSuccess = true,
                    GasEstimate = result?.GasEstimate ?? 0.0025m,
                    GasPrice = gasPrice ?? "20",
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new UnstakeResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to estimate unstake gas for {amount}: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Unstake tokens
        /// </summary>
        public async Task<UnstakeResult> UnstakeAsync(string address, decimal amount, string gasPrice = null, bool estimate = false, string privateKey = null)
        {
            try
            {
                if (estimate)
                {
                    return await EstimateUnstakeGasAsync(amount, gasPrice);
                }

                var request = new
                {
                    address = address,
                    amount = amount,
                    gasPrice = gasPrice,
                    privateKey = privateKey,
                    operation = "unstake"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/unstake", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<UnstakeResponse>(responseContent);
                return new UnstakeResult
                {
                    IsSuccess = result?.Success ?? false,
                    TransactionHash = result?.TransactionHash ?? string.Empty,
                    GasUsed = result?.GasUsed ?? 0m,
                    ErrorMessage = result?.Success == false ? "Unstaking failed" : string.Empty
                };
            }
            catch (Exception ex)
            {
                return new UnstakeResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to unstake {amount} tokens: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get staking rewards
        /// </summary>
        public async Task<RewardsResult> GetRewardsAsync(string address = null)
        {
            try
            {
                var url = string.IsNullOrEmpty(address) 
                    ? $"{_baseUrl}/api/blockchain/rewards"
                    : $"{_baseUrl}/api/blockchain/rewards/{address}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RewardsResponse>(content);
                return new RewardsResult
                {
                    IsSuccess = true,
                    Rewards = result?.Rewards ?? 0m,
                    Currency = result?.Currency ?? "ETH",
                    LastRewardDate = result?.LastRewardDate ?? DateTime.UtcNow,
                    TotalStaked = result?.TotalStaked ?? 0m,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new RewardsResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to get rewards: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get transaction history
        /// </summary>
        public async Task<HistoryResult> GetTransactionHistoryAsync(string address = null)
        {
            try
            {
                var url = string.IsNullOrEmpty(address) 
                    ? $"{_baseUrl}/api/blockchain/transactions"
                    : $"{_baseUrl}/api/blockchain/transactions/{address}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var transactions = JsonSerializer.Deserialize<List<TransactionInfo>>(content) ?? new List<TransactionInfo>();
                return new HistoryResult
                {
                    IsSuccess = true,
                    Transactions = transactions,
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new HistoryResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to get transaction history: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get transaction history (alias for GetTransactionHistoryAsync)
        /// </summary>
        public async Task<HistoryResult> GetHistoryAsync(string address = null)
        {
            return await GetTransactionHistoryAsync(address);
        }

        /// <summary>
        /// Estimate gas cost for contract deployment
        /// </summary>
        public async Task<DeployResult> EstimateDeployGasAsync(string contract, string gasPrice = null)
        {
            try
            {
                var request = new
                {
                    contract = contract,
                    gasPrice = gasPrice,
                    operation = "deploy"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/estimate-gas", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GasEstimateResponse>(responseContent);
                return new DeployResult
                {
                    IsSuccess = true,
                    GasEstimate = result?.GasEstimate ?? 0.05m,
                    GasPrice = gasPrice ?? "20",
                    ErrorMessage = string.Empty
                };
            }
            catch (Exception ex)
            {
                return new DeployResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to estimate deploy gas: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Deploy a smart contract
        /// </summary>
        public async Task<DeployResult> DeployContractAsync(string contract, string gasPrice = null, bool estimate = false, string privateKey = null)
        {
            try
            {
                if (estimate)
                {
                    return await EstimateDeployGasAsync(contract, gasPrice);
                }

                var request = new
                {
                    contract = contract,
                    gasPrice = gasPrice,
                    privateKey = privateKey,
                    operation = "deploy"
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/blockchain/deploy", content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DeployResponse>(responseContent);
                return new DeployResult
                {
                    IsSuccess = result?.Success ?? false,
                    TransactionHash = result?.TransactionHash ?? string.Empty,
                    ContractAddress = result?.ContractAddress ?? string.Empty,
                    GasUsed = result?.GasUsed ?? 0m,
                    ErrorMessage = result?.Success == false ? "Deployment failed" : string.Empty
                };
            }
            catch (Exception ex)
            {
                return new DeployResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to deploy contract: {ex.Message}"
                };
            }
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Peanuts configuration
    /// </summary>
    public class PeanutsConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Peanuts configuration info
    /// </summary>
    public class PeanutsConfigurationInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Size { get; set; }
    }

    /// <summary>
    /// Peanuts validation result
    /// </summary>
    public class PeanutsValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Peanuts configuration version
    /// </summary>
    public class PeanutsConfigurationVersion
    {
        public string VersionId { get; set; } = string.Empty;
        public string ConfigId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Peanuts exception
    /// </summary>
    public class PeanutsException : Exception
    {
        public PeanutsException(string message) : base(message) { }
        public PeanutsException(string message, Exception innerException) : base(message, innerException) { }
    }

    #region Blockchain Response Classes

    /// <summary>
    /// Gas estimation response
    /// </summary>
    public class GasEstimateResponse
    {
        public decimal GasEstimate { get; set; }
        public string Currency { get; set; } = "ETH";
        public string TransactionHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Transfer response
    /// </summary>
    public class TransferResponse
    {
        public bool Success { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Mint response
    /// </summary>
    public class MintResponse
    {
        public bool Success { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal Amount { get; set; }
        public string Recipient { get; set; } = string.Empty;
    }

    /// <summary>
    /// Burn response
    /// </summary>
    public class BurnResponse
    {
        public bool Success { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal Amount { get; set; }
        public string From { get; set; } = string.Empty;
    }

    /// <summary>
    /// Stake response
    /// </summary>
    public class StakeResponse
    {
        public bool Success { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal Amount { get; set; }
        public string Staker { get; set; } = string.Empty;
        public DateTime StakedAt { get; set; }
    }

    /// <summary>
    /// Unstake response
    /// </summary>
    public class UnstakeResponse
    {
        public bool Success { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal Amount { get; set; }
        public string Staker { get; set; } = string.Empty;
        public DateTime UnstakedAt { get; set; }
    }

    /// <summary>
    /// Rewards response
    /// </summary>
    public class RewardsResponse
    {
        public decimal Rewards { get; set; }
        public string Currency { get; set; } = "ETH";
        public DateTime LastRewardDate { get; set; }
        public decimal TotalStaked { get; set; }
    }

    /// <summary>
    /// Deploy response
    /// </summary>
    public class DeployResponse
    {
        public bool Success { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public string ContractName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Transaction information
    /// </summary>
    public class TransactionInfo
    {
        public string TransactionHash { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal GasUsed { get; set; }
        public decimal GasPrice { get; set; }
        public string Operation { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string BlockNumber { get; set; } = string.Empty;
    }

    #endregion

    #region Blockchain Result Classes

    /// <summary>
    /// Transfer operation result
    /// </summary>
    public class TransferResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal GasEstimate { get; set; }
        public string GasPrice { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mint operation result
    /// </summary>
    public class MintResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal GasEstimate { get; set; }
        public string GasPrice { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Burn operation result
    /// </summary>
    public class BurnResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal GasEstimate { get; set; }
        public string GasPrice { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Stake operation result
    /// </summary>
    public class StakeResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal GasEstimate { get; set; }
        public string GasPrice { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal Rewards { get; set; }
    }

    /// <summary>
    /// Unstake operation result
    /// </summary>
    public class UnstakeResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal GasEstimate { get; set; }
        public string GasPrice { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal RewardsClaimed { get; set; }
    }

    /// <summary>
    /// Rewards operation result
    /// </summary>
    public class RewardsResult
    {
        public bool IsSuccess { get; set; }
        public decimal Rewards { get; set; }
        public string Currency { get; set; } = "ETH";
        public DateTime LastRewardDate { get; set; }
        public decimal TotalStaked { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal AvailableRewards { get; set; }
        public decimal Apy { get; set; }
        public DateTime NextReward { get; set; }
    }

    /// <summary>
    /// History operation result
    /// </summary>
    public class HistoryResult
    {
        public bool IsSuccess { get; set; }
        public List<TransactionInfo> Transactions { get; set; } = new List<TransactionInfo>();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Deploy operation result
    /// </summary>
    public class DeployResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public decimal GasUsed { get; set; }
        public decimal GasEstimate { get; set; }
        public string GasPrice { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    #endregion

    /// <summary>
    /// Balance response
    /// </summary>
    public class BalanceResponse
    {
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "PEANUTS";
        public string Address { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Balance operation result
    /// </summary>
    public class BalanceResult
    {
        public bool IsSuccess { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "PEANUTS";
        public string Address { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal UsdValue { get; set; }
        public decimal Change24h { get; set; }
    }
} 
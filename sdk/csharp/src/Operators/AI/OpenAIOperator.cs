using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Linq;

namespace TuskLang.Operators
{
    /// <summary>
    /// OpenAI API operator for TuskTsk
    /// Provides comprehensive OpenAI integration with real API calls
    /// </summary>
    public class OpenAIOperator : BaseOperator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIOperator> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public OpenAIOperator(string apiKey, ILogger<OpenAIOperator> logger = null, HttpClient httpClient = null)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAIOperator>.Instance;
            _httpClient = httpClient ?? new HttpClient();
            _baseUrl = "https://api.openai.com/v1";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TuskTsk-OpenAI-Operator/1.0");
        }

        #region Chat Completions

        /// <summary>
        /// Create chat completion
        /// </summary>
        public async Task<ChatCompletionResponse> CreateChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Chat completion created: model={request.Model}, tokens={result?.Usage?.TotalTokens}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating chat completion with model: {request.Model}");
                throw;
            }
        }

        /// <summary>
        /// Create simple chat completion
        /// </summary>
        public async Task<string> CreateSimpleChatCompletionAsync(string message, string model = "gpt-3.5-turbo", 
            double temperature = 0.7, int maxTokens = 1000, CancellationToken cancellationToken = default)
        {
            var request = new ChatCompletionRequest
            {
                Model = model,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "user", Content = message }
                },
                Temperature = temperature,
                MaxTokens = maxTokens
            };

            var response = await CreateChatCompletionAsync(request, cancellationToken);
            return response?.Choices?.FirstOrDefault()?.Message?.Content;
        }

        /// <summary>
        /// Create streaming chat completion
        /// </summary>
        public async IAsyncEnumerable<ChatCompletionChunk> CreateStreamingChatCompletionAsync(ChatCompletionRequest request, 
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                request.Stream = true;
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new System.IO.StreamReader(stream);
                
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);
                        if (data == "[DONE]") break;
                        
                        var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, _jsonOptions);
                        if (chunk != null)
                        {
                            yield return chunk;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating streaming chat completion with model: {request.Model}");
                throw;
            }
        }

        #endregion

        #region Completions

        /// <summary>
        /// Create text completion
        /// </summary>
        public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/completions", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<CompletionResponse>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Text completion created: model={request.Model}, tokens={result?.Usage?.TotalTokens}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating text completion with model: {request.Model}");
                throw;
            }
        }

        /// <summary>
        /// Create simple text completion
        /// </summary>
        public async Task<string> CreateSimpleCompletionAsync(string prompt, string model = "text-davinci-003", 
            double temperature = 0.7, int maxTokens = 1000, CancellationToken cancellationToken = default)
        {
            var request = new CompletionRequest
            {
                Model = model,
                Prompt = prompt,
                Temperature = temperature,
                MaxTokens = maxTokens
            };

            var response = await CreateCompletionAsync(request, cancellationToken);
            return response?.Choices?.FirstOrDefault()?.Text?.Trim();
        }

        #endregion

        #region Embeddings

        /// <summary>
        /// Create embeddings
        /// </summary>
        public async Task<EmbeddingResponse> CreateEmbeddingsAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/embeddings", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Embeddings created: model={request.Model}, count={result?.Data?.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating embeddings with model: {request.Model}");
                throw;
            }
        }

        /// <summary>
        /// Create simple embeddings
        /// </summary>
        public async Task<List<float>> CreateSimpleEmbeddingsAsync(string text, string model = "text-embedding-ada-002", 
            CancellationToken cancellationToken = default)
        {
            var request = new EmbeddingRequest
            {
                Model = model,
                Input = text
            };

            var response = await CreateEmbeddingsAsync(request, cancellationToken);
            return response?.Data?.FirstOrDefault()?.Embedding;
        }

        /// <summary>
        /// Create embeddings for multiple texts
        /// </summary>
        public async Task<List<List<float>>> CreateMultipleEmbeddingsAsync(List<string> texts, string model = "text-embedding-ada-002", 
            CancellationToken cancellationToken = default)
        {
            var request = new EmbeddingRequest
            {
                Model = model,
                Input = texts
            };

            var response = await CreateEmbeddingsAsync(request, cancellationToken);
            return response?.Data?.Select(d => d.Embedding).ToList();
        }

        #endregion

        #region Fine-tuning

        /// <summary>
        /// Create fine-tuning job
        /// </summary>
        public async Task<FineTuningJob> CreateFineTuningJobAsync(FineTuningJobRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/fine_tuning/jobs", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FineTuningJob>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Fine-tuning job created: id={result?.Id}, model={result?.Model}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating fine-tuning job with model: {request.Model}");
                throw;
            }
        }

        /// <summary>
        /// List fine-tuning jobs
        /// </summary>
        public async Task<FineTuningJobList> ListFineTuningJobsAsync(int? limit = null, string after = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var queryParams = new List<string>();
                if (limit.HasValue) queryParams.Add($"limit={limit.Value}");
                if (!string.IsNullOrEmpty(after)) queryParams.Add($"after={after}");
                
                var url = $"{_baseUrl}/fine_tuning/jobs";
                if (queryParams.Any()) url += "?" + string.Join("&", queryParams);
                
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FineTuningJobList>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Fine-tuning jobs listed: count={result?.Data?.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing fine-tuning jobs");
                throw;
            }
        }

        /// <summary>
        /// Get fine-tuning job
        /// </summary>
        public async Task<FineTuningJob> GetFineTuningJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/fine_tuning/jobs/{jobId}", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FineTuningJob>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Fine-tuning job retrieved: id={result?.Id}, status={result?.Status}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting fine-tuning job: {jobId}");
                throw;
            }
        }

        /// <summary>
        /// Cancel fine-tuning job
        /// </summary>
        public async Task<FineTuningJob> CancelFineTuningJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/fine_tuning/jobs/{jobId}/cancel", null, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FineTuningJob>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Fine-tuning job cancelled: id={result?.Id}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling fine-tuning job: {jobId}");
                throw;
            }
        }

        #endregion

        #region Models

        /// <summary>
        /// List models
        /// </summary>
        public async Task<ModelList> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/models", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ModelList>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Models listed: count={result?.Data?.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                throw;
            }
        }

        /// <summary>
        /// Get model
        /// </summary>
        public async Task<Model> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/models/{modelId}", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<Model>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Model retrieved: id={result?.Id}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting model: {modelId}");
                throw;
            }
        }

        #endregion

        #region Files

        /// <summary>
        /// Upload file
        /// </summary>
        public async Task<FileInfo> UploadFileAsync(string filePath, string purpose, CancellationToken cancellationToken = default)
        {
            try
            {
                using var fileStream = System.IO.File.OpenRead(filePath);
                using var formData = new MultipartFormDataContent();
                using var fileContent = new StreamContent(fileStream);
                
                formData.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));
                formData.Add(new StringContent(purpose), "purpose");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/files", formData, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FileInfo>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"File uploaded: id={result?.Id}, filename={result?.Filename}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// List files
        /// </summary>
        public async Task<FileList> ListFilesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/files", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FileList>(responseContent, _jsonOptions);
                
                _logger.LogDebug($"Files listed: count={result?.Data?.Count}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                throw;
            }
        }

        /// <summary>
        /// Delete file
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/files/{fileId}", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                _logger.LogDebug($"File deleted: id={fileId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {fileId}");
                throw;
            }
        }

        #endregion

        #region Request/Response Models

        public class ChatCompletionRequest
        {
            public string Model { get; set; }
            public List<ChatMessage> Messages { get; set; }
            public double? Temperature { get; set; }
            public int? MaxTokens { get; set; }
            public bool? Stream { get; set; }
        }

        public class ChatMessage
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        public class ChatCompletionResponse
        {
            public string Id { get; set; }
            public string Object { get; set; }
            public long Created { get; set; }
            public string Model { get; set; }
            public List<ChatChoice> Choices { get; set; }
            public Usage Usage { get; set; }
        }

        public class ChatChoice
        {
            public int Index { get; set; }
            public ChatMessage Message { get; set; }
            public string FinishReason { get; set; }
        }

        public class ChatCompletionChunk
        {
            public string Id { get; set; }
            public string Object { get; set; }
            public long Created { get; set; }
            public string Model { get; set; }
            public List<ChatChoiceDelta> Choices { get; set; }
        }

        public class ChatChoiceDelta
        {
            public int Index { get; set; }
            public ChatMessage Delta { get; set; }
            public string FinishReason { get; set; }
        }

        public class CompletionRequest
        {
            public string Model { get; set; }
            public string Prompt { get; set; }
            public double? Temperature { get; set; }
            public int? MaxTokens { get; set; }
        }

        public class CompletionResponse
        {
            public string Id { get; set; }
            public string Object { get; set; }
            public long Created { get; set; }
            public string Model { get; set; }
            public List<CompletionChoice> Choices { get; set; }
            public Usage Usage { get; set; }
        }

        public class CompletionChoice
        {
            public string Text { get; set; }
            public int Index { get; set; }
            public string FinishReason { get; set; }
        }

        public class EmbeddingRequest
        {
            public string Model { get; set; }
            public object Input { get; set; } // string or List<string>
        }

        public class EmbeddingResponse
        {
            public string Object { get; set; }
            public List<EmbeddingData> Data { get; set; }
            public string Model { get; set; }
            public Usage Usage { get; set; }
        }

        public class EmbeddingData
        {
            public string Object { get; set; }
            public List<float> Embedding { get; set; }
            public int Index { get; set; }
        }

        public class FineTuningJobRequest
        {
            public string Model { get; set; }
            public string TrainingFile { get; set; }
            public string ValidationFile { get; set; }
            public Dictionary<string, object> Hyperparameters { get; set; }
            public string Suffix { get; set; }
        }

        public class FineTuningJob
        {
            public string Id { get; set; }
            public string Object { get; set; }
            public long CreatedAt { get; set; }
            public string Model { get; set; }
            public string Status { get; set; }
            public string TrainingFile { get; set; }
            public string ValidationFile { get; set; }
            public string FineTunedModel { get; set; }
            public Dictionary<string, object> Hyperparameters { get; set; }
            public string Suffix { get; set; }
        }

        public class FineTuningJobList
        {
            public string Object { get; set; }
            public List<FineTuningJob> Data { get; set; }
        }

        public class Model
        {
            public string Id { get; set; }
            public string Object { get; set; }
            public long Created { get; set; }
            public string OwnedBy { get; set; }
        }

        public class ModelList
        {
            public string Object { get; set; }
            public List<Model> Data { get; set; }
        }

        public class FileInfo
        {
            public string Id { get; set; }
            public string Object { get; set; }
            public long CreatedAt { get; set; }
            public string Filename { get; set; }
            public string Purpose { get; set; }
            public long Bytes { get; set; }
        }

        public class FileList
        {
            public string Object { get; set; }
            public List<FileInfo> Data { get; set; }
        }

        public class Usage
        {
            public int PromptTokens { get; set; }
            public int CompletionTokens { get; set; }
            public int TotalTokens { get; set; }
        }

        #endregion
    }
} 
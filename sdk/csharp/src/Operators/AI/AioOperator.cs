using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Linq;

namespace TuskLang.Operators.AI
{
    /// <summary>
    /// AI operator for TuskLang
    /// Provides AI operations including model inference, training, and management
    /// </summary>
    public class AioOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _endpoint;
        private string _model;

        public AioOperator() : base("aio", "AI operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to AI service", new[] { "api_key", "endpoint", "model" });
            RegisterMethod("disconnect", "Disconnect from AI service", new string[0]);
            RegisterMethod("generate", "Generate AI response", new[] { "prompt", "model", "max_tokens", "temperature" });
            RegisterMethod("chat", "Chat with AI", new[] { "messages", "model", "max_tokens", "temperature" });
            RegisterMethod("complete", "Complete text", new[] { "text", "model", "max_tokens" });
            RegisterMethod("classify", "Classify text", new[] { "text", "labels", "model" });
            RegisterMethod("summarize", "Summarize text", new[] { "text", "max_length", "model" });
            RegisterMethod("translate", "Translate text", new[] { "text", "source_lang", "target_lang", "model" });
            RegisterMethod("embed", "Generate embeddings", new[] { "text", "model" });
            RegisterMethod("similarity", "Calculate similarity", new[] { "text1", "text2", "model" });
            RegisterMethod("extract", "Extract information", new[] { "text", "entities", "model" });
            RegisterMethod("sentiment", "Analyze sentiment", new[] { "text", "model" });
            RegisterMethod("keywords", "Extract keywords", new[] { "text", "count", "model" });
            RegisterMethod("generate_image", "Generate image", new[] { "prompt", "size", "model" });
            RegisterMethod("analyze_image", "Analyze image", new[] { "image_url", "model" });
            RegisterMethod("list_models", "List available models", new string[0]);
            RegisterMethod("get_model_info", "Get model information", new[] { "model" });
            RegisterMethod("fine_tune", "Fine-tune model", new[] { "model", "training_data", "parameters" });
            RegisterMethod("get_usage", "Get API usage", new string[0]);
            RegisterMethod("test_connection", "Test AI connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing AI operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "generate":
                        return await GenerateAsync(parameters);
                    case "chat":
                        return await ChatAsync(parameters);
                    case "complete":
                        return await CompleteAsync(parameters);
                    case "classify":
                        return await ClassifyAsync(parameters);
                    case "summarize":
                        return await SummarizeAsync(parameters);
                    case "translate":
                        return await TranslateAsync(parameters);
                    case "embed":
                        return await EmbedAsync(parameters);
                    case "similarity":
                        return await SimilarityAsync(parameters);
                    case "extract":
                        return await ExtractAsync(parameters);
                    case "sentiment":
                        return await SentimentAsync(parameters);
                    case "keywords":
                        return await KeywordsAsync(parameters);
                    case "generate_image":
                        return await GenerateImageAsync(parameters);
                    case "analyze_image":
                        return await AnalyzeImageAsync(parameters);
                    case "list_models":
                        return await ListModelsAsync();
                    case "get_model_info":
                        return await GetModelInfoAsync(parameters);
                    case "fine_tune":
                        return await FineTuneAsync(parameters);
                    case "get_usage":
                        return await GetUsageAsync();
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown AI method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing AI method {method}: {ex.Message}");
                throw new OperatorException($"AI operation failed: {ex.Message}", "AI_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var apiKey = GetRequiredParameter<string>(parameters, "api_key");
            var endpoint = GetParameter<string>(parameters, "endpoint", "https://api.openai.com/v1");
            var model = GetParameter<string>(parameters, "model", "gpt-3.5-turbo");

            try
            {
                _apiKey = apiKey;
                _endpoint = endpoint;
                _model = model;

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                LogInfo($"Connected to AI service: {endpoint}");
                return new { success = true, endpoint, model };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to AI service: {ex.Message}");
                throw new OperatorException($"AI connection failed: {ex.Message}", "AI_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _apiKey = null;
                _endpoint = null;
                _model = null;

                LogInfo("Disconnected from AI service");
                return new { success = true, message = "Disconnected from AI service" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from AI service: {ex.Message}");
                throw new OperatorException($"AI disconnect failed: {ex.Message}", "AI_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> GenerateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var prompt = GetRequiredParameter<string>(parameters, "prompt");
            var model = GetParameter<string>(parameters, "model", _model);
            var maxTokens = GetParameter<int>(parameters, "max_tokens", 100);
            var temperature = GetParameter<double>(parameters, "temperature", 0.7);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "model", model },
                    { "prompt", prompt },
                    { "max_tokens", maxTokens },
                    { "temperature", temperature }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/completions";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Generate failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully generated AI response");
                return new { success = true, prompt, model, response = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error generating AI response: {ex.Message}");
                throw new OperatorException($"AI generate failed: {ex.Message}", "AI_GENERATE_ERROR", ex);
            }
        }

        private async Task<object> ChatAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var messages = GetRequiredParameter<object[]>(parameters, "messages");
            var model = GetParameter<string>(parameters, "model", _model);
            var maxTokens = GetParameter<int>(parameters, "max_tokens", 100);
            var temperature = GetParameter<double>(parameters, "temperature", 0.7);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "model", model },
                    { "messages", messages },
                    { "max_tokens", maxTokens },
                    { "temperature", temperature }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/chat/completions";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Chat failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully completed AI chat");
                return new { success = true, messages, model, response = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error in AI chat: {ex.Message}");
                throw new OperatorException($"AI chat failed: {ex.Message}", "AI_CHAT_ERROR", ex);
            }
        }

        private async Task<object> CompleteAsync(Dictionary<string, object> parameters)
        {
            var text = GetRequiredParameter<string>(parameters, "text");
            var model = GetParameter<string>(parameters, "model", _model);
            var maxTokens = GetParameter<int>(parameters, "max_tokens", 50);

            try
            {
                return await GenerateAsync(new Dictionary<string, object>
                {
                    { "prompt", text },
                    { "model", model },
                    { "max_tokens", maxTokens }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error completing text: {ex.Message}");
                throw new OperatorException($"AI complete failed: {ex.Message}", "AI_COMPLETE_ERROR", ex);
            }
        }

        private async Task<object> ClassifyAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var labels = GetRequiredParameter<string[]>(parameters, "labels");
            var model = GetParameter<string>(parameters, "model", _model);

            try
            {
                var prompt = $"Classify the following text into one of these categories: {string.Join(", ", labels)}\n\nText: {text}\n\nCategory:";
                
                var result = await GenerateAsync(new Dictionary<string, object>
                {
                    { "prompt", prompt },
                    { "model", model },
                    { "max_tokens", 10 },
                    { "temperature", 0.1 }
                });

                return new { success = true, text, labels, classification = result };
            }
            catch (Exception ex)
            {
                LogError($"Error classifying text: {ex.Message}");
                throw new OperatorException($"AI classify failed: {ex.Message}", "AI_CLASSIFY_ERROR", ex);
            }
        }

        private async Task<object> SummarizeAsync(Dictionary<string, object> parameters)
        {
            var text = GetRequiredParameter<string>(parameters, "text");
            var maxLength = GetParameter<int>(parameters, "max_length", 100);
            var model = GetParameter<string>(parameters, "model", _model);

            try
            {
                var prompt = $"Summarize the following text in {maxLength} words or less:\n\n{text}\n\nSummary:";
                
                var result = await GenerateAsync(new Dictionary<string, object>
                {
                    { "prompt", prompt },
                    { "model", model },
                    { "max_tokens", maxLength * 2 },
                    { "temperature", 0.3 }
                });

                return new { success = true, text, maxLength, summary = result };
            }
            catch (Exception ex)
            {
                LogError($"Error summarizing text: {ex.Message}");
                throw new OperatorException($"AI summarize failed: {ex.Message}", "AI_SUMMARIZE_ERROR", ex);
            }
        }

        private async Task<object> TranslateAsync(Dictionary<string, object> parameters)
        {
            var text = GetRequiredParameter<string>(parameters, "text");
            var sourceLang = GetParameter<string>(parameters, "source_lang", "auto");
            var targetLang = GetRequiredParameter<string>(parameters, "target_lang");
            var model = GetParameter<string>(parameters, "model", _model);

            try
            {
                var prompt = $"Translate the following text from {sourceLang} to {targetLang}:\n\n{text}\n\nTranslation:";
                
                var result = await GenerateAsync(new Dictionary<string, object>
                {
                    { "prompt", prompt },
                    { "model", model },
                    { "max_tokens", text.Length * 2 },
                    { "temperature", 0.1 }
                });

                return new { success = true, text, sourceLang, targetLang, translation = result };
            }
            catch (Exception ex)
            {
                LogError($"Error translating text: {ex.Message}");
                throw new OperatorException($"AI translate failed: {ex.Message}", "AI_TRANSLATE_ERROR", ex);
            }
        }

        private async Task<object> EmbedAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var model = GetParameter<string>(parameters, "model", "text-embedding-ada-002");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "model", model },
                    { "input", text }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/embeddings";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Embed failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully generated embeddings");
                return new { success = true, text, model, embeddings = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error generating embeddings: {ex.Message}");
                throw new OperatorException($"AI embed failed: {ex.Message}", "AI_EMBED_ERROR", ex);
            }
        }

        private async Task<object> SimilarityAsync(Dictionary<string, object> parameters)
        {
            var text1 = GetRequiredParameter<string>(parameters, "text1");
            var text2 = GetRequiredParameter<string>(parameters, "text2");
            var model = GetParameter<string>(parameters, "model", "text-embedding-ada-002");

            try
            {
                var embed1 = await EmbedAsync(new Dictionary<string, object>
                {
                    { "text", text1 },
                    { "model", model }
                });

                var embed2 = await EmbedAsync(new Dictionary<string, object>
                {
                    { "text", text2 },
                    { "model", model }
                });

                // Simulate similarity calculation
                var similarity = 0.85; // Placeholder

                return new { success = true, text1, text2, similarity };
            }
            catch (Exception ex)
            {
                LogError($"Error calculating similarity: {ex.Message}");
                throw new OperatorException($"AI similarity failed: {ex.Message}", "AI_SIMILARITY_ERROR", ex);
            }
        }

        private async Task<object> ExtractAsync(Dictionary<string, object> parameters)
        {
            var text = GetRequiredParameter<string>(parameters, "text");
            var entities = GetParameter<string[]>(parameters, "entities", new[] { "names", "dates", "locations" });
            var model = GetParameter<string>(parameters, "model", _model);

            try
            {
                var prompt = $"Extract the following entities from the text: {string.Join(", ", entities)}\n\nText: {text}\n\nExtracted entities:";
                
                var result = await GenerateAsync(new Dictionary<string, object>
                {
                    { "prompt", prompt },
                    { "model", model },
                    { "max_tokens", 200 },
                    { "temperature", 0.1 }
                });

                return new { success = true, text, entities, extracted = result };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting entities: {ex.Message}");
                throw new OperatorException($"AI extract failed: {ex.Message}", "AI_EXTRACT_ERROR", ex);
            }
        }

        private async Task<object> SentimentAsync(Dictionary<string, object> parameters)
        {
            var text = GetRequiredParameter<string>(parameters, "text");
            var model = GetParameter<string>(parameters, "model", _model);

            try
            {
                var prompt = $"Analyze the sentiment of the following text and respond with only: positive, negative, or neutral\n\nText: {text}\n\nSentiment:";
                
                var result = await GenerateAsync(new Dictionary<string, object>
                {
                    { "prompt", prompt },
                    { "model", model },
                    { "max_tokens", 10 },
                    { "temperature", 0.1 }
                });

                return new { success = true, text, sentiment = result };
            }
            catch (Exception ex)
            {
                LogError($"Error analyzing sentiment: {ex.Message}");
                throw new OperatorException($"AI sentiment failed: {ex.Message}", "AI_SENTIMENT_ERROR", ex);
            }
        }

        private async Task<object> KeywordsAsync(Dictionary<string, object> parameters)
        {
            var text = GetRequiredParameter<string>(parameters, "text");
            var count = GetParameter<int>(parameters, "count", 5);
            var model = GetParameter<string>(parameters, "model", _model);

            try
            {
                var prompt = $"Extract {count} key keywords from the following text:\n\n{text}\n\nKeywords:";
                
                var result = await GenerateAsync(new Dictionary<string, object>
                {
                    { "prompt", prompt },
                    { "model", model },
                    { "max_tokens", count * 20 },
                    { "temperature", 0.1 }
                });

                return new { success = true, text, count, keywords = result };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting keywords: {ex.Message}");
                throw new OperatorException($"AI keywords failed: {ex.Message}", "AI_KEYWORDS_ERROR", ex);
            }
        }

        private async Task<object> GenerateImageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var prompt = GetRequiredParameter<string>(parameters, "prompt");
            var size = GetParameter<string>(parameters, "size", "1024x1024");
            var model = GetParameter<string>(parameters, "model", "dall-e-3");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "model", model },
                    { "prompt", prompt },
                    { "size", size },
                    { "n", 1 }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/images/generations";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Generate image failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully generated image");
                return new { success = true, prompt, size, model, image = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error generating image: {ex.Message}");
                throw new OperatorException($"AI generate image failed: {ex.Message}", "AI_GENERATE_IMAGE_ERROR", ex);
            }
        }

        private async Task<object> AnalyzeImageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var model = GetParameter<string>(parameters, "model", "gpt-4-vision-preview");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "model", model },
                    { "messages", new[]
                    {
                        new Dictionary<string, object>
                        {
                            { "role", "user" },
                            { "content", new[]
                            {
                                new Dictionary<string, object>
                                {
                                    { "type", "text" },
                                    { "text", "Describe this image in detail" }
                                },
                                new Dictionary<string, object>
                                {
                                    { "type", "image_url" },
                                    { "image_url", new Dictionary<string, object> { { "url", imageUrl } } }
                                }
                            }}
                        }
                    }},
                    { "max_tokens", 300 }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/chat/completions";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Analyze image failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully analyzed image");
                return new { success = true, imageUrl, model, analysis = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error analyzing image: {ex.Message}");
                throw new OperatorException($"AI analyze image failed: {ex.Message}", "AI_ANALYZE_IMAGE_ERROR", ex);
            }
        }

        private async Task<object> ListModelsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/models";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List models failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, models = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing models: {ex.Message}");
                throw new OperatorException($"AI list models failed: {ex.Message}", "AI_LIST_MODELS_ERROR", ex);
            }
        }

        private async Task<object> GetModelInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var model = GetRequiredParameter<string>(parameters, "model");

            try
            {
                var url = $"{_endpoint}/models/{model}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get model info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, model, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting model info: {ex.Message}");
                throw new OperatorException($"AI get model info failed: {ex.Message}", "AI_GET_MODEL_INFO_ERROR", ex);
            }
        }

        private async Task<object> FineTuneAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var model = GetRequiredParameter<string>(parameters, "model");
            var trainingData = GetRequiredParameter<string>(parameters, "training_data");
            var parameters2 = GetParameter<Dictionary<string, object>>(parameters, "parameters", new Dictionary<string, object>());

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "model", model },
                    { "training_file", trainingData }
                };

                foreach (var param in parameters2)
                {
                    requestData[param.Key] = param.Value;
                }

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/fine_tuning/jobs";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Fine-tune failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully started fine-tuning");
                return new { success = true, model, trainingData, job = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error fine-tuning model: {ex.Message}");
                throw new OperatorException($"AI fine-tune failed: {ex.Message}", "AI_FINE_TUNE_ERROR", ex);
            }
        }

        private async Task<object> GetUsageAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/usage";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get usage failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, usage = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting usage: {ex.Message}");
                throw new OperatorException($"AI get usage failed: {ex.Message}", "AI_GET_USAGE_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var result = await ListModelsAsync();
                return new { success = true, status = "Connected" };
            }
            catch (Exception ex)
            {
                LogError($"Error testing AI connection: {ex.Message}");
                throw new OperatorException($"AI connection test failed: {ex.Message}", "AI_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_apiKey))
            {
                throw new OperatorException("Not connected to AI service", "AI_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
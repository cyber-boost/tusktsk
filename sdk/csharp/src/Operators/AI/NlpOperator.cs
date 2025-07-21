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
    /// Natural Language Processing operator for TuskLang
    /// Provides NLP operations including text analysis, language detection, and processing
    /// </summary>
    public class NlpOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _endpoint;
        private string _language;

        public NlpOperator() : base("nlp", "Natural Language Processing operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to NLP service", new[] { "api_key", "endpoint", "language" });
            RegisterMethod("disconnect", "Disconnect from NLP service", new string[0]);
            RegisterMethod("tokenize", "Tokenize text", new[] { "text", "language" });
            RegisterMethod("pos_tag", "Part-of-speech tagging", new[] { "text", "language" });
            RegisterMethod("ner", "Named Entity Recognition", new[] { "text", "language" });
            RegisterMethod("sentiment", "Sentiment analysis", new[] { "text", "language" });
            RegisterMethod("language_detect", "Detect language", new[] { "text" });
            RegisterMethod("lemmatize", "Lemmatize text", new[] { "text", "language" });
            RegisterMethod("stem", "Stem text", new[] { "text", "language" });
            RegisterMethod("chunk", "Text chunking", new[] { "text", "language" });
            RegisterMethod("parse", "Dependency parsing", new[] { "text", "language" });
            RegisterMethod("extract_keywords", "Extract keywords", new[] { "text", "count", "language" });
            RegisterMethod("extract_phrases", "Extract phrases", new[] { "text", "language" });
            RegisterMethod("summarize", "Text summarization", new[] { "text", "max_length", "language" });
            RegisterMethod("translate", "Text translation", new[] { "text", "source_lang", "target_lang" });
            RegisterMethod("correct", "Text correction", new[] { "text", "language" });
            RegisterMethod("similarity", "Text similarity", new[] { "text1", "text2", "language" });
            RegisterMethod("classify", "Text classification", new[] { "text", "categories", "language" });
            RegisterMethod("extract_entities", "Extract entities", new[] { "text", "entity_types", "language" });
            RegisterMethod("extract_relations", "Extract relations", new[] { "text", "language" });
            RegisterMethod("get_supported_languages", "Get supported languages", new string[0]);
            RegisterMethod("test_connection", "Test NLP connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing NLP operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "tokenize":
                        return await TokenizeAsync(parameters);
                    case "pos_tag":
                        return await PosTagAsync(parameters);
                    case "ner":
                        return await NerAsync(parameters);
                    case "sentiment":
                        return await SentimentAsync(parameters);
                    case "language_detect":
                        return await LanguageDetectAsync(parameters);
                    case "lemmatize":
                        return await LemmatizeAsync(parameters);
                    case "stem":
                        return await StemAsync(parameters);
                    case "chunk":
                        return await ChunkAsync(parameters);
                    case "parse":
                        return await ParseAsync(parameters);
                    case "extract_keywords":
                        return await ExtractKeywordsAsync(parameters);
                    case "extract_phrases":
                        return await ExtractPhrasesAsync(parameters);
                    case "summarize":
                        return await SummarizeAsync(parameters);
                    case "translate":
                        return await TranslateAsync(parameters);
                    case "correct":
                        return await CorrectAsync(parameters);
                    case "similarity":
                        return await SimilarityAsync(parameters);
                    case "classify":
                        return await ClassifyAsync(parameters);
                    case "extract_entities":
                        return await ExtractEntitiesAsync(parameters);
                    case "extract_relations":
                        return await ExtractRelationsAsync(parameters);
                    case "get_supported_languages":
                        return await GetSupportedLanguagesAsync();
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown NLP method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing NLP method {method}: {ex.Message}");
                throw new OperatorException($"NLP operation failed: {ex.Message}", "NLP_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var apiKey = GetRequiredParameter<string>(parameters, "api_key");
            var endpoint = GetParameter<string>(parameters, "endpoint", "https://api.nlp-service.com/v1");
            var language = GetParameter<string>(parameters, "language", "en");

            try
            {
                _apiKey = apiKey;
                _endpoint = endpoint;
                _language = language;

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                LogInfo($"Connected to NLP service: {endpoint}");
                return new { success = true, endpoint, language };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to NLP service: {ex.Message}");
                throw new OperatorException($"NLP connection failed: {ex.Message}", "NLP_CONNECTION_ERROR", ex);
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
                _language = null;

                LogInfo("Disconnected from NLP service");
                return new { success = true, message = "Disconnected from NLP service" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from NLP service: {ex.Message}");
                throw new OperatorException($"NLP disconnect failed: {ex.Message}", "NLP_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> TokenizeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/tokenize";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Tokenize failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully tokenized text");
                return new { success = true, text, language, tokens = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error tokenizing text: {ex.Message}");
                throw new OperatorException($"NLP tokenize failed: {ex.Message}", "NLP_TOKENIZE_ERROR", ex);
            }
        }

        private async Task<object> PosTagAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/pos_tag";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"POS tagging failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully performed POS tagging");
                return new { success = true, text, language, posTags = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error performing POS tagging: {ex.Message}");
                throw new OperatorException($"NLP POS tag failed: {ex.Message}", "NLP_POS_TAG_ERROR", ex);
            }
        }

        private async Task<object> NerAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/ner";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"NER failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully performed Named Entity Recognition");
                return new { success = true, text, language, entities = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error performing NER: {ex.Message}");
                throw new OperatorException($"NLP NER failed: {ex.Message}", "NLP_NER_ERROR", ex);
            }
        }

        private async Task<object> SentimentAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/sentiment";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Sentiment analysis failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully performed sentiment analysis");
                return new { success = true, text, language, sentiment = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error performing sentiment analysis: {ex.Message}");
                throw new OperatorException($"NLP sentiment failed: {ex.Message}", "NLP_SENTIMENT_ERROR", ex);
            }
        }

        private async Task<object> LanguageDetectAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/language_detect";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Language detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected language");
                return new { success = true, text, language = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting language: {ex.Message}");
                throw new OperatorException($"NLP language detect failed: {ex.Message}", "NLP_LANGUAGE_DETECT_ERROR", ex);
            }
        }

        private async Task<object> LemmatizeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/lemmatize";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lemmatization failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully lemmatized text");
                return new { success = true, text, language, lemmas = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error lemmatizing text: {ex.Message}");
                throw new OperatorException($"NLP lemmatize failed: {ex.Message}", "NLP_LEMMATIZE_ERROR", ex);
            }
        }

        private async Task<object> StemAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/stem";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Stemming failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully stemmed text");
                return new { success = true, text, language, stems = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error stemming text: {ex.Message}");
                throw new OperatorException($"NLP stem failed: {ex.Message}", "NLP_STEM_ERROR", ex);
            }
        }

        private async Task<object> ChunkAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/chunk";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Chunking failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully chunked text");
                return new { success = true, text, language, chunks = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error chunking text: {ex.Message}");
                throw new OperatorException($"NLP chunk failed: {ex.Message}", "NLP_CHUNK_ERROR", ex);
            }
        }

        private async Task<object> ParseAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/parse";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Parsing failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully parsed text");
                return new { success = true, text, language, parse = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error parsing text: {ex.Message}");
                throw new OperatorException($"NLP parse failed: {ex.Message}", "NLP_PARSE_ERROR", ex);
            }
        }

        private async Task<object> ExtractKeywordsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var count = GetParameter<int>(parameters, "count", 10);
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "count", count },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/extract_keywords";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Keyword extraction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully extracted keywords");
                return new { success = true, text, count, language, keywords = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting keywords: {ex.Message}");
                throw new OperatorException($"NLP extract keywords failed: {ex.Message}", "NLP_EXTRACT_KEYWORDS_ERROR", ex);
            }
        }

        private async Task<object> ExtractPhrasesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/extract_phrases";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Phrase extraction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully extracted phrases");
                return new { success = true, text, language, phrases = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting phrases: {ex.Message}");
                throw new OperatorException($"NLP extract phrases failed: {ex.Message}", "NLP_EXTRACT_PHRASES_ERROR", ex);
            }
        }

        private async Task<object> SummarizeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var maxLength = GetParameter<int>(parameters, "max_length", 100);
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "max_length", maxLength },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/summarize";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Summarization failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully summarized text");
                return new { success = true, text, maxLength, language, summary = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error summarizing text: {ex.Message}");
                throw new OperatorException($"NLP summarize failed: {ex.Message}", "NLP_SUMMARIZE_ERROR", ex);
            }
        }

        private async Task<object> TranslateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var sourceLang = GetParameter<string>(parameters, "source_lang", "auto");
            var targetLang = GetRequiredParameter<string>(parameters, "target_lang");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "source_lang", sourceLang },
                    { "target_lang", targetLang }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/translate";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Translation failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully translated text");
                return new { success = true, text, sourceLang, targetLang, translation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error translating text: {ex.Message}");
                throw new OperatorException($"NLP translate failed: {ex.Message}", "NLP_TRANSLATE_ERROR", ex);
            }
        }

        private async Task<object> CorrectAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/correct";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Text correction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully corrected text");
                return new { success = true, text, language, corrected = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error correcting text: {ex.Message}");
                throw new OperatorException($"NLP correct failed: {ex.Message}", "NLP_CORRECT_ERROR", ex);
            }
        }

        private async Task<object> SimilarityAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text1 = GetRequiredParameter<string>(parameters, "text1");
            var text2 = GetRequiredParameter<string>(parameters, "text2");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text1", text1 },
                    { "text2", text2 },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/similarity";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Similarity calculation failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully calculated text similarity");
                return new { success = true, text1, text2, language, similarity = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error calculating similarity: {ex.Message}");
                throw new OperatorException($"NLP similarity failed: {ex.Message}", "NLP_SIMILARITY_ERROR", ex);
            }
        }

        private async Task<object> ClassifyAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var categories = GetRequiredParameter<string[]>(parameters, "categories");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "categories", categories },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/classify";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Classification failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully classified text");
                return new { success = true, text, categories, language, classification = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error classifying text: {ex.Message}");
                throw new OperatorException($"NLP classify failed: {ex.Message}", "NLP_CLASSIFY_ERROR", ex);
            }
        }

        private async Task<object> ExtractEntitiesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var entityTypes = GetParameter<string[]>(parameters, "entity_types", new[] { "person", "organization", "location" });
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "entity_types", entityTypes },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/extract_entities";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Entity extraction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully extracted entities");
                return new { success = true, text, entityTypes, language, entities = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting entities: {ex.Message}");
                throw new OperatorException($"NLP extract entities failed: {ex.Message}", "NLP_EXTRACT_ENTITIES_ERROR", ex);
            }
        }

        private async Task<object> ExtractRelationsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/extract_relations";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Relation extraction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully extracted relations");
                return new { success = true, text, language, relations = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting relations: {ex.Message}");
                throw new OperatorException($"NLP extract relations failed: {ex.Message}", "NLP_EXTRACT_RELATIONS_ERROR", ex);
            }
        }

        private async Task<object> GetSupportedLanguagesAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/languages";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get languages failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, languages = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting supported languages: {ex.Message}");
                throw new OperatorException($"NLP get languages failed: {ex.Message}", "NLP_GET_LANGUAGES_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var result = await GetSupportedLanguagesAsync();
                return new { success = true, status = "Connected" };
            }
            catch (Exception ex)
            {
                LogError($"Error testing NLP connection: {ex.Message}");
                throw new OperatorException($"NLP connection test failed: {ex.Message}", "NLP_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_apiKey))
            {
                throw new OperatorException("Not connected to NLP service", "NLP_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
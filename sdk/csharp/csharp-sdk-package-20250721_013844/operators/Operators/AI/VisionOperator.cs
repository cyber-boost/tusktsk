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
    /// Computer Vision operator for TuskLang
    /// Provides vision operations including image analysis, object detection, and processing
    /// </summary>
    public class VisionOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _endpoint;

        public VisionOperator() : base("vision", "Computer Vision operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Vision service", new[] { "api_key", "endpoint" });
            RegisterMethod("disconnect", "Disconnect from Vision service", new string[0]);
            RegisterMethod("analyze", "Analyze image", new[] { "image_url", "features" });
            RegisterMethod("detect_objects", "Detect objects in image", new[] { "image_url", "confidence" });
            RegisterMethod("detect_faces", "Detect faces in image", new[] { "image_url", "attributes" });
            RegisterMethod("detect_text", "Detect text in image", new[] { "image_url", "language" });
            RegisterMethod("detect_landmarks", "Detect landmarks in image", new[] { "image_url" });
            RegisterMethod("detect_brands", "Detect brands in image", new[] { "image_url" });
            RegisterMethod("detect_colors", "Detect colors in image", new[] { "image_url" });
            RegisterMethod("detect_adult", "Detect adult content", new[] { "image_url" });
            RegisterMethod("detect_celebrities", "Detect celebrities in image", new[] { "image_url" });
            RegisterMethod("detect_emotions", "Detect emotions in image", new[] { "image_url" });
            RegisterMethod("classify", "Classify image", new[] { "image_url", "categories" });
            RegisterMethod("describe", "Describe image", new[] { "image_url", "max_candidates" });
            RegisterMethod("tag", "Tag image", new[] { "image_url", "confidence" });
            RegisterMethod("generate_thumbnail", "Generate thumbnail", new[] { "image_url", "width", "height", "smart_crop" });
            RegisterMethod("extract_ocr", "Extract OCR text", new[] { "image_url", "language" });
            RegisterMethod("compare", "Compare images", new[] { "image1_url", "image2_url" });
            RegisterMethod("find_similar", "Find similar images", new[] { "image_url", "count" });
            RegisterMethod("get_image_info", "Get image information", new[] { "image_url" });
            RegisterMethod("test_connection", "Test Vision connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Vision operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "analyze":
                        return await AnalyzeAsync(parameters);
                    case "detect_objects":
                        return await DetectObjectsAsync(parameters);
                    case "detect_faces":
                        return await DetectFacesAsync(parameters);
                    case "detect_text":
                        return await DetectTextAsync(parameters);
                    case "detect_landmarks":
                        return await DetectLandmarksAsync(parameters);
                    case "detect_brands":
                        return await DetectBrandsAsync(parameters);
                    case "detect_colors":
                        return await DetectColorsAsync(parameters);
                    case "detect_adult":
                        return await DetectAdultAsync(parameters);
                    case "detect_celebrities":
                        return await DetectCelebritiesAsync(parameters);
                    case "detect_emotions":
                        return await DetectEmotionsAsync(parameters);
                    case "classify":
                        return await ClassifyAsync(parameters);
                    case "describe":
                        return await DescribeAsync(parameters);
                    case "tag":
                        return await TagAsync(parameters);
                    case "generate_thumbnail":
                        return await GenerateThumbnailAsync(parameters);
                    case "extract_ocr":
                        return await ExtractOcrAsync(parameters);
                    case "compare":
                        return await CompareAsync(parameters);
                    case "find_similar":
                        return await FindSimilarAsync(parameters);
                    case "get_image_info":
                        return await GetImageInfoAsync(parameters);
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown Vision method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Vision method {method}: {ex.Message}");
                throw new OperatorException($"Vision operation failed: {ex.Message}", "VISION_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var apiKey = GetRequiredParameter<string>(parameters, "api_key");
            var endpoint = GetParameter<string>(parameters, "endpoint", "https://api.vision-service.com/v1");

            try
            {
                _apiKey = apiKey;
                _endpoint = endpoint;

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                LogInfo($"Connected to Vision service: {endpoint}");
                return new { success = true, endpoint };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Vision service: {ex.Message}");
                throw new OperatorException($"Vision connection failed: {ex.Message}", "VISION_CONNECTION_ERROR", ex);
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

                LogInfo("Disconnected from Vision service");
                return new { success = true, message = "Disconnected from Vision service" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from Vision service: {ex.Message}");
                throw new OperatorException($"Vision disconnect failed: {ex.Message}", "VISION_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> AnalyzeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var features = GetParameter<string[]>(parameters, "features", new[] { "objects", "faces", "text", "landmarks" });

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "features", features }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/analyze";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Analyze failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully analyzed image");
                return new { success = true, imageUrl, features, analysis = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error analyzing image: {ex.Message}");
                throw new OperatorException($"Vision analyze failed: {ex.Message}", "VISION_ANALYZE_ERROR", ex);
            }
        }

        private async Task<object> DetectObjectsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var confidence = GetParameter<double>(parameters, "confidence", 0.5);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "confidence", confidence }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_objects";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Object detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected objects in image");
                return new { success = true, imageUrl, confidence, objects = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting objects: {ex.Message}");
                throw new OperatorException($"Vision detect objects failed: {ex.Message}", "VISION_DETECT_OBJECTS_ERROR", ex);
            }
        }

        private async Task<object> DetectFacesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var attributes = GetParameter<string[]>(parameters, "attributes", new[] { "age", "gender", "emotion" });

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "attributes", attributes }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_faces";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Face detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected faces in image");
                return new { success = true, imageUrl, attributes, faces = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting faces: {ex.Message}");
                throw new OperatorException($"Vision detect faces failed: {ex.Message}", "VISION_DETECT_FACES_ERROR", ex);
            }
        }

        private async Task<object> DetectTextAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var language = GetParameter<string>(parameters, "language", "en");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_text";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Text detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected text in image");
                return new { success = true, imageUrl, language, text = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting text: {ex.Message}");
                throw new OperatorException($"Vision detect text failed: {ex.Message}", "VISION_DETECT_TEXT_ERROR", ex);
            }
        }

        private async Task<object> DetectLandmarksAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_landmarks";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Landmark detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected landmarks in image");
                return new { success = true, imageUrl, landmarks = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting landmarks: {ex.Message}");
                throw new OperatorException($"Vision detect landmarks failed: {ex.Message}", "VISION_DETECT_LANDMARKS_ERROR", ex);
            }
        }

        private async Task<object> DetectBrandsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_brands";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Brand detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected brands in image");
                return new { success = true, imageUrl, brands = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting brands: {ex.Message}");
                throw new OperatorException($"Vision detect brands failed: {ex.Message}", "VISION_DETECT_BRANDS_ERROR", ex);
            }
        }

        private async Task<object> DetectColorsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_colors";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Color detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected colors in image");
                return new { success = true, imageUrl, colors = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting colors: {ex.Message}");
                throw new OperatorException($"Vision detect colors failed: {ex.Message}", "VISION_DETECT_COLORS_ERROR", ex);
            }
        }

        private async Task<object> DetectAdultAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_adult";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Adult content detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected adult content in image");
                return new { success = true, imageUrl, adultContent = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting adult content: {ex.Message}");
                throw new OperatorException($"Vision detect adult failed: {ex.Message}", "VISION_DETECT_ADULT_ERROR", ex);
            }
        }

        private async Task<object> DetectCelebritiesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_celebrities";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Celebrity detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected celebrities in image");
                return new { success = true, imageUrl, celebrities = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting celebrities: {ex.Message}");
                throw new OperatorException($"Vision detect celebrities failed: {ex.Message}", "VISION_DETECT_CELEBRITIES_ERROR", ex);
            }
        }

        private async Task<object> DetectEmotionsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_emotions";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Emotion detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected emotions in image");
                return new { success = true, imageUrl, emotions = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting emotions: {ex.Message}");
                throw new OperatorException($"Vision detect emotions failed: {ex.Message}", "VISION_DETECT_EMOTIONS_ERROR", ex);
            }
        }

        private async Task<object> ClassifyAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var categories = GetParameter<string[]>(parameters, "categories", new string[0]);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl }
                };

                if (categories.Length > 0)
                {
                    requestData["categories"] = categories;
                }

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

                LogInfo($"Successfully classified image");
                return new { success = true, imageUrl, categories, classification = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error classifying image: {ex.Message}");
                throw new OperatorException($"Vision classify failed: {ex.Message}", "VISION_CLASSIFY_ERROR", ex);
            }
        }

        private async Task<object> DescribeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var maxCandidates = GetParameter<int>(parameters, "max_candidates", 3);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "max_candidates", maxCandidates }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/describe";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Description failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully described image");
                return new { success = true, imageUrl, maxCandidates, description = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error describing image: {ex.Message}");
                throw new OperatorException($"Vision describe failed: {ex.Message}", "VISION_DESCRIBE_ERROR", ex);
            }
        }

        private async Task<object> TagAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var confidence = GetParameter<double>(parameters, "confidence", 0.5);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "confidence", confidence }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/tag";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Tagging failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully tagged image");
                return new { success = true, imageUrl, confidence, tags = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error tagging image: {ex.Message}");
                throw new OperatorException($"Vision tag failed: {ex.Message}", "VISION_TAG_ERROR", ex);
            }
        }

        private async Task<object> GenerateThumbnailAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var width = GetParameter<int>(parameters, "width", 200);
            var height = GetParameter<int>(parameters, "height", 200);
            var smartCrop = GetParameter<bool>(parameters, "smart_crop", true);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "width", width },
                    { "height", height },
                    { "smart_crop", smartCrop }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/thumbnail";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Thumbnail generation failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully generated thumbnail");
                return new { success = true, imageUrl, width, height, smartCrop, thumbnail = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error generating thumbnail: {ex.Message}");
                throw new OperatorException($"Vision generate thumbnail failed: {ex.Message}", "VISION_GENERATE_THUMBNAIL_ERROR", ex);
            }
        }

        private async Task<object> ExtractOcrAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var language = GetParameter<string>(parameters, "language", "en");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "language", language }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/ocr";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OCR extraction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully extracted OCR text");
                return new { success = true, imageUrl, language, ocr = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting OCR: {ex.Message}");
                throw new OperatorException($"Vision extract OCR failed: {ex.Message}", "VISION_EXTRACT_OCR_ERROR", ex);
            }
        }

        private async Task<object> CompareAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var image1Url = GetRequiredParameter<string>(parameters, "image1_url");
            var image2Url = GetRequiredParameter<string>(parameters, "image2_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image1_url", image1Url },
                    { "image2_url", image2Url }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/compare";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Image comparison failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully compared images");
                return new { success = true, image1Url, image2Url, comparison = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error comparing images: {ex.Message}");
                throw new OperatorException($"Vision compare failed: {ex.Message}", "VISION_COMPARE_ERROR", ex);
            }
        }

        private async Task<object> FindSimilarAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");
            var count = GetParameter<int>(parameters, "count", 10);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl },
                    { "count", count }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/find_similar";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Similar image search failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully found similar images");
                return new { success = true, imageUrl, count, similar = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error finding similar images: {ex.Message}");
                throw new OperatorException($"Vision find similar failed: {ex.Message}", "VISION_FIND_SIMILAR_ERROR", ex);
            }
        }

        private async Task<object> GetImageInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageUrl = GetRequiredParameter<string>(parameters, "image_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_url", imageUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/image_info";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get image info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved image information");
                return new { success = true, imageUrl, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting image info: {ex.Message}");
                throw new OperatorException($"Vision get image info failed: {ex.Message}", "VISION_GET_IMAGE_INFO_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                // Test with a simple image analysis
                var testResult = await AnalyzeAsync(new Dictionary<string, object>
                {
                    { "image_url", "https://example.com/test.jpg" },
                    { "features", new[] { "objects" } }
                });

                return new { success = true, status = "Connected" };
            }
            catch (Exception ex)
            {
                LogError($"Error testing Vision connection: {ex.Message}");
                throw new OperatorException($"Vision connection test failed: {ex.Message}", "VISION_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_apiKey))
            {
                throw new OperatorException("Not connected to Vision service", "VISION_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
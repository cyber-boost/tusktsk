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
    /// Speech operator for TuskLang
    /// Provides speech operations including speech-to-text, text-to-speech, and audio processing
    /// </summary>
    public class SpeechOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _endpoint;
        private string _language;

        public SpeechOperator() : base("speech", "Speech processing operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Speech service", new[] { "api_key", "endpoint", "language" });
            RegisterMethod("disconnect", "Disconnect from Speech service", new string[0]);
            RegisterMethod("speech_to_text", "Convert speech to text", new[] { "audio_url", "language", "format" });
            RegisterMethod("text_to_speech", "Convert text to speech", new[] { "text", "voice", "language", "format" });
            RegisterMethod("transcribe", "Transcribe audio file", new[] { "audio_url", "language", "model" });
            RegisterMethod("synthesize", "Synthesize speech", new[] { "text", "voice", "speed", "pitch" });
            RegisterMethod("recognize_speaker", "Recognize speaker", new[] { "audio_url", "speakers" });
            RegisterMethod("detect_language", "Detect spoken language", new[] { "audio_url" });
            RegisterMethod("extract_emotion", "Extract emotion from speech", new[] { "audio_url" });
            RegisterMethod("extract_keywords", "Extract keywords from speech", new[] { "audio_url", "count" });
            RegisterMethod("summarize_speech", "Summarize speech content", new[] { "audio_url", "max_length" });
            RegisterMethod("translate_speech", "Translate speech", new[] { "audio_url", "source_lang", "target_lang" });
            RegisterMethod("analyze_pronunciation", "Analyze pronunciation", new[] { "audio_url", "text" });
            RegisterMethod("detect_silence", "Detect silence periods", new[] { "audio_url", "threshold" });
            RegisterMethod("extract_timestamps", "Extract word timestamps", new[] { "audio_url" });
            RegisterMethod("get_available_voices", "Get available voices", new[] { "language" });
            RegisterMethod("get_supported_languages", "Get supported languages", new string[0]);
            RegisterMethod("get_audio_info", "Get audio file information", new[] { "audio_url" });
            RegisterMethod("convert_format", "Convert audio format", new[] { "audio_url", "format" });
            RegisterMethod("test_connection", "Test Speech connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Speech operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "speech_to_text":
                        return await SpeechToTextAsync(parameters);
                    case "text_to_speech":
                        return await TextToSpeechAsync(parameters);
                    case "transcribe":
                        return await TranscribeAsync(parameters);
                    case "synthesize":
                        return await SynthesizeAsync(parameters);
                    case "recognize_speaker":
                        return await RecognizeSpeakerAsync(parameters);
                    case "detect_language":
                        return await DetectLanguageAsync(parameters);
                    case "extract_emotion":
                        return await ExtractEmotionAsync(parameters);
                    case "extract_keywords":
                        return await ExtractKeywordsAsync(parameters);
                    case "summarize_speech":
                        return await SummarizeSpeechAsync(parameters);
                    case "translate_speech":
                        return await TranslateSpeechAsync(parameters);
                    case "analyze_pronunciation":
                        return await AnalyzePronunciationAsync(parameters);
                    case "detect_silence":
                        return await DetectSilenceAsync(parameters);
                    case "extract_timestamps":
                        return await ExtractTimestampsAsync(parameters);
                    case "get_available_voices":
                        return await GetAvailableVoicesAsync(parameters);
                    case "get_supported_languages":
                        return await GetSupportedLanguagesAsync();
                    case "get_audio_info":
                        return await GetAudioInfoAsync(parameters);
                    case "convert_format":
                        return await ConvertFormatAsync(parameters);
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown Speech method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Speech method {method}: {ex.Message}");
                throw new OperatorException($"Speech operation failed: {ex.Message}", "SPEECH_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var apiKey = GetRequiredParameter<string>(parameters, "api_key");
            var endpoint = GetParameter<string>(parameters, "endpoint", "https://api.speech-service.com/v1");
            var language = GetParameter<string>(parameters, "language", "en-US");

            try
            {
                _apiKey = apiKey;
                _endpoint = endpoint;
                _language = language;

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                LogInfo($"Connected to Speech service: {endpoint}");
                return new { success = true, endpoint, language };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Speech service: {ex.Message}");
                throw new OperatorException($"Speech connection failed: {ex.Message}", "SPEECH_CONNECTION_ERROR", ex);
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

                LogInfo("Disconnected from Speech service");
                return new { success = true, message = "Disconnected from Speech service" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from Speech service: {ex.Message}");
                throw new OperatorException($"Speech disconnect failed: {ex.Message}", "SPEECH_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> SpeechToTextAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var language = GetParameter<string>(parameters, "language", _language);
            var format = GetParameter<string>(parameters, "format", "wav");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "language", language },
                    { "format", format }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/speech_to_text";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Speech to text failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully converted speech to text");
                return new { success = true, audioUrl, language, format, text = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error converting speech to text: {ex.Message}");
                throw new OperatorException($"Speech to text failed: {ex.Message}", "SPEECH_TO_TEXT_ERROR", ex);
            }
        }

        private async Task<object> TextToSpeechAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var voice = GetParameter<string>(parameters, "voice", "en-US-Standard-A");
            var language = GetParameter<string>(parameters, "language", _language);
            var format = GetParameter<string>(parameters, "format", "mp3");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "voice", voice },
                    { "language", language },
                    { "format", format }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/text_to_speech";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Text to speech failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully converted text to speech");
                return new { success = true, text, voice, language, format, audio = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error converting text to speech: {ex.Message}");
                throw new OperatorException($"Text to speech failed: {ex.Message}", "TEXT_TO_SPEECH_ERROR", ex);
            }
        }

        private async Task<object> TranscribeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var language = GetParameter<string>(parameters, "language", _language);
            var model = GetParameter<string>(parameters, "model", "general");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "language", language },
                    { "model", model }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/transcribe";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Transcription failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully transcribed audio");
                return new { success = true, audioUrl, language, model, transcription = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error transcribing audio: {ex.Message}");
                throw new OperatorException($"Transcription failed: {ex.Message}", "TRANSCRIPTION_ERROR", ex);
            }
        }

        private async Task<object> SynthesizeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var text = GetRequiredParameter<string>(parameters, "text");
            var voice = GetParameter<string>(parameters, "voice", "en-US-Standard-A");
            var speed = GetParameter<double>(parameters, "speed", 1.0);
            var pitch = GetParameter<double>(parameters, "pitch", 1.0);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", text },
                    { "voice", voice },
                    { "speed", speed },
                    { "pitch", pitch }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/synthesize";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Synthesis failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully synthesized speech");
                return new { success = true, text, voice, speed, pitch, audio = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error synthesizing speech: {ex.Message}");
                throw new OperatorException($"Synthesis failed: {ex.Message}", "SYNTHESIS_ERROR", ex);
            }
        }

        private async Task<object> RecognizeSpeakerAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var speakers = GetParameter<int>(parameters, "speakers", 2);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "speakers", speakers }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/recognize_speaker";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Speaker recognition failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully recognized speakers");
                return new { success = true, audioUrl, speakers, recognition = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error recognizing speakers: {ex.Message}");
                throw new OperatorException($"Speaker recognition failed: {ex.Message}", "SPEAKER_RECOGNITION_ERROR", ex);
            }
        }

        private async Task<object> DetectLanguageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_language";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Language detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected spoken language");
                return new { success = true, audioUrl, language = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting language: {ex.Message}");
                throw new OperatorException($"Language detection failed: {ex.Message}", "LANGUAGE_DETECTION_ERROR", ex);
            }
        }

        private async Task<object> ExtractEmotionAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/extract_emotion";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Emotion extraction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully extracted emotion from speech");
                return new { success = true, audioUrl, emotion = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting emotion: {ex.Message}");
                throw new OperatorException($"Emotion extraction failed: {ex.Message}", "EMOTION_EXTRACTION_ERROR", ex);
            }
        }

        private async Task<object> ExtractKeywordsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var count = GetParameter<int>(parameters, "count", 10);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "count", count }
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

                LogInfo($"Successfully extracted keywords from speech");
                return new { success = true, audioUrl, count, keywords = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting keywords: {ex.Message}");
                throw new OperatorException($"Keyword extraction failed: {ex.Message}", "KEYWORD_EXTRACTION_ERROR", ex);
            }
        }

        private async Task<object> SummarizeSpeechAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var maxLength = GetParameter<int>(parameters, "max_length", 100);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "max_length", maxLength }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/summarize_speech";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Speech summarization failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully summarized speech");
                return new { success = true, audioUrl, maxLength, summary = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error summarizing speech: {ex.Message}");
                throw new OperatorException($"Speech summarization failed: {ex.Message}", "SPEECH_SUMMARIZATION_ERROR", ex);
            }
        }

        private async Task<object> TranslateSpeechAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var sourceLang = GetParameter<string>(parameters, "source_lang", "auto");
            var targetLang = GetRequiredParameter<string>(parameters, "target_lang");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "source_lang", sourceLang },
                    { "target_lang", targetLang }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/translate_speech";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Speech translation failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully translated speech");
                return new { success = true, audioUrl, sourceLang, targetLang, translation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error translating speech: {ex.Message}");
                throw new OperatorException($"Speech translation failed: {ex.Message}", "SPEECH_TRANSLATION_ERROR", ex);
            }
        }

        private async Task<object> AnalyzePronunciationAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var text = GetRequiredParameter<string>(parameters, "text");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "text", text }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/analyze_pronunciation";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Pronunciation analysis failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully analyzed pronunciation");
                return new { success = true, audioUrl, text, pronunciation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error analyzing pronunciation: {ex.Message}");
                throw new OperatorException($"Pronunciation analysis failed: {ex.Message}", "PRONUNCIATION_ANALYSIS_ERROR", ex);
            }
        }

        private async Task<object> DetectSilenceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var threshold = GetParameter<double>(parameters, "threshold", 0.1);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "threshold", threshold }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/detect_silence";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Silence detection failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully detected silence periods");
                return new { success = true, audioUrl, threshold, silence = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error detecting silence: {ex.Message}");
                throw new OperatorException($"Silence detection failed: {ex.Message}", "SILENCE_DETECTION_ERROR", ex);
            }
        }

        private async Task<object> ExtractTimestampsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/extract_timestamps";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Timestamp extraction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully extracted word timestamps");
                return new { success = true, audioUrl, timestamps = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error extracting timestamps: {ex.Message}");
                throw new OperatorException($"Timestamp extraction failed: {ex.Message}", "TIMESTAMP_EXTRACTION_ERROR", ex);
            }
        }

        private async Task<object> GetAvailableVoicesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var language = GetParameter<string>(parameters, "language", _language);

            try
            {
                var url = $"{_endpoint}/voices?language={language}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get voices failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, language, voices = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting available voices: {ex.Message}");
                throw new OperatorException($"Get voices failed: {ex.Message}", "GET_VOICES_ERROR", ex);
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
                throw new OperatorException($"Get languages failed: {ex.Message}", "GET_LANGUAGES_ERROR", ex);
            }
        }

        private async Task<object> GetAudioInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/audio_info";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get audio info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved audio information");
                return new { success = true, audioUrl, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting audio info: {ex.Message}");
                throw new OperatorException($"Get audio info failed: {ex.Message}", "GET_AUDIO_INFO_ERROR", ex);
            }
        }

        private async Task<object> ConvertFormatAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var audioUrl = GetRequiredParameter<string>(parameters, "audio_url");
            var format = GetRequiredParameter<string>(parameters, "format");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "audio_url", audioUrl },
                    { "format", format }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/convert_format";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Format conversion failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully converted audio format");
                return new { success = true, audioUrl, format, converted = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error converting audio format: {ex.Message}");
                throw new OperatorException($"Format conversion failed: {ex.Message}", "FORMAT_CONVERSION_ERROR", ex);
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
                LogError($"Error testing Speech connection: {ex.Message}");
                throw new OperatorException($"Speech connection test failed: {ex.Message}", "SPEECH_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_apiKey))
            {
                throw new OperatorException("Not connected to Speech service", "SPEECH_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
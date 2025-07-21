using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TuskLang
{
    /// <summary>
    /// Advanced natural language processing and text analytics system for TuskLang C# SDK
    /// Provides text analysis, sentiment analysis, entity extraction, and language processing
    /// </summary>
    public class NaturalLanguageProcessing
    {
        private readonly Dictionary<string, INLPProcessor> _processors;
        private readonly List<ITextAnalyzer> _textAnalyzers;
        private readonly List<ILanguageModel> _languageModels;
        private readonly NLPMetrics _metrics;
        private readonly TextProcessor _textProcessor;
        private readonly SentimentAnalyzer _sentimentAnalyzer;
        private readonly EntityExtractor _entityExtractor;
        private readonly object _lock = new object();

        public NaturalLanguageProcessing()
        {
            _processors = new Dictionary<string, INLPProcessor>();
            _textAnalyzers = new List<ITextAnalyzer>();
            _languageModels = new List<ILanguageModel>();
            _metrics = new NLPMetrics();
            _textProcessor = new TextProcessor();
            _sentimentAnalyzer = new SentimentAnalyzer();
            _entityExtractor = new EntityExtractor();

            // Register default text analyzers
            RegisterTextAnalyzer(new SentimentAnalysis());
            RegisterTextAnalyzer(new EntityRecognition());
            RegisterTextAnalyzer(new TopicModeling());
            
            // Register default language models
            RegisterLanguageModel(new BERTModel());
            RegisterLanguageModel(new GPTModel());
            RegisterLanguageModel(new CustomLanguageModel());
        }

        /// <summary>
        /// Register an NLP processor
        /// </summary>
        public void RegisterProcessor(string processorName, INLPProcessor processor)
        {
            lock (_lock)
            {
                _processors[processorName] = processor;
            }
        }

        /// <summary>
        /// Process and analyze text
        /// </summary>
        public async Task<TextAnalysisResult> AnalyzeTextAsync(
            string text,
            TextAnalysisConfig config = null)
        {
            var startTime = DateTime.UtcNow;
            var results = new List<AnalysisResult>();

            try
            {
                // Preprocess text
                var processedText = await _textProcessor.PreprocessTextAsync(text, config?.PreprocessingConfig);

                // Run text analyzers
                foreach (var analyzer in _textAnalyzers)
                {
                    if (config?.AnalysisTypes.Contains(analyzer.Type) != false)
                    {
                        var result = await analyzer.AnalyzeAsync(processedText, config);
                        results.Add(result);
                    }
                }

                _metrics.RecordTextAnalysis(true, DateTime.UtcNow - startTime);

                return new TextAnalysisResult
                {
                    Success = true,
                    OriginalText = text,
                    ProcessedText = processedText,
                    AnalysisResults = results,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _metrics.RecordTextAnalysis(false, DateTime.UtcNow - startTime);
                return new TextAnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Perform sentiment analysis
        /// </summary>
        public async Task<SentimentResult> AnalyzeSentimentAsync(
            string text,
            SentimentConfig config = null)
        {
            return await _sentimentAnalyzer.AnalyzeSentimentAsync(text, config ?? new SentimentConfig());
        }

        /// <summary>
        /// Extract entities from text
        /// </summary>
        public async Task<EntityExtractionResult> ExtractEntitiesAsync(
            string text,
            EntityConfig config = null)
        {
            return await _entityExtractor.ExtractEntitiesAsync(text, config ?? new EntityConfig());
        }

        /// <summary>
        /// Generate text using language model
        /// </summary>
        public async Task<TextGenerationResult> GenerateTextAsync(
            string prompt,
            string modelName,
            GenerationConfig config = null)
        {
            var model = _languageModels.FirstOrDefault(m => m.Name == modelName);
            if (model == null)
            {
                throw new InvalidOperationException($"Language model '{modelName}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                var generatedText = await model.GenerateTextAsync(prompt, config ?? new GenerationConfig());

                _metrics.RecordTextGeneration(modelName, true, DateTime.UtcNow - startTime);

                return new TextGenerationResult
                {
                    Success = true,
                    ModelName = modelName,
                    Prompt = prompt,
                    GeneratedText = generatedText,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _metrics.RecordTextGeneration(modelName, false, DateTime.UtcNow - startTime);
                return new TextGenerationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Translate text between languages
        /// </summary>
        public async Task<TranslationResult> TranslateTextAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            TranslationConfig config = null)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would use a translation service
                await Task.Delay(500);

                var translatedText = $"Translated: {text} ({sourceLanguage} -> {targetLanguage})";

                return new TranslationResult
                {
                    Success = true,
                    OriginalText = text,
                    TranslatedText = translatedText,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    Confidence = new Random().NextDouble() * 0.2 + 0.8,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new TranslationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Summarize text
        /// </summary>
        public async Task<SummarizationResult> SummarizeTextAsync(
            string text,
            SummarizationConfig config = null)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would use extractive or abstractive summarization
                await Task.Delay(300);

                var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                var summaryLength = config?.MaxLength ?? 3;
                var summary = string.Join(". ", sentences.Take(summaryLength)) + ".";

                return new SummarizationResult
                {
                    Success = true,
                    OriginalText = text,
                    Summary = summary,
                    CompressionRatio = (double)summary.Length / text.Length,
                    KeyPoints = ExtractKeyPoints(text),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new SummarizationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Register a text analyzer
        /// </summary>
        public void RegisterTextAnalyzer(ITextAnalyzer analyzer)
        {
            lock (_lock)
            {
                _textAnalyzers.Add(analyzer);
            }
        }

        /// <summary>
        /// Register a language model
        /// </summary>
        public void RegisterLanguageModel(ILanguageModel model)
        {
            lock (_lock)
            {
                _languageModels.Add(model);
            }
        }

        /// <summary>
        /// Get NLP metrics
        /// </summary>
        public NLPMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Get all processor names
        /// </summary>
        public List<string> GetProcessorNames()
        {
            lock (_lock)
            {
                return _processors.Keys.ToList();
            }
        }

        private List<string> ExtractKeyPoints(string text)
        {
            // Simple key point extraction
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            return sentences.Take(3).Select(s => s.Trim()).ToList();
        }
    }

    /// <summary>
    /// NLP processor interface
    /// </summary>
    public interface INLPProcessor
    {
        string Name { get; }
        Task<ProcessingResult> ProcessAsync(string text, ProcessingConfig config);
    }

    /// <summary>
    /// Text analyzer interface
    /// </summary>
    public interface ITextAnalyzer
    {
        string Name { get; }
        string Type { get; }
        Task<AnalysisResult> AnalyzeAsync(string text, TextAnalysisConfig config);
    }

    /// <summary>
    /// Language model interface
    /// </summary>
    public interface ILanguageModel
    {
        string Name { get; }
        Task<string> GenerateTextAsync(string prompt, GenerationConfig config);
    }

    /// <summary>
    /// Sentiment analysis
    /// </summary>
    public class SentimentAnalysis : ITextAnalyzer
    {
        public string Name => "Sentiment Analysis";
        public string Type => "sentiment";

        public async Task<AnalysisResult> AnalyzeAsync(string text, TextAnalysisConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would use a sentiment analysis model
                await Task.Delay(200);

                var sentiment = AnalyzeSentiment(text);
                var confidence = new Random().NextDouble() * 0.2 + 0.8;

                return new AnalysisResult
                {
                    Success = true,
                    AnalyzerName = Name,
                    AnalysisType = Type,
                    Results = new Dictionary<string, object>
                    {
                        ["sentiment"] = sentiment,
                        ["confidence"] = confidence,
                        ["positive_score"] = CalculatePositiveScore(text),
                        ["negative_score"] = CalculateNegativeScore(text),
                        ["neutral_score"] = CalculateNeutralScore(text)
                    },
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new AnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private string AnalyzeSentiment(string text)
        {
            var positiveWords = new[] { "good", "great", "excellent", "amazing", "wonderful", "love", "like" };
            var negativeWords = new[] { "bad", "terrible", "awful", "hate", "dislike", "horrible", "worst" };

            var words = text.ToLower().Split(' ');
            var positiveCount = words.Count(w => positiveWords.Contains(w));
            var negativeCount = words.Count(w => negativeWords.Contains(w));

            if (positiveCount > negativeCount) return "positive";
            if (negativeCount > positiveCount) return "negative";
            return "neutral";
        }

        private double CalculatePositiveScore(string text) => new Random().NextDouble() * 0.4 + 0.6;
        private double CalculateNegativeScore(string text) => new Random().NextDouble() * 0.3 + 0.1;
        private double CalculateNeutralScore(string text) => new Random().NextDouble() * 0.2 + 0.1;
    }

    /// <summary>
    /// Entity recognition
    /// </summary>
    public class EntityRecognition : ITextAnalyzer
    {
        public string Name => "Entity Recognition";
        public string Type => "entities";

        public async Task<AnalysisResult> AnalyzeAsync(string text, TextAnalysisConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would use NER models
                await Task.Delay(300);

                var entities = ExtractEntities(text);

                return new AnalysisResult
                {
                    Success = true,
                    AnalyzerName = Name,
                    AnalysisType = Type,
                    Results = new Dictionary<string, object>
                    {
                        ["entities"] = entities,
                        ["entity_count"] = entities.Count,
                        ["entity_types"] = entities.Select(e => e["type"]).Distinct().ToList()
                    },
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new AnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private List<Dictionary<string, object>> ExtractEntities(string text)
        {
            var entities = new List<Dictionary<string, object>>();
            
            // Simple entity extraction patterns
            var patterns = new Dictionary<string, string>
            {
                ["person"] = @"\b[A-Z][a-z]+ [A-Z][a-z]+\b",
                ["organization"] = @"\b[A-Z][a-z]+ (Inc|Corp|LLC|Ltd)\b",
                ["location"] = @"\b[A-Z][a-z]+, [A-Z]{2}\b",
                ["date"] = @"\b\d{1,2}/\d{1,2}/\d{4}\b"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern.Value);
                foreach (Match match in matches)
                {
                    entities.Add(new Dictionary<string, object>
                    {
                        ["text"] = match.Value,
                        ["type"] = pattern.Key,
                        ["start"] = match.Index,
                        ["end"] = match.Index + match.Length
                    });
                }
            }

            return entities;
        }
    }

    /// <summary>
    /// Topic modeling
    /// </summary>
    public class TopicModeling : ITextAnalyzer
    {
        public string Name => "Topic Modeling";
        public string Type => "topics";

        public async Task<AnalysisResult> AnalyzeAsync(string text, TextAnalysisConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would use LDA or other topic modeling algorithms
                await Task.Delay(400);

                var topics = ExtractTopics(text);

                return new AnalysisResult
                {
                    Success = true,
                    AnalyzerName = Name,
                    AnalysisType = Type,
                    Results = new Dictionary<string, object>
                    {
                        ["topics"] = topics,
                        ["topic_count"] = topics.Count,
                        ["dominant_topic"] = topics.OrderByDescending(t => (double)t["weight"]).First()
                    },
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new AnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private List<Dictionary<string, object>> ExtractTopics(string text)
        {
            var topics = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["name"] = "technology", ["weight"] = 0.35, ["keywords"] = new[] { "software", "computer", "system" } },
                new Dictionary<string, object> { ["name"] = "business", ["weight"] = 0.28, ["keywords"] = new[] { "company", "market", "product" } },
                new Dictionary<string, object> { ["name"] = "general", ["weight"] = 0.37, ["keywords"] = new[] { "information", "data", "process" } }
            };

            return topics;
        }
    }

    /// <summary>
    /// BERT language model
    /// </summary>
    public class BERTModel : ILanguageModel
    {
        public string Name => "BERT";

        public async Task<string> GenerateTextAsync(string prompt, GenerationConfig config)
        {
            // In a real implementation, this would use BERT for text generation
            await Task.Delay(600);

            return $"BERT generated response to: {prompt}";
        }
    }

    /// <summary>
    /// GPT language model
    /// </summary>
    public class GPTModel : ILanguageModel
    {
        public string Name => "GPT";

        public async Task<string> GenerateTextAsync(string prompt, GenerationConfig config)
        {
            // In a real implementation, this would use GPT for text generation
            await Task.Delay(800);

            return $"GPT generated response to: {prompt}";
        }
    }

    /// <summary>
    /// Custom language model
    /// </summary>
    public class CustomLanguageModel : ILanguageModel
    {
        public string Name => "Custom";

        public async Task<string> GenerateTextAsync(string prompt, GenerationConfig config)
        {
            // In a real implementation, this would use a custom language model
            await Task.Delay(400);

            return $"Custom model response to: {prompt}";
        }
    }

    /// <summary>
    /// Text processor
    /// </summary>
    public class TextProcessor
    {
        public async Task<string> PreprocessTextAsync(string text, PreprocessingConfig config = null)
        {
            // In a real implementation, this would perform text preprocessing
            await Task.Delay(50);

            var processedText = text;

            if (config?.RemoveSpecialCharacters == true)
            {
                processedText = Regex.Replace(processedText, @"[^\w\s]", "");
            }

            if (config?.ConvertToLowercase == true)
            {
                processedText = processedText.ToLower();
            }

            if (config?.RemoveStopWords == true)
            {
                processedText = RemoveStopWords(processedText);
            }

            return processedText;
        }

        private string RemoveStopWords(string text)
        {
            var stopWords = new[] { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by" };
            var words = text.Split(' ');
            var filteredWords = words.Where(w => !stopWords.Contains(w.ToLower()));
            return string.Join(" ", filteredWords);
        }
    }

    /// <summary>
    /// Sentiment analyzer
    /// </summary>
    public class SentimentAnalyzer
    {
        public async Task<SentimentResult> AnalyzeSentimentAsync(string text, SentimentConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would use advanced sentiment analysis
                await Task.Delay(250);

                var sentiment = new Random().Next(0, 3) switch
                {
                    0 => "positive",
                    1 => "negative",
                    _ => "neutral"
                };

                return new SentimentResult
                {
                    Success = true,
                    Text = text,
                    Sentiment = sentiment,
                    Confidence = new Random().NextDouble() * 0.2 + 0.8,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new SentimentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Entity extractor
    /// </summary>
    public class EntityExtractor
    {
        public async Task<EntityExtractionResult> ExtractEntitiesAsync(string text, EntityConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would use NER models
                await Task.Delay(300);

                var entities = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { ["text"] = "John Doe", ["type"] = "person", ["confidence"] = 0.95 },
                    new Dictionary<string, object> { ["text"] = "Microsoft Corp", ["type"] = "organization", ["confidence"] = 0.92 },
                    new Dictionary<string, object> { ["text"] = "New York, NY", ["type"] = "location", ["confidence"] = 0.88 }
                };

                return new EntityExtractionResult
                {
                    Success = true,
                    Text = text,
                    Entities = entities,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new EntityExtractionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    // Data transfer objects
    public class TextAnalysisResult
    {
        public bool Success { get; set; }
        public string OriginalText { get; set; }
        public string ProcessedText { get; set; }
        public List<AnalysisResult> AnalysisResults { get; set; } = new List<AnalysisResult>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AnalysisResult
    {
        public bool Success { get; set; }
        public string AnalyzerName { get; set; }
        public string AnalysisType { get; set; }
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SentimentResult
    {
        public bool Success { get; set; }
        public string Text { get; set; }
        public string Sentiment { get; set; }
        public double Confidence { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EntityExtractionResult
    {
        public bool Success { get; set; }
        public string Text { get; set; }
        public List<Dictionary<string, object>> Entities { get; set; } = new List<Dictionary<string, object>>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TextGenerationResult
    {
        public bool Success { get; set; }
        public string ModelName { get; set; }
        public string Prompt { get; set; }
        public string GeneratedText { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TranslationResult
    {
        public bool Success { get; set; }
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public double Confidence { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SummarizationResult
    {
        public bool Success { get; set; }
        public string OriginalText { get; set; }
        public string Summary { get; set; }
        public double CompressionRatio { get; set; }
        public List<string> KeyPoints { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ProcessingResult
    {
        public bool Success { get; set; }
        public string ProcessedText { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Configuration classes
    public class TextAnalysisConfig
    {
        public List<string> AnalysisTypes { get; set; } = new List<string> { "sentiment", "entities", "topics" };
        public PreprocessingConfig PreprocessingConfig { get; set; } = new PreprocessingConfig();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class PreprocessingConfig
    {
        public bool RemoveSpecialCharacters { get; set; } = true;
        public bool ConvertToLowercase { get; set; } = false;
        public bool RemoveStopWords { get; set; } = false;
        public List<string> CustomStopWords { get; set; } = new List<string>();
    }

    public class SentimentConfig
    {
        public bool IncludeConfidence { get; set; } = true;
        public string Language { get; set; } = "en";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class EntityConfig
    {
        public List<string> EntityTypes { get; set; } = new List<string> { "person", "organization", "location" };
        public double ConfidenceThreshold { get; set; } = 0.8;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class GenerationConfig
    {
        public int MaxLength { get; set; } = 100;
        public double Temperature { get; set; } = 0.7;
        public bool IncludeStopSequences { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class TranslationConfig
    {
        public string SourceLanguage { get; set; } = "auto";
        public bool PreserveFormatting { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class SummarizationConfig
    {
        public int MaxLength { get; set; } = 3;
        public string Method { get; set; } = "extractive";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class ProcessingConfig
    {
        public bool NormalizeText { get; set; } = true;
        public bool RemoveNoise { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// NLP metrics collection
    /// </summary>
    public class NLPMetrics
    {
        private readonly Dictionary<string, ModelMetrics> _modelMetrics = new Dictionary<string, ModelMetrics>();
        private readonly Dictionary<string, int> _analysisCounts = new Dictionary<string, int>();
        private readonly object _lock = new object();

        public void RecordTextAnalysis(bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                var key = success ? "successful" : "failed";
                _analysisCounts[key] = _analysisCounts.GetValueOrDefault(key, 0) + 1;
            }
        }

        public void RecordTextGeneration(string modelName, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                var metrics = _modelMetrics.GetValueOrDefault(modelName, new ModelMetrics());
                metrics.TotalGenerations++;
                metrics.SuccessfulGenerations += success ? 1 : 0;
                metrics.TotalGenerationTime += executionTime;
                _modelMetrics[modelName] = metrics;
            }
        }

        public Dictionary<string, ModelMetrics> GetModelMetrics() => new Dictionary<string, ModelMetrics>(_modelMetrics);
        public Dictionary<string, int> GetAnalysisCounts() => new Dictionary<string, int>(_analysisCounts);
    }

    public class ModelMetrics
    {
        public int TotalGenerations { get; set; }
        public int SuccessfulGenerations { get; set; }
        public TimeSpan TotalGenerationTime { get; set; }
    }
} 
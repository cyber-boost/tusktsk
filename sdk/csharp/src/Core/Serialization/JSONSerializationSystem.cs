using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Linq;

namespace TuskLang.Serialization
{
    /// <summary>
    /// Production-ready JSON.NET serialization system for TuskTsk
    /// Handles all data types with performance optimization and error handling
    /// </summary>
    public class JSONSerializationSystem : IDisposable
    {
        private readonly JsonSerializerSettings _defaultSettings;
        private readonly JsonSerializer _serializer;
        private readonly ILogger<JSONSerializationSystem> _logger;
        private readonly Dictionary<Type, JsonConverter> _customConverters;
        private bool _disposed = false;

        public JSONSerializationSystem(ILogger<JSONSerializationSystem> logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<JSONSerializationSystem>.Instance;
            _customConverters = new Dictionary<Type, JsonConverter>();
            
            _defaultSettings = CreateDefaultSettings();
            _serializer = JsonSerializer.Create(_defaultSettings);
        }

        #region Configuration

        /// <summary>
        /// Create default JSON serializer settings optimized for TuskTsk
        /// </summary>
        private JsonSerializerSettings CreateDefaultSettings()
        {
            var settings = new JsonSerializerSettings
            {
                // Formatting
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                
                // Null value handling
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                
                // Type handling
                TypeNameHandling = TypeNameHandling.None,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                
                // Property handling
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                
                // Error handling
                Error = HandleSerializationError,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                
                // Performance optimizations
                MaxDepth = 64,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                
                // Cultural settings
                Culture = CultureInfo.InvariantCulture,
                
                // Converters
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter(new CamelCaseNamingStrategy()),
                    new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ" },
                    new TuskConfigurationConverter(),
                    new TuskDataConverter(),
                    new DictionaryConverter(),
                    new TimeSpanConverter(),
                    new GuidConverter()
                }
            };

            return settings;
        }

        /// <summary>
        /// Handle serialization errors
        /// </summary>
        private void HandleSerializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
        {
            _logger.LogError(e.ErrorContext.Error, $"JSON serialization error at path: {e.ErrorContext.Path}");
            e.ErrorContext.Handled = true;
        }

        /// <summary>
        /// Register custom converter for specific type
        /// </summary>
        public void RegisterConverter<T>(JsonConverter converter)
        {
            _customConverters[typeof(T)] = converter;
            _defaultSettings.Converters.Add(converter);
            
            _logger.LogDebug($"Registered custom converter for type {typeof(T).Name}");
        }

        /// <summary>
        /// Create settings with custom options
        /// </summary>
        public JsonSerializerSettings CreateSettings(Action<JsonSerializerSettings> configure = null)
        {
            var settings = JsonConvert.DefaultSettings?.Invoke() ?? CreateDefaultSettings();
            configure?.Invoke(settings);
            return settings;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        public async Task<string> SerializeAsync<T>(T obj, JsonSerializerSettings settings = null)
        {
            try
            {
                return await Task.Run(() => 
                {
                    var effectiveSettings = settings ?? _defaultSettings;
                    return JsonConvert.SerializeObject(obj, effectiveSettings);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error serializing object of type {typeof(T).Name}");
                throw new SerializationException($"Failed to serialize {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Serialize object to JSON string synchronously
        /// </summary>
        public string Serialize<T>(T obj, JsonSerializerSettings settings = null)
        {
            try
            {
                var effectiveSettings = settings ?? _defaultSettings;
                return JsonConvert.SerializeObject(obj, effectiveSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error serializing object of type {typeof(T).Name}");
                throw new SerializationException($"Failed to serialize {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Serialize object to formatted JSON string
        /// </summary>
        public async Task<string> SerializePrettyAsync<T>(T obj, JsonSerializerSettings settings = null)
        {
            try
            {
                return await Task.Run(() => 
                {
                    var effectiveSettings = settings ?? _defaultSettings;
                    effectiveSettings.Formatting = Formatting.Indented;
                    return JsonConvert.SerializeObject(obj, effectiveSettings);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pretty-serializing object of type {typeof(T).Name}");
                throw new SerializationException($"Failed to pretty-serialize {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Serialize object to JSON bytes
        /// </summary>
        public async Task<byte[]> SerializeToBytesAsync<T>(T obj, JsonSerializerSettings settings = null)
        {
            try
            {
                var json = await SerializeAsync(obj, settings);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error serializing object of type {typeof(T).Name} to bytes");
                throw new SerializationException($"Failed to serialize {typeof(T).Name} to bytes", ex);
            }
        }

        /// <summary>
        /// Serialize object to file
        /// </summary>
        public async Task SerializeToFileAsync<T>(T obj, string filePath, JsonSerializerSettings settings = null)
        {
            try
            {
                var json = await SerializeAsync(obj, settings);
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                
                _logger.LogDebug($"Serialized {typeof(T).Name} to file: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error serializing object of type {typeof(T).Name} to file: {filePath}");
                throw new SerializationException($"Failed to serialize {typeof(T).Name} to file", ex);
            }
        }

        /// <summary>
        /// Serialize object to stream
        /// </summary>
        public async Task SerializeToStreamAsync<T>(T obj, Stream stream, JsonSerializerSettings settings = null)
        {
            try
            {
                using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
                using var jsonWriter = new JsonTextWriter(writer);
                
                var effectiveSerializer = settings != null ? JsonSerializer.Create(settings) : _serializer;
                effectiveSerializer.Serialize(jsonWriter, obj);
                
                await writer.FlushAsync();
                _logger.LogDebug($"Serialized {typeof(T).Name} to stream");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error serializing object of type {typeof(T).Name} to stream");
                throw new SerializationException($"Failed to serialize {typeof(T).Name} to stream", ex);
            }
        }

        #endregion

        #region Deserialization

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        public async Task<T> DeserializeAsync<T>(string json, JsonSerializerSettings settings = null)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    return default(T);

                return await Task.Run(() => 
                {
                    var effectiveSettings = settings ?? _defaultSettings;
                    return JsonConvert.DeserializeObject<T>(json, effectiveSettings);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deserializing JSON to type {typeof(T).Name}");
                throw new SerializationException($"Failed to deserialize to {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Deserialize JSON string to object synchronously
        /// </summary>
        public T Deserialize<T>(string json, JsonSerializerSettings settings = null)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    return default(T);

                var effectiveSettings = settings ?? _defaultSettings;
                return JsonConvert.DeserializeObject<T>(json, effectiveSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deserializing JSON to type {typeof(T).Name}");
                throw new SerializationException($"Failed to deserialize to {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Deserialize JSON bytes to object
        /// </summary>
        public async Task<T> DeserializeFromBytesAsync<T>(byte[] bytes, JsonSerializerSettings settings = null)
        {
            try
            {
                if (bytes == null || bytes.Length == 0)
                    return default(T);

                var json = Encoding.UTF8.GetString(bytes);
                return await DeserializeAsync<T>(json, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deserializing bytes to type {typeof(T).Name}");
                throw new SerializationException($"Failed to deserialize bytes to {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Deserialize JSON file to object
        /// </summary>
        public async Task<T> DeserializeFromFileAsync<T>(string filePath, JsonSerializerSettings settings = null)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var result = await DeserializeAsync<T>(json, settings);
                
                _logger.LogDebug($"Deserialized {typeof(T).Name} from file: {filePath}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deserializing file to type {typeof(T).Name}: {filePath}");
                throw new SerializationException($"Failed to deserialize file to {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Deserialize JSON stream to object
        /// </summary>
        public async Task<T> DeserializeFromStreamAsync<T>(Stream stream, JsonSerializerSettings settings = null)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                using var jsonReader = new JsonTextReader(reader);
                
                var effectiveSerializer = settings != null ? JsonSerializer.Create(settings) : _serializer;
                var result = effectiveSerializer.Deserialize<T>(jsonReader);
                
                _logger.LogDebug($"Deserialized {typeof(T).Name} from stream");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deserializing stream to type {typeof(T).Name}");
                throw new SerializationException($"Failed to deserialize stream to {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Try deserialize with error handling
        /// </summary>
        public bool TryDeserialize<T>(string json, out T result, JsonSerializerSettings settings = null)
        {
            result = default(T);
            try
            {
                result = Deserialize<T>(json, settings);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to deserialize JSON to type {typeof(T).Name}");
                return false;
            }
        }

        #endregion

        #region Type Conversion

        /// <summary>
        /// Convert object from one type to another via JSON
        /// </summary>
        public async Task<TTarget> ConvertAsync<TSource, TTarget>(TSource source, JsonSerializerSettings settings = null)
        {
            try
            {
                var json = await SerializeAsync(source, settings);
                return await DeserializeAsync<TTarget>(json, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error converting from {typeof(TSource).Name} to {typeof(TTarget).Name}");
                throw new SerializationException($"Failed to convert from {typeof(TSource).Name} to {typeof(TTarget).Name}", ex);
            }
        }

        /// <summary>
        /// Deep clone object via JSON serialization
        /// </summary>
        public async Task<T> DeepCloneAsync<T>(T obj, JsonSerializerSettings settings = null)
        {
            try
            {
                var json = await SerializeAsync(obj, settings);
                return await DeserializeAsync<T>(json, settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deep cloning object of type {typeof(T).Name}");
                throw new SerializationException($"Failed to deep clone {typeof(T).Name}", ex);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate JSON string
        /// </summary>
        public bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JsonConvert.DeserializeObject(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate and get JSON schema information
        /// </summary>
        public JsonValidationResult ValidateJson<T>(string json)
        {
            var result = new JsonValidationResult();
            
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    result.IsValid = false;
                    result.Errors.Add("JSON string is null or empty");
                    return result;
                }

                var obj = JsonConvert.DeserializeObject<T>(json, _defaultSettings);
                result.IsValid = true;
                result.DeserializedObject = obj;
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Errors.Add($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Performance Metrics

        /// <summary>
        /// Measure serialization performance
        /// </summary>
        public async Task<SerializationPerformanceResult> MeasureSerializationAsync<T>(T obj, int iterations = 1000)
        {
            var result = new SerializationPerformanceResult();
            var times = new List<TimeSpan>();

            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await SerializeAsync(obj);
                stopwatch.Stop();
                times.Add(stopwatch.Elapsed);
            }

            result.TotalIterations = iterations;
            result.TotalTime = TimeSpan.FromTicks(times.Sum(t => t.Ticks));
            result.AverageTime = TimeSpan.FromTicks(result.TotalTime.Ticks / iterations);
            result.MinTime = times.Min();
            result.MaxTime = times.Max();

            _logger.LogInformation($"Serialization performance for {typeof(T).Name}: {result.AverageTime.TotalMilliseconds:F2}ms average");
            return result;
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    #region Custom Converters

    /// <summary>
    /// Custom converter for TuskConfiguration
    /// </summary>
    public class TuskConfigurationConverter : JsonConverter<Dictionary<string, object>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, object> value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        public override Dictionary<string, object> ReadJson(JsonReader reader, Type objectType, Dictionary<string, object> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var result = new Dictionary<string, object>();
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propertyName = reader.Value.ToString();
                    reader.Read();
                    result[propertyName] = serializer.Deserialize(reader);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Custom converter for TuskData
    /// </summary>
    public class TuskDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(object) || objectType.IsInterface;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // Handle different data types appropriately
            switch (value)
            {
                case string s:
                    writer.WriteValue(s);
                    break;
                case int i:
                    writer.WriteValue(i);
                    break;
                case double d:
                    writer.WriteValue(d);
                    break;
                case bool b:
                    writer.WriteValue(b);
                    break;
                case DateTime dt:
                    writer.WriteValue(dt);
                    break;
                default:
                    serializer.Serialize(writer, value);
                    break;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    return reader.Value;
                case JsonToken.Integer:
                    return Convert.ToInt64(reader.Value);
                case JsonToken.Float:
                    return Convert.ToDouble(reader.Value);
                case JsonToken.Boolean:
                    return Convert.ToBoolean(reader.Value);
                case JsonToken.Date:
                    return Convert.ToDateTime(reader.Value);
                case JsonToken.Null:
                    return null;
                default:
                    return serializer.Deserialize(reader);
            }
        }
    }

    /// <summary>
    /// Enhanced dictionary converter
    /// </summary>
    public class DictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, object> value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            foreach (var kvp in value.OrderBy(x => x.Key))
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        public override Dictionary<string, object> ReadJson(JsonReader reader, Type objectType, Dictionary<string, object> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var dictionary = new Dictionary<string, object>();
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var key = reader.Value.ToString();
                    reader.Read();
                    dictionary[key] = serializer.Deserialize(reader);
                }
            }

            return dictionary;
        }
    }

    /// <summary>
    /// TimeSpan converter for better JSON representation
    /// </summary>
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString(@"d\.hh\:mm\:ss\.fff"));
        }

        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = reader.Value?.ToString();
            return TimeSpan.TryParse(value, out var result) ? result : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// GUID converter for consistent formatting
    /// </summary>
    public class GuidConverter : JsonConverter<Guid>
    {
        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString("D"));
        }

        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = reader.Value?.ToString();
            return Guid.TryParse(value, out var result) ? result : Guid.Empty;
        }
    }

    #endregion

    #region Result Classes

    /// <summary>
    /// JSON validation result
    /// </summary>
    public class JsonValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public object DeserializedObject { get; set; }
    }

    /// <summary>
    /// Serialization performance result
    /// </summary>
    public class SerializationPerformanceResult
    {
        public int TotalIterations { get; set; }
        public TimeSpan TotalTime { get; set; }
        public TimeSpan AverageTime { get; set; }
        public TimeSpan MinTime { get; set; }
        public TimeSpan MaxTime { get; set; }
    }

    #endregion

    #region Extensions

    /// <summary>
    /// Extension methods for JSON serialization
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// Convert object to JSON string
        /// </summary>
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None);
        }

        /// <summary>
        /// Convert object to pretty JSON string
        /// </summary>
        public static string ToPrettyJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        /// <summary>
        /// Parse JSON string to object
        /// </summary>
        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Try parse JSON string to object
        /// </summary>
        public static bool TryFromJson<T>(this string json, out T result)
        {
            result = default(T);
            try
            {
                result = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    #endregion
} 
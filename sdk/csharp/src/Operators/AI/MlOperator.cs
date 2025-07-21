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
    /// Machine Learning operator for TuskLang
    /// Provides ML operations including model training, prediction, and evaluation
    /// </summary>
    public class MlOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _apiKey;
        private string _endpoint;
        private Dictionary<string, object> _models;

        public MlOperator() : base("ml", "Machine Learning operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to ML service", new[] { "api_key", "endpoint" });
            RegisterMethod("disconnect", "Disconnect from ML service", new string[0]);
            RegisterMethod("train", "Train ML model", new[] { "algorithm", "data", "parameters", "target" });
            RegisterMethod("predict", "Make prediction", new[] { "model_id", "features" });
            RegisterMethod("evaluate", "Evaluate model", new[] { "model_id", "test_data", "metrics" });
            RegisterMethod("save_model", "Save model", new[] { "model_id", "name", "description" });
            RegisterMethod("load_model", "Load model", new[] { "model_id" });
            RegisterMethod("delete_model", "Delete model", new[] { "model_id" });
            RegisterMethod("list_models", "List all models", new string[0]);
            RegisterMethod("get_model_info", "Get model information", new[] { "model_id" });
            RegisterMethod("update_model", "Update model", new[] { "model_id", "parameters" });
            RegisterMethod("export_model", "Export model", new[] { "model_id", "format" });
            RegisterMethod("import_model", "Import model", new[] { "model_data", "format", "name" });
            RegisterMethod("hyperparameter_tuning", "Tune hyperparameters", new[] { "algorithm", "data", "parameter_grid" });
            RegisterMethod("cross_validation", "Perform cross validation", new[] { "algorithm", "data", "folds" });
            RegisterMethod("feature_importance", "Get feature importance", new[] { "model_id" });
            RegisterMethod("confusion_matrix", "Get confusion matrix", new[] { "model_id", "test_data" });
            RegisterMethod("roc_curve", "Get ROC curve", new[] { "model_id", "test_data" });
            RegisterMethod("get_metrics", "Get model metrics", new[] { "model_id" });
            RegisterMethod("test_connection", "Test ML connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing ML operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "train":
                        return await TrainAsync(parameters);
                    case "predict":
                        return await PredictAsync(parameters);
                    case "evaluate":
                        return await EvaluateAsync(parameters);
                    case "save_model":
                        return await SaveModelAsync(parameters);
                    case "load_model":
                        return await LoadModelAsync(parameters);
                    case "delete_model":
                        return await DeleteModelAsync(parameters);
                    case "list_models":
                        return await ListModelsAsync();
                    case "get_model_info":
                        return await GetModelInfoAsync(parameters);
                    case "update_model":
                        return await UpdateModelAsync(parameters);
                    case "export_model":
                        return await ExportModelAsync(parameters);
                    case "import_model":
                        return await ImportModelAsync(parameters);
                    case "hyperparameter_tuning":
                        return await HyperparameterTuningAsync(parameters);
                    case "cross_validation":
                        return await CrossValidationAsync(parameters);
                    case "feature_importance":
                        return await FeatureImportanceAsync(parameters);
                    case "confusion_matrix":
                        return await ConfusionMatrixAsync(parameters);
                    case "roc_curve":
                        return await RocCurveAsync(parameters);
                    case "get_metrics":
                        return await GetMetricsAsync(parameters);
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown ML method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing ML method {method}: {ex.Message}");
                throw new OperatorException($"ML operation failed: {ex.Message}", "ML_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var apiKey = GetRequiredParameter<string>(parameters, "api_key");
            var endpoint = GetParameter<string>(parameters, "endpoint", "https://api.ml-service.com/v1");

            try
            {
                _apiKey = apiKey;
                _endpoint = endpoint;
                _models = new Dictionary<string, object>();

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                LogInfo($"Connected to ML service: {endpoint}");
                return new { success = true, endpoint };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to ML service: {ex.Message}");
                throw new OperatorException($"ML connection failed: {ex.Message}", "ML_CONNECTION_ERROR", ex);
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
                _models = null;

                LogInfo("Disconnected from ML service");
                return new { success = true, message = "Disconnected from ML service" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from ML service: {ex.Message}");
                throw new OperatorException($"ML disconnect failed: {ex.Message}", "ML_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> TrainAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var algorithm = GetRequiredParameter<string>(parameters, "algorithm");
            var data = GetRequiredParameter<object[]>(parameters, "data");
            var parameters2 = GetParameter<Dictionary<string, object>>(parameters, "parameters", new Dictionary<string, object>());
            var target = GetRequiredParameter<string>(parameters, "target");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "algorithm", algorithm },
                    { "data", data },
                    { "target", target },
                    { "parameters", parameters2 }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/train";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Train failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var modelId = Guid.NewGuid().ToString();
                _models[modelId] = resultObj;

                LogInfo($"Successfully trained ML model: {modelId}");
                return new { success = true, modelId, algorithm, target, model = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error training ML model: {ex.Message}");
                throw new OperatorException($"ML train failed: {ex.Message}", "ML_TRAIN_ERROR", ex);
            }
        }

        private async Task<object> PredictAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");
            var features = GetRequiredParameter<Dictionary<string, object>>(parameters, "features");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var requestData = new Dictionary<string, object>
                {
                    { "model_id", modelId },
                    { "features", features }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/predict";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Predict failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully made prediction with model {modelId}");
                return new { success = true, modelId, features, prediction = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error making prediction: {ex.Message}");
                throw new OperatorException($"ML predict failed: {ex.Message}", "ML_PREDICT_ERROR", ex);
            }
        }

        private async Task<object> EvaluateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");
            var testData = GetRequiredParameter<object[]>(parameters, "test_data");
            var metrics = GetParameter<string[]>(parameters, "metrics", new[] { "accuracy", "precision", "recall", "f1" });

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var requestData = new Dictionary<string, object>
                {
                    { "model_id", modelId },
                    { "test_data", testData },
                    { "metrics", metrics }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/evaluate";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Evaluate failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully evaluated model {modelId}");
                return new { success = true, modelId, metrics, evaluation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error evaluating model: {ex.Message}");
                throw new OperatorException($"ML evaluate failed: {ex.Message}", "ML_EVALUATE_ERROR", ex);
            }
        }

        private async Task<object> SaveModelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");
            var name = GetRequiredParameter<string>(parameters, "name");
            var description = GetParameter<string>(parameters, "description", "");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var requestData = new Dictionary<string, object>
                {
                    { "model_id", modelId },
                    { "name", name },
                    { "description", description }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/models/save";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Save model failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully saved model {modelId} as {name}");
                return new { success = true, modelId, name, description, saved = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error saving model: {ex.Message}");
                throw new OperatorException($"ML save model failed: {ex.Message}", "ML_SAVE_MODEL_ERROR", ex);
            }
        }

        private async Task<object> LoadModelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");

            try
            {
                var url = $"{_endpoint}/models/{modelId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Load model failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                _models[modelId] = resultObj;

                LogInfo($"Successfully loaded model {modelId}");
                return new { success = true, modelId, model = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error loading model: {ex.Message}");
                throw new OperatorException($"ML load model failed: {ex.Message}", "ML_LOAD_MODEL_ERROR", ex);
            }
        }

        private async Task<object> DeleteModelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");

            try
            {
                var url = $"{_endpoint}/models/{modelId}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete model failed: {response.StatusCode} - {errorContent}");
                }

                _models.Remove(modelId);

                LogInfo($"Successfully deleted model {modelId}");
                return new { success = true, modelId };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting model: {ex.Message}");
                throw new OperatorException($"ML delete model failed: {ex.Message}", "ML_DELETE_MODEL_ERROR", ex);
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
                throw new OperatorException($"ML list models failed: {ex.Message}", "ML_LIST_MODELS_ERROR", ex);
            }
        }

        private async Task<object> GetModelInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var modelInfo = _models[modelId] as Dictionary<string, object>;
                return new { success = true, modelId, info = modelInfo };
            }
            catch (Exception ex)
            {
                LogError($"Error getting model info: {ex.Message}");
                throw new OperatorException($"ML get model info failed: {ex.Message}", "ML_GET_MODEL_INFO_ERROR", ex);
            }
        }

        private async Task<object> UpdateModelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");
            var parameters2 = GetRequiredParameter<Dictionary<string, object>>(parameters, "parameters");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var requestData = new Dictionary<string, object>
                {
                    { "model_id", modelId },
                    { "parameters", parameters2 }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/models/{modelId}";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Update model failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                _models[modelId] = resultObj;

                LogInfo($"Successfully updated model {modelId}");
                return new { success = true, modelId, parameters = parameters2, model = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error updating model: {ex.Message}");
                throw new OperatorException($"ML update model failed: {ex.Message}", "ML_UPDATE_MODEL_ERROR", ex);
            }
        }

        private async Task<object> ExportModelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");
            var format = GetParameter<string>(parameters, "format", "pickle");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var requestData = new Dictionary<string, object>
                {
                    { "model_id", modelId },
                    { "format", format }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/models/{modelId}/export";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Export model failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully exported model {modelId} in {format} format");
                return new { success = true, modelId, format, exported = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error exporting model: {ex.Message}");
                throw new OperatorException($"ML export model failed: {ex.Message}", "ML_EXPORT_MODEL_ERROR", ex);
            }
        }

        private async Task<object> ImportModelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelData = GetRequiredParameter<string>(parameters, "model_data");
            var format = GetParameter<string>(parameters, "format", "pickle");
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "model_data", modelData },
                    { "format", format },
                    { "name", name }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/models/import";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Import model failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var modelId = Guid.NewGuid().ToString();
                _models[modelId] = resultObj;

                LogInfo($"Successfully imported model as {name}");
                return new { success = true, modelId, name, format, imported = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error importing model: {ex.Message}");
                throw new OperatorException($"ML import model failed: {ex.Message}", "ML_IMPORT_MODEL_ERROR", ex);
            }
        }

        private async Task<object> HyperparameterTuningAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var algorithm = GetRequiredParameter<string>(parameters, "algorithm");
            var data = GetRequiredParameter<object[]>(parameters, "data");
            var parameterGrid = GetRequiredParameter<Dictionary<string, object[]>>(parameters, "parameter_grid");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "algorithm", algorithm },
                    { "data", data },
                    { "parameter_grid", parameterGrid }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/tune";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Hyperparameter tuning failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully completed hyperparameter tuning");
                return new { success = true, algorithm, parameterGrid, tuning = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error in hyperparameter tuning: {ex.Message}");
                throw new OperatorException($"ML hyperparameter tuning failed: {ex.Message}", "ML_HYPERPARAMETER_TUNING_ERROR", ex);
            }
        }

        private async Task<object> CrossValidationAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var algorithm = GetRequiredParameter<string>(parameters, "algorithm");
            var data = GetRequiredParameter<object[]>(parameters, "data");
            var folds = GetParameter<int>(parameters, "folds", 5);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "algorithm", algorithm },
                    { "data", data },
                    { "folds", folds }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/cross_validate";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Cross validation failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully completed cross validation with {folds} folds");
                return new { success = true, algorithm, folds, validation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error in cross validation: {ex.Message}");
                throw new OperatorException($"ML cross validation failed: {ex.Message}", "ML_CROSS_VALIDATION_ERROR", ex);
            }
        }

        private async Task<object> FeatureImportanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var url = $"{_endpoint}/models/{modelId}/feature_importance";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Feature importance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved feature importance for model {modelId}");
                return new { success = true, modelId, featureImportance = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting feature importance: {ex.Message}");
                throw new OperatorException($"ML feature importance failed: {ex.Message}", "ML_FEATURE_IMPORTANCE_ERROR", ex);
            }
        }

        private async Task<object> ConfusionMatrixAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");
            var testData = GetRequiredParameter<object[]>(parameters, "test_data");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var requestData = new Dictionary<string, object>
                {
                    { "model_id", modelId },
                    { "test_data", testData }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/models/{modelId}/confusion_matrix";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Confusion matrix failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully generated confusion matrix for model {modelId}");
                return new { success = true, modelId, confusionMatrix = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error generating confusion matrix: {ex.Message}");
                throw new OperatorException($"ML confusion matrix failed: {ex.Message}", "ML_CONFUSION_MATRIX_ERROR", ex);
            }
        }

        private async Task<object> RocCurveAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");
            var testData = GetRequiredParameter<object[]>(parameters, "test_data");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var requestData = new Dictionary<string, object>
                {
                    { "model_id", modelId },
                    { "test_data", testData }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/models/{modelId}/roc_curve";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"ROC curve failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully generated ROC curve for model {modelId}");
                return new { success = true, modelId, rocCurve = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error generating ROC curve: {ex.Message}");
                throw new OperatorException($"ML ROC curve failed: {ex.Message}", "ML_ROC_CURVE_ERROR", ex);
            }
        }

        private async Task<object> GetMetricsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var modelId = GetRequiredParameter<string>(parameters, "model_id");

            try
            {
                if (!_models.ContainsKey(modelId))
                {
                    throw new Exception($"Model {modelId} not found");
                }

                var url = $"{_endpoint}/models/{modelId}/metrics";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get metrics failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved metrics for model {modelId}");
                return new { success = true, modelId, metrics = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting metrics: {ex.Message}");
                throw new OperatorException($"ML get metrics failed: {ex.Message}", "ML_GET_METRICS_ERROR", ex);
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
                LogError($"Error testing ML connection: {ex.Message}");
                throw new OperatorException($"ML connection test failed: {ex.Message}", "ML_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_apiKey))
            {
                throw new OperatorException("Not connected to ML service", "ML_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
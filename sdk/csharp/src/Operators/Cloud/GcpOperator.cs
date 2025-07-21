using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Linq;

namespace TuskLang.Operators.Cloud
{
    /// <summary>
    /// Google Cloud Platform operator for TuskLang
    /// Provides GCP cloud operations including Compute Engine, Cloud Storage, Cloud Functions, and other GCP services
    /// </summary>
    public class GcpOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _projectId;
        private string _credentials;
        private string _endpoint;

        public GcpOperator()
        {
            _httpClient = new HttpClient();
            RequiredFields = new List<string> { "project_id", "credentials" };
            OptionalFields = new List<string> { "endpoint" };
        }

        public override string GetName()
        {
            return "gcp";
        }

        protected override string GetDescription()
        {
            return "Google Cloud Platform operations";
        }

        public override async Task<object> ExecuteAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            try
            {
                Log("info", $"Executing GCP operator with config: {JsonSerializer.Serialize(config)}");

                // Extract method from config
                if (!config.ContainsKey("method"))
                {
                    throw new ArgumentException("Method is required in config");
                }

                string method = config["method"].ToString();
                var parameters = config.ContainsKey("parameters") 
                    ? (Dictionary<string, object>)config["parameters"] 
                    : new Dictionary<string, object>();

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "compute_list_instances":
                        return await ComputeListInstancesAsync();
                    case "compute_start_instance":
                        return await ComputeStartInstanceAsync(parameters);
                    case "compute_stop_instance":
                        return await ComputeStopInstanceAsync(parameters);
                    case "compute_delete_instance":
                        return await ComputeDeleteInstanceAsync(parameters);
                    case "compute_create_instance":
                        return await ComputeCreateInstanceAsync(parameters);
                    case "compute_get_instance_info":
                        return await ComputeGetInstanceInfoAsync(parameters);
                    case "storage_list_buckets":
                        return await StorageListBucketsAsync();
                    case "storage_create_bucket":
                        return await StorageCreateBucketAsync(parameters);
                    case "storage_delete_bucket":
                        return await StorageDeleteBucketAsync(parameters);
                    case "storage_list_objects":
                        return await StorageListObjectsAsync(parameters);
                    case "storage_upload_object":
                        return await StorageUploadObjectAsync(parameters);
                    case "storage_download_object":
                        return await StorageDownloadObjectAsync(parameters);
                    case "storage_delete_object":
                        return await StorageDeleteObjectAsync(parameters);
                    case "functions_list":
                        return await FunctionsListAsync();
                    case "functions_invoke":
                        return await FunctionsInvokeAsync(parameters);
                    case "functions_create":
                        return await FunctionsCreateAsync(parameters);
                    case "functions_delete":
                        return await FunctionsDeleteAsync(parameters);
                    case "sql_list_instances":
                        return await SqlListInstancesAsync();
                    case "sql_create_instance":
                        return await SqlCreateInstanceAsync(parameters);
                    case "sql_delete_instance":
                        return await SqlDeleteInstanceAsync(parameters);
                    case "monitoring_get_metrics":
                        return await MonitoringGetMetricsAsync(parameters);
                    case "monitoring_put_metric":
                        return await MonitoringPutMetricAsync(parameters);
                    case "iam_list_users":
                        return await IamListUsersAsync();
                    case "iam_create_user":
                        return await IamCreateUserAsync(parameters);
                    case "iam_delete_user":
                        return await IamDeleteUserAsync(parameters);
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown method: {method}");
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, config, context);
                throw;
            }
        }

        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            return await ExecuteAsync(config, context);
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var projectId = GetRequiredParameter<string>(parameters, "project_id");
            var credentials = GetRequiredParameter<string>(parameters, "credentials");

            try
            {
                _projectId = projectId;
                _credentials = credentials;
                _endpoint = "https://compute.googleapis.com";

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await GetAccessTokenAsync()}");
                _httpClient.DefaultRequestHeaders.Add("X-Project-Id", projectId);

                LogInfo($"Connected to GCP project: {projectId}");
                return new { success = true, projectId, endpoint = _endpoint };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to GCP: {ex.Message}");
                throw new Exception($"GCP connection failed: {ex.Message}", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _projectId = null;
                _credentials = null;
                _endpoint = null;

                LogInfo("Disconnected from GCP");
                return new { success = true, message = "Disconnected from GCP" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from GCP: {ex.Message}");
                throw new Exception($"GCP disconnect failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ComputeListInstancesAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/compute/v1/projects/{_projectId}/aggregated/instances";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Compute list instances failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed GCP Compute Engine instances");
                return new { success = true, instances = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing GCP Compute Engine instances: {ex.Message}");
                throw new Exception($"GCP Compute list instances failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ComputeStartInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var zone = GetRequiredParameter<string>(parameters, "zone");
            var instanceName = GetRequiredParameter<string>(parameters, "instance_name");

            try
            {
                var url = $"{_endpoint}/compute/v1/projects/{_projectId}/zones/{zone}/instances/{instanceName}/start";
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Compute start instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully started GCP Compute Engine instance: {instanceName}");
                return new { success = true, zone, instanceName, operation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error starting GCP Compute Engine instance: {ex.Message}");
                throw new Exception($"GCP Compute start instance failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ComputeStopInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var zone = GetRequiredParameter<string>(parameters, "zone");
            var instanceName = GetRequiredParameter<string>(parameters, "instance_name");

            try
            {
                var url = $"{_endpoint}/compute/v1/projects/{_projectId}/zones/{zone}/instances/{instanceName}/stop";
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Compute stop instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully stopped GCP Compute Engine instance: {instanceName}");
                return new { success = true, zone, instanceName, operation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error stopping GCP Compute Engine instance: {ex.Message}");
                throw new Exception($"GCP Compute stop instance failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ComputeDeleteInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var zone = GetRequiredParameter<string>(parameters, "zone");
            var instanceName = GetRequiredParameter<string>(parameters, "instance_name");

            try
            {
                var url = $"{_endpoint}/compute/v1/projects/{_projectId}/zones/{zone}/instances/{instanceName}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Compute delete instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully deleted GCP Compute Engine instance: {instanceName}");
                return new { success = true, zone, instanceName, operation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting GCP Compute Engine instance: {ex.Message}");
                throw new Exception($"GCP Compute delete instance failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ComputeCreateInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var zone = GetRequiredParameter<string>(parameters, "zone");
            var instanceName = GetRequiredParameter<string>(parameters, "instance_name");
            var machineType = GetRequiredParameter<string>(parameters, "machine_type");
            var image = GetRequiredParameter<string>(parameters, "image");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "name", instanceName },
                    { "machineType", $"zones/{zone}/machineTypes/{machineType}" },
                    { "disks", new[]
                    {
                        new Dictionary<string, object>
                        {
                            { "boot", true },
                            { "autoDelete", true },
                            { "initializeParams", new Dictionary<string, object>
                            {
                                { "sourceImage", $"projects/debian-cloud/global/images/{image}" }
                            }}
                        }
                    }},
                    { "networkInterfaces", new[]
                    {
                        new Dictionary<string, object>
                        {
                            { "network", "global/networks/default" },
                            { "accessConfigs", new[]
                            {
                                new Dictionary<string, object>
                                {
                                    { "name", "External NAT" },
                                    { "type", "ONE_TO_ONE_NAT" }
                                }
                            }}
                        }
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/compute/v1/projects/{_projectId}/zones/{zone}/instances";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Compute create instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created GCP Compute Engine instance: {instanceName}");
                return new { success = true, zone, instanceName, machineType, image, operation = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating GCP Compute Engine instance: {ex.Message}");
                throw new Exception($"GCP Compute create instance failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ComputeGetInstanceInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var zone = GetRequiredParameter<string>(parameters, "zone");
            var instanceName = GetRequiredParameter<string>(parameters, "instance_name");

            try
            {
                var url = $"{_endpoint}/compute/v1/projects/{_projectId}/zones/{zone}/instances/{instanceName}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Compute get instance info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved GCP Compute Engine instance info: {instanceName}");
                return new { success = true, zone, instanceName, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting GCP Compute Engine instance info: {ex.Message}");
                throw new Exception($"GCP Compute get instance info failed: {ex.Message}", ex);
            }
        }

        private async Task<object> StorageListBucketsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"https://storage.googleapis.com/storage/v1/b?project={_projectId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage list buckets failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed GCP Cloud Storage buckets");
                return new { success = true, buckets = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing GCP Cloud Storage buckets: {ex.Message}");
                throw new Exception($"GCP Storage list buckets failed: {ex.Message}", ex);
            }
        }

        private async Task<object> StorageCreateBucketAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var location = GetParameter<string>(parameters, "location", "US");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "name", bucketName },
                    { "location", location }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://storage.googleapis.com/storage/v1/b?project={_projectId}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage create bucket failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created GCP Cloud Storage bucket: {bucketName}");
                return new { success = true, bucketName, location, bucket = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating GCP Cloud Storage bucket: {ex.Message}");
                throw new Exception($"GCP Storage create bucket failed: {ex.Message}", ex);
            }
        }

        private async Task<object> StorageDeleteBucketAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");

            try
            {
                var url = $"https://storage.googleapis.com/storage/v1/b/{bucketName}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage delete bucket failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted GCP Cloud Storage bucket: {bucketName}");
                return new { success = true, bucketName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting GCP Cloud Storage bucket: {ex.Message}");
                throw new Exception($"GCP Storage delete bucket failed: {ex.Message}", ex);
            }
        }

        private async Task<object> StorageListObjectsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var prefix = GetParameter<string>(parameters, "prefix", "");

            try
            {
                var url = $"https://storage.googleapis.com/storage/v1/b/{bucketName}/o";
                if (!string.IsNullOrEmpty(prefix))
                {
                    url += $"?prefix={prefix}";
                }

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage list objects failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully listed GCP Cloud Storage objects: {bucketName}");
                return new { success = true, bucketName, prefix, objects = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing GCP Cloud Storage objects: {ex.Message}");
                throw new Exception($"GCP Storage list objects failed: {ex.Message}", ex);
            }
        }

        private async Task<object> StorageUploadObjectAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var objectName = GetRequiredParameter<string>(parameters, "object_name");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");

            try
            {
                var url = $"https://storage.googleapis.com/upload/storage/v1/b/{bucketName}/o?uploadType=media&name={objectName}";
                var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
                var content = new ByteArrayContent(fileContent);

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage upload file failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully uploaded file to GCP Cloud Storage: {objectName}");
                return new { success = true, bucketName, objectName, filePath, resultObject = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error uploading file to GCP Cloud Storage: {ex.Message}");
                throw new Exception($"GCP Storage upload file failed: {ex.Message}", ex);
            }
        }

        private async Task<object> StorageDownloadObjectAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var objectName = GetRequiredParameter<string>(parameters, "object_name");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");

            try
            {
                var url = $"https://storage.googleapis.com/storage/v1/b/{bucketName}/o/{objectName}?alt=media";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage download file failed: {response.StatusCode} - {errorContent}");
                }

                var objectContent = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(filePath, objectContent);

                LogInfo($"Successfully downloaded file from GCP Cloud Storage: {objectName}");
                return new { success = true, bucketName, objectName, filePath };
            }
            catch (Exception ex)
            {
                LogError($"Error downloading file from GCP Cloud Storage: {ex.Message}");
                throw new Exception($"GCP Storage download file failed: {ex.Message}", ex);
            }
        }

        private async Task<object> StorageDeleteObjectAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var objectName = GetRequiredParameter<string>(parameters, "object_name");

            try
            {
                var url = $"https://storage.googleapis.com/storage/v1/b/{bucketName}/o/{objectName}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage delete object failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted GCP Cloud Storage object: {objectName}");
                return new { success = true, bucketName, objectName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting GCP Cloud Storage object: {ex.Message}");
                throw new Exception($"GCP Storage delete object failed: {ex.Message}", ex);
            }
        }

        private async Task<object> FunctionsListAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"https://cloudfunctions.googleapis.com/v1/projects/{_projectId}/locations/-/functions";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Functions list failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed GCP Cloud Functions");
                return new { success = true, functions = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing GCP Cloud Functions: {ex.Message}");
                throw new Exception($"GCP Functions list failed: {ex.Message}", ex);
            }
        }

        private async Task<object> FunctionsInvokeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var functionName = GetRequiredParameter<string>(parameters, "function_name");
            var payload = GetRequiredParameter<string>(parameters, "payload");

            try
            {
                var url = $"https://us-central1-{_projectId}.cloudfunctions.net/{functionName}";
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Functions invoke failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully invoked GCP Cloud Function: {functionName}");
                return new { success = true, functionName, payload, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error invoking GCP Cloud Function: {ex.Message}");
                throw new Exception($"GCP Functions invoke failed: {ex.Message}", ex);
            }
        }

        private async Task<object> FunctionsCreateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var functionName = GetRequiredParameter<string>(parameters, "function_name");
            var runtime = GetRequiredParameter<string>(parameters, "runtime");
            var source = GetRequiredParameter<string>(parameters, "source");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "name", $"projects/{_projectId}/locations/us-central1/functions/{functionName}" },
                    { "runtime", runtime },
                    { "sourceArchiveUrl", source },
                    { "entryPoint", "helloWorld" },
                    { "httpsTrigger", new Dictionary<string, object>() }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://cloudfunctions.googleapis.com/v1/projects/{_projectId}/locations/us-central1/functions";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Functions deploy failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully deployed GCP Cloud Function: {functionName}");
                return new { success = true, functionName, runtime, source, function = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error deploying GCP Cloud Function: {ex.Message}");
                throw new Exception($"GCP Functions deploy failed: {ex.Message}", ex);
            }
        }

        private async Task<object> FunctionsDeleteAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var functionName = GetRequiredParameter<string>(parameters, "function_name");

            try
            {
                var url = $"https://cloudfunctions.googleapis.com/v1/projects/{_projectId}/locations/us-central1/functions/{functionName}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Functions delete failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted GCP Cloud Function: {functionName}");
                return new { success = true, functionName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting GCP Cloud Function: {ex.Message}");
                throw new Exception($"GCP Functions delete failed: {ex.Message}", ex);
            }
        }

        private async Task<object> SqlListInstancesAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"https://sqladmin.googleapis.com/sql/v1beta4/projects/{_projectId}/instances";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SQL list instances failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed GCP Cloud SQL instances");
                return new { success = true, instances = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing GCP Cloud SQL instances: {ex.Message}");
                throw new Exception($"GCP SQL list instances failed: {ex.Message}", ex);
            }
        }

        private async Task<object> SqlCreateInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var instanceName = GetRequiredParameter<string>(parameters, "instance_name");
            var databaseVersion = GetRequiredParameter<string>(parameters, "database_version");
            var tier = GetRequiredParameter<string>(parameters, "tier");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "name", instanceName },
                    { "databaseVersion", databaseVersion },
                    { "settings", new Dictionary<string, object>
                    {
                        { "tier", tier }
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://sqladmin.googleapis.com/sql/v1beta4/projects/{_projectId}/instances";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SQL create instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created GCP Cloud SQL instance: {instanceName}");
                return new { success = true, instanceName, databaseVersion, tier, instance = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating GCP Cloud SQL instance: {ex.Message}");
                throw new Exception($"GCP SQL create instance failed: {ex.Message}", ex);
            }
        }

        private async Task<object> SqlDeleteInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var instanceName = GetRequiredParameter<string>(parameters, "instance_name");

            try
            {
                var url = $"https://sqladmin.googleapis.com/sql/v1beta4/projects/{_projectId}/instances/{instanceName}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SQL delete instance failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted GCP Cloud SQL instance: {instanceName}");
                return new { success = true, instanceName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting GCP Cloud SQL instance: {ex.Message}");
                throw new Exception($"GCP SQL delete instance failed: {ex.Message}", ex);
            }
        }

        private async Task<object> MonitoringGetMetricsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var metricType = GetRequiredParameter<string>(parameters, "metric_type");
            var filter = GetRequiredParameter<string>(parameters, "filter");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "filter", filter },
                    { "interval", new Dictionary<string, object>
                    {
                        { "startTime", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ") },
                        { "endTime", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://monitoring.googleapis.com/v3/projects/{_projectId}/timeSeries:list";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Monitoring get metrics failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved GCP Cloud Monitoring metrics");
                return new { success = true, metricType, filter, metrics = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting GCP Cloud Monitoring metrics: {ex.Message}");
                throw new Exception($"GCP Monitoring get metrics failed: {ex.Message}", ex);
            }
        }

        private async Task<object> MonitoringPutMetricAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var metricType = GetRequiredParameter<string>(parameters, "metric_type");
            var value = GetRequiredParameter<double>(parameters, "value");
            var labels = GetParameter<Dictionary<string, object>>(parameters, "labels", new Dictionary<string, object>());

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "timeSeries", new[]
                    {
                        new Dictionary<string, object>
                        {
                            { "metric", new Dictionary<string, object>
                            {
                                { "type", metricType },
                                { "labels", labels }
                            }},
                            { "resource", new Dictionary<string, object>
                            {
                                { "type", "global" },
                                { "labels", new Dictionary<string, object>
                                {
                                    { "project_id", _projectId }
                                }}
                            }},
                            { "points", new[]
                            {
                                new Dictionary<string, object>
                                {
                                    { "interval", new Dictionary<string, object>
                                    {
                                        { "endTime", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                                    }},
                                    { "value", new Dictionary<string, object>
                                    {
                                        { "doubleValue", value }
                                    }}
                                }
                            }}
                        }
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://monitoring.googleapis.com/v3/projects/{_projectId}/timeSeries";
                var response = await _httpClient.CreateAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Monitoring write metric failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully wrote GCP Cloud Monitoring metric");
                return new { success = true, metricType, value, labels };
            }
            catch (Exception ex)
            {
                LogError($"Error writing GCP Cloud Monitoring metric: {ex.Message}");
                throw new Exception($"GCP Monitoring write metric failed: {ex.Message}", ex);
            }
        }

        private async Task<object> LoggingListLogsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var filter = GetParameter<string>(parameters, "filter", "");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "resourceNames", new[] { $"projects/{_projectId}" } },
                    { "filter", filter },
                    { "orderBy", "timestamp desc" },
                    { "pageSize", 50 }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = "https://logging.googleapis.com/v2/entries:list";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Logging list logs failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed GCP Cloud Logging logs");
                return new { success = true, filter, logs = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing GCP Cloud Logging logs: {ex.Message}");
                throw new Exception($"GCP Logging list logs failed: {ex.Message}", ex);
            }
        }

        private async Task<object> LoggingWriteLogAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var logName = GetRequiredParameter<string>(parameters, "log_name");
            var severity = GetRequiredParameter<string>(parameters, "severity");
            var message = GetRequiredParameter<string>(parameters, "message");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "entries", new[]
                    {
                        new Dictionary<string, object>
                        {
                            { "logName", $"projects/{_projectId}/logs/{logName}" },
                            { "severity", severity },
                            { "textPayload", message },
                            { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                        }
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = "https://logging.googleapis.com/v2/entries:write";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Logging write log failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully wrote GCP Cloud Logging log: {logName}");
                return new { success = true, logName, severity, message };
            }
            catch (Exception ex)
            {
                LogError($"Error writing GCP Cloud Logging log: {ex.Message}");
                throw new Exception($"GCP Logging write log failed: {ex.Message}", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var result = await ComputeListInstancesAsync();
                return new { success = true, status = "Connected" };
            }
            catch (Exception ex)
            {
                LogError($"Error testing GCP connection: {ex.Message}");
                throw new Exception($"GCP connection test failed: {ex.Message}", ex);
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var tokenUrl = "https://oauth2.googleapis.com/token";
                var tokenData = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _credentials },
                    { "scope", "https://www.googleapis.com/auth/cloud-platform" }
                };

                var tokenContent = new FormUrlEncodedContent(tokenData);
                var tokenResponse = await _httpClient.PostAsync(tokenUrl, tokenContent);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to get GCP access token");
                }

                var tokenResult = await tokenResponse.Content.ReadAsStringAsync();
                var tokenObj = JsonSerializer.Deserialize<Dictionary<string, object>>(tokenResult);
                return tokenObj["access_token"].ToString();
            }
            catch (Exception ex)
            {
                LogError($"Error getting GCP access token: {ex.Message}");
                throw new Exception($"GCP access token failed: {ex.Message}", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_projectId))
            {
                throw new Exception("Not connected to GCP");
            }
        }

        private T GetRequiredParameter<T>(Dictionary<string, object> parameters, string key)
        {
            if (!parameters.ContainsKey(key))
            {
                throw new ArgumentException($"Required parameter '{key}' is missing");
            }
            return (T)parameters[key];
        }

        private void LogInfo(string message)
        {
            Log("info", message);
        }

        private void LogError(string message)
        {
            Log("error", message);
        }

        private void LogWarning(string message)
        {
            Log("warning", message);
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
            _httpClient?.Dispose();
        }
    }
} 
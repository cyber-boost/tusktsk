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
    /// AWS operator for TuskLang
    /// Provides AWS cloud operations including EC2, S3, Lambda, and other AWS services
    /// </summary>
    public class AwsOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _accessKey;
        private string _secretKey;
        private string _region;
        private string _endpoint;

        public AwsOperator() : base("aws", "AWS cloud operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to AWS", new[] { "access_key", "secret_key", "region" });
            RegisterMethod("disconnect", "Disconnect from AWS", new string[0]);
            RegisterMethod("ec2_list_instances", "List EC2 instances", new string[0]);
            RegisterMethod("ec2_start_instance", "Start EC2 instance", new[] { "instance_id" });
            RegisterMethod("ec2_stop_instance", "Stop EC2 instance", new[] { "instance_id" });
            RegisterMethod("ec2_terminate_instance", "Terminate EC2 instance", new[] { "instance_id" });
            RegisterMethod("ec2_create_instance", "Create EC2 instance", new[] { "image_id", "instance_type", "key_name" });
            RegisterMethod("ec2_get_instance_info", "Get EC2 instance info", new[] { "instance_id" });
            RegisterMethod("s3_list_buckets", "List S3 buckets", new string[0]);
            RegisterMethod("s3_create_bucket", "Create S3 bucket", new[] { "bucket_name", "region" });
            RegisterMethod("s3_delete_bucket", "Delete S3 bucket", new[] { "bucket_name" });
            RegisterMethod("s3_list_objects", "List S3 objects", new[] { "bucket_name", "prefix" });
            RegisterMethod("s3_upload_file", "Upload file to S3", new[] { "bucket_name", "key", "file_path" });
            RegisterMethod("s3_download_file", "Download file from S3", new[] { "bucket_name", "key", "file_path" });
            RegisterMethod("s3_delete_object", "Delete S3 object", new[] { "bucket_name", "key" });
            RegisterMethod("lambda_list_functions", "List Lambda functions", new string[0]);
            RegisterMethod("lambda_invoke", "Invoke Lambda function", new[] { "function_name", "payload" });
            RegisterMethod("lambda_create_function", "Create Lambda function", new[] { "function_name", "runtime", "handler", "code" });
            RegisterMethod("lambda_delete_function", "Delete Lambda function", new[] { "function_name" });
            RegisterMethod("rds_list_instances", "List RDS instances", new string[0]);
            RegisterMethod("rds_create_instance", "Create RDS instance", new[] { "instance_id", "engine", "size" });
            RegisterMethod("rds_delete_instance", "Delete RDS instance", new[] { "instance_id" });
            RegisterMethod("cloudwatch_get_metrics", "Get CloudWatch metrics", new[] { "namespace", "metric_name", "dimensions" });
            RegisterMethod("cloudwatch_put_metric", "Put CloudWatch metric", new[] { "namespace", "metric_name", "value", "unit" });
            RegisterMethod("iam_list_users", "List IAM users", new string[0]);
            RegisterMethod("iam_create_user", "Create IAM user", new[] { "username" });
            RegisterMethod("iam_delete_user", "Delete IAM user", new[] { "username" });
            RegisterMethod("test_connection", "Test AWS connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing AWS operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "ec2_list_instances":
                        return await Ec2ListInstancesAsync();
                    case "ec2_start_instance":
                        return await Ec2StartInstanceAsync(parameters);
                    case "ec2_stop_instance":
                        return await Ec2StopInstanceAsync(parameters);
                    case "ec2_terminate_instance":
                        return await Ec2TerminateInstanceAsync(parameters);
                    case "ec2_create_instance":
                        return await Ec2CreateInstanceAsync(parameters);
                    case "ec2_get_instance_info":
                        return await Ec2GetInstanceInfoAsync(parameters);
                    case "s3_list_buckets":
                        return await S3ListBucketsAsync();
                    case "s3_create_bucket":
                        return await S3CreateBucketAsync(parameters);
                    case "s3_delete_bucket":
                        return await S3DeleteBucketAsync(parameters);
                    case "s3_list_objects":
                        return await S3ListObjectsAsync(parameters);
                    case "s3_upload_file":
                        return await S3UploadFileAsync(parameters);
                    case "s3_download_file":
                        return await S3DownloadFileAsync(parameters);
                    case "s3_delete_object":
                        return await S3DeleteObjectAsync(parameters);
                    case "lambda_list_functions":
                        return await LambdaListFunctionsAsync();
                    case "lambda_invoke":
                        return await LambdaInvokeAsync(parameters);
                    case "lambda_create_function":
                        return await LambdaCreateFunctionAsync(parameters);
                    case "lambda_delete_function":
                        return await LambdaDeleteFunctionAsync(parameters);
                    case "rds_list_instances":
                        return await RdsListInstancesAsync();
                    case "rds_create_instance":
                        return await RdsCreateInstanceAsync(parameters);
                    case "rds_delete_instance":
                        return await RdsDeleteInstanceAsync(parameters);
                    case "cloudwatch_get_metrics":
                        return await CloudWatchGetMetricsAsync(parameters);
                    case "cloudwatch_put_metric":
                        return await CloudWatchPutMetricAsync(parameters);
                    case "iam_list_users":
                        return await IamListUsersAsync();
                    case "iam_create_user":
                        return await IamCreateUserAsync(parameters);
                    case "iam_delete_user":
                        return await IamDeleteUserAsync(parameters);
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown AWS method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing AWS method {method}: {ex.Message}");
                throw new OperatorException($"AWS operation failed: {ex.Message}", "AWS_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var accessKey = GetRequiredParameter<string>(parameters, "access_key");
            var secretKey = GetRequiredParameter<string>(parameters, "secret_key");
            var region = GetParameter<string>(parameters, "region", "us-east-1");

            try
            {
                _accessKey = accessKey;
                _secretKey = secretKey;
                _region = region;
                _endpoint = $"https://ec2.{region}.amazonaws.com";

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("X-Amz-Access-Key", accessKey);
                _httpClient.DefaultRequestHeaders.Add("X-Amz-Secret-Key", secretKey);
                _httpClient.DefaultRequestHeaders.Add("X-Amz-Region", region);

                LogInfo($"Connected to AWS in region: {region}");
                return new { success = true, region, endpoint = _endpoint };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to AWS: {ex.Message}");
                throw new OperatorException($"AWS connection failed: {ex.Message}", "AWS_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _accessKey = null;
                _secretKey = null;
                _region = null;
                _endpoint = null;

                LogInfo("Disconnected from AWS");
                return new { success = true, message = "Disconnected from AWS" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from AWS: {ex.Message}");
                throw new OperatorException($"AWS disconnect failed: {ex.Message}", "AWS_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> Ec2ListInstancesAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/ec2/instances";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"EC2 list instances failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed EC2 instances");
                return new { success = true, instances = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing EC2 instances: {ex.Message}");
                throw new OperatorException($"EC2 list instances failed: {ex.Message}", "EC2_LIST_INSTANCES_ERROR", ex);
            }
        }

        private async Task<object> Ec2StartInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var instanceId = GetRequiredParameter<string>(parameters, "instance_id");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "instance_id", instanceId }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/ec2/instances/{instanceId}/start";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"EC2 start instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully started EC2 instance: {instanceId}");
                return new { success = true, instanceId, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error starting EC2 instance: {ex.Message}");
                throw new OperatorException($"EC2 start instance failed: {ex.Message}", "EC2_START_INSTANCE_ERROR", ex);
            }
        }

        private async Task<object> Ec2StopInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var instanceId = GetRequiredParameter<string>(parameters, "instance_id");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "instance_id", instanceId }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/ec2/instances/{instanceId}/stop";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"EC2 stop instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully stopped EC2 instance: {instanceId}");
                return new { success = true, instanceId, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error stopping EC2 instance: {ex.Message}");
                throw new OperatorException($"EC2 stop instance failed: {ex.Message}", "EC2_STOP_INSTANCE_ERROR", ex);
            }
        }

        private async Task<object> Ec2TerminateInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var instanceId = GetRequiredParameter<string>(parameters, "instance_id");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "instance_id", instanceId }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/ec2/instances/{instanceId}/terminate";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"EC2 terminate instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully terminated EC2 instance: {instanceId}");
                return new { success = true, instanceId, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error terminating EC2 instance: {ex.Message}");
                throw new OperatorException($"EC2 terminate instance failed: {ex.Message}", "EC2_TERMINATE_INSTANCE_ERROR", ex);
            }
        }

        private async Task<object> Ec2CreateInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var imageId = GetRequiredParameter<string>(parameters, "image_id");
            var instanceType = GetRequiredParameter<string>(parameters, "instance_type");
            var keyName = GetRequiredParameter<string>(parameters, "key_name");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "image_id", imageId },
                    { "instance_type", instanceType },
                    { "key_name", keyName }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/ec2/instances";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"EC2 create instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created EC2 instance");
                return new { success = true, imageId, instanceType, keyName, instance = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating EC2 instance: {ex.Message}");
                throw new OperatorException($"EC2 create instance failed: {ex.Message}", "EC2_CREATE_INSTANCE_ERROR", ex);
            }
        }

        private async Task<object> Ec2GetInstanceInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var instanceId = GetRequiredParameter<string>(parameters, "instance_id");

            try
            {
                var url = $"{_endpoint}/ec2/instances/{instanceId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"EC2 get instance info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved EC2 instance info: {instanceId}");
                return new { success = true, instanceId, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting EC2 instance info: {ex.Message}");
                throw new OperatorException($"EC2 get instance info failed: {ex.Message}", "EC2_GET_INSTANCE_INFO_ERROR", ex);
            }
        }

        private async Task<object> S3ListBucketsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/s3/buckets";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"S3 list buckets failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed S3 buckets");
                return new { success = true, buckets = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing S3 buckets: {ex.Message}");
                throw new OperatorException($"S3 list buckets failed: {ex.Message}", "S3_LIST_BUCKETS_ERROR", ex);
            }
        }

        private async Task<object> S3CreateBucketAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var region = GetParameter<string>(parameters, "region", _region);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "bucket_name", bucketName },
                    { "region", region }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/s3/buckets";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"S3 create bucket failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created S3 bucket: {bucketName}");
                return new { success = true, bucketName, region, bucket = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating S3 bucket: {ex.Message}");
                throw new OperatorException($"S3 create bucket failed: {ex.Message}", "S3_CREATE_BUCKET_ERROR", ex);
            }
        }

        private async Task<object> S3DeleteBucketAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");

            try
            {
                var url = $"{_endpoint}/s3/buckets/{bucketName}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"S3 delete bucket failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted S3 bucket: {bucketName}");
                return new { success = true, bucketName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting S3 bucket: {ex.Message}");
                throw new OperatorException($"S3 delete bucket failed: {ex.Message}", "S3_DELETE_BUCKET_ERROR", ex);
            }
        }

        private async Task<object> S3ListObjectsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var prefix = GetParameter<string>(parameters, "prefix", "");

            try
            {
                var url = $"{_endpoint}/s3/buckets/{bucketName}/objects";
                if (!string.IsNullOrEmpty(prefix))
                {
                    url += $"?prefix={prefix}";
                }

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"S3 list objects failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully listed S3 objects in bucket: {bucketName}");
                return new { success = true, bucketName, prefix, objects = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing S3 objects: {ex.Message}");
                throw new OperatorException($"S3 list objects failed: {ex.Message}", "S3_LIST_OBJECTS_ERROR", ex);
            }
        }

        private async Task<object> S3UploadFileAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var key = GetRequiredParameter<string>(parameters, "key");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "bucket_name", bucketName },
                    { "key", key },
                    { "file_path", filePath }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/s3/buckets/{bucketName}/objects/{key}";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"S3 upload file failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully uploaded file to S3: {bucketName}/{key}");
                return new { success = true, bucketName, key, filePath, upload = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error uploading file to S3: {ex.Message}");
                throw new OperatorException($"S3 upload file failed: {ex.Message}", "S3_UPLOAD_FILE_ERROR", ex);
            }
        }

        private async Task<object> S3DownloadFileAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var key = GetRequiredParameter<string>(parameters, "key");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "bucket_name", bucketName },
                    { "key", key },
                    { "file_path", filePath }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/s3/buckets/{bucketName}/objects/{key}/download";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"S3 download file failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully downloaded file from S3: {bucketName}/{key}");
                return new { success = true, bucketName, key, filePath, download = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error downloading file from S3: {ex.Message}");
                throw new OperatorException($"S3 download file failed: {ex.Message}", "S3_DOWNLOAD_FILE_ERROR", ex);
            }
        }

        private async Task<object> S3DeleteObjectAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var bucketName = GetRequiredParameter<string>(parameters, "bucket_name");
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                var url = $"{_endpoint}/s3/buckets/{bucketName}/objects/{key}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"S3 delete object failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted S3 object: {bucketName}/{key}");
                return new { success = true, bucketName, key };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting S3 object: {ex.Message}");
                throw new OperatorException($"S3 delete object failed: {ex.Message}", "S3_DELETE_OBJECT_ERROR", ex);
            }
        }

        private async Task<object> LambdaListFunctionsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/lambda/functions";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lambda list functions failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed Lambda functions");
                return new { success = true, functions = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing Lambda functions: {ex.Message}");
                throw new OperatorException($"Lambda list functions failed: {ex.Message}", "LAMBDA_LIST_FUNCTIONS_ERROR", ex);
            }
        }

        private async Task<object> LambdaInvokeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var functionName = GetRequiredParameter<string>(parameters, "function_name");
            var payload = GetRequiredParameter<string>(parameters, "payload");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "function_name", functionName },
                    { "payload", payload }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/lambda/functions/{functionName}/invoke";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lambda invoke failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully invoked Lambda function: {functionName}");
                return new { success = true, functionName, payload, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error invoking Lambda function: {ex.Message}");
                throw new OperatorException($"Lambda invoke failed: {ex.Message}", "LAMBDA_INVOKE_ERROR", ex);
            }
        }

        private async Task<object> LambdaCreateFunctionAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var functionName = GetRequiredParameter<string>(parameters, "function_name");
            var runtime = GetRequiredParameter<string>(parameters, "runtime");
            var handler = GetRequiredParameter<string>(parameters, "handler");
            var code = GetRequiredParameter<string>(parameters, "code");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "function_name", functionName },
                    { "runtime", runtime },
                    { "handler", handler },
                    { "code", code }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/lambda/functions";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lambda create function failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created Lambda function: {functionName}");
                return new { success = true, functionName, runtime, handler, function = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating Lambda function: {ex.Message}");
                throw new OperatorException($"Lambda create function failed: {ex.Message}", "LAMBDA_CREATE_FUNCTION_ERROR", ex);
            }
        }

        private async Task<object> LambdaDeleteFunctionAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var functionName = GetRequiredParameter<string>(parameters, "function_name");

            try
            {
                var url = $"{_endpoint}/lambda/functions/{functionName}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lambda delete function failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted Lambda function: {functionName}");
                return new { success = true, functionName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting Lambda function: {ex.Message}");
                throw new OperatorException($"Lambda delete function failed: {ex.Message}", "LAMBDA_DELETE_FUNCTION_ERROR", ex);
            }
        }

        private async Task<object> RdsListInstancesAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/rds/instances";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"RDS list instances failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed RDS instances");
                return new { success = true, instances = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing RDS instances: {ex.Message}");
                throw new OperatorException($"RDS list instances failed: {ex.Message}", "RDS_LIST_INSTANCES_ERROR", ex);
            }
        }

        private async Task<object> RdsCreateInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var instanceId = GetRequiredParameter<string>(parameters, "instance_id");
            var engine = GetRequiredParameter<string>(parameters, "engine");
            var size = GetRequiredParameter<string>(parameters, "size");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "instance_id", instanceId },
                    { "engine", engine },
                    { "size", size }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/rds/instances";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"RDS create instance failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created RDS instance: {instanceId}");
                return new { success = true, instanceId, engine, size, instance = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating RDS instance: {ex.Message}");
                throw new OperatorException($"RDS create instance failed: {ex.Message}", "RDS_CREATE_INSTANCE_ERROR", ex);
            }
        }

        private async Task<object> RdsDeleteInstanceAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var instanceId = GetRequiredParameter<string>(parameters, "instance_id");

            try
            {
                var url = $"{_endpoint}/rds/instances/{instanceId}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"RDS delete instance failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted RDS instance: {instanceId}");
                return new { success = true, instanceId };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting RDS instance: {ex.Message}");
                throw new OperatorException($"RDS delete instance failed: {ex.Message}", "RDS_DELETE_INSTANCE_ERROR", ex);
            }
        }

        private async Task<object> CloudWatchGetMetricsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var namespace2 = GetRequiredParameter<string>(parameters, "namespace");
            var metricName = GetRequiredParameter<string>(parameters, "metric_name");
            var dimensions = GetParameter<Dictionary<string, object>>(parameters, "dimensions", new Dictionary<string, object>());

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "namespace", namespace2 },
                    { "metric_name", metricName },
                    { "dimensions", dimensions }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/cloudwatch/metrics";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"CloudWatch get metrics failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved CloudWatch metrics");
                return new { success = true, namespace2, metricName, dimensions, metrics = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting CloudWatch metrics: {ex.Message}");
                throw new OperatorException($"CloudWatch get metrics failed: {ex.Message}", "CLOUDWATCH_GET_METRICS_ERROR", ex);
            }
        }

        private async Task<object> CloudWatchPutMetricAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var namespace2 = GetRequiredParameter<string>(parameters, "namespace");
            var metricName = GetRequiredParameter<string>(parameters, "metric_name");
            var value = GetRequiredParameter<double>(parameters, "value");
            var unit = GetRequiredParameter<string>(parameters, "unit");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "namespace", namespace2 },
                    { "metric_name", metricName },
                    { "value", value },
                    { "unit", unit }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/cloudwatch/metrics";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"CloudWatch put metric failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully put CloudWatch metric");
                return new { success = true, namespace2, metricName, value, unit, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error putting CloudWatch metric: {ex.Message}");
                throw new OperatorException($"CloudWatch put metric failed: {ex.Message}", "CLOUDWATCH_PUT_METRIC_ERROR", ex);
            }
        }

        private async Task<object> IamListUsersAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/iam/users";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"IAM list users failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed IAM users");
                return new { success = true, users = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing IAM users: {ex.Message}");
                throw new OperatorException($"IAM list users failed: {ex.Message}", "IAM_LIST_USERS_ERROR", ex);
            }
        }

        private async Task<object> IamCreateUserAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var username = GetRequiredParameter<string>(parameters, "username");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "username", username }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/iam/users";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"IAM create user failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created IAM user: {username}");
                return new { success = true, username, user = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating IAM user: {ex.Message}");
                throw new OperatorException($"IAM create user failed: {ex.Message}", "IAM_CREATE_USER_ERROR", ex);
            }
        }

        private async Task<object> IamDeleteUserAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var username = GetRequiredParameter<string>(parameters, "username");

            try
            {
                var url = $"{_endpoint}/iam/users/{username}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"IAM delete user failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted IAM user: {username}");
                return new { success = true, username };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting IAM user: {ex.Message}");
                throw new OperatorException($"IAM delete user failed: {ex.Message}", "IAM_DELETE_USER_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var result = await Ec2ListInstancesAsync();
                return new { success = true, status = "Connected" };
            }
            catch (Exception ex)
            {
                LogError($"Error testing AWS connection: {ex.Message}");
                throw new OperatorException($"AWS connection test failed: {ex.Message}", "AWS_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_accessKey))
            {
                throw new OperatorException("Not connected to AWS", "AWS_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
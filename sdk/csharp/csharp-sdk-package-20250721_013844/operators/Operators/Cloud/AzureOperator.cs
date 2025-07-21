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
    /// Azure operator for TuskLang
    /// Provides Azure cloud operations including VM, Storage, Functions, and other Azure services
    /// </summary>
    public class AzureOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _subscriptionId;
        private string _tenantId;
        private string _clientId;
        private string _clientSecret;
        private string _endpoint;

        public AzureOperator() : base("azure", "Azure cloud operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Azure", new[] { "subscription_id", "tenant_id", "client_id", "client_secret" });
            RegisterMethod("disconnect", "Disconnect from Azure", new string[0]);
            RegisterMethod("vm_list", "List virtual machines", new string[0]);
            RegisterMethod("vm_start", "Start virtual machine", new[] { "resource_group", "vm_name" });
            RegisterMethod("vm_stop", "Stop virtual machine", new[] { "resource_group", "vm_name" });
            RegisterMethod("vm_deallocate", "Deallocate virtual machine", new[] { "resource_group", "vm_name" });
            RegisterMethod("vm_create", "Create virtual machine", new[] { "resource_group", "vm_name", "image", "size" });
            RegisterMethod("vm_get_info", "Get VM information", new[] { "resource_group", "vm_name" });
            RegisterMethod("storage_list_accounts", "List storage accounts", new string[0]);
            RegisterMethod("storage_create_account", "Create storage account", new[] { "resource_group", "account_name", "sku" });
            RegisterMethod("storage_delete_account", "Delete storage account", new[] { "resource_group", "account_name" });
            RegisterMethod("storage_list_containers", "List storage containers", new[] { "account_name" });
            RegisterMethod("storage_create_container", "Create storage container", new[] { "account_name", "container_name" });
            RegisterMethod("storage_delete_container", "Delete storage container", new[] { "account_name", "container_name" });
            RegisterMethod("storage_upload_blob", "Upload blob to storage", new[] { "account_name", "container_name", "blob_name", "file_path" });
            RegisterMethod("storage_download_blob", "Download blob from storage", new[] { "account_name", "container_name", "blob_name", "file_path" });
            RegisterMethod("storage_delete_blob", "Delete blob from storage", new[] { "account_name", "container_name", "blob_name" });
            RegisterMethod("functions_list", "List Azure Functions", new string[0]);
            RegisterMethod("functions_invoke", "Invoke Azure Function", new[] { "function_name", "payload" });
            RegisterMethod("functions_create", "Create Azure Function", new[] { "name", "runtime", "code" });
            RegisterMethod("functions_delete", "Delete Azure Function", new[] { "name" });
            RegisterMethod("sql_list_servers", "List SQL servers", new string[0]);
            RegisterMethod("sql_create_server", "Create SQL server", new[] { "resource_group", "server_name", "admin_user", "admin_password" });
            RegisterMethod("sql_delete_server", "Delete SQL server", new[] { "resource_group", "server_name" });
            RegisterMethod("monitor_get_metrics", "Get monitoring metrics", new[] { "resource_id", "metric_name" });
            RegisterMethod("monitor_put_metric", "Put monitoring metric", new[] { "resource_id", "metric_name", "value" });
            RegisterMethod("test_connection", "Test Azure connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Azure operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "vm_list":
                        return await VmListAsync();
                    case "vm_start":
                        return await VmStartAsync(parameters);
                    case "vm_stop":
                        return await VmStopAsync(parameters);
                    case "vm_deallocate":
                        return await VmDeallocateAsync(parameters);
                    case "vm_create":
                        return await VmCreateAsync(parameters);
                    case "vm_get_info":
                        return await VmGetInfoAsync(parameters);
                    case "storage_list_accounts":
                        return await StorageListAccountsAsync();
                    case "storage_create_account":
                        return await StorageCreateAccountAsync(parameters);
                    case "storage_delete_account":
                        return await StorageDeleteAccountAsync(parameters);
                    case "storage_list_containers":
                        return await StorageListContainersAsync(parameters);
                    case "storage_create_container":
                        return await StorageCreateContainerAsync(parameters);
                    case "storage_delete_container":
                        return await StorageDeleteContainerAsync(parameters);
                    case "storage_upload_blob":
                        return await StorageUploadBlobAsync(parameters);
                    case "storage_download_blob":
                        return await StorageDownloadBlobAsync(parameters);
                    case "storage_delete_blob":
                        return await StorageDeleteBlobAsync(parameters);
                    case "functions_list":
                        return await FunctionsListAsync();
                    case "functions_invoke":
                        return await FunctionsInvokeAsync(parameters);
                    case "functions_create":
                        return await FunctionsCreateAsync(parameters);
                    case "functions_delete":
                        return await FunctionsDeleteAsync(parameters);
                    case "sql_list_servers":
                        return await SqlListServersAsync();
                    case "sql_create_server":
                        return await SqlCreateServerAsync(parameters);
                    case "sql_delete_server":
                        return await SqlDeleteServerAsync(parameters);
                    case "monitor_get_metrics":
                        return await MonitorGetMetricsAsync(parameters);
                    case "monitor_put_metric":
                        return await MonitorPutMetricAsync(parameters);
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown Azure method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Azure method {method}: {ex.Message}");
                throw new OperatorException($"Azure operation failed: {ex.Message}", "AZURE_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var subscriptionId = GetRequiredParameter<string>(parameters, "subscription_id");
            var tenantId = GetRequiredParameter<string>(parameters, "tenant_id");
            var clientId = GetRequiredParameter<string>(parameters, "client_id");
            var clientSecret = GetRequiredParameter<string>(parameters, "client_secret");

            try
            {
                _subscriptionId = subscriptionId;
                _tenantId = tenantId;
                _clientId = clientId;
                _clientSecret = clientSecret;
                _endpoint = "https://management.azure.com";

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {await GetAccessTokenAsync()}");
                _httpClient.DefaultRequestHeaders.Add("X-Subscription-Id", subscriptionId);

                LogInfo($"Connected to Azure subscription: {subscriptionId}");
                return new { success = true, subscriptionId, tenantId, endpoint = _endpoint };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Azure: {ex.Message}");
                throw new OperatorException($"Azure connection failed: {ex.Message}", "AZURE_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _subscriptionId = null;
                _tenantId = null;
                _clientId = null;
                _clientSecret = null;
                _endpoint = null;

                LogInfo("Disconnected from Azure");
                return new { success = true, message = "Disconnected from Azure" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from Azure: {ex.Message}");
                throw new OperatorException($"Azure disconnect failed: {ex.Message}", "AZURE_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> VmListAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/providers/Microsoft.Compute/virtualMachines?api-version=2021-04-01";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"VM list failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed Azure VMs");
                return new { success = true, vms = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing Azure VMs: {ex.Message}");
                throw new OperatorException($"Azure VM list failed: {ex.Message}", "AZURE_VM_LIST_ERROR", ex);
            }
        }

        private async Task<object> VmStartAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var vmName = GetRequiredParameter<string>(parameters, "vm_name");

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Compute/virtualMachines/{vmName}/start?api-version=2021-04-01";
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"VM start failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully started Azure VM: {vmName}");
                return new { success = true, resourceGroup, vmName };
            }
            catch (Exception ex)
            {
                LogError($"Error starting Azure VM: {ex.Message}");
                throw new OperatorException($"Azure VM start failed: {ex.Message}", "AZURE_VM_START_ERROR", ex);
            }
        }

        private async Task<object> VmStopAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var vmName = GetRequiredParameter<string>(parameters, "vm_name");

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Compute/virtualMachines/{vmName}/powerOff?api-version=2021-04-01";
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"VM stop failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully stopped Azure VM: {vmName}");
                return new { success = true, resourceGroup, vmName };
            }
            catch (Exception ex)
            {
                LogError($"Error stopping Azure VM: {ex.Message}");
                throw new OperatorException($"Azure VM stop failed: {ex.Message}", "AZURE_VM_STOP_ERROR", ex);
            }
        }

        private async Task<object> VmDeallocateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var vmName = GetRequiredParameter<string>(parameters, "vm_name");

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Compute/virtualMachines/{vmName}/deallocate?api-version=2021-04-01";
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"VM deallocate failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deallocated Azure VM: {vmName}");
                return new { success = true, resourceGroup, vmName };
            }
            catch (Exception ex)
            {
                LogError($"Error deallocating Azure VM: {ex.Message}");
                throw new OperatorException($"Azure VM deallocate failed: {ex.Message}", "AZURE_VM_DEALLOCATE_ERROR", ex);
            }
        }

        private async Task<object> VmCreateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var vmName = GetRequiredParameter<string>(parameters, "vm_name");
            var image = GetRequiredParameter<string>(parameters, "image");
            var size = GetRequiredParameter<string>(parameters, "size");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "location", "eastus" },
                    { "properties", new Dictionary<string, object>
                    {
                        { "hardwareProfile", new Dictionary<string, object> { { "vmSize", size } } },
                        { "storageProfile", new Dictionary<string, object>
                        {
                            { "imageReference", new Dictionary<string, object>
                            {
                                { "publisher", "Canonical" },
                                { "offer", "UbuntuServer" },
                                { "sku", "18.04-LTS" },
                                { "version", "latest" }
                            }}
                        }},
                        { "osProfile", new Dictionary<string, object>
                        {
                            { "computerName", vmName },
                            { "adminUsername", "azureuser" },
                            { "adminPassword", "Azure123456!" }
                        }},
                        { "networkProfile", new Dictionary<string, object>
                        {
                            { "networkInterfaces", new[] { new Dictionary<string, object> { { "id", $"/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Network/networkInterfaces/{vmName}-nic" } } }
                        }}
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Compute/virtualMachines/{vmName}?api-version=2021-04-01";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"VM create failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created Azure VM: {vmName}");
                return new { success = true, resourceGroup, vmName, image, size, vm = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating Azure VM: {ex.Message}");
                throw new OperatorException($"Azure VM create failed: {ex.Message}", "AZURE_VM_CREATE_ERROR", ex);
            }
        }

        private async Task<object> VmGetInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var vmName = GetRequiredParameter<string>(parameters, "vm_name");

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Compute/virtualMachines/{vmName}?api-version=2021-04-01";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"VM get info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved Azure VM info: {vmName}");
                return new { success = true, resourceGroup, vmName, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting Azure VM info: {ex.Message}");
                throw new OperatorException($"Azure VM get info failed: {ex.Message}", "AZURE_VM_GET_INFO_ERROR", ex);
            }
        }

        private async Task<object> StorageListAccountsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/providers/Microsoft.Storage/storageAccounts?api-version=2021-04-01";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage list accounts failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed Azure storage accounts");
                return new { success = true, accounts = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing Azure storage accounts: {ex.Message}");
                throw new OperatorException($"Azure storage list accounts failed: {ex.Message}", "AZURE_STORAGE_LIST_ACCOUNTS_ERROR", ex);
            }
        }

        private async Task<object> StorageCreateAccountAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var accountName = GetRequiredParameter<string>(parameters, "account_name");
            var sku = GetParameter<string>(parameters, "sku", "Standard_LRS");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "location", "eastus" },
                    { "sku", new Dictionary<string, object> { { "name", sku } } },
                    { "kind", "StorageV2" },
                    { "properties", new Dictionary<string, object>
                    {
                        { "supportsHttpsTrafficOnly", true }
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Storage/storageAccounts/{accountName}?api-version=2021-04-01";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage create account failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created Azure storage account: {accountName}");
                return new { success = true, resourceGroup, accountName, sku, account = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating Azure storage account: {ex.Message}");
                throw new OperatorException($"Azure storage create account failed: {ex.Message}", "AZURE_STORAGE_CREATE_ACCOUNT_ERROR", ex);
            }
        }

        private async Task<object> StorageDeleteAccountAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var accountName = GetRequiredParameter<string>(parameters, "account_name");

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Storage/storageAccounts/{accountName}?api-version=2021-04-01";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage delete account failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted Azure storage account: {accountName}");
                return new { success = true, resourceGroup, accountName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting Azure storage account: {ex.Message}");
                throw new OperatorException($"Azure storage delete account failed: {ex.Message}", "AZURE_STORAGE_DELETE_ACCOUNT_ERROR", ex);
            }
        }

        private async Task<object> StorageListContainersAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var accountName = GetRequiredParameter<string>(parameters, "account_name");

            try
            {
                var url = $"https://{accountName}.blob.core.windows.net/?resttype=container";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage list containers failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully listed Azure storage containers: {accountName}");
                return new { success = true, accountName, containers = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing Azure storage containers: {ex.Message}");
                throw new OperatorException($"Azure storage list containers failed: {ex.Message}", "AZURE_STORAGE_LIST_CONTAINERS_ERROR", ex);
            }
        }

        private async Task<object> StorageCreateContainerAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var accountName = GetRequiredParameter<string>(parameters, "account_name");
            var containerName = GetRequiredParameter<string>(parameters, "container_name");

            try
            {
                var url = $"https://{accountName}.blob.core.windows.net/{containerName}?resttype=container";
                var response = await _httpClient.PutAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage create container failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully created Azure storage container: {containerName}");
                return new { success = true, accountName, containerName };
            }
            catch (Exception ex)
            {
                LogError($"Error creating Azure storage container: {ex.Message}");
                throw new OperatorException($"Azure storage create container failed: {ex.Message}", "AZURE_STORAGE_CREATE_CONTAINER_ERROR", ex);
            }
        }

        private async Task<object> StorageDeleteContainerAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var accountName = GetRequiredParameter<string>(parameters, "account_name");
            var containerName = GetRequiredParameter<string>(parameters, "container_name");

            try
            {
                var url = $"https://{accountName}.blob.core.windows.net/{containerName}?resttype=container";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage delete container failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted Azure storage container: {containerName}");
                return new { success = true, accountName, containerName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting Azure storage container: {ex.Message}");
                throw new OperatorException($"Azure storage delete container failed: {ex.Message}", "AZURE_STORAGE_DELETE_CONTAINER_ERROR", ex);
            }
        }

        private async Task<object> StorageUploadBlobAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var accountName = GetRequiredParameter<string>(parameters, "account_name");
            var containerName = GetRequiredParameter<string>(parameters, "container_name");
            var blobName = GetRequiredParameter<string>(parameters, "blob_name");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");

            try
            {
                var url = $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}";
                var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
                var content = new ByteArrayContent(fileContent);

                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage upload blob failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully uploaded blob to Azure storage: {blobName}");
                return new { success = true, accountName, containerName, blobName, filePath };
            }
            catch (Exception ex)
            {
                LogError($"Error uploading blob to Azure storage: {ex.Message}");
                throw new OperatorException($"Azure storage upload blob failed: {ex.Message}", "AZURE_STORAGE_UPLOAD_BLOB_ERROR", ex);
            }
        }

        private async Task<object> StorageDownloadBlobAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var accountName = GetRequiredParameter<string>(parameters, "account_name");
            var containerName = GetRequiredParameter<string>(parameters, "container_name");
            var blobName = GetRequiredParameter<string>(parameters, "blob_name");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");

            try
            {
                var url = $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage download blob failed: {response.StatusCode} - {errorContent}");
                }

                var blobContent = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(filePath, blobContent);

                LogInfo($"Successfully downloaded blob from Azure storage: {blobName}");
                return new { success = true, accountName, containerName, blobName, filePath };
            }
            catch (Exception ex)
            {
                LogError($"Error downloading blob from Azure storage: {ex.Message}");
                throw new OperatorException($"Azure storage download blob failed: {ex.Message}", "AZURE_STORAGE_DOWNLOAD_BLOB_ERROR", ex);
            }
        }

        private async Task<object> StorageDeleteBlobAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var accountName = GetRequiredParameter<string>(parameters, "account_name");
            var containerName = GetRequiredParameter<string>(parameters, "container_name");
            var blobName = GetRequiredParameter<string>(parameters, "blob_name");

            try
            {
                var url = $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Storage delete blob failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted blob from Azure storage: {blobName}");
                return new { success = true, accountName, containerName, blobName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting blob from Azure storage: {ex.Message}");
                throw new OperatorException($"Azure storage delete blob failed: {ex.Message}", "AZURE_STORAGE_DELETE_BLOB_ERROR", ex);
            }
        }

        private async Task<object> FunctionsListAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/providers/Microsoft.Web/sites?api-version=2021-02-01";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Functions list failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed Azure Functions");
                return new { success = true, functions = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing Azure Functions: {ex.Message}");
                throw new OperatorException($"Azure Functions list failed: {ex.Message}", "AZURE_FUNCTIONS_LIST_ERROR", ex);
            }
        }

        private async Task<object> FunctionsInvokeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var functionName = GetRequiredParameter<string>(parameters, "function_name");
            var payload = GetRequiredParameter<string>(parameters, "payload");

            try
            {
                var url = $"https://{functionName}.azurewebsites.net/api/HttpTrigger";
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Functions invoke failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully invoked Azure Function: {functionName}");
                return new { success = true, functionName, payload, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error invoking Azure Function: {ex.Message}");
                throw new OperatorException($"Azure Functions invoke failed: {ex.Message}", "AZURE_FUNCTIONS_INVOKE_ERROR", ex);
            }
        }

        private async Task<object> FunctionsCreateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");
            var runtime = GetRequiredParameter<string>(parameters, "runtime");
            var code = GetRequiredParameter<string>(parameters, "code");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "location", "eastus" },
                    { "properties", new Dictionary<string, object>
                    {
                        { "kind", "functionapp" },
                        { "reserved", true },
                        { "siteConfig", new Dictionary<string, object>
                        {
                            { "runtime", runtime }
                        }}
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/default/providers/Microsoft.Web/sites/{name}?api-version=2021-02-01";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Functions create failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created Azure Function: {name}");
                return new { success = true, name, runtime, function = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating Azure Function: {ex.Message}");
                throw new OperatorException($"Azure Functions create failed: {ex.Message}", "AZURE_FUNCTIONS_CREATE_ERROR", ex);
            }
        }

        private async Task<object> FunctionsDeleteAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/default/providers/Microsoft.Web/sites/{name}?api-version=2021-02-01";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Functions delete failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted Azure Function: {name}");
                return new { success = true, name };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting Azure Function: {ex.Message}");
                throw new OperatorException($"Azure Functions delete failed: {ex.Message}", "AZURE_FUNCTIONS_DELETE_ERROR", ex);
            }
        }

        private async Task<object> SqlListServersAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/providers/Microsoft.Sql/servers?api-version=2021-02-01-preview";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SQL list servers failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo("Successfully listed Azure SQL servers");
                return new { success = true, servers = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error listing Azure SQL servers: {ex.Message}");
                throw new OperatorException($"Azure SQL list servers failed: {ex.Message}", "AZURE_SQL_LIST_SERVERS_ERROR", ex);
            }
        }

        private async Task<object> SqlCreateServerAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var serverName = GetRequiredParameter<string>(parameters, "server_name");
            var adminUser = GetRequiredParameter<string>(parameters, "admin_user");
            var adminPassword = GetRequiredParameter<string>(parameters, "admin_password");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "location", "eastus" },
                    { "properties", new Dictionary<string, object>
                    {
                        { "administratorLogin", adminUser },
                        { "administratorLoginPassword", adminPassword }
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Sql/servers/{serverName}?api-version=2021-02-01-preview";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SQL create server failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created Azure SQL server: {serverName}");
                return new { success = true, resourceGroup, serverName, adminUser, server = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating Azure SQL server: {ex.Message}");
                throw new OperatorException($"Azure SQL create server failed: {ex.Message}", "AZURE_SQL_CREATE_SERVER_ERROR", ex);
            }
        }

        private async Task<object> SqlDeleteServerAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceGroup = GetRequiredParameter<string>(parameters, "resource_group");
            var serverName = GetRequiredParameter<string>(parameters, "server_name");

            try
            {
                var url = $"{_endpoint}/subscriptions/{_subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Sql/servers/{serverName}?api-version=2021-02-01-preview";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SQL delete server failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted Azure SQL server: {serverName}");
                return new { success = true, resourceGroup, serverName };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting Azure SQL server: {ex.Message}");
                throw new OperatorException($"Azure SQL delete server failed: {ex.Message}", "AZURE_SQL_DELETE_SERVER_ERROR", ex);
            }
        }

        private async Task<object> MonitorGetMetricsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceId = GetRequiredParameter<string>(parameters, "resource_id");
            var metricName = GetRequiredParameter<string>(parameters, "metric_name");

            try
            {
                var url = $"{_endpoint}{resourceId}/providers/Microsoft.Insights/metrics?api-version=2018-01-01&metricnames={metricName}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Monitor get metrics failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully retrieved Azure monitoring metrics");
                return new { success = true, resourceId, metricName, metrics = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting Azure monitoring metrics: {ex.Message}");
                throw new OperatorException($"Azure monitor get metrics failed: {ex.Message}", "AZURE_MONITOR_GET_METRICS_ERROR", ex);
            }
        }

        private async Task<object> MonitorPutMetricAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var resourceId = GetRequiredParameter<string>(parameters, "resource_id");
            var metricName = GetRequiredParameter<string>(parameters, "metric_name");
            var value = GetRequiredParameter<double>(parameters, "value");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "value", value },
                    { "metricName", metricName }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_endpoint}{resourceId}/providers/Microsoft.Insights/metrics?api-version=2018-01-01";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Monitor put metric failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully put Azure monitoring metric");
                return new { success = true, resourceId, metricName, value, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error putting Azure monitoring metric: {ex.Message}");
                throw new OperatorException($"Azure monitor put metric failed: {ex.Message}", "AZURE_MONITOR_PUT_METRIC_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var result = await VmListAsync();
                return new { success = true, status = "Connected" };
            }
            catch (Exception ex)
            {
                LogError($"Error testing Azure connection: {ex.Message}");
                throw new OperatorException($"Azure connection test failed: {ex.Message}", "AZURE_CONNECTION_TEST_ERROR", ex);
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var tokenUrl = $"https://login.microsoftonline.com/{_tenantId}/oauth2/token";
                var tokenData = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "resource", "https://management.azure.com/" }
                };

                var tokenContent = new FormUrlEncodedContent(tokenData);
                var tokenResponse = await _httpClient.PostAsync(tokenUrl, tokenContent);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to get Azure access token");
                }

                var tokenResult = await tokenResponse.Content.ReadAsStringAsync();
                var tokenObj = JsonSerializer.Deserialize<Dictionary<string, object>>(tokenResult);
                return tokenObj["access_token"].ToString();
            }
            catch (Exception ex)
            {
                LogError($"Error getting Azure access token: {ex.Message}");
                throw new OperatorException($"Azure access token failed: {ex.Message}", "AZURE_ACCESS_TOKEN_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_subscriptionId))
            {
                throw new OperatorException("Not connected to Azure", "AZURE_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
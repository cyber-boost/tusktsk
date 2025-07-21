using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Linq;

namespace TuskLang.Operators.Database
{
    /// <summary>
    /// Elasticsearch operator for TuskLang
    /// Provides Elasticsearch operations including document indexing, search, and cluster management
    /// </summary>
    public class ElasticsearchOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _baseUrl;
        private string _username;
        private string _password;
        private string _defaultIndex;

        public ElasticsearchOperator() : base("elasticsearch", "Elasticsearch search and analytics operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Elasticsearch", new[] { "url", "username", "password", "default_index" });
            RegisterMethod("index", "Index a document", new[] { "index", "id", "document" });
            RegisterMethod("get", "Get a document by ID", new[] { "index", "id" });
            RegisterMethod("update", "Update a document", new[] { "index", "id", "document" });
            RegisterMethod("delete", "Delete a document", new[] { "index", "id" });
            RegisterMethod("search", "Search documents", new[] { "index", "query", "size", "from" });
            RegisterMethod("bulk_index", "Bulk index documents", new[] { "index", "documents" });
            RegisterMethod("create_index", "Create a new index", new[] { "name", "mappings", "settings" });
            RegisterMethod("delete_index", "Delete an index", new[] { "name" });
            RegisterMethod("list_indices", "List all indices", new string[0]);
            RegisterMethod("get_mapping", "Get index mapping", new[] { "index" });
            RegisterMethod("put_mapping", "Update index mapping", new[] { "index", "mapping" });
            RegisterMethod("refresh", "Refresh an index", new[] { "index" });
            RegisterMethod("flush", "Flush an index", new[] { "index" });
            RegisterMethod("cluster_health", "Get cluster health", new string[0]);
            RegisterMethod("cluster_stats", "Get cluster statistics", new string[0]);
            RegisterMethod("node_stats", "Get node statistics", new string[0]);
            RegisterMethod("ping", "Ping Elasticsearch", new string[0]);
            RegisterMethod("info", "Get Elasticsearch info", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Elasticsearch operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "index":
                        return await IndexAsync(parameters);
                    case "get":
                        return await GetAsync(parameters);
                    case "update":
                        return await UpdateAsync(parameters);
                    case "delete":
                        return await DeleteAsync(parameters);
                    case "search":
                        return await SearchAsync(parameters);
                    case "bulk_index":
                        return await BulkIndexAsync(parameters);
                    case "create_index":
                        return await CreateIndexAsync(parameters);
                    case "delete_index":
                        return await DeleteIndexAsync(parameters);
                    case "list_indices":
                        return await ListIndicesAsync();
                    case "get_mapping":
                        return await GetMappingAsync(parameters);
                    case "put_mapping":
                        return await PutMappingAsync(parameters);
                    case "refresh":
                        return await RefreshAsync(parameters);
                    case "flush":
                        return await FlushAsync(parameters);
                    case "cluster_health":
                        return await ClusterHealthAsync();
                    case "cluster_stats":
                        return await ClusterStatsAsync();
                    case "node_stats":
                        return await NodeStatsAsync();
                    case "ping":
                        return await PingAsync();
                    case "info":
                        return await InfoAsync();
                    default:
                        throw new ArgumentException($"Unknown Elasticsearch method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Elasticsearch method {method}: {ex.Message}");
                throw new OperatorException($"Elasticsearch operation failed: {ex.Message}", "ELASTICSEARCH_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var username = GetParameter<string>(parameters, "username", null);
            var password = GetParameter<string>(parameters, "password", null);
            var defaultIndex = GetParameter<string>(parameters, "default_index", null);

            try
            {
                _baseUrl = url.TrimEnd('/');
                _username = username;
                _password = password;
                _defaultIndex = defaultIndex;

                _httpClient = new HttpClient();

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
                }

                // Test connection
                var response = await _httpClient.GetAsync($"{_baseUrl}/_cluster/health");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to connect to Elasticsearch: {response.StatusCode}");
                }

                LogInfo($"Connected to Elasticsearch at {_baseUrl}");
                return new { success = true, url = _baseUrl, defaultIndex };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Elasticsearch: {ex.Message}");
                throw new OperatorException($"Elasticsearch connection failed: {ex.Message}", "ELASTICSEARCH_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> IndexAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");
            var id = GetParameter<string>(parameters, "id", null);
            var document = GetRequiredParameter<Dictionary<string, object>>(parameters, "document");

            try
            {
                var json = JsonSerializer.Serialize(document);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string url;
                if (!string.IsNullOrEmpty(id))
                {
                    url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_doc/{Uri.EscapeDataString(id)}";
                }
                else
                {
                    url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_doc";
                }

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Index failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully indexed document in {index}");
                return new { success = true, index, id = resultObj.GetValueOrDefault("_id"), result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error indexing document in {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch index failed: {ex.Message}", "ELASTICSEARCH_INDEX_ERROR", ex);
            }
        }

        private async Task<object> GetAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");
            var id = GetRequiredParameter<string>(parameters, "id");

            try
            {
                var url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_doc/{Uri.EscapeDataString(id)}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, index, id, document = resultObj.GetValueOrDefault("_source") };
            }
            catch (Exception ex)
            {
                LogError($"Error getting document {id} from {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch get failed: {ex.Message}", "ELASTICSEARCH_GET_ERROR", ex);
            }
        }

        private async Task<object> UpdateAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");
            var id = GetRequiredParameter<string>(parameters, "id");
            var document = GetRequiredParameter<Dictionary<string, object>>(parameters, "document");

            try
            {
                var updateData = new { doc = document };
                var json = JsonSerializer.Serialize(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_update/{Uri.EscapeDataString(id)}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Update failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully updated document {id} in {index}");
                return new { success = true, index, id, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error updating document {id} in {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch update failed: {ex.Message}", "ELASTICSEARCH_UPDATE_ERROR", ex);
            }
        }

        private async Task<object> DeleteAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");
            var id = GetRequiredParameter<string>(parameters, "id");

            try
            {
                var url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_doc/{Uri.EscapeDataString(id)}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully deleted document {id} from {index}");
                return new { success = true, index, id, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting document {id} from {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch delete failed: {ex.Message}", "ELASTICSEARCH_DELETE_ERROR", ex);
            }
        }

        private async Task<object> SearchAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");
            var query = GetRequiredParameter<Dictionary<string, object>>(parameters, "query");
            var size = GetParameter<int>(parameters, "size", 10);
            var from = GetParameter<int>(parameters, "from", 0);

            try
            {
                var searchData = new
                {
                    query = query,
                    size = size,
                    from = from
                };

                var json = JsonSerializer.Serialize(searchData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_search";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Search failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, index, query, size, from, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error searching in {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch search failed: {ex.Message}", "ELASTICSEARCH_SEARCH_ERROR", ex);
            }
        }

        private async Task<object> BulkIndexAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");
            var documents = GetRequiredParameter<Dictionary<string, object>[]>(parameters, "documents");

            try
            {
                var bulkData = new StringBuilder();
                foreach (var doc in documents)
                {
                    var id = doc.GetValueOrDefault("_id")?.ToString();
                    var source = new Dictionary<string, object>(doc);
                    source.Remove("_id");

                    // Index action
                    var indexAction = new { index = new { _index = index, _id = id } };
                    bulkData.AppendLine(JsonSerializer.Serialize(indexAction));

                    // Document
                    bulkData.AppendLine(JsonSerializer.Serialize(source));
                }

                var content = new StringContent(bulkData.ToString(), Encoding.UTF8, "application/x-ndjson");
                var url = $"{_baseUrl}/_bulk";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Bulk index failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully bulk indexed {documents.Length} documents in {index}");
                return new { success = true, index, count = documents.Length, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error bulk indexing in {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch bulk index failed: {ex.Message}", "ELASTICSEARCH_BULK_INDEX_ERROR", ex);
            }
        }

        private async Task<object> CreateIndexAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");
            var mappings = GetParameter<Dictionary<string, object>>(parameters, "mappings", null);
            var settings = GetParameter<Dictionary<string, object>>(parameters, "settings", null);

            try
            {
                var indexData = new Dictionary<string, object>();
                if (mappings != null) indexData["mappings"] = mappings;
                if (settings != null) indexData["settings"] = settings;

                var json = JsonSerializer.Serialize(indexData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/{Uri.EscapeDataString(name)}";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Create index failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created index {name}");
                return new { success = true, name, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating index {name}: {ex.Message}");
                throw new OperatorException($"Elasticsearch create index failed: {ex.Message}", "ELASTICSEARCH_CREATE_INDEX_ERROR", ex);
            }
        }

        private async Task<object> DeleteIndexAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                var url = $"{_baseUrl}/{Uri.EscapeDataString(name)}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete index failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully deleted index {name}");
                return new { success = true, name, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting index {name}: {ex.Message}");
                throw new OperatorException($"Elasticsearch delete index failed: {ex.Message}", "ELASTICSEARCH_DELETE_INDEX_ERROR", ex);
            }
        }

        private async Task<object> ListIndicesAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_baseUrl}/_cat/indices?format=json";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List indices failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var indices = JsonSerializer.Deserialize<object[]>(result);

                return new { success = true, indices };
            }
            catch (Exception ex)
            {
                LogError($"Error listing indices: {ex.Message}");
                throw new OperatorException($"Elasticsearch list indices failed: {ex.Message}", "ELASTICSEARCH_LIST_INDICES_ERROR", ex);
            }
        }

        private async Task<object> GetMappingAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");

            try
            {
                var url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_mapping";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get mapping failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var mapping = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, index, mapping };
            }
            catch (Exception ex)
            {
                LogError($"Error getting mapping for {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch get mapping failed: {ex.Message}", "ELASTICSEARCH_GET_MAPPING_ERROR", ex);
            }
        }

        private async Task<object> PutMappingAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");
            var mapping = GetRequiredParameter<Dictionary<string, object>>(parameters, "mapping");

            try
            {
                var json = JsonSerializer.Serialize(mapping);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_mapping";
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Put mapping failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully updated mapping for {index}");
                return new { success = true, index, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error updating mapping for {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch put mapping failed: {ex.Message}", "ELASTICSEARCH_PUT_MAPPING_ERROR", ex);
            }
        }

        private async Task<object> RefreshAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");

            try
            {
                var url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_refresh";
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Refresh failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully refreshed index {index}");
                return new { success = true, index, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing index {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch refresh failed: {ex.Message}", "ELASTICSEARCH_REFRESH_ERROR", ex);
            }
        }

        private async Task<object> FlushAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var index = GetParameter<string>(parameters, "index", _defaultIndex) ?? throw new ArgumentException("Index is required");

            try
            {
                var url = $"{_baseUrl}/{Uri.EscapeDataString(index)}/_flush";
                var response = await _httpClient.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Flush failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully flushed index {index}");
                return new { success = true, index, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error flushing index {index}: {ex.Message}");
                throw new OperatorException($"Elasticsearch flush failed: {ex.Message}", "ELASTICSEARCH_FLUSH_ERROR", ex);
            }
        }

        private async Task<object> ClusterHealthAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_baseUrl}/_cluster/health";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Cluster health failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, health };
            }
            catch (Exception ex)
            {
                LogError($"Error getting cluster health: {ex.Message}");
                throw new OperatorException($"Elasticsearch cluster health failed: {ex.Message}", "ELASTICSEARCH_CLUSTER_HEALTH_ERROR", ex);
            }
        }

        private async Task<object> ClusterStatsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_baseUrl}/_cluster/stats";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Cluster stats failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var stats = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, stats };
            }
            catch (Exception ex)
            {
                LogError($"Error getting cluster stats: {ex.Message}");
                throw new OperatorException($"Elasticsearch cluster stats failed: {ex.Message}", "ELASTICSEARCH_CLUSTER_STATS_ERROR", ex);
            }
        }

        private async Task<object> NodeStatsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_baseUrl}/_nodes/stats";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Node stats failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var stats = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, stats };
            }
            catch (Exception ex)
            {
                LogError($"Error getting node stats: {ex.Message}");
                throw new OperatorException($"Elasticsearch node stats failed: {ex.Message}", "ELASTICSEARCH_NODE_STATS_ERROR", ex);
            }
        }

        private async Task<object> PingAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/_cluster/health");
                var success = response.IsSuccessStatusCode;

                return new { success, statusCode = (int)response.StatusCode };
            }
            catch (Exception ex)
            {
                LogError($"Error pinging Elasticsearch: {ex.Message}");
                throw new OperatorException($"Elasticsearch ping failed: {ex.Message}", "ELASTICSEARCH_PING_ERROR", ex);
            }
        }

        private async Task<object> InfoAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}");
                var result = await response.Content.ReadAsStringAsync();
                var info = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, info };
            }
            catch (Exception ex)
            {
                LogError($"Error getting Elasticsearch info: {ex.Message}");
                throw new OperatorException($"Elasticsearch info failed: {ex.Message}", "ELASTICSEARCH_INFO_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_baseUrl))
            {
                throw new OperatorException("Not connected to Elasticsearch server", "ELASTICSEARCH_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            _httpClient?.Dispose();
            base.Dispose();
        }
    }
} 
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
    /// InfluxDB operator for TuskLang
    /// Provides InfluxDB operations including time-series data storage, queries, and management
    /// </summary>
    public class InfluxDbOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _baseUrl;
        private string _token;
        private string _org;
        private string _bucket;

        public InfluxDbOperator() : base("influxdb", "InfluxDB time-series database operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to InfluxDB", new[] { "url", "token", "org", "bucket" });
            RegisterMethod("write", "Write data points", new[] { "measurement", "tags", "fields", "timestamp" });
            RegisterMethod("query", "Query data", new[] { "query" });
            RegisterMethod("query_range", "Query data with time range", new[] { "query", "start", "end" });
            RegisterMethod("create_bucket", "Create a new bucket", new[] { "name", "retention_policy" });
            RegisterMethod("delete_bucket", "Delete a bucket", new[] { "name" });
            RegisterMethod("list_buckets", "List all buckets", new string[0]);
            RegisterMethod("create_measurement", "Create a new measurement", new[] { "name" });
            RegisterMethod("delete_measurement", "Delete a measurement", new[] { "name" });
            RegisterMethod("list_measurements", "List all measurements", new string[0]);
            RegisterMethod("create_user", "Create a new user", new[] { "username", "password" });
            RegisterMethod("delete_user", "Delete a user", new[] { "username" });
            RegisterMethod("list_users", "List all users", new string[0]);
            RegisterMethod("create_org", "Create a new organization", new[] { "name", "description" });
            RegisterMethod("delete_org", "Delete an organization", new[] { "name" });
            RegisterMethod("list_orgs", "List all organizations", new string[0]);
            RegisterMethod("ping", "Ping InfluxDB server", new string[0]);
            RegisterMethod("health", "Check InfluxDB health", new string[0]);
            RegisterMethod("stats", "Get InfluxDB statistics", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing InfluxDB operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "write":
                        return await WriteAsync(parameters);
                    case "query":
                        return await QueryAsync(parameters);
                    case "query_range":
                        return await QueryRangeAsync(parameters);
                    case "create_bucket":
                        return await CreateBucketAsync(parameters);
                    case "delete_bucket":
                        return await DeleteBucketAsync(parameters);
                    case "list_buckets":
                        return await ListBucketsAsync();
                    case "create_measurement":
                        return await CreateMeasurementAsync(parameters);
                    case "delete_measurement":
                        return await DeleteMeasurementAsync(parameters);
                    case "list_measurements":
                        return await ListMeasurementsAsync();
                    case "create_user":
                        return await CreateUserAsync(parameters);
                    case "delete_user":
                        return await DeleteUserAsync(parameters);
                    case "list_users":
                        return await ListUsersAsync();
                    case "create_org":
                        return await CreateOrgAsync(parameters);
                    case "delete_org":
                        return await DeleteOrgAsync(parameters);
                    case "list_orgs":
                        return await ListOrgsAsync();
                    case "ping":
                        return await PingAsync();
                    case "health":
                        return await HealthAsync();
                    case "stats":
                        return await StatsAsync();
                    default:
                        throw new ArgumentException($"Unknown InfluxDB method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing InfluxDB method {method}: {ex.Message}");
                throw new OperatorException($"InfluxDB operation failed: {ex.Message}", "INFLUXDB_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var token = GetRequiredParameter<string>(parameters, "token");
            var org = GetRequiredParameter<string>(parameters, "org");
            var bucket = GetRequiredParameter<string>(parameters, "bucket");

            try
            {
                _baseUrl = url.TrimEnd('/');
                _token = token;
                _org = org;
                _bucket = bucket;

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {token}");

                // Test connection
                var response = await _httpClient.GetAsync($"{_baseUrl}/ping");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to connect to InfluxDB: {response.StatusCode}");
                }

                LogInfo($"Connected to InfluxDB at {_baseUrl}");
                return new { success = true, url = _baseUrl, org, bucket };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to InfluxDB: {ex.Message}");
                throw new OperatorException($"InfluxDB connection failed: {ex.Message}", "INFLUXDB_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> WriteAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var measurement = GetRequiredParameter<string>(parameters, "measurement");
            var tags = GetParameter<Dictionary<string, string>>(parameters, "tags", new Dictionary<string, string>());
            var fields = GetRequiredParameter<Dictionary<string, object>>(parameters, "fields");
            var timestamp = GetParameter<DateTime?>(parameters, "timestamp", null);

            try
            {
                var lineProtocol = BuildLineProtocol(measurement, tags, fields, timestamp);
                var content = new StringContent(lineProtocol, Encoding.UTF8, "text/plain");

                var url = $"{_baseUrl}/api/v2/write?org={Uri.EscapeDataString(_org)}&bucket={Uri.EscapeDataString(_bucket)}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Write failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully wrote data to measurement {measurement}");
                return new { success = true, measurement, tags, fields, timestamp };
            }
            catch (Exception ex)
            {
                LogError($"Error writing to measurement {measurement}: {ex.Message}");
                throw new OperatorException($"InfluxDB write failed: {ex.Message}", "INFLUXDB_WRITE_ERROR", ex);
            }
        }

        private async Task<object> QueryAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var query = GetRequiredParameter<string>(parameters, "query");

            try
            {
                var queryData = new
                {
                    query = query,
                    type = "flux"
                };

                var json = JsonSerializer.Serialize(queryData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/api/v2/query?org={Uri.EscapeDataString(_org)}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Query failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var parsedResult = ParseQueryResult(result);

                return new { success = true, query, result = parsedResult };
            }
            catch (Exception ex)
            {
                LogError($"Error executing query: {ex.Message}");
                throw new OperatorException($"InfluxDB query failed: {ex.Message}", "INFLUXDB_QUERY_ERROR", ex);
            }
        }

        private async Task<object> QueryRangeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var query = GetRequiredParameter<string>(parameters, "query");
            var start = GetRequiredParameter<string>(parameters, "start");
            var end = GetRequiredParameter<string>(parameters, "end");

            try
            {
                var rangeQuery = $"{query} |> range(start: {start}, stop: {end})";
                return await QueryAsync(new Dictionary<string, object> { { "query", rangeQuery } });
            }
            catch (Exception ex)
            {
                LogError($"Error executing range query: {ex.Message}");
                throw new OperatorException($"InfluxDB range query failed: {ex.Message}", "INFLUXDB_QUERY_RANGE_ERROR", ex);
            }
        }

        private async Task<object> CreateBucketAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");
            var retentionPolicy = GetParameter<string>(parameters, "retention_policy", "30d");

            try
            {
                var bucketData = new
                {
                    name = name,
                    retentionRules = new[]
                    {
                        new { type = "expire", everySeconds = ParseRetentionPolicy(retentionPolicy) }
                    }
                };

                var json = JsonSerializer.Serialize(bucketData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/api/v2/buckets?org={Uri.EscapeDataString(_org)}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Create bucket failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                LogInfo($"Successfully created bucket {name}");
                return new { success = true, name, retentionPolicy };
            }
            catch (Exception ex)
            {
                LogError($"Error creating bucket {name}: {ex.Message}");
                throw new OperatorException($"InfluxDB create bucket failed: {ex.Message}", "INFLUXDB_CREATE_BUCKET_ERROR", ex);
            }
        }

        private async Task<object> DeleteBucketAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                var url = $"{_baseUrl}/api/v2/buckets/{Uri.EscapeDataString(name)}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete bucket failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted bucket {name}");
                return new { success = true, name };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting bucket {name}: {ex.Message}");
                throw new OperatorException($"InfluxDB delete bucket failed: {ex.Message}", "INFLUXDB_DELETE_BUCKET_ERROR", ex);
            }
        }

        private async Task<object> ListBucketsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_baseUrl}/api/v2/buckets?org={Uri.EscapeDataString(_org)}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List buckets failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var buckets = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, buckets };
            }
            catch (Exception ex)
            {
                LogError($"Error listing buckets: {ex.Message}");
                throw new OperatorException($"InfluxDB list buckets failed: {ex.Message}", "INFLUXDB_LIST_BUCKETS_ERROR", ex);
            }
        }

        private async Task<object> CreateMeasurementAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                // InfluxDB doesn't have explicit measurement creation, so we'll write a sample point
                var fields = new Dictionary<string, object> { { "created", true } };
                var tags = new Dictionary<string, string> { { "operation", "create" } };

                await WriteAsync(new Dictionary<string, object>
                {
                    { "measurement", name },
                    { "tags", tags },
                    { "fields", fields }
                });

                LogInfo($"Successfully created measurement {name}");
                return new { success = true, name };
            }
            catch (Exception ex)
            {
                LogError($"Error creating measurement {name}: {ex.Message}");
                throw new OperatorException($"InfluxDB create measurement failed: {ex.Message}", "INFLUXDB_CREATE_MEASUREMENT_ERROR", ex);
            }
        }

        private async Task<object> DeleteMeasurementAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                var query = $"from(bucket: \"{_bucket}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{name}\") |> drop()";
                await QueryAsync(new Dictionary<string, object> { { "query", query } });

                LogInfo($"Successfully deleted measurement {name}");
                return new { success = true, name };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting measurement {name}: {ex.Message}");
                throw new OperatorException($"InfluxDB delete measurement failed: {ex.Message}", "INFLUXDB_DELETE_MEASUREMENT_ERROR", ex);
            }
        }

        private async Task<object> ListMeasurementsAsync()
        {
            EnsureConnected();

            try
            {
                var query = $"import \"influxdata/influxdb/schema\" schema.measurements(bucket: \"{_bucket}\")";
                var result = await QueryAsync(new Dictionary<string, object> { { "query", query } });

                return new { success = true, measurements = result };
            }
            catch (Exception ex)
            {
                LogError($"Error listing measurements: {ex.Message}");
                throw new OperatorException($"InfluxDB list measurements failed: {ex.Message}", "INFLUXDB_LIST_MEASUREMENTS_ERROR", ex);
            }
        }

        private async Task<object> CreateUserAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var username = GetRequiredParameter<string>(parameters, "username");
            var password = GetRequiredParameter<string>(parameters, "password");

            try
            {
                var userData = new
                {
                    name = username,
                    password = password
                };

                var json = JsonSerializer.Serialize(userData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/api/v2/users";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Create user failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully created user {username}");
                return new { success = true, username };
            }
            catch (Exception ex)
            {
                LogError($"Error creating user {username}: {ex.Message}");
                throw new OperatorException($"InfluxDB create user failed: {ex.Message}", "INFLUXDB_CREATE_USER_ERROR", ex);
            }
        }

        private async Task<object> DeleteUserAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var username = GetRequiredParameter<string>(parameters, "username");

            try
            {
                var url = $"{_baseUrl}/api/v2/users/{Uri.EscapeDataString(username)}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete user failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted user {username}");
                return new { success = true, username };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting user {username}: {ex.Message}");
                throw new OperatorException($"InfluxDB delete user failed: {ex.Message}", "INFLUXDB_DELETE_USER_ERROR", ex);
            }
        }

        private async Task<object> ListUsersAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_baseUrl}/api/v2/users";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List users failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, users };
            }
            catch (Exception ex)
            {
                LogError($"Error listing users: {ex.Message}");
                throw new OperatorException($"InfluxDB list users failed: {ex.Message}", "INFLUXDB_LIST_USERS_ERROR", ex);
            }
        }

        private async Task<object> CreateOrgAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");
            var description = GetParameter<string>(parameters, "description", "");

            try
            {
                var orgData = new
                {
                    name = name,
                    description = description
                };

                var json = JsonSerializer.Serialize(orgData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/api/v2/orgs";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Create organization failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully created organization {name}");
                return new { success = true, name, description };
            }
            catch (Exception ex)
            {
                LogError($"Error creating organization {name}: {ex.Message}");
                throw new OperatorException($"InfluxDB create organization failed: {ex.Message}", "INFLUXDB_CREATE_ORG_ERROR", ex);
            }
        }

        private async Task<object> DeleteOrgAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                var url = $"{_baseUrl}/api/v2/orgs/{Uri.EscapeDataString(name)}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete organization failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted organization {name}");
                return new { success = true, name };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting organization {name}: {ex.Message}");
                throw new OperatorException($"InfluxDB delete organization failed: {ex.Message}", "INFLUXDB_DELETE_ORG_ERROR", ex);
            }
        }

        private async Task<object> ListOrgsAsync()
        {
            EnsureConnected();

            try
            {
                var url = $"{_baseUrl}/api/v2/orgs";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List organizations failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var orgs = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, orgs };
            }
            catch (Exception ex)
            {
                LogError($"Error listing organizations: {ex.Message}");
                throw new OperatorException($"InfluxDB list organizations failed: {ex.Message}", "INFLUXDB_LIST_ORGS_ERROR", ex);
            }
        }

        private async Task<object> PingAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/ping");
                var success = response.IsSuccessStatusCode;

                return new { success, statusCode = (int)response.StatusCode };
            }
            catch (Exception ex)
            {
                LogError($"Error pinging InfluxDB: {ex.Message}");
                throw new OperatorException($"InfluxDB ping failed: {ex.Message}", "INFLUXDB_PING_ERROR", ex);
            }
        }

        private async Task<object> HealthAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                var result = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, health };
            }
            catch (Exception ex)
            {
                LogError($"Error checking InfluxDB health: {ex.Message}");
                throw new OperatorException($"InfluxDB health check failed: {ex.Message}", "INFLUXDB_HEALTH_ERROR", ex);
            }
        }

        private async Task<object> StatsAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/metrics");
                var result = await response.Content.ReadAsStringAsync();

                return new { success = true, stats = result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting InfluxDB stats: {ex.Message}");
                throw new OperatorException($"InfluxDB stats failed: {ex.Message}", "INFLUXDB_STATS_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_baseUrl))
            {
                throw new OperatorException("Not connected to InfluxDB server", "INFLUXDB_NOT_CONNECTED");
            }
        }

        private string BuildLineProtocol(string measurement, Dictionary<string, string> tags, Dictionary<string, object> fields, DateTime? timestamp)
        {
            var sb = new StringBuilder();
            sb.Append(measurement);

            // Add tags
            if (tags != null && tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    sb.Append($",{tag.Key}={tag.Value}");
                }
            }

            sb.Append(" ");

            // Add fields
            var fieldParts = new List<string>();
            foreach (var field in fields)
            {
                var value = field.Value;
                string fieldValue;

                if (value is string strValue)
                {
                    fieldValue = $"\"{strValue.Replace("\"", "\\\"")}\"";
                }
                else if (value is bool boolValue)
                {
                    fieldValue = boolValue.ToString().ToLower();
                }
                else if (value is int || value is long || value is double || value is float)
                {
                    fieldValue = value.ToString();
                }
                else
                {
                    fieldValue = $"\"{JsonSerializer.Serialize(value)}\"";
                }

                fieldParts.Add($"{field.Key}={fieldValue}");
            }

            sb.Append(string.Join(",", fieldParts));

            // Add timestamp
            if (timestamp.HasValue)
            {
                var nanoSeconds = ((DateTimeOffset)timestamp.Value).ToUnixTimeNanoseconds();
                sb.Append($" {nanoSeconds}");
            }

            return sb.ToString();
        }

        private object ParseQueryResult(string result)
        {
            try
            {
                return JsonSerializer.Deserialize<object>(result);
            }
            catch
            {
                return result;
            }
        }

        private int ParseRetentionPolicy(string policy)
        {
            if (string.IsNullOrEmpty(policy))
                return 30 * 24 * 3600; // 30 days default

            var value = int.Parse(new string(policy.Where(char.IsDigit).ToArray()));
            var unit = new string(policy.Where(char.IsLetter).ToArray()).ToLower();

            return unit switch
            {
                "s" => value,
                "m" => value * 60,
                "h" => value * 3600,
                "d" => value * 24 * 3600,
                "w" => value * 7 * 24 * 3600,
                _ => value * 24 * 3600 // Default to days
            };
        }

        public override void Dispose()
        {
            _httpClient?.Dispose();
            base.Dispose();
        }
    }
} 
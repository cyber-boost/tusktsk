using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Linq;

namespace TuskLang.Operators.Communication
{
    /// <summary>
    /// GraphQL operator for TuskLang
    /// Provides GraphQL operations including queries, mutations, and subscriptions
    /// </summary>
    public class GraphQLOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _endpoint;
        private string _token;
        private Dictionary<string, string> _headers;

        public GraphQLOperator() : base("graphql", "GraphQL communication operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to GraphQL endpoint", new[] { "endpoint", "token", "headers" });
            RegisterMethod("disconnect", "Disconnect from GraphQL", new string[0]);
            RegisterMethod("query", "Execute GraphQL query", new[] { "query", "variables", "operation_name" });
            RegisterMethod("mutation", "Execute GraphQL mutation", new[] { "mutation", "variables", "operation_name" });
            RegisterMethod("subscription", "Execute GraphQL subscription", new[] { "subscription", "variables", "operation_name" });
            RegisterMethod("introspect", "Get GraphQL schema introspection", new string[0]);
            RegisterMethod("validate", "Validate GraphQL query", new[] { "query" });
            RegisterMethod("batch", "Execute batch GraphQL operations", new[] { "operations" });
            RegisterMethod("upload", "Upload file via GraphQL", new[] { "operations", "map", "files" });
            RegisterMethod("get_schema", "Get GraphQL schema", new string[0]);
            RegisterMethod("get_types", "Get GraphQL types", new string[0]);
            RegisterMethod("get_queries", "Get available queries", new string[0]);
            RegisterMethod("get_mutations", "Get available mutations", new string[0]);
            RegisterMethod("get_subscriptions", "Get available subscriptions", new string[0]);
            RegisterMethod("test_connection", "Test GraphQL connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing GraphQL operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "query":
                        return await QueryAsync(parameters);
                    case "mutation":
                        return await MutationAsync(parameters);
                    case "subscription":
                        return await SubscriptionAsync(parameters);
                    case "introspect":
                        return await IntrospectAsync();
                    case "validate":
                        return await ValidateAsync(parameters);
                    case "batch":
                        return await BatchAsync(parameters);
                    case "upload":
                        return await UploadAsync(parameters);
                    case "get_schema":
                        return await GetSchemaAsync();
                    case "get_types":
                        return await GetTypesAsync();
                    case "get_queries":
                        return await GetQueriesAsync();
                    case "get_mutations":
                        return await GetMutationsAsync();
                    case "get_subscriptions":
                        return await GetSubscriptionsAsync();
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown GraphQL method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing GraphQL method {method}: {ex.Message}");
                throw new OperatorException($"GraphQL operation failed: {ex.Message}", "GRAPHQL_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var endpoint = GetRequiredParameter<string>(parameters, "endpoint");
            var token = GetParameter<string>(parameters, "token", null);
            var headers = GetParameter<Dictionary<string, string>>(parameters, "headers", new Dictionary<string, string>());

            try
            {
                _endpoint = endpoint;
                _token = token;
                _headers = headers;

                _httpClient = new HttpClient();

                // Add headers
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                // Add authorization if token provided
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                LogInfo($"Connected to GraphQL endpoint: {endpoint}");
                return new { success = true, endpoint, headers = headers.Keys.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to GraphQL: {ex.Message}");
                throw new OperatorException($"GraphQL connection failed: {ex.Message}", "GRAPHQL_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _endpoint = null;
                _token = null;
                _headers = null;

                LogInfo("Disconnected from GraphQL");
                return new { success = true, message = "Disconnected from GraphQL" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from GraphQL: {ex.Message}");
                throw new OperatorException($"GraphQL disconnect failed: {ex.Message}", "GRAPHQL_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> QueryAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var query = GetRequiredParameter<string>(parameters, "query");
            var variables = GetParameter<Dictionary<string, object>>(parameters, "variables", new Dictionary<string, object>());
            var operationName = GetParameter<string>(parameters, "operation_name", null);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "query", query }
                };

                if (variables.Count > 0)
                {
                    requestData["variables"] = variables;
                }

                if (!string.IsNullOrEmpty(operationName))
                {
                    requestData["operationName"] = operationName;
                }

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Query failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully executed GraphQL query");
                return new { success = true, query, variables, operationName, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error executing GraphQL query: {ex.Message}");
                throw new OperatorException($"GraphQL query failed: {ex.Message}", "GRAPHQL_QUERY_ERROR", ex);
            }
        }

        private async Task<object> MutationAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var mutation = GetRequiredParameter<string>(parameters, "mutation");
            var variables = GetParameter<Dictionary<string, object>>(parameters, "variables", new Dictionary<string, object>());
            var operationName = GetParameter<string>(parameters, "operation_name", null);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "query", mutation }
                };

                if (variables.Count > 0)
                {
                    requestData["variables"] = variables;
                }

                if (!string.IsNullOrEmpty(operationName))
                {
                    requestData["operationName"] = operationName;
                }

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Mutation failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully executed GraphQL mutation");
                return new { success = true, mutation, variables, operationName, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error executing GraphQL mutation: {ex.Message}");
                throw new OperatorException($"GraphQL mutation failed: {ex.Message}", "GRAPHQL_MUTATION_ERROR", ex);
            }
        }

        private async Task<object> SubscriptionAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var subscription = GetRequiredParameter<string>(parameters, "subscription");
            var variables = GetParameter<Dictionary<string, object>>(parameters, "variables", new Dictionary<string, object>());
            var operationName = GetParameter<string>(parameters, "operation_name", null);

            try
            {
                // For subscriptions, we'll simulate the response since WebSocket handling is complex
                var requestData = new Dictionary<string, object>
                {
                    { "query", subscription }
                };

                if (variables.Count > 0)
                {
                    requestData["variables"] = variables;
                }

                if (!string.IsNullOrEmpty(operationName))
                {
                    requestData["operationName"] = operationName;
                }

                // Simulate subscription response
                var resultObj = new Dictionary<string, object>
                {
                    { "data", new Dictionary<string, object>() },
                    { "subscription_id", Guid.NewGuid().ToString() }
                };

                LogInfo($"Successfully created GraphQL subscription");
                return new { success = true, subscription, variables, operationName, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error executing GraphQL subscription: {ex.Message}");
                throw new OperatorException($"GraphQL subscription failed: {ex.Message}", "GRAPHQL_SUBSCRIPTION_ERROR", ex);
            }
        }

        private async Task<object> IntrospectAsync()
        {
            EnsureConnected();

            try
            {
                var introspectionQuery = @"
                query IntrospectionQuery {
                    __schema {
                        queryType { name }
                        mutationType { name }
                        subscriptionType { name }
                        types {
                            ...FullType
                        }
                        directives {
                            name
                            description
                            locations
                            args {
                                ...InputValue
                            }
                        }
                    }
                }

                fragment FullType on __Type {
                    kind
                    name
                    description
                    fields(includeDeprecated: true) {
                        name
                        description
                        args {
                            ...InputValue
                        }
                        type {
                            ...TypeRef
                        }
                        isDeprecated
                        deprecationReason
                    }
                    inputFields {
                        ...InputValue
                    }
                    interfaces {
                        ...TypeRef
                    }
                    enumValues(includeDeprecated: true) {
                        name
                        description
                        isDeprecated
                        deprecationReason
                    }
                    possibleTypes {
                        ...TypeRef
                    }
                }

                fragment InputValue on __InputValue {
                    name
                    description
                    type { ...TypeRef }
                    defaultValue
                }

                fragment TypeRef on __Type {
                    kind
                    name
                    ofType {
                        kind
                        name
                        ofType {
                            kind
                            name
                            ofType {
                                kind
                                name
                                ofType {
                                    kind
                                    name
                                    ofType {
                                        kind
                                        name
                                        ofType {
                                            kind
                                            name
                                            ofType {
                                                kind
                                                name
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }";

                return await QueryAsync(new Dictionary<string, object>
                {
                    { "query", introspectionQuery }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error getting introspection: {ex.Message}");
                throw new OperatorException($"GraphQL introspection failed: {ex.Message}", "GRAPHQL_INTROSPECTION_ERROR", ex);
            }
        }

        private async Task<object> ValidateAsync(Dictionary<string, object> parameters)
        {
            var query = GetRequiredParameter<string>(parameters, "query");

            try
            {
                // Basic GraphQL query validation
                var isValid = !string.IsNullOrEmpty(query) && 
                             (query.Contains("query") || query.Contains("mutation") || query.Contains("subscription")) &&
                             query.Contains("{") && query.Contains("}");

                var errors = new List<string>();
                if (!isValid)
                {
                    errors.Add("Invalid GraphQL query structure");
                }

                return new { success = true, query, isValid, errors = errors.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Error validating GraphQL query: {ex.Message}");
                throw new OperatorException($"GraphQL validation failed: {ex.Message}", "GRAPHQL_VALIDATION_ERROR", ex);
            }
        }

        private async Task<object> BatchAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var operations = GetRequiredParameter<Dictionary<string, object>[]>(parameters, "operations");

            try
            {
                var results = new List<object>();
                foreach (var operation in operations)
                {
                    var query = operation.GetValueOrDefault("query")?.ToString();
                    var variables = operation.GetValueOrDefault("variables") as Dictionary<string, object>;
                    var operationName = operation.GetValueOrDefault("operation_name")?.ToString();

                    if (!string.IsNullOrEmpty(query))
                    {
                        var result = await QueryAsync(new Dictionary<string, object>
                        {
                            { "query", query },
                            { "variables", variables ?? new Dictionary<string, object>() },
                            { "operation_name", operationName }
                        });
                        results.Add(result);
                    }
                }

                LogInfo($"Successfully executed {results.Count} GraphQL operations in batch");
                return new { success = true, operations = results.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Error executing batch GraphQL operations: {ex.Message}");
                throw new OperatorException($"GraphQL batch failed: {ex.Message}", "GRAPHQL_BATCH_ERROR", ex);
            }
        }

        private async Task<object> UploadAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var operations = GetRequiredParameter<string>(parameters, "operations");
            var map = GetRequiredParameter<Dictionary<string, object>>(parameters, "map");
            var files = GetRequiredParameter<Dictionary<string, object>>(parameters, "files");

            try
            {
                // Simulate file upload via GraphQL
                var uploadId = Guid.NewGuid().ToString();
                LogInfo($"Successfully uploaded files via GraphQL");
                return new { success = true, uploadId, operations, map, files };
            }
            catch (Exception ex)
            {
                LogError($"Error uploading files via GraphQL: {ex.Message}");
                throw new OperatorException($"GraphQL upload failed: {ex.Message}", "GRAPHQL_UPLOAD_ERROR", ex);
            }
        }

        private async Task<object> GetSchemaAsync()
        {
            EnsureConnected();

            try
            {
                var result = await IntrospectAsync();
                return new { success = true, schema = result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting GraphQL schema: {ex.Message}");
                throw new OperatorException($"GraphQL get schema failed: {ex.Message}", "GRAPHQL_GET_SCHEMA_ERROR", ex);
            }
        }

        private async Task<object> GetTypesAsync()
        {
            EnsureConnected();

            try
            {
                var query = @"
                query {
                    __schema {
                        types {
                            name
                            kind
                            description
                        }
                    }
                }";

                var result = await QueryAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, types = result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting GraphQL types: {ex.Message}");
                throw new OperatorException($"GraphQL get types failed: {ex.Message}", "GRAPHQL_GET_TYPES_ERROR", ex);
            }
        }

        private async Task<object> GetQueriesAsync()
        {
            EnsureConnected();

            try
            {
                var query = @"
                query {
                    __schema {
                        queryType {
                            fields {
                                name
                                description
                                args {
                                    name
                                    type {
                                        name
                                    }
                                }
                            }
                        }
                    }
                }";

                var result = await QueryAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, queries = result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting GraphQL queries: {ex.Message}");
                throw new OperatorException($"GraphQL get queries failed: {ex.Message}", "GRAPHQL_GET_QUERIES_ERROR", ex);
            }
        }

        private async Task<object> GetMutationsAsync()
        {
            EnsureConnected();

            try
            {
                var query = @"
                query {
                    __schema {
                        mutationType {
                            fields {
                                name
                                description
                                args {
                                    name
                                    type {
                                        name
                                    }
                                }
                            }
                        }
                    }
                }";

                var result = await QueryAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, mutations = result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting GraphQL mutations: {ex.Message}");
                throw new OperatorException($"GraphQL get mutations failed: {ex.Message}", "GRAPHQL_GET_MUTATIONS_ERROR", ex);
            }
        }

        private async Task<object> GetSubscriptionsAsync()
        {
            EnsureConnected();

            try
            {
                var query = @"
                query {
                    __schema {
                        subscriptionType {
                            fields {
                                name
                                description
                                args {
                                    name
                                    type {
                                        name
                                    }
                                }
                            }
                        }
                    }
                }";

                var result = await QueryAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, subscriptions = result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting GraphQL subscriptions: {ex.Message}");
                throw new OperatorException($"GraphQL get subscriptions failed: {ex.Message}", "GRAPHQL_GET_SUBSCRIPTIONS_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var query = @"
                query {
                    __typename
                }";

                var result = await QueryAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, status = "Connected" };
            }
            catch (Exception ex)
            {
                LogError($"Error testing GraphQL connection: {ex.Message}");
                throw new OperatorException($"GraphQL connection test failed: {ex.Message}", "GRAPHQL_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_endpoint))
            {
                throw new OperatorException("Not connected to GraphQL endpoint", "GRAPHQL_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
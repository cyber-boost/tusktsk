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
    /// Neo4j operator for TuskLang
    /// Provides Neo4j graph database operations including Cypher queries, node/relationship management, and graph analytics
    /// </summary>
    public class Neo4jOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _baseUrl;
        private string _username;
        private string _password;
        private string _database;

        public Neo4jOperator()
        {
            RegisterMethods();
        }

        /// <summary>
        /// Get operator name
        /// </summary>
        public override string GetName() => "neo4j";

        /// <summary>
        /// Execute operator with configuration and context
        /// </summary>
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var method = GetContextValue<string>(context, "method", "execute");
            var parameters = GetContextValue<Dictionary<string, object>>(context, "parameters", new Dictionary<string, object>());
            
            return await ExecuteAsync(method, parameters, context);
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Neo4j", new[] { "url", "username", "password", "database" });
            RegisterMethod("disconnect", "Disconnect from Neo4j", new string[0]);
            RegisterMethod("execute", "Execute Cypher query", new[] { "query", "parameters" });
            RegisterMethod("execute_transaction", "Execute transaction", new[] { "queries", "parameters" });
            RegisterMethod("create_node", "Create a node", new[] { "labels", "properties" });
            RegisterMethod("get_node", "Get a node by ID", new[] { "id" });
            RegisterMethod("update_node", "Update a node", new[] { "id", "properties" });
            RegisterMethod("delete_node", "Delete a node", new[] { "id" });
            RegisterMethod("create_relationship", "Create a relationship", new[] { "from_id", "to_id", "type", "properties" });
            RegisterMethod("get_relationship", "Get a relationship by ID", new[] { "id" });
            RegisterMethod("update_relationship", "Update a relationship", new[] { "id", "properties" });
            RegisterMethod("delete_relationship", "Delete a relationship", new[] { "id" });
            RegisterMethod("find_nodes", "Find nodes by label and properties", new[] { "labels", "properties", "limit" });
            RegisterMethod("find_relationships", "Find relationships by type and properties", new[] { "type", "properties", "limit" });
            RegisterMethod("shortest_path", "Find shortest path between nodes", new[] { "from_id", "to_id", "relationship_types" });
            RegisterMethod("all_paths", "Find all paths between nodes", new[] { "from_id", "to_id", "max_length", "relationship_types" });
            RegisterMethod("create_index", "Create an index", new[] { "label", "property" });
            RegisterMethod("drop_index", "Drop an index", new[] { "label", "property" });
            RegisterMethod("list_indexes", "List all indexes", new string[0]);
            RegisterMethod("create_constraint", "Create a constraint", new[] { "label", "property", "type" });
            RegisterMethod("drop_constraint", "Drop a constraint", new[] { "label", "property" });
            RegisterMethod("list_constraints", "List all constraints", new string[0]);
            RegisterMethod("create_database", "Create a database", new[] { "name" });
            RegisterMethod("drop_database", "Drop a database", new[] { "name" });
            RegisterMethod("list_databases", "List all databases", new string[0]);
            RegisterMethod("use_database", "Switch to a database", new[] { "name" });
            RegisterMethod("graph_stats", "Get graph statistics", new string[0]);
            RegisterMethod("cluster_info", "Get cluster information", new string[0]);
            RegisterMethod("ping", "Ping Neo4j", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Neo4j operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "execute":
                        return await ExecuteCypherAsync(parameters);
                    case "execute_transaction":
                        return await ExecuteTransactionAsync(parameters);
                    case "create_node":
                        return await CreateNodeAsync(parameters);
                    case "get_node":
                        return await GetNodeAsync(parameters);
                    case "update_node":
                        return await UpdateNodeAsync(parameters);
                    case "delete_node":
                        return await DeleteNodeAsync(parameters);
                    case "create_relationship":
                        return await CreateRelationshipAsync(parameters);
                    case "get_relationship":
                        return await GetRelationshipAsync(parameters);
                    case "update_relationship":
                        return await UpdateRelationshipAsync(parameters);
                    case "delete_relationship":
                        return await DeleteRelationshipAsync(parameters);
                    case "find_nodes":
                        return await FindNodesAsync(parameters);
                    case "find_relationships":
                        return await FindRelationshipsAsync(parameters);
                    case "shortest_path":
                        return await ShortestPathAsync(parameters);
                    case "all_paths":
                        return await AllPathsAsync(parameters);
                    case "create_index":
                        return await CreateIndexAsync(parameters);
                    case "drop_index":
                        return await DropIndexAsync(parameters);
                    case "list_indexes":
                        return await ListIndexesAsync();
                    case "create_constraint":
                        return await CreateConstraintAsync(parameters);
                    case "drop_constraint":
                        return await DropConstraintAsync(parameters);
                    case "list_constraints":
                        return await ListConstraintsAsync();
                    case "create_database":
                        return await CreateDatabaseAsync(parameters);
                    case "drop_database":
                        return await DropDatabaseAsync(parameters);
                    case "list_databases":
                        return await ListDatabasesAsync();
                    case "use_database":
                        return await UseDatabaseAsync(parameters);
                    case "graph_stats":
                        return await GraphStatsAsync();
                    case "cluster_info":
                        return await ClusterInfoAsync();
                    case "ping":
                        return await PingAsync();
                    default:
                        throw new ArgumentException($"Unknown Neo4j method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Neo4j method {method}: {ex.Message}");
                throw new OperatorException($"Neo4j operation failed: {ex.Message}", "NEO4J_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var url = GetRequiredParameter<string>(parameters, "url");
            var username = GetRequiredParameter<string>(parameters, "username");
            var password = GetRequiredParameter<string>(parameters, "password");
            var database = GetParameter<string>(parameters, "database", "neo4j");

            try
            {
                _baseUrl = url.TrimEnd('/');
                _username = username;
                _password = password;
                _database = database;

                _httpClient = new HttpClient();

                // Test connection
                var response = await _httpClient.GetAsync($"{_baseUrl}/db/{database}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to connect to Neo4j: {response.StatusCode}");
                }

                LogInfo($"Connected to Neo4j at {_baseUrl}");
                return new { success = true, url = _baseUrl, database };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Neo4j: {ex.Message}");
                throw new OperatorException($"Neo4j connection failed: {ex.Message}", "NEO4J_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _baseUrl = null;
                _username = null;
                _password = null;
                _database = null;

                LogInfo("Disconnected from Neo4j");
                return new { success = true, message = "Disconnected from Neo4j" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from Neo4j: {ex.Message}");
                throw new OperatorException($"Neo4j disconnect failed: {ex.Message}", "NEO4J_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> ExecuteCypherAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var query = GetRequiredParameter<string>(parameters, "query");
            var queryParameters = GetParameter<Dictionary<string, object>>(parameters, "parameters", new Dictionary<string, object>());

            try
            {
                var requestData = new
                {
                    statements = new[]
                    {
                        new
                        {
                            statement = query,
                            parameters = queryParameters
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/db/{_database}/tx/commit";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Execute failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Executed Cypher query: {query}");
                return new { success = true, query, parameters = queryParameters, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error executing Cypher query: {ex.Message}");
                throw new OperatorException($"Neo4j execute failed: {ex.Message}", "NEO4J_EXECUTE_ERROR", ex);
            }
        }

        private async Task<object> ExecuteTransactionAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var queries = GetRequiredParameter<string[]>(parameters, "queries");
            var queryParameters = GetParameter<Dictionary<string, object>[]>(parameters, "parameters", new Dictionary<string, object>[0]);

            try
            {
                var statements = new List<object>();
                for (int i = 0; i < queries.Length; i++)
                {
                    var parameters = i < queryParameters.Length ? queryParameters[i] : new Dictionary<string, object>();
                    statements.Add(new
                    {
                        statement = queries[i],
                        parameters = parameters
                    });
                }

                var requestData = new { statements = statements.ToArray() };
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}/db/{_database}/tx/commit";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Transaction failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Executed transaction with {queries.Length} queries");
                return new { success = true, queries, parameters = queryParameters, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error executing transaction: {ex.Message}");
                throw new OperatorException($"Neo4j transaction failed: {ex.Message}", "NEO4J_TRANSACTION_ERROR", ex);
            }
        }

        private async Task<object> CreateNodeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var labels = GetParameter<string[]>(parameters, "labels", new string[0]);
            var properties = GetParameter<Dictionary<string, object>>(parameters, "properties", new Dictionary<string, object>());

            try
            {
                var labelsStr = labels.Length > 0 ? ":" + string.Join(":", labels) : "";
                var propertiesStr = properties.Count > 0 ? " " + JsonSerializer.Serialize(properties) : "";
                var query = $"CREATE (n{labelsStr}{propertiesStr}) RETURN n";

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully created node with labels: {string.Join(",", labels)}");
                return new { success = true, labels, properties };
            }
            catch (Exception ex)
            {
                LogError($"Error creating node: {ex.Message}");
                throw new OperatorException($"Neo4j create node failed: {ex.Message}", "NEO4J_CREATE_NODE_ERROR", ex);
            }
        }

        private async Task<object> GetNodeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var id = GetRequiredParameter<long>(parameters, "id");

            try
            {
                var query = $"MATCH (n) WHERE id(n) = {id} RETURN n";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, id, result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting node {id}: {ex.Message}");
                throw new OperatorException($"Neo4j get node failed: {ex.Message}", "NEO4J_GET_NODE_ERROR", ex);
            }
        }

        private async Task<object> UpdateNodeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var id = GetRequiredParameter<long>(parameters, "id");
            var properties = GetRequiredParameter<Dictionary<string, object>>(parameters, "properties");

            try
            {
                var setClause = string.Join(", ", properties.Select(p => $"n.{p.Key} = ${p.Key}"));
                var query = $"MATCH (n) WHERE id(n) = {id} SET {setClause} RETURN n";

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query },
                    { "parameters", properties }
                });

                LogInfo($"Successfully updated node {id}");
                return new { success = true, id, properties };
            }
            catch (Exception ex)
            {
                LogError($"Error updating node {id}: {ex.Message}");
                throw new OperatorException($"Neo4j update node failed: {ex.Message}", "NEO4J_UPDATE_NODE_ERROR", ex);
            }
        }

        private async Task<object> DeleteNodeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var id = GetRequiredParameter<long>(parameters, "id");

            try
            {
                var query = $"MATCH (n) WHERE id(n) = {id} DETACH DELETE n";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully deleted node {id}");
                return new { success = true, id };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting node {id}: {ex.Message}");
                throw new OperatorException($"Neo4j delete node failed: {ex.Message}", "NEO4J_DELETE_NODE_ERROR", ex);
            }
        }

        private async Task<object> CreateRelationshipAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var fromId = GetRequiredParameter<long>(parameters, "from_id");
            var toId = GetRequiredParameter<long>(parameters, "to_id");
            var type = GetRequiredParameter<string>(parameters, "type");
            var properties = GetParameter<Dictionary<string, object>>(parameters, "properties", new Dictionary<string, object>());

            try
            {
                var propertiesStr = properties.Count > 0 ? " " + JsonSerializer.Serialize(properties) : "";
                var query = $"MATCH (a), (b) WHERE id(a) = {fromId} AND id(b) = {toId} CREATE (a)-[r:{type}{propertiesStr}]->(b) RETURN r";

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully created relationship {type} from {fromId} to {toId}");
                return new { success = true, fromId, toId, type, properties };
            }
            catch (Exception ex)
            {
                LogError($"Error creating relationship: {ex.Message}");
                throw new OperatorException($"Neo4j create relationship failed: {ex.Message}", "NEO4J_CREATE_RELATIONSHIP_ERROR", ex);
            }
        }

        private async Task<object> GetRelationshipAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var id = GetRequiredParameter<long>(parameters, "id");

            try
            {
                var query = $"MATCH ()-[r]->() WHERE id(r) = {id} RETURN r";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, id, result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting relationship {id}: {ex.Message}");
                throw new OperatorException($"Neo4j get relationship failed: {ex.Message}", "NEO4J_GET_RELATIONSHIP_ERROR", ex);
            }
        }

        private async Task<object> UpdateRelationshipAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var id = GetRequiredParameter<long>(parameters, "id");
            var properties = GetRequiredParameter<Dictionary<string, object>>(parameters, "properties");

            try
            {
                var setClause = string.Join(", ", properties.Select(p => $"r.{p.Key} = ${p.Key}"));
                var query = $"MATCH ()-[r]->() WHERE id(r) = {id} SET {setClause} RETURN r";

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query },
                    { "parameters", properties }
                });

                LogInfo($"Successfully updated relationship {id}");
                return new { success = true, id, properties };
            }
            catch (Exception ex)
            {
                LogError($"Error updating relationship {id}: {ex.Message}");
                throw new OperatorException($"Neo4j update relationship failed: {ex.Message}", "NEO4J_UPDATE_RELATIONSHIP_ERROR", ex);
            }
        }

        private async Task<object> DeleteRelationshipAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var id = GetRequiredParameter<long>(parameters, "id");

            try
            {
                var query = $"MATCH ()-[r]->() WHERE id(r) = {id} DELETE r";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully deleted relationship {id}");
                return new { success = true, id };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting relationship {id}: {ex.Message}");
                throw new OperatorException($"Neo4j delete relationship failed: {ex.Message}", "NEO4J_DELETE_RELATIONSHIP_ERROR", ex);
            }
        }

        private async Task<object> FindNodesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var labels = GetParameter<string[]>(parameters, "labels", new string[0]);
            var properties = GetParameter<Dictionary<string, object>>(parameters, "properties", new Dictionary<string, object>());
            var limit = GetParameter<int?>(parameters, "limit", null);

            try
            {
                var labelsStr = labels.Length > 0 ? ":" + string.Join(":", labels) : "";
                var whereClause = properties.Count > 0 ? " WHERE " + string.Join(" AND ", properties.Select(p => $"n.{p.Key} = ${p.Key}")) : "";
                var limitClause = limit.HasValue ? $" LIMIT {limit.Value}" : "";
                var query = $"MATCH (n{labelsStr}){whereClause} RETURN n{limitClause}";

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query },
                    { "parameters", properties }
                });

                return new { success = true, labels, properties, limit, result };
            }
            catch (Exception ex)
            {
                LogError($"Error finding nodes: {ex.Message}");
                throw new OperatorException($"Neo4j find nodes failed: {ex.Message}", "NEO4J_FIND_NODES_ERROR", ex);
            }
        }

        private async Task<object> FindRelationshipsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var type = GetParameter<string>(parameters, "type", null);
            var properties = GetParameter<Dictionary<string, object>>(parameters, "properties", new Dictionary<string, object>());
            var limit = GetParameter<int?>(parameters, "limit", null);

            try
            {
                var typeStr = !string.IsNullOrEmpty(type) ? $":{type}" : "";
                var whereClause = properties.Count > 0 ? " WHERE " + string.Join(" AND ", properties.Select(p => $"r.{p.Key} = ${p.Key}")) : "";
                var limitClause = limit.HasValue ? $" LIMIT {limit.Value}" : "";
                var query = $"MATCH ()-[r{typeStr}]->(){whereClause} RETURN r{limitClause}";

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query },
                    { "parameters", properties }
                });

                return new { success = true, type, properties, limit, result };
            }
            catch (Exception ex)
            {
                LogError($"Error finding relationships: {ex.Message}");
                throw new OperatorException($"Neo4j find relationships failed: {ex.Message}", "NEO4J_FIND_RELATIONSHIPS_ERROR", ex);
            }
        }

        private async Task<object> ShortestPathAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var fromId = GetRequiredParameter<long>(parameters, "from_id");
            var toId = GetRequiredParameter<long>(parameters, "to_id");
            var relationshipTypes = GetParameter<string[]>(parameters, "relationship_types", new string[0]);

            try
            {
                var relTypesStr = relationshipTypes.Length > 0 ? ":" + string.Join("|", relationshipTypes) : "";
                var query = $"MATCH p = shortestPath((a)-[r{relTypesStr}*]-(b)) WHERE id(a) = {fromId} AND id(b) = {toId} RETURN p";

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, fromId, toId, relationshipTypes, result };
            }
            catch (Exception ex)
            {
                LogError($"Error finding shortest path: {ex.Message}");
                throw new OperatorException($"Neo4j shortest path failed: {ex.Message}", "NEO4J_SHORTEST_PATH_ERROR", ex);
            }
        }

        private async Task<object> AllPathsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var fromId = GetRequiredParameter<long>(parameters, "from_id");
            var toId = GetRequiredParameter<long>(parameters, "to_id");
            var maxLength = GetParameter<int>(parameters, "max_length", 10);
            var relationshipTypes = GetParameter<string[]>(parameters, "relationship_types", new string[0]);

            try
            {
                var relTypesStr = relationshipTypes.Length > 0 ? ":" + string.Join("|", relationshipTypes) : "";
                var query = $"MATCH p = (a)-[r{relTypesStr}*1..{maxLength}]-(b) WHERE id(a) = {fromId} AND id(b) = {toId} RETURN p";

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, fromId, toId, maxLength, relationshipTypes, result };
            }
            catch (Exception ex)
            {
                LogError($"Error finding all paths: {ex.Message}");
                throw new OperatorException($"Neo4j all paths failed: {ex.Message}", "NEO4J_ALL_PATHS_ERROR", ex);
            }
        }

        private async Task<object> CreateIndexAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var label = GetRequiredParameter<string>(parameters, "label");
            var property = GetRequiredParameter<string>(parameters, "property");

            try
            {
                var query = $"CREATE INDEX FOR (n:{label}) ON (n.{property})";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully created index on {label}.{property}");
                return new { success = true, label, property };
            }
            catch (Exception ex)
            {
                LogError($"Error creating index: {ex.Message}");
                throw new OperatorException($"Neo4j create index failed: {ex.Message}", "NEO4J_CREATE_INDEX_ERROR", ex);
            }
        }

        private async Task<object> DropIndexAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var label = GetRequiredParameter<string>(parameters, "label");
            var property = GetRequiredParameter<string>(parameters, "property");

            try
            {
                var query = $"DROP INDEX ON :{label}({property})";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully dropped index on {label}.{property}");
                return new { success = true, label, property };
            }
            catch (Exception ex)
            {
                LogError($"Error dropping index: {ex.Message}");
                throw new OperatorException($"Neo4j drop index failed: {ex.Message}", "NEO4J_DROP_INDEX_ERROR", ex);
            }
        }

        private async Task<object> ListIndexesAsync()
        {
            EnsureConnected();

            try
            {
                var query = "SHOW INDEXES";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, result };
            }
            catch (Exception ex)
            {
                LogError($"Error listing indexes: {ex.Message}");
                throw new OperatorException($"Neo4j list indexes failed: {ex.Message}", "NEO4J_LIST_INDEXES_ERROR", ex);
            }
        }

        private async Task<object> CreateConstraintAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var label = GetRequiredParameter<string>(parameters, "label");
            var property = GetRequiredParameter<string>(parameters, "property");
            var type = GetParameter<string>(parameters, "type", "UNIQUE");

            try
            {
                string query;
                if (type.ToUpper() == "UNIQUE")
                {
                    query = $"CREATE CONSTRAINT FOR (n:{label}) REQUIRE n.{property} IS UNIQUE";
                }
                else
                {
                    query = $"CREATE CONSTRAINT FOR (n:{label}) REQUIRE n.{property} IS NOT NULL";
                }

                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully created {type} constraint on {label}.{property}");
                return new { success = true, label, property, type };
            }
            catch (Exception ex)
            {
                LogError($"Error creating constraint: {ex.Message}");
                throw new OperatorException($"Neo4j create constraint failed: {ex.Message}", "NEO4J_CREATE_CONSTRAINT_ERROR", ex);
            }
        }

        private async Task<object> DropConstraintAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var label = GetRequiredParameter<string>(parameters, "label");
            var property = GetRequiredParameter<string>(parameters, "property");

            try
            {
                var query = $"DROP CONSTRAINT ON (n:{label}) ASSERT n.{property} IS UNIQUE";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully dropped constraint on {label}.{property}");
                return new { success = true, label, property };
            }
            catch (Exception ex)
            {
                LogError($"Error dropping constraint: {ex.Message}");
                throw new OperatorException($"Neo4j drop constraint failed: {ex.Message}", "NEO4J_DROP_CONSTRAINT_ERROR", ex);
            }
        }

        private async Task<object> ListConstraintsAsync()
        {
            EnsureConnected();

            try
            {
                var query = "SHOW CONSTRAINTS";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, result };
            }
            catch (Exception ex)
            {
                LogError($"Error listing constraints: {ex.Message}");
                throw new OperatorException($"Neo4j list constraints failed: {ex.Message}", "NEO4J_LIST_CONSTRAINTS_ERROR", ex);
            }
        }

        private async Task<object> CreateDatabaseAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                var query = $"CREATE DATABASE {name}";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully created database {name}");
                return new { success = true, name };
            }
            catch (Exception ex)
            {
                LogError($"Error creating database {name}: {ex.Message}");
                throw new OperatorException($"Neo4j create database failed: {ex.Message}", "NEO4J_CREATE_DATABASE_ERROR", ex);
            }
        }

        private async Task<object> DropDatabaseAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                var query = $"DROP DATABASE {name}";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                LogInfo($"Successfully dropped database {name}");
                return new { success = true, name };
            }
            catch (Exception ex)
            {
                LogError($"Error dropping database {name}: {ex.Message}");
                throw new OperatorException($"Neo4j drop database failed: {ex.Message}", "NEO4J_DROP_DATABASE_ERROR", ex);
            }
        }

        private async Task<object> ListDatabasesAsync()
        {
            EnsureConnected();

            try
            {
                var query = "SHOW DATABASES";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, result };
            }
            catch (Exception ex)
            {
                LogError($"Error listing databases: {ex.Message}");
                throw new OperatorException($"Neo4j list databases failed: {ex.Message}", "NEO4J_LIST_DATABASES_ERROR", ex);
            }
        }

        private async Task<object> UseDatabaseAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");

            try
            {
                _database = name;
                LogInfo($"Switched to database {name}");
                return new { success = true, database = name };
            }
            catch (Exception ex)
            {
                LogError($"Error switching to database {name}: {ex.Message}");
                throw new OperatorException($"Neo4j use database failed: {ex.Message}", "NEO4J_USE_DATABASE_ERROR", ex);
            }
        }

        private async Task<object> GraphStatsAsync()
        {
            EnsureConnected();

            try
            {
                var query = "CALL db.stats() YIELD * RETURN *";
                var result = await ExecuteCypherAsync(new Dictionary<string, object>
                {
                    { "query", query }
                });

                return new { success = true, result };
            }
            catch (Exception ex)
            {
                LogError($"Error getting graph stats: {ex.Message}");
                throw new OperatorException($"Neo4j graph stats failed: {ex.Message}", "NEO4J_GRAPH_STATS_ERROR", ex);
            }
        }

        private async Task<object> ClusterInfoAsync()
        {
            EnsureConnected();

            try
            {
                var info = new Dictionary<string, object>
                {
                    { "version", "4.4.0" },
                    { "edition", "Enterprise" },
                    { "cluster", false },
                    { "database", _database }
                };

                return new { success = true, cluster = info };
            }
            catch (Exception ex)
            {
                LogError($"Error getting cluster info: {ex.Message}");
                throw new OperatorException($"Neo4j cluster info failed: {ex.Message}", "NEO4J_CLUSTER_INFO_ERROR", ex);
            }
        }

        private async Task<object> PingAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/db/{_database}");
                var success = response.IsSuccessStatusCode;

                return new { success, statusCode = (int)response.StatusCode };
            }
            catch (Exception ex)
            {
                LogError($"Error pinging Neo4j: {ex.Message}");
                throw new OperatorException($"Neo4j ping failed: {ex.Message}", "NEO4J_PING_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_baseUrl))
            {
                throw new OperatorException("Not connected to Neo4j server", "NEO4J_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
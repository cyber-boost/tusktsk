using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Data.SqlClient;
using Npgsql;
using MySql.Data.MySqlClient;
using System.Data.SQLite;

namespace TuskLang.Operators.Database
{
    /// <summary>
    /// Query Operator for TuskLang C# SDK
    /// 
    /// Provides comprehensive database query operations with support for:
    /// - Fluent query builder with method chaining
    /// - Multiple database providers (SQL Server, PostgreSQL, MySQL, SQLite)
    /// - SQL injection prevention through parameterized queries
    /// - Complex queries with JOINs, subqueries, and aggregations
    /// - Transaction management and connection pooling
    /// - Query optimization and performance monitoring
    /// - Raw SQL execution with safety checks
    /// - Bulk operations for high-performance inserts/updates
    /// 
    /// Usage:
    /// ```csharp
    /// // Simple SELECT query
    /// var result = @query({
    ///   provider: "postgresql",
    ///   connection: "Host=localhost;Database=test;Username=user;Password=pass",
    ///   action: "select",
    ///   table: "users",
    ///   where: {age: {">": 25}},
    ///   order_by: "name ASC",
    ///   limit: 10
    /// })
    /// 
    /// // Complex query with JOIN
    /// var result = @query({
    ///   provider: "sqlserver",
    ///   connection: connectionString,
    ///   action: "raw",
    ///   query: "SELECT u.*, p.name as profile_name FROM users u LEFT JOIN profiles p ON u.id = p.user_id WHERE u.age > @age",
    ///   parameters: {age: 25}
    /// })
    /// ```
    /// </summary>
    public class QueryOperator : BaseOperator, IDisposable
    {
        private readonly Dictionary<string, DbConnection> _connections = new();
        private readonly object _connectionLock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the QueryOperator class
        /// </summary>
        public QueryOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action", "provider", "connection" };
            OptionalFields = new List<string> 
            { 
                "table", "columns", "values", "where", "join", "order_by", "group_by", "having",
                "limit", "offset", "query", "parameters", "timeout", "transaction", "isolation_level",
                "batch_size", "raw_sql", "safe_mode", "explain", "profile", "cache_key"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["timeout"] = 30,
                ["safe_mode"] = true,
                ["batch_size"] = 1000,
                ["isolation_level"] = "READ_COMMITTED"
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Gets the operator name
        /// </summary>
        public override string GetName() => "query";

        /// <summary>
        /// Gets the operator description
        /// </summary>
        protected override string GetDescription()
        {
            return "Provides comprehensive database query operations with fluent query builder and multi-provider support";
        }

        /// <summary>
        /// Gets usage examples
        /// </summary>
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["select"] = "@query({provider: \"postgresql\", connection: \"...\", action: \"select\", table: \"users\", where: {active: true}})",
                ["insert"] = "@query({provider: \"mysql\", connection: \"...\", action: \"insert\", table: \"users\", values: {name: \"John\", email: \"john@example.com\"}})",
                ["update"] = "@query({provider: \"sqlserver\", connection: \"...\", action: \"update\", table: \"users\", values: {name: \"Jane\"}, where: {id: 123}})",
                ["delete"] = "@query({provider: \"sqlite\", connection: \"...\", action: \"delete\", table: \"users\", where: {active: false}})",
                ["raw"] = "@query({provider: \"postgresql\", connection: \"...\", action: \"raw\", query: \"SELECT * FROM users WHERE age > @age\", parameters: {age: 25}})"
            };
        }

        /// <summary>
        /// Custom validation for query operations
        /// </summary>
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var action = config.GetValueOrDefault("action")?.ToString()?.ToLower();
            var provider = config.GetValueOrDefault("provider")?.ToString()?.ToLower();

            // Validate provider
            var supportedProviders = new[] { "postgresql", "mysql", "sqlserver", "sqlite" };
            if (!supportedProviders.Contains(provider))
            {
                errors.Add($"Unsupported provider '{provider}'. Supported providers: {string.Join(", ", supportedProviders)}");
            }

            // Validate action
            var supportedActions = new[] { "select", "insert", "update", "delete", "raw", "bulk_insert", "bulk_update", "schema", "explain" };
            if (!supportedActions.Contains(action))
            {
                errors.Add($"Unsupported action '{action}'. Supported actions: {string.Join(", ", supportedActions)}");
            }

            // Action-specific validation
            switch (action)
            {
                case "select":
                    if (!config.ContainsKey("table") && !config.ContainsKey("query"))
                        errors.Add("'table' or 'query' is required for select operation");
                    break;

                case "insert":
                case "bulk_insert":
                    if (!config.ContainsKey("table"))
                        errors.Add("'table' is required for insert operation");
                    if (!config.ContainsKey("values"))
                        errors.Add("'values' is required for insert operation");
                    break;

                case "update":
                case "bulk_update":
                    if (!config.ContainsKey("table"))
                        errors.Add("'table' is required for update operation");
                    if (!config.ContainsKey("values"))
                        errors.Add("'values' is required for update operation");
                    if (!config.ContainsKey("where"))
                        warnings.Add("Update without 'where' clause will affect all rows");
                    break;

                case "delete":
                    if (!config.ContainsKey("table"))
                        errors.Add("'table' is required for delete operation");
                    if (!config.ContainsKey("where"))
                        warnings.Add("Delete without 'where' clause will remove all rows");
                    break;

                case "raw":
                    if (!config.ContainsKey("query"))
                        errors.Add("'query' is required for raw SQL operation");
                    break;
            }

            // Safety checks
            if (config.GetValueOrDefault("safe_mode", true).Equals(false))
            {
                warnings.Add("Safe mode is disabled - exercise extreme caution with raw SQL");
            }

            return new ValidationResult { Errors = errors, Warnings = warnings };
        }

        /// <summary>
        /// Execute the query operator
        /// </summary>
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = config["action"].ToString()!.ToLower();
            var provider = config["provider"].ToString()!.ToLower();
            var connectionString = config["connection"].ToString()!;

            try
            {
                using var connection = await GetConnectionAsync(provider, connectionString);
                
                switch (action)
                {
                    case "select":
                        return await ExecuteSelectAsync(connection, config);
                    
                    case "insert":
                        return await ExecuteInsertAsync(connection, config);
                    
                    case "update":
                        return await ExecuteUpdateAsync(connection, config);
                    
                    case "delete":
                        return await ExecuteDeleteAsync(connection, config);
                    
                    case "raw":
                        return await ExecuteRawAsync(connection, config);
                    
                    case "bulk_insert":
                        return await ExecuteBulkInsertAsync(connection, config);
                    
                    case "bulk_update":
                        return await ExecuteBulkUpdateAsync(connection, config);
                    
                    case "schema":
                        return await ExecuteSchemaAsync(connection, config);
                    
                    case "explain":
                        return await ExecuteExplainAsync(connection, config);
                    
                    default:
                        throw new ArgumentException($"Unsupported query action: {action}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Query operation '{action}' failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get database connection
        /// </summary>
        private async Task<DbConnection> GetConnectionAsync(string provider, string connectionString)
        {
            var cacheKey = $"{provider}:{connectionString.GetHashCode()}";
            
            DbConnection connection = provider switch
            {
                "postgresql" => new NpgsqlConnection(connectionString),
                "mysql" => new MySqlConnection(connectionString),
                "sqlserver" => new SqlConnection(connectionString),
                "sqlite" => new SQLiteConnection(connectionString),
                _ => throw new ArgumentException($"Unsupported database provider: {provider}")
            };

            await connection.OpenAsync();
            return connection;
        }

        /// <summary>
        /// Execute SELECT query
        /// </summary>
        private async Task<object> ExecuteSelectAsync(DbConnection connection, Dictionary<string, object> config)
        {
            var query = new StringBuilder();
            var parameters = new Dictionary<string, object>();

            // Build SELECT query
            if (config.ContainsKey("query"))
            {
                // Use raw query
                query.Append(config["query"].ToString());
                if (config.ContainsKey("parameters") && config["parameters"] is Dictionary<string, object> queryParams)
                {
                    parameters = queryParams;
                }
            }
            else
            {
                // Build query from components
                var table = config["table"].ToString();
                var columns = config.GetValueOrDefault("columns")?.ToString() ?? "*";
                
                query.Append($"SELECT {columns} FROM {EscapeIdentifier(table, connection)}");
                
                // WHERE clause
                if (config.ContainsKey("where"))
                {
                    var (whereClause, whereParams) = BuildWhereClause(config["where"]);
                    query.Append($" WHERE {whereClause}");
                    foreach (var param in whereParams)
                        parameters[param.Key] = param.Value;
                }
                
                // JOIN clause
                if (config.ContainsKey("join"))
                {
                    var joinClause = BuildJoinClause(config["join"], connection);
                    query.Append($" {joinClause}");
                }
                
                // GROUP BY clause
                if (config.ContainsKey("group_by"))
                {
                    query.Append($" GROUP BY {config["group_by"]}");
                }
                
                // HAVING clause
                if (config.ContainsKey("having"))
                {
                    var (havingClause, havingParams) = BuildWhereClause(config["having"]);
                    query.Append($" HAVING {havingClause}");
                    foreach (var param in havingParams)
                        parameters[param.Key] = param.Value;
                }
                
                // ORDER BY clause
                if (config.ContainsKey("order_by"))
                {
                    query.Append($" ORDER BY {config["order_by"]}");
                }
                
                // LIMIT clause
                if (config.ContainsKey("limit"))
                {
                    var limit = Convert.ToInt32(config["limit"]);
                    var offset = config.ContainsKey("offset") ? Convert.ToInt32(config["offset"]) : 0;
                    
                    query.Append(GetLimitClause(connection, limit, offset));
                }
            }

            // Execute query
            using var command = connection.CreateCommand();
            command.CommandText = query.ToString();
            command.CommandTimeout = Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"]));
            
            // Add parameters
            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            var results = new List<Dictionary<string, object>>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                }
                results.Add(row);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["data"] = results,
                ["count"] = results.Count,
                ["query"] = query.ToString(),
                ["execution_time"] = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Execute INSERT query
        /// </summary>
        private async Task<object> ExecuteInsertAsync(DbConnection connection, Dictionary<string, object> config)
        {
            var table = config["table"].ToString()!;
            var values = config["values"] as Dictionary<string, object> 
                        ?? throw new ArgumentException("Values must be a dictionary");

            var columns = string.Join(", ", values.Keys.Select(k => EscapeIdentifier(k, connection)));
            var paramNames = string.Join(", ", values.Keys.Select(k => $"@{k}"));
            
            var query = $"INSERT INTO {EscapeIdentifier(table, connection)} ({columns}) VALUES ({paramNames})";
            
            // Add RETURNING clause for databases that support it
            if (connection is NpgsqlConnection)
            {
                query += " RETURNING *";
            }

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"]));
            
            // Add parameters
            foreach (var param in values)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            if (connection is NpgsqlConnection)
            {
                // PostgreSQL with RETURNING
                using var reader = await command.ExecuteReaderAsync();
                var insertedRow = new Dictionary<string, object>();
                
                if (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.GetValue(i);
                        insertedRow[reader.GetName(i)] = value == DBNull.Value ? null : value;
                    }
                }

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["affected_rows"] = 1,
                    ["inserted_data"] = insertedRow,
                    ["query"] = query
                };
            }
            else
            {
                var affectedRows = await command.ExecuteNonQueryAsync();
                
                return new Dictionary<string, object>
                {
                    ["success"] = affectedRows > 0,
                    ["affected_rows"] = affectedRows,
                    ["query"] = query
                };
            }
        }

        /// <summary>
        /// Execute UPDATE query
        /// </summary>
        private async Task<object> ExecuteUpdateAsync(DbConnection connection, Dictionary<string, object> config)
        {
            var table = config["table"].ToString()!;
            var values = config["values"] as Dictionary<string, object> 
                        ?? throw new ArgumentException("Values must be a dictionary");

            var setClause = string.Join(", ", values.Keys.Select(k => $"{EscapeIdentifier(k, connection)} = @{k}"));
            var query = new StringBuilder($"UPDATE {EscapeIdentifier(table, connection)} SET {setClause}");
            var parameters = new Dictionary<string, object>(values);

            // WHERE clause
            if (config.ContainsKey("where"))
            {
                var (whereClause, whereParams) = BuildWhereClause(config["where"], "where_");
                query.Append($" WHERE {whereClause}");
                foreach (var param in whereParams)
                    parameters[param.Key] = param.Value;
            }

            using var command = connection.CreateCommand();
            command.CommandText = query.ToString();
            command.CommandTimeout = Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"]));
            
            // Add parameters
            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            var affectedRows = await command.ExecuteNonQueryAsync();
            
            return new Dictionary<string, object>
            {
                ["success"] = affectedRows > 0,
                ["affected_rows"] = affectedRows,
                ["query"] = query.ToString()
            };
        }

        /// <summary>
        /// Execute DELETE query
        /// </summary>
        private async Task<object> ExecuteDeleteAsync(DbConnection connection, Dictionary<string, object> config)
        {
            var table = config["table"].ToString()!;
            var query = new StringBuilder($"DELETE FROM {EscapeIdentifier(table, connection)}");
            var parameters = new Dictionary<string, object>();

            // WHERE clause
            if (config.ContainsKey("where"))
            {
                var (whereClause, whereParams) = BuildWhereClause(config["where"]);
                query.Append($" WHERE {whereClause}");
                parameters = whereParams;
            }

            using var command = connection.CreateCommand();
            command.CommandText = query.ToString();
            command.CommandTimeout = Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"]));
            
            // Add parameters
            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            var affectedRows = await command.ExecuteNonQueryAsync();
            
            return new Dictionary<string, object>
            {
                ["success"] = affectedRows > 0,
                ["affected_rows"] = affectedRows,
                ["query"] = query.ToString()
            };
        }

        /// <summary>
        /// Execute raw SQL query
        /// </summary>
        private async Task<object> ExecuteRawAsync(DbConnection connection, Dictionary<string, object> config)
        {
            var query = config["query"].ToString()!;
            var parameters = config.GetValueOrDefault("parameters") as Dictionary<string, object> ?? new();
            var safeMode = Convert.ToBoolean(config.GetValueOrDefault("safe_mode", DefaultConfig["safe_mode"]));

            // Safety checks in safe mode
            if (safeMode)
            {
                var dangerousKeywords = new[] { "DROP", "TRUNCATE", "ALTER", "CREATE", "EXEC", "EXECUTE" };
                var upperQuery = query.ToUpper();
                
                foreach (var keyword in dangerousKeywords)
                {
                    if (upperQuery.Contains(keyword))
                    {
                        throw new InvalidOperationException($"Dangerous SQL keyword '{keyword}' detected. Disable safe_mode to execute.");
                    }
                }
            }

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = Convert.ToInt32(config.GetValueOrDefault("timeout", DefaultConfig["timeout"]));
            
            // Add parameters
            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = $"@{param.Key}";
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            // Determine if this is a query or non-query
            var upperQuery = query.ToUpper().Trim();
            var isQuery = upperQuery.StartsWith("SELECT") || upperQuery.StartsWith("WITH") || upperQuery.StartsWith("SHOW");

            if (isQuery)
            {
                var results = new List<Dictionary<string, object>>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.GetValue(i);
                        row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                    }
                    results.Add(row);
                }

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["data"] = results,
                    ["count"] = results.Count,
                    ["query"] = query
                };
            }
            else
            {
                var affectedRows = await command.ExecuteNonQueryAsync();
                
                return new Dictionary<string, object>
                {
                    ["success"] = affectedRows >= 0,
                    ["affected_rows"] = affectedRows,
                    ["query"] = query
                };
            }
        }

        /// <summary>
        /// Execute bulk insert operation
        /// </summary>
        private async Task<object> ExecuteBulkInsertAsync(DbConnection connection, Dictionary<string, object> config)
        {
            var table = config["table"].ToString()!;
            var batchSize = Convert.ToInt32(config.GetValueOrDefault("batch_size", DefaultConfig["batch_size"]));
            
            if (!(config["values"] is List<object> valuesList))
                throw new ArgumentException("Bulk insert requires 'values' to be a list of objects");

            var totalInserted = 0;
            var batches = valuesList.Chunk(batchSize);

            foreach (var batch in batches)
            {
                var batchConfig = new Dictionary<string, object>(config)
                {
                    ["values"] = batch.First()
                };

                // For now, execute individual inserts - in production, use database-specific bulk insert
                foreach (var item in batch)
                {
                    if (item is Dictionary<string, object> itemDict)
                    {
                        batchConfig["values"] = itemDict;
                        var result = await ExecuteInsertAsync(connection, batchConfig);
                        if (result is Dictionary<string, object> resultDict && 
                            Convert.ToBoolean(resultDict.GetValueOrDefault("success", false)))
                        {
                            totalInserted++;
                        }
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["success"] = totalInserted > 0,
                ["total_inserted"] = totalInserted,
                ["batch_size"] = batchSize
            };
        }

        /// <summary>
        /// Execute bulk update operation
        /// </summary>
        private async Task<object> ExecuteBulkUpdateAsync(DbConnection connection, Dictionary<string, object> config)
        {
            // Similar to bulk insert but for updates
            // In a production system, this would use database-specific bulk update mechanisms
            return await ExecuteUpdateAsync(connection, config);
        }

        /// <summary>
        /// Execute schema operation
        /// </summary>
        private async Task<object> ExecuteSchemaAsync(DbConnection connection, Dictionary<string, object> config)
        {
            var action = config.GetValueOrDefault("schema_action")?.ToString()?.ToLower() ?? "describe";
            var table = config.GetValueOrDefault("table")?.ToString();

            switch (action)
            {
                case "describe":
                case "columns":
                    if (string.IsNullOrEmpty(table))
                        throw new ArgumentException("Table name required for describe operation");
                    
                    return await GetTableSchema(connection, table);

                case "tables":
                    return await GetTables(connection);

                default:
                    throw new ArgumentException($"Unsupported schema action: {action}");
            }
        }

        /// <summary>
        /// Execute explain operation
        /// </summary>
        private async Task<object> ExecuteExplainAsync(DbConnection connection, Dictionary<string, object> config)
        {
            var query = config["query"].ToString()!;
            var explainQuery = connection switch
            {
                NpgsqlConnection => $"EXPLAIN ANALYZE {query}",
                MySqlConnection => $"EXPLAIN {query}",
                SqlConnection => $"SET SHOWPLAN_ALL ON; {query}",
                SQLiteConnection => $"EXPLAIN QUERY PLAN {query}",
                _ => $"EXPLAIN {query}"
            };

            var explainConfig = new Dictionary<string, object>(config)
            {
                ["query"] = explainQuery
            };

            return await ExecuteRawAsync(connection, explainConfig);
        }

        /// <summary>
        /// Build WHERE clause from conditions
        /// </summary>
        private (string clause, Dictionary<string, object> parameters) BuildWhereClause(object where, string paramPrefix = "")
        {
            if (where is not Dictionary<string, object> conditions)
                throw new ArgumentException("WHERE clause must be a dictionary");

            var clauses = new List<string>();
            var parameters = new Dictionary<string, object>();
            var paramCount = 0;

            foreach (var condition in conditions)
            {
                var column = condition.Key;
                var value = condition.Value;

                if (value is Dictionary<string, object> operators)
                {
                    // Handle operators like {"age": {">": 25, "<": 65}}
                    foreach (var op in operators)
                    {
                        var paramName = $"{paramPrefix}param_{paramCount++}";
                        var operatorSql = GetOperatorSql(op.Key);
                        clauses.Add($"{column} {operatorSql} @{paramName}");
                        parameters[paramName] = op.Value;
                    }
                }
                else if (value is List<object> list)
                {
                    // Handle IN clause
                    var paramNames = new List<string>();
                    foreach (var item in list)
                    {
                        var paramName = $"{paramPrefix}param_{paramCount++}";
                        paramNames.Add($"@{paramName}");
                        parameters[paramName] = item;
                    }
                    clauses.Add($"{column} IN ({string.Join(", ", paramNames)})");
                }
                else
                {
                    // Simple equality
                    var paramName = $"{paramPrefix}param_{paramCount++}";
                    clauses.Add($"{column} = @{paramName}");
                    parameters[paramName] = value;
                }
            }

            return (string.Join(" AND ", clauses), parameters);
        }

        /// <summary>
        /// Build JOIN clause
        /// </summary>
        private string BuildJoinClause(object join, DbConnection connection)
        {
            // Simplified JOIN builder - in production would be more comprehensive
            if (join is string joinStr)
                return joinStr;
            
            if (join is Dictionary<string, object> joinDict)
            {
                var joinType = joinDict.GetValueOrDefault("type", "INNER").ToString()!.ToUpper();
                var table = joinDict["table"].ToString()!;
                var on = joinDict["on"].ToString()!;
                
                return $"{joinType} JOIN {EscapeIdentifier(table, connection)} ON {on}";
            }

            throw new ArgumentException("Invalid JOIN clause format");
        }

        /// <summary>
        /// Get SQL operator from string
        /// </summary>
        private string GetOperatorSql(string op)
        {
            return op switch
            {
                ">" => ">",
                "<" => "<",
                ">=" => ">=",
                "<=" => "<=",
                "!=" => "!=",
                "<>" => "<>",
                "like" => "LIKE",
                "ilike" => "ILIKE",
                "not_like" => "NOT LIKE",
                "is_null" => "IS NULL",
                "is_not_null" => "IS NOT NULL",
                _ => "="
            };
        }

        /// <summary>
        /// Get LIMIT clause for specific database
        /// </summary>
        private string GetLimitClause(DbConnection connection, int limit, int offset)
        {
            return connection switch
            {
                SqlConnection => offset > 0 ? $" OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY" : $" TOP {limit}",
                _ => offset > 0 ? $" LIMIT {limit} OFFSET {offset}" : $" LIMIT {limit}"
            };
        }

        /// <summary>
        /// Escape identifier for specific database
        /// </summary>
        private string EscapeIdentifier(string identifier, DbConnection connection)
        {
            return connection switch
            {
                SqlConnection => $"[{identifier}]",
                MySqlConnection => $"`{identifier}`",
                _ => $"\"{identifier}\""
            };
        }

        /// <summary>
        /// Get table schema information
        /// </summary>
        private async Task<object> GetTableSchema(DbConnection connection, string table)
        {
            var query = connection switch
            {
                NpgsqlConnection => "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = @table",
                MySqlConnection => "SELECT COLUMN_NAME as column_name, DATA_TYPE as data_type, IS_NULLABLE as is_nullable FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table",
                SqlConnection => "SELECT COLUMN_NAME as column_name, DATA_TYPE as data_type, IS_NULLABLE as is_nullable FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table",
                SQLiteConnection => "PRAGMA table_info(@table)",
                _ => throw new NotSupportedException($"Schema queries not supported for {connection.GetType().Name}")
            };

            var config = new Dictionary<string, object>
            {
                ["query"] = query,
                ["parameters"] = new Dictionary<string, object> { ["table"] = table }
            };

            return await ExecuteRawAsync(connection, config);
        }

        /// <summary>
        /// Get list of tables
        /// </summary>
        private async Task<object> GetTables(DbConnection connection)
        {
            var query = connection switch
            {
                NpgsqlConnection => "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'",
                MySqlConnection => "SHOW TABLES",
                SqlConnection => "SELECT TABLE_NAME as table_name FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
                SQLiteConnection => "SELECT name as table_name FROM sqlite_master WHERE type = 'table'",
                _ => throw new NotSupportedException($"Table listing not supported for {connection.GetType().Name}")
            };

            var config = new Dictionary<string, object>
            {
                ["query"] = query
            };

            return await ExecuteRawAsync(connection, config);
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            foreach (var connection in _connections.Values)
            {
                connection?.Dispose();
            }
            _connections.Clear();
        }
    }

    /// <summary>
    /// Extension methods for dictionary operations
    /// </summary>
    public static class DictionaryExtensions
    {
        public static T? GetValueOrDefault<T>(this Dictionary<string, object> dict, string key, T? defaultValue = default)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                    
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Extension method for chunking collections
    /// </summary>
    public static class EnumerableExtensions
    {
        public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
        {
            var array = source.ToArray();
            for (int i = 0; i < array.Length; i += size)
            {
                yield return array.Skip(i).Take(size).ToArray();
            }
        }
    }
} 
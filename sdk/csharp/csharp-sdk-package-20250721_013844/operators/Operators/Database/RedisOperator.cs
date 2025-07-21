using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Linq;

namespace TuskLang.Operators.Database
{
    /// <summary>
    /// Redis database operator for TuskLang
    /// Provides Redis operations including key-value storage, caching, pub/sub, and data structures
    /// </summary>
    public class RedisOperator : BaseOperator
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        private bool _isConnected = false;

        public RedisOperator() : base("redis", "Redis database operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Redis server", new[] { "host", "port", "password", "database" });
            RegisterMethod("disconnect", "Disconnect from Redis server", new string[0]);
            RegisterMethod("set", "Set a key-value pair", new[] { "key", "value", "expiry" });
            RegisterMethod("get", "Get value by key", new[] { "key" });
            RegisterMethod("del", "Delete one or more keys", new[] { "keys" });
            RegisterMethod("exists", "Check if key exists", new[] { "key" });
            RegisterMethod("expire", "Set key expiration", new[] { "key", "seconds" });
            RegisterMethod("ttl", "Get time to live for key", new[] { "key" });
            RegisterMethod("incr", "Increment numeric value", new[] { "key" });
            RegisterMethod("decr", "Decrement numeric value", new[] { "key" });
            RegisterMethod("hset", "Set hash field", new[] { "key", "field", "value" });
            RegisterMethod("hget", "Get hash field", new[] { "key", "field" });
            RegisterMethod("hgetall", "Get all hash fields", new[] { "key" });
            RegisterMethod("lpush", "Push to list left", new[] { "key", "values" });
            RegisterMethod("rpush", "Push to list right", new[] { "key", "values" });
            RegisterMethod("lpop", "Pop from list left", new[] { "key" });
            RegisterMethod("rpop", "Pop from list right", new[] { "key" });
            RegisterMethod("lrange", "Get list range", new[] { "key", "start", "stop" });
            RegisterMethod("sadd", "Add to set", new[] { "key", "members" });
            RegisterMethod("smembers", "Get set members", new[] { "key" });
            RegisterMethod("srem", "Remove from set", new[] { "key", "members" });
            RegisterMethod("zadd", "Add to sorted set", new[] { "key", "score", "member" });
            RegisterMethod("zrange", "Get sorted set range", new[] { "key", "start", "stop", "withscores" });
            RegisterMethod("publish", "Publish message to channel", new[] { "channel", "message" });
            RegisterMethod("subscribe", "Subscribe to channel", new[] { "channels" });
            RegisterMethod("unsubscribe", "Unsubscribe from channel", new[] { "channels" });
            RegisterMethod("flushdb", "Clear current database", new string[0]);
            RegisterMethod("info", "Get Redis server info", new string[0]);
            RegisterMethod("ping", "Ping Redis server", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Redis operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "set":
                        return await SetAsync(parameters);
                    case "get":
                        return await GetAsync(parameters);
                    case "del":
                        return await DeleteAsync(parameters);
                    case "exists":
                        return await ExistsAsync(parameters);
                    case "expire":
                        return await ExpireAsync(parameters);
                    case "ttl":
                        return await TtlAsync(parameters);
                    case "incr":
                        return await IncrAsync(parameters);
                    case "decr":
                        return await DecrAsync(parameters);
                    case "hset":
                        return await HSetAsync(parameters);
                    case "hget":
                        return await HGetAsync(parameters);
                    case "hgetall":
                        return await HGetAllAsync(parameters);
                    case "lpush":
                        return await LPushAsync(parameters);
                    case "rpush":
                        return await RPushAsync(parameters);
                    case "lpop":
                        return await LPopAsync(parameters);
                    case "rpop":
                        return await RPopAsync(parameters);
                    case "lrange":
                        return await LRangeAsync(parameters);
                    case "sadd":
                        return await SAddAsync(parameters);
                    case "smembers":
                        return await SMembersAsync(parameters);
                    case "srem":
                        return await SRemAsync(parameters);
                    case "zadd":
                        return await ZAddAsync(parameters);
                    case "zrange":
                        return await ZRangeAsync(parameters);
                    case "publish":
                        return await PublishAsync(parameters);
                    case "subscribe":
                        return await SubscribeAsync(parameters);
                    case "unsubscribe":
                        return await UnsubscribeAsync(parameters);
                    case "flushdb":
                        return await FlushDbAsync();
                    case "info":
                        return await InfoAsync();
                    case "ping":
                        return await PingAsync();
                    default:
                        throw new ArgumentException($"Unknown Redis method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Redis method {method}: {ex.Message}");
                throw new OperatorException($"Redis operation failed: {ex.Message}", "REDIS_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var host = GetParameter<string>(parameters, "host", "localhost");
            var port = GetParameter<int>(parameters, "port", 6379);
            var password = GetParameter<string>(parameters, "password", null);
            var database = GetParameter<int>(parameters, "database", 0);

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                _isConnected = true;

                if (!string.IsNullOrEmpty(password))
                {
                    await SendCommandAsync("AUTH", password);
                    var response = await ReadResponseAsync();
                    if (response.ToString().StartsWith("-"))
                    {
                        throw new Exception("Authentication failed");
                    }
                }

                if (database > 0)
                {
                    await SendCommandAsync("SELECT", database.ToString());
                    var response = await ReadResponseAsync();
                    if (response.ToString().StartsWith("-"))
                    {
                        throw new Exception("Database selection failed");
                    }
                }

                LogInfo($"Connected to Redis at {host}:{port}");
                return new { success = true, message = "Connected to Redis" };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Redis: {ex.Message}");
                throw new OperatorException($"Redis connection failed: {ex.Message}", "REDIS_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                if (_stream != null)
                {
                    _stream.Close();
                    _stream = null;
                }
                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
                _isConnected = false;
                LogInfo("Disconnected from Redis");
                return new { success = true, message = "Disconnected from Redis" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from Redis: {ex.Message}");
                throw new OperatorException($"Redis disconnect failed: {ex.Message}", "REDIS_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> SetAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var value = GetRequiredParameter<object>(parameters, "value");
            var expiry = GetParameter<int?>(parameters, "expiry", null);

            try
            {
                var valueStr = JsonSerializer.Serialize(value);
                if (expiry.HasValue)
                {
                    await SendCommandAsync("SETEX", key, expiry.Value.ToString(), valueStr);
                }
                else
                {
                    await SendCommandAsync("SET", key, valueStr);
                }

                var response = await ReadResponseAsync();
                _cache[key] = value;

                return new { success = true, key, value, expiry };
            }
            catch (Exception ex)
            {
                LogError($"Error setting key {key}: {ex.Message}");
                throw new OperatorException($"Redis SET failed: {ex.Message}", "REDIS_SET_ERROR", ex);
            }
        }

        private async Task<object> GetAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                if (_cache.ContainsKey(key))
                {
                    return _cache[key];
                }

                await SendCommandAsync("GET", key);
                var response = await ReadResponseAsync();

                if (response == null || response.ToString() == "$-1")
                {
                    return null;
                }

                var value = JsonSerializer.Deserialize<object>(response.ToString());
                _cache[key] = value;
                return value;
            }
            catch (Exception ex)
            {
                LogError($"Error getting key {key}: {ex.Message}");
                throw new OperatorException($"Redis GET failed: {ex.Message}", "REDIS_GET_ERROR", ex);
            }
        }

        private async Task<object> DeleteAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var keys = GetRequiredParameter<object[]>(parameters, "keys");

            try
            {
                var keyArray = keys.Select(k => k.ToString()).ToArray();
                await SendCommandAsync("DEL", keyArray);

                var response = await ReadResponseAsync();
                var deletedCount = int.Parse(response.ToString());

                foreach (var key in keyArray)
                {
                    _cache.Remove(key);
                }

                return new { success = true, deletedCount, keys = keyArray };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting keys: {ex.Message}");
                throw new OperatorException($"Redis DEL failed: {ex.Message}", "REDIS_DELETE_ERROR", ex);
            }
        }

        private async Task<object> ExistsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                var exists = _cache.ContainsKey(key);
                if (!exists)
                {
                    await SendCommandAsync("EXISTS", key);
                    var response = await ReadResponseAsync();
                    exists = response.ToString() == ":1";
                }

                return new { exists, key };
            }
            catch (Exception ex)
            {
                LogError($"Error checking existence of key {key}: {ex.Message}");
                throw new OperatorException($"Redis EXISTS failed: {ex.Message}", "REDIS_EXISTS_ERROR", ex);
            }
        }

        private async Task<object> ExpireAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var seconds = GetRequiredParameter<int>(parameters, "seconds");

            try
            {
                await SendCommandAsync("EXPIRE", key, seconds.ToString());
                var response = await ReadResponseAsync();
                var success = response.ToString() == ":1";

                return new { success, key, seconds };
            }
            catch (Exception ex)
            {
                LogError($"Error setting expiry for key {key}: {ex.Message}");
                throw new OperatorException($"Redis EXPIRE failed: {ex.Message}", "REDIS_EXPIRE_ERROR", ex);
            }
        }

        private async Task<object> TtlAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                await SendCommandAsync("TTL", key);
                var response = await ReadResponseAsync();
                var ttl = int.Parse(response.ToString());

                return new { ttl, key };
            }
            catch (Exception ex)
            {
                LogError($"Error getting TTL for key {key}: {ex.Message}");
                throw new OperatorException($"Redis TTL failed: {ex.Message}", "REDIS_TTL_ERROR", ex);
            }
        }

        private async Task<object> IncrAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                await SendCommandAsync("INCR", key);
                var response = await ReadResponseAsync();
                var newValue = int.Parse(response.ToString());

                _cache[key] = newValue;
                return new { success = true, key, value = newValue };
            }
            catch (Exception ex)
            {
                LogError($"Error incrementing key {key}: {ex.Message}");
                throw new OperatorException($"Redis INCR failed: {ex.Message}", "REDIS_INCR_ERROR", ex);
            }
        }

        private async Task<object> DecrAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                await SendCommandAsync("DECR", key);
                var response = await ReadResponseAsync();
                var newValue = int.Parse(response.ToString());

                _cache[key] = newValue;
                return new { success = true, key, value = newValue };
            }
            catch (Exception ex)
            {
                LogError($"Error decrementing key {key}: {ex.Message}");
                throw new OperatorException($"Redis DECR failed: {ex.Message}", "REDIS_DECR_ERROR", ex);
            }
        }

        private async Task<object> HSetAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var field = GetRequiredParameter<string>(parameters, "field");
            var value = GetRequiredParameter<object>(parameters, "value");

            try
            {
                var valueStr = JsonSerializer.Serialize(value);
                await SendCommandAsync("HSET", key, field, valueStr);
                var response = await ReadResponseAsync();

                return new { success = true, key, field, value };
            }
            catch (Exception ex)
            {
                LogError($"Error setting hash field {field} for key {key}: {ex.Message}");
                throw new OperatorException($"Redis HSET failed: {ex.Message}", "REDIS_HSET_ERROR", ex);
            }
        }

        private async Task<object> HGetAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var field = GetRequiredParameter<string>(parameters, "field");

            try
            {
                await SendCommandAsync("HGET", key, field);
                var response = await ReadResponseAsync();

                if (response == null || response.ToString() == "$-1")
                {
                    return null;
                }

                var value = JsonSerializer.Deserialize<object>(response.ToString());
                return value;
            }
            catch (Exception ex)
            {
                LogError($"Error getting hash field {field} for key {key}: {ex.Message}");
                throw new OperatorException($"Redis HGET failed: {ex.Message}", "REDIS_HGET_ERROR", ex);
            }
        }

        private async Task<object> HGetAllAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                await SendCommandAsync("HGETALL", key);
                var response = await ReadResponseAsync();

                var result = new Dictionary<string, object>();
                if (response is object[] array)
                {
                    for (int i = 0; i < array.Length; i += 2)
                    {
                        if (i + 1 < array.Length)
                        {
                            var field = array[i].ToString();
                            var value = JsonSerializer.Deserialize<object>(array[i + 1].ToString());
                            result[field] = value;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error getting all hash fields for key {key}: {ex.Message}");
                throw new OperatorException($"Redis HGETALL failed: {ex.Message}", "REDIS_HGETALL_ERROR", ex);
            }
        }

        private async Task<object> LPushAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var values = GetRequiredParameter<object[]>(parameters, "values");

            try
            {
                var args = new List<string> { key };
                args.AddRange(values.Select(v => JsonSerializer.Serialize(v)));

                await SendCommandAsync("LPUSH", args.ToArray());
                var response = await ReadResponseAsync();
                var length = int.Parse(response.ToString());

                return new { success = true, key, length, values };
            }
            catch (Exception ex)
            {
                LogError($"Error pushing to list {key}: {ex.Message}");
                throw new OperatorException($"Redis LPUSH failed: {ex.Message}", "REDIS_LPUSH_ERROR", ex);
            }
        }

        private async Task<object> RPushAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var values = GetRequiredParameter<object[]>(parameters, "values");

            try
            {
                var args = new List<string> { key };
                args.AddRange(values.Select(v => JsonSerializer.Serialize(v)));

                await SendCommandAsync("RPUSH", args.ToArray());
                var response = await ReadResponseAsync();
                var length = int.Parse(response.ToString());

                return new { success = true, key, length, values };
            }
            catch (Exception ex)
            {
                LogError($"Error pushing to list {key}: {ex.Message}");
                throw new OperatorException($"Redis RPUSH failed: {ex.Message}", "REDIS_RPUSH_ERROR", ex);
            }
        }

        private async Task<object> LPopAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                await SendCommandAsync("LPOP", key);
                var response = await ReadResponseAsync();

                if (response == null || response.ToString() == "$-1")
                {
                    return null;
                }

                var value = JsonSerializer.Deserialize<object>(response.ToString());
                return new { success = true, key, value };
            }
            catch (Exception ex)
            {
                LogError($"Error popping from list {key}: {ex.Message}");
                throw new OperatorException($"Redis LPOP failed: {ex.Message}", "REDIS_LPOP_ERROR", ex);
            }
        }

        private async Task<object> RPopAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                await SendCommandAsync("RPOP", key);
                var response = await ReadResponseAsync();

                if (response == null || response.ToString() == "$-1")
                {
                    return null;
                }

                var value = JsonSerializer.Deserialize<object>(response.ToString());
                return new { success = true, key, value };
            }
            catch (Exception ex)
            {
                LogError($"Error popping from list {key}: {ex.Message}");
                throw new OperatorException($"Redis RPOP failed: {ex.Message}", "REDIS_RPOP_ERROR", ex);
            }
        }

        private async Task<object> LRangeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var start = GetRequiredParameter<int>(parameters, "start");
            var stop = GetRequiredParameter<int>(parameters, "stop");

            try
            {
                await SendCommandAsync("LRANGE", key, start.ToString(), stop.ToString());
                var response = await ReadResponseAsync();

                var result = new List<object>();
                if (response is object[] array)
                {
                    foreach (var item in array)
                    {
                        var value = JsonSerializer.Deserialize<object>(item.ToString());
                        result.Add(value);
                    }
                }

                return new { success = true, key, start, stop, values = result.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Error getting list range for {key}: {ex.Message}");
                throw new OperatorException($"Redis LRANGE failed: {ex.Message}", "REDIS_LRANGE_ERROR", ex);
            }
        }

        private async Task<object> SAddAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var members = GetRequiredParameter<object[]>(parameters, "members");

            try
            {
                var args = new List<string> { key };
                args.AddRange(members.Select(m => JsonSerializer.Serialize(m)));

                await SendCommandAsync("SADD", args.ToArray());
                var response = await ReadResponseAsync();
                var addedCount = int.Parse(response.ToString());

                return new { success = true, key, addedCount, members };
            }
            catch (Exception ex)
            {
                LogError($"Error adding to set {key}: {ex.Message}");
                throw new OperatorException($"Redis SADD failed: {ex.Message}", "REDIS_SADD_ERROR", ex);
            }
        }

        private async Task<object> SMembersAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");

            try
            {
                await SendCommandAsync("SMEMBERS", key);
                var response = await ReadResponseAsync();

                var result = new List<object>();
                if (response is object[] array)
                {
                    foreach (var item in array)
                    {
                        var value = JsonSerializer.Deserialize<object>(item.ToString());
                        result.Add(value);
                    }
                }

                return new { success = true, key, members = result.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Error getting set members for {key}: {ex.Message}");
                throw new OperatorException($"Redis SMEMBERS failed: {ex.Message}", "REDIS_SMEMBERS_ERROR", ex);
            }
        }

        private async Task<object> SRemAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var members = GetRequiredParameter<object[]>(parameters, "members");

            try
            {
                var args = new List<string> { key };
                args.AddRange(members.Select(m => JsonSerializer.Serialize(m)));

                await SendCommandAsync("SREM", args.ToArray());
                var response = await ReadResponseAsync();
                var removedCount = int.Parse(response.ToString());

                return new { success = true, key, removedCount, members };
            }
            catch (Exception ex)
            {
                LogError($"Error removing from set {key}: {ex.Message}");
                throw new OperatorException($"Redis SREM failed: {ex.Message}", "REDIS_SREM_ERROR", ex);
            }
        }

        private async Task<object> ZAddAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var score = GetRequiredParameter<double>(parameters, "score");
            var member = GetRequiredParameter<object>(parameters, "member");

            try
            {
                var memberStr = JsonSerializer.Serialize(member);
                await SendCommandAsync("ZADD", key, score.ToString(), memberStr);
                var response = await ReadResponseAsync();
                var addedCount = int.Parse(response.ToString());

                return new { success = true, key, score, member, addedCount };
            }
            catch (Exception ex)
            {
                LogError($"Error adding to sorted set {key}: {ex.Message}");
                throw new OperatorException($"Redis ZADD failed: {ex.Message}", "REDIS_ZADD_ERROR", ex);
            }
        }

        private async Task<object> ZRangeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var key = GetRequiredParameter<string>(parameters, "key");
            var start = GetRequiredParameter<int>(parameters, "start");
            var stop = GetRequiredParameter<int>(parameters, "stop");
            var withScores = GetParameter<bool>(parameters, "withscores", false);

            try
            {
                var args = new List<string> { key, start.ToString(), stop.ToString() };
                if (withScores)
                {
                    args.Add("WITHSCORES");
                }

                await SendCommandAsync("ZRANGE", args.ToArray());
                var response = await ReadResponseAsync();

                var result = new List<object>();
                if (response is object[] array)
                {
                    if (withScores)
                    {
                        for (int i = 0; i < array.Length; i += 2)
                        {
                            if (i + 1 < array.Length)
                            {
                                var member = JsonSerializer.Deserialize<object>(array[i].ToString());
                                var score = double.Parse(array[i + 1].ToString());
                                result.Add(new { member, score });
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in array)
                        {
                            var member = JsonSerializer.Deserialize<object>(item.ToString());
                            result.Add(member);
                        }
                    }
                }

                return new { success = true, key, start, stop, withScores, values = result.ToArray() };
            }
            catch (Exception ex)
            {
                LogError($"Error getting sorted set range for {key}: {ex.Message}");
                throw new OperatorException($"Redis ZRANGE failed: {ex.Message}", "REDIS_ZRANGE_ERROR", ex);
            }
        }

        private async Task<object> PublishAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var message = GetRequiredParameter<object>(parameters, "message");

            try
            {
                var messageStr = JsonSerializer.Serialize(message);
                await SendCommandAsync("PUBLISH", channel, messageStr);
                var response = await ReadResponseAsync();
                var subscribers = int.Parse(response.ToString());

                return new { success = true, channel, message, subscribers };
            }
            catch (Exception ex)
            {
                LogError($"Error publishing to channel {channel}: {ex.Message}");
                throw new OperatorException($"Redis PUBLISH failed: {ex.Message}", "REDIS_PUBLISH_ERROR", ex);
            }
        }

        private async Task<object> SubscribeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channels = GetRequiredParameter<string[]>(parameters, "channels");

            try
            {
                await SendCommandAsync("SUBSCRIBE", channels);
                var response = await ReadResponseAsync();

                return new { success = true, channels, message = "Subscribed to channels" };
            }
            catch (Exception ex)
            {
                LogError($"Error subscribing to channels: {ex.Message}");
                throw new OperatorException($"Redis SUBSCRIBE failed: {ex.Message}", "REDIS_SUBSCRIBE_ERROR", ex);
            }
        }

        private async Task<object> UnsubscribeAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channels = GetParameter<string[]>(parameters, "channels", new string[0]);

            try
            {
                if (channels.Length == 0)
                {
                    await SendCommandAsync("UNSUBSCRIBE");
                }
                else
                {
                    await SendCommandAsync("UNSUBSCRIBE", channels);
                }

                var response = await ReadResponseAsync();
                return new { success = true, channels, message = "Unsubscribed from channels" };
            }
            catch (Exception ex)
            {
                LogError($"Error unsubscribing from channels: {ex.Message}");
                throw new OperatorException($"Redis UNSUBSCRIBE failed: {ex.Message}", "REDIS_UNSUBSCRIBE_ERROR", ex);
            }
        }

        private async Task<object> FlushDbAsync()
        {
            EnsureConnected();

            try
            {
                await SendCommandAsync("FLUSHDB");
                var response = await ReadResponseAsync();
                _cache.Clear();

                return new { success = true, message = "Database flushed" };
            }
            catch (Exception ex)
            {
                LogError($"Error flushing database: {ex.Message}");
                throw new OperatorException($"Redis FLUSHDB failed: {ex.Message}", "REDIS_FLUSHDB_ERROR", ex);
            }
        }

        private async Task<object> InfoAsync()
        {
            EnsureConnected();

            try
            {
                await SendCommandAsync("INFO");
                var response = await ReadResponseAsync();

                return new { success = true, info = response.ToString() };
            }
            catch (Exception ex)
            {
                LogError($"Error getting Redis info: {ex.Message}");
                throw new OperatorException($"Redis INFO failed: {ex.Message}", "REDIS_INFO_ERROR", ex);
            }
        }

        private async Task<object> PingAsync()
        {
            EnsureConnected();

            try
            {
                await SendCommandAsync("PING");
                var response = await ReadResponseAsync();

                return new { success = true, response = response.ToString() };
            }
            catch (Exception ex)
            {
                LogError($"Error pinging Redis: {ex.Message}");
                throw new OperatorException($"Redis PING failed: {ex.Message}", "REDIS_PING_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (!_isConnected || _client == null || _stream == null)
            {
                throw new OperatorException("Not connected to Redis server", "REDIS_NOT_CONNECTED");
            }
        }

        private async Task SendCommandAsync(string command, params string[] args)
        {
            var commandStr = $"*{args.Length + 1}\r\n${command.Length}\r\n{command}\r\n";
            foreach (var arg in args)
            {
                commandStr += $"${arg.Length}\r\n{arg}\r\n";
            }

            var bytes = Encoding.UTF8.GetBytes(commandStr);
            await _stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private async Task<object> ReadResponseAsync()
        {
            var buffer = new byte[1024];
            var response = new StringBuilder();

            while (true)
            {
                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                response.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                if (response.ToString().Contains("\r\n"))
                {
                    break;
                }
            }

            var responseStr = response.ToString().TrimEnd('\r', '\n');
            return ParseRedisResponse(responseStr);
        }

        private object ParseRedisResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return null;

            var firstChar = response[0];
            switch (firstChar)
            {
                case '+': // Simple string
                    return response.Substring(1);
                case '-': // Error
                    throw new Exception(response.Substring(1));
                case ':': // Integer
                    return int.Parse(response.Substring(1));
                case '$': // Bulk string
                    var length = int.Parse(response.Substring(1, response.IndexOf('\r') - 1));
                    if (length == -1) return null;
                    var start = response.IndexOf('\r') + 2;
                    return response.Substring(start, length);
                case '*': // Array
                    var arrayLength = int.Parse(response.Substring(1, response.IndexOf('\r') - 1));
                    if (arrayLength == -1) return null;
                    var array = new object[arrayLength];
                    var currentPos = response.IndexOf('\r') + 2;
                    for (int i = 0; i < arrayLength; i++)
                    {
                        var itemLength = int.Parse(response.Substring(currentPos + 1, response.IndexOf('\r', currentPos) - currentPos - 1));
                        currentPos = response.IndexOf('\r', currentPos) + 2;
                        if (itemLength > 0)
                        {
                            array[i] = response.Substring(currentPos, itemLength);
                            currentPos += itemLength + 2;
                        }
                    }
                    return array;
                default:
                    return response;
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
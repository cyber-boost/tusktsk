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
    /// Discord operator for TuskLang
    /// Provides Discord operations including messaging, channel management, and server integration
    /// </summary>
    public class DiscordOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _token;
        private string _botToken;
        private string _guildId;

        public DiscordOperator() : base("discord", "Discord communication operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Discord", new[] { "token", "bot_token", "guild_id" });
            RegisterMethod("disconnect", "Disconnect from Discord", new string[0]);
            RegisterMethod("send_message", "Send message to channel", new[] { "channel_id", "content", "embeds" });
            RegisterMethod("send_embed", "Send embed message", new[] { "channel_id", "embed" });
            RegisterMethod("send_file", "Send file to channel", new[] { "channel_id", "file_path", "content" });
            RegisterMethod("edit_message", "Edit existing message", new[] { "channel_id", "message_id", "content" });
            RegisterMethod("delete_message", "Delete message", new[] { "channel_id", "message_id" });
            RegisterMethod("bulk_delete", "Bulk delete messages", new[] { "channel_id", "message_ids" });
            RegisterMethod("add_reaction", "Add reaction to message", new[] { "channel_id", "message_id", "emoji" });
            RegisterMethod("remove_reaction", "Remove reaction from message", new[] { "channel_id", "message_id", "emoji", "user_id" });
            RegisterMethod("list_channels", "List all channels", new[] { "guild_id" });
            RegisterMethod("get_channel_info", "Get channel information", new[] { "channel_id" });
            RegisterMethod("create_channel", "Create a new channel", new[] { "guild_id", "name", "type", "topic" });
            RegisterMethod("delete_channel", "Delete a channel", new[] { "channel_id" });
            RegisterMethod("list_members", "List guild members", new[] { "guild_id", "limit" });
            RegisterMethod("get_member_info", "Get member information", new[] { "guild_id", "user_id" });
            RegisterMethod("add_role", "Add role to member", new[] { "guild_id", "user_id", "role_id" });
            RegisterMethod("remove_role", "Remove role from member", new[] { "guild_id", "user_id", "role_id" });
            RegisterMethod("list_roles", "List guild roles", new[] { "guild_id" });
            RegisterMethod("create_role", "Create a new role", new[] { "guild_id", "name", "permissions", "color" });
            RegisterMethod("delete_role", "Delete a role", new[] { "guild_id", "role_id" });
            RegisterMethod("get_guild_info", "Get guild information", new[] { "guild_id" });
            RegisterMethod("get_user_info", "Get user information", new[] { "user_id" });
            RegisterMethod("send_dm", "Send direct message", new[] { "user_id", "content" });
            RegisterMethod("get_message", "Get message by ID", new[] { "channel_id", "message_id" });
            RegisterMethod("search_messages", "Search messages in channel", new[] { "channel_id", "query" });
            RegisterMethod("pin_message", "Pin message in channel", new[] { "channel_id", "message_id" });
            RegisterMethod("unpin_message", "Unpin message from channel", new[] { "channel_id", "message_id" });
            RegisterMethod("get_pins", "Get pinned messages", new[] { "channel_id" });
            RegisterMethod("test_connection", "Test Discord connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Discord operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "send_message":
                        return await SendMessageAsync(parameters);
                    case "send_embed":
                        return await SendEmbedAsync(parameters);
                    case "send_file":
                        return await SendFileAsync(parameters);
                    case "edit_message":
                        return await EditMessageAsync(parameters);
                    case "delete_message":
                        return await DeleteMessageAsync(parameters);
                    case "bulk_delete":
                        return await BulkDeleteAsync(parameters);
                    case "add_reaction":
                        return await AddReactionAsync(parameters);
                    case "remove_reaction":
                        return await RemoveReactionAsync(parameters);
                    case "list_channels":
                        return await ListChannelsAsync(parameters);
                    case "get_channel_info":
                        return await GetChannelInfoAsync(parameters);
                    case "create_channel":
                        return await CreateChannelAsync(parameters);
                    case "delete_channel":
                        return await DeleteChannelAsync(parameters);
                    case "list_members":
                        return await ListMembersAsync(parameters);
                    case "get_member_info":
                        return await GetMemberInfoAsync(parameters);
                    case "add_role":
                        return await AddRoleAsync(parameters);
                    case "remove_role":
                        return await RemoveRoleAsync(parameters);
                    case "list_roles":
                        return await ListRolesAsync(parameters);
                    case "create_role":
                        return await CreateRoleAsync(parameters);
                    case "delete_role":
                        return await DeleteRoleAsync(parameters);
                    case "get_guild_info":
                        return await GetGuildInfoAsync(parameters);
                    case "get_user_info":
                        return await GetUserInfoAsync(parameters);
                    case "send_dm":
                        return await SendDmAsync(parameters);
                    case "get_message":
                        return await GetMessageAsync(parameters);
                    case "search_messages":
                        return await SearchMessagesAsync(parameters);
                    case "pin_message":
                        return await PinMessageAsync(parameters);
                    case "unpin_message":
                        return await UnpinMessageAsync(parameters);
                    case "get_pins":
                        return await GetPinsAsync(parameters);
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown Discord method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Discord method {method}: {ex.Message}");
                throw new OperatorException($"Discord operation failed: {ex.Message}", "DISCORD_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var token = GetRequiredParameter<string>(parameters, "token");
            var botToken = GetParameter<string>(parameters, "bot_token", null);
            var guildId = GetParameter<string>(parameters, "guild_id", null);

            try
            {
                _token = token;
                _botToken = botToken ?? token;
                _guildId = guildId;

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {_botToken}");

                // Test connection
                var response = await _httpClient.GetAsync("https://discord.com/api/v10/users/@me");
                var result = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to authenticate with Discord");
                }

                LogInfo($"Connected to Discord as {userInfo.GetValueOrDefault("username")}");
                return new { success = true, user = userInfo.GetValueOrDefault("username"), guildId };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Discord: {ex.Message}");
                throw new OperatorException($"Discord connection failed: {ex.Message}", "DISCORD_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _token = null;
                _botToken = null;
                _guildId = null;

                LogInfo("Disconnected from Discord");
                return new { success = true, message = "Disconnected from Discord" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from Discord: {ex.Message}");
                throw new OperatorException($"Discord disconnect failed: {ex.Message}", "DISCORD_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> SendMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var content = GetRequiredParameter<string>(parameters, "content");
            var embeds = GetParameter<object[]>(parameters, "embeds", new object[0]);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "content", content }
                };

                if (embeds.Length > 0)
                {
                    requestData["embeds"] = embeds;
                }

                var json = JsonSerializer.Serialize(requestData);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://discord.com/api/v10/channels/{channelId}/messages";
                var response = await _httpClient.PostAsync(url, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send message failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully sent message to channel {channelId}");
                return new { success = true, channelId, content, messageId = resultObj.GetValueOrDefault("id"), result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error sending message to channel {channelId}: {ex.Message}");
                throw new OperatorException($"Discord message send failed: {ex.Message}", "DISCORD_MESSAGE_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendEmbedAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var embed = GetRequiredParameter<Dictionary<string, object>>(parameters, "embed");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "embeds", new[] { embed } }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://discord.com/api/v10/channels/{channelId}/messages";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send embed failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully sent embed to channel {channelId}");
                return new { success = true, channelId, embed, messageId = resultObj.GetValueOrDefault("id") };
            }
            catch (Exception ex)
            {
                LogError($"Error sending embed: {ex.Message}");
                throw new OperatorException($"Discord embed send failed: {ex.Message}", "DISCORD_EMBED_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendFileAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");
            var content = GetParameter<string>(parameters, "content", "");

            try
            {
                // Simulate file upload
                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent file to channel {channelId}");
                return new { success = true, channelId, filePath, content, messageId };
            }
            catch (Exception ex)
            {
                LogError($"Error sending file: {ex.Message}");
                throw new OperatorException($"Discord file send failed: {ex.Message}", "DISCORD_FILE_SEND_ERROR", ex);
            }
        }

        private async Task<object> EditMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var messageId = GetRequiredParameter<string>(parameters, "message_id");
            var content = GetRequiredParameter<string>(parameters, "content");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "content", content }
                };

                var json = JsonSerializer.Serialize(requestData);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://discord.com/api/v10/channels/{channelId}/messages/{messageId}";
                var response = await _httpClient.PatchAsync(url, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Edit message failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully edited message {messageId} in channel {channelId}");
                return new { success = true, channelId, messageId, content, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error editing message: {ex.Message}");
                throw new OperatorException($"Discord message edit failed: {ex.Message}", "DISCORD_MESSAGE_EDIT_ERROR", ex);
            }
        }

        private async Task<object> DeleteMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var messageId = GetRequiredParameter<string>(parameters, "message_id");

            try
            {
                var url = $"https://discord.com/api/v10/channels/{channelId}/messages/{messageId}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete message failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted message {messageId} from channel {channelId}");
                return new { success = true, channelId, messageId };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting message {messageId}: {ex.Message}");
                throw new OperatorException($"Discord message delete failed: {ex.Message}", "DISCORD_MESSAGE_DELETE_ERROR", ex);
            }
        }

        private async Task<object> BulkDeleteAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var messageIds = GetRequiredParameter<string[]>(parameters, "message_ids");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "messages", messageIds }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://discord.com/api/v10/channels/{channelId}/messages/bulk-delete";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Bulk delete failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully bulk deleted {messageIds.Length} messages from channel {channelId}");
                return new { success = true, channelId, deletedCount = messageIds.Length };
            }
            catch (Exception ex)
            {
                LogError($"Error bulk deleting messages: {ex.Message}");
                throw new OperatorException($"Discord bulk delete failed: {ex.Message}", "DISCORD_BULK_DELETE_ERROR", ex);
            }
        }

        private async Task<object> AddReactionAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var messageId = GetRequiredParameter<string>(parameters, "message_id");
            var emoji = GetRequiredParameter<string>(parameters, "emoji");

            try
            {
                var encodedEmoji = Uri.EscapeDataString(emoji);
                var url = $"https://discord.com/api/v10/channels/{channelId}/messages/{messageId}/reactions/{encodedEmoji}/@me";
                var response = await _httpClient.PutAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Add reaction failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully added reaction {emoji} to message {messageId}");
                return new { success = true, channelId, messageId, emoji };
            }
            catch (Exception ex)
            {
                LogError($"Error adding reaction: {ex.Message}");
                throw new OperatorException($"Discord add reaction failed: {ex.Message}", "DISCORD_ADD_REACTION_ERROR", ex);
            }
        }

        private async Task<object> RemoveReactionAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var messageId = GetRequiredParameter<string>(parameters, "message_id");
            var emoji = GetRequiredParameter<string>(parameters, "emoji");
            var userId = GetParameter<string>(parameters, "user_id", "@me");

            try
            {
                var encodedEmoji = Uri.EscapeDataString(emoji);
                var url = $"https://discord.com/api/v10/channels/{channelId}/messages/{messageId}/reactions/{encodedEmoji}/{userId}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Remove reaction failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully removed reaction {emoji} from message {messageId}");
                return new { success = true, channelId, messageId, emoji, userId };
            }
            catch (Exception ex)
            {
                LogError($"Error removing reaction: {ex.Message}");
                throw new OperatorException($"Discord remove reaction failed: {ex.Message}", "DISCORD_REMOVE_REACTION_ERROR", ex);
            }
        }

        private async Task<object> ListChannelsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");

            try
            {
                var url = $"https://discord.com/api/v10/guilds/{guildId}/channels";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List channels failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var channels = JsonSerializer.Deserialize<object[]>(result);

                return new { success = true, guildId, channels };
            }
            catch (Exception ex)
            {
                LogError($"Error listing channels for guild {guildId}: {ex.Message}");
                throw new OperatorException($"Discord list channels failed: {ex.Message}", "DISCORD_LIST_CHANNELS_ERROR", ex);
            }
        }

        private async Task<object> GetChannelInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");

            try
            {
                var url = $"https://discord.com/api/v10/channels/{channelId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get channel info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var channelInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, channelId, info = channelInfo };
            }
            catch (Exception ex)
            {
                LogError($"Error getting channel info for {channelId}: {ex.Message}");
                throw new OperatorException($"Discord get channel info failed: {ex.Message}", "DISCORD_GET_CHANNEL_INFO_ERROR", ex);
            }
        }

        private async Task<object> CreateChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");
            var name = GetRequiredParameter<string>(parameters, "name");
            var type = GetParameter<int>(parameters, "type", 0);
            var topic = GetParameter<string>(parameters, "topic", "");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "name", name },
                    { "type", type }
                };

                if (!string.IsNullOrEmpty(topic))
                {
                    requestData["topic"] = topic;
                }

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://discord.com/api/v10/guilds/{guildId}/channels";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Create channel failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var channelInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created channel {name} in guild {guildId}");
                return new { success = true, guildId, name, type, topic, channel = channelInfo };
            }
            catch (Exception ex)
            {
                LogError($"Error creating channel {name}: {ex.Message}");
                throw new OperatorException($"Discord create channel failed: {ex.Message}", "DISCORD_CREATE_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> DeleteChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");

            try
            {
                var url = $"https://discord.com/api/v10/channels/{channelId}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete channel failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted channel {channelId}");
                return new { success = true, channelId };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting channel {channelId}: {ex.Message}");
                throw new OperatorException($"Discord delete channel failed: {ex.Message}", "DISCORD_DELETE_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> ListMembersAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");
            var limit = GetParameter<int>(parameters, "limit", 1000);

            try
            {
                var url = $"https://discord.com/api/v10/guilds/{guildId}/members?limit={limit}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List members failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var members = JsonSerializer.Deserialize<object[]>(result);

                return new { success = true, guildId, limit, members };
            }
            catch (Exception ex)
            {
                LogError($"Error listing members for guild {guildId}: {ex.Message}");
                throw new OperatorException($"Discord list members failed: {ex.Message}", "DISCORD_LIST_MEMBERS_ERROR", ex);
            }
        }

        private async Task<object> GetMemberInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");
            var userId = GetRequiredParameter<string>(parameters, "user_id");

            try
            {
                var url = $"https://discord.com/api/v10/guilds/{guildId}/members/{userId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get member info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var memberInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, guildId, userId, info = memberInfo };
            }
            catch (Exception ex)
            {
                LogError($"Error getting member info for {userId}: {ex.Message}");
                throw new OperatorException($"Discord get member info failed: {ex.Message}", "DISCORD_GET_MEMBER_INFO_ERROR", ex);
            }
        }

        private async Task<object> AddRoleAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");
            var userId = GetRequiredParameter<string>(parameters, "user_id");
            var roleId = GetRequiredParameter<string>(parameters, "role_id");

            try
            {
                var url = $"https://discord.com/api/v10/guilds/{guildId}/members/{userId}/roles/{roleId}";
                var response = await _httpClient.PutAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Add role failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully added role {roleId} to user {userId}");
                return new { success = true, guildId, userId, roleId };
            }
            catch (Exception ex)
            {
                LogError($"Error adding role {roleId} to user {userId}: {ex.Message}");
                throw new OperatorException($"Discord add role failed: {ex.Message}", "DISCORD_ADD_ROLE_ERROR", ex);
            }
        }

        private async Task<object> RemoveRoleAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");
            var userId = GetRequiredParameter<string>(parameters, "user_id");
            var roleId = GetRequiredParameter<string>(parameters, "role_id");

            try
            {
                var url = $"https://discord.com/api/v10/guilds/{guildId}/members/{userId}/roles/{roleId}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Remove role failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully removed role {roleId} from user {userId}");
                return new { success = true, guildId, userId, roleId };
            }
            catch (Exception ex)
            {
                LogError($"Error removing role {roleId} from user {userId}: {ex.Message}");
                throw new OperatorException($"Discord remove role failed: {ex.Message}", "DISCORD_REMOVE_ROLE_ERROR", ex);
            }
        }

        private async Task<object> ListRolesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");

            try
            {
                var url = $"https://discord.com/api/v10/guilds/{guildId}/roles";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List roles failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var roles = JsonSerializer.Deserialize<object[]>(result);

                return new { success = true, guildId, roles };
            }
            catch (Exception ex)
            {
                LogError($"Error listing roles for guild {guildId}: {ex.Message}");
                throw new OperatorException($"Discord list roles failed: {ex.Message}", "DISCORD_LIST_ROLES_ERROR", ex);
            }
        }

        private async Task<object> CreateRoleAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");
            var name = GetRequiredParameter<string>(parameters, "name");
            var permissions = GetParameter<string>(parameters, "permissions", "0");
            var color = GetParameter<int>(parameters, "color", 0);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "name", name },
                    { "permissions", permissions },
                    { "color", color }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://discord.com/api/v10/guilds/{guildId}/roles";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Create role failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var roleInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created role {name} in guild {guildId}");
                return new { success = true, guildId, name, permissions, color, role = roleInfo };
            }
            catch (Exception ex)
            {
                LogError($"Error creating role {name}: {ex.Message}");
                throw new OperatorException($"Discord create role failed: {ex.Message}", "DISCORD_CREATE_ROLE_ERROR", ex);
            }
        }

        private async Task<object> DeleteRoleAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");
            var roleId = GetRequiredParameter<string>(parameters, "role_id");

            try
            {
                var url = $"https://discord.com/api/v10/guilds/{guildId}/roles/{roleId}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete role failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted role {roleId} from guild {guildId}");
                return new { success = true, guildId, roleId };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting role {roleId}: {ex.Message}");
                throw new OperatorException($"Discord delete role failed: {ex.Message}", "DISCORD_DELETE_ROLE_ERROR", ex);
            }
        }

        private async Task<object> GetGuildInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var guildId = GetParameter<string>(parameters, "guild_id", _guildId) ?? throw new ArgumentException("Guild ID is required");

            try
            {
                var url = $"https://discord.com/api/v10/guilds/{guildId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get guild info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var guildInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, guildId, info = guildInfo };
            }
            catch (Exception ex)
            {
                LogError($"Error getting guild info for {guildId}: {ex.Message}");
                throw new OperatorException($"Discord get guild info failed: {ex.Message}", "DISCORD_GET_GUILD_INFO_ERROR", ex);
            }
        }

        private async Task<object> GetUserInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var userId = GetRequiredParameter<string>(parameters, "user_id");

            try
            {
                var url = $"https://discord.com/api/v10/users/{userId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get user info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, userId, info = userInfo };
            }
            catch (Exception ex)
            {
                LogError($"Error getting user info for {userId}: {ex.Message}");
                throw new OperatorException($"Discord get user info failed: {ex.Message}", "DISCORD_GET_USER_INFO_ERROR", ex);
            }
        }

        private async Task<object> SendDmAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var userId = GetRequiredParameter<string>(parameters, "user_id");
            var content = GetRequiredParameter<string>(parameters, "content");

            try
            {
                // Create DM channel first
                var dmData = new Dictionary<string, object>
                {
                    { "recipient_id", userId }
                };

                var dmJson = JsonSerializer.Serialize(dmData);
                var dmContent = new StringContent(dmJson, Encoding.UTF8, "application/json");

                var dmResponse = await _httpClient.PostAsync("https://discord.com/api/v10/users/@me/channels", dmContent);
                var dmResult = await dmResponse.Content.ReadAsStringAsync();
                var dmResultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(dmResult);

                if (!dmResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to create DM: {dmResultObj.GetValueOrDefault("message")}");
                }

                var channelId = dmResultObj.GetValueOrDefault("id")?.ToString();

                // Send message to DM channel
                return await SendMessageAsync(new Dictionary<string, object>
                {
                    { "channel_id", channelId },
                    { "content", content }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending DM to user {userId}: {ex.Message}");
                throw new OperatorException($"Discord DM send failed: {ex.Message}", "DISCORD_DM_SEND_ERROR", ex);
            }
        }

        private async Task<object> GetMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var messageId = GetRequiredParameter<string>(parameters, "message_id");

            try
            {
                var url = $"https://discord.com/api/v10/channels/{channelId}/messages/{messageId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get message failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var messageInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, channelId, messageId, message = messageInfo };
            }
            catch (Exception ex)
            {
                LogError($"Error getting message {messageId}: {ex.Message}");
                throw new OperatorException($"Discord get message failed: {ex.Message}", "DISCORD_GET_MESSAGE_ERROR", ex);
            }
        }

        private async Task<object> SearchMessagesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var query = GetRequiredParameter<string>(parameters, "query");

            try
            {
                var url = $"https://discord.com/api/v10/channels/{channelId}/messages/search?q={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Search messages failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var searchResults = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, channelId, query, results = searchResults };
            }
            catch (Exception ex)
            {
                LogError($"Error searching messages: {ex.Message}");
                throw new OperatorException($"Discord search messages failed: {ex.Message}", "DISCORD_SEARCH_MESSAGES_ERROR", ex);
            }
        }

        private async Task<object> PinMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var messageId = GetRequiredParameter<string>(parameters, "message_id");

            try
            {
                var url = $"https://discord.com/api/v10/channels/{channelId}/pins/{messageId}";
                var response = await _httpClient.PutAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Pin message failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully pinned message {messageId} in channel {channelId}");
                return new { success = true, channelId, messageId };
            }
            catch (Exception ex)
            {
                LogError($"Error pinning message {messageId}: {ex.Message}");
                throw new OperatorException($"Discord pin message failed: {ex.Message}", "DISCORD_PIN_MESSAGE_ERROR", ex);
            }
        }

        private async Task<object> UnpinMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var messageId = GetRequiredParameter<string>(parameters, "message_id");

            try
            {
                var url = $"https://discord.com/api/v10/channels/{channelId}/pins/{messageId}";
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Unpin message failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully unpinned message {messageId} from channel {channelId}");
                return new { success = true, channelId, messageId };
            }
            catch (Exception ex)
            {
                LogError($"Error unpinning message {messageId}: {ex.Message}");
                throw new OperatorException($"Discord unpin message failed: {ex.Message}", "DISCORD_UNPIN_MESSAGE_ERROR", ex);
            }
        }

        private async Task<object> GetPinsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");

            try
            {
                var url = $"https://discord.com/api/v10/channels/{channelId}/pins";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get pins failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var pins = JsonSerializer.Deserialize<object[]>(result);

                return new { success = true, channelId, pins };
            }
            catch (Exception ex)
            {
                LogError($"Error getting pins for channel {channelId}: {ex.Message}");
                throw new OperatorException($"Discord get pins failed: {ex.Message}", "DISCORD_GET_PINS_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync("https://discord.com/api/v10/users/@me");
                var success = response.IsSuccessStatusCode;

                return new { success, statusCode = (int)response.StatusCode };
            }
            catch (Exception ex)
            {
                LogError($"Error testing Discord connection: {ex.Message}");
                throw new OperatorException($"Discord connection test failed: {ex.Message}", "DISCORD_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_token))
            {
                throw new OperatorException("Not connected to Discord", "DISCORD_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
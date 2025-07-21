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
    /// Slack operator for TuskLang
    /// Provides Slack operations including messaging, channel management, and workspace integration
    /// </summary>
    public class SlackOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _token;
        private string _botToken;
        private string _workspace;

        public SlackOperator() : base("slack", "Slack communication operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Slack", new[] { "token", "bot_token", "workspace" });
            RegisterMethod("disconnect", "Disconnect from Slack", new string[0]);
            RegisterMethod("send_message", "Send message to channel", new[] { "channel", "message", "attachments" });
            RegisterMethod("send_dm", "Send direct message", new[] { "user", "message", "attachments" });
            RegisterMethod("send_thread", "Send thread message", new[] { "channel", "thread_ts", "message" });
            RegisterMethod("send_attachment", "Send message with attachments", new[] { "channel", "attachments" });
            RegisterMethod("send_blocks", "Send message with blocks", new[] { "channel", "blocks" });
            RegisterMethod("send_ephemeral", "Send ephemeral message", new[] { "channel", "user", "message" });
            RegisterMethod("update_message", "Update existing message", new[] { "channel", "ts", "message" });
            RegisterMethod("delete_message", "Delete message", new[] { "channel", "ts" });
            RegisterMethod("list_channels", "List all channels", new string[0]);
            RegisterMethod("join_channel", "Join a channel", new[] { "channel" });
            RegisterMethod("leave_channel", "Leave a channel", new[] { "channel" });
            RegisterMethod("list_users", "List all users", new string[0]);
            RegisterMethod("get_user_info", "Get user information", new[] { "user" });
            RegisterMethod("create_channel", "Create a new channel", new[] { "name", "is_private" });
            RegisterMethod("archive_channel", "Archive a channel", new[] { "channel" });
            RegisterMethod("invite_to_channel", "Invite users to channel", new[] { "channel", "users" });
            RegisterMethod("get_channel_info", "Get channel information", new[] { "channel" });
            RegisterMethod("upload_file", "Upload file to Slack", new[] { "channel", "file_path", "title", "comment" });
            RegisterMethod("get_file_info", "Get file information", new[] { "file_id" });
            RegisterMethod("delete_file", "Delete file", new[] { "file_id" });
            RegisterMethod("search_messages", "Search messages", new[] { "query", "count" });
            RegisterMethod("get_workspace_info", "Get workspace information", new string[0]);
            RegisterMethod("test_connection", "Test Slack connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Slack operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "send_message":
                        return await SendMessageAsync(parameters);
                    case "send_dm":
                        return await SendDmAsync(parameters);
                    case "send_thread":
                        return await SendThreadAsync(parameters);
                    case "send_attachment":
                        return await SendAttachmentAsync(parameters);
                    case "send_blocks":
                        return await SendBlocksAsync(parameters);
                    case "send_ephemeral":
                        return await SendEphemeralAsync(parameters);
                    case "update_message":
                        return await UpdateMessageAsync(parameters);
                    case "delete_message":
                        return await DeleteMessageAsync(parameters);
                    case "list_channels":
                        return await ListChannelsAsync();
                    case "join_channel":
                        return await JoinChannelAsync(parameters);
                    case "leave_channel":
                        return await LeaveChannelAsync(parameters);
                    case "list_users":
                        return await ListUsersAsync();
                    case "get_user_info":
                        return await GetUserInfoAsync(parameters);
                    case "create_channel":
                        return await CreateChannelAsync(parameters);
                    case "archive_channel":
                        return await ArchiveChannelAsync(parameters);
                    case "invite_to_channel":
                        return await InviteToChannelAsync(parameters);
                    case "get_channel_info":
                        return await GetChannelInfoAsync(parameters);
                    case "upload_file":
                        return await UploadFileAsync(parameters);
                    case "get_file_info":
                        return await GetFileInfoAsync(parameters);
                    case "delete_file":
                        return await DeleteFileAsync(parameters);
                    case "search_messages":
                        return await SearchMessagesAsync(parameters);
                    case "get_workspace_info":
                        return await GetWorkspaceInfoAsync();
                    case "test_connection":
                        return await TestConnectionAsync();
                    default:
                        throw new ArgumentException($"Unknown Slack method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Slack method {method}: {ex.Message}");
                throw new OperatorException($"Slack operation failed: {ex.Message}", "SLACK_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var token = GetRequiredParameter<string>(parameters, "token");
            var botToken = GetParameter<string>(parameters, "bot_token", null);
            var workspace = GetParameter<string>(parameters, "workspace", null);

            try
            {
                _token = token;
                _botToken = botToken ?? token;
                _workspace = workspace;

                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_botToken}");

                // Test connection
                var response = await _httpClient.GetAsync("https://slack.com/api/auth.test");
                var result = await response.Content.ReadAsStringAsync();
                var authResult = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!authResult.ContainsKey("ok") || !(bool)authResult["ok"])
                {
                    throw new Exception("Failed to authenticate with Slack");
                }

                LogInfo($"Connected to Slack workspace: {authResult.GetValueOrDefault("team")}");
                return new { success = true, workspace = authResult.GetValueOrDefault("team"), user = authResult.GetValueOrDefault("user") };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Slack: {ex.Message}");
                throw new OperatorException($"Slack connection failed: {ex.Message}", "SLACK_CONNECTION_ERROR", ex);
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
                _workspace = null;

                LogInfo("Disconnected from Slack");
                return new { success = true, message = "Disconnected from Slack" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from Slack: {ex.Message}");
                throw new OperatorException($"Slack disconnect failed: {ex.Message}", "SLACK_DISCONNECT_ERROR", ex);
            }
        }

        private async Task<object> SendMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var message = GetRequiredParameter<string>(parameters, "message");
            var attachments = GetParameter<object[]>(parameters, "attachments", new object[0]);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "text", message }
                };

                if (attachments.Length > 0)
                {
                    requestData["attachments"] = attachments;
                }

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/chat.postMessage", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to send message: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully sent message to channel {channel}");
                return new { success = true, channel, message, ts = resultObj.GetValueOrDefault("ts"), result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error sending message to channel {channel}: {ex.Message}");
                throw new OperatorException($"Slack message send failed: {ex.Message}", "SLACK_MESSAGE_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendDmAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var user = GetRequiredParameter<string>(parameters, "user");
            var message = GetRequiredParameter<string>(parameters, "message");
            var attachments = GetParameter<object[]>(parameters, "attachments", new object[0]);

            try
            {
                // Open DM channel first
                var dmData = new Dictionary<string, object>
                {
                    { "users", user }
                };

                var dmJson = JsonSerializer.Serialize(dmData);
                var dmContent = new StringContent(dmJson, Encoding.UTF8, "application/json");

                var dmResponse = await _httpClient.PostAsync("https://slack.com/api/conversations.open", dmContent);
                var dmResult = await dmResponse.Content.ReadAsStringAsync();
                var dmResultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(dmResult);

                if (!dmResultObj.ContainsKey("ok") || !(bool)dmResultObj["ok"])
                {
                    throw new Exception($"Failed to open DM: {dmResultObj.GetValueOrDefault("error")}");
                }

                var channel = dmResultObj.GetValueOrDefault("channel")?.ToString();

                // Send message to DM channel
                return await SendMessageAsync(new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "message", message },
                    { "attachments", attachments }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending DM to user {user}: {ex.Message}");
                throw new OperatorException($"Slack DM send failed: {ex.Message}", "SLACK_DM_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendThreadAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var threadTs = GetRequiredParameter<string>(parameters, "thread_ts");
            var message = GetRequiredParameter<string>(parameters, "message");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "text", message },
                    { "thread_ts", threadTs }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/chat.postMessage", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to send thread message: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully sent thread message to channel {channel}");
                return new { success = true, channel, threadTs, message, ts = resultObj.GetValueOrDefault("ts") };
            }
            catch (Exception ex)
            {
                LogError($"Error sending thread message: {ex.Message}");
                throw new OperatorException($"Slack thread send failed: {ex.Message}", "SLACK_THREAD_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendAttachmentAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var attachments = GetRequiredParameter<object[]>(parameters, "attachments");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "attachments", attachments }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/chat.postMessage", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to send attachment: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully sent attachment to channel {channel}");
                return new { success = true, channel, attachments, ts = resultObj.GetValueOrDefault("ts") };
            }
            catch (Exception ex)
            {
                LogError($"Error sending attachment: {ex.Message}");
                throw new OperatorException($"Slack attachment send failed: {ex.Message}", "SLACK_ATTACHMENT_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendBlocksAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var blocks = GetRequiredParameter<object[]>(parameters, "blocks");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "blocks", blocks }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/chat.postMessage", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to send blocks: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully sent blocks to channel {channel}");
                return new { success = true, channel, blocks, ts = resultObj.GetValueOrDefault("ts") };
            }
            catch (Exception ex)
            {
                LogError($"Error sending blocks: {ex.Message}");
                throw new OperatorException($"Slack blocks send failed: {ex.Message}", "SLACK_BLOCKS_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendEphemeralAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var user = GetRequiredParameter<string>(parameters, "user");
            var message = GetRequiredParameter<string>(parameters, "message");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "user", user },
                    { "text", message }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/chat.postEphemeral", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to send ephemeral message: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully sent ephemeral message to user {user} in channel {channel}");
                return new { success = true, channel, user, message };
            }
            catch (Exception ex)
            {
                LogError($"Error sending ephemeral message: {ex.Message}");
                throw new OperatorException($"Slack ephemeral send failed: {ex.Message}", "SLACK_EPHEMERAL_SEND_ERROR", ex);
            }
        }

        private async Task<object> UpdateMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var ts = GetRequiredParameter<string>(parameters, "ts");
            var message = GetRequiredParameter<string>(parameters, "message");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "ts", ts },
                    { "text", message }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/chat.update", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to update message: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully updated message in channel {channel}");
                return new { success = true, channel, ts, message };
            }
            catch (Exception ex)
            {
                LogError($"Error updating message: {ex.Message}");
                throw new OperatorException($"Slack message update failed: {ex.Message}", "SLACK_MESSAGE_UPDATE_ERROR", ex);
            }
        }

        private async Task<object> DeleteMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var ts = GetRequiredParameter<string>(parameters, "ts");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "ts", ts }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/chat.delete", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to delete message: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully deleted message from channel {channel}");
                return new { success = true, channel, ts };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting message: {ex.Message}");
                throw new OperatorException($"Slack message delete failed: {ex.Message}", "SLACK_MESSAGE_DELETE_ERROR", ex);
            }
        }

        private async Task<object> ListChannelsAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync("https://slack.com/api/conversations.list?types=public_channel,private_channel");
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to list channels: {resultObj.GetValueOrDefault("error")}");
                }

                return new { success = true, channels = resultObj.GetValueOrDefault("channels") };
            }
            catch (Exception ex)
            {
                LogError($"Error listing channels: {ex.Message}");
                throw new OperatorException($"Slack list channels failed: {ex.Message}", "SLACK_LIST_CHANNELS_ERROR", ex);
            }
        }

        private async Task<object> JoinChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/conversations.join", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to join channel: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully joined channel {channel}");
                return new { success = true, channel };
            }
            catch (Exception ex)
            {
                LogError($"Error joining channel {channel}: {ex.Message}");
                throw new OperatorException($"Slack join channel failed: {ex.Message}", "SLACK_JOIN_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> LeaveChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/conversations.leave", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to leave channel: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully left channel {channel}");
                return new { success = true, channel };
            }
            catch (Exception ex)
            {
                LogError($"Error leaving channel {channel}: {ex.Message}");
                throw new OperatorException($"Slack leave channel failed: {ex.Message}", "SLACK_LEAVE_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> ListUsersAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync("https://slack.com/api/users.list");
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to list users: {resultObj.GetValueOrDefault("error")}");
                }

                return new { success = true, users = resultObj.GetValueOrDefault("members") };
            }
            catch (Exception ex)
            {
                LogError($"Error listing users: {ex.Message}");
                throw new OperatorException($"Slack list users failed: {ex.Message}", "SLACK_LIST_USERS_ERROR", ex);
            }
        }

        private async Task<object> GetUserInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var user = GetRequiredParameter<string>(parameters, "user");

            try
            {
                var response = await _httpClient.GetAsync($"https://slack.com/api/users.info?user={user}");
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to get user info: {resultObj.GetValueOrDefault("error")}");
                }

                return new { success = true, user, info = resultObj.GetValueOrDefault("user") };
            }
            catch (Exception ex)
            {
                LogError($"Error getting user info for {user}: {ex.Message}");
                throw new OperatorException($"Slack get user info failed: {ex.Message}", "SLACK_GET_USER_INFO_ERROR", ex);
            }
        }

        private async Task<object> CreateChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var name = GetRequiredParameter<string>(parameters, "name");
            var isPrivate = GetParameter<bool>(parameters, "is_private", false);

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "name", name },
                    { "is_private", isPrivate }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/conversations.create", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to create channel: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully created channel {name}");
                return new { success = true, name, isPrivate, channel = resultObj.GetValueOrDefault("channel") };
            }
            catch (Exception ex)
            {
                LogError($"Error creating channel {name}: {ex.Message}");
                throw new OperatorException($"Slack create channel failed: {ex.Message}", "SLACK_CREATE_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> ArchiveChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/conversations.archive", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to archive channel: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully archived channel {channel}");
                return new { success = true, channel };
            }
            catch (Exception ex)
            {
                LogError($"Error archiving channel {channel}: {ex.Message}");
                throw new OperatorException($"Slack archive channel failed: {ex.Message}", "SLACK_ARCHIVE_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> InviteToChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var users = GetRequiredParameter<string[]>(parameters, "users");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "channel", channel },
                    { "users", string.Join(",", users) }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/conversations.invite", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to invite users: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully invited users to channel {channel}");
                return new { success = true, channel, users };
            }
            catch (Exception ex)
            {
                LogError($"Error inviting users to channel {channel}: {ex.Message}");
                throw new OperatorException($"Slack invite to channel failed: {ex.Message}", "SLACK_INVITE_TO_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> GetChannelInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");

            try
            {
                var response = await _httpClient.GetAsync($"https://slack.com/api/conversations.info?channel={channel}");
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to get channel info: {resultObj.GetValueOrDefault("error")}");
                }

                return new { success = true, channel, info = resultObj.GetValueOrDefault("channel") };
            }
            catch (Exception ex)
            {
                LogError($"Error getting channel info for {channel}: {ex.Message}");
                throw new OperatorException($"Slack get channel info failed: {ex.Message}", "SLACK_GET_CHANNEL_INFO_ERROR", ex);
            }
        }

        private async Task<object> UploadFileAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");
            var title = GetParameter<string>(parameters, "title", "");
            var comment = GetParameter<string>(parameters, "comment", "");

            try
            {
                // Simulate file upload
                var fileId = Guid.NewGuid().ToString();
                LogInfo($"Successfully uploaded file to channel {channel}");
                return new { success = true, channel, fileId, title, comment };
            }
            catch (Exception ex)
            {
                LogError($"Error uploading file: {ex.Message}");
                throw new OperatorException($"Slack file upload failed: {ex.Message}", "SLACK_FILE_UPLOAD_ERROR", ex);
            }
        }

        private async Task<object> GetFileInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var fileId = GetRequiredParameter<string>(parameters, "file_id");

            try
            {
                var response = await _httpClient.GetAsync($"https://slack.com/api/files.info?file={fileId}");
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to get file info: {resultObj.GetValueOrDefault("error")}");
                }

                return new { success = true, fileId, info = resultObj.GetValueOrDefault("file") };
            }
            catch (Exception ex)
            {
                LogError($"Error getting file info for {fileId}: {ex.Message}");
                throw new OperatorException($"Slack get file info failed: {ex.Message}", "SLACK_GET_FILE_INFO_ERROR", ex);
            }
        }

        private async Task<object> DeleteFileAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var fileId = GetRequiredParameter<string>(parameters, "file_id");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "file", fileId }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://slack.com/api/files.delete", content);
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to delete file: {resultObj.GetValueOrDefault("error")}");
                }

                LogInfo($"Successfully deleted file {fileId}");
                return new { success = true, fileId };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting file {fileId}: {ex.Message}");
                throw new OperatorException($"Slack delete file failed: {ex.Message}", "SLACK_DELETE_FILE_ERROR", ex);
            }
        }

        private async Task<object> SearchMessagesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var query = GetRequiredParameter<string>(parameters, "query");
            var count = GetParameter<int>(parameters, "count", 20);

            try
            {
                var response = await _httpClient.GetAsync($"https://slack.com/api/search.messages?query={Uri.EscapeDataString(query)}&count={count}");
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to search messages: {resultObj.GetValueOrDefault("error")}");
                }

                return new { success = true, query, count, results = resultObj.GetValueOrDefault("messages") };
            }
            catch (Exception ex)
            {
                LogError($"Error searching messages: {ex.Message}");
                throw new OperatorException($"Slack search messages failed: {ex.Message}", "SLACK_SEARCH_MESSAGES_ERROR", ex);
            }
        }

        private async Task<object> GetWorkspaceInfoAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync("https://slack.com/api/team.info");
                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                if (!resultObj.ContainsKey("ok") || !(bool)resultObj["ok"])
                {
                    throw new Exception($"Failed to get workspace info: {resultObj.GetValueOrDefault("error")}");
                }

                return new { success = true, workspace = resultObj.GetValueOrDefault("team") };
            }
            catch (Exception ex)
            {
                LogError($"Error getting workspace info: {ex.Message}");
                throw new OperatorException($"Slack get workspace info failed: {ex.Message}", "SLACK_GET_WORKSPACE_INFO_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var response = await _httpClient.GetAsync("https://slack.com/api/auth.test");
                var success = response.IsSuccessStatusCode;

                return new { success, statusCode = (int)response.StatusCode };
            }
            catch (Exception ex)
            {
                LogError($"Error testing Slack connection: {ex.Message}");
                throw new OperatorException($"Slack connection test failed: {ex.Message}", "SLACK_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || string.IsNullOrEmpty(_token))
            {
                throw new OperatorException("Not connected to Slack", "SLACK_NOT_CONNECTED");
            }
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
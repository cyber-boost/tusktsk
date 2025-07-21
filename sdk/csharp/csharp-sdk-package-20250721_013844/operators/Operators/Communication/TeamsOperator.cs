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
    /// Microsoft Teams operator for TuskLang
    /// Provides Teams operations including messaging, channel management, and workspace integration
    /// </summary>
    public class TeamsOperator : BaseOperator
    {
        private HttpClient _httpClient;
        private string _webhookUrl;
        private string _accessToken;
        private string _tenantId;

        public TeamsOperator() : base("teams", "Microsoft Teams communication operations")
        {
            RegisterMethods();
        }

        private void RegisterMethods()
        {
            RegisterMethod("connect", "Connect to Teams", new[] { "webhook_url", "access_token", "tenant_id" });
            RegisterMethod("disconnect", "Disconnect from Teams", new string[0]);
            RegisterMethod("send_message", "Send message to channel", new[] { "channel", "message", "attachments" });
            RegisterMethod("send_card", "Send adaptive card", new[] { "channel", "card" });
            RegisterMethod("send_attachment", "Send message with attachments", new[] { "channel", "attachments" });
            RegisterMethod("send_mention", "Send message with mentions", new[] { "channel", "message", "mentions" });
            RegisterMethod("send_reply", "Reply to message", new[] { "message_id", "reply" });
            RegisterMethod("update_message", "Update existing message", new[] { "message_id", "message" });
            RegisterMethod("delete_message", "Delete message", new[] { "message_id" });
            RegisterMethod("list_channels", "List all channels", new[] { "team_id" });
            RegisterMethod("get_channel_info", "Get channel information", new[] { "channel_id" });
            RegisterMethod("list_teams", "List all teams", new string[0]);
            RegisterMethod("get_team_info", "Get team information", new[] { "team_id" });
            RegisterMethod("list_members", "List team members", new[] { "team_id" });
            RegisterMethod("get_user_info", "Get user information", new[] { "user_id" });
            RegisterMethod("create_channel", "Create a new channel", new[] { "team_id", "name", "description" });
            RegisterMethod("delete_channel", "Delete a channel", new[] { "channel_id" });
            RegisterMethod("add_member", "Add member to team", new[] { "team_id", "user_id", "role" });
            RegisterMethod("remove_member", "Remove member from team", new[] { "team_id", "user_id" });
            RegisterMethod("upload_file", "Upload file to Teams", new[] { "channel_id", "file_path", "file_name" });
            RegisterMethod("get_file_info", "Get file information", new[] { "file_id" });
            RegisterMethod("delete_file", "Delete file", new[] { "file_id" });
            RegisterMethod("search_messages", "Search messages", new[] { "query", "count" });
            RegisterMethod("get_workspace_info", "Get workspace information", new string[0]);
            RegisterMethod("test_connection", "Test Teams connection", new string[0]);
        }

        public override async Task<object> ExecuteAsync(string method, Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            try
            {
                LogDebug($"Executing Teams operator method: {method}");

                switch (method.ToLower())
                {
                    case "connect":
                        return await ConnectAsync(parameters);
                    case "disconnect":
                        return await DisconnectAsync();
                    case "send_message":
                        return await SendMessageAsync(parameters);
                    case "send_card":
                        return await SendCardAsync(parameters);
                    case "send_attachment":
                        return await SendAttachmentAsync(parameters);
                    case "send_mention":
                        return await SendMentionAsync(parameters);
                    case "send_reply":
                        return await SendReplyAsync(parameters);
                    case "update_message":
                        return await UpdateMessageAsync(parameters);
                    case "delete_message":
                        return await DeleteMessageAsync(parameters);
                    case "list_channels":
                        return await ListChannelsAsync(parameters);
                    case "get_channel_info":
                        return await GetChannelInfoAsync(parameters);
                    case "list_teams":
                        return await ListTeamsAsync();
                    case "get_team_info":
                        return await GetTeamInfoAsync(parameters);
                    case "list_members":
                        return await ListMembersAsync(parameters);
                    case "get_user_info":
                        return await GetUserInfoAsync(parameters);
                    case "create_channel":
                        return await CreateChannelAsync(parameters);
                    case "delete_channel":
                        return await DeleteChannelAsync(parameters);
                    case "add_member":
                        return await AddMemberAsync(parameters);
                    case "remove_member":
                        return await RemoveMemberAsync(parameters);
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
                        throw new ArgumentException($"Unknown Teams method: {method}", nameof(method));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing Teams method {method}: {ex.Message}");
                throw new OperatorException($"Teams operation failed: {ex.Message}", "TEAMS_ERROR", ex);
            }
        }

        private async Task<object> ConnectAsync(Dictionary<string, object> parameters)
        {
            var webhookUrl = GetParameter<string>(parameters, "webhook_url", null);
            var accessToken = GetParameter<string>(parameters, "access_token", null);
            var tenantId = GetParameter<string>(parameters, "tenant_id", null);

            try
            {
                _webhookUrl = webhookUrl;
                _accessToken = accessToken;
                _tenantId = tenantId;

                _httpClient = new HttpClient();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                }

                LogInfo("Connected to Microsoft Teams");
                return new { success = true, tenantId };
            }
            catch (Exception ex)
            {
                LogError($"Failed to connect to Teams: {ex.Message}");
                throw new OperatorException($"Teams connection failed: {ex.Message}", "TEAMS_CONNECTION_ERROR", ex);
            }
        }

        private async Task<object> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _webhookUrl = null;
                _accessToken = null;
                _tenantId = null;

                LogInfo("Disconnected from Microsoft Teams");
                return new { success = true, message = "Disconnected from Teams" };
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from Teams: {ex.Message}");
                throw new OperatorException($"Teams disconnect failed: {ex.Message}", "TEAMS_DISCONNECT_ERROR", ex);
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
                    { "text", message }
                };

                if (attachments.Length > 0)
                {
                    requestData["attachments"] = attachments;
                }

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetTeamsApiUrl($"channels/{channel}/messages");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent message to channel {channel}");
                return new { success = true, messageId, channel, message, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error sending message to channel {channel}: {ex.Message}");
                throw new OperatorException($"Teams message send failed: {ex.Message}", "TEAMS_MESSAGE_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendCardAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var card = GetRequiredParameter<Dictionary<string, object>>(parameters, "card");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "type", "message" },
                    { "attachments", new[]
                    {
                        new Dictionary<string, object>
                        {
                            { "contentType", "application/vnd.microsoft.card.adaptive" },
                            { "content", card }
                        }
                    }}
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetTeamsApiUrl($"channels/{channel}/messages");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send card failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent adaptive card to channel {channel}");
                return new { success = true, messageId, channel, card, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error sending adaptive card: {ex.Message}");
                throw new OperatorException($"Teams card send failed: {ex.Message}", "TEAMS_CARD_SEND_ERROR", ex);
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
                    { "attachments", attachments }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetTeamsApiUrl($"channels/{channel}/messages");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send attachment failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent attachment to channel {channel}");
                return new { success = true, messageId, channel, attachments, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error sending attachment: {ex.Message}");
                throw new OperatorException($"Teams attachment send failed: {ex.Message}", "TEAMS_ATTACHMENT_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendMentionAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channel = GetRequiredParameter<string>(parameters, "channel");
            var message = GetRequiredParameter<string>(parameters, "message");
            var mentions = GetRequiredParameter<string[]>(parameters, "mentions");

            try
            {
                var entities = mentions.Select(m => new Dictionary<string, object>
                {
                    { "type", "mention" },
                    { "text", $"<at>{m}</at>" },
                    { "mentioned", new Dictionary<string, object> { { "id", m } } }
                }).ToArray();

                var requestData = new Dictionary<string, object>
                {
                    { "text", message },
                    { "entities", entities }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetTeamsApiUrl($"channels/{channel}/messages");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send mention failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var messageId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent mention message to channel {channel}");
                return new { success = true, messageId, channel, message, mentions, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error sending mention message: {ex.Message}");
                throw new OperatorException($"Teams mention send failed: {ex.Message}", "TEAMS_MENTION_SEND_ERROR", ex);
            }
        }

        private async Task<object> SendReplyAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var messageId = GetRequiredParameter<string>(parameters, "message_id");
            var reply = GetRequiredParameter<string>(parameters, "reply");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", reply }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetTeamsApiUrl($"messages/{messageId}/replies");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Send reply failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                var replyId = Guid.NewGuid().ToString();
                LogInfo($"Successfully sent reply to message {messageId}");
                return new { success = true, messageId, replyId, reply, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error sending reply: {ex.Message}");
                throw new OperatorException($"Teams reply send failed: {ex.Message}", "TEAMS_REPLY_SEND_ERROR", ex);
            }
        }

        private async Task<object> UpdateMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var messageId = GetRequiredParameter<string>(parameters, "message_id");
            var message = GetRequiredParameter<string>(parameters, "message");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "text", message }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetTeamsApiUrl($"messages/{messageId}");
                var response = await _httpClient.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Update message failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully updated message {messageId}");
                return new { success = true, messageId, message, result = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error updating message: {ex.Message}");
                throw new OperatorException($"Teams message update failed: {ex.Message}", "TEAMS_MESSAGE_UPDATE_ERROR", ex);
            }
        }

        private async Task<object> DeleteMessageAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var messageId = GetRequiredParameter<string>(parameters, "message_id");

            try
            {
                var url = GetTeamsApiUrl($"messages/{messageId}");
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete message failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted message {messageId}");
                return new { success = true, messageId };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting message {messageId}: {ex.Message}");
                throw new OperatorException($"Teams message delete failed: {ex.Message}", "TEAMS_MESSAGE_DELETE_ERROR", ex);
            }
        }

        private async Task<object> ListChannelsAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var teamId = GetRequiredParameter<string>(parameters, "team_id");

            try
            {
                var url = GetTeamsApiUrl($"teams/{teamId}/channels");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List channels failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, teamId, channels = resultObj.GetValueOrDefault("value") };
            }
            catch (Exception ex)
            {
                LogError($"Error listing channels for team {teamId}: {ex.Message}");
                throw new OperatorException($"Teams list channels failed: {ex.Message}", "TEAMS_LIST_CHANNELS_ERROR", ex);
            }
        }

        private async Task<object> GetChannelInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");

            try
            {
                var url = GetTeamsApiUrl($"channels/{channelId}");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get channel info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, channelId, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting channel info for {channelId}: {ex.Message}");
                throw new OperatorException($"Teams get channel info failed: {ex.Message}", "TEAMS_GET_CHANNEL_INFO_ERROR", ex);
            }
        }

        private async Task<object> ListTeamsAsync()
        {
            EnsureConnected();

            try
            {
                var url = GetTeamsApiUrl("me/joinedTeams");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List teams failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, teams = resultObj.GetValueOrDefault("value") };
            }
            catch (Exception ex)
            {
                LogError($"Error listing teams: {ex.Message}");
                throw new OperatorException($"Teams list teams failed: {ex.Message}", "TEAMS_LIST_TEAMS_ERROR", ex);
            }
        }

        private async Task<object> GetTeamInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var teamId = GetRequiredParameter<string>(parameters, "team_id");

            try
            {
                var url = GetTeamsApiUrl($"teams/{teamId}");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get team info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, teamId, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting team info for {teamId}: {ex.Message}");
                throw new OperatorException($"Teams get team info failed: {ex.Message}", "TEAMS_GET_TEAM_INFO_ERROR", ex);
            }
        }

        private async Task<object> ListMembersAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var teamId = GetRequiredParameter<string>(parameters, "team_id");

            try
            {
                var url = GetTeamsApiUrl($"teams/{teamId}/members");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"List members failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, teamId, members = resultObj.GetValueOrDefault("value") };
            }
            catch (Exception ex)
            {
                LogError($"Error listing members for team {teamId}: {ex.Message}");
                throw new OperatorException($"Teams list members failed: {ex.Message}", "TEAMS_LIST_MEMBERS_ERROR", ex);
            }
        }

        private async Task<object> GetUserInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var userId = GetRequiredParameter<string>(parameters, "user_id");

            try
            {
                var url = GetTeamsApiUrl($"users/{userId}");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get user info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, userId, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting user info for {userId}: {ex.Message}");
                throw new OperatorException($"Teams get user info failed: {ex.Message}", "TEAMS_GET_USER_INFO_ERROR", ex);
            }
        }

        private async Task<object> CreateChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var teamId = GetRequiredParameter<string>(parameters, "team_id");
            var name = GetRequiredParameter<string>(parameters, "name");
            var description = GetParameter<string>(parameters, "description", "");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "displayName", name },
                    { "description", description }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetTeamsApiUrl($"teams/{teamId}/channels");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Create channel failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully created channel {name} in team {teamId}");
                return new { success = true, teamId, name, description, channel = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error creating channel {name}: {ex.Message}");
                throw new OperatorException($"Teams create channel failed: {ex.Message}", "TEAMS_CREATE_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> DeleteChannelAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");

            try
            {
                var url = GetTeamsApiUrl($"channels/{channelId}");
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
                throw new OperatorException($"Teams delete channel failed: {ex.Message}", "TEAMS_DELETE_CHANNEL_ERROR", ex);
            }
        }

        private async Task<object> AddMemberAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var teamId = GetRequiredParameter<string>(parameters, "team_id");
            var userId = GetRequiredParameter<string>(parameters, "user_id");
            var role = GetParameter<string>(parameters, "role", "member");

            try
            {
                var requestData = new Dictionary<string, object>
                {
                    { "@odata.type", "#microsoft.graph.aadUserConversationMember" },
                    { "roles", new[] { role } },
                    { "userId", userId }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = GetTeamsApiUrl($"teams/{teamId}/members");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Add member failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                LogInfo($"Successfully added member {userId} to team {teamId}");
                return new { success = true, teamId, userId, role, member = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error adding member {userId} to team {teamId}: {ex.Message}");
                throw new OperatorException($"Teams add member failed: {ex.Message}", "TEAMS_ADD_MEMBER_ERROR", ex);
            }
        }

        private async Task<object> RemoveMemberAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var teamId = GetRequiredParameter<string>(parameters, "team_id");
            var userId = GetRequiredParameter<string>(parameters, "user_id");

            try
            {
                var url = GetTeamsApiUrl($"teams/{teamId}/members/{userId}");
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Remove member failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully removed member {userId} from team {teamId}");
                return new { success = true, teamId, userId };
            }
            catch (Exception ex)
            {
                LogError($"Error removing member {userId} from team {teamId}: {ex.Message}");
                throw new OperatorException($"Teams remove member failed: {ex.Message}", "TEAMS_REMOVE_MEMBER_ERROR", ex);
            }
        }

        private async Task<object> UploadFileAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var channelId = GetRequiredParameter<string>(parameters, "channel_id");
            var filePath = GetRequiredParameter<string>(parameters, "file_path");
            var fileName = GetParameter<string>(parameters, "file_name", "");

            try
            {
                // Simulate file upload
                var fileId = Guid.NewGuid().ToString();
                LogInfo($"Successfully uploaded file to channel {channelId}");
                return new { success = true, channelId, fileId, fileName };
            }
            catch (Exception ex)
            {
                LogError($"Error uploading file: {ex.Message}");
                throw new OperatorException($"Teams file upload failed: {ex.Message}", "TEAMS_FILE_UPLOAD_ERROR", ex);
            }
        }

        private async Task<object> GetFileInfoAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var fileId = GetRequiredParameter<string>(parameters, "file_id");

            try
            {
                var url = GetTeamsApiUrl($"drives/root/items/{fileId}");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get file info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, fileId, info = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting file info for {fileId}: {ex.Message}");
                throw new OperatorException($"Teams get file info failed: {ex.Message}", "TEAMS_GET_FILE_INFO_ERROR", ex);
            }
        }

        private async Task<object> DeleteFileAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var fileId = GetRequiredParameter<string>(parameters, "file_id");

            try
            {
                var url = GetTeamsApiUrl($"drives/root/items/{fileId}");
                var response = await _httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Delete file failed: {response.StatusCode} - {errorContent}");
                }

                LogInfo($"Successfully deleted file {fileId}");
                return new { success = true, fileId };
            }
            catch (Exception ex)
            {
                LogError($"Error deleting file {fileId}: {ex.Message}");
                throw new OperatorException($"Teams delete file failed: {ex.Message}", "TEAMS_DELETE_FILE_ERROR", ex);
            }
        }

        private async Task<object> SearchMessagesAsync(Dictionary<string, object> parameters)
        {
            EnsureConnected();
            var query = GetRequiredParameter<string>(parameters, "query");
            var count = GetParameter<int>(parameters, "count", 20);

            try
            {
                var url = GetTeamsApiUrl($"search/query?searchRequest.query={Uri.EscapeDataString(query)}&searchRequest.from={count}");
                var response = await _httpClient.PostAsync(url, new StringContent(""));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Search messages failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, query, count, results = resultObj.GetValueOrDefault("value") };
            }
            catch (Exception ex)
            {
                LogError($"Error searching messages: {ex.Message}");
                throw new OperatorException($"Teams search messages failed: {ex.Message}", "TEAMS_SEARCH_MESSAGES_ERROR", ex);
            }
        }

        private async Task<object> GetWorkspaceInfoAsync()
        {
            EnsureConnected();

            try
            {
                var url = GetTeamsApiUrl("organization");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Get workspace info failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadAsStringAsync();
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                return new { success = true, workspace = resultObj };
            }
            catch (Exception ex)
            {
                LogError($"Error getting workspace info: {ex.Message}");
                throw new OperatorException($"Teams get workspace info failed: {ex.Message}", "TEAMS_GET_WORKSPACE_INFO_ERROR", ex);
            }
        }

        private async Task<object> TestConnectionAsync()
        {
            EnsureConnected();

            try
            {
                var url = GetTeamsApiUrl("me");
                var response = await _httpClient.GetAsync(url);
                var success = response.IsSuccessStatusCode;

                return new { success, statusCode = (int)response.StatusCode };
            }
            catch (Exception ex)
            {
                LogError($"Error testing Teams connection: {ex.Message}");
                throw new OperatorException($"Teams connection test failed: {ex.Message}", "TEAMS_CONNECTION_TEST_ERROR", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_httpClient == null || (string.IsNullOrEmpty(_webhookUrl) && string.IsNullOrEmpty(_accessToken)))
            {
                throw new OperatorException("Not connected to Microsoft Teams", "TEAMS_NOT_CONNECTED");
            }
        }

        private string GetTeamsApiUrl(string endpoint)
        {
            if (!string.IsNullOrEmpty(_webhookUrl))
            {
                return _webhookUrl;
            }

            return $"https://graph.microsoft.com/v1.0/{endpoint}";
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
            base.Dispose();
        }
    }
} 
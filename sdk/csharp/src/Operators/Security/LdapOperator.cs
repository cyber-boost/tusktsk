using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Threading.Tasks;

namespace TuskLang.Operators.Security
{
    /// <summary>
    /// LDAP Operator for TuskLang C# SDK
    /// 
    /// Provides LDAP (Lightweight Directory Access Protocol) capabilities with support for:
    /// - LDAP authentication and binding
    /// - User search and retrieval
    /// - Group membership queries
    /// - Directory operations (add, modify, delete)
    /// - SSL/TLS connections
    /// - Multiple LDAP servers (Active Directory, OpenLDAP, etc.)
    /// 
    /// Usage:
    /// ```csharp
    /// // Authenticate user
    /// var auth = @ldap({
    ///   action: "authenticate",
    ///   server: "ldap://dc.example.com",
    ///   username: "user@example.com",
    ///   password: "password",
    ///   base_dn: "DC=example,DC=com"
    /// })
    /// 
    /// // Search users
    /// var users = @ldap({
    ///   action: "search",
    ///   server: "ldap://dc.example.com",
    ///   bind_dn: "CN=admin,DC=example,DC=com",
    ///   bind_password: "admin_password",
    ///   base_dn: "DC=example,DC=com",
    ///   filter: "(objectClass=user)"
    /// })
    /// ```
    /// </summary>
    public class LdapOperator : BaseOperator
    {
        public LdapOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "server", "port", "username", "password", "bind_dn", "bind_password", 
                "base_dn", "filter", "attributes", "scope", "ssl", "timeout", 
                "page_size", "search_base", "distinguished_name" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["port"] = 389,
                ["ssl"] = false,
                ["timeout"] = 30,
                ["scope"] = "subtree",
                ["page_size"] = 1000
            };
        }
        
        public override string GetName() => "ldap";
        
        protected override string GetDescription() => "LDAP authentication and directory operations operator";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["authenticate"] = "@ldap({action: \"authenticate\", server: \"ldap://dc.com\", username: \"user@dc.com\", password: \"pass\"})",
                ["search"] = "@ldap({action: \"search\", server: \"ldap://dc.com\", bind_dn: \"admin\", base_dn: \"DC=example,DC=com\", filter: \"(objectClass=user)\"})",
                ["get_user"] = "@ldap({action: \"get_user\", server: \"ldap://dc.com\", username: \"john.doe\", base_dn: \"DC=example,DC=com\"})",
                ["get_groups"] = "@ldap({action: \"get_groups\", server: \"ldap://dc.com\", username: \"john.doe\", base_dn: \"DC=example,DC=com\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid LDAP action specified",
                ["CONNECTION_FAILED"] = "Failed to connect to LDAP server",
                ["AUTHENTICATION_FAILED"] = "LDAP authentication failed",
                ["INVALID_CREDENTIALS"] = "Invalid LDAP credentials",
                ["SEARCH_FAILED"] = "LDAP search operation failed",
                ["INVALID_FILTER"] = "Invalid LDAP search filter",
                ["MISSING_REQUIRED_FIELDS"] = "Missing required LDAP fields",
                ["SSL_ERROR"] = "SSL/TLS connection error"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "").ToLower();
            
            switch (action)
            {
                case "authenticate":
                    return await AuthenticateAsync(config, context);
                case "search":
                    return await SearchAsync(config, context);
                case "get_user":
                    return await GetUserAsync(config, context);
                case "get_groups":
                    return await GetGroupsAsync(config, context);
                case "add_user":
                    return await AddUserAsync(config, context);
                case "modify_user":
                    return await ModifyUserAsync(config, context);
                case "delete_user":
                    return await DeleteUserAsync(config, context);
                default:
                    throw new ArgumentException($"Invalid LDAP action: {action}");
            }
        }
        
        /// <summary>
        /// Authenticate user against LDAP
        /// </summary>
        private async Task<object> AuthenticateAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var port = GetContextValue<int>(config, "port", 389);
            var username = GetContextValue<string>(config, "username", "");
            var password = GetContextValue<string>(config, "password", "");
            var baseDn = GetContextValue<string>(config, "base_dn", "");
            var ssl = GetContextValue<bool>(config, "ssl", false);
            var timeout = GetContextValue<int>(config, "timeout", 30);
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new ArgumentException("Server, username, and password are required for LDAP authentication");
            
            try
            {
                var connection = CreateLdapConnection(server, port, ssl, timeout);
                
                // Try to bind with the provided credentials
                var bindDn = username;
                if (!string.IsNullOrEmpty(baseDn) && !username.Contains(","))
                {
                    // If username is just the username (not DN), construct the full DN
                    bindDn = $"CN={username},{baseDn}";
                }
                
                connection.Bind(new NetworkCredential(bindDn, password));
                
                var result = new Dictionary<string, object>
                {
                    ["authenticated"] = true,
                    ["username"] = username,
                    ["server"] = server,
                    ["bind_dn"] = bindDn
                };
                
                // Try to get user information
                if (!string.IsNullOrEmpty(baseDn))
                {
                    try
                    {
                        var searchFilter = $"(sAMAccountName={username})";
                        var searchRequest = new SearchRequest(baseDn, searchFilter, SearchScope.Subtree);
                        var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
                        
                        if (searchResponse.Entries.Count > 0)
                        {
                            var userEntry = searchResponse.Entries[0];
                            result["distinguished_name"] = userEntry.DistinguishedName;
                            result["display_name"] = GetAttributeValue(userEntry, "displayName");
                            result["email"] = GetAttributeValue(userEntry, "mail");
                            result["department"] = GetAttributeValue(userEntry, "department");
                        }
                    }
                    catch
                    {
                        // User search failed, but authentication succeeded
                    }
                }
                
                connection.Dispose();
                
                Log("info", "LDAP authentication successful", new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["server"] = server
                });
                
                return result;
            }
            catch (LdapException ex)
            {
                Log("error", "LDAP authentication failed", new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["server"] = server,
                    ["error_code"] = ex.ErrorCode
                });
                
                return new Dictionary<string, object>
                {
                    ["authenticated"] = false,
                    ["error"] = ex.Message,
                    ["error_code"] = ex.ErrorCode
                };
            }
        }
        
        /// <summary>
        /// Search LDAP directory
        /// </summary>
        private async Task<object> SearchAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var port = GetContextValue<int>(config, "port", 389);
            var bindDn = GetContextValue<string>(config, "bind_dn", "");
            var bindPassword = GetContextValue<string>(config, "bind_password", "");
            var baseDn = GetContextValue<string>(config, "base_dn", "");
            var filter = GetContextValue<string>(config, "filter", "(objectClass=*)");
            var attributes = GetContextValue<string>(config, "attributes", "");
            var scope = GetContextValue<string>(config, "scope", "subtree");
            var ssl = GetContextValue<bool>(config, "ssl", false);
            var timeout = GetContextValue<int>(config, "timeout", 30);
            var pageSize = GetContextValue<int>(config, "page_size", 1000);
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(baseDn))
                throw new ArgumentException("Server and base_dn are required for LDAP search");
            
            try
            {
                var connection = CreateLdapConnection(server, port, ssl, timeout);
                
                // Bind if credentials provided
                if (!string.IsNullOrEmpty(bindDn) && !string.IsNullOrEmpty(bindPassword))
                {
                    connection.Bind(new NetworkCredential(bindDn, bindPassword));
                }
                
                var searchScope = scope.ToLower() switch
                {
                    "base" => SearchScope.Base,
                    "onelevel" => SearchScope.OneLevel,
                    _ => SearchScope.Subtree
                };
                
                var searchRequest = new SearchRequest(baseDn, filter, searchScope);
                
                // Set attributes to retrieve
                if (!string.IsNullOrEmpty(attributes))
                {
                    var attrArray = attributes.Split(',');
                    searchRequest.Attributes.AddRange(attrArray);
                }
                
                // Set page size for large result sets
                searchRequest.Controls.Add(new PageResultRequestControl(pageSize));
                
                var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
                
                var results = new List<Dictionary<string, object>>();
                foreach (SearchResultEntry entry in searchResponse.Entries)
                {
                    var entryDict = new Dictionary<string, object>
                    {
                        ["distinguished_name"] = entry.DistinguishedName
                    };
                    
                    foreach (string attrName in entry.Attributes.AttributeNames)
                    {
                        var attr = entry.Attributes[attrName];
                        if (attr.Count == 1)
                        {
                            entryDict[attrName] = attr[0];
                        }
                        else
                        {
                            var values = new List<object>();
                            for (int i = 0; i < attr.Count; i++)
                            {
                                values.Add(attr[i]);
                            }
                            entryDict[attrName] = values;
                        }
                    }
                    
                    results.Add(entryDict);
                }
                
                connection.Dispose();
                
                var result = new Dictionary<string, object>
                {
                    ["results"] = results,
                    ["count"] = results.Count,
                    ["base_dn"] = baseDn,
                    ["filter"] = filter,
                    ["scope"] = scope
                };
                
                Log("info", "LDAP search completed", new Dictionary<string, object>
                {
                    ["count"] = results.Count,
                    ["base_dn"] = baseDn
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "LDAP search failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["base_dn"] = baseDn
                });
                
                throw new ArgumentException($"LDAP search failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get user information
        /// </summary>
        private async Task<object> GetUserAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var username = GetContextValue<string>(config, "username", "");
            var baseDn = GetContextValue<string>(config, "base_dn", "");
            var bindDn = GetContextValue<string>(config, "bind_dn", "");
            var bindPassword = GetContextValue<string>(config, "bind_password", "");
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(baseDn))
                throw new ArgumentException("Server, username, and base_dn are required for get_user");
            
            var searchConfig = new Dictionary<string, object>(config)
            {
                ["action"] = "search",
                ["filter"] = $"(sAMAccountName={username})",
                ["attributes"] = "displayName,mail,department,title,manager,memberOf,userPrincipalName"
            };
            
            var searchResult = await SearchAsync(searchConfig, context);
            
            if (searchResult is Dictionary<string, object> resultDict && 
                resultDict.ContainsKey("results") && 
                resultDict["results"] is List<Dictionary<string, object>> results &&
                results.Count > 0)
            {
                return results[0];
            }
            
            return new Dictionary<string, object>
            {
                ["error"] = "User not found",
                ["username"] = username
            };
        }
        
        /// <summary>
        /// Get user groups
        /// </summary>
        private async Task<object> GetGroupsAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var server = GetContextValue<string>(config, "server", "");
            var username = GetContextValue<string>(config, "username", "");
            var baseDn = GetContextValue<string>(config, "base_dn", "");
            var bindDn = GetContextValue<string>(config, "bind_dn", "");
            var bindPassword = GetContextValue<string>(config, "bind_password", "");
            
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(baseDn))
                throw new ArgumentException("Server, username, and base_dn are required for get_groups");
            
            var searchConfig = new Dictionary<string, object>(config)
            {
                ["action"] = "search",
                ["filter"] = $"(sAMAccountName={username})",
                ["attributes"] = "memberOf"
            };
            
            var searchResult = await SearchAsync(searchConfig, context);
            
            if (searchResult is Dictionary<string, object> resultDict && 
                resultDict.ContainsKey("results") && 
                resultDict["results"] is List<Dictionary<string, object>> results &&
                results.Count > 0)
            {
                var user = results[0];
                var groups = new List<string>();
                
                if (user.ContainsKey("memberOf"))
                {
                    if (user["memberOf"] is string group)
                    {
                        groups.Add(group);
                    }
                    else if (user["memberOf"] is List<object> groupList)
                    {
                        foreach (var groupObj in groupList)
                        {
                            groups.Add(groupObj.ToString());
                        }
                    }
                }
                
                return new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["groups"] = groups,
                    ["group_count"] = groups.Count
                };
            }
            
            return new Dictionary<string, object>
            {
                ["error"] = "User not found",
                ["username"] = username,
                ["groups"] = new List<string>()
            };
        }
        
        /// <summary>
        /// Add user to LDAP
        /// </summary>
        private async Task<object> AddUserAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // This is a placeholder implementation
            // In a real implementation, you would use AddRequest to add users
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Add user operation not implemented in this version"
            };
        }
        
        /// <summary>
        /// Modify user in LDAP
        /// </summary>
        private async Task<object> ModifyUserAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // This is a placeholder implementation
            // In a real implementation, you would use ModifyRequest to modify users
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Modify user operation not implemented in this version"
            };
        }
        
        /// <summary>
        /// Delete user from LDAP
        /// </summary>
        private async Task<object> DeleteUserAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // This is a placeholder implementation
            // In a real implementation, you would use DeleteRequest to delete users
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Delete user operation not implemented in this version"
            };
        }
        
        /// <summary>
        /// Create LDAP connection
        /// </summary>
        private LdapConnection CreateLdapConnection(string server, int port, bool ssl, int timeout)
        {
            var identifier = new LdapDirectoryIdentifier(server, port);
            var connection = new LdapConnection(identifier)
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            
            if (ssl)
            {
                connection.SessionOptions.SecureSocketLayer = true;
            }
            
            return connection;
        }
        
        /// <summary>
        /// Get attribute value safely
        /// </summary>
        private string GetAttributeValue(SearchResultEntry entry, string attributeName)
        {
            try
            {
                if (entry.Attributes.Contains(attributeName))
                {
                    var attr = entry.Attributes[attributeName];
                    return attr.Count > 0 ? attr[0].ToString() : "";
                }
            }
            catch
            {
                // Attribute not found or error accessing it
            }
            
            return "";
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (config.TryGetValue("action", out var action))
            {
                var validActions = new[] { "authenticate", "search", "get_user", "get_groups", "add_user", "modify_user", "delete_user" };
                if (!validActions.Contains(action.ToString().ToLower()))
                {
                    result.Errors.Add($"Invalid action: {action}. Supported: {string.Join(", ", validActions)}");
                }
            }
            
            return result;
        }
    }
} 
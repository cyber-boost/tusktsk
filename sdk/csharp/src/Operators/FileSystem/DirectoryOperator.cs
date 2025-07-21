using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace TuskLang.Operators.FileSystem
{
    /// <summary>
    /// Directory Operator for TuskLang C# SDK
    /// 
    /// Provides directory operations with support for:
    /// - Directory creation and deletion
    /// - Directory listing and searching
    /// - Directory copying and moving
    /// - Directory information and metadata
    /// - File and subdirectory enumeration
    /// - Directory permissions and security
    /// - Directory size calculation
    /// 
    /// Usage:
    /// ```csharp
    /// // Create directory
    /// var result = @directory({
    ///   action: "create",
    ///   path: "/path/to/new/directory"
    /// })
    /// 
    /// // List directory contents
    /// var result = @directory({
    ///   action: "list",
    ///   path: "/path/to/directory"
    /// })
    /// ```
    /// </summary>
    public class DirectoryOperator : BaseOperator
    {
        public DirectoryOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "path", "pattern", "recursive", "include_files", "include_directories",
                "sort_by", "sort_direction", "limit", "offset", "permissions",
                "timeout", "backup", "atomic"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["recursive"] = false,
                ["include_files"] = true,
                ["include_directories"] = true,
                ["sort_by"] = "name",
                ["sort_direction"] = "asc",
                ["timeout"] = 300,
                ["atomic"] = false
            };
        }
        
        public override string GetName() => "directory";
        
        protected override string GetDescription() => "Directory operator for managing directories and their contents";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["create"] = "@directory({action: \"create\", path: \"/path/to/new/directory\"})",
                ["list"] = "@directory({action: \"list\", path: \"/path/to/directory\"})",
                ["copy"] = "@directory({action: \"copy\", path: \"/source/dir\", destination: \"/dest/dir\"})",
                ["info"] = "@directory({action: \"info\", path: \"/path/to/directory\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_PATH"] = "Invalid directory path",
                ["DIRECTORY_NOT_FOUND"] = "Directory not found",
                ["ACCESS_DENIED"] = "Access denied",
                ["DIRECTORY_EXISTS"] = "Directory already exists",
                ["DISK_FULL"] = "Disk full",
                ["TIMEOUT_EXCEEDED"] = "Directory operation timeout exceeded",
                ["PERMISSION_ERROR"] = "Permission error",
                ["RECURSIVE_ERROR"] = "Recursive operation error"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "");
            var path = GetContextValue<string>(config, "path", "");
            var pattern = GetContextValue<string>(config, "pattern", "*");
            var recursive = GetContextValue<bool>(config, "recursive", false);
            var includeFiles = GetContextValue<bool>(config, "include_files", true);
            var includeDirectories = GetContextValue<bool>(config, "include_directories", true);
            var sortBy = GetContextValue<string>(config, "sort_by", "name");
            var sortDirection = GetContextValue<string>(config, "sort_direction", "asc");
            var limit = GetContextValue<int>(config, "limit", 0);
            var offset = GetContextValue<int>(config, "offset", 0);
            var permissions = GetContextValue<string>(config, "permissions", "");
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var backup = GetContextValue<bool>(config, "backup", false);
            var atomic = GetContextValue<bool>(config, "atomic", false);
            
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("Action is required");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Check timeout
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException("Directory operation timeout exceeded");
                }
                
                switch (action.ToLower())
                {
                    case "create":
                        return await CreateDirectoryAsync(path, permissions, atomic);
                    
                    case "delete":
                        return await DeleteDirectoryAsync(path, recursive, backup);
                    
                    case "list":
                        return await ListDirectoryAsync(path, pattern, recursive, includeFiles, includeDirectories, sortBy, sortDirection, limit, offset);
                    
                    case "copy":
                        return await CopyDirectoryAsync(path, config, recursive, backup);
                    
                    case "move":
                        return await MoveDirectoryAsync(path, config, recursive, backup);
                    
                    case "info":
                        return await GetDirectoryInfoAsync(path);
                    
                    case "exists":
                        return await CheckDirectoryExistsAsync(path);
                    
                    case "size":
                        return await GetDirectorySizeAsync(path, recursive);
                    
                    case "empty":
                        return await EmptyDirectoryAsync(path, backup);
                    
                    case "search":
                        return await SearchDirectoryAsync(path, pattern, recursive, includeFiles, includeDirectories, limit, offset);
                    
                    default:
                        throw new ArgumentException($"Unknown directory action: {action}");
                }
            }
            catch (Exception ex)
            {
                Log("error", "Directory operation failed", new Dictionary<string, object>
                {
                    ["action"] = action,
                    ["path"] = path,
                    ["error"] = ex.Message
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message,
                    ["action"] = action,
                    ["path"] = path
                };
            }
        }
        
        /// <summary>
        /// Create directory
        /// </summary>
        private async Task<object> CreateDirectoryAsync(string path, string permissions, bool atomic)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory path is required");
            
            try
            {
                var finalPath = atomic ? path + ".tmp" : path;
                
                if (Directory.Exists(finalPath))
                {
                    throw new ArgumentException($"Directory already exists: {finalPath}");
                }
                
                Directory.CreateDirectory(finalPath);
                
                // Move temp directory to final location if atomic
                if (atomic)
                {
                    Directory.Move(finalPath, path);
                }
                
                var directoryInfo = new DirectoryInfo(path);
                
                Log("info", "Directory created successfully", new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["created"] = directoryInfo.CreationTime
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["created"] = directoryInfo.CreationTime,
                    ["created"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to create directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Delete directory
        /// </summary>
        private async Task<object> DeleteDirectoryAsync(string path, bool recursive, bool backup)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory path is required");
            
            if (!Directory.Exists(path))
                throw new ArgumentException($"Directory not found: {path}");
            
            try
            {
                // Create backup if requested
                if (backup)
                {
                    var backupPath = path + ".backup";
                    CopyDirectoryRecursive(path, backupPath);
                }
                
                Directory.Delete(path, recursive);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["recursive"] = recursive,
                    ["deleted"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to delete directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// List directory contents
        /// </summary>
        private async Task<object> ListDirectoryAsync(string path, string pattern, bool recursive, bool includeFiles, bool includeDirectories, string sortBy, string sortDirection, int limit, int offset)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory path is required");
            
            if (!Directory.Exists(path))
                throw new ArgumentException($"Directory not found: {path}");
            
            try
            {
                var items = new List<Dictionary<string, object>>();
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                // Get files
                if (includeFiles)
                {
                    var files = Directory.GetFiles(path, pattern, searchOption);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        items.Add(new Dictionary<string, object>
                        {
                            ["name"] = fileInfo.Name,
                            ["path"] = file,
                            ["type"] = "file",
                            ["size"] = fileInfo.Length,
                            ["created"] = fileInfo.CreationTime,
                            ["modified"] = fileInfo.LastWriteTime,
                            ["extension"] = fileInfo.Extension
                        });
                    }
                }
                
                // Get directories
                if (includeDirectories)
                {
                    var directories = Directory.GetDirectories(path, pattern, searchOption);
                    foreach (var directory in directories)
                    {
                        var directoryInfo = new DirectoryInfo(directory);
                        items.Add(new Dictionary<string, object>
                        {
                            ["name"] = directoryInfo.Name,
                            ["path"] = directory,
                            ["type"] = "directory",
                            ["created"] = directoryInfo.CreationTime,
                            ["modified"] = directoryInfo.LastWriteTime
                        });
                    }
                }
                
                // Sort items
                items = SortItems(items, sortBy, sortDirection);
                
                // Apply pagination
                if (offset > 0)
                {
                    items = items.Skip(offset).ToList();
                }
                
                if (limit > 0)
                {
                    items = items.Take(limit).ToList();
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["pattern"] = pattern,
                    ["recursive"] = recursive,
                    ["items"] = items,
                    ["total_count"] = items.Count,
                    ["sort_by"] = sortBy,
                    ["sort_direction"] = sortDirection
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to list directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Copy directory
        /// </summary>
        private async Task<object> CopyDirectoryAsync(string path, Dictionary<string, object> config, bool recursive, bool backup)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Source directory path is required");
            
            var destination = GetContextValue<string>(config, "destination", "");
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("Destination path is required");
            
            if (!Directory.Exists(path))
                throw new ArgumentException($"Source directory not found: {path}");
            
            try
            {
                // Create backup if requested
                if (backup && Directory.Exists(destination))
                {
                    var backupPath = destination + ".backup";
                    CopyDirectoryRecursive(destination, backupPath);
                }
                
                // Create destination directory if needed
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }
                
                CopyDirectoryRecursive(path, destination);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["source"] = path,
                    ["destination"] = destination,
                    ["recursive"] = recursive,
                    ["copied"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to copy directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Move directory
        /// </summary>
        private async Task<object> MoveDirectoryAsync(string path, Dictionary<string, object> config, bool recursive, bool backup)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Source directory path is required");
            
            var destination = GetContextValue<string>(config, "destination", "");
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("Destination path is required");
            
            if (!Directory.Exists(path))
                throw new ArgumentException($"Source directory not found: {path}");
            
            try
            {
                // Create backup if requested
                if (backup && Directory.Exists(destination))
                {
                    var backupPath = destination + ".backup";
                    CopyDirectoryRecursive(destination, backupPath);
                }
                
                // Create destination directory if needed
                var destParent = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destParent) && !Directory.Exists(destParent))
                {
                    Directory.CreateDirectory(destParent);
                }
                
                Directory.Move(path, destination);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["source"] = path,
                    ["destination"] = destination,
                    ["moved"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to move directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get directory information
        /// </summary>
        private async Task<object> GetDirectoryInfoAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory path is required");
            
            if (!Directory.Exists(path))
                throw new ArgumentException($"Directory not found: {path}");
            
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                var files = directoryInfo.GetFiles();
                var directories = directoryInfo.GetDirectories();
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["name"] = directoryInfo.Name,
                    ["parent"] = directoryInfo.Parent?.FullName,
                    ["created"] = directoryInfo.CreationTime,
                    ["modified"] = directoryInfo.LastWriteTime,
                    ["accessed"] = directoryInfo.LastAccessTime,
                    ["file_count"] = files.Length,
                    ["directory_count"] = directories.Length,
                    ["total_count"] = files.Length + directories.Length,
                    ["exists"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to get directory info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if directory exists
        /// </summary>
        private async Task<object> CheckDirectoryExistsAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory path is required");
            
            var exists = Directory.Exists(path);
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["path"] = path,
                ["exists"] = exists
            };
        }
        
        /// <summary>
        /// Get directory size
        /// </summary>
        private async Task<object> GetDirectorySizeAsync(string path, bool recursive)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory path is required");
            
            if (!Directory.Exists(path))
                throw new ArgumentException($"Directory not found: {path}");
            
            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(path, "*", searchOption);
                
                long totalSize = 0;
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["size"] = totalSize,
                    ["size_kb"] = totalSize / 1024.0,
                    ["size_mb"] = totalSize / (1024.0 * 1024.0),
                    ["file_count"] = files.Length,
                    ["recursive"] = recursive
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to get directory size: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Empty directory
        /// </summary>
        private async Task<object> EmptyDirectoryAsync(string path, bool backup)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory path is required");
            
            if (!Directory.Exists(path))
                throw new ArgumentException($"Directory not found: {path}");
            
            try
            {
                // Create backup if requested
                if (backup)
                {
                    var backupPath = path + ".backup";
                    CopyDirectoryRecursive(path, backupPath);
                }
                
                var files = Directory.GetFiles(path);
                var directories = Directory.GetDirectories(path);
                
                // Delete files
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                
                // Delete directories
                foreach (var directory in directories)
                {
                    Directory.Delete(directory, true);
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["files_deleted"] = files.Length,
                    ["directories_deleted"] = directories.Length,
                    ["emptied"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to empty directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Search directory
        /// </summary>
        private async Task<object> SearchDirectoryAsync(string path, string pattern, bool recursive, bool includeFiles, bool includeDirectories, int limit, int offset)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Directory path is required");
            
            if (!Directory.Exists(path))
                throw new ArgumentException($"Directory not found: {path}");
            
            try
            {
                var items = new List<Dictionary<string, object>>();
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                // Search files
                if (includeFiles)
                {
                    var files = Directory.GetFiles(path, pattern, searchOption);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        items.Add(new Dictionary<string, object>
                        {
                            ["name"] = fileInfo.Name,
                            ["path"] = file,
                            ["type"] = "file",
                            ["size"] = fileInfo.Length,
                            ["created"] = fileInfo.CreationTime,
                            ["modified"] = fileInfo.LastWriteTime
                        });
                    }
                }
                
                // Search directories
                if (includeDirectories)
                {
                    var directories = Directory.GetDirectories(path, pattern, searchOption);
                    foreach (var directory in directories)
                    {
                        var directoryInfo = new DirectoryInfo(directory);
                        items.Add(new Dictionary<string, object>
                        {
                            ["name"] = directoryInfo.Name,
                            ["path"] = directory,
                            ["type"] = "directory",
                            ["created"] = directoryInfo.CreationTime,
                            ["modified"] = directoryInfo.LastWriteTime
                        });
                    }
                }
                
                // Apply pagination
                if (offset > 0)
                {
                    items = items.Skip(offset).ToList();
                }
                
                if (limit > 0)
                {
                    items = items.Take(limit).ToList();
                }
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["pattern"] = pattern,
                    ["recursive"] = recursive,
                    ["items"] = items,
                    ["total_count"] = items.Count
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to search directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Copy directory recursively
        /// </summary>
        private void CopyDirectoryRecursive(string source, string destination)
        {
            var sourceInfo = new DirectoryInfo(source);
            var destinationInfo = Directory.CreateDirectory(destination);
            
            // Copy files
            foreach (var file in sourceInfo.GetFiles())
            {
                file.CopyTo(Path.Combine(destination, file.Name), true);
            }
            
            // Copy subdirectories
            foreach (var subdirectory in sourceInfo.GetDirectories())
            {
                var newDestination = Path.Combine(destination, subdirectory.Name);
                CopyDirectoryRecursive(subdirectory.FullName, newDestination);
            }
        }
        
        /// <summary>
        /// Sort items
        /// </summary>
        private List<Dictionary<string, object>> SortItems(List<Dictionary<string, object>> items, string sortBy, string sortDirection)
        {
            var sorted = sortBy.ToLower() switch
            {
                "name" => items.OrderBy(x => x["name"]).ToList(),
                "size" => items.OrderBy(x => x.GetValueOrDefault("size", 0L)).ToList(),
                "created" => items.OrderBy(x => x["created"]).ToList(),
                "modified" => items.OrderBy(x => x["modified"]).ToList(),
                "type" => items.OrderBy(x => x["type"]).ToList(),
                _ => items
            };
            
            if (sortDirection.ToLower() == "desc")
            {
                sorted.Reverse();
            }
            
            return sorted;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("action"))
            {
                result.Errors.Add("Action is required");
            }
            
            var action = GetContextValue<string>(config, "action", "");
            var validActions = new[] { "create", "delete", "list", "copy", "move", "info", "exists", "size", "empty", "search" };
            
            if (!string.IsNullOrEmpty(action) && !Array.Exists(validActions, a => a == action.ToLower()))
            {
                result.Errors.Add($"Invalid action: {action}. Valid actions are: {string.Join(", ", validActions)}");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            if (config.TryGetValue("limit", out var limit) && limit is int limitValue && limitValue < 0)
            {
                result.Errors.Add("Limit must be non-negative");
            }
            
            if (config.TryGetValue("offset", out var offset) && offset is int offsetValue && offsetValue < 0)
            {
                result.Errors.Add("Offset must be non-negative");
            }
            
            return result;
        }
    }
} 
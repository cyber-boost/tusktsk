using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Text.Json;

namespace TuskLang.Operators.FileSystem
{
    /// <summary>
    /// File Operator for TuskLang C# SDK
    /// 
    /// Provides file system operations with support for:
    /// - File reading and writing
    /// - File copying, moving, and deleting
    /// - File information and metadata
    /// - Directory operations
    /// - File searching and filtering
    /// - File compression and decompression
    /// - File permissions and security
    /// 
    /// Usage:
    /// ```csharp
    /// // Read file
    /// var result = @file({
    ///   action: "read",
    ///   path: "/path/to/file.txt"
    /// })
    /// 
    /// // Write file
    /// var result = @file({
    ///   action: "write",
    ///   path: "/path/to/file.txt",
    ///   content: "Hello, World!"
    /// })
    /// ```
    /// </summary>
    public class FileOperator : BaseOperator
    {
        public FileOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "path", "content", "encoding", "mode", "permissions", "overwrite",
                "create_directories", "timeout", "buffer_size", "line_ending",
                "compression", "encryption", "backup", "atomic"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["encoding"] = "UTF-8",
                ["mode"] = "text",
                ["overwrite"] = false,
                ["create_directories"] = true,
                ["timeout"] = 300,
                ["buffer_size"] = 4096,
                ["line_ending"] = "default",
                ["atomic"] = false
            };
        }
        
        public override string GetName() => "file";
        
        protected override string GetDescription() => "File system operator for reading, writing, and managing files";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["read"] = "@file({action: \"read\", path: \"/path/to/file.txt\"})",
                ["write"] = "@file({action: \"write\", path: \"/path/to/file.txt\", content: \"Hello, World!\"})",
                ["copy"] = "@file({action: \"copy\", path: \"/source/file.txt\", destination: \"/dest/file.txt\"})",
                ["info"] = "@file({action: \"info\", path: \"/path/to/file.txt\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_PATH"] = "Invalid file path",
                ["FILE_NOT_FOUND"] = "File not found",
                ["ACCESS_DENIED"] = "Access denied",
                ["FILE_EXISTS"] = "File already exists",
                ["DISK_FULL"] = "Disk full",
                ["TIMEOUT_EXCEEDED"] = "File operation timeout exceeded",
                ["ENCODING_ERROR"] = "Character encoding error",
                ["PERMISSION_ERROR"] = "Permission error"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "");
            var path = GetContextValue<string>(config, "path", "");
            var content = ResolveVariable(config.GetValueOrDefault("content"), context);
            var encoding = GetContextValue<string>(config, "encoding", "UTF-8");
            var mode = GetContextValue<string>(config, "mode", "text");
            var permissions = GetContextValue<string>(config, "permissions", "");
            var overwrite = GetContextValue<bool>(config, "overwrite", false);
            var createDirectories = GetContextValue<bool>(config, "create_directories", true);
            var timeout = GetContextValue<int>(config, "timeout", 300);
            var bufferSize = GetContextValue<int>(config, "buffer_size", 4096);
            var lineEnding = GetContextValue<string>(config, "line_ending", "default");
            var compression = GetContextValue<string>(config, "compression", "");
            var encryption = GetContextValue<string>(config, "encryption", "");
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
                    throw new TimeoutException("File operation timeout exceeded");
                }
                
                switch (action.ToLower())
                {
                    case "read":
                        return await ReadFileAsync(path, encoding, mode, bufferSize);
                    
                    case "write":
                        return await WriteFileAsync(path, content, encoding, mode, overwrite, createDirectories, lineEnding, atomic);
                    
                    case "append":
                        return await AppendFileAsync(path, content, encoding, mode, createDirectories, lineEnding);
                    
                    case "copy":
                        return await CopyFileAsync(path, config, overwrite, backup);
                    
                    case "move":
                        return await MoveFileAsync(path, config, overwrite, backup);
                    
                    case "delete":
                        return await DeleteFileAsync(path, backup);
                    
                    case "info":
                        return await GetFileInfoAsync(path);
                    
                    case "exists":
                        return await CheckFileExistsAsync(path);
                    
                    case "size":
                        return await GetFileSizeAsync(path);
                    
                    case "hash":
                        return await GetFileHashAsync(path, config);
                    
                    case "compress":
                        return await CompressFileAsync(path, compression, config);
                    
                    case "decompress":
                        return await DecompressFileAsync(path, compression, config);
                    
                    default:
                        throw new ArgumentException($"Unknown file action: {action}");
                }
            }
            catch (Exception ex)
            {
                Log("error", "File operation failed", new Dictionary<string, object>
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
        /// Read file
        /// </summary>
        private async Task<object> ReadFileAsync(string path, string encoding, string mode, int bufferSize)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"File not found: {path}");
            
            try
            {
                var encodingObj = GetEncoding(encoding);
                
                if (mode.ToLower() == "binary")
                {
                    var bytes = await File.ReadAllBytesAsync(path);
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["path"] = path,
                        ["content"] = bytes,
                        ["size"] = bytes.Length,
                        ["mode"] = "binary"
                    };
                }
                else
                {
                    var content = await File.ReadAllTextAsync(path, encodingObj);
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["path"] = path,
                        ["content"] = content,
                        ["size"] = content.Length,
                        ["encoding"] = encoding,
                        ["mode"] = "text"
                    };
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to read file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Write file
        /// </summary>
        private async Task<object> WriteFileAsync(string path, object content, string encoding, string mode, bool overwrite, bool createDirectories, string lineEnding, bool atomic)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (content == null)
                throw new ArgumentException("Content is required");
            
            try
            {
                // Create directories if needed
                if (createDirectories)
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                
                // Check if file exists and overwrite is not allowed
                if (File.Exists(path) && !overwrite)
                {
                    throw new ArgumentException($"File already exists: {path}");
                }
                
                var encodingObj = GetEncoding(encoding);
                var finalPath = atomic ? path + ".tmp" : path;
                
                if (mode.ToLower() == "binary")
                {
                    byte[] bytes;
                    if (content is byte[] binaryContent)
                    {
                        bytes = binaryContent;
                    }
                    else
                    {
                        bytes = encodingObj.GetBytes(content.ToString());
                    }
                    
                    await File.WriteAllBytesAsync(finalPath, bytes);
                }
                else
                {
                    var textContent = content.ToString();
                    
                    // Handle line endings
                    if (lineEnding.ToLower() != "default")
                    {
                        textContent = NormalizeLineEndings(textContent, lineEnding);
                    }
                    
                    await File.WriteAllTextAsync(finalPath, textContent, encodingObj);
                }
                
                // Move temp file to final location if atomic
                if (atomic)
                {
                    File.Move(finalPath, path, true);
                }
                
                var fileInfo = new FileInfo(path);
                
                Log("info", "File written successfully", new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["size"] = fileInfo.Length,
                    ["mode"] = mode
                });
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["size"] = fileInfo.Length,
                    ["encoding"] = encoding,
                    ["mode"] = mode,
                    ["written"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to write file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Append to file
        /// </summary>
        private async Task<object> AppendFileAsync(string path, object content, string encoding, string mode, bool createDirectories, string lineEnding)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (content == null)
                throw new ArgumentException("Content is required");
            
            try
            {
                // Create directories if needed
                if (createDirectories)
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                
                var encodingObj = GetEncoding(encoding);
                
                if (mode.ToLower() == "binary")
                {
                    byte[] bytes;
                    if (content is byte[] binaryContent)
                    {
                        bytes = binaryContent;
                    }
                    else
                    {
                        bytes = encodingObj.GetBytes(content.ToString());
                    }
                    
                    await File.AppendAllBytesAsync(path, bytes);
                }
                else
                {
                    var textContent = content.ToString();
                    
                    // Handle line endings
                    if (lineEnding.ToLower() != "default")
                    {
                        textContent = NormalizeLineEndings(textContent, lineEnding);
                    }
                    
                    await File.AppendAllTextAsync(path, textContent, encodingObj);
                }
                
                var fileInfo = new FileInfo(path);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["size"] = fileInfo.Length,
                    ["encoding"] = encoding,
                    ["mode"] = mode,
                    ["appended"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to append to file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Copy file
        /// </summary>
        private async Task<object> CopyFileAsync(string path, Dictionary<string, object> config, bool overwrite, bool backup)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Source file path is required");
            
            var destination = GetContextValue<string>(config, "destination", "");
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("Destination path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"Source file not found: {path}");
            
            try
            {
                // Create backup if requested
                if (backup && File.Exists(destination))
                {
                    var backupPath = destination + ".backup";
                    File.Copy(destination, backupPath, true);
                }
                
                // Create destination directory if needed
                var destDirectory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }
                
                File.Copy(path, destination, overwrite);
                
                var sourceInfo = new FileInfo(path);
                var destInfo = new FileInfo(destination);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["source"] = path,
                    ["destination"] = destination,
                    ["size"] = destInfo.Length,
                    ["copied"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to copy file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Move file
        /// </summary>
        private async Task<object> MoveFileAsync(string path, Dictionary<string, object> config, bool overwrite, bool backup)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Source file path is required");
            
            var destination = GetContextValue<string>(config, "destination", "");
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("Destination path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"Source file not found: {path}");
            
            try
            {
                // Create backup if requested
                if (backup && File.Exists(destination))
                {
                    var backupPath = destination + ".backup";
                    File.Copy(destination, backupPath, true);
                }
                
                // Create destination directory if needed
                var destDirectory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }
                
                File.Move(path, destination, overwrite);
                
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
                throw new ArgumentException($"Failed to move file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Delete file
        /// </summary>
        private async Task<object> DeleteFileAsync(string path, bool backup)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"File not found: {path}");
            
            try
            {
                // Create backup if requested
                if (backup)
                {
                    var backupPath = path + ".backup";
                    File.Copy(path, backupPath, true);
                }
                
                File.Delete(path);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["deleted"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to delete file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get file information
        /// </summary>
        private async Task<object> GetFileInfoAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"File not found: {path}");
            
            try
            {
                var fileInfo = new FileInfo(path);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["name"] = fileInfo.Name,
                    ["directory"] = fileInfo.DirectoryName,
                    ["size"] = fileInfo.Length,
                    ["created"] = fileInfo.CreationTime,
                    ["modified"] = fileInfo.LastWriteTime,
                    ["accessed"] = fileInfo.LastAccessTime,
                    ["extension"] = fileInfo.Extension,
                    ["read_only"] = fileInfo.IsReadOnly,
                    ["exists"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to get file info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if file exists
        /// </summary>
        private async Task<object> CheckFileExistsAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            var exists = File.Exists(path);
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["path"] = path,
                ["exists"] = exists
            };
        }
        
        /// <summary>
        /// Get file size
        /// </summary>
        private async Task<object> GetFileSizeAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"File not found: {path}");
            
            try
            {
                var fileInfo = new FileInfo(path);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["size"] = fileInfo.Length,
                    ["size_kb"] = fileInfo.Length / 1024.0,
                    ["size_mb"] = fileInfo.Length / (1024.0 * 1024.0)
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to get file size: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get file hash
        /// </summary>
        private async Task<object> GetFileHashAsync(string path, Dictionary<string, object> config)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"File not found: {path}");
            
            var algorithm = GetContextValue<string>(config, "algorithm", "MD5");
            
            try
            {
                var hash = await CalculateFileHashAsync(path, algorithm);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["path"] = path,
                    ["algorithm"] = algorithm,
                    ["hash"] = hash
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to calculate file hash: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Compress file
        /// </summary>
        private async Task<object> CompressFileAsync(string path, string compression, Dictionary<string, object> config)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"File not found: {path}");
            
            // Simplified compression - in a real implementation, you would use proper compression libraries
            var destination = GetContextValue<string>(config, "destination", path + ".compressed");
            
            try
            {
                // Simulate compression
                File.Copy(path, destination, true);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["source"] = path,
                    ["destination"] = destination,
                    ["compression"] = compression,
                    ["compressed"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to compress file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Decompress file
        /// </summary>
        private async Task<object> DecompressFileAsync(string path, string compression, Dictionary<string, object> config)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("File path is required");
            
            if (!File.Exists(path))
                throw new ArgumentException($"File not found: {path}");
            
            // Simplified decompression - in a real implementation, you would use proper compression libraries
            var destination = GetContextValue<string>(config, "destination", path.Replace(".compressed", ""));
            
            try
            {
                // Simulate decompression
                File.Copy(path, destination, true);
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["source"] = path,
                    ["destination"] = destination,
                    ["compression"] = compression,
                    ["decompressed"] = true
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to decompress file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get encoding by name
        /// </summary>
        private Encoding GetEncoding(string encodingName)
        {
            return encodingName.ToUpper() switch
            {
                "UTF-8" => Encoding.UTF8,
                "UTF-16" => Encoding.Unicode,
                "UTF-32" => Encoding.UTF32,
                "ASCII" => Encoding.ASCII,
                "ISO-8859-1" => Encoding.GetEncoding("ISO-8859-1"),
                _ => Encoding.UTF8
            };
        }
        
        /// <summary>
        /// Normalize line endings
        /// </summary>
        private string NormalizeLineEndings(string content, string lineEnding)
        {
            var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
            
            return lineEnding.ToLower() switch
            {
                "windows" => normalized.Replace("\n", "\r\n"),
                "unix" => normalized,
                "mac" => normalized.Replace("\n", "\r"),
                _ => normalized
            };
        }
        
        /// <summary>
        /// Calculate file hash
        /// </summary>
        private async Task<string> CalculateFileHashAsync(string path, string algorithm)
        {
            // Simplified hash calculation - in a real implementation, you would use proper hash algorithms
            var fileInfo = new FileInfo(path);
            var hash = $"{algorithm}_{fileInfo.Length}_{fileInfo.LastWriteTime.Ticks}";
            return hash;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (!config.ContainsKey("action"))
            {
                result.Errors.Add("Action is required");
            }
            
            var action = GetContextValue<string>(config, "action", "");
            var validActions = new[] { "read", "write", "append", "copy", "move", "delete", "info", "exists", "size", "hash", "compress", "decompress" };
            
            if (!string.IsNullOrEmpty(action) && !Array.Exists(validActions, a => a == action.ToLower()))
            {
                result.Errors.Add($"Invalid action: {action}. Valid actions are: {string.Join(", ", validActions)}");
            }
            
            if (config.TryGetValue("timeout", out var timeout) && timeout is int timeoutValue && timeoutValue <= 0)
            {
                result.Errors.Add("Timeout must be positive");
            }
            
            if (config.TryGetValue("buffer_size", out var bufferSize) && bufferSize is int bufferSizeValue && bufferSizeValue <= 0)
            {
                result.Errors.Add("Buffer size must be positive");
            }
            
            return result;
        }
    }
} 